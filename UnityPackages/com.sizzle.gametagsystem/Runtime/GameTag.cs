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

        /// <summary>
        /// 직렬화된 원본 태그 문자열을 반환합니다. null은 빈 문자열로 정규화됩니다.
        /// </summary>
        public string TagName => m_tagName ?? string.Empty;

        /// <summary>
        /// 문자열로부터 GameTag를 생성합니다. null 입력은 빈 태그로 처리됩니다.
        /// </summary>
        public GameTag(string tagName)
        {
            m_tagName = Normalize(tagName);
            m_cachedTagData = null;
        }
        
        /// <summary>
        /// 태그 문자열이 비어 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(TagName);

        /// <summary>
        /// 디버깅과 로그 출력용으로 태그 문자열 자체를 반환합니다.
        /// </summary>
        public override string ToString() => TagName;

        /// <summary>
        /// 다른 GameTag와 문자열이 정확히 일치하는지 비교합니다.
        /// </summary>
        public bool IsExact(GameTag otherTag) => string.Equals(TagName, otherTag.TagName, StringComparison.Ordinal);

        /// <summary>
        /// 다른 태그 문자열과 정확히 일치하는지 비교합니다.
        /// </summary>
        public bool IsExact(string otherTagName) => string.Equals(TagName, Normalize(otherTagName), StringComparison.Ordinal);
        
        /// <summary>
        /// 다른 GameTag의 문자열이 현재 태그 문자열의 부분 문자열로 포함되는지 확인합니다.
        /// 계층 관계가 아니라 단순 문자열 Contains 비교입니다.
        /// </summary>
        public bool IsContains(GameTag otherTag) => ContainsInternal(otherTag.TagName);

        /// <summary>
        /// 입력 문자열이 현재 태그 문자열의 부분 문자열로 포함되는지 확인합니다.
        /// 계층 관계가 아니라 단순 문자열 Contains 비교입니다.
        /// </summary>
        public bool IsContains(string otherTagName) => ContainsInternal(otherTagName);
        
        /// <summary>
        /// 현재 태그가 대상 태그의 하위 계층이거나 정확히 같은 태그인지 확인합니다.
        /// 이름과 달리 exact match도 true를 반환합니다.
        /// </summary>
        public bool ChildOf(GameTag otherTag) => ChildOfOrExact(otherTag);

        /// <summary>
        /// 현재 태그가 대상 태그 문자열의 하위 계층이거나 정확히 같은 태그인지 확인합니다.
        /// 이름과 달리 exact match도 true를 반환합니다.
        /// </summary>
        public bool ChildOf(string otherTagName) => ChildOfOrExact(otherTagName);

        /// <summary>
        /// 현재 태그가 대상 태그의 하위 계층이거나 정확히 같은 태그인지 확인합니다.
        /// 비교는 구분자('.') 경계를 고려하여 수행됩니다.
        /// </summary>
        public bool ChildOfOrExact(GameTag otherTag) => MatchesHierarchy(otherTag.TagName, includeExactMatch: true);

        /// <summary>
        /// 현재 태그가 대상 태그 문자열의 하위 계층이거나 정확히 같은 태그인지 확인합니다.
        /// 비교는 구분자('.') 경계를 고려하여 수행됩니다.
        /// </summary>
        public bool ChildOfOrExact(string otherTagName) => MatchesHierarchy(otherTagName, includeExactMatch: true);

        /// <summary>
        /// 현재 태그가 대상 태그의 엄격한 하위 계층인지 확인합니다.
        /// exact match는 false를 반환합니다.
        /// </summary>
        public bool StrictChildOf(GameTag otherTag) => MatchesHierarchy(otherTag.TagName, includeExactMatch: false);

        /// <summary>
        /// 현재 태그가 대상 태그 문자열의 엄격한 하위 계층인지 확인합니다.
        /// exact match는 false를 반환합니다.
        /// </summary>
        public bool StrictChildOf(string otherTagName) => MatchesHierarchy(otherTagName, includeExactMatch: false);
        
        public static bool operator ==(GameTag a, GameTag b) => a.Equals(b);
        public static bool operator !=(GameTag a, GameTag b) => !a.Equals(b);
        
        /// <summary>
        /// object 기반 동등성 비교를 GameTag 정확 일치 비교로 연결합니다.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is GameTag otherTag)
                return Equals(otherTag);
            return false;
        }

        /// <summary>
        /// 다른 GameTag와 정확히 같은 태그인지 비교합니다.
        /// </summary>
        public bool Equals(GameTag other)
        {
            return IsExact(other);
        }
        
        /// <summary>
        /// 태그 문자열의 ordinal hash code를 반환합니다.
        /// 캐시를 사용해 반복 계산 비용을 줄입니다.
        /// </summary>
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

        /// <summary>
        /// 태그를 구분자 기준으로 분리한 계층 배열을 캐시된 읽기 전용 참조로 반환합니다.
        /// 호출자는 반환 배열을 수정하지 않아야 합니다.
        /// </summary>
        public IReadOnlyList<string> GetTagNameHierarchyCached()
        {
            return GetCachedTagData().Hierarchy;
        }

        /// <summary>
        /// 각 계층의 누적 경로를 캐시된 읽기 전용 참조로 반환합니다.
        /// 예: Skill.Attack.Fireball -> Skill, Skill.Attack, Skill.Attack.Fireball
        /// </summary>
        internal IReadOnlyList<string> GetTagPathHierarchyCached()
        {
            return GetCachedTagData().HierarchyPaths;
        }

        /// <summary>
        /// 태그 문자열 부분 일치 비교의 실제 구현입니다.
        /// 빈 태그나 빈 입력은 false를 반환합니다.
        /// </summary>
        private bool ContainsInternal(string otherTagName)
        {
            string normalizedOtherTagName = Normalize(otherTagName);
            return !IsEmpty && !string.IsNullOrEmpty(normalizedOtherTagName) &&
                   TagName.Contains(normalizedOtherTagName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 계층 접두사와 구분자 경계를 이용해 하위 태그 관계를 판정합니다.
        /// includeExactMatch가 true면 exact match도 true로 취급합니다.
        /// </summary>
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

        /// <summary>
        /// 외부 입력을 내부 비교용 문자열로 정규화합니다.
        /// 현재는 null만 빈 문자열로 변환합니다.
        /// </summary>
        private static string Normalize(string tagName)
        {
            return tagName ?? string.Empty;
        }

        /// <summary>
        /// 해시와 계층 분해 결과를 공용 캐시에서 가져오거나 새로 생성합니다.
        /// 동일 문자열의 반복 비교 비용을 줄이기 위한 내부 헬퍼입니다.
        /// </summary>
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
        
        /// <summary>
        /// 빈 태그를 나타내는 편의 상수입니다.
        /// </summary>
        public static GameTag Empty => new GameTag(string.Empty);
    }
}
