using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// <para>Unity 공식 <see cref="IObjectPool{T}"/> 인터페이스를 구현하여 표준 풀링 규격을 100% 준수하면서,</para>
    /// <para>GameObject 및 Component에 특화된 프리팹 자동 인스턴스화, 부모 트랜스폼 계층 정리,
    /// 위치/회전 지정 대여(<see cref="Get(Vector3, Quaternion)"/>), RAII using 스코프 반납을 원스톱으로 지원하는 고수준 풀러입니다.</para>
    /// </summary>
    /// <typeparam name="T">풀링할 대상 컴포넌트 타입</typeparam>
    public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : Component
    {
        private readonly T m_prefab;
        private readonly Transform m_parent;
        private readonly Stack<T> m_pool;
        private readonly Action<T> m_actionOnGet;
        private readonly Action<T> m_actionOnRelease;
        private readonly Action<T> m_actionOnDestroy;
        private readonly int m_maxSize;
        private readonly bool m_collectionCheck;

        private int m_countAll;

        /// <summary>현재 풀에 비활성 상태로 보관 중인 객체 수입니다. (IObjectPool 구현)</summary>
        public int CountInactive => m_pool.Count;

        /// <summary>풀에 의해 생성되어 현재 씬에서 활성 사용 중인 객체 수입니다.</summary>
        public int CountActive => m_countAll - m_pool.Count;

        /// <summary>풀에 의해 생성된 총 인스턴스 수입니다. (IObjectPool 구현)</summary>
        public int CountAll => m_countAll;

        /// <summary>
        /// GameObject/Component 전용 오브젝트 풀을 생성합니다.
        /// </summary>
        /// <param name="prefab">복제할 원본 프리팹</param>
        /// <param name="initialSize">초기 미리 생성해둘 인스턴스 수</param>
        /// <param name="maxSize">풀에 보관할 수 있는 최대 비활성 인스턴스 수 (초과 시 Destroy)</param>
        /// <param name="parent">풀 객체들이 비활성화되어 대기할 부모 트랜스폼</param>
        /// <param name="actionOnGet">대여 시 추가 실행할 콜백</param>
        /// <param name="actionOnRelease">반납 시 추가 실행할 콜백</param>
        /// <param name="actionOnDestroy">파괴 시 추가 실행할 콜백</param>
        /// <param name="collectionCheck">중복 반납 검사 여부 (기본 true)</param>
        public ObjectPool(
            T prefab,
            int initialSize = 10,
            int maxSize = 100,
            Transform parent = null,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true)
        {
            m_prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            m_parent = parent;
            m_maxSize = Mathf.Max(1, maxSize);
            m_actionOnGet = actionOnGet;
            m_actionOnRelease = actionOnRelease;
            m_actionOnDestroy = actionOnDestroy;
            m_collectionCheck = collectionCheck;

            m_pool = new Stack<T>(initialSize);

            Preload(initialSize);
        }

        /// <summary>
        /// 지정한 수만큼 인스턴스를 미리 생성하여 풀에 적재합니다.
        /// </summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = CreateNew();
                instance.gameObject.SetActive(false);
                if (m_parent != null) instance.transform.SetParent(m_parent, false);
                m_pool.Push(instance);
            }
        }

        /// <summary>
        /// 풀에서 인스턴스를 대여(Get)하고 활성화합니다. (IObjectPool 표준 메서드)
        /// </summary>
        public T Get()
        {
            T instance = null;

            while (m_pool.Count > 0)
            {
                instance = m_pool.Pop();
                if (instance != null) break;
            }

            if (instance == null)
            {
                instance = CreateNew();
            }

            instance.gameObject.SetActive(true);
            m_actionOnGet?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// 씬의 특정 위치와 회전을 지정하여 인스턴스를 즉시 대여합니다.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T instance = Get();
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>
        /// C# using 스코프를 통해 블록을 벗어나면 자동으로 반납되는 RAII 패턴 풀 객체를 대여합니다.
        /// </summary>
        public PooledObject<T> Get(out T value)
        {
            value = Get();
            return new PooledObject<T>(value, this);
        }

        /// <summary>
        /// 사용이 끝난 인스턴스를 풀에 반납(Release)하고 비활성화합니다. (IObjectPool 표준 메서드)
        /// </summary>
        public void Release(T element)
        {
            if (element == null) return;

            if (m_collectionCheck && m_pool.Contains(element))
            {
                throw new InvalidOperationException($"동일한 인스턴스({element.name})가 이미 풀에 반납되어 있습니다. 중복 반납을 확인하세요.");
            }

            m_actionOnRelease?.Invoke(element);
            element.gameObject.SetActive(false);

            if (m_pool.Count < m_maxSize)
            {
                if (m_parent != null)
                {
                    element.transform.SetParent(m_parent, false);
                }
                m_pool.Push(element);
            }
            else
            {
                // 최대 풀 수용량 초과 시 영구 파괴
                DestroyInstance(element);
            }
        }

        /// <summary>
        /// 풀에 적재된 모든 비활성 객체를 파괴하고 풀을 초기화합니다. (IObjectPool 표준 메서드)
        /// </summary>
        public void Clear()
        {
            while (m_pool.Count > 0)
            {
                T instance = m_pool.Pop();
                if (instance != null)
                {
                    DestroyInstance(instance);
                }
            }
        }

        /// <summary>
        /// 풀을 정리하고 모든 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        private T CreateNew()
        {
            T instance = UnityEngine.Object.Instantiate(m_prefab, m_parent);
            m_countAll++;
            return instance;
        }

        private void DestroyInstance(T instance)
        {
            m_actionOnDestroy?.Invoke(instance);
            if (instance != null && instance.gameObject != null)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
            m_countAll--;
        }
    }
}
