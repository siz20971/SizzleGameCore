using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary> 블라인드 슬랫 분할 방향 </summary>
    public enum BlindsDirection
    {
        /// <summary> 가로 방향 슬랫 </summary>
        Horizontal = 0,
        /// <summary> 세로 방향 슬랫 </summary>
        Vertical = 1
    }

    /// <summary>
    /// 베네시안 블라인드(Venetian Blinds)가 3D 회전하며 화면을 가리거나 여는 화면 전환 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_Blinds", menuName = "Sizzle/Transitions/Profiles/Venetian Blinds")]
    public class BlindsTransitionProfile : ScreenTransitionProfile
    {
        [Header("Blinds Settings")]
        [Tooltip("블라인드 슬랫 개수")]
        [SerializeField, Range(4f, 64f)] private float m_slatCount = 16f;

        [Tooltip("슬랫 분할 방향 (수평 / 수직)")]
        [SerializeField] private BlindsDirection m_direction = BlindsDirection.Horizontal;

        [Tooltip("슬랫 회전 순차 지연율 (0 = 동시 회전, 1 = 물결치듯 순차 회전)")]
        [SerializeField, Range(0f, 1f)] private float m_waveStagger = 0.5f;

        [Tooltip("3D 회전 입체 음영 깊이")]
        [SerializeField, Range(0f, 1f)] private float m_shadingIntensity = 0.35f;

        [Tooltip("슬랫 모서리 경계 부드러움")]
        [SerializeField, Range(0f, 0.1f)] private float m_softness = 0.01f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropSlatCount = Shader.PropertyToID("_SlatCount");
        private static readonly int PropDirection = Shader.PropertyToID("_Direction");
        private static readonly int PropWaveStagger = Shader.PropertyToID("_WaveStagger");
        private static readonly int PropShadingIntensity = Shader.PropertyToID("_ShadingIntensity");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        public float SlatCount => m_slatCount;
        public BlindsDirection Direction => m_direction;
        public float WaveStagger => m_waveStagger;
        public float ShadingIntensity => m_shadingIntensity;
        public float Softness => m_softness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/BlindsTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            material.SetColor(PropColor, m_color);
            material.SetFloat(PropSlatCount, m_slatCount);
            material.SetFloat(PropDirection, (float)m_direction);
            material.SetFloat(PropWaveStagger, m_waveStagger);
            material.SetFloat(PropShadingIntensity, m_shadingIntensity);
            material.SetFloat(PropSoftness, m_softness);

            float initialProgress = (phase == TransitionPhase.Enter) ? 0f : 1f;
            material.SetFloat(PropProgress, initialProgress);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float clamped = Mathf.Clamp01(progress);
            float p = (phase == TransitionPhase.Enter) ? clamped : (1f - clamped);

            material.SetFloat(PropProgress, p);
        }

        public override void OnCompleteMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;
            float finalProgress = (phase == TransitionPhase.Enter) ? 1f : 0f;
            material.SetFloat(PropProgress, finalProgress);
        }
    }
}
