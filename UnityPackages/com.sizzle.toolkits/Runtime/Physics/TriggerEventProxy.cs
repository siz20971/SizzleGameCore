using UnityEngine;

namespace Sizzle.Toolkits.Physics
{
    /// <summary>
    /// Unity 3D 트리거 이벤트(OnTriggerEnter, OnTriggerExit, OnTriggerStay)를 수신하여 C# 이벤트 델리게이트로 중계하는 프록시 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerEventProxy : MonoBehaviour
    {
        /// <summary> 3D 트리거 이벤트 핸들러 델리게이트 </summary>
        public delegate void TriggerEvent(Collider collider);

        /// <summary> 3D 트리거 영역에 다른 콜라이더가 진입할 때 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerEntered;
        /// <summary> 3D 트리거 영역에서 다른 콜라이더가 벗어날 때 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerExited;
        /// <summary> 3D 트리거 영역 안에 다른 콜라이더가 머무는 동안 매 프레임 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerStaying;

        private void OnTriggerEnter(Collider collider)
        {
            OnTriggerEntered?.Invoke(collider);
        }
        private void OnTriggerExit(Collider collider)
        {
            OnTriggerExited?.Invoke(collider);
        }
        private void OnTriggerStay(Collider collider)
        {
            OnTriggerStaying?.Invoke(collider);
        }
    }
}
