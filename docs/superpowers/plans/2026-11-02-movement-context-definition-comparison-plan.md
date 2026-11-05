# 이동 StaleContext — 특성별 비교 계약·프로필 선택 수정 Task 계획 (2026-11-02)

- 사용자 결정: 이번 Core 수정은 우리 측이 맡는다. "보완점 적용해서 계획 수정해"에 따라 기존 두 파일 초안을 **특성별 SameValues 계약 방식**으로 개정했다.
- 설계: [개정 설계](../specs/2026-11-02-movement-context-definition-comparison-design.md). 구현 계약·필드/선택 정책의 원본은 설계다. 이 문서는 Task·순서·완료 상태를 관리한다.
- 검토 근거: [Core 구조 우선 리뷰](../specs/2026-11-02-movement-context-definition-comparison-review.md). 원인·공유 기록: [샷건 이동 조사·구현 인계](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md).
- 검증 기본 제한: 새 시험 작성·MSBuild·임의 시험 실행 없음. 기존 시험은 추상 계약 변경으로 필요한 가짜 타입 override만 최소 대응한다. assertion/입력/실행 내용은 변경하지 않는다. **후속 사용자 지시 "테스트 돌려"로 이번 MC1 관련 기존 Core EditMode 테스트는 Unity Test Runner에서 실행했다.** 새 시험 작성이나 MSBuild 허용으로 넓히지 않는다.
- 후속 사용자 요청으로 이번 작업의 코드·meta·관련 문서를 Git 스테이징 대상으로 준비한다. 커밋·push는 하지 않으며 기존 사용자 변경을 유지한다. 이전 통합 인계 파일은 사용하지 않는다.

## Phase MC0 — 리뷰 보완·계획 개정 (완료)

- [x] 사용자 지시로 우리 측 Core 직접 구현 범위를 확정한다.
- [x] 초기 17형식 이동 분기 초안의 R1·R2·R3를 검토한다.
- [x] 대안 A 채택: TraitSpec의 공통 진입점/추상 비교와 구체 타입별 구현, 이동의 Unit/Profile 조합 비교로 소유를 나눈다.
- [x] 새 타입 비교 누락의 컴파일 검출, 특성 키 집합 동등성, 원본 객체 참조 유지 경계를 설계에 명시한다.
- [x] RequiredProfile 선택 재사용과 위치 교환/강제 이동 의존 감사의 선행 조건을 정한다.
- [x] 변경 파일·공개 소스 계약 영향·기존 시험의 컴파일용 최소 대응·Phase 리뷰 지점·Unity 인수를 정리한다.

**현재 상태: MC1·MC2A·MC2B 구현과 Unity 컴파일 확인 완료. 사용자 "남은 Phase 끝까지 진행하고 마지막에 검증 및 테스트 작업 진행해" 지시로 추가 전달 파일을 포함한 MB1·MB2와 최종 검증을 마쳤다. 기존 EditMode 5,619 통과/8 실패이며 후속 사용자 진지 구축 정상 확인을 반영했다. rank별 전수와 세부 경계 인수는 남아 있다. 실행하지 않은 세부 인수와 최종 시험 결과는 인계 §14/추적 계획에 구분한다.**

## Phase MC1 — 특성 설정 비교 계약 (코드·Unity 컴파일 확인, 기존 시험 1건 실패)

- [x] MC1.1 실제 TraitSpec 파생 선언과 설정 필드 목록을 다시 조사한다. 현재 구체 Spec 37개 및 시험 가짜 타입 3개와 대조하며, 다른 파생자 발견 시 범위 영향을 먼저 확인한다.
- [x] MC1.2 TraitSpec.cs에 설계 D1의 공통 SameValues와 보호 추상 SameValuesCore를 추가한다. 형식/키/null의 공통 계약과 파생 구현 전제를 한국어 XML로 적는다.
- [x] MC1.3 구체 Spec 37개가 자기 설정을 비교하게 한다. 무설정 타입·거리별 목록·반경/주기/기간을 포함한다. 폐기 특성의 실행/등록은 변경하지 않는다.
- [x] MC1.4 기존 시험 3파일의 InvalidFocus/ReservedTraitSpec에 필수 override만 추가한다. 새 시험·assertion·입력·기능 변경은 하지 않는다.
- [x] MC1.5 각 타입의 전체 설정 속성과 비교를 정적 대조한다. 전역 Equals/HashCode·설정 대신 상태 비교·참조 fallback·특성 switch·새 어셈블리 참조가 없는지 확인한다.
- [x] MC1.6 diff·코딩 규칙·Core Unity 참조 부재를 확인하고 인계 기록을 갱신한다. **멈추고 Phase 결과를 보고한다.**

