using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Sizzle.GameTagSystem;

public class GameTagSystemTestScene : MonoBehaviour
{
    [Header("Basic GameTag (Character Validation Test)")]
    public GameTag gameTagValue;

    [Header("GameTagOption Tests")]
    [Tooltip("기본 부모 하위 태그 필터링 (하위 태그만 선택 가능)")]
    [GameTagOption(parent = "Status.Buff")]
    public GameTag buffTagValue;

    [Tooltip("부모 자체(Status.Buff)도 포함 허용")]
    [GameTagOption(parent = "Status.Buff", includeParent = true)]
    public GameTag buffTagWithParent;

    [Tooltip("텍스트 편집 잠금, 오직 드롭다운 팝업으로만 선택")]
    [GameTagOption(parent = "Status.Buff", dropdownOnly = true)]
    public GameTag buffTagDropdownOnly;

    [Tooltip("Status.Buff.Elemental 하위 카테고리 제외")]
    [GameTagOption(parent = "Status.Buff", exclude = "Status.Buff.Elemental")]
    public GameTag buffTagExcludeElemental;

    [Tooltip("제외(Status.Buff.Speed) + 드롭다운 전용 복합 옵션")]
    [GameTagOption(parent = "Status.Buff", exclude = "Status.Buff.Speed", dropdownOnly = true)]
    public GameTag buffTagExcludeAndDropdownOnly;

    [Tooltip("string 필드에도 GameTagOption 적용 가능")]
    [GameTagOption(parent = "Status.Buff")]
    public string buffTagStringValue;

    [Serializable]
    public class CustomTestPayload
    {
        public string Sender;
        public int Number;
        public override string ToString() => $"Sender={Sender}, Number={Number}";
    }

    [ContextMenu("Test/Notify Tag With String Data")]
    public void ContextMenuNotifyTagWithStringData()
    {
        EnsureContainer();
        GameTag tag = new GameTag("Status.Buff.Attack");
        string payload = "Attack Power +50 Boost!";
        container.NotifyTag(tag, payload);
        UnityEngine.Debug.Log($"[ContextMenu] NotifyTag called: {tag} with payload '{payload}'");
    }

    [ContextMenu("Test/Notify Tag With Custom Object Data")]
    public void ContextMenuNotifyTagWithObjectData()
    {
        EnsureContainer();
        GameTag tag = new GameTag("Event.Hit");
        var payload = new CustomTestPayload { Sender = "Hero", Number = 777 };
        container.NotifyTag(tag, payload);
        UnityEngine.Debug.Log($"[ContextMenu] NotifyTag called: {tag} with object payload '{payload}'");
    }

    [ContextMenu("Test/Validate & Sanitize Tag String")]
    public void ContextMenuValidateAndSanitize()
    {
        string raw = "Status.Buff@#$_Fire 123!한글";
        bool isValid = GameTag.IsValidTagName(raw, out string reason);
        string sanitized = GameTag.SanitizeTagName(raw);
        UnityEngine.Debug.Log($"[ContextMenu] Raw: '{raw}' -> IsValid: {isValid} ({reason}) -> Sanitized: '{sanitized}'");
    }

    private int selectedTab = 0;
    private string[] tabNames = new string[] { "GameTag Compare", "GameTagContainer", "Benchmark" };

    // --- Tab 1 Variables ---
    private string inputTagString = "Skill.Attack";
    private GameTag[] sampleTags = new GameTag[]
    {
        new GameTag("Skill"),
        new GameTag("Skill.Attack"),
        new GameTag("Skill.Attack.Fireball"),
        new GameTag("Skill.Defense"),
        new GameTag("Status"),
        new GameTag("Status.Buff"),
        new GameTag("Status.Debuff.Stun"),
        new GameTag("Item.Consumable.Potion")
    };

    // --- Tab 2 Variables ---
    private GameTagContainer container;
    private string actionTagName = "Skill.Attack.Light";
    private string queryTagName = "Skill.Attack";
    private string timedTagDurationText = "2";
    private float timedTagDuration = 2f;
    private string notifyPayloadText = "Sample Payload Data";
    
