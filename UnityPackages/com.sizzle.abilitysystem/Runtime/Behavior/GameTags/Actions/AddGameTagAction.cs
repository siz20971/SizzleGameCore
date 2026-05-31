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
        name: "Add Game Tag",
        description: "Adds an exact GameTag to the target AbilityProcessor TagContainer.",
        story: "Add game tag [Tag] on [Target]",
        category: "Action/Sizzle/Game Tags",
        id: "307ce90e57e64bf8825e4f0b1a2cf105")]
    public partial class AddGameTagAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<string> Tag = new BlackboardVariable<string>(string.Empty);

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag tag = BehaviorAbilityUtility.GetGameTagValue(Tag);
            if (tag.IsEmpty)
            {
                LogFailure("Tag is empty.");
                return Status.Failure;
            }

            processor.TagContainer.AddTag(tag);
            return Status.Success;
        }
    }
}