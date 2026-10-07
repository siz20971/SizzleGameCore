using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 아날로그 비디오 테이프 되감기(VHS Rewind) 느낌의 수평 트래킹 왜곡 및 노이즈 효과 프로필입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_VHSRewind", menuName = "Sizzle/Transitions/Profiles/VHS Rewind")]
    public class VHSRewindTransitionProfile : ScreenTransitionProfile
    {
        [Header("VHS Settings")]
        [Tooltip("정전기 스노우 노이즈 스파크 색상")]
        [SerializeField] private Color m_noiseColor = new Color(0.85f, 0.9f, 0.95f, 1f);

        [Tooltip("트래킹 왜곡 바 상하 이동 속도")]
        [SerializeField, Range(1f, 40f)] private float m_trackingSpeed = 12f;

        [Tooltip("아날로그 스노우 노이즈 강도")]
        [SerializeField, Range(0f, 1f)] private float m_noiseIntensity = 0.5f;

        [Tooltip("트래킹 왜곡 바 높이/두께")]
        [SerializeField, Range(0.01f, 0.3f)] private float m_trackingBarHeight = 0.12f;

        [SerializeField] private Color m_color = new Color(0.02f, 0.02f, 0.05f, 1f);
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropNoiseColor = Shader.PropertyToID("_NoiseColor");
        private static readonly int PropTrackingSpeed = Shader.PropertyToID("_TrackingSpeed");
        private static readonly int PropNoiseIntensity = Shader.PropertyToID("_NoiseIntensity");
        private static readonly int PropTrackingBarHeight = Shader.PropertyToID("_TrackingBarHeight");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        /// <summary>정전기 스노우 노이즈 스파크의 색상입니다.</summary>
        public Color NoiseColor => m_noiseColor;

        /// <summary>화면 상하로 스캔/롤링되는 트래킹 왜곡 라인의 이동 속도입니다.</summary>
        public float TrackingSpeed => m_trackingSpeed;

        /// <summary>아날로그 스노우 노이즈의 강도입니다.</summary>
        public float NoiseIntensity => m_noiseIntensity;

        /// <summary>수평 트래킹 왜곡 바의 세로 두께/높이 비율입니다.</summary>
        public float TrackingBarHeight => m_trackingBarHeight;

        /// <summary>전환 후 덮이는 배경 색상입니다.</summary>
        public Color TransitionColor => m_color;

        /// <summary>트랜지션에 사용할 셰이더 템플릿입니다.</summary>
        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/VHSRewindTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            material.SetColor(PropColor, m_color);
            material.SetColor(PropNoiseColor, m_noiseColor);
            material.SetFloat(PropTrackingSpeed, m_trackingSpeed);
            material.SetFloat(PropNoiseIntensity, m_noiseIntensity);
            material.SetFloat(PropTrackingBarHeight, m_trackingBarHeight);

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
