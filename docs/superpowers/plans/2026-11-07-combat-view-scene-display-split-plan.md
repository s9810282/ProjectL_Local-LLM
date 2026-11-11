# 전투 View 씬 표시 나누기 Task 계획 (DS-0~DS-d)

- 날짜: 2026-11-07
- 설계: [scene-display-split-design](../specs/2026-11-07-combat-view-scene-display-split-design.md)(DS1~DS7, P1~P15)
- 상태: **결정 완료, 착수.**
  - 결정(11-07 사용자 "Unity 확인은 Phase 모두 종료 후로 하고, 나머지는 권장대로"):
    - Q-DS1 A: DS1~DS7을 모두 한다.
    - Q-DS2 A: 상호작용 피드백을 셋으로 나눈다.
    - Q-DS3 A: 해제 뒤 규칙은 공유 수명 하나가 맡는다.
    - Q-DS5 A: 화면 표면을 셋으로 나눈다.
    - **Q-DS4 B: Phase마다 멈추지 않고 DS-d까지 이어서 한다. Unity 확인(§6)은 모두 끝난 뒤 한 번 한다.** 작은 묶음 셋 Q-SB4와 같은 뜻이다.
  - 설계에서 정한 P1~P15는 쓴 그대로 간다.
  - 이 계획에서 정한 TP-1~TP-13(§0)은 사용자 확인 대상이다.
- 규칙
  - View 작업이라 새 시험을 쓰지 않는다. MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
  - `Assets/Tests`(435개)는 바뀌는 형식을 쓰지 않는다. 고칠 시험이 없을 것으로 예상한다.
  - 커밋·push는 사용자가 한다.
- 기준 판: HEAD `eafd42f5`(원격 브랜치와 같음, 11-07 확인).

## 0. 계획에서 정한 것 (사용자 확인 대상)

설계와 다른 것은 † 표시다.

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-1 † | 입력 피드백 묶음 `CombatInputFeedback`의 생성자는 계약 열다섯 개를 받지 않는다. **주인 객체 일곱**(포인터 매퍼·고스트 피드백·`FieldView`·공격 피드백·사건 Presenter·이동 초안·HUD 표면)을 받는다. 어느 주인이 어느 계약을 맡는지는 묶음 안 한 곳에서 정한다 | 인자 열다섯 개 가운데 넷이 같은 객체(매퍼)다. 연결표가 Factory와 묶음 두 곳에 생기지 않게 한다. 설계 DS2는 속성 형식만 정했다 |
| TP-2 | 포인터 매퍼 생성자에 씬 루트를 넷째 인자로 받는다(필수). `IsPointerOverUi`·`TryCellCenterToScreen`·`TryWarpCursor`는 계약 구현이라 `public`으로 바꾼다. `Release`는 루트 참조를 비우지 않는다(P5) | 스냅 켜짐·감도는 실행 중 Inspector 값을 읽어야 한다 |
| TP-3 | 고스트 피드백은 포인터 매퍼와 공유 수명을 받는다. 내부 메서드는 `ICombatPlacementFeedback` 명시 구현이 된다. `MoveGhostToWorld`는 `MoveGhostToPointer` 안으로 들어간다 | 쓰는 곳이 표시 하나뿐이다 |
| TP-4 | 공격 피드백은 표시 스냅숏과 공유 수명을 받는다. 계약 넷(공격 범위·호버 범위·궤적·넉백)을 명시 구현한다. 호버 범위는 원점을 찾고 예외를 던진 뒤, 원점 칸을 넘겨 지금 함수를 부른다(검사 차례 그대로) | 원점 찾기 규칙을 옮기지 않고 자리만 바꾼다 |
| TP-5 | 이동 초안 표시 생성자에 포인터 매퍼를 다섯째 인자로 받는다. `ICombatMovementFeedback`를 명시 구현한다. 끝내기·범위 지우기도 자기 `_released`로 거른다 | P4·P9 |
| TP-6 † | 캐릭터 목록 고정 함수(`SnapshotCharacters`)를 표시 스냅숏의 정적 함수 `CombatDisplaySnapshot.Copy`로 옮긴다 | 스냅숏의 규칙이라 같은 곳에 둔다. 동작은 같다 |
| TP-7 † | 결과 화면의 `confirm` null 검사는 결과 화면 객체 `Show`가 한다. 표시는 해제 확인만 하고 넘긴다 | 검사를 두 번 하지 않는다. 차례(해제 확인 → 인자 검사)는 그대로다. P2의 "인자 검사 뒤 넘긴다"를 이렇게 읽는다 |
| TP-8 | DS-a 끝에는 표시가 아직 묶음 계약의 하위 계약을 구현하고, 옮긴 멤버는 새 객체로 넘기기만 한다. DS-b가 이것을 걷는다 | 설계 §6. Phase 사이에 Unity 확인이 없어도 Phase마다 맞는 코드로 둔다 |
| TP-9 | 필드 표시는 격자 `Mapper`를 읽기 전용으로 내놓는다. 사건 Presenter·포인터를 연결하는 일은 표시가 한다(P4). 정리·해제 묶음은 생성자에서 한 번 만든다 | §3.8 |
| TP-10 | 미리 만들 수 `CombatPrewarmPlan.From(characters, reinforcements)`는 키별 수(`Counts`·`Keys`)와 플레이어 키 목록을 돌려준다. 표시는 유닛 → 고스트 → 사건 Presenter 차례로 `Prewarm`을 부른다(지금 차례) | DS6 |
| TP-11 † | 표시의 정리 목록은 묶음 합치기 함수 둘(`Combine` 두 형태)을 함수 하나 `Join(params Action[][])`로 바꿔 만든다. 중첩 실행이 없는 것은 같다 | 묶음이 여럿(필드 셋·기믹)이라 형태 둘로는 모자란다 |
| TP-12 | 공유 수명·스냅숏·배경음 정책은 Factory가 하나씩 만들어 쓰는 객체마다 같은 것을 넘긴다. Factory는 포인터 매퍼를 이동 초안·고스트 피드백보다 먼저 만든다(P9) | |
| TP-13 | `ICombatActionDisplay` 문서의 "구현: CombatSceneDisplay"는 행동 버튼 표시로 고친다. 묶음 계약 문서는 새 모양으로 다시 쓴다 | |

