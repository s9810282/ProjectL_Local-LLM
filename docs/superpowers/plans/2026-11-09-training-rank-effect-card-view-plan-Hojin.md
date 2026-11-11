# 희생·먼지 연막 훈련 카드 효과 표시 Task 계획

> **For agentic workers:** REQUIRED SUB-SKILL: 구현자는 `superpowers:executing-plans`를 적용해 Task 순서대로 진행한다. 프로젝트의 작성자 역할·Phase 승인·검증 제한이 우선한다. 체크박스는 실제 수행 증거가 있을 때만 완료한다.

**Goal:** 희생·먼지 연막 훈련 카드가 Core의 현재·다음 숫자와 bool 효과를 정확히 보여 주고 기존 숫자 강화 카드를 보존한다.

**Architecture:** 기존 카드 조립에서 값 변환을 분리한다. 효과 표시 등록표가 공개 bool 선택과 숫자 줄 사용 여부만 정하고, 변환기는 받은 스냅샷과 View 문구로 문자열을 만든다. 구매·후보·화면 수명은 기존 경로를 사용한다.

**Tech Stack:** Unity 6000.0.73f1, C#, ScriptableObject, 기존 uGUI/TextMeshPro 카드.

**Spec:** [훈련 카드 효과 표시 설계](../specs/2026-11-09-training-rank-effect-card-view-design-Hojin.md). 구현자는 설계와 이 계획을 함께 읽는다.

**상태:** 2026-11-09 TC-A 구현·정적 검수 및 TC-B 사용자 Unity 확인·인수 완료. 사용자가 남은 확인 항목 전체를 확인하고 작업 종료를 요청했다. 상세 결과와 확인 출처는 §5를 따른다.

**별도 후속 정리:** 2026-11-09 사용자 승인으로 진행한 View 미사용 코드 3묶음 정리는 구현 및 사용자 Unity 확인 완료. 상점·고용소·훈련소 모두 오류 없이 정상 동작했다고 사용자 확인을 받았다. 상세 기록은 §5를 따른다.

## 공통 제약

- 범위: 훈련 카드 문구 데이터, 값 변환, 카드 연결, 운영 View 에셋 3개와 해당 표시의 검증.
- 현재·다음 효과는 `offer.CurrentValues`·`offer.NextValues`에서 각각 읽는다.
- 기존 `rankValueFormat` 이름과 6자리 계약은 유지한다.
- Core 규칙·계약·InGame 에셋, 구매 명령·가격 정책, UI 등록·수명·입력, 공용 카드 프리팹은 이번 구현 범위에 넣지 않는다.
- 새 시험 코드를 작성하지 않고 MSBuild를 실행하지 않는다. 컴파일·재생은 사용자 Unity 확인으로 남긴다.
- 각 Phase가 끝나면 보고하고 멈춘다. 다음 Phase 승인을 추정하지 않는다.
- 일반 역할은 C# 구현 Claude, 검수와 승인된 표현 에셋 구성 Codex, Phase 인수·커밋·push 사용자다. 이번 TC-A는 사용자의 이 세션 직접 진행 요청 범위에서 Codex가 수행한다.
- Allman, 공백 4칸, 블록 네임스페이스, UTF-8/LF, 한국어 주석. 새 `.cs`와 `.meta`를 함께 관리하고 기존 GUID를 유지한다.
- 다른 작업자의 변경을 덮어쓰거나 stash/reset/clean하지 않는다. 설계와 다른 결정은 구현 전에 사용자에게 확인한다.

## Review Focus

| ID | 놓치기 쉬운 조건 | 소유 Task·확인 |
|---|---|---|
| R1 | 먼지 연막은 bool만 변하고 숫자는 0 → 0이다. 숫자 누락을 예외로 보거나 0 줄을 그리지 않아야 한다. | A2 정적 대조, B1 U2 |
| R2 | 희생의 bool과 보호막은 독립 값이다. 등급·고정값으로 대체하거나 하나를 누락하지 않아야 한다. | A2 값 흐름 대조, B1 U1 |
| R3 | 구매 뒤 후보가 사라져도 구매 직전 값으로 완료 카드를 그려야 한다. | A2 호출 경로 확인, B1 U4 |
| R4 | 키 불일치·문구/스냅샷 누락·잘못된 형식을 정상 빈 표시로 감추면 안 된다. | A1/A2 검증 분기 검수, B1 U7에서 부트·표시 오류 기록 |
| R5 | 효과 문구와 기본 설명이 함께 길어지며 공통 형식 변경은 기존 숫자 카드에도 적용된다. | A3 에셋 대조, B1 U3/U6 |

