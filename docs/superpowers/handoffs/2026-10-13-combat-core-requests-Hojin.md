# 전투 Core 요청

- 작성자: 호진(이호진)

적 AI 보안관 배지 공격 문제 확인 요청과 가시심장 반사 연결 요청을 모았다. 원래 문서를 부(部)로 나눠 내용 그대로 담았고, 각 부의 결정 번호와 절 번호는 원래 문서와 같다.

| 부 | 원래 문서 |
|---|---|
| [1부](#1부-적-ai가-보안관-배지-보유-대상을-공격하지-않는-문제--core-확인-요청) | 적 AI가 보안관 배지 보유 대상을 공격하지 않는 문제 — Core 확인 요청 |
| [2부](#2부-가시심장-반사-처리-연결--core-요청-인계) | 가시심장 반사 처리 연결 — Core 요청 인계 |

---

## 1부. 적 AI가 보안관 배지 보유 대상을 공격하지 않는 문제 — Core 확인 요청

- 작성일: 2026-10-13
- 작성: View 작업(호진)
- 받는 사람: Core 작업자(천명님)
- 상태: **미전달·미합의·미수정.** Core·View·Framework·UI·Camera 실행 코드와 DB 에셋은 바꾸지 않았다. (→ 바뀜: Core가 AI를 고쳤다 — 2026-10-20 천명님 `16333690`(배지를 깨는 타격을 유효 타격으로 셈), 2026-10-24 `d4a6d6fd`(`EnemyAiPolicy.TrySelectAttackAim`이 피해 0인 합법 적대 대상도 공격). 이 요청에 대한 회신 문서는 찾지 못했다.)
- 우선순위: 미정(사용자가 정한다).
- 옮긴 곳: 이 내용은 처음에 [Core 경계 인계 §5.12](2026-09-24-view-core-data-boundary-handoff.md)에 적었다. §5는 View가 Core 영역에 손댄 기록을 모으는 절이라 2026-10-13에 이 요청 문서로 옮겼다. 경계 인계에는 §13.17에 연결만 둔다.

### 1. 증상

사용자가 Play에서 적이 메인 캐릭터에 접근하지만 메인 체력이 줄지 않는다고 보고했다.

- 운영 값: Demon 공격력 1·사거리 1·공격 비용 1, 메인 HP 50·물리방어 0.
- 적 자동 턴의 이동·공격은 연결돼 있다.
- 사용자가 실제로 어느 스테이지에서 배지를 얻었는지는 재생 스냅샷이 없어 확정하지 않았다.

### 2. 원인 추정 (코드 확인)

- `EnemyAiPolicy.TrySelectAttackAim`은 투영된 양수 HP 손실만 공격 가치로 집계하고, 합계가 0이면 그 후보를 건너뛴다.
- 보안관 배지는 얻으면 공격 한 번을 막는 상태가 된다. 배지를 가진 대상에 대한 유효한 공격은 투영 결과가 "배지 소모, HP 손실 0"이라 선택되지 않는다.
- `EnemyAiTurnService`도 공격을 실행하지 않으므로 배지가 그대로 남고, 같은 상황이 계속된다.
- 실제로 공격했다가 한 번 막히는 경우와는 다르다. 그 경우에는 배지가 소모되고 다음 공격부터 피해가 들어가야 한다.

### 3. 영향 범위

보안관 배지(`InGameGroup/Object/Item/Object_SheriffBadge.asset`)는 전투 스테이지 **11개 모두**(`Combat_Normal_A`~`Combat_Normal_J`, `Combat_Boss`)의 칸 (3,2)에 있다(2026-10-13 에셋 확인). 처음 기록한 "`Combat_Normal_A`에 있다"보다 넓다. 배지를 얻은 유닛은 사실상 모든 전투에서 적 AI의 공격 대상에서 빠질 수 있다.

### 4. 재현

#### 4.1 테스트 조건 대조 (실행함)

기존 `CombatCommonCommandTests.AutomaticInput`을 재사용한 임시 진단으로, 인접한 적·공격력 1·같은 AP에서 배지 유무만 바꿨다.

| 조건 | 결과 |
|---|---|
| 배지 없음, 자동 턴 3회 | HP `100→99→98→97` |
| 배지 있음, 자동 턴 | 공격 없음, HP 100, 배지 미소모 |
| 배지 있음, 공격 의도를 명시해 직접 계산 | 공격 수락, HP 손실 0과 배지 소모가 함께 반환 |

- HP 100은 검사 fixture 값이며 운영 메인 HP 50은 바꾸지 않았다.
- 기존 AI·배지 테스트 25건과 이 진단 1건은 **26/26 통과**, Unity 종료 코드 0이다. 이 통과는 증상 재현을 뜻하며 수정 완료를 뜻하지 않는다.
- 이 결과는 Core 조건 재현이며 사용자의 실제 씬 상태를 읽은 결과는 아니다.
- 임시 진단 스크립트는 Assets에서 지웠다. 진단 코드·XML·로그는 `.superpowers/sdd/2026-10-13-enemy-attack-audit/`에 있으나 git에서 제외되는 폴더라 작성한 PC에만 있다. 다시 실행하려면 위 표의 조건으로 같은 진단을 만들면 된다.

#### 4.2 Play 재현 순서 (예상, Play로 다시 확인하지 않음)

1. 아무 전투 노드에 들어간다(모든 전투 스테이지의 (3,2)에 배지가 있다).
2. 아군 유닛으로 (3,2)의 보안관 배지를 얻는다.
3. Demon이 그 유닛에 인접하도록 턴을 넘긴다.

- 기대: Demon이 공격해 배지가 한 번 막고 소모된다. 다음 적 턴부터 HP가 준다.
- 실제(사용자 보고·진단 기준): Demon이 접근만 하고 공격하지 않는다. HP와 배지가 그대로다.

### 5. 요청

배지 소모처럼 다음 공격을 가능하게 만드는 보유 방어 상태 변경을 AI의 공격 가치에 포함할지 정책을 확인해 주세요. 변경한다면 다음을 함께 검증해 주세요.

- 배지 보유 대상에 첫 공격 → 보호 소모 → 다음 적 턴의 실제 피해
- 다른 피해 가능 대상과의 우선순위
- 실제 공격력 0·AP 부족일 때 기존 건너뛰기 유지

View에서 피해·공격 선택을 대신 계산하거나 배지 상태를 지우는 우회는 추가하지 않는다.

### 6. 회신 양식

| 항목 | 답변 | 담당 | 날짜 |
|---|---|---|---|
| 정책: 보호 소모를 공격 가치에 포함하는가 | | | |
| 수정 범위와 검증 테스트 | | | |
| 우선순위·반영 시점 | | | |

---

## 2부. 가시심장 반사 처리 연결 — Core 요청 인계

- 작성: 2026-10-15, 호진 View → Core 작업자.
- 상태: **요청 문서 작성 완료·Core 미구현·외부 미전달.** (→ 바뀜: Core 연결은 천명님 `ebf26246`(2026-10-16)으로 완료 — [Core 전달 문서 §5.31~§5.33](2026-09-24-view-core-data-boundary-handoff.md#531-thorn-heart-contracts-and-isolated-reflection-2026-10-16-phase-th1). 아래 §3의 미지원 예외와 §6의 "현재 미지원 예외를 기대하는 회귀"는 연결 전 상태) 사용자는 이번 세션에서 인계서 작성과 기존 Core 전달 문서 연결만 요청했다. Core 코드·Buffer·DB·테스트·View·씬·프리팹은 수정하지 않는다. Core 담당자는 아래 미확정 정책을 확인해 설계 → Task 계획 → Phase 단위로 진행한다. 이 문서가 구현 실행 승인은 아니다.
- 조사 기준: HEAD `95dbf3f587547a45d075c963417aea2b26ea9cf7`와 현재 미커밋 작업본. ER6·SD1·SDV0 및 다른 작업자의 변경을 보존한다.
- 요청 원본은 이 문서다. [Core 경계 전달 §18](2026-09-24-view-core-data-boundary-handoff.md#18-thorn-heart-reflection-connection-request-2026-10-15)에서 이 문서로 연결한다. [소울 드랍 Core 확인 메모](2026-10-15-soul-drop-handoff-Hojin.md#2부-소울-드랍-core-계약-확인-메모--2026-10-15)의 기존 계약 인수와 구분한다.

### 1. 요청 목적과 완료 범위

보드에 배치한 **가시심장**의 반사 효과를 실제 Core 공격 정산에 연결해 달라는 요청이다. 현재는 설정을 컴파일하고 조건에 맞는 고정량을 반환하는 규칙까지만 있으며, 가시심장이 포함된 로드아웃의 공격 투영 생성은 미지원 예외로 중단한다. Core에서 반사 피해·사망·후속 효과를 정산하고 기존 원자 게시 경계로 결과를 공개하면, 전투 View 작업자가 게시된 사실을 표시한다.

가시심장 반사는 **소울 드랍 필드 구슬 표시의 선행 조건이 아니다.** 소울 드랍은 기존 공개 사실로 별도 진행한다. 가시밭/ThornField, 시각적인 가시 모양, 반응 공격/포효를 가시심장 자체와 같은 기능으로 취급하지 않는다. 이번 요청에 접촉 피해·새 퍼센트 반사·새 UI/카메라·새 공용 서비스는 포함하지 않는다.

### 2. 현재 기획·데이터·구현 근거

Core 소스 접두사 `CORE`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`다. 아래 줄번호는 조사 시점 기준이며 파일의 실제 선언을 다시 확인한다.

| 근거 | 확인한 현재 상태 |
|---|---|
| Project L `기획 브레인스토밍/안재령의고뇌/아티팩트 효과 (임시).txt` 11~19행 | 옛 이름 **가갑**, UnitDamaged·적군 source·밤·마지막 공격 적군 대상·피해량3·즉시0턴. 임시 기획 값이며 최종 밸런스 승인으로 확대하지 않는다 |
| 같은 폴더 `아티팩트 기획서.pptx` 5~12슬라이드 | 공통 Trigger/Source/Target/Effect/Value/지속 문법. 중첩 Effect의 처리를 정하라는 메모는 있으나 가시심장의 발동 횟수·정산 순서는 확정하지 않음 |
| `기획 문서/아트 관련 문서/아티팩트 아트 기획서 초안.pptx` 8슬라이드 | **가시심장**, 1×2, 피격 시 반사 피해. 접촉 피해를 연상시키는 외형 지시를 실제 접촉 피해 규칙으로 확대하지 않음 |
| [실제 InGame SO](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/Item/Artifact_ThornArmor.asset) 15~48행, [View SO](../../../Assets/ProjectL/DataBaseSO/ViewGroup/Artifact/Item/ArtifactView_ThornArmor.asset) | ID1002, `night_reflect_damage`, 현재 운영 저작은 Flat/None/3·duration0. View 이름은 가시심장. 기존 파일명·ID·2칸·GUID·등록은 보존 |
| [ArtifactEffectCompiler](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Artifact/ArtifactEffectCompiler.cs) 215~242·311~312행 | Enemy/IsNight/이번 공격의 LastAttacker Enemy/Damage/Flat/None/즉시, 음이 아닌 정수 설정량을 검증해 ThornHeartRule 생성. 수치3을 코드 상수로 고정하지 않음 |
| [ThornHeartRule](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Artifact/Rules/ThornHeartRule.cs) 10~22행 | 밤이고 `hasActualEnemyAttackDamageToAlly`일 때 설정량 반환. 실제 HP 적용·대상 결정·사망·사건 생산은 하지 않음. 제품 코드의 EvaluateDamage 호출은 현재 확인되지 않음 |

원문은 저장소 밖 `C:/Users/Hyeon2-400/Dropbox/Project L/`에서 텍스트/발표자 노트를 읽기 전용으로 확인했다. 일반 기획서는 발표자 노트가 없고 아트 기획서 노트는 숫자1뿐이었다. 삽입 그림·동영상 전체 시각 검수는 하지 않았다. 원문·운영 SO가 모두3이라는 사실과 최종 수치/정산 정책의 승인은 구분한다. 이번 요청에서 SO 값은 바꾸지 않는다.

### 3. 현재 미연결 지점과 영향 경로

1. [SoulEffectCalculator.RequireSupportedAttackSettlement](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Artifact/SoulEffectCalculator.cs) 24~28행은 컴파일된 로드아웃에 ThornHeartRule이 있으면 `NotSupportedException`으로 중단한다. 메시지는 “반사 Artifact의 공격 정산 순서는 기획 확정 후 연결해야 합니다.”다.
2. [CommandAttackProjection](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CommandAttackProjection.cs) 51~52행이 이 검사를 실제 호출한다. 발동 조건을 만족했을 때만 중단하는 것이 아니라 **해당 로드아웃의 공격 투영 생성에서** 중단한다.
3. [CombatAttackCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackCalculator.cs)의 일반 공격·Reaction이 공통 Execute를 사용한다. [CombatCommandCalculator.Preview](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandCalculator.cs)도 같은 공격 계산에 들어간다. [EnemyAiPolicy](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyAiPolicy.cs)는 그 Preview 투영을 평가하고 실제 AI 행동은 같은 Calculate를 사용한다. 따라서 일반 공격만 지원하는 부분 연결로 완료하지 않는다.
4. 현재 공격의 직접 피해·넉백·가시밭·비용·강제 도착·처치 연쇄와 최종 사망/영혼/승패 정산 사이에 반사 단계를 연결해야 한다. 삽입 순서는 아래 정책 확인 항목이며 이 요청서에서 정하지 않는다. 실제 피해·사망·후속 게시가 연결된 지원 경로를 갖춘 뒤 해당 미지원 판정을 정상 지원으로 대체한다. 그 전에는 View에서 예외를 우회하거나 guard를 제거하지 않는다.

### 4. Core 담당자에게 요청할 정산과 정책 확인

현재 컴파일러의 이번 공격 LastAttacker 범위와 설정 고정량을 기준으로 삼는다. [ArtifactEffectKey](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/ArtifactEffectKey.cs)의 옛 “최근 공격 기록 범위 별도 확정” 주석 때문에 새 전역 LastAttacker 저장소를 만들지 않는다. 기존 계약으로 결정되지 않은 항목은 기획/사용자 확인 후 설계에 명시한다.

| 항목 | Core가 확정·연결할 내용 |
|---|---|
| 실제 피해와 대상 | 원 공격의 방어/보유 방어·감소 뒤 어떤 사실을 실제 피해로 볼지, 0피해·완전 방어·실패/미명중·과잉 피해·피격자 사망 시 발동 여부. 원 공격자와 피격 아군을 명확히 연결 |
| 반사 피해 적용 | 설정 고정량에 반사 대상의 방어·감소·면역·위치 보정을 적용하는 방식, Requested/Applied의 의미. 받은 피해 비율을 새로 계산하지 않음 |
| 발동 단위·중첩 | 공격 한 번/피해 대상/다단 타격별 횟수, 같은/다른 가시심장 여러 배치의 합산·적용 순서와 출처. 중복 사망/중복 발동 방지 |
| 피해 원인 범위 | 일반 직접 공격, 포효 Reaction, 처치 연쇄·넉백 충돌·도착 환경·폭발·가시밭 중 어디까지 해당하는지. Source Enemy라는 사실만으로 모든 파생 피해를 공격 피격으로 처리하지 않음 |
| 재귀·이미 죽은 대상 | 반사 피해가 추가 반사를 발생시키는지와 종료 보장, 반사 적용 전에 공격자가 죽었거나 사라진 경우, 한 명령에서 같은 대상을 여러 번 참조하는 경우 |
| 정산 순서 | 원 공격·반사·피격자/공격자 사망·넉백/도착·처치 연쇄·포효·영혼 생성·승패/완료의 순서. 반사로 공격자가 죽었을 때 남은 공격/이동/후속 처리를 어떻게 끝낼지 |
| Preview·AI | 실제 반사와 공유할 계산 경계, 공개 Preview에 포함할 범위, AI의 자기 피해/자기 사망 평가. 기존 공개 Preview의 처치 연쇄/대기 포효 제한은 계약 검토 없이 넓히지 않음 |
| 0·상한 | 규칙은 설정량0을 허용하지만 발동 Snapshot은 Requested>0만 허용한다. 0설정/실제0적용의 사건 유무, 여러 발동 합산의 상한/overflow 처리 |

Core는 규칙과 계산을 소유하고 View는 결과만 읽는다. [SSOT·SOLID 기준 §4](2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙)과 [Framework 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md#6-framework-contract)을 유지한다. 기존 공격 투영·완료/게시 수명에 연결하고 반사 전용 지속 상태 서비스·중복 계산/완료 번호를 추가하지 않는다. Reaction의 기존 AP 검사/지불/환급 차이도 유지한다.

### 5. 필요한 공개 결과와 원자 게시

View가 반사량·대상·순서를 재계산하거나 최신 Buffer로 사건 당시의 사실을 추측하지 않도록 다음 정보를 공개해 달라는 요청이다. **기존 공개 계약으로 충분하면 재사용한다.** 부족한 정보가 있으면 Core가 필요한 최소 순수 C# 계약과 생산 시점·사용 예를 인계한다. 이 문서에서 새 API 이름/서명을 확정하지 않는다.

| 공개할 사실 | 현재 재사용 가능한 계약·확인 사항 |
|---|---|
| 발동 출처·반사 대상·요청량/실제 적용량 | [CombatArtifactEvent](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs)의 Activations와 [ArtifactActivationSnapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/ArtifactActivationSnapshot.cs)의 Origin/TargetId/RequestedAmount/AppliedAmount. 설정0은 Requested>0 생성자 계약과 정합하게 결정 |
| 반사를 일으킨 피격 아군·원래 공격자·피해 귀속 | 발동 사실과 해당 체력/사망 사건의 연관이 모호하지 않아야 함. [CombatDamageOrigin](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatDamageOrigin.cs)의 PhysicalCell/CreditedCharacterId/AttackGroup을 검토. 이것만으로 Artifact 출처/반사 종류까지 모두 표현된다고 가정하지 않음 |
| 실제 체력·사망의 전후 사실 | CombatCharacterEvent의 Expected/Final·DamageOrigins, 유효한 EntityId와 실제 사망 위치. 원 공격자와 명령 ActorId가 다른 Reaction에서도 결과만으로 식별 가능해야 함 |
| 사망 영혼·승패·완료 | 기존 CombatSoulEvent/SoulField, FinalState/Outcome/Completion과 일치. 반사로 인한 적 사망도 확정한 순서에 따라 한 번 정산. View는 별도로 영혼/사망/승패를 만들지 않음 |
| 재생 순서와 최종 상태 | [CombatCommandTransition](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatCommandTransition.cs)의 Events/Characters/Field/FinalState/Outcome/Attack/Completion을 통해 같은 명령의 순서 있는 사건과 최종 Buffer가 일치 |

[BattleTransactions.Apply](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BattleTransactions.cs) 147~178행의 **모두 Prepare한 뒤 Install**하는 원자 게시 경계를 유지한다. 반사 HP·사망·영혼·명령 결과·완료를 기존 transition에 포함하고, 계산/검증 실패 또는 stale 원본에서는 부분 게시하지 않는다. 메인 캐릭터의 지속 HP와 완료 정산도 기존 경계를 따른다. View의 결과 순번·ack·마지막 결과 소비·F1·취소/Detach 수명을 변경하는 별도 경로를 만들지 않는다.

### 6. Core에 요청하는 검증 항목

아래는 **후속 구현의 인수 기준**이며 이번 문서 작업에서 실행한 테스트가 아니다. 정책 미확정 항목의 기대값은 §4를 먼저 확정한 뒤 테스트로 고정한다. 기존 EditMode 어셈블리를 사용하고 새 테스트 asmdef를 추가하지 않는다.

| 검증 묶음 | 필요한 assertion |
|---|---|
| 기존 스키마·실제 저작 | 현재 ID1002 SO→Table→컴파일 경로와 Flat 설정값 보존. 설정0/다른 양수도 동작하며3을 하드코딩하지 않음. 잘못된 값·오염 스키마는 계속 거절 |
| 발동/미발동 | 밤·적의 유효한 아군 피격과 낮·피해0·거절/미명중·다른 진영/원인의 경계. 확정한 실제 피해 정의에 따라 정확한 횟수·금액 |
| 대상·다단·중첩 | LastAttacker가 이번 공격자이며 이전 공격자/명령 ActorId로 잘못 대체되지 않음. 범위 피해·다단·여러 배치/출처의 발동 순서·합산·중복 처리 검증 |
| 반사·사망·후속 순서 | 반사 치사·피격자 치사·동시사망·이미 죽은 공격자, 이후 넉백/강제 도착/연쇄/포효의 확정 동작. Died·영혼 생성·승패/Completion이 정확히 한 번이고 최종 상태와 일치 |
| Main·Reaction·Preview·AI | 같은 입력에 같은 반사 규칙과 인과관계. 실제 결과와 Preview의 계약상 공개 부분 일치. Preview는 원본/Buffer 변경0. Reaction AP 추가 지불0, AI는 별도 반사 산식 없이 자기 피해/사망을 정책대로 평가 |
| 금액·귀속·0·상한 | Requested/Applied·최종 HP 차이·DamageOrigins/출처 일치. 0설정/0실적용 Snapshot 정책, 과잉 피해·중첩/overflow·반사 재귀의 종료 보장 |
| 원자성·수명 | stale Character/SoulField, 잘못된 최종 사실·완료·중간 계산 예외에서 모든 Buffer/명령/결과 불변. 거절/재처리로 중복 반사·중복 사건/완료가 생기지 않음. 정상 마지막 결과/ack 경계 유지 |
| 회귀·실제 경로 | 가시심장 없는 공격/이동/비용·소울 드랍·가시밭·장애물/포효·일반 종료 유지. 실제 SO를 배치한 로드아웃에서 반사 정산이 실행되고 최종 공개 결과로 확인됨. View 자동 표시/사람 조작 검증은 별도 인수로 기록 |

현재 테스트 접점은 [ArtifactThornHeartRuleTests](../../../Assets/Tests/EditMode/ArtifactThornHeartRuleTests.cs), [CombatCommandEffectTests](../../../Assets/Tests/EditMode/CombatCommandEffectTests.cs), [CombatCommandPublicationTests](../../../Assets/Tests/EditMode/Framework/CombatCommandPublicationTests.cs), [BossReactionPublicationTests](../../../Assets/Tests/EditMode/Framework/BossReactionPublicationTests.cs)다. `UnresolvedReflectionLoadout_DoesNotActivateOnNewAttackRoute`는 현재 미지원 예외를 기대하는 회귀다. 통합 구현 시 실제 반사 결과·원본 불변을 검증하는 인수 기준으로 갱신하고, 연결 전부터 예외 기대만 지워 통과시킨 것으로 보지 않는다.

직전 [SDV0 기준선](../plans/2026-10-15-soul-drop-plan-Hojin.md#32-sdv0-인수-2026-10-15)(소울 드랍 계획 §3.2)은 참고 이력이다. 후속 구현 시작 시 현재 작업본의 관련/전체 기준선을 새로 실행하고 실패의 이름·label·메시지를 비교한다. 독립 규칙/guard 테스트 통과와 실제 반사 정산·원자 게시·View 표시 완료를 구분한다.

### 7. Core 구현 후 돌려받을 인계와 이번 종료

Core 담당자에게 요청하는 반환 자료는 확정한 §4 정책, 실제 연결 파일/공개 결과와 예제, 계산·원자 게시·회귀의 XML/실패 비교, 운영 SO/허용 로드아웃, 미지원 조합·사람 미확인 목록이다. 공개 계약이 바뀌면 기존 소비자 호환/추가 View 작업을 명시한다. 이후 전투 View 작업자는 [hojinView 가이드](../viewDocs/view-development-guide-Hojin.md)에 따라 기존 표시/결과 수명 안에서 연결한다.

이번 세션의 완료 범위는 이 요청서와 기존 Core 전달 문서의 연결·문서 검증까지다. Core 구현·가시심장 동작 검증·전투 View 연출·새 테스트 실행은 하지 않았다. 새 정책/수치/순서의 확정이 필요하면 먼저 확인하며 이 문서 작성만으로 승인받은 것으로 처리하지 않는다. 문서 갱신 뒤 멈추고 커밋·push·스테이징·외부 발송은 하지 않는다.

작성 검증: 새 요청서1개와 기존 인계2개만 변경했다. 시작 작업본8,045개 파일과 비교해 범위 밖 변경0·index 보존, 문서3개의 로컬 링크164건/anchor·UTF-8/LF/BOM·공백/diff 검사 통과, 기획 원본3개 hash 보존을 확인했다. 실제 소스 대조와 독립 문서 리뷰에서 필수 수정 지적은 없었다. 이 결과는 문서 검증이며 반사 처리 구현/테스트 완료를 뜻하지 않는다.
