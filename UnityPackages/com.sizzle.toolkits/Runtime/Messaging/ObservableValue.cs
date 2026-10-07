using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 값의 변경을 감지하여 값이 달라질 때만 이벤트를 통지하는 가벼운 반응형(Reactive) 프로퍼티 래퍼입니다.
    /// 외부 무거운 라이브러리(UniRx, R3) 없이도 데이터 기반 UI 갱신이나 상태 동기화를 구현할 수 있습니다.
    /// </summary>
    /// <typeparam name="T">보관할 값의 타입</typeparam>
    [Serializable]
    public class ObservableValue<T>
    {
        [SerializeField] private T m_value;

        /// <summary>
        /// 값이 변경되었을 때 호출되는 델리게이트 이벤트입니다.
        /// (매개변수: 이전 값, 새로운 값)
        /// </summary>
        public event Action<T, T> OnValueChanged;

        /// <summary>
        /// 현재 보관된 값입니다. 새 값을 대입하면 값이 다를 때만 이벤트가 발동합니다.
        /// </summary>
        public T Value
        {
            get => m_value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(m_value, value))
                {
                    T oldValue = m_value;
                    m_value = value;
                    OnValueChanged?.Invoke(oldValue, m_value);
                }
            }
        }

        /// <summary>
        /// 기본값으로 인스턴스를 초기화합니다.
        /// </summary>
        public ObservableValue()
        {
            m_value = default;
        }

        /// <summary>
        /// 초기값을 지정하여 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="initialValue">초기값</param>
        public ObservableValue(T initialValue)
        {
            m_value = initialValue;
        }

        /// <summary>
        /// 이벤트를 트리거하지 않고 내부 값을 조용히 변경합니다.
        /// </summary>
        /// <param name="newValue">새로운 값</param>
        public void SetValueSilent(T newValue)
        {
            m_value = newValue;
        }

        /// <summary>
        /// 현재 값을 기준으로 OnValueChanged 이벤트를 강제로 발생시킵니다.
        /// </summary>
        public void Notify()
        {
            OnValueChanged?.Invoke(m_value, m_value);
        }

        public static implicit operator T(ObservableValue<T> observable)
        {
            return observable != null ? observable.Value : default;
        }

        public override string ToString()
        {
            return m_value != null ? m_value.ToString() : "null";
        }
    }
}
