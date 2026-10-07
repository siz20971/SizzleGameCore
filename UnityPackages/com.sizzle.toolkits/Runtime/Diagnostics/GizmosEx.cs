using UnityEngine;

namespace Sizzle.Toolkits.Diagnostics
{
    /// <summary>
    /// Unity Gizmos 확장을 통해 단방향/양방향 화살표 및 다양한 2D 콜라이더 영역(Box, Circle, Capsule, Polygon 등)의 시각화를 지원하는 유틸리티입니다.
    /// </summary>
    public static class GizmosEx
    {
        /// <summary>
        /// 시작점(origin)에서 목적지(dest) 방향으로 단방향 화살표 기즈모를 렌더링합니다.
        /// </summary>
        /// <param name="origin">화살표 시작점</param>
        /// <param name="dest">화살표 끝점 (촉 위치)</param>
        /// <param name="c">화살표 색상 (null일 경우 빨간색)</param>
        /// <param name="headLength">화살표 촉 길이</param>
        public static void DrawArrow(Vector3 origin, Vector3 dest, Color? c = null, float headLength = 1f)
        {
            Color oldColor = Gizmos.color;

            Gizmos.color = c.GetValueOrDefault(Color.red);

            Vector3 dir = dest - origin;
            Gizmos.DrawLine(origin, dest);

            Vector3 right = Quaternion.AngleAxis(30, Vector3.forward) * -dir.normalized * headLength;
            Vector3 left = Quaternion.AngleAxis(-30, Vector3.forward) * -dir.normalized * headLength;

            Gizmos.DrawLine(dest, dest + right);
            Gizmos.DrawLine(dest, dest + left);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 시작점과 끝점 양쪽에 화살표 촉이 있는 양방향 화살표 기즈모를 렌더링합니다.
        /// </summary>
        /// <param name="origin">시작점</param>
        /// <param name="dest">끝점</param>
        /// <param name="c">화살표 색상 (null일 경우 빨간색)</param>
        /// <param name="headLength">화살표 촉 길이</param>
        public static void DrawTwoWayArrow(Vector3 origin, Vector3 dest, Color? c = null, float headLength = 1f)
        {
            Color oldColor = Gizmos.color;

            Gizmos.color = c.GetValueOrDefault(Color.red);

            Vector3 dir = dest - origin;
            Gizmos.DrawLine(origin, dest);

            Vector3 right1 = Quaternion.AngleAxis(30, Vector3.forward) * -dir.normalized * headLength;
            Vector3 left1 = Quaternion.AngleAxis(-30, Vector3.forward) * -dir.normalized * headLength;
            Gizmos.DrawLine(dest, dest + right1);
            Gizmos.DrawLine(dest, dest + left1);

            Vector3 right2 = Quaternion.AngleAxis(30, Vector3.forward) * dir.normalized * headLength;
            Vector3 left2 = Quaternion.AngleAxis(-30, Vector3.forward) * dir.normalized * headLength;
            Gizmos.DrawLine(origin, origin + right2);
            Gizmos.DrawLine(origin, origin + left2);

            Gizmos.color = oldColor;
        }

        /// <summary>
        /// 2D 콜라이더의 실제 형태(Box, Circle, Capsule, Polygon, Edge, Composite)에 맞춰 반투명 면적과 외곽선 기즈모를 그립니다.
        /// </summary>
        /// <param name="col">대상 Collider2D</param>
        /// <param name="color">기본 색상</param>
        /// <param name="lineAlpha">외곽선 투명도 (기본 0.8)</param>
        /// <param name="areaAlpha">내부 면적 투명도 (기본 0.01)</param>
        public static void DrawCollider2DArea(
            Collider2D col,
            Color color,
            float lineAlpha = 0.8f,
            float areaAlpha = 0.01f)
        {
            if (col == null)
                return;

            Color areaColor = new Color(color.r, color.g, color.b, areaAlpha);
            Color lineColor = new Color(color.r, color.g, color.b, lineAlpha);

            if (col is BoxCollider2D boxCollider)
            {
                DrawBoxCollider(boxCollider, areaColor, lineColor);
            }
            else if (col is CircleCollider2D circleCollider)
            {
                DrawCircleCollider(circleCollider, areaColor, lineColor);
            }
            else if (col is CapsuleCollider2D capsuleCollider)
            {
                DrawCapsuleCollider(capsuleCollider, areaColor, lineColor);
            }
            else if (col is PolygonCollider2D polygonCollider)
            {
                DrawPolygonCollider(polygonCollider, areaColor, lineColor);
            }
            else if (col is EdgeCollider2D edgeCollider)
            {
                DrawEdgeCollider(edgeCollider, lineColor);
            }
            else if (col is CompositeCollider2D compositeCollider)
            {
                DrawCompositeCollider(compositeCollider, lineColor);
            }
        }

        private static void DrawBoxCollider(BoxCollider2D collider, Color areaColor, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            Gizmos.color = areaColor;
            Gizmos.DrawCube(collider.offset, collider.size);

            Gizmos.color = lineColor;
            Gizmos.DrawWireCube(collider.offset, collider.size);

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }

        private static void DrawCircleCollider(CircleCollider2D collider, Color areaColor, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            Gizmos.color = areaColor;
            Gizmos.DrawSphere(
                collider.offset,
                collider.radius);

            Gizmos.color = lineColor;
            Gizmos.DrawWireSphere(
                collider.offset,
                collider.radius);

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }

        private static void DrawCapsuleCollider(CapsuleCollider2D collider, Color areaColor, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            Vector2 size = collider.size;

            float radius;
            float height;

            if (collider.direction == CapsuleDirection2D.Vertical)
            {
                radius = size.x * 0.5f;
                height = size.y;
            }
            else
            {
                radius = size.y * 0.5f;
                height = size.x;
            }

            float cylinderLength = Mathf.Max(0f, height - radius * 2f);

            Vector3 offset = collider.offset;

            // Unity 기본 Gizmo만 사용하려면 근사 표현
            Gizmos.color = areaColor;

            if (collider.direction == CapsuleDirection2D.Vertical)
            {
                Gizmos.DrawCube(
                    offset,
                    new Vector3(size.x, cylinderLength, 0f));

                Gizmos.color = lineColor;
                Gizmos.DrawWireCube(
                    offset,
                    new Vector3(size.x, cylinderLength, 0f));

                Gizmos.DrawWireSphere(
                    offset + Vector3.up * (cylinderLength * 0.5f),
                    radius);

                Gizmos.DrawWireSphere(
                    offset + Vector3.down * (cylinderLength * 0.5f),
                    radius);
            }
            else
            {
                Gizmos.DrawCube(
                    offset,
                    new Vector3(cylinderLength, size.y, 0f));

                Gizmos.color = lineColor;
                Gizmos.DrawWireCube(
                    offset,
                    new Vector3(cylinderLength, size.y, 0f));

                Gizmos.DrawWireSphere(
                    offset + Vector3.right * (cylinderLength * 0.5f),
                    radius);

                Gizmos.DrawWireSphere(
                    offset + Vector3.left * (cylinderLength * 0.5f),
                    radius);
            }

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }

        private static void DrawPolygonCollider(PolygonCollider2D collider, Color areaColor, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            for (int pathIndex = 0; pathIndex < collider.pathCount; pathIndex++)
            {
                Vector2[] points = collider.GetPath(pathIndex);

                if (points.Length < 2)
                    continue;

                Gizmos.color = lineColor;

                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 a = points[i];
                    Vector3 b = points[(i + 1) % points.Length];

                    Gizmos.DrawLine(a, b);
                }
            }

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }

        private static void DrawEdgeCollider(EdgeCollider2D collider, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            Vector2[] points = collider.points;

            if (points.Length >= 2)
            {
                Gizmos.color = lineColor;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    Gizmos.DrawLine(
                        points[i],
                        points[i + 1]);
                }
            }

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }

        private static void DrawCompositeCollider(CompositeCollider2D collider, Color lineColor)
        {
            Color oldColor = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = collider.transform.localToWorldMatrix;

            for (int pathIndex = 0;
                 pathIndex < collider.pathCount;
                 pathIndex++)
            {
                int pointCount = collider.GetPathPointCount(pathIndex);

                if (pointCount < 2)
                    continue;

                Vector2[] points = new Vector2[pointCount];
                collider.GetPath(pathIndex, points);

                Gizmos.color = lineColor;

                for (int i = 0; i < pointCount; i++)
                {
                    Vector3 a = points[i];
                    Vector3 b = points[(i + 1) % pointCount];

                    Gizmos.DrawLine(a, b);
                }
            }

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;
        }
    }
}
