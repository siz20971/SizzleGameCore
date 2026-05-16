//using Sizzle.GameTagSystem;

//namespace Sizzle.AbilitySystem
//{
//    /// <summary>
//    /// Task 유지시간동안 OwnerCharacter의 TagContainer에 Tag들을 추가합니다.
//    /// Task 종료 시 Tag들을 제거합니다.
//    /// </summary>
//    public class AbilityTaskAddTagsByDuration : AbilityTaskByDurationBase
//    {
//        private GameTag[] m_tags;
        
//        public AbilityTaskAddTagsByDuration(AbilityBase rootAbility, GameTag[] tags, float duration)
//        {
//            m_rootAbility = rootAbility;
//            m_tags = tags;
//            SetDuration(duration);
//        }

//        protected override void OnTaskStart()
//        {
//            foreach (GameTag tag in m_tags)
//                m_rootAbility.OwnerTagContainer.AddTag(tag);
//            base.OnTaskStart();
//        }

//        protected override void OnTaskEnd(bool isExpired)
//        {
//            foreach (GameTag tag in m_tags)
//                m_rootAbility.OwnerTagContainer.RemoveTag(tag);
//            base.OnTaskEnd(isExpired);
//        }
//    }
//}