# SizzleGameCore

Unity에서 사용할 수 있는 `Sizzle` 계열 코어 패키지를 모아둔 저장소입니다.

## UnityPackages

이 저장소에는 현재 아래 3개의 UPM 패키지가 포함되어 있습니다.

| 패키지 | 최신 버전 태그 | 설명 | 문서 |
| :--- | :---: | :--- | :---: |
| `com.sizzle.gametagsystem` | `GameTagSystem/v0.1.8` | 문자열 기반 계층 태그로 상태, 이벤트, 조건 검사를 다루는 경량 태그 시스템<br>• exact / descendant 계층 조회<br>• Timed tag(자동 만료) 및 Notify tag(단발 이벤트) 지원 | [상세 보기](./UnityPackages/com.sizzle.gametagsystem/README.md) |
| `com.sizzle.abilitysystem` | `AbilitySystem/v0.1.14` | `ScriptableObject` 기반 어빌리티 실행 및 제어 프레임워크<br>• `GameTag` 규칙 기반 발동 조건, 차단, 취소, 트리거 제어<br>• 모듈형 액션 시퀀스(`ActionSequence`) 및 디버거 툴 제공 | [상세 보기](./UnityPackages/com.sizzle.abilitysystem/README.md) |
| `com.sizzle.toolkits` | `Toolkits/v0.2.0` | 게임 개발 전반을 위한 고성능 모듈형 기반 툴킷 모음<br>• 가상화 스크롤뷰(`RecycledScrollView`) 및 UI 유틸리티<br>• FSM 상태머신, Awaitable 17종 화면 전환(Transitions)<br>• 비동기 이벤트 버스, $O(1)$ 선입력 버퍼, 수학/진단 툴 | [상세 보기](./UnityPackages/com.sizzle.toolkits/README.md) |

## Install

Unity Package Manager 기준으로 설치할 수 있습니다.

### 1. Git URL로 설치

`Window > Package Manager > + > Add package from git URL...` 에서 아래 URL을 사용합니다.

#### GameTag System

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.gametagsystem#GameTagSystem/v0.1.8
```

#### Ability System

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.abilitysystem#AbilitySystem/v0.1.14
```

#### Toolkits

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.toolkits#Toolkits/v0.2.0
```

> 최신 개발 버전을 받으려면 패키지 버전 태그 대신 `#main`을 지정할 수도 있습니다.  
> `com.sizzle.abilitysystem`은 `com.sizzle.gametagsystem`을 의존하므로, Git URL을 사용할 때는 `com.sizzle.gametagsystem`을 함께 추가하는 것을 권장합니다.

### 2. 로컬 경로로 설치

저장소를 직접 내려받아 두었다면 `Packages/manifest.json`에 `file:` 경로로 추가해서 사용할 수 있습니다.

```json
{
  "dependencies": {
    "com.sizzle.toolkits": "file:../SizzleGameCore/UnityPackages/com.sizzle.toolkits",
    "com.sizzle.gametagsystem": "file:../SizzleGameCore/UnityPackages/com.sizzle.gametagsystem",
    "com.sizzle.abilitysystem": "file:../SizzleGameCore/UnityPackages/com.sizzle.abilitysystem"
  }
}
```

Windows 환경에서는 프로젝트처럼 상대 경로 또는 절대 경로를 사용할 수도 있습니다.

```json
{
  "dependencies": {
    "com.sizzle.toolkits": "file:D:/Repository/SizzleGameCore/UnityPackages/com.sizzle.toolkits",
    "com.sizzle.gametagsystem": "file:D:/Repository/SizzleGameCore/UnityPackages/com.sizzle.gametagsystem",
    "com.sizzle.abilitysystem": "file:D:/Repository/SizzleGameCore/UnityPackages/com.sizzle.abilitysystem"
  }
}
```

## Package 선택 가이드

- **`com.sizzle.gametagsystem`**  
  상태 태그, 이벤트 태그, timed tag 등 경량 계층 태그 관리가 필요한 경우
- **`com.sizzle.abilitysystem`**  
  스킬/어빌리티 실행기와 태그 기반 상호작용 제어 및 모듈형 액션 시퀀스가 필요한 경우
- **`com.sizzle.toolkits`**  
  가상화 UI(`RecycledScrollView`), FSM 상태머신, 화면 전환, 이벤트 버스, 선입력 버퍼 등 범용 기반 툴킷이 필요한 경우
