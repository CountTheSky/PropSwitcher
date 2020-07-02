using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Klyte.PropSwitcher
{
    public class PSController : BaseController<PropSwitcherMod, PSController>
    {
        public static readonly string FOLDER_NAME = "PropSwitcher";
        public static readonly string FOLDER_PATH = FileUtils.BASE_FOLDER_PATH + FOLDER_NAME;

        public const int MAX_ACCURACY_VALUE = 9;

        private static readonly Dictionary<string, Tuple<int, float>> m_cachedValues = new Dictionary<string, Tuple<int, float>>();




    }
}