자동 시험 대신 정적 계약 대조와 사용자 Unity 확인으로 검증한다. 확인을 위해 가짜 스냅샷 실행기·시험 도우미·개발 fixture를 새로 만들거나 운영 Core 값을 바꾸지 않는다. 검수만 한 오류 경로는 실행 검증으로 기록하지 않는다.

## 1. 구현 전 읽기와 파일 지도

작업 루트가 `C:/Users/Hyeon2-400/Project_L`이면 `C:/Users/Hyeon2-400/ProjectL-Hojin/rules/hojin-rules.md`를 먼저 읽는다. 이어서 루트·`Assets/AGENTS.md`, 위 설계, [Core 인계](../handoffs/2026-11-09-sacrifice-dust-screen-upgrades-core-view-handoff.md)를 읽는다.

아래 별칭은 이 문서의 정확한 파일 경로를 짧게 적기 위한 것이다. 네임스페이스·서비스 키·새 디렉터리를 뜻하지 않는다.

| 별칭 | 저장소 루트 기준 경로 |
|---|---|
| Shop | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop` |
| Trait | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item` |
| Text | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageOfferText/Item` |
| Sacrifice | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Heavy/TraitVisual_Sacrifice.asset` |
| DustScreen | `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/Rider/TraitVisual_DustScreen.asset` |
| TrainingText | `Assets/ProjectL/DataBaseSO/ViewGroup/StageOfferText/Item/StageOfferText_Training.asset` |

새 파일 2개와 각각의 `.meta`, 기존 코드 5개, 운영 View 에셋 3개가 구현 대상이다. 새 helper의 책임·전체 경로는 설계 §5에 있다. `ShopPopupModelBuilder`, `ShopPopupViewFactory`, 카드 프리팹은 읽기 확인 대상이다.

착수 시 `git status --short`와 대상별 `git diff -- <경로>`를 확인한다. 문서 작성 시 이미 있던 Audio/FMOD 및 별도 StageSelect 평가 문서의 수정은 이 계획의 산출물이 아니다. 구현 시점의 변경을 다시 확인하며 이 기록을 현재 상태로 단정하지 않는다.

## 2. Phase TC-A — 코드·에셋 연결과 정적 검수

설계·구현 승인 후 A1 → A2 → A3 순서로 진행한다. A1의 문구 계약만 추가한 중간 상태를 카드 수정 완료로 보고하지 않는다. 이 Phase가 끝나면 Unity 검증 대기로 보고하고 멈춘다.

### Task A1. 숫자·효과 문구 계약 확장

**Files:** 수정 `Trait/TraitVisualDataSO.cs`, `Text/StageOfferLineKind.cs`, `Text/StageOfferTextData.cs`, `Text/StageOfferTextDataSO.cs`, `TrainingText`.

**Interfaces:** 기존 `RankValueFormat` 유지. 새 `internal string RankEffectFormat` 제공. `StageOfferLineKind.TrainingEffectEnabled = 16`, `TrainingEffectDisabled = 17`과 인자 수 0인 `FormatLine` 계약을 제공한다.

