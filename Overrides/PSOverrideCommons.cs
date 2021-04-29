using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.UI;
using Klyte.PropSwitcher.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;
using static RenderManager;

namespace Klyte.PropSwitcher.Overrides
{
    internal class PSOverrideCommons
    {
        private static PSOverrideCommons instance;

        public static PSOverrideCommons Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PSOverrideCommons();
                }
                return instance;
            }
        }

        public static void Reset() => instance = null;

        internal readonly Dictionary<long, Item> SwitcherCache = new Dictionary<long, Item>();

        private readonly HashSet<Vector4> renderOverlayCirclePositions = new HashSet<Vector4>();

        private long ToCacheKey(ref InstanceID id, int propIdx) => ((long)id.RawData << 16) | ((long)propIdx & ((1 << 16) - 1));

        public bool GetTargetInfo_internal(PrefabInfo info, ref InstanceID id, ref Vector3 randomizerParameters, int propIdx, bool isCalc, out Item result)
        {
            var cacheKey = ToCacheKey(ref id, propIdx);
            if (!isCalc)
            {
                return SwitcherCache.TryGetValue(cacheKey, out result) && !(result is null);
            }


            if (info is null || PSPropData.Instance?.Entries is null)
            {
                result = null;
                return false;
            }

            if (SwitcherCache.ContainsKey(cacheKey))
            {
                SwitcherCache.Remove(cacheKey);
            }

            string parentName = null;
            var refId = id;
            if (refId.NetLane != 0)
            {
                refId.NetSegment = NetManager.instance.m_lanes.m_buffer[refId.NetLane].m_segment;
            }
            if (refId.NetSegment != 0)
            {
                parentName = NetManager.instance.m_segments.m_buffer[refId.NetSegment].Info.name;

            }
            else if (refId.NetNode != 0)
            {
                parentName = NetManager.instance.m_nodes.m_buffer[refId.NetNode].Info.name;
            }
            else if (refId.Building != 0)
            {
                parentName = BuildingManager.instance.m_buildings.m_buffer[refId.Building].Info.name;
            }

            XmlDictionary<PrefabChildEntryKey, SwitchInfo> switchInfoDictGlobal = null;
            SwitchInfo switchInfo = null;
            SwitchInfo.Item infoItem = null;
            var defaultKey = new PrefabChildEntryKey(info.name);
            var indexKey = new PrefabChildEntryKey(propIdx);
            if (parentName != null)
            {
                if ((PSPropData.Instance.PrefabChildEntries.TryGetValue(parentName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> switchInfoDict)) | (PropSwitcherMod.Controller?.GlobalPrefabChildEntries?.TryGetValue(parentName, out switchInfoDictGlobal) ?? false))
                {
                    if (propIdx >= 0 && ((switchInfoDict?.TryGetValue(indexKey, out switchInfo) ?? false) || (switchInfoDictGlobal?.TryGetValue(indexKey, out switchInfo) ?? false)))
                    {
                        if (TryApplyInfo(ref refId, switchInfo, ref infoItem, ref randomizerParameters))
                        {
                            result = infoItem;
                            SwitcherCache[cacheKey] = result;
                            return true;
                        }
                    }
                    if ((switchInfoDict?.TryGetValue(defaultKey, out switchInfo) ?? false) || (switchInfoDictGlobal?.TryGetValue(defaultKey, out switchInfo) ?? false))
                    {
                        if (TryApplyInfo(ref refId, switchInfo, ref infoItem, ref randomizerParameters))
                        {
                            result = infoItem;
                            SwitcherCache[cacheKey] = result;
                            return true;
                        }
                    }
                }
            }
            if (PSPropData.Instance.Entries.ContainsKey(defaultKey))
            {
                switchInfo = PSPropData.Instance.Entries[defaultKey];
                if (TryApplyInfo(ref refId, switchInfo, ref infoItem, ref randomizerParameters))
                {
                    result = infoItem;
                    SwitcherCache[cacheKey] = result;
                    return true;
                }
            }
            result = null;
            return false;

        }

        private static bool TryApplyInfo(ref InstanceID id, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem, ref Vector3 randomizerParameters)
        {
            if (switchInfo.SwitchItems.Length > 0)
            {
                if (switchInfo.SwitchItems.Length == 1)
                {
                    infoItem = switchInfo.SwitchItems[0];
                }
                else
                {
                    var positionSeed = ((Mathf.RoundToInt(randomizerParameters.x) | 1) * (Mathf.RoundToInt(randomizerParameters.z) | 1)) + (Math.Abs(Mathf.RoundToInt(randomizerParameters.y) | 1) * 17);
                    var seed = switchInfo.SeedSource == SwitchInfo.RandomizerSeedSource.POSITION || id == default || id.Prop != 0 ? positionSeed : (int)id.Index;
                    var r = new Randomizer(seed);
                    var targetIdx = r.Int32((uint)switchInfo.SwitchItems.Length);
                    infoItem = switchInfo.SwitchItems[targetIdx];
                }

                return true;
            }
            return false;
        }

        public void CheckIfShallCircle(string parentName, PrefabInfo info, int propIdx, Vector3 position)
        {
            if (PSBuildingPropTab.Instance?.component?.isVisible is true && !(info is null))
            {
                if (parentName == PSBuildingPropTab.Instance.GetCurrentParentPrefabInfo()?.name)
                {

                    var currentKey = PSBuildingPropTab.Instance?.GetCurrentEditingKey();
                    if (!(currentKey is null) && (currentKey.SourcePrefab == info.name || currentKey.PrefabIdx == propIdx))
                    {
                        var magnitude = 0f;
                        if (info is TreeInfo tf)
                        {
                            magnitude = Mathf.Max(tf.m_mesh.bounds.size.x, tf.m_mesh.bounds.size.z, 1);
                        }
                        if (info is PropInfo pf)
                        {
                            magnitude = Mathf.Max(pf.m_mesh.bounds.size.x, pf.m_mesh.bounds.size.z, 1);
                        }
                        renderOverlayCirclePositions.Add(new Vector4(position.x, position.y, position.z, magnitude));

                    }
                }
            }
        }

        public readonly Color orange = new Color(1, .5f, 0, 1);

        public void OnRenderOverlay(CameraInfo camInfo)
        {
            var circleSize = Mathf.Lerp(0, 3f, SimulationManager.instance.m_realTimer % 0.8f / 0.8f);
            foreach (var position in renderOverlayCirclePositions)
            {
                var orangeHalf = new Color(1, .5f, 0, (3 - circleSize) * .25f);
                Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(camInfo, orange, position, position.w, -1f, 1280f, false, true);
                Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(camInfo, orangeHalf, position, position.w + circleSize, -1f, 1280f, false, true);
            }
            renderOverlayCirclePositions.Clear();
        }

        public string CurrentStatusRecalculation { get; private set; }

        public void RecalculateProps() => SimulationManager.instance.StartCoroutine(RecalculatePropsAsync());


        private IEnumerator RecalculatePropsAsync()
        {
            yield return 0;
            for (int i = 0; i < RenderManager.instance.m_groups.Length; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }

        }
    }
}