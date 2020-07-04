using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Libraries;
using Klyte.PropSwitcher.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.PropSwitcher.UI
{
    public abstract class PSPrefabPropTab<T> : UICustomControl where T : PrefabInfo
    {
        protected UITextField m_prefab;
        private UIPanel m_titleRow;
        private UITextField m_in;
        private UITextField m_out;
        private UIScrollablePanel m_detourList;
        private UITemplateList<UIPanel> m_listItems;

        protected abstract Dictionary<string, T> PrefabsLoaded { get; }

        private UIPanel m_actionBar;
        private UIButton m_addButton;

        protected void Awake()
        {
            UIPanel layoutPanel = GetComponent<UIPanel>();
            layoutPanel.padding = new RectOffset(8, 8, 10, 10);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 10, 10);
            layoutPanel.clipChildren = true;
            var uiHelper = new UIHelperExtension(layoutPanel);

            AddFilterableInput(Locale.Get("K45_PS_PARENTPREFAB", typeof(T).Name), uiHelper, out m_prefab, out UIListBox popup, OnChangeFilterPrefab, GetCurrentValuePrefab, OnChangeValuePrefab);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHFROM"), uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddFilterableInput(Locale.Get("K45_PS_SWITCHTO"), uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            m_addButton = uiHelper.AddButton(Locale.Get("K45_PS_ADDREPLACEMENTRULE"), OnAddRule) as UIButton;
            m_prefab.eventTextSubmitted += (x, y) => UpdateDetoursList();
            popup.eventSelectedIndexChanged += (x, y) => UpdateDetoursList();

            AddButtonInEditorRow(m_prefab, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Dropper, EnablePickTool, Locale.Get("K45_PS_ENABLETOOLPICKER"), true, 30).zOrder = m_prefab.zOrder + 1;

            float listContainerWidth = layoutPanel.width - 20;
            KlyteMonoUtils.CreateUIElement(out m_titleRow, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, listContainerWidth, 25));
            m_titleRow.autoLayout = true;
            m_titleRow.wrapLayout = false;
            m_titleRow.autoLayoutDirection = LayoutDirection.Horizontal;

            CreateRowPlaceHolder(listContainerWidth - 20, m_titleRow, out UILabel col1Title, out UILabel col2Title, out UILabel col3Title);
            KlyteMonoUtils.LimitWidthAndBox(col1Title, col1Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col2Title, col2Title.width, true);
            KlyteMonoUtils.LimitWidthAndBox(col3Title, col3Title.width, true);
            col1Title.text = Locale.Get("K45_PS_SWITCHFROM_TITLE");
            col2Title.text = Locale.Get("K45_PS_SWITCHTO_TITLE");
            col3Title.text = Locale.Get("K45_PS_ACTIONS_TITLE");

            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "previewPanel", new UnityEngine.Vector4(0, 0, listContainerWidth, 370));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_detourList.backgroundSprite = "OptionsScrollbarTrack";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;

            m_listItems = new UITemplateList<UIPanel>(m_detourList, PSGlobalPropTab.DETOUR_ITEM_TEMPLATE);
            KlyteMonoUtils.CreateUIElement(out m_actionBar, layoutPanel.transform, "topBar", new UnityEngine.Vector4(0, 0, layoutPanel.width, 50));
            m_actionBar.autoLayout = true;
            m_actionBar.autoLayoutDirection = LayoutDirection.Vertical;
            m_actionBar.padding = new RectOffset(5, 5, 5, 5);
            m_actionBar.autoFitChildrenVertically = true;
            var m_topHelper = new UIHelperExtension(m_actionBar);

            AddLabel("", m_topHelper, out UILabel m_labelSelectionDescription, out UIPanel m_containerSelectionDescription);
            var m_btnDelete = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnClearList, "K45_PS_CLEARLIST", false);
            var m_btnExport = AddButtonInEditorRow(m_containerSelectionDescription, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Export, OnExportAsGlobal, "K45_PS_EXPORTASGLOBAL", false);


            UpdateDetoursList();
        }

        internal abstract void EnablePickTool();

        private string OnChangeValuePrefab(int arg1, string[] arg2)
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
        private string GetCurrentValuePrefab() => "";

        private string[] OnChangeFilterPrefab(string arg)
        {
            return PrefabsLoaded
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
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
            if (PSPropData.Instance.PrefabChildEntries.TryGetValue(targetPrefabName, out SimpleXmlDictionary<string, PropSwitchInfo> currentEditingSelection))
            {
                var targetFilename = Path.Combine(PSController.DefaultGlobalPropConfigurationFolder, $"{targetPrefabName}.xml");
                File.WriteAllText(targetFilename, XmlUtils.DefaultXmlSerialize(new ILibableAsContainer<string, PropSwitchInfo>
                {
                    SaveName = targetPrefabName,
                    Data = currentEditingSelection
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
            }
        }

        protected T GetCurrentTargetPrefab() => PrefabsLoaded.TryGetValue(m_prefab.text, out T info1) ? info1 : null;



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
            if (PSPropData.Instance.PrefabChildEntries.TryGetValue(GetCurrentTargetPrefab()?.name ?? "", out SimpleXmlDictionary<string, PropSwitchInfo> currentEditingSelection))
            {
                currentEditingSelection.Remove(component.parent.parent.stringUserData);

                for (int i = 0; i < 32; i++)
                {
                    RenderManager.instance.UpdateGroups(i);
                }
            }
            UpdateDetoursList();
        }


        protected void UpdateDetoursList()
        {
            bool isEditable = !(GetCurrentTargetPrefab()?.name).IsNullOrWhiteSpace();
            PSPropData.Instance.PrefabChildEntries.TryGetValue(GetCurrentTargetPrefab()?.name ?? "", out SimpleXmlDictionary<string, PropSwitchInfo> currentEditingSelection);
            m_in.parent.isVisible = isEditable;
            m_out.parent.isVisible = isEditable;
            m_addButton.isVisible = isEditable;
            m_detourList.parent.isVisible = isEditable;
            m_actionBar.isVisible = isEditable;
            m_titleRow.isVisible = isEditable;

            if (isEditable)
            {
                var keyList = currentEditingSelection?.Keys.OrderBy(x => PropSwitcherMod.Controller.PropsLoaded.Where(y => x == y.Value.name).FirstOrDefault().Key ?? x).ToArray() ?? new string[0];
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

                    col1.text = PropSwitcherMod.Controller.PropsLoaded.Where(y => keyList[i] == y.Value.name).FirstOrDefault().Key ?? keyList[i];

                    var target = currentEditingSelection[keyList[i]]?.TargetProp;

                    col2.text = PropSwitcherMod.Controller.PropsLoaded.Where(y => target == y.Value.name).FirstOrDefault().Key ?? target ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER");

                    currentItem.backgroundSprite = currentItem.zOrder % 2 == 0 ? "" : "InfoPanel";

                }
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
            if (!PSPropData.Instance.PrefabChildEntries.ContainsKey(GetCurrentTargetPrefab().name) || PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name] == null)
            {
                PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name] = new SimpleXmlDictionary<string, PropSwitchInfo>();
            }
            PSPropData.Instance.PrefabChildEntries[GetCurrentTargetPrefab().name][PropSwitcherMod.Controller.PropsLoaded[m_in.text].name] = new Xml.PropSwitchInfo { TargetProp = m_out.text.IsNullOrWhiteSpace() ? null : PropSwitcherMod.Controller.PropsLoaded[m_out.text].name };

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
            T prefab = GetCurrentTargetPrefab();
            return PropSwitcherMod.Controller.PropsLoaded
                .Where(x => (!PSPropData.Instance.PrefabChildEntries.ContainsKey(prefab.name) || !PSPropData.Instance.PrefabChildEntries[prefab.name].ContainsKey(x.Value.name)))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
        }

        internal abstract bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, PropInfo> x);
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

        private string[] OnChangeFilterIn(string arg)
        {
            T prefab = GetCurrentTargetPrefab();
            return PropSwitcherMod.Controller.PropsLoaded
                .Where(x => (!PSPropData.Instance.PrefabChildEntries.ContainsKey(prefab.name) || !PSPropData.Instance.PrefabChildEntries[prefab.name].ContainsKey(x.Value.name)) && IsPropAvailableOnCurrentPrefab(x))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.name.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
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
                    m_prefab.text = PropSwitcherMod.Controller.BuildingsLoaded.Where(x => x.Value.name == infoName).FirstOrDefault().Key ?? "";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.BuildingEditorToolInstance.enabled = true;
        }
        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, PropInfo> x) => GetCurrentTargetPrefab().m_props.Where(y => y.m_finalProp == x.Value).FirstOrDefault() != default;
    }
    public class PSNetPropTab : PSPrefabPropTab<NetInfo>
    {
        protected override Dictionary<string, NetInfo> PrefabsLoaded => PropSwitcherMod.Controller.NetsLoaded;

        internal override bool IsPropAvailableOnCurrentPrefab(KeyValuePair<string, PropInfo> x) => GetCurrentTargetPrefab().m_lanes?.SelectMany(y => y?.m_laneProps?.m_props ?? new NetLaneProps.Prop[0]).Where(y => y?.m_finalProp == x.Value).FirstOrDefault() != default;
        internal override void EnablePickTool()
        {
            m_prefab.text = "";
            UpdateDetoursList();
            PropSwitcherMod.Controller.RoadSegmentToolInstance.OnSelectSegment += (x) =>
            {
                if (x > 0)
                {
                    string infoName = NetManager.instance.m_segments.m_buffer[x].Info.name;
                    m_prefab.text = PropSwitcherMod.Controller.NetsLoaded.Where(x => x.Value.name == infoName).FirstOrDefault().Key ?? "";
                    UpdateDetoursList();
                }
            };
            PropSwitcherMod.Controller.RoadSegmentToolInstance.enabled = true;
        }
    }
}