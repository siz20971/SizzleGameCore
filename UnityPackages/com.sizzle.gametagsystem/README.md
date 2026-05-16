# Sizzle GameTag System

`Sizzle GameTag System`은 문자열 기반 계층 태그를 이용해 게임 상태를 표현하고 조회하는 경량 태그 시스템입니다.

예를 들어 다음과 같은 태그를 사용할 수 있습니다.

- `State.Stunned`
- `Skill.Attack`
- `Skill.Attack.Fireball`
- `Weapon.Ranged.Bow`

태그는 `.` 구분자를 기준으로 계층 구조를 가지며, 단순 문자열 비교뿐 아니라 exact, 하위 계층, 타이머 기반 태그 운용까지 지원합니다.

## 언제 사용하나

다음과 같은 상황에서 유용합니다.

- 캐릭터 상태 표현: `State.Stunned`, `State.Invincible`
- 행동/스킬 분류: `Skill.Attack`, `Skill.Support.Heal`
- 조건 검사: 특정 태그를 보유 중인지, 하위 태그를 포함하는지 확인
- 일시적 상태 부여: 짧은 시간 동안만 유지되는 태그 추가
- 이벤트 신호: 상태 변경 없이 `NotifyTag`로 특정 태그 이벤트 방송

## 활용 시나리오

### 상태 제어

플레이어가 `State.Stunned`를 보유 중이면 스킬 사용을 막고, `State.Invincible`를 보유 중이면 피격 처리만 우회하는 식으로 분기할 수 있습니다.

### 스킬 계열 판정

`Skill.Attack.Fireball`과 `Skill.Attack.Light`를 모두 `Skill.Attack` 계열로 취급하고 싶다면 exact 비교 대신 계층 비교를 사용하면 됩니다.

### 버프/디버프 지속 시간 관리

슬로우, 침묵, 무적처럼 일정 시간 뒤 사라져야 하는 상태는 `AddTagTimed`와 `Tick`으로 단순하게 관리할 수 있습니다.

### 이벤트 브로드캐스트

실제 보유 상태를 바꾸지 않고 `Event.Hit`, `Event.PerfectGuard` 같은 순간 이벤트만 발행하고 싶다면 `NotifyTag`를 사용합니다.

## 핵심 타입

### GameTag

`GameTag`는 태그 문자열 하나를 감싸는 값 타입입니다.

주요 역할:

- 태그 이름 보관
- exact 비교
- 문자열 부분 포함 비교
- 계층 비교
- 계층 분해 캐시 제공

예시:

```csharp
using Sizzle.GameTagSystem;

GameTag fireball = new GameTag("Skill.Attack.Fireball");
GameTag attack = new GameTag("Skill.Attack");

bool exact = fireball.IsExact(attack);            // false
bool child = fireball.ChildOf(attack);            // true
bool childOrExact = attack.ChildOfOrExact(attack); // true
bool strictChild = attack.StrictChildOf(attack);   // false
```

주의:

- `ChildOf`는 이름과 달리 exact match도 `true`로 취급합니다.
- `StrictChildOf`는 exact match를 제외한 엄격한 하위 계층만 `true`입니다.
- `IsContains`는 계층 비교가 아니라 문자열 `Contains` 비교입니다.

### GameTagContainer

`GameTagContainer`는 태그를 실제로 보유하고 조회하는 컨테이너입니다.

주요 역할:

- 태그 스택 추가/제거
- exact 보유 여부 조회
- 하위 태그 포함 여부 조회
- timed tag 관리
- 변경 이벤트와 notify 이벤트 발행

## 구조 한눈에 보기

```text
GameTag (값 타입)
    -> 태그 문자열 보관
    -> exact / contains / 계층 비교
    -> 계층 경로 캐시

GameTagContainer (상태 컨테이너)
    -> 태그 스택 추가/제거
    -> exact / descendant 조회
    -> timed tag 갱신
    -> 변경 이벤트 / notify 이벤트 발행
```

## 빠른 시작

### 1. 태그 생성

```csharp
GameTag attack = new GameTag("Skill.Attack");
GameTag fireball = new GameTag("Skill.Attack.Fireball");
```

### 2. 컨테이너 생성 및 태그 추가

```csharp
using Sizzle.GameTagSystem;

GameTagContainer container = new GameTagContainer();

container.AddTag(new GameTag("State.Combat"));
container.AddTag(new GameTag("Skill.Attack.Fireball"));
container.AddTag(new GameTag("Skill.Attack.Fireball"));
```

### 3. 태그 제거 및 스택 확인

