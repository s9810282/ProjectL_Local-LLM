# PositionSwap 이펙트 선행·시간 후 동시 교환 Task 계획 (2026-11-02)

- 설계 원본: [동시 자리 교환 설계](../specs/2026-11-02-position-swap-delayed-teleport-design.md).
- 상태: **Unity 컴파일·교환 미리보기 검증 완료. 기존 EditMode 5,619건 중 5,607 통과·12 실패, PositionSwap 관련 32건 통과. 실제 전투 교환 입력은 샷건 배치 직후 에디터 네이티브 크래시로 미확인.** 자세한 증거와 공백은 마지막 실행 기록을 따른다.
- 사용자 요구: 이동 확정 → 샷건 모션 없이 이펙트 시작 → 설정 시간 대기 → 두 유닛 동시 교환·이펙트 제거.
- 기존 Core StaleContext 작업/인덱스와 다른 작업자의 클립·다중 발사 작업은 변경하지 않는다. 새 문서는 스테이징하지 않는다.
- 새 시험/기존 시험 검증 내용 변경·MSBuild·커밋·push 없음. Phase마다 멈춰 보고한다. 2026-11-02 사용자 **“실행 및 검증 까지 진행해”**의 명시적 실행 지시를 받아 PS3에서는 Unity GUI 컴파일·재생·기존 시험 실행을 진행했다.

## PS0 — 설계·Task (완료)

- [x] 현재 이동자 선행 걷기·승객 보간·행동 종료 회수 경로를 확인했다.
- [x] 자연 파티클 종료 대신 View 대기시간 한 값을 완료 기준으로 정리했다.
- [x] 두 유닛의 원래 위치 유지·동시 최종 사실 반영·즉시 이펙트 제거 요구를 명시했다.
- [x] 여러 칸의 선행 걸음, 중간 환경/피해 사건, 접촉 Gate·방향 복귀·되돌리기, 취소·세대 경계를 기록했다.
- [x] 기존 쌍 계획/보류 저장소/ShownFacts/조각 실행/이펙트 수명을 재사용하는 방향과 8기준 구조 검토를 작성했다.
- [x] 사용자 에셋 편집 및 다른 작업자의 클립/다중 발사 수정과 겹치는 파일을 확인했다. 이번 단계에서는 문서 두 개만 추가한다.

## PS1 — 전달 계약·기능 경계 및 사전 대조 (완료, 컴파일 미확인)

- [x] PS1.1 구현 전 최신 배치/순차 재생·접촉 Gate·표시 사실 갱신 경로를 대조한다. PositionSwap이 실제 발동한 명령의 선행 걸음을 식별하고 모션 생략 중에도 원래 사건/완료 순서가 진행되는지 확정한다.
- [x] PS1.2 PositionSwap 전용 View 입력/불변 값과 선택 설정을 추가하고 WaitSeconds의 유한·0 이상 검사, 다른 특성/발동 종류·기존 Relocate 동시 설정의 오류 검사를 정한다.
- [x] PS1.3 기존 CombatActivationRows로 같은 설정을 전달한다. 쌍 식별은 CombatPlacementBatchPlan을 확장하고 다른 위치에 복제하지 않는다.
- [x] PS1.4 전용 CombatPositionSwapPresentation 협력 객체와 기존 조각 수명을 연결한다. 생성/주입은 SceneEventPresenter 조립 지점으로 제한한다.
- [x] PS1.5 이펙트 즉시 회수 API의 좁은 확장과 재사용 번호/해제 경로를 확인한다. 기존 Recall/일반 큐의 수명은 바꾸지 않는다.
- [x] PS1.6 변경 파일·기존 API/설정 호환·LF/UTF-8·신규 meta를 검토하고 멈춰 보고한다. 컴파일 미확인.

사전 대조의 대상은 설계 §3~5가 정의한 기존 파일과 직접 소비자다. 새로운 공용 실행기/프레임워크·Core 변경이나 다른 작업자 파일 수정이 필요하면 이 Phase에서 구현하지 않고 설계/범위를 먼저 확인한다.

## PS2 — 표시 보류·대기·동시 반영 통합 (코드 완료, 컴파일 미확인)

