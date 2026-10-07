using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if CORE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Sizzle.Toolkits.Transitions.Drivers
{
    /// <summary>
    /// URP(Universal Render Pipeline) 환경에서 카메라 후처리 또는 풀스크린 패스를 통해 화면 전환 셰이더 머티리얼을 렌더링하는 트랜지션 드라이버입니다.
    /// </summary>
    public class URPFullScreenTransitionDriver : MonoBehaviour, ITransitionDriver
    {
        [Tooltip("트랜지션을 적용할 카메라 (null일 경우 Camera.main 사용)")]
        [SerializeField] private Camera m_targetCamera;

        private readonly Dictionary<Shader, Material> m_materialCache = new Dictionary<Shader, Material>();
        private Material m_currentMaterial;
        private ScreenTransitionProfile m_currentProfile;
        private bool m_isRunning;
        private int m_executionId;
        private float m_currentProgress;

        /// <summary> 대상 렌더링 카메라 </summary>
        public Camera TargetCamera
        {
            get => m_targetCamera != null ? m_targetCamera : Camera.main;
            set => m_targetCamera = value;
        }

        /// <summary> 현재 적용 중인 트랜지션 머티리얼 </summary>
        public Material CurrentMaterial => m_currentMaterial;
        /// <summary> 현재 트랜지션 진행도 (0.0 ~ 1.0) </summary>
        public float CurrentProgress => m_currentProgress;
        /// <summary> 트랜지션 재생 중 여부 </summary>
        public bool IsRunning => m_isRunning;

        public async Awaitable PlayAsync(ScreenTransitionProfile profile, TransitionPhase phase, float duration = -1f, CancellationToken cancellationToken = default)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            m_executionId++;
            int currentId = m_executionId;

            m_currentProfile = profile;
            m_currentMaterial = GetOrCreateMaterial(profile);

            profile.OnInitMaterial(m_currentMaterial, phase);
            m_isRunning = true;

            float elapsed = 0f;
            float effectiveDuration = (duration > 0f) ? duration : profile.Duration;
            AnimationCurve curve = (phase == TransitionPhase.Enter) ? profile.EnterCurve : profile.ExitCurve;

            try
            {
                while (elapsed < effectiveDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (currentId != m_executionId)
                        return;

                    float delta = profile.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    elapsed += delta;

                    float t = Mathf.Clamp01(elapsed / effectiveDuration);
                    float evaluatedProgress = (curve != null) ? curve.Evaluate(t) : t;
                    m_currentProgress = evaluatedProgress;

                    profile.Apply(m_currentMaterial, evaluatedProgress, phase);

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                if (currentId == m_executionId)
                {
                    profile.OnCompleteMaterial(m_currentMaterial, phase);

                    if (phase == TransitionPhase.Exit)
                    {
                        m_isRunning = false;
                    }
                }
            }
            finally
            {
                if (currentId == m_executionId)
                {
                    m_isRunning = false;
                }
            }
        }

        public void SetProgress(float progress)
        {
            m_currentProgress = Mathf.Clamp01(progress);
            if (m_currentProfile != null && m_currentMaterial != null)
            {
                m_currentProfile.Apply(m_currentMaterial, m_currentProgress, TransitionPhase.Enter);
            }
        }

        public void Abort()
        {
            m_executionId++;
            m_isRunning = false;
            m_currentProgress = 0f;
        }

        private Material GetOrCreateMaterial(ScreenTransitionProfile profile)
        {
            Shader shader = profile.ShaderTemplate;
            if (shader == null)
            {
                Debug.LogWarning($"[URPFullScreenTransitionDriver] Profile '{profile.name}' has no ShaderTemplate.");
                return null;
            }

            if (!m_materialCache.TryGetValue(shader, out Material mat) || mat == null)
            {
                mat = profile.CreateInstanceMaterial();
                if (mat == null)
                    mat = new Material(shader) { hideFlags = HideFlags.DontSave };

                m_materialCache[shader] = mat;
            }

            return mat;
        }

        private void OnDestroy()
        {
            foreach (var kvp in m_materialCache)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            m_materialCache.Clear();
        }
    }
}
