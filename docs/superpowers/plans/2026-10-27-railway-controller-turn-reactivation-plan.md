# 기차 기믹 재활성화 — Task 계획

작성일: 2026-10-27. 상태: **A~C 코드 반영·D 기존 View 연결 인수. 최신 B~D의 컴파일·테스트·Unity 확인은 사용자 요청으로 일괄 대기**.

정책 원본: [설계서](../specs/2026-10-27-railway-controller-turn-reactivation-design.md). 발동 주기·대상·순서를 이 문서에 중복 정의하지 않는다.

## 1. 진행 규칙과 소유권

- 후속 사용자 지시로 Core 수정과 A를 진행했고, A 보고 뒤 **B 승인 → 중간 검증/중단을 건너뛰고 D까지 진행**하도록 범위가 확장됐다. 이번 B~D 검증은 사용자가 마지막에 한꺼번에 확인한다.
- 설계 R1~R3과 P1~P4는 승인됐다. 기존 5/5·밤 사이 누계 유지·회수 전 판정이 확정 기준이다.
- 사용자 지시의 Core 수정 예외를 승인된 A~C 구현 범위에 적용하고 Core 인계에 기록한다.
- 기본은 Phase별 보고·중단이다. **이번 B~D는 사용자의 명시적 지시로 중간 중단·검증을 생략**하고 D 연결 뒤 보고한다. 정책 변경이 필요하면 먼저 질문한다.
- 커밋·push는 사용자 담당이다. 기존 작업 트리 변경과 옛 Missing Script 에셋을 보존한다.
- 현재 다른 작업이 있는 Core 경계 인계·약한 지반 계획을 덮어쓰지 않는다. 실제 전달 시 같은 담당자와 편집 범위를 조율한다.

## 2. Phase 개요

| Phase | 책임 | 산출물 | 종료 리뷰 |
|---|---|---|---|
| A | Core 담당 | 종료 사실·기차 설정 전달·명칭 이전 | 전이/직렬화/기존 진행 유지 |
| B | Core 담당 | 기차 규칙·직접 사망·공통 정산 | 판정·후속 정산·사건 계약 |
| C | Core 담당 | 두 종료 경계 연결·원자적 게시 | 밤 회수·전투 종료·중복 방지 |
| D | View 담당, Core 인수 후 | 기존 사건 재생 호환 확인·필요 최소 연결 | 실제 Unity 재생·문서 인계 |

A/B만으로 운영 기차가 발동한다고 보고하지 않는다. C에서 두 경계를 연결한 뒤 운영 동작이 생기며, D의 시각 검증은 별도다.

## 3. Phase A — 계약·설정·이전

### 사전 조건

- [x] A0. 설계 P1~P4의 승인 상태와 Core 담당 범위를 기록한다.
- [x] A1. 아래 경로의 생성·보관·게시·소비 흐름을 실제 코드로 추적하고 정확한 수정 파일 목록을 기록한다. 누락한 계약이 있으면 Phase 시작 전에 계획을 보완한다.

```text
CombatStageData / CombatStageDetail.RailwayData
  → BattleDefinitionAdapter / RailwayGimmickSpec / StageConfig
  → BattlePreparationCalculator / 준비 사실 / CombatStateBuffer
  → CombatCommandInput / CombatCommandState / CombatTurnFacts
  → 종료 계산 → CombatCommandTransition → Buffer 검증·게시
```

### Task

