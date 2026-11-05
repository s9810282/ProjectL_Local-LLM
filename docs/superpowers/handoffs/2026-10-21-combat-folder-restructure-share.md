# 전투 View 폴더·네임스페이스 정리 — 전체 공유 (2026-10-21, 미전달)

작성: 용근 작업(Claude). 근거: [폴더 정리 계획](../plans/2026-10-19-combat-folder-restructure-plan.md)(대응표 §3이 원본, 실행 기록 §9). **완료 커밋 `81354f33`**(Unity 컴파일·시험·Missing Script 문제 없음 — 2026-10-21 사용자 확인). 새 파일을 어느 폴더에 둘지는 [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md)가 원본입니다.
대상: 현강님·민형님·호진님·천명님(Combat 파일이나 Combat을 쓰는 파일을 고친 분 모두).

## 1. 무엇이 바뀌었나

`ViewSystem/Combat/` 아래가 평면(루트 148개 + `Display/` 93개)이라, `.cs` 241개를 역할별 폴더로 나누고 **네임스페이스를 폴더 경로에 맞췄습니다.** Combat 안의 문서 `.md` 셋도 설명하는 코드 옆으로 옮겼습니다. 동작·클래스 이름·멤버는 그대로입니다.

- 바뀐 줄은 `namespace` 줄, `using` 줄, MonoBehaviour 16개의 `[MovedFrom]` 한 줄, 완전 이름·부분 이름 참조 세 줄뿐입니다.
- `.meta`를 함께 옮겨 **GUID가 같습니다.** 씬·프리팹의 스크립트 연결은 그대로입니다.
- 루트 12개(`CombatViewSystem`·`ICombatViewService`·`CombatSceneRoot` 등)는 옮기지 않았습니다. 이 형식만 쓰는 파일(`OptionUI`, 튜토리얼 `TutorialViewSystem`·`TutorialViewUIDisplay` 등)은 바뀌지 않았습니다.

`NS` = `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat`

| 폴더 | 네임스페이스 | 두는 것 |
|---|---|---|
| `Combat/` | `NS` | 조정자·씬 연결·결과 수명 |
| `Input/` | `NS.Input` | 수신·분배·허용 판단·Gate |
| `Input/Interaction/` | `NS.Input.Interaction` | 배치·공격·아이템 끌기·호버 |
| `Input/MovePath/` | `NS.Input.MovePath` | 이동 경로·복귀 |
| `Input/UiEntry/` | `NS.Input.UiEntry` | 덱·가방 UI가 부르는 입구 |
| `Input/Assist/` | `NS.Input.Assist` | 커서 스냅·환경 설명·거절 안내 |
| `Input/ActionButtons/` | `NS.Input.ActionButtons` | 행동 버튼·포기·시작 확인 |
| `Input/Contracts/` | `NS.Input.Contracts` | 입력 → 표시 계약 |
| `Display/` | `NS.Display` | 표시 입구(`CombatSceneDisplay` 등) |
| `Display/Feedback/` | `NS.Display.Feedback` | 입력 중 표시·궤적 구성 |
| `Display/Field/` | `NS.Display.Field` | 격자·칸·강조·높이 표·궤적 그리기 |
| `Display/Unit/` | `NS.Display.Unit` | 유닛·유닛 HUD·표시 창구·클립 채널 |
| `Display/Map/` | `NS.Display.Map` | 지형·오브젝트·레일·열차 칸·가시밭·프랍 |
| `Display/Hud/` | `NS.Display.Hud` | 화면 UI·종료 화면 칸 |
| `Playback/` | `NS.Playback` | 사건 Presenter·재생 조정·단계 전환 |
| `Playback/Ordering/` | `NS.Playback.Ordering` | 순차 재생·동시 묶음·경로 표 |
| `Playback/Handlers/` | `NS.Playback.Handlers` | 사건 처리기 |
| `Playback/Players/` | `NS.Playback.Players` | 시작 조건·실행 목록·재생기 |
| `Playback/Support/` | `NS.Playback.Support` | 대기·소리·강조·보인 사실·연출 값 |
| `Presentation/Pieces/` | `NS.Presentation.Pieces` | 조각·조립·클립 재생 |
| `Presentation/Attack/` | `NS.Presentation.Attack` | 공격 재생·애니메이션·투사체·발사 기하·Gate |
| `Presentation/Movement/` | `NS.Presentation.Movement` | 걸음 레인·접촉·몸·높이 식 |
| `Presentation/Reaction/` | `NS.Presentation.Reaction` | 피격 반응·맞은 방향 |
| `Presentation/Railway/` | `NS.Presentation.Railway` | 기차·기차 카메라 |
| `Presentation/Cameras/` | `NS.Presentation.Cameras` | 카메라 창구 |
| `Presentation/Effects/` | `NS.Presentation.Effects` | 이펙트 창구 |

