using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    public interface IGameTagListener
    {
        void OnGameTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info);
        void OnGameTagNotified(GameTag gameTag);
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

        private Dictionary<GameTag, int> m_ownTagCounts = new Dictionary<GameTag, int>();

        /// <summary>
        /// 부모 태그 계층별 레퍼런스 카운트. HasParentTag O(1) 조회용.
        /// 예: "Skill.Attack.Fireball" 추가 시 "Skill", "Skill.Attack", "Skill.Attack.Fireball" 각각 +1
        /// </summary>
        private Dictionary<string, int> m_parentTagRefCounts = new Dictionary<string, int>();

        public IList<GameTag> GetOwnTags()
        {
            return m_ownTagCounts.Keys.ToList();
        }

        public int GetTagStack(GameTag tag)
        {
            return m_ownTagCounts.TryGetValue(tag, out int count) ? count : 0;
        }

        public event GameTagOwnshipChangedHandler OnTagOwnshipChanged = null;
        public event GameTagNotifiedHandler OnTagNotified = null;

        private List<IGameTagListener> m_gameTagListeners = new List<IGameTagListener>();

        public void AddListener(IGameTagListener listener)
        {
            m_gameTagListeners.Add(listener);
        }

        public void RemoveListener(IGameTagListener listener)
        {
            m_gameTagListeners.Remove(listener);
        }

        public void AddTag(GameTag tag)
        {
            if (tag.IsEmpty)
                return;

            bool addedNewTag = m_ownTagCounts.TryAdd(tag, 1);
            if (addedNewTag)
                UpdateParentTagRefCounts(tag, +1);
            else
                m_ownTagCounts[tag]++;

            GameTagOwnshipChangeInfo info = new GameTagOwnshipChangeInfo()
            {
                Tag = tag,
                Added = true,
                Remains = m_ownTagCounts[tag]
            };

            OnTagOwnshipChanged?.Invoke(info);

            foreach (IGameTagListener listener in m_gameTagListeners)
                listener.OnGameTagOwnshipChanged(info);
        }

        public void RemoveTag(GameTag tag)
        {
            if (tag.IsEmpty || !m_ownTagCounts.ContainsKey(tag))
            {
                return;
            }

            m_ownTagCounts[tag]--;
            int remains = m_ownTagCounts[tag];

            if (remains <= 0)
            {
                m_ownTagCounts.Remove(tag);
                UpdateParentTagRefCounts(tag, -1);
                remains = 0;
            }

            GameTagOwnshipChangeInfo info = new GameTagOwnshipChangeInfo()
            {
                Tag = tag,
                Added = false,
                Remains = remains
            };

            OnTagOwnshipChanged?.Invoke(info);

            foreach (IGameTagListener listener in m_gameTagListeners)
                listener.OnGameTagOwnshipChanged(info);
        }

        /// <summary>
        /// 태그의 모든 부모 계층에 대해 레퍼런스 카운트를 업데이트합니다.
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

        public void NotifyTag(GameTag tag)
        {
            if (tag.IsEmpty)
                return;

            OnTagNotified?.Invoke(tag);

            foreach (IGameTagListener listener in m_gameTagListeners)
                listener.OnGameTagNotified(tag);
        }

        public bool HasExactTag(GameTag tag)
        {
            return !tag.IsEmpty && m_ownTagCounts.ContainsKey(tag);
        }

        public bool HasExactTagsAll(GameTag[] tags)
        {
            if (tags == null)
                return false;

            return tags.All(tag => HasExactTag(tag));
        }

        public bool HasExactTagsAny(GameTag[] tags)
        {
            return tags != null && tags.Any(tag => HasExactTag(tag));
        }

        public bool HasParentTag(GameTag tag)
        {
            return GetTagOrDescendantCount(tag) > 0;
        }

        public bool HasChildTag(GameTag tag)
        {
            return GetTagOrDescendantCount(tag) > GetTagStack(tag);
        }

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

        private class TimedTagEntry
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
            private readonly Action m_cancelAction;
            private readonly Func<bool> m_isValidFunc;

            internal TimedTagHandle(Action cancelAction, Func<bool> isValidFunc)
            {
                m_cancelAction = cancelAction;
                m_isValidFunc = isValidFunc;
            }

            /// <summary>아직 만료·취소되지 않은 경우 true.</summary>
            public bool IsValid => m_isValidFunc?.Invoke() ?? false;

            /// <summary>
            /// 태그를 즉시 제거하고 타이머를 취소합니다.
            /// </summary>
            public void Cancel() => m_cancelAction?.Invoke();
        }

        private List<TimedTagEntry> m_timedTags = new List<TimedTagEntry>();

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
            return new TimedTagHandle(
                cancelAction: () =>
                {
                    if (entry.Cancelled) return;
                    entry.Cancelled = true;
                    RemoveTag(entry.Tag);
                },
                isValidFunc: () => !entry.Cancelled
            );
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
                    m_timedTags.RemoveAt(i);
                    continue;
                }

                entry.Remaining -= deltaTime;

                if (entry.Remaining <= 0f)
                {
                    entry.Cancelled = true;
                    RemoveTag(entry.Tag);
                    m_timedTags.RemoveAt(i);
                }
            }
        }

        private int GetTagOrDescendantCount(GameTag tag)
        {
            if (tag.IsEmpty)
                return 0;

            return m_parentTagRefCounts.TryGetValue(tag.TagName, out int count) ? count : 0;
        }
    }
}