- [x] A2. 설계 §4.1의 종료 누계를 턴 사실·생성자·복사·SameValues·초기화에 반영한다. 다음 턴·밤·배치 복사에서 값을 보존한다.
- [x] A3. Buffer의 실제 전이 검증 위치를 확인해 정상 종료 +1/기타 보존/새 전투 0을 검증한다. 기존 시작 순번·플레이어 종료 수 의미는 유지한다.
- [x] A4. 불변 레일·기차 설정을 현재 준비 경로에서 명령 계산까지 전달한다. **기존 CombatBattleFacts 경로가 있어 생산 코드 추가 없이 비어 있지 않은 설정의 조립·복사 시험으로 확인했다.**
- [x] A5. SO·순수 Data·Spec·Adapter의 UnitTurns 명칭을 ControllerTurns로 이전한다. 직렬화 이름은 FormerlySerializedAs로 보존한다. 도구·JSON·테스트 생성자 호출도 범위 검색한다.
- [x] A6. Normal_A/B/C·Boss·Test의 F/I·enabled·레일·GUID는 **에셋 diff 없음**으로 보존 확인했다. YAML 직접 변경 없음. 실제 읽기/재저장은 A9 대기다. 도구 작업본 JSON 이름은 단위 중립적이라 유지한다.
- [x] A7. 새/이동 .cs가 있으면 .meta를 함께 생성·보존한다. 순수 계약에 Unity 참조를 넣지 않는다.

### 검증·종료 보고

- [x] A8. 계약 동등성·모든 운영 턴 복사·밤 경계·초기화 및 잘못된 누계 전이 테스트를 독립 Mono/NUnit에서 실행했다(§8).
- [ ] A9. 기존 5/5 저장값을 새 명칭으로 읽는 실제 에셋/직렬화 검증과 `DataBaseAssetTests`를 실행한다. 새 경로만 만든 임시 객체 검사로 실제 이전 검증을 대체하지 않는다.
- [ ] A10. Unity 컴파일 및 기존 플레이어/적 진행 관련 EditMode 검사를 실행한다. **MSBuild 컴파일·독립 NUnit 진행/게시 검사는 통과, Unity 실행은 프로젝트 열림 잠금으로 대기**다. 기차 보류는 유지한다.
- [x] A11. 변경 파일·저장값 보존·검증 결과·기차 미연결 상태·Core 인계 기록을 정리했다. Phase A 코드 보고 뒤 **멈춘다**. A9/A10이 남으므로 전체 검증 완료로 표시하지 않는다.

## 4. Phase B — 기차 계산과 정산

### Task

- [x] B1. 현재 null stub과 공통 사망 정산 계약을 대조해 기차 계산 책임을 분리한다. 옛 unit-clock/Timeline 그래프를 복원하지 않는다.
- [x] B2. 설계 §3의 F/I/C 판정을 순수 계산으로 구현한다. 설정 없음/미발동과 잘못된 설정을 구분하고 후자를 조용히 무시하지 않는다.
- [x] B3. 현재 필드의 대상 집합을 한 번 확정하고 중복 없이 직접 사망 변경을 만든다. 구조물·대기석·이미 죽은 대상은 제외한다.
- [x] B4. 기차 사건·결정적 직접 사망 사건을 만들고 현재 공통 정산으로 영혼·깃발·아이템·보상·통계·승패를 연결한다. 환경 사망의 기존 귀속을 유지한다.
- [x] B5. 다수 직접 사망을 한 발동 묶음으로 정산한다. 첫 대상 정산으로 나머지 직접 대상이 누락되거나 같은 대상을 두 번 정산하지 않도록 한다.
- [x] B6. 피해자가 없는 발동도 기존 View 사건 계약을 만족한다. 새 사건 형식/서비스는 실제 계약이 필요할 때만 설계 변경 후 추가한다.

### 검증·종료 보고

- [ ] B7. F=1/I=1, F=5/I=5, F=2/I=3의 최초·반복·직전·직후를 검증한다. F/I 0 이하와 잘못된 레일은 준비 검증에서 실패한다.
- [ ] B8. 양 진영 대상·레일 중복 접촉·빈 대상·비레일·대기석·기존 사망·구조물 제외를 검증한다. 운영 다칸 허용 범위를 넓히지 않되 순수 대상 교집합 규칙의 중복 제거를 확인한다.
- [ ] B9. 주인공 사망·목표 적 사망·동시 사망의 기존 승패 우선순위, 직접 사망별 보상/통계 1회 및 공통 후속 정산을 검증한다.
- [ ] B10. 출력 사건이 현 `CombatRailwayContactPlayback`의 직접 사망 묶음 계약과 일치하는지 검사한다. 일반 공격 사건으로 위장하지 않는다.
- [x] B11. 판정·정산 구현 내역을 §9에 기록했다. 사용자 지시로 이 지점의 검증·중단은 생략하고 C/D 연결로 진행했다.

