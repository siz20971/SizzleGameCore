using System;
using UnityEngine;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// GameObject/Transform 계층 구조(Hierarchy)를 따라 이벤트를 부모 방향으로 버블링(Bubbling)하거나,
    /// 모든 자식 방향으로 터널링/브로드캐스팅(Tunneling)하는 계층형 이벤트 릴레이 컴포넌트입니다.
    /// UI 복합 위젯 계층이나 부위별 액터 구조에서 직관적인 이벤트 전파에 활용됩니다.
    /// </summary>
    public class HierarchicalEventRelay : MonoBehaviour
    {
        /// <summary>
        /// 특정 인터페이스나 메서드를 구현한 부모 방향으로 이벤트를 위로 전파(Bubble Up)합니다.
        /// </summary>
        /// <typeparam name="TTarget">수신할 컴포넌트 타입 또는 인터페이스</typeparam>
        /// <param name="origin">출발 트랜스폼</param>
        /// <param name="action">실행할 액션</param>
        /// <param name="includeSelf">자기 자신 포함 여부</param>
        public static void BubbleUp<TTarget>(Transform origin, Action<TTarget> action, bool includeSelf = true) where TTarget : class
        {
            if (origin == null || action == null) return;

            Transform current = includeSelf ? origin : origin.parent;

            while (current != null)
            {
                var targets = current.GetComponents<TTarget>();
                for (int i = 0; i < targets.Length; i++)
                {
                    action.Invoke(targets[i]);
                }

                current = current.parent;
            }
        }

        /// <summary>
        /// 하위의 모든 자식 트랜스폼 방향으로 이벤트를 아래로 전파(Tunnel Down)합니다.
        /// </summary>
        /// <typeparam name="TTarget">수신할 컴포넌트 타입 또는 인터페이스</typeparam>
        /// <param name="root">최상위 부모 트랜스폼</param>
        /// <param name="action">실행할 액션</param>
        /// <param name="includeInactive">비활성화된 자식 포함 여부</param>
        public static void TunnelDown<TTarget>(Transform root, Action<TTarget> action, bool includeInactive = false) where TTarget : class
        {
            if (root == null || action == null) return;

            var targets = root.GetComponentsInChildren<TTarget>(includeInactive);
            for (int i = 0; i < targets.Length; i++)
            {
                action.Invoke(targets[i]);
            }
        }

        /// <summary>
        /// 현재 컴포넌트 기준으로 부모 방향으로 버블링합니다.
        /// </summary>
        public void Bubble<TTarget>(Action<TTarget> action, bool includeSelf = true) where TTarget : class
        {
            BubbleUp(transform, action, includeSelf);
        }

        /// <summary>
        /// 현재 컴포넌트 기준으로 하위 모든 자식으로 터널링합니다.
        /// </summary>
        public void Tunnel<TTarget>(Action<TTarget> action, bool includeInactive = false) where TTarget : class
        {
            TunnelDown(transform, action, includeInactive);
        }
    }
}
