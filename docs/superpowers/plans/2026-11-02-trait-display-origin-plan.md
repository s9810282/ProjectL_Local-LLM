# 특성 표시 순서·출처 공개 Task 계획 (2026-11-02, 코드 착수 전)

> 설계: [trait-display-origin-design](../specs/2026-11-02-trait-display-origin-design.md)(TO1~TO6). 결정: Q-TO1~Q-TO4 모두 A(2026-11-02 사용자).
> 기준 판: `06835a862`. Phase마다 멈추고 보고한다. 커밋·push는 사용자가 한다.
> 검증: Unity Test Runner(`ProjectL.Tests.Core`, `ProjectL.Tests.CoreIntegration`). MSBuild는 돌리지 않는다. 결과는 LocalLow의 Test Runner XML을 읽는다.

## 0. 계획 전에 확인한 것

- **P4 확인됨:** DB 특성 순서는 `UnitSpecAdapter.ReadUnit`(`CharacterSystem/UnitSpecAdapter.cs:23-27`)과 `UnitSpec` 생성자에서 정렬 없이 그대로 복사된다. 정렬은 `UnitCatalog.cs:103`, `UnitDefinition.cs:92`, `TraitSettingsSet.cs:34` 셋뿐이다. → 유닛 기본 묶음 = DB 유닛 SO의 특성 목록 순서.
- **P5:** `BattleTraitSettings`는 "한 전투 동안 바뀌지 않는다"가 계약이다(`Contracts/Combat/BattleTraitSettings.cs:10`). 성장은 전투 중 사용 불가. 구현 중 특성 목록을 전투 중에 바꾸는 경로가 보이면 멈추고 보고한다.
- **시험 작성자:** 호출을 맞출 시험 4개(`TraitSettingsSetTests`, `OwnedCharacterStatsCalculatorTests`, `ImpactBoltAuthoringTests`, `BattleTraitSettingsPublicationTests`)는 모두 천명님 작성이다. Core 쪽에서 바로 고친다.

## 1. 계획에서 정한 것 (확인 대상)

- **TP-1** `TraitDisplayEntry`는 `sealed class`(다른 스냅샷 꼴). 값 비교 `Same(other)` 하나.
- **TP-2** `TraitSettingsSet` 새 생성자는 `IEnumerable<TraitDisplayEntry> display`와 `IEnumerable<TraitSpec> traits`를 함께 받지 않고, 항목 하나에 둘을 묶은 `IEnumerable<(TraitSpec Spec, TraitOrigin Origin)>` 하나만 받는다. 키 집합이 어긋날 여지를 생성자 서명에서 없앤다.
- **TP-3** `TraitSettingsSet.Empty`는 그대로(빈 표시).
- **TP-4** `Compose`의 표시 출력은 기존 `ranks`처럼 선택 인자 `List<TraitDisplayEntry> display`. 공개 입구 `Resolve(basis, owned, catalog)`·`ResolveBase`의 서명은 바꾸지 않고, 표시가 필요한 두 곳(`BuildTraitSettings`, `OwnedCharacterStatsCalculator`)이 쓰는 internal 입구에서만 받는다.
- **TP-5** 출처 판정 함수는 `Compose` 안 지역 함수 하나(`OriginOf(key)`). 병과 고유 키는 `benefit?.TraitKey`(현재 전투 병과 기준, 기존 변수 그대로).
- **TP-6** 표시 순서 정렬은 안정 정렬(묶음 순서만 비교, 같은 묶음은 합성 목록 순서). 합성 목록은 이미 유닛 기본 → (병과) → 획득 순으로 쌓이지만, Q-TO2·Q-TO3 때문에 병과 고유가 앞쪽 자리(유닛 기본 자리나 획득 자리)에 있을 수 있어 정렬이 필요하다.
- **TP-7** `OwnedCharacterStatsCalculator`는 이미 `Resolve(basis, record, catalog, ranks)`를 부른다. 같은 호출에 `display`를 더한다.

## 2. Phase TO-a 값 (계산·화면 변화 없음)

