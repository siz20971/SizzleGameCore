using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sizzle.Toolkits.Messaging
{
    /// <summary>
    /// 단방향 알림이 아닌, 특정 요청(Request)을 보내고 비동기 응답(Response)을 받아오는 RPC(Request-Response) 패턴의 메시지 버스입니다.
    /// UI에서 데이터 매니저에 비동기 조회 요청을 하거나, 시스템 간 결합 없는 쿼리 처리에 유용합니다.
    /// </summary>
    public static class RequestResponseBus
    {
        private static readonly Dictionary<Type, Delegate> s_handlers = new();
        private static readonly object s_lock = new();

        /// <summary>
        /// 특정 요청 타입에 대한 처리기(Handler)를 등록합니다. (단일 핸들러 보장)
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        /// <typeparam name="TResponse">응답 데이터 타입</typeparam>
        /// <param name="handler">요청을 받아 비동기로 응답을 반환하는 함수</param>
        public static void RegisterHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        {
            if (handler == null) return;

            Type reqType = typeof(TRequest);
            lock (s_lock)
            {
                if (s_handlers.ContainsKey(reqType))
                {
                    throw new InvalidOperationException($"요청 타입 {reqType.Name}에 대한 핸들러가 이미 등록되어 있습니다.");
                }
                s_handlers[reqType] = handler;
            }
        }

        /// <summary>
        /// 동기 응답 함수를 비동기 핸들러로 등록합니다.
        /// </summary>
        public static void RegisterHandler<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            if (handler == null) return;
            RegisterHandler<TRequest, TResponse>(req => Task.FromResult(handler(req)));
        }

        /// <summary>
        /// 등록된 요청 처리기를 해제합니다.
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        public static void UnregisterHandler<TRequest>()
        {
            lock (s_lock)
            {
                s_handlers.Remove(typeof(TRequest));
            }
        }

        /// <summary>
        /// 요청을 전송하고 비동기로 응답을 수신합니다.
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        /// <typeparam name="TResponse">응답 데이터 타입</typeparam>
        /// <param name="request">요청 객체</param>
        /// <returns>처리기가 반환한 응답 객체</returns>
        /// <exception cref="InvalidOperationException">등록된 핸들러가 없을 경우</exception>
        public static async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
        {
            Delegate del = null;
            lock (s_lock)
            {
                s_handlers.TryGetValue(typeof(TRequest), out del);
            }

            if (del == null)
            {
                throw new InvalidOperationException($"요청 타입 {typeof(TRequest).Name}을 처리할 등록된 핸들러가 없습니다.");
            }

            if (del is Func<TRequest, Task<TResponse>> asyncHandler)
            {
                return await asyncHandler.Invoke(request);
            }

            throw new InvalidCastException($"요청 타입 {typeof(TRequest).Name}의 핸들러 응답 타입이 일치하지 않습니다.");
        }

        /// <summary>
        /// 등록된 모든 핸들러를 초기화합니다.
        /// </summary>
        public static void ClearAll()
        {
            lock (s_lock)
            {
                s_handlers.Clear();
            }
        }
    }
}