## 5. Phase C — 종료 경계·게시 연결

### Task

- [x] C1. EndPlayerTurn의 기존 허용/신선도 검사 뒤, 이번 종료 누계로 기차를 검사한다. 기차가 끝낸 전투에는 적 턴 시작을 만들지 않는다.
- [x] C2. EndEnemyPhase에서 적 전체 국면 종료마다 검사한다. 적 AI의 개별 행동·진행 Tick에는 누계를 올리지 않는다.
- [x] C3. 적 마지막 국면의 기차는 밤 종료 정리/회수 전에 수행한다. 계속되는 전투에서만 기존 밤 종료·아이템 만료·회수를 적용한다.
- [x] C4. 공통 기차 계산을 두 경계에서 사용하고 종료 결과와 기차 정산 결과를 기존 투영/합성 경로로 결합한다. 중간 세계를 게시하지 않으며 종료된 단계 뒤에 다른 단계를 붙이지 않는다.
- [x] C5. 기존 결과 순번·expected/final·pending result 게시 경로를 유지하고 중간 결과를 게시하지 않도록 연결했다. 취소·실패 시 원자성 실행 검증은 C8/C9 대기다.
- [x] C6. 기차에 대한 보류 검사를 새 지원 동작으로 바꾼다. 같은 테스트에 있는 다른 보류 기능의 검증은 유지한다. 구형 단위 테스트를 통째로 복원하지 않는다.
- [x] C7. 기존 Core 보류 설계·인계에는 새 문서 링크와 대체 범위만 추가하고 과거 검증 기록을 성공으로 다시 쓰지 않는다.

### 통합 검증

| 시나리오 | 검증 |
|---|---|
| 플레이어 종료 → 적 종료 연속 진행 | C가 각각 +1, F/I에 해당하는 경계만 1회 발동 |
| 적 여러 명/행동 없음 | 개별 행동 수와 무관하게 적 전체 종료만 +1 |
| 중복·거절·오래된 종료/반복 Advance | 누계·사건·사망·보상이 중복되지 않음 |
| 종료 전 공격으로 이미 승패 확정 | 가상 종료·추가 기차 없음 |
| F=16인 첫 밤 마지막 적 종료 | 회수 전 피격, 생존 시에만 밤 종료·회수 |
| F=17 또는 반복 시점이 다음 밤 | 배치/재진입 중 카운트 없음, 누계 보존 |
| 기차에 의한 전투 종료 | 다음 턴/AP 재충전·회수 실행 없음, 최종 결과는 게시 |
| 원본 불일치/게시 실패 | 누계만 증가하거나 사망 일부만 남지 않음 |
| 기차 미설정/꺼진 스테이지 | 기존 턴·밤 진행 유지, 다른 보류 기능 활성화 없음 |

- [ ] C8. 위 시나리오를 실제 Core 공개 경로·Buffer 게시 통합 테스트로 실행한다. 내부 계산기 호출만으로 통합 완료를 주장하지 않는다.
- [ ] C9. 승패·보상·Run 결과 게시 관련 기존 회귀를 영향 범위에 맞춰 실행한다. 실패는 신규/기존/미확인으로 증거와 함께 구분한다.
- [ ] C10. Unity 컴파일·EditMode 결과·사건 예시·남은 Unity 시각 확인을 보고하고 **멈춘다**.

## 6. Phase D — View 인수·Unity 확인

