using System.Collections.Generic;

namespace Sizzle.GameTagSystem.Editor
{
    public static class GameTagCache
    {
        private static List<GameTag> m_cachedGameTags = new List<GameTag>();

        public static IList<GameTag> GetCachedGameTags()
        {
            return m_cachedGameTags.AsReadOnly();
        }
        
        public static bool Contains(GameTag tag)
        {
            return m_cachedGameTags.Contains(tag);
        }

        public static void Add(GameTag tag)
        {
            if (!m_cachedGameTags.Contains(tag))
                m_cachedGameTags.Add(tag);
        }
        
        public static void AddRange(IEnumerable<GameTag> tags)
        {
            foreach (GameTag tag in tags)
            {
                if (!m_cachedGameTags.Contains(tag))
                    m_cachedGameTags.Add(tag);
            }
        }
        
        public static void Remove(GameTag tag)
        {
            if (m_cachedGameTags.Contains(tag))
                m_cachedGameTags.Remove(tag);
        }
        
        public static void ClearCache()
        {
            m_cachedGameTags.Clear();
        }
    }
}