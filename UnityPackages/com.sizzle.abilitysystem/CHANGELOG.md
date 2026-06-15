# Changelog

## 0.1.4 - 2026-06-15
- 기존 `AbilityRuntimeContext`의 `State / Cache` 구조를 `AbilityRuntimeContext<TState>` 로 간소화하고 초기화 대상을 TState로 한정했습니다.
- `Ability` 스크립트를 생성하는 툴과 상속 대상 클래스를 지정하기 위한 `AbilityTemplate` Attribute를 추가했습니다.

## 0.1.3 - 2026-05-31

- `Unity Behavior` 어빌리티 실행/취소/전체 취소 및 게임 태그 조작용 Unity Behavior 액션과 조건 노드를 추가했습니다.
- `UniTask` 어빌리티 실행 후 종료 대기, 활성/비활성 대기, 태그 보유/알림 대기용 비동기 확장 메서드를 제공했습니다.
- `Timeline` 어빌리티 실행/취소 및 태그 Add/Remove/Notify/Timed Add를 수행하는 커맨드 트랙을 제공했습니다.
- `AbilityProcessor`에 문자열 기반 실행/조회 오버로드와 태그 기반 취소 API를 추가했습니다.
- `AbilityProcessorResolver`를 추가해 `AbilityProcessor` 또는 `IAbilitySystemComponent`를 일관되게 해석하도록 정리했습니다.
- 코어 런타임 asmdef에서 optional package `versionDefines`를 제거하고, 선택 기능 의존성을 전용 asmdef로 분리했습니다.
- 어빌리티 종료 혹은 취소 요청 후 LateUpdate에서 어빌리티 컨텍스트가 정리되기전에 활성화 요청을 할 경우, 어빌리티가 의도치않게 시작하던 버그를 수정했습니다.

## 0.1.2 - 2026-05-27

- `AbilityRuntimeContext`에 `State / Cache` 분리 구조와 `AbilityRuntimeContext<TState, TCache>` 제네릭 베이스를 추가했습니다.
- 기존 `GameObject`, `Ability`, `Processor`, `IsActive`, `ElapsedTime`, `ActivatedTime`, `PendingEndReason` 프로퍼티는 호환용 `Obsolete` 포워더로 유지했습니다.
- `AbilityProcessor`가 종료 시 등록된 모든 컨텍스트를 정리하도록 보완했습니다.
- 종료 요청된 어빌리티가 cleanup 전 추가 tick 을 수행하지 않도록 수정했습니다.
- 중복 `MainTag` 또는 `TriggerTag`를 가진 어빌리티 등록을 거부하도록 변경했습니다.
- 어빌리티 해제 시 `OnAbilityUnregistered` 이벤트가 실제로 발행되도록 보완했습니다.
- README를 최신 구조와 사용 방식에 맞게 갱신했습니다.

## 0.1.1 - 2026-05-24

- Ability Debugger에 활성화 결과 상세, 활성 어빌리티 상태 보기, 차단 원인 분석, 플레이 모드 제어 기능을 확장했습니다.
- 프로세서 목록에 `Pick`, `Ping` 바로가기를 추가하고 디버거와 히스토리 화면을 상단 탭으로 분리했습니다.
- 히스토리 필터, 카테고리 토글, 색상 행, 접기 옵션을 추가해 기록 탐색을 개선했습니다.
- 캐시된 태그 선택, timed tag 표시, 최근 이벤트 추적을 추가해 플레이 모드 디버깅을 빠르게 했습니다.
