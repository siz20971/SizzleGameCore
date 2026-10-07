using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 모자이크 픽셀 블록 단위로 화면을 쪼개고 해상도를 낮추며 전환하는 레트로 픽셀레이트 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_Pixelate", menuName = "Sizzle/Transitions/Profiles/Pixelate")]
    public class PixelateTransitionProfile : ScreenTransitionProfile
    {
        [Tooltip("가로 픽셀 분할 수 (Square Pixels 활성화 시 1:1 완벽한 정사각형 픽셀의 가로 열 기준 개수)")]
        [SerializeField, Range(4f, 200f)] private float m_pixelCount = 48f;

        [Tooltip("1:1 완벽한 정사각형 픽셀 강제 여부 (가로 분할 수를 기준으로 화면 종횡비를 반영하여 찌그러짐 없는 1:1 정사각 픽셀로 자동 계산)")]
        [SerializeField] private bool m_squarePixels = true;

        [Tooltip("랜덤 패턴 시드값")]
        [SerializeField] private float m_seed = 1337f;

        [Tooltip("블록 알파 전환 부드러움 (0 = 디지털 블록 On/Off)")]
        [SerializeField, Range(0f, 0.2f)] private float m_softness = 0f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropPixelCount = Shader.PropertyToID("_PixelCount");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropSquarePixels = Shader.PropertyToID("_SquarePixels");
        private static readonly int PropSeed = Shader.PropertyToID("_Seed");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");

        public float PixelCount => m_pixelCount;
        public bool SquarePixels => m_squarePixels;
        public float Seed => m_seed;
        public float Softness => m_softness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/PixelateTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropPixelCount, m_pixelCount);
            material.SetFloat(PropAspectRatio, aspect);
            material.SetFloat(PropSquarePixels, m_squarePixels ? 1f : 0f);
            material.SetFloat(PropSeed, m_seed);
            material.SetFloat(PropSoftness, m_softness);

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
