using System;
using System.Collections;
using UnityEngine;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// <see cref="CanvasGroup"/>의 Alpha, Interactable, BlocksRaycasts를 부드럽게 페이드인/아웃하고
    /// 팝업 창의 열기/닫기 라이프사이클을 1줄로 제어하는 헬퍼 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private float m_defaultDuration = 0.25f;
        [SerializeField] private EaseType m_easeType = EaseType.OutQuad;
        [SerializeField] private bool m_deactivateOnHide = true;

        private Coroutine m_fadeCoroutine;

        /// <summary>연결된 CanvasGroup 컴포넌트입니다.</summary>
        public CanvasGroup CanvasGroup => m_canvasGroup;

        /// <summary>현재 완전히 표시(Alpha >= 1)되어 있는지 여부입니다.</summary>
        public bool IsVisible => m_canvasGroup != null && m_canvasGroup.alpha >= 0.99f && gameObject.activeSelf;

        private void Reset()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// 캔버스 그룹을 페이드인하여 화면에 표시합니다.
        /// </summary>
        /// <param name="duration">지속 시간(초, null일 경우 기본값)</param>
        /// <param name="onComplete">완료 시 콜백</param>
        public void Show(float? duration = null, Action onComplete = null)
        {
            gameObject.SetActive(true);
            float d = duration ?? m_defaultDuration;
            StartFade(1f, true, true, d, onComplete);
        }

        /// <summary>
        /// 캔버스 그룹을 페이드아웃하여 화면에서 숨깁니다.
        /// </summary>
        /// <param name="duration">지속 시간(초, null일 경우 기본값)</param>
        /// <param name="onComplete">완료 시 콜백</param>
        public void Hide(float? duration = null, Action onComplete = null)
        {
            float d = duration ?? m_defaultDuration;
            StartFade(0f, false, false, d, () =>
            {
                if (m_deactivateOnHide)
                {
                    gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 애니메이션 없이 즉시 화면에 표시합니다.
        /// </summary>
        public void ShowInstant()
        {
            if (m_fadeCoroutine != null) StopCoroutine(m_fadeCoroutine);
            gameObject.SetActive(true);
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 애니메이션 없이 즉시 화면에서 숨깁니다.
        /// </summary>
        public void HideInstant()
        {
            if (m_fadeCoroutine != null) StopCoroutine(m_fadeCoroutine);
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
            if (m_deactivateOnHide)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Unity 6 Awaitable을 지원하는 비동기 Show 메서드입니다.
        /// </summary>
        public async Awaitable ShowAsync(float? duration = null)
        {
            var completionSource = new AwaitableCompletionSource();
            Show(duration, () => completionSource.SetResult());
            await completionSource.Awaitable;
        }

        /// <summary>
        /// Unity 6 Awaitable을 지원하는 비동기 Hide 메서드입니다.
        /// </summary>
        public async Awaitable HideAsync(float? duration = null)
        {
            var completionSource = new AwaitableCompletionSource();
            Hide(duration, () => completionSource.SetResult());
            await completionSource.Awaitable;
        }

        private void StartFade(float targetAlpha, bool interactable, bool blocksRaycasts, float duration, Action onComplete)
        {
            if (m_fadeCoroutine != null)
            {
                StopCoroutine(m_fadeCoroutine);
            }

            m_fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, interactable, blocksRaycasts, duration, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool interactable, bool blocksRaycasts, float duration, Action onComplete)
        {
            float startAlpha = m_canvasGroup.alpha;
            float elapsed = 0f;

            if (duration <= 0f)
            {
                m_canvasGroup.alpha = targetAlpha;
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = TweenEasing.Evaluate(m_easeType, t);
                    m_canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                    yield return null;
                }
                m_canvasGroup.alpha = targetAlpha;
            }

            m_canvasGroup.interactable = interactable;
            m_canvasGroup.blocksRaycasts = blocksRaycasts;
            m_fadeCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
