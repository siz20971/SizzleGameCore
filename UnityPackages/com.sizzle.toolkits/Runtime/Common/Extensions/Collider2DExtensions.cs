using UnityEngine;
using Sizzle.Toolkits.Types;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// Collider2D의 경계(Bounds) 앵커 좌표 및 영역 내 무작위 좌표 획득을 지원하는 확장 메서드를 제공합니다.
    /// </summary>
    public static class Collider2DExtensions
    {
        /// <summary>
        /// Collider의 Anchor 위치를 반환합니다.
        /// </summary>
        public static Vector3 GetPosition(this Collider2D collider, AlignmentAnchor anchor)
        {
            if (!collider)
                return Vector3.zero;

            Vector3 position = Vector3.zero;
            Bounds bounds = collider.bounds;

            switch (anchor)
            {
                case AlignmentAnchor.TopLeft:
                    position.x = bounds.min.x;
                    position.y = bounds.max.y;
                    break;
                case AlignmentAnchor.MiddleLeft:
                    position.x = bounds.min.x;
                    position.y = bounds.center.y;
                    break;
                case AlignmentAnchor.BottomLeft:
                    position.x = bounds.min.x;
                    position.y = bounds.min.y;
                    break;
                case AlignmentAnchor.TopCenter:
                    position.x = bounds.center.x;
                    position.y = bounds.max.y;
                    break;
                case AlignmentAnchor.Center:
                    position.x = bounds.center.x;
                    position.y = bounds.center.y;
                    break;
                case AlignmentAnchor.BottomCenter:
                    position.x = bounds.center.x;
                    position.y = bounds.min.y;
                    break;
                case AlignmentAnchor.TopRight:
                    position.x = bounds.max.x;
                    position.y = bounds.max.y;
                    break;
                case AlignmentAnchor.MiddleRight:
                    position.x = bounds.max.x;
                    position.y = bounds.center.y;
                    break;
                case AlignmentAnchor.BottomRight:
                    position.x = bounds.max.x;
                    position.y = bounds.min.y;
                    break;
            }

            return position;
        }

        /// <summary>
        /// Collider 범위의 랜덤한 위치를 반환합니다.
        /// </summary>
        public static Vector3 GetRandomPosition(this Collider2D collider)
        {
            if (!collider)
                return Vector3.zero;

            Bounds bounds = collider.bounds;
            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Collider 범위의 Anchor 위치 기준으로 전체 크기를 section 크기만큼 나눈 범위에서 랜덤한 위치를 반환합니다. 
        /// </summary>
        public static Vector3 GetRandomPosition(this Collider2D collider, AlignmentAnchor anchor, int section)
        {
            float width = collider.bounds.size.x / section;
            float height = collider.bounds.size.y / section;
            Vector3 originPosition = collider.GetPosition(anchor);

            Vector3 randomPosition = Vector3.zero;

            switch (anchor)
            {
                case AlignmentAnchor.TopLeft:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x, originPosition.x + width);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height, originPosition.y);
                    break;
                case AlignmentAnchor.MiddleLeft:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x, originPosition.x + width);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height / 2f, originPosition.y + height / 2f);
                    break;
                case AlignmentAnchor.BottomLeft:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x, originPosition.x + width);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y, originPosition.y + height);
                    break;
                case AlignmentAnchor.TopCenter:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width / 2f, originPosition.x + width / 2f);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height, originPosition.y);
                    break;
                case AlignmentAnchor.Center:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width / 2f, originPosition.x + width / 2f);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height / 2f, originPosition.y + height / 2f);
                    break;
                case AlignmentAnchor.BottomCenter:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width / 2f, originPosition.x + width / 2f);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y, originPosition.y + height);
                    break;
                case AlignmentAnchor.TopRight:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width, originPosition.x);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height, originPosition.y);
                    break;
                case AlignmentAnchor.MiddleRight:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width, originPosition.x);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y - height / 2f, originPosition.y + height / 2f);
                    break;
                case AlignmentAnchor.BottomRight:
                    randomPosition.x = UnityEngine.Random.Range(originPosition.x - width, originPosition.x);
                    randomPosition.y = UnityEngine.Random.Range(originPosition.y, originPosition.y + height);
                    break;
            }

            return randomPosition;
        }
    }
}
