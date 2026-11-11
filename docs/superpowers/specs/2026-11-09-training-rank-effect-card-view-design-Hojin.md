# 희생·먼지 연막 훈련 카드 효과 표시 설계

- 작성일·갱신일: 2026-11-09
- 작성: 사용자 요청에 따른 Codex 설계·TC-A 실행 기록. 일반 작성자 역할을 변경하는 문서는 아니다.
- 상태: 2026-11-09 TC-A 구현·정적 검수 및 TC-B 사용자 Unity 확인·인수 완료. 사용자의 전체 확인 및 종료 요청에 따라 이번 훈련 카드 작업을 마무리했다. 확인 출처와 범위는 Task 계획 §5를 따른다.
- 계획: [Task 계획](../plans/2026-11-09-training-rank-effect-card-view-plan-Hojin.md)
- 근거: [Core → View 인계](../handoffs/2026-11-09-sacrifice-dust-screen-upgrades-core-view-handoff.md), 현재 코드·운영 에셋.
- 기존 설계: [훈련소 View 연결 설계](2026-11-02-stage-training-view-design-Hojin.md). 이 문서는 TV8의 숫자 전용 표시 계약과 훈련 Details 조합만 확장한다.

## 1. 목적과 범위

훈련소에서 희생·먼지 연막 상세 카드를 예외 없이 열고, Core가 제공한 현재·다음 값으로 강화 내용을 읽을 수 있게 한다. 희생은 사망 방지 여부와 보호막 양을 함께, 먼지 연막은 같은 진영의 공격 사거리 감소 면제 여부를 표시한다.

포함 범위는 훈련 카드 문구 데이터, 값 변환, 카드 연결, 운영 View 에셋 3개와 해당 표시의 검증이다. 구매 완료 카드도 구매 직전 스냅샷으로 같은 변환을 사용한다.

보유·전투 유닛의 특성 설명, 연막 칸 설명, HP·보호막 연출, ShieldGranted 이펙트는 별도 후속 작업이다. Core 규칙·계약·InGame 에셋, 구매 명령·가격 정책, UI 등록·수명·입력, 공용 카드 프리팹은 이번 구현 범위에 넣지 않는다. 후속 범위를 끝내기 전에는 전체 Core 인계 완료로 보고하지 않는다.

## 2. 구현 전 확인한 사실 (2026-11-09)

| 위치 | 현재 상태와 의미 |
|---|---|
| `TrainingCardContentBuilder.ValueLine` | `Amount`·`SecondAmount`만 처리하고 `RankValueFormat`이 비면 예외를 던진다. |
| 희생·먼지 연막 TraitVisual | 두 에셋 모두 `rankValueFormat`이 비어 있다. |
| `TraitRankValues` | `PreventsLethalSacrificeDamage`·`ExemptsAlliedAttackRangePenalty`가 공개돼 있다. |
| 운영 성장 에셋 | 희생 2등급은 사망 방지 true·보호막 10, 먼지 연막 2등급은 같은 진영 면제 true. 두 에셋 모두 rank3가 없다. |
| `ShopPopupModelBuilder.BuildTraits` | 구매 전은 `TrainingChoices`, 구매 후 산 카드는 `TrainingPurchase`를 사용한다. 구매 입력에는 현재 등급을 넣는다. |
| `StageOfferText_Training` | `TrainingDetails` 형식이 `{0}\n{1}`이다. 값 변화와 Core 기본 설명이 구분 없이 붙는다. |
| `StageEventOfferCard/DetailsInfo/Details` | 현재 기본 프리팹의 Rect는 360×130, TMP 자동 크기는 20~30이다. 실제 화면의 인스턴스 덮어쓰기와 긴 문구의 적합성은 Unity 확인이 필요하다. |

`offer.Description`은 최종 등급별 설명이 아닌 Core의 기본 설명이다. 먼지 연막에서는 기본 설명의 사거리 감소와 강화로 얻는 면제를 구분해서 보여야 한다. 문자열에서 숫자를 추출하거나 Core 문장을 View 코드로 치환하지 않는다.

## 3. 접근 방식 비교

