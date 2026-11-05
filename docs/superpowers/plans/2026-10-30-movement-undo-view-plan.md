# 이동 되돌리기 View 구현 계획

- 2026-10-31 / UV1·UV2 코드 연결 완료. UV3 코드 검토·시험 소스 준비 및 사용자 씬 연결·되돌리기 정상 동작 확인 완료. 자동 시험·세부 경계 사례는 별도 미확인.
- 원본: [설계](../specs/2026-10-30-movement-undo-view-design.md). 사용자가 10-31 UV1부터 UV3까지 연속 진행을 지시했고, 이어 씬 편집·에디터 테스트는 제외하도록 지정했다.
- 이번 작업은 사용자 지시로 Phase 연속 진행. 커밋·push는 사용자가 한다. 계획 밖 결정은 먼저 확인한다.

## Phase UV1 — 가용성 최신성 계약

| Task | 변경/검증 |
|---|---|
| UV1.1 | CombatInputFacts에 QueryRevision 전달, FactsReader에서 현재 Buffer 값 읽기. 조립용/시험용 Facts 전달 지점 확인 |
| UV1.2 | Tracker 키와 응답 수락/Read에 revision 검사. 기존 공유 슬롯 선점·Ready 유지·Clear/Stop 계약 보존 |
| UV1.3 | 의미 있는 시험: 결과 순번 그대로 revision만 변경, revision 0/오래된 답, 선택 A→B→A 늦은 답, 전경 선점 중 Pending 정리/Ready 유지, Stop 후 재요청 없음 |
| UV1.4 | Unity 컴파일 및 영향받는 기존 행동 가용성·이동 시작·공격 진입·공유 preview 슬롯 시험. 새 API를 쓴 가짜 답도 실제 SourceRevision으로 구성 |

종료: 최신성 검사 통과, 기존 이동/공격이 조회 중/갱신 뒤 정상 진입. UI는 아직 활성화하지 않는다. 결과를 보고하고 멈춘다.

## Phase UV2 — 의도 제출과 기존 버튼·키 연결

| Task | 변경/검증 |
|---|---|
| UV2.1 | CombatMovementUndoInput과 meta 생성. 현재 Facts/동일 가능 여부 함수/Tracker 답/Gate.SubmitPending 연결. 자체 token/pending 저장 없음 |
| UV2.2 | InputFactory에서 조립, 연결 입력 묶음에 보관, Router.Return 전달. StageOf에 UndoMove 추가. 기존 blocked tutorial 입력 보고 재사용 |
| UV2.3 | InputSurfaces·ActionButtons에 같은 CanRequest 투영. Panel ReturnSlot 처리·Visible/interactable·returnButton 검사 복구. Controller의 기존 Return Press 등록 |
| UV2.4 | 옛 무동작 Path.RequestReturn 내부 위임/주석의 실제 호출자 확인 후 필요한 정리만 수행. 입력 에셋/저장 키/슬롯은 유지 |
| UV2.5 | Ready만 정확한 actor/battle/turn/move token 제출, Unknown/Pending/stale/전경/미처리/재생/적 턴/모달/정지/MoveOnly/기록 모드 차단. 버튼+R 같은 프레임 반복에도 한 명령. 수락·거절 결과 후 일치 번호만 pending 해제 |
| UV2.6 | UI는 EventSystem handler dispatch로 slot 2 확인(onClick 직접 호출 금지). 단축키는 기존 InputBindings 경로. Bind 해제 뒤 콜백 없음, 공격·턴 종료 버튼 동작 유지 |

종료: 컴파일·기능/입력 경계 시험 통과. 계획 밖 새 UI나 Core 변경 없음. 보고하고 멈춘다.

## Phase UV3 — 실제 결과 재생·Unity 인수

