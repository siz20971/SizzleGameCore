using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sizzle.AbilitySystem.Editor
{
    public class AbilityScriptGeneratorWindow : EditorWindow
    {
        private string m_namespace = "Sizzle.Gameplay.Abilities";
        private string m_savePath = "Assets/Scripts/Abilities";
        private string m_menuPath = "Sizzle/Abilities/";
        
        private string m_abilityName = "NewAbility";
        private bool m_includeStateStruct = true;

        private List<TemplateInfo> m_templates = new List<TemplateInfo>();
        private int m_selectedTemplateIndex = 0;

        private struct TemplateInfo
        {
            public string DisplayName;
            public Type BaseType;
            public int GenericArgCount;
        }

        [MenuItem("Tools/Sizzle/AbilitySystem/New Ability Script...")]
        public static void ShowWindow()
        {
            var window = GetWindow<AbilityScriptGeneratorWindow>("New Ability Script");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPrefs();
            ScanTemplates();
        }

        private void OnDisable()
        {
            SavePrefs();
        }

        private void ScanTemplates()
        {
            m_templates.Clear();

            var types = TypeCache.GetTypesWithAttribute<AbilityTemplateAttribute>();
            foreach (var type in types)
            {
                if (!type.IsGenericTypeDefinition)
                    continue; // Must be generic to inject Context/Payload

                var attr = type.GetCustomAttribute<AbilityTemplateAttribute>();
                m_templates.Add(new TemplateInfo
                {
                    DisplayName = attr.DisplayName,
                    BaseType = type,
                    GenericArgCount = type.GetGenericArguments().Length
                });
            }

            m_templates = m_templates.OrderBy(t => t.DisplayName).ToList();
        }

        private void LoadPrefs()
        {
            string prefix = Application.productName + ".AbilityGenerator.";
            m_namespace = EditorPrefs.GetString(prefix + "Namespace", "Sizzle.Gameplay.Abilities");
            m_savePath = EditorPrefs.GetString(prefix + "SavePath", "Assets/Scripts/Abilities");
            m_menuPath = EditorPrefs.GetString(prefix + "MenuPath", "Sizzle/Abilities/");
            m_includeStateStruct = EditorPrefs.GetBool(prefix + "IncludeState", true);
        }

        private void SavePrefs()
        {
            string prefix = Application.productName + ".AbilityGenerator.";
            EditorPrefs.SetString(prefix + "Namespace", m_namespace);
            EditorPrefs.SetString(prefix + "SavePath", m_savePath);
            EditorPrefs.SetString(prefix + "MenuPath", m_menuPath);
            EditorPrefs.SetBool(prefix + "IncludeState", m_includeStateStruct);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ability Script Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("입력한 설정에 따라 Ability, Context, State, Payload 보일러플레이트 코드를 일괄 생성합니다.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            m_abilityName = EditorGUILayout.TextField("Ability Name", m_abilityName);
            if (!string.IsNullOrEmpty(m_abilityName) && m_abilityName.EndsWith("Ability"))
            {
                // To avoid MyAbilityAbility
                m_abilityName = m_abilityName.Substring(0, m_abilityName.Length - "Ability".Length);
            }

            EditorGUILayout.Space();

            m_namespace = EditorGUILayout.TextField("Namespace", m_namespace);
            
            EditorGUILayout.BeginHorizontal();
            m_savePath = EditorGUILayout.TextField("Save Path", m_savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        m_savePath = "Assets" + path.Substring(Application.dataPath.Length);
                    else
                        m_savePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            m_menuPath = EditorGUILayout.TextField("CreateAssetMenu Path", m_menuPath);
            
            EditorGUILayout.Space();
            m_includeStateStruct = EditorGUILayout.Toggle("Include State (struct)", m_includeStateStruct);

            if (m_templates.Count > 0)
            {
                string[] templateNames = m_templates.Select(t => t.DisplayName).ToArray();
                m_selectedTemplateIndex = EditorGUILayout.Popup("Base Template", m_selectedTemplateIndex, templateNames);
            }
            else
            {
                EditorGUILayout.HelpBox("No [AbilityTemplate] classes found in project.", MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
            }

            GUILayout.FlexibleSpace();

            GUI.enabled = m_templates.Count > 0 && !string.IsNullOrEmpty(m_abilityName);
            if (GUILayout.Button("Generate Script", GUILayout.Height(30)))
            {
                GenerateScript();
            }
            GUI.enabled = true;
            EditorGUILayout.Space();
        }

        private void GenerateScript()
        {
            if (!Directory.Exists(m_savePath))
            {
                Directory.CreateDirectory(m_savePath);
            }

            string className = m_abilityName + "Ability";
            string filePath = Path.Combine(m_savePath, className + ".cs");

            if (File.Exists(filePath))
            {
                if (!EditorUtility.DisplayDialog("File exists", $"The file {className}.cs already exists. Overwrite?", "Yes", "No"))
                    return;
            }

            var template = m_templates[m_selectedTemplateIndex];
            string cleanBaseName = GetCleanTypeName(template.BaseType);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Sizzle.AbilitySystem;");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(m_namespace))
            {
                sb.AppendLine($"namespace {m_namespace}");
                sb.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(m_namespace) ? "" : "    ";

            // State
            if (m_includeStateStruct)
            {
                sb.AppendLine($"{indent}public struct {m_abilityName}State");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    // Add variables that should be reset automatically here");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine();
            }

            // Context
            if (m_includeStateStruct)
                sb.AppendLine($"{indent}public class {m_abilityName}Context : AbilityRuntimeContext<{m_abilityName}State>");
            else
                sb.AppendLine($"{indent}public class {m_abilityName}Context : AbilityRuntimeContext");
            
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    // Add persistent cache variables here (e.g., Transform, Components)");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Payload
            bool hasPayload = template.GenericArgCount == 2;
            if (hasPayload)
            {
                sb.AppendLine($"{indent}public class {m_abilityName}Payload : AbilityActivatePayload");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    // Add activation parameters here");
                sb.AppendLine($"{indent}}}");
                sb.AppendLine();
            }

            // Ability Class
            string menuPathFull = m_menuPath.EndsWith("/") ? m_menuPath + m_abilityName : m_menuPath;
            sb.AppendLine($"{indent}[CreateAbilityAssetMenu(\"{menuPathFull}\")]");

            string payloadTypeString = hasPayload ? $", {m_abilityName}Payload" : "";
            sb.AppendLine($"{indent}public class {m_abilityName}Ability : {cleanBaseName}<{m_abilityName}Context{payloadTypeString}>");
            sb.AppendLine($"{indent}{{");
            
            string payloadParamType = hasPayload ? $"{m_abilityName}Payload" : "AbilityActivatePayload";
            
            sb.AppendLine($"{indent}    protected override void OnActivate({m_abilityName}Context context, {payloadParamType} payload)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    protected override void OnDeactivate(AbilityEndReason endReason, {m_abilityName}Context context)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(m_namespace))
            {
                sb.AppendLine("}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Ability script generated at {filePath}", "OK");
        }

        private string GetCleanTypeName(Type type)
        {
            string name = type.Name;
            int backtickIndex = name.IndexOf('`');
            if (backtickIndex > 0)
            {
                name = name.Substring(0, backtickIndex);
            }
            return name;
        }
    }
}
