using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Libraries;
using Klyte.PropSwitcher.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

namespace Klyte.PropSwitcher.UI
{
    public abstract class PSPrefabPropTab<T> : UICustomControl, IPSBaseTab where T : PrefabInfo
    {

        protected UITextField m_prefab;
        private UIPanel m_titleRow;
        private UICheckBox m_seedSource;
        protected UITextField m_in;
        protected UITextField m_out;
        protected UITextField m_rotationOffset;
        private UIScrollablePanel m_detourList;
        private UITemplateList<UIPanel> m_listItems;
        private UIButton m_btnDelete;
        private UIButton m_btnExport;
        protected abstract Dictionary<string, T> PrefabsLoaded { get; }

        private UIPanel m_actionBar;
        private UIButton m_addButton;
        private UITextField m_filterIn;
        private UITextField m_filterOut;
        private UIPanel m_filterRow;
        private UIDropDown m_filterSource;

        protected void Awake()
        {
            UIPanel layoutPanel = GetComponent<UIPanel>();
            layoutPanel.padding = new RectOffset(8, 8, 10, 10);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 5);
            layoutPanel.clipChildren = true;
            var uiHelper = new UIHelperExtension(layoutPanel);

            KlyteMonoUtils.CreateUIElement(out m_actionBar, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, layoutPanel.width, 50));
            m_actionBar.autoLayout = true;
            m_actionBar.autoLayoutDirection = LayoutDirection.Vertical;
            m_actionBar.padding = new RectOffset(5, 5, 5, 5);
            m_actionBar.autoFitChildrenVertically = true;
            var m_topHelper = new UIHelperExtension(m_actionBar);

