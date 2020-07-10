using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Klyte.PropSwitcher.Overrides
{

    public class PropInstanceOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {


            //System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            //LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            //var orMeth = typeof(BuildingManager).GetMethod("EndRenderingImpl", RedirectorUtils.allFlags);
            //AddRedirect(orMeth, null, postRenderMeshs);
            //System.Reflection.MethodInfo afterEndOverlayImpl = typeof(WTSBuildingPropsSingleton).GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);
            var allMethods = typeof(PropInstance).GetMethods(RedirectorUtils.allFlags).Where(x => x.Name == "RenderInstance" && x.GetParameters().Length > 3);
            var objMethod = typeof(PropInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static);
            var propSwitchMethod = GetType().GetMethod("ApplySwitch");
            var propSwitchMethodGlobal = GetType().GetMethod("ApplySwitchGlobal");
            foreach (var method in allMethods)
            {
                AddRedirect(method, propSwitchMethod);
            }
            AddRedirect(typeof(PropInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethodGlobal);
            AddRedirect(typeof(PropInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethod);
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethodGlobal);
            AddRedirect(objMethod, null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("UpdateProp", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));

        }

        public static bool ApplySwitch(ref PropInfo info, ref InstanceID id, ref float angle) => (info = GetTargetInfo(info, ref id, ref angle)) != null;
        public static bool ApplySwitchGlobal(ref PropInfo info) => (info = GetTargetInfoWithoutId(info)) != null;

        public static IEnumerable<CodeInstruction> DetourRenederInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("GetTargetInfoWithoutId",RedirectorUtils.allFlags)),
                        });
                    i += 2;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static PropInfo GetTargetInfoWithoutId(PropInfo info)
        {
            InstanceID id = default;
            float angle = 0;
            return GetTargetInfo_internal(info, ref id, ref angle);
        }

        public static PropInfo GetTargetInfo(PropInfo info, ref InstanceID id, ref float angle) => GetTargetInfo_internal(info, ref id, ref angle);

        private static PropInfo GetTargetInfo_internal(PropInfo info, ref InstanceID id, ref float angle)
        {
            if (info == null || PSPropData.Instance?.Entries == null)
            {
                return info;
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

            SimpleXmlDictionary<string, SwitchInfo> switchInfoDictGlobal = null;
            SwitchInfo switchInfo = null;
            SwitchInfo.Item infoItem = null;
            if (parentName != null && (PSPropData.Instance.PrefabChildEntries.TryGetValue(parentName, out SimpleXmlDictionary<string, SwitchInfo> switchInfoDict) | (PropSwitcherMod.Controller?.GlobalPrefabChildEntries?.TryGetValue(parentName, out switchInfoDictGlobal) ?? false)) && ((switchInfoDict?.TryGetValue(info.name, out switchInfo) ?? false) || (switchInfoDictGlobal?.TryGetValue(info.name, out switchInfo) ?? false)) && switchInfo != null)
            {
                TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem);
                if (infoItem != null)
                {
                    return infoItem.CachedProp;
                }
            }

            if (PSPropData.Instance.Entries.ContainsKey(info.name))
            {
                switchInfo = PSPropData.Instance.Entries[info.name];
                TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem);
                if (infoItem != null)
                {
                    return infoItem.CachedProp;
                }
            }

            return info;
        }

        private static void TryApplyInfo(ref InstanceID id, ref float angle, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem)
        {
            if (switchInfo.SwitchItems.Length > 0)
            {
                if (switchInfo.SwitchItems.Length == 1)
                {
                    infoItem = switchInfo.SwitchItems[0];
                }
                else
                {
                    var r = new Randomizer(id.Index);
                    infoItem = switchInfo.SwitchItems[r.Int32((uint)switchInfo.SwitchItems.Length)];
                }

                angle += infoItem.RotationOffset;
            }
        }
    }
}