using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;
using static Klyte.PropSwitcher.PSController;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

namespace Klyte.PropSwitcher.UI
{
    public class PSBuildingPropTab : PSPrefabPropTab<BuildingInfo>
    {
        public PSBuildingPropTab Instance { get; private set; }
        protected UITextField[] m_positionOffset;

        public void Start() => Instance = this;

        private Dictionary<string, Tuple<int, Vector3, PrefabInfo>> currentReplacementSpotList;
        private string cachedInfo;

        protected override Dictionary<string, BuildingInfo> PrefabsLoaded => PropSwitcherMod.Controller.BuildingsLoaded;

        protected override string TitleLocale => "K45_PS_BUILDINGPROP_EDITOR";

        internal override void EnablePickTool()
        {
            m_prefab.text = "";
            UpdateDetoursList();
            PropSwitcherMod.Controller.BuildingEditorToolInstance.OnBuildingSelect += (x) =>
            {
                if (x > 0)
                {
                    string infoName = BuildingManager.instance.m_buildings.m_buffer[x].Info.name;
                    m_prefab.text = PropSwitcherMod.Controller.BuildingsLoaded.Where(x => x.Value?.name == infoName).FirstOrDefault().Key ?? "";
                    m_in.text = "";
                    m_out.text = "";
                    m_selectedEntry = null;
                    m_rotationOffset.text = "0.000";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.BuildingEditorToolInstance.enabled = true;
        }
        internal override bool IsPropAvailable(KeyValuePair<string, TextSearchEntry> x) => GetCurrentParentPrefab().m_props.Union(GetCurrentParentPrefab().m_subBuildings?.SelectMany(x => x.m_buildingInfo.m_props) ?? new BuildingInfo.Prop[0]).Any(y => y?.m_finalProp?.name == x.Value.prefabName || y?.m_finalTree?.name == x.Value.prefabName);

        protected override string[] OnChangeFilterIn(string arg)
        {
            BuildingInfo prefab = GetCurrentParentPrefab();

            return PropSwitcherMod.Controller.PropsLoaded?
                    .Union(PropSwitcherMod.Controller.TreesLoaded)?
                    .Where(x => IsPropAvailable(x))
                    .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                    .Select(x => x.Key)
                    .OrderBy((x) => x)
                .Union(
                    currentReplacementSpotList.Keys.Where(x => LocaleManager.cultureInfo.CompareInfo.IndexOf(x, arg, CompareOptions.IgnoreCase) >= 0)
                )
                .ToArray() ?? new string[0];
        }
        protected override PrefabChildEntryKey GetEntryFor(string v) =>
            v.StartsWith("Prop #") && currentReplacementSpotList.TryGetValue(v, out Tuple<int, Vector3, PrefabInfo> item)
                ? new PrefabChildEntryKey(item.First)
                : base.GetEntryFor(v);

        protected override bool IsProp(string v)
        {
            if (v?.StartsWith("Prop #") ?? false)
            {
                if (currentReplacementSpotList.TryGetValue(v, out Tuple<int, Vector3, PrefabInfo> prop))
                {
                    return prop.Third is PropInfo;
                }
            }
            return base.IsProp(v);
        }

        protected override BuildingInfo GetCurrentParentPrefab() => PropSwitcherMod.Controller.BuildingsLoaded.TryGetValue(m_prefab.text, out BuildingInfo info1) ? info1 : null;
        protected override void DoExtraInputOptions(UIHelperExtension uiHelper)
        {
            AddVector3Field(Locale.Get("K45_PS_POSITIONOFFSETLABEL"), out m_positionOffset, uiHelper, (x) => { });
            base.DoExtraInputOptions(uiHelper);
        }

        protected override void WriteExtraSettings(SwitchInfo info, Item currentItem)
        {
            base.WriteExtraSettings(info, currentItem);
            currentItem.PositionOffset = m_positionOffset[0].parent.isVisible
                ? new Vector3Xml()
                {
                    X = float.TryParse(m_positionOffset[0].text, out float x) ? x : 0,
                    Y = float.TryParse(m_positionOffset[1].text, out float y) ? y : 0,
                    Z = float.TryParse(m_positionOffset[2].text, out float z) ? z : 0
                }
                : new Vector3Xml();
        }
        protected override void DoOnUpdateDetoursList(bool isEditable, Item targetItem)
        {
            var prefab = GetCurrentParentPrefab();
            if (cachedInfo != prefab?.name)
            {
                cachedInfo = prefab?.name;
                if (cachedInfo != null)
                {
                    currentReplacementSpotList = prefab.m_props.Select((x, i) => Tuple.New(i, x.m_position, new PrefabChildEntryKey(i).ToString(prefab), (PrefabInfo)x.m_finalProp ?? x.m_finalTree)).ToDictionary(x => x.Third, x => Tuple.New(x.First, x.Second, x.Fourth));
                }
            }
            base.DoOnUpdateDetoursList(isEditable, targetItem);
            m_positionOffset[0].parent.isVisible = isEditable && (m_selectedEntry?.PrefabIdx ?? -1) >= 0;
            m_positionOffset[0].text = (targetItem?.PositionOffset?.X ?? 0).ToString("0.000");
            m_positionOffset[1].text = (targetItem?.PositionOffset?.Y ?? 0).ToString("0.000");
            m_positionOffset[2].text = (targetItem?.PositionOffset?.Z ?? 0).ToString("0.000");
        }

        protected override void SetCurrentLoadedExtraData(string fromSource, SwitchInfo info, Item item)
        {
            base.SetCurrentLoadedExtraData(fromSource, info, item);
            m_positionOffset[0].parent.isVisible = IsProp(fromSource) && (m_selectedEntry?.PrefabIdx ?? -1) >= 0;
        }

        protected override void DoOnChangeValueIn(string fromSource)
        {
            base.DoOnChangeValueIn(fromSource);
            m_positionOffset[0].parent.isVisible = IsProp(fromSource) && (m_selectedEntry?.PrefabIdx ?? -1) >= 0;
        }
    }
}