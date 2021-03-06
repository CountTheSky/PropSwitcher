using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Tools;
using Klyte.PropSwitcher.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.PropSwitcher
{
    public class PSController : BaseController<PropSwitcherMod, PSController>
    {
        public static readonly string FOLDER_NAME = "PropSwitcher";
        public static readonly string FOLDER_PATH = FileUtils.BASE_FOLDER_PATH + FOLDER_NAME;

        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();

        public const int MAX_ACCURACY_VALUE = 9;

        protected override void StartActions()
        {
            ReloadPropGlobals();
            if (RoadSegmentToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            }

            if (BuildingEditorToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<BuildingEditorTool>();
            }
            base.StartActions();
        }

        public class TextSearchEntry
        {
            public List<string> terms;
            public string prefabName;
            public string displayName;

            internal bool MatchesTerm(string arg) => terms.Any(x => LocaleManager.cultureInfo.CompareInfo.IndexOf(x, arg, CompareOptions.IgnoreCase) >= 0);
        }

        private Dictionary<string, TextSearchEntry> m_propsLoaded;
        public Dictionary<string, TextSearchEntry> PropsLoaded
        {
            get
            {
                if (m_propsLoaded == null)
                {
                    m_propsLoaded = GetInfos<PropInfo>().Where(x => x?.name != null)
                    .Select(
                        x => new TextSearchEntry()
                        {
                            prefabName = x.name,
                            displayName = GetListName(x),
                            terms = new List<string>()
                            {
                                x.name,
                                GetListName(x),
                                x.GetUncheckedLocalizedTitle(),
                                PropIndexes.instance.AuthorList.TryGetValue(x.name.Split('.')[0], out string author) ? author : null
                            }.Where(x => x != null).ToList()
                        }
                    )
                    .ToDictionary(x => x.displayName, x => x);
                }
                return m_propsLoaded;
            }
        }

        private Dictionary<string, TextSearchEntry> m_treesLoaded;
        public Dictionary<string, TextSearchEntry> TreesLoaded
        {
            get
            {
                if (m_treesLoaded == null)
                {
                    m_treesLoaded = GetInfos<TreeInfo>().Where(x => x?.name != null)
                    .Select(
                        x => new TextSearchEntry()
                        {
                            prefabName = x.name,
                            displayName = GetListName(x),
                            terms = new List<string>()
                            {
                                x.name,
                                GetListName(x),
                                x.GetUncheckedLocalizedTitle(),
                                TreeIndexes.instance.AuthorList.TryGetValue(x.name.Split('.')[0], out string author) ? author : null
                            }.Where(x => x != null).ToList()
                        }
                    )
                    .ToDictionary(x => x.displayName, x => x);
                }
                return m_treesLoaded;
            }
        }

        private Dictionary<string, BuildingInfo> m_buildingsLoaded;
        public Dictionary<string, BuildingInfo> BuildingsLoaded
        {
            get
            {
                if (m_buildingsLoaded == null)
                {
                    m_buildingsLoaded = GetInfos<BuildingInfo>().Where(x => x?.name != null).GroupBy(x => GetListName(x)).Select(x => Tuple.New(x.Key, x.FirstOrDefault())).ToDictionary(x => x.First, x => x.Second);
                }
                return m_buildingsLoaded;
            }
        }

        private Dictionary<string, NetInfo> m_netsLoaded;
        public Dictionary<string, NetInfo> NetsLoaded
        {
            get
            {
                if (m_netsLoaded == null)
                {
                    m_netsLoaded = GetInfos<NetInfo>().Where(x => x?.name != null).GroupBy(x => GetListName(x)).Select(x => Tuple.New(x.Key, x.FirstOrDefault())).ToDictionary(x => x.First, x => x.Second);
                }
                return m_netsLoaded;
            }
        }

        public const string DEFAULT_GLOBAL_PROP_CONFIG_FOLDER = "DefaultReplacingPropOnPrefabs";

        public static string DefaultGlobalPropConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GLOBAL_PROP_CONFIG_FOLDER;

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


        private static string GetListName<T>(T x) where T : PrefabInfo => x is TreeInfo || (x?.name?.EndsWith("_Data") ?? false) ? $"{x?.GetLocalizedTitle()}" : x?.name ?? "";

        public SimpleXmlDictionary<string, SimpleXmlDictionary<string, SwitchInfo>> GlobalPrefabChildEntries { get; set; } = new SimpleXmlDictionary<string, SimpleXmlDictionary<string, SwitchInfo>>();
        internal void ReloadPropGlobals()
        {
            LogUtils.DoLog("LOADING BUILDING CONFIG START -----------------------------");

            var errorList = new List<string>();
            GlobalPrefabChildEntries.Clear();

            FileUtils.EnsureFolderCreation(DefaultGlobalPropConfigurationFolder);

            foreach (string filename in Directory.GetFiles(DefaultGlobalPropConfigurationFolder, "*.xml"))
            {
                try
                {
                    if (CommonProperties.DebugMode)
                    {
                        LogUtils.DoLog($"Trying deserialize {filename}:\n{File.ReadAllText(filename)}");
                    }
                    using FileStream stream = File.OpenRead(filename);
                    LoadDescriptorsFromXml(filename, stream, GlobalPrefabChildEntries);
                }
                catch (Exception e)
                {
                    LogUtils.DoWarnLog($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}\n{e}");
                    errorList.Add($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}");
                }
            }

            if (errorList.Count > 0)
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    title = $"{PropSwitcherMod.Instance.SimpleName} - Errors loading Files",
                    message = string.Join("\r\n", errorList.ToArray()),
                    useFullWindowWidth = true,
                    showButton1 = true,
                    textButton1 = "Okay...",
                    showClose = true

                }, (x) => true);

            }
            for (int i = 0; i < 32; i++)
            {
                RenderManager.instance.UpdateGroups(i);
            }
            LogUtils.DoLog("LOADING GLOBAL CONFIG END -----------------------------");
        }


        private void LoadDescriptorsFromXml(string filePath, FileStream stream, SimpleXmlDictionary<string, SimpleXmlDictionary<string, SwitchInfo>> referenceDic)
        {
            var serializer = new XmlSerializer(typeof(ILibableAsContainer<string, SwitchInfo>));
            if (serializer.Deserialize(stream) is ILibableAsContainer<string, SwitchInfo> config)
            {
                referenceDic[config.SaveName] = config.Data;
                referenceDic[config.SaveName].Values.ForEach(x => x.m_fileSource = filePath);
            }
            else
            {
                throw new Exception("The file wasn't recognized as a valid descriptor!");
            }
        }
    }
}