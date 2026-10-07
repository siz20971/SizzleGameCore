using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Sizzle.Toolkits.Input
{
    /// <summary>
    /// Unity New Input System의 InputActionAsset에 런타임 키 리바인딩(바인딩 오버라이드)을 일괄 적용하고 JSON 형태로 관리하는 헬퍼 클래스입니다.
    /// </summary>
    public static class InputActionOverrideHelper
    {
        /// <summary>
        /// 바인딩 오버라이드를 적용할 액션 이름과 원본 입력 경로, 교체할 입력 경로 정보 구조체입니다.
        /// </summary>
        public readonly struct OverrideEntry
        {
            /// <summary> 오버라이드 대상 액션 이름 (예: "Move", "Jump") </summary>
            public string ActionName { get; }
            /// <summary> 원본 입력 경로 (예: "<Keyboard>/w") </summary>
            public string SourceInputPath { get; }
            /// <summary> 새로 교체할 입력 경로 (예: "<Keyboard>/upArrow") </summary>
            public string OverrideInputPath { get; }

            public OverrideEntry(string actionName, string sourceInputPath, string overrideInputPath)
            {
                ActionName = actionName;
                SourceInputPath = sourceInputPath;
                OverrideInputPath = overrideInputPath;
            }
        }

        /// <summary>
        /// 대상 InputActionAsset의 기존 바인딩 오버라이드를 초기화하고, 전달된 오버라이드 목록을 적용합니다.
        /// </summary>
        /// <param name="asset">오버라이드를 적용할 InputActionAsset</param>
        /// <param name="overrides">적용할 OverrideEntry 컬렉션</param>
        public static void OverrideActions(InputActionAsset asset, IEnumerable<OverrideEntry> overrides)
        {
            if (asset == null)
                return;

            string presetJson = BuildPreset(asset, overrides);

            asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(presetJson))
                asset.LoadBindingOverridesFromJson(presetJson);
        }

        private static string BuildPreset(InputActionAsset baseAsset, IEnumerable<OverrideEntry> overrides)
        {
            if (baseAsset == null)
                return string.Empty;

            var clone = Object.Instantiate(baseAsset);
            try
            {
                ApplyOverrides(clone, overrides);
                return clone.SaveBindingOverridesAsJson();
            }
            finally
            {
                Object.Destroy(clone);
            }
        }

        private static void ApplyOverrides(InputActionAsset asset, IEnumerable<OverrideEntry> overrides)
        {
            if (asset == null || overrides == null)
                return;

            foreach (OverrideEntry entry in overrides)
            {
                InputAction action = asset.FindAction(entry.ActionName, false);
                if (action == null)
                    continue;

                int bindingIndex = FindBindingIndex(action, entry.SourceInputPath);
                if (bindingIndex < 0)
                    continue;

                action.ApplyBindingOverride(bindingIndex, new InputBinding { overridePath = entry.OverrideInputPath });
            }
        }

        private static int FindBindingIndex(InputAction action, string sourceInputPath)
        {
            if (action == null)
                return -1;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.path == sourceInputPath)
                    return i;
            }

            return -1;
        }
    }
}
