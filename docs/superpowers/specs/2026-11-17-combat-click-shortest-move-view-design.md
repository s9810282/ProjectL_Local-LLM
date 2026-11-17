# 전투 View 설계 — 드래그와 함께 쓰는 클릭 최단 경로 이동

2026-11-17 · 사용자 요청으로 이 설계를 기준으로 [Phase별 View 계획](../plans/2026-11-17-combat-click-shortest-move-view-plan.md)을 작성했다. Phase 1~4 코드 반영 및 사용자 Unity 우선 확인 3건 완료. 기준은 Core 인계 커밋 `f6b6ae1f8`과 현재 작업 트리의 View 입력이다. Core 신규 계약은 코드 인계 상태이며 컴파일·실행 검증은 남아 있다. [Core → View 인계 §2~3](../handoffs/2026-11-17-combat-click-shortest-move-view-handoff.md) · [조작 정책 V1·V2](2026-11-17-combat-click-shortest-move-core-design.md)

## 1. 플레이어가 하는 일

선택한 아군의 앵커를 누른 채 경로를 그리면 기존 드래그가 그 경로로 이동한다. 선택한 아군의 이동 범위 안 **빈 칸**에서 누르고 같은 칸에서 떼면 Core가 준 최단 경로를 미리 본다. 미리보기가 표시된 뒤 **처음 찍은 목적 칸**을 다시 눌렀다 떼면 이동 하나를 제출한다. 시간 제한을 둔 더블클릭이 아니다. 공개 효과로 예상 정지 칸이 달라져도 확인할 칸은 처음 찍은 목적지다.

예를 들어 A를 선택하고 B를 한 번 클릭한 뒤 C로 커서를 옮기면 B 미리보기가 유지된다. C를 클릭하면 C 경로로 교체된다. B의 견적을 기다리며 누르고 있던 중 응답이 와도 그 버튼을 놓는 것으로 이동하지 않는다. 다른 유닛을 누르면 선택을 바꿀 수 있고, 자리 교환이나 위험 칸 수동 우회에는 기존 드래그를 쓴다.

## 2. 현재 코드의 연결 지점과 바꿀 책임

| 현재 소유자 | 관찰한 동작 | 설계 결정 |
|---|---|---|
| [CombatSceneInputDriver.Route][driver] / [CombatSceneInput.Route][scene] | Driver가 Router보다 먼저 `InspectUnit`을 호출한다. 빈 칸이면 선택이 풀린다. SceneInput은 라우팅 전후의 전경 활성 여부로 환경 누름을 별도로 전달한다. | 선택 직전에 Path에 **이번 누름의 처리 방식**을 한 번 묻고 같은 결과를 선택·라우팅·환경에 쓴다. Driver는 동기화와 연결 재확인만 맡는다. |
| [CombatPathInput][path] | 원점 Press가 드래그를 시작한다. 이동 범위는 선택 상태와 드래그가 함께 쓰며, 공격 드래그 인계도 이 객체에 도착한다. | 기존 Path 소유자 안에서 드래그 또는 클릭 제스처 하나만 활성화한다. 클릭의 Press/Release·후보 전이는 작은 내부 객체가 맡는다. Router 대상이나 입력 Layer를 늘리지 않는다. |
| [CombatMovementRangeRequest][req] / [CombatMovementRangeTracker][tracker] | 세대·요청 번호·전투·행위자·원점·결과 순번을 대조한다. 수락 결과는 화면으로 보내고 성공한 빈 범위의 근거를 별도로 보존한다. | 사실에 QueryRevision을 포함하고 수락 결과를 범위 표시 수명 동안 보관한다. 클릭은 그 결과의 `TryGetRoute`를 읽는다. |
| [CombatMovementInteraction][move] | 드래그 초안 편집, 거절 시 접두 복구, Move 견적 검증, 놓은 뒤 자동 제출이 한 객체에 있다. | 견적 요청의 소유·성공 검증·승인된 Move 제출을 내부 협력 객체 하나로 추출해 두 입력이 공유한다. 드래그의 샘플링·접두 복구·동결 후 제출 시점은 기존 소유자에 둔다. |
| [CombatMovementPlanningPresenter][presenter] | 초안은 칸 목록으로 그리고 고스트는 포인터의 월드 좌표를 따라간다. | 클릭은 경로 강조를 재사용하고 고스트에는 예상 정지 **칸**을 전달한다. 드래그의 포인터 추적은 그대로 쓴다. |

