using System.Collections.Generic;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// 원시 데이터(문자열 등)를 파싱하여 TData 컬렉션으로 변환하는 테이블 파서 기본 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TData">파싱 대상 데이터 모델 타입</typeparam>
    public interface ITableParser<TData>
    {
        /// <summary>
        /// 원시 텍스트 데이터를 파싱하여 데이터 컬렉션을 반환합니다.
        /// </summary>
        /// <param name="rawContent">원시 문자열(CSV, JSON 등)</param>
        /// <returns>파싱된 TData 컬렉션</returns>
        IEnumerable<TData> Parse(string rawContent);
    }
}
