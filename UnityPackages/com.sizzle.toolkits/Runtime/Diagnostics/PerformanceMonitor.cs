using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace Sizzle.Toolkits.Diagnostics
{
    /// <summary>
    /// 게임 실행 중 실시간 FPS, 프레임 타임, 메모리, 가비지 컬렉션, 시스템/디바이스 정보 등을 OnGUI 오버레이로 출력하는 고성능 성능 모니터 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PerformanceMonitor : MonoBehaviour
    {
        /// <summary> 모니터링 패널에 표시할 정보 섹션 플래그 </summary>
        [Flags]
        public enum DisplaySectionFlags
        {
            None = 0,
            Fps = 1 << 0,
            FrameTimingDetails = 1 << 1,
            FrameTiming = Fps | FrameTimingDetails,
            TimeAndSync = 1 << 2,
            Memory = 1 << 3,
            GarbageCollection = 1 << 4,
            System = 1 << 5,
            Screen = 1 << 6,
            Quality = 1 << 7,
            Audio = 1 << 8,
            Scene = 1 << 9,
            All = ~0,
        }

        /// <summary> 화면 내 모니터링 패널 배치 위치 </summary>
        public enum OverlayPosition
        {
            TopLeft,
            BottomLeft,
            TopRight,
            BottomRight,
        }

        private const float Margin = 12f;
        private const float Padding = 10f;
        private const float MinPanelWidth = 300f;
        private const float MaxPanelWidth = 560f;

        [Header("Overlay")]
        public bool IsVisible = true;

        public bool ShowBackground = true;

        [Min(8)]
        public int FontSize = 16;

        public OverlayPosition Position = OverlayPosition.TopLeft;

        [Min(0.1f)]
        public float UpdateInterval = 0.5f;

        [Header("Sections")]
        [FormerlySerializedAs("ShowFrameTiming")]
        [InspectorName("Show FPS")]
        public bool ShowFps = true;

        public bool ShowFrameTimingDetails = true;
        public bool ShowTimeAndSync = true;
        public bool ShowMemory = true;
        public bool ShowGarbageCollection = true;
        public bool ShowSystem = true;
        public bool ShowScreen = true;
        public bool ShowQuality = true;
        public bool ShowAudio = true;
        public bool ShowScene = true;

        public DisplaySectionFlags DisplayFlags
        {
            get => BuildFlagsFromSectionToggles();
            set
            {
                ApplyFlagsToSectionToggles(value);
                RefreshNow();
            }
        }

        public string CurrentText => m_cachedText;

        private readonly StringBuilder m_builder = new StringBuilder(1024);

        private GUIStyle m_boxStyle;
        private GUIStyle m_labelStyle;
        private string m_cachedText = string.Empty;
        private Vector2 m_cachedPanelSize;
        private int m_cachedFontSize = -1;
        private float m_nextRefreshTime;
        private float m_accumulatedUnscaledTime;
        private int m_accumulatedFrameCount;
        private float m_averageFps;
        private float m_averageFrameTimeMs;
        private int m_lastGc0CollectionCount;
        private int m_lastGc1CollectionCount;
        private int m_lastGc2CollectionCount;
        private bool m_isLayoutDirty = true;

        private void OnEnable()
        {
            ClampSettings();
            CacheGcCollectionCounts();
            ResetFrameAggregation();
            RefreshNow();
        }

        private void OnValidate()
        {
            ClampSettings();
            RefreshNow();
        }

        private void Update()
        {
            m_accumulatedUnscaledTime += Time.unscaledDeltaTime;
            m_accumulatedFrameCount++;

            if (Time.unscaledTime >= m_nextRefreshTime)
                RefreshNow();
        }

        private void OnGUI()
        {
            if (!IsVisible)
                return;

            EnsureGuiStyles();

            if (string.IsNullOrEmpty(m_cachedText))
                RefreshNow();

            Rect panelRect = GetPanelRect();
            if (ShowBackground)
                GUI.Box(panelRect, GUIContent.none, m_boxStyle);

            Rect contentRect = new Rect(
                panelRect.x + Padding,
                panelRect.y + Padding,
                panelRect.width - (Padding * 2f),
                panelRect.height - (Padding * 2f));

            GUI.Label(contentRect, m_cachedText, m_labelStyle);
        }

        /// <summary> 모니터링 오버레이 표시 여부를 설정합니다. </summary>
        public void SetVisible(bool isVisible)
        {
            IsVisible = isVisible;
        }

        /// <summary> 모니터링 오버레이 표시 상태를 토글(반전)합니다. </summary>
        public void ToggleVisible()
        {
            IsVisible = !IsVisible;
        }

        /// <summary> 표시할 섹션 플래그를 일괄 설정합니다. </summary>
        public void SetDisplayFlags(DisplaySectionFlags flags)
        {
            DisplayFlags = flags;
        }

        /// <summary> 특정 섹션 플래그를 추가로 활성화합니다. </summary>
        public void EnableSections(DisplaySectionFlags flags)
        {
            DisplayFlags |= flags;
        }

        /// <summary> 특정 섹션 플래그를 비활성화합니다. </summary>
        public void DisableSections(DisplaySectionFlags flags)
        {
            DisplayFlags &= ~flags;
        }

        /// <summary> 특정 섹션 플래그의 표시 여부를 토글합니다. </summary>
        public void ToggleSections(DisplaySectionFlags flags)
        {
            DisplayFlags ^= flags;
        }

        /// <summary> 특정 섹션 플래그의 활성화 여부를 지정합니다. </summary>
        public void SetSectionEnabled(DisplaySectionFlags flags, bool isEnabled)
        {
            if (isEnabled)
                EnableSections(flags);
            else
                DisableSections(flags);
        }

        /// <summary> 특정 섹션이 현재 활성화되어 있는지 확인합니다. </summary>
        public bool IsSectionEnabled(DisplaySectionFlags flag)
        {
            return (DisplayFlags & flag) == flag;
        }

        /// <summary> 모니터링 데이터를 즉시 계산하고 텍스트 레이아웃을 갱신합니다. </summary>
        public void RefreshNow()
        {
            UpdateAverageFrameTiming();
            RebuildCachedText();
            m_nextRefreshTime = Time.unscaledTime + UpdateInterval;
            ResetFrameAggregation();
        }

        private void ClampSettings()
        {
            FontSize = Mathf.Max(8, FontSize);
            UpdateInterval = Mathf.Max(0.1f, UpdateInterval);
        }

        private void EnsureGuiStyles()
        {
            if (m_boxStyle != null && m_labelStyle != null && m_cachedFontSize == FontSize)
                return;

            m_boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset((int)Padding, (int)Padding, (int)Padding, (int)Padding),
            };

            m_labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                richText = false,
            };

            m_cachedFontSize = FontSize;
            m_isLayoutDirty = true;
        }

        private void UpdateAverageFrameTiming()
        {
            if (m_accumulatedFrameCount <= 0 || m_accumulatedUnscaledTime <= 0f)
            {
                if (Time.unscaledDeltaTime > 0f)
                {
                    m_averageFps = 1f / Time.unscaledDeltaTime;
                    m_averageFrameTimeMs = Time.unscaledDeltaTime * 1000f;
                }

                return;
            }

            m_averageFps = m_accumulatedFrameCount / m_accumulatedUnscaledTime;
            m_averageFrameTimeMs = (m_accumulatedUnscaledTime / m_accumulatedFrameCount) * 1000f;
        }

        private void ResetFrameAggregation()
        {
            m_accumulatedUnscaledTime = 0f;
            m_accumulatedFrameCount = 0;
        }

        private void RebuildCachedText()
        {
            DisplaySectionFlags flags = DisplayFlags;

            m_builder.Length = 0;

            if ((flags & DisplaySectionFlags.Fps) != 0)
                AppendFpsSection();

            if ((flags & DisplaySectionFlags.FrameTimingDetails) != 0)
                AppendFrameTimingDetailsSection();

            if ((flags & DisplaySectionFlags.TimeAndSync) != 0)
                AppendTimeAndSyncSection();

            if ((flags & DisplaySectionFlags.Memory) != 0)
                AppendMemorySection();

            if ((flags & DisplaySectionFlags.GarbageCollection) != 0)
                AppendGarbageCollectionSection();

            if ((flags & DisplaySectionFlags.System) != 0)
                AppendSystemSection();

            if ((flags & DisplaySectionFlags.Screen) != 0)
                AppendScreenSection();

            if ((flags & DisplaySectionFlags.Quality) != 0)
                AppendQualitySection();

            if ((flags & DisplaySectionFlags.Audio) != 0)
                AppendAudioSection();

            if ((flags & DisplaySectionFlags.Scene) != 0)
                AppendSceneSection();

            if (m_builder.Length == 0)
                m_builder.Append("No monitor sections enabled.");

            m_cachedText = m_builder.ToString();
            m_isLayoutDirty = true;
        }

        private void AppendFpsSection()
        {
            AppendSectionHeader("FPS");
            AppendKeyValue("FPS", m_averageFps.ToString("F1"));
        }

        private void AppendFrameTimingDetailsSection()
        {
            AppendSectionHeader("Frame Timing Details");
            AppendKeyValue("Frame Time", m_averageFrameTimeMs.ToString("F2") + " ms");
            AppendKeyValue("Smooth FPS", SafeFpsFromDelta(Time.smoothDeltaTime).ToString("F1"));
            AppendKeyValue("Delta Time", (Time.deltaTime * 1000f).ToString("F2") + " ms");
            AppendKeyValue("Unscaled Delta", (Time.unscaledDeltaTime * 1000f).ToString("F2") + " ms");
        }

        private void AppendTimeAndSyncSection()
        {
            AppendSectionHeader("Time & Sync");
            AppendKeyValue("Runtime", FormatDuration(Time.realtimeSinceStartup));
            AppendKeyValue("Frame Count", Time.frameCount.ToString());
            AppendKeyValue("Time Scale", Time.timeScale.ToString("F2"));
            AppendKeyValue("Fixed Delta", (Time.fixedDeltaTime * 1000f).ToString("F2") + " ms");
            AppendKeyValue("Max Delta", (Time.maximumDeltaTime * 1000f).ToString("F2") + " ms");
            AppendKeyValue("Target Frame Rate", FormatTargetFrameRate(Application.targetFrameRate));
            AppendKeyValue("VSync Count", QualitySettings.vSyncCount.ToString());
        }

        private void AppendMemorySection()
        {
            AppendSectionHeader("Memory");
            AppendKeyValue("Allocated", FormatBytes(Profiler.GetTotalAllocatedMemoryLong()));
            AppendKeyValue("Reserved", FormatBytes(Profiler.GetTotalReservedMemoryLong()));
            AppendKeyValue("Unused Reserved", FormatBytes(Profiler.GetTotalUnusedReservedMemoryLong()));
            AppendKeyValue("Mono Used", FormatBytes(Profiler.GetMonoUsedSizeLong()));
            AppendKeyValue("Mono Heap", FormatBytes(Profiler.GetMonoHeapSizeLong()));
            AppendKeyValue("GC Memory", FormatBytes(GC.GetTotalMemory(false)));
        }

        private void AppendGarbageCollectionSection()
        {
            int currentGc0CollectionCount = GC.CollectionCount(0);
            int currentGc1CollectionCount = GC.CollectionCount(1);
            int currentGc2CollectionCount = GC.CollectionCount(2);

            AppendSectionHeader("Garbage Collection");
            AppendKeyValue("Mode", GarbageCollector.GCMode.ToString());
            AppendKeyValue("Incremental", GarbageCollector.isIncremental.ToString());
            AppendKeyValue("Gen0", FormatCollectionCount(currentGc0CollectionCount, currentGc0CollectionCount - m_lastGc0CollectionCount));
            AppendKeyValue("Gen1", FormatCollectionCount(currentGc1CollectionCount, currentGc1CollectionCount - m_lastGc1CollectionCount));
            AppendKeyValue("Gen2", FormatCollectionCount(currentGc2CollectionCount, currentGc2CollectionCount - m_lastGc2CollectionCount));

            m_lastGc0CollectionCount = currentGc0CollectionCount;
            m_lastGc1CollectionCount = currentGc1CollectionCount;
            m_lastGc2CollectionCount = currentGc2CollectionCount;
        }

        private void AppendSystemSection()
        {
            AppendSectionHeader("System");
            AppendKeyValue("Platform", Application.platform.ToString());
            AppendKeyValue("Unity Version", Application.unityVersion);
            AppendKeyValue("OS", SystemInfo.operatingSystem);
            AppendKeyValue("CPU", SystemInfo.processorType + " (" + SystemInfo.processorCount + " cores)");
            AppendKeyValue("GPU", SystemInfo.graphicsDeviceName);
            AppendKeyValue("System Memory", SystemInfo.systemMemorySize + " MB");
            AppendKeyValue("Graphics Memory", SystemInfo.graphicsMemorySize + " MB");
            AppendKeyValue("Battery", SystemInfo.batteryStatus.ToString());
        }

        private void AppendScreenSection()
        {
            AppendSectionHeader("Screen");
            AppendKeyValue("Resolution", Screen.width + " x " + Screen.height);
            AppendKeyValue("Refresh Rate", GetCurrentRefreshRateHz().ToString("F1") + " Hz");
            AppendKeyValue("Fullscreen", Screen.fullScreenMode.ToString());
            AppendKeyValue("DPI", Screen.dpi > 0f ? Screen.dpi.ToString("F1") : "Unknown");
            AppendKeyValue("Orientation", Screen.orientation.ToString());
            AppendKeyValue("Safe Area", FormatRect(Screen.safeArea));
        }

        private void AppendQualitySection()
        {
            AppendSectionHeader("Quality");
            AppendKeyValue("Quality Level", GetQualityLevelName());
            AppendKeyValue("Anti Aliasing", QualitySettings.antiAliasing <= 0 ? "Disabled" : QualitySettings.antiAliasing + "x");
            AppendKeyValue("Anisotropic", QualitySettings.anisotropicFiltering.ToString());
            AppendKeyValue("LOD Bias", QualitySettings.lodBias.ToString("F2"));
            AppendKeyValue("Pixel Lights", QualitySettings.pixelLightCount.ToString());
            AppendKeyValue("Shadow Distance", QualitySettings.shadowDistance.ToString("F1"));
            AppendKeyValue("Texture Limit", QualitySettings.globalTextureMipmapLimit.ToString());
        }

        private void AppendAudioSection()
        {
            AudioConfiguration config = AudioSettings.GetConfiguration();

            AppendSectionHeader("Audio");
            AppendKeyValue("Sample Rate", AudioSettings.outputSampleRate + " Hz");
            AppendKeyValue("Speaker Mode", AudioSettings.speakerMode.ToString());
            AppendKeyValue("DSP Time", AudioSettings.dspTime.ToString("F2"));
            AppendKeyValue("DSP Buffer", config.dspBufferSize.ToString());
            AppendKeyValue("Real Voices", config.numRealVoices.ToString());
            AppendKeyValue("Virtual Voices", config.numVirtualVoices.ToString());
        }

        private void AppendSceneSection()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            AppendSectionHeader("Scene");
            AppendKeyValue("Active Scene", activeScene.IsValid() ? activeScene.name : "None");
            AppendKeyValue("Loaded Scenes", SceneManager.sceneCount.ToString());
            AppendKeyValue("Root Objects", activeScene.IsValid() ? activeScene.rootCount.ToString() : "0");
            AppendKeyValue("Scene Loaded", activeScene.isLoaded.ToString());
        }

        private void AppendSectionHeader(string title)
        {
            if (m_builder.Length > 0)
                m_builder.AppendLine();

            m_builder.Append("[ ").Append(title).AppendLine(" ]");
        }

        private void AppendKeyValue(string key, string value)
        {
            m_builder.Append(key).Append(": ").AppendLine(value);
        }

        private Rect GetPanelRect()
        {
            UpdatePanelSizeIfNeeded();

            float x = Position == OverlayPosition.TopRight || Position == OverlayPosition.BottomRight
                ? Screen.width - m_cachedPanelSize.x - Margin
                : Margin;

            float y = Position == OverlayPosition.BottomLeft || Position == OverlayPosition.BottomRight
                ? Screen.height - m_cachedPanelSize.y - Margin
                : Margin;

            return new Rect(x, y, m_cachedPanelSize.x, m_cachedPanelSize.y);
        }

        private void UpdatePanelSizeIfNeeded()
        {
            if (!m_isLayoutDirty)
                return;

            GUIContent content = new GUIContent(m_cachedText);
            float screenWidth = Mathf.Max(1f, Screen.width - (Margin * 2f));
            float maxWidth = Mathf.Min(MaxPanelWidth, screenWidth);
            float contentWidth = Mathf.Clamp(GetLongestLineWidth() + (Padding * 2f), MinPanelWidth, maxWidth);
            float contentHeight = m_labelStyle.CalcHeight(content, contentWidth - (Padding * 2f)) + (Padding * 2f);

            m_cachedPanelSize = new Vector2(contentWidth, contentHeight);
            m_isLayoutDirty = false;
        }

        private float GetLongestLineWidth()
        {
            if (string.IsNullOrEmpty(m_cachedText))
                return MinPanelWidth - (Padding * 2f);

            float width = 0f;
            string[] lines = m_cachedText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                float lineWidth = m_labelStyle.CalcSize(new GUIContent(lines[i])).x;
                if (lineWidth > width)
                    width = lineWidth;
            }

            return width;
        }

        private DisplaySectionFlags BuildFlagsFromSectionToggles()
        {
            DisplaySectionFlags flags = DisplaySectionFlags.None;

            if (ShowFps)
                flags |= DisplaySectionFlags.Fps;

            if (ShowFrameTimingDetails)
                flags |= DisplaySectionFlags.FrameTimingDetails;

            if (ShowTimeAndSync)
                flags |= DisplaySectionFlags.TimeAndSync;

            if (ShowMemory)
                flags |= DisplaySectionFlags.Memory;

            if (ShowGarbageCollection)
                flags |= DisplaySectionFlags.GarbageCollection;

            if (ShowSystem)
                flags |= DisplaySectionFlags.System;

            if (ShowScreen)
                flags |= DisplaySectionFlags.Screen;

            if (ShowQuality)
                flags |= DisplaySectionFlags.Quality;

            if (ShowAudio)
                flags |= DisplaySectionFlags.Audio;

            if (ShowScene)
                flags |= DisplaySectionFlags.Scene;

            return flags;
        }

        private void ApplyFlagsToSectionToggles(DisplaySectionFlags flags)
        {
            ShowFps = (flags & DisplaySectionFlags.Fps) != 0;
            ShowFrameTimingDetails = (flags & DisplaySectionFlags.FrameTimingDetails) != 0;
            ShowTimeAndSync = (flags & DisplaySectionFlags.TimeAndSync) != 0;
            ShowMemory = (flags & DisplaySectionFlags.Memory) != 0;
            ShowGarbageCollection = (flags & DisplaySectionFlags.GarbageCollection) != 0;
            ShowSystem = (flags & DisplaySectionFlags.System) != 0;
            ShowScreen = (flags & DisplaySectionFlags.Screen) != 0;
            ShowQuality = (flags & DisplaySectionFlags.Quality) != 0;
            ShowAudio = (flags & DisplaySectionFlags.Audio) != 0;
            ShowScene = (flags & DisplaySectionFlags.Scene) != 0;
        }

        private void CacheGcCollectionCounts()
        {
            m_lastGc0CollectionCount = GC.CollectionCount(0);
            m_lastGc1CollectionCount = GC.CollectionCount(1);
            m_lastGc2CollectionCount = GC.CollectionCount(2);
        }

        private static float SafeFpsFromDelta(float deltaTime)
        {
            return deltaTime > 0f ? 1f / deltaTime : 0f;
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            return value.ToString(unitIndex == 0 ? "F0" : "F2") + " " + units[unitIndex];
        }

        private static string FormatCollectionCount(int totalCount, int deltaCount)
        {
            string deltaText = deltaCount > 0 ? " (+" + deltaCount + ")" : string.Empty;
            return totalCount + deltaText;
        }

        private static string FormatDuration(float seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        private static string FormatTargetFrameRate(int targetFrameRate)
        {
            return targetFrameRate <= 0 ? "Platform Default" : targetFrameRate.ToString();
        }

        private static string FormatRect(Rect rect)
        {
            return rect.x.ToString("F0") + ", " + rect.y.ToString("F0") + ", " + rect.width.ToString("F0") + " x " + rect.height.ToString("F0");
        }

        private static float GetCurrentRefreshRateHz()
        {
#if UNITY_2021_2_OR_NEWER
            return (float)Screen.currentResolution.refreshRateRatio.value;
#else
            return Screen.currentResolution.refreshRate;
#endif
        }

        private static string GetQualityLevelName()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            string[] qualityNames = QualitySettings.names;

            if (qualityNames == null || qualityLevel < 0 || qualityLevel >= qualityNames.Length)
                return qualityLevel.ToString();

            return qualityNames[qualityLevel];
        }
    }
}
