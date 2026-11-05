# 공격 Drag → 이동 계획

- 작성자: 호진(이호진)
- 상태: P0~P2 완료(2026-10-17), 사용자 커밋 `32654528`. 자동 회귀는 통과했고 사용자 Play는 일부만 확인했다(§2).
- 설계: [공격 모드에서 유닛 Drag로 이동 전환 설계](../specs/2026-10-16-attack-drag-to-move-design-Hojin.md)
- 정리: 2026-10-27 Task 단계·검증 명령·P0~P2 실행 기록을 결과 요약으로 줄였다. 끝난 Task 단계·시험 수치·증거 경로는 git `10914570`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| P0 기준선 | 관련 18개 fixture 기준선, 공유 제품 파일 병행 편집 확인 | 완료 2026-10-17 | 기존 실패 6건(§3.1) | — |
| P1 구현·회귀 | 몸짓·경로 진입 세 메서드·Interaction hook·Router·Factory, Attack Exit 보완(사용자 승인), 새 시험 두 파일 | 완료 2026-10-17 | 새 단위 52·통합 6 통과, 기존 결과 변화 없음 | `32654528` |
| P2 Unity 확인·인계 | 같은 조건 재실행, 사용자 Play, 인계 문서 | 완료 2026-10-17 | 자동 통과, 사용자 Play 일부(§2) | `32654528` |

기준 HEAD는 `ea38bbda`(P0·P1), 사용자 커밋 `32654528`(P2)이다. `32654528`에는 StageLegend·Boot·StageSelect 작업도 섞여 있다.

## 2 남은 일

- [ ] 사용자 Play 미확인·부분 확인 항목(2026-10-17 사용자 보고 기준):

| 항목 | 상태 |
|---|---|
| ① 짧은 클릭 공격·같은 칸 움직임/스냅 | 미확인 |
| ② 유닛 Drag → 칸 이탈 때 전환 → 놓기 이동 | 부분 확인(이동·칸 이탈 뒤 범위 유지 보고) |
| ③ 취소 → E/공격 버튼 → 옛 release 무시 → 새 입력 | 부분 확인(Drag 중 공격키 전환·Space 턴 종료 보고) |
| ④ 기존 UI 버튼·Submit·UI 시작 Drag | 미확인 |
| ⑤ 포커스·씬 이탈 중 정리 | 미확인 |

- [ ] 실제 OS 커서 Warp·창 포커스·씬 이탈은 자동 Boot 시험 하나로 대신하지 않았다. 자연 입력 확인이 필요하면 위 ①·⑤로 본다.
- [ ] 기존 실패 6건(§3.1)의 지금 결과는 다시 돌려 보지 않았다. 행동 패널 쪽 준비 오류는 2026-10-24 용근님 1라운드 V-c가 시험 도우미를 고쳤다. 손패 2건은 용근님의 `HandPresenter` 삭제 결정에 묶여 있다.

이 기능 때문에 Core·Framework·UI·Camera에 요청할 것은 없다.

## 3 작업별 결과

<a id="42-구현-전부터-있는-실패-6건"></a>

### 3.1 P0 기준선

- 한 것: 관련 18개 fixture를 돌려 기준선을 남겼다. 공유 제품 파일 4개(Router·Interaction·PathInput·Factory)의 병행 편집이 없음을 확인했다.
- 관련 fixture(다시 확인할 때 같은 선택, 모두 `ProjectL.Tests.Framework.`): `CombatInteractionTests`·`CombatInputRouterTests`·`CombatInputReceiverTests`·`CombatInputPolicyTests`·`CombatCommandGateTests`·`CombatInputAssemblyTests`·`CombatPathInputCleanupTests`·`CombatMovementFeedbackTests`·`CombatMovementPlanningInputTests`·`CombatMovementRangeFeedbackTests`·`CombatMovementViewTests`·`CombatCursorSnapTests`·`MovementDraftTests`·`MovementPublicationTests`·`StageInputTests`·`UnityStageInputTests`·`UIInputBindingTests`·`CombatDisplayReuseTests`, P1부터 `HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`를 더해 20개. 2026-10-27 기준 20개 모두 있다.
- 구현 전부터 있던 실패 6건은 모두 `CombatDisplayReuseTests`의 **준비 단계** 실패다. 이 기능 실패가 아니며, 시험을 빼거나 고치지 않았다.
  - 행동 패널 4건(`ActionPanel_ButtonsReachEntriesThroughEventSystem_NotOnClick`, `ActionPanelReturn_NewVisibleLifetime_…`, `ActionPanelReturn_QuoteAndVisibility…`, `HudFeedback_WarmRepeatedValues_DoNotAllocateStrings`): 시험이 리플렉션으로 넣는 `returnText` 필드가 `CombatActionPanel`에서 주석 처리돼 NullReferenceException.
  - 손패 2건(`HandCards_OrderBySlot_…`, `HandCards_PickByScreenPoint_…`): 일반 GameObject의 `UnitCardView` Transform을 RectTransform으로 캐스팅해 InvalidCastException.
