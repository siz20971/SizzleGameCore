namespace Sizzle.AbilitySystem
{
    public enum AbilityActivateResult
    {
        None,
        FailedAbilityNotFound,
        FailedCanNotUse,
        FailedNotHasAllRequiredTag,
        FailedHasAnyBlockTag,
        FailedBlockedByOther,
        FailedAlreadyActive,
        FailedInvalidActivateInfo,
        Success,
    }
}