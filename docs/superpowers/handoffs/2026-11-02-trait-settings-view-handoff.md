# 특성 설정값 공개 — View 인계 (2026-11-02, 천명 → View)

Core가 특성 설명에 들어갈 숫자를 **등급·증강·전직이 반영된 최종값**으로 공개했습니다. View는 이 값으로 설명 숫자를 채우고, 옛 경로(특성마다 따로 있던 설명 속성)를 정리해 주세요.

- 설계: [trait-settings-publication-design](../specs/2026-11-02-trait-settings-publication-design.md) · 계획·실행 기록: [trait-settings-publication-plan](../plans/2026-11-02-trait-settings-publication-plan.md)
- 이 변경은 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §64(Description 수치 요청)의 **특성 부분**과 §55 CVI-R1(전투 밖 재생·위압 설정)에 대한 답입니다. 아이템·유물·환경·훈련/증강 미리보기는 아직입니다.
- Core·CoreIntegration 시험 통과(새 실패 0). 화면 동작은 바뀌지 않았습니다(새 값을 더하기만 함).

## 1. 새로 읽을 곳 두 군데

| 화면 | 읽는 곳 | 형식 |
|---|---|---|
| 전투 밖(덱 카드·덱 편집·보유 카드) | `OwnedCharacterStatsSnapshot.TraitSettings` | `TraitSettingsSet` |
| 전투 중(정보창·손패·적 포함) | `CombatStateData.TraitSettings.For(유닛 CharacterCombatFacts)` | `TraitSettingsSet` |

- `TraitSettingsSet`(`Contracts/Snapshots/Traits/`)은 그 유닛이 가진 특성의 설정(`TraitSpec`)을 특성 키 순으로 담습니다. 순서는 `Stats.TraitKeys`·전투 특성 상태와 같아요.
- 꺼내는 법:

```csharp
if (settings.TryGet<StationaryFireTraitSpec>(out var fire))
    text = $"이동하지 않고 공격하면 피해 +{fire.ExtraDamagePercent}%";

if (settings.TryGet<EntrenchmentTraitSpec>(out var entrench))
    text = $"반경 {entrench.RadiusCells}칸 아군의 받는 피해 {entrench.DamageReductionPercent}% 감소";

if (settings.TryGet<TrackingShotTraitSpec>(out var tracking))
    text = $"체력 {tracking.TargetHealthThresholdPercent}% 이상인 적에게 피해 +{tracking.ExtraDamagePercent}%";
```

- 설정 형식은 `Contracts/Commands/Traits/*TraitSpec` 37종입니다. 형식이 없는 키(2연발·행운의 탄환·연쇄 폭발·도어 브리칭·약자 멸시·정밀 사격·이동 공격 할인)는 묶음에 나오지 않아요. "설정 없음 = 문구만"으로 처리해 주세요.
- **전투 중 "보유 개체면 그 개체 설정, 아니면 유닛 기본 설정" 규칙은 `For`가 합니다.** View에서 따로 고르지 마세요. 아직 필드에 없는 유닛(소환 예고 등)은 `ForUnit(유닛 키)`로 읽으면 됩니다.
- 전투 표는 전투 시작 때 한 번 만들어지고 전투 내내 같습니다(전투 중에는 등급이 바뀌지 않음).
- 중첩·잔량·표적·남은 턴 같은 **현재 진행값은 여기 없습니다.** 지금처럼 `TraitStatuses`·상태 효과에서 읽으세요.

§55 CVI-R1에서 요청한 전투 밖 재생·위압 값도 이 묶음으로 나옵니다: `RegenerationTraitSpec.HealAmount`·`HealEveryOwnSideTurnEnds`, `IntimidationTraitSpec.AttackReductionPercent`.

## 2. 옛 전용 속성 정리 (View 작업 — 필수)

아래를 모두 새 읽기(§1)로 옮기고 옛 경로를 지워 주세요.

