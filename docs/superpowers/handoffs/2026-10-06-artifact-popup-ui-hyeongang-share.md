# 현강님 공유 — 아티팩트 팝업이 현강님 UI를 사용하는 방식 (2026-10-06)

- 작성일: 2026-10-06
- 작성: 호진(View, 스테이지 선택·아티팩트 담당)
- 대상: 현강님(UI 담당)
- 브랜치: `feature/Hojin/Stage_Shop_Event` (커밋 전일 수 있음)
- 상태: **전달용 문서. 전달 미실시.** 확인·이견은 §8 표에 적어 주세요.
- 관련: 화면 문서 [ArtifactPopupUI.md](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.md), [UI 이전 설계](../specs/2026-10-06-artifact-popup-ui-migration-design.md), [팝업 계획](../plans/2026-10-05-artifact-popup-plan.md) §9·§11

이 문서는 **아티팩트 팝업이 현강님이 만든 UI 코드·문서 중 무엇을, 어떻게 쓰고 있는지** 알려 드리는 문서입니다. 현강님이 UI 기반 코드를 바꾸실 때 아티팩트 팝업에 영향이 있는지 이 문서로 확인하실 수 있습니다.

## 0. 한눈에

1. 아티팩트 팝업의 2D UI를 현강님 구조의 **`UIBase` 화면 `ArtifactPopupUI`** 하나로 만들었습니다. 3D 보드·모델·드래그는 UI가 아니라서 View(`ArtifactViewSystem`)에 있습니다.
2. 버튼 입력은 모두 **`UIInputBinding` + `IUIInputReceiver`** 로 받습니다. `Button.onClick` 리스너와 Inspector On Click은 쓰지 않습니다.
3. 열기·닫기는 모두 **UISystem(`OpenUI`/`CloseUI`)** 을 거칩니다. `Open`/`Hide`를 직접 부르지 않습니다.
4. 현강님 파일은 **`UIIDs.cs`에 `Artifact = 13` 한 줄**만 바꿨습니다. 현강님 화면 폴더에는 **새 파일 2개**(`ArtifactPopupUI.cs`, `ArtifactPopupUI.md`)를 추가했습니다. `UIBase`·`UISystem`·`UIInputBinding` 등 기반 코드는 고치지 않았습니다.
5. 팝업은 화면의 **"뒤로가기" 버튼**으로 닫습니다. ESC 닫기는 사용자 결정으로 넣지 않았습니다.

## 1. 사용한 현강님 구성 요소

| 구성 요소 | 어떻게 쓰는지 | 사용 위치 |
|---|---|---|
| `UIBase` | `ArtifactPopupUI`가 상속. `ID`·`GetUIData`·`BindUIService`·`Open`·`Hide` 구현. `ExportUIService`·`ParentID`는 기본 구현 그대로(루트 화면) | `Screens/Stage/ArtifactPopupUI.cs` |
| `IUIInputReceiver` | `ArtifactPopupUI`가 구현. `Click`(열기 −1, 닫기 −2, 목록 슬롯 0 이상), `HoverEnter`(목록 슬롯 → 상세 문구), `HoverExit`(사용 안 함) | 같은 파일 |
| `UIInputBinding.Attach` | 열기 버튼·뒤로가기 버튼은 `Awake`에서, 목록 슬롯 Button은 처음 만들 때 한 번 연결 | 같은 파일 |
| `UIId` | `UIId.Artifact = 13` 새로 추가 | `UIIDs.cs` |
| `IUISystemService.OpenUI` | 열기 버튼 클릭 시 `OpenUI(UIId.Artifact)` | `ArtifactPopupUI.cs` |
| `IUISystemService.CloseUI` | 뒤로가기 버튼 클릭 시, 그리고 StageSelect 씬을 떠날 때(View) `CloseUI(UIId.Artifact)` | `ArtifactPopupUI.cs`, `ArtifactViewSystem.cs` |
| `UISystem.BindScene` 흐름 | 씬 로드 때 `GetUIData`(View DB 조회)·`BindUIService`(Buffer·UISystem 서비스 받기)가 불린다는 전제에 의존 | `ArtifactPopupUI.cs` |
| `UISystem` 초기화·재바인딩의 `Hide` | 열리지 않은 UI의 `Hide`가 불린다는 동작에 의존. 그래서 `Hide`는 Panel만 끔(§4-1) | `ArtifactPopupUI.cs` |
| `BackCloseUI`·`HideAllUI` | 아티팩트 코드가 직접 부르지는 않음. 다른 곳에서 불려도 같은 `Hide` 경로로 정상 정리되도록 만들어 둠(테스트로 확인) | — |

