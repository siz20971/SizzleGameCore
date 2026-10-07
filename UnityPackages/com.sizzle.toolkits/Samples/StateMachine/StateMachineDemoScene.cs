using Sizzle.Toolkits;
using Sizzle.Toolkits.StateMachine;
using UnityEngine;

namespace Sizzle.Toolkits.Demo.StateMachine
{
    /// <summary>
    /// <see cref="EnumStateMachine{TState, TContext}"/>의 동작을 검증하기 위한 데모 씬 컨트롤러입니다.
    /// </summary>
    public class StateMachineDemoScene : MonoBehaviour
    {
        [SerializeField]
        private SampleContext m_context;

        void Start()
        {
            m_stateMachine = new EnumStateMachine<StateType, SampleContext>(m_context);

            m_stateMachine.AddStates(
                new WaitPressAState(),
                new IntValueChangeState()
            );

            m_stateMachine.InitializeFSM();
            m_stateMachine.SetInitialState(StateType.WaitPressA);
        }

        void Update()
        {
            m_stateMachine.UpdateTick(Time.deltaTime);
        }

        private EnumStateMachine<StateType, SampleContext> m_stateMachine;

        public enum StateType
        {
            WaitPressA,
            IntValueChangeState
        }

        [System.Serializable]
        public class SampleContext
        {
            public int IntValue;
        }

        public class IntValueChangeState : EnumStateBase<StateType, SampleContext>
        {
            public override StateType StateType => StateType.IntValueChangeState;
            private int m_previousIntValue;

            public override void OnEnter(StateType prevState)
            {
                Debug.Log("Entered IntValueChangeState");
                m_previousIntValue = m_context.IntValue;
            }

            public override void OnExit(StateType nextState)
            {
                Debug.Log("Exiting IntValueChangeState");
            }

            public override void Tick(float deltaTime)
            {
                if (m_context.IntValue != m_previousIntValue) // Check if the value has changed
                {
                    Debug.Log("IntValue changed, transitioning to WaitPressAState");
                    RequestStateChange(StateType.WaitPressA);
                }
            }
        }

        public class WaitPressAState : EnumStateBase<StateType, SampleContext>
        {
            public override StateType StateType => StateType.WaitPressA;

            public override void OnEnter(StateType prevState)
            {
                Debug.Log("Entered WaitPressAState");
            }

            public override void OnExit(StateType nextState)
            {
                Debug.Log("Exiting WaitPressAState");
            }

            public override void Tick(float deltaTime)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.A))
                {
                    Debug.Log("A key pressed, transitioning to IntValueChangeState");
                    RequestStateChange(StateType.IntValueChangeState);
                }
            }
        }
    }
}
