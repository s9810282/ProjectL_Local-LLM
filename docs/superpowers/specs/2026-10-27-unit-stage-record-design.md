# 유닛별 스테이지 기록(받은 피해·넣은 피해·이동 거리·처치 수)

Date: 2026-10-27
Status: 결정 완료(2026-10-27 사용자 "Q-S1~Q-S7 권장안대로") — §8 모두 권장안(A). 단 Q-S7은 Framework 시험을 고치지 않는다는 지시에 따라 **B(끝 선택 인자)로 바뀜**(계획 TP-1 a). Task 계획 [unit-stage-record-plan](../plans/2026-10-27-unit-stage-record-plan.md) S-1~S-3 구현 완료(2026-10-27, Core·CoreIntegration 시험 통과 — 사용자). 경험치 공식은 기획 뒤 별도.
Source baseline: `feature/Cheon/Core/NewCombatSytem`, HEAD `4640511d`, clean working tree.

경로 약어: `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `B` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`.

## 1. 요구와 범위

사용자 요구(2026-10-27):

- 유닛마다 한 스테이지 동안의 네 값을 저장한다.
  1. 받은 피해
  2. 넣은 피해
  3. 이동한 거리
  4. 적을 처치한 횟수
- 스테이지가 끝나 이 값으로 경험치를 준 뒤 초기화되는 수명을 가진다.
- 나중에 스테이지마다 이 값으로 경험치를 다르게 줄 예정이지만 **공식은 아직 기획되지 않았다.**
- 이번 범위는 **저장하는 기능과 열람하는 기능**까지다.

범위 안: Core 계약·집계·Buffer 저장 규칙·열람 경로.
범위 밖: 경험치 공식과 그 SO 값(기획 뒤), 화면 표시(View 작업자, §9), 적 유닛 기록, 비전투 스테이지(상점·강화·이벤트에는 기록할 행동이 없다).

이 문서의 "스테이지"는 전투 노드 하나 = 전투 하나다. 코드의 전투 수명(`PrepareBattle` → 명령들 → 완료 → `EndBattle`)과 같다.

## 2. 코드에서 확인한 사실

