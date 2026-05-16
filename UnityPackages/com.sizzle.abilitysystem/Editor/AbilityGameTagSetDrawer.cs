using Sizzle.AbilitySystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityGameTagSet))]
public class AbilityGameTagSetDrawer : PropertyDrawer
{
    // ── Section 정의 ──────────────────────────────────────────────
    private struct SectionDef
    {
        public string FieldName;
        public string Label;
        public string Icon;      // 유니코드 아이콘
        public string Tooltip;   // 접힌 상태에서도 보이는 한줄 설명
        public Color AccentColor;
    }

    private static readonly SectionDef[] SECTIONS =
    {
        new SectionDef
        {
            FieldName  = "m_cancelAbilitiesWithTag",
            Label      = "Cancel Abilities With Tag",
            Icon       = "\u2716",  // ✖
            Tooltip    = "실행 시, 이 태그를 메인 태그로 가진 활성 어빌리티를 취소합니다.",
            AccentColor = new Color(0.90f, 0.35f, 0.35f)   // red
        },
        new SectionDef
        {
            FieldName  = "m_blockAbilitiesWithTag",
            Label      = "Block Abilities With Tag",
            Icon       = "\u26D4",  // ⛔
            Tooltip    = "실행 중, 이 태그를 메인 태그로 가진 어빌리티의 실행을 차단합니다.",
            AccentColor = new Color(0.95f, 0.60f, 0.25f)   // orange
        },
        new SectionDef
        {
            FieldName  = "m_activationOwnedTags",
            Label      = "Activation Owned Tags",
            Icon       = "\u2605",  // ★
            Tooltip    = "실행 중, Owner가 이 태그들을 보유합니다.",
            AccentColor = new Color(0.30f, 0.70f, 0.95f)   // blue
        },
        new SectionDef
        {
            FieldName  = "m_activationRequiredTags",
            Label      = "Activation Required Tags",
            Icon       = "\u2714",  // ✔
            Tooltip    = "Owner가 이 태그들을 모두 보유해야 실행할 수 있습니다.",
            AccentColor = new Color(0.35f, 0.80f, 0.45f)   // green
        },
        new SectionDef
        {
            FieldName  = "m_activationBlockedTags",
            Label      = "Activation Blocked Tags",
            Icon       = "\u2718",  // ✘
            Tooltip    = "Owner가 이 태그들 중 하나라도 보유하면 실행할 수 없습니다.",
            AccentColor = new Color(0.70f, 0.40f, 0.85f)   // purple
        }
    };

    // ── 상수 ──────────────────────────────────────────────────────
    private const float SECTION_PAD       = 4f;
    private const float SECTION_INNER_PAD = 3f;
    private const float ACCENT_BAR_WIDTH  = 3f;
    private const float TOOLTIP_HEIGHT    = 14f;

    // ── 캐시 (property path 단위) ─────────────────────────────────
    private readonly Dictionary<string, bool[]> m_foldoutMap = new();
    private readonly Dictionary<string, ReorderableList> m_listCache = new();

    // ═══════════════════════════════════════════════════════════════
    // GetPropertyHeight
    // ═══════════════════════════════════════════════════════════════
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        bool[] foldouts = GetFoldouts(property);
        float line   = EditorGUIUtility.singleLineHeight;
        float sp     = EditorGUIUtility.standardVerticalSpacing;

        // Main Tag
        float h = line + sp;

        // Sections
        for (int i = 0; i < SECTIONS.Length; i++)
            h += GetSectionHeight(property, foldouts, i);

        // Trigger Tag
        h += line + sp;

