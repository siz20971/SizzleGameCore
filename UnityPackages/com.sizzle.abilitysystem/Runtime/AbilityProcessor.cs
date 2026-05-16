using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sizzle.GameTagSystem;

namespace Sizzle.AbilitySystem
{
    public delegate void AbilityRegisteredHandler(Ability ability, AbilityRuntimeContext context);
    public delegate void AbilityUnregisteredHandler(Ability ability, AbilityRuntimeContext context);

    public class AbilityProcessor : MonoBehaviour
    {
        public GameTagContainer TagContainer { get; } = new GameTagContainer();

        // References
        [Tooltip("초기화 호출시 등록될 어빌리티 리스트")]
        [SerializeField] protected List<Ability> m_defaultAbilities = new List<Ability>();

        // 이 AbilityProcessor에 등록된 모든 어빌리티 컨텍스트
        private List<AbilityRuntimeContext> m_totalContexts = new List<AbilityRuntimeContext>();

        /// <summary> MainTag를 통해 실행 가능한 AbilityContext</summary>
        private Dictionary<GameTag, AbilityRuntimeContext> m_activatableContexts = new Dictionary<GameTag, AbilityRuntimeContext>();
        /// <summary> TriggerTag를 통해 실행 가능한 AbilityContext. TagContainer에 Tag가 트리거 될 때 실행 </summary>
        private Dictionary<GameTag, AbilityRuntimeContext> m_triggerableContexts = new Dictionary<GameTag, AbilityRuntimeContext>();

        // 현재 활성화된 어빌리티들의 Context 리스트. Update에서 이 리스트를 순회하면서 활성화된 어빌리티들을 Tick함.
        private List<AbilityRuntimeContext> m_activeContexts = new List<AbilityRuntimeContext>();

        // Public Properties

        // Events
        public event AbilityRegisteredHandler OnAbilityRegistered;
        public event AbilityUnregisteredHandler OnAbilityUnregistered;

        private List<AbilityRuntimeContext> m_pendingRemoveContexts = new List<AbilityRuntimeContext>();

        public bool AnyAbilityIsActive => m_activeContexts.Count > 0;

        private void Awake()
        {
            TagContainer.OnTagNotified += OnTagNotified;
        }

        public void Initialize()
        {
            foreach (Ability ability in m_defaultAbilities)
                RegistAbility(ability, null);
        }

        /// <summary> AbilityProcessor에 초기화 시점에 등록될 어빌리티 애셋 리스트를 반환합니다. 반환된 리스트는 읽기 전용입니다. </summary>
        public IList<Ability> GetDefaultAbilities() => m_defaultAbilities.AsReadOnly();

        public IList<AbilityRuntimeContext> GetAllAbilityContexts() => m_totalContexts.AsReadOnly();

        public AbilityRuntimeContext GetAbilityContext(GameTag mainTag)
        {
            if (mainTag.IsEmpty)
                return null;
            if (!m_activatableContexts.TryGetValue(mainTag, out AbilityRuntimeContext context))
                return null;
            return context;
        }

        public Ability RegistAbility(Ability ability, Action<Ability> onRegistered)
        {
            if (!ability)
                return null;

            if (!ability.Initialize(this))
            {
                return null;
            }

            AbilityRuntimeContext context = ability.CreateContext();
            
            if (context == null)
            {
                Debug.LogError($"[AbilityProcessor] RegistAbility Failed. CreateContext returned null. Ability:{ability.name}");
                return null;
            }

            // runtimeContext 생성 준비.
            if (!context.Initialize(ability, this))
            {
                Debug.LogError($"[AbilityProcessor] RegistAbility Failed. Context Initialize Failed. Ability:{ability.name}");
                return null;
            }

            context.Reset();

            if (!string.IsNullOrEmpty(ability.MainTag.TagName))
            {
                if (!m_activatableContexts.TryAdd(ability.MainTag, context))
                    Debug.Log("[AbilityProcessor] RegistAbility Failed. AlreadyExist Tag:" + ability.MainTag);
            }

            if (!string.IsNullOrEmpty(ability.TagSet.TriggerTag.TagName))
            {
                if (!m_triggerableContexts.TryAdd(ability.TagSet.TriggerTag, context))
                    Debug.Log("[AbilityProcessor] RegistAbility Failed. AlreadyExist TriggerTag:" + ability.TagSet.TriggerTag);
            }

            m_totalContexts.Add(context);

            onRegistered?.Invoke(ability);
            OnAbilityRegistered?.Invoke(ability, context);

            return ability;
        }

