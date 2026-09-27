# Sizzle.AbilitySystem - Action Sequence Samples

이 샘플 폴더(`Samples/ActionSequence`)는 `Sizzle.AbilitySystem`의 모듈형 액션 시퀀스(`ActionSequence`) 및 단위 액션(`AbilityAction`)을 활용하는 예제를 제공합니다.

---

## 📂 샘플 구성

- **`SampleBranchingSequenceAbility.cs`**:
  - 단일 어빌리티 내에서 여러 개의 `ActionSequence`(`m_initialSequence`, `m_successBranch`, `m_failureBranch`)를 정의하여 조건부로 분기 실행하는 예제 어빌리티입니다.
  - 인스펙터의 `[⚡ Edit Flow]` 버튼이나 플로우 에디터 창(`Window > Sizzle > Ability System > Action Sequence Flow`) 상단의 탭을 통해 각 분기 시퀀스를 전환하며 시각적으로 편집할 수 있습니다.

---

## 💡 핵심 사용법

### 1. 단일 시퀀스 어빌리티
어떤 어빌리티 클래스든(쿨다운, 사거리 스펙을 가진 프로젝트 고유 베이스 포함) `ActionSequence` 필드를 선언하고 실행할 수 있습니다:

```csharp
public class MyAbility : EntityAbilityBase<MyAbility.RuntimeContext>
{
    [SerializeField] private ActionSequence m_sequence = new ActionSequence();

    protected override void OnActivate(RuntimeContext context, AbilityActivatePayload payload)
    {
        var instance = m_sequence.CreateInstance(context);
        instance.OnCompleted += () => context.RequestComplete();
        instance.OnCanceled += () => context.RequestCancel();
        instance.Start();
    }
}
```

### 2. 조건부 다중 분기 시퀀스 (A/B Branching)
`SampleBranchingSequenceAbility.cs`를 참조하세요:

```csharp
[SerializeField] private ActionSequence m_initialSequence = new ActionSequence();
[SerializeField] private ActionSequence m_successBranch = new ActionSequence();
[SerializeField] private ActionSequence m_failureBranch = new ActionSequence();
```
* **인스펙터**: 각 시퀀스마다 `[⚡ Edit Flow]` 버튼이 제공되어 개별 편집 가능.
* **플로우 에디터 창**: 상단 툴바에 `Sequence: [ Initial Sequence | Success Branch | Failure Branch ]` 탭이 생성되어 탭 클릭만으로 시퀀스를 자유롭게 전환하며 편집 가능.
