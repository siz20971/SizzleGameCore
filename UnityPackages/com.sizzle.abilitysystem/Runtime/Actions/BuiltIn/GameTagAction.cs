using System;
using Sizzle.GameTagSystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    public enum GameTagOperationType
    {
        Add,
        Remove
    }

    /// <summary>
    /// 대상 엔티티의 GameTagContainer에 게임 태그를 추가(Add)하거나 제거(Remove)하는 액션입니다.
    /// </summary>
    [Serializable]
    public class GameTagAction : AbilityAction
    {
        [Header("Tag Settings")]
        [Tooltip("태그 조작 유형 (추가 / 제거)")]
        public GameTagOperationType Operation = GameTagOperationType.Add;

        [Tooltip("대상 게임 태그")]
        public GameTag Tag;

        protected override void OnStart(AbilityRuntimeContext context)
        {
            var container = context?.Processor?.TagContainer ?? context?.GameObject?.GetComponent<GameTagContainer>();
            if (container != null && !Tag.IsEmpty)
            {
                switch (Operation)
                {
                    case GameTagOperationType.Add:
                        container.AddTag(Tag);
                        break;
                    case GameTagOperationType.Remove:
                        container.RemoveTag(Tag);
                        break;
                }
            }

            Complete();
        }
    }
}
