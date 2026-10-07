using UnityEngine;

namespace Sizzle.Toolkits.Animations
{
    /// <summary>
    /// SpriteRenderer를 기반으로 프레임 단위의 스프라이트 배열을 재생하는 경량 2D 애니메이션 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private Sprite[] m_frames;
        [Min(0.01f)]
        [SerializeField] private float m_framesPerSecond = 12f;
        [SerializeField] private bool m_loop = true;
        [SerializeField] private bool m_playOnEnable = true;
        [SerializeField] private bool m_ignoreTimeScale;
        [SerializeField] private bool m_hideWhenNotPlaying;

        private int m_currentFrameIndex;
        private float m_elapsedTime;
        private bool m_isPlaying;

        /// <summary> 현재 애니메이션이 재생 중인지 여부입니다. </summary>
        public bool IsPlaying => m_isPlaying;

        /// <summary> 현재 렌더링 중인 스프라이트 프레임의 인덱스입니다. </summary>
        public int CurrentFrameIndex => m_currentFrameIndex;

        /// <summary> 애니메이션에 등록된 전체 스프라이트 프레임 개수입니다. </summary>
        public int FrameCount => m_frames == null ? 0 : m_frames.Length;

        private void Awake()
        {
            CacheComponents();
            ApplyCurrentFrame();
            UpdateVisibility();
        }

        private void OnEnable()
        {
            if (m_playOnEnable)
            {
                Play();
                return;
            }

            ApplyCurrentFrame();
            UpdateVisibility();
        }

        private void Update()
        {
            if (!m_isPlaying || FrameCount <= 1)
                return;

            float deltaTime = m_ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(0.01f, m_framesPerSecond);

            m_elapsedTime += deltaTime;
            while (m_elapsedTime >= frameDuration)
            {
                m_elapsedTime -= frameDuration;
                AdvanceFrame();

                if (!m_isPlaying)
                    break;
            }
        }

        /// <summary>
        /// 애니메이션을 처음(0번 프레임)부터 재생합니다.
        /// </summary>
        public void Play()
        {
            m_elapsedTime = 0f;
            m_currentFrameIndex = 0;
            m_isPlaying = FrameCount > 0;
            ApplyCurrentFrame();
            UpdateVisibility();
        }

        /// <summary>
        /// 애니메이션 재생을 정지합니다.
        /// </summary>
        /// <param name="resetToFirstFrame">정지 시 0번 첫 프레임으로 되돌릴지 여부</param>
        public void Stop(bool resetToFirstFrame = true)
        {
            m_isPlaying = false;
            m_elapsedTime = 0f;

            if (resetToFirstFrame)
            {
                m_currentFrameIndex = 0;
                ApplyCurrentFrame();
            }

            UpdateVisibility();
        }

        /// <summary>
        /// 애니메이션을 일시 정지합니다. 현재 프레임 상태를 유지합니다.
        /// </summary>
        public void Pause()
        {
            m_isPlaying = false;
            UpdateVisibility();
        }

        /// <summary>
        /// 일시 정지된 애니메이션을 현재 프레임부터 다시 재생합니다.
        /// </summary>
        public void Resume()
        {
            if (FrameCount == 0)
                return;

            m_isPlaying = true;
            UpdateVisibility();
        }

        /// <summary>
        /// 애니메이션의 특정 프레임을 강제로 지정하여 표시합니다.
        /// </summary>
        /// <param name="frameIndex">표시할 프레임 인덱스 (0 ~ FrameCount - 1)</param>
        public void SetFrame(int frameIndex)
        {
            if (FrameCount == 0)
                return;

            m_currentFrameIndex = Mathf.Clamp(frameIndex, 0, FrameCount - 1);
            m_elapsedTime = 0f;
            ApplyCurrentFrame();
        }

        private void AdvanceFrame()
        {
            if (FrameCount == 0)
            {
                m_isPlaying = false;
                return;
            }

            if (m_currentFrameIndex >= FrameCount - 1)
            {
                if (m_loop)
                {
                    m_currentFrameIndex = 0;
                }
                else
                {
                    m_isPlaying = false;
                    ApplyCurrentFrame();
                    return;
                }
            }
            else
            {
                m_currentFrameIndex++;
            }

            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (m_spriteRenderer == null || FrameCount == 0)
                return;

            m_currentFrameIndex = Mathf.Clamp(m_currentFrameIndex, 0, FrameCount - 1);
            m_spriteRenderer.sprite = m_frames[m_currentFrameIndex];
        }

        private void UpdateVisibility()
        {
            if (m_spriteRenderer == null)
                return;

            m_spriteRenderer.enabled = !m_hideWhenNotPlaying || m_isPlaying;
        }

        private void CacheComponents()
        {
            if (m_spriteRenderer == null)
                m_spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnValidate()
        {
            m_framesPerSecond = Mathf.Max(0.01f, m_framesPerSecond);
            CacheComponents();

            if (FrameCount == 0)
            {
                m_currentFrameIndex = 0;
                UpdateVisibility();
                return;
            }

            m_currentFrameIndex = Mathf.Clamp(m_currentFrameIndex, 0, FrameCount - 1);
            ApplyCurrentFrame();
            UpdateVisibility();
        }
    }
}