- 그래서 이 6건의 행동 단언은 확인하지 못했다.

### 3.2 P1 구현·회귀

- 한 것: 새 `CombatAttackDragGesture`(+ `.meta`), `CombatPathInput`의 `TryGetAttackDragOrigin`·`IsAttackDragOriginCurrent`·`TryBeginTransferredDrag`, `CombatInteraction.TryHandleAttackDragPointer`와 수명 입구 무효화, Router hook(UI 가림 판정 뒤·조기 반환 전), Factory `CreateSceneInteraction`에서 생성·주입. 새 시험 `HojinCombatAttackDragTests`·`HojinCombatAttackDragIntegrationTests`(+ `.meta`).
- 계획과 다른 점: 기존 `AttackTargetingInteraction.Exit` 재진입 결함을 실제로 재현하고 사용자 승인("한정 보완 승인", 2026-10-17)을 받아 그 파일 하나를 더 고쳤다([설계 §6](../specs/2026-10-16-attack-drag-to-move-design-Hojin.md#6-공격-exit-재진입-보완-p1-한정-승인)).
- 검토에서 찾아 고친 것: 정리 콜백에서 새 Path 요청이 생기면 옛 전환이 그 요청 번호를 덮던 문제. 지금은 새 요청·초안을 보존하고 옛 release도 넘기지 않는다.
- 통합 시험은 실제 Core 견적·context와 실제 Boot → Combat(운영 DB·Live)에서 공개 Framework Submit으로 E·클릭·Drag를 보내 Move 1회·공격 중복 0·수락·표시 확인까지 본다. 준비용 배치·전투 진입만 기존 공개 View 입력 Buffer를 쓴다.
- 시험 준비만 보완한 것: Core 수락 사유 빈 문자열, 메인 체력 지속 원본, 사망 개체 필드 점유 금지에 맞춘 시험 준비. 제품 코드는 아니다.
- 확인: 새 시험 모두 통과, 기존 결과는 이름·결과·실패 메시지까지 같음(기존 실패 6건 유지).

<a id="p2-results"></a>

### 3.3 P2 Unity 확인·인계

- 한 것: 사용자 커밋 `32654528` 위에서 같은 fixture를 다시 돌려 P0·P1과 결과가 같음을 확인했다. [인계](../handoffs/2026-10-17-attack-drag-to-move-handoff-Hojin.md)를 썼다(미전달).
- 사용자 Play(2026-10-17): 이동·칸 이탈 뒤 범위 상태 유지, 이동 중 되돌아가기는 다시 이동, Drag 중 공격키 = 공격 전환, Drag 중 Space = 턴 종료를 보고했다. 사용자는 "현재 저렇게 되어있는게 제일 좋다고 봅니다. 그래서 문제는 없습니다."라고 했다. 제품 변경 요청으로 보지 않고 지금 동작을 유지했다.
- 이 보고를 Play 다섯 항목 전체 통과로 넓히지 않는다(§2 표).

### 3.4 이후 다른 작업자 변경

용근님이 2026-10-19~26에 시험 서명·폴더·넘겨받기 위치를 바꿨다. 목록은 [설계 §7](../specs/2026-10-16-attack-drag-to-move-design-Hojin.md#7-다른-작업자-영향)에 있다. 시험 확인 내용은 그대로다.

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [공격 Drag→이동 전환 인계](../handoffs/2026-10-17-attack-drag-to-move-handoff-Hojin.md) | 다음 전투 작업자(용근님) | 미전달 |
