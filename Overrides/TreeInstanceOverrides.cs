using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Klyte.PropSwitcher.Overrides
{

    public class TreeInstanceOverrides : Redirector, IRedirectable
    {

        private static readonly MethodInfo m_tree_PopulateGroupData = typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~BindingFlags.Instance);
        private static readonly MethodInfo m_tree_RenderInstance = typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~BindingFlags.Instance);

        public void Awake()
        {


            //System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            //LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            //var orMeth = typeof(BuildingManager).GetMethod("EndRenderingImpl", RedirectorUtils.allFlags);
            //AddRedirect(orMeth, null, postRenderMeshs);
            //System.Reflection.MethodInfo afterEndOverlayImpl = typeof(WTSBuildingPropsSingleton).GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);



            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Public), null, null, GetType().GetMethod("Transpile_BuildingAI_RenderProps"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_RenderInstance"));
            AddRedirect(typeof(BuildingAI).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_BuildingAI_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_PopulateGroupData"));


            AddRedirect(typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("UpdateTree", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("RayCast", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("OverlapQuad", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));

        }

        public static bool ApplySwitchGlobal(ref TreeInfo info) => (info = GetTargetInfoWithoutId(info)) != null;
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_RenderProps(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(BuildingInfo.Prop).GetField("m_finalTree", RedirectorUtils.allFlags))
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_2 ),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") )
                    });
                    i += 4;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_NetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(NetLaneProps.Prop).GetField("m_finalTree", RedirectorUtils.allFlags))
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_2 ),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromNetSegment") )
                    });
                    i += 4;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(BuildingInfo.Prop).GetField("m_finalTree", RedirectorUtils.allFlags))
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1 ),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") )
                    });
                    i += 4;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_NetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(NetLaneProps.Prop).GetField("m_finalTree", RedirectorUtils.allFlags))
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1 ),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromNetSegment") )
                    });
                    i += 4;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static IEnumerable<CodeInstruction> DetourRenederInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(TreeInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction( OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoWithoutId",RedirectorUtils.allFlags)),
                        });
                    i += 2;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static TreeInfo GetTargetInfoWithoutId(TreeInfo info) => GetTargetInfo_internal(info);
        public static TreeInfo GetTargetInfoFromNetSegment(TreeInfo info, ushort segmentId) => GetTargetInfo_internal(info, new InstanceID { NetSegment = segmentId });
        public static TreeInfo GetTargetInfoFromBuilding(TreeInfo info, ushort buildingId) => GetTargetInfo_internal(info, new InstanceID { Building = buildingId });
        private static TreeInfo GetTargetInfo_internal(TreeInfo info, InstanceID id = default)
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
            if (id.NetNode != 0)
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
                TryApplyInfo(ref id, switchInfo, ref infoItem);
                if (infoItem != null)
                {
                    return infoItem.CachedTree;
                }
            }

            if (PSPropData.Instance.Entries.ContainsKey(info.name))
            {
                switchInfo = PSPropData.Instance.Entries[info.name];
                TryApplyInfo(ref id, switchInfo, ref infoItem);
                if (infoItem != null)
                {
                    return infoItem.CachedTree;
                }
            }

            return info;
        }

        private static void TryApplyInfo(ref InstanceID id, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem)
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
            }
        }

    }
}