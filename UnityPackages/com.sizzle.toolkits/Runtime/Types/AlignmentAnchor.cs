namespace Sizzle.Toolkits.Types
{
    /// <summary>
    /// 2D 및 UI 영역의 기준 정렬 위치를 나타내는 9방향 앵커 열거형입니다.
    /// </summary>
    public enum AlignmentAnchor
    {
        /// <summary> 좌하단 </summary>
        BottomLeft = 0,
        /// <summary> 중앙 하단 </summary>
        BottomCenter = 1,
        /// <summary> 중앙 하단 (별칭) </summary>
        Bottom = BottomCenter,
        /// <summary> 우하단 </summary>
        BottomRight = 2,
        /// <summary> 좌측 중앙 </summary>
        MiddleLeft = 3,
        /// <summary> 좌측 중앙 (별칭) </summary>
        Left = MiddleLeft,
        /// <summary> 정중앙 </summary>
        Center = 4,
        /// <summary> 우측 중앙 </summary>
        MiddleRight = 5,
        /// <summary> 우측 중앙 (별칭) </summary>
        Right = MiddleRight,
        /// <summary> 좌상단 </summary>
        TopLeft = 6,
        /// <summary> 중앙 상단 </summary>
        TopCenter = 7,
        /// <summary> 중앙 상단 (별칭) </summary>
        Top = TopCenter,
        /// <summary> 우상단 </summary>
        TopRight = 8,
    }
}
