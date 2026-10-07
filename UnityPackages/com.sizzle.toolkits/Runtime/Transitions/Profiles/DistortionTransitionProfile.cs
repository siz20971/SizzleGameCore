using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 노멀/웨이브 디스토션을 적용하여 화면을 굴절 및 왜곡시키며 전환하는 디스토션 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_Distortion", menuName = "Sizzle/Transitions/Profiles/Distortion")]
    public class DistortionTransitionProfile : ScreenTransitionProfile
    {
        [Header("Transition Textures")]
        [SerializeField] private Texture2D m_mainTexture;
        [SerializeField] private Texture2D m_multiplyTexture;
        [SerializeField, Range(0f, 1f)] private float m_textureBlend = 0.0f;

        [Header("Distortion Properties")]
        [SerializeField] private Color m_color = Color.black;
        [SerializeField, Range(-1f, 1f)] private float m_waveStrength = 0.0f;
        [SerializeField, Range(-1f, 1f)] private float m_swirlStrength = 0.0f;
        [SerializeField] private float m_timeScale = 0.0f;

        [SerializeField] private Shader m_shader;

        private static readonly int PropMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PropMultiplyTex = Shader.PropertyToID("_MultiplyTex");
        private static readonly int PropTextureBlend = Shader.PropertyToID("_TextureBlend");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCutOff = Shader.PropertyToID("_CutOff");
        private static readonly int PropWaveStrength = Shader.PropertyToID("_WaveStrength");
        private static readonly int PropSwirlStrength = Shader.PropertyToID("_SwirlStrength");
        private static readonly int PropTimeScale = Shader.PropertyToID("_TimeScale");

        public Texture2D MainTexture => m_mainTexture;
        public Texture2D MultiplyTexture => m_multiplyTexture;
        public float TextureBlend => m_textureBlend;
        public Color TransitionColor => m_color;
        public float WaveStrength => m_waveStrength;
        public float SwirlStrength => m_swirlStrength;
        public float TimeScale => m_timeScale;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                {
                    m_shader = Shader.Find("Sizzle/Transitions/DistortionTransition");
                    if (m_shader == null)
                        m_shader = Shader.Find("NKTools/UI/SimpleTransition");
                }
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            if (m_mainTexture != null)
                material.SetTexture(PropMainTex, m_mainTexture);
            if (m_multiplyTexture != null)
                material.SetTexture(PropMultiplyTex, m_multiplyTexture);

            material.SetFloat(PropTextureBlend, m_textureBlend);
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropWaveStrength, m_waveStrength);
            material.SetFloat(PropSwirlStrength, m_swirlStrength);
            material.SetFloat(PropTimeScale, m_timeScale);

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
