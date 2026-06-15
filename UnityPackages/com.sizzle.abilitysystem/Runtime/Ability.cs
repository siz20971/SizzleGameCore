using UnityEngine;
using Sizzle.GameTagSystem;

namespace Sizzle.AbilitySystem
{
    public abstract class Ability : ScriptableObject
    {
        public GameTag MainTag => m_tagSet.MainTag;
        
        [SerializeField] private AbilityGameTagSet m_tagSet = new AbilityGameTagSet();
        [SerializeField] private AbilityReactivationPolicy m_reactivationPolicy = AbilityReactivationPolicy.Deny;

        public AbilityGameTagSet TagSet => m_tagSet;
        public AbilityReactivationPolicy ReactivationPolicy => m_reactivationPolicy;

        /// <summary>
        /// 이 어빌리티의 MainTag가 지정한 문자열 태그의 자식 계열인지 반환합니다.
        /// </summary>
        public bool IsChildTag(string tag) => MainTag.ChildOf(tag);

        /// <summary>
        /// 이 어빌리티의 MainTag가 지정한 태그의 자식 계열인지 반환합니다.
        /// </summary>
        public bool IsChildTag(GameTag tag) => MainTag.ChildOf(tag);

        /// <summary>
        /// AbilityProcessor 에 등록될때 호출됩니다.
        /// Initialize 가 실패할 경우, 등록되지 않습니다.
        /// </summary>
        internal virtual bool Initialize(AbilityProcessor processor)
        {
            if (processor == null)
            {
                Debug.LogError($"AbilityProcessor is null. Failed to initialize ability {name}");
                return false;
            }
            
            return true;
        }

        internal AbilityRuntimeContext CreateContext()
        {
            return CreateContextInternal();
        }

        /// <summary>
        /// 이 어빌리티에 대응되는 런타임 컨텍스트 인스턴스를 생성합니다.
        /// </summary>
        protected abstract AbilityRuntimeContext CreateContextInternal();

        /// <summary>
        /// 어빌리티 발동 가능성 여부
        /// </summary>
        internal abstract bool CanActivate(AbilityRuntimeContext context, AbilityActivatePayload payload);

