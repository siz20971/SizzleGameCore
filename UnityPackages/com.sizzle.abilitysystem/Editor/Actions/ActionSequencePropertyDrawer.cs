using Sizzle.AbilitySystem.Actions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sizzle.AbilitySystem.Editor
{
    /// <summary>
    /// ActionSequence 필드를 인스펙터에 노출할 때 표시되는 전용 PropertyDrawer입니다.
    /// 인스펙터에서 시퀀스 요약 정보를 표시하고, 원클릭으로 플로우 에디터 창을 열어 해당 시퀀스를 바로 편집할 수 있게 합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(ActionSequence))]
    public class ActionSequencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.18f, 0.2f, 0.24f, 0.6f),
                    borderTopLeftRadius = 6, borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6, borderBottomRightRadius = 6,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = new Color(0.28f, 0.35f, 0.45f),
                    borderBottomColor = new Color(0.28f, 0.35f, 0.45f),
                    borderLeftColor = new Color(0.28f, 0.35f, 0.45f),
                    borderRightColor = new Color(0.28f, 0.35f, 0.45f),
                    paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8,
                    marginBottom = 8, marginTop = 4
                }
            };

            var actionsProp = property.FindPropertyRelative("Actions");
            int actionCount = actionsProp != null ? actionsProp.arraySize : 0;

            // ── 헤더 라인 ──
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween
                }
            };

            // 왼쪽: 시퀀스 명칭 및 액션 수 뱃지
            var headerLeft = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };

            var titleLabel = new Label(property.displayName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    color = new Color(0.9f, 0.95f, 1f),
                    marginRight = 6
                }
            };
            headerLeft.Add(titleLabel);

            var countBadge = new Label($"[{actionCount} Actions]")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.8f, 0.95f),
                    backgroundColor = new Color(0.1f, 0.15f, 0.25f),
                    paddingTop = 2, paddingBottom = 2, paddingLeft = 6, paddingRight = 6,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4
                }
            };
            headerLeft.Add(countBadge);
            header.Add(headerLeft);

            // 오른쪽: [⚡ Edit Flow] 버튼
            var editFlowBtn = new Button(() =>
            {
                var targetObj = property.serializedObject.targetObject as ScriptableObject;
                if (targetObj != null)
                {
                    ActionSequenceFlowWindow.OpenSequence(targetObj, property.propertyPath);
                }
            })
            {
                text = "⚡ Edit Flow",
                tooltip = "전용 플로우 에디터 창을 열어 이 시퀀스를 시각적으로 편집합니다.",
                style =
                {
                    height = 22,
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = new Color(0.2f, 0.48f, 0.8f),
                    color = Color.white,
                    borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                    paddingLeft = 8, paddingRight = 8
                }
            };
            header.Add(editFlowBtn);
            container.Add(header);

            // ── 접이식 세부 프로퍼티 리스트 (인라인 확인용) ──
            if (actionsProp != null)
            {
                var foldout = new Foldout
                {
                    text = "Raw Actions List",
                    value = false,
                    style = { marginTop = 4, fontSize = 11, color = new Color(0.65f, 0.7f, 0.75f) }
                };

                var propertyField = new PropertyField(actionsProp);
                foldout.Add(propertyField);
                container.Add(foldout);
            }

            return container;
        }
    }
}
