using System;
using System.Threading;
using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 씬 전환 또는 화면 가리기/열기 연출 전체 라이프사이클을 총괄하는 싱글톤 트랜지션 매니저입니다.
    /// <para>Enter(가리기) -> 비동기 로딩 작업 실행 -> Exit(열기) 파이프라인을 지원합니다.</para>
    /// </summary>
    public class ScreenTransitionManager : MonoBehaviour
    {
        [Header("Default Settings")]
        [SerializeField] private ScreenTransitionProfile m_defaultProfile;

        [Header("Driver Configuration")]
        [Tooltip("ITransitionDriver 인터페이스를 구현한 컴포넌트")]
        [SerializeField] private MonoBehaviour m_driverComponent;

        [Tooltip("씬 전환 중에도 이 오브젝트를 유지할지 여부")]
        [SerializeField] private bool m_dontDestroyOnLoad = true;

        private ITransitionDriver m_driver;
        private CancellationTokenSource m_activeCts;

        /// <summary> 싱글톤 인스턴스 </summary>
        public static ScreenTransitionManager Instance { get; private set; }

        /// <summary> 현재 바인딩된 화면 전환 렌더링 드라이버 (uGUI 또는 URP FullScreen) </summary>
        public ITransitionDriver Driver
        {
            get => m_driver;
            set => m_driver = value;
        }

        /// <summary> 기본으로 사용할 트랜지션 프로파일 </summary>
        public ScreenTransitionProfile DefaultProfile
        {
            get => m_defaultProfile;
            set => m_defaultProfile = value;
        }

        /// <summary> 현재 화면 전환 연출이 진행 중인지 여부 </summary>
        public bool IsTransitioning => m_activeCts != null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (m_dontDestroyOnLoad)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }

                ResolveDriver();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void ResolveDriver()
        {
            if (m_driverComponent != null && m_driverComponent is ITransitionDriver driver)
            {
                m_driver = driver;
            }
            else
            {
                m_driver = GetComponentInChildren<ITransitionDriver>();
            }

            if (m_driver == null)
            {
                Debug.LogWarning("[ScreenTransitionManager] ITransitionDriver not found in inspector or children.");
            }
        }

        private CancellationToken SetupNewTransitionToken(CancellationToken externalToken)
        {
            // 기존 재생 중인 트랜지션이 있다면 즉시 취소 및 드라이버 리셋
            if (m_activeCts != null)
            {
                m_activeCts.Cancel();
                m_activeCts.Dispose();
                m_activeCts = null;
                m_driver?.Abort();
            }

            m_activeCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            return m_activeCts.Token;
        }

        /// <summary>
        /// 화면을 가리고(Enter) -> 비동기 작업(씬 로드 등)을 수행한 뒤 -> 화면을 여는(Exit) 전체 라이프사이클을 수행합니다.
        /// 이미 트랜지션이 진행 중인 경우, 기존 작업을 취소하고 새로운 트랜지션을 즉시 시작합니다.
        /// </summary>
        public async Awaitable TransitionAsync(
            Func<Awaitable> transitionTask,
            ScreenTransitionProfile profile = null,
            float duration = -1f,
            CancellationToken cancellationToken = default)
        {
            CancellationToken token = SetupNewTransitionToken(cancellationToken);
            CancellationTokenSource cts = m_activeCts;

            ScreenTransitionProfile targetProfile = profile != null ? profile : m_defaultProfile;

            if (m_driver == null)
            {
                Debug.LogWarning("[ScreenTransitionManager] Driver is not configured. Executing transitionTask directly without visual transition.");
                if (transitionTask != null)
                {
                    await transitionTask();
                }
                return;
            }

            if (targetProfile == null)
            {
                Debug.LogWarning("[ScreenTransitionManager] No profile specified and default profile is null. Executing transitionTask directly.");
                if (transitionTask != null)
                {
                    await transitionTask();
                }
                return;
            }

            try
            {
                // 1. Enter 연출 (화면 가리기)
                await m_driver.PlayAsync(targetProfile, TransitionPhase.Enter, duration, token);

                // 2. 비동기 작업 수행 (씬 로딩, 리소스 언로드 등)
                if (transitionTask != null)
                {
                    await transitionTask();
                }

                // 3. Exit 연출 (화면 열기)
                await m_driver.PlayAsync(targetProfile, TransitionPhase.Exit, duration, token);
            }
            catch (OperationCanceledException)
            {
                // 새로운 트랜지션 시작이나 외부 요청으로 취소된 경우 정상 중단
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenTransitionManager] Exception during transition: {ex.Message}\n{ex.StackTrace}");
                m_driver?.Abort();
                throw;
            }
            finally
            {
                if (m_activeCts == cts)
                {
                    m_activeCts.Dispose();
                    m_activeCts = null;
                }
            }
        }

        /// <summary>
        /// 화면을 가리는 Enter 연출만 단독 실행합니다.
        /// </summary>
        public async Awaitable EnterAsync(ScreenTransitionProfile profile = null, float duration = -1f, CancellationToken cancellationToken = default)
        {
            CancellationToken token = SetupNewTransitionToken(cancellationToken);
            CancellationTokenSource cts = m_activeCts;
            ScreenTransitionProfile targetProfile = profile != null ? profile : m_defaultProfile;

            try
            {
                if (m_driver != null && targetProfile != null)
                {
                    await m_driver.PlayAsync(targetProfile, TransitionPhase.Enter, duration, token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (m_activeCts == cts)
                {
                    m_activeCts.Dispose();
                    m_activeCts = null;
                }
            }
        }

        /// <summary>
        /// 화면을 여는 Exit 연출만 단독 실행합니다.
        /// </summary>
        public async Awaitable ExitAsync(ScreenTransitionProfile profile = null, float duration = -1f, CancellationToken cancellationToken = default)
        {
            CancellationToken token = SetupNewTransitionToken(cancellationToken);
            CancellationTokenSource cts = m_activeCts;
            ScreenTransitionProfile targetProfile = profile != null ? profile : m_defaultProfile;

            try
            {
                if (m_driver != null && targetProfile != null)
                {
                    await m_driver.PlayAsync(targetProfile, TransitionPhase.Exit, duration, token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (m_activeCts == cts)
                {
                    m_activeCts.Dispose();
                    m_activeCts = null;
                }
            }
        }

        /// <summary>
        /// 진행 중인 연출을 즉시 취소하고 화면을 복원합니다.
        /// </summary>
        public void Abort()
        {
            if (m_activeCts != null)
            {
                m_activeCts.Cancel();
                m_activeCts.Dispose();
                m_activeCts = null;
            }
            m_driver?.Abort();
        }

        private void OnDestroy()
        {
            if (m_activeCts != null)
            {
                m_activeCts.Cancel();
                m_activeCts.Dispose();
                m_activeCts = null;
            }
        }
    }
}
