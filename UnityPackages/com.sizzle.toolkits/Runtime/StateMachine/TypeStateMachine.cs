using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.StateMachine
{
    // ------------------------------------------------------------
    // 타입 기반 StateMachine — 클래스 타입으로 상태를 식별
    // RequestStateChange<BossAState_PhaseTwo>() 형태로 전환
    // ------------------------------------------------------------

    /// <summary>
    /// C# 타입 기반 유한 상태 머신의 기본 조작 인터페이스입니다.
    /// </summary>
    public interface ITypeStateMachine
    {
        /// <summary> 현재 활성 상태의 이름 </summary>
        string CurrentStateName { get; }

        /// <summary> 현재 활성 상태의 C# Type </summary>
        Type CurrentStateType { get; }

        /// <summary> 등록된 모든 상태를 초기화하고 별칭 맵을 구축합니다. </summary>
        void InitializeFSM();

        /// <summary> 시작 상태를 지정합니다. </summary>
        void SetInitialState(Type initialStateType);

        /// <summary> 등록된 특정 상태 인스턴스를 가져옵니다. </summary>
        ITypeState GetState<TState>() where TState : ITypeState;

        /// <summary> 다음 상태 타입으로의 전이를 요청합니다. </summary>
        void RequestStateChange(Type nextStateType);

        /// <summary> 프레임 업데이트 </summary>
        void UpdateTick(float deltaTime);

        /// <summary> 물리 업데이트 </summary>
        void FixedUpdateTick(float deltaTime);
    }

    /// <summary>
    /// C# 클래스 타입을 상태 키로 사용하는 강력한 유한 상태 머신(FSM) 구현체입니다.
    /// <para>제네릭 상태 전이(RequestStateChange&lt;T&gt;), 상속 체인 기반 베이스 타입 다형성 별칭 매핑(Alias Map)을 지원합니다.</para>
    /// </summary>
    /// <typeparam name="TContext">상태들에 주입될 공유 컨텍스트/소유자 객체 타입</typeparam>
    public class TypeStateMachine<TContext> : ITypeStateMachine, IDisposable
    {
        /// <summary> 현재 활성화된 상태의 이름입니다. </summary>
        public string CurrentStateName => m_currentState?.StateName;

        /// <summary> 현재 활성화된 상태의 C# Type입니다. </summary>
        public Type CurrentStateType => m_currentState?.GetType();

        /// <summary> 상태 변경 시 발생하는 이벤트 델리게이트 </summary>
        public delegate void StateChangedHandler(Type fromState, Type toState);

        /// <summary> 상태가 성공적으로 전환되었을 때 발생하는 이벤트입니다. </summary>
        public event StateChangedHandler OnStateChanged;

        private readonly Dictionary<Type, TypeStateBase<TContext>> m_states
            = new Dictionary<Type, TypeStateBase<TContext>>();

        // 베이스 타입 → 등록된 서브타입. InitializeFSM 시점에 완전히 구축됩니다.
        // 예: 특정 기본 상태 타입을 캐릭터별 대응 상태 타입으로 치환
        private readonly Dictionary<Type, Type> m_aliasMap
            = new Dictionary<Type, Type>();

        private bool m_initialized = false;
        private TypeStateBase<TContext> m_currentState;
        protected readonly TContext m_context;

        /// <summary>
        /// 공유 컨텍스트를 주입받아 타입 상태 머신을 생성합니다.
        /// </summary>
        public TypeStateMachine(TContext context)
        {
            m_context = context;
        }

        /// <summary>
        /// 상태 머신에 사용할 상태 인스턴스들을 등록합니다. (InitializeFSM 호출 전에만 가능)
        /// </summary>
        public void AddStates(params TypeStateBase<TContext>[] states)
        {
            if (m_initialized)
                throw new InvalidOperationException("Cannot add states after the state machine has been initialized.");

            foreach (var state in states)
            {
                Type stateType = state.GetType();
                if (m_states.ContainsKey(stateType))
                    throw new InvalidOperationException($"State of type {stateType.Name} has already been added.");
                m_states[stateType] = state;
            }
        }

        /// <summary>
        /// 별칭 맵을 구축하고 등록된 모든 상태에 컨텍스트 및 상태 머신을 주입하여 초기화합니다.
        /// </summary>
        public void InitializeFSM()
        {
            if (m_initialized) return;
            BuildAliasMap();
            foreach (var state in m_states.Values)
                state.Initialize(m_context, this);
            m_initialized = true;
        }

        /// <summary>
        /// 등록된 모든 상태의 상속 체인을 탐색하여 베이스 타입 별칭 맵을 구축합니다.
        /// 추상 타입에 도달하면 탐색을 중단합니다.
        /// (예: BossHit → EnemyHit은 구체 타입이므로 별칭 등록. HitStateBase는 추상이므로 중단)
        /// 같은 베이스 타입을 상속하는 구체 타입이 2개 이상 등록되면 즉시 예외를 던집니다.
        /// </summary>
        private void BuildAliasMap()
        {
            var stopType = typeof(TypeStateBase<TContext>);
            foreach (var stateType in m_states.Keys)
            {
                var baseType = stateType.BaseType;
                while (baseType != null && baseType != stopType && !baseType.IsAbstract)
                {
                    if (m_states.ContainsKey(baseType))
                        break; // 베이스 타입 자체가 등록되어 있으면 중단

                    if (m_aliasMap.TryGetValue(baseType, out var existing))
                        throw new InvalidOperationException(
                            $"Ambiguous state role: '{baseType.Name}'을 '{existing.Name}'과 '{stateType.Name}'이 동시에 상속합니다. " +
                            $"전이 시 정확한 타입을 사용하거나 하나만 등록하세요.");

                    m_aliasMap[baseType] = stateType;
                    baseType = baseType.BaseType;
                }
            }
        }

        /// <summary> 제네릭 상태 타입을 지정하여 시작 상태를 설정합니다. </summary>
        public void SetInitialState<TState>() where TState : TypeStateBase<TContext>
            => SetInitialState(typeof(TState));

        /// <summary> 시작 상태를 지정하고 해당 상태의 OnEnter를 호출합니다. </summary>
        public void SetInitialState(Type stateType)
        {
            if (!m_initialized)
                throw new InvalidOperationException("State machine must be initialized before setting the initial state.");
            if (!m_states.TryGetValue(stateType, out var initialState))
            {
                if (!m_aliasMap.TryGetValue(stateType, out var aliasedType) ||
                    !m_states.TryGetValue(aliasedType, out initialState))
                    throw new KeyNotFoundException($"State of type {stateType.Name} not found in the state machine.");
                stateType = aliasedType;
            }
            m_currentState = initialState;
            m_currentState.OnEnter(stateType);
        }

        /// <summary> 현재 활성화된 상태가 TState 타입인지 확인합니다. </summary>
        public bool IsCurrentState<TState>() where TState : TypeStateBase<TContext>
            => m_currentState is TState;

        /// <summary> 제네릭 상태 타입을 지정하여 상태 전이를 요청합니다. </summary>
        public void RequestStateChange<TState>() where TState : TypeStateBase<TContext>
            => ChangeState(typeof(TState));

        /// <summary> 상태 타입을 지정하여 상태 전이를 요청합니다. </summary>
        public void RequestStateChange(Type nextStateType)
            => ChangeState(nextStateType);

        /// <summary> 등록된 상태 인스턴스를 타입으로 검색하여 반환합니다. </summary>
        public ITypeState GetState<TState>() where TState : ITypeState
        {
            if (!m_states.TryGetValue(typeof(TState), out var state))
                return null;
            return state;
        }

        private void ChangeState(Type nextStateType)
        {
            if (!m_initialized)
                throw new InvalidOperationException("State machine must be initialized before changing states.");
            if (!m_states.TryGetValue(nextStateType, out var nextState))
            {
                if (!m_aliasMap.TryGetValue(nextStateType, out var aliasedType) ||
                    !m_states.TryGetValue(aliasedType, out nextState))
                    throw new KeyNotFoundException($"State of type {nextStateType.Name} not found in the state machine.");
                nextStateType = aliasedType;
            }
            if (!m_currentState.CanTransitionTo(nextStateType))
                throw new InvalidOperationException($"Cannot transition from {m_currentState.GetType().Name} to {nextStateType.Name}.");

            Type prevStateType = m_currentState.GetType();
            m_currentState.OnExit(nextStateType);
            m_currentState = nextState;
            m_currentState.OnEnter(prevStateType);

            OnStateChanged?.Invoke(prevStateType, nextStateType);
        }

        /// <summary> 현재 활성 상태의 Tick(deltaTime)을 호출합니다. </summary>
        public void UpdateTick(float deltaTime)
        {
            if (!m_initialized) return;
            m_currentState.Tick(deltaTime);
        }

        /// <summary> 현재 활성 상태의 FixedTick(deltaTime)을 호출합니다. </summary>
        public void FixedUpdateTick(float deltaTime)
        {
            if (!m_initialized) return;
            m_currentState.FixedTick(deltaTime);
        }

        /// <summary> 등록된 모든 상태의 Dispose()를 호출하여 리소스를 해제합니다. </summary>
        public void Dispose()
        {
            foreach (var state in m_states.Values)
                state.Dispose();
        }
    }
}
