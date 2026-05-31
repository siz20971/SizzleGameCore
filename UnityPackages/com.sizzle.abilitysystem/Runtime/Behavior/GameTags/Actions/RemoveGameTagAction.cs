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
        name: "Remove Game Tag",
        description: "Removes an exact GameTag from the target AbilityProcessor TagContainer.",
        story: "Remove game tag [Tag] on [Target]",
        category: "Action/Sizzle/Game Tags",
        id: "df17dfa7bdc94ef49e21900740764c06")]
    public partial class RemoveGameTagAction : Action
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

            processor.TagContainer.RemoveTag(tag);
            return Status.Success;
        }
    }
}