## 3. 누름의 소유권

판정 시점은 `SelectPressed`의 선택 변경 **전**이다. 같은 포인터의 UI 차폐·필드 칸을 한 번 판정하고, 점유는 정보창 선택이 사용하는 보인 사실의 `ICombatInspectionFactsReader.TryPick`를 공유한다. 캐릭터 앵커만 검사하면 다중 칸 유닛이나 선택 가능한 구조물의 점유 칸을 빈 칸으로 오인한다. 경로 존재 여부로 점유를 추론해서도 안 된다. Core 범위는 자리 교환 종점까지 포함한다.

아래는 판정 순서다. 기존 전경이 잡은 Release, UI 차폐, 연결·대상·입력 정책을 먼저 존중한다. 클릭 후보가 있는 동안에도 **이번 누름을 처리하는 방식**과 **배경 조회를 막는 전경 소유**는 서로 다른 질문이다.

| 누른 곳과 상태 | 해당 Press와 Release 처리 |
|---|---|
| 드래그 중/놓고 승인 대기, 공격·스킬·배치·아이템 입력, UI 위 | 기존 소유자에 맡긴다. 클릭은 살아 있는 드래그를 가로채지 않는다. UI 위에서 클릭을 시작하지 않는다. |
| 선택한 행위자의 앵커 | 클릭 후보와 자기 견적을 정리하고 기존 Path Press → 샘플링 → Release로 진행한다. 범위가 대기 중이어도 드래그의 기존 견적 유예가 작동한다. |
| 다른 유닛·구조물 또는 선택 대상의 다른 점유 칸 | 클릭 후보를 취소하고 기존 Inspect와 후속 Router를 한 번씩 진행한다. 새 아군을 선택한 그 누름으로 기존 드래그가 시작될 수 있다. |
| Live·PlayerInput·Path에서 선택한 이동 가능 아군, 필드 안 빈 칸, 현재 범위 미준비 | 누름을 소비하고 선택 유지. 가용성 우선 조회, 다른 요청 때문에 MoveRange를 아직 게시하지 못한 상태, 갱신 대기를 포함한다. Release까지 무동작으로 마치고 응답 후 새 클릭을 기다린다. 이 일시적인 소비가 배경 조회를 막아서는 안 된다. |
| 같은 조건, 수락한 범위에 빈 목적지 경로 있음 | Path가 Press를 예약하고 Inspect·환경 고정·드래그 시작을 건너뛴다. §4에 따라 같은 칸 Release에서만 처리한다. |
| 범위 밖 빈 칸, 수락한 실패/빈 범위, 이동 불가가 이미 확정된 선택 | 기존 빈 칸 선택 해제 및 환경 입력 경로. 후보가 있었다면 먼저 정리한다. 실패 응답을 무기한 대기로 취급하지 않는다. |
| 선택한 이동 행위자가 없거나 클릭 자격이 없는 모드 | 기존 선택·환경 흐름. 현재 브랜치에서는 기록 전용 모드가 제거됐다(`c9b8c4d64`). |

Path가 소비한 Press는 후보가 생기지 않은 경우에도 토네이도 목적 범위 고정으로 보내지 않는다. Driver의 공통 후처리(가용성·환경·버튼 동기화)는 계속한다. 선택/정리/표시 콜백 뒤에는 연결과 세대를 다시 읽는다. 취소된 Press에 딸린 Release가 새 대상의 공격·배치·이동 확정으로 흘러가지 않도록 해당 Press 수명을 끝까지 구분한다.

## 4. 클릭 후보와 두 번째 클릭

클릭 입력은 후보의 세대, 해당 범위 사실과 불변 경로 참조를 소유한다. 목적지는 경로의 Endpoint다. 별도로 이번 Press에 **누른 칸·누를 때의 용도·대상 세대**를 기록한다. 견적 요청 번호와 승인 문맥은 공통 견적 소유자가 보관한다. 포인터 이동·카메라 이동으로 후보를 편집하지 않는다.

