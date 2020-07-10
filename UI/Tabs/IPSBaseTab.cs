using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Xml;

namespace Klyte.PropSwitcher.UI
{
    public interface IPSBaseTab
    {
        void UpdateDetoursList();
        SimpleXmlDictionary<string, SwitchInfo> TargetDictionary(string prefabName);
    }
}