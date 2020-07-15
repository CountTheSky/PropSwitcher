using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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


            AddRedirect(typeof(BuildingAI).GetMethod("CalculatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_XxxxPropGroupData"));
            AddRedirect(typeof(BuildingAI).GetMethod("PopulatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_XxxxPropGroupData"));
            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~BindingFlags.Public), null, null, GetType().GetMethod("TranspileBuildingAI_RenderProps"));
            AddRedirect(typeof(NetLane).GetMethod("CalculateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_CalculateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_RenderInstance"));
        }



        #region BuildingAI
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_XxxxPropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            var angleVar = il.DeclareLocal(typeof(float));

            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.RemoveAt(i);
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloca_S,angleVar),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CalculatePropGroupData",RedirectorUtils.allFlags)),
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

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_RenderProps(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            var angleVar = il.DeclareLocal(typeof(float));
            for (int i = 2; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.RemoveAt(i);
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldloca_S,angleVar),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CalculatePropGroupData",RedirectorUtils.allFlags)),
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
            }


            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static PropInfo BuildingAI_CalculatePropGroupData(BuildingInfo.Prop prop, BuildingAI buildingAI, ushort buildingID, out float angle)
        {
            angle = 0;
            var finalProp = prop.m_finalProp;
            if (finalProp == null)
            {
                return null;
            }

            ref Building buildingData = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
            Matrix4x4 matrix4x2 = default;
            matrix4x2.SetTRS(Building.CalculateMeshPosition(buildingAI.m_info, buildingData.m_position, buildingData.m_angle, buildingData.Length), Quaternion.AngleAxis(buildingData.m_angle * 57.29578f, Vector3.down), Vector3.one);
            Vector3 vector2 = matrix4x2.MultiplyPoint(prop.m_position);

            var id = new InstanceID { Building = buildingID };
            var result = GetTargetInfo(finalProp, ref id, ref angle, ref vector2);

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
            if (finalInfo == null)
            {
                return null;
            }

            ref NetLane thiz = ref NetManager.instance.m_lanes.m_buffer[laneId];

            var id = new InstanceID { NetSegment = thiz.m_segment };
            var vector3 = thiz.m_bezier.Position(1f / j);
            vector3.x += j;
            var result = GetTargetInfo(finalInfo, ref id, ref angleOffset, ref vector3);
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
            return GetTargetInfo(info, ref id, ref angle, ref position);
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
            return GetTargetInfo_internal(info, ref id, ref angle, ref vector3);
        }

        public static PropInfo GetTargetInfo(PropInfo info, ref InstanceID id, ref float angle, ref Vector3 position) => GetTargetInfo_internal(info, ref id, ref angle, ref position);

        private static PropInfo GetTargetInfo_internal(PropInfo info, ref InstanceID id, ref float angle, ref Vector3 position)
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
                TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem, ref position);
                if (infoItem != null)
                {
                    return infoItem.CachedProp;
                }
            }

            if (PSPropData.Instance.Entries.ContainsKey(info.name))
            {
                switchInfo = PSPropData.Instance.Entries[info.name];
                TryApplyInfo(ref id, ref angle, switchInfo, ref infoItem, ref position);
                if (infoItem != null)
                {
                    return infoItem.CachedProp;
                }
            }

            return info;
        }

        private static void TryApplyInfo(ref InstanceID id, ref float angle, SwitchInfo switchInfo, ref SwitchInfo.Item infoItem, ref Vector3 position)
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
            }
        }
    }
}