```csharp
GameTag fireball = new GameTag("Skill.Attack.Fireball");

int stackBefore = container.GetTagStack(fireball);
container.RemoveTag(fireball);
int stackAfter = container.GetTagStack(fireball);
```

### 4. 태그 조회

```csharp
GameTag attack = new GameTag("Skill.Attack");
GameTag fireball = new GameTag("Skill.Attack.Fireball");

bool hasExactFireball = container.HasExactTag(fireball);
bool hasAttackOrDescendant = container.HasParentTag(attack);
bool hasStrictChildOfAttack = container.HasChildTag(attack);
```

샘플 씬과 UI에서는 아래 표현으로 표시됩니다.

- `Has Exact Tag`: 정확히 그 태그를 보유 중인가
- `Has Tag Or Descendant`: 그 태그 또는 하위 태그를 보유 중인가
- `Has Strict Child Tag`: 정확히 그 태그만이 아니라 하위 태그가 실제로 있는가

조회 함수 의미는 다음처럼 이해하면 됩니다.

| 함수 | 의미 |
| --- | --- |
| `HasExactTag(tag)` | 해당 태그를 정확히 보유 중인가 |
| `HasParentTag(tag)` | 해당 태그 또는 그 하위 태그를 하나라도 보유 중인가 |
| `HasChildTag(tag)` | 해당 태그의 엄격한 하위 태그를 하나라도 보유 중인가 |

예시:

- `Skill.Attack.Fireball`만 보유 중이면 `HasExactTag(Skill.Attack)`는 `false`
- `Skill.Attack.Fireball`만 보유 중이면 `HasParentTag(Skill.Attack)`는 `true`
- `Skill.Attack`만 보유 중이면 `HasChildTag(Skill.Attack)`는 `false`
- `Skill.Attack`와 `Skill.Attack.Fireball`을 함께 보유 중이면 `HasChildTag(Skill.Attack)`는 `true`

## Timed Tag 사용법

`AddTagTimed`는 일정 시간 후 자동으로 제거되는 태그를 추가합니다.

```csharp
GameTag slow = new GameTag("State.Debuff.Slow");

GameTagContainer.TimedTagHandle handle = container.AddTagTimed(slow, 3.0f);
```

이 기능을 쓰려면 매 프레임 `Tick(deltaTime)`을 호출해야 합니다.

```csharp
private GameTagContainer m_tagContainer = new GameTagContainer();

private void Update()
{
    m_tagContainer.Tick(Time.deltaTime);
}
```

필요하면 핸들로 조기 취소할 수 있습니다.

```csharp
if (handle.IsValid)
    handle.Cancel();
```

흐름은 아래처럼 이해하면 됩니다.

1. `AddTagTimed`가 즉시 태그를 1스택 추가합니다.
2. 내부 timed entry가 duration을 보관합니다.
3. 매 프레임 `Tick(deltaTime)`이 남은 시간을 감소시킵니다.
4. 시간이 끝나면 자동으로 `RemoveTag`가 호출됩니다.

## 이벤트 및 리스너

`GameTagContainer`는 두 종류의 이벤트를 제공합니다.

- `OnTagOwnshipChanged`: 태그 보유 수량이 변경될 때 호출
- `OnTagNotified`: `NotifyTag` 호출 시 전달되는 단발성 이벤트

### C# 이벤트 구독

```csharp
private GameTagContainer m_tagContainer;

private void OnEnable()
{
    m_tagContainer.OnTagOwnshipChanged += HandleTagChanged;
    m_tagContainer.OnTagNotified += HandleTagNotified;
}

private void OnDisable()
{
    m_tagContainer.OnTagOwnshipChanged -= HandleTagChanged;
    m_tagContainer.OnTagNotified -= HandleTagNotified;
}

private void HandleTagChanged(GameTagContainer.GameTagOwnshipChangeInfo info)
{
    Debug.Log($"Tag Changed: {info.Tag.TagName}, Added: {info.Added}, Remains: {info.Remains}");
}

private void HandleTagNotified(GameTag gameTag)
{
    Debug.Log($"Tag Notified: {gameTag.TagName}");
}
```

### IGameTagListener 사용

```csharp
using Sizzle.GameTagSystem;

public class TagListenerExample : MonoBehaviour, IGameTagListener
{
    private readonly GameTagContainer m_container = new GameTagContainer();

    private void OnEnable()
    {
        m_container.AddListener(this);
    }

    private void OnDisable()
    {
        m_container.RemoveListener(this);
    }

    public void OnGameTagOwnshipChanged(GameTagContainer.GameTagOwnshipChangeInfo info)
    {
        Debug.Log($"Changed: {info.Tag.TagName}");
    }

    public void OnGameTagNotified(GameTag gameTag)
    {
        Debug.Log($"Notified: {gameTag.TagName}");
    }
}
```

