using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;
using System.Linq;
using UnityEngine;

namespace Klyte.PropSwitcher.UI
{
    public class PSSwitchEntry : UICustomControl
    {
        public const string DETOUR_ITEM_TEMPLATE = "K45_PS_TemplateDetourListItem";
        private const string DETOUR_SUBITEM_TEMPLATE = "K45_PS_TemplateDetourListItem_subitem";
        private UIPanel m_panel;
        private UILabel m_from;
        private UITemplateList<UIPanel> m_replaceSubItems;
        private UIPanel m_subitemContainer;
        private SwitchInfo m_currentLoadedInfo;
        private PrefabChildEntryKey m_currentFromSource;
        private string m_parentPrefabName;

        private PSPrefabTabParent m_mainPanelController;

        public PSPrefabTabParent MainPanelController
        {
            get
            {
                if (m_mainPanelController == null)
                {
                    m_mainPanelController = GetComponentInParent<PSPrefabTabParent>();
                }

                return m_mainPanelController;
            }
        }

        public static void CreateTemplateDetourItem(float targetWidth)
        {
            if (!UITemplateUtils.GetTemplateDict().ContainsKey(PSSwitchEntry.DETOUR_ITEM_TEMPLATE))
            {
                var go = new GameObject();
                go.SetActive(false);
                UIPanel panel = go.AddComponent<UIPanel>();
                go.AddComponent<PSSwitchEntry>();
                panel.width = targetWidth;
                UITemplateUtils.GetTemplateDict()[PSSwitchEntry.DETOUR_ITEM_TEMPLATE] = panel;
            }
        }
        protected void Awake()
        {
            m_panel = GetComponent<UIPanel>();
            m_panel.size = new Vector2(m_panel.width, 31);
            m_panel.autoLayout = true;
            m_panel.wrapLayout = false;
            m_panel.autoFitChildrenVertically = true;
            m_panel.autoLayoutDirection = LayoutDirection.Horizontal;
            m_panel.autoLayoutPadding = new RectOffset(0, 0, 3, 3);
            m_panel.eventClicked += (x, y) =>
            {
                if (!y.used)
                {
                    MainPanelController.SetCurrentLoadedData(m_currentFromSource, m_currentLoadedInfo);
                    y.Use();
                }

            };

            new UIHelperExtension(m_panel, LayoutDirection.Horizontal);

            CreateRowPlaceHolder(m_panel.width, m_panel, out m_from, out m_subitemContainer);
            CreateTemplateSubItem();
            m_replaceSubItems = new UITemplateList<UIPanel>(m_subitemContainer, DETOUR_SUBITEM_TEMPLATE);


            KlyteMonoUtils.LimitWidthAndBox(m_from, m_panel.width * 0.35f, true);

        }

        private void CreateTemplateSubItem()
        {
            if (!UITemplateUtils.GetTemplateDict().ContainsKey(DETOUR_SUBITEM_TEMPLATE))
            {
                var go = new GameObject();
                go.SetActive(false);
                UIPanel panel = go.AddComponent<UIPanel>();
                go.AddComponent<PSSwitchEntrySubItem>();
                panel.width = m_panel.width * .65f;

                UITemplateUtils.GetTemplateDict()[DETOUR_SUBITEM_TEMPLATE] = panel;
            }
        }


