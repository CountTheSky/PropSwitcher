using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.PropSwitcher
{
    public class PSPanel : BasicKPanel<PropSwitcherMod, PSController, PSPanel>
    {
        private const string DETOUR_ITEM_TEMPLATE = "K45_PS_TemplateDetourListItem";
        private Dictionary<string, string> m_propsLoaded;
        private UITextField m_in;
        private UITextField m_out;
        private UIScrollablePanel m_detourList;
        private UITemplateList<UIPanel> m_listItems;

        public override float PanelWidth => m_controlContainer.width;

        public override float PanelHeight => 800;

        public Dictionary<string, string> PropsLoaded
        {
            get {
                if (m_propsLoaded == null)
                {
                    m_propsLoaded = GetInfos<PropInfo>().Where(x => x?.name != null).GroupBy(x => GetListName(x)).Select(x => Tuple.New(x.Key, x.FirstOrDefault())).ToDictionary(x => x.First, x => x.Second.name);
                }
                return m_propsLoaded;
            }
        }
        private List<T> GetInfos<T>() where T : PrefabInfo
        {
            var list = new List<T>();
            uint num = 0u;
            while (num < (ulong)PrefabCollection<T>.LoadedCount())
            {
                T prefabInfo = PrefabCollection<T>.GetLoaded(num);
                if (prefabInfo != null)
                {
                    list.Add(prefabInfo);
                }
                num += 1u;
            }
            return list;
        }
        private static string GetListName(PropInfo x) => (x?.name?.EndsWith("_Data") ?? false) ? $"{x?.GetLocalizedTitle()}" : x?.name ?? "";

        protected override void AwakeActions()
        {
            KlyteMonoUtils.CreateUIElement(out UIPanel layoutPanel, MainPanel.transform, "LayoutPanel", new Vector4(0, 40, PanelWidth, PanelHeight - 40));
            layoutPanel.padding = new RectOffset(8, 8, 10, 10);
            layoutPanel.autoLayout = true;
            layoutPanel.autoLayoutDirection = LayoutDirection.Vertical;
            layoutPanel.autoLayoutPadding = new RectOffset(0, 0, 10, 10);
            layoutPanel.clipChildren = true;
            var uiHelper = new UIHelperExtension(layoutPanel);

            AddFilterableInput("FROM", uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddFilterableInput("TO", uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            uiHelper.AddButton("ADD!!!", OnAddRule);
            KlyteMonoUtils.CreateUIElement(out UIPanel m_listContainer, layoutPanel.transform, "previewPanel", new UnityEngine.Vector4(0, 0, layoutPanel.width - 20, 500));

            KlyteMonoUtils.CreateScrollPanel(m_listContainer, out m_detourList, out _, m_listContainer.width - 20, m_listContainer.height);
            m_detourList.backgroundSprite = "OptionsScrollbarTrack";
            m_detourList.autoLayout = true;
            m_detourList.autoLayoutDirection = LayoutDirection.Vertical;
            CreateTemplateDetourItem();
            m_listItems = new UITemplateList<UIPanel>(m_detourList, DETOUR_ITEM_TEMPLATE);
            UpdateDetoursList();

        }
        private void CreateTemplateDetourItem()
        {
            var targetWidth = m_detourList.width;
            var go = new GameObject();
            UIPanel panel = go.AddComponent<UIPanel>();
            panel.size = new Vector2(targetWidth, 36);
            panel.autoLayout = true;
            panel.wrapLayout = false;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;


            var uiHelper = new UIHelperExtension(panel, LayoutDirection.Horizontal);
            KlyteMonoUtils.CreateUIElement(out UILabel column1, panel.transform, "FromLbl", new Vector4(0, 0, targetWidth * 0.4f, 30));
            column1.minimumSize = new Vector2(0, 30);
            column1.verticalAlignment = UIVerticalAlignment.Middle;
            column1.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out UILabel column2, panel.transform, "ToLbl", new Vector4(0, 0, targetWidth * 0.4f, 30));
            column2.minimumSize = new Vector2(0, 30);
            column2.verticalAlignment = UIVerticalAlignment.Middle;
            column2.textAlignment = UIHorizontalAlignment.Center;
            KlyteMonoUtils.CreateUIElement(out UIPanel actionsPanel, panel.transform, "ActionsPanel", new Vector4(0, 0, targetWidth * 0.175f, 30));

            KlyteMonoUtils.InitCircledButton(actionsPanel, out UIButton removeButton, Commons.UI.SpriteNames.CommonsSpriteNames.K45_X, null, "K45_PS_REMOVE_PROP_RULE", 30);
            removeButton.name = "RemoveItem";

            UITemplateUtils.GetTemplateDict()[DETOUR_ITEM_TEMPLATE] = panel;
        }

        private void OnRemoveDetour(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (PSData.Instance.Entries.ContainsKey(component.parent.parent.stringUserData))
            {
                PSData.Instance.Entries.Remove(component.parent.parent.stringUserData);

                for (int i = 0; i < 32; i++)
                {
                    RenderManager.instance.UpdateGroups(i);
                }
            }
            UpdateDetoursList();
        }


        private void UpdateDetoursList()
        {
            var keyList = PSData.Instance.Entries.Keys.OrderBy(x => PropsLoaded.Where(y => x == y.Value).FirstOrDefault().Key ?? x).ToArray();
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

                col1.text = PropsLoaded.Where(y => keyList[i] == y.Value).FirstOrDefault().Key ?? keyList[i];

                var target = PSData.Instance.Entries[keyList[i]]?.TargetProp;

                col2.text = PropsLoaded.Where(y => target == y.Value).FirstOrDefault().Key ?? target;

            }

        }


        private void OnAddRule()
        {
            if (m_in.text.IsNullOrWhiteSpace() || m_out.text.IsNullOrWhiteSpace() || m_in.text == m_out.text)
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = "INVALID INPUT! CANNOT BE NULL OR EMPTY OR IN EQUALS OUT!",
                    showButton1 = true,
                    textButton1 = Locale.Get("EXCEPTION_OK")
                }, x => true);
                return;
            }

            PSData.Instance.Entries[PropsLoaded[m_in.text]] = new Xml.SwitchInfo { TargetProp = PropsLoaded[m_out.text] };

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
        private string[] OnChangeFilterOut(string arg) => PropsLoaded
                .Where(x => !PSData.Instance.Entries.ContainsKey(x.Value))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();
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
            PropsLoaded
                .Where(x => !PSData.Instance.Entries.ContainsKey(x.Value))
                .Where((x) => arg.IsNullOrWhiteSpace() ? true : LocaleManager.cultureInfo.CompareInfo.IndexOf(x.Value + (PrefabUtils.instance.AuthorList.TryGetValue(x.Value.Split('.')[0], out string author) ? "\n" + author : ""), arg, CompareOptions.IgnoreCase) >= 0)
                .Select(x => x.Key)
                .OrderBy((x) => x)
                .ToArray();

    }
}