## 1. DS-0 착수 전 확인 (코드 변경 없음)

| Task | 내용 |
|---|---|
| T0.1 | 원격 확인: 이 브랜치 원격 = HEAD인지. develop이 고칠 파일과 겹치는지 |
| T0.2 | 작성자 확인: 설계 §1.7 파일들. 새 에이전트 하나가 스크래치패드의 blobless 전체 이력 복제로 `git blame -w -M -C`를 돌린다. 사용자 저장소는 건드리지 않는다 |
| T0.3 | 시험 확인: `Assets/Tests`에서 바뀌는 형식을 grep |
| T0.4 | 해제 목록이 입력 피드백을 부르지 않는지, 정보창 UI 콜백 지점 셋(설계 §1.5)을 코드로 다시 확인 |
| T0.5 | 결과 기록(§8.1). 계획과 다른 사실이 있으면 멈추고 묻는다 |

## 2. DS-a 공유 수명·스냅숏·선택 표시·화면 표면 셋 (DS1·DS3·DS4)

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | 새 `Display/CombatDisplayLifetime.cs` | `IsReleased`·`MarkReleased()`. 표시 해제 첫 줄에서 세운다 |
| Ta.2 | 새 `Display/CombatDisplaySnapshot.cs` | `Characters`·`Turn`·`Phase`, `Record`·`Clear`(목록 비움·턴 null, 국면은 그대로 — 지금과 같음), 정적 `Copy`(TP-6) |
| Ta.3 | 새 `Selection/CombatSelectionDisplay.cs` | `ICombatUnitInspection` 구현. `Synchronize`·`SetObjective`·`AfterFacts`·`RenderHighlights`·`RefreshInspectionSelection`·`Clear`·`ReleaseInspection`·`InspectUnit`(설계 DS3). 목표 대상 `_objectiveTarget`을 여기로 옮긴다 |
| Ta.4 | 새 `Display/Hud/CombatHudSurfaces.cs` | `Present`·`RenderStatus`·`RenderPanels`·`ClearHud`·`ClearTurn`·`ClearUnitInfo`(Unity null 검사)·가방 끌기 `ICombatItemDragFeedback`(공유 수명으로 막음) |
| Ta.5 | 새 `Display/Hud/CombatActionButtonsDisplay.cs` | `ICombatActionDisplay` 구현. 새 연결·가용성은 공유 수명으로 막고, 연결 해제는 전달한다. `Hide`·`Unbind`는 막지 않는다 |
| Ta.6 | 새 `Display/Hud/CombatResultScreenDisplay.cs` | `Show`(인자 검사 TP-7 → 승리 효과음 → 결과 화면, 확인되면 완료 뒤 배경음)·`Hide`(Unity null 검사) |
| Ta.7 | `Display/CombatSceneDisplay.cs` | 새 객체 여섯을 받는다(생성 인자). `_isReleased` → 공유 수명. 표시 목록 사실·목표·선택 갈래·HUD·행동 버튼·결과 화면 본문을 새 객체 호출로 바꾼다. 하위 계약 멤버는 넘기기로 남긴다(TP-8). `Actions`·`Inspection` 속성을 더한다 |
| Ta.8 | `Factory/…/CombatSceneDisplayFactory.cs` | 공유 수명·스냅숏·배경음 정책·화면 표면 셋·선택 표시를 만든다. 정보창 선택 바뀜 알림을 선택 표시에 건다 |
| Ta.9 | `Display/ICombatBattleDisplay.cs`·`CombatViewSystem.cs`·`CombatViewConnection.cs` | 계약에 `Actions` 속성을 더한다. 조정자 593-594는 `display.Actions`로, 464는 `created.Inspection`으로, 연결 상태 44는 `new CombatInputSurfaces(display.Actions)`로 바꾼다 |

