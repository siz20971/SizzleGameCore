using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 지정한 단일 색상(검정, 흰색 등)으로 화면을 부드럽게 페이드 아웃/인 시키는 클래식 컬러 페이드 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_ColorFade", menuName = "Sizzle/Transitions/Profiles/Color Fade")]
    public class ColorFadeTransitionProfile : ScreenTransitionProfile
    {
        [Header("Fade Settings")]
        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");

        public Color FadeColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/ColorFadeTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;
            Color initialColor = m_color;
            initialColor.a = (phase == TransitionPhase.Enter) ? 0f : m_color.a;
            material.SetColor(PropColor, initialColor);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float alpha = (phase == TransitionPhase.Enter)
                ? Mathf.Clamp01(progress)
                : 1f - Mathf.Clamp01(progress);

            Color c = m_color;
            c.a *= alpha;
            material.SetColor(PropColor, c);
        }

        public override void OnCompleteMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;
            Color finalColor = m_color;
            finalColor.a = (phase == TransitionPhase.Enter) ? m_color.a : 0f;
            material.SetColor(PropColor, finalColor);
        }
    }
}
