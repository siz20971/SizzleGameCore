using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 발행된 최신 이벤트의 마지막 값을 캐싱하여 보관하고,
    /// 나중에 새로 구독(Subscribe)한 리스너에게도 즉시 최신 상태를 1회 자동 전달해 주는 스티키(Sticky) 이벤트 버스입니다.
    /// 플레이어 스탯, 게임 상태, 네트워크 연결 상태 등 지속적인 상태 동기화에 유용합니다.
    /// </summary>
    public static class StickyMessageBus
    {
        private static readonly Dictionary<Type, List<Delegate>> s_subscribers = new();
        private static readonly Dictionary<Type, object> s_lastEvents = new();
        private static readonly object s_lock = new();

        /// <summary>
        /// 스티키 이벤트를 구독합니다.
        /// 이전에 발행된 캐시 데이터가 존재할 경우, 핸들러 등록 즉시 최신 데이터로 1회 호출됩니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입</typeparam>
        /// <param name="handler">수신 핸들러</param>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type type = typeof(T);
            object lastEvent = null;

            lock (s_lock)
            {
                if (!s_subscribers.TryGetValue(type, out var list))
                {
                    list = new List<Delegate>();
                    s_subscribers[type] = list;
                }

                if (!list.Contains(handler))
                {
                    list.Add(handler);
                }

                s_lastEvents.TryGetValue(type, out lastEvent);
            }

            // 캐싱된 최신 이벤트가 있다면 즉시 전달
            if (lastEvent != null && lastEvent is T typedEvent)
            {
                handler.Invoke(typedEvent);
            }
        }

        /// <summary>
        /// 스티키 이벤트 구독을 해제합니다.
        /// </summary>
        /// <typeparam name="T">해제할 이벤트 타입</typeparam>
        /// <param name="handler">등록 해제할 핸들러</param>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type type = typeof(T);
            lock (s_lock)
            {
                if (s_subscribers.TryGetValue(type, out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        s_subscribers.Remove(type);
                    }
                }
            }
        }

        /// <summary>
        /// 스티키 이벤트를 발행하고 최신 상태로 캐싱합니다.
        /// 현재 등록된 모든 구독자에게 전달되며, 이후 신규 구독자에게도 전달됩니다.
        /// </summary>
        /// <typeparam name="T">발행할 이벤트 타입</typeparam>
        /// <param name="eventData">전달할 이벤트 데이터</param>
        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);
            Delegate[] snapshot = null;

            lock (s_lock)
            {
                s_lastEvents[type] = eventData;

                if (s_subscribers.TryGetValue(type, out var list))
                {
                    snapshot = list.ToArray();
                }
            }

            if (snapshot != null)
            {
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
        /// 특정 이벤트 타입의 캐시된 최신 데이터를 가져옵니다.
        /// </summary>
        public static bool TryGetLast<T>(out T lastEvent)
        {
            lock (s_lock)
            {
                if (s_lastEvents.TryGetValue(typeof(T), out var obj) && obj is T typed)
                {
                    lastEvent = typed;
                    return true;
                }
            }

            lastEvent = default;
            return false;
        }

        /// <summary>
        /// 특정 이벤트 타입의 캐시된 데이터를 제거합니다.
        /// </summary>
        public static void ClearSticky<T>()
        {
            lock (s_lock)
            {
                s_lastEvents.Remove(typeof(T));
            }
        }

        /// <summary>
        /// 모든 구독자와 캐시 데이터를 완전히 초기화합니다.
        /// </summary>
        public static void ClearAll()
        {
            lock (s_lock)
            {
                s_subscribers.Clear();
                s_lastEvents.Clear();
            }
        }
    }
}
