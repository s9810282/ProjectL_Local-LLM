# 유닛별 스테이지 기록 Task 계획

Date: 2026-10-27
Status: S-1~S-3 완료(2026-10-27). S-1·S-2는 사용자가 Core·CoreIntegration 시험 통과를 확인했다. TP-1 a(Q-S7 B) 결정. 커밋 전.
Design: [unit-stage-record-design](../specs/2026-10-27-unit-stage-record-design.md) — Q-S1~Q-S7 모두 권장안(2026-10-27 사용자 "권장안대로").
Source baseline: `feature/Cheon/Core/NewCombatSytem`, HEAD `4640511d`.

경로 약어: `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `B` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`, `T` = `Assets/Tests/EditMode`.

## 0. 진행 규칙

- Phase S-1 → S-2 → S-3 순서. Phase가 끝나면 멈추고 보고한다. 커밋·push는 사용자가 한다.
- 시험은 `ProjectL.Tests.Core`(`T/` 바로 아래)와 `ProjectL.Tests.CoreIntegration`(`T/CoreIntegration/`)만 고치고 더한다. MSBuild는 돌리지 않는다. 검증은 사용자가 Unity Test Runner로 두 어셈블리를 돌린다.
- 새 `.cs`는 Unity가 만든 `.meta`와 함께 커밋한다.

### 0.1 결정 반영(설계 §8)

| ID | 결정 | 계획에서 닿는 곳 |
|---|---|---|
| Q-S1 A | 피해 = 실제 HP 감소(`ActualHealthLoss`) | T10 |
| Q-S2 A | 넣은 피해 = 적 캐릭터에 들어간 피해만 | T11 |
| Q-S3 A | 추락은 받은 피해 아님 | T10(경계를 지나지 않으므로 코드 없음), T15 사례 |
| Q-S4 A | 이동 거리 = 자발 경로 칸 수 | T12 |
| Q-S5 A | 처치 = 보상 자격 처치(`CreditedKill`) | T11 |
| Q-S6 A | 영수증 행 대상에 "기록이 비지 않은 보유 아군" 추가 | T14 |
| Q-S7 A → **B** | `CharacterRewardReceipt` 생성자 끝 선택 인자(TP-1 a, 2026-10-27 사용자) | T3, TP-1 |

### 0.2 계획에서 정한 것(설계에 없던 세부)

| ID | 내용 | 이유 |
|---|---|---|
| **TP-1 (착수 전 확인)** | **Framework 시험 파일은 고치지 않는다**(2026-10-27 사용자 "Core·CoreIntegration 테스트만 진행·수정"). 그런데 Q-S7 A(영수증 생성자 필수 인자)면 `ProjectL.Tests.Framework`의 생성 호출 6곳(`CombatEndUITests` 2·`CombatRewardExperienceAnimationTests` 1·`CombatClearRewardIntegrationTests` 3)이 컴파일되지 않는다. Unity는 컴파일 오류가 하나라도 있으면 새 코드를 불러오지 않으므로 Core 시험도 새 코드로 돌지 않는다. 선택: **a. Q-S7을 B로 바꿈** — 끝 선택 인자 `stageRecord = null`(= `Empty`), Framework는 그대로 컴파일됨, 영수증 생성처(`CombatExperienceSettlement`)는 늘 값을 넘기고 T5 대조가 빠짐을 막음 / b. A 유지 — Framework 컴파일 오류를 감수. **권장: a.** | 사용자 지시와 Core 시험 실행을 함께 지키려면 생성자 모양으로 피해야 한다. |
| TP-2 | 귀속 함수 입력은 사망 사건이 아니라 `(input, projected, origins, reflections)`. 반사 목록은 `CombatDamageCredit.Reflections(events)`가 한 번 만든다. | 처치(사망 사건의 출처)와 피해 1회(경계의 출처)가 같은 함수를 부르려면 출처 목록을 받아야 한다. |
| TP-3 | 경계는 `ActualHealthLoss > 0`일 때만 피해 항목을 남긴다. 항목의 출처 = 체력 사건에 넣은 목록(`eventOrigins ?? [origin]`). | 0 항목은 기록을 바꾸지 않는다. 처치 귀속과 같은 출처를 보게 한다. |
| TP-4 | 정산 함수는 보유 아군마다 증분을 모아 `With(stageRecord:)`를 한 번만 부른다(EntityId 순). | 사본 생성 횟수를 줄이고 결과 순서를 고정한다. |
| TP-5 | Buffer의 `RequireNoEarnedExperience`를 `RequireFreshBattleProgress`(획득 경험치·기록 둘 다 비어야 함) 하나로 넓힌다. 호출 3곳 그대로. | 같은 수명 규칙을 한 함수가 맡는다. |
| TP-6 | `CharacterStageRecord.Add(int damageTaken = 0, int damageDealt = 0, int distanceMoved = 0, int kills = 0)`(음수 예외, 합은 `checked`). | 호출처가 이름 있는 인자로 한 값만 더한다. |

