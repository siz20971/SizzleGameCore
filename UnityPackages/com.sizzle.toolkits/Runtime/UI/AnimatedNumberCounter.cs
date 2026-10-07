using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if CORE_TMPRO
using TMPro;
#endif

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 골드, 스코어, 경험치 수치가 변경될 때 목표치까지 부드럽게 롤링(카운트업/다운) 애니메이션을 적용해 주는 텍스트 카운터 컴포넌트입니다.
    /// uGUI Text와 UnityEvent를 통한 TextMeshPro 연동을 모두 지원합니다.
    /// </summary>
    public class AnimatedNumberCounter : MonoBehaviour
    {
        [Header("Target & Format")]
        [Tooltip("숫자를 표시할 uGUI Text 컴포넌트 (선택 사항)")]
        [SerializeField] private Text m_uiText;

#if CORE_TMPRO
        [Tooltip("숫자를 표시할 TextMeshPro 컴포넌트 (선택 사항)")]
        [SerializeField] private TMP_Text m_tmpText;
#endif

        [Tooltip("숫자 앞에 붙을 접두사 (예: '$', 'Lv.')")]
        [SerializeField] private string m_prefix = "";

        [Tooltip("숫자 뒤에 붙을 접미사 (예: ' Gold', ' Exp', '개')")]
        [SerializeField] private string m_suffix = "";

        [Tooltip("3자리마다 콤마(,) 천 단위 구분 기호를 표시할지 여부 (예: 1,234,567)")]
        [SerializeField] private bool m_useThousandsSeparator = true;

        [Tooltip("큰 숫자를 단위 축약 형식(예: 1.2K, 3.5M)으로 표시할지 여부")]
        [SerializeField] private bool m_compactFormat = false;

        [Header("Animation")]
        [Tooltip("목표 숫자까지 롤링 카운팅되는 총 소요 시간(초)")]
        [SerializeField] private float m_duration = 0.5f;

        [Tooltip("숫자 카운팅 시 적용할 이징(Easing) 보간 곡선")]
        [SerializeField] private EaseType m_easeType = EaseType.OutQuad;

        [Header("Events")]
        [Tooltip("포맷팅된 문자열이 갱신될 때마다 호출되는 이벤트 (TextMeshPro 연동 시 text 전달)")]
        [SerializeField] private UnityEvent<string> m_onFormattedStringChanged;

        [Tooltip("정수 수치가 갱신될 때마다 호출되는 이벤트")]
        [SerializeField] private UnityEvent<long> m_onValueChanged;

        private long m_currentValue;
        private long m_targetValue;
        private Coroutine m_countCoroutine;

        /// <summary>현재 표시 중인 수치입니다.</summary>
        public long CurrentValue => m_currentValue;

        /// <summary>목표 수치입니다.</summary>
        public long TargetValue => m_targetValue;

        /// <summary>수치 변경 시 포맷팅된 문자열을 수신하는 C# 이벤트입니다.</summary>
        public event Action<string> OnFormattedStringChanged;

        private void Reset()
        {
            m_uiText = GetComponent<Text>();
#if CORE_TMPRO
            m_tmpText = GetComponent<TMP_Text>();
#endif
        }

        private void Awake()
        {
            if (m_uiText == null)
            {
                m_uiText = GetComponent<Text>();
            }
#if CORE_TMPRO
            if (m_tmpText == null)
            {
                m_tmpText = GetComponent<TMP_Text>();
            }
#endif
        }

        /// <summary>
        /// 목표 숫자로 부드럽게 카운팅을 시작합니다.
        /// </summary>
        /// <param name="target">도달할 목표 숫자</param>
        /// <param name="duration">지속 시간(초, null일 경우 기본값)</param>
        public void SetTarget(long target, float? duration = null)
        {
            m_targetValue = target;
            float d = duration ?? m_duration;

            if (m_countCoroutine != null)
            {
                StopCoroutine(m_countCoroutine);
            }

            if (!gameObject.activeInHierarchy || d <= 0f)
            {
                SetInstant(target);
                return;
            }

            m_countCoroutine = StartCoroutine(CountRoutine(m_currentValue, m_targetValue, d));
        }

        /// <summary>
        /// 애니메이션 없이 즉시 수치를 변경합니다.
        /// </summary>
        public void SetInstant(long value)
        {
            if (m_countCoroutine != null)
            {
                StopCoroutine(m_countCoroutine);
                m_countCoroutine = null;
            }

            m_currentValue = value;
            m_targetValue = value;
            ApplyDisplay(value);
        }

        private IEnumerator CountRoutine(long start, long end, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = TweenEasing.Evaluate(m_easeType, t);

                long current = (long)Mathf.Lerp(start, end, eased);
                if (current != m_currentValue)
                {
                    m_currentValue = current;
                    ApplyDisplay(current);
                }
                yield return null;
            }

            m_currentValue = end;
            ApplyDisplay(end);
            m_countCoroutine = null;
        }

        private void ApplyDisplay(long value)
        {
            string formattedValue;

            if (m_compactFormat)
            {
                formattedValue = FormatUtils.FormatCompact(value);
            }
            else if (m_useThousandsSeparator)
            {
                formattedValue = FormatUtils.FormatComma(value);
            }
            else
            {
                formattedValue = value.ToString();
            }

            string fullText = $"{m_prefix}{formattedValue}{m_suffix}";

            if (m_uiText != null)
            {
                m_uiText.text = fullText;
            }

#if CORE_TMPRO
            if (m_tmpText != null)
            {
                m_tmpText.text = fullText;
            }
#endif

            m_onFormattedStringChanged?.Invoke(fullText);
            m_onValueChanged?.Invoke(value);
            OnFormattedStringChanged?.Invoke(fullText);
        }
    }
}
