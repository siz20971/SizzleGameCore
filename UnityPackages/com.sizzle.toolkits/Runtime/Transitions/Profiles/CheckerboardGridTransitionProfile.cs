using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary> 체커보드 타일 등장 순서 모드 </summary>
    public enum CheckerboardTransitionMode
    {
        /// <summary> 흑백 타일 번갈아 등장 </summary>
        Alternating = 0,
        /// <summary> 방향성 웨이브를 타며 순차 등장 </summary>
        DirectionalWave = 1,
        /// <summary> 모든 타일 동시 등장 </summary>
        Simultaneous = 2
    }

    /// <summary> 체커보드 웨이브 진행 방향 </summary>
    public enum CheckerboardWaveDirection
    {
        BottomLeftToTopRight = 0,   // 좌하단 -> 우상단 (기본 대각선)
        TopLeftToBottomRight = 1,   // 좌상단 -> 우하단
        BottomRightToTopLeft = 2,   // 우하단 -> 좌상단
        TopRightToBottomLeft = 3,   // 우상단 -> 좌하단
        LeftToRight = 4,            // 좌 -> 우
        RightToLeft = 5,            // 우 -> 좌
        BottomToTop = 6,            // 하 -> 상
        TopToBottom = 7             // 상 -> 하
    }

    /// <summary> 체커보드 셀 단위 형태 (정사각형 또는 다이아몬드) </summary>
    public enum CheckerboardCellShape
    {
        /// <summary> 직교 정사각형 </summary>
        Square = 0,
        /// <summary> 45도 회전된 마름모 다이아몬드 </summary>
        Diamond = 1
    }

    /// <summary>
    /// 체커보드(바둑판) 격자 타일들이 순차적 또는 번갈아 가며 스케일/페이드되어 화면을 덮거나 여는 트랜지션 프로파일입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "P_Transition_CheckerboardGrid", menuName = "Sizzle/Transitions/Profiles/Checkerboard Grid")]
    public class CheckerboardGridTransitionProfile : ScreenTransitionProfile
    {
        [Header("Grid Partition")]
        [Tooltip("가로 분할 타일 수 (Columns).\nSquare Grid가 활성화되어 있으면 이 Columns 값을 기준으로 화면 종횡비를 자동 계산하여 타일 하나하나가 왜곡 없는 완벽한 1:1 정사각형이 되도록 세로 타일 수를 결정합니다.")]
        [SerializeField, Range(2f, 64f)] private float m_columns = 16f;

        [Tooltip("세로 분할 타일 수 (Rows).\nSquare Grid가 비활성화(False)되어 있을 때만 수동으로 사용됩니다.")]
        [SerializeField, Range(2f, 64f)] private float m_rows = 9f;

        [Tooltip("정사각형 격자 모드 (Square Grid).\n활성화 시 Columns 값을 가로 타일 수 기준으로 삼고 화면 종횡비(Aspect Ratio)를 반영하여, 각 타일이 왜곡 없는 1:1 완벽한 정사각형이 되도록 세로 타일 수를 자동 계산하고 상하 여백을 중앙 정렬합니다.")]
        [SerializeField] private bool m_squareGrid = true;

        [Header("Animation & Shape")]
        [Tooltip("체커보드 전개 모드:\n- Alternating: 체스판처럼 짝수/홀수 타일이 번갈아 전개\n- DirectionalWave: 지정한 방향으로 물결치듯 순차 전개\n- Simultaneous: 모든 타일이 동시에 전개")]
        [SerializeField] private CheckerboardTransitionMode m_mode = CheckerboardTransitionMode.DirectionalWave;

        [Tooltip("DirectionalWave 모드일 때 전개 방향 (대각선 4방향 및 직선 4방향)")]
        [SerializeField] private CheckerboardWaveDirection m_waveDirection = CheckerboardWaveDirection.BottomLeftToTopRight;

        [Tooltip("타일 확장 형태 (Square: 사각형, Diamond: 다이아몬드)")]
        [SerializeField] private CheckerboardCellShape m_shape = CheckerboardCellShape.Square;

        [Tooltip("타일 경계면 부드러움 (0 = 선명한 칼 경계)")]
        [SerializeField, Range(0f, 0.2f)] private float m_softness = 0f;

        [SerializeField] private Color m_color = Color.black;
        [SerializeField] private Shader m_shader;

        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropColumns = Shader.PropertyToID("_Columns");
        private static readonly int PropRows = Shader.PropertyToID("_Rows");
        private static readonly int PropSquareGrid = Shader.PropertyToID("_SquareGrid");
        private static readonly int PropAspectRatio = Shader.PropertyToID("_AspectRatio");
        private static readonly int PropCheckerboardMode = Shader.PropertyToID("_CheckerboardMode");
        private static readonly int PropWaveDirection = Shader.PropertyToID("_WaveDirection");
        private static readonly int PropCellShape = Shader.PropertyToID("_CellShape");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");
        private static readonly int PropSoftness = Shader.PropertyToID("_Softness");

        public float Columns => m_columns;
        public float Rows => m_rows;
        public bool SquareGrid => m_squareGrid;
        public CheckerboardTransitionMode Mode => m_mode;
        public CheckerboardWaveDirection WaveDirection => m_waveDirection;
        public CheckerboardCellShape Shape => m_shape;
        public float Softness => m_softness;
        public Color TransitionColor => m_color;

        public override Shader ShaderTemplate
        {
            get
            {
                if (m_shader == null)
                    m_shader = Shader.Find("Sizzle/Transitions/CheckerboardGridTransition");
                return m_shader;
            }
        }

        public override void OnInitMaterial(Material material, TransitionPhase phase)
        {
            if (material == null) return;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            material.SetColor(PropColor, m_color);
            material.SetFloat(PropColumns, m_columns);
            material.SetFloat(PropRows, m_rows);
            material.SetFloat(PropSquareGrid, m_squareGrid ? 1f : 0f);
            material.SetFloat(PropAspectRatio, aspect);
            material.SetFloat(PropCheckerboardMode, (float)m_mode);
            material.SetFloat(PropWaveDirection, (float)m_waveDirection);
            material.SetFloat(PropCellShape, (float)m_shape);
            material.SetFloat(PropSoftness, m_softness);

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
