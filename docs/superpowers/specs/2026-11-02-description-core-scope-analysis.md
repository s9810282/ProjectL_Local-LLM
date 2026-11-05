# Description 수치 공개를 위한 Core 수정 범위 분석

2026-11-02. 사용자 요청: **“Core에서의 수정 범위 분석해줘”**. 현재 작업 트리의 생산자·Contracts·Buffer·소비 경로를 읽은 분석이다. Core 구현 승인이 아니며 이번 분석에서 코드·에셋·시험을 변경하지 않았다. **컴파일 미확인.**

## 1. 결론

Core의 주된 작업은 **이미 적용된 설정을 불변 읽기값으로 공개하고 전투 밖 조회에도 전달하는 것**이다. 계산 규칙을 View에 복제할 필요는 없다. 다만 여러 공개 계약과 생산 경로가 연결되므로 필드 몇 개를 덧붙이는 것으로 끝나는 작업은 아니다.

| 영역 | 수정 크기 | 핵심 |
|---|---|---|
| 특성의 보유·전투 설정 | 가장 큼 | 대부분의 특성 설정을 공통 형태로 공개. 개체별 최종 정의, 복사·동등성·표시 시점까지 연결 |
| 훈련·증강 선택 결과 | 중간 | 기존 최종 정의 계산을 재사용해 현재/다음·선택 결과 설정을 생성하고 제안에 고정 |
| 아이템 | 중간 | 기존 `ItemDefinition`/`ItemEffectSpec` 재사용. 상점·미장착·장착의 공개 전달 경로 추가 |
| 유물 | 중간 | 기존 `ArtifactEffectData` 재사용. 미배치/배치 목록에 효과 설정을 공개 |
| 환경 | 제한적이지만 규칙 원본에 닿음 | 고지대·모래폭풍의 코드 내 숫자를 같은 원본으로 공개. 값/조건은 유지 |
| 회복·기존 상태 효과 | 추가 Core 수정 불필요 | 필요한 회복 예상값·상태 효과 값은 이미 공개됨 |
| 옛 강화 상품 | 현재 구현 대상에서 제외 | 현재 운영 진입은 훈련. 생성되지 않는 옛 상품을 활성화하지 않음 |

Core의 공격·피해·이동·구매·성장·유물 배치 결과를 바꾸는 것은 이 범위에 포함하지 않는다. 문구와 실제 규칙의 불일치는 별도로 기록한다(§7).

## 2. 권장 공개 형태

Core는 `피해증가율` 같은 **View 토큰 이름이나 완성 문장**을 관리하지 않는다. Core는 종류와 실제 설정값을, View는 토큰 이름/단위/문장/조건부 문장 조각을 소유한다.

- 특성은 `TraitKey`별 불변 **설정 묶음**을 공개하고 현재 상태와 분리한다. 보유/전투/선택지가 같은 설정 표현을 사용한다.
- 기존 `Contracts/Commands/Traits/*TraitSpec`은 종류별 읽기 프로퍼티와 `SameValues`를 갖고 있어 재사용 후보로 적합하다. 거리별 비율도 `DistanceDamageScalePercents.Copy`가 변경할 수 없는 목록으로 복사한다. 같은 숫자를 가진 Description 전용 DTO를 특성마다 새로 복제하는 방안보다 우선 검토한다.
- 전투는 실제 `TraitDefinition`에서 같은 설정 Contracts로 투영하는 연결이 필요하다. 종류별 투영은 기능별로 두고 등록/조립을 분리한다. 보유 쪽은 이미 최종 `resolved UnitSpec`을 얻으므로 원본 숫자를 재계산하지 않는다.
- 값 묶음의 정확한 타입/프로퍼티명은 후속 설계에서 확정한다. 예를 들어 `TraitSettings` 컬렉션을 기존 읽기 snapshot에 추가할 수 있다. 이 문서의 이름은 새 API가 구현됐다는 뜻이 아니다.
- 설정에 없는 현재 중첩·잔량·표적·남은 턴은 기존 `TraitStatuses`/상태 효과 snapshot에 남긴다. 전투 밖 설명을 위해 가짜 전투 상태를 만들지 않는다.
- 기존 가시밭/표식/끌어오기/충격 볼트/정찰의 개별 공개 프로퍼티는 초기 이전에서 호환성을 유지한다. 중복 숫자 원본을 만들지 않고 같은 정의에서 투영한다. 즉시 삭제는 이번 범위에 필요하지 않다.

## 3. 특성: 수정 지점과 전파 범위

