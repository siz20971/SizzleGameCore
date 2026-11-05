using System;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// CSV 파싱 동작을 제어하는 설정 옵션 클래스입니다.
    /// <para>권장: <see cref="CSVParseConfig"/>를 직접 사용하십시오.</para>
    /// </summary>
    [Obsolete("Use CSVParseConfig instead of CSVParserOptions.")]
    public class CSVParserOptions : CSVParseConfig
    {
        public new static CSVParserOptions Default => new CSVParserOptions();
        public new static CSVParserOptions TSV => new CSVParserOptions { Delimiter = '\t' };
    }
}
