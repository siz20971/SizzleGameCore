# Changelog

## 0.2.0 - 2026-11-05
### 신규 기능 및 개선 (Data 모듈)
- **CSVParser**: RFC 4180 표준 규격(따옴표 이스케이프, 셀 내 개행, BOM 처리)을 준수하는 경량·고성능 CSV 파서 신규 구현.
- **CSVColumnAttribute**: `[CSVColumn]` 어트리뷰트를 통해 데이터 모델의 public/private 필드 및 private setter 프로퍼티 자동 매핑 지원.
- **CSVParseConfig**: 전역 상태 없이 파서 옵션(구분자, 주석 스킵 등)과 커스텀 타입 컨버터(GameTag 등)를 인스턴스 단위로 주입하는 설정 아키텍처 도입.
- **CSVParseResult<TData>**: 파싱 성공/실패 수, `CSVParseError` 상세 진단 리포트 및 `ToDictionary()`, `Find()`, `FindAll()` 등 편리한 쿼리/열거 기능 제공.
- **DataTable 연동**: `DataTable<TKey, TData>.Load(CSVParseResult<TData>)` 직결 로드 지원.
- **단위 테스트**: NUnit 기반 Unity Test Runner 테스트 스위트 구축 (`Tests/Editor/TableParserTests.cs`).
- **레거시 정리**: `CSVTableParser` 제거 및 `CSVParser` 중심으로 API 단일화.

## 0.1.0 - 2026-10-07
### 신규 패키지 배포
- `ProjectParry`에서 `Sizzle.Toolkits`를 독립 UPM 패키지(`com.sizzle.toolkits`)로 분리 및 이관.
- **주요 모듈 포함**:
  - `UI`: `RecycledScrollView` (가상화 스크롤뷰, 대각선/다열 레이아웃, 센터링 스냅 지원), `AnimatedNumberCounter`, `CanvasGroupFader`, `DualFollowSlider`, `LongPressButton`, `TypewriterText`, `VirtualJoystick`, `WorldSpaceUIFollower` 등.
  - `Input`: O(1) 비트마스크 입력 버퍼링(`InputBuffer`), 방향 감지(`Direction8`), 입력 장치 자동 감지.
  - `StateMachine`: 상태 머신 및 계층형 FSM 구조 지원.
  - `Messaging`: 글로벌/로컬 이벤트 버스(`GameEventBus`, `LocalEventBus`), UniTask/Awaitable 비동기 트리거.
  - `Transitions`: 17종 셰이더 기반 화면 전환 효과 시스템(`ScreenTransitionManager`).
  - `Math`: 8방향 2D 수학 유틸리티, 보간 헬퍼.
  - `Common`: `ObjectPool<T>`, `WeightedRandomPicker<T>`, `SingletonMono<T>`, `TweenEasing` (30종 자체 이징 함수).
  - `Data`: 직렬화 가능한 딕셔너리 및 데이터 구조.
  - `Diagnostics`: 프레임 모니터링, 성능 디버깅 유틸리티.
