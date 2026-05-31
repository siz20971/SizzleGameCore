using System;
using Sizzle.GameTagSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Sizzle.AbilitySystem.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Cancel Ability",
        description: "Requests cancellation for the active ability identified by GameTag.",
        story: "Cancel ability [AbilityTag] on [Target]",
        category: "Action/Sizzle/Ability",
        id: "f2467a39558b4b0dbb4af8868e0eb203")]
    public partial class CancelAbilityAction : Action
    {
        [Tooltip("AbilityProcessor를 찾을 대상 오브젝트입니다. 비어 있으면 현재 Behavior Graph의 GameObject를 사용합니다.")]
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        [Tooltip("취소할 어빌리티의 MainTag 문자열입니다.")]
        [SerializeReference] public BlackboardVariable<string> AbilityTag = new BlackboardVariable<string>(string.Empty);

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag abilityTag = BehaviorAbilityUtility.GetGameTagValue(AbilityTag);
            if (abilityTag.IsEmpty)
            {
                LogFailure("AbilityTag is empty.");
                return Status.Failure;
            }

            return processor.CancelAbility(abilityTag) ? Status.Success : Status.Failure;
        }
    }
}