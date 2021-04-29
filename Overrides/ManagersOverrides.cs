using ColossalFramework;
using ColossalFramework.Math;
using Harmony;
using Klyte.Commons.Extensions;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.PropSwitcher.Overrides
{
    public class ManagersOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            AddRedirect(typeof(NetManager).GetMethod("UpdateSegmentRenderer", RedirectorUtils.allFlags), null, null, GetType().GetMethod("Transpile_NetLane_UpdateSegmentRenderer"));
            AddRedirect(typeof(BuildingManager).GetMethod("GetLayers", RedirectorUtils.allFlags), GetType().GetMethod("PreGetLayers"));
        }

        public static IEnumerable<CodeInstruction> Transpile_NetLane_UpdateSegmentRenderer(ILGenerator il, IEnumerable<CodeInstruction> instr)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count - 2; i++)
            {
                if (instrList[i].opcode == OpCodes.Stloc_S && instrList[i].operand is LocalBuilder lb && lb.LocalIndex == 16)
                {
                    var lblLoop = il.DefineLabel();
                    var codeList = new List<CodeInstruction>
                            {
                                 new CodeInstruction(OpCodes.Ldloc_S,16),
                                 new CodeInstruction(OpCodes.Ldarg_S,1),
                                 new CodeInstruction(OpCodes.Ldloc_S,15),
                                 new CodeInstruction(OpCodes.Ldloc_S,11),
                                 new CodeInstruction(OpCodes.Ldloca_S,10),
                                 new CodeInstruction(OpCodes.Call, typeof(ManagersOverrides).GetMethod("CalculateLayerFlags", RedirectorUtils.allFlags) ),
                                 new CodeInstruction(OpCodes.Br,lblLoop ),
                            };
                    instrList.InsertRange(i + 1, codeList);
                    for (int j = i; j < instrList.Count - 4; j++)
                    {
                        if (instrList[j + 3].opcode == OpCodes.Stloc_S && instrList[j + 3].operand is LocalBuilder lb2 && lb2.LocalIndex == 15)
                        {
                            instrList[j].labels.Add(lblLoop);
                            break;
                        }
                    }
                    break;
                }
            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static void CalculateLayerFlags(NetLaneProps.Prop prop, ushort segmentId, uint laneId, int i, ref int flags)
        {
            if (!(prop.m_finalTree is null))
            {
                ref NetLane lane = ref NetManager.instance.m_lanes.m_buffer[laneId];
                int num2 = 2;
                if (prop.m_repeatDistance > 1f)
                {
                    num2 *= Mathf.Max(1, Mathf.RoundToInt(lane.m_length / prop.m_repeatDistance));
                }
                for (int k = 1; k < num2; k += 2)
                {
                    var tree = TreeInstanceOverrides.GetTargetInfoFromNetLane(prop.m_finalTree, laneId, i, k, false);
                    if (tree != null)
                    {
                        flags |= 1 << tree.m_prefabDataLayer;
                    }
                }
            }
            if (!(prop.m_finalProp is null))
            {
                ref NetLane lane = ref NetManager.instance.m_lanes.m_buffer[laneId];
                int num2 = 2;
                if (prop.m_repeatDistance > 1f)
                {
                    num2 *= Mathf.Max(1, Mathf.RoundToInt(lane.m_length / prop.m_repeatDistance));
                }
                for (int k = 1; k < num2; k += 2)
                {
                    var targetProp = PropInstanceOverrides.NetLane_CalculateTargetProp(prop.m_finalProp, laneId, out _, i, k, false);
                    if (targetProp != null)
                    {
                        flags |= 1 << targetProp.m_prefabDataLayer;
                        if (targetProp.m_effectLayer != -1)
                        {
                            flags |= 1 << targetProp.m_effectLayer;
                        }
                    }
                }
            }
        }

        public static bool PreGetLayers(ref int __result, BuildingInfo info, ushort building)
        {
            if (info == null)
            {
                __result = 0;
                return false;
            }
            int num = (1 << info.m_prefabDataLayer) | (1 << Singleton<NotificationManager>.instance.m_notificationLayer);
            if (info.m_props != null)
            {
                for (int i = 0; i < info.m_props.Length; i++)
                {
                    Randomizer randomizer = new Randomizer((building << 6) | info.m_props[i].m_index);
                    if (randomizer.Int32(100U) < info.m_props[i].m_probability)
                    {
                        PropInfo propInfo = PropInstanceOverrides.BuildingAI_CalculateTargetPropIgnoreOffsets(info.m_props[i], info.m_buildingAI, building, i);
                        TreeInfo treeInfo = TreeInstanceOverrides.GetTargetInfoFromBuilding(info.m_props[i].m_finalTree, null, building, i, out _, false);
                        if (propInfo != null)
                        {
                            propInfo = propInfo.GetVariation(ref randomizer);
                            num |= 1 << propInfo.m_prefabDataLayer;
                            if (propInfo.m_effectLayer != -1)
                            {
                                LightSystem lightSystem = Singleton<RenderManager>.instance.lightSystem;
                                if (info.m_isFloating && info.m_props[i].m_fixedHeight && propInfo.m_effectLayer == lightSystem.m_lightLayer)
                                {
                                    num |= 1 << lightSystem.m_lightLayerFloating;
                                }
                                else
                                {
                                    num |= 1 << propInfo.m_effectLayer;
                                }
                            }
                        }
                        else if (treeInfo != null)
                        {
                            treeInfo = treeInfo.GetVariation(ref randomizer);
                            num |= 1 << treeInfo.m_prefabDataLayer;
                        }
                    }
                }
            }
            __result = num;
            return false;
        }
    }
}