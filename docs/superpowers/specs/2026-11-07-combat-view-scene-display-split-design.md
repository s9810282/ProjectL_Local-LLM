# 전투 View 씬 표시 나누기 설계 (DS1~DS7: 입력 피드백 주인·선택·화면·필드 표시)

- 날짜: 2026-11-07
- 상태: **설계 2판, 결정 완료(11-07 사용자 "Unity 확인은 Phase 모두 종료 후로 하고, 나머지는 권장대로").** Q-DS1·Q-DS2·Q-DS3·Q-DS5 A, **Q-DS4 B**(Phase마다 멈추지 않고 DS-d까지 이어 하며 Unity 확인은 끝에 한 번). P1~P15는 그대로 간다. Task 계획 [scene-display-split-plan](../plans/2026-11-07-combat-view-scene-display-split-plan.md)(TP-1~TP-13).
  - 독립 검토(11-07, 새 에이전트 하나): 높음 0·중간 6·낮음 12, 모두 반영했다(§12).
  - 1판에서 바뀐 것:
    - 해제 뒤 규칙 권장을 공유 수명 객체로 바꿨다(Q-DS3).
    - 화면 표면을 셋으로 나눴다(Q-DS5).
    - Phase 순서를 바꿨다.
    - 정리 목록·결과 끝 그리기 대응표를 넣었다(§3.8).
    - 이름을 `SD` → `DS`로 바꿨다. 예전 CombatSceneDisplay 후속 `SD-A`~`SD-D`와 겹치지 않게 하려는 것이다.
