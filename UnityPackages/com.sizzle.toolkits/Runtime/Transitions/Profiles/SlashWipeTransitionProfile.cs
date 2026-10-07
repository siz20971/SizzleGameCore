using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 사선/대각선 방향의 날카로운 검격(Slash) 섬광 효과와 함께 화면이 전환되는 트랜지션 프로필입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_SlashWipe", menuName = "Sizzle/Transitions/Profiles/Slash Wipe")]
    public class SlashWipeTransitionProfile : ScreenTransitionProfile
    {
        [Header("Slash Settings")]
        [Tooltip("검격 궤적 섬광 발광 색상")]
        [SerializeField] private Color m_slashColor = new Color(0.3f, 0.85f, 1f, 1f);

        [Tooltip("절단 사선 각도 (도)")]
        [SerializeField, Range(-80f, 80f)] private float m_angle = 35f;

        [Tooltip("검격 궤적 섬광 두께")]
        [SerializeField, Range(0.01f, 0.1f)] private float m_slashWidth = 0.03f;

        [SerializeField] private Color m_color = new Color(0.02f, 0.02f, 0.03f, 1f);
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropSlashColor = Shader.PropertyToID("_SlashColor");
        private static readonly int PropAngle = Shader.PropertyToID("_Angle");
        private static readonly int PropSlashWidth = Shader.PropertyToID("_SlashWidth");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        /// <summary>검격 궤적 섬광의 발광 색상입니다.</summary>
        public Color SlashColor => m_slashColor;

        /// <summary>검격 절단면의 사선 각도(도)입니다.</summary>
        public float Angle => m_angle;

        /// <summary>검격 궤적 섬광 라인의 두께입니다.</summary>
        public float SlashWidth => m_slashWidth;

        /// <summary>전환 후 덮이는 배경 색상입니다.</summary>
        public Color TransitionColor => m_color;

        /// <summary>트랜지션에 사용할 셰이더 템플릿입니다.</summary>
        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/SlashWipeTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetColor(PropSlashColor, m_slashColor);
            material.SetFloat(PropAngle, m_angle);
            material.SetFloat(PropSlashWidth, m_slashWidth);
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