1. **`GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs`**
   - `Create(visuals, traits, thornFieldDescription, huntersMarkDescription, pullDescription, traitAvailability, reconSquadDescription)`의 특성별 설명 인자와 `traits[i] switch { TraitKey.HuntersMark => …, … }`를 지우고, `TraitSettingsSet` 하나를 받아 숫자를 꺼내게 바꿔 주세요.
   - 전투용 `Create(visuals, ReadOnlySpan<TraitStatusSnapshot>)`도 숫자는 설정 묶음에서 읽게 해 주세요. 상태 스냅샷은 사용 가능 여부(`UnavailableReason` 등)와 현재 진행값에만 씁니다.
   - `DescribeThornField`·`DescribePull`·`ConfiguredValues`(표식·정찰·위압·재생)가 받는 형식을 상태 스냅샷(`*TraitStatusSnapshot`)에서 설정 형식(`ThornFieldTraitSpec`·`PullTraitSpec`·`HuntersMarkTraitSpec`·`ReconSquadTraitSpec`·`IntimidationTraitSpec`·`RegenerationTraitSpec`)으로 바꿔 주세요. 값 이름은 같습니다.
   - `Read`의 "끌어오기·표식·정찰 설명에는 Core 설정 스냅샷이 필요" 검사는 설정 묶음 기준으로 바꿔 주세요.
2. **옛 속성을 넘기는 곳 셋**에서 옛 속성 읽기를 지워 주세요.
   - `GlobalSystem/UISystem/Screens/Global/UnitCardUI.cs:265`(`_displayedStats?.ThornFieldDescription` 등). 전투 중 경로(`_displayed`)는 `CombatStateData.TraitSettings.For(_displayed)`를 넘겨 주세요.
   - `GlobalSystem/UISystem/Screens/Stage/DeckSlotInfoUI.cs:59`
   - `ViewSystem/StageMap/Deck/DeckEditModelBuilder.cs:53`, 그리고 `ViewSystem/StageMap/Deck/DeckEditDisplayModel.cs`의 카드 모델 설명 속성 셋(`HuntersMarkDescription`·`PullDescription`·`ReconSquadDescription`)과 생성자 인자. `TraitSettings` 하나로 바꿔 주세요.
3. **끝나면 Core(천명)에 알려 주세요.** 확인 방법: View 코드(`Assets/ProjectL/Script`, Core 폴더 제외)에서 `ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`·`ImpactBoltDescription`·`ReconSquadDescription` 검색 결과가 0이면 됩니다.

## 3. Core 다음 단계 (예고)

View 정리 알림을 받으면 Core가 **SP-d**에서 `OwnedCharacterStatsSnapshot`의 옛 속성 다섯(`ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`·`ImpactBoltDescription`·`ReconSquadDescription`)과 생성자 인자를 지웁니다. **그 뒤로는 옛 속성을 쓰는 코드가 컴파일 오류가 납니다.** 그래서 §2를 먼저 끝내 주세요.

상태 스냅샷 안에 섞인 설정값(가시밭·표식·끌어오기·정찰·위압·재생)은 규칙·연출이 그 값을 읽는지 Core가 조사한 뒤 따로 정합니다. 지금은 남아 있지만 설명 숫자로는 쓰지 말아 주세요.

## 4. 별건 참고 — 특성 이펙트 세 건

전에 메모로 드린 내용과 같습니다. 모두 Core 변경 없이 View에서 됩니다.

- **진지 구축(320)·추적 사격(410):** 피해가 실제로 바뀐 타격마다 `CombatEffectsEvent`가 옵니다. 종류는 `DamageModified`(140), `TargetIds` = 맞은 유닛, `ResultOrigin` = 그 타격이에요. 외형 에셋 `activations`에 kind 140 줄을 넣고 자리를 대상 몸(SubjectBody)으로 두면 됩니다. 바뀐 피해가 0이면 사실이 오지 않아요.
- **고정 사격(380):** `StatusReset`은 "이동함 → 안 함"으로 바뀔 때만 와서, 배치 직후 첫 턴과 안 움직인 다음 턴에는 오지 않습니다. 상시 이펙트 조건(`CombatTraitAlwaysCondition`) 하나를 더해 `StationaryFireTraitStatusSnapshot.MovedThisOwnTurn == false`인 동안 보유 유닛 몸에 띄워 주세요. 보너스 값은 위 §1의 `StationaryFireTraitSpec.ExtraDamagePercent`입니다.
