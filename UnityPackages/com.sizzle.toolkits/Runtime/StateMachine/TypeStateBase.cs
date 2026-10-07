using System;

namespace Sizzle.Toolkits.StateMachine
{
    /// <summary>
    /// C# 타입(Type)으로 식별되는 FSM 상태의 공통 인터페이스입니다.
    /// </summary>
    public interface ITypeState : IDisposable
    {
        /// <summary> StateName은 기본적으로 캐싱된 GetType().Name을 반환하지만, 필요할 경우 오버라이드 하세요. </summary>
        string StateName { get; }

        /// <summary> 상태에 진입할 때 1회 호출됩니다. </summary>
        /// <param name="prevState">이전 상태의 Type</param>
        void OnEnter(Type prevState);

        /// <summary> 상태에서 벗어날 때 1회 호출됩니다. </summary>
        /// <param name="nextState">전이될 다음 상태의 Type</param>
        void OnExit(Type nextState);

        /// <summary> 프레임 업데이트 시 매 프레임 호출됩니다. </summary>
        /// <param name="deltaTime">프레임 경과 시간</param>
        void Tick(float deltaTime);

        /// <summary> 고정 프레임 물리 업데이트 시 호출됩니다. </summary>
        /// <param name="deltaTime">물리 프레임 경과 시간</param>
        void FixedTick(float deltaTime);

        /// <summary> 지정한 다음 상태 타입으로 전이 가능한지 검사합니다. </summary>
        /// <param name="nextState">전이하려는 다음 상태 타입</param>
        /// <returns>전이 허용 여부</returns>
        bool CanTransitionTo(Type nextState);
    }

    /// <summary>
    /// C# 클래스 타입을 상태 식별자로 사용하는 상태 머신(TypeStateMachine)의 각 상태를 구현하기 위한 추상 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TContext">상태 머신 소유자 또는 공유 컨텍스트 타입</typeparam>
    public abstract class TypeStateBase<TContext> : ITypeState
    {
        private string m_stateName;

        /// <summary> 상태의 디버그/표시 이름 (기본: GetType().Name) </summary>
        public virtual string StateName
        {
            get
            {
                if (m_stateName == null)
                    m_stateName = GetType().Name;
                return m_stateName;
            }
        }

        /// <summary> 소유자 객체 또는 공유 컨텍스트 </summary>
        protected TContext m_context { get; private set; }
        /// <summary> 소속된 상위 TypeStateMachine 인스턴스 </summary>
        protected TypeStateMachine<TContext> m_stateMachine { get; private set; }

        /// <summary> 제네릭 상태 타입을 지정하여 상태 전환을 요청합니다. </summary>
        protected void RequestStateChange<TState>()
            where TState : TypeStateBase<TContext>
            => m_stateMachine.RequestStateChange<TState>();

        internal void Initialize(TContext context, TypeStateMachine<TContext> stateMachine)
        {
            m_context = context;
            m_stateMachine = stateMachine;
            OnInitialize();
        }

        /// <summary> 초기화 시 1회 호출되는 가상 메서드입니다. </summary>
        protected virtual void OnInitialize() { }

        /// <summary> 상태에 진입할 때 호출됩니다. </summary>
        public abstract void OnEnter(Type prevState);
        /// <summary> 상태에서 벗어날 때 호출됩니다. </summary>
        public abstract void OnExit(Type nextState);
        /// <summary> 매 프레임 업데이트 시 호출됩니다. </summary>
        public abstract void Tick(float deltaTime);

        /// <summary> 물리 업데이트(FixedUpdate) 시 호출됩니다. </summary>
        public virtual void FixedTick(float deltaTime) { }

        /// <summary> 다음 상태로의 전이 가능 여부를 검사합니다. (기본 true) </summary>
        public virtual bool CanTransitionTo(Type nextState) => true;

        /// <summary> 상태 정리 및 리소스 해제 </summary>
        public virtual void Dispose() { }
    }
}