- [x] A1.1 대상 diff와 enum 현재 값을 확인한다. 16·17이 다른 의미로 사용 중이면 적용하지 말고 설계 충돌을 보고한다.
- [x] A1.2 `TraitVisualData`에 `rankEffectFormat`, `RankEffectFormat`, `ValidateRankEffectFormat()`을 추가하고 `Validate()`에서 호출한다. 기존 숫자 6자리 검사와 저장 필드명은 유지한다.
- [x] A1.3 새 검사에 비어 있는 형식의 허용, 두 상태 자리의 실제 사용, 잘못된 형식의 문맥 있는 예외를 넣는다. `{0}` 또는 `{1}` 누락, `{2}`, 닫히지 않은 중괄호, `{{0}}`만 쓴 문장을 코드 대조로 검수한다(R4). 실행하지 않은 사례는 정적 확인으로 기록한다.
- [x] A1.4 두 enum 항목과 `ValueCountOf`의 0 인자 분류를 추가한다. Inspector 툴팁에 적용/미적용 줄, 효과 형식의 2자리, 숫자 형식이 필요한 카드의 조건, TrainingDetails의 첫 인자가 효과·수치 전체라는 뜻을 적는다.
- [x] A1.5 TrainingText에 `적용`·`미적용` 줄을 추가하고 `TrainingDetails`를 설계 TC4의 형식으로 바꾼다. 기존 stageKeys·가격·버튼·오류 문구를 유지한다.
- [x] A1.6 제공된 새 enum 값과 에셋 kind가 일치하고, 기존 모든 `ValueCountOf` 분류가 유지되는지 정적으로 확인한다. 다른 StageOfferText 에셋에 새 낱말을 일괄 추가하지 않는다.

**완료 증거:** 스키마·형식 검사 diff와 훈련 텍스트 에셋의 정확한 값. 이 Task 단독으로 두 특성 카드의 예외가 해소됐다고 보고하지 않는다.

### Task A2. 등록표·변환기와 카드 연결

**Files:** 추가 `Shop/TrainingTraitValueBindings.cs`·`.meta`, `Shop/TrainingTraitValueFormatter.cs`·`.meta`; 수정 `Shop/TrainingCardContentBuilder.cs`, `Sacrifice`, `DustScreen`.

**Consumes:** A1의 `TraitVisualData.RankEffectFormat`과 두 상태 낱말. 기존 `TraitRankValues`, `TrainingChoiceSnapshot`.

**Produces:**

- `TrainingTraitValueBindings.TryGet(TraitKey key, out TrainingTraitValueBinding binding)`
- 같은 파일의 `internal readonly struct TrainingTraitValueBinding`: `bool ShowAmount`, `Func<TraitRankValues, bool> ReadEffect`, 두 값을 받는 생성자.
- `TrainingTraitValueFormatter.Format(TraitVisualData visual, TrainingChoiceSnapshot offer, StageOfferTextData text) : string`

- [x] A2.1 설계 TC2의 두 항목을 읽기 전용 등록표에 넣는다. 희생은 숫자+사망 방지, 먼지 연막은 숫자 없음+같은 진영 면제다. 숫자·등급·가격을 등록표에 저장하지 않는다.
- [x] A2.2 변환기에 인자·스냅샷·키 일치 검사를 먼저 넣는다. `visual.Key`, `offer.TraitKey`, 양쪽 `TraitRankValues.Key`가 일치해야 한다. null 인자는 `ArgumentNullException`, 구성·상태 오류는 문맥 있는 `InvalidOperationException`을 사용한다.
- [x] A2.3 숫자 전용 기본 경로와 등록된 효과 경로를 구현한다. 숫자는 기존 6자리, 효과는 현재·다음 bool 각각으로 공통 낱말을 선택한다. 두 항목이 있으면 효과 → 숫자 순서로 LF 하나를 넣는다.
- [x] A2.4 TC5의 문구 누락·지원되지 않는 조합·형식 오류를 검사한다. 상태 낱말은 사용하는 경로에서 필수로 검사한다. 오류를 빈 문자열이나 영문 bool fallback으로 바꾸지 않는다.
- [x] A2.5 `TrainingCardContentBuilder.Trait`의 `ValueLine` 호출을 `TrainingTraitValueFormatter.Format(visual, offer, text)`로 대체하고 기존 `ValueLine` 중복을 제거한다. 이름·가격·등급·버튼은 기존 조립 경로를 유지한다.
- [x] A2.6 희생·먼지 연막의 형식을 설계 TC4와 정확히 맞춘다. 기존 Description·TrailDescription·아이콘·발동 연출·항상 효과·프리팹·GUID는 건드리지 않는다.
- [x] A2.7 R1/R2/R4를 정적 대조한다: 희생 현재 0·다음 10, false/true를 각각 읽는지; 먼지 연막은 0/0을 출력하지 않는지; bool이 false/false 또는 true/false여도 현재·다음 값을 독립적으로 읽는지; 기존 단일/이중 수치가 같은 자리에 들어가는지 확인한다.
- [x] A2.8 `ShopPopupModelBuilder.BuildTraits`에서 `TrainingPurchase` → 같은 `Trait` 호출을 추적해 구매 후에도 동일 변환기가 구매 전 스냅샷을 쓰는지 확인한다(R3). 이 확인을 위해 구매 컨트롤러·Buffer를 수정하지 않는다.
- [x] A2.9 새 `.meta`의 GUID 유일성과 namespace 경로, 정적 변환기의 서비스/Factory 등록 추가가 없음을 확인한다.