- 요청: 사용자 "지금까지 한거 푸쉬하고, 씬 나누기 설계 진행하자"(11-07).
  - [평가 후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §6.6 묶음 셋 중 두 번째다(사용자 "1,2,3 대로 진행", 11-06).
  - 첫째 묶음 [선택 인자 나머지](2026-11-06-combat-view-optional-args-design.md)(C5-02)는 11-07에 push했다(`eafd42f5`).
- 대상 문제([검증된 문제 목록](2026-11-04-combat-view-evaluation-issues.md)): **C2-03 [중간, SOLID] 2·3단계.** `CombatSceneDisplay`가 God 퍼사드다. 표시 조정·배경음 정책·결과 화면·행동 버튼·피드백 위임·HUD 층 파괴를 함께 맡는다.
  - 1단계(평가 후속 4차-a, `fd8b0d2f`)에서는 배경음 정책만 `Playback/Support/CombatBgmPolicy`로 뺐다.
  - 평가가 적은 고칠 방향은 셋이다.
    - 배경음 정책을 작은 객체로 뺀다(끝남).
    - 입력 피드백 하위 계약은 실제 협력 객체가 직접 구현해 내놓는다.
    - 필드 그리기와 완료·행동 버튼 표면은 별도 표시 객체로 나누고, 표시에는 수명·순서만 남긴다.
  - 근거는 [중간 29개 코드 확인 기록](../plans/2026-11-04-combat-view-evaluation-fixes-assessment.md) C2-03 절이다.
  - 함께 풀리는 낮음: C2-17(`ICombatInteractionFeedback` ISP), C2-15의 기믹 범위 형식 검사(`FieldUnitView` 다운캐스트는 남음).
- 범위: `ViewSystem/Combat/` 안의 표시·입력 계약과 구현, 그리고 이 표시를 조립하는 줄(`Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`·`CombatSceneInputFactory.cs`).
  - 조립 줄은 CLAUDE.md가 허용하는 연결 작업("Factory에서 우리 시스템을 조립하는 줄")이다. 민형님께 공유한다(§10).
  - 우리 영역이지만 이번에 고치지 않는 파일: `UnitHUDLayer`(`Display/Unit/`), `CombatActionPanel`(`Display/Hud/`), 결과 화면 `CombatEndUI`(CLAUDE.md 영역 목록).
  - 다른 작업자 영역은 가방 `PackUI`(GlobalSystem UISystem) 하나이고, 고치지 않는다.
  - 골격·Core·Framework·GlobalSystem·다른 씬도 고치지 않는다.
- 기준 판: HEAD `eafd42f5`. 줄 번호는 이 판 기준이다.
- 규칙: View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. `Assets/Tests`(435개)에서 바뀌는 형식을 쓰는 곳은 없다. 리플렉션 문자열도 없다(11-07 grep·검토).

## 1. 지금 구조 (확인 사실)

### 1.1 `CombatSceneDisplay`가 하는 일

`Display/CombatSceneDisplay.cs`의 크기:
- 876줄, 필드 46개
- 생성자 인자 38개(177줄 한 줄에 셋)
- 구현하는 계약 넷: `ICombatBattleDisplay`·`ICombatUnitInspection`·`ICombatGimmickRangeFeedback`·`ICombatNexusRangeFeedback`

| 묶음 | 줄 | 내용 |
|---|---|---|
| 수명·순서 | 228-287, 291-337, 422-444 | `Install`·`Refresh`(`RefreshCore`)·`Clear`·`Release`. 정리 묶음 셋(`_fieldBound`·`_gimmickClears`·`_gimmickReleases`), 목록 넷(Install 앞부분·Install 실패·Clear·Release) |
| 유닛 준비·미리 만들기 | 231-240, 345-372 | `CreateUnitPreparation`, `Prewarm`(키별 수·플레이어 키 계산) |
| 필드 그리기 | 257-265, 307-308, 318-325 | 필드 `Build`·바탕 높이·타일 연결·레일 설치·정적 강조, 표면 높이 맞춤, 오브젝트·지형·흔적·기믹·가시밭 그리기 |
| 선택·목표 강조 | 67-79, 385-420, 494-502 | 표시 목록 사실(`_displayedCharacters`·`_displayedTurn`·`_displayedPhase`)과 목표(`_objectiveTarget`), `RefreshInspectionSelection`, 선택·목표 강조, 행동 커서, 정보창 누름 `InspectUnit` |
| 화면 표면 | 254-255, 311, 326-331, 446-486 | HUD 골드·턴 표시·가방 재고·정보창, 국면 배경음, 결과 화면과 승리·완료 뒤 배경음, 행동 버튼 연결·가용성·숨김 |
| 입력 피드백 위임 | 490-825 | 해제 확인 뒤 협력 객체에 넘기는 멤버 49개(아래) |
| 자원 해제 | 833-851 | 정보창 비우기, 유닛 HUD 층 GameObject 파괴 |

입력 피드백 위임 49개의 내역:
- 개수: 포인터 8, 고스트 7, 가방 끌기 3, 호버 칸 2, 공격 범위·영향·막힘·체력 8, 호버 범위 2, 기믹·본부 범위 4, 궤적 3, 넉백 2, 공격 모드 대상 2, 이동 초안·범위 8.
- 대부분은 넘기기만 한다. 다른 일을 하는 것:
  - `InnerCellInset`·`CursorSnapThreshold`는 해제를 보지 않는다.
  - `ShowHoverRange`는 원점을 찾고 없으면 예외를 던진다.
  - `MoveGhostToPointer`·`UpdateMovementPointer`는 좌표를 바꾼다.

### 1.2 묶음 계약 `ICombatBattleDisplay`

- `Display/ICombatBattleDisplay.cs:22-24`가 하위 계약 열둘을 상속한다.
  - `ICombatPointerPicker`·`ICombatInteractionFeedback`·`ICombatMovementFeedback`·`ICombatPointerMapping`
  - `ICombatCursorDevice`·`ICombatActionDisplay`·`ICombatPointerOcclusion`·`ICombatItemDragFeedback`
  - `ICombatUnitRangeFeedback`·`ICombatAttackTrajectoryFeedback`·`ICombatKnockbackPreviewFeedback`·`ICombatAttackModeTargets`
- 자기 멤버는 여덟이다: `EventPlayback`·`CameraControl`·`ShowCompletion`·`Install`·`Refresh`·`Clear`·`Release`·`BindItemUnequip`.
- 운영 구현은 `CombatSceneDisplay` 하나다. 시험 가짜는 없다.
- 조정자 쪽이 부르는 것은 자기 멤버와 행동 버튼 둘뿐이다.
  - `CombatViewSystem.cs`
    - 296 `Clear`
    - 455 `CreateUnitPreparation`(구체 형식)
    - 464 `AttachCreated(created, root, created, created.EntryPresentation, …)`. 셋째 인자가 정보창 선택 계약이다.
    - 527·714 `Release`, 559 `EventPlayback`
    - 593-594 `BindActions`·`SetActionsAvailable`
    - 607 `Install`, 649 입력 생성, 806 `ShowCompletion`
  - `CombatResultConsumer.cs:118` `Refresh`
  - `CombatViewConnection.cs`: 98·157 `BindItemUnequip`, 44 입력 표면 `new CombatInputSurfaces(display)`
- 하위 계약 열둘은 입력 Factory가 `display` 하나를 각 입력 객체에 여러 번 넘길 때 쓰인다(`CombatSceneInputFactory.cs:83·98·152·154·157·159·161-162·163·172`).
- 입력 Factory가 형식 검사를 둘 한다(51-54): `display is not ICombatGimmickRangeFeedback`, `display is not ICombatNexusRangeFeedback`. 두 계약은 묶음 계약 밖이라 실행 중에 꺼낸다(C2-15).

### 1.3 하위 계약마다 쓰는 쪽과 실제 주인

| 계약 | 멤버 | 쓰는 쪽 | 실제로 일하는 객체 |
|---|---|---|---|
| `ICombatPointerPicker`·`ICombatPointerMapping` | `TryPickCell` / `InnerCellInset`·`TryMapPointer` | 입력 대부분 | `CombatPointerMapper`가 **이미 구현**한다(`Display/Feedback/CombatPointerMapper.cs:23`) |
| `ICombatPointerOcclusion` | `IsPointerOverUi` | Router·카메라·환경 호버·유닛 호버 | 매퍼 `internal IsPointerOverUi`(167) |
| `ICombatCursorDevice` | 스냅 켜짐·감도·칸 중심 화면 좌표·커서 옮기기 | 커서 스냅 | 켜짐·감도는 씬 루트 값(실행 중 Inspector 변경을 읽음), 나머지는 매퍼(126·152) |
| `ICombatInteractionFeedback`(17) | 고스트·원본 숨김 7 | 배치 Drag만 | `CombatPlacementFeedback`(포인터로 옮길 때는 매퍼의 서는 점) |
| 〃 | 호버 칸 2 | 호버 추적(배치·공격·아이템 안) | `FieldView.SetHoverCell`·`ClearHoverCell` |
| 〃 | 범위·영향·막힘·체력 8 | 공격 조준만 | `CombatAttackFeedback` |
| `ICombatMovementFeedback`(+ 범위 2) | 초안·편집·포인터·범위 8 | 경로 입력·범위 추적 | `CombatMovementPlanningPresenter`(이름이 다름 — `BeginEditing`·`UpdatePointer`·`ShowRange`, 포인터는 매퍼의 서는 점으로) |
| `ICombatUnitRangeFeedback` | 호버 범위 2 | 유닛 호버 | `CombatAttackFeedback.ShowHoverRange(…, sourceCells)`, 원점 칸은 표시 목록에서 찾음(632-641) |
| `ICombatAttackTrajectoryFeedback`·`ICombatKnockbackPreviewFeedback` | 3·2 | 공격 조준 | `CombatAttackFeedback` |
| `ICombatAttackModeTargets` | 2 | 공격 조준 | `CombatSceneEventPresenter`(271-282, Mapper가 없으면 값만 저장) |
| `ICombatGimmickRangeFeedback`·`ICombatNexusRangeFeedback` | 2·2 | 환경 호버 | `FieldView` 강조 층 |
| `ICombatItemDragFeedback` | 3 | 아이템 Drag | 가방 `PackUI`(`ICombatPackDisplay`를 명시 구현, `PackUI.cs:25·182-205`) |
| `ICombatActionDisplay` | 2 | 조정자·입력 표면 | `CombatActionPanel`(씬 UI, 연결 사이에 살아 있음) |
| `ICombatUnitInspection` | `Selection`·`InspectUnit` | 조정자·입력 분배 | 선택 상태·정보창 Presenter·포인터·HUD 층·필드 강조를 표시가 엮음 |

- `ICombatInteractionFeedback`를 받는 넷은 서로 겹치지 않는 부분만 쓴다(11-07 grep·검토).
  - 배치 Drag는 고스트 7을 쓴다.
  - 공격 조준은 범위·영향 8을 쓴다.
  - 호버 추적은 호버 칸 2를 쓴다.
  - 아이템 Drag는 받은 계약을 호버 추적(`new CombatHoverTracker(feedback, kind)`)에만 넘긴다. 배치·공격도 호버 추적에 넘긴다.

### 1.4 해제 뒤 규칙(D9)과 그것을 지키는 곳

D9(표시 머리 주석 45-47줄):
- 해제 뒤 사건 시작·Install·Refresh·완료 표시는 예외를 던진다.
- 표시 의도·집기는 아무것도 하지 않는다(집기는 false).
- 연결 해제(콜백 모두 null)는 해제 뒤에도 전달한다.

지금은 표시가 위임 메서드마다 `_isReleased`를 본다. `Release`가 맨 먼저 이것을 세우므로(434), 해제 목록이 도는 동안에도 의도는 이미 막힌다.

일하는 객체도 셋은 스스로 지킨다.
- `CombatPointerMapper`: `IsBound => !_isReleased && _mapper != null`(48)을 모든 입구가 본다. 자기 `Release`(66-75)가 필드·카메라를 비운다.
- `CombatMovementPlanningPresenter`: 자기 `_released`(22)를 입구마다 확인한다.
- `FieldView`: 강조 입구는 `_mapper == null`이면 돌아간다(230·238·256·276). `Release`가 Mapper를 비운다(452).
  - 이 확인은 "해제됨"이 아니라 "아직 Build 안 됨"을 본다. `FieldView`는 씬 객체라 다음 Build 때 다시 살아난다.
  - 씬 로드 한 번에 연결은 하나라서(`CombatViewSystem.cs:287-288`) 운영에서는 같은 뜻이다.
  - `SetLayerCells`·`SetAffectedCells`는 칸 목록이 null이면 Mapper 확인보다 먼저 예외를 던진다(253-254, 265-266).

스스로 지키지 않는 것:
- `CombatPlacementFeedback`·`CombatAttackFeedback`: 해제 상태가 없다.
- 행동 버튼 패널·가방: 연결 사이에 살아 있어 연결의 해제를 모른다.

해제 순서(`CombatViewSystem.Detach`, 679-718):
1. 준비 취소 → 확인창 닫기 → 루트 구독 해제 → 진입 연출 취소 → 재생 중단
2. **입력 정지(`StopInput`)·입력 연결 해제**
3. 연결 비움 → 연출 끊기
4. **표시 `Release`**

입력이 표시 해제보다 먼저 선다. 지금 해제 목록과 그 콜백은 입력 피드백을 부르지 않는다(검토가 확인).
- 정보창 Presenter `Clear`는 선택 바뀜 알림을 내지 않는다.
- 덱 카드의 `OnUIHidden`이 읽는 연결은 그때 이미 null이다.
- 인수 전 해제 길(`ReleaseUnowned`)에는 아직 입력이 없다.

따라서 D9는 늦게 오는 Unity 콜백을 막는 안전장치다.

### 1.5 순서가 중요한 곳

- `Install`(242-287)
  1. 해제 확인 → 선택 비우기(245) → 턴 표시 비우기(246) → 캐릭터 스냅숏(248) → 정리 묶음 `_fieldBound`
  2. 결과 화면 닫기 → HUD 표시
  3. 필드 `Build` → 바탕 높이 → 타일 연결 → 레일 설치 → 사건 Presenter·포인터 연결 → 정적 강조
  4. 진입 준비 → `RefreshCore` → 미리 만들기 → 진입 연출 준비
  5. 실패하면 선택을 비운 뒤 정리 목록 하나를 돈다(275·279).
- `RefreshCore`(297-337)
  1. 재생 끝 검사 → 표시 목록 사실 기록 → 정보창 사실 동기화(304) → 표면 높이 맞춤(바뀌면 다시 그림) → 국면 배경음 → 목표 대상 정하기
  2. 사실 그리기 `_events.Refresh`. 안에서 정보창 Presenter가 선택 바뀜 알림으로 강조를 맞춘다.
     - 이때의 UI 콜백(`ShowSelection`)이 연결을 놓을 수 있다.
     - 놓으면 이어지는 `_objects.Render`가 Mapper null로 `ArgumentNullException`을 던진다(`FieldObjectPresenter.cs:66`, 지금도 있는 잠재 동작).
  3. 행동 커서 → HUD 선택 → 오브젝트 → 지형 → 흔적 → 기믹(진입 보류 경유) → 가시밭
  4. HUD 골드 → 턴 표시 → **진입 배치 표시**(같은 턴 표시 UI·덱 카드를 다룸) → 가방 → 정보창
  5. **해제 확인**(정보창 UI 콜백이 연결을 놓을 수 있음) → 선택 강조 → 목표 강조
- `Clear`(425-426)·`Release`(439-443)
  - 목록 하나를 차례로 돈다.
  - 한 항목이 실패해도 나머지를 하고 실패를 모은다(`CleanupErrors.RunAll`).
  - 묶음은 `Combine`으로 펼쳐 끼우고 중첩 실행하지 않는다. 실패가 한 겹으로 모인다(853-870).
- `InspectUnit`(494-502): 정보창 선택 → HUD 선택 → **해제 확인**(정보창 UI 콜백이 연결을 놓을 수 있음) → 선택 강조.

### 1.6 Factory

- `CombatSceneDisplayFactory.Create`(67-206)가 협력 객체를 만들고, 38인자 생성자를 위치 인자로 부른다(183-188).
- 입력 Factory(`CombatSceneInputFactory`)는 `display` 하나를 열 곳 넘게 넘긴다.

### 1.7 다른 작업자 줄

- 얕은 복제의 blame은 경계 커밋(`^Droute` 519줄)이 대부분이라 믿을 수 없다.
- 11-04 확인 기록은 표시 파일을 "용근(Claude) 44·호진님 15·현강님 12·천명님 4"로 적었다.
- 착수 전(DS-0)에 전체 이력으로 다시 센다. 선택 인자 묶음 OA-0과 같은 방법이다. 셀 파일:
  - 표시 파일, 입력 Factory
  - 피드백 셋, 입력 기능 넷(배치·공격·아이템·호버 추적)
  - 포인터 매퍼, `FieldView`, 이동 초안 표시, 사건 Presenter

## 2. 목표와 비목표

- 목표
  - 표시(`CombatSceneDisplay`)에는 수명(설치·결과 끝 그리기·정리·해제)과 그 순서만 남긴다.
  - 입력 피드백 하위 계약은 실제로 일하는 객체가 구현하고, 입력 Factory는 그 객체를 계약 이름으로 받는다. 표시의 위임 층과 형식 검사 둘을 없앤다.
  - 선택·목표 강조, HUD 표면, 행동 버튼, 결과 화면, 필드 그리기를 각각 객체로 나눈다.
  - 생성자 인자는 이름으로 넘긴다.
  - 보이는 동작은 같다(§4).
- 비목표
  - 기믹 공통 계약(C2-01 2단계)·사건 처리기 조립 옮기기(C4-02 3단계)는 다음 묶음 ③에서 한다.
    - 이 묶음의 필드 표시는 기믹 표시 넷을 지금처럼 하나씩 받고, 정리·해제 묶음을 `Action[]` 그대로 둔다.
    - ③은 그 자리만 공통 목록으로 바꾼다.
  - `PackUI`·`CombatEndUI`·`UnitHUDLayer`·`CombatActionPanel`은 고치지 않는다.
  - 조정자 계약의 자기 멤버(설치·그리기·정리·해제·결과 화면·재생 계약·카메라 시점)는 동작을 바꾸지 않는다.

## 3. 설계

### DS1 연결 수명 `CombatDisplayLifetime` — 해제 뒤 규칙의 원본 하나 (Q-DS3 A)

- 새 `Display/CombatDisplayLifetime.cs`(sealed): `IsReleased`와 `MarkReleased()`를 가진다.
  - 표시의 `_isReleased`를 대신한다.
  - 표시 `Release`의 첫 줄(지금 434와 같은 자리)에서 `MarkReleased()`를 부른다.
- 막는 것은 **입력 표시 의도·집기·새 연결뿐이다.** 이것을 읽는 객체는 넷이다.
  - 고스트 피드백
  - 공격 피드백
  - 선택 표시(`InspectUnit`)
  - HUD 표면(가방 끌기)·행동 버튼(새 연결·가용성)
- 막지 않는 것:
  - 숨기기·정리 항목(행동 버튼 숨김·연결 해제(null)·결과 화면 닫기·정보창 비우기 등)
  - 결과 끝 그리기·설치 단계. 지금처럼 표시가 해제 확인으로 예외를 던지고, 하위 단계에는 새 확인을 넣지 않는다. 그래서 §1.5의 정보창 UI 콜백 뒤 동작도 그대로다.
- 포인터 매퍼·이동 초안·`FieldView`의 자기 상태는 그대로 둔다.
  - 이것은 "자기 자원을 놓았음"이라는 다른 사실이다.
  - 표시 해제가 시작된 순간부터 매퍼·이동 초안 입구를 막는 것은 이 객체가 아니라 그 자기 상태다.
  - 표시 해제는 입력이 선 뒤에만 불리고, 해제 목록은 입력 피드백을 부르지 않는다(§1.4). 그래서 운영에서 닿는 차이는 없다(P4).

### DS2 입력 피드백은 실제 주인이 구현한다 (C2-03 2단계)

- 묶음 계약 `ICombatBattleDisplay`는 하위 계약 열둘을 더는 상속하지 않는다.
  - 대신 입력 피드백 묶음 `CombatInputFeedback`을 `Feedback`으로 내놓는다(새 `Display/CombatInputFeedback.cs`, sealed class, 계약 형식의 읽기 전용 속성).
  - 입력 Factory는 `display.Feedback.Picker`·`.Placement`처럼 이름으로 꺼내 넘긴다.
  - 형식 검사 둘(§1.2)은 없어진다.
- 묶음 속성과 구현:

| 속성 | 계약 | 구현 |
|---|---|---|
| `Picker`·`Mapping`·`Occlusion`·`Cursor` | `ICombatPointerPicker`·`ICombatPointerMapping`·`ICombatPointerOcclusion`·`ICombatCursorDevice` | `CombatPointerMapper` 하나(뒤 둘을 더 구현, 스냅 켜짐·감도는 씬 루트를 받아 매번 읽음, P5) |
| `Placement` | 새 `ICombatPlacementFeedback`(고스트·원본 숨김 7) | `CombatPlacementFeedback` |
| `HoverCell` | 새 `ICombatHoverCellFeedback`(호버 칸 2) | `FieldView`(명시 구현, 궤적·넉백 캔버스 계약과 같은 방식) |
| `AttackRange` | 새 `ICombatAttackRangeFeedback`(범위·영향·막힘·체력 8) | `CombatAttackFeedback` |
| `UnitRange`·`Trajectory`·`Knockback` | 기존 셋 | `CombatAttackFeedback` |
| `AttackModeTargets` | 기존 | `CombatSceneEventPresenter`(명시 구현) |
| `Movement` | `ICombatMovementFeedback`(+ 범위) | `CombatMovementPlanningPresenter`(명시 구현이 이름을 잇고, 화면 좌표는 매퍼로 서는 점을 구함) |
| `GimmickRange`·`NexusRange` | 기존 둘 | `FieldView`(명시 구현) |
| `ItemDrag` | `ICombatItemDragFeedback` | HUD 표면(DS4)이 가방에 넘긴다(P6) |

- **`ICombatInteractionFeedback`는 셋으로 나눈다(Q-DS2 A).** 넷 다 쓰는 부분이 겹치지 않으므로(§1.3), 쓰는 쪽은 자기가 쓰는 계약만 받는다.
  - 배치 Drag: `(…, ICombatPlacementFeedback placement, ICombatHoverCellFeedback hover, …)`
  - 공격 조준: `(…, ICombatAttackRangeFeedback range, ICombatHoverCellFeedback hover, …)`
  - 아이템 Drag·호버 추적: `ICombatHoverCellFeedback`
  - `ICombatInteractionFeedback.cs`는 `git mv`로 `ICombatAttackRangeFeedback.cs`가 된다. 나머지 둘은 새 파일이다.
  - 셋 다 `Input/Contracts/`에 둔다(폴더 안내 §2 "입력 기능 여럿이 표시에 요청하는 계약").
- 호버 범위 원점은 표시 스냅숏(DS3 `CombatDisplaySnapshot`)에서 찾고, 공격 피드백이 그 스냅숏을 받는다. 찾는 규칙과 예외 문구는 지금(632-641)과 같다. 태그만 `[CombatSceneDisplay]` → `[CombatAttackFeedback]`으로 바뀐다.
- 위임 층이 없어진 뒤에도 주인 쪽에 이름을 잇는 명시 구현 줄이 약 25개 남는다(이동 초안 8, `FieldView` 6, HUD·행동 버튼 5, 매퍼 3~5, 사건 Presenter 2). 없어지는 것은 "표시가 해제를 확인한 뒤 넘기는 층"이다.

### DS3 선택 표시 `CombatSelectionDisplay`와 표시 스냅숏 `CombatDisplaySnapshot`

- 새 `Display/CombatDisplaySnapshot.cs`: 마지막 결과 끝 그리기의 캐릭터 스냅숏·턴·국면이다. 지금의 `_displayedCharacters`·`_displayedTurn`·`_displayedPhase`를 옮긴다.
  - 쓰는 곳은 표시 하나다. `RefreshCore`가 기록하고, 선택 비우기가 비운다.
  - 읽는 곳은 둘이다: 선택 표시(행동 커서), 공격 피드백(호버 범위 원점).
  - 이름은 기존 `CombatShownFacts`(재생 중 보인 사실)·`CombatInspectionFacts`와 헷갈리지 않게 "스냅숏"으로 했다.
- 새 `Selection/CombatSelectionDisplay.cs`: 선택·목표 강조를 맡고, `ICombatUnitInspection`을 구현한다. 멤버:
  - `Synchronize(stage, owned, traits)`: 정보창 사실 동기화(지금 304)
  - `SetObjective(objective)`: 목표 대상(314)
  - `AfterFacts()`: 행동 커서 → HUD 선택(316-317)
  - `RenderHighlights()`: 선택 강조 → 목표 강조(335-336)
  - `RefreshInspectionSelection()`: 선택이 바뀔 때 HUD 선택 → 선택 강조 → 행동 커서 → 목표 강조(67-74). Factory가 정보창 선택 바뀜 알림에 건다(지금 189).
  - `Clear()`: 정보창 비우기 → HUD 선택 비우기 → 목표 비우기
  - `ReleaseInspection()`: 정보창 Presenter 해제
  - `InspectUnit`: 지금 494-502와 같다. 둘째 해제 확인은 공유 수명을 본다.
- 조정자는 `AttachCreated(created, root, created.Inspection, …)`로 이 객체를 정보창 선택 계약으로 받는다(464, P1).

### DS4 화면 표면 셋 — HUD 표면·행동 버튼·결과 화면 (Q-DS5 A)

- 새 `Display/Hud/CombatHudSurfaces.cs`: 결과 끝에 화면 공간 UI를 그린다.
  - `Present()`: HUD 표시
  - `RenderStatus(runGold, turn, phase)`: HUD 골드 → 턴 표시(326-327)
  - `RenderPanels(inventory, stagePhase)`: 가방 재고 → 정보창(330-331)
  - `ClearHud()`·`ClearTurn()`·`ClearUnitInfo()`: 정보창 비우기의 주인은 여기 하나다.
  - 가방 끌기 그림 `ICombatItemDragFeedback`: 공유 수명으로 막고 가방에 넘긴다(P6).
  - 그리기를 둘로 나누는 까닭: 그 사이에 진입 배치 표시(328)가 같은 턴 표시 UI·덱 카드를 다루므로 순서를 지켜야 한다.
- 새 `Display/Hud/CombatActionButtonsDisplay.cs`: `ICombatActionDisplay`를 구현한다.
  - 새 연결과 가용성은 공유 수명으로 막는다. 연결 해제(모두 null)와 숨김은 막지 않는다(지금 470-486).
  - 조정자는 `display.Actions`로 부르고(593-594), 입력 표면은 `new CombatInputSurfaces(display.Actions)`로 만든다.
- 새 `Display/Hud/CombatResultScreenDisplay.cs`: 결과 화면을 맡는다(지금 446-464).
  - `Show(completion, confirm)`: 승리면 승리 효과음, 확인이 받아들여지면 완료 뒤 배경음
  - `Hide()`: 정리 목록에서 불리므로 파괴된 객체를 거른다.
  - `ShowCompletion`은 조정자 계약에 남는다. 표시가 해제 확인(예외)과 인자 검사 뒤 넘긴다(P2).
- 배경음 정책 `CombatBgmPolicy`는 Factory가 하나 만들어, 표시(국면 배경음, 311)와 결과 화면(승리·완료 뒤)에 같은 객체를 넘긴다. 규칙의 원본은 여전히 정책 하나다.
  - 1판은 화면 표면 하나가 배경음까지 맡았다. 검토 M6의 지적대로, 그러면 C2-03의 실패 시나리오("입력 쪽 표면 추가와 배경음 규칙 변경이 같은 생성자에서 충돌")가 그 객체 안에서 다시 생긴다.

### DS5 필드 표시 `CombatFieldDisplay`

- 새 `Display/Map/CombatFieldDisplay.cs`: 필드 위 물체를 구성하고 그린다.
  - `Build(state)`: 필드 `Build` → 바탕 높이 → 타일 연결 → 레일 설치(257-262)
  - `ApplyStaticHighlights(state)`: 정적 강조(265)
  - `SyncSurface(state)`: 표면 높이 맞춤, 바뀌면 다시 그림(307-308)
  - `RenderObjects(values)`: 오브젝트 → 지형 → 흔적 → 기믹(진입 보류 경유) → 가시밭(318-325)
  - 정리 묶음(`Action[]`, 생성 때 한 번 만든다):
    - 정리: `BoundClears`(레일·가시밭), `ObjectClears`(오브젝트·지형), `GimmickClears`(흔적·토네이도·낙석·하붑)
    - 해제: `BoundReleases`·`ObjectReleases`·`GimmickReleases`
    - `ReleaseField`(필드 해제)
- 사건 Presenter·포인터 연결(263-264)은 표시가 `Build`와 `ApplyStaticHighlights` 사이에 부른다(P4).
- 기믹 표시 넷은 지금처럼 하나씩 받는다. ③이 `GimmickClears`·`GimmickReleases`와 기믹 그리기 자리를 공통 계약 목록으로 바꾼다.

### DS6 미리 만들 수 `CombatPrewarmPlan`

- 새 `Display/Unit/CombatPrewarmPlan.cs`(static): 캐릭터 목록과 본부 증원 명세에서 키별 수와 플레이어 키를 구한다(지금 345-367).
- 표시는 그 결과로 유닛·고스트·사건 Presenter의 `Prewarm`을 지금 순서대로 부른다(368-371).

### DS7 남는 표시와 생성 인자

- `CombatSceneDisplay`에 남는 것(예상 약 300줄):
  - 수명 넷(`Install`·`Refresh`·`Clear`·`Release`)과 그 순서, 정리 목록 넷(§3.8)
  - 선택 비우기 단계 `ClearSelection`(= 선택 표시 `Clear` → 스냅숏 비우기)
  - 유닛 준비 생성, 미리 만들기 호출, 결과 화면 입구, `BindItemUnequip`
  - 속성: 재생 계약·카메라 시점·입력 피드백 묶음·행동 버튼·정보창 선택 계약
  - 유닛 HUD 층 파괴(연결 수명 자원 해제)
- 생성자 인자(약 19개, 모두 이름 인자로 넘긴다):
  - 수명 `CombatDisplayLifetime`, 스냅숏 `CombatDisplaySnapshot`, 입력 피드백 묶음
  - 필드 표시, 선택 표시, HUD 표면, 행동 버튼, 결과 화면, 배경음 정책
  - 유닛 표시, 고스트, 유닛 HUD 층
  - 이동 초안 표시: 정리 목록이 `Clear`·`Release`를 부른다. 묶음 속성은 계약 형식이라 그것으로는 부를 수 없다.
  - 사건 Presenter, 재생 조정, 포인터 매퍼, 카메라 창구, 카메라 시점, 진입 연출
- 고스트 피드백·공격 피드백은 정리 목록에 항목이 없다(지금도 없음). 해제 뒤 규칙은 공유 수명이 맡으므로, 표시는 이 둘을 받지 않는다.

### 3.8 정리 목록과 결과 끝 그리기 대응표

규칙:
- 목록은 표시에 남기고, 항목 순서는 지금과 같다.
- 하위 객체의 묶음은 지금 `_gimmickClears`처럼 `Action[]`으로 받아 `Combine`으로 끼운다.
- 하위 객체 안에서 `RunAll`을 또 돌리지 않는다. 그래야 실패가 한 겹으로 모이고 문구 태그도 그대로다.

목록별 차례:
- 정리 묶음 `_fieldBound`(지금 218-222): 진입 취소 → 사건 보류 취소 → 이동 초안 `Clear` → 사건 Presenter `Clear` → 포인터 `Unbind` → **필드 `BoundClears`**(레일·가시밭).
- Install 실패(지금 275·279): `ClearSelection` 뒤 다음 목록을 돈다.
  - `_fieldBound` → HUD `ClearTurn` → HUD `ClearUnitInfo` → 필드 `ObjectClears` → 필드 `GimmickClears`
- Clear(지금 425-426):
  1. 준비 취소 → `ClearSelection` → `_fieldBound`
  2. HUD `ClearUnitInfo` → 체력 예상 지우기 → 고스트 `Hide` → 유닛 `Release`
  3. 결과 화면 `Hide` → HUD `ClearHud` → HUD `ClearTurn` → 행동 버튼 숨김
  4. 필드 `ObjectClears` → 필드 `GimmickClears` → 필드 `ReleaseField`
- Release(지금 434-443):
  1. 수명 `MarkReleased` → 재생 조정 `MarkReleased`
  2. 준비 취소 → `ClearSelection` → 진입 취소 → 선택 표시 `ReleaseInspection` → HUD `ClearUnitInfo`
  3. 사건 보류 취소 → 이동 초안 `Release` → 체력 예상 지우기 → 포인터 `Release` → 사건 Presenter `Release` → 필드 `BoundReleases`
  4. 결과 화면 `Hide` → HUD `ClearTurn` → 행동 버튼 연결 해제 → 행동 버튼 숨김
  5. 고스트 `Release` → 유닛 `Release` → 유닛 HUD 층 해제 → 유닛 풀 해제 → 유닛 HUD 층 파괴
  6. 필드 `ObjectReleases` → 필드 `GimmickReleases` → 카메라 시점 되돌림 → 카메라 창구 맵 떼기 → 필드 `ReleaseField`
- Install(지금 242-287):
  1. 해제 확인 → `ClearSelection` → HUD `ClearTurn` → 캐릭터 스냅숏 → `RunAll(_fieldBound)`
  2. 결과 화면 `Hide` → HUD `Present`
  3. 필드 `Build` → 사건 Presenter·포인터 `Bind` → 필드 `ApplyStaticHighlights`
  4. 진입 준비 → `RefreshCore` → 미리 만들기 → 진입 연출 준비
- RefreshCore(지금 297-337):
  1. 재생 끝 검사 → 스냅숏 기록 → 선택 표시 `Synchronize` → 필드 `SyncSurface` → 배경음 국면 → 선택 표시 `SetObjective`
  2. 사건 Presenter `Refresh` → 선택 표시 `AfterFacts` → 필드 `RenderObjects`
  3. HUD `RenderStatus` → 진입 배치 표시 → HUD `RenderPanels`
  4. 해제 확인 → 선택 표시 `RenderHighlights`

`ClearSelection` 안의 순서만 바뀐다(P15).
- 지금(385-392): 정보창 → HUD 선택 → 표시 목록 → 목표
- 바뀐 뒤: 선택 표시 `Clear`(정보창 → HUD 선택 → 목표) → 스냅숏 비우기
- 목표·표시 목록은 값만 비우는 줄이라 차례가 바뀌어도 보이는 차이는 없다.

## 4. 지킬 것 (동작 같음)

- `Install`·`RefreshCore`·`Clear`·`Release`·`InspectUnit`의 단계 순서(§1.5)는 하나도 바꾸지 않는다. 대응표는 §3.8이다.
- 해제 확인 두 곳(`RefreshCore` 정보창 뒤, `InspectUnit` 정보창 뒤)은 그대로 둔다.
- 사실 그리기 안의 UI 콜백 뒤에 새 해제 확인을 더하지 않는다. 지금 잠재 동작(§1.5 2단계)을 바꾸지 않으려는 것이다.
- D9:
  - 해제 뒤 표시 의도는 아무것도 하지 않고, 집기는 false다.
  - 연결 해제(null)는 해제 뒤에도 전달한다.
  - 설치·그리기·결과 화면은 해제 뒤 예외다.
  - 해제가 시작되는 순간(공유 수명)부터 막는다.
- 예외 종류와 문구는 같다. 태그가 바뀌는 것은 호버 범위 원점 하나다(`[CombatAttackFeedback]`, P8).
- 다음은 같다: 미리 만들기 수, 배경음 시점, 행동 버튼 보임·누름 가능, 커서 스냅 켜짐·감도(실행 중 Inspector 변경을 읽음).

## 5. 바뀌는 것 (보이는 동작 없음)

- 새 파일 열하나(+ `.meta`):
  - `Display/CombatDisplayLifetime`, `Display/CombatDisplaySnapshot`, `Display/CombatInputFeedback`
  - `Selection/CombatSelectionDisplay`
  - `Display/Hud/CombatHudSurfaces`, `Display/Hud/CombatActionButtonsDisplay`, `Display/Hud/CombatResultScreenDisplay`
  - `Display/Map/CombatFieldDisplay`, `Display/Unit/CombatPrewarmPlan`
  - `Input/Contracts/ICombatPlacementFeedback`, `Input/Contracts/ICombatHoverCellFeedback`
- 이름 바꿈 하나(`git mv`, `.meta` GUID 유지): `ICombatInteractionFeedback` → `ICombatAttackRangeFeedback`. 지우는 파일은 없다.
- 바뀌는 파일:
  - 표시, 묶음 계약, 두 Factory
  - 포인터 매퍼, `FieldView`
  - 고스트·공격·이동 초안 피드백, 사건 Presenter
  - 입력 기능 넷(배치·공격·아이템·호버 추적)
  - 조정자(두 자리), 연결 상태(한 자리)
- 크기 예상: 표시 876 → 약 300줄. 전체로는 약 +200줄이다. 새 파일 머리 주석과 명시 구현 줄이 늘고, 위임 층이 빠진다.
- 운영에서 닿지 않는 차이:
  - 해제 시작 뒤 매퍼·이동 초안 입구를 막는 것은 그 객체의 자기 상태다. 이 상태는 해제 목록에서 그 항목이 돌 때 선다.
  - 해제 뒤 기믹·본부 범위에 null 목록을 넘기면, 지금은 아무것도 안 하지만 바뀐 뒤에는 `FieldView`가 `ArgumentNullException`을 던진다.
  - 표시 해제는 입력이 선 뒤에만 불리므로 둘 다 운영에서 닿지 않는다(§1.4). DS-0에서 다시 확인한다.

## 6. Phase(안)

검토 M1을 반영해 순서를 바꿨다. 계약 상속을 걷기(DS-b) 전에 그것을 받을 객체(DS-a)를 먼저 만들어, Phase마다 컴파일되게 한다.

| Phase | 내용 | Unity 확인(안) |
|---|---|---|
| DS-0 | 착수 전 확인: 원격, 전체 이력 작성자(§1.7), 시험 grep, 해제 목록이 입력 피드백을 부르지 않는지, 정보창 UI 콜백 지점 | — |
| DS-a | DS1 공유 수명, DS3 스냅숏·선택 표시, DS4 HUD 표면·행동 버튼·결과 화면. 조정자·연결 상태가 `display.Actions`·`created.Inspection`을 쓴다. 묶음 계약은 아직 하위 계약을 상속하고, 표시의 행동 버튼·가방 끌기 멤버는 새 객체로 넘긴다(DS-b에서 걷음) | 선택·목표 강조, 행동 커서, 정보창, 행동 버튼, HUD 골드·턴 표시·가방, 결과 화면·배경음 |
| DS-b | DS2 입력 피드백: 계약 셋, 주인 구현, 묶음, 입력 Factory, 묶음 계약 상속·표시 위임 층 걷기 | 배치 고스트·호버, 공격 범위·영향·궤적·넉백·체력, 유닛·기믹·본부 호버 범위, 이동 초안·고스트, 아이템 끌기, 커서 스냅, 공격 모드 대상 이펙트 |
| DS-c | DS5 필드 표시, DS6 미리 만들 수, DS7 생성 인자·정리 목록(§3.8)·표시 정리 | 전투 진입(보드·기믹 등장), 결과 끝 그리기, 전투 다시 시작·씬 나가기 뒤 Hierarchy |
| DS-d | 문서·공유·폴더 배치 안내(`Selection/` 줄 포함)·묶음 끝 검토·평가 아티팩트 10차 | — |

- Unity 확인 횟수는 Q-DS4로 정한다.

## 7. 7기준 점검 (설계안)

| 기준 | 점검 |
|---|---|
| SSOT | 보인 목록은 스냅숏 하나, "이 연결의 표시가 해제됨"은 공유 수명 하나, 커서 스냅 값은 씬 루트 하나, 배경음 규칙은 정책 하나다. 매퍼·이동 초안·`FieldView`의 상태는 자기 자원의 사실이라 다른 사실이다. |
| SOLID | SRP: 표시는 수명·순서를 맡고, 하위 객체는 바뀌는 이유가 하나씩이다(필드 그리기, 선택 강조, HUD 표면, 행동 버튼, 결과 화면, 입력 피드백 주인). ISP: 상호작용 피드백 17을 셋으로 나누고, 조정자 계약이 입력 계약을 더는 상속하지 않는다. OCP: 새 입력 피드백은 묶음 속성 하나와 구현 객체로 더하고 표시는 건드리지 않는다. |
| FW 계약 | 서비스 등록·수명은 그대로다. Factory는 조립 줄만 바뀐다. |
| 기능 변경 독립성 | 평가의 실패 시나리오가 없어진다. 입력 피드백 추가는 입력 Factory·구현 객체에서, 배경음 규칙 변경은 정책에서 고친다. 같은 생성자를 함께 고치지 않는다. |
| 불필요/우회 | 표시 위임 층(해제 확인 두 겹), 입력 Factory 형식 검사 둘, `ICombatInteractionFeedback`에서 쓰지 않는 멤버를 받던 것이 사라진다. |
| C#/Unity 권장 | 명시 인터페이스 구현(기존 `ICombatTrajectoryCanvas` 선례), 이름 인자, 정적 계산 함수, 연결 수명 객체. |
| 매 프레임 | 입력 피드백 호출마다 한 단계가 줄어든다. 무시할 만하다. |

- 점수 어림(같은 감점표):
  - 다 풀림으로 셀 때: C2 SOLID 75 → 82.5, 요청 7기준 80.4 → **81.5**(+1.07), 12기준 85.0 → **85.6**(+0.625), 중간 해결 19 → 20.
  - 다시 평가할 때 남는 것(HUD 층 파괴, 가방 끌기 넘김 셋, 조정자 계약의 `Feedback` 속성)을 C2-03 "일부"로 읽으면 점수는 그대로다. 그래서 범위는 **+0 ~ +1.07**로 본다.
  - 1판보다 화면 표면을 더 잘게 나눠, 새 중간이 생길 위험은 줄였다.
  - 함께 풀리는 낮음(C2-17, C2-15 일부)은 낮음 상한 때문에 점수에는 그대로다.

## 8. 정할 것

결정(11-07 사용자 "Unity 확인은 Phase 모두 종료 후로 하고, 나머지는 권장대로"): Q-DS1·Q-DS2·Q-DS3·Q-DS5 = A, **Q-DS4 = B**.

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-DS1 | 범위 | **A** DS1~DS7 모두. 평가가 적은 세 방향을 다 해야 C2-03이 다 풀린다. 필드 표시(DS5)는 옮기기만 하고 기믹 넷은 지금 모양 그대로 둬서, ③이 그 자리만 바꾼다 | B 입력 피드백(DS1·DS2)만. C 필드 표시는 ③에서 기믹 계약과 함께 |
| Q-DS2 | `ICombatInteractionFeedback`(17) 나누기 | **A** 셋(배치·호버 칸·공격 범위)으로 나눈다. 쓰는 쪽 넷이 서로 겹치지 않아 생성자 인자만 바뀐다 | B 그대로 두고 조립 객체 하나가 구현한다(넘기기 17개가 그 객체로 옮겨 남음) |
| Q-DS3 | 해제 뒤 규칙(D9)을 지키는 곳 | **A** 연결 수명 공유 객체 `CombatDisplayLifetime` 하나. 표시의 `_isReleased`를 대신하고 해제 시작 순간 한 번 세운다. 읽는 객체는 넷이다. 1판 권장(객체마다 자기 상태)에서 바꿨다. 새 상태 넷이 모두 "표시가 해제됨"이라는 같은 사실의 사본이 되고, 세우는 시점도 목록 자리마다 달라지기 때문이다(검토 M4·M5) | B 객체마다 자기 해제 상태를 두고, 표시 해제 목록 맨 앞에서 각자의 `Release`를 부른다(새 타입은 없지만 같은 사실이 넷 더 생기고, 표시가 고스트·공격 피드백을 더 받음) |
| Q-DS4 | Unity 확인 | **A** Phase마다(DS-a·DS-b·DS-c 뒤). 구조 변경이 넓어 문제가 나면 원인을 좁힌다 | B 모든 Phase 뒤 한 번(작은 묶음 셋 Q-SB4처럼) |
| Q-DS5 | 화면 표면 나누기 | **A** 셋(HUD 표면·행동 버튼·결과 화면). 배경음 정책은 따로 두고, 표시와 결과 화면이 같은 객체를 받는다 | B 둘(HUD 표면 / 결과 화면 + 행동 버튼). C 하나(1판, 검토 M6이 새 God 위험을 지적) |

## 9. 설계에서 정한 것 (확인 대상)

- P1 조정자 계약 `ICombatBattleDisplay`
  - 자기 멤버 여덟은 그대로다.
  - 더하는 속성은 `Feedback`(입력 피드백 묶음)과 `Actions`(`ICombatActionDisplay`) 둘이다.
  - `Inspection`(`ICombatUnitInspection`)은 구체 표시에만 둔다. 지금도 정보창 선택 계약은 묶음 계약 밖이고, Attach가 구체 형식으로 받는다.
  - `Feedback`·`CameraControl`은 조정자가 쓰지 않고 입력 Factory로 넘기기만 한다(649). 그래도 입력 Factory 계약(`ICombatSceneInputFactory.CreateSceneInput(display, sources)`)을 바꾸지 않으려고 계약에 둔다(검토 L9, 받아들인 한계).
- P2 해제 확인(예외)은 표시에 남는다: `Install`·`Refresh`·`Clear`·`ShowCompletion`·`CreateUnitPreparation`. 하위 객체는 표시가 정한 순서대로 불릴 때만 일한다.
- P3 공유 수명을 읽는 곳은 넷이다: 고스트 피드백, 공격 피드백, 선택 표시 `InspectUnit`, HUD 표면 가방 끌기·행동 버튼(새 연결·가용성). 숨기기·정리·그리기는 읽지 않는다(DS1).
- P4 해제 시작 뒤 포인터 매퍼·이동 초안·`FieldView`를 막는 것은 자기 상태다(DS1). 그 사이에 입력 피드백을 부르는 길이 없는지 DS-0에서 다시 확인한다.
- P5 커서 스냅
  - 켜짐은 매퍼의 해제 상태로만 거른다. Mapper 연결 여부는 보지 않는다. 지금 표시가 해제만 보기 때문이다(520).
  - 감도는 해제와 상관없이 읽는다.
  - 매퍼 `Release`(66-75)는 씬 루트 참조를 비우지 않는다(필드·카메라만 비운다, 검토 L7).
- P6 가방 끌기 그림은 HUD 표면이 가방에 넘긴다(넘기기 셋이 남는다). `PackUI`는 다른 작업자 영역이고 `ICombatPackDisplay`를 명시 구현해서, 계약 상속을 바꾸면 그 파일을 고쳐야 하기 때문이다.
- P7 `FieldView`는 호버 칸·기믹 범위·본부 범위 계약을 명시 구현한다. 해제·정리 뒤에는 Mapper가 비어 아무것도 하지 않는다. 운영에서 같은 뜻인 것은 씬 로드 한 번에 연결이 하나라서다(§1.4).
- P8 예외 문구 태그가 바뀌는 것은 호버 범위 원점 하나다. 문구를 읽는 시험은 없다.
- P9 이동 초안 표시와 고스트 피드백은 화면 좌표 → 서는 점 변환에 포인터 매퍼를 받는다. 그래서 Factory는 매퍼를 이 둘보다 먼저 만든다. 지금은 이동 초안이 먼저(105 → 114)지만, 매퍼는 `FieldView`만 필요해 앞으로 옮겨도 된다.
- P10 공격 모드 대상은 사건 Presenter가 명시 구현한다. Presenter는 Mapper가 없으면 값만 저장하고(271-282) `Release`에서 정리한다. 연결마다 만드는 객체라 해제 뒤 화면 영향은 없다.
- P11 폴더 배치:
  - `Display/` 바로 아래: 공유 수명·스냅숏·묶음(표시 입구에 딸린 값)
  - `Selection/`: 선택 표시
  - `Display/Hud/`: 화면 표면 셋
  - `Display/Map/`: 필드 표시(필드 위 물체가 대부분이고 격자 `Build`도 부름)
  - `Display/Unit/`: 미리 만들 수
  - `Input/Contracts/`: 계약 셋
  - 폴더 배치 안내에 줄을 더한다. `Selection/` 1단 폴더 줄도 새로 넣는다(DS-d).
- P12 `ICombatInteractionFeedback.cs`는 `git mv`로 `ICombatAttackRangeFeedback.cs`가 된다. 다른 둘은 새 파일이다.
- P13 Factory의 표시 조립 실패 정리(192-205)는 그대로다. 새 하위 객체는 Unity 객체를 만들지 않아 정리할 것이 늘지 않는다.
- P14 C2-01 1단계의 기믹 묶음은 필드 표시의 `GimmickClears`·`GimmickReleases`가 된다. 표시의 목록은 지금 자리에서 이것을 끼운다(§3.8).
- P15 `ClearSelection` 안에서 목표 비우기와 표시 목록 비우기의 차례가 바뀐다. 둘 다 값만 비우는 줄이라 보이는 차이는 없다(§3.8).

## 10. 공유 (구현 뒤, 미전달 예정)

- 다른 작업자 줄은 DS-0에서 전체 이력으로 센 뒤 정한다(§1.7). 11-04 기록 기준으로는 호진님(기믹 줄·입력 기능 일부), 현강님(표시 파일 일부), 천명님(표시 파일 일부)이 대상일 것이다.
- 민형님: 표시·입력 Factory의 조립 줄이 바뀌므로 Factory 공유 문서에 새 절을 쓴다. 연결 계약(서비스 키·등록)은 바뀌지 않는다.

## 11. 위험

- 중간이다. 바뀌는 파일이 스물 가까이 되고, 순서가 중요한 수명 메서드를 손댄다.
  - 줄이는 방법: 순서는 표시에 남기고 단계 본문만 옮긴다(§3.8 대응표). Phase마다 Unity로 확인한다(Q-DS4 A).
- 컴파일 오류가 나기 쉬운 곳: 입력 Factory의 인자 자리, 명시 구현의 서명, `ICombatInteractionFeedback`를 받던 생성자 넷. 컴파일에서 드러난다.
- 동작 차이가 나기 쉬운 곳:
  - 정리 목록 순서
  - 해제 확인 자리
  - 공유 수명이 막는 범위(P3, 숨기기를 막으면 해제 때 버튼이 숨겨지지 않음)
  - 커서 스냅 켜짐 조건(P5)
  - 호버 범위 원점 목록(같은 스냅숏을 읽어야 함)
- ③과 겹침: 필드 표시를 ③이 다시 고친다. ③의 설계는 이 묶음이 끝난 판을 기준으로 쓴다.

## 12. 독립 검토 기록 (2026-11-07)

새 에이전트 하나가 HEAD `eafd42f5` 코드와 대조했다(읽기 전용). 높음 0·중간 6·낮음 12이고, 모두 반영했다.

| 번호 | 지적 | 반영 |
|---|---|---|
| M1 | 계약 상속을 걷는 SD-a가, 받을 객체(`Actions`·화면 표면·표시 목록 사실)를 만드는 SD-b보다 먼저라 SD-a 끝에서 컴파일이 깨짐 | §6 순서를 바꿈(객체 먼저 DS-a, 상속 걷기 DS-b) |
| M2 | 하위 객체 하나의 정리 항목이 목록 여러 곳에 흩어져, "입구 하나로 같은 자리" 정리가 안 되고 중첩 `RunAll`이면 실패 모음이 겹침 | §3.8 대응표. 하위 객체는 `Action[]` 묶음·단계 메서드를 내놓고 표시가 `Combine`, 중첩 금지 |
| M3 | 결과 끝 그리기·설치에서 하위 객체 단계가 이어져 있지 않은데 입구가 없음(배경음·HUD 사이 진입 배치 표시, 정보창 동기화 주인, Install 앞부분 누락) | DS3~DS5 입구 목록, HUD 그리기를 둘로, §1.5·§3.8에 Install 앞부분 |
| M4 | 생성자 목록에 정리·해제에 필요한 이동 초안·고스트·공격 피드백이 없음 | DS7에 이동 초안을 넣음. 고스트·공격 피드백은 공유 수명으로 정리 항목이 필요 없음(Q-DS3 A) |
| M5 | 새 해제 상태의 범위·세우는 자리가 정해지지 않음(숨기기를 막으면 안 됨, 그리기 단계에 새 확인을 넣으면 잠재 예외 동작이 바뀜, `InspectUnit` 둘째 확인이 볼 상태) | DS1(막는 것·막지 않는 것), Q-DS3 권장을 공유 수명으로 바꿈, §1.5에 셋째 UI 콜백 지점 |
| M6 | 화면 표면 하나가 너무 넓어 C2-03 실패 시나리오가 그 안에서 다시 생김 | DS4를 셋으로, 배경음 정책은 따로(Q-DS5) |
| L1 | `FieldView`의 확인은 "Build 안 됨"이고, null 목록 예외가 먼저 남 | §1.4·§5·P7에 전제(씬 하나에 연결 하나)와 차이 |
| L2 | `UnitHUDLayer`·`CombatActionPanel`·`CombatEndUI`는 우리 영역, 다른 영역은 `PackUI`뿐 | 머리 범위 줄 고침 |
| L3 | 정보창 비우기·표시 목록의 주인이 두 곳 | HUD 표면 하나, 스냅숏 쓰는 곳은 표시 하나 |
| L4 | `git mv`면 새 여덟·지움 없음 | §5 고침(새 열하나·이름 바꿈 하나) |
| L5 | 태그가 바뀌는 것은 호버 원점 하나 | §4·P8 고침 |
| L6 | "통과 49" 정의가 부정확, 주인 쪽 명시 구현 약 25줄이 남음 | §1.1·DS2 고침 |
| L7 | 감도를 해제와 상관없이 읽으려면 매퍼가 루트 참조를 비우면 안 됨 | P5 |
| L8 | C2-17·C2-15도 풀림 | 머리·§7 |
| L9 | 조정자 계약에 `Feedback`이 남음 | P1(받아들인 한계, 입력 Factory 계약 유지) |
| L10 | 폴더 안내에 `Selection/` 줄이 없음, 이름이 헷갈림 | P11, 이름 `CombatDisplaySnapshot` |
| L11 | Phase 이름이 예전 SD-A~SD-D와 겹쳐 보임 | `DS`로 바꿈 |
| L12 | 사건 Presenter는 해제 뒤 값을 저장함 | P10 고침 |

- 검토가 맞다고 확인한 것:
  - §1.1의 크기·수(876줄·필드 46·인자 38·위임 49)
  - §1.2의 쓰는 줄, §1.3의 주인 줄
  - D9 근거, Detach 순서, 해제 목록 안전성
  - 커서 스냅 조건, 호버 원점 스냅숏
  - Factory 순환 없음, 명시 구현 선례
  - 시험 참조 0
  - `RefreshInspectionSelection` 순서

## 13. 구현 메모 (2026-11-07, DS-0~DS-d)

- 결정: Q-DS1·Q-DS2·Q-DS3·Q-DS5 A, Q-DS4 B. DS-0 → DS-d를 멈추지 않고 이어 했다. 실행 기록은 [계획](../plans/2026-11-07-combat-view-scene-display-split-plan.md) §8이다. **컴파일 미확인**(MSBuild 없음), 새 시험 없음, 시험 변경 없음. Unity 확인은 계획 §6의 U-1~U-12를 한 번에 한다.
- 결과: `CombatSceneDisplay` 876 → 349줄, 필드 46 → 20, 생성 인자 38 → 19(이름 있는 인자), 입력 피드백 위임 49 → 0. 새 `.cs` 열하나, 이름 바꿈 하나(`ICombatInteractionFeedback` → `ICombatAttackRangeFeedback`). Factory는 조립 줄만 바뀌었다(민형님 공유 §52).
- 설계와 같게 간 것: DS1~DS7, P1~P15, 계획 TP-1~TP-13. §3.8 대응표 차례는 묶음 끝 검토가 HEAD와 줄마다 대조해 같다고 확인했다. 다른 곳은 P15(`ClearSelection` 안 목표 비우기와 목록 비우기의 앞뒤, 값만 비움) 하나다.
- 설계와 다르게 간 것: 계획 TP-8의 DS-a 중간 판(하위 계약을 표시가 잠시 넘기던 모양)을 만들지 않았다. Q-DS4 B로 Phase 사이 Unity 확인이 없어, 처음부터 DS-c 끝 모양으로 썼다.
- 해제 뒤 규칙(D9)이 실제로 지켜지는 자리(검토 §3 대조): 공유 수명이 고스트 7·아이템 끌기 3·공격 범위 8·호버 범위 2·궤적 3·넉백 2와 행동 버튼·`InspectUnit`·`BindItemUnequip`을 막는다. 포인터 매퍼·이동 초안·`FieldView`는 자기 해제 상태로 거른다. 공격 모드 대상은 값만 저장한다(P10). 연결 해제가 입력을 먼저 멈추므로 해제 뒤 입력 피드백 호출은 운영에서 생기지 않는다.
- 묶음 끝 검토(새 에이전트 하나, 읽기만): 높음 1·중간 0·낮음 5.
  - H1 결함: `CombatResultScreenDisplay.cs`에 `Contracts.Common` using이 빠져 `StageOutcome`을 찾지 못함(CS0103) → 한 줄 넣음. 같은 실수를 바뀐 파일 38개에서 스크립트로 다시 찾았고, 나온 후보는 모두 멤버 이름이 타입 이름과 겹친 오탐이었다.
  - L4(해제 뒤 규칙을 실제보다 넓게 적은 주석 둘)·L5(낡은 주석 셋 — 사건 Presenter 77, `CombatObjectiveDisplay` 8, `CombatBgmPolicy` 7, 그리고 CLAUDE.md·계획 문서)는 고쳤다.
  - L3(커밋 함정: 인덱스에 이름만 바뀐 옛 내용이 있고 새 파일 열하나는 추적 전)은 계획 §7대로 한 커밋에 함께 넣으면 된다.
  - **L1·L2 권장대로 반영(11-07 사용자 "L1, L2 권장대로 진행해", 컴파일 미확인):** L1 결과 화면 표면이 골격의 공개 열거 `LifeCycles.Combat.CombatCompletionDisplay`와 이름이 같아(두 네임스페이스를 함께 쓰면 CS0104) `Display/Hud/CombatResultScreenDisplay`로 바꿨다(파일·`.meta` 이름만, GUID 그대로 — 커밋 전 새 파일이라 `git mv` 아님). 이 설계 본문에서 옛 이름으로 적은 결과 화면 클래스도 새 이름으로 고쳤다. L2 `CombatMovementPlanningPresenter`의 인자 하나 생성자(바깥 호출자 없음, 다섯 인자 생성자의 `: this(field)` 사슬로만 쓰임)를 지우고 `field` 검사를 다섯 인자 생성자로 옮겼다. 그 협력자를 다시 검사하던 갈래는 원래 없어 더 지울 것은 없다.
- 점수(평가 아티팩트 10차, 같은 감점표에서 C2-03을 다 풀림으로 셈): 요청 7기준 80.4 → **81.4**, 12기준 85.0 → **85.6**, 중간 해결 19 → 20/29. 다시 평가할 때 남는 것(HUD 층 파괴가 표시에 남음, 가방 끌기 넘김, 조정자 계약의 `Feedback` 속성)을 일부로 읽으면 점수는 9차와 같다(§7의 범위 +0 ~ +1.07).
- 다른 작업자 줄: 현강님 §27·호진님 §20·천명님 특성 공유 §10·민형님 Factory §52(모두 미전달). `57af3244` 줄은 바뀌지 않았다.