## 1. Phase S-1 — 계약·Buffer(동작 변화 없음, 기록은 늘 `Empty`)

### T1. 새 계약 `C/Contracts/Combat/CharacterStageRecord.cs`

- `public sealed class CharacterStageRecord`: `DamageTaken`·`DamageDealt`·`DistanceMoved`·`Kills`(int, get-only), `static Empty`, `IsEmpty`, `Add(...)`(TP-6, 모두 0이면 자신을 돌려줌), `SameValues(other)`(null이면 false, `CharacterExperienceFacts.SameValues` 꼴).
- 생성자 private, 값은 0 이상. XML doc에 네 값의 정의(설계 D3)와 "전투 동안만 사는 보유 아군 기록, 원본은 전투 사실" 한 줄.

### T2. `C/Contracts/Combat/CharacterCombatFacts.cs`

- `public CharacterStageRecord StageRecord { get; }`(doc: 획득 경험치와 같은 수명).
- 생성자 끝 선택 인자 `CharacterStageRecord stageRecord = null` → `?? CharacterStageRecord.Empty`. 검사: `!StageRecord.IsEmpty && !ownedId.HasValue`이면 `ArgumentException`("기록은 보유 개체만 가질 수 있습니다.").
- `SameValues`에 `!StageRecord.SameValues(other.StageRecord)`.
- `With(..., CharacterStageRecord stageRecord = null)`·`WithPlacement`·`Rebuild`가 값을 넘긴다(`Rebuild` 인자 하나 추가, `:188` 생성 호출 끝에 전달).
- `BattleCharacterCalculator.cs:76`(전투 준비 생성)은 인자를 넘기지 않아 `Empty`다 — 고치지 않음.

### T3. `C/Contracts/Combat/CharacterRewardReceipt.cs`

- 생성자 끝 필수 인자 `CharacterStageRecord stageRecord`(null이면 `ArgumentNullException`), 속성 `StageRecord`, `SameValues`에 비교 추가. 머리 경고 주석은 그대로 맞는다.

### T4. 영수증 생성처 `C/Application/Combat/Experience/CombatExperienceSettlement.cs:91`

- `new CharacterRewardReceipt(..., facts.StageRecord)`. 행 대상은 T14(S-2)에서 바꾼다.

### T5. `C/Contracts/Combat/CombatRewardConsistency.cs` `ValidateExperienceSettlement`

- 기존 행 대조(`entry.CharacterKey != ...` 블록)에 `!entry.StageRecord.SameValues(recipients[0].StageRecord)`를 더한다(문구: "정산 내역의 스테이지 기록이 최종 전투 사실과 다릅니다.").

### T6. `B/Character/CharacterBuffer.cs`

- `:405-412` 옆에 기록 규칙: 명령 게시면 네 값 각각 `final >= expected`, 아니면 `SameValues`. 문구는 경험치 문구와 같은 꼴("스테이지 기록은 줄어들 수 없습니다." / "스테이지 기록은 전투 명령 게시로만 바꿀 수 있습니다.").
- `RequireNoEarnedExperience` → `RequireFreshBattleProgress`(TP-5), 문구 "새 전투 캐릭터는 획득 경험치·스테이지 기록 없이 시작해야 합니다."

