using Klyte.Commons.Utils;
using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;

namespace Klyte.PropSwitcher.UI
{
    public interface IPSBaseTab
    {
        void UpdateDetoursList();
        XmlDictionary<PrefabChildEntryKey, SwitchInfo> TargetDictionary(string prefabName);
        XmlDictionary<PrefabChildEntryKey, SwitchInfo> CreateTargetDictionary(string prefabName);

        void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info);

        void SetCurrentLoadedData(PrefabChildEntryKey fromSource, SwitchInfo info, string target);
    }
}