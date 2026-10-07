# SizzleGameCore

Unity에서 사용할 수 있는 `Sizzle` 계열 코어 패키지를 모아둔 저장소입니다.

## UnityPackages

이 저장소에는 현재 아래 3개의 UPM 패키지가 포함되어 있습니다.

| 패키지 | 설명 | 문서 |
| --- | --- | --- |
| `com.sizzle.gametagsystem` | 문자열 기반 계층 태그를 이용해 상태, 이벤트, 버프/디버프, 조건 검사를 구성하는 경량 GameTag 시스템입니다. exact / descendant 조회, timed tag, notify tag를 지원합니다. | [`UnityPackages/com.sizzle.gametagsystem/README.md`](./UnityPackages/com.sizzle.gametagsystem/README.md) |
| `com.sizzle.abilitysystem` | `ScriptableObject` 기반 어빌리티를 `AbilityProcessor`로 실행하고, `GameTag` 규칙으로 실행 조건, 차단, 취소, 트리거를 제어하는 Ability 시스템입니다. | [`UnityPackages/com.sizzle.abilitysystem/README.md`](./UnityPackages/com.sizzle.abilitysystem/README.md) |
| `com.sizzle.toolkits` | 게임 개발 전반을 위한 기반 툴킷 모음입니다. 가상화 스크롤뷰(`RecycledScrollView`), FSM 상태머신, 화면 전환 셰이더(Transitions), 비동기 이벤트 버스, 입력 버퍼 및 UI/수학 유틸리티를 제공합니다. | [`UnityPackages/com.sizzle.toolkits/README.md`](./UnityPackages/com.sizzle.toolkits/README.md) |

## Install

Unity Package Manager 기준으로 설치할 수 있습니다.

### 1. Git URL로 설치

`Window > Package Manager > + > Add package from git URL...` 에서 아래 URL을 사용합니다.

#### GameTag System

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.gametagsystem#main
```

#### Ability System

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.abilitysystem#main
```

#### Toolkits

```text
https://github.com/siz20971/SizzleGameCore.git?path=/UnityPackages/com.sizzle.toolkits#main
```

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

- 상태 태그, 이벤트 태그, timed tag가 필요하면 `com.sizzle.gametagsystem`
- 스킬/어빌리티 실행기와 태그 기반 조건 제어가 필요하면 `com.sizzle.abilitysystem`
- 최적화 UI(`RecycledScrollView` 등), 상태머신, 화면 전환, 이벤트 버스 등 기반 툴킷이 필요하면 `com.sizzle.toolkits`
