# 서브 유닛 레벨 특성 성장 — Task 계획

> **과거 계획 — 현재 계약으로 대체됨(2026-10-29 정정).** 이 문서의 서브 전용·고정 획득/강화 칸은 당시 구현 이력이다. 현재는 메인·서브 모두 설정된 레벨과 후보 풀에 따라 특성 획득·강화를 지원한다. 후속 작업은 [병과별 성장 계획](2026-10-27-category-trait-progression-implementation-plan.md)과 [현재 Core 인계](../handoffs/2026-10-27-category-trait-progression-handoff.md)를 따른다. 최신 콘텐츠 상태는 [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md)를 따른다.

Date: 2026-10-27 (3판: 판매 후보 분리·저장 직전 Run 종료 재검사·Apply 책임 반영, §10)
Design: [sub-trait-growth-design](../specs/2026-10-27-sub-trait-growth-design.md)
Status: 계획 3판. 2026-10-27 사용자 지시로 **G-1~G-3 Core 코드·시험 코드 작성, G-4 문서 정리 완료**(§8.1~§8.4). **Unity 컴파일·시험·재생 확인 대기.** G1.18·G1.19와 화면은 [View·기획 최종 인계](../handoffs/2026-10-27-sub-trait-growth-core-handoff.md). 연결 설계 D3의 `Apply` 책임은 이 계획 TP-17로 구체화한다.
Source baseline: 특성 성장 착수는 `feature/Cheon/Core/NewCombatSytem`, HEAD `5a1bfbe1`(1판 `4640511d`). 후속 병합으로 현재 HEAD는 `928362f7c`, 특성 성장 변경은 미커밋으로 보존(§8.7).

경로 약어: `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `B` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`, `CS` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem`, `DB` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table`, `T` = `Assets/Tests/EditMode`.

## 0. 결정과 착수 조건

### 0.1 설계 결정(2026-10-27 사용자 "Q-T1~Q-T8 모두 권장안")

| ID | 결정 |
|---|---|
| Q-T1 A | 획득 = 특성 하나 추가, 강화 = 가진 특성 하나를 강화형으로 교체. 종점 = 특성 2개·강화 1개. |
| Q-T2 A | 강화 상점도 같은 두 칸에 쓴다(레벨 조건 없음, 종점 공유). |
| Q-T3 A | 레벨 SO는 전역 하나(카테고리 설정 SO). |
| Q-T4 A | SO 칸 둘(획득 레벨·강화 레벨). 종점 상수(2·1)는 코드 규칙. |
| Q-T5 A | 열린 종류의 후보 전부 중 플레이어가 하나를 고른다. 무작위 제시·저장 없음. |
| Q-T6 A | 비용 없음. |
| Q-T7 A | 고르면 확정(되돌리기·다시 고르기 없음). |
| Q-T8 A | 구조를 먼저 넣는다. 후보가 없는 유닛은 칸만 열리고 고를 것이 없다. |

### 0.2 계획에서 정한 것(설계에 없거나 설계를 좁힌 것 — 확인 상태는 머리말 참고)

| ID | 정한 것 | 근거 |
|---|---|---|
| TP-1 | `LevelProgressionSpec`의 5인자 생성자에 **끝 선택 인자 둘** `subTraitAcquireLevel = 0`, `subTraitEnhanceLevel = 0`. 각 값은 0 또는 2 이상. 6인자(이전 호출자 호환) 생성자는 0으로 넘긴다. | 생성자 호출 시험 24곳(Core 쪽 18·Framework 6)이 그대로 컴파일된다. 기존 칸과 같은 범위 검사 위치. |
| TP-2 | `OwnedCharacterRecord`의 끝 선택 인자 `enhancement`(`OwnedEnhancement`)를 **`growth`(`OwnedTraitGrowth`, null = `Empty`)로 바꾼다.** 속성 `Enhancement` → `Growth`, `SameEnhancement` → `SameGrowth`. 호환 속성은 두지 않는다. | 설계 D1. 위치 인자로 레벨·경험치까지만 넘기는 호출처는 그대로다. |
| TP-3 | 칸 계약 `OwnedTraitGrowth`의 생성자 검사: 두 칸이 같은 키면 예외. 메인 기록이 빈 칸이 아니면 **기록 생성자**가 예외(D2). 칸에 어떤 정의가 들어갈 수 있는지는 계약이 모른다(정의를 모르므로). | 계약이 지킬 수 있는 것만 계약에 둔다. |
| TP-4 | `EnhancementChange` → **`TraitGrowthChange`**(`git mv`, GUID 유지). 검사: 식별·레벨·경험치 같음, Expected의 칸 중 정확히 하나가 비었다가 Final에서 채워지고 다른 칸은 같음. 채워진 칸을 `Kind`(`TraitGrowthKind`)·`Added`(`OwnedEnhancement`)로 낸다. | 상점 거래(`ShopTransactions`)가 정의 없이 `change.Added.SameValues(offer.Enhancement)`로 대조할 수 있다. |
| TP-5 | Buffer: `PreparedEnhancementChange` → `PreparedTraitGrowthChange`(`git mv`, GUID 유지), store의 `PrepareEnhancement`/`InstallEnhancement` → `PrepareGrowth`/`InstallGrowth`. 공개 쓰기 `ICharacterBufferWriteSink.ApplyGrowth(TraitGrowthChange)`(준비 + 설치, `Acquire`와 같은 꼴)를 더한다. | 선택 명령은 상점 거래 없이 로스터만 바꾼다. 이 인터페이스를 구현하는 시험 가짜는 없다(확인함). |
| TP-6 | 선택 판단은 순수 함수 `TraitGrowthRules.PlanChoice`. 서비스는 Buffer·DB 읽기, 판정 호출, `ApplyGrowth`. **서비스가 직접 맡는 것(전투 중·Run 끝남·원본 바뀜 거절, 실제 저장·revision 갱신)은 CoreIntegration 시험으로 덮는다(G3.5).** | 2판 고침(검토 5). 순수 판정만 시험하면 서비스 책임이 빈다. CoreIntegration은 `ProjectL.Systems` 내부에 접근할 수 있고(`Systems/AssemblyInfo.cs:4`), 로스터를 실제 조립으로 만드는 선례가 있다(`T/CoreIntegration/BossStagePreparationTests.cs:31-40`). |
| TP-7 | 상점 실패 이유 `AlreadyEnhanced`는 **이름을 그대로 두고** 뜻을 "그 칸이 이미 참"으로 넓힌다(XML doc). | View `ShopResultPolicy.cs:36`이 이 값을 쓴다. |
| TP-8 | 정의 모양 검사는 DB `EnhancementData.Validate`(부트 DB 생성)에서 실패시킨다. 지금 에셋 넷은 모두 맞는다. | 설계 D4. |
| TP-9 | **`ProjectL.Tests.Framework`는 고치지 않는다.** 착수 HEAD `5a1bfbe1`에는 삭제 예정 시험 479개가 있었다. 후속 §8.5의 단일 파일 예외는 §8.6에서 원복했고, §8.7 병합으로 전체 삭제를 인수했다. Core 쪽 통합 시험(서비스·Buffer 연결)은 Framework 대신 **CoreIntegration**에 둔다(2026-10-27 사용자) — G3.5가 이것이다. | Unity는 어느 어셈블리든 컴파일 오류가 있으면 Test Runner를 돌리지 않는다. 삭제 선행조건은 §8.7에서 해소했지만 Unity 컴파일·시험 통과는 여전히 미확인이다. |
| TP-10 | **합성기는 정의 모양을 보지 않는다.** `OwnedUnitSpecResolver.Resolve`는 칸 순서(획득 → 강화)대로 각 정의의 교체 → 추가를 지금 본문 그대로 적용한다. 모양 판단(`KindOf`)은 성장 규칙만 쓴다. | 2판 고침(검토 1). 합성(정의 만들기)과 성장 규칙(무엇을 고를 수 있나)은 바뀌는 이유가 다르다. 이렇게 하면 합성기 전환이 G-1 안에서 끝나고, 모양이 둘 다 찬 시험 정의(`EnhancementActionCostTests:162-164` PrecisionShot)도 그대로 합성된다. |
| TP-11 | **G-1은 동작 보존 단계다.** 상점은 "칸 중 하나라도 차면 `AlreadyEnhanced`"(지금의 한 유닛 한 강화)를 유지하고, 결과 칸만 정의 모양으로 고른다. 칸별 판정·종점·획득 특성 강화는 G-2에서 공통 후보 판정으로 바꾼다. | 2판 고침(검토 1·2). G-1이 끝나면 컴파일되고 지금과 같은 화면·결과여야 Unity 확인이 의미 있다. |
| TP-12 | **정의 검사와 적용 가능 검사를 나눈다.** 강화 스테이지 진입 때 `EnhancementShopDefinitionAdapter`는 정의 자체만 본다(`TraitGrowthRules.ValidateDefinition`: 모양, 대상 유닛, 획득이면 기본 정의에 더해 합성 가능, 강화면 바꿀 특성이 기본 특성이거나 같은 유닛의 획득 정의가 더하는 특성이고, 그 경로로 합성 가능). 보유 개체에 지금 붙일 수 있는지는 후보 판정(`Candidates`)이 상품 만들기·구매 재검증 모두에서 본다. | 2판 고침(검토 3). 지금 어댑터는 성장 없는 임시 기록에 정의를 단독 적용해, 획득한 특성의 강화형을 등록하면 진입에서 예외가 난다. |
| TP-13 | **로스터 서비스 준비 조건을 나눈다.** 기존 `RequireReady`(획득·초기 드로우)는 지금 조건 그대로. 성장 두 멤버만 `RequireGrowthReady`(카테고리 설정 필요, 강화 표는 선택 — 없으면 후보 0). `InitData`는 새 의존이 없어도 예외를 내지 않는다. | 2판 고침(검토 총평 LSP·ISP). 성장용 DB 의존 때문에 기존 획득 기능이 실패하면 안 된다. |
| TP-14 | **시험 데이터 바꿈과 기대 동작 바꿈을 나눠 적는다.** 데이터 바꿈 = 검증 의도는 그대로 두고 새 규칙에서 유효한 입력으로(예: 빈 정의 → 획득 정의 하나). 기대 동작 바꿈 = 새 동작에 맞춘 새 사례(G-2·G-3). 각 시험 Task 표에 둘을 따로 둔다. | 2판 고침(검토 4). `EnhancementShopRuleTests.Definition()`(`T/EnhancementShopRuleTests.cs:16-18`)은 Added·Replaced가 모두 빈 정의라 새 모양 규칙에서 성장 불가다. |
| TP-15 | **합성용 정의 목록과 상점 판매 허용 키를 분리한다.** `Candidates`에는 현재 특성을 합성할 정의를 함께 넘기되, 상점은 그 결과를 별도로 전달받은 `detail.CandidateKeys`로 제한한 뒤 가중 추첨한다. 구매 재검증도 판매 허용 키 포함 여부와 공통 후보 조건을 모두 확인한다. | 합성 때문에 읽은 다른 보유 개체의 성장 정의나 정의 검사용 선행 획득 정의가 판매 목록을 넓히면 안 된다. 판매 허용 범위의 원본은 스테이지 설정이다. |
| TP-16 | **선택 명령은 저장 직전에 Run 종료 여부를 다시 확인한다.** 모든 정의 조회·판정 뒤 Run ID·로스터 revision을 대조하고, 같더라도 `Outcome != InProgress`면 `RunEnded`로 거절한다. 그 뒤 외부 조회 없이 `ApplyGrowth`한다. | Run 종료는 Run ID·로스터 revision을 바꾸지 않을 수 있다. 기존 `CharacterRosterService.Publish`도 저장 직전에 `RunEnded`를 재검사한다. |
| TP-17 | **`Apply(record, definition)`는 후보 검증을 마친 뒤 칸을 채우는 Application 내부 도우미다.** `PlanChoice`와 `EnhancementPurchaseRules`만 각 경로의 판정을 통과한 같은 기록·정의로 호출한다. 모양·대상 일치·빈 칸 등 전달받은 값으로 확인할 수 있는 불변식을 지키며, 식별·레벨·경험치·다른 칸을 보존한다. 특성 수·중복·교체 대상·보류 특성 판정은 `Candidates`가, 레벨 조건은 `PlanChoice`가 맡는다. | 기본 정의·기존 성장 정의가 없는 `Apply`에서 종점과 적용 가능 조건을 다시 계산하지 않는다. 공개 선택·구매 판정은 검증 없이 이 도우미를 호출할 수 없으며, Buffer는 저장 불변식만 맡는다. |

