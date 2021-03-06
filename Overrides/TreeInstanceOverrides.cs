using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.PropSwitcher.Overrides
{

    public class TreeInstanceOverrides : Redirector, IRedirectable
    {

        private static readonly MethodInfo m_tree_PopulateGroupData = typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~BindingFlags.Instance);
        private static readonly MethodInfo m_tree_RenderInstance = typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~BindingFlags.Instance);

        public void Awake()
        {
            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Public), null, null, GetType().GetMethod("Transpile_BuildingAI_RenderProps"));
            AddRedirect(typeof(BuildingAI).GetMethod("PopulatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_BuildingAI_PopulateGroupData"));
            AddRedirect(typeof(BuildingAI).GetMethod("CalculatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_BuildingAI_CalculateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_RenderInstance"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("CalculateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_CalculateGroupData"));



            AddRedirect(typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("UpdateTree", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("RayCast", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("OverlapQuad", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("AfterTerrainUpdated", RedirectorUtils.allFlags), GetType().GetMethod("CheckValidTree"));
        }

        #region Instances
        public static bool CheckValidTree(ref TreeInstance __instance) => GetTargetInfoWithPosition(PrefabCollection<TreeInfo>.GetPrefab(__instance.m_infoIndex), __instance.Position) != null;
        public static IEnumerable<CodeInstruction> DetourRenederInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            ProcessDetour(il, instrList, "GetTargetInfoWithPosition");
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        private static void ProcessDetour(ILGenerator il, List<CodeInstruction> instrList, string getMethod)
        {
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(TreeInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    var localProp = il.DeclareLocal(typeof(TreeInfo));
                    var labelCont = il.DefineLabel();
                    var lastInstr = new CodeInstruction(OpCodes.Ldloc_S, localProp);
                    lastInstr.labels.Add(labelCont);
                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstance).GetProperty("Position",RedirectorUtils.allFlags).GetGetMethod()),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod(getMethod,RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Stloc_S,localProp),
                        new CodeInstruction(OpCodes.Ldloc_S,localProp),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Brfalse,labelCont),
                        new CodeInstruction(OpCodes.Ret),
                        lastInstr,
                        }); ;
                    i += 2;
                }

            }
        }
        #endregion
        #region BuildingAI
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalTree")
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1 ),
                        new CodeInstruction(OpCodes.Ldloc_S,7),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") ),

                    });
                    i += 8;
                }

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalTree")
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1 ),
                        new CodeInstruction(OpCodes.Ldloc_S,16),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") ),

                    });
                    i += 8;
                }

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
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
                        new CodeInstruction(OpCodes.Ldloc_S,11),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") ),

                    }); ;
                    i += 8;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        #endregion
        #region NetLane

        public static IEnumerable<CodeInstruction> Transpile_NetLane_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            ApplyTreeRenderSelection(il, instrList, 1, 27, 25, 5);
            LogUtils.PrintMethodIL(instrList);
            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_NetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            ApplyTreeRenderSelection(il, instrList, 2, 27, 25, 5);
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> Transpile_NetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            ApplyTreeRenderSelection(il, instrList, 3, 35, 33, 11);
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        private static void ApplyTreeRenderSelection(ILGenerator il, List<CodeInstruction> instrList, int laneIdArgIdx, int selTreeIdx, int kIteratorIdx, int iIteratorIdx)
        {
            for (int i = 0; i < instrList.Count - 2; i++)
            {
                if (instrList[i + 1].opcode == OpCodes.Stloc_S && instrList[i + 1].operand is LocalBuilder builder && builder.LocalIndex == selTreeIdx)
                {
                    for (int j = i; j < instrList.Count - 2; j++)
                    {
                        if (
                            instrList[j].opcode == OpCodes.Ldloc_S && instrList[j].operand is LocalBuilder builder1 && builder1.LocalIndex == kIteratorIdx
                            && instrList[j + 1].opcode == OpCodes.Ldc_I4_2
                            && instrList[j + 2].opcode == OpCodes.Add
                            )
                        {
                            var loopLabel = il.DefineLabel();
                            instrList[j].labels.Add(loopLabel);

                            instrList.RemoveAt(i);
                            instrList.RemoveAt(i);
                            instrList.InsertRange(i, new List<CodeInstruction>
                            {
                                 new CodeInstruction(OpCodes.Ldarg_S,laneIdArgIdx),
                                 new CodeInstruction(OpCodes.Ldloc_S,iIteratorIdx),
                                 new CodeInstruction(OpCodes.Ldloc_S,kIteratorIdx),
                                 new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("NetLane_RenderInstance", RedirectorUtils.allFlags) ),
                                 new CodeInstruction(OpCodes.Stloc_S,selTreeIdx),
                                 new CodeInstruction(OpCodes.Ldloc_S,selTreeIdx),
                                 new CodeInstruction(OpCodes.Brfalse,loopLabel),
                            });
                            break;
                        }
                    }
                    break;
                }

            }
        }


        public static TreeInfo NetLane_RenderInstance(TreeInfo treeInfo, ref Randomizer randomizer2, ushort laneId, int i, int k)
        {
            var finalTree = GetTargetInfoFromNetLane(treeInfo, laneId, i, k);
            return finalTree?.GetVariation(ref randomizer2);
        }
        #endregion

        public static TreeInfo GetTargetInfoWithoutId(TreeInfo info) => GetTargetInfo_internal(info);
        public static TreeInfo GetTargetInfoWithPosition(TreeInfo info, Vector3 position) => GetTargetInfo_internal(info, position);
        public static TreeInfo GetTargetInfoFromNetLane(TreeInfo info, ushort laneId, int itemId, int iteration) => GetTargetInfo_internal(info, new Vector3(laneId, itemId, iteration), new InstanceID { NetLane = laneId });
        
        public static TreeInfo GetTargetInfoFromBuilding(TreeInfo info, ushort buildingId, int itemId) => GetTargetInfo_internal(info, new Vector3(buildingId, itemId), new InstanceID { Building = buildingId });
        private static TreeInfo GetTargetInfo_internal(TreeInfo info, Vector3 position = default, InstanceID id = default)
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
                TryApplyInfo(ref id, switchInfo, ref infoItem, position);
                if (infoItem != null)
                {
                    return infoItem.CachedTree;
                }
            }

            if (PSPropData.Instance.Entries.ContainsKey(info.name))
            {
                switchInfo = PSPropData.Instance.Entries[info.name];
                TryApplyInfo(ref id, switchInfo, ref infoItem, position);
                if (infoItem != null)
                {
                    return infoItem.CachedTree;
                }
            }

            return info;
        }

        private static void TryApplyInfo(ref InstanceID id, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem, Vector3 position)
        {
            if (switchInfo.SwitchItems.Length > 0)
            {
                if (switchInfo.SwitchItems.Length == 1)
                {
                    infoItem = switchInfo.SwitchItems[0];
                }
                else
                {
                    var seed = switchInfo.SeedSource == SwitchInfo.RandomizerSeedSource.POSITION || id == default ? (int)(position.x + position.y + position.z) % 1000 : (int)id.Index;
                    var r = new Randomizer(seed);
                    var targetIdx = r.Int32((uint)switchInfo.SwitchItems.Length);
                    infoItem = switchInfo.SwitchItems[targetIdx];

                }
            }
        }

    }
}