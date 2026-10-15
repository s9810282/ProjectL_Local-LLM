# 전투 진행 골격 구현 계획

- 작성일: 2026-09-30
- 기준: [전투 진행 골격 설계 8차](../specs/2026-09-30-combat-flow-skeleton-design.md). 2026-10-03 `81c8c38` 인수에 따라 명시적 진입 계약 이전을 Phase 6A로 추가하고 기존 수명 통합을 6B로 구분했다.
- 상태: **Phase 0~5·6A/B3 유지. `5078ee0e`의 F1·정상 F2 기반은 사용자 검증 완료 확인으로 인수. 일반 승리의 StageSelect 자동 복귀 호출자 추가(§6.14, `bfe63a27`). 골격 7·View F 문서 인계 완료(§6.15).** 패배·최종 승리 목적지는 사용자 지시로 보류하고 포기는 Core API 인수 후 연결한다. 따라서 6B 전체 완료는 아니다. Stage 골드·Run 재화의 요청은 [Core 요청 3번](../handoffs/2026-10-05-combat-core-requests-phase6b.md#11-요청-3-상세--두-재화의-원본초기화보존)을 따른다.
- 대상: 스테이지 내부 `CombatLifeCycle`·`CombatFlow`·`CombatPlan`·전투 Job. `StageLifeCycle`은 스테이지 맵이며 이번 대상이 아니다.
- 방식: 설계 → 이 Task 계획 → Phase별 구현·검증·보고. **각 Phase가 끝나면 멈춘다.** 새 기능은 기본적으로 실습 워크시트로 제공하며, 사용자가 직접 진행을 요청한 경우 해당 Phase를 구현한다.
- 커밋·push와 외부 전달은 사용자가 한다. 코드 구현·검증 상태는 실행 기록으로 구분하며 문서 작성만으로 담당자 합의를 완료 처리하지 않는다.

## 0. 완성 목표와 현재 착수 범위

완성 목표는 **준비 완료 → 국면에 따른 Job 등록 → 처리 결과 확인 → 연출 대기 → 다음 등록**을 잇는 전투 진행 골격이다. Flow는 진행 조건, Plan은 Job 구성, Job은 처리 서비스 호출을 맡는다. 규칙은 Core가 계산하고, 상태·결과는 Buffer가 보관한다.

| Phase | 목표 | 착수 조건 | 완료 후 얻는 것 |
|---|---|---|---|
| 0 | 기준 확인·공용 변경 공유 문서·연동 계약 정리 | 문서·기준 정리 완료. 기준 테스트 결과 미수신 | 수정 범위와 합의 대기 사항이 구분된 작업 기준 |
| 1 | JobType·Plan·Job 네 개의 틀 | Phase 0의 기준·공유 문서 준비 | 컴파일·독립 테스트 가능한 작업 구성 |
| 2 | CombatFlow 1단계(F3 무관 부분)와 단위 검증 | Phase 1 | 준비·명령·미리보기 확인, 연출 대기, 정지를 가짜 상태로 검증. `Automatic`은 등록 없이 대기(K9) |
| 3 | CombatLifeCycle 훅 1단계 연결 | Phase 2 | 바인딩·진입·Tick·이탈 훅이 Flow·Plan에 연결됨. **이탈 미완성**. 실제 진입은 활성화하지 않고 공통 바인딩은 검증(K10) |
| 4 | 자동 진행 등록·결과 확인(F3 연결) | Phase 3 + **F3 타입 확보. 생산 연결 완료에는 읽기 서비스 구현·등록도 필요** | Flow가 `AdvanceCombat`을 등록하고 수락/거절로 계속·정지를 판단 |
| 5 | Job의 Core 서비스 호출 연결 | Phase 4 + **R6 서비스·R1~R4 기록·F3 결과 구현 확보** | 실제 Plan·Job·Runner 경로로 처리·결과 확인 |
| 6A | 명시적 전투 진입 계약 이전 | Phase 5 + `EnterBattle` 계약 인수. View B3와 함께 진행 | 대상 전달·진입 결과·한 번의 활성화, 두 개발 모드 진입 |
| 6B | F1 인수·실패·이탈 수명 통합 | 6A. F1은 기존 View 구현 인수, F2는 별도 계약 확보 | 예외 뒤 재실행 차단, 안전한 정상/포기 이탈·재진입 |
| 7 | 검증 결과·인계·View 연동 조건 기록 | 완료한 Phase의 증거 확보 | 골격 완료 범위와 남은 실제 구동·View 작업이 명확한 인계 |

**현재는 6B의 일반 승리 복귀 연결까지 반영한 상태다(§6.14).** 아래 순서 1·2와 F1은 사용자 완료 확인으로 마감했다. 실제 DB 조립(T1/G7)과 F2 전체 완료를 기다리지 않고 유효한 fixture를 사용한다. 운영 DB 성공과 이탈/재진입은 별도이며 완료 범위의 Phase 7 부분 인계는 가능하다.

### 0.1 병합 후 실행 순서와 계획 간 소유권

**2026-10-05 / `5078ee0e` 인수 후 통합 순서의 원본이다.** 기존 Phase·Task 번호를 유지한다. 표의 순서는 권장 진행 순서이며, 외부 계약 요청과 독립 작업까지 앞 단계의 전체 완료를 기다리지는 않는다. 각 Phase의 결과를 보고한 뒤 다음 구현을 진행한다.

**진행 갱신:** **1·2번과 3번 F1 완료.** 4~5번의 정상 종료 기반도 커밋 검증 확인으로 인수했으며 일반 승리 자동 호출자를 추가했다. 남은 구현은 패배·최종 승리 목적지 확정 후 연결과 Core 포기 API 인수 후 연결이다. 실제 결과는 §6.14 및 View §7.23을 따른다. 아래 표의 완료한 작업을 다시 수행하지 않는다.

| 순서 | 기존 Phase / Task | 남은 작업과 완료 기준 | 진행 조건 |
|---|---|---|---|
| 1 | **골격 6A + View B3 마감** / 6A.7, B3.4~5 | §6.11의 자동 회귀 증거를 연결하고 Boot의 두 모드·정상 거절·잘못된 개발 설정·Stage 시작 사례 중 미확인 재생을 마감한다. 대상별 진입·활성화 1회와 운영 F2 미완료를 구분해 인계 | **바로 가능.** 기존 진입 코드를 다시 구현하지 않으며 Core DB source는 선행 조건 아님 |
| 2 | **View E·E2·A2 잔여 검증** / E.7·E.9, A2.6 | 새 경로 이동·Core 견적·Return·공격 취소 후 이동 복구·최종 결과 확인을 인수한다. 카메라 줌·프레임 변화·UI 겹침과 실제 카탈로그를 사용하는 fixture Boot 재생을 확인. 기존 자동 테스트는 재사용하고 누락만 보강 | **바로 가능.** DB 카탈로그 생성과 운영 DB 전투 준비는 별개. E.8은 다음 F1과 공동 검증 |
| 3 | **골격 6B — F1** / 6.0a·6.3, View E.8·X3 | **완료.** `5078ee0e`의 Bootstrap·입력·연출 정리 보완을 사용자 검증 완료 확인으로 인수 | 새 오류 경계·전역 플래그를 만들지 않음 |
| 4 | **골격 6B — F2 계약** / 6.0b, View X6 | 정상 이탈 서비스·Completion 표시·EndBattle 소유자는 확정. 남은 것은 패배·최종 승리 목적지와 Core 소유 포기 공개 API의 실제 서명·실패/중복 의미 인수 | 목적지는 사용자 보류, 포기는 Core API 대기. 요청 문서는 준비됨 |
| 5 | **골격 6B — F2 구현·통합** / 6.1·6.2·6.4·6.5, View X6 | 일반 승리의 실제 호출자 → 결과 확인 → EndBattle → StageSelect 연결 반영(§6.14). 남은 목적지·포기 계약을 받으면 같은 이탈 경계에 연결 | 일반 승리 추가 코드의 실행 기록은 §6.14. 후속 연결은 §0.2 조건 인수 후. T1/G7 전체 완료를 기다리지 않음 |
| 병행 | **View A2 후속 운영 DB 인수** / T1/G7·시작 재화 | Core가 제공된 지정 방식·유닛·스테이지 데이터를 실제 준비 정의로 조립한 뒤 `useDevBattle=false`로 진입·공격·미리보기·드로우를 검증. 실제 로스터·필드·적·Economy가 반영되고 fixture가 개입하지 않는지 확인 | **2026-10-07 해소·적용:** 기본 DB 소스가 전투 정의를 돌려주고(Core `5f2d0381`, 병합 `35f247a9`), 개발 진입에 전투 정의 선택(`devBattleDefinition` = Fixture/Database)을 더해 Combat 직접 시작에서도 DB 정의로 확인 가능(View 계획 §7.56 S7). 운영 경로는 S2(다른 작업자) 뒤. ~~현재 Core 구현 대기.~~ §0.2의 준비 소스·재화 조건이 충족되면 4~5와 병행. 운영 이탈·재진입 전체 인수만 F2도 필요 |
| 마지막 | **골격 7 + View F** | **현재 코드 범위의 문서 인계 완료(§6.15 / View §7.24).** 설계·현재 통합 UML·공유·Core 답변 반영 범위·새 공동 인계 정리 | 기능 전체 완료와 구분. 남은 구현을 인수할 때 해당 문서 갱신. 외부 발송은 사용자 |

이미 구현한 골격 0~5, 6A 진입 코드, View 0~E2·A2 코드를 다시 만드는 계획이 아니다. **F1(오류 시 실행 중단), F2(정상/포기 이탈 수명), F3(처리 결과 읽기·보존)**는 계약 이름이고, **View Phase F**는 인계 작업이다. F3는 인수됐으며 F2에서 보존 회귀를 확인한다.

| 한 번만 구현할 대상 | 구현 계획 | 다른 계획의 역할 |
|---|---|---|
| Job 몸통·Flow/Core 통합 테스트 | 골격 Phase 5 | View는 연결된 Job을 사용 |
| 진입 대상·결과 전달, Job, 활성화 수명 | 골격 6A | View B3는 확정된 좁은 진입 계약을 사용. ViewSystem/Interaction에서 Core 직접 호출 금지 |
| Bootstrap 개발 진입·두 모드 대응 | View B3, 골격 6A와 공동 검증 | 골격 세션이 계약·호출 예시·대상 파일을 먼저 확정. 파일 수정은 두 세션에서 중복하지 않음 |
| GameBootstrap·RuntimeFaultBoundary | View B, 입력/표현 연결 C·E | 골격 6.3에서 실제 경계 인수·수명 회귀 검증 |
| 개발용 준비 입력·실행 모드·씬 | View B | 골격 Phase 5는 테스트 fixture만 사용 |
| 전환 요청 경계·포기/정리 순서 | 골격 Phase 6, Core·Framework 계약 인수 후 | View는 새 입력 중단·결과 표시/완료 연결 |
| DB 에셋·기존 전투 Economy 연결 | DB 정렬 계획 T11, View A에서 필요한 부분 수행 | 골격 테스트의 선행 조건으로 삼지 않음 |

**현재 이어갈 출발점:** 골격 §6.15와 [View 계획 §7](2026-10-01-combat-view-plan.md#7-검증과-실행-기록)의 최신 §7.24를 읽는다. §6.9~13은 이전 기록이다. Bootstrap·씬·F1·경로 이동·Return을 다시 만들지 않는다. Flow 분기 원본은 골격 설계 §4.1, 이탈 호출 정책 원본은 §6.4다.

### 0.2 구현 차단 사유와 해소 작업

Core에 전달할 최신 요청·실제 API·인수 기준은 [2026-10-05 Core 요청사항](../handoffs/2026-10-05-combat-core-requests-phase6b.md)에 정리했다. 아래는 Phase에 미치는 영향이다. 운영 DB 검증은 개발 source를 주입하지 않는 **정상 로스터·챕터·진입 호출자**를 사용하며, 현재 Boot의 Combat 단축 시작에서 토글만 끄면 직접 진입이 거절되므로 호출자 정렬도 골격·View에서 수행해야 한다.

| 막히는 범위 | 현재 사유 | 해야 할 일 / 담당 | 해소 기준 |
|---|---|---|---|
| **6B F2의 패배·최종 승리 전환** | **2026-10-06 사용자 결정(설계 K21):** 최종 목적지는 Title, 화면이 생길 때까지 **새 Run 준비 후 StageSelect**. 최종 여부는 `EndBattle` 뒤 Core `CompleteChapter`로만 알 수 있고 현재 런타임은 연결 정의가 없어 `DefinitionUnavailable` → 임시로 Run 종료 | **골격 담당**이 같은 F2 경계에 Run 종료 판정과 `Combat/NewRun` 준비 Job(초기화 → 로드아웃 1 로스터)을 연결(§6.16). Title 화면이 생기면 목적지만 교체 | Run 종료 결과 확인 → EndBattle → 새 Run 준비 Job 완료(Outcome InProgress, 로스터 재지급) → StageSelect. Run이 이어지는 결과는 초기화 없이 이동. Run 시작 재화는 DB 대기라 0 |
| **6B F2의 자발적 포기** | 공개 API는 `AbandonBattle(ArtifactLoadoutSnapshot)`이며 View가 Core 정산 입력을 조립할 수 없음 | **Core 담당**이 현재 상태/로드아웃을 내부에서 읽는 공개 포기 경계를 제공. **골격 담당**은 실제 API 인수 후 Job/전환 연결. [포기 계약 요청](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) 참조 | 실제 서명·반환·중복 호출 의미와 Completion 기록 테스트 확보. 인자 없는 API 이름은 아직 제안일 뿐이며 View에서 payload를 임의 구성하지 않음 |
| **A2 후속 운영 DB 전투 준비(T1/G7)** | **2026-10-07 해소:** 기본 DB 소스가 공격 프로필까지 조립해 반환한다(Core `5f2d0381`). 개발 확인은 `devBattleDefinition = Database`(S7). 아래는 이전 사유: A2의 지정 방식 테이블·유닛 참조/query는 제공됐지만 `BattleDefinitionAdapter`의 새 공격 프로필 조립과 기본 source 반환이 미연결. `DatabaseBattleInitializationSource.Read`는 조회 뒤 여전히 무조건 예외 | **Core 담당**이 `TargetingMethodKey` 조회 → 지정 방식·공격 프로필 → 전체 준비 정의 조립을 연결하고, 정의 검증 후 source가 반환하도록 수정. 단순 예외 삭제로 마감하지 않음. 요청 원본은 [DB 경계 인계 §5.6](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달) | 실제 DB·로스터·필드·구조물·적·Economy·공격 정의로 `useDevBattle=false` 진입 및 처리 통과. View는 계속 Buffer의 사실/응답만 사용 |
| **재화를 포함한 운영 플레이** | **2026-10-06 Stage 골드 분리 인수(Core `da3a22db`, 병합 `459a69e3`):** 드로우용 Stage 골드는 `CombatStateData.StageGold`(준비 때 `Economy.InitialGold` 지급, `EndBattle`로 정리), `CurrencyBuffer`는 상점·회복용 Run 재화만 맡는다. 전투 HUD는 `StageGold`를 읽는다(전역 `MainCharactorUI`는 Run 재화 유지). 골격 코드는 재화를 읽지 않아 변경 없음. **남은 것: Run 시작 재화의 DB 값(Excel DB 대기, 그전까지 새 Run은 0)** | Core가 [요청 3 상세](../handoffs/2026-10-05-combat-core-requests-phase6b.md#11-요청-3-상세--두-재화의-원본초기화보존)의 확정 규칙에 맞춰 분리·초기화·수명 연결. View는 인수한 읽기 API로 표시 | 새 Stage는 해당 SO 초기 골드, 같은 Run 재화는 유지, 새 Run만 Run SO/DB 초기 금액 적용. 상점·회복과 드로우의 잔액 교차 변경 없음. View 직접 지급·초기화 없음 |
| **지뢰·Object·목적지 보정의 실제 콘텐츠 인수** | 저작 데이터와 실제 상황에서의 표시 연결이 남음 | **데이터/Core 담당**이 해당 사실을 제공하고 **View 담당**은 받은 경로·이벤트·견적으로 표시. [병합 잔여 조건](../handoffs/2026-10-04-stage-merge-verification.md#remaining-dependencies) 참조 | 해당 콘텐츠를 넣은 실제 경로 테스트 통과. 기본 이동·F1/F2 작업의 공통 선행 조건으로 확대하지 않음 |

6A/B3·View E/E2/A2와 `5078ee0e`의 F1·정상 F2 기반은 사용자 검증 완료 확인으로 마감했다. 이후 변경과 Core 미구현 조건은 별도로 추적한다. 시작 서브 수 변경, Reason 코드별 HUD, 비이동 VFX·Drag 외형 보강, 대규모 성능 측정은 현재 기본 흐름의 차단 조건이 아니며 View §8에서 필요 시 확장한다. Core 요청 문서 작성과 실제 외부 전달/답변은 구분한다.

## 1. 지킬 경계

- Core 로직·Buffer 저장 구조는 다른 담당자 영역이다. 이 계획에서는 서비스 요청과 우리 Job의 호출 연결을 수행한다. 다른 영역의 코드가 필요하면 전달 문서에 요구사항을 적는다.
- 기존 JobRegister → Plan → Global Runner를 사용한다. 6A에서 대상 전달에 필요한 좁은 계약을 확정하되 별도 Runner, Core 요청 큐, 범용 payload 프레임워크, 이벤트 버스는 추가하지 않는다.
- `CombatFlow`는 `CombatLifeCycle`이 소유하는 일반 `sealed` 클래스다. 새 Flow 서비스 모듈이나 쓰임 하나인 Flow 인터페이스는 만들지 않는다.
- Flow의 가변 상태는 정지 여부와 진행 중인 확인 조건 하나다. 국면·체력·위치·실제 처리 번호는 Buffer에서 읽는다. `IsIdle`의 복제 플래그를 두지 않는다.
- **F3 타입 전에는 Flow가 `AdvanceCombat`을 등록하지 않는다(K9).** `Automatic` 국면에서는 기다리기만 한다. 순번 증가만 보는 임시 성공 판정을 두지 않는다. 결과 읽기 의존성·등록·확인은 Phase 4에서 한꺼번에 더한다.
- Job 네 개는 `struct : IJobService`다. 생성자는 서비스와 우선순위를 받고, `Execute()`에서 처리 서비스를 한 번 호출한다. 완료 후 재호출은 무시한다.
- 연결 전 Job은 명시적인 `InvalidOperationException`으로 미연결을 드러낸다. 현재 네 Job은 Core에 연결됐다. 예외를 삼키거나 `finally`에서 완료로 표시하지 않는다. 예외 재실행 방지는 F1에서 보장한다.
- 모든 전투 Job의 Priority는 0이다. 준비 선행과 명령/미리보기 순서는 **Flow의 등록 조건**으로 보장한다. 높은 Priority로 준비 순서를 대신하지 않는다.
- `JobType.None/Start/NewRun`의 숫자는 유지한다. 새 값은 `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5`다. 구현 시작 때 다른 담당자의 변경과 충돌하지 않는지 다시 확인한다.
- `BeginPreparation()`은 멈추지 않음, Runner Idle, 전투 없음(`!HasBattle`), 진행 중 확인 없음의 네 조건을 확인한다. 하나라도 어기면 **등록하지 않고 `InvalidOperationException`을 던진다(K11).**
- `Stop()`은 새 등록만 막는다. 이미 등록된 Job을 취소하거나 입력 Buffer를 비우지 않는다. 정상 이탈의 Stop에는 오류 로그가 없고, 실패를 감지한 경로에서만 원인을 한 번 보고한다.
- 전투 View는 상태 변경 후 오래된 미리보기를 무효화한다. 같은 대상의 미리보기가 여전히 필요하면 `SetPreview()`로 새 번호를 발급받는다. Flow는 번호만 보고 등록하며 View의 선택·규칙을 계산하지 않는다.
- 코딩 형식은 블록 네임스페이스, Allman, 공백 4칸, UTF-8/LF, 한국어 XML doc이다. 새 `.cs`·폴더·테스트에는 `.meta`를 함께 다룬다. 기존 파일 이동은 GUID를 유지한다.
- 테스트는 기존 `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef`를 사용한다. 새 테스트 asmdef를 만들지 않는다. 컴파일·테스트는 Unity 에디터에서 확인한다.

## 2. Phase 0 당시 코드에서 확인한 출발점 (과거 기준)

| 확인한 사실 | 계획에 미치는 영향 |
|---|---|
| `CombatLifeCycle`은 진입·이탈 TODO이며 `SceneIndex => 3`이다. 전투 진입 호출 경로는 현재 소스에서 확인되지 않지만 PostBindService는 모든 등록 LifeCycle을 순회한다 | Phase 3에서 기존 Buffer·Job 바인딩을 검증한다. 실제 진입·씬 번호 변경은 별도 씬 결정 이후다 |
| `CombatPlan.Start`는 `CombatJob` 하나를 만들고, 그 Job은 현재 조용히 완료한다 | Phase 1에서 준비 Job으로 이름·동작을 바꾼다 |
| `CombatJob.cs.meta` GUID는 `87148ad8eb6ab384ca930f4bda0fc576`이다(계획 리뷰 때 재확인) | `CombatPrepareJob.cs.meta`로 함께 옮기고 GUID를 유지한다. 구현 직전 파일이 바뀌었으면 당시 GUID를 재확인한다 |
| JobRegister는 이미 CombatPlan을 등록하고 서비스 참조를 넘긴다 | 이번 골격을 위해 Register 서명이나 Combat 등록 람다를 변경할 필요가 없다 |
| `IInGameCoreSystemGroupService`에는 아직 `CombatCommands`가 없다 | Phase 1~4에서는 존재하지 않는 API를 호출하지 않는다. Phase 5 전에 실제 계약과 그룹 노출을 받아 확인한다 |
| 상태 Buffer에는 국면·번호 읽기 자리가 있지만 번호를 갱신하는 처리 서비스는 없다 | Phase 2·4는 가짜 상태 읽기 구현을 사용한다. 실제 Core 처리 완료로 보고하지 않는다 |
| F3 결과 읽기 인터페이스는 현재 코드에 없다 | Phase 2는 F3 없이 진행한다(K9). Phase 4 전에는 가칭 이름을 생산 코드에 선언하지 않는다 |
| JobSystemTests가 Combat Start를 공용 동작의 표본으로 사용한다. `CombatJob`/`CombatPlan`에 기대는 테스트는 이 파일 하나다 | Phase 1에서 Lobby 표본으로 옮기고 전투 전용 구성 검증을 별도로 둔다 |
| (병합 뒤 갱신) 테스트에서 `IInGameCoreSystemGroupService`를 구현한 가짜는 `CombatPlanTests`의 `FakeCoreService` 하나다. `JobSystemTests`로 들어온 가짜는 쓰이지 않아 제거했다 | Core가 CoreGroup에 서비스를 더하면(Phase 5) `CombatPlanTests`의 가짜가 컴파일 오류를 낸다. 그 멤버를 `null`로 더하면 된다. 병합 전 계획 작성 때는 가짜가 없었다 |
| Runner는 예외 Job을 유지하고 Runtime Tick은 예외를 전파한다 | 실제 구동자는 예외 후 같은 Runtime을 다시 Tick하지 않아야 한다. Flow 테스트만으로 F1을 검증할 수 없다 |
| 루트 순서는 Global Runner → InGame View → LifeCycle Flow다 | 결과 보존과 연속 미리보기 테스트는 이 순서를 재현한다 |
| CombatLifeCycle은 기존 Factory와 Base를 통해 이미 수명에 등록된다 | Flow 연결만을 위해 Factory·asmdef·키를 추가하지 않는다. F2의 공개 연결 계약은 별도 확정한다 |

위 사실은 계획 작성 시점의 기준이다. 구현 재개 때 관련 파일의 변경을 확인하고, 다른 작업자의 수정이 있으면 보존한다.

## 3. 선행 계약과 인수 조건

2026-10-05 정상 F2 계약은 [설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05)가 원본이다. `CombatExit.RequestNormalExit(destination)`·`ICombatExitPresentation`·`CombatExitSequence`·Manager 전환 전 검사를 연결했다. Core 포기 API는 아직 미인수이며 가칭 구현으로 대체하지 않는다. 구현과 기존 검증 인수는 §6.13~14, 문서 인계는 §6.15다.

F 번호는 이 설계의 조건이다. Core 이전 계획의 G 번호와 혼용하지 않는다.

| 조건 | 받아야 할 내용 | 필요한 시점 | 현재 상태 |
|---|---|---|---|
| F1 | 예외 뒤 Runtime 재Tick·입력·정상 전환을 중단 | 6A·6B | `5078ee0e` 사용자 검증 완료 확인 인수. Core 답변 대기 아님 |
| F2 | 전환 소유자·공개 정지 경계, Job 종료, Completion 표시·정리 | Phase 6B | 정상 이탈 기반·일반 승리 호출자 연결. 포기 API는 Core 대기, 패배·최종 승리 목적지는 사용자 보류(설계 §6.4) |
| F3 — 읽기 계약 | 결과 순번·처리 종류·수락/거절·이유를 읽는 인터페이스와 값 타입의 이름·경로·어셈블리, 조회 방식 | **Phase 4 시작 전** | **인수(2026-09-30, Core `4c376244`)**: `CombatResult`·`CombatProcessingKind`, `ICombatResultReadSink` |
| F3 — 읽기 서비스 구현·등록 | 실제 인스턴스 제공, 서비스 키/그룹 노출과 Export, LifeCycle에서 조회 가능한 경로 | **Task 4.4 및 Phase 4 생산 연결 완료 전** | **인수**: `CombatStateBuffer`가 구현, `CombatStateBufferKey.Results`, `IBufferQuery.CombatResultReadSink` |
| F3 — 실제 기록·보존 | 한 처리 결과를 상태·순번과 함께 게시. View 확인·EndBattle·다음 준비 뒤 최신 결과/번호 유지 | Phase 5·6 | `83f485d1` 구현 인수. 실제 Flow 통합 검증은 Phase 5 |
| R6와 R1~R4 | 네 처리 API·CoreGroup 노출, 번호와 국면 갱신 | Phase 5 | Core 구현 인수, 네 Job 연결 완료(§6.8) |
| 준비 입력 | 테스트 정의 주입과 운영 DB 소스 구분 | Phase 5 / 운영 진입 | 테스트는 기존 `IBattleInitializationSource` 사용 가능. 운영 T1/G7은 차단 유지 |

F3의 세 행은 같은 계약의 **타입, 읽기 서비스 구현·등록, 실제 결과 기록을 구분한 것**이다. 저장소를 여럿 만드는 뜻이 아니다. 타입을 받으면 가짜를 주입하는 Flow 단위 작업을 준비할 수 있지만 Task 4.4까지 완료하려면 실제 읽기 서비스가 조회돼야 한다. 생성자 변경으로 기존 LifeCycle 호출을 깨뜨리지 않도록 필수 의존성 추가와 바인딩을 함께 반영한다. 타입만 있을 때 생산 코드를 통과시키려는 임시 생성자나 빈 결과 서비스는 추가하지 않는다.

합의된 API가 도착하면 문서에 실제 파일·멤버명을 적은 뒤 그 이름으로 구현한다. 서비스 노출이나 결과 저장 구조를 우리 쪽에서 추측해 만들지 않는다. 담당자의 문서 작성과 전달·답변 수신도 구분해 기록한다.

### 3.1 요청·인수 기록

> **2026-10-05 병합 인수:** Stage 브랜치에 `e0b02cc7`을 fast-forward했다. 6A/B3 진입 이전과 View A2, 경로 이동 통합이 포함된다. 골격 검증 인수는 §6.11, View 상태는 View 계획 §7.20이다. F2의 전환/포기/표시 계약과 운영 준비 소스·재화 조건은 아직 남아 있으며 §0.2에서 필요한 작업을 추적한다. 아래 날짜별 기록은 당시 인수 이력이다.

> **2026-10-03 진입 계약 인수:** `81c8c38`을 `491cae4d`로 병합했다. `Combat.EnterBattle(int chapterKey, StageNodeId targetNodeId)`의 수락은 Current/Character/Combat 공동 게시이며 Job 완료·씬/입력 활성화와 다르다. 초기 진행 거절은 소스 조회·진입 게시 없이 정상 반환하고, 잘못된 인자·데이터·보존 전투·처리 오류는 예외다. `PrepareBattle()`은 확정된 선택의 호환용이며 신규 첫/다음 진입에 쓰지 않는다. `IBattleInitializationSource.Read(database, chapterKey, stageKey)` 서명 호환은 반영했지만 실행 경로 이전은 6A 대상이다. Core 단독 검증과 V/D/A 완료를 구분한다.

> **2026-10-02 현재 인수:** `83f485d1`이 HEAD에 반영됐다. 실제 API와 수명은 아래 새 표가 현재 기준이며, 그 아래 날짜별 표는 당시 답변 이력이다. 후속 사용자 요청으로 Phase 5 코드를 반영했다. 검증 상태는 §6.8이며 외부 합의 완료를 뜻하지 않는다.

| 인수한 멤버 / 경로 | 의미 | 우리 구현·검증 |
|---|---|---|
| `CoreGroup.Combat.PrepareBattle()` | 정상 반환 시 전투 설치. 기본 `DatabaseBattleInitializationSource`는 T1/G7 차단 | 기존 준비 Job 유지, 명시적인 테스트 정의로 실제 준비 확인 |
| `ProcessNextCommand()` | 다음 명령 하나. true는 거절 포함 결과 게시, false는 처리/게시 없음 | CombatCommandJob 연결, 처리 번호·결과·연출 대기 검증 |
| `ProcessLatestPreview()` | 현재 미응답 요청 게시. false는 요청 없음/이미 응답 등 | CombatPreviewJob 연결, 교체·해제 및 결과 순번 불변 검증 |
| `AdvanceCombat()` | Automatic에서 기믹/다음 턴 시작/적 턴 한 단위 게시. false는 전투 없음/다른 국면 | CombatAdvanceJob 연결, 실제 진행 결과와 Flow 판정 검증 |
| `CombatStateBuffer`의 InstallCommand/InstallPreview | 상태·번호·최신 결과/미리보기 게시. Core는 입력 삭제·연출 확인을 하지 않음 | Runner → View 확인 → Flow 순서를 통합 테스트로 재현 |
| `CombatStateReadSink.Completion`, `AbandonBattle(loadout)`, `EndBattle()` | Completion 경로는 일반 CombatResult 발행과 다름. 정리 전 완료 기록 필요 | Phase 6: 설계 §6.2 계약을 인수한 뒤 연결 |

API 파일은 `RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs`이며 공개 인터페이스는 기존 `ICombatProcessingService`다. Factory·Buffer 등록은 이미 존재한다. `GetDrawAvailability`·`GetTimeline`도 도착했지만 View의 Buffer 읽기 경로 합의와 표시 범위 확장은 이번 Phase 5에 넣지 않는다.

> 2026-09-30 갱신: 사용자 지시로 `1a51234c` 병합 충돌을 해결했다. K2와 Plan 네 분기를 유지하고 준비 Job에 `CoreGroup.Combat.PrepareBattle()`를 연결했다. Phase 5 준비 부분만 앞당겼으며 Phase 4·5 전체 완료는 아니다. 검증·커밋 상태는 §6.5, 남은 Core 요청은 [전투 Core 요청 총정리](../handoffs/2026-09-30-combat-core-requests-consolidated.md)가 기준이다.
>
> 2026-09-30 Core 답변 수신: [Core 답변](../handoffs/2026-09-30-combat-core-response-to-view.md). K2 확정, 병합 방식 승인, 게임 규칙(서브 HP 복원, 자발적 포기 = 패배, 타임라인 10칸) 확정, F3 4단계 제공 예정, 나머지 API는 후속 계약. 아래 표의 Core 행을 갱신했다. Framework(F1·F2의 Framework 부분) 답변은 아직 없다.

| 항목 | 전달용 문서 | 외부 전달 | 응답 담당/날짜·합의 | 실제 파일·타입·멤버/등록 | 구현·검증 증거 |
|---|---|---|---|---|---|
| 공용 enum·테스트 변경 | [Framework §2](../handoffs/2026-09-30-combat-flow-framework-share.md#2-phase-1-공용-변경-예정), 준비 완료 | 미확인 | 미수신 | 예정 파일은 §4 | 코드 미착수 |
| F1 | Framework §3, 준비 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F2 | Framework §4 + [Core §13.4](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#134-f2-결과-확인전투-종료-수명-요청), 준비 완료 | Core: 전달됨. Framework: 미확인 | Core(천명님, 2026-09-30): 자발적 포기·로비 복귀 = 패배 확정. 호출 순서·호출자는 후속. Framework 미수신 | 미정 | 미확인 |
| F3 타입 | Core §13.3 → 총정리 §5.3 | 전달됨 | 천명님(2026-09-30): **제공**([Task 4A 인계](../handoffs/2026-09-30-combat-result-task4a-handoff.md), `4c376244`, 병합 `1a95f398`) | `CoreCaculator/Contracts/Combat/CombatResult.cs`(`ProjectL.Core.Contracts`): `CombatResult(Sequence, Kind, CommandNumber, Accepted, Reason)`, `CombatProcessingKind { Command = 1, Advance = 2 }` | Core 검증: 집중 EditMode 21개 통과(인계 §6). View 병합 뒤 검증은 §6.6 |
| F3 읽기 서비스 구현·등록 | Core §13.3 → 총정리 §5.3 | 전달됨 | 천명님(2026-09-30): **제공**(같은 인계) | `BufferSystems/Combat/CombatStateBuffer.cs`: `ICombatResultReadSink.TryGetLatest(out CombatResult)`(false = 결과 없음). 기존 `CombatStateBuffer`가 구현·Export, 키 `CombatStateBufferKey.Results`, 조회 `IBufferQuery.CombatResultReadSink` | Task 4.4 바인딩(§6.6) |
| 입력 없음 처리(Task 4A §5-1) | [View 답변](../handoffs/2026-09-30-combat-result-task4a-view-reply.md) §2 | 전달됨(사용자, 2026-09-30) | View 결정(K14): 실행 시점까지 입력 유지. 천명님(2026-10-01): **동의, §2.1 전제 확정**(호출 한 번 = 결과 하나, 사건은 결과 하나의 목록에). [Core 답변](../handoffs/2026-10-01-combat-core-reply-to-task4a-view-reply.md) | — | Flow 코드 변경 없음 |
| 결과 순번 두 곳·자동 진행 거절(확인 요청) | View 답변 §3.1·§3.2 | 전달됨(사용자, 2026-09-30) | 천명님(2026-10-01): **순번 원본은 발행 카운터**, 결과 있을 때 불일치는 오류. 결과 없음 + 양수 순번 정상. 정상 상황은 처리 완료 + 다음 국면, 거절은 계속 불가일 때만(View 정지 동의) | `LatestResultSequence`(원본), `CombatResult.Sequence`(같은 번호) | Phase 4 구현과 일치. 결과 없음 + 양수 순번 테스트 추가(§6.6) |
| 명령·미리보기·자동 진행 처리 API 일정 | — | — | 천명님(2026-10-01): 실제 구현은 Core **Task 7**. Task 5 완료만으로 Job 연결을 시작하지 않는다 | 미정(인계 §3 제안만) | Phase 5 착수 조건 조정 |
| F3 기록·보존 | Core §13.3 → 총정리 §5.3 | 전달됨 | 천명님: 3·4단계. CombatStateBuffer 우선 검토, 무제한 목록은 전제하지 않음 | 미정 | 미확인 |
| 소유 경계 K2·병합 방식 | 총정리 §2·§3 | 전달됨 | 천명님(사용자 승인, 2026-09-30): **확정·승인** | 병합 `02b9529a` | §6.5 |
| R6·R1~R4·준비 입력 출처 | Core §13.1~13.2 → 총정리 §5.1·5.2·5.8 | 전달됨 | 천명님(2026-09-30): 실제 API는 준비뿐, 나머지 멤버는 후속 계약. 번호는 건너뛸 수 있음. G7 일정 미정 | `CoreSystem/CombatSystem/CombatProcessingService.cs`: `ICombatProcessingService.PrepareBattle()` → `CoreGroup.Combat`. Factory에 서비스·모듈 등록. 운영 입력 G7은 미연결 | 준비 Job 연결·테스트 갱신. 병합 후 Unity 검증은 §6.5 |
| Stage 골드·Run 재화 분리(요청 3) | [Core 요청 2026-10-05](../handoffs/2026-10-05-combat-core-requests-phase6b.md) §1.1 | 전달됨 | 천명님(2026-10-06): **Stage 골드 분리·초기 지급 구현**, Run 시작 재화는 Excel DB 대기([Core 계획](2026-10-06-stage-gold-run-currency-split-plan.md)) | `CombatStateData.StageGold`(드로우 전용), `ICurrencyBufferReadSink` = Run 재화(상점·회복). `CombatViewSystem.BindServices`에서 재화 인자 제거, HUD는 `StageGold` | Core 병합 `459a69e3`, 사용자 확인: 병합 문제 없음(2026-10-06). 골격 코드 변경 없음 |

기록할 때에는 응답 내용뿐 아니라 제공된 경로·멤버, 등록 여부, 실제 검증 근거를 채운다. 문서를 썼다는 이유로 전달·합의를 완료로 바꾸지 않는다.

## 4. 변경 파일 지도

아래 `Runtime/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/`, `Tests/`는 `Assets/Tests/EditMode/Framework/`를 뜻한다.

| 파일 | 작업 | Phase |
|---|---|---|
| `Runtime/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | JobType 세 값 추가. 공용 파일 | 1 |
| `Runtime/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs` | Start와 새 시나리오를 해당 Job 하나로 매핑 | 1 |
| `Runtime/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatJob.cs` 및 `.meta` | CombatPrepareJob으로 이동·이름 변경 | 1 |
| 같은 `Jobs/CombatCommandJob.cs`, `CombatPreviewJob.cs`, `CombatAdvanceJob.cs` 및 `.meta` | 새 Job 골격, 이후 서비스 호출 연결 | 1, 5 |
| `Tests/CombatPlanTests.cs` 및 `.meta` | 구성·등록·서비스 주입과 미연결 실패 검증, 이후 연결 검증으로 갱신 | 1, 5 |
| `Tests/JobSystemTests.cs` | 일반 Job 시스템 표본을 Combat → Lobby로 교체. 공용 테스트 | 1 |
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs` 및 `.meta` | 1단계: 등록 판단·준비/명령/미리보기 확인·정지. 이후 자동 진행 등록·결과 확인 추가 | 2, 4 |
| `Tests/CombatFlowTests.cs` 및 `.meta` | 가짜 상태를 주입한 진행 정책 검증. 이후 가짜 결과 읽기로 자동 진행 검증 추가 | 2, 4 |
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs` | 1단계 훅 연결(이탈 미완성). 이후 결과 읽기 바인딩, 이탈 경계 연결 | 3, 4, 6 |
| `Tests/CombatFlowIntegrationTests.cs` 및 `.meta` | 실제 Register·Plan·Runner·서비스 연결, 이후 수명 경계 검증 | 5, 6 |
| `docs/superpowers/handoffs/2026-09-30-combat-flow-framework-share.md` | 공용 변경 예정/결과와 F1·F2 협의 내용. 새 파일 | 0, 7 |
| `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` | Core/Flow 소유 경계, R6·F3·이탈 정리 요청 절 추가 | 0, 7 |
| 설계 문서·이 계획 | 합의·검증·남은 일의 실제 상태 기록 | 0, 각 Phase, 7 |
| `docs/superpowers/handoffs/2026-09-30-combat-flow-local-session-handoff.md` | 완료 범위와 후속 View 작업 인계. 새 파일 | 7 |

`CombatPlanTests.cs`는 설계 §11의 Plan 검증을 별도 파일로 배치한 것이다(설계 §10에도 반영). 테스트 파일 분리는 기존 테스트 어셈블리 안에서 한다.

**이 계획에서 구현하지 않는 것:** Core 처리 서비스와 Buffer 기록, F1 구동자 중복 구현, 미합의 F2 API의 임의 추가, F3 생산 타입/저장 구조 수정, 부트스트랩·씬 구성·View 모듈·입력 Layer 전환·결과 화면·FieldCamera·Architecture. F1 구현은 View 계획에서, F2 전환 소유자·공개 API와 실제 파일은 담당 협의 뒤 §3과 Phase 6.0b에서 확정한다. 그 계약을 연결하고 이탈 순서를 검증하는 작업은 Phase 6에 포함한다. 추가 공용 수정이 필요하면 먼저 범위를 갱신한다.

## Phase 0 — 기준과 공유 문서 준비

**목표:** 코드 변경 전에 공용 변경과 외부 의존성을 보이게 만든다. F1~F3의 답변 수신은 Phase 0의 문서 작성 완료와 별도로 추적한다.

- [x] **0.1 현재 기준 기록.** 브랜치·HEAD·작업 트리, JobType 숫자, GUID, 현재 서비스/결과 계약을 재확인했다(§6.1). 기준 실행 항목을 사용자에게 요청했다. 사용자 답변은 "지금 컴파일 에러 없음"이며 테스트 결과는 미수신으로 기록했다. 체크는 기준 확인·요청·상태 기록 완료를 뜻하며 테스트 통과를 뜻하지 않는다.
- [x] **0.2 Framework 공유 문서 작성.** 새 전달 문서에 enum 세 값, JobSystemTests 표본 교체, 기존 Runner 유지, F1·F2 인수 조건과 답변 표를 작성했다. 외부 발송·담당자 합의는 하지 않았다.
- [x] **0.3 Core 전달 문서 보충.** 기존 문서 §13에 준비까지 포함한 Flow·Plan·Job 소유 경계, 네 서비스/R1~R4, F3 타입·읽기 서비스 등록·결과 보존, F2의 마지막 결과·EndBattle·중도 이탈 수명을 요청으로 적었다.
- [x] **0.4 합의 기록 표 준비.** §3.1에 전달·응답·실제 API·등록·검증을 분리해 추적할 표를 마련했다. 설계·계획의 K9~K11·미리보기·정상 Stop 규칙을 대조하고, 비활성 LifeCycle의 바인딩과 F3 서비스 등록 선행 조건을 보완했다.

**완료 조건:** 공용 변경 범위와 외부 요청이 문서로 준비돼 있고, 승인/응답을 받지 않은 항목을 완료로 표시하지 않았다.

**보고:** 작성한 두 전달 문서, **사용자에게 요청한 기준 컴파일·테스트 항목**과 받은 결과 또는 미실행 사유, 현재 F1~F3 상태. 다음 Phase의 파일·Job 이름 변경·테스트 표본 교체와 에디터 import 확인을 설명하고 멈춘다.

## Phase 1 — Plan·Job 골격과 기존 테스트 정렬

**목표:** Core 구현 없이도 요청 종류가 올바른 Job으로 구성되고 미연결 원인이 드러나도록 만든다. 아직 `CombatLifeCycle.OnEnter`에서는 등록하지 않는다.

- [x] **1.1 JobType 추가.** `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5`를 추가한다. 기존 값을 변경하지 않는다. 다른 Plan의 알 수 없는 타입 거부는 유지한다.
- [x] **1.2 Job 네 개 구성.** 기존 CombatJob과 `.meta`를 CombatPrepareJob으로 옮긴다. 명령·미리보기·진행 Job을 추가한다. 생성자에서 서비스 null을 거부하고 Priority를 보존하며, 초기 `IsCompleted`는 false다. Execute는 설계의 미연결 예외를 던진다.
- [x] **1.3 CombatPlan 매핑.** None은 빈 목록, Start/ProcessCommand/ProcessPreview/AdvanceCombat은 해당 Job 하나를 새로 만든다. 모든 Priority는 0이다. 서비스와 JobType으로 목록을 만드는 기존 서명은 유지한다. Plan에서는 Execute하지 않는다.
- [x] **1.4 JobSystemTests 표본 교체.** 일반 등록·서비스 주입·반복 생성·Tick·Shutdown 검증을 LobbyJob 기준으로 바꾼다. `NonStagePlans_…` 목록에서 Combat을 제외한다. 전투 전용 실패를 숨기지 않도록 다음 Task의 전투 검증을 함께 추가한다.
- [x] **1.5 CombatPlanTests 작성.** 아래 관찰 가능한 계약을 확인한다. 실제 JobRegisterSystem이 올바른 Plan으로 보내는 경로도 확인한다.
- [x] **1.6 Unity import·검증.** 새 `.meta`와 이동 GUID를 확인하고 컴파일, CombatPlanTests, 변경된 JobSystemTests를 실행한다. 실패하면 해당 Phase에서 해결하거나 외부 선행 실패를 명시한다.

| 검증 | 기대 |
|---|---|
| 시나리오별 구성 | None 0개, 나머지는 맞는 타입 1개·Priority 0·미완료 |
| 등록과 실행 구분 | Register만 호출했을 때 Core 호출이나 미연결 예외가 발생하지 않음 |
| 서비스 전달·독립 생성 | 주입한 서비스가 Job에 전달되며 반복 등록으로 새 Job이 만들어짐. 현 단계는 기존 테스트의 최소한의 필드 확인 방식을 재사용 가능 |
| 잘못된 요청 | null 서비스 및 알 수 없는 JobType을 기존 예외 규약으로 거부 |
| 미연결 Execute | 네 Job 모두 명시적 미연결 예외, IsCompleted는 false |
| 실제 Runner 경로 | Combat Start 등록 뒤 Tick에서 미연결 예외가 전파됨. 테스트 종료 시 Runner를 정리. 이 검사를 F1 재실행 중단 보장으로 보고하지 않음 |
| 공용 회귀 | Lobby 표본과 기존 Runner 우선순위·완료·Clear 테스트 통과 |

**보고:** 변경 파일·GUID·enum 값·Plan 매핑·컴파일과 테스트 결과, Framework 공유 내용. 다음 Phase 2(Flow 1단계)의 파일·분기 순서·테스트를 설명하고 멈춘다.

## Phase 2 — CombatFlow 1단계(F3 무관 부분)와 단위 검증

**목표:** F3 없이 만들 수 있는 Flow 전부를 구현한다(K9). 준비·명령·미리보기의 등록과 확인, 연출 대기, 정지, Reset이다. `Automatic` 국면에서는 등록하지 않고 기다린다.

- [x] **2.1 의존성·내부 상태.** 상태 읽기, 입력 읽기, Runner, Register를 생성자로 받는다. **결과 읽기(F3)는 받지 않는다.** 필요한 계약만 보관하고 null을 거부한다(`ArgumentNullException`). 확인 종류와 그에 필요한 번호 하나, 정지 상태만 둔다.
- [x] **2.2 Reset·Stop.** Reset은 이전 확인 조건과 정지를 초기화한다. Stop은 반복 호출해도 안전하게 새 등록만 막는다. 정상 Stop에는 LogError가 없고 Buffer/Runner를 비우지 않는다. 실패 감지 경로는 원인을 한 번 기록하고 정지한다.
- [x] **2.3 BeginPreparation.** 다음 네 조건을 확인한다: ① 멈추지 않음(Reset으로만 풀림) ② Runner Idle ③ `!HasBattle` ④ 진행 중인 확인 조건 없음(준비 대기 중복 포함). 하나라도 어기면 **아무것도 등록하지 않고 `InvalidOperationException`을 던진다(K11).** 통과하면 준비 확인을 기록하고 Start를 한 번 등록한다. 실제 상태 설치는 Core가 맡는다.
- [x] **2.4 Tick 분기.** 아래 순서를 적용한다. 준비·명령·미리보기의 완료 확인은 다음 등록 전에 끝낸다. 새 등록 뒤에는 즉시 반환한다.
- [x] **2.5 CombatFlowTests.** 실제 CombatViewBuffer, 가짜 상태 읽기, 기록 가능한 가짜 Runner·Register로 아래 시나리오를 검증한다. 테스트 보조 타입은 테스트 파일 내부에 둔다.
- [x] **2.6 컴파일·검증.** Unity 컴파일과 CombatFlowTests를 실행한다. 예상된 실패 로그는 `LogAssert.Expect`로 확인하고, 정상 이탈 Stop에서 오류 로그가 없는 것도 확인한다.

Tick의 실행 순서(1단계):

1. 정지 상태면 종료한다.
2. Runner가 일하는 중이면 기다린다.
3. 앞서 등록한 처리의 완료 조건을 확인한다. 실패면 한 번 보고하고 정지한다.
4. 전투가 없으면 아무것도 등록하지 않는다.
5. PlayerInput + 연출 완료 + 미처리 명령이면 ProcessCommand를 등록한다.
6. PlayerInput + 새 미리보기 요청이면 ProcessPreview를 등록한다.
7. 연출이 남았으면 기다린다.
8. **Automatic이면 기다린다(1단계).** Phase 4에서 AdvanceCombat 등록으로 바꾼다.
9. 그 밖은 등록하지 않는다.

| 시나리오 | 기대 |
|---|---|
| 준비 선행 조건 위반 | Runner Busy / 전투 있음 / 확인 대기 중이면 `InvalidOperationException`, 등록 없음 |
| 준비 중 일반 입력 도착 | Start만 등록되고, Runner 완료와 HasBattle 확인 전 일반 작업 등록 없음 |
| 준비 정상/잘못된 반환 | HasBattle true면 계속, false면 한 번 보고하고 정지 |
| Runner Busy | 추가 등록 없음. 같은 명령의 중복 등록 없음 |
| 명령·미리보기 동시 대기 | 연출 완료면 명령 우선, 연출 중이면 미리보기만 허용 |
| 연속 미리보기 | Runner → View 새 요청 → Flow 순서를 반복해도 연출 완료 후 대기 명령이 밀리지 않음 |
| 명령 결과 | 처리 번호가 기대 번호 이상이면 계속. 거절도 처리로 인정. 번호가 그대로면 정지 |
| 미리보기 결과 | 응답 번호가 기대 번호 이상이면 계속. 현재 요청이 교체·비워졌으면 정지하지 않음. 같은 요청에 응답이 없으면 정지 |
| Automatic(1단계) | 등록 없음, 정지 없음. 국면이 PlayerInput으로 바뀌면 정상 판단으로 돌아옴 |
| Finished·None·전투 없음 | 추가 등록 없음 |
| 정상 Stop·실패 정지·Reset | 정상 Stop은 오류 없음, 실패는 한 번 보고, 둘 다 이후 등록 없음. 새 전투의 Reset 뒤 다시 시작 가능 |
| 미리보기 재요청 계약 | 테스트가 View 역할로 상태 변경 후 ClearPreview/SetPreview를 수행하면 새 번호가 처리됨. 이것을 실제 View 구현 검증으로 보고하지 않음 |

**보고:** Flow의 내부 상태와 읽기 의존성, 등록 순서, 테스트 결과. `Automatic`이 1단계에서 대기만 한다는 점과 미연결 Job이 그대로라는 점을 기록한다. 다음 Phase 3(훅 연결)을 설명하고 멈춘다.

## Phase 3 — CombatLifeCycle 훅 1단계 연결

**목표:** Flow와 Plan을 LifeCycle 훅에 연결한다(K10). 실제 전투 진입 경로는 활성화하지 않지만 서비스 바인딩은 Runtime의 공통 경로에서 실행되므로 기존 조립을 확인한다. **이탈은 미완성**으로 표시하며 F2 전환 요청 경계는 만들지 않는다.

- [x] **3.1 OnBindServices.** 기존 Buffer·Job 컨텍스트에서 상태 읽기, 입력 읽기·쓰기, Runner, Register를 받아 Flow(1단계 생성자)를 만든다. 입력 쓰기는 `CombatViewWriteSink`만 보관한다. 기존 Factory의 LifeCycle 등록을 재사용한다. 미등록 서비스는 초기화 오류로 드러내고 빈 구현으로 대체하지 않는다.
- [x] **3.2 OnEnter.** 입력 Clear → Flow Reset → BeginPreparation 순으로 연결한다. 준비 데이터를 여기서 조립하지 않는다. F2의 "이전 구간 정리 완료" 선행 조건은 아직 보장되지 않으며, BeginPreparation의 K11 검사가 최소한의 보호다.
- [x] **3.3 OnTick.** Flow.Tick을 호출한다. LifeCycle에 별도의 국면 분기·자동 진행·Core 직접 호출을 추가하지 않는다.
- [x] **3.4 OnExit(미완성).** Flow Stop → 입력 Clear만 한다. Runner를 비우지 않는다(D7). 코드 주석과 문서에 "F2 전 이탈 미완성: 이미 등록된 Job 종료를 보장하지 않음"을 적는다.
- [x] **3.5 컴파일·검증.** Unity 컴파일, Phase 1~2 테스트, 기존 FrameworkRuntimeCompositionTests의 실제 PostBindService 사례를 확인한다. 코드 리뷰로 훅이 Flow·Plan 호출만 하는지 확인한다. 별도 훅 테스트는 Phase 6에 두며 가짜 로딩 서비스/콜백으로 씬 없이 검증할 수 있다. 로딩 서비스가 필요하다는 이유로 바인딩 검증이 불가능하다고 보지 않는다.

**보고:** 연결한 훅과 이탈 미완성 표시 위치, 컴파일 결과. 여기까지가 외부 조건 없이 가능한 범위임을 적는다. 다음 Phase 4는 F3 타입이 있어야 시작할 수 있으므로, 타입이 없으면 기다리는 이유와 필요한 계약(§3 F3 읽기 계약)을 설명하고 멈춘다.

## Phase 4 — 자동 진행 등록·결과 확인(F3 연결)

**착수 조건:** Flow 단위 작업(4.0~4.3·4.5)은 Core 답변 §5의 **1단계(최소 읽기 계약)**, LifeCycle 바인딩(4.4)은 **2단계(생산 조회 등록)**를 받은 뒤 시작한다. Flow 단위 작업에는 F3의 최소 읽기 인터페이스·값 타입이 합의돼 생산 어셈블리에 정의돼 있어야 한다. **생산 연결 완료에는 Task 4.4가 조회할 실제 읽기 서비스 구현·등록도 필요하다.** 결과 생산 로직은 Phase 5에서 검증한다. 타입만 확보했으면 단위 변경·테스트를 준비하되 필수 생성자 변경과 LifeCycle 바인딩은 함께 반영해 기존 조립을 유지한다. 임시 빈 서비스나 생산 코드의 테스트 타입 의존으로 이 조건을 우회하지 않는다.

> 2026-09-30 착수: Core Task 4A가 타입(1단계)과 실제 조회 등록(2단계)을 함께 제공했다. 두 착수 조건이 모두 충족돼 4.0~4.5를 한 번에 반영했다(§6.6).

- [x] **4.0 F3 타입·등록 조건 인수.** 실제 파일·네임스페이스·읽기 API와 Task 4.4에 필요한 구현·등록 경로를 §3에 각각 기록한다. 가칭 이름을 그대로 새로 선언하거나 테스트 타입에 생산 코드를 의존시키지 않는다. 타입 추가 자체의 담당자는 합의된 소유자를 따른다.
- [x] **4.1 Flow 의존성 추가.** 생성자에 F3 결과 읽기를 더하고 null을 거부한다. 자동 진행 확인에 필요한 기준 순번을 확인 조건에 더한다.
- [x] **4.2 Tick 8행 교체.** `Automatic`이면 등록 때의 최신 결과 순번 S를 기록하고 AdvanceCombat을 등록한다.
- [x] **4.3 자동 진행 결과 확인.** S 이후의 해당 처리 결과가 수락일 때만 계속한다. 거절·누락·종류 불일치·순번 미갱신은 원인을 한 번 보고하고 정지한다.
- [x] **4.4 LifeCycle 바인딩.** 실제 읽기 서비스 구현·등록을 확보한 뒤 `OnBindServices`에서 조회해 Flow에 넘긴다. 전투 비활성 상태의 Runtime.PostBindService도 성공해야 한다. 타입만 있고 조회할 서비스가 없으면 이 Task는 대기이며 Phase 4 완료로 표시하지 않는다.
- [x] **4.5 테스트 보강·검증.** CombatFlowTests에 가짜 결과 읽기를 더하고 아래 시나리오를 추가한다. Phase 2의 `Automatic(1단계)` 시나리오는 이 Phase의 기대로 바꾼다. Unity 컴파일, CombatFlowTests와 FrameworkRuntimeCompositionTests의 바인딩 사례를 실행한다.

| 시나리오 | 기대 |
|---|---|
| 자동 진행 등록 | Automatic + 연출 완료면 AdvanceCombat 하나 |
| 자동 진행 결과 | 기준 순번 이후의 해당 처리 결과가 수락일 때만 계속. 거절·누락·종류 불일치·순번 미갱신은 정지 |
| 거절 뒤 재등록 없음 | View가 거절 결과를 확인한 뒤에도 AdvanceCombat을 다시 등록하지 않음 |
| 결과 조회 수명 | View 확인 뒤 Flow가 최신 결과를 조회하는 순서를 모사. 실제 저장소의 보존 검증은 Phase 5에서 수행 |

**보고:** 인수한 F3 타입, Flow 변경, 테스트 결과. 다음 Phase 5에 필요한 네 서비스·그룹 노출·결과 기록을 구체적으로 설명하고 멈춘다.

## Phase 5 — Core 서비스 연결과 처리 경로 검증

> 병합 선반영(2026-09-30): 사용자 지시로 5.1·5.2의 준비 Job 부분만 `1a51234c` 통합에 포함했다(§6.5). 2026-10-02에는 나머지 세 Job과 테스트를 작성했다(§6.8). 아래 구현·작성 완료 표시는 Unity 실행 통과를 뜻하지 않으며 5.4·5.5는 결과 수신 후 완료한다.

**착수 조건 충족(2026-10-02):** R6·R1~R4·F3 발행 및 명시적 준비 입력 주입 자리를 인수했다. 운영 소스 T1/G7 성공은 별도다. 테스트는 기존 `IBattleInitializationSource`·DB query fixture로 실제 Core를 조립한다. 준비·명령에 필요한 유닛/구조물/카테고리·공격 프로필·경제 정의와 로스터·진행 선택·재화 전제를 명시하며 Core가 상태를 계산·게시하게 한다. 가짜로 번호만 올린 테스트를 실제 처리 증거로 삼지 않는다.

**범위 확인(K19):** 실행 모드·공유 오류 검사의 규칙은 골격 설계 §4.1이 원본이며 구현은 View B다. Phase 5에서 Flow 생성자·분기·Factory를 확장하지 않는다.

- [x] **5.B 병합 기준 검증.** `CombatPlanTests`, `CombatFlowTests`, `JobSystemTests`, `CharacterBattlePreparationTests`, `CombatCommandPublicationTests`, `CombatCommonPublicationTests`, `CombatPreviewPublicationTests`, `FrameworkRuntimeCompositionTests`를 실행한다. 실제 에셋 검증은 `DataBaseAssetTests`·`ArtifactDataTableTests` 결과를 별도로 기록한다. Core 인계의 Economy 5건은 참고 기록이며 로컬 결과를 확인하기 전에 동일 실패로 단정하지 않는다.

- [x] **5.0 인수 계약을 코드와 대조.** §3.1·설계 §5.2의 실제 멤버를 사용한다. 번호가 건너뛸 수 있다는 점과 명령 없음 반환이 Flow 확인 조건과 맞는지 확인한다. bool true는 명령 처리 여부이며 게임 규칙 수락과 다름을 확인한다. 자동 진행은 F3의 수락/거절을 사용한다. 새 가칭 API를 만들지 않는다.
- [x] **5.1 세 Job 몸통 연결.** Command → `ProcessNextCommand()`, Preview → `ProcessLatestPreview()`, Advance → `AdvanceCombat()`. **정상 true/false 모두 IsCompleted = true**, 예외에서는 미완료·원래 예외 전파. 완료 후 Execute는 재호출하지 않는다. 준비 Job·Plan 네 분기·Priority 0·공용 Runner는 유지한다.
- [x] **5.2 골격 테스트 갱신.** CombatPlanTests의 세 미연결 기대를 호출·완료·예외 검증으로 바꾼다. 가짜에 각 메서드별 횟수·반환·예외 주입을 둔다. 정상 false에서도 Runner가 Idle로 돌아오고 재호출하지 않는지, 예외 때 Job 미완료와 원인 보존을 확인한다. 서비스 등록만으로 호출이 발생하지 않는 검증과 공용 Lobby 표본은 유지한다. 테스트 작성 완료, 실행 대기.
- [x] **5.3 CombatFlowIntegrationTests 추가.** 실제 JobRegisterSystem·CombatPlan·JobRunnerSystem과 Flow를 연결한다. 제공된 Core 서비스와 실제 Buffer로 상태·결과 기록을 확인하는 사례를 포함한다. 계산 입력은 명시적인 테스트 fixture를 사용하며 옛 Core 직접 호출 경로를 만들지 않는다. 테스트 작성 완료, 실행 대기.
- [ ] **5.4 결과 보존·예외 확인.** View 역할의 결과 확인·정리 이후에도 F3 최신 결과가 조회되는지 실제 결과 구현으로 확인한다. Core 예외는 전파되고 완료로 바뀌지 않아야 한다. F1이 아직 없으면 이 테스트는 catch/finally에서 정리하고 생산 예외 복구 검증은 Phase 6으로 남긴다.
- [ ] **5.5 검증.** Unity 컴파일, 수정한 Plan/Job 테스트, Flow 단위 및 추가된 통합 테스트를 실행한다. Core fixture·생산 DB 문제를 우리 골격의 성공으로 덮지 않는다.

| 통합 사례 | 실행과 기대 |
|---|---|
| 준비 | Flow.BeginPreparation → 실제 Register/Plan/Runner/Core → HasBattle. 준비 확인 전 일반 Job 없음 |
| 명령 수락·거절 | 실제 명령 처리 후 번호와 최종 결과 한 개. 거절도 완료하고 View 확인 전 다음 상태 변경 없음 |
| 입력 없음 / 등록 뒤 제거 | 입력 없음은 등록 0. 등록 후 제거는 K14 위반 주입: Job 정상 false·Idle 복귀 후 Flow 오류 1회·재등록 0 |
| 미리보기 교체·해제 | 실행 직전 교체/해제, 응답 번호와 요청 동일성. false라도 요청 해제는 정상. 게임 상태·결과 순번 불변 |
| 자동 진행 | 실제 EnterCombat 이후 Advance 결과의 종류·순번·수락·국면 확인. 거절·누락·불일치는 실패 주입 대역으로 별도 검사하며 실제 Core 정상 경로와 구분 |
| 처리 순서·보존 | Runner → View 역할의 표시 완료/입력 정리/확인 → Flow. 확인 뒤 최신 결과가 읽히며 표시 지연 중 상태 변경 등록 0 |
| 종료·다음 준비 데이터 수명 | Core를 통해 정상 완료한 뒤 EndBattle/다음 준비. 확인된 최신 결과·번호 유지, Completion은 다음 준비 때 해제. F2 씬 이탈 검증과 구분 |
| 예외 | 원래 예외, Job 미완료. 테스트가 finally에서 정리하며 F1 생산 구현의 성공으로 주장하지 않음 |

Core 서비스의 실제 상태 기록을 확인하지 못했다면 **호출 연결 확인**과 **실제 처리 검증**을 별도 상태로 보고한다. 추가 파일은 `CombatFlowIntegrationTests.cs`와 `.meta`이며 기존 Framework 테스트 어셈블리를 사용한다. 생산 DB·씬·Core 소유 코드 변경은 이 Phase에 넣지 않는다.

**보고:** 실제 API 이름·파일, 네 Job 연결 결과, 호출 횟수와 Buffer 기록 검증, 미해결 Core/DB 의존성. 다음은 F1·F2가 필요한 수명 통합이며, 이 단계까지도 실제 전투 씬 진입을 활성화하지 않는다.

## Phase 6 — 진입 이전·실패·이탈 수명 통합

### Phase 6A — 명시적 전투 진입 계약 이전

**착수 조건:** 병합된 Core 진입 계약과 기존 Phase 5. [설계 §6.3](../specs/2026-09-30-combat-flow-skeleton-design.md#63-명시적-전투-진입-계약-k20) 및 Core 인계 V1~V5를 따른다. 실제 DB 준비와 F2 전체 완료는 착수 조건이 아니다. View B3는 같은 진입 경로의 호출자 이전이며 통합 완료 전에 반드시 연결한다.

- [x] **6A.0 경계·전달 방식 확정.** 챕터/노드 의도의 보관 수명, Register/Plan/Job으로 전달하는 좁은 계약, 요청과 결과의 대응, 결과 수신자, 준비 중 Tick을 유지할 호출자와 활성화 시점을 확정한다. Combat OnEnter 이전에 Job을 실행하는 경우 비활성 CombatFlow의 OnTick에 의존하지 않는다. 씬을 먼저 로드하는 경우에도 수락 전 입력/일반 처리를 막는다. 기존 Framework 등록 경로·순수 값/좁은 포트·수명 소유자를 검토하고 설계와 실제 API를 기록한다. 합의되지 않은 타입명을 이미 제공된 API처럼 적지 않는다.
- [x] **6A.1 대상 전달(V1).** 현재 챕터와 클릭/개발 설정의 노드 의도를 캡처해 실행까지 유지한다. Current의 확정 선택을 보관소로 쓰거나 실행 시점의 다른 클릭을 읽지 않는다. 기존 명령/미리보기 Buffer를 진입 요청 큐로 확장하지 않는다. 공용 Register/Plan 변경은 기존 Stage/Lobby 등 호출자 호환을 검증한다.
- [x] **6A.2 진입 Job·결과(V2).** `EnterBattle(chapterKey, nodeId)`를 한 번 호출한다. 수락/정상 거절 모두 결과를 전달한 뒤 Job을 한 번 완료하고, 예외는 완료로 바꾸지 않고 F1에 전파한다. 수락 결과를 이번 요청과 대응시켜 활성화한다. `HasBattle`·보존 CombatResult/순번만으로 진입 성공을 추측하지 않는다. 정상 거절은 자동 재시도·전역 오류 정지 없이 요청자에게 반환한다.
- [x] **6A.3 LifeCycle·View B3 연결(V3).** 수락된 전투에 `OnEnter → BeginPreparation → PrepareBattle()`이 재실행되지 않게 한다. Bootstrap의 Combat `ConfirmStageEntry`를 제거하고 로스터/챕터 준비 뒤 동일한 진입 요청을 전달한다. Live/RecordInputOnly 모두 유효한 챕터·대상을 사용하되 기록 모드는 수락 후 처리 Job 등록 0을 유지한다. 입력 정리 시점·초기 그리기·입력 허용 순서를 View B3와 맞춘다. 회복/상점 등의 기존 확정 경로와 배치→전투 `EnterCombat` 명령은 구분한다.
- [x] **6A.4 취소·교체·실패(V4).** 등록 전/후 요청 취소·교체의 처리와 결과 수신 수명을 정의한다. 오래된 요청 자동 재시도, 이미 실행/수락된 요청의 조용한 재실행, 임의 Core reset을 금지한다. 수락 후 활성화 실패는 기존 F1 및 명시한 수명 정책으로 처리하고 운영 재진입은 F2와 연결한다. 이미 등록된 작업의 입력을 조기에 지우거나 Global Runner를 무조건 Clear하지 않는다.
- [x] **6A.5 단위·공용 회귀.** 요청 캡처, 수락/거절/예외, 완료 1회, 중복 등록 차단, 실패 후 자동 재시도 없음, 기존 세 처리 Job 및 등록 호출자 회귀를 검사한다. 기존 `CharacterBattlePreparationTests`의 호환 Prepare 검증과 신규 진입 검증을 구분한다. 병합본 테스트 코드와 전체 실행 기록을 대조해 인수(§6.11); 이번 세션에서 재실행한 것은 아니다.
- [x] **6A.6 실제 실행 경로(V5).** 실제 Register → Plan → Runner → Job → Core → Buffer → Flow로 서로 다른 첫/다음 대상을 검증한다. 정상 거절, 예외, 요청 교체/취소, 중복 요청, LifeCycle 활성화 정확히 한 번, 기록 모드 처리를 포함한다. 다음 대상 검증은 정상 정리된 전투를 전제로 하며 Core를 테스트에서 정리한 결과를 운영 씬 이탈 완료로 보고하지 않는다. 병합본의 `CombatEntryIntegrationTests`와 전체 실행 기록을 인수했다(§6.11). 서비스 직접 호출/가짜 Job만으로 완료하지 않는다.
- [x] **6A.7 B3 인계·범위 보고.** 실제 진입 계약·호출자·수정 파일·테스트 결과를 기록하고 View B3 결과를 링크한다. Core gate V의 실제 실행 증거와 D/A·F2 미완료 조건을 구분한다. 골격만 연결하고 B3가 빠졌으면 6A를 부분 완료로 남긴다.

**예상 수정 범위:** `JobRegisterSystem.cs`, `CombatPlan.cs`, `CombatPrepareJob.cs`, `CombatFlow.cs`, `CombatLifeCycle.cs`, 필요한 좁은 진입 계약/기존 실행 호출자, 관련 테스트. `GameBootstrap.cs`·개발 설정·필요 시 View 입력 활성화 연결은 B3와 담당을 정해 함께 이전한다. Core 계산·Buffer 게시를 재구현하지 않는다.

**남은 마감:** 6A.7에서 View B3의 잔여 재생·인계를 연결한다. 이후 통합 순서는 §0.1을 따른다. 각 증거는 해당 계획의 실행 기록에 한 번 적고 다른 문서는 링크한다.

### Phase 6B — F1 인수·실패·이탈 수명 통합

아래 6.0a~6.5 번호는 기존 참조를 유지한다. 6A의 진입 수락과 한 번의 활성화를 전제로 읽는다.

**착수 조건:** 6A의 진입 이전 뒤 F1·F2를 나눠 인수한다. 기존 F1은 6A에서도 사용하며 6B에서 실제 수명 증거를 검증한다. F2는 설계 §6.2의 공개 전환 경계·포기 입력·Completion 표시 계약 확보 후 구현한다. 전체 Phase 6 완료에는 F2까지 필요하다. 테스트 View의 훅 검증을 운영 표시·이탈 완료로 보고하지 않는다.

**진행 단위:** 6.0a·6.3의 F1은 사용자 검증 완료 확인으로 인수했다. 6.0b·6.1·6.2는 정상/포기 범위를 구분하며 최신 구현은 §6.14다. 일반 승리 복귀 호출자는 연결했으며, 패배·최종 승리 목적지는 보류하고 포기는 Core API 인수 전까지 남긴다. 전체 통합 인수는 6.4·6.5를 따르며 운영 DB 인수는 독립적으로 진행한다.

- [x] **6.0a F1 인수.** View B·C·E의 실제 파일·입력 예외 처리·최초 오류 1회·재Tick/전환 차단·Shutdown 증거를 기록한다. 경로 Drag·Return·시간 기반 이동 연출과 Detach 중 실패를 기존 테스트/병합 증거에 대응시키고 빈 곳만 보강한다(View E.8·X3). 골격에 두 번째 전역 오류 플래그·별도 Bootstrap을 만들지 않는다. Flow의 로컬 Stop은 그대로 별개다.
- [ ] **6.0b F2 계약·파일 확정.** 전환 요청 소유자와 공개 정지 경로, Core가 포기 입력을 조립하는 실제 API, Completion 표시 완료, EndBattle 호출자를 §3에 기록한다. Core 요청은 전달 문서로 준비하며 View가 ArtifactLoadoutSnapshot을 조립하는 우회는 금지한다. 공개 계약을 받기 전 가칭 서비스 구현을 만들지 않는다.
- [x] **6.1 진입·OnEnter 선행 조건.** 이전 구간의 작업/전투 정리를 확인한 뒤 새 진입 요청을 등록한다(K11). 수락 후 활성화에는 준비된 전투가 있어야 하며 `!HasBattle` 등록 조건을 다시 적용하지 않는다. 6A의 중복 준비 방지와 F2 정리 조건을 함께 검증한다. 기존 커밋 인수와 일반 승리 자동 복귀 뒤 다음 대상 회귀는 §6.14.
- [ ] **6.2 정상·포기 이탈 연결.** 새 Flow 등록/입력 제출을 중단하되 기존 입력과 진행 중 연출 완료 예약은 보존한다. 오류·Detach용 StopInput과 구분한다. Runner Idle → 기존 CombatResult 표시·확인 → 필요 시 Core 포기 기록 → Completion 표시 완료 → EndBattle → 정리 확인·최종 Idle → Change/Exit. 이미 정상 종료된 결과를 포기로 덮지 않는다. 포기는 새 결과 순번·Finished 전환을 기다리지 않고 Completion을 확인하며 확인 번호를 위조하지 않는다. OnExit는 최종 Stop·Clear만 맡는다.
- [x] **6.3 예외·Runtime 종료 경로.** 실제 F1 구동 경계가 예외 뒤 같은 Runtime을 재Tick하지 않는지 확인한다. 정상 이탈 절차를 치명적 실패 정리에 강제 적용하지 않는다. 정상 Shutdown·반복 정리에서도 취소된 Job을 다시 실행하지 않는다.
- [ ] **6.4 LifeCycle·수명 통합 검증.** 실제 Runner와 F1·F2 구현을 대상으로 아래 사례를 검사한다. 씬 없이 확인하는 훅 테스트에는 가짜 로딩 서비스와 등록된 enter/exit 콜백을 사용한다. 실제 경계가 없는 테스트용 가짜 소유자로 통과한 결과를 F1·F2 구현 검증으로 대신하지 않는다.
- [ ] **6.5 Unity 컴파일·필요 회귀.** Phase 1~6의 관련 테스트를 실행한다. 새 실패나 공용 의존성 변경이 없으면 무관한 대규모 테스트를 반복하지 않는다. 실제 카탈로그·재생 검증 여부는 별도 기록한다.

| 검증 | 기대 |
|---|---|
| 진입부터 첫 처리 | 이번 요청의 수락 결과 확인·Job 완료·LifeCycle 활성화가 한 번씩 이루어진 뒤 일반 처리 시작. HasBattle만으로 활성화하지 않음 |
| Job 등록 직후 이탈 | ProcessCommand와 AdvanceCombat 각각에서 기존 Job 종료 전 전환 없음, 입력 조기 삭제 없음 |
| 연출 중 이탈 요청 | 새 제출만 차단, 진행 중 완료 콜백은 유효. 일반 결과 확인과 Completion 표시 전 정리·전환 없음 |
| 경로 이동·Return 수명 중 이탈 | 미제출 Drag·새 미리보기/Return 입력을 닫되 제출된 명령과 결과 재생은 보존. 이동을 포함한 모든 결과 이벤트 완료 후 최종 다시 그리기·Acknowledge 1회. 실제 Detach 뒤 늦은 콜백은 무효 |
| 이탈 완료 뒤 Tick | 옛 전투 서비스 호출이 더 늘지 않음 |
| 이탈 후 재진입 | 옛 Job·활성 전투·미확인 결과 없음, 새 Start 한 번. 확인된 최신 결과와 처리 번호 보존은 정상이며 다시 재생하지 않음 |
| 자발적 포기 | 패배 Completion 1회, 새 CombatResult 순번을 기다리지 않음, 표시 완료 전 EndBattle/전환 없음 |
| 중복 이탈 / 종료와 포기 경합 | 중복 정산·표시·정리 없음. 먼저 확정된 정상 완료는 보존 |
| 정상 이탈 Stop | 오류 로그 없음, 반복 Stop/정리 안전 |
| 실행 예외 뒤 다음 프레임 | 같은 Runtime 재Tick 없음, 실패 서비스 호출 1회, 오류 보고 1회, Job을 정상 완료로 표시하지 않음 |
| 이탈 대기 중 예외 | F1 실패 경로로 중단하고 정상 씬 전환을 계속하지 않음 |
| Runtime 전체 Shutdown | 수명 정리 뒤 Job 재실행 없음. 정상 씬 이탈과 다른 경로임을 확인 |
| 실제 View 연결 전후 | 테스트 View 사용 결과와 운영 View의 입력 중단·결과 확인·미리보기 갱신 검증을 구분 |

**보고:** LifeCycle 훅·공개 정지 경계의 실제 파일, F1~F3 증거, 정상/실패/재진입 테스트, 실제 씬·DB·View 검증 여부. 다음 Phase의 인계 문서와 후속 View 작업을 설명하고 멈춘다.

## Phase 7 — 결과 기록과 후속 View 인계

- [x] **7.1 설계·계획 갱신.** 실제 파일/계약·기존 설계와의 차이·Phase별 결과·남은 외부 조건을 적는다. 합의가 변경된 경우 UML도 수정한다. 검사하지 않은 항목을 통과로 표시하지 않는다.
- [x] **7.2 공유 문서 결과 보충.** Framework 문서에는 실제 공용 diff와 F1·F2 상태, Core 전달 문서에는 사용한 API와 F3 보존·정리 계약을 기록한다. 사용자에게 문서로 전달하고 메시지 발송은 하지 않는다.
- [x] **7.3 새 로컬 인계 작성.** [2026-10-05 골격·View 공동 인계](../handoffs/2026-10-05-combat-view-skeleton-session-handoff.md)에 완료 범위, 테스트 증거, 외부 의존성, 다음 시작점을 정리했다. 이전 로컬 인계에는 새 시작점 링크를 남겼다. Phase 1~4에서 인계할 경우 미연결 상태(Job 미연결, 자동 진행 미등록, 이탈 미완성)를 제목/요약에서 명확히 한다.
- [x] **7.4 View 인수 조건 남기기.** 아래 조건을 다음 전투 View 설계·구현에 전달한다. 이번 골격에서 새 View 모듈을 만들지는 않는다.

View 인수 조건:

- 게임 상태 변경 결과를 확인하면 기존 미리보기 표시를 무효화한다. 미리보기가 여전히 필요한 대상이면 새 번호로 재요청하고, 필요 없으면 ClearPreview한다. 포인터가 정지해 있어도 상태 변화 후 예전 결과를 계속 표시하지 않는다.
- 여러 변경을 한 번에 관찰했다면 현재 상태에 대한 최신 요청 하나로 합친다. 변화가 없는 매 프레임에 같은 요청을 새로 발급하지 않는다.
- 모든 결과를 확인한다. 거절·연출 없는 결과도 AcknowledgePresented를 갱신한다. 최신 결과의 F3 보존 조건을 지킨다.
- 배치→전투 입력 Layer 전환은 EnterCombat의 수락 결과 뒤에 수행한다. Flow에서 입력 Layer를 변경하지 않는다.
- 정상 이탈 요청 뒤에는 새 입력 기록을 멈추되 기존 Job이 읽을 명령을 먼저 지우지 않는다. 표시·Buffer 정리는 F2의 합의된 순서를 따른다.

**완료 보고:** 완료된 Phase 범위, 실제 실행한 테스트와 결과, 미실행/실패 항목, 후속 작업. 골격·Core 처리 연결·LifeCycle 통합·플레이 가능 상태를 구분한다.

## 5. 원칙·리뷰 지적과 Task 대응

| 기준 | 적용 Task |
|---|---|
| SSOT: 상태는 Buffer, 규칙은 Core | 2.1~2.5, 4.1~4.3, 5.1~5.4. Flow의 가변 사본·Core 규칙 계산 없음 |
| 단일 책임: Flow/Plan/Job/LifeCycle 분리 | 1.3, 2.4, 3.1~3.4, 5.1, 6.1~6.2 |
| FW-001/002/004: 공개 계약·기존 수명 등록 | 3.1, 4.0, 6.0. 미확정 F2의 새 공개 API는 인수 후 등록 범위 확정 |
| 예외 Job 반복 실행 | 1.5의 현재 동작 확인, 5.4의 예외 전파, **6.3~6.4에서 F1 실제 경계 검증** |
| 이탈 뒤 기존 Job 실행 | 3.4의 미완성 표시, 6.2~6.4, F2 인수 조건 |
| 미리보기에 밀리는 명령 | 2.4~2.5의 명령 우선·연속 갱신 테스트 |
| 자동 진행 거절 반복 | 2.4의 1단계 미등록(K9), F3 타입/구현 인수, 4.2~4.5·5.3~5.4 |
| 준비 실패의 무한 대기·중복 진입 | 2.3~2.5(K11), 3.2, 6.1·6.4 |
| 상태 변경 뒤 오래된 미리보기 | 2.5의 재요청 계약 모사, 7.4의 실제 View 인수 조건 |
| 정상 이탈 오류 로그 | 2.2·2.6, 6.4 |

## 6. 검증과 실행 기록

컴파일과 EditMode 테스트는 Unity 에디터로 확인한다. 도구로 실행할 수 없으면 사용자에게 실행 항목을 구체적으로 전달하고 결과를 받기 전까지 미실행으로 기록한다. DB 에셋을 바꾸지 않는 이번 골격에서 기존 카탈로그 문제를 임의 수정하지 않는다. 실제 DB·씬을 사용하는 확인 단계에서는 그 선행 문제가 해결됐는지 다시 확인한다.

각 Phase 보고에는 **목표 대비 변경, 실제 파일, 컴파일/테스트 결과, 남은 계약, 다음 Phase의 목표·파일·핵심 동작·공용/에디터 주의점·확인 방법**을 포함한다.

| Phase | 코드/문서 작업 | Unity 컴파일 | 테스트 | 남은 조건 |
|---|---|---|---|---|
| 0 | 문서·기준 정리 완료 | 사용자 확인: 오류 없음 | 기준 4개 묶음 결과 미수신 | 외부 전달·합의와 테스트 결과는 별도 추적 |
| 1 | 완료(§6.2), 커밋 `d1dcb92f` | 사용자 확인: 오류 없음 | 사용자 보고: 모두 통과 | 없음 |
| 2 | 완료(§6.3), 커밋 `23934177`. K11 네 번째 조건은 Phase 3와 함께 재검증 완료 | 사용자 확인: 오류 없음 | 사용자 보고: 모두 통과(조건 추가 후 포함) | 없음 |
| 3 | 완료(§6.4) | 사용자 확인: 오류 없음 | 사용자 보고: 모두 통과(조립 회귀 포함) | 없음 |
| 4 | 완료(§6.6) | 사용자 확인: 오류 없음 | 사용자 보고: 모두 통과 | 없음 |
| 5 | 완료 결정 유지. 병합본에서 통합 fixture 수정 반영(§6.11) | 병합 검증 기록 인수 | 최신 전체 통과 기록은 §6.11의 원본 참조. §6.8의 실패 20건은 과거 결과 | 운영 DB·F2 성공과 구분 |
| 6A | 완료(§6.12): B3·잔여 검증 사용자 확인 및 공동 인계 연결 | 병합 검증 기록 + 사용자 완료 확인 | 6A.5~6 자동 회귀 인수, 추가 수치/로그는 미수신 | 운영 DB·F2와 구분 |
| 6B | F1·정상 F2 기반 인수 + 일반 승리 복귀 호출자 추가(§6.14) | `5078ee0e` 사용자 검증 완료 확인. 이후 호출자 변경은 MSBuild 3대상 오류 0 | 변경 후 관리 코드 92/92 통과. 신규 Unity 실행 결과는 별도 | 패배·최종 승리 목적지 보류, Core 포기 API 대기 |
| 7 | **문서 인계 완료(§6.15)** | 문서 작업으로 Unity 재실행 없음 | 기존 구현 증거 인수, 문서 링크·HTML 도표 확인 | View F와 공동 인계. 6B 잔여 구현과 구분 |

### 6.1 Phase 0 실행 기록 (2026-09-30)

**완료 범위:** 기준 확인·실행 결과 요청/기록, Framework 공유 문서 작성, Core 전달 문서 §13 보충, 계약 추적 표와 설계·계획 정합성 보완. 생산·테스트 코드, 씬·에셋은 수정하지 않았다. Phase 1에는 착수하지 않았다.

| 기준 | 확인 결과 |
|---|---|
| 작업 브랜치·HEAD | `feature/Yonggeun/Stage`, `3a2b6777` |
| 작업 전 수정 파일 | `CLAUDE.md`, `docs/superpowers/specs/2026-09-30-stage-input-design.md`. 이번 작업에서 수정하지 않음 |
| 작업 전 미추적 파일 | Combat View 인계(2026-09-30), Combat Flow 설계·계획, Combat Framework 조사 문서. 이 중 설계·계획만 이번 Phase 범위로 보완 |
| JobType | None 0, Start 1, NewRun 2. 추가 예정 3·4·5는 아직 없음 |
| Combat Job GUID | `87148ad8eb6ab384ca930f4bda0fc576` 재확인. 이번 Phase에서는 이동하지 않음 |
| Plan·Job·Flow | CombatPlan.Start → CombatJob 하나. 현재 Job은 정상 완료하며 CombatFlow 파일은 없음 |
| 현재 서비스 | CoreGroup에 전투 처리 서비스 없음. F3 타입/읽기 등록 없음. 상태 읽기 필드는 있으나 세 번호의 갱신 구현 없음 |
| 기존 등록·수명 | CombatPlan과 CombatLifeCycle은 이미 Factory/레지스트리에 등록됨. PostBindService는 전투 활성 여부와 무관하게 실행됨 |
| 진입·씬 | 현재 소스에서 전투 진입 호출자 없음. Build Settings는 0번 Stage 씬만 있고 CombatLifeCycle.SceneIndex는 3 |
| 기존 테스트 영향 | JobSystemTests의 Combat 참조 5개, 그중 완료 전제 3개 확인. LobbyJob 대체에 필요한 서비스 필드·완료 동작 존재 |

확인 근거는 `JobRootType.cs`, `CombatPlan.cs`, `CombatJob.cs/.meta`, `InGameCoreSystemGroup.cs`, `CombatStateBuffer.cs`, `LifeCycleSystemGroup.PostBindService`, `LifeCycleSystemFactory`, `JobRegisterSystem.Initialize`, `JobSystemTests.cs`, `FrameworkRuntimeCompositionTests.cs`, `ProjectSettings/EditorBuildSettings.asset`이다. 코드 경로는 §4 및 설계 §2에서 찾을 수 있다.

| 검증 항목 | 결과·출처 | 후속 |
|---|---|---|
| Unity 컴파일 | **사용자 답변: "지금 컴파일 에러 없음"**(2026-09-30) | 코드 변경 뒤 해당 Phase에서 다시 확인 |
| JobSystemTests | 사용자에게 기준 실행 요청. 결과 미수신, 에이전트 미실행 | 결과 수신 시 기록 |
| CombatStateBufferTests | 동일 | 동일 |
| CombatViewBufferTests | 동일 | 동일 |
| FrameworkRuntimeCompositionTests | 동일. 실제 PostBindService 회귀 기준용 | 동일 |
| 문서·코드 정합성 | enum·GUID·현재 API·바인딩 경로를 소스로 대조 | Phase 1 직전 다른 작업자의 변경 재확인 |
| 문서 형식·참조 | 변경한 문서 4개의 파일 링크 대상 존재, UTF-8 해석·코드 펜스 짝·행 끝 공백 확인. Core 전달 문서 diff 공백 검사 통과 | 코드·테스트 실행 결과와 구분 |

현재 세션에는 Unity 에디터 테스트 실행 도구가 없어 계획에 따라 사용자에게 결과를 요청했다. 컴파일 오류 없음은 사용자 확인이며 테스트 통과로 확대하지 않는다. 기존 인계의 DB 카탈로그 실패 기록은 과거 보고일 뿐 이번 기준 테스트 실패로 재판정하지 않았다.

문서 준비는 완료했지만 외부 메시지 발송·담당자 합의·F1~F3 구현은 완료되지 않았다. 커밋·push는 하지 않았다. 다음 Phase는 enum 추가, Job 이름/메타 이동, Plan 매핑, Lobby 표본 교체와 전투 구성 테스트다. 각 Phase 뒤 보고·리뷰하는 작업 방식에 따라 여기서 멈춘다.

### 6.2 Phase 1 실행 기록 (2026-09-30)

**완료 범위:** 1.1~1.6. 커밋 `d1dcb92f`(사용자). 1.6은 사용자 확인으로 완료했다. `CombatLifeCycle`·Flow·Buffer·Core는 수정하지 않았다.

| 파일 | 변경 |
|---|---|
| `JobRegister/JobRootType.cs` (공용) | `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5` 추가. 기존 값 유지 |
| `Combat/Jobs/CombatJob.cs` → `CombatPrepareJob.cs` | `git mv`로 `.cs`·`.meta`를 함께 이동. GUID `87148ad8eb6ab384ca930f4bda0fc576` 유지 확인. 조용한 완료를 미연결 예외로 교체 |
| `Combat/Jobs/CombatCommandJob.cs` · `CombatPreviewJob.cs` · `CombatAdvanceJob.cs` | 새 Job 틀. `.meta`는 Unity import가 만든다 |
| `Combat/Plans/CombatPlan.cs` | Start·ProcessCommand·ProcessPreview·AdvanceCombat → 해당 Job 하나, Priority 0. 서명 유지 |
| `Tests/JobSystemTests.cs` (공용) | 표본을 `JobRootType.Lobby`/`LobbyJob`으로 교체(using, 등록 11곳, 타입 확인 3곳). `NonStagePlans_…` 목록에서 Combat 제외, 이유 주석 추가 |
| `Tests/CombatPlanTests.cs` | 새 테스트 8개 메서드(TestCase 포함 14건). `.meta`는 Unity import가 만든다 |

Job 네 개의 공통 동작:

- 생성자는 null 서비스를 `ArgumentNullException`으로 거부하고 Priority를 보존한다. 초기 `IsCompleted`는 false다.
- `Execute()`는 완료 상태면 바로 돌아간다. `default(struct)`처럼 서비스가 없으면 "requires an in-game service" 예외를, 그 밖에는 "…서비스가 연결되지 않았습니다(R6)." 예외를 던진다. 둘 다 `InvalidOperationException`이며 `IsCompleted`는 false로 남는다.
- 설계 §5.3 틀과 다른 점: 옛 `CombatJob`에 있던 서비스 null 검사를 네 Job에 유지했다. `default` 인스턴스를 드러내고, 필드가 읽히지 않는다는 컴파일 경고를 피한다.

`CombatPlanTests`가 확인하는 것:

| 테스트 | 계획 1.5 검증 행 |
|---|---|
| `Register_BuildsOneIncompleteJobWithPriorityZeroAndInjectedService`(4건) | 시나리오별 구성, 서비스 전달 |
| `Register_None_BuildsNoJob` | None 0개 |
| `Register_CreatesFreshJobEachTime` | 독립 생성 |
| `Register_RejectsNullServiceAndNonCombatTypes` | 잘못된 요청(null, `NewRun`, 미정의 값) |
| `JobConstructors_RejectNullService` | 잘못된 요청(생성자) |
| `Execute_ThrowsNotConnected_AndStaysIncomplete`(4건) | 미연결 Execute |
| `Registry_RoutesCombatRootToCombatPlan_WithoutExecuting` | 등록과 실행 구분, 실제 JobRegisterSystem 경로 |
| `RealRunner_PropagatesNotConnectedException_AndKeepsTheJob` | 실제 Runner 경로. 끝에서 `Clear()`. F1 보장으로 보고하지 않음 |

정적 확인: 저장소 전체에서 옛 `CombatJob` 타입 참조가 남지 않았다(문서 제외). 새 파일은 LF·BOM 없음으로 기존 파일과 맞췄다.

| 검증 항목 | 결과 |
|---|---|
| Unity 컴파일·import | 사용자 확인: 컴파일 오류 없음. 새 `.meta` 4개 생성·커밋 확인 |
| `CombatPrepareJob.cs.meta` GUID | 파일 기준 유지 확인. import 뒤 에디터 확인 필요 |
| CombatPlanTests·JobSystemTests | 사용자 보고: 통과 |
| Phase 0 기준 테스트 4묶음 | 사용자 보고(Phase 1 뒤): 요청한 테스트 모두 통과 |

### 6.3 Phase 2 실행 기록 (2026-09-30)

**완료 범위:** 2.1~2.6. 2.6은 사용자 확인(컴파일 오류 없음, 테스트 모두 통과)으로 완료했다. 그 뒤 K11 네 번째 조건을 추가했으므로 해당 변경은 Phase 3 검증 때 다시 실행한다. `CombatLifeCycle`·Plan·Job·Buffer·Core·공용 파일은 수정하지 않았다.

| 파일 | 내용 |
|---|---|
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs` | `internal sealed class`. 생성자(상태 읽기·입력 읽기·Runner·Register), `IsStopped`, `Reset`, `Stop`, `BeginPreparation`, `Tick`. `.meta`는 Unity import가 만든다 |
| `Tests/CombatFlowTests.cs` | 16개 메서드(TestCase 포함 18건). `.meta`는 Unity import가 만든다 |

`CombatFlow`의 상태는 확인 종류(`None`·`Preparation`·`Command`·`Preview`), 기대 번호 하나, 정지 여부뿐이다. 국면·번호·입력은 매 Tick Buffer에서 읽는다.

| 동작 | 구현 |
|---|---|
| Tick 순서 | 정지 → Runner 바쁨 → 확인 → 전투 없음 → PlayerInput에서 (연출 완료 + 명령) → (새 미리보기) → 그 밖 등록 없음. 한 Tick 최대 한 번 |
| 확인 통과 | 준비: `HasBattle`. 명령: `HandledCommandNumber ≥ N`. 미리보기: `AnsweredPreviewNumber ≥ P` 또는 요청이 비워지거나 바뀜 |
| 확인 실패 | `[CombatFlow] …` `Debug.LogError` 한 번, 확인 조건 지움, 정지 |
| `Register` 실패 | 등록 뒤에 확인 조건을 기록하므로 등록 예외가 나면 Flow 상태는 그대로 |
| Automatic | 1단계라 등록·정지 없이 대기(K9) |

`CombatFlowTests`와 계획 Phase 2 표의 대응:

| 테스트 | 계획 표 행 |
|---|---|
| `Constructor_RejectsNullDependencies` | 2.1 null 거부 |
| `BeginPreparation_RejectsStoppedBusyRunnerExistingBattleAndPendingCheck_WithoutRegistering` | 준비 선행 조건 위반 |
| `Preparation_GeneralInputWaitsUntilBattleIsConfirmed` | 준비 중 일반 입력 도착, 준비 정상 반환 |
| `Preparation_ReturningWithoutBattle_ReportsOnceAndStops` | 준비 잘못된 반환 |
| `BusyRunner_RegistersNothing_AndSameCommandIsRegisteredOnce` | Runner Busy |
| `CommandAndPreview_CommandFirstWhenPresented_PreviewOnlyWhilePresenting` | 명령·미리보기 동시 대기 |
| `ContinuousPreviews_DoNotDelayPendingCommandAfterPresentation` | 연속 미리보기 |
| `CommandResult_HandledNumberMoved_ContinuesEvenWhenRejected` / `…Unchanged_ReportsOnceAndStops` | 명령 결과 |
| `PreviewResult_AnsweredClearedOrReplaced_DoesNotStop` / `…SameRequestUnanswered_ReportsOnceAndStops` | 미리보기 결과 |
| `Automatic_FirstStage_WaitsWithoutRegisteringOrStopping` | Automatic(1단계) |
| `FinishedNoneOrNoBattle_RegistersNothing`(3건) | Finished·None·전투 없음 |
| `NormalStop_LogsNothing_BlocksRegistration_AndResetRestarts` / `FailureStop_BlocksRegistration_UntilReset` | 정상 Stop·실패 정지·Reset |
| `PreviewRequestedAgainAfterStateChange_IsProcessedWithNewNumber` | 미리보기 재요청 계약(View 역할 모사, 실제 View 검증 아님) |

테스트 방식: 가짜 Register가 등록 때 가짜 Runner를 바쁨으로 바꾸고, 테스트가 Core 역할로 상태 값을 바꾼 뒤 Runner를 비운다. Job은 실행하지 않는다. 실패 로그는 `LogAssert.Expect`, 정상 Stop은 `LogAssert.NoUnexpectedReceived`로 확인한다.

**설계 변경(사용자 결정, 2026-09-30):** K11에 "멈춘 상태가 아님"을 네 번째 조건으로 추가했다. 처음 구현은 설계대로 정지 상태를 조건에서 뺐는데, 그러면 `Stop()` 뒤 `Reset()` 없이 `BeginPreparation()`을 부를 때 Start가 등록돼 `Stop`의 "새 등록을 막는다" 계약과 어긋났다. 설계 §6·§9·§13.1, 이 계획 §1·2.3을 함께 고쳤고, `BeginPreparation_Rejects…` 테스트 이름에 `Stopped`를 더해 정상 Stop 뒤 거부를, `FailureStop_…` 테스트에 실패 정지 뒤 Reset 전 거부를 추가했다.

| 검증 항목 | 결과 |
|---|---|
| Unity 컴파일·import | 사용자 확인: 오류 없음(K11 조건 추가 전) |
| CombatFlowTests | 사용자 보고: 모두 통과. K11 조건 추가 뒤 Phase 3 검증에서 재실행·통과 |
| 기존 CombatPlanTests·JobSystemTests 회귀 | 사용자 보고: 통과 |

### 6.4 Phase 3 실행 기록 (2026-09-30)

**완료 범위:** 3.1~3.5. 3.5는 사용자 확인(컴파일 오류 없음, 테스트 모두 통과)으로 완료했다. 새 파일·공용 파일 변경은 없다.

| 파일 | 변경 |
|---|---|
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs` | 필드 `_combatViewWrite`·`_flow`. 훅 네 개 연결. 클래스 설명에 1단계 연결(K10)과 미연결 경계(F2·F3)를 적음 |

| 훅 | 동작 |
|---|---|
| `OnBindServices` | `BufferSystemKey.Buffer`(IBufferQuery)와 `SystemGroupsService.Global.JobServiceCommand`를 조회. 입력 쓰기는 `CombatViewWriteSink`만 보관하고 Flow를 만든다(상태 읽기·입력 읽기·Runner·Register) |
| `OnEnter` | 입력 `Clear` → `Reset` → `BeginPreparation` |
| `OnTick` | `Flow.Tick()`만 호출 |
| `OnExit` | `Stop` → 입력 `Clear`. Runner는 비우지 않는다(D7). "이탈 미완성(F2 전)" 주석 |

바인딩 확인(코드 기준):

- 두 키 모두 기존 코드가 같은 방식으로 조회한다(`JobRegisterSystem`·`ChapterProgressionCoordinator`·`StageGeneratorSystem`은 `BufferSystemKey.Buffer`, `LifeCycleBase`는 `SystemGroupsService.Global`).
- `LifeCycleBase.PostBindService`가 Global 로딩 서비스를 받은 뒤 `OnBindServices`를 부른다. 두 서비스가 없으면 `KeyNotFoundException`이 초기화 오류로 드러난다(빈 구현 대체 없음).
- `FrameworkRuntimeCompositionTests`의 `ChapterCoordinator_BindsRealBufferServicesThroughCoreGroup`, `ChapterCoordinator_UsesRegisteredGraphAndSelectsCurrentMap`이 실제 runtime으로 `PostBindService()`를 불러 이 바인딩을 지난다. 이 두 테스트가 3.5의 바인딩 회귀 기준이다.

코드 리뷰 기준(3.5): 훅은 Flow·Plan(Flow를 통한 등록)과 입력 `Clear`만 부른다. 국면 분기·Core 직접 호출·Runner 조작이 없다. `OnEnter`는 이 구간에 도달하지 않으므로 지금 실행되지 않으며, 실행되더라도 `Start` Job이 미연결 예외를 던진다(F1 전 실제 진입 경로는 켜지 않는다).

| 검증 항목 | 결과 |
|---|---|
| Unity 컴파일 | 사용자 확인: 오류 없음 |
| `FrameworkRuntimeCompositionTests`(바인딩 회귀) | 사용자 보고: 통과 |
| `CombatFlowTests`(K11 네 번째 조건 포함)·`CombatPlanTests`·`JobSystemTests` | 사용자 보고: 통과 |

### 6.5 1a51234c 병합 통합 기록 (2026-09-30)

- 요청: 사용자 "너가 잘 병합해줘". `feature/Yonggeun/Stage` HEAD `916c948e`에서 이미 진행 중이던 `1a51234cb4b613012d5a69a1fabc45e00b00ca87` 병합을 이어서 정리했다. 병합 커밋은 저장소 규칙에 따라 사용자에게 남긴다.
- 충돌: 삭제/수정 `CombatJob.cs`는 삭제 상태 유지, 내용 충돌 `CombatPlan.cs`는 `CombatPrepareJob`과 네 분기를 유지했다. 준비 호출은 기존 `CombatPrepareJob`에 옮겼고 `.meta` GUID `87148ad8eb6ab384ca930f4bda0fc576`을 유지한다.
- 준비 Job은 `CoreGroup.Combat.PrepareBattle()`를 한 번 호출하고 정상 반환 뒤 완료된다. 예외를 삼키거나 완료로 바꾸지 않는다. `CharacterBattlePreparationTests`의 옛 Job 이름만 바꿨다. Core 알고리즘을 별도로 수정하지 않았다.
- `CombatPlanTests`는 준비 호출 횟수·완료·원래 예외 보존·Runner Idle 복귀를 검증하도록 갱신했다. 나머지 세 Job의 미연결 검증과 공용 `JobSystemTests`의 Lobby 표본은 유지한다.
- Phase 5 준비 부분만 선반영했다. 운영 입력 G7, 명령·미리보기·자동 진행 서비스, F3, F1·F2는 여전히 필요하다. Flow·LifeCycle·씬 진입 경로는 변경하지 않았다.

| 검증 항목 | 결과 |
|---|---|
| 충돌 마커·옛 `CombatJob` C# 참조·GUID | 정적 확인: 마커·옛 타입 참조 없음, 준비 Job GUID 유지·중복 없음 |
| diff 공백 검사 | 이번 해결·문서 수정은 통과. 들어온 커밋의 폴더 `.meta` 3개에 기존 Unity 빈 값 뒤 공백이 있으며 그대로 유지 |
| Unity 컴파일 | 사용자 확인: 컴파일 오류 없음(2026-09-30, 병합 코드 수정 후) |
| EditMode | 사용자 보고: 전체 통과(2026-09-30, 병합 코드 수정 후). 요청한 범위에 `CombatPlanTests`, `CombatFlowTests`, `CharacterBattlePreparationTests`, `CharacterRosterServiceTests`, `BattlePreparationRuleTests`, `CharacterRosterRuleTests`, `JobSystemTests`, `CombatStateBufferTests`, `CombatViewBufferTests`, `FrameworkRuntimeCompositionTests` 포함 |

Phase 0~3의 이전 통과 보고는 이번 병합 후 검증 결과로 재사용하지 않는다.

**병합 뒤 후속(2026-09-30, Core 답변 반영):**

- Core 답변 §2-5에 따라 `JobSystemTests`의 쓰지 않는 준비 가짜(`FakeCoreService`·`PreparationProbe`)를 제거하고 `FakeInGameService.CoreGroup`을 `null`로 되돌렸다. Lobby·Artifact 표본 Job은 `CoreGroup`을 쓰지 않는다. 테스트에서 CoreGroup 가짜는 `CombatPlanTests` 하나만 남는다.
- 답변 원문을 `docs/superpowers/handoffs/2026-09-30-combat-core-response-to-view.md`로 저장소에 복사했다(원본과 바이트 동일).
- 설계 5차(K12·K13), 요청 총정리의 답변 기록, 이 계획 §2·§3.1·Phase 4 착수 조건·5.0을 병합·답변 뒤 상태로 맞췄다.

| 검증 항목 | 결과 |
|---|---|
| `JobSystemTests`(가짜 제거 뒤) | 사용자 보고(2026-09-30): 문제없음 |
| Core 답변 §2-7 결과 공유 | 위 병합 검증 결과를 천명님께 전달해야 함(사용자) |

### 6.6 Phase 4 실행 기록 (2026-10-01)

- 요청: 사용자 "반영해서 너가 Phase4 진행해줘". 입력 없음 처리는 K14(실행 시점까지 입력 유지), Core 확인 요청은 [View 답변](../handoffs/2026-09-30-combat-result-task4a-view-reply.md)으로 사용자가 전달했다.
- 인수 기준: Core `4c376244`, 병합 `1a95f398`. 인수 가이드 §2.1·§2.3의 확인 항목을 코드로 대조했다(결과 없음 구분, 필드, 불변·Unity 무참조, 전투 전 조회, 같은 저장소).
- Core 파일은 수정하지 않았다. 새 파일·`.meta`·공용 파일 변경은 없다.

| 파일 | 변경 |
|---|---|
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs` | 생성자 두 번째 인자 `ICombatResultReadSink results`(null 거부). `PendingCheck.Advance`. Tick에서 `PlayerInput` 분기 뒤 연출 대기(7행)를 공통 조건으로 두고 `Automatic`이면 S = `LatestResultSequence`를 기록해 `AdvanceCombat` 등록(8행). `FindAdvanceFailure`: 순번 > S → 결과 있음 → 결과 순번 = 상태 순번 → `Kind == Advance` → `Accepted`. 어긋나면 원인 한 번 보고 후 정지 |
| `Runtime/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs` | `OnBindServices`에서 `buffers.CombatResultReadSink`를 Flow에 넘김. 클래스 설명의 "F3 바인딩 미연결" 문구 제거 |
| `Tests/CombatFlowTests.cs` | 가짜 `FakeResults`(최신 하나, View 확인으로 지우지 않음). 생성자 null 검사 5개. `Automatic_FirstStage_…`를 아래 시나리오로 교체 |

| 시나리오(테스트) | 기대 |
|---|---|
| `Automatic_Presented_RegistersOneAdvance_AndIgnoresCommandAndPreview` | Automatic + 연출 완료면 AdvanceCombat 하나. 명령·미리보기는 등록하지 않음 |
| `Automatic_WhilePresenting_WaitsUntilAcknowledged` | 연출 중에는 등록 없음, 확인 뒤 AdvanceCombat |
| `AdvanceResult_AcceptedAfterBase_ContinuesToNextDecision` | 수락 + PlayerInput 전환 뒤 다음 명령 등록 |
| `Automatic_NoResultWithPositiveSequence_UsesKeptNumbersForNextBattle` | 결과 없음 + 발행 번호 7 유지(Core 답변으로 추가): S = 7로 등록, 결과 8 수락 뒤 정지 없이 연출 대기 |
| `AdvanceResult_ReadAfterViewAcknowledgement_KeepsAdvancingInAutomatic` | Runner → View 확인 → Flow 순서에서도 최신 결과를 읽고 다음 진행 등록(결과 조회 수명 모사) |
| `AdvanceResult_NotAcceptedAdvanceAfterBase_ReportsOnceAndNeverReRegisters` ×5 | 거절·누락·종류 불일치·순번 미갱신·순번 불일치 모두 한 번 보고 후 정지. View가 확인한 뒤에도 재등록 없음 |

| 검증 항목 | 결과 |
|---|---|
| Unity 컴파일 | 사용자 확인(2026-10-01): 오류 없음 |
| `CombatFlowTests`(결과 없음 + 양수 순번 테스트 포함) | 사용자 보고(2026-10-01): 모두 통과 |
| `FrameworkRuntimeCompositionTests`(실제 바인딩) | 사용자 보고(2026-10-01): 통과 |
| 회귀: `CombatPlanTests`, `JobSystemTests`, `CharacterBattlePreparationTests`, Core 새 테스트 `CombatResultContractTests`·`CombatResultReadBindingTests`·`CombatStateBufferTests` | 사용자 보고(2026-10-01): 모두 통과(`1a95f398` 병합 뒤 첫 실행) |

**Core 답변(2026-10-01):** [Core 답변](../handoffs/2026-10-01-combat-core-reply-to-task4a-view-reply.md)이 세 요청 모두 현재 구현과 맞게 확정했다(결과 하나 전제, 순번 원본 = 발행 카운터, 결과 없음 + 양수 순번 정상, 자동 진행 거절 = 계속 불가일 때만). Flow 코드는 바꾸지 않았고, 결과 없음 + 양수 순번 시나리오 테스트 하나를 더했다.

**남은 것:** 실제 결과 게시와 Job 연결은 Phase 5이며, 처리 API가 구현되는 Core **Task 7** 이후에 시작한다.

### 6.7 병합 후 재설계·인수 기록 (2026-10-02)

- 기준 커밋: `83f485d11fdae23eaee2552c967692702b592c1e`. §6.6까지는 해당 시점의 이력이다. 현재 Core Task 7의 명령·미리보기·자동 진행 API와 F3 발행·보존은 도착했고, 세 Job 연결은 아직 하지 않았다.
- 사용자 요청: 보완점을 반영해 설계·구현 계획을 다시 작성. 이번 변경은 문서만이며 코드·에셋·테스트 구현은 포함하지 않는다.
- 현재 검증: 사용자 보고 **컴파일 오류 없음, EditMode 미실행**. 이전 전체 통과 및 Core 인계의 테스트 수치를 이번 HEAD의 실행 결과로 사용하지 않는다. 기존 에셋의 `CombatStageDetail.Economy` 누락 여부는 기준 회귀에서 다시 확인한다.

| 순서 | 산출물 | 완료 증거 |
|---|---|---|
| 1. Phase 5.B | 병합 후 회귀 기준 | 테스트 묶음별 통과/실패/미실행과 실패 원인 분류 |
| 2. Phase 5 | 세 Job의 호출·완료 연결, 실제 Core 통합 테스트 | true/false/예외, 결과 확인 지연, 미리보기 교체, EndBattle·재준비 보존 검증 |
| 3. View 0·A~E | DB 참조 검증, 개발 fixture, 부트스트랩·단일 F1, 씬·입력·표시 | RecordInputOnly 진단과 별도의 개발 Live(E.9) 증거 |
| 4. Phase 6 | 동일 F1 인수 + 합의 후 F2 | 예외 후 재실행 차단, Completion 기반 이탈, 중복 요청·늦은 완료·재진입 검증 |
| 5. 운영 인수 | T1/G7 및 실제 맵→전투→결과→복귀 | 기본 DB 준비 소스 성공과 실제 씬 전환·정리 검증 |

F2 대기 중 새 제출 차단은 오류·Detach용 `StopInput`과 구분해 남은 결과의 연출·확인을 유지한다. 현재 포기는 새 CombatResult를 발행하지 않으므로 Completion 표시 완료를 별도로 기다린다. Core의 `ArtifactLoadoutSnapshot` 조립을 View로 옮기지 않으며 공개 호출 계약과 전환 소유자가 확정돼야 F2를 구현한다.

연결 문서: [View 설계 4차](../specs/2026-09-30-combat-view-design.md), [View 계획](2026-10-01-combat-view-plan.md), [Core 인수 안내](2026-09-30-combat-flow-core-intake-guide.md). 이 기록은 외부 전달·담당자 합의·커밋을 뜻하지 않는다.

### 6.8 Phase 5 구현 기록(2026-10-02)

사용자 요청으로 세 Job의 미연결 예외를 실제 Core 호출로 바꿨다. 아래 기록 순서대로 작성 → Unity 검증을 거쳤고 최종 상태는 이 절 끝의 **사용자 결정**이다. Core·Buffer·공용 Runner/Register·Flow·LifeCycle 생산 코드는 수정하지 않았다. Flow의 실행 모드·공유 오류 검사는 설계 §4.1이 단일 원본이며 View B2에서 구현한다.

| 파일 | 반영 내용 |
|---|---|
| `Combat/Jobs/CombatCommandJob.cs` | `CoreGroup.Combat.ProcessNextCommand()` 정상 반환 뒤 완료 |
| `Combat/Jobs/CombatPreviewJob.cs` | `CoreGroup.Combat.ProcessLatestPreview()` 정상 반환 뒤 완료 |
| `Combat/Jobs/CombatAdvanceJob.cs` | `CoreGroup.Combat.AdvanceCombat()` 정상 반환 뒤 완료 |
| `Assets/Tests/EditMode/Framework/CombatPlanTests.cs` | 선택한 서비스 호출 1회, true/false 뒤 완료·Runner Idle 복귀, 완료 전 상태·원래 예외 보존. 등록만으로 실행되지 않는 기존 검증 유지 |
| `Assets/Tests/EditMode/Framework/CombatFlowIntegrationTests.cs`와 `.meta` | 실제 Flow/Register/Plan/Runner/Core/Buffer를 연결하는 15개 NUnit 사례 추가 |

통합 fixture는 기존 `IBattleInitializationSource`에 유효한 정의를 공급하고 실제 로스터 초기화를 호출한다. 전투 진입 전 맵·노드·선택·재화를 테스트에서 설정하며 전투 상태·결과·번호는 Core가 게시한다. 표시 완료는 테스트 소비자가 입력 정리·확인을 수행해 모사한다. 실패 사례는 정상 Core 발행 이후 Flow의 읽기 경계만 교란한다.

| 통합 테스트 묶음 | 작성한 기대 조건 |
|---|---|
| 준비·입력 없음 | 준비 등록 1회, 준비 확인 이후 명령 등록, 입력 없을 때 처리 등록 없음 |
| 명령 수락·거절·등록 뒤 제거 | 실제 상태·번호·결과, 표시 완료 전 다음 명령 대기, K14 위반 시 정상 false 이후 오류 1회·재등록 없음 |
| 미리보기 교체·취소·우선순위 | 실행 시점의 최신 요청, 취소 시 정상 false, 상태·결과 순번 불변, 명령 우선 및 연출 대기 중 미리보기 처리 |
| 실제 자동 진행·읽기 실패 주입 | EnterCombat 표시 후 실제 Advance, View 확인 이후 Flow 결과 조회, 결과 누락·거절·종류/순번 불일치 시 정지 |
| 종료·다음 준비·예외 | 실제 승리/진행 반영 후 EndBattle·다음 준비의 결과/번호 보존, Completion 해제, 실제 준비 소스 예외의 원인·Runner Job 보존 |

검증 기록:

- 변경 전: 사용자 확인 Unity 컴파일 오류 없음, `83f485d1` 병합 후 EditMode 미실행. 기준 회귀 결과를 추가로 요청했으며 아직 수신하지 않았다.
- 변경 전·후 `ProjectL.Tests.Framework.csproj` 최초 MSBuild: 모두 오류 0건, 기존 경고 6건. 이름·형식 정리 후 최종 증분 빌드도 오류 0건이며 재컴파일한 테스트의 기존 경고 1건만 출력했다. 새 통합 테스트는 임시 targets로 Compile 항목에 포함했다. 생성된 csproj는 수정하지 않았다. 변경 전·최종 로그는 로컬 `Temp/CodexPhase5-baseline-build.log`, `Temp/CodexPhase5-build.log`다.
- 기존 경고: `CombatCommandEffects.cs` nullable 문맥 1건, `AudioBase.cs`·`AudioSystem.cs` 중복 using 4건, `StageBufferInteractionTests.cs` 미할당 필드 1건. Phase 5 변경으로 추가된 경고는 없다.
- **이번 변경 후 Unity 컴파일·EditMode 테스트는 미실행.** MSBuild는 컴파일 증거이며 NUnit 실행 통과를 뜻하지 않는다. 5.B·5.4·5.5는 아직 완료하지 않는다.
- 남은 회귀: `CombatPlanTests`, `CombatFlowTests`, `CombatFlowIntegrationTests`, `JobSystemTests`, `CharacterBattlePreparationTests`, `CombatCommandPublicationTests`, `CombatCommonPublicationTests`, `CombatPreviewPublicationTests`, `CombatStateBufferTests`, `CombatViewBufferTests`, `FrameworkRuntimeCompositionTests`. 실제 DB 에셋 묶음은 5.B에 따라 결과를 별도 기록한다.

T1/G7 운영 DB 준비, 실제 View 표시, F1 런타임 중단, F2 씬 이탈·재진입은 이 구현의 완료 범위에 포함하지 않는다. 커밋·push는 하지 않았다.

**Unity 검증(사용자 실행, 2026-10-02, `TestResults.xml`):** 컴파일 오류 없음. EditMode 1642개 중 1622 통과, 실패 20.

| 실패 | 원인 | 처리 |
|---|---|---|
| `CombatFlowIntegrationTests` 15개 전부 | fixture `StageConfig(8, 6, 8, 2, 4, …)`가 Core의 기존 규칙(배치 깊이 합 < 맵 높이, `a18d86d6`부터)을 어겨 `Fixture` 생성자에서 `ArgumentOutOfRangeException`. 테스트 본문은 실행되지 않았다. Core 문제가 아니다 | **사용자 결정으로 수정 보류.** 수정안: 맵 높이 7 + `HeadquartersCell` (0, 3). 수정 뒤에도 실제 흐름의 첫 실행이므로 추가 실패 가능 |
| `DataBaseAssetTests` 4개, `ArtifactDataTableTests` 1개 | 실제 에셋의 `CombatStageDetail` Economy 누락(Core 인계에 기록된 기존 5건) | 우리 범위 밖 |

- 통과: `CombatPlanTests`(세 Job의 true/false 완료·예외 보존), `CombatFlowTests`, `JobSystemTests`, `CharacterBattlePreparationTests`, Core 게시 테스트 3묶음, `CombatStateBufferTests`, `CombatViewBufferTests`, `FrameworkRuntimeCompositionTests`.
- 상태: 5.B 완료. 5.4·5.5는 **실제 Flow→Core 통합 경로가 미검증**이라 체크하지 않는다. Job 호출 연결은 Plan 테스트로 확인됐다(호출 연결 확인 ≠ 실제 처리 검증).
- **사용자 결정(2026-10-02): Phase 5 완료로 본다.** 통합 테스트 실패는 테스트 코드(fixture) 문제이며 View 작업과 골격 Phase 6을 막지 않는다. 실제 Flow→Core 처리 경로는 View 계획 E.9(개발 Live)에서 실제 런타임으로 확인한다. fixture 수정안(맵 높이 7, HQ (0, 3))은 위 표에 남긴다.

### 6.9 진입 계약 인수·다음 세션 작업 순서 (2026-10-03)

- 사용자 지시: 골격 구현은 별도 세션에서 진행하고, 이 세션은 기존 설계·계획·인수 안내를 갱신한다. 이번 기록은 6A 구현 착수/완료를 뜻하지 않는다.
- 기준 커밋: `491cae4d`에 `81c8c38`과 선행 Core 변경을 병합했다. 병합 호환 수정·컴파일 증거는 [View 계획 §7.15](2026-10-01-combat-view-plan.md#715-core-81c8c38-병합-반영-2026-10-03)를 참조한다. Unity 실행 테스트는 그 증거에 포함되지 않는다.
- 다음 세션: 6A.0 → 6A.1~6A.4와 View B3 연결 → 6A.5~6A.7. 기존 Phase 5 완료 결정은 유지한다. 새 진입 검증을 위해 필요한 유효 fixture와 등록 경로 테스트는 별도로 확보한다.
- 이후: View E/E2 검증과 6B의 F1 인수, View A2/Core T1/G7 및 F2 계약·구현, 실제 DB·씬 인수. 골격 7/View F는 완료 범위별 부분 인계가 가능하다.

### 6.10 Phase 6A 실행 기록 (2026-10-04, 코드 완료·Unity 검증 대기)

사용자 지시 “그러면 골격 6A 진행해”로 `491cae4d` 위에서 구현했다. 시작 시 수정돼 있던 설계·계획·CLAUDE 7개 파일의 내용을 보존했다. Core·Buffer·Runner·씬·에셋은 수정하지 않았다. 기존 Phase 5의 통합 fixture 15건 보류 결정을 유지한다.

- **확정 계약:** [설계 §6.3](../specs/2026-09-30-combat-flow-skeleton-design.md#63-명시적-전투-진입-계약-k20)의 6A.0. CombatEntryRequest와 기존 CombatLifeCycle이 진입 수명을 담당한다. 별도 실행기·전역 번호·콜백 큐 없음.
- **등록:** `IJobRegisterSystem.RegisterCombatEntry(request)` → `CombatPlan.RegisterEntry` → 기존 CombatPrepareJob의 대상 생성자 → `Core.Combat.EnterBattle`. 결과 저장 후 Job 완료, Runner Idle 뒤 LifeCycle 모듈 Tick이 소비한다. 정상 거절은 요청자로 반환하고 활성화하지 않는다. 예외 Job은 미완료로 보존한다.
- **활성화:** Combat 비활성 중에도 Runtime이 진입 결과를 확인한다. 수락 후 Change(Combat) 1회 → Enter 0에서 이번 대상/결과·Current 일치 확인 → 입력 Clear → 준비된 Flow 활성화 → View Enter 100 Attach → 첫 그리기 후 Bind. 활성 Flow Tick은 Manager만 담당한다.
- **취소·교체:** 실행 전 취소만 허용한다. 동일 Queued 대상은 같은 요청 반환. 다른 대상은 취소 Job 배출 뒤 새 요청. 실행/수락 뒤 취소·재요청은 거부한다. 활성화 실패는 F1, 재시도·Core reset 없음. 종료 시 소유 참조를 놓는다.
- **B3:** [View 계획 §7.17](2026-10-01-combat-view-plan.md#717-phase-b3-실행-기록-2026-10-04-코드-완료검증-대기). Bootstrap의 사전 ConfirmStageEntry와 LifeCycle의 중복 Prepare 제거, 두 모드에 동일 진입 적용. View에는 ICombatEntryRead만 연결한다.
- **호환:** 구형 Register(Combat, Start)·PrepareJob 두 인자 생성자·Flow.BeginPreparation은 확정 선택의 기존 호출자/테스트용으로 유지한다. 신규 LifeCycle·Bootstrap은 사용하지 않는다. 공용 Register 테스트 대역 2곳은 새 멤버를 구현했다.

| 검사 | 결과와 한계 |
|---|---|
| MSBuild | 실제 소스 목록 targets로 Framework·CombatViewBoundary 테스트와 Assembly-CSharp-Editor 빌드 오류 0·새 경고 0(Framework 기준 기존 경고 6건). Systems만 UNITY_EDITOR/DEVELOPMENT_BUILD 정의를 제거한 조건부 컴파일도 오류 0. Unity 컴파일/플레이어 빌드와 구분 |
| 관리 코드 직접 실행 | CombatEntryIntegrationTests 11건 + CombatPlanTests 34건 + JobSystemTests 8건 = **53/53 통과**. NUnit Assert를 가진 메서드를 별도 .NET 프로세스에서 호출. Unity Test Runner·씬 재생을 대신하지 않음 |
| 신규 통합 fixture | 유효한 맵 깊이·메인 범위, 서로 다른 Stage 100/101의 보드 폭 8/9. Core 상태/번호를 가짜로 게시하지 않음. 실제 Runtime/Register/Plan/Runner/Core/Buffer/LifeCycle/Flow 사용, 씬 로딩·결과 표시는 대역 |
| 신규 단위 회귀 | Plan 3건: 캡처 대상·중복 등록·완료 후 재호출 0·누락/불일치 수락 거부. View 1건: HasBattle만으로 입력 연결하지 않음(작성·컴파일, Unity 실행 대기) |
| Unity/EditMode | 사용자에게 전체 실행 결과 요청, 미수신. 최소 CombatEntryIntegrationTests·CombatPlanTests·CombatFlowTests·JobSystemTests·CombatViewSystemTests·FrameworkRuntimeCompositionTests·CharacterBattlePreparationTests 및 Buffer 회귀 |
| 재생 | View B3.5 두 모드의 Boot → 진입 → 최초 표시·입력, 정상 거절·F1 확인 대기 |

로컬 재현 스크립트·로그는 `Temp/CodexPhase6A/`이며 Git 대상이 아니다. 6A.0~4는 구현 반영했으며 6A.5~7은 Unity/EditMode·B3 재생 결과까지 받아 완료 처리한다. EndBattle과 이탈을 테스트에서 호출한 다음 대상 검증은 운영 F2 구현 완료가 아니다. T1/G7·A2·F2는 후속 범위다. 커밋·push·외부 메시지 발송은 하지 않았다.

### 6.11 e0b02cc 병합 인수와 잔여 Phase 정리 (2026-10-05)

- Stage 브랜치를 `0d0dcc70`에서 `e0b02cc7`로 fast-forward했다. 충돌·새 병합 커밋 없이 반영했으며, 이번 잔여 계획 정리 시작 시 작업 트리는 깨끗했다.
- 자동 검증 원본은 [Stage 병합 검증 기록](../handoffs/2026-10-04-stage-merge-verification.md#verification)이다. Unity 6000.0.73f1의 Core·Framework·CombatViewBoundary 전체 통과 기록을 인수한다. 원본 XML/log는 해당 기록의 로컬 경로에 있으며 Git에 포함되지 않는다. **이번 문서 정리에서 Unity를 새로 실행하거나 원본 XML을 재검사한 것은 아니다.**
- 6A.5의 `CombatPlanTests`·`JobSystemTests` 및 6A.6의 `CombatEntryIntegrationTests` 코드와 위 전체 실행 범위를 대조했다. 진입 테스트는 실제 Register/Runner/Core/Buffer/LifeCycle을 사용하며 두 모드, 거절, 취소·교체 제한, 오류 뒤 재시도 차단, 활성화 1회, 첫/다음 대상과 이전 결과 보존을 검사한다. 이를 근거로 **6A.5~6의 자동 검증을 인수**한다. 다음 대상 테스트의 직접 Core 정리는 운영 F2 구현 증거가 아니다.
- §6.8의 `CombatFlowIntegrationTests` fixture는 병합에서 배치 구역 중첩을 수정하고 새 경로 이동 계약에 맞췄다. 당시의 통합 15건·Economy 5건 실패를 현재 미해결 차단으로 남기지 않는다. 이전 실행 기록 자체는 보존한다.
- View A2와 경로 이동·Return·순서 있는 시간 기반 이동 연출이 반영됐다. View별 검증·잔여 재생은 [View 계획 §7.20](2026-10-01-combat-view-plan.md#720-e0b02cc-병합-인수와-잔여-검증-2026-10-05)이 원본이다. 정상 Boot 재생, 실제 씬 자동 테스트, 운영 DB source 성공은 서로 다른 증거다.
- 다음 작업은 §0.1 순서로 진행한다. Core의 기본 source는 여전히 T1/G7 예외를 던지고 포기는 외부 payload를 받는다. F1 인수·F2 연결·운영 재화/DB 검증을 완료로 올리지 않았다. 이번 변경은 계획과 상태 기록 정리이며 코드 구현·커밋·push·외부 전달은 하지 않았다.

### 6.12 Phase 6B F1 보완과 Core 요청 (2026-10-05)

**착수 근거:** 사용자가 §0.1의 1·2번(6A/B3 마감, View E/E2/A2 잔여 검증)이 끝났다고 확인한 뒤 6B 진행과 Core 요청 정리를 지시했다. 6A.7 및 View B3.4~5·E.7·E.9·A2.6은 그 확인과 기존 병합 검증을 연결해 마감한다. 개별 재생 로그나 새로운 전체 테스트 수치를 받은 것은 아니며, 이 확인을 신규 F1 보완·F2·운영 DB 성공으로 확대하지 않는다.

**6.0a·6.3 변경:**

- `GameBootstrap`: 초기화 중 공유 오류/종료가 확정되면 개발 진행 준비·초기 전환·Tick 시작을 차단한다. 기존 Update의 오류 뒤 재Tick 방지와 Shutdown 1회 수명은 유지한다.
- `CombatViewSystem`: Return·FocusLost/CaptureLost를 기존 Receive/F1 경계로 통합. Unity 씬 비활성화 콜백은 별도 guarded Detach에서 같은 경계로 보고하며, 실패해도 표시 해제 시도 후 오류를 남긴다. Framework가 호출하는 Detach의 예외 전파 계약은 유지한다.
- `CombatPathInput`·`CombatMovementInteraction`: 입력 수명을 먼저 무효화하고 이동 요청·Return 표시·Draft 정리를 각각 시도한다. Return 표시 정리 실패가 Draft 정리를 막지 않는다.
- `OrderedCombatPresentation`: 결과/늦은 완료를 먼저 무효화하고 이동·다른 연출 취소를 각각 시도해 실패를 모은다. 취소 재진입을 차단하며 이미 분리한 이동 재생을 중복 취소하지 않는다. 새 재생에서 이동 재생기를 다시 만든다.
- Flow 분기·Core 계산/Buffer·Framework Runner·씬 에셋은 변경하지 않았다. 새 오류 경계·서비스·asmdef를 만들지 않았다.

| F1 요구 / 경계 | 증거 |
|---|---|
| 최초 원인 고정·보고 1회·정리 실패 수집 | 기존 `RuntimeFaultBoundaryTests` |
| 실제 구동자에서 실패 Job 재실행/완료 위조 없음, 외부 오류 뒤 Tick/초기 전환 차단 | 신규 `GameBootstrapFaultBoundaryTests`: 실제 Bootstrap 메시지·FrameworkRuntime·JobRunner 사용. 자동 Boot/DB 시작은 범위 밖 |
| 입력 정리 → Runtime Shutdown 1회, 정리 실패 뒤 재시작/재시도 없음 | 같은 Bootstrap 테스트의 정상/입력 실패/하위 Shutdown 실패 사례. Framework는 하위 실패 시 순회를 멈추므로 전체 자원 해제 성공을 주장하지 않음 |
| Return·포커스/캡처·씬 비활성화 예외의 공통 경계 연결 | `CombatViewSystemTests` 신규 4사례. 비활성화는 실제 연결 핸들러를 호출해 검사 |
| 취소 실패에도 나머지 정리, 재진입·중복 정리, 재사용 | 신규 `CombatPresentationCleanupTests` 4사례·`CombatPathInputCleanupTests` 1사례 |
| 일반 결과 확인·늦은 콜백·정상 종료 결과 보존 | 기존 `CombatResultPlaybackTests`·`CombatViewSystemTests`·`CombatMovementViewTests`. 변경 후 Unity 회귀 대상 |

**실행:**

- 수정 전 `CombatPresentationCleanupTests` 관리 실행 4건 중 3건 실패로 취소 실패/재진입 결함 재현. Windows PowerShell 실행은 참조 어셈블리 제약으로 사용하지 못했고, 실제 재현/통과 기록은 PowerShell 7 실행이다.
- 수정 후 전체 현재 소스를 포함한 MSBuild: `ProjectL.Tests.Framework`, `ProjectL.Tests.CombatViewBoundary`, `Assembly-CSharp-Editor` **오류 0**. 기존 Audio using·Core nullable·미할당 필드·System.Net.Http 참조 경고는 유지하며 변경 파일의 새 경고는 없다.
- 관리 코드 **83/83 통과**: RuntimeFaultBoundary·CombatResultPlayback·CombatPresentationCleanup·CombatPathInputCleanup·CombatEntryIntegration·CombatPlan·JobSystem. SetUp/TearDown과 TestCase 인자를 적용했다. Unity 밖 실행이며 Bootstrap/씬 테스트의 통과 증거가 아니다.
- Unity Refresh·컴파일 및 신규 3묶음과 View/이동/Flow/진입 회귀를 사용자에게 요청했다. **새 Unity 결과 수신 전이므로 6.0a·6.3·6.5의 최종 인수는 대기**다. 로컬 검증 스크립트/로그는 `Temp/CodexPhase6B/`이며 Git 대상이 아니다.

**6.0b 준비 및 다음 작업:** [2026-10-05 Core 요청사항](../handoffs/2026-10-05-combat-core-requests-phase6b.md)에 공개 포기 API·기본 DB source·시작 재화를 정리했다. 외부 전달/답변·F2 계약 확정·6.1~6.2 구현은 미완료다. F1 Unity 인수 후 기존 LifeCycle·View 수명에서 전환 요청 소유자, Completion 표시 완료, EndBattle 호출 파일/계약을 먼저 확정하고 정상 종료부터 연결한다. 포기는 실제 Core API 인수 뒤 연결한다. 이번 작업에서 커밋·push·외부 메시지 발송은 하지 않았다.

### 6.13 Phase 6B 정상 F2 선행 구현·재화 요청 정정 (2026-10-05)

**지시:** F1은 Core 회신과 무관하게 진행하고 Unity 검증 항목·기대 결과를 제공한다. F2는 가능한 정상 종료 부분부터 구현하며 포기는 Core API 인수 후 연결한다. 초기 금액은 SO/DB 원본, Stage 골드·Run 재화 분리·기존 지급/보존 규칙을 따른다. View 직접 지급·초기화는 하지 않는다.

**구체 계약:** [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05)가 원본이다. `CombatLifeCycle` 서비스가 `CombatExit.RequestNormalExit(destination)`을 공개하고 `CombatExitSequence`에 순서를 위임한다. 정상 Completion이 없는 전투와 포기는 거절한다. `CombatFlow.Stop`과 View 제출 정지 → 실제 Runner Idle → 남은 일반 결과 확인 → 승패 확인 UI → EndBattle 1회 → 보존/정리 확인 → Manager.Change를 연결했다. 직접 Manager.Change/Exit는 로딩 전에 검사하고 Runtime Shutdown은 별도 F1 정리를 유지한다. Flow Tick 판단표·Framework 기반 코드·Core·재화 Buffer는 수정하지 않았다.

**View:** `ICombatExitPresentation`의 제출 정지는 Playback/명령을 보존하며 결과 후처리에서 입력을 다시 열지 않는다. `CombatHud`의 전용 승패 확인 버튼과 Combat 씬 참조를 추가했다. 일반 결과 확인과 Completion 확인은 서로 다른 신호이며 별도 게임 결과 순번을 만들지 않는다. 상세 View 기록은 [View 계획 §7.22](2026-10-01-combat-view-plan.md#722-골격-6b-정상-종료-표시-연결-2026-10-05)이다.

**검증:**

- 현 소스 전체를 포함한 MSBuild: `ProjectL.Tests.Framework`, `ProjectL.Tests.CombatViewBoundary`, `Assembly-CSharp-Editor` 모두 오류 0. 기존 경고 외 이번 수정 파일의 새 경고 없음.
- 기존 관리 회귀 및 `CombatEntryIntegrationTests` **89건 통과 / 실패 0**. 실제 Runtime·Register·Plan·Runner·Core·Buffer·LifeCycle을 사용하고 씬 로딩·결과 소비/Completion UI는 대역이다. 최종 결과 표시 대기, 실제 Runner의 등록 작업 보존·Idle 대기, 중복/다른 목적지 요청, 직접 Manager 우회 차단, Completion 콜백 뒤 예외·Detach·전환 실패, 다른 대상 재진입과 오래된 확인 콜백을 검증했다.
- 실제 View의 제출 정지/Playback 보존 및 Completion 확인 1회·Detach 뒤 무효화 3건을 추가했다. 컴파일은 통과했으며 Unity 실행은 미확인이다.
- Combat 씬의 추가 오브젝트 ID 중복·참조 연결 및 문서 링크를 정적으로 검사했다. Unity 씬 로드/시각 확인은 대신하지 않는다.
- **Unity 컴파일·EditMode·Play는 대기.** F1과 F2의 실행 묶음·순서·기대 결과는 [2026-10-05 Unity 검증 문서](../handoffs/2026-10-05-combat-phase6b-unity-validation.md)가 원본이다. 6.0a·6.3·6.5를 통과로 바꾸지 않는다.

**남은 일:** 승리/패배별 실제 화면 목적지와 요청 호출자를 질문했으며 답변 대기다. 목적지는 공개 API의 필수 인자로 남기고 자동 전환 정책은 만들지 않았다. 포기 입력 Core API·포기 연결, EndBattle 자체의 실패 주입 확대와 실제 Unity 정상/패배 재생 인수를 포함한 전체 F2는 미완료다. 따라서 6.0b·6.2·6.4 전체 체크는 남긴다. StageSelect 자체의 다음 노드 클릭 UI와 Lobby 런 종료 화면은 별도 화면 작업이다.

**Core 요청 3 정정:** [새 Core 요청 문서 §1.1](../handoffs/2026-10-05-combat-core-requests-phase6b.md#11-요청-3-상세--두-재화의-원본초기화보존)에 확정 기획 근거·현재 공통 잔액 불일치·두 재화의 초기화/보존 표·실제 Stage SO 값·남은 Run SO/DB 확인 항목을 기록했다. 기존 규칙을 다시 미정으로 돌리지 않았으며, Core 코드·재화 원본·콘텐츠 금액은 변경하지 않았다. 공용 Manager 변경 공유도 문서로 준비했으며 외부 발송·커밋·push는 하지 않았다.

### 6.14 Phase 6B 일반 승리 복귀 호출자 연결 (2026-10-05)

**기존 커밋 인수:** 사용자가 “지금 커밋한 것들은 검증 됐음”이라고 확인했다. 시작 HEAD는 `5078ee0e`이며 작업 트리는 깨끗했다. 해당 커밋의 F1 보완 및 정상 F2 서비스·표시·정리 구현을 인수하고 6.0a·6.3과 View E.8을 마감한다. §6.12~13의 Unity 대기 기록은 작성 당시 이력이다. 새 테스트 수치/개별 Play 로그는 받지 않았으며 이번 추가 코드의 Unity 통과로 확대하지 않는다.

**사용자 결정:** “일반 승리만 StageSelect, 패배·최종 승리 목적지는 보류.” 자동 요청 정책은 [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05)에 반영했다.

- `CombatLifeCycle`이 Core Completion을 읽어 기존 `RequestNormalExit`의 실제 호출자가 된다. Flow의 처리 확인 후 요청하고 기존 `CombatExitSequence`가 마지막 결과 → Completion 확인 → EndBattle → StageSelect 이동을 수행한다.
- `CombatFlow` 판단표, Core, Buffer, 재화, Bootstrap, 씬 파일은 수정하지 않았다. Run 시작/종료/챕터 연장 정책을 추가하지 않았다.
- `CombatEntryIntegrationTests`를 자동 호출에 맞춰 갱신했다. 결과 확인 전 대기, 일반 승리 후 StageSelect와 다음 전투 진입, 기존 Run/잔액 보존, 실제 Core 챕터 완료·패배·포기의 자동 전환 제외를 확인한다. 테스트의 포기 API 직접 호출은 운영 포기 연결이 아니다.

**이번 변경 확인:** MSBuild `ProjectL.Tests.Framework`·`ProjectL.Tests.CombatViewBoundary`·`Assembly-CSharp-Editor` 오류 0. 관리 코드 회귀 **92/92 통과**, 실제 Runtime·Runner·Core·Buffer·LifeCycle 사용, 씬 로딩·표시는 대역. 로그는 Git에서 제외된 `Temp/CodexPhase6B/compile-routing-summary.log`와 `managed-routing.log`다. Unity를 새로 실행하지 않았다. 이번 호출자의 실제 재생 확인은 일반 승리 → 결과 확인 → StageSelect이며, 이미 인수한 커밋의 F1 검증을 재요청하지 않는다.

**남은 구현:** 패배·챕터 완료(최종 승리 포함)는 목적지 확정 후 기존 경계에 연결한다. 포기는 현재 공개 서명이 여전히 `AbandonBattle(ArtifactLoadoutSnapshot)`이므로 Core 소유 입력 조립 API를 받은 뒤 연결한다. 6.0b·6.2·6.4·6.5 전체 완료 표시는 남긴다. 운영 T1/G7·두 재화 분리는 별도 Core 인수이며 다음 노드 선택 UI·Run 종료 화면은 별도 화면 작업이다. 커밋·push·외부 발송은 하지 않았다.


### 6.15 Phase 7·View F 공동 문서 인계 (2026-10-05)

**지시·기준:** 사용자가 문서 작업인 골격 7과 View F를 세션 구분 없이 여기서 진행하도록 요청했다. 시작 HEAD는 `bfe63a27`, 작업 트리는 깨끗했다. 일반 승리 연결이 커밋된 사실은 확인했으나 이후 Unity 검증까지 완료됐다고 추정하지 않는다.

**문서 작업 완료:**

- 7.1 / View F.1: View 설계 11차에서 경로 이동·Return·OrderedPresentation·SceneDisplay·서비스 노출을 코드에 맞췄다. 골격 파일 목록에 ExitSequence·Manager guard를 추가했다. Flow 판단은 골격 §4.1, 이탈 정책은 §6.4만 원본으로 유지한다.
- [현재 통합 UML](../specs/2026-10-05-combat-current-uml.md)과 [오프라인 HTML](../specs/2026-10-05-combat-current-uml.html)에 골격·Plan/Job·View 관계, 진입, 경로/결과, 정상 이탈, 상태 경계를 정리했다. 옛 UML은 당시 스냅샷임을 표시하고 최신 링크를 연결했다.
- 7.2 / View F.2~3: [공동 Framework 공유](../handoffs/2026-10-05-combat-view-skeleton-framework-share.md)에 실제 공용 변경·호출자 대응을 정리했다. 기존 Core 메모 §8과 최신 요청 §5에서 반영된 답변과 미인수 API를 구분했다. 새 Core 요청 원본을 중복 생성하지 않았다.
- 7.3~4 / View F.4: [새 공동 인계](../handoffs/2026-10-05-combat-view-skeleton-session-handoff.md)에 책임·외부 경계, 개발/운영 증거, 후속 작업을 연결했다. 결과 확인·미리보기 갱신·입력 Layer·이탈 수명 조건을 포함한다.

**확인:** 변경한 문서 19개의 로컬 파일/절 링크 328개에 오류가 없다. Mermaid 구조·타입명을 코드와 대조했고, HTML은 오프라인 Chrome에서 7개 탭·6개 도표·확대·인쇄 전환을 확인했다. 도표 밖으로 잘리는 글자와 페이지 스크립트 오류가 없으며 데스크톱·좁은 화면을 시각 점검했다. `git diff --check`도 통과했다. 이번 변경은 문서·HTML뿐이며 Unity·기존 테스트를 다시 실행하지 않았다. 문서 Phase 7은 완료지만 6B 전체와 운영 전투 완료는 아니다. 포기 API·운영 source·재화는 Core 인수, 패배·최종 승리 목적지는 사용자 보류를 유지한다. 커밋·push·외부 발송은 하지 않았다.

### 6.16 Phase 6B 패배·최종 승리 → 새 Run 준비 후 StageSelect (2026-10-06)

- 사용자 결정(설계 §6.4·K21): 패배·최종 승리의 최종 목적지는 Title, 그 화면이 생길 때까지 **새 Run 준비 후 StageSelect**. 최종이 아닌 챕터 클리어는 범위 밖(1a)이지만, 최종 여부를 `EndBattle` 뒤에만 알 수 있어 Core가 다음 챕터를 선택하면 초기화 없이 StageSelect로 간다(방안 A). 연결 정의가 없는 현재 런타임의 `DefinitionUnavailable`은 임시로 Run 종료. 새 Run 로스터는 로드아웃 1(임시 상수).
- 확인한 Core 사실: Buffer `IRunProgressBufferWriteSink.ResetRunProgress()`(전투·현재 위치·스테이지/맵 진행·로스터·Run 재화·Run 결과 초기화, 맵 등록·Artifact 보유 유지), Core `Characters.InitializeRoster`, `ChapterProgression.CompleteChapter`(전투가 남으면 거절). 패배는 Run 결과 Defeat를 기록해 초기화 없이는 StageSelect의 `EnterChapter`가 거절된다.

| 파일 | 변경 |
|---|---|
| ~~`Combat/Jobs/CombatNewRunRosterJob.cs`~~ | 처음에 만들었으나 Core `RunStart.StartNewRun` 도입으로 2026-10-07 삭제(아래 정정) |
| `Combat/Plans/CombatPlan.cs` | `JobType.NewRun` → Stage와 같은 `NewRunJob`(Core `RunStart.StartNewRun(NewRunLoadoutKey)`). `NewRunLoadoutKey = 1`. 처음의 `ResetRunProgressJob` → `CombatNewRunRosterJob` 2단계는 병합 정리(`29a7bbbe`)에서 대체 |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`(공용) | Combat Plan 등록을 Buffer 전달 람다로 변경(Framework 공유 문서 §1 기록) |
| `Combat/CombatExitSequence.cs` | 상태 `PreparingNewRun` 추가. EndBattle 뒤 `EndsRun()`(보스 승리면 `CompleteChapter`, 그 밖은 Core `Outcome`) → Run 종료면 `Register(Combat, NewRun)` 후 Runner Idle·`Outcome == InProgress`·전투 없음 확인 뒤 전환 |
| `Combat/CombatLifeCycle.cs` | 자동 요청을 `RequestStageReturnAfterCompletion()`으로 바꿔 포기가 아닌 모든 Completion에서 StageSelect 이탈 요청. 이탈 순서에 ChapterProgression·NewRun 등록 전달 |
| `Tests/CombatPlanTests.cs` | NewRun Buffer 필수, 실제 Registry로 초기화 → 로드아웃 1 지급 순서(지급 시점 Run 재화 0), 지급 거절 시 예외·미완료 |
| `Tests/CombatEntryIntegrationTests.cs` | 옛 "챕터 승리·패배는 이탈 안 함" 기대를 교체: 연결 정의 없음 보스 승리, 최종 챕터(`RunCompleted`), 실제 적 턴 공격으로 만든 패배 → 결과 확인 → EndBattle → `PreparingNewRun` → 새 Run 준비 → StageSelect 전환 요청. 포기는 자동 이탈 없음 유지 |

| 검증 항목 | 결과 |
|---|---|
| MSBuild(`ProjectL.Tests.Framework`, 새 파일 포함) | 오류 0, 새 경고 0(기존 UI 경고만) |
| Unity Mono 관리 코드 실행기(Unity Test Runner 아님) | `CombatPlanTests`·`CombatEntryIntegrationTests` 59개 중 58 통과. 새 테스트 전부 통과. 실패 1: `NextEntry_UsesNextTargetAndRetainsOldResultWithoutTreatingItAsAcceptance` — `5376b174`(호진, 10-06)가 StageSelect 진입에 아티팩트 보드 설정을 필수로 만들어 테스트 DB fixture에서 `StageLifeCycle.OnEnter`가 예외. 이번 변경과 무관 |
| Unity 컴파일·EditMode | 사용자 확인(2026-10-07): 컴파일 오류 없음, 테스트 모두 통과 |

**남은 것:** Title 화면이 생기면 Run 종료 목적지 교체, 운영 로드아웃 선택, Run 시작 재화 DB, 포기 API. 최종이 아닌 챕터 클리어의 화면 정책은 범위 밖으로 남는다(현재는 Core가 다음 챕터를 선택한 경우만 초기화 없이 StageSelect).

**정정·후속(2026-10-07):**

- Core `5f2d0381`이 새 Run 시작 서비스 `IRunStartService.StartNewRun(loadoutKey)`(이전 Run 정리·시작 로스터·시작 Run 재화를 한 번에)와 무인자 `AbandonBattle()`(`BattleAbandonResult`)를 제공했다. 병합 정리 `29a7bbbe`에서 Combat `NewRun`이 Stage와 같은 `NewRunJob` 하나로 바뀌었다(SSOT: 새 Run 시작 규칙은 Core 한 곳).
- 이에 따라 쓰지 않는 `CombatNewRunRosterJob.cs`(+`.meta`)와 그 테스트를 지웠다. `CombatPlanTests`의 로스터 대역·Buffer 생성 보조도 같이 정리했다.
- 병합에서 `CombatEntryIntegrationTests.cs`가 K21 이전 판으로 되돌아가 K21 테스트 4개(연결 정의 없음 보스 승리, 최종 챕터, 실제 패배, 포기 자동 이탈 없음)와 fixture 옵션이 빠져 있었다. `b7f41128` 판으로 복원하고 포기 호출만 `AbandonBattle() == Abandoned`로 맞췄다.
- 테스트 DB fixture에 아티팩트 보드 설정(3×3)을 넣어 `5376b174` 이후 실패하던 `NextEntry_…`를 고쳤다.
- 남은 일의 순서·담당은 [View 계획 §7.56](2026-10-01-combat-view-plan.md)이 원본이다. S3는 골격 호출자(`CompleteChapter`)가 이미 있고 Core의 챕터 연결 정의 원본을 기다린다.

| 검증 항목 | 결과 |
|---|---|
| MSBuild(`ProjectL.Tests.Framework`) | 오류 0. Unity가 만든 csproj에 지운 파일이 남아 있어 확인용 targets에서만 Compile Remove(Unity 재생성 시 해소) |
| Unity Mono 관리 코드 실행기 | `CombatPlanTests`·`CombatEntryIntegrationTests` 59/59 통과(`NextEntry_…` 포함) |
| Unity 컴파일·EditMode | 사용자 확인(2026-10-07): 컴파일 오류 없음, 테스트 모두 통과 |

### 6.17 Phase 6B S1 자발적 포기 연결 (2026-10-07)

- 사용자 결정(설계 §6.5·K22, 추천 A1~A5 그대로): 공개 API `ICombatExitService.RequestAbandon()`(목적지 인자 없음, Live 전용), Core `AbandonBattle()`은 이탈 순서가 직접 한 번 호출, 대기 중 실제 승패가 먼저 기록되면 그 승패로 정상 이탈, 이미 제출된 명령은 끝까지 처리.
- Core 계약(`5f2d0381`): `BattleAbandonResult { Abandoned, AlreadyCompleted, RunEnded, NoActiveBattle }`. `Abandoned`는 Completion(패배·자발적 포기)과 Run 결과 Defeat만 기록하고 일반 `CombatResult`는 게시하지 않는다.

| 파일 | 변경 |
|---|---|
| `Combat/CombatExitSequence.cs` | `ICombatExitService.RequestAbandon()` 추가, 상태 `Abandoning`. 공통 시작 `Begin`. Draining에서 Runner Idle·표시 완료 뒤 `Abandon()`: 직전 결과·순번 기억 → `AbandonBattle()` → `Abandoned`면 포기 Completion·순번 불변·결과 보존 확인, `AlreadyCompleted`면 기존 최종 결과 확인, 그 밖은 오류. EndBattle 뒤 보존 확인을 "기억한 결과(없을 수 있음)·순번" 기준으로 일반화. 포기 중 `RequestNormalExit`은 false, 정상 이탈 중 `RequestAbandon`은 false |
| `Combat/CombatLifeCycle.cs` | `RequestAbandon()` 공개(Live·초기화 뒤만). Run 종료 목적지 `RunEndDestination`(현재 Stage, Title 화면이 생기면 교체) |
| `Tests/CombatEntryIntegrationTests.cs` | 배치 국면 포기 → 포기 Completion·결과 순번 불변 → 확인 → EndBattle → 새 Run → StageSelect 요청 / 등록된 명령을 먼저 처리·표시한 뒤 포기 1회 / 등록된 공격이 먼저 승리 → 승리 표시·새 Run 없이 전환 / 기록 모드·진입 전·정상 이탈 중 거절과 중복 요청 합치기. `WinBattle`에 마지막 공격 등록 직후 콜백 추가 |

| 검증 항목 | 결과 |
|---|---|
| MSBuild(`ProjectL.Tests.Framework`) | 오류 0, 새 경고 0 |
| Unity Mono 관리 코드 실행기 | `CombatPlanTests`·`CombatEntryIntegrationTests` 63/63 통과 |
| Unity 컴파일·EditMode | 사용자 확인(2026-10-07): 컴파일 오류 없음, 테스트 모두 통과 |

**미검증으로 남긴 것:** Core `AbandonBattle()` 예외(DB 정의 오류) 주입은 fixture에 정의 없는 배치 Artifact를 만들기 어려워 테스트하지 않았다. 예외는 기존 이탈과 같은 `Tick`의 catch → 오류 경계 1회 보고 경로를 탄다(EndBattle·전환 실패 테스트가 같은 경로를 검증).

**다음:** View 세션 4-V(포기 버튼·확인창 → `LifeCycles.CombatExit.RequestAbandon()`). 버튼은 `ExitStatus == Idle`이고 전투가 진행 중일 때만 누를 수 있게 하고, false면 아무것도 하지 않는다.
