using Sizzle.GameTagSystem;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem.Editor
{
    public class AbilityGameTagViewer : EditorWindow
    {
        [MenuItem(PATHS.MENUITEM_ROOT + "Ability Tag Viewer")]
        public static void ShowWindow()
        {
            GetWindow<AbilityGameTagViewer>("Ability Tag Viewer");
        }

        private void OnEnable()
        {
            m_tagGraphDict = CreateGameTagGraph();
        }
        
        private const float LIST_AREA_WIDTH = 250;
        private const float PADDING = 5;
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(PADDING, PADDING, LIST_AREA_WIDTH - PADDING, position.height - PADDING * 2), EditorStyles.helpBox);
            DrawLeftPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(LIST_AREA_WIDTH + PADDING, PADDING, position.width - LIST_AREA_WIDTH - PADDING * 2, position.height - PADDING * 2), EditorStyles.helpBox);
            DrawRightPanel();
            GUILayout.EndArea();
        }

        private void DrawLeftPanel()
        {
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh")))
            {
                AbilityGameTagCodeGenerator.AskGenerate();
                m_tagGraphDict = CreateGameTagGraph();
            }
            
            if (GUILayout.Button("Export Text"))
            {
                string path = EditorUtility.SaveFilePanel("Save Text", "", "GameTagHierarchy", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    StringBuilder sb = new StringBuilder();
            
                    foreach (var kvp in m_tagGraphDict)
                    {
                        DrawNode(kvp.Value, sb, 0);
                    }
                    global::System.IO.File.WriteAllText(path, sb.ToString());
                }
            }

            EditorGUILayout.Separator();
        }

        private void DrawRightPanel()
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var kvp in m_tagGraphDict)
            {
                DrawNode(kvp.Value, sb, 0);
            }
            
            EditorGUILayout.TextArea(sb.ToString());
        }
        
        private void DrawNode(GameTagGraphNode node, StringBuilder sb, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                sb.Append("    ");
                if (i == (depth - 1))
                    sb.Append("-");
            }
            
            sb.AppendLine(node.TagName);
            
            foreach (var child in node.Children)
                DrawNode(child, sb, depth + 1);
        }
        
        private Dictionary<string, GameTagGraphNode> m_tagGraphDict = new Dictionary<string, GameTagGraphNode>();

        public class GameTagGraphNode
        {
            public string TagName;
            public List<GameTagGraphNode> Children = new List<GameTagGraphNode>();
            
            public GameTagGraphNode AddChildIfNotExist(string tagName)
            {
                foreach (var child in Children)
                {
                    if (child.TagName == tagName)
                        return child;
                }
                
                var newNode = new GameTagGraphNode() { TagName = tagName };
                Children.Add(newNode);
                return newNode;
            }
        }

        private Dictionary<string , GameTagGraphNode> CreateGameTagGraph()
        {
            List<GameTag> allTags = new List<GameTag>();
            GameTagUtils.GetCachedGameTags(out IList<GameTag> cachedTags);
            allTags.AddRange(cachedTags);

            Dictionary<string, GameTagGraphNode> tagGraphDict = new Dictionary<string, GameTagGraphNode>();

            foreach (GameTag tag in allTags)
            {
                IReadOnlyList<string> tagNameHierarchy = tag.GetTagNameHierarchyCached();

                if (tagNameHierarchy.Count == 0)
                    continue;

                GameTagGraphNode rootNode = null;
                GameTagGraphNode currentNode = null;

                string rootTagName = tagNameHierarchy[0];
                
                if (!tagGraphDict.TryGetValue(rootTagName, out rootNode))
                {
                    rootNode = new GameTagGraphNode();
                    rootNode.TagName = rootTagName;
                    tagGraphDict.Add(rootTagName, rootNode);
                }

                currentNode = rootNode;
                
                for (int i = 1; i < tagNameHierarchy.Count; i++)
                {
                    string tagName = tagNameHierarchy[i];
                    
                    GameTagGraphNode node = currentNode.AddChildIfNotExist(tagName);
                    currentNode = node;
                }
            }
            
            return tagGraphDict;
        }
    }
}