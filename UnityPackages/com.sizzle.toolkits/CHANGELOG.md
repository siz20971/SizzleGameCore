# Changelog

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
