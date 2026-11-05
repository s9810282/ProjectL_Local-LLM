# 새 Core 규칙 대응 — 로비·스테이지 선택 설계

- 작성자: 호진(이호진)
- 상태: 설계 초안, Q-NC1~Q-NC8(§8) 결정 대기, 코드 변경 없음. 10-27 대조: Core 브랜치가 병합돼(`9748bd3a`) 착수 조건(Q-NC8 A)은 충족했고, 그사이 다른 작업자가 D1·D5·CR2에 닿는 곳을 먼저 바꿨다(§2.1). 결정 전에 계획 NC-0b 대조로 이 설계를 다시 맞춰야 한다.
- 범위: 새 전투 규칙이 스테이지 선택의 덱 카드·덱 편집 카드·상점 강화 상품과 로비에 닿는 곳. Core 규칙·계산, 전투 View 입력·재생·HUD(용근님 영역), Stage 실행 층, 특성·강화 문구의 존폐(기획)는 다루지 않는다(§1).
- 계획: [새 Core 대응 계획](../plans/2026-10-26-new-core-lobby-stage-response-plan-Hojin.md)
- 정리: 2026-10-27 머리말·상태를 다듬고 §2.1(10-27 지금 코드와 다른 점)을 더했다. 옛 판은 git `10914570`.
- 설계 판: Core `4bd6b574f`(2026-10-26 06:49), 우리 `334fb533a`, 공통 조상 `d4a6d6fd8`. 줄 번호는 따로 적지 않으면 `4bd6b574f` 판이다.
- Core 원본(천명님 문서, 바뀔 수 있음): 진영 턴 설계 `specs/2026-10-23-player-turn-shared-actioncost-design.md`, 직군 이동 SO 설계 `specs/2026-10-25-category-movement-so-design.md`, 특성 재작업 설계 `specs/2026-10-26-category-and-unit-trait-rework-design.md`, Core→View 인계 `handoffs/2026-10-24-player-turn-core-to-view-handoff.md`·`handoffs/2026-10-26-category-unit-trait-core-view-handoff.md`. 10-27 병합 뒤 모두 우리 브랜치에 있다.

## 1. 목적과 범위

Core 작업자(천명님)가 새 전투 규칙을 만들고 있다. 이 문서는 그 규칙이 로비와 스테이지 선택 화면에 닿는 곳을 찾고, 화면이 Core가 게시한 사실만 읽어 맞추도록 구조를 정한다. 새 규칙을 화면에서 다시 계산하지 않는다.

| 범위 안 | 범위 밖 |
|---|---|
| 스테이지 선택의 덱 카드(전역 `UnitCardUI`), 덱 편집 팝업 카드, 상점의 강화 상품, 로비(덱 팝업·메인 UI·출발·유물) 확인 | Core 규칙·계산(요청으로만), 전투 View의 입력·재생·HUD 대응(용근님 영역), Stage 실행 층(LifeCycle·Flow·Plan·Job), 특성·강화 콘텐츠의 문구와 존폐(기획), 병합 충돌 해결(전투 파일) |

## 2. Core 변경과 우리 브랜치의 상태

| 커밋 | 내용 | 우리 브랜치 |
|---|---|---|
| `42d3263ac`·`d4a6d6fd8` (10-24) | 진영 공유 행동력 10, 유닛당 한 행동(이동 또는 공격), 유닛 턴·타임라인 제거, 강화의 이동·공격 비용 덮어쓰기 제거, 전투 스테이지 `NightLength` 제거, 메인 지속 상태의 행동력·이동 칸 제거, 시간에 기대는 특성·아이템은 "미뤄짐"(쓸 수 없음) | 들어옴(현강님 병합 `62237a3cf` 경유) |
| `1ba7aec47` (10-25) | 직군별 이동 거리·이동 비용을 `CategoryConfigDataSO`로 옮김 | 들어옴(`9748bd3a`, 10-27) |
| `c816c46df` (10-26) | Core 통합 시험 어셈블리 분리 | 들어옴(`9748bd3a`) |
| `4bd6b574f` (10-26) | 라이더 환급·헤비 보호·광란 삭제, 샷건 = 표식(`HuntersMark`), 카우보이 = 끌어오기(`Pull`), 보유 능력치에 표식·끌어오기 설정값, 전투 View와 카드·덱 편집 연결 | 들어옴(`9748bd3a`) |