### 0.3 계획 중 새로 확인한 사실

- **지금 강화 후보는 0개다.** 독 가시(CactusCannon)가 바꾸는 가시밭(`ThornField`)은 보류 특성이다(`C/Application/Combat/DeferredCombatFeatures.cs:17`). 상점 후보 필터와 같은 함수를 쓰므로 성장 후보에서도 빠진다. 지금 고를 수 있는 것은 **획득만**(ShotGun 2연발·도어 브리칭, DynamiteHammer 연쇄 폭발)이다.
- 전투 준비는 강화 표 전체를 정의에 넣는다(`CS/CombatSystem/DatabaseBattleInitializationSource.cs:44-45`). 합성기만 칸 둘을 읽으면 전투 쪽 입력 조립은 그대로다.
- 로스터 서비스는 강화 표를 `IHasEnhancementTable`(선택)로 얻는다(`CS/CharacterSystem/CharacterStatsQueryService.cs:108`과 같은 방식).
- 선택 거절 "전투 중"은 `ICharacterBufferReadSink.Characters.Count != 0`(전투 사실)로 판단한다. Buffer 준비도 같은 조건에서 예외를 던진다(`B/Character/CharacterBuffer.cs:142`).
- 상점 진입은 상품 계산에 기본 `UnitSpec`을 넘기지 않고(`CS/ShopSystem/ShopService.cs:122-126`), 구매는 구매할 정의 하나만 넘긴다(`ShopService.cs:203-212`). G-2가 이 입력을 늘린다(검토 2).
- View·UI·LifeCycle 코드는 `record.Enhancement`를 읽지 않는다(확인함). 보유 기록을 읽는 곳은 모두 Core·Buffer·상점이다.
- 빈 정의(Added·Replaced 모두 빔)를 쓰는 Core 시험: `EnhancementShopRuleTests.cs:16-18`, `OwnedEnhancementResolutionTests.cs:109-110`. 두 모양이 모두 찬 정의: `EnhancementActionCostTests.cs:162-164`(PrecisionShot). TP-10으로 합성 시험은 그대로 통과하고, 모양 규칙을 쓰는 상점 시험만 데이터를 바꾼다.

### 0.4 기준 판·착수 조건

- HEAD `5a1bfbe1`. 1판 기준 `4640511d` 뒤 바뀐 파일 중 이 계획이 고치는 것은 `B/Character/CharacterBuffer.cs`·`C/Application/Combat/BattlePreparationCalculator.cs`·`C/Application/Combat/CombatCommandCalculator.cs`다. 강화 관련 줄은 그대로이고 `CombatCommandCalculator`의 판정 줄만 `:221` → `:224`로 옮겨졌다. 유닛 에셋 둘(`Unit_Cowboy`·`Unit_ShotGun`)이 바뀌었지만 서브 여덟의 기본 특성 수는 여전히 1개다.
- **Framework 시험 삭제가 이 브랜치에 들어와 있어야 G-1 Unity 확인이 된다(TP-9).** 들어오기 전에 코드는 쓸 수 있지만, 확인은 삭제를 병합한 뒤로 미룬다.
- 작업 트리에는 이 계획과 무관한 문서 둘(`2026-10-27-player-turn-auto-end-design.md`·`-plan.md`, 커밋 전)이 있다. 고치는 코드 파일이 겹치는지는 그 계획을 착수할 때 대조한다(지금 코드 변경 없음).
- 스테이지 기록 설계(같은 날)와는 고치는 파일이 겹치지 않는다.

## 1. Phase G-1 계약·합성 전환 (동작 보존, 컴파일 가능한 단위)

