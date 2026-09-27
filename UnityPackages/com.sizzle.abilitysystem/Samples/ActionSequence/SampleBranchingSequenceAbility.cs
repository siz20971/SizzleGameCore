using Sizzle.AbilitySystem.Actions;
using UnityEngine;

namespace Sizzle.AbilitySystem.Samples
{
    /// <summary>
    /// 단일 어빌리티 내에서 다중 ActionSequence를 정의하여
    /// 초기 시퀀스 실행 후 조건에 따라 A 또는 B 시퀀스로 분기하는 예제 어빌리티입니다.
    /// 인스펙터나 플로우 에디터 창에서 탭을 전환하며 각 분기 시퀀스를 개별적으로 편집할 수 있습니다.
    /// </summary>
    [CreateAbilityAssetMenu("Samples/Branching Sequence Ability")]
    public class SampleBranchingSequenceAbility : Ability<SampleBranchingSequenceAbility.RuntimeContext>
    {
        public class RuntimeContext : AbilityRuntimeContext
        {
            public ActionSequence.Instance ActiveSequenceInstance;
            public bool DidBranchConditionPass;

            protected override void OnReset()
            {
                base.OnReset();
                ActiveSequenceInstance = null;
                DidBranchConditionPass = false;
            }
        }

        [Header("1. 초기 공통 선행 시퀀스")]
        [Tooltip("어빌리티 발동 시 가장 먼저 실행되는 선행 액션 시퀀스")]
        [SerializeField] private ActionSequence m_initialSequence = new ActionSequence();

        [Header("2. 조건부 분기 시퀀스")]
        [Tooltip("조건 성공 시(예: 히트 성공, 카운터 성공 등) 실행되는 시퀀스")]
        [SerializeField] private ActionSequence m_successBranch = new ActionSequence();

        [Tooltip("조건 실패 시(예: 빗나감, 방어됨 등) 실행되는 시퀀스")]
        [SerializeField] private ActionSequence m_failureBranch = new ActionSequence();

        public ActionSequence InitialSequence => m_initialSequence;
        public ActionSequence SuccessBranch => m_successBranch;
        public ActionSequence FailureBranch => m_failureBranch;

        protected override void OnActivate(RuntimeContext context, AbilityActivatePayload payload)
        {
            // 1단계: 선행 시퀀스 실행
            PlayInitialSequence(context);
        }

        private void PlayInitialSequence(RuntimeContext context)
        {
            context.ActiveSequenceInstance = m_initialSequence.CreateInstance(context);
            context.ActiveSequenceInstance.OnCompleted += () => OnInitialSequenceCompleted(context);
            context.ActiveSequenceInstance.OnCanceled += () => context.RequestCancel();
            context.ActiveSequenceInstance.Start();
        }

        private void OnInitialSequenceCompleted(RuntimeContext context)
        {
            // 예시: 50% 확률 또는 특정 게임플레이 조건 체크 (실제 게임에서는 HitCount, Tag 등을 검사)
            context.DidBranchConditionPass = Random.value > 0.5f;

            ActionSequence selectedBranch = context.DidBranchConditionPass ? m_successBranch : m_failureBranch;

            if (selectedBranch != null && selectedBranch.ActionCount > 0)
            {
                context.ActiveSequenceInstance = selectedBranch.CreateInstance(context);
                context.ActiveSequenceInstance.OnCompleted += () => context.RequestComplete();
                context.ActiveSequenceInstance.OnCanceled += () => context.RequestCancel();
                context.ActiveSequenceInstance.Start();
            }
            else
            {
                context.RequestComplete();
            }
        }

        protected override void OnUpdateTick(float deltaTime, RuntimeContext context)
        {
            context.ActiveSequenceInstance?.Update(deltaTime);
        }

        protected override void OnDeactivate(AbilityEndReason endReason, RuntimeContext context)
        {
            context.ActiveSequenceInstance?.Cancel();
            context.ActiveSequenceInstance = null;
        }
    }
}