| 출처 | 확인한 것 | 이 설계에 주는 뜻 |
|---|---|---|
| `C/Contracts/Combat/CharacterCombatFacts.cs`, `CharacterExperienceFacts.cs` | 아군 전투 사실이 이미 **이번 전투 획득 경험치 `Experience.Earned`** 를 가진다. "전투 동안만 살고 전투가 끝나면 사실과 함께 버려진다", "보유 개체만 0이 아닌 값". 사실은 `With`/`Rebuild` 한 경로로만 복사되고, 필드를 더하면 `SameValues`·`Rebuild`·`ContractShapeGuardTests`를 함께 고친다는 경고가 있다. | 요구한 수명과 **같은 수명·같은 대상**의 선례가 있다. 새 기록도 같은 자리에 두면 수명 관리 코드가 새로 생기지 않는다. |
| `B/Character/CharacterBuffer.cs:405-412, 428, 566, 674` | Buffer가 `Earned`에 대해 세 규칙을 지킨다: 명령 게시로만 바뀜, 줄지 않음, 새 전투 사실(준비·생성)은 0에서 시작. `EndBattle`이 전투 기록을 모두 지운다(`:594`). | 같은 규칙을 기록에 적용하면 "초기화"는 `EndBattle`이 이미 한다. |
| `C/Application/Combat/CombatDamageApplication.cs` | **캐릭터 피해 1회의 유일한 적용 경계**. 아이템 단계 뒤 `RemainingDamage`(HP로 자르기 전)와 `ActualHealthLoss`(실제 HP 감소)를 구분해 돌려준다. 출처 `CombatDamageOrigin.CreditedCharacterId`(가해 캐릭터)를 받는다. 대상별 누계 장부(`_appliedDamage`·`_recovery`)를 이미 가진다. | 받은 피해·넣은 피해를 한 곳에서 잴 수 있다. |
| `C/Application/Combat/MovementProjection.cs:196-215` | 추락(`ApplyFall`)은 이 경계를 **거치지 않고** 남은 체력만큼 바로 죽인다("추락은 피해·장애물 반응을 거치지 않는다"). | 추락을 받은 피해로 셀지 정해야 한다(Q-S3). |
| `C/Application/Combat/MovementProjection.cs:44, 126-133, 367-372` | 자발 이동은 `Enter(cell)`이 칸마다 `_entered`에 넣고 `DistanceSpent = _entered.Count`. 넉백·끌어오기·토네이도 같은 강제 이동은 `RecordForcedMove`로 따로 간다. | 이동 거리 = 자발 경로 칸 수가 이미 한 값으로 있다. 강제 이동 포함 여부는 Q-S4. |
| `C/Application/Combat/Rewards/ConfirmedKillCalculator.cs` | 새로 죽은 적마다 경험치를 받는 아군을 고른다. 규칙(`Recipient`)은 **기록된 첫 가해자 → 가시심장 반사의 맞은 아군 → 플레이어 명령의 행동자** 순서. 보상 정의가 `IsEligible == false`(명시적 제외)인 적은 목록에서 빠진다. 이 목록의 수가 Run 처치 수(`RunProgressBuffer.EnemiesDefeated`)로 쌓인다(`B/Core/BattleTransactions.cs:215`). | 처치 수는 이 목록으로 세면 Run 처치 수와 정의가 같다(Q-S5). |
| 같은 파일 + `ThornHeartAttackScope.cs:66`, `AttackForcedMovement.cs:62`, `MovementEnvironmentCalculator.cs:52·111·136` | 가시심장 반사·넉백으로 밟은 가시·환경 피해는 출처의 `CreditedCharacterId`가 **null**이다. 처치는 위 대체 규칙으로 행동자 등에게 귀속되지만, 출처만 보면 가해자가 없다. | "넣은 피해"를 출처만으로 세면 **처치는 받았는데 넣은 피해는 0**인 유닛이 생긴다. 귀속 규칙을 처치와 공유해야 한다(D4). |
| `C/Application/Combat/CombatCommandCalculator.cs:89-175` (`Finish`) | 명령 하나의 마지막 정산 지점. 처치 수집 → 경험치 정산(`CombatExperienceSettlement.Settle`) → 변경 게시 순서. 호출처는 `MovementProjection.Complete`(모든 이동·공격·배치·턴 경로)와 `BossRoarCalculator`의 초기화 단계(피해 없음) 둘. 적 턴은 단계별 계산을 `CombatCommandComposition.Merge`로 잇고, 다음 단계는 앞 단계의 최종 사실에서 시작한다. | 명령마다 한 번 기록을 더하면 적 턴·포효 반응도 자연히 이어진다. |
| `C/Application/Combat/Experience/CombatExperienceSettlement.cs` | 승리 단계에서 보유 기록에 경험치를 반영하고 캐릭터별 영수증 행 `CharacterRewardReceipt`(처치·클리어 경험치, 레벨 전후)를 만든다. 행 대상 = 클리어 참여자 또는 처치 경험치가 있는 보유 아군. | 공식이 정해지면 이 함수가 기록을 읽는다. 지금은 읽지 않는다. |
| `C/Contracts/Combat/BattleRewardReceipt.cs`, `B/.../CombatExitJobs.cs:131`, `GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:107` | 이탈 순서는 `EndBattle` → 결과 화면. 결과 화면은 전투 사실이 사라진 뒤 완료 기록의 `Rewards.Characters`를 읽는다. | 전투 뒤 열람은 영수증 행에 기록 사본이 있어야 한다. |
| `B/Character/CharacterData.cs` | View가 읽는 캐릭터 조회값. 전투 사실을 감싸고 필요한 값을 속성으로 낸다(`Experience`는 아직 내지 않음). | 전투 중 열람은 속성 하나로 된다. |

위 사실은 코드를 읽어 확인한 것이다. Unity·시험은 돌리지 않았다.

## 3. 접근 비교

