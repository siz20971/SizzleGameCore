using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Sizzle.Toolkits.Transitions.Drivers
{
    /// <summary>
    /// uGUI Image 컴포넌트를 기반으로 캔버스 상에서 화면 전환 셰이더 머티리얼을 렌더링하는 트랜지션 드라이버입니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UGUITransitionDriver : MonoBehaviour, ITransitionDriver
    {
        [SerializeField] private Image m_image;

        private readonly Dictionary<Shader, Material> m_materialCache = new Dictionary<Shader, Material>();
        private Material m_currentMaterial;
        private ScreenTransitionProfile m_currentProfile;
        private bool m_isRunning;
        private int m_executionId;

        /// <summary> 현재 트랜지션 연출이 재생 중인지 여부 </summary>
        public bool IsRunning => m_isRunning;

        private void Awake()
        {
            if (m_image == null)
                m_image = GetComponent<Image>();

            // 초기 상태는 비활성화
            gameObject.SetActive(false);
        }

        public async Awaitable PlayAsync(ScreenTransitionProfile profile, TransitionPhase phase, float duration = -1f, CancellationToken cancellationToken = default)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // 이전 실행 중단
            m_executionId++;
            int currentId = m_executionId;

            m_currentProfile = profile;
            Material material = GetOrCreateMaterial(profile);
            m_currentMaterial = material;

            if (m_image != null)
                m_image.material = material;

            profile.OnInitMaterial(material, phase);
            gameObject.SetActive(true);
            m_isRunning = true;

            float elapsed = 0f;
            float effectiveDuration = (duration > 0f) ? duration : profile.Duration;
            AnimationCurve curve = (phase == TransitionPhase.Enter) ? profile.EnterCurve : profile.ExitCurve;

            try
            {
                while (elapsed < effectiveDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 다른 PlayAsync가 호출되었으면 조기 종료
                    if (currentId != m_executionId)
                        return;

                    float delta = profile.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    elapsed += delta;

                    float t = Mathf.Clamp01(elapsed / effectiveDuration);
                    float evaluatedProgress = (curve != null) ? curve.Evaluate(t) : t;

                    profile.Apply(material, evaluatedProgress, phase);

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                if (currentId == m_executionId)
                {
                    profile.OnCompleteMaterial(material, phase);

                    // Exit 연출이 끝나면 렌더러 비활성화
                    if (phase == TransitionPhase.Exit)
                    {
                        gameObject.SetActive(false);
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
            if (m_currentProfile != null && m_currentMaterial != null)
            {
                m_currentProfile.Apply(m_currentMaterial, Mathf.Clamp01(progress), TransitionPhase.Enter);
            }
        }

        public void Abort()
        {
            m_executionId++;
            m_isRunning = false;
            gameObject.SetActive(false);
        }

        private Material GetOrCreateMaterial(ScreenTransitionProfile profile)
        {
            Shader shader = profile.ShaderTemplate;
            if (shader == null)
            {
                Debug.LogWarning($"[UGUITransitionDriver] Profile '{profile.name}' has no ShaderTemplate. Fallback to default UI material.");
                return m_image.defaultMaterial;
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
