using System;
using System.Reflection;
using UnityEditor;

namespace Sizzle.GameTagSystem.Editor
{
    /// <summary>
    /// 프로젝트 내의 [GameTagPreset] 어트리뷰트가 붙은 클래스나 
    /// GameTagPreset ScriptableObject 애셋을 자동으로 수집하여 GameTagCache에 등록합니다.
    /// </summary>
    [InitializeOnLoad]
    internal class GameTagCacheAutoCollector : AssetPostprocessor
    {
        static GameTagCacheAutoCollector()
        {
            // 에디터 로드 혹은 스크립트 재컴파일 시 수집 수행
            EditorApplication.delayCall += CollectPresets;
        }

        [MenuItem("Sizzle/Game Tag System/Refresh GameTag Cache")]
        public static void ForceRefresh()
        {
            GameTagCache.ClearCache();
            CollectPresets();
            UnityEngine.Debug.Log("GameTagCache has been successfully refreshed.");
        }

        private static void CollectPresets()
        {
            // 1. ScriptableObject 애셋 수집
            string[] guids = AssetDatabase.FindAssets("t:GameTagPreset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameTagPreset preset = AssetDatabase.LoadAssetAtPath<GameTagPreset>(path);
                if (preset != null && preset.Tags != null)
                {
                    GameTagCache.AddRange(preset.Tags);
                }
            }

            // 2. [GameTagPreset] 특성이 붙은 클래스의 정적 필드/프로퍼티 수집
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null) continue;

                    try
                    {
                        if (type.IsDefined(typeof(GameTagPresetAttribute), false))
                        {
                            CollectFromType(type);
                        }
                    }
                    catch (Exception)
                    {
                        // Some types might throw exceptions when reading attributes. Ignore them.
                    }
                }
            }
        }

        private static void CollectFromType(Type type)
        {
            // 정적(Static) 요소만 수집합니다.
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            
            // 필드 수집
            var fields = type.GetFields(flags);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameTag))
                {
                    var tag = (GameTag)field.GetValue(null);
                    if (!tag.IsEmpty)
                        GameTagCache.Add(tag);
                }
                else if (field.FieldType == typeof(string))
                {
                    var tagString = (string)field.GetValue(null);
                    if (!string.IsNullOrEmpty(tagString))
                        GameTagCache.Add(new GameTag(tagString));
                }
            }

            // 프로퍼티 수집
            var properties = type.GetProperties(flags);
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(GameTag) && prop.CanRead)
                {
                    var tag = (GameTag)prop.GetValue(null);
                    if (!tag.IsEmpty)
                        GameTagCache.Add(tag);
                }
                else if (prop.PropertyType == typeof(string) && prop.CanRead)
                {
                    var tagString = (string)prop.GetValue(null);
                    if (!string.IsNullOrEmpty(tagString))
                        GameTagCache.Add(new GameTag(tagString));
                }
            }
        }

        // 애셋 변경(추가/삭제 등) 발생 시 캐시 갱신
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool requireUpdate = false;

            foreach (string path in importedAssets)
            {
                if (path.EndsWith(".asset"))
                {
                    var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (type == typeof(GameTagPreset))
                    {
                        requireUpdate = true;
                        break;
                    }
                }
            }

            if (!requireUpdate)
            {
                foreach (string path in deletedAssets)
                {
                    // 삭제된 애셋은 타입을 정확히 알기 어렵지만 .asset 확장자면 보수적으로 갱신 시도
                    if (path.EndsWith(".asset")) 
                    {
                        requireUpdate = true;
                        break;
                    }
                }
            }
            
            if (requireUpdate)
            {
                // 실시간 동기화를 위해 기존 캐시를 비우고 재수집
                // (주의: 외부에서 스크립트로 Add 한 내용이 있다면 다시 주입되어야 할 수 있음)
                GameTagCache.ClearCache();
                CollectPresets();
            }
        }
    }
}
