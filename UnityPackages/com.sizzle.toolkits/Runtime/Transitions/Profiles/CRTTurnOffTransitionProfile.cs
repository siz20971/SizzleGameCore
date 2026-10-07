using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 옛날 브라운관(CRT) 모니터의 전원이 꺼지며 수직/수평으로 전자빔이 수축되어 한 점의 섬광으로 사라지는 레트로 TV 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_CRTTurnOff", menuName = "Sizzle/Transitions/Profiles/CRT Turn Off")]
    public class CRTTurnOffTransitionProfile : ScreenTransitionProfile
    {
        [Header("CRT Turn-Off Settings")]
        [Tooltip("전자빔 섬광 발광 색상")]
        [SerializeField] private Color m_beamColor = new Color(0.9f, 0.95f, 1f, 1f);

        [Tooltip("전자빔 발광 강도")]
        [SerializeField, Range(1f, 5f)] private float m_beamIntensity = 2.5f;

        [Tooltip("수평 수축 빔 두께")]
        [SerializeField, Range(0.001f, 0.02f)] private float m_beamThickness = 0.005f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropBeamColor = Shader.PropertyToID("_BeamColor");
        private static readonly int PropBeamIntensity = Shader.PropertyToID("_BeamIntensity");
        private static readonly int PropBeamThickness = Shader.PropertyToID("_BeamThickness");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        public Color BeamColor => m_beamColor;
        public float BeamIntensity => m_beamIntensity;
        public float BeamThickness => m_beamThickness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/CRTTurnOffTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetColor(PropBeamColor, m_beamColor);
            material.SetFloat(PropBeamIntensity, m_beamIntensity);
            material.SetFloat(PropBeamThickness, m_beamThickness);
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
