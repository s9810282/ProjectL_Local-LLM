# 옛 공격 지정 방식 필드 제거 — 설계와 계획

**날짜·상태:** 2026-10-06. 설계 승인(D1~D7 권장안, D2는 선인장 캐논 형태 확인). Phase 1 완료(Unity EditMode 통과). Phase 2 완료(Unity EditMode 통과). Phase 3 완료. 전체 Phase 완료. 브랜치 `feature/Cheon/refactor/core-system`, Claude는 커밋하지 않는다. Phase마다 멈추고 보고한다.

**근거:** 지정 방식 구현 계획 [§6](2026-10-01-attack-targeting-methods-implementation-plan.md)의 "에셋·호출자 이전 증거가 생긴 뒤 중복된 옛 필드·overload를 제거한다". 지금 유닛 5개(Main·Akimbo·Demon·Sniper·ShotGun)가 모두 `targetingMethod`를 참조하고, 승인된 지정 방식과 일치함을 `DataBaseAssetTests.InitialUnit_TargetingMethodMatchesApprovedProfile`이 확인한다. 실제 전투 준비(`DatabaseBattleInitializationSource` → `BattleDefinitionAdapter.ReadAttackProfiles`)도 DB의 지정 방식을 읽는다.

## 1 현재 상태 (문제)

공격 모양이 두 곳에 기록돼 있다(SSOT 위반).

| 정보 | 새 원본 | 옛 기록 | 옛 기록이 아직 하는 일 |
|---|---|---|---|
| 모양·선택·스플래시 | `UnitDataSO.targetingMethod` → `TargetingMethodData` → `CombatAttackProfile.Method` | `UnitDataSO.appointMethod`(`AppointMethod` enum), `attackTargetMode`(`AttackTargetMode` enum) | 실제 범위·대상 계산에는 쓰이지 않는다. 검사 규칙에만 쓰인다(아래) |
| 사거리 | `UnitDataSO.attackRange` (원본은 하나) | 옛 `AttackPatternSpec(AppointMethod, range)`에 담겨 전달 | `UnitSpec.AppointRange` → `UnitStatsSnapshot.AppointRange` → 전투 사거리 |

옛 기록이 남아 있는 검사 규칙:

| 위치 | 규칙 | 새 지정 방식에서의 상태 |
|---|---|---|
| `UnitData.ValidateMainCharacter` | 메인은 옛 RadialSquare·사거리 1·SingleTarget | `DataBaseAssetTests`가 메인의 지정 방식(1번)을 이미 확인 |
| `UnitData.ValidateAttackProfile` / `UnitSpec` 생성자 / `UnitDefinition` 생성자 / `UnitCatalog.ValidateAttackConfiguration` | `AttackProfileCompatibility.GetSupportedAimKind(category, AttackPatternSpec, AttackTargetMode, impact)`: 일반 Category는 SingleCell 충격이고 옛 RadialSquare+SingleTarget 또는 옛 전방형+AllHostiles, Cannon은 옛 모양+AllDamageables+특정 충격 조합 | Category–충격 관계만 의미가 남는다. 옛 모양·대상 방식 조합은 새 경로와 무관 |
| `UnitCatalog.ValidateAttackConfiguration` | 옛 ForwardExpanding/ForwardCone 공격자는 1×1 | `CombatAttackProfile`이 다칸 유닛을 모든 새 지정 방식에서 거부(더 넓은 규칙)하므로 중복 |
| `UnitCatalog.ValidateTraitCompatibility` | ThornField 특성은 옛 판정 Cell 조준 + Square 충격 | 실제 가시밭 적용은 새 지정의 `PrimaryCell`을 쓴다. 새 기준은 "칸 선택(Single)" |
| `UnitStatsSnapshot.AppointMethod` | 값 보관·복사·검증, Buffer 비교기가 비교 | 읽어서 판단하는 곳이 없다 |

