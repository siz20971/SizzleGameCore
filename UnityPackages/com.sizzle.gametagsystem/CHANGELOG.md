# Changelog

## 0.1.7 - 2026-09-20
### 신규 기능
- **GameTagOptionAttribute 추가**:
  - `[GameTagOption(parent = "...")]`를 통해 인스펙터 드롭다운 선택 메뉴를 특정 부모의 하위 태그로만 제한.
  - 직접 텍스트 입력 시 하위 이름만 입력해도 자동으로 부모 접두사 완성 (`restrictToParent`).
  - 부모 태그 자체 허용 여부 옵션 (`includeParent`).
  - 텍스트 직접 입력을 제한하고 팝업 선택만 강제하는 드롭다운 전용 모드 (`dropdownOnly`).
  - 특정 태그 또는 하위 카테고리를 제외하는 필터 옵션 (`exclude`, `excludeTags`).
  - 메뉴 트리 가독성을 위해 부모 접두사를 생략한 상대 경로 표시 옵션 (`relativePathInMenu`).
- **GameTag 작성 시 문자 제한 및 정제**:
  - 태그 명칭에 영문 대소문자, 숫자, 하이픈(`-`), 언더스코어(`_`), 계층 구분자(`.`)만 허용하도록 유효성 검사(`IsValidTagName`) 및 자동 정제(`SanitizeTagName`) 추가.
  - 인스펙터 입력 시 잘못된 문자 자동 정제 및 ⚠️ 경고 피드백 표시.
- **TagContainer Notify 페이로드 전달 기능**:
  - `NotifyTag(GameTag tag, object payload = null)` 및 `NotifyTag<T>(GameTag tag, T payload)` 지원으로 자유로운 데이터를 함께 브로드캐스트 가능.
  - `OnTagNotifiedWithData` 이벤트 추가.
  - `IGameTagListener`에 C# 디폴트 인터페이스 구현(`OnGameTagNotified(GameTag, object)`)을 추가하여 기존 리스너와 100% 하위 호환성 유지.

## 0.1.6 - 2026-09-14
### 버그 수정
- `GameTagCacheAutoCollector`가 유니티 내부 어셈블리 리플렉션 중 예외가 발생할 경우 수집 로직이 중단되어 [GameTagPreset] 클래스의 태그가 누락되던 문제 수정.

## 0.1.5 - 2026-09-14

### 기능 (에디터 자동화)
- GameTagCache에 태그를 자동으로 수집하는 기능 추가
  - `[GameTagPreset]` 어트리뷰트를 사용하여 정적 클래스의 태그 자동 수집 지원.
  - `GameTagPreset` ScriptableObject 애셋을 통한 태그 관리 및 자동 수집 지원.
  - `GameTagCacheAutoCollector`를 통한 에디터 로드/컴파일/애셋 변경 시 실시간 동기화 구현.

## 0.1.4 - 2026-09-08

### 변경 사항 (에디터 편의성)
- GameTag 드롭다운 메뉴 항목이 알파벳순으로 정렬되도록 개선.
- 계층 구조(폴더형) 표시 설정을 단순 켜고 끄기(bool)에서 계층 깊이(int) 제어로 변경 (0: 평면, 1: 1단계 분리, -1: 무제한).

## 0.1.3 - 2026-09-06

### 변경 사항 (최적화)
- `GameTagContainer`에 `HasExactTagsAll(IList<GameTag>)` 및 `HasExactTagsAny(IList<GameTag>)` 오버로드를 추가하여 리스트 순회 시 배열 변환(`.ToArray()`)에 따른 런타임 GC 할당 방지.
- 에디터 `GameTagCache`의 검색 및 중복 검사 로직을 `HashSet<GameTag>` 기반으로 전환하여 $O(1)$ 성능을 확보하고, Dirty 플래그 기반으로 읽기 리스트를 재구성하도록 개선.

## 0.1.2 - 2026-07-20

### 변경 사항 (최적화)
- `GameTagContainer` 이벤트 리스너 호출 루프에서 발생하던 GC 할당 제거 (Dirty 플래그 기반 캐싱 적용).
- `HasExactTagsAll` 및 `HasExactTagsAny` 함수에서 LINQ 사용 시 발생하던 클로저 및 Enumerator 할당 제거.
- `AddTagTimed` 함수 호출 시 캡처되던 무명 메서드(클로저) 할당을 `TimedTagHandle` 구조체 직접 참조 방식으로 변경하여 제거.
- `AddTag` 및 `RemoveTag` 호출 시 발생하던 딕셔너리 이중 검색 횟수를 최소화하여 오버헤드 개선.
- `Tick` 루프 내 만료 태그 삭제 처리를 $O(1)$ 복잡도의 Swap-Back 기법으로 최적화.
- `GameTag` 내부 락(`lock`)을 `ConcurrentDictionary`로 교체하여 잡 시스템(JobSystem) 및 멀티스레드 환경의 병목 현상 제거.
- 동적으로 생성되는 문자열로 인한 잠재적 메모리 누수를 방지할 수 있도록 `GameTag.ClearCache` 유틸리티 메서드 추가.
- `GetTimedTags()` 호출 시 동적 리스트가 할당되는 문제를 해결하기 위해 Fill-List 패턴의 오버로딩 함수 추가.

## 0.1.1 - 2026-05-24

### 추가 사항
- 에디터 툴 및 디버깅 윈도우 지원을 위한 지속 시간 태그(Timed-tag) 스냅샷 접근자 추가.
- Ability Debugger와 같은 외부 툴에서 런타임 내부 상태를 건드리지 않고 남은 지속 시간을 읽어올 수 있도록 기능 활성화.
