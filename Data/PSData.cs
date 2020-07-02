using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Xml;
using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Data
{
    public class PSData : DataExtensorBase<PSData>
    {
        [XmlElement("Entries")]
        public SimpleXmlDictionary<string, SwitchInfo> Entries { get; set; } = new SimpleXmlDictionary<string, SwitchInfo>();

        public override string SaveId { get; } = "K45_PS_BasicData";
    }

}