### T7. `B/Character/CharacterData.cs`

- `public CharacterStageRecord StageRecord => _facts.StageRecord;` (전투 중 열람 입구).

### T8. 시험(S-1)

| 파일 | 할 일 |
|---|---|
| `T/ContractShapeGuardTests.cs` | `CharacterCombatFacts`·`CharacterRewardReceipt` 목록에 `StageRecord` |
| `T/BattleRewardReceiptTests.cs`(1)·`T/CombatRewardConsistencyTests.cs`(6)·`T/CombatRewardSettlementTests.cs`(2) | 생성 호출 끝에 `CharacterStageRecord.Empty` |
| `T/CharacterCombatFactsSameValuesTests.cs` | 기록만 다른 두 사실이 다르다는 사례 |
| `T/ContractWithPreservationTests.cs` | `With`·`WithPlacement`가 기록을 보존하는 사례 |
| 새 `T/CharacterStageRecordTests.cs` | 음수 거절·`Add` 누적·0 더하기는 같은 객체·넘침 예외·`SameValues`, 적 사실에 기록을 주면 거절 |
| TP-1 | Framework 시험은 고치지 않음(TP-1 a면 생성자 호출 그대로 컴파일됨) |

S-1 확인(사용자): 두 어셈블리 전체 통과(동작 변화가 없으므로 기존 결과와 같아야 함).

## 2. Phase S-2 — 집계

### T9. 귀속 함수 `C/Application/Combat/Rewards/CombatDamageCredit.cs`(새)

- `internal static EntityId? Recipient(CombatCommandInput input, IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, IReadOnlyList<CombatDamageOrigin> origins, IReadOnlyList<ThornHeartReflectionContext> reflections)` — `ConfirmedKillCalculator.Recipient`·`OwnedAlly` 본문을 그대로 옮긴다(첫 가해자 → 반사 → 명령 행동자).
- `internal static IReadOnlyList<ThornHeartReflectionContext> Reflections(IReadOnlyList<CombatEvent> events)`.
- `ConfirmedKillCalculator`는 `CombatDamageCredit.Recipient(input, projected, victimDeaths[0].DamageOrigins, reflections)`를 부른다. 처치 동작 같음.

### T10. 피해 항목 `C/Application/Combat/CombatDamageApplication.cs`

- 내부 값 `CombatStageDamageEntry(EntityId RecipientId, IReadOnlyList<CombatDamageOrigin> Origins, int HealthLoss)`(같은 파일 또는 T11 파일).
- `Apply`에서 체력 사건을 남긴 직후 `actualHealthLoss > 0`이면 항목 추가(TP-3). 이미 죽은 대상의 이른 반환은 항목 없음.
- `internal IReadOnlyList<CombatStageDamageEntry> CaptureStageDamage()`(복사본).
- 클래스 doc에 "스테이지 기록의 피해 원천이다. 이 경계를 지나지 않는 HP 감소(추락)는 기록되지 않는다" 한 줄.

### T11. 정산 `C/Application/Combat/Experience/CombatStageRecordSettlement.cs`(새)

- `internal sealed class CombatStageTally`: `Damage`(항목 목록), `ActorId`, `ActorDistance`.
- `internal static void Record(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected, IReadOnlyList<CombatEvent> events, IReadOnlyList<CreditedKill> kills, CombatStageTally tally)`:
  - 항목마다: 받은 개체가 보유 아군이면 `DamageTaken += HealthLoss`. 받은 개체가 적이면 `CombatDamageCredit.Recipient(...)`의 아군에 `DamageDealt += HealthLoss`(Q-S2).
  - 행동자가 보유 아군이고 `ActorDistance > 0`이면 `DistanceMoved += ActorDistance`(Q-S4).
  - `kills`마다 `RecipientId`에 `Kills += 1`(Q-S5).
  - 증분을 모아 개체당 `With` 한 번(TP-4). `tally`가 null이면 처치만 반영.