| 안 | 장점 | 한계 | 결정 |
|---|---|---|---|
| 두 에셋의 기존 숫자 형식에 고정 강화 문장 입력 | 변경이 작다. | bool을 읽지 않아 실제 설정과 문구가 달라질 수 있다. | 제외 |
| 기존 숫자 형식 + 효과 여부 전용 형식 + 작은 표시 등록표 | 기존 숫자 카드를 유지하고 새 효과를 실제 bool로 표시한다. 변경 지점이 훈련 표시 안에 모인다. | 문구 필드 하나와 상태 낱말 두 개가 필요하다. | 사용자 TC-A 진행 지시에 따라 적용 |
| 모든 특성 설명·훈련 문구를 범용 조건식 시스템으로 통합 | 복잡한 문장 조합까지 처리할 수 있다. | 보유·전투 설명과 무관한 기존 특성까지 변경·검증 범위가 커진다. | 이번 범위에서 제외 |

사용자가 요청한 숫자·효과 여부 분리 방향을 구체화한 설계다. 2026-11-09 "설계대로 TC-A부터 진행" 지시에 따라 아래 계약을 구현했고, 같은 날 TC-B의 실제 Unity 화면·구매·회귀 항목에 대한 사용자 전체 확인을 받아 인수를 완료했다.

## 4. 표시 계약

### TC1. 원본은 받은 스냅샷

- 현재·다음 효과는 `offer.CurrentValues`·`offer.NextValues`에서 각각 읽는다.
- `CurrentRank`·`NextRank`는 등급 표시에만 쓴다. 등급으로 bool이나 보호막 양을 추측하지 않는다.
- `Amount`와 `SecondAmount`의 기존 6개 숫자 자리 의미를 보존한다: `{0}` 현재, `{1}` 차이, `{2}` 다음, `{3}` 둘째 현재, `{4}` 둘째 차이, `{5}` 둘째 다음.
- 숫자 줄의 존재는 표시 등록의 `ShowAmount`로 정한다. 값이 0이거나 차이가 0이라는 이유로 줄을 없애지 않는다.
- 두 특성의 `SecondAmount`는 null이다. 기존 숫자 카드의 null → 0 처리와 두 값 카드 형식은 유지한다.
- 방문 중 가격·구매 가능 여부·후보·등급 한도는 기존 Buffer/구매 흐름이 소유한다. View가 3등급 후보를 따로 걸러내지 않는다.

### TC2. 표시 등록표

새 `TrainingTraitValueBindings`는 훈련 표시 전용 읽기 전용 등록표다. 등록은 다음 두 줄이며 규칙 수치를 저장하지 않는다.

| TraitKey | ShowAmount | ReadEffect가 읽는 값 |
|---|---|---|
| Sacrifice | true | `TraitRankValues.PreventsLethalSacrificeDamage` |
| DustScreen | false | `TraitRankValues.ExemptsAlliedAttackRangePenalty` |

등록이 없는 특성은 기존 숫자 전용 경로를 사용한다. 효과 등록이 없는 특성에 효과 형식만 넣은 잘못된 설정은 해당 카드를 만들 때 예외로 알린다. 특성별 if/switch를 카드 조립·구매·UI에 분산시키지 않는다.

### TC3. 문구 소유와 데이터 추가

`TraitVisualData`에 직렬화 문자열 `rankEffectFormat`과 읽기 전용 접근자 `RankEffectFormat`을 추가한다. `{0}`은 현재 상태 낱말, `{1}`은 다음 상태 낱말이다. 기존 `rankValueFormat` 이름과 6자리 계약은 유지한다. 기존 툴팁의 "훈련되는 특성에 비우면 멈춤"은 "숫자 줄을 표시하는 훈련 카드에 필수, 효과만 표시하는 카드는 사용하지 않음"으로 바꾼다.

공통 낱말은 `StageOfferText_Training`의 줄로 관리한다. `StageOfferLineKind`에 다음 값을 추가하고 `StageOfferTextData.ValueCountOf`에는 인자 수 0으로 등록한다. 구현 직전 다른 변경과 enum 값 충돌이 없는지 확인한다.

| 새 줄 | 값 | 운영 문구 |
|---|---|---|
| `TrainingEffectEnabled` | 16 | `적용` |
| `TrainingEffectDisabled` | 17 | `미적용` |

사용자가 보는 문구는 에셋에만 둔다. 효과 형식과 true/false 낱말을 한 문자열로 하드코딩하거나 bool의 영문 문자열을 직접 출력하지 않는다.

### TC4. 운영 에셋 문구

