using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// 문자열 셀 데이터를 대상 C# 타입으로 변환하는 순수 타입 변환 엔진입니다.
    /// <para>전역 상태를 갖지 않으며, <see cref="CSVParseConfig"/>에 등록된 로컬 컨버터 목록을 우선 실행합니다.</para>
    /// <para>기본형, Enum, Nullable, 배열/리스트, Unity 벡터/컬러 변환을 내장하며, 단일 string 생성자(GameTag 등) 자동 감지를 지원합니다.</para>
    /// </summary>
    public static class CSVTypeConverter
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> s_parseMethodCache = new();
        private static readonly ConcurrentDictionary<Type, ConstructorInfo> s_stringCtorCache = new();

        /// <summary>
        /// 문자열 값을 주어진 대상 타입으로 안전하게 변환합니다.
        /// </summary>
        /// <param name="rawValue">변환할 원본 문자열</param>
        /// <param name="targetType">목표 타입</param>
        /// <param name="config">파서 설정 객체 (로컬 커스텀 컨버터 및 구분자 보유)</param>
        /// <param name="result">변환 결과 객체</param>
        /// <param name="errorMessage">실패 시 에러 메시지</param>
        /// <returns>변환 성공 여부</returns>
        public static bool TryConvert(string rawValue, Type targetType, CSVParseConfig config, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = null;

            if (targetType == null)
            {
                errorMessage = "Target type is null.";
                return false;
            }

            char[] arraySeparators = config?.ArraySeparators ?? new[] { '|', ';' };

            // 1. CSVParseConfig에 등록된 로컬 커스텀 컨버터 우선 확인
            if (config != null && config.TryGetConverter(targetType, out var customConverter))
            {
                try
                {
                    result = customConverter(rawValue);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Custom converter for '{targetType.Name}' failed: {ex.Message}";
                    return false;
                }
            }

            // 2. string 자체인 경우
            if (targetType == typeof(string))
            {
                result = rawValue;
                return true;
            }

            // 3. Nullable<T> 처리
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    result = null;
                    return true;
                }
                return TryConvert(rawValue, underlyingType, config, out result, out errorMessage);
            }

            // 4. 빈 문자열에 대한 값 타입 / 참조 타입 기본 처리
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                result = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                return true;
            }

            // 5. Enum 처리
            if (targetType.IsEnum)
            {
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int enumInt))
                {
                    result = Enum.ToObject(targetType, enumInt);
                    return true;
                }

                try
                {
                    result = Enum.Parse(targetType, rawValue, ignoreCase: true);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Cannot parse '{rawValue}' as enum '{targetType.Name}': {ex.Message}";
                    return false;
                }
            }

            // 6. 기본 원시 타입 (Primitive Types)
            if (TryConvertPrimitive(rawValue, targetType, out result))
            {
                return true;
            }

            // 7. 배열 (T[]) 처리
            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                return TryConvertArray(rawValue, elementType, config, arraySeparators, out result, out errorMessage);
            }

            // 8. 제네릭 List<T> 처리
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = targetType.GetGenericArguments()[0];
                return TryConvertList(rawValue, elementType, config, arraySeparators, out result, out errorMessage);
            }

            // 9. Unity 기본 구조체 (Vector2, Vector3, Color)
            if (TryConvertUnityTypes(rawValue, targetType, out result))
            {
                return true;
            }

            // 10. 지능형 자동 감지: 단일 string 인자 생성자 (GameTag 등)
            if (TryConvertViaStringConstructor(rawValue, targetType, out result))
            {
                return true;
            }

            // 11. 지능형 자동 감지: public static T Parse(string) 메서드
            if (TryConvertViaParseMethod(rawValue, targetType, out result))
            {
                return true;
            }

            // 12. Convert.ChangeType 최종 폴백
            try
            {
                result = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Cannot convert '{rawValue}' to type '{targetType.Name}': {ex.Message}";
                return false;
            }
        }

        #region Internal Converters

        private static bool TryConvertPrimitive(string raw, Type targetType, out object result)
        {
            result = null;
            var culture = CultureInfo.InvariantCulture;

            if (targetType == typeof(int))
            {
                if (int.TryParse(raw, NumberStyles.Integer, culture, out int v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(float))
            {
                if (float.TryParse(raw, NumberStyles.Float, culture, out float v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(bool))
            {
                if (raw.Equals("1") || raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Equals("yes", StringComparison.OrdinalIgnoreCase) || raw.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }
                if (raw.Equals("0") || raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw.Equals("no", StringComparison.OrdinalIgnoreCase) || raw.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
                if (bool.TryParse(raw, out bool b)) { result = b; return true; }
                return false;
            }
            if (targetType == typeof(long))
            {
                if (long.TryParse(raw, NumberStyles.Integer, culture, out long v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(double))
            {
                if (double.TryParse(raw, NumberStyles.Float, culture, out double v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(short))
            {
                if (short.TryParse(raw, NumberStyles.Integer, culture, out short v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(byte))
            {
                if (byte.TryParse(raw, NumberStyles.Integer, culture, out byte v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(uint))
            {
                if (uint.TryParse(raw, NumberStyles.Integer, culture, out uint v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(ulong))
            {
                if (ulong.TryParse(raw, NumberStyles.Integer, culture, out ulong v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(ushort))
            {
                if (ushort.TryParse(raw, NumberStyles.Integer, culture, out ushort v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(sbyte))
            {
                if (sbyte.TryParse(raw, NumberStyles.Integer, culture, out sbyte v)) { result = v; return true; }
                return false;
            }
            if (targetType == typeof(decimal))
            {
                if (decimal.TryParse(raw, NumberStyles.Number, culture, out decimal v)) { result = v; return true; }
                return false;
            }

            return false;
        }

        private static bool TryConvertArray(string raw, Type elementType, CSVParseConfig config, char[] arraySeparators, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = null;

            char[] seps = (arraySeparators != null && arraySeparators.Length > 0) ? arraySeparators : new[] { '|', ';' };
            string[] tokens = raw.Split(seps, StringSplitOptions.RemoveEmptyEntries);

            Array array = Array.CreateInstance(elementType, tokens.Length);
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (!TryConvert(token, elementType, config, out object elementValue, out errorMessage))
                {
                    errorMessage = $"Array element at index {i} failed: {errorMessage}";
                    return false;
                }
                array.SetValue(elementValue, i);
            }

            result = array;
            return true;
        }

        private static bool TryConvertList(string raw, Type elementType, CSVParseConfig config, char[] arraySeparators, out object result, out string errorMessage)
        {
            result = null;
            errorMessage = null;

            char[] seps = (arraySeparators != null && arraySeparators.Length > 0) ? arraySeparators : new[] { '|', ';' };
            string[] tokens = raw.Split(seps, StringSplitOptions.RemoveEmptyEntries);

            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType, tokens.Length);

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (!TryConvert(token, elementType, config, out object elementValue, out errorMessage))
                {
                    errorMessage = $"List element at index {i} failed: {errorMessage}";
                    return false;
                }
                list.Add(elementValue);
            }

            result = list;
            return true;
        }

        private static bool TryConvertUnityTypes(string raw, Type targetType, out object result)
        {
            result = null;
            var culture = CultureInfo.InvariantCulture;

            if (targetType == typeof(Vector2))
            {
                string[] parts = raw.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                    float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y))
                {
                    result = new Vector2(x, y);
                    return true;
                }
                return false;
            }

            if (targetType == typeof(Vector3))
            {
                string[] parts = raw.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 &&
                    float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                    float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y) &&
                    float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float z))
                {
                    result = new Vector3(x, y, z);
                    return true;
                }
                return false;
            }

            if (targetType == typeof(Color))
            {
                if (raw.StartsWith("#") && ColorUtility.TryParseHtmlString(raw, out Color colorFromHtml))
                {
                    result = colorFromHtml;
                    return true;
                }

                string[] parts = raw.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float r) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float g) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float b))
                    {
                        float a = 1f;
                        if (parts.Length >= 4)
                        {
                            float.TryParse(parts[3].Trim(), NumberStyles.Float, culture, out a);
                        }

                        if (r > 1f || g > 1f || b > 1f || a > 1f)
                        {
                            r /= 255f; g /= 255f; b /= 255f; a /= 255f;
                        }

                        result = new Color(r, g, b, a);
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        private static bool TryConvertViaStringConstructor(string raw, Type targetType, out object result)
        {
            result = null;

            if (!s_stringCtorCache.TryGetValue(targetType, out var ctor))
            {
                ctor = targetType.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string) },
                    null);

                s_stringCtorCache[targetType] = ctor;
            }

            if (ctor != null)
            {
                try
                {
                    result = ctor.Invoke(new object[] { raw });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryConvertViaParseMethod(string raw, Type targetType, out object result)
        {
            result = null;

            if (!s_parseMethodCache.TryGetValue(targetType, out var method))
            {
                method = targetType.GetMethod(
                    "Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                s_parseMethodCache[targetType] = method;
            }

            if (method != null && method.ReturnType == targetType)
            {
                try
                {
                    result = method.Invoke(null, new object[] { raw });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        #endregion
    }
}