            AddLabel("", m_topHelper, out UILabel m_labelSelectionDescription, out UIPanel m_containerSelectionDescription);
            m_btnDelete = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", false);
            m_btnExport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_PS_EXPORTASGLOBAL", false);
            AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadFiles, "K45_PS_RELOADGLOBAL", false);

            AddFilterableInput(Locale.Get("K45_PS_PARENTPREFAB", typeof(T).Name), uiHelper, out m_prefab, out UIListBox popup, OnChangeFilterPrefab, GetCurrentValuePrefab, OnChangeValuePrefab);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddCheckboxLocale("K45_PS_SAMESEEDFORBUILDINGNET", out m_seedSource, uiHelper, (x) => { });
            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            AddVector2Field(Locale.Get("K45_PS_ROTATIONOFFSET"), out UITextField[] m_rotationOffset, uiHelper, (x) => { });
            this.m_rotationOffset = m_rotationOffset[0];
            Destroy(m_rotationOffset[1]);
            m_addButton = uiHelper.AddButton(Locale.Get("K45_PS_ADDREPLACEMENTRULE"), OnAddRule) as UIButton;
            m_prefab.eventTextSubmitted += (x, y) => UpdateDetoursList();
            popup.eventSelectedIndexChanged += (x, y) => UpdateDetoursList();

            m_prefab.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
            m_in.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
            m_out.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";

            AddButtonInEditorRow(m_prefab, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Dropper, EnablePickTool, Locale.Get("K45_PS_ENABLETOOLPICKER"), true, 30).zOrder = m_prefab.zOrder + 1;

            float listContainerWidth = layoutPanel.width - 20;
            KlyteMonoUtils.CreateUIElement(out m_titleRow, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            m_titleRow.autoLayout = true;
            m_titleRow.wrapLayout = false;
            m_titleRow.autoLayoutDirection = LayoutDirection.Horizontal;

            KlyteMonoUtils.CreateUIElement(out m_filterRow, layoutPanel.transform, "filterRow", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            m_filterRow.autoLayout = true;
            m_filterRow.wrapLayout = false;
            m_filterRow.autoLayoutDirection = LayoutDirection.Horizontal;
            CreateFilterPlaceHolder(listContainerWidth - 20, m_filterRow, out m_filterIn, out m_filterOut, out m_filterSource);

            CreateRowPlaceHolder(listContainerWidth - 20, m_titleRow, out UILabel col1Title, out UILabel col2Title, out UILabel col3Title, out UILabel col4Title);
            KlyteMonoUtils.LimitWidthAndBox(col1Title, col1Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col2Title, col2Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col3Title, col3Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col4Title, col4Title.width, true);
            col1Title.text = Locale.Get("K45_PS_SWITCHFROM_TITLE");
            col2Title.text = Locale.Get("K45_PS_SWITCHTO_TITLE");
            col3Title.text = Locale.Get("K45_PS_ROTATION_TITLE");
            col4Title.text = Locale.Get("K45_PS_ACTIONS_TITLE");

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "previewPanel", new UnityEngine.Vector4(0, 0, listContainerWidth, 370));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_detourList.backgroundSprite = "GenericPanel";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;
            PSSwitchEntry.CreateTemplateDetourItem(m_detourList.width);
            m_listItems = new UITemplateList<UIPanel>(m_detourList, PSSwitchEntry.DETOUR_ITEM_TEMPLATE);



            UpdateDetoursList();
            this.m_rotationOffset.parent.isVisible = false;
        }

        private void CreateFilterPlaceHolder(float targetWidth, UIPanel panel, out UITextField filterIn, out UITextField fillterOut, out UIDropDown filterSource)
        {
            KlyteMonoUtils.CreateUIElement(out filterIn, panel.transform, "FromFld", new Vector4(0, 0, targetWidth * 0.38f, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(filterIn);
            filterIn.minimumSize = new Vector2(0, 25);
            filterIn.verticalAlignment = UIVerticalAlignment.Middle;
            filterIn.eventTextChanged += (x, y) => UpdateDetoursList();
            filterIn.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");
            KlyteMonoUtils.CreateUIElement(out fillterOut, panel.transform, "ToFld", new Vector4(0, 0, targetWidth * 0.38f, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(fillterOut);
            fillterOut.minimumSize = new Vector2(0, 25);
            fillterOut.verticalAlignment = UIVerticalAlignment.Middle;
            fillterOut.eventTextChanged += (x, y) => UpdateDetoursList();
            fillterOut.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");

            filterSource = UIHelperExtension.CloneBasicDropDownNoLabel(Enum.GetNames(typeof(SourceFilterOptions)).Select(x => Locale.Get("K45_PS_FILTERSOURCEITEM", x)).ToArray(), (x) => UpdateDetoursList(), panel);
            filterSource.area = new Vector4(0, 0, targetWidth * 0.24f, 28);
            filterSource.textScale = 1;
            filterSource.zOrder = 2;
        }

        private enum SourceFilterOptions
        {
            ALL,
            GLOBAL,
            SAVEGAME,
            ACTIVE
        }

        private void OnReloadFiles()
        {
            PropSwitcherMod.Controller.ReloadPropGlobals();
            UpdateDetoursList();
        }


        internal abstract void EnablePickTool();

        private string OnChangeValuePrefab(int arg1, string[] arg2)
        {
            m_in.text = "";
            m_out.text = "";
            m_rotationOffset.text = "0.000";
            m_rotationOffset.parent.isVisible = false;
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                return arg2[arg1];
            }
            else
            {
                return "";
            }
        }
        private string GetCurrentValuePrefab() => "";

        private string[] OnChangeFilterPrefab(string arg)
        {
            return PrefabsLoaded
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value.name + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        private void OnClearList()
        {
            PSPropData.Instance.PrefabChildEntries.Remove(m_prefab.text);
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();
        }

        private void OnExportAsGlobal()
        {
            var targetPrefabName = GetCurrentTargetPrefab()?.name ?? "";
            if (PSPropData.Instance.PrefabChildEntries.TryGetValue(targetPrefabName, out SimpleXmlDictionary<string, SwitchInfo> currentEditingSelection) | PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(targetPrefabName, out SimpleXmlDictionary<string, SwitchInfo> globalCurrentEditingSelection))
            {
                SimpleXmlDictionary<string, SwitchInfo> result = globalCurrentEditingSelection ?? new SimpleXmlDictionary<string, SwitchInfo>();
                foreach (var entry in currentEditingSelection)
                {
                    result[entry.Key] = entry.Value;
                }
                var targetFilename = Path.Combine(PSController.DefaultGlobalPropConfigurationFolder, $"{targetPrefabName}.xml");
                File.WriteAllText(targetFilename, XmlUtils.DefaultXmlSerialize(new ILibableAsContainer<string, SwitchInfo>
                {
                    SaveName = targetPrefabName,
                    Data = result
                }));
                PropSwitcherMod.Controller?.ReloadPropGlobals();

                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = string.Format(Locale.Get("K45_PS_SUCCESSEXPORTDATAGLOBAL"), PSLibPropSettings.Instance.DefaultXmlFileBaseFullPath),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK"),
                    showButton2 = true,
                    textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
                }, (x) =>
                {
                    if (x == 2)
                    {
                        Utils.OpenInFileBrowser(targetFilename);
                        return false;
                    }
                    return true;
                });

                UpdateDetoursList();
            }
        }

        protected T GetCurrentTargetPrefab() => PrefabsLoaded.TryGetValue(m_prefab.text, out T info1) ? info1 : null;



        private static void CreateRowPlaceHolder(float targetWidth, UIPanel panel, out UILabel column1, out UILabel column2, out UILabel column3, out UILabel actionsContainer)
        {
            KlyteMonoUtils.CreateUIElement(out column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * 0.38f, 25));
            column1.minimumSize = new Vector2(0, 25);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column2, panel.transform, "ToLbl", new Vector4(0, 0, targetWidth * 0.38f, 25));
            column2.minimumSize = new Vector2(0, 25);
            column2.verticalAlignment = UIVerticalAlignment.Middle;
            column2.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column3, panel.transform, "RotLbl", new Vector4(0, 0, targetWidth * 0.06f, 25));
            column3.minimumSize = new Vector2(0, 25);
            column3.verticalAlignment = UIVerticalAlignment.Middle;
            column3.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out actionsContainer, panel.transform, "ActionsPanel", new Vector4(0, 0, targetWidth * 0.18f, 25));
            actionsContainer.minimumSize = new Vector2(0, 25);
            actionsContainer.verticalAlignment = UIVerticalAlignment.Middle;
            actionsContainer.textAlignment = UIHorizontalAlignment.Center;
        }


        public void UpdateDetoursList()
        {
            bool isEditable = !(GetCurrentTargetPrefab()?.name).IsNullOrWhiteSpace();
            PSPropData.Instance.PrefabChildEntries.TryGetValue(GetCurrentTargetPrefab()?.name ?? "", out SimpleXmlDictionary<string, SwitchInfo> currentEditingSelection);
            PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(GetCurrentTargetPrefab()?.name ?? "", out SimpleXmlDictionary<string, SwitchInfo> globalCurrentEditingSelection);
            m_in.parent.isVisible = isEditable;
            m_out.parent.isVisible = isEditable;
            m_rotationOffset.parent.isVisible = isEditable && PropSwitcherMod.Controller.PropsLoaded.ContainsKey(m_in.text);
            m_addButton.isVisible = isEditable;
            m_detourList.parent.isVisible = isEditable;
            m_btnDelete.isVisible = isEditable;
            m_btnExport.isVisible = isEditable;
            m_titleRow.isVisible = isEditable;
            m_filterRow.isVisible = isEditable;
            m_seedSource.isVisible = isEditable;


            if (isEditable)
            {
                var keyListLocal = currentEditingSelection?.Where(x =>
                            m_filterSource.selectedIndex != (int)SourceFilterOptions.GLOBAL
                         && (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key))
                         && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.TargetPrefab))))
                    .Select(x => Tuple.New(x.Key, x.Value)).OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x.First == y.Value).FirstOrDefault().Key ?? x.First).ToArray() ?? new Tuple<string, SwitchInfo>[0];
                var keyListGlobal = globalCurrentEditingSelection?.Where(x =>
                            m_filterSource.selectedIndex != (int)SourceFilterOptions.SAVEGAME
                         && (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key))
                         && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.TargetPrefab)))
                         && (m_filterSource.selectedIndex != (int)SourceFilterOptions.ACTIVE || keyListLocal.Count(y => y.First == x.Key) == 0)
                         )
                    .Select(x => Tuple.New(x.Key, x.Value)).OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x.First == y.Value).FirstOrDefault().Key ?? x.First).ToArray() ?? new Tuple<string, SwitchInfo>[0];
                var rows = m_listItems.SetItemCount(keyListLocal.Length + keyListGlobal.Length);
                BuildItems(ref rows, keyListGlobal, 0, true, currentEditingSelection);
                BuildItems(ref rows, keyListLocal, keyListGlobal.Length, false);
            }

        }
        private static bool CheckIfPrefabMatchesFilter(string filter, string prefabName) => LocaleManager.cultureInfo.CompareInfo.IndexOf(prefabName == null ? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER") : prefabName + (PrefabUtils.instance.AuthorList.TryGetValue(prefabName.Split('.')[0], out string author) ? "\n" + author : ""), filter, CompareOptions.IgnoreCase) >= 0;


        private void BuildItems(ref UIPanel[] rows, Tuple<string, SwitchInfo>[] keyList, int offset, bool isGlobal, Dictionary<string, SwitchInfo> localList = null)
        {
            for (int i = offset; i < offset + keyList.Length; i++)
            {
                var currentData = keyList[i - offset];
                var targetTextColor = isGlobal ? (localList?.Count(x => x.Key == currentData.First) ?? 0) == 0 ? Color.green : Color.red : Color.white;

                rows[i].GetComponent<PSSwitchEntry>().SetData(GetCurrentTargetPrefab()?.name, currentData.First, currentData.Second, targetTextColor, isGlobal);
            }
        }

        private void OnAddRule()
        {
            if ((GetCurrentTargetPrefab()?.name).IsNullOrWhiteSpace())
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_INVALIDPARENTPREFABSELECTION"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }
            if (m_in.text.IsNullOrWhiteSpace())
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_INVALIDINPUTINOUT"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }
            if (!PSPropData.Instance.PrefabChildEntries.ContainsKey(GetCurrentTargetPrefab().name) || PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name] == null)
            {
                PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name] = new SimpleXmlDictionary<string, SwitchInfo>();
            }
            _ = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_in.text, out string inText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_in.text, out inText);
            _ = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_out.text, out string outText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_out.text, out outText);

            if (!PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name].ContainsKey(inText))
            {
                PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name][inText] = new Xml.SwitchInfo();
            }

            PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name][inText].Add(m_out.text.IsNullOrWhiteSpace() ? null : outText, float.TryParse(m_rotationOffset.text, out float offset) ? offset % 360 : 0);
            PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name][inText].SeedSource = m_seedSource.isChecked ? RandomizerSeedSource.INSTANCE : RandomizerSeedSource.POSITION;
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();

        }

        private string OnChangeValueOut(int arg1, string[] arg2)
        {
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                return arg2[arg1];
            }
            else
            {
                return "";
            }
        }
        private string GetCurrentValueOut() => "";
        private string[] OnChangeFilterOut(string arg)
        {
            if (m_in.text.IsNullOrWhiteSpace())
            {
                return new string[0];
            }

            T prefab = GetCurrentTargetPrefab();
            return (PropSwitcherMod.Controller.PropsLoaded.ContainsKey(m_in.text) ? PropSwitcherMod.Controller.PropsLoaded : PropSwitcherMod.Controller.TreesLoaded)
                //.Where(x => (!PSPropData.Instance.PrefabChildEntries.ContainsKey(prefab.name) || !PSPropData.Instance.PrefabChildEntries[prefab.name].ContainsKey(x.Value)))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        internal abstract bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, string> x);
        private string GetCurrentValueIn()
        {
            m_rotationOffset.parent.isVisible = false;
            return "";
        }

        private string OnChangeValueIn(int arg1, string[] arg2)
        {
            m_out.text = "";
            m_rotationOffset.text = "0.000";
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                m_rotationOffset.parent.isVisible = PropSwitcherMod.Controller.PropsLoaded.ContainsKey(arg2[arg1]);
                return arg2[arg1];
            }
            else
            {
                m_rotationOffset.parent.isVisible = false;
                return "";
            }
        }

        private string[] OnChangeFilterIn(string arg)
        {
            T prefab = GetCurrentTargetPrefab();
            return PropSwitcherMod.Controller.PropsLoaded
            .Union(PropSwitcherMod.Controller.TreesLoaded)
                .Where(x => IsPropAvailableOnCurrentPrefab(x))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        public SimpleXmlDictionary<string, SwitchInfo> TargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries.TryGetValue(prefabName, out SimpleXmlDictionary<string, SwitchInfo> result) ? result : null;
        public SimpleXmlDictionary<string, SwitchInfo> CreateTargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries[prefabName] = new SimpleXmlDictionary<string, SwitchInfo>();
        public void SetCurrentLoadedData(string fromSource, SwitchInfo info) => SetCurrentLoadedData(fromSource, info, null);
        public void SetCurrentLoadedData(string fromSource, SwitchInfo info, string target)
        {
            m_seedSource.isChecked = info.SeedSource == RandomizerSeedSource.INSTANCE;
            m_in.text = PropSwitcherMod.Controller.PropsLoaded.Union(PropSwitcherMod.Controller.TreesLoaded).Where(y => fromSource == y.Value).FirstOrDefault().Key ?? fromSource ?? "";
            var targetSwitch = info.SwitchItems.Where(x => x.TargetPrefab == target).FirstOrDefault() ?? info.SwitchItems[0];
            m_out.text = PropSwitcherMod.Controller.PropsLoaded.Union(PropSwitcherMod.Controller.TreesLoaded).Where(y => targetSwitch.TargetPrefab == y.Value).FirstOrDefault().Key ?? targetSwitch.TargetPrefab ?? "";
            m_rotationOffset.text = targetSwitch.RotationOffset.ToString("F3");
            m_rotationOffset.parent.isVisible = PropSwitcherMod.Controller.PropsLoaded.ContainsKey(fromSource);
        }
    }

    public class PSBuildingPropTab : PSPrefabPropTab<BuildingInfo>
    {
        protected override Dictionary<string, BuildingInfo> PrefabsLoaded => PropSwitcherMod.Controller.BuildingsLoaded;

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
                    m_rotationOffset.text = "0.000";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.BuildingEditorToolInstance.enabled = true;
        }
        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, string> x) => GetCurrentTargetPrefab().m_props.Where(y => y.m_finalProp?.name == x.Value || y.m_finalTree?.name == x.Value).FirstOrDefault() != default;
    }
    public class PSNetPropTab : PSPrefabPropTab<NetInfo>
    {
        protected override Dictionary<string, NetInfo> PrefabsLoaded => PropSwitcherMod.Controller.NetsLoaded;

        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, string> x) => GetCurrentTargetPrefab().m_lanes?.SelectMany(y => y?.m_laneProps?.m_props ?? new NetLaneProps.Prop[0]).Where(y => y?.m_finalProp?.name == x.Value || y?.m_finalTree?.name == x.Value).FirstOrDefault() != default;
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
                    m_rotationOffset.text = "0.000";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
    }
}