| 안 | 내용 | 판단 |
|---|---|---|
| A. 적용 경계가 사실에 바로 씀 | `Apply` 안에서 받은 사람·가해자 사실의 기록을 `With`로 바꿔 공유 사전에 넣는다. | **기각.** 지금 경계는 받은 사람 사실만 사전에 쓴다. 가해자 사실까지 쓰게 하면, 공격·특성·아이템 경로가 들고 있던 가해자 사본을 나중에 사전에 다시 쓸 때 기록이 지워질 수 있다. 그런 경로가 없는지 판단하려면 모든 공격·이동 경로의 내부를 읽어야 한다(이번 조사에서 모두 확인하지는 않음). |
| B. Finish에서 사건 목록으로 다시 계산 | `HealthChanged`·`Died` 사건의 체력 전후와 출처로 피해를 다시 센다. | **기각.** 피해 직후 아이템 회복이 두 번째 체력 사건을 내고, 넉백 가시는 출처 여러 개를 한 사건에 묶어 피해 1회와 사건이 1:1이 아니다. 종료 체력 정산·최대 체력 변경도 체력 사건이다. 사건 의미가 바뀔 때마다 기록이 깨진다. |
| C. Buffer에 따로 저장소 | 보유 ID별 기록을 별도 Buffer(또는 Combat Buffer 칸)에 두고 명령 거래가 준비·설치한다. | **기각.** "이번 전투의 아군 개체 사실"의 두 번째 원본이 된다. 전투 준비·생성·종료·거래 원자성을 따로 맞춰야 하며, 같은 수명의 `Earned`와 다른 길을 걷는다. |
| **D. 명령 동안 장부로 모으고 Finish 한 곳에서 사실에 더함** (권장) | 적용 경계·이동 투영이 증분만 모은다. `Finish`가 처치 수집 직후 한 번, 최종 사실에 더한다. 저장은 전투 사실의 새 묶음, 수명·Buffer 규칙은 `Earned`와 같다. | 덮어쓰기 위험 없음(마지막에 한 번 씀). 원본은 전투 사실 하나. 수명 코드 새로 없음. |

## 4. 설계 결정

### D1. 저장 위치 — 전투 사실의 새 묶음 `CharacterStageRecord`

- 새 불변 계약 `C/Contracts/Combat/CharacterStageRecord.cs`: `DamageTaken`, `DamageDealt`, `DistanceMoved`, `Kills`(모두 `int`, 0 이상), `Empty`, 더하기 함수(넘치면 `checked` 예외), `SameValues`.
- `CharacterCombatFacts`에 `StageRecord` 속성. 생성자 끝 선택 인자 `stageRecord = null`(= `Empty`)은 이 클래스의 기존 관례(모든 묶음이 끝 선택 인자)를 따른다. `SameValues`·`With`·`Rebuild`·`ContractShapeGuardTests` 목록에 더한다(파일 머리 경고대로).
- 생성자 검사: `Empty`가 아니면 보유 개체여야 한다(`Earned`와 같은 규칙, 적·소환물은 늘 `Empty`).
- 경험치 묶음과 합치지 않는다. 경험치는 "보상", 기록은 "행동 사실"로 바뀌는 이유가 다르다. 공식이 생기면 기록 → 경험치로 한 방향만 읽는다.

### D2. 대상 — 보유 아군(메인·서브)만

경험치를 받는 개체가 보유 아군뿐이고(`CharacterExperienceFacts` 규칙), 적 기록을 쓸 기획이 없다. 손패에만 있던 서브는 0으로 남는다. 손패로 복귀하거나 죽어도 같은 전투 사실이라 기록은 유지된다.

### D3. 네 값의 정의(권장안, 확정은 Q-S1~Q-S5)

| 값 | 권장 정의 | 원천 |
|---|---|---|
| 받은 피해 | 피해 적용 경계 1회마다 **실제 HP 감소**(`ActualHealthLoss`, 아이템 감소 뒤·HP로 자른 값)를 받은 아군에 더함. 회복은 빼지 않는다. 추락은 포함하지 않음. | `CombatDamageApplication.Apply` |
| 넣은 피해 | 같은 피해 1회가 **적**에게 들어갔을 때, D4의 귀속 아군에게 실제 HP 감소를 더함. 아군·구조물·장애물 피해는 세지 않음. | 같은 곳 |
| 이동 거리 | 그 유닛이 행동자인 명령의 **자발 경로 칸 수**(`DistanceSpent`). 강제 이동·배치·손패 복귀는 세지 않음. | `MovementProjection` |
| 처치 수 | `ConfirmedKillCalculator`의 귀속 처치(`CreditedKill`)마다 받는 아군에 1. Run 처치 수와 같은 정의. | `ConfirmedKillCalculator.Collect` |

### D4. 귀속 규칙 하나 — `CombatDamageCredit`

`ConfirmedKillCalculator.Recipient`(첫 가해자 → 가시심장 반사 → 명령 행동자)를 `C/Application/Combat/Rewards/CombatDamageCredit.cs`로 옮겨, 처치와 넣은 피해가 같은 함수를 부른다. 함수 본문·순서는 그대로다(처치 동작 같음).