- [x] PS2.1 양쪽 위치와 실제 교환 명령의 선행 모션을 보류한다. 일반 이동/교환 미성립 결과·중간 사건은 기존 의미로 소비한다.
- [x] PS2.2 ForPresentation/ApplyCharacter 및 ReleaseUnclaimedPlacements와 연결해 HP/상태 갱신이 위치를 먼저 적용하거나 보류를 버리지 않도록 한다.
- [x] PS2.3 MovementChanged에서 발동 피드백을 한 번 재생하고 설정 시간 동안 기존 실행기에서 기다린다. 후속 사건/결과 끝은 대기 완료를 기다린다.
- [x] PS2.4 완료 한 지점에서 두 Final을 함께 반영·Render 1회·해당 발동 이펙트 즉시 회수·사건 완료 1회를 실행한다. 걷기/보간/방향 복귀가 섞이지 않도록 한다.
- [x] PS2.4a 방어막·버프/디버프 HUD/유지 이펙트는 기존 상태 반영/동기화 경로에 둔다. 교환 발동 이펙트의 회수가 기존 상태 이펙트를 지우지 않는지, 전체 상태 스냅샷이 보류 위치를 먼저 적용하지 않는지 확인한다. 상태 사건의 소비 대기와 상태 표시 규칙 변경을 구분한다.
- [x] PS2.5 취소·씬 해제·세대 변경·시작 실패에서 자원과 보류를 정리한다. 이미 반환/재사용된 이펙트와 늦은 완료는 다른 발동에 영향을 주지 않는다.
- [x] PS2.6 최신 PositionSwap 에셋의 사용자 변경을 유지하며 새 설정을 연결한다. 초기 대기 0.5초 제안, 기존 Relocate/개별 시간 끝의 중복 시계 제거를 명시적으로 반영한다.
- [x] PS2.7 정적 순서/회수/기존 기능 diff를 검토하고 인계를 기록한 뒤 멈춰 보고한다. 컴파일 미확인.

## PS3 — 정적 인수·Unity 실행 검증 (일부 완료, 실제 전투 교환 미확인)

- [x] 운영 샷건의 PositionSwap 특성 입력, View 표의 에셋 GUID 연결과 활성화/0.5초 설정을 정적으로 대조했다.
- [x] 기존 Scenario_PositionSwapRestore의 합성 결과 계약을 대조하고, 선택적 CommandKind/ActorId에 의존하던 교환 식별을 원본 Pair/발동 기준으로 보완했다.
- [x] 기존 조립 미리보기의 여러 칸 경유 쌍에서 선행 걷기 없이 기존 두 위치의 이펙트가 시작되고, 대기 후 두 위치가 교환되며 전용 이펙트가 사라지는 것을 관찰했다. a의 창 입력만 key 11(샷건)로 선택했으며 프리셋 에셋은 수정하지 않았다.
- [x] 미리보기에서 0초 즉시 완료, 기본 0.5초, 관찰용 5초를 재생했다. 기본 0.5초의 활성 이펙트/교환 완료 화면도 각각 확인했다. 프레임 단위 계측 증거와는 구분한다.
- [ ] 실제 전투 입력에서 인접/여러 칸 경로·방향 복귀·같은 프레임 교환을 확인한다. 현재 실제 전투는 샷건 배치 직후 네이티브 렌더 크래시로 이동 입력 전에 중단됐다.
- [x] 기존 미리보기의 두 참여자 Returned 쌍 복원 및 재생 완료를 확인했다.
- [ ] 일반 이동·교환 미성립/이동 중단·넉백/끌어오기·기존 Relocate·쌍 되돌리기가 유지되는지.
- [ ] 중간 지형/피해/사망·교환 도착의 방어막/환경 사건을 건너뛰지 않는지.
- [ ] 기존 방어막·버프/디버프 유지 이펙트가 교환 뒤에도 상태에 따라 유지/해제되고, 교환 전용 이펙트만 종료되는지. 환경의 상태 효과와 지뢰/지형 사건을 구분해 확인한다.
- [x] 미리보기 30초 대기 중 중단·초기화를 실행해 이펙트 제거/초기 위치 복원을 확인했고, 추가 31초 뒤에도 늦은 교환이 없었다. 이후 0초와 기본 0.5초 재생도 완료했다.
- [ ] 실제 씬 이탈·다음 전투 결과/연속 교환의 입력 해제는 남았다. 미리보기 환경 종료에서는 별도 Setting UI 미등록 오류를 발견했다.
- [x] Unity 컴파일/재생 및 기존 시험 결과를 실제 범위대로 기록했다. View 시험 작성·기존 시험 수정 없음.
- [x] DataBaseAssetTests 76건을 실행했다(69 통과·7 실패). 이어 기존 EditMode 전체 5,619건도 실행했다(5,607 통과·12 실패). 실패를 고치거나 판정을 완화하지 않았다.