목표: 보유 기록의 칸을 바꾸고, 그것을 읽는 모든 곳(합성기 포함)을 한 번에 옮긴다. 상점은 지금 규칙(한 유닛 한 강화)을 유지한다(TP-11). SO 레벨 기본 0이라 아무 칸도 열리지 않는다. **G-1이 끝나면 컴파일되고 화면·결과가 지금과 같다.**

| Task | 파일 | 내용 |
|---|---|---|
| G1.1 | `C/Contracts/Enhancements/TraitGrowthKind.cs`(새, + `.meta`) | `None = 0, Acquire = 1, Enhance = 2`. |
| G1.2 | `C/Contracts/Enhancements/OwnedTraitGrowth.cs`(새, + `.meta`) | `Acquired`·`Enhanced`, `Empty`, `IsEmpty`, `Get(kind)`, `With(kind, value)`(빈 칸만 채움), `SameValues`, `Keys`(찬 칸 키, 획득 → 강화 순서). TP-3. |
| G1.3 | `C/Contracts/Roster/OwnedCharacterRecord.cs` | TP-2·TP-3. |
| G1.4 | `C/Contracts/Roster/OwnedCharacterChange.cs` | `SameGrowth`, 문구 "경험치 변경은 특성 성장을 바꿀 수 없습니다". |
| G1.5 | `C/Contracts/Enhancements/EnhancementChange.cs` → `TraitGrowthChange.cs` | TP-4. |
| G1.6 | `C/Contracts/Shop/ShopPurchaseDecision.cs` | `EnhancementChange` → `GrowthChange`(`TraitGrowthChange`). |
| G1.7 | `C/Contracts/Commands/LevelProgressionSpec.cs` | TP-1. |
| G1.8 | `C/Application/Roster/TraitGrowthRules.cs`(새, + `.meta`) | **G-1 범위는 모양 규칙만:** `MaximumTraits = 2`, `KindOf(EnhancementDefinition)`, DB용 `KindOf(addedCount, replacedCount)`. 나머지 함수는 G-2. |
| G1.9 | `C/Application/Enhancements/OwnedUnitSpecResolver.cs` | **(1판 G2.1을 여기로, 검토 1)** `Resolve`가 `owned.Growth.Keys`의 정의를 칸 순서대로 적용(TP-10). 정의를 못 찾거나 중복·대상 불일치·레벨 불일치는 지금과 같은 예외. 칸이 비면 기본 정의 그대로. `BuildProfiles` 무변경. |
| G1.10 | `C/Application/Roster/ExperienceRules.cs:56` | 새 기록에 `record.Growth`. |
| G1.11 | `C/Application/Shop/EnhancementPurchaseRules.cs` | TP-11: `!target.Growth.IsEmpty` → `AlreadyEnhanced`. 결과 기록은 `target.Growth.With(TraitGrowthRules.KindOf(definition), offer.Enhancement)`. 모양이 성장 불가면 `InvalidEnhancement`. |
| G1.12 | `C/Application/Shop/EnhancementOfferPlanner.cs:27` | TP-11: `o.Growth.IsEmpty`. |
| G1.13 | `C/Application/Combat/BattlePreparationCalculator.cs:77`·`C/Application/Combat/CombatCommandCalculator.cs:224`·`C/Application/Roster/OwnedCharacterStatsCalculator.cs:80` | `!record.Growth.IsEmpty`. 문구 "강화된 개체" → "특성이 성장한 개체". |
| G1.14 | `CS/CharacterSystem/CharacterStatsQueryService.cs:106-111` | 정의 키를 `o.Growth.Keys`에서 모음. |
| G1.15 | `CS/ShopSystem/EnhancementShopDefinitionAdapter.cs:30-31` | 임시 기록을 `growth: Empty.With(KindOf(row), …)`로(검사 동작은 G-1 그대로, 나누는 것은 G2.4). |
| G1.16 | `B/Character/PreparedEnhancementChange.cs` → `PreparedTraitGrowthChange.cs`, `B/Character/CharacterBuffer.cs` | TP-5. |
| G1.17 | `B/Core/ShopTransactions.cs:120-137` | `decision.GrowthChange`, `change.Added.SameValues(offer.Enhancement)`, `PrepareGrowth`/`InstallGrowth`. |
| G1.18 | `DB/Category/Item/CategoryConfigDataSO.cs` | `LevelProgressionInput`에 칸 둘(`Min(0)`, 툴팁 "0 = 없음, 2 이상"). "전부 0이면 꺼짐" 판정에는 넣지 않는다. |
| G1.19 | `DB/Enhancement/Item/EnhancementData.cs` | `Validate`에 모양 검사(TP-8, `KindOf(addedCount, replacedCount)` 호출). |
| G1.20 | 시험 맞춤(Core 두 어셈블리) | 아래 표. |
| G1.21 | (없음, TP-9) | Framework 시험은 고치지 않는다. 착수 때 삭제가 들어왔는지만 확인한다. |
| G1.22 | 새 시험(Core) `T/OwnedTraitGrowthTests.cs`·`T/TraitGrowthChangeTests.cs`(+ `.meta`) | §5.1. |

G1.20 Core 시험 맞춤(TP-14 구분):

| 파일 | 호출만 | 데이터 바꿈(의도 유지) | 기대 동작 바꿈 |
|---|---|---|---|
| `T/OwnedEnhancementStateTests.cs` | `enhancement:` → `growth:`, `Enhancement` → `Growth.Get(…)` | — | — |
| `T/OwnedEnhancementResolutionTests.cs` | 같음(빈 정의 `:109`는 합성기가 모양을 안 봐 그대로, TP-10) | — | — |
| `T/EnhancementActionCostTests.cs`·`T/EnhancementFollowUpPreparationTests.cs` | `Input(enhanced, recipe)` 도우미가 칸에 넣도록 | — | — |
| `T/CoreIntegration/ImpactBoltAuthoringTests.cs:213-216` | 같음 | — | — |
| `T/EnhancementShopRuleTests.cs` | `Owned(…, enhancement)` 도우미 | `Definition()`을 획득 정의(더하는 특성 하나)로. 구매·후보 개수 단언은 그대로 | — (G-1은 동작 보존) |
| `T/ShopPurchaseRuleTests.cs`·`T/ShopOfferPlannerTests.cs` | `EnhancementChange` 이름·속성 | 빈 정의를 쓰면 위와 같이 | — |

변경 영향(G-1):

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 보유 기록 칸·합성기 | 경험치 정산(기록 재생성), 전투 준비·공격 프로필·능력치 조회 합성, 상점 구매·후보 | `ExperienceRulesTests`, `CombatRewardSettlementTests`·`CombatKillRewardTests`, G1.20 표 전부, `OwnedCharacterStatsCalculatorTests`, 강화 특성 시험(`EnhancementDoubleShotTests` 등) | 전투 명령 계산 본문, 사건 형식, View |
| `LevelProgressionSpec`·SO 칸 | DB SO 읽기 | `ExperienceRulesTests` | 경험치 공식(값 0이면 무관) |

## 2. Phase G-2 공통 후보 판정·상점 전환 (동작 변화: 상점이 칸별로 팜, 획득 특성의 강화 가능)

목표: "이 보유 개체에 이 정의를 붙일 수 있나"를 한 함수로 만들고, 상점 상품 만들기·구매 재검증·(G-3) 선택 명령이 모두 그것을 부른다(검토 2). 정의 자체 검사는 따로 둔다(검토 3).

