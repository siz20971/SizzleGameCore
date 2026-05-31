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
        description: "Checks whether the ability identified by tag string is currently active.",
        story: "Ability [AbilityTag] is active on [Target]",
        category: "Condition/Sizzle/Ability",
        id: "9de7a624ef144216a8ebd4dfe8b1be08")]
    public partial class IsAbilityActiveCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<string> AbilityTag = new BlackboardVariable<string>(string.Empty);

        private AbilityProcessor m_processor;

        public override void OnStart()
        {
            BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor);
        }

        public override bool IsTrue()
        {
            GameTag abilityTag = BehaviorAbilityUtility.GetGameTagValue(AbilityTag);
            return m_processor != null && !abilityTag.IsEmpty && m_processor.IsActive(abilityTag);
        }

        public override void OnEnd()
        {
            m_processor = null;
        }
    }
}