- 근거: "이 피해는 어느 아군의 것인가"는 한 사실이다. 따로 두면 넉백 가시·반사 처치에서 처치 수와 넣은 피해가 서로 다른 사람을 가리킨다(§2 표).
- 그래서 적용 경계는 귀속을 판단하지 않고 **피해 항목**(받은 개체, 사건 출처 목록 = `eventOrigins ?? [origin]`, 실제 HP 감소)만 남긴다. 반사 문맥과 행동자는 `Finish`에서야 모두 보이므로 판단은 거기서 한다.

### D5. 집계 흐름 — 명령마다 한 번

```text
CombatDamageApplication.Apply ──피해 항목 기록──┐
MovementProjection (자발 경로 칸 수) ─────────┤
                                              ▼
MovementProjection.Complete ──CombatStageTally──► CombatCommandCalculator.Finish
                                                    │ 1. 처치 수집 (ConfirmedKillCalculator)
                                                    │ 2. CombatStageRecordSettlement.Record   ← 새
                                                    │      항목 → 받은/넣은 피해(CombatDamageCredit)
                                                    │      행동자 → 이동 거리, 처치 → 처치 수
                                                    │      보유 아군 사실에 With(stageRecord:)
                                                    │ 3. CombatExperienceSettlement.Settle (승리면 영수증에 사본)
                                                    ▼
                                         CharacterChanges → Buffer 게시
```

- 장부 `CombatStageTally`(내부 값 묶음): 피해 항목 목록 + 행동자 이동 칸 수. 적용 경계가 피해 항목을, `Complete`가 이동 칸 수를 채운다. 공격 투영과 도착 투영은 이미 같은 경계 인스턴스를 쓰므로 한 명령의 피해가 한 장부에 모인다.
- `Finish`의 새 끝 선택 인자 `tally`가 null이면 더하지 않는다(포효 초기화 단계는 피해가 없다).
- 적 턴: 단계마다 `Finish`가 기록을 더하고, 다음 단계는 그 사실에서 시작하므로 `Merge` 결과의 최종 사실에 누계가 남는다.
- 미리보기도 같은 계산을 지나지만 게시되지 않으므로 저장되지 않는다.
- 새 정산 함수 자리: `C/Application/Combat/Experience/CombatStageRecordSettlement.cs`(기록은 경험치 정산의 입력이라 같은 폴더).

### D6. 수명 — Buffer 규칙은 `Earned`와 같게

| 시점 | 동작 | 지키는 곳 |
|---|---|---|
| 전투 준비·전투 중 생성 | 모든 사실 `Empty`로 시작 | `CharacterBuffer` 새 전투 사실 검사(`RequireNoEarnedExperience`를 "획득 경험치·기록 없음" 검사 하나로 넓힘) |
| 명령 게시 | 네 값 각각 줄지 않음 | `CharacterBuffer` 변경 검사(`:405` 옆) |
| 다른 쓰기 경로(완료 게시 등) | 바꿀 수 없음 | 같은 곳 |
| 승리 정산 | 기록 사본을 영수증 행에 넣음(D7) | `CombatExperienceSettlement` |
| `EndBattle` | 전투 사실과 함께 사라짐 = **초기화** | 기존 `CharacterBuffer.EndBattle` |
| 패배·포기 | 영수증 없이 사라짐(경험치도 주지 않는 기존 규칙) | 기존 |

새 초기화 코드는 없다. "경험치를 준 뒤 초기화"는 승리 정산(같은 명령) → `EndBattle` 순서로 이미 성립한다.

### D7. 열람 — 두 시점, 새 서비스 없음

| 시점 | 읽는 곳 | 바뀌는 것 |
|---|---|---|
| 전투 중(유닛 정보 창 등) | `ICharacterBufferReadSink.Characters` / `TryGet` → `CharacterData.StageRecord` | `B/Character/CharacterData.cs` 속성 한 줄 |
| 전투 뒤(결과 화면) | `BattleCompletionChanges.Rewards.Characters[i].StageRecord` | `CharacterRewardReceipt`에 `StageRecord` 칸(생성자 끝 선택 인자, Q-S7 B) |

