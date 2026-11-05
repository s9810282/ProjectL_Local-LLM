# 전투 이펙트 구조 2판 Task 계획 (끝 규칙·상태 유지·상시·위치 이동 행동)

- 날짜: 2026-10-31
- 상태: **EL-a~EL-h 구현 커밋 `36a6d91`. 2026-11-01 사용자 "인계 문서 확인하고 너가 검증까지 진행해줘"에 따라 로컬 Unity 컴파일·기존 EditMode 전체·운영 전투·연출 도구를 확인했다. 검증 결과·프리팹 보완·남은 항목은 §5.4가 현재 원본이다(전체 시험 실패 3건, 수동 확인 일부 잔여).** 계획에서 정한 것 TP-E1~TP-E30(§1). 다음 세션 인계: [handoffs/2026-11-01-combat-effect-structure-next-session-handoff.md](../handoffs/2026-11-01-combat-effect-structure-next-session-handoff.md).
- 요청: 사용자 "Q-EL4 : 위치 이동 유닛 액션으로 추가. 이펙트와 시간 동일하게 사용 / 나머지는 권장대로 진행해". 위치 이동 방식은 질문에 "A 유닛 행동(차례로)".
- 설계 원본: [이펙트 끝 규칙 설계 2판](../specs/2026-10-31-combat-effect-lifetime-design.md)(EL1~EL11). 정리표: [특성 이펙트 정리표](https://claude.ai/artifact/1QtPyzLxLbsJrKQvV1Y71T)(25줄, 18:09 기준).
- 결정(2026-10-31 사용자):
  - Q-EL4 = 위치 이동 유닛 행동. 이동자가 먼저 걷고, 승객은 발동 순간 옛 자리에서 새 자리로 갑니다. 이펙트 9는 그 행동과 같은 시간 동안 보입니다. 설계 EL11로 적었습니다.
  - 나머지 = 권장: Q-EL1 A(큐 끝 자연·시간 — 여기에 Q-EL4의 행동 끝이 더해짐), Q-EL2 A, Q-EL3 A, Q-EL5 A, Q-EL6 A, Q-EL7 A, Q-EL8 A, Q-EL9 A. 마무리 사격 Q-FS4도 A(호버 중 유지).
- 이 계획이 대신하는 것: [특성 시스템 View 계획](2026-10-30-combat-trait-system-view-plan.md) TS-h(승객도 이동 레인, TP-31~TP-36)는 하지 않습니다. 위치 교환은 EL-f가 맡습니다.
- 작업 규칙: View 작업이라 새 시험 코드는 쓰지 않고 MSBuild도 돌리지 않습니다(보고에 "컴파일 미확인"). 기존 시험은 우리 변경으로 컴파일이 깨지는 곳만 최소로 맞춥니다. 다른 사람이 쓴 시험이면 고치기 전에 묻습니다. 검토 에이전트는 묶음 끝(EL-h)에 한 번 돌립니다. Phase마다 멈추고 보고합니다.

## 0. 요지·순서

| Phase | 내용 | 화면 |
|---|---|---|
| EL-a | 큐 끝 값(자연·시간), 창구 입구 하나·모든 이펙트를 받침 위에, 받침 시간 끝, 끝나지 않는 프리팹 경고 | 변화 없음 |
| EL-b | 유지 표시 부품·`Render` 연결, 상태 외형 칸 둘·HUD 규칙 함수, 다시 내기. 쥠 규칙·줄 칸 둘·`PlayHeld`·`RecallBy`·결과 끝 다시 내기를 걷음. 단독 행동 에셋을 상태 외형 보호막으로 | 단독 행동은 같음, 결속 보호막에도 이펙트 |
| EL-c | 특성 상시 목록·조건 "보유 동안"·DB 검사, 희생·굳건한 의지 에셋 | 희생·굳건한 의지 상시 이펙트 |
| EL-d | 조건 "공격 모드 대상", 공격 조준 → 표시 의도(마무리 사격 FS-b) | 마무리 사격 대상(이펙트를 고른 뒤) |
| EL-e | 결속 방향 칸, 피드백 계획에서 방향 정하기, DB 검사 | 결속 오를 때·내릴 때 |
| **EL-f** | **위치 이동 행동(EL11)**: 행동 자리 `Relocate`, 승객 배치 미루기, 큐 끝 "행동 끝", 위치 교환 에셋 | 승객이 발동 순간 새 자리로 감, 이펙트 9가 그동안만 |
| EL-g | 정리표 값을 에셋으로(순간 줄 15·상태 외형 둘·희생 거리 0.4) | 정리표대로 |
| EL-h | 문서·공유·묶음 끝 검토 | — |

- 설계 §8의 EL-f·EL-g는 이 계획에서 EL-g·EL-h가 됩니다. 사이에 위치 이동 EL-f를 넣었습니다(설계 §8도 고침).
- 순서는 설계 EL9와 같습니다. EL-f는 EL-a(창구 입구)만 있으면 되므로 EL-b~EL-e와 순서를 바꿀 수 있습니다.


### 0.1 7기준 검토 (2026-10-31, 계획 1판 대조)

사용자 지시 "SSOT·SOLID·FW 계약·독립성·캡슐화·C#/Unity 권장·무의미한 If/Tick 검토 후 보완 반영". 계획 1판을 지금 코드와 대조했다.

| 번호 | 기준 | 찾은 것 | 반영 |
|---|---|---|---|
| R1 | 캡슐화·SSOT (중간) | 1판 TP-E7 ②는 "행동을 시작하지 않은 유닛의 미룬 배치를 놓기"를 특성 처리기에 두었다. 위치 교환만의 지식이 일반 특성 처리기에 들어간다. | 미룬 배치 보관은 보인 사실 하나(`Hold`·`TryClaim`·`ReleaseUnclaimed`). 처리기는 `Relocate` 행동일 때 가져가기만 한다. 아무도 가져가지 않은 것은 사건 Presenter가 사건 처리기 시작 바로 뒤 한 곳에서 놓는다(TP-E7 고침). |
| R2 | 결함 예방 (중간) | `Relocate` 행동 동안 그 유닛의 더 새 사실이 반영되면, 행동 완료 때 옛(가져간) 사실을 다시 써서 위치가 되돌아갈 수 있다. | 그 유닛의 새 사실 반영(`ApplyCharacter`·`ApplyTraitState`)은 쥔 것·가져간 것을 모두 버린다. 완료는 아직 가져간 상태일 때만 반영한다(TP-E7 고침). |
| R3 | 독립성·FW (중간, 누락) | 발동 줄 그리기 `TraitActivationVisualDrawer`(`DataBaseSystem/Editor/CombatBodyTextDrawer.cs`)가 칸 이름 목록을 직접 갖는다. EL-b에서 `keepWhileStatus`·`recallKinds`를 지우고 그리기를 안 고치면 인스펙터가 예외로 멈추고, EL-e의 `direction`은 보이지 않는다. | Eb.6·Ee.1에 그리기 목록 고치기를 넣는다(TP-E24). |
| R4 | SOLID (중간) | 유지 표시 부품 하나가 상태·보유 동안·공격 모드 대상 세 출처를 모두 계산한다. 조건이 늘면 부품이 커진다(OCP). | 부품은 "보여야 할 것" 목록 비교·내기·거두기만 한다. 출처마다 작은 클래스가 목록을 채운다(`ICombatPersistentEffectSource.Collect`, TP-E23). |
| R5 | FW·독립성 (낮음) | 1판 TP-E15는 표 둘을 `CombatDisplayTables`에서 받는다고 적었다. 사건 Presenter는 표 묶음이 아니라 큐 값 답 `CombatCueSources`를 받고, 특성 외형 표는 이미 그 안에 있다. | 상태 외형 표를 `CombatCueSources`에 더한다(만드는 곳 `CombatDisplayTables`, 우리 파일). Factory 무변경이 확실해진다(TP-E15 고침). |
| R6 | 독립성 (낮음, 누락) | 특성 연출 확인 도구(`CombatTraitPreviewFactory`, 사용자 작성)가 같은 `CombatEventPlaybackFactory`로 사건 재생을 만든다. 모든 Phase 변경이 그 도구에도 간다. | 연결할 것은 없다. Phase마다 Unity 확인에 그 도구를 넣는다. |
| R7 | C#/Unity (낮음) | 끝나지 않는 프리팹 판단을 낼 때마다 `GetComponentsInChildren`으로 하면 할당이 생긴다. 회수 때 링 버퍼 판단도 같다. | 종류마다 처음 한 번만 판단하고 창구가 결과를 기억한다. 판단은 재사용 목록으로 한다(TP-E25). |
| R8 | SSOT (낮음) | 큐 끝 열거(`CombatEffectEnd`)와 창구 놓기 값의 끝이 둘이다. | 큐 → 놓기 값 변환은 한 함수 `CombatEffectPlacement.ForCue`만 한다. 행동 끝은 창구에서 "부르는 쪽이 거둠"이다(TP-E26). |
| R9 | 결함 예방 (낮음) | 지연이 있는 행동 끝 큐는 행동이 먼저 끝난 뒤에 낼 수 있다. | 행동 묶음이 이미 끝났으면 내지 않는다(TP-E27). |
| R10 | 무의미한 If (낮음) | 특성 처리기의 피드백 없음 갈래(`_feedback == null`)는 운영·확인 도구 모두 피드백을 넘겨 닿지 않는다. 행동 끝을 두 길에 연결해야 한다. | EL-f에서 피드백을 필수로 하고 그 갈래를 걷는다(TP-E28). 이 클래스를 만드는 곳은 사건 Presenter 하나이고 시험 참조가 없다. |
| R11 | 확인 (문제 없음) | 1판 TP-E4 "결과 끝에 남은 행동 끝 이펙트 거둠"이 행동 중에 끊지 않나 | 결과 끝 최종 사실(`Refresh`)은 재생 완료(조각 실행 포함) 뒤에만 온다. 그대로 둔다. |

- 점수(알려진 문제 기준, 예상): 1판 그대로 약 8.4 → 반영 뒤 약 9.1. 남는 것: 시간 끝은 쓰는 줄이 아직 없다(Q-EL1 A 결정으로 남김, Unity 확인 ① 뒤 다시 본다). 공격 모드 대상의 연결 길은 EL-d 착수 때 정한다(TP-E16).

## 1. 계획에서 정한 것 (사용자 확인 대상)

설계에 없거나 설계와 다르게 정한 것입니다.

### 1.1 위치 이동 행동 (EL-f, Q-EL4)

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-E1 | 큐 끝은 **자연·시간·행동 끝 셋**입니다. 열거 `CombatEffectEnd`: `Natural = 0`·`Seconds = 1`·`UnitAction = 2`. 시간 끝은 Q-EL1 A대로 남깁니다. 지금 쓰는 줄은 없고, Unity 확인 ①에서 폭발 연기가 남으면 폭발 큐 셋에 씁니다. | 사용자 "이펙트와 시간 동일하게 사용"을 값 하나로 지키려면 이펙트가 행동의 끝을 따라야 합니다. 시간 끝에 같은 초를 따로 적으면 두 값이 어긋날 수 있습니다(SSOT). |
| TP-E2 | 행동 끝은 **특성 발동 줄의 큐에서만**, 그리고 그 줄에 유닛 행동이 있을 때만 고를 수 있습니다. DB 생성 때 검사합니다. 공격·피격·사망·사실 순간·토네이도 큐는 고르면 DB 생성 실패입니다. | 행동이 없는 길에서는 끝이 오지 않습니다. |
| TP-E3 | 행동 끝 = **그 발동이 연 유닛 행동이 모두 끝날 때**입니다. 연 행동이 하나도 없으면(바쁨·건너뜀) 그 자리에서 바로 거둡니다. 큐는 지금처럼 행동 전에 냅니다(자리 = 움직이기 전 몸). | 위치 교환은 대상이 둘(이동자·승객)이고 이펙트 9가 두 몸에 나는데, 행동은 승객만 합니다. 발동 단위로 묶으면 두 이펙트가 같이 끝납니다. 몸마다 정하면 이동자 이펙트는 바로 사라집니다. |
| TP-E4 | 행동 끝 이펙트는 행동 완료 콜백에서 거둡니다. 결과 취소는 지금 `StopAll`이 지웁니다. 실행이 표시를 잃어 완료가 오지 않을 때를 위해, 결과 끝에 남은 행동 끝 이펙트를 모두 거둡니다. | 링 버퍼 이펙트(9)는 스스로 끝나지 않습니다. 남으면 지울 곳이 없습니다. |
| TP-E5 | 새 행동 자리 **`CombatUnitActionPlace.Relocate = 4`(위치 이동)**. 그 유닛의 **미뤄 둔 사실 자리**로 한 번 가고 돌아오지 않습니다. 쓰는 칸은 바깥 시간(`outSeconds`)과 걸음 모양(`look`)뿐입니다. 거리·머묾·돌아오는 시간은 0, 보기는 `Keep`이어야 합니다(DB 생성 때 검사). 도착 방향은 사실 방향입니다. | 칸이 뜻을 갖지 않으면 값으로 받지 않습니다. 사실 방향으로 끝나야 사실을 반영할 때 튀지 않습니다. |
| TP-E6 | `Relocate` 행동은 **미뤄 둔 배치가 있는 유닛만** 합니다. 없으면(이동자 — 이미 사실 자리) 경고 없이 건너뜁니다. 줄 값은 `Targets`로 둡니다. | 대상 둘 중 승객만 움직여야 합니다. 거리가 0인지로 판단하면 자리 계산 오차에 기대게 됩니다. |
| TP-E7 | **승객 배치 미루기(검토 R1·R2로 고침):** 보관은 보인 사실 하나(`CombatShownFacts`)다. ① 교환 쌍이 시작되면(이동자 걸음 시작) 승객 끝 사실을 쥔다(`HoldPlacement`). 이동자 도착 때 함께 반영하지 않는다. ② `Relocate` 행동이 그것을 가져간다(`TryClaimPlacement`). 행동 완료 콜백이 아직 가져간 상태이면 반영 + `Render`(서는 자리 대여가 옛 자리로 되돌린 바로 뒤, 같은 프레임). ③ 아무도 가져가지 않은 것은 사건 Presenter가 사건 처리기 시작 바로 뒤에 놓는다(`ReleaseUnclaimedPlacements` — 줄·행동이 없을 때의 안전장치, 지금의 순간 이동과 같음). 부르는 곳은 그 한 곳이다. ④ 그 유닛의 더 새 사실 반영은 쥔 것·가져간 것을 모두 버린다(새 사실이 이김). ⑤ 결과 끝·취소는 모두 버린다(최종 사실이 맞춤). `DeferPlacementPair`·`_arrivingPlacements`·`ShowCharacter`의 짝 갈래는 걷는다. 되돌리기 쌍(`Returned` 둘)은 지금처럼 바로 함께 반영한다. | 사건 순서가 이동자 걸음 → 승객 걸음 → 발동이라, 발동 순간 행동을 하려면 승객이 그때까지 옛 자리에 있어야 합니다. 특성 처리기는 위치 교환을 모르고 "`Relocate`는 미룬 배치를 가져간다"만 압니다. |
| TP-E8 | **차례로:** 이동자가 끝점에 닿은 뒤 발동까지는 둘이 겹쳐 보입니다. 승객 행동 동안 승객이 주체인 사건(출발 칸의 지뢰·가시 진입 등)은 지금 주체 막기 규칙대로 행동이 끝난 뒤 시작합니다. 이동자가 주체인 사건은 기다리지 않습니다. 주체 없는 사건은 모든 유닛 연출을 기다립니다. | 사용자 결정 "차례로". 막기 규칙은 고치지 않습니다(`CombatSubjectBlocking`). |
| TP-E9 | 위치 교환(440) 에셋: `MovementChanged` 줄의 큐 = 9·SubjectBody·끝 행동 끝, 행동 = `Targets`·`Relocate`·뛰기·바깥 시간 0.5초(Q-EL4 1판 권장 값, 에셋에서 조정). | 정리표에 시간이 없어 1판 권장 값을 씁니다. 승객이 이동자를 넘어 뛰는 것이 겹침보다 덜 어색합니다. 경로가 길면 출발 칸까지 호 하나입니다. |

### 1.2 나머지 (EL-a~EL-e·EL-g)

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-E10 | 창구 입구는 `Play(EffectType, Pose, CombatEffectPlacement)` 하나입니다. 놓기 값 = 따라갈 유닛(비면 그 자리)·끝(`Natural`·`Seconds(t)`·`Held`). 손잡이 `CombatEffectHandle`(`Recall()`)을 돌려줍니다. 행동 끝(EL-f)은 창구에서는 `Held`이고, 행동 묶음이 손잡이로 거둡니다. | 창구는 "언제 끝나나"의 판단을 모르게 합니다. 시간 끝만 받침이 직접 봅니다(매 프레임). |
| TP-E11 | 오래 보이는 이펙트 값은 새 값 하나를 상태 외형·상시 목록이 함께 씁니다: DB 입력 `CombatHeldEffectEntry`(종류·위치 보정·회전 보정) → 값 `CombatHeldEffect`. 보정 계산은 큐와 같은 `CombatCuePoints.Apply`입니다. | 같은 모양을 두 곳에 따로 적지 않습니다. |
| TP-E12 | HUD 규칙 함수는 `CombatUnitStatuses.IsShown(StatusEffectSnapshot)`(정적)입니다. `AddTo`와 유지 표시 부품이 함께 부릅니다. 설계의 가칭 `CombatUnitStatusRows.IsShown`은 쓰지 않습니다. | HUD 규칙의 주인이 이미 `CombatUnitStatuses`입니다. |
| TP-E13 | 다시 내기: 유지 표시 부품이 특성 발동 시작을 받습니다(`NotifyActivation(trait)`). 대상마다 지금 보인 상태 중 상태 외형 `restartOn`에 그 발동 종류가 있는 것을 거두고, 바로 뒤 `Render` 맞추기에서 새로 냅니다. 부르는 곳은 특성 피드백 시작(`CombatTraitFeedbackPlayback.Start`) 한 곳입니다. | 설계 EL4는 피드백이 `Restart(unit, key)`를 부르게 했으나, 그러면 피드백이 상태 외형 표를 읽어야 합니다. 표를 아는 곳을 부품 하나로 둡니다. |
| TP-E14 | "보유 동안" = 보인 사실의 그 유닛이 필드 위·살아 있음, `Stats.TraitStatuses`에 그 특성이 있고 `IsAvailable`(Q-EL5 A). 숨김(끌기·사망 유지)은 받침이 그리기만 끕니다(FE11 그대로). | 설계 EL3 표 그대로. 판정 함수는 부품 안 한 곳. |
| TP-E15 | 유지 표시 부품은 사건 Presenter가 이펙트 창구와 함께 만들고 `CombatShownFacts`에 넘깁니다. `CombatShownFacts.Render`가 유닛을 그린 바로 뒤 `Sync`를 부릅니다. 표 둘은 큐 값 답 `CombatCueSources`로 받습니다: 특성 외형은 이미 있고, 상태 외형 표를 더합니다(만드는 곳 `CombatDisplayTables`, 검토 R5). Factory 무변경. | Presenter 생성자 안 조립 규칙(분리 설계 Q-S3 A). 확인 도구도 같은 값 답을 씁니다(검토 R6). |
| TP-E16 | 공격 모드 대상: 표시 의도 `ICombatAttackModeTargets`(`Show(actor, ids)`·`Clear()`)를 공격 표시 의도 사슬(`ICombatBattleDisplay` → `CombatSceneDisplay`)에 붙이고, 씬 표시가 유지 표시 부품에 넘깁니다. 지우는 곳은 공격 조준의 지금 지우기 자리(모드 종료·`BeforeResult`·새 응답)를 함께 씁니다. 호버 중에도 유지합니다(Q-FS4 A). 정확한 연결 길은 EL-d 착수 때 코드로 다시 확인합니다. | 궤적·넉백 미리보기와 같은 사슬입니다. |
| TP-E17 | 끝나지 않는 프리팹 경고(Q-EL8 A)는 받침이 낸 인스턴스의 파티클을 보고 판단합니다. 끝이 자연인데 Looping이나 Ring Buffer가 있으면 종류마다 연결 동안 한 번 경고합니다. | View DB는 Render 표 프리팹을 읽지 않습니다. |
| TP-E18 | 결속 방향: 발동 줄 칸 `direction`(`CombatActivationDirection` `Any`·`Up`·`Down`)은 `DefenseChanged` 줄에서만 `Any`가 아닐 수 있습니다(DB 검사). 줄 찾기는 (종류, 방향) → (종류, `Any`)입니다. 다음 상태 사건이 없으면 `Any`만 봅니다. | 설계 EL7 그대로. |
| TP-E19 | 걷는 줄 칸(`keepWhileStatus`·`recallKinds`)과 그 값을 가진 에셋 YAML 두 줄(단독 행동 `ShieldGranted`, 굳건한 의지 `KnockbackApplied`)은 같은 커밋에서 고칩니다. 굳건한 의지 줄은 EL-c에서 상시 목록으로 옮길 때 지웁니다. EL-b에서는 그 줄의 `keepWhileStatus`만 걷고 큐는 남깁니다(EL-b~EL-c 사이 잠깐 순간 이펙트). | 설계 EL8 주의(모르는 칸은 Unity가 조용히 버림). |
| TP-E20 | EL-g는 착수 때 정리표를 다시 읽고 그 값으로 에셋을 고칩니다. 지금 계획의 줄 수(순간 15)는 18:09 기준입니다. | 사용자가 정리표를 더 고칠 수 있습니다. |
| TP-E21 | 행동 끝·`Relocate`가 아닌 기존 행동(희생 둘)은 바꾸지 않습니다. 희생 거리 0.4는 EL-g에서 값만 바꿉니다(Q-EL9 A). | 정리표 그대로. |
| TP-E22 | 대신 맞기 앞 항목(`CombatGuardLead`)의 희생 큐는 EL-c에서 에셋 큐를 걷으면 내는 것이 없어집니다. 코드 길은 남깁니다(다른 줄이 큐를 고를 수 있음). | 큐 줄 비우기만으로 보고 ①이 풀립니다. |
| TP-E23 | 유지 표시 부품(`CombatPersistentEffects`)은 목록 비교·내기·거두기만 합니다. 출처마다 작은 클래스가 "보여야 할 것"을 채웁니다: 계약 `ICombatPersistentEffectSource.Collect(shown, list)`, 구현 셋 — 상태(`CombatStatusEffectSource`, EL-b)·보유 동안(`CombatOwnedTraitEffectSource`, EL-c)·공격 모드 대상(`CombatAttackModeEffectSource`, EL-d). 같은 폴더 `Presentation/Effects/`. | 검토 R4. 새 조건 = 열거 값 + 출처 하나. |
| TP-E24 | 발동 줄 그리기(`DataBaseSystem/Editor/CombatBodyTextDrawer.cs`의 `TraitActivationVisualDrawer`, 사용자 작성)의 칸 목록을 같은 Phase에서 고칩니다: EL-b 두 칸 빼기, EL-e `direction` 더하기. 그 파일의 다른 줄은 건드리지 않습니다. | 검토 R3. 없는 칸을 찾으면 그리기가 예외로 멈춥니다. |
| TP-E25 | 창구가 종류마다 처음 낸 인스턴스로 한 번 판단해 기억합니다: 끝나지 않는 파티클이 있는지(경고, TP-E17)·링 버퍼가 있는지(회수 모양). 판단은 재사용 목록(`GetComponentsInChildren(List<T>)`)으로 합니다. | 검토 R7. 같은 종류는 같은 프리팹입니다. |
| TP-E26 | 큐 → 놓기 값 변환은 `CombatEffectPlacement.ForCue(cue, unit)` 한 함수입니다. 놓기 값의 끝은 `Natural`·`Seconds`·`Held`(부르는 쪽이 손잡이로 거둠)이고, 큐의 행동 끝(EL-f)은 `Held`가 됩니다. | 검토 R8. |
| TP-E27 | 행동 끝 큐가 지연 때문에 행동이 끝난 뒤에 나면 내지 않습니다(행동 묶음이 끝남을 기억). | 검토 R9. |
| TP-E28 | 특성 처리기는 피드백(`CombatTraitFeedbackPlayback`)을 필수로 받고, 피드백 없음 갈래를 걷습니다(EL-f). | 검토 R10. 만드는 곳이 사건 Presenter 하나이고 늘 넘깁니다. |
| TP-E29 | 시간 끝은 받침이 `LateUpdate`에서 `Time.deltaTime`을 빼 셉니다(파티클과 같은 시계). 새 Tick을 만들지 않습니다. | 받침은 이미 매 프레임 돕니다(따라가기). |
| TP-E30 | 그 자리 이펙트도 받침 위에 냅니다(EL5). 받침은 지금처럼 빈 것을 다시 써서 개수는 동시에 보이는 이펙트 수 정도입니다. | 손잡이·시간 끝·경고를 한 곳에서 다룹니다. |

## 2. Phase와 Task

### EL-a 창구 입구 하나·큐 끝 값 (EL1·EL5·EL6)

| Task | 할 일 |
|---|---|
| Ea.1 | `DataBaseSystem/Data/View/CombatPresentation/CombatEffectEnd.cs`(새, + `.meta`): `Natural = 0`·`Seconds = 1`. (`UnitAction = 2`는 EL-f) |
| Ea.2 | `CombatCueEntries.cs` `CombatEffectCueEntry`: 칸 `end`·`endSeconds`(툴팁), `ToCue()`. `CombatEffectCue`: `End`·`EndSeconds`, 시간 끝이면 `endSeconds > 0`(생성자 검사, 규칙 원본 `CombatPresentationValueRules`에 한 줄). |
| Ea.3 | `Presentation/Effects/CombatEffectPlacement.cs`·`CombatEffectHandle.cs`(새, + `.meta`): 놓기 값(따라갈 유닛·끝)과 손잡이(TP-E10). |
| Ea.4 | `CombatEffectWindow`: `Play(type, pose, placement)` 하나. 옛 `Play`·`PlayFollowing`은 이 입구로 옮기고 지움. `PlayHeld`·`RecallBy`·`SettleHeld`는 EL-b에서 지우므로 이 Phase에는 남김(쥠 규칙이 새 입구 위에서 돌게 한 줄 바꿈). |
| Ea.5 | `CombatEffectFollower`: 따라갈 유닛이 없으면 제자리(지금 그 자리 이펙트도 받침 위, TP-E30), 시간 끝(TP-E29), 손잡이 회수. 창구가 종류마다 한 번 판단(끝나지 않는 파티클 경고 TP-E17·링 버퍼, TP-E25). |
| Ea.6 | 부르는 곳 바꾸기: `CombatCueComposer`·`CombatEffectPiece`·`CombatAttackPlayback.PlayImpact`·`CombatMomentCuePlayback`·`CombatCharacterEventHandler`(피격·사망·토네이도)·`CombatGuardLead`·`CombatTraitEventHandler` 대체 길·`CombatTraitFeedbackPlayback`. 큐 값의 `End`를 놓기 값으로 넘김. |

확인(Ua, 사용자 Unity): ① 컴파일 ② 공격·피격·사망·오브젝트·토네이도·특성 발동 이펙트가 지금과 같음 ③ 단독 행동 보호막 유지·회수가 지금과 같음 ④ 폭발(Boom) 연기·불이 끝나는지(설계 §6 ①) ⑤ 경고가 나오는 종류 목록(콘솔) ⑥ 특성 연출 확인 도구에서도 ②가 같음(검토 R6).

### EL-b 유지 표시 부품·상태 외형 칸 (EL2·EL4)

| Task | 할 일 |
|---|---|
| Eb.1 | `CombatHeldEffectEntry`·`CombatHeldEffect`(새, + `.meta`, TP-E11). |
| Eb.2 | `UnitStatusVisualDataSO`·`UnitStatusVisualData`: `whileEffect`·`restartOn`(발동 종류 목록), 이펙트 종류 검사(`CombatPresentationEffectRules.IsKnown`). |
| Eb.3 | `CombatUnitStatuses.IsShown`(TP-E12), `AddTo`가 부름. |
| Eb.4 | `Presentation/Effects/CombatPersistentEffects.cs`·`ICombatPersistentEffectSource.cs`·`CombatStatusEffectSource.cs`(새, + `.meta`, TP-E23): 열쇠 (몸 유닛, 출처), `Sync(characters)`, `NotifyActivation(trait)`(TP-E13), `Clear`. 상태 외형 표는 `CombatCueSources`에 더함(TP-E15). |
| Eb.5 | `CombatShownFacts.Render` 끝에서 `Sync`. 생성·연결(TP-E15). 결과 취소·연결 해제 때 `Clear`. |
| Eb.6 | 걷기: `CombatEffectHold.cs`(+ `.meta`), `CombatTraitStatusPresence.cs`(+ `.meta`, 다른 쓰는 곳이 없으면), 창구 `PlayHeld`·`RecallBy`·`SettleHeld`, 특성 피드백 `HoldOf`·`SettleHeld`·발동마다 짝 회수, `CombatSceneEventPresenter.cs:184` 호출, 행 보관 `KeptActivationKinds`·`KeptKindsOf`, `TraitActivationVisual`의 `keepWhileStatus`·`recallKinds`(+ 검사), 발동 줄 그리기 칸 목록 두 줄(TP-E24). |
| Eb.7 | 에셋(같은 커밋, TP-E19): `UnitStatusVisual_Shield` `whileEffect` 8·`restartOn` [`ShieldReset`], 단독 행동(240) `ShieldGranted` 줄의 큐 8·`keepWhileStatus` 걷음, 굳건한 의지(330) 줄의 `keepWhileStatus`만 걷음. |

확인(Ub): ① 컴파일 ② 단독 행동: 보호막이 켜져 있는 동안 이펙트, 꺼지면 사라짐, HUD 아이콘과 같은 순간, 손패 왕복 뒤 다시 나옴 ③ 결속 보호막에도 같은 이펙트(이제 상태 외형이 정함) ④ 턴 끝 리셋 때 보호막 이펙트가 다시 재생(Q-EL3 A) ⑤ 끌기·사망 유지 동안 숨고 끝나면 다시 보임.

### EL-c 상시 목록 "보유 동안" (EL3)

| Task | 할 일 |
|---|---|
| Ec.1 | `DataBaseSystem/Data/View/Table/Trait/Item/CombatTraitAlwaysCondition.cs`(새, + `.meta`): `WhileOwned = 0`·`AttackModeTarget = 1`. |
| Ec.2 | `TraitVisualDataSO`·`TraitVisualData`: 목록 `alwaysEffects`(조건 + `CombatHeldEffectEntry`), 검사. |
| Ec.3 | 새 출처 `CombatOwnedTraitEffectSource.cs`(+ `.meta`): 상시 출처(특성, 보유 유닛, 항목 번호), 보유 동안 판정(TP-E14). |
| Ec.4 | 에셋: 희생(300) `DamageRedirected` 줄의 큐 11 걷음(행동 둘 그대로), 상시 [보유 동안 · 11]. 굳건한 의지(330) 290 줄 걷음, 상시 [보유 동안 · 10]. |

확인(Uc): ① 컴파일 ② 희생·굳건한 의지: 필드에 있는 동안 늘 이펙트, 손패·사망이면 사라짐 ③ 희생 행동 뒤 이펙트가 남지 않음(보고 ①) ④ 굳건한 의지 이펙트가 나옴(보고 ②).

### EL-d 상시 "공격 모드 대상" (EL3·EL4, 마무리 사격 FS-b)

| Task | 할 일 |
|---|---|
| Ed.1 | 표시 의도 `Input/Contracts/ICombatAttackModeTargets.cs`(새, + `.meta`) 또는 공격 표시 의도에 멤버 둘 — 착수 때 사슬 확인(TP-E16). |
| Ed.2 | `AttackTargetingInteraction`: 범위 응답의 `InstantDefeatTargetIds`로 `Show`, 지우는 자리에서 `Clear`. 다른 사람 줄이면 변경 정리에 적음. |
| Ed.3 | 새 출처 `CombatAttackModeEffectSource.cs`(+ `.meta`): 받은 목록을 들고, 부품이 `Show`·`Clear` 자리에서 한 번 맞추고 그 뒤 `Render`에서도 같은 목록으로. |
| Ed.4 | 시험 가짜 표시가 새 멤버로 컴파일이 깨지면 최소로 맞춤(다른 사람 시험이면 먼저 물음). |

확인(Ud): ① 컴파일 ② 마무리 사격 특성 외형에 상시 [공격 모드 대상 · 이펙트]를 임시로 넣고, 공격 모드에 들어가면 조건 맞는 적에게만 나옴 ③ 대상에 호버해도 유지 ④ 모드 끄기·공격하면 사라짐. 마무리 사격 이펙트 종류는 정리표에 없어, 고르기 전에는 에셋을 넣지 않습니다.

### EL-e 결속 방향 (EL7)

| Task | 할 일 |
|---|---|
| Ee.1 | `CombatActivationDirection.cs`(새, + `.meta`), `TraitActivationVisual` 칸 `direction`, 검사(TP-E18), 줄 찾기 열쇠, 발동 줄 그리기 칸 목록 한 줄(TP-E24). |
| Ee.2 | `CombatTraitFeedbackPlan.Build`: 다음 상태 사건 전·후 `CohesionDefenseBonus`로 방향, 발동마다 정한 방향 보관. |
| Ee.3 | `CombatActivationRows`: (종류, 방향) → (종류, `Any`) 찾기. |

확인(Ue): ① 컴파일 ② 결속 방어력이 오르면 오를 때 줄, 내리면 내릴 때 줄(임시 값으로).

### EL-f 위치 이동 행동 (EL11, Q-EL4)

| Task | 할 일 |
|---|---|
| Ef.1 | `CombatEffectEnd.UnitAction = 2`, 큐 값 검사(TP-E2): `TraitActivationVisual` 검사가 "행동 끝 큐가 있으면 행동이 있어야 함", 다른 큐 칸 검사가 행동 끝 거절. |
| Ef.2 | `CombatUnitActionPlace.Relocate = 4`(문서 주석), `CombatUnitActionData` 검사(TP-E5), 입력 툴팁. |
| Ef.3 | `CombatShownFacts`: `HoldPlacement(facts)`·`TryClaimPlacement(id, out facts)`·`ReleaseClaimedPlacement(id)`(아직 가져간 상태면 반영 + `Render`)·`ReleaseUnclaimedPlacements()`, `ApplyCharacter`·`ApplyTraitState`가 그 유닛의 쥔 것·가져간 것을 버림, `ForgetPlacementPairs` → `ForgetHeldPlacements`. `_arrivingPlacements`·`DeferPlacementPair`·`ShowCharacter` 짝 갈래 걷음(TP-E7). 사건 Presenter가 사건 처리기 시작 바로 뒤 `ReleaseUnclaimedPlacements` 한 줄. |
| Ef.4 | `CombatMovementPresentation.BeginStep`: 쌍 시작에서 `HoldPlacement(pair.Partner.Final)`. 승객 걸음은 지금처럼 바로 완료. |
| Ef.5 | `CombatUnitActionComposer.ComposeRelocate(actor, action, destination, hopHeight)`: 서는 자리 대여 + 이동 조각 하나(돌아오기 없음). 자리 계산은 `CombatUnitActionPlaces`에 `Relocate` 갈래(목표 자세를 받아 그대로). |
| Ef.6 | `CombatTraitEventHandler`: `Relocate`면 `TryClaimPlacement`가 되는 유닛만(TP-E6), 목표 = `StandingPoseOf(가져간 사실)`, 완료 콜백 = `ReleaseClaimedPlacement`. 시작하지 못하면 가져간 것을 돌려놓음(③이 놓음). 피드백 필수·없음 갈래 걷기(TP-E28). |
| Ef.7 | 행동 끝 이펙트 묶음 `Playback/Support/CombatActivationActionGroup.cs`(새, + `.meta`): 발동 하나의 행동 수·손잡이, 마지막 행동 완료 또는 시작한 행동 0이면 거둠, 끝난 뒤 늦게 나는 큐는 내지 않음(TP-E27), 결과 끝에 남은 것 거둠(TP-E3·TP-E4). 특성 피드백이 행동 끝 큐를 `Held`로 내고 손잡이를 묶음에 넣음. |
| Ef.8 | 에셋(TP-E9): 위치 교환(440) `MovementChanged` 줄. |
| Ef.9 | 특성 시스템 View 계획 TS-h에 "대신함" 표시(이 계획 작성 때 이미 함). |

확인(Uf): ① 컴파일 ② 위치 교환: 이동자가 끝점까지 걷고, 승객이 그 뒤 출발 칸으로 한 번 뛰어감 ③ 이펙트 9가 두 유닛에 나고 승객이 도착할 때 같이 사라짐 ④ 이펙트 9가 승객을 따라감 ⑤ 경로가 여러 칸일 때 승객이 출발 칸까지 호 하나 ⑥ 출발 칸에 지뢰·가시가 있으면 승객이 도착한 뒤 발동 ⑦ 교환 직후 되돌리기는 지금처럼 둘이 바로 돌아옴 ⑧ 줄의 행동을 비우면 승객이 발동 순간 순간 이동(안전장치).

### EL-g 정리표 값을 에셋으로 (EL8)

| Task | 할 일 |
|---|---|
| Eg.1 | 정리표를 다시 읽음(TP-E20). |
| Eg.2 | 순간 줄: 발동 줄·큐(종류·자리·끝 자연), 결속은 방향 둘. 없는 발동 줄은 새로. |
| Eg.3 | 상태 외형 약점 노출(`UnitStatusVisual_WeaknessExposure`) `whileEffect` 12. 사냥꾼의 표식(190)·정찰 부대(450)의 상태 동안 줄은 만들지 않음(상태 외형이 맡음). |
| Eg.4 | 희생 행동 거리 0.4(Q-EL9 A). |

확인(Ug): 설계 §6의 2·5·9·10, 정리표 줄마다 한 번씩.

### EL-h 문서·공유·묶음 끝 검토

| Task | 할 일 |
|---|---|
| Eh.1 | 호진님 View DB 칸 메모(상태 외형 칸 둘·특성 외형 상시 목록·발동 줄 방향·큐 끝·행동 자리 `Relocate`). |
| Eh.2 | [이펙트·소리 창구 안내](../handoffs/2026-10-29-combat-effect-sound-cue-guide.md) 고침(끝 셋·유지 이펙트 자리). |
| Eh.3 | "대신함" 표시: 따라가기·회수 설계 FE4·FE6·FE10, 마무리 사격 설계 FS2·FS3, 도발 설계 TT-b 유지 이펙트. |
| Eh.4 | 폴더 배치 안내(새 파일 자리), 설계 구현 메모. |
| Eh.5 | 검토 에이전트 하나(7기준), 보고. |

## 3. 다른 사람 줄과 공유

| 곳 | 누구 | 할 일 |
|---|---|---|
| `AttackTargetingInteraction`(EL-d) | 착수 때 blame 확인 | 바꾼 줄을 변경 정리에 적어 넘김 |
| Factory(TP-E15) | 민형님 | 바뀌면 Factory 공유 문서에 새 절 |
| View DB 칸(EL-b·EL-c·EL-e·EL-f) | 호진님 | Eh.1 |
| 시험 가짜(EL-d) | 착수 때 확인 | 다른 사람 시험이면 고치기 전에 물음 |

## 4. 커밋 묶음 (사용자)

- EL-a: 새 `.cs` 셋(+ `.meta`)과 부르는 곳 변경은 한 커밋.
- EL-b: 코드와 에셋 YAML(상태 외형 보호막·단독 행동·굳건한 의지 줄)은 한 커밋(TP-E19).
- EL-c·EL-e·EL-f: 각 Phase의 SO 코드와 에셋은 한 커밋.
- 새 파일은 `.meta`와 함께.

## 5. 실행 기록

### 5.1 EL-a 코드 완료 (2026-10-31, 사용자 "검토 후에 보완점 반영해서 작업 시작해", 새 시험 없음, 시험 변경 없음, 에셋 변경 없음, Ua 문제 없음 — 2026-11-01 사용자, 커밋 `fc596cbf9`)

- 새 파일 셋(+ `.meta`):
  - `DataBaseSystem/Data/View/CombatPresentation/CombatEffectEnd.cs` — `Natural = 0`·`Seconds = 1`.
  - `ViewSystem/Combat/Presentation/Effects/CombatEffectPlacement.cs` — 놓기 값(따라갈 유닛·시간 끝·부르는 쪽이 거둠), 큐 변환 `ForCue` 한 곳(TP-E26), `AsHeld`(EL-b·EL-f가 씀).
  - `ViewSystem/Combat/Presentation/Effects/CombatEffectHandle.cs` — 손잡이(값 형식, 받침 사용 번호로 다른 이펙트를 거두지 않음).
- 바꾼 파일:
  - `CombatCueEntries.cs`: 이펙트 큐 입력 칸 `end`·`endSeconds`(툴팁), 값 `End`·`EndSeconds`. 시간 끝이면 초 > 0(값 생성자 → DB 생성 때 검사). 다른 끝의 초는 보관하지 않음.
  - `CombatEffectWindow.cs`: 입구 `Play(type, pose, placement)` 하나(옛 `Play(type, 위치, 회전)`·`PlayFollowing` 지움). 그 자리 이펙트도 받침 위(TP-E30). 종류마다 처음 낸 인스턴스로 끝나지 않는 파티클·링 버퍼를 한 번 판단해 기억(재사용 목록, TP-E25), 자연 끝이면 종류마다 한 번 경고(TP-E17). `PlayHeld`·`RecallBy`·`SettleHeld`는 EL-b까지 그대로(같은 내기 함수 위).
  - `CombatEffectFollower.cs`: 사용 번호·손잡이·`Recall(use)`, 시간 끝(`LateUpdate`에서 `Time.deltaTime`, TP-E29), 링 버퍼 판단을 창구 값(`Adopt`)으로(낼 때마다 `GetComponentsInChildren` 걷음).
  - `CombatEffectPiece.cs`: 따라가기 칸 대신 놓기 값. `At(effects, cue, at, unit)` 하나(옛 `At(effects, effect, at)`·`Following` 지움), 클립 지점·발사 지점도 큐의 끝을 따름.
  - `CombatCuePoints.cs`: 몸·칸 자리 조각을 `At(effects, cue, at, unit)` 하나로.
  - `CombatAttackPlayback.PlayImpact`: 접촉 이펙트도 같은 입구·큐의 끝.
- 계획과 다른 점: 없음. 기차 이펙트(`FieldRailwayPresenter`)는 큐가 아니라 기차 몸에 붙는 자기 Scope라 이 창구 밖이다(바꾸지 않음).
- 화면 변화 없음(모든 큐가 자연 끝, 칸이 없는 에셋은 기본값 0 = 자연). 끝나지 않는 파티클 경고는 새로 나온다 — 지금 자연 끝으로 쓰는 7·8·9·10·11·12·2 종류 가운데 실제로 낸 것마다 한 번(설계 §1.3).
- 확인(Ua): §2 EL-a 표 아래 ①~⑥.

### 5.2 EL-b 코드·에셋 완료 (2026-11-01, 사용자 "확인했어" = Ua 문제 없음 → EL-b, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

- 새 파일 넷(+ `.meta`):
  - `DataBaseSystem/Data/View/CombatPresentation/CombatHeldEffect.cs` — 입력 `CombatHeldEffectEntry`(종류·위치/회전 보정, Empty면 값 없음)·값 `CombatHeldEffect`(TP-E11).
  - `ViewSystem/Combat/Presentation/Effects/ICombatPersistentEffectSource.cs` — 출처 계약·열쇠 `CombatPersistentEffectKey`(몸 유닛 + 상태 키 또는 특성·보유 유닛·항목)·`CombatPersistentEffectWant`(TP-E23).
  - `…/Presentation/Effects/CombatStatusEffectSource.cs` — 상태 출처(필드 위 살아 있는 유닛, HUD와 같은 규칙, 상태 외형 `WhileEffect`).
  - `…/Presentation/Effects/CombatPersistentEffects.cs` — 유지 표시 부품(`Sync`·`NotifyActivation`·`Clear`, 재사용 목록, 밖에서 끝난 이펙트는 다음 맞추기에서 다시 냄).
- 바꾼 파일:
  - 상태 외형 `UnitStatusVisualData`·`SO`: 칸 `whileEffect`·`restartOn`(툴팁), 검사(다시 내기 종류 정의·겹침 없음, 유지 이펙트 없이 다시 내기 금지).
  - `CombatUnitStatuses.IsShown`(정적, TP-E12) — `AddTo`와 상태 출처가 함께 부름.
  - `CombatCuePoints.Apply(pose, 위치 보정, 회전 보정)` — 보정 규칙 원본 하나(큐 판은 이것을 부름).
  - `CombatEffectPlacement.Following(unit)`.
  - `CombatCueSources`: 생성 인자·속성 `UnitStatuses`(만드는 곳 `CombatDisplayTables`, TP-E15).
  - `CombatShownFacts`: 생성 인자 `persistent`, `Render` 끝에서 `Sync`(몸 읽기 대리자는 한 번 만듦), `Clear`가 유지 이펙트도 거둠.
  - 사건 Presenter: 유지 표시 부품·상태 출처를 만들어 보인 사실·특성 피드백에 넘김, `Refresh`의 결과 끝 다시 내기 호출 걷음.
  - 특성 피드백: 창구 대신 유지 표시 부품, 발동 시작에서 `NotifyActivation`(TP-E13), 쥠 규칙·결과 끝 다시 내기·발동 짝 회수 걷음.
- 걷은 것: `CombatEffectHold.cs`·`CombatTraitStatusPresence.cs`(+ `.meta`), 창구 `PlayHeld`·`RecallBy`·`IsShown`·`SettleHeld`, 받침의 쥠 규칙(상태 규칙·발동 짝·결과 끝 맞추기, 매 프레임 상태 검사), 조각 `Held`, 큐 붙이기·순간 큐의 쥠 인자, 줄 보관 `KeptKindsOf`·줄 값 `KeepWhileStatus`·`RecallKinds`·`HoldsEffects`, 큐 값 답·특성 외형 `KeptActivationKinds`, 발동 줄 칸 `keepWhileStatus`·`recallKinds`(+ 검사), 발동 줄 그리기 칸 목록 두 줄(TP-E24).
- 에셋(코드와 한 커밋, TP-E19):
  - `UnitStatusVisual_Shield`: `whileEffect` 8(DefensiceShield)·`restartOn` [240 `ShieldReset`].
  - 단독 행동(`TraitVisual_SoloAction`) `ShieldGranted` 줄: 큐 8 걷음(몸 글자는 그대로), `keepWhileStatus`·`recallKinds` 줄 걷음.
  - 굳건한 의지(`TraitVisual_SteadfastWill`) 290 줄: `keepWhileStatus`·`recallKinds` 줄만 걷음(큐는 EL-c에서 상시 목록으로 옮길 때 걷음).
- 계획과 다른 점: 없음. 다시 내기는 원하는 것 값에 다시 내기 발동 목록을 실어 부품이 판단한다(상태 출처만의 특별 입구를 두지 않음, TP-E13·TP-E23의 구현 모양).
- 화면 차이: 보호막 이펙트를 단독 행동 줄이 아니라 상태 외형이 정해서, **결속(290) 보호막에도 같은 이펙트가 나온다**. 턴 끝 리셋(`ShieldReset`)마다 보호막 이펙트가 다시 재생된다(Q-EL3 A).
- 확인(Ub): §2 EL-b 표 아래 ①~⑤, 그리고 ⑥ 특성 연출 확인 도구에서 보호막 유지·회수, ⑦ 특성 외형 에셋 인스펙터(발동 줄)가 예외 없이 열리는지.


### 5.3 EL-c~EL-h 한 번에 (2026-11-01, 사용자 "그냥 한번에 진행하고 Patch 파일 하나로 주셈", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

사용자 지시로 Phase마다 멈추지 않고 EL-c부터 EL-h까지 이어 했고, 확인(Uc~Ug)은 끝에 한 번에 받습니다. 기준 판은 원격 `d297a96ef`(EL-a 커밋 `fc596cbf9` 뒤 사용자·민형님 커밋을 빨리 감기로 받음, 겹치는 파일 없음)이고 EL-b도 같은 패치에 들어 있습니다.

#### EL-c 상시 "보유 동안"

- 새 파일(+ `.meta`): `DataBaseSystem/Data/View/Table/Trait/Item/CombatTraitAlwaysCondition.cs`(`WhileOwned = 0`·`AttackModeTarget = 1`), 같은 폴더 `TraitAlwaysEffect.cs`(입력 `TraitAlwaysEffectEntry` = 조건 + `CombatHeldEffectEntry`, 값 `TraitAlwaysEffect`), `ViewSystem/Combat/Presentation/Effects/CombatOwnedTraitEffectSource.cs`.
- `TraitVisualData`: 칸 `alwaysEffects`(툴팁)·`AlwaysEffects`. 값은 DB 생성 검사(`Validate`)가 한 번 만들어 보관합니다(`[NonSerialized]`). 유지 표시 부품은 "같은 이펙트"를 값(종류·보정, `CombatHeldEffect.SameAs`)으로 가립니다(묶음 끝 검토 L1 — 처음에는 같은 객체로 가렸음). 빈 항목·Empty 이펙트·정의되지 않은 조건은 DB 생성 실패.
- `CombatCueSources.Traits`(특성 외형 표), 사건 Presenter의 출처 목록에 보유 동안 출처.
- 에셋: 희생(300) `DamageRedirected` 줄 큐 11 걷음(행동 둘 그대로) + 상시 [보유 동안 · 11], 굳건한 의지(330) 290 줄 걷음 + 상시 [보유 동안 · 10]. 희생 행동 거리 0.4(EL-g 값, 같은 파일이라 여기서).

#### EL-d 상시 "공격 모드 대상"

- 새 파일(+ `.meta`): `Input/Contracts/ICombatAttackModeTargets.cs`(`ShowAttackModeTargets(actor, ids)`·`ClearAttackModeTargets()`), `Presentation/Effects/CombatAttackModeEffectSource.cs`.
- 사슬: 공격 조준 `AttackTargetingInteraction`(새 필수 인자) → 씬 표시(`ICombatBattleDisplay`가 상속) → 사건 Presenter `ShowAttackModeTargets`·`ClearAttackModeTargets` → 출처가 바뀌었을 때만 보인 사실 `SyncPersistent`(유닛을 다시 그리지 않고 유지 이펙트만 맞춤).
- 보이는 때: 범위 응답 성공(`InstantDefeatTargetIds`). 지우는 때: 범위 실패 응답·결과 재생 직전(`BeforeResult`)·모드 정리(`ResetRangeAnswer`). 호버로는 지우지 않습니다(Q-FS4 A).
- Factory 연결 줄 하나(`CombatSceneInputFactory`, 민형님 공유 §45). 마무리 사격 에셋은 이펙트 종류가 정리표에 없어 넣지 않았습니다(Ud ②는 임시 값으로 확인).
- 계획과 다른 점: 계약 멤버 이름 `Show`/`Clear` → `ShowAttackModeTargets`/`ClearAttackModeTargets`(씬 표시의 `Clear()`와 겹침).

#### EL-e 결속 방향

- 새 파일(+ `.meta`): `DataBaseSystem/Data/View/Table/Trait/Item/CombatActivationDirection.cs`(`Any`·`Up`·`Down`).
- 발동 줄 칸 `direction`·`AllowsDirection`(규칙 원본 하나 — `DefenseChanged`만), 검사(방향 정의·종류 제한·같은 (종류, 방향) 두 줄), 특성 외형 `TryGetActivation(kind, direction, …)`·`Activations`·`TryGetFirstActivation`(도구용), 발동 줄 그리기 칸 목록에 `direction`(TP-E24).
- 줄 찾기: 큐 값 답 (종류, 방향) → 발동 줄 보관이 없으면 (종류, Any)(TP-E18). 몸 글자 프리팹 모으기는 줄을 모두 훑음.
- 방향: 피드백 계획 `DirectionOf`가 발동 뒤 그 발동 유닛의 사실 사건 앞뒤 `CohesionDefenseBonus`를 비교해 처음 바뀐 쪽으로 정하고, 발동(`Occurrence`)이 고른 줄을 들고 있습니다. 피드백(큐·몸 글자)과 특성 처리기(행동·추가 공격 표시)가 같은 줄을 씁니다(`RowOf`).
- 계획과 다른 점: 비교할 사건을 "다음 상태 사건"뿐 아니라 그 유닛의 캐릭터 사건까지 봅니다. 손패 복귀·사망 정리의 `DefenseChanged`는 상태 사건 없이 캐릭터 사건이 뒤따라서, 상태 사건만 보면 늘 방향 모름(Any)이 됩니다. 특성 연출 확인 도구의 검사(`PreviewTraitFeedbackValidation`)도 방향을 받아 (종류, 방향) → (종류, Any)로 찾습니다(묶음 끝 검토 L2, 방향을 보지 않는 계획 만들기는 걷음).

#### EL-f 위치 이동 행동

- 새 파일(+ `.meta`): `Presentation/Effects/CombatEffectActionGroup.cs`(발동 하나의 행동 끝 이펙트 묶음).
- 값: `CombatEffectEnd.UnitAction = 2`(큐 규칙: 특성 발동 칸만, 줄 검사: 유닛 행동이 있는 줄만 — TP-E2), `CombatUnitActionPlace.Relocate = 4`(값 검사: 거리·머묾·돌아오기 0·보는 쪽 Keep — TP-E5), 입력·큐 툴팁.
- 창구: 놓기 값 `ActionBound`(행동 끝 → 부르는 쪽 거둠), 창구가 그 손잡이를 모아 결과 끝 `RecallActionBound`(사건 Presenter `Refresh`, TP-E4), 결과 취소 `StopAll`이 목록을 비움.
- 조각·조립: 이펙트 조각이 묶음을 받아 행동이 끝난 뒤 늦게 온 큐는 내지 않음(TP-E27), 큐 붙이기 받는 곳 `CombatCueSinks.ActionGroup`, 순간 큐 `PlayAccent(…, actionGroup)`, 특성 피드백 `Start(…, actionGroup)`.
- 보인 사실: `HoldPlacement`·`TryClaimPlacement`·`UnclaimPlacement`·`ReleaseClaimedPlacement`·`ReleaseUnclaimedPlacements`·`ForgetHeldPlacements`(상태는 새 보관 `Playback/Support/CombatDeferredPlacements`(+ `.meta`)가 갖고 보인 사실은 반영·그리기만 — 묶음 끝 검토 M3), `ApplyCharacter`가 그 유닛의 쥔·가져간 배치를 버림(R2). `DeferPlacementPair`·`_arrivingPlacements`·`ShowCharacter` 짝 갈래 걷음(TP-E7). 이동 표시가 교환 쌍 시작에서 쥠, 사건 Presenter가 사건 처리기 시작 바로 뒤 한 곳에서 놓음.
- 행동: `CombatUnitActionComposer.ComposeRelocate`(서는 자리 대여 + 이동 조각 하나), 자리 계산은 `Relocate`를 받지 않음(끝 자리는 미뤄 둔 사실 자리), 대신 맞기에는 `Relocate`를 넣지 않음(앞 항목이 경고 한 번 뒤 뺌).
- 특성 처리기: 피드백 필수·없음 갈래 걷음(TP-E28), 생성 인자 `cues`·`rows` 걷음(줄은 피드백이 답함), 발동마다 행동 끝 묶음(큐 → 행동 열기 → 닫기), `Relocate`는 가져간 유닛만 그 사실 자리로(완료 = 반영 + 묶음 닫기, 시작 못 하면 돌려놓고 경고 한 번 → 순간 이동).
- 에셋: 위치 교환(440) `MovementChanged` 줄 큐 9 · SubjectBody · 끝 UnitAction, 행동 Targets · Relocate · 뛰기 · 0.5초(TP-E9).
- 계획과 다른 점: 묶음 파일 자리·이름 `Playback/Support/CombatActivationActionGroup` → `Presentation/Effects/CombatEffectActionGroup`(이펙트 조각이 묶음을 읽어 Presentation → Playback 의존이 생기지 않게). 대신 맞기(`DamageRedirected`) 줄의 행동 끝 큐는 세는 곳이 없어 DB 생성에서 막습니다(묶음 끝 검토 M1).

#### EL-g 정리표 값

- 정리표(18:09, 다시 읽음 — TP-E20, 바뀐 줄 없음)대로: 순간 줄 15(호진님 메모 §2), 상태 외형 약점 노출 `whileEffect` 12, 희생 거리 0.4. 범용 버프·디버프는 Render 종류 13 `Buff`·14 `DeBuff`.
- 사냥꾼의 표식(190)·정찰 부대(450)의 상태 동안 줄은 만들지 않았습니다(상태 외형이 맡음). 두 줄에 이펙트가 없음을 확인했습니다.

#### EL-h 문서·공유

- 민형님 Factory 공유 §45, 새 호진님 View DB 칸 메모 `handoffs/2026-11-01-combat-effect-structure-hojin-share.md`, 이펙트·소리 창구 안내(§2.2 칸·§2.9 오래 보이는 이펙트·§3 값·§6 위치), "대신함" 표시(따라가기·회수 설계 FE4·FE6·FE10, 마무리 사격 설계 FS2·FS3, 도발 설계 켜짐 판정 줄), 폴더 배치 안내(새 파일·규칙 줄), 설계 §10 구현 메모. 모두 미전달.
- 특성 연출 확인 도구(사용자 작성 `Dev/TraitPreview/TraitActivationPreviewHost.cs`)의 줄 목록·설명이 방향 줄만 있는 종류(결속 340)도 보이게 `TryGetFirstActivation`으로 바꿈(세 줄).

#### 묶음 끝 검토 (2026-11-01, 새 에이전트 하나, 7기준)

약 8.4(높음 0, 중간 3, 낮음 11). 반영한 것(같은 패치, 컴파일 미확인):

- M1 대신 맞기 줄의 행동 끝 큐가 결과 끝까지 남음 → 발동 줄 검사가 `DamageRedirected` 줄의 끝 UnitAction을 막음.
- M2 위치 이동 행동 사이 승객의 더 새 사실이 와서 가져간 배치가 버려지면, 대여가 옛 자리로 되돌린 뒤 다시 그리지 않아 옛 칸에 남음 → `ReleaseClaimedPlacement`가 늘 한 번 그림.
- M3(일부) 미뤄 둔 배치 상태가 보인 사실에 섞임(SRP) → 보관 `CombatDeferredPlacements`로 뺌. "모든 사건 시작에서 놓음"·Core 사건 순서에 기댐은 그대로(놓는 곳은 한 곳, 다른 원인의 교환은 옛 순간 이동과 같음).
- L1 확인 도구의 `Validate`가 같은 값 객체를 새로 만들면 유지 이펙트가 다시 재생될 수 있음 → 값 비교 `CombatHeldEffect.SameAs`.
- L2 확인 도구 검사가 방향 줄(결속)을 못 봄 → 방향을 받아 찾음, 몸 글자 프리팹 검사도 그 종류의 줄 모두.
- L3 "행동 끝 큐가 있나" 판단이 두 곳 → 큐 묶음 `CombatEventCues.EndsWithAction` 하나(줄 검사·특성 처리기).
- L6 늘 참인 검사 걷음(위치 이동의 `_mapper() != null` — 사건 시작이 이미 요구, 그래서 특성 처리기 생성 인자 `mapper`도 걷음; 특성 외형 표 조회 뒤 `visual == null`).
- L7 `HoldPlacement`가 넣은 뒤 예외 → 먼저 검사(보관 `Hold`).
- L9 상태 외형 값 주석("조회마다 새로") 바로잡음, 유지 이펙트 값 오류에 상태 키를 붙임.

그대로 둔 것(보고만): L4 행동이 하나도 열리지 않은 발동의 행동 끝 이펙트는 0프레임(줄 검사는 행동 있음만 봄 — 런타임 바쁨·가져갈 배치 없음), L5 그리기마다 상태 목록 읽기 할당(`StatusEffectReadModel.Capture`), L8 큐 값 답이 표 둘을 그대로 냄, L10 반복하지 않는 유지 이펙트 프리팹은 끝나면 다음 그리기에서 다시 재생(경고 없음 — 끝나지 않는 파티클 경고의 반대 경우), L11 계획 파일 이름 다름(위 EL-f 기록). 예상 반영 뒤 약 8.9.

#### 확인(사용자 Unity, 한 번에)

- 컴파일, Ub ①~⑦(§5.2), Uc ①~④, Ud ①~④(마무리 사격 상시 [공격 모드 대상 · 아무 이펙트]를 임시로), Ue ①~②(결속 오를 때 13·내릴 때 14), Uf ①~⑧, Ug(정리표 줄마다 한 번), 특성 연출 확인 도구에서 같은 것.

### 5.4 로컬 Unity 검증 (2026-11-01, Codex, 전체 통과 아님)

사용자 요청으로 인계 문서를 읽고 실제 에디터에서 검증했다. 기준은 `36a6d913911f03c6ed0e9a903ff4ef9344a68865`와 시작 때부터 있던 작업 트리다. 기존 `Unit_ShotGun`의 희생·굳건한 의지·단독 행동 설정과 FMOD 캐시 변경은 보존했다. 새 시험 코드·기존 시험 수정·MSBuild·커밋·push는 하지 않았다.

#### 컴파일·기존 시험

- Unity **6000.0.73f1 / DX11**에서 Asset Refresh 후 에디터 컴파일 성공. `Editor.log`의 빌드 성공과 Play 진입을 확인했다. Core `CombatCommandEffects.cs:144`의 CS8632 경고는 남아 있다.
- Unity Test Runner **EditMode 전체 5,296건: 성공 5,293 / 실패 3 / 건너뜀 0**, 128.103초. 결과 XML은 로컬 `Logs/CodexVerification/2026-11-01-effect-structure/editmode-results.xml`, 실패 상세는 같은 폴더 `test-failures.json`이다(Logs는 Git 제외).
- 관련 묶음: `StructureHitPresentationTests` 22/22, `MovementPassiveUndoTests` 3/3, `MovementUndoLifetimeTests` 16/16, `StatusEffectContractTests` 8/8, `StatusEffectReadModelTests` 5/5, `StatusEffectRetirementTests` 4/4, `StatusEffectPublicationTests` 9/9, `TauntTraitTests` 8/8, `TauntPublicationTests` 2/2, `DataBaseAssetTests` 9/9. `TraitActivationOutcomeTests`는 68/69다.

| 실패 시험 | 실제 실패 | 처리 |
|---|---|---|
| `FbxPlacementTests.PrefabCatalog_ListsRegularAndVariantAndAddsAnIndependentVolumeProfile` | `EditorWindow.Close()`의 NullReferenceException, `Assets/Tests/Editor/FbxPlacement/FbxPlacementTests.cs:690` | 도구 창 닫기 경로. 실행 뒤 `FBX 맵 작업실`의 Invalid editor window 메시지도 관찰. 시험·도구 코드 무변경 |
| `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)` | 발동 개수 기대 1 / 실제 2, `Assets/Tests/EditMode/TraitActivationOutcomeTests.cs:618` | CT-a에서 도발 `StatusApplied`가 추가된 계약과 시험의 전체 도발 발동 개수 단정이 충돌하는 것으로 판단. Core 인계 §62.2에 검토 요청, 시험 무변경 |
| `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", False)` | 해당 Cinemachine 타입을 못 찾는 TypeLoadException, `Assets/Tests/EditMode/CoreIntegration/CombatCameraZoomPoseTests.cs:247` | FieldCamera 담당 영역이므로 무변경 |

세 시험은 `36a6d91`에서 수정한 파일이 아니다. 부모 커밋을 별도 실행하지 않았으므로 세 실패의 최초 발생 커밋까지 확정한 것은 아니다. 전체 시험은 프리팹 보완 전에 한 번 실행했고, 보완 후에는 해당 이펙트의 실제 재생·DB 진입·에셋 참조를 다시 확인했다.

#### 실행에서 찾고 보완한 것

`Scenario_PositionSwapRestore`가 이펙트 9 생성 때 `ArgumentException: This Object is not have particleStopNotifier`로 중단됐다. `EffectPool.ElementNode`는 루트의 `ParticleStopNotifier`를 필수로 요구한다. 등록된 이펙트 프리팹을 대조해 같은 누락 3개를 보완했다.

- `Assets/ProjectL/Script/Render/Effect/BuffeEffect/ForcedMovement/ForcedMovement.prefab` (9)
- `Assets/ProjectL/Script/Render/Effect/BuffeEffect/Execution/Execution.prefab` (7)
- `Assets/ProjectL/Script/Render/Effect/AttackEffect/Stage1BossAttack/AttackEffect.prefab` (6)

각 루트에 **기존 `ParticleStopNotifier` 컴포넌트 하나만** 추가했다(각 13줄). 스크립트·파티클 값·에셋 GUID·`.meta`·종료 정책은 바꾸지 않았다. 9는 위치 교환 전체 재생, 7은 운영 공격 모드 대상 표시, 6은 포효 단독/시나리오 재생으로 생성 예외가 사라진 것을 확인했다. 종류 2~14의 등록 프리팹 모두 필수 알림을 갖는다. 13 `DefaultRed`는 14 `DefaultBlue`의 프리팹 Variant에서 상속한다(컴포넌트 제거 없음). 근거는 로컬 `effect-prefab-inventory.json`·`position-swap-failure.log`다.

SSOT·SOLID·FW·독립성: 보완은 기존 풀 계약을 에셋이 충족하게 한 것이다. 새 서비스/루트 Export/분기/폴링이나 Core 규칙을 추가하지 않았다. 조립 경로와 이펙트 수명 책임은 기존 설계 그대로다. 관련 없는 FieldCamera·Architecture·Core 구현은 수정하지 않았다.

#### 수동 재생 결과와 한계

운영 경로는 기존 Play From Boot 설정의 Database / Test(10000) / 로드아웃 1 / 챕터 1 / 노드 2로 확인했다. 연출 도구는 합성 결과이므로 Core 계산 검증과 구분한다.

| 범위 | 확인한 것 | 남은 것 |
|---|---|---|
| Ub 상태 유지 | `Scenario_ShieldHistory`의 두 출처 합산·적격 보호막 소진 때 HUD/이펙트 회수·예산 복원 때 재표시. 운영 단독 행동의 손패 왕복·인접 아군에 따른 해제/복구·턴 종료 리셋·집기 숨김/취소 복구. 결속 보호막 표시. 합성 사망 후 상태 이펙트 회수. 특성 외형 Inspector 열기 | 사망 연출 중 숨김을 프레임 단위로 대조하지는 않음 |
| Uc 보유 동안 | 운영 희생 11·굳건한 의지 10 상시 표시, 집기/취소·손패 복귀/재배치, 이동/되돌리기 후 추적. 합성 WhileOwned 출처의 사망 후 회수 | **실제 희생 가로채기 행동 뒤 잔류(Uc③)** 및 두 특성 각각의 운영 사망 재현은 미완료. 합성 사망은 도구가 보유를 지원하는 PositionSwap에 WhileOwned 11을 임시로 넣어 공통 출처를 검증 |
| Ud 공격 모드 대상 / CT-b | Ballista에 FinishingShot을 임시 부여(문턱 1000%), 외형에 AttackModeTarget 7 임시 설정. 공격 모드에서 여러 적에 표시·제외 대상 미표시, 호버 유지, 모드 해제 회수, 재진입 복구, 실제 공격 결과에서 회수·처치 완료 | 운영 에셋의 이펙트 종류 선택은 여전히 사용자 결정. 검증 값은 전부 원복 |
| Ue 방향 | 운영 결속의 인접 배치/이동 때 방어력 5→6→5. 도구의 명시 상태 0→5→0과 Up/Down 두 줄 모두 재생 완료, Down의 파란 표시 관찰 | Up 색상의 순간 화면은 확정하지 못해 **Ue② 전체 수락으로 표시하지 않음** |
| Uf 위치 이동 | 기본 0.5초 `Scenario_PositionSwapRestore` 4항목 완료. 임시 10초로 두 칸 경로 끝의 승객 이동 중 두 유닛의 9, 승객 몸 추적, 행동 완료 후 둘 모두 회수, 이후 두 참여자 복원 확인 | **Uf⑥ 지뢰·가시 실제 사건 순서 미확인**(기존 도구에 해당 사건 블록 없음). **Uf⑧ 행동만 비우기 미실행**: UnitAction 큐를 남긴 채 행동만 비우면 TP-E2의 DB 검사가 거절하므로 확인 절차를 먼저 정리해야 함 |
| Ug 순간 15줄 | 임시 시나리오로 포효·재생·피의 부름·위압·무장 해제·발목/방어구 부수기·선봉장·깔끔한 뒷정리·사기 증진·가속·고정 사격 두 줄·결속 두 방향을 **15/15 끝까지 재생**. 가속/깔끔한 뒷정리는 Activation→State 근거도 함께 작성 | 연결/재생 완료 증거이며 모든 짧은 파티클의 색·세부 타이밍을 프레임별로 수락한 것은 아님 |
| 약점 노출 | `Scenario_WeaknessExposure_Refresh` 4항목: 출처 갱신·다른 출처 제거 뒤에도 하나의 12 표시 유지, 중복 표시 없이 재생 완료 | — |
| CT-a / FE-a | 운영 적에게 도발당함 표시가 생겨도 표시 변환 예외 없음. 운영 희생·굳건한 의지·보호막의 이동/되돌리기 추적 확인 | CT-a의 설명 수명 문자열·거리 이탈/되돌리기·보유자 사망·AI 우선 대상 전체 절차는 미완료. 관련 기존 Core 시험 결과는 위 표 |

임시로 바꾼 `Unit_Ballista`·`TraitVisual_FinishingShot`·`TraitVisual_PositionSwap`·`Scenario_ManualStep`은 **검증 전 백업과 SHA-256이 같은 바이트로 모두 원복**했다. 재현용 임시 시나리오 사본과 매니페스트는 로컬 Logs 폴더에만 있다. Game의 Play Focused는 원래 Play Maximized로 복원했고, Play를 종료해 Combat 씬으로 돌아왔다. Play From Boot 설정·씬 파일은 저장/변경하지 않았다.

#### 별도 관찰·후속

- 연출 도구의 **환경 종료** 때 `InvalidOperationException: [UISystem] 닫으려는 UI가 등록되어 있지 않습니다. UIId: Setting`이 반복된다(`OptionInputRouter.Shutdown:52`). 일반 운영 Play 종료와 구분한다. 필수 UI 수명 검사를 우회하거나 Global/Architecture 영역을 임의 수정하지 않았다.
- 운영 공격 때 `Boom`의 Natural + Looping/Ring Buffer 경고(EL6)가 나왔다. 설계된 경고 동작이며 해당 큐/프리팹 종료 설정 검토가 남는다. `CombatExperienceEvent(ExperienceEarned)`의 표시 계약 없음 경고, 일부 외형 BoxCollider 경고, 필드 겹침 2단계 경고도 별도 관찰했다.
- 미리보기의 강한 조명은 포효 없는 새 환경에서도 같았다. 포효 잔류로 보았던 중간 판단은 철회했다. 포효 프리팹의 종료 결함으로 확정하지 않는다.
- 다음 리뷰는 이 절의 미확인 항목과 세 시험 실패를 대상으로 한다. **컴파일 성공·주요 재생 확인을 Ub~Ug/CT-a 전체 완료나 전체 시험 통과로 확대하지 않는다.**
