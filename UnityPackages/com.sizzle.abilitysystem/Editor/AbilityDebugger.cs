using Sizzle.GameTagSystem;
using Sizzle.GameTagSystem.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem.Editor
{
    public class AbilityDebugger : EditorWindow
    {
        private enum DebuggerTab
        {
            Debugger,
            History,
        }

        private struct DebuggerEventEntry
        {
            public double Time;
            public string Category;
            public string Message;
        }

        private struct AbilityContextSnapshot
        {
            public bool IsActive;
            public AbilityEndReason PendingEndReason;
        }

        [MenuItem(PATHS.MENUITEM_ROOT + "Ability Debugger")]
        public static void ShowWindow()
        {
            GetWindow<AbilityDebugger>("Ability Debugger");
        }

        // ── State ─────────────────────────────────────────────────────
        private List<AbilityProcessor> m_abilityProcessors = new List<AbilityProcessor>();
        private AbilityProcessor m_selectedProcessor = null;
        private AbilityRuntimeContext m_selectedAbilityContext = null;
        private string m_tagInput = "";
        private string m_abilityFilter = "";
        private string m_historyFilter = "";
        private bool m_showActiveAbilitiesOnly = false;
        private bool m_showActivationHistoryOnly = false;
        private bool m_historyOptionsExpanded = true;
        private bool m_showSystemHistory = true;
        private bool m_showAbilityHistory = true;
        private bool m_showTagHistory = true;
        private bool m_showActivateHistory = true;
        private float m_timedTagDuration = 3f;
        private Vector2 m_processorScrollPos;
        private Vector2 m_abilityScrollPos;
        private Vector2 m_activeDetailScrollPos;
        private Vector2 m_historyScrollPos;
        private Vector2 m_tagScrollPos;
        private string m_lastActivationSource = "";
        private string m_lastActivationTagName = "";
        private string m_lastActivationAbilityName = "";
        private string m_lastActivationDetail = "";
        private AbilityActivateResult m_lastActivationResult = AbilityActivateResult.None;
        private DebuggerTab m_selectedTab = DebuggerTab.Debugger;
        private readonly List<DebuggerEventEntry> m_eventHistory = new List<DebuggerEventEntry>();
        private readonly Dictionary<AbilityRuntimeContext, AbilityContextSnapshot> m_contextSnapshots = new Dictionary<AbilityRuntimeContext, AbilityContextSnapshot>();

        // ── Styles ────────────────────────────────────────────────────
        private GUIStyle m_selectedButtonStyle;
        private GUIStyle m_activeStyle;
        private GUIStyle m_inactiveStyle;
        private Texture2D m_selectedBgTex;

        private const float LIST_WIDTH = 200f;
        private const float PAD = 6f;
        private const float TAB_HEIGHT = 22f;
        private const int MAX_HISTORY_ENTRIES = 100;

        private void OnEnable() => RefreshAbilityProcessors();

        private void OnDisable()
        {
            DetachProcessorEvents();
        }

        private void Update()
        {
            PollProcessorEvents();
            Repaint();
        }

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

            // 상단 툴바에서 오른쪽 패널을 실시간 디버깅 화면과 히스토리 화면으로 전환합니다.
            DrawTopTabs();

            float contentY = PAD + TAB_HEIGHT + PAD;
            float contentHeight = position.height - contentY - PAD;

            // 왼쪽 패널은 씬에 있는 AbilityProcessor 선택과 오브젝트 바로가기 버튼을 출력합니다.
            GUILayout.BeginArea(new Rect(PAD, contentY, LIST_WIDTH - PAD, contentHeight), EditorStyles.helpBox);
            DrawProcessorListPanel();
            GUILayout.EndArea();

            float rightX = LIST_WIDTH + PAD;
            // 오른쪽 패널은 현재 선택된 프로세서 기준의 디버그 정보를 출력합니다.
            GUILayout.BeginArea(new Rect(rightX, contentY, position.width - rightX - PAD, contentHeight), EditorStyles.helpBox);
            DrawDetailPanel();
            GUILayout.EndArea();
        }

        private void DrawTopTabs()
        {
            // 오른쪽 영역에서 사용할 메인 모드 탭을 출력합니다.
            Rect tabsRect = new Rect(PAD, PAD, position.width - PAD * 2, TAB_HEIGHT);
            m_selectedTab = (DebuggerTab)GUI.Toolbar(tabsRect, (int)m_selectedTab, new[] { "Debugger", "History" });
        }

        // ── Left Panel ────────────────────────────────────────────────

        private void DrawProcessorListPanel()
        {
            // 씬의 모든 AbilityProcessor를 나열하고 각 행마다 Pick/Ping 보조 버튼을 제공합니다.
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
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(processor.name, selected ? m_selectedButtonStyle : GUI.skin.button))
                    SetSelectedProcessor(processor);
                if (GUILayout.Button("Pick", EditorStyles.miniButtonLeft, GUILayout.Width(34)))
                    SelectProcessorObject(processor);
                if (GUILayout.Button("Ping", EditorStyles.miniButtonRight, GUILayout.Width(34)))
                    PingProcessorObject(processor);
                EditorGUILayout.EndHorizontal();
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

            if (m_selectedTab == DebuggerTab.History)
            {
                // History 탭에서는 선택된 프로세서의 이벤트 히스토리 전용 화면을 출력합니다.
                DrawHistoryTabPanel();
                return;
            }

            GUILayout.Space(4);
            // 어빌리티 목록과 개별 액션 버튼, 선택 상태를 출력합니다.
            DrawSectionHeader("Abilities");
            DrawAbilitiesSection();

            GUILayout.Space(6);
            // 플레이 모드에서 선택된 프로세서 상태를 직접 바꾸는 제어 버튼을 출력합니다.
            DrawSectionHeader("Controls");
            DrawControlsSection();

            GUILayout.Space(6);
            // 현재 활성 어빌리티들의 실시간 런타임 상태를 출력합니다.
            DrawSectionHeader("Active Details");
            DrawActiveAbilityDetailsSection();

            GUILayout.Space(6);
            // 선택했거나 마지막으로 다룬 어빌리티의 차단 원인 분석 정보를 출력합니다.
            DrawSectionHeader("Blocker Analysis");
            DrawBlockerAnalysisSection();

            GUILayout.Space(6);
            // 가장 최근의 활성화 시도와 그 결과 요약을 출력합니다.
            DrawSectionHeader("Last Activation");
            DrawLastActivationSection();

            GUILayout.Space(6);
            // 플레이 모드에서 태그 상태를 수동으로 바꾸기 위한 테스트 도구를 출력합니다.
            DrawSectionHeader("Tag Operations");
            DrawTagOperationsSection();

            GUILayout.Space(6);
            // 현재 정확히 보유 중인 태그와 timed tag 남은 시간 정보를 출력합니다.
            DrawSectionHeader("Own Tags");
            DrawOwnTagsSection();
        }

        // ── Abilities Section ─────────────────────────────────────────

        private void DrawAbilitiesSection()
        {
            // 등록된 어빌리티 목록과 이름/태그 필터 UI를 출력합니다.
            IList<AbilityRuntimeContext> contexts = m_selectedProcessor.GetAllAbilityContexts();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter", GUILayout.Width(32));
            m_abilityFilter = EditorGUILayout.TextField(m_abilityFilter);
            m_showActiveAbilitiesOnly = GUILayout.Toggle(m_showActiveAbilitiesOnly, "Active Only", EditorStyles.miniButton, GUILayout.Width(86));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            if (contexts.Count == 0)
            {
                EditorGUILayout.LabelField("  등록된 어빌리티 없음", EditorStyles.miniLabel);
                return;
            }

            int visibleCount = 0;
            m_abilityScrollPos = EditorGUILayout.BeginScrollView(m_abilityScrollPos, GUILayout.MaxHeight(150));
            foreach (AbilityRuntimeContext ctx in contexts)
            {
                if (ctx?.Ability == null) continue;
                if (!ShouldShowAbility(ctx)) continue;
                visibleCount++;
                DrawAbilityRow(ctx);
            }
            EditorGUILayout.EndScrollView();

            if (visibleCount == 0)
                EditorGUILayout.LabelField("  필터와 일치하는 어빌리티 없음", EditorStyles.miniLabel);
        }

        private void DrawAbilityRow(AbilityRuntimeContext ctx)
        {
            // 한 행은 하나의 등록된 어빌리티와 그에 대한 직접 디버그 액션을 나타냅니다.
            bool active = ctx.IsActive;
            Ability ability = ctx.Ability;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.2f, 0.55f, 0.2f) : new Color(0.22f, 0.22f, 0.22f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            GUILayout.Label(active ? "●" : "○", active ? m_activeStyle : m_inactiveStyle, GUILayout.Width(14));
            if (GUILayout.Button(ability.name, ctx.Equals(m_selectedAbilityContext) ? m_selectedButtonStyle : GUI.skin.label, GUILayout.MinWidth(80)))
                m_selectedAbilityContext = ctx;
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
                if (active)
                {
                    if (GUILayout.Button("Cancel", EditorStyles.miniButtonMid, GUILayout.Width(52)))
                        CancelAbilityAndRecord(ctx, "Ability Row");
                }

                if (GUILayout.Button("Activate", active ? EditorStyles.miniButtonRight : EditorStyles.miniButton, GUILayout.Width(56)))
                    TryActivateAndRecord(ability.MainTag, ability, "Ability Row");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawControlsSection()
        {
            // 선택된 프로세서에 대한 큰 단위의 상태 제어 버튼을 출력합니다.
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel All", EditorStyles.miniButtonLeft))
                    CancelAllAbilitiesAndRecord();
                if (GUILayout.Button("Clear Tags", EditorStyles.miniButtonMid))
                    ClearOwnTagsAndRecord();
                if (GUILayout.Button("Reinitialize", EditorStyles.miniButtonRight))
                    ReinitializeProcessorAndRecord();
                EditorGUILayout.EndHorizontal();
            }

            if (!Application.isPlaying)
                EditorGUILayout.LabelField("  컨트롤 작업은 플레이 모드에서만 가능합니다.", EditorStyles.miniLabel);
        }

        // ── Tag Operations Section ────────────────────────────────────

        private void DrawTagOperationsSection()
        {
            // 태그 입력, 캐시 기반 태그 선택, 수동 태그/활성화 명령 UI를 출력합니다.
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
                    AddTagAndRecord(new GameTag(m_tagInput));
                if (GUILayout.Button("Remove", EditorStyles.miniButton))
                    RemoveTagAndRecord(new GameTag(m_tagInput));
                if (GUILayout.Button("Notify", EditorStyles.miniButton))
                    NotifyTagAndRecord(new GameTag(m_tagInput));
                if (GUILayout.Button("Activate", EditorStyles.miniButton))
                    TryActivateAndRecord(new GameTag(m_tagInput), null, "Tag Operations");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Duration (s)", GUILayout.Width(74));
                m_timedTagDuration = Mathf.Max(0.01f, EditorGUILayout.FloatField(m_timedTagDuration, GUILayout.Width(50)));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Timed", EditorStyles.miniButton, GUILayout.Width(76)))
                    AddTimedTagAndRecord(new GameTag(m_tagInput), m_timedTagDuration);
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
            // 프로세서가 정확히 보유 중인 태그와 timed tag 카운트다운 정보를 출력합니다.
            IList<GameTag> ownTags = m_selectedProcessor.TagContainer.GetOwnTags();
            IList<GameTagContainer.TimedTagInfo> timedTags = m_selectedProcessor.TagContainer.GetTimedTags();

            if (ownTags.Count == 0)
            {
                EditorGUILayout.LabelField("  (없음)", EditorStyles.miniLabel);
                return;
            }

            m_tagScrollPos = EditorGUILayout.BeginScrollView(m_tagScrollPos, GUILayout.MaxHeight(120));
            foreach (GameTag tag in ownTags)
            {
                int stack = m_selectedProcessor.TagContainer.GetTagStack(tag);
                int timedCount = GetTimedTagCount(timedTags, tag);
                float maxRemaining = GetTimedTagMaxRemaining(timedTags, tag);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("  •", GUILayout.Width(16));
                EditorGUILayout.LabelField(tag.TagName);
                GUILayout.FlexibleSpace();
                if (timedCount > 0)
                    EditorGUILayout.LabelField($"{maxRemaining:0.00}s", EditorStyles.miniLabel, GUILayout.Width(48));
                if (stack > 1)
                    EditorGUILayout.LabelField($"x{stack}", EditorStyles.miniLabel, GUILayout.Width(28));
                if (timedCount > 1)
                    EditorGUILayout.LabelField($"t{timedCount}", EditorStyles.miniLabel, GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawLastActivationSection()
        {
            // 가장 최근 활성화 요청의 출처, 결과, 사람이 읽기 쉬운 진단 내용을 출력합니다.
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

        private void DrawHistorySection()
        {
            // 히스토리 리스트 뷰 안에 필터링된 이벤트 행들을 출력합니다.
            if (m_eventHistory.Count == 0)
            {
                EditorGUILayout.LabelField("  아직 기록된 이벤트가 없습니다.", EditorStyles.miniLabel);
                return;
            }

            int visibleCount = 0;
            m_historyScrollPos = EditorGUILayout.BeginScrollView(m_historyScrollPos, GUILayout.MaxHeight(160));
            foreach (DebuggerEventEntry entry in m_eventHistory)
            {
                if (!ShouldShowHistoryEntry(entry))
                    continue;

                visibleCount++;
                DrawHistoryEntry(entry);
            }
            EditorGUILayout.EndScrollView();

            if (visibleCount == 0)
                EditorGUILayout.LabelField("  필터와 일치하는 기록이 없습니다.", EditorStyles.miniLabel);
        }

        private void DrawHistoryTabPanel()
        {
            // 히스토리 전용 필터와 오른쪽 이벤트 리스트 뷰를 출력합니다.
            GUILayout.Space(4);
            m_historyOptionsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_historyOptionsExpanded, "History Options");
            if (m_historyOptionsExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filter", GUILayout.Width(32));
                m_historyFilter = EditorGUILayout.TextField(m_historyFilter);
                m_showActivationHistoryOnly = GUILayout.Toggle(m_showActivationHistoryOnly, "Activation Only", EditorStyles.miniButton, GUILayout.Width(102));
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(42)))
                    ClearHistory();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                m_showSystemHistory = GUILayout.Toggle(m_showSystemHistory, "System", EditorStyles.miniButtonLeft);
                m_showAbilityHistory = GUILayout.Toggle(m_showAbilityHistory, "Ability", EditorStyles.miniButtonMid);
                m_showTagHistory = GUILayout.Toggle(m_showTagHistory, "Tag", EditorStyles.miniButtonMid);
                m_showActivateHistory = GUILayout.Toggle(m_showActivateHistory, "Activate", EditorStyles.miniButtonRight);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.Space(4);
            DrawHistorySection();
        }

        private void DrawActiveAbilityDetailsSection()
        {
            // 현재 활성 상태인 모든 어빌리티의 확장 런타임 필드를 출력합니다.
            IList<AbilityRuntimeContext> contexts = m_selectedProcessor.GetAllAbilityContexts();
            List<AbilityRuntimeContext> activeContexts = new List<AbilityRuntimeContext>();

            foreach (AbilityRuntimeContext context in contexts)
            {
                if (context?.Ability == null || !context.IsActive)
                    continue;

                activeContexts.Add(context);
            }

            if (activeContexts.Count == 0)
            {
                EditorGUILayout.LabelField("  활성 어빌리티 없음", EditorStyles.miniLabel);
                return;
            }

            m_activeDetailScrollPos = EditorGUILayout.BeginScrollView(m_activeDetailScrollPos, GUILayout.MaxHeight(170));
            foreach (AbilityRuntimeContext context in activeContexts)
                DrawActiveAbilityDetail(context);
            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAbilityDetail(AbilityRuntimeContext context)
        {
            // 활성 어빌리티 하나의 라이프사이클 상태와 태그 규칙 스냅샷을 출력합니다.
            Ability ability = context.Ability;
            AbilityGameTagSet tagSet = ability.TagSet;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(ability.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"MainTag: {GetTagName(ability.MainTag)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Elapsed: {context.ElapsedTime:0.00}s", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Activated At: {context.ActivatedTime:0.00}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Pending End: {context.PendingEndReason}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Reactivation: {ability.ReactivationPolicy}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Trigger Tag: {GetTagName(tagSet.TriggerTag)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Owned Tags: {FormatTagList(tagSet.ActivationOwnedTags)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Required Tags: {FormatTagList(tagSet.ActivationRequiredTags)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Blocked Tags: {FormatTagList(tagSet.ActivationBlockedTags)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Cancel Tags: {FormatTagList(tagSet.CancelAbilitiesWithTag)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Blocks: {FormatTagList(tagSet.BlockAbilitiesWithTag)}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawBlockerAnalysisSection()
        {
            // 현재 프로세서 상태에서 선택한 어빌리티가 왜 활성화되거나 차단되는지 출력합니다.
            AbilityRuntimeContext context = ResolveAnalysisContext();
            if (context?.Ability == null)
            {
                EditorGUILayout.LabelField("  분석할 어빌리티를 목록에서 선택하거나 Tag 입력 후 Activate를 시도하세요.", EditorStyles.miniLabel);
                return;
            }

            Ability ability = context.Ability;
            EditorGUILayout.LabelField($"Target: {ability.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"MainTag: {GetTagName(ability.MainTag)}", EditorStyles.miniLabel);

            IList<string> lines = BuildBlockerAnalysisLines(context);
            foreach (string line in lines)
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
        }

        private void DrawHistoryEntry(DebuggerEventEntry entry)
        {
            // 히스토리 리스트 뷰의 색상 구분된 이벤트 한 줄을 출력합니다.
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 6f);
            EditorGUI.DrawRect(rowRect, GetHistoryEntryBackground(entry.Category));

            Rect categoryRect = new Rect(rowRect.x + 4f, rowRect.y + 3f, 64f, rowRect.height - 6f);
            Rect messageRect = new Rect(rowRect.x + 72f, rowRect.y + 3f, rowRect.width - 76f, rowRect.height - 6f);

            EditorGUI.LabelField(categoryRect, entry.Category, EditorStyles.boldLabel);
            EditorGUI.LabelField(messageRect, $"[{entry.Time:0.00}] {entry.Message}", EditorStyles.miniLabel);
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
            AddHistoryEntry("Activate", $"Request from {source}: {GetTagName(tag)}");
            AbilityActivateResult result = m_selectedProcessor.TryActivateAbility(tag);

            m_lastActivationSource = source;
            m_lastActivationTagName = string.IsNullOrEmpty(tag.TagName) ? "(empty)" : tag.TagName;
            m_lastActivationAbilityName = targetAbility != null ? targetAbility.name : "(not found)";
            m_lastActivationResult = result;
            m_lastActivationDetail = BuildActivationDetail(tag, targetAbility, result, context?.IsActive ?? false);
            AddHistoryEntry("Activate", $"Result {result}: {m_lastActivationDetail}");
            Repaint();
        }

        private void AddTagAndRecord(GameTag tag)
        {
            m_selectedProcessor.TagContainer.AddTag(tag);
        }

        private void RemoveTagAndRecord(GameTag tag)
        {
            m_selectedProcessor.TagContainer.RemoveTag(tag);
        }

        private void NotifyTagAndRecord(GameTag tag)
        {
            m_selectedProcessor.TagContainer.NotifyTag(tag);
        }

        private void AddTimedTagAndRecord(GameTag tag, float duration)
        {
            AddHistoryEntry("Tag", $"Timed add request: {GetTagName(tag)} ({duration:0.00}s)");
            m_selectedProcessor.TagContainer.AddTagTimed(tag, duration);
        }

        private void SelectProcessorObject(AbilityProcessor processor)
        {
            if (processor == null)
                return;

            Selection.activeObject = processor.gameObject;
            EditorGUIUtility.PingObject(processor.gameObject);
            AddHistoryEntry("System", $"Selected processor object: {processor.gameObject.name}");
        }

        private void PingProcessorObject(AbilityProcessor processor)
        {
            if (processor == null)
                return;

            EditorGUIUtility.PingObject(processor.gameObject);
            AddHistoryEntry("System", $"Ping processor object: {processor.gameObject.name}");
        }

        private void CancelAbilityAndRecord(AbilityRuntimeContext context, string source)
        {
            if (context == null || context.Ability == null || !context.IsActive)
                return;

            context.RequestCancel();
            AddHistoryEntry("Ability", $"Cancel request from {source}: {context.Ability.name}");
        }

        private void CancelAllAbilitiesAndRecord()
        {
            AddHistoryEntry("Ability", "Cancel all requested");
            m_selectedProcessor.CancelAllAbilities();
        }

        private void ClearOwnTagsAndRecord()
        {
            List<GameTag> ownTags = new List<GameTag>(m_selectedProcessor.TagContainer.GetOwnTags());
            AddHistoryEntry("Tag", $"Clear own tags requested ({ownTags.Count})");

            foreach (GameTag tag in ownTags)
            {
                int stack = m_selectedProcessor.TagContainer.GetTagStack(tag);
                for (int i = 0; i < stack; i++)
                    m_selectedProcessor.TagContainer.RemoveTag(tag);
            }
        }

        private void ReinitializeProcessorAndRecord()
        {
            AddHistoryEntry("System", "Processor reinitialize requested");
            m_selectedProcessor.CancelAllAbilities();

            List<GameTag> abilityTags = new List<GameTag>();
            foreach (AbilityRuntimeContext context in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (context?.Ability == null)
                    continue;

                abilityTags.Add(context.Ability.MainTag);
            }

            foreach (GameTag tag in abilityTags)
                m_selectedProcessor.UnregistAbility(tag);

            ClearOwnTagsAndRecord();
            m_selectedProcessor.Initialize();
            m_contextSnapshots.Clear();
            CaptureContextSnapshots();
            m_selectedAbilityContext = null;
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

        private static string FormatTagList(IList<GameTag> tags)
        {
            if (tags == null || tags.Count == 0)
                return "-";

            List<string> tagNames = new List<string>(tags.Count);
            foreach (GameTag tag in tags)
                tagNames.Add(GetTagName(tag));

            return string.Join(", ", tagNames);
        }

        private static string GetTagName(GameTag tag)
        {
            return string.IsNullOrEmpty(tag.TagName) ? "-" : tag.TagName;
        }

        private bool ShouldShowHistoryEntry(DebuggerEventEntry entry)
        {
            if (m_showActivationHistoryOnly && !string.Equals(entry.Category, "Activate", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsHistoryCategoryVisible(entry.Category))
                return false;

            if (string.IsNullOrWhiteSpace(m_historyFilter))
                return true;

            string filter = m_historyFilter.Trim();
            return entry.Category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || entry.Message.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClearHistory()
        {
            m_eventHistory.Clear();
        }

        private bool ShouldShowAbility(AbilityRuntimeContext context)
        {
            if (context?.Ability == null)
                return false;

            if (m_showActiveAbilitiesOnly && !context.IsActive)
                return false;

            if (string.IsNullOrWhiteSpace(m_abilityFilter))
                return true;

            string filter = m_abilityFilter.Trim();
            if (context.Ability.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string mainTagName = context.Ability.MainTag.TagName ?? string.Empty;
            return mainTagName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private AbilityRuntimeContext ResolveAnalysisContext()
        {
            if (m_selectedAbilityContext != null && m_selectedAbilityContext.Ability != null)
                return m_selectedAbilityContext;

            if (!string.IsNullOrWhiteSpace(m_lastActivationTagName))
                return m_selectedProcessor.GetAbilityContext(new GameTag(m_lastActivationTagName));

            return null;
        }

        private IList<string> BuildBlockerAnalysisLines(AbilityRuntimeContext context)
        {
            List<string> lines = new List<string>();
            Ability ability = context.Ability;

            if (context.IsActive)
                lines.Add("- 현재 활성 상태입니다.");
            else
                lines.Add("- 현재 비활성 상태입니다.");

            List<string> missingRequired = new List<string>();
            foreach (GameTag requiredTag in ability.TagSet.ActivationRequiredTags)
            {
                if (!m_selectedProcessor.TagContainer.HasExactTag(requiredTag))
                    missingRequired.Add(requiredTag.TagName);
            }

            lines.Add(missingRequired.Count == 0
                ? "- Required tags: 충족됨"
                : $"- Required tags 부족: {string.Join(", ", missingRequired)}");

            List<string> presentBlockTags = new List<string>();
            foreach (GameTag blockedTag in ability.TagSet.ActivationBlockedTags)
            {
                if (m_selectedProcessor.TagContainer.HasExactTag(blockedTag))
                    presentBlockTags.Add(blockedTag.TagName);
            }

            lines.Add(presentBlockTags.Count == 0
                ? "- Blocking tags: 없음"
                : $"- Blocking tags 존재: {string.Join(", ", presentBlockTags)}");

            List<string> blockingAbilities = new List<string>();
            foreach (AbilityRuntimeContext otherContext in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (otherContext?.Ability == null || !otherContext.IsActive)
                    continue;

                foreach (GameTag blockTag in otherContext.Ability.TagSet.BlockAbilitiesWithTag)
                {
                    if (ability.MainTag.ChildOf(blockTag))
                    {
                        blockingAbilities.Add($"{otherContext.Ability.name} -> {blockTag.TagName}");
                        break;
                    }
                }
            }

            lines.Add(blockingAbilities.Count == 0
                ? "- Active ability blockers: 없음"
                : $"- Active ability blockers: {string.Join(", ", blockingAbilities)}");

            AbilityActivateResult simulatedResult = SimulateActivationResult(context);
            lines.Add($"- 예상 TryActivate 결과: {simulatedResult}");
            lines.Add("- CanActivate: 런타임 내부 로직이라 에디터에서 직접 판별 불가");
            lines.Add($"- Reactivation policy: {ability.ReactivationPolicy}");
            lines.Add($"- Owned on activate: {FormatTagList(ability.TagSet.ActivationOwnedTags)}");

            return lines;
        }

        private bool IsHistoryCategoryVisible(string category)
        {
            switch (category)
            {
                case "System":
                    return m_showSystemHistory;
                case "Ability":
                    return m_showAbilityHistory;
                case "Tag":
                    return m_showTagHistory;
                case "Activate":
                    return m_showActivateHistory;
                default:
                    return true;
            }
        }

        private static Color GetHistoryEntryBackground(string category)
        {
            switch (category)
            {
                case "System":
                    return new Color(0.20f, 0.28f, 0.38f, 0.35f);
                case "Ability":
                    return new Color(0.20f, 0.35f, 0.22f, 0.35f);
                case "Tag":
                    return new Color(0.38f, 0.28f, 0.16f, 0.35f);
                case "Activate":
                    return new Color(0.36f, 0.20f, 0.20f, 0.35f);
                default:
                    return new Color(0.20f, 0.20f, 0.20f, 0.25f);
            }
        }

        private AbilityActivateResult SimulateActivationResult(AbilityRuntimeContext context)
        {
            if (context == null || context.Ability == null)
                return AbilityActivateResult.FailedAbilityNotFound;

            Ability ability = context.Ability;
            if (context.IsActive && ability.ReactivationPolicy == AbilityReactivationPolicy.Deny)
                return AbilityActivateResult.FailedAlreadyActive;

            if (!string.IsNullOrEmpty(ability.MainTag.TagName) && !m_selectedProcessor.HasActivatableAbility(ability.MainTag))
                return AbilityActivateResult.FailedAbilityNotFound;

            foreach (GameTag requiredTag in ability.TagSet.ActivationRequiredTags)
            {
                if (!m_selectedProcessor.TagContainer.HasExactTag(requiredTag))
                    return AbilityActivateResult.FailedNotHasAllRequiredTag;
            }

            foreach (GameTag blockedTag in ability.TagSet.ActivationBlockedTags)
            {
                if (m_selectedProcessor.TagContainer.HasExactTag(blockedTag))
                    return AbilityActivateResult.FailedHasAnyBlockTag;
            }

            foreach (AbilityRuntimeContext otherContext in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (otherContext?.Ability == null || !otherContext.IsActive || ReferenceEquals(otherContext, context))
                    continue;

                foreach (GameTag blockTag in otherContext.Ability.TagSet.BlockAbilitiesWithTag)
                {
                    if (ability.MainTag.ChildOf(blockTag))
                        return AbilityActivateResult.FailedBlockedByOther;
                }
            }

            return AbilityActivateResult.Success;
        }

        private static int GetTimedTagCount(IList<GameTagContainer.TimedTagInfo> timedTags, GameTag targetTag)
        {
            int count = 0;
            for (int i = 0; i < timedTags.Count; i++)
            {
                if (timedTags[i].Tag.Equals(targetTag))
                    count++;
            }

            return count;
        }

        private static float GetTimedTagMaxRemaining(IList<GameTagContainer.TimedTagInfo> timedTags, GameTag targetTag)
        {
            float maxRemaining = 0f;
            for (int i = 0; i < timedTags.Count; i++)
            {
                if (!timedTags[i].Tag.Equals(targetTag))
                    continue;

                maxRemaining = Mathf.Max(maxRemaining, timedTags[i].Remaining);
            }

            return maxRemaining;
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

            AbilityProcessor nextProcessor = m_selectedProcessor != null && m_abilityProcessors.Contains(m_selectedProcessor)
                ? m_selectedProcessor
                : m_abilityProcessors.Count > 0 ? m_abilityProcessors[0] : null;
            SetSelectedProcessor(nextProcessor);
        }

        private void SetSelectedProcessor(AbilityProcessor processor)
        {
            if (ReferenceEquals(m_selectedProcessor, processor))
                return;

            DetachProcessorEvents();
            m_selectedProcessor = processor;
            m_selectedAbilityContext = null;
            m_eventHistory.Clear();
            m_contextSnapshots.Clear();

            if (m_selectedProcessor == null)
                return;

            AttachProcessorEvents();
            CaptureContextSnapshots();
            AddHistoryEntry("System", $"Selected processor: {m_selectedProcessor.name}");
        }

        private void AttachProcessorEvents()
        {
            if (m_selectedProcessor == null)
                return;

            m_selectedProcessor.TagContainer.OnTagOwnshipChanged += HandleTagOwnshipChanged;
            m_selectedProcessor.TagContainer.OnTagNotified += HandleTagNotified;
            m_selectedProcessor.OnAbilityRegistered += HandleAbilityRegistered;
            m_selectedProcessor.OnAbilityUnregistered += HandleAbilityUnregistered;
        }

        private void DetachProcessorEvents()
        {
            if (m_selectedProcessor == null)
                return;

            m_selectedProcessor.TagContainer.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
            m_selectedProcessor.TagContainer.OnTagNotified -= HandleTagNotified;
            m_selectedProcessor.OnAbilityRegistered -= HandleAbilityRegistered;
            m_selectedProcessor.OnAbilityUnregistered -= HandleAbilityUnregistered;
        }

        private void HandleTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info)
        {
            string action = info.Added ? "Add" : "Remove";
            AddHistoryEntry("Tag", $"{action}: {GetTagName(info.Tag)} (stack {info.Remains})");
        }

        private void HandleTagNotified(GameTag tag)
        {
            AddHistoryEntry("Tag", $"Notify: {GetTagName(tag)}");
        }

        private void HandleAbilityRegistered(Ability ability, AbilityRuntimeContext context)
        {
            AddHistoryEntry("Ability", $"Registered: {ability.name} ({GetTagName(ability.MainTag)})");
            if (context != null)
                m_contextSnapshots[context] = CreateSnapshot(context);
        }

        private void HandleAbilityUnregistered(Ability ability, AbilityRuntimeContext context)
        {
            AddHistoryEntry("Ability", $"Unregistered: {ability.name} ({GetTagName(ability.MainTag)})");
            if (context != null)
                m_contextSnapshots.Remove(context);
        }

        private void PollProcessorEvents()
        {
            if (m_selectedProcessor == null)
                return;

            HashSet<AbilityRuntimeContext> seenContexts = new HashSet<AbilityRuntimeContext>();
            foreach (AbilityRuntimeContext context in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (context?.Ability == null)
                    continue;

                seenContexts.Add(context);
                AbilityContextSnapshot current = CreateSnapshot(context);

                if (!m_contextSnapshots.TryGetValue(context, out AbilityContextSnapshot previous))
                {
                    m_contextSnapshots[context] = current;
                    continue;
                }

                if (!previous.IsActive && current.IsActive)
                    AddHistoryEntry("Ability", $"Activated: {context.Ability.name}");

                if (previous.PendingEndReason != current.PendingEndReason)
                {
                    if (current.PendingEndReason != AbilityEndReason.None)
                        AddHistoryEntry("Ability", $"End requested: {context.Ability.name} -> {current.PendingEndReason}");
                    else if (previous.PendingEndReason != AbilityEndReason.None && !current.IsActive)
                        AddHistoryEntry("Ability", $"Deactivated: {context.Ability.name} ({previous.PendingEndReason})");
                }
                else if (previous.IsActive && !current.IsActive)
                {
                    AddHistoryEntry("Ability", $"Deactivated: {context.Ability.name}");
                }

                m_contextSnapshots[context] = current;
            }

            List<AbilityRuntimeContext> removedContexts = new List<AbilityRuntimeContext>();
            foreach (KeyValuePair<AbilityRuntimeContext, AbilityContextSnapshot> pair in m_contextSnapshots)
            {
                if (!seenContexts.Contains(pair.Key))
                    removedContexts.Add(pair.Key);
            }

            foreach (AbilityRuntimeContext context in removedContexts)
                m_contextSnapshots.Remove(context);
        }

        private static AbilityContextSnapshot CreateSnapshot(AbilityRuntimeContext context)
        {
            return new AbilityContextSnapshot
            {
                IsActive = context.IsActive,
                PendingEndReason = context.PendingEndReason,
            };
        }

        private void AddHistoryEntry(string category, string message)
        {
            m_eventHistory.Insert(0, new DebuggerEventEntry
            {
                Time = EditorApplication.timeSinceStartup,
                Category = category,
                Message = message,
            });

            if (m_eventHistory.Count > MAX_HISTORY_ENTRIES)
                m_eventHistory.RemoveAt(m_eventHistory.Count - 1);
        }

        private void CaptureContextSnapshots()
        {
            if (m_selectedProcessor == null)
                return;

            foreach (AbilityRuntimeContext context in m_selectedProcessor.GetAllAbilityContexts())
            {
                if (context?.Ability == null)
                    continue;

                m_contextSnapshots[context] = CreateSnapshot(context);
            }
        }
    }
}
