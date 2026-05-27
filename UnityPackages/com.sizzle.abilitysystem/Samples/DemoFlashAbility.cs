using Sizzle.AbilitySystem;
using UnityEngine;

namespace Sizzle.AbilitySystem.Samples
{
    [CreateAbilityAssetMenu("Demo/Flash Ability")]
    public class DemoFlashAbility : Ability<DemoFlashAbility.Context>
    {
        [SerializeField] private Color m_flashColor = Color.yellow;
        [SerializeField, Min(0.01f)] private float m_duration = 0.4f;
        [SerializeField] private AnimationCurve m_blendCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        public class ContextState : AbilityRuntimeSharedState
        {
            public Color OriginalColor { get; set; }
            public bool HasVisualTarget { get; set; }

            public override void Reset()
            {
                base.Reset();
                OriginalColor = default;
                HasVisualTarget = false;
            }
        }

        public class ContextCache : AbilityRuntimeSharedCache
        {
            public SpriteRenderer SpriteRenderer { get; set; }
            public Renderer Renderer { get; set; }
            public Material RuntimeMaterial { get; set; }
            public string ColorPropertyName { get; set; }
        }

        public class Context : AbilityRuntimeContext<ContextState, ContextCache>
        {
        }

        protected override bool CanActivate(Context context, AbilityActivatePayload payload)
        {
            return context?.Cache.GameObject != null;
        }

        protected override void OnActivate(Context context, AbilityActivatePayload payload)
        {
            CacheVisualTarget(context);

            if (!context.State.HasVisualTarget)
            {
                context.RequestComplete();
                return;
            }

            ApplyColor(context, 1f);

            if (m_duration <= 0f)
                context.RequestComplete();
        }

        protected override void OnUpdateTick(float deltaTime, Context context)
        {
            if (!context.State.HasVisualTarget)
            {
                context.RequestComplete();
                return;
            }

            float normalizedTime = Mathf.Clamp01(context.State.ElapsedTime / m_duration);
            float blend = EvaluateCurve(normalizedTime);
            ApplyColor(context, blend);

            if (normalizedTime >= 1f)
                context.RequestComplete();
        }

        protected override void OnDeactivate(AbilityEndReason endReason, Context context)
        {
            if (!context.State.HasVisualTarget)
                return;

            ApplyColor(context, 0f);
        }

        private void CacheVisualTarget(Context context)
        {
            ContextCache cache = context.Cache;
            ContextState state = context.State;
            GameObject target = cache.GameObject;
            state.HasVisualTarget = false;
            cache.SpriteRenderer ??= target.GetComponent<SpriteRenderer>();

            if (cache.SpriteRenderer != null)
            {
                state.OriginalColor = cache.SpriteRenderer.color;
                state.HasVisualTarget = true;
                return;
            }

            cache.Renderer ??= target.GetComponent<Renderer>();
            if (cache.Renderer == null)
            {
                Debug.LogWarning($"[{name}] No SpriteRenderer or Renderer found on {target.name}.", target);
                return;
            }

            cache.RuntimeMaterial ??= cache.Renderer.material;
            if (cache.RuntimeMaterial.HasProperty("_BaseColor"))
                cache.ColorPropertyName = "_BaseColor";
            else if (cache.RuntimeMaterial.HasProperty("_Color"))
                cache.ColorPropertyName = "_Color";
            else
            {
                Debug.LogWarning($"[{name}] Renderer on {target.name} does not expose _BaseColor or _Color.", target);
                cache.RuntimeMaterial = null;
                cache.Renderer = null;
                cache.ColorPropertyName = null;
                return;
            }

            state.OriginalColor = cache.RuntimeMaterial.GetColor(cache.ColorPropertyName);
            state.HasVisualTarget = true;
        }

        private void ApplyColor(Context context, float blend)
        {
            ContextCache cache = context.Cache;
            ContextState state = context.State;
            Color color = Color.Lerp(state.OriginalColor, m_flashColor, Mathf.Clamp01(blend));

            if (cache.SpriteRenderer != null)
            {
                cache.SpriteRenderer.color = color;
                return;
            }

            if (cache.RuntimeMaterial != null && !string.IsNullOrEmpty(cache.ColorPropertyName))
                cache.RuntimeMaterial.SetColor(cache.ColorPropertyName, color);
        }

        private float EvaluateCurve(float normalizedTime)
        {
            if (m_blendCurve == null || m_blendCurve.length == 0)
                return 1f - normalizedTime;

            return m_blendCurve.Evaluate(normalizedTime);
        }
    }
}