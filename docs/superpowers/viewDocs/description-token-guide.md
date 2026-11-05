# Description 토큰 정의와 작성 안내

2026-11-02. **§2·7의 환경·회복과 §8.3의 특성 33종·59개 수치 연결은 코드 등록 완료다.** `78fdc81`의 보유/전투 `TraitSettings`에 View를 연결했고, §6의 특성 7종 SO가 실제 이름 토큰을 사용한다. 나머지 특성 SO는 기존 문구를 유지하므로 토큰 등록 완료와 본문 이전 완료를 구분한다. §8.4의 목록/문자열은 별도 설계, §8.5의 아이템·유물·잔여 환경·선택지는 공개 대기다. **컴파일 미확인, 사용자 Unity 확인 대기.** 전체 인수 범위는 [커버리지 목록](description-numeric-coverage.md)을 따른다.

## 1. 어디에 등록하는가

환경 설명의 허용 토큰과 값 출처는 [CombatEnvironmentTokenBindings.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentTokenBindings.cs)가 원본이다. **설명 종류별 등록**이므로 지뢰의 `{피해량}`과 화약통의 `{피해량}`은 서로 다른 공개값을 읽는다.

- SO에는 문장 틀만 적는다. 예: `대상에게 {피해량}의 피해를 줍니다.`
- 연결 코드에는 토큰 이름과 typed 공개값 읽기를 함께 적는다. 이 등록을 템플릿 검사에도 사용한다.
- [CombatEnvironmentDescriptionFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatEnvironmentDescriptionFactory.cs)는 등록된 외형과 연결 코드를 조립한다. 연결 시점에 문법·미등록 토큰·중복 등록을 검사한다.
- [DescriptionTemplate.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/Common/Descriptions/DescriptionTemplate.cs)는 검사한 틀에 값만 넣는다. DB 조회·규칙 계산·키별 판정은 하지 않는다.

새 문구는 해당 설명에 등록된 토큰을 사용할 수 있다. 새 수치가 필요하면 **기능별 연결 코드와 공개값 경로를 먼저 추가**한다. SO에 이름을 적는 것만으로 자동 연결되지 않는다.

특성은 [TraitDescriptionBindings.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Descriptions/Traits/TraitDescriptionBindings.cs), 회복은 [RecoveryDescriptionBindings.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Recovery/RecoveryDescriptionBindings.cs)가 등록 원본이다. Combat 전용 등록부 하나에 다른 씬의 규칙을 모으지 않는다.

## 2. 현재 환경 호버에서 사용할 수 있는 토큰

아래 표는 작성 안내이며 실제 허용 목록은 연결 코드가 원본이다. 모든 행의 소비 화면은 환경 칸 호버다.

| 설명 종류 / SO 필드 | 토큰 | 공개값 | 기존 `{n}` |
|---|---|---|---|
| ObjectVisual Mine / Description | `{피해량}` | `MineSourceFact.Damage` | 같은 값의 별칭 |
| ObjectVisual Flag / Description | `{우선도증가량}` | `FlagSourceFact.PriorityBonus` | 같은 값의 별칭 |
| TerrainVisual WeakGround / Description | `{붕괴횟수}`, `{남은횟수}` | `StaticTerrainFact.EntryThreshold`, `RemainingEntries` | 붕괴횟수의 별칭 |
| TerrainVisual PresenceModifier / Description | `{공격력증가량}`, `{최대체력증가량}` | `DestinationTileModifierFact.AttackBonus`, `MaxHealthBonus` | 없음 |
| TraitVisual DustScreen / TrailDescription | `{사거리감소량}`, `{피해감소율}` | `TraitTrailFact.AttackRangeReduction`, `AttackDamageReductionPercent` | 없음 |
| UnitVisual Structure_PowderBarrel / Description | `{피해량}` | `ObstacleContentProfile.ExplosionDamage` | 같은 값의 별칭 |
| UnitVisual Structure_Supplies / Description | `{피격공격력증가량}`, `{파괴공격력증가량}` | `ObstacleContentProfile.HitAttackBonus`, `DestructionAttackBonus` | 없음 |
| TerrainVisual Tornado / Description | `{이동주기}` | 같은 전투 기믹/원본의 `TornadoMemberSpec.RepeatIntervalControllerTurns` | 같은 값의 별칭 |
| RockfallVisual / Description | `{최대체력손실률}` | 같은 전투 기믹의 `RockfallGimmickSpec.MaximumHealthLossPercent` | 같은 값의 별칭 |