**완료 증거:** 코드·에셋 diff, 숫자/효과/구매 후 값의 데이터 경로 대조. 실행 검증·컴파일 완료 주장은 하지 않는다.

### Task A3. 영향 검수와 Phase 보고

**Files:** 구현 대상 전체, 이 계획의 실행 기록. 설계 변경이 필요한 경우 사용자 확인 뒤 설계도 함께 갱신한다.

**Interfaces:** 새로운 동작 없음. A1/A2가 설계 TC1~TC6과 일치하는지 검수한다.

- [x] A3.1 변경 파일 목록을 설계 §5와 대조한다. Core·InGame 에셋·시험 코드·다른 작업자 파일·공용 프리팹에 이 작업의 변경이 없어야 한다.
- [x] A3.2 기존 숫자 형식의 자리와 에셋이 보존됐는지 확인한다. 대표는 PositionSwap(한 값), Cohesion·TrackingShot(두 값)이다. 새 공통 Details 형식 때문에 이 카드들도 B1에서 확인한다(R5).
- [x] A3.3 설계 §6의 SSOT·SOLID 5원칙·FW·독립성 표를 실제 diff와 대조한다. `ShopPopupViewFactory` 생성자 경로, 서비스 등록/상위 Export 추가 없음, 기존 UI 등록·입력 경로 유지를 기록한다.
- [x] A3.4 대상별 `git diff --check -- <변경 경로>`와 `git diff --stat -- <변경 경로>`를 확인한다. 새 미추적 파일은 따로 내용을 검수하고 UTF-8/LF·메타 쌍을 확인한다. 저장소 전체 변경을 스테이징하지 않는다.
- [x] A3.5 문서 링크·Task 대응·수정 범위를 확인하고 실행 기록에 변경 파일, 정적 검수 결과, 남은 Unity 항목을 남긴다.
- [x] A3.6 **"TC-A 코드·에셋 연결 완료, 컴파일 미확인, TC-B 사용자 Unity 확인 대기"**로 보고하고 멈춘다. 커밋·push하지 않는다.

## 3. Phase TC-B — 사용자 Unity 확인과 인수

### Task B1. 실제 카드·구매·회귀 확인

**Files:** 이 계획의 실행 기록만 갱신한다. 구현 결함 수정이 필요하면 승인된 범위인지 먼저 확인하고 Phase 결과에 실패 사실을 남긴다.

**담당:** 사용자 Unity 확인, 결과를 받은 작성자/검수자가 문서에 기록. 미수신은 미확인이다.

준비: 현재 운영 설정에서 희생 또는 먼지 연막을 가진 훈련 가능 유닛으로 방문한다. 두 특성의 rank2 연결을 Inspector에서 확인하되, 검증 편의를 위해 Core 에셋·경제·훈련 후보를 코드로 바꾸지 않는다. 대상 유닛/방문을 준비할 수 없으면 해당 항목은 미확인으로 남긴다.

