> **현재 사용 기준 — 2026-10-26:** 화면 입력은 각 View InputFactory에서 조립하고 슬롯은 `InGame.ViewGroup.Input`이 소유한다. 새 Core의 진영 턴·공용 AP·유닛별 행동 사용은 Core 응답으로 읽고, 이동 되돌리기는 §2.1의 최신 연결을 따른다. 선택/Facts/전체 조립은 §2.5의 5라운드 결과를 따른다. V4-n 및 V5-a/b/c는 각각 사용자 컴파일·기존 시험·Unity 정상 보고로 인수했다. 직접 실행/개별 로그 검증과 구분하며 [최종 인수와 확인 공백](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 기록했다. 아래 상대 기록 보존 문구와 §10의 최초 한계는 당시 이력이다. 화면별 입력 조회·조립은 갱신된 예제와 [입력 설계](../specs/2026-10-13-view-structure-design-Hojin.md), [실행 기록](../plans/2026-10-13-view-structure-plan-Hojin.md#32-화면별-입력-조립-phase13-2026-10-1718)을 따른다. 과거 검증 건수를 이번 검증으로 합산하지 않는다.

> **상대방 기록 보존(코드 미병합):** 상대 커밋 `c1b9b0a`의 문서 내용을 보존했다. 본문의 완료·검증은 당시 브랜치 범위이며 현재 코드의 병합 후 검증을 뜻하지 않는다. 현재 코드에는 아직 상대 변경을 적용하지 않았다. [병합 검토·수정 안내](../plans/2026-10-10-stage-map-plan-Hojin.md#4-c1b9b0a-병합-검토-2026-10-11)를 함께 읽는다.

# 스테이지 입력 사용 안내 — View 작업자용

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 대상: 입력을 받아 화면을 움직이거나 명령을 기록하는 View 작업자
- 근거: [스테이지 내부 입력 설계](../specs/2026-09-30-stage-input-design.md)(규칙 I1~I9, 결정 D1~D7, §11 구현 결과)
- 브랜치: `feature/Yonggeun/Stage`

## 0. 한 줄 요약

**View는 Unity 입력을 직접 읽지 않는다.** `IStageInputReceiver`를 구현해서 수신 슬롯에 붙고(`Bind`), 지금 받을 입력의 종류는 Layer로 고른다(`ChangeLayer`/`ClearLayer`). 키가 무엇인지, 콜백이 언제 오는지는 입력 계층이 처리한다.

View가 쓰는 건 네 가지뿐이다.

| 쓰는 것 | 어디서 받나 | 하는 일 |
|---|---|---|
| `IStageInputReceiver` | View가 직접 구현 | 입력을 받는 메서드 9개 |
| `IStageInputReceiverSlot` | `resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input.Stage` | `Bind(this)` / `Unbind(this)` |
| `IInputLayerService` | `resolver.GetService(SystemGroupsService.Global).InputServiceCommand.InputLayerServices` | `ChangeLayer(Layer)` / `ClearLayer()` |
| `StageInputLayers` | 정적 클래스 | `Deployment`, `Combat` |

### 0.1 정의·조립·소유·실행 책임

| 책임 | 현재 위치 |
|---|---|
| 액션/Layer 이름과 키 설정 대상 | 각 화면의 Names/Layers/KeyBindings. 물리 키 기본값은 기존 InputActionAsset |
| 슬롯·Controller 생성과 동일 슬롯 연결 | StageInputFactory·StageSelectionInputFactory·ArtifactInputFactory·LobbyInputFactory |
| 슬롯4개 보관·접근 | ViewGroup.Input의 Stage·StageSelection·Artifact·Lobby. 별도 Service/Module 아님 |
| 화면 조립 결과 합성 | InGameViewSystemFactory. InputRegistrationSet은 목록만 방어 복사하고 객체/실행 상태를 복제하지 않음 |
| 공통 옵션 Router 소유 | GlobalSystemFactory가1회 생성하고 Global 마지막 모듈로 등록. 닫기 요청은 Global.OptionCloseRequests |
| Configuration·Configure·장치 입력 실행 | UnityInputBridge.InitData에서1회. 화면 전환에서는 Bind/Unbind·Layer만 변경 |
| 그룹 간 연결 | Runtime Factory는 View 등록 결과를 Global에 전달. 화면 슬롯·옵션 서비스를 루트에 재공개하지 않음 |

Controller/Layer/키 대상의 최종 등록 수는7/8/6이다. 화면 슬롯은 runtime마다 하나씩 만들고 Controller와 View가 같은 인스턴스를 쓴다. 슬롯을 얻기 위해 Factory를 다시 호출하거나 루트 fallback을 추가하지 않는다.

## 1. 구조

```text
[키 설정]  Assets/ProjectL/Input/PlayerInputActions.inputactions
    ▲ 참조 (View DB: DataBaseSO/ViewGroup/InputSettings/Item/InputSettings_Default)
    │
[Global 입력 계층]  GlobalSystem/InputSystem/UnityBridge/   ← View가 직접 부르지 않음
  UnityInputBridge: 에셋 복제·구독 → InputSystem.Submit(맵, 액션, 단계, 값)
  UnityInputLayerRuntime: 현재 Layer의 ActionMap만 켬
    │
[Global의 공통 InputSystem]  Layer가 허용한 맵만 경로표로 전달
    │
[스테이지 입력]  InGameSystem/ViewSystem/StageInput/
  DeploymentInputController / CombatInputController
    │  "무엇을 했나" (포인터 위치, 선택 누름·뗌·취소, 버튼 요청)
    ▼
  StageInputReceiverSlot ◄── Bind / Unbind ── 여러분의 View (IStageInputReceiver)
                                               └ 뜻을 정하고 CombatViewBuffer에 명령 기록
```

- **씬에 `PlayerInput` 컴포넌트는 필요 없다.** `UnityInputBridge`가 그 일을 대신한다.
  - 액션 에셋을 들고 있다.
  - 맵을 켜고 끈다.
  - `actionTriggered` 콜백을 받는다.
- 이름으로 함수를 찾는 SendMessage 방식 대신, Controller가 시작할 때 **함수를 직접 등록**한다. 이름이 틀리면 조용히 무시되지 않고 시작할 때 검증 에러가 난다.

## 2. 입력 한 번의 흐름 — Space로 턴 종료

```text
Space 누름
 → Unity: "Combat/EndTurn" Performed          (Combat 맵이 켜져 있을 때만)
 → UnityInputBridge → InputSystem.Submit("Combat", "EndTurn", Performed, true)
     └ 현재 Layer가 Combat 맵을 허용하는가? 아니면 여기서 버림
 → CombatInputController가 등록한 함수 → 슬롯.EndTurnRequested()
 → 슬롯에 붙은 View.EndTurnRequested()
 → View: CombatViewBuffer에 턴 종료 명령 기록(Core의 진영 턴 기준)
```

### 2.1 이동 되돌리기 — 기존 Undo 버튼·R (2026-10-31)

`Combat/Return` 액션과 기본 `<Keyboard>/r`, `KeyBindingAction.Return`, 행동 패널 ReturnSlot 2를 그대로 사용한다. 키 설정·재바인딩 저장값은 바꾸지 않는다. 기존 `returnButton`은 필수 참조이며 씬 연결은 사용자가 관리한다. 버튼은 전투 패널과 함께 보이고, 현재 선택 아군의 Core 응답이 준비되어 되돌릴 수 있을 때만 활성화한다.

버튼·키 → 기존 수신 창구 → Router.Return → `CombatMovementUndoInput` → `CombatCommandGate.SubmitPending` → `UndoMove` 순서다. 조회의 `SourceRevision`과 Buffer `QueryRevision`, 전투·턴·선택·요청 번호가 맞는 응답만 사용한다. Unknown/Pending·미선택·전경 입력(공격 조준/경로 작성 등)·명령 대기·재생·모달·튜토리얼 제한·종료/정지는 제출하지 않는다. 최신성 검사와 pending 원본을 별도로 복제하지 않는다.

Core가 보내는 `Returned` 사건을 기존 순차 재생으로 한 번 적용해 원래 칸·방향을 즉시 표시한다. 역방향 걷기는 없으며 `UndoMovement` 메타데이터로 거리를 따로 수정하지 않는다. 결과 표시/확인 뒤 이동 가능 여부를 다시 조회한다. 배치 우클릭 `ReturnUnit`과 경로 취소는 별개다. 동작 확인은 [계획 실행 기록](../plans/2026-10-30-movement-undo-view-plan.md#작성-기록)을 따른다. 2026-10-31 사용자 씬 연결·되돌리기 정상 작동 확인 완료. EditMode 실행·세부 경계 사례의 개별 확인은 별도 미확인이다.

### 2.2 현재 전투 이동 조작 (2026-10-26, Drag 방식 유지)

설계: [이동 Drag 입력](../specs/2026-10-13-combat-move-drag-input-design.md), 범위 수명은 [이동 모드 범위](../specs/2026-10-19-combat-move-mode-range-design.md). Q 입력은 없앴다.

0. 공격 모드가 아니면 이동 모드다. 선택한 아군의 현재 Core 이동 범위 응답을 표시한다. 공격 모드를 끄거나 Drag를 취소해도 유효한 응답은 다시 표시한다. 행동 사용·진영 턴·결과가 바뀌면 옛 응답을 버리고 현재 Core 응답을 따른다. 공격 모드·결과 재생 중·턴 종료 대기·적 차례에는 보이지 않는다.
1. 이동 가능한 선택 아군의 칸(앵커)에서 좌클릭을 **누르면** Drag가 시작된다. 같은 아군을 다시 눌러도 선택을 유지하므로 다시 Drag할 수 있다. 아직 가용성 응답이 없다면 기존 Core 이동 범위·상세 경로 승인 절차를 따른다.
2. 누른 채로 끌면 지나간 칸으로 경로를 그린다(경로 규칙은 아래와 같다).
3. 끝점에서 버튼을 **놓으면** 경로가 고정되고, 같은 경로의 최신 Core 승인 응답을 받은 뒤 이동한다. 놓은 뒤의 커서 이동은 무시한다.
4. 시작 칸·필드 밖·UI 위·아직 받지 않은 칸에서 놓거나, 놓은 뒤 Core가 이동 불가로 답하면 이동하지 않고 지정을 취소한다. 다시 Drag하면 된다.
5. Drag 중 우클릭·E·턴 종료·포커스 상실은 지정을 취소한다. R은 실행된 이동 되돌리기이며, Drag·공격 조준 중에는 제출하지 않는다.
6. 경로 지정·배치 Drag·공격 모드 중에는 커서가 필드의 다른 칸으로 넘어갈 때마다 OS 커서가 그 칸 중심으로 옮겨진다(같은 칸 안·필드 밖·UI 위는 자유, 2026-10-13). 끄려면 `CombatSceneRoot`의 Snap Cursor To Grid를 끈다. 설계: [커서 그리드 스냅](../specs/2026-10-13-combat-cursor-grid-snap-design.md).
7. 유닛 칸에 커서를 올리면 그 유닛의 공격 범위가 보인다(아군·적·차례가 아닌 유닛 모두, 배치·전투 국면, 2026-10-19). 아군은 공격 모드와 같은 공격 범위 색, 적은 적 공격 범위 색이다(FieldView의 Attack Range Color·Enemy Attack Range Color). Drag·경로 지정·공격 모드·결과 재생·대기·적 차례에는 보이지 않고, 끝나면 마지막 커서 위치로 다시 보인다. 범위 칸은 Core 조회 응답만 그린다. 설계: [유닛 호버 공격 범위](../specs/2026-10-19-combat-unit-hover-attack-range-design.md). 미리보기 슬롯의 규칙은 `CombatPreviewSlotRule` 한 곳이다. 전경 요청이 우선하고 안정된 동안 선택 가용성→이동 모드 범위→호버 순서로 조회한다. 유닛이 없는 칸에 플레이어가 아는 지뢰나 살아 있는 화약통이 있으면 그 피해 칸이 위험 범위 색(FieldView의 Hazard Range Color)으로 바로 보인다(Core 공개 입구 `CombatHazardAreas`, 미리보기 요청 없음, 2026-10-20 호버 설계 §8).

UI/필드 밖에서 놓으면 확정하지 않고 취소한다. 커서를 이미 지나온 경로 칸에 올리면 그 칸까지 경로가 잘린다(2026-10-09). 벗어났다가 돌아올 때는 기존 경로의 칸(그 칸까지 잘림)에서 이어야 하며 빈 구간을 자동 연결하지 않는다. 경로가 멈춘 동안 HUD는 "끝점에서 이어 그리세요"를, Core가 거절했으면 "이동 불가: 이유"를 보인다. E·턴 전환·포커스 상실·입력 해제는 경로 지정을 끝낸다.

경로 강조(청록)는 Core가 확인한 유지 경로에만 적용한다. 검증 중인 후보는 노랑, 거절된 후보는 빨강으로 별도 표시한다. 거절 시 남은 경로의 어느 칸에 붙은 칸을 가리키면 그 경로 칸까지 잘린 뒤 이어진다(이동 유닛 주변 4칸 포함, 2026-10-09). 먼 칸으로 건너뛰거나 자동 우회하지 않는다. Drag 중 불가 후보에 닿으면 기존 경로를 보존하고 다른 후보로 이어 그릴 수 있다(놓은 뒤 거절은 4번처럼 취소). 접두 경로가 표시되어 있어도 실행에는 전체 경로의 최신 승인이 필요하다.

**2026-11-02 뛰어넘기 경유 입력 보완(1차 미해결 후 재보완, 컴파일·재생 미확인):** 드래그 중 Core가 `DestinationOccupied`로 거절해도 전체 편집 후보를 남겨 다음 칸으로 이어 그릴 수 있게 했다. 예를 들어 `출발 → 빈 칸 → 벽`의 착지 불가 응답이 먼저 와도 그 너머까지 이어 전체 경로를 다시 견적낸다. 정규화 단계의 거절은 ValidStepCount가 0이므로 이 값으로 마지막 칸/중간 칸의 거절을 구분하지 않는다. 이동 가능 판정은 Core가 하며 View는 뛰어넘기 여부나 지형 통과 규칙을 계산하지 않는다. 강조는 이전 성공 경로와 현재 초안의 공통 접두, 끝점은 불가 색으로 유지한다. 다른 사유의 거절(거리 초과 등)은 기존 접두 자르기를 따르고, 놓은 뒤 모든 거절은 취소한다. 일반 유닛의 막힌 경로도 편집 후보만 남으며, 되짚거나 취소할 수 있고 Core 성공 없이 실행되지 않는다. 설계·범위와 사용자 확인 항목은 [뛰어넘기 드래그 보완 계획](../plans/2026-11-02-leap-drag-preview-plan.md)을 따른다.

### 2.3 새 Core 행동 가용성 소비 (2026-10-26)

필드에서 같은 유닛을 다시 눌러도 선택을 유지하고 강조를 즉시 맞춘다. 다른 유닛을 누르면 전환하고 빈 칸은 선택을 해제한다. 덱의 핀 토글은 기존대로다. 공격/경로 지정·UI 가림·결과 재생·명령 대기에서는 기존 선택 보호 조건을 따른다.

| 접점 | 현재 계약 |
|---|---|
| 조회 조립/소유 | `CombatSceneInputFactory`가 `CombatActionAvailabilityTracker` 하나를 만들고 SceneInput이 소유/정지한다. 공격/경로는 같은 Tracker를 빌린다. 별도 서비스/루트 등록은 없다 |
| Core 조회 | 기존 Preview 슬롯에 `CombatViewPreviewData.ActionAvailability(selectedActor)`를 게시하고 `CombatPreviewResult.ActionAvailability`를 소비한다. View가 InGame DB나 Core 계산기를 직접 조회하지 않는다 |
| 응답 수명 | Unknown/Pending/Ready. 전투·진영 턴·결과 순번·선택 개체가 바뀌면 폐기한다. 요청 번호/슬롯과 응답의 전투·턴·개체도 확인한다. 종료/죽음/필드 이탈/씬 해제의 옛 응답은 적용하지 않는다 |
| 슬롯 우선순위 | 전경 요청이 우선한다. 안정 시 가용성→이동 범위→호버 순서다. Pending이 선점되면 안정된 뒤 다시 조회하고, 현재 Ready는 중복 조회하지 않는다. 다른 소유자의 미응답 슬롯을 지우지 않는다 |
| 공격 버튼/E | 새 공격 진입은 현재 키 Ready.CanAttack이 필요하다. Unknown/Pending이면 확인 중이므로 새 진입을 허용하지 않는다. 늦게 답이 와도 이전 입력을 자동 실행하지 않는다. 이미 켜진 공격 모드 끄기는 유지한다 |
| 이동 Drag | 현재 Ready.CanMove=false이면 실제 시작 시도에 Core 사유를 안내한다. Unknown/Pending은 기존 Core 이동 범위·상세 경로 승인 절차를 유지한다. 가용성 응답만으로 이동을 확정하지 않는다 |
| 거절 안내 | 실제 시도/최종 거절에서만 기존 안내 창구를 쓴다. 조회·호버마다 팝업을 띄우지 않는다. 비용 부족 문구는 “공용 AP가 부족합니다!”다 |

Core의 공용 AP·비용·이동 한도·행동 사용 판정을 View에서 다시 계산하지 않는다. 선택 원본은 Display가 소유한 `CombatSelectionState`, Facts의 입력 선택 이름은 `SelectedActorId`다. 정보창/강조/입력은 같은 선택을 읽으며 상세 소유권은 §2.5를 따른다.

### 2.4 진영 턴·상태 표시·이동 실습 (2026-10-26)

- 선택이나 남은 AP 없이 수동 턴 종료가 가능하다. 공통 Gate·PlayerInput·Layer·결과 표시 완료·미처리 명령 검사는 유지하며 AP=0 자동 종료는 추가하지 않았다.
- 정보창은 기존 비용 칸에 미사용/이동함/공격함/보류와 현재 Core 응답의 짧은 불가 사유를 표시한다. A→B 전환·슬라이드 완료·재표시 때도 현재 개체의 비용/상태를 함께 복원한다. 기존 TMP 50×50 영역의 가독성은 레이아웃 변경 때 함께 확인한다.
- `NightCheckerUI`는 완료된 아군 턴 수와 배치/아군 준비/아군 턴/적 준비/적 응답을 함께 표시한다. 적 응답 중 8/8은 밤 종료 판정이 아니다. Core 국면 전환을 따른다.
- 특성 Snapshot에 전체/부분 사용 불가가 공개되면 표시한다. 정적 TraitKey 설명에는 현재 활성 여부를 단정하지 않는다. 보류된 Core 효과를 View에서 재구현하지 않는다.
- MoveOnly 실습도 수동 종료로 다음 턴을 받을 수 있다. 밤이 끝나 Deployment에 돌아오면 **같은 전투의 Running/Practice/PracticeGoal 대기**일 때만 기존 정상 EnterCombat으로 재진입한다. 실습 통과 뒤 다음 설명을 기다리는 제한 유지 구간에는 이 예외를 적용하지 않는다.
- 실습 통과는 기존대로 수락된 플레이어 Move의 실제 진입 칸과 결과 표시 완료로만 판단한다. 턴 종료/재진입만으로 통과하지 않는다. 항상 이동 불가능한 데이터는 별도 기획/튜토리얼 판단이 필요하다.

**확인 상태:** 4라운드 a/d/e/f/g/h 및 W13~W16 뒤 사용자 “컴파일·기존 시험·M01~M14 모두 정상” 보고로 V4-n을 인수했다. 5라운드 a/b/c도 각 변경 뒤 정상 보고를 받았다. 동작 목록은 [4라운드 계획](../plans/2026-10-25-combat-view-stage-round4-plan.md), 최종 결과/직접 실행과의 구분은 [5라운드 계획 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 둔다.

### 2.5 5라운드 선택·사실 읽기·운영 조립 (2026-10-26)

- `CombatSceneDisplayFactory`가 `Selection/CombatSelectionState`를 만들고 Display가 소유한다. `ICombatSelectionReader`는 정보 대상 `SelectedEntityId`와 살아 있는 필드 아군의 `SelectedActorId`/점유 칸만 제공한다. 적 정보/무선택의 Actor Empty는 정상이다. 선택 상태가 AP/행동 자격을 계산하지 않는다.
- Display는 Install/완료 Refresh마다 불변 CharacterData 참조 목록을 한 번 복사해 선택과 UnitInfo에 같은 목록을 전달한다. Buffer의 살아 있는 읽기 전용 래퍼를 시점 고정으로 간주하지 않는다. 같은 국면의 턴/결과는 유효 선택을 유지하고 죽음/이탈/국면 변경은 정리한다. UI Hide/Clear는 빌린 선택을 쓰지 않으며 Display Clear/Release/새 Install이 소유 상태를 정리한다.
- 선택 입력은 명시적 `ICombatUnitInspection`으로 전달한다. AttachCreated가 TakeDisplay→BeginUse→첫 표시 순서로 Reader를 연결한다(호출자 없던 다른 입구 `AttachDisplay`는 2026-11-05 조정자 나누기 CS6에서 지웠다). StopInput이 종료 화면의 선택을 먼저 지우지 않는다.
- `Input/CombatInputFactsReader.Read(includeUnhandledCommand, followUp)`는 매번 현재 연결/Buffer를 읽는다. 특정 씬의 Display/Selection을 서비스 수명에 캡처하거나 Tick Facts를 캐시하지 않는다. 선택 ID를 먼저 읽어 빈 범위 관측에 값으로 넘긴다. 이 관측이 Facts를 역호출하면 순환이 되므로 일반 Path의 `Facts.SelectedActorId` 읽기와 구분한다. Preview 요청 번호 일치·P/S·followUp/includeUnhandled 의미는 유지한다.
- 바인딩에서 확인한 `IUIConfirmationPresentation`을 Sources.Confirmation으로 넘기며 운영 Factory 깊이에서 Text를 캐스팅하지 않는다. 전체 운영 생성은 필수 재료를 검사하고 선택/경로/공격/아이템/호버·같은 가용성 Tracker를 명시적으로 연결한다. 정상 무전투/미연결/반복 해제와 구성 누락은 구분한다.
- 기존 부분 조립은 시험 어셈블리의 `CombatInputTestFactory.CreateInteraction`만 사용한다. Runtime이 이 도우미를 호출하지 않는다. 새 서비스/키/Builder·루트 Export·화면 등록은 없다. UI 표현 변경은 UnitInfo, 선택 정책은 Selection/Display, 사실 추가는 Reader/Facts/소비자와 필요한 조립에서 처리한다. 공통 수명/Preview 변경은 관련 통합 회귀가 필요하다.

실행/허락·기능/구조 인수·잔여 작업의 원본은 [5라운드 계획 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)이다. 공유 문서는 준비 상태이며 외부 발송하지 않았다.

## 3. View에서 입력 받기 — 현재 조회와 연결

실제 전투 수명과 입력 연결은 `CombatViewSystem`·`CombatInputReceiver`가 소유한다. 아래는 필요한 조회 경로와 계약을 설명하는 발췌이며, 실제 전투에 두 번째 수신자나 별도 슬롯을 추가하는 예제가 아니다.

### 3.1 PostBindService에서 필요한 참조 받기

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var input = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input;
IStageInputReceiverSlot slot = input.Stage;
IInputLayerService layers = global.InputServiceCommand.InputLayerServices;
```

슬롯 계약은 `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput`, Layer 서비스는 `ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View`, 그룹 키는 `ProjectL.Systems.ServiceKeys`에 있다. 다른 화면은 같은 context의 StageSelection·Artifact·Lobby를 받으며 하위 Scope/Receiver에는 필요한 슬롯만 전달한다. 화면 소비자가 Controller를 생성하거나 Configure를 호출하지 않는다.

### 3.2 연결·해제 수명

화면의 선행 데이터·서비스 준비가 끝난 뒤 현재 연결의 수신자를 `slot.Bind(receiver)`하고 필요한 Layer를 선택한다. 같은 슬롯에 이미 연결된 수신자를 다시 Bind하지 않는다. 화면 이탈/오류/종료에는 현재 연결 소유자가 Unbind하고 자신의 입력과 표시를 정리한다. 실제 순서와 재진입 보호는 기존 `CombatViewSystem`·Receiver·Scope를 유지한다.

강제 취소는 표시 정리만 하며 명령을 제출하지 않는다. 누름을 받은 소유자에게만 정상 뗌 또는 취소가 전달되고, 이전 연결의 늦은 입력은 새 연결을 조작하지 못해야 한다. 수신자는 입력 의도를 해석하고 기존 CombatViewBuffer 요청 경로를 사용한다. View가 Core 규칙을 다시 계산하거나 Core 상태를 직접 쓰지 않는다.

### 3.3 배치 → 전투 Layer 동기화

전투 시작 버튼을 눌렀다는 사실만으로 Combat Layer를 켜지 않는다. 현재 전투 입력 정책은 Core가 게시한 국면·결과와 View의 처리/재생 수명을 확인해 Layer를 동기화한다. 전투 시작이 거절되면 배치 입력이 유지되어야 한다. 결과 소비와 명령 대기는 이미 실제 전투에 연결돼 있으며 새 결과 처리 루프를 추가하지 않는다.

### 3.4 UI 버튼

uGUI Button은 Unity EventSystem → `UIInputBinding` → `IUIInputReceiver.Click(slot)` → 기존 화면 의도 경로를 사용한다. `UIInputBinding.Attach(button, receiver, slot)` 또는 기존 바인딩을 사용하고, 연결 수명 종료에는 기존 정리 경로로 해제한다. Button.onClick 구독이나 Inspector UnityEvent를 입력 경로로 추가하지 않는다.

키보드와 UI 버튼은 같은 기능 의도를 사용하지만 모든 UI 클릭을 InputSystem.Submit으로 다시 보내라는 뜻은 아니다. UI 위의 필드 입력 차단과 선택 취소는 현재 전투 입력의 피커·Router·Policy가 담당한다. 클릭·키보드 Submit·Hover 규칙은 [Assets 입력 규칙](../../../Assets/AGENTS.md)을 유지한다.

## 4. 수신 메서드 규칙

| 메서드 | 오는 때 | View가 할 일 |
|---|---|---|
| `PointerMoved(x, y)` | 포인터가 움직일 때(화면 좌표) | 호버·하이라이트. 좌표 → 칸 변환은 View 몫 |
| `SelectPressed(x, y)` | 선택 버튼을 누른 순간(마지막 포인터 좌표) | 배치: 집기 / 전투 이동: 이동 가능한 선택 아군 칸이면 Drag 시작 |
| `SelectReleased(x, y)` | **정상적으로 뗀** 순간 | 배치·공격: 확정 후보 / 전투 이동: 놓은 칸으로 확정 |
| `SelectCanceled()` | 선택이 **강제로** 끝남: Layer 전환·`ClearLayer`·종료·`Unbind` | **정리만 한다. 명령 기록 금지** |
| `Cancel()` | 취소 키(우클릭) | 진행 중인 제스처 정리 |
| `EnterCombatRequested()` | 배치 Layer의 버튼 | 기존 전투 시작 요청 경로로 전달 |
| `EndTurnRequested()`, `AttackModeRequested()` | 전투 Layer의 버튼 | 명령 기록 또는 모드 전환 |
| `RequestReturn()` | 전투 Undo 버튼·기존 R | 현재 선택의 최신 가용성 응답으로 UndoMove 제출(§2.1) |
| `CameraPanChanged(x, y)` | 배치·전투: 카메라 이동 키 방향이 바뀔 때(모두 떼면 (0, 0)) | 마지막 값을 기억했다가 매 프레임 이동(§13) |
| `CameraZoomScrolled(delta)` | 배치·전투: 휠을 굴릴 때마다(위 = 양수) | 부호만 세어 줌 칸으로(§13) |

보장되는 것:

- **한 번 누르면 `SelectPressed` 한 번**이다. 누른 채 있어도 반복되지 않는다.
- 누름 뒤에는 `SelectReleased`와 `SelectCanceled` 중 **정확히 하나**가 온다.
- **누름을 받은 View만** 그 뗌·취소를 받는다.
  - 누른 채 View가 바뀌면 옛 View가 `SelectCanceled`를 받는다.
  - 새 View에는 옛 뗌이 가지 않는다.
- 포인터 입력이 끊겨도(0, 0)으로 튀지 않는다.

## 5. Layer

| Layer | 켜지는 ActionMap | 쓰는 때 |
|---|---|---|
| (없음) | 없음 | 시작 직후, `ClearLayer()` 뒤 |
| `StageInputLayers.Deployment` | Common + Deployment + CombatCamera | 전투 진입 전 배치 |
| `StageInputLayers.Combat` | Common + Combat + CombatCamera | 전투 중 |

- `CombatCamera`(카메라 이동·줌, §13)는 두 Layer가 함께 켜므로 배치 ↔ 전투 전환에서 끄거나 다시 켜지 않는다.

- 입력을 전부 끌 때는 `ClearLayer()`를 쓴다. `ChangeLayer(default)`는 예외다.
- Layer를 바꾸는 순간 누르던 선택은 `SelectCanceled`로 끝난다.
- 등록되지 않은 Layer나, 이름만 같고 맵 구성이 다른 Layer로는 바꿀 수 없다(예외). 항상 `StageInputLayers`의 값을 쓴다.
- 연출 중이나 적 턴처럼 잠깐 입력을 막을 때도 Layer로 막는다(`ClearLayer` 후 다시 `ChangeLayer`). Controller에 상태 판단을 넣지 않는다.

## 6. 새 입력 추가하기 — 예: 전투 중 T키로 배치 방향 회전

1. **액션 에셋:** `Assets/ProjectL/Input/PlayerInputActions.inputactions`를 연다. `Combat` 맵에 액션 `Rotate`(Button)를 추가하고 `<Keyboard>/t`에 바인딩한다. R은 이동 되돌리기 액션이므로 이 예제에서 재사용하지 않는다.
2. **이름 상수:** `StageInputNames.Combat`에 추가한다.
   ```csharp
   internal const string Rotate = "Rotate";
   ```
3. **수신 계약:** `IStageInputReceiver`에 메서드를 추가한다.
   ```csharp
   /// <summary>전투 단계: 배치 방향 회전을 요청했습니다.</summary>
   void RotateRequested();
   ```
4. **슬롯 전달:** `StageInputReceiverSlot`에 추가한다.
   ```csharp
   public void RotateRequested() => _receiver?.RotateRequested();
   ```
5. **Controller 바인딩:** `CombatInputController.Configure`에 추가한다.
   ```csharp
   input.Press(StageInputNames.Combat.Rotate, _receiver.RotateRequested);
   ```
6. **View 구현:** `IStageInputReceiver`를 구현한 View와 테스트용 기록 대상(`StageInputTests.RecordingReceiver`)에 메서드를 추가한다.
7. **확인:** 컴파일하고 EditMode `StageInputTests`·`UnityStageInputTests`를 돌린다. 에셋에 액션이 없거나 이름이 다르면 `UnityStageInputTests.ProjectAsset_MatchesStageInputConfiguration`이 실패한다. 실행 중이라면 시작할 때 `Input configuration is invalid`로 멈춘다.

바인딩 종류:

| 메서드 | 받는 입력 | 쓰는 곳 |
|---|---|---|
| `input.Press(name, action)` | 버튼 Performed 한 번 | 단발 요청(턴 종료, 모드) |
| `input.Button(name, bool→)` | 누름 true / 뗌·취소 false | 누르는 동안 유지되는 동작 |
| `input.Float` / `input.Vector2` | 값(Canceled 때 0도 전달) | 축, 스크롤 |
| `input.Raw(name, kind, snapshot→)` | 단계와 값을 그대로 | 특별한 해석이 필요할 때(선택 버튼이 이 방식) |

## 7. 새 화면의 입력 추가하기

Configure는 런타임에 한 번이다. 모든 화면의 Controller·Layer·키 설정 대상은 초기 조립에서 준비하고 화면 전환 중에는 슬롯의 수신자만 바꾼다.

1. 해당 View 입력 폴더에 Names/Layer·수신 계약·슬롯·Controller와 작은 InputFactory를 둔다. 기존 화면 액션 변경은 그 화면 안에서 처리한다.
2. 화면 Factory가 슬롯을 한 번 만들고 같은 슬롯을 Controller와 조립 결과의 Slot에 전달한다. 등록 정보는 InputRegistrationSet으로 반환한다. 키 설정 대상은 해당 화면 KeyBindings, 물리 키 기본값은 에셋을 원본으로 둔다.
3. 새 화면을 추가할 때만 View 입력 context에 필요한 슬롯 프로퍼티를 더하고 InGameViewSystemFactory에서 새 Factory 결과를 합성한다. 기존 화면의 구체 Controller/Layer 목록을 Runtime에 복제하지 않는다.
4. InGame 조립 결과의 Registration을 Runtime이 GlobalSystemFactory.CreateWithInputRegistration으로 전달한다. Global은 공용 옵션만 더하고 InputSystemFactory에 공통 목록을 전달한다. 루트는 Buffer → Global → InGame → LifeCycle 순서로 Export한다.
5. 한 (ActionMap, Action)은 중복 등록하지 않는다. Configure·슬롯 동일성·runtime 격리와 해당 화면의 취소/팝업 복귀 검사를 확인한다.

현재 StageSelectionInputFactory는 Selection과 Popup(둘 다 `StageSelectionInputLayers`, 팝업 이름 `StageMapPopup`)을 함께 등록한다. LobbyInputFactory는 Lobby/DeckPopup, ArtifactInputFactory는 Artifact 입력을 등록한다. 새 기능을 위해 Framework의 Builder/Reader/Store 계약을 바꾸거나 내부 Factory/context를 새 서비스로 등록하지 않는다.

## 8. 하지 말 것

| 하지 말 것 | 이유 |
|---|---|
| 씬에 `PlayerInput`을 붙이거나 `InputAction`·`Keyboard.current`를 직접 읽기 | 입력 경로가 둘이 되어 Layer 차단이 안 먹는다 |
| Controller를 직접 호출 | Controller는 입력 계층 내부다. View는 수신 메서드로만 받는다 |
| `SelectCanceled`에서 명령 기록 | 강제 종료(화면 전환 등)가 배치·이동으로 확정된다 |
| `Unbind` 없이 View 파괴 | 파괴된 객체가 입력을 받는다. 파괴 **전에** `Unbind` |
| 이미 붙은 상태에서 다시 `Bind` | 예외다(같은 대상이어도). 먼저 `Unbind` |
| `ChangeLayer(default)` | 예외다. `ClearLayer()` |
| Controller에 전투 상태 판단 넣기 | Controller는 "무엇을 했나"만 전한다. 뜻은 View가 정한다 |

## 9. 키 설정 교체와 에러

**키 설정 교체:** `DataBaseSO/ViewGroup/InputSettings/Item/InputSettings_Default`의 **Actions** 칸에 다른 `.inputactions`를 넣는다.
- 코드를 고치지 않아도 된다.
- 이 테이블은 아이템이 정확히 하나여야 한다.
- 새 에셋에도 맵·액션 이름과 종류가 같아야 한다.

| 에러 메시지 | 뜻 | 고칠 곳 |
|---|---|---|
| `Input configuration is invalid:` + 목록 | 에셋과 코드가 안 맞음(맵·액션 없음, 값 종류 다름, 켤 Layer 없음) | 목록에 나온 에셋 액션 또는 `StageInputNames`·Controller |
| `[UnityInputBridge] … is required.` | View DB의 입력 설정이 없음 | `InputSettings_Default`와 ViewGroup 등록 |
| `Input Layer is not registered` / `does not match the registered definition` | 등록 목록에 없는 Layer, 또는 이름만 같은 다른 Layer | `StageInputLayers` 값 사용 |
| `InputSystem is not configured.` | 초기화 전(`InitData` 전)에 `ChangeLayer`/`ClearLayer` 호출 | 호출 시점을 `Initialize` 이후로 |
| `입력 수신 대상이 이미 연결되어 있습니다` | 중복 `Bind` | 앞 View가 `Unbind`했는지 확인 |

## 10. 최초 입력 구현 당시 한계 (2026-09-30 이력)

아래는 초기 구현 당시 기록이다. 현재 Boot/Combat View는 연결돼 있으며 이동 지정의 최신 사용법은 §12를 따른다.

- **런타임 부트스트랩이 없다.** `FrameworkRuntime`을 만들고 Tick하는 구동기가 없어서, 에디터 재생으로 입력을 확인할 수 없다. 확인은 EditMode 테스트로 한다.
- **실제 장치로 누름·뗌 순서를 검사하는 테스트는 보류했다.** 입력 해석 규칙은 `StageInputTests`(Submit 직접 호출)로, 맵 전환은 `UnityStageInputTests`로 확인한다.
- 프로젝트 설정의 전역 액션(`InputSystem_Actions`, Unity 기본 템플릿)이 재생 때 자동으로 켜진다. 받는 코드가 없어 동작에는 영향이 없다.
- 전투 View(수신 구현, 칸 변환, 드래그, 명령 기록)는 다음 설계 대상이다.

## 11. 파일 위치

| 파일 | 역할 |
|---|---|
| `Assets/ProjectL/Input/PlayerInputActions.inputactions` | 키 설정 |
| `Assets/ProjectL/DataBaseSO/ViewGroup/InputSettings/` | 어떤 키 설정을 쓸지(설정형 DB) |
| `Systems/RuntimeSystem/GlobalSystem/InputSystem/UnityBridge/` | Unity 연결(브리지, Layer 런타임, 검증기, 값 변환) |
| `Systems/RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs` | Framework 라우터(`ChangeLayer`, `ClearLayer`, `Submit`) |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/` | 이름, Layer, 수신 계약, 슬롯, Controller |
| `Systems/ServiceKeys/StageInputServiceKey.cs` | 독립 Slot 검사·호환 키. 운영 루트 조회 경로가 아님 |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/StageInputFactory.cs`, `StageInputKeyBindings.cs` | 같은 슬롯의 배치/전투/카메라 조작 Controller·Layer·키 설정 대상 조립 |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/CombatCameraInputController.cs` | 카메라 조작 맵(`CombatCamera`) Controller(§13) |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Cameras/CombatCameraInput.cs` | 카메라 키·휠·가장자리 상태 → 매 프레임 시점 호출(§13) |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/Input/InGameViewInputContext.cs` | runtime별 화면 슬롯4개 접근 |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Selection/` | Display 소유 선택 상태·Reader·Inspection 계약 |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputFactsReader.cs` | 판단마다 현재 연결/Buffer에서 Facts 읽기 |
| `Assets/Tests/EditMode/Framework/CombatInputTestFactory.cs` | 시험 전용 부분 입력 조립, Runtime 사용 금지 |
| `Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | 화면 조립 결과 합성·ViewGroup 소유 |
| `Systems/Factory/Runtime/RunTimeSystemFactory.cs` | 등록 결과 전달과 그룹 연결 |
| `Tests/EditMode/Framework/StageInputTests.cs`, `UnityStageInputTests.cs` | 테스트 |


## 12. 현재 전투 이동 지정 (2026-10-13, Phase 2 코드 반영·화면 확인 대기)

전투의 플레이어 입력 단계에서 이동 모드면 선택 아군의 현재 Core 이동 범위를 요청해 표시한다([이동 모드 범위](../specs/2026-10-19-combat-move-mode-range-design.md), 현재 규칙은 위 §2.2). 이동 가능한 선택 아군 칸을 누르면 그 범위로 경로 편집을 시작하며 실제 위치의 모델만 약간 흐려지고 커서 고스트가 필드 평면을 따라 움직인다. HUD·체력 숫자·Collider는 유지한다. 가용성 응답이 없을 때도 기존 범위·상세 경로 승인 절차는 유지하며, 응답 뒤 최신 경로의 정확한 이동 견적을 요청한다.

고스트는 견적 대기 중 중립색, 현재 경로 승인 시 유효색, Core가 거절한 후보에서는 무효색으로 표시된다. 같은 칸 안의 커서 움직임도 추적한다. UI/필드 밖에서는 숨기고 기존 경로 끝점으로 재진입하면 다시 표시한다. 놓아서 확정한 뒤 견적을 기다릴 때는 고스트의 월드 위치를 동결한다. 취소·E·턴 종료·입력 차단·Detach·제출 시 경로/고스트를 정리하고 원본 재질과 색을 복원한다. 범위는 이동 모드가 이어지면(우클릭·놓기 실패) 남고, 공격 모드 진입·턴 종료·입력 차단·Detach·제출(결과 재생)에서 지운다. 행동 사용 뒤에는 새 Core 응답을 따르며 되돌리기는 전경 입력이 끝난 뒤에만 제출되며 결과 갱신으로 범위를 다시 맞춘다.

표현 조정 원본은 `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset`의 `movementOriginalOpacity`(기본 0.65), `neutralGhostTint`(0.8, 0.8, 0.8, 0.5), 기존 유효/무효 고스트 색·크기·높이다. Ghost Material에는 View 소유 `Resources/View/MovementGhost.mat`을 연결했다. 모델은 원래 슬롯의 텍스처/색을 보존한 기존 셰이더의 투명 변형을 사용하며 공유 원본 재질을 수정하지 않는다. 실제 셰이더/화면 지원과 검증 상태는 [이동 표시 Task 계획](../plans/2026-10-13-movement-planning-feedback-plan.md) §4.4를 따른다.

**2026-11-02 뛰어넘기 이동 연출(코드 반영, 컴파일·재생 미확인):** 실제 Leap 발동이 있는 연속 통과 구간과 그다음 착지 걸음을 한 번의 점프로 잇는다. 일반 칸은 기존 이동이고, 굽은 경로도 원래 경유 칸을 따른다. 시간·높이는 `DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` → `Combat Presentation Settings Data` → **뛰어넘기 구간 연출**의 `Leap Seconds`(구간 전체 0.5초, 회전·중간 사건 대기 제외) / `Leap Height`(꼭대기 추가 높이 1)로 조정한다. 칸당 일반 Hop 설정과 별개이며 중간 피해·특성·환경 사건은 원래 순서로 재생한다. 범위·Unity 확인 항목은 [장애물 구간 도약 계획](../plans/2026-11-02-leap-obstacle-hop-plan.md)을 따른다.

## 13. 전투 카메라 조작 입력 (2026-10-27, CC-c 코드 반영·Unity 확인 대기)

설계 [카메라 조작](../specs/2026-10-26-combat-camera-control-design.md)(CC1~CC7), 계획·실행 기록 [카메라 조작 계획](../plans/2026-10-27-combat-camera-control-plan.md) §6.

| 입력 | 액션(맵 `CombatCamera`) | 바인딩 |
|---|---|---|
| 이동 | `Pan` — Value Vector2 | WASD 2D 묶음 + 화살표 2D 묶음 |
| 줌 | `Zoom` — **PassThrough** Axis | `<Mouse>/scroll/y` |
| 화면 가장자리 이동 | (액션 없음) | 전투 맵의 `PointerMoved` 마지막 좌표 |

흐름:

```text
CombatCameraInputController → 슬롯 → CombatInputReceiver(CameraPanChanged·CameraZoomScrolled)
  → 입력 분배 CombatSceneInputDriver.Route(카메라 요청은 선택·가용성·버튼 갈래 없이) → CombatSceneInput.Route → CombatCameraInput(상태 저장)
조정자 Tick 끝 → 입력 분배 TickEnd → CombatSceneInput.TickCamera(facts, characters) → CombatInputPolicy.CanMoveCamera → ICombatCameraControl(CombatCameraView).Zoom·Pan
  → 시점이 바뀌었으면(그 Tick·다음 Tick·줌 렌즈가 옮겨 가는 동안) 마지막 포인터로 CombatInputRouter.RefreshPointer(2026-11-04 평가 C10-01)
```

규칙:

- 카메라 요청 판정은 `CombatInputRouter.IsCameraRequest` 하나다. 카메라 요청은 Router `Route`로 가지 않는다(가면 예외).
- **확인창이 열려 있어도 수신 창구가 카메라 요청을 통과시킨다(계획 P5).** 입력은 상태만 저장하고 움직일지는 Tick의 정책이 정하므로, 확인창 동안 뗀 키가 버려져 닫힌 뒤 계속 움직이는 일이 없다.
- 움직여도 되는지: `CombatInputPolicy.CanMoveCamera` = 입력 연결·전투 그림·종료(결과 화면) 아님·확인창 닫힘·실제 Layer가 배치/전투. 명령 대기·재생 중·적 턴은 막지 않는다. 막힌 동안의 휠은 버리고 누른 키 방향은 남긴다.
- 휠은 값의 크기를 보지 않고 한 번 = 한 칸, 위 = 줌인이다(P1). 연출 줌(포효·기차) 중에는 창구가 거절해 버린다(Q-CC6 A).
- 가장자리 이동은 포인터가 창 안·가장자리 띠(두께 × 화면 짧은 변) 안·UI 위가 아닐 때만이다(Q-CC5 A). 창 포커스를 잃으면 키·휠·마지막 포인터를 모두 잊는다.
- 이동 속도·제한·줌 범위는 스테이지마다 View DB `StageViewPropData.cameraSettings`(`CombatCameraSettings_Default`)이고, 처음 보기 크기는 맵 프리팹 카메라 렌즈값이다. 키 재설정 목록(`StageInputKeyBindings`)에는 넣지 않았다(Q-CC7 A).
