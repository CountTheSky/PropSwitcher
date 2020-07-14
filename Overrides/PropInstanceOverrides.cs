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
            AddRedirect(objMethod, null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("UpdateProp", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourPropInstanceObjMethods"));


            AddRedirect(typeof(BuildingAI).GetMethod("CalculatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileBuildingAI_CalculatePropGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("CalculateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("TranspileNetLane_CalculateGroupData"));
        }



        #region BuildingAI
        public static IEnumerable<CodeInstruction> TranspileBuildingAI_CalculatePropGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Ldfld && instrList[i].operand is FieldInfo fi && fi.Name == "m_finalProp")
                {
                    instrList.RemoveAt(i);
                    instrList.InsertRange(i, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,7),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("BuildingAI_CalculatePropGroupData",RedirectorUtils.allFlags)),
                        });
                    i += 6;
                }

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static PropInfo BuildingAI_CalculatePropGroupData(BuildingInfo.Prop prop, int j, BuildingAI buildingAI, ushort buildingID)
        {
            var finalProp = prop.m_finalProp;
            if (finalProp == null)
            {
                return null;
            }

            ref Building buildingData = ref BuildingManager.instance.m_buildings.m_buffer[buildingID];
            Matrix4x4 matrix4x2 = default;
            matrix4x2.SetTRS(Building.CalculateMeshPosition(buildingAI.m_info, buildingData.m_position, buildingData.m_angle, buildingData.Length), Quaternion.AngleAxis(buildingData.m_angle * 57.29578f, Vector3.down), Vector3.one);
            Vector3 vector2 = matrix4x2.MultiplyPoint(buildingAI.m_info.m_props[j].m_position);

            var id = new InstanceID { Building = buildingID };
            var angleDummy = 0f;
            var result = GetTargetInfo(finalProp, ref id, ref angleDummy, ref vector2);

            return result;
        }


        #endregion

        #region NetLane
        public static IEnumerable<CodeInstruction> TranspileNetLane_CalculateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 0; i < instrList.Count - 2; i++)
            {
                if (instrList[i + 1].opcode == OpCodes.Stloc_S && instrList[i + 1].operand is LocalBuilder fi && fi.LocalIndex == 11)
                {
                    var localProp = il.DeclareLocal(typeof(PropInfo));
                    var endloopLabel = il.DefineLabel();

                    instrList.InsertRange(i - 1, new List<CodeInstruction>{
                        new CodeInstruction(OpCodes.Ldloc_S,6),
                        new CodeInstruction(OpCodes.Ldloc_S,10),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloc_S,7),
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Ldarg_S,6),
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("NetLane_CalculateGroupData",RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Stloc_S,localProp),
                        new CodeInstruction(OpCodes.Ldloc_S,localProp),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction( OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Equality",RedirectorUtils.allFlags)),
                        new CodeInstruction( OpCodes.Brtrue,endloopLabel ),
                        new CodeInstruction(OpCodes.Ldloc_S,localProp),
                        });
                    while (i < instrList.Count)
                    {
                        if (instrList[i].opcode == OpCodes.Ldloc_S && instrList[i].operand is LocalBuilder lb && lb.LocalIndex == 10)
                        {
                            instrList[i].labels.Add(endloopLabel);
                            break;
                        }
                        i++;
                    }
                    break;
                }

            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static PropInfo NetLane_CalculateGroupData(PropInfo finalInfo, NetLaneProps.Prop prop, int j, uint laneId, int num2, bool flag, bool invert)
        {
            if (finalInfo == null)
            {
                return null;
            }

            ref NetLane thiz = ref NetManager.instance.m_lanes.m_buffer[laneId];
            ref NetSegment thizSeg = ref NetManager.instance.m_segments.m_buffer[thiz.m_segment];

            float startAngle = thizSeg.m_cornerAngleStart * 0.0245436933f;
            float endAngle = thizSeg.m_cornerAngleEnd * 0.0245436933f;


            bool flag2 = flag != invert;

            float num3 = prop.m_segmentOffset * 0.5f;
            if (thiz.m_length != 0f)
            {
                num3 = Mathf.Clamp(num3 + prop.m_position.z / thiz.m_length, -0.5f, 0.5f);
            }
            if (flag2)
            {
                num3 = -num3;
            }


            float num4 = num3 + (float)j / num2;
            Vector3 vector = thiz.m_bezier.Position(num4);
            //LogUtils.DoWarnLog($"thiz.m_bezier = {thiz.m_bezier.a} {thiz.m_bezier.b} {thiz.m_bezier.c} {thiz.m_bezier.d} | vector = {vector} | num4 = {num4} |num3 = {num3} |num2= {num2} |flag2 ={flag2}|prop.m_segmentOffset = {prop.m_segmentOffset}  (seg = {thiz.m_segment} | idx ={new InstanceID { NetSegment = thiz.m_segment }} | origProp = {finalInfo} | j = {j})");
            Vector3 vector2 = default;// thiz.m_bezier.Tangent(num4);
            if (vector2 != Vector3.zero)
            {
                if (flag2)
                {
                    vector2 = -vector2;
                }
                vector2.y = 0f;
                if (prop.m_position.x != 0f)
                {
                    vector2 = Vector3.Normalize(vector2);
                    vector.x += vector2.z * prop.m_position.x;
                    vector.z -= vector2.x * prop.m_position.x;
                }
                float num5 = Mathf.Atan2(vector2.x, -vector2.z);
                if (prop.m_cornerAngle != 0f || prop.m_position.x != 0f)
                {
                    float num6 = endAngle - startAngle;
                    if (num6 > 3.14159274f)
                    {
                        num6 -= 6.28318548f;
                    }
                    if (num6 < -3.14159274f)
                    {
                        num6 += 6.28318548f;
                    }
                    float num7 = startAngle + num6 * num4;
                    num6 = num7 - num5;
                    if (num6 > 3.14159274f)
                    {
                        num6 -= 6.28318548f;
                    }
                    if (num6 < -3.14159274f)
                    {
                        num6 += 6.28318548f;
                    }
                    num5 += num6 * prop.m_cornerAngle;
                    if (num6 != 0f && prop.m_position.x != 0f)
                    {
                        float num8 = Mathf.Tan(num6);
                        vector.x += vector2.x * num8 * prop.m_position.x;
                        vector.z += vector2.z * num8 * prop.m_position.x;
                    }
                }
            }

            var id = new InstanceID { NetSegment = thiz.m_segment };
            var angleDummy = 0f;
            var result = GetTargetInfo(finalInfo, ref id, ref angleDummy, ref vector);

            return result;
        }

        #endregion

        public static bool ApplySwitch(ref PropInfo info, ref InstanceID id, ref float angle, ref Vector3 position) => (info = GetTargetInfo(info, ref id, ref angle, ref position)) != null;
        public static bool ApplySwitchGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle) => (info = GetTargetInfoGlobal(ref info, ref position, propID, ref angle)) != null;


        public static PropInfo GetTargetInfoGlobal(ref PropInfo info, ref Vector3 position, ushort propID, ref float angle)
        {
            var id = new InstanceID { Prop = propID };
            return GetTargetInfo(info, ref id, ref angle, ref position);
        }

        public static IEnumerable<CodeInstruction> DetourPropInstanceObjMethods(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    var localInfo = il.DeclareLocal(typeof(PropInfo));
                    var localAngle = il.DeclareLocal(typeof(float));
                    var localPosition = il.DeclareLocal(typeof(Vector3));

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
                        });
                    i += 10;
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
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    var localInfo = il.DeclareLocal(typeof(PropInfo));
                    var localAngle = il.DeclareLocal(typeof(float));
                    var localPosition = il.DeclareLocal(typeof(Vector3));

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
                    var positionSeed = (Mathf.RoundToInt(position.x) % 100000) + (Mathf.RoundToInt(position.z) % 100000 * 100000);
                    var seed = positionSeed;// switchInfo.SeedSource == SwitchInfo.RandomizerSeedSource.POSITION || id == default || id.Prop != 0 ? (Mathf.RoundToInt(position.x) % 100000) + (Mathf.RoundToInt(position.z) % 100000 * 100000) : (int)id.Index;
                    var r = new Randomizer(seed);
                    var targetIdx = r.Int32((uint)switchInfo.SwitchItems.Length);

                    //       LogUtils.DoWarnLog($"Getting model seed: id = b:{id.Building} ns:{id.NetSegment} nl:{id.NetLane} p:{id.Prop} ({id});pos = {position}; postionSeed: {positionSeed}; targetIdx: {targetIdx}; switchInfo: {switchInfo.GetHashCode().ToString("X16")}; source: {Environment.StackTrace}");
                    //LogUtils.DoWarnLog($"seed =  {id.Index} +{(int)(position.x + position.y + position.z) % 100} = {seed} | targetIdx = {targetIdx} | position = {position}");
                    infoItem = switchInfo.SwitchItems[targetIdx];
                }

                angle += infoItem.RotationOffset * Mathf.Deg2Rad;
            }
        }
    }
}