`PresenceModifier.Description`은 현재 에셋에서 비어 있다. 등록된 두 토큰을 사용할 수 있지만 빈 문구를 새 문장으로 채우지는 않았다. 먼지 연막의 **TrailDescription만 이번 연결 대상**이고 특성 아이콘의 Description은 Phase B 대상이다.

고지대·모래폭풍·벽·절벽·보안관 배지·하붑은 현재 새 수치 토큰 없이 기존 문구를 유지한다. 해당 필드에 임의 토큰을 넣으면 오류다. 미공개 규칙값은 [Core 요청 §64](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#64-게임-description-수치-공개-요청-2026-11-02-설계안미구현미전달) 후속으로 연결한다.

## 3. 작성 문법

- 이름은 문자 또는 `_`로 시작하고 이후 문자·숫자·`_`만 허용한다. 대소문자를 구분한다. 중괄호 안의 공백은 허용하지 않는다.
- 숫자에 붙는 `%`, `칸`, `회`, `턴`과 필요한 문장 부호는 SO에 적는다. 수치는 문화권에 영향을 받지 않는 숫자 문자열로 표시한다.
- 같은 토큰을 한 문장에 여러 번 사용할 수 있다. 서로 다른 설명의 토큰을 가져다 쓰려면 그 설명에도 값 연결이 있어야 한다.
- `{피해량 * 2}`, `{피해량:N0}`, `{0}`은 이 명명 토큰 경로에서 지원하지 않는다. 기존 상태 효과·훈련의 `{0}` 형식은 각 기존 formatter에서 계속 사용한다.
- 일반 중괄호 자체를 보이려면 `{{`, `}}`로 적는다. `<color=...>` 같은 Rich Text와 실제 줄바꿈은 보존한다.
- 빈 설명은 유효하다. 환경 호버는 설명을 숨기고 기존 범위 표시를 유지한다.
- 틀에는 현재 값이나 기본값을 써 넣지 않는다. 런타임에 SO 원문을 변경하지 않는다.

코드의 등록 예시는 다음과 같다. 이름·공개값·호환 별칭을 한 항목으로 관리한다.

```csharp
new DescriptionToken<ObstacleContentProfile>(
    "피해량", source => source.ExplosionDamage, "n")
```

## 4. 오류와 갱신 시점

- 잘못된 토큰/괄호/중복 등록: 환경 호버 연결을 준비할 때 `InvalidOperationException`. 메시지에는 설명 종류·키·필드·문제 토큰 또는 위치가 들어간다.
- 수치 원본 없음/형식 불일치: 표시 변환 때 오류. 0이나 이전 개체의 수치로 대신하지 않는다.
- 외형 테이블에 존재하지 않는 선택적 항목은 준비 단계에서 만들지 않는다. 실제 해당 환경을 표시하면 기존 외형 조회 및 설명 조회가 필수 연결을 요구한다.
- 틀은 환경 호버 연결마다 한 번 검사한다. 숫자는 기존 호버가 전달하는 `CombatStateData`에서 읽으며 완성 문구를 종류 키 하나로 전역 저장하지 않는다.
- 변경한 SO 문구는 다음 DB/호버 연결부터 사용한다. 실행 중 SO를 편집해 연결된 읽기 데이터와 틀이 즉시 교체된다고 가정하지 않는다.

## 5. Unity 확인 항목

1. Unity 컴파일 후 지뢰·깃발·화약통·보급품의 실제 설정 수치와 설명을 비교한다.
2. 약한 지반을 밟은 뒤 남은 횟수가 갱신되는지, 서로 다른 수치의 먼지 흔적을 잘못 하나로 묶지 않는지 확인한다.
3. 토네이도별 이동 주기와 낙석 비율, 빈 설명의 범위 표시, 줄바꿈·색상을 확인한다.
4. 필요하면 임시 문구에 등록되지 않은 토큰을 넣어 대상·토큰이 포함된 오류가 나는지 확인한 뒤 원문을 복원한다. 새 Test 코드는 작성하지 않는다.
5. 에셋 값이 바뀌었으므로 기존 `DataBaseAssetTests`도 사용자 확인 항목이다.

## 6. 특성 본문 토큰

아래는 SO 본문까지 이전한 7종이다. 사용할 수 있는 전체 특성 토큰은 **§8.3**을 따른다.

| 특성 / 필드 | 토큰 | 같은 시점의 공개 설정 |
|---|---|---|
| ThornField / Description | `{피해량}`, `{지속턴수}` | `ThornDamage`, `LifetimeUnitTurns` |
| ThornField / DefenseReductionDescription | `{방어력감소율}` | `DefenseReductionPercent`가 양수일 때만 문장 추가 |
| Pull / Description | `{추가사거리}` | `ExtraRangeCells` |
| HuntersMark / Description | `{피해증가율}`, `{지속횟수}` | `BonusDamagePercent`, `DurationAffectedSideTurnEnds` |
| ReconSquad / Description | `{피해증가율}`, `{반경}` | `IncreasePercent`, `Radius` |
| ImpactBolt / Description | `{밀치기거리}` | `DistanceCells` |
| Intimidation / Description | `{공격력감소율}` | `AttackReductionPercent` |
| Regeneration / Description | `{회복량}`, `{회복주기}` | `HealAmount`, `HealEveryOwnSideTurnEnds` |

각 값은 해당 `TraitSpec`의 설정 필드다. 보유/덱은 같은 보유 조회의 `TraitSettings`, 전투/손패는 해당 전투의 `TraitSettings.For(character)`를 사용한다. `TraitStatusSnapshot`은 가용성 안내에만 사용하며 옛 특성별 `*Description` 속성은 읽지 않는다. 완성 문자열을 CharacterKey별로 캐시하지 않는다. 기존 콘텐츠 갱신 경계에서 틀을 검사하고 채우며 매 프레임 전체 외형 Table을 순회하지 않는다.

위압/재생의 코드 수치 줄은 같은 문장의 SO 토큰으로 옮겼다. 이제 보유/덱에서도 같은 설정 숫자를 표시하고 중복 수치 줄은 붙이지 않는다. 다른 특성의 고정 본문 이전은 후속이다. 훈련·증강 본문은 아직 별도 계약 대기이므로 위 표가 해당 선택지에도 자동 연결됐다는 뜻은 아니다. 가시밭의 사용 불가 안내는 계속 표시한다.

Unity 확인에는 같은 정의의 서로 다른 설정 2개체, 덱 상세창/보유 카드/손패/전투 정보의 동일 수치, 가시밭의 조건부 방어 감소 문장, 표식/정찰의 중복 수치 줄 제거를 포함한다.

## 7. 회복 선택지 토큰

| 효과 종류 | 설정 토큰 | 예상량 토큰 |
|---|---|---|
| Heal / Flat | `{회복량}` = `Option.Heal.Value` | `{예상회복량}` = `ProjectedHealAmount` |
| Heal / PercentOfMax | `{회복률}` = `Option.Heal.Value` | `{예상회복량}` = `ProjectedHealAmount` |
| Heal / Full | 없음 | `{예상회복량}` = `ProjectedHealAmount` |
| MaxHealthGrowth | `{최대체력증가량}` = `Option.MaxHealthIncrease` | `{예상최대체력증가량}` = `ProjectedMaxHealthIncrease` |

현재 효과 종류에 없는 토큰은 오류다. 체력 상한 때문에 실제 회복량이 줄어도 본문의 설정량을 임의로 고치지 않으며, 예상량은 Core 값을 읽는다. 바깥 레이아웃의 예상량 줄은 `RecoveryProjected` 또는 새 `RecoveryMaxHealthProjected`를 사용한다. 정량/비율/전체/최대 체력 증가, 효과 없음·골드 부족·적용 완료 상태를 Unity에서 확인한다.

## 8. 문구 이전에 정할 토큰 정의안 (2026-11-02)

사용자 지적 **“그럼 일단 토큰을 정의해야하는거아님?”**에 따라 먼저 정의했고, 후속 **“토큰 정의 해줄래?”**로 §8.3을 실제 등록했다. 작업 순서는 **토큰 정의 → 기능별 코드 등록·값 전달 → SO 문구 작성 → 소비 화면 확인**이다. 이 문서는 설명용 목록이며 런타임 허용 목록과 getter의 원본은 기능별 등록 코드다. 공개 대기·미결 항목은 등록된 토큰과 구분한다.

### 8.1 정의 단위와 공통 규칙

하나의 정의에는 다음을 기록한다.

| 항목 | 정의 |
|---|---|
| 적용 범위 | 설명 종류·키·필드. 예: `TraitVisual / Entrenchment / Description` |
| 토큰 이름 | SO에서 사용하는 정확한 이름. 예: `{피해감소율}` |
| 의미·단위 | 무엇에 대한 수치인지, 단위와 시간 경계. 이름이 같아도 해당 종류의 정의를 따른다 |
| 원본 | 공개 타입과 프로퍼티. View의 임의 상수·InGame DB·Domain 직접 조회는 사용하지 않는다 |
| 시점 | 보유 개체의 최종 설정 / 해당 전투 설정 / 선택지 결과 / 현재 상태 중 하나 |
| 필수 여부 | 문장에 토큰이 있으면 해당 원본이 필수. 없는 값을 0이나 다른 개체 값으로 채우지 않는다 |
| 조건 | 조건부 문장은 공개값을 확인해 기능별로 선택. 템플릿 안 계산·조건식 없음 |
| 상태 | 등록됨 / 정의안·연결 전 / 공개 대기 / 별도 표시 설계 필요 |

- 등록의 식별자는 **설명 종류와 토큰 이름의 조합**이다. 전역 enum 하나나 전역 숫자 Dictionary를 모든 설명에 노출하지 않는다.
- 같은 의미에는 같은 이름을 쓴다: 피해 증가율은 `{피해증가율}`, 피해 감소율은 `{피해감소율}`, 칸 반경은 `{반경}`. 다른 기준이면 이름이나 정의에 구분을 드러낸다.
- `%`·`칸`·`회`·부호는 문장에 쓴다. 공개 백분율은 그대로 읽으며 `20`을 `0.2`로 바꾸거나 배율/증가율을 혼용하지 않는다.
- `int`/`long` 수치는 기존 `decimal` 읽기 경로로 옮긴다. 이 단계에서 parser에 문자열·목록·계산 문법을 추가하지 않는다.
- 기존 §2·6·7 이름은 유지한다. `{n}`은 이미 등록된 환경의 이전 호환용으로만 남긴다. 같은 수치의 임의 동의어를 계속 늘리지 않는다.
- 새 설정 설명 토큰에 현재 중첩·누적 보상·보호막 잔량·남은 턴을 넣지 않는다. 기존 상태 효과의 숫자 인덱스 문법은 유지한다.
- 이름 정의가 게임 규칙을 변경하지 않는다. 반경의 모양·중심 포함·비교 연산·발동 조건은 실제 규칙에 맞는 문장으로 표현한다.

### 8.2 특성의 공용 값 출처

아래 표의 적용 범위는 각 특성의 `TraitVisual.Description`이며 가시밭 추가 문장은 `DefenseReductionDescription`도 포함한다. 시점은 **설정값**이다.

- 보유·덱: 같은 개체 조회의 `OwnedCharacterStatsSnapshot.TraitSettings`.
- 전투·손패·적: 현재 표시 대상과 같은 전투의 `CombatStateData.TraitSettings.For(character)`.
- 값 선택은 `TraitKey`로 해당 `TraitSpec`을 찾는다. 최종 등급·증강·전직 계산은 Core가 수행한다. View에서 기본 정의로 다시 계산하지 않는다.
- 훈련/증강은 앞으로 결과 설정이 제공되면 같은 토큰 정의를 사용한다. 현재 보유 설정을 후보 선택 결과로 대신하지 않는다.
- 기존 5종의 이름을 유지하고 값 출처를 아래 `TraitSpec`으로 옮겼다. 위압/재생도 같은 경로로 연결했다.

### 8.3 특성 토큰 이름과 필드

표의 `이름 → 필드`에서 필드는 해당 행의 `*TraitSpec` 프로퍼티다. 아래 **33종·59개 연결은 코드 등록 완료**이며, 해당 특성의 SO `Description`에 사용할 수 있다. SO 본문까지 이전된 것은 §6의 7종이다. 수치 없는 3종과 목록/문자열은 §8.4에서 별도로 다룬다.

| 특성 / 원본 타입 | 토큰 이름 → 필드 | 의미·단위 |
|---|---|---|
| 가속 / `AccelerationTraitSpec` | `{이동칸당공격력증가량}` → `AttackPerMovedCell` | 자발 이동 칸당 설정 증가량 |
| 발목 부수기 / `AnkleBreakerTraitSpec` | `{지속횟수}` → `DurationAffectedSideTurnEnds` | 대상 진영 턴 종료 횟수 |
| 방어구 부수기 / `ArmorBreakerTraitSpec` | `{방어력감소율}` → `DefenseReductionPercentPerHit` | 타격당 감소 백분율 |
| 피의 부름 / `CallOfBloodTraitSpec` | `{회복률}` → `EnemyMaxHealthHealPercent` | 처치한 적 최대 체력 기준 백분율 |
| 깔끔한 뒷정리 / `CleanSweepTraitSpec` | `{아군공격당공격력증가량}` → `AttackBonusPerAllyAttack`; `{지속횟수}` → `DurationControllerTurnEnds` | 증가량 / 관리 턴 종료 횟수 |
| 결속 / `CohesionTraitSpec` | `{아군당방어력증가량}` → `DefensePerAdjacentAlly`; `{아군당보호막량}` → `ShieldAmountPerAlly`; `{반경}` → `RadiusCells`; `{초기화주기}` → `ResetEveryControllerTurnEnds` | 아군당 기여량 / 칸 / 관리 턴 종료 횟수 |
| 무장 해제 / `DisarmTraitSpec` | `{지속횟수}` → `DurationAffectedSideTurnEnds` | 대상 진영 턴 종료 횟수 |
| 먼지 연막 본문 / `DustScreenTraitSpec` | `{사거리감소량}` → `AttackRangeReduction`; `{피해감소율}` → `AttackDamageReductionPercent`; `{지속횟수}` → `OpposingTurnEndsToExpire` | 칸 / 백분율 / 상대 진영 턴 종료 횟수. 흔적 호버의 현재값과 구분 |
| 진지 구축 / `EntrenchmentTraitSpec` | `{반경}` → `RadiusCells`; `{피해감소율}` → `DamageReductionPercent` | 칸 / 받는 피해 감소 백분율 |
| 마무리 사격 / `FinishingShotTraitSpec` | `{처치기준율}` → `AttackThresholdPercent` | 공격력 기준 백분율. 대상 현재 체력이 그 기준보다 **작을 때** |
| 집중 / `FocusTraitSpec` | `{중첩당추가피해량}` → `DamagePerStack`; `{최대중첩수}` → `MaxStacks` | 추가 피해량 / 중첩 수. 공격력 증가나 현재 중첩으로 표현하지 않음 |
| 일점 포격 / `FocusedBombardmentTraitSpec` | `{필요적수}` → `RequiredEnemyCount`; `{피해증가율}` → `ExtraDamagePercent` | 명 / 가하는 피해 증가 백분율. 적 수는 **정확히 일치** 조건 |
| 땅 다지기 / `GroundCompactionTraitSpec` | `{이동범위증가량}` → `MovementRangeBonus`; `{지속횟수}` → `OpposingTurnEndsToExpire` | 칸 / 상대 진영 턴 종료 횟수 |
| 사냥꾼의 표식 / `HuntersMarkTraitSpec` | `{피해증가율}` → `BonusDamagePercent`; `{지속횟수}` → `DurationAffectedSideTurnEnds` | 받는 피해 증가 백분율 / 대상 진영 턴 종료 횟수. **기존 이름 유지** |
| 사냥꾼 / `HunterTraitSpec` | `{처치당공격력증가량}` → `AttackPerTargetKill`; `{최대보상횟수}` → `MaxRewardsPerStage` | 지정 표적 처치당 증가량 / 스테이지당 횟수. 누적 보상·남은 횟수가 아님 |
| 충격 볼트 / `ImpactBoltTraitSpec` | `{밀치기거리}` → `DistanceCells` | 칸. **기존 이름 유지** |
| 위압 / `IntimidationTraitSpec` | `{공격력감소율}` → `AttackReductionPercent` | 백분율 |
| 넉백 / `KnockbackTraitSpec` | `{밀치기거리}` → `DistanceCells` | 칸 |
| 사기 증진 / `MoraleBoostTraitSpec` | `{공격력증가율}` → `DefeatedEnemyBaseAttackPercent`; `{지속횟수}` → `DurationControllerTurnEnds` | 처치한 적의 **기본 공격력** 기준 백분율 / 관리 턴 종료 횟수 |
| 사령술 / `NecromancyTraitSpec` | `{소환수한도}` → `MaxLivingSummons`; `{첫행동지연}` → `FirstActionDelay` | 같은 출처의 살아 있는 소환수 수 / 행동 타임라인 시간. 턴으로 바꾸지 않음 |
| 관통 볼트 / `PiercingFalloffTraitSpec` | `{대상당피해감소율}` → `ReductionPercentPerTarget`; `{최대관통대상수}` → `MaxTargetsPerRay` | 백분율 / 방향별 설정 대상 수 상한. 실제 타격 수와 구분하며 0을 임의로 무한으로 해석하지 않음 |
| 위치 교환 / `PositionSwapTraitSpec` | `{보호막량}` → `ShieldAmount` | 설정 보호막량. 잔여 예산이 아님 |
| 끌어오기 / `PullTraitSpec` | `{추가사거리}` → `ExtraRangeCells` | 칸. **기존 이름 유지** |
| 정찰 부대 / `ReconSquadTraitSpec` | `{피해증가율}` → `IncreasePercent`; `{반경}` → `Radius` | 받는 피해 증가 백분율 / 직교 탐색 반경(칸). **기존 이름 유지** |
| 재생 / `RegenerationTraitSpec` | `{회복량}` → `HealAmount`; `{회복주기}` → `HealEveryOwnSideTurnEnds` | 회복량 / 자기 진영 턴 종료 횟수 |
| 포효 / `RoarTraitSpec` | `{필요피격횟수}` → `HitThreshold` | 밤 피격 발동 횟수. 현재 누적 피격 횟수가 아님 |
| 희생 / `SacrificeTraitSpec` | `{반경}` → `RadiusCells`; `{최대발동횟수}` → `MaxTriggersPerEnemyTurn` | 칸 / 적 턴당 횟수 |
| 단독 행동 / `SoloActionTraitSpec` | `{보호막량}` → `ShieldAmount`; `{반경}` → `RadiusCells`; `{필요주변점유칸수}` → `RequiredOccupiedNeighborCount`; `{초기화주기}` → `ResetEveryControllerTurnEnds` | 설정량 / 칸 / 주변의 서로 다른 점유 **칸 수** / **자기 진영 턴 종료 횟수**. 유닛 수가 아니며 점유 수는 정확히 일치 조건. 원본 프로퍼티의 옛 이름과 실제 주기 단위를 구분 |
| 고정 사격 / `StationaryFireTraitSpec` | `{피해증가율}` → `ExtraDamagePercent` | 가하는 피해 증가 백분율 |
| 도발 / `TauntTraitSpec` | `{반경}` → `RadiusCells`; `{피해감소율}` → `DamageReductionPercent` | 칸 / 받는 피해 감소 백분율 |
| 가시밭 / `ThornFieldTraitSpec` | `{피해량}` → `ThornDamage`; `{지속턴수}` → `LifetimeUnitTurns`; `{방어력감소율}` → `DefenseReductionPercent` | 피해량 / 유닛 턴 수 / 백분율. **기존 이름 유지**, 양수 방어 감소는 별도 문장 |
| 추적 사격 / `TrackingShotTraitSpec` | `{대상체력기준율}` → `TargetHealthThresholdPercent`; `{피해증가율}` → `ExtraDamagePercent` | 대상 최대 체력 기준 백분율 / 가하는 피해 증가 백분율. 현재 체력 비율이 기준 **이상**일 때 |
| 선봉장 / `VanguardTraitSpec` | `{공격력증가량}` → `AttackBonus`; `{지속횟수}` → `DurationControllerTurnEnds` | 증가량 / 관리 턴 종료 횟수 |

코드 등록의 예시:

```csharp
// TraitKey.Entrenchment의 등록 안에서 사용한다.
DescriptionToken<TraitSpec>.From<EntrenchmentTraitSpec>(
    "피해감소율", source => source.DamageReductionPercent)
```

문장 예시: `주변 {반경}칸 아군이 받는 피해를 {피해감소율}% 감소시킵니다.` 최종 본문에는 해당 특성의 자신 포함 여부·범위 모양 등 기존 조건도 보존한다.

### 8.4 숫자 하나로 정의하지 않는 값과 제외 항목

| 대상 | 원본·현재 상태 | 다음 결정 |
|---|---|---|
| 산탄 `DistanceDamageScale` | `DistanceDamageScaleTraitSpec.PercentByDistance`는 거리별 백분율 목록(0번 = 거리 1) | 목록 표시 방식이 필요. `{거리별피해배율}`은 **후보 이름**이며 현재 parser에 등록 가능한 숫자 토큰이 아니다. 두 거리만 하드코딩하거나 배열 인덱스 문법을 임의 추가하지 않음 |
| 사령술 소환 이름 | `NecromancyTraitSpec.SummonedUnitKey`는 키이며 수치가 아님 | `{소환유닛명}`은 **후보 이름**. View 외형의 표시 이름을 연결하는 문자열 표시 설계가 필요 |
| 가시밭 시간 경계 | `ThornFieldTraitSpec.TickPhase`는 enum | enum 번호를 출력하지 않고 기능이 시작/종료 문장을 선택. 유닛 턴 기능의 보류 상태 유지 |
| 포효 밀치기 | 거리 원본은 보스의 `KnockbackTraitSpec.DistanceCells` | 포효의 숫자로 중복 등록하지 않음. 포효 문장에도 필요하면 같은 설정 묶음의 넉백을 읽는 별도 연결을 설계 |
| Leap / SteadfastWill / KillChainAttack | 설정 수치 없음 | 수치 토큰 없음. 정책 문구 유지 |
| 퇴역 7키 | DoubleShot / LuckyBullet / ChainExplosion / DoorBreaching / ContemptForWeak / PrecisionShot / MoveAttackDiscount | 새 토큰 등록·기능 활성화 없음. 기존 가용성 안내 유지 |

문자열/목록 지원은 기존 D3의 숫자 parser 범위를 바꾸므로 구현 전에 설계 결정을 받아야 한다. 이번 정의 단계에서는 후보와 미결 항목만 남긴다.

### 8.5 아이템·환경·유물·선택지의 정의 상태

환경의 **등록된 이름·원본은 §2**, 회복은 **§7**을 그대로 사용한다. 같은 목록을 별도 실행 테이블로 복제하지 않는다. 아래 새 이름은 모두 **정의안·공개 전달 대기**다.

| 설명 종류 | 토큰 이름 → 원본 또는 필요한 계약 | 의미·단위 |
|---|---|---|
| 위스키 | `{발동체력기준율}` → `WhiskeyItemSpec.TriggerHealthPercent`; `{회복률}` → `WhiskeyItemSpec.HealPercent` | 피격 후 현재 체력의 최대 체력 대비 기준(이하) / 최대 체력 대비 회복 백분율 |
| 로켓 | `{발동피해기준율}` → `RoketItemSpec.TriggerDamagePercent`; `{피해감소율}` → `RoketItemSpec.ReductionPercent` | 아이템 적용 전 피해의 최대 체력 대비 기준(이상) / 피해 감소 백분율 |
| 랜턴 | `{방어력감소량}` → `LanternItemSpec.DefensePenalty` | 고정 감소량. 감소율로 쓰지 않음 |
| 밧줄 | `{우선도감소량}` → `RopeItemSpec.PriorityPenalty` | 고정 감소량. 사용 보류 유지 |
| 고지대 | `{피해감소율}` → Core가 공개할 같은 규칙 설정 | 백분율. 현재 View 읽기 프로퍼티 없음 |
| 모래폭풍 | `{사거리감소량}`, `{피해감소율}` → Core가 공개할 같은 규칙 설정 | 칸 / 백분율. 0 사거리 보존 등 적용 조건은 규칙 확인 필요 |
| 유물 | 효과별 `{피해량}` / `{회복량}` / `{생성량}` / `{지속횟수}`는 **후보 이름** | `ArtifactEffectData`의 `entryId`·효과 종류·값 단위와 공개 전달 계약을 확정한 뒤 유물별로 정의. 첫 번째 효과의 value를 일괄 대입하지 않음 |
| 훈련·증강 | 특성별 §8.3의 이름 재사용 예정 | 현재/다음 또는 선택 결과 `TraitSpec`을 각 문장의 원본으로 전달. 현재 공개된 Amount만으로 누락 설정을 추측하지 않음 |

아이템 타입은 존재하지만 상점·가방·장착 UI에 같은 설정을 전달하는 공개 경로는 아직이다. 유물은 현재 효과 `value`의 의미·단위가 종류별로 다르므로 일반 `{효과값}` 하나로 등록하지 않는다. 미결 항목에 임의 기본 숫자를 넣어 완료 처리하지 않는다.

### 8.6 상태 효과·비교 글과 구현 순서

기존 `UnitStatusVisual.DescriptionTemplate`, `TraitVisual.RankValueFormat`, `StageOfferText`의 `{0}` 계열은 유지한다. 이 정의 단계에서 이름 토큰과 섞거나 한 번 더 치환하지 않는다. 특히 상태 효과 방어력 감소의 `{0}`은 `EffectiveDefense`이며 감소율이 아니다. 훈련 비교의 `{0}/{1}/{2}`는 현재/차이/다음, `{3}/{4}/{5}`는 실제 두 번째 값이 있는 특성의 같은 순서다.

1. 완료: 이름·의미·단위 정의와 §8.3의 종류별 토큰 등록.
2. 완료: 등록/formatter가 `TraitSpec`과 `TraitSettingsSet`을 읽도록 이전하고 보유/전투 소비 경로 연결.
3. 후속: 나머지 SO 본문을 등록된 이름으로 이전. 현재 7종이며 가용성 안내·기존 조건부 문장은 유지했다.
4. 후속: 목록/문자열 등 미결 표시 설계 결정. 공개 대기 범위는 각각의 계약 확보 뒤 진행.
5. 사용자 확인: 같은 정의의 서로 다른 강화/등급 개체, 보유/덱/손패/전투, 조건부 문장과 기존 `DataBaseAssetTests`를 Unity에서 확인.

구조 검토: 숫자 원본은 Core 설정, 허용 목록과 값 읽기는 같은 기능별 등록(SSOT). parser·종류별 연결·문장·Factory 조립을 분리(S/O). 키/타입/필수값을 검사하고 필요한 Contracts만 사용(L/I/D). 새 서비스·루트 Export·UI 등록·수명 변경 없음(JMH/FW). 한 특성 추가는 그 특성 등록·문구에 한정하며 공통 parser 변경은 전체 소비 경로 검토가 필요하다(기능 독립성).

등록/연결 구현은 [계획 §13](../plans/2026-11-02-description-numeric-values-plan.md#13-특성-토큰-등록읽기-연결-2026-11-02)에 기록했다. 특성 scalar 필드 59개와 등록/문서 대응, 토큰을 쓰는 7개 SO와 Table 등록을 정적으로 대조했다. Core·Test 수정, 컴파일·MSBuild·Unity 실행, 커밋·push·외부 발송 없음. **컴파일 미확인.**