| Task | 파일 | 내용 |
|---|---|---|
| G2.1 | `C/Application/Roster/TraitGrowthRules.cs` | `CurrentTraits(record, basis, definitions)` = `OwnedUnitSpecResolver.Resolve` 결과 특성 키(합성 원본 하나). `Candidates(record, kind, basis, definitions)`: 그 유닛의 그 종류 정의 중 보류 특성 아님(`DeferredCombatFeatures.IsTraitAvailable`, 상점과 같은 함수) && 그 칸 빔 && 획득 — 더할 키가 지금 특성에 없고 결과 수 ≤ `MaximumTraits` / 강화 — 바꿀 키가 지금 특성에 있음. 레벨·상점 판매 허용 범위는 보지 않는다(성장 공용). `IsUnlocked(record, kind, spec)`: 서브 && 진행 켜짐 && 레벨 ≥ 그 종류 레벨(0 아님) && 칸 빔. `internal Apply(record, definition)`: 검증된 후보의 칸만 채운 새 기록(TP-17); 종점·적용 가능 조건을 재계산하지 않는다. `PlanChoice(record, key, basis, definitions, spec)`는 대상·열림·후보를 검사한 뒤에만 `Apply`를 호출해 `TraitGrowthDecision`을 반환한다. `ValidateDefinition(definition, basis, sameUnitDefinitions)`(TP-12). |
| G2.2 | `C/Contracts/Enhancements/TraitGrowthDecision.cs`·`TraitGrowthFailureReason.cs`(새, + `.meta`) | 실패 이유 `NotSub`·`NotUnlocked`·`NotCandidate`·`UnknownDefinition`, 서비스 쪽 `NotOwned`·`InBattle`·`RunEnded`·`SourceChanged`. |
| G2.3 | `C/Application/Shop/EnhancementOfferPlanner.cs` | 입력에 보유 정의 키별 기본 `UnitSpec` 표와 **별도 판매 허용 키 목록**(`detail.CandidateKeys`)을 더한다. 정의 목록은 판매 정의와 합성에 필요한 기존 성장 정의를 키별로 중복 없이 포함한다. 후보 풀 = 각 보유 개체마다 두 종류의 `Candidates` 합 중 판매 허용 키에 포함된 것만. 이 제한 뒤 가중 추첨하며 기존 키 정렬·가중치·개수 규칙을 유지한다(TP-15). |
| G2.4 | `C/Application/Shop/EnhancementPurchaseRules.cs` | 입력에 대상의 기본 `UnitSpec`과 그 유닛의 정의 목록(대상 칸에 찬 정의 포함), 별도 판매 허용 키 목록을 더한다. 판매 허용 키 밖이면 `InvalidEnhancement`. "칸 하나라도 참" 거절 → 판매 정의 종류의 칸이 차면 `AlreadyEnhanced`(TP-7), 그 밖에 `Candidates`에 없으면 `InvalidEnhancement`. 기존 상품·가격·잔액 검사와 이 판정을 통과한 같은 기록·정의로만 내부 `Apply`를 호출한다(TP-17). 상점은 레벨 조건을 검사하지 않는다. |
| G2.5 | `CS/ShopSystem/ShopService.cs:122-126`(진입)·`:199-212`(구매) | 진입: 보유 정의 키마다 `UnitSpecAdapter.ReadUnit(_units.GetUnit(key))`, 스테이지 판매 정의와 **보유 개체의 이미 찬 칸 정의를 표에서 읽어** 합성용 정의 목록을 만든다(키별 중복 제거). 판매 허용 범위는 `detail.CandidateKeys`를 별도 인자로 G2.3에 전달하며 추가 조회로 넓히지 않는다. 구매: 대상 기본 정의 + 대상 칸 정의 + 판매 정의와 해당 스테이지의 판매 허용 키를 G2.4에 넘긴다. 읽은 뒤 `RevalidatePurchase`·원본 대조는 지금 자리 그대로. |
| G2.6 | `CS/ShopSystem/EnhancementShopDefinitionAdapter.cs` | TP-12: 단독 적용 대신 `ValidateDefinition`. 강화 정의가 획득 특성을 바꾸면 같은 유닛 획득 정의를 강화 표(`GetAll`)에서 찾아 그 경로로 합성 확인. 찾지 못하면 지금처럼 진입 예외(저작 오류). |
| G2.7 | 시험 | 아래 표. |

G2.7 시험(TP-14 구분):

| 파일 | 호출만 | 데이터 바꿈 | 기대 동작 바꿈(새 사례) |
|---|---|---|---|
| `T/EnhancementShopRuleTests.cs` | 기본 정의 인자를 넘기는 도우미 | — | 획득만 찬 개체에 강화 상품이 나옴·구매 성공, 강화 칸이 비어도 바꿀 특성이 없으면 상품 없음·구매 `InvalidEnhancement`, 같은 종류 칸이 차면 `AlreadyEnhanced`, 결과 특성 3개가 되는 획득은 없음 |
| `T/ShopPurchaseRuleTests.cs`·`T/ShopOfferPlannerTests.cs` | 같음 | — | — |
| 새 `T/TraitGrowthRulesTests.cs`(+ `.meta`) | — | — | §5.2 |

G2.7 추가 회귀(TP-15·TP-17):

- ShotGun A는 2연발 획득, B는 미성장이고 스테이지 판매 허용 키는 도어 브리칭뿐인 입력: A의 합성용 2연발 정의를 읽어도 B에게 2연발 상품이 나오지 않는다. 합성용 정의가 추가돼도 허용 상품의 순서·가중 추첨 결과는 같은 seed에서 유지된다.
- 판매 허용 키가 비었으면 합성용 정의가 있어도 상품 0개이고 RNG를 소비하지 않는다. 판매 허용 키 밖 상품을 구매 판정에 직접 넘겨도 `InvalidEnhancement`, 성장 변경·결제 결정 없음.
- `PlanChoice`와 상점 구매 양쪽에서 결과 특성 3개·중복 획득·교체 대상 없음인 선택은 실패하고 성장 변경을 만들지 않는다. 성공 사례는 식별·레벨·경험치·다른 칸을 보존한다. 내부 `Apply`를 독립적인 후보 검증 API로 시험하지 않는다.

변경 영향(G-2):

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 후보 판정·상점 입력 | 강화 스테이지 진입·구매·재검증 | G2.7 표 | 일반 상점(유닛·아이템) 경로, 전투, 경험치 |
| 정의 검사 | 강화 스테이지 진입 | `EnhancementShopRuleTests`, DB 에셋 시험(Unity) | 합성기 본문 |

지금 에셋으로는 상점 결과가 G-1과 같다(유닛당 획득 후보만 있고 강화 후보 0, 기본 특성 1개라 종점에 닿지 않음). 차이는 같은 유닛이 강화 상점을 다시 만났을 때 다른 종류의 칸을 살 수 있는 것인데, 지금 강화 후보가 0개라 화면에 나오지 않는다.

## 3. Phase G-3 선택 서비스 (동작 변화: 선택 명령이 생김, SO 레벨 0이면 늘 `NotUnlocked`)

