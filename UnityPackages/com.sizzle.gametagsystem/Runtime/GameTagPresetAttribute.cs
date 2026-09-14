using System;

namespace Sizzle.GameTagSystem
{
    /// <summary>
    /// 이 어트리뷰트가 적용된 클래스(또는 구조체) 내의 정적 GameTag 및 string 필드/프로퍼티는
    /// 에디터에서 자동으로 수집되어 GameTagCache에 등록됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class GameTagPresetAttribute : Attribute
    {
    }
}
