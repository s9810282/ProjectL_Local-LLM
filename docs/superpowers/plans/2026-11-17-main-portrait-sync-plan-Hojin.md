# 메인 초상화 동기화 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 로비·스테이지 선택·전투에서 각 화면의 메인 캐릭터 초상화를 동기화하고 성능 특성을 검수한다.

**Architecture:** 기존 MainCharactorUI의 초상화만 내부 표시 클래스로 분리한다. 로비 Scope의 읽기 연결과 확정 Run ReadSink를 화면 정책에 따라 사용하며 캐릭터 키가 바뀔 때만 InfoIcon을 조회한다.

**Tech Stack:** Unity 6000.0.73f1, C#, uGUI, 기존 JMH Framework·View DB·Buffer ReadSink.

**Spec:** [메인 초상화 동기화 설계](../specs/2026-11-17-main-portrait-sync-design-Hojin.md)

## Global Constraints

- 사용자 승인한 단일 MP-a Phase만 구현하고 검수·보고한다. MM-c 표현 보정 대기는 유지한다.
- View 시험 코드·MSBuild·자동 EditMode·커밋·push 금지. **컴파일 미확인**으로 인계한다.
- Core·InGame DB·FieldCamera·Architecture 수정 금지. C# 추가 시 .meta 동반, UTF-8 LF.
- UI는 기존 MainCharactor 등록·Global Context 경로를 재사용한다.

## Review Focus

1. 로비 첫 진입·재진입에서 이전 Run 메인의 얼굴이 남지 않는다.
2. 로비 → 스테이지 → 전투 → 일반 승리 → 스테이지에서 확정 메인을 유지한다. 전투 개체/능력치 게시 대기가 얼굴을 바꾸지 않는다.
3. 로비 연결 실패·교체·반복 해제에서 이전 연결이 새 연결을 지우거나 선택값을 계속 보관하지 않는다.
4. 누락된 이미지/보유 기록은 잘못된 기본 얼굴로 숨기지 않고, 초기 연결·Run 리셋 대기는 정상적으로 비운다.
5. 변경 없는 프레임에 DB 조회·Sprite 대입·객체 할당이 추가되지 않으며, 같은 키의 로스터 변경은 이미지 갱신을 유발하지 않는다.

## Task 1 / MP-a: 공유 초상화 연결과 검수

**Files** (아래 런타임 경로의 기준은 `Assets/ProjectL/Script/Systems/RuntimeSystem/`):
- Create: `GlobalSystem/UISystem/Screens/Global/MainCharacterPortrait.cs` 및 `.meta` — 표시와 캐시.
- Modify: `GlobalSystem/UISystem/Screens/Global/MainCharactorUI.cs` — 조립·갱신 위임, 카테고리 초상화/기본 얼굴 제거.
- Modify: `GlobalSystem/UISystem/GlobalUIPresentation.cs` — 로비 읽기 연결 API와 화면 모드.
- Modify: `InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs`, `LobbyViewServices.cs`, `LobbyViewScope.cs` — Global Context 조회와 연결 수명.
- Modify: `Assets/ProjectL/Prefabs/UI/GlobalUICanvas.prefab` — `portrait` 필드를 실제 `Portrait_Head` Image(fileID 4337552421349035197)에 연결한다.

**Interfaces:**
- `IGlobalUIPresentation.BindLobbyMainPortrait(Func<EntityKey> readMain): IDisposable`는 연결 해제 손잡이를 반환한다.
- `MainCharacterPortrait.BindLobby(Func<EntityKey>): IDisposable`, `SetLobbyMode(bool)`, `Refresh()`를 Main UI가 위임한다. 초기 모드는 미지정이다.
- `LobbyViewServices.BindMainPortrait: Func<Func<EntityKey>, IDisposable>`를 Scope가 Start에서 호출하고 Dispose에서 반환한다.

- [x] 1. 승인한 원본·수명·오류/성능 계약을 구현하고 C#/.meta를 연결한다.
- [x] 2. diff/호출 경로·SSOT·SOLID·FW·기능 독립성을 정적으로 검수하고 별도 최종 리뷰를 받는다.
- [x] 3. 성능 정적 근거와 Unity 확인 항목, 미확인 범위를 기록한다.

**정적 확인:** 대상 파일 `git diff --check`(출력 없음), 새 파일 .meta GUID 중복 없음, 기존 InfoIcon 두 개 참조, 인터페이스 구현/호출자 누락 없음. 기존 시험이 구현 인터페이스 때문에 컴파일이 깨지는 경우만 최소로 맞춘다.

**사용자 Unity 확인:** 아킴보/스나이퍼 교차 선택(호버로는 변경 없음), 재선택, 팝업 개폐, 출발/스테이지/전투/일반 승리, 포기·패배·최종 승리 후 로비 기본값, 개발 Stage/Combat 직행. HP·골드·시너지와 콘솔도 확인한다. Profiler는 대상 기기의 Development Player에서 평상시 CPU/GC Alloc, 선택 변경 프레임의 Canvas rebuild/UI batch를 비교한다. 에디터 Deep Profile 수치만으로 기기 FPS를 단정하지 않는다.

## 실행 기록

