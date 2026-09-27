using System;
using UnityEngine;

namespace Sizzle.AbilitySystem.Actions
{
    /// <summary>
    /// 콘솔에 디버그 로그를 출력하고 즉시 다음 액션으로 진행하는 테스트/디버깅용 액션입니다.
    /// </summary>
    [Serializable]
    public class DebugLogAction : AbilityAction
    {
        [Header("Debug Settings")]
        [TextArea(1, 3)]
        public string Message = "Sequence action executed!";
        public LogType LogType = LogType.Log;

        protected override void OnStart(AbilityRuntimeContext context)
        {
            string senderName = context?.GameObject != null ? context.GameObject.name : "Unknown";
            string logText = $"[{nameof(DebugLogAction)}] [{senderName}] {Message}";

            switch (LogType)
            {
                case LogType.Warning:
                    Debug.LogWarning(logText, context?.GameObject);
                    break;
                case LogType.Error:
                    Debug.LogError(logText, context?.GameObject);
                    break;
                default:
                    Debug.Log(logText, context?.GameObject);
                    break;
            }

            Complete();
        }
    }
}