| 에셋·필드 | 입력할 값 |
|---|---|
| 희생 `rankEffectFormat` | `대신 받은 피해로 사망 방지: {0} → {1}` |
| 희생 `rankValueFormat` | `피해 처리 후 보호막: {0} → {2} (+{1})` |
| 먼지 연막 `rankEffectFormat` | `연막 생성자와 같은 진영의 공격 사거리 감소 면제: {0} → {1}` |
| 먼지 연막 `rankValueFormat` | 빈 값. 숫자 줄을 사용하지 않는다. |
| 훈련 `TrainingDetails` | `강화 내용: {0}\n기본 효과: {1}` |

`TrainingDetails`의 인자 수는 2를 유지한다. 첫 인자는 변환기가 만든 효과·수치 줄, 둘째는 기존 `offer.Description`이다. 이 구분은 모든 훈련 특성 카드에 적용되므로 숫자 전용 카드도 회귀 확인한다. 새 헤더는 최종 등급 설명으로 오해하지 않도록 하는 구분이며 Core 기본 문장을 재작성하지 않는다.

희생의 출력 순서는 사망 방지 여부 → 보호막 수치다. 먼지 연막은 면제 여부 한 항목만 출력하며 `0 → 0`이나 빈 숫자 줄을 붙이지 않는다. 효과 항목과 숫자 항목은 LF 하나로 연결한다.

운영 값 예시는 희생 `미적용 → 적용`, 보호막 `0 → 10 (+10)`, 먼지 연막 `미적용 → 적용`이다. 이 값은 검증 기대값이며 View 코드의 상수가 아니다. bool이 같은 스냅샷이면 같은 상태끼리 표시한다. `true → false`도 전달된 값 그대로 표시한다.

### TC5. 검증과 실패

DB 생성 시 기존 `ValidateRankValueFormat`은 유지하고, 새 `ValidateRankEffectFormat()`은 비어 있지 않은 효과 형식의 `string.Format` 문법과 두 자리 사용을 검사한다. 서로 다른 검사용 문자열 2개로 형식을 완성한 결과에 양쪽 값이 모두 들어가는지 확인한다. 이스케이프된 중괄호를 실제 자리로 잘못 세지 않는다.

DB 검사는 모든 TraitVisual에 적용되므로 효과 형식의 빈 값 자체는 허용한다. 어떤 훈련 카드에 어떤 줄이 필수인지는 TC2 등록표에 따라 카드 생성 시 판단한다. 등록표를 DB 계층에 복제하거나 DB가 ViewSystem 변환기를 참조하게 하지 않는다.

| 조건 | 처리 |
|---|---|
| visual·offer·text 인자가 null | `ArgumentNullException` |
| 현재·다음 값 누락, visual/offer/각 값의 TraitKey 불일치 | 특성·대상을 포함한 `InvalidOperationException` |
| 숫자 경로에서 `RankValueFormat` 누락 | 기존과 같은 실패 계약 유지 |
| 등록된 효과의 `RankEffectFormat` 또는 상태 낱말 누락 | 누락 필드/줄 종류를 포함한 예외 |
| 먼지 연막에 숫자 형식이 잘못 설정됨 | 사용하지 않는 문구를 묵살하지 않고 구성 오류로 보고 |
| 효과 등록 없는 특성에 효과 형식 설정 | 지원되지 않는 표시 설정으로 보고 |
| 형식 자리 오류 | `FormatException`을 문맥이 있는 `InvalidOperationException`으로 감싼다. |

기본 문구 fallback으로 실패를 감추지 않는다. 예외는 기존 팝업 오류 경계가 처리하며 변환기는 화면을 직접 닫거나 Buffer를 수정하지 않는다.

### TC6. 카드 조립과 구매 후 표시

`TrainingCardContentBuilder.Trait`는 외형 조회, 이름·가격·버튼·등급 조립을 맡는다. 기존 `ValueLine`을 새 무상태 `TrainingTraitValueFormatter.Format` 호출로 대체한다. 원본은 호출자가 넘긴 `offer` 하나로 유지한다.

구매 후에는 `ShopPopupModelBuilder`가 이미 넘기는 `TrainingPurchase`를 사용한다. 새 스냅샷 재조회나 View 소유 구매 캐시를 추가하지 않는다. 산 카드의 `1 → 2` 표시를 `2 → 3`으로 바꾸지 않는다.

