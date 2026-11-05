using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// CSV 파싱 결과와 진단 정보(성공/실패 수, 오류 목록) 및 편리한 쿼리 기능을 제공하는 전용 결과 컨테이너입니다.
    /// <para><see cref="IReadOnlyList{TData}"/>를 구현하여 foreach 순회 및 인덱스 조회가 즉시 가능합니다.</para>
    /// </summary>
    /// <typeparam name="TData">파싱된 데이터 모델 타입</typeparam>
    public class CSVParseResult<TData> : IReadOnlyList<TData>
    {
        private static readonly IReadOnlyList<TData> s_emptyRows = Array.Empty<TData>();
        private static readonly IReadOnlyList<CSVParseError> s_emptyErrors = Array.Empty<CSVParseError>();

        /// <summary> 성공적으로 파싱된 데이터 목록입니다. </summary>
        public IReadOnlyList<TData> Rows { get; }

        /// <summary> 파싱 과정에서 발생한 모든 에러 목록입니다. </summary>
        public IReadOnlyList<CSVParseError> Errors { get; }

        /// <summary> 파싱 대상이었던 데이터 행의 총 개수입니다. (헤더 행 제외) </summary>
        public int TotalRowCount { get; }

        /// <summary> 성공적으로 생성된 데이터 레코드 수입니다. </summary>
        public int SuccessCount => Rows.Count;

        /// <summary> 파싱에 실패하거나 오류가 발생한 레코드 수입니다. </summary>
        public int FailedCount => Errors.Count;

        /// <summary>
        /// 파싱이 완전히 성공했는지 여부입니다. (오류가 0건이고 성공 레코드가 1건 이상일 때 true)
        /// </summary>
        public bool IsValid => Errors.Count == 0 && Rows.Count > 0;

        /// <summary> 파싱 중 오류가 1건이라도 발생했는지 여부입니다. </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary> 성공 레코드 목록이 비어 있는지 여부입니다. </summary>
        public bool IsEmpty => Rows.Count == 0;

        #region IReadOnlyList Implementation

        public int Count => Rows.Count;

        public TData this[int index] => Rows[index];

        public IEnumerator<TData> GetEnumerator() => Rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Rows.GetEnumerator();

        #endregion

        #region Constructors

        public CSVParseResult(IReadOnlyList<TData> rows, IReadOnlyList<CSVParseError> errors, int totalRowCount)
        {
            Rows = rows ?? s_emptyRows;
            Errors = errors ?? s_emptyErrors;
            TotalRowCount = totalRowCount;
        }

        /// <summary> 빈 실패 결과를 생성합니다. </summary>
        public static CSVParseResult<TData> Empty()
        {
            return new CSVParseResult<TData>(s_emptyRows, s_emptyErrors, 0);
        }

        #endregion

        #region Query & Utility Helpers

        /// <summary>
        /// 성공적으로 파싱된 데이터를 고유 키 기반의 Dictionary로 즉시 변환합니다.
        /// </summary>
        /// <typeparam name="TKey">딕셔너리 키 타입</typeparam>
        /// <param name="keySelector">TData에서 키를 추출하는 람다 함수</param>
        /// <param name="comparer">키 비교자 (선택 사항)</param>
        public Dictionary<TKey, TData> ToDictionary<TKey>(Func<TData, TKey> keySelector, IEqualityComparer<TKey> comparer = null) where TKey : notnull
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var dict = comparer != null
                ? new Dictionary<TKey, TData>(Rows.Count, comparer)
                : new Dictionary<TKey, TData>(Rows.Count);

            int count = Rows.Count;
            for (int i = 0; i < count; i++)
            {
                var row = Rows[i];
                var key = keySelector(row);
                dict[key] = row;
            }

            return dict;
        }

        /// <summary>
        /// 성공적으로 파싱된 데이터를 그룹 키 기반의 ILookup으로 변환합니다. (동일 키에 여러 행이 매핑되는 경우)
        /// </summary>
        /// <typeparam name="TKey">그룹 키 타입</typeparam>
        /// <param name="keySelector">TData에서 그룹 키를 추출하는 람다 함수</param>
        public ILookup<TKey, TData> ToLookup<TKey>(Func<TData, TKey> keySelector)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            return Rows.ToLookup(keySelector);
        }

        /// <summary>
        /// 조건과 일치하는 첫 번째 요소를 검색합니다. (없을 경우 default 반환)
        /// </summary>
        public TData Find(Predicate<TData> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            int count = Rows.Count;
            for (int i = 0; i < count; i++)
            {
                if (match(Rows[i]))
                    return Rows[i];
            }

            return default;
        }

        /// <summary>
        /// 조건과 일치하는 모든 요소를 새 List로 필터링하여 반환합니다.
        /// </summary>
        public List<TData> FindAll(Predicate<TData> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var list = new List<TData>();
            int count = Rows.Count;
            for (int i = 0; i < count; i++)
            {
                if (match(Rows[i]))
                    list.Add(Rows[i]);
            }

            return list;
        }

        /// <summary>
        /// 첫 번째 요소를 가져옵니다. 빈 결과일 경우 default를 반환합니다.
        /// </summary>
        public TData FirstOrDefault()
        {
            return Rows.Count > 0 ? Rows[0] : default;
        }

        /// <summary>
        /// 조건과 일치하는 첫 번째 요소를 가져옵니다. 일치 항목이 없으면 default를 반환합니다.
        /// </summary>
        public TData FirstOrDefault(Predicate<TData> match)
        {
            return Find(match);
        }

        /// <summary>
        /// 발생한 모든 파싱 오류를 Unity 콘솔에 로깅합니다.
        /// </summary>
        /// <param name="customLogger">커스텀 로거 델리게이트 (기본값: Debug.LogError)</param>
        public void LogErrors(Action<string> customLogger = null)
        {
            if (!HasErrors) return;

            var logAction = customLogger ?? (msg => Debug.LogError(msg));
            int errorCount = Errors.Count;
            for (int i = 0; i < errorCount; i++)
            {
                logAction($"[{typeof(TData).Name}] {Errors[i]}");
            }
        }

        #endregion
    }
}
