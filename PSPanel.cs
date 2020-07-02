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
        private Dictionary<string, string> m_propsLoaded;

        public override float PanelWidth => m_controlContainer.width;

        public override float PanelHeight => 500;

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
            var uiHelper = new UIHelperExtension(layoutPanel);

            AddFilterableInput("FROM", uiHelper, out m_in, out _, OnChangeFilterIn, GetCurrentValueIn, OnChangeValueIn);
            AddFilterableInput("TO", uiHelper, out m_out, out _, OnChangeFilterOut, GetCurrentValueOut, OnChangeValueOut);
            uiHelper.AddButton("ADD!!!", OnAddRule);
        }

        private UITextField m_in;
        private UITextField m_out;

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