/*
 * ==============================================================================================
 * [DataTable<TKey, TData> 사용법 및 예시]
 *
 * 1. 개요
 *    - 단일 데이터 모델(TData)과 고유 키(TKey)를 매핑하여 보관 및 관리하는 데이터 테이블 추상 기본 클래스입니다.
 *    - O(1) Key 검색, 인덱스 기반 순차 접근, Zero-Allocation foreach 및 필터링 쿼리를 지원합니다.
 *    - CSV, JSON 등 실제 데이터 소스의 취득 및 파싱은 상속받은 구체적 Table 클래스의 Load()에서 구현합니다.
 *
 * 2. Table 상속 클래스 구현 예시 (CSV/JSON 데이터 소스 + ITableParser 연동)
 *    public class ItemData
 *    {
 *        public int Id;
 *        public string Name;
 *        public int Price;
 *    }
 *
 *    public class ItemTable : DataTable<int, ItemData>
 *    {
 *        private readonly string _resourcePath;
 *
 *        public ItemTable(string resourcePath = "Datas/ItemTable")
 *        {
 *            _resourcePath = resourcePath;
 *        }
 *
 *        public override void Load()
 *        {
 *            TextAsset textAsset = Resources.Load<TextAsset>(_resourcePath);
 *            if (textAsset == null) return;
 *
 *            // (방법 1) CSVTableParser를 사용한 1줄 로드 (자동 매핑 + Key 람다)
 *            Load(textAsset, new CSVTableParser<ItemData>(), item => item.Id);
 *
 *            // (방법 2) JSON 포맷인 경우:
 *            // Load(textAsset, new JSONTableParser<ItemData>(), item => item.Id);
 *        }
 *    }
 *
 * 3. 런타임 사용 예시
 *    var table = new ItemTable();
 *    table.Load();
 *
 *    // (1) O(1) 안전 조회 (GC Alloc 0 Byte, Key 미존재 시 예외 없이 false 반환)
 *    if (table.TryGet(1001, out var item))
 *    {
 *        Debug.Log(item.Name);
 *    }
 *
 *    // (2) 인덱서 접근
 *    var firstItem = table[1001];
 *
 *    // (3) foreach 순회 (값 형식 열거자 바인딩으로 GC Alloc 0 Byte)
 *    foreach (var data in table)
 *    {
 *        // data 처리
 *    }
 *
 *    // (4) 고속 필터링 (외부 캐싱 리스트를 전달하여 매 프레임 new List 생성 방지)
 *    List<ItemData> buffer = new List<ItemData>();
 *    table.FindAll(x => x.Price >= 500, buffer);
 * ==============================================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// 데이터 테이블 공통 라이프사이클 및 기본 속성을 정의하는 비제네릭 인터페이스입니다.
    /// </summary>
    public interface IDataTable
    {
        /// <summary> 데이터 로드가 완료되었는지 여부 </summary>
        bool IsLoaded { get; }

        /// <summary> 테이블에 로드된 전체 레코드 수 </summary>
        int Count { get; }

        /// <summary> 테이블 데이터를 로드합니다. </summary>
        void Load();

        /// <summary> 테이블 데이터를 메모리에서 해제합니다. </summary>
        void Unload();
    }

    /// <summary>
    /// 자유로운 TKey와 TData를 설정하여 단일 데이터 타입의 데이터를 보유하는 고성능 데이터 테이블 추상 기본 클래스입니다.
    /// <para>O(1) 키 기반 룩업, 인덱스 접근, Zero-Allocation foreach 열거 및 고속 필터링을 지원합니다.</para>
    /// </summary>
    /// <typeparam name="TKey">데이터를 식별하는 고유 키 타입 (int, string, enum 등)</typeparam>
    /// <typeparam name="TData">테이블이 보관하는 데이터 모델 타입</typeparam>
    public abstract class DataTable<TKey, TData> : IDataTable, IReadOnlyList<TData> where TKey : notnull
    {
        private readonly Dictionary<TKey, TData> _dataDict;
        private readonly List<TData> _dataList;
        protected readonly object SyncLock = new object();

        #region Properties & Events

        /// <summary> 데이터가 성공적으로 로드되었는지 여부입니다. </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary> 테이블에 적재된 데이터 항목의 총 개수입니다. </summary>
        public int Count => _dataList.Count;

        /// <summary> 테이블이 비어 있는지 여부입니다. </summary>
        public bool IsEmpty => _dataList.Count == 0;

        /// <summary> 로드된 모든 데이터 항목의 읽기 전용 리스트입니다. (인덱스 순차 접근용) </summary>
        public IReadOnlyList<TData> All => _dataList;

        /// <summary> 키-값 쌍으로 매핑된 읽기 전용 딕셔너리입니다. </summary>
        public IReadOnlyDictionary<TKey, TData> Table => _dataDict;

        /// <summary> 등록된 모든 키의 컬렉션입니다. </summary>
        public Dictionary<TKey, TData>.KeyCollection Keys => _dataDict.Keys;

        /// <summary> 등록된 모든 데이터 값의 컬렉션입니다. </summary>
        public Dictionary<TKey, TData>.ValueCollection Values => _dataDict.Values;

        /// <summary> 테이블 데이터가 로드 완료되었을 때 발생하는 이벤트입니다. </summary>
        public event Action OnLoaded;

        /// <summary> 테이블 데이터가 비워졌을 때(Clear) 발생하는 이벤트입니다. </summary>
        public event Action OnCleared;

        #endregion

        #region Constructors

        protected DataTable(IEqualityComparer<TKey> comparer = null)
        {
            _dataDict = comparer != null ? new Dictionary<TKey, TData>(comparer) : new Dictionary<TKey, TData>();
            _dataList = new List<TData>();
        }

        protected DataTable(int initialCapacity, IEqualityComparer<TKey> comparer = null)
        {
            int cap = System.Math.Max(0, initialCapacity);
            _dataDict = comparer != null ? new Dictionary<TKey, TData>(cap, comparer) : new Dictionary<TKey, TData>(cap);
            _dataList = new List<TData>(cap);
        }

        #endregion

        #region Indexers & Lookup Methods

        /// <summary> 키를 통해 데이터에 접근하는 인덱서입니다. (미존재 시 KeyNotFoundException 발생) </summary>
        public TData this[TKey key] => Get(key);

        TData IReadOnlyList<TData>.this[int index] => _dataList[index];

        /// <summary>
        /// 키를 통해 데이터를 가져옵니다. 키가 존재하지 않으면 KeyNotFoundException을 던집니다.
        /// </summary>
        public TData Get(TKey key)
        {
            if (_dataDict.TryGetValue(key, out var data))
                return data;

            throw new KeyNotFoundException($"[{GetType().Name}] Key '{key}' was not found in table.");
        }

        /// <summary>
        /// GC 할당 없이 키로 데이터를 안전하게 조회합니다.
        /// </summary>
        /// <param name="key">조회할 키</param>
        /// <param name="data">조회된 데이터 (없으면 default)</param>
        /// <returns>키 존재 여부</returns>
        public bool TryGet(TKey key, out TData data)
        {
            return _dataDict.TryGetValue(key, out data);
        }

        /// <summary>
        /// 키로 데이터를 조회하며, 키가 없을 경우 지정한 기본값을 반환합니다.
        /// </summary>
        public TData GetOrDefault(TKey key, TData defaultValue = default)
        {
            return _dataDict.TryGetValue(key, out var data) ? data : defaultValue;
        }

        /// <summary> 특정 키가 테이블에 포함되어 있는지 확인합니다. </summary>
        public bool ContainsKey(TKey key) => _dataDict.ContainsKey(key);

        /// <summary> 특정 데이터 값이 테이블에 포함되어 있는지 확인합니다. </summary>
        public bool ContainsValue(TData data) => _dataList.Contains(data);

        /// <summary>
        /// 리스트 인덱스 번호(0 ~ Count - 1)로 데이터를 가져옵니다.
        /// </summary>
        public TData GetByIndex(int index)
        {
            if ((uint)index >= (uint)_dataList.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"[{GetType().Name}] Index {index} is out of range. (Count: {_dataList.Count})");

            return _dataList[index];
        }

        /// <summary>
        /// 리스트 인덱스 번호로 데이터를 안전하게 가져옵니다.
        /// </summary>
        public bool TryGetByIndex(int index, out TData data)
        {
            if ((uint)index < (uint)_dataList.Count)
            {
                data = _dataList[index];
                return true;
            }

            data = default;
            return false;
        }

        #endregion

        #region Queries

        /// <summary> 조건과 일치하는 첫 번째 데이터 요소를 검색합니다. </summary>
        public TData Find(Predicate<TData> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            int count = _dataList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = _dataList[i];
                if (match(item))
                    return item;
            }
            return default;
        }

        /// <summary> 조건과 일치하는 첫 번째 데이터의 인덱스를 검색합니다. (없으면 -1) </summary>
        public int FindIndex(Predicate<TData> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            int count = _dataList.Count;
            for (int i = 0; i < count; i++)
            {
                if (match(_dataList[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// GC 할당 없이 호출자의 기존 리스트 버퍼를 재사용하여 검색 결과를 채웁니다.
        /// </summary>
        public void FindAll(Predicate<TData> match, List<TData> results)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            int count = _dataList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = _dataList[i];
                if (match(item))
                    results.Add(item);
            }
        }

        /// <summary> 조건과 일치하는 모든 데이터를 새 리스트로 반환합니다. </summary>
        public List<TData> FindAll(Predicate<TData> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var results = new List<TData>();
            FindAll(match, results);
            return results;
        }

        /// <summary> 조건과 일치하는 요소가 하나라도 존재하는지 확인합니다. </summary>
        public bool Exists(Predicate<TData> match) => FindIndex(match) != -1;

        /// <summary> 테이블의 모든 요소에 대해 지정한 액션을 실행합니다. </summary>
        public void ForEach(Action<TData> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            int count = _dataList.Count;
            for (int i = 0; i < count; i++)
            {
                action(_dataList[i]);
            }
        }

        #endregion

        #region Enumerator

        // foreach 순회 시 박싱 없는 struct 열거자 반환 (GC Alloc 0 byte)
        public List<TData>.Enumerator GetEnumerator() => _dataList.GetEnumerator();

        IEnumerator<TData> IEnumerable<TData>.GetEnumerator() => _dataList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dataList.GetEnumerator();

        #endregion

        #region Lifecycle

        /// <summary>
        /// 데이터 소스로부터 테이블 데이터를 로드하는 추상 메서드입니다. 하위 클래스에서 데이터 경로 및 파서를 지정하여 구현합니다.
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// 원시 텍스트와 파서, 키 선택 람다식을 받아 데이터를 채우는 공용 로드 메서드입니다.
        /// </summary>
        /// <param name="rawContent">원시 텍스트 (CSV, JSON 등)</param>
        /// <param name="parser">ITableParser 구현체</param>
        /// <param name="keySelector">TData에서 TKey를 추출하는 람다식</param>
        public virtual void Load(string rawContent, ITableParser<TData> parser, Func<TData, TKey> keySelector)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            Clear();

            var items = parser.Parse(rawContent);
            if (items is ICollection<TData> collection)
            {
                Reserve(collection.Count);
            }

            foreach (var item in items)
            {
                if (item != null)
                {
                    Add(keySelector(item), item);
                }
            }

            MarkLoaded();
        }

        /// <summary>
        /// 원시 텍스트와 파서를 받아 데이터를 채웁니다. 키는 오버라이드된 GetKey(TData)를 통해 추출합니다.
        /// </summary>
        /// <param name="rawContent">원시 텍스트 (CSV, JSON 등)</param>
        /// <param name="parser">ITableParser 구현체</param>
        public virtual void Load(string rawContent, ITableParser<TData> parser)
        {
            Load(rawContent, parser, GetKey);
        }

        /// <summary>
        /// Unity TextAsset 에셋과 파서를 받아 데이터를 로드합니다.
        /// </summary>
        /// <param name="asset">로드할 TextAsset</param>
        /// <param name="parser">ITableParser 구현체</param>
        /// <param name="keySelector">키 선택 람다식 (null일 경우 GetKey 사용)</param>
        public virtual void Load(TextAsset asset, ITableParser<TData> parser, Func<TData, TKey> keySelector = null)
        {
            if (asset == null)
            {
                Debug.LogError($"[{GetType().Name}] TextAsset is null.");
                return;
            }

            if (keySelector != null)
                Load(asset.text, parser, keySelector);
            else
                Load(asset.text, parser, GetKey);
        }

        /// <summary> 테이블 데이터를 메모리에서 해제하고 상태를 비웁니다. </summary>
        public virtual void Unload() => Clear();

        /// <summary> 기존 데이터를 비우고 다시 로드합니다. </summary>
        public virtual void Reload()
        {
            Clear();
            Load();
        }

        /// <summary> 테이블 내의 모든 딕셔너리 및 리스트 데이터를 비우고 IsLoaded를 false로 초기화합니다. </summary>
        public virtual void Clear()
        {
            lock (SyncLock)
            {
                _dataDict.Clear();
                _dataList.Clear();
                IsLoaded = false;
                OnCleared?.Invoke();
            }
        }

        #endregion

        #region Protected Helpers

        /// <summary>
        /// 파싱 전 버퍼 용량을 미리 확보하여 내부 재할당 및 해시 재계산을 방지합니다.
        /// </summary>
        protected void Reserve(int capacity)
        {
            if (capacity <= 0) return;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            _dataDict.EnsureCapacity(capacity);
#endif
            if (_dataList.Capacity < capacity)
                _dataList.Capacity = capacity;
        }

        protected bool Add(TKey key, TData data)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (SyncLock)
            {
                if (_dataDict.ContainsKey(key))
                {
                    Debug.LogWarning($"[{GetType().Name}] Duplicate key '{key}' ignored.");
                    return false;
                }

                _dataDict.Add(key, data);
                _dataList.Add(data);
                return true;
            }
        }

        protected void AddOrUpdate(TKey key, TData data)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            lock (SyncLock)
            {
                if (_dataDict.ContainsKey(key))
                {
                    _dataDict[key] = data;
                    int idx = _dataList.FindIndex(item => EqualityComparer<TKey>.Default.Equals(GetKey(item), key));
                    if (idx >= 0)
                        _dataList[idx] = data;
                    else
                        _dataList.Add(data);
                }
                else
                {
                    _dataDict.Add(key, data);
                    _dataList.Add(data);
                }
            }
        }

        protected bool Remove(TKey key)
        {
            if (key == null) return false;

            lock (SyncLock)
            {
                if (_dataDict.TryGetValue(key, out var data))
                {
                    _dataDict.Remove(key);
                    _dataList.Remove(data);
                    return true;
                }
                return false;
            }
        }

        protected virtual TKey GetKey(TData data) => default;

        protected void MarkLoaded()
        {
            IsLoaded = true;
            OnLoaded?.Invoke();
        }

        #endregion
    }
}
