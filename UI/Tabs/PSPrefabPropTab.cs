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
using static Klyte.PropSwitcher.PSController;
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
        protected PrefabChildEntryKey m_selectedEntry;

        protected abstract string TitleLocale { get; }


        protected void Awake()
        {
            UIPanel layoutPanel = GetComponent<UIPanel>();
            layoutPanel.padding = new RectOffset(8, 8, 0, 0);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 3);
            layoutPanel.clipChildren = true;
            var uiHelper = new UIHelperExtension(layoutPanel);

            KlyteMonoUtils.CreateUIElement(out m_actionBar, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, layoutPanel.width, 30));
            m_actionBar.autoLayout = true;
            m_actionBar.autoLayoutDirection = LayoutDirection.Vertical;
            m_actionBar.padding = new RectOffset(0, 0, 0, 2);
            m_actionBar.autoFitChildrenVertically = true;
            var m_topHelper = new UIHelperExtension(m_actionBar);

            AddLabel(Locale.Get(TitleLocale), m_topHelper, out UILabel m_labelSelectionDescription, out UIPanel m_containerSelectionDescription, false);
            m_labelSelectionDescription.size = new Vector2(m_containerSelectionDescription.width - m_containerSelectionDescription.padding.left - m_containerSelectionDescription.padding.right - 4, 30);
            m_labelSelectionDescription.padding.top = 8;
            m_btnDelete = AddButtonInEditorRow(m_labelSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", true, 30);
            m_btnExport = AddButtonInEditorRow(m_labelSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_PS_EXPORTASGLOBAL", true, 30);
            AddButtonInEditorRow(m_labelSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadFiles, "K45_PS_RELOADGLOBAL", true, 30);

            AddFilterableInput(Locale.Get("K45_PS_PARENTPREFAB", typeof(T).Name), uiHelper, out m_prefab, out UIListBox popup, OnChangeFilterPrefab, OnChangeValuePrefab);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, OnChangeValueIn);


            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, OnChangeValueOut);
            AddVector2Field(Locale.Get("K45_PS_ROTATIONOFFSET"), out UITextField[] m_rotationOffset, uiHelper, (x) => { });
            DoExtraInputOptions(uiHelper);
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
            KlyteMonoUtils.CreateUIElement(out filterIn, panel.transform, "FromFld", new Vector4(0, 0, targetWidth * 0.33f, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(filterIn);
            filterIn.minimumSize = new Vector2(0, 25);
            filterIn.verticalAlignment = UIVerticalAlignment.Middle;
            filterIn.eventTextChanged += (x, y) => UpdateDetoursList();
            filterIn.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");
            KlyteMonoUtils.CreateUIElement(out fillterOut, panel.transform, "ToFld", new Vector4(0, 0, targetWidth * 0.33f, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(fillterOut);
            fillterOut.minimumSize = new Vector2(0, 25);
            fillterOut.verticalAlignment = UIVerticalAlignment.Middle;
            fillterOut.eventTextChanged += (x, y) => UpdateDetoursList();
            fillterOut.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");

            filterSource = UIHelperExtension.CloneBasicDropDownNoLabel(Enum.GetNames(typeof(SourceFilterOptions)).Select(x => Locale.Get("K45_PS_FILTERSOURCEITEM", x)).ToArray(), (x) => UpdateDetoursList(), panel);
            filterSource.area = new Vector4(0, 0, targetWidth * 0.34f, 28);
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

        private string OnChangeValuePrefab(string sel, int arg1, string[] arg2)
        {
            m_in.text = "";
            m_out.text = "";
            m_rotationOffset.text = "0.000";
            m_rotationOffset.parent.isVisible = false;
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                m_selectedEntry = GetEntryFor(arg2[arg1]);
                return arg2[arg1];
            }
            else
            {
                m_selectedEntry = null;
                return "";
            }
        }

        private string[] OnChangeFilterPrefab(string arg) => PrefabsLoaded
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value.name + (PropIndexes.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();

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
            var targetPrefabName = GetCurrentParentPrefab()?.name ?? "";
            if (PSPropData.Instance.PrefabChildEntries.TryGetValue(m_prefab.name, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> currentEditingSelection) | PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(targetPrefabName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> globalCurrentEditingSelection))
            {
                var result = globalCurrentEditingSelection ?? new XmlDictionary<PrefabChildEntryKey, SwitchInfo>();
                foreach (var entry in currentEditingSelection)
                {
                    result[entry.Key] = entry.Value;
                }
                var targetFilename = Path.Combine(PSController.DefaultGlobalPropConfigurationFolder, $"{targetPrefabName}.xml");
                File.WriteAllText(targetFilename, XmlUtils.DefaultXmlSerialize(new ILibableAsContainer<PrefabChildEntryKey, SwitchInfo>
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

        protected abstract T GetCurrentParentPrefab();

        private static void CreateRowPlaceHolder(float targetWidth, UIPanel panel, out UILabel column1, out UILabel column2, out UILabel column3, out UILabel actionsContainer)
        {
            KlyteMonoUtils.CreateUIElement(out column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * 0.33f, 25));
            column1.minimumSize = new Vector2(0, 25);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column2, panel.transform, "ToLbl", new Vector4(0, 0, targetWidth * 0.33f, 25));
            column2.minimumSize = new Vector2(0, 25);
            column2.verticalAlignment = UIVerticalAlignment.Middle;
            column2.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column3, panel.transform, "RotLbl", new Vector4(0, 0, targetWidth * 0.16f, 25));
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
            var prefabName = GetCurrentParentPrefab()?.name;
            bool isEditable = !prefabName.IsNullOrWhiteSpace();
            m_in.parent.isVisible = isEditable;
            m_out.parent.isVisible = isEditable;
            m_rotationOffset.parent.isVisible = isEditable && PropSwitcherMod.Controller.PropsLoaded.ContainsKey(m_in.text);
            m_addButton.isVisible = isEditable;
            m_detourList.parent.isVisible = isEditable;
            m_btnDelete.isVisible = isEditable;
            m_btnExport.isVisible = isEditable;
            m_titleRow.isVisible = isEditable;
            m_filterRow.isVisible = isEditable;
            DoOnUpdateDetoursList(isEditable);


            if (isEditable)
            {
                PSPropData.Instance.PrefabChildEntries.TryGetValue(prefabName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> currentEditingSelection);
                PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(GetCurrentParentPrefab()?.name ?? "", out XmlDictionary<PrefabChildEntryKey, SwitchInfo> globalCurrentEditingSelection);
                var keyListLocal = currentEditingSelection?.Where(x =>
                            m_filterSource.selectedIndex != (int)SourceFilterOptions.GLOBAL
                         && (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key.ToString(GetCurrentParentPrefab())))
                         && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.CachedProp?.GetUncheckedLocalizedTitle() ?? z.CachedTree?.GetUncheckedLocalizedTitle()))))
                    .Select(x => Tuple.New(x.Key, x.Value)).OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x.First.FromContext(GetCurrentParentPrefab()) == y.Value.prefabName).FirstOrDefault().Key ?? x.First.FromContext(GetCurrentParentPrefab())).ToArray() ?? new Tuple<PrefabChildEntryKey, SwitchInfo>[0];
                var keyListGlobal = globalCurrentEditingSelection?.Where(x =>
                            m_filterSource.selectedIndex != (int)SourceFilterOptions.SAVEGAME
                         && (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key.ToString(GetCurrentParentPrefab())))
                         && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.CachedProp?.GetUncheckedLocalizedTitle() ?? z.CachedTree?.GetUncheckedLocalizedTitle())))
                         && (m_filterSource.selectedIndex != (int)SourceFilterOptions.ACTIVE || keyListLocal.Count(y => y.First == x.Key) == 0)
                         )
                    .Select(x => Tuple.New(x.Key, x.Value)).OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x.First.FromContext(GetCurrentParentPrefab()) == y.Value.prefabName).FirstOrDefault().Key ?? x.First.FromContext(GetCurrentParentPrefab())).ToArray() ?? new Tuple<PrefabChildEntryKey, SwitchInfo>[0];
                var rows = m_listItems.SetItemCount(keyListLocal.Length + keyListGlobal.Length);
                BuildItems(ref rows, keyListGlobal, 0, true, currentEditingSelection);
                BuildItems(ref rows, keyListLocal, keyListGlobal.Length, false);
            }

        }

        private static bool CheckIfPrefabMatchesFilter(string filter, string prefabName) => LocaleManager.cultureInfo.CompareInfo.IndexOf(prefabName, filter, CompareOptions.IgnoreCase) >= 0;


        private void BuildItems(ref UIPanel[] rows, Tuple<PrefabChildEntryKey, SwitchInfo>[] keyList, int offset, bool isGlobal, Dictionary<PrefabChildEntryKey, SwitchInfo> localList = null)
        {
            var parentPrefab = GetCurrentParentPrefab();
            for (int i = offset; i < offset + keyList.Length; i++)
            {
                var currentData = keyList[i - offset];
                var targetTextColor = isGlobal ? (localList?.Count(x => x.Key == currentData.First) ?? 0) == 0 ? Color.green : Color.red : Color.white;

                rows[i].GetComponent<PSSwitchEntry>().SetData(parentPrefab.name, parentPrefab, currentData.First, currentData.Second, targetTextColor, isGlobal);
            }
        }

        private void OnAddRule()
        {
            if ((GetCurrentParentPrefab()?.name).IsNullOrWhiteSpace())
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_INVALIDPARENTPREFABSELECTION"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }
            var currentEditingKey = GetCurrentEditingKey();
            var currentParent = GetCurrentParentPrefab().name;
            if (!PSPropData.Instance.PrefabChildEntries.ContainsKey(currentParent) || PSPropData.Instance.PrefabChildEntries[currentParent] == null)
            {
                PSPropData.Instance.PrefabChildEntries[currentParent] = new XmlDictionary<PrefabChildEntryKey, SwitchInfo>();
            }

            var inputProp = currentEditingKey.FromContext(GetCurrentParentPrefab());
            if (inputProp.IsNullOrWhiteSpace())
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_INVALIDINPUTINOUT"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }
            _ = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_out.text, out TextSearchEntry outText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_out.text, out outText);
            if (!PSPropData.Instance.PrefabChildEntries[currentParent].ContainsKey(currentEditingKey))
            {
                PSPropData.Instance.PrefabChildEntries[currentParent][currentEditingKey] = new Xml.SwitchInfo();
            }

            PSPropData.Instance.PrefabChildEntries[currentParent][currentEditingKey].Add(m_out.text.IsNullOrWhiteSpace() ? null : outText.prefabName, float.TryParse(m_rotationOffset.text, out float offset) ? offset % 360 : 0);
            WriteExtraSettings(PSPropData.Instance.PrefabChildEntries[currentParent][currentEditingKey]);
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();
        }

        private void OnChangeSeedSource(bool newVal)
        {
            var currentEditingKey = GetCurrentEditingKey();
            var currentParent = GetCurrentParentPrefab().name;
            if (!PSPropData.Instance.PrefabChildEntries[currentParent].ContainsKey(currentEditingKey))
            {
                return;
            }
            PSPropData.Instance.PrefabChildEntries[currentParent][currentEditingKey].SeedSource = m_seedSource.isChecked ? RandomizerSeedSource.INSTANCE : RandomizerSeedSource.POSITION;
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();
        }

        private string OnChangeValueOut(string sel, int arg1, string[] arg2) => arg1 >= 0 && arg1 < arg2.Length ? arg2[arg1] : "";
        private string[] OnChangeFilterOut(string arg)
        {
            if (m_in.text.IsNullOrWhiteSpace())
            {
                return new string[0];
            }

            T prefab = GetCurrentParentPrefab();
            return (IsProp(m_in.text) ? PropSwitcherMod.Controller.PropsLoaded : PropSwitcherMod.Controller.TreesLoaded)
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        internal abstract bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, TextSearchEntry> x);

        private string OnChangeValueIn(string sel, int arg1, string[] arg2)
        {
            m_out.text = "";
            m_rotationOffset.text = "0.000";
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                m_rotationOffset.parent.isVisible = IsProp(arg2[arg1]);
                m_selectedEntry = GetEntryFor(arg2[arg1]);
                return arg2[arg1];
            }
            else
            {
                m_rotationOffset.parent.isVisible = false;
                return "";
            }
        }

        protected virtual bool IsProp(string v) => PropSwitcherMod.Controller.PropsLoaded.ContainsKey(v);

        protected virtual string[] OnChangeFilterIn(string arg)
        {
            T prefab = GetCurrentParentPrefab();
            return PropSwitcherMod.Controller.PropsLoaded
            .Union(PropSwitcherMod.Controller.TreesLoaded)
                .Where(x => IsPropAvailableOnCurrentPrefab(x))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        protected virtual PrefabChildEntryKey GetEntryFor(string v) =>
            new PrefabChildEntryKey(
                PropSwitcherMod.Controller.PropsLoaded
                .Union(PropSwitcherMod.Controller.TreesLoaded)
                .Where(x => x.Key == v).FirstOrDefault().Value.prefabName
                );


        public XmlDictionary<PrefabChildEntryKey, SwitchInfo> TargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries.TryGetValue(prefabName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> result) ? result : null;
        public XmlDictionary<PrefabChildEntryKey, SwitchInfo> CreateTargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries[prefabName] = new XmlDictionary<PrefabChildEntryKey, SwitchInfo>();
        public void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info) => SetCurrentLoadedData(fromSource, info, null);
        public void SetCurrentLoadedData(PrefabChildEntryKey fromSourceSrc, SwitchInfo info, string target)
        {
            m_selectedEntry = fromSourceSrc;
            var fromSource = m_selectedEntry.FromContext(GetCurrentParentPrefab());
            m_in.text = m_selectedEntry.ToString(GetCurrentParentPrefab());
            var targetSwitch = info.SwitchItems.Where(x => x.TargetPrefab == target).FirstOrDefault() ?? info.SwitchItems[0];
            m_out.text = PropSwitcherMod.Controller.PropsLoaded.Union(PropSwitcherMod.Controller.TreesLoaded).Where(y => targetSwitch.TargetPrefab == y.Value.prefabName).FirstOrDefault().Key ?? targetSwitch.TargetPrefab ?? "";
            m_rotationOffset.text = targetSwitch.RotationOffset.ToString("F3");
            m_rotationOffset.parent.isVisible = PropSwitcherMod.Controller.PropsLoaded.ContainsKey(fromSource);
            SetCurrentLoadedExtraData(fromSource, info);
        }

        protected virtual void DoOnUpdateDetoursList(bool isEditable) => m_seedSource.isVisible = isEditable;
        protected virtual void DoExtraInputOptions(UIHelperExtension uiHelper) => AddCheckboxLocale("K45_PS_SAMESEEDFORBUILDINGNET", out m_seedSource, uiHelper, OnChangeSeedSource);
        protected virtual void WriteExtraSettings(SwitchInfo item) => item.SeedSource = m_seedSource.isChecked ? RandomizerSeedSource.INSTANCE : RandomizerSeedSource.POSITION;
        protected virtual void SetCurrentLoadedExtraData(string fromSource, SwitchInfo info) => m_seedSource.isChecked = info.SeedSource == RandomizerSeedSource.INSTANCE;

        protected PrefabChildEntryKey GetCurrentEditingKey() => m_selectedEntry;
    }

    public class PSBuildingPropTab : PSPrefabPropTab<BuildingInfo>
    {
        public PSBuildingPropTab Instance { get; private set; }

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
        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, TextSearchEntry> x) => GetCurrentParentPrefab().m_props.Union(GetCurrentParentPrefab().m_subBuildings?.SelectMany(x => x.m_buildingInfo.m_props) ?? new BuildingInfo.Prop[0]).Any(y => y?.m_finalProp?.name == x.Value.prefabName || y?.m_finalTree?.name == x.Value.prefabName);

        protected override string[] OnChangeFilterIn(string arg)
        {
            BuildingInfo prefab = GetCurrentParentPrefab();
            if (cachedInfo != prefab.name)
            {
                currentReplacementSpotList = prefab.m_props.Select((x, i) => Tuple.New(i, x.m_position, new PrefabChildEntryKey(i).ToString(prefab), (PrefabInfo)x.m_finalProp ?? x.m_finalTree)).ToDictionary(x => x.Third, x => Tuple.New(x.First, x.Second, x.Fourth));
            }
            return PropSwitcherMod.Controller.PropsLoaded
                    .Union(PropSwitcherMod.Controller.TreesLoaded)
                    .Where(x => IsPropAvailableOnCurrentPrefab(x))
                    .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                    .Select(x => x.Key)
                    .OrderBy((x) => x)
                .Union(
                    currentReplacementSpotList.Keys.Where(x => LocaleManager.cultureInfo.CompareInfo.IndexOf(x, arg, CompareOptions.IgnoreCase) >= 0)
                )
                .ToArray();
        }
        protected override PrefabChildEntryKey GetEntryFor(string v) =>
            v.StartsWith("Prop #") && currentReplacementSpotList.TryGetValue(v, out Tuple<int, Vector3, PrefabInfo> item)
                ? new PrefabChildEntryKey(item.First)
                : base.GetEntryFor(v);

        protected override bool IsProp(string v)
        {
            if (v.StartsWith("Prop #"))
            {
                if (currentReplacementSpotList.TryGetValue(v, out Tuple<int, Vector3, PrefabInfo> prop))
                {
                    return prop.Third is PropInfo;
                }
            }
            return base.IsProp(v);
        }

        protected override BuildingInfo GetCurrentParentPrefab() => PropSwitcherMod.Controller.BuildingsLoaded.TryGetValue(m_prefab.text, out BuildingInfo info1) ? info1 : null;
    }
    public class PSNetPropTab : PSPrefabPropTab<NetInfo>
    {
        protected override Dictionary<string, NetInfo> PrefabsLoaded => PropSwitcherMod.Controller.NetsLoaded;

        protected override string TitleLocale => "K45_PS_NETPROP_EDITOR";
        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, TextSearchEntry> x) => GetCurrentParentPrefab().m_lanes?.SelectMany(y => y?.m_laneProps?.m_props ?? new NetLaneProps.Prop[0]).Where(y => y?.m_finalProp?.name == x.Value.prefabName || y?.m_finalTree?.name == x.Value.prefabName).FirstOrDefault() != default;
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