**View 영향:** View 실제 코드(`ViewSystem/Combat` 등)는 옛 지정 방식을 읽지 않는다. 공격 가능 칸·조준은 Core의 `AttackRangeResult`(`AttackAims`)로 받는다. 옛 필드를 쓰는 View 쪽 코드는 개발용 `ViewSystem/Dev/DevBattleInitializationSource`(Core 입력을 직접 만드는 fixture) 한 곳이다.

**위험:** `targetingMethod`만 바꾸고 `appointMethod`를 두면 검사가 옛 모양 기준으로 돈다. 반대로 인스펙터의 `appointMethod`를 바꿔도 게임 동작은 그대로라 기획 입력을 오해하게 만든다.

## 2 목표 구조

| 항목 | 바뀐 뒤 |
|---|---|
| DB | `UnitDataSO`·`UnitData`에서 `appointMethod`·`attackTargetMode` 제거. `attackRange`·`targetingMethod`는 유지 |
| Contracts | `AttackPatternSpec` 삭제. `UnitSpec`은 `int AttackRange`를 직접 가진다(`AttackPattern`·`AppointMethod`·`AppointRange`·`AttackTargetMode` 제거). `AttackProfileCompatibility`의 옛 overload 삭제(새 지정 방식 overload만 남음). `UnitStatsSnapshot.AppointMethod` 제거(`AppointRange`와 별칭 `AttackRange`는 유지, 범위 밖) |
| Domain | `UnitDefinition`도 같은 방식으로 `int AttackRange` |
| 검사 이전 | Category–충격 관계 → `AttackProfileCompatibility.RequireSupportedImpact` 하나에 두고 `UnitSpec`·`UnitDefinition`(Phase 3에서 `UnitData`도)이 호출(D3, SSOT). ThornField–지정 방식 → `CombatAttackProfile`(D2). 메인 공격 형태 → `DataBaseAssetTests` 기대값(D1). 전방형 1×1 → 삭제(중복) |
| enum | `AppointMethod`·`AttackTargetMode` 삭제(D7) |
| 호출자 | `BattleDefinitionAdapter`, `TemporaryCoreDatabase`(개발), `CharacterCombatFactsComparer`(Buffer), Core 계산기 4곳의 `new UnitStatsSnapshot(…)`, `DevBattleInitializationSource`(View 개발 fixture, D4) |

새 규칙 후보(D3):

- 일반 Category(Base·Rider·Heavy): 충격은 `SingleCell`, 범위 0.
- Cannon: 충격은 `SingleCell`이 아니고 범위 1 이상(기존 `UnitCatalog` 규칙 유지).
- 옛 Cannon 조합(옛 모양 + 충격 종류 짝)은 새 경로에서 의미가 없어 옮기지 않는다.

## 3 범위 밖 (후속 후보, 이번에 하지 않음)

| 후보 | 이유 |
|---|---|
| `ImpactPatternSpec`의 Cannon `Square`·`HorizontalRow`·`PierceRayCount` | 새 공격 경로는 관통 대상 예산(`ImpactPattern.Range`)만 쓴다. 사각 충격은 지정 방식의 스플래시가 대신하고, 관통 광선 수는 계산기에서 3으로 고정이다. 실제 Cannon 유닛 에셋이 없어 이번 제거와 분리해 조사한다 |
| `AttackAimKind.Entity`·`AttackAim.ForEntity` | 옛 개체 조준. View는 Cell·Direction·All만 보낸다. `AttackAimKind`는 View·Buffer가 쓰는 공용 계약이라 따로 다룬다 |
| `AttackFailureReason.NotSupportAppointMethod` | 사용처 없음. enum 숫자 값 변화 여부를 확인한 뒤 따로 정리 |
| `UnitStatsSnapshot.AppointRange`/`AttackRange` 별칭 정리 | 이름만의 중복이고 사용처가 많다(테스트 48곳) |

## 4 결정 필요

