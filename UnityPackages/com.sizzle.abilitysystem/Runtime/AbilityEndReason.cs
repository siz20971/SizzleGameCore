namespace Sizzle.AbilitySystem
{
    public enum AbilityEndReason
    {
        None,
        Completed, // 어빌리티가 정상적으로 완료된 경우
        CancelByReactivation, // 재실행 정책이 Restart인 어빌리티가 같은 어빌리티에 의해 재실행된 경우
        Interrupted, // 외부 요인에 의해 어빌리티가 중단된 경우 (예: 캐릭터가 넉백 당함)
        Canceled, // 플레이어가 어빌리티를 취소한 경우 (예: 스킬 버튼을 다시 누름)
    }
}