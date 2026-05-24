using Sizzle.AbilitySystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Samples
{
    [CreateAbilityAssetMenu("Demo/Log Ability")]
    public class DemoLogAbility : Ability<DemoLogAbility.Context>
    {
        [SerializeField] private string m_activateMessage = "Demo ability activated";
        [SerializeField] private string m_deactivateMessage = "Demo ability finished";
        [SerializeField, Min(0f)] private float m_duration = 0f;
        [SerializeField] private bool m_includeProcessorName = true;

        public class Context : AbilityRuntimeContext
        {
            protected override void OnReset()
            {
            }
        }

        protected override void OnActivate(Context context, AbilityActivatePayload payload)
        {
            Debug.Log(FormatMessage(context, m_activateMessage));

            if (m_duration <= 0f)
                context.RequestComplete();
        }

        protected override void OnUpdateTick(float deltaTime, Context context)
        {
            if (m_duration > 0f && context.ElapsedTime >= m_duration)
                context.RequestComplete();
        }

        protected override void OnDeactivate(AbilityEndReason endReason, Context context)
        {
            Debug.Log($"{FormatMessage(context, m_deactivateMessage)} ({endReason})");
        }

        private string FormatMessage(Context context, string message)
        {
            if (!m_includeProcessorName || context?.Processor == null)
                return message;

            return $"[{context.Processor.name}] {message}";
        }
    }
}