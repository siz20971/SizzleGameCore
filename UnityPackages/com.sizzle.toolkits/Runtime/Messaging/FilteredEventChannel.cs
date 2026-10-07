using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 동일한 이벤트 데이터 타입이라도 특정 채널 식별자(문자열, 정수 등)나 필터 조건식에 따라 관심 있는 수신자에게만 선택적으로 이벤트를 라우팅하는 필터링 채널 버스입니다.
    /// 예: 특정 팀 ID 대상 이벤트, 퀘스트 ID별 이벤트, UI 서브채널 분기 등에 유용합니다.
    /// </summary>
    /// <typeparam name="TEvent">이벤트 데이터 타입</typeparam>
    public class FilteredEventChannel<TEvent>
    {
        private class Subscription
        {
            public readonly Action<TEvent> Handler;
            public readonly Func<TEvent, bool> Filter;

            public Subscription(Action<TEvent> handler, Func<TEvent, bool> filter)
            {
                Handler = handler;
                Filter = filter;
            }
        }

        private readonly Dictionary<string, List<Action<TEvent>>> m_channelSubscribers = new();
        private readonly List<Subscription> m_predicateSubscribers = new();

        /// <summary>
        /// 특정 채널 키(ID)에 핸들러를 구독합니다.
        /// </summary>
        /// <param name="channelKey">채널 식별자 (예: "TeamA", "CombatLog")</param>
        /// <param name="handler">이벤트 수신 핸들러</param>
        public void Subscribe(string channelKey, Action<TEvent> handler)
        {
            if (string.IsNullOrEmpty(channelKey) || handler == null) return;

            if (!m_channelSubscribers.TryGetValue(channelKey, out var list))
            {
                list = new List<Action<TEvent>>();
                m_channelSubscribers[channelKey] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// 특정 조건식(Predicate)을 만족할 때만 호출되는 핸들러를 구독합니다.
        /// </summary>
        /// <param name="filter">이벤트 필터 조건식</param>
        /// <param name="handler">이벤트 수신 핸들러</param>
        public void Subscribe(Func<TEvent, bool> filter, Action<TEvent> handler)
        {
            if (filter == null || handler == null) return;
            m_predicateSubscribers.Add(new Subscription(handler, filter));
        }

        /// <summary>
        /// 특정 채널에서 핸들러 구독을 해제합니다.
        /// </summary>
        public void Unsubscribe(string channelKey, Action<TEvent> handler)
        {
            if (string.IsNullOrEmpty(channelKey) || handler == null) return;

            if (m_channelSubscribers.TryGetValue(channelKey, out var list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>
        /// 조건식 구독 목록에서 핸들러를 해제합니다.
        /// </summary>
        public void Unsubscribe(Action<TEvent> handler)
        {
            if (handler == null) return;
            m_predicateSubscribers.RemoveAll(s => s.Handler == handler);
        }

        /// <summary>
        /// 특정 채널로 이벤트를 발행합니다.
        /// </summary>
        public void Publish(string channelKey, TEvent eventData)
        {
            if (string.IsNullOrEmpty(channelKey)) return;

            if (m_channelSubscribers.TryGetValue(channelKey, out var list))
            {
                var snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i]?.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// 조건식 기반 구독자들에게 이벤트를 발행하고 조건을 통과한 핸들러만 실행합니다.
        /// </summary>
        public void PublishFiltered(TEvent eventData)
        {
            var snapshot = m_predicateSubscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].Filter(eventData))
                {
                    snapshot[i].Handler?.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// 모든 구독자를 비웁니다.
        /// </summary>
        public void Clear()
        {
            m_channelSubscribers.Clear();
            m_predicateSubscribers.Clear();
        }
    }
}