대상: TraitSpec 1개 + 구체 Spec 37개 + 기존 시험 3개. 이 Phase는 파생 타입의 컴파일 계약까지 함께 맞추는 단위다. 이동 문맥은 아직 연결하지 않으므로 **버그 해결 완료로 보고하지 않는다.**

## Phase MC2 — 프로필 의존 감사 (완료·MC2A/MC2B 분리)

- [x] MC2.1 편집 전 의존 감사를 수행했다. 환경 피해의 진지 구축 오라에서 행위자·수집자 밖의 프로필을 읽는 추가 의존을 확인하고 [인계 §10](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#10-mc21-프로필-의존-감사와-중단-2026-11-02)에 대조표를 기록했다.
- [x] MC2.2 추가 의존 추적/참여 집합 변경 필요를 확인해 구현 전 중단했다. 이후 사용자 승인으로 기존 MC2.3~7을 아래 MC2A/MC2B로 대체했다. 기본형 제거/선택 통일은 MC2A에서 구현하지 않는다.

### 감사 후 범위 분리 — 사용자 승인 (2026-11-02)

사용자 "진행해" 지시로 값 비교 연결과 추가 의존 수집 보강을 분리했다. MC2A는 신규 비교 부품/meta와 기존 MovementContextCalculator의 비교 부분만 수정하고 Capture를 유지한다. MC2B는 실제 의존 추적·RequiredProfile 선택 정합을 별도 설계한 뒤 진행한다. 이미 존재하는 누락 의존까지 MC2A에서 해결했다고 보고하지 않는다.

OwnedUnitSpecResolver·TraitRankValues·CombatCommandCalculator·Domain·DTO/Buffer·View·SO·씬·Factory 변경까지 이번 구현 권한을 넓히지 않는다.

## Phase MC2A — 현재 수집 범위를 유지한 값 비교 연결 (코드·Unity 컴파일 확인)

- [x] MC2A.1 MovementDefinitionComparison.cs/meta 추가. UnitSpec의 독립 설정 속성 18개를 대조하고 특성은 TraitKey와 TraitSpec.SameValues로 비교한다. Method·Behavior·Occlusion·KillReward의 기존 참조 동일성 정책을 유지한다.
- [x] MC2A.2 MovementContextCalculator의 Definitions/OwnedProfiles 비교를 공통 부품에 연결하고 기존 SameProfile의 필드 중복을 제거한다.
- [x] MC2A.3 편집 전/후 소스를 정적 대조해 Capture 전체·Matches·다른 SameValues 조건·SameMover/Target 등 나머지 비교가 동일함을 확인한다. 기본/보유 동시 수집·정렬/참여 집합을 유지한다.
- [x] MC2A.4 TraitKey 중복 금지, Domain의 TraitKey 정렬, 효과 상태의 키 조회와 엔진의 Origin 정렬을 확인한다. 새 비교는 정렬/배열 사본/캐시 없이 키를 찾아 자기 설정 비교에 위임한다.
- [x] MC2A.5 diff 공백·LF/UTF-8·새 GUID 중복/UnityEngine 참조 부재를 확인하고 인계 §11을 기록한다. 신규 시험·MSBuild·실행 검증은 하지 않는다. **리뷰 지점에서 멈춰 보고한다.**

## Phase MC2B — 실제 프로필 의존 수집 (코드·Unity 컴파일 확인)

- [x] MC2B.1 실제 프로필 읽기/공개 경계를 조사해 Application 내부 수집기·선택 결과 기록·파일 범위를 [MC2B 설계](../specs/2026-11-02-movement-profile-read-tracking-design.md)로 정리했다.
- [x] MC2B.2 부정 후보/면역·도착 전 종료·환경 피해/오라·특성 존재/도발/처치 경로와 기존 DTO 동결을 구체화하고 8기준으로 정적 리뷰했다. 사용자 확인 뒤 진행할 Task는 [MC2B 구현 계획](2026-11-02-movement-profile-read-tracking-plan.md)이 원본이다.
- [x] MC2B.3 후속 사용자 지시로 MB1 수집기·전달 경로, MB2 Capture 활성화를 구현했다. 기존 2인자 생성자 호환을 복구하고 Unity 컴파일을 확인했다.

MB1 사전 대조에서 CombatEffectiveStatsCalculator의 간접 읽기를 확인했고, 후속 사용자 지시로 이를 포함해 구현했다. MB1은 18 C#(신규 1·기존 17)+meta, MB2는 MovementContextCalculator 1개다. MC2B에서 Contracts/Domain/공개 DTO·시험·서비스/Factory·View/에셋 변경은 없다.

## Phase MC3 — 사용자 Unity 인수

후속 최종 사용자 지시로 Unity 컴파일과 기존 시험을 실행했다(인계 §14). 아래의 세부 인수는 직접 실행한 증거가 있을 때만 완료로 표시한다. 새 시험 작성·기존 시험 판정 수정·MSBuild는 하지 않는다.

- [x] MC3.0 최종 Unity 컴파일·기존 전체 EditMode 실행/실패 대조·구조/diff 확인을 마치고 인계 §14에 실제 증거/한계를 기록했다. Combat 진입·샷건/메인 배치까지 확인했으며 아래 세부 재현은 미확인이다.

- [x] MC3.U1 후속 사용자 진지 구축 정상 확인을 인수했다. 확인한 rank 등 세부 조건은 미보고이며 아래 전수 인수와 구분한다.

- [ ] MC3.1 샷건·진지 구축 rank 1·추가 구매 없음에서 미리보기 → 경로 확정 수락.
- [ ] MC3.2 rank 2·3, 다른 훈련 특성·병과 기본 특성·무설정 특성에서 동일 설정 반복 생성 시 수락.
- [ ] MC3.3 실제 특성 수치·반경/주기·목록 원소·TraitKey/형식·보유 선택 변경 시 거절. 특성 목록 배치 순서만 바뀐 경우는 같은 것으로 처리.
- [ ] MC3.4 기본형 대체·보유형 선택·관련 수집자·위치 교환·환경 강제 이동/면역 등 감사한 경계에서 정상 수락/실제 의존 변경 거절.
- [ ] MC3.5 전투/행위자/초안 경로 불일치의 거절과 거절 시 무변경 유지.
- [ ] MC3.6 결과 8과 같은 재현이 계속되면 실제 비교 실패 항목 관측을 별도 설계한다. 정적 결함 수정만으로 그 로그의 실행 원인까지 확정하지 않는다.

## 실행 기록

- 2026-11-02: 사용자 지시로 Core 직접 구현 방향의 초기 설계·Task를 작성했다. 후속 리뷰에서 특성 설정 소유(R1), 누락 지원 검출(R2), 프로필 선택/의존 범위(R3)를 보완 대상으로 기록했다.
- 2026-11-02 개정: 사용자 지시로 대안 A를 채택하고 초기 두 파일·17형식 분기 Task를 대체했다. MC1(타입별 계약) → MC2(의존 감사·문맥 연결/선택) → MC3(사용자 인수)로 나눴다. R3의 의존 포함 증명은 MC2.1 완료 조건이며 미확인 상태에서 Capture를 축소하지 않는다.
- 계획 개정 시에는 문서만 수정했다. 이후 아래 MC1 구현으로 상태가 진행됐다.
- 2026-11-02 MC1: 사용자 "오케이 작업 진행해" 지시로 TraitSpec 1개·구체 Spec 37개·기존 시험 가짜 타입 3개를 수정했다. 33개 스칼라 설정 타입·거리 목록 타입 1개·무설정 타입 3개, 총 설정 속성 62개를 정적 대조했다. 모든 직접 파생 타입 40개에 필수 override가 있다.
- MC1 정적 확인: 기존 생성자·설정·시험 검증 본문은 그대로이며 비교 메서드만 추가됐다. diff 공백 검사·LF/UTF-8·UnityEngine 참조 부재 확인. 신규 파일/meta·Equals/GetHashCode·특성 등록/실행·조립 변경 없음. MSBuild·시험·Unity 실행은 하지 않았다.
- MC1 리뷰 지점에서 중단했다. 다음은 MC2.1 프로필 의존 감사이며 MovementContextCalculator/이동 연결은 아직 변경하지 않았다. 스테이징·커밋·push 없음. **컴파일 미확인.**
- 2026-11-02 후속 검증: 사용자 "테스트 돌려" 지시로 Unity 6000.0.73f1 Test Runner의 ProjectL.Tests.Core.dll EditMode 4,637건을 실행했다. **4,636 통과·1 실패·건너뜀 0**, 실행 시각 05:14:32~05:14:57(KST). 수정된 Contracts/시험 어셈블리로 실행돼 Unity 컴파일도 확인했다. 위 컴파일 미확인은 MC1 코드 완료 보고 당시의 상태다.
- 실패: TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True), 618행, 기대 활성화 1회/실제 2회. 11월 1일 저장 결과에도 같은 시험·메시지로 실패한 기록이 있다. 이 결과를 이번 변경의 새 회귀로 단정하지 않으며 시험이나 도발 실행 규칙은 수정하지 않았다.
- 가짜 타입을 맞춘 기존 시험군은 BattlePreparationRuleTests 33/33, LegacyEnhancementRetirementTests 17/17, StatusEffectRetirementTests 4/4 통과했다. 원본 [결과 XML](../../../Logs/CodexVerification/2026-11-02-mc1-trait-value-comparison/editmode-results.xml)·이전 결과 대조는 [구현 인계 §9](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#9-mc1-사용자-지시-테스트-실행-2026-11-02)에 기록했다. MC2 구현이나 게임 재생 재현은 진행하지 않았다.
- 2026-11-02 MC2.1: 사용자 "다음 페이즈 진행해" 지시로 의존 감사를 수행했다. 환경 피해는 CombatDamageApplication → CaptureIncoming을 거쳐 움직이지 않은 진지 구축 보유자의 설정까지 읽지만 현재 Capture는 행위자/Owned 도착 수집자만 프로필에 담는다. 상태 스냅샷/EffectTargets 비교가 해당 설정을 대신하지 못함을 확인해 MC2.2에서 중단했다.
- MC2 실행 코드·meta·시험 변경 없음. MC1의 컴파일/기존 시험 결과는 유지하며 MC2 새 코드는 없고 컴파일·재생 미확인이다. 값 비교 연결(MC2A)과 의존 추적 설계(MC2B)를 나누는 제안을 기록했으며 사용자 확인 전 구현하지 않는다.
- 2026-11-02 MC2A: 사용자 "진행해"로 분리안을 승인받았다. 신규 MovementDefinitionComparison.cs/meta와 기존 MovementContextCalculator의 비교 부분을 수정했다. Capture/Matches/다른 사실 비교는 편집 전과 동일하며 기본·보유 정의 모두 타입별 값 비교로 연결됐다.
- MC2A 정적 확인: UnitSpec의 독립 설정 18개, 프로필 OwnedId/Method, 특성 키 집합/설정, 기존 원본 참조 계약을 대조했다. 새 GUID 중복 없음·LF/UTF-8·diff 공백 검사 확인. 시험/Unity/MSBuild 실행 없음. MC2A 리뷰 지점에서 중단하며 **컴파일 미확인**이다. MC2B의 추가 의존 누락과 실제 샷건 이동 재현은 남아 있다.
- 2026-11-02 MC2B: 사용자 "다음 진행해"로 설계/Task를 구체화했다. 환경 피해의 존재 재계산·도발·처치/사냥꾼까지 수집기를 전달하도록 읽기 경로를 조사했다. 별도 추적 설계/계획을 만들고 불변 입력/Contracts/Domain을 유지하는 Application 내부 인자 전달 방식과 기존 문맥 목록 동결을 선택했다.
- MC2B 설계 단계에서는 문서만 변경했다. MB1/MB2 코드·meta·시험·에셋·스테이징은 수정하지 않았고 구현 전 리뷰 지점에서 멈춘다. **컴파일 미확인.**
- 2026-11-02 MB1 사전 대조에서 기존 설계의 결속력 방어 간접 읽기 누락을 발견했다. CombatEffectiveStatsCalculator.cs 1개를 추가하는 범위 개정안을 작성했고 코드 편집 전에 멈췄다. 별도 설계 §9/추적 계획의 확인 대기 상태를 따른다.

- 2026-11-02 최종 진행: 사용자 지시로 추가 전달 파일을 포함한 MB1·MB2 구현 완료. Unity 컴파일 확인 후 기존 EditMode 전체 시험과 실패 대조를 마쳤다(5,619 통과/8 실패). 새 시험/시험 본문 변경·MSBuild·스테이징·커밋·push 없음. 최종 근거와 남은 세부 인수는 [인계 §14](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#14-mb1mb2-구현과-최종-검증-2026-11-02)를 따른다.

- 2026-11-02 후속 전달 준비: 사용자 진지 구축 정상 확인과 이번 작업 Git 스테이징 요청을 반영했다. 확인 범위·Core 인수 계약·기존 시험 결과·잔여 인수의 현재 요약은 [Core 구현 전달 문서](../handoffs/2026-11-02-movement-stale-context-core-implementation-handoff.md)를 따른다. 이번 준비의 실행 코드/시험 변경은 없다.
