using System;
using UnityEngine;
#if CORE_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;
#endif

namespace Sizzle.Toolkits.Input
{
    /// <summary>
    /// 감지된 입력 디바이스의 종류를 나타내는 열거형입니다.
    /// </summary>
    public enum InputDeviceType
    {
        /// <summary>키보드 및 마우스</summary>
        KeyboardMouse,
        /// <summary>Microsoft Xbox 계열 게임패드</summary>
        XboxGamepad,
        /// <summary>Sony PlayStation DualShock / DualSense 게임패드</summary>
        PlayStationGamepad,
        /// <summary>Nintendo Switch Pro Controller 및 Joy-Con</summary>
        SwitchGamepad,
        /// <summary>기타 범용 DirectInput / XInput 호환 게임패드</summary>
        GenericGamepad,
        /// <summary>모바일 터치스크린</summary>
        TouchScreen,
        /// <summary>알 수 없거나 초기화되지 않은 장치</summary>
        Unknown
    }

    /// <summary>
    /// 입력 장치의 하드웨어 연결 상태 변경 유형입니다.
    /// </summary>
    public enum DeviceConnectionStatus
    {
        /// <summary>새로운 장치가 연결/인식됨</summary>
        Connected,
        /// <summary>장치가 물리적으로 분리/연결 해제됨</summary>
        Disconnected,
        /// <summary>일시적으로 끊겼던 장치가 다시 연결됨</summary>
        Reconnected,
        /// <summary>장치가 시스템에서 완전히 제거됨</summary>
        Removed,
        /// <summary>장치의 레이아웃 또는 하드웨어 구성이 변경됨</summary>
        ConfigurationChanged
    }

    /// <summary>
    /// <para>Unity New Input System과 연동하여 연결된 입력 장치의 변화, 활성 조작 디바이스의 실시간 전환,
    /// 그리고 컨트롤러 연결/해제/재연결 등 하드웨어 특수 이벤트를 통합 감지 및 관리하는 순수 C# 정적 유틸리티 클래스입니다.</para>
    /// <para>게임패드 배터리 방전이나 케이블 분리로 인한 패드 단선 감지, 게임패드 연결 시 자동 일시정지(Pause),
    /// 키보드/마우스와 게임패드 간 조작 전환 시 UI 키 가이드 아이콘 실시간 스위칭 등에 활용됩니다.</para>
    /// </summary>
    public static class InputDeviceDetector
    {
        private static bool s_isEnabled;
        private static InputDeviceType s_currentDeviceType = InputDeviceType.KeyboardMouse;

#if CORE_INPUTSYSTEM
        private static UnityEngine.InputSystem.InputDevice s_lastActiveDevice;
        private static IDisposable s_anyButtonEventListener;
#endif

        /// <summary>
        /// 플레이어가 현재 조작 중인 입력 장치 유형(키보드/패드 등)이 변경되었을 때 발생하는 이벤트입니다.
        /// (인자: 변경된 새로운 InputDeviceType)
        /// </summary>
        public static event Action<InputDeviceType> OnDeviceTypeChanged;

        /// <summary>
        /// 게임패드나 입력 장치가 물리적으로 연결/해제/재연결되었을 때 발생하는 하드웨어 상태 이벤트입니다.
        /// (인자: 장치 이름, 연결 상태 유형)
        /// </summary>
        public static event Action<string, DeviceConnectionStatus> OnDeviceConnectionStatusChanged;

        /// <summary>
        /// 현재 마지막으로 조작이 감지된 활성 입력 장치 유형입니다.
        /// </summary>
        public static InputDeviceType CurrentDeviceType => s_currentDeviceType;

        /// <summary>
        /// 감지기가 활성화되어 입력 이벤트를 수신 중인지 여부입니다.
        /// </summary>
        public static bool IsEnabled => s_isEnabled;

#if CORE_INPUTSYSTEM
        /// <summary>
        /// 마지막으로 입력을 발생시킨 Unity InputSystem 장치 인스턴스입니다.
        /// </summary>
        public static UnityEngine.InputSystem.InputDevice LastActiveDevice => s_lastActiveDevice;
#endif

        /// <summary>
        /// 입력 장치 모니터링을 활성화하고 Input System의 액션 및 디바이스 이벤트를 구독합니다.
        /// 게임 시작 시 또는 컨트롤러 매니저 초기화 시 호출합니다.
        /// </summary>
        public static void Enable()
        {
            if (s_isEnabled) return;
            s_isEnabled = true;

#if CORE_INPUTSYSTEM
            InputSystem.onActionChange += HandleActionChange;
            InputSystem.onDeviceChange += HandleDeviceChange;
            s_anyButtonEventListener = InputSystem.onAnyButtonPress.Call(HandleAnyButtonPress);

            // 초기 연결 상태 확인
            if (Gamepad.current != null)
            {
                s_lastActiveDevice = Gamepad.current;
                s_currentDeviceType = ResolveDeviceType(Gamepad.current);
            }
#endif
        }

