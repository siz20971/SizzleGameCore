using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Sizzle.AbilitySystem.Editor
{
    [FilePath("ProjectSettings/AbilitySystemEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AbilitySystemEditorSettings : ScriptableSingleton<AbilitySystemEditorSettings>
    {
        [SerializeField]
        private string m_generatedFilePath = "Assets/Scripts/Generated/AbilityTags_Generated.cs";
        
        [SerializeField]
        private string m_customFilePath = "Assets/Scripts/AbilityTags.cs";

        public string GeneratedFilePath
        {
            get => m_generatedFilePath;
            set
            {
                if (m_generatedFilePath != value)
                {
                    m_generatedFilePath = value;
                    Save(true);
                }
            }
        }

        public string CustomFilePath
        {
            get => m_customFilePath;
            set
            {
                if (m_customFilePath != value)
                {
                    m_customFilePath = value;
                    Save(true);
                }
            }
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }

    public static class AbilitySystemSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Sizzle/Ability System", SettingsScope.Project)
            {
                label = "Ability System",
                guiHandler = (searchContext) =>
                {
                    var settings = AbilitySystemEditorSettings.instance;
                    
                    DrawPathField("Generated File Path", "자동 생성되는 AbilityTags 스크립트의 경로입니다.", 
                        settings.GeneratedFilePath, 
                        (newPath) => settings.GeneratedFilePath = newPath);

                    DrawPathField("Custom File Path", "커스텀 태그를 정의하는 스크립트의 경로입니다.", 
                        settings.CustomFilePath, 
                        (newPath) => settings.CustomFilePath = newPath);
                },
                keywords = new HashSet<string>(new[] { "Ability", "Tag", "Path", "Sizzle" })
            };

            return provider;
        }

        private static void DrawPathField(string label, string tooltip, string currentPath, global::System.Action<string> onPathChanged)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUI.BeginChangeCheck();
            string newPath = EditorGUILayout.TextField(new GUIContent(label, tooltip), currentPath);
            if (EditorGUI.EndChangeCheck())
            {
                onPathChanged(newPath);
            }

            bool exists = global::System.IO.File.Exists(currentPath);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUI.BeginDisabledGroup(exists);
            if (GUILayout.Button("생성하기", GUILayout.Width(80)))
            {
                // 생성하기는 Generator를 실행하여 파일을 만듦
                AbilityGameTagCodeGenerator.Generate();
                GUIUtility.ExitGUI(); // UI 레이아웃 갱신 방지
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!exists);
            if (GUILayout.Button("옮기기", GUILayout.Width(80)))
            {
                string directory = "Assets";
                string fileName = "AbilityTags";
                if (!string.IsNullOrEmpty(currentPath))
                {
                    directory = global::System.IO.Path.GetDirectoryName(currentPath);
                    fileName = global::System.IO.Path.GetFileNameWithoutExtension(currentPath);
                }

                string selectedPath = EditorUtility.SaveFilePanelInProject($"[{label}] 위치 이동", fileName, "cs", "새로운 위치를 지정하세요", directory);
                if (!string.IsNullOrEmpty(selectedPath) && selectedPath != currentPath)
                {
                    string error = AssetDatabase.MoveAsset(currentPath, selectedPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        onPathChanged(selectedPath);
                    }
                    else
                    {
                        Debug.LogError($"파일 이동 실패: {error}");
                    }
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }
    }
}
