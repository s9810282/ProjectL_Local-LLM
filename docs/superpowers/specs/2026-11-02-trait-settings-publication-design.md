# 특성 설정값 공개 설계 (설명 수치 1단계)

2026-11-02. 사용자 요청: "특성부터 설계". 원 분석은 사용자가 받은 `2026-11-02-description-core-scope-analysis.md`(저장소 밖) §1~§3이다. 이 문서는 그중 **특성의 보유·전투 설정 공개**만 다룬다. 훈련·증강 미리보기, 아이템·유물, 환경 숫자는 뒤 단계다(§8).

설계만 했다. 코드·에셋·시험 변경 없음. **컴파일 미확인.**

## 1. 목표

설명 문구의 숫자(예: 고정 사격 "피해 +N%", 진지 구축 "반경 N칸·피해 N% 감소")를 View가 **Core가 실제로 쓰는 최종값**으로 채우게 한다.

- 숫자는 Core가, 문장·토큰·단위는 View가 소유한다(DB 규칙 P6).
- 특성이 하나 늘거나 수치 구조가 바뀌어도 공개 계약·View 화면 코드를 고치지 않게 한다.

## 2. 지금 상태 (코드 확인)

### 2.1 숫자의 원본은 이미 하나다

- 특성 설정의 원본은 `UnitSpec.Traits`(`ReadOnlyMemory<TraitSpec>`)다. 보유 개체는 `OwnedUnitSpecResolver.Resolve(basis, record, catalog)`, 보유가 아닌 개체(적·소환·기본)는 `ResolveBase(spec, catalog)`가 등급·증강·전직을 반영한 최종 `TraitSpec`을 만든다.
- 이 resolver를 쓰는 곳:
  - 보유 카드: `Application/Roster/OwnedCharacterStatsCalculator.cs:86`
  - 전투 준비: `Application/Combat/BattlePreparationCalculator.cs:77`(기본)·`:92`(보유)
  - 전투 명령 입력: `CombatSystem/CombatInputAssembler.cs:78` → `OwnedUnitSpecResolver.BuildProfiles`(기본 프로필 + 보유 개체 프로필)
- 전투 중 규칙은 명령마다 프로필의 `TraitSpec`에서 정의를 다시 만든다(`CombatCommandEffects.cs:57-64`). 프로필 선택 규칙은 `CombatCommandCalculator.RequiredProfile`(보유 ID·유닛 키가 맞는 프로필, 없으면 기본 프로필)이다.
- `TraitSpec`은 public 불변 계약이고 `SameValues`가 있다. `TraitKey` 44종 중 37종이 형식을 가진다(나머지 7종은 SP6)(`Contracts/Commands/Traits/`).

### 2.2 공개는 특성마다 따로, 두 갈래다

| 화면 | 숫자를 받는 곳 | 문제 |
|---|---|---|
| 전투 밖(덱 카드·덱 편집) | `OwnedCharacterStatsSnapshot`의 전용 속성 다섯(`ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`·`ImpactBoltDescription`·`ReconSquadDescription`), `OwnedCharacterStatsCalculator.cs:95-114`가 정의에서 하나씩 뽑음 | 특성 하나를 늘릴 때마다 공개 계약 속성·생성자, Calculator 분기, View 화면 셋(`UnitCardUI`·`DeckSlotInfoUI`·`DeckEditModelBuilder`)과 `UnitCardTraitContent.Create` 인자를 함께 고친다 |
| 전투 중(정보창·손패) | `CharacterCombatFacts.Stats.TraitStatuses`. 일부 특성(가시밭·표식·끌어오기·정찰·위압·재생)의 상태 스냅샷에 설정값이 섞여 있음(`TraitEffectProvider`) | 설정이 상태에 섞인 특성만 숫자가 나온다. 나머지 30여 종은 숫자 경로가 없다 |

View의 소비 지점은 `GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs` 하나이고, 지금은 특성 키별 switch로 위 두 갈래를 받는다.

### 2.3 전투 중 설정은 바뀌지 않는다

