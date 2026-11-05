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
