# 서브 유닛 레벨 특성 성장(특성 획득·특성 강화)

> **과거 설계 — 현재 계약으로 대체됨(2026-10-29 정정).** 아래의 “서브만”, “메인 제외”, 획득 1회·강화 1회 제한은 당시 설계 이력이며 현재 규칙이 아니다. 현재 Core는 **메인·서브 모두** 병과별로 설정한 레벨에 특성을 선택해 획득·강화하는 공통 경로를 지원한다. 현재 계약은 [병과별 성장 설계](2026-10-27-category-trait-progression-design.md)와 [Core 인계](../handoffs/2026-10-27-category-trait-progression-handoff.md), 화면과 최신 콘텐츠 상태는 [증강 설계](2026-10-28-trait-growth-design-Hojin.md)를 따른다.

Date: 2026-10-27
Status: 결정 완료(2026-10-27 사용자 "Q-T1~Q-T8 모두 권장안"). [Task 계획](../plans/2026-10-27-sub-trait-growth-plan.md) 3판의 TP-1~TP-17을 반영. G-1~G-3 Core 코드·시험 코드 작성, G-4 문서 정리. **Unity 컴파일·시험·재생은 미확인**이며 DB/SO·View 잔여는 [최종 인계](../handoffs/2026-10-27-sub-trait-growth-core-handoff.md). 현재 구현 메모는 §11이다.
Source baseline: 특성 성장 착수 HEAD `5a1bfbe1`(최초 조사 `4640511d`). 현재 `feature/Cheon/Core/NewCombatSytem` HEAD `928362f7c`에 Refector-Stage를 fast-forward 병합했고 특성 성장 미커밋 변경을 보존했다(계획 §8.7).

