namespace Sizzle.Toolkits.Types
{
    /// <summary>
    /// 2D 8방향(상, 하, 좌, 우 및 4개 대각선)을 나타내는 열거형입니다.
    /// </summary>
    public enum Direction8
    {
        /// <summary> 방향 없음 </summary>
        None = 0,
        /// <summary> 위쪽 (상) </summary>
        Up = 1,
        /// <summary> 오른쪽 위 (우상) </summary>
        UpRight = 2,
        /// <summary> 오른쪽 (우) </summary>
        Right = 3,
        /// <summary> 오른쪽 아래 (우하) </summary>
        DownRight = 4,
        /// <summary> 아래쪽 (하) </summary>
        Down = 5,
        /// <summary> 왼쪽 아래 (좌하) </summary>
        DownLeft = 6,
        /// <summary> 왼쪽 (좌) </summary>
        Left = 7,
        /// <summary> 왼쪽 위 (좌상) </summary>
        UpLeft = 8,
    }
}
