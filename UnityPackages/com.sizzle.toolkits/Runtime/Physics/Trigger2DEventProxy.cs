using UnityEngine;

namespace Sizzle.Toolkits.Physics
{
    /// <summary>
    /// Unity 2D 트리거 이벤트(OnTriggerEnter2D, OnTriggerExit2D, OnTriggerStay2D)를 수신하여 C# 이벤트 델리게이트로 중계하는 프록시 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Trigger2DEventProxy : MonoBehaviour
    {
        /// <summary> 2D 트리거 이벤트 핸들러 델리게이트 </summary>
        public delegate void TriggerEvent(Collider2D collider);

        /// <summary> 2D 트리거 영역에 다른 콜라이더가 진입할 때 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerEntered;
        /// <summary> 2D 트리거 영역에서 다른 콜라이더가 벗어날 때 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerExited;
        /// <summary> 2D 트리거 영역 안에 다른 콜라이더가 머무는 동안 매 프레임 발생하는 이벤트입니다. </summary>
        public event TriggerEvent OnTriggerStaying;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            OnTriggerEntered?.Invoke(collider);
        }
        private void OnTriggerExit2D(Collider2D collider)
        {
            OnTriggerExited?.Invoke(collider);
        }
        private void OnTriggerStay2D(Collider2D collider)
        {
            OnTriggerStaying?.Invoke(collider);
        }
    }
}