- 2026-11-17: 사용자 지시를 개인 역할 규칙에 반영했다. 현재 기능 브랜치에서 기존 MM-a/b 변경을 보존하며 직접 구현한다.
- Ruling: 사용자 명시 구현 승인 및 기존 checkout 작업 흐름을 따른다. 별도 worktree·승인 재요청은 하지 않는다. 스킬의 시험/커밋 단계보다 AGENTS의 금지 규칙을 우선하며, 이 문서에 진행·검수 기록을 남긴다.
- Pre-flight: 단일 Task. Global API → Lobby 서비스 대리자 → Scope lease, Main UI → 내부 표시 클래스의 서명을 위와 같이 일치시킨다.
- MP-a 구현: 새 `MainCharacterPortrait`/.meta와 기존 C# 5개를 연결했다. 로비 연결 한 번에 읽기 대리자·해제 객체를 만들고, 기존 LateUpdate에서 값 비교 뒤 필요할 때만 표시한다. 기존 인터페이스 구현체와 LobbyViewServices 생성자는 각각 1곳이며 모두 갱신했다. 시험 코드의 호출 수정은 필요하지 않았다.
- Ruling: 기존 얼굴 Image 재사용을 위해 프리팹 참조 오류를 수정했다. 기존 필드의 fileID 978539052832581289는 `TextBox/5_450.png` 테두리였고, 실제 얼굴은 `Portrait_Head`의 fileID 4337552421349035197(Akimbo InfoIcon)이었다. 참조 수정이 없으면 얼굴이 고정되고 테두리에 얼굴이 그려지므로 승인된 표시를 구현하는 데 필요한 연결 보정이다. 표시 정책·레이아웃 결정은 바꾸지 않는다.
- 정적 확인: 대상 `git diff --check` 통과, C#/.meta/문서/프리팹 10개 UTF-8 LF 확인. 새 .meta GUID는 1곳뿐이다. 두 InfoIcon GUID는 각각 `Sprites/AlphaIcon/Akimbo.png.meta`, `Sniper.png.meta`로 연결된다. 프리팹 diff는 참조 한 줄뿐이며 Boot가 해당 프리팹을 생성한다. 씬의 별도 portrait override는 없다.
- 등록 경로 확인: `GlobalSystemFactory`가 UISystem과 GlobalUIPresentation을 생성해 Global 내부 Builder에 등록 → `GlobalSystemGroup`이 기존 GlobalUIPresentation 속성으로 보관하고 루트에는 Group만 Export → `LobbyViewSystem.PostBindService`가 Group Context에서 읽어 좁은 연결 대리자만 넘긴다. 등록 UIId/부모/Policy와 개폐 호출은 그대로다. Lobby Attach(100) 뒤 ShowLobby UI(200)이며, 연결 도중 실패하면 기존 Scope 정리가 연결도 반환한다.
- 최종 독립 검수(`review_portrait_sync`): Critical/Important/Minor 지적 없음. 원본·수명·캐시·어셈블리 접근·생성/등록/조회·프리팹 연결을 읽기 전용으로 확인했고 사용자 Unity 확인 단계로 인계 가능하다고 판정했다.
- Final Ruling: 검수 제외 항목 중 MM-a 모델 기능과 MM-c 표현 보정은 별도 기존 범위로 유지한다. 이미 fault된 화면의 정지 정책은 기존 UI 경계를 따르며 정상 Refresh에서만 초상화가 비워진다. 실제 임포트/렌더·전환 깜박임·FPS/GC 수치는 미확인으로 인계하고, 금지된 시험 미작성은 성공 증거로 간주하지 않는다.
- **MP-a 상태: 코드·프리팹 연결·정적 검수 완료, 컴파일·Unity 재생·Profiler 실측 대기.** 커밋·push하지 않았다.

## 성능 검수 결과 — 정적 근거, 실측 아님

| 상황 | 추가 초상화 처리 | 화면 다시 그리기 영향 |
|---|---|---|
| 로비에서 선택이 그대로 | 원본 선택 getter와 enum 값 비교 | 초상화 Sprite/활성 상태 대입 없음 |
| 스테이지·전투에서 덱/로스터가 그대로 | 현재 덱 참조, RosterRevision 비교 | 초상화 DB/보유 기록 조회·Sprite 대입 없음 |
| 서브 덱 편집·전직 등으로 Revision만 변경 | 보유 Dictionary 조회 후 같은 캐릭터 키인지 비교 | 같은 키면 아이콘 조회·Sprite 대입 없음 |
| 다른 캐릭터를 메인으로 선택 | View DB Dictionary에서 기존 Sprite 참조 조회 | 실제 Sprite가 다를 때만 setter 호출 |
| 로비 진입·이탈 | 연결 시 대리자/해제 객체 생성, 폐기 시 소유 연결만 제거 | 초기 대기/리셋 때 얼굴을 비우는 갱신 가능 |

정상적인 변경 없는 프레임의 초상화 코드에는 객체 생성·컬렉션 순회·LINQ·텍스처 로드/복제가 없다. 원본 `RunDeckBuffer.TryGetCurrent`는 보관한 참조를 반환하며, `CharacterBuffer.TryGetOwned`와 `UnitVisualTable.TryGetUnitVisual`은 Dictionary 조회다. 따라서 새 동기화가 매 프레임 큰 작업을 반복하는 구조는 아니다.

설치된 `com.unity.ugui@47e51ce530b9`의 `Runtime/UGUI/UI/Core/Image.cs`에서 실제 Sprite 변경 시 `SetAllDirty()`를 호출하는 것을 확인했다. 동일 크기/텍스처이면 일부 갱신을 생략하지만, 선택 변경 프레임의 UI mesh/Canvas rebuild·배칭 비용을 0으로 보장하지 않는다. 이미지·Canvas 수는 늘리지 않았다. 기존 HP/골드 갱신의 성능은 이번 개선 수치에 포함하지 않는다.

**컴파일 미확인. Unity 재생·Profiler·대상 기기 FPS/GC Alloc 수치는 미측정이다.** 사용자 확인은 Task 1의 경로와 Profiler 비교 항목을 따른다. 이번 변경은 DB 에셋 값을 바꾸지 않았다.
