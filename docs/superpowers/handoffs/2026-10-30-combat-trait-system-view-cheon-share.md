# 전투 특성 시스템 View 대응 — 천명님 공유 (2026-10-30, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 천명님.
> 상태: View 쪽 코드·에셋 완료(TS-a~TS-f5), **Unity 컴파일 확인(사용자)**, 재생 확인은 일부 대기, 커밋은 TS-a~TS-e까지(`034b0b26`)이고 TS-f는 커밋 전.
> 관련 문서: [Core → View 인계](2026-10-28-trait-system-core-view-handoff.md) §9 · [View 설계](../specs/2026-10-29-combat-trait-system-view-design.md) · [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) · [View 계획 §7](../plans/2026-10-30-combat-trait-system-view-plan.md) · Core 전달 문서 §5.59·§5.61~§5.64·§53·§54

## 요약

- Core 파일은 이번 묶음에서 바꾸지 않았습니다. 병합(`6b0755be2`) 때 충돌 난 `CombatMovementCalculator` 하나만 두 쪽을 합쳤습니다(§3).
- 인계 §9의 질문에 답하는 View 대응을 넣었습니다. 추가 공격·이동 원인·흔적·상태 사건·대신 맞기를 모두 Core 사실 칸으로 읽습니다(§5).
- 천명님 줄 둘(§1)과 천명님 시험 두 줄(§2)을 맞췄습니다. 시험이 확인하는 내용은 그대로입니다.
- 이번 묶음의 새 요청은 없습니다. 표식 줄은 예고하신 공통 버프·디버프 계약이 오면 그 계약으로 옮깁니다(§4).
- 따로 전달할 요청: 대신 맞은 공격의 넉백(Core 전달 문서 §53, 사용자가 나중에 전달), 걸린 상태 공통 목록(§49 R5 보강·Q14).

## 1. 바꾼 천명님 줄 (둘)

| 자리 | 바꾼 것 | 왜 |
|---|---|---|
| `ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs` 표식 갈래(`4bd6b574`, 지금 `:51`~`:58`) | 새 `CharacterDebuffState`를 만들 때 디버프 **여섯 칸**을 모두 옮기고 표식만 바꿈(이름 있는 인자) | 병합 직후 이 줄이 셋(위압·방어 감소·표식)만 넘겨, 표식 사건이 오면 새 디버프 셋(무장 해제·발목 꺾기·방어구 파괴)이 HUD에서 사라졌습니다. 인계 §5 "HuntersMark-only update must preserve the other five"대로 고쳤습니다. |
| `ViewSystem/Combat/Playback/Ordering/OrderedCombatPresentation.cs` 클래스 선언 줄 | `: ICombatPresentation` → `: ICombatPresentation, ICombatSourceSettlement` | 대신 맞은 수호자가 "그 공격의 맞는 순간 뒤 자기 후속(피격·사망)이 모두 끝났나"를 순차 재생기에 묻습니다(유닛 행동 설계 AA5). 질문 본문 `IsSettled`와 연결 한 줄은 우리 줄입니다. |

## 2. 바꾼 천명님 시험 두 줄 (`Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs`)

| 자리 | 바꾼 것 | 왜 |
|---|---|---|
| `:271` 시험 가짜 `Playback` | `public void BindSettlement(ICombatSourceSettlement settlement) => _presenter.BindSettlement(settlement);` 한 줄 | 재생 계약 `ICombatEventPlayback`에 연결 함수 하나가 생겼습니다(Core 전달 문서 §5.61). |
| `:446` 필드 창구 생성 | 인자 `(_, _) => { }` 하나(흔적 맞춤 `showTrail`, 지형 바뀜 다음 자리) | 필드 창구 `CombatFieldEventPorts`에 필수 칸 하나가 생겼습니다(특성 흔적 표시, 설계 TS7). |

- 두 줄 모두 컴파일 맞춤만입니다(사용자가 Unity에서 컴파일 확인).
- 부탁: 브랜치에서 이 시험을 고치셨다면 병합 때 위 두 꼴을 남겨 주세요.

## 3. 병합 때 합친 Core 줄 — `Application/Combat/CombatMovementCalculator.cs`

