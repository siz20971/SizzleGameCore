# Sizzle Ability System

`Sizzle Ability System`은 `ScriptableObject` 기반 어빌리티를 `AbilityProcessor`에 등록하고, `GameTag`를 이용해 실행 조건과 상호작용을 제어하는 Unity 패키지입니다.

이 패키지는 다음 문제를 풀기 위해 만들어졌습니다.

- 스킬/어빌리티를 데이터 애셋으로 분리하고 싶을 때
- 캐릭터 상태와 태그 조건으로 발동 가능 여부를 제어하고 싶을 때
- 같은 계열 스킬끼리 차단, 취소, 트리거 관계를 만들고 싶을 때
- 재실행 정책을 통일된 방식으로 다루고 싶을 때
- 실행 중인 어빌리티와 태그 상태를 에디터에서 확인하고 싶을 때

이 패키지는 [com.sizzle.gametagsystem](../com.sizzle.gametagsystem/README.md) 에 의존합니다.

## 핵심 개념

### Ability

`Ability`는 실행 가능한 어빌리티 애셋의 베이스 클래스입니다.

주요 역할:

- 실행 로직 정의
- 실행 가능 여부 검사
- activate / reactivate / deactivate 라이프사이클 처리
- `AbilityGameTagSet`을 통한 태그 규칙 정의

커스텀 어빌리티는 보통 아래 둘 중 하나를 상속해서 만듭니다.

- `Ability<TContext>`
- `Ability<TContext, TActivatePayload>`

### AbilityRuntimeContext

어빌리티의 수명주기 정보와 개별 런타임 데이터(State/Cache)를 보관하는 클래스입니다. 
최신 구조에서는 **"자동으로 초기화되는 값(State)"**과 **"계속 유지되는 참조(Cache)"**를 명확히 분리하면서도 상속 깊이를 최소화했습니다.

핵심 구조:

- `AbilityRuntimeContext`: 공통 시스템 상태 (`IsActive`, `ElapsedTime`, `GameObject`, `AbilityProcessor` 등)
- `AbilityRuntimeContext<TState>`: 매 실행마다 자동 초기화(Reset)되는 커스텀 `State` 구조체를 포함하는 제네릭 컨텍스트

권장 규칙:

- **State (상태)**: 어빌리티가 활성화될 때마다 초기화되어야 하는 변수는 `TState` **구조체(struct)**에 정의합니다. 프레임워크가 매 실행 시 `default`를 할당하여 자동으로 모든 변수를 0이나 null로 완벽하게 초기화해 줍니다.
- **Cache (캐시)**: 매번 리셋될 필요 없이 수명주기 내내 유지되어야 하는 컴포넌트/오브젝트 참조는 `Context` 클래스의 **일반 멤버 변수**로 선언합니다.
- (시스템 변수 접근): `context.IsActive`, `context.ElapsedTime`, `context.GameObject` 등 시스템 프로퍼티는 래핑 없이 `Context` 본체에서 직접 접근할 수 있습니다.

### AbilityProcessor

`AbilityProcessor`는 실제 런타임 실행을 담당하는 컴포넌트입니다.

주요 역할:

- 기본 어빌리티 등록
- 메인 태그로 어빌리티 실행
- 트리거 태그 수신 시 자동 실행
- 활성 어빌리티 업데이트 루프 관리
- `GameTagContainer`를 통한 상태 태그 보유 및 조회
- 차단/필수/취소 태그 규칙 처리

### AbilityGameTagSet

`AbilityGameTagSet`은 어빌리티의 태그 규칙을 묶는 데이터입니다.

구성 요소:

- `MainTag`: 이 어빌리티의 대표 태그
- `CancelAbilitiesWithTag`: 실행 시 취소할 어빌리티 계열
- `BlockAbilitiesWithTag`: 실행 중 차단할 어빌리티 계열
- `ActivationOwnedTags`: 실행 중 소유할 태그
- `ActivationRequiredTags`: 실행을 위해 모두 필요로 하는 태그
- `ActivationBlockedTags`: 하나라도 있으면 실행을 막는 태그
- `TriggerTag`: `NotifyTag`로 들어오면 실행되는 트리거 태그

