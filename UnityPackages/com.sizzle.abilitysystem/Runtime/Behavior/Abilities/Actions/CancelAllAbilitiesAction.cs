using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Sizzle.AbilitySystem.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Cancel All Abilities",
        description: "Cancels all active abilities on the target AbilityProcessor.",
        story: "Cancel all abilities on [Target]",
        category: "Action/Sizzle/Ability",
        id: "5a972875314d4b6ba2c8b8a9b8928b04")]
    public partial class CancelAllAbilitiesAction : Action
    {
        [Tooltip("AbilityProcessor를 찾을 대상 오브젝트입니다. 비어 있으면 현재 Behavior Graph의 GameObject를 사용합니다.")]
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            processor.CancelAllAbilities();
            return Status.Success;
        }
    }
}