### T12. `C/Application/Combat/MovementProjection.cs` `Complete`

- `new CombatStageTally(Damage.CaptureStageDamage(), Input.ActorId, DistanceSpent)`를 `Finish`에 넘긴다. 강제 이동(`RecordForcedMove`)은 `_entered`에 들어가지 않으므로 그대로 빠진다.

### T13. `C/Application/Combat/CombatCommandCalculator.cs` `Finish`

- 끝 선택 인자 `CombatStageTally stageTally = null`. `ConfirmedKillCalculator.Collect` 바로 다음, `CombatExperienceSettlement.Settle` 전에 `CombatStageRecordSettlement.Record(input, projected, events, kills, stageTally)`.
- `BossRoarCalculator.cs:80`의 초기화 단계는 인자를 넘기지 않는다(피해 없음, 처치 없음).

### T14. 영수증 행 대상(Q-S6) `CombatExperienceSettlement.Apply:66`

- 조건에 `|| !c.StageRecord.IsEmpty`. 진행 설정이 꺼져 있어도 승리면 행이 생긴다(지금 클리어 참여자 행과 같은 처리).

### T15. 시험(S-2)

새 `T/CombatStageRecordTests.cs`(Core, `CombatCommonCommandTests.Command`·`CombatCommandTestData` 도우미, `ExperienceAttributionPathTests` 꼴):

| 사례 | 기대 |
|---|---|
| 아군 일반 공격이 적 체력 10 중 6을 깎음 | 공격자 `DamageDealt` 6, 적 사실 기록 `Empty` |
| 적 공격 피해 15, 아군 체력 10 | 아군 `DamageTaken` 10(넘친 피해 제외, Q-S1) |
| 피해 직전 아이템이 피해를 줄임 | 줄인 뒤 실제 감소만 |
| 넉백으로 적을 가시에 밀어 처치 | 같은 행동자에게 `Kills` 1·가시 피해만큼 `DamageDealt`(T9 공유 규칙) |
| 자발 이동 3칸 | `DistanceMoved` 3. 넉백당한 아군 `DistanceMoved` 0 |
| 낭떠러지 추락 사망 | `DamageTaken` 0(Q-S3) |
| 아군이 원인인 피해가 다른 아군에게 들어감(화약통 폭발 등, 구현 때 실제 경로로 고름) | 맞은 아군 `DamageTaken`만, `DamageDealt` 0(Q-S2) |
| 보상 제외(`UnitKillRewardSpec.Excluded`) 적 처치 | `Kills` 0(Q-S5) |
| 승리 단계 | 영수증 행 `StageRecord` = 최종 사실, 클리어 비참여 아군이라도 기록이 있으면 행이 있음(Q-S6) |

`T/CoreIntegration/`(Buffer 게시까지):

| 파일 | 사례 |
|---|---|
| `CombatCommandPublicationTests.cs` 또는 새 `StageRecordPublicationTests.cs` | 두 명령에 걸쳐 기록이 누적되어 `CharacterData.StageRecord`로 보임. 줄어든 기록 변경 게시는 거절. |
| `CombatCompletionPublicationTests.cs` | 승리 완료·`EndBattle` 뒤 `Completion.Rewards`의 행 기록이 남고, 다음 전투 준비 사실은 `Empty` |

S-2 확인(사용자): 두 어셈블리 전체. **실패 가능 지점:** 손으로 만든 기대 사실을 결과 사실과 `SameValues`로 비교하는 기존 시험은 이제 기록이 달라 실패할 수 있다. 그 경우 확인 내용은 그대로 두고 기대 사실에 기록을 맞춘다(Core 시험 = 사용자 시험).

## 3. Phase S-3 — 마무리

- T16. View 전달 메모(채팅 붙여넣기용 글, 파일 안 만듦): 읽는 곳 두 개(`CharacterData.StageRecord`, `Completion.Rewards.Characters[i].StageRecord`), 네 값 정의, 영수증 생성자 변경(TP-1 결과).
- T17. 설계 문서 상태줄, 이 계획의 실행 기록.

