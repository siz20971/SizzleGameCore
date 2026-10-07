using System.Threading;
using UnityEngine;

namespace Sizzle.Toolkits.Transitions
{
    /// <summary>
    /// 화면 전환 연출의 진행 단계 (화면 가리기 Enter, 화면 열기 Exit)를 정의하는 열거형입니다.
    /// </summary>
    public enum TransitionPhase
    {
        /// <summary> 화면 가리기 단계 (Fade In / Mask In / 화면 덮음) </summary>
        Enter,
        /// <summary> 화면 열기 단계 (Fade Out / Mask Out / 화면 드러냄) </summary>
        Exit
    }

    /// <summary>
    /// 실제 렌더링 파이프라인(uGUI 또는 URP FullScreen 등)에서 트랜지션 연출을 구동하는 드라이버 인터페이스입니다.
    /// </summary>
    public interface ITransitionDriver
    {
        /// <summary>
        /// 지정한 프로파일과 단계(Enter/Exit)에 맞춰 화면 전환 연출을 재생합니다.
        /// duration이 0 이하일 경우 profile.Duration을 기본값으로 사용합니다.
        /// </summary>
        Awaitable PlayAsync(ScreenTransitionProfile profile, TransitionPhase phase, float duration = -1f, CancellationToken cancellationToken = default);

        /// <summary>
        /// 수동으로 전환 진행도(0 ~ 1)를 반영합니다.
        /// </summary>
        void SetProgress(float progress);

        /// <summary>
        /// 진행 중인 연출을 즉시 중단하고 렌더러를 리셋합니다.
        /// </summary>
        void Abort();
    }
}