## 3. DS-b 입력 피드백 주인 구현·묶음 (DS2)

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `git mv` `Input/Contracts/ICombatInteractionFeedback.cs` → `ICombatAttackRangeFeedback.cs`(+ `.meta`) | 범위·영향·막힘·체력 8개만 남긴다 |
| Tb.2 | 새 `Input/Contracts/ICombatPlacementFeedback.cs`·`ICombatHoverCellFeedback.cs` | 고스트·원본 숨김 7개 / 호버 칸 2개 |
| Tb.3 | `Display/Feedback/CombatPointerMapper.cs` | `ICombatPointerOcclusion`·`ICombatCursorDevice`도 구현한다(TP-2) |
| Tb.4 | `Display/Feedback/CombatPlacementFeedback.cs` | `ICombatPlacementFeedback` 명시 구현, 공유 수명·매퍼(TP-3) |
| Tb.5 | `Display/Feedback/CombatAttackFeedback.cs` | 계약 넷 명시 구현, 스냅숏·공유 수명(TP-4) |
| Tb.6 | `Display/Feedback/CombatMovementPlanningPresenter.cs` | `ICombatMovementFeedback` 명시 구현, 매퍼(TP-5) |
| Tb.7 | `Display/Field/FieldView.cs` | 호버 칸·기믹 범위·본부 범위 명시 구현(P7) |
| Tb.8 | `Playback/CombatSceneEventPresenter.cs` | `ICombatAttackModeTargets` 명시 구현(P10) |
| Tb.9 | 새 `Display/CombatInputFeedback.cs` | 주인 일곱을 받아 계약 열다섯 속성으로 내놓는다(TP-1) |
| Tb.10 | `Input/Interaction/` 넷 | 배치 Drag(고스트·호버 칸), 공격 조준(공격 범위·호버 칸), 아이템 Drag(호버 칸), 호버 추적(호버 칸)이 자기 계약만 받는다 |
| Tb.11 | `Display/ICombatBattleDisplay.cs` | 하위 계약 상속을 걷고 `Feedback` 속성을 더한다(P1) |
| Tb.12 | `Display/CombatSceneDisplay.cs` | 위임 멤버 49개와 `ICombatGimmickRangeFeedback`·`ICombatNexusRangeFeedback` 구현을 걷는다. 묶음을 받아 `Feedback`으로 내놓는다 |
| Tb.13 | `Factory/…/CombatSceneInputFactory.cs` | `display.Feedback.X`로 넘긴다. 형식 검사 둘을 걷는다 |
| Tb.14 | `Factory/…/CombatSceneDisplayFactory.cs` | 매퍼를 먼저 만들고(TP-12), 주인 생성 인자를 맞추고, 묶음을 만든다 |

## 4. DS-c 필드 표시·미리 만들 수·생성 인자 (DS5·DS6·DS7)

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | 새 `Display/Map/CombatFieldDisplay.cs` | `Build`·`ApplyStaticHighlights`·`SyncSurface`·`RenderObjects`·`Mapper`·묶음 여섯·`ReleaseField`(DS5·TP-9) |
| Tc.2 | 새 `Display/Unit/CombatPrewarmPlan.cs` | TP-10 |
| Tc.3 | `Display/CombatSceneDisplay.cs` | 필드 협력 객체 열넷 → 필드 표시 하나. 미리 만들기는 계획을 부른다. 정리 목록 넷을 §3.8 대응표대로 `Join`으로 다시 쓴다(TP-11). 생성자는 이름 인자 약 19개 |
| Tc.4 | `Factory/…/CombatSceneDisplayFactory.cs` | 필드 표시를 만들고, 표시 생성 줄을 이름 인자로 바꾼다 |
| Tc.5 | 표시·새 파일 주석 | 머리 주석·D9 문장을 새 구조에 맞춘다 |