**쓰지 않는 것:** `GlobalUIBase`, `OpenGlobalUI`, `OpenChildUI`/자식 UI, `UIPointerClickBoundary`, `UIHoverAction`.

**참고한 현강님 문서·코드:** `Assets/AGENTS.md`(UI 입력 규칙), `Assets/ProjectL/Script/README.md` 54·56행(씬 UI 바인딩), `DeckButtonUI.md`(바깥 버튼 + 자식 Panel 구조), `CombatLogUI.md`(화면 문서 형식), `TimelineUI`·`DeckButtonUI`(UI가 Buffer·DB를 직접 읽는 방식), `CombatLogUITests`·`GlobalUITests`(테스트 방식).

## 2. 씬 구성 (StageSelect)

```text
ArtifactPopupCanvas (Canvas, CanvasScaler, GraphicRaycaster, ArtifactSceneRoot, ArtifactPopupView, ArtifactPopupUI)
├─ ArtifactOpenButton (Button) "아티팩트"      열기 버튼. Panel 밖
└─ Panel (기본 비활성)                         열고 닫는 별도 자식
   ├─ PoolArea (GridLayoutGroup)               하단 보유 목록(슬롯 생성 위치, 반환 영역)
   ├─ StatusText / Title / ArtifactName / EffectDescription (TMP_Text)
   └─ CloseButton (Button) "뒤로가기"
```

- 목록 슬롯 프리팹 `Prefabs/UI/Artifact/ArtifactItem.prefab`에 `Button`을 추가했습니다(Transition·Navigation None, On Click 비어 있음).
- Inspector 연결은 화면 문서 [ArtifactPopupUI.md](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.md)에 표로 적었습니다.

## 3. 동작 흐름

```text
[열기]     열기 Button ─UIInputBinding─► ArtifactPopupUI.Click(−1)
               └─ (View가 연결돼 있고 오류 상태가 아니면) UISystem.OpenUI(UIId.Artifact)
                    └─ ArtifactPopupUI.Open(): Panel 켬 · 목록 그림 → View에 "열림" 알림
                         └─ View: 3D 보드 켬, 입력 Layer → Artifact
[닫기]     뒤로가기 Button ─UIInputBinding─► Click(−2) → UISystem.CloseUI(UIId.Artifact)
               └─ ArtifactPopupUI.Hide(): View에 "숨김" 알림 → 문구 지움 · Panel 끔
                    └─ View: 드래그 취소, 3D 보드 끔, 입력 Layer → StageSelection
[상세]     목록 슬롯 Hover/Click ─UIInputBinding─► 이름·효과 문구 표시(View DB)
[씬 이탈]  View가 UI와의 연결을 먼저 끊고 → UISystem.CloseUI(UIId.Artifact) → View 자체 정리
```

- UI와 View는 씬 진입 때 한 번 서로 연결합니다(`UIInputBinding.Bind`처럼 "수신자를 한 번 연결"하는 방식). C# 이벤트 구독은 쓰지 않습니다.
- UI가 Buffer(`ArtifactBuffer`)에서 보유 목록을, View DB(`ArtifactViewTable`)에서 이름·설명을 직접 읽습니다. 열려 있을 때만 Buffer 스냅샷이 바뀌었는지 보고 목록을 다시 그립니다.
- 상태 문구(배치 실패 이유 등)의 내용은 View가 정하고, UI는 표시만 합니다.
- 드래그로 배치·이동, Q/E 회전, 우클릭 회수는 UI 입력이 아니라 InputSystem `Artifact` Layer로 View가 받습니다.

## 4. 현강님 기준에 없어서 제가 정한 것 — 확인 부탁

1. **`Hide`가 루트를 끄지 않고 Panel만 끕니다.** `StageUI`·`DeckButtonUI`는 `Hide`에서 루트를 끄지만, 팝업은 열기 버튼이 같은 루트 아래에 있습니다. UISystem이 씬 바인딩·초기화 때 닫힌 UI의 `Hide`를 부르므로, 루트를 끄면 열기 버튼이 사라집니다.
2. **UISystem 서비스는 `BindUIService`의 resolver에서 받습니다**(`Global.UIServiceCommand`). `UIBase`의 `uiSystemService` 필드가 private이라 하위 클래스가 쓸 수 없기 때문입니다. `UIBase`에 하위 클래스용 접근 방법을 두실 계획이면 그쪽으로 바꾸겠습니다.
3. **네임스페이스는 폴더 경로**(`ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage`)입니다. `StageUI`와 같고, `DeckButtonUI`·`TimelineUI`의 `ProjectL.Systems`와는 다릅니다. 프로젝트 코딩 규칙(네임스페이스 = 폴더 경로)을 따랐습니다.
4. **UI ↔ View 연결은 좁은 인터페이스 2개**입니다. `IArtifactPopupScreen`은 View가 UI에 요구하는 것(상태·상세 문구 표시, 좌표 판정)이고, `IArtifactPopupHost`는 UI가 View에 알리는 것(열 수 있는지, 열림, 숨김)입니다. 둘 다 View 폴더에 `internal`로 있고, `ArtifactPopupUI`는 명시적 구현으로 이 인터페이스를 구현합니다.
5. **열기 버튼 위치:** `DeckButtonUI.md` 구조를 따라 팝업 UI가 열기 버튼을 소유합니다. StageSelect 공용 HUD 규칙이 생기면 그 규칙을 따르겠습니다.