Core 브랜치는 10-27 `9748bd3a`(`origin/feature/Cheon/Core/NewCombatSytem` → `feature/Hojin/StageSelect`)로 병합됐다. 전투 View 쪽 충돌 정리는 이 설계의 범위가 아니다. Core 브랜치는 그 뒤로도 진행 중이다.

### 2.1 10-27 지금 코드와 다른 점

설계를 쓴 뒤 다른 작업자가 같은 곳을 바꿨다(HEAD `10914570` 대조). 결정 전에 NC-0b에서 다시 맞춘다.

| 설계 항목 | 설계 때(`4bd6b574f`) | 지금 | 누가 |
|---|---|---|---|
| F1·D1·Q-NC2 정보 줄 | 7줄, 우선도 줄 있음, 이동 거리 없음 | 우선도 줄이 빠지고 `Atk AP`·`Move AP` 표기. `ShowDetails`는 5줄(`int×6` 오버로드), `ShowDeckDetails`는 6줄. 이동 거리 줄은 여전히 없다 | 현강 `6568bb94`(10-26) |
| F4·D5·Q-NC3·Q-NC5 사용 불가 표시 | 서식이 사용 가능 여부를 읽지 않음 | `UnitCardTraitContent`가 상태의 사용 가능 여부를 읽어 이름 뒤 "(현재 사용 불가)"·"(일부 사용 불가)"와 설명 끝 안내를 붙인다. 안내는 사유 코드별 문장(`DescribeAvailability`)이라 D5의 "사유 코드를 해석하지 않고 일반 문구 하나"와 다르다. 전투 카드(`TraitStatuses`)에서만 동작한다 | 용근 `11c07868`(10-26) |
| F4·CR1·Q-NC4 보유 스냅샷 | 사용 가능 여부 없음 | `OwnedCharacterStatsSnapshot.TraitAvailability`(`IReadOnlyList<TraitAvailabilitySnapshot>`)와 `ImpactBoltDescription`이 생겼다. 특성 상태 목록(`TraitStatuses`)은 없다. `UnitCardUI`·`DeckEditUI`는 `TraitAvailability`·`ImpactBoltDescription`을 아직 읽지 않는다 | 천명 `80567226`(10-27) |
| F5·D6·CR2·Q-NC6 강화 후보 | 거르지 않음 | `EnhancementOfferPlanner.IsAvailable`이 쓸 수 없는 특성을 더하는 강화를 후보에서 뺀다. CR2 A와 같은 결과다 | 천명 `d586bbf1`(10-27) |
| F2·D3 덱 편집 가시 | 가시 설정값 `null` | 그대로(`DeckEditUI.cs:269-270`). `DeckEditCardModel`의 표식·끌어오기 칸도 그대로 | — |
| D9·Q-NC8 착수 조건 | Core 병합 전 | 충족(`9748bd3a`) | 호진 |

확인 필요: 충격 볼트 특성(`ImpactBoltTraitStatusSnapshot`)의 설정값은 카드 서식이 읽지 않아(`UnitCardTraitContent`의 설명 분기에 없음) View DB 고정 문구로 보일 것이다. 실행으로 확인하지 않았다.

## 3. 로비·스테이지 선택에 닿는 곳

경로 약자: `U/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/`, `V/` = `…/RuntimeSystem/InGameSystem/ViewSystem/`, `C/` = `…/InGameSystem/CoreSystem/CoreCaculator/`. 설계 때(`4bd6b574f`) 사실이다. 지금 바뀐 것은 §2.1.