- [x] D1. Core에서 제공한 실제 결과를 기존 Railway 재생 경로로 소비한다. 형식이 맞으면 View 코드는 변경하지 않는다. 계약 차이가 있으면 구현 전 설계 리뷰로 돌아간다.
- [ ] D2. 최초 플레이어 측 발동·적 측 반복 발동·피해자 없음·양 진영 다수 직접 사망의 실제 재생을 확인한다.
- [ ] D3. 접촉 순간 사망, 사망 중 열차 계속 이동, 열차+모든 직접 사망 완료 후 후속 사건 진행, 직접 사망 재생 중복 없음, 소리·기차 카메라 기존 동작을 확인한다.
- [ ] D4. 마지막 적 국면에서 기차가 먼저 보이고 이후 아침·회수로 진행하는지, 기차로 승패가 나면 최종 결과 재생 뒤 기존 이탈 정책을 따르는지 확인한다.
- [ ] D5. 재생 중 Cancel·씬 해제·전투 재진입에서 대기·이펙트·열차·사망 대여 자원이 남지 않고 늦은 완료가 다음 전투를 건드리지 않는지 확인한다.
- [ ] D6. 기존 기차 접촉/사건 순서/결과 재생 관련 테스트와 설정 이전 후 실제 에셋 검사를 실행한다. Test Runner 미실행과 수동 확인은 별도 기록한다.
- [x] D7. 설계·계획 상태와 Core 경계 인계를 실제 완료 범위로 갱신하고 공유 문서를 준비한다. 외부 발송은 별도 사용자 지시가 있어야 한다.
- [x] D8. 최종 변경과 검증 미실행 상태를 정리하고 **멈춘다**. 커밋·push하지 않는다.

## 7. 리뷰 체크리스트

- [ ] 종료 누계·설정·발동 여부의 SSOT와 파생값 분리가 유지되는가?
- [ ] 양쪽 턴 종료에 살상 규칙이 복제되지 않고 조립/순서와 기능 구현이 분리되는가?
- [ ] SOLID 5원칙·JMH/FW·P1/P6/P7을 설계 §7의 근거로 점검했는가?
- [ ] 기존 Buffer·결과 게시·정산 계약을 우회하지 않고 적 마지막 턴의 회수 전 판정이 보장되는가?
- [ ] 기차 변경 때문에 관련 없는 토네이도·약한 지반·UI·FieldCamera 구현까지 바꾸지 않는가?
- [ ] 공통 계약 변경에 필요한 통합 검증과 무관한 기능의 관성적 전체 검증을 구분했는가?
- [ ] Core 담당 범위·공유 문서 편집 충돌·Phase 종료 중단을 준수했는가?

## 8. 실행 기록

### 2026-10-27 — 문서 작성

- 작성: 설계 초안과 이 Task 계획.
- 읽기 확인: 현 턴 종료·밤 회수·기차 stub·준비 설정·명령 합성·사건 계약·이전 기차 View 설계.
- 구현/에셋 변경: 없음. 컴파일·Unity·EditMode 실행: 없음(문서 작성 단계).
- A~D: 전부 미착수. 다음 리뷰 대상은 설계 P1~P4와 Core 작업 범위다.

### 2026-10-27 — Phase A 코드 반영·검증

**승인:** 사용자가 제안 기준을 승인하고 5/5·밤 사이 누계·회수 전 판정을 명시했다. Core 수정 허용, Phase A 뒤 보고·중단 지시를 받았다.

**생산 코드 변경 파일(기존 파일, GUID 유지):**

| 경로(Assets/ProjectL/Script/ 아래) | 변경 |
|---|---|
| `Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatTurnFacts.cs` | 종료 누계·음수 거절·SameValues |
| 같은 CoreCaculator `Application/Combat/CombatTurnCalculator.cs` | 성공한 양 진영 종료 +1, 진입/시작/밤 복사 보존. 기차 발동 호출 없음 |
| `Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs` | 초기 0, 종료별 +1·기타 보존 검증. 기존 원본/최종 게시 경계 유지 |
| 같은 CoreCaculator `Contracts/Commands/StageGimmicks/RailwayGimmickSpec.cs` | ControllerTurns 명칭·기존 양수 검증 유지 |
| `Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageDetail.cs` | RailwayData 명칭·검증 참조 변경 |
| 같은 폴더 `CombatStageData.cs` | 새 직렬화 필드명·FormerlySerializedAs·단위 설명 |
| `Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/BattleDefinitionAdapter.cs` | 새 기차 설정 프로퍼티 사용 |
| `Tool/Editor/CombatStageSoAssets.cs`, `CombatLayoutWindow.SoMode.cs` | SO 필드 접근 이름·단위 표시 |