파일 하나하나가 어디로 갔는지는 계획 §3 대응표에 있습니다.

## 2. 병합할 때 할 일

1. **Combat 파일 내용을 고친 브랜치:** 바뀐 줄이 적어 git이 이름 변경으로 따라옵니다. 병합 뒤 그 파일의 `namespace` 줄이 폴더와 같은지만 보면 됩니다.
2. **Combat 형식을 쓰는 새 코드:** `CS0246`(형식을 찾을 수 없음)이 나면 그 형식의 폴더를 위 표에서 찾아 `using NS.<폴더>;`를 더합니다. 예: `FieldUnitView` → `using ...Combat.Display.Unit;`, `CombatUnitInfo` → `using ...Combat.Display.Hud;`.
3. **Combat 안에 새 `.cs`를 만든 브랜치:** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) §1의 판단 순서로 폴더를 골라 옮기고 `namespace`를 폴더 경로로 바꿉니다(`.meta` 함께). 옛 `NS`·`NS.Display`에 남겨도 컴파일은 되지만 규칙에서 벗어납니다.
4. **MonoBehaviour를 새로 옮길 때:** `[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "옛 네임스페이스")]`를 붙입니다(이번 16개와 같은 꼴).

## 3. 다른 작업자가 만든 파일

기준은 **처음 추가한 사람**입니다(git 기록, 이름 변경을 따라감). 옛 위치의 `C/`는 `ViewSystem/Combat/`입니다.

### 3.1 옮긴 파일 — 23개(`.cs` 20 + `.md` 3)

| 작업자 | 옛 위치 | 새 위치 | 파일 |
|---|---|---|---|
| 현강님(Twinsgm) | `C/` | `Input/UiEntry/` | `CombatDeckHoverInput`, `ICombatDeckHoverInput`, `CombatDeckPlacementInput`, `ICombatDeckPlacementInput`, `CombatItemInput` |
| 현강님 | `C/` | `Input/Assist/` | `CombatActionNotice`, `CombatEnvironmentHover`, `CombatEnvironmentText` |
| 현강님 | `C/` | `Input/ActionButtons/` | `CombatDeploymentConfirmation.cs`·`.md` |
| 현강님 | `C/Display/` | `Display/Hud/` | `CombatUnitInfo.cs`·`.md`, `CombatRewardSlot`, `CombatRewardExperienceAnimation` |
| 현강님 | `C/Display/` | `Display/Unit/` | `IUnitHUDAnchor`, `UnitHUDLayer`, `UnitHUD-screen-layer.md` |
| 현강님 | `C/Display/` | `Display/Map/` | `EnvironmentVisualKinds` |
| 민형님(JMH) | `C/` | `Playback/` | `CombatPhaseTransitionPresenter`, `CombatPhaseTransitionSettings` |
| 천명님(GwonCheonmyeong) | `C/` | `Input/MovePath/` | `CombatMovementInteraction` |
| 천명님 | `C/` | `Playback/Ordering/` | `CombatMovementPlayback` |
| 호진님(Leehojin) | `C/` | `Input/Interaction/` | `CombatAttackDragGesture` |