### ActionSequence & AbilityAction (모듈형 액션 시퀀스)

`ActionSequence`는 어빌리티 내부에서 순차적으로 실행되는 단위 액션(`AbilityAction`)들의 흐름을 캡슐화한 경량 실행 엔진입니다.

특정 어빌리티 클래스를 강제로 상속할 필요 없이, 기존 프로젝트 고유의 어빌리티 베이스 클래스(쿨다운, 마나, 사거리 등)에 `[SerializeField] private ActionSequence m_sequence = new ActionSequence();` 형태로 필드로 합성(Composition)하여 자유롭게 사용할 수 있습니다.

- `AbilityAction`: 단위 액션 추상 베이스 클래스 (`OnStart`, `OnUpdate`, `OnCleanup`, `Delay`, `Clone()`)
- `ActionSequence`: 단위 액션 목록 및 실행 상태를 격리 관리하는 실행 엔진 (`CreateInstance(context)`)
- `ActionCompositionAbility`: 코드 작성 없이 인스펙터 및 플로우 에디터만으로 시퀀스를 구성하는 기본 비제네릭 어빌리티
- `ActionSequenceAsset`: 여러 어빌리티에서 공용으로 재사용할 수 있는 독립 ScriptableObject 애셋
- `NamedActionSequence`: 다중 분기 시퀀스를 이름과 함께 다루는 데이터 구조

## 빠른 시작

### 1. AbilityProcessor 배치

씬의 캐릭터 또는 시스템 오브젝트에 `AbilityProcessor`를 붙입니다.

```csharp
using Sizzle.AbilitySystem;
using UnityEngine;

public class PlayerAbilityBootstrap : MonoBehaviour
{
    [SerializeField] private AbilityProcessor m_processor;

    private void Start()
    {
        m_processor.Initialize();
    }
}
```

중요:

- `AbilityProcessor`는 `Awake`에서 기본 어빌리티를 자동 등록하지 않습니다.
- `m_defaultAbilities`를 사용하려면 `Initialize()`를 직접 호출해야 합니다.

### 2. 커스텀 Ability 만들기

```csharp
using Sizzle.AbilitySystem;
using UnityEngine;

// 1. 매번 리셋이 필요한 변수들은 State 구조체에 모아둡니다.
public struct DashState
{
    public Vector3 Direction;
}

// 2. 캐시할 참조 변수는 Context의 일반 멤버로 둡니다.
public class DashContext : AbilityRuntimeContext<DashState>
{
    public Transform Transform;
}

[CreateAbilityAssetMenu("Sizzle/Abilities/Dash")]
public class DashAbility : Ability<DashContext>
{
    protected override bool CanActivate(DashContext context, AbilityActivatePayload payload)
    {
        context.Transform ??= context.GameObject.transform;
        return true;
    }

    protected override void OnActivate(DashContext context, AbilityActivatePayload payload)
    {
        context.State.Direction = context.Transform.forward;
        Debug.Log("Dash activated");
    }

    protected override void OnUpdateTick(float deltaTime, DashContext context)
    {
        if (context.ElapsedTime >= 0.2f)
            context.RequestComplete();
    }

    protected override void OnDeactivate(AbilityEndReason endReason, DashContext context)
    {
        Debug.Log($"Dash ended: {endReason}");
    }
}
```

### 3. Ability 등록 및 실행

기본 등록 리스트를 쓰는 경우:

- `AbilityProcessor` 인스펙터의 `Default Abilities`에 애셋을 넣습니다.
- 런타임에 `Initialize()`를 호출합니다.

직접 등록하는 경우:

```csharp
using Sizzle.AbilitySystem;
using Sizzle.GameTagSystem;
using UnityEngine;

public class AbilityRegisterExample : MonoBehaviour
{
    [SerializeField] private AbilityProcessor m_processor;
    [SerializeField] private Ability m_dashAbility;

    private void Start()
    {
        m_processor.RegistAbility(m_dashAbility, null);
        m_processor.TryActivateAbility(new GameTag("Skill.Movement.Dash"));
    }
}
```

## 실행 흐름

어빌리티 실행은 대략 아래 순서로 진행됩니다.

