using System;
using Sizzle.GameTagSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Sizzle.AbilitySystem.Behavior
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Has Exact Game Tag",
        description: "Checks whether the target AbilityProcessor TagContainer currently owns the specified exact GameTag.",
        story: "Target [Target] has exact game tag [Tag]",
        category: "Condition/Sizzle/Game Tags",
        id: "4743ec7d0aa5416faf23563c730ef40b")]
    public partial class HasExactGameTagCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<GameTag> Tag;

        private AbilityProcessor m_processor;

        public override void OnStart()
        {
            BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor);
        }

        public override bool IsTrue()
        {
            GameTag tag = Tag != null ? Tag.Value : default;
            return m_processor != null && !tag.IsEmpty && m_processor.TagContainer.HasExactTag(tag);
        }

        public override void OnEnd()
        {
            m_processor = null;
        }
    }
}