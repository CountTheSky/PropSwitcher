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
    public class PSGlobalPropTab : UICustomControl
    {
        internal const string DETOUR_ITEM_TEMPLATE = "K45_PS_TemplateDetourListItemGlobal";
        private UITextField m_in;
        private UITextField m_out;
        private UIScrollablePanel m_detourList;
        private UITemplateList<UIPanel> m_listItems;

        private UIPanel m_actionBar;

        protected void Awake()
        {
            UIPanel layoutPanel = GetComponent<UIPanel>();
            layoutPanel.padding = new RectOffset(8, 8, 10, 10);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 10, 10);
            layoutPanel.clipChildren = true;
            var uiHelper = new UIHelperExtension(layoutPanel);

            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            uiHelper.AddButton(Locale.Get("K45_PS_ADDREPLACEMENTRULE"), OnAddRule);

            float listContainerWidth = layoutPanel.width - 20;
            KlyteMonoUtils.CreateUIElement(out UIPanel titleRow, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            titleRow.autoLayout = true;
            titleRow.wrapLayout = false;
            titleRow.autoLayoutDirection = LayoutDirection.Horizontal;

            CreateRowPlaceHolder(listContainerWidth - 20, titleRow, out UILabel col1Title, out UILabel col2Title, out UILabel col3Title);
            KlyteMonoUtils.LimitWidthAndBox(col1Title, col1Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col2Title, col2Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col3Title, col3Title.width, true);
            col1Title.text = Locale.Get("K45_PS_SWITCHFROM_TITLE");
            col2Title.text = Locale.Get("K45_PS_SWITCHTO_TITLE");
            col3Title.text = Locale.Get("K45_PS_ACTIONS_TITLE");

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "previewPanel", new UnityEngine.Vector4(0, 0, listContainerWidth, 380));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_detourList.backgroundSprite = "OptionsScrollbarTrack";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;
            CreateTemplateDetourItem();
            m_listItems = new UITemplateList<UIPanel>(m_detourList, DETOUR_ITEM_TEMPLATE);
            UpdateDetoursList();

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

        private void CreateTemplateDetourItem()
        {
            var targetWidth = m_detourList.width;
            var go = new GameObject();
            UIPanel panel = go.AddComponent<UIPanel>();
            panel.size = new Vector2(targetWidth, 31);
            panel.autoLayout = true;
            panel.wrapLayout = false;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;
            panel.autoLayoutPadding = new RectOffset(0, 0, 3, 3);


            var uiHelper = new UIHelperExtension(panel, LayoutDirection.Horizontal);
            CreateRowPlaceHolder(targetWidth, panel, out _, out _, out UIPanel actionsPanel);

            KlyteMonoUtils.InitCircledButton(actionsPanel, out UIButton removeButton, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, null, "K45_PS_REMOVE_PROP_RULE", 22);
            removeButton.name = "RemoveItem";

            UITemplateUtils.GetTemplateDict()[DETOUR_ITEM_TEMPLATE] = panel;
        }

        private static void CreateRowPlaceHolder<T>(float targetWidth, UIPanel panel, out UILabel column1, out UILabel column2, out T actionsContainer) where T : UIComponent
        {
            KlyteMonoUtils.CreateUIElement(out column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * 0.4f, 25));
            column1.minimumSize = new Vector2(0, 25);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out column2, panel.transform, "ToLbl", new Vector4(0, 0, targetWidth * 0.4f, 25));
            column2.minimumSize = new Vector2(0, 25);
            column2.verticalAlignment = UIVerticalAlignment.Middle;
            column2.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out actionsContainer, panel.transform, "ActionsPanel", new Vector4(0, 0, targetWidth * 0.175f, 25));
            actionsContainer.minimumSize = new Vector2(0, 25);
        }

        private void OnRemoveDetour(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (PSPropData.Instance.Entries.ContainsKey(component.parent.parent.stringUserData))
            {
                PSPropData.Instance.Entries.Remove(component.parent.parent.stringUserData);

                for (int i = 0; i < 32; i++)
                {
                    RenderManager.instance.UpdateGroups(i);
                }
            }
            UpdateDetoursList();
        }


        private void UpdateDetoursList()
        {
            var keyList = PSPropData.Instance.Entries.Keys.OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x == y.Value).FirstOrDefault().Key ?? x).ToArray();
            UIPanel[] districtChecks = m_listItems.SetItemCount(keyList.Length);
            for (int i = 0; i < keyList.Length; i++)
            {
                UIPanel currentItem = m_listItems.items[i];
                UILabel col1 = currentItem.Find<UILabel>("FromLbl");
                UILabel col2 = currentItem.Find<UILabel>("ToLbl");
                if (currentItem.objectUserData == null)
                {
                    KlyteMonoUtils.LimitWidthAndBox(col1, currentItem.parent.width * 0.4f, true);
                    KlyteMonoUtils.LimitWidthAndBox(col2, currentItem.parent.width * 0.4f, true);
                    UIButton removeButton = currentItem.Find<UIButton>("RemoveItem");
                    removeButton.eventClicked += OnRemoveDetour;

                    currentItem.objectUserData = true;
                }

                currentItem.stringUserData = keyList[i];

                col1.text = PropSwitcherMod.Controller.PropsLoaded.Where(y => keyList[i] == y.Value).FirstOrDefault().Key ?? keyList[i];

                var target = PSPropData.Instance.Entries[keyList[i]]?.TargetPrefab;

                col2.text = PropSwitcherMod.Controller.PropsLoaded.Where(y => target == y.Value).FirstOrDefault().Key ?? target ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER");

                currentItem.backgroundSprite = currentItem.zOrder % 2 == 0 ? "" : "InfoPanel";

            }

        }


        private void OnAddRule()
        {
            if (m_in.text.IsNullOrWhiteSpace() || m_in.text == m_out.text)
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

            PSPropData.Instance.Entries[inText] = new Xml.SwitchInfo { TargetPrefab = m_out.text.IsNullOrWhiteSpace() ? null : outText };

            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            m_in.text = "";
            m_out.text = "";
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
            return (PropSwitcherMod.Controller.PropsLoaded.ContainsValue(m_in.text) ? PropSwitcherMod.Controller.PropsLoaded : PropSwitcherMod.Controller.TreesLoaded)
                .Where(x => !PSPropData.Instance.Entries.ContainsKey(x.Value))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        private string GetCurrentValueIn() => "";
        private string OnChangeValueIn(int arg1, string[] arg2)
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

        private string[] OnChangeFilterIn(string arg) =>
            PropSwitcherMod.Controller.PropsLoaded
            .Union(PropSwitcherMod.Controller.TreesLoaded)
            .Where(x => !PSPropData.Instance.Entries.ContainsKey(x.Value))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();

    }
}