    private List<string> logs = new List<string>();
    private const int maxLogCount = 30;
    
    private Vector2 logScrollPos;
    private Vector2 ownedTagScrollPos;

    // --- Tab 3 (Benchmark) Variables ---
    private string strContainerCount = "1000";
    private string strInitialTags = "20";
    private string strOpsCount = "10";
    private string strTimedTags = "5";

    private int benchContainerCount = 1000;
    private int benchInitialTagsPerContainer = 20;
    private int benchOperationsPerContainer = 10;
    private int benchTimedTagsPerContainer = 5;

    private List<GameTagContainer> benchContainers = new List<GameTagContainer>();
    private string benchBurstResult = "Ready to test.\n";
    private Vector2 benchBurstScroll;

    private bool benchToggleQueries = false;
    private bool benchToggleAddRemove = false;
    private bool benchToggleTick = false;

    private double benchMsQueries = 0;
    private double benchMsAddRemove = 0;
    private double benchMsTick = 0;

    private GameTag[] precomputedTagsForBench;
    private Stopwatch sharedStopwatch = new Stopwatch();

    private void Awake()
    {
        EnsureContainer();
    }

    private void OnEnable()
    {
        EnsureContainer();
        container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
        container.OnTagNotifiedWithData += HandleTagNotifiedWithData;
    }

    private void OnDisable()
    {
        if (container != null)
        {
            container.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
            container.OnTagNotifiedWithData -= HandleTagNotifiedWithData;
        }
    }

    private void Update()
    {
        if (container != null)
        {
            container.Tick(Time.deltaTime);
        }

        if (selectedTab == 2 && benchContainers.Count > 0)
        {
            if (benchToggleQueries)
            {
                sharedStopwatch.Restart();
                for (int i = 0; i < benchContainers.Count; i++)
                {
                    var c = benchContainers[i];
                    for (int op = 0; op < benchOperationsPerContainer; op++)
                    {
                        var tag = precomputedTagsForBench[op % precomputedTagsForBench.Length];
                        c.HasExactTag(tag);
                        c.HasParentTag(tag);
                        c.HasChildTag(tag);
                    }
                }
                sharedStopwatch.Stop();
                benchMsQueries = sharedStopwatch.Elapsed.TotalMilliseconds;
            }
            else benchMsQueries = 0;

            if (benchToggleAddRemove)
            {
                sharedStopwatch.Restart();
                for (int i = 0; i < benchContainers.Count; i++)
                {
                    var c = benchContainers[i];
                    for (int op = 0; op < benchOperationsPerContainer; op++)
                    {
                        var tag = precomputedTagsForBench[op % precomputedTagsForBench.Length];
                        c.AddTag(tag);
                        c.RemoveTag(tag);
                    }
                }
                sharedStopwatch.Stop();
                benchMsAddRemove = sharedStopwatch.Elapsed.TotalMilliseconds;
            }
            else benchMsAddRemove = 0;

            if (benchToggleTick)
            {
                float dt = Time.deltaTime;
                sharedStopwatch.Restart();
                for (int i = 0; i < benchContainers.Count; i++)
                {
                    benchContainers[i].Tick(dt);
                }
                sharedStopwatch.Stop();
                benchMsTick = sharedStopwatch.Elapsed.TotalMilliseconds;
            }
            else benchMsTick = 0;
        }
    }

    private void EnsureContainer()
    {
        if (container == null)
            container = new GameTagContainer();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

        // Tabs
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        GUILayout.Space(10);

        if (selectedTab == 0)
            DrawTab1();
        else if (selectedTab == 1)
            DrawTab2();
        else if (selectedTab == 2)
            DrawTab3();

        GUILayout.EndArea();
    }