## 5. DS-d 문서·공유·묶음 끝 검토

| Task | 내용 |
|---|---|
| Td.1 | 묶음 끝 검토: 새 에이전트 하나가 컴파일 오류 후보·§3.8 순서 대조·D9 범위·P1~P15·TP를 HEAD와 비교한다 |
| Td.2 | 검토 반영. 결함은 고치고, 설계 밖은 사용자 결정으로 넘긴다 |
| Td.3 | 폴더 배치 안내: 새 파일 열하나의 자리, `Selection/` 1단 줄, 규칙 줄(입력 피드백은 주인이 구현) |
| Td.4 | 공유 메모(미전달): DS-0 작성자 표대로 고른 작업자 절, 민형님 Factory 공유 새 절 |
| Td.5 | 설계 §13 구현 메모, 이 계획 §8, 후속 계획 §6.6 줄, 평가 아티팩트 10차, CLAUDE.md |

## 6. Unity 확인 (사용자, 모든 Phase 뒤 한 번)

| 번호 | 확인 | 기대 |
|---|---|---|
| U-1 | 컴파일 | 오류 없음 |
| U-2 | 전투 진입(배치 국면) | 보드·선로·구조물·오브젝트·적 등장이 전과 같다. 콘솔 예외 없음 |
| U-3 | 배치 Drag | 고스트가 칸·칸 밖 포인터를 따라가고, 가능·불가 색과 호버 칸이 보인다. 필드 유닛을 끌면 원본이 숨고 놓으면 돌아온다 |
| U-4 | 아이템 Drag | 아이콘이 포인터를 따라가고 호버 칸이 보인다. 장착하면 수량이 줄고 HUD 아이콘이 생긴다 |
| U-5 | 커서 스냅 | 씬 루트 스냅 켜짐·감도를 실행 중 Inspector에서 바꿔도 바로 따른다 |
| U-6 | 유닛·오브젝트·본부 호버 | 아군·적 공격·이동 범위, 지뢰·화약통 위험 범위, 본부 증원 범위가 전과 같다 |
| U-7 | 전투 국면 공격 모드 | 범위·영향·막힌 벽·예상 체력·궤적·넉백 화살표·마무리 사격 대상 이펙트가 보인다. 모드를 끝내면 지워진다 |
| U-8 | 이동 모드 | 범위·경로·고스트(고지대 칸 위 포함)·이동 되돌리기 버튼이 전과 같다 |
| U-9 | 선택·정보창 | 유닛 칸을 누르면 정보창·HUD 선택·선택 강조가 맞고, 보스 목표 강조가 사건마다 따라간다 |
| U-10 | 행동 버튼·턴 표시·HUD 골드·가방 | 보임·누름 가능·턴 문구·골드·재고가 전과 같다 |
| U-11 | 결과 화면 | 승리 효과음, 확인 뒤 완료 배경음, 확인 뒤 떠나기가 전과 같다 |
| U-12 | 전투 다시 시작·씬 나가기 | 콘솔 예외가 없고 Hierarchy에 남는 유닛 HUD 층·고스트가 없다 |

## 7. 바뀌는 파일과 커밋 묶음 (사용자)

- 새 `.cs` 열하나. `.meta`는 Unity 기본 형식으로 함께 만든다(조정자 나누기 CS-e 선례).
  - `Display/`: `CombatDisplayLifetime`·`CombatDisplaySnapshot`·`CombatInputFeedback`
  - `Selection/`: `CombatSelectionDisplay`
  - `Display/Hud/`: `CombatHudSurfaces`·`CombatActionButtonsDisplay`·`CombatResultScreenDisplay`
  - `Display/Map/`: `CombatFieldDisplay`
  - `Display/Unit/`: `CombatPrewarmPlan`
  - `Input/Contracts/`: `ICombatPlacementFeedback`·`ICombatHoverCellFeedback`
