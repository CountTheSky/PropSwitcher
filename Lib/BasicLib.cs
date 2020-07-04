using Klyte.Commons.Interfaces;
using Klyte.Commons.Libraries;
using Klyte.PropSwitcher.Xml;
using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Libraries
{
    [XmlRoot("LibPropSettings")] public class PSLibPropSettings : LibBaseFile<PSLibPropSettings, ILibableAsContainer<string, PropSwitchInfo>> { protected override string XmlName => "LibPropSwitchList"; }


}