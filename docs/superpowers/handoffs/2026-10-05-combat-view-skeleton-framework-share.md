# 전투 골격·View — Framework 공용 변경 인계

- 작성일: 2026-10-05. 코드 기준 `bfe63a27`. 골격 Phase 7.2 / View F.2 공동 결과.
- 대상: Framework 및 다른 View 작업자. **전달용 문서 준비 완료, 이번 세션에서 발송하지 않음.** 담당자 합의를 받았다는 뜻이 아니다.
- 기존 [골격 공유](2026-09-30-combat-flow-framework-share.md)·[View 공유](2026-10-02-combat-view-framework-share.md)의 날짜별 기록을 현재 코드와 대조했다. 세부 설계 원본은 골격 §4.1·§6.3·§6.4와 View §10.1이다.

## 1. 실제 공용 변경과 호출자 영향

| 파일 / 계약 | 반영된 변경 | 다른 호출자에게 필요한 대응 |
|---|---|---|
| `Systems/Factory/Runtime/RunTimeSystemFactory.cs` / `FrameworkRuntimeFactory.Create` | optional battle source, execution mode, shared fault boundary 조립 | 리플렉션 호출은 실제 3개 매개변수 서명 사용. 기존 C# 선택 인자 호출과 구분 |
| `Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | OrderedCombatPresentation 주입, View의 서비스·모듈 Export, 동일 F1에 StopInput 연결 | 씬 없는 Core 조립은 Empty View 경로 사용. 필수 서비스 없는 실 View 초기화 금지 |
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | 기존 None/Start/NewRun 숫자 유지, ProcessCommand/ProcessPreview/AdvanceCombat 추가 | enum 숫자 충돌 방지. Priority로 준비 순서를 대체하지 않음 |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | `RegisterCombatEntry(CombatEntryRequest)` 추가. **2026-10-06(K21):** Combat Plan 등록을 Stage와 같은 람다로 바꿔 Buffer를 전달(`CombatPlan.Register(service, type, _buffers)`). Combat 루트의 `JobType.NewRun`(기존 값 2 재사용) = Run 초기화 → 로드아웃 1 로스터 지급 | 기존 Register(root,type)·인터페이스는 그대로. Combat Plan을 직접 부르는 코드는 NewRun에서만 Buffer 인자가 필요 |
| `LifeCycleSystemGroup.cs`·`ServiceKeys/SystemGroupsService.cs` | CombatEntry·CombatExit 노출, 기존 LifeCycle이 서비스도 Export | 새 Runtime/Runner 없이 기존 그룹 서비스로 접근 |
| `LifeCycleSystem/Base/LifeCycleManager.cs` | 현재 모듈의 선택적 `ILifeCycleTransitionGuard`를 Change/Exit 사전에 검사 | 활성 Combat은 정상 이탈 서비스를 통해 이동. 다른 모듈은 기존 동작 유지. Shutdown은 정상 F2 대기를 우회 |
| `Common/RuntimeFaultBoundary.cs`·`LifeCycleSystem/LifeCycles/Combat/CombatExecutionMode.cs` | View/골격/Bootstrap이 같은 오류 인스턴스와 실행 모드를 사용 | 오류 상태 중복 소유 금지. 기록 모드는 에디터/개발 전용 |
| `LifeCycleSystem/Base/SceneIndices.cs`·Build Settings | Boot 0, StageSelect 1, Combat 2. Lobby 3·Artifact 4는 예약 | 예약 번호를 실제 씬 제공으로 해석하지 않음. 옛 Stage.unity는 새 StageSelect가 아님 |
| `ViewSystem/Boot/GameBootstrap.cs`·CombatSceneRoot/씬 | BindScene 1회, 수명 단계 후 시작 요청. 개발 두 모드는 동일 진입 경계 사용 | 다른 씬 시스템도 기존 로딩 Enter/Exit 등록 사용. 별도 Bootstrap 생성 금지 |

위 파일의 실제 경로는 [통합 UML 소스 표](../specs/2026-10-05-combat-current-uml.md#7-코드-찾아보기)와 [골격 파일 목록](../specs/2026-09-30-combat-flow-skeleton-design.md#10-파일)을 따른다. Framework 기반 계층과 공용 JobRunner의 실행·완료·Clear 계약은 이번 전투 수명 구현에서 바꾸지 않았다.

## 2. 실행 수명 인수

- Manager의 활성 Tick은 CombatLifeCycle.OnTick으로 이어진다. 별도 모듈 Tick은 비활성 상태의 Entry 결과를 관측하며 Flow를 중복 실행하지 않는다.
- 로딩 Enter는 LifeCycle 0 → View 100, Exit는 View -100 → LifeCycle 0 순서다. View는 결과 확인 이후에만 정상 해제되고 LifeCycle은 최종 Stop·입력 Clear를 맡는다.
- Job.Execute 예외 뒤 Job은 미완료로 남을 수 있다. F1이 후속 Runtime Tick을 막으며 오류를 정상 완료로 바꾸지 않는다. 입력·Return·포커스/캡처·씬 비활성화 실패도 동일 경계를 사용한다.
- 정상 F2는 새 입력만 멈추고 일반 결과 표시를 보존한다. 실제 요청·대기·EndBattle·전환 소유자는 골격 §6.4를 따른다. Scene Exit 콜백에서 이미 시작한 로딩을 취소하려 하지 않는다.
- Combat이 글로벌 Runner를 지우거나 전체 런타임의 다른 작업을 취소하지 않는다. Runtime Shutdown은 정상 전환과 구분해 자원을 해제한다.

## 3. 검증 출처·외부 조건

구현 및 증거는 [공동 인계 §2](2026-10-05-combat-view-skeleton-session-handoff.md#2-인계-범위와-증거)와 각 계획의 최신 기록을 참조한다. `5078ee0e`는 사용자 검증 확인을 인수했고, `bfe63a27` 일반 승리 호출자는 외부 빌드/관리 회귀 기록이 있다. 이번 문서 정리에서 Unity를 재실행하지 않았다.

포기 API는 Core 대기, 패배·최종 승리 목적지는 사용자 보류다. StageSelect의 다음 노드 선택은 별도 화면 작업이다. DB source·두 재화의 운영 연결은 [Core 요청](2026-10-05-combat-core-requests-phase6b.md)으로 분리한다. 옛 Render 임시 Material/Shader 병합 건은 기존 View 공유 §6.1의 독립 항목이며 이 작업에서 해결했다고 표시하지 않는다.

## 4. 전달 기록

| 항목 | 상태 |
|---|---|
| 실제 코드·호출자 영향·F1/F2 소유권 문서 | 작성 완료 |
| 담당자에게 외부 발송 | 이번 세션에서 하지 않음 |
| 담당자 수신·새 승인/회신 | 미확인 |

사용자가 전달한 뒤 새 답변이 오면 실제 서명·영향·담당 변경만 원본 설계/계획에 반영한다.