| Task | 파일 | 내용 |
|---|---|---|
| G3.1 | `C/Contracts/Roster/TraitGrowthChoices.cs`·`TraitGrowthResult.cs`(새, + `.meta`) | 조회: 보유 ID, 열린 종류마다 후보 행(정의 키·이름·설명·종류·더하거나 바꾸는 특성 키). 결과: 성공·실패 이유·최종 기록. |
| G3.2 | `CS/CharacterSystem/CharacterRosterService.cs` `InitData` | `CategoryConfigTableQuery`·`IHasEnhancementTable`를 **있으면** 받는다. 없어도 예외 없음(TP-13). |
| G3.3 | 같은 파일 `ChooseTraitGrowth(CharacterInstanceId, EnhancementKey)` | `RequireGrowthReady` → Run 끝남(`RunEnded`)·전투 중(`InBattle`) 거절 → Run ID·로스터 revision 기억 → 기록(`NotOwned`)·기본 정의·그 유닛의 정의 읽기 → `PlanChoice` → revision·Run ID 다시 확인(다르면 `SourceChanged`, 쓰지 않음) → **Run 종료 여부 재검사**(같은 ID·revision이어도 종료됐으면 `RunEnded`, 쓰지 않음) → 성공한 결정만 `CharacterWriteSink.ApplyGrowth`. 최종 재검사 뒤 저장 사이에는 외부 정의 조회를 하지 않는다(TP-16). |
| G3.4 | 같은 파일 `GetTraitGrowthChoices(CharacterInstanceId)` | 같은 읽기로 열린 종류마다 `Candidates`. 전투 중에도 조회는 된다. |
| G3.5 | 새 시험(CoreIntegration) `T/CoreIntegration/TraitGrowthChoiceServiceTests.cs`(+ `.meta`) | **(검토 5)** 실제 조립(`BufferSystemFactory` + Core 서비스, `BossStagePreparationTests` 선례)으로: ① 성공 — 칸 채워짐·로스터 revision 1 증가·능력치 조회 특성 목록에 반영 ② 같은 칸 두 번째 선택 `NotUnlocked`·무변경 ③ 전투 중 `InBattle`·무변경 ④ Run 끝남 `RunEnded`·무변경 ⑤ 읽는 도중 로스터가 바뀌면 `SourceChanged`·무변경(시험 DB의 강화 표 읽기에 획득 한 번을 끼우는 고리) ⑥ 메인 `NotSub` ⑦ 강화 표 없는 DB에서 기존 `InitializeRoster`·`AcquireSub`가 그대로 성공(TP-13) ⑧ **정의 조회 도중 같은 Run이 종료됨** — Run ID·로스터 revision이 유지된 상태에서도 `RunEnded`, 성장 기록·revision 무변경(TP-16). ⑤의 무변경은 고리에서 수행한 획득 이후 추가 성장 쓰기가 없음을 뜻하고, ⑧은 Run 종료 사실을 유지하되 성장 쓰기가 없음을 뜻한다. |
| G3.6 | `T/CoreIntegration/CoreIntegrationTestData.cs` | 강화 표(`IHasEnhancementTable`)·카테고리 설정 레벨 값을 넣는 도우미, ⑤·⑧용 읽기 고리. ⑧은 새 Run 생성·로스터 변경 없이 현재 Run 종료만 유발한다. 기존 시험이 쓰는 기본값은 그대로(강화 표 없음). |

변경 영향(G-3):

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 로스터 서비스 멤버 둘·준비 조건 | Buffer `ApplyGrowth`, 기존 획득·초기 드로우 | G3.5, `CharacterRosterContractTests`, CoreIntegration 전체(공용 시험 DB를 고침) | 상점·전투 경로(본문 무변경) |

## 4. Phase G-4 마무리

| Task | 내용 |
|---|---|
| G4.1 | View 인계 텍스트(채팅 붙여넣기용): 조회·명령 하나씩, 실패 이유, 결과 화면 뒤 StageSelect에서 열림 표시, 유닛 정보 창은 능력치 조회 특성 목록이 이미 성장 반영, `AlreadyEnhanced` 뜻 넓힘(TP-7), 강화 상점이 같은 유닛에 다른 종류 칸을 팔 수 있음, 지금 강화 후보 0개. |
| G4.2 | 기획 알림 텍스트: SO 칸 위치(카테고리 설정 → Level Progression), 후보 저작 모양(Added 1 또는 Replaced 1), 획득한 특성의 강화형은 같은 유닛의 그 획득 정의가 있어야 함(TP-12), 지금 후보 현황. |
| G4.3 | 설계 문서 구현 메모(계획과 다른 점), 이 계획 실행 기록. |

## 5. 새 시험 사례(Core)

### 5.1 G-1

`OwnedTraitGrowthTests`: 빈 칸, 칸 채우기, 찬 칸에 채우면 예외, 두 칸 같은 키 예외, `Keys` 순서, 메인 기록에 칸 값이 있으면 기록 생성자 예외.

`TraitGrowthChangeTests`: 빈 → 획득, 획득 → 획득 + 강화(`Kind = Enhance`, `Added`), 두 칸 동시 변경·레벨/경험치 변경·찬 칸 값 바꿈·칸 비움은 예외.

`OwnedEnhancementResolutionTests`(기존 파일 사례 추가): 획득 칸 + 그 특성의 강화 칸 → 강화형 하나, 획득 + 기본 특성 강화 → 둘 다 반영, 칸 순서가 획득 → 강화.

### 5.2 G-2 `TraitGrowthRulesTests`

- `KindOf`: Added 1·Replaced 0 = 획득, Added 0·Replaced 1 = 강화, 둘 다 1·둘 다 0·Added 2 = 성장 불가.
- `IsUnlocked`: 레벨 = 획득 레벨이면 열림, 하나 낮으면 닫힘, SO 0·진행 꺼짐·메인·칸 참이면 닫힘, 강화 레벨이 낮으면 강화만 열림, 두 단계 이상 올라 둘 다 넘으면 둘 다 열림.
- `Candidates`: 다른 유닛 제외, 보류 특성 제외, 획득 — 가진 키 제외·결과 3개 제외, 강화 — 기본 특성 강화형 포함·획득 특성 강화형은 획득 뒤에만·없는 특성 강화형 제외, 칸이 차면 비어 있음.
- `PlanChoice`: 성공(다른 값 그대로), `NotSub`, `NotUnlocked`, `NotCandidate`, `UnknownDefinition`.
- `Apply` 책임 경계: 후보·열림 검증은 `PlanChoice`와 상점 구매의 공개 판정 경로에서 검증한다(G2.7). 내부 도우미는 검증된 정의로 칸 하나만 채우며, 종점·적용 가능 조건을 중복 계산하지 않는다(TP-17).
- `ValidateDefinition`: 기본 특성 강화 통과, 같은 유닛 획득 정의가 있는 획득 특성 강화 통과, 그 획득 정의가 없으면 실패, 모양 불가 실패.

## 6. 검증

- MSBuild는 돌리지 않는다. Phase마다 사용자가 Unity Test Runner로 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 돌린다(Framework 시험은 삭제 예정, TP-9).
- G-1 뒤 재생 확인: 강화 스테이지에서 강화 구매·다음 전투 특성이 지금과 같다.
- G-3 뒤 재생 확인: `subTraitAcquireLevel`을 2로 두고 ShotGun을 레벨 2로 올린 뒤 비전투에서 명령을 부르면 2연발이 붙고 다음 전투에 반영된다(화면이 없어 시험·디버거로).
- Phase마다 멈추고 보고한다. 커밋은 사용자.

## 7. 커밋 묶음(사용자)

- G-1: 새 파일 다섯(`TraitGrowthKind`·`OwnedTraitGrowth`·`TraitGrowthRules`·새 시험 둘)과 `.meta`, 이름 변경 둘(GUID 유지)을 함께. 계약·합성기·Buffer·상점의 Core 변경을 함께 인수한다. G1.18·G1.19 DB/SO는 Core-only 범위에서 제외되어 View 인계로 남는다.
- G-2: 새 `TraitGrowthDecision`·`TraitGrowthFailureReason`·`TraitGrowthRulesTests`(+ `.meta`).
- G-3: 새 `TraitGrowthChoices`·`TraitGrowthResult`·`TraitGrowthChoiceServiceTests`(+ `.meta`), `CoreIntegrationTestData` 변경.
- G-4: 최종 인계 문서·이 계획·연결 설계의 상태 및 구현 메모. `.superpowers/sdd/` 실행 보조 기록은 git-ignored다.

## 8. 실행 기록

### 8.1 G-1 Core 범위 반영 (2026-10-27)

사용자 지시: G-1부터 진행하되 끝나면 다음 Phase로 넘어가지 않고 보고. 작업자는 Core 담당이며 Core만 구현하고 나머지는 View에 인계한다. 작업 위치는 사용자 선택에 따라 기존 `feature/Cheon/Core/NewCombatSytem` 브랜치다. 커밋·push는 하지 않았다.