| 사건 | 상태 전이와 부작용 |
|---|---|
| 첫 목적지 또는 다른 합법 목적지를 눌렀다가 같은 칸에서 뗌 | Release 때 선택·범위 출처·입력 허용을 재검사한다. 이전 후보의 요청과 표시를 정리하고 새 세대의 전체 경로로 `CombatViewPreviewData.Move(actor, path)` 한 건을 게시한다. 중립 경로를 표시하며 제출은 0건이다. |
| 견적 대기 중 동일 칸 Press | 이 Press의 용도를 무동작으로 고정한다. Press와 Release 사이 응답이 와도 확정으로 승격하지 않는다. |
| 현재 후보의 유효 견적 도착 | §5 검증을 통과하면 경로와 예상 정지 위치를 표시한다. **표시 콜백이 완료되고 같은 세대가 살아 있을 때** 확정 가능으로 전이한다. 응답 처리에서 명령을 제출하지 않는다. |
| 표시 뒤 시작한 동일 목적지 Press → 동일 칸 Release | 누를 때 승인된 후보 세대·요청과 지금의 칸·세대·출처·허용 정책·슬롯·승인 문맥을 다시 대조한다. 확정 자격을 먼저 소비해 재진입을 차단하고 공통 제출 입구로 Move 1건을 보낸다. |
| 다른 칸·UI·필드 밖에서 Release, 포커스/캡처 손실, 우클릭 취소 | 이번 누름과 후보·표시·자기 견적을 정리한다. 이전 목적지로 제출하거나 다음 Release를 예약하지 않는다. |
| 현재 견적이 이동 불가 | 기존 거절 이유로 무효 표시·안내한다. 부분 경로 실행이나 드래그식 접두 복구는 적용하지 않는다. 같은 칸 재클릭은 새 첫 견적으로만 처리할 수 있다. 다른 목적지·취소도 허용한다. |
| revision·행위자·원점·턴·결과 순번 변경, 전투 교체, 모드 이탈·결과 재생·씬 해제 | 후보와 누름, 견적 자격을 함께 폐기한다. 동일 행위자의 revision 변경을 발견한 목적지 누름도 소비한다. 여전히 이동 모드이면 최신 범위만 다시 요청한다. 이전 목적지 자동 재견적·제출은 없다. |

우클릭은 기존 `Request.Cancel`을 사용한다. ESC의 현재 실제 바인딩은 공용 `OpenSettings`이며 옵션이 Settings Layer를 덮어쓴다. 이 설계에서는 옵션이 열릴 때 전투 Layer 이탈로 후보·누름을 취소하고 닫아도 복원하지 않는다. 전투 전용 ESC 중복 바인딩은 추가하지 않는다. 옵션을 열지 않고 후보만 취소하는 조작까지 원한다면 공용 입력 우선순위에 대한 별도 결정이 필요하다.

## 5. 범위·견적·명령 수명

