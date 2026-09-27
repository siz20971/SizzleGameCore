using System;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 지정된 시간(Duration) 동안 대기한 후 다음 액션으로 진행하는 범용 대기 액션입니다.
    /// 선딜레이, 후딜레이, 스킬 차징 시간 등을 구성할 때 사용합니다.
    /// </summary>
    [Serializable]
    public class WaitDelayAction : AbilityAction
    {
        [Header("Wait Settings")]
        [Tooltip("대기 시간 (초)")]
        [Min(0f)]
        public float Duration = 0.5f;

        protected override void OnStart(AbilityRuntimeContext context)
        {
            if (Duration <= 0f)
            {
                Complete();
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (ElapsedTime >= Duration)
            {
                Complete();
            }
        }
    }
}