추가 계약 저장소는 없다. 기존 `CombatPreparation` → `CombatStateData` → `CombatInputAssembler` → `CombatBattleFacts` → 입력 파생 복사 경로를 시험했다. 종료 누계는 양쪽 종료와 초기화만 변경하고 나머지 생산 생성 지점 네 곳의 보존을 대조했다. Framework 등록·Factory·View 재생기·다른 보류 기능은 변경하지 않았다.

**시험 변경:** 순수 `ControllerTurnCountTests` 3건, CoreIntegration `ControllerTurnCountPublicationTests` 8건, Framework `RailwayControllerTurnAuthoringTests` 6건을 추가하고 .meta를 함께 만들었다. 실제 SO 명칭에 의존하던 `InGameDataTableTests`·`HojinCombatTornadoTerrainAssetTests`의 Railway 프로퍼티 참조만 이전했다. 토네이도 설정 이름은 유지했다.

**검증 결과:**

- VS 2022 MSBuild: Core·CoreIntegration·Framework 테스트 어셈블리와 Tool.Editor 컴파일 성공(오류 0). 신규 시험도 스크래치 targets로 Compile에 포함했다. 기존 미할당 필드 경고가 있으며 이번 변경 파일의 새 경고는 없다.
- 새로 빌드한 어셈블리를 Unity 부속 Mono + NUnit으로 실행: 순수 Core **3695/3695 통과**, 건너뜀 0. 그 안에 신규 누계 3건과 기존 진영 시작/종료·8턴 밤·입력 복사·보류 기능 검사가 포함된다.
- 신규 게시 통합 **8/8 통과**. 실제 서비스/Buffer 경로에서 양측 증가·밤 16 보존/다음 밤 17·새 준비 0·미리보기/이동 비증가·잘못된 양측 종료 누계의 원자적 거절/정상 재시도·설정 조립/복사·기차 5/5 미발동을 확인했다.
- 추가 게시/서비스/완료 회귀 묶음은 **45건 중 44 통과, 1 실행 환경 실패**다. 실패는 기존 `MovementPublicationTests.ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime`의 첫 `ScriptableObject.CreateInstance`가 독립 Mono에서 Unity native internal call을 찾지 못한 것이다. 게임 로직 실행 전 실패이며 코드 회귀 판정 근거로 쓰지 않는다. 통과 44건에는 신규 8건이 포함된다(중복 합산 금지).
- Unity EditMode 시도는 현재 사용자가 연 프로젝트의 다른 인스턴스 잠금으로 중단됐다(`HandleProjectAlreadyOpenInAnotherInstance`). 기존 에디터를 종료하거나 잠금을 지우지 않았다. **신규 SO 실제 이전·재저장 6건, DataBaseAssetTests, 위 native 회귀 및 Unity 내 컴파일/실행은 미확인**이다. 외부 컴파일/NUnit을 Unity Test Runner 통과로 표시하지 않는다.
- DB SO·프리팹·씬 diff 없음, 새 시험 meta GUID 중복 없음, 코드 공백 검사 통과. 이전 저장값은 그대로이며 실제 Unity 읽기는 대기다.

증거 위치: `.utmp/railway-controller-turn/phase-a/`의 `build-*.log`, `pure-regression.xml`, `integration-focused.xml`, `integration-regression.xml`, `focused.log`(프로젝트 잠금). 스크래치 산출물은 ignored 폴더에 두었다.

**원칙 점검:** 누계는 턴 사실의 단일 원본, 기차 설정은 기존 불변 준비 경로다(SSOT). 턴 순서와 기능 구현은 분리하고 기차 stub은 유지한다(SRP/OCP). 기존 명령·거절·입력 복사 계약과 좁은 의존성을 유지한다(LSP/ISP/DIP). FW-001~003 등록 변경 없음, FW-004 기존 거래 경계를 통과하고 결과·수명 계약을 바꾸지 않았다. 무관한 토네이도·약한 지반·FieldCamera·Architecture는 편집하지 않았다.

