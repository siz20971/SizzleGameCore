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
        name: "Activate Ability",
        description: "Activates an ability by tag string on the target AbilityProcessor and optionally waits until it finishes.",
        story: "Activate ability [AbilityTag] on [Target]",
        category: "Action/Sizzle/Ability",
        id: "2b0ebd4ef8db4f3e8b8613ecb079c001")]
    public partial class ActivateAbilityAction : Action
    {
        [Tooltip("AbilityProcessor를 찾을 대상 오브젝트입니다. 비어 있으면 현재 Behavior Graph의 GameObject를 사용합니다.")]
        [SerializeReference] public BlackboardVariable<GameObject> Target;

        [Tooltip("실행할 어빌리티의 MainTag 문자열입니다.")]
        [SerializeReference] public BlackboardVariable<string> AbilityTag = new BlackboardVariable<string>(string.Empty);

        [Tooltip("실행에 성공한 경우 어빌리티가 종료될 때까지 대기할지 여부입니다.")]
        [SerializeReference] public BlackboardVariable<bool> WaitForEnd = new BlackboardVariable<bool>(false);

        [Tooltip("실행 결과를 기록할 선택적 블랙보드 변수입니다.")]
        [SerializeReference] public BlackboardVariable<AbilityActivateResult> Result;

        private AbilityProcessor m_processor;
        private AbilityRuntimeContext m_context;

        protected override Status OnStart()
        {
            if (!BehaviorAbilityUtility.TryResolveProcessor(Target, GameObject, out m_processor))
            {
                LogFailure("AbilityProcessor or IAbilitySystemComponent was not found on the target GameObject.");
                return Status.Failure;
            }

            GameTag abilityTag = BehaviorAbilityUtility.GetGameTagValue(AbilityTag);
            if (abilityTag.IsEmpty)
            {
                LogFailure("AbilityTag is empty.");
                return Status.Failure;
            }

            AbilityActivateResult result = m_processor.TryActivateAbility(abilityTag);
            BehaviorAbilityUtility.SetBlackboardValue(Result, result);

            if (result != AbilityActivateResult.Success)
                return Status.Failure;

            if (!BehaviorAbilityUtility.GetBoolValue(WaitForEnd))
                return Status.Success;

            m_context = m_processor.GetAbilityContext(abilityTag);
            return m_context != null && m_context.State.IsActive ? Status.Running : Status.Success;
        }

        protected override Status OnUpdate()
        {
            return m_context == null || !m_context.State.IsActive ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            m_context = null;
            m_processor = null;
        }
    }
}