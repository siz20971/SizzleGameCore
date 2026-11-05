using System;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// CSV 컬럼 헤더와 데이터 모델 클래스의 멤버(필드 또는 프로퍼티)를 매핑하는 어트리뷰트입니다.
    /// <para>public/private 필드 및 private setter 프로퍼티 모두 지원합니다.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// public class CharacterData
    /// {
    ///     [CSVColumn("id", IsRequired = true)]
    ///     public string Id;
    ///
    ///     [CSVColumn("name")]
    ///     public string Name { get; private set; }
    ///
    ///     [CSVColumn("level", DefaultValue = 1)]
    ///     private int m_level;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CSVColumnAttribute : Attribute
    {
        /// <summary> 매핑할 CSV 컬럼(헤더) 이름입니다. </summary>
        public string ColumnName { get; }

        /// <summary>
        /// 필수 컬럼 여부입니다. (기본값: false)
        /// <para>true일 경우, CSV 헤더에 컬럼이 없거나 셀 값이 비어있으면 파싱 오류로 기록됩니다.</para>
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// CSV 셀 값이 비어있을 때 대신 할당할 기본값입니다. (기본값: null)
        /// <para>미지정 시 타입의 default 값이 사용됩니다.</para>
        /// </summary>
        public object DefaultValue { get; set; } = null;

        /// <summary>
        /// 매핑할 CSV 컬럼 이름을 지정하여 어트리뷰트를 초기화합니다.
        /// </summary>
        /// <param name="columnName">CSV 헤더의 컬럼 이름</param>
        public CSVColumnAttribute(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));

            ColumnName = columnName;
        }
    }
}
