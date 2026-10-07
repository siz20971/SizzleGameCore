using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// 가중치(Weight) 확률에 따라 요소를 무작위 추첨하는 제네릭 유틸리티입니다.
    /// 누적합(Cumulative Sum) 기반 이진 탐색으로 O(log N)의 빠른 추첨을 보장합니다.
    /// </summary>
    /// <typeparam name="T">추첨할 항목의 타입</typeparam>
    public class WeightedRandomPicker<T>
    {
        private readonly struct Entry
        {
            public readonly T Item;
            public readonly float Weight;
            public readonly float CumulativeWeight;

            public Entry(T item, float weight, float cumulativeWeight)
            {
                Item = item;
                Weight = weight;
                CumulativeWeight = cumulativeWeight;
            }
        }

        private readonly List<Entry> m_entries = new();
        private float m_totalWeight = 0f;
        private readonly Random m_random;

        /// <summary>등록된 항목의 총 가중치 합계입니다.</summary>
        public float TotalWeight => m_totalWeight;

        /// <summary>등록된 항목의 총 개수입니다.</summary>
        public int Count => m_entries.Count;

        /// <summary>
        /// 새로운 가중치 추첨기 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="seed">선택적 시드 번호 (지정하지 않을 경우 시스템 시간 기반)</param>
        public WeightedRandomPicker(int? seed = null)
        {
            m_random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 항목과 가중치를 추가합니다.
        /// </summary>
        /// <param name="item">추가할 항목</param>
        /// <param name="weight">가중치 (0보다 커야 함)</param>
        public WeightedRandomPicker<T> Add(T item, float weight)
        {
            if (weight <= 0f) return this;

            m_totalWeight += weight;
            m_entries.Add(new Entry(item, weight, m_totalWeight));
            return this;
        }

        /// <summary>
        /// 가중치 확률에 따라 항목 1개를 무작위 추첨합니다.
        /// </summary>
        /// <returns>선택된 항목</returns>
        /// <exception cref="InvalidOperationException">등록된 항목이 없거나 총 가중치가 0 이하인 경우</exception>
        public T PickOne()
        {
            if (m_entries.Count == 0 || m_totalWeight <= 0f)
                throw new InvalidOperationException("WeightedRandomPicker에 등록된 유효한 항목이 없습니다.");

            double randomValue = m_random.NextDouble() * m_totalWeight;

            // 이진 탐색 (Binary Search)
            int low = 0;
            int high = m_entries.Count - 1;

            while (low < high)
            {
                int mid = (low + high) / 2;
                if (m_entries[mid].CumulativeWeight < randomValue)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return m_entries[low].Item;
        }

        /// <summary>
        /// 등록된 항목이 없으면 기본값을 반환하는 안전한 추첨 메서드입니다.
        /// </summary>
        /// <param name="defaultValue">실패 시 반환할 기본값</param>
        public T PickOneOrDefault(T defaultValue = default)
        {
            if (m_entries.Count == 0 || m_totalWeight <= 0f)
                return defaultValue;

            return PickOne();
        }

        /// <summary>
        /// 모든 항목을 비우고 초기화합니다.
        /// </summary>
        public void Clear()
        {
            m_entries.Clear();
            m_totalWeight = 0f;
        }
    }
}
