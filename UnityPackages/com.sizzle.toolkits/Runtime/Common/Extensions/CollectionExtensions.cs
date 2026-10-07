using System;
using System.Collections.Generic;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// IList 및 배열 등 컬렉션 타입에 대한 안전 조회 및 무작위 셔플 확장 메서드를 제공합니다.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// 인덱스가 범위 내에 있는 경우 해당 요소를 반환하고, 그렇지 않으면 default(T)를 반환합니다.
        /// </summary>
        public static T GetSafe<T>(this IList<T> list, int index)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return (index >= 0 && index < list.Count) ? list[index] : default;
        }

        /// <summary>
        /// 인덱스가 범위 내에 있는 경우 해당 요소를 반환하고, 그렇지 않으면 fallback 값을 반환합니다.
        /// </summary>
        public static T GetSafe<T>(this IList<T> list, int index, T fallback)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return (index >= 0 && index < list.Count) ? list[index] : fallback;
        }

        /// <summary>
        /// Fisher-Yates 알고리즘을 사용하여 리스트의 요소들을 인플레이스(In-place)로 무작위 섞습니다.
        /// </summary>
        /// <typeparam name="T">요소 타입</typeparam>
        /// <param name="list">셔플할 리스트</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }

        /// <summary>
        /// Fisher-Yates 알고리즘을 사용하여 배열의 요소들을 인플레이스(In-place)로 무작위 섞습니다.
        /// </summary>
        /// <typeparam name="T">요소 타입</typeparam>
        /// <param name="array">셔플할 배열</param>
        public static void Shuffle<T>(this T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            System.Random rng = new System.Random();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        /// <summary>
        /// 리스트에서 무작위 요소 1개를 안전하게 반환합니다. 컬렉션이 비어있으면 fallback 값을 반환합니다.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list, T fallback = default)
        {
            if (list == null || list.Count == 0)
                return fallback;

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 배열에서 무작위 요소 1개를 안전하게 반환합니다. 배열이 비어있으면 fallback 값을 반환합니다.
        /// </summary>
        public static T GetRandom<T>(this T[] array, T fallback = default)
        {
            if (array == null || array.Length == 0)
                return fallback;

            return array[UnityEngine.Random.Range(0, array.Length)];
        }
    }
}