등급·증강·전직은 전투 밖(훈련·증강 선택·전직)에서만 바뀐다. 전투 참가 목록도 전투 시작에 고정된다(`CombatStateData.Membership`). 그래서 전투 중 설정은 **전투 준비 때 한 번 정하면 끝까지 같다.** 적 소환·출현도 전투 정의의 유닛 목록(`BattleInitialization.Units`, 소환 대상 포함 — `BattleDefinitionAdapter.cs:187-190`) 안에서만 일어난다.

## 3. 설계

### SP1. 공개 형태는 기존 `TraitSpec`을 그대로 쓴다

설명 전용 DTO를 특성마다 새로 만들지 않는다. 새 계약은 묶음 하나뿐이다.

```csharp
// Contracts/Snapshots/Traits/TraitSettingsSet.cs (가칭)
public sealed class TraitSettingsSet
{
    public static TraitSettingsSet Empty { get; }
    public IReadOnlyList<TraitSpec> Traits { get; }          // UnitSpec.Traits 순서 그대로
    public bool TryGet(TraitKey key, out TraitSpec spec);
    public bool TryGet<T>(out T spec) where T : TraitSpec;   // 화면이 형식으로 바로 꺼냄
    public bool SameValues(TraitSettingsSet other);           // TraitSpec.SameValues로 비교
    internal static TraitSettingsSet From(UnitSpec unit);     // 만드는 입구 하나
}
```

- 숫자의 원본은 resolver가 만든 `UnitSpec.Traits` 하나다. 묶음은 그 참조를 담는 읽기 모델이다(복사·재계산 없음).
- 현재 진행값(중첩·잔량·표적·남은 턴)은 넣지 않는다. 그것은 지금처럼 `TraitStatuses`·상태 효과가 원본이다.

### SP2. 보유 개체: `OwnedCharacterStatsSnapshot.TraitSettings`

- `OwnedCharacterStatsCalculator`가 이미 구한 `resolved`(`:86`)로 `TraitSettingsSet.From(resolved)`를 담는다. 새 계산 없음.
- 검사: `TraitSettings`의 키 순서 = `Stats.TraitKeys` 순서(같은 정의에서 나오므로 늘 같아야 한다, 다르면 생성자 예외).
- `CharacterStatsQueryService`·`CharacterPresentationPublisher`·`CharacterStatsBuffer`는 스냅샷 객체를 통째로 옮기므로 수정 없음(확인 대상).

### SP3. 전투: 전투 고정 사실에 `BattleTraitSettings` 하나

```csharp
// Contracts/Combat/BattleTraitSettings.cs (가칭)
public sealed class BattleTraitSettings
{
    // 유닛 키별 기본 설정(적·소환·성장 없는 보유) + 보유 ID별 설정
    public TraitSettingsSet For(CharacterCombatFacts character); // RequiredProfile과 같은 선택 규칙
    public TraitSettingsSet ForUnit(EntityKey key);               // 아직 필드에 없는 유닛(소환 예고 등)
}
```

- 만드는 곳: `BattlePreparationCalculator`. 이미 `baseUnits`(`:77`)와 보유 `effective`(`:92`)를 구하므로 그 결과를 담기만 한다. 보유 개체는 성장이 없어도 `Resolve` 결과를 담아 `For`가 늘 정확히 찾게 한다.
- 선택 규칙 `For`는 계약 안 한 곳이다. View는 "보유 ID가 있으면 보유 설정, 없으면 유닛 키 기본"을 따로 구현하지 않는다.
- 자리: `CombatPreparation`(전투 고정 사실) → `CombatStateData`가 생성자에서 받고 복사 생성자가 그대로 넘긴다. 명령마다 다시 만들지 않는다(§2.3).
- 명령 입력의 프로필(`BuildProfiles`)과 같은 resolver·같은 정의에서 나오므로 값이 같다. 두 경로가 같은 함수를 부르는 것이 이 일치의 근거다.

### SP4. `CharacterCombatFacts`·`UnitStatsSnapshot`에는 넣지 않는다

- 둘은 사건마다 복사·비교되고, `ContractShapeGuardTests`(`CharacterCombatFacts_PropertiesAreFixed`·`UnitStatsSnapshot_PropertiesAreFixed`)가 속성 목록을 고정한다.
- 전투 중 바뀌지 않는 값을 사건마다 실어 나를 이유가 없다. 전투 고정 사실 한 곳(SP3)이면 충분하다.