1. `TryActivateAbility(mainTag)` 호출
2. 해당 `MainTag`의 컨텍스트 검색
3. 이미 활성 상태인지 검사
4. `ReactivationPolicy`에 따라 재실행 처리 결정
5. `CanActivate` 검사
6. `ActivationRequiredTags` / `ActivationBlockedTags` 검사
7. 다른 활성 어빌리티의 `BlockAbilitiesWithTag` 검사
8. 실행 성공 시 컨텍스트 활성화
9. `ActivationOwnedTags`를 `TagContainer`에 추가
10. `CancelAbilitiesWithTag` 규칙에 맞는 활성 어빌리티 취소 요청

추가 보장:

- 종료 요청된 어빌리티는 cleanup 전까지 추가 tick 을 더 수행하지 않습니다.
- `MainTag` 또는 `TriggerTag`가 중복된 어빌리티는 등록이 거부됩니다.
- 해제 시 `OnAbilityUnregistered` 이벤트가 실제로 발행됩니다.

## 태그 규칙 이해하기

### MainTag

`MainTag`는 어빌리티를 식별하는 대표 태그입니다.

예시:

- `Skill.Attack.Light`
- `Skill.Attack.Heavy`
- `Skill.Support.Heal`

### Required / Blocked

```text
ActivationRequiredTags: 모두 있어야 실행 가능
ActivationBlockedTags: 하나라도 있으면 실행 불가
```

예시:

- `State.CanAct`가 있어야 실행 가능
- `State.Stunned`가 있으면 실행 불가

### Cancel / Block

```text
CancelAbilitiesWithTag: 이 어빌리티가 켜질 때 다른 활성 어빌리티를 취소
BlockAbilitiesWithTag: 이 어빌리티가 켜져 있는 동안 다른 어빌리티 실행을 차단
```

예를 들어 `Skill.Attack` 계열이 이미 실행 중일 때 다른 `Skill.Attack.*` 계열 실행을 막거나, 상위 계열 진입 시 기존 하위 계열을 취소하는 식으로 사용할 수 있습니다.

### ActivationOwnedTags

어빌리티가 실행 중일 때 소유자가 갖게 되는 태그입니다.

예시:

- `State.Attacking`
- `State.Casting`
- `State.MovementLocked`

이 태그들은 다른 어빌리티의 Required / Blocked 규칙과 연결되는 핵심 수단입니다.

### TriggerTag

`TriggerTag`는 `AbilityProcessor.TagContainer.NotifyTag(...)` 호출을 통해 실행되는 태그입니다.

예시:

```csharp
processor.TagContainer.NotifyTag(new GameTag("Event.CounterWindow"));
```

이 경우 해당 `TriggerTag`를 가진 어빌리티가 자동으로 실행됩니다.

## ReactivationPolicy

재실행 정책은 같은 어빌리티가 이미 활성 상태일 때 다시 실행 요청이 들어오면 어떻게 처리할지 정의합니다.

### Deny

이미 활성 상태면 재실행을 거부합니다.

### RestartFromBeginning

기존 실행을 종료한 뒤 처음부터 다시 실행합니다.

### Reactivate

기존 활성 상태를 유지한 채 `OnReactivate`를 호출합니다.

다음 같은 경우에 적합합니다.

- 연타 콤보
- 차지 갱신
- 입력 버퍼 소비

`CanActivate`가 실패했을 때도 별도 처리하고 싶다면 `OnReactivateBlocked`를 override하면 됩니다.

## Payload 사용

추가 인자가 필요한 어빌리티는 `AbilityActivatePayload`를 상속한 payload 타입을 정의하고 `Ability<TContext, TPayload>`를 사용하면 됩니다.

```csharp
using Sizzle.AbilitySystem;
using UnityEngine;

public class AimPayload : AbilityActivatePayload
{
    public Vector3 TargetPoint;
}

public class AimContext : AbilityRuntimeContext
{
}

public class AimAbility : Ability<AimContext, AimPayload>
{
    protected override void OnActivate(AimContext context, AimPayload payload)
    {
        Debug.Log($"Aim target: {payload.TargetPoint}");
    }

    protected override void OnDeactivate(AbilityEndReason endReason, AimContext context)
    {
    }
}
```

