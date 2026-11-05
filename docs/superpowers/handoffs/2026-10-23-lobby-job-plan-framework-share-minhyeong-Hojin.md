# 로비 실행 계층 Job·Plan 재구성 — 민형님 공유 (2026-10-23)

- 작성자: 호진(이호진)
- **받는 사람:** 민형님(Framework)
- **보내는 쪽:** View 작업(로비 담당)
- **상태:** 작성 완료·미전달. 담당자 수신·회신은 미확인.
- **코드 기준:** Phase 0~1 `e46398d4`, Phase 2~4는 이 문서를 쓸 때 커밋 전. 커밋은 사용자가 한다.
- **원본:** [로비 설계](../specs/2026-10-11-lobby-design-Hojin.md)의 Job·Plan 재구성 절(옛 재구성 설계), [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)(옛 Task·실행 기록). 둘은 2026-10-27에 옛 재구성 설계·계획을 합친 문서이며 § 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`).

## 1. 변경 결과

로비를 전투·튜토리얼과 같은 흐름으로 바꿨다. `LobbyLifeCycle`은 트리거만 한다 — 오류가 없고 Runner가 비면 `Register(Lobby, Next)`를 한 번 건다. `LobbyPlan`이 그 자리에서 사실을 읽어(`LobbyProgress.Read`) `Decide`로 다음 단계를 고르고, 단계 Job 목록을 바로 돌려준다(전투의 `CombatNextJob` 같은 실행 순간 판단 Job은 두지 않았다). Core 호출과 그 호출 직후 확인은 Job이 한다. 옛 `LobbyFlow`·`LobbyDepartureFlow`·`ArtifactEditFlow`는 지웠다.

| 단계 | 등록하는 Job(같은 우선순위라 등록 순서대로 같은 Runner Tick에 이어 실행) |
|---|---|
| 아티팩트 편집 N | `ArtifactCommandJob(N)` → `LobbyArtifactCheckJob(편집, N)` |
| 아티팩트 프리뷰 N | `ArtifactPreviewJob(N)` → `LobbyArtifactCheckJob(프리뷰, N)` |
| 출발 | `LobbyRunStartJob` → `LobbyTransitionJob`(성공이면 `Change(Stage)`) |
| 할 일 없음 | 공유 빈 목록(매 프레임 할당 없음) |

## 2. 공용 파일 변경

경로는 `Assets/ProjectL/Script/Systems/` 기준이다.

| 파일 | 변경과 소비자 영향 |
|---|---|
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | ① 로비 줄이 `_lifeCycles`도 넘긴다(`LobbyPlan.Register(service, jobType, _buffers, _lifeCycles)`). 대리자는 지금처럼 `Initialize`에서 한 번 만든다. Runner는 넘기지 않는다. ② Artifact Plan `using`이 `…LifeCycles.Lobby.Artifact.Plans`로 바뀜(폴더 이동). ③ `_lifeCycles` 필드 주석에 로비를 더함. 공개 등록 메서드 서명·다른 루트 줄은 그대로 |
| `ServiceKeys/Runtime/LifeCycle/LifeCycleSystemServiceKey.cs` | 키 둘 추가: `LobbyProgress`(`ILobbyProgressReader`), `LobbyArtifactChecks`(`ILobbyArtifactChecks`) |
| `RuntimeSystem/LifeCycleSystem/LifeCycleSystemGroup.cs` | `ILifeCycleSystemGroupService`에 속성 둘(`LobbyProgress`·`LobbyArtifactChecks`)과 생성자에서 꺼내기. **이 인터페이스를 구현한 시험 대역은 속성 둘을 더해야 컴파일된다** — 이번에 `BootStartButtonTests.Group`·`TestLifeCycleGroup`을 `null`로 맞췄다 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | `CreateComposition`에서 `LobbyArtifactDomain`(기록 누락 알림 = `Debug.LogError`)과 `LobbyProgress`를 만들어 각각 `AddService`·`AddModule`(바인딩 참여). `LobbyLifeCycle` 생성자에 오류 경계·사실 읽기·도메인을 필수로 넘긴다(대체 생성 없음) |

로비 루트가 받는 `JobType`은 `None`·`Start`·`Next`다. 옛 `NewRun`은 거절한다(전투 R3와 같음). `Start`의 `LobbyJob`은 Framework 표본이라 그대로 두었고 `JobSystemTests`는 바뀌지 않는다. Artifact 루트(`None`·`Start`·`ProcessCommand`·`ProcessPreview`)도 그대로다. **(2026-10-29 바뀜: `LobbyJob`·`ArtifactJob`·Artifact 루트를 걷었다 — [씬 Factory 공유](2026-10-28-stage-map-scene-factory-share-minhyeong-Hojin.md) R-f 절이 대신함.)**

## 3. 옮긴 파일 (GUID 유지, 네임스페이스 = 폴더)

- `LifeCycles/Artifact/Plans/ArtifactPlan.cs`, `LifeCycles/Artifact/Jobs/ArtifactCommandJob.cs`·`ArtifactPreviewJob.cs`·`ArtifactJob.cs` → `LifeCycles/Lobby/Artifact/Plans`·`Jobs`. 아티팩트 편집은 로비에서만 하기 때문이다. `ArtifactPlan`에 생성 함수 `CreateCommand`·`CreatePreview`를 더했다.
- `LifeCycles/Artifact/Jobs/ArtifactBoardInitializeJob.cs` → `LifeCycleSystem/Common/`. 생성 함수 `Create(…)`와 우선순위 상수를 이 형식 안으로 옮겼다(부트 전역 준비 `ArtifactBoardPreparation`이 쓴다).
- `LifeCycles/Artifact/ArtifactLifeCycle.cs`(예약된 아티팩트 씬 LifeCycle)는 그대로 두었고, 요약 주석의 옛 `ArtifactEditFlow` 이름만 고쳤다. 빈 `LifeCycles/Artifact/Jobs`·`Plans` 폴더와 그 `.meta`는 지웠다.
- DB는 옮기지 않았다(아티팩트 정의는 전투도 읽는 InGame 값).

## 4. 동작 차이 (Framework 쪽에서 보일 수 있는 것)

- **Stage 전환 때 Runner가 비어 있지 않다(B1).** 전환이 출발 연쇄의 마지막 Job 안에서 일어나서다(전투 J7과 같은 방식). Stage `OnEnter`는 비동기 로드 뒤라 그때는 비어 있다.
- **로비 진입 때 Runner가 바빠도 예외가 아니다(B5).** 빌 때까지 다음 판단을 걸지 않을 뿐이다.
- **로비 이탈 검사(-200 노드)는 Runner를 보지 않는다(B4).** 로비 자기 사실(처리 안 된 편집, 진행 중인 출발)만 본다. 이탈 노드 순서(-200 검사 → -100 View 해제 → 0 LifeCycle)는 그대로다.
- Job·Plan 예외는 지금처럼 `GameBootstrap.Update`의 공용 오류 경계로 간다. 아티팩트 처리 기록 누락만 공용 오류가 아니라 오류 로그 + 아티팩트 편집 정지다(기존 정책 D11 유지).

## 5. SSOT·SOLID·Framework 계약

판단 원본은 `LobbyPlan.Decide`, 이탈 조건 원본은 `LobbyPlan.CanLeave`, 출발 요청 단계 전이 원본은 요청 안의 전이 표 하나다. 사실 읽기(`LobbyProgress`)는 캐시 없이 매번 Buffer·로딩 서비스에서 읽는다. LifeCycle(트리거)·Plan(판단)·Job(호출·확인)·View(입력·질문 읽기)로 나눴고, 단계 Job은 그룹을 받지 않고 자기 서비스만 생성자로 받는다.

FW-001에 따라 제공 인터페이스로 서비스를 등록하고, FW-002에 따라 바인딩이 필요한 두 객체를 모듈로도 등록했다. Builder는 한 번 Build한다. Runner·Register·Manager는 공개 서비스 경로로만 쓴다. Framework 기반(`Framework/`) 코드 변경은 없다.

## 6. 검증과 전달

MSBuild는 돌리지 않았고 새 시험 코드는 쓰지 않았다(사용자 결정). Phase 0~4마다 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인했다(모두 문제 없음, Phase 4는 빠뜨린 시험 파일 하나를 지운 뒤). 상세 결과 파일은 받지 않았다.

이 문서를 외부로 발송하지 않았다. 답변이 오면 변경 영향만 원본 설계·계획에 반영한다.
