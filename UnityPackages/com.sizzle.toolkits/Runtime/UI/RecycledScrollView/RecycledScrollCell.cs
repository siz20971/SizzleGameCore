using UnityEngine;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// <see cref="RecycledScrollView{TData, TCell}"/>에서 사용되는 재사용 셀 뷰의 제네릭 추상 기반 클래스입니다.
    /// 스크롤뷰 아이템 UI 프리팹에 상속하여 사용합니다.
    /// </summary>
    /// <typeparam name="TData">바인딩할 데이터 모델 타입</typeparam>
    [RequireComponent(typeof(RectTransform))]
    public abstract class RecycledScrollCell<TData> : MonoBehaviour
    {
        private RectTransform m_rectTransform;

        /// <summary>셀의 RectTransform 컴포넌트입니다.</summary>
        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = GetComponent<RectTransform>();
                return m_rectTransform;
            }
        }

        /// <summary>현재 셀에 바인딩된 데이터 목록의 0-based 인덱스입니다. (바인딩 해제 시 -1)</summary>
        public int Index { get; private set; } = -1;

        /// <summary>현재 셀에 바인딩된 데이터 인스턴스입니다.</summary>
        public TData Data { get; private set; }

        /// <summary>현재 셀이 스크롤 뷰포트의 정중앙에 위치하고 있는지 여부입니다.</summary>
        public bool IsCentered { get; private set; }

        protected virtual void Awake()
        {
            if (m_rectTransform == null)
                m_rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 데이터가 셀에 할당되어 화면에 표시될 때 호출됩니다.
        /// 서브클래스에서 텍스트, 아이콘, 버튼 상태 등의 뷰 갱신을 구현합니다.
        /// </summary>
        /// <param name="data">바인딩할 데이터</param>
        /// <param name="index">데이터의 인덱스</param>
        public virtual void OnBind(TData data, int index)
        {
            Data = data;
            Index = index;
        }

        /// <summary>
        /// 셀이 화면 밖으로 벗어나 풀로 회수될 때 호출됩니다.
        /// 리소스 해제, 이벤트 리스너 해제 등을 구현합니다.
        /// </summary>
        public virtual void OnUnbind()
        {
            Data = default;
            Index = -1;
            IsCentered = false;
        }

        /// <summary>
        /// 셀이 스크롤 뷰포트의 정중앙에 진입하거나 벗어날 때 호출되는 가상 메서드입니다.
        /// 하이라이트 테두리 활성화, 스케일 확대(Punch), 선택 사운드 재생 등에 활용합니다.
        /// </summary>
        /// <param name="isCentered">중앙에 위치하게 되었는지 여부</param>
        public virtual void OnCenterStatusChanged(bool isCentered)
        {
            IsCentered = isCentered;
        }
    }
}