## 모듈형 액션 시퀀스 (ActionSequence & AbilityAction)

`ActionSequence`는 어빌리티를 단계별 실행 단위(Action)들의 흐름으로 조립할 수 있게 해주는 경량 모듈입니다.

기존 어빌리티 시스템의 상속 계층(`EntityAbilityBase`, `PlayerAbility`, `BossAbility` 등)을 깨뜨리지 않고, 원하는 어빌리티 클래스 안에 **`[SerializeField] private ActionSequence m_sequence = new ActionSequence();`** 필드만 추가하면 즉시 사용할 수 있습니다.

### 1. 단일 시퀀스 활용 (합성 패턴)

프로젝트 고유의 쿨다운/마나/사거리 등의 스펙을 가진 어빌리티 클래스에서 시퀀스를 합성하여 구동하는 기본 패턴입니다.

```csharp
using Sizzle.AbilitySystem;
using Sizzle.AbilitySystem.Actions;
using UnityEngine;

[CreateAbilityAssetMenu("Combat/Slash Attack")]
public class SlashAttackAbility : Ability<SlashAttackAbility.RuntimeContext>
{
    public class RuntimeContext : AbilityRuntimeContext
    {
        public ActionSequence.Instance ActiveSequence;

        protected override void OnReset()
        {
            base.OnReset();
            ActiveSequence = null;
        }
    }

    [Header("Sequence")]
    [SerializeField] private ActionSequence m_sequence = new ActionSequence();

    protected override void OnActivate(RuntimeContext context, AbilityActivatePayload payload)
    {
        // 시퀀스 실행 인스턴스 생성 (컨텍스트 바인딩)
        context.ActiveSequence = m_sequence.CreateInstance(context);
        
        // 수명주기 이벤트 연결
        context.ActiveSequence.OnCompleted += () => context.RequestComplete();
        context.ActiveSequence.OnCanceled += () => context.RequestCancel();
        
        // 시퀀스 시작
        context.ActiveSequence.Start();
    }

    protected override void OnUpdateTick(float deltaTime, RuntimeContext context)
    {
        context.ActiveSequence?.Update(deltaTime);
    }

    protected override void OnDeactivate(AbilityEndReason endReason, RuntimeContext context)
    {
        context.ActiveSequence?.Cancel();
        context.ActiveSequence = null;
    }
}
```

### 2. 커스텀 AbilityAction 구현

단위 액션은 `AbilityAction`을 상속받아 필요한 로직을 오버라이드합니다.

```csharp
using System;
using Sizzle.AbilitySystem;
using Sizzle.AbilitySystem.Actions;
using UnityEngine;

[Serializable]
public class PlayAnimationAction : AbilityAction
{
    [SerializeField] private string m_animStateName = "Attack";
    [SerializeField] private float m_duration = 0.5f;

    // 액션이 시작될 때 호출 (지연 대기 Delay 이후)
    protected override void OnStart(AbilityRuntimeContext context)
    {
        var animator = GameObject?.GetComponentInChildren<Animator>();
        animator?.Play(m_animStateName);

        if (m_duration <= 0f)
            Complete();
    }

    // 액션이 실행 중일 때 매 프레임 호출
    protected override void OnUpdate(float deltaTime)
    {
        if (ElapsedTime >= m_duration)
            Complete();
    }

    // 액션이 정상 완료되거나 중도 취소될 때 항상 호출
    protected override void OnCleanup()
    {
    }
}
```

- **`Delay`**: 액션이 큐에서 차례가 되었을 때, 실제 `OnStart`가 호출되기 전 대기하는 시간(초).
- **`Complete()`**: 액션이 완료되었음을 알려 다음 액션으로 진행시킵니다.
- **`Cancel()`**: 액션을 취소하고 시퀀스 전체를 중단합니다.
- **`Clone()`**: 리플렉션 및 JSON 직렬화 기반 딥클론 기능이 내장되어 있어, 에디터 및 런타임 복제 시 `[SerializeReference]`로 인한 객체 참조 공유 문제가 발생하지 않습니다.

### 3. 기본 제공 내장 액션 (Built-in Actions)

