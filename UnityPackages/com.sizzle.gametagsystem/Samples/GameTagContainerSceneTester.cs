using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Sizzle.GameTagSystem
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Sizzle/GameTag Container Scene Tester")]
    public class GameTagContainerSceneTester : MonoBehaviour
    {
        [Header("Initial State")]
        [SerializeField] private List<GameTag> m_initialTags = new List<GameTag>();
        [SerializeField] private bool m_applyInitialTagsOnAwake = true;

        [Header("Input")]
        [SerializeField] private string m_actionTagName = "Skill.Attack.Light";
        [SerializeField] private string m_queryTagName = "Skill.Attack";
        [SerializeField] private float m_timedTagDuration = 2f;
        [SerializeField] private string m_timedTagDurationText = "2";

        [Header("View")]
        [SerializeField] private int m_maxLogCount = 30;

        private readonly List<string> m_logs = new List<string>();
        private Vector2 m_logScrollPosition;
        private Vector2 m_ownedTagScrollPosition;
        private GameTagContainer m_container;
        private bool m_initialTagsApplied;

        private void Awake()
        {
            EnsureContainer();
        }

        private void OnEnable()
        {
            EnsureContainer();
            m_container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
            m_container.OnTagNotified += HandleTagNotified;

            if (m_applyInitialTagsOnAwake && !m_initialTagsApplied)
            {
                ApplyInitialTags();
                m_initialTagsApplied = true;
            }
        }

        private void OnDisable()
        {
            if (m_container == null)
                return;

            m_container.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
            m_container.OnTagNotified -= HandleTagNotified;
        }

        private void Update()
        {
            if (m_container == null)
                return;

            m_container.Tick(Time.deltaTime);
        }

        private void OnGUI()
        {
            EnsureContainer();

            const float padding = 12f;
            const float leftPanelWidth = 420f;
            float leftHeight = Screen.height - padding * 2f;
            float rightWidth = Screen.width - leftPanelWidth - padding * 3f;

            GUILayout.BeginArea(new Rect(padding, padding, leftPanelWidth, leftHeight), GUI.skin.box);
            DrawControlPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(leftPanelWidth + padding * 2f, padding, rightWidth, leftHeight), GUI.skin.box);
            DrawStatePanel();
            GUILayout.EndArea();
        }

        private void DrawControlPanel()
        {
            GUILayout.Label("GameTag Container Tester");
            GUILayout.Space(6f);

            GUILayout.Label("Action Tag");
            m_actionTagName = GUILayout.TextField(m_actionTagName ?? string.Empty);

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Tag"))
                AddTag(m_actionTagName);

            if (GUILayout.Button("Remove Tag"))
                RemoveTag(m_actionTagName);

            if (GUILayout.Button("Notify Tag"))
                NotifyTag(m_actionTagName);

            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();

            GUILayout.Label("Timed Duration", GUILayout.Width(100f));
            m_timedTagDurationText = GUILayout.TextField(m_timedTagDurationText ?? string.Empty);
            if (float.TryParse(m_timedTagDurationText, out float parsedDuration))
                m_timedTagDuration = Mathf.Max(0f, parsedDuration);

            if (GUILayout.Button("Add Timed Tag", GUILayout.Width(120f)))
                AddTimedTag(m_actionTagName, m_timedTagDuration);

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Query Tag");
            m_queryTagName = GUILayout.TextField(m_queryTagName ?? string.Empty);

            GameTag queryTag = CreateTagOrEmpty(m_queryTagName);
            bool hasExactTag = !queryTag.IsEmpty && m_container.HasExactTag(queryTag);
            bool hasExactOrChildTag = !queryTag.IsEmpty && m_container.HasParentTag(queryTag);
            bool hasChildOnlyTag = !queryTag.IsEmpty && m_container.HasChildTag(queryTag);

            GUILayout.Space(6f);
            GUILayout.Label($"Has Exact Tag: {hasExactTag}");
            GUILayout.Label($"Has Exact Or Child Tag: {hasExactOrChildTag}");
            GUILayout.Label($"Has Child Only Tag: {hasChildOnlyTag}");

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Logs"))
                m_logs.Clear();

            if (GUILayout.Button("Reset Container"))
                ResetContainer();

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Logs");
            m_logScrollPosition = GUILayout.BeginScrollView(m_logScrollPosition, GUILayout.ExpandHeight(true));

            if (m_logs.Count == 0)
            {
                GUILayout.Label("No logs yet.");
            }
            else
            {
                for (int i = m_logs.Count - 1; i >= 0; i--)
                    GUILayout.Label(m_logs[i]);
            }

            GUILayout.EndScrollView();
        }

        private void DrawStatePanel()
        {
            GUILayout.Label("Current Owned Tags");
            GUILayout.Space(6f);

            IList<GameTag> ownTags = m_container.GetOwnTags();
            m_ownedTagScrollPosition = GUILayout.BeginScrollView(m_ownedTagScrollPosition, GUILayout.ExpandHeight(true));

            if (ownTags.Count == 0)
            {
                GUILayout.Label("No owned tags.");
            }
            else
            {
                for (int i = 0; i < ownTags.Count; i++)
                {
                    GameTag ownTag = ownTags[i];
                    int stack = m_container.GetTagStack(ownTag);
                    GUILayout.Label($"{ownTag.TagName} (Stack: {stack})");
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10f);
            GUILayout.Label("Current Summary");
            GUILayout.TextArea(BuildSummaryText(ownTags), GUILayout.MinHeight(160f));
        }

        private void EnsureContainer()
        {
            if (m_container == null)
                m_container = new GameTagContainer();
        }

        private void ApplyInitialTags()
        {
            for (int i = 0; i < m_initialTags.Count; i++)
            {
                GameTag initialTag = m_initialTags[i];
                if (initialTag.IsEmpty)
                    continue;

                m_container.AddTag(initialTag);
            }
        }

        private void ResetContainer()
        {
            if (m_container != null)
            {
                m_container.OnTagOwnshipChanged -= HandleTagOwnshipChanged;
                m_container.OnTagNotified -= HandleTagNotified;
            }

            m_container = new GameTagContainer();
            m_container.OnTagOwnshipChanged += HandleTagOwnshipChanged;
            m_container.OnTagNotified += HandleTagNotified;
            m_initialTagsApplied = false;

            Log("Container reset.");

            if (m_applyInitialTagsOnAwake)
            {
                ApplyInitialTags();
                m_initialTagsApplied = true;
            }
        }

        private void AddTag(string tagName)
        {
            GameTag tag = CreateTagOrEmpty(tagName);
            if (tag.IsEmpty)
            {
                Log("Add Tag ignored: empty tag.");
                return;
            }

            m_container.AddTag(tag);
        }

        private void AddTimedTag(string tagName, float duration)
        {
            GameTag tag = CreateTagOrEmpty(tagName);
            if (tag.IsEmpty)
            {
                Log("Add Timed Tag ignored: empty tag.");
                return;
            }

            m_container.AddTagTimed(tag, duration);
            Log($"Timed tag added: {tag.TagName} ({duration:0.##}s)");
        }

        private void RemoveTag(string tagName)
        {
            GameTag tag = CreateTagOrEmpty(tagName);
            if (tag.IsEmpty)
            {
                Log("Remove Tag ignored: empty tag.");
                return;
            }

            if (!m_container.HasExactTag(tag))
            {
                Log($"Remove Tag skipped: {tag.TagName} is not owned.");
                return;
            }

            m_container.RemoveTag(tag);
        }

        private void NotifyTag(string tagName)
        {
            GameTag tag = CreateTagOrEmpty(tagName);
            if (tag.IsEmpty)
            {
                Log("Notify Tag ignored: empty tag.");
                return;
            }

            m_container.NotifyTag(tag);
        }

        private string BuildSummaryText(IList<GameTag> ownTags)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Owned Tag Count: {ownTags.Count}");

            GameTag queryTag = CreateTagOrEmpty(m_queryTagName);
            if (!queryTag.IsEmpty)
            {
                sb.AppendLine($"Query: {queryTag.TagName}");
                sb.AppendLine($"- Has Exact: {m_container.HasExactTag(queryTag)}");
                sb.AppendLine($"- Has Exact Or Child: {m_container.HasParentTag(queryTag)}");
                sb.AppendLine($"- Has Child Only: {m_container.HasChildTag(queryTag)}");
            }

            GameTag actionTag = CreateTagOrEmpty(m_actionTagName);
            if (!actionTag.IsEmpty)
                sb.AppendLine($"Action Tag Stack: {m_container.GetTagStack(actionTag)}");

            return sb.ToString();
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
            m_logs.Add($"[{timestamp}] {message}");

            if (m_logs.Count > m_maxLogCount)
                m_logs.RemoveAt(0);
        }

        private static GameTag CreateTagOrEmpty(string tagName)
        {
            string normalizedTagName = string.IsNullOrWhiteSpace(tagName) ? string.Empty : tagName.Trim();
            return new GameTag(normalizedTagName);
        }
    }
}