    private void DrawTab1()
    {
        GUILayout.Label("--- GameTag Comparison & Character Validation ---", GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Input Tag:", GUILayout.Width(100));
        inputTagString = GUILayout.TextField(inputTagString);
        GUILayout.EndHorizontal();

        bool isValid = GameTag.IsValidTagName(inputTagString, out string errorReason);
        string sanitized = GameTag.SanitizeTagName(inputTagString);

        GUILayout.BeginHorizontal();
        string formatStatus = isValid ? "Format Valid [A-Za-z0-9_.-]" : $"Format Invalid: {errorReason}";
        GUILayout.Label(formatStatus);
        if (GUILayout.Button("Sanitize Input", GUILayout.Width(120)))
        {
            inputTagString = sanitized;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GameTag inputTag = new GameTag(inputTagString);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Sample Tag", GUILayout.Width(160));
        GUILayout.Label("Exact", GUILayout.Width(60));
        GUILayout.Label("Is Sample Parent of Input?", GUILayout.Width(180));
        GUILayout.Label("Is Sample Child of Input?", GUILayout.Width(180));
        GUILayout.EndHorizontal();

        foreach (var sample in sampleTags)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(sample.TagName, GUILayout.Width(160));
            
            bool isExact = inputTag.IsExact(sample);
            bool isParent = inputTag.StrictChildOf(sample);
            bool isChild = sample.StrictChildOf(inputTag);

            GUILayout.Label(isExact.ToString(), GUILayout.Width(60));
            GUILayout.Label(isParent.ToString(), GUILayout.Width(180));
            GUILayout.Label(isChild.ToString(), GUILayout.Width(180));
            GUILayout.EndHorizontal();
        }
    }

    private void DrawTab2()
    {
        GUILayout.BeginHorizontal();

        // Left Panel: Controls
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.45f));
        GUILayout.Label("--- Control Panel ---", GUI.skin.box);
        GUILayout.Space(6f);

        GUILayout.Label("Action Tag:");
        actionTagName = GUILayout.TextField(actionTagName ?? string.Empty);

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Tag")) AddTag(actionTagName);
        if (GUILayout.Button("Remove Tag")) RemoveTag(actionTagName);
        if (GUILayout.Button("Notify Tag")) NotifyTag(actionTagName);
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Notify Payload:", GUILayout.Width(100f));
        notifyPayloadText = GUILayout.TextField(notifyPayloadText ?? string.Empty);
        if (GUILayout.Button("Notify with Payload", GUILayout.Width(140f)))
            NotifyTagWithPayload(actionTagName, notifyPayloadText);
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Timed Duration:", GUILayout.Width(100f));
        timedTagDurationText = GUILayout.TextField(timedTagDurationText ?? string.Empty);
        if (float.TryParse(timedTagDurationText, out float parsedDuration))
            timedTagDuration = Mathf.Max(0f, parsedDuration);

        if (GUILayout.Button("Add Timed Tag", GUILayout.Width(120f)))
            AddTimedTag(actionTagName, timedTagDuration);
        GUILayout.EndHorizontal();

        GUILayout.Space(15f);
        
        GUILayout.Label("Query Tag:");
        queryTagName = GUILayout.TextField(queryTagName ?? string.Empty);
        GameTag queryTag = CreateTagOrEmpty(queryTagName);
        
        GUILayout.Space(4f);
        GUILayout.Label($"Has Exact Tag: {(!queryTag.IsEmpty && container.HasExactTag(queryTag))}");
        GUILayout.Label($"Has Tag Or Descendant: {(!queryTag.IsEmpty && container.HasParentTag(queryTag))}");
        GUILayout.Label($"Has Strict Child Tag: {(!queryTag.IsEmpty && container.HasChildTag(queryTag))}");

