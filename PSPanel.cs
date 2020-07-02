using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.PropSwitcher
{
    public class PSPanel : BasicKPanel<PropSwitcherMod, PSController, PSPanel>
    {
        public override float PanelWidth => 400;

        public override float PanelHeight => 350;

        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out UIPanel layoutPanel, MainPanel.transform, "LayoutPanel", new Vector4(0, 40, PanelWidth, PanelHeight - 40));
            layoutPanel.padding = new RectOffset(8, 8, 10, 10);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 10, 10);
            var uiHelper = new UIHelperExtension(layoutPanel);



        }

    }
}