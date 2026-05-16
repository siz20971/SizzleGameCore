using System;

namespace Sizzle.AbilitySystem
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CreateAbilityAssetMenuAttribute : Attribute
    {
        public string MenuName {get;}
        public int Priority {get;}
    
        public CreateAbilityAssetMenuAttribute(string menuName = null, int priority = -1)
        {
            MenuName = menuName;
        }
    }
}
