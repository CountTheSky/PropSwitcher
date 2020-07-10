using Klyte.PropSwitcher.Data;
using Klyte.PropSwitcher.Overrides;
using Klyte.PropSwitcher.Xml;

namespace Klyte.PropSwitcher.Shared
{
    public static class PSShared
    {
        public static PropInfo TranslateProp(PropInfo originalProp, ref InstanceID id)
        {
            float angle = 0;
            return PropInstanceOverrides.GetTargetInfo(originalProp, ref id, ref angle);
        }
    }

}
