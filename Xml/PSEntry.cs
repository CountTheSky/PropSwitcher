using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Xml
{
    public class SwitchInfo
    {
        [XmlAttribute]
        public string TargetProp { get; set; }

        private PropInfo m_cachedInfo;

        public PropInfo CachedProp
        {
            get {
                if (TargetProp != null && (m_cachedInfo == null || m_cachedInfo?.name != TargetProp))
                {
                    m_cachedInfo = PrefabCollection<PropInfo>.FindLoaded(TargetProp ?? "");
                    if (m_cachedInfo == null)
                    {
                        TargetProp = null;
                    }
                }
                return m_cachedInfo;
            }
        }
    }
}