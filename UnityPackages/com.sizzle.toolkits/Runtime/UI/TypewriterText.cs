using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if CORE_TMPRO
using TMPro;
#endif

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 대화창이나 스토리 내레이션 텍스트를 한 글자씩 타이핑하듯 출력하는 개선된 타이프라이터 컴포넌트입니다.
    /// 문자열을 자르는 방식 대신, 전체 텍스트 레이아웃을 고정한 채 가시성을 조절하는 투명 태그 방식을 사용하여
    /// 단어가 완성되면서 갑자기 다음 줄로 튕겨 내려가는 줄바꿈(Word Wrap Jitter) 현상을 원천 방지합니다.
    /// 리치텍스트(&lt;color&gt;, &lt;b&gt; 등) 태그를 안전하게 보존합니다.
    /// </summary>
    public class TypewriterText : MonoBehaviour
    {
        [Header("UI Target")]
        [Tooltip("텍스트를 출력할 uGUI Text 컴포넌트")]
        [SerializeField] private Text m_uiText;

#if CORE_TMPRO
        [Tooltip("텍스트를 출력할 TextMeshPro 컴포넌트")]
        [SerializeField] private TMP_Text m_tmpText;
#endif

        [Header("Typing Speed")]
        [Tooltip("일반 글자당 출력 대기 시간(초)")]
        [SerializeField] private float m_characterDelay = 0.04f;

        [Tooltip("쉼표(,)를 만났을 때 추가 일시정지 시간(초)")]
        [SerializeField] private float m_commaPause = 0.15f;

        [Tooltip("마침표(.), 물음표(?), 느낌표(!)를 만났을 때 추가 일시정지 시간(초)")]
        [SerializeField] private float m_periodPause = 0.3f;

        [Header("Events")]
        [Tooltip("글자 출력 시마다 발생하는 이벤트 (타자기 타건 사운드 등)")]
        [SerializeField] private UnityEvent m_onTypingCharacter;

        [Tooltip("타이핑 완료 시 발생하는 이벤트")]
        [SerializeField] private UnityEvent m_onTypingCompleted;

        [Tooltip("글자 출력 시 갱신되는 텍스트 문자열 이벤트")]
        [SerializeField] private UnityEvent<string> m_onTextChanged;

        private Coroutine m_typeCoroutine;
        private string m_fullText = "";
        private bool m_isTyping;

        // 리치텍스트 태그를 감지하기 위한 정규식
        private static readonly Regex TagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

        /// <summary>현재 타이핑 애니메이션이 진행 중인지 여부입니다.</summary>
        public bool IsTyping => m_isTyping;

        /// <summary>전체 원본 텍스트입니다.</summary>
        public string FullText => m_fullText;

        /// <summary>글자 출력 시마다 발생하는 C# 이벤트입니다.</summary>
        public event Action OnCharacterTyped;

        /// <summary>타이핑 완료 시 발생하는 C# 이벤트입니다.</summary>
        public event Action OnTypingCompleted;

        /// <summary>화면에 출력되는 텍스트가 갱신될 때마다 전체 부분 텍스트를 전달하는 C# 이벤트입니다.</summary>
        public event Action<string> OnTextChanged;

        private void Reset()
        {
            m_uiText = GetComponent<Text>();
#if CORE_TMPRO
            m_tmpText = GetComponent<TMP_Text>();
#endif
        }

        private void Awake()
        {
            if (m_uiText == null)
            {
                m_uiText = GetComponent<Text>();
            }

            if (m_uiText != null)
            {
                m_uiText.supportRichText = true;
            }

#if CORE_TMPRO
            if (m_tmpText == null)
            {
                m_tmpText = GetComponent<TMP_Text>();
            }

            if (m_tmpText != null)
            {
                m_tmpText.richText = true;
            }
#endif
        }

        /// <summary>
        /// 새로운 텍스트로 줄바꿈 튐 없는 타이핑 애니메이션을 시작합니다.
        /// </summary>
        /// <param name="text">출력할 전체 텍스트</param>
        /// <param name="onComplete">완료 시 콜백</param>
        public void Play(string text, Action onComplete = null)
        {
            if (m_typeCoroutine != null)
            {
                StopCoroutine(m_typeCoroutine);
            }

            m_fullText = text ?? "";

            if (!gameObject.activeInHierarchy || m_characterDelay <= 0f)
            {
                Skip();
                onComplete?.Invoke();
                return;
            }

            m_typeCoroutine = StartCoroutine(TypeRoutine(m_fullText, onComplete));
        }

        /// <summary>
        /// 진행 중인 타이핑을 즉시 완료하고 전체 텍스트를 화면에 온전히 표시(스킵)합니다.
        /// </summary>
        public void Skip()
        {
            if (m_typeCoroutine != null)
            {
                StopCoroutine(m_typeCoroutine);
                m_typeCoroutine = null;
            }

            ApplyFullText(m_fullText);
            m_isTyping = false;
            m_onTypingCompleted?.Invoke();
            OnTypingCompleted?.Invoke();
        }

        private IEnumerator TypeRoutine(string text, Action onComplete)
        {
            m_isTyping = true;

            // 순수 텍스트 길이 및 가시 인덱스 계산
            int totalLength = text.Length;
            int visibleCharCount = 0;

            // 순수 글자 수 카운트 (태그 제외)
            int pureCharCount = GetPureCharacterCount(text);

            while (visibleCharCount <= pureCharCount)
            {
                string renderedText = BuildFadedText(text, visibleCharCount);
                if (m_uiText != null)
                {
                    m_uiText.text = renderedText;
                }

#if CORE_TMPRO
                if (m_tmpText != null)
                {
                    m_tmpText.text = renderedText;
                }
#endif

                m_onTextChanged?.Invoke(renderedText);
                OnTextChanged?.Invoke(renderedText);

                m_onTypingCharacter?.Invoke();
                OnCharacterTyped?.Invoke();

                char currentChar = GetNthPureChar(text, visibleCharCount - 1);
                float delay = m_characterDelay;

                if (currentChar == ',' || currentChar == ';')
                {
                    delay += m_commaPause;
                }
                else if (currentChar == '.' || currentChar == '?' || currentChar == '!')
                {
                    delay += m_periodPause;
                }

                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }

                visibleCharCount++;
            }

            ApplyFullText(text);
            m_isTyping = false;
            m_typeCoroutine = null;
            m_onTypingCompleted?.Invoke();
            OnTypingCompleted?.Invoke();
            onComplete?.Invoke();
        }

        private void ApplyFullText(string text)
        {
            if (m_uiText != null)
            {
                m_uiText.text = text;
            }

#if CORE_TMPRO
            if (m_tmpText != null)
            {
                m_tmpText.text = text;
            }
#endif

            m_onTextChanged?.Invoke(text);
            OnTextChanged?.Invoke(text);
        }

        /// <summary>
        /// 지정한 가시 글자 수까지는 원래 색상으로, 그 이후 글자는 투명 태그(&lt;color=#00000000&gt;)로 래핑하여
        /// 레이아웃과 줄바꿈을 완벽히 유지한 채 텍스트를 조립합니다.
        /// </summary>
        private static string BuildFadedText(string originalText, int visibleCharCount)
        {
            if (visibleCharCount <= 0)
            {
                return $"<color=#00000000>{originalText}</color>";
            }

            int pureCount = 0;
            int splitIndex = originalText.Length;

            for (int i = 0; i < originalText.Length; i++)
            {
                if (originalText[i] == '<')
                {
                    int closeTag = originalText.IndexOf('>', i);
                    if (closeTag > i)
                    {
                        i = closeTag;
                        continue;
                    }
                }

                pureCount++;
                if (pureCount == visibleCharCount)
                {
                    splitIndex = i + 1;
                    break;
                }
            }

            if (splitIndex >= originalText.Length)
            {
                return originalText;
            }

            string visiblePart = originalText.Substring(0, splitIndex);
            string hiddenPart = originalText.Substring(splitIndex);

            return $"{visiblePart}<color=#00000000>{hiddenPart}</color>";
        }

        private static int GetPureCharacterCount(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '<')
                {
                    int closeTag = text.IndexOf('>', i);
                    if (closeTag > i)
                    {
                        i = closeTag;
                        continue;
                    }
                }
                count++;
            }
            return count;
        }

        private static char GetNthPureChar(string text, int targetIndex)
        {
            if (targetIndex < 0) return '\0';

            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '<')
                {
                    int closeTag = text.IndexOf('>', i);
                    if (closeTag > i)
                    {
                        i = closeTag;
                        continue;
                    }
                }

                if (count == targetIndex)
                {
                    return text[i];
                }
                count++;
            }
            return '\0';
        }
    }
}