- 이 중 MonoBehaviour 셋(`CombatUnitInfo`·`CombatRewardSlot`·`UnitHUDLayer`)에는 `[MovedFrom]`이 붙었습니다. 씬·프리팹 연결은 GUID로 그대로입니다.
- 나머지 206개는 용근 작업이 만든 파일입니다. 그중 여러분이 고친 적 있는 파일(예: `FieldUnitView`·`ObjectViewSet`·`TerrainViewSet`·`PlacementGhost`·`FieldHighlightState`·`FieldView`)은 §2-1처럼 병합 때 이름 변경으로 따라옵니다.

### 3.2 `using`만 바뀐 파일 (Combat 밖)

| 작업자 | 파일 |
|---|---|
| 현강님 | Factory `CombatUnitHUDFactory`, UI `DeckCardList`·`BattleChangerUI`·`CombatEndUI`·`PackUI`, 시험 12개(`BattleChangerUITests`·`CombatActionNoticeTests`·`CombatDeploymentConfirmationTests`·`CombatEndUITests`·`CombatEnvironmentHoverTests`·`CombatRewardExperienceAnimationTests`·`CombatUnitInfoTests`·`DeckButtonUITests`·`DeckCardHoverTests`·`GameAudioTests`·`UnitHUDLayerTests`·`UnitHUDTests`) |
| 민형님 | Factory `InGameViewSystemFactory`, 시험 `CombatPhaseTransitionPresenterTests`. Factory 영역의 `CombatSceneDisplayFactory`·`CombatSceneInputFactory`(용근 작업이 만듦)도 `using`이 바뀌었습니다 |
| 호진님 | 튜토리얼 `TutorialViewFocusLocator`(`using` 둘: `Display.Unit`·`Display.Hud`), 시험 6개(`StageMapCombatEntryBootSmokeTests`·`TutorialViewFocusLocatorTests`·`HojinCombatAttackDragIntegrationTests`·`HojinCombatAttackDragTests`·`HojinCombatTornadoTerrainVisualTests`·`LobbyDepartureIntegrationTests`) |
| 천명님 | 시험 4개(`CombatMovementViewTests`·`FrenzyViewContractTests`·`MovementDraftTests`·`WallAttackInteractionTests`) |

시험 폴더 구조는 그대로입니다. `ViewDataTableTests`(용근)의 완전 이름 두 곳은 `...Combat.Display.Map.RailwayTrainVisualBinding`으로 고쳤습니다.

## 4. 문서 링크

- 현강님 문서 셋(`.md`)도 설명하는 코드 옆으로 옮겼습니다(§3.1, `.meta` 함께, 내용 무수정). 문서 안에는 경로가 없어 고칠 곳이 없었습니다.
- 그래서 이 문서를 경로로 가리키던 링크 둘이 옛 위치를 가리킵니다. 다른 분 문서라 고치지 않았습니다. 필요하면 고쳐 주세요.
  - 호진님 [튜토리얼 Buffer·Job 설계](../specs/2026-10-18-tutorial-buffer-job-public-contract-design-Hojin.md) 512줄: `CombatDeploymentConfirmation.cs`·`.md` 링크 → 새 위치 `ViewSystem/Combat/Input/ActionButtons/`
  - 현강님 기록 [데이터 경계 인계](2026-09-24-view-core-data-boundary-handoff.md) 1213줄: `CombatUnitInfo.md` 경로 → 새 위치 `ViewSystem/Combat/Display/Hud/`
- 그 밖에 문서 70개가 옛 `.cs` 경로를 가리킵니다. 지난 설계·계획·인계는 이력이라 고치지 않는다는 것이 계획의 범위입니다.