- 충돌: 천명님 쪽은 이동력 `CombatActionAdmission.MovementAllowance(input, actor)`와 칸 검사 둘(`MovementRouteValidator.CanTraverse` = 지나갈 수 있나, `CanEnter` = 멈출 수 있나)로 나눴고, 우리 쪽은 너비 우선 탐색을 `Reach` 하나로 꺼내 유닛 호버 이동 칸 `UnitMoveCells`가 같이 쓰게 했습니다(Core 전달 문서 §5.59).
- 합친 꼴: `Reach`가 `CanTraverse`로 지나갈 수 있는 칸까지 넓히고, 행동 범위 `Range`와 `UnitMoveCells`가 그중 거리가 있고 `CanEnter`인 칸만 남깁니다. 이동력은 둘 다 `MovementAllowance`입니다.
- 확인 부탁: 행동 범위 결과가 천명님 판과 같은지 봐 주세요. 지나가기만 되는 칸(도약 보유 유닛이 넘는 유닛·벽·낭떠러지 칸, `LeapTraitRule.AllowsIntermediate`)은 넓히는 데만 쓰이고 결과에는 없어야 합니다. 도약이 없는 유닛은 두 검사가 같아 옛 결과와 같습니다.

## 4. 표식 줄 — 공통 효과 계약을 기다림 (요청 없음)

- 지금: 표식 사건(`CombatHuntersMarkEvent`) 하나를 보이려고 View가 `new CharacterDebuffState(…)`로 여섯 칸을 손으로 옮깁니다(§1).
- 처음에는 `CharacterDebuffState.WithHuntersMark`를 요청하려 했지만, 10-30에 예고하신 공통 버프·디버프 구조(표식 → 약점 노출)로 이 갈래 자체가 옮길 대상이 되어 요청하지 않습니다.
- 계약 인계를 받으면 이 갈래를 그 계약으로 옮깁니다. 그 전까지는 지금 꼴(여섯 칸 옮김)로 둡니다.
- 참고 — View가 특성별 상태를 직접 읽는 곳(옮길 대상):
  - HUD 걸린 상태 줄 표 `Display/Unit/CombatUnitStatuses.Rows` 한 곳(디버프 여섯·특성 효과 둘·가속 보유 상태). 공통 목록이 오면 이 표의 읽기만 바꿉니다.
  - 표식 갈래(위)와 형식 표의 `CombatHuntersMarkEvent` 줄, 상태 외형 `UnitStatusVisual_HuntersMark`(이름·"공격 시 소모" 문구), 위압 줄의 우선도 값(`PriorityPenalty`)·문구.
  - 유닛 카드·덱 편집의 표식 설명(`UnitCardTraitContent`·`UnitCardUI`·`DeckEditDisplayModel`·`DeckEditModelBuilder`·`DeckEditUI`)은 천명님 줄입니다.

## 5. View가 읽는 새 Core 사실 (참고 — 바꿀 것은 없습니다)

| 사실 | View에서 | 기대하는 Core 계약 |
|---|---|---|
| 발동 사실의 추가 공격 결과 `ChildAttack` | 추가 공격인지의 판정 원본(발동 종류 이름 목록은 걷음). 칸·방향은 `TargetCell`·`TargetCells`·`AttackDirection`·`TargetIds`에서 | 추가 공격 시작은 발동 하나짜리 사건(`AttackResultFactory.AttachChild`), 방향이 있음. 아니면 View가 계약 오류로 멈춥니다 |
| 공격 결과 안 가로채기 `Interception` | 대신 맞기(희생): 수호자가 공격 전에 지켜진 유닛 앞으로 나가 맞고 돌아옴(Core 위치는 그대로) | 공격 결과의 발동 목록(포효·추가 공격이면 그 결과) 안에만 옴, 수호자 = `SourceUnitId` = `GuardianId` = `ActualRecipientId` |
| 걸음 사실의 이동 원인 `MovementCause` | 스스로 이동(뛰어 이동)인지 밀림(걷기)인지 | 밀림 걸음은 원인을 채움(Forced). 보통 스스로 이동은 비어 있어도 됨 |
| 흔적 `TraitTrailFact`(환경 목록·이동 원본 사건) | 땅 다지기·먼지 연막 흔적 표시 | 지운 흔적도 `Active = false`로 목록에 남음 |
| 상태 사건 `CombatTraitStateEvent` | 걸린 상태가 바뀐 순간 HUD를 바꿈 | 근원 `Origin`이 피해 묶음 열쇠와 같은 꼴 |
| 디버프 여섯 칸·`TraitEffects`·가속 보유 상태 | HUD 걸린 상태 칸(무장 해제·발목 꺾기·방어구 파괴·공격력 증가·보호막·가속) | 가속 쌓인 공격력은 `AccelerationTraitStatusSnapshot.HeldAttackBonus` |

## 6. 따로 전달할 것

