using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 노이즈 텍스처를 기반으로 화면이 불에 타들어가듯 경계선이 발광하며 사라지거나 나타나는 번 디졸브(Burn Dissolve) 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_BurnDissolve", menuName = "Sizzle/Transitions/Profiles/Burn Dissolve")]
    public class BurnDissolveTransitionProfile : ScreenTransitionProfile
    {
        [Header("Dissolve Settings")]
        [SerializeField] private Texture2D m_noiseTexture;
        [SerializeField] private Color m_color = Color.black;

        [Header("Burn Edge Settings")]
        [Tooltip("타들어가는 경계선 발광 색상 (HDR 지원 시 고강도 블룸 가능)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color m_edgeColor = new Color(1f, 0.4f, 0.05f, 1.5f);

        [Tooltip("경계선 두께")]
        [SerializeField, Range(0.001f, 0.2f)] private float m_edgeWidth = 0.06f;

        [SerializeField] private Shader m_shader;

        private static readonly int PropMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropEdgeColor = Shader.PropertyToID("_EdgeColor");
        private static readonly int PropEdgeWidth = Shader.PropertyToID("_EdgeWidth");
        private static readonly int PropCutOff = Shader.PropertyToID("_CutOff");

        public Texture2D NoiseTexture => m_noiseTexture;
        public Color TransitionColor => m_color;
        public Color EdgeColor => m_edgeColor;
        public float EdgeWidth => m_edgeWidth;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/BurnDissolveTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            if (m_noiseTexture != null)
                material.SetTexture(PropMainTex, m_noiseTexture);

            material.SetColor(PropColor, m_color);
            material.SetColor(PropEdgeColor, m_edgeColor);
            material.SetFloat(PropEdgeWidth, m_edgeWidth);

            float initialCutoff = (phase == TransitionPhase.Enter) ? -0.1f : 1.15f;
            material.SetFloat(PropCutOff, initialCutoff);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float clamped = Mathf.Clamp01(progress);
            // Enter: -0.1 -> 1.15, Exit: 1.15 -> -0.1
            float cutoff = (phase == TransitionPhase.Enter)
                ? Mathf.Lerp(-0.1f, 1.15f, clamped)
                : Mathf.Lerp(1.15f, -0.1f, clamped);

            material.SetFloat(PropCutOff, cutoff);
        }

        public override void OnCompleteMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;
            float finalCutoff = (phase == TransitionPhase.Enter) ? 1.15f : -0.1f;
            material.SetFloat(PropCutOff, finalCutoff);
        }
    }
}
