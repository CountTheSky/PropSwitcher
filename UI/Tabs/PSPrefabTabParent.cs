using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;
using static Klyte.PropSwitcher.PSController;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

namespace Klyte.PropSwitcher.UI
{
    public abstract class PSPrefabTabParent : UICustomControl
    {
        public const float COL_PROPTOREPLACE_PROPORTION = .35f;
        public const float COL_PROPREPLACEMENT_PROPORTION = .35f;
        public const float COL_OFFSETS_PROPORTION = .2f;
        public const float COL_ACTIONS_PROPORTION = .1f;

        public const float SUBCOL_TOTAL_PROPORTION = COL_PROPREPLACEMENT_PROPORTION + COL_OFFSETS_PROPORTION + COL_ACTIONS_PROPORTION;
        public const float SUBCOL_PROPREPLACEMENT_PROPORTION = COL_PROPREPLACEMENT_PROPORTION / SUBCOL_TOTAL_PROPORTION;
        public const float SUBCOL_OFFSETS_PROPORTION = COL_OFFSETS_PROPORTION / SUBCOL_TOTAL_PROPORTION;
        public const float SUBCOL_ACTIONS_PROPORTION = COL_ACTIONS_PROPORTION / SUBCOL_TOTAL_PROPORTION;

        protected UICheckBox m_seedSource;
        protected UITextField m_in;
        protected UITextField m_out;
        protected UIScrollablePanel m_detourList;
        protected UITemplateList<UIPanel> m_listItems;
        protected UIPanel m_actionBar;
        protected UITextField m_filterIn;
        protected UITextField m_filterOut;
        protected UITextField m_rotationOffset;
        protected UIPanel m_titleRow;
        protected UIPanel m_filterRow;
        protected UIButton m_addButton;

        protected PrefabChildEntryKey m_selectedEntry;
        protected abstract string TitleLocale { get; }
        protected abstract bool HasParentPrefab { get; }

        protected void Awake()
        {
            UIPanel windowPanel = GetComponent<UIPanel>();
            windowPanel.padding = new RectOffset(3,3, 0, 0);
            windowPanel.autoLayout = true;
            windowPanel.autoLayoutDirection = LayoutDirection.Vertical;
            windowPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            windowPanel.clipChildren = true;

            var formHeight = 270f;

            KlyteMonoUtils.CreateUIElement(out UIPanel layoutPanel, windowPanel.transform, "topFormPanel", new Vector4(0, 0, windowPanel.width, formHeight));
            layoutPanel.padding = new RectOffset(2, 2, 0, 0);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 3);
            layoutPanel.clipChildren = true;
            CreateTopForm(layoutPanel);

            KlyteMonoUtils.CreateUIElement(out UIPanel listPanel, windowPanel.transform, "listPanel", new Vector4(0, 0, windowPanel.width, windowPanel.height - formHeight - 40));
            listPanel.padding = new RectOffset(2, 2, 0, 0);
            listPanel.autoLayout = true;
            listPanel.autoLayoutDirection = LayoutDirection.Vertical;
            listPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 3);
            listPanel.clipChildren = true;
            CreateList(listPanel);