        /// <summary>
        /// 입력 장치 모니터링을 비활성화하고 구독 중인 이벤트를 모두 해제합니다.
        /// </summary>
        public static void Disable()
        {
            if (!s_isEnabled) return;
            s_isEnabled = false;

#if CORE_INPUTSYSTEM
            InputSystem.onActionChange -= HandleActionChange;
            InputSystem.onDeviceChange -= HandleDeviceChange;
            s_anyButtonEventListener?.Dispose();
            s_anyButtonEventListener = null;
#endif
        }

#if CORE_INPUTSYSTEM
        private static void HandleAnyButtonPress(InputControl control)
        {
            if (control == null || control.device == null) return;
            var device = control.device;
            s_lastActiveDevice = device;

            InputDeviceType detected = ResolveDeviceType(device);
            if (detected != InputDeviceType.Unknown && detected != s_currentDeviceType)
            {
                s_currentDeviceType = detected;
                OnDeviceTypeChanged?.Invoke(detected);
            }
        }

        private static void HandleActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed)
            {
                if (obj is InputAction action && action.activeControl != null)
                {
                    var device = action.activeControl.device;
                    s_lastActiveDevice = device;

                    InputDeviceType detected = ResolveDeviceType(device);
                    if (detected != InputDeviceType.Unknown && detected != s_currentDeviceType)
                    {
                        s_currentDeviceType = detected;
                        OnDeviceTypeChanged?.Invoke(detected);
                    }
                }
            }
        }

        private static void HandleDeviceChange(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
        {
            if (device == null) return;

            DeviceConnectionStatus? status = change switch
            {
                InputDeviceChange.Added => DeviceConnectionStatus.Connected,
                InputDeviceChange.Removed => DeviceConnectionStatus.Removed,
                InputDeviceChange.Disconnected => DeviceConnectionStatus.Disconnected,
                InputDeviceChange.Reconnected => DeviceConnectionStatus.Reconnected,
                InputDeviceChange.ConfigurationChanged => DeviceConnectionStatus.ConfigurationChanged,
                _ => null
            };

            if (status.HasValue)
            {
                OnDeviceConnectionStatusChanged?.Invoke(device.displayName ?? device.name, status.Value);

                // 현재 사용 중이던 패드가 끊겼을 때 키보드/마우스로 안전 폴백
                if (status.Value == DeviceConnectionStatus.Disconnected && device == s_lastActiveDevice)
                {
                    s_currentDeviceType = InputDeviceType.KeyboardMouse;
                    OnDeviceTypeChanged?.Invoke(InputDeviceType.KeyboardMouse);
                }
            }
        }

        /// <summary>
        /// 전달된 InputDevice로부터 세부적인 InputDeviceType을 판별합니다.
        /// </summary>
        public static InputDeviceType ResolveDeviceType(UnityEngine.InputSystem.InputDevice device)
        {
            if (device == null) return InputDeviceType.Unknown;

            if (device is Keyboard || device is Mouse)
            {
                return InputDeviceType.KeyboardMouse;
            }
            if (device is XInputController)
            {
                return InputDeviceType.XboxGamepad;
            }
            if (device is DualShockGamepad)
            {
                return InputDeviceType.PlayStationGamepad;
            }
            if (device is SwitchProController)
            {
                return InputDeviceType.SwitchGamepad;
            }
            if (device is Gamepad)
            {
                // 제품명 기반 추가 식별 시도
                string layout = device.layout.ToLowerInvariant();
                if (layout.Contains("dualshock") || layout.Contains("dualsense"))
                    return InputDeviceType.PlayStationGamepad;
                if (layout.Contains("xbox"))
                    return InputDeviceType.XboxGamepad;
                if (layout.Contains("switch"))
                    return InputDeviceType.SwitchGamepad;

                return InputDeviceType.GenericGamepad;
            }
            if (device is Touchscreen)
            {
                return InputDeviceType.TouchScreen;
            }

            return InputDeviceType.Unknown;
        }
#endif
    }

    /// <summary>
    /// InputDeviceType에 대한 판별 및 편의 확장 메서드를 제공합니다.
    /// </summary>
    public static class InputDeviceTypeExtensions
    {
        /// <summary>
        /// 해당 입력 디바이스가 게임패드 계열(Xbox, PlayStation, Switch, Generic)인지 여부를 반환합니다.
        /// </summary>
        public static bool IsGamepad(this InputDeviceType deviceType)
        {
            switch (deviceType)
            {
                case InputDeviceType.XboxGamepad:
                case InputDeviceType.PlayStationGamepad:
                case InputDeviceType.SwitchGamepad:
                case InputDeviceType.GenericGamepad:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 해당 입력 디바이스가 키보드/마우스인지 여부를 반환합니다.
        /// </summary>
        public static bool IsKeyboardMouse(this InputDeviceType deviceType)
        {
            return deviceType == InputDeviceType.KeyboardMouse;
        }
    }
}
