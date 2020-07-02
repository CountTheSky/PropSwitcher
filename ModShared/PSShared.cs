using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;

namespace Klyte.PropSwitcher.Shared
{
    public static class PSShared
    {
        public static string TranslateProp(string originalProp) => PSData.Instance.Entries.TryGetValue(originalProp, out SwitchInfo info) && info != null ? info.TargetProp : originalProp;
    }

}
