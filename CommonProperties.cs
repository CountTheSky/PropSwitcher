using Klyte.PropSwitcher;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => PropSwitcherMod.DebugMode;
        public static string Version => PropSwitcherMod.Version;
        public static string ModName => PropSwitcherMod.Instance.SimpleName;
        public static string Acronym => "PS";
        public static string ModRootFolder { get; } = PSController.FOLDER_PATH;
        public static string ModDllRootFolder { get; } = PropSwitcherMod.RootFolder;
        public static string ModIcon => PropSwitcherMod.Instance.IconName;

        public static string GitHubRepoPath { get; } = "klyte45/PropSwitcher";
    }
}