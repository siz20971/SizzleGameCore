using System.Collections;
using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// MonoBehaviour가 아닌 순수 C# 일반 클래스나 정적(static) 메서드에서 코루틴을 실행할 수 있도록 지원하는 전역 코루틴 호스트입니다.
    /// </summary>
    public class GlobalCoroutineRunner : PersistentSingletonMono<GlobalCoroutineRunner>
    {
        /// <summary>
        /// 코루틴을 전역 호스트에서 실행합니다.
        /// </summary>
        /// <param name="routine">실행할 코루틴 루틴</param>
        /// <returns>생성된 Coroutine 핸들</returns>
        public static Coroutine Run(IEnumerator routine)
        {
            if (routine == null) return null;
            return Instance.StartCoroutine(routine);
        }

        /// <summary>
        /// 실행 중인 코루틴을 중지합니다.
        /// </summary>
        /// <param name="coroutine">중지할 Coroutine 핸들</param>
        public static void Stop(Coroutine coroutine)
        {
            if (coroutine != null && HasInstance)
            {
                Instance.StopCoroutine(coroutine);
            }
        }

        /// <summary>
        /// 전역 호스트에서 실행 중인 모든 코루틴을 일괄 중지합니다.
        /// </summary>
        public static void StopAll()
        {
            if (HasInstance)
            {
                Instance.StopAllCoroutines();
            }
        }
    }
}
