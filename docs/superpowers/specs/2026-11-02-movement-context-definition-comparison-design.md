# 이동 StaleContext — 특성별 값 비교 계약·이동 문맥 수정 설계 (2026-11-02)

- 사용자 결정: 이번 이동 문맥 결함은 우리 측에서 Core까지 직접 수정한다. 후속 지시 "보완점 적용해서 계획 수정해"에 따라 [구조 리뷰](2026-11-02-movement-context-definition-comparison-review.md)의 **대안 A**를 채택한다.
- 원인·로그: [샷건 이동 조사·구현 인계](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md). 실행에서 결과 8의 실패 비교 항목을 관측한 것은 아니다.
- Task·진행 기록: [구현 계획](../plans/2026-11-02-movement-context-definition-comparison-plan.md).
- 상태: **MC1·MC2A·MC2B의 남은 구현과 Unity 컴파일 확인 완료.** 사용자 최종 진행 지시로 결속력 방어 간접 읽기를 포함한 실제 프로필 추적/선택 동결을 연결했다. 최종 기존 EditMode는 5,619 통과/8 실패다. 후속 사용자 진지 구축 정상 확인을 반영했으며 rank별 전수와 실행하지 않은 세부 인수는 [구현 인계 §14](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#14-mb1mb2-구현과-최종-검증-2026-11-02), 세부 Task는 [추적 계획](../plans/2026-11-02-movement-profile-read-tracking-plan.md)을 따른다.
- 새 시험 작성·MSBuild는 하지 않는다. 추상 계약 추가로 컴파일이 깨지는 기존 시험 가짜 타입만 최소 대응한다. 시험 실행/컴파일·재생 확인은 사용자 지시 범위를 따른다. 이번 MC1 기존 시험 실행은 후속 "테스트 돌려" 지시로 수행했다.

## 1. 목표와 소유 경계

같은 설정으로 재생성된 정의·특성을 이동 문맥에서 같다고 판정한다. 실제 의존 값·선택 개체·전투/행위자/경로가 달라지면 기존 거절을 유지한다.

| 소유자 | 책임 |
|---|---|
| Contracts의 TraitSpec 및 구체 특성 타입 | 자기 설정의 명시적 값 비교 계약과 구현 |
| Application의 MovementDefinitionComparison (신규, internal static) | 이동 문맥의 UnitSpec·CombatAttackProfile 조합 비교. 구체 특성의 종류/필드는 해석하지 않음 |
| MovementContextCalculator | 문맥 수집·목록 비교 연결·기존 입장 검증 |
| 기존 CombatCommandCalculator.RequiredProfile | 보유 프로필 우선 및 기본 프로필 대체의 선택 정책. 새 선택기를 만들지 않음 |

OwnedUnitSpecResolver·TraitRankValues의 생성/등급 계산은 유지한다. View·Buffer·SO·씬·Factory·서비스 등록·전역 Object.Equals/GetHashCode는 이번 범위 밖이다.

## 2. 비교 계약

### D1. 특성이 자기 설정을 비교한다 — R1·R2 보완

TraitSpec에 다음 명시적 계약을 추가한다.

```csharp
public bool SameValues(TraitSpec other);
protected abstract bool SameValuesCore(TraitSpec other);
```

- 공개 비가상 진입점에서 동일 참조는 true, null·정확한 런타임 형식·TraitKey 불일치는 false로 처리한 뒤 구체 구현으로 위임한다. 공통 검사를 파생 타입마다 반복하지 않는다.
- SameValuesCore는 각 구체 타입의 **모든 설정 값**을 비교한다. 등급 수치 외에 반경·주기·횟수·지속 시간도 포함한다. 정확한 형식 검사를 통과한 인자만 받는다는 전제를 한국어 XML 주석에 명시한다.
- 현재 구체 타입 37개 모두 구현한다. 훈련 가능한 17개만 따로 분류하지 않는다. 설정 필드가 없는 KillChainAttack·Leap·SteadfastWill은 공통 형식/키 확인 후 true다. 보류/폐기된 Spec도 비교 계약만 맞추며 기능을 다시 활성화하지 않는다.
- DistanceDamageScale은 기존 DistanceDamageScalePercents.SameValues로 목록의 길이·순서·원소 값을 비교한다. 목록 참조 비교로 대신하지 않는다.
- 새 구체 타입은 추상 멤버 구현 없이 컴파일할 수 없다. 미지원 타입을 참조 비교로 대체해 정상 StaleContext로 숨기는 경로는 두지 않는다. **새 필드의 비교 누락까지 컴파일러가 검출하는 것은 아니므로**, 설정 필드를 추가할 때 해당 타입의 비교도 함께 검토한다.
- 기존 TraitStatusSnapshot·AppliedStatus의 자기 값 비교 패턴을 따른다. 상태 스냅샷으로 설정을 비교하지 않는다. 진지 구축 상태에는 반경/피해 감소 설정이 없어 대체할 수 없다.
- Object.Equals/GetHashCode·IEquatable·기존 컬렉션의 동등성 의미는 변경하지 않는다. 값 동등성은 명시적 SameValues를 호출하는 경계에 적용한다.
- 특성별 switch·지원 목록·리플렉션 속성 열거·직렬화·문자열/해시 비교는 추가하지 않는다. 정확한 형식 확인을 위한 GetType()은 속성 탐색과 구분한다.

이는 **공개 C# 소스 계약 추가**다. TraitSpec 파생자는 새 추상 멤버에 대응해야 한다. 생성자·직렬화 필드·DTO/Buffer 형식이 바뀌지 않는다는 사실과 구분한다.

### D2. UnitSpec·보유 프로필의 조합 비교

- 이동 내부 부품에서 UnitSpec 비교를 한 번 정의하고 기본 Definitions와 OwnedProfiles가 함께 사용한다.
- 기존 SameProfile의 정의 필드를 유지한다: Key, Role, MoveMethod, AttackRange, AttackPower, PhysicalDefense, MaxHealth, Priority, CategoryKey, SpecializationCategory, OwnerEligibility, FootprintWidth/Height, FootprintLocalCells, Traits, Behavior, Occlusion, KillReward.
- FootprintLocalCells는 기존 순서/좌표 비교를 유지한다. 파생값 CombatCategoryKey는 별도 비교 사실로 추가하지 않는다.
- Trait 목록은 **TraitKey 기준 집합 동등성**으로 정의한다. UnitSpec은 중복 키를 금지하고 Domain.UnitDefinition은 키로 정렬한다. 길이·키·각 TraitSpec.SameValues를 비교하며 입력 배열의 배치 순서만 다른 경우는 같다. MC2에서 효과 구성 경로까지 순서 비의존을 정적 확인하고, 다른 의미가 발견되면 임의로 구현하지 않는다.
- 불변 목록은 Span/인덱스로 읽는다. 작은 특성 목록은 키를 직접 찾아 비교하며 정렬·배열 사본·새 캐시를 만들지 않는다. 동일 참조/null 검사는 공통 진입점에서 처리한다.
- CombatAttackProfile은 OwnedId, Method 참조, 위 UnitSpec 비교로 판단한다. MovementContextCalculator의 기존 SameProfile 필드 비교는 제거한다.

### D3. 참조 동일성을 유지하는 경계

Method·Behavior·Occlusion·KillReward는 이번 수정에서 **전투 준비에 고정된 원본 객체의 동일성**을 유지한다. 현재 재합성은 이를 그대로 전달한다. 다른 참조로 교체하면 설정 수치가 같아 보여도 기존처럼 다르다고 판단한다.

이는 모든 정의를 대상으로 한 완전한 의미 동등성 선언이 아니다. 이 객체들까지 매번 재생성되는 경로가 발견되면 해당 타입의 계약을 별도 설계한다. 임의로 true 처리하거나 전역 Equals를 추가하지 않는다.

### D4. 실제 프로필 선택과 Capture — MC2B 별도 설계

MC2A는 Capture를 유지했다. 감사에서 행위자·수집자 밖의 의존을 확인한 뒤, 실제 프로필 읽기 추적을 [MC2B 설계](2026-11-02-movement-profile-read-tracking-design.md)로 구체화했다. **구현 전 리뷰 단계**이며 이 문서에서 추적 계약을 중복 정의하지 않는다.

- 선택의 원본은 기존 RequiredProfile이다. Application의 한 Capture 수집기가 실제 반환 결과를 기록하고, Capture는 그 기록을 기존 대상 사실/기본 정의/보유 프로필 목록에 고정하는 안이다.
- 행위자·수집자 목록만으로 전체 의존을 정하지 않는다. 효과가 없거나 범위 밖인 후보도 실제로 프로필을 읽었다면 기록한다.
- 영혼 수집의 Collectors/Owned/SoulCounts/Main/Souls는 프로필 읽기 집합과 분리해 유지한다.
- 불변 CombatCommandInput/공개 DTO에 관찰자를 추가하거나 전역 상태로 추적하지 않는다. 기존 공개 환경 필터와 관찰자 경계를 유지한다.
- API·수명·부정 후보·전달 경로·구현 파일 범위·호환/검증의 원본은 별도 설계, MB1/MB2 순서는 별도 계획이다.

### D5. 기존 거절·오류 경계

전투·행위자·초안 경로·턴·위치·환경·실제 설정 변경의 거절과 무변경 보장을 유지한다. 샷건/진지 구축 특례, 문맥 검사 생략, Tick/Update/폴링은 추가하지 않는다.

상세 실패 항목용 공개 API/로그는 이번에 추가하지 않는다. 구현 후에도 사용자 재현이 계속되면 실제 불일치 항목 계측을 별도 설계한다. 현재 정적 결함과 결과 8의 실제 실행 원인을 구분한다.

## 3. 변경 파일과 호환 범위

CoreCaculator 아래 경로 기준이다.

| 파일군 | 변경 내용 | Phase |
|---|---|---|
| Contracts/Commands/Traits/TraitSpec.cs | 공통 진입점·추상 설정 비교 계약 | MC1 |
| 같은 폴더의 구체 TraitSpec 37개 | 자기 설정 비교 구현. 새 특성 실행 로직은 없음 | MC1 |
| Application/Combat/MovementDefinitionComparison.cs 및 meta (신규) | Unit/Profile 조합 비교 | MC2A |
| Application/Combat/MovementContextCalculator.cs | 비교 연결·중복 제거만 적용. Capture 유지 | MC2A |

기존 시험의 직접 파생 가짜 타입은 아래 3곳이다. **추상 멤버 미구현에 대한 컴파일용 override 추가만** MC1에 함께 한다. 기존 검증 내용·입력·assertion·실행 흐름은 바꾸지 않는다. 다른 실행 실패 수정이 필요하면 먼저 묻는다.

- Assets/Tests/EditMode/BattlePreparationRuleTests.cs: InvalidFocus
- Assets/Tests/EditMode/LegacyEnhancementRetirementTests.cs: ReservedTraitSpec
- Assets/Tests/EditMode/StatusEffectRetirementTests.cs: ReservedTraitSpec

MC1/MC2A 범위는 런타임 C# 40개(기존 39개·신규 1개), 기존 시험 3개와 새 meta 1개다. 신규 파일만 GUID를 만들고 기존 경로·namespace·GUID·asmdef는 유지한다. MC2.1 결과 이 범위를 넘겨야 하면 별도 확인 전 수정하지 않는다.

## 4. 구조 검토와 변경 독립성

| 기준 | 개정 설계의 적용 |
|---|---|
| **Core 구조 — 1순위** | Contracts의 특성은 자기 설정 비교, Application은 문맥 조합/선택을 맡는다. 기존 자기 값 비교 패턴·RequiredProfile을 사용한다. |
| SSOT | 합성/등급 규칙은 기존 Core, 특성 동등성은 각 타입, Unit/Profile 조합 비교는 공통 부품이 소유한다. 프로필 의존/선택 정합은 MC2B 잔여다. 상태 사본이나 이동 전용 특성 스키마를 만들지 않는다. |
| SOLID S/O | 새 특성은 자기 타입에 비교를 구현하며 이동 분기를 고치지 않는다. 문맥 수집과 조합 비교는 분리한다. |
| SOLID L | 모든 Spec은 같은 참조/null/정확한 형식/키와 자기 설정의 동등성 계약을 지킨다. 반사성·대칭성·추이성을 깨는 타입별 예외를 두지 않는다. |
| SOLID I/D | 소비자는 좁은 SameValues에 의존한다. Contracts가 Application/Domain/Buffer를 참조하지 않는다. 이를 위한 새 서비스나 미래용 인터페이스는 만들지 않는다. |
| JMH/FW | 순수 C#·noEngineReferences와 기존 조립 경계를 유지한다. 새 등록/수명/UI/렌더 자원이 없어 해당 FW 계약은 직접 적용 대상이 아니다. 공개 파생 계약 변경 영향은 MC1에서 전수 확인한다. |
| 캡슐화 | 구체 특성의 필드 지식은 해당 타입 안에 둔다. 이동 부품은 TraitKey와 공통 비교만 안다. |
| C#/Unity·if/Tick | 불변 입력·명시적 접근자·Allman·한국어 XML 주석을 따른다. 값 차이에 필요한 분기만 두며 Tick·폴링·전역 캐시는 없다. |

대표 변경 시나리오의 차이는 다음과 같다.

| 변경 | 초기 초안 | 개정안 |
|---|---|---|
| 특성 추가/설정 필드 변경 | 특성/합성 외 이동의 형식 목록·필드 비교도 수정 | 해당 특성 비교와 필요한 기존 생성/등록만 수정. 이동은 수정하지 않음 |
| 이동 문맥의 조합 필드 변경 | 이동 부품에 특성 세부 구조도 함께 존재 | Unit/Profile 조합 부품만 검토. 특성 실행 로직은 재해석하지 않음 |
| 공통 TraitSpec 계약 변경 | 적용하지 않아 확장 누락을 컴파일로 잡지 못함 | 이번 한 번은 37타입·3가짜 타입의 공동 대응 필요. 이후 새 타입은 추상 계약으로 누락 방지 |

기능 분리가 통합 확인을 없애지는 않는다. 이번 공통 계약은 해당 타입들의 정적 대조가 필요하고, 이동 문맥 연결은 사용자 Unity 인수가 필요하다. 관련 없는 Outline·공격 연출·카메라 변경/검증은 요구하지 않는다.

## 5. 검증과 리뷰 지점

- MC1: 실제 모든 TraitSpec 파생 선언과 각 설정 속성·목록을 정적 대조한다. 37개와 가짜 타입 3개는 현재 조사 수이며 구현 때 새 파생자가 추가됐는지 재확인한다. 새 타입 누락은 추상 계약으로, 새 필드 누락은 타입별 검토로 방지한다.
- MC2A: Unit/Profile 필드·특성 키 집합·참조 유지 경계를 대조하고, Capture/Matches/나머지 거절 경로가 편집 전과 동일한지 정적 확인한다. meta·UnityEngine 참조 부재·diff도 확인한다. D4의 의존/선택 정합은 MC2B에서 별도 설계한다.
- 각 코드 Phase 종료 시 멈추고 실제 파일·계약 변경·정적 검토·잔여 조건을 보고한다. MC1만 끝난 상태를 이동 결함 수정 완료로 보고하지 않는다.
- MC3 사용자 Unity: 샷건 rank 1/구매 없음, rank 2·3/다른 특성, 동일 설정 재생성·실제 설정 변경, 기본형 대체·보유 선택 변경, 위치 교환·환경 강제 이동·수집자, 기존 전투/행위자/경로 거절을 확인한다.
- Test 코드 신규 작성·MSBuild·사용자 지시 없는 시험/Unity 실행은 하지 않는다. MC1 후속 실행은 사용자 지시로 수행했으며 결과는 §6을 따른다.

## 6. MC1 구현 기록 (2026-11-02)

사용자 진행 지시로 D1만 구현했다. TraitSpec 공통 진입점과 구체 Spec 37개(스칼라 33·거리 목록 1·무설정 3), 기존 시험 가짜 타입 3개의 컴파일용 override를 추가했다. 설정 속성 62개를 타입별 비교와 정적 대조했고 기존 생성자·설정·시험 검증 본문은 보존했다.

새 파일/어셈블리 참조·전역 Equals/GetHashCode·특성 등록/실행 규칙 변경은 없다. D2~D4의 이동 연결은 아직 구현하지 않았으며 MC1 리뷰 지점에서 멈춘다. 파일 목록·검토 결과는 [구현 인계 §8](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#8-mc1-구현-결과-2026-11-02)에 기록했다. **컴파일 미확인.**

후속 사용자 "테스트 돌려" 지시로 Unity Test Runner에서 Core EditMode 4,637건을 실행했다. 4,636 통과·1 실패이며 도발 활성화 기대 1회/실제 2회 실패는 11월 1일 결과에도 존재한다. 수정 코드의 Unity 컴파일은 확인됐으며, 위 컴파일 미확인은 코드 완료 보고 당시의 상태다. 시험 본문·규칙은 추가 수정하지 않았다. 상세 근거는 [구현 인계 §9](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#9-mc1-사용자-지시-테스트-실행-2026-11-02)를 따른다. 이동 문맥 연결·게임 재생 검증은 여전히 남아 있다.

## 7. MC2 의존 감사 결과·범위 분리 제안 (2026-11-02)

**D4의 기존 참여 집합만으로 전체 프로필 의존이 보호된다고 판단할 수 없어 구현 전 중단했다.** 이동 환경 피해는 MovementProjection.ChangeHealth → CombatDamageApplication.Apply → CombatCommandEffects.CaptureIncoming을 통과한다. 마지막 경계는 수신자 외에 필드의 살아 있는 진지 구축 보유자들의 RequiredProfile도 읽는다. 도착하지 않은 오라 제공자는 기존 행위자/수집자 집합에서 빠질 수 있다.

EntrenchmentTraitStatusSnapshot은 형식만 비교하며 반경/감소율을 갖지 않는다. EffectTargets의 상태 비교도 프로필의 해당 설정을 포함하지 않는다. 따라서 상태가 같다는 이유로 이 설정 의존을 대체할 수 없다. 위치 교환/강제 이동 역시 도착 전 종료·OwnedId 없는 대상 등에서 기존 수집자 조건만으로 프로필 포함을 보장하지 않는다. 실행 재현/새 시험은 하지 않은 **정적 의존 감사 결과**다. 샷건 결과 8의 추가 실행 원인을 확정한 것은 아니다.

설계 D4/계획 MC2.2의 규칙대로 구현 전 중단한 뒤, 아래 분리안을 제안했다. 이후 사용자 "진행해" 지시로 **MC2A 선행·MC2B 별도 설계의 분리를 승인받았다.** 기존의 MC2 한 Phase에서 비교/수집을 함께 변경하는 계획을 대체한다.

- **MC2A:** Capture는 그대로 두고 D2·D3의 Unit/Profile 값 비교와 MC1의 특성 비교만 연결한다. 신규 비교 부품/meta와 기존 MovementContextCalculator의 비교 부분만 수정한다. 기존 실제 상태/경로 거절 검사는 유지한다. 이로써 참조 재생성 결함을 먼저 수정하되 추가 의존 누락은 잔여 사항으로 표시한다.
- **MC2B:** 실제 프로필 읽기 경계에서 의존을 기록하는 방식을 별도 설계한 뒤 RequiredProfile 선택과 Capture를 맞춘다. 오라 조건을 이동 코드에 복제하거나 유닛 종류별 예외를 추가하지 않는다. 피해/특성 존재 판정과 공개/숨김 경계의 추가 파일·영향을 확정한 후 구현한다.

새 프로필 의존을 비교 부품에 특성별 분기로 넣으면 R1의 책임 중복을 되풀이한다. 값 비교와 의존 수집은 변경 이유가 달라 분리했다. 전체 대조표·소스 근거는 [인계 §10](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#10-mc21-프로필-의존-감사와-중단-2026-11-02)이다.

## 8. MC2A 구현 결과 (2026-11-02)

MovementDefinitionComparison에 UnitSpec의 독립 설정 속성 18개와 CombatAttackProfile의 OwnedId/Method/Definition 조합 비교를 구현했다. 특성은 키로 짝을 찾아 각 TraitSpec.SameValues를 호출한다. 발자국 셀 순서와 Method·Behavior·Occlusion·KillReward 참조 정책은 유지한다. 특성별 형식 분기·새 캐시·정렬/배열 사본은 없다.

MovementContextCalculator는 기본 Definitions와 OwnedProfiles 모두 이 부품을 사용하며 기존 SameProfile 필드 중복은 제거했다. 편집 전 소스와 대조해 Capture 전체·Matches·다른 SameValues 조건·SameMover/Target 등 나머지 비교는 동일함을 확인했다. Domain의 TraitKey 정렬, 효과 구성의 키별 상태 조회, 엔진의 Origin 정렬도 확인했다.

이번 Phase 변경은 신규 비교 파일/meta와 기존 MovementContextCalculator뿐이다. 공개 DTO/서비스·View·SO·시험·생성 규칙은 변경하지 않았다. MC2B의 누락 의존 보강과 실제 샷건 재현은 남아 있다. **컴파일 미확인**이며 정적 확인 후 MC2A 리뷰 지점에서 멈춘다. 상세 파일/검토 결과는 [인계 §11](../handoffs/2026-11-02-shotgun-movement-stale-context-core-request.md#11-mc2a-값-비교-연결-구현-2026-11-02)을 따른다.

## 9. MC2B 설계/Task 구체화 (2026-11-02)

사용자 "다음 진행해"로 승인된 MC2B 설계 단계를 진행했다. 직접 이동뿐 아니라 환경 피해 뒤의 존재 재계산·도발·처치/사냥꾼 경로를 포함해 조사했고, 수집기 생성/전달과 Capture 활성화를 별도 코드 Phase로 나눴다.

설계 계약은 [실제 프로필 읽기 추적 설계](2026-11-02-movement-profile-read-tracking-design.md), Task/완료 상태는 [추적 구현 계획](../plans/2026-11-02-movement-profile-read-tracking-plan.md)에서 관리한다. 이번 단계는 문서만 수정했으며 MB1 코드는 미착수다. **컴파일 미확인.**

## 10. MC2B 최종 구현 반영 (2026-11-02)

후속 사용자 지시로 별도 추적 설계 §9의 추가 파일까지 포함해 MB1·MB2를 구현했다. 기존 RequiredProfile 선택 결과만 기록하고 공개 이동 투영의 실제 읽기 집합을 기존 EffectTargets/Definitions/OwnedProfiles에 고정한다. MC2A의 비교와 그 밖의 Collector·환경/경로/전투·게시 검증은 유지한다. Unity 컴파일은 확인했으며 시험/수동 인수의 실제 범위는 인계 §14가 원본이다. 이전 절의 컴파일 미확인·구현 전 상태는 각 단계 시점의 기록이다.

- 2026-11-02 후속 전달 준비: 사용자 진지 구축 정상 확인과 이번 작업 Git 스테이징 요청을 반영했다. 확인 범위·Core 인수 계약·기존 시험 결과·잔여 인수의 현재 요약은 [Core 구현 전달 문서](../handoffs/2026-11-02-movement-stale-context-core-implementation-handoff.md)를 따른다. 이번 준비의 실행 코드/시험 변경은 없다.
