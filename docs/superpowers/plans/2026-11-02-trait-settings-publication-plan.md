# 특성 설정값 공개 Task 계획 (SP-a~SP-c)

2026-11-02. 설계 [trait-settings-publication-design](../specs/2026-11-02-trait-settings-publication-design.md)의 결정(Q-SP1~Q-SP4 모두 A, 사용자 "권장안대로")을 Task로 나눈 계획이다. **코드 착수 전.** Phase마다 멈추고 보고한다.

경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` 기준(`CC/`), 시험은 `Assets/Tests/EditMode/` 기준.

## 0. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 내용 | 이유 |
|---|---|---|
| P1 | `TraitSettingsSet`은 `CC/Contracts/Snapshots/Traits/`, `BattleTraitSettings`는 `CC/Contracts/Combat/`에 둔다 | 같은 폴더의 기존 읽기 계약 옆. 네임스페이스 = 폴더 |
| P2 | 보유 스냅샷 새 인자 `traitSettings`는 `stats` 바로 뒤 필수 인자 | 위치 인자 호출이 모두 컴파일 오류로 드러나 빠뜨릴 수 없음(Q-SP2 A) |
| P3 | `CombatPreparation`은 구역을 받는 생성자에만 필수 인자 `traitSettings`(`playerZone` 뒤)를 더하고, 줄 수를 받는 옛 생성자는 `drawOrder` 뒤(첫 선택 인자 `drawCursor` 앞)에 둔다 | 두 생성자 모두 필수. 옛 생성자는 시험 한 곳(P8)만 씀 |
| P4 | 전투 표는 `BuildProfiles`와 같은 함수로 만든다: 유닛 키마다 `ResolveBase(unit, growth)`, 보유 개체마다 `Resolve(unit, record, growth)` | 명령이 쓰는 프로필과 같은 계산이어야 `For` 결과 = 규칙이 쓰는 값 |
| P5 | `For(character)` 규칙 = `RequiredProfile`과 같음: 보유 ID가 있고 그 ID 항목이 있으면 보유 설정, 아니면 유닛 키 기본. 유닛 키도 없으면 `KeyNotFoundException` | 조용히 빈 값을 주지 않음 |
| P6 | 만드는 함수는 하나. ~~계약 파일 안 `BattleTraitSettings.Build`~~ → `OwnedUnitSpecResolver.BuildTraitSettings`(SP-b 실행 기록 §2.1, 어셈블리 방향 때문) | 표 생성 규칙 한 곳 |
| P7 | 새 Core 시험은 `ProjectL.Tests.Core`(단위)와 `CoreIntegration`(일치 확인)에만 | 메모리 규칙(Core 시험 어셈블리 둘) |
| P8 | `ControllerTurnCountPublicationTests.cs:143`(작성자 Droute)은 고치기 전에 묻는다 | CLAUDE.md: 다른 사람 시험은 먼저 묻기 |

## 1. Phase SP-a — 보유 설정

| Task | 내용 |
|---|---|
| Ta.1 | 새 `CC/Contracts/Snapshots/Traits/TraitSettingsSet.cs`(+ `.meta`는 Unity가 만듦): `Empty`, `Traits`(읽기 전용 복사 목록), `TryGet(TraitKey, out TraitSpec)`, `TryGet<T>(out T)`, `SameValues`(개수·순서·`TraitSpec.SameValues`), `internal static From(UnitSpec)`. 생성자 검사: null 항목·중복 키 거절 |
| Ta.2 | `CC/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`: 속성 `TraitSettings`, 생성자 `stats` 뒤 필수 인자(null 거절), 키 순서 = `stats.TraitKeys` 검사(다르면 `ArgumentException`). 옛 전용 속성 다섯은 그대로(Q-SP3 A) |
| Ta.3 | `CC/Application/Roster/OwnedCharacterStatsCalculator.cs:118`: `TraitSettingsSet.From(resolved)`를 넘김 |
| Ta.4 | 시험 호출 맞춤(모두 천명 작성): `OwnedCharacterStatsCalculatorTests.cs:84·334·336·339`, `RunDeckContractTests.cs:39` → `TraitSettingsSet.Empty` 또는 해당 값, `CoreIntegration/ImpactBoltAuthoringTests.cs:218` → `first.TraitSettings`, `:222` → `TraitSettingsSet.Empty`(두 시험이 확인하는 "충격 볼트 설명 불일치" 예외 의도를 유지) |
| Ta.5 | 새 시험(`ProjectL.Tests.Core`): `TraitSettingsSetTests` — 조회·형식 조회·비교·중복 거절. `OwnedCharacterStatsCalculatorTests`에 사례 하나 — 성장(등급)이 있는 보유 개체의 `TraitSettings`가 `OwnedUnitSpecResolver.Resolve` 결과의 `Traits`와 `SameValues`, 키 순서 불일치 생성 거절 |

**Test Runner 확인:** `OwnedCharacterStatsCalculatorTests`, `RunDeckContractTests`, `ImpactBoltAuthoringTests`, 새 `TraitSettingsSetTests`. 화면 변화 없음.

### 1.1 SP-a 실행 기록 (2026-11-02, 사용자 "SP-a 진행하고, 테스트 너가 실행하고, Core 테스트랑 CoreIntegration만 돌려")

- **한 것:** 새 `CC/Contracts/Snapshots/Traits/TraitSettingsSet.cs`(+ `.meta`, Unity가 만듦), `OwnedCharacterStatsSnapshot.TraitSettings`(`stats` 뒤 필수 인자, null·키 순서 검사), `OwnedCharacterStatsCalculator`가 `TraitSettingsSet.From(resolved)`를 넘김.
- **계획과 다른 점:** `UnitSpec.Traits`는 저작 순서이고 `UnitCatalog`는 정의를 특성 키 순으로 정렬한다(`UnitCatalog.cs:103`). 그래서 묶음 생성자가 키 순으로 정렬해 `Stats.TraitKeys`·전투 특성 상태와 순서를 맞춘다. 시험이 묶음을 직접 만들 수 있게 생성자는 public이다(`From`은 internal).
- **시험 호출 맞춤(모두 천명 작성):** `OwnedCharacterStatsCalculatorTests` 네 곳, `RunDeckContractTests:39`, `ImpactBoltAuthoringTests:218·222`. 가시 불일치 시험(`:84`)은 키에 맞는 묶음을 넘겨 원래 확인하던 `thornFieldDescription` 예외를 유지했다.
- **새 시험:** `TraitSettingsSetTests`(4), `OwnedCharacterStatsCalculatorTests`에 가시 설정값 단언·`OwnedTraitSettings_FollowTraitKeysAndConstructorChecksThem`, `ImpactBoltAuthoringTests`에 개체별 거리(1·2)와 `Resolve` 결과 일치 단언.
- **실행(Unity 배치 모드, `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`):** 5487개 중 통과 5470, 실패 17(Core 1, CoreIntegration 16). **실패 17개는 같은 날 21:37 Test Runner 결과(`TestResults.xml`)의 실패 목록과 정확히 같다** — 새로 생긴 실패 0, 이번 변경과 무관(DB 에셋·덱 UI·카메라·도발 발동). 바뀐 시험 네 파일은 모두 통과(69 사례).
- 실행이 바꾼 파일: 새 `.cs` 둘의 `.meta`만.

## 2. Phase SP-b — 전투 설정

| Task | 내용 |
|---|---|
| Tb.1 | 새 `CC/Contracts/Combat/BattleTraitSettings.cs`: `Empty`는 두지 않음(Q-SP2 A). `For(CharacterCombatFacts)`, `ForUnit(EntityKey)`, `internal static Build(IReadOnlyList<UnitSpec> units, IReadOnlyList<OwnedCharacterRecord> owned, TraitGrowthCatalog growth)`(P4·P6). 검사: 유닛 키 중복·보유 ID 중복·보유 개체의 유닛 키가 표에 없음 거절 |
| Tb.2 | `CC/Contracts/Combat/BattlePreparation.cs`(`CombatPreparation`): 속성 `TraitSettings`, 두 생성자에 필수 인자(P3), null 거절 |
| Tb.3 | `CC/Application/Combat/BattlePreparationCalculator.cs:197`: `BattleTraitSettings.Build(initialization.Units, owned, initialization.TraitGrowth)`를 넘김. 기존 `baseUnits`·`effective` 계산은 그대로(규칙용 정의) |
| Tb.4 | `BufferSystems/Combat/CombatStateData.cs`: 속성 `TraitSettings`, 준비 생성자에서 `preparation.TraitSettings`, 복사 생성자에서 `source.TraitSettings` |
| Tb.5 | 시험 호출 맞춤: `CoreIntegration/StageGimmickStatePublicationTests.cs:81`·`TornadoGimmickPublicationTests.cs:97`(천명) → `current.TraitSettings`. `ControllerTurnCountPublicationTests.cs:143`(Droute)은 **P8 답을 받은 뒤** 같은 한 줄 |
| Tb.6 | 새 시험(`CoreIntegration`): 운영 DB 전투 하나를 준비해 필드·손패의 모든 캐릭터에 대해 `CombatStateData.TraitSettings.For(c)`가 명령 입력 `RequiredProfile(input, c).Definition.Traits`와 `SameValues` — 보유(성장 있음·없음)·적. 소환이 있는 스테이지면 소환 개체도. 단위(`ProjectL.Tests.Core`): `BattleTraitSettingsTests` — 보유 우선·기본 대체·없는 키 예외·중복 거절 |

**Test Runner 확인:** Tb.5의 세 파일, 새 `BattleTraitSettingsTests`·일치 시험, 전투 준비 회귀(`BattleEnvironmentPreparationTests`·`InitialTornadoPlacementTests`). 화면 변화 없음.

### 2.1 SP-b 실행 기록 (2026-11-02, 사용자 "SP-b 진행", P8 허락 — "고쳐도 됨")

- **한 것:** 새 `CC/Contracts/Combat/BattleTraitSettings.cs`(+ `.meta`, public 생성자 = 유닛 키 표 + 보유 ID 표(유닛 키 포함), `For`·`ForUnit`), `OwnedUnitSpecResolver.BuildTraitSettings`(`BuildProfiles` 바로 아래), `CombatPreparation.TraitSettings`(두 생성자 모두 필수, P3), `BattlePreparationCalculator`가 표를 넘김, `CombatStateData.TraitSettings`(준비·복사 생성자).
- **계획과 다른 점:** P6의 "표 생성 함수를 계약 파일 안에"는 어셈블리 방향(Contracts → Application 참조 불가) 때문에 못 한다. 대신 `OwnedUnitSpecResolver.BuildTraitSettings`(Application)에 두었다 — `BuildProfiles`와 같은 파일·같은 함수(`ResolveBase`·`Resolve`)라 명령 프로필과의 일치 근거가 더 분명하다. `For`는 보유 항목의 유닛 키가 캐릭터와 다르면 `InvalidOperationException`(조용히 기본값으로 넘어가지 않음).
- **시험 호출 맞춤:** `StageGimmickStatePublicationTests:81`·`TornadoGimmickPublicationTests:97`(천명), `ControllerTurnCountPublicationTests:143`(Droute, 사용자 허락, 인자 하나만 — 단언 무변경. Droute께 변경 정리 전달 필요).
- **새 시험:** `BattleTraitSettingsTests`(Core 3: 보유 우선·기본 대체, 없는 키·보유 키 불일치 예외, 생성자 검사), `CoreIntegration/BattleTraitSettingsPublicationTests`(2: 명령 입력의 모든 캐릭터에서 `For` = 프로필 선택 규칙으로 고른 `TraitSpec`, 전투 표 = 보유 카드 `TraitSettings` — 배치·전투 진입 명령으로 여러 번 복사된 뒤 확인).
- **실행(Unity 배치 모드, Core·CoreIntegration):** 첫 실행에서 새 통합 시험 하나가 시험 준비 문제로 실패(`Publication()`은 정의를 포착하는 출처가 필요 → `EncounterSource`로 고침). 다시 실행: 5492개 중 통과 5475, 실패 17 — **SP-a 기준 실행의 실패 17개와 정확히 같음, 새 실패 0.** 새 시험 5개·호출을 맞춘 시험 세 파일·전투 준비 회귀(`BattleEnvironmentPreparationTests` 53·`InitialTornadoPlacementTests` 9) 모두 통과.
- 실행이 바꾼 파일: 새 `.cs` 넷의 `.meta`만.

## 3. Phase SP-c — 기록·전달

| Task | 내용 |
|---|---|
| Tc.1 | 설계 문서에 구현 메모(계획과 다른 점) |
| Tc.2 | View 인계 문서 `docs/superpowers/handoffs/2026-11-02-trait-settings-view-handoff.md`(구현이 끝난 API 기준으로 작성). 내용은 아래 "View 인계 문서에 넣을 것" |
| Tc.3 | Core 전달 문서(`handoffs/2026-09-24-view-core-data-boundary-handoff.md`)에 Core 변경 기록 한 절, 제목 갱신(CLAUDE.md 규칙) |

### 3.0 SP-c 실행 기록 (2026-11-02, 사용자 "SP-c 진행", 코드 변경 없음)

- Tc.1: 설계 문서 §7.1 구현 메모(설계와 다른 점 둘: 키 순 정렬, 표 생성 함수 위치).
- Tc.2: [View 인계 문서](../handoffs/2026-11-02-trait-settings-view-handoff.md) — 아래 네 항목 모두 넣음. 옛 전용 속성 정리는 파일·줄·속성 이름까지 적은 필수 작업으로, 끝나면 검색 결과 0을 Core에 알리게 했다. §64·§55 CVI-R1의 답임을 밝힘. 미전달.
- Tc.3: Core 전달 문서 제목·머리 알림 갱신(CLAUDE.md 규칙), 새 §5.81(Core 변경 기록), §64.6(특성 부분 답, 병합 때 §64.4 → §64.6), §55 CVI-R1 답 한 단락. 미전달.
- 남은 것: 사용자 커밋(SP-a·SP-b 코드·시험·새 `.cs` 넷의 `.meta`, 문서 넷), View 인계 전달, Droute께 시험 한 줄 변경 알림, View 정리 알림 뒤 SP-d.

### View 인계 문서에 넣을 것 (Tc.2)

1. **새 읽기 두 곳과 사용 예:** 전투 밖은 `OwnedCharacterStatsSnapshot.TraitSettings`, 전투 중은 `CombatStateData.TraitSettings.For(유닛)`. 예: `settings.TryGet<StationaryFireTraitSpec>(out var s)` → `s.ExtraDamagePercent`. 보유·기본 선택 규칙은 `For`가 하므로 View가 따로 만들지 않는다.
2. **옛 전용 속성 정리 지시(View 작업, 필수):** 아래를 모두 새 읽기로 옮기고 옛 경로를 지운다.
   - `UnitCardTraitContent`: 특성 키별 설명 인자 switch(`thornFieldDescription`·`huntersMarkDescription`·`pullDescription`·`reconSquadDescription`)와 `Create` 오버로드의 그 인자들을 지우고, 특성 설정 묶음 하나를 받아 숫자를 꺼내게 바꾼다.
   - 위 인자를 넘기는 곳 셋에서 옛 속성 읽기를 지운다: `UnitCardUI`(`:265`), `DeckSlotInfoUI`(`:59`), `DeckEditModelBuilder`(`:53`)와 `DeckEditDisplayModel`의 설명 속성·생성자 인자.
   - 설명 숫자를 상태 스냅샷에서 읽는 곳(`UnitCardTraitContent.ConfiguredValues`의 표식·정찰·위압·재생, `DescribeThornField`·`DescribePull`이 받는 상태 형식)을 설정 묶음 읽기로 바꾼다. 상태 스냅샷은 현재 진행값(중첩·남은 턴 등)에만 쓴다.
   - 끝나면 Core에 "옛 설명 속성 읽는 곳 없음"을 알린다. 확인 방법: View 코드에서 `ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`·`ImpactBoltDescription`·`ReconSquadDescription` 검색 결과 0.
3. **Core 다음 단계 예고:** View 정리 알림을 받으면 Core가 SP-d로 옛 속성 다섯을 지운다. 그 뒤로는 옛 속성을 쓰면 컴파일 오류가 난다.
4. **별건 참고:** 진지 구축·추적 사격 kind 140 줄 추가, 고정 사격 상시 이펙트 조건(이미 전달한 메모와 같은 내용, 한 단락).

## 3.1 Phase SP-d — 옛 전용 속성 제거 (View 정리 알림 뒤)

| Task | 내용 |
|---|---|
| Td.1 | `OwnedCharacterStatsSnapshot`에서 `ThornFieldDescription`·`HuntersMarkDescription`·`PullDescription`·`ImpactBoltDescription`·`ReconSquadDescription` 속성·생성자 인자·키 일치 검사 삭제 |
| Td.2 | `OwnedCharacterStatsCalculator.cs:95-114`의 특성별 분기 삭제 |
| Td.3 | 시험 호출·단언 맞춤(`OwnedCharacterStatsCalculatorTests`, `ImpactBoltAuthoringTests:215·218·222` — 설명 속성 대신 `TraitSettings`로 같은 값을 확인) |
| Td.4 | 상태 스냅샷 안 설정값(가시밭·표식·끌어오기·정찰·위압·재생) 정리는 규칙·연출이 그 값을 읽는지 따로 확인한 뒤 결정(이 Phase에서는 조사만) |

## 4. 커밋 묶음

SP-a와 SP-b는 각각 한 커밋으로 묶을 수 있다(생성자 인자 추가와 시험 호출 맞춤은 같은 커밋이어야 컴파일됨). 새 `.cs` 둘은 Unity가 만든 `.meta`와 함께.

## 5. 범위 밖

- View 화면 이전(`UnitCardTraitContent` 등) — View 작업자.
- 옛 전용 속성 다섯·상태 안 설정값 제거 — View 이전 뒤 별도 작업.
- 훈련·증강·아이템·유물·환경 — 설계 §8.
