using ColossalFramework;
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


            //System.Reflection.MethodInfo postRenderMeshs = GetType().GetMethod("AfterRenderMeshes", RedirectorUtils.allFlags);
            //LogUtils.DoLog($"Patching=> {postRenderMeshs}");
            //var orMeth = typeof(BuildingManager).GetMethod("EndRenderingImpl", RedirectorUtils.allFlags);
            //AddRedirect(orMeth, null, postRenderMeshs);
            //System.Reflection.MethodInfo afterEndOverlayImpl = typeof(WTSBuildingPropsSingleton).GetMethod("AfterEndOverlayImpl", RedirectorUtils.allFlags);



            AddRedirect(typeof(BuildingAI).GetMethod("RenderProps", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Public), null, null, GetType().GetMethod("Transpile_BuildingAI_RenderProps"));
            AddRedirect(typeof(BuildingAI).GetMethod("PopulatePropGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_BuildingAI_PopulateGroupData"));
            AddRedirect(typeof(NetLane).GetMethod("RenderInstance", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_RenderInstance"));
            AddRedirect(typeof(NetLane).GetMethod("PopulateGroupData", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_PopulateGroupData"));



            AddRedirect(typeof(TreeInstance).GetMethod("RenderInstance", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("UpdateTree", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("RayCast", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("OverlapQuad", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(TreeInstance).GetMethod("AfterTerrainUpdated", RedirectorUtils.allFlags), GetType().GetMethod("CheckValidTree"));
            //         AddRedirect(typeof(TreeInstance).GetProperty("GrowState", RedirectorUtils.allFlags).GetGetMethod(), null, GetType().GetMethod("OverrideGrowState"));

        }

        public static bool ApplySwitchGlobal(ref TreeInfo info) => (info = GetTargetInfoWithoutId(info)) != null;

        public static bool CheckValidTree(ref TreeInstance __instance) => GetTargetInfoWithPosition(PrefabCollection<TreeInfo>.GetPrefab(__instance.m_infoIndex), __instance.Position) != null;
        //   public static void OverrideGrowState(ref TreeInstance __instance, ref int __result) => __result *= (GetTargetInfoWithPosition(PrefabCollection<TreeInfo>.GetPrefab(__instance.m_infoIndex), __instance.Position) != null ? 1 : 0);
        //public static bool PreTreeInstance_GrowState(ref TreeInstance __instance, ref int result)
        //{
        //    result = GetTargetInfoWithPosition(PrefabCollection<TreeInfo>.GetPrefab(__instance.m_infoIndex), __instance.Position);

        //    return false;
        //}

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

        #region BuildingAI
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
                        //positionCalc                        
                        new CodeInstruction(OpCodes.Ldarg_S,5 ),
                        new CodeInstruction(OpCodes.Ldflda,typeof(RenderManager.Instance).GetField("m_dataMatrix1", RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Ldloc_S,12),
                        new CodeInstruction(OpCodes.Ldfld,typeof(BuildingInfo.Prop).GetField("m_position", RedirectorUtils.allFlags)),
                        new CodeInstruction(OpCodes.Call,typeof(Matrix4x4).GetMethod("MultiplyPoint", RedirectorUtils.allFlags)),

                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("GetTargetInfoFromBuilding") ),

                    }); ;
                    i += 8;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static IEnumerable<CodeInstruction> Transpile_BuildingAI_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);

            for (int i = 1; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Brfalse && instrList[i - 1].opcode == OpCodes.Ldloc_S && instrList[i - 1].operand is LocalBuilder builder && builder.LocalIndex == 19 && instrList[i].operand is Label lbl)
                {
                    i++;
                    while (!instrList[i].labels.Contains(lbl))
                    {
                        instrList.RemoveAt(i);
                    }
                    instrList.InsertRange(i, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldloc_S,19),
                        new CodeInstruction(OpCodes.Ldloca_S,17),
                        new CodeInstruction(OpCodes.Ldarg_S,7),
                        new CodeInstruction(OpCodes.Ldarg_S,4),
                        new CodeInstruction(OpCodes.Ldloca_S,15),
                        new CodeInstruction(OpCodes.Ldloc_S,16),
                        new CodeInstruction(OpCodes.Ldarg_S,8),
                        new CodeInstruction(OpCodes.Ldarg_S,9),
                        new CodeInstruction(OpCodes.Ldarg_S,10),
                        new CodeInstruction(OpCodes.Ldarg_S,11),
                        new CodeInstruction(OpCodes.Ldarg_S,12),
                        new CodeInstruction(OpCodes.Ldarg_S,13),
                        new CodeInstruction(OpCodes.Ldarg_S,14),
                        new CodeInstruction(OpCodes.Ldarg_S,15),
                        new CodeInstruction(OpCodes.Call,typeof(TreeInstanceOverrides).GetMethod("BuildingAI_ProcessTree",RedirectorUtils.allFlags))
                    });
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        protected static void BuildingAI_ProcessTree(BuildingAI thiz, ushort buildingID, ref Building buildingData, TreeInfo treeInfo, ref Randomizer randomizer2, int layer, bool trees,
            ref Matrix4x4 matrix4x2, int j, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            treeInfo = GetTargetInfo_internal(treeInfo, thiz.m_info.m_props[j].m_position, new InstanceID { Building = buildingID })?.GetVariation(ref randomizer2);
            if (treeInfo == null)
            {
                return;
            }

            float scale3 = treeInfo.m_minScale + randomizer2.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
            float brightness = treeInfo.m_minBrightness + randomizer2.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
            if (treeInfo.m_prefabDataLayer == layer && trees)
            {
                Vector3 vector3 = matrix4x2.MultiplyPoint(thiz.m_info.m_props[j].m_position);
                if (!thiz.m_info.m_props[j].m_fixedHeight)
                {
                    vector3.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector3);
                }
                else if (thiz.m_info.m_requireHeightMap)
                {
                    vector3.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector3) + thiz.m_info.m_props[j].m_position.y;
                }
                ushort instanceHolder = (buildingData.m_parentBuilding != 0 && Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None) ? Building.FindParentBuilding(buildingID) : buildingID;
                Vector4 colorLocation = RenderManager.GetColorLocation(instanceHolder);
                if (!thiz.m_info.m_colorizeEverything)
                {
                    colorLocation.z = 0f;
                }
                TreeInstance.PopulateGroupData(treeInfo, vector3, scale3, brightness, colorLocation, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
            }
        }
        #endregion

        #region NetLane
        public static IEnumerable<CodeInstruction> Transpile_NetLane_RenderInstance(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count - 1; i++)
            {
                if (instrList[i].opcode == OpCodes.Brfalse && instrList[i + 1].opcode == OpCodes.Ldloca_S && instrList[i + 1].operand is LocalBuilder builder && builder.LocalIndex == 32 && instrList[i].operand is Label lbl)
                {
                    i++;
                    while (!instrList[i].labels.Contains(lbl))
                    {
                        instrList.RemoveAt(i);
                    }
                    instrList.InsertRange(i, new List<CodeInstruction>
                    {
                         new CodeInstruction(OpCodes.Ldarg_1),
                         new CodeInstruction(OpCodes.Ldarg_2),
                         new CodeInstruction(OpCodes.Ldarg_3),
                         new CodeInstruction(OpCodes.Ldloc_S,11),
                         new CodeInstruction(OpCodes.Ldloc_2),
                         new CodeInstruction(OpCodes.Ldloc_S,13),
                         new CodeInstruction(OpCodes.Ldloc_S,14),
                         new CodeInstruction(OpCodes.Ldloc_S,15),
                         new CodeInstruction(OpCodes.Ldloc_S,12),
                         new CodeInstruction(OpCodes.Ldarg_S,16),
                         new CodeInstruction(OpCodes.Ldarga_S,15),
                        new CodeInstruction(OpCodes.Call, typeof(TreeInstanceOverrides).GetMethod("NetLane_RenderInstance", RedirectorUtils.allFlags) )
                    });
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static void NetLane_RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, int i, bool shallInvert, int num2, int num3, float num4, NetLaneProps.Prop prop, ref int propIndex, ref RenderManager.Instance data)
        {
            ref NetLane thiz = ref NetManager.instance.m_lanes.m_buffer[laneID];
            var randomizer2 = new Randomizer((int)(laneID + (uint)i));
            for (int k = 1; k <= num2; k += 2)
            {
                if (randomizer2.Int32(100u) < prop.m_probability)
                {
                    float t = num4 + k / (float)num2;
                    Vector3 position = thiz.m_bezier.Position(t);
                    if (propIndex != -1)
                    {
                        position.y = data.m_extraData.GetUShort(num3++) * 0.015625f;
                    }
                    position.y += prop.m_position.y;
                    if (prop.m_position.x != 0f)
                    {
                        Vector3 vector3 = thiz.m_bezier.Tangent(t);
                        if (shallInvert)
                        {
                            vector3 = -vector3;
                        }
                        vector3.y = 0f;
                        vector3 = Vector3.Normalize(vector3);
                        position.x += vector3.z * prop.m_position.x;
                        position.z -= vector3.x * prop.m_position.x;
                    }
                    var finalTree = GetTargetInfo_internal(prop.m_finalTree, position, new InstanceID { NetSegment = segmentID });
                    if (finalTree == null)
                    {
                        continue;
                    }
                    TreeInfo variation2 = finalTree.GetVariation(ref randomizer2);
                    float scale2 = variation2.m_minScale + randomizer2.Int32(10000u) * (variation2.m_maxScale - variation2.m_minScale) * 0.0001f;
                    float brightness = variation2.m_minBrightness + randomizer2.Int32(10000u) * (variation2.m_maxBrightness - variation2.m_minBrightness) * 0.0001f;
                    global::TreeInstance.RenderInstance(cameraInfo, variation2, position, scale2, brightness, RenderManager.DefaultColorLocation);
                }
            }
        }
        public static IEnumerable<CodeInstruction> Transpile_NetLane_PopulateGroupData(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 4; i < instrList.Count; i++)
            {
                if (instrList[i].opcode == OpCodes.Bne_Un &&
                    instrList[i - 1].opcode == OpCodes.Ldarg_S && instrList[i - 1].operand is byte op && op == 11 &&
                     instrList[i - 2].opcode == OpCodes.Ldfld && instrList[i - 3].opcode == OpCodes.Ldloc_S && instrList[i - 3].operand is LocalBuilder localBuilder && localBuilder.LocalIndex == 23
                     && instrList[i].operand is Label lbl)
                {
                    i++;
                    while (!instrList[i].labels.Contains(lbl))
                    {
                        instrList.RemoveAt(i);
                    }

                    var newInstrs = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldloc_S,5),
                        new CodeInstruction(OpCodes.Ldloc_2),
                        new CodeInstruction(OpCodes.Ldloc_S,7),
                        new CodeInstruction(OpCodes.Ldloc_S,8),
                        new CodeInstruction(OpCodes.Ldarg_S,10),
                        new CodeInstruction(OpCodes.Ldloc_S,6),
                        new CodeInstruction(OpCodes.Ldloc_S,23),
                        new CodeInstruction(OpCodes.Ldarg_S,12),
                        new CodeInstruction(OpCodes.Ldarg_S,13),
                        new CodeInstruction(OpCodes.Ldarg_S,14),
                        new CodeInstruction(OpCodes.Ldarg_S,15),
                        new CodeInstruction(OpCodes.Ldarg_S,16),
                        new CodeInstruction(OpCodes.Ldarg_S,17),
                        new CodeInstruction(OpCodes.Ldarg_S,18),
                        new CodeInstruction(OpCodes.Ldarg_S,19),

                        new CodeInstruction(OpCodes.Call,typeof(TreeInstanceOverrides).GetMethod("NetLane_PopulateGroupData",RedirectorUtils.allFlags))
                    };
                    instrList.InsertRange(i, newInstrs);
                    i += instrList.Count;

                }
            }

            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }
        public static void NetLane_PopulateGroupData(ushort segmentID, uint laneID, int i, bool shallInvert, int num2, float num3, bool terrainHeight, NetLaneProps.Prop prop, TreeInfo finalTree, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance)
        {
            ref NetLane thiz = ref NetManager.instance.m_lanes.m_buffer[laneID];
            var randomizer2 = new Randomizer((int)(laneID + (uint)i));
            for (int k = 1; k <= num2; k += 2)
            {
                float t = num3 + k / (float)num2;
                Vector3 vector3 = thiz.m_bezier.Position(t);
                if (prop.m_position.x != 0f)
                {
                    Vector3 vector4 = thiz.m_bezier.Tangent(t);
                    if (shallInvert)
                    {
                        vector4 = -vector4;
                    }
                    vector4.y = 0f;
                    vector4 = Vector3.Normalize(vector4);
                    vector3.x += vector4.z * prop.m_position.x;
                    vector3.z -= vector4.x * prop.m_position.x;
                }
                if (terrainHeight)
                {
                    vector3.y = Singleton<TerrainManager>.instance.SampleDetailHeight(vector3);
                }
                vector3.y += prop.m_position.y;
                finalTree = GetTargetInfo_internal(finalTree, vector3, new InstanceID { NetSegment = segmentID });
                if (finalTree == null)
                {
                    continue;
                }

                TreeInfo variation2 = finalTree.GetVariation(ref randomizer2);
                float scale2 = variation2.m_minScale + randomizer2.Int32(10000u) * (variation2.m_maxScale - variation2.m_minScale) * 0.0001f;
                float brightness = variation2.m_minBrightness + randomizer2.Int32(10000u) * (variation2.m_maxBrightness - variation2.m_minBrightness) * 0.0001f;

                if (randomizer2.Int32(100u) < prop.m_probability)
                {
                    global::TreeInstance.PopulateGroupData(variation2, vector3, scale2, brightness, RenderManager.DefaultColorLocation, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
                }
            }

        }
        #endregion

        public static TreeInfo GetTargetInfoWithoutId(TreeInfo info) => GetTargetInfo_internal(info);
        public static TreeInfo GetTargetInfoWithPosition(TreeInfo info, Vector3 position) => GetTargetInfo_internal(info, position);
        public static TreeInfo GetTargetInfoFromNetSegment(TreeInfo info, ushort segmentId, Vector3 position) => GetTargetInfo_internal(info, position, new InstanceID { NetSegment = segmentId });
        public static TreeInfo GetTargetInfoFromBuilding(TreeInfo info, ushort buildingId, Vector3 position) => GetTargetInfo_internal(info, position, new InstanceID { Building = buildingId });
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
                    //LogUtils.DoWarnLog($"seed =  {id.Index} +{(int)(position.x + position.y + position.z) % 100} = {seed} | targetIdx = {targetIdx} | position = {position}");
                    infoItem = switchInfo.SwitchItems[targetIdx];

                }
            }
        }

    }
}