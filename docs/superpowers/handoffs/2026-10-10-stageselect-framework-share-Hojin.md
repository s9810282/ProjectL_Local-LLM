# StageSelect P4~P7·공용 정리 구조 공유 — Framework·Core·전투 담당자께

- 작성자: 호진(이호진)
- 작성: 2026-10-10, View 작업(호진)
- 기준: 사용자가 완료한 병합 `9d4c76e50c044c8f4215c2f7d3e963bbc1082f07`과 그 위의 연결 보완 작업 트리. 커밋·push는 사용자가 한다.
- 상태: **공유 자료 준비, 미전달·미합의.** **2026-10-13 전달 전 갱신 필요:** §10.2·§10.3의 일부 사실이 이후 바뀌었다. View Factory에 로비 View가 더해졌고, 보드 초기화는 Stage LifeCycle이 아니라 Boot의 `ArtifactBoardPreparation`이 맡으며, StageFlow에는 아티팩트 도메인이 없다(LV5). Buffer Factory 등록은 15개다(RecoveryView·LobbyView·RecoveryResult 추가). 씬 번호는 2026-10-13 변경(Lobby 1·Stage 2·Combat 3)을 §10.2에 반영했다.
- 범위: [SA 설계](../specs/2026-10-10-stage-map-design-Hojin.md)와 [SA 계획](../plans/2026-10-10-stage-map-plan-Hojin.md). §1~§4는 P4 당시 기록, §5는 공용 오류 연결, §6은 아티팩트 계약 교체 기록이며 **맵/상점 직접 조회는 §7, 아티팩트는 §8, S5.2 Layer 등록은 §9, 사용자 병합 후 현재 단일 실행/정리 연결과 검증은 §11, 타 작업자 문서의 옛 구조 설명 갱신 요청은 §12**를 따른다. 모두 사용자 승인 로컬 변경이며 외부 합의 기록이 아니다. (2026-10-27: SA 설계·계획은 스테이지 맵 설계·계획으로, 상세 시퀀스 UML과 병합 검토는 View 구조 설계의 흐름 절과 스테이지 맵 계획으로 합쳐졌다. 아래 `§`·`P…` 번호는 합치기 전 판 기준이며 옛 판은 git `10914570`에 있다.)
- 기준: [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001/002/003/101·§9, [구조 문서](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md) §7. 최초 §7의 점선 경로를 프로젝트 결정으로 채택했으며 기존 직접 Runner 등록이 MUST 위반이라고 주장하지 않는다.
- 2026-10-11 문서 후속: 민형님 원본 3개를 정리 직전 백업으로 복원한 것은 이전 문서 정리의 이력이다. 현재는 사용자 병합 `9d4c76e5`에서 선택한 원본 내용을 보존한다. 현재 구현 설명은 [View 구조 설계의 흐름 절(옛 상세 시퀀스 UML)](../specs/2026-10-13-view-structure-design-Hojin.md), 원본 갱신 요청은 §10·§12를 따른다. 아래 §3은 P4 당시 요청 이력이다. 외부 전달·담당자 수락은 미수행이다.

## 1. 구현한 변경

이 절과 §2~§4는 **P4 당시 구현·요청 기록**이다. 빈 Shop Plan·미연결 Flow·기존 Scheduler 설명을 현재 코드에 다시 적용하지 않는다. 현재 연결은 §7·§8·§11을 따른다.

아래 파일은 `Assets/ProjectL/Script/Systems/` 기준이다.

| 위치 | 실제 변경 |
|---|---|
| `RuntimeSystem/BufferSystems/ShopView/` | 구매·나가기 명령과 번호, 읽기/쓰기 서비스. 입력 목록은 읽기 전용 뷰이며 처리 번호·게임 결과는 소유하지 않음 |
| `RuntimeSystem/BufferSystems/ArtifactView/`·`StageView/` | prefix 삭제와 Clear. 아티팩트는 명령과 발급 번호를 공유하는 최신 프리뷰 슬롯 추가. Clear/삭제로 발급 번호를 초기화하지 않음 |
| `Factory/Runtime/Buffer/BufferSystemFactory.cs`·`ServiceKeys/DataBaseGroupServiceKey.cs`·`RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | Shop Export → typed Read/Write key → `IBufferQuery` 제공. 기존 저장소 거래·Core 결과 게시 본문은 변경하지 않음 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs`·`JobRegisterSystem.cs` | 기존 enum 값 유지, `Shop = 4` 추가. Shop Plan 등록과 Artifact 입력 Read Sink 전달용 람다 추가 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/ShopPlan.cs`·`LifeCycles/Artifact/Plans/ArtifactPlan.cs` | static Plan에 필요한 입력 Read Sink만 전달. Shop은 `None`의 빈 목록만 지원, Artifact의 기존 `None`/`Start` 유지. 새 처리 분기/Job은 Core 인수 뒤 추가 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/` | `StageFlow`·`StageNodeEntryFlow`·`ShopFlow`·`ArtifactEditFlow` 준비. Runner Idle·로딩/fault·준비 조건, 한 Tick 하나, 기대 번호 확인과 1회 정지, 프리뷰 취소/교체, 전투 요청 수명 검사 |
| `StageLifeCycle.cs`·`LifeCycleSystemGroup.cs`·`ServiceKeys/SystemGroupsService.cs` | `IStageEntryAttemptRead : IServiceModule`을 StageLifeCycle에서 Export하고 그룹/typed key로 제공. 기존 맵 번호와 같은 `CombatEntryRequest` 참조만 노출 |

## 2. 등록 규칙과 W1 경계

- 서비스는 기존 Factory/Export 경로를 이용한다. 입력 Buffer는 데이터 서비스만 등록하며 실행 생명주기가 없어 별도 AddModule을 하지 않는다.
- StageLifeCycle의 기존 기반 클래스가 모듈을 등록한다. 새 읽기 서비스를 내보내기 위해 AddModule/ISystemTick을 중복 추가하지 않았다. Flow도 모듈로 등록하지 않는다.
- Plan 사전의 `Func<IInGameSystemGroupService, JobType, List<IJobService>>` 서명은 유지한다. 필요한 입력 Read Sink는 기존 람다 조립에서만 전달하며 Plan에 상태를 보관하지 않는다. 신규 처리 Job 자체는 아직 없다.
- 운영 StageLifeCycle의 새 읽기 대상은 미연결이고 빈 결과를 반환한다. `BindEntryAttempts`와 새 Flow 생성은 테스트에서만 사용한다. 실제 연결·이탈/Shutdown 정리는 P6에서 함께 구현한다.
- 운영은 기존 맵 Flow와 상점/아티팩트 스케줄러만 실행한다. 새 Flow Tick·Shop UI Submit·아티팩트 최신 프리뷰 제출을 연결하지 않았다. 공용 fault 콜백, UIBase/UISystem/입력 기반도 변경하지 않았다.
- Core 결과 번호와 맵 분기 경계는 테스트용 로컬 delegate다. H1~H4 API·정상 거절·skip·번호 수명과 실제 Job/입력 정리 순서는 인수 후 검증해야 한다.

## 3. 구조 문서 갱신 요청

아래는 민형님께 준비했던 **P4 당시 요청**입니다. 이후 P5~P7 전환과 사용자 병합으로 대체됐으며, 실제 원본 갱신에는 §10~§12의 현재 요청을 사용해 주세요. 이 자료 작성으로 담당자의 수락이나 문서 갱신 완료를 기록하지 않습니다.

1. `JobRootType`의 현재 이름 관례를 **LifeCycle 이름 → 처리 도메인**까지 확장하는 SA 결정(D3). `Shop`은 별도 LifeCycle이 아니라 StageLifeCycle 아래 처리 도메인입니다. enum 추가가 Framework MUST 변경을 뜻하지 않습니다.
2. `JobRegisterSystem`의 Shop/Artifact 람다 조립과 좁은 입력 Read Sink 전달. 기존 사전 서명과 Stage/Combat 경로는 유지합니다.
3. StageLifeCycle 소유 조정 Flow와 세 도메인 Flow, `IStageEntryAttemptRead` 제공 경로. **현재 운영 연결은 점선/미연결**, 기존 W1 소비자는 실선으로 구분해 주세요. P5/P6/P7 전환 이후에만 활성 경로로 바꿉니다.
4. 입력 번호/목록과 Core 처리 번호/게임 결과의 소유 구분, 최신 프리뷰 슬롯의 취소/교체와 발급 번호 유지. 실제 Core 필드 이름은 H1~H4 인수 뒤 확정합니다.

구조 문서 원본과 Framework 기반 코드를 이번 P4에서 직접 수정하지 않았습니다. 확인 결과가 구현 변경을 요구하면 실제 인수/후속 Phase에서 함께 반영합니다.

## 4. 검증과 한계

P4의 RED→GREEN, 관련 회귀·전체 기준선 비교·순차 MSBuild 결과와 증거 경로는 [SA 계획의 P4 검증 기록](../plans/2026-10-10-stage-map-plan-Hojin.md)을 원본으로 봅니다. 테스트는 실제 Buffer/서비스 조립/기존 전투 요청 상태를 사용하지만 실제 H1~H4 호출·운영 노드 분류·씬 이탈 순서·화면 연결·공용 오류 정리는 완료로 판단하지 않습니다.

## 5. P3.9 공용 오류 콜백 소유권 변경 (당시 기록, 사용자 승인, 미전달)

- Factory가 기존 Combat·StageMap·Artifact 모듈을 조립하고 `InGameViewFaultCleanup.StopInput` 하나를 공유 RuntimeFaultBoundary에 연결합니다. helper는 View 그룹 소유 일반 객체이며 서비스/모듈/Tick으로 등록하지 않습니다. 기존 모듈 수와 한 번의 Builder Build를 유지합니다(FW-001/002/003).
- helper는 Combat → Artifact → StageMap/Shop의 StopInput을 모두 시도합니다. 각 View가 자기 입력·임시 표현·Layer·구독을 정리합니다. 조립 계층이 개별 UI 자원을 직접 정리하지 않습니다.
- 그룹 Shutdown은 입력 정리 후 기존 자식 모듈 역순 종료를 모두 시도하고 finally에서 콜백을 해제합니다. 실패는 모아서 보고하며 반복/재진입 종료는 중복 실행하지 않습니다. 도중 연결 해제에도 남은 호출을 보장하도록 시작 시 정리 함수를 보관합니다.
- Combat.Shutdown에서 공용 경계 콜백 해제만 제거했습니다. 자기 Detach/StopInput·전투 계산/진입/연출은 유지합니다. 기존 다른 View에는 경계 콜백 해제 코드가 없었으며 자체 카메라/UI 구독 해제는 계속 각 View 책임입니다.
- 맵·상점·아티팩트 대기 입력을 Clear하되 발급 번호·Core 결과·Global Runner는 유지합니다. 같은 런타임 오류 복구/재연결은 허용하지 않습니다. Write Sink만 기존 서비스 경로에서 받으며 새 서비스 키/조회 계약은 추가하지 않습니다(FW-101).
- Framework 기반·RuntimeFaultBoundary·UIBase/UISystem/UIInputBinding은 수정하지 않았습니다. P3.10·새 UI 이벤트/Submit·운영 Flow 연결은 없습니다. 실제 W1 오류 정리·종료 실패·최종 연결 해제 증거는 SA 계획 P3.9 기록을 따릅니다.

구조 문서 갱신 요청: 공용 콜백의 연결/해제 소유자는 View 그룹, 개별 자원 정리는 각 View로 표시해 주세요. 이는 단일 콜백 계약에 맞춘 프로젝트 조립 결정이며 Framework 문서에 없는 새로운 MUST를 주장하지 않습니다. 이 자료는 민형님/전투 작업자에게 **미전달·미합의**이고 해당 소유 문서 원본은 직접 수정하지 않았습니다.

## 6. P3.10 아티팩트 UI 수명 계약 교체 (2026-10-11, 사용자 승인)

아티팩트 옛 View Host/Screen을 삭제하고 UI 옆의 IArtifactPopupDisplay를 View가 사용합니다. UI 소유 표시·칸 판정·허용/동기 수명만 제공하며 기존 작은 화면 대체 테스트를 유지하기 위해 SA 설계 §2.5의 허용 범위를 적용했습니다. 새 Framework 서비스/키/모듈/Tick이나 Contracts 폴더는 없습니다. Session이 Attach의 false→한 번 구독/초기 동기화→허용, Detach/fault의 false→해제→자체 정리를 소유합니다.

기존 Factory·그룹·공용 helper·RuntimeFaultBoundary와 UIBase·UISystem·UIInputBinding은 시작 복사본과 같습니다. 기존 3개 View 모듈·FW 등록/수명/좁은 서비스 경로도 유지합니다. UI에 View 타입·Core·Buffer 읽기·DB 조회를 넣지 않았고 기존 UISystem 전환과 직접 UIInputBinding 수신을 사용합니다. 동기 수명 알림은 버튼 입력 Event Bus가 아닙니다. 관찰자 예외를 격리해 남은 정리를 수행하며 Session 실패는 기존 경계로 보고합니다.

P3.10 테스트·컴파일·파일/검토 증거는 SA 계획의 후속 기록을 따릅니다. P5/P6·신규 Flow·상점 화면·Core/전투 인수는 진행하지 않았고 구조 문서 원본을 직접 고치지 않았습니다. 이 자료도 미전달·미합의입니다.

## 7. 맵·상점 H3/H4 직접 조회 (2026-10-11, 로컬 구현·자동 검증 완료)

최초 P5/P6에서 맵·상점 운영 경로를 전환했고, 사용자가 요청 조회를 Core로 통일하는 후속 구현을 승인했습니다. **후속 로컬 구현·자동 검증을 완료했으며** [SA 계획 P6.6~P6.10](../plans/2026-10-10-stage-map-plan-Hojin.md)과 [Core 교체 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md)를 기준으로 확인합니다. 최초 번호·내용 API와 `p56-*` 통과 수치는 이력입니다.

```text
UI/View → StageView·ShopView 입력 Buffer
        → StageLifeCycle/Flow → JobRegister → static Plan
        → struct Job(기대 번호·Core 인터페이스, 맵 임시 seed 공급자)
        → Core가 입력 직접 조회 → 기존 동기 게임 서비스 → Core 결과 Buffer
        → View가 결과/문맥 확인·표시·입력 정리
```

- H3는 `ProcessNextShopCommand(N)`, H4 일반은 `ProcessNextNodeCommand(N, nextSeed)`, H4 전투는 `TryProcessNextBattleCommand(N, out result)`입니다. Job의 요청 내용·입력/결과 Sink·반환 뒤 결과 대조를 제거하고 정상 true/false 모두 완료합니다. Plan의 필요한 입력/처리 번호 읽기는 유지합니다. Core가 결과를 게시하며 View/Job은 대신 쓰지 않습니다.
- `StageViewCommandData.ChapterKey`는 필수이며 실제 표시한 맵의 챕터로 제출합니다. 표시 전 제출을 막고 StageLifeCycle도 입력 챕터로 전투를 분기합니다. 번호 있는 RequestEntry는 등록 전에 입력 N·chapter/node와 요청 대응을 검사합니다. Core 미실행 false만 Prepare 내부 Running→Cancelled로 끝내며 일반 실행 중 취소는 계속 거부합니다. Bootstrap·Prepared→Activating→Active·취소 Job 배출과 동일 요청 참조 보존을 유지합니다.
- 입력 없음·번호 불일치·이미 처리됨은 상태/결과 변경 없이 false입니다. 같은 도메인 재진입은 예외입니다. 임시 seed는 JobRegisterSystem 인스턴스에서 공급하고 Core가 유효 입력/잠금 뒤 한 번 호출한 뒤 번호를 재확인합니다. static Plan 상태나 Run seed R4 완료로 해석하지 않습니다.
- 기존 동기 Shop/Combat 서비스를 시작한 뒤 정상 수락/거절은 입력 Clear와 관계없이 결과를 게시합니다. commit 이후 결과를 버리거나 Clear를 거래 롤백으로 해석하지 않습니다. 비전투 맵·상점 Flow는 처리 번호 또는 기대 입력 소멸로 대기를 해제하며 같은 입력의 실제 기록 누락만 해당 도메인을 정지합니다. 실제 예외·공용 fault는 전체 정지이고 전투 Cancelled는 기존 배출/입력 제거를 모두 기다립니다.
- 이탈 순서는 Stage 준비 -200 → View Detach -100 → LifeCycle Exit 0입니다. 병합된 전환 Job은 Runner가 바쁜 동안 Manager.Change와 동기 Load를 호출하므로, 같은 요청·N·수락·준비 완료·Activating·Manager Combat을 모두 확인한 경우에만 허용합니다. 그 외 busy/fault 정상 전환은 View 정리 전에 거절하며 공용 오류 정리/Shutdown은 idle을 기다리지 않습니다. 빠른 Stage 복귀는 같은 종료 요청의 Closed·Runner idle 뒤 준비를 한 번 등록합니다(§11). 전역 Runner/Framework 로딩 구현을 바꾸는 계약이 아닙니다.
- 실제 ShopUI/카드/프리팹/StageSelect 참조와 옛 상점/맵 소비자 삭제는 최초 P56 이력입니다. UISystem 전환·UIInputBinding/OnClick 0개·동기 UI 수명/오류 알림·입력 Buffer 제출을 유지합니다. 공용 콜백 소유권은 View 그룹이며 연결된 Combat 정리가 다른 Layer도 비운다는 기존 한계와 종료의 반복 StopInput도 유지합니다.

구조 문서에는 세 도메인 모두 활성 경로로 표시하고 **입력 Buffer → Core 직접 조회**, Plan/Job의 기대 번호·좁은 Core 의존성을 반영해 주세요. 직접 조회는 사용자 선택이며 Framework MUST가 아닙니다. 구조 문서 원본 갱신·외부 전달/합의는 미수행입니다. 직접 조회 후속 당시 자동 테스트는 `Logs/StageSelectSAVerification/stage-shop-pull-related.xml` 365/364 통과/실패 0/기존 판정 불가 1, `stage-shop-pull-all.xml` 3769/3734 통과/기존 실패 34/기존 판정 불가 1/skip 0입니다. 전체 실패 fullname은 `p7-pull-final-all.xml`과 같아 추가/해소 0이며 Unity exit 2를 전체 통과로 표현하지 않습니다. 그때의 빌드·독립 검토 및 당시 잔여 목록은 과거 이력이고, 상점 S5 후속은 상점 계획, 병합 후 새 검증은 §11을 따릅니다.

## 8. P7 아티팩트 활성 경로 (2026-10-11, 사용자 승인, 미전달)

기존 ArtifactEditingService가 IArtifactCommandProcessingService를 함께 Export하고, ArtifactBuffer가 편집/프리뷰 번호 결과를 제공한다. 서비스를 중복 모듈로 등록하지 않았다. StageLifeCycle 소유 ArtifactEditFlow → JobRegister → static ArtifactPlan → struct ArtifactCommandJob/ArtifactPreviewJob이 실제 운영 경로다. Flow는 읽기/등록, Core는 요청 조회/계산/게시, View는 표시/입력 정리를 맡는다. P7 최초 구현 뒤 사용자 추가 승인으로 아티팩트를 Core 직접 조회 방식으로 보완하고 자동 검증을 완료했다. 실제 API는 bool ProcessNextCommand(expectedRequestNumber)·ProcessLatestPreview(expectedRequestNumber)이며 기존 추가 인터페이스·같은 partial 서비스를 유지한다. Plan은 기대 번호와 좁은 Core 인터페이스만 Job에 넘기고, Job의 요청 내용·입력/결과 Sink를 제거한다. Core가 바인딩한 입력 Read Sink를 읽으며 입력 없음·번호 불일치·이미 처리됨은 false로 정상 완료한다.

Session이 연결 수명의 입력 Clear·구독·슬롯·자기 Layer를 정리하며 DragController.Stop으로 이전 수신자를 종료한다. 일반 숨김은 제출 편집을 유지한다. Job 등록 후 정상 이탈의 Runner 배출 검사는 기존 StageLifeCycle -200 노드가 담당한다. 같은 Stage에서 Detach→Attach는 이 가드를 거치지 않을 수 있어 Job의 기대 번호로 이전 실행을 새 입력과 분리한다. Core는 계산 전·게시 전 번호 확인으로 취소/교체된 제안의 게시를 막는다. ArtifactEditFlow는 처리 번호 ≥ N 또는 기대 편집 입력의 소멸로 이전 기다림을 끝내 새 연결의 요청을 등록한다. 같은 기대 입력이 남고 번호가 누락되면 도메인 정지를 유지한다. 이 P7 보완은 실제 재연결 편집 실패를 재현해 추가했다. 당시 Stage/ShopFlow와 맵/상점 Job의 결과 검사는 유지했으며 현재 H3/H4 후속 변경은 §7을 따른다. 아티팩트 Job은 결과 Sink를 읽지 않는다. Framework 기반·공용 오류 콜백 연결/해제 방식·UI 기반·전투 실행 코드는 변경하지 않았다.

설계 §2.4의 점선이던 아티팩트 경로도 이제 활성 경로다. H1/H2의 실제 API·수명·검증·교체 방법은 [P7 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계)를 따른다. Framework 문서 소유자의 인수나 원본 다이어그램 갱신 완료를 뜻하지 않는다.

직접 조회 보완은 전투와 요청 조회 주체를 맞추라는 사용자 결정이며 Framework가 강제한 함수 형태가 아니다. 전투의 무인자 API와 기대 번호 인자는 다르며, 좁은 Core 의존성으로 FW-101을 적용한다. static Plan·struct Job·모듈 등록·Framework 기반·UI 계약의 기존 규격은 유지한다. 최초 P7의 검증 수치는 이력이며 이번 보완의 실제 결과는 SA 계획 P7.5~P7.9에 별도로 기록한다. P7 당시 유지한 H3/H4 API는 현재 §7의 후속 직접 조회 구현·자동 테스트를 완료했고 최종 빌드·독립 검토도 완료했다.

## 9. S5.2 실제 Play에서 누락된 상점 Layer 등록 보완 (2026-10-11, 미전달)

- 실제 Boot → StageSelect 씬 → 상점 노드 클릭 → 상점 팝업 진입 경로에서, 팝업을 열며 입력 Layer를 ShopPopup으로 전환할 때 `UnityInputLayerRuntime.ResolveEntry`가 `Input Layer is not registered: ShopPopup`을 보고해 런타임이 정지했다. 기존 Controller/UI 테스트의 대체 Layer 서비스는 운영 등록 누락을 발견하지 못했다.
- 변경 파일은 `Systems/Factory/Runtime/RunTimeSystemFactory.cs`다. `StageMap.Shop` using과 루트 `inputLayers` 목록의 `ShopPopupInputLayers.Popup` 한 항목만 추가했다. 기존 전투·맵·아티팩트와 같은 생성 시 등록 경로이며 FW-005/006의 설정 수명을 따른다. 기존 Common 액션 맵만 사용하는 Layer 정의를 재사용하므로 별도 Controller/ActionMap/서비스/모듈/우회 입력을 추가하지 않았다.
- Framework 기반, Global InputSystem, UnityInputLayerRuntime, UIBase/UISystem/UIInputBinding, Core·Plan·Job·전투 실행 코드는 바꾸지 않았다. 씬·프리팹·DB 변경도 없다.
- 새 `ShopStagePlaySmokeTests`는 실제 운영 경로와 Raycast/UIInputBinding을 사용한다. 자연 맵을 찾는 검증이라 `[Explicit]`로 직접 선택한다. 최종 그래픽 Play 1/1과 관련 회귀 100/100 통과, 오류·예외 기본 감지 유지. 골드 부족은 테스트 중 잔액 0을 설정했다가 복원한 별도 경계조건이다. 자세한 증거·재실행·전투 바닥의 별도 시각 문제는 [상점 계획 S5.2 기록](../plans/2026-10-09-stage-shop-plan-Hojin.md)을 따른다.
- 이 문서 갱신은 민형님/전투/UI 작업자에게 실제 전달하거나 인수받은 기록이 아니다. 이후 루트 조립을 교체할 때 상점 Layer 등록도 유지해야 한다.

## 10. 민형님 원본 문서 갱신 요청 (2026-10-11, 원본 복원·미전달)

이전 문서 정리에서 사용자 요청으로 아래 세 문서를 **정리 직전 백업과 동일한 바이트로 복원**했다. 당시 Git HEAD로 되돌린 것이 아니며 문서 정리 이전의 변경을 보존했다. 이후 사용자가 incoming을 선택해 완료한 `9d4c76e5`의 문서 내용은 이번 보완에서도 그대로 유지한다. 아래 내용은 Framework 담당자가 검토하여 원본에 반영할 **요청 사항**이며, 반영·전달·합의 완료 기록이 아니다.

### 10.1 원본별 요청 사항

| 원본 | 요청 사항 |
|---|---|
| [framework-architecture.md](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md) | Bootstrap·Flow·Plan·Job을 미구현/미연결로 설명하는 부분과 클래스/시퀀스 UML을 현재 연결에 맞춰 갱신 요청. 실제 조립·등록 순서, 씬 번호, Buffer·DB 등록 현황도 아래 근거와 대조 요청 |
| [framework-programmer-guide.md](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) | 현재 실행 경로와 서비스 접근 예제, Render API 및 오래된 구현 상태 설명 갱신 요청. 과거 테스트 수치는 당시 기록으로 유지하고 현재 검증은 SA/상점 계획으로 연결 요청 |
| [data-table-core-view-guide.md](../../../Assets/ProjectL/Script/Systems/Documentation/data-table-core-view-guide.md) | 초기 Excel 스키마·삭제 이력·제안과 현재 런타임 DB 구성을 구분 요청. 현재 Factory/Query/SO 경로 및 등록된 맵·스테이지·View 테이블 설명 갱신 요청 |

**Framework 프로그래머 가이드 §6의 FW 규칙 25개와 MUST/SHOULD/MAY 등급을 바꾸라는 요청이 아니다.** 현재 프로젝트 구현을 설명하는 예제·현황·다이어그램의 갱신 요청이다. Core의 입력 직접 조회를 새 Framework MUST로 규정하거나, UI가 Buffer에 쓰는 프로젝트 결정을 기존 UI 공통 선례로 표현하지 않는다.

### 10.2 Bootstrap·등록·실행 경로 요청

- `GameBootstrap`은 실제로 존재한다. 씬 바인딩 → InitData → PostBindService → Initialize 후 Update에서 런타임 Tick을 호출하고 Stage/Combat 전환 시 UI를 재연결한다. 오류 뒤 Tick/전환 중지와 종료 정리는 실제 코드 범위로 설명하고, Framework 전체 롤백이나 예외 뒤 모든 Shutdown 완료를 보장한다고 쓰지 않는다.
- 런타임 초기화는 Buffer → Global → InGame → LifeCycle, 종료는 LifeCycle → InGame → Global이다. Global 등록은 Render → Utility → Audio → Input → Loading → UI → Job 순서다. Factory 경로는 `Factory/DBFactory`·`Factory/Runtime`이다. 정적인 파일 개수는 기준일을 표시하거나 제거 요청한다.
- View Factory는 전투·맵·아티팩트 View 및 공용 오류 정리 연결을 조립한다. Core Factory는 맵 생성/스테이지/챕터 진행 외에도 roster·상점·아티팩트·전투·맵 명령·회복·run 서비스를 조립한다. 실제 asmdef 참조도 현재 파일과 대조하되 Core의 분리된 어셈블리 경계를 유지한다.
- JobRootType은 Lobby/Stage/Artifact/Combat/Shop이며 JobType에는 None/Start/NewRun/ProcessCommand/ProcessPreview/AdvanceCombat 외에 incoming의 Next/ExitCheck가 있다. 모든 루트가 모든 타입을 지원하는 것은 아니다. 현재 CombatPlan은 None/Next/ExitCheck를 받고 진입은 RegisterEntry를 쓴다. static Plan과 custom runner의 Job 구성, struct Job의 boxing 및 등록 시 Buffer/seed delegate 의존성을 실제 구현대로 설명 요청한다.
- Stage Start는 StageJob, NewRun은 NewRunJob(priority 0) → StageJob(priority 1), 맵 명령은 StageNodeEntryJob이다. ShopCommandJob, Artifact 명령/프리뷰 Job 및 Combat 준비/명령/프리뷰/진행 Job이 연결되어 있다. 보드 초기화(priority 2)는 Stage LifeCycle의 직접 Runner 경로다(→ 바뀜: Boot의 `ArtifactBoardPreparation`이 맡는다, 위 2026-10-13 갱신 참고). Lobby와 독립 Artifact Start 등 남은 뼈대는 구현 완료로 바꾸지 않는다.
- 맵·상점·아티팩트의 Job은 좁은 Core 서비스와 기대 번호를 전달하고 **Core가 입력 Buffer를 직접 읽는다**. 맵 seed delegate는 별도 의존성이다. Core가 게임 결과/처리 번호를 게시하고 View가 입력을 제거한다. Flow는 실행 시점과 기다림·취소를 조정한다.
- StageFlow는 맵 진입 → 상점 → 아티팩트 명령 → 프리뷰 순서와 Runner/로딩/오류 가드를 조정한다. 전투 진행은 CombatLifeCycle의 Next 등록 → CombatPlan.Decide → 진행/전환 Job으로 표시하고, 삭제된 CombatFlow를 현재 실행자로 그리지 않는다. 남은 Lobby/독립 Artifact Flow TODO와 구분 요청한다.
- 전투 진입 제공자는 독립 CombatEntryService이며 CombatEntryRequest·RegisterCombatEntry·CombatPlan.RegisterEntry → Prepare → Transition이라는 별도 준비 수명이다. 전환 Job → Manager → 동기 Stage 이탈, CombatLifeCycle.OnEnter → ActivateAccepted, 빠른 Stage 복귀와 ExitCheck의 순서를 그림에 포함 요청한다. Prepared와 Active, Core 결과와 요청 수명을 구분한다.
- 실제 씬 번호는 `SceneIndices`가 원본이며 2026-10-13 현재 Boot 0, Lobby 1, StageSelect(Stage) 2, Combat 3이고 Artifact 4는 예약 번호다. 원본의 점선 확장 방향은 최초 설계 이력으로 보존하고 현재 연결은 실선/호출로 구분 요청한다.

### 10.3 Buffer·DB·Render 설명 요청

- Buffer Factory는 현재 12개를 등록한다(→ 바뀜: 2026-10-13 현재 15개, RecoveryView·LobbyView·RecoveryResult 추가): RunProgress, CurrentProgress, MapProgress, Artifact, Character, CombatState, Currency, StageProgress 및 CombatView, StageView, ArtifactView, ShopView. MapProgress가 삭제되었다는 현재 상태 설명은 대조 요청한다. 입력 저장소와 Core 상태 저장소, 처리 기록 소유자를 구분하고 실제 읽기/쓰기 서비스로 예제를 갱신 요청한다.
- 런타임 InGame DB의 명시적 Query 등록 14개와 초기 Excel Master 10개 설명을 혼동하지 않도록 구분 요청한다. View DB도 현재 Factory 기준으로 표기한다. StageMapTable·StageRootStageTable·StageViewPropTable은 현재 등록되어 있으므로 과거 삭제 기록을 현재 삭제 지시로 사용하지 않는다.
- 현재 StageTableQuery.GetStage(int)는 StageRootData를 반환한다. 현재 IInGameQuery에는 ItemTableQuery/CharacterTableQuery가 없다. Excel의 `Queries/InGameDataBase/Table/Stage/Core/StageTable.cs`와 런타임의 `Queries/InGameDataBase/InGameTable/StageTable.cs`를 구분 요청한다.
- 현재 StageRootDataSO → StageBaseData, CombatStageData → CombatStageDetail의 필드·loadout·적 데이터 경로가 있다. 데이터 가이드 §11의 별도 View SO는 미채택 제안으로 구분한다. Excel Core 행/View SO 구분을 모든 런타임 저작 방식의 강제 규칙으로 확대하지 않는다.
- 데이터 가이드 §9 예제·§10 Master 추가 안내·§12 삭제/테스트 기록에는 적용 시점과 범위를 표시하고 현재 Factory를 그대로 교체하는 지시로 읽히지 않게 정리 요청한다. 고유 스키마와 과거 이력은 유지한다.
- Render Factory의 Camera/Effect/Mesh/Color/Light/Outline/AreaOutline/RenderFeature 및 CoreGroup/ViewGroup 접근을 현재 코드와 대조 요청한다. Material Query는 사용 중이며 Outline 속성은 `_SelectOutline`이다. 카메라는 `global.RenderServiceCommand.CoreGroup.CameraGroup`, zoom/shake는 CameraEffectDispatch 경로로 예제를 확인 요청한다. RenderFeatureCameraFilter의 오래된 파일 링크도 실제 경로와 대조 요청한다.

### 10.4 검토 근거와 인수 조건

아래 경로는 `Assets/ProjectL/Script/Systems/` 기준이다. 원본의 예제/API 이름은 담당자가 해당 소스와 대조 후 확정한다.

| 근거 | 확인할 내용 |
|---|---|
| `RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs`, `Factory/Runtime/RunTimeSystemFactory.cs` | Boot·Tick·종료, 그룹/상점 입력 Layer 등록 |
| `Factory/Runtime/`의 Core/View/Global/Buffer Factory | 서비스·View·Buffer 실제 조립과 등록 순서 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/`, `RuntimeSystem/LifeCycleSystem/LifeCycles/` | enum·Plan·Job·Flow 및 전투 진입 수명 |
| `Factory/DBFactory/Group/InGameDataBaseFactory.cs`, `ViewDataBaseFactory.cs`, `DataBaseSystem/Queries/InGameDataBase/InGameDataBase.cs` | 현재 DB/Query 등록, Excel 스키마와 런타임 Query 차이 |
| [View 구조 설계의 흐름 절(옛 현재 시퀀스 UML)](../specs/2026-10-13-view-structure-design-Hojin.md), [SA 설계](../specs/2026-10-10-stage-map-design-Hojin.md) | 실제 세 도메인의 처리 순서와 프로젝트 결정 |
| [SA 계획](../plans/2026-10-10-stage-map-plan-Hojin.md), [상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md) | 날짜별 검증 증거·기존 실패·완료/잔여 범위 |

담당자 검토 후 원본의 구현 설명·예제·다이어그램이 현재 소스와 일치하고, 규범 본문과 고유 과거 기록이 보존되면 인수 기록을 추가한다. 현재는 **요청 문서 준비만 완료**했으며 원본 갱신·외부 전달·수락은 미완료다.

## 11. 사용자 병합 후 단일 실행·정리 연결 보완 (2026-10-11, 미전달)

사용자가 incoming을 선택해 완료한 HEAD `9d4c76e5`(부모 `a441daa1`, incoming `c1b9b0a3`)를 기준으로 복구한 연결입니다. 새 기준선 빌드는 전투 3인자 진입 미구현과 삭제된 전환 guard 참조로 실패했습니다. incoming의 EntryService·Decide·Next/ExitCheck·전환 Job을 유지하고 기존 StageMap·Shop·Artifact를 같은 실행 경계에 연결했습니다. 이전 검증 candidate를 그대로 적용했다고 가정하지 않았습니다.

| 소유 대상 | 현재 한 곳의 소유자와 이번 보완 |
|---|---|
| 운영 프레임 실행 | GameBootstrap → FrameworkRuntime.Tick → Global(JobRunner)·InGame(View)·LifeCycle(Manager). 도메인별 두 번째 Runtime/Tick/Runner를 추가하지 않음 |
| 등록·실행 | 기존 JobRegisterSystem/Runner. Stage의 지연 seed 공급·Artifact Buffer 전달·Shop Plan을 이 등록기에 연결하고 incoming Combat 등록을 유지 |
| 요청·결과 계약 제공 | 기존 StageCommands/ArtifactCommands typed key와 StageEntryAttempts 그룹 projection. 새 서비스 인스턴스나 대리 결과 저장소 없음 |
| 전투 요청 수명 | 독립 CombatEntryService. 첫 미처리 양수 N/chapter/node와 Queued 동일성을 검사하고 Prepare에서 기존 StageCommands를 호출. Bootstrap 번호 0 경로 유지 |
| Stage 이탈·복귀 | Stage -200에서 같은 요청/N/수락/준비 완료/Activating/Manager Combat을 모두 검증한 전환만 busy 허용. 빠른 복귀는 같은 Current Closed·전투 없음·Runner idle 뒤 준비/Reset 한 번 |
| 공용 오류 상태 | Bootstrap에서 조립한 RuntimeFaultBoundary. LifeCycleSystemFactory의 `new StageLifeCycle(faults)`로 Stage도 같은 경계를 사용 |
| 오류 정리 연결·해제 | View 그룹 소유 InGameViewFaultCleanup 하나. 각 View의 자체 Detach/StopInput과 입력·구독·표시 정리는 그대로 유지 |
| 종료/실패 active | Manager.Exit 거절 시 이전 active 복구. 역순 종료로 이미 Shutdown한 자식에는 재Exit하지 않으며 일반 모듈 Exit fallback 유지 |

단일 소유는 같은 책임의 중복 실행을 제거/예방한다는 뜻입니다. StageCommands·Shop·Artifact·Combat의 서로 다른 도메인 계산을 하나의 범용 서비스로 합치지 않습니다. ArtifactEditingService가 서로 다른 두 인터페이스를 **같은 객체**로 Export하는 것은 FW-001에 맞으며, Session/DragController의 자식 Tick도 별도 모듈 등록이 아닙니다. 이 구조로 SSOT·SOLID와 FW-001/002/003/004/016·FW-101을 확인했습니다. UI는 기존 UIBase/UISystem/UIInputBinding과 동기 수명 알림, Core는 계산·결과 게시 경계를 유지합니다.

새 증거는 `.superpowers/sdd/2026-10-11-post-user-merge/` 및 [스테이지 맵 계획의 병합 검토 결과](../plans/2026-10-10-stage-map-plan-Hojin.md)(옛 병합 검토 §7)을 원본으로 합니다. 공유 오류 조립 회귀가 수정 전 1건 실패한 뒤 새 전체 실행에서 통과했습니다. `repaired-all.xml`은 **4037건 중 4001 통과·실패 34·판정 불가 1·skip 1**, native exit 2입니다. 관련 부분집합은 612건 중 611 통과·실패 0·기존 판정 불가 1이며 전체에 포함됩니다. `(fullname, message)`와 중복 개수까지 비교한 실패 34건은 이전 `actual-main-all.xml` 및 `s53a-all.xml`과 같아 추가 실패 0입니다. `repaired-play.xml`은 상점 1·아티팩트 2·벽 공격 1의 **4/4 통과**, native exit 0입니다. 전체 테스트가 모두 통과한 것은 아닙니다.

이번 C# 수정은 운영 8개·테스트 9개이며 Core 계산·Framework 기반·UI·씬·에셋·GridDecal은 병합 HEAD와 같습니다. Framework 원본 3개와 병합에서 선택된 타 작업자 문서를 직접 고치거나 되살리지 않았습니다. 위 구조/다이어그램 갱신은 이 공유 자료와 우리 설계/UML에만 반영했습니다. 외부 전달·합의·커밋·push는 수행하지 않았습니다.

## 12. 옛 구조 설명 갱신 요청 (2026-10-11, 원본 보존·미전달)

사용자가 주석과 자체 문서의 현행화를 요청하면서 **다른 작업자의 문서는 직접 수정하지 말 것**을 지정했습니다. 아래 원본은 그대로 두고 담당자가 반영할 요청만 준비했습니다. 문서 작성자와 담당은 문서 표기 및 Git 이력으로 확인했으며, 과거 설계·검증 수치를 삭제하거나 현재 실행 결과로 바꾸는 요청이 아닙니다. Framework 원본 3개의 요청은 §10을 재사용하고 FW 규범 자체를 변경하지 않습니다.

### 12.1 담당자별 요청

| 담당·원본 | 확인한 오래된 안내 | 반영 요청 |
|---|---|---|
| 민형님: §10.1의 Framework 원본 3개 | Bootstrap/Flow의 미구현 안내와 옛 Job 등록 예시·씬/DB 도표 | §10.2~§10.4 및 §11을 기준으로 현재 구현 현황·예제를 갱신해 주세요. MUST/SHOULD/MAY 등급과 당시 기록은 보존합니다 |
| 전투 담당: [2026-10-09 프로그래머 인계](2026-10-09-combat-skeleton-view-programmer-handoff.md) §0~§3 | `CombatFlow.cs`를 작업 시작점으로, `CombatLifeCycle.RequestEntry`·`CombatExit`·guard를 현재 호출 경계로 안내 | [Job·Plan 재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md)와 현재 코드로 작업 지도를 갱신하거나, 상단에 과거 스냅샷·대체 문서 안내를 추가해 주세요. 진입은 독립 EntryService, 판단은 Plan.Decide, 진행·종료는 Next/ExitCheck와 전환 Job입니다 |
| 전투 담당: [2026-10-07 UML HTML](../specs/2026-10-07-combat-view-skeleton-current-uml.html) | HTML 단독 열람 시 `CombatFlow`·`CombatExitSequence` 도표가 현재 UML로 보임. 연결 MD의 과거 경고는 HTML에 없음 | 도표를 당시 기록으로 보존하면서 HTML 상단에도 재구성 전 스냅샷임을 표시하고 최신 재구성·[View 구조 설계의 흐름 절(옛 통합 시퀀스 UML)](../specs/2026-10-13-view-structure-design-Hojin.md)로 연결해 주세요 |
| 전투 담당: [View 계획](../plans/2026-10-01-combat-view-plan.md), [10-07 UML MD](../specs/2026-10-07-combat-view-skeleton-current-uml.md), [골격 설계](../specs/2026-09-30-combat-flow-skeleton-design.md), [View 설계](../specs/2026-09-30-combat-view-design.md)의 첫 배너 | 병합 전에 추가한 “상대방 코드 미병합” 안내가 남음 | 배너만 현재 `9d4c76e5` 이후 통합·연결 보완 상태로 정정하고 [스테이지 맵 계획의 병합 검토 결과](../plans/2026-10-10-stage-map-plan-Hojin.md)(옛 병합 검토 §7)을 연결해 주세요. 상대 브랜치와 이번 검증 기록은 계속 구분합니다. 배너 일부를 호진이 작성했어도 타 소유 문서이므로 이번에 직접 고치지 않았습니다 |
| 전투/프로젝트 안내 담당: [AGENTS.md](../../../AGENTS.md)의 현재 상태·시작점 | 현재 시작점이 이전 골격 인계로 연결되고, 날짜별 `CombatExitSequence`·`CombatExit` 이력 위에 새 재구성 기준이 없음 | 작업 규칙과 날짜별 이력을 보존하고 현재 시작점에 재구성·사용자 병합 후 기준을 연결해 주세요. 옛 API 설명은 당시 기록임을 명시합니다 |
| 천명님: [아티팩트 Framework·View 인계](2026-10-03-artifact-framework-view-handoff-ko.md) §1·§2·§8 | 화면·입력 미연결, Artifact Plan의 명령/프리뷰 처리 미구현 안내 | 10-03 인수 당시 상태임을 표시하고 [P7 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계)와 통합 UML로 연결해 주세요. 현재는 ArtifactPlan의 명령/프리뷰 Job이 기대 번호로 CoreGroup.ArtifactCommands를 호출하며 보드 초기화는 별도입니다 |
| 천명님: [Stage Map View 인계](../guides/stage-map-view-handoff.md) 서두·§1~§2 | “현재 동작만 설명”이라고 하면서 `StageMapSession → Unity Data Catalog → StageSessionFactory`와 옛 어셈블리 경계를 운영 지침으로 제시 | 이전 Core 구조의 인계임을 상단에 표시하고 [현재 맵 입력 안내](../viewDocs/stage-map-guide-Hojin.md)·통합 UML·[P56 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md)로 연결해 주세요. 옛 예시를 현행 코드로 복구하지 않습니다 |

### 12.2 현재 코드와 대조할 기준

- 맵·상점·아티팩트는 `입력 Buffer → StageFlow/각 도메인 Flow → JobRegisterSystem → static Plan → 공용 JobRunner → Job → Core → 결과 Buffer → View`입니다. Core가 기대 번호의 입력을 직접 조회하고, View가 자기 입력을 정리합니다. 삭제된 Scheduler·요청·Host/Screen을 병행 등록하지 않습니다.
- 전투 진입은 `CombatEntryService → CombatPlan.RegisterEntry → CombatPrepareJob → CombatEntryTransitionJob`입니다. 진행 판단은 `CombatPlan.Decide`, 등록 요청은 `Next`/`ExitCheck`입니다. 맵 번호 전달·동기 Stage 이탈·빠른 복귀 조건은 통합 UML §5와 §11을 따릅니다.
- 아티팩트 UI는 `IArtifactPopupDisplay`와 동기 `OpenStateChanged`를 사용하고 Session이 구독 수명을 소유합니다. 공용 오류 콜백은 `InGameViewFaultCleanup` 한 곳에서 연결·해제하고 개별 View가 자기 자원을 정리합니다. 정리의 멱등 재호출과 중복 실행 주체는 구분합니다.
- `ArtifactLifeCycle`의 독립 씬 훅과 `ArtifactPlan.Start`의 기본 골격은 남아 있지만, 운영 팝업의 진입·편집 경로가 아닙니다. 예약 골격이나 상점 Run seed R4 임시 공급원까지 모두 제거됐다고 표현하지 않습니다.

이번 주석 정리는 `RuntimeFaultBoundary.cs`, `LifeCycleBase.cs`, `LifeCycleManager.cs`, `ArtifactLifeCycle.cs`의 설명만 바꿨습니다. 실행문·API·등록·테스트는 유지하며 자체 문서의 현재 설명을 정정하고 원 결정·검증은 이력으로 보존했습니다. 시작 스냅샷과 대조하여 타 소유 문서·주석을 제외한 C# 내용·기존 스테이징이 그대로임을 확인했습니다. 링크·공백·UTF-8/LF 검사도 통과했습니다. 자세한 범위와 검증은 SA 계획의 마지막 기록을 따르며 Unity 테스트는 다시 실행하지 않았습니다. 담당자 전달·수락·커밋·push는 수행하지 않았습니다.
