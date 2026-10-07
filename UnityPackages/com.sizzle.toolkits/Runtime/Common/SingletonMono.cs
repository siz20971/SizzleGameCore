using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// 단일 씬 내에서 유일성이 보장되는 기본 MonoBehaviour 싱글톤 베이스 클래스입니다.
    /// 중복 생성 방지 및 애플리케이션 종료 시 고스트 오브젝트 생성 방지 처리가 내장되어 있습니다.
    /// </summary>
    /// <typeparam name="T">싱글톤으로 파생될 컴포넌트 타입</typeparam>
    public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T s_instance;
        private static bool s_isApplicationQuitting;
        private static readonly object s_lock = new();

        /// <summary>현재 활성화된 싱글톤 인스턴스입니다.</summary>
        public static T Instance
        {
            get
            {
                if (s_isApplicationQuitting)
                {
                    return null;
                }

                lock (s_lock)
                {
                    if (s_instance == null)
                    {
                        s_instance = FindFirstObjectByType<T>();
                        if (s_instance == null)
                        {
                            var singletonObject = new GameObject(typeof(T).Name);
                            s_instance = singletonObject.AddComponent<T>();
                        }
                    }
                    return s_instance;
                }
            }
        }

        /// <summary>싱글톤 인스턴스가 현재 씬에 유효하게 존재하는지 여부입니다.</summary>
        public static bool HasInstance => s_instance != null;

        protected virtual void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this as T;
            }
            else if (s_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            s_isApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }

    /// <summary>
    /// 씬이 전환되어도 파괴되지 않고 유지되는(DontDestroyOnLoad) 영속 싱글톤 베이스 클래스입니다.
    /// </summary>
    /// <typeparam name="T">영속 싱글톤으로 파생될 컴포넌트 타입</typeparam>
    public abstract class PersistentSingletonMono<T> : SingletonMono<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();

            if (Instance == this)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
