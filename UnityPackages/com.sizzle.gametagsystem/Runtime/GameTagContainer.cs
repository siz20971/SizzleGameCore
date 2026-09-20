using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    /// <summary>
    /// GameTagContainer의 소유/알림 이벤트를 수신하는 리스너 인터페이스입니다.
    /// </summary>
    public interface IGameTagListener
    {
        /// <summary>
        /// 태그 소유 수량이 변했을 때 호출됩니다.
        /// Added와 Remains를 함께 확인해 현재 상태를 해석합니다.
        /// </summary>
        void OnGameTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info);

        /// <summary>
        /// 태그 알림 이벤트가 발생했을 때 호출됩니다.
        /// 소유 여부와 무관한 단발성 notify 신호입니다.
        /// </summary>
        void OnGameTagNotified(GameTag gameTag);

        /// <summary>
        /// 데이터(페이로드)가 포함된 태그 알림 이벤트가 발생했을 때 호출됩니다.
        /// 기본 구현은 기존 OnGameTagNotified(gameTag)를 호출하도록 되어 있어 하위 호환성을 유지합니다.
        /// </summary>
        void OnGameTagNotified(GameTag gameTag, object payload) => OnGameTagNotified(gameTag);
    }

    [Serializable]
    public class GameTagContainer
    {
        public struct GameTagOwnshipChangeInfo
        {
            public GameTag Tag;
            public bool Added;
            public int Remains;
        }

        public delegate void GameTagOwnshipChangedHandler(GameTagOwnshipChangeInfo info);
        public delegate void GameTagNotifiedHandler(GameTag gameTag);
        public delegate void GameTagNotifiedWithDataHandler(GameTag gameTag, object payload);

        private Dictionary<GameTag, int> m_ownTagCounts = new Dictionary<GameTag, int>();

        /// <summary>
        /// 부모 태그 계층별 레퍼런스 카운트. HasParentTag O(1) 조회용.
        /// 예: "Skill.Attack.Fireball" 추가 시 "Skill", "Skill.Attack", "Skill.Attack.Fireball" 각각 +1
        /// </summary>
        private Dictionary<string, int> m_parentTagRefCounts = new Dictionary<string, int>();

        /// <summary>
        /// 현재 정확히 소유 중인 태그 목록을 반환합니다.
        /// 부모 계층 태그는 자동으로 펼쳐 넣지 않고 직접 보유한 태그만 반환합니다.
        /// </summary>
        public IList<GameTag> GetOwnTags()
        {
            return m_ownTagCounts.Keys.ToList();
        }

        /// <summary>
        /// 특정 태그를 정확히 몇 스택 보유 중인지 반환합니다.
        /// 없으면 0을 반환합니다.
        /// </summary>
        public int GetTagStack(GameTag tag)
        {
            return m_ownTagCounts.TryGetValue(tag, out int count) ? count : 0;
        }

        public event GameTagOwnshipChangedHandler OnTagOwnshipChanged = null;
        public event GameTagNotifiedHandler OnTagNotified = null;
        public event GameTagNotifiedWithDataHandler OnTagNotifiedWithData = null;

        private List<IGameTagListener> m_gameTagListeners = new List<IGameTagListener>();
        private IGameTagListener[] m_gameTagListenersCache = Array.Empty<IGameTagListener>();
        private bool m_isGameTagListenersDirty = false;

        /// <summary>
        /// 태그 변경/알림을 수신할 리스너를 등록합니다.
        /// 중복 등록은 허용되므로 같은 인스턴스를 여러 번 넣으면 중복 호출됩니다.
        /// </summary>
        public void AddListener(IGameTagListener listener)
        {
            m_gameTagListeners.Add(listener);
            m_isGameTagListenersDirty = true;
        }

        /// <summary>
        /// 등록된 리스너를 제거합니다.
        /// 동일 리스너가 여러 번 등록된 경우 한 번 호출로 한 항목만 제거됩니다.
        /// </summary>
        public void RemoveListener(IGameTagListener listener)
        {
            m_gameTagListeners.Remove(listener);
            m_isGameTagListenersDirty = true;
        }

        /// <summary>
        /// 태그를 1스택 추가합니다.
        /// exact stack과 부모 계층 참조 카운트를 함께 증가시키고 소유 변경 이벤트를 발행합니다.
        /// </summary>
        public void AddTag(GameTag tag)
        {
            if (tag.IsEmpty)
                return;

            m_ownTagCounts.TryGetValue(tag, out int count);
            m_ownTagCounts[tag] = count + 1;

            UpdateParentTagRefCounts(tag, +1);

            GameTagOwnshipChangeInfo info = new GameTagOwnshipChangeInfo()
            {
                Tag = tag,
                Added = true,
                Remains = m_ownTagCounts[tag]
            };

            OnTagOwnshipChanged?.Invoke(info);
            NotifyOwnshipChangedListeners(info);
        }

        /// <summary>
        /// 태그를 1스택 제거합니다.
        /// 마지막 스택이 제거되면 exact 소유 목록에서도 삭제하고 소유 변경 이벤트를 발행합니다.
        /// </summary>
        public void RemoveTag(GameTag tag)
        {
            if (tag.IsEmpty || !m_ownTagCounts.TryGetValue(tag, out int count))
            {
                return;
            }

            int remains = count - 1;

            UpdateParentTagRefCounts(tag, -1);

            if (remains <= 0)
            {
                m_ownTagCounts.Remove(tag);
                remains = 0;
            }
            else
            {
                m_ownTagCounts[tag] = remains;
            }

            GameTagOwnshipChangeInfo info = new GameTagOwnshipChangeInfo()
            {
                Tag = tag,
                Added = false,
                Remains = remains
            };

            OnTagOwnshipChanged?.Invoke(info);
            NotifyOwnshipChangedListeners(info);
        }

        private void NotifyOwnshipChangedListeners(GameTagOwnshipChangeInfo info)
        {
            if (m_gameTagListeners.Count == 0)
                return;

            if (m_isGameTagListenersDirty)
            {
                m_gameTagListenersCache = m_gameTagListeners.ToArray();
                m_isGameTagListenersDirty = false;
            }

            for (int i = 0; i < m_gameTagListenersCache.Length; i++)
                m_gameTagListenersCache[i]?.OnGameTagOwnshipChanged(info);
        }

        /// <summary>
        /// 태그의 모든 누적 경로에 대해 참조 카운트를 갱신합니다.
        /// exact 태그 자신도 포함되므로 HasParentTag/HasChildTag 계산에 함께 사용됩니다.
        /// </summary>
        private void UpdateParentTagRefCounts(GameTag tag, int delta)
        {
            IReadOnlyList<string> hierarchyPaths = tag.GetTagPathHierarchyCached();
            if (hierarchyPaths.Count == 0)
                return;

            for (int i = 0; i < hierarchyPaths.Count; i++)
            {
                string key = hierarchyPaths[i];

                if (!m_parentTagRefCounts.TryGetValue(key, out int count))
                    count = 0;

                count += delta;

                if (count <= 0)
                    m_parentTagRefCounts.Remove(key);
                else
                    m_parentTagRefCounts[key] = count;
            }
        }

        /// <summary>
        /// 태그 알림 이벤트를 발행합니다. 선택적으로 자유로운 데이터(payload)를 함께 전달할 수 있습니다.
        /// 소유 상태는 변경하지 않고 notify 이벤트와 리스너 콜백만 호출합니다.
        /// </summary>
        public void NotifyTag(GameTag tag, object payload = null)
        {
            if (tag.IsEmpty)
                return;

            OnTagNotified?.Invoke(tag);
            OnTagNotifiedWithData?.Invoke(tag, payload);
            NotifyTagListeners(tag, payload);
        }

        /// <summary>
        /// 태그 알림 이벤트를 발행합니다. 제네릭 타입의 데이터(payload)를 함께 전달할 수 있습니다.
        /// </summary>
        public void NotifyTag<T>(GameTag tag, T payload)
        {
            NotifyTag(tag, (object)payload);
        }

        private void NotifyTagListeners(GameTag tag, object payload)
        {
            if (m_gameTagListeners.Count == 0)
                return;

            if (m_isGameTagListenersDirty)
            {
                m_gameTagListenersCache = m_gameTagListeners.ToArray();
                m_isGameTagListenersDirty = false;
            }

            for (int i = 0; i < m_gameTagListenersCache.Length; i++)
                m_gameTagListenersCache[i]?.OnGameTagNotified(tag, payload);
        }

        /// <summary>
        /// 해당 태그를 정확히 1스택 이상 보유 중인지 확인합니다.
        /// 하위 태그 보유 여부는 고려하지 않습니다.
        /// </summary>
        public bool HasExactTag(GameTag tag)
        {
            return !tag.IsEmpty && m_ownTagCounts.ContainsKey(tag);
        }

        /// <summary>
        /// 모든 태그를 정확히 보유 중인지 확인합니다.
        /// 배열이 null이면 false를 반환합니다.
        /// </summary>
        public bool HasExactTagsAll(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (!HasExactTag(tags[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 모든 태그를 정확히 보유 중인지 확인합니다.
        /// 리스트가 null이면 false를 반환합니다.
        /// 배열 할당 없이 IList를 직접 순회합니다.
        /// </summary>
        public bool HasExactTagsAll(IList<GameTag> tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (!HasExactTag(tags[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 태그 배열 중 하나라도 정확히 보유 중인지 확인합니다.
        /// 배열이 null이면 false를 반환합니다.
        /// </summary>
        public bool HasExactTagsAny(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (HasExactTag(tags[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 태그 리스트 중 하나라도 정확히 보유 중인지 확인합니다.
        /// 리스트가 null이면 false를 반환합니다.
        /// 배열 할당 없이 IList를 직접 순회합니다.
        /// </summary>
        public bool HasExactTagsAny(IList<GameTag> tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (HasExactTag(tags[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 해당 태그 또는 그 하위 태그를 하나라도 보유 중인지 확인합니다.
        /// 이름과 달리 부모를 찾는 함수가 아니라 exact 또는 descendant 존재 여부를 묻습니다.
        /// </summary>
        public bool HasParentTag(GameTag tag)
        {
            return GetTagOrDescendantCount(tag) > 0;
        }

        /// <summary>
        /// 해당 태그의 엄격한 하위 태그를 하나라도 보유 중인지 확인합니다.
        /// exact 태그만 있는 경우는 false를 반환합니다.
        /// </summary>
        public bool HasChildTag(GameTag tag)
        {
            return GetTagOrDescendantCount(tag) > GetTagStack(tag);
        }

        /// <summary>
        /// 모든 태그에 대해 해당 태그 또는 그 하위 태그를 보유 중인지 확인합니다.
        /// 배열이 null이면 false를 반환합니다.
        /// </summary>
        public bool HasParentTagsAll(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (!HasParentTag(tags[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 태그 배열 중 하나라도 해당 태그 또는 그 하위 태그를 보유 중인지 확인합니다.
        /// 배열이 null이면 false를 반환합니다.
        /// </summary>
        public bool HasParentTagsAny(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (HasParentTag(tags[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 모든 태그에 대해 엄격한 하위 태그를 하나 이상 보유 중인지 확인합니다.
        /// exact 태그만 있는 경우는 false입니다.
        /// </summary>
        public bool HasChildTagsAll(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (!HasChildTag(tags[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 태그 배열 중 하나라도 엄격한 하위 태그를 보유 중인지 확인합니다.
        /// exact 태그만 있는 경우는 true로 취급하지 않습니다.
        /// </summary>
        public bool HasChildTagsAny(GameTag[] tags)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (HasChildTag(tags[i]))
                    return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // Timed Tag
        // ─────────────────────────────────────────────────────────────

        internal class TimedTagEntry
        {
            public GameTag Tag;
            public float Remaining;
            public bool Cancelled;

            public TimedTagEntry(GameTag tag, float duration)
            {
                Tag = tag;
                Remaining = duration;
                Cancelled = false;
            }
        }

        /// <summary>
        /// <see cref="AddTagTimed"/>으로 추가된 지속 시간 태그를 조기 취소할 수 있는 핸들.
        /// </summary>
        public readonly struct TimedTagHandle
        {
            private readonly GameTagContainer m_container;
            private readonly TimedTagEntry m_entry;

            internal TimedTagHandle(GameTagContainer container, TimedTagEntry entry)
            {
                m_container = container;
                m_entry = entry;
            }

            /// <summary>아직 만료·취소되지 않은 경우 true.</summary>
            public bool IsValid => m_entry != null && !m_entry.Cancelled;

            /// <summary>
            /// 태그를 즉시 제거하고 타이머를 취소합니다.
            /// </summary>
            public void Cancel()
            {
                if (IsValid)
                {
                    m_entry.Cancelled = true;
                    m_container.RemoveTag(m_entry.Tag);
                }
            }
        }

        public readonly struct TimedTagInfo
        {
            public GameTag Tag { get; }
            public float Remaining { get; }

            public TimedTagInfo(GameTag tag, float remaining)
            {
                Tag = tag;
                Remaining = remaining;
            }
        }

        private List<TimedTagEntry> m_timedTags = new List<TimedTagEntry>();

        public void GetTimedTags(List<TimedTagInfo> results)
        {
            if (results == null) return;
            results.Clear();

            for (int i = 0; i < m_timedTags.Count; i++)
            {
                TimedTagEntry entry = m_timedTags[i];
                if (entry == null || entry.Cancelled)
                    continue;

                results.Add(new TimedTagInfo(entry.Tag, entry.Remaining));
            }
        }

        public IList<TimedTagInfo> GetTimedTags()
        {
            List<TimedTagInfo> timedTags = new List<TimedTagInfo>(m_timedTags.Count);
            GetTimedTags(timedTags);
            return timedTags;
        }

        /// <summary>
        /// 지속 시간이 있는 태그를 추가합니다. <see cref="Tick"/>을 매 프레임 호출해야 만료됩니다.
        /// </summary>
        /// <param name="tag">추가할 태그</param>
        /// <param name="duration">지속 시간 (초)</param>
        /// <returns>조기 취소에 사용할 수 있는 핸들</returns>
        public TimedTagHandle AddTagTimed(GameTag tag, float duration)
        {
            if (tag.IsEmpty)
                return default;

            AddTag(tag);
            var entry = new TimedTagEntry(tag, duration);
            m_timedTags.Add(entry);
            return new TimedTagHandle(this, entry);
        }

        /// <summary>
        /// 지속 시간 태그의 타이머를 진행시킵니다. 소유 MonoBehaviour의 Update에서 호출하십시오.
        /// </summary>
        /// <param name="deltaTime">경과 시간 (예: Time.deltaTime)</param>
        public void Tick(float deltaTime)
        {
            for (int i = m_timedTags.Count - 1; i >= 0; i--)
            {
                TimedTagEntry entry = m_timedTags[i];

                if (entry.Cancelled)
                {
                    // [O(1) 삭제] 순서가 무관하므로 맨 마지막 원소와 교환 후 마지막 원소를 삭제(Swap-Back)하여 요소 이동 오버헤드 방지
                    m_timedTags[i] = m_timedTags[m_timedTags.Count - 1];
                    m_timedTags.RemoveAt(m_timedTags.Count - 1);
                    continue;
                }

                entry.Remaining -= deltaTime;

                if (entry.Remaining <= 0f)
                {
                    entry.Cancelled = true;
                    RemoveTag(entry.Tag);
                    
                    // [O(1) 삭제] 순서가 무관하므로 맨 마지막 원소와 교환 후 마지막 원소를 삭제(Swap-Back)하여 요소 이동 오버헤드 방지
                    m_timedTags[i] = m_timedTags[m_timedTags.Count - 1];
                    m_timedTags.RemoveAt(m_timedTags.Count - 1);
                }
            }
        }

        /// <summary>
        /// 특정 태그 경로에 매핑된 exact + descendant 총 스택 수를 반환합니다.
        /// exact 스택만 알고 싶다면 GetTagStack을 사용해야 합니다.
        /// </summary>
        private int GetTagOrDescendantCount(GameTag tag)
        {
            if (tag.IsEmpty)
                return 0;

            return m_parentTagRefCounts.TryGetValue(tag.TagName, out int count) ? count : 0;
        }
    }
}