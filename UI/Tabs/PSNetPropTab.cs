using System.Collections.Generic;
using System.Linq;
using static Klyte.PropSwitcher.PSController;

namespace Klyte.PropSwitcher.UI
{
    public class PSNetPropTab : PSPrefabPropTab<NetInfo>
    {
        protected override Dictionary<string, NetInfo> PrefabsLoaded => PropSwitcherMod.Controller.NetsLoaded;

        protected override string TitleLocale => "K45_PS_NETPROP_EDITOR";
        internal override bool IsPropAvailable(KeyValuePair<string, TextSearchEntry> x) => GetCurrentParentPrefab().m_lanes?.SelectMany(y => y?.m_laneProps?.m_props ?? new NetLaneProps.Prop[0]).Where(y => y?.m_finalProp?.name == x.Value.prefabName || y?.m_finalTree?.name == x.Value.prefabName).FirstOrDefault() != default;
        internal override void EnablePickTool()
        {
            m_prefab.text = "";
            UpdateDetoursList();
            PropSwitcherMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (x) =>
            {
                if (x > 0)
                {
                    string infoName = NetManager.instance.m_segments.m_buffer[x].Info.name;
                    m_prefab.text = PropSwitcherMod.Controller.NetsLoaded.Where(x => x.Value?.name == infoName).FirstOrDefault().Key ?? "";
                    m_in.text = "";
                    m_out.text = "";
                    m_selectedEntry = null;
                    m_rotationOffset.text = "0.000";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.RoadSegmentToolInstance.enabled = true;
        }

        protected override NetInfo GetCurrentParentPrefab() => PropSwitcherMod.Controller.NetsLoaded.TryGetValue(m_prefab.text, out NetInfo info1) ? info1 : null;

    }
}