using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 단위 액션(AbilityAction)들의 순차 실행 파이프라인을 캡슐화한 직렬화 가능 모듈입니다.
    /// 특정 어빌리티 클래스를 상속받을 필요 없이, 어떤 어빌리티나 컴포넌트든 필드로 포함하여 시퀀스 실행을 제어할 수 있습니다.
    /// </summary>
    [Serializable]
    public class ActionSequence
    {
        [SerializeReference]
        public List<AbilityAction> Actions = new List<AbilityAction>();

        public int ActionCount => Actions != null ? Actions.Count : 0;

        /// <summary>
        /// 런타임 실행 인스턴스를 생성합니다.
        /// ScriptableObject 원본 데이터 오염을 방지하기 위해 모든 액션을 복제(Clone)하여 독립 구동합니다.
        /// </summary>
        public Instance CreateInstance(AbilityRuntimeContext context)
        {
            return new Instance(this, context);
        }

        /// <summary>
        /// 런타임 실행을 관리하는 독립 인스턴스 클래스입니다.
        /// 동일한 ActionSequence 데이터를 여러 캐릭터나 다중 발동 상황에서 안전하게 동시 구동합니다.
        /// </summary>
        public class Instance
        {
            public AbilityRuntimeContext Context { get; private set; }
            public int CurrentIndex { get; private set; } = -1;
            public AbilityAction CurrentAction { get; private set; }
            public List<AbilityAction> InstancedActions { get; private set; } = new List<AbilityAction>();

            public bool IsRunning { get; private set; }
            public bool IsFinished { get; private set; }
            public bool IsCanceled { get; private set; }

            public event Action OnCompleted;
            public event Action OnCanceled;

            private Action<AbilityAction> m_onActionCompletedHandler;
            private Action<AbilityAction> m_onActionCanceledHandler;

            public Instance(ActionSequence template, AbilityRuntimeContext context)
            {
                Context = context;
                InstancedActions.Clear();

                if (template?.Actions != null)
                {
                    foreach (var action in template.Actions)
                    {
                        if (action != null)
                        {
                            InstancedActions.Add(action.Clone());
                        }
                    }
                }

                m_onActionCompletedHandler = OnStepActionCompleted;
                m_onActionCanceledHandler = OnStepActionCanceled;
            }

            public void Start()
            {
                if (IsRunning || IsFinished) return;

                IsRunning = true;
                CurrentIndex = -1;

                if (InstancedActions.Count > 0)
                {
                    AdvanceToNextAction();
                }
                else
                {
                    CompleteSequence();
                }
            }

            public void Update(float deltaTime)
            {
                if (!IsRunning || IsFinished) return;

                if (CurrentAction != null && !CurrentAction.IsFinished)
                {
                    CurrentAction.Update(deltaTime);
                }
            }

            public void Cancel()
            {
                if (!IsRunning || IsFinished) return;

                IsRunning = false;
                IsCanceled = true;
                IsFinished = true;

                if (CurrentAction != null)
                {
                    CurrentAction.OnCompleted -= m_onActionCompletedHandler;
                    CurrentAction.OnCanceled -= m_onActionCanceledHandler;
                    CurrentAction.Cancel();
                    CurrentAction = null;
                }

                OnCanceled?.Invoke();
            }

            private void AdvanceToNextAction()
            {
                if (CurrentAction != null)
                {
                    CurrentAction.OnCompleted -= m_onActionCompletedHandler;
                    CurrentAction.OnCanceled -= m_onActionCanceledHandler;
                }

                CurrentIndex++;

                if (CurrentIndex >= InstancedActions.Count)
                {
                    CompleteSequence();
                    return;
                }

                CurrentAction = InstancedActions[CurrentIndex];
                CurrentAction.OnCompleted += m_onActionCompletedHandler;
                CurrentAction.OnCanceled += m_onActionCanceledHandler;
                CurrentAction.Start(Context);
            }

            private void OnStepActionCompleted(AbilityAction action)
            {
                AdvanceToNextAction();
            }

            private void OnStepActionCanceled(AbilityAction action)
            {
                Cancel();
            }

            private void CompleteSequence()
            {
                IsRunning = false;
                IsFinished = true;
                CurrentAction = null;
                OnCompleted?.Invoke();
            }
        }
    }
}