아래 경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` 기준이다.

| 파일/경계 | 필요한 수정 또는 확인 |
|---|---|
| `CoreCaculator/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` | 개체별 최종 특성 설정 묶음 추가. TraitKeys/가용성/OwnedId와 일치 검사 |
| `CoreCaculator/Application/Roster/OwnedCharacterStatsCalculator.cs` | 현재 5종만 뽑는 코드를 공용 설정 투영으로 연결. 이미 호출하는 `OwnedUnitSpecResolver.Resolve`의 최종 설정 사용 |
| `CoreCaculator/Contracts/Snapshots/UnitStatsSnapshot.cs` | 전투 설정 묶음을 이 위치에 추가할 경우 생성자·`With`·`SameValues`를 함께 갱신. 숫자 변경이 읽기 비교에서 사라지면 안 됨 |
| `CoreCaculator/Application/Combat/BattleCharacterCalculator.cs` | `CreateOwned`/`CreateEnemy`가 공용 `Assemble`을 쓰므로 보유·적·소환에 같은 설정 투영 연결 |
| 신규 특성 설정 투영/등록 | 실제 정의→Contracts 설정 변환. 공용 문자열 parser나 UI를 Core에 넣지 않음 |
| `CoreCaculator/Application/Combat/Traits/TraitEffectProvider.cs` | 현재는 일부 설정과 실행 상태를 함께 생성한다. 권장안은 새 설정을 별도로 게시하여 모든 실행 상태 재생성을 고치지 않는 것. 기존 상태 계약은 유지 |
| `CharacterSystem/CharacterStatsQueryService.cs`의 `OwnedStatsProjection` | 보유 카드와 덱 조회의 공용 계산 경로. 반환 객체를 그대로 전달할 수 있는지 확인; 숫자별 새 조회를 만들 필요 없음 |

**그대로 활용할 경로:** `InGameCoreSystemFactory`가 `CharacterStatsQueryService`와 `CharacterPresentationPublisher`를 생성한다. Publisher는 Core Tick에서 계산 결과를 `CharacterStatsBuffer`에 게시한다. View의 `ICharacterStatsReadSink`는 이 Buffer를 읽는다. 결과 객체 안의 설정을 늘리면 Publisher/Buffer 저장 코드는 현재 전체 결과를 보관하므로 기능 변경 없이 통과할 가능성이 높다. 보유/덱 갱신의 원본 버전·Pending 처리를 유지한다.

전투 값은 초기화 시 올바른 정의에서 만들고 이후 `With` 복사에서 유지해야 한다. 이동/공격/예비 복귀마다 최신 DB를 다시 읽는 설계는 피한다. 손패·전투 정보는 현재 표시 중인 snapshot에 담긴 설정을 사용한다.

## 4. 훈련·증강: 선택 결과의 설정

| 파일/경계 | 필요한 수정 |
|---|---|
| `CoreCaculator/Contracts/Snapshots/TrainingChoiceSnapshot.cs` | CurrentValues/NextValues 비교값은 유지하고 본문용 현재/다음 설정 추가 |
| `CoreCaculator/Application/Shop/TrainingRules.cs` | 현재는 `ValuesAt(rank)`의 Amount만 전달한다. 동일 Owned 원본과 기존 resolver로 현재/구매 후 최종 설정을 구해 전달 |
| `CoreCaculator/Application/Enhancements/OwnedUnitSpecResolver.cs` | 등급·증강·병과 기본 특성을 합치는 원본. 필요하면 기존 계산을 재사용하는 미리보기 입구만 추출. 별도 등급 공식 금지 |
| `CoreCaculator/Contracts/Roster/TraitGrowthChoices.cs` | 각 후보에 선택 결과의 설정 추가 |
| `CharacterSystem/CharacterRosterService.cs`의 `PrepareNextTraitGrowthOffer` | 현재 이름/설명만 선택한다. 실제 후보 적용 후 설정을 만들어 frozen offer에 포함 |
| `CoreCaculator/Application/Roster/TraitGrowthRules.cs` | 기존 선택 적용은 `Growth.Append`→최종 보유 기록이다. 같은 순수 처리 경로를 결과 미리보기에 재사용할 필요가 있으면 최소 추출 |

특히 `definition.Enhanced`를 그대로 표시하면 안 된다. `OwnedUnitSpecResolver`는 훈련 가능한 특성에서 증강을 등급 증가로 반영하고, 구매 이력·원본 특성을 합쳐 `ResolveRank`를 적용한다. 결과 미리보기도 이 경로를 거쳐야 실제 획득값과 일치한다.

`BufferSystems/Current/CurrentShopData.TrainingPurchase`는 구매 직전 선택지 객체를 보존한다. 새 설정을 선택지 안에 넣으면 구매 후 유지 카드도 같은 값을 쓸 수 있다. `TraitGrowthOffer`도 후보 객체를 동결하므로 재개/재표시 때 최신 DB 값으로 임의 교체하지 않는다. 이 Buffer들은 무조건 수정 대상이 아니라 **전달/동결 보존 확인 대상**이다.

## 5. 아이템·유물

### 아이템

재사용 가능: `Contracts/Items/ItemDefinition`은 Key+typed Effect이며 Whiskey/Roket/Lantern/Rope 설정 타입이 이미 있다. `CombatSystem/ItemDefinitionAdapter`도 SO 입력을 같은 순수 설정으로 변환한다.

| 경로 | 수정 후보 |
|---|---|
| 상점 | `ShopSystem/ShopDefinitionAdapter` → `Contracts/Commands/ShopCandidateSpec` → `Application/Shop/ShopOfferPlanner` → `Contracts/Snapshots/ShopPurchaseSnapshot`에 효과 설정을 함께 전달. 추첨/가격/구매 규칙은 유지 |
| 미장착 재고 | `ItemSystem/ItemInventoryService`가 가진 정의를 기존 Item Buffer의 읽기 경계로 게시. `BufferSystems/Run/ItemInventoryBuffer.cs` 등 저장/초기화·읽기 계약을 연결 |
| 전투 장착 | 실제 `BattleInitialization.ItemDefinitions`와 같은 정의를 전투 읽기에 보관. 초기화 게시/`CombatStateData` 복사 경로 확인. 전역 운영 DB 값으로 개발 fixture의 다른 설정을 덮지 않음 |

수량 0으로 재고에서 사라져도 장착 설명의 정의는 남아야 한다. 정의와 수량의 수명이 다르므로 매번 `Counts` 재생성에 설정을 복제하거나, 장착 시 설정을 잃는 구조를 피한다. 명령 입력의 `ItemDefinitions`가 있다는 이유로 View가 Core 입력 조립기를 직접 호출하지 않는다.

Rope는 현재 `ItemRuleRegistry.IsAvailable`에서 `DeferredUnitTurnTiming`으로 제외된다. 설명값 공개 때문에 상점/장착 자격을 바꾸지 않는다.

### 유물

재사용 가능: `Contracts/Common/ArtifactEffectData`는 효과/값/조건/대상/수명과 `entryId`를 포함하며 하위 값도 구조체다. 별도 숫자 DTO를 다시 만드는 대신 불변 목록 안에 값 복사로 공개할 수 있다.

- 생산 후보: `ArtifactSystem/ArtifactEditingService`. 초기화 때 전체 정의를 읽고 편집 때 대상 정의를 이미 읽는다.
- 전달 후보: `BufferSystems/Artifact/ArtifactBufferSnapshot`, `ArtifactData`, `ArtifactBuffer`와 초기화/편집 게시 계약. 미배치/배치 모두 같은 정의의 설정을 얻고, 편집 게시 뒤에도 보존해야 한다.
- `ArtifactLoadoutReader`/`ArtifactPlacedEffectSnapshot`은 배치된 유물의 전투 계산 입력이다. 미배치 설명을 위해 이 내부 reader를 View에서 호출하지 않는다.
- 한 유물의 여러 효과는 `entryId`/종류로 식별한다. “첫 번째 효과의 수치”처럼 배열 순서에 의존한 숫자 선택은 피한다.
- 현재 식별/배치 계약에는 별도 유물 등급이 없다. 설명 공개를 이유로 새 등급/성장 규칙을 도입하지 않는다. 효과 계산·배치 판정·반격·회복·영혼 정산은 수정 대상이 아니다.

## 6. 환경과 SSOT

`CoreCaculator/Domain/Movement/EnvironmentTerrainRules.cs`에는 현재 다음 숫자가 직접 사용된다.

- 모래폭풍: `inputRange > 1 ? inputRange - 1 : inputRange`. 원래 사거리 0은 0을 유지한다.
- 모래폭풍 피해 잔존율 80, 고지대의 조건부 피해 잔존율 70.

이 값은 View용 별도 상수로 복사할 수 없다. **규칙과 공개 투영이 함께 읽는 한 원본으로 이름을 붙여 추출**하는 최소 수정이 필요하다. 숫자·우선순위·고지대의 발사 위치 조건·0 사거리 보존은 그대로 둔다. 따라서 “Core 규칙 파일을 전혀 건드리지 않는다”는 범위 보장은 정확하지 않다. 동작 변경 없이 원본을 공개하는 수정이다.

공개 후보는 환경 규칙 설정 snapshot을 Core에서 만들고 전투 읽기에 보관하는 방식이다. `StaticTerrainFact`/`CombatStateData` 등 실제 저장 위치는 설계에서 확정한다. 모든 칸마다 고정된 설정을 중복 저장할 필요는 없다. 위치/활성/공개 여부와 설정은 구분하고, 기존 `KnownTo`와 환경 가시성은 유지한다.

지뢰·화약통 범위는 `CombatHazardAreas`라는 기존 공용 입구가 있으므로 별도 범위 공식을 만들지 않는다. 이미 공개된 피해/보너스/횟수/기믹 주기 등은 추가 Core 공개 대상에서 제외한다.

## 7. 분석으로 정정된 범위와 별도 문제

1. **옛 강화 상품은 현재 운영 경로가 아니다.** `ShopService.EnterShop`은 `EnhancementStageDetail`을 `Training` 방문으로 만든다. TrainingPrices가 없으면 진입을 거절하며, 옛 `Enhancement` 구매도 `SubTraitPurchaseUnavailable`로 거절한다. `ShopPurchaseSnapshot.ForEnhancement`는 운영 생성 호출이 검색되지 않았다. 기존 View builder가 존재하는 것만으로 활성 기능이라고 볼 수 없다. 후속 구현은 훈련·증강 결과에 집중하고 옛 상품은 인계 목록만 유지한다.
2. **지뢰 원문/규칙 불일치:** `ObjectVisual_Mine.Description`은 주변 8칸에 절반 피해라고 쓰지만 `MineMovementTileRule.Evaluate`는 모든 대상에게 `mine.Damage`를 요청한다. 값 공개만으로 해소되지 않는다. 현재 규칙에 맞는 문구 정정인지 기획 변경인지 별도로 결정해야 하며, 이번 작업에서 Core 피해를 절반으로 바꾸지 않는다.
3. **기존 구조 검사 영향:** `UnitStatsSnapshot` 주석은 필드 추가 시 `SameValues`·`With`·`ContractShapeGuardTests`를 함께 맞추라고 명시한다. 실제 `UnitStatsSnapshot_PropertiesAreFixed`는 프로퍼티 목록을 고정해 검사한다. 새 프로퍼티 추가 시 이 기존 시험은 컴파일과 별개로 실패할 수 있다. 시험 작성자/수정 권한을 확인하고 사용자 규칙에 따라 처리해야 한다. Core 수정 허용만으로 시험 변경 제한이 해제되지 않는다.

## 8. 구조 검토와 진행 단위

| 기준 | 적용 |
|---|---|
| SSOT | 최종 정의/resolver가 숫자 원본. Core의 문자열이나 View SO 숫자에서 역파싱하지 않음. 환경 상수도 같은 원본 사용 |
| S | 규칙 계산, 설정 투영, Buffer 게시, 토큰 치환/문장, Factory 조립을 분리 |
| O | 특성별 설정 투영/등록에 추가. 거대한 전역 Description switch나 개별 화면 생성자 인자를 계속 늘리지 않음 |
| L | 값 복사/동등성/키 일치/시점/필수값 오류의 기존 보장 유지. 상태를 설정으로 대용하지 않음 |
| I | 특성/아이템/유물/환경에 필요한 읽기값만 공개. 모든 규칙을 담는 전역 Dictionary나 전체 DB resolver를 UI에 넘기지 않음 |
| D | View는 Contracts와 Buffer 읽기를 사용. Domain/규칙 실행기/InGame DB를 직접 참조하지 않음 |
| JMH/FW | 기존 Factory→내부 등록→Buffer 게시→ReadSink 소비 경로 유지. 투영기는 로컬 부품. 새 수명/읽기 포트가 필요한 경우 해당 Group/Buffer Context에 연결하고 루트 재등록으로 우회하지 않음 |
| 기능 독립성 | 한 특성 변경은 그 특성 설정/투영/문구에 한정. 한 아이템은 해당 효과/세 소비 경로. 공용 snapshot/parser 변경은 해당 모든 소비 경로의 회귀가 필요 |

권장 구현 순서:

1. 특성 설정 공개 형태와 복사·동등성 계약 확정 → 보유·전투 전체 연결.
2. 같은 설정 표현으로 훈련 현재/다음·증강 결과 연결. 구매 후 유지 카드/동결 제안 확인.
3. 아이템과 유물을 각 기능 단위로 공개. 기존 계산용 타입 재사용, 초기화/Run·전투 수명 확인.
4. 환경 규칙값의 SSOT 공개와 잔여 문구 대조.
5. View 토큰 연결·전체 커버리지 갱신·사용자 Unity 인수.

이 분석은 Core 구현 요청을 구체화한 것이다. 코드·에셋·Test 코드 작성/수정, MSBuild·Unity 실행, 커밋·push·외부 발송은 하지 않았다. **컴파일 미확인.**