        return h;
    }

    // ═══════════════════════════════════════════════════════════════
    // OnGUI
    // ═══════════════════════════════════════════════════════════════
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool[] foldouts = GetFoldouts(property);

        EditorGUI.BeginProperty(position, label, property);

        float y      = position.y;
        float line   = EditorGUIUtility.singleLineHeight;
        float sp     = EditorGUIUtility.standardVerticalSpacing;

        // ── Main Tag ──────────────────────────────────────────────
        SerializedProperty mainTag = property.FindPropertyRelative("m_mainTag");
        Rect mainRect = new Rect(position.x, y, position.width, line);
        EditorGUI.PropertyField(mainRect, mainTag, new GUIContent("\u25C6  Main Tag", "이 어빌리티의 메인 태그"));
        y += line + sp;

        // ── Sections ──────────────────────────────────────────────
        for (int i = 0; i < SECTIONS.Length; i++)
        {
            float sectionH = GetSectionHeight(property, foldouts, i);
            Rect sectionRect = new Rect(position.x, y, position.width, sectionH);
            DrawSection(sectionRect, property, foldouts, i);
            y += sectionH;
        }

        // ── Trigger Tag ───────────────────────────────────────────
        SerializedProperty triggerTag = property.FindPropertyRelative("m_triggerTag");
        Rect triggerRect = new Rect(position.x, y, position.width, line);
        EditorGUI.PropertyField(triggerRect, triggerTag, new GUIContent("\u25B6  Trigger Tag", "이 태그가 Owner에 Notify 되면 어빌리티가 작동합니다."));

        EditorGUI.EndProperty();
    }

    // ═══════════════════════════════════════════════════════════════
    // Section 렌더링
    // ═══════════════════════════════════════════════════════════════
    private void DrawSection(Rect area, SerializedProperty property, bool[] foldouts, int index)
    {
        ref SectionDef sec = ref SECTIONS[index];
        SerializedProperty listProp = property.FindPropertyRelative(sec.FieldName);
        int count = listProp.arraySize;

        float line = EditorGUIUtility.singleLineHeight;
        float sp   = EditorGUIUtility.standardVerticalSpacing;

        // ── 배경 박스 ─────────────────────────────────────────────
        Rect boxRect = new Rect(area.x, area.y + SECTION_PAD * 0.5f, area.width, area.height - SECTION_PAD);
        DrawBoxBackground(boxRect, sec.AccentColor);

        // ── Foldout 헤더 ──────────────────────────────────────────
        float contentX = area.x + ACCENT_BAR_WIDTH + SECTION_INNER_PAD;
        float contentW = area.width - ACCENT_BAR_WIDTH - SECTION_INNER_PAD * 2f;

        // 아이콘 + 이름 + 개수 뱃지
        string headerText = $"{sec.Icon}  {sec.Label}";
        string badgeText  = count > 0 ? $"  [{count}]" : "";

        Rect foldoutRect = new Rect(contentX, area.y + SECTION_PAD, contentW, line);

        // 뱃지 색상으로 개수 표시
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            richText  = true
        };

        string richLabel = count > 0
            ? $"{headerText}<color=#{ColorToHex(sec.AccentColor)}>{badgeText}</color>"
            : headerText;

        foldouts[index] = EditorGUI.Foldout(foldoutRect, foldouts[index], new GUIContent(richLabel, sec.Tooltip), true, foldoutStyle);

        if (!foldouts[index])
        {
            // 접힌 상태에서 설명 한줄 표시
            Rect descRect = new Rect(contentX + 12f, area.y + SECTION_PAD + line, contentW - 12f, TOOLTIP_HEIGHT);
            GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontSize = 10,
                wordWrap = false
            };
            EditorGUI.LabelField(descRect, sec.Tooltip, descStyle);
            return;
        }

        // ── ReorderableList ───────────────────────────────────────
        string listKey = GetListKey(property, sec.FieldName);
        ReorderableList list = GetOrCreateList(listKey, property.serializedObject, listProp, sec);

        float listY = area.y + SECTION_PAD + line + sp;
        float listH = list.GetHeight();
        Rect listRect = new Rect(contentX, listY, contentW, listH);
        list.DoList(listRect);
    }

    private float GetSectionHeight(SerializedProperty property, bool[] foldouts, int index)
    {
        ref SectionDef sec = ref SECTIONS[index];
        float line = EditorGUIUtility.singleLineHeight;
        float sp   = EditorGUIUtility.standardVerticalSpacing;

        if (!foldouts[index])
        {
            // 헤더 + 설명 한줄 + 패딩
            return line + TOOLTIP_HEIGHT + SECTION_PAD + sp;
        }

        // 헤더 + 리스트 + 패딩
        SerializedProperty listProp = property.FindPropertyRelative(sec.FieldName);
        string listKey = GetListKey(property, sec.FieldName);
        ReorderableList list = GetOrCreateList(listKey, property.serializedObject, listProp, sec);

        return line + sp + list.GetHeight() + SECTION_PAD + sp;
    }

    // ═══════════════════════════════════════════════════════════════
    // 시각 요소
    // ═══════════════════════════════════════════════════════════════
    private static void DrawBoxBackground(Rect rect, Color accentColor)
    {
        // 전체 배경
        Color bgColor = EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.22f, 0.22f, 0.6f)
            : new Color(0.82f, 0.82f, 0.82f, 0.5f);
        EditorGUI.DrawRect(rect, bgColor);

        // 좌측 액센트 바
        Rect barRect = new Rect(rect.x, rect.y, ACCENT_BAR_WIDTH, rect.height);
        EditorGUI.DrawRect(barRect, accentColor);

        // 테두리 (상하 라인)
        Color borderColor = EditorGUIUtility.isProSkin
            ? new Color(0.15f, 0.15f, 0.15f, 0.4f)
            : new Color(0.70f, 0.70f, 0.70f, 0.4f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
    }

    // ═══════════════════════════════════════════════════════════════
    // 캐시 유틸
    // ═══════════════════════════════════════════════════════════════
    private bool[] GetFoldouts(SerializedProperty property)
    {
        string key = property.propertyPath;
        if (!m_foldoutMap.TryGetValue(key, out bool[] foldouts) || foldouts.Length != SECTIONS.Length)
        {
            foldouts = new bool[SECTIONS.Length];
            m_foldoutMap[key] = foldouts;
        }
        return foldouts;
    }

    private static string GetListKey(SerializedProperty property, string fieldName)
        => $"{property.serializedObject.targetObject.GetHashCode()}:{property.propertyPath}.{fieldName}";

    private ReorderableList GetOrCreateList(string key, SerializedObject so, SerializedProperty listProp, in SectionDef sec)
    {
        if (m_listCache.TryGetValue(key, out ReorderableList cached) && cached.serializedProperty.serializedObject == so)
            return cached;

        var list = new ReorderableList(so, listProp, true, false, true, true)
        {
            elementHeightCallback = idx =>
                EditorGUI.GetPropertyHeight(listProp.GetArrayElementAtIndex(idx)) + EditorGUIUtility.standardVerticalSpacing,

            drawElementCallback = (rect, idx, active, focused) =>
            {
                rect.y += EditorGUIUtility.standardVerticalSpacing * 0.5f;
                rect.height -= EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, listProp.GetArrayElementAtIndex(idx), GUIContent.none);
            },

            drawNoneElementCallback = rect =>
            {
                GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontStyle = FontStyle.Italic };
                EditorGUI.LabelField(rect, "비어 있음 — + 버튼으로 태그를 추가하세요", style);
            }
        };

        m_listCache[key] = list;
        return list;
    }

    private static string ColorToHex(Color c)
        => $"{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
}