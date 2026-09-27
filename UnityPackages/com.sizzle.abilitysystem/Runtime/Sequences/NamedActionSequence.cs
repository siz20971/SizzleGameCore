using System;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 분기형 어빌리티(Branching Ability)에서 특정 조건이나 페이즈에 대응하는 시퀀스를
    /// 이름(Name)과 함께 묶어 정의할 수 있도록 지원하는 데이터 구조체입니다.
    /// </summary>
    [Serializable]
    public class NamedActionSequence
    {
        public string Name = "New Sequence";
        public ActionSequence Sequence = new ActionSequence();

        public NamedActionSequence() { }

        public NamedActionSequence(string name)
        {
            Name = name;
            Sequence = new ActionSequence();
        }

        public NamedActionSequence(string name, ActionSequence sequence)
        {
            Name = name;
            Sequence = sequence ?? new ActionSequence();
        }
    }
}
