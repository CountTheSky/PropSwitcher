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
    public abstract class PSPrefabPropTab<T> : PSPrefabTabParent where T : PrefabInfo
    {

        protected UITextField m_prefab;
        protected UIButton m_btnDelete;
        protected UIButton m_btnExport;
        protected abstract Dictionary<string, T> PrefabsLoaded { get; }

        private UIDropDown m_filterSource;

        #region Superclass Implementation

        protected override bool HasParentPrefab { get; } = true;
        protected override void AddActionButtons(UILabel reference)
        {
            m_btnDelete = AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", true, 30);
            m_btnExport = AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_PS_EXPORTASGLOBAL", true, 30);
            AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Reload, OnReloadFiles, "K45_PS_RELOADGLOBAL", true, 30);
        }
        protected override void PreMainForm(UIHelperExtension uiHelper)
        {
            base.PreMainForm(uiHelper);
            AddFilterableInput(Locale.Get("K45_PS_PARENTPREFAB", typeof(T).Name), uiHelper, out m_prefab, out UIListBox popup, OnChangeFilterParent, OnChangeParentPrefab, 100);
            m_prefab.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
            AddButtonInEditorRow(m_prefab, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Dropper, EnablePickTool, Locale.Get("K45_PS_ENABLETOOLPICKER"), true, 30).zOrder = m_prefab.zOrder + 1;
        }
        protected override void OnAddRule()
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
            base.OnAddRule();
        }
        protected override void DoWithFilterRow(float targetWidth, UIPanel panel, out UITextField filterIn, out UITextField fillterOut)
        {
            base.DoWithFilterRow(targetWidth, panel, out filterIn, out fillterOut);

            m_filterSource = UIHelperExtension.CloneBasicDropDownNoLabel(Enum.GetNames(typeof(SourceFilterOptions)).Select(x => Locale.Get("K45_PS_FILTERSOURCEITEM", x)).ToArray(), (x) => UpdateDetoursList(), panel);
            m_filterSource.area = new Vector4(0, 0, targetWidth * (COL_ACTIONS_PROPORTION + COL_OFFSETS_PROPORTION), 28);
            m_filterSource.textScale = 1;
            m_filterSource.zOrder = 2;

        }
        public override void UpdateDetoursList(Item targetItem)
        {
            var prefabName = GetCurrentParentPrefab()?.name ?? "";
            bool isEditable = !prefabName.IsNullOrWhiteSpace();
            m_in.parent.isVisible = isEditable;
            m_out.parent.isVisible = isEditable;
            m_rotationOffset.parent.isVisible = isEditable && IsProp(m_in.text);
            m_addButton.isVisible = isEditable;
            m_detourList.parent.isVisible = isEditable;
            m_btnDelete.isVisible = isEditable;
            m_btnExport.isVisible = isEditable;
            m_titleRow.isVisible = isEditable;
            m_filterRow.isVisible = isEditable;
            DoOnUpdateDetoursList(isEditable, targetItem);


            if (isEditable)
            {
                base.UpdateDetoursList(targetItem);
            }

        }

        protected override List<DetourListParameterContainer> GetFilterLists()
        {
            PSPropData.Instance.PrefabChildEntries.TryGetValue(GetCurrentParentPrefab()?.name ?? "", out XmlDictionary<PrefabChildEntryKey, SwitchInfo> currentEditingSelection);
            PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(GetCurrentParentPrefab()?.name ?? "", out XmlDictionary<PrefabChildEntryKey, SwitchInfo> globalCurrentEditingSelection);

            return new List<DetourListParameterContainer>()
            {
              new DetourListParameterContainer(currentEditingSelection,((x)=>m_filterSource.selectedIndex != (int)SourceFilterOptions.GLOBAL),false,null),
              new DetourListParameterContainer(globalCurrentEditingSelection,((x)=>m_filterSource.selectedIndex != (int)SourceFilterOptions.SAVEGAME && (m_filterSource.selectedIndex != (int)SourceFilterOptions.ACTIVE || currentEditingSelection.Count(z => z.Key == x.Key) == 0)),true, currentEditingSelection),
            };
        }


        public override XmlDictionary<PrefabChildEntryKey, SwitchInfo> TargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries.TryGetValue(prefabName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> result) ? result : null;
        public override XmlDictionary<PrefabChildEntryKey, SwitchInfo> CreateTargetDictionary(string prefabName) => PSPropData.Instance.PrefabChildEntries[prefabName] = new XmlDictionary<PrefabChildEntryKey, SwitchInfo>();
        public override void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info) => SetCurrentLoadedData(fromSource, info, null);
        public override void SetCurrentLoadedData(PrefabChildEntryKey fromSourceSrc, SwitchInfo info, string target)
        {
            m_selectedEntry = fromSourceSrc;
            var targetSwitch = info.SwitchItems.Where(x => x.TargetPrefab == target).FirstOrDefault() ?? info.SwitchItems[0];
            m_out.text = PropSwitcherMod.Controller.PropsLoaded.Union(PropSwitcherMod.Controller.TreesLoaded).Where(y => targetSwitch.TargetPrefab == y.Value.prefabName).FirstOrDefault().Key ?? targetSwitch.TargetPrefab ?? "";
            m_in.text = m_selectedEntry.ToString(GetCurrentParentPrefab());
            UpdateDetoursList(targetSwitch);
        }

        #endregion

        #region Action Buttons
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
            if (
                PSPropData.Instance.PrefabChildEntries.TryGetValue(m_prefab.name, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> currentEditingSelection)
                | PropSwitcherMod.Controller.GlobalPrefabChildEntries.TryGetValue(targetPrefabName, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> globalCurrentEditingSelection)
                )
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
        private void OnReloadFiles()
        {
            PropSwitcherMod.Controller.ReloadPropGlobals();
            UpdateDetoursList();
        }
        internal abstract void EnablePickTool();

        #endregion

        #region Current Data Keys
        protected override XmlDictionary<PrefabChildEntryKey, SwitchInfo> GetCurrentRuleList(bool createIfNotExists = false)
        {
            if (PSPropData.Instance.PrefabChildEntries.TryGetValue(m_prefab.text, out XmlDictionary<PrefabChildEntryKey, SwitchInfo> data) && data != null)
            {
                return data;
            }
            if (createIfNotExists && GetCurrentParentPrefab() is T parent && parent != null)
            {
                PSPropData.Instance.PrefabChildEntries[parent.name] = new XmlDictionary<PrefabChildEntryKey, SwitchInfo>();
                return PSPropData.Instance.PrefabChildEntries[parent.name];
            }
            return null;
        }

        protected override string GetCurrentInValue() => m_selectedEntry.FromContext(GetCurrentParentPrefab());
        protected abstract T GetCurrentParentPrefab();
        protected override PrefabInfo GetCurrentParentPrefabInfo() => GetCurrentParentPrefab();
        #endregion

        #region New prefab selectors callbacks
        private string OnChangeParentPrefab(string sel, int arg1, string[] arg2)
        {
            m_in.text = "";
            m_out.text = "";
            m_selectedEntry = null;
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
        private string[] OnChangeFilterParent(string arg) => PrefabsLoaded
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value.name + (PropIndexes.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        #endregion

        #region General Utility

        #endregion

        #region Inheritance Hooks
        protected virtual void DoOnUpdateDetoursList(bool isEditable, Item targetItem)
        {
            m_seedSource.isVisible = isEditable;
            m_rotationOffset.text = (targetItem?.RotationOffset ?? 0).ToString("0.##");
        }
        protected virtual void SetCurrentLoadedExtraData(string fromSource, SwitchInfo info, Item item)
        {
            m_seedSource.isChecked = info.SeedSource == RandomizerSeedSource.INSTANCE;
            m_rotationOffset.text = item.RotationOffset.ToString("0.#");
            m_rotationOffset.parent.isVisible = IsProp(fromSource);
        }
        #endregion

        private enum SourceFilterOptions
        {
            ALL,
            GLOBAL,
            SAVEGAME,
            ACTIVE
        }
    }
}