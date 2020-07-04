using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Xml;
using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Data
{
    public class PSPropData : DataExtensorBase<PSPropData>
    {
        [XmlElement("Entries")]
        public SimpleXmlDictionary<string, PropSwitchInfo> PropEntries { get; set; } = new SimpleXmlDictionary<string, PropSwitchInfo>();
        [XmlElement("PrefabChildEntries")]
        public SimpleXmlDictionary<string, SimpleXmlDictionary<string, PropSwitchInfo>> PrefabChildEntries { get; set; } = new SimpleXmlDictionary<string, SimpleXmlDictionary<string, PropSwitchInfo>>();

        public override string SaveId { get; } = "K45_PS_BasicData";
    }

}
