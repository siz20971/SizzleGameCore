using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 임의의 각도(Angle)와 경계면 소프트니스(Softness)를 지정하여 선형 방향으로 화면을 쓸어 넘기는 방향성 와이프 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_DirectionalWipe", menuName = "Sizzle/Transitions/Profiles/Directional Wipe")]
    public class DirectionalWipeTransitionProfile : ScreenTransitionProfile
    {
        [Header("Wipe Settings")]
        [Tooltip("와이프 진행 각도 (0 = 좌->우, 90 = 하->상, 180 = 우->좌, 270 = 상->하)")]
        [SerializeField, Range(0f, 360f)] private float m_angle = 0f;

        [Tooltip("경계선의 부드러움 (0 = 칼같은 경계)")]
        [SerializeField, Range(0f, 0.5f)] private float m_softness = 0.05f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropAngle = Shader.PropertyToID("_Angle");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");

        public float Angle => m_angle;
        public float Softness => m_softness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/DirectionalWipeTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropAngle, m_angle);
            material.SetFloat(PropSoftness, m_softness);
            material.SetFloat(PropAspectRatio, aspect);

            float initialProgress = (phase == TransitionPhase.Enter) ? 0f : 1f;
            material.SetFloat(PropProgress, initialProgress);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetFloat(PropAspectRatio, aspect);

            float clamped = Mathf.Clamp01(progress);
            // Enter: 0 -> 1 (화면 덮음), Exit: 1 -> 0 (화면 열림)
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
