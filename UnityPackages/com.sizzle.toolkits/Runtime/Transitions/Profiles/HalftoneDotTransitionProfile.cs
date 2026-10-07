using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary> 하프톤 도트 확산 웨이브 모드 </summary>
    public enum HalftoneWaveMode
    {
        /// <summary> 화면 전체 균일한 크기로 도트 확대/축소 </summary>
        Uniform = 0,
        /// <summary> 화면 중앙에서 바깥쪽으로 도트 확산 </summary>
        CenterOut = 1,
        /// <summary> 대각선 방향으로 순차 확산 </summary>
        Diagonal = 2
    }

    /// <summary>
    /// 만화 인쇄물 스타일의 하프톤 망점(Dot)들이 크기를 키우며 화면을 덮거나 줄어들며 드러내는 팝아트풍 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_HalftoneDot", menuName = "Sizzle/Transitions/Profiles/Halftone Dot")]
    public class HalftoneDotTransitionProfile : ScreenTransitionProfile
    {
        [Header("Halftone Settings")]
        [Tooltip("도트 격자 밀도 (화면 종횡비를 반영하여 찌그러짐 없는 1:1 완벽한 원형 도트가 렌더링됨)")]
        [SerializeField, Range(10f, 150f)] private float m_dotDensity = 40f;

        [Tooltip("도트 격자 회전 각도 (만화 인쇄 표준 45도 권장)")]
        [SerializeField, Range(0f, 90f)] private float m_angle = 45f;

        [Tooltip("전개 모드 (Uniform=균일 확대, CenterOut=중심 확산, Diagonal=대각선 와이프)")]
        [SerializeField] private HalftoneWaveMode m_waveMode = HalftoneWaveMode.CenterOut;

        [Tooltip("도트 테두리의 부드러움 (0 = 선명한 원형)")]
        [SerializeField, Range(0f, 0.2f)] private float m_softness = 0.02f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropDotDensity = Shader.PropertyToID("_DotDensity");
        private static readonly int PropAngle = Shader.PropertyToID("_Angle");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
        private static readonly int PropWaveMode = Shader.PropertyToID("_WaveMode");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        public float DotDensity => m_dotDensity;
        public float Angle => m_angle;
        public HalftoneWaveMode WaveMode => m_waveMode;
        public float Softness => m_softness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/HalftoneDotTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropDotDensity, m_dotDensity);
            material.SetFloat(PropAngle, m_angle);
            material.SetFloat(PropAspectRatio, aspect);
            material.SetFloat(PropSoftness, m_softness);
            material.SetFloat(PropWaveMode, (float)m_waveMode);

            float initialProgress = (phase == TransitionPhase.Enter) ? 0f : 1f;
            material.SetFloat(PropProgress, initialProgress);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetFloat(PropAspectRatio, aspect);

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
