using ColossalFramework;
using Klyte.Commons.Utils;
using System;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.PropSwitcher.Xml
{
    public class SwitchInfo
    {
        [XmlAttribute("TargetPrefab")]
        [Obsolete]
        public string Old_TargetPrefab
        {
            get => null;
            set {
                if (!value.IsNullOrWhiteSpace())
                {
                    LegacyLoaded = true;
                    SwitchItems = SwitchItems.Where(x => x.TargetPrefab != value).Union(new Item[] { new Item { TargetPrefab = value } }).ToArray();
                }
            }
        }

        internal bool LegacyLoaded { get; private set; } = false;

        [XmlElement("SwitchItem")]
        public Item[] SwitchItems { get; set; } = new Item[0];
        [XmlIgnore]
        internal string m_fileSource;
        [XmlAttribute("seedSource")]
        public RandomizerSeedSource SeedSource { get; set; }

        public class Item
        {

            [XmlAttribute("targetPrefab")]
            public string TargetPrefab { get; set; }
            [XmlAttribute("weightDraw")]
            public ushort WeightInDraws { get; set; }
            [XmlAttribute("rotationOffset")]
            public float RotationOffset { get; set; }
            [XmlAttribute("positionOffset")]
            public Vector3Xml PositionOffset { get; set; } = new Vector3Xml();



            private PropInfo m_cachedPropInfo;
            private string m_lastTryTargetProp;

            public PropInfo CachedProp
            {
                get {
                    if (TargetPrefab != m_lastTryTargetProp)
                    {
                        m_cachedPropInfo = PrefabCollection<PropInfo>.FindLoaded(TargetPrefab ?? "");

                        m_lastTryTargetProp = TargetPrefab;
                    }
                    return m_cachedPropInfo;
                }
            }

            private TreeInfo m_cachedTreeInfo;
            private string m_lastTryTargetTree;

            public TreeInfo CachedTree
            {
                get {
                    if (TargetPrefab != m_lastTryTargetTree)
                    {
                        m_cachedTreeInfo = PrefabCollection<TreeInfo>.FindLoaded(TargetPrefab ?? "");

                        m_lastTryTargetTree = TargetPrefab;
                    }
                    return m_cachedTreeInfo;
                }
            }
        }

        public enum RandomizerSeedSource
        {
            INSTANCE,
            POSITION
        }

        internal void Add(string newPrefab, float rotationOffset) => SwitchItems = SwitchItems.Where(x => x.TargetPrefab != newPrefab).Union(new Item[] { new Item { TargetPrefab = newPrefab, RotationOffset = rotationOffset } }).ToArray();
        internal void Remove(string prefabName) => SwitchItems = SwitchItems.Where(x => x.TargetPrefab != prefabName).ToArray();
    }
}