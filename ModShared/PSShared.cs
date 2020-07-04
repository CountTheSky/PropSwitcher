using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Xml;

namespace Klyte.PropSwitcher.Shared
{
    public static class PSShared
    {
        public static string TranslateProp(string originalProp) => PSPropData.Instance.PropEntries.TryGetValue(originalProp, out PropSwitchInfo info) && info != null ? info.TargetProp : originalProp;
    }

}
