using UnityEngine;
using Sizzle.Toolkits.Types;

namespace Sizzle.Toolkits.Math
{
    /// <summary>
    /// 2D 벡터/좌표와 8방향 열거형(Direction8) 간의 상호 변환 유틸리티를 제공합니다.
    /// </summary>
    public static class Direction2DUtils
    {
        /// <summary> Vector2 방향 벡터를 가장 가까운 Direction8 방향으로 변환합니다. </summary>
        public static Direction8 ToDirection8FromVector2(this Vector2 direction)
            => ToDirection8FromXY(direction.x, direction.y);

        /// <summary> x, y 좌표 방향 벡터를 가장 가까운 Direction8 방향으로 변환합니다. (각도 45도 간격 기준 분할) </summary>
        public static Direction8 ToDirection8FromXY(float x, float y)
        {
            if (x == 0f && y == 0f)
                return Direction8.None;

            float angle360 = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            if (angle360 < 0f)
                angle360 += 360f;

            if (angle360 >= 337.5f || angle360 < 22.5f)
                return Direction8.Right;
            else if (angle360 < 67.5f)
                return Direction8.UpRight;
            else if (angle360 < 112.5f)
                return Direction8.Up;
            else if (angle360 < 157.5f)
                return Direction8.UpLeft;
            else if (angle360 < 202.5f)
                return Direction8.Left;
            else if (angle360 < 247.5f)
                return Direction8.DownLeft;
            else if (angle360 < 292.5f)
                return Direction8.Down;
            else // angle360 < 337.5f
                return Direction8.DownRight;
        }

        /// <summary> Direction8 방향을 x, y 단위 성분으로 분해하여 반환합니다. </summary>
        public static void ToXYFromDirection8(Direction8 direction, out float x, out float y)
        {
            ToXYFromDirection8(direction, out Vector2 vector);
            x = vector.x;
            y = vector.y;
        }

        /// <summary> Direction8 방향을 단위 벡터(Vector2)로 변환합니다. </summary>
        public static void ToXYFromDirection8(Direction8 direction, out Vector2 vector)
        {
            switch (direction)
            {
                case Direction8.None:
                    vector = Vector2.zero;
                    break;
                case Direction8.Up:
                    vector = Vector2.up;
                    break;
                case Direction8.UpRight:
                    vector = new Vector2(1f, 1f).normalized;
                    break;
                case Direction8.Right:
                    vector = Vector2.right;
                    break;
                case Direction8.DownRight:
                    vector = new Vector2(1f, -1f).normalized;
                    break;
                case Direction8.Down:
                    vector = Vector2.down;
                    break;
                case Direction8.DownLeft:
                    vector = new Vector2(-1f, -1f).normalized;
                    break;
                case Direction8.Left:
                    vector = Vector2.left;
                    break;
                case Direction8.UpLeft:
                    vector = new Vector2(-1f, 1f).normalized;
                    break;
                default:
                    vector = Vector2.zero;
                    break;
            }
        }

        /// <summary> Direction8 방향을 단위 벡터(Vector2)로 변환하는 확장 메서드입니다. </summary>
        public static Vector2 ToVector2(this Direction8 direction)
        {
            ToXYFromDirection8(direction, out Vector2 vector);
            return vector;
        }
    }
}