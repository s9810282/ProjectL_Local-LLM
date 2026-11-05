# 배치·전투 사건의 아침·밤 스텐실 전환

2026-10-18. 사용자 지시: 우선 기존 사건 재생 경로에 연결하고, 배치→전투는 밤, 전투→배치는 아침으로 바꾼다. 전환용 버추얼 카메라는 사용자가 맵 프리팹에 이미 배치했다고 확인했다. Phase C1 코드·관리 검증 완료, Unity 화면 검증 대기. 씬에 새 카메라를 추가하지 않았다.

## 설계

- 기존 CombatSceneEventPlayback.BeginEvent에서 CombatTurnEvent의 CombatEntered·NightEnded를 전용 CombatPhaseTransitionPresenter에 위임한다. 입력 Layer나 FinalizeResult에 재생 책임을 넣지 않는다. 사건은 단계 전환의 원본이며 View에 이전 StagePhase 사본을 두지 않는다.
- Presenter는 ScreenTransition 서비스의 Scop을 대여하고 Struct를 전달한다. 기존 사건 Presenter에도 사건을 전달해 BGM과 원래 사건 처리를 보존한다. 두 처리가 모두 완료되면 Scop을 Dispose한 뒤 사건 completed를 한 번 호출한다. 전환 진행 Tick은 RenderSystem만 담당하고 Presenter는 완료 여부만 확인한다.
- 밤: Night Camera / BefalRendering / magenta. 아침: Day Camera / AfterRendering / cyan. 우선 기존 수동 테스트와 같은 속도 12·목표 반경 100을 C# 설정 모듈 한 곳에 정의한다. 물리 카메라 이름·고정 레이어·원 중심·경계 폭은 기존 ScreenTransitionInitModule을 따른다.
- Factory가 설정·Presenter를 조립하고 CombatDisplayServices.From이 실제 전환 서비스를 제공한다. 기존 단위 테스트의 서비스 묶음에서는 전환 서비스를 생략할 수 있으며 그 조립에서는 기존 사건 처리만 실행한다. 운영 From 경로는 반드시 실제 서비스를 전달한다.
- 재생 중 다음 사건 시작과 결과 완료를 차단한다. 취소·Install 전 재연결·Clear·Release·실패에서 Scop을 반환하며 완료 통지를 보내지 않는다. 늦게 온 이전 사건 콜백은 세대로 차단한다. 성공 후 반환은 최종 Main 마스크와 버추얼 Priority를 유지한다.
- 생성된 맵 프리팹의 Day Camera·Night Camera가 기존 AfterLoad 카탈로그 수집에 포함되어야 한다. 이름은 유일해야 하며 프리팹/운영 씬 저작값은 이번 코드 연결에서 변경하지 않는다.

## Task — Phase C1 (코드 완료)

1. 전용 Presenter와 C# 설정 모듈·meta를 추가한다.
2. 서비스 묶음·Factory·사건 재생 조정자와 Display 정리 경로에 연결한다.
3. 관리 테스트로 밤/아침 선택, 사건+전환 완료 대기, 1회 통지·반환, 취소·실패·늦은 콜백을 확인한다. 관련 기존 전환 회귀와 빌드를 확인한다.
4. 사용법·공유 기록을 갱신하고 Phase 완료 보고한다. 실제 Unity 화면과 사용자 프리팹의 카메라 배치 확인은 별도로 구분한다.

## 점검

SSOT: 단계 전환은 Core 결과 사건, 요청값은 설정 모듈, 진행량·최종 마스크는 ScreenTransition Scop이 소유한다. SRP/ISP/DIP: 전투 사건 조정자는 위임, Presenter는 전환 자원/완료 합류, 실제 출력 기능은 기존 Render 서비스로 분리한다. 기존 사건 순서·완료·취소와 Scop 반환 계약(FW-001/002/011/014)을 유지하고 새로운 전역 서비스는 추가하지 않는다. 카메라 설정과 조립은 각각 모듈·Factory에 모아 관련 없는 이동·공격·입력 기능의 구현 변경을 피한다. Core·FieldCamera·Architecture·씬·프리팹은 수정하지 않는다.

## 실행 기록

- CombatPhaseTransitionPresenter와 CombatPhaseTransitionSettings·meta를 추가했다. 기존 사건 처리와 전환 완료를 기다리고 성공 시 반환 후 1회 완료 통지한다. 실패·취소는 통지 없이 반환하며 세대로 오래된 콜백을 차단한다. 화면 전환 진행량을 별도 Tick하지 않는다.
- CombatDisplayServices.From에서 실제 ScreenTransition 서비스를 선택하고 CombatSceneDisplayFactory가 Presenter를 생성·주입한다. 테스트의 기존 직접 서비스 묶음에서는 생략할 수 있어 관련 없는 장면 테스트가 실제 카메라·스텐실을 요구하지 않는다.
- CombatSceneEventPlayback은 단계 사건을 위임하며 재생 중 다음 사건과 결과 완료를 막는다. Display의 Install/Clear/Release 정리 목록을 CancelPendingEvents로 연결해 전환 자원도 회수한다. 기존 기차·사건·이동 기능의 구현은 변경하지 않았다.
- 사용자 답변 이후 저장된 Assets/ProjectL/Prefabs/Stage/MapProp/Stage.prefab에서 활성 Night Camera(layer 31)·Day Camera(layer 30)를 확인했다. StageViewProp_Normal_A.asset도 해당 프리팹 GUID 1ce6aae51690ec44b870495ef65251c0을 참조한다. 이 씬/에셋은 사용자가 편집한 상태를 유지했다.
- MSBuild ProjectL.Tests.Framework·Assembly-CSharp 성공, 변경 소스 새 경고 없음. 로그: Logs/combat-phase-transition-build.log·Logs/combat-phase-transition-render-build.log.
- Unity Mono에서 새 Presenter 관리 테스트 13개와 기존 ScreenTransition 초기화/출력/Scop/Tick/Render 경계 43개, 총 56/56 통과(Logs/combat-phase-transition-tests.log). 밤/아침 선택·기존 사건 보존·양쪽 완료 대기·반환 후 1회 통지·취소·실패·재진입·늦은 콜백을 확인했다.
- CombatSceneEventPresenterTests에 실제 Factory/Display/OrderedCombatPresentation 경로의 전환 완료 대기와 Clear/재설치/Release 반환 테스트 2개를 추가했다. 작성·컴파일 확인 상태이며 Unity 네이티브 실행은 대기 중이다. 열린 에디터를 유지했고 별도 Unity 인스턴스를 실행하지 않았다.
- 실제 스텐실 화면·카메라 구도·최종 마스크는 Unity Play에서 확인해야 한다. Core·FieldCamera·Architecture·씬·프리팹·기존 공개 서비스 키는 이번 연결에서 변경하지 않았다. C1 리뷰 지점에서 보고한다.
