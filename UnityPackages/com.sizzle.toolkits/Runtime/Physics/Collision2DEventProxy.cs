using UnityEngine;

namespace Sizzle.Toolkits.Physics
{
    /// <summary>
    /// Unity 2D 충돌 이벤트(OnCollisionEnter2D, OnCollisionExit2D, OnCollisionStay2D)를 수신하여 C# 이벤트 델리게이트로 중계하는 프록시 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collision2DEventProxy : MonoBehaviour
    {
        /// <summary> 2D 충돌 이벤트 핸들러 델리게이트 </summary>
        public delegate void CollisionEvent(Collision2D collision);

        /// <summary> 2D 물리 충돌이 시작될 때 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionEntered;
        /// <summary> 2D 물리 충돌이 끝날 때 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionExited;
        /// <summary> 2D 물리 충돌이 지속되는 동안 매 프레임 발생하는 이벤트입니다. </summary>
        public event CollisionEvent OnCollisionStaying;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            OnCollisionEntered?.Invoke(collision);
        }
        private void OnCollisionExit2D(Collision2D collision)
        {
            OnCollisionExited?.Invoke(collision);
        }
        private void OnCollisionStay2D(Collision2D collision)
        {
            OnCollisionStaying?.Invoke(collision);
        }
    }
}
