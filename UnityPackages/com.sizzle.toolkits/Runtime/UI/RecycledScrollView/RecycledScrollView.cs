using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// <para>대량의 데이터 목록을 최소한의 활성 셀만으로 가상화하여 렌더링하는 고성능 제네릭 재사용 스크롤뷰입니다.</para>
    /// <para>수직/수평 스크롤, 다단 컬럼/행(Lanes), 대각선 시프트(Diagonal Offset), 뷰포트 중앙 진입 감지 이벤트를 완벽 지원합니다.</para>
    /// </summary>
    /// <typeparam name="TData">바인딩할 데이터 모델 타입</typeparam>
    /// <typeparam name="TCell">데이터를 표시할 재사용 셀 컴포넌트 타입 (<see cref="RecycledScrollCell{TData}"/> 상속)</typeparam>
    public abstract class RecycledScrollView<TData, TCell> : MonoBehaviour where TCell : RecycledScrollCell<TData>
    {
        [Header("Scroll References")]
        [Tooltip("연결된 uGUI ScrollRect 컴포넌트 (비어 있을 경우 자동으로 탐색)")]
        [SerializeField] private ScrollRect m_scrollRect;

        [Tooltip("아이템들이 보이는 마스크 뷰포트 RectTransform (비어 있을 경우 ScrollRect.viewport 사용)")]
        [SerializeField] private RectTransform m_viewport;

        [Tooltip("아이템들이 배치되는 Content RectTransform (비어 있을 경우 ScrollRect.content 사용)")]
        [SerializeField] private RectTransform m_content;

        [Tooltip("인스턴스화할 셀 프리팹")]
        [SerializeField] private TCell m_cellPrefab;

        [Header("Layout Settings")]
        [Tooltip("스크롤 방향 (수직 / 수평)")]
        [SerializeField] private RecycledScrollDirection m_direction = RecycledScrollDirection.Vertical;

        [Tooltip("수직 스크롤 시 컬럼(열) 수, 수평 스크롤 시 행(Row) 수")]
        [SerializeField, Min(1)] private int m_columnCount = 1;

        [Tooltip("개별 아이템의 기본 크기(폭, 높이)")]
        [SerializeField] private Vector2 m_itemSize = new Vector2(100f, 100f);

        [Tooltip("아이템 간의 간격(Spacing)")]
        [SerializeField] private Vector2 m_spacing = Vector2.zero;

        [Tooltip("대각선 배치를 위한 단계별 시프트 오프셋입니다.\n수직 스크롤 시 행마다 X 좌표가 시프트되며, 수평 스크롤 시 열마다 Y 좌표가 시프트됩니다.")]
        [SerializeField] private Vector2 m_stepOffset = Vector2.zero;

        [Tooltip("컨텐츠 외곽 패딩(여백)")]
        [SerializeField] private RectOffset m_padding = new RectOffset();

        [Tooltip("뷰포트 외곽 상하/좌우에 미리 버퍼링해 둘 여유 슬롯 행/열 수")]
        [SerializeField, Min(1)] private int m_bufferCount = 2;

        [Header("Center Detection")]
        [Tooltip("스크롤 이동 시 뷰포트 정중앙에 가장 가까운 아이템이 변경될 때 발생하는 이벤트 (인덱스 전달)")]
        [SerializeField] private UnityEvent<int> m_onCenterItemChanged = new();

        // Data & Pooling
        private readonly List<TData> m_dataList = new();
        private readonly Dictionary<int, TCell> m_activeCells = new();
        private readonly Queue<TCell> m_cellPool = new();

        private int m_currentCenteredIndex = -1;
        private Coroutine m_scrollCoroutine;
        private readonly Vector3[] m_viewportCorners = new Vector3[4];

        #region Public Properties
        /// <summary>연결된 ScrollRect 컴포넌트입니다.</summary>
        public ScrollRect ScrollRect => m_scrollRect;

        /// <summary>아이템이 배치되는 Content RectTransform입니다.</summary>
        public RectTransform Content => m_content;

        /// <summary>뷰포트 RectTransform입니다.</summary>
        public RectTransform Viewport => m_viewport;

        /// <summary>현재 바인딩된 전체 데이터 목록입니다.</summary>
        public IReadOnlyList<TData> DataList => m_dataList;

        /// <summary>전체 데이터 수입니다.</summary>
        public int TotalCount => m_dataList.Count;

        /// <summary>현재 뷰포트 정중앙에 위치한 아이템의 인덱스입니다. (없을 경우 -1)</summary>
        public int CurrentCenteredIndex => m_currentCenteredIndex;

        /// <summary>현재 뷰포트 정중앙에 위치한 아이템의 데이터입니다.</summary>
        public TData CurrentCenteredData => (m_currentCenteredIndex >= 0 && m_currentCenteredIndex < m_dataList.Count)
            ? m_dataList[m_currentCenteredIndex]
            : default;

        /// <summary>스크롤 방향입니다.</summary>
        public RecycledScrollDirection Direction
        {
            get => m_direction;
            set
            {
                if (m_direction != value)
                {
                    m_direction = value;
                    SetupScrollRectDirection();
                    Refresh();
                }
            }
        }

        /// <summary>수직 스크롤 시 컬럼(열) 수, 수평 스크롤 시 행(Row) 수입니다.</summary>
        public int ColumnCount
        {
            get => m_columnCount;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (m_columnCount != clamped)
                {
                    m_columnCount = clamped;
                    Refresh();
                }
            }
        }

        /// <summary>개별 아이템 크기입니다.</summary>
        public Vector2 ItemSize
        {
            get => m_itemSize;
            set
            {
                m_itemSize = value;
                Refresh();
            }
        }

        /// <summary>아이템 간 간격입니다.</summary>
        public Vector2 Spacing
        {
            get => m_spacing;
            set
            {
                m_spacing = value;
                Refresh();
            }
        }

        /// <summary>대각선 배치를 위한 행/열 단계별 시프트 오프셋입니다.</summary>
        public Vector2 StepOffset
        {
            get => m_stepOffset;
            set
            {
                m_stepOffset = value;
                Refresh();
            }
        }

        /// <summary>인스턴스화할 셀 프리팹입니다.</summary>
        public TCell CellPrefab
        {
            get => m_cellPrefab;
            set => m_cellPrefab = value;
        }

        /// <summary>
        /// 스크롤뷰 컴포넌트를 코드로 동적 생성하거나 초기화할 때 핵심 참조들을 일괄 설정합니다.
        /// </summary>
        public void SetupReferences(ScrollRect scrollRect, RectTransform viewport, RectTransform content, TCell cellPrefab)
        {
            m_scrollRect = scrollRect;
            m_viewport = viewport;
            m_content = content;
            m_cellPrefab = cellPrefab;
            SetupContentAnchors();
            SetupScrollRectDirection();
        }
        #endregion

        #region Events
        /// <summary>뷰포트 정중앙에 위치한 아이템이 변경되었을 때 발생하는 C# 이벤트입니다. (인덱스, 데이터)</summary>
        public event Action<int, TData> OnItemCentered;

        /// <summary>뷰포트 정중앙에 위치한 아이템이 변경되었을 때 발생하는 UnityEvent입니다. (인덱스)</summary>
        public UnityEvent<int> OnCenterItemChangedEvent => m_onCenterItemChanged;

        /// <summary>셀이 데이터에 바인딩되어 활성화될 때 발생하는 C# 이벤트입니다. (인덱스, 셀 인스턴스)</summary>
        public event Action<int, TCell> OnCellBound;
        #endregion

        protected virtual void Reset()
        {
            m_scrollRect = GetComponent<ScrollRect>();
            if (m_scrollRect != null)
            {
                m_viewport = m_scrollRect.viewport;
                m_content = m_scrollRect.content;
            }
        }

        protected virtual void Awake()
        {
            InitializeReferences();
            SetupContentAnchors();
            SetupScrollRectDirection();
        }

        protected virtual void OnEnable()
        {
            if (m_scrollRect != null)
            {
                m_scrollRect.onValueChanged.AddListener(HandleScrollChanged);
            }
        }

        protected virtual void OnDisable()
        {
            if (m_scrollRect != null)
            {
                m_scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
            }

            if (m_scrollCoroutine != null)
            {
                StopCoroutine(m_scrollCoroutine);
                m_scrollCoroutine = null;
            }
        }

        private void InitializeReferences()
        {
            if (m_scrollRect == null)
                m_scrollRect = GetComponent<ScrollRect>();

            if (m_scrollRect != null)
            {
                if (m_viewport == null)
                    m_viewport = m_scrollRect.viewport != null ? m_scrollRect.viewport : (RectTransform)m_scrollRect.transform;
                if (m_content == null)
                    m_content = m_scrollRect.content;
            }
        }

        private void SetupContentAnchors()
        {
            if (m_content == null) return;

            // uGUI 표준 Top-Left 기준 좌표계로 고정
            m_content.anchorMin = new Vector2(0f, 1f);
            m_content.anchorMax = new Vector2(0f, 1f);
            m_content.pivot = new Vector2(0f, 1f);
        }

        private void SetupScrollRectDirection()
        {
            if (m_scrollRect == null) return;

            m_scrollRect.vertical = (m_direction == RecycledScrollDirection.Vertical);
            m_scrollRect.horizontal = (m_direction == RecycledScrollDirection.Horizontal);
        }

        /// <summary>
        /// 새로운 데이터 컬렉션을 할당하고 스크롤뷰를 처음부터 갱신합니다.
        /// </summary>
        /// <param name="dataCollection">표시할 데이터 목록</param>
        /// <param name="resetScrollPosition">스크롤 위치를 맨 처음으로 초기화할지 여부</param>
        public void SetData(IEnumerable<TData> dataCollection, bool resetScrollPosition = true)
        {
            m_dataList.Clear();
            if (dataCollection != null)
            {
                m_dataList.AddRange(dataCollection);
            }

            if (resetScrollPosition && m_content != null)
            {
                m_content.anchoredPosition = Vector2.zero;
            }

            UpdateContentSize();
            UpdateVisibleCells(forceRebind: true);
            UpdateCenteredItem();
        }

        /// <summary>
        /// 데이터 개수나 레이아웃 설정이 변경되었을 때 화면 배치를 강제 새로고침합니다.
        /// </summary>
        public void Refresh()
        {
            UpdateContentSize();
            UpdateVisibleCells(forceRebind: true);
            UpdateCenteredItem();
        }

        private void HandleScrollChanged(Vector2 scrollPos)
        {
            UpdateVisibleCells(forceRebind: false);
            UpdateCenteredItem();
        }

        #region Item Positioning & Bounds
        /// <summary>
        /// 지정한 인덱스 아이템의 Content 기준 좌상단 anchoredPosition 위치를 계산합니다.
        /// <para>대각선 배치(StepOffset) 및 다단 컬럼/행(ColumnCount) 설정이 반영됩니다. 서브클래스에서 오버라이드하여 곡선형 배치를 구현할 수 있습니다.</para>
        /// </summary>
        /// <param name="index">아이템의 0-based 인덱스</param>
        /// <returns>Content 로컬 좌표 (Top-Left 기준, Y값은 음수)</returns>
        public virtual Vector2 CalculateItemPosition(int index)
        {
            int laneCount = Mathf.Max(1, m_columnCount);
            int majorIndex; // 수직일 경우 행(Row), 수평일 경우 열(Column)
            int minorIndex; // 수직일 경우 열(Column), 수평일 경우 행(Row)

            if (m_direction == RecycledScrollDirection.Vertical)
            {
                majorIndex = index / laneCount;
                minorIndex = index % laneCount;

                float x = m_padding.left + (minorIndex * (m_itemSize.x + m_spacing.x)) + (majorIndex * m_stepOffset.x);
                float y = -(m_padding.top + (majorIndex * (m_itemSize.y + m_spacing.y)) + (minorIndex * m_stepOffset.y));
                return new Vector2(x, y);
            }
            else
            {
                majorIndex = index / laneCount;
                minorIndex = index % laneCount;

                float x = m_padding.left + (majorIndex * (m_itemSize.x + m_spacing.x)) + (minorIndex * m_stepOffset.x);
                float y = -(m_padding.top + (minorIndex * (m_itemSize.y + m_spacing.y)) + (majorIndex * m_stepOffset.y));
                return new Vector2(x, y);
            }
        }

        /// <summary>
        /// 전체 아이템의 대각선 시프트 및 간격을 고려하여 Content의 총 영역 크기(sizeDelta)를 계산하고 갱신합니다.
        /// </summary>
        protected virtual void UpdateContentSize()
        {
            if (m_content == null || m_viewport == null) return;

            int count = TotalCount;
            if (count <= 0)
            {
                m_content.sizeDelta = Vector2.zero;
                return;
            }

            int laneCount = Mathf.Max(1, m_columnCount);
            int totalMajor = Mathf.CeilToInt((float)count / laneCount);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // 외곽 4개 모서리 후보 인덱스를 검사하여 바운딩 박스를 빠르게 계산
            int[] testIndices =
            {
                0,
                Mathf.Min(count - 1, laneCount - 1),
                Mathf.Min(count - 1, (totalMajor - 1) * laneCount),
                count - 1
            };

            foreach (int idx in testIndices)
            {
                Vector2 pos = CalculateItemPosition(idx);
                float left = pos.x;
                float right = pos.x + m_itemSize.x;
                float top = pos.y;
                float bottom = pos.y - m_itemSize.y;

                if (left < minX) minX = left;
                if (right > maxX) maxX = right;
                if (bottom < minY) minY = bottom;
                if (top > maxY) maxY = top;
            }

            float contentWidth = Mathf.Max(m_viewport.rect.width, (maxX - minX) + m_padding.left + m_padding.right);
            float contentHeight = Mathf.Max(m_viewport.rect.height, (maxY - minY) + m_padding.top + m_padding.bottom);

            m_content.sizeDelta = new Vector2(contentWidth, contentHeight);
        }
        #endregion

        #region Virtualization & Recycling
        private void UpdateVisibleCells(bool forceRebind)
        {
            if (m_content == null || m_viewport == null || TotalCount <= 0)
            {
                RecycleAllCells();
                return;
            }

            // 뷰포트의 월드 모서리를 Content 로컬 좌표로 변환
            m_viewport.GetWorldCorners(m_viewportCorners);
            Vector2 viewMinInContent = m_content.InverseTransformPoint(m_viewportCorners[0]); // Bottom-Left
            Vector2 viewMaxInContent = m_content.InverseTransformPoint(m_viewportCorners[2]); // Top-Right

            int laneCount = Mathf.Max(1, m_columnCount);
            int totalMajor = Mathf.CeilToInt((float)TotalCount / laneCount);

            int startMajor;
            int endMajor;

            if (m_direction == RecycledScrollDirection.Vertical)
            {
                float stepY = Mathf.Max(1f, m_itemSize.y + m_spacing.y);
                startMajor = Mathf.FloorToInt((-viewMaxInContent.y - m_padding.top) / stepY);
                endMajor = Mathf.CeilToInt((-viewMinInContent.y - m_padding.top) / stepY);
            }
            else
            {
                float stepX = Mathf.Max(1f, m_itemSize.x + m_spacing.x);
                startMajor = Mathf.FloorToInt((viewMinInContent.x - m_padding.left) / stepX);
                endMajor = Mathf.CeilToInt((viewMaxInContent.x - m_padding.left) / stepX);
            }

            // 대각선 시프트 및 스크롤 버퍼 여유분 적용
            startMajor = Mathf.Clamp(startMajor - m_bufferCount, 0, totalMajor - 1);
            endMajor = Mathf.Clamp(endMajor + m_bufferCount, 0, totalMajor - 1);

            int startIdx = startMajor * laneCount;
            int endIdx = Mathf.Min(TotalCount - 1, (endMajor + 1) * laneCount - 1);

            // 1. 화면 범위를 벗어난 셀 회수
            var toRemove = new List<int>();
            foreach (var kvp in m_activeCells)
            {
                int activeIndex = kvp.Key;
                if (activeIndex < startIdx || activeIndex > endIdx || forceRebind)
                {
                    toRemove.Add(activeIndex);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                RecycleCell(toRemove[i]);
            }

            // 2. 가시 범위 내에 필요한 셀 활성화 및 배치
            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                if (!m_activeCells.ContainsKey(idx))
                {
                    TCell cell = GetOrCreateCell();
                    cell.gameObject.SetActive(true);

                    RectTransform rt = cell.RectTransform;
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.sizeDelta = m_itemSize;
                    rt.anchoredPosition = CalculateItemPosition(idx);

                    cell.OnBind(m_dataList[idx], idx);
                    if (idx == m_currentCenteredIndex)
                    {
                        cell.OnCenterStatusChanged(true);
                    }

                    m_activeCells.Add(idx, cell);
                    OnCellBound?.Invoke(idx, cell);
                }
            }
        }

        private TCell GetOrCreateCell()
        {
            if (m_cellPool.Count > 0)
            {
                return m_cellPool.Dequeue();
            }

            if (m_cellPrefab == null)
            {
                Debug.LogError($"[{GetType().Name}] CellPrefab이 할당되지 않았습니다.", this);
                return null;
            }

            TCell newCell = Instantiate(m_cellPrefab, m_content);
            return newCell;
        }

        private void RecycleCell(int index)
        {
            if (m_activeCells.TryGetValue(index, out TCell cell))
            {
                cell.OnUnbind();
                cell.gameObject.SetActive(false);
                m_cellPool.Enqueue(cell);
                m_activeCells.Remove(index);
            }
        }

        private void RecycleAllCells()
        {
            foreach (var kvp in m_activeCells)
            {
                kvp.Value.OnUnbind();
                kvp.Value.gameObject.SetActive(false);
                m_cellPool.Enqueue(kvp.Value);
            }
            m_activeCells.Clear();
        }
        #endregion

        #region Center Detection
        private void UpdateCenteredItem()
        {
            if (m_viewport == null || m_content == null || m_activeCells.Count <= 0)
                return;

            // 뷰포트의 정중앙 월드 좌표를 Content 로컬 좌표계로 변환
            Vector3 viewportCenterWorld = m_viewport.TransformPoint(m_viewport.rect.center);
            Vector2 centerInContent = m_content.InverseTransformPoint(viewportCenterWorld);

            int closestIndex = -1;
            float minDistanceSqr = float.MaxValue;

            foreach (var kvp in m_activeCells)
            {
                Vector2 itemPos = kvp.Value.RectTransform.anchoredPosition;
                // 셀의 중심점 좌표 (Top-Left pivot 기준)
                Vector2 itemCenter = new Vector2(itemPos.x + (m_itemSize.x * 0.5f), itemPos.y - (m_itemSize.y * 0.5f));

                float distSqr = (itemCenter - centerInContent).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestIndex = kvp.Key;
                }
            }

            if (closestIndex != m_currentCenteredIndex && closestIndex >= 0)
            {
                // 이전 중앙 셀 상태 변경
                if (m_activeCells.TryGetValue(m_currentCenteredIndex, out TCell prevCell))
                {
                    prevCell.OnCenterStatusChanged(false);
                }

                m_currentCenteredIndex = closestIndex;

                // 신규 중앙 셀 상태 변경
                if (m_activeCells.TryGetValue(m_currentCenteredIndex, out TCell nextCell))
                {
                    nextCell.OnCenterStatusChanged(true);
                }

                TData data = (m_currentCenteredIndex < m_dataList.Count) ? m_dataList[m_currentCenteredIndex] : default;
                OnItemCentered?.Invoke(m_currentCenteredIndex, data);
                m_onCenterItemChanged?.Invoke(m_currentCenteredIndex);
            }
        }
        #endregion

        #region Navigation & Scrolling Control
        /// <summary>
        /// 지정한 인덱스의 아이템이 뷰포트 정중앙에 오도록 부드럽게 스크롤합니다.
        /// </summary>
        /// <param name="index">스크롤할 아이템 인덱스</param>
        /// <param name="duration">이동 소요 시간 (0 이하일 경우 즉시 스냅)</param>
        /// <param name="easeType">적용할 이징 곡선</param>
        public void ScrollToItem(int index, float duration = 0.25f, EaseType easeType = EaseType.OutQuad)
        {
            if (m_content == null || m_viewport == null || TotalCount <= 0)
                return;

            index = Mathf.Clamp(index, 0, TotalCount - 1);

            Vector2 itemPos = CalculateItemPosition(index);
            Vector2 itemCenterInContent = new Vector2(itemPos.x + (m_itemSize.x * 0.5f), itemPos.y - (m_itemSize.y * 0.5f));
            Vector2 viewportCenterInViewport = m_viewport.rect.center;

            // itemCenter가 viewportCenter에 오도록 하기 위한 목표 content.anchoredPosition 계산
            Vector2 targetContentPos = new Vector2(
                viewportCenterInViewport.x - itemCenterInContent.x,
                viewportCenterInViewport.y - itemCenterInContent.y
            );

            // 스크롤 방향 외 축은 기존 위치 유지
            if (m_direction == RecycledScrollDirection.Vertical)
            {
                targetContentPos.x = m_content.anchoredPosition.x;
            }
            else
            {
                targetContentPos.y = m_content.anchoredPosition.y;
            }

            // 바운더리 클램핑
            targetContentPos = ClampContentPosition(targetContentPos);

            if (m_scrollCoroutine != null)
            {
                StopCoroutine(m_scrollCoroutine);
                m_scrollCoroutine = null;
            }

            if (duration <= 0f || !gameObject.activeInHierarchy)
            {
                m_content.anchoredPosition = targetContentPos;
                HandleScrollChanged(Vector2.zero);
            }
            else
            {
                m_scrollCoroutine = StartCoroutine(ScrollRoutine(m_content.anchoredPosition, targetContentPos, duration, easeType));
            }
        }

        private Vector2 ClampContentPosition(Vector2 pos)
        {
            float minX = -(m_content.rect.width - m_viewport.rect.width);
            float maxX = 0f;
            float minY = 0f;
            float maxY = Mathf.Max(0f, m_content.rect.height - m_viewport.rect.height);

            if (m_content.rect.width <= m_viewport.rect.width) minX = maxX = 0f;
            if (m_content.rect.height <= m_viewport.rect.height) minY = maxY = 0f;

            float clampedX = Mathf.Clamp(pos.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
            float clampedY = Mathf.Clamp(pos.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

            return new Vector2(clampedX, clampedY);
        }

        private IEnumerator ScrollRoutine(Vector2 startPos, Vector2 targetPos, float duration, EaseType easeType)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = TweenEasing.Evaluate(easeType, t);

                m_content.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            m_content.anchoredPosition = targetPos;
            m_scrollCoroutine = null;
        }
        #endregion
    }
}
