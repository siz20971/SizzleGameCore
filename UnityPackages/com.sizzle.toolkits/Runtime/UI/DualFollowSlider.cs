using UnityEngine;
using UnityEngine.UI;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 메인 슬라이더와 지연 추적(Follow) 서브 슬라이더를 연동하여 체력 감소 잔상 연출 등을 제공하는 듀얼 게이지 슬라이더 UI 컴포넌트입니다.
    /// </summary>
    public class DualFollowSlider : MonoBehaviour
    {
        [Header("Slider Gauge")]
        [SerializeField] private Slider m_mainSlider;
        [SerializeField] private Slider m_subSlider;

        [Header("Follow Settings")]
        [SerializeField] private float m_followDelay = 0.5f;
        [SerializeField] private float m_followSpeed = 0.5f;
        [SerializeField] private bool m_followIfDecreaseOnly = true;

        private float m_remainDelay = 0f;

        // All internal timing/lerp values are normalized (0..1)
        private float m_startValue = 0f;
        private float m_currentValue = 0f;
        private float m_targetValue = 0f;

        private bool m_isFollowing = false;

        protected Slider MainSlider => m_mainSlider;

        /// <summary>
        /// 정규화된 값(0.0 ~ 1.0)을 전달하여 게이지를 설정합니다.
        /// <para>값이 감소할 경우 서브 슬라이더가 딜레이 후 부드럽게 뒤따라옵니다.</para>
        /// </summary>
        /// <param name="targetValue">목표 정규화 값 (0 ~ 1)</param>
        public void SetValue(float targetValue)
        {
            if (m_mainSlider == null || m_subSlider == null)
                return;

            float mainNorm = GetNormalizedValue(m_mainSlider);
            float clampedTarget = Mathf.Clamp01(targetValue);
            bool hasChanged = !Mathf.Approximately(mainNorm, clampedTarget);

            if (m_followIfDecreaseOnly && clampedTarget >= mainNorm)
            {
                SetSliderNormalizedValue(m_mainSlider, clampedTarget);
                SetSliderNormalizedValue(m_subSlider, clampedTarget);
                m_isFollowing = false;
                if (hasChanged)
                    OnValueChanged(mainNorm, clampedTarget);
                return;
            }

            SetSliderNormalizedValue(m_mainSlider, clampedTarget);

            if (hasChanged)
                OnValueChanged(mainNorm, clampedTarget);

            if (clampedTarget < mainNorm)
                OnValueDecreased(mainNorm, clampedTarget);

            m_remainDelay = m_followDelay;
            m_startValue = GetNormalizedValue(m_subSlider);
            m_currentValue = m_startValue;
            m_targetValue = clampedTarget;

            m_isFollowing = true;
        }

        /// <summary>
        /// 메인 슬라이더의 값이 변경되었을 때 호출됩니다.
        /// </summary>
        protected virtual void OnValueChanged(float prevNorm, float newNorm) { }

        /// <summary>
        /// 값이 감소할 때 호출됩니다. 서브클래스에서 override하여 감소 연출을 추가할 수 있습니다.
        /// 호출 시점에 MainSlider는 이미 newNorm으로 업데이트된 상태입니다.
        /// </summary>
        protected virtual void OnValueDecreased(float prevNorm, float newNorm) { }

        /// <summary>
        /// 지연 추적 연출 없이 메인 및 서브 슬라이더의 값을 즉시 목표치로 동기화합니다.
        /// </summary>
        /// <param name="targetValue">목표 정규화 값 (0 ~ 1)</param>
        public void SetValueInstant(float targetValue)
        {
            if (m_mainSlider == null || m_subSlider == null)
                return;
            SetSliderNormalizedValue(m_mainSlider, targetValue);
            SetSliderNormalizedValue(m_subSlider, targetValue);
            m_isFollowing = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_mainSlider == null)
            {
                Debug.LogWarning("MainSlider is not assigned.");
            }
            if (m_subSlider == null)
            {
                Debug.LogWarning("SubSlider is not assigned.");
            }
        }
#endif

        protected virtual void Update()
        {
            UpdateSubGaugeFill();
        }

        private void UpdateSubGaugeFill()
        {
            if (!m_isFollowing)
                return;

            if (m_remainDelay > 0f)
            {
                m_remainDelay -= Time.deltaTime;
                if (m_remainDelay < 0f)
                    m_remainDelay = 0f;
                return;
            }

            if (m_currentValue != m_targetValue)
            {
                m_currentValue = Mathf.MoveTowards(m_currentValue, m_targetValue, m_followSpeed * Time.deltaTime);
                SetSliderNormalizedValue(m_subSlider, m_currentValue);
            }
            else
            {
                m_isFollowing = false;
            }
        }

        private float GetNormalizedValue(Slider s)
        {
            if (s == null) return 0f;
            return Mathf.InverseLerp(s.minValue, s.maxValue, s.value);
        }

        private void SetSliderNormalizedValue(Slider s, float normalized)
        {
            if (s == null) return;
            normalized = Mathf.Clamp01(normalized);
            s.value = Mathf.Lerp(s.minValue, s.maxValue, normalized);
        }
    }
}