using UnityEngine;
using System.Collections.Generic;
using Serializable = System.SerializableAttribute;
using Sizzle.GameTagSystem;

namespace Sizzle.AbilitySystem
{
    [Serializable]
    public class AbilityGameTagSet
    {
        [Tooltip("이 어빌리티의 메인 태그")]
        [SerializeField] private GameTag m_mainTag;
        public GameTag MainTag => m_mainTag;

        [Tooltip("이 어빌리티가 실행될 때, 실행 중인 어빌리티 중 메인 태그가 이 태그들의 자식에 해당하면, 그 어빌리티를 취소함")]
        [SerializeField] private List<GameTag> m_cancelAbilitiesWithTag = new List<GameTag>();
        public IList<GameTag> CancelAbilitiesWithTag => m_cancelAbilitiesWithTag.AsReadOnly();

        [Tooltip("이 어빌리티가 실행되는 동안, 이 태그들의 자식 태그를 가진 어빌리티의 실행을 차단함")]
        [SerializeField] private List<GameTag> m_blockAbilitiesWithTag = new List<GameTag>();
        public IList<GameTag> BlockAbilitiesWithTag => m_blockAbilitiesWithTag.AsReadOnly();

        [Tooltip("이 어빌리티가 실행되는 동안, Owner가 이 태그들을 보유함")]
        [SerializeField] private List<GameTag> m_activationOwnedTags = new List<GameTag>();
        public IList<GameTag> ActivationOwnedTags => m_activationOwnedTags.AsReadOnly();

        [Tooltip("이 태그들을 보유하고 있어야, 어빌리티가 실행될 수 있음")]
        [SerializeField] private List<GameTag> m_activationRequiredTags = new List<GameTag>();
        public IList<GameTag> ActivationRequiredTags => m_activationRequiredTags.AsReadOnly();
        
        [Tooltip("이 태그들 중 하나라도 보유하고 있으면, 어빌리티가 실행될 수 없음")]
        [SerializeField] private List<GameTag> m_activationBlockedTags = new List<GameTag>();
        public IList<GameTag> ActivationBlockedTags => m_activationBlockedTags.AsReadOnly();

        [Tooltip("이 태그가 OwnerCharacter에 Notify 되면 어빌리티가 작동")]
        [SerializeField] private GameTag m_triggerTag;
        public GameTag TriggerTag => m_triggerTag;
        
        public void SetMainTag(GameTag tag)
        {
            m_mainTag = tag;
        }
    }
}