        internal void Activate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            context.Reset();
            context.ProcessActivate();
            OnActivate(context, payload);
        }

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 이미 활성 상태에서 재실행 요청이 들어오면 호출됩니다.
        /// </summary>
        internal void DoReactivate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            context.ProcessReactivate();
            OnReactivate(context, payload);
        }

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 CanActivate가 실패한 경우 호출됩니다.
        /// 입력 버퍼링 등 추가 처리가 필요한 경우 override하여 사용합니다.
        /// </summary>
        internal void DoReactivateBlocked(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            OnReactivateBlocked(context, payload);
        }

        internal void UpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            context.ProcessUpdate(deltaTime);
            OnUpdateTick(deltaTime, context);
        }


        internal void FixedUpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            context.ProcessFixedUpdate(deltaTime);
            OnFixedUpdateTick(deltaTime, context);
        }


        internal void LateUpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            context.ProcessLateUpdate(deltaTime);
            OnLateUpdateTick(deltaTime, context);
        }

        /// <summary> 어빌리티를 종료시킴 </summary>
        internal void Deactivate(AbilityEndReason endReason, AbilityRuntimeContext context, bool resetContext = true)
        {
            OnDeactivate(endReason, context);
            context.Reset();
        }

        #region Ability Lifecycle Abstract Methods
        /// <summary>
        /// 어빌리티가 최초 활성화될 때 실행할 로직을 구현합니다.
        /// </summary>
        protected abstract void OnActivate(AbilityRuntimeContext context, AbilityActivatePayload payload);

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 호출됩니다. 기본 구현은 OnActivate와 동일하게 처리합니다.
        /// </summary>
        protected virtual void OnReactivate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            OnActivate(context, payload);
        }

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 CanActivate가 실패한 경우 호출됩니다.
        /// 기본 구현은 비어있습니다. 입력 버퍼링 등이 필요한 경우 override하여 사용합니다.
        /// </summary>
        protected virtual void OnReactivateBlocked(AbilityRuntimeContext context, AbilityActivatePayload payload) { }

        /// <summary>
        /// 어빌리티가 활성 상태인 동안 매 프레임 호출되는 업데이트 훅입니다.
        /// </summary>
        protected virtual void OnUpdateTick(float deltaTime, AbilityRuntimeContext context) { }

        /// <summary>
        /// 어빌리티가 활성 상태인 동안 물리 프레임마다 호출되는 업데이트 훅입니다.
        /// </summary>
        protected virtual void OnFixedUpdateTick(float deltaTime, AbilityRuntimeContext context) { }

        /// <summary>
        /// 어빌리티가 활성 상태인 동안 LateUpdate 시점에 호출되는 훅입니다.
        /// </summary>
        protected virtual void OnLateUpdateTick(float deltaTime, AbilityRuntimeContext context) { }

        /// <summary>
        /// 어빌리티가 종료될 때 정리 로직을 구현합니다.
        /// </summary>
        protected abstract void OnDeactivate(AbilityEndReason endReason, AbilityRuntimeContext context);
        #endregion
    }

    /////////////////////////////////////////////////////////////////////////////////////
    /// 아래는 Ability를 상속받아 추가적인 제네릭 타입을 받는 추상 클래스들입니다.
    /////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Ability의 런타임 컨텍스트를 제네릭 타입으로 받는 추상 클래스입니다.
    /// </summary>
    [AbilityTemplate("Basic Ability (Context Only)")]
    public abstract class Ability<TContext> : Ability
            where TContext : AbilityRuntimeContext, new()
    {
        /// <summary>
        /// 이 Ability 타입에 맞는 구체 컨텍스트를 생성합니다.
        /// </summary>
        protected sealed override AbilityRuntimeContext CreateContextInternal()
        {
            TContext context = new TContext();
            return context;
        }

        internal sealed override bool CanActivate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            return CanActivate(context as TContext, payload);
        }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 활성화 가능 여부를 검사합니다.
        /// 기본 구현은 항상 true를 반환합니다.
        /// </summary>
        protected virtual bool CanActivate(TContext context, AbilityActivatePayload payload)
        {
            return true;
        }

        protected sealed override void OnDeactivate(AbilityEndReason endReason, AbilityRuntimeContext context)
        {
            OnDeactivate(endReason, context as TContext);
        }

        protected sealed override void OnActivate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            OnActivate(context as TContext, payload);
        }

        protected sealed override void OnReactivate(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            OnReactivate(context as TContext, payload);
        }

        protected sealed override void OnReactivateBlocked(AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            OnReactivateBlocked(context as TContext, payload);
        }

        protected sealed override void OnFixedUpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            OnFixedUpdateTick(deltaTime, context as TContext);
        }

        protected sealed override void OnLateUpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            OnLateUpdateTick(deltaTime, context as TContext);
        }

        protected sealed override void OnUpdateTick(float deltaTime, AbilityRuntimeContext context)
        {
            OnUpdateTick(deltaTime, context as TContext);
        }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 최초 활성화 로직을 구현합니다.
        /// </summary>
        protected abstract void OnActivate(TContext context, AbilityActivatePayload payload);

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 호출됩니다. 기본 구현은 OnActivate와 동일하게 동작합니다.
        /// </summary>
        protected virtual void OnReactivate(TContext context, AbilityActivatePayload payload)
        {
            OnActivate(context, payload);
        }

        /// <summary>
        /// ReactivationPolicy가 Reactivate일 때 CanActivate가 실패한 경우 호출됩니다.
        /// </summary>
        protected virtual void OnReactivateBlocked(TContext context, AbilityActivatePayload payload) { }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 물리 프레임 업데이트 로직을 구현합니다.
        /// </summary>
        protected virtual void OnFixedUpdateTick(float deltaTime, TContext context) { }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 LateUpdate 로직을 구현합니다.
        /// </summary>
        protected virtual void OnLateUpdateTick(float deltaTime, TContext context) { }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 일반 프레임 업데이트 로직을 구현합니다.
        /// </summary>
        protected virtual void OnUpdateTick(float deltaTime, TContext context) { }

        /// <summary>
        /// 타입이 확정된 컨텍스트 기준으로 종료 시 정리 로직을 구현합니다.
        /// </summary>
        protected abstract void OnDeactivate(AbilityEndReason endReason, TContext context);

    }

    /// <summary>
    /// Ability의 런타임 컨텍스트와 ActivatePayload을 제네릭 타입으로 받는 추상 클래스입니다.
    /// </summary>
    [AbilityTemplate("Ability with Payload")]
    public abstract class Ability<TContext, TActivatePayload> : Ability<TContext>
            where TContext : AbilityRuntimeContext, new()
            where TActivatePayload : AbilityActivatePayload
    {
        protected sealed override void OnActivate(TContext context, AbilityActivatePayload payload)
        {
            OnActivate(context, payload as TActivatePayload);
        }

        /// <summary>
        /// 타입이 확정된 컨텍스트와 페이로드 기준으로 최초 활성화 로직을 구현합니다.
        /// </summary>
        protected abstract void OnActivate(TContext context, TActivatePayload payload);

        protected sealed override void OnReactivate(TContext context, AbilityActivatePayload payload)
        {
            OnReactivate(context, payload as TActivatePayload);
        }

        /// <summary>
        /// 타입이 확정된 컨텍스트와 페이로드 기준으로 Reactivate 로직을 구현합니다.
        /// 기본 구현은 OnActivate를 재사용합니다.
        /// </summary>
        protected virtual void OnReactivate(TContext context, TActivatePayload payload)
        {
            OnActivate(context, payload);
        }
    }
}