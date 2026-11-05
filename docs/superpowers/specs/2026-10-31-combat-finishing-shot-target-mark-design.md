# 마무리 사격 공격 모드 대상 표시 (설계 초안)

- 날짜: 2026-10-31
- 상태: **Q-FS1·Q-FS2 결정(2026-10-31 사용자 "권장대로" = A·A), FS-a Core 코드 완료(컴파일 미확인, 새 시험 없음, Core 전달 문서 §5.75), Q-FS3·Q-FS4·FS-b View는 Core 검토 뒤. 2026-10-31 [이펙트 구조 2판](2026-10-31-combat-effect-lifetime-design.md)이 View 부분(FS2·FS3)을 상시 목록 조건 "공격 모드 대상"으로 대신한다(EL3·EL-d) — Q-FS3은 A(특성 외형 칸)로 정해졌다. Q-FS4도 A(호버 중 유지, 2026-10-31 사용자 "나머지는 권장대로"). View 구현은 [이펙트 구조 계획](../plans/2026-10-31-combat-effect-structure-plan.md) EL-d.** 계획 [특성 이펙트 Core 변경 계획](../plans/2026-10-31-combat-trait-core-changes-plan.md)
- **대신함(2026-11-01, [이펙트 구조 2판](2026-10-31-combat-effect-lifetime-design.md) EL3·EL-d):** FS2(값 = 특성 외형 칸)·FS3(View 붙이고 거두기)는 특성 외형 상시 목록 조건 `AttackModeTarget`과 유지 표시 부품의 공격 모드 대상 출처(`CombatAttackModeEffectSource`)로 구현했습니다. 공격 조준 → 씬 표시 → 사건 Presenter 사슬은 표시 의도 `ICombatAttackModeTargets`입니다. 이펙트 종류는 정리표에 없어 에셋은 비워 두었습니다.
- 요청: 사용자 "마무리 사격 특성 보유자가 공격 모드에 들어가면, 효과가 충족된 적 대상 유닛에게만 이펙트를 띄우고 싶다."
- 관련: [이펙트 끝 규칙 설계](2026-10-31-combat-effect-lifetime-design.md)(쥔 이펙트·끝 규칙), [특성 이펙트 정리표](../viewDocs/combat-trait-effect-sheet.md)(마무리 사격 400 줄).

## 0. 요지

- 공격 모드에 들어가면 View는 이미 Core에 공격 범위를 묻습니다(`AttackRange` 미리보기 → `CombatAttackQueries.Range`).
- 그 응답(`AttackRangeResult`)에 **마무리 사격이 걸리는 대상 목록**을 Core가 함께 실어 줍니다. 판정은 Core가 실제 공격에서 쓰는 `FinishingShotApplication.Qualifies` 하나입니다.
- View는 그 목록의 유닛에 이펙트를 붙이고, 공격 모드를 벗어나거나 응답이 바뀌면 거둡니다.
- 판정 식(`현재 체력 × 100 < 공격력 × 20`)과 기준 값(20%)은 Core 규칙 값입니다. View가 베끼지 않습니다(View는 InGame DB를 읽지 않는다는 규칙 P6, 그리고 SSOT).

## 1. 지금 (코드 확인)

| 항목 | 지금 |
|---|---|
| 판정 | `FinishingShotTraitRule.Qualifies`: `현재 체력 × 100 < 공격력 × 기준(20)`. 공격력은 버프까지 들어간 실제 값(`CombatEffectiveStatsCalculator.AttackPower`). 기준은 적 최대 체력이 아니라 **보유자 공격력의 20%**. |
| 입구 | `FinishingShotApplication.Qualifies(input, actor, target, structure)` — Core 내부(`internal`). 실제 공격·처치 연쇄·희생 계획이 부릅니다. |
| 공격 모드 | 진입하면 `AttackTargetingInteraction`이 `AttackRange` 미리보기를 요청하고, 응답이 오면 범위 칸을 칠합니다(`AttackTargetingInteraction.cs` 응답 처리 → `CombatAttackFeedback.ShowRangeCells`). |
| 범위 응답 | `AttackRangeResult`: 범위 칸·기본 공격 칸·**사거리 안 대상 `InRangeTargetIds`**·조준·막힘 근거. 마무리 사격 정보는 없습니다. |
| 공격 호버 미리보기 | 실제 공격 계산을 돌려 예상 체력이 0으로 나오지만, "마무리 사격으로 처치"는 따로 표시되지 않습니다. |

## 2. 설계

### FS1 Core: 범위 응답에 마무리 사격 대상

