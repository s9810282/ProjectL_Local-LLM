# 특성 표시 순서·출처 공개 설계 (2026-11-02, 초안, 코드 변경 없음)

> 상태: **Core 구현 완료(TO-a~TO-c, 2026-11-02, 시험 통과·새 실패 0, 커밋 전, 미전달 — §9).** 결정: 사용자 "모두 A" = Q-TO1~Q-TO4 A. Task 계획은 [trait-display-origin-plan](../plans/2026-11-02-trait-display-origin-plan.md). 사용자(Core 작업자) 요청 "고유 특성이냐, 유닛 기본 특성이냐, 레벨 업으로 획득한 특성이냐도 표시되면 될 것 같다".
> 이 설계는 View의 [특성 획득 순서 표시 요청](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#trait-acquisition-display-order-request)(2026-10-29, 미전달)에 답하고, 거기에 출처를 더한다.

## 0. 배경

- 11-01 병과 기본 특성이 운영 데이터에 켜졌다(Rider = 뛰어넘기 `Leap`, Heavy = 굳건한 의지 `SteadfastWill`). 원래 특성이 2개인 Rider·Heavy 유닛은 이제 3개다.
- 전투 정보창 `CombatUnitInfoView`는 특성 칸이 2개로 고정돼 있고 3개면 예외를 던진다(`CombatUnitInfoView.cs:194`). 유닛을 누르면 전투가 멈춘다. StageSelect 카드(`UnitCardView.abilitySlots`, `DeckSlotInfoAbilities`)도 같은 제한이 있다.
- **그 오류는 View가 n개를 받게 고쳐야 없어진다.** 획득 특성만으로도 2개를 넘기 때문이다. 이 설계와 독립이며, View 담당 작업이다(§7).
- 이 설계는 Core가 "이 특성이 왜 붙어 있나(출처)"와 "어떤 순서로 보일까(표시 순서)"를 사실로 주는 것만 다룬다. 보이는 모양은 UI가 정한다.

## 1. 지금 코드 (2026-11-02, `06835a862`)

### 1.1 출처를 아는 곳은 하나

`OwnedUnitSpecResolver.Compose`(`Application/Enhancements/OwnedUnitSpecResolver.cs:51-116`)가 특성 목록을 합성한다.

| 순서 | 무엇 | 코드 |
|---|---|---|
| 1 | 유닛 정의 특성(DB `UnitSpec.Traits`)을 복사 | `:59` |
| 2 | 전투 병과의 기본 특성이 목록에 없으면 뒤에 붙임 | `:55-61` (`categoryOnly`) |
| 3 | 성장 선택을 차례로: 획득(Acquire)이면 뒤에 붙임, 강화(Enhance)면 그 자리를 교체 | `:66-90` |
| 4 | 훈련 등급 반영(자리 그대로) | `:94-110` |

이 시점의 목록은 출처 순서를 지키고 있다. 그러나 출처는 지역 변수(`categoryOnly`)로만 쓰이고 버려진다. 결과 `UnitSpec`에는 출처가 없다.

### 1.2 순서가 사라지는 곳

- `UnitCatalog.CreateDefinition`(`Application/BootstrapModels/UnitCatalog.cs:103`)과 `UnitDefinition` 생성자(`Domain/Entities/UnitDefinition.cs:92`)가 특성을 `TraitKey` 오름차순으로 정렬한다.
- `TraitSettingsSet` 생성자(`Contracts/Snapshots/Traits/TraitSettingsSet.cs:34`)도 키 순으로 정렬한다.
- 키 순서는 계산·비교가 기대는 성질이다(`UnitStatsSnapshot.SameValues`는 특성 목록을 순서까지 비교, `OwnedCharacterStatsSnapshot`은 설정과 키가 같은 순서인지 검사). **바꾸지 않는다.**

### 1.3 View가 읽는 곳

| 장면 | 읽는 값 | 소비자 |
|---|---|---|
| 전투 정보창 | `CharacterCombatFacts.Stats.TraitStatuses`(키 순) | `CombatInspectionInfo.cs:96` |
| 전투 카드 | 같음 | `UnitCardUI.cs:262` |
| 전투 밖 카드 | `OwnedCharacterStatsSnapshot.Stats.TraitKeys`(키 순) | `UnitCardTraitContent.cs:42`, `DeckSlotInfoUI.cs:58` |

두 경로 모두 같은 합성 결과를 `TraitSettingsSet`으로도 이미 싣는다.

- 전투: `BattlePreparationCalculator.cs:200` → `OwnedUnitSpecResolver.BuildTraitSettings` → `BattleTraitSettings` → Buffer `CombatStateData.TraitSettings`(`For(유닛)`).
- 전투 밖: `OwnedCharacterStatsCalculator.cs:119` → `OwnedCharacterStatsSnapshot.TraitSettings`.

View는 아직 `TraitSettings`를 읽지 않는다(검색 0건).

## 2. 요구

1. 특성마다 출처 셋 중 하나: **유닛 기본**(DB 유닛 정의), **병과 고유**(병과 기본 특성), **레벨업 획득**(성장 Acquire).
2. 표시 순서: 출처 묶음 순서(Q-TO1) → 묶음 안에서 유닛 기본은 DB 입력 순서, 획득은 획득 순서. 강화는 자리를 바꾸지 않는다.
3. 전투 안·밖에서 같은 보유 개체는 같은 순서·출처.
4. 적·소환 등 보유하지 않은 유닛도 같은 규칙(획득 없음).
5. 계산·비교의 키 순서는 그대로.

## 3. 설계

### TO1 출처 값

새 열거 `Contracts/Common/TraitOrigin.cs`:

```csharp
public enum TraitOrigin
{
    None = 0,
    Unit = 1,      // 유닛 기본: DB 유닛 정의
    Category = 2,  // 병과 고유: 현재 전투 병과의 기본 특성
    Growth = 3,    // 레벨업 획득: 성장 Acquire
}
```

새 값 `Contracts/Snapshots/Traits/TraitDisplayEntry.cs`: `TraitKey Key`, `TraitOrigin Origin`(둘 다 필수, `None` 거절).

### TO2 출처 판정 규칙(한 곳)

`Compose` 안에서 최종 목록의 특성마다 정한다. 판정 순서:

1. 키가 **현재 전투 병과의 기본 특성 키**와 같으면 `Category`.
2. 아니면, 성장 선택 중 그 키의 **Acquire 단계**가 있으면 `Growth`.
3. 아니면 `Unit`.

1번을 맨 앞에 두는 것은 Q-TO2·Q-TO3의 권장안(A) 때문이다. 이렇게 하면 "유닛이 원래 같은 특성을 가진 경우"와 "전직 전에 성장으로 같은 특성을 얻은 경우"가 별도 갈래 없이 같은 규칙으로 정해진다.

### TO3 표시 순서(한 곳)

같은 `Compose` 안에서 만든다.

- 묶음 순서: Q-TO1(권장 A: 유닛 기본 → 병과 고유 → 레벨업 획득).
- 유닛 기본: `basis.Traits` 순서(DB 입력 순서).
- 병과 고유: 최대 1개.
- 레벨업 획득: `Growth.Selections`의 Acquire 순서.
- 강화·훈련 등급은 자리를 바꾸지 않는다(키로 자리를 정하므로 저절로).

### TO4 공개 자리: `TraitSettingsSet.Display` (Q-TO4 권장 A)

- `TraitSettingsSet`에 `IReadOnlyList<TraitDisplayEntry> Display`를 더한다. `Traits`(키 순)는 그대로.
- 생성자는 표시 순서의 항목 목록을 받는다: `TraitSettingsSet(IEnumerable<(TraitSpec Spec, TraitOrigin Origin)> entries)`. 안에서 `Display`는 받은 순서, `Traits`는 키 순으로 만든다. 옛 생성자(설정만)는 지운다(호환 감싸기 없음, 호출처 영향은 §6).
- 검사: 키 중복 없음, 출처 `None` 없음, 병과 고유는 최대 1개.
- `SameValues`는 설정 값만 비교한다(출처·순서는 표시 사실이라 넣지 않음). 운영 코드는 `SameValues`를 부르지 않는다(시험만).
- `From(UnitSpec)`는 `From(UnitSpec, IReadOnlyList<TraitDisplayEntry>)`로 바꾼다. 두 목록의 키 집합이 다르면 예외.

이 자리를 고른 이유:
- 생산자가 이미 하나(`OwnedUnitSpecResolver`)이고, 전투·전투 밖 두 경로가 이미 이 값을 싣는다. 새 운반 경로가 필요 없다.
- "전투 동안 바뀌지 않는 유닛별 특성 사실"이라는 성격이 같다.
- `UnitStatsSnapshot`은 모든 계산이 다시 만드는 값이라(필드 추가 때 `ContractShapeGuardTests`·`With`·`SameValues` 동시 수정) 표시 사실을 넣지 않는다.

### TO5 resolver 출력

`Compose`가 이미 등급을 `Dictionary<TraitKey, int> ranks` 출력으로 내는 꼴을 따른다. `List<TraitDisplayEntry> display` 출력을 같은 방식으로 더하고, `BuildTraitSettings`·`OwnedCharacterStatsCalculator`가 그것으로 `TraitSettingsSet`을 만든다. `ResolveBase`가 바뀐 것이 없어 `basis`를 그대로 돌려줄 때도 표시 목록은 만든다(모두 `Unit`, 또는 병과 고유 포함).

### TO6 View가 쓰는 법(View 담당에게 넘길 내용)

- 전투: `CombatStateData.TraitSettings.For(유닛).Display` 순서로 돌고, 상태는 `Stats.TraitStatuses`에서 키로 찾는다.
- 전투 밖: `OwnedCharacterStatsSnapshot.TraitSettings.Display` 순서로 돌고, 사용 가능 여부·등급은 지금처럼 키로 찾는다.
- 출처를 어떻게 보일지(표시 아이콘·자리·문구)는 UI가 정한다.

## 4. 결정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-TO1 | 출처 묶음의 표시 순서 | **A** 유닛 기본 → 병과 고유 → 레벨업 획득(10-29 요구 "기본 특성 1번" 유지) | B 병과 고유 → 유닛 기본 → 획득 |
| Q-TO2 | 유닛이 원래 병과 기본 특성과 같은 키를 가진 경우(예: Heavy 유닛이 굳건한 의지를 원래 가짐, 설정 값은 유닛 것) | **A** 병과 고유(같은 병과는 모두 같게 보임) | B 유닛 기본 |
| Q-TO3 | 메인이 전직 전에 성장으로 얻은 특성이 전직 병과의 기본 특성과 같은 경우 | **A** 병과 고유(TO2 1번 규칙 하나로) | B 레벨업 획득(이력 우선) |
| Q-TO4 | 공개 자리 | **A** `TraitSettingsSet.Display` | B 새 묶음 형식을 `BattleTraitSettings`·`OwnedCharacterStatsSnapshot`에 따로 실음 |

## 5. 설계에서 정한 것(확인 대상)

- P1 출처 이름·값은 TO1 그대로. View가 출처 값을 직접 쓴다(문구는 View DB).
- P2 `TraitSettingsSet` 옛 생성자는 지운다(호환 감싸기 없음). 시험 호출처를 맞춘다(§6).
- P3 `SameValues`에 출처·순서를 넣지 않는다.
- P4 `UnitSpec.Traits`가 DB 유닛 SO의 특성 목록 순서를 지키는지 구현 첫 단계에서 확인한다(지금 코드상 정렬하는 곳은 위 셋뿐).
- P5 전투 중 유닛의 특성 목록은 바뀌지 않는다는 전제를 구현 때 확인한다(성장은 전투 밖에서만).

## 6. 영향

| 대상 | 바뀌는 것 |
|---|---|
| 새 `.cs` 둘 | `TraitOrigin`, `TraitDisplayEntry`(+ `.meta`) |
| `TraitSettingsSet` | `Display`, 생성자·`From` 서명 |
| `OwnedUnitSpecResolver` | `Compose` 표시 출력, `BuildTraitSettings` |
| `OwnedCharacterStatsCalculator` | `TraitSettingsSet` 만드는 줄 |
| 시험 | `TraitSettingsSetTests`, `OwnedCharacterStatsCalculatorTests:103`, `ImpactBoltAuthoringTests:222`, `BattleTraitSettingsPublicationTests:31` 호출 맞춤 + 새 사례(§8) |
| Buffer·View·Framework | 없음(View는 읽을 값이 생길 뿐) |

계산·비교 순서(`UnitDefinition`, `UnitStatsSnapshot`, `CharacterBaselineStatsSnapshot.TraitKeys`)는 바뀌지 않는다.

## 7. View 쪽(별도, 이 설계와 독립)

1. 정보창·카드의 특성 칸 2개 고정을 n개로(전투 UI 개편 설계 §4.2 "n개 항목을 모두 수용"과 같은 방향). **이것이 전투 멈춤의 수정이다.**
2. 이 설계가 들어오면 TO6대로 순서·출처를 읽는다.

1번은 2번을 기다리지 않는다. 전달은 View 담당에게 짧은 메모로.

## 8. Phase(안)

- **TO-a 값:** `TraitOrigin`·`TraitDisplayEntry`·`TraitSettingsSet` 변경, 시험 호출 맞춤. 화면·계산 변화 없음.
- **TO-b 생산:** `Compose` 출처·순서, `BuildTraitSettings`·`OwnedCharacterStatsCalculator`. 새 Core 시험 사례:
  - Rider 원래 특성 2 + 뛰어넘기 → [유닛, 유닛, 병과]
  - Heavy 원래 굳건한 의지 보유 → 병과 고유 1개(Q-TO2)
  - 획득 B 뒤 원래 A 강화 → A 자리 그대로, B는 획득
  - 키가 작은 특성을 나중에 획득 → 뒤에
  - 메인 전직 전 획득 = 전직 병과 기본(Q-TO3)
  - 적(보유 아님) → 획득 없음
  - 전투(`BattleTraitSettings.For`)와 전투 밖(`OwnedCharacterStatsSnapshot`)의 같은 개체가 같은 `Display`
- **TO-c 문서:** Core 전달 문서의 10-29 요청 절에 "대신함" 표시(파일 제목도 갱신), View 담당 메모(채팅 문안).

## 9. 구현 메모 (2026-11-02, TO-a~TO-c 완료, 커밋 전)

- 설계대로 구현했다. 기록 원본은 [계획 §2.1·§3.1·§4.1](../plans/2026-11-02-trait-display-origin-plan.md), Core 전달 문서 [§5.82](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#trait-display-origin).
- TO5의 resolver 출력은 `List<TraitDisplayEntry> display` 출력 인자로 했다(기존 `ranks` 꼴). 판정·정렬은 `OwnedUnitSpecResolver.DisplayOf` 한 곳.
- 호출을 맞춘 시험은 §6 표의 넷에 `BattleTraitSettingsTests:62`(대상 형식 `new(traits)`)가 더해져 다섯이다.
- 시험: Core + CoreIntegration 5,503개 중 실패 25, 기준선과 같은 목록(새 실패 0). 기준선에서 이미 실패하던 운영 DB 병과 기본 특성 시험(`DataBaseAssetTests.CategoryDefaultBattle_*`)은 이 작업과 별개로 남아 있다.
- View 쪽 §7은 아직이다(칸 n개·`Display` 읽기).