### 실행 절차와 남은 확인 순서

1. Unity 스크립트 컴파일 성공과 Play 진입은 확인했다. 아래 절차 중 실제 실행한 범위/결과는 마지막 기록을 따른다.
2. `ProjectL > Combat > Trait Activation Preview`에서 **조립 재생**을 선택하고, **프리셋 빠른 불러오기 → Scenario_PositionSwapRestore**를 고른 뒤 **구성 확인**, 재생을 진행한다. 기존 프리셋은 a=(0,0), b=(2,0), a의 경유 칸=(1,0), 교환/두 참여자 복원을 명시한다. a가 경유 칸까지 걷지 않고 원래 두 위치에서 이펙트가 시작되며, 0.5초 뒤 a=(2,0)/b=(0,0)와 이펙트 제거가 함께 일어나는지 본다. 뒤의 복원은 기존 즉시 쌍 복원 경로다.
3. 위 프리셋은 key 10의 합성 연출이므로 실제 전투에서 **샷건**의 인접/여러 칸 이동 확정도 확인한다. 기존 유지 이펙트가 있는 상태, 교환 후 방어막, 중간 환경 사건, 교환 미성립/중단과 일반 이동·넉백·끌어오기·되돌리기를 함께 확인한다. 합성 프리셋은 Core 발동 조건/계산 검증의 증거로 쓰지 않는다.
4. `TraitVisual_PositionSwap.asset > traitVisualData > activations > MovementChanged > positionSwap > waitSeconds`를 0 / 0.5 / 다른 값으로 바꾸어 확인한다. 발동 줄 값은 연결 동안 캐시하므로 값을 바꾼 뒤 새 전투 또는 미리보기 Play 모드 재진입으로 확인한다. 0초는 이펙트 유지 프레임 없이 즉시 교환할 수 있다. 확인 후 기본 0.5초로 돌린다.
5. 0.5초 대기 중 재생 중지/씬 이탈을 하고 다시 재생해, 남은 이펙트·늦은 자리 교환·입력 조기 해제가 없는지 본다. 기존 `DataBaseAssetTests`도 Unity에서 실행하고 결과를 기록한다.

기존 단계는 `AGENTS.md`의 “컴파일·재생 확인은 사용자가 Unity에서” 지시를 따라 실행하지 않았다. 이번 PS3 실행은 일반적인 “진행해”가 아닌 사용자 “실행 및 검증 까지 진행해”의 명시적 실행 지시에 따른 것이다. 아래의 과거 “컴파일 미확인” 문구는 해당 단계 종료 당시 기록이다.

## 실행 기록

- 2026-11-02: 사용자 4단계 요구를 받아 현재 코드의 동시 큐+승객 이동과 다른 시간 기준을 정리했다. 고정 대기 완료가 위치 교환과 이펙트 제거를 함께 지시하므로 자연 종료 대기나 이펙트 시간 추정이 필요하지 않다.
- 이번 단계는 위 설계와 이 계획 두 문서만 작성했다. 실행 코드·에셋·기존 인덱스·Core 전달 묶음은 수정하지 않았으며 리뷰 지점에서 멈춘다. **컴파일 미확인.**
- 후속 사용자 확인: 방어막·환경 효과를 통째로 교환 완료에 묶은 설명을 정정했다. 상태 기반 HUD/유지 이펙트는 기존 상태 읽기/동기화 경로이고 PositionSwap 전용 이펙트와 수명이 다르다. 상태 사건 소비의 대기로 인한 시점 차이와 표시 규칙 변경을 구분하도록 설계 §4와 PS2/PS3 확인 항목을 보완했다. 실행 코드·에셋 변경 없음, 컴파일 미확인.

### 2026-11-02 PS1 결과

사용자 “오케이 시작해”에 따라 PS1만 구현했다. 새 선택 설정은 기본 비활성이며 PositionSwap 에셋은 수정하지 않았다. **PS1 종료 당시 화면은 기존 이동/Relocate 방식이었다. 이후 PS2에서 새 네 단계 동작과 에셋을 연결했다.**