## 4. 변경 영향

| 고치는 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 전투 사실 계약(T2) | 모든 사실 복사(`With`/`Rebuild` 한 경로), Buffer 변경 검사 | 계약 모양·`SameValues`·`With` 보존·Buffer 게시 | 공격·이동 계산 본문(사본 경로가 하나라 자동 보존) |
| 영수증(T3~T5, T14) | 보상 일관성 대조, 결과 화면 읽기(View) | 보상 정산·일관성·완료 게시 | 재화·명성 계산 |
| 피해 경계(T10) | 경계를 부르는 모든 피해 경로(직접·추가·반사·환경·장애물) | `CombatStageRecordTests`, 기존 피해·아이템 시험 | 경계 밖 HP 변화(회복·정규화·추락) |
| 귀속 함수 이동(T9) | 처치 보상·Run 처치 수 | `CombatKillRewardTests`·`ExperienceAttributionPathTests` | — |
| `Finish` 순서(T13) | 처치 수집 → 기록 → 경험치 | 경험치 정산 시험 | 사망 정산·영혼·종료 체력 |

View·Framework 코드, Factory·ServiceKeys, DB·SO는 바뀌지 않는다. 사건 형식이 늘지 않아 View 사건 경로 표에도 영향이 없다.

## 5. 실행 기록

### S-1 코드 완료(2026-10-27, 컴파일·시험 미확인)

사용자 "TP-1 a로 하고(Q-S7 B, 끝 선택 인자) S-1 진행해". Framework 시험은 돌리지도 고치지도 않는다(사용자 지시).

바꾼 것:

- 새 `C/Contracts/Combat/CharacterStageRecord.cs`(`.meta`는 Unity가 만든다): 네 값·`Empty`·`IsEmpty`·`Add`·`IsAtLeast`·`SameValues`.
- `CharacterCombatFacts`: `StageRecord` 속성, 생성자 끝 선택 인자, 보유 개체만 기록 가능 검사, `SameValues`, `With(... , stageRecord)`(끝 인자), `WithPlacement`·`Rebuild` 전달.
- `CharacterRewardReceipt`: 끝 선택 인자 `stageRecord = null`(= `Empty`, TP-1 a), 속성, `SameValues`.
- `CombatExperienceSettlement`: 영수증 행에 `facts.StageRecord`를 넘김.
- `CombatRewardConsistency.ValidateExperienceSettlement`: 행 기록 = 최종 전투 사실 기록.
- `CharacterBuffer`: 명령 게시면 줄지 않음(`IsAtLeast`), 다른 경로면 그대로, `RequireNoEarnedExperience` → `RequireFreshBattleProgress`.
- `CharacterData.StageRecord`.

시험(Core 어셈블리): `ContractShapeGuardTests`(사실·영수증 목록 + 새 `CharacterStageRecord` 목록), `CharacterCombatFactsSameValuesTests`(기록만 다른 사례), `ContractWithPreservationTests`(`With`가 기록만 바꿈·`WithPlacement`가 보존), 새 `T/CharacterStageRecordTests.cs`(누적·0 더하기·음수·넘침·비교·적 사실 거절·영수증 기본값과 비교).

계획과 다른 점:

- `CharacterStageRecord.IsAtLeast`를 더했다. Buffer가 "네 값 각각 줄지 않음"을 한 줄로 묻게 하려는 것이다(T6의 규칙 그대로).
- `RequireFreshBattleProgress` 호출은 2곳이다(계획의 "3곳"은 정의까지 센 것).
- TP-1 a로 영수증 인자가 선택이 되어, Core 시험의 영수증 생성 호출 9곳(`BattleRewardReceiptTests`·`CombatRewardConsistencyTests`·`CombatRewardSettlementTests`)은 고치지 않았다.

