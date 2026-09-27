using Sizzle.AbilitySystem.Actions;
using UnityEngine;

namespace Sizzle.AbilitySystem
{
    /// <summary>
    /// Sizzle.AbilitySystem 코어에서 제공하는 기본 비제네릭 컴포지션 어빌리티입니다.
    /// 별도의 C# 상속 코드 없이, 인스펙터나 플로우 에디터에서 ActionSequence를 구성하여 바로 에셋으로 생성해 사용합니다.
    /// </summary>
    [CreateAbilityAssetMenu("Action Composition Ability")]
    public class ActionCompositionAbility : Ability<ActionCompositionAbility.RuntimeContext>
    {
        public class RuntimeContext : AbilityRuntimeContext
        {
            public ActionSequence.Instance SequenceInstance;

            protected override void OnReset()
            {
                base.OnReset();
                SequenceInstance = null;
            }
        }

        [Header("Sequence Settings")]
        [SerializeField] private ActionSequence m_sequence = new ActionSequence();

        public ActionSequence Sequence => m_sequence;

        protected override void OnActivate(RuntimeContext context, AbilityActivatePayload payload)
        {
            context.SequenceInstance = m_sequence.CreateInstance(context);
            context.SequenceInstance.OnCompleted += () => context.RequestComplete();
            context.SequenceInstance.OnCanceled += () => context.RequestCancel();
            context.SequenceInstance.Start();
        }

        protected override void OnUpdateTick(float deltaTime, RuntimeContext context)
        {
            context.SequenceInstance?.Update(deltaTime);
        }

        protected override void OnDeactivate(AbilityEndReason endReason, RuntimeContext context)
        {
            context.SequenceInstance?.Cancel();
            context.SequenceInstance = null;
        }
    }
}
