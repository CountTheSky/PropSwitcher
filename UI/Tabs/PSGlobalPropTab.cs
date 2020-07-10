using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Libraries;
using Klyte.PropSwitcher.Xml;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.PropSwitcher.UI
{
    public class PSGlobalPropTab : UICustomControl, IPSBaseTab
    {
        private UITextField m_in;
        private UITextField m_out;
        private UIScrollablePanel m_detourList;
        private UITemplateList<UIPanel> m_listItems;

        private UIPanel m_actionBar;
        private UITextField m_filterIn;
        private UITextField m_filterOut;
        private UITextField m_rotationOffset;

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
            var m_btnDelete = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", false);
            var m_btnImport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Import, OnImportData, "K45_PS_IMPORTFROMLIB", false);
            var m_btnExport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, () => OnExportData(), "K45_PS_EXPORTTOLIB", false);

            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            AddVector2Field(Locale.Get("K45_PS_ROTATIONOFFSET"), out UITextField[] m_rotationOffset, uiHelper, (x) => { });
            this.m_rotationOffset = m_rotationOffset[0];
            Destroy(m_rotationOffset[1]);
            uiHelper.AddButton(Locale.Get("K45_PS_ADDREPLACEMENTRULE"), OnAddRule);
            m_in.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";
            m_out.tooltipLocaleID = "K45_PS_FIELDSFILTERINFORMATION";

            float listContainerWidth = layoutPanel.width - 20;
            KlyteMonoUtils.CreateUIElement(out UIPanel titleRow, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            titleRow.autoLayout = true;
            titleRow.wrapLayout = false;
            titleRow.autoLayoutDirection = LayoutDirection.Horizontal;

            CreateRowPlaceHolder(listContainerWidth - 20, titleRow, out UILabel col1Title, out UILabel col2Title, out UILabel col3Title, out UILabel col4Title);
            KlyteMonoUtils.LimitWidthAndBox(col1Title, col1Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col2Title, col2Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col3Title, col3Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col4Title, col4Title.width, true);
            col1Title.text = Locale.Get("K45_PS_SWITCHFROM_TITLE");
            col2Title.text = Locale.Get("K45_PS_SWITCHTO_TITLE");
            col3Title.text = Locale.Get("K45_PS_ROTATION_TITLE");
            col4Title.text = Locale.Get("K45_PS_ACTIONS_TITLE");

            KlyteMonoUtils.CreateUIElement(out UIPanel filterRow, layoutPanel.transform, "filterRow", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            filterRow.autoLayout = true;
            filterRow.wrapLayout = false;
            filterRow.autoLayoutDirection = LayoutDirection.Horizontal;
            CreateFilterPlaceHolder(listContainerWidth - 20, filterRow, out m_filterIn, out m_filterOut);

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "previewPanel", new UnityEngine.Vector4(0, 0, listContainerWidth, 420));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_detourList.backgroundSprite = "GenericPanel";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;
            PSSwitchEntry.CreateTemplateDetourItem(m_detourList.width);
            m_listItems = new UITemplateList<UIPanel>(m_detourList, PSSwitchEntry.DETOUR_ITEM_TEMPLATE);
            UpdateDetoursList();


        }

        private void OnClearList()
        {
            PSPropData.Instance.Entries = new SimpleXmlDictionary<string, SwitchInfo>();
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            UpdateDetoursList();
        }

        private void OnExportData(string defaultText = null)
        {
            K45DialogControl.ShowModalPromptText(new K45DialogControl.BindProperties
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

        }
        private static void AddCurrentListToLibrary(string text)
        {
            PSLibPropSettings.Reload();
            var newItem = new ILibableAsContainer<string, SwitchInfo>
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
                        for (int i = 0; i < 32; i++)
                        {
                            RenderManager.instance.UpdateGroups(i);
                        }
                        UpdateDetoursList();
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

        private void CreateFilterPlaceHolder(float targetWidth, UIPanel panel, out UITextField filterIn, out UITextField fillterOut)
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
        }



        public void UpdateDetoursList()
        {
            var keyList = PSPropData.Instance.Entries.Where(x =>
            (m_filterIn.text.IsNullOrWhiteSpace() || CheckIfPrefabMatchesFilter(m_filterIn.text, x.Key))
            && (m_filterOut.text.IsNullOrWhiteSpace() || x.Value.SwitchItems.Any(z => CheckIfPrefabMatchesFilter(m_filterOut.text, z.TargetPrefab)))
            ).OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x.Key == y.Value).FirstOrDefault().Key ?? x.Key).ToArray();
            UIPanel[] rows = m_listItems.SetItemCount(keyList.Length);
            for (int i = 0; i < keyList.Length; i++)
            {
                rows[i].GetComponent<PSSwitchEntry>().SetData(null, keyList[i].Key, keyList[i].Value, Color.white, false);
            }

        }


        private void OnAddRule()
        {
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

            _ = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_in.text, out string inText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_in.text, out inText);
            _ = PropSwitcherMod.Controller.PropsLoaded.TryGetValue(m_out.text, out string outText) || PropSwitcherMod.Controller.TreesLoaded.TryGetValue(m_out.text, out outText);

            if (!PSPropData.Instance.Entries.ContainsKey(inText))
            {
                PSPropData.Instance.Entries[inText] = new Xml.SwitchInfo();
            }
            PSPropData.Instance.Entries[inText].Add(m_out.text.IsNullOrWhiteSpace() ? null : outText, float.TryParse(m_rotationOffset.text, out float offset) ? offset % 360 : 0);

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
            return (PropSwitcherMod.Controller.PropsLoaded.ContainsKey(m_in.text) ? PropSwitcherMod.Controller.PropsLoaded : PropSwitcherMod.Controller.TreesLoaded)
                //.Where(x => !PSPropData.Instance.Entries.ContainsKey(x.Value))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : CheckIfPrefabMatchesFilter(arg, x.Value))
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        private static bool CheckIfPrefabMatchesFilter(string filter, string prefabName) => LocaleManager.cultureInfo.CompareInfo.IndexOf(prefabName == null ? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER") : prefabName + (PrefabUtils.instance.AuthorList.TryGetValue(prefabName.Split('.')[0], out string author) ? "\n" + author : ""), filter, CompareOptions.IgnoreCase) >= 0;
        private string GetCurrentValueIn() => "";
        private string OnChangeValueIn(int arg1, string[] arg2)
        {
            m_out.text = "";
            if (arg1 >= 0 && arg1 < arg2.Length)
            {
                return arg2[arg1];
            }
            else
            {
                return "";
            }
        }

        private string[] OnChangeFilterIn(string arg) =>
            PropSwitcherMod.Controller.PropsLoaded
            .Union(PropSwitcherMod.Controller.TreesLoaded)
            .Where(x => PSPropData.Instance.Entries.Values.Where(y => y.SwitchItems.Any(z => z.TargetPrefab == x.Value)).Count() == 0)
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        public SimpleXmlDictionary<string, SwitchInfo> TargetDictionary(string prefabName) => PSPropData.Instance.Entries;
    }
}