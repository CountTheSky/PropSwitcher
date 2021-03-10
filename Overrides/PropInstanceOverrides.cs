using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Klyte.PropSwitcher.Xml.SwitchInfo;
using static RenderManager;

namespace Klyte.PropSwitcher.Overrides
{

    public class PropInstanceOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {
            AddRedirect(typeof(PropInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), GetType().GetMethod("ApplySwitchGlobal"));
            AddRedirect(typeof(PropInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("UpdateProp", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));

            AddRedirect(typeof(BuildingAI).GetMethod("CalculatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_CalculatePropGroupData"));//7
            AddRedirect(typeof(BuildingAI).GetMethod("PopulatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_PopulatePropGroupData"));//16
            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~BindingFlags.Public), null, null, GetType().GetMethod("TranspileBuildingAI_RenderProps"));
            AddRedirect(typeof(BuildingManager).GetMethod("EndOverlay"), null, GetType().GetMethod("AfterEndRenderOverlayBuilding"));
            AddRedirect(typeof(NetLane).GetMethod("CalculateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_CalculateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_RenderInstance"));
        }

        public static void AfterEndRenderOverlayBuilding(CameraInfo cameraInfo) => PSOverrideCommons.OnRenderOverlay(cameraInfo);

        #region BuildingAI
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_CalculatePropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => TranspileBuildingAI_XxxxPropGroupData(7, instr, il);
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_PopulatePropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => TranspileBuildingAI_XxxxPropGroupData(16, instr, il);
        private static IEnumerable<CodeInstruction> TranspileBuildingAI_XxxxPropGroupData(int jVarIdx, IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            var angleVar = il.DeclareLocal(typeof(float));
            var posVar = il.DeclareLocal(typeof(Vector3));

            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.RemoveAt(i);
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloca_S,angleVar),
                        new CodeInstruction(OpCodes.Ldloca_S,posVar),
                        new CodeInstruction(OpCodes.Ldloc_S, jVarIdx),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CalculateTargetProp",RedirectorUtils.allFlags)),
                        });
                    i += 6;
                }
                if (instrList[i - 2].opcode == OpCodes.Ldfld && instrList[i - 2].operand is FieldInfo fi2 && fi2.Name == "m_radAngle"
                    && instrList[i - 1].opcode == OpCodes.Add)
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,angleVar),
                        new CodeInstruction(OpCodes.Add),
                        });
                    i += 3;
                }
                if (instrList[i - 1].opcode == OpCodes.Ldfld && instrList[i - 1].operand is FieldInfo fi3 && fi3.Name == "m_position" && fi3.DeclaringType == typeof(BuildingInfo.Prop))
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,posVar),
                        new CodeInstruction(OpCodes.Call, typeof(Vector3).GetMethod("op_Addition")),
                        });
                    i += 3;
                }

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_RenderProps(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            var angleVar = il.DeclareLocal(typeof(float));
            var posOffsetVar = il.DeclareLocal(typeof(Vector3));
            for (int i = 2; i < instrList.Count - 2; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.RemoveAt(i);
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldloca_S,angleVar),
                        new CodeInstruction(OpCodes.Ldloca_S,posOffsetVar),
                        new CodeInstruction(OpCodes.Ldloc_S,11),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CalculateTargetProp",RedirectorUtils.allFlags)),
                        });
                    i += 6;
                }
                if (instrList[i - 2].opcode == OpCodes.Ldfld && instrList[i - 2].operand is FieldInfo fi2 && fi2.Name == "m_radAngle"
                  && instrList[i - 1].opcode == OpCodes.Add)
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,angleVar),
                        new CodeInstruction(OpCodes.Add),
                        });
                    i += 3;
                }
                if (instrList[i - 1].opcode == OpCodes.Ldfld && instrList[i - 1].operand is FieldInfo fi3 && fi3.Name == "m_position" && fi3.DeclaringType == typeof(BuildingInfo.Prop))
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,posOffsetVar),
                        new CodeInstruction(OpCodes.Call, typeof(Vector3).GetMethod("op_Addition")),
                        });
                    i += 3;
                }
                if (instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb && lb.LocalIndex == 11
                    && instrList[i + 1].opcode == OpCodes.Ldc_I4_1
                    && instrList[i + 2].opcode == OpCodes.Add)
                {
                    var lbl1 = il.DefineLabel();
                    var startCmd = new CodeInstruction(OpCodes.Ldloca_S, 18)
                    {
                        labels = instrList[i].labels
                    };
                    instrList[i].labels = new List<Label> { lbl1 };
                    instrList.InsertRange(i, new CodeInstruction[] {
                        startCmd,
                        new CodeInstruction(OpCodes.Brfalse, lbl1),
                        new CodeInstruction(OpCodes.Ldloc_S, 18),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldloc_S,12),
                        new CodeInstruction(OpCodes.Ldloc_S,11),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CheckDrawCircle",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Br, lbl1),
                    });
                    i += 8;
                }
            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static void BuildingAI_CheckDrawCircle(Vector3 location, BuildingAI buildingAI, BuildingInfo.Prop prop, ushort j) => PSOverrideCommons.CheckIfShallCircle(buildingAI.m_info.name, prop.m_finalProp, j, location);

        public static PropInfo BuildingAI_CalculateTargetProp(BuildingInfo.Prop prop, BuildingAI buildingAI, ushort buildingID, out float angle, out Vector3 positionOffset, ushort j)
        {
            angle = 0;
            positionOffset = default;
            var finalProp = prop.m_finalProp;
            if (finalProp == null)
            {
                return null;
            }
            Vector3 vector2 = new Vector3(buildingID, 0, j);
            var id = new InstanceID { Building = buildingID };
            var result = GetTargetInfo(finalProp, ref id, ref positionOffset, ref angle, ref vector2, j);
            return result;
        }


        #endregion

        #region NetLane
        public static IEnumerable<CodeInstruction> TranspileNetLane_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            ProcessInstructions_NetLane(il, instrList, true, 1, 8, 11, 10, 4, 12, 7);//, "Calculate");

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> TranspileNetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            ProcessInstructions_NetLane(il, instrList, true, 2, 9, 14, 12, 6, 20, 11);//, "Populate");

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> TranspileNetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            ProcessInstructions_NetLane(il, instrList, false, 3, 16, 21, 19, 12);//, "Render");

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        private static void ProcessInstructions_NetLane(ILGenerator il, List<CodeInstruction> instrList, bool relocateLayerCheck, byte laneIdArgIdx, int finalPropIdx, int variationIdx, int jIdx, byte propDataIdx, byte hasPropsIdx = 0, byte layerArgIdx = 0)//, string sourceStr)
        {
            var angleOffsetVar = il.DeclareLocal(typeof(float));
            for (int i = 1; i < instrList.Count - 3; i++)
            {
                if (relocateLayerCheck && instrList[i - 1].opcode == OpCodes.Ldarg_S && instrList[i - 1].operand is byte b && b == hasPropsIdx)
                {
                    instrList[i].opcode = OpCodes.Ldc_I4_0;
                }

                if (relocateLayerCheck && instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb1 && lb1.LocalIndex == finalPropIdx
                    && instrList[i + 1].opcode == OpCodes.Ldfld && instrList[i + 1].operand is FieldInfo fi1 && fi1.Name == "m_prefabDataLayer"
                    && instrList[i + 2].opcode == OpCodes.Ldarg_S && instrList[i + 2].operand is byte b1 && b1 == layerArgIdx
                    && instrList[i + 3].opcode == OpCodes.Beq)
                {
                    instrList.RemoveRange(i, 8);
                }

                if (instrList[i + 1].opcode == OpCodes.Stloc_S && instrList[i + 1].operand is LocalBuilder fi && fi.LocalIndex == variationIdx)
                {
                    var localProp = il.DeclareLocal(typeof(PropInfo));
                    var localHasProp = il.DeclareLocal(typeof(bool));
                    var endloopLabel = il.DefineLabel();
                    var lastInsertLabel = il.DefineLabel();
                    var lastInsert = new CodeInstruction(OpCodes.Ldloc_S, localProp);
                    lastInsert.labels.Add(lastInsertLabel);

                    instrList.InsertRange(i - 1,
                    new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,jIdx),
                        new CodeInstruction(OpCodes.Ldarg_S,laneIdArgIdx),
                  //    new CodeInstruction(OpCodes.Ldarg_S,layerArgIdx),
                    //  new CodeInstruction(OpCodes.Ldstr,sourceStr),
                        new CodeInstruction(OpCodes.Ldloca_S,angleOffsetVar),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("NetLane_CalculateGroupData",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Stloc_S,localProp)
                    }
                    .Union(relocateLayerCheck ?
                            new List<CodeInstruction>{
                            new CodeInstruction(OpCodes.Ldloc_S,localProp),
                            new CodeInstruction(OpCodes.Ldnull),
                            new CodeInstruction(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                            new CodeInstruction(OpCodes.Stloc_S,localHasProp),
                            new CodeInstruction(OpCodes.Ldarg_S,hasPropsIdx),
                            new CodeInstruction(OpCodes.Ldarg_S,hasPropsIdx),
                            new CodeInstruction(OpCodes.Ldind_U1),
                            new CodeInstruction(OpCodes.Ldloc_S,localHasProp),
                            new CodeInstruction(OpCodes.Or),
                            new CodeInstruction(OpCodes.Stind_I1),
                            new CodeInstruction(OpCodes.Ldloc_S,localHasProp),
                            new CodeInstruction(OpCodes.Brtrue,endloopLabel ),
                            new CodeInstruction(OpCodes.Ldloc_S,localProp),
                            new CodeInstruction(OpCodes.Ldfld,typeof(PrefabInfo).GetField("m_prefabDataLayer",RedirectorUtils.allFlags)),
                            new CodeInstruction(OpCodes.Ldarg_S,layerArgIdx),
                            new CodeInstruction(OpCodes.Beq,lastInsertLabel),
                            new CodeInstruction(OpCodes.Ldloc_S,localProp),
                            new CodeInstruction(OpCodes.Ldfld,typeof(PropInfo).GetField("m_effectLayer",RedirectorUtils.allFlags)),
                            new CodeInstruction(OpCodes.Ldarg_S,layerArgIdx),
                            new CodeInstruction(OpCodes.Bne_Un,endloopLabel),
                            lastInsert
                        } : new List<CodeInstruction>() {
                            new CodeInstruction(OpCodes.Ldloc_S,localProp),
                            new CodeInstruction(OpCodes.Ldnull),
                            new CodeInstruction(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                            new CodeInstruction(OpCodes.Brtrue,endloopLabel),
                            lastInsert }));
                    while (i < instrList.Count)
                    {
                        if (instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb && lb.LocalIndex == jIdx)
                        {
                            instrList[i].labels.Add(endloopLabel);
                            break;
                        }
                        i++;
                    }
                    break;
                }
            }
            for (int i = 0; i < instrList.Count - 3; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb1 && lb1.LocalIndex == propDataIdx
                    && instrList[i + 1].opcode == OpCodes.Ldfld && instrList[i + 1].operand is FieldInfo fi1 && fi1.Name == "m_angle")
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,angleOffsetVar),
                        new CodeInstruction(OpCodes.Add),
                        });
                    i += 6;
                }
            }
        }

        public static PropInfo NetLane_CalculateGroupData(PropInfo finalInfo, int j, uint laneId, out float angleOffset)//, int layer, string source)
        {
            angleOffset = 0;
            Vector3 offsetToAdd = default;
            if (finalInfo == null)
            {
                return null;
            }

            ref NetLane thiz = ref NetManager.instance.m_lanes.m_buffer[laneId];

            var id = new InstanceID { NetSegment = thiz.m_segment };
            var vector3 = thiz.m_bezier.Position(1f / j);
            vector3.x += j;
            var result = GetTargetInfo(finalInfo, ref id, ref offsetToAdd, ref angleOffset, ref vector3);
            //if (result != finalInfo)
            //{
            //    LogUtils.DoWarnLog($"s={thiz.m_segment}; l= {laneId}; j = {j}; vector = {vector}; src = {source}; layer = {layer};  finalInfo = {finalInfo} == prop = {result}; layers = {result.m_prefabDataLayer} | {result.m_effectLayer}");
            //}
            return result;
        }

        #endregion

        public static bool ApplySwitchGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle) => (info = GetTargetInfoGlobal(ref info, ref position, propID, ref angle)) != null;


        public static PropInfo GetTargetInfoGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle)
        {
            var id = new InstanceID { Prop = propID };
            var offsetToAdd = default(Vector3);
            return GetTargetInfo(info, ref id, ref offsetToAdd, ref angle, ref position);
        }

        public static IEnumerable<CodeInstruction> DetourPropInstanceObjMethods(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            var localAngle = il.DeclareLocal(typeof(float));
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand is MethodInfo mi && mi.Name == "get_Angle")
                {
                    instrList.RemoveAt(i);
                    instrList[i - 1].opcode = OpCodes.Ldloc_S;
                    instrList[i - 1].operand = localAngle;
                }
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    var localInfo = il.DeclareLocal(typeof(PropInfo));
                    var localPosition = il.DeclareLocal(typeof(Vector3));
                    var lblPass = il.DefineLabel();
                    var lastInstr = new CodeInstruction(OpCodes.Ldloc_S, localInfo);
                    lastInstr.labels.Add(lblPass);


                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Stloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldloca_S, localInfo),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstance).GetProperty("Position").GetGetMethod()),
                        new CodeInstruction(OpCodes.Stloc_S, localPosition),
                        new CodeInstruction(OpCodes.Ldloca_S, localPosition),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstance).GetProperty("Angle").GetGetMethod()),
                        new CodeInstruction(OpCodes.Stloc_S, localAngle),
                        new CodeInstruction(OpCodes.Ldloca_S, localAngle),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("GetTargetInfoGlobal",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Stloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Brfalse_S, lblPass),
                        new CodeInstruction(OpCodes.Ret),
                        lastInstr,
                        });
                    i += 14;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> DetourRenederInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            var localAngle = il.DeclareLocal(typeof(float));
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand is MethodInfo mi && mi.Name == "get_Angle")
                {
                    instrList.RemoveAt(i);
                    instrList[i - 1].opcode = OpCodes.Ldloc_S;
                    instrList[i - 1].operand = localAngle;
                }
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    var localInfo = il.DeclareLocal(typeof(PropInfo));
                    var localPosition = il.DeclareLocal(typeof(Vector3));
                    var lblPass = il.DefineLabel();
                    var lastInstr = new CodeInstruction(OpCodes.Ldloc_S, localInfo);
                    lastInstr.labels.Add(lblPass);


                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Stloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldloca_S, localInfo),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstance).GetProperty("Position").GetGetMethod()),
                        new CodeInstruction(OpCodes.Stloc_S, localPosition),
                        new CodeInstruction(OpCodes.Ldloca_S, localPosition),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstance).GetProperty("Angle").GetGetMethod()),
                        new CodeInstruction(OpCodes.Stloc_S, localAngle),
                        new CodeInstruction(OpCodes.Ldloca_S, localAngle),
                        new CodeInstruction(OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("GetTargetInfoGlobal",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Stloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldloc_S, localInfo),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Brfalse_S, lblPass),
                        new CodeInstruction(OpCodes.Ret),
                        lastInstr,
                        });
                    i += 10;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static PropInfo GetTargetInfoWithoutId(PropInfo info, Vector3 vector3)
        {
            InstanceID id = default;
            float angle = 0;
            Vector3 offsetToAdd = default;
            return GetTargetInfo_internal(info, ref id, ref offsetToAdd, ref angle, ref vector3, -1);
        }

        public static PropInfo GetTargetInfo(PropInfo info, ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, ref Vector3 position, int propIdx = -1) => GetTargetInfo_internal(info, ref id, ref offsetToAdd, ref angle, ref position, propIdx);

        private static PropInfo GetTargetInfo_internal(PropInfo info, ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, ref Vector3 position, int propIdx) => PSOverrideCommons.GetTargetInfo_internal(info, ref id, ref offsetToAdd, ref angle, ref position, propIdx, out Item result) ? result?.CachedProp : info;
    }
}