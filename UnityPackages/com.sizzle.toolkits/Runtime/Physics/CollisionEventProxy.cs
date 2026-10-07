using UnityEngine;

namespace Sizzle.Toolkits.Physics
{
    /// <summary>
    /// Unity 3D 충돌 이벤트(OnCollisionEnter, OnCollisionExit, OnCollisionStay)를 수신하여 C# 이벤트 델리게이트로 중계하는 프록시 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollisionEventProxy : MonoBehaviour
    {
        /// <summary> 3D 충돌 이벤트 핸들러 델리게이트 </summary>
        public delegate void CollisionEvent(Collision collision);

        /// <summary> 3D 물리 충돌이 시작될 때 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionEntered;
        /// <summary> 3D 물리 충돌이 끝날 때 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionExited;
        /// <summary> 3D 물리 충돌이 지속되는 동안 매 프레임 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionStaying;

        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEntered?.Invoke(collision);
        }
        private void OnCollisionExit(Collision collision)
        {
            OnCollisionExited?.Invoke(collision);
        }
        private void OnCollisionStay(Collision collision)
        {
            OnCollisionStaying?.Invoke(collision);
        }
    }
}
