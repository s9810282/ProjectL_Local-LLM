# 공격 모드에서 유닛 Drag로 이동 전환 설계

- 작성자: 호진(이호진)
- 상태: 구현 완료(사용자 커밋 `32654528`, 2026-10-17). 자동 회귀는 통과했고 사용자 Play는 일부만 확인했다([계획 §2](../plans/2026-10-17-attack-drag-to-move-plan-Hojin.md#2-남은-일)). 2026-10-24에 용근님이 넘겨받기 본문을 `CombatAttackDragHandover`로 옮겼다(§7).
- 범위: 공격 모드에서 현재 행동 유닛을 누른 채 그 칸을 벗어나면 공격 표시를 정리하고 기존 이동 Drag로 넘긴다. 경로 계산·견적·제출, 입력 Layer, Core 규칙은 다루지 않는다.
- 계획: [공격 Drag → 이동 계획](../plans/2026-10-17-attack-drag-to-move-plan-Hojin.md)
- 공유 메모: [공격 Drag→이동 전환 인계](../handoffs/2026-10-17-attack-drag-to-move-handoff-Hojin.md)
- 정리: 2026-10-27 1부(전환 설계)·2부(P1 Exit 보완 결정)를 한 문서로 다시 썼다. 옛 판은 git `10914570`.

## 1 목적과 범위

사용자 확정 요구(2026-10-17)다.

1. 유닛 칸을 **누르는 순간에는 전환하지 않는다.** 실제 Drag가 확인된 뒤 전환한다.
2. 짧은 클릭은 기존 공격 입력을 유지한다.
3. 전환할 때 공격 표시를 먼저 정리한 뒤 기존 이동 모드로 넘긴다.
4. 기존 UI의 클릭·Drag·Submit·버튼 기능은 유지한다. UI에서 시작한 입력을 이 전환으로 해석하지 않는다는 뜻이다. UI 입력을 없애거나 끄지 않는다.
5. Framework·Core·전투 계약과 전투 작업자의 진행 작업을 유지한다.

이 요청은 [View 계획 §7.75](../plans/2026-10-01-combat-view-plan.md#775-이동-drag-입력-2026-10-13-d1d2-사용자-unity-확인-완료)의 "공격 모드에서는 유닛 Drag를 시작하지 않는다"를 바꾸는 후속 결정이다. 경로 계산·견적·승인·제출과 입력 수명 규칙은 바꾸지 않는다.

범위 밖: Core·Buffer·Job/Plan/Flow·Framework·FieldCamera·Architecture·공격 조준 계산·경로 계산기(`CombatMovementInteraction`·`MovementDraft`·`MovementPointerSampler`)·입력 액션·Layer·씬·프리팹·UI 제품 코드.

## 2 지금 구조

`Combat/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`다.

| 객체 | 책임 |
|---|---|
| 몸짓 `Combat/Input/Interaction/CombatAttackDragGesture` | 누름 자격·시작 값(`Origin`)·칸 이탈 판정·release 소유권·세대만 갖는다. 공격 상태·이동 초안·Layer·Core 규칙은 복사하지 않는다 |
| 넘겨받기 `Combat/Input/Interaction/CombatAttackDragHandover` | "언제 넘기나"와 전환 순서(§5). 받는 쪽은 좁은 계약 `ICombatAttackDragTarget`으로만 본다. 2026-10-24 용근님이 `CombatInteraction`에서 옮겼다 |
| 경로 입력 `Combat/Input/MovePath/CombatPathInput` | 진입 자격과 실제 경로 입력·수명. `ICombatAttackDragTarget`을 명시 구현한다 |
| 입력 묶음 `Combat/Input/Interaction/CombatInteraction` | `TryHandleAttackDragPointer`를 넘겨받기에 위임하고, 수명 입구에서 몸짓을 무효화한다 |
| Router `Combat/Input/CombatInputRouter` | 기존 대상 판정과 UI 가림 결과를 넘긴다. 제스처 상태·이동 허용 정책을 갖지 않는다 |
| 공격 `Combat/Input/Interaction/AttackTargetingInteraction` | 공격 모드·표시·자기 미리보기 정리. Exit 재진입 보완(§6)만 더했다 |
| Factory `Factory/Runtime/InGame/View/CombatSceneInputFactory` | 몸짓 생성·주입만 한다. 경로·몸짓 없는 공격 단독 조립도 그대로다 |

유지한 계약:

| 사실 | 원본 |
|---|---|
| 전투 Layer에서 공격이 켜져 있으면 Attack, 아니면 Path로 보낸다. RecordInputOnly는 늘 Attack이다 | `CombatInputRouter` |
| 일반 이동 Drag는 활성 아군 행위자의 앵커 칸에서만 시작한다 | `CombatPathInput`, [이동 Drag 설계](2026-10-13-combat-move-drag-input-design.md) |
| 경로는 놓을 때 확정하고, 정확한 경로의 Core 견적·context 승인 뒤 제출한다 | 같은 설계, `CombatMovementInteraction` |
| 실제 공격은 SelectReleased에서 제출한다. 범위·조준·AP의 원본은 Core 응답이다 | `AttackTargetingInteraction`, [View 설계 §7·§21](2026-09-30-combat-view-design.md) |
| 이동 편집은 기존 `CanEditPath`의 대기·연출·미처리 명령·종료 조건을 따른다 | `CombatInputPolicy`, `CombatCommandGate` |
| 스냅은 입력 처리 뒤 커서를 같은 칸 중심으로 옮기고 다음 위치 이벤트가 다시 들어온다 | `CombatCursorSnap`, [커서 스냅 설계](2026-10-13-combat-cursor-grid-snap-design.md) |
| 입력 수집·설정·Layer·Submit은 Framework가 갖는다 | [FW 개발 지침 FW-004~007](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) |

## 3 결정 기록

| 결정 | 날짜·근거 |
|---|---|
| Drag 판정 = **누른 유닛 칸을 벗어나 다른 유효 필드 칸에 들어감**. 픽셀 임계값·감도 설정·커서 장치 계약 변경은 넣지 않는다 | 2026-10-17 사용자 확정. 격자 이동 첫 걸음과 같고, 같은 칸 중심 스냅 이벤트가 전환을 일으키지 않는다 |
| 전환 후보는 관찰만 한다. 공격 모드·범위·호버·미리보기 요청을 지우지 않는다 | 2026-10-17 설계 |
| release 억제는 **새 몸짓이 가진 유닛 누름에만** 한다. UI 시작 누름·소유권 없는 기존 공격 release에 전역 "새 press 필요" 조건을 더하지 않는다 | 2026-10-17 설계 |
| P1 `AttackTargetingInteraction.Exit` 재진입 결함은 그 파일 한정으로 고친다(§6) | 2026-10-17 사용자 "한정 보완 승인" |
| 이동 뒤 범위 유지, Drag 중 공격키 = 공격 전환, Drag 중 Space = 턴 종료인 지금 동작을 그대로 둔다 | 2026-10-17 사용자 "현재 저렇게 되어있는게 제일 좋다고 봅니다. 그래서 문제는 없습니다." |

## 4 입력 동작

### 4.1 전환 후보

- Live의 Combat Layer·공격 모드이고, 기존 입력 수명과 `CanEditPath`가 허용할 때만 후보를 만든다.
- 누른 칸이 현재 행동 중인 살아 있는 아군 필드 유닛의 앵커여야 하고, 기존 이동 가능 행위자 조건을 채워야 한다.
- 다른 유닛·적·빈 칸 누름은 기존 공격 흐름이다. RecordInputOnly의 기록용 행위자 선택도 그대로다.
- 이동을 시작할 수 없는 누름은 공격 모드를 끝내지 않는다.

### 4.2 클릭·Drag·취소

| 입력 | 결과 |
|---|---|
| 유효한 유닛 누름 → 같은 칸에서 놓기 | 같은 release를 기존 공격 흐름에 한 번 넘긴다. 공격 가능 여부는 Core 응답·Gate가 정한다 |
| 유효한 유닛 누름 → 다른 필드 칸으로 Drag | release 소유권을 이동으로 먼저 확정 → 공격 표시·자기 요청 정리 → 최신 사실 재조회 → 기존 이동 Drag 시작·현재 포인터까지 경로 샘플링 |
| 전환 뒤 놓기 | 기존 경로 확정·견적/context 승인·제출. 공격 명령은 내지 않는다 |
| UI에서 누름 → UI나 필드에서 놓기 | UI 기능과 기존 공격의 UI 가림·놓기 처리를 그대로 둔다. 전환 후보를 만들지 않는다 |
| 전환 뒤 시작 실패·UI/필드 밖 놓기·잘못된 경로 | 기존 이동 취소. 같은 release를 공격으로 되돌리지 않는다 |
| 취소·포커스 상실·선택 취소·결과 재생·행위자 변경·씬 이탈·오류 정지 | 후보와 진행 중 몸짓을 정리한다. 같은 누름의 늦은 release로 공격하지 않는다 |
| 후보를 누른 채 E 또는 공격 버튼 | 후보를 취소하고 그 release를 소비한다. E 재진입·공격 버튼 토글은 그대로다 |
| 취소 뒤 E/공격 버튼으로 다시 공격 진입 | 새 누름부터 다시 받는다 |

전환을 정한 뒤의 release 소유권은 경로 시작 실패나 재진입으로 바뀌지 않는다. 이미 제출한 명령을 취소하거나 처리 완료로 보지 않는다.

## 5 몸짓 계약과 전환 순서

### 5.1 몸짓 `CombatAttackDragGesture`

모두 `internal`이고 서비스로 등록하지 않는다.

- `Origin`(readonly struct): `BattleId`·`ActorId`·`Anchor`·`ResultSequence`·`PressedScreen`. 사실 묶음 전체는 보관하지 않는다.
- `ReleaseRoute`: `PassThrough`(일반 클릭·소유권 없는 release), `ForwardToPath`(전환한 누름, 한 번), `Consume`(취소한 소유 누름, 한 번).
- 상태: Idle → Candidate → MoveOwned, 소유 누름 취소는 SuppressUntilRelease. Idle 취소는 억제를 만들지 않는다.
- 멤버: `BeginPress(Origin?)`·`TryGetCandidate`·`TryPromote(x, y, overUi)`·`Owns(generation)`·`Release()`·`Invalidate()`·`Reset()`.
- 세대: 새 누름·취소·모드 재요청·정지에서 이전 세대를 무효화한다. 외부 콜백 전후로 세대를 비교한다. 같은 행위자로 공격 모드에 다시 들어와도 이전 후보를 잇지 않는다.

### 5.2 받는 쪽 계약 `ICombatAttackDragTarget`(경로 입력)

| 멤버 | 계약 |
|---|---|
| `TryGetAttackDragOrigin(x, y, out origin)` | Path 자신의 최신 facts(미처리 명령 포함)로 일반 진입과 같은 자격을 본다. 읽기만 한다 |
| `IsAttackDragOriginCurrent(origin, expectedTarget)` | 원점·전투·행위자·결과 정체성을 다시 확인한다. `expectedTarget`은 공격 후보 때 Attack, 공격 정리 뒤 Path다. 가짜 `IsAttackActive`를 만들지 않는다 |
| `TryBeginTransferredDrag(origin)` | `PressedScreen`으로 기존 앵커 진입만 시작한다. 자기 콜백 뒤 원점·facts·planning version을 다시 보고, 이미 새 Path가 시작됐으면 잇지 않는다 |

### 5.3 전환 순서

1. Router의 기존 UI 가림 판정을 먼저 적용한다. 막힌 누름에서는 후보를 만들지 않는다. 이미 몸짓이 가진 누름의 release·취소 정리에 필요한 사실만 더 넘긴다.
2. 칸 이탈을 만족하면 같은 release가 공격으로 돌아가지 않게 소유권부터 고정한다.
3. 공격 소유자가 기존 정리 경로로 모드·범위·영향·호버·예상 체력과 자기 미리보기 요청을 비운다. 다른 기능의 요청 번호는 지우지 않는다.
4. 표시 콜백 뒤 최신 facts·입력 연결·전투·행위자/시작 칸/결과 정체성·몸짓 세대를 다시 확인한다.
5. 누른 원래 좌표로 경로 진입을 시작하고, 세대를 다시 확인한 뒤 현재 Drag 좌표를 기존 샘플러에 넘긴다.
6. 이후 범위·미리보기·놓기·확정은 기존 경로 입력이 맡는다.

정리와 경로 시작 사이 콜백에서 취소·정지·새 결과가 생기면 옛 전환을 잇지 않는다. 예외는 기존 `CombatInputReceiver`/`RuntimeFaultBoundary` 경계로 보낸다. 포인터 처리 뒤 커서 스냅 순서는 그대로다. 수명 입구(`SelectCanceled`·`CancelDragOrAttack`·`AttackModeRequested`·`RequestReturn`·`BeforeResult`·`CancelPresentation`·`CancelInputs`·`Stop`)는 표시 콜백보다 먼저 몸짓을 무효화한다.

<a id="2부-p1-공격-정리-재진입-범위-확인"></a>

## 6 공격 Exit 재진입 보완 (P1 한정 승인)

- 결정: 2026-10-17 사용자 "한정 보완 승인".
- 재현(제품 수정 전, native EditMode): 기존 `AttackTargetingInteraction.Exit`는 표시 콜백 뒤에도 요청 정리를 계속했다. `ClearRangeCells` 콜백에서 같은 행위자의 공격 모드를 다시 열면 새 AttackRange 요청이 생기는데, 바깥 Exit가 그 요청을 지웠다. 결과는 "공격 켜짐·미리보기 없음"이다. 몸짓을 조립하기 전에도 재현돼 기존 결함이다.
- 보완: Exit가 끝내려는 요청과 상태를 외부 표시 콜백 **전에** 놓고, 콜백 중 새 공격 수명이 생기면 옛 정리가 그 요청·상태·표시를 다시 비우지 않는다.
- 유지: 공격 조준·비용·Core API·Gate·입력 액션·UI 기능, 기존 시험 단언.
- 몸짓 쪽 세대 검사도 그대로 둔다. Attack 안의 보존과 옛 전환 중지는 다른 책임이다.

## 7 다른 작업자 영향

이 기능 뒤에 다른 작업자가 바꾼 것이다. 확인 내용(시험 단언)은 바뀌지 않았다.

| 날짜 | 누가 | 바꾼 것 | 기록 |
|---|---|---|---|
| 2026-10-19 | 용근님 | `HojinCombatAttackDragTests` 가짜 표시 `SetHoverCell` 서명(셋째 인자 `CombatHoverKind`) | [궤적·호버 색 공유](../handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md) |
| 2026-10-21 | 용근님 | 폴더 정리: `Combat/` → `Combat/Input/Interaction/`(몸짓), `Input/MovePath/`(경로 입력). 네임스페이스 = 폴더 | [폴더 정리 공유](../handoffs/2026-10-21-combat-folder-restructure-share.md) |
| 2026-10-24 | 용근님 | 2라운드 V2-c: `CombatInteraction.TryHandleAttackDragPointer` 본문을 `CombatAttackDragHandover`로 옮김(위임 한 줄은 남음), 받는 쪽 계약 `ICombatAttackDragTarget`. 몸짓과 경로 입력 세 메서드는 그대로. V2-e: 통합 시험 `:128` 리플렉션 경로 | [전투 View 구조 공유 §11](../handoffs/2026-10-22-combat-view-structure-hojin-share.md) |
| 2026-10-25 | 용근님 | 3라운드 V3-c: 통합 시험 `:130` 리플렉션 경로를 `_connection`으로 | 같은 공유 메모 |
| 2026-10-25~26 | 용근님 | 4·5라운드: 시험 Rig의 공격 진입 Ready 준비(Q-T8 e), Facts 이름·부분 생성 `CombatInputTestFactory`(Q-T9) | [튜토리얼 T7 인계](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md) |
| 2026-10-28 | 용근님 | 후보 조건에 Core 가용성 `CanMove`를 더함(`CombatPathInput.TryGetAttackDragOrigin`, 응답이 있고 이동 불가면 후보 없음). 천명님 이동 후 공격 허용(`8ece0bbc`) 뒤 이동한 유닛도 공격 모드에 들어가게 되어, 자기 칸을 끌면 공격 모드가 끝나고 이동은 거절되던 것(§4.1 마지막 줄 위반)을 맞춤. 컴파일 미확인 | 이 표 |

2026-10-27 용근님 [클릭 이동 설계](2026-10-27-combat-click-move-design.md)는 이 설계를 "바꾸지 않는 규칙"으로 둔다.
