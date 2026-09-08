using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sizzle.GameTagSystem.Editor
{
    [FilePath("ProjectSettings/GameTagSystemEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GameTagSystemEditorSettings : ScriptableSingleton<GameTagSystemEditorSettings>
    {
        [SerializeField] 
        private int m_hierarchyDepth = -1; // -1: 제한 없음, 0: 평면, 1: 1단계 분리, 2: 2단계 분리

        public int HierarchyDepth
        {
            get => m_hierarchyDepth;
            set
            {
                if (m_hierarchyDepth != value)
                {
                    m_hierarchyDepth = value;
                    Save(true);
                }
            }
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }

    public static class GameTagSystemSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Sizzle/Game Tag System", SettingsScope.Project)
            {
                label = "Game Tag System",
                guiHandler = (searchContext) =>
                {
                    var settings = GameTagSystemEditorSettings.instance;
                    
                    EditorGUI.BeginChangeCheck();
                    
                    GUIContent depthLabel = new GUIContent("Hierarchy Depth", 
                        "Sizzle 태그 드롭다운 메뉴를 구분자('.') 기준으로 몇 단계까지 계층(폴더)으로 분리할지 설정합니다.\n0이면 모두 평면 리스트로 표시되며, -1이면 모든 구분자를 계층으로 분리합니다.");
                    int newValue = EditorGUILayout.IntField(depthLabel, settings.HierarchyDepth);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.HierarchyDepth = newValue;
                    }
                },
                keywords = new HashSet<string>(new[] { "GameTag", "Sizzle", "Tag", "Menu" })
            };

            return provider;
        }
    }
}
