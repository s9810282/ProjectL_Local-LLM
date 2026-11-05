# 샷건 이동 StaleContext — Core 구현·진지 구축 사용자 확인 이력 (2026-11-02)

- 작성일: 2026-11-02
- 작성: View 작업, 사용자 지시로 이번 Core 수정도 직접 담당
- 상태: **MC1·MC2A·MB1·MB2 코드 완료, Unity 컴파일 확인, 최종 EditMode 5,619 통과/8 실패.** 후속 사용자 진지 구축 정상 확인을 반영했다. 현재 Core 전달 요약은 §15의 새 문서를 따른다. 확인한 rank 범위는 별도 미보고다.
- 후속 상태: [Core 구조 우선 검토](../specs/2026-11-02-movement-context-definition-comparison-review.md)의 대안 A를 채택했다. 특성별 설정 비교·추상 계약으로 방향을 바꾸며, 실제 프로필 의존 감사는 MC2의 구현 전 필수 조건이다.
- 사용자 결정: "우리측에서 작업하는걸로 방향 잡자". 이 건의 이동 정의 비교 범위에 한정하며, Test 코드·MSBuild 제한은 유지한다.
- 검증: 후속 사용자 "남은 Phase 끝까지 진행하고 마지막에 검증 및 테스트 작업 진행해" 지시로 추가 파일을 포함해 남은 구현과 최종 검증을 진행했다. 새 시험/기존 시험 본문 변경·MSBuild는 하지 않았다. 이전 절의 미확인 상태는 각 기록 시점의 이력이다.

## 1. 문제 요약

조사 당시 샷건의 이동/경로 확정이 `StaleContext`로 반복 거절됐다. 수정 전 코드에는 **내용이 같은 정의·특성을 매 요청마다 새 객체로 만든 뒤, 이동 문맥에서는 객체 참조로 비교하는 경로**가 있었다. 현재는 수정 및 후속 사용자 정상 확인을 반영했다.

샷건의 `진지 구축(Entrenchment)`은 이 경로를 타는 구체 사례다. 수정 대상은 정의 재합성과 문맥 동등성의 계약이며, 다른 훈련 가능한 특성도 같은 조건에 해당할 수 있다.

**실제 결과 8에서 실패한 비교 항목을 실행 중 관측하지는 않았다.** 아래는 사용자 확인·기존 로그·현재 소스에서 확인한 사실과 우리 측 수정/인수 범위다.

## 2. 보고된 동작과 로그

- 사용자 확인: **샷건으로 유닛 이동/이동 경로를 확정하는 중** 발생했다.
- 보고 로그:

```text
[CombatViewSystem] 결과 8가 거절됐습니다. 사유 코드: StaleContext
```

- 조사 당시 `C:/Users/Hyeon2-400/AppData/Local/Unity/Editor/Editor.log`에서 결과 7·8·9·10·11의 같은 거절을 확인했다. 결과 8은 당시 12286행이다. 로그 행 번호는 이후 바뀔 수 있다.
- 직전 부트 로그: 개발 진입, 로드아웃 1, PlacedStage Test(키 10000).
- 기존 로그에는 행위자 ID·명령 경로·문맥 비교 실패 항목이 없다. 유닛과 동작은 사용자 답변으로 확인했다.
- `CombatViewSystem.FinalizeResult`는 Core가 게시한 거절 결과를 표시한다. `StaleContext`를 직접 판정하지 않는다.
- `CombatCommandService.ProcessNextCommand`는 이동의 `MovementContextCalculator.Matches`가 false이면 `RejectStale`를 게시한다. 경로/문맥 경로 불일치도 같은 사유가 될 수 있으므로 실행에서 실패 항목을 구분해야 한다.

## 3. 소스에서 확인한 원인 경로

| 순서 | 위치 | 확인한 동작 |
|---|---|---|
| 1 | [CombatInputAssembler.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatInputAssembler.cs), Assemble | 미리보기/확정 명령을 조립할 때마다 `OwnedUnitSpecResolver.BuildProfiles`를 호출한다. |
| 2 | [OwnedUnitSpecResolver.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Enhancements/OwnedUnitSpecResolver.cs), Compose | `definition.IsTrainable`인 특성은 구매 이력이 없는 **rank 1도** `ResolveRank`를 호출하고 `changed = true`로 설정한다. 새 `UnitSpec`을 반환한다. |
| 3 | [TraitGrowthDefinition.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Enhancements/TraitGrowthDefinition.cs), ResolveRank | rank 1을 `TraitRankValues.FromNormal(Normal)`로 해석한 뒤 `Apply`한다. |
| 4 | [TraitRankValues.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Enhancements/TraitRankValues.cs), Apply | 진지 구축 등 지원 특성의 새 `TraitSpec` 객체를 만든다. 같은 등급·수치여도 이전 요청과 객체 참조가 다르다. |
| 5 | [MovementContextCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementContextCalculator.cs), Capture | 행위자/관련 수집자의 기본 정의를 `Definitions`, 보유 프로필을 `OwnedProfiles`에 담는다. 기본 프로필과 보유 프로필이 모두 생성되므로 보유 행위자라고 기본 정의 비교가 자동 제외되지는 않는다. |
| 6 | 같은 파일, SameValues·SameProfile | 아래 두 비교가 남아 있다. `UnitSpec`과 `TraitSpec`/`EntrenchmentTraitSpec`은 값 동등성을 구현하지 않아 참조 비교가 된다. |
| 7 | [CombatCommandService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatCommandService.cs), ProcessNextCommand | 문맥 비교가 false이면 이동을 `StaleContext`로 거절한다. |

