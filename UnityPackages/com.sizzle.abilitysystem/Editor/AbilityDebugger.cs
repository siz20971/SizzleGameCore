using Sizzle.GameTagSystem;
using Sizzle.GameTagSystem.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem.Editor
{
    public class AbilityDebugger : EditorWindow
    {
        [MenuItem(PATHS.MENUITEM_ROOT + "Ability Debugger")]
        public static void ShowWindow()
        {
            GetWindow<AbilityDebugger>("Ability Debugger");
        }

        // ── State ─────────────────────────────────────────────────────
        private List<AbilityProcessor> m_abilityProcessors = new List<AbilityProcessor>();
        private AbilityProcessor m_selectedProcessor = null;
        private string m_tagInput = "";
        private float m_timedTagDuration = 3f;
        private Vector2 m_processorScrollPos;
        private Vector2 m_abilityScrollPos;
        private Vector2 m_tagScrollPos;
        private string m_lastActivationSource = "";
        private string m_lastActivationTagName = "";
        private string m_lastActivationAbilityName = "";
        private string m_lastActivationDetail = "";
        private AbilityActivateResult m_lastActivationResult = AbilityActivateResult.None;

        // ── Styles ────────────────────────────────────────────────────
        private GUIStyle m_selectedButtonStyle;
        private GUIStyle m_activeStyle;
        private GUIStyle m_inactiveStyle;
        private Texture2D m_selectedBgTex;

        private const float LIST_WIDTH = 200f;
        private const float PAD = 6f;

        private void OnEnable() => RefreshAbilityProcessors();
        private void Update() => Repaint();

        private void InitStyles()
        {
            if (m_selectedBgTex == null)
            {
                m_selectedBgTex = new Texture2D(1, 1);
                m_selectedBgTex.SetPixel(0, 0, new Color(0.22f, 0.48f, 0.22f));
                m_selectedBgTex.Apply();
            }

            if (m_selectedButtonStyle == null)
            {
                m_selectedButtonStyle = new GUIStyle(GUI.skin.button);
                m_selectedButtonStyle.normal.background = m_selectedBgTex;
                m_selectedButtonStyle.normal.textColor = Color.white;
                m_selectedButtonStyle.fontStyle = FontStyle.Bold;
            }

            if (m_activeStyle == null)
            {
                m_activeStyle = new GUIStyle(EditorStyles.label);
                m_activeStyle.normal.textColor = new Color(0.35f, 1f, 0.35f);
                m_activeStyle.fontStyle = FontStyle.Bold;
            }

            if (m_inactiveStyle == null)
            {
                m_inactiveStyle = new GUIStyle(EditorStyles.label);
                m_inactiveStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            GUILayout.BeginArea(new Rect(PAD, PAD, LIST_WIDTH - PAD, position.height - PAD * 2), EditorStyles.helpBox);
            DrawProcessorListPanel();
            GUILayout.EndArea();

            float rightX = LIST_WIDTH + PAD;
            GUILayout.BeginArea(new Rect(rightX, PAD, position.width - rightX - PAD, position.height - PAD * 2), EditorStyles.helpBox);
            DrawDetailPanel();
            GUILayout.EndArea();
        }

        // ── Left Panel ────────────────────────────────────────────────

        private void DrawProcessorListPanel()
        {
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Height(22)))
                RefreshAbilityProcessors();

            DrawHLine();

            EditorGUILayout.LabelField("Processors", EditorStyles.boldLabel);
            DrawHLine();

            m_processorScrollPos = EditorGUILayout.BeginScrollView(m_processorScrollPos);
            foreach (AbilityProcessor processor in m_abilityProcessors)
            {
                if (processor == null) continue;
                bool selected = processor.Equals(m_selectedProcessor);
                if (GUILayout.Button(processor.name, selected ? m_selectedButtonStyle : GUI.skin.button))
                    m_selectedProcessor = processor;
            }

            if (m_abilityProcessors.Count == 0)
                EditorGUILayout.HelpBox("없음", MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ── Right Panel ───────────────────────────────────────────────

        private void DrawDetailPanel()
        {
            if (m_selectedProcessor == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("좌측에서 AbilityProcessor를 선택하세요.", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            EditorGUILayout.LabelField($"  ◎  {m_selectedProcessor.name}", EditorStyles.boldLabel);
            DrawHLine();

            GUILayout.Space(4);
            DrawSectionHeader("Abilities");
            DrawAbilitiesSection();

            GUILayout.Space(6);
            DrawSectionHeader("Last Activation");
            DrawLastActivationSection();

            GUILayout.Space(6);
            DrawSectionHeader("Tag Operations");
            DrawTagOperationsSection();

            GUILayout.Space(6);
            DrawSectionHeader("Own Tags");
            DrawOwnTagsSection();
        }

        // ── Abilities Section ─────────────────────────────────────────

        private void DrawAbilitiesSection()
        {
            IList<AbilityRuntimeContext> contexts = m_selectedProcessor.GetAllAbilityContexts();

            if (contexts.Count == 0)
            {
                EditorGUILayout.LabelField("  등록된 어빌리티 없음", EditorStyles.miniLabel);
                return;
            }

            m_abilityScrollPos = EditorGUILayout.BeginScrollView(m_abilityScrollPos, GUILayout.MaxHeight(150));
            foreach (AbilityRuntimeContext ctx in contexts)
            {
                if (ctx?.Ability == null) continue;
                DrawAbilityRow(ctx);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAbilityRow(AbilityRuntimeContext ctx)
        {
            bool active = ctx.IsActive;
            Ability ability = ctx.Ability;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.2f, 0.55f, 0.2f) : new Color(0.22f, 0.22f, 0.22f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            GUILayout.Label(active ? "●" : "○", active ? m_activeStyle : m_inactiveStyle, GUILayout.Width(14));
            EditorGUILayout.LabelField(ability.name, active ? m_activeStyle : m_inactiveStyle, GUILayout.MinWidth(80));
            GUILayout.FlexibleSpace();

            string tagName = ability.MainTag.TagName;
            if (!string.IsNullOrEmpty(tagName))
                GUILayout.Label(tagName, EditorStyles.miniLabel, GUILayout.MaxWidth(180));

            if (GUILayout.Button("Select", EditorStyles.miniButtonLeft, GUILayout.Width(48)))
                SelectAbilityAsset(ability);

            if (GUILayout.Button("Props", EditorStyles.miniButtonMid, GUILayout.Width(48)))
                OpenAbilityProperties(ability);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Activate", EditorStyles.miniButtonRight, GUILayout.Width(56)))
                    TryActivateAndRecord(ability.MainTag, ability, "Ability Row");
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Tag Operations Section ────────────────────────────────────

        private void DrawTagOperationsSection()
        {
            GUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag", GUILayout.Width(32));
            m_tagInput = EditorGUILayout.TextField(m_tagInput);
            if (GUILayout.Button("Pick", EditorStyles.miniButtonLeft, GUILayout.Width(42)))
                ShowCachedTagMenu(m_tagInput);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.miniButtonRight, GUILayout.Width(24)))
                RefreshCachedTagMenu();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            bool canOperate = Application.isPlaying && !string.IsNullOrWhiteSpace(m_tagInput);
            using (new EditorGUI.DisabledScope(!canOperate))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add", EditorStyles.miniButton))
                    m_selectedProcessor.TagContainer.AddTag(new GameTag(m_tagInput));
                if (GUILayout.Button("Remove", EditorStyles.miniButton))
                    m_selectedProcessor.TagContainer.RemoveTag(new GameTag(m_tagInput));
                if (GUILayout.Button("Notify", EditorStyles.miniButton))
                    m_selectedProcessor.TagContainer.NotifyTag(new GameTag(m_tagInput));
                if (GUILayout.Button("Activate", EditorStyles.miniButton))
                    TryActivateAndRecord(new GameTag(m_tagInput), null, "Tag Operations");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration (s)", GUILayout.Width(74));
                m_timedTagDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField(m_timedTagDuration, GUILayout.Width(50)));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Timed", EditorStyles.miniButton, GUILayout.Width(76)))
                    m_selectedProcessor.TagContainer.AddTagTimed(new GameTag(m_tagInput), m_timedTagDuration);
                EditorGUILayout.EndHorizontal();
            }

            if (!Application.isPlaying)
            {
                GUILayout.Space(2);
                EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다.", MessageType.None);
            }
        }

        // ── Own Tags Section ──────────────────────────────────────────

        private void DrawOwnTagsSection()
        {
            IList<GameTag> ownTags = m_selectedProcessor.TagContainer.GetOwnTags();

            if (ownTags.Count == 0)
            {
                EditorGUILayout.LabelField("  (없음)", EditorStyles.miniLabel);
                return;
            }

            m_tagScrollPos = EditorGUILayout.BeginScrollView(m_tagScrollPos, GUILayout.MaxHeight(120));
            foreach (GameTag tag in ownTags)
            {
                int stack = m_selectedProcessor.TagContainer.GetTagStack(tag);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("  •", GUILayout.Width(16));
                EditorGUILayout.LabelField(tag.TagName);
                GUILayout.FlexibleSpace();
                if (stack > 1)
                    EditorGUILayout.LabelField($"x{stack}", EditorStyles.miniLabel, GUILayout.Width(28));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawLastActivationSection()
        {
            if (string.IsNullOrEmpty(m_lastActivationSource))
            {
                EditorGUILayout.LabelField("  아직 활성화 시도 기록이 없습니다.", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField($"Source: {m_lastActivationSource}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Result: {m_lastActivationResult}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Tag: {m_lastActivationTagName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Ability: {m_lastActivationAbilityName}", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(m_lastActivationDetail, GetActivationResultMessageType(m_lastActivationResult));
        }

        // ── Utilities ─────────────────────────────────────────────────

        private void DrawSectionHeader(string title)
        {
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2);
            EditorGUI.DrawRect(r, new Color(0.13f, 0.13f, 0.13f, 0.8f));
            EditorGUI.LabelField(r, $"  {title}", EditorStyles.boldLabel);
        }

        private void DrawHLine()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        private static void SelectAbilityAsset(Ability ability)
        {
            if (!ability)
                return;

            Selection.activeObject = ability;
            EditorGUIUtility.PingObject(ability);
            EditorUtility.FocusProjectWindow();
        }

        private static void OpenAbilityProperties(Ability ability)
        {
            if (!ability)
                return;

            SelectAbilityAsset(ability);
            EditorUtility.OpenPropertyEditor(ability);
        }

        private void TryActivateAndRecord(GameTag tag, Ability ability, string source)
        {
            AbilityRuntimeContext context = m_selectedProcessor.GetAbilityContext(tag);
            Ability targetAbility = ability != null ? ability : context?.Ability;
            AbilityActivateResult result = m_selectedProcessor.TryActivateAbility(tag);

            m_lastActivationSource = source;
            m_lastActivationTagName = string.IsNullOrEmpty(tag.TagName) ? "(empty)" : tag.TagName;
            m_lastActivationAbilityName = targetAbility != null ? targetAbility.name : "(not found)";
            m_lastActivationResult = result;
            m_lastActivationDetail = BuildActivationDetail(tag, targetAbility, result, context?.IsActive ?? false);
            Repaint();
        }

        private string BuildActivationDetail(GameTag tag, Ability ability, AbilityActivateResult result, bool wasAlreadyActive)
        {
            switch (result)
            {
                case AbilityActivateResult.Success:
                    return ability != null
                        ? $"{ability.name} activation succeeded for tag '{tag.TagName}'."
                        : $"Activation succeeded for tag '{tag.TagName}'.";

                case AbilityActivateResult.FailedAbilityNotFound:
                    return $"No registered ability matched the exact MainTag '{tag.TagName}'.";

                case AbilityActivateResult.FailedAlreadyActive:
                    return ability != null
                        ? $"{ability.name} is already active and its ReactivationPolicy is Deny."
                        : "The target ability is already active and denied reactivation.";

                case AbilityActivateResult.FailedCanNotUse:
                    return ability != null
                        ? wasAlreadyActive && ability.ReactivationPolicy == AbilityReactivationPolicy.Reactivate
                            ? $"{ability.name} rejected reactivation because CanActivate returned false."
                            : $"{ability.name} rejected activation because CanActivate returned false."
                        : "The target ability rejected activation because CanActivate returned false.";

                case AbilityActivateResult.FailedNotHasAllRequiredTag:
                    return BuildMissingRequiredTagsDetail(ability);

                case AbilityActivateResult.FailedHasAnyBlockTag:
                    return BuildBlockedTagsDetail(ability);

                case AbilityActivateResult.FailedBlockedByOther:
                    return BuildBlockingAbilityDetail(ability);

                case AbilityActivateResult.FailedInvalidActivateInfo:
                    return "The activation payload or runtime state was invalid.";

                default:
                    return $"Activation result: {result}";
            }
        }

        private string BuildMissingRequiredTagsDetail(Ability ability)
        {
            if (ability == null)
                return "Activation failed because required tags were missing.";

            List<string> missingTags = new List<string>();
            foreach (GameTag requiredTag in ability.TagSet.ActivationRequiredTags)
            {
                if (!m_selectedProcessor.TagContainer.HasExactTag(requiredTag))
                    missingTags.Add(requiredTag.TagName);
            }

            if (missingTags.Count == 0)
                return $"{ability.name} requires tags that are not currently satisfied.";

            return $"{ability.name} is missing required tags: {string.Join(", ", missingTags)}.";
        }

        private string BuildBlockedTagsDetail(Ability ability)
        {
            if (ability == null)
                return "Activation failed because one or more blocking tags are present.";

            List<string> blockingTags = new List<string>();
            foreach (GameTag blockedTag in ability.TagSet.ActivationBlockedTags)
            {
                if (m_selectedProcessor.TagContainer.HasExactTag(blockedTag))
                    blockingTags.Add(blockedTag.TagName);
            }

            if (blockingTags.Count == 0)
                return $"{ability.name} is blocked by one or more active tags.";

            return $"{ability.name} is blocked by tags: {string.Join(", ", blockingTags)}.";
        }

        private string BuildBlockingAbilityDetail(Ability ability)
        {
            if (ability == null)
                return "Activation failed because another active ability is blocking this tag family.";

            List<string> blockers = new List<string>();
            foreach (AbilityRuntimeContext context in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (context == null || context.Ability == null || !context.IsActive)
                    continue;

                foreach (GameTag blockTag in context.Ability.TagSet.BlockAbilitiesWithTag)
                {
                    if (ability.MainTag.ChildOf(blockTag))
                    {
                        blockers.Add($"{context.Ability.name} -> {blockTag.TagName}");
                        break;
                    }
                }
            }

            if (blockers.Count == 0)
                return $"{ability.name} is blocked by another active ability.";

            return $"{ability.name} is blocked by active abilities: {string.Join(", ", blockers)}.";
        }

        private static MessageType GetActivationResultMessageType(AbilityActivateResult result)
        {
            switch (result)
            {
                case AbilityActivateResult.Success:
                    return MessageType.Info;

                case AbilityActivateResult.None:
                    return MessageType.None;

                default:
                    return MessageType.Warning;
            }
        }

        private void RefreshCachedTagMenu()
        {
            GameTagUtils.CacheGameTagsInAbilityAssets();
        }

        private void ShowCachedTagMenu(string filterText)
        {
            IList<GameTag> allTags = GameTagCache.GetCachedGameTags();
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Manual/Input"), false, () =>
            {
                GUI.FocusControl(null);
            });

            if (allTags == null || allTags.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Cached Tags/No cached tags"));
                menu.ShowAsContext();
                return;
            }

            List<GameTag> childTags = new List<GameTag>();
            List<GameTag> containsTags = new List<GameTag>();

            foreach (GameTag tag in allTags)
            {
                if (string.IsNullOrEmpty(filterText))
                {
                    containsTags.Add(tag);
                    continue;
                }

                if (tag.ChildOfOrExact(filterText))
                    childTags.Add(tag);
                else if (tag.TagName.Contains(filterText))
                    containsTags.Add(tag);
            }

            if (childTags.Count > 0)
            {
                foreach (GameTag tag in childTags)
                    AddCachedTagMenuItem(menu, tag);
            }

            if (containsTags.Count > 0)
            {
                if (childTags.Count > 0)
                    menu.AddSeparator("Cached Tags/");

                foreach (GameTag tag in containsTags)
                    AddCachedTagMenuItem(menu, tag);
            }

            if (childTags.Count == 0 && containsTags.Count == 0)
                menu.AddDisabledItem(new GUIContent("Cached Tags/No matching tags"));

            menu.ShowAsContext();
        }

        private void AddCachedTagMenuItem(GenericMenu menu, GameTag tag)
        {
            string tagName = tag.TagName;
            menu.AddItem(new GUIContent($"Cached Tags/{tagName}"), m_tagInput == tagName, () =>
            {
                m_tagInput = tagName;
                Repaint();
            });
        }

        private void RefreshAbilityProcessors()
        {
            if (m_abilityProcessors == null) m_abilityProcessors = new();
            m_abilityProcessors.Clear();
            AbilityProcessor[] processors = FindObjectsByType<AbilityProcessor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AbilityProcessor p in processors)
                m_abilityProcessors.Add(p);

            if (m_selectedProcessor != null && !m_abilityProcessors.Contains(m_selectedProcessor))
                m_selectedProcessor = m_abilityProcessors.Count > 0 ? m_abilityProcessors[0] : null;
        }
    }
}
