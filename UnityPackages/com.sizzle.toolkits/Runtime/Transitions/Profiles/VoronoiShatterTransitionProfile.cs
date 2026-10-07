using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 화면이 불규칙한 유리/석재 파편처럼 산산이 부서지며(Voronoi Shatter) 전환되는 트랜지션 프로필입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_VoronoiShatter", menuName = "Sizzle/Transitions/Profiles/Voronoi Shatter")]
    public class VoronoiShatterTransitionProfile : ScreenTransitionProfile
    {
        [Header("Voronoi Shatter Settings")]
        [Tooltip("보로노이 파편 균열선 발광 색상")]
        [SerializeField] private Color m_crackColor = new Color(0.7f, 0.85f, 1f, 1f);

        [Tooltip("파편 밀도 (보로노이 셀 스케일)")]
        [SerializeField, Range(3f, 20f)] private float m_shardScale = 8f;

        [Tooltip("균열선(크랙) 두께")]
        [SerializeField, Range(0.01f, 0.15f)] private float m_crackWidth = 0.05f;

        [SerializeField] private Color m_color = new Color(0.02f, 0.02f, 0.03f, 1f);
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropCrackColor = Shader.PropertyToID("_CrackColor");
        private static readonly int PropShardScale = Shader.PropertyToID("_ShardScale");
        private static readonly int PropCrackWidth = Shader.PropertyToID("_CrackWidth");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        /// <summary>보로노이 파편 사이의 균열선 발광 색상입니다.</summary>
        public Color CrackColor => m_crackColor;

        /// <summary>파편의 밀도 및 크기를 결정하는 보로노이 셀 스케일입니다.</summary>
        public float ShardScale => m_shardScale;

        /// <summary>파편 분할 균열선(크랙)의 두께입니다.</summary>
        public float CrackWidth => m_crackWidth;

        /// <summary>전환 후 덮이는 배경 색상입니다.</summary>
        public Color TransitionColor => m_color;

        /// <summary>트랜지션에 사용할 셰이더 템플릿입니다.</summary>
        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/VoronoiShatterTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetColor(PropCrackColor, m_crackColor);
            material.SetFloat(PropShardScale, m_shardScale);
            material.SetFloat(PropCrackWidth, m_crackWidth);
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
