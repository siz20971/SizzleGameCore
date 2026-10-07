using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 동양화 수묵화 화선지에 먹물이 유기적으로 스며들며 번져나가는 효과를 모사한 잉크 블리드 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_InkBleed", menuName = "Sizzle/Transitions/Profiles/Ink Bleed")]
    public class InkBleedTransitionProfile : ScreenTransitionProfile
    {
        [Header("Ink Bleed Settings")]
        [Tooltip("먹물이 퍼져나가는 시작 중심 좌표 (기본 0.5, 0.5)")]
        [SerializeField] private Vector2 m_center = new Vector2(0.5f, 0.5f);

        [Tooltip("먹물 외곽선 유기적 비산 거칠기")]
        [SerializeField, Range(0f, 1f)] private float m_roughness = 0.4f;

        [Tooltip("화선지 흡수 번짐 부드러움 (0 = 선명한 잉크 자국)")]
        [SerializeField, Range(0f, 0.3f)] private float m_feather = 0.06f;

        [SerializeField] private Color m_color = new Color(0.03f, 0.03f, 0.04f, 1f);
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCenter = Shader.PropertyToID("_Center");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropRoughness = Shader.PropertyToID("_Roughness");
        private static readonly int PropFeather = Shader.PropertyToID("_Feather");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        public Vector2 Center { get => m_center; set => m_center = value; }
        public float Roughness => m_roughness;
        public float Feather => m_feather;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/InkBleedTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetVector(PropCenter, new Vector4(m_center.x, m_center.y, 0f, 0f));
            material.SetFloat(PropAspectRatio, aspect);
            material.SetFloat(PropRoughness, m_roughness);
            material.SetFloat(PropFeather, m_feather);

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
