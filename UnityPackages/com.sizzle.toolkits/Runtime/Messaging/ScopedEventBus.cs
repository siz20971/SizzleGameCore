using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 전역 싱글톤이 아닌 특정 오브젝트나 몬스터, UI 팝업 단위로 격리되어 작동하는 인스턴스형 로컬 이벤트 버스입니다.
    /// 동일 엔티티에 속한 여러 컴포넌트 간의 결합도를 낮추는 데 최적화되어 있습니다.
    /// </summary>
    public class ScopedEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> m_subscribers = new();

        /// <summary>
        /// 지정한 타입의 이벤트를 구독합니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입</typeparam>
        /// <param name="handler">이벤트 수신 시 실행할 핸들러</param>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type type = typeof(T);
            if (!m_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                m_subscribers[type] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// 지정한 타입의 이벤트 구독을 해제합니다.
        /// </summary>
        /// <typeparam name="T">해제할 이벤트 타입</typeparam>
        /// <param name="handler">등록 해제할 핸들러</param>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type type = typeof(T);
            if (m_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    m_subscribers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 해당 스코프 버스에 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T">발행할 이벤트 타입</typeparam>
        /// <param name="eventData">전달할 이벤트 데이터</param>
        public void Publish<T>(T eventData)
        {
            Type type = typeof(T);
            if (m_subscribers.TryGetValue(type, out var list))
            {
                // 순회 중 컬렉션 변경 방지를 위해 역순 또는 복사 순회
                var snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (snapshot[i] is Action<T> action)
                    {
                        action.Invoke(eventData);
                    }
                }
            }
        }

        /// <summary>
        /// 등록된 모든 구독자를 비우고 초기화합니다.
        /// </summary>
        public void Clear()
        {
            m_subscribers.Clear();
        }
    }
}
