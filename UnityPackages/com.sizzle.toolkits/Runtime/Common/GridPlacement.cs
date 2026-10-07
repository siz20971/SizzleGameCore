using UnityEngine;
using Sizzle.Toolkits.Types;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// 지정한 평면(AxisPlane)과 앵커(AlignmentAnchor) 기준에 맞춰 자식 또는 대상 Transform들을 그리드 행렬 형태로 자동 정렬 및 배치하는 컴포넌트입니다.
    /// </summary>
    [ExecuteInEditMode]
    public class GridPlacement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private AxisPlane m_axis = AxisPlane.XY;
        [SerializeField] private AlignmentAnchor m_anchor = AlignmentAnchor.Center;
        [SerializeField] private Vector2 m_gridSize = new Vector2(1, 1);
        [SerializeField] private int m_rowCount = 2;

        [Space]
        [Tooltip("매 프레임 그리드 정렬을 자동으로 갱신할지 여부")]
        public bool AutoReposition = false;

        [Header("Targets")]
        [SerializeField] private Transform[] m_targets;

        /// <summary>
        /// 모든 직계 자식 Transform들을 정렬 대상(m_targets)으로 등록합니다.
        /// </summary>
        [ContextMenu("Add All Children to Targets")]
        public void AddAllChildrenToTargets()
        {
            m_targets = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                m_targets[i] = transform.GetChild(i);
        }

        /// <summary>
        /// 등록된 타겟 Transform들을 현재 그리드 설정(축, 앵커, 간격, 행 수)에 맞춰 재배치합니다.
        /// </summary>
        [ContextMenu("Reposition")]
        public void Reposition()
        {
            if (m_targets == null || m_targets.Length == 0)
            {
                return;
            }

            int countPerLine = Mathf.Max(1, m_rowCount);
            int lineCount = Mathf.CeilToInt((float)m_targets.Length / countPerLine);

            PlacementSettings placement = GetPlacementSettings();
            Vector2 anchorFactors = GetAnchorFactors();
            int horizontalItemCount = placement.isPrimaryHorizontal ? countPerLine : lineCount;
            int verticalItemCount = placement.isPrimaryHorizontal ? lineCount : countPerLine;

            Vector3 startPosition = transform.position
                - placement.horizontalAxis * ((horizontalItemCount - 1) * placement.horizontalSpacing * anchorFactors.x)
                - placement.verticalAxis * ((verticalItemCount - 1) * placement.verticalSpacing * anchorFactors.y);

            for (int i = 0; i < m_targets.Length; i++)
            {
                if (m_targets[i] == null)
                {
                    continue;
                }

                int primaryIndex = i % countPerLine;
                int secondaryIndex = i / countPerLine;

                Vector3 targetPosition = startPosition
                    + placement.primaryAxis * (primaryIndex * placement.primarySpacing)
                    + placement.secondaryAxis * (secondaryIndex * placement.secondarySpacing);

                m_targets[i].position = targetPosition;
            }
        }

        private PlacementSettings GetPlacementSettings()
        {
            switch (m_axis)
            {
                case AxisPlane.XY:
                    return new PlacementSettings(Vector3.right, m_gridSize.x, Vector3.up, m_gridSize.y, Vector3.right, m_gridSize.x, Vector3.up, m_gridSize.y, true);
                case AxisPlane.YX:
                    return new PlacementSettings(Vector3.up, m_gridSize.y, Vector3.right, m_gridSize.x, Vector3.right, m_gridSize.x, Vector3.up, m_gridSize.y, false);
                case AxisPlane.YZ:
                    return new PlacementSettings(Vector3.up, m_gridSize.y, Vector3.forward, m_gridSize.x, Vector3.up, m_gridSize.y, Vector3.forward, m_gridSize.x, true);
                case AxisPlane.ZY:
                    return new PlacementSettings(Vector3.forward, m_gridSize.x, Vector3.up, m_gridSize.y, Vector3.up, m_gridSize.y, Vector3.forward, m_gridSize.x, false);
                case AxisPlane.XZ:
                    return new PlacementSettings(Vector3.right, m_gridSize.x, Vector3.forward, m_gridSize.y, Vector3.right, m_gridSize.x, Vector3.forward, m_gridSize.y, true);
                case AxisPlane.ZX:
                    return new PlacementSettings(Vector3.forward, m_gridSize.y, Vector3.right, m_gridSize.x, Vector3.right, m_gridSize.x, Vector3.forward, m_gridSize.y, false);
                default:
                    return new PlacementSettings(Vector3.right, m_gridSize.x, Vector3.up, m_gridSize.y, Vector3.right, m_gridSize.x, Vector3.up, m_gridSize.y, true);
            }
        }

        private Vector2 GetAnchorFactors()
        {
            switch (m_anchor)
            {
                case AlignmentAnchor.BottomLeft:
                    return new Vector2(0f, 0f);
                case AlignmentAnchor.BottomCenter:
                    return new Vector2(0.5f, 0f);
                case AlignmentAnchor.BottomRight:
                    return new Vector2(1f, 0f);
                case AlignmentAnchor.MiddleLeft:
                    return new Vector2(0f, 0.5f);
                case AlignmentAnchor.Center:
                    return new Vector2(0.5f, 0.5f);
                case AlignmentAnchor.MiddleRight:
                    return new Vector2(1f, 0.5f);
                case AlignmentAnchor.TopLeft:
                    return new Vector2(0f, 1f);
                case AlignmentAnchor.TopCenter:
                    return new Vector2(0.5f, 1f);
                case AlignmentAnchor.TopRight:
                    return new Vector2(1f, 1f);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        private readonly struct PlacementSettings
        {
            public readonly Vector3 primaryAxis;
            public readonly float primarySpacing;
            public readonly Vector3 secondaryAxis;
            public readonly float secondarySpacing;
            public readonly Vector3 horizontalAxis;
            public readonly float horizontalSpacing;
            public readonly Vector3 verticalAxis;
            public readonly float verticalSpacing;
            public readonly bool isPrimaryHorizontal;

            public PlacementSettings(
                Vector3 primaryAxis,
                float primarySpacing,
                Vector3 secondaryAxis,
                float secondarySpacing,
                Vector3 horizontalAxis,
                float horizontalSpacing,
                Vector3 verticalAxis,
                float verticalSpacing,
                bool isPrimaryHorizontal)
            {
                this.primaryAxis = primaryAxis;
                this.primarySpacing = primarySpacing;
                this.secondaryAxis = secondaryAxis;
                this.secondarySpacing = secondarySpacing;
                this.horizontalAxis = horizontalAxis;
                this.horizontalSpacing = horizontalSpacing;
                this.verticalAxis = verticalAxis;
                this.verticalSpacing = verticalSpacing;
                this.isPrimaryHorizontal = isPrimaryHorizontal;
            }
        }

        private void Update()
        {
            if (AutoReposition)
            {
                Reposition();
            }
        }
    }
}