| ID | 질문 | 권장 |
|---|---|---|
| D1 | 메인 캐릭터의 "공격 형태 고정" 규칙(옛 RadialSquare·1·SingleTarget)을 어디에 둘까 | 실행 검사에서는 빼고, `DataBaseAssetTests`의 메인 기대값(지정 방식 1번)에 사거리 1 확인을 더한다. PlayerOnly·1×1·특성 없음 검사는 유지. 특정 유닛의 공격 형태는 기획 데이터이지 코드 불변식이 아니다 |
| D2 | ThornField 특성 제약 | `CombatAttackProfile`에서 "ThornField는 칸 선택(Single) 지정 방식에서만" 검사(관통 특성 검사와 같은 자리). 옛 Square 충격 조건은 버린다 |
| D3 | Category–충격 규칙 | §2 후보대로. 규칙은 `AttackProfileCompatibility.RequireSupportedImpact` 한 곳에 두고 호출만 한다. Cannon 충격 범위 1 이상은 기존대로 `UnitCatalog`(전투 준비 시점) |
| D4 | View 개발 fixture `DevBattleInitializationSource`는 `UnitSpec` 생성자 변경으로 컴파일이 깨진다 | 같은 Phase에서 인자만 맞추는 최소 수정을 하고 View 공유 메모에 기록한다. 따로 맡기면 그동안 프로젝트가 컴파일되지 않는다 |
| D5 | DB(`UnitDataSO`·`UnitData`, DB-SO 설계는 View 작업자 영역) 수정을 이번 작업에 포함할까 | 포함한다(지난 특성 작업 Phase 3과 같은 방식). DB-SO 설계 문서 담당자에게 공유 메모로 알린다 |
| D6 | 에셋 YAML에 남는 `appointMethod`·`attackTargetMode` 키 | 에셋은 손대지 않는다. Unity는 모르는 키를 무시하고 다음 저장 때 지운다. 옛 `directionalAttackPattern` 잔여와 같은 처리 |
| D7 | 쓰는 곳이 없어지는 `AppointMethod`·`AttackTargetMode` enum | 삭제한다. CLAUDE.md의 DB 규칙 P4 공용 타입 목록에서 두 이름을 빼야 한다(CLAUDE.md 수정은 사용자 확인 뒤) |

## 5 SSOT·SOLID·FW 점검

| 항목 | 판단 |
|---|---|
| SSOT | 공격 모양은 `TargetingMethodData` 하나, 사거리는 `UnitData.AttackRange` 하나가 된다. 메인 공격 형태 기대값은 에셋 테스트 한 곳 |
| S | 유닛 정의는 유닛 값만, 지정 방식과의 결합 규칙은 `CombatAttackProfile`이 맡는다(이미 그 책임으로 문서화돼 있음) |
| O/L/I/D | 새 추상화 없음. 삭제와 검사 이전만 한다 |
| 과한 추상화 | 호환 래퍼·옛 생성자 overload를 남기지 않는다(호출자를 직접 고친다) |
| P1 | DB InGame Data는 계속 순수 C# |

## 6 Phase

각 Phase 끝에 MSBuild 컴파일(어떤 방식인지 기록)과 Unity EditMode(사용자 실행)를 구분해 기록하고 멈춘다. 테스트는 기존 `UnitSpecTestFactory`·`CombatCommandTestData`를 먼저 고쳐 개별 테스트 수정을 줄인다.

### Phase 1 — 새 검사 먼저 (동작 보존)

1. `CombatAttackProfile`에 ThornField–Single 검사(D2). 테스트: Single + ThornField 허용, Direction·All + ThornField 거부.
2. Category–충격 검사 `AttackProfileCompatibility.RequireSupportedImpact`를 만들고 `UnitSpec`·`UnitDefinition`에서 호출(D3). 아직 옛 검사도 같이 돈다. 테스트: 일반 Category + Cannon 충격 거부, Cannon + SingleCell 거부.
3. `DataBaseAssetTests` 메인 기대값에 사거리 1(D1).

### Phase 2 — Core에서 옛 필드 제거