| 변경 파일 | 내용 |
|---|---|
| `DataBaseSystem/Data/View/CombatPresentation/CombatPositionSwapVisualInput.cs`·`CombatPositionSwapVisualData.cs` (신규, meta 포함) | 선택 활성화·대기 0.5초 기본 입력, 유한한 0 이상 불변 값 |
| `DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs` | PositionSwap/MovementChanged/ActivationStart 전용 검사. 유닛 행동/추가 공격과 병용 금지, 이펙트 지연 0·끝 UnitAction 검사 |
| `Combat/Playback/Support/CombatActivationRows.cs` | 같은 발동 줄 캐시에 선택 값을 전달. 기존 호출은 선택 인자의 null 기본값 유지 |
| `Combat/Playback/Support/CombatPlacementBatchPlan.cs` | 기존 Pair 원본에 발동 사건/목록 번호·행위자의 선행 걸음을 연결. 쌍·출처·두 대상·경로 시작점과 승객 도착점의 불일치/누락/중복을 검출 |
| `Combat/Playback/Support/CombatPositionSwapPresentation.cs` (신규, meta 포함) | 현재 결과 대응 조회와 기존 행동 조각 실행기를 사용하는 대기 조립. Prepare/Clear 수명 연결, 실제 걸음/발동 소비 연결은 PS2 |
| `Combat/Presentation/Pieces/CombatPositionSwapDelayPiece.cs` (신규, meta 포함) | 설정 시간 한 값만 기다리는 기존 ICombatPresentationPiece 구현. 0초는 Begin 안에서 끝남 |
| `Combat/Playback/Support/CombatMovementPresentation.cs`·`Combat/Playback/CombatSceneEventPresenter.cs` | 조립 지점에서 쌍 계획 한 번 생성 후 공유. 기존 Prepare(CombatResult) 호출도 유지 |
| `Combat/Presentation/Effects/CombatEffectHandle.cs`·`CombatEffectFollower.cs` | 별도 RecallImmediately 추가. 같은 사용 번호의 잔여 입자를 기존 StopNow/멈춤 알림 경로로 정리하고 기존 Recall은 유지 |

표의 처음 세 경로는 `Assets/ProjectL/Script/Systems/` 기준, `Combat/` 경로는 그 아래 `RuntimeSystem/InGameSystem/ViewSystem/` 기준이다. 기존 파일 7개·신규 C# 4개와 meta 4개, 이 설계/계획 문서 2개가 이번 범위다. TraitActivationVisual의 다른 작업자 Cue2/Cue3 검사 추가는 유지했다.

사전 대조 결론:

- Core는 일반 자발 걸음의 MovementCause를 비울 수 있다. 기존 CombatMovementKinds.Of/IsSelfDriven으로 선행 걸음을 식별해 판정을 복제하지 않았다. 교환 쌍/발동이 없는 결과는 전용 대응이 비어 있으며 일반 이동 경로를 사용한다.
- 접촉 Gate는 실제 생성된 이동 레인이 소유한다. PS2에서 해당 걸음의 완료를 모션 없이 전하면 레인을 만들지 않으며 원래 중간 사건은 남는다. BeginMovement 소리·EndMovement 방향 복귀도 함께 생략해야 하므로 PS2.1/PS2.4에서 연결한다.
- MovementChanged 뒤 방어막/도착 환경 사건은 전용 대기가 사건 완료를 보류하면 그 뒤 소비한다. 앞선 중간 지형·피해·상태 사건은 순서를 유지한다. 상태 기반 HUD/유지 이펙트의 적용 규칙은 바꾸지 않는다.
- ApplyCharacter가 현재 보류 배치를 버리고 전체 위치도 반영하므로, HP/상태 갱신 중 표시 위치를 지키는 처리는 PS2.2에 남아 있다. PS1은 이 경로를 수정하거나 새 설정을 활성화하지 않았다.

구조 검토: Core/Buffer를 수정하지 않았고 쌍·이동 분류·값 검사·캐시·실행 세대·풀 반환의 기존 원본을 재사용했다. 위치 교환 협력 객체는 결과 대응/대기 조립만 소유하며 SceneEventPresenter는 생성과 수명 연결을 맡는다(SRP/DIP). 기존 호출·인터페이스/기본 설정은 유지하고 선택 입력과 ICombatPresentationPiece로 확장했다(OCP/LSP/ISP). 별도 서비스·Update·코루틴·전역 Tick 검사는 추가하지 않았다. FW-011/012의 효과 멈춤 알림 경로와 입력 검증을 유지했다.

