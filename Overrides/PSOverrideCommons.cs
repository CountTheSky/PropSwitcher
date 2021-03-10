using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.UI;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;
using static RenderManager;

namespace Klyte.PropSwitcher.Overrides
{
    internal class PSOverrideCommons
    {
        private static readonly HashSet<Vector4> renderOverlayCirclePositions = new HashSet<Vector4>();

        public static bool GetTargetInfo_internal(PrefabInfo info, ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, ref Vector3 randomizerParameters, int propIdx, out Item result)
        {
            if (info == null || PSPropData.Instance?.Entries == null)
            {
                result = null;
                return false;
            }
            string parentName = null;
            if (id.NetSegment != 0)
            {
                parentName = NetManager.instance.m_segments.m_buffer[id.NetSegment].Info.name;

            }
            else if (id.NetNode != 0)
            {
                parentName = NetManager.instance.m_nodes.m_buffer[id.NetNode].Info.name;

            }
            else if (id.NetLane != 0)
            {
                parentName = NetManager.instance.m_segments.m_buffer[NetManager.instance.m_lanes.m_buffer[id.NetLane].m_segment].Info.name;

            }
            else if (id.Building != 0)
            {
                parentName = BuildingManager.instance.m_buildings.m_buffer[id.Building].Info.name;
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
                        if (TryApplyInfo(ref id, ref offsetToAdd, ref angle, switchInfo, ref infoItem, ref randomizerParameters))
                        {
                            result = infoItem;
                            return true;
                        }
                    }
                    if ((switchInfoDict?.TryGetValue(defaultKey, out switchInfo) ?? false) || (switchInfoDictGlobal?.TryGetValue(defaultKey, out switchInfo) ?? false))
                    {
                        if (TryApplyInfo(ref id, ref offsetToAdd, ref angle, switchInfo, ref infoItem, ref randomizerParameters))
                        {
                            result = infoItem;
                            return true;
                        }
                    }
                }
            }
            if (PSPropData.Instance.Entries.ContainsKey(defaultKey))
            {
                switchInfo = PSPropData.Instance.Entries[defaultKey];
                if (TryApplyInfo(ref id, ref offsetToAdd, ref angle, switchInfo, ref infoItem, ref randomizerParameters))
                {
                    result = infoItem;
                    return true;
                }
            }
            result = null;
            return false;

        }

        private static bool TryApplyInfo(ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem, ref Vector3 randomizerParameters)
        {
            if (switchInfo.SwitchItems.Length > 0)
            {
                if (switchInfo.SwitchItems.Length == 1)
                {
                    infoItem = switchInfo.SwitchItems[0];
                }
                else
                {
                    var positionSeed = (Mathf.RoundToInt(randomizerParameters.x) | 1) * (Mathf.RoundToInt(randomizerParameters.z) | 1) * (Mathf.RoundToInt(randomizerParameters.y) | 1);
                    var seed = switchInfo.SeedSource == SwitchInfo.RandomizerSeedSource.POSITION || id == default || id.Prop != 0 ? positionSeed : (int)id.Index;
                    var r = new Randomizer(seed);
                    var targetIdx = r.Int32((uint)switchInfo.SwitchItems.Length);

                    //   LogUtils.DoWarnLog($"Getting model seed: id = b:{id.Building} ns:{id.NetSegment} nl:{id.NetLane} p:{id.Prop} ({id});pos = {position}; postionSeed: {positionSeed}; targetIdx: {targetIdx}; switchInfo: {switchInfo.GetHashCode().ToString("X16")}; source: {Environment.StackTrace}");
                    //LogUtils.DoWarnLog($"seed =  {id.Index} +{(int)(position.x + position.y + position.z) % 100} = {seed} | targetIdx = {targetIdx} | position = {position}");
                    infoItem = switchInfo.SwitchItems[targetIdx];
                }

                angle += infoItem.RotationOffset * Mathf.Deg2Rad;
                offsetToAdd += infoItem.PositionOffset;
                return true;
            }
            return false;
        }

        public static void CheckIfShallCircle(string parentName, PrefabInfo info, int propIdx, Vector3 position)
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

        public static readonly Color orange = new Color(1, .5f, 0, 1);

        public static void OnRenderOverlay(CameraInfo camInfo)
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
    }
}