using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

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
            ApplyTreeRenderSelection(il, instrList, 1, -1, 14, 5);
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
        }

        #endregion

        public static TreeInfo GetTargetInfoWithoutId(TreeInfo info) => GetTargetInfo_internal(info);
        public static TreeInfo GetTargetInfoWithPosition(TreeInfo info, Vector3 position) => GetTargetInfo_internal(info, position);
        public static TreeInfo GetTargetInfoFromNetLane(TreeInfo info, uint laneId, int itemId, int iteration) => GetTargetInfo_internal(info, new Vector3(laneId, itemId, iteration), new InstanceID { NetLane = laneId }, itemId);
        public static TreeInfo GetTargetInfoFromBuilding(TreeInfo info, ushort buildingId, int itemId) => GetTargetInfo_internal(info, new Vector3(buildingId, itemId), new InstanceID { Building = buildingId }, itemId);
        private static TreeInfo GetTargetInfo_internal(TreeInfo info, Vector3 position = default, InstanceID id = default, int propIdx = -1)
        {
            float angle = 0;
            return PSOverrideCommons.GetTargetInfo_internal(info, ref id, ref angle, ref position, propIdx, out Item result) ? result?.CachedTree : info;
        }
    }
}