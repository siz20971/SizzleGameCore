using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 콜백 등록 및 해제 없이, C# async/await를 사용하여 특정 이벤트가 발생할 때까지 직관적으로 대기할 수 있는 비동기 메시지 스트림 헬퍼입니다.
    /// 튜토리얼 스텝 진행, 보스 처치 대기, 팝업 닫힘 대기 등에 유용합니다.
    /// </summary>
    public static class AsyncMessageStream
    {
        /// <summary>
        /// 특정 이벤트가 전역 <see cref="GameEventBus"/>를 통해 발생할 때까지 비동기로 대기합니다.
        /// </summary>
        /// <typeparam name="T">대기할 이벤트 타입</typeparam>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <param name="timeoutSeconds">타임아웃(초) - 지정 시 초과되면 TimeoutException 발생</param>
        /// <returns>수신된 이벤트 데이터</returns>
        public static async Task<T> WaitForAsync<T>(
            CancellationToken cancellationToken = default,
            float timeoutSeconds = 0f)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            Action<T> handler = evt => tcs.TrySetResult(evt);

            GameEventBus.Subscribe<T>(handler);

            CancellationTokenRegistration? reg = null;
            CancellationTokenSource timeoutCts = null;

            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            }

            if (timeoutSeconds > 0f)
            {
                timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                timeoutCts.Token.Register(() => tcs.TrySetException(new TimeoutException($"WaitForAsync<{typeof(T).Name}> 대기 시간 초과 ({timeoutSeconds}s)")));
            }

            try
            {
                return await tcs.Task;
            }
            finally
            {
                GameEventBus.Unsubscribe<T>(handler);
                reg?.Dispose();
                timeoutCts?.Dispose();
            }
        }

        /// <summary>
        /// 조건을 만족하는 특정 이벤트가 발생할 때까지 비동기로 대기합니다.
        /// </summary>
        /// <typeparam name="T">대기할 이벤트 타입</typeparam>
        /// <param name="predicate">이벤트 통과 조건식</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <param name="timeoutSeconds">타임아웃(초)</param>
        public static async Task<T> WaitForAsync<T>(
            Func<T, bool> predicate,
            CancellationToken cancellationToken = default,
            float timeoutSeconds = 0f)
        {
            if (predicate == null)
            {
                return await WaitForAsync<T>(cancellationToken, timeoutSeconds);
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            Action<T> handler = evt =>
            {
                try
                {
                    if (predicate(evt))
                    {
                        tcs.TrySetResult(evt);
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            GameEventBus.Subscribe<T>(handler);

            CancellationTokenRegistration? reg = null;
            CancellationTokenSource timeoutCts = null;

            if (cancellationToken.CanBeCanceled)
            {
                reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            }

            if (timeoutSeconds > 0f)
            {
                timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                timeoutCts.Token.Register(() => tcs.TrySetException(new TimeoutException($"WaitForAsync<{typeof(T).Name}> 조건 대기 시간 초과 ({timeoutSeconds}s)")));
            }

            try
            {
                return await tcs.Task;
            }
            finally
            {
                GameEventBus.Unsubscribe<T>(handler);
                reg?.Dispose();
                timeoutCts?.Dispose();
            }
        }
    }
}
