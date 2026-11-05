# 전투 특성 발동 연출 — 천명님 공유 (2026-10-29, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 천명님.
> 상태: View 쪽 코드·에셋 완료(TA-a~TA-e), **컴파일·Unity 일부 미확인**(TA-a~TA-c는 Unity 확인 문제 없음, TA-d·TA-e는 확인 대기), 커밋 전.
> 관련 문서: [View 설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md) · [View 계획 §6](../plans/2026-10-29-combat-trait-activation-presentation-plan.md) · [Core 요청](2026-10-29-combat-trait-activation-core-request.md) · Core 전달 문서 [§5.60](2026-09-24-view-core-data-boundary-handoff.md)·[§49](2026-09-24-view-core-data-boundary-handoff.md#trait-activation-request)

## 요약

- Core 파일은 바꾸지 않았습니다.
- View가 이제 특성 발동 사건(`CombatEffectsEvent`)을 받아 연출합니다. 특성 외형 표의 특성 줄에 발동 연출 줄을 채우면 발동 순간 이펙트·흔들림이 나오고, 처치 연쇄·대상 있는 추가 공격은 발동 유닛이 대상에게 다시 쏩니다. 줄이 비면 전과 같습니다.
- 천명님 시험 `CoreIntegration/PullAndMarkPresentationTests.cs` 두 줄을 컴파일·실행에 맞췄습니다(§1). 확인하는 내용은 그대로입니다.
- 천명님 줄 하나(사냥꾼의 표식 HUD 칸, `4bd6b574`)를 표로 옮겼습니다(§2). 화면은 같습니다.
- **따로 전달할 것:** Core 요청 문서의 R5 보강과 §9 Q14(걸린 상태 공통 목록)는 처음 전달한 뒤에 더한 것입니다(§5).

## 1. 바꾼 천명님 시험 두 줄

| 자리 | 바꾼 것 | 왜 |
|---|---|---|
| `:365` | `attacks.FireShot(new CombatAttackEvent(…), …)` → `attacks.FireShot(CombatAttackShape.Of(new CombatAttackEvent(…)), …)` | 공격 조립·재생·종류 전략이 공격 사건 대신 View 값 `CombatAttackShape`(출처 사건·공격자·방향·타격 칸·영향 칸·주 대상)를 받게 됐습니다. 특성 추가 공격에는 공격 사건이 없기 때문입니다(설계 TA5, Core 전달 문서 §5.60). 컴파일 맞춤입니다 |
| `:67` | `_units.Configure(_visuals, 1f, hudLayer: layer)` 끝에 `statuses: …CombatDisplayTables.From(PresentationDatabase().ViewQuery).UnitStatuses`(이제 `Configure`의 셋째 필수 인자 — 이름 있는 인자라 이 줄은 그대로 컴파일됨) | 표식 HUD 칸이 이제 상태 줄 표에서 나옵니다(§2). 줄 표를 넘기지 않으면 표식 칸이 생기지 않아 `MarkApplyConsume_OneIndicator`와 `CancelOrSceneExit_ReleasesMotionAndMarkVisuals(false)`가 실행에서 실패합니다. 같은 파일 `:419`가 이미 같은 꼴로 실제 DB 표를 씁니다(사용자 지시 "한 줄 고쳐") |

- `:67`은 `SetUp`마다 DB를 한 번 더 만듭니다. 시험 시간만 조금 늘어납니다.
- 두 시험 모두 컴파일은 확인하지 않았습니다(전투 View는 MSBuild를 돌리지 않음, 사용자가 Unity에서 확인).
- 부탁: 브랜치에서 이 줄을 고치셨다면 병합 때 위 두 꼴을 남겨 주세요.

## 2. 표로 옮긴 천명님 줄 — 사냥꾼의 표식 HUD 칸

- 옛 자리: `FieldUnitPresenter.RenderUnitHud`의 표식 블록(`4bd6b574`). 특성 외형 표의 표식 줄(키 190)의 아이콘과 코드 안 고정 문구로 칸을 만들었습니다.
- 지금: 유닛에 걸린 상태(버프·디버프) 모두를 같은 길로 그립니다(설계 TA9).
  - 아이콘·이름·설명 틀·구분은 새 View DB 표 **상태 외형 표**(`UnitStatusVisual`)의 에셋이 가집니다. 표식은 `DataBaseSO/ViewGroup/UnitStatusVisual/Item/UnitStatusVisual_HuntersMark.asset`입니다.
  - 걸렸는지와 틀에 넣을 값은 상태 줄 표 `Display/Unit/CombatUnitStatuses`가 Core 사실 `CharacterDebuffState`에서 읽습니다. 표식은 `Debuffs.HuntersMark.BonusDamagePercent` 하나입니다.
- 같은 것: HUD 칸 키 `HuntersMarkDebuff`, 문구("사냥꾼의 표식: 다른 아군의 일반·추가 공격 피해 N% 증가. 공격 시 소모됩니다."), 자리(아이템 칸 다음).
- 다른 것: 아이콘은 이제 상태 외형 에셋의 아이콘입니다. 지금 둘 다 비어 있어 화면은 같습니다(HUD 기본 아이콘).
- 같은 길로 위압(`Debuffs.Intimidation`)과 방어 감소(`Debuffs.DefenseReduction`)도 HUD에 나옵니다. 지금까지는 보이지 않던 상태입니다.

## 3. 특성 외형 표에 칸이 생김

- `TraitVisualData`에 발동 연출 줄 목록 `activations`가 생겼습니다. 줄 하나 = 발동 종류(`TraitActivationKind`) 하나이고, 큐(이펙트·흔들림)와 공격 표시 덮어쓰기(추가 공격 줄만)를 담습니다.
- 비어 있으면 그대로 통과합니다. 천명님이 만든 외형 에셋(표식 190·끌어오기 200 등)은 고칠 것이 없습니다.
  - Unity가 인스펙터에서 다시 저장하면 에셋에 `activations: []` 한 줄이 생깁니다. 값은 같습니다.
- 검사(부트 DB 생성 때): 같은 발동 종류 두 줄, 정의되지 않은 종류, 큐 칸 규칙 위반, 추가 공격이 아닌 줄에 공격 표시 덮어쓰기, 덮어쓰기의 이펙트 큐가 행위자 지점 자리(이펙트 지점·발사 지점)를 씀 → 예외. 공격 클립 유닛에서 덮어쓰기 계열(근접/원거리)이 다르면 전투 때 경고 한 번 뒤 그 유닛의 공격 표시로 칩니다.
- 새로 만든 특성 외형 에셋 둘: 행운의 탄환(키 170)과 연쇄 폭발(키 180)입니다. 둘 다 아이콘은 비었습니다. 이름과 설명은 규칙 코드를 보고 쓴 초안이라 문구를 확인해 주세요.
  - 행운의 탄환: "난사가 발동할 때마다 적 건물이나 보스 하나를 골라 한 번 더 공격합니다."
  - 연쇄 폭발: "밀려난 대상이 살아 있으면 그 대상에게 추가 피해를 줍니다."
  - 키 140·150은 아직 외형 줄이 없습니다(Core 전달 문서 §51).

## 4. View가 기대는 Core 동작 (요청 없음)

Core 순서가 바뀌면 View 재생 순서가 어긋나므로 적어 둡니다.

- 처치 연쇄와 대상 있는 추가 공격(연쇄 폭발·행운의 탄환)의 발동 사건(`CombatEffectsEvent`)은 그 타격의 표식·보유 방어·체력 사건 **앞**에 있습니다(대조: `FollowUpAttackExecutor.cs:82-87`, `KillChainAttackCalculator.cs:78-88`).
  - View는 발동 사건에서 다시 친 공격의 맞는 순간에 사건을 완료합니다. 그래서 뒤따르는 피해는 맞는 순간부터 시작합니다.
- 발동 사실의 `TargetId`는 캐릭터 또는 구조물입니다. View는 보인 사실에서 두 쪽 모두 찾습니다.
- 대상 없는 추가 공격(2연발 `FollowUpAttack`)은 방향·칸 사실이 없어 다시 치지 않고 경고를 한 번 남깁니다(Core 요청 R7을 기다림).
- 한 사건에 다시 치는 추가 공격은 하나라고 봅니다(지금 Core가 하나만 냄). 둘째부터는 경고 뒤 건너뜁니다.
- View 결정(Q-TD1 A·검토 M1): 대상 있는 추가 공격이 든 발동 사건은 이전 공격 연출이 다 끝나고 다른 유닛 연출(피격·사망 반응·넉백)도 다 끝난 뒤 시작합니다(출처 둘이 겹치지 않고, 다시 친 공격 동안 다른 유닛 반응이 멈추지 않게). 옛 장벽(주체 없음)과 거의 같은 시점입니다.
- 그 밖의 발동 사건(큐만·2연발)은 주체가 발동 유닛이라 그 유닛 연출만 기다립니다. 그래서 연출 줄이 없어도 다른 유닛 반응과 겹쳐 전보다 조금 일찍 시작할 수 있습니다(검토 L4).

## 5. 따로 전달할 것 — R5 보강·§9 Q14

Core 요청 문서를 처음 전달한 뒤 사용자 결정으로 더했습니다.

- **R5 보강:** 걸린 상태를 공통 목록으로도 내 주세요. 항목은 상태 종류 키(Core 열거), 값(정수 목록, 차례는 종류마다 Core가 정함), 남은 턴(없으면 비움), 건 유닛(있으면)입니다.
  - 지금 `CharacterDebuffState`의 칸 셋은 형식이 서로 다릅니다. 버프·디버프가 늘 때마다 Core 형식·사실 칸·View 줄이 함께 늘지 않게 하려는 것입니다.
  - 버프/디버프 구분과 아이콘·이름·설명은 View 상태 외형 표가 종류 키로 가집니다. Core는 구분을 정하지 않아도 됩니다.
- **Q14:** 목록의 자리. View 쪽 입장은 A입니다 — 지금 칸 셋은 계산용으로 그대로 두고, 목록은 같은 값에서 만든 보기로 둡니다(한 값을 두 곳에 저장하지 않음).
- 목록이 오면 View는 상태 줄 표의 읽기만 목록으로 바꿉니다. 새 상태는 Core 목록 항목 + View 외형 에셋 하나로 보입니다.

## 6. 부탁

1. 병합 때 §1의 시험 두 줄 꼴을 남겨 주세요.
2. §5의 R5 보강·Q14에 답을 주세요(처음 요청한 R1~R10·Q1~Q13과 함께 보셔도 됩니다).
3. §3의 새 외형 에셋 둘(170·180)의 이름·설명 문구를 확인해 주세요.