1. `UnitSpec`·`UnitDefinition`: `AttackPatternSpec attackPattern`·`AttackTargetMode` 인자를 `int attackRange`로 교체, 옛 검사 호출 삭제.
2. `AttackPatternSpec.cs` 삭제, `AttackProfileCompatibility` 옛 overload 삭제.
3. `UnitCatalog`: 옛 호환 검사·전방형 1×1 검사·ThornField 옛 검사 삭제(Phase 1의 새 검사가 대신).
4. `UnitStatsSnapshot.AppointMethod` 제거 → `BattleCharacterCalculator`·`CharacterSettlementCalculator`·`SoulEffectCalculator`·`PositionTileModifierCalculator`·`CombatCommandEffects`·`CharacterCombatFactsComparer` 수정.
5. `BattleDefinitionAdapter`: `data.AttackRange`만 넘긴다(DB 필드는 아직 남아 있지만 읽지 않음).
6. `DevBattleInitializationSource`·`TemporaryCoreDatabase` 최소 수정(D4).
7. 테스트 헬퍼·테스트 약 40개 파일의 생성자 호출 정리(기계적 변경).

### Phase 3 — DB 필드·enum 제거와 인계

1. `UnitDataSO`·`UnitData`에서 두 필드와 `CreateAttackPattern`·옛 검사 삭제, 메인 공격 형태 검사 삭제(D1). 생성자 인자 제거 → `BattleDefinitionAdapterTests`·`InGameDataTableTests`·`TemporaryCoreDatabase` 수정.
2. `AppointMethod.cs`·`AttackTargetMode.cs` 삭제(D7). 남은 참조 0 확인.
3. 공유 메모: View 작업자(Dev fixture 수정 내역, DB 필드 제거, 인스펙터에서 두 항목이 사라짐), CLAUDE.md P4 수정 제안.
4. 지정 방식 구현 계획 §6과 owner inventory에 "옛 필드 제거 완료" 기록.

## 7 실행 기록

(Phase 진행 때 채운다.)

### 7.1 Phase 1 — 새 검사 먼저 (2026-10-06, 코드 완료, Unity EditMode 대기)

- D1~D7 권장안 승인. D2: 선인장 캐논(정사각형 5·칸 선택 1·정사각형 주변 스플래시 3)이 가시밭 특성의 실제 사용처이며 칸 선택이라 규칙과 맞다. 가시밭은 `PrimaryCell` 중심으로 생기므로 방향·전체 선택에서는 지금까지 효과가 조용히 사라지는 구조였다(이번 검사로 생성 시점에 거부).
- 수정:
  - `Contracts/Commands/AttackProfileCompatibility.cs`: `RequireSupportedImpact(CategoryKey, ImpactPatternSpec)` 추가(일반 Category = 단일 칸 충격, Cannon = 단일 칸 아닌 충격, 그 밖 Category = 미정의 거부).
  - `Contracts/Commands/UnitSpec.cs`, `Domain/Entities/UnitDefinition.cs`: 생성자에서 `RequireSupportedImpact` 호출. 옛 `GetSupportedAimKind` 호출은 Phase 2까지 함께 유지.
  - `Contracts/Combat/CombatAttackProfile.cs`: 가시밭 특성 + 칸 선택이 아닌 지정 방식 거부.
  - `Tests/EditMode/Framework/DataBaseAssetTests.cs`: 유닛별 승인 기대값에 사거리 추가(Akimbo 1, Demon 1, Sniper 4, Main 1, ShotGun 2). 메인 공격 형태 고정 규칙의 새 자리(D1).
- 새 테스트 `Tests/EditMode/AttackProfileRuleTests.cs`(10건): 일반 Category 3종 단일 칸만 허용, Cannon 3종 충격 허용·단일 칸 거부, 입력 누락 거부, 선인장 캐논 형태 + 가시밭 허용, 방향·전체 + 가시밭 거부.
- 컴파일 ①: 새 테스트 파일은 `CustomAfterMicrosoftCommonTargets`로 명시, `ProjectL.Tests.Core`·`ProjectL.Tests.Framework` MSBuild 빌드 오류 0.
- 보조 실행(NUnitLite, Unity 대체 아님): Core 테스트 1388건 중 1351 통과, 새 10건 통과. 실패 37건은 이전과 같은 NUnit 3.5/3.13 API 차이(`MissingMethodException`) 목록.
- **Unity EditMode(사용자 실행, 2026-10-06): `ProjectL.Tests.Core` 전체·`DataBaseAssetTests` 통과.** Phase 1 완료.

