using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 재사용 스크롤뷰 테스트를 위한 샘플 데이터 모델입니다.
    /// </summary>
    [Serializable]
    public class SampleItemData
    {
        public int Id;
        public string Title;
        public Color Color;

        public SampleItemData(int id, string title, Color color)
        {
            Id = id;
            Title = title;
            Color = color;
        }
    }

    /// <summary>
    /// <see cref="SampleItemData"/>를 렌더링하는 샘플 재사용 셀 컴포넌트입니다.
    /// </summary>
    public class SampleItemCell : RecycledScrollCell<SampleItemData>
    {
        [SerializeField] private Text m_titleText;
        [SerializeField] private Image m_background;
        [SerializeField] private Image m_centerHighlightBorder;

        public override void OnBind(SampleItemData data, int index)
        {
            base.OnBind(data, index);

            if (m_titleText != null)
                m_titleText.text = $"#{index} {data.Title}";

            if (m_background != null)
                m_background.color = data.Color;
        }

        public override void OnCenterStatusChanged(bool isCentered)
        {
            base.OnCenterStatusChanged(isCentered);

            // 중앙에 들어왔을 때 테두리 하이라이트 활성화 및 스케일 강조
            if (m_centerHighlightBorder != null)
            {
                m_centerHighlightBorder.gameObject.SetActive(isCentered);
            }

            transform.localScale = isCentered ? Vector3.one * 1.1f : Vector3.one;
        }
    }

    /// <summary>
    /// 인스펙터에 직접 부착할 수 있는 구체(Concrete) 재사용 스크롤뷰 컴포넌트입니다.
    /// 대각선 배치 및 중앙 진입 감지를 지원합니다.
    /// </summary>
    public class SampleRecycledScrollView : RecycledScrollView<SampleItemData, SampleItemCell>
    {
        private void Start()
        {
            // 테스트용 대량 데이터 생성 (1,000개)
            var sampleList = new SampleItemData[1000];
            for (int i = 0; i < sampleList.Length; i++)
            {
                Color randomColor = Color.HSVToRGB((i * 0.05f) % 1f, 0.6f, 0.8f);
                sampleList[i] = new SampleItemData(i, $"Item {i}", randomColor);
            }

            // 스크롤뷰에 데이터 할당
            SetData(sampleList);

            // 중앙 진입 이벤트 구독
            OnItemCentered += (index, data) =>
            {
                Debug.Log($"[Centered] Index: {index}, Title: {data?.Title}");
            };
        }
    }
}