경험치 공식이 생기면 `CombatExperienceSettlement.Apply`가 `facts.StageRecord`를 읽고, 같은 행에 그 결과 경험치를 넣는다. 영수증 행 대상은 Q-S6.

## 5. 바뀌는 파일

| 묶음 | 파일 | 내용 |
|---|---|---|
| 계약 | `C/Contracts/Combat/CharacterStageRecord.cs`(새, + `.meta`) | D1 |
| | `C/Contracts/Combat/CharacterCombatFacts.cs` | 속성·생성자·`SameValues`·`With`·`Rebuild`·검사 |
| | `C/Contracts/Combat/CharacterRewardReceipt.cs` | `StageRecord`·`SameValues` |
| 집계 | `C/Application/Combat/CombatDamageApplication.cs` | 피해 항목 기록·꺼내기 |
| | `C/Application/Combat/Rewards/CombatDamageCredit.cs`(새, + `.meta`) | `Recipient` 옮김 |
| | `C/Application/Combat/Rewards/ConfirmedKillCalculator.cs` | 옮긴 함수 호출만 |
| | `C/Application/Combat/Experience/CombatStageRecordSettlement.cs`(새, + `.meta`) | D5 정산 + `CombatStageTally` |
| | `C/Application/Combat/MovementProjection.cs` | `Complete`가 장부를 넘김 |
| | `C/Application/Combat/CombatCommandCalculator.cs` | `Finish` 인자 하나·호출 한 줄 |
| | `C/Application/Combat/Experience/CombatExperienceSettlement.cs` | 영수증 행에 사본·행 대상(Q-S6) |
| | `C/Contracts/Combat/CombatRewardConsistency.cs` | 영수증 행의 기록 = 그 보유 아군의 최종 사실 기록(Buffer 게시 전 대조, 이미 있는 행 대조 옆) |
| Buffer | `B/Character/CharacterBuffer.cs` | D6 검사 |
| | `B/Character/CharacterData.cs` | `StageRecord` 속성 |
| 시험(Core 두 어셈블리만) | `ContractShapeGuardTests`, `CharacterCombatFactsSameValuesTests`, `ContractWithPreservationTests`, `BattleRewardReceiptTests`·`CombatRewardConsistencyTests`·`CombatRewardSettlementTests`(영수증 생성 호출), 새 기록 정산 시험 | 필드 목록·호출 맞춤·정의 사례 |

바뀌지 않는 것: 공격·이동·특성·아이템·환경 계산 본문, 사건 형식(View 사건 경로 표에 새 줄이 필요 없음), Factory·ServiceKeys·Framework, DB·SO.

## 6. 변경 독립성 점검(대표 변경 사례)

| 변경 사례 | 수정 범위 | 이해 범위 | 검증 범위 | 판단 |
|---|---|---|---|---|
| 기획이 경험치 공식을 정함 | `CombatExperienceSettlement.Apply`(+ 공식 값이 SO면 `LevelProgressionSpec` 경로) | 기록 네 값의 정의(D3)·영수증 행 | 경험치 정산·보상 일관성 시험 | 피해·이동·처치 집계 내부를 몰라도 된다. 필요한 연결만 남음. |
| 새 특성이 새 피해 경로를 만듦 | 없음(경계 `Apply`를 지나면 자동 집계) | "캐릭터 피해는 경계를 지난다"는 기존 계약 | 새 경로 시험에 기록 단언 하나 | 아이템·처치와 같은 기존 계약을 공유. 추락처럼 경계를 우회하면 누락되므로 경계 주석에 기록도 적는다. |
| 이동 거리에 넉백을 넣기로 바꿈(Q-S4 B) | `MovementProjection.RecordForcedMove`에서 장부에 칸 수 더하기 | 강제 이동 칸 수 계산 | 기록 정산 시험·강제 이동 시험 | 한 곳. 공식·피해 집계는 무관. |
| 처치 귀속 규칙을 바꿈 | `CombatDamageCredit` 한 곳 | 귀속 규칙 | 처치 보상·Run 처치 수·넣은 피해 시험 | 처치와 넣은 피해가 함께 바뀌는 것은 같은 규칙이라 필요한 연결. |

확인 수준: 제안상의 예상. 위 경로는 코드에서 확인했고 실행 검증은 없다.

## 7. 원칙 점검