| ID | 사용자 확인 | 기대 결과 |
|---|---|---|
| U1 | 희생 훈련 상세 열기 | 예외 없음. 사망 방지 `미적용 → 적용`, 피해 처리 후 보호막 `0 → 10 (+10)` 모두 표시. 일반 피해까지 죽지 않는다는 문구가 없어야 한다. |
| U2 | 먼지 연막 훈련 상세 열기 | 예외 없음. 연막 생성자와 같은 진영의 **공격 사거리** 감소 면제 표시. 불필요한 0/0 숫자·빈 줄 없음. 피해 감소 면제로 잘못 표현하지 않음. |
| U3 | U1/U2 및 숫자 카드의 전체 본문 읽기 | 강화 내용과 기본 효과가 구분되고 긴 조건·숫자·화살표가 잘리거나 버튼/가격과 겹치지 않는다. 실제 인스턴스 크기·폰트·화면 배율을 함께 기록한다. |
| U4 | 각 강화 구매 후 화면 확인 | 구매 완료 카드에 같은 구매 전/후 변화가 유지된다. `2 → 3`으로 바뀌지 않고 다른 카드는 기존 흐름대로 잠긴다. |
| U5 | 구매 후 다음 훈련 방문 | 해당 특성의 3등급 후보 없음. View에서 별도 등급 필터를 추가하지 않아도 Core 후보 목록대로 표시한다. |
| U6 | PositionSwap 및 Cohesion 또는 TrackingShot 훈련 카드 | 단일·이중 숫자의 현재/증가/다음 값, 가격·등급·구매 버튼, 기본 설명이 정상이다. 유닛 선택·뒤로·재열기도 기존 흐름대로 동작한다. |
| U7 | Unity 컴파일·DB 초기화·기존 `DataBaseAssetTests` | 결과를 사용자가 확인한다. 새 시험 작성·임의 시험 수정 없음. 실패는 실제 메시지와 이 작업 관련 여부를 구분한다. |

- [x] B1.1 U1~U7의 실제 결과·확인자·날짜를 실행 기록에 적는다. 미실행 항목은 미확인으로 남긴다.
- [x] B1.2 문구 잘림 또는 오류가 있으면 재현 대상과 실제 표시를 기록한다. 설계의 필수 조건/숫자를 빼서 통과시키지 않는다. 프리팹 변경이 필요하면 사용자에게 설계 변경을 먼저 요청한다.
- [x] B1.3 필수 확인이 모두 끝났을 때만 훈련 카드 수정의 인수를 기록한다. 컴파일 결과를 받기 전에는 `컴파일 미확인`을 유지한다.
- [x] B1.4 후속인 보유·전투 설명/연막 설명/희생 연출이 이 Phase 완료에 포함되지 않음을 기록하고 멈춘다.

## 4. 설계 대응과 계획 자체 검수

| 설계 | 구현/검증 Task |
|---|---|
| TC1 원본·숫자 계약 | A2.2~A2.3, A2.7, U1/U2/U6 |
| TC2 표시 등록 | A2.1, A2.4, A3.3 |
| TC3 문구 계약 | A1.2~A1.6 |
| TC4 실제 문구·기본 설명 | A1.5, A2.6, U1~U3 |
| TC5 오류 | A1.3, A2.2/A2.4/A2.7, U7 |
| TC6 카드·구매 후 | A2.5/A2.8, U4/U5 |
| §6 원칙·독립성 | A3.1~A3.3 |
| §7 검증·Phase·레이아웃 | A3.6, B1 전체 |

계획 자체 검수: 설계 요구의 Task 대응, 메서드/필드명 일치, Review Focus 5건의 정적·Unity 확인 위치, 금지된 시험 작성·MSBuild·커밋 단계가 없는지를 확인한다. 이는 제품 실행 검증과 구분한다.

## 5. 실행 기록

### 2026-11-09 TC-B 사용자 전체 확인·인수 및 작업 종료

- 확인자·출처: 사용자. 남은 TC-B 항목 U1~U7을 순서대로 안내한 뒤 "오케이 다 확인했어 이거는 여기서 끝내자"라는 전체 확인 및 종료 요청을 받았다.

| 항목 | 사용자 확인 결과 |
|---|---|
| U1·U2 | 희생의 사망 방지·보호막 변화와 먼지 연막의 같은 진영 공격 사거리 감소 면제 문구 확인 완료. |
| U3 | 강화 내용·기본 효과 구분 및 문구 잘림·겹침 확인 완료. 해상도·배율·폰트의 구체 값은 별도 전달받지 않았다. |
| U4 | 구매 완료 카드의 구매 전후 변화와 `1 → 2등급` 유지 확인 완료. |
| U5 | 다음 훈련소 방문에서 두 특성의 3등급 제안이 나오지 않는지 확인 완료. |
| U6 | 기존 단일·이중 숫자형 특성 카드의 수치·가격·등급·설명 회귀 확인 완료. |
| U7 | 앞서 받은 오류 없는 Unity 재생 보고와 이번 전체 확인 응답에 따라 컴파일·DB 초기화·기존 `DataBaseAssetTests` 확인 완료로 인수했다. 개별 시험 로그·실행 건수는 전달받지 않았다. |

