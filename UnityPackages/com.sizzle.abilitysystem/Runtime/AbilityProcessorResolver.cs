using UnityEngine;

namespace Sizzle.AbilitySystem
{
    public static class AbilityProcessorResolver
    {
        public static bool TryResolve(GameObject source, out AbilityProcessor processor)
        {
            processor = Resolve(source);
            return processor != null;
        }

        public static AbilityProcessor Resolve(GameObject source)
        {
            if (source == null)
                return null;

            if (source.TryGetComponent(out AbilityProcessor processor))
                return processor;

            MonoBehaviour[] components = source.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IAbilitySystemComponent abilitySystemComponent && abilitySystemComponent.AbilityProcessor != null)
                    return abilitySystemComponent.AbilityProcessor;
            }

            return null;
        }
    }
}