## NotifyTag는 언제 쓰나

`NotifyTag`는 컨테이너의 보유 상태를 바꾸지 않고, 특정 태그 이벤트만 발행하고 싶을 때 사용합니다.

예시:

- `Event.Hit`
- `Event.PerfectDodge`
- `Skill.Cast.Start`

```csharp
container.NotifyTag(new GameTag("Event.Hit"));
```

`NotifyTag`는 상태 저장이 아니라 신호 전달용입니다. 따라서 `NotifyTag("Event.Hit")`를 호출해도 `HasExactTag("Event.Hit")`가 true가 되지는 않습니다.

## Inspector에서 GameTag 사용

`GameTag`는 직렬화 가능한 struct이므로 `SerializeField`로 인스펙터에 노출할 수 있습니다.

```csharp
[SerializeField] private GameTag m_mainTag;
```

패키지에는 `GameTagPropertyDrawer`가 포함되어 있어 인스펙터에서 태그 문자열을 직접 입력하거나 캐시된 태그 목록을 선택할 수 있습니다.

다만 주의할 점이 있습니다.

- `GameTagPropertyDrawer`의 태그 선택 메뉴는 `GameTagCache`가 채워져 있어야 정상 동작합니다.
- `com.sizzle.gametagsystem` 패키지 자체는 캐시를 자동으로 채우지 않습니다.
- 현재 워크스페이스에서는 `com.sizzle.abilitysystem`의 editor 유틸리티가 캐시를 채우는 방식으로 연결되어 있습니다.

즉, 이 패키지만 단독 사용한다면 프로젝트의 editor 초기화 코드에서 직접 캐시를 채워야 합니다.

예시:

```csharp
using Sizzle.GameTagSystem;
using Sizzle.GameTagSystem.Editor;
using UnityEditor;

public static class GameTagEditorBootstrap
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        GameTagCache.ClearCache();
        GameTagCache.Add(new GameTag("State.Stunned"));
        GameTagCache.Add(new GameTag("Skill.Attack"));
        GameTagCache.Add(new GameTag("Skill.Attack.Fireball"));
    }
}
```

## 샘플

패키지에는 동작을 바로 확인할 수 있는 샘플 씬이 포함되어 있습니다.

- 샘플 경로: `Samples/DemoScene`
- 샘플 스크립트: `Samples/GameTagContainerSceneTester.cs`

샘플에서는 다음을 확인할 수 있습니다.

- 태그 추가/제거
- timed tag 추가
- exact, parent, child 계열 조회 결과
- 로그 출력
- 현재 보유 태그와 스택 표시

샘플의 조회 텍스트는 아래 의미를 기준으로 읽으면 됩니다.

- `Has Exact Tag`: exact match만 검사
- `Has Tag Or Descendant`: exact 또는 descendant 존재 여부 검사
- `Has Strict Child Tag`: descendant만 검사

## 권장 사용 패턴

- 태그 이름은 `Root.Category.Item`처럼 일관된 계층 규칙으로 관리합니다.
- exact 비교와 계층 비교를 혼용할 때는 함수 의미를 명확히 구분해서 사용합니다.
- timed tag를 쓰는 컨테이너는 반드시 `Tick`을 호출합니다.
- 반복적으로 쓰는 태그는 코드 상수나 static readonly 필드로 모아두는 편이 안전합니다.
- `HasParentTag`와 `ChildOf`는 이름보다 실제 동작 의미를 기준으로 사용합니다.

## 주의할 점

- `ChildOf`는 이름과 달리 exact match도 `true`입니다.
- `HasParentTag`는 부모 태그를 찾는 함수라기보다 exact 또는 descendant 존재 여부를 묻는 함수입니다.
- `HasChildTag`는 exact match만 있는 경우 `false`이며, 엄격한 하위 태그가 있을 때만 `true`입니다.
- timed tag를 사용하면서 `Tick`을 호출하지 않으면 자동 만료되지 않습니다.

## 요약

이 패키지는 다음 두 축으로 이해하면 됩니다.

- `GameTag`: 태그 하나를 표현하고 비교하는 타입
- `GameTagContainer`: 태그를 보유하고 조회하며 이벤트를 발행하는 타입

작고 단순한 구조이지만, 상태 시스템, 스킬 분류, 조건 검사, 일시적 효과 처리에 폭넓게 사용할 수 있습니다.