- Core 전달 문서 §53 — 대신 맞은 공격의 넉백 요청 R-AA1(지금 Core는 지켜진 유닛을 밂). 사용자가 나중에 전달합니다.
- Core 요청 문서(§49)의 R5 보강·Q14 — 걸린 상태 공통 목록. 처음 전달한 뒤에 더한 것입니다.

## 7. 2026-11-05 전투 View 평가 후속 5차 — 끌어오기 도착 사건 한 줄·질문 둘 (미전달)

[후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §9(5차)에서 천명님 줄 하나를 고쳤습니다. 줄 번호는 HEAD `fd8b0d2f` 기준입니다. 컴파일 미확인, 새 시험 없음.

- `Playback/Support/CombatShownFacts.cs` 295(`4bd6b574`, C10-13): 끌어오기 도착 뒤 체력·사망 사건을 보인 자리로 바꿔 만들 때 `DamageOrigins`까지만 넘겨, 피해 원인(`DamageCause`, 예: 직접 처치)이 기본값으로 바뀌던 것을 고쳤습니다. 위치 보존 갈래(바로 위)와 같이 사건 칸 여덟을 모두 넘깁니다. 지금 View가 피해 원인을 읽지 않아 화면 변화는 없습니다.

- 6차 주석 정리(평가 C12)에서 천명님 줄의 주석 둘을 고쳤습니다(`d4a6d6fd`, `ICombatActionDisplay` 12·`ICombatMovementFeedback` 11, 지운 시험 가짜·폐지된 복귀 슬롯 설명). 동작은 같습니다.
- 천명님 계정 커밋 `57af3244`의 줄 넷(`CombatSceneInputFactory` 61, `CombatEndTurnConfirmation` 35, `CombatEndTurnEntry` 28·29)도 주석을 고쳤습니다. 프로젝트 기록상 11-03 턴 종료 확인창은 용근 작업이라 작성자를 확인한 뒤 알려 드립니다.

여쭐 것

- **C1-57 특성 발동 → 상태 연결 사실(Core 요청):** View는 어느 특성 발동이 어느 상태를 걸었는지를 사본 표(`CombatTraitFeedbackProfiles` 38-48: CleanSweep → 공격력 증가, ReconSquad·HuntersMark → 약점 노출, 가속 → 보유 공격 보너스)로 짐작합니다. Core 상태 형식이 바뀌면 그 특성의 상태 이펙트·글자가 빠집니다(경고는 남음). `TraitActivationSnapshot`이나 `CombatTraitStateEvent`가 그 발동이 부여·변경한 상태 항목(출처 키와 상태 식별)을 함께 공개해 주실 수 있을까요? 공개하지 않기로 하시면 지금 사본 표를 받아들인다고 기록만 남기겠습니다. Core 전달 문서 §65에도 같은 요청을 적었습니다.
- **C1-42 곡사 공통 수명 — (2026-11-12 정정: 이 질문은 필요 없게 됐습니다.** 곡사·기반 두 파일 모두 용근 줄입니다(GitHub 파일 이력 `37188bd5`·`dad08ef1`, 얕은 clone의 blame 경계 커밋을 잘못 읽음). 곡사를 기반 위로 올리는 일은 11-12 SM-b로 끝냈고, 정리 순서는 옛 곡사와 같습니다([계획](../plans/2026-11-12-combat-view-sound-preset-mortar-lifecycle-plan.md) §10.3).**) 옛 질문: `CombatMortarAnimation`(천명님 파일)을 공통 기반 `CombatAttackMotion`(호진님 파일)에 훅 둘을 더해 올리려 합니다(약 130줄, 구조 단계). 정리 순서가 '무기 해제 → 탄 회수 → 무기 자세 → 유닛 자세'에서 기반의 해제 → 자세 복원으로 바뀝니다. 진행해도 될지 호진님과 함께 합의를 부탁드립니다.

## 8. 2026-11-05 전투 View 조정자 나누기 — 확인창·턴 종료 줄 (미전달)

[조정자 나누기 계획](../plans/2026-11-05-combat-view-coordinator-split-plan.md)(설계 [coordinator-split-design](../specs/2026-11-05-combat-view-coordinator-split-design.md) CS1~CS6)에서 천명님 계정 커밋 `57af3244`의 줄을 고치거나 옮겼습니다. 줄 번호는 HEAD `dfb49b34` 기준입니다. 새 시험 없음, 시험 변경 없음. Unity 컴파일·재생 문제 없음(사용자, Ua~Ud).

동작은 그대로입니다. 바뀐 것은 자리와 이름입니다.

- `Input/CombatSceneInput.cs`
  - 확인창 입구 속성 `EndTurn`(82-83)을 private 필드 `_endTurn`으로 바꿨습니다. 배치 진입 `Deployment`도 같이 `_deployment`가 되었습니다(CS-a). 밖에서는 묶음 입구 `RequestEndTurn()`·`RequestEnterCombat()`을 부릅니다.
  - 생성자 대입(57)·`IsConfirmationOpen`(86)·`CloseConfirmations`(95·97)·`SyncConfirmations`(106-107)는 필드 이름만 바뀌었습니다. 클래스 설명(28)은 "부품은 밖에 내지 않는다"로 고쳤습니다.
- `Input/ActionButtons/CombatEndTurnConfirmation.cs` 47: `CountIdleUnits`가 자기 클래스 안에서만 쓰여 internal → private으로 내렸습니다(CS-a).
- `CombatViewSystem.cs`
  - 1128-1132 턴 종료 갈래(`connection.Input.EndTurn.Request()`)는 새 입력 분배 `Input/CombatSceneInputDriver.Route`로 옮기고 `connection.Input.RequestEndTurn()`을 부릅니다(CS-a·CS-b).
  - 1175 `SyncConfirmations()`는 입력 분배의 `SyncActionButtons`로 옮겼습니다(CS-b). 순서(확인창 맞춤 → 보임 확인 → 표면 맞춤)는 같습니다.
  - 719 입력 재료 `afterConfirmedSubmit: SyncActionButtons`는 람다 `() => _inputDriver.SyncActionButtons(_connection)`가 되었습니다(CS-b, 계획 TP-1). 부를 때 지금 연결을 읽는 것은 같습니다.
  - 760·769·801·939 확인창 닫기와 1030-1033 `IsModalOpen`은 그대로입니다.
- `Input/CombatSceneInputSources.cs` 33(주석만): `afterConfirmedSubmit` 인자 설명의 "조정자의 입구"를 "입력 분배 `SyncActionButtons`"로 고쳤습니다.
- `Input/ActionButtons/CombatEndTurnEntry.cs` 32(주석만): `afterSubmit` 인자 설명을 같은 뜻으로 고쳤습니다(묶음 끝 검토 낮음).

여쭐 것

- `57af3244` 줄의 작성자: 프로젝트 기록상 11-03 턴 종료 확인창은 용근 작업입니다. 천명님 계정으로 커밋된 까닭을 확인한 뒤, 이 절을 천명님께 넘길지 정합니다(§7 끝의 같은 질문).

## 9. 2026-11-06 전투 View 선택 인자 나머지 — 생성자 서명·입력 사실·턴 종료 확인창 (미전달)

[선택 인자 나머지 계획](../plans/2026-11-06-combat-view-optional-args-plan.md)(설계 [optional-args-design](../specs/2026-11-06-combat-view-optional-args-design.md) OA1·OA3·OA4·OA5, 평가 C5-02 2단계)에서 천명님 줄을 고쳤습니다. 작성자는 전체 이력으로 셌습니다(설계 §1.7). 줄 번호는 고친 뒤(기준 판 `7279a9d5` 위) 기준입니다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음. 시험 변경은 우리 시험(`StructureHitPresentationTests.cs:168`) 호출 한 곳뿐이고, 천명님 시험은 고치지 않았습니다.

운영 조립이 늘 넘기는 협력자·값을 필수로 받고, 쓰는 곳의 `?.`·null 갈래를 지웠습니다. 운영에서 보이는 동작은 같습니다.

- `Input/MovePath/CombatPathInput.cs` 88-91 생성자 서명(`e0b02cc7`): 마지막 두 인자 `rejections`·`actionAvailability`의 기본값 `= null`을 지우고 93-95에서 `?? throw`로 막습니다. 쓰는 곳의 `?.` 넷(155·263·266·651)을 지웠습니다.
- `Input/MovePath/CombatMovementInteraction.cs` 76-77 생성자 서명(`87348b86`): `rejections` 기본값을 지우고 86에서 막습니다.
- `Input/CombatInputFacts.cs` 40 `battleInstanceId`·`controllerTurnSequence`(`d4a6d6fd`): 기본값을 지웠습니다. 입력 사실 인자 28개가 모두 필수입니다(결정 Q-OA1 A). 운영 읽기는 이미 모두 넘깁니다.
- `Input/ActionButtons/CombatActionButtons.cs` 19 `canUseReturn = false`(`d4a6d6fd`): 기본값을 지웠습니다.
- `Input/ActionButtons/CombatEndTurnConfirmation.cs`(`57af3244`)
  - 36 생성자: `presentation ?? throw new ArgumentNullException(nameof(presentation))`. 35 주석도 같은 뜻으로 고쳤습니다.
  - 44 `IsOpen`·93 `Close`: `_presentation?.`·`?? false`를 지웠습니다.
  - 확인창을 여는 곳(옛 74-75)의 `InvalidOperationException` 갈래를 지웠습니다. 예외 종류가 생성 때 `ArgumentNullException`으로 바뀝니다(계획 TP-5). 운영에서는 둘 다 나지 않습니다.

참고 — View가 기대는 Core 보장(요청 없음)

- 경로 입력이 필드 유닛의 칸·방향을 다시 확인하지 않게 되었습니다(`actor.Anchor.HasValue` 삭제, `actor.Rotation.Value`). 근거는 `CharacterCombatFacts` 생성자의 위치 검사(255-258)가 `Area == Field`인 유닛의 Anchor·Rotation을 요구한다는 것입니다. 이 보장을 풀게 되면(필드 유닛의 위치를 비워 두는 사실을 허용) View 경로 입력이 `InvalidOperationException`으로 멈춥니다. 그때 알려 주시면 됩니다.

여쭐 것

- `57af3244` 줄의 작성자: §8 끝과 같은 질문입니다. 확인한 뒤 이 절을 천명님께 넘길지 정합니다.

## 10. 2026-11-07 전투 View 씬 표시 나누기 — 공격 범위·막힌 벽 계약과 재화 표시 (미전달)

[씬 표시 나누기 계획](../plans/2026-11-07-combat-view-scene-display-split-plan.md)(설계 [scene-display-split-design](../specs/2026-11-07-combat-view-scene-display-split-design.md) DS2·DS4, 평가 C2-03 2·3단계)에서 천명님 줄을 옮기거나 고쳤습니다. 작성자는 기준 판 `eafd42f5`의 전체 이력으로 셌습니다(계획 T0.2). 줄 번호는 고친 뒤 기준입니다. 컴파일 미확인, 새 시험 없음. 천명님 시험은 고치지 않았고, 바뀐 형식을 쓰는 시험도 없습니다(T0.3 grep). Core 파일은 건드리지 않았고 요청도 없습니다.

- 공격 범위·막힌 벽 계약(`4bd6b574` `basicCells` 인자, `66f6bb41` `ShowAttackBlockers`·`ClearAttackBlockers`)
  - 입력 피드백 계약 `ICombatInteractionFeedback`(17)을 쓰는 쪽마다 셋으로 나눴습니다(결정 Q-DS2 A). 공격 조준 몫은 `git mv`로 `Input/Contracts/ICombatAttackRangeFeedback.cs`가 되었고, 천명님 줄(20 `ShowRangeCells(…, basicCells = null)`, 30-32 막힌 벽 둘과 문서)은 글자 그대로입니다.
  - `CombatSceneDisplay`의 위임 셋(`ShowRangeCells`·`ShowAttackBlockers`·`ClearAttackBlockers`)을 지웠습니다. `Display/Feedback/CombatAttackFeedback.cs`가 계약을 직접 구현합니다(91·140·151, 명시적 구현). 본문은 같고, 맨 앞에 해제 뒤 막기(`_lifetime.IsReleased`) 한 줄이 생겼습니다(설계 DS1, 옛 표시의 `ThrowIfReleased` 자리). `basicCells` 기본값은 계약에만 있습니다.
- 재화 표시(`d4d9fb24` `_hud.Render(values.RunGold)`)
  - 새 `Display/Hud/CombatHudSurfaces.RenderStatus(runGold, turn, phase)`가 HUD 재화와 턴 표시를 함께 그립니다. 표시는 `_hud.RenderStatus(values.RunGold, …)`(223)로 부릅니다. 값과 차례는 같습니다.
- 해제 목록 줄(`98fd0c64`, `ClearTurnCost`를 걷은 줄)
  - `HideCompletion, _turnDisplay.Clear`는 `_completion.Hide, _hud.ClearTurn`(290)이 되었습니다. 차례는 같습니다.
- 그대로인 줄: `CombatAttackFeedback`의 칸 필드 셋(`_blockedWallCells`·`_extensionCells`·`_basicCells`), `CombatPointerMapper._uiHits`, `AttackTargetingInteraction`의 `_input`·`_requests`·`_requestBattleId`·`_requestTurnSequence`·`_rangeBlockers`.
- `57af3244` 줄은 이번에 바뀌지 않았습니다(§8·§9의 작성자 질문은 그대로).