1. **범위 수락.** 요청 시점의 전투 ID·선택 행위자·원점·결과 순번·QueryRevision과 요청 세대/번호를 기록한다. 기존 `TryConsume` 검사에 `range.SourceRevision > 0 && range.SourceRevision == current.QueryRevision`을 더한다. 오래된 응답은 표시하지 않는다. 최신 사실과 맞지 않는 대기 요청은 번호로 회수해 배경 범위를 갱신할 수 있게 한다. Facts 조립 중 빈 범위 근거를 조회할 때는 기존처럼 Buffer의 revision을 직접 읽어 재귀 호출을 피한다.
2. **수락 결과 수명.** Tracker가 표시 중인 범위 결과와 그 사실을 소유한다. 클릭 조회는 현재 사실에 맞는 수락 결과에서 경로 참조만 꺼낸다. 별도 목적지 사전·BFS·경로 복사는 필요 없다. 실패/성공 빈 범위/미응답을 구분한다. 표시 해제와 턴 종료 판단용 성공 빈 범위 근거의 기존 수명 차이를 보존한다. Core의 `MovementRangeResult.Success`가 각 경로를 검증하고 그 끝점에서 목적지를 생성한다. View는 해당 칸의 `TryGetRoute` 결과로 클릭을 처리한다.
3. **단일 슬롯.** 배경 범위는 기존 가용성 우선순위와 `CombatPreviewSlotRule`을 따른다. 수락된 범위는 슬롯을 놓은 뒤에도 같은 사실이면 재사용한다. 클릭 견적은 드래그와 같은 전경 Move로 전환한다. 대기·표시 중 단위 호버, 가용성, HUD·포효 등의 배경 쓰기는 기존 전경 소유 조건으로 양보시킨다. `IsInputOwnerActive`는 커서 스냅·Undo·선택에도 쓰이므로 클릭 후보를 드래그 중으로 일괄 취급하지 않는다. 배경 조회 억제에는 후보를 포함하고, 다른 유닛 선택은 §3, 커서 스냅은 기존 드래그 정책을 따른다. 예상과 달리 슬롯을 잃으면 후보를 버린다. 늦은 응답·취소는 자기 번호의 `ClearPreview(number)`만 사용한다.
4. **공통 견적과 제출.** 드래그가 현재 수행하는 검사를 한 곳에 둔다. 요청 번호·수명·요청 버전, 슬롯 종류·행위자·전체 경로, 성공 응답의 `ValidStepCount`, `PathMovement.RequestedPath`, `MovementContext`의 기대 사실·전투 ID·행위자·관찰자·Move 종류·전체 경로를 검증한다. 클릭의 요청 버전은 후보 세대, 드래그는 초안 revision을 쓴다. 클릭은 추가로 범위 출처가 현재 QueryRevision과 맞아야 한다. 폐기된 응답으로 현재 후보를 정리하지 않는다. 최신 응답의 계약 불일치는 제출 불가다.
5. **기존 명령 경계.** `CombatMovementInteraction.Confirm`은 드래그에서만 동결 후 응답 자동 제출을 허용한다. 클릭은 표시 뒤 확인한 Release에서만 승인된 `actor, path, context`를 공통 제출 함수에 넘긴다. 기존 `CombatViewCommandData.Move`, 쓰기 sink의 Submit, 제출 번호·미처리 명령·연출 차단 및 `CanEditPath`를 유지한다. 클릭 취소가 이미 제출한 명령을 해제하지 않는다. 튜토리얼 허용표와 차단 안내를 우회하지 않는다.

## 6. 화면 표현과 정리

첫 클릭 직후에는 Core의 전체 요청 경로를 중립으로 그린다. 성공 견적이 오면 기존 경로 층은 요청 경로를, 고스트는 `MovePreview.PathMovement.FinalStop`을 나타낸다. 강제 이동·중단으로 두 칸이 다를 수 있으므로 목적지 표시는 처음 선택한 칸에 유지한다. 고스트는 기존 Presenter에서 `FieldGridMapper.CellSurfaceToWorld`로 배치해 타일 높이를 반영한다. 드래그용 `UpdateMovementPointer(..., frozen)`는 마지막 포인터 월드 자세를 유지하므로 클릭 정지 칸 표시에는 칸을 받는 좁은 표시 호출을 둔다.

클릭 후보를 대체하거나 취소하면 자기 경로·끝점·고스트를 지우고 원본 유닛의 투명도를 복구한다. 이미 붙어 있는 표시 수명과 정리 경계를 재사용한다. 첫 클릭에 유닛 Transform·행동 사용·결과 재생은 바뀌지 않는다. 명령 이후의 이동·진입 효과·중단·Undo는 기존 결과 표시가 맡는다.

## 7. 구조 리뷰