### SP5. 규칙·상태 계약은 바꾸지 않는다

- `TraitEffectProvider`, 각 `*TraitStatusSnapshot`, 규칙·효과 실행은 그대로다. 공격·피해·이동 결과는 바뀌지 않는다.
- 상태 스냅샷에 섞인 설정값(가시밭·표식 등)과 보유 스냅샷의 전용 속성 다섯은 **이번에는 남긴다**(View가 새 경로로 옮긴 뒤 정리, §7 Q-SP3).

### SP6. 형식이 없는 특성

`TraitSpec` 형식이 없는 키(2연발·행운의 탄환·연쇄 폭발·도어 브리칭·약자 멸시·정밀 사격·이동 공격 할인)는 `UnitSpec`에 실릴 수 없어 묶음에 나오지 않는다. View는 "설정 없음 = 문구만"으로 다룬다. 이 키들은 지금 꺼져 있거나 다른 경로(강화) 소속이다.

## 4. 바뀌는 곳

| 파일 | 변경 |
|---|---|
| 새 `Contracts/Snapshots/Traits/TraitSettingsSet.cs` | SP1 |
| 새 `Contracts/Combat/BattleTraitSettings.cs` | SP3 |
| `Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` | `TraitSettings` 속성·생성자 인자·키 순서 검사 |
| `Application/Roster/OwnedCharacterStatsCalculator.cs` | `resolved`로 묶음 생성(한 줄) |
| `Contracts/Combat/BattlePreparation.cs`(`CombatPreparation`) | `TraitSettings` 속성·생성자 인자 |
| `Application/Combat/BattlePreparationCalculator.cs` | 기본·보유 설정 모아 `BattleTraitSettings` 생성 |
| `BufferSystems/Combat/CombatStateData.cs` | 속성 하나, 두 생성자에서 받기·넘기기 |

Framework·GlobalSystem·Factory·ServiceKeys·View 무변경. View는 더해진 속성을 원할 때 읽으면 된다(이 단계는 순수 추가).

**기존 시험 영향(생성자 인자 추가 시):** `OwnedCharacterStatsSnapshot` 생성 — `OwnedCharacterStatsCalculatorTests`(4)·`RunDeckContractTests`(1)·`CoreIntegration/ImpactBoltAuthoringTests`(2). `CombatPreparation` 생성 — `CoreIntegration/ControllerTurnCountPublicationTests`·`StageGimmickStatePublicationTests`·`TornadoGimmickPublicationTests`(각 1). 작성자는 착수 때 `git blame`으로 확인한다. 우리 줄은 호출만 맞추고, 다른 사람 줄이면 고치기 전에 묻는다.

## 5. 변경 독립성 점검 (대표 변경 사례)

| 변경 사례 | 지금 | 설계 뒤 |
|---|---|---|
| 새 특성 추가(설명에 숫자 있음) | 보유 스냅샷 속성·생성자, Calculator 분기, View 화면 셋 인자, 표시 switch까지 수정 | `TraitSpec` 형식(원래 필요) + View 문장 한 곳. 공개 계약·화면 연결 무변경 |
| 기존 특성 수치·등급 규칙 변경 | DB·resolver만(이미 국소) | 같음. 공개값이 resolver 결과라 자동으로 따라감 |
| 설명 문장만 변경 | View 문장 | 같음 |

남는 연결(필요한 연결): View가 `TraitSpec` 형식의 읽기 속성 이름을 안다. 숫자를 쓰려면 피할 수 없는 계약이다.

## 6. 보존·검증

- 보존: resolver가 숫자 원본(SSOT), 규칙 결과 무변경, 상태 계약 무변경, 사건 복사 대상 무증가.
- 확인할 것(Test Runner): 보유 카드 묶음 = `resolved.Traits`(등급 반영 값), 전투 `For`가 보유·적·소환·성장 없는 보유 모두에서 명령 입력 프로필과 같은 `TraitSpec` 값을 돌려줌, `CombatStateData` 복사 뒤에도 같은 참조, 키 순서 검사.
- 이 문서는 제안 단계다. 컴파일·실행 확인 없음.