| # | 사실 | 근거 | 화면 영향 |
|---|---|---|---|
| F1 | 카드 정보 줄은 두 문자열이다. `ShowDetails`는 `HP·DEF·ATK·ATK COST·MOVE COST·PRIORITY`, `ShowDeckDetails`는 `레벨·최대 체력·공격·방어·ATK COST·Move COST·Priority`. 새 규칙에는 행동 순서가 없어 우선도가 쓰이는 곳이 없다(Core 설계 "Priority's new mechanical role is not settled"). 직군 이동 거리 `MoveRange`(이제 SO 값)는 두 스냅샷에 다 있는데 카드에 없다 | `U/Global/UnitCardView.cs:189`·`:197`, 스냅샷 `C/Contracts/Snapshots/CharacterBaselineStatsSnapshot.cs`·`UnitStatsSnapshot.cs` | 덱 카드·덱 편집 카드·전투 카드가 쓸모없는 우선도를 보여 주고 이동 거리는 안 보여 준다(우선도 줄은 10-26 현강님이 지움, §2.1) |
| F2 | 같은 보유 카드를 두 곳이 따로 조립한다. `UnitCardUI`는 `:239-266`, `DeckEditUI`는 `:236-271`. `4bd6b574f`에서 천명님이 표식·끌어오기 설명을 두 곳에 각각 넣었다 | 해당 줄, `git diff d4a6d6fd8 4bd6b574f` | 덱 편집은 가시밭 설정값을 넘기지 않는다(`DeckEditUI:268`의 셋째 인자 `null`). 그래서 선인장 대포·독 가시 강화 카드가 View DB 고정 문구("5의 피해")를 보여 준다. 천명님 인계도 "existing unrelated DeckEdit ThornField description omission"으로 적었다 |
| F3 | 특성 설명이 두 경로다. 전투 카드는 상태 목록 하나(`UnitCardTraitContent.Create(visuals, TraitStatuses)`)를, 보유 카드는 특성마다 따로 받은 칸(`ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`)을 쓴다. `DeckEditCardModel`은 표식 짝만 검사하고 끌어오기 짝은 검사하지 않는다 | `U/Global/UnitCardTraitContent.cs:16`·`:29`, `V/StageMap/Deck/DeckEditDisplayModel.cs:31-45` | 설정값 있는 특성 하나를 더할 때 Core 스냅샷·계산기, 덱 편집 모델·Builder·UI, `UnitCardUI`, 서식까지 7곳(작업자 셋)을 고쳐야 했다(`4bd6b574f`의 실제 변경) |
| F4 | 쓸 수 없는 특성이 있다. `DeferredCombatFeatures.IsTraitAvailable`이 가시밭·집중·이동 후 공격 할인·사령술·재생을 끄고, 위압은 우선도 부분만 끈다. 전투 상태 스냅샷은 `IsAvailable`·`PartialUnavailableReason`을 싣지만 카드 서식은 읽지 않는다. 보유 스냅샷에는 사용 가능 여부가 없다 | `C/Application/Combat/DeferredCombatFeatures.cs`, `C/Application/Combat/Traits/TraitEffectProvider.cs:144-146`, `C/Contracts/Snapshots/Traits/TraitStatusSnapshot.cs` | 플레이어 유닛 중 선인장 대포(가시밭), 저격수(집중), 실더(위압 일부)의 카드가 동작하지 않는 효과를 동작하는 것처럼 보여 준다(전투 카드는 10-26 용근님 표시, 보유 스냅샷은 10-27 천명님 `TraitAvailability`, §2.1) |
| F5 | 강화 상점 후보는 특성 사용 가능 여부를 보지 않는다. 아이템은 이미 거른다(`itemRules.IsAvailable`로 밧줄 제외) | `CoreSystem/ShopSystem/EnhancementShopDefinitionAdapter.cs:16-34`, `CoreSystem/ShopSystem/ShopDefinitionAdapter.cs:37`, `C/Application/Shop/EnhancementOfferPlanner.cs` | 독 가시(`Enhancement_PoisonThorn`, 700골드)는 가시밭이 꺼져 있어 사도 효과가 없다. `ShopPurchaseSnapshot`에는 사용 가능 여부가 없다(10-27 Core가 후보에서 뺌, §2.1) |
| F6 | View DB 특성 문구 중 유닛 턴을 전제로 한 것이 있다. 가시밭 "5턴 동안", 집중(전체가 꺼짐), 재생 "행동 종료 시", 이동 후 공격 할인 "행동력 비용", 사령술 "자신의 턴에", 위압 "우선도를 추가로 n 감소". 강화 다섯 개의 설명에는 비용 언급이 없다 | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/*.asset`, `…/InGameGroup/Enhancement/Item/*.asset` | 콘텐츠 문제다. 고칠지는 기획 결정(D7) |
| F7 | 로비에는 고칠 곳이 없다. 덱 팝업은 이름·초상화만 보여 주고 후보 계약 전이라 빈 목록이다. 메인 UI는 체력만 보여 준다. 출발·상점·회복 실패 사유 열거는 `42d3263ac` 이전과 같다. 유물 쪽 변화는 영혼 소멸 시간이 더 흐르지 않는 것뿐이고, 영혼 드롭 설명은 시간을 말하지 않는다 | `U/Lobby/LobbyDeckUI.cs:162-177`, `V/Lobby/Deck/LobbyDeckPopupController.cs:47`, `U/Global/MainCharactorUI.cs`, `C/Contracts/Results/*FailureReason.cs`, `C/Application/Artifact/SoulEffectCalculator.cs:77`, `ArtifactView_SoulDrop.asset` | 로비는 이번에 바꾸지 않는다(D8) |
| F8 | 스테이지 선택의 다른 곳도 닿지 않는다. `NightLength`·`MaxActionCost`를 읽는 비전투 코드가 없다. 유닛 상품 카드는 레벨·보유 수, 회복 카드는 체력만 보여 준다 | 비전투 View·UI 전체 검색 | 바꿀 곳 없음 |

## 4. 지킬 것

- 화면은 Core가 게시한 값만 읽는다. 쓸 수 없는 특성 목록, 끌어오기 사거리(A+P), 직군 이동값을 View에 따로 두지 않는다.
- 카드 한 장의 모양은 한 곳이 정한다. 전투 카드·보유 카드·덱 편집 카드, 나중의 로비 덱 상세가 같은 곳을 쓴다.
- 설정값 있는 특성이 새로 생겨도 View 쪽은 서식 한 곳만 바꾼다.
- 카드 글은 지금처럼 값이 바뀔 때만 만든다. 매 프레임 경로에 할당을 더하지 않는다.
- UI 등록·입력·수명은 바꾸지 않는다(`Assets/AGENTS.md`의 UIBase·`UIInputBinding` 규약 그대로). 새 화면·새 서비스를 만들지 않는다.

## 5. 설계

### D1 카드 능력치 값 하나 — `UnitCardStats`

`U/Global/UnitCardStats.cs`(새 파일)에 읽기 전용 값 `UnitCardStats`를 둔다. 칸은 체력·최대 체력·방어력·공격력·공격 비용·이동 비용·이동 거리다. 만드는 길은 둘이다.

- `From(CharacterData)`: 전투 값. 유효 방어력·유효 공격력과 설치된 `Stats.AttackCost`·`MoveCost`·`MoveRange`를 옮긴다.
- `From(CharacterBaselineStatsSnapshot)`: 보유 값. 배치 전 기준 능력치를 옮긴다.

계산은 하지 않고 게시된 값을 옮기기만 한다. `UnitCardView`의 두 메서드는 이 값을 받는다(`ShowDetails(UnitCardStats)`, `ShowDeckDetails(int? level, UnitCardStats)`). 정보 줄의 구성은 `UnitCardView` 한 곳에 남는다. 권장안(Q-NC2 A)은 우선도 줄을 이동 거리 줄로 바꾸는 것이다. 우선도 줄은 이미 빠졌으므로(§2.1) 지금은 "이동 거리 줄을 더할지"가 남는다. 프리팹의 줄 배치와 아이콘은 Codex가 확인한다.

### D2 카드 조립 한 곳 — `UnitCardContent`

`U/Global/UnitCardContent.cs`(새 파일)에 정적 함수 둘을 둔다.

- `ShowOwned(view, visuals, level, OwnedCharacterStatsSnapshot)`: 정보 줄(`ShowDeckDetails`)과 특성 내용(`ShowAbilityContent`)을 채운다.
- `ShowCombat(view, visuals, level, CharacterData)`: 같은 일을 전투 값으로 한다.

`UnitCardUI`의 보유·전투 두 경로와 `DeckEditUI`가 이 함수를 부른다. 이름·초상화(`ShowCharacter`), 사망·배치 표시처럼 카드마다 다른 것은 각자 남긴다. 로비 덱에 후보 계약이 생기면 로비 상세도 `ShowOwned`를 부른다. 지금은 부르지 않는다(D8).

### D3 덱 편집 카드는 보유 스냅샷을 그대로 갖는다

`DeckEditCardModel`에서 `HuntersMarkDescription`·`PullDescription` 칸과 부분 짝 검사를 지운다. 대신 `OwnedStats`(`OwnedCharacterStatsSnapshot`, 없으면 null) 하나를 갖는다. `Stats`는 `OwnedStats?.Stats`로 파생한다(따로 저장하지 않음, 규칙 P7). 특성 키와 설정값의 짝은 Core 스냅샷 생성자가 이미 검사한다. 모델은 `OwnedStats.OwnedId`가 카드 ID와 같은지만 검사한다. `DeckEditModelBuilder:46-47`은 스냅샷을 그대로 넘긴다.

효과: 가시밭 설정값 누락(F2)이 풀린다. 설정값 칸이 늘어도(10-27 `ImpactBoltDescription`처럼) 덱 편집은 바뀌지 않는다.

### D4 특성 설명 경로 하나 — Core 요청 CR1과 바꿀 자리

Core에 보유 능력치 스냅샷이 특성 상태 목록을 내 달라고 요청한다(§7 CR1). 목록은 전투와 같은 상태 형식이고, 정의의 설정값으로 만들며, 전투와 같은 사용 가능 사유를 붙인다. 받으면 `ShowOwned`가 전투와 같은 `UnitCardTraitContent.Create(visuals, statuses)` 한 경로를 쓴다.

받기 전에는 `ShowOwned` 안에서 지금 칸 셋(`ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`)을 칸 오버로드로 넘긴다. 목록이 오면 고칠 곳은 `ShowOwned` 한 줄이다. 그때 칸 오버로드(`Create(visuals, keys, thorn, mark, pull)`)를 쓰는 운영 코드가 없어진다. 지울지는 그때 묻는다. 이 오버로드와 그것을 단언하는 시험 셋은 천명님 것이다.

10-27 Core는 목록 대신 사용 가능 여부 목록 `TraitAvailability`를 더했다(§2.1). 이것으로 CR1의 "사용 가능 사유"는 받은 셈이다. 남은 것은 설정값 칸이 늘 때마다 생기는 경로 차이다. CR1을 그대로 요청할지, `TraitAvailability`를 칸 경로와 함께 읽을지는 Q-NC4에서 다시 정한다.

### D5 쓸 수 없는 특성 표시 — 서식 한 곳

`UnitCardTraitContent.Append`가 상태 스냅샷을 보고 표시를 더한다.

- `IsAvailable == false`: 이름 뒤에 표지, 설명 첫 줄에 안내.
- `PartialUnavailableReason`이 있음: 설명 끝에 안내 한 줄.

사유 코드(`DeferredUnitTurnTiming` 등)를 문장으로 하나하나 바꾸지 않고 일반 문구 하나를 쓴다. 그래서 Core가 사유 코드를 늘려도 View는 바뀌지 않는다. 문구는 Q-NC5에서 정한다.

전투 카드는 상태에 사유가 있어 바로 표시된다(Q-NC3). 보유 카드는 CR1(또는 `TraitAvailability`)을 읽은 뒤 표시된다.

10-26 용근님이 같은 자리에 표시를 먼저 넣었다(§2.1). 표지는 "(현재 사용 불가)"·"(일부 사용 불가)"이고 안내는 사유 코드별 문장이다. 이 설계와 다른 점(사유 코드 해석, 문구)을 그대로 둘지 Q-NC5에서 정한다.

### D6 상점 강화 — Core 요청 CR2

쓸 수 없는 특성을 더하거나 바꾸는 강화를 후보에서 빼 달라고 요청한다(§7 CR2). 아이템의 밧줄 거르기와 같은 방식이고, View는 바뀌지 않는다. 대안(Q-NC6 B)은 상품 스냅샷에 사용 가능 여부를 싣고 View가 표시하는 것이다. 구매 거절은 그 경우에도 Core가 한다.

10-27 천명님 `d586bbf1`이 `EnhancementOfferPlanner`에서 같은 거르기를 넣었다(§2.1). CR2는 요청하지 않아도 되는 상태다.

### D7 문구 점검 목록 — 기획 결정

F6의 특성 문구와 독 가시 강화를 사용자에게 목록으로 넘긴다. 이번 코드 작업이 아니다. 규칙을 설명하는 글이라 바꿀 문구는 사용자가 정한다(Q-NC7).

### D8 로비는 이번에 바꾸지 않는다

F7 근거로 로비 코드·씬은 바꾸지 않는다. 로비 덱의 후보 계약은 이번 Core 변경에 없다. 그 계약이 생기면 상세 카드는 D2 `ShowOwned`를 쓴다.

### D9 착수 시점

사용자가 Core 브랜치를 병합한 뒤 착수한다(Q-NC8 A). 병합한 판에서 이 문서의 계약과 줄 번호를 다시 대조한다(계획 NC-0b). Core 요청 문서(CR1·CR2)는 결정 직후에 먼저 쓴다(계획 NC-0a). 병합은 10-27 `9748bd3a`로 끝났다.

## 6. 고치는 파일과 줄 주인

줄 주인은 `4bd6b574f` 판 `git blame`이다. 병합 뒤 다시 센다(계획 NC-0b). Twinsgm = 현강님, Droute = 용근님, GwonCheonmyeong = 천명님.

| 파일 | 바꾸는 것 | 줄 주인 |
|---|---|---|
| `U/Global/UnitCardStats.cs`(새, `.meta` 함께) | D1 | — |
| `U/Global/UnitCardContent.cs`(새, `.meta` 함께) | D2 | — |
| `U/Global/UnitCardView.cs:156-203` | D1 서명·정보 줄 | 현강님 주, 용근님·천명님 일부 |
| `U/Global/UnitCardUI.cs:239-266` | D2 호출 | 현강님 25·천명님 2·용근님 1 |
| `U/Global/UnitCardTraitContent.cs:52-77` | D5 | 현강님 51·천명님 45(파일 전체), 10-26 용근님 표시 줄 |
| `U/Stage/DeckEditUI.cs:236-271` | D2 호출 | 용근님 33·천명님 2 |
| `V/StageMap/Deck/DeckEditDisplayModel.cs:11-50` | D3 | 용근님·천명님 |
| `V/StageMap/Deck/DeckEditModelBuilder.cs:44-48` | D3 | 용근님·천명님 |
| 시험 `Assets/Tests/EditMode/Framework/UnitCardViewTests.cs:230` | D1 서명 맞춤(컴파일) | 현강님 |
| 시험 `Assets/Tests/EditMode/CoreIntegration/HuntersMarkCardContentTests.cs:76-78` | D3 칸 이름 맞춤(컴파일) | 천명님 |

호진님 줄(상점 표시 Builder)은 CR2가 A 방식이면 바뀌지 않는다. 씬·프리팹은 바꾸지 않는다. Codex는 `UnitCard.prefab`·`DeckEditUI.prefab`·`Combat.unity`의 카드 정보 줄 옆에 우선도 아이콘이 있는지만 확인한다.

D5가 서식에 표시를 더해도 천명님 서식 시험(`UnitCardTraitContentTests` 등)은 상태 스냅샷을 직접 만들어 쓴다. 직접 만든 스냅샷은 사용 가능 상태라 표시가 붙지 않아 깨지지 않을 것으로 본다(실행 확인 아님).

## 7. Core 요청(천명님)

요청 문서(계획 NC-0a)는 아직 쓰지 않았다.

### CR1 보유 능력치의 특성 상태 목록

- 무엇: `OwnedCharacterStatsSnapshot`에 `ReadOnlyMemory<TraitStatusSnapshot> TraitStatuses`를 둔다. 순서와 키는 `Stats.TraitKeys`와 같다. 항목은 전투와 같은 상태 형식이다(예: `ThornFieldTraitStatusSnapshot`·`FocusTraitStatusSnapshot`). 값은 정의의 설정값이고, 전투에만 있는 값(예: 쌓인 수)은 초기값이다. 각 항목에 전투와 같은 사용 가능 사유를 붙인다(`WithAvailability`, `DeferredCombatFeatures`와 위압의 일부 규칙).
- 왜: 보유 카드가 전투 카드와 같은 서식 경로를 쓰고(F3), 꺼진 특성을 표시하고(F4), 설정값 있는 특성이 새로 생겨도 덱 편집·카드가 바뀌지 않게 한다.
- View가 하는 것: `UnitCardContent.ShowOwned` 한 줄을 바꾼다(D4). 특성마다 받던 칸은 View가 더 읽지 않는다. 그 칸을 남길지는 Core가 정한다.
- 10-27 상황: 사용 가능 여부만 `TraitAvailability`로 생겼다(§2.1, D4).

### CR2 강화 후보에서 쓸 수 없는 특성 빼기

- 무엇: 강화 후보를 만들 때(`EnhancementShopDefinitionAdapter.Build` 또는 `EnhancementOfferPlanner.Plan`), 더하거나 바꾸는 특성 중 하나라도 쓸 수 없는 강화는 뺀다. 아이템의 `ShopDefinitionAdapter:37`이 밧줄을 거르는 것과 같은 방식이다. 해당하는 것은 독 가시(키 7, 선인장 대포, 가시밭 교체) 하나다.
- 대안: `ShopPurchaseSnapshot`에 사용 가능 여부와 사유를 싣는다. 이 경우 View가 표시하고 구매 거절은 Core가 한다(Q-NC6 B).
- View가 하는 것: A 방식이면 없다.
- 10-27 상황: Core가 `EnhancementOfferPlanner.IsAvailable`로 이미 거른다(§2.1).

## 8. 정할 것

모두 결정 대기다. 각 질문의 첫 안이 권장이다. "10-27 상황"은 §2.1의 변화가 그 질문에 주는 영향이다.

| ID | 질문 | A(권장) | B | C | 10-27 상황 |
|---|---|---|---|---|---|
| Q-NC1 | 공용 카드(현강님)·덱 편집(용근님) 파일을 누가 고치나 | 이 작업에서 고치고 바뀐 줄을 공유 메모로 넘긴다. 덱 편집 모델·UI는 용근님 내부 구현이라 10-25에 정한 "용근님 코드는 연결 부분만" 규칙의 예외 허락이 필요하다 | 공용 카드만 고치고 덱 편집(D2 호출·D3)은 용근님께 요청 메모. 그때까지 덱 편집 가시 누락(F2)과 조립 두 벌이 남는다 | 모두 요청 메모 | 변화 없음 |
| Q-NC2 | 카드 정보 줄 | 우선도 줄을 이동 거리 줄로 바꾼다(7줄 그대로). 라벨 표기를 `ATK COST`·`MOVE COST`·`MOVE RANGE`로 맞춘다 | 우선도를 남기고 이동 거리 줄을 더한다(8줄, 프리팹 줄 배치 다시 — Codex) | 그대로 둔다 | 우선도 줄은 현강님이 지웠고 표기는 `Atk AP`·`Move AP`. 이동 거리 줄 추가와 표기만 남음 |
| Q-NC3 | 같은 위젯인 전투 카드(손패·정보창·전투 중 덱 카드)에도 적용하나 | 적용한다. 줄 구성과 사용 불가 표시가 같아진다. 용근님께 공유한다(전투 View 영역) | 전투 밖에서만. `UnitCardView`에 보유 전용 메서드를 따로 둔다(줄 구성 두 벌) | — | 전투 카드에는 용근님 표시가 이미 들어감 |
| Q-NC4 | 특성 설명 경로 | CR1을 요청한다. 받기 전에는 D2 한 곳에서 지금 칸을 넘긴다 | 요청하지 않고 지금 칸 방식을 유지한다(D3로 덱 편집 누락만 고침). 새 설정형 특성마다 Core 칸 + D2 한 줄 + 서식 | — | Core가 `TraitAvailability`(사용 가능 여부만)와 `ImpactBoltDescription` 칸을 더함 |
| Q-NC5 | 사용 불가 표시 문구 | 이름 뒤 "(미적용)", 설명 첫 줄 "현재 규칙에서는 적용되지 않는 효과입니다.", 일부면 설명 끝 "일부 효과는 현재 적용되지 않습니다." | 표시하지 않고 기획 결정을 기다린다 | 쓸 수 없는 특성은 카드에서 숨긴다 | 용근님 문구("(현재 사용 불가)"·사유별 문장)가 이미 있음. 그대로 쓸지 A로 바꿀지 |
| Q-NC6 | 상점의 쓸 수 없는 강화 | CR2 A: Core가 후보에서 뺀다 | CR2 B: 상품에 사용 가능 여부를 싣고 View가 표시한다 | 요청하지 않고 기획 결정을 기다린다 | Core가 A와 같은 결과를 냄 |
| Q-NC7 | 문구 점검(F6) | 목록만 넘기고 고치지 않는다 | 사용자가 정한 문구로 Codex가 `TraitVisual` 설명을 고친다(허용 필드: description) | — | 변화 없음 |
| Q-NC8 | 착수 시점 | 사용자가 Core 브랜치(`4bd6b574f` 이후 끝)를 병합한 뒤. 병합 판에서 다시 대조한다 | 지금 HEAD에서 시작한다. `4bd6b574f`가 바꾼 같은 줄(덱 편집·카드)을 고치게 되어 병합 충돌이 생긴다 | — | 병합됨(`9748bd3a`) |

## 9. 원칙 점검

| 기준 | 점검 |
|---|---|
| SSOT | 정보 줄 구성은 `UnitCardView` 한 곳, 카드 조립은 `UnitCardContent` 한 곳, 특성 서식은 `UnitCardTraitContent` 한 곳이다. 특성 값·사용 가능 여부의 원본은 Core 스냅샷이다. 덱 편집 모델은 특성 값을 복사하지 않고 스냅샷을 가리킨다. 쓸 수 없는 특성 목록을 View에 두지 않는다 |
| SRP | 값 옮기기(`UnitCardStats`), 카드 조립(`UnitCardContent`), 특성 글(`UnitCardTraitContent`), 위젯 표시(`UnitCardView`), 화면 수명(`UnitCardUI`·`DeckEditUI`)이 나뉜다 |
| OCP | 설정형 특성이 생기면 Core 상태 형식과 서식의 설명 하나만 더한다(CR1 뒤). 사유 코드가 늘어도 View는 그대로다 |
| LSP·ISP | 덱 편집 모델은 필요한 보유 스냅샷 하나만 노출한다. 상태 스냅샷의 기존 계약(불변·`SameValues`)을 그대로 쓴다 |
| DIP | 화면은 Buffer·조회 계약(`OwnedCharacterStatsReader`, 덱 편집 기준 스냅샷)만 읽는다. Core Application 함수(`DeferredCombatFeatures`)를 View가 부르지 않는다 |
| FW 계약 | 서비스 등록·조립·UI 등록·입력 연결을 바꾸지 않는다(FW-001~016 영향 없음). Buffer에 쓰지 않는다 |
| 성능 | `UnitCardUI`는 지금처럼 참조가 바뀔 때만 다시 그린다. 새 값 형식은 구조체이고 글은 다시 그릴 때만 만든다. 유휴 GC 0을 Profiler로 확인한다 |

| 실제 변경 | 설계 때 고칠 곳 | 이 설계 뒤 |
|---|---|---|
| 설정값 있는 특성 추가 | Core 칸·계산기, 덱 편집 모델·Builder·UI, `UnitCardUI`, 서식(7곳, 작업자 셋) | CR1 뒤: Core 상태 형식 + 서식 설명 하나. CR1 전: Core 칸 + `ShowOwned` 한 줄 + 서식 |
| 카드 정보 줄 항목 변경 | `UnitCardView` 문자열 둘 + 호출 셋(`UnitCardUI` 둘, `DeckEditUI`) | `UnitCardStats` + `UnitCardView` 한 곳 |
| 특성 사용 가능 여부 표시 | 표시 안 됨 | 서식 한 곳 |
| 덱 편집 카드 모양 | `DeckEditUI`가 따로 조립 | `UnitCardContent` 재사용 |
| 강화 후보 규칙 | — | Core 한 곳(CR2), View 무변경 |

## 10. 검증

- C#을 바꾼 Phase는 "컴파일 미확인"으로 보고한다. 컴파일과 재생은 사용자가 Unity에서 확인한다. 새 시험 코드·MSBuild·Test Runner 실행 요청은 하지 않는다.
- 깨지는 기존 시험은 §6의 두 곳만 컴파일되게 맞춘다. 다른 작업자 시험이라 공유 메모에 적는다.
- 사용자 재생 확인(계획에 Phase별로 나눔):
  1. 스테이지 선택 덱 버튼 → 카드 hover·고정: 정보 줄(공격 비용·이동 비용·이동 거리), 샷건 표식 10%, 카우보이 끌어오기 1칸.
  2. 덱 편집 팝업: 1과 같은 카드 내용. 선인장 대포의 가시 설명이 설정값으로 보이는지(독 가시를 산 뒤면 7·30%).
  3. 전투(Q-NC3 A): 손패·정보창 정보 줄, 선인장 대포·저격수의 사용 불가 표시, 실더의 일부 안내.
  4. 로비: 덱 팝업·메인 체력 표시가 그대로인지.
  5. 상점: 독 가시가 후보에 나오지 않는지.
  6. Profiler: 스테이지 선택 유휴 GC 0.
- 실행으로 재현하지 못한 실패(예: 정보 줄 아이콘 어긋남)는 "미확인"으로 남긴다.

## 11. 남기는 것

- 전투 View 쪽 새 규칙 대응(행동 가능 여부 소비, 공유 행동력 HUD 등)은 Core 인계 §3이 용근님 영역에 남긴 일이다. 이 설계는 공용 카드에서 전투가 같이 바뀌는 부분만 다룬다.
- 특성·강화의 존폐와 대체는 기획이 정한다(Core 설계 D7). 이 설계는 화면이 그 상태를 사실대로 보여 주게만 한다.
- 로비 덱의 후보 계약, 유닛 상품 카드의 능력치 표시는 이번 Core 변경과 무관해 범위 밖이다.
