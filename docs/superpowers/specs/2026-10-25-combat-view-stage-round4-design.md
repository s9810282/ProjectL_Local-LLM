# 전투 View 단계 4라운드 설계 — 새 Core 대응 우선 (2026-10-25)

- **상태: 2026-10-26 V4-n 기능 인수 완료(사용자 실행 보고).** V5-0에서 `11c07868` 이후 결과를 확인하여 사용자 **"컴파일·기존 시험·M01~M14 모두 정상"** 답변을 받았다. a/d/e/f/g/h 코드·W13~W16 정적 검토와 m 문서 정리를 포함해 Core 대응 완료로 기록한다. 직접 Unity를 실행하거나 개별 로그를 확인한 것은 아니다. 최초 요청은 새 Core 대응과 남은 리팩토링을 함께 설계하는 것이었다. 이후 사용자 결정 **"4라운드에서는 구조 개선보다는 Core 변경 대응을 우선으로 하자"**에 따라 이번 라운드의 완료 목표를 Core 대응으로 좁혔다.
- Task: [round4-plan](../plans/2026-10-25-combat-view-stage-round4-plan.md). 2026-10-26 사용자 **"수정한대로 구현 차례대로 진행해"**로 수정안 A와 명시한 외부 접점/기존 시험 대응 범위의 구현을 승인했다. 새 시험 금지·Phase별 사용자 Unity 확인·커밋/push 금지는 유지한다.
- 조사 기준: 새 Core/AP UI 병합 `6425ec18` 및 `130b3308`. 두 커밋 사이 Combat 코드는 같다. V4-0의 구현 기준은 `f616d5d9`이며 추가 병합은 계획 §6에 기록한다.
- 4라운드 전 평가는 단일 정적 평가 **8.3/10**(SSOT 9.0, SOLID 8.25, FW 9.75, 있는 기능 활용 7.75, 독립성 7.5, 불필요·우회 7.5)이다. 블라인드 A/B 결과나 Unity 성공 증거가 아니다. 점수 상승을 이번 라운드의 완료 조건으로 삼지 않는다.
- 원칙: [설계 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), Framework 개발 지침·프로그래머 가이드. Core 규칙·Buffer 계약은 소비하며 복제/수정하지 않는다.
- 약어: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `G/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 이번 라운드의 목표와 제외

**완료 목표는 새 진영 턴에서 선택·이동·공격·공용 AP·턴 종료·밤 진행·설명이 올바르게 동작하는 것이다.** 기존 구조 안에서 대응하며, Core 대응에 직접 필요한 요청 소유자·표시 변환만 추가한다.

이미 연결된 공용 AP(`CombatTurnCostPresenter`), 이동/공격 비용 예상, 선택 없는 `EndPlayerTurn`, 공격 전투/턴 번호 검사, 이동 기대 문맥을 유지한다. AP 10·이동 비용·공격 비용·이동 한도·행동 가능 여부를 View에서 다시 계산하지 않는다.

| 이번에 하는 것 | 현재 구조에서의 대응 |
|---|---|
| 선택/Drag 충돌·선택 강조 | 기존 `CombatUnitInfo` 선택 원본과 `ICombatUnitInspection`을 유지하며 입력 순서·즉시 강조 갱신 수정 |
| 행동 가능 여부 | `ActionAvailability` 요청 수명·슬롯·버튼/키 연결 |
| 새 실패 이유 | 실제 시도/거절 결과의 안내 |
| 새 규칙 상태 설명 | 기존 정보창·밤 텍스트·특성 설명에서 공개 상태 소비 |
| 이동 실습 | 새 행동 제한에서도 다음 턴으로 회복할 수 있는 정책 |
| 폐지 기능의 사용자 접점 | 체크포인트 복귀의 숨김/무동작·필수 참조 문제 해소, 현재 안내 수정 |
| 기능 검증/공유 | 새 Core 인수 시나리오·타인 영역 변경 기록 |

**다음 구조 개선 라운드로 이월:** 선택 원본을 정보창에서 분리하기, `CombatInputFactsReader`, 광범위한 필수 생성자/시험 조립 정리, 표면 높이 기준 통일, `CombatResultConsumer`, 레이어 이름/배경음/정리 목록의 공통화, 대규모 미사용 API 삭제, 블라인드 구조 점수 측정.

이월한 문제는 해결된 것으로 세지 않는다. 기능 대응 때문에 그 파일을 수정하더라도 관련 없는 분리/이름 변경/형식 정리를 함께 하지 않는다. 기능 수정을 위해 더 큰 구조 변경이 반드시 필요하면 이유·대안을 먼저 제시하고 Task를 수정한다.

## 1. 근거와 문제 분류

줄은 조사 기준 Combat 코드이며 구현 시 다시 찾는다.

| ID | 코드 근거 | 이번 처리 |
|---|---|---|
| C4-1 | `CombatUnitInfo.cs:144` 선택 토글, `CombatViewSystem.cs:1017` Router 전 선택, `CombatPathInput.cs:211·397` 선택 읽기 | 동일 유닛 재누름 Drag 충돌 대응 |
| C4-2 | `CombatSceneDisplay.cs:227`의 결과 Refresh에서만 선택 강조 갱신 | 클릭 시 즉시 같은 선택을 표시 |
| C4-3 | View의 `ActionAvailability` 소비자 없음, 공격 CanEnter는 일반 자격만 확인 | Core 가용성 응답 연결 |
| C4-4 | `CombatActionNotice.cs:28·45`에 새 거절 이유 없음 | 이유 안내 |
| C4-5 | `NightCheckerUI.cs:60`은 플레이어 완료 수만 표시 | 진영·8번째 적 응답 구분 |
| C4-6 | `UnitCardTraitContent.cs:16`은 사용 불가 상태를 설명에 반영하지 않음 | Snapshot의 가동 상태 소비 |
| C4-7 | Router Return은 무동작, `returnButton`은 숨김인데 필수 검사 | 사용자 접점/구성 문제만 정리 |
| C4-8 | MoveOnly에서 성공한 빈 이동 범위만 턴 종료 근거 | 수동 턴 종료로 회복 가능하게 제안 |
| R4-1~3 | 조정자 책임 집중·시험용 null 허용·바닥/표면 선택 분산 | 후속 구조 개선으로 이월 |

선택/Drag·강조는 코드 흐름에서 찾은 문제이며 Unity 재현은 아직이다. 재현 여부와 의도한 클릭 정책을 V4-0에서 구분한다.

## 2. 유지할 소유권과 허용할 변경

| 사실 | 원본 | 변경 제한 |
|---|---|---|
| 진영·턴·공용 AP·유닛 ActionUse | Core가 게시한 Buffer | View 규칙/가변 사본 금지 |
| 가능 여부·실패 이유·가격 | Core Preview | 예산/병종/ActionUse로 판정식 재작성 금지 |
| 선택 ID | 현재 `CombatUnitInfo._selected` | 이번에는 그대로. 입력/강조가 같은 값을 읽도록 맞춤 |
| 입력 허용 조건 | 기존 `CombatInputPolicy` | 버튼/키/Tracker에 같은 조건식 복제 금지 |
| 요청 번호 | CombatViewBuffer | 자기 번호만 비움 |
| 결과 세대·확인 번호·Gate | 기존 Playback/Buffer/Gate | 새 Consumer/진행 상태 만들지 않음 |

새 전역 서비스·ServiceKey·Framework 등록은 없다. 기존 Factory에서 Tracker 같은 필요한 기능 객체를 조립한다. 조정자의 ReadFacts/ConsumeResult/FinalizeResult는 위치와 순서를 유지한다.

`ActiveActorId`라는 기존 Facts 이름은 이번에는 광범위하게 바꾸지 않는다. 의미가 "View가 선택한 아군"임을 현재 주석/수정한 접점에 명시한다. 기본값/캐스트를 전부 없애는 작업도 이월한다. 다만 실제 운영에서 선택 창구가 누락됐으면 연결 시 구성 오류로 진단하고 Empty fallback으로 성공 처리하지 않는다.

## 3. 선택·이동 Drag·강조 (V4-a)

### 3.1 클릭 정책 제안(Q-V29 A)

필드의 같은 유닛 재누름은 선택을 유지하고, 빈 칸 누름은 해제, 다른 유닛 누름은 교체한다. **덱/손패의 재클릭 토글·핀 규약은 그대로다.** 기존 필드 토글 동작의 변경은 2026-10-26 수정안 구현 지시로 승인됐다. B는 채택하지 않은 release 토글+Drag 별도 설계 대안이다.

- 새 선택 상태 클래스나 선택 인터페이스를 만들지 않는다. 현재 `SelectCell`에서 누름 정책만 맞추고 `InspectUnit`이 바뀐 선택의 정보와 강조를 같은 호출에서 갱신한다.
- 정보창은 기존 완료 표시 캐릭터 목록을 사용한다. 클릭 때문에 전체 `Display.Refresh`나 Core 결과 확인을 실행하지 않는다.
- 공격 모드·현재 경로 지정 중·UI 위·확인창·재생/후처리·적 자동 턴에서는 선택을 변경하지 않는다. 경로 지정 중 빈 칸 누름이 선택을 지워 경로를 취소하지 않게 한다.
- 죽음/필드 이탈·단계 전환/해제의 기존 선택 정리를 유지한다. 같은 밤의 진영 턴 전환에는 생존 선택을 유지한다. 적 선택은 정보만 보이고 행동 ID는 Empty다.
- `InspectUnit` 같은 외부 표시 콜백 뒤에는 현재 연결을 다시 확인한 뒤 Router로 전달한다. 옛 연결의 입력을 새 연결에 보내지 않는다.

선택 원본의 UI 종속은 남는 구조 부채다. 이 Phase를 선택 SSOT 분리 완료라고 보고하지 않는다.

## 4. 행동 가능성·요청 수명 (V4-d)

새 `Input/Interaction/CombatActionAvailabilityTracker.cs`(+meta) 한 개는 **Core 대응에 필요한 기능 구현**이다. 연결 입력 묶음이 소유하고 Factory가 만든다. 입력/결과 처리기를 일반화하는 기회로 확대하지 않는다.

### 4.1 키와 수락 조건

- 상태: Unknown/Pending/Ready. 실패 응답도 Ready로 보관한다.
- 키: `(BattleInstanceId, ControllerTurnSequence, LatestResultSequence, 선택 아군 ID)`.
- 자기 요청 번호·현재 슬롯·응답 ActorId/전투/턴·현재 키가 모두 맞아야 수락한다. 응답에 없는 결과 순번은 요청 때 잡아 현재 값과 대조한다.
- 같은 키의 Ready는 슬롯을 잃어도 재요청하지 않는다. 같은 턴에 다른 유닛이 AP를 쓴 경우도 결과 순번 변화로 무효화한다.
- 선점/슬롯 상실로 응답을 받지 못한 Pending은 요청 번호를 폐기하고 Unknown으로 돌아간다. 전경이 끝나고 다시 조회 가능한 빈 슬롯이 생기면 **같은 키라도 재요청**한다. 폐기한 번호의 늦은 응답은 수락하지 않는다. 같은 키의 Ready 캐시와 대기 요청의 재시도를 구별한다.
- 선택/새 결과/진영 턴/전투 교체·죽음·필드 이탈·입력 닫힘·Stop에서 취소한다. 자기 슬롯만 비우고 늦은 답은 무시한다.
- 현재 `ReadFacts`와 연결 값에서 키를 조립해 Tracker에 넘긴다. Tracker가 Facts를 다시 읽거나 Facts가 Tracker 갱신을 부르는 순환은 만들지 않는다.

### 4.2 기존 한 슬롯 사용

1. 전경의 배치·공격·이동 상세/확정 Preview가 우선이다. Tracker가 덮어쓰지 않는다.
2. 전경이 없고 S=P인 플레이어 입력에서 빈 슬롯에 선택 가용성→이동 모드 배경 범위→유닛 호버 순으로 요청한다. 기존 `CombatPreviewSlotRule`의 번호/미응답 검사를 사용한다.
3. Tracker가 자기 응답을 소비한 뒤 현재 Facts를 새로 읽어 기존 Interaction Tick을 진행한다. 옛 PreviewAnswered 신호만으로 답을 적용하지 않는다.
4. 사용자의 첫 이동 Drag는 가용성 Pending 때문에 막지 않는다. 필요하면 Pending을 내려놓고 기존 MoveRange/MovePreview 경로로 넘긴다. 실제 실행은 기존 성공 Preview와 기대 문맥만 승인한다.
5. 현재 키에 소유 중인 Pending 또는 Ready가 있으면 중복 요청하지 않는다. Ready를 얻기 전에 선점당한 요청은 4.1의 재시도 규칙을 따른다. 전역 요청 큐·별도 Core 조회 서비스·일반 목적 우선순위 시스템은 만들지 않는다.

`CombatInputPolicy`에 ActionAvailability의 Combat 단계와 조회 허용을 추가한다. MoveOnly에서는 실행 없는 조회로 허용하고 설명 독점 Layer에서는 요청하지 않는다. 수동 시도가 아닌 조회로 튜토리얼 막힘 안내를 내지 않는다.

기존 BeforeResult/AfterAcknowledged/Stop 접점에 무효화·재조회만 연결한다. 결과 확인 순서 자체를 재작성하지 않는다.

## 5. 행동 입구·버튼·안내 (V4-e)

- 공격 버튼/키는 기존 공통 CanEnter 판단에서 현재 키의 Ready.CanAttack을 읽는다. Unknown/Pending이면 새 진입을 기다리고, 나중에 자동으로 모드를 켜는 의도 큐는 두지 않는다. 이미 켜진 모드 끄기는 허용한다.
- 이동은 유효 Ready.CanMove=false이면 실제 사용자 시도에 이유를 안내한다. 아직 Unknown이면 기존 범위/상세 Core Preview로 판단한다. 누락 응답을 실행 승인으로 쓰지 않는다.
- `EndPlayerTurn`은 선택·가용성 응답과 독립이다. AP=0 자동 종료는 추가하지 않는다.
- `_canAffordAttack` 등 기존 범위/상세 응답은 조준/가격/경로의 최종 승인 사실로 유지한다. View가 AP/병종에서 가능성을 재계산하지 않는다.
- 안내: ActionAlreadyUsed=이번 턴 행동 사용, ActionDeferred=현재 행동 보류, WrongController=행동 가능한 진영 아님, StaleTurn/StaleContext=상황이 바뀌어 재지정 필요. 기존 비용 부족 안내는 공용 AP 의미로 맞춘다.
- Poll/호버/조회 응답만으로 팝업을 내지 않는다. 실제 시도 또는 최종 거절에서 한 번 안내한다. 사전 거절 시 명령을 게시하지 않아 중복을 막는다.
- 옛 요청의 전투/턴 번호를 최신 값으로 치환하지 않는다. 이동의 기대 문맥·결과 번호 대조를 보존한다.

## 6. 상태 표시·특성 설명 (V4-f, 승인된 접점만)

Q-V30 A는 Global UI 중 아래 파일의 전투 접점만 수정하는 허락이다. Q-V31 A는 새 UI 없이 기존 텍스트를 쓰는 제안이다.

- `CombatUnitInfo`: 기존 이동/공격 비용 텍스트에 선택 유닛의 Available/Moved/Attacked/Deferred 의미와 현재 불가 이유를 표시한다. 보통 가격은 완료 Buffer Stats, 불가 이유는 같은 키의 Core 응답이다.
- `CombatSceneInput`→명시 표시 메서드로 Ready/Unknown을 전달한다. 상태가 달라질 때만 갱신하고 오래된 이유는 비운다. 필요하면 작은 `Display/Hud/CombatActionStateText.cs` 변환만 추가한다. 전체 선택/표시 계약 재설계는 하지 않는다.
- 전달 빈도와 텍스트 재구성을 구별한다. 표시 중인 개체·선택 ID·전투/턴/결과 키에 맞는 상태만 비용 텍스트에 적용한다. A→B 슬라이드 중 B의 Ready가 먼저 와도 A의 텍스트에 붙이지 않는다. 전달받은 값은 표시용으로 보관하고, 기존 `ApplyCharacter`·전환 완료·다시 표시할 때 현재 개체에 맞춰 기본 비용과 상태를 **함께 재구성**한다. 동일 Ready를 다시 전달하지 않아도 숫자 갱신이 상태 문구를 지우지 않아야 한다. 선택/키 무효화·해제 때 오래된 표시 상태를 비우며 가용성 원본은 Tracker로 유지한다.
- `G/Screens/Stage/NightCheckerUI.cs`: 기존 progressText로 `아군 턴 · 3/8`, `적 응답 · 8/8`처럼 표시한다. 진영 준비도 구분한다. 8/8만으로 밤 종료라고 쓰지 않는다. 비교 캐시에 StagePhase/ControllerPhase를 포함하고 기존 live Buffer 읽기 시점은 유지한다.
- `G/Screens/Global/UnitCardTraitContent.cs`: Snapshot의 IsAvailable/UnavailableReason/PartialUnavailableReason을 설명에 반영한다. 키 목록/DeferredCombatFeatures 호출을 View에 복제하지 않는다. 정적 TraitKey 경로에는 현재 가동 상태를 단정하지 않는다.

기존 CombatUnitInfo/Deck 자식·NightChecker·카드의 UIId/부모/Policy·등록/Bind/Open/Hide 경로를 유지한다. 새 UIBase·전용 진영 패널·부대 전체 행동 배지·필수 씬 참조는 추가하지 않는다. 전체 배지는 Q-V31 B로 별도 설계한다.

## 7. 이동 실습의 회복 경로 (V4-g)

Q-V32 A: MoveOnly 실습에서도 평상시 Gate 조건을 통과한 수동 EndPlayerTurn을 허용한다. 선택 없음·AP 부족·행동 사용 상태에서 다음 턴으로 회복할 수 있게 한다.

- 실습 통과는 계속 수락된 플레이어 이동이 표시 완료됐을 때다. 턴 종료로 통과시키거나 Move 결과를 만들지 않는다.
- Policy에서 성공한 빈 범위만 요구하던 제약을 제거한다. 기존 HasCurrentAcceptedEmptyMovementRange 추적/시험 전용 API를 한꺼번에 삭제하는 일은 이번 범위가 아니다. 남긴 사실은 후속 구조 정리 목록에 기록한다.
- `TutorialFactsReader`의 옛 Core 활성 유닛 주석은 새 선택 의미에 맞춘다. 특정 유닛 목표·Jobs/Store 상태 전이는 바꾸지 않는다.
- **밤 경계 회복 정책은 Q-V38 A로 승인됐다(2026-10-26).** 이동 없이 수동 종료를 반복하면 8번째 적 응답 뒤 Deployment가 된다. MoveOnly에 Move/EndPlayerTurn만 허용하면 EnterCombat이 막혀 정상 데이터에서도 실습이 멈춘다. 이 상황을 아래의 불능 데이터 문제로 분류하지 않는다.
- Q-V38 A는 미완료 MoveOnly 실습의 Deployment에서 **기존 자격 검사·Gate·결과 표시 조건을 지키는 EnterCombat만 제한적으로 허용**한다. 공격/일반 배치 제한은 그대로이며 Core가 전투 진입을 거절하면 우회하지 않는다. B는 채택하지 않은 담당자 실습 재시도 대안이며 Jobs/Store 변경은 승인 범위가 아니다. A 구현·Unity 확인 전에는 V4-g의 회복 경로를 완료로 보고하지 않는다.
- 구현에서 "미완료"는 같은 전투의 Running/Practice/PracticeGoal 대기로 구별한다. 기존 튜토리얼 접점이 공개 세션의 사실을 읽어 `IsTutorialPracticeGoalPending`으로 전달하고 Policy가 MoveOnly/Deployment와 함께 확인한다. 실습 통과 뒤 다음 설명 시작을 기다리는 MoveOnly 유지 구간에는 재진입 예외를 주지 않는다. 새 규칙 조회나 FactsReader 추출은 필요하지 않다.
- 언제까지나 이동할 수 없는 실습 구성은 담당자 데이터 문제로 남긴다. 이번 변경이 모든 기획 불능 상태를 해결했다고 주장하지 않는다.

## 8. 폐지 기능의 사용자 접점 (V4-h)

이번에는 체크포인트 복귀 때문에 새 Core 사용이 막히거나 잘못 안내되는 부분만 정리한다.

- 복귀 버튼의 숨김/무동작을 보장하고, 숨긴 버튼을 필수로 요구하는 검사를 걷는다. 폐지된 버튼 참조가 null이면 해당 슬롯의 Bind/입력 Attach/표시도 건너뛰어야 한다. 검사만 없애고 뒤의 Attach에서 실패하게 두지 않는다. 연결이 없어도 나머지 전투 UI가 정상 진입해야 한다.
- 실제 CombatInputController에서 체크포인트 복귀 키 소비를 해제한다. 배치 ReturnUnit/우클릭 손패 회수/경로 취소는 보존한다.
- 현재 사용자 안내와 코드 접점의 복귀·타임라인·유닛 AP 문구를 갱신한다. 과거 실행 기록은 지우지 않는다.
- **IStageInputReceiver·슬롯·Router 열거·BindActions 세 번째 인자·CanUseReturn 등 호환용 API의 일괄 삭제, 씬/프리팹 객체 대청소는 이월한다.** 남은 호환 입구는 요청을 만들지 않는다는 사실을 명시한다.
- 공용 KeyBindingAction.Return·설정 저장값·PlayerInputActions 예약 액션은 유지한다. 설정 화면 노출은 담당자 인계 잔여로 기록하고 임의 저장 호환 변경을 하지 않는다.

Q-V33은 위의 제한된 사용자 접점 범위를 뜻한다. 최초 초안의 전체 복귀 사슬 제거 제안은 이번 라운드에서 철회했다.

## 9. Phase와 변경 경계

기존 Phase ID는 재사용하지 않는다. 삭제/이월한 ID를 다른 작업으로 바꾸면 과거 대화와 혼동되므로 아래처럼 남긴다.

| Phase | 현재 상태/범위 |
|---|---|
| V4-0 | 기준/병합 검증 |
| V4-a | 기존 구조의 선택/Drag/강조 기능 수정 |
| V4-b | 이월: 선택 원본/계약 분리 |
| V4-c | 이월: FactsReader·명칭 전수 변경 |
| V4-d·e | Core 가용성 수명·행동/안내 |
| V4-f·g | 상태 UI·특성·이동 실습 |
| V4-h | 폐지 기능 사용자 접점만 |
| V4-i·j·k·l | 이월: 필수 조립·높이·결과 소비·작은 구조 정리 |
| V4-m·n | Core 대응 공유·기능 인수. 구조 A/B 측정은 이월 |

우리 V/F의 전투 접점은 Phase 승인 뒤 진행한다. Global UI는 Q-V30, 튜토리얼/공용 입력 접점은 Q-V32·33·38, 다른 사람 시험은 Q-T8 허락 범위만 다룬다. Core/Buffer/Framework/ServiceKeys·FieldCamera/Architecture·StageMap/Lobby·Missing Script 에셋은 수정하지 않는다. 필요한 계약이 실제로 없으면 인계하고 우회하지 않는다.

## 10. 결정 상태

| 번호 | 현재 제안/상태 | 대안/주의 |
|---|---|---|
| 범위 우선순위 | **사용자 결정: Core 대응 우선, 독립적 구조 개선 이월. 10-26 수정안 구현 승인** | Phase별 Unity 확인 유지 |
| Q-V29 | A 승인: 같은 필드 유닛 선택 유지, 빈 칸 해제 | B 미채택. 덱 토글은 유지 |
| Q-V30 | A 승인: NightCheckerUI·UnitCardTraitContent 전투 접점 수정 | 다른 Global UI 수정으로 확대하지 않음 |
| Q-V31 | A 승인: 기존 정보창/밤 텍스트 사용 | 전체 유닛 배지/별도 진영 UI는 별도 설계 |
| Q-V32 | A 승인: MoveOnly 수동 EndPlayerTurn 허용, 통과는 실제 이동만 | 실습 목표/Jobs/Store 변경 없음 |
| Q-V33 | A 승인: 복귀 사용자 접점만 정리, 호환 API/저장 예약 유지 | 전체 사슬 제거는 이월 |
| Q-V34~36 | **이번 결정 대상에서 제외, 후속 구조 개선으로 이월** | 필수 조립·FactsReader·Consumer |
| Q-V37 | 기존 W 끝 검토 방식 승계. 이번은 기능 인수로 종료 | 블라인드 구조 A/B 측정은 이월 |
| Q-V38 | A 승인: 미완료 MoveOnly의 Deployment에서 정상 조건의 EnterCombat만 허용 | B 미채택. V4-g에서 구현/확인 |
| Q-T8 | Task §5 a~e의 한정된 기존 시험 대응 승인 | 목록 밖 시험의 의미 변경·새 사례 추가로 확대하지 않음 |
| Q-T9 | A 기존 새 시험 금지 지시 승계 | 새 사례를 원하면 별도 변경 요청 |

새 구조를 먼저 완성해야 기능 Phase로 갈 수 있다는 종전 전제는 제거했다. 위 승인 범위를 벗어나는 UI/타인 시험 변경이 필요하면 구현 전에 범위를 다시 확인한다.

## 11. 원칙과 완료 기준

- **SSOT:** 규칙/예산/가용성은 Core, 선택은 이번에는 기존 UI 원본 하나, 재생 토큰은 기존 Playback을 사용한다. 선택 원본의 위치가 이상적이지 않다는 이유로 이번에 다시 분리하지 않는다.
- **SOLID/독립성:** 새 조회 수명은 Tracker에 모으되 기능 수정 범위를 넘는 분리·Factory 재구성은 하지 않는다. 표현 변경은 기존 UI/표시 접점에 한정하고 Policy/핵심 규칙을 복제하지 않는다.
- **FW/JMH:** 기존 Factory→등록→Group Export→Global Context 조회 경로, 입력 Submit/Configure, UI 등록·이력, 대여 자원 반환을 유지한다. 기반 자체의 리팩토링은 금지한다.
- 커밋/push는 사용자, MSBuild/새 시험 사례 추가는 하지 않는다. Phase마다 Unity 확인과 실제 증거/미확인을 기록한다.
- 완료는 선택·행동 가능성·AP·수동 턴 종료·밤8/적응답·특성 설명·이동 실습의 확인과 폐지 기능 오작동 없음이다. 허락 대기/미확인/이월은 별도로 표시한다.
- 초기 평가 8.3은 참고 기준이다. 구조 점수 상승·줄 수 감소·모든 부채 해결은 이번 라운드 완료 조건이 아니다.

## 12. 후속 구조 개선 목록(이번 미착수)

| 기존 제안 | 이월 이유/후속 판단 |
|---|---|
| CombatSelectionState·선택 표시 계약 | 기능 수정 뒤 별도 선택 소유권 설계 |
| CombatInputFactsReader·ActiveActor 명칭 전수 변경 | Core 대응의 선행조건 아님 |
| 운영 협력자 필수화·시험 도우미 개편 | 광범위한 생성 계약 변경을 분리 |
| CombatSurfacePlacement·표면 목표점/가시밭 | 기존 높이 문제이며 새 Core 규칙 대응과 독립 |
| CombatResultConsumer | 결과 확인/재진입 순서 변경 위험을 별도 관리 |
| 레이어 이름·배경음·정리 목록 공통화 | 기능 인수와 무관한 정리 |
| 복귀/타임라인 API 전체 삭제·빈 범위 전용 사실 제거 | 사용자 접점 대응 뒤 참조/직렬화 영향 조사 |
| 구조 A/B 측정 | 후속 구조 라운드에서 동일 Core 기준으로 실시 |

Core 기능을 맞추는 동안 새로 발견한 구조 문제도 이 표에 추가할 수 있지만 자동으로 구현하지 않는다.

2026-10-26 후속: 위 첫 세 항목(선택 원본·FactsReader/명칭·운영/시험 조립)은 사용자 요청으로 [5라운드 설계](2026-10-26-combat-view-stage-round5-design.md)·[Task](../plans/2026-10-26-combat-view-stage-round5-plan.md)에 구체화하고 독립 검토를 반영했다. 이후 수정한 계획대로 순차 진행 지시를 받아 V5-0을 완료했다. 이때 V4-n 전체 정상 보고로 기능 인수를 마쳤고 V5 코드 구현은 아직 하지 않았다. 나머지 이월 항목은 이번 요청에 포함하지 않는다.
