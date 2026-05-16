using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sizzle.GameTagSystem.Editor
{
    [CustomPropertyDrawer(typeof(GameTag))]
    public class GameTagPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("m_tagName");

            float buttonWidth = 30;
            Rect textRect = new Rect(position.x, position.y, position.width - buttonWidth * 2, position.height);
            Rect btnFiltered = new Rect(position.x + position.width - buttonWidth * 2, position.y, buttonWidth, position.height);
            Rect btnTotal = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            EditorGUI.PropertyField(textRect, valueProperty, label);

            if (GUI.Button(btnFiltered, "\ud83d\udd0d"))
                ShowTagMenu(valueProperty, valueProperty.stringValue);

            if (GUI.Button(btnTotal, "📋"))
                ShowTagMenu(valueProperty, "");
        }

        private void ShowTagMenu(SerializedProperty valueProperty, string filterText)
        {
            IList<GameTag> tags = GameTagCache.GetCachedGameTags();
            
            if (tags == null || tags.Count == 0)
            {
                Debug.LogError("GameTagCache가 비어있습니다. GameTagCache class 함수들을 통해, 캐시를 채워주세요");
                return;
            }

            GenericMenu menu = new GenericMenu();
            
            IList<GameTag> allTags = GameTagCache.GetCachedGameTags();
            
            if (string.IsNullOrEmpty(filterText)) // 필터 없으면 전부 보여줌.
            {
                foreach (GameTag gameTag in allTags)
                {
                    string tagName = gameTag.TagName;
                    menu.AddItem(new GUIContent(tagName),
                        valueProperty.stringValue.Equals(tagName),
                        () =>
                        {
                            valueProperty.stringValue = tagName;
                            valueProperty.serializedObject.ApplyModifiedProperties();
                        });
                }
            }
            else // 필터 필요한 경우, Child 먼저 / Contains 이후
            {
                List<GameTag> childAssetTags = new List<GameTag>();
                List<GameTag> containsAssetTags = new List<GameTag>();
                
                foreach (GameTag tag in allTags)
                {
                    if (tag.ChildOfOrExact(filterText))
                        childAssetTags.Add(tag);
                    else if (tag.TagName.Contains(filterText))
                        containsAssetTags.Add(tag);
                }

                if (childAssetTags.Count > 0)
                {
                    menu.AddSeparator("-- Child Of --");
                    
                    foreach (var gameTag in childAssetTags)
                    {
                        string tagName = gameTag.TagName;
                        menu.AddItem(new GUIContent(tagName),
                            valueProperty.stringValue.Equals(tagName),
                            () =>
                            {
                                valueProperty.stringValue = tagName;
                                valueProperty.serializedObject.ApplyModifiedProperties();
                            });
                    }
                }
                
                if (containsAssetTags.Count > 0)
                {
                    menu.AddSeparator("-- Contains --");
                    
                    foreach (var gameTag in containsAssetTags)
                    {
                        string tagName = gameTag.TagName;
                        menu.AddItem(new GUIContent(tagName),
                            valueProperty.stringValue.Equals(tagName),
                            () =>
                            {
                                valueProperty.stringValue = tagName;
                                valueProperty.serializedObject.ApplyModifiedProperties();
                            });
                    }
                }
            }
            
            menu.ShowAsContext();
        }
    }
}