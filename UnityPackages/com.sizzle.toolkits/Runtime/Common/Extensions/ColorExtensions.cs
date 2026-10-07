using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// <see cref="Color"/> 및 <see cref="Color32"/> 편의 확장 메서드 모음입니다.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// 색상의 RGB는 유지하고 알파(A) 값만 변경한 새 Color를 반환합니다.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// 색상을 16진수 HTML/Hex 문자열(예: #RRGGBB 또는 #RRGGBBAA)로 변환합니다.
        /// </summary>
        /// <param name="color">변환할 색상</param>
        /// <param name="includeAlpha">알파 채널 포함 여부</param>
        public static string ToHex(this Color color, bool includeAlpha = false)
        {
            Color32 c32 = color;
            return includeAlpha
                ? $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}"
                : $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
        }

        /// <summary>
        /// 16진수 문자열(#RRGGBB 또는 RRGGBB)로부터 Color를 파싱합니다.
        /// 파싱 실패 시 기본값(white)을 반환합니다.
        /// </summary>
        public static Color FromHex(string hexString, Color defaultColor = default)
        {
            if (string.IsNullOrEmpty(hexString))
                return defaultColor == default ? Color.white : defaultColor;

            if (!hexString.StartsWith("#"))
                hexString = "#" + hexString;

            if (ColorUtility.TryParseHtmlString(hexString, out Color result))
            {
                return result;
            }

            return defaultColor == default ? Color.white : defaultColor;
        }

        /// <summary>
        /// 색상의 밝기를 factor 비율(0~1)만큼 밝게 만듭니다.
        /// </summary>
        public static Color Lighten(this Color color, float factor)
        {
            factor = Mathf.Clamp01(factor);
            return Color.Lerp(color, Color.white, factor);
        }

        /// <summary>
        /// 색상의 밝기를 factor 비율(0~1)만큼 어둡게 만듭니다.
        /// </summary>
        public static Color Darken(this Color color, float factor)
        {
            factor = Mathf.Clamp01(factor);
            return Color.Lerp(color, Color.black, factor);
        }
    }
}