S-2 때 볼 것(코드 대조로 찾음): `IntimidationAttackTests.cs:163`·`IntimidationStatePreservationTests.cs:31`은 사실의 모든 속성을 반사로 `Is.EqualTo` 비교한다. S-1에서는 기록이 늘 같은 `Empty` 객체라 통과하지만, S-2에서 공격이 기록을 바꾸면 `:163`이 실패할 수 있다(그때 `StageRecord`를 제외 목록에 넣는다). 시험 도우미가 사실을 생성자로 다시 만들며 기록을 버리는지도 S-2에서 확인한다.

확인(사용자): `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 동작 변화가 없으므로 기존 결과와 같아야 하고, 새 시험은 모두 통과해야 한다.

#### S-1 확인 중 계획 밖 수정(2026-10-27, 사용자 지시)

사용자 실행에서 `PullAndMarkPresentationTests.Pull_HistoricalHealthEvents_DoNotRewindArrival(True)`가 실패했다(`AudioSystem.RequireCue` 예외). S-1과 무관하다. 사망 사건의 효과음이 초기화 전 `AudioSystem`에 닿는데, 현강님 `6568bb94`(10-26)부터 초기화 전 재생 요청이 `false` 대신 예외가 되었다. 사용자 "오디오를 소리 없는 시험용 가짜로 바꿔줘"에 따라:

- `T/CoreIntegration/PullAndMarkPresentationTests.cs`: `CreatePresenter`의 `new AudioSystem()` → 파일 안 `SilentAudio`(모든 재생·정지 `false`·0, 확인 내용은 그대로), 주석 고침.
- `T/CoreIntegration/ProjectL.Tests.CoreIntegration.asmdef`: 참조에 `FMODPlus` 추가(`IAudioSystemService` 멤버가 `AudioPlayerId`를 써서 가짜를 구현하려면 필요).

S-1 확인 완료(2026-10-27, 사용자 "Core·CoreIntegration 시험 모두 통과").

### S-2 코드 완료(2026-10-27, 컴파일·시험 미확인)

사용자 "S-2 진행해".

바꾼 것:

- 새 `C/Application/Combat/Rewards/CombatDamageCredit.cs`: `ConfirmedKillCalculator`의 귀속 규칙(`Recipient`·`OwnedAlly`)을 본문 그대로 옮기고 입력을 출처 목록으로(TP-2), 반사 목록 `Reflections`. `ConfirmedKillCalculator`는 이것을 부른다(처치 동작 같음).
- `CombatDamageApplication`: 체력 사건을 남긴 직후 `ActualHealthLoss > 0`이면 피해 항목(`CombatStageDamageEntry`, 같은 파일)을 남김(TP-3), `CaptureStageDamage`.
- 새 `C/Application/Combat/Experience/CombatStageRecordSettlement.cs`: `Record`(받은 피해 = 보유 아군이 받은 항목, 넣은 피해 = 적이 받은 항목의 귀속 아군, 이동 = 행동자 `DistanceSpent`, 처치 = `CreditedKill`), 장부 `CombatStageTally`. 증분은 `CharacterStageRecord`로 모아 개체당 `With` 한 번(TP-4).
- `MovementProjection.Complete`: 장부를 만들어 `Finish`에 넘김.
- `CombatCommandCalculator.Finish`: 끝 선택 인자 `stageTally`, 처치 수집 직후·경험치 정산 전에 `Record`.
- `CombatExperienceSettlement.Apply`: 행 대상에 `!StageRecord.IsEmpty`(Q-S6).

시험:

- 새 `T/CombatStageRecordTests.cs`(Core): 공격(넘친 피해 제외·처치 1·적과 메인 기록 비어 있음), 보상 제외 적(피해는 셈·처치는 안 셈), 적 공격(받은 피해 = 실제 HP 감소, 체력 5면 5), 배치 지뢰 처치(처치·넣은 피해가 같은 행동자, 자기 피해는 받은 피해만), 이동 2칸(이동자만 2), 추락 사망(기록 없음), 승리 영수증(행 기록 사본·기록만 있는 비참여자 행).
- `T/CoreIntegration/CombatRewardPublicationTests.cs`에 `StageRecord_AccumulatesPerCommand_IsCopiedToTheReceipt_AndEndsWithTheBattle` 추가: 두 명령에 걸친 누적·`CharacterData.StageRecord`·줄인 기록 게시 거절(저장소 그대로)·영수증 행·`EndBattle` 뒤 캐릭터 없음·영수증 유지. 도우미 `WithCharacters`.
- `T/CombatCommandTestData.With(CharacterCombatFacts …)`: 사실을 다시 만들 때 기록도 넘김(지금 시험은 모두 빈 기록이라 결과는 같음).

계획과 다른 점:

- 피해 항목 값 `CombatStageDamageEntry`는 T11 파일이 아니라 경계 파일(`CombatDamageApplication.cs`)에 두었다. 만드는 곳과 같은 곳이다.
- 증분 묶음은 별도 누적 클래스 대신 `CharacterStageRecord.Add`를 그대로 썼다(넘침 규칙이 한 곳).
- T15의 "아군이 원인인 피해가 아군에게 들어감" 사례는 배치 지뢰 사례가 대신한다(행동자 자신이 지뢰 범위에 있으면 받은 피해만 늘고 넣은 피해는 적 10만).
- 계획의 `IntimidationAttackTests.cs:163` 우려는 해당 없음: 그 시험은 `Finish`를 거치지 않는 `CombatDebuffApplication.CompleteAttack`만 부른다.

기존 시험 영향(코드 대조, 실행 미확인): `Characters.Characters, Is.Empty` 단언들은 거절 또는 피해·이동·처치가 없는 명령이라 영향 없음으로 봤다. 사건 안 사실(`CombatCharacterEvent.Final`)에는 그 명령의 기록 증분이 들어가지 않는다(획득 경험치와 같은 방식). 사건 사실과 변경 최종 사실을 서로 비교하는 시험이 있으면 그 차이로 실패할 수 있다.

확인(사용자): `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체.

