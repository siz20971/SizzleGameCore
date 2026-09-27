using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 독립된 ScriptableObject 에셋 파일로 관리되는 액션 시퀀스입니다.
    /// 여러 어빌리티, 무기 프리셋, 또는 공용 연출에서 동일한 액션 시퀀스를 재사용할 때 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ActionSequence_New", menuName = "Sizzle/Abilities/Action Sequence Asset", order = 100)]
    public class ActionSequenceAsset : ScriptableObject
    {
        [TextArea(2, 4)]
        [SerializeField] private string m_description;

        [SerializeField] private ActionSequence m_sequence = new ActionSequence();

        public string Description => m_description;
        public ActionSequence Sequence => m_sequence;
    }
}
