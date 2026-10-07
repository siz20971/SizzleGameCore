using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.Toolkits.Input
{
    /// <summary>
    /// <para>액션 및 격투 게임에서 공격/회피/점프 키 선입력(Input Buffering)을 처리하는 제네릭 고성능 입력 버퍼입니다.</para>
    /// <para>리스트 순회 및 삭제 오버헤드 없이, 액션별 최신 타임스탬프를 딕셔너리로 관리하여
    /// <b>O(1) 조회, O(1) 소비 및 Zero-Alloc</b>으로 극도로 가볍고 빠르게 동작합니다.</para>
    /// </summary>
    /// <typeparam name="TKey">액션 또는 입력 유형 키 타입 (예: string, enum, GameTag 등)</typeparam>
    public class InputBuffer<TKey>
    {
        private readonly struct BufferEntry
        {
            public readonly float Timestamp;
            public readonly float CustomDuration;
            public readonly object CustomData;

            public BufferEntry(float timestamp, float customDuration, object customData)
            {
                Timestamp = timestamp;
                CustomDuration = customDuration;
                CustomData = customData;
            }
        }

        private readonly Dictionary<TKey, BufferEntry> m_buffer;
        private float m_defaultBufferDuration;

        /// <summary>기본 선입력 유지 유효 시간(초)입니다.</summary>
        public float DefaultBufferDuration
        {
            get => m_defaultBufferDuration;
            set => m_defaultBufferDuration = Mathf.Max(0.01f, value);
        }

        /// <summary>현재 하나 이상의 유효한 선입력이 대기 중인지 여부입니다.</summary>
        public bool IsBuffered => IsAnyBuffered;

        /// <summary>현재 하나 이상의 유효한 선입력이 대기 중인지 여부입니다.</summary>
        public bool IsAnyBuffered
        {
            get
            {
                float now = Time.unscaledTime;
                foreach (var kvp in m_buffer)
                {
                    if (now - kvp.Value.Timestamp <= kvp.Value.CustomDuration)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 지정된 기본 선입력 유지 시간으로 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="defaultBufferDuration">선입력 유지 시간(초, 기본 0.2초)</param>
        /// <param name="comparer">선택적 키 비교자</param>
        public InputBuffer(float defaultBufferDuration = 0.2f, IEqualityComparer<TKey> comparer = null)
        {
            m_defaultBufferDuration = Mathf.Max(0.01f, defaultBufferDuration);
            m_buffer = comparer != null ? new Dictionary<TKey, BufferEntry>(comparer) : new Dictionary<TKey, BufferEntry>();
        }

        /// <summary>
        /// 액션 입력을 버퍼에 등록합니다. (O(1) 즉시 갱신)
        /// </summary>
        /// <param name="action">액션 식별자</param>
        /// <param name="customDuration">이 액션에만 적용할 개별 유효 시간(초, null일 경우 기본값)</param>
        /// <param name="customData">동봉할 커스텀 데이터</param>
        public void Buffer(TKey action, float? customDuration = null, object customData = null)
        {
            if (action == null) return;

            float duration = customDuration ?? m_defaultBufferDuration;
            m_buffer[action] = new BufferEntry(Time.unscaledTime, duration, customData);
        }

        /// <summary>
        /// 특정 액션의 선입력이 유효 시간 내에 존재하는지 O(1)로 확인하고, 유효하면 즉시 소비(무효화)합니다.
        /// </summary>
        /// <param name="action">확인할 액션 키</param>
        /// <returns>선입력 존재 및 소비 성공 여부</returns>
        public bool Consume(TKey action)
        {
            return Consume(action, out _);
        }

        /// <summary>
        /// 특정 액션의 선입력을 소비하면서 동봉된 커스텀 데이터를 함께 반환합니다. (O(1))
        /// </summary>
        public bool Consume(TKey action, out object customData)
        {
            customData = null;
            if (action == null) return false;

            if (m_buffer.TryGetValue(action, out var entry))
            {
                if (Time.unscaledTime - entry.Timestamp <= entry.CustomDuration)
                {
                    customData = entry.CustomData;
                    // 만료 처리하여 재소비 방지 (제거하지 않고 타임스탬프만 무효화하여 딕셔너리 내부 재할당 방지)
                    m_buffer[action] = new BufferEntry(-999f, 0f, null);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 소비하지 않고 특정 액션이 현재 유효한 선입력 상태인지 O(1)로 확인합니다.
        /// </summary>
        public bool HasAction(TKey action)
        {
            if (action == null) return false;

            if (m_buffer.TryGetValue(action, out var entry))
            {
                return Time.unscaledTime - entry.Timestamp <= entry.CustomDuration;
            }

            return false;
        }

        /// <summary>
        /// 소비하지 않고 특정 액션이 현재 유효한 선입력 상태인지 O(1)로 확인합니다. (<see cref="HasAction"/>의 단축 별칭)
        /// </summary>
        public bool Has(TKey action) => HasAction(action);

        /// <summary>
        /// 특정 액션의 선입력 잔여 유효 시간(초)을 반환합니다. 만료되었거나 없으면 0을 반환합니다.
        /// </summary>
        public float GetRemaining(TKey action)
        {
            if (action == null) return 0f;

            if (m_buffer.TryGetValue(action, out var entry))
            {
                float elapsed = Time.unscaledTime - entry.Timestamp;
                float remaining = entry.CustomDuration - elapsed;
                return remaining > 0f ? remaining : 0f;
            }

            return 0f;
        }

        /// <summary>
        /// 특정 액션의 선입력을 무효화합니다.
        /// </summary>
        public void ClearAction(TKey action)
        {
            if (action != null)
            {
                m_buffer[action] = new BufferEntry(-999f, 0f, null);
            }
        }

        /// <summary>
        /// 특정 액션의 선입력을 무효화합니다. (<see cref="ClearAction"/>의 단축 별칭)
        /// </summary>
        public void Clear(TKey action) => ClearAction(action);

        /// <summary>
        /// 모든 선입력 데이터를 초기화합니다.
        /// </summary>
        public void ClearAll()
        {
            m_buffer.Clear();
        }

        /// <summary>
        /// 기존 매 프레임 순회 방식과의 호환을 위한 빈 메서드입니다. 타임스탬프 기반 검사이므로 호출하지 않아도 무방합니다.
        /// </summary>
        public void Tick(float deltaTime) { }
    }

    /// <summary>
    /// 문자열 키 기반의 기본 선입력 버퍼 클래스입니다.
    /// </summary>
    public class InputBuffer : InputBuffer<string>
    {
        public InputBuffer(float defaultBufferDuration = 0.2f)
            : base(defaultBufferDuration, StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
