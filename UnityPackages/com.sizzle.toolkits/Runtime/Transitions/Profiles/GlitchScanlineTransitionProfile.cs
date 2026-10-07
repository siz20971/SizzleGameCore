using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 사이버펑크 스타일의 수평 스캔라인과 디지털 블록 노이즈 왜곡을 발생시키는 글리치 스캔라인 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_GlitchScanline", menuName = "Sizzle/Transitions/Profiles/Glitch Scanline")]
    public class GlitchScanlineTransitionProfile : ScreenTransitionProfile
    {
        [Header("Colors")]
        [Tooltip("기본 트랜지션 색상")]
        [SerializeField] private Color m_color = new Color(0.04f, 0.04f, 0.08f, 1f);

        [Tooltip("글리치 경계선 발광/포인트 색상")]
        [SerializeField] private Color m_glitchColor = new Color(0.15f, 0.85f, 1f, 1f);

        [Header("Scanline Settings")]
        [Tooltip("가로 스캔라인 개수")]
        [SerializeField, Range(50f, 600f)] private float m_scanlineCount = 240f;

        [Tooltip("스캔라인 명암 강도")]
        [SerializeField, Range(0f, 1f)] private float m_scanlineIntensity = 0.35f;

        [Header("Glitch Dynamics")]
        [Tooltip("가로 슬라이스 지터 강도")]
        [SerializeField, Range(0f, 0.5f)] private float m_jitterAmount = 0.15f;

        [Tooltip("노이즈 점멸/플리커 속도")]
        [SerializeField, Range(1f, 60f)] private float m_flickerSpeed = 25f;

        [Tooltip("디지털 블록 열 개수")]
        [SerializeField, Range(4f, 128f)] private float m_blockColumns = 32f;

        [Tooltip("디지털 블록 행 개수")]
        [SerializeField, Range(4f, 128f)] private float m_blockRows = 18f;

        [Tooltip("글리치 엣지 하이라이트 대역폭")]
        [SerializeField, Range(0.01f, 0.3f)] private float m_glitchEdgeWidth = 0.12f;

        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropGlitchColor = Shader.PropertyToID("_GlitchColor");
        private static readonly int PropScanlineCount = Shader.PropertyToID("_ScanlineCount");
        private static readonly int PropScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int PropJitterAmount = Shader.PropertyToID("_JitterAmount");
        private static readonly int PropFlickerSpeed = Shader.PropertyToID("_FlickerSpeed");
        private static readonly int PropBlockColumns = Shader.PropertyToID("_BlockColumns");
        private static readonly int PropBlockRows = Shader.PropertyToID("_BlockRows");
        private static readonly int PropGlitchEdgeWidth = Shader.PropertyToID("_GlitchEdgeWidth");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        public Color TransitionColor => m_color;
        public Color GlitchColor => m_glitchColor;
        public float ScanlineCount => m_scanlineCount;
        public float ScanlineIntensity => m_scanlineIntensity;
        public float JitterAmount => m_jitterAmount;
        public float FlickerSpeed => m_flickerSpeed;
        public float BlockColumns => m_blockColumns;
        public float BlockRows => m_blockRows;
        public float GlitchEdgeWidth => m_glitchEdgeWidth;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/GlitchScanlineTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            material.SetColor(PropColor, m_color);
            material.SetColor(PropGlitchColor, m_glitchColor);
            material.SetFloat(PropScanlineCount, m_scanlineCount);
            material.SetFloat(PropScanlineIntensity, m_scanlineIntensity);
            material.SetFloat(PropJitterAmount, m_jitterAmount);
            material.SetFloat(PropFlickerSpeed, m_flickerSpeed);
            material.SetFloat(PropBlockColumns, m_blockColumns);
            material.SetFloat(PropBlockRows, m_blockRows);
            material.SetFloat(PropGlitchEdgeWidth, m_glitchEdgeWidth);

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
