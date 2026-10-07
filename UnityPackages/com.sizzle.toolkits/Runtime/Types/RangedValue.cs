using UnityEngine;

namespace Sizzle.Toolkits.Types
{
    /// <summary>
    /// 최소값(Min)과 최대값(Max)을 가지며 값이 해당 범위 내에 존재하는지 검사할 수 있는 제네릭 범위 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="T">범위 값의 타입</typeparam>
    public abstract class RangedValue<T>
    {
        [SerializeField] private T m_min;
        [SerializeField] private T m_max;
        
        /// <summary> 범위의 최소값 </summary>
        public T Min => m_min;
        /// <summary> 범위의 최대값 </summary>
        public T Max => m_max;

        public RangedValue() { }
        public RangedValue(T min, T max)
        {
            m_min = min;
            m_max = max;
        }

        /// <summary> 지정한 값이 [Min, Max] 범위 내에 포함되는지 여부를 반환합니다. </summary>
        public abstract bool IsInRange(T value);
    }

    /// <summary>
    /// 인스펙터에서 직렬화 가능한 정수형(int) 최소/최대 범위 클래스입니다.
    /// </summary>
    [System.Serializable]
    public class RangedInt : RangedValue<int>
    {
        public RangedInt() : base() { }
        public RangedInt(int min, int max) : base(min, max) { }

        /// <summary> 정수 값이 [Min, Max] 범위 내에 포함되는지 확인합니다. </summary>
        public override bool IsInRange(int value)
        {
            return value >= Min && value <= Max;
        }
    }

    /// <summary>
    /// 인스펙터에서 직렬화 가능한 부동소수점(float) 최소/최대 범위 클래스입니다.
    /// </summary>
    [System.Serializable]
    public class RangedFloat : RangedValue<float>
    {
        public RangedFloat() : base() { }
        public RangedFloat(float min, float max) : base(min, max) { }

        /// <summary> 실수 값이 [Min, Max] 범위 내에 포함되는지 확인합니다. </summary>
        public override bool IsInRange(float value)
        {
            return value >= Min && value <= Max;
        }
    }
}
