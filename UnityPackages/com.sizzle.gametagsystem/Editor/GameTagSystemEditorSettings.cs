using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sizzle.GameTagSystem.Editor
{
    [FilePath("ProjectSettings/GameTagSystemEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GameTagSystemEditorSettings : ScriptableSingleton<GameTagSystemEditorSettings>
    {
        [SerializeField] 
        private bool m_useHierarchicalMenu = true;

        public bool UseHierarchicalMenu
        {
            get => m_useHierarchicalMenu;
            set
            {
                if (m_useHierarchicalMenu != value)
                {
                    m_useHierarchicalMenu = value;
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
                    
                    GUIContent toggleLabel = new GUIContent("Use Hierarchical Menu", 
                        "Sizzle 태그 드롭다운 메뉴를 구분자('.') 기준으로 단계별(폴더형) 메뉴로 묶어서 표시할지 여부를 설정합니다.\n해제 시 모든 태그가 하나의 리스트로 평평하게 표시됩니다.");
                    bool newValue = EditorGUILayout.Toggle(toggleLabel, settings.UseHierarchicalMenu);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.UseHierarchicalMenu = newValue;
                    }
                },
                keywords = new HashSet<string>(new[] { "GameTag", "Sizzle", "Tag", "Menu" })
            };

            return provider;
        }
    }
}
