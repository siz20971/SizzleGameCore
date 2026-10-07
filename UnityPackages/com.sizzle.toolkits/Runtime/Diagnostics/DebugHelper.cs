using UnityEngine;

namespace Sizzle.Toolkits.Diagnostics
{
    /// <summary>
    /// 평면(XY, XZ, YZ) 단위의 박스 및 원, 그리고 회전된 3D 큐브 디버그 렌더링 헬퍼를 제공합니다.
    /// </summary>
    public static class DebugHelper
    {
        /// <summary> 디버그 도형을 그릴 기준 2D 평면 </summary>
        public enum Plane
        {
            /// <summary> 2D 표준 평면 (X: 우측, Y: 상단, 법선: 앞쪽) </summary>
            XY,
            /// <summary> 탑다운 3D 수평 평면 (X: 우측, Z: 앞쪽, 법선: 상단) </summary>
            XZ,
            /// <summary> 측면 수직 평면 (Y: 상단, Z: 앞쪽, 법선: 우측) </summary>
            YZ
        }

        /// <summary> XY 평면상에 회전된 2D 박스를 그립니다. </summary>
        public static void DrawBoxXY(Vector3 center, Vector2 size, float angle, Color color, float duration = 0f)
        {
            DrawBox(Plane.XY, center, size, angle, color, duration);
        }

        /// <summary> XZ 평면(지면)상에 회전된 2D 박스를 그립니다. </summary>
        public static void DrawBoxXZ(Vector3 center, Vector2 size, float angle, Color color, float duration = 0f)
        {
            DrawBox(Plane.XZ, center, size, angle, color, duration);
        }

        /// <summary> YZ 평면상에 회전된 2D 박스를 그립니다. </summary>
        public static void DrawBoxYZ(Vector3 center, Vector2 size, float angle, Color color, float duration = 0f)
        {
            DrawBox(Plane.YZ, center, size, angle, color, duration);
        }

        /// <summary> 지정한 평면(Plane)상에 회전된 2D 박스 와이어프레임을 그립니다. </summary>
        public static void DrawBox(Plane plane, Vector3 center, Vector2 size, float angle, Color color, float duration = 0f)
        {
            Vector2 h = size * 0.5f;

            // 평면에 따른 축 및 회전축(Rotation Axis) 결정
            var (xAxis, yAxis, rotAxis) = plane switch
            {
                Plane.XY => (Vector3.right, Vector3.up, Vector3.forward),
                Plane.XZ => (Vector3.right, Vector3.forward, Vector3.up),
                Plane.YZ => (Vector3.up, Vector3.forward, Vector3.right),
                _ => (Vector3.right, Vector3.up, Vector3.forward)
            };

            // angle로부터 쿼터니언 생성 (GC 할당 없음)
            Quaternion rotation = Quaternion.AngleAxis(angle, rotAxis);

            // 회전이 적용된 4개의 꼭짓점 계산
            Vector3 c0 = center + rotation * (xAxis * -h.x + yAxis * -h.y);
            Vector3 c1 = center + rotation * (xAxis * h.x + yAxis * -h.y);
            Vector3 c2 = center + rotation * (xAxis * h.x + yAxis * h.y);
            Vector3 c3 = center + rotation * (xAxis * -h.x + yAxis * h.y);

            Debug.DrawLine(c0, c1, color, duration);
            Debug.DrawLine(c1, c2, color, duration);
            Debug.DrawLine(c2, c3, color, duration);
            Debug.DrawLine(c3, c0, color, duration);
        }


        /// <summary> XY 평면상에 원을 그립니다. </summary>
        public static void DrawCircleXY(Vector3 center, float radius, Color color, float duration = 0f)
        {
            DrawCircle(Plane.XY, center, radius, 36, color, duration);
        }

        /// <summary> XZ 평면(지면)상에 원을 그립니다. </summary>
        public static void DrawCircleXZ(Vector3 center, float radius, Color color, float duration = 0f)
        {
            DrawCircle(Plane.XZ, center, radius, 36, color, duration);
        }

        /// <summary> YZ 평면상에 원을 그립니다. </summary>
        public static void DrawCircleYZ(Vector3 center, float radius, Color color, float duration = 0f)
        {
            DrawCircle(Plane.YZ, center, radius, 36, color, duration);
        }

        /// <summary> 지정한 평면(Plane)상에 세그먼트 분할 수에 맞춰 원을 그립니다. </summary>
        public static void DrawCircle(Plane plane, Vector3 center, float radius, int segments, Color color, float duration = 0f)
        {
            Vector3 axis1, axis2;
            switch (plane)
            {
                case Plane.XY:
                    axis1 = Vector3.right;
                    axis2 = Vector3.up;
                    break;
                case Plane.XZ:
                    axis1 = Vector3.right;
                    axis2 = Vector3.forward;
                    break;
                case Plane.YZ:
                    axis1 = Vector3.up;
                    axis2 = Vector3.forward;
                    break;
                default:
                    axis1 = Vector3.right;
                    axis2 = Vector3.up;
                    break;
            }
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + axis1 * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + Mathf.Cos(angle) * axis1 * radius + Mathf.Sin(angle) * axis2 * radius;
                Debug.DrawLine(prevPoint, newPoint, color, duration);
                prevPoint = newPoint;
            }
        }

        /// <summary> 중심점, 크기(size), 회전(rotation)을 적용하여 3D 큐브 와이어프레임을 그립니다. </summary>
        public static void DrawCube(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration = 0f)
        {
            Vector3 h = size * 0.5f;

            Vector3 c0 = center + rotation * new Vector3(-h.x, -h.y, -h.z);
            Vector3 c1 = center + rotation * new Vector3(h.x, -h.y, -h.z);
            Vector3 c2 = center + rotation * new Vector3(h.x, -h.y, h.z);
            Vector3 c3 = center + rotation * new Vector3(-h.x, -h.y, h.z);
            Vector3 c4 = center + rotation * new Vector3(-h.x, h.y, -h.z);
            Vector3 c5 = center + rotation * new Vector3(h.x, h.y, -h.z);
            Vector3 c6 = center + rotation * new Vector3(h.x, h.y, h.z);
            Vector3 c7 = center + rotation * new Vector3(-h.x, h.y, h.z);

            Debug.DrawLine(c0, c1, color, duration);
            Debug.DrawLine(c1, c2, color, duration);
            Debug.DrawLine(c2, c3, color, duration);
            Debug.DrawLine(c3, c0, color, duration);
            Debug.DrawLine(c4, c5, color, duration);
            Debug.DrawLine(c5, c6, color, duration);
            Debug.DrawLine(c6, c7, color, duration);
            Debug.DrawLine(c7, c4, color, duration);
            Debug.DrawLine(c0, c4, color, duration);
            Debug.DrawLine(c1, c5, color, duration);
            Debug.DrawLine(c2, c6, color, duration);
            Debug.DrawLine(c3, c7, color, duration);
        }
    }
}
