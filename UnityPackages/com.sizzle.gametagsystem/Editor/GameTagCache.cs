using System.Collections.Generic;

namespace Sizzle.GameTagSystem.Editor
{
    public static class GameTagCache
    {
        private static HashSet<GameTag> m_cachedGameTagSet = new HashSet<GameTag>();
        private static List<GameTag> m_cachedGameTagList = new List<GameTag>();
        private static bool m_isDirty = false;

        public static IList<GameTag> GetCachedGameTags()
        {
            if (m_isDirty)
                RebuildList();

            return m_cachedGameTagList.AsReadOnly();
        }
        
        public static bool Contains(GameTag tag)
        {
            return m_cachedGameTagSet.Contains(tag);
        }

        public static void Add(GameTag tag)
        {
            if (m_cachedGameTagSet.Add(tag))
                m_isDirty = true;
        }
        
        public static void AddRange(IEnumerable<GameTag> tags)
        {
            foreach (GameTag tag in tags)
            {
                if (m_cachedGameTagSet.Add(tag))
                    m_isDirty = true;
            }
        }
        
        public static void Remove(GameTag tag)
        {
            if (m_cachedGameTagSet.Remove(tag))
                m_isDirty = true;
        }
        
        public static void ClearCache()
        {
            m_cachedGameTagSet.Clear();
            m_cachedGameTagList.Clear();
            m_isDirty = false;
        }

        private static void RebuildList()
        {
            m_cachedGameTagList.Clear();
            m_cachedGameTagList.AddRange(m_cachedGameTagSet);
            m_isDirty = false;
        }
    }
}