            UpdateDetoursList();
        }

        private void CreateTopForm(UIPanel layoutPanel)
        {
            KlyteMonoUtils.CreateUIElement(out m_actionBar, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, layoutPanel.width, 30));
            m_actionBar.autoLayout = true;
            m_actionBar.autoLayoutDirection = LayoutDirection.Vertical;
            m_actionBar.padding = new RectOffset(0, 0, 0, 2);
            m_actionBar.autoFitChildrenVertically = true;
            var m_topHelper = new UIHelperExtension(m_actionBar);

            AddLabel(Locale.Get(TitleLocale), m_topHelper, out UILabel m_labelSelectionDescription, out UIPanel m_containerSelectionDescription, false);
            m_labelSelectionDescription.size = new Vector2(m_containerSelectionDescription.width - m_containerSelectionDescription.padding.left - m_containerSelectionDescription.padding.right - 4, 30);
            m_labelSelectionDescription.padding.top = 8;

            AddActionButtons(m_labelSelectionDescription);

            var uiHelper = new UIHelperExtension(layoutPanel);

            PreMainForm(uiHelper);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, OnChangeValueIn,100);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, OnChangeValueOut,100);
            AddVector2Field(Locale.Get("K45_PS_ROTATIONOFFSET"), out UITextField[] m_rotationOffset, uiHelper, (x) => { });
            DoExtraInputOptions(uiHelper);

            this.m_rotationOffset = m_rotationOffset[0];
            Destroy(m_rotationOffset[1]);
            m_addButton = uiHelper.AddButton(Locale.Get("K45_PS_ADDREPLACEMENTRULE"), OnAddRule) as UIButton;
            this.m_rotationOffset.parent.isVisible = false;

            m_in.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
            m_out.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
        }
        private void CreateList(UIPanel layoutPanel)
        {
            float listContainerWidth = layoutPanel.width - 20;

            KlyteMonoUtils.CreateUIElement(out m_titleRow, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            m_titleRow.autoLayout = true;
            m_titleRow.wrapLayout = false;
            m_titleRow.autoLayoutDirection = LayoutDirection.Horizontal;

            KlyteMonoUtils.CreateUIElement(out m_filterRow, layoutPanel.transform, "filterRow", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            m_filterRow.autoLayout = true;
            m_filterRow.wrapLayout = false;
            m_filterRow.autoLayoutDirection = LayoutDirection.Horizontal;
            DoWithFilterRow(listContainerWidth, m_filterRow, out m_filterIn, out m_filterOut);

            CreateRowPlaceHolder(listContainerWidth, m_titleRow, out UILabel col1Title, out UILabel col2Title, out UILabel col3Title, out UILabel col4Title);
            KlyteMonoUtils.LimitWidthAndBox(col1Title, col1Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col2Title, col2Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col3Title, col3Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col4Title, col4Title.width, true);
            col1Title.text = Locale.Get("K45_PS_SWITCHFROM_TITLE");
            col2Title.text = Locale.Get("K45_PS_SWITCHTO_TITLE");
            col3Title.text = Locale.Get("K45_PS_ROTATION_TITLE");
            col4Title.text = Locale.Get("K45_PS_ACTIONS_TITLE");

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "listContainer", new UnityEngine.Vector4(0, 0, listContainerWidth, layoutPanel.height - 50));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, listContainerWidth, m_listContainer.height);
            m_detourList.backgroundSprite = "GenericPanel";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;
            PSSwitchEntry.CreateTemplateDetourItem(m_detourList.width);
            m_listItems = new UITemplateList<UIPanel>(m_detourList, PSSwitchEntry.DETOUR_ITEM_TEMPLATE);
        }

        protected abstract void AddActionButtons(UILabel reference);
        protected virtual void PreMainForm(UIHelperExtension uiHelper) { }
        protected virtual void DoExtraInputOptions(UIHelperExtension uiHelper) => AddCheckboxLocale("K45_PS_SAMESEEDFORBUILDINGNET", out m_seedSource, uiHelper, OnChangeSeedSource);
        protected void OnChangeSeedSource(bool newVal)
        {
            var currentEditingKey = GetCurrentEditingKey();
            var currentList = GetCurrentRuleList();
            if (currentEditingKey == null || (!currentList?.ContainsKey(GetCurrentEditingKey()) ?? false))
            {
                return;
            }
            currentList[currentEditingKey].SeedSource = m_seedSource.isChecked ? RandomizerSeedSource.INSTANCE : RandomizerSeedSource.POSITION;
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();
        }

        #region Prefab selectors callbacks
        protected virtual string[] OnChangeFilterOut(string arg) =>
            m_in.text.IsNullOrWhiteSpace()
                ? (new string[0])
                : (IsProp(m_in.text) ? PropSwitcherMod.Controller.PropsLoaded : PropSwitcherMod.Controller.TreesLoaded)
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        protected virtual string OnChangeValueOut(string currentVal, int arg1, string[] arg2) => arg1 >= 0 && arg1 < arg2.Length ? arg2[arg1] : "";
        protected virtual string[] OnChangeFilterIn(string arg) =>
            PropSwitcherMod.Controller.PropsLoaded
            .Union(PropSwitcherMod.Controller.TreesLoaded)
                .Where(x => IsPropAvailable(x))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : x.Value.MatchesTerm(arg))
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        protected virtual string OnChangeValueIn(string sel, int arg1, string[] arg2)
        {
            m_out.text = "";
            m_rotationOffset.text = "0";
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                DoOnChangeValueIn(arg2[arg1]);
                return arg2[arg1];
            }
            else
            {
                m_rotationOffset.parent.isVisible = false;
                return "";
            }
        }

        #endregion
        protected virtual void OnAddRule()
        {
            var currentEditingKey = GetCurrentEditingKey();
            var currentList = GetCurrentRuleList(true);

            var inputProp = GetCurrentInValue();
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
            if (!GetCurrentOutValue(out string targetValue))
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_INVALIDINPUTINOUT"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }
            if (!currentList.ContainsKey(currentEditingKey))
            {
                currentList[currentEditingKey] = new Xml.SwitchInfo();
            }
            var currentItem = currentList[currentEditingKey].Add(targetValue, float.TryParse(m_rotationOffset.text, out float offset) ? offset % 360 : 0);
            WriteExtraSettings(currentList[currentEditingKey], currentItem);
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList(currentItem);
        }

        protected void CreateRowPlaceHolder(float targetWidth, UIPanel panel, out UILabel column1, out UILabel column2, out UILabel column3, out UILabel actionsContainer)
        {
            KlyteMonoUtils.CreateUIElement(out column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * COL_PROPTOREPLACE_PROPORTION, 25));
            column1.minimumSize = new Vector2(0, 25);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column2, panel.transform, "ToLbl", new Vector4(0, 0, targetWidth * COL_PROPREPLACEMENT_PROPORTION, 25));
            column2.minimumSize = new Vector2(0, 25);
            column2.verticalAlignment = UIVerticalAlignment.Middle;
            column2.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column3, panel.transform, "RotLbl", new Vector4(0, 0, targetWidth * COL_OFFSETS_PROPORTION, 25));
            column3.minimumSize = new Vector2(0, 25);
            column3.verticalAlignment = UIVerticalAlignment.Middle;
            column3.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out actionsContainer, panel.transform, "ActionsPanel", new Vector4(0, 0, targetWidth * COL_ACTIONS_PROPORTION, 25));
            actionsContainer.minimumSize = new Vector2(0, 25);
            actionsContainer.verticalAlignment = UIVerticalAlignment.Middle;
            actionsContainer.textAlignment = UIHorizontalAlignment.Center;
        }

        protected virtual void DoWithFilterRow(float targetWidth, UIPanel panel, out UITextField filterIn, out UITextField fillterOut)
        {
            KlyteMonoUtils.CreateUIElement(out filterIn, panel.transform, "FromFld", new Vector4(0, 0, targetWidth * COL_PROPTOREPLACE_PROPORTION, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(filterIn);
            filterIn.minimumSize = new Vector2(0, 25);
            filterIn.verticalAlignment = UIVerticalAlignment.Middle;
            filterIn.eventTextChanged += (x, y) => UpdateDetoursList();
            filterIn.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");
            KlyteMonoUtils.CreateUIElement(out fillterOut, panel.transform, "ToFld", new Vector4(0, 0, targetWidth * COL_PROPREPLACEMENT_PROPORTION, 25));
            KlyteMonoUtils.UiTextFieldDefaultsForm(fillterOut);
            fillterOut.minimumSize = new Vector2(0, 25);
            fillterOut.verticalAlignment = UIVerticalAlignment.Middle;
            fillterOut.eventTextChanged += (x, y) => UpdateDetoursList();
            fillterOut.tooltip = Locale.Get("K45_PS_TYPETOFILTERTOOLTIP");
        }
        public void UpdateDetoursList() => UpdateDetoursList(GetCurrentEditingItem());
        public virtual void UpdateDetoursList(Item currentItem)
        {
            var parent = GetCurrentParentPrefabInfo();
            var entries = GetFilterLists().Select(w => Tuple.New(w,
                w.list?
                    .Where(x =>
                        w.itemAdditionalValidation(x)
                        && (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key.ToString(parent)))
                        && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.CachedProp?.GetUncheckedLocalizedTitle() ?? z.CachedTree?.GetUncheckedLocalizedTitle() ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER"))))
                     )
                    .Select(x => Tuple.New(x.Key, x.Value))
                    .OrderBy(x => x.First.ToString(parent))
                    .OrderBy(x => x.First.PrefabIdx < 0 ? 99999 : x.First.PrefabIdx)
                    .ToArray()
                 ?? new Tuple<PrefabChildEntryKey, SwitchInfo>[0]
                ));

            var rows = m_listItems.SetItemCount(entries.Sum(x => x.Second.Length));
            var counter = 0;
            foreach (var entry in entries)
            {
                BuildItems(ref rows, entry.Second, counter, entry.First.differColor, entry.First.localList);
                counter += entry.Second.Length;
            }
        }

        protected abstract List<DetourListParameterContainer> GetFilterLists();

        public abstract XmlDictionary<PrefabChildEntryKey, SwitchInfo> TargetDictionary(string prefabName);
        public abstract XmlDictionary<PrefabChildEntryKey, SwitchInfo> CreateTargetDictionary(string prefabName);
        public abstract void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info);
        public abstract void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info, string target);

        #region Current Data Keys
        protected abstract XmlDictionary<PrefabChildEntryKey, SwitchInfo> GetCurrentRuleList(bool createIfNotExists = false);
        protected virtual PrefabInfo GetCurrentParentPrefabInfo() => null;
        protected Item GetCurrentEditingItem()
        {
            SwitchInfo info = null;
            return GetCurrentEditingKey() != null
                    && (GetCurrentRuleList()?.TryGetValue(GetCurrentEditingKey(), out info) ?? false)
                    && GetCurrentOutValue(out string outVal)
                    ? info?.Get(outVal)
                    : null;
        }
        protected bool GetCurrentOutValue(out string value)
        {
            var couldFind = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_out.text, out TextSearchEntry outText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_out.text, out outText);
            value = !m_out.text.IsNullOrWhiteSpace() && couldFind ? outText.prefabName : null;
            return m_out.text.IsNullOrWhiteSpace() || couldFind;
        }
        protected abstract string GetCurrentInValue();
        protected virtual PrefabChildEntryKey GetCurrentEditingKey() => m_selectedEntry;
        #endregion

        #region General Utility
        protected static bool CheckIfPrefabMatchesFilter(string filter, string prefabName) => LocaleManager.cultureInfo.CompareInfo.IndexOf(prefabName ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER"), filter, CompareOptions.IgnoreCase) >= 0;
        protected virtual bool IsProp(string v) => !v.IsNullOrWhiteSpace() && PropSwitcherMod.Controller.PropsLoaded.ContainsKey(v);
        protected virtual PrefabChildEntryKey GetEntryFor(string v) =>
         new PrefabChildEntryKey(
             PropSwitcherMod.Controller.PropsLoaded
             .Union(PropSwitcherMod.Controller.TreesLoaded)
             .Where(x => x.Key == v).FirstOrDefault().Value.prefabName
             );
        protected void BuildItems(ref UIPanel[] rows, Tuple<PrefabChildEntryKey, SwitchInfo>[] keyList, int offset, bool isGlobal, Dictionary<PrefabChildEntryKey, SwitchInfo> localList = null)
        {
            var parentPrefab = GetCurrentParentPrefabInfo();
            for (int i = offset; i < offset + keyList.Length; i++)
            {
                var currentData = keyList[i - offset];
                var targetTextColor = isGlobal ? (localList?.Count(x => x.Key == currentData.First) ?? 0) == 0 ? Color.green : Color.red : Color.white;
                rows[i].GetComponent<PSSwitchEntry>().SetData(parentPrefab?.name, parentPrefab, currentData.First, currentData.Second, targetTextColor, isGlobal);
            }
        }
        #endregion

        #region Inheritance Hooks
        protected virtual void DoOnChangeValueIn(string v)
        {
            m_selectedEntry = GetEntryFor(v);
            m_rotationOffset.parent.isVisible = IsProp(v);
        }
        internal abstract bool IsPropAvailable(KeyValuePair<string, TextSearchEntry> x);
        protected virtual void WriteExtraSettings(SwitchInfo switchInfo, Item currentItem) => switchInfo.SeedSource = m_seedSource.isChecked ? RandomizerSeedSource.INSTANCE : RandomizerSeedSource.POSITION;
        #endregion


        protected class DetourListParameterContainer
        {
            public XmlDictionary<PrefabChildEntryKey, SwitchInfo> list;
            public Func<KeyValuePair<PrefabChildEntryKey, SwitchInfo>, bool> itemAdditionalValidation;
            public bool differColor;
            public XmlDictionary<PrefabChildEntryKey, SwitchInfo> localList;

            public DetourListParameterContainer(XmlDictionary<PrefabChildEntryKey, SwitchInfo> list, Func<KeyValuePair<PrefabChildEntryKey, SwitchInfo>, bool> itemAdditionalValidation, bool differColor, XmlDictionary<PrefabChildEntryKey, SwitchInfo> localList)
            {
                this.list = list;
                this.itemAdditionalValidation = itemAdditionalValidation;
                this.differColor = differColor;
                this.localList = localList;
            }
        }
    }
}