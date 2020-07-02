using Klyte.Commons.Interfaces;
using System.Reflection;

[assembly: AssemblyVersion("0.0.0.*")]
namespace Klyte.PropSwitcher
{
    public class PropSwitcherMod : BasicIUserMod<PropSwitcherMod, PSController, PSPanel>
    {

        public override string IconName => "K45_PS_Icon";
        public override string SimpleName => "Prop Switcher";
        public override string Description => "Simple switch from a prop model to another in all their uses";


    }
}