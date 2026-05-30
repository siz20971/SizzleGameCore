using System;
using Sizzle.GameTagSystem;
using UnityEngine;
using UnityEngine.Playables;

namespace Sizzle.AbilitySystem.Timeline
{
    [Serializable]
    public class AbilityTimelineCommandPlayableBehaviour : PlayableBehaviour
    {
        public AbilityTimelineCommandType Command = AbilityTimelineCommandType.ActivateAbility;
        public GameTag Tag;
        public float Duration = 1f;

        public bool Execute(GameObject boundObject)
        {
            AbilityProcessor processor = AbilityProcessorResolver.Resolve(boundObject);
            if (processor == null)
            {
                Debug.LogWarning("[AbilityTimelineCommand] AbilityProcessor or IAbilitySystemComponent was not found on the bound GameObject.", boundObject);
                return false;
            }

            switch (Command)
            {
                case AbilityTimelineCommandType.CancelAllAbilities:
                    processor.CancelAllAbilities();
                    return true;
            }

            if (Tag.IsEmpty)
            {
                Debug.LogWarning($"[AbilityTimelineCommand] Tag is empty for command {Command}.", boundObject);
                return false;
            }

            switch (Command)
            {
                case AbilityTimelineCommandType.ActivateAbility:
                    return processor.TryActivateAbility(Tag) == AbilityActivateResult.Success;

                case AbilityTimelineCommandType.CancelAbility:
                    return processor.CancelAbility(Tag);

                case AbilityTimelineCommandType.AddTag:
                    processor.TagContainer.AddTag(Tag);
                    return true;

                case AbilityTimelineCommandType.RemoveTag:
                    processor.TagContainer.RemoveTag(Tag);
                    return true;

                case AbilityTimelineCommandType.NotifyTag:
                    processor.TagContainer.NotifyTag(Tag);
                    return true;

                case AbilityTimelineCommandType.AddTimedTag:
                    processor.TagContainer.AddTagTimed(Tag, Mathf.Max(0f, Duration));
                    return true;

                default:
                    return false;
            }
        }
    }
}