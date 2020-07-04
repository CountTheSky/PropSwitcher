using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Xml
{
    public class SwitchInfo
    {
        [XmlAttribute]
        public string TargetPrefab { get; set; }

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
}