| Task | 상태 | 반영 |
|---|---|---|
| G1.1~G1.7 | Core 코드 반영 | 성장 종류·두 칸 불변 계약, 메인 빈 칸 검사, 한 칸 변경 계약, 보유 기록·상점 결정 계약 이전, 레벨 설정 기본 0·0 또는 2 이상 검사. 기존 5/6인자 레벨 설정 호출 유지 |
| G1.8~G1.15 | Core 코드 반영 | G-1용 모양 판정만 제공(`None` = 성장 불가). 합성은 획득 칸 → 강화 칸. 경험치·전투 준비·공격 프로필·능력치 조회 소비 경로 이전. 상점은 한 유닛 한 구매 유지. CoreSystem 상점 어댑터의 임시 기록도 새 계약 사용 |
| G1.16~G1.17 | Core 저장 경로 반영 | `PrepareGrowth`/`InstallGrowth`, 공개 `ApplyGrowth`, 상점 결제의 `GrowthChange.Added` 대조. Buffer는 원본·revision·전투 기록 존재 검사만 담당 |
| G1.18~G1.19 | **View 인계, 미구현** | 사용자 Core-only 범위에 따라 DB/SO 파일을 수정하지 않음. 아래 인계 항목 참고 |
| G1.20 | Core 시험 호출·데이터 수정 | `OwnedEnhancementStateTests`, `OwnedEnhancementResolutionTests`, `EnhancementActionCostTests`, `EnhancementShopRuleTests`, `CoreIntegration/ImpactBoltAuthoringTests` 이전. 상점 시험의 빈 정상 정의를 특성 하나 추가 정의로 변경. 공유 도우미만 바뀌는 `EnhancementFollowUpPreparationTests`와 변경할 호출이 없는 `ShopPurchaseRuleTests`·`ShopOfferPlannerTests`는 파일 수정 없음 |
| G1.21 | 계획대로 수정 없음 | `Assets/Tests/EditMode/Framework`에 `.cs` 479개가 아직 있으며 옛 API 참조가 남아 있음. 삭제·호환 수정하지 않음(TP-9). 삭제 반영 전 Unity 전체 컴파일 및 Test Runner 확인은 진행할 수 없음 |
| G1.22 및 관련 회귀 | **시험 코드 작성, 미실행** | 새 `OwnedTraitGrowthTests`·`TraitGrowthChangeTests`. 기존 시험에 두 칸 합성 순서·경험치 정산의 두 칸 보존·레벨 설정 호환/범위·상점의 종류별 칸 저장과 한 구매 유지·빈 정의 구매 거절 사례 추가 |

확인한 것:

- `git diff --check` 통과. 변경 소스·meta의 UTF-8/LF와 새 파일의 meta 쌍 확인.
- `EnhancementChange` → `TraitGrowthChange`의 GUID `7f67f041eca041be8b2841481a303ca4`, `PreparedEnhancementChange` → `PreparedTraitGrowthChange`의 GUID `43b6d29f2fb6422c9941e4733f481a05` 유지. 파일 이동은 인덱스를 바꾸지 않는 방식으로 수행했고 stage하지 않음.
- production 및 Core/CoreIntegration 시험에서 이전 성장 계약 참조를 검색해 전환 누락 없음 확인. 상품 자체의 `ShopPurchaseSnapshot.Enhancement` 등 변경 대상이 아닌 계약은 유지.
- Core 변경 파일에 `using UnityEngine` 추가 없음. DB/SO·View·Framework 시험 파일 변경 없음.
- 읽기 전용 별도 리뷰에서 조치가 필요한 추가 지적 없음. 검토는 소스 수준이며 SSOT·SOLID·FW 경계, 칸 불변식, 합성 순서, 상점/Buffer 연결과 변경 독립성을 확인함.
- **MSBuild·Unity 컴파일·EditMode·PlayMode는 실행하지 않았다.** 시험 코드를 작성했지만 RED/GREEN 실행 증거는 없다. 사용자 Unity 확인 및 TP-9 선행조건이 남았으므로 Phase 전체 검증 완료로 표시하지 않는다.

View 인계 항목(G-1 잔여):

1. **G1.18 `CategoryConfigDataSO.LevelProgressionInput`:** `subTraitAcquireLevel`·`subTraitEnhanceLevel` 두 직렬화 필드와 툴팁 추가. 새 Core `LevelProgressionSpec`에 같은 이름의 named argument로 전달. 값은 0 또는 2 이상이며 기존 레벨 진행 활성화 판정은 유지한다. 현재 SO는 기존 생성자 경로라 두 값이 모두 0이다.
2. **G1.19 `EnhancementData.Validate`:** `TraitGrowthRules.KindOf(AddedTraits.Count, ReplacedTraits.Count)`가 `TraitGrowthKind.None`이면 데이터 오류로 거절. 모양 원본은 Core 함수이며 DB에 같은 규칙을 복사하지 않는다. 현재 Core 상점 어댑터·구매 판정의 모양 검사는 연결했지만 DB 전체 생성 시 조기 검증은 아직 없다.
3. **검증 경계:** Framework 시험 삭제가 해당 브랜치에 반영된 뒤 사용자가 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 실행한다. DB/SO 인계분까지 연결한 뒤 기존 강화 상점 구매 → 다음 전투 반영을 확인한다.

G-1 종료 당시에는 성장 상태와 소비 경로 전환까지만 반영했다. 레벨별 열림·후보·칸별 상점 구매·특성 수 제한 판정은 아래 G-2 기록, 실제 조회·선택 서비스는 G-3 범위다.

### 8.2 G-2 Core 범위 반영 (2026-10-27)

사용자 지시: "좋아. 그럼 G-2 진행해줘". 기존 Core-only 범위와 현재 브랜치 작업 방식을 유지하고 G-2까지만 진행한다. G-1의 미커밋 변경을 보존했으며 커밋·push는 하지 않았다.

| Task | 상태 | 반영 |
|---|---|---|
| G2.1~G2.2 | Core 코드 반영 | `TraitGrowthRules`에 열림·현재 특성·후보·순수 선택·정의 검사 추가. `TraitGrowthDecision`·`TraitGrowthFailureReason` 신설. 특성 2개 한도·중복 획득·교체 대상·보류 특성을 공통 후보 함수가 판정. 내부 `Apply`는 검증된 후보의 칸만 채움 |
| G2.3~G2.4 | Core 코드 반영 | 상품 생성과 구매 판정이 공통 후보를 사용. `bases`/`basis`·합성용 정의·판매 허용 키를 명시적으로 받음. 허용 키로 제한한 뒤 기존 순서·가중치·개수 규칙으로 추첨. 구매 시에도 판매 허용 키와 후보를 재검사. `AlreadyEnhanced`는 해당 종류의 칸이 찬 경우이며 열거값은 유지하고 XML doc만 보완 |
| G2.5 | CoreSystem 연결 반영 | 진입 시 판매 정의와 기존 성장 정의를 키별로 합치되 `CandidateKeys`는 별도로 전달. 구매 시 대상 기본 정의·기존 성장 정의·판매 정의·해당 스테이지 허용 키를 전달. 추가 DB 조회 뒤 기존 `RevalidatePurchase`·로스터/Run 원본 비교를 유지 |
| G2.6 | CoreSystem 연결 반영 | 강화 상점 어댑터가 `ValidateDefinition`으로 저작을 검사. 기본에 없는 특성을 강화하는 정의는 같은 유닛의 획득 정의를 `GetAll`에서 찾아 합성 확인. 검사 때문에 읽은 획득 정의를 판매 정의로 반환하지 않음 |
| G2.7 | **시험 코드 작성·수정, 미실행** | 새 `TraitGrowthRulesTests`: 독립 레벨 조건, 후보 제외, 획득 전후 강화, 두 칸 선택과 합성, 거절 사유, 정의 저작 경로. `EnhancementShopRuleTests`: 새 입력 도우미, 같은 종류 재구매 거절, 레벨 제한 없는 획득 특성 강화, 합성용 정의의 판매 후보 유입 차단·허용 키 밖 구매 거절, 특성 3개·중복 획득 거절. 상품 생성 시 빈 정의가 제외되므로 그 사례는 고정 상품 입력을 직접 만들어 구매 거절도 검사 |

검증 상태:

- 정적 검사: `git diff --check`, 변경 소스 UTF-8/LF, 새 C#의 meta 쌍, Core의 Unity 의존 추가 여부 확인. production·Core/CoreIntegration의 상점 판정 호출은 새 시그니처로 연결됨.
- Astra 별도 읽기 전용 리뷰에서 조치가 필요한 production 결함·호출 누락·새 경계 위반은 발견되지 않았다. 리뷰의 시험 입력 보완 의견에 따라 보류 특성 필터/가중 추첨 회귀의 정상 후보 둘을 실제 후보가 되는 데이터로 바꾸고, 두 키가 여러 seed에서 모두 추첨되는지도 확인하도록 시험 코드를 보완했다. 실행 결과를 뜻하지 않는다.
- **Unity 컴파일·EditMode·PlayMode·MSBuild는 실행하지 않았다.** TP-9의 Framework 시험 삭제가 아직 이 브랜치에 없으며 479개 `.cs`와 옛 API 참조는 그대로다. 시험 실행·통과와 Phase 전체 검증 완료를 주장하지 않는다.
- G1.18·G1.19 DB/SO 인계분, View 코드·에셋·Framework 시험 파일은 수정하지 않았다. CoreSystem의 DB 읽기·변환 연결만 G2.5·G2.6 범위에서 변경했다.
- **G-3의 로스터 조회·선택 서비스는 아직 연결하지 않았다.** 레벨 조건을 받는 순수 `PlanChoice`는 존재하지만 플레이어 선택을 저장하는 외부 서비스 API는 G-3에서 제공한다. 여기서 멈추고 G-2 결과를 보고한다.

### 8.3 G-3 Core 범위 반영 (2026-10-27)

사용자 지시: "G-3 시작해줘". 기존 Core-only 범위와 현재 브랜치 작업 방식을 유지하고 G-3까지만 진행한다. G-1·G-2의 미커밋 변경을 보존했으며 커밋·push는 하지 않았다.

| Task | 상태 | 반영 |
|---|---|---|
| G3.1 | Core 계약 반영 | `TraitGrowthChoices`는 보유 ID와 불변 후보 행(`Key`, `Name`, `Description`, `Kind`, `TraitKey`)을 제공. `TraitGrowthResult`는 성공·실패 사유와 성공했을 때 저장된 `Final` 기록을 제공. 새 파일과 meta 쌍 추가 |
| G3.2 | CoreSystem 연결 반영 | `InitData`에서 카테고리 설정 표와 선택적인 강화 표를 받음. 기존 `RequireReady`는 그대로 유지. 성장 두 API만 `RequireGrowthReady`에서 유닛 표·카테고리 표·Buffer 바인딩을 요구하며 강화 표가 없으면 빈 정의 목록 사용 |
| G3.3 | 선택 명령 반영 | `ChooseTraitGrowth`는 Run 종료·전투 중·미보유 거절 후 Run ID·로스터 revision을 캡처한 원본으로 기본 정의·해당 유닛 성장 정의·전역 레벨 설정을 읽고 `PlanChoice` 호출. 모든 조회 뒤 ID·revision과 Run 종료 여부를 재검사하고 성공한 변경만 `ApplyGrowth`로 저장. 정의 조회 중 전투가 시작된 경우도 저장 직전 `InBattle`로 거절 |
| G3.4 | 조회 반영 | `GetTraitGrowthChoices`는 열린 종류의 `Candidates`를 표시 행으로 변환하며 쓰지 않음. 미보유·메인·닫힌 칸·후보 없음은 빈 행. 전투 중에도 조회 가능. 상점 판매 키의 제한 없이 해당 유닛의 정의에서 선택 |
| G3.5 | **통합 시험 코드 작성, 미실행** | 실제 `FrameworkRuntime`·Core·Buffer 조립으로 저장 성공·revision 증가·능력치 조회 특성 목록 반영·다른 칸 보존, 같은 칸 재선택, 전투 중 거절과 조회 허용, Run 종료 거절, 읽는 중 로스터/Run ID 변경, 같은 Run 종료(TP-16), 읽는 중 전투 시작, 메인·미보유·정의 없음·레벨 미달, 강화 표 없음 사례 작성 |
| G3.6 | 시험 입력 확장 | `CoreIntegrationTestData`가 선택적인 강화 표와 읽기 고리를 제공. 기본 생성자는 여전히 강화 표 없음. 레벨 값은 기존 `CategoryConfigData.LevelProgression` 입력으로 전달. 시험의 획득은 실제 보유 한도(정의당 1·카테고리당 서브 2)를 지키도록 새 샷건(Cannon)과 발리스타(Heavy)를 사용 |

검증 상태:

- 소스·호출 경로와 저장 경계를 대조하고 `git diff --check`, 새 C# meta 쌍·UTF-8/LF, Core의 Unity 의존 추가 여부를 확인했다. 실제 시험 실행 결과와 구분한다.
- 별도 Astra 읽기 전용 리뷰에서 조치가 필요한 결함은 발견되지 않았다. 서비스의 최종 재검사·기존 API 준비 조건·불변 계약·시험의 보유 한도·덱/전투 진입·능력치 특성 순서를 소스로 확인했다. 시험 통과를 뜻하지 않는다.
- **Unity 컴파일·EditMode·PlayMode·MSBuild는 실행하지 않았다.** 시험을 먼저 작성했지만 RED/GREEN 실행 증거는 없다. Framework 시험 삭제(TP-9)가 아직 이 브랜치에 없으며 사용자 Unity 확인이 남았다.
- G1.18·G1.19 DB/SO 인계분, View·에셋·Framework 시험은 수정하지 않았다. Factory·서비스 키·경험치·전투 규칙·상점 본문도 G-3에서 변경하지 않았다.
- **G-4는 시작하지 않는다.** View·기획 최종 인계 문구와 설계 문서 마무리는 다음 Phase다. 현재 운영 SO는 획득·강화 레벨을 넘기지 않아 두 값이 기본 0이므로, G1.18 인계분이 연결되기 전에는 레벨 선택이 열리지 않는다.

### 8.4 G-4 문서 정리 (2026-10-27)

사용자 지시: "G-4 진행해봐". G-4는 문서 작업만 수행하며 기존 Core 구현·시험 코드·DB/SO·에셋·View·Framework 시험을 추가로 수정하지 않았다.

