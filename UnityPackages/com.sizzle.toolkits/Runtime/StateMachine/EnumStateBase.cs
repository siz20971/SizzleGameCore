using System;

namespace Sizzle.Toolkits.StateMachine
{
    /// <summary>
    /// 열거형(Enum)으로 식별되는 FSM 상태의 공통 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TStateType">상태 식별에 사용할 Enum 타입</typeparam>
    public interface IEnumState<TStateType> : IDisposable where TStateType : Enum
    {
        /// <summary> 이 상태가 나타내는 Enum 키 값 </summary>
        TStateType StateType { get; }

        /// <summary> 상태에 진입할 때 1회 호출됩니다. </summary>
        /// <param name="prevState">이전 상태</param>
        void OnEnter(TStateType prevState);

        /// <summary> 상태를 벗어날 때 1회 호출됩니다. </summary>
        /// <param name="nextState">전이될 다음 상태</param>
        void OnExit(TStateType nextState);

        /// <summary> 프레임 업데이트 시 매 프레임 호출됩니다. </summary>
        /// <param name="deltaTime">프레임 경과 시간</param>
        void Tick(float deltaTime);

        /// <summary> 고정 프레임 물리 업데이트 시 호출됩니다. </summary>
        /// <param name="deltaTime">물리 프레임 경과 시간</param>
        void FixedTick(float deltaTime);

        /// <summary> 지정한 다음 상태로 전이 가능한지 유효성을 검사합니다. </summary>
        /// <param name="nextState">전이하려는 다음 상태</param>
        /// <returns>전이 허용 여부</returns>
        bool CanTransitionTo(TStateType nextState);
    }

    /// <summary>
    /// 열거형(Enum) 기반 상태 머신(EnumStateMachine)의 각 상태를 구현하기 위한 추상 기본 클래스입니다.
    /// </summary>
    /// <typeparam name="TStateType">상태 식별 Enum 타입</typeparam>
    /// <typeparam name="TContext">상태 머신 소유자 또는 공유 컨텍스트 타입</typeparam>
    public abstract class EnumStateBase<TStateType, TContext> : IEnumState<TStateType> where TStateType : Enum
    {
        /// <summary> 이 상태가 나타내는 고유 Enum 키 값 </summary>
        public abstract TStateType StateType { get; }

        /// <summary> 소유자 객체 또는 공유 컨텍스트 </summary>
        protected TContext m_context { get; private set; }
        /// <summary> 이 상태가 소속된 상위 상태 머신 인스턴스 </summary>
        protected EnumStateMachine<TStateType, TContext> m_stateMachine { get; private set; }

        /// <summary> 상태 머신에 다음 상태로의 전이를 요청합니다. </summary>
        protected void RequestStateChange(TStateType nextStateType)
        {
            m_stateMachine.RequestStateChange(nextStateType);
        }

        /// <summary> 상태 머신 및 컨텍스트를 바인딩하여 상태를 초기화합니다. </summary>
        public void Initialize(TContext context, EnumStateMachine<TStateType, TContext> stateMachine)
        {
            m_context = context;
            m_stateMachine = stateMachine;
            OnInitialize();
        }

        /// <summary> 초기화 시 1회 호출되는 가상 메서드입니다. </summary>
        protected virtual void OnInitialize() { }

        /// <summary> 상태에 진입할 때 호출됩니다. </summary>
        public abstract void OnEnter(TStateType prevState);
        /// <summary> 상태에서 벗어날 때 호출됩니다. </summary>
        public abstract void OnExit(TStateType nextState);
        /// <summary> 매 프레임 업데이트 시 호출됩니다. </summary>
        public abstract void Tick(float deltaTime);

        /// <summary> 물리 업데이트(FixedUpdate) 시 호출됩니다. </summary>
        public virtual void FixedTick(float deltaTime)
        {
            // Override this method if fixed update logic is required.
        }

        /// <summary> 다음 상태로의 전이 가능 여부를 검사합니다. (기본 true) </summary>
        public virtual bool CanTransitionTo(TStateType nextState)
        {
            // Override this method to define custom transition rules.
            return true;
        }

        /// <summary> 상태 정리 및 리소스 해제 </summary>
        public virtual void Dispose() { }
    }
}