- **SSOT:** 기록 원본은 전투 사실 하나, 영수증은 승리 정산 때 만든 사본(완료 기록과 같은 수명의 읽기 모델). 귀속 규칙은 `CombatDamageCredit` 하나. 피해 1회의 원본은 적용 경계 하나. 이동 칸 수는 기존 `DistanceSpent`.
- **SRP:** 경계는 "피해 1회가 있었다"만 남기고, 누가 받는지·어떻게 더하는지는 정산 함수가, 경험치 환산은 경험치 정산이 맡는다.
- **OCP/독립성:** 새 피해 경로·새 특성은 기록 코드를 고치지 않는다(§6).
- **FW 계약:** 새 서비스·키·Factory 연결 없음. Buffer 쓰기는 기존 명령 거래 안.
- **Core/View 경계:** View는 기록을 읽기만 한다(P6: 규칙 값은 Buffer로).

## 8. 정할 것

| ID | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-S1 | 피해 값은 무엇을 세나 | A 실제 HP 감소(넘친 피해 제외) / B 아이템 뒤 요청 피해(HP로 자르기 전, 넘친 피해 포함) / C 둘 다 저장 | **A.** "체력을 얼마나 깎았나"가 화면 숫자와 맞는다. 기획이 넘친 피해를 쓰겠다면 B로 바꾸는 곳은 정산 함수 한 줄. |
| Q-S2 | 넣은 피해 대상 | A 적만 / B 적 + 구조물·장애물 / C 아군 오사 포함 모든 캐릭터 | **A.** 처치 수와 같은 기준. 구조물 피해는 다른 경로라 B는 집계 지점이 하나 늘어난다. |
| Q-S3 | 추락 사망을 받은 피해로 세나 | A 안 셈(피해가 아님) / B 남은 체력만큼 셈 | **A.** Core가 추락을 피해 경로 밖으로 정의했다. |
| Q-S4 | 이동 거리 | A 자발 경로 칸만 / B 넉백·끌어오기·토네이도 포함 / C 따로 두 값 | **A.** "유닛이 이동한 거리"의 일반적 뜻. 대각선 한 칸 = 1. |
| Q-S5 | 처치 수 | A 보상 자격 처치만(= Run 처치 수 정의) / B 보상 제외 적도 포함 | **A.** 유닛 처치 수 합 = 그 전투의 Run 처치 증가량이 성립. |
| Q-S6 | 결과 영수증 행 대상 | A 지금 대상(클리어 참여자 + 처치 경험치 있음) + **기록이 0이 아닌 보유 아군** / B 지금 그대로 | **A.** 기록이 있는데 행이 없으면 결과 화면에서 열람할 수 없다. 표시 칸 수(4)는 지금 클리어 참여자 기준이라 View 영향은 없다. |
| Q-S7 | 영수증 생성자 | A `stageRecord` 필수 인자(구조 우선) / B 끝 선택 인자 | **A.** 필수로 두면 사본이 빠지는 실수가 컴파일에서 막힌다. 영향: Core 시험 셋 외에 `ProjectL.Tests.Framework`의 `CombatEndUITests`·`CombatRewardExperienceAnimationTests`·`CombatClearRewardIntegrationTests`가 같은 생성자를 불러 컴파일이 깨진다. 그 셋을 호출만 맞출지 사용자 결정 필요. |

## 9. 범위 밖·넘길 것

- View 표시(유닛 정보 창·결과 화면 칸)는 View 작업자 몫. 구현 뒤 Core → View 인계에 "읽는 곳 두 개(D7)·네 값 정의(D3)"를 적는다.
- 경험치 공식·스테이지별 배율 SO는 기획 확정 뒤 별도 설계.
- `CharacterData`는 `Experience`(획득 경험치)도 아직 내지 않는다. 필요하면 같은 때 한 줄로 낼 수 있다(이번 범위 아님, 보고만).

## 10. Phase 안(결정 뒤 Task 계획에서 확정)

1. **S-1 계약·Buffer:** `CharacterStageRecord`, 사실·영수증 칸, Buffer 검사, `CharacterData` 속성. 동작 없음(늘 `Empty`).
2. **S-2 집계:** `CombatDamageCredit` 이동, 경계 피해 항목, 장부, `Finish` 정산, 영수증 사본·행 대상.
3. **S-3 마무리:** Core 시험(두 어셈블리), View 인계 메모.

Phase마다 멈추고 보고한다. 검증은 사용자가 Unity Test Runner로 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 돌린다.
