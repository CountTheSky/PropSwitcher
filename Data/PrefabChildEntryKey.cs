using ColossalFramework;
using ColossalFramework.Globalization;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Klyte.PropSwitcher.Data
{
    public class PrefabChildEntryKey : IEquatable<PrefabChildEntryKey>, ILegacyConverter<PrefabChildEntryKey>
    {
        private string sourcePrefab;
        private int prefabIdx = -1;
        public PrefabChildEntryKey() { }
        public PrefabChildEntryKey(string sourcePrefab)
        {
            SourcePrefab = sourcePrefab;
            PrefabIdx = prefabIdx;
        }

        public PrefabChildEntryKey(int prefabIdx) => PrefabIdx = prefabIdx;

        [XmlAttribute("sourcePrefab")]
        public string SourcePrefab
        {
            get => sourcePrefab; set
            {
                if (value.IsNullOrWhiteSpace())
                {
                    sourcePrefab = null;
                }
                else
                {
                    prefabIdx = -1;
                    sourcePrefab = value;
                }
            }
        }

        [XmlAttribute("prefabIdx")]
        public int PrefabIdx
        {
            get => prefabIdx; set
            {
                if (value >= 0)
                {
                    prefabIdx = value;
                    sourcePrefab = null;
                }
                else
                {
                    prefabIdx = -1;
                }
            }
        }
        public override bool Equals(object obj) => Equals(obj as PrefabChildEntryKey);
        public bool Equals(PrefabChildEntryKey other) => other != null && SourcePrefab == other.SourcePrefab && PrefabIdx == other.PrefabIdx;
        public PrefabChildEntryKey From(string strVal) => new PrefabChildEntryKey(strVal);

        public override int GetHashCode()
        {
            var hashCode = 1043268732;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourcePrefab);
            hashCode = hashCode * -1521134295 + PrefabIdx.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(PrefabChildEntryKey left, PrefabChildEntryKey right) => EqualityComparer<PrefabChildEntryKey>.Default.Equals(left, right);
        public static bool operator !=(PrefabChildEntryKey left, PrefabChildEntryKey right) => !(left == right);

        internal string FromContext<T>(T t) where T : PrefabInfo
        {

            if (prefabIdx >= 0 && t is BuildingInfo buildingInfo)
            {
                if (prefabIdx < buildingInfo.m_props.Length)
                {
                    var propData = buildingInfo.m_props[prefabIdx];
                    return propData.m_finalProp?.name ?? propData.m_finalTree?.name;
                }
            }
            return sourcePrefab;
        }

        internal string ToString<T>(T parent) where T : PrefabInfo
        {
            if (prefabIdx == -1)
            {
                return PropSwitcherMod.Controller.TreesLoaded?.Union(PropSwitcherMod.Controller.PropsLoaded ?? new Dictionary<string, PSController.TextSearchEntry>())?.Where(x => x.Value.prefabName == SourcePrefab).FirstOrDefault().Value?.displayName ?? Locale.Get("K45_PS_REMOVEPROPPLACEHOLDER");
            }
            if (parent is BuildingInfo bi && prefabIdx < bi.m_props.Length)
            {
                var result = $"Prop #{prefabIdx.ToString("000")}: {bi.m_props[prefabIdx].m_finalProp?.GetUncheckedLocalizedTitle() ?? bi.m_props[prefabIdx].m_finalTree?.GetUncheckedLocalizedTitle()} @ {bi.m_props[prefabIdx].m_position}";
                return result;
            }
            return $"INVALID CONTEXT! {PrefabIdx} || {SourcePrefab} @ {parent}";
        }
    }
}