| 기준 | 적용과 변경 범위 |
|---|---|
| SSOT, P6/P7 | 합법 경로는 Core의 수락 범위, 실제 제출 자격은 승인 견적, 선택은 기존 선택 상태, 보드 위치 변환은 필드 Mapper가 원본이다. View 로컬 상태는 어떤 누름을 확인했는지 표현한다. |
| S와 O | 클릭의 재클릭 정책만 바뀌면 클릭 협력 객체와 해당 입력 인수 사례를 본다. 드래그 초안에 클릭 단계를 넣으면 샘플링·접두 복구까지 검증해야 하므로 분리한다. |
| L과 I | 드래그 호출자는 응답 전 Release 후 제출·점유 거절 복구·취소 동작을 유지한다. 선택은 누름 소유 결과만, Presenter는 경로·정지 칸만 받는다. 표시가 클릭 후보나 요청 번호를 해석하지 않는다. |
| D와 조립 | 기존 `CombatSceneInputFactory`가 씬의 입력·표시·선택 의존성을 연결한다. 클릭 상태와 견적 소유자는 Path 내부 수명의 일반 C# 객체로 둔다. 외부 의존성 교체는 Factory와 해당 좁은 계약에서 다룬다. |
| JMH/FW | 기존 서비스·모듈과 Builder 조립(FW-001~003), Buffer sink 소유권(FW-004), 기존 입력 수집·Submit·슬롯(FW-005~007), 기존 표시 자원 해제와 연결 수명(FW-011/016)을 따른다. 공용 Framework·Core·FieldCamera·씬·SO 변경 요구 없음. |
| 공유 계약의 실제 회귀 | 공통 견적 검증을 바꾸면 드래그와 클릭 **둘 다** 확인한다. Press 분배를 바꾸면 정보 선택·환경 고정·공격 드래그 인계를 함께 본다. 정지 칸의 시각 표현 변경은 Presenter와 지형별 고스트 확인에 한정한다. |

## 8. 사용자 Unity 인수 조건

| 장면 | 확인할 관찰값 |
|---|---|
| 직선·장애물 우회·위험 칸 경로 | 첫 클릭 후 요청 경로와 견적의 정지 칸 표시. 포인터·카메라 이동 후에도 유지. 명령 증가 0, 유닛 위치·행동 사용 그대로. 강제 이동 견적에서는 원래 클릭한 칸에서만 확정. |
| 응답 지연·연타 | 범위 응답 전 빈 칸 클릭은 선택 유지·후보 0. 견적 전 같은 칸 재클릭 및 Press와 Release 사이 응답은 제출 0. 표시 후 새 같은 칸 클릭만 Move 1건. 반복 Release·연타로 추가 제출 없음. |
| 후보 교체·출처 변경 | A→B→A 교체 후 오래된 A 응답 무시. 범위 수락 뒤/견적 뒤/확정 Press 뒤 각각 revision 변경 시 Release로 제출 0. 범위 갱신 후 새 첫 클릭 필요. |
| 선택·환경·슬롯 경쟁 | 범위 안 토네이도 빈 칸은 이동만 처리. 범위 대기 중에는 이동·환경 고정 모두 0. 범위 밖/확정 빈 범위/실패 시 기존 선택 해제. 다른 유닛·다중 칸 점유·구조물 선택 가능. 호버·HUD 조회 때문에 승인 견적이 교체되지 않음. |
| 드래그 회귀 | 원점 누르기만 하면 이동 0. 경로 그리기·되짚기·자리 교환·도약·놓은 뒤 늦은 승인·UI 밖 취소·공격 드래그 인계가 기존과 같음. 클릭 후보에서 원점 드래그로 바꿔도 드래그 명령만 1건. |
| 취소·모드 전환 | 우클릭·ESC 설정 열고 닫기·포커스/캡처 상실·UI 위/필드 밖 Release·행위자 교체·턴 종료·결과 재생·씬 이탈 후 잔여 고스트/흐림/재제출 없음. 남의 슬롯 보존. |
| 제약·실행 후 | 튜토리얼 제한·모달·미처리 명령 중 추가 제출 없음. Live에서 유효한 견적·문맥이 없으면 클릭 Move 기록 0. 이동 결과와 가능한 Undo가 드래그와 같은 경로로 반영됨. |

누름 소유권, 견적·제출 공유 범위, 예상 정지 칸 표시의 리뷰 지점은 Phase별 View 계획에 있다. **컴파일 미확인.**

[driver]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatSceneInputDriver.cs
[scene]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatSceneInput.cs
[path]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs
[req]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementRangeRequest.cs
[tracker]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementRangeTracker.cs
[move]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs
[presenter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Feedback/CombatMovementPlanningPresenter.cs