정적 diff/공백 검사·직접 소비자/호출 계약을 대조했다. 신규 meta GUID 4개는 Assets/Packages에서 각 1개이고, 이번 17개 파일의 UTF-8(BOM 없음)/LF·대체 문자·후행 공백 검사를 통과했다. 이번 파일의 인덱스 변경도 없다. **새 시험 작성/기존 시험 수정·MSBuild·Unity 컴파일/재생은 수행하지 않았다. 컴파일 미확인.** 스테이징·커밋·push 없음. PS1 리뷰 지점에서 멈추며, 다음은 PS2다.

### 2026-11-02 PS2 결과

사용자 “다음 진행해”에 따라 PS2를 구현했다. 현재 코드/에셋의 재생 순서는 **이동 확정 → 선행 걷기 없이 기존 두 위치에서 발동 피드백 → 0.5초 대기 → 원본 두 Final 동시 반영·교환 발동 이펙트 즉시 회수 → 후속 사건**이다. 화면 동작은 사용자 Unity 확인 전이다.

| 변경 파일 | PS2 내용 |
|---|---|
| `Combat/Playback/Support/CombatDeferredPlacements.cs` | 기존 보류 저장소의 항목에 위치 보존 여부를 넣었다. 두 배치를 함께 가져가기/꺼내기를 추가하고 개별 반영·되돌려 놓기로 쌍을 풀지 못하게 했다. 기존 Relocate 항목은 위치 보존 모드를 사용하지 않는다. |
| `Combat/Playback/Support/CombatShownFacts.cs` | 첫 선행 걸음부터 두 Final을 보류한다. HP/상태 반영 시 위치·회전·점유 칸만 현재 표시 값으로 보존하며, HP·상태·행동 값은 사건 값을 사용한다. 완료는 두 보류를 검사/꺼낸 뒤 둘 다 적용하고 교환 그리기를 한 번 호출한다. |
| `Combat/Playback/Support/CombatPositionSwapPresentation.cs` | 원본 걸음 대응으로 모션을 생략하고, 발동 피드백·기존 조각 실행기의 고정 대기·두 배치 반영·그 발동 이펙트 종료·사건 완료를 연결했다. 별도 위치 사본이나 완료 세대를 갖지 않는다. |
| `Combat/Playback/Support/CombatMovementPresentation.cs` | 전용 대응이 있는 걸음만 즉시 완료한다. 이동 소리 시작과 경로 끝 방향 복귀도 생략한다. 대응이 없는 일반 이동/밀림/기존 위치 교환·복귀는 기존 분기로 간다. |
| `Combat/Playback/Handlers/CombatTraitEventHandler.cs` | 발동 목록에서 전용 교환만 협력 객체로 넘긴다. 그 발동의 기존 큐/Relocate 경로를 중복 실행하지 않으며, 다른 발동은 기존 루프를 유지한다. |
| `Combat/Playback/CombatSceneEventPresenter.cs` | 같은 피드백/표시 사실/실행기/세대 읽기를 조립해 주입했다. 취소는 공용 CleanupErrors 묶음 안에서 교환 이펙트를 회수해 실패 시에도 나머지 정리를 계속한다. |
| `Combat/Presentation/Effects/CombatEffectActionGroup.cs` | 기본 회수는 기존 Recall이다. 전용 교환은 즉시 회수 모드를 고르고 그 발동의 손잡이만 거둔다. 정상 완료/시작 실패/취소와 늦은 닫기의 중복 회수를 막고, 한 회수 실패가 다른 손잡이 회수를 막지 않도록 오류를 모은다. |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Heavy/TraitVisual_PositionSwap.asset` | `positionSwap.enabled: 1`, `waitSeconds: 0.5`, `unitActions: []`, 이펙트 `end: UnitAction(2)`, `endSeconds: 0`. 사용자 편집 중이던 effect 9/대상 몸 자리·보정·텍스트·상시/흔적 설정은 유지했다. |

위 `Combat/` 경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/` 기준이다. PS2는 C# 7개·View 에셋 1개·설계/계획 2개를 수정했다. 신규 파일/meta는 없다. 다른 작업자의 클립/다중 발사 파일·Core·FieldCamera·Architecture는 수정하지 않았다.

정적 순서/수명 대조:

