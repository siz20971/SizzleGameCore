using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 게임 이벤트 버스로부터 특정 이벤트(T)를 수신하기 위한 리스너 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">구독할 이벤트 데이터 모델 타입</typeparam>
    public interface IGameEventListener<T>
    {
        /// <summary>
        /// 이벤트가 발행되었을 때 호출되는 콜백 메서드입니다.
        /// </summary>
        /// <param name="eventData">수신된 이벤트 데이터</param>
        void OnGameEvent(T eventData);
    }

    /// <summary>
    /// 강한 결합 없이 타입 기반으로 이벤트를 발행(Publish)하고 구독(Subscribe)할 수 있는 정적 인게임 이벤트 버스입니다.
    /// </summary>
    public static class GameEventBus
    {
        private static Dictionary<Type, object> _listeners = new Dictionary<Type, object>();

        /// <summary>
        /// 특정 이벤트 타입(T)의 리스너를 이벤트 버스에 등록합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입</typeparam>
        /// <param name="listener">이벤트를 수신할 리스너 인스턴스</param>
        public static void Subscribe<T>(IGameEventListener<T> listener)
        {
            Type type = typeof(T);

            if (!_listeners.ContainsKey(type))
                _listeners[type] = new List<IGameEventListener<T>>();

            List<IGameEventListener<T>> list = _listeners[type] as List<IGameEventListener<T>>;
            if (!list.Contains(listener))
            {
                list.Add(listener);
            }
        }

        /// <summary>
        /// 등록된 이벤트 리스너를 이벤트 버스에서 해제합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입</typeparam>
        /// <param name="listener">해제할 리스너 인스턴스</param>
        public static void Unsubscribe<T>(IGameEventListener<T> listener)
        {
            Type type = typeof(T);
            if (_listeners.TryGetValue(type, out var listObj))
            {
                List<IGameEventListener<T>> list = listObj as List<IGameEventListener<T>>;
                list.Remove(listener);
            }
        }

        private class ActionEventListener<TEvent> : IGameEventListener<TEvent>
        {
            public Action<TEvent> Action { get; }
            public ActionEventListener(Action<TEvent> action) => Action = action;
            public void OnGameEvent(TEvent eventData) => Action?.Invoke(eventData);
        }

        /// <summary>
        /// 특정 이벤트 타입(T)의 액션 콜백을 이벤트 버스에 등록합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입</typeparam>
        /// <param name="action">이벤트를 수신할 콜백</param>
        public static void Subscribe<T>(Action<T> action)
        {
            if (action == null) return;
            Subscribe<T>(new ActionEventListener<T>(action));
        }

        /// <summary>
        /// 등록된 액션 콜백을 이벤트 버스에서 해제합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입</typeparam>
        /// <param name="action">해제할 콜백</param>
        public static void Unsubscribe<T>(Action<T> action)
        {
            if (action == null) return;
            Type type = typeof(T);
            if (_listeners.TryGetValue(type, out var listObj))
            {
                if (listObj is List<IGameEventListener<T>> list)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is ActionEventListener<T> ael && ael.Action.Equals(action))
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 특정 이벤트 타입(T)의 데이터 인스턴스를 구독 중인 모든 리스너에게 전파합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입</typeparam>
        /// <param name="eventData">전파할 이벤트 데이터</param>
        public static void Publish<T>(T eventData)
        {
            Type type = typeof(T);
            if (_listeners.TryGetValue(type, out var listObj))
            {
                List<IGameEventListener<T>> list = listObj as List<IGameEventListener<T>>;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].OnGameEvent(eventData);
                }
            }
        }
    }
}