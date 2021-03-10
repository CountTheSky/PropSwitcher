using Klyte.PropSwitcher.Overrides;
using System;
using UnityEngine;

namespace Klyte.PropSwitcher.Shared
{
    public static class PSShared
    {
        [Obsolete]
        public static PropInfo TranslateProp(PropInfo originalProp, ref InstanceID id, ref Vector3 position) => TranslateProp(originalProp, ref id, ref position, out _);
        public static PropInfo TranslateProp(PropInfo originalProp, ref InstanceID id, ref Vector3 position, out float angleOffset)
        {
            angleOffset = 0;
            Vector3 pos = default;
            return PropInstanceOverrides.GetTargetInfo(originalProp, ref id, ref pos, ref angleOffset, ref position);
        }
    }

}
