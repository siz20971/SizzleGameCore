using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// CSV 파싱 규칙, 포맷 옵션 및 커스텀 타입 컨버터 목록을 보유하는 설정 객체입니다.
    /// <para>프로젝트 전역에서 공용 설정을 정의해 두고 재사용하거나, 테이블별로 필요한 규칙을 독립적으로 구성할 수 있습니다.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// // 1) 프로젝트 공용 Config 생성 및 컨버터 등록
    /// var gameConfig = new CSVParseConfig()
    ///     .AddConverter&lt;GameTag&gt;(raw => new GameTag(raw))
    ///     .AddConverter&lt;StatType&gt;(raw => Enum.Parse&lt;StatType&gt;(raw, true));
    ///
    /// // 2) 파싱 시 Config 주입
    /// var characters = CSVParser.Parse&lt;CharacterData&gt;(csv1, gameConfig);
    /// var skills = CSVParser.Parse&lt;SkillData&gt;(csv2, gameConfig);
    /// </code>
    /// </example>
    public class CSVParseConfig
    {
        private readonly Dictionary<Type, Func<string, object>> _customConverters = new();

        /// <summary> 열 구분자 문자입니다. (기본값: 콤마 ',') TSV인 경우 '\t'로 설정할 수 있습니다. </summary>
        public char Delimiter { get; set; } = ',';

        /// <summary> 헤더 행의 개수입니다. (기본값: 1 - 첫 번째 행이 컬럼 헤더) </summary>
        public int HeaderRowCount { get; set; } = 1;

        /// <summary> # 또는 // 로 시작하는 주석 행을 건너뛸지 여부입니다. (기본값: true) </summary>
        public bool SkipComments { get; set; } = true;

        /// <summary>
        /// [CSVColumn] 어트리뷰트가 명시된 멤버만 바인딩할지 여부입니다. (기본값: false)
        /// <para>false일 경우, 어트리뷰트가 없는 멤버도 변수명/프로퍼티명(m_, _ 접두사 자동 처리)과 컬럼명을 비교하여 매핑합니다.</para>
        /// <para>true일 경우, 반드시 [CSVColumn] 어트리뷰트가 선언된 멤버만 매핑 대상이 됩니다.</para>
        /// </summary>
        public bool RequireAttribute { get; set; } = false;

        /// <summary>
        /// 파싱 또는 타입 변환 실패 시 즉시 예외(FormatException 등)를 발생시킬지 여부입니다. (기본값: false)
        /// <para>false일 경우, 오류는 <see cref="CSVParseResult{TData}.Errors"/>에 기록되고 다음 행 파싱을 계속 진행합니다.</para>
        /// </summary>
        public bool ThrowOnError { get; set; } = false;

        /// <summary> 셀 값의 앞뒤 공백을 자동으로 Trim할지 여부입니다. (기본값: true) </summary>
        public bool TrimValues { get; set; } = true;

        /// <summary>
        /// 배열 및 리스트(List&lt;T&gt;) 필드 파싱 시 사용할 서브 구분자 목록입니다. (기본값: '|', ';')
        /// </summary>
        public char[] ArraySeparators { get; set; } = new[] { '|', ';' };

        #region Custom Converters

        /// <summary>
        /// 이 Config 인스턴스에 특정 타입에 대한 커스텀 파싱 람다를 등록합니다.
        /// </summary>
        /// <typeparam name="T">대상 데이터 타입</typeparam>
        /// <param name="converter">문자열을 받아 T 인스턴스를 반환하는 변환 람다</param>
        /// <returns>메서드 체이닝을 위한 Config 인스턴스</returns>
        public CSVParseConfig AddConverter<T>(Func<string, T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            _customConverters[typeof(T)] = raw => converter(raw);
            return this;
        }

        /// <summary>
        /// 이 Config 인스턴스에 Type을 지정하여 커스텀 파싱 람다를 등록합니다.
        /// </summary>
        public CSVParseConfig AddConverter(Type targetType, Func<string, object> converter)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            _customConverters[targetType] = converter;
            return this;
        }

        /// <summary>
        /// 등록된 커스텀 컨버터가 있는지 확인하고 가져옵니다.
        /// </summary>
        public bool TryGetConverter(Type targetType, out Func<string, object> converter)
        {
            return _customConverters.TryGetValue(targetType, out converter);
        }

        /// <summary>
        /// 등록된 특정 타입의 커스텀 컨버터를 제거합니다.
        /// </summary>
        public bool RemoveConverter<T>()
        {
            return _customConverters.Remove(typeof(T));
        }

        /// <summary>
        /// 등록된 모든 커스텀 컨버터를 비웁니다.
        /// </summary>
        public void ClearConverters()
        {
            _customConverters.Clear();
        }

        #endregion

        #region Fluent Builder Methods

        /// <summary> 구분자 문자를 변경합니다. </summary>
        public CSVParseConfig WithDelimiter(char delimiter)
        {
            Delimiter = delimiter;
            return this;
        }

        /// <summary> [CSVColumn] 어트리뷰트 필수 여부를 설정합니다. </summary>
        public CSVParseConfig WithRequireAttribute(bool require)
        {
            RequireAttribute = require;
            return this;
        }

        /// <summary> 에러 발생 시 예외 투척 여부를 설정합니다. </summary>
        public CSVParseConfig WithThrowOnError(bool throwOnError)
        {
            ThrowOnError = throwOnError;
            return this;
        }

        /// <summary> 배열 서브 구분자 목록을 변경합니다. </summary>
        public CSVParseConfig WithArraySeparators(params char[] separators)
        {
            if (separators != null && separators.Length > 0)
            {
                ArraySeparators = separators;
            }
            return this;
        }

        /// <summary>
        /// 현재 Config의 복사본을 생성합니다. (기존 컨버터 목록 포함)
        /// </summary>
        public CSVParseConfig Clone()
        {
            var copy = new CSVParseConfig
            {
                Delimiter = Delimiter,
                HeaderRowCount = HeaderRowCount,
                SkipComments = SkipComments,
                RequireAttribute = RequireAttribute,
                ThrowOnError = ThrowOnError,
                TrimValues = TrimValues,
                ArraySeparators = (char[])ArraySeparators.Clone()
            };

            foreach (var kvp in _customConverters)
            {
                copy._customConverters[kvp.Key] = kvp.Value;
            }

            return copy;
        }

        #endregion

        #region Presets

        /// <summary> 기본 CSV 파싱 설정 인스턴스를 생성하여 반환합니다. </summary>
        public static CSVParseConfig Default => new CSVParseConfig();

        /// <summary> 탭('\t') 구분자를 사용하는 TSV 전용 설정 인스턴스를 반환합니다. </summary>
        public static CSVParseConfig TSV => new CSVParseConfig { Delimiter = '\t' };

        #endregion
    }
}
