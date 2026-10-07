using System;
using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// 지원되는 이징(Easing) 보간 곡선 타입입니다.
    /// </summary>
    public enum EaseType
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InSine,
        OutSine,
        InOutSine,
        InExpo,
        OutExpo,
        InOutExpo,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
        InElastic,
        OutElastic,
        InOutElastic
    }

    /// <summary>
    /// 외부 트윈 라이브러리 없이 순수 수학 함수로 부드러운 이징(Easing) 보간을 평가하는 유틸리티입니다.
    /// </summary>
    public static class TweenEasing
    {
        /// <summary>
        /// 0과 1 사이의 정규화된 진행률(t)을 지정된 이징 곡선으로 평가합니다.
        /// </summary>
        /// <param name="type">이징 타입</param>
        /// <param name="t">진행률 (0 ~ 1)</param>
        /// <returns>이징이 적용된 보간값</returns>
        public static float Evaluate(EaseType type, float t)
        {
            t = Mathf.Clamp01(t);

            return type switch
            {
                EaseType.Linear => t,
                EaseType.InQuad => t * t,
                EaseType.OutQuad => t * (2f - t),
                EaseType.InOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                EaseType.InCubic => t * t * t,
                EaseType.OutCubic => (--t) * t * t + 1f,
                EaseType.InOutCubic => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f,
                EaseType.InSine => 1f - Mathf.Cos(t * (Mathf.PI * 0.5f)),
                EaseType.OutSine => Mathf.Sin(t * (Mathf.PI * 0.5f)),
                EaseType.InOutSine => -0.5f * (Mathf.Cos(Mathf.PI * t) - 1f),
                EaseType.InExpo => t == 0f ? 0f : Mathf.Pow(2f, 10f * (t - 1f)),
                EaseType.OutExpo => Mathf.Approximately(t, 1f) ? 1f : -Mathf.Pow(2f, -10f * t) + 1f,
                EaseType.InOutExpo => EvaluateInOutExpo(t),
                EaseType.InBack => EvaluateInBack(t),
                EaseType.OutBack => EvaluateOutBack(t),
                EaseType.InOutBack => EvaluateInOutBack(t),
                EaseType.InBounce => 1f - EvaluateOutBounce(1f - t),
                EaseType.OutBounce => EvaluateOutBounce(t),
                EaseType.InOutBounce => t < 0.5f ? (1f - EvaluateOutBounce(1f - 2f * t)) * 0.5f : EvaluateOutBounce(2f * t - 1f) * 0.5f + 0.5f,
                EaseType.InElastic => EvaluateInElastic(t),
                EaseType.OutElastic => EvaluateOutElastic(t),
                EaseType.InOutElastic => EvaluateInOutElastic(t),
                _ => t
            };
        }

        private static float EvaluateInOutExpo(float t)
        {
            if (t == 0f) return 0f;
            if (Mathf.Approximately(t, 1f)) return 1f;
            if ((t *= 2f) < 1f) return 0.5f * Mathf.Pow(2f, 10f * (t - 1f));
            return 0.5f * (-Mathf.Pow(2f, -10f * --t) + 2f);
        }

        private static float EvaluateInBack(float t)
        {
            const float s = 1.70158f;
            return t * t * ((s + 1f) * t - s);
        }

        private static float EvaluateOutBack(float t)
        {
            const float s = 1.70158f;
            return --t * t * ((s + 1f) * t + s) + 1f;
        }

        private static float EvaluateInOutBack(float t)
        {
            float s = 1.70158f * 1.525f;
            if ((t *= 2f) < 1f) return 0.5f * (t * t * ((s + 1f) * t - s));
            return 0.5f * ((t -= 2f) * t * ((s + 1f) * t + s) + 2f);
        }

        private static float EvaluateOutBounce(float t)
        {
            if (t < (1f / 2.75f))
            {
                return 7.5625f * t * t;
            }
            else if (t < (2f / 2.75f))
            {
                return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
            }
            else if (t < (2.5f / 2.75f))
            {
                return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
            }
            else
            {
                return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
            }
        }

        private static float EvaluateInElastic(float t)
        {
            if (t == 0f) return 0f;
            if (Mathf.Approximately(t, 1f)) return 1f;
            const float p = 0.3f;
            const float s = p / 4f;
            return -(Mathf.Pow(2f, 10f * (t -= 1f)) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p));
        }

        private static float EvaluateOutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (Mathf.Approximately(t, 1f)) return 1f;
            const float p = 0.3f;
            const float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        private static float EvaluateInOutElastic(float t)
        {
            if (t == 0f) return 0f;
            if (Mathf.Approximately(t, 1f)) return 1f;
            const float p = 0.3f * 1.5f;
            const float s = p / 4f;
            if ((t *= 2f) < 1f)
            {
                return -0.5f * (Mathf.Pow(2f, 10f * (t -= 1f)) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p));
            }
            return Mathf.Pow(2f, -10f * (t -= 1f)) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) * 0.5f + 1f;
        }
    }
}
