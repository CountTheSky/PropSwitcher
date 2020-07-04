using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.UI;
using UnityEngine;

namespace Klyte.PropSwitcher
{
    public class PSPanel : BasicKPanel<PropSwitcherMod, PSController, PSPanel>
    {
        public override float PanelWidth => m_controlContainer.width;

        public override float PanelHeight => 800;

        private UITabstrip m_stripMain;

        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out m_stripMain, MainPanel.transform, "WTSTabstrip", new Vector4(5, 40, MainPanel.width - 10, 40));
            m_stripMain.startSelectedIndex = -1;
            m_stripMain.selectedIndex = -1;

            KlyteMonoUtils.CreateUIElement(out UITabContainer tabContainer, MainPanel.transform, "WTSTabContainer", new Vector4(0, 80, MainPanel.width, MainPanel.height - 80));
            m_stripMain.tabPages = tabContainer;

            m_stripMain.CreateTabLocalized<PSGlobalPropTab>("ToolbarIconZoomOutGlobe", "K45_PS_GLOBALPROP_TAB", "PSGlobalPropTab", false);
            m_stripMain.CreateTabLocalized<PSBuildingPropTab>("IconAssetBuilding", "K45_PS_BUILDINGPROP_TAB", "PSBuildingPropTab", false);
            m_stripMain.CreateTabLocalized<PSNetPropTab>("IconAssetRoad", "K45_PS_NETPROP_TAB", "PSNetPropTab", false);
        }

    }
}