using Sizzle.AbilitySystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Samples
{
    [CreateAbilityAssetMenu("Demo/Move Ability")]
    public class DemoMoveAbility : Ability<DemoMoveAbility.Context>
    {
        [SerializeField] private Vector3 m_localOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(0.01f)] private float m_duration = 0.35f;
        [SerializeField] private bool m_restorePositionOnEnd = true;
        [SerializeField] private AnimationCurve m_motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public class Context : AbilityRuntimeContext
        {
            public Transform TargetTransform { get; set; }
            public Vector3 StartLocalPosition { get; set; }

            protected override void OnReset()
            {
                TargetTransform = null;
                StartLocalPosition = default;
            }
        }

        protected override bool CanActivate(Context context, AbilityActivatePayload payload)
        {
            return context?.GameObject != null;
        }

        protected override void OnActivate(Context context, AbilityActivatePayload payload)
        {
            context.TargetTransform = context.GameObject.transform;
            context.StartLocalPosition = context.TargetTransform.localPosition;

            if (m_duration <= 0f)
            {
                ApplyPosition(context, 1f);
                context.RequestComplete();
                return;
            }

            ApplyPosition(context, 0f);
        }

        protected override void OnUpdateTick(float deltaTime, Context context)
        {
            if (context.TargetTransform == null)
            {
                context.RequestCancel();
                return;
            }

            float normalizedTime = Mathf.Clamp01(context.ElapsedTime / m_duration);
            ApplyPosition(context, normalizedTime);

            if (normalizedTime >= 1f)
                context.RequestComplete();
        }

        protected override void OnDeactivate(AbilityEndReason endReason, Context context)
        {
            if (m_restorePositionOnEnd && context.TargetTransform != null)
                context.TargetTransform.localPosition = context.StartLocalPosition;
        }

        private void ApplyPosition(Context context, float normalizedTime)
        {
            if (context.TargetTransform == null)
                return;

            float easedTime = EvaluateCurve(normalizedTime);
            context.TargetTransform.localPosition = context.StartLocalPosition + m_localOffset * easedTime;
        }

        private float EvaluateCurve(float normalizedTime)
        {
            if (m_motionCurve == null || m_motionCurve.length == 0)
                return normalizedTime;

            return m_motionCurve.Evaluate(normalizedTime);
        }
    }
}