| Task | 변경/검증 |
|---|---|
| UV3.1 | 실제 Core 공개 조회→UndoMove→ordered Returned 경로의 통합 시험. Final 사실 위치/방향/행동/거리 1회 적용, 결과 확인 뒤 재이동 가능. metadata 별도 적용 없음 |
| UV3.2 | A/B 교차 이동·선택 변경, 원래 칸 막힘→해제 뒤 재조회, 거절·소모 token·새 턴/전투, 이동 효과 발생·공격 뒤 불가. Core 알고리즘 재시험 대신 View의 응답/제출/표시 경계 확인 |
| UV3.3 | 정지·Detach·Exit·재진입 시 자기 요청/콜백 정리, 이미 제출한 명령/결과 수명 유지. Returned 취소/해제·회전/자리 대여 잔존 없음 |
| UV3.4 | Unity 재생: 기존 Undo 버튼/R 및 재바인딩, 미선택/조회 대기/공격 모드/경로 초안/튜토리얼·확인창, 빠른 중복 누름, 되돌린 뒤 이동·공격·턴 종료, 배치 우클릭 회수 유지 |
| UV3.5 | 입력 안내와 현재 설계/계획에 실제 실행 환경·결과·미확인 항목 기록. 필요 시 Core 공유 문서에 소비 계약 확인만 기록, 외부 발송 없음 |

종료: 실제 Unity 컴파일/EditMode 및 수동 인수 결과를 구분해 보고. 실패하면 원인과 잔여를 적고 완료로 기록하지 않는다.

## 검증 범위

- 기존 테스트 파일명은 구현 착수 시 현재 트리에서 확인한다. 없는 시험명이나 테스트 실행을 완료로 기록하지 않는다.
- 공유 Tracker 변경이므로 이동/공격 조회 회귀는 필수. FieldCamera/Architecture 등 무관 기능의 광범위 재검증은 추가하지 않는다.
- Unity 생성 csproj가 삭제 파일을 참조하면 프로젝트 파일을 재생성해 확인한다. 이전 병합 때 CS2001로 중단한 MSBuild는 성공 증거가 아니다.

## 작성 기록

2026-10-30: 현재 입력/버튼/조회/정책/Gate/Returned 재생 및 Core 인계 확인. 설계·계획만 작성. 별도 선행 오류 수정: TraitPreviewInterceptionBuilder의 제거된 ActionCostSettlementSnapshot/ActionFundingKind/costFree를 현 UnitActionUseSettlementSnapshot/reaction 계약으로 맞춤. 해당 수정은 아직 Unity 컴파일 미확인.
### 2026-10-31 UV1~UV3 연결 기록

- 사용자 지시: UV1부터 연속 진행. 후속으로 "씬은 내가 만질테니까, 에디터 테스트 말고 진행해". 이후 UI 조작/Unity 테스트 실행을 중단하고 코드·정적 컴파일·문서로 범위를 한정했다.
- UV1: Facts/Reader에 QueryRevision 전달. Tracker 키/Read/응답 수락에 양수 revision·SourceRevision 일치 검사. 선택/요청/preview 슬롯/Ready 수명은 유지.
- UV2: 새 CombatMovementUndoInput과 meta. InputFactory 생성→SceneInput 보관→Router.Return 전달, 같은 CanRequest를 InputSurfaces/ActionButtons로 투영. StageOf와 PendingInputCommand 허용 종류에 UndoMove 추가. 기존 Gate 대기·Resolve·Tracker.Stop을 사용하며 별도 상태 저장 없음.
- UI/키: Controller에서 기존 Return Press 소비자 복구, Panel ReturnSlot 2의 기존 UIInputBinding 콜백 복구와 Visible/interactable 적용·필수 returnButton 검사. 씬/프리팹/입력 액션 에셋은 수정하지 않았다. 기존 호출이 없던 Interaction/PathInput 무동작 RequestReturn 내부 경로 제거. 공개 수신 슬롯은 유지.
- UV3.1~3: 실제 Core fixture를 사용하는 MovementUndoViewTests 5건 소스 추가(메타 포함). revision만 바뀐 응답 무효화, 0/unbound 응답, 선택 왕복/늦은 답, Stop 뒤 명령 보존, 전경/모달/중복 제출, 실제 Undo→ordered Returned 한 번 전달→결과 확인→재이동을 다룬다. 시험 실행 결과가 아니라 준비한 소스의 범위다. 실제 FieldUnitView의 자리/회전 대여 해제는 이 spy 시험으로 검증하지 않았다.
- 재생 코드 정적 검토: 기존 EventTypeTable의 필드→필드 Returned → OrderedCombatPresentation → CombatMovementPlayback → CombatMovementPresentation.ShowReturn 경로 재사용. Final 사실만 적용하며 UndoMovement 메타데이터를 별도로 반영하는 코드는 추가하지 않았다. Returned는 역방향 걸음/새 연출 자원을 만들지 않는다.
- 실행 제한: 최초 Unity 배치 실행은 프로젝트가 다른 Unity 인스턴스에 열려 있어 시작 전 중단. 테스트 XML 없음, 통과 건수 없음(Temp/undo-view-tests.log). 이후 사용자의 실행 제외 지시를 따랐다. 실제 EditMode/PlayMode/수동 인수는 미확인이다.
- 정적 검증: 최초 MSBuild는 재생성 전 csproj의 StageMapCameraFramingPending 누락으로 실패. 재생성된 csproj에서 운영 Systems 컴파일 통과. 시험 소스의 SoulCountChange using 누락을 고쳤다. 최종 컴파일 결과는 아래 후속 기록을 따른다.
- 구조 검토: Core/Buffer/Framework 구현 변경·서비스 등록 없음. 조회 원본 Tracker, pending 원본 Gate, 선택 원본 SelectionState, 재생 원본 기존 ordered 사건. 되돌리기 조건 변경은 UndoInput, 조립 변경은 InputFactory, UI 표시 변경은 Panel에 모인다. 공유 revision 계약 변경은 이동/공격 조회의 사용자 회귀가 필요하다.

