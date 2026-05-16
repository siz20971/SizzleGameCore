//using Sizzle.GameTagSystem;

//namespace Sizzle.AbilitySystem
//{
//    /// <summary>
//    /// Task 유지시간동안 OwnerCharacter의 TagContainer에 Tag를 추가합니다.
//    /// Task 종료 시 Tag를 제거합니다.
//    /// </summary>
//    public class AbilityTaskAddTagByDuration : AbilityTaskByDurationBase
//    {
//        private GameTag m_tag;
        
//        public AbilityTaskAddTagByDuration(AbilityBase rootAbility, GameTag tag, float duration)
//        {
//            m_rootAbility = rootAbility;
//            m_tag = tag;
//            SetDuration(duration);
//        }

//        protected override void OnTaskStart()
//        {
//            m_rootAbility.OwnerTagContainer.AddTag(m_tag);
//            base.OnTaskStart();
//        }

//        protected override void OnTaskEnd(bool isExpired)
//        {
//            m_rootAbility.OwnerTagContainer.RemoveTag(m_tag);
//            base.OnTaskEnd(isExpired);
//        }
//    }
//}