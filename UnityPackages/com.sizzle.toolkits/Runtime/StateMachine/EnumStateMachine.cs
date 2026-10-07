using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.StateMachine
{
    /// <summary>
    /// 열거형 기반 유한 상태 머신의 기본 조작 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TStateType">상태 열거형 타입</typeparam>
    public interface IStateMachine<TStateType> where TStateType : Enum
    {
        /// <summary> 현재 활성화된 상태의 열거형 값 </summary>
        TStateType CurrentStateType { get; }

        /// <summary> 등록된 상태들을 초기화합니다. </summary>
        void InitializeFSM();

        /// <summary> 시작 상태를 지정하고 OnEnter를 호출합니다. </summary>
        void SetInitialState(TStateType initialStateType);

        /// <summary> 다음 상태로의 전이를 요청합니다. </summary>
        void RequestStateChange(TStateType nextStateType);

        /// <summary> 프레임 단위 업데이트를 수행합니다. </summary>
        void UpdateTick(float deltaTime);

        /// <summary> 물리 단위 고정 프레임 업데이트를 수행합니다. </summary>
        void FixedUpdateTick(float deltaTime);
    }

    /// <summary>
    /// 열거형(Enum) 값을 상태 키로 사용하는 범용 유한 상태 머신(FSM) 구현체입니다.
    /// <para>상태 진입/퇴출 라이프사이클, 전이 조건 검증, 상태 변경 이벤트 전파를 지원합니다.</para>
    /// </summary>
    /// <typeparam name="TStateType">상태 식별에 사용할 열거형 타입</typeparam>
    /// <typeparam name="TContext">상태들에 주입될 공유 컨텍스트/소유자 객체 타입</typeparam>
    public class EnumStateMachine<TStateType, TContext> : IStateMachine<TStateType>, IDisposable
        where TStateType : Enum
    {
        /// <summary> 현재 활성화된 상태의 열거형 키 값입니다. </summary>
        public TStateType CurrentStateType => m_currentState != null ? m_currentState.StateType : default;

        /// <summary> 상태 변경 시 발생하는 이벤트 델리게이트 </summary>
        public delegate void StateChangedHandler(TStateType fromState, TStateType toState);

        private readonly Dictionary<TStateType, EnumStateBase<TStateType, TContext>> m_states
            = new Dictionary<TStateType, EnumStateBase<TStateType, TContext>>();

        private bool m_initialized = false;
        private EnumStateBase<TStateType, TContext> m_currentState;
        private readonly TContext m_context;

        /// <summary> 상태가 성공적으로 전환되었을 때 발생하는 이벤트입니다. </summary>
        public event StateChangedHandler OnStateChanged;

        /// <summary>
        /// 공유 컨텍스트를 주입받아 열거형 상태 머신을 생성합니다.
        /// </summary>
        public EnumStateMachine(TContext context)
        {
            m_context = context;
        }

        /// <summary>
        /// 상태 머신에 사용할 상태 인스턴스들을 등록합니다. (InitializeFSM 호출 전에만 가능)
        /// </summary>
        public void AddStates(params EnumStateBase<TStateType, TContext>[] states)
        {
            if (m_initialized)
            {
                throw new InvalidOperationException("Cannot add states after the state machine has been initialized.");
            }

            foreach (var state in states)
            {
                if (m_states.ContainsKey(state.StateType))
                    throw new InvalidOperationException($"State of type {state.StateType} has already been added.");
                m_states[state.StateType] = state;
            }
        }

        /// <summary>
        /// 등록된 모든 상태에 컨텍스트와 상태 머신 참조를 주입하여 초기화합니다.
        /// </summary>
        public void InitializeFSM()
        {
            if (m_initialized)
                return;

            foreach (var state in m_states.Values)
                state.Initialize(m_context, this);

            m_initialized = true;
        }

        /// <summary>
        /// 상태 머신의 시작 상태를 지정하고 해당 상태의 OnEnter를 호출합니다.
        /// </summary>
        public void SetInitialState(TStateType initialStateType)
        {
            if (!m_initialized)
                throw new InvalidOperationException("State machine must be initialized before setting the initial state.");
            if (!m_states.TryGetValue(initialStateType, out var initialState))
                throw new KeyNotFoundException($"State of type {initialStateType} not found in the state machine.");
            m_currentState = initialState;
            m_currentState.OnEnter(initialStateType);
        }

        protected void ChangeState(TStateType nextStateType)
        {
            if (!m_initialized)
                throw new InvalidOperationException("State machine must be initialized before changing states.");

            if (!m_states.TryGetValue(nextStateType, out var nextState))
                throw new KeyNotFoundException($"State of type {nextStateType} not found in the state machine.");

            if (!m_currentState.CanTransitionTo(nextStateType))
                throw new InvalidOperationException($"Cannot transition from state {m_currentState.StateType} to state {nextStateType}.");

            TStateType prevStateType = m_currentState.StateType;

            m_currentState.OnExit(nextStateType);
            m_currentState = nextState;
            m_currentState.OnEnter(m_currentState.StateType);
            
            OnStateChanged?.Invoke(prevStateType, nextStateType);
        }

        /// <summary>
        /// 다음 상태로의 전환을 요청합니다. 현재 상태의 CanTransitionTo 검증을 거친 후 OnExit -> OnEnter 순으로 전이됩니다.
        /// </summary>
        public void RequestStateChange(TStateType nextStateType)
        {
            ChangeState(nextStateType);
        }

        /// <summary>
        /// 현재 활성 상태의 Tick(deltaTime)을 호출합니다.
        /// </summary>
        public void UpdateTick(float deltaTime)
        {
            if (!m_initialized)
                return;

            m_currentState.Tick(deltaTime);
        }

        /// <summary>
        /// 현재 활성 상태의 FixedTick(deltaTime)을 호출합니다.
        /// </summary>
        public void FixedUpdateTick(float deltaTime)
        {
            if (!m_initialized)
                return;

            m_currentState.FixedTick(deltaTime);
        }

        /// <summary>
        /// 등록된 모든 상태의 Dispose()를 호출하여 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            foreach (var state in m_states.Values)
                state.Dispose();
        }
    }
}