| Task | 내용 |
|---|---|
| Ta.1 | 새 `Contracts/Common/TraitOrigin.cs`(+ `.meta`): `None = 0, Unit = 1, Category = 2, Growth = 3`, 한국어 요약 주석 |
| Ta.2 | 새 `Contracts/Snapshots/Traits/TraitDisplayEntry.cs`(+ `.meta`): `Key`·`Origin`, `None`·정의 밖 값 거절(`System.Enum.IsDefined`), `Same` |
| Ta.3 | `TraitSettingsSet`: 생성자를 `(TraitSpec Spec, TraitOrigin Origin)` 목록으로 바꿈. `Display`(받은 순서)·`Traits`(키 순) 둘을 만듦. 검사: null·키 중복·출처 `None`·병과 고유 2개 이상 거절. `SameValues`는 설정 값만(주석에 명시). 클래스 요약에 `Display` 설명 |
| Ta.4 | `TraitSettingsSet.From(UnitSpec)` → `From(UnitSpec unit, IReadOnlyList<TraitDisplayEntry> display)`. 키 집합이 `unit.Traits`와 다르면 `InvalidOperationException`. 표시 순서대로 `(spec, origin)`을 묶어 생성자에 넘김 |
| Ta.5 | 호출 맞춤(임시로 모두 `Unit` 출처): `OwnedUnitSpecResolver.BuildTraitSettings` 두 줄, `OwnedCharacterStatsCalculator.cs:119`. TO-b에서 실제 출처로 바꿈 |
| Ta.6 | 시험 호출 맞춤: `TraitSettingsSetTests`(생성 7곳), `OwnedCharacterStatsCalculatorTests:103`, `ImpactBoltAuthoringTests:222`, `BattleTraitSettingsPublicationTests:31`. 확인 내용은 그대로 |
| Ta.7 | `TraitSettingsSetTests`에 새 사례: `Display`는 받은 순서·`Traits`는 키 순, 출처 `None` 거절, 병과 고유 2개 거절, `SameValues`가 출처를 보지 않음 |

**Ua(사용자):** Test Runner `ProjectL.Tests.Core` 전체 + `CoreIntegration`의 `BattleTraitSettingsPublicationTests`·`ImpactBoltAuthoringTests`. 기대: 기존 결과와 같음 + 새 사례 통과.

### 2.1 TO-a 실행 기록 (2026-11-02, 코드 완료)

- 사용자 지시로 Claude가 Unity batchmode로 두 어셈블리를 돌렸다(`-runTests -testPlatform EditMode -assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration"`).
- 기준선(코드 변경 전): 5,492개 중 실패 25(Core 1, CoreIntegration 24).
- TO-a 뒤: 5,495개 중 실패 25. **새 실패 0, 고쳐진 것 0**(실패 목록이 기준선과 같음). 새 사례 3개 통과. 바꾼 시험 다섯 클래스의 사례 68개 모두 통과.
- 계획과 다른 점: Ta.6 호출 맞춤에 `BattleTraitSettingsTests.cs:62`(천명님, 대상 형식 `new(traits)`)를 더했다. 계획 때 `new TraitSettingsSet(` 검색만 해서 놓쳤고, 첫 실행이 컴파일 오류로 멈춰 찾았다.
- 임시 도우미 `OwnedUnitSpecResolver.UnitOriginDisplay`(명세 순서, 모두 `Unit`)는 TO-b에서 지운다.
- 실행 전후 git 상태 같음(Unity가 바꾼 파일 없음).

## 3. Phase TO-b 출처·순서 생산

| Task | 내용 |
|---|---|
| Tb.1 | `Compose`에 `List<TraitDisplayEntry> display = null` 출력. 성장 선택에서 Acquire한 키 집합을 모음(기존 반복문 안 한 줄) |
| Tb.2 | 최종 `traits` 확정 뒤 `OriginOf`: 병과 고유 키면 `Category` → Acquire 키면 `Growth` → 그 밖 `Unit`. 묶음 순서(Unit, Category, Growth)로 안정 정렬해 `display`에 채움. `!changed`로 `basis`를 돌려주는 길에서도 채움 |
| Tb.3 | internal 입구: `ResolveBase(basis, catalog, display)`, `Resolve(basis, owned, catalog, ranks, display)`. 공개 입구 서명 그대로 |
| Tb.4 | `BuildTraitSettings`가 두 입구로 표시를 받아 `TraitSettingsSet.From(spec, display)` |
| Tb.5 | `OwnedCharacterStatsCalculator.cs:86`·`:119`가 같은 `display`를 씀 |
| Tb.6 | 새 Core 시험(새 파일 `TraitDisplayOriginTests.cs` + `.meta`, `ProjectL.Tests.Core`): 설계 §8 사례 일곱. 고정물은 기존 `OwnedUnitSpecResolver` 시험 꼴을 따름 |
| Tb.7 | `CoreIntegration` 새 사례 하나(기존 `BattleTraitSettingsPublicationTests`에 추가): 운영 DB의 Rider·Heavy 유닛에서 전투 `BattleTraitSettings.For`와 전투 밖 `OwnedCharacterStatsSnapshot.TraitSettings`의 `Display`가 같고, 병과 고유가 하나 |

