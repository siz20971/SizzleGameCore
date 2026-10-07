using System;
using System.Collections;
using Sizzle.Toolkits.Transitions;
using Sizzle.Toolkits.Transitions.Drivers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sizzle.Toolkits.Demo.Transitions
{
    /// <summary>
    /// <see cref="ScreenTransitionManager"/> 및 17종의 트랜지션 프로필 효과를 테스트하기 위한 OnGUI 기반 데모 컨트롤러입니다.
    /// </summary>
    public class TransitionTestScene : MonoBehaviour
    {
        [Header("New Transition System")]
        [SerializeField] private ScreenTransitionManager m_transitionManager;

        [Tooltip("테스트할 트랜지션 프로파일 목록 (Array)")]
        [SerializeField] private ScreenTransitionProfile[] m_profiles;

        private string m_currentRunningProfileName = null;

        public ScreenTransitionProfile[] Profiles
        {
            get => m_profiles;
            set => m_profiles = value;
        }

        private void Awake()
        {
            if (m_transitionManager == null)
            {
                m_transitionManager = FindFirstObjectByType<ScreenTransitionManager>();
            }

            // 씬에 ScreenTransitionManager가 없는 경우 자동 생성 및 UGUITransitionDriver 연결
            if (m_transitionManager == null)
            {
                var driver = FindFirstObjectByType<UGUITransitionDriver>();
                var managerGo = new GameObject("ScreenTransitionManager");
                m_transitionManager = managerGo.AddComponent<ScreenTransitionManager>();
                if (driver != null)
                {
                    m_transitionManager.Driver = driver;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 400, 620));
            GUILayout.Label("<b>Sizzle Screen Transition System</b>", GUI.skin.label);
            GUILayout.Space(5);

            string statusText = m_transitionManager != null && m_transitionManager.IsTransitioning
                ? $"<color=yellow>Status: Running ({m_currentRunningProfileName})</color>"
                : "<color=green>Status: Idle</color>";
            GUILayout.Label(statusText);
            GUILayout.Space(10);

            if (m_transitionManager != null)
            {
                GUILayout.Label("<b>[Registered Profiles (Click to Play/Interrupt)]</b>");

                if (m_profiles != null && m_profiles.Length > 0)
                {
                    for (int i = 0; i < m_profiles.Length; i++)
                    {
                        var profile = m_profiles[i];
                        if (profile == null) continue;

                        string btnText = string.IsNullOrEmpty(profile.name)
                            ? $"Profile [{i}] (1s Mock Task)"
                            : $"{profile.name} (1s Mock Task)";

                        if (GUILayout.Button(btnText))
                        {
                            RunTransition(profile);
                        }
                    }
                }
                else
                {
                    GUILayout.Label("<i>No profiles assigned to m_profiles array.</i>");
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Abort Current Transition"))
                {
                    m_transitionManager.Abort();
                    m_currentRunningProfileName = null;
                }
            }
            else
            {
                GUILayout.Label("<i>ScreenTransitionManager not assigned.</i>");
            }

            GUILayout.EndArea();
        }

        private async void RunTransition(ScreenTransitionProfile profile)
        {
            if (m_transitionManager == null) return;

            string profileName = profile != null ? profile.name : "Default";
            m_currentRunningProfileName = profileName;

            Debug.Log($"[TransitionTestScene] Requesting transition with '{profileName}' (Interrupts previous if active)");

            try
            {
                await m_transitionManager.TransitionAsync(async () =>
                {
                    Debug.Log($"[TransitionTestScene] Screen covered by '{profileName}'. Simulating 1s async loading task...");
                    await Awaitable.WaitForSecondsAsync(1.0f);
                    Debug.Log($"[TransitionTestScene] 1s task finished for '{profileName}'. Proceeding to Exit...");
                }, profile);

                if (m_currentRunningProfileName == profileName)
                {
                    Debug.Log($"[TransitionTestScene] Transition '{profileName}' fully completed!");
                    m_currentRunningProfileName = null;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[TransitionTestScene] Transition '{profileName}' was cancelled/interrupted by a new transition.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TransitionTestScene] Transition '{profileName}' failed: {ex.Message}");
                m_currentRunningProfileName = null;
            }
        }
    }
}