## 7. 결정할 것

**결정 완료(2026-11-02 사용자 "권장안대로"): Q-SP1~Q-SP4 모두 A.** Q-SP3 순서(사용자 지시): View 인계 문서가 View에 옛 전용 속성 읽기 정리를 지시하고, View 정리 알림 뒤 Core가 속성을 지운다(계획 SP-d). Task 계획은 [trait-settings-publication-plan](../plans/2026-11-02-trait-settings-publication-plan.md).

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-SP1 | 전투 설정 자리 | **A. `CombatPreparation` → `CombatStateData` 고정 필드(SP3).** B: `CharacterCombatFacts`에 개체별 필드(사건마다 복사·형태 검사 시험 수정). C: 유닛 키 표만 두고 보유 개체는 보유 스냅샷으로(View가 두 곳을 골라 읽음) |
| Q-SP2 | 새 생성자 인자를 필수로 할지 | **A. 필수**(운영 경로가 빠뜨리면 바로 예외). 기존 시험 호출 7곳 정도를 맞춘다. B: 선택 인자 + 빈 묶음 기본값(시험 무변경, 빠뜨려도 조용히 빔) |
| Q-SP3 | 보유 스냅샷 전용 속성 다섯·상태 안 설정값 정리 시점 | **A. 이번에는 남기고, View가 새 경로로 옮긴 뒤 별도 작업으로 제거.** B: 이번에 함께 제거(View 화면 셋 동시 수정 필요 — View 영역) |
| Q-SP4 | 묶음 타입 | **A. 새 `TraitSettingsSet`(조회 함수 포함).** B: `IReadOnlyList<TraitSpec>`만 공개(조회·비교를 View가 직접) |

## 7.1 구현 메모 (2026-11-02, SP-a·SP-b)

- 구현은 이 설계대로 끝났다. Core·CoreIntegration 시험 통과(새 실패 0, 기존 실패 17 그대로). 실행 기록은 [계획 §1.1·§2.1](../plans/2026-11-02-trait-settings-publication-plan.md).
- **설계와 다른 점 둘:**
  - `UnitSpec.Traits`는 입력 순서이고 `UnitCatalog`는 정의를 특성 키 순으로 정렬한다. `TraitSettingsSet`이 키 순으로 정렬해 담아 `Stats.TraitKeys`·전투 특성 상태와 순서를 맞춘다(SP1의 "UnitSpec.Traits 순서 그대로"를 바꿈).
  - 전투 표 생성 함수는 계약 안이 아니라 `OwnedUnitSpecResolver.BuildTraitSettings`(Application, `BuildProfiles` 옆)다. Contracts는 Application을 참조하지 못한다. 계약은 public 생성자(유닛 키 표 + 보유 ID 표)만 둔다.
- `For`는 보유 항목의 유닛 키가 캐릭터와 다르면 예외다(조용히 기본값으로 넘어가지 않음).
- View 인계: [trait-settings-view-handoff](../handoffs/2026-11-02-trait-settings-view-handoff.md). 옛 전용 속성 정리는 View가 끝낸 뒤 Core SP-d.

## 8. 다음 단계 (범위 밖)

1. 훈련 현재/다음·증강 후보 결과 설정: 같은 `TraitSettingsSet`을 `TrainingChoiceSnapshot`·`TraitGrowthChoices`에 담는다(resolver 재사용, 원 분석 §4).
2. 아이템·유물 설정 공개(원 분석 §5).
3. 환경 숫자(모래폭풍 80·고지대 70·사거리 −1)를 `EnvironmentTerrainRules`에서 이름 있는 원본으로 추출해 공개(원 분석 §6).
4. 지뢰 문구·규칙 불일치는 기획 결정(원 분석 §7-2).

## 9. Phase (안)

- **SP-a** 보유: `TraitSettingsSet`·보유 스냅샷 속성·Calculator 한 줄, 시험 호출 맞춤.
- **SP-b** 전투: `BattleTraitSettings`·`CombatPreparation`·준비 계산·`CombatStateData`, 시험 호출 맞춤.
- **SP-c** View 전달 메모(새 경로 사용법·옛 속성 정리 예고).

결정(§7) 뒤 Task 계획을 쓴다.
