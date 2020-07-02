using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

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
            foreach (var method in allMethods)
            {
                AddRedirect(method, propSwitchMethod);
            }
            AddRedirect(typeof(PropInstance).GetMethod("RenderLod", RedirectorUtils.allFlags), propSwitchMethod);
            AddRedirect(typeof(PropInstance).GetMethod("TerrainUpdated", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethod);
            AddRedirect(typeof(PropInstance).GetMethod("PopulateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethod);
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Instance), propSwitchMethod);
            AddRedirect(objMethod, null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("UpdateProp", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("CheckOverlap", RedirectorUtils.allFlags), null, null, GetType().GetMethod("DetourRenederInstanceObj"));
            AddRedirect(typeof(PropInstance).GetMethod("CalculateGroupData", RedirectorUtils.allFlags & ~System.Reflection.BindingFlags.Static), null, null, GetType().GetMethod("DetourRenederInstanceObj"));

        }

        public static string m_in = "Bus Stop Large";
        public static string m_out = "São Paulo 2000's bus stop.São Paulo 2000's bus stop_Data";

        public static void ApplySwitch(ref PropInfo info) => info = GetTargetInfo(info);

        public static IEnumerable<CodeInstruction> DetourRenederInstanceObj(IEnumerable<CodeInstruction> instr, ILGenerator il)
        {
            var instrList = new List<CodeInstruction>(instr);
            for (int i = 0; i < instrList.Count; i++)
            {
                if (instrList[i].operand == typeof(PropInstance).GetProperty("Info", RedirectorUtils.allFlags).GetGetMethod())
                {
                    instrList.InsertRange(i + 1, new List<CodeInstruction>{
                        new CodeInstruction( OpCodes.Call, typeof(PropInstanceOverrides).GetMethod("GetTargetInfo",RedirectorUtils.allFlags)),
                        });
                    i += 2;
                }

            }
            LogUtils.PrintMethodIL(instrList);

            return instrList;
        }

        public static PropInfo GetTargetInfo(PropInfo info)
        {
            if (info.name == m_in)
            {
                info = PrefabCollection<PropInfo>.FindLoaded(m_out ?? "") ?? info;

                //  LogUtils.DoWarnLog($"info.m_hasEffects ={info.m_hasEffects }");
            }

            return info;
        }

    }
}