- `AttackRangeResult`에 `InstantDefeatTargetIds`(`ReadOnlyMemory<EntityId>`)를 더합니다. `Success`의 끝 선택 인자라 기존 호출은 그대로입니다.
- `CombatAttackQueries.Range`가 대상 후보마다 `FinishingShotApplication.Qualifies`로 거릅니다. 캐릭터와 구조물 모두 넣습니다.
  - 후보는 Q-FS1이 정합니다. 권장 A: 사거리 안 대상(`InRangeTargetIds`).
- 마무리 사격을 갖지 않은 유닛이면 `Qualifies`가 바로 거짓이라 빈 목록입니다.
- 이 작업에서 Core를 고칠지는 Q-FS2입니다. 고치면 Core 전달 문서 §5에 적습니다.

### FS2 값: 어떤 이펙트를 띄우나 (Q-FS3)

- 권장 A: **마무리 사격 특성 외형 에셋**에 "공격 모드 대상 표시" 칸을 둡니다.
  - 이펙트 큐 항목 목록 꼴로, 이펙트 종류와 위치·회전 보정을 고릅니다. 자리는 대상 몸으로 고정이고, 몸을 따라갑니다.
  - 칸이 비어 있으면 아무것도 띄우지 않습니다.
- 값을 특성 외형에 두는 까닭: 다른 특성이 같은 꼴의 표시를 원하게 되면 그 특성 에셋에 칸만 채우면 됩니다.

### FS3 View: 붙이고 거두기

- **새 표시 부품 `CombatAttackTargetMarks`**(가칭, `Display/Feedback/`)가 표시할 대상 집합 하나를 가집니다.
  - 붙이기: 범위 응답이 오면 이 부품에 "공격자, 마무리 사격 대상 목록"을 넘깁니다. 부품은 공격자가 그 특성의 표시 칸을 가지면 대상마다 이펙트를 냅니다.
  - 거두기: 공격 모드 끝, 새 범위 응답(목록이 바뀜), 결과 재생 시작(`BeforeResult`), 입력 정지 때 집합을 비웁니다.
- 이펙트는 이펙트 창구의 쥔 이펙트(`PlayHeld`)로 냅니다. 쥘 규칙은 "그 대상이 지금 표시 집합에 있음"입니다.
  - 그래서 집합에서 빠지면 다음 프레임에 받침이 거둡니다. 링 버퍼·반복 이펙트도 남지 않습니다.
  - 끝 규칙 설계가 정리되면 그 꼴(EL3 끝 판정 값)로 옮깁니다.
- 입력 쪽 연결은 표시 계약에 멤버를 더하지 않습니다. 좁은 계약 하나를 조준 입력 생성자의 끝 선택 인자로 받습니다. 표시 계약에 멤버를 더하면 시험 가짜 표시 여럿의 컴파일이 깨지기 때문입니다(궤적 미리보기 때와 같은 방식).
- 공격 호버 중에도 표시를 유지할지는 Q-FS4입니다.

### FS4 다시 계산하는 때

- 범위 응답이 다시 올 때마다 목록을 바꿉니다: 공격 모드 진입, 같은 공격 모드에서 결과 뒤 다시 요청.
- 버프로 공격력이 바뀌어도 다음 응답에 반영됩니다. 판정이 Core에 있기 때문입니다.

## 3. 시험

- 새 시험은 쓰지 않습니다(View 작업 규칙).
- Core `AttackRangeResult.Success`는 끝 선택 인자라 기존 시험 호출이 그대로 됩니다.
- 표시 계약을 바꾸지 않으므로 시험 가짜 표시는 바뀌지 않습니다.

## 4. 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-FS1 | 어떤 적에게 띄우나 | **A** 사거리 안 대상(이번에 칠 수 있는 적) 중 충족된 적 / B 필드의 충족된 적 모두 |
| Q-FS2 | Core 변경을 이 작업에서 하나 | **A** 이 작업에서(Core 전달 문서 §5 기록) / B 천명님께 요청 |
| Q-FS3 | 이펙트 값 자리 | **A** 마무리 사격 특성 외형 에셋의 "공격 모드 대상 표시" 칸 / B 전투 연출 설정 표 공통 칸 하나 |
| Q-FS4 | 공격 대상에 호버하는 동안 | **A** 그대로 유지 / B 호버 중에는 숨김 (결정 2026-10-31: A) |

## 5. Phase(안)

| Phase | 내용 |
|---|---|
| FS-a | Core: `AttackRangeResult.InstantDefeatTargetIds`, `Range` 계산 |
| FS-b | View: 특성 외형 칸, 표시 부품, 조준 입력 연결, 거두기 시점 |
| FS-c | 정리표·문서·Core 전달 문서 §5·천명님 공유 |

컴파일·재생은 사용자가 Unity에서 확인합니다.
