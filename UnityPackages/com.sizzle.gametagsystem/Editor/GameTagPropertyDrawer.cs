using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sizzle.GameTagSystem.Editor
{
    [CustomPropertyDrawer(typeof(GameTag))]
    public class GameTagPropertyDrawer : PropertyDrawer
    {
        public static int hierarchyDepth
        {
            get => GameTagSystemEditorSettings.instance.HierarchyDepth;
            set => GameTagSystemEditorSettings.instance.HierarchyDepth = value;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = GetValueProperty(property);
            if (valueProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Use GameTag or string field");
                return;
            }

            GameTagOptionAttribute option = GetOptionAttribute(property);
            string currentVal = valueProperty.stringValue ?? string.Empty;
            bool isInvalid = !IsValidTagValue(currentVal, option, out string invalidReason);

            float buttonWidth = 30f;
            float warningIconWidth = isInvalid ? 22f : 0f;

            Rect textRect = new Rect(position.x, position.y, position.width - buttonWidth * 2 - warningIconWidth, position.height);
            Rect warningRect = isInvalid ? new Rect(position.x + position.width - buttonWidth * 2 - warningIconWidth, position.y, warningIconWidth, position.height) : Rect.zero;
            Rect btnFiltered = new Rect(position.x + position.width - buttonWidth * 2, position.y, buttonWidth, position.height);
            Rect btnTotal = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            GUIContent fieldLabel = new GUIContent(label.text, isInvalid ? invalidReason : label.tooltip);

            if (option != null && option.DropdownOnly)
            {
                Color prevColor = GUI.color;
                if (isInvalid)
                    GUI.color = new Color(1f, 0.75f, 0.75f);

                Rect contentRect = EditorGUI.PrefixLabel(textRect, fieldLabel);
                string displayLabel = string.IsNullOrEmpty(currentVal) ? "<None>" : currentVal;
                if (GUI.Button(contentRect, new GUIContent(displayLabel, isInvalid ? invalidReason : displayLabel), EditorStyles.popup))
                {
                    ShowTagMenu(valueProperty, "", option);
                }
                GUI.color = prevColor;
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Color prevColor = GUI.color;
                if (isInvalid)
                    GUI.color = new Color(1f, 0.75f, 0.75f); // 비정상 값인 경우 붉은 틴트

                string newText = EditorGUI.DelayedTextField(textRect, fieldLabel, currentVal);
                GUI.color = prevColor;

                if (EditorGUI.EndChangeCheck())
                {
                    string validated = ValidateAndFormatInput(newText, currentVal, option);
                    valueProperty.stringValue = validated;
                    valueProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            if (isInvalid)
            {
                GUIContent warnIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
                warnIcon.tooltip = invalidReason;
                GUI.Label(warningRect, warnIcon);
            }

            string filterTooltip = option != null && !string.IsNullOrEmpty(option.Parent)
                ? $"'{option.Parent}' 하위 태그 중 검색/필터링"
                : "입력값으로 필터링";
            string totalTooltip = option != null && !string.IsNullOrEmpty(option.Parent)
                ? $"'{option.Parent}' 하위 태그 목록"
                : "전체 태그 목록";

            if (GUI.Button(btnFiltered, new GUIContent("🔍", filterTooltip)))
                ShowTagMenu(valueProperty, valueProperty.stringValue, option);

            if (GUI.Button(btnTotal, new GUIContent("📋", totalTooltip)))
                ShowTagMenu(valueProperty, "", option);
        }

        private SerializedProperty GetValueProperty(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.String)
                return property;

            return property.FindPropertyRelative("m_tagName");
        }

        private GameTagOptionAttribute GetOptionAttribute(SerializedProperty property)
        {
            if (attribute is GameTagOptionAttribute attr)
                return attr;

            if (fieldInfo != null)
            {
                var customAttr = fieldInfo.GetCustomAttribute<GameTagOptionAttribute>(true);
                if (customAttr != null)
                    return customAttr;
            }

            return null;
        }

        private bool IsValidTagValue(string tagName, GameTagOptionAttribute option, out string reason)
        {
            reason = string.Empty;

            if (!GameTag.IsValidTagName(tagName, out string formatError))
            {
                reason = formatError;
                return false;
            }

            if (option == null || string.IsNullOrEmpty(option.Parent))
                return true;

            if (string.IsNullOrEmpty(tagName))
            {
                if (!option.AllowEmpty)
                {
                    reason = $"'{option.Parent}'의 하위 태그가 필요합니다 (빈 태그 불가).";
                    return false;
                }
                return true;
            }

            GameTag tag = new GameTag(tagName);
            bool matches = option.IncludeParent
                ? tag.ChildOfOrExact(option.Parent)
                : tag.StrictChildOf(option.Parent);

            if (!matches)
            {
                reason = $"'{tagName}'은(는) '{option.Parent}'의 하위 태그가 아닙니다.";
                return false;
            }

            return true;
        }

        private static string ValidateAndFormatInput(string input, string oldValue, GameTagOptionAttribute option)
        {
            input = (input ?? string.Empty).Trim();

            // 유효하지 않은 문자 정제
            if (!GameTag.IsValidTagName(input, out string formatError))
            {
                string sanitized = GameTag.SanitizeTagName(input);
                Debug.LogWarning($"[GameTag] '{input}'에 허용되지 않은 문자가 포함되어 정제되었습니다 ('{sanitized}'): {formatError}");
                input = sanitized;
            }

            if (option == null || string.IsNullOrEmpty(option.Parent))
                return input;

            // 1. 빈 문자열 처리
            if (string.IsNullOrEmpty(input))
            {
                if (option.AllowEmpty)
                    return string.Empty;

                Debug.LogWarning($"[GameTagOption] 빈 태그 입력이 허용되지 않습니다. 이전 값('{oldValue}')으로 복원합니다.");
                return oldValue;
            }

            // 2. 부모 자체인 경우
            if (string.Equals(input, option.Parent, StringComparison.Ordinal))
            {
                if (option.IncludeParent)
                    return input;

                Debug.LogWarning($"[GameTagOption] 부모 태그('{option.Parent}') 자체는 허용되지 않으며 하위 태그만 입력할 수 있습니다. 이전 값('{oldValue}')으로 복원합니다.");
                return oldValue;
            }

            // 3. 이미 부모의 엄격한 하위 태그인 경우 (예: "Status.Buff.A")
            GameTag tag = new GameTag(input);
            if (tag.StrictChildOf(option.Parent))
            {
                return input;
            }

            // 4. 부모로 시작하지 않는 경우 -> 부모 강제 제한 여부 확인
            if (!option.RestrictToParent)
            {
                return input;
            }

            // 점(.)으로 구분하여 첫 번째 세그먼트 확인
            string[] parentSegments = option.Parent.Split(GameTag.SEPARATOR);
            string[] inputSegments = input.Split(GameTag.SEPARATOR);

            // 계층이 2단계 이상이고 첫 세그먼트가 부모와 다르면 (예: "Skill.Attack") 명백히 다른 계층이므로 차단
            if (inputSegments.Length > 1 && !string.Equals(inputSegments[0], parentSegments[0], StringComparison.Ordinal))
            {
                Debug.LogWarning($"[GameTagOption] '{input}'은(는) 부모 '{option.Parent}'의 하위 태그가 아닙니다. 입력을 취소하고 이전 값('{oldValue}')으로 복원합니다.");
                return oldValue;
            }

            // 5. 하위 이름만 입력한 경우 자동 완성 (예: "A" -> "Status.Buff.A")
            string combined;
            if (input.StartsWith(option.Parent, StringComparison.Ordinal))
            {
                combined = input;
            }
            else
            {
                combined = $"{option.Parent}.{input.TrimStart(GameTag.SEPARATOR)}";
            }

            return combined;
        }

        private void ShowTagMenu(SerializedProperty valueProperty, string filterText, GameTagOptionAttribute option)
        {
            IList<GameTag> tags = GameTagCache.GetCachedGameTags();

            if (tags == null || tags.Count == 0)
            {
                Debug.LogError("GameTagCache가 비어있습니다. GameTagCache class 함수들을 통해, 캐시를 채워주세요");
                return;
            }

            GenericMenu menu = new GenericMenu();
            int depth = hierarchyDepth;
            string currentVal = valueProperty.stringValue ?? string.Empty;

            // 1. 부모 필터링 적용
            bool hasParent = option != null && !string.IsNullOrEmpty(option.Parent);
            string parent = hasParent ? option.Parent : string.Empty;
            bool includeParent = option != null && option.IncludeParent;
            bool relativePath = option == null || option.RelativePathInMenu;
            bool allowEmpty = option == null || option.AllowEmpty;

            IEnumerable<GameTag> candidateQuery = tags;
            if (hasParent)
            {
                candidateQuery = candidateQuery.Where(t =>
                    includeParent ? t.ChildOfOrExact(parent) : t.StrictChildOf(parent));
            }

            List<GameTag> targetTags = candidateQuery.OrderBy(t => t.TagName).ToList();

            // 2. 상단 헤더 및 비우기 항목
            if (hasParent)
            {
                menu.AddDisabledItem(new GUIContent($"[Parent: {parent}]"));
                menu.AddSeparator("");
            }

            if (allowEmpty)
            {
                menu.AddItem(new GUIContent("<None>"), string.IsNullOrEmpty(currentVal), () =>
                {
                    valueProperty.stringValue = string.Empty;
                    valueProperty.serializedObject.ApplyModifiedProperties();
                });
                menu.AddSeparator("");
            }

            if (targetTags.Count == 0)
            {
                string emptyMsg = hasParent
                    ? $"'{parent}' 하위 태그가 캐시에 없습니다."
                    : "사용 가능한 태그가 없습니다.";
                menu.AddDisabledItem(new GUIContent(emptyMsg));
                menu.ShowAsContext();
                return;
            }

            // 3. 필터링 여부에 따른 분기
            if (string.IsNullOrEmpty(filterText))
            {
                foreach (GameTag gameTag in targetTags)
                {
                    string fullTagName = gameTag.TagName;
                    string displayTagName = GetMenuDisplayName(fullTagName, parent, relativePath, depth);

                    menu.AddItem(new GUIContent(displayTagName),
                        currentVal.Equals(fullTagName, StringComparison.Ordinal),
                        () =>
                        {
                            valueProperty.stringValue = fullTagName;
                            valueProperty.serializedObject.ApplyModifiedProperties();
                        });
                }
            }
            else
            {
                List<GameTag> childAssetTags = new List<GameTag>();
                List<GameTag> containsAssetTags = new List<GameTag>();

                foreach (GameTag tag in targetTags)
                {
                    if (tag.ChildOfOrExact(filterText))
                        childAssetTags.Add(tag);
                    else if (tag.TagName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                        containsAssetTags.Add(tag);
                }

                if (childAssetTags.Count > 0)
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("-- Child Of --"));

                    foreach (var gameTag in childAssetTags)
                    {
                        string fullTagName = gameTag.TagName;
                        string displayTagName = GetMenuDisplayName(fullTagName, parent, relativePath, depth);
                        menu.AddItem(new GUIContent(displayTagName),
                            currentVal.Equals(fullTagName, StringComparison.Ordinal),
                            () =>
                            {
                                valueProperty.stringValue = fullTagName;
                                valueProperty.serializedObject.ApplyModifiedProperties();
                            });
                    }
                }

                if (containsAssetTags.Count > 0)
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("-- Contains --"));

                    foreach (var gameTag in containsAssetTags)
                    {
                        string fullTagName = gameTag.TagName;
                        string displayTagName = GetMenuDisplayName(fullTagName, parent, relativePath, depth);
                        menu.AddItem(new GUIContent(displayTagName),
                            currentVal.Equals(fullTagName, StringComparison.Ordinal),
                            () =>
                            {
                                valueProperty.stringValue = fullTagName;
                                valueProperty.serializedObject.ApplyModifiedProperties();
                            });
                    }
                }
            }

            menu.ShowAsContext();
        }

        private string GetMenuDisplayName(string fullTagName, string parent, bool relativePath, int depth)
        {
            if (string.IsNullOrEmpty(parent) || !relativePath)
            {
                return GetDisplayTagName(fullTagName, depth);
            }

            if (string.Equals(fullTagName, parent, StringComparison.Ordinal))
            {
                return $"<{parent}> (Parent)";
            }

            string prefix = parent + GameTag.SEPARATOR;
            if (fullTagName.StartsWith(prefix, StringComparison.Ordinal))
            {
                string subPath = fullTagName.Substring(prefix.Length);
                return GetDisplayTagName(subPath, depth);
            }

            return GetDisplayTagName(fullTagName, depth);
        }

        private string GetDisplayTagName(string tagName, int depth)
        {
            if (depth == 0)
                return tagName;

            if (depth < 0)
                return tagName.Replace(GameTag.SEPARATOR, '/');

            var chars = tagName.ToCharArray();
            int replaceCount = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == GameTag.SEPARATOR)
                {
                    chars[i] = '/';
                    replaceCount++;
                    if (replaceCount >= depth)
                        break;
                }
            }
            return new string(chars);
        }
    }

    /// <summary>
    /// GameTagOptionAttribute가 적용된 필드 전용 PropertyDrawer입니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameTagOptionAttribute))]
    public class GameTagOptionDrawer : GameTagPropertyDrawer
    {
    }
}