- 검증 구분: 위 결과는 사용자 보고에 따른 인수다. Codex는 Unity·컴파일·MSBuild·시험을 직접 실행하지 않았다(자체 컴파일 미확인).
- 종료 범위: **훈련 카드 TC-A·TC-B와 View 미사용 코드 3묶음 정리 완료.** 보유·전투 설명, 연막 설명, 희생의 HP·보호막 연출은 별도 후속 범위이며 이번 확인에 포함하거나 자동 착수하지 않는다.
- 종료 처리: 계획의 B1 체크박스와 설계의 현재 상태를 갱신했다. 게임 코드·에셋 추가 변경, 커밋·push 없이 이번 작업을 종료한다.

### 2026-11-09 View 미사용 코드 3묶음 정리 — 사용자 Unity 확인 완료

- 승인 범위: 사용자 "Core는 건드리지 말고, 방금 확인한 View 쪽 미사용 3묶음만 정리해줘" 지시에 따른 별도 후속 정리다.
- 정리 1: `EnhancementShopOfferContentBuilder.cs`와 `.meta`, `ShopPopupViewFactory`의 해당 생성기 등록을 제거했다.
- 정리 2: View의 `EnhancementDetails` 문구 종류·형식 검증·툴팁과 `StageOfferText_Shop`·`StageOfferText_Mercenary`·`StageOfferText_Training`의 `lines` 안 `kind: 3` 항목을 제거했다. 문구 번호 3은 비워 두고 나머지 enum 값과 `errors` 항목은 유지했다.
- 정리 3: `ShopResultPolicy`에서 발생처가 없는 `AlreadyEnhanced`·`InvalidEnhancement`·`UnsupportedEnhancementLevel` 처리 분기만 제거했다.
- 보존 범위: Core·Buffer·InGame DB, 레벨업의 `TraitEnhanceLabel`, 기존 TC-A 훈련 카드 문구와 적용·미적용 표시를 유지했다.
- 구현 시 정적 확인: 승인 파일 2개 삭제·8개 수정만 적용됐음을 작업 전후 대조했다. 삭제 대상의 코드·에셋 참조가 남지 않았고 대상 `git diff --check`가 통과했다.
- 사용자 확인(2026-11-09): "Unity에서 상점·고용소·훈련소 다 확인했고 오류 없이 잘 돼. 이번 View 미사용 코드 정리 확인 완료로 기록해줘."
- 결과: **상점·고용소·훈련소 정상 동작 및 오류 없음 — 사용자 Unity 확인 완료. 이번 View 미사용 코드 정리 인수 완료.**
- 당시 검증 구분: Codex는 Unity·컴파일·MSBuild·시험을 직접 실행하지 않았다(자체 컴파일 미확인). 이 시점에는 `DataBaseAssetTests`의 별도 실행 결과가 미수신이어서 TC-B 체크박스를 유지했다. 이후의 전체 확인·인수는 위 TC-B 종료 기록을 따른다.
- 이번 기록 요청에서는 이 문서만 갱신했다. 게임 코드·에셋 추가 변경과 커밋·push는 하지 않았다.

### 2026-11-09 TC-A 코드·에셋 연결 및 정적 검수 완료

