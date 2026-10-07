using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 그레이스케일 마스크 텍스처의 픽셀 밝기(Luminance) 순서에 따라 화면을 닦아내는 범용 텍스처 마스크 와이프 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_MaskWipe", menuName = "Sizzle/Transitions/Profiles/Mask Wipe")]
    public class MaskWipeTransitionProfile : ScreenTransitionProfile
    {
        [Header("Mask Settings")]
        [SerializeField] private Texture2D m_maskTexture;
        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private bool m_invert = false;
        [SerializeField, Range(0f, 0.5f)] private float m_softness = 0.0f;
        [SerializeField] private Shader m_shader;

        private static readonly int PropMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCutOff = Shader.PropertyToID("_CutOff");
        private static readonly int PropInvert = Shader.PropertyToID("_Invert");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");

        public Texture2D MaskTexture => m_maskTexture;
        public Color TransitionColor => m_color;
        public bool Invert => m_invert;
        public float Softness => m_softness;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/MaskWipeTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            if (m_maskTexture != null)
                material.SetTexture(PropMainTex, m_maskTexture);

            material.SetColor(PropColor, m_color);
            material.SetFloat(PropInvert, m_invert ? 1f : 0f);
            material.SetFloat(PropSoftness, m_softness);

            float initialCutoff = (phase == TransitionPhase.Enter) ? -0.1f : 1.1f;
            material.SetFloat(PropCutOff, initialCutoff);
        }

        public override void Apply(Material material, float progress, TransitionPhase phase)
        {
            if (material == null) return;

            float clamped = Mathf.Clamp01(progress);
            float cutoff = (phase == TransitionPhase.Enter)
                ? Mathf.Lerp(-0.1f, 1.1f, clamped)
                : Mathf.Lerp(1.1f, -0.1f, clamped);

            material.SetFloat(PropCutOff, cutoff);
        }

        public override void OnCompleteMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float finalCutoff = (phase == TransitionPhase.Enter) ? 1.1f : -0.1f;
            material.SetFloat(PropCutOff, finalCutoff);
        }
    }
}
