# 전투 골격 Phase 6B — Core 요청사항

- 작성일: **2026-10-05**
- 작성: Combat 골격·View / 대상: Core 담당
- 코드 기준: `feature/Yonggeun/Stage`, `bfe63a27` (Core 인수 커밋은 `e0b02cc7`)
- 상태: **2026-10-05 사용자 전달 방향 반영. 재화 보완본의 외부 전달 미확인, Core API 구현·인수 미완료.**
- 이 문서가 현재 Core 요청의 원본이다. 이전 [DB 경계 인계 §13.8](2026-09-24-view-core-data-boundary-handoff.md#138-골격-6b-진행에-따른-core-요청-정리-2026-10-05-미전달)의 요청 내용을 분리했다.

6A/B3·View E/E2/A2 및 `5078ee0e`의 F1·정상 F2 기반은 사용자 검증 완료 확인으로 인수했다. 일반 승리 복귀 호출자도 추가했으며 Core 생산 코드는 수정하지 않았다. 최신 상태는 [골격 계획 §6.14](../plans/2026-09-30-combat-flow-skeleton-plan.md#614-phase-6b-일반-승리-복귀-호출자-연결-2026-10-05)를 따른다. **F1은 완료됐고 아래 Core 요청의 회신 대기가 아니다.** 포기 공개 API는 다시 확인했으나 기존 payload 서명만 있어 요청 1을 유지한다.

## 1. 필요한 구현 세 가지

| 순서 / 필요한 시점 | 현재 코드와 막히는 이유 | Core에 요청하는 작업 | 완료·인수 조건 |
|---|---|---|---|
| **1. F2 자발적 포기 연결 전** | `ICombatProcessingService.AbandonBattle(ArtifactLoadoutSnapshot)`는 호출자가 정산 입력을 제공한다. View는 로드아웃·Artifact DB를 조립할 수 없다. 서비스 내부에는 `ReadArtifactLoadout()`이 존재한다 | 현재 Core Buffer·DB로 정산 입력을 읽어 포기하는 **공개 경계** 제공. 무인자 overload는 제안이며 이름·서명은 Core가 확정. 계산기/테스트용 기존 payload 경계를 무조건 삭제할 필요는 없음 | 실제 API·서비스 노출과 성공/거절/예외/중복 호출 의미 제공. 배치 Artifact 효과 반영, 포기 Completion·캐릭터·진행 정산 1회, 기존 승패 보존. 잘못된 DB에서 부분 정산 없이 실패하는지 검증 |
| **2. fixture 없는 운영 전투 준비 전 — T1/G7** | `DatabaseBattleInitializationSource.Read`는 `BattleDefinitionAdapter.Read(...)` 뒤 결과를 반환하지 않고 무조건 예외. A2의 지정 방식 테이블·유닛 참조·query는 이미 제공됨 | [DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달)의 `TargetingMethodKey` 조회 → 지정 방식·공격 프로필 → **전체 BattleInitialization** 조립/검증 → 기본 source 반환 연결. 옛 enum을 새 enum으로 추정 변환하거나 예외만 삭제하지 않음 | 실제 카탈로그·선택된 chapter/node·보유 로스터로 첫/다음 전투 준비 통과. 유닛 수치·필드·구조물·적·Economy·공격 방식이 실제 데이터와 일치. Single/Direction 공격·미리보기 통과. 잘못된 참조는 설치/선택 게시 전에 실패 |
| **3. Stage 골드·Run 재화 분리 및 운영 초기화 인수 전** | 드로우·상점·회복이 동일 CurrencyBuffer를 사용하며 Stage InitialGold를 운영 잔액에 설치하지 않음. 기존 분리·보존 규칙과 불일치 | **Stage 골드와 Run 재화를 별도 원본으로 분리**하고, 각 초기 금액은 SO/DB에서 Core가 읽어 해당 수명 시작에 한 번 설치. 기존 확정 정책과 현 구현의 차이를 §1.1 기준으로 확인·수정 | 드로우는 Stage 골드만, 상점·회복은 Run 재화만 차감. 새 Stage/새 Run/국면 변경/화면 재진입/종료에서 두 잔액의 초기화·보존·실패 원자성 검증. View 직접 지급·초기화 없음 |

**진행 관계:** 1번은 F2의 포기 분기를 막는다. 정상 종료의 전환/표시 계약은 골격·View가 따로 정할 수 있다. 2·3번은 운영 인수 조건이며 fixture 기반 F1/F2 수명 검증을 막지 않는다. API 이름만 만든 빈 구현이나 개발용 Buffer 직접 쓰기는 완료 근거가 아니다.

### 1.1 요청 3 상세 — 두 재화의 원본·초기화·보존

**확정 지시:** 초기 금액은 SO/DB를 원본으로 사용한다. View는 지급·잔액 초기화·InGame DB 조회를 하지 않는다. 옛 구현의 타입·API를 부활시키라는 요청이 아니라, 아래 **사용자 확정 게임 규칙을 현 Buffer/Core 구조가 보존하는지** 확인하는 요청이다.

| 구분 | Stage 골드(전투 안) | Run 재화(스테이지 사이) |
|---|---|---|
| 사용처 | 수동 전투 드로우 | 상점 구매·재뽑기, 회복 |
| 초기 금액 원본 | Stage가 참조하는 `StageEconomyDataSO.initialGold` → 불변 `StageEconomyData.InitialGold`/`StageEconomySpec` | Run 시작 설정 SO/DB. 현재 새 DB 구조에서 대응 필드·query·초기화 경로를 찾지 못했으므로 Core가 실제 소스 위치/누락 여부를 회신 |
| 최초 지급 | 새 Stage 세션 준비에 해당 설정으로 새 잔액 생성. 초기 유닛 지급은 무료 | 명시적 새 Run 생성에 해당 설정으로 새 잔액 생성 |
| 유지 | 같은 Stage의 배치↔전투·낮/밤·화면 재연결에서 추가 지급·초기화하지 않음 | 같은 Run의 Stage 이동·상점·회복·화면 이탈/복귀에서 유지 |
| 종료·다음 시작 | Stage 수명 종료 시 해당 잔액 수명도 종료. 다음 새 Stage는 이전 잔액을 이어받지 않고 그 Stage 설정으로 시작 | 종료된 Run의 최종 잔액은 결과 조회에 보존하고 변경을 잠금. **명시적 새 Run**만 초기값 적용. 결과 화면 이동만으로 새 Run을 만들지 않음 |
| 서로의 영향 | 드로우 성공·실패 모두 Run 잔액 불변 | 상점·회복 성공·실패 모두 Stage 골드 불변. 두 재화의 자동 교환·이월 없음 |

**기존 확정 근거:**

- [Stage 골드·드로우 인계](2026-09-19-stage-gold-and-draw-view-handoff.md): InitialGold는 콘텐츠 값, 새 Stage마다 새 잔액, 국면 변경 시 유지, 초기 지급 무료, 드로우가 Run 재화에 영향 없음.
- 드로우 계획 §22 (Git 이력: `2026-09-18-run-character-deck-stage-draw-implementation-plan.md`): 두 독립 재화·Stage 세션 수명·SO 값 복사와 잔액의 독립성.
- 상점 설계 (Git 이력: `2026-09-19-sub-character-shop-stage-design.md`): 상점 결제는 Run 재화.
- 회복 설계 §1·§4.1 (Git 이력: `2026-09-19-main-character-recovery-stage-design.md`): 사용자 확정(2026-09-19), 회복 비용은 Run 재화. Stage 골드와 분리.
- Run 종료·지갑 설계 §4·§7 (Git 이력: `2026-09-18-run-termination-wallet-core-design.md`): 화면 이탈은 Run 종료가 아니며 같은 Run 잔액 유지, 종료 결과 잔액 보존, 명시적 새 Run과 재진입 구분. 옛 생성자의 기본 0은 현재 운영 콘텐츠의 확정 금액으로 사용하지 않는다.

**현재 코드에서 확인한 불일치 (`e0b02cc7`):**

| 경로 | 확인한 사실 | Core 확인 요청 |
|---|---|---|
| `CombatProcessingService.ReadInput` / 드로우 계산 | `CurrencyReadSink.Amount`를 전투 Gold 입력으로 전달 | Stage 골드 읽기로 분리할 실제 API·snapshot 계약 |
| `CharacterShopService` | 같은 `CurrencyReadSink.Amount`로 구매·재뽑기 계산 | Run 재화 결제가 맞는지, 기존 공유 잔액 사용은 이전 중 누락인지 확인 |
| `CombatProcessingService.Recover` 및 회복 확정 | 같은 잔액으로 회복 비용 계산 | 회복 Stage 서비스까지 Run 재화 읽기/쓰기 일치 검증 |
| `BufferSystemGroup` | 드로우·상점·회복의 확정 모두 동일 `_currencyStore` 사용. `PrepareBattle`/`EndBattle`에 별도 Stage 잔액 수명 없음 | 읽기만 분리하지 말고 거래 확정·초기화·폐기까지 원본을 분리. 잘못된 준비/거절에서 부분 지급·차감 없음 |
| `StageEconomyDataSO` → `BattleDefinitionAdapter` | `InitialGold` 전달 입력은 있으나 운영 준비에서 잔액에 설치하는 경로 없음 | 실제 준비 성공 시 1회 설치 및 같은 SO를 공유하는 두 Stage의 잔액 독립 검증 |
| `TemporaryCoreRuntimeFactory` | Stage InitialGold를 공통 CurrencyBuffer에 직접 주입 | 개발 조립 이력이며 운영 해법/두 재화 분리 근거로 사용하지 않음 |
| `CombatViewSystem.ReadDisplayValues` | 현재 HUD도 공통 잔액 읽음 | Core가 분리된 읽기 계약을 제공하면 전투 HUD는 Stage 골드로 연결. View에서 소유/초기화하지 않음 |

**Core 인수 사례:** 서로 다른 테스트 설정(예: Stage 10·드로우 4, Run 30)을 SO/DB로 준비한다. 허용된 드로우 뒤 Stage만 6, 상점/회복 뒤 Run만 해당 비용만큼 감소해야 한다. 부족·거절·잘못된 정의에서는 두 재화와 관련 유닛/HP/보유 상태가 함께 보존돼야 한다. 같은 Stage의 국면 변경·표시 재연결은 6을 유지하고, 다른 새 Stage는 그 SO 초기값으로 시작하며 Run은 유지한다. 종료 Run 결과에는 최종 잔액을 보존하고, 명시적 새 Run만 새 설정값으로 시작한다. 반복 초기화·준비 실패·씬 진입 실패 뒤 재호출에서 중복 지급이 없어야 한다. 10/4/30은 검증용 값이며 운영 금액 제안이 아니다.

**남은 확인 사항만 분리:**

1. **구현 확인:** Run 초기 재화의 현 SO/DB 항목·query와 Core 공개 새 Run 시작 API는 무엇인가? 없다면 새 DB 항목/초기화 경계를 담당별로 제공한다. Stage용 InitialGold를 Run 초기값으로 재사용하지 않는다.
2. **구현 확인:** 현재 공통 CurrencyBuffer가 의도적 정책 변경인가, 이전 중 누락인가? 정책 변경 근거가 없다면 기존 분리 규칙을 보존하는 수정과 영향받는 드로우·상점·회복·HUD API를 회신한다.
3. **운영 입력 확인:** 현재 [StageEconomy_1.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Economy/Item/StageEconomy_1.asset)은 초기 골드 **200**, 수동 드로우 비용 **40**이다. 이 설정을 참조하는 Stage는 해당 값을 사용한다. 대응하는 Run 초기 금액 SO/DB는 찾지 못했으므로 **Run 설정 위치·저작값만** 확인한다. Stage 수치를 Run 설정이나 하드코딩 기본값으로 복제하지 않는다.
4. **범위 확인:** 같은 활성 Stage의 표시 재연결은 잔액 유지로 확정돼 있다. 이미 끝난 동일 노드의 재도전이나 앱 종료 후 복구는 새 Stage/새 Run/저장 복원 중 어떤 기능인지 별도 기획이 있어야 하며 이번 초기화 작업에서 새로 허용하지 않는다. 자동 수입·보상 정책도 이 요청에서 만들지 않는다.

초기 지급의 소유 단위(새 Stage·명시적 새 Run), 국면/화면 재진입 유지, 드로우·상점·회복의 결제 재화를 다시 미정으로 돌리지 않는다. Core는 실제 초기화 호출·실패/중복 처리·수명 종료와 위 규칙의 대응을 회신해 달라.

## 2. F2에서 유지·확인할 기존 Core 의미

다음은 현재 코드를 기준으로 View가 사용할 계약이다. 의미가 바뀌면 구현과 함께 알려 달라. 새 결과 큐·전역 순번을 요청하는 내용이 아니다.

- **포기:** 현재 Completion을 기록하며 일반 `CombatResult` 발행·순번 증가·`Phase = Finished`를 보장하지 않는다. View는 포기 뒤 일반 결과를 기다리거나 확인 번호를 만들어 올리지 않는다.
- **이미 완료된 전투:** 반환 `false`를 새 포기 성공으로 해석하지 않는다. 현재 Completion을 확인하고 정상 승패를 보존한다. `RunEnded`, 활성 전투 없음, DB/정산 실패의 반환/예외 의미도 명시해 달라.
- **EndBattle:** 활성 전투의 Completion 전 정리는 거부한다. 정리 뒤 `HasBattle=false`·활성 캐릭터 정리와 최신 결과/번호·Completion 보존, 다음 준비의 Completion 초기화 의미를 유지하는지 확인해 달라.
- **호출 순서:** Flow 새 등록·View 새 제출 중단 → Runner Idle → 남은 일반 결과 표시/확인 → 필요 시 포기 → Completion 표시 완료 → EndBattle → 최종 정리 확인 → 전환. Core에 연출 완료 판단·씬 전환을 요청하지 않는다.

## 3. 골격·View에서 할 일과 요청 범위 밖

- F2 전환 요청 소유자·공개 정지 경계, Completion의 최소 표시 UI·완료 통지, `EndBattle` 호출자는 **골격·View 담당**이 확정한다. 필요한 공용 계약만 Framework 담당과 맞추며 Core 요청에 씬·UI·Runner 책임을 섞지 않는다.
- 운영 DB 검증은 기본 source를 쓰는 정상 진입 호출자에서 로스터·챕터를 준비해 `EnterBattle` 경로로 실행한다. 현재 `GameBootstrap`의 Combat 단축 시작은 `useDevBattle=false`이면 직접 진입을 거절한다. **개발 토글만 끄는 것으로 운영 인수가 되는 것은 아니다.** 운영 호출자/검증 진입 설정은 골격·View 작업이다.
- F1은 기존 `GameBootstrap`·`RuntimeFaultBoundary` 하나로 진행한다. Core 오류를 정상 완료로 바꾸거나 새 오류 플래그/재시도 기능을 요구하지 않는다.
- 지뢰·Object·목적지 보정 저작 데이터는 해당 콘텐츠 인수 때 확인한다. Reason 코드별 HUD·시작 서브 수 변경은 후속 항목이며 위 세 구현의 범위를 확대하지 않는다.

## 4. 회신 항목과 인수 상태

요청별 **실제 파일·API 서명·호출/바인딩 시점·반환/실패/중복 호출 의미·검증 결과**를 알려 달라. 미정인 게임 규칙은 미정이라고 적고 임의 값으로 채우지 않는다.

| 항목 | 문서 준비 | 외부 전달 | 답변/API 인수 | 현재 상태 |
|---|---|---|---|---|
| 포기 입력을 Core가 조립하는 공개 경계 | 완료 | 미확인 | **2026-10-06 Core 구현(미커밋): `BattleAbandonResult AbandonBattle()`** (`plans/2026-10-06-new-run-start-contract-plan.md` §6) | F2 포기 연결(골격·View) 대기 |
| T1/G7 기본 source 조립·반환 | 완료, A2 데이터/API는 [DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달) | 미확인 | 미수신 | 운영 전투 준비 대기 |
| Stage 골드·Run 재화 분리·SO/DB 초기화·보존 | 보완 완료 | 보완본 전달 미확인 | **2026-10-06 Core 구현(미커밋): Stage 골드 분리·초기 지급 완료, Run 시작 재화는 DB 대기** (`plans/2026-10-06-stage-gold-run-currency-split-plan.md`) | Run 시작 재화 DB 대기 |

이 문서는 전달 가능한 요청 정리다. Core 구현·담당자 합의·외부 메시지 발송 완료를 뜻하지 않는다.


## 5. 골격 7 / View F에서 전달하는 반영 결과

일반 승리의 운영 호출자·F1·정상 이탈 구현과 기존 Core 답변 반영 범위는 [공동 인계](2026-10-05-combat-view-skeleton-session-handoff.md)에 정리했다. F3 최신 결과/번호·Completion 보존과 EndBattle 정리 의미를 기존 구현 그대로 사용한다. 입력 유지·한 처리 한 결과·예외 전파에 대한 기존 답변을 새 미수신 계약으로 취급하지 않는다.

§4의 '미수신/API 미인수'는 이번 요청 1~3을 해소할 **실제 공개 API/구현 인수**가 없다는 뜻이며, 전달받은 방향·규칙 답변이 없었다는 뜻이 아니다. 재화의 원본·지급/보존 정책은 확정됐고 공개 포기 경계·준비 source·분리된 잔액 구현이 남았다. 이번 세션에서 상대에게 발송하지 않았다.