        private static void CreateRowPlaceHolder(float targetWidth, UIPanel panel, out UILabel column1, out UIPanel actionsContainer)
        {
            KlyteMonoUtils.CreateUIElement(out column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * PSPrefabTabParent.COL_PROPTOREPLACE_PROPORTION, 31));
            column1.minimumSize = new Vector2(0, 31);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out actionsContainer, panel.transform, "SubItems", new Vector4(0, 0, targetWidth * PSPrefabTabParent.SUBCOL_TOTAL_PROPORTION, 25));
            actionsContainer.minimumSize = new Vector2(0, 25);
            actionsContainer.autoFitChildrenVertically = true;
            actionsContainer.autoLayout = true;
            actionsContainer.autoLayoutDirection = LayoutDirection.Vertical;
        }





        public void SetData(string parentPrefab, PrefabInfo info, PrefabChildEntryKey fromPropSrc, SwitchInfo targetInfo, Color textColor, bool isGlobal)
        {

            m_currentLoadedInfo = targetInfo;
            m_currentFromSource = fromPropSrc;
            m_parentPrefabName = parentPrefab;

            var fromProp = fromPropSrc.FromContext(info);

            m_from.text = fromPropSrc.ToString(info);
            m_from.suffix = targetInfo.SwitchItems.Length > 1 ? "\n" + Locale.Get("K45_PS_RANDOMIZEBY", targetInfo.SeedSource.ToString()) : "";
            m_from.tooltip = fromPropSrc.ToString(info) + (PropIndexes.instance.AuthorList.TryGetValue(fromProp.Split('.')[0], out string author) ? "\n" + author : TreeIndexes.instance.AuthorList.TryGetValue(fromProp.Split('.')[0], out author) ? "\n" + author : "");
            m_from.textColor = textColor;
            m_from.minimumSize = new Vector2(m_from.minimumSize.x, 31 * targetInfo.SwitchItems.Length);

            var panels = m_replaceSubItems.SetItemCount(targetInfo.SwitchItems.Length);
            for (int i = 0; i < targetInfo.SwitchItems.Length; i++)
            {
                panels[i].GetComponent<PSSwitchEntrySubItem>().SetData(targetInfo.SwitchItems[i], textColor, isGlobal, this, fromPropSrc);
            }

            m_panel.backgroundSprite = "OptionsScrollbarTrack";
        }

        private void RemoveItself()
        {
            MainPanelController.TargetDictionary(m_parentPrefabName)?.Remove(m_currentFromSource);
            MainPanelController.ForceUpdate();
        }

        public class PSSwitchEntrySubItem : UICustomControl
        {
            private UIPanel m_panel;
            private UILabel m_to;
            private UILabel m_rotationOffset;
            private UIPanel m_actionsPanel;
            private UIButton m_removeButton;
            private UIButton m_questionMark;
            private UIButton m_gotoFile;
            private UIButton m_copyToCity;
            private string m_currentPrefabTarget;
            private PSSwitchEntry m_mainRow;

            public void Awake()
            {
                m_panel = GetComponent<UIPanel>();
                m_panel.size = new Vector2(m_panel.width, 31);
                m_panel.autoLayout = true;
                m_panel.wrapLayout = false;
                m_panel.autoLayoutDirection = LayoutDirection.Horizontal;
                m_panel.autoLayoutPadding = new RectOffset(0, 0, 0, 3);
                m_panel.eventClicked += (x, y) =>
                {
                    if (!y.used)
                    {
                        m_mainRow.MainPanelController.SetCurrentLoadedData(m_mainRow.m_currentFromSource, m_mainRow.m_currentLoadedInfo, m_currentPrefabTarget);
                        y.Use();
                    }
                };


                var uiHelper = new UIHelperExtension(m_panel, LayoutDirection.Horizontal);
                KlyteMonoUtils.CreateUIElement(out m_to, m_panel.transform, "ToLbl", new Vector4(0, 0, m_panel.width * PSPrefabTabParent.SUBCOL_PROPREPLACEMENT_PROPORTION, 28));
                m_to.minimumSize = new Vector2(0, 28);
                m_to.verticalAlignment = UIVerticalAlignment.Middle;
                m_to.textAlignment = UIHorizontalAlignment.Center;
                KlyteMonoUtils.CreateUIElement(out m_rotationOffset, m_panel.transform, "RotLbl", new Vector4(0, 0, m_panel.width * PSPrefabTabParent.SUBCOL_OFFSETS_PROPORTION, 28));
                m_rotationOffset.minimumSize = new Vector2(0, 28);
                m_rotationOffset.verticalAlignment = UIVerticalAlignment.Middle;
                m_rotationOffset.textAlignment = UIHorizontalAlignment.Center;
                KlyteMonoUtils.CreateUIElement(out m_actionsPanel, m_panel.transform, "ActionsPanel", new Vector4(0, 0, m_panel.width * PSPrefabTabParent.SUBCOL_ACTIONS_PROPORTION, 28));
                m_actionsPanel.minimumSize = new Vector2(0, 28);

                m_actionsPanel.autoLayout = true;
                m_actionsPanel.autoLayoutPadding = new RectOffset(2, 2, 2, 2);

                KlyteMonoUtils.InitCircledButton(m_actionsPanel, out m_removeButton, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, OnRemoveDetour, "K45_PS_REMOVE_PROP_RULE", 22);
                m_removeButton.name = "RemoveItem";
                KlyteMonoUtils.InitCircledButton(m_actionsPanel, out m_questionMark, Commons.UI.SpriteNames.CommonsSpriteNames.K45_QuestionMark, null, "K45_PS_GLOBALCONFIGURATION_INFO", 22);
                m_questionMark.disabledBgSprite = "";
                m_questionMark.name = "AboutItemInformation";
                KlyteMonoUtils.InitCircledButton(m_actionsPanel, out m_gotoFile, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Load, OnGoToGlobalFile, "K45_PS_GLOBALCONFIGURATION_GOTOFILE", 22);
                m_gotoFile.name = "GoToFile";
                KlyteMonoUtils.InitCircledButton(m_actionsPanel, out m_copyToCity, Commons.UI.SpriteNames.CommonsSpriteNames.K45_Copy, OnCopyToCity, "K45_PS_GLOBALCONFIGURATION_COPYTOCITY", 22);
                m_copyToCity.name = "GoToFile";

                KlyteMonoUtils.LimitWidthAndBox(m_to, m_panel.width * PSPrefabTabParent.SUBCOL_PROPREPLACEMENT_PROPORTION, true);
                KlyteMonoUtils.LimitWidthAndBox(m_rotationOffset, m_panel.width * PSPrefabTabParent.SUBCOL_OFFSETS_PROPORTION, true);
                m_questionMark.Disable();


            }

            private void OnCopyToCity(UIComponent component, UIMouseEventParameter eventParam)
            {
                LogUtils.DoWarnLog($"m_mainRow.MainPanelController = {m_mainRow?.MainPanelController}|m_mainRow.m_currentFromSource={m_mainRow?.m_currentFromSource}| m_mainRow.m_currentLoadedInfo ={m_mainRow?.m_currentLoadedInfo}");
                (m_mainRow.MainPanelController.TargetDictionary(m_mainRow.m_parentPrefabName) ?? m_mainRow.MainPanelController.CreateTargetDictionary(m_mainRow.m_parentPrefabName))[m_mainRow.m_currentFromSource] = XmlUtils.DefaultXmlDeserialize<SwitchInfo>(XmlUtils.DefaultXmlSerialize(m_mainRow.m_currentLoadedInfo));
                for (int i = 0; i < 32; i++)
                {
                    RenderManager.instance.UpdateGroups(i);
                }

                m_mainRow.MainPanelController.UpdateDetoursList();
                eventParam.Use();

            }

            private void OnRemoveDetour(UIComponent component, UIMouseEventParameter eventParam)
            {
                m_mainRow.m_currentLoadedInfo.Remove(m_currentPrefabTarget);

                if (m_mainRow.m_currentLoadedInfo.SwitchItems.Length == 0)
                {
                    m_mainRow.RemoveItself();
                }

                StartCoroutine(PSPrefabTabParent.UpdateAllRenderGroups());
                m_mainRow.MainPanelController.UpdateDetoursList();
                eventParam.Use();
            }

            private void OnGoToGlobalFile(UIComponent component, UIMouseEventParameter eventParam)
            {
                if (m_mainRow.m_currentLoadedInfo.m_fileSource != null)
                {
                    Utils.OpenInFileBrowser(m_mainRow.m_currentLoadedInfo.m_fileSource);
                }
                eventParam.Use();
            }

            public void SetData(SwitchInfo.Item targetItem, Color textColor, bool isGlobal, PSSwitchEntry parent, PrefabChildEntryKey entryKey)
            {
                m_mainRow = parent;
                m_currentPrefabTarget = targetItem.TargetPrefab;
                m_to.text = PropSwitcherMod.Controller.PropsLoaded?.Union(PropSwitcherMod.Controller.TreesLoaded)?.Where(y => targetItem.TargetPrefab == y.Value.prefabName).FirstOrDefault().Key ?? targetItem.TargetPrefab ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER");
                m_to.tooltip = targetItem.TargetPrefab != null ? targetItem.TargetPrefab + (PropIndexes.instance.AuthorList.TryGetValue(targetItem.TargetPrefab.Split('.')[0], out string author) ? "\n" + author : TreeIndexes.instance.AuthorList.TryGetValue(targetItem.TargetPrefab.Split('.')[0], out author) ? "\n" + author : "") : Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER");
                m_to.textColor = textColor;

                m_rotationOffset.isVisible = targetItem.TargetPrefab != null && (targetItem.CachedProp != null || entryKey.PrefabIdx >= 0);
                m_rotationOffset.text = string.Join("|", new string[] { (targetItem.CachedProp != null ? $"{targetItem.RotationOffset.ToString("G3")}°" : ""), (entryKey.PrefabIdx >= 0 ? $"({targetItem.PositionOffset.X.ToString("F1")},{targetItem.PositionOffset.Y.ToString("F1")},{targetItem.PositionOffset.Z.ToString("F1")})" : "") }.Where(x => !x.IsNullOrWhiteSpace()).ToArray());
                m_rotationOffset.textColor = textColor;

                m_gotoFile.isVisible = isGlobal;
                m_questionMark.isVisible = isGlobal;
                m_removeButton.isVisible = !isGlobal;
                m_copyToCity.isVisible = isGlobal;

                m_panel.backgroundSprite = m_panel.zOrder % 2 == 1 ? "" : "GenericPanelDark";
            }
        }

    }
}