작업 중 열린 에디터의 FMOD 캐시(`Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`) 변경도 관찰됐다. 이번 구현에서 편집하지 않았으며 되돌리지 않았다.

**중단 상태:** Phase A 코드 구현 완료, A9/A10 Unity 검증 대기. B~D는 미착수다. 기차는 아직 발동하지 않는다. 커밋·push·외부 메시지 발송 없음.

## 9. B~D 연결 기록과 일괄 확인 목록

### 사용자 지시와 상태

2026-10-27: 사용자가 Phase B를 요청한 뒤, 작업 도중 **"검증 건너뛰고 PhaseD 까지 진행해 검증 마지막에 한번에할게"**라고 범위를 확장했다. 이에 B/C 계산·종료 경계 연결과 D의 기존 View 사건 경로 인수까지 진행했다. 기본 Phase별 검증·중단 대신 이번 묶음은 마지막 사용자 확인을 기다린다.

**이번 변경의 컴파일·테스트·Unity 실행은 하지 않았다.** 위 §8의 Phase A 통과 기록은 당시 상태의 증거이며 최신 B~D가 통과했다는 뜻이 아니다. 체크된 B/C Task는 코드 반영 상태, 체크되지 않은 시험·화면 Task는 최종 일괄 확인 대기다. D는 별도 View 코드 변경이 필요 없는 기존 경로 연결이며 시각 인수 완료는 아니다.

### 변경 내용

1. `CombatGimmickCalculator.TryAdvance`: F/I/C 발동 판단, 레일 유효성 검사, 살아 있는 양 진영 필드 캐릭터의 레일 교집합 집계, ID 순 직접 즉사, 피해자 없는 기차 사건을 구현했다. 카운터·다음 발동 상태·Timeline 실행 그래프는 추가하지 않았다. 기차 ID는 준비 목록 위치 +1이다.
2. 즉사 변경을 모두 만든 뒤 기존 `CombatCommandCalculator.Finish`로 정산한다. `RailwayGimmickActivationSnapshot` 뒤 직접 `Died`가 연속하므로 기존 접촉 재생이 소비할 수 있는 형식이다. 아이템·디버프·승패·클리어 보상은 공통 경로를 따른다. 현재 보류 중인 영혼 생성·깃발 드롭을 새로 켜지 않는다.
3. 공통 귀속은 원천이 없으면 명령 ActorId로 돌아가므로, 기차 내부 정산 입력만 ActorId를 비우고 원본 입력으로 합성한다. 가상의 공격자·처치 귀속을 만들지 않는다. **이 환경 무귀속 사망은 기존 공통 규칙상 처치 골드·경험치·그 목록 기반 Run 처치 수가 0이다.** 클리어 보상은 승리 시 기존대로다. 통계 정책의 별도 확대는 이번 범위에 넣지 않았다.
4. `CombatTurnCalculator.EndPlayerTurn`과 `EndEnemyPhase`는 누계 증가·종료 사실 뒤 같은 계산기를 호출한다. 마지막 적 종료에서도 우선 Combat/StartingPlayer의 종료 중간 사실로 기차를 정산하고, 계속되는 경우만 별도 `EndNight`에서 PreparationIndex 증가·아이템 만료·회수를 한다. 종결이면 회수/밤 종료/다음 시작 없이 최종 결과를 반환한다.
5. 기존 `CombatCommandComposition.Project/Merge`를 사용해 원본 대비 최종 변경 하나만 반환한다. Buffer 거래·결과 확인·자동 적 개별 행동 경로는 새로 만들거나 우회하지 않는다.
6. D 연결: 기존 `CombatEventTypeTable`의 Railway 분류, `CombatSceneEventPlayback.BeginPresentation/BeginEvent`, `CombatRailwayContactPlayback.Prepare/Begin`의 접촉 사망·열차/모든 사망 완료 대기, 중복 사망 소비 방지, 기존 취소·카메라/소리 경로를 재사용한다. View·Factory·씬·프리팹 수정은 없다.

