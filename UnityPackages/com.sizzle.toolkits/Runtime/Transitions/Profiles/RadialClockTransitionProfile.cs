using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 시계 바늘이 회전하듯 각도(Angle)를 중심으로 방사형 부채꼴 모양으로 화면을 덮거나 여는 레이디얼 클락 와이프 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_RadialClock", menuName = "Sizzle/Transitions/Profiles/Radial Clock")]
    public class RadialClockTransitionProfile : ScreenTransitionProfile
    {
        [Header("Radial Clock Settings")]
        [Tooltip("회전 중심 좌표 (기본 0.5, 0.5)")]
        [SerializeField] private Vector2 m_center = new Vector2(0.5f, 0.5f);

        [SerializeField] private Color m_color = Color.black;

        [Tooltip("시작 각도 (0 = 3시 방향, 90 = 12시 방향, 180 = 9시 방향, 270 = 6시 방향)")]
        [SerializeField, Range(0f, 360f)] private float m_startAngle = 90f;

        [Tooltip("시계 방향 회전 여부")]
        [SerializeField] private bool m_clockwise = true;

        [Tooltip("회전 바늘 경계면의 부드러움 (0 = 칼같은 경계, 0.2 = 부드러운 그라데이션)")]
        [SerializeField, Range(0f, 0.2f)] private float m_softness = 0.01f;

        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCenter = Shader.PropertyToID("_Center");
        private static readonly int PropStartAngle = Shader.PropertyToID("_StartAngle");
        private static readonly int PropClockwise = Shader.PropertyToID("_Clockwise");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");

        public Vector2 Center => m_center;
        public Color TransitionColor => m_color;
        public float StartAngle => m_startAngle;
        public bool Clockwise => m_clockwise;
        public float Softness => m_softness;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/RadialClockTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetVector(PropCenter, new Vector4(m_center.x, m_center.y, 0f, 0f));
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropStartAngle, m_startAngle);
            material.SetFloat(PropClockwise, m_clockwise ? 1f : 0f);
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
            // Enter: 0 -> 1 (회전하며 덮음), Exit: 1 -> 0 (회전하며 열림)
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
