using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    [Serializable]
    public struct GameTag : IEquatable<GameTag>
    {
        public const char SEPARATOR = '.';

        private sealed class CachedTagData
        {
            public readonly int HashCode;
            public readonly string[] Hierarchy;
            public readonly string[] HierarchyPaths;

            public CachedTagData(string tagName)
            {
                HashCode = StringComparer.Ordinal.GetHashCode(tagName);

                if (string.IsNullOrEmpty(tagName))
                {
                    Hierarchy = Array.Empty<string>();
                    HierarchyPaths = Array.Empty<string>();
                    return;
                }

                Hierarchy = tagName.Split(SEPARATOR);
                HierarchyPaths = new string[Hierarchy.Length];

                string currentPath = string.Empty;
                for (int i = 0; i < Hierarchy.Length; i++)
                {
                    currentPath = i == 0 ? Hierarchy[i] : currentPath + SEPARATOR + Hierarchy[i];
                    HierarchyPaths[i] = currentPath;
                }
            }
        }

        private static readonly object s_cacheLock = new object();
        private static readonly Dictionary<string, CachedTagData> s_cachedTagDataByName = new Dictionary<string, CachedTagData>
        {
            { string.Empty, new CachedTagData(string.Empty) }
        };
        
        [SerializeField] private string m_tagName;
        [NonSerialized] private CachedTagData m_cachedTagData;
        public string TagName => m_tagName ?? string.Empty;

        public GameTag(string tagName)
        {
            m_tagName = Normalize(tagName);
            m_cachedTagData = null;
        }
        
        public bool IsEmpty => string.IsNullOrEmpty(TagName);

        public override string ToString() => TagName;

        public bool IsExact(GameTag otherTag) => string.Equals(TagName, otherTag.TagName, StringComparison.Ordinal);
        public bool IsExact(string otherTagName) => string.Equals(TagName, Normalize(otherTagName), StringComparison.Ordinal);
        
        public bool IsContains(GameTag otherTag) => ContainsInternal(otherTag.TagName);
        public bool IsContains(string otherTagName) => ContainsInternal(otherTagName);
        
        public bool ChildOf(GameTag otherTag) => ChildOfOrExact(otherTag);
        public bool ChildOf(string otherTagName) => ChildOfOrExact(otherTagName);

        public bool ChildOfOrExact(GameTag otherTag) => MatchesHierarchy(otherTag.TagName, includeExactMatch: true);
        public bool ChildOfOrExact(string otherTagName) => MatchesHierarchy(otherTagName, includeExactMatch: true);

        public bool StrictChildOf(GameTag otherTag) => MatchesHierarchy(otherTag.TagName, includeExactMatch: false);
        public bool StrictChildOf(string otherTagName) => MatchesHierarchy(otherTagName, includeExactMatch: false);
        
        public static bool operator ==(GameTag a, GameTag b) => a.Equals(b);
        public static bool operator !=(GameTag a, GameTag b) => !a.Equals(b);
        
        public override bool Equals(object obj)
        {
            if (obj is GameTag otherTag)
                return Equals(otherTag);
            return false;
        }

        public bool Equals(GameTag other)
        {
            return IsExact(other);
        }
        
        public override int GetHashCode()
        {
            return GetCachedTagData().HashCode;
        }
        
        /// <summary>
        /// GameTag 의 계층을 나누어 문자열로 반환.
        /// </summary>
        /// <returns></returns>
        public string[] GetTagNameHierarchy()
        {
            string[] hierarchy = GetCachedTagData().Hierarchy;
            if (hierarchy.Length == 0)
                return Array.Empty<string>();

            return (string[])hierarchy.Clone();
        }

        public IReadOnlyList<string> GetTagNameHierarchyCached()
        {
            return GetCachedTagData().Hierarchy;
        }

        internal IReadOnlyList<string> GetTagPathHierarchyCached()
        {
            return GetCachedTagData().HierarchyPaths;
        }

        private bool ContainsInternal(string otherTagName)
        {
            string normalizedOtherTagName = Normalize(otherTagName);
            return !IsEmpty && !string.IsNullOrEmpty(normalizedOtherTagName) &&
                   TagName.Contains(normalizedOtherTagName, StringComparison.Ordinal);
        }

        private bool MatchesHierarchy(string otherTagName, bool includeExactMatch)
        {
            string normalizedOtherTagName = Normalize(otherTagName);
            if (IsEmpty || string.IsNullOrEmpty(normalizedOtherTagName))
                return false;

            if (!TagName.StartsWith(normalizedOtherTagName, StringComparison.Ordinal))
                return false;

            if (TagName.Length == normalizedOtherTagName.Length)
                return includeExactMatch;

            return TagName[normalizedOtherTagName.Length] == SEPARATOR;
        }

        private static string Normalize(string tagName)
        {
            return tagName ?? string.Empty;
        }

        private CachedTagData GetCachedTagData()
        {
            if (m_cachedTagData != null)
                return m_cachedTagData;

            string tagName = TagName;

            lock (s_cacheLock)
            {
                if (!s_cachedTagDataByName.TryGetValue(tagName, out CachedTagData cachedTagData))
                {
                    cachedTagData = new CachedTagData(tagName);
                    s_cachedTagDataByName.Add(tagName, cachedTagData);
                }

                m_cachedTagData = cachedTagData;
                return cachedTagData;
            }
        }
        
        public static GameTag Empty => new GameTag(string.Empty);
    }
}
