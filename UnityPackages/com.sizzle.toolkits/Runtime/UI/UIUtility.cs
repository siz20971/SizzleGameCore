using UnityEngine;

namespace Sizzle.Toolkits.UI
{
    /// <summary>
    /// UI 배치, 화면 경계 클램핑, 좌표 변환을 지원하는 범용 UI 유틸리티 클래스입니다.
    /// </summary>
    public static class UIUtility
    {
        /// <summary>
        /// 대상 <see cref="RectTransform"/>이 화면 경계(Screen Bounds) 밖으로 벗어나지 않도록 스크린 좌표를 클램핑하여 반환합니다.
        /// 툴팁, 컨텍스트 메뉴, 플로팅 팝업 등이 화면 가장자리에 잘리는 것을 방지할 때 사용합니다.
        /// </summary>
        /// <param name="rect">배치할 대상 RectTransform</param>
        /// <param name="targetScreenPos">배치하고자 하는 원래 스크린 좌표</param>
        /// <param name="padding">화면 모서리와의 여백(픽셀)</param>
        /// <returns>화면 내로 보정된 스크린 좌표</returns>
        public static Vector2 ClampToScreen(RectTransform rect, Vector2 targetScreenPos, float padding = 10f)
        {
            if (rect == null) return targetScreenPos;

            Vector2 size = rect.rect.size;
            Vector2 pivot = rect.pivot;

            float minX = padding + (size.x * pivot.x);
            float maxX = Screen.width - padding - (size.x * (1f - pivot.x));
            float minY = padding + (size.y * pivot.y);
            float maxY = Screen.height - padding - (size.y * (1f - pivot.y));

            // 화면이 팝업 크기보다 작은 경우 안전 보정
            if (minX > maxX) minX = maxX = (minX + maxX) * 0.5f;
            if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;

            float clampedX = Mathf.Clamp(targetScreenPos.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetScreenPos.y, minY, maxY);

            return new Vector2(clampedX, clampedY);
        }

        /// <summary>
        /// 대상 <see cref="RectTransform"/>이 부모 또는 지정된 바운더리 RectTransform 영역을 벗어나지 않도록 anchoredPosition을 클램핑합니다.
        /// </summary>
        /// <param name="rect">배치할 대상 RectTransform</param>
        /// <param name="boundary">가둘 바운더리 RectTransform</param>
        /// <param name="padding">경계 여백</param>
        /// <returns>보정된 anchoredPosition</returns>
        public static Vector2 ClampToBoundary(RectTransform rect, RectTransform boundary, float padding = 0f)
        {
            if (rect == null || boundary == null) return rect != null ? rect.anchoredPosition : Vector2.zero;

            Rect bRect = boundary.rect;
            Vector2 rSize = rect.rect.size;
            Vector2 rPivot = rect.pivot;

            float minX = bRect.xMin + padding + (rSize.x * rPivot.x);
            float maxX = bRect.xMax - padding - (rSize.x * (1f - rPivot.x));
            float minY = bRect.yMin + padding + (rSize.y * rPivot.y);
            float maxY = bRect.yMax - padding - (rSize.y * (1f - rPivot.y));

            if (minX > maxX) minX = maxX = (minX + maxX) * 0.5f;
            if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;

            Vector2 currentPos = rect.anchoredPosition;
            float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
            float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);

            return new Vector2(clampedX, clampedY);
        }

        /// <summary>
        /// 3D 월드 좌표를 주어진 Canvas 기준의 로컬 좌표(anchoredPosition)로 변환합니다.
        /// </summary>
        /// <param name="canvas">대상 Canvas</param>
        /// <param name="worldPosition">3D 월드 좌표</param>
        /// <param name="worldCamera">월드를 비추는 카메라 (null일 경우 Camera.main)</param>
        /// <returns>Canvas 로컬 좌표</returns>
        public static Vector2 WorldToCanvasPosition(Canvas canvas, Vector3 worldPosition, Camera worldCamera = null)
        {
            if (canvas == null) return Vector2.zero;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            Vector3 screenPos = cam != null ? cam.WorldToScreenPoint(worldPosition) : (Vector3)worldPosition;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvasCam, out Vector2 localPoint);
            return localPoint;
        }

        /// <summary>
        /// 스크린 좌표를 주어진 Canvas 기준의 로컬 좌표로 변환합니다.
        /// </summary>
        /// <param name="canvas">대상 Canvas</param>
        /// <param name="screenPosition">스크린 좌표 (예: Input.mousePosition)</param>
        /// <returns>Canvas 로컬 좌표</returns>
        public static Vector2 ScreenToCanvasPosition(Canvas canvas, Vector2 screenPosition)
        {
            if (canvas == null) return screenPosition;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCam, out Vector2 localPoint);
            return localPoint;
        }
    }
}
