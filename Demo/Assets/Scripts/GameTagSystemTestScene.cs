using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sizzle.GameTagSystem;

public class GameTagSystemTestScene : MonoBehaviour
{
    private int selectedTab = 0;
    private string[] tabNames = new string[] { "GameTag Compare", "GameTagContainer" };

    // Tab 1 Variables
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

    // Tab 2 Variables
    private GameTagContainer container;
    private string actionTagName = "Skill.Attack.Light";
    private string queryTagName = "Skill.Attack";
    private string timedTagDurationText = "2";
    private float timedTagDuration = 2f;
    
    private List<string> logs = new List<string>();
    private const int maxLogCount = 30;
    
    private Vector2 logScrollPos;
    private Vector2 ownedTagScrollPos;

    private void Awake()
    {
        EnsureContainer();
    }

    private void OnEnable()
    {
        EnsureContainer();
        container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
        container.OnTagNotified += HandleTagNotified;
    }

    private void OnDisable()
    {
        if (container != null)
        {
            container.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
            container.OnTagNotified -= HandleTagNotified;
        }
    }

    private void Update()
    {
        if (container != null)
        {
            container.Tick(Time.deltaTime);
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
        {
            DrawTab1();
        }
        else if (selectedTab == 1)
        {
            DrawTab2();
        }

        GUILayout.EndArea();
    }

    private void DrawTab1()
    {
        GUILayout.Label("--- GameTag Comparison ---", GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Input Tag:", GUILayout.Width(100));
        inputTagString = GUILayout.TextField(inputTagString);
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
            
            // Exact: 정확히 일치하는지
            bool isExact = inputTag.IsExact(sample);
            
            // Parent: sample이 input의 부모인지 (input이 sample의 자식인지)
            bool isParent = inputTag.StrictChildOf(sample);
            
            // Child: sample이 input의 자식인지
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
        DrawControlPanel();
        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Right Panel: State
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(Screen.width * 0.45f));
        DrawStatePanel();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawControlPanel()
    {
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
    }

    private void DrawStatePanel()
    {
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
    }

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
            container.OnTagNotified -= HandleTagNotified;
        }

        container = new GameTagContainer();
        container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
        container.OnTagNotified += HandleTagNotified;
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

    private void HandleTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info)
    {
        string action = info.Added ? "Added" : "Removed";
        Log($"{action}: {info.Tag.TagName} (Remains: {info.Remains})");
    }

    private void HandleTagNotified(GameTag gameTag)
    {
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
}