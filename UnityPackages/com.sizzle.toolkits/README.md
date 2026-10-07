# Sizzle.Toolkits

Unity 6 기반의 경량, 고성능, 모듈형 게임 개발 툴킷 프레임워크입니다.  
프로덕션 레벨의 상태 머신(FSM), 17종 화면 전환(Transition), 비동기 이벤트 버스(GameEventBus), UI/물리 유틸리티, 데이터 테이블 파서, 진단/디버깅 도구, $O(1)$ 선입력 버퍼 및 반응형 데이터 프로퍼티 등을 파일 하나로 손쉽게 탐색하고 적용할 수 있도록 설계되었습니다.

---

## 📑 목차 (Table of Contents)

1. [빠른 기능 색인표 (Quick Index)](#1-빠른-기능-색인표-quick-index)
2. [아키텍처 및 네임스페이스 설계 규칙](#2-아키텍처-및-네임스페이스-설계-규칙)
3. [디렉터리 구조](#3-디렉터리-구조)
4. [모듈별 상세 레퍼런스 및 사용 예제](#4-모듈별-상세-레퍼런스-및-사용-예제)
   - [4.1 Animations (스프라이트 애니메이션)](#41-animations-스프라이트-애니메이션)
   - [4.2 Common (공통 확장, 오브젝트 풀러, 이징, 싱글톤, 포맷터)](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터)
   - [4.3 Data (CSV/JSON 파서 및 고속 데이터 테이블)](#43-data-csvjson-파서-및-고속-데이터-테이블)
   - [4.4 Diagnostics (로깅, 성능 모니터링, 디버그 와이어 및 기즈모)](#44-diagnostics-로깅-성능-모니터링-디버그-와이어-및-기즈모)
   - [4.5 Input (O(1) 선입력 버퍼, 입력 장치 감지기, 액션 오버라이드)](#45-input-o1-선입력-버퍼-입력-장치-감지기-액션-오버라이드)
   - [4.6 Math (2D 8방향 벡터 수학)](#46-math-2d-8방향-벡터-수학)
   - [4.7 Messaging (글로벌/로컬 이벤트 버스, 반응형 값, async/await 스트림)](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림)
   - [4.8 Physics (2D/3D 충돌 및 트리거 이벤트 프록시)](#48-physics-2d3d-충돌-및-트리거-이벤트-프록시)
   - [4.9 StateMachine (열거형 및 타입 기반 FSM)](#49-statemachine-열거형-및-타입-기반-fsm)
   - [4.10 Transitions (Awaitable 기반 17종 화면 전환 시스템)](#410-transitions-awaitable-기반-17종-화면-전환-시스템)
   - [4.11 Types (기본 범용 타입 및 범위 래퍼)](#411-types-기본-범용-타입-및-범위-래퍼)
   - [4.12 UI (페이더, 롤링 카운터, 화면 클램핑, 가상 조이스틱, 타이프라이터)](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터)
5. [에디터 도구 및 데모 씬 안내](#5-에디터-도구-및-데모-씬-안내)
6. [환경 요구사항 및 의존성](#6-환경-요구사항-및-의존성)

---

## 1. 빠른 기능 색인표 (Quick Index)

원하는 모듈 또는 클래스를 클릭하면 상세 설명과 사용 예제로 즉시 이동합니다.

| 카테고리 | 네임스페이스 | 주요 클래스 / 구조체 | 한 줄 요약 | 바로가기 |
| :--- | :--- | :--- | :--- | :---: |
| **Common** | `Sizzle.Toolkits` | `WeightedRandomPicker<T>` | 가중치 확률 기반 $O(\log N)$ 이진 탐색 무작위 추첨기 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `ObjectPool<T>` | Unity `IObjectPool<T>` 표준 준수 및 GameObject 위치 대여 지원 풀러 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `SingletonMono<T>`, `PersistentSingletonMono<T>` | 중복 방지 및 씬 전환 유지(`DontDestroyOnLoad`) 지원 싱글톤 베이스 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `TweenEasing`, `EaseType` | 외부 라이브러리 없이 30여 종 이징 커브를 평가하는 순수 수학 유틸 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `CollectionExtensions`, `MonoExtensions`, `Collider2DExtensions`, `TransformExtensions`, `ColorExtensions`, `LayerMaskExtensions`, `FormatUtils` | 컬렉션/모노/콜라이더/트랜스폼/색상/레이어/포맷팅 편의 확장 모음 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `GlobalCoroutineRunner` | 일반 C# 클래스나 static 영역에서 코루틴을 돌리는 전역 호스트 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Common** | `Sizzle.Toolkits` | `GridPlacement` | 자식 트랜스폼들을 평면 격자 형태로 자동 정렬하는 컴포넌트 | [이동](#42-common-공통-확장-오브젝트-풀러-이징-싱글톤-포맷터) |
| **Input** | `Sizzle.Toolkits.Input` | `InputBuffer` | **[액션 게임 필수]** 리스트 순회 비용 없는 $O(1)$ 딕셔너리 기반 선입력 버퍼 | [이동](#45-input-o1-선입력-버퍼-입력-장치-감지기-액션-오버라이드) |
| **Input** | `Sizzle.Toolkits.Input` | `InputDeviceDetector` | 키보드/패드 실시간 감지 및 패드 단선/재연결 하드웨어 이벤트 감지기 | [이동](#45-input-o1-선입력-버퍼-입력-장치-감지기-액션-오버라이드) |
| **Input** | `Sizzle.Toolkits.Input` | `InputActionOverrideHelper` | New Input System의 액션 바인딩을 런타임에 임시 가로채고 복원 | [이동](#45-input-o1-선입력-버퍼-입력-장치-감지기-액션-오버라이드) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `GameEventBus` | 제네릭 구조체/클래스 기반의 타입 안전한 전역 Pub/Sub 이벤트 버스 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `ScopedEventBus` | 몬스터 1마리, 플레이어 1개 등 특정 엔티티 내부 컴포넌트끼리 소통하는 로컬 버스 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `ObservableValue<T>` | 값 변경 시에만 이벤트를 통지하는 가벼운 반응형 바인딩 프로퍼티 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `StickyMessageBus` | 최신 상태를 기억해 신규 구독자에게도 즉시 1회 전달하는 스티키 버스 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `AsyncMessageStream` | `await AsyncMessageStream.WaitForAsync<T>()` 형태로 특정 이벤트 발생 대기 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `RequestResponseBus` | 단방향 알림 대신 요청을 보내고 비동기 응답(`Task<T>`)을 받는 RPC 패턴 버스 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **Messaging** | `Sizzle.Toolkits.Messaging` | `QueuedMessageBus`, `DebouncedEventDispatcher`, `FilteredEventChannel<T>`, `HierarchicalEventRelay`, `EventBusDebugger` | 큐잉 지연 배치, 디바운스/쓰로틀, 채널 라우터, 계층 버블링, 이벤트 트레이서 | [이동](#47-messaging-글로벌로컬-이벤트-버스-반응형-값-asyncawait-스트림) |
| **UI** | `Sizzle.Toolkits.UI` | `UIUtility` | RectTransform이 화면 경계를 벗어나지 않도록 클램핑 및 월드-캔버스 좌표 변환 | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `CanvasGroupFader` | 팝업 열기/닫기 시 CanvasGroup Alpha/상호작용 페이드 1줄 제어 (Awaitable) | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `AnimatedNumberCounter` | 수치 변경 시 목표치까지 부드럽게 롤링(카운트업/다운)되는 텍스트 컴포넌트 | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `LongPressButton` | `Selectable` 상속으로 마우스/터치/키보드/게임패드 Submit을 완벽 지원하는 홀드 버튼 | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `TypewriterText` | 단어 줄바꿈 튐(Jitter) 없이 전체 레이아웃을 고정한 채 가시성을 밝히는 타이프라이터 | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `RecycledScrollView<TData, TCell>`, `RecycledScrollCell<TData>` | 대량 데이터 가상화 재사용 스크롤뷰 (수직/수평, 다단 컬럼, 대각선 시프트, 중앙 감지) | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **UI** | `Sizzle.Toolkits.UI` | `VirtualJoystick`, `DualFollowSlider`, `WorldSpaceUIFollower` | 모바일 가상 조이스틱, 체력바 완충 지연 잔상 슬라이더, 월드 타깃 추적 UI | [이동](#412-ui-페이더-롤링-카운터-화면-클램핑-가상-조이스틱-타이프라이터) |
| **Animations** | `Sizzle.Toolkits.Animations` | `SimpleSpriteAnimation` | 무거운 Animator 없이 Sprite 배열을 순차 재생하는 경량 컴포넌트 | [이동](#41-animations-스프라이트-애니메이션) |
| **Data** | `Sizzle.Toolkits.Data` | `DataTable<TKey, TData>`, `CSVTableParser<TData>`, `JSONTableParser<TData>` | RFC 4180 호환 CSV/JSON 파서 및 $O(1)$ 기본키 룩업/LINQ 쿼리 데이터 테이블 | [이동](#43-data-csvjson-파서-및-고속-데이터-테이블) |
| **Diagnostics** | `Sizzle.Toolkits.Diagnostics` | `Logger`, `PerformanceMonitor`, `DebugExtensions`, `DebugHelper`, `GizmosEx` | 심각도 로거, 온스크린 실시간 FPS/메모리 모니터링, 박스/원/부채꼴/화살표 디버그 렌더링 | [이동](#44-diagnostics-로깅-성능-모니터링-디버그-와이어-및-기즈모) |
| **Math** | `Sizzle.Toolkits.Math` | `Direction2DUtils` | 2D 입력 벡터 $\leftrightarrow$ 8방향(`Direction8`) 각도 및 단위 벡터 상호 변환 | [이동](#46-math-2d-8방향-벡터-수학) |
| **Physics** | `Sizzle.Toolkits.Physics` | `Collision2DEventProxy`, `CollisionEventProxy`, `Trigger2DEventProxy`, `TriggerEventProxy` | 하위 오브젝트의 2D/3D 충돌 및 트리거 이벤트를 부모나 특정 리스너로 릴레이 | [이동](#48-physics-2d3d-충돌-및-트리거-이벤트-프록시) |
| **StateMachine** | `Sizzle.Toolkits.StateMachine` | `EnumStateMachine`, `EnumStateBase`, `TypeStateMachine`, `TypeStateBase` | 직관적인 열거형(Enum) FSM 및 확장성 높은 클래스 타입/별칭 기반 FSM 제공 | [이동](#49-statemachine-열거형-및-타입-기반-fsm) |
| **Transitions** | `Sizzle.Toolkits.Transitions` | `ScreenTransitionManager`, `ScreenTransitionProfile` (17종), 드라이버 2종 | Unity 6 `Awaitable` 기반 비동기 화면 전환 오케스트레이터 및 17종 셰이더 프로필 | [이동](#410-transitions-awaitable-기반-17종-화면-전환-시스템) |
| **Types** | `Sizzle.Toolkits.Types` | `RangedValue<T>`, `RangedInt`, `RangedFloat`, `AlignmentAnchor`, `AxisPlane`, `Direction8` | Min-Max 클램프 및 백분율 지원 수치 래퍼, 9방향 앵커, 6축 평면 열거형 | [이동](#411-types-기본-범용-타입-및-범위-래퍼) |

---

## 2. 아키텍처 및 네임스페이스 설계 규칙

툴킷 개발 및 유지보수 시 직관적인 사용성과 인지 비용 절감을 위해 명확한 네임스페이스 & 폴더 매핑 규칙을 준수합니다.

### 2.1 `Common` 폴더 격리 규칙 (높은 접근성 보장)
* **경로**: `Assets/Plugins/Sizzle/Runtime/Common/` 및 `Common/Extensions/`
* **네임스페이스**: **`namespace Sizzle.Toolkits`** (루트 네임스페이스)
* **목적**: 확장 메서드(`Common/Extensions/`), 오브젝트 풀, 쿨다운 타이머 등 프로젝트 전반에서 매번 서브 네임스페이스를 임포트하지 않고도 `using Sizzle.Toolkits;` 선언만으로 즉시 자동 완성되어야 하는 핵심 도구들을 보관합니다. 물리적으로는 `Extensions/` 하위로 깔끔하게 격리하되 네임스페이스는 루트를 공유합니다.

### 2.2 도메인 모듈 1:1 매칭 규칙
* **경로**: `Assets/Plugins/Sizzle/Runtime/<ModuleName>/`
* **네임스페이스**: **`namespace Sizzle.Toolkits.<ModuleName>`**
* **규칙**: `Common` 폴더 외부에 위치한 모든 1급 모듈 폴더는 **폴더명과 네임스페이스가 100% 1:1로 일치**합니다.
  * 예: `Runtime/UI/` $\to$ `namespace Sizzle.Toolkits.UI`
  * 예: `Runtime/Input/` $\to$ `namespace Sizzle.Toolkits.Input`
  * 예: `Runtime/Messaging/` $\to$ `namespace Sizzle.Toolkits.Messaging`

---

## 3. 디렉터리 구조

```
Assets/Plugins/Sizzle/
├── README.md                          # 툴킷 종합 안내 문서 (본 파일)
├── Runtime/                           # Sizzle.Toolkits 어셈블리 (프로덕션 런타임)
│   ├── Common/                        # [루트 네임스페이스: Sizzle.Toolkits]
│   │   ├── Extensions/                # 확장 메서드 격리 폴더
│   │   │   ├── CollectionExtensions.cs # 컬렉션/배열 안전 조회 및 셔플 확장
│   │   │   ├── Collider2DExtensions.cs # 2D 콜라이더 앵커 위치 계산 확장
│   │   │   ├── ColorExtensions.cs      # 색상 알파, Hex, 밝기 조절 확장
│   │   │   ├── LayerMaskExtensions.cs  # 레이어마스크 비트연산 편의 확장
│   │   │   ├── MonoExtensions.cs       # 컴포넌트 안전 획득, 자식 계층 검색/생성, 레이어 일괄 변경
│   │   │   └── TransformExtensions.cs  # 트랜스폼 조작 편의 확장
│   │   ├── FormatUtils.cs             # 시간/숫자 축약 및 RichText 포맷터
│   │   ├── GlobalCoroutineRunner.cs   # 일반 클래스 지원 전역 코루틴 호스트
│   │   ├── GridPlacement.cs           # 그리드 자동 배치 유틸리티 컴포넌트
│   │   ├── ObjectPool.cs              # Unity IObjectPool 표준 구현 및 GameObject 특화 풀러
│   │   ├── SingletonMono.cs           # 싱글톤 및 영속 싱글톤 베이스
│   │   ├── TweenEasing.cs             # 순수 수학 기반 30여종 Easing 평가기
│   │   └── WeightedRandomPicker.cs    # 가중치 기반 O(log N) 무작위 추첨기
│   ├── UI/                            # (Sizzle.Toolkits.UI) UI 제어 컴포넌트
│   │   ├── UIUtility.cs               # 화면 경계 클램핑 및 좌표 변환 유틸리티
│   │   ├── CanvasGroupFader.cs        # 캔버스 그룹 페이드 및 상호작용 제어기
│   │   ├── AnimatedNumberCounter.cs   # 부드러운 숫자 롤링 카운트업/다운
│   │   ├── LongPressButton.cs         # Selectable 상속 마우스/패드/키보드 롱프레스 버튼
│   │   ├── TypewriterText.cs          # 줄바꿈 튐 없는 고정 레이아웃 타이프라이터
│   │   ├── VirtualJoystick.cs         # 모바일 온스크린 가상 조이스틱
│   │   ├── DualFollowSlider.cs        # 체력바 완충 잔상 슬라이더
│   │   └── WorldSpaceUIFollower.cs    # 월드 타깃 추적 스크린 UI 팔로워
│   ├── Input/                         # (Sizzle.Toolkits.Input) 입력 유틸리티
│   │   ├── InputBuffer.cs             # O(1) 고성능 액션 선입력 버퍼
│   │   ├── InputDeviceDetector.cs     # 입력 장치 변화 및 연결/해제 이벤트 감지기
│   │   └── InputActionOverrideHelper.cs # New Input System 임시 오버라이드
│   ├── Messaging/                     # (Sizzle.Toolkits.Messaging) 이벤트 및 통신
│   │   ├── GameEventBus.cs            # 타입 안전 글로벌 Pub/Sub 이벤트 버스
│   │   ├── ScopedEventBus.cs          # 엔티티/오브젝트 로컬 이벤트 버스
│   │   ├── ObservableValue.cs         # 반응형 값 변경 감지 프로퍼티
│   │   ├── StickyMessageBus.cs        # 최신 상태 기억형 스티키 버스
│   │   ├── AsyncMessageStream.cs      # async/await 이벤트 대기 스트림
│   │   ├── RequestResponseBus.cs      # 비동기 요청-응답 RPC 패턴 버스
│   │   ├── QueuedMessageBus.cs        # 큐잉 및 일괄 배치 디스패치 버스
│   │   ├── DebouncedEventDispatcher.cs# 디바운스/쓰로틀 이벤트 디스패처
│   │   ├── FilteredEventChannel.cs    # 채널 및 조건식 기반 이벤트 라우터
│   │   ├── HierarchicalEventRelay.cs  # 계층형 버블링/터널링 이벤트 전파자
│   │   └── EventBusDebugger.cs        # 이벤트 흐름 추적 링 버퍼 디버거
│   ├── Animations/                    # (Sizzle.Toolkits.Animations) 경량 스프라이트 애니메이션
│   ├── Data/                          # (Sizzle.Toolkits.Data) 테이블 파서 및 데이터 테이블
│   ├── Diagnostics/                   # (Sizzle.Toolkits.Diagnostics) 로깅, 성능 모니터, 기즈모
│   ├── Math/                          # (Sizzle.Toolkits.Math) 8방향 벡터 변환 수학 유틸리티
│   ├── Physics/                       # (Sizzle.Toolkits.Physics) 2D/3D 충돌/트리거 프록시
│   ├── StateMachine/                  # (Sizzle.Toolkits.StateMachine) 유한 상태 머신
│   ├── Transitions/                   # (Sizzle.Toolkits.Transitions) Awaitable 17종 트랜지션
│   ├── Types/                         # (Sizzle.Toolkits.Types) 공통 기본 타입
│   └── Sizzle.Toolkits.asmdef
└── ...
```

---

## 4. 모듈별 상세 레퍼런스 및 사용 예제

### 4.2 Common (공통 확장, 오브젝트 풀러, 이징, 싱글톤, 포맷터)
> **네임스페이스**: `Sizzle.Toolkits` (루트 네임스페이스)

`using Sizzle.Toolkits;` 선언만으로 프로젝트 전반에서 매일 쓰이는 핵심 편의 도구들입니다.

#### 1) `WeightedRandomPicker<T>` (가중치 무작위 추첨기)
누적합 기반 이진 탐색으로 $O(\log N)$에 아이템/몬스터를 추첨합니다.
```csharp
var dropTable = new WeightedRandomPicker<string>();
dropTable.Add("Gold", 70f)
         .Add("Potion", 25f)
         .Add("LegendarySword", 5f);

string reward = dropTable.PickOne(); // 5% 확률로 전설의 검
```

#### 2) `ObjectPool<T>` (Unity IObjectPool 규격 GameObject 풀러)
Unity 공식 `IObjectPool<T>` 인터페이스를 구현하며, 위치/회전 지정 대여와 `using` 스코프 자동 반납을 지원합니다.
```csharp
[SerializeField] private Bullet m_bulletPrefab;
private ObjectPool<Bullet> m_bulletPool;

private void Awake()
{
    m_bulletPool = new ObjectPool<Bullet>(m_bulletPrefab, initialSize: 20, parent: transform);
}

public void Shoot(Vector3 pos, Quaternion rot)
{
    // 위치/회전을 지정하여 즉시 대여
    Bullet b = m_bulletPool.Get(pos, rot);
    // 반납 시 m_bulletPool.Release(b);
}
```

#### 3) `SingletonMono<T>` & `PersistentSingletonMono<T>`
```csharp
// 씬 전환 시에도 유지되는 영속 싱글톤
public class AudioManager : PersistentSingletonMono<AudioManager>
{
    public void PlayBgm(string name) { /* ... */ }
}
// 사용: AudioManager.Instance.PlayBgm("Theme");
```

#### 4) `TweenEasing` & `EaseType` (순수 수학 Easing 평가기)
```csharp
// 0부터 1까지의 진행률 t에 대해 탄성(Back/Bounce 등) 커브 계산
float easedValue = TweenEasing.Evaluate(EaseType.OutBack, 0.5f);
```

#### 5) `TransformExtensions`, `ColorExtensions`, `LayerMaskExtensions`, `FormatUtils`
```csharp
// 트랜스폼 X 좌표만 변경 및 자식 일괄 정리
transform.SetPositionX(5.0f);
transform.DestroyAllChildren();

// 색상 알파 및 Hex 변환
Color fadeColor = Color.red.WithAlpha(0.5f);
string hex = fadeColor.ToHex();

// 레이어마스크 검사
if (m_enemyLayerMask.Contains(other.gameObject)) { /* ... */ }

// 포맷팅 헬퍼
string timeStr = FormatUtils.FormatTime(125f); // "02:05"
string compact = FormatUtils.FormatCompact(1500000); // "1.5M"
string redText = "위험!".WithColor(Color.red).Bold();
```

---

### 4.5 Input (O(1) 선입력 버퍼, 입력 장치 감지기, 액션 오버라이드)
> **네임스페이스**: `Sizzle.Toolkits.Input`

#### 1) `InputBuffer` (O(1) 고성능 선입력 버퍼)
리스트 순회 비용 없이 딕셔너리와 타임스탬프를 활용해 $O(1)$로 선입력을 보관하고 즉시 소비합니다.
```csharp
private InputBuffer m_inputBuffer = new(defaultBufferDuration: 0.25f);

private void Update()
{
    if (Input.GetKeyDown(KeyCode.Z)) m_inputBuffer.Buffer("Attack");
    if (Input.GetKeyDown(KeyCode.Space)) m_inputBuffer.Buffer("Dodge");

    // 캐릭터가 행동 가능한 타이밍에 O(1) 소비
    if (CanAct())
    {
        if (m_inputBuffer.Consume("Dodge"))
        {
            PerformDodge();
        }
        else if (m_inputBuffer.Consume("Attack"))
        {
            PerformAttack();
        }
    }
}
```

#### 2) `InputDeviceDetector` (입력 장치 변화 및 연결/해제 이벤트 감지기)
키보드/마우스 $\leftrightarrow$ 게임패드 실시간 전환뿐만 아니라, 컨트롤러 단선(Disconnect)이나 신규 장치 연결 이벤트를 통합 감지합니다.
```csharp
// 감지기 활성화 및 이벤트 바인딩
InputDeviceDetector.Enable();
InputDeviceDetector.OnDeviceTypeChanged += HandleDeviceTypeChanged;
InputDeviceDetector.OnDeviceConnectionStatusChanged += HandleConnectionStatusChanged;

private void HandleDeviceTypeChanged(InputDeviceType deviceType)
{
    // 조작 장치에 맞춰 UI 키 가이드 이미지 교체
    m_keyGuideImage.sprite = (deviceType == InputDeviceType.XboxGamepad) ? m_xboxIcon : m_keyboardIcon;
}

private void HandleConnectionStatusChanged(string deviceName, DeviceConnectionStatus status)
{
    if (status == DeviceConnectionStatus.Disconnected)
    {
        Debug.LogWarning($"패드 연결 끊김: {deviceName}! 게임을 자동 일시정지합니다.");
        PauseGame();
    }
}
```

#### 3) `InputActionOverrideHelper` (New Input System 액션 런타임 오버라이드)
UI 팝업이나 미니게임 진입 시 기존 플레이어 조작 액션을 임시 가로채고 복원합니다.

---

### 4.7 Messaging (글로벌/로컬 이벤트 버스, 반응형 값, async/await 스트림)
> **네임스페이스**: `Sizzle.Toolkits.Messaging`

#### 1) `ScopedEventBus` (오브젝트 로컬 이벤트 버스)
특정 캐릭터나 몬스터 내부의 컴포넌트끼리만 격리 통신할 때 사용합니다.
```csharp
public class CharacterEntity : MonoBehaviour
{
    public readonly ScopedEventBus LocalBus = new();
}

// 하위 컴포넌트에서:
m_entity.LocalBus.Subscribe<StaminaChangedEvent>(e => m_staminaUI.UpdateGauge(e.Current));
m_entity.LocalBus.Publish(new StaminaChangedEvent(80));
```

#### 2) `ObservableValue<T>` (가벼운 반응형 바인딩 프로퍼티)
```csharp
public class PlayerStats
{
    public ObservableValue<int> Hp = new(100);
}

// UI 연동: 값이 실제로 바뀔 때만 호출됨
stats.Hp.OnValueChanged += (oldHp, newHp) => Debug.Log($"HP 변경: {oldHp} -> {newHp}");
stats.Hp.Value = 90; // 이벤트 발동
```

#### 3) `AsyncMessageStream` (C# async/await 이벤트 대기)
```csharp
// 튜토리얼에서 플레이어가 공격할 때까지 대기
await AsyncMessageStream.WaitForAsync<PlayerAttackEvent>();
Debug.Log("플레이어가 공격 완료! 다음 튜토리얼 스텝 진행");
```

#### 4) `RequestResponseBus` (비동기 RPC 패턴 버스)
```csharp
// 등록 측:
RequestResponseBus.RegisterHandler<GetItemInfoRequest, ItemInfoResponse>(req =>
{
    return new ItemInfoResponse(req.ItemId, "엑스칼리버");
});

// 호출 측:
var info = await RequestResponseBus.RequestAsync<GetItemInfoRequest, ItemInfoResponse>(new(101));
```

---

### 4.12 UI (페이더, 롤링 카운터, 화면 클램핑, 가상 조이스틱, 타이프라이터)
> **네임스페이스**: `Sizzle.Toolkits.UI`

#### 1) `UIUtility` (화면 경계 클램핑 및 좌표 변환 정적 유틸)
툴팁이나 팝업이 화면 밖으로 잘리지 않도록 안전하게 위치를 보정합니다.
```csharp
// 마우스 위치에 툴팁을 띄울 때 화면 모서리를 벗어나지 않게 클램핑
Vector2 clampedScreenPos = UIUtility.ClampToScreen(m_tooltipRect, Input.mousePosition, padding: 15f);
m_tooltipRect.position = clampedScreenPos;
```

#### 2) `CanvasGroupFader` (팝업 열기/닫기 1줄 제어)
```csharp
[SerializeField] private CanvasGroupFader m_popupFader;

public async void OpenPopup()
{
    // Awaitable 비동기 페이드인
    await m_popupFader.ShowAsync(duration: 0.3f);
}

public void ClosePopup()
{
    m_popupFader.Hide();
}
```

#### 3) `AnimatedNumberCounter` (숫자 롤링 카운트업)
```csharp
[SerializeField] private AnimatedNumberCounter m_goldCounter;

public void AddGold(long amount)
{
    m_goldCounter.SetTarget(m_goldCounter.TargetValue + amount);
}
```

#### 4) `LongPressButton` (Selectable 상속 마우스/패드/키보드 롱프레스 버튼)
`Selectable`을 상속하여 마우스/터치뿐만 아니라 키보드/게임패드 내비게이션 및 Submit 입력을 완벽 지원합니다.
```csharp
[SerializeField] private LongPressButton m_skipButton;

private void Start()
{
    m_skipButton.OnHoldCompleted += () => Debug.Log("롱프레스 완료! 컷신 스킵");
}
```

#### 5) `TypewriterText` (줄바꿈 튐 없는 고정 레이아웃 타이프라이터)
단어가 완성되면서 갑자기 다음 줄로 튕겨 내려가는 Word Wrap Jitter 현상을 투명 태그 기반으로 원천 방지합니다.
```csharp
[SerializeField] private TypewriterText m_typewriter;

public void ShowDialog(string message)
{
    m_typewriter.Play(message);
}

public void OnScreenClicked()
{
    if (m_typewriter.IsTyping)
    {
        m_typewriter.Skip(); // 즉시 전체 텍스트 출력
    }
}
```

#### 6) `VirtualJoystick` (온스크린 가상 조이스틱)
```csharp
[SerializeField] private VirtualJoystick m_joystick;

private void Update()
{
    Vector2 moveDir = m_joystick.InputVector;
    transform.Translate(moveDir * 5f * Time.deltaTime);
}
```

#### 7) `RecycledScrollView<TData, TCell>` (가상화 재사용 스크롤뷰)
수백~수천 개의 데이터를 화면 가시 범위 내의 소수 슬롯만으로 가상화하여 렌더링합니다. 수직/수평, N단 컬럼, 대각선 시프트 배치 및 뷰포트 중앙 진입 감지 이벤트를 지원합니다.
```csharp
public class InventoryScrollView : RecycledScrollView<ItemData, ItemCell>
{
    private void Start()
    {
        SetData(itemList);
        OnItemCentered += (index, data) => Debug.Log($"중앙 선택: {data.Name}");
    }
}
```

---

### 4.1 Animations (스프라이트 애니메이션)
`SimpleSpriteAnimation` 컴포넌트로 Sprite 배열을 전달하여 경량 2D 애니메이션을 재생합니다.

### 4.3 Data (CSV/JSON 파서 및 고속 데이터 테이블)
`DataTable<TKey, TData>`, `CSVTableParser<TData>`, `JSONTableParser<TData>`를 통해 기획 텍스트를 $O(1)$ 기본키 테이블로 로드합니다.

### 4.4 Diagnostics (로깅, 성능 모니터링, 디버그 와이어 및 기즈모)
`Logger`, `PerformanceMonitor`, `DebugExtensions.DrawBoxCast`, `GizmosEx`로 디버깅과 프로파일링을 지원합니다.

### 4.6 Math (2D 8방향 벡터 수학)
`Direction2DUtils`로 `Vector2`와 `Direction8`을 상호 변환합니다.

### 4.8 Physics (2D/3D 충돌 및 트리거 이벤트 프록시)
`Collision2DEventProxy`, `CollisionEventProxy`, `Trigger2DEventProxy`, `TriggerEventProxy`로 자식 콜라이더 이벤트를 중계합니다.

### 4.9 StateMachine (열거형 및 타입 기반 FSM)
`EnumStateMachine`과 `TypeStateMachine` 2가지 방식의 고성능 FSM을 제공합니다.

### 4.10 Transitions (Awaitable 기반 17종 화면 전환 시스템)
`ScreenTransitionManager.Instance.TransitionAsync()`로 17종 셰이더 프로파일(ColorFade, SlashWipe, VHSRewind, VoronoiShatter 등)을 비동기 구동합니다.

### 4.11 Types (기본 범용 타입 및 범위 래퍼)
`RangedInt`, `RangedFloat`, `AlignmentAnchor`, `Direction8`, `AxisPlane` 등 제약 수치 타입을 제공합니다.

---

## 5. 에디터 도구 및 데모 씬 안내

* **에디터 도구**:
  - `ScreenTransitionManagerEditor`: 인스펙터 실시간 테스트 및 Abort 제어.
  - `Tools > Sizzle > Run Table Parser Tests`: CSV/JSON 테이블 파서 자동 검증.
* **데모 씬**:
  - `Assets/Plugins/Sizzle/Demo/UI/RecycledScrollViewDemoScene.unity` (5개 옵션별 재사용 스크롤뷰 및 버튼 이동 검증)
  - `Assets/Plugins/Sizzle/Demo/Transitions/TransitionTestScene.unity` (17종 트랜지션 실시간 테스트)
  - `Assets/Plugins/Sizzle/Demo/StateMachine/StateMachineDemoScene.unity` (FSM 테스트)

---

## 6. 환경 요구사항 및 의존성

* **Unity Version**: Unity 6 (6000.0.0f1 이상 권장)
* **Scripting Runtime**: C# 9.0+ / Unity `Awaitable` 지원 필수
* **Render Pipeline**: Universal Render Pipeline (URP) 및 Built-in Render Pipeline (uGUI 드라이버 모드)
* **Dependencies**:
  - `com.unity.ugui` (uGUI 화면 전환 및 UI 컴포넌트)
  - `com.unity.inputsystem` (Input 모듈)
  - `com.unity.render-pipelines.universal` (URP 드라이버)

---
© 2026 Sizzle Toolkits. Distributed under the MIT License.