현재 비교 지점:

```csharp
// SameValues: 기본 정의 목록
a.Definitions.SequenceEqual(b.Definitions)

// SameProfile: 보유 프로필의 특성 목록
x.Traits.ToArray().SequenceEqual(y.Traits.ToArray())
```

따라서 **미리보기와 확정 사이 게임 값이 바뀌지 않아도**, 동일한 의미의 정의·특성을 다시 생성했다는 이유로 문맥이 다르다고 판정될 수 있다. `Definitions` 비교만 수정하면 `OwnedProfiles`의 특성 비교가 남을 수 있으므로 둘을 함께 점검해야 한다.

### 샷건의 운영 데이터 연결

- [Unit_ShotGun.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset)에 `EntrenchmentTraitAuthoring`이 있다.
- [TraitGrowth_Entrenchment.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/TraitGrowth_Entrenchment.asset)은 rank2/rank3가 설정되어 `IsTrainable`이다. 추가 훈련 구매 전의 1등급도 위 재합성 경로를 탄다.
- 해당 정의의 GUID `e03a75aeac9a42ffa81fb4d3f59dd831`은 [TraitGrowthDefinitionTable.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/TraitGrowthDefinitionTable.asset)에 등록되어 있다. `BattleDefinitionAdapter`는 이 성장 테이블을 사용해 카탈로그를 만든다.

## 4. View 경계에서 확인한 내용

- `CombatMovementInteraction.OnPreview`는 요청 번호·초안 Revision·전투·행위자·정규화 경로를 검사한 응답의 `MovementContext`를 보관한다.
- `SubmitFrozen`은 해당 요청 경로와 보관한 문맥을 `CombatViewCommandData.Move`에 함께 전달한다.
- 최근 Outline 방식·겹침 깊이·캐논 피해 영역 경계 작업은 이 이동 문맥/명령 전달 경로를 수정하지 않았다.
- View에서 특성 비교를 복제하거나 문맥 검사를 생략하는 우회는 추가하지 않는다.

## 5. 우리 측 Core 구현 방향

1. TraitSpec에 명시적 SameValues/추상 SameValuesCore 계약을 두고 구체 타입 37개가 자기 설정을 비교한다. 새 타입의 비교 구현 누락은 컴파일 단계에서 드러나게 한다. 이동에 17형식 분기/참조 fallback을 두는 초기 초안은 폐기한다.
2. MovementDefinitionComparison은 UnitSpec·CombatAttackProfile 조합만 비교하고 특성별 설정은 공통 계약에 위임한다. 특성 목록은 TraitKey 기준으로 비교한다. 상세 비교·참조 유지 정책의 원본은 설계 D1~D3다.
3. MC2A는 Capture의 수집 범위/선택을 유지하고 비교만 연결했다. MC2B는 실제 선택 결과를 Application 내부 수집기에 기록하고 기존 문맥 목록으로 고정하도록 별도 설계했다. 구현은 아직 시작하지 않았다(§12).
4. OwnedUnitSpecResolver/TraitRankValues의 생성 규칙·전역 Equals·View 문맥 전달·DTO/Buffer 계약은 유지한다. TraitSpec의 공개 파생 계약 추가와 기존 시험 가짜 타입 3곳의 컴파일용 최소 대응은 계획 범위에 포함한다. 새 시험·MSBuild·실행 검증은 하지 않는다.
5. MC1 특성 계약·MC2 의존 감사 뒤 사용자 승인으로 MC2A 값 비교 연결과 MC2B 의존 추적 설계를 분리했다. 코드 Phase마다 멈춰 보고하며 Unity 인수는 실제 구현 버전별로 구분한다. 결과 8의 실패 항목은 아직 재현으로 특정하지 않았다.

## 6. 인수 조건

- 운영 성장 카탈로그를 사용한 **샷건·진지 구축 1등급·추가 구매 없음** 상태에서 미리보기 → 경로 확정 → ProcessNextCommand가 수락된다.
- 동일 상태에서 프로필/특성을 반복 생성해도 결과가 같다. 진지 구축 2·3등급, 다른 훈련 가능한 특성, 병과 기본 특성 합성도 확인한다.
- 행위자 외에 특성 재합성 대상 수집자가 문맥에 포함되는 경우도 의미가 같으면 수락한다.
- 실제 의존 값이 바뀌면 기존 거절과 무변경 보장을 유지한다.
- 요청/초안 경로·행위자·전투가 맞지 않는 경우의 거절도 유지한다.
- 기존 `OwnedEnhancementResolutionTests.MovementContext_HandlesRebuiltDefinitionsAndRejectsChangedOwnedSelection`의 카탈로그는 rank2/rank3 없는 Focus 일반/강화형이다. 이 시험의 존재가 훈련 가능한 rank 1 재합성을 검증했다는 뜻은 아니다. 이번 조사에서 시험은 실행하지 않았다.