- 이름 바꿈 하나: `git mv` `ICombatInteractionFeedback` → `ICombatAttackRangeFeedback`(+ `.meta`, GUID 그대로).
- 고친 `.cs`
  - 표시 셋: 표시·묶음 계약·`ICombatActionDisplay` 문서
  - Factory 둘
  - 주인 여섯: 매퍼·고스트·공격·이동 초안·`FieldView`·사건 Presenter
  - 입력 기능 넷
  - 조정자·연결 상태
- 한 커밋으로 넣는다. 나눠 넣으면 컴파일이 깨진다.

## 8. 실행 기록

결정(2026-11-07 사용자 "Unity 확인은 Phase 모두 종료 후로 하고, 나머지는권장대로"): Q-DS1·Q-DS2·Q-DS3·Q-DS5 A, Q-DS4 B(Unity 확인은 DS-d 뒤 한 번, §6), P1~P15·TP-1~TP-13 그대로. 작은 묶음 셋(Q-SB4)처럼 DS-0 → DS-d를 멈추지 않고 이어 했다. 모두 **컴파일 미확인**(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경 없음.

### 8.1 DS-0 착수 전 확인 (코드 변경 없음)

- T0.1 원격: 이 브랜치 원격 = HEAD `eafd42f5`. develop이 4커밋 앞서지만 고친 파일은 `StageMapSceneFactory` 하나라 이 묶음과 겹치지 않는다.
- T0.2 작성자: 새 에이전트 하나가 스크래치패드의 blobless 전체 이력 복제(`eafd42f5`)로 고칠 파일 20개에 `git blame -w -M -C`를 돌렸다(`-C -C` 교차 확인, `^` 경계 커밋 없음, 10-21 폴더 이동 따라감). 사용자 저장소는 건드리지 않았다.
  - 현강님: 표시의 HUD 층·턴 표시·결과 화면·정보창 멤버(`cc8be90e`·`01f5c787`·`fb30cbd1`·`8b61db0f`·`1013a4df`·`ad7e6b28`), 이동 초안 클래스 요약(`2fb4d2bc`), Factory 줄 셋(`CreateEnvironmentHover` `63a1c23e`, 경로 입력 `notices` `80eb7481`, 이동 초안 생성 `2fb4d2bc`).
  - 천명님: `ShowRangeCells`의 `basicCells`(`4bd6b574`), 막힌 벽 계약·위임(`66f6bb41`), 재화 `RunGold`(`d4d9fb24`), 해제 목록 줄(`98fd0c64`).
  - 호진님: 토네이도·낙석·하붑 멤버·생성 인자(`9d364b8d`·`678dbafb`·`87642ff5`), 선택 주석·되살린 줄(`88377dec`).
  - 민형님: 실질 줄 없음(using 줄만, blame이 같은 커밋의 다른 파일 줄을 맞춘 것).
  - `57af3244`(작성자 천명님, 커밋 메시지 끝에 Claude 공동 작성): 그 줄(`CombatSceneInputFactory` 93-101 턴 종료 확인창)은 이번에 바뀌지 않았다. 작성자 질문은 천명님 메모 §8·§9 그대로다.
- T0.3 시험: `Assets/Tests`에서 바뀌는 형식(`ICombatInteractionFeedback`·`CombatSceneDisplay`·`ICombatBattleDisplay`·주인 여섯의 바뀐 생성자)을 참조하는 곳 0.
- T0.4 해제 목록은 입력 피드백을 부르지 않는다. 정보창 Presenter의 `Clear`는 선택 바뀜 알림을 내지 않는다(설계 §1.5 그대로).
- T0.5 계획과 다른 사실 없음.

### 8.2 DS-a·DS-b·DS-c 코드 (컴파일 미확인)

- 새 `.cs` 열하나(+ `.meta`, Unity 기본 형식·새 GUID, 겹치는 GUID 없음)와 이름 바꿈 하나(`git mv` `ICombatInteractionFeedback` → `ICombatAttackRangeFeedback`, GUID 그대로). 목록은 §7.
- `CombatSceneDisplay` 876 → 347줄, 필드 46 → 20, 생성 인자 38 → 19(이름 있는 인자), 입력 피드백 위임 49 → 0. 남은 바깥 입구는 수명(`Install`·`Refresh`·`Clear`·`Release`)·결과 화면·아이템 해제 연결·속성 여섯(`Feedback`·`Actions`·`Inspection`·`CameraControl`·`EntryPresentation`·`EventPlayback`)이다.
- 정리·해제 목록 넷과 설치 실패 목록은 설계 §3.8 대응표 차례 그대로 `Join(params Action[][])`(TP-11)로 다시 썼다. 중첩 `RunAll` 없음.
- 계획과 다른 점
  - **TP-8 건너뜀:** DS-a 끝의 "하위 계약 넘기기" 중간 판을 만들지 않고 DS-b·DS-c의 최종 모양으로 바로 썼다. Q-DS4 B로 Phase 사이 Unity 확인이 없어 중간 판을 확인할 자리가 없었다. 최종 모양은 계획 Task 표와 같다.
  - 입력 Factory에 `display.Feedback ?? throw`를 넣었다가 늘 거짓인 null 검사라 뺐다(표시 생성자가 묶음을 필수로 받음).
- 다른 작업자 줄을 바꾼 자리는 §8.1 작성자 표대로 공유 메모에 적었다(§8.3).
- `CombatSceneDisplay`는 검토 반영 뒤 349줄이다.


### 8.3 DS-d 문서·공유·묶음 끝 검토

- Td.1 묶음 끝 검토(새 에이전트 하나, 읽기만, HEAD `eafd42f5`와 대조): 높음 1·중간 0·낮음 5. 차례는 P15 하나 말고 HEAD와 같고, D9 차이는 운영에서 닿지 않으며, 입력 기능마다 HEAD와 같은 주인에 닿는다고 확인했다. 자세한 것은 설계 §13.
- Td.2 반영
  - H1(`Display/Hud/CombatResultScreenDisplay.cs`의 `Contracts.Common` using 누락, CS0103)을 고쳤다. 바뀐 파일 38개를 스크립트로 다시 훑어 빠진 using 후보를 찾았고, 나온 것은 모두 멤버 이름과 타입 이름이 겹친 오탐이었다.
  - L4 주석 셋(표시 머리 remarks, `ICombatBattleDisplay.Feedback`, 묶음 remarks)을 실제 지키는 자리대로 고쳤다.
  - L5 낡은 주석 셋(사건 Presenter 77, `Display/Unit/CombatObjectiveDisplay.cs` 8, `Playback/Support/CombatBgmPolicy.cs` 7)과 CLAUDE.md 줄, 이 계획 §8.3 참조를 고쳤다. `CombatObjectiveDisplay.cs`는 이 묶음이 고친 파일에 더해진다(주석만).
  - L3 커밋 함정은 §7대로 한 커밋으로 넣으면 된다(인덱스에 `git mv` 때의 옛 내용이 있으므로 새 내용을 다시 넣어야 함).
  - **L1·L2 권장대로 반영(11-07 사용자 "L1, L2 권장대로 진행해", 컴파일 미확인):** L1 결과 화면 표면 옛 이름 `Display/Hud/CombatCompletionDisplay` → `CombatResultScreenDisplay`(골격 열거 `LifeCycles.Combat.CombatCompletionDisplay`와 이름이 겹쳐서, 파일·`.meta` 이름만 바꿈·GUID 그대로, 이 계획 Ta.6·§7 이름도 고침). L2 이동 초안 표시의 인자 하나 생성자를 지우고 `field` 검사를 다섯 인자 생성자로 옮김.
- Td.3 폴더 배치 안내: 머리 갱신 줄, §0 나무(`Selection/` 줄·`Display/` 역할), §1.1 줄 여섯(`Input/Contracts/`·`Display/`·`Unit/`·`Map/`·`Hud/`·새 `Selection/`), `Playback/Support/` 배경음 정책 설명, §2 규칙 줄 둘(입력 피드백 계약은 주인이 구현·묶음에 속성 하나, 해제 뒤 규칙은 공유 수명)과 미리 만들기 줄(`CombatPrewarmPlan.From`).
- Td.4 공유 메모(모두 미전달): 현강님 §27, 호진님 §20, 천명님 특성 공유 §10, 민형님 Factory 공유 §52.
- Td.5 설계 §13, 후속 계획 §6.6 줄, 평가 아티팩트 10차(요청 7기준 81.4·12기준 85.6, 중간 해결 20/29), CLAUDE.md.
- 커밋·push(11-07 사용자 "오케이 지금까지 한거 푸쉬하고"): Claude가 §7 목록 + `CombatObjectiveDisplay.cs`(주석)·`CombatResultScreenDisplay.cs`(L1 이름)를 코드·문서 한 커밋으로 넣었다.
- 남은 것: Unity 확인 U-1~U-12(§6).