#### S-2 확인 중 고친 것(2026-10-27)

1. 컴파일 오류 CS0103: `CombatStageRecordTests`에 `Contracts.Results` using이 빠짐 → 추가.
2. `CombatKillRewardTests.FinalKillDuringRoarMineReaction_MergesGrantAndVictoryExperienceOnce:234`(`Single()` 원소 여럿): Q-S6으로 기록만 있는 아군도 행을 받아 행이 하나가 아니게 됨(설계대로 바뀐 동작). 확인 내용("경험치 20을 받은 행이 하나")은 그대로 두고 `Single(c => c.TotalExperience > 0)`로 맞춤.
3. 새 통합 시험 `StageRecord_…:106` NRE: 이 시험 전투에는 클리어 보상이 없어 승리 완료에 보상 영수증이 없음(시험 구성 실수). `RewardSource`·`Create`에 선택 인자 `clearRewards`(기본 없음 — 기존 시험 그대로)를 더하고, 이 시험만 `new StageClearRewardSpec(0, 0, 0)`으로 만듦.

S-2 확인 완료(2026-10-27, 사용자 "Core·CoreIntegration 시험 모두 통과").

### S-3 완료(2026-10-27, 문서만)

- T16: View 전달 메모는 채팅에 붙여넣기용 글로만 썼다(파일 없음, 미전달). 내용: 읽는 곳 두 개, 네 값 정의, 사건 안 사실에는 그 명령의 증분이 없다는 점, 영수증 생성자 끝 선택 인자, `PullAndMarkPresentationTests` 오디오 가짜·CoreIntegration asmdef `FMODPlus` 참조.
- T17: 설계 문서 상태줄과 이 계획 상태줄.

**작업 끝(S-1~S-3).** 남은 것: 사용자 커밋(새 `.cs` 다섯 — `CharacterStageRecord`·`CombatDamageCredit`·`CombatStageRecordSettlement`·`CharacterStageRecordTests`·`CombatStageRecordTests` — 를 Unity가 만든 `.meta`와 함께), View 메모 전달, 경험치 공식(기획 확정 뒤 별도 설계, 고칠 곳은 `CombatExperienceSettlement.Apply`가 `facts.StageRecord`를 읽는 자리).
