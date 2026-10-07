using System;
using System.Collections;
using UnityEngine;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 짧은 시간 동안 폭발적으로 발생하는 이벤트나 입력을 압축(Debounce)하거나 최대 빈도를 제한(Throttle)하여 시스템 부하를 방지하는 디스패처입니다.
    /// 검색창 실시간 입력 처리, 윈도우 리사이즈, 버튼 중복 연타 방지 등에 적합합니다.
    /// </summary>
    public class DebouncedEventDispatcher
    {
        private Coroutine m_debounceCoroutine;
        private float m_lastThrottleTime = -999f;

        /// <summary>
        /// 디바운스(Debounce): 마지막 호출 시점부터 지정된 딜레이(초) 동안 추가 호출이 없을 때 비로소 액션을 1회 실행합니다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <param name="delaySeconds">대기할 지연 시간(초)</param>
        public void Debounce(Action action, float delaySeconds)
        {
            if (action == null) return;

            if (m_debounceCoroutine != null)
            {
                GlobalCoroutineRunner.Stop(m_debounceCoroutine);
            }

            m_debounceCoroutine = GlobalCoroutineRunner.Run(DebounceRoutine(action, delaySeconds));
        }

        /// <summary>
        /// 쓰로틀(Throttle): 최초 호출 시 액션을 즉시 실행하고, 이후 지정된 쿨다운(초) 동안의 모든 호출을 무시합니다.
        /// </summary>
        /// <param name="action">실행할 액션</param>
        /// <param name="intervalSeconds">무시할 인터벌 주기(초)</param>
        /// <returns>액션이 실제로 실행되었는지 여부</returns>
        public bool Throttle(Action action, float intervalSeconds)
        {
            if (action == null) return false;

            float now = Time.unscaledTime;
            if (now - m_lastThrottleTime >= intervalSeconds)
            {
                m_lastThrottleTime = now;
                action.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 대기 중인 디바운스 작업을 취소합니다.
        /// </summary>
        public void Cancel()
        {
            if (m_debounceCoroutine != null)
            {
                GlobalCoroutineRunner.Stop(m_debounceCoroutine);
                m_debounceCoroutine = null;
            }
        }

        private IEnumerator DebounceRoutine(Action action, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            m_debounceCoroutine = null;
            action.Invoke();
        }
    }
}
