using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Libraries;
using Klyte.PropSwitcher.Overrides;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Linq;
using static Klyte.Commons.UI.DefaultEditorUILib;
using static Klyte.PropSwitcher.PSController;
using static Klyte.PropSwitcher.Xml.SwitchInfo;

namespace Klyte.PropSwitcher.UI
{
    public class PSGlobalPropTab : PSPrefabTabParent
    {
        protected UIButton m_btnImport;
        protected UIButton m_btnDelete;
        protected UIButton m_btnExport;

        #region Superclass Implementation
        protected override bool HasParentPrefab { get; } = false;
        protected override string TitleLocale => "K45_PS_GLOBAL_EDITOR";
        protected override void AddActionButtons(UILabel reference)
        {
            m_btnDelete = AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", true, 30);
            m_btnImport = AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Import, OnImportData, "K45_PS_IMPORTFROMLIB", true, 30);
            m_btnExport = AddButtonInEditorRow(reference, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, () => OnExportData(), "K45_PS_EXPORTTOLIB", true, 30);
        }
        protected override List<DetourListParameterContainer> GetFilterLists() => new List<DetourListParameterContainer>()
        {
            new DetourListParameterContainer(PSPropData.Instance.Entries,(x)=>true,false,null)
        };
        public override XmlDictionary<PrefabChildEntryKey, SwitchInfo> TargetDictionary(string prefabName) => PSPropData.Instance.Entries;
        public override XmlDictionary<PrefabChildEntryKey, SwitchInfo> CreateTargetDictionary(string prefabName) => TargetDictionary(prefabName);
        public override void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info) => SetCurrentLoadedData(fromSource, info, null);
        public override void SetCurrentLoadedData(PrefabChildEntryKey fromSourceSrc, SwitchInfo info, string target)
        {
            m_selectedEntry = fromSourceSrc;
            var fromSource = fromSourceSrc.SourcePrefab;
            m_in.text = PropSwitcherMod.Controller.PropsLoaded?.Union(PropSwitcherMod.Controller.TreesLoaded)?.Where(y => fromSource == y.Value.prefabName).FirstOrDefault().Key ?? fromSource ?? "";
            var targetSwitch = info.SwitchItems.Where(x => x.TargetPrefab == target).FirstOrDefault() ?? info.SwitchItems[0];
            m_out.text = PropSwitcherMod.Controller.PropsLoaded?.Union(PropSwitcherMod.Controller.TreesLoaded)?.Where(y => targetSwitch.TargetPrefab == y.Value.prefabName).FirstOrDefault().Key ?? targetSwitch.TargetPrefab ?? "";
            m_rotationOffset.text = targetSwitch.RotationOffset.ToString("F3");
            m_seedSource.isChecked = info.SeedSource == RandomizerSeedSource.INSTANCE;
            m_rotationOffset.parent.isVisible = IsProp(m_in.text);

            ForceUpdate();
            UpdateDetoursList(targetSwitch);
        }

        #endregion

        #region Action Buttons
        private void OnClearList()
        {
            PSPropData.Instance.Entries.Clear();

            PSOverrideCommons.Instance.RecalculateProps();
            ForceUpdate();
        }
        private void OnExportData(string defaultText = null) => K45DialogControl.ShowModalPromptText(new K45DialogControl.BindProperties
        {
            defaultTextFieldContent = defaultText,
            message = Locale.Get("K45_PS_TYPESAVENAMEFORLIST"),
            showButton1 = true,
            textButton1 = Locale.Get("SAVE"),
            showButton2 = true,
            textButton2 = Locale.Get("CANCEL"),
        }, (ret, text) =>
        {
            if (ret == 1)
            {
                if (text.IsNullOrWhiteSpace())
                {
                    K45DialogControl.UpdateCurrentMessage($"<color #FFFF00>{Locale.Get("K45_PS_INVALIDNAME")}</color>\n\n{Locale.Get("K45_PS_TYPESAVENAMEFORLIST")}");
                    return false;
                }
                PSLibPropSettings.Reload();
                var currentData = PSLibPropSettings.Instance.Get(text);
                if (currentData == null)
                {
                    AddCurrentListToLibrary(text);
                }
                else
                {
                    K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                    {
                        message = string.Format(Locale.Get("K45_PS_CONFIRMOVERWRITE"), text),
                        showButton1 = true,
                        textButton1 = Locale.Get("YES"),
                        showButton2 = true,
                        textButton2 = Locale.Get("NO"),
                    }, (x) =>
                    {
                        if (x == 1)
                        {
                            AddCurrentListToLibrary(text);
                        }
                        else
                        {
                            OnExportData(text);
                        }
                        return true;
                    });
                }
            }
            return true;
        });
        private static void AddCurrentListToLibrary(string text)
        {
            PSLibPropSettings.Reload();
            var newItem = new ILibableAsContainer<PrefabChildEntryKey, SwitchInfo>
            {
                Data = PSPropData.Instance.Entries
            };
            PSLibPropSettings.Instance.Add(text, ref newItem);
            K45DialogControl.ShowModal(new K45DialogControl.BindProperties
            {
                message = string.Format(Locale.Get("K45_PS_SUCCESSEXPORTDATA"), PSLibPropSettings.Instance.DefaultXmlFileBaseFullPath),
                showButton1 = true,
                textButton1 = Locale.Get("EXCEPTION_OK"),
                showButton2 = true,
                textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
            }, (x) =>
            {
                if (x == 2)
                {
                    ColossalFramework.Utils.OpenInFileBrowser(PSLibPropSettings.Instance.DefaultXmlFileBaseFullPath);
                    return false;
                }
                return true;
            });
        }
        private void OnImportData()
        {
            PSLibPropSettings.Reload();
            string[] optionList = PSLibPropSettings.Instance.List().ToArray();
            if (optionList.Length > 0)
            {
                K45DialogControl.ShowModalPromptDropDown(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_SELECTCONFIGTOLOAD"),
                    showButton1 = true,
                    textButton1 = Locale.Get("LOAD"),
                    showButton2 = true,
                    textButton2 = Locale.Get("CANCEL"),
                }, optionList, 0, (ret, idx, selText) =>
                {
                    if (ret == 1)
                    {
                        var newConfig = PSLibPropSettings.Instance.Get(selText);
                        PSPropData.Instance.Entries = newConfig.Data;

                        PSOverrideCommons.Instance.RecalculateProps();
                        ForceUpdate();
                    }
                    return true;
                });
            }
            else
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = Locale.Get("K45_PS_EMPTYRULESLISTWARNING"),
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK"),
                    showButton2 = true,
                    textButton2 = Locale.Get("K45_CMNS_GOTO_FILELOC"),
                }, (x) =>
                {
                    if (x == 2)
                    {
                        PSLibPropSettings.Instance.EnsureFileExists();
                        ColossalFramework.Utils.OpenInFileBrowser(PSLibPropSettings.Instance.DefaultXmlFileBaseFullPath);
                        return false;
                    }
                    return true;
                });
            }

        }

        #endregion

        #region Current Data Keys
        protected override XmlDictionary<PrefabChildEntryKey, SwitchInfo> GetCurrentRuleList(bool createIfNotExists = false) => PSPropData.Instance.Entries;
        protected override string GetCurrentInValue() => m_selectedEntry?.SourcePrefab;
        #endregion


        #region Inheritance Hooks
        internal override bool IsPropAvailable(KeyValuePair<string, TextSearchEntry> x) => PSPropData.Instance.Entries.Values.Where(y => y.SwitchItems.Any(z => z.TargetPrefab == x.Value.prefabName)).Count() == 0;

        #endregion


    }
}