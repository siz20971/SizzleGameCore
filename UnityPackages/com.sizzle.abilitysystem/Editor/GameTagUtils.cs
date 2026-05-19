using Sizzle.AbilitySystem.Editor;
using Sizzle.GameTagSystem;
using Sizzle.GameTagSystem.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem
{
    public static class GameTagUtils
    {
        /// <summary>
        /// 캐싱된 GameTag 목록을 반환합니다
        /// </summary>
        public static void GetCachedGameTags(out IList<GameTag> tags)
        {
            tags = GameTagCache.GetCachedGameTags();
        }
        
        /// <summary>
        /// 프로젝트의 Abiltiy 애셋에 등록된 GameTagCache에 캐싱합니다.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void CacheGameTagsInAbilityAssets()
        {
            HashSet<GameTag> allTagSet = new HashSet<GameTag>();
            
            string[] guids = AssetDatabase.FindAssets("t:Ability");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Ability ability = AssetDatabase.LoadAssetAtPath<Ability>(path);

                if (ability == null)
                    continue;

                allTagSet.Add(ability.MainTag);

                foreach (GameTag tag in ability.TagSet.CancelAbilitiesWithTag)
                    allTagSet.Add(tag);

                foreach (GameTag tag in ability.TagSet.BlockAbilitiesWithTag)
                    allTagSet.Add(tag);

                foreach (GameTag tag in ability.TagSet.ActivationOwnedTags)
                    allTagSet.Add(tag);

                foreach (GameTag tag in ability.TagSet.ActivationRequiredTags)
                    allTagSet.Add(tag);

                foreach (GameTag tag in ability.TagSet.ActivationBlockedTags)
                    allTagSet.Add(tag);

                allTagSet.Add(ability.TagSet.TriggerTag);
                
                Resources.UnloadAsset(ability);
            }

            foreach (var value in GetPublicConstStringValuesFromFile(AbilityGameTagCodeGenerator.CUSTOM_FILE_PATH))
                allTagSet.Add(new GameTag(value));
            
            List<GameTag> allTags = allTagSet.ToList();
            allTags.RemoveAll(tag => string.IsNullOrEmpty(tag.TagName));
            allTags.Sort((a, b) => a.TagName.CompareTo(b.TagName));
            
            GameTagCache.ClearCache();
            GameTagCache.AddRange(allTagSet);
            
            global::System.GC.Collect();
        }

        private static List<string> GetPublicConstStringValuesFromFile(string filePath)
        {
            string pattern = @"public\s+static\s+readonly\s+GameTag\s+(\w+)\s*=\s*new\s+GameTag\(""([^""]+)""\);";

            bool fileExists = global::System.IO.File.Exists(filePath);
            if (!fileExists)
            {
                Debug.LogWarning($"File not found: {filePath}");
                return new List<string>();
            }

            MatchCollection matches = Regex.Matches(global::System.IO.File.ReadAllText(filePath), pattern);
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    result[key] = value;
                }
            }
            
            return result.Values.ToList();
        }
    }
}