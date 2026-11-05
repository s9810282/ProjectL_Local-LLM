# 보유 카드 공격 무늬 공개 (덱 편집용) 설계

- 작성: 2026-11-03, Core(천명)
- 상태: 코드 완료(컴파일 미확인, Test Runner 확인 대기), View 표시는 미착수(현강님 몫)

## 1. 요청

덱 편집 UI에서 유닛의 공격 모양·조준 종류·스플래시와 범위 칸 무늬를 보여 주고 싶다.
지금 전투 밖 조회(`ICharacterStatsReadSink.ReadOwned()`·`DeckEditorSnapshot.Stats`)에는 사거리 숫자만 있고 지정 방식이 없다.

## 2. 결정 (2026-11-03 사용자)

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-P1 | 무늬에 넣을 규칙 | 모양·각도(아군 정면)·기준 사거리만. 전장 규칙(지형 보정·보드·하붑·벽)과 특성 효과(끌어오기 추가 사거리·캐논 인접 금지·관통 조준)는 넣지 않는다 |
| Q-P2 | 캐논 인접 금지를 전투 밖에서 알기 | Q-P1에서 빠져 해당 없음 |
| Q-P3 | 방향 조준 무늬 | 모든 방향 합침(전투 범위 표시와 같은 모양) |
| 구조 | 계산기 입력·변환·전투와 공유 | 계산기 입력을 공격 프로필로, DB → 지정 방식 변환 하나로, 전투 범위 첫 단계와 같은 함수 |

## 3. 계약

`OwnedCharacterStatsSnapshot.AttackPattern` (`AttackPatternSnapshot`, 필수, null 아님)

| 값 | 뜻 |
|---|---|
| `Method` | 지정 방식 `TargetingMethodSpec`(모양·선택·스플래시·스플래시 비율·각도) |
| `RangeCells` | 공격자 칸 (0,0) 기준 아군 정면의 지정 가능 칸, Y·X 순서. 방향 조준은 모든 방향을 합침 |
| `SplashOffsets` | 주 칸 기준 추가 범위 칸. 스플래시 없음이면 비어 있음(생성자가 검사) |

- 조준 종류는 저장하지 않는다(P7). `AttackProfileCompatibility.GetSupportedAimKind(Method)`로 얻는다.
- 덱 편집은 `DeckEditorSnapshot.Stats`로 같은 스냅샷을 받는다.
- 증강·전직은 지정 방식을 바꾸지 않는다(`TraitGrowthCatalogValidator`가 `basis.Method` 유지). 그래서 무늬는 정의의 지정 방식과 개체별 `Stats.AttackRange`로 만든다.

## 4. 바꾼 곳

| 파일 | 변경 |
|---|---|
| `Contracts/Snapshots/AttackPatternSnapshot.cs` | 새 계약 |
| `Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` | 생성자 `stats` 다음 필수 인자 `attackPattern`, 속성 `AttackPattern` |
| `Application/Roster/OwnedCharacterStatsCalculator.cs` | 입력 `UnitSpec[]` → `CombatAttackProfile[]`(`Calculate`·`CalculateForMains`), 무늬 계산 `ToPattern` = `AppointRangeCalculator.CalculateRangeCells(method, AttackRange, (0,0), Player)` + `AppointShapeCalculator.CreateSplashOffsets` |
| `CharacterSystem/UnitSpecAdapter.cs` | `ReadProfile(IInGameQuery, UnitSpec)`(키 0 거절·결합 오류 감싸기, `BattleDefinitionAdapter`에서 옮김), `ReadMethod(TargetingMethodData)`(필드 대응 유일한 정의) |
| `CombatSystem/BattleDefinitionAdapter.cs` | `ReadAttackProfiles`가 `ReadProfile`을 부름(서명 그대로) |
| `CharacterSystem/CharacterStatsQueryService.cs` | 보유 능력치 조회가 `ReadProfile`로 프로필을 만들어 계산기에 넘김 |
| `Factory/DBFactory/Group/InGameDataBaseFactory.cs` | 성장 유닛 프로필 검사의 변환을 `ReadMethod`로(사본 제거) |

동작 차이: 키 0 유닛 거절 문구가 "전투에 넣을 수" → "공격 프로필을 만들 수"로 바뀜. 보유 능력치 조회도 키 0 유닛이면 예외(지금 아군 에셋 9개 모두 지정 방식 있음).

## 5. 시험

- 호출 맞춤(천명님 시험만): `OwnedCharacterStatsCalculatorTests`·`EnhancementActionCostTests`·`MainSpecializationCombatTests`·`OwnedEnhancementResolutionTests`·`TraitGrowthStatCoexistenceTests`·`TraitStateContractTests`·`RunDeckContractTests`, CoreIntegration `HuntersMarkAuthoringTests`·`ImpactBoltAuthoringTests`·`ReconSquadAuthoringTests`·`RetiredTraitAuthoringTests`.
- 시험 도우미: `UnitSpecTestFactory.DefaultMethod()`(직선·단일)·`Profiles(...)`.
- 새 시험 하나: `OwnedCharacterStatsCalculatorTests.AttackPattern_UsesProfileMethodAndBaselineRangeFromAttackerCell`.

## 6. View 쪽 (미착수)

`DeckEditCardModel`은 `Stats`(`CharacterBaselineStatsSnapshot`)만 들고 있어, 표시하려면 `DeckEditModelBuilder.cs:52`에서 `stats?.AttackPattern`도 모델에 실어야 한다. 모양·조준·스플래시 이름 문구와 무늬 그리기는 View DB·UI 몫이다.
