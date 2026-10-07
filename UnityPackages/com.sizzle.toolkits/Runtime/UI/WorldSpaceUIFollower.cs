using System;
using UnityEngine;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// 3D/2D 월드 공간의 대상 Transform 위치를 카메라를 통해 스크린 좌표로 변환하여 UI RectTransform이 실시간 추적하도록 돕는 컴포넌트입니다.
    /// <para>부드러운 감쇠 이동(Damping), 화면 경계 클램핑(KeepInScreen), 카메라 후방 반전/가림 처리를 지원합니다.</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class WorldSpaceUIFollower : MonoBehaviour
    {
        public enum FollowUpdateMode
        {
            Update,
            LateUpdate,
            FixedUpdate,
        }

        [Header("Follow Target")]
        [SerializeField] private Transform m_followTarget;
        [SerializeField] private Vector3 m_worldOffset;

        [Header("Follow Settings")]
        [Tooltip("Cinemachine 또는 카메라 갱신 타이밍에 맞춰 UI 추적 업데이트 시점을 선택합니다.")]
        [SerializeField] private FollowUpdateMode m_updateMode = FollowUpdateMode.LateUpdate;
        [Tooltip("0 이하면 즉시 이동, 0보다 크면 부드럽게 이동합니다.")]
        [SerializeField] private float m_damping = 0f;

        [Header("Screen Clamp")]
        [Tooltip("화면 범위 내에 UI가 항상 표시되도록 강제합니다.")]
        [SerializeField] private bool m_keepInScreen = false;
        [Tooltip("화면 범위 안쪽 여백 비율입니다. (예: 0.1은 화면 크기의 10% 여백)")]
        [SerializeField, Range(0f, 0.5f)] private float m_paddingRatio = 0.01f;

        private Camera m_camera;
        private RectTransform m_rectTransform;
        private Vector3 m_worldVelocity;
        private Vector3 m_currentWorldPos;
        private Vector3 m_previousFixedWorldPos;
        private Vector3 m_latestFixedWorldPos;
        private bool m_isFirstFrame = true;
        private bool m_hasBinding;
        private bool m_hasFixedWorldSample;

        /// <summary> 추적 대상이 파괴되거나 소실되었을 때 발생하는 이벤트입니다. </summary>
        public event Action<WorldSpaceUIFollower> TargetLost;

        /// <summary> 현재 추적 중인 대상 Transform </summary>
        public Transform FollowTarget => m_followTarget;
        /// <summary> 대상 위치에 더해지는 월드 오프셋 </summary>
        public Vector3 WorldOffset => m_worldOffset;
        /// <summary> 현재 유효한 추적 대상을 따라가고 있는지 여부 </summary>
        public bool IsFollowing => m_hasBinding && m_followTarget != null;

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (m_updateMode != FollowUpdateMode.Update)
                return;

            UpdateFollower(useFixedInterpolation: false, Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (m_updateMode == FollowUpdateMode.LateUpdate)
            {
                UpdateFollower(useFixedInterpolation: false, Time.deltaTime);
                return;
            }

            if (m_updateMode == FollowUpdateMode.FixedUpdate)
                UpdateFollower(useFixedInterpolation: true, Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (m_updateMode != FollowUpdateMode.FixedUpdate)
                return;

            CacheFixedTargetWorldPosition();
        }

        private void UpdateFollower(bool useFixedInterpolation, float deltaTime)
        {
            if (!m_hasBinding)
                return;

            if (m_camera == null)
            {
                m_camera = Camera.main;
                if (m_camera == null)
                    return;
            }

            if (m_followTarget == null)
            {
                m_hasBinding = false;
                ResetState(clearTarget: true);
                TargetLost?.Invoke(this);
                return;
            }

            Vector3 targetWorldPos = useFixedInterpolation
                ? ResolveInterpolatedFixedTargetWorldPosition()
                : GetTargetWorldPosition();

            if (m_isFirstFrame)
            {
                m_currentWorldPos = targetWorldPos;
                m_isFirstFrame = false;
            }

            if (m_damping > 0f)
            {
                float safeDeltaTime = deltaTime > 0f ? deltaTime : 0.0001f;
                m_currentWorldPos = Vector3.SmoothDamp(m_currentWorldPos, targetWorldPos, ref m_worldVelocity, m_damping, Mathf.Infinity, safeDeltaTime);
            }
            else
            {
                m_currentWorldPos = targetWorldPos;
                m_worldVelocity = Vector3.zero;
            }

            Vector3 screenPos = m_camera.WorldToScreenPoint(m_currentWorldPos);

            bool isBehindCamera = screenPos.z < 0f;
            if (isBehindCamera)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            if (m_keepInScreen)
            {
                float paddingX = Screen.width * m_paddingRatio;
                float paddingY = Screen.height * m_paddingRatio;

                float scaleX = m_rectTransform.lossyScale.x;
                float scaleY = m_rectTransform.lossyScale.y;

                float minXOffset = m_rectTransform.rect.width * m_rectTransform.pivot.x * scaleX;
                float maxXOffset = m_rectTransform.rect.width * (1f - m_rectTransform.pivot.x) * scaleX;
                float minYOffset = m_rectTransform.rect.height * m_rectTransform.pivot.y * scaleY;
                float maxYOffset = m_rectTransform.rect.height * (1f - m_rectTransform.pivot.y) * scaleY;

                float minX = paddingX + minXOffset;
                float maxX = Screen.width - paddingX - maxXOffset;
                float minY = paddingY + minYOffset;
                float maxY = Screen.height - paddingY - maxYOffset;

                if (minX > maxX)
                {
                    float mid = (minX + maxX) * 0.5f;
                    minX = mid;
                    maxX = mid;
                }

                if (minY > maxY)
                {
                    float mid = (minY + maxY) * 0.5f;
                    minY = mid;
                    maxY = mid;
                }

                if (isBehindCamera)
                {
                    Vector2 clampCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
                    Vector2 dir = (Vector2)screenPos - clampCenter;
                    if (dir.sqrMagnitude < 0.001f)
                        dir = Vector2.up;

                    float tX = dir.x > 0f ? (maxX - clampCenter.x) / dir.x : (dir.x < 0f ? (minX - clampCenter.x) / dir.x : float.MaxValue);
                    float tY = dir.y > 0f ? (maxY - clampCenter.y) / dir.y : (dir.y < 0f ? (minY - clampCenter.y) / dir.y : float.MaxValue);
                    float t = Mathf.Min(tX, tY);

                    screenPos.x = clampCenter.x + dir.x * t;
                    screenPos.y = clampCenter.y + dir.y * t;
                }

                screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
                screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);
            }
            else if (isBehindCamera)
            {
                screenPos.x = -10000f;
                screenPos.y = -10000f;
            }

            screenPos.z = 0f;
            m_rectTransform.position = screenPos;
        }

        private void CacheFixedTargetWorldPosition()
        {
            if (!m_hasBinding || m_followTarget == null)
                return;

            Vector3 targetWorldPos = GetTargetWorldPosition();
            if (!m_hasFixedWorldSample)
            {
                m_previousFixedWorldPos = targetWorldPos;
                m_latestFixedWorldPos = targetWorldPos;
                m_hasFixedWorldSample = true;
                return;
            }

            m_previousFixedWorldPos = m_latestFixedWorldPos;
            m_latestFixedWorldPos = targetWorldPos;
        }

        private Vector3 ResolveInterpolatedFixedTargetWorldPosition()
        {
            if (!m_hasFixedWorldSample)
                return GetTargetWorldPosition();

            float fixedDeltaTime = Time.fixedDeltaTime;
            if (fixedDeltaTime <= Mathf.Epsilon)
                return m_latestFixedWorldPos;

            float interpolation = Mathf.Clamp01((Time.time - Time.fixedTime) / fixedDeltaTime);
            return Vector3.Lerp(m_previousFixedWorldPos, m_latestFixedWorldPos, interpolation);
        }

        private Vector3 GetTargetWorldPosition()
        {
            return m_followTarget.position + m_worldOffset;
        }

        /// <summary>
        /// 새로운 월드 타겟 Transform과 오프셋을 바인딩하여 추적을 시작합니다.
        /// </summary>
        /// <param name="target">추적할 대상 Transform</param>
        /// <param name="worldOffset">대상 위치에 더해질 월드 오프셋</param>
        public void Bind(Transform target, Vector3 worldOffset)
        {
            m_followTarget = target;
            m_worldOffset = worldOffset;
            m_hasBinding = target != null;

            if (m_camera == null)
                m_camera = Camera.main;

            ResetState(clearTarget: false);

            if (target != null)
            {
                Vector3 targetWorldPos = GetTargetWorldPosition();
                m_previousFixedWorldPos = targetWorldPos;
                m_latestFixedWorldPos = targetWorldPos;
                m_hasFixedWorldSample = true;
            }
        }

        /// <summary>
        /// 현재 바인딩된 타겟 추적을 해제하고 위치 갱신을 중단합니다.
        /// </summary>
        public void Unbind()
        {
            m_hasBinding = false;
            ResetState(clearTarget: true);
        }

        private void ResetState(bool clearTarget)
        {
            if (clearTarget)
                m_followTarget = null;

            m_worldVelocity = Vector3.zero;
            m_isFirstFrame = true;
            m_hasFixedWorldSample = false;
        }
    }
}
