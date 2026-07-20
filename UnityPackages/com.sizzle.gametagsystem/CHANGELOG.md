# Changelog

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