- **인접·여러 칸:** Pair를 다시 찾지 않고 PS1 대응을 사용한다. 첫 행위자 걸음에서 두 배치를 claimed 상태로 등록하므로 중간 사건의 ReleaseUnclaimedPlacements가 먼저 반영하지 않는다. 해당 걸음의 이동 레인이 없어 접촉 Gate/보간/걸음 소리도 열리지 않으며 EndMovement는 완료만 전한다.
- **HP/상태:** ForPresentation은 피해/정규화/사망 표현의 위치만 보존하고 근원·원인 메타데이터는 유지한다. ApplyCharacter도 같은 표시 위치를 사용하고 교환 보류는 버리지 않는다. 실제 교환이 성립한 필드 참여자만 이 모드로 들어간다. 교환 미성립/이동 중 사망·중단 결과에는 대응 쌍이 없어 기존 재생을 한다.
- **0초·정상 완료:** 대기는 기존 조각 한 개뿐이다. 0초는 Begin에서 바로 끝나고, 기다림 항목이 끝나면 기존 실행기가 자신을 제거한 뒤 한 번 완료한다. 완료 콜백은 두 Final 반영/Render 1회와 발동 그룹 닫기를 모두 시도한 후 오류가 없고 세대가 같을 때만 사건 완료를 전한다.
- **방어막·환경:** 발동 피드백은 기존 Start 한 곳을 사용한다. 전용 그룹은 해당 발동의 UnitAction 큐 손잡이만 소유한다. 상태 유지 이펙트·HUD는 기존 Render/Sync와 상태 읽기 경로를 유지한다. MovementChanged는 주체 없는 장벽 사건이므로 후속 방어막/도착 환경 사건은 대기 완료 뒤 소비하고, 앞선 중간 사건은 순서를 바꾸지 않는다.
- **취소·실패:** 취소 시 먼저 기존 보류를 비우고, 교환 그룹/실행기/나머지 표시/이펙트 창구 정리를 계속한다. 시작 실패는 그 쌍의 보류와 그 그룹을 정리하고 오류를 전한다. 기존 조각 실행기의 세대/완료 1회 계약과 Handle의 사용 번호 검사를 사용하므로 취소 뒤 정상 완료나 재사용된 다른 이펙트 회수가 일어나지 않게 했다.

구조 검토:

- **Core·SSOT:** 계산/기록/Buffer 변경 없음. 최종 값은 원본 Pair, 현재 위치는 ShownFacts, 보류는 DeferredPlacements, 시간은 WaitSeconds, 세대는 Presenter, 반환은 기존 Handle/Follower/Scope 경로가 원본이다. 별도 세계/상태 표시 규칙을 만들지 않았다.
- **SOLID·캡슐화:** 조립은 Presenter, 전용 순서/종료는 PositionSwap 협력 객체, 표시 사실 반영은 ShownFacts로 나뉜다(SRP/DIP). 내부 대응 조회는 private이고 외부에는 걸음 생략/발동 시작만 제공한다(ISP). 기존 인터페이스와 기본 모드/소비자를 유지해 전용 선택 설정으로 확장했다(OCP/LSP). 공유 저장소·그룹 변경은 일반 Relocate/UnitAction 회귀의 사용자 확인 항목에 포함한다.
- **기능 독립성·FW·Unity:** 클립/다중 발사·FieldCamera 구현을 수정하지 않고 기존 조각/이펙트 계약으로 연결했다. 새 서비스/Builder 등록·전역 이벤트·UI 등록 단위·MonoBehaviour.Update·코루틴·전역 Tick 검사는 없다. 기존 조각의 Tick만 대기시간을 센다. 이펙트를 직접 Destroy/외부 Pool 반환하지 않고 FW-011의 기존 멈춤 알림 경로를 사용한다.

정적 검토 결과: PS1/PS2 합계 22개 파일의 UTF-8(BOM 없음)/LF·대체 문자 검사와 코드/문서 후행 공백·diff 검사를 통과했다. 신규 meta GUID 4개는 Assets/Packages에서 각 1개다. 기존 Unity 에셋 직렬화의 빈 값 뒤 공백은 유지했다. 직접 소비자/생성자 호출·장벽/조각 완료/취소·개별/쌍 배치 분기를 대조했고 기존 시험 코드는 수정하지 않았다. 이번 파일의 인덱스 변경은 없다.

새 시험 작성/기존 시험 수정·MSBuild·Unity 컴파일/재생·스테이징·커밋·push는 수행하지 않았다. **컴파일 미확인.** 남은 단계는 위 PS3의 사용자 Unity 컴파일/재생 및 DataBaseAssetTests 확인이다.

