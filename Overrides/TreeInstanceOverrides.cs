using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;
using static RenderManager;

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



            AddRedirect(typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourTreeInstanceObjRender"));
            AddRedirect(typeof(TreeInstance).GetMethod("UpdateTree", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourTreeInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("RayCast", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourTreeInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourTreeInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("OverlapQuad", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourTreeInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourTreeInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourTreeInstanceObjCalc"));
            AddRedirect(typeof(TreeInstance).GetMethod("AfterTerrainUpdated", RedirectorUtils.allFlags), GetType().GetMethod("CheckValidTree"));
        }

        #region Instances
        public static bool CheckValidTree(ref TreeInstance __instance, uint treeID) => GetTargetInfoWithPosition(PrefabCollection<TreeInfo>.GetPrefab(__instance.m_infoIndex), __instance.Position, treeID, false) != null;
        public static IEnumerable<CodeInstruction> DetourTreeInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessDetour(il, instr, "GetTargetInfoWithPosition", false, false);
        public static IEnumerable<CodeInstruction> DetourTreeInstanceObjCalc(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessDetour(il, instr, "GetTargetInfoWithPosition", false, true);
        public static IEnumerable<CodeInstruction> DetourTreeInstanceObjRender(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessDetour(il, instr, "GetTargetInfoWithPosition", true, false);
        private static IEnumerable<CodeInstruction> ProcessDetour(ILGenerator il, IEnumerable<CodeInstruction> instr, string getMethod, bool isRender, bool isCalc)
        {
            var instrList = new List<CodeInstruction>(instr);
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
                        new CodeInstruction(isRender?OpCodes.Ldarg_2:OpCodes.Ldarg_1),
                        new CodeInstruction(isCalc? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
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

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        #endregion
        #region BuildingAI
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => Transpile_BuildingAI_Xxxxxx(instr, il, false, true, new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Ldloc_S, 7));
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => Transpile_BuildingAI_Xxxxxx(instr, il, false, false, new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Ldloc_S, 16));
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_RenderProps(IEnumerable<CodeInstruction> instr, ILGenerator il) => Transpile_BuildingAI_Xxxxxx(instr, il, true, false, new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Ldloc_S, 11));
        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_Xxxxxx(IEnumerable<CodeInstruction> instr, ILGenerator il, bool isRender, bool isCalc, CodeInstruction ldCamInfo, CodeInstruction ldArgBuildingId, CodeInstruction ldLocPropId)
        {
            var instrList = new List<CodeInstruction>(instr);
            var posOffsetVar = il.DeclareLocal(typeof(Vector3));
            for (int i = 3; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(BuildingInfo.Prop).GetField("m_finalTree", RedirectorUtils.allFlags))
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>
                    {
                        ldCamInfo,
                        ldArgBuildingId,
                        ldLocPropId,
                        new CodeInstruction(OpCodes.Ldloca_S,posOffsetVar),
                        new CodeInstruction(isCalc? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") ),

                    });
                    i += 8;
                }
                if (instrList[i - 1].opcode == OpCodes.Ldfld && instrList[i - 1].operand is FieldInfo fi3 && fi3.Name == "m_position" && fi3.DeclaringType == typeof(BuildingInfo.Prop))
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,posOffsetVar),
                        new CodeInstruction(OpCodes.Call, typeof(Vector3).GetMethod("op_Addition")),
                        });
                    i += 3;
                }
                if (isRender)
                {
                    if (instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb && lb.LocalIndex == 11
                    && instrList[i + 1].opcode == OpCodes.Ldc_I4_1
                    && instrList[i + 2].opcode == OpCodes.Add)
                    {
                        var lbl1 = il.DefineLabel();
                        var startCmd = new CodeInstruction(OpCodes.Ldloca_S, 29)
                        {
                            labels = instrList[i].labels
                        };
                        instrList[i].labels = new List<Label> { lbl1 };
                        instrList.InsertRange(i, new CodeInstruction[] {
                        startCmd,
                        new CodeInstruction(OpCodes.Brfalse, lbl1),
                        new CodeInstruction(OpCodes.Ldloc_S, 29),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldloc_S,12),
                        new CodeInstruction(OpCodes.Ldloc_S,11),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("BuildingAI_CheckDrawCircle",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Br, lbl1),
                    });
                        i += 8;
                    }
                }
            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static void BuildingAI_CheckDrawCircle(Vector3 location, BuildingAI buildingAI, BuildingInfo.Prop prop, ushort j) => PSOverrideCommons.Instance.CheckIfShallCircle(buildingAI.m_info.name, prop.m_finalTree, j, location);
        #endregion
        #region NetLane

        public static IEnumerable<CodeInstruction> Transpile_NetLane_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => ApplyTreeRenderSelection(il, instr, true, 1, -1, 14, 5);
        public static IEnumerable<CodeInstruction> Transpile_NetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => ApplyTreeRenderSelection(il, instr, false, 2, 27, 25, 5);
        public static IEnumerable<CodeInstruction> Transpile_NetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il) => ApplyTreeRenderSelection(il, instr, false, 3, 35, 33, 11);

        private static IEnumerable<CodeInstruction> ApplyTreeRenderSelection(ILGenerator il, IEnumerable<CodeInstruction> instr, bool isCalc, int laneIdArgIdx, int selTreeIdx, int kIteratorIdx, int iIteratorIdx)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count - 2; i++)
            {
                if (instrList[i].opcode == OpCodes.Callvirt && instrList[i].operand is MethodInfo mi && mi.DeclaringType == typeof(TreeInfo) && mi.Name == "GetVariation")
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
                            var codeList = new List<CodeInstruction>
                            {
                                 new CodeInstruction(OpCodes.Ldarg_S,laneIdArgIdx),
                                 new CodeInstruction(OpCodes.Ldloc_S,iIteratorIdx),
                                 new CodeInstruction(OpCodes.Ldloc_S,kIteratorIdx),
                                 new CodeInstruction(isCalc? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                                 new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromNetLane", RedirectorUtils.allFlags) ),

                            };
                            if (selTreeIdx < 0)
                            {
                                selTreeIdx = il.DeclareLocal(typeof(TreeInfo)).LocalIndex;
                            }
                            codeList.AddRange(new CodeInstruction[] {
                                    new CodeInstruction(OpCodes.Stloc_S,selTreeIdx),
                                    new CodeInstruction(OpCodes.Ldloc_S,selTreeIdx),
                                    new CodeInstruction(OpCodes.Brfalse,loopLabel),
                                    new CodeInstruction(OpCodes.Ldloc_S, selTreeIdx)
                                });

                            instrList.InsertRange(i - 1, codeList);
                            break;
                        }
                    }
                    break;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        #endregion

        public static TreeInfo GetTargetInfoWithPosition(TreeInfo info, Vector3 position, uint treeId, bool isCalc) => GetTargetInfo_internal(info, isCalc, -1, position, new InstanceID { Tree = treeId });
        public static TreeInfo GetTargetInfoFromNetLane(TreeInfo info, uint laneId, int itemId, int iteration, bool isCalc) => GetTargetInfo_internal(info, isCalc, ((itemId << 8) | (iteration & 0xff)), new Vector3(laneId, itemId, iteration), new InstanceID { NetLane = laneId });
        public static TreeInfo GetTargetInfoFromBuilding(TreeInfo info, CameraInfo camInfo, ushort buildingId, int itemId, out Vector3 positionOffset, bool isCalc) => GetTargetInfo_internal(info, out positionOffset, isCalc, itemId, new Vector3(buildingId << 2, 0, itemId << 2), new InstanceID { Building = buildingId });
        private static TreeInfo GetTargetInfo_internal(TreeInfo info, bool isCalc, int propIdx, Vector3 position = default, InstanceID id = default) => GetTargetInfo_internal(info, out _, isCalc, propIdx, position, id);
        private static TreeInfo GetTargetInfo_internal(TreeInfo info, out Vector3 positionOffset, bool isCalc, int propIdx, Vector3 position = default, InstanceID id = default)
        {
            if (PSOverrideCommons.Instance.GetTargetInfo_internal(info, ref id, ref position, propIdx, isCalc, out Item result))
            {
                if (result != null)
                {
                    positionOffset = result.PositionOffset;
                    return result.CachedTree;
                }
                positionOffset = default;
                return null;
            }
            else
            {
                positionOffset = default;
                return info;
            }
        }

    }
}