## 7. 구조 검토와 현재 상태

- **SSOT·Core 구조:** 합성은 기존 Core, 특성 설정 동등성은 각 TraitSpec, 프로필 선택은 RequiredProfile이 소유한다. View는 응답 문맥 전달과 거절 표시를 맡는다.
- **SOLID·캡슐화:** 이동은 특성 세부 형식/필드를 해석하지 않고 공통 계약에 위임한다. 대표 변경 시나리오와 남는 공통 파생 계약의 영향은 설계 §4를 따른다. 의존 범위가 실제 사용과 일치하는지는 MC2.1에서 확인한다.
- **JMH/FW:** 새 서비스·등록·Builder·Export·렌더 자원 수명 변경이 필요한 문제로 보이지 않는다.
- **기능 변경 독립성:** 수정 범위는 Core의 정의 합성/이동 문맥 경계로 잡는다. Outline·공격 연출·카메라·일반 특성 실행 규칙을 함께 수정하는 방식으로 넓히지 않는다.
- 이 문서는 우리 측 직접 구현의 원인·공유 기록이다. **MC1 특성 계약/기존 시험 확인 뒤 MC2A 비교 연결을 완료했다. Capture·View·SO·씬은 이번 Phase에서 유지했고 실제 이동 재현과 MC2A 컴파일은 미확인이다.**
- 외부 전달·Git 스테이징·커밋·push는 하지 않았다.

## 8. MC1 구현 결과 (2026-11-02)

### 실제 변경 파일

