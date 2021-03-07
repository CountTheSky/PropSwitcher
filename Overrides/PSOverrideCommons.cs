using ColossalFramework.Math;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

namespace Klyte.PropSwitcher.Overrides
{
    internal class PSOverrideCommons
    {
        public static bool GetTargetInfo_internal(PrefabInfo info, ref InstanceID id, ref float angle, ref Vector3 position, int propIdx, out Item result)
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
                        if (TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem, ref position))
                        {
                            result = infoItem;
                            return true;
                        }
                    }
                    if ((switchInfoDict?.TryGetValue(defaultKey, out switchInfo) ?? false) || (switchInfoDictGlobal?.TryGetValue(defaultKey, out switchInfo) ?? false))
                    {
                        if (TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem, ref position))
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
                if (TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem, ref position))
                {
                    result = infoItem;
                    return true;
                }
            }
            result = null;
            return false;

        }

        private static bool TryApplyInfo(ref InstanceID id, ref float angle, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem, ref Vector3 position)
        {
            if (switchInfo.SwitchItems.Length > 0)
            {
                if (switchInfo.SwitchItems.Length == 1)
                {
                    infoItem = switchInfo.SwitchItems[0];
                }
                else
                {
                    var positionSeed = (Mathf.RoundToInt(position.x) >> 2) * (Mathf.RoundToInt(position.z) >> 2);
                    var seed = switchInfo.SeedSource == SwitchInfo.RandomizerSeedSource.POSITION || id == default || id.Prop != 0 ? positionSeed : (int)id.Index;
                    var r = new Randomizer(seed);
                    var targetIdx = r.Int32((uint)switchInfo.SwitchItems.Length);

                    //   LogUtils.DoWarnLog($"Getting model seed: id = b:{id.Building} ns:{id.NetSegment} nl:{id.NetLane} p:{id.Prop} ({id});pos = {position}; postionSeed: {positionSeed}; targetIdx: {targetIdx}; switchInfo: {switchInfo.GetHashCode().ToString("X16")}; source: {Environment.StackTrace}");
                    //LogUtils.DoWarnLog($"seed =  {id.Index} +{(int)(position.x + position.y + position.z) % 100} = {seed} | targetIdx = {targetIdx} | position = {position}");
                    infoItem = switchInfo.SwitchItems[targetIdx];
                }

                angle += infoItem.RotationOffset * Mathf.Deg2Rad;
                return true;
            }
            return false;
        }
    }
}