using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sizzle.Toolkits.UI;

namespace Sizzle.Toolkits.Demo.UI
{
    /// <summary>
    /// 데모 씬에서 사용할 데이터 모델입니다.
    /// </summary>
    [Serializable]
    public class DemoItemData
    {
        public int Id;
        public string Title;
        public string Subtitle;
        public Color BgColor;

        public DemoItemData(int id, string title, string subtitle, Color color)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            BgColor = color;
        }
    }

    /// <summary>
    /// <see cref="DemoItemData"/>를 렌더링하고 중앙 진입 시 시각적 하이라이트를 표시하는 셀 뷰입니다.
    /// </summary>
    public class DemoScrollCell : RecycledScrollCell<DemoItemData>
    {
        [SerializeField] private Text m_titleText;
        [SerializeField] private Text m_subText;
        [SerializeField] private Image m_background;
        [SerializeField] private Image m_centerBorder;

        public void BindComponents(Text title, Text sub, Image bg, Image border)
        {
            m_titleText = title;
            m_subText = sub;
            m_background = bg;
            m_centerBorder = border;
        }

        public override void OnBind(DemoItemData data, int index)
        {
            base.OnBind(data, index);

            if (m_titleText != null)
                m_titleText.text = $"#{index} {data.Title}";

            if (m_subText != null)
                m_subText.text = data.Subtitle;

            if (m_background != null)
                m_background.color = data.BgColor;

            UpdateCenterVisual(IsCentered);
        }

        public override void OnCenterStatusChanged(bool isCentered)
        {
            base.OnCenterStatusChanged(isCentered);
            UpdateCenterVisual(isCentered);
        }

        private void UpdateCenterVisual(bool isCentered)
        {
            if (m_centerBorder != null)
            {
                m_centerBorder.gameObject.SetActive(isCentered);
            }

            transform.localScale = isCentered ? Vector3.one * 1.08f : Vector3.one;
        }
    }

    /// <summary>
    /// 인스펙터 및 씬에서 사용할 수 있는 구체(Concrete) 재사용 스크롤뷰 컴포넌트입니다.
    /// </summary>
    public class DemoScrollView : RecycledScrollView<DemoItemData, DemoScrollCell>
    {
    }

    /// <summary>
    /// <see cref="RecycledScrollView{TData, TCell}"/>의 5가지 핵심 옵션 및 작동 상태를 한눈에 검증하는 데모 씬 컨트롤러입니다.
    /// <list type="bullet">
    /// <item>구역 1: 수직 기본 선형 리스트 (Vertical 1-Column)</item>
    /// <item>구역 2: 수직 대각선 시프트 리스트 (Vertical Diagonal StepX=35 - 페르소나 스타일)</item>
    /// <item>구역 3: 수직 다단 그리드 리스트 (Vertical 3-Columns Grid)</item>
    /// <item>구역 4: 수평 기본 가로 리스트 (Horizontal 1-Row)</item>
    /// <item>구역 5: 수평 계단식 대각선 그리드 (Horizontal 2-Rows Diagonal StepY=-18)</item>
    /// </list>
    /// </summary>
    public class RecycledScrollViewDemoScene : MonoBehaviour
    {
        private Font m_font;
        private DemoScrollCell m_cellPrefab;

        private void Awake()
        {
            m_font = GetDefaultFont();
            EnsureEventSystem();
            EnsureCamera();
            BuildDemoUI();
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                string[] fontNames = { "Arial", "Segoe UI", "Tahoma", "Helvetica" };
                f = Font.CreateDynamicFontFromOSFont(fontNames, 14);
            }
            return f;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
                camGo.transform.position = new Vector3(0, 0, -10);
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            }
        }

        private void BuildDemoUI()
        {
            // Canvas 생성
            var canvasGo = new GameObject("DemoCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 공용 셀 템플릿 프리팹 생성 (비활성화 상태 보관)
            m_cellPrefab = CreateCellTemplate(canvasGo.transform);

            // 헤더 바 (상단 타이틀)
            CreateHeaderBar(canvasGo.transform);

            // 5개 구역 컨테이너 배치 (가로 1920 기준 5분할 카드)
            float startX = -760f;
            float stepX = 380f;
            float cardWidth = 350f;
            float cardHeight = 880f;
            float cardY = -50f;

            // 1. 수직 1열 기본 선형 리스트
            CreateSection(canvasGo.transform,
                index: 1,
                title: "1. Vertical Linear\n[1-Column, Normal]",
                pos: new Vector2(startX + (stepX * 0), cardY),
                size: new Vector2(cardWidth, cardHeight),
                direction: RecycledScrollDirection.Vertical,
                columns: 1,
                itemSize: new Vector2(310f, 65f),
                spacing: new Vector2(0f, 10f),
                stepOffset: Vector2.zero,
                itemCount: 60,
                buttonConfigs: new[]
                {
                    ("Top (#0)", 0),
                    ("Mid (#30)", 30),
                    ("Next (+1)", -101),
                    ("Prev (-1)", -102),
                    ("Bottom (#59)", 59),
                    ("Random", -103)
                });

            // 2. 수직 대각선 시프트 리스트 (페르소나/격투 스타일)
            CreateSection(canvasGo.transform,
                index: 2,
                title: "2. Vertical Diagonal\n[StepX = 30px Shift]",
                pos: new Vector2(startX + (stepX * 1), cardY),
                size: new Vector2(cardWidth, cardHeight),
                direction: RecycledScrollDirection.Vertical,
                columns: 1,
                itemSize: new Vector2(230f, 60f),
                spacing: new Vector2(0f, 10f),
                stepOffset: new Vector2(30f, 0f), // 대각선 시프트
                itemCount: 50,
                buttonConfigs: new[]
                {
                    ("Top (#0)", 0),
                    ("Step (#15)", 15),
                    ("Step (#30)", 30),
                    ("Next (+1)", -101),
                    ("Prev (-1)", -102),
                    ("Bottom (#49)", 49)
                });

            // 3. 수직 3단 그리드
            CreateSection(canvasGo.transform,
                index: 3,
                title: "3. Vertical Grid\n[3-Columns Matrix]",
                pos: new Vector2(startX + (stepX * 2), cardY),
                size: new Vector2(cardWidth, cardHeight),
                direction: RecycledScrollDirection.Vertical,
                columns: 3,
                itemSize: new Vector2(96f, 85f),
                spacing: new Vector2(10f, 10f),
                stepOffset: Vector2.zero,
                itemCount: 60,
                buttonConfigs: new[]
                {
                    ("Row 1 (#0)", 0),
                    ("Row 5 (#12)", 12),
                    ("Row 10 (#27)", 27),
                    ("Next (+3)", -104),
                    ("Last (#59)", 59),
                    ("Random", -103)
                });

            // 4. 수평 기본 1행 리스트
            CreateSection(canvasGo.transform,
                index: 4,
                title: "4. Horizontal Single\n[1-Row Carousel]",
                pos: new Vector2(startX + (stepX * 3), cardY),
                size: new Vector2(cardWidth, cardHeight),
                direction: RecycledScrollDirection.Horizontal,
                columns: 1,
                itemSize: new Vector2(140f, 220f),
                spacing: new Vector2(14f, 0f),
                stepOffset: Vector2.zero,
                itemCount: 40,
                buttonConfigs: new[]
                {
                    ("Start (#0)", 0),
                    ("Mid (#20)", 20),
                    ("Next (+1)", -101),
                    ("Prev (-1)", -102),
                    ("End (#39)", 39),
                    ("Random", -103)
                });

            // 5. 수평 계단식 2행 대각선 그리드
            CreateSection(canvasGo.transform,
                index: 5,
                title: "5. Horizontal Diagonal\n[2-Rows, StepY = -18px]",
                pos: new Vector2(startX + (stepX * 4), cardY),
                size: new Vector2(cardWidth, cardHeight),
                direction: RecycledScrollDirection.Horizontal,
                columns: 2,
                itemSize: new Vector2(120f, 95f),
                spacing: new Vector2(14f, 14f),
                stepOffset: new Vector2(0f, -18f), // 열마다 아래로 시프트되는 대각선
                itemCount: 40,
                buttonConfigs: new[]
                {
                    ("Start (#0)", 0),
                    ("Col 5 (#10)", 10),
                    ("Col 10 (#20)", 20),
                    ("Next (+2)", -105),
                    ("End (#39)", 39),
                    ("Random", -103)
                });
        }

        private void CreateHeaderBar(Transform parent)
        {
            var headerGo = new GameObject("HeaderBar");
            headerGo.transform.SetParent(parent, false);
            var rt = headerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 80f);

            var bg = headerGo.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);

            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(headerGo.transform, false);
            var text = titleGo.AddComponent<Text>();
            text.font = m_font;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "<b>Sizzle.Toolkits - RecycledScrollView Feature Showcase</b>\n<size=14><color=#90CAF9>Vertical · Diagonal (StepOffset) · Multi-Column · Horizontal · Center Detection</color></size>";

            var textRt = titleGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
        }

        private void CreateSection(Transform parent, int index, string title, Vector2 pos, Vector2 size,
            RecycledScrollDirection direction, int columns, Vector2 itemSize, Vector2 spacing, Vector2 stepOffset,
            int itemCount, (string label, int actionCode)[] buttonConfigs)
        {
            // 1. 카드 패널
            var cardGo = new GameObject($"Section_{index}");
            cardGo.transform.SetParent(parent, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = pos;
            cardRt.sizeDelta = size;

            var cardBg = cardGo.AddComponent<Image>();
            cardBg.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);

            // 2. 타이틀 헤더
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = m_font;
            titleText.fontSize = 15;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            titleText.text = title;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(size.x - 16f, 44f);

            // 3. 중앙 감지 상태 표시 라벨 (Center Indicator)
            var statusGo = new GameObject("CenterIndicator");
            statusGo.transform.SetParent(cardGo.transform, false);
            var statusBg = statusGo.AddComponent<Image>();
            statusBg.color = new Color(0.08f, 0.25f, 0.20f, 0.9f);
            var statusText = CreateLabel(statusGo.transform, "Centered: <b>#0</b>", 13, FontStyle.Normal, TextAnchor.MiddleCenter, Color.green);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0f, 1f);
            statusRt.anchorMax = new Vector2(1f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -56f);
            statusRt.sizeDelta = new Vector2(size.x - 20f, 32f);

            // 4. 스크롤 뷰 컨테이너 영역
            float scrollAreaHeight = size.y - 56f - 32f - 145f;
            var scrollAreaGo = new GameObject("ScrollViewArea");
            scrollAreaGo.transform.SetParent(cardGo.transform, false);
            var scrollAreaRt = scrollAreaGo.AddComponent<RectTransform>();
            scrollAreaRt.anchorMin = new Vector2(0f, 1f);
            scrollAreaRt.anchorMax = new Vector2(1f, 1f);
            scrollAreaRt.pivot = new Vector2(0.5f, 1f);
            scrollAreaRt.anchoredPosition = new Vector2(0f, -94f);
            scrollAreaRt.sizeDelta = new Vector2(size.x - 16f, scrollAreaHeight);

            var areaBg = scrollAreaGo.AddComponent<Image>();
            areaBg.color = new Color(0.10f, 0.11f, 0.13f, 1f);

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollAreaGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0f, 1f);
            viewportRt.sizeDelta = Vector2.zero;
            var mask = viewportGo.AddComponent<RectMask2D>();

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            // ScrollRect & DemoScrollView
            var scrollRect = scrollAreaGo.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = (direction == RecycledScrollDirection.Horizontal);
            scrollRect.vertical = (direction == RecycledScrollDirection.Vertical);
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;

            var scrollView = scrollAreaGo.AddComponent<DemoScrollView>();
            scrollView.SetupReferences(scrollRect, viewportRt, contentRt, m_cellPrefab);
            scrollView.Direction = direction;
            scrollView.ColumnCount = columns;
            scrollView.ItemSize = itemSize;
            scrollView.Spacing = spacing;
            scrollView.StepOffset = stepOffset;

            // 데이터 채우기
            var dummyData = GenerateDummyData(itemCount, index);
            scrollView.SetData(dummyData);

            // 중앙 감지 이벤트 바인딩
            scrollView.OnItemCentered += (centerIdx, data) =>
            {
                if (statusText != null && data != null)
                {
                    statusText.text = $"Centered: <b><color=yellow>#{centerIdx}</color></b> ({data.Title})";
                }
            };

            // 5. 하단 버튼 바 (스크롤 커서 이동 컨트롤러)
            var buttonBarGo = new GameObject("ButtonBar");
            buttonBarGo.transform.SetParent(cardGo.transform, false);
            var buttonBarRt = buttonBarGo.AddComponent<RectTransform>();
            buttonBarRt.anchorMin = new Vector2(0f, 0f);
            buttonBarRt.anchorMax = new Vector2(1f, 0f);
            buttonBarRt.pivot = new Vector2(0.5f, 0f);
            buttonBarRt.anchoredPosition = new Vector2(0f, 8f);
            buttonBarRt.sizeDelta = new Vector2(size.x - 16f, 125f);

            // 6개 버튼 3x2 그리드 배치
            float btnW = (size.x - 24f) / 3f;
            float btnH = 52f;
            for (int i = 0; i < buttonConfigs.Length; i++)
            {
                var (btnLabel, actionCode) = buttonConfigs[i];
                int col = i % 3;
                int row = i / 3;
                Vector2 btnPos = new Vector2(col * (btnW + 4f) - (size.x - 16f) * 0.5f + (btnW * 0.5f), -(row * (btnH + 6f)) - 10f);

                CreateActionButton(buttonBarGo.transform, btnLabel, btnPos, new Vector2(btnW - 2f, btnH), () =>
                {
                    int targetIndex;
                    int cur = scrollView.CurrentCenteredIndex;

                    if (actionCode >= 0)
                    {
                        targetIndex = actionCode;
                    }
                    else if (actionCode == -101) // Next +1
                    {
                        targetIndex = Mathf.Clamp(cur + 1, 0, itemCount - 1);
                    }
                    else if (actionCode == -102) // Prev -1
                    {
                        targetIndex = Mathf.Clamp(cur - 1, 0, itemCount - 1);
                    }
                    else if (actionCode == -103) // Random
                    {
                        targetIndex = UnityEngine.Random.Range(0, itemCount);
                    }
                    else if (actionCode == -104) // Next +3 (Grid)
                    {
                        targetIndex = Mathf.Clamp(cur + 3, 0, itemCount - 1);
                    }
                    else if (actionCode == -105) // Next +2
                    {
                        targetIndex = Mathf.Clamp(cur + 2, 0, itemCount - 1);
                    }
                    else
                    {
                        targetIndex = 0;
                    }

                    // 스크롤뷰 부드러운 센터링 이동 실행
                    scrollView.ScrollToItem(targetIndex, duration: 0.28f);
                });
            }
        }

        private DemoScrollCell CreateCellTemplate(Transform parent)
        {
            var cellGo = new GameObject("CellTemplate");
            cellGo.transform.SetParent(parent, false);
            var cellRt = cellGo.AddComponent<RectTransform>();
            cellRt.sizeDelta = new Vector2(100f, 100f);
            cellRt.pivot = new Vector2(0f, 1f);

            var bg = cellGo.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.28f, 0.35f);

            // 중앙 하이라이트 테두리
            var borderGo = new GameObject("HighlightBorder");
            borderGo.transform.SetParent(cellGo.transform, false);
            var borderRt = borderGo.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(6f, 6f);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.84f, 0f, 0.9f);
            borderGo.SetActive(false);

            // 타이틀 텍스트
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cellGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = m_font;
            titleText.fontSize = 13;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.4f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.sizeDelta = Vector2.zero;

            // 서브 텍스트
            var subGo = new GameObject("Sub");
            subGo.transform.SetParent(cellGo.transform, false);
            var subText = subGo.AddComponent<Text>();
            subText.font = m_font;
            subText.fontSize = 10;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.8f, 0.8f, 0.85f);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.4f);
            subRt.sizeDelta = Vector2.zero;

            var cell = cellGo.AddComponent<DemoScrollCell>();
            cell.BindComponents(titleText, subText, bg, borderImg);

            cellGo.SetActive(false);
            return cell;
        }

        private Text CreateLabel(Transform parent, string text, int size, FontStyle style, TextAnchor align, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = m_font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.text = text;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return txt;
        }

        private void CreateActionButton(Transform parent, string label, Vector2 pos, Vector2 size, Action onClick)
        {
            var btnGo = new GameObject($"Btn_{label}");
            btnGo.transform.SetParent(parent, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 1f);
            btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 1f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = size;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.24f, 0.42f, 0.65f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.58f, 0.88f);
            colors.pressedColor = new Color(0.15f, 0.30f, 0.50f);
            btn.colors = colors;

            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = m_font;
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
        }

        private List<DemoItemData> GenerateDummyData(int count, int sectionId)
        {
            var list = new List<DemoItemData>(count);
            for (int i = 0; i < count; i++)
            {
                // 섹션별 고유 테마 색상 그라데이션
                float hue = ((sectionId * 0.2f) + (i * 0.015f)) % 1f;
                Color col = Color.HSVToRGB(hue, 0.55f, 0.45f);
                string title = $"Item {i}";
                string desc = $"Sec {sectionId} · Lv.{i + 1}";
                list.Add(new DemoItemData(i, title, desc, col));
            }
            return list;
        }
    }
}
