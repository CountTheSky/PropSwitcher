using Klyte.PropSwitcher.Overrides;
using UnityEngine;

namespace Klyte.PropSwitcher.Shared
{
    public static class PSShared
    {
        public static PropInfo TranslateProp(PropInfo originalProp, ref InstanceID id, ref Vector3 position)
        {
            float angle = 0;
            return PropInstanceOverrides.GetTargetInfo(originalProp, ref id, ref angle, ref position);
        }
    }

}
