using System;
using UnityEngine;

namespace Sizzle.AbilitySystem
{
    public abstract class AbilityRuntimeState
    {
        public virtual void Reset() { }
    }

    public abstract class AbilityRuntimeCache : IDisposable
    {
        public virtual void Dispose() { }
    }

    public class AbilityRuntimeSharedState : AbilityRuntimeState
    {
        public bool IsActive { get; internal set; }
        public float ElapsedTime { get; internal set; }
        public float ActivatedTime { get; internal set; }
        public AbilityEndReason PendingEndReason { get; internal set; } = AbilityEndReason.None;

        public override void Reset()
        {
            IsActive = false;
            ElapsedTime = 0f;
            ActivatedTime = 0f;
            PendingEndReason = AbilityEndReason.None;
        }
    }

    public class AbilityRuntimeSharedCache : AbilityRuntimeCache
    {
        public GameObject GameObject { get; internal set; }
        public Ability Ability { get; internal set; }
        public AbilityProcessor Processor { get; internal set; }
    }

    /// <summary>
    /// 어빌리티 실행 중 발생 / 변경되는 정보들을 담는 컨텍스트 클래스입니다.
    /// </summary>
    public abstract class AbilityRuntimeContext : IDisposable
    {
        private readonly AbilityRuntimeSharedState m_state = new AbilityRuntimeSharedState();
        private readonly AbilityRuntimeSharedCache m_cache = new AbilityRuntimeSharedCache();

        protected virtual AbilityRuntimeSharedState RuntimeState => m_state;
        protected virtual AbilityRuntimeSharedCache RuntimeCache => m_cache;

        public AbilityRuntimeSharedState State => RuntimeState;
        public AbilityRuntimeSharedCache Cache => RuntimeCache;

        [Obsolete("Use Cache.GameObject instead.")]
        public GameObject GameObject => RuntimeCache.GameObject;

        [Obsolete("Use Cache.Ability instead.")]
        public Ability Ability => RuntimeCache.Ability;

        [Obsolete("Use Cache.Processor instead.")]
        public AbilityProcessor Processor => RuntimeCache.Processor;

        [Obsolete("Use State.IsActive instead.")]
        public bool IsActive => RuntimeState.IsActive;

        [Obsolete("Use State.ElapsedTime instead.")]
        public float ElapsedTime => RuntimeState.ElapsedTime;

        [Obsolete("Use State.ActivatedTime instead.")]
        public float ActivatedTime => RuntimeState.ActivatedTime;

        [Obsolete("Use State.PendingEndReason instead.")]
        public AbilityEndReason PendingEndReason => RuntimeState.PendingEndReason;

        internal bool Initialize(Ability ability, AbilityProcessor processor)
        {
            RuntimeCache.Ability = ability;
            RuntimeCache.Processor = processor;

            if (RuntimeCache.Ability == null || RuntimeCache.Processor == null)
                return false;

            RuntimeCache.GameObject = processor.gameObject;

            return OnInitialize();
        }

        protected virtual bool OnInitialize() { return true; }

        /// <summary> 컨텍스트 생성 및 어빌리티가 종료될때 초기화됩니다.</summary>
        public void Reset()
        {
            RuntimeState.Reset();

            OnReset();
        }

        /// <summary> 어빌리티가 실행되기 직전, 종료 직후 호출됩니다. </summary>
        protected virtual void OnReset() { }

        // 어빌리티가 실행되었을대 호출됨
        internal void ProcessActivate()
        {
            RuntimeState.IsActive = true;
            RuntimeState.ActivatedTime = Time.time;
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
            RuntimeState.ElapsedTime = 0f;
            RuntimeState.ActivatedTime = Time.time;
            RuntimeState.PendingEndReason = AbilityEndReason.None;
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
            RuntimeState.ElapsedTime += deltaTime;
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
            RuntimeState.IsActive = false;
            RuntimeState.PendingEndReason = AbilityEndReason.Completed;
        }

        public void RequestCancel()
        {
            RuntimeState.IsActive = false;
            RuntimeState.PendingEndReason = AbilityEndReason.Canceled;
        }
        #endregion

        public virtual void Dispose()
        {
            RuntimeCache.Dispose();
        }
    }

    public abstract class AbilityRuntimeContext<TState, TCache> : AbilityRuntimeContext
        where TState : AbilityRuntimeSharedState, new()
        where TCache : AbilityRuntimeSharedCache, new()
    {
        private readonly TState m_state = new TState();
        private readonly TCache m_cache = new TCache();

        protected sealed override AbilityRuntimeSharedState RuntimeState => m_state;
        protected sealed override AbilityRuntimeSharedCache RuntimeCache => m_cache;

        public new TState State => m_state;
        public new TCache Cache => m_cache;
    }
}