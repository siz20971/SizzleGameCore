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
        [SerializeReference] public BlackboardVariable<GameTag> Tag;

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag tag = Tag != null ? Tag.Value : default;
            if (tag.IsEmpty)
            {
                LogFailure("Tag is empty.");
                return Status.Failure;
            }

            processor.TagContainer.AddTag(tag);
            return Status.Success;
        }
    }

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
        [SerializeReference] public BlackboardVariable<GameTag> Tag;

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag tag = Tag != null ? Tag.Value : default;
            if (tag.IsEmpty)
            {
                LogFailure("Tag is empty.");
                return Status.Failure;
            }

            processor.TagContainer.RemoveTag(tag);
            return Status.Success;
        }
    }

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
        [SerializeReference] public BlackboardVariable<GameTag> Tag;
        [SerializeReference] public BlackboardVariable<float> Duration = new BlackboardVariable<float>(0f);

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out AbilityProcessor processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag tag = Tag != null ? Tag.Value : default;
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