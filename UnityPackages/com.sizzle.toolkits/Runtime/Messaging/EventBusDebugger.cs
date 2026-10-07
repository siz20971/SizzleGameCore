using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 이벤트 버스를 통해 발행되는 이벤트들의 흐름(타입, 시간, 발행 횟수)을 링 버퍼에 기록하여
    /// 디버깅 및 프로파일링을 지원하는 디버그 트레이서입니다.
    /// </summary>
    public static class EventBusDebugger
    {
        /// <summary>
        /// 단일 이벤트 발행 기록 레코드입니다.
        /// </summary>
        public readonly struct EventRecord
        {
            public readonly Type EventType;
            public readonly float Time;
            public readonly string Summary;

            public EventRecord(Type eventType, float time, string summary)
            {
                EventType = eventType;
                Time = time;
                Summary = summary;
            }
        }

        private static readonly Queue<EventRecord> s_history = new();
        private static readonly Dictionary<Type, int> s_eventCounts = new();
        private static int s_maxHistorySize = 100;
        private static bool s_isEnabled = true;
        private static readonly object s_lock = new();

        /// <summary>디버그 추적 활성화 여부입니다.</summary>
        public static bool IsEnabled
        {
            get => s_isEnabled;
            set => s_isEnabled = value;
        }

        /// <summary>최대 보관할 히스토리 레코드 수입니다.</summary>
        public static int MaxHistorySize
        {
            get => s_maxHistorySize;
            set => s_maxHistorySize = System.Math.Max(10, value);
        }

        /// <summary>
        /// 이벤트 발행 내역을 기록합니다.
        /// </summary>
        public static void Record<T>(T eventData)
        {
            if (!s_isEnabled) return;

            Type type = typeof(T);
            float time = UnityEngine.Time.time;
            string summary = eventData != null ? eventData.ToString() : "null";

            lock (s_lock)
            {
                s_eventCounts[type] = s_eventCounts.GetValueOrDefault(type, 0) + 1;

                if (s_history.Count >= s_maxHistorySize)
                {
                    s_history.Dequeue();
                }

                s_history.Enqueue(new EventRecord(type, time, summary));
            }
        }

        /// <summary>
        /// 특정 이벤트 타입이 지금까지 총 몇 번 발행되었는지 조회합니다.
        /// </summary>
        public static int GetPublishCount<T>()
        {
            lock (s_lock)
            {
                return s_eventCounts.GetValueOrDefault(typeof(T), 0);
            }
        }

        /// <summary>
        /// 현재 보관 중인 최근 이벤트 기록 목록을 배열로 가져옵니다.
        /// </summary>
        public static EventRecord[] GetHistorySnapshot()
        {
            lock (s_lock)
            {
                return s_history.ToArray();
            }
        }

        /// <summary>
        /// 기록된 모든 히스토리와 카운트를 초기화합니다.
        /// </summary>
        public static void Clear()
        {
            lock (s_lock)
            {
                s_history.Clear();
                s_eventCounts.Clear();
            }
        }
    }
}