- `WaitDelayAction`: 설정된 시간(`Duration`) 동안 대기하는 범용 지연 액션.
- `DebugLogAction`: 설정된 메시지와 로그 레벨(Info, Warning, Error)을 콘솔에 출력하는 액션.
- `TimedGameTagAction`: 실행 시 `GameTagContainer`에 일정 시간(`Duration`) 유지되는 태그를 추가(`AddTagTimed`)하는 액션.
- `NotifyGameTagAction`: 실행 시 `GameTagContainer`를 통해 태그 알림(`NotifyTag`)을 발행하는 액션.
- `GameTagAction`: 실행 시 `GameTagContainer`에 태그를 추가(`Add`)하거나 제거(`Remove`)하는 선택형 액션.

### 4. 조건부 다중 분기 시퀀스 (Branching Sequences)

선행 시퀀스 실행 후 조건(예: 타격 성공 여부, 카운터 여부 등)에 따라 서로 다른 후속 시퀀스를 실행할 수 있습니다.

```csharp
[SerializeField] private ActionSequence m_initialSequence = new ActionSequence();
[SerializeField] private ActionSequence m_successBranch = new ActionSequence();
[SerializeField] private ActionSequence m_failureBranch = new ActionSequence();
```

- 인스펙터에서 각 시퀀스마다 `[⚡ Edit Flow]` 버튼이 제공됩니다.
- `ActionSequenceFlowWindow` 에디터 창 상단에 분기 시퀀스 목록 탭이 자동 생성되어 손쉽게 전환하며 편집할 수 있습니다.
- 자세한 구현은 `Samples/ActionSequence/SampleBranchingSequenceAbility.cs`를 참조하세요.

### 5. ActionCompositionAbility & ActionSequenceAsset

- **`ActionCompositionAbility`**: C# 스크립트 작성 없이 인스펙터 및 플로우 에디터만으로 순수 데이터 시퀀스를 구성해 등록/발동할 수 있는 기본 제공 어빌리티입니다.
- **`ActionSequenceAsset`**: 시퀀스 데이터를 독립된 `ScriptableObject` 애셋으로 저장하여 여러 어빌리티나 시스템에서 재사용할 수 있습니다.

## 디버깅과 에디터 툴

패키지는 아래 에디터 도구를 제공합니다.

### Action Sequence Flow Window

메뉴:

- `Sizzle/Ability System/Action Sequence Flow Editor`
- `Tools/Sizzle/AbilitySystem/Action Sequence Flow Editor`
- `Window/Ability System/Action Sequence Flow Editor`
- 또는 인스펙터의 `ActionSequence` 필드에 있는 `[⚡ Edit Flow]` 버튼 클릭

기능:

- 가로 타임라인/노드 플로우 스타일의 시각적 액션 편집
- 단일 및 다중 분기 시퀀스(`InitialSequence`, `SuccessBranch` 등) 자동 감지 및 상단 탭 전환
- 툴바 슬라이더를 통한 액션 편집 카드 너비(Width) 실시간 조절
- 각 액션 카드 상단에서 구현 C# 스크립트 파일 바로 열기(`[Open Script]`)
- SerializeReference 깊은 복사(Deep Clone)를 지원하는 복제(`Duplicate`) 기능
- 프로젝트 내 정의된 `AbilityAction` 타입을 검색하여 즉시 추가하는 드롭다운 팝업

### Ability Debugger

메뉴:

- `Tools/Sizzle/AbilitySystem/Ability Debugger`

기능:

- 씬의 `AbilityProcessor` 목록 표시
- 등록된 어빌리티와 활성 상태 확인
- 활성화 결과, 차단 원인, 최근 이벤트 히스토리 확인
- 태그 Add / Remove / Notify / Timed Add
- 메인 태그로 직접 Activate 실행
- 현재 소유 태그와 스택 확인

### Ability Tag Viewer

메뉴:

- `Tools/Sizzle/AbilitySystem/Ability Tag Viewer`

기능:

- 프로젝트의 Ability 태그 트리 시각화
- 텍스트 export
- 태그 목록 새로고침

### AbilityTags 코드 생성기

메뉴:

- `Tools/Sizzle/AbilitySystem/Generate AbilityTags Define Script`