| Task | 상태 | 결과 |
|---|---|---|
| G4.1 | 인계 문구 작성 완료 | [최종 인계 §2](../handoffs/2026-10-27-sub-trait-growth-core-handoff.md#2-view-전달-문구채팅-붙여넣기용): API 두 개와 실제 응답 필드·실패 사유, 전투 정리 뒤 StageSelect 알림/선택/재조회, 기존 능력치 조회 특성 목록 사용, 종류별 상점 칸·`AlreadyEnhanced` 의미와 현재 View 정책 확인 사항 |
| G4.2 | 인계 문구 작성 완료 | 같은 문서 §3: 전역 레벨 둘·0 또는 2 이상, 후보 저작 모양과 획득 특성 강화의 선행 정의, 현재 획득 후보 3개·강화 후보 0개. §4에 미구현 G1.18·G1.19의 정확한 SO/DB 위치와 연결 요구 |
| G4.3 | 설계·실행 기록 갱신 | 설계 §11에 실제 API·편의 메서드·상수·Apply 책임·저장 전 재검사·Core-only 범위 조정과 검증 수준 기록. 착수 전 조사 표는 이력으로 명시. 현재 상태를 이 계획 §8 및 인계 문서로 연결 |

문서 대조·검증:

- 실제 공개 계약·서비스, SO의 기존 생성자 호출, 운영 강화 정의 넷과 대상 유닛 GUID, `DeferredCombatFeatures`, `ShopResultPolicy`를 읽어 인계 내용과 대조했다. 운영 SO에는 특성 레벨 입력이 아직 없고 기본 0·0이며, 독 가시는 보류 특성으로 제외된다.
- SSOT·SOLID·FW 및 기능 독립성은 설계 §11에 정리했다. View가 레벨/후보/합성을 복사하지 않고 조회·명령을 사용하도록 인계했다.
- G-4 시작 시점의 변경 소스·meta 51개 해시가 문서 작업 뒤 모두 같음을 확인했다. 문서 세 개의 로컬 링크 대상 16개와 `git diff --check`, UTF-8/LF를 확인했다.
- **인계 문서는 준비한 상태이며 외부로 전달하지 않았다. Unity 컴파일·Core/CoreIntegration 시험·PlayMode·MSBuild도 실행하지 않았다.** Framework 시험 삭제 반영, 사용자 Unity 확인, View/DB 잔여 구현과 기획 값·콘텐츠 설정은 계속 남는다. G-4 문서 완료를 전체 운영 활성화 완료로 해석하지 않는다.
- 커밋·push·stage는 하지 않았다. 여기서 멈추고 G-4 결과를 보고한다.

### 8.5 후속 컴파일 누락 보완 (2026-10-27)

사용자가 `Framework/DevelopmentGrantServiceTests.cs:451`의 CS0535를 보고했다. 원인은 G-3에서 `ICharacterRosterService`에 추가한 두 멤버를 시험 가짜 `RecordingRoster`가 구현하지 않은 것이다. TP-9에 따라 해당 파일을 남겨둔 상태였으며 Framework 삭제 선행조건이 실제 컴파일 오류로 나타났다.

- 자동 승인 검토가 기존 Framework 수정 제외 지시를 근거로 최초 패치를 거절하여 그 시도에서는 변경되지 않았다. 사용자가 **"해당 파일만 수정 허용"**으로 명시적으로 예외를 승인한 뒤 적용했다. TP-9의 나머지 범위는 유지한다.
- 해당 파일에 `EnhancementKey` namespace using과 `GetTraitGrowthChoices`·`ChooseTraitGrowth` 두 구현만 추가했다. 기존 시험 가짜의 방식대로 호출 시 `AssertionException`을 던져 개발 지급 경로가 성장 API를 사용하지 않음을 유지한다. production 인터페이스·규칙을 바꾸지 않았다.
- 소스 시그니처와 using, 변경 범위, `git diff --check`를 확인했다. Unity 재컴파일·시험은 실행하지 않았으므로 전체 컴파일 정상화를 주장하지 않는다. 다른 Framework 시험의 옛 API 참조를 포괄 수정하지 않았다.
- 여러 유닛의 레벨 선택은 Core에서 보유 ID별로 독립 판정한다. 팝업 중첩·표시 순서·대기열은 View 인수 범위이며 Core에서 창을 띄우는 구현은 없다. 한 유닛의 선택이 다른 유닛의 선택 기회를 소모하지 않는다.

### 8.6 Framework 시험 보완 원복 (2026-10-27)

사용자가 Framework 시험은 전체 삭제 예정이므로 먼저 원복하라고 지시했다. §8.5에서 추가한 `DevelopmentGrantServiceTests.cs`의 using 하나와 메서드 두 개를 제거했다. 현재 Framework 시험 파일의 작업 트리 변경은 없으며, G-1~G-3 Core·CoreIntegration 변경은 유지한다. 이 원복은 컴파일 오류 해결이나 Framework 시험 삭제가 아니다.

### 8.7 Refector-Stage 병합과 Framework 시험 삭제 인수 (2026-10-27)

사용자가 `origin/feature/Yonggeun/Refector-Stage`를 현재 브랜치에 병합하라고 지시했다. 원격을 fetch한 결과 대상은 `928362f7c6b704eb4a34f6c610432248d3e57739`이며, 기존 HEAD `5a1bfbe1`을 포함하는 후속 15개 커밋이었다. `git merge --ff-only`로 반영해 새 커밋을 만들지 않았고 push하지 않았다.

- 기존 특성 성장 미커밋 작업은 파일 백업과 stash로 보관했다. Git 자동 복원이 CRLF/LF 차이로 중단돼 파일 백업으로 복원했다. 원격에서 삭제한 Framework 파일은 복원하지 않았다.
- 실제 변경이 겹친 `BattlePreparationCalculator.cs`는 착수 원본·로컬 수정·병합 후 파일의 3-way 비교로 결합했다. 로컬 `Growth.IsEmpty` 전환과 원격의 최초 토네이도 배치·`environmentProgress` 전달을 모두 유지했다. 다른 로컬 57개 경로는 바이트 내용 또는 삭제 상태가 백업과 같다. Framework의 과거 시험 파일 한 개는 보관 시 줄바꿈 차이로만 잡힌 항목이며 원격 삭제를 유지했다.
- 현재 HEAD와 대상 원격 SHA가 같으며 unmerged 항목·`MERGE_HEAD`·staging 변경이 없다. Framework 경로의 tracked 파일과 디스크 `.cs`가 모두 0개이고, 보고된 `DevelopmentGrantServiceTests.cs`는 존재하지 않는다. CoreIntegration 시험은 유지했다.
- 정상 저장소 줄바꿈 설정으로 `git diff --check`를 확인했다. **Unity 재컴파일·Core/CoreIntegration 시험·PlayMode·MSBuild는 실행하지 않았다.** 보고된 Framework 오류의 원본 파일 삭제를 확인한 것이며 전체 컴파일 성공을 주장하지 않는다.

## 9. 2판 검토 반영 기록 (2026-10-27)

외부 검토(사용자가 붙여 넣은 소스 대조 리뷰) 다섯 건을 코드와 대조했고 모두 사실이었다.

| 검토 | 확인한 근거 | 반영 |
|---|---|---|
| 1 G-1만으로 컴파일이 안 됨 | `OwnedUnitSpecResolver.cs:19-24`가 `owned.Enhancement`를 읽음 | 합성기 전환을 G1.9로(1판 G2.1), 합성기는 모양을 보지 않게(TP-10), G-1은 동작 보존(TP-11) |
| 2 상점에 후보 판정 입력이 없음 | `ShopService.cs:122-126`(진입에 기본 정의 없음)·`:203-212`(정의 하나만) | G-2를 "공통 후보 판정·상점 전환"으로, 상품 만들기와 구매 재검증이 같은 `Candidates`를 씀(G2.3~G2.5) |
| 3 획득 특성 강화형 등록 시 진입 예외 | `EnhancementShopDefinitionAdapter.cs:29-31`이 성장 없는 임시 기록에 단독 적용 | 정의 검사·적용 가능 검사 분리(TP-12, G2.6) |
| 4 기존 시험이 호출만으로 안 됨 | `EnhancementShopRuleTests.cs:16-18` 빈 정의 | 데이터 바꿈·기대 동작 바꿈을 표로 나눔(TP-14, G1.20·G2.7) |
| 5 서비스 책임 시험 공백 | CoreIntegration 내부 접근(`Systems/AssemblyInfo.cs:4`)·실제 조립 선례(`BossStagePreparationTests.cs:31-40`) | CoreIntegration 시험 G3.5·시험 DB 도우미 G3.6, TP-6 고침 |
| 총평 LSP·ISP | — | 로스터 준비 조건 분리(TP-13, G3.2·G3.5 ⑦) |
| 총평 HEAD | `5a1bfbe1` | §0.4 갱신 |

## 10. 3판 검토 반영 기록 (2026-10-27)

사용자 요청: "지적한 두 항목이랑 Apply 책임까지 계획에 반영해줘. 코드는 아직 수정하지 말고."

| 항목 | 반영 | 검증 계획 |
|---|---|---|
| 합성용 정의가 판매 후보로 섞일 수 있음 | TP-15, G2.3~G2.5: 합성용 정의 목록과 스테이지 판매 허용 키를 별도 입력으로 유지. 상품 추첨 전 제한하고 구매 때도 확인 | G2.7: 동일 종류의 다른 보유 개체에 판매 목록 밖 상품이 나오지 않음, 허용 키가 비면 RNG 미소비, 목록 밖 구매 거절 |
| 같은 Run의 종료가 ID·revision 대조를 통과할 수 있음 | TP-16, G3.3: 모든 정의 조회 뒤 저장 직전 `RunEnded` 재검사 | G3.5 ⑧·G3.6: 조회 도중 동일 Run 종료, 성장 기록·revision 무변경 |
| `Apply(record, definition)`의 재검증 범위가 불명확함 | TP-17, G2.1·G2.4: Application 내부의 검증된 후보 적용 도우미로 한정. 후보 조건은 `Candidates`, 레벨 조건은 `PlanChoice`가 소유 | G2.7·§5.2: 공개 판정 경로의 부적합 선택 거절과 정상 선택의 다른 값 보존 |

이번 반영은 계획 문서만 수정했다. 구현·시험 코드 작성과 Unity 실행은 하지 않았다. TP-9의 외부 선행조건과 Phase 종료 보고 지점은 유지한다.
