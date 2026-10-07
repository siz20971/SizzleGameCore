using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 이벤트를 즉시 처리하지 않고 큐에 적재해두었다가, 지정된 시점(프레임 말단, 특정 라이프사이클)에 일괄 배치(Batch) 순차 처리하는 큐형 메시지 버스입니다.
    /// 멀티스레드나 물리 계산 도중 발생한 이벤트를 메인 스레드 렌더링 시점에 안전하게 몰아서 디스패치할 때 유용합니다.
    /// </summary>
    public class QueuedMessageBus
    {
        private readonly struct QueuedItem
        {
            public readonly Type EventType;
            public readonly object EventData;

            public QueuedItem(Type eventType, object eventData)
            {
                EventType = eventType;
                EventData = eventData;
            }
        }

        private readonly Dictionary<Type, List<Delegate>> m_subscribers = new();
        private readonly Queue<QueuedItem> m_queue = new();
        private readonly object m_lock = new();

        /// <summary>현재 큐에 적재되어 대기 중인 메시지 수입니다.</summary>
        public int PendingCount
        {
            get
            {
                lock (m_lock) return m_queue.Count;
            }
        }

        /// <summary>
        /// 특정 타입의 이벤트를 구독합니다.
        /// </summary>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Type type = typeof(T);

            lock (m_lock)
            {
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
        }

        /// <summary>
        /// 특정 타입의 이벤트 구독을 해제합니다.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            Type type = typeof(T);

            lock (m_lock)
            {
                if (m_subscribers.TryGetValue(type, out var list))
                {
                    list.Remove(handler);
                }
            }
        }

        /// <summary>
        /// 이벤트를 큐에 인큐(Enqueue)합니다. (즉시 실행되지 않음)
        /// </summary>
        public void Enqueue<T>(T eventData)
        {
            lock (m_lock)
            {
                m_queue.Enqueue(new QueuedItem(typeof(T), eventData));
            }
        }

        /// <summary>
        /// 큐에 적재된 모든 이벤트를 FIFO 순서로 일괄 디스패치합니다.
        /// </summary>
        public void DispatchAll()
        {
            Queue<QueuedItem> processingQueue;

            lock (m_lock)
            {
                if (m_queue.Count == 0) return;
                processingQueue = new Queue<QueuedItem>(m_queue);
                m_queue.Clear();
            }

            while (processingQueue.Count > 0)
            {
                var item = processingQueue.Dequeue();
                Delegate[] snapshot = null;

                lock (m_lock)
                {
                    if (m_subscribers.TryGetValue(item.EventType, out var list))
                    {
                        snapshot = list.ToArray();
                    }
                }

                if (snapshot != null)
                {
                    for (int i = 0; i < snapshot.Length; i++)
                    {
                        try
                        {
                            snapshot[i].DynamicInvoke(item.EventData);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 대기 중인 큐를 비웁니다.
        /// </summary>
        public void ClearQueue()
        {
            lock (m_lock)
            {
                m_queue.Clear();
            }
        }
    }
}