**Ub(사용자):** Test Runner `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 기대: 기존 결과와 같음(알려진 카메라 형식 실패 하나 제외) + 새 사례 통과.

### 3.1 TO-b 실행 기록 (2026-11-02, 코드 완료)

- `Compose`가 Acquire 키를 모으고 최종 목록에서 `DisplayOf`(병과 기본 키 → Acquire 키 → 그 밖, 출처 순서 안정 정렬)로 표시 목록을 채운다. internal 입구 `ResolveBase(…, display)`·`Resolve(…, ranks, display)`, 공개 서명 그대로. 임시 `UnitOriginDisplay`는 지웠다.
- 새 Core 시험 `TraitDisplayOriginTests`(7): 유닛 정의 순서 유지·병과 고유 뒤, 원래 가진 기본 특성 = 병과 고유(Q-TO2), 작은 키 나중 획득·강화 자리 유지, 획득 순서, 메인 전직 전 획득(Q-TO3, 기본 특성 연결 있음 → 병과 / 없음 → 획득), 기본 없는 Cannon, `BuildTraitSettings` = resolver.
- 통합 시험 1개 추가(`BattleTraitSettingsPublicationTests`): 전투 `For(행위자·메인)` = 전투 밖 카드 `Display`, 모든 전투 캐릭터의 표시 키 = 설정 키, 병과 고유 ≤ 1.
- 계획과 다른 점: Tb.6 일곱 사례 중 "적(보유 아님)"은 기본 특성 없는 Cannon 사례·`BuildTraitSettings` 사례로 대신했다(보유 아닌 경로 = `ResolveBase`). Tb.7은 운영 DB 유닛을 고르지 않고 기존 전투 고정물의 모든 캐릭터로 확인했다(운영 기본 특성 DB 시험 `DataBaseAssetTests.CategoryDefaultBattle_*`이 기준선에서 이미 실패 중이라 기대지 않음).
- Unity batchmode(Core + CoreIntegration): 5,503개 중 실패 25. **기준선과 실패 목록 같음, 새 실패 0.** 새 사례 8개 통과. 실행 전후 다른 파일 변경 없음.

## 4. Phase TO-c 문서

| Task | 내용 |
|---|---|
| Tc.1 | Core 전달 문서 10-29 "특성 획득 순서 표시 요청" 절에 "대신함 → 이 설계" 표시, 새 절(바뀐 계약·View 읽는 법). **파일 제목도 갱신**(CLAUDE.md 2026-11-02 지시) |
| Tc.2 | 설계 문서 끝에 구현 메모(계획과 다른 점) |
| Tc.3 | View 담당 메모는 채팅 문안으로만(파일 없음): ① 특성 칸 2개 고정 → n개(전투 멈춤 수정, 이 작업과 독립) ② `TraitSettings.For(유닛).Display` / `OwnedCharacterStatsSnapshot.TraitSettings.Display` 순서·출처 읽는 법 |

### 4.1 TO-c 실행 기록 (2026-11-02, 문서 완료)

- Tc.1: Core 전달 문서 새 §5.82(앵커 `trait-display-origin`), 10-29 요청 절 머리에 "대신함" 표시, 파일 제목·맨 위 요약 갱신.
- Tc.2: 설계 §9 구현 메모.
- Tc.3: View 담당 메모는 채팅 문안으로만 드림(파일 없음).

## 5. 바뀌는 파일

- 새: `Contracts/Common/TraitOrigin.cs`, `Contracts/Snapshots/Traits/TraitDisplayEntry.cs`, `Assets/Tests/EditMode/TraitDisplayOriginTests.cs`(+ 각 `.meta`)
- 고침: `TraitSettingsSet.cs`, `OwnedUnitSpecResolver.cs`, `OwnedCharacterStatsCalculator.cs`, 시험 4개
- 안 바뀜: `UnitDefinition`, `UnitCatalog`, `UnitStatsSnapshot`, `CharacterBaselineStatsSnapshot`, Buffer, View, Framework, DB 에셋

커밋 때 새 `.cs`는 `.meta`와 함께. TO-a만 커밋해도 컴파일·동작은 성립한다(모두 `Unit` 출처).