### 사용자 Unity 확인 항목(최초 미실행 목록, 후속 확인은 아래 기록)

1. CombatActionPanel.returnButton에 기존 Undo Button 참조 확인, Button.onClick에 별도 실행 이벤트를 달지 않는다. 기존 슬롯 2는 코드가 연결한다.
2. 안전한 이동 후 선택 유지 상태에서 Undo 버튼/R(재바인딩 포함)→원래 칸/방향→다시 이동. 빠른 버튼+R 중복 요청이 하나만 제출되는지 확인.
3. 미선택/조회 대기/공격 조준/경로 작성/모달/튜토리얼/적 턴/종료·정지에서 비활성화. 배치 우클릭 회수·공격·턴 종료 유지.
4. 원래 칸 점유→불가→점유 해제 후 가능, A/B 교차 선택/이동, 공격/이동 효과 발생 뒤 불가. 불가 응답이 체크포인트 삭제를 뜻한다고 추정하지 않는다.
5. 결과 중 Detach/Exit/재진입, 회전/자리 대여·콜백 잔존, 위치·누적 이동 거리·행동 표시와 최종 Buffer 일치 확인.
6. 추가한 MovementUndoViewTests 및 기존 MovementUndoPublicationTests는 컴파일 확인과 별도로 실제 Test Runner 실행이 필요하다. EventSystem 버튼 dispatch/키 수신/실제 모델 재생·해제의 실행 증거도 아직 없다.
### 최종 정적 컴파일 (2026-10-31)

- VS 2022 MSBuild, 재생성된 ProjectL.Tests.CoreIntegration.csproj /t:Build /p:Configuration=Debug: 종료 코드 0. ProjectL.Systems와 ProjectL.Tests.CoreIntegration(추가 시험 5건 포함) 컴파일 성공. 기존 UI 직렬화 내부 필드 CS0649 경고가 있으며 되돌리기 변경 파일의 오류는 없다.
- 최종 로그: Temp/undo-view-build-final.log(로컬 산출물). git diff --check의 코드·문서 공백/충돌 검사 통과. 실제 테스트 실행·씬 인수 완료를 의미하지 않는다.

### 사용자 씬 연결·기능 확인 완료 (2026-10-31)

- 사용자 보고: "씬 연결했고 되돌리기 정상 작동해." 씬 연결 및 이동 되돌리기 기본 동작을 사용자 확인 완료로 반영한다(UV3.4 기본 기능 인수).
- 증거 구분: Codex가 직접 재생하거나 Test Runner를 실행한 결과가 아니다. 버튼/R 각각·재바인딩·중복 입력·A/B 교차·점유/효과 차단·Detach/Exit 등 개별 사례의 수행 여부까지 확대해 기록하지 않는다.
- UV3.5 문서 갱신 완료. 자동 시험 실행과 위 세부 경계 사례의 확인 상태는 별도 미확인으로 유지한다. 이전 실행 대기·미실행 문구는 이 확인 전의 이력이다.
