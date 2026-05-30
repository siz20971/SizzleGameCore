using Unity.Behavior;
using UnityEngine;

namespace Sizzle.AbilitySystem.Behavior
{
    internal static class BehaviorAbilityUtility
    {
        internal static bool TryResolveProcessor(BlackboardVariable<GameObject> target, GameObject fallback, out AbilityProcessor processor)
        {
            GameObject source = target != null && target.Value != null ? target.Value : fallback;
            processor = ResolveProcessor(source);
            return processor != null;
        }

        internal static AbilityProcessor ResolveProcessor(GameObject source)
        {
            return AbilityProcessorResolver.Resolve(source);
        }

        internal static void SetBlackboardValue<T>(BlackboardVariable<T> variable, T value)
        {
            if (variable != null)
                variable.Value = value;
        }

        internal static bool GetBoolValue(BlackboardVariable<bool> variable)
        {
            return variable != null && variable.Value;
        }
    }
}