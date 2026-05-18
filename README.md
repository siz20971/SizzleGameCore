# SizzleGameCore

Unity에서 사용할 수 있는 `Sizzle` 계열 코어 패키지를 모아둔 저장소입니다.

## UnityPackages

이 저장소에는 현재 아래 두 개의 UPM 패키지가 포함되어 있습니다.

| 패키지 | 설명 | 문서 |
| --- | --- | --- |
| `com.sizzle.gametagsystem` | 문자열 기반 계층 태그를 이용해 상태, 이벤트, 버프/디버프, 조건 검사를 구성하는 경량 GameTag 시스템입니다. exact / descendant 조회, timed tag, notify tag를 지원합니다. | [`UnityPackages/com.sizzle.gametagsystem/README.md`](./UnityPackages/com.sizzle.gametagsystem/README.md) |
| `com.sizzle.abilitysystem` | `ScriptableObject` 기반 어빌리티를 `AbilityProcessor`로 실행하고, `GameTag` 규칙으로 실행 조건, 차단, 취소, 트리거를 제어하는 Ability 시스템입니다. | [`UnityPackages/com.sizzle.abilitysystem/README.md`](./UnityPackages/com.sizzle.abilitysystem/README.md) |

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

> `com.sizzle.abilitysystem`은 `com.sizzle.gametagsystem`에 의존하므로, Git URL로 사용할 때는 `com.sizzle.gametagsystem`도 함께 추가하는 것을 권장합니다.

### 2. 로컬 경로로 설치

저장소를 직접 내려받아 두었다면 `Packages/manifest.json`에 `file:` 경로를 추가해서 사용할 수 있습니다.

```json
{
  "dependencies": {
    "com.sizzle.gametagsystem": "file:../SizzleGameCore/UnityPackages/com.sizzle.gametagsystem",
    "com.sizzle.abilitysystem": "file:../SizzleGameCore/UnityPackages/com.sizzle.abilitysystem"
  }
}
```

Windows 환경에서는 데모 프로젝트처럼 절대 경로를 사용할 수도 있습니다.

```json
{
  "dependencies": {
    "com.sizzle.gametagsystem": "file:D:/Repository/SizzleGameCore/UnityPackages/com.sizzle.gametagsystem",
    "com.sizzle.abilitysystem": "file:D:/Repository/SizzleGameCore/UnityPackages/com.sizzle.abilitysystem"
  }
}
```

## Package 선택 가이드

- 상태 태그, 이벤트 태그, timed tag가 필요하면 `com.sizzle.gametagsystem`
- 스킬/어빌리티 실행기와 태그 기반 조건 제어가 필요하면 `com.sizzle.abilitysystem`
- 어빌리티 시스템을 사용할 예정이면 두 패키지를 함께 설치