### 2026-11-02 PS3 정적 인수 및 호환 보완

사용자 “다음 진행해”에 따라 남은 실행 확인 준비와 직접 소비자 대조를 진행했다. 이번 코드 변경은 기존 `CombatPlacementBatchPlan.cs` 한 파일이고, 새 시험·미리보기 구현·Core·에셋은 수정하지 않았다.

기존 `PreviewScenarioCompiler`는 교환 사실이 포함된 조립 묶음에도 `CommandKind.None`/빈 `ActorId`를 사용한다. PS1에서 추가했던 Move/ActorId 필수 검사는 이 결과를 거절할 수 있었다. 명령 메타데이터를 전제로 삼은 부분을 제거하고, 기존 Pair의 첫 참여자 ID와 대응 발동 SourceUnitId/TargetIds로 선행 자발 걸음을 고르게 했다. 쌍 탐색을 복제하거나 미리보기 분기/전용 플래그를 운영 코드에 추가하지 않았다. 발동 누락/중복·다른 출처/대상·선행 경로 시작점/승객 도착점 불일치 검사는 유지한다. 다른 행위자의 걸음을 잘못 생략하지 않도록 해당 ID의 경로만 선택하고 소비한다.

기존 `Scenario_PositionSwapRestore`의 경유 걸음 → 자발/강제 교환 쌍 → 두 대상을 가진 MovementChanged와 뒤의 Returned 쌍을 소스/에셋 값으로 대조했다. 운영 `Unit_ShotGun.asset`에는 PositionSwapTraitAuthoring이 있고, TraitVisual 표의 GUID는 활성화된 PositionSwap 외형 에셋을 가리킨다. 해당 프리셋과 메뉴/입력 계약으로 위 사용자 확인 절차를 구체화했다. 실행해 통과한 증거는 아니다.

**PS3의 Unity 컴파일·재생·DataBaseAssetTests는 미확인이다.** 사용자 결과를 받기 전에는 PS3/전체 검증 완료로 처리하지 않는다. 스테이징·커밋·push 없음.

### 2026-11-02 PS3 Unity 실행 결과

사용자 **“실행 및 검증 까지 진행해”**에 따라 열린 Unity 6000.0.73f1에서 GUI로 Refresh·Play·Test Runner를 실행했다. 새 시험 작성/기존 시험 변경·MSBuild·Core/FieldCamera/Architecture 수정·스테이징·커밋·push는 하지 않았다.

- **컴파일 확인:** 최신 소스 이후 Unity 재컴파일 성공(`Tundra build success`, 7.59초·33개 갱신)과 Play 실행을 확인했다. `ProjectL.Systems.dll`은 UTC 2026-11-01 22:55:08, 마지막 코드 보완 파일은 22:20:54다. 로그의 그 이전 단계 CS7036은 최신 성공 전 기록이며 현재 컴파일 실패로 집계하지 않는다.
- **교환 미리보기:** 기존 `Scenario_PositionSwapRestore`를 사용해 a=(0,0), b=(2,0), 경유=(1,0)에서 선행 모션 생략·두 원래 위치의 VFX·대기 후 교환/해당 VFX 제거를 관찰했다. 창의 a Key만 `Unit_Shot Gun`으로 선택했고 프리셋/씬 파일은 변경하지 않았다. 0초/기본 0.5초/5초, 교환 후 Returned 쌍 복원, 취소 후 재생을 확인했다. 30초 대기 중 취소한 뒤 추가 31초 관찰에서도 늦은 교환은 없었다. 임시 대기시간 변경은 0.5초로 복구했고 에셋 SHA-256은 검증 전과 같은 `523D25F9B62316DC2FECACC9669C778056E5908B98722B3F27A3EA873994758A`다.
- **기존 시험:** DataBaseAssetTests는 76건 중 69 통과·7 실패. 전체 EditMode는 81.33초, **5,619건 중 5,607 통과·12 실패·건너뜀 0**. PositionSwapMovementTests 15, UndoTests 10, AuthoringTests 1, PublicationTests 6, 합계 **32건 전부 통과**다. 이 Core 시험의 통과를 View 프레임/실제 입력 검증으로 대신하지 않는다.
- **실제 전투:** 기존 PC의 Play From Boot 설정(운영 DB, PlacedStage `Test`/10000, Loadout 1)으로 Combat 진입하고 덱에서 샷건(와이엇)을 배치했다. 배치 직후 Unity가 **`SharedMaterialData::UpdatePerMaterialCB → Material::AcquireSharedMaterialData → BaseRenderer::FlattenSharedMaterialData → URP Cull`** 네이티브 스택으로 크래시했다. 교환 명령 입력 전이므로 실제 전투 교환/방어막·유지 이펙트/중간 환경 사건/일반 강제 이동 재생은 아직 미확인이다. 이 스택만으로 크래시 원인을 이번 변경 또는 다른 특정 기능으로 단정하지 않는다.
- **별도 오류:** 미리보기 환경 종료에서 `OptionInputRouter.Shutdown → UISystem.CloseUI(Setting)`의 미등록 오류가 발생했다. 전체 시험 이후 실제 Play 진입에는 `Invalid editor window of type: FbxPlacementWindow`도 발생했다. 해당 에디터 오류의 자동 정지를 해제해 Combat 진입을 확인했으나, 뒤의 크래시로 Error Pause 설정을 UI에서 되돌리지 못했다. 에디터 재개 시 원래 켜짐으로 복원해야 한다.
- **복구 제한:** 크래시 뒤 Bug Reporter/Unity Hub 창 접근은 `Computer Use app approval timed out`으로 끝났다. 자동 승인 심사 거절이 아니라 추가 앱 접근 승인 시간 초과이며, 버그 보고를 외부로 제출하지 않았다. Unity를 다시 연 뒤 남은 실제 전투 항목을 확인해야 한다.

