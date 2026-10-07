using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.Toolkits.Data
{
    /// <summary>
    /// Unity의 JsonUtility를 활용하여 JSON 텍스트를 TData 컬렉션으로 변환하는 테이블 파서입니다.
    /// <para>- 루트 배열 형태 ([{...}, {...}]) 및 래퍼 객체 형태 ({"items": [...]}) 모두 지원</para>
    /// </summary>
    /// <typeparam name="TData">파싱 대상 데이터 모델 타입 (JsonUtility 직렬화 가능해야 함)</typeparam>
    public class JSONTableParser<TData> : ITableParser<TData>
    {
#pragma warning disable 0649
        [Serializable]
        private class ArrayWrapper
        {
            public TData[] items;
            public TData[] data;
            public TData[] list;
        }
#pragma warning restore 0649

        private readonly string _arrayPropertyName;

        /// <summary>
        /// JSON 테이블 파서를 생성합니다.
        /// </summary>
        /// <param name="arrayPropertyName">루트가 객체일 때 배열이 담긴 프로퍼티 이름 (기본 "items")</param>
        public JSONTableParser(string arrayPropertyName = "items")
        {
            _arrayPropertyName = string.IsNullOrEmpty(arrayPropertyName) ? "items" : arrayPropertyName;
        }

        /// <summary>
        /// 원시 JSON 문자열을 파싱하여 TData 목록을 반환합니다.
        /// </summary>
        public IEnumerable<TData> Parse(string rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                return Array.Empty<TData>();

            string trimmed = rawContent.Trim();

            try
            {
                // 케이스 1: 루트가 배열인 경우 [...] -> JsonUtility 지원을 위해 {"items": [...]} 형태로 래핑
                if (trimmed.StartsWith("["))
                {
                    string wrappedJson = $"{{\"items\":{trimmed}}}";
                    var wrapper = JsonUtility.FromJson<ArrayWrapper>(wrappedJson);
                    return wrapper?.items ?? Array.Empty<TData>();
                }

                // 케이스 2: 루트가 객체인 경우 {...} -> 내부 items, data, list 필드 자동 감지
                if (trimmed.StartsWith("{"))
                {
                    var wrapper = JsonUtility.FromJson<ArrayWrapper>(trimmed);
                    if (wrapper != null)
                    {
                        if (wrapper.items != null && wrapper.items.Length > 0)
                            return wrapper.items;
                        if (wrapper.data != null && wrapper.data.Length > 0)
                            return wrapper.data;
                        if (wrapper.list != null && wrapper.list.Length > 0)
                            return wrapper.list;
                    }
                }

                Debug.LogWarning($"[JSONTableParser] Unable to parse JSON array for type {typeof(TData).Name}.");
                return Array.Empty<TData>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSONTableParser] Error parsing JSON for type {typeof(TData).Name}: {ex.Message}");
                return Array.Empty<TData>();
            }
        }
    }
}
