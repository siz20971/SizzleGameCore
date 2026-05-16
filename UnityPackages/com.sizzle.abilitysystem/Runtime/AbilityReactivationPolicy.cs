namespace Sizzle.AbilitySystem
{
    /// <summary>
    /// 어빌리티가 실행 중일 때 같은 어빌리티를 재실행하면 어떻게 동작할지에 대한 정책.
    /// </summary>
    public enum AbilityReactivationPolicy
    {
        /// <summary>이미 활성화된 경우 재실행을 무시합니다. (기본값)</summary>
        Deny,

        /// <summary>기존 어빌리티는 Canceled 사유로 종료된 후 처음부터 재실행됩니다.</summary>
        RestartFromBeginning,

        /// <summary>
        /// 이미 활성화된 경우 OnReactivate를 호출합니다. IsActive 상태가 유지됩니다.
        /// 콤보, 차지 등 연속성이 필요한 Ability에서 OnReactivate를 override하여 사용합니다.
        /// </summary>
        Reactivate,
    }
}