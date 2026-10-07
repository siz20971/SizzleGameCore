using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Sizzle.Toolkits.Diagnostics
{
    /// <summary>
    /// 로깅 심각도 수준(Info, Warning, Error)을 정의하는 열거형입니다.
    /// </summary>
    public enum LogLevel
    {
        /// <summary> 일반 정보성 로그 </summary>
        Info,
        /// <summary> 경고성 로그 </summary>
        Warning,
        /// <summary> 에러 로그 </summary>
        Error
    }

    /// <summary>
    /// 호출자 파일명, 메서드명, 라인 번호를 자동으로 포함하여 포맷팅된 콘솔 로그를 출력하는 유틸리티 클래스입니다.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Debug 빌드 또는 UnityEditor 환경에서만 로그를 출력합니다.
        /// <para>포맷: [LogLevel] [FileName]::[MethodName] (Line [LineNumber]): [Message]</para>
        /// </summary>
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Log(string message, LogLevel level = LogLevel.Info,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogImplement(message, level, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// Debug / Editor 조건을 무시하고 항상 로그를 출력합니다.
        /// [LogLevel] [FileName]::[MethodName] (Line [LineNumber]): [Message]
        /// </summary>
        public static void LogAlways(string message, LogLevel level = LogLevel.Info,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogImplement(message, level, memberName, filePath, lineNumber);
        }

        private static void LogImplement(string message, LogLevel level = LogLevel.Info,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string logMessage = $"[{level}] {System.IO.Path.GetFileName(filePath)}::{memberName} (Line {lineNumber}): {message}";
            switch (level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                default:
                    Debug.Log(logMessage);
                    break;
            }
        }
    }
}