### 7.2 Phase 2 — Core에서 옛 필드 제거 (2026-10-06, 코드 완료, Unity EditMode 대기)

- Contracts:
  - `AttackPatternSpec.cs`(+`.meta`) 삭제.
  - `AttackProfileCompatibility`: 옛 overload `GetSupportedAimKind(CategoryKey, AttackPatternSpec, AttackTargetMode, ImpactPatternSpec)` 삭제. 새 지정 방식 overload와 `RequireSupportedImpact`만 남음.
  - `UnitSpec`: `AttackPatternSpec attackPattern, AttackTargetMode attackTargetMode` 인자를 `int attackRange`로 교체. `AttackPattern`·`AppointMethod`·`AppointRange`·`AttackTargetMode` 속성 제거, `AttackRange`는 저장 속성. 음수 사거리 거부.
  - `UnitStatsSnapshot`: `AppointMethod` 속성·인자·검증 제거(`AppointRange`와 별칭 `AttackRange`는 유지).
- Domain: `UnitDefinition`도 `int attackRange`로 교체(내부 검사 인자 이름 `appointRange` → `attackRange`). 메인 캐릭터 "1×1·사거리 1" 검사(`ValidateCommonValues`)는 옛 enum과 무관한 사거리 규칙이라 그대로 둔다.
- Application: `UnitCatalog`의 옛 호환 검사·옛 전방형 1×1 검사·`ValidateTraitCompatibility`(가시밭 옛 검사) 삭제, Cannon 충격 범위 1 이상만 남김. `BattleCharacterCalculator`(`definition.AttackRange`), `CharacterSettlementCalculator`·`SoulEffectCalculator`·`PositionTileModifierCalculator`·`CombatCommandEffects`의 스냅샷 생성 인자 정리.
- Core 밖 컴파일 연동: `BattleDefinitionAdapter.ReadUnit`(옛 `AppointMethod` switch 삭제, `data.AttackRange` 전달), `BufferSystems/Character/CharacterCombatFactsComparer`(`AppointMethod` 비교 삭제), `ViewSystem/Dev/DevBattleInitializationSource`(D4 최소 수정: 생성자 인자 `attackRange`).
- **계획과 다른 순서 1건:** `UnitData.ValidateAttackProfile`이 삭제한 `AttackPatternSpec`과 옛 overload를 써서, Phase 3 항목 중 이 검사 교체만 당겨왔다. 이제 `RequireSupportedImpact`(Category–충격)만 호출한다. `UnitData`·`UnitDataSO`의 `appointMethod`·`attackTargetMode` 필드, 메인 공격 형태 검사, `TemporaryCoreDatabase`·DB 테스트의 생성자 인자는 Phase 3 그대로.
- 테스트:
  - 헬퍼 `UnitSpecTestFactory`·`UnitStateTestFactory` 인자 교체, `CombatCommandTestData.Spec` 정리, 호출부의 `AttackPatternSpec.CreateX(n), AttackTargetMode.Y`를 `n`으로, `UnitStatsSnapshot` 생성 30곳의 `AppointMethod` 인자 제거(perl 일괄 치환 후 컴파일로 확인).
  - `AttackPatternSpecTests.cs`(+`.meta`) 삭제(삭제한 타입 전용 7건).
  - `UnitCombatStaticStatsTests` 재작성: 옛 별칭·null 패턴·`AttackTargetMode` 검증 5건 삭제, 음수 사거리 거부(UnitSpec·UnitDefinition)와 일반 Category + 영역 충격 거부 3건 추가.
  - `BattleEnvironmentPreparationTests`의 `AppointMethod` 비교 1줄 삭제.
