using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// <see cref="Selectable"/>을 상속받아 마우스, 터치뿐만 아니라 키보드 내비게이션 및 게임패드(Submit 입력)에서도
    /// 온전히 동작하는 uGUI 표준 호환 롱프레스(Hold) 버튼 컴포넌트입니다.
    /// 버튼을 누르고 있는 동안 게이지가 차오르고, 지속 시간을 채우면 발동됩니다.
    /// </summary>
    public class LongPressButton : Selectable, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler, IUpdateSelectedHandler
    {
        [Header("Hold Settings")]
        [Tooltip("롱프레스 완료에 필요한 총 누름 지속 시간(초)")]
        [SerializeField] private float m_requiredHoldTime = 1.0f;

        [Tooltip("손을 뗐을 때 게이지가 감쇠되는 속도 (0이면 즉시 리셋)")]
        [SerializeField] private float m_fadeSpeed = 3.0f;

        [Header("UI Feedback")]
        [Tooltip("누름 진행률(0~1)을 시각화할 Image (선택 사항, Image Type이 Filled여야 함)")]
        [SerializeField] private Image m_progressImage;

        [Header("Events")]
        [Tooltip("홀드가 성공적으로 완료되었을 때 실행되는 이벤트")]
        [SerializeField] private UnityEvent m_onHoldComplete = new();

        [Tooltip("홀드 진행률(0~1)이 변경될 때마다 호출되는 이벤트")]
        [SerializeField] private UnityEvent<float> m_onHoldProgress = new();

        private float m_currentHoldTime;
        private bool m_isHolding;
        private bool m_isCompleted;
        private bool m_isSubmitPressed;

        /// <summary>0부터 1까지의 정규화된 홀드 진행률입니다.</summary>
        public float Progress => Mathf.Clamp01(m_currentHoldTime / Mathf.Max(0.01f, m_requiredHoldTime));

        /// <summary>필요한 총 홀드 시간(초)입니다.</summary>
        public float RequiredHoldTime
        {
            get => m_requiredHoldTime;
            set => m_requiredHoldTime = Mathf.Max(0.01f, value);
        }

        /// <summary>홀드가 성공적으로 완료되었을 때 발생하는 C# 이벤트입니다.</summary>
        public event Action OnHoldCompleted;

        protected override void OnDisable()
        {
            base.OnDisable();
            ResetHold();
        }

        private void Update()
        {
            if (!IsInteractable())
            {
                if (m_isHolding || m_currentHoldTime > 0f)
                {
                    ResetHold();
                }
                return;
            }

            // 키보드/게임패드에서 Submit 키(Enter, Space, Gamepad A)를 뗐는지 감지
            if (m_isSubmitPressed)
            {
                if (!UnityEngine.Input.GetButton("Submit") && !UnityEngine.Input.GetKey(KeyCode.Space) && !UnityEngine.Input.GetKey(KeyCode.Return))
                {
                    m_isSubmitPressed = false;
                    m_isHolding = false;
                }
            }

            if (m_isHolding && !m_isCompleted)
            {
                m_currentHoldTime += Time.unscaledDeltaTime;
                UpdateFeedback();

                if (m_currentHoldTime >= m_requiredHoldTime)
                {
                    m_isCompleted = true;
                    m_onHoldComplete?.Invoke();
                    OnHoldCompleted?.Invoke();
                }
            }
            else if (!m_isHolding && m_currentHoldTime > 0f)
            {
                if (m_fadeSpeed <= 0f)
                {
                    m_currentHoldTime = 0f;
                }
                else
                {
                    m_currentHoldTime = Mathf.Max(0f, m_currentHoldTime - Time.unscaledDeltaTime * m_fadeSpeed);
                }
                UpdateFeedback();
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsInteractable()) return;

            m_isHolding = true;
            m_isCompleted = false;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            m_isHolding = false;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            m_isHolding = false;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable()) return;

            // Submit 키(Enter/Space/Gamepad A)가 눌린 시점
            m_isSubmitPressed = true;
            m_isHolding = true;
            m_isCompleted = false;
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            if (!IsInteractable()) return;

            // 선택된 상태에서 Submit 키 입력 검사
            if (UnityEngine.Input.GetButtonDown("Submit") || UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                m_isSubmitPressed = true;
                m_isHolding = true;
                m_isCompleted = false;
            }
        }

        private void UpdateFeedback()
        {
            float p = Progress;
            if (m_progressImage != null)
            {
                m_progressImage.fillAmount = p;
            }
            m_onHoldProgress?.Invoke(p);
        }

        /// <summary>
        /// 홀드 상태 및 게이지 진행률을 강제로 리셋합니다.
        /// </summary>
        public void ResetHold()
        {
            m_isHolding = false;
            m_isSubmitPressed = false;
            m_isCompleted = false;
            m_currentHoldTime = 0f;
            UpdateFeedback();
        }
    }
}
