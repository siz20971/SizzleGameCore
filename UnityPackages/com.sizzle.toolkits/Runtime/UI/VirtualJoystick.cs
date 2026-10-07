using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 모바일 터치 및 온스크린 조작을 위한 2D 가상 조이스틱 컴포넌트입니다.
    /// 고정형(Fixed) 및 터치한 위치에 조이스틱이 동적으로 나타나는 플로팅(Floating) 모드를 모두 지원합니다.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Rects")]
        [Tooltip("조이스틱의 배경 플레이트")]
        [SerializeField] private RectTransform m_background;

        [Tooltip("움직이는 조이스틱 핸들(놉)")]
        [SerializeField] private RectTransform m_handle;

        [Header("Settings")]
        [Tooltip("핸들의 최대 이동 반경(픽셀)")]
        [SerializeField] private float m_handleRange = 80f;

        [Tooltip("데드존 반경 (0 ~ 1)")]
        [SerializeField] private float m_deadZone = 0.1f;

        [Tooltip("터치한 위치로 배경이 이동하는 플로팅 조이스틱 모드 여부")]
        [SerializeField] private bool m_isFloating = false;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector2> m_onInputChanged;

        private Vector2 m_inputVector = Vector2.zero;
        private Vector2 m_originalBackgroundPos;
        private Canvas m_canvas;

        /// <summary>정규화된 2D 입력 벡터 (-1 ~ 1)입니다.</summary>
        public Vector2 InputVector => m_inputVector;

        /// <summary>수평 입력값 (-1 ~ 1)입니다.</summary>
        public float Horizontal => m_inputVector.x;

        /// <summary>수직 입력값 (-1 ~ 1)입니다.</summary>
        public float Vertical => m_inputVector.y;

        /// <summary>입력 벡터 변경 시 호출되는 C# 이벤트입니다.</summary>
        public event Action<Vector2> OnInputChanged;

        private void Awake()
        {
            if (m_background == null) m_background = GetComponent<RectTransform>();
            m_canvas = GetComponentInParent<Canvas>();
            if (m_background != null)
            {
                m_originalBackgroundPos = m_background.anchoredPosition;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_isFloating && m_background != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_background.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint);

                m_background.anchoredPosition = localPoint;
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_background == null || m_handle == null) return;

            Camera cam = (m_canvas != null && m_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                ? null
                : eventData.pressEventCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_background, eventData.position, cam, out Vector2 position))
            {
                position = Vector2.ClampMagnitude(position, m_handleRange);
                m_handle.anchoredPosition = position;

                // 0 ~ 1 정규화 및 데드존 처리
                Vector2 rawNormalized = position / m_handleRange;
                if (rawNormalized.magnitude < m_deadZone)
                {
                    m_inputVector = Vector2.zero;
                }
                else
                {
                    m_inputVector = rawNormalized;
                }

                m_onInputChanged?.Invoke(m_inputVector);
                OnInputChanged?.Invoke(m_inputVector);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_inputVector = Vector2.zero;

            if (m_handle != null)
            {
                m_handle.anchoredPosition = Vector2.zero;
            }

            if (m_isFloating && m_background != null)
            {
                m_background.anchoredPosition = m_originalBackgroundPos;
            }

            m_onInputChanged?.Invoke(Vector2.zero);
            OnInputChanged?.Invoke(Vector2.zero);
        }
    }
}
