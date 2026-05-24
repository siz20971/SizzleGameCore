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

        public class Context : AbilityRuntimeContext
        {
            public SpriteRenderer SpriteRenderer { get; set; }
            public Renderer Renderer { get; set; }
            public Material RuntimeMaterial { get; set; }
            public string ColorPropertyName { get; set; }
            public Color OriginalColor { get; set; }
            public bool HasVisualTarget { get; set; }

            protected override void OnReset()
            {
                SpriteRenderer = null;
                Renderer = null;
                RuntimeMaterial = null;
                ColorPropertyName = null;
                OriginalColor = default;
                HasVisualTarget = false;
            }
        }

        protected override bool CanActivate(Context context, AbilityActivatePayload payload)
        {
            return context?.GameObject != null;
        }

        protected override void OnActivate(Context context, AbilityActivatePayload payload)
        {
            CacheVisualTarget(context);

            if (!context.HasVisualTarget)
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
            if (!context.HasVisualTarget)
            {
                context.RequestComplete();
                return;
            }

            float normalizedTime = Mathf.Clamp01(context.ElapsedTime / m_duration);
            float blend = EvaluateCurve(normalizedTime);
            ApplyColor(context, blend);

            if (normalizedTime >= 1f)
                context.RequestComplete();
        }

        protected override void OnDeactivate(AbilityEndReason endReason, Context context)
        {
            if (!context.HasVisualTarget)
                return;

            ApplyColor(context, 0f);
        }

        private void CacheVisualTarget(Context context)
        {
            GameObject target = context.GameObject;
            context.SpriteRenderer = target.GetComponent<SpriteRenderer>();

            if (context.SpriteRenderer != null)
            {
                context.OriginalColor = context.SpriteRenderer.color;
                context.HasVisualTarget = true;
                return;
            }

            context.Renderer = target.GetComponent<Renderer>();
            if (context.Renderer == null)
            {
                Debug.LogWarning($"[{name}] No SpriteRenderer or Renderer found on {target.name}.", target);
                return;
            }

            context.RuntimeMaterial = context.Renderer.material;
            if (context.RuntimeMaterial.HasProperty("_BaseColor"))
                context.ColorPropertyName = "_BaseColor";
            else if (context.RuntimeMaterial.HasProperty("_Color"))
                context.ColorPropertyName = "_Color";
            else
            {
                Debug.LogWarning($"[{name}] Renderer on {target.name} does not expose _BaseColor or _Color.", target);
                context.RuntimeMaterial = null;
                context.Renderer = null;
                return;
            }

            context.OriginalColor = context.RuntimeMaterial.GetColor(context.ColorPropertyName);
            context.HasVisualTarget = true;
        }

        private void ApplyColor(Context context, float blend)
        {
            Color color = Color.Lerp(context.OriginalColor, m_flashColor, Mathf.Clamp01(blend));

            if (context.SpriteRenderer != null)
            {
                context.SpriteRenderer.color = color;
                return;
            }

            if (context.RuntimeMaterial != null && !string.IsNullOrEmpty(context.ColorPropertyName))
                context.RuntimeMaterial.SetColor(context.ColorPropertyName, color);
        }

        private float EvaluateCurve(float normalizedTime)
        {
            if (m_blendCurve == null || m_blendCurve.length == 0)
                return 1f - normalizedTime;

            return m_blendCurve.Evaluate(normalizedTime);
        }
    }
}