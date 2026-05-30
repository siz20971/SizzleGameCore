using System;
using Sizzle.GameTagSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Sizzle.AbilitySystem.Behavior
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Ability Active",
        description: "Checks whether the ability identified by GameTag is currently active.",
        story: "Ability [AbilityTag] is active on [Target]",
        category: "Condition/Sizzle/Ability",
        id: "9de7a624ef144216a8ebd4dfe8b1be08")]
    public partial class IsAbilityActiveCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<GameTag> AbilityTag;

        private AbilityProcessor m_processor;

        public override void OnStart()
        {
            BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor);
        }

        public override bool IsTrue()
        {
            GameTag abilityTag = AbilityTag != null ? AbilityTag.Value : default;
            return m_processor != null && !abilityTag.IsEmpty && m_processor.IsActive(abilityTag);
        }

        public override void OnEnd()
        {
            m_processor = null;
        }
    }

    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Ability Active (String)",
        description: "Checks whether the ability identified by tag string is currently active.",
        story: "Ability [AbilityTagName] is active on [Target]",
        category: "Condition/Sizzle/Ability",
        id: "ea3e443bc70647ff93158c9953d04509")]
    public partial class IsAbilityActiveByStringCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<string> AbilityTagName = new BlackboardVariable<string>(string.Empty);

        private AbilityProcessor m_processor;

        public override void OnStart()
        {
            BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor);
        }

        public override bool IsTrue()
        {
            string abilityTagName = AbilityTagName != null ? AbilityTagName.Value : string.Empty;
            return m_processor != null && !string.IsNullOrWhiteSpace(abilityTagName) && m_processor.IsActive(abilityTagName);
        }

        public override void OnEnd()
        {
            m_processor = null;
        }
    }

    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Any Ability Is Active",
        description: "Checks whether any ability is currently active on the target AbilityProcessor.",
        story: "Any ability is active on [Target]",
        category: "Condition/Sizzle/Ability",
        id: "61883ef44d6e4b129f90d47c8b14660a")]
    public partial class AnyAbilityIsActiveCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        private AbilityProcessor m_processor;

        public override void OnStart()
        {
            BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor);
        }

        public override bool IsTrue()
        {
            return m_processor != null && m_processor.AnyAbilityIsActive;
        }

        public override void OnEnd()
        {
            m_processor = null;
        }
    }
}