using System;
using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 화면 전환 연출의 시간, 이징 커브, 셰이더 및 머티리얼 파라미터를 정의하는 ScriptableObject 프로파일 기본 클래스입니다.
    /// </summary>
    public abstract class ScreenTransitionProfile : ScriptableObject
    {
        [Header("Common Settings")]
        [SerializeField] private float m_duration = 0.5f;
        [SerializeField] private AnimationCurve m_enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool m_useUnscaledTime = true;

        /// <summary> 전환 연출 기본 지속 시간 (초) </summary>
        public float Duration => Mathf.Max(0.001f, m_duration);
        /// <summary> Enter(화면 가리기) 진행도 보간 커브 </summary>
        public AnimationCurve EnterCurve => m_enterCurve;
        /// <summary> Exit(화면 열기) 진행도 보간 커브 </summary>
        public AnimationCurve ExitCurve => m_exitCurve;
        /// <summary> Time.timeScale의 영향을 받지 않는 UnscaledTime 사용 여부 </summary>
        public bool UseUnscaledTime => m_useUnscaledTime;

        /// <summary>
        /// 이 연출에 사용될 기본 셰이더 템플릿
        /// </summary>
        public abstract Shader ShaderTemplate { get; }

        /// <summary>
        /// 드라이버가 인스턴스화하여 사용할 머티리얼을 생성합니다.
        /// </summary>
        public virtual Material CreateInstanceMaterial()
        {
            var shader = ShaderTemplate;
            return shader != null ? new Material(shader) { hideFlags = HideFlags.DontSave } : null;
        }

        /// <summary>
        /// 연출 단계 시작 시 머티리얼에 텍스처나 기본 프로퍼티를 1회 바인딩합니다.
        /// </summary>
        public virtual void OnInitMaterial(Material material, TransitionPhase phase) { }

        /// <summary>
        /// 매 프레임 진행도(0 ~ 1)에 따라 머티리얼 파라미터를 업데이트합니다.
        /// </summary>
        /// <param name="material">조작할 머티리얼 인스턴스</param>
        /// <param name="progress">커브가 평가된 진행도 (0.0 ~ 1.0)</param>
        /// <param name="phase">현재 트랜지션 단계 (Enter 또는 Exit)</param>
        public abstract void Apply(Material material, float progress, TransitionPhase phase);

        /// <summary>
        /// 연출 단계가 완료되었을 때 후처리 로직(선택 구현)
        /// </summary>
        public virtual void OnCompleteMaterial(Material material, TransitionPhase phase) { }
    }
}