Core 경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/Traits/`다. 기존 파일만 수정했으며 meta/경로/namespace는 유지했다.

| 파일군 | 파일명 |
|---|---|
| 공통 계약 | TraitSpec.cs |
| 구체 설정 A~D | AccelerationTraitSpec.cs, AnkleBreakerTraitSpec.cs, ArmorBreakerTraitSpec.cs, CallOfBloodTraitSpec.cs, CleanSweepTraitSpec.cs, CohesionTraitSpec.cs, DisarmTraitSpec.cs, DistanceDamageScaleTraitSpec.cs, DustScreenTraitSpec.cs |
| 구체 설정 E~I | EntrenchmentTraitSpec.cs, FinishingShotTraitSpec.cs, FocusTraitSpec.cs, FocusedBombardmentTraitSpec.cs, GroundCompactionTraitSpec.cs, HunterTraitSpec.cs, HuntersMarkTraitSpec.cs, ImpactBoltTraitSpec.cs, IntimidationTraitSpec.cs |
| 구체 설정 K~R | KillChainAttackTraitSpec.cs, KnockbackTraitSpec.cs, LeapTraitSpec.cs, MoraleBoostTraitSpec.cs, NecromancyTraitSpec.cs, PiercingFalloffTraitSpec.cs, PositionSwapTraitSpec.cs, PullTraitSpec.cs, ReconSquadTraitSpec.cs, RegenerationTraitSpec.cs, RoarTraitSpec.cs |
| 구체 설정 S~V | SacrificeTraitSpec.cs, SoloActionTraitSpec.cs, StationaryFireTraitSpec.cs, SteadfastWillTraitSpec.cs, TauntTraitSpec.cs, ThornFieldTraitSpec.cs, TrackingShotTraitSpec.cs, VanguardTraitSpec.cs |
| 기존 시험 가짜 타입 | Assets/Tests/EditMode/BattlePreparationRuleTests.cs, Assets/Tests/EditMode/LegacyEnhancementRetirementTests.cs, Assets/Tests/EditMode/StatusEffectRetirementTests.cs |

### 공개 계약·정적 확인

- TraitSpec에 공개 비가상 SameValues와 보호 추상 SameValuesCore를 추가했다. 파생자는 자기 설정 비교를 구현해야 한다. 기존 37개 구체 타입과 시험 가짜 타입 3개 모두 대응했다.
- 스칼라 설정 타입 33개·거리 목록 타입 1개·무설정 타입 3개, 총 설정 속성 62개를 대조했다. 진지 구축의 반경/감소율, 기간/주기 등 보존 설정도 포함한다. 거리 목록은 기존 전용 값 비교를 사용한다.
- 수정 전 소스와 메서드 추가분을 정적 대조해 생성자·설정 값·시험 본문/검증 내용 보존을 확인했다. 기존 시험 수정은 가짜 타입의 무설정 비교 override뿐이다.
- diff 공백 검사 통과, LF/UTF-8 확인. Core에 UnityEngine·새 서비스/조립·전역 Equals/GetHashCode·이동용 특성 switch/참조 fallback은 추가하지 않았다. 설정은 각 타입이 소유하므로 새 특성은 이동 분기를 수정하지 않는다.
- MSBuild·시험·Unity 컴파일/재생은 실행하지 않았다. 새 시험 작성도 없다. **컴파일 미확인.**

### 남은 작업·리뷰 지점

MC1 완료 후 멈춘다. 이번 변경만으로 이동 StaleContext가 해결된 것은 아니다. 다음 MC2.1에서 실제 프로필 의존을 확인한 뒤 Unit/Profile 비교와 Capture 선택을 연결한다. Unity 컴파일은 새 추상 계약의 전체 파생자 대응을 우선 확인하고, 이동 수락/실제 변경 거절 인수는 MC2 연결 후 진행한다.

## 9. MC1 사용자 지시 테스트 실행 (2026-11-02)

사용자 "테스트 돌려" 지시를 이번 MC1 관련 기존 시험 실행 허용으로 받아 Unity 6000.0.73f1의 Test Runner에서 ProjectL.Tests.Core.dll을 선택 실행했다. 새 시험 작성·MSBuild·MC2 구현은 수행하지 않았다.

- 실행 시각: 2026-11-02 05:14:32~05:14:57 KST, 전체 24.86초.
- 결과: **4,637 실행 / 4,636 통과 / 1 실패 / 건너뜀 0 / 미결정 0**. 다른 어셈블리의 미실행 990건은 이 실행의 건너뜀 수가 아니다.
- 가짜 타입 대응 시험군: BattlePreparationRuleTests 33/33, LegacyEnhancementRetirementTests 17/17, StatusEffectRetirementTests 4/4 통과.
- 수정 소스 뒤 생성된 Contracts·Core 시험 어셈블리로 실행했다. Unity 컴파일 확인으로 갱신한다. §8의 컴파일 미확인은 코드 완료 보고 시점의 이력이다.
- [실행 결과 XML](../../../Logs/CodexVerification/2026-11-02-mc1-trait-value-comparison/editmode-results.xml). SHA-256: `B9D6A7488FF89F0F2E66809997564E7A925949EDB308A459AD6561053E909F44`.

실패는 `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`의 618행이다. 도발 활성화 이벤트 수가 기대 1회인데 실제 2회다. [11월 1일 저장 결과](../../../Logs/CodexVerification/2026-11-01-effect-structure/editmode-results.xml)에도 같은 시험이 같은 메시지로 실패했다. 따라서 MC1에서 새로 발생한 실패라고 볼 근거는 없으며 기존 도발 실패로 기록한다. 이번 요청에서는 실패 시험이나 도발 실행 규칙을 수정하지 않았다.

이번 결과는 기존 시험 회귀와 파생 계약의 컴파일 확인이다. 새 SameValues의 직접 동등성 검증·이동 문맥 연결·샷건 실제 이동 수락까지 증명한 것은 아니다. MC2 의존 감사/연결과 이후 게임 재생 인수는 남아 있다.

## 10. MC2.1 프로필 의존 감사와 중단 (2026-11-02)

사용자 "다음 페이즈 진행해" 지시로 MC2.1의 읽기 경로를 대조했다. 아래 파일 경로는 CoreCaculator 기준이다. 정적 감사이며 실행 재현·새 시험·컴파일은 수행하지 않았다.

| 경로 | 실제 읽는 값/원본 | 기존 기대 사실과의 관계 |
|---|---|---|
| 자발적 이동·Leap·정찰/이동 성공·특성 흔적 | RequiredProfile로 행위자 설정/효과를 읽음 | 행위자 프로필은 기존 기본/보유 수집에 포함됨 |
| 위치 교환 | CombatMovementCalculator.CanLand가 행위자/동승자의 발자국·Behavior를 읽음 | PositionSwapPassenger는 캐릭터 상태다. 도착 전에 사망/종료하거나 OwnedId가 없으면 수집자 프로필에는 포함이 보장되지 않음. 상태 비교에 프로필 정의는 없음 |
| 환경 강제 이동·토네이도 | 면역 Behavior·발자국 프로필을 읽은 뒤 이동/도착 여부 결정 | 면역/중단은 도착 전이며 ArrivalCells는 Terminal/사망을 먼저 검사함. 도착 수집자만으로 읽은 대상 전부를 나타내지 못함 |
| 도착 영혼 수집 | Collect가 보유 기록·캐릭터 사실·Main을 읽음 | 기존 수집자/보유/SoulCounts/Main 비교는 유지해야 함. 기본 UnitSpec을 추가로 읽는다고 가정하지 않음 |
| **환경 피해·진지 구축 오라** | 피해 수신자뿐 아니라 살아 있는 필드의 진지 구축 보유자들에서 RequiredProfile을 읽고 반경/감소율로 피해를 계산 | **움직이지 않고 도착하지 않은 오라 제공자가 행위자/수집자 밖일 수 있음. 상태 비교로 설정 의존을 대신할 수 없음** |

### 추가 의존의 소스 근거

1. [MovementProjection.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementProjection.cs)의 ChangeHealth 375행은 환경 피해를 Damage.Apply에 넘긴다. 미리보기 생성자도 CombatDamageApplication을 사용한다.
2. [CombatDamageApplication.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatDamageApplication.cs) 82~89행은 환경 피해를 CaptureIncoming/효과 엔진에 통과시킨다. 73행의 특성 존재 재계산도 있어 후속 의존 추적 설계에서 함께 확인해야 한다.
3. [CombatCommandEffects.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandEffects.cs) 33~36행은 수신자와 별도로 살아 있는 필드 진지 구축 보유자의 RequiredProfile을 읽는다. 이동/도착 여부로 후보를 제한하지 않는다.
4. [EntrenchmentEffectRule.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Effects/EntrenchmentEffectRule.cs) 33행은 설정의 피해 감소율을, EntrenchmentTraitRule은 설정의 반경을 사용한다.
5. [MovementContextCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementContextCalculator.cs) 67~75행의 프로필 참여자는 행위자와 OwnedId가 있는 도착 수집자뿐이다. 121·162행 이후 EffectTargets 비교에는 이 프로필 설정이 없다. [EntrenchmentTraitStatusSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/Traits/EntrenchmentTraitStatusSnapshot.cs)도 반경/감소율 없이 형식만 비교한다.

**판정:** 기존 집합의 RequiredProfile 선택만 통일해도 모든 필요한 설정이 보호된다는 MC2.1 조건을 통과하지 못했다. 이 누락을 이번 작업에서 새로 만들었다는 뜻은 아니다. 현재 소스에서 확인한 기존 의존 수집 문제이며 결과 8의 실제 실패 항목으로 확정하지 않는다.

### 중단·권장 분리안

계획 MC2.2는 추가 의존 추적/참여 집합 변경이 필요하면 구현 전에 멈추도록 정했다. 이 조건을 적용해 신규 MovementDefinitionComparison/meta와 MovementContextCalculator를 **아직 수정하지 않았다.** MC1 변경과 결과는 유지한다.

기존 Capture를 유지한 값 비교 연결을 MC2A로 먼저 하고 실제 의존 추적/선택 정합은 MC2B에서 별도 설계하는 안을 제안했다. 이후 사용자 "진행해"로 분리를 승인받아 MC2A를 구현했다(§11). 새 오라 조건·특성별 분기·추가 서비스/DTO는 넣지 않았으며 MC2B는 아직 구현하지 않았다.

## 11. MC2A 값 비교 연결 구현 (2026-11-02)

사용자 "진행해" 지시로 승인한 범위만 구현했다. Core 경로는 `CoreCaculator/Application/Combat/`다.

| 실제 변경 파일 | 변경 내용 |
|---|---|
| MovementDefinitionComparison.cs (신규) | internal 무상태 UnitSpec/CombatAttackProfile 조합 비교. 특성 설정은 TraitSpec.SameValues에 위임 |
| MovementDefinitionComparison.cs.meta (신규) | GUID `acb228b4a27e4b98be07cdf704ac6aa7`, 기존 GUID와 중복 없음 |
| MovementContextCalculator.cs | Definitions/OwnedProfiles를 공통 비교에 연결. 기존 SameProfile 필드 비교 제거 |

### 정적 확인

- UnitSpec의 독립 설정 속성 18개를 비교 코드와 대조했다. CombatCategoryKey·UsesPiercingShot 등 파생값은 별도 비교 사실로 추가하지 않았다. 프로필은 OwnedId/Method/Definition을 비교한다.
- 특성은 길이/키/설정으로 비교하며 입력 순서를 의미에 넣지 않는다. UnitSpec의 중복 TraitKey 금지, Domain.UnitDefinition의 TraitKey 정렬, 효과 구성의 키별 상태 조회와 엔진의 Origin 정렬을 근거로 확인했다.
- FootprintLocalCells는 순서/좌표까지 비교하고, Method·Behavior·Occlusion·KillReward는 기존 전투 원본 참조 정책을 유지한다.
- 편집 전/후 소스로 **Capture 전체·Matches·다른 SameValues 조건·SameMover/Target 등 나머지 비교가 동일함**을 확인했다. 기본/보유 동시 수집·참여 집합·정렬·다른 사실 검증은 유지됐다.
- 새 비교 부품에는 구체 특성 분기/지원 표·새 서비스·가변 상태·캐시·정렬/배열 사본·Tick/폴링·UnityEngine 참조가 없다. Unit/Profile 조합은 한 곳, 특성 설정은 각 타입에 남아 있다.
- diff 공백 검사·LF/UTF-8·새 meta/GUID를 확인했다. 신규 파일만 GUID를 만들었고 기존 경로/namespace/meta는 유지했다.
- 시험 작성/수정·MSBuild·Unity/시험 실행은 이번 Phase에 하지 않았다. **컴파일 미확인.** §9의 결과는 MC1 버전이며 MC2A 통과 증거로 사용하지 않는다.

### 남은 확인·리뷰 지점

MC2A 코드/정적 확인 완료 후 멈춘다. 사용자 Unity에서 재합성된 샷건 진지 구축 rank 1·2·3의 이동 수락, 다른 특성/기본 정의·보유 선택의 실제 변경 거절, 특성 순서만 다른 경우를 확인해야 한다. 감사에서 확인한 다른 유닛 프로필의 누락 의존은 MC2B의 별도 설계/구현으로 남는다. 실제 결과 8의 실패 항목을 재현으로 확정한 것은 아니다. 스테이징·커밋·push·외부 전달은 하지 않았다.

## 12. MC2B 설계·Task 구체화 (2026-11-02)

사용자 "다음 진행해"에 따라 다음 MC2B의 설계 단계를 진행했다. 코드 구현은 하지 않았다.

- 계약 원본: [실제 프로필 읽기 추적 설계](../specs/2026-11-02-movement-profile-read-tracking-design.md).
- Task/진행 상태 원본: [추적 구현 계획](../plans/2026-11-02-movement-profile-read-tracking-plan.md).
- 단순 이동/오라 외에 환경 피해 전후의 단독 행동·결속력 존재 재계산, 도발 후보, 처치 특성/사냥꾼 설정 읽기를 확인해 전달 경로에 포함했다. 공개 ProjectRoute 뒤의 Complete/보상 전용 경로는 추적 수명에서 분리했다.
- Application 내부 CombatProfileReadSet을 한 Capture가 소유하고 RequiredProfile의 실제 반환 결과만 기록하는 안이다. 기존 2인자 선택 코드·후보 조건/규칙은 유지하며, 효과 없는 후보도 프로필을 읽었다면 기록한다.
- Capture는 읽기 주체를 기존 대상 사실에, 선택 결과를 기본/보유 정의 목록에 고정한다. 영혼 수집 사실은 별도로 유지한다. 불변 입력/공개 DTO/Domain에 관찰자·수집기를 넣거나 새 서비스/전역 상태를 만들지 않는다.
- 계획 영향은 Application의 런타임 C# 18개(기존 17·신규 1)와 신규 meta 1개다. MB1은 수집기/전달 경로만, MB2는 Capture 활성화/동결이다. 각 코드 Phase에서 멈춰 보고하도록 했다. 자세한 파일/Task는 원본 문서에서 관리한다.
- Core 구조 우선, SSOT·SOLID 5원칙·JMH/FW·변경 독립성·캡슐화·C#/Unity·if/Tick 기준으로 정적 검토했다. 명시적 전달 방식이라 새 프로필 소비 함수의 수집기 전달은 계속 정적 검토해야 하며, 미래 전달 누락을 컴파일러가 전부 잡는다고 주장하지 않는다.

이번 단계는 문서만 변경했다. MB1/MB2 코드·meta·시험·에셋·스테이징·커밋·push·외부 전달은 변경하지 않았다. 설계 리뷰 지점에서 멈추며 **컴파일 미확인**이다. MC1의 이전 시험 결과를 MC2A/MC2B의 통과 증거로 사용하지 않는다.

## 13. MB1 사전 대조 — 추가 전달 파일 1개 확인 (2026-11-02)

사용자 "다음 진행해"로 MB1 준비를 시작한 뒤 실제 원본을 대조하다 기존 설계의 전달 목록에서 누락한 하위 호출을 확인했다. 코드는 편집하지 않았다.

- TraitPresenceReconciler.Calculate 57행은 각 수신자의 CombatEffectiveStatsCalculator.CohesionDefenseBonus를 호출한다.
- CombatEffectiveStatsCalculator.CohesionDefenseBonus 77행은 RequiredProfile로 결속력 설정을 읽는다. 앞선 출처 목록은 죽은 개체를 제외하지만 수신자 순회는 제외하지 않아, 앞선 기록으로 모두 보호된다고 가정할 수 없다.
- [설계 §9](../specs/2026-11-02-movement-profile-read-tracking-design.md#9-mb1-사전-대조-보완--결속력-방어의-간접-읽기-후속-지시로-구현)에 **CombatEffectiveStatsCalculator.cs 1개 추가안**을 기록했다. 내부 CohesionDefenseBonus 마지막에 선택적 수집기 인자를 추가하고, 같은 메서드의 RequiredProfile 및 기존 TraitPresenceReconciler 호출에 전달하는 변경이다.
- 결속력 방어 산식·특성/사망 후보 조건·공개 능력치 API·기존 호출의 null 동작은 유지한다. MB1은 기존 17 C#에서 18 C#으로, 전체 추적 계획은 18 C#에서 19 C#으로 늘어난다. MB2 범위는 그대로다.
- 승인 범위를 넘는 결정을 먼저 확인하라는 AGENTS.md 9행과 추적 설계/계획의 추가 파일 확인 규칙에 따라 코드 편집 전에 멈췄다. 후보 조건을 복제해 이 읽기를 수집기 밖에서 흉내 내거나 기록을 누락하는 우회는 하지 않는다.

사전 대조 당시에는 범위 개정안 확인 대기·컴파일 미확인이었다. 이후 최종 진행 지시를 받아 구현한 현재 상태는 §14를 따른다.

## 14. MB1·MB2 구현과 최종 검증 (2026-11-02)

### 진행 범위와 구현

사용자 최종 지시로 §13의 CombatEffectiveStatsCalculator까지 포함해 MB1·MB2를 연속 구현하고 마지막에 검증했다. MC2B 실제 변경은 Application의 C# 19개(기존 18·신규 1)와 신규 meta 1개이며, 파일별 책임 원본은 [추적 설계 §6](../specs/2026-11-02-movement-profile-read-tracking-design.md#6-변경-파일--구현-시-허용-범위)다.

- CombatProfileReadSet은 EntityId별 실제 선택 결과를 한 Capture 동안 기록한다. 반복 읽기는 중복 제거하고 같은 개체의 혼합 선택은 예외로 드러낸다. 읽기 전용 사전만 내부에 공개한다. 신규 meta GUID: `2eb691f66f7f4d759356f78813f6bc9c`.
- RequiredProfile의 기존 2인자 선택 본문을 유지하고 내부 3인자 오버로드에서 같은 반환 객체를 기록한다. 보유 우선·성장 개체 누락·기본형 대체 판단을 Capture에 복제하지 않았다.
- Actor/CanLand에서 시작해 이동/위치 교환·강제 이동 면역·정찰/흔적·토네이도·환경 피해 수신자/진지 구축 오라·피해 전후 존재/도발·결속력 방어·처치/사냥꾼에 같은 수집기를 전달했다. 효과 없는 후보도 기존 계산이 프로필을 읽으면 기록한다.
- Capture만 새 수집기를 활성화한다. 읽은 주체는 기존 EffectTargets에 합치고, 선택된 기본 Definitions/보유 OwnedProfiles를 기존 키 순서로 고정한다. 입력에 없는 주체나 그룹 내 혼합 선택은 조용히 제외하지 않는다.
- Collectors/Owned/SoulCounts/Main/Souls, 공개 환경/숨김 재필터, 전투/행위자/초안 경로 및 다른 사실 비교, MC2A 값 비교와 기존 게시 경계는 유지했다. MC2B에서 Contracts/Domain/공개 DTO·With/Buffer/View/씬/에셋/Factory/서비스는 수정하지 않았다.

### 정적 확인과 구조 검토

MB1의 62개 전달 편집을 편집 전/후 소스와 역대조해 기존 선택/후보/정렬/규칙/산식 본문이 유지됨을 확인했다. MB2 Capture와 생성자 호환 수정도 준비된 편집 결과와 정확히 대조했다. 새 GUID 중복·UTF-8/LF·Core UnityEngine 참조 부재·공백 diff를 확인했다.

선택은 기존 RequiredProfile, 후보는 기존 각 계산기, 설정 비교는 TraitSpec/MovementDefinitionComparison, 추적은 지역 수집기, 동결은 Capture가 각각 소유한다. 새 특성 switch·가변 입력/관찰자·전역/스레드 상태·서비스/이벤트/Tick은 없다. 조립은 기존 이동/피해 생성 경계에서 연결한다. 미래의 프로필 소비 함수가 수집기를 전달하는지는 계속 호출 경로 검토가 필요하며 컴파일러의 완전 검출을 주장하지 않는다.

### Unity 컴파일과 기존 시험

Unity 6000.0.73f1에서 최종 Core/시험 어셈블리 컴파일을 확인했다. 최종 컴파일 로그는 `Tundra build success (7.59 seconds), 29 items updated, 1448 evaluated`다. CombatCommandEffects의 기존 nullable 주석 경고 CS8632가 남으며 변경 전 144행에서 같은 경고가 난 기록도 있다. MSBuild는 실행하지 않았다.

첫 전체 실행은 5,627건 중 5,614 통과/13 실패였다. 그중 5건은 기존 시험이 리플렉션으로 호출하는 MovementProjection(input, bool) 생성자를 3인자로 바꾼 호환 회귀였다. 기존 2인자 생성자를 복구하고 3인자 구현에 null로 위임했다. 시험 본문/판정은 수정하지 않았다. 이후 Unity 재컴파일과 전체 재검증을 진행했다.

| 최종 EditMode | 전체 | 통과 | 실패 |
|---|---:|---:|---:|
| Core | 4,637 | 4,636 | 1 |
| CoreIntegration | 847 | 841 | 6 |
| CombatField Editor | 53 | 53 | 0 |
| FbxPlacement Editor | 58 | 57 | 1 |
| OutlineNormals Editor | 3 | 3 | 0 |
| JourneyUI Editor | 28 | 28 | 0 |
| Addressables DocExample | 1 | 1 | 0 |
| **합계** | **5,627** | **5,619** | **8** |

최종 실행 시각은 **2026-11-02 06:02:34~06:03:55 KST**, 건너뜀/불확정 0이다.

- [최종 결과 XML](../../../Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/editmode-results.xml)
- [호환 수정 전 첫 전체 결과](../../../Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/editmode-all-initial-results.xml)
- [컴파일 로그 발췌](../../../Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/compile-evidence.log) · [검증 요약·최종 소스 해시](../../../Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/verification-summary.json)

생성자 호환 실패가 났던 TornadoGimmickPublicationTests는 최종 5/5 통과했다. 관련 기존 시험군도 MovementContract 14/14, PlayerTurnMovement 17/17, PositionSwapMovement 15/15, MovementEnvironment 22/22, HighGroundMovement 8/8, WeakGroundMovement 20/20, MovementEffectPublication 10/10, MovementPublication 3/3, TraitMovementAttackPublication 7/7 통과했다. 이는 기존 회귀 범위이며 새 읽기 집합의 모든 수치/선택 조합을 직접 검증한 새 시험은 아니다.

### 남은 실패 8건의 대조

| 구분 | 실패/근거 | 판단 |
|---|---|---|
| 이전과 동일 1 | FbxPlacementTests.PrefabCatalog_ListsRegularAndVariantAndAddsAnIndependentVolumeProfile, EditorWindow.Close의 NullReferenceException(690행) | 11월 1일 XML에도 같은 시험/메시지 실패 |
| 이전과 동일 1 | TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True), 기대 1/실제 2(618행) | 11월 1일 및 MC1 XML과 같음 |
| 이전과 동일 1 | CombatCameraZoomPoseTests의 CinemachineCameraOffset 형식 로드 실패(247행) | 11월 1일 XML과 같음. FieldCamera 영역은 수정하지 않음 |
| DB 1 | CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools, 샷건 SteadfastWill 기대 1/실제 0(194행) | 현재 사용자 에셋은 PositionSwap만 가진다. 이번 작업에서 에셋을 바꾸지 않음 |
| DB 1 | GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait, KillChainAttack 기대/실제 빈 목록(242행) | 현재 사용자 Akimbo 에셋의 traits가 비어 있음 |
| DB 1 | RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases, 샷건 1개 기대/실제 상점 후보 5개(68행) | 현재 Shop_Unit의 후보 5개와 시험 기대가 다름 |
| 준비 2 | CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider/Heavy), BattlePreparationCalculator.Place의 보드/점유 충돌(시험 368행) | 이동 Capture 전 초기 배치 준비에서 실패. 준비 계산/입력 에셋은 이 기능에서 수정하지 않았으며 어느 배치의 충돌인지는 추가 계측하지 않음 |

DB/준비 5건은 예전 저장 XML에 같은 실패 이력이 없으므로 모두 “기존 실패”라고 단정하지 않는다. 실패 지점과 현재 데이터/이번 변경 경계를 구분해 기록한다. 다른 작업자의 시험 판정/입력·카메라·배치/도발 실행 규칙을 바꿔 통과시키지 않았다.

### 게임 재생 확인과 잔여 인수

기존 Play From Boot 설정(Combat, 개발 로드아웃 1, 챕터 1/노드 2, 지정 Test 스테이지 10000, 시작 아이템 4줄)을 그대로 사용했다. 최대화 재생은 FbxPlacementWindow의 Invalid editor window 오류로 반복 일시정지되어, 재생 화면만 임시로 Play Focused로 바꿔 Combat 진입과 샷건/메인 배치를 확인했다. 재생 종료 뒤 Play Maximized로 복구했다. 씬 저장/설정 에셋 편집은 하지 않았다. [재생 시도 로그](../../../Logs/CodexVerification/2026-11-02-movement-profile-read-tracking/playmode-attempt.log)를 남겼다.

전투 시작 입력 뒤 전투 국면 전환/샷건 이동 확정은 확인하지 못했다. 이 시도에서 새 StaleContext 로그가 없다는 사실만으로 이동 수락을 입증하지 않는다. 현재 샷건 에셋은 PositionSwap 설정이므로 원래 진지 구축 rank 1·2·3 재현 조건과도 다르다.

**코드 Phase와 기존 시험 실행은 끝났다. MC3/MB3의 새 프로필 추적 경계별 수동 인수는 미확인으로 남긴다.** 동일 설정 재생성/실제 값·키·선택 변경, 움직이지 않은 오라 출처/효과 없는 후보, 사망/면역·도착 전 종료, 존재/처치 설정 변경, 미사용 프로필/숨김만 바뀐 경우를 각각 실행 검증했다고 표시하지 않는다. 결과 8의 실제 비교 실패 항목도 직접 계측하지 않았다.

커밋·push·Git 스테이징·외부 전달은 하지 않았다. 이전 통합 인계 파일은 읽거나 수정하지 않았으며 이 문서에 Core 변경과 검증을 기록했다.

## 15. Core 전달 준비·후속 사용자 확인 (2026-11-02)

사용자가 진지 구축 정상 확인 후 Core 전달 문서 작성과 작업물 Git 스테이징을 요청했다. 확인한 rank/추가 구매/오라 조건은 미보고이므로 이전 §14의 세부 미확인을 전수 통과로 바꾸지 않는다. 현재 전달 요약은 [Core 구현 전달 문서](2026-11-02-movement-stale-context-core-implementation-handoff.md)이며, 공개 소스 계약·인수 경계·시험 8건의 실패 근거·잔여 검증을 자립적으로 읽을 수 있게 정리했다.

이번 준비에서는 실행 코드를 바꾸거나 시험을 다시 실행하지 않았다. 이번 수정의 C# 61개(운영 58·기존 시험 가짜 타입 3), 신규 meta 2개, 관련 문서 7개를 스테이징 대상으로 지정했다. 사용자 에셋/씬·다른 작업 문서·Git ignore된 로컬 XML/로그는 대상에서 제외한다. 이전 통합 인계 파일은 사용하지 않으며 커밋·push·외부 발송은 하지 않는다.