기능:

- 프로젝트의 Ability 애셋을 스캔
- 사용 중인 태그를 모아 `AbilityTags_Generated.cs` 생성
- 커스텀 태그는 `Assets/Scripts/AbilityTags.cs`에 분리 유지
- 생성 후 `GameTagCache`를 갱신해 인스펙터 선택 목록과 연동

## GameTagSystem 연동

이 패키지는 내부적으로 `AbilityProcessor.TagContainer`를 통해 `GameTagSystem`을 사용합니다.

즉 다음이 가능합니다.

- 상태 태그 직접 추가/제거
- notify 기반 이벤트 트리거
- timed tag 사용
- 조건 태그 조회

예시:

```csharp
using Sizzle.GameTagSystem;

processor.TagContainer.AddTag(new GameTag("State.CanAct"));
processor.TagContainer.AddTagTimed(new GameTag("State.Invincible"), 1.5f);
processor.TagContainer.NotifyTag(new GameTag("Event.Parry"));
```

## Create 메뉴 확장

`CreateAbilityAssetMenuAttribute`가 붙은 `ScriptableObject` 타입은 에디터에서 동적으로 생성 메뉴에 추가됩니다.

현재 메뉴 경로는 아래와 같습니다.

- `Assets/Create/Platformer2D/Character/Ability/...`

예시:

```csharp
using Sizzle.AbilitySystem;

[CreateAbilityAssetMenu("Dash")]
public class DashAbility : Ability<DashContext>
{
    protected override void OnActivate(DashContext context, AbilityActivatePayload payload) { }
    protected override void OnDeactivate(AbilityEndReason endReason, DashContext context) { }
}
```

## 샘플

패키지에는 아래의 샘플들이 포함되어 있습니다.

### 1. Demo Scene

- 샘플 경로: `Samples/DemoScene`
- 샘플 내용:
  - `AbilityProcessor`를 통한 어빌리티 실행
  - 태그 기반 실행 조건
  - `NotifyTag` 기반 트리거 실행
  - 에디터 디버거와 태그 가시화 도구 사용

### 2. Action Sequence Samples

- 샘플 경로: `Samples/ActionSequence`
- 샘플 내용:
  - `SampleBranchingSequenceAbility`: 선행 시퀀스 실행 후 조건에 따라 분기(Success/Failure)하는 다중 시퀀스 어빌리티 예제
  - `ActionSequenceFlowWindow`를 통한 시각적 플로우 편집 및 탭 전환 확인

## 주의할 점

- `AbilityProcessor.Initialize()`를 호출하지 않으면 `Default Abilities`가 등록되지 않습니다.
- `MainTag`와 `TriggerTag`는 중복 등록 시 등록 자체가 거부됩니다.
- `ActivationRequiredTags`와 `ActivationBlockedTags`는 현재 exact 태그 기준으로 검사됩니다.
- `CancelAbilitiesWithTag`와 `BlockAbilitiesWithTag`는 계층 태그 비교에 의존합니다.
- `AbilityRuntimeContext<TState>`의 **`TState`는 반드시 필드(Field)로 선언**해야 하며, 프로퍼티(Property)로 선언 시 C# 구조체 특성상 복사본 수정 에러가 발생합니다. 프레임워크가 제공하는 기본 선언 구조를 그대로 따르세요.

## 요약

이 패키지는 다음 세 축으로 이해하면 됩니다.

- `Ability`: ScriptableObject로 정의하는 실행 로직
- `AbilityRuntimeContext`: 공통 실행 상태와 개별 구조체(State) 및 멤버 변수(Cache)를 보관하는 런타임 컨텍스트
- `ActionSequence` & `AbilityAction`: 어빌리티 로직을 단계별 단위 액션으로 분할하고 조립할 수 있는 모듈형 시퀀스 엔진
- `AbilityProcessor`: 태그 조건, 실행 상태, 갱신 루프를 관리하는 런타임 실행기

`GameTagSystem`과 함께 사용하면 상태 기반 실행 제어, 계층 스킬 분기, 트리거 이벤트, 재실행 정책 및 단계별 액션 시퀀스를 단순하고 유연한 구조로 다룰 수 있습니다.