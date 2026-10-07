using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 클래식 애니메이션 및 영화 연출에 자주 쓰이는 원형 조리개(Iris) 개폐 화면 전환 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_IrisCircle", menuName = "Sizzle/Transitions/Profiles/Iris Circle")]
    public class IrisCircleTransitionProfile : ScreenTransitionProfile
    {
        [Header("Iris Circle Settings")]
        [Tooltip("조리개 중심 좌표 (기본 0.5, 0.5 = 화면 정중앙)")]
        [SerializeField] private Vector2 m_center = new Vector2(0.5f, 0.5f);

        [SerializeField] private Color m_color = Color.black;

        [Tooltip("원 테두리의 부드러움 (0 = 칼같은 원, 0.5 = 매우 부드러움)")]
        [SerializeField, Range(0f, 0.5f)] private float m_softness = 0.02f;

        [Tooltip("반전 여부 (true = 중앙에서 원이 커지며 화면을 덮음)")]
        [SerializeField] private bool m_invert = false;

        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCenter = Shader.PropertyToID("_Center");
        private static readonly int PropRadius = Shader.PropertyToID("_Radius");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropInvert = Shader.PropertyToID("_Invert");

        public Vector2 Center { get => m_center; set => m_center = value; }
        public Color TransitionColor => m_color;
        public float Softness => m_softness;
        public bool Invert => m_invert;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/IrisCircleTransition");
                return m_shader;
            }
        }

        private float CalculateMaxDistance(float aspect)
        {
            float maxDx = Mathf.Max(m_center.x, 1f - m_center.x) * aspect;
            float maxDy = Mathf.Max(m_center.y, 1f - m_center.y);
            return Mathf.Sqrt(maxDx * maxDx + maxDy * maxDy);
        }

        private void GetRadiusBounds(float aspect, out float openRadius, out float closedRadius)
        {
            float maxDist = CalculateMaxDistance(aspect);
            // 소프트니스 밴드가 완전히 화면 바깥으로 나가도록 마진 확보 (어떤 softness 값이어도 100% 완전 투명 보장)
            openRadius = maxDist + m_softness * 2.0f + 0.05f;

            // 소프트니스 밴드가 중심점(0,0)을 완전히 지나쳐 화면 중심까지 100% 덮이도록 음수 반경 보장
            closedRadius = -m_softness * 2.0f - 0.05f;
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetFloat(PropAspectRatio, aspect);
            material.SetVector(PropCenter, new Vector4(m_center.x, m_center.y, 0f, 0f));
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropSoftness, m_softness);
            material.SetFloat(PropInvert, m_invert ? 1f : 0f);

            GetRadiusBounds(aspect, out float openRadius, out float closedRadius);

            float initialRadius;
            if (!m_invert)
            {
                // 일반 모드: Enter = openRadius(투명) -> closedRadius(완전 암전)
                initialRadius = (phase == TransitionPhase.Enter) ? openRadius : closedRadius;
            }
            else
            {
                // 반전 모드: Enter = closedRadius(투명) -> openRadius(완전 암전)
                initialRadius = (phase == TransitionPhase.Enter) ? closedRadius : openRadius;
            }

            material.SetFloat(PropRadius, initialRadius);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetFloat(PropAspectRatio, aspect);

            GetRadiusBounds(aspect, out float openRadius, out float closedRadius);
            float clamped = Mathf.Clamp01(progress);

            float radius;
            if (!m_invert)
            {
                // 일반 모드 (조리개 닫힘/열림)
                radius = (phase == TransitionPhase.Enter)
                    ? Mathf.Lerp(openRadius, closedRadius, clamped)
                    : Mathf.Lerp(closedRadius, openRadius, clamped);
            }
            else
            {
                // 반전 모드 (중앙에서 원이 번짐)
                radius = (phase == TransitionPhase.Enter)
                    ? Mathf.Lerp(closedRadius, openRadius, clamped)
                    : Mathf.Lerp(openRadius, closedRadius, clamped);
            }

            material.SetFloat(PropRadius, radius);
        }

        public override void OnCompleteMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            GetRadiusBounds(aspect, out float openRadius, out float closedRadius);

            float finalRadius;
            if (!m_invert)
            {
                finalRadius = (phase == TransitionPhase.Enter) ? closedRadius : openRadius;
            }
            else
            {
                finalRadius = (phase == TransitionPhase.Enter) ? openRadius : closedRadius;
            }

            material.SetFloat(PropRadius, finalRadius);
        }
    }
}
