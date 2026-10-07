using System;
using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// 게임 내 수치, 시간, 텍스트 포맷팅 유틸리티 및 문자열 확장 메서드 모음입니다.
    /// </summary>
    public static class FormatUtils
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

        /// <summary>
        /// 초(seconds) 단위 시간을 "MM:SS" 또는 "HH:MM:SS" 형식으로 변환합니다.
        /// </summary>
        /// <param name="seconds">초 단위 시간</param>
        /// <param name="includeHours">시간 단위 포함 여부</param>
        public static string FormatTime(float seconds, bool includeHours = false)
        {
            if (seconds < 0f) seconds = 0f;

            var timeSpan = TimeSpan.FromSeconds(seconds);

            return includeHours || timeSpan.Hours > 0
                ? $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// 밀리초까지 포함된 시간 형식 "MM:SS.ff"로 변환합니다.
        /// </summary>
        public static string FormatTimePrecise(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds / 10:D2}";
        }

        /// <summary>
        /// 정수 수치를 3자리마다 콤마가 포함된 문자열(예: 1,234,567)로 변환합니다.
        /// </summary>
        public static string FormatComma(long number)
        {
            return $"{number:N0}";
        }

        /// <summary>
        /// 큰 숫자를 단위 축약 형식(예: 1.2K, 3.5M, 10.0B)으로 변환합니다.
        /// </summary>
        /// <param name="value">축약할 수치</param>
        /// <param name="decimalPlaces">소수점 자릿수 (기본 1자리)</param>
        public static string FormatCompact(double value, int decimalPlaces = 1)
        {
            if (value < 0)
                return "-" + FormatCompact(-value, decimalPlaces);

            if (value < 1000)
                return value.ToString("0");

            int magnitude = 0;
            double current = value;

            while (current >= 1000 && magnitude < Suffixes.Length - 1)
            {
                current /= 1000.0;
                magnitude++;
            }

            string format = "{0:F" + decimalPlaces + "}{1}";
            return string.Format(format, current, Suffixes[magnitude]);
        }

        #region RichText Extensions
        /// <summary>
        /// 문자열에 Unity RichText 컬러 태그(&lt;color=#RRGGBB&gt;...&lt;/color&gt;)를 입힙니다.
        /// </summary>
        public static string WithColor(this string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }

        /// <summary>
        /// 문자열에 Unity RichText 굵게(&lt;b&gt;...&lt;/b&gt;) 태그를 입힙니다.
        /// </summary>
        public static string Bold(this string text)
        {
            return $"<b>{text}</b>";
        }

        /// <summary>
        /// 문자열에 Unity RichText 기울임(&lt;i&gt;...&lt;/i&gt;) 태그를 입힙니다.
        /// </summary>
        public static string Italic(this string text)
        {
            return $"<i>{text}</i>";
        }

        /// <summary>
        /// 문자열에 Unity RichText 크기(&lt;size=...&gt;...&lt;/size&gt;) 태그를 입힙니다.
        /// </summary>
        public static string WithSize(this string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }
        #endregion
    }
}
