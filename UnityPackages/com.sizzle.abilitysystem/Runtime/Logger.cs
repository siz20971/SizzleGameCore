using System.Text;

namespace Sizzle.AbilitySystem
{
    internal class Logger
    {
        private const string Prefix = "[AbilitySystem] ";
        private static StringBuilder _sb = new StringBuilder();

        public static void Log(string message)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.Append(message);
            UnityEngine.Debug.Log(_sb.ToString());
        }

        public static void LogFormat(string format, params object[] args)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.AppendFormat(format, args);
            UnityEngine.Debug.Log(_sb.ToString());
        }

        public static void LogWarning(string message)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.Append(message);
            UnityEngine.Debug.LogWarning(_sb.ToString());
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.AppendFormat(format, args);
            UnityEngine.Debug.LogWarning(_sb.ToString());
        }

        public static void LogError(string message)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.Append(message);
            UnityEngine.Debug.LogError(_sb.ToString());
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            _sb.Clear();
            _sb.Append(Prefix);
            _sb.AppendFormat(format, args);
            UnityEngine.Debug.LogError(_sb.ToString());
        }
    }
}