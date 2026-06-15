using System;

namespace Sizzle.AbilitySystem
{
    /// <summary>
    /// AbilityScriptGeneratorWindow에서 템플릿 부모 클래스로 표시될 수 있도록 지정하는 속성입니다.
    /// 제네릭 타입인 어빌리티 클래스(예: Ability<TContext>)에 붙여 사용합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AbilityTemplateAttribute : Attribute
    {
        public string DisplayName { get; }

        public AbilityTemplateAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
