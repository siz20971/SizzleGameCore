using System;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// CSV 파싱 도중 발생한 행/셀 단위의 에러 상세 정보입니다.
    /// </summary>
    public readonly struct CSVParseError
    {
        /// <summary> 오류가 발생한 실제 CSV 행 번호 (1-based index) </summary>
        public int RowIndex { get; }

        /// <summary> 오류가 발생한 컬럼 이름 (헤더명) </summary>
        public string ColumnName { get; }

        /// <summary> 변환을 시도했던 원본 셀 문자열 </summary>
        public string RawValue { get; }

        /// <summary> 오류 원인 설명 메시지 </summary>
        public string Message { get; }

        /// <summary> 파싱 또는 변환 도중 발생한 내부 예외 객체 (없을 경우 null) </summary>
        public Exception Exception { get; }

        public CSVParseError(int rowIndex, string columnName, string rawValue, string message, Exception exception = null)
        {
            RowIndex = rowIndex;
            ColumnName = columnName ?? string.Empty;
            RawValue = rawValue ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public override string ToString()
        {
            string colInfo = string.IsNullOrEmpty(ColumnName) ? string.Empty : $", Col: '{ColumnName}'";
            string rawInfo = string.IsNullOrEmpty(RawValue) ? string.Empty : $", Raw: '{RawValue}'";
            return $"[CSV Error] Row: {RowIndex}{colInfo} - {Message}{rawInfo}";
        }
    }
}
