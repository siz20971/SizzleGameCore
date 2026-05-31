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
        name: "Add Timed Game Tag",
        description: "Adds a GameTag for the given duration to the target AbilityProcessor TagContainer.",
        story: "Add timed game tag [Tag] for [Duration] seconds on [Target]",
        category: "Action/Sizzle/Game Tags",
        id: "6a041ddd5ba9465b8eae99ce8f860f07")]
    public partial class AddTimedGameTagAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<string> Tag = new BlackboardVariable<string>(string.Empty);
        [SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>(0f);

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

            float duration = Duration != null ? Mathf.Max(0f, Duration.Value) : 0f;
            processor.TagContainer.AddTagTimed(tag, duration);
            return Status.Success;
        }
    }
}
