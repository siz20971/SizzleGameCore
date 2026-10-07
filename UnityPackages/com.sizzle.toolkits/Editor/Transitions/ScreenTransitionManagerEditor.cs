using Sizzle.Toolkits.Transitions;
using UnityEditor;
using UnityEngine;

namespace Sizzle.Toolkits.Editor.Transitions
{
    /// <summary>
    /// <see cref="ScreenTransitionManager"/> 컴포넌트의 커스텀 인스펙터 에디터입니다.
    /// 플레이 모드에서 트랜지션 실행 상태 모니터링, 강제 중단(Abort), 기본 프로필 테스트 버튼을 제공합니다.
    /// </summary>
    [CustomEditor(typeof(ScreenTransitionManager))]
    public class ScreenTransitionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var manager = (ScreenTransitionManager)target;
            if (manager == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Transition Controls (Editor Testing)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("트랜지션 테스트 및 Abort 제어는 Play Mode에서 활성화됩니다.", MessageType.Info);
                return;
            }

            bool isRunning = manager.IsTransitioning;
            EditorGUILayout.HelpBox(
                isRunning ? "상태: 트랜지션 실행 중 (Running)" : "상태: 대기 중 (Idle)",
                isRunning ? MessageType.Warning : MessageType.None);

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(!isRunning))
            {
                if (GUILayout.Button("Abort Current Transition", GUILayout.Height(28)))
                {
                    manager.Abort();
                }
            }

            using (new EditorGUI.DisabledScope(isRunning || manager.DefaultProfile == null))
            {
                if (GUILayout.Button("Test Default Transition (1s Mock)", GUILayout.Height(28)))
                {
                    _ = manager.TransitionAsync(async () =>
                    {
                        await Awaitable.WaitForSecondsAsync(1.0f);
                    });
                }
            }
        }
    }
}
