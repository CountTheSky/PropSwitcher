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
    public class PropInstanceOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {
            AddRedirect(typeof(PropInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), GetType().GetMethod("ApplySwitchGlobal"));
            AddRedirect(typeof(PropInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("UpdateProp", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourPropInstanceObjMethodsCalc"));

            AddRedirect(typeof(BuildingAI).GetMethod("CalculatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_CalculatePropGroupData"));//7
            AddRedirect(typeof(BuildingAI).GetMethod("PopulatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_PopulatePropGroupData"));//16
            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~BindingFlags.Public), null, null, GetType().GetMethod("TranspileBuildingAI_RenderProps"));
            AddRedirect(typeof(BuildingManager).GetMethod("EndOverlay"), null, GetType().GetMethod("AfterEndRenderOverlayBuilding"));
            AddRedirect(typeof(NetLane).GetMethod("CalculateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_CalculateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_RenderInstance"));
        }

        public static void AfterEndRenderOverlayBuilding(CameraInfo cameraInfo) => PSOverrideCommons.Instance.OnRenderOverlay(cameraInfo);

        #region BuildingAI
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_CalculatePropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => TranspileBuildingAI_XxxxPropGroupData(7, instr, il, true);
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_PopulatePropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => TranspileBuildingAI_XxxxPropGroupData(16, instr, il, false);
        private static IEnumerable<CodeInstruction> TranspileBuildingAI_XxxxPropGroupData(int jVarIdx, IEnumerable<CodeInstruction> instr, ILGenerator il, bool isCalc)
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
                        new CodeInstruction(isCalc?OpCodes.Ldc_I4_1:OpCodes.Ldc_I4_0),
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
                        new CodeInstruction(OpCodes.Ldc_I4_0),
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

        public static void BuildingAI_CheckDrawCircle(Vector3 location, BuildingAI buildingAI, BuildingInfo.Prop prop, ushort j) => PSOverrideCommons.Instance.CheckIfShallCircle(buildingAI.m_info.name, prop.m_finalProp, j, location);

        public static PropInfo BuildingAI_CalculateTargetProp(BuildingInfo.Prop prop, BuildingAI buildingAI, ushort buildingID, out float angle, out Vector3 positionOffset, ushort j, bool isCalc)
        {
            angle = 0;
            positionOffset = default;
            var finalProp = prop.m_finalProp;
            if (finalProp == null)
            {
                return null;
            }
            Vector3 vector2 = new Vector3(buildingID << 2, 0, j << 2);
            var id = new InstanceID { Building = buildingID };
            var result = GetTargetInfo(finalProp, ref id, ref positionOffset, ref angle, ref vector2, isCalc, j);
            return result;
        }


        #endregion

        #region NetLane
        public static IEnumerable<CodeInstruction> TranspileNetLane_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessInstructions_NetLane(il, instr, true, 1, 5, 10, "CLC");
        public static IEnumerable<CodeInstruction> TranspileNetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessInstructions_NetLane(il, instr, false, 2, 5, 12, "POP");
        public static IEnumerable<CodeInstruction> TranspileNetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il) => ProcessInstructions_NetLane(il, instr, false, 3, 11, 19, "RND");

        private static IEnumerable<CodeInstruction> ProcessInstructions_NetLane(ILGenerator il, IEnumerable<CodeInstruction> instr, bool isCalc, int argIdxLaneId, int propIdIdx, int jIdx, string source)
        {
            var instrList = new List<CodeInstruction>(instr);
            var angleOffsetVar = il.DeclareLocal(typeof(float));
            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_S,argIdxLaneId),
                        new CodeInstruction(OpCodes.Ldloca_S,angleOffsetVar),
                        new CodeInstruction(OpCodes.Ldloc_S, jIdx),
                        new CodeInstruction(OpCodes.Ldloc_S, propIdIdx),
                        new CodeInstruction(isCalc?OpCodes.Ldc_I4_1:OpCodes.Ldc_I4_0),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("NetLane_CalculateTargetProp",RedirectorUtils.allFlags)),
                        });
                    i += 6;
                }
                if (instrList[i - 1].opcode == OpCodes.Ldfld && instrList[i - 1].operand is FieldInfo fi2 && fi2.Name == "m_angle" && fi2.DeclaringType == typeof(NetLaneProps.Prop))
                {
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,angleOffsetVar),
                        new CodeInstruction(OpCodes.Add),
                        });
                    i += 3;
                }

            }
            LogUtils.PrintMethodIL(instrList);
            return instrList;
        }

        public static PropInfo NetLane_CalculateTargetProp(PropInfo finalInfo, uint laneId, out float angle, int j, int propIdx, bool isCalc)
        {
            angle = 0;
            Vector3 offsetPosition = default;
            var vector3 = new Vector3(laneId, propIdx, j);

            var id = new InstanceID { NetLane = laneId };
            var propIdxTgt = ((propIdx << 8) | (j & 0xff));
            return GetTargetInfo(finalInfo, ref id, ref offsetPosition, ref angle, ref vector3, isCalc, propIdxTgt);
        }
        #endregion

        public static bool ApplySwitchGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle) => (info = GetTargetInfoGlobal(ref info, ref position, propID, ref angle, false)) != null;


        public static PropInfo GetTargetInfoGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle, bool isCalc)
        {
            var id = new InstanceID { Prop = propID };
            var offsetToAdd = default(Vector3);
            return GetTargetInfo(info, ref id, ref offsetToAdd, ref angle, ref position, isCalc, -1);
        }

        public static IEnumerable<CodeInstruction> DetourPropInstanceObjMethods(IEnumerable<CodeInstruction> instr, ILGenerator il) => DetourPropInstanceObjMethods_exec(instr, il, false);
        public static IEnumerable<CodeInstruction> DetourPropInstanceObjMethodsCalc(IEnumerable<CodeInstruction> instr, ILGenerator il) => DetourPropInstanceObjMethods_exec(instr, il, true);

        public static IEnumerable<CodeInstruction> DetourPropInstanceObjMethods_exec(IEnumerable<CodeInstruction> instr, ILGenerator il, bool isCalc)
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
                        new CodeInstruction(isCalc?OpCodes.Ldc_I4_1:OpCodes.Ldc_I4_0),
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
                        new CodeInstruction(OpCodes.Ldc_I4_0),
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

        public static PropInfo GetTargetInfo(PropInfo info, ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, ref Vector3 position, bool isCalc, int propIdx) => GetTargetInfo_internal(info, ref id, ref offsetToAdd, ref angle, ref position, propIdx, isCalc);

        private static PropInfo GetTargetInfo_internal(PropInfo info, ref InstanceID id, ref Vector3 offsetToAdd, ref float angle, ref Vector3 position, int propIdx, bool isCalc)
        {
            if (PSOverrideCommons.Instance.GetTargetInfo_internal(info, ref id, ref position, propIdx, isCalc, out Item result))
            {
                if (result != null)
                {
                    offsetToAdd += result.PositionOffset;
                    angle += result.RotationOffset;
                    return result.CachedProp;
                }
                return null;
            }
            else
            {
                return info;
            }
        }
    }
}