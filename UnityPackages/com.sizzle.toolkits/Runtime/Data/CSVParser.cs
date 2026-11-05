/*
 * ==============================================================================================
 * [CSVParser 사용 가이드 (How to use CSVParser)]
 *
 * 1. 개요
 *    CSVParser는 RFC 4180 표준을 준수하는 경량·고성능 CSV 파싱 엔진입니다.
 *    [CSVColumn] 어트리뷰트를 통해 클래스/구조체의 public/private 필드 및 private setter 프로퍼티를
 *    CSV 헤더 컬럼과 1:1 매핑할 수 있으며, 전용 결과 객체(CSVParseResult<TData>)를 통해
 *    파싱 성공/실패 통계, 상세 에러 진단 및 O(1) Dictionary 변환/검색 쿼리를 지원합니다.
 *
 * 2. 데이터 모델 클래스 작성 예시
 *    public class CharacterData
 *    {
 *        [CSVColumn("id", IsRequired = true)]
 *        public string Id;
 *
 *        [CSVColumn("name")]
 *        public string Name { get; private set; }
 *
 *        [CSVColumn("level", DefaultValue = 1)]
 *        private int m_level;
 *
 *        [CSVColumn("tag")]
 *        public GameTag Tag; // 단일 string 생성자(public GameTag(string)) 자동 감지 지원
 *
 *        [CSVColumn("rewards")]
 *        public int[] RewardIds; // "101|102|103" 파이프/세미콜론 자동 배열 변환
 *
 *        public int Level => m_level;
 *    }
 *
 * 3. 기본 사용법 (1줄 파싱)
 *    string csvText = "id,name,level\nID_01,Hero,10\nID_02,Mage,15";
 *    CSVParseResult<CharacterData> result = CSVParser.Parse<CharacterData>(csvText);
 *
 *    if (result.IsValid)
 *    {
 *        foreach (var data in result)
 *        {
 *            Debug.Log($"[{data.Id}] {data.Name} (Lv.{data.Level})");
 *        }
 *    }
 *    else
 *    {
 *        result.LogErrors(); // 문제 행/열/원인 상세 로그 출력
 *    }
 *
 * 4. CSVParseConfig를 활용한 커스텀 컨버터 및 옵션 주입 (권장 패턴)
 *    전역 상태를 사용하지 않고, 프로젝트 전용 Config 인스턴스를 생성해 두고 재사용하거나 주입합니다.
 *
 *    // (1) 프로젝트 공용 Config 생성 및 컨버터 등록
 *    public static class GameTableConfig
 *    {
 *        public static readonly CSVParseConfig Instance = new CSVParseConfig()
 *            .AddConverter<GameTag>(raw => new GameTag(raw))
 *            .AddConverter<StatType>(raw => Enum.Parse<StatType>(raw, true));
 *    }
 *
 *    // (2) Config를 전달하여 파싱 (테이블 간 컨버터 공유 및 완벽한 격리)
 *    var characters = CSVParser.Parse<CharacterData>(csv1, GameTableConfig.Instance);
 *    var skills = CSVParser.Parse<SkillData>(csv2, GameTableConfig.Instance);
 *
 * 5. DataTable 연동
 *    var result = CSVParser.Parse<CharacterData>(textAsset, GameTableConfig.Instance);
 *    table.Load(result, x => x.Id);
 * ==============================================================================================
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// RFC 4180 표준 호환 및 [CSVColumn] 어트리뷰트 기반 범용 CSV 파서입니다.
    /// <para>전역 상태 없이 <see cref="CSVParseConfig"/> 객체를 통해 파서 옵션 및 커스텀 컨버터를 주입받아 동작합니다.</para>
    /// </summary>
    public static class CSVParser
    {
        private sealed class MemberBinding
        {
            public string ColumnName;
            public FieldInfo Field;
            public PropertyInfo Property;
            public Type MemberType;
            public bool IsRequired;
            public object DefaultValue;

            public void SetValue(object target, object value)
            {
                if (Field != null)
                {
                    Field.SetValue(target, value);
                }
                else if (Property != null)
                {
                    Property.SetValue(target, value, null);
                }
            }
        }

        private sealed class TypeBindingPlan
        {
            public readonly Dictionary<string, MemberBinding> BindingsByColumnName;
            public readonly List<MemberBinding> RequiredBindings;

            public TypeBindingPlan(Dictionary<string, MemberBinding> bindings, List<MemberBinding> required)
            {
                BindingsByColumnName = bindings;
                RequiredBindings = required;
            }
        }

        private static readonly ConcurrentDictionary<(Type TargetType, bool RequireAttribute), TypeBindingPlan> s_planCache = new();

        #region Public Static API

        /// <summary>
        /// CSV 원시 텍스트를 파싱하여 <typeparamref name="TData"/> 모델 목록과 진단 정보를 담은 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="TData">기본 생성자(new())를 가진 데이터 모델 클래스 또는 구조체</typeparam>
        /// <param name="csvBody">파싱할 CSV 본문 문자열</param>
        /// <param name="config">파서 설정 객체 (null일 경우 <see cref="CSVParseConfig.Default"/> 적용)</param>
        /// <returns>파싱 결과 컨테이너 (<see cref="CSVParseResult{TData}"/>)</returns>
        public static CSVParseResult<TData> Parse<TData>(string csvBody, CSVParseConfig config = null)
        {
            config ??= CSVParseConfig.Default;

            if (string.IsNullOrEmpty(csvBody))
            {
                return CSVParseResult<TData>.Empty();
            }

            var rows = ParseCSVRows(csvBody, config.Delimiter, config.SkipComments);
            if (rows.Count <= config.HeaderRowCount)
            {
                return CSVParseResult<TData>.Empty();
            }

            string[] headerRow = rows[0];
            var plan = GetOrCreateBindingPlan(typeof(TData), config.RequireAttribute);

            // 헤더 컬럼 인덱스 매핑 구성
            var columnIndexToBinding = new MemberBinding[headerRow.Length];
            var boundColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int col = 0; col < headerRow.Length; col++)
            {
                string headerName = headerRow[col];
                if (config.TrimValues && headerName != null)
                {
                    headerName = headerName.Trim();
                }

                if (string.IsNullOrEmpty(headerName))
                    continue;

                if (plan.BindingsByColumnName.TryGetValue(headerName, out var binding))
                {
                    columnIndexToBinding[col] = binding;
                    boundColumns.Add(binding.ColumnName);
                }
            }

            var successRows = new List<TData>(rows.Count - config.HeaderRowCount);
            var errors = new List<CSVParseError>();

            // 필수 컬럼(IsRequired)이 CSV 헤더에 아예 없는지 사전 검사
            for (int i = 0; i < plan.RequiredBindings.Count; i++)
            {
                var req = plan.RequiredBindings[i];
                if (!boundColumns.Contains(req.ColumnName))
                {
                    var err = new CSVParseError(1, req.ColumnName, string.Empty, $"Required column '{req.ColumnName}' is missing from CSV header.");
                    errors.Add(err);

                    if (config.ThrowOnError)
                        throw new FormatException(err.ToString());
                }
            }

            Type dataType = typeof(TData);
            int dataRowCount = rows.Count - config.HeaderRowCount;

            // 데이터 행 루프
            for (int r = config.HeaderRowCount; r < rows.Count; r++)
            {
                int actualRowNumber = r + 1; // 1-based CSV 행 번호
                string[] row = rows[r];
                TData instance;

                try
                {
                    instance = (TData)Activator.CreateInstance(dataType);
                }
                catch (Exception ex)
                {
                    var err = new CSVParseError(actualRowNumber, string.Empty, string.Empty, $"Failed to instantiate {dataType.Name}: {ex.Message}", ex);
                    errors.Add(err);

                    if (config.ThrowOnError)
                        throw;

                    continue;
                }

                bool rowHasError = false;

                // 컬럼별 값 바인딩
                for (int col = 0; col < columnIndexToBinding.Length; col++)
                {
                    var binding = columnIndexToBinding[col];
                    if (binding == null) continue;

                    string rawCell = (col < row.Length) ? row[col] : string.Empty;
                    if (config.TrimValues && rawCell != null)
                    {
                        rawCell = rawCell.Trim();
                    }

                    // 필수값 빈 셀 체크
                    if (string.IsNullOrEmpty(rawCell))
                    {
                        if (binding.IsRequired)
                        {
                            var err = new CSVParseError(actualRowNumber, binding.ColumnName, rawCell, $"Required column '{binding.ColumnName}' has an empty value.");
                            errors.Add(err);
                            rowHasError = true;

                            if (config.ThrowOnError)
                                throw new FormatException(err.ToString());

                            continue;
                        }

                        // 기본값이 지정되어 있다면 대체
                        if (binding.DefaultValue != null)
                        {
                            binding.SetValue(instance, binding.DefaultValue);
                            continue;
                        }
                    }

                    // 타입 변환 시도 (config에 등록된 로컬 컨버터 우선 적용)
                    if (CSVTypeConverter.TryConvert(rawCell, binding.MemberType, config, out object convertedValue, out string errorMessage))
                    {
                        try
                        {
                            binding.SetValue(instance, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            var err = new CSVParseError(actualRowNumber, binding.ColumnName, rawCell, $"Failed to set member value: {ex.Message}", ex);
                            errors.Add(err);
                            rowHasError = true;

                            if (config.ThrowOnError)
                                throw;
                        }
                    }
                    else
                    {
                        var err = new CSVParseError(actualRowNumber, binding.ColumnName, rawCell, errorMessage ?? $"Failed to convert to {binding.MemberType.Name}.");
                        errors.Add(err);
                        rowHasError = true;

                        if (config.ThrowOnError)
                            throw new FormatException(err.ToString());
                    }
                }

                if (!rowHasError)
                {
                    successRows.Add(instance);
                }
            }

            return new CSVParseResult<TData>(successRows, errors, dataRowCount);
        }

        /// <summary>
        /// CSV 파싱을 안전하게 시도하고, 오류 없이 완전히 성공했는지 여부를 bool로 반환합니다.
        /// </summary>
        /// <typeparam name="TData">데이터 모델 타입</typeparam>
        /// <param name="csvBody">CSV 문자열</param>
        /// <param name="result">생성된 파싱 결과 객체</param>
        /// <param name="config">파서 설정 객체</param>
        /// <returns>오류가 0건이고 데이터가 1건 이상 로드되었으면 true</returns>
        public static bool TryParse<TData>(string csvBody, out CSVParseResult<TData> result, CSVParseConfig config = null)
        {
            result = Parse<TData>(csvBody, config);
            return result.IsValid;
        }

        /// <summary>
        /// Unity <see cref="TextAsset"/> 에셋으로부터 CSV를 파싱합니다.
        /// </summary>
        /// <typeparam name="TData">데이터 모델 타입</typeparam>
        /// <param name="textAsset">파싱할 Unity TextAsset</param>
        /// <param name="config">파서 설정 객체</param>
        /// <returns>파싱 결과 객체</returns>
        public static CSVParseResult<TData> Parse<TData>(TextAsset textAsset, CSVParseConfig config = null)
        {
            if (textAsset == null)
            {
                Debug.LogError($"[CSVParser] TextAsset is null for {typeof(TData).Name}.");
                return CSVParseResult<TData>.Empty();
            }

            return Parse<TData>(textAsset.text, config);
        }

        /// <summary>
        /// 로컬 파일 시스템 경로의 CSV 파일을 읽어 파싱합니다.
        /// </summary>
        /// <typeparam name="TData">데이터 모델 타입</typeparam>
        /// <param name="filePath">절대 또는 상대 파일 경로</param>
        /// <param name="config">파서 설정 객체</param>
        /// <param name="encoding">텍스트 인코딩 (기본값: UTF-8)</param>
        /// <returns>파싱 결과 객체</returns>
        public static CSVParseResult<TData> ParseFile<TData>(string filePath, CSVParseConfig config = null, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
            {
                var errors = new[] { new CSVParseError(0, string.Empty, filePath, $"File does not exist: {filePath}") };
                return new CSVParseResult<TData>(Array.Empty<TData>(), errors, 0);
            }

            string content = File.ReadAllText(filePath, encoding ?? Encoding.UTF8);
            return Parse<TData>(content, config);
        }

        #endregion

        #region Reflection & Binding Plan

        private static TypeBindingPlan GetOrCreateBindingPlan(Type targetType, bool requireAttribute)
        {
            return s_planCache.GetOrAdd((targetType, requireAttribute), key => CreateBindingPlan(key.TargetType, key.RequireAttribute));
        }

        private static TypeBindingPlan CreateBindingPlan(Type targetType, bool requireAttribute)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = targetType.GetFields(flags);
            var properties = targetType.GetProperties(flags);

            var bindingsByColumn = new Dictionary<string, MemberBinding>(StringComparer.OrdinalIgnoreCase);
            var requiredBindings = new List<MemberBinding>();

            // 1. Fields 탐색
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var attr = field.GetCustomAttribute<CSVColumnAttribute>(true);

                if (attr != null)
                {
                    var binding = new MemberBinding
                    {
                        ColumnName = attr.ColumnName,
                        Field = field,
                        MemberType = field.FieldType,
                        IsRequired = attr.IsRequired,
                        DefaultValue = attr.DefaultValue
                    };
                    bindingsByColumn[attr.ColumnName] = binding;
                    if (attr.IsRequired) requiredBindings.Add(binding);
                }
                else if (!requireAttribute)
                {
                    string candidateName = CleanMemberName(field.Name);
                    var binding = new MemberBinding
                    {
                        ColumnName = candidateName,
                        Field = field,
                        MemberType = field.FieldType,
                        IsRequired = false,
                        DefaultValue = null
                    };
                    bindingsByColumn[candidateName] = binding;
                }
            }

            // 2. Properties 탐색
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var attr = prop.GetCustomAttribute<CSVColumnAttribute>(true);

                if (prop.SetMethod == null)
                    continue;

                if (attr != null)
                {
                    var binding = new MemberBinding
                    {
                        ColumnName = attr.ColumnName,
                        Property = prop,
                        MemberType = prop.PropertyType,
                        IsRequired = attr.IsRequired,
                        DefaultValue = attr.DefaultValue
                    };
                    bindingsByColumn[attr.ColumnName] = binding;
                    if (attr.IsRequired) requiredBindings.Add(binding);
                }
                else if (!requireAttribute)
                {
                    string candidateName = CleanMemberName(prop.Name);
                    if (!bindingsByColumn.ContainsKey(candidateName))
                    {
                        var binding = new MemberBinding
                        {
                            ColumnName = candidateName,
                            Property = prop,
                            MemberType = prop.PropertyType,
                            IsRequired = false,
                            DefaultValue = null
                        };
                        bindingsByColumn[candidateName] = binding;
                    }
                }
            }

            return new TypeBindingPlan(bindingsByColumn, requiredBindings);
        }

        private static string CleanMemberName(string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return string.Empty;

            if (memberName.StartsWith("m_", StringComparison.OrdinalIgnoreCase) && memberName.Length > 2)
            {
                return memberName.Substring(2);
            }
            if (memberName.StartsWith("_", StringComparison.OrdinalIgnoreCase) && memberName.Length > 1)
            {
                return memberName.Substring(1);
            }

            return memberName;
        }

        #endregion

        #region RFC 4180 CSV Tokenizer Core

        /// <summary>
        /// RFC 4180 표준 규격을 준수하여 따옴표 이스케이프(""), 셀 내 개행 및 쉼표를 완벽히 토큰화합니다.
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
                            i++; // 이스케이프 따옴표 건너뛰기
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
                        i++; // CRLF
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

            // 마지막 행 플러시
            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell.ToString());
                if (IsValidRow(currentRow, skipComments))
                {
                    rows.Add(currentRow.ToArray());
                }
            }

            // UTF-8 BOM(\uFEFF) 시작부 제거
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
