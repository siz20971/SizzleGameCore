using System;
using UnityEngine;

namespace Sizzle.AbilitySystem
{
    /// <summary>
    /// 어빌리티 실행 중 발생 / 변경되는 정보들을 담는 컨텍스트 클래스입니다.
    /// </summary>
    public abstract class AbilityRuntimeContext : IDisposable
    {
        public GameObject GameObject { get; internal set; }
        public Ability Ability { get; internal set; }
        public AbilityProcessor Processor { get; internal set; }

        public bool IsActive { get; internal set; }
        public float ElapsedTime { get; internal set; }
        public float ActivatedTime { get; internal set; }
        public AbilityEndReason PendingEndReason { get; internal set; } = AbilityEndReason.None;

        internal bool Initialize(Ability ability, AbilityProcessor processor)
        {
            Ability = ability;
            Processor = processor;

            if (Ability == null || Processor == null)
                return false;

            GameObject = processor.gameObject;

            return OnInitialize();
        }

        protected virtual bool OnInitialize() { return true; }

        /// <summary> 컨텍스트 생성 및 어빌리티가 종료될때 초기화됩니다.</summary>
        public void Reset()
        {
            IsActive = false;
            ElapsedTime = 0f;
            ActivatedTime = 0f;
            PendingEndReason = AbilityEndReason.None;

            OnReset();
        }

        /// <summary> 어빌리티가 실행되기 직전, 종료 직후 호출됩니다. </summary>
        protected virtual void OnReset() { }

        // 어빌리티가 실행되었을대 호출됨
        internal void ProcessActivate()
        {
            IsActive = true;
            ActivatedTime = Time.time;
            OnActivated();
        }

        /// <summary>
        /// 어빌리티가 활성화될 때(최초 Activate 및 Reactivate 모두) 호출됩니다.
        /// 재진입 가능하도록 멱등(idempotent)하게 작성되어야 합니다.
        /// </summary>
        protected virtual void OnActivated() { }

        /// <summary>
        /// ReactivationPolicy.Reactivate 정책으로 재실행될 때 호출됩니다.
        /// IsActive 상태를 유지한 채 타이밍 정보만 초기화합니다.
        /// </summary>
        internal void ProcessReactivate()
        {
            ElapsedTime = 0f;
            ActivatedTime = Time.time;
            PendingEndReason = AbilityEndReason.None;
            OnActivated();
            OnReactivated();
        }

        /// <summary>
        /// Reactivate 정책으로 재실행될 때만 추가 호출됩니다.
        /// 일반 Activate 경로에서는 호출되지 않습니다.
        /// </summary>
        protected virtual void OnReactivated() { }

        // 어빌리티가 매 프레임 업데이트될때 호출됨
        internal void ProcessUpdate(float deltaTime)
        {
            ElapsedTime += deltaTime;
            OnUpdate(deltaTime);
        }

        protected virtual void OnUpdate(float deltaTime) { }

        internal void ProcessFixedUpdate(float fixedDeltaTime)
        {
            OnFixedUpdate(fixedDeltaTime);
        }

        protected virtual void OnFixedUpdate(float fixedDeltaTime) { }

        internal void ProcessLateUpdate(float deltaTime)
        {
            OnLateUpdate(deltaTime);
        }

        protected virtual void OnLateUpdate(float deltaTime) { }

        #region Public API for Ability Logic
        public void RequestComplete()
        {
            IsActive = false;
            PendingEndReason = AbilityEndReason.Completed;
        }

        public void RequestCancel()
        {
            IsActive = false;
            PendingEndReason = AbilityEndReason.Canceled;
        }
        #endregion

        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// 사용자 정의 State(상태)를 구조체(Struct) 형태로 관리하는 제네릭 컨텍스트 클래스입니다.
    /// Cache(캐시/참조) 변수는 이 클래스를 상속받는 하위 클래스의 멤버로 선언하고, 
    /// 매번 리셋되어야 하는 변수만 TState 구조체 안에 정의하세요.
    /// </summary>
    /// <typeparam name="TState">
    /// 매 활성화 시 자동으로 0/null 초기화(default 할당)되는 상태 구조체.
    /// [주의] 내부 구조 상 반드시 프로퍼티가 아닌 필드(Field)로 선언해야 합니다.
    /// </typeparam>
    public abstract class AbilityRuntimeContext<TState> : AbilityRuntimeContext
        where TState : struct
    {
        /// <summary>
        /// 매 어빌리티 활성화 시 자동으로 초기화되는 상태 필드.
        /// [주의사항] TState는 구조체이므로 값을 수정하기 위해 반드시 필드로 유지되어야 합니다.
        /// </summary>
        public TState State;

        protected override void OnReset()
        {
            base.OnReset();
            // 구조체는 default를 할당하면 내부의 모든 필드가 자동 초기화됩니다.
            State = default;
        }
    }
}