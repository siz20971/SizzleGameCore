using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem.Editor
{
    [InitializeOnLoad]
    public static class CreateAbilityAssetMenuProcessor
    {
        static CreateAbilityAssetMenuProcessor()
        {
            IEnumerable<Type> scriptableObjectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(ScriptableObject)))
                .Where(type => type.GetCustomAttribute<CreateAbilityAssetMenuAttribute>() != null);

            foreach (Type type in scriptableObjectTypes)
            {
                CreateAbilityAssetMenuAttribute attribute = type.GetCustomAttribute<CreateAbilityAssetMenuAttribute>();
                string menuName = attribute.MenuName ?? type.Name; // 메뉴 이름이 지정되지 않으면 클래스명 사용
                AddMenuItem(menuName, type, attribute.Priority);
            }
        }

        private static void AddMenuItem(string menuName, Type type, int priority = -1)
        {
            string menuItemPath = $"Assets/Create/Platformer2D/Character/Ability/{menuName}";

            global::System.Action execute = () => {
                CreateAsset(type);
            };
        
            // Unity의 메뉴 시스템에 동적으로 추가
            var methodInfo = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.NonPublic | BindingFlags.Static);
            methodInfo?.Invoke(null, new object[]
            {
                menuItemPath,
                null,
                null,
                priority,
                execute,
                null
            });
        }
    
        private static void CreateAsset(Type type)
        {
            var asset = ScriptableObject.CreateInstance(type);
            string path = $"{GetSelectedPathOrFallback()}/New {type.Name}.asset";
        
            // 같은 이름이 존재하면 유니크한 이름으로 변경
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    
        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            if (Selection.activeObject != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (global::System.IO.Directory.Exists(selectedPath))
                        path = selectedPath;
                    else
                        path = global::System.IO.Path.GetDirectoryName(selectedPath);
                }
            }
        
            Debug.Log("PATH : "+ path);

            return path;
        }
    }
}