- A1: `rankEffectFormat`·접근자·DB 생성 형식 검사를 추가했다. 검사는 실제 두 자리 치환 여부를 확인하며 숫자 형식의 6자리 계약은 유지했다. 적용/미적용 enum은 16/17, 인자 수는 각각 0이다. 훈련 텍스트 에셋에 두 낱말과 강화/기본 구분을 연결했다.
- A2: 표시 등록표와 변환기 2개 및 `.meta`를 추가하고 `TrainingCardContentBuilder.ValueLine`을 변환기 호출로 대체했다. 희생은 두 bool 스냅샷·보호막 수치를 독립적으로 읽고, 먼지 연막은 숫자 줄 없이 bool 효과만 표시한다. 운영 문구는 설계 TC4와 일치한다.
- A2 검수: null·키 불일치·필수 형식/낱말 누락·지원하지 않는 조합의 실패 경로를 코드로 대조했다. `{0}`/`{1}` 누락·이스케이프 자리·잘못된 형식, false/false·true/false, 0 값도 정적으로 검토했다. 실행 검증은 하지 않았다.
- 구매 후 경로: 기존 `ShopPopupModelBuilder.BuildTraits`의 `TrainingPurchase`가 같은 `Trait` → formatter로 전달된다. 구매·Buffer·후보 필터 변경 없음.
- A3: 별도 읽기 전용 검수에서 Critical/Important/Minor 지적 없음. SSOT·SOLID 5원칙·JMH/FW·조립과 기능 분리를 대조했다. `TraitVisualTable`의 DB 생성 검증 연결과 Factory → ModelBuilder → CardBuilder → formatter 경로를 확인했다. 새 서비스 등록·Export·resolver 조회 없음.
- 정적 증거: 대상 `git diff --check` 오류 없음. 새 GUID 2개는 각각 해당 `.meta`에서만 1회 검색됐다. 대상 14개(코드·에셋·메타·문서)의 UTF-8/LF·대체 문자·메타 존재를 검사했고 기존 대상 8개의 CRLF만 LF로 맞췄다. 기존 숫자 카드 에셋과 Core·InGame 에셋·시험·프리팹·Factory의 diff 없음.
- 작업 전부터 수정돼 있던 Audio/FMOD·다른 StageSelect 문서 4개의 SHA-256을 다시 비교해 동일함을 확인했다. 해당 변경을 덮어쓰지 않았다.
- 검수 유보 판정: 실제 글자 잘림/겹침·구매/재방문·컴파일/DB 초기화는 TC-B 확인이다. Core 규칙·보유/전투 설명·VFX·공용 프리팹은 별도 후속 범위다. 현재 Phase에서 확인했다고 주장하거나 자동으로 구현 범위를 넓히지 않는다.
- 종료: TC-A 코드·에셋 연결 완료. **컴파일 미확인**, Unity·MSBuild·시험 실행 없음. TC-B 사용자 확인 대기에서 멈춘다. 커밋·push하지 않았다.

### 2026-11-09 TC-A 착수

- 사용자 지시: "그럼 설계대로 TC-A부터 진행해줘." 이번 직접 진행 요청 범위의 코드·에셋 연결은 이 Codex 세션에서 수행한다. 일반 역할 규칙이나 TC-B 진행 권한으로 확대하지 않는다.
- 착수 상태: `feature/Hojin/11_03_StageSelect`, 적용 루트 `C:/Users/Hyeon2-400/Project_L`. 대상 기존 코드·에셋의 작업 전 diff 없음. Audio/FMOD와 다른 StageSelect 문서의 기존 변경은 보존한다.
- 실행 방식: 승인된 로컬 작업 경로와 계획의 검증 제약을 우선한다. 별도 checkout·시험 코드·MSBuild·커밋은 만들거나 실행하지 않는다. 진행 기록은 이 문서에 남긴다.
- 사전 인터페이스 대조: A1의 `RankEffectFormat`·상태 줄 16/17을 A2 변환기가 사용하고, A2의 `Format(visual, offer, text)`를 기존 카드 조립이 호출한다. 인자·이름·enum 충돌 없음.
- A1.1: enum의 기존 마지막 값은 15이며 16·17은 미사용이다.

### 2026-11-09 설계·계획 초안 작성

- 사용자 요청: 훈련 카드 수정을 완성할 수 있는 설계와 Task 계획 작성.
- 수행: 현재 Core 인계·표시 코드·View 문구 에셋·공용 카드 크기·작업 규칙을 읽고 새 문서 2개 작성.
- 문서 자체 검수: 설계 TC1~TC6의 Task 대응과 타입·필드명, Review Focus 5건의 확인 위치를 대조했다. 로컬 링크 8개가 모두 존재하고 두 문서의 UTF-8/LF·행 끝 공백·대체 문자를 검사해 문제 없음을 확인했다. 구현 체크박스 25개는 모두 미완료 상태다.
- 상태: 구현 Task 미착수. 게임 코드·에셋·시험 코드 미변경. Unity·MSBuild 미실행, 컴파일 미확인.
- 다음 검토 지점: 설계의 문구·필드 계약·TC-A 범위. 승인 뒤 기존 작성자 역할에 맞춰 구현한다.