### 후속 실행을 위해 준비한 시험 소스

- `Assets/Tests/EditMode/RailwayGimmickTests.cs`(+ .meta): F1/I1·F5/I5·F2/I3 경계, 양 진영 직접 사망의 연속 사건·무귀속, 공통 승패 우선순위, 잘못된 설정, 플레이어 종료 연결, 마지막 적 종료의 기차/회수/종결 순서 사례를 추가했다. **미실행**이다.
- 기존 `CoreIntegration/ControllerTurnCountPublicationTests`의 설정 전달 사례는 C=4까지 미발동, 다음 플레이어 종료 C=5에서 레일 위 메인 사망·기차 1회까지 보도록 갱신했다. **변경 뒤 미실행**이다.
- `BossHitCounterTests`·`BossObjectiveTests`·`ExperienceAttributionPathTests`의 기차 정의가 없는 사례는 보류라고 잘못 부르지 않도록 시험 이름만 바꿨다. 다른 보류 기능 검사 내용은 유지했다.

### 최종 일괄 확인 순서

1. Unity 컴파일 및 A9의 SO 이전/재저장 6건·`DataBaseAssetTests`를 먼저 확인한다. 기존 5/5·enabled·레일/외형 참조 보존을 확인한다.
2. 신규 `RailwayGimmickTests`, `ControllerTurnCountTests`, `CoreIntegration.ControllerTurnCountPublicationTests`를 실행한다. 오래된 요청/중복 재적용·개별 적 행동·미리보기 비증가·다칸 대상 중복 제거·대기석/구조물 제외도 B/C 체크리스트대로 확인한다.
3. 기존 밤·적 AI·공통 사망·아이템/보상·완료 게시 회귀를 실행한다. A의 독립 Mono에서 실행 환경 실패였던 ScriptableObject 사례는 Unity 안에서 다시 확인한다.
4. Unity 운영 5/5에서 **내 세 번째 턴 종료(누계 5)**와 **적 다섯 번째 턴 종료(누계 10)** 발동을 확인한다. 16을 지나 다음 밤에도 누계가 유지되는지 본다. 마지막 적 종료 자체의 발동은 별도 시험 설정 F=16으로 확인하고 운영 에셋 수치는 바꾸지 않는다.
5. 피해자 없음·양 진영 다수 직접 사망·메인/지정 보스 동시 사망·열차 접촉 순간의 기존 사망·소리/카메라를 확인한다. 마지막 적 국면에서는 기차 뒤 회수, 종결이면 회수 생략인지 본다.
6. 열차와 모든 직접 사망 연출의 완료 대기·중복 사망 없음·Cancel/Release·재진입·최종 결과 재생 후 기존 Run 이탈 정책을 확인한다.

### 원칙 및 범위 기록

종료 누계는 기존 턴 사실 한 곳, 설정은 기존 불변 준비 입력 한 곳이다(SSOT). 종료 계산기는 순서·기차 계산기는 발동/대상·공통 Finish는 정산·View는 재생을 소유한다(SRP). 두 종료 지점에 살상 규칙을 복제하지 않았고 범용 스케줄러/새 서비스·인터페이스를 만들지 않았다(OCP/ISP). 원본 입력·거래와 기존 비기차 경로 계약을 유지하며 결과 형식을 재사용한다(LSP/DIP). FW 등록·Builder·Factory 변경이 없고 Buffer 우회/중간 게시도 없다. 기능 변경 범위는 기차 계산과 종료 조정에 머물며 FieldCamera·Architecture·다른 보류 기능을 수정하지 않았다. 이는 구현의 책임 배치 기록이며 실행 검증 통과 주장은 아니다.

Core 변경과 다른 보류 정책 유지 사항은 경계 인계 §5.57에 기록했다. 최신 구현 검증은 사용자 일괄 확인 대기 상태로 멈춘다. 커밋·push·외부 메시지 발송 없음.