        GUILayout.Space(15f);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Logs")) logs.Clear();
        if (GUILayout.Button("Reset Container")) ResetContainer();
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        GUILayout.Label("--- Logs ---", GUI.skin.box);
        logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.ExpandHeight(true));
        if (logs.Count == 0)
        {
            GUILayout.Label("No logs yet.");
        }
        else
        {
            for (int i = logs.Count - 1; i >= 0; i--)
                GUILayout.Label(logs[i]);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Right Panel: State
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.45f));
        GUILayout.Label("--- Current Owned Tags ---", GUI.skin.box);
        GUILayout.Space(6f);

        IList<GameTag> ownTags = container.GetOwnTags();
        IList<GameTagContainer.TimedTagInfo> timedTags = container.GetTimedTags();

        ownedTagScrollPos = GUILayout.BeginScrollView(ownedTagScrollPos, GUILayout.ExpandHeight(true));
        
        if (ownTags.Count == 0 && timedTags.Count == 0)
        {
            GUILayout.Label("No owned tags.");
        }
        else
        {
            foreach (var ownTag in ownTags)
            {
                int stack = container.GetTagStack(ownTag);
                GUILayout.Label($"- {ownTag.TagName} (Stack: {stack})");
            }

            foreach (var t in timedTags)
            {
                GUILayout.Label($"- [Timed] {t.Tag.TagName} (Remaining: {t.Remaining:F1}s)");
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(15f);
        GUILayout.Label("--- Summary ---", GUI.skin.box);
        GUILayout.TextArea(BuildSummaryText(ownTags), GUILayout.MinHeight(120f));
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawTab3()
    {
        GUILayout.BeginHorizontal();

        // Panel 1: Parameters Setup
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.28f));
        GUILayout.Label("--- Parameters ---", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Containers:", GUILayout.Width(140));
        strContainerCount = GUILayout.TextField(strContainerCount);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Init Tags / Container:", GUILayout.Width(140));
        strInitialTags = GUILayout.TextField(strInitialTags);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Ops / Container:", GUILayout.Width(140));
        strOpsCount = GUILayout.TextField(strOpsCount);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Timed Tags / Cont:", GUILayout.Width(140));
        strTimedTags = GUILayout.TextField(strTimedTags);
        GUILayout.EndHorizontal();
        
        if (int.TryParse(strContainerCount, out int c)) benchContainerCount = Mathf.Max(1, c);
        if (int.TryParse(strInitialTags, out int t)) benchInitialTagsPerContainer = Mathf.Max(0, t);
        if (int.TryParse(strOpsCount, out int o)) benchOperationsPerContainer = Mathf.Max(1, o);
        if (int.TryParse(strTimedTags, out int m)) benchTimedTagsPerContainer = Mathf.Max(0, m);

        GUILayout.Space(10);
        if (GUILayout.Button("Setup Initial State", GUILayout.Height(40)))
        {
            SetupBenchmark();
        }

        GUILayout.Space(10);
        GUILayout.Label($"Current Setup: {benchContainers.Count} Containers");
        GUILayout.EndVertical();


        // Panel 2: Burst Tests
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.35f));
        GUILayout.Label("--- Burst Tests (One-time spike) ---", GUI.skin.box);

        if (GUILayout.Button("1. Tag Creation (100,000 strings to GameTag)")) RunBurstCreation();
        if (GUILayout.Button("2. Tag Comparison (100,000 IsExact & ChildOf)")) RunBurstComparison();
        if (GUILayout.Button("3. Add/Remove Tag (For all setup containers)")) RunBurstAddRemove();

        GUILayout.Space(10);
        GUILayout.Label("Burst Results:", GUI.skin.box);
        benchBurstScroll = GUILayout.BeginScrollView(benchBurstScroll, GUILayout.ExpandHeight(true));
        GUILayout.TextArea(benchBurstResult, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        
        if (GUILayout.Button("Clear Results")) benchBurstResult = "";
        GUILayout.EndVertical();


        // Panel 3: Continuous Tests
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.33f));
        GUILayout.Label("--- Continuous Tests (Per Frame ms) ---", GUI.skin.box);
        
        if (benchContainers.Count == 0)
        {
            GUILayout.Label("\nPlease 'Setup Initial State' first.", GUI.skin.label);
        }
        else
        {
            benchToggleQueries = GUILayout.Toggle(benchToggleQueries, " Continuous Queries (Exact/Parent/Child)");
            GUILayout.Label($"  -> Time Taken: {benchMsQueries:F3} ms", GUI.skin.label);
            
            GUILayout.Space(10);
            benchToggleAddRemove = GUILayout.Toggle(benchToggleAddRemove, " Continuous Add/Remove");
            GUILayout.Label($"  -> Time Taken: {benchMsAddRemove:F3} ms", GUI.skin.label);

            GUILayout.Space(10);
            benchToggleTick = GUILayout.Toggle(benchToggleTick, " Continuous Timed Tag Ticking (Update)");
            GUILayout.Label($"  -> Time Taken: {benchMsTick:F3} ms", GUI.skin.label);

            GUILayout.Space(20);
            GUILayout.Label("Total Benchmark Frame Time:", GUI.skin.box);
            GUILayout.Label($" {(benchMsQueries + benchMsAddRemove + benchMsTick):F3} ms", GUI.skin.label);
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    // --- Tab 2 Helper Methods ---
    private GameTag CreateTagOrEmpty(string tagName)
    {
        string normalized = string.IsNullOrWhiteSpace(tagName) ? string.Empty : tagName.Trim();
        return new GameTag(normalized);
    }

    private void ResetContainer()
    {
        if (container != null)
        {
            container.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
            container.OnTagNotifiedWithData -= HandleTagNotifiedWithData;
        }

        container = new GameTagContainer();
        container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
        container.OnTagNotifiedWithData += HandleTagNotifiedWithData;
        Log("Container reset.");
    }

    private void AddTag(string tagName)
    {
        GameTag tag = CreateTagOrEmpty(tagName);
        if (tag.IsEmpty) { Log("Add Tag ignored: empty tag."); return; }
        container.AddTag(tag);
    }

    private void AddTimedTag(string tagName, float duration)
    {
        GameTag tag = CreateTagOrEmpty(tagName);
        if (tag.IsEmpty) { Log("Add Timed Tag ignored: empty tag."); return; }
        container.AddTagTimed(tag, duration);
        Log($"Timed tag added: {tag.TagName} ({duration:0.##}s)");
    }

    private void RemoveTag(string tagName)
    {
        GameTag tag = CreateTagOrEmpty(tagName);
        if (tag.IsEmpty) { Log("Remove Tag ignored: empty tag."); return; }
        if (!container.HasExactTag(tag)) { Log($"Remove Tag skipped: {tag.TagName} is not owned."); return; }
        container.RemoveTag(tag);
    }

    private void NotifyTag(string tagName)
    {
        GameTag tag = CreateTagOrEmpty(tagName);
        if (tag.IsEmpty) { Log("Notify Tag ignored: empty tag."); return; }
        container.NotifyTag(tag);
    }

    private void NotifyTagWithPayload(string tagName, string payload)
    {
        GameTag tag = CreateTagOrEmpty(tagName);
        if (tag.IsEmpty) { Log("Notify Tag ignored: empty tag."); return; }
        container.NotifyTag(tag, payload);
    }

    private void HandleTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info)
    {
        string action = info.Added ? "Added" : "Removed";
        Log($"{action}: {info.Tag.TagName} (Remains: {info.Remains})");
    }

    private void HandleTagNotifiedWithData(GameTag gameTag, object payload)
    {
        if (payload != null)
            Log($"Notified: {gameTag.TagName} | Payload: [{payload}]");
        else
            Log($"Notified: {gameTag.TagName}");
    }

    private void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        logs.Add($"[{timestamp}] {message}");

        if (logs.Count > maxLogCount)
            logs.RemoveAt(0);
    }

    private string BuildSummaryText(IList<GameTag> ownTags)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Owned Tag Count: {ownTags.Count}");

        GameTag queryTag = CreateTagOrEmpty(queryTagName);
        if (!queryTag.IsEmpty)
        {
            sb.AppendLine($"Query: {queryTag.TagName}");
            sb.AppendLine($"- Has Exact: {container.HasExactTag(queryTag)}");
            sb.AppendLine($"- Has Tag Or Descendant: {container.HasParentTag(queryTag)}");
            sb.AppendLine($"- Has Strict Child Tag: {container.HasChildTag(queryTag)}");
        }

        GameTag actionTag = CreateTagOrEmpty(actionTagName);
        if (!actionTag.IsEmpty)
            sb.AppendLine($"Action Tag Stack: {container.GetTagStack(actionTag)}");

        return sb.ToString();
    }


    // --- Tab 3 Benchmark Helper Methods ---
    private void SetupBenchmark()
    {
        benchContainers.Clear();
        int totalUniqueTags = Mathf.Max(benchInitialTagsPerContainer + benchOperationsPerContainer, 1);
        precomputedTagsForBench = new GameTag[totalUniqueTags];
        
        for(int i = 0; i < precomputedTagsForBench.Length; i++)
        {
            precomputedTagsForBench[i] = new GameTag($"Tag.Level1_{i}.Level2.Level3");
        }

        for (int i = 0; i < benchContainerCount; i++)
        {
            var c = new GameTagContainer();
            for (int t = 0; t < benchInitialTagsPerContainer; t++)
            {
                c.AddTag(precomputedTagsForBench[t]);
            }
            for(int t = 0; t < benchTimedTagsPerContainer; t++)
            {
                c.AddTagTimed(new GameTag($"Timed.Tag_{t}"), 9999f);
            }
            benchContainers.Add(c);
        }

        // Turn off continuous test upon reset
        benchToggleQueries = false;
        benchToggleAddRemove = false;
        benchToggleTick = false;
        benchMsQueries = 0;
        benchMsAddRemove = 0;
        benchMsTick = 0;

        benchBurstResult = $"[{DateTime.Now:HH:mm:ss}] Setup Complete: {benchContainerCount} containers initialized.\n" + benchBurstResult;
    }

    private void RunBurstCreation()
    {
        int count = 100000;
        sharedStopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            var t = new GameTag($"Test.Burst.Create_{i}");
        }
        sharedStopwatch.Stop();
        benchBurstResult = $"[{DateTime.Now:HH:mm:ss}] Created {count} GameTags in {sharedStopwatch.Elapsed.TotalMilliseconds:F2} ms\n" + benchBurstResult;
    }

    private void RunBurstComparison()
    {
        int count = 100000;
        GameTag t1 = new GameTag("Skill.Attack.Fireball");
        GameTag t2 = new GameTag("Skill.Attack");
        sharedStopwatch.Restart();
        int trueCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (t1.StrictChildOf(t2)) trueCount++;
            if (t1.IsExact(t2)) trueCount++;
        }
        sharedStopwatch.Stop();
        benchBurstResult = $"[{DateTime.Now:HH:mm:ss}] Compared {count} times in {sharedStopwatch.Elapsed.TotalMilliseconds:F2} ms\n" + benchBurstResult;
    }

    private void RunBurstAddRemove()
    {
        if (benchContainers.Count == 0)
        {
            benchBurstResult = $"[{DateTime.Now:HH:mm:ss}] Error: No containers. Please Setup first.\n" + benchBurstResult;
            return;
        }
        
        sharedStopwatch.Restart();
        for (int i = 0; i < benchContainers.Count; i++)
        {
            var c = benchContainers[i];
            for (int op = 0; op < benchOperationsPerContainer; op++)
            {
                var tag = precomputedTagsForBench[op % precomputedTagsForBench.Length];
                c.AddTag(tag);
                c.RemoveTag(tag);
            }
        }
        sharedStopwatch.Stop();
        int totalOps = benchContainers.Count * benchOperationsPerContainer * 2;
        benchBurstResult = $"[{DateTime.Now:HH:mm:ss}] Add/Remove {totalOps} ops in {sharedStopwatch.Elapsed.TotalMilliseconds:F2} ms\n" + benchBurstResult;
    }
}