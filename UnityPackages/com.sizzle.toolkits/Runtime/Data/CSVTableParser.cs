using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// RFC 4180 표준을 준수하는 범용 CSV 테이블 파서입니다.
    /// <para>- 리플렉션 캐싱 기반 자동 필드 매핑 및 고성능 델리게이트 매핑 듀얼 지원</para>
    /// <para>- 따옴표 내 쉼표, 이스케이프 따옴표(""), 개행문자(\n) 완벽 처리</para>
    /// <para>- UTF-8 BOM(\uFEFF) 자동 제거 및 주석(#, //) 라인 건너뛰기 지원</para>
    /// </summary>
    /// <typeparam name="TData">파싱 대상 데이터 모델 타입</typeparam>
    public class CSVTableParser<TData> : ITableParser<TData>
    {
        private struct ColumnBinding
        {
            public int ColumnIndex;
            public FieldInfo Field;
            public PropertyInfo Property;
            public Type TargetType;
            public bool IsEnum;
        }

        private readonly Func<string[], TData> _customRowConverter;
        private readonly int _headerRowCount;
        private readonly char _delimiter;
        private readonly bool _hasHeader;
        private readonly bool _skipComments;

        /// <summary>
        /// 리플렉션을 통해 CSV 헤더와 클래스/구조체 필드명을 자동 매핑하는 파서를 생성합니다.
        /// </summary>
        /// <param name="headerRowCount">헤더 행의 개수 (기본 1: 첫 행이 헤더)</param>
        /// <param name="delimiter">구분자 문자 (기본 콤마 ',')</param>
        /// <param name="skipComments"># 또는 // 로 시작하는 주석 행 무시 여부</param>
        public CSVTableParser(int headerRowCount = 1, char delimiter = ',', bool skipComments = true)
        {
            _headerRowCount = System.Math.Max(0, headerRowCount);
            _delimiter = delimiter;
            _hasHeader = _headerRowCount > 0;
            _skipComments = skipComments;
            _customRowConverter = null;
        }

        /// <summary>
        /// 고성능/GC 최소화를 위한 커스텀 변환 델리게이트를 사용하는 파서를 생성합니다.
        /// </summary>
        /// <param name="customRowConverter">토큰 배열을 TData 인스턴스로 변환하는 람다/메서드</param>
        /// <param name="headerRowCount">헤더 행의 개수 (기본 1: 첫 행이 헤더)</param>
        /// <param name="delimiter">구분자 문자 (기본 콤마 ',')</param>
        /// <param name="skipComments"># 또는 // 로 시작하는 주석 행 무시 여부</param>
        public CSVTableParser(Func<string[], TData> customRowConverter, int headerRowCount = 1, char delimiter = ',', bool skipComments = true)
        {
            _customRowConverter = customRowConverter ?? throw new ArgumentNullException(nameof(customRowConverter));
            _headerRowCount = System.Math.Max(0, headerRowCount);
            _delimiter = delimiter;
            _hasHeader = _headerRowCount > 0;
            _skipComments = skipComments;
        }

        /// <summary>
        /// 원시 CSV 문자열을 파싱하여 TData 목록을 반환합니다.
        /// </summary>
        public IEnumerable<TData> Parse(string rawContent)
        {
            var rows = ParseCSVRows(rawContent, _delimiter, _skipComments);
            if (rows.Count == 0)
                return Array.Empty<TData>();

            if (_customRowConverter != null)
            {
                int startIndex = _hasHeader ? _headerRowCount : 0;
                var resultList = new List<TData>(System.Math.Max(0, rows.Count - startIndex));
                for (int i = startIndex; i < rows.Count; i++)
                {
                    var item = _customRowConverter(rows[i]);
                    if (item != null)
                        resultList.Add(item);
                }
                return resultList;
            }

            return ParseWithAutoMapping(rows);
        }

        #region Auto Mapping Implementation

        private List<TData> ParseWithAutoMapping(List<string[]> rows)
        {
            if (rows.Count <= _headerRowCount)
                return new List<TData>();

            string[] headers = rows[0];
            var bindings = BuildBindings(headers);

            int dataRowCount = rows.Count - _headerRowCount;
            var result = new List<TData>(dataRowCount);
            Type targetType = typeof(TData);

            for (int r = _headerRowCount; r < rows.Count; r++)
            {
                string[] row = rows[r];
                TData instance;

                try
                {
                    instance = (TData)Activator.CreateInstance(targetType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CSVTableParser] Failed to instantiate {targetType.Name}: {ex.Message}");
                    break;
                }

                for (int b = 0; b < bindings.Length; b++)
                {
                    ref var binding = ref bindings[b];
                    if (binding.ColumnIndex >= row.Length)
                        continue;

                    string cellValue = row[binding.ColumnIndex];
                    if (string.IsNullOrEmpty(cellValue) && binding.TargetType != typeof(string))
                        continue;

                    object convertedValue = ConvertValue(cellValue, binding.TargetType, binding.IsEnum);
                    if (binding.Field != null)
                    {
                        binding.Field.SetValue(instance, convertedValue);
                    }
                    else if (binding.Property != null && binding.Property.CanWrite)
                    {
                        binding.Property.SetValue(instance, convertedValue);
                    }
                }

                result.Add(instance);
            }

            return result;
        }

        private ColumnBinding[] BuildBindings(string[] headers)
        {
            Type type = typeof(TData);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = type.GetFields(flags);
            var props = type.GetProperties(flags);

            var list = new List<ColumnBinding>();

            for (int col = 0; col < headers.Length; col++)
            {
                string header = headers[col].Trim();
                if (string.IsNullOrEmpty(header))
                    continue;

                FieldInfo matchedField = null;
                for (int f = 0; f < fields.Length; f++)
                {
                    if (IsNameMatch(fields[f].Name, header))
                    {
                        matchedField = fields[f];
                        break;
                    }
                }

                if (matchedField != null)
                {
                    list.Add(new ColumnBinding
                    {
                        ColumnIndex = col,
                        Field = matchedField,
                        TargetType = matchedField.FieldType,
                        IsEnum = matchedField.FieldType.IsEnum
                    });
                    continue;
                }

                PropertyInfo matchedProp = null;
                for (int p = 0; p < props.Length; p++)
                {
                    if (props[p].CanWrite && IsNameMatch(props[p].Name, header))
                    {
                        matchedProp = props[p];
                        break;
                    }
                }

                if (matchedProp != null)
                {
                    list.Add(new ColumnBinding
                    {
                        ColumnIndex = col,
                        Property = matchedProp,
                        TargetType = matchedProp.PropertyType,
                        IsEnum = matchedProp.PropertyType.IsEnum
                    });
                }
            }

            return list.ToArray();
        }

        private static bool IsNameMatch(string memberName, string headerName)
        {
            if (string.Equals(memberName, headerName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Strip prefix 'm_'
            if (memberName.StartsWith("m_", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(memberName.Substring(2), headerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Strip prefix '_'
            if (memberName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(memberName.Substring(1), headerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object ConvertValue(string value, Type targetType, bool isEnum)
        {
            try
            {
                if (isEnum)
                {
                    if (int.TryParse(value, out int intVal))
                        return Enum.ToObject(targetType, intVal);
                    return Enum.Parse(targetType, value, true);
                }

                if (targetType == typeof(string))
                    return value;

                if (targetType == typeof(bool))
                {
                    if (value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (value.Equals("0") || value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return bool.Parse(value);
                }

                Type underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
                    return ConvertValue(value, underlyingType, underlyingType.IsEnum);
                }

                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CSVTableParser] Type conversion failed. Value: '{value}' -> Type: {targetType.Name} ({ex.Message})");
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        #endregion

        #region Low-level RFC 4180 CSV Tokenizer

        /// <summary>
        /// RFC 4180 표준 규칙을 준수하여 CSV 원시 텍스트를 행(row) 및 셀(cell) 단위로 토큰화합니다.
        /// </summary>
        public static List<string[]> ParseCSVRows(string rawContent, char delimiter = ',', bool skipComments = true)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(rawContent))
                return rows;

            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            bool insideQuotes = false;
            int length = rawContent.Length;

            for (int i = 0; i < length; i++)
            {
                char c = rawContent[i];

                if (c == '"')
                {
                    if (insideQuotes)
                    {
                        if (i + 1 < length && rawContent[i + 1] == '"')
                        {
                            currentCell.Append('"');
                            i++; // 이스케이프된 따옴표 건너뛰기
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else
                    {
                        insideQuotes = true;
                    }
                }
                else if (c == delimiter && !insideQuotes)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else if ((c == '\r' || c == '\n') && !insideQuotes)
                {
                    if (c == '\r' && i + 1 < length && rawContent[i + 1] == '\n')
                    {
                        i++; // CRLF의 LF 건너뛰기
                    }

                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    if (IsValidRow(currentRow, skipComments))
                    {
                        rows.Add(currentRow.ToArray());
                    }
                    currentRow.Clear();
                }
                else
                {
                    currentCell.Append(c);
                }
            }

            // 파일 끝에 개행문자가 없는 경우 마지막 행 플러시
            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                if (IsValidRow(currentRow, skipComments))
                {
                    rows.Add(currentRow.ToArray());
                }
            }

            // UTF-8 BOM(\uFEFF)이 첫 번째 셀 시작부에 있는 경우 제거
            if (rows.Count > 0 && rows[0].Length > 0 && rows[0][0].Length > 0 && rows[0][0][0] == '\uFEFF')
            {
                rows[0][0] = rows[0][0].TrimStart('\uFEFF');
            }

            return rows;
        }

        private static bool IsValidRow(List<string> row, bool skipComments)
        {
            if (row == null || row.Count == 0)
                return false;

            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
                return false;

            if (skipComments && row.Count > 0)
            {
                string first = row[0].TrimStart();
                if (first.StartsWith("#") || first.StartsWith("//"))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
