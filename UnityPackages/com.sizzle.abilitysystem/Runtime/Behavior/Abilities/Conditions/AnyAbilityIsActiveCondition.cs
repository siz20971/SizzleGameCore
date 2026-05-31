using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Sizzle.AbilitySystem.Behavior
{
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