## 5. 현강님이 코드를 바꾸실 때 아티팩트에 영향이 있는 경우

| 바뀌는 것 | 아티팩트 영향 |
|---|---|
| `UIBase`의 추상 멤버 서명(`GetUIData`·`BindUIService`·`Open`·`Hide`·`ID`) | `ArtifactPopupUI` 컴파일 수정 필요 |
| `IUIInputReceiver` 메서드나 `UIInputBinding.Attach` 서명·클릭 조건(왼쪽 클릭·Submit, 상호작용 가능 여부) | 버튼·슬롯 입력 |
| `OpenUI`/`CloseUI`의 이력 처리, 일반 화면을 새로 열 때 기존 화면을 닫는 규칙 | 다른 일반 화면이 열리면 팝업이 `Hide`되고 View가 정리됨(정상 동작). 규칙이 바뀌면 알려 주세요 |
| 씬 바인딩 때 닫힌 UI의 `Hide`를 부르는 동작 | §4-1의 Panel만 끄는 방식과 관련 |
| `UIId` 값 재배치 | `Artifact = 13` 유지 필요(병합 때 `Timeline = 12`와 함께) |

## 6. 테스트·검증

- `ArtifactPopupUITests`: 실제 UISystem(`GlobalUITests`와 같은 리플렉션 생성)으로 열기·닫기, `BackCloseUI`·`HideAllUI` 후 정리, 초기화 때 Panel만 숨김, On Click 0개, Hover 상세, 좌표 판정, 목록 슬롯 재사용을 확인합니다. 클릭·Hover는 `CombatLogUITests`처럼 `ExecuteEvents`로 보냅니다.
- `ArtifactPopupBootSmokeTests` 2개: 실제 Boot → StageSelect·DB·Core로 열기·닫기·상세·맵 복원, 배치·회전·이동·회수를 확인합니다.
- Unity 6000.0.73f1 배치 실행에서 관련 테스트가 모두 통과했습니다. ESC 제거 뒤의 실제 마우스 Play 확인은 따로 남아 있습니다.
- **참고 정보:** 배치 EditMode 실행에서는 `MonoBehaviour.Awake`가 호출되지 않는 것으로 보입니다. 그래서 현강님 기존 테스트 6개(`CombatLogUITests` 2, `DeckButtonUITests` 3, `GlobalUITests` 1)가 배치 실행에서 실패합니다. 변경 전(HEAD)에서도 똑같이 실패해서 이번 작업과는 무관합니다. 원인은 확정하지 않았습니다. 아티팩트 테스트는 `Awake`를 직접 호출해 이 문제를 피했습니다.

## 7. 변경 이력

- 2026-10-06: 처음에는 ESC(InputSystem `Artifact/Close` → `BackCloseUI`)로도 닫았으나, 사용자 요청으로 제거했습니다. 지금은 뒤로가기 버튼으로만 닫습니다. `Common/OpenSettings`의 ESC 바인딩은 그대로입니다.

## 8. 확인·회신

| 번호 | 내용 | 현강님 회신 |
|---|---|---|
| Q1 | `UIId.Artifact = 13` 사용, 병합 때 `Timeline = 12`와 함께 유지 | |
| Q2 | 현강님 화면 폴더(`Screens/Stage`)에 `ArtifactPopupUI.cs`·`.md`를 두는 것 | |
| Q3 | `Hide`가 Panel만 끄는 방식(§4-1) | |
| Q4 | UISystem 서비스를 resolver에서 받는 방식(§4-2), `UIBase` 접근자 계획 여부 | |
| Q5 | 네임스페이스 기준(§4-3) | |
| Q6 | StageSelect 공용 HUD 규칙이 생기면 열기 버튼 위치를 그쪽으로 옮길지(§4-5) | |
