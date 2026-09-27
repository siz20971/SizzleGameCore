using System;
using Sizzle.GameTagSystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 대상 엔티티의 GameTagContainer에 일정 시간 동안 유지되는 게임 태그를 추가하는 액션입니다.
    /// </summary>
    [Serializable]
    public class TimedGameTagAction : AbilityAction
    {
        [Header("Tag Settings")]
        [Tooltip("추가할 게임 태그")]
        public GameTag Tag;

        [Tooltip("태그 유지 시간 (초)")]
        [Min(0f)]
        public float Duration = 1.0f;

        protected override void OnStart(AbilityRuntimeContext context)
        {
            var container = context?.Processor?.TagContainer ?? context?.GameObject?.GetComponent<GameTagContainer>();
            if (container != null && !Tag.IsEmpty)
            {
                container.AddTagTimed(Tag, Mathf.Max(0f, Duration));
            }

            Complete();
        }
    }
}
