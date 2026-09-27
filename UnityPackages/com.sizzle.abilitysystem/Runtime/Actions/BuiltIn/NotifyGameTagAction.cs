using System;
using Sizzle.GameTagSystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 대상 엔티티의 GameTagContainer에 게임 태그 알림(Notify)을 발행하는 액션입니다.
    /// </summary>
    [Serializable]
    public class NotifyGameTagAction : AbilityAction
    {
        [Header("Tag Settings")]
        [Tooltip("알림을 보낼 게임 태그")]
        public GameTag Tag;

        protected override void OnStart(AbilityRuntimeContext context)
        {
            var container = context?.Processor?.TagContainer ?? context?.GameObject?.GetComponent<GameTagContainer>();
            if (container != null && !Tag.IsEmpty)
            {
                container.NotifyTag(Tag);
            }

            Complete();
        }
    }
}
