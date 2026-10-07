namespace Sizzle.Toolkits.Types
{
    /// <summary>
    /// 그리드 배치 및 디버그 렌더링 시 기준이 되는 2차원 투영 평면 축 조합을 정의하는 열거형입니다.
    /// </summary>
    public enum AxisPlane
    {
        /// <summary> 기본 수평 X, 수직 Y 평면 </summary>
        XY = 0,
        /// <summary> 기본 수직 Y, 수평 X 평면 </summary>
        YX = 1,
        /// <summary> 기본 수직 Y, 깊이 Z 평면 </summary>
        YZ = 2,
        /// <summary> 기본 깊이 Z, 수직 Y 평면 </summary>
        ZY = 3,
        /// <summary> 기본 수평 X, 깊이 Z 탑다운 평면 </summary>
        XZ = 4,
        /// <summary> 기본 깊이 Z, 수평 X 탑다운 평면 </summary>
        ZX = 5,
    }
}