        public bool UnregistAbility(GameTag tag)
        {
            if (tag.IsEmpty)
                return false;

            if (!m_activatableContexts.TryGetValue(tag, out AbilityRuntimeContext context))
                return false;

            return UnregistAbilityImplement(context);
        }


        /// <summary>
        /// 어빌리티를 실행 요청합니다.
        /// </summary>
        /// <param name="gameTag">어빌리티를 실행 요청하기 위해 사용된 게임태그</param>
        /// <returns>어빌리티 실행 시도 결과</returns>
        public AbilityActivateResult TryActivateAbility(GameTag gameTag, AbilityActivatePayload payload = null)
        {
            if (!m_activatableContexts.TryGetValue(gameTag, out AbilityRuntimeContext targetContext))
                return AbilityActivateResult.FailedAbilityNotFound;
            return TryActivateAbilityImplement(targetContext, payload);
        }

        public void CancelAllAbilities()
        {
            foreach (AbilityRuntimeContext context in m_activeContexts)
            {
                if (context == null || context.Ability == null || !context.IsActive)
                    continue;
                context.Ability.Deactivate(AbilityEndReason.Canceled, context);
                foreach (GameTag tag in context.Ability.TagSet.ActivationOwnedTags)
                    TagContainer.RemoveTag(tag);
            }
            m_activeContexts.Clear();
        }

        #region Update Ability Ticks
        private void Update()
        {
            float deltaTime = Time.deltaTime;

            TagContainer.Tick(deltaTime);

            for (int i = m_activeContexts.Count - 1; i >= 0; i--)
            {
                AbilityRuntimeContext context = m_activeContexts[i];
                if (context == null || context.Ability == null)
                    continue;
                context.Ability.UpdateTick(deltaTime, context);
            }

            OnUpdate();
        }

        protected virtual void OnUpdate() { }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            for (int i = m_activeContexts.Count - 1; i >= 0; i--)
            {
                AbilityRuntimeContext context = m_activeContexts[i];
                if (context == null || context.Ability == null)
                    continue;
                context.Ability.FixedUpdateTick(deltaTime, context);
            }

            OnFixedUpdate();
        }

        protected virtual void OnFixedUpdate() { }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            for (int i = m_activeContexts.Count - 1; i >= 0; i--)
            {
                AbilityRuntimeContext context = m_activeContexts[i];
                if (context == null || context.Ability == null)
                    continue;
                context.Ability.LateUpdateTick(deltaTime, context);
            }