전체 실패 12건은 다음과 같다. 앞선 `2026-11-02-movement-profile-read-tracking/editmode-results.xml`에도 같은 이름으로 실패한 것은 8건이다. 비교 결과가 없는 나머지 4건을 이번 View 변경의 회귀 또는 기존 실패라고 단정하지 않는다.

| 실패 묶음 | 건수 | 실제 결과 |
|---|---:|---|
| FbxPlacementTests의 PrefabCatalog | 1 | EditorWindow.Close에서 NullReferenceException, 이전에도 실패 |
| TraitActivationOutcomeTests의 Taunt | 1 | 발동 수 기대 1/실제 2, 이전에도 실패 |
| CombatCameraZoomPoseTests | 1 | CinemachineCameraOffset 타입 로드 실패, 이전에도 실패 |
| DataBaseAssetTests | 7 | 특성 배정/아킴보 입력, StageKey 1101 누락 2건, 캐논 성장 이력 2건, 휴식 선택지 불일치. 캐논 2건 외 5건은 이전에도 실패 |
| RunGoldFacilityTests | 2 | 휴식 선택지 기대/실제 불일치, 이전 실패 목록에는 없음 |

증거는 저장소의 무시 경로 `Logs/CodexVerification/2026-11-02-position-swap-delayed-teleport/`에 보존했다: `editmode-results.xml`, `database-asset-tests.xml`, `test-failures.json`, `verification-summary.json`, `native-render-crash.txt`, 임시 설정 복구용 원본 YAML. 실제 전투 검증 공백과 실패가 남아 있으므로 전체 통과/PS3 완전 완료로 표시하지 않는다.

### 2026-11-02 Inspector 대기시간 표시 누락 보완

사용자 “Wait Seconds가 어딨음” 확인으로 `TraitActivationVisualDrawer`의 명시적 표시 목록에 새 `positionSwap` 필드가 빠져 있음을 발견했다. 앞서 일반 Inspector에서 편집할 수 있다고 안내한 것은 잘못이었다. `CombatBodyTextDrawer.cs`의 기존 목록에 해당 필드를 추가해 높이 계산과 그리기가 같은 목록으로 반영되게 했다. 설정 값·런타임·Core·검증 판정은 변경하지 않았다.

스크립트 반영 뒤 `TraitVisual_PositionSwap.asset`의 **Trait Visual Data → Activations → Element 0(Kind: MovementChanged) → Position Swap → Wait Seconds**에서 편집한다. Position Swap은 Unit Actions 다음에 있고, Enabled는 기존 에셋에서 켜져 있으며 Wait Seconds는 0.5다. 위의 Unity 실행·시험 결과는 이 Editor 표시 보완 전 결과다. 이번 보완은 정적 필드 연결·diff 검사를 했으며 **컴파일 미확인, Inspector 실제 표시 미확인**이다. 새 시험·MSBuild·스테이징·커밋·push 없음.