## 5. 파일과 인터페이스

아래 경로는 저장소 루트 기준이다.

| 작업 | 경로 | 책임·계약 |
|---|---|---|
| 추가 | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingTraitValueBindings.cs` + `.meta` | `internal static bool TryGet(TraitKey key, out TrainingTraitValueBinding binding)`. 같은 파일의 `internal readonly struct TrainingTraitValueBinding`은 `bool ShowAmount`, `Func<TraitRankValues, bool> ReadEffect`와 두 값을 받는 생성자를 갖는다. 등록 함수는 null을 허용하지 않는다. |
| 추가 | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingTraitValueFormatter.cs` + `.meta` | `internal static string Format(TraitVisualData visual, TrainingChoiceSnapshot offer, StageOfferTextData text)`. 검증·값 변환·줄 결합을 수행한다. |
| 수정 | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingCardContentBuilder.cs` | 변환기 연결, 기존 숫자 변환 중복 제거. |
| 수정 | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs` | `rankEffectFormat`·접근자·형식 검사·툴팁. |
| 수정 | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferLineKind.cs` | 공통 적용/미적용 줄 2개. |
| 수정 | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferTextData.cs` | 두 새 줄의 인자 수 0. TrainingDetails 설명 주석 갱신. |
| 수정 | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageOfferText/Item/StageOfferTextDataSO.cs` | 새 줄과 TrainingDetails의 Inspector 계약 안내. |
| 수정 | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Heavy/TraitVisual_Sacrifice.asset` | TC4의 두 형식. |
| 수정 | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Rider/TraitVisual_DustScreen.asset` | TC4의 효과 형식. |
| 수정 | `Assets/ProjectL/DataBaseSO/ViewGroup/StageOfferText/Item/StageOfferText_Training.asset` | 공통 상태 낱말과 기본/강화 구분. |

새 helper는 같은 Shop 네임스페이스 안의 내부 협력 객체다. `.meta`는 새 GUID로 작성하고 기존 파일·에셋 GUID는 유지한다. 기존 파일을 이동하거나 SO의 네임스페이스를 변경하지 않는다.

## 6. SSOT·SOLID·JMH/FW·기능 독립성 점검

기준은 [View 설계 원칙 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [Framework 개발 지침 §23·§26~28](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)이다.

| 기준 | 이번 적용 |
|---|---|
| SSOT | 숫자·bool·가격·등급·후보는 Core 스냅샷, 한국어 표현은 View SO. `ShowAmount`는 표시할 항목의 종류이며 강화 규칙이나 수치가 아니다. |
| S | 카드 조립, 특성별 표시 등록, 순수 변환, 에셋 형식 검사를 분리한다. |
| O | bool 효과의 연결은 등록표 항목으로 추가한다. 카드 조립·구매 컨트롤러에 특성 분기를 추가하지 않는다. |
| L | 기존 숫자 6자리 계약과 잘못된 필수 구성의 예외 계약을 유지한다. 새 helper는 기존 서비스 인터페이스를 대체하지 않는다. |
| I | 변환기는 visual·offer·text만 받는다. 전체 Buffer·Context·UI 서비스를 받지 않는다. |
| D | 기존 `ITraitVisualTableQuery`를 통한 조회를 유지한다. 공개 스냅샷만 소비하며 Core 실행 서비스·InGame DB를 조회하지 않는다. |
| FW-001~003 | helper는 서비스·모듈이 아니며 Builder/Export 등록을 추가하지 않는다. |
| FW-004·005~009 | 입력·구매는 기존 Submit 경로, 개폐는 기존 UISystem 경로를 유지한다. 새 화면·구독·MonoBehaviour·직접 SetActive 호출이 없다. |
| JMH §26~28 | 현재 두 효과를 처리하는 작은 등록표와 변환기만 둔다. 범용 조건식 파서·새 System·추가 Factory 계층은 만들지 않는다. |

조립 경로는 현재 `ShopPopupViewFactory.Create`가 만드는 `TrainingCardContentBuilder` → 이를 보관하는 `ShopPopupModelBuilder` → `TrainingCardContentBuilder.Trait` → 새 정적 변환기다. 새 helper의 내부 서비스 등록·상위 Export·resolver 조회는 모두 없다. 기존 생성자와 조회 의존성을 유지한다.

UI 등록 단위는 기존 `StageOfferUI`이고 카드 안 문자열만 바뀐다. 기존 UIId·부모·Policy·바인딩·열기/닫기·재사용/해제 계약에 새 변경이 없다. 같은 `StageOfferCardView`에 완성된 문자열을 전달한다.

기능 변경 시나리오의 경계는 다음과 같다.

| 변경 시나리오 | 이번 설계 뒤 수정·검증 위치 |
|---|---|
| 희생 문구 수정 | 희생 View 에셋, 해당 카드 읽기 확인 |
| 적용/미적용 낱말 수정 | 훈련 텍스트 에셋 한 곳, bool 카드 두 종류 |
| 새로운 bool 공개값의 연결 | 등록표와 해당 에셋. 숫자·조립 계약이 같으면 구매 흐름 수정 불필요 |
| 숫자 자리 의미·카드 공통 조합 변경 | 공통 변환/TrainingDetails와 숫자 한 값·두 값·bool 카드 회귀 확인 |

기존 `ValueLine` 안에 효과 분기를 계속 넣으면 카드 조립 코드까지 함께 읽고 수정해야 한다. 제안 구조에서는 변환 변경이 helper에 모이지만 `TraitVisualData`와 훈련 공통 텍스트 검증은 공유 경계이므로 부트 DB 확인과 기존 숫자 카드 회귀는 필요하다.

## 7. Phase와 완료 기준

| Phase | 산출물 | 종료 조건 |
|---|---|---|
| TC-A 구현·정적 검수 | TC1~TC6 코드 연결과 운영 에셋 3개를 한 묶음으로 완성 | Task A1~A3 정적 확인 후 보고하고 멈춤. 컴파일 미확인 유지. |
| TC-B Unity 인수 | 사용자의 실제 화면·구매·회귀 확인과 결과 기록 | Task B1의 필수 항목을 확인하고 미확인/실패를 구분해 기록. |

TC-A는 스키마만 반영한 상태로 완료 보고하지 않는다. 형식·연결·에셋이 함께 들어가야 한다. TC-B는 별도 인수 지점이며 TC-A 완료가 TC-B 통과를 뜻하지 않는다.

최종 완료는 두 카드의 예외 해소, 실제 값 표시, 구매 전후 표시 일관성, 기본/강화 설명 구분, 글자 잘림·겹침 없음, 기존 숫자 카드 회귀 통과를 모두 만족한 때다. 재방문에서 3등급 후보가 없는지도 기존 후보 경로의 통합 확인에 포함한다.

새 시험 코드를 작성하지 않고 MSBuild를 실행하지 않는다. 컴파일·재생은 사용자 Unity 확인으로 남긴다. View 에셋 변경이 있으므로 기존 `DataBaseAssetTests`도 사용자 확인 항목에 포함하되 시험을 새로 쓰거나 임의 수정하지 않는다.

긴 문구가 기존 카드에 들어가지 않으면 TC-B 미완료로 남긴다. 숫자·조건을 생략하거나 자동 글자 축소를 임의로 더 낮춰 통과시키지 않는다. 프리팹/레이아웃 변경은 별도 설계 수정·사용자 결정 뒤 진행한다.

## 8. 역할·현재 상태

- 설계 작성 요청의 산출물은 이 설계와 연결된 Task 계획 두 문서다. 이후 사용자의 이 세션 TC-A 직접 진행 요청 범위에서 Codex가 §5의 코드·에셋을 반영했다.
- 일반 역할은 C# 구현 Claude, 검수와 승인된 표현 에셋 구성 Codex, Phase 인수·커밋·push 사용자다. 이번 TC-A 직접 실행을 다른 Phase나 Core 수정 권한으로 확대하지 않는다.
- 이번 작업으로 Core 전달 문서는 수정하지 않는다. 새 Core 의존성이나 계약 부족이 발견되면 구현을 넓히기 전에 사용자에게 알린다.
- TC-A 정적 검수 완료: 별도 읽기 전용 검수에서 수정이 필요한 문제 없음. 기존 숫자 계약·구매 전 스냅샷 경로·새 GUID·UTF-8/LF·변경 범위를 확인했다. 상세 증거는 계획의 실행 기록에 있다.
- Unity 검증·외부 전달·커밋·push는 수행하지 않았다. 컴파일 미확인.
