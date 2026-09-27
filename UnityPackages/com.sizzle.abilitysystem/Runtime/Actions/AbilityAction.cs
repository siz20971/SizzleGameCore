using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 어빌리티 내에서 실행되는 모듈형 단위 액션의 추상 기반 클래스입니다.
    /// Sizzle.AbilitySystem 코어 레이어에 위치하며 특정 게임 도메인(ICharacter 등)에 종속되지 않습니다.
    /// </summary>
    [Serializable]
    public abstract class AbilityAction
    {
        public ActionState State { get; private set; } = ActionState.WaitingForDelay;
        public float ElapsedTime { get; private set; } = 0f;
        public bool IsFinished => State == ActionState.Finished || State == ActionState.Canceled;

        public AbilityRuntimeContext Context { get; private set; }
        public GameObject GameObject => Context?.GameObject;
        public Ability Ability => Context?.Ability;
        public AbilityProcessor Processor => Context?.Processor;

        protected virtual float StartDelay => 0f;

        /// <summary> 액션이 정상적으로 완료되었을 때 호출되는 이벤트 </summary>
        public event Action<AbilityAction> OnCompleted;
        /// <summary> 액션이 중간에 취소되었을 때 호출되는 이벤트 </summary>
        public event Action<AbilityAction> OnCanceled;

        /// <summary>
        /// 액션을 시작합니다. 딜레이가 있다면 WaitingForDelay 상태가 됩니다.
        /// </summary>
        public void Start(AbilityRuntimeContext context)
        {
            Context = context;
            ElapsedTime = 0f;
            State = StartDelay > 0f ? ActionState.WaitingForDelay : ActionState.Running;

            if (State == ActionState.Running)
            {
                OnStart(context);
            }
        }

        /// <summary>
        /// 매 프레임 어빌리티 루프에서 호출되는 업데이트 메서드입니다.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            ElapsedTime += deltaTime;

            if (State == ActionState.WaitingForDelay)
            {
                if (ElapsedTime >= StartDelay)
                {
                    State = ActionState.Running;
                    ElapsedTime -= StartDelay; // 정밀 델타 보정
                    OnStart(Context);

                    if (IsFinished) return;
                }
                else
                {
                    return;
                }
            }

            if (State == ActionState.Running)
            {
                OnUpdate(deltaTime);
            }
        }

        /// <summary>
        /// 외부 요인 또는 어빌리티 종료로 액션을 강제 중단할 때 호출합니다.
        /// </summary>
        public void Cancel()
        {
            if (IsFinished) return;

            State = ActionState.Canceled;
            OnCanceledAction();
            OnCleanup();
            OnCanceled?.Invoke(this);
        }

        /// <summary>
        /// 파생 클래스 내부에서 자체적으로 액션의 목표를 달성했을 때 호출하여 정상 종료합니다.
        /// </summary>
        protected void Complete()
        {
            if (IsFinished) return;

            State = ActionState.Finished;
            OnCompletedAction();
            OnCleanup();
            OnCompleted?.Invoke(this);
        }

        // ── 생명주기 가상 메서드 ──

        /// <summary> 딜레이 대기가 끝나고 실제 로직이 시작될 때 1회 호출됩니다. </summary>
        protected virtual void OnStart(AbilityRuntimeContext context) { }

        /// <summary> Running 상태일 때 매 프레임 호출됩니다. </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary> Complete()가 호출되어 정상 완료될 때 호출됩니다. </summary>
        protected virtual void OnCompletedAction() { }

        /// <summary> Cancel()이 호출되어 강제 취소될 때 호출됩니다. </summary>
        protected virtual void OnCanceledAction() { }

        /// <summary> 정상 완료 또는 강제 취소 시 공통으로 호출되는 정리 훅입니다. </summary>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// 다중 객체가 동일한 어빌리티 데이터를 공유하거나 에디터에서 복제할 때 데이터 오염을 방지하기 위해
        /// [SerializeReference] 및 내부 클래스 참조를 포함하여 인스턴스를 깊은 복사(Deep Clone)합니다.
        /// </summary>
        public virtual AbilityAction Clone()
        {
            return DeepClone(this);
        }

        #region Deep Clone Utility

        private class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        protected static T DeepClone<T>(T source)
        {
            if (source == null) return default;
            var visited = new Dictionary<object, object>(ReferenceComparer.Instance);
            return (T)DeepCloneInternal(source, visited);
        }

        private static object DeepCloneInternal(object source, Dictionary<object, object> visited)
        {
            if (source == null) return null;

            Type type = source.GetType();

            // 1. 값 타입(기본형, struct, enum) 및 불변 객체(string)는 그대로 복사
            if (type.IsValueType || type == typeof(string))
            {
                return source;
            }

            // 2. UnityEngine.Object (에셋, 프리팹, 컴포넌트 등)는 참조 복사 유지
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return source;
            }

            // 2.5 UnityEngine의 네이티브 래퍼 타입 (AnimationCurve, Gradient 등) 안전 처리
            if (source is AnimationCurve curve)
            {
                var newCurve = new AnimationCurve(curve.keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode
                };
                visited[source] = newCurve;
                return newCurve;
            }

            if (source is Gradient gradient)
            {
                var newGrad = new Gradient();
                newGrad.SetKeys(gradient.colorKeys, gradient.alphaKeys);
                newGrad.mode = gradient.mode;
                visited[source] = newGrad;
                return newGrad;
            }

            // 3. 순환 참조 및 중복 복제 방지
            if (visited.TryGetValue(source, out var existing))
            {
                return existing;
            }

            // 4. 배열(Array) 복제
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                Array sourceArray = (Array)source;
                Array targetArray = Array.CreateInstance(elementType, sourceArray.Length);
                visited[source] = targetArray;

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    targetArray.SetValue(DeepCloneInternal(sourceArray.GetValue(i), visited), i);
                }
                return targetArray;
            }

            // 5. 일반 클래스 인스턴스 복제 ([SerializeReference] 대상 객체, 일반 C# 클래스 등)
            object target;
            try
            {
                target = Activator.CreateInstance(type);
            }
            catch
            {
                target = FormatterServices.GetUninitializedObject(type);
            }
            visited[source] = target;

            // 모든 상속 계층의 인스턴스 필드 순회
            Type currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType.GetFields(BindingFlags.Instance |
                                                   BindingFlags.Public |
                                                   BindingFlags.NonPublic |
                                                   BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    // 델리게이트/이벤트는 복제하지 않음 (이벤트 핸들러 간섭 방지)
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    // IntPtr / UIntPtr 네이티브 포인터 필드는 절대 직접 복사하지 않음 (이중 해제 및 크래시 방지)
                    if (field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr))
                    {
                        continue;
                    }

                    object fieldValue = field.GetValue(source);
                    field.SetValue(target, DeepCloneInternal(fieldValue, visited));
                }

                currentType = currentType.BaseType;
            }

            return target;
        }

        #endregion
    }
}