            OnLateUpdate();
            CleanUpInactiveAbilities();
        }

        protected virtual void OnLateUpdate() { }

        private void CleanUpInactiveAbilities()
        {
            foreach (AbilityRuntimeContext context in m_activeContexts)
            {
                if (context == null || context.Ability == null || context.PendingEndReason == AbilityEndReason.None)
                    continue;
                m_pendingRemoveContexts.Add(context);
            }

            foreach (AbilityRuntimeContext context in m_pendingRemoveContexts)
            {
                context.Ability.Deactivate(context.PendingEndReason, context);
                foreach (GameTag tag in context.Ability.TagSet.ActivationOwnedTags)
                    TagContainer.RemoveTag(tag);

                m_activeContexts.Remove(context);
            }

            m_pendingRemoveContexts.Clear();
        }
        #endregion

        #region Callback Methods
        private void OnTagNotified(GameTag gametag)
        {
            if (!m_triggerableContexts.TryGetValue(gametag, out AbilityRuntimeContext targetContext))
                return;
            TryActivateAbilityImplement(targetContext, null);
        }
        #endregion

        #region Public Methods
        public bool IsActive(GameTag mainTag)
        {
            if (!m_activatableContexts.TryGetValue(mainTag, out AbilityRuntimeContext context))
                return false;
            return context.IsActive;
        }

        public bool HasActivatableAbility(GameTag gameTag) => m_activatableContexts.ContainsKey(gameTag);
        public bool HasTriggableAbility(GameTag gameTag) => m_triggerableContexts.ContainsKey(gameTag);
        #endregion

        #region Helper Methods
        /// <summary>
        /// 어빌리티를 실행 요청합니다.
        /// </summary>
        /// <param name="targetAbility">실행할 어빌리티</param>
        /// <param name="payload">어빌리티 실행에 필요한 페이로드</param>
        /// <returns>어빌리티 실행 시도 결과</returns>
        private AbilityActivateResult TryActivateAbilityImplement(
            AbilityRuntimeContext context, AbilityActivatePayload payload)
        {
            if (context == null)
                return AbilityActivateResult.FailedAbilityNotFound;

            Ability targetAbility = context.Ability;
            if (targetAbility == null)
                return AbilityActivateResult.FailedAbilityNotFound;

            if (context.IsActive)
            {
                switch (targetAbility.ReactivationPolicy)
                {
                    case AbilityReactivationPolicy.Deny:
                        return AbilityActivateResult.FailedAlreadyActive;

                    case AbilityReactivationPolicy.RestartFromBeginning:
                        targetAbility.Deactivate(AbilityEndReason.Canceled, context);
                        foreach (GameTag tag in targetAbility.TagSet.ActivationOwnedTags)
                            TagContainer.RemoveTag(tag);
                        m_activeContexts.Remove(context);
                        break; // 이후 신규 활성화 플로우로 진행

                    case AbilityReactivationPolicy.Reactivate:
                        if (!targetAbility.CanActivate(context, payload))
                        {
                            targetAbility.DoReactivateBlocked(context, payload);
                            return AbilityActivateResult.FailedCanNotUse;
                        }
                        if (!TagContainer.HasExactTagsAll(targetAbility.TagSet.ActivationRequiredTags.ToArray()))
                            return AbilityActivateResult.FailedNotHasAllRequiredTag;
                        if (TagContainer.HasExactTagsAny(targetAbility.TagSet.ActivationBlockedTags.ToArray()))
                            return AbilityActivateResult.FailedHasAnyBlockTag;
                        targetAbility.DoReactivate(context, payload);
                        return AbilityActivateResult.Success;
                }
            }

            if (!targetAbility.CanActivate(context, payload))
                return AbilityActivateResult.FailedCanNotUse;

            AbilityGameTagSet tagSet = targetAbility.TagSet;

            // 활성화에 필요한 태그들이 모두 있는지 확인.
            bool hasAllRequiredTag = TagContainer.HasExactTagsAll(tagSet.ActivationRequiredTags.ToArray());
            if (!hasAllRequiredTag)
                return AbilityActivateResult.FailedNotHasAllRequiredTag;

            // 활성화를 차단하는 태그들이 하나라도 있는지 확인.
            bool hasAnyBlockTag = TagContainer.HasExactTagsAny(tagSet.ActivationBlockedTags.ToArray());
            if (hasAnyBlockTag)
                return AbilityActivateResult.FailedHasAnyBlockTag;

            // 실행중인 어빌리티가 다른 어빌리티 실행을 차단하는 리스트를 체크.
            foreach (AbilityRuntimeContext ctx in m_activeContexts)
            {
                if (!ctx.IsActive)
                    continue;

                foreach (GameTag blockTag in ctx.Ability.TagSet.BlockAbilitiesWithTag)
                {
                    if (tagSet.MainTag.ChildOf(blockTag))
                        return AbilityActivateResult.FailedBlockedByOther;
                }
            }

            // 실행.
            m_activeContexts.Add(context);
            targetAbility.Activate(context, payload);

            foreach (GameTag tag in tagSet.ActivationOwnedTags)
                TagContainer.AddTag(tag);

            // 어빌리티가 활성화되면 취소될 어빌리티들을 처리.
            foreach (GameTag cancelTag in tagSet.CancelAbilitiesWithTag)
            {
                List<AbilityRuntimeContext> cancelContexts = m_activeContexts.FindAll(context => context.Ability.IsChildTag(cancelTag));
                foreach (AbilityRuntimeContext ctx in cancelContexts)
                {
                    if (!ctx.IsActive) continue;
                    ctx.RequestCancel();
                }
            }

            return AbilityActivateResult.Success;
        }

        private bool UnregistAbilityImplement(AbilityRuntimeContext context)
        {
            if (context == null || context.Ability == null)
                return false;

            Ability ability = context.Ability;

            if (context.IsActive)
            {
                ability.Deactivate(AbilityEndReason.Canceled, context);
                foreach (GameTag tag in ability.TagSet.ActivationOwnedTags)
                    TagContainer.RemoveTag(tag);
            }

            m_totalContexts.Remove(context);
            m_activatableContexts.Remove(ability.MainTag);
            m_triggerableContexts.Remove(ability.TagSet.TriggerTag);
            m_activeContexts.Remove(context);
            m_pendingRemoveContexts.Remove(context);
            context.Dispose();
            return true;
        }
        #endregion

        protected virtual void OnDestroy()
        {
            TagContainer.OnTagNotified -= OnTagNotified;

            for (int i = m_activeContexts.Count - 1; i >= 0; i--)
            {
                AbilityRuntimeContext context = m_activeContexts[i];
                if (context != null)
                    UnregistAbilityImplement(context);
            }

            m_totalContexts.Clear();
            m_activatableContexts.Clear();
            m_triggerableContexts.Clear();

            OnAbilityRegistered = null;
            OnAbilityUnregistered = null;
        }
    }
}