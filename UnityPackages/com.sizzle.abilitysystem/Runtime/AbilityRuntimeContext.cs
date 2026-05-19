using System;
using UnityEngine;

namespace Sizzle.AbilitySystem
{
    /// <summary>
    /// 어빌리티 실행 중 발생 / 변경되는 정보들을 담는 컨텍스트 클래스입니다.
    /// </summary>
    public abstract class AbilityRuntimeContext : IDisposable
    {
        // 최초 초기화 시점에 할당된 후 변경되지 않는 정보들
        public GameObject GameObject { get; private set; }
        public Ability Ability { get; private set; }
        public AbilityProcessor Processor { get; private set; }


        // 어빌리티 실행 중 변경되는 정보들
        public bool IsActive { get; private set; }
        public float ElapsedTime { get; set; }
        public float ActivatedTime { get; private set; }
        public AbilityEndReason PendingEndReason { get; private set; } = AbilityEndReason.None;

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
            PendingEndReason = AbilityEndReason.None;
            ElapsedTime = 0f;
            ActivatedTime = 0f;

            OnReset();
        }

        /// <summary> 어빌리티가 실행되기 직전, 종료 직후 호출됩니다. </summary>
        protected abstract void OnReset();

        // 어빌리티가 실행되었을대 호출됨
        internal void ProcessActivate()
        {
            IsActive = true;
            ActivatedTime = Time.time;
            OnActivated();   // ← 추가: 최초 Activate에서도 훅 호출
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
            OnActivated();   // ← OnProcessActivate → OnActivated 로 통합
            OnReactivated(); // ← 추가: Reactivate 전용 훅
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

        // 어빌리티가 종료되었을때 호출됨


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

        public virtual void Dispose() { }
    }
}