- 작업 중 실수와 복구: 삭제 과정에서 `git rm --cached`로 두 파일 삭제를 인덱스에 올렸다가 `git reset HEAD -- <파일>`로 즉시 되돌렸다. 인덱스 변경 없음, 작업 트리 삭제만 남음.
- 컴파일 ①: Unity csproj에 남은 삭제 파일 두 개를 `CustomAfterMicrosoftCommonTargets`의 `Compile Remove`로 빼고 MSBuild. `ProjectL.Systems`·`ProjectL.Tests.Core`·`ProjectL.Tests.Framework`·`CombatViewBoundary`·`StageComposition`·`StageFoundation.PlayMode`·`Assembly-CSharp-Editor` 오류 0.
- 보조 실행(NUnitLite): Core 1379건 중 1342 통과. 실패 37건은 모두 NUnit 3.5/3.13 `MissingMethodException`(이전과 같은 37건). 총수 −9 = 삭제 12 − 추가 3.
- **Unity EditMode(사용자 실행, 2026-10-06): 전체 통과**(스모크 제외). Phase 2 완료.

### 7.3 Phase 3 — DB 필드·enum 제거와 인계 (2026-10-06, 코드 완료, Unity EditMode 대기)

- DB:
  - `UnitDataSO`: `appointMethod`·`attackTargetMode` 직렬화 필드와 `GetData` 인자 제거. 에셋 YAML의 남은 키는 손대지 않음(D6).
  - `UnitData`: 두 속성·생성자 인자·enum 검증 제거, 메인 캐릭터 옛 공격 형태 검사 삭제(D1, PlayerOnly·1×1·특성 없음 유지). XML doc을 "사거리 = `AttackRange`, 모양·선택·스플래시 = 지정 방식 테이블"로 갱신.
- enum 삭제(D7): `Contracts/Common/AppointMethod.cs`·`AttackTargetMode.cs`(+`.meta`). 작업 트리에서만 삭제했고 git 인덱스는 건드리지 않았다.
- 생성자 호출 정리: `Factory/Runtime/Development/TemporaryCoreDatabase`, 테스트 `CharacterRosterDatabase`·`BattleDefinitionAdapterTests`(2)·`TargetingMethodDataTests`.
- `TargetingMethodContractTests.SerializedMethods_KeepNewAndLegacyMeaningsSeparate`에서 옛 enum 값 단언 2줄을 지우고 이름을 `SerializedMethods_KeepStableValues`로 바꿨다.
- 남은 참조: `AppointMethod`·`AttackTargetMode`·`AttackPatternSpec` 코드 참조 0(`AttackFailureReason.NotSupportAppointMethod`는 이름만 같은 별개 값, §3 후속 후보).
- 인계:
  - [View·DB 작업자 공유 메모](../handoffs/2026-10-06-legacy-attack-fields-removal-view-share.md)(미전달): 인스펙터 항목 제거, 생성자 변경, Dev fixture 최소 수정 확인 요청, DB-SO 설계 문서의 공용 타입·필드 표 정리 요청.
  - 지정 방식 구현 계획 §6과 owner inventory 끝에 완료 기록 추가.
  - CLAUDE.md DB 규칙 P4: 공용 타입 목록에서 두 enum을 빼고 지정 방식·사거리의 원본을 적었다(사용자 확인 뒤, D7).
- 컴파일 ①: Unity csproj에 남은 삭제 파일을 `Compile Remove`로 빼고 MSBuild. `ProjectL.Tests.Core`·`Framework`·`CombatViewBoundary`·`StageComposition`·`StageFoundation.PlayMode`·`Assembly-CSharp`·`Assembly-CSharp-Editor` 오류 0.
- 보조 실행(NUnitLite): Core 1379건 중 1342 통과, 실패 37건은 모두 NUnit 3.5/3.13 `MissingMethodException`(Phase 2와 동일).
- **Unity EditMode(사용자 실행, 2026-10-06): 전체 통과**(스모크 제외). Phase 3 완료.
