//using Cysharp.Threading.Tasks;
//using System;
//using System.Threading;
//using UnityEngine;

//namespace Sizzle.AbilitySystem
//{

//    public abstract class AbilityTaskBase : IDisposable
//    {
//        public delegate void AbilityTaskStateChangeHandler(State task);

//        public enum State
//        {
//            None,
//            WaitForStart,
//            Start,
//            Running,
//            End,
//        }

//        protected Ability m_rootAbility;
//        private CancellationTokenSource m_cts;

//        public State TaskState { get; private set; } = State.None;

//        public event AbilityTaskStateChangeHandler OnTaskStateChange;

//        private void ChangeTaskState(State newState)
//        {
//            if (TaskState == newState)
//                return;

//            State prevState = TaskState;
//            TaskState = newState;

//            OnTaskStateChange?.Invoke(newState);
//        }

//        public void Run(float delay = 0f)
//        {
//            Cancel();
//            m_cts = new CancellationTokenSource();
//            AsyncTaskUpdate(delay, m_cts.Token).Forget();
//        }

//        public void Cancel()
//        {
//            if (m_cts == null)
//                return;

//            try
//            {
//                if (!m_cts.IsCancellationRequested)
//                    m_cts.Cancel();
//            }
//            finally
//            {
//                m_cts.Dispose();
//                m_cts = null;
//            }
//        }

//        protected abstract bool IsTaskExpired();

//        private bool IsTaskInvalid()
//        {
//            if (m_rootAbility == null) // || !m_rootAbility.IsActive)
//                return true;

//            return false;
//        }

//        private async UniTask AsyncTaskUpdate(float delay, CancellationToken token)
//        {
//            ChangeTaskState(State.WaitForStart);

//            if (delay > 0f)
//            {
//                bool cancelled = await UniTask.Delay((int)(delay * 1000), cancellationToken: token)
//                    .SuppressCancellationThrow();
//                if (cancelled)
//                    return;
//            }

//            if (token.IsCancellationRequested)
//                return;

//            ChangeTaskState(State.Start);
//            OnTaskStart();

//            await UniTask.Yield(PlayerLoopTiming.Update);

//            ChangeTaskState(State.Running);

//            while (!token.IsCancellationRequested)
//            {
//                await UniTask.Yield(PlayerLoopTiming.Update);

//                if (token.IsCancellationRequested)
//                {
//                    ChangeTaskState(State.End);
//                    OnTaskEnd(false);
//                    break;
//                }

//                if (IsTaskExpired())
//                {
//                    ChangeTaskState(State.End);
//                    OnTaskEnd(true);
//                    break;
//                }

//                if (IsTaskInvalid())
//                {
//                    ChangeTaskState(State.End);
//                    OnTaskEnd(false);
//                    break;
//                }

//                OnTaskUpdate(Time.deltaTime);
//            }
//        }

//        protected virtual void OnTaskStart() { }
//        protected virtual void OnTaskUpdate(float deltaTime) { }

//        /// <param name="isExpired">if True : 구현된 종료 조건에 맞춰 종료됨 / False : Ability가 작동 종료되는 등 </param>
//        protected virtual void OnTaskEnd(bool isExpired) { }

//        public void Dispose()
//        {
//            Cancel();
//        }
//    }


//    /// <summary>
//    /// 설정된 기간 동안 작동되는 AbilityTask
//    /// </summary>
//    public abstract class AbilityTaskByDurationBase : AbilityTaskBase
//    {
//        private float m_duration = -1.0f;
//        private float m_remainTime = -1.0f;

//        public void SetDuration(float duration)
//        {
//            m_duration = duration;
//            m_remainTime = duration;
//        }

//        protected override bool IsTaskExpired()
//        {
//            return m_remainTime <= 0f;
//        }

//        protected override void OnTaskStart()
//        {
//            base.OnTaskStart();

//            if (m_duration <= 0f)
//                Debug.LogError("AbilityTaskByDuration Require SetDuration(float) before TaskStart");

//            m_remainTime = m_duration;
//        }

//        protected override void OnTaskUpdate(float deltaTime)
//        {
//            m_remainTime -= deltaTime;
//        }

//        protected override void OnTaskEnd(bool isExpired)
//        {
//            base.OnTaskEnd(isExpired);
//            m_remainTime = -1.0f;
//        }
//    }
//}