경로 약어: `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `B` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`, `DB` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table`.

## 1. 요구와 범위

사용자 요구(2026-10-27):

- 보유 유닛이 **특정 레벨에 도달할 때마다 특성 획득·특성 강화**를 할 수 있다.
- 그 레벨은 **SO로 조절**한다.
- **서브 유닛의 종점(최대치)은 특성 2개, 그중 하나만 강화 특성**이다.

이 문서가 읽은 뜻(Q-T1로 확인):

```text
레벨 1 (시작)        : 기본 특성 1개                       예) 선인장 대포 [가시밭]
획득 레벨 도달        : + 특성 1개 획득 → 특성 2개            예) [가시밭] [X]
강화 레벨 도달        : 두 특성 중 하나를 강화형으로 교체       예) [독 가시] [X]   ← 종점
```

- "특성 획득" = 기본에 없던 특성 하나를 더한다.
- "특성 강화" = 이미 가진 특성 하나를 그 강화형으로 바꾼다(특성 수는 그대로).
- 종점 = 특성 2개 + 강화 1개. 그 뒤로는 레벨이 올라도 특성은 바뀌지 않는다.

범위 안: Core 계약·규칙·선택 명령·조회, Buffer 저장, 레벨 SO 칸, 기존 강화 정의 재사용.
범위 밖: 화면(선택 팝업·결과 화면 알림·유닛 정보 창 표시, View 작업자 §9), 새 특성 콘텐츠 제작, 메인 캐릭터(§4 D2).

## 2. 착수 전 코드 조사 이력

아래 표는 설계 착수 전 구조를 기록한 것이다. 현재 성장 계약·API·인계 상태는 §11과 최종 인계를 따른다.

| 출처 | 확인한 것 | 이 설계에 주는 뜻 |
|---|---|---|
| `DataBaseSO/InGameGroup/Unit/Item/Playerable/*.asset` | 서브 여덟(Akimbo·Ballista·CactusCannon·Cowboy·DynamiteHammer·Shielder·ShotGun·Sniper)과 메인 모두 **기본 특성 정확히 1개**. | 종점 2개 = 획득 1번. |
| `C/Contracts/Roster/OwnedCharacterRecord.cs` | Run 동안 유지하는 보유 기록 = ID·정의 키·역할·`Level`·`Experience`·`OwnedEnhancement Enhancement`(강화 하나 또는 null). | 특성 성장 상태는 여기 둔다(같은 수명). 지금 칸은 "강화 하나"만 담는다. |
| `C/Contracts/Enhancements/EnhancementDefinition.cs`, `DB/Enhancement/Item/EnhancementDataSO.cs` | 강화 정의 = 대상 캐릭터·`AddedTraits`(더할 특성)·`ReplacedTraits`(같은 키 특성을 바꿀 특성)·비용·가중치·이름·설명. 레벨은 1만. | **획득 = Added 1개, 강화 = Replaced 1개**로 이미 표현된다. 새 정의 표가 필요 없다. |
| `DataBaseSO/InGameGroup/Enhancement/Item/*.asset` | 지금 넷: ShotGun 2연발·도어 브리칭(Added), DynamiteHammer 연쇄 폭발(Added), CactusCannon 독 가시(Replaced: 가시밭 → 강화 가시밭). | 획득 후보 셋이 이미 있다. 독 가시는 바꾸는 가시밭이 보류 특성(`DeferredCombatFeatures`)이라 후보에서 빠지므로 **지금 강화 후보는 0개**다(계획 §0.3에서 바로잡음). 나머지 서브 다섯은 후보가 없다. |
| `C/Application/Enhancements/OwnedUnitSpecResolver.cs` | 보유 기록의 강화를 기본 정의에 합성하는 **유일한 위치**(교체 → 추가 순서). 전투 준비·능력치 조회·공격 프로필이 모두 이것을 부른다. | 획득·강화 둘을 합성하도록 넓히면 전투·조회가 자동으로 따라온다. 획득한 특성을 강화하려면 **추가 → 교체** 순서여야 한다. |
| `C/Application/Shop/EnhancementPurchaseRules.cs`, `EnhancementOfferPlanner.cs`, `B/Core/ShopTransactions.cs` | 강화 상점이 `Enhancement == null`인 보유 개체에 강화 하나를 판다(`AlreadyEnhanced`로 두 번째 거절). Buffer는 `PrepareEnhancement`/`InstallEnhancement`로 쓴다. 보류 특성(`DeferredCombatFeatures`)을 쓰는 정의는 후보에서 뺀다. | 지금 강화 상점도 특성을 바꾸는 두 번째 길이다. 종점 규칙은 두 길이 공유해야 한다(Q-T2). |
| `C/Contracts/Commands/LevelProgressionSpec.cs`, `DB/Category/Item/CategoryConfigDataSO.cs` "Level Progression" | 레벨·경험치 수치 묶음이 카테고리 설정 SO 한 곳에 있다(꺼짐 = 전부 0). | 특성 레벨도 같은 묶음에 두면 "레벨 관련 기획 값은 한 SO"가 유지된다. |
| `C/Application/Combat/Experience/CombatExperienceSettlement.cs`, `C/Application/Roster/ExperienceRules.cs` | 레벨은 **전투 승리 정산에서만** 오르고, 한 번에 여러 단계 오를 수 있다. 메인 성장은 같은 곳에서 자동으로 고른다. | 획득·강화 선택은 플레이어 입력이 필요하므로 전투 정산 안에서 할 수 없다. 전투 뒤(비전투) 명령이 된다. |
| `C/Contracts/Roster/OwnedCharacterChange.cs`, `C/Contracts/Enhancements/EnhancementChange.cs` | 경험치 변경은 강화를 못 바꾸고, 강화 변경은 레벨·경험치를 못 바꾼다(두 변경이 서로 독립). | 같은 원칙으로 "특성 성장 변경"을 둔다. |

위 사실은 코드·에셋을 읽어 확인했다. Unity·시험은 돌리지 않았다. 기획 문서(`캐릭터 레벨 밸런스.pptx`, `통합 기획서.pptx`)에는 레벨별 특성 규칙이 없다.

## 3. 접근 비교

| 안 | 내용 | 판단 |
|---|---|---|
| A. 레벨업 정산에서 자동 부여 | 레벨이 오를 때 후보 중 하나를 seed로 골라 바로 넣는다(메인 성장과 같은 방식). | **기각.** "할 수 있게"는 플레이어 선택으로 읽힌다. 후보가 하나뿐일 때만 같다(Q-T5 C로 남김). |
| B. 레벨업 때 "대기 선택"을 기록에 저장 | 정산이 대기 목록(획득 1·강화 1)을 만들고 선택 명령이 지운다. | **기각.** 대기 여부는 (레벨, SO 레벨, 이미 채운 칸)에서 계산된다. 저장하면 같은 사실의 두 번째 원본이 되고, SO 레벨을 바꾸거나 콘텐츠를 더할 때 이미 만든 대기와 어긋난다. |
| **C. 칸 둘을 기록에 두고, 열림은 계산, 선택은 비전투 명령** (권장) | 보유 기록에 `획득 칸`·`강화 칸`(각각 비었거나 강화 정의 하나). "열렸나" = 레벨 ≥ SO 레벨 && 칸 비었음 && 서브. 후보는 강화 정의에서 그때 계산. | 저장하는 사실은 "무엇을 골랐나"뿐. 정산 코드 무변경. SO 레벨을 바꾸면 바로 반영된다. |

## 4. 설계 결정

### D1. 저장 — 보유 기록의 성장 칸 `OwnedTraitGrowth`

- 새 불변 계약 `C/Contracts/Enhancements/OwnedTraitGrowth.cs`: `Acquired`(`OwnedEnhancement` 또는 null), `Enhanced`(`OwnedEnhancement` 또는 null), `Empty`, `IsEmpty`, `SameValues`, `Get(kind)`·`With(kind, value)`·`Keys`(계획 TP-3에서 구체화).
- `OwnedCharacterRecord.Enhancement`를 **`Growth`로 바꾼다**(같은 끝 선택 인자 자리, null = `Empty`). 지금 "강화 하나" 칸은 이 두 칸 중 하나로 흡수된다(Q-T2). 호환 속성은 두지 않는다(구조 우선) — 바뀌는 호출처는 §5.
- `SameEnhancement` → `SameGrowth`. `OwnedCharacterChange`(경험치 변경)는 성장을 바꿀 수 없다는 규칙을 그대로 둔다.
- 칸 값으로 `OwnedEnhancement`(키·레벨 1)를 재사용한다. 상점 스냅샷·구매 결과가 이미 이 형식을 쓴다.

### D2. 대상 — 서브만

- 사용자 요구가 "서브 유닛의 종점"이다. 메인은 별도 랜덤 성장(`MainGrowthRules`)이 있고, 통합 기획서도 메인을 강화 목록에서 뺀다.
- 규칙이 메인의 칸을 늘 비어 있게 지킨다(기록 생성자 검사).

### D3. 종점 규칙 하나 — `TraitGrowthRules`

`C/Application/Roster/TraitGrowthRules.cs`가 이 시스템의 **유일한 규칙 위치**다(경험치 공식이 `ExperienceRules` 하나인 것과 같은 꼴).

| 함수 | 내용 |
|---|---|
| `KindOf(EnhancementDefinition)` | 정의 모양으로 종류를 정한다. Added 1·Replaced 0 = `Acquire`, Added 0·Replaced 1 = `Enhance`. 다른 모양은 성장에 쓸 수 없다(DB 검사, D4). 종류 칸을 따로 두지 않는다(P7: 계산되는 값). |
| `IsUnlocked(record, kind, spec)` | 서브 && 진행 설정 켜짐 && 그 종류 레벨이 0이 아님 && `record.Level ≥ 그 레벨` && 그 칸이 비었음. |
| `Candidates(record, kind, basis, definitions)` | 그 유닛(`CharacterKey`)의 그 종류 정의 중, 보류 특성이 아니고(`DeferredCombatFeatures`, 상점 필터와 같은 함수) 종점을 넘지 않는 것. 획득: 더할 특성 키가 지금 특성에 없음 && 결과 특성 수 ≤ **2**. 강화: 바꿀 특성 키가 지금 특성(기본 + 획득)에 있음. |
| `PlanChoice(record, key, basis, definitions, spec)` | 레벨 열림과 공통 후보를 판정해 순수 `TraitGrowthDecision`을 만든다. |
| `internal Apply(record, definition)` | 검증된 후보의 칸 하나만 채우고 식별·레벨·경험치·다른 칸을 보존한다. 대상·모양·빈 칸 불변식은 확인하며 후보·레벨·종점은 다시 계산하지 않는다(계획 TP-17). |

- 상수 `MaximumTraits = 2`는 이 클래스에 두며 강화 최대 1회는 기록의 강화 칸 하나가 보장한다. 별도 `MaximumEnhanced` 상수는 만들지 않는다. 두 한도는 Q-T4 A에 따라 코드 규칙이다.
- 강화 레벨이 획득 레벨보다 낮게 설정되면 강화가 먼저 열린다. 그때는 기본 특성만 강화 후보다. 규칙은 순서를 강제하지 않는다.
- 한 번에 두 단계 이상 올라 둘 다 열리면 둘 다 고를 수 있다(대기 저장 없이 계산이라 자연히 성립).

### D4. 후보 — 기존 강화 정의를 그대로 쓴다

- 새 정의 표·SO를 만들지 않는다. `EnhancementDataSO` 하나가 "이 유닛이 얻을 수 있는 특성"과 "이 특성의 강화형"을 모두 표현한다.
- 획득한 특성의 강화형도 같은 방식으로 저작한다(예: ShotGun용 Replaced = 강화 2연발).
- DB 검사(`EnhancementData.Validate`)에 "Added 1·Replaced 0 또는 Added 0·Replaced 1" 모양 규칙을 더한다. 지금 넷은 모두 맞는다. 모양 규칙 원본은 `TraitGrowthRules.KindOf`이고 DB 검사가 그것을 부른다(같은 판정 두 벌 금지).
- 이름·설명·아이콘은 지금처럼 정의(이름·설명)와 View 특성 외형 표(`TraitVisual`)가 낸다.

### D5. 레벨 SO — `LevelProgressionSpec`에 두 칸

`CategoryConfigDataSO` "Level Progression" 묶음에 칸 둘을 더한다(Q-T3·Q-T4 권장안).

| 칸 | 뜻 | 0이면 |
|---|---|---|
| `subTraitAcquireLevel` | 서브가 이 레벨에 도달하면 획득 칸이 열린다 | 획득 없음 |
| `subTraitEnhanceLevel` | 서브가 이 레벨에 도달하면 강화 칸이 열린다 | 강화 없음 |

- 0이 아니면 2 이상(레벨 1은 시작이라 "도달"이 아님). 범위 검사는 계약 생성자가 한다(기존 칸과 같은 방식).
- 진행 설정이 꺼져 있으면(전부 0) 레벨이 오르지 않으므로 칸도 열리지 않는다.
- 전역 값 하나다. 카테고리·유닛별로 달라야 하면 Q-T3 B·C.

### D6. 선택 — 비전투 명령 하나, 조회 하나

`ICharacterRosterService`(Core, `CoreSystem/CharacterSystem/CharacterRosterService.cs`)에 더한다.

```text
TraitGrowthChoices GetTraitGrowthChoices(CharacterInstanceId ownedId)
  → 열린 종류마다 후보 목록(정의 키·이름·설명·더하거나 바꾸는 특성 키). 열린 것이 없으면 빈 값.

TraitGrowthResult ChooseTraitGrowth(CharacterInstanceId ownedId, EnhancementKey key)
  → 성공이면 Buffer 보유 기록을 바꾸고 Final 반환. 실패 이유: NotOwned · NotSub · NotUnlocked · NotCandidate · UnknownDefinition · InBattle · RunEnded · SourceChanged.
```

- 전투 중(전투 사실이 있을 때)은 거절한다. 전투 정의는 준비 때 합성되므로 중간에 바뀌면 안 된다.
- 비용은 없다(Q-T6). 고른 뒤 되돌리거나 다시 고를 수 없다(Q-T7).
- Buffer: `PrepareEnhancement`/`InstallEnhancement`와 `EnhancementChange`를 **성장 변경 `TraitGrowthChange`**로 바꾼다(식별·레벨·경험치 같음, 칸 하나가 비었다가 채워짐, 다른 칸 그대로). 상점과 선택 명령이 같은 쓰기 길을 쓴다.
- 선택하지 않고 다음 전투에 들어가도 된다(Q-T5 권장 A). 열린 칸은 남는다.
- 조회 응답은 `OwnedId`, `Items`이며 각 행은 `Key`·`Name`·`Description`·`Kind`·`TraitKey`다. 미보유·메인·잠김·후보 없음은 빈 행이며 빈 결과가 최대 성장을 뜻하지는 않는다. 전투 중에도 조회는 가능하다.
- 기존 로스터 획득의 준비 조건은 유지한다. 성장 API만 카테고리 설정을 요구하고 강화 표가 없으면 후보가 없다(계획 TP-13).
- 명령은 모든 DB 조회 뒤 Run ID·로스터 revision, 같은 Run의 종료 여부를 확인하고 외부 조회 없이 저장한다(TP-16). 조회 중 전투가 시작된 경우도 저장 직전에 `InBattle`로 거절한다.

### D7. 합성 — `OwnedUnitSpecResolver` 순서 바꾸기

- `Resolve`가 `record.Growth`의 두 칸을 읽는다: **획득 칸 → 강화 칸** 순서. 그래야 획득한 특성의 강화형이 맞게 바뀐다. 각 정의 안에서는 지금처럼 교체 → 추가를 적용하고, 정의 모양(획득·강화 판정)은 보지 않는다(모양은 성장 규칙만 판단, 계획 TP-10).
- 검사는 지금 것을 유지한다(추가 중복·교체 대상 없음은 예외).
- 전투 준비·능력치 조회·공격 프로필·`RequiredProfile`의 "강화된 개체" 판정은 `Growth.IsEmpty`를 본다. 본문은 그대로다.

### D8. 강화 상점과의 관계(Q-T2 권장 A 기준)

- 상점도 같은 두 칸에 쓴다. 판매 정의의 종류(`KindOf`)가 정하는 칸이 비어 있고 후보 조건(D3)을 지킬 때만 판다. `AlreadyEnhanced`의 이름·번호는 유지하며 그 상품 종류의 칸이 찼다는 뜻으로 사용한다(계획 TP-7).
- 상점은 레벨 조건을 보지 않는다(돈으로 일찍 사는 길). 종점은 칸 둘이 지키므로 두 길을 합쳐도 넘지 않는다.
- 상품 만들기와 구매 재검증은 선택 명령과 같은 후보 판정(`TraitGrowthRules.Candidates`)을 쓴다. 그래서 상점 서비스가 보유 개체의 기본 정의와 이미 찬 칸의 정의를 함께 넘긴다(계획 G2.3~G2.5). 강화 스테이지 진입 때의 정의 검사는 정의 자체만 보고, 획득한 특성의 강화형은 같은 유닛의 획득 정의 경로로 확인한다(계획 TP-12).
- 상점으로 산 칸은 레벨이 되어도 다시 열리지 않는다(칸이 이미 참).
- 합성에 필요한 정의와 판매 허용 키를 별도 입력으로 유지한다. 판매 범위는 강화 스테이지 `CandidateKeys` 하나이며, 추첨 전과 구매 재검증에서 적용한다(계획 TP-15). 한 방문에 한 번 구매는 유지하고, 다음 방문에는 같은 유닛의 다른 종류 빈 칸을 살 수 있다.
- 상점에서 계속 팔지, 레벨 성장만 남길지는 콘텐츠 결정이라 이 설계는 구조만 맞춘다.

### D9. 전체 흐름

```text
[전투 승리 정산]  ExperienceRules.Gain → 보유 기록 Level 상승   (무변경)
        │
[EndBattle → 결과 화면 → StageSelect]
        │  View: GetTraitGrowthChoices(id)  ← 열렸으면 "특성 선택 가능" 표시
        │  플레이어 선택 → ChooseTraitGrowth(id, key)
        ▼
  TraitGrowthRules.Apply → TraitGrowthChange → CharacterBuffer 보유 기록 Growth 칸
        │
[다음 전투 준비]  OwnedUnitSpecResolver.Resolve(기본 + 획득 → 강화) → 전투 정의
```

## 5. 바뀌는 파일

| 묶음 | 파일 | 내용 |
|---|---|---|
| 계약 | `C/Contracts/Enhancements/OwnedTraitGrowth.cs`(새, + `.meta`) | D1 |
| | `C/Contracts/Roster/OwnedCharacterRecord.cs` | `Enhancement` → `Growth`, 메인은 빈 칸 검사, `SameGrowth` |
| | `C/Contracts/Roster/OwnedCharacterChange.cs` | `SameGrowth` 호출 |
| | `C/Contracts/Enhancements/EnhancementChange.cs` → `TraitGrowthChange.cs` | 칸 하나 채움 검사(GUID 유지 이름 변경) |
| | `C/Contracts/Commands/LevelProgressionSpec.cs` | 칸 둘·범위 검사 |
| | `C/Contracts/Roster/TraitGrowthChoices.cs`·`TraitGrowthResult.cs`(새, + `.meta`) | D6 조회·결과 |
| 규칙 | `C/Application/Roster/TraitGrowthRules.cs`(새, + `.meta`) | D3 |
| | `C/Application/Enhancements/OwnedUnitSpecResolver.cs` | D7 |
| | `C/Application/Roster/ExperienceRules.cs` | 새 기록 만들 때 `Growth` 넘김(한 줄) |
| | `C/Application/Shop/EnhancementPurchaseRules.cs`·`EnhancementOfferPlanner.cs` | D8 칸 판정 |
| | `C/Application/Combat/BattlePreparationCalculator.cs`·`CombatCommandCalculator.cs`(`RequiredProfile`)·`C/Application/Roster/OwnedCharacterStatsCalculator.cs` | `Enhancement != null` → `!Growth.IsEmpty` |
| 서비스 | `CoreSystem/CharacterSystem/CharacterRosterService.cs` | D6 조회·명령 |
| | `CoreSystem/CharacterSystem/CharacterStatsQueryService.cs` | 칸 둘에서 정의 키 모으기 |
| | `CoreSystem/ShopSystem/ShopService.cs`·`ShopSystem/EnhancementShopDefinitionAdapter.cs` | 상품·구매에 기본 정의·찬 칸 정의를 넘김, 정의 검사 분리(D8) |
| Buffer | `B/Character/CharacterBuffer.cs`·`PreparedEnhancementChange.cs`(→ 성장 변경), `B/Core/ShopTransactions.cs` | D6 쓰기 길 |
| DB | `DB/Category/Item/CategoryConfigDataSO.cs` | 칸 둘·툴팁 |
| | `DB/Enhancement/Item/EnhancementData.cs` | 모양 검사(`KindOf` 호출) |
| 에셋 | `CategoryConfig` 에셋 | 칸 둘 값(기획 값, 사용자 입력) |
| 시험(Core 두 어셈블리만) | `OwnedEnhancementResolutionTests`·`OwnedEnhancementStateTests`·`EnhancementShopRuleTests`·`ShopPurchaseRuleTests`·`ShopOfferPlannerTests`·`ExperienceRulesTests`·`CharacterRosterContractTests` 등 `enhancement:` 인자를 넘기는 곳, 새 `TraitGrowthRulesTests` | 호출 맞춤·규칙 사례 |

바뀌지 않는 것: 전투 계산(공격·이동·특성 효과 본문), 경험치 정산·처치·클리어 보상, 사건 형식, Factory·ServiceKeys·Framework, View 코드.

`ProjectL.Tests.Framework`는 사용자 지시로 제외한다(계획 TP-9). 착수 시 479개 `.cs`가 남아 있었으나 후속 Refector-Stage 병합으로 삭제를 인수했다(계획 §8.7). 서비스·Buffer 통합 시험은 `ProjectL.Tests.CoreIntegration`에 작성했으며 Unity 컴파일·시험은 미확인이다.

## 6. 변경 독립성 점검(대표 변경 사례)

| 변경 사례 | 수정 범위 | 이해 범위 | 검증 범위 | 판단 |
|---|---|---|---|---|
| 기획이 획득·강화 레벨을 바꿈 | `CategoryConfig` 에셋 값 둘 | 칸 뜻(D5) | 없음(규칙 시험은 값과 무관) | 코드 무변경. |
| 새 유닛에 획득 특성 하나를 더함 | `EnhancementDataSO` 에셋 하나 + 표 등록 + View 특성 외형 | 정의 모양 규칙(D4) | DB 에셋 시험 | 규칙·서비스 무변경. 지금 상점 콘텐츠와 같은 일. |
| 종점을 특성 3개로 바꿈 | `TraitGrowthRules` 상수 + 칸 수(기록 계약) | D1·D3 | 규칙·기록 시험 | 칸 수가 계약에 박혀 있어 한 곳이 아니다. 지금 요구가 "최대치"라 받아들임. |
| 경험치 공식을 바꿈(스테이지 기록 설계) | `CombatExperienceSettlement` | 경험치 정산 | 경험치 시험 | 특성 성장은 `Level`만 읽으므로 무관. |
| 강화 상점을 없앰 | 상점 쪽만(정의·후보 공유 그대로) | D8 | 상점 시험 | 성장 칸·선택 명령 무관. |

확인 수준: 제안상의 예상. 호출 경로는 코드에서 확인했고 실행 검증은 없다.

## 7. 원칙 점검

- **SSOT:** 고른 사실은 보유 기록 칸 하나. "열렸나"·"후보"는 저장하지 않고 레벨·SO·칸에서 계산. 종점·모양 규칙은 `TraitGrowthRules` 하나(DB 검사·상점·선택 명령이 부름). 합성은 `OwnedUnitSpecResolver` 하나.
- **SRP:** 경험치 정산은 레벨만, 성장 규칙은 열림·후보·적용만, 서비스는 Buffer 왕복만, 합성은 정의 만들기만.
- **OCP/독립성:** 새 특성·새 유닛 후보는 에셋만 더한다(§6).
- **FW 계약:** 새 서비스·키·Factory 연결 없음. 기존 `ICharacterRosterService`에 멤버 둘.
- **Core/View 경계:** View는 조회 결과를 그리고 명령만 보낸다. 레벨 규칙을 View가 알지 않는다.

## 8. 정할 것

| ID | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-T1 | 요구를 §1처럼 읽어도 되나 | A 획득 = 특성 하나 추가, 강화 = 가진 특성 하나를 강화형으로 교체 / B 다른 뜻(설명 필요) | **A.** 지금 강화 정의의 Added·Replaced와 그대로 맞는다. |
| Q-T2 | 지금 강화 상점(강화 스테이지)과의 관계 | A 상점도 같은 두 칸에 씀(레벨 조건 없이, 종점 공유) / B 상점 강화를 없애고 레벨 성장만 / C 따로 둠(종점을 넘을 수 있음) | **A.** 구조는 하나로 두고 판매 여부는 콘텐츠 결정으로 남긴다. C는 "최대치" 요구와 맞지 않는다. |
| Q-T3 | 레벨 SO 범위 | A 전역 하나(카테고리 설정 SO) / B 카테고리별 / C 유닛별(Unit SO) | **A.** 지금 요구에 차등 얘기가 없다. B·C로 바꿀 때는 `IsUnlocked`의 레벨 읽기 한 곳. |
| Q-T4 | 레벨 SO 모양 | A 칸 둘(획득 레벨·강화 레벨) / B 단계 목록(레벨·종류 여러 줄) | **A.** 종점이 획득 1·강화 1이라 목록은 검사만 늘린다. 종점 상수(2·1)는 코드 규칙으로 둔다. |
| Q-T5 | 고르는 방식 | A 열리면 그 유닛의 후보 전부 중 하나를 플레이어가 고름 / B 무작위 k개 제시(제시 목록 저장 필요) / C 후보가 하나면 자동 | **A.** 지금 유닛당 후보가 0~2개. B는 다시 뽑기 방지용 저장이 생긴다. |
| Q-T6 | 비용 | A 없음 / B 골드·영혼 등 | **A.** 레벨이 대가. 상점은 지금 비용 그대로. |
| Q-T7 | 고른 뒤 바꿀 수 있나 | A 확정(되돌릴 수 없음) / B 비전투 중 다시 고름 | **A.** |
| Q-T8 | 후보가 없는 유닛(지금 서브 다섯: Akimbo·Ballista·Cowboy·Shielder·Sniper, 강화는 CactusCannon만) | A 칸만 열리고 후보 0 → 아무것도 못 고름(콘텐츠가 생기면 그때부터 고를 수 있음) / B 후보 콘텐츠를 먼저 만듦 | **A** 로 구조를 먼저 넣고, 콘텐츠는 기획 일정에 맡긴다. 콘텐츠 없이 켜면 획득 레벨에 도달해도 화면에 고를 것이 없다는 점은 기획에 알린다. |

## 9. 범위 밖·넘길 것

- **View 작업자:** 결과 화면·전투 정리 뒤 StageSelect에서 "특성 선택 가능" 알림, 선택 팝업(후보 이름·설명·특성 아이콘), 기존 능력치 조회로 유닛 정보 갱신. `AlreadyEnhanced`는 그 상품 종류 칸이 이미 찼다는 의미로 관련 문구를 확인한다. Core-only 지시에 따라 남은 SO/DB 연결(G1.18·G1.19)도 최종 인계 §4에 명시했다.
- **기획:** 획득·강화 레벨 값, 서브 다섯의 획득 후보와 각 특성의 강화형(D4 모양대로).
- 메인 캐릭터 특성 성장(D2), 종점 3개 이상, 강화 2단계(`OwnedEnhancement.Level` 2 이상)는 이번에 다루지 않는다.

## 10. Phase 안(결정 뒤 Task 계획에서 확정)

(계획 3판에서 확정한 순서)

1. **G-1 계약·합성 전환:** 칸 계약·기록·변경·Buffer 이름, 합성기, `.Enhancement`를 읽는 모든 곳, SO 칸, DB 모양 검사. 상점은 지금 규칙 유지. 동작 보존, 컴파일 가능한 단위.
2. **G-2 공통 후보 판정·상점 전환:** `TraitGrowthRules` 후보·열림·적용·정의 검사, 상점 상품·구매가 같은 판정을 씀, 진입 정의 검사 분리.
3. **G-3 선택 서비스:** 조회·선택 명령, 준비 조건 분리, CoreIntegration 서비스 시험.
4. **G-4 마무리:** View·기획 인계 텍스트, 기록.

Phase마다 멈추고 보고한다. 검증은 사용자가 Unity Test Runner로 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 돌린다.

## 11. 구현 메모와 인계 상태 (2026-10-27)

사용자는 각 Phase를 따로 승인했으며 자신이 Core 담당임을 명시했다. 현재 브랜치에서 G-1~G-3 Core 구현과 시험 코드 작성, G-4 문서 정리까지 진행했다. 커밋·push는 사용자 담당이다.

| 설계 구체화·범위 조정 | 최종 반영 |
|---|---|
| D1 편의 메서드·상수 | `Get(kind)`·`With(kind, value)`·`Keys`로 통일. 강화 1회는 단일 칸으로 보장 |
| D3 Apply 책임 | 내부 도우미. 후보는 `Candidates`, 레벨은 `PlanChoice`, 판매 범위·가격은 상점 규칙이 판정(TP-17) |
| D4·D8 정의 검사와 현재 적용 가능성 | `ValidateDefinition`은 기본/획득 경로로 저작 가능성 검사. 실제 개체별 조건은 공통 후보 함수. 판매 키는 합성 정의와 별도 유지(TP-12·TP-15) |
| D6 준비·저장 경계 | 기존 API 준비 조건 보존. 성장용 준비 분리. DB 조회 뒤 Run/revision·종료·전투 재검사, `ApplyGrowth`로 저장(TP-13·TP-16) |
| SO/DB·View | Core-only 지시로 G1.18·G1.19와 화면·에셋 값 변경을 인계. 운영 SO가 두 특성 레벨을 전달하지 않아 현재 0·0. 레벨 선택 활성화는 인계분과 기획 값 설정 뒤 가능 |
| 검증 | 정적 검사와 별도 소스 리뷰 수행. Core/CoreIntegration 시험 코드 작성. TP-9 Framework 삭제는 후속 Refector-Stage 병합으로 반영. Unity 컴파일·EditMode·PlayMode·MSBuild 미실행, 사용자 Unity 확인 대기 |

현재 원칙 점검: 선택 사실은 Buffer 보유 기록, 후보·열림은 공통 Core 규칙, 합성은 `OwnedUnitSpecResolver`가 각각 소유한다(SSOT). 기존 Factory 조립·서비스 등록을 유지하며 성장 의존이 획득 API의 준비 조건을 넓히지 않는다(SRP·LSP·ISP·DIP/FW). 정의가 허용하는 새 후보는 콘텐츠로 확장하고 전투·경험치 본문은 건드리지 않았다(OCP·기능 독립성).

View·기획용 채팅 붙여넣기 문구, 정확한 SO/DB 작업 위치, 단계별 검증은 [최종 인계](../handoffs/2026-10-27-sub-trait-growth-core-handoff.md)에 있다. 문서 준비는 외부 전달이나 실행 검증 완료를 뜻하지 않는다.
