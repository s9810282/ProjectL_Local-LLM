# CombatViewBuffer 연결을 위한 Core 측 요청

- 작성일: 2026-09-29
- 작성: View 작업(스테이지 내부 담당)
- 전달 대상: Core 작업자(천명님)
- 목적: 천명님 계획의 **G5 "View 입력/결과 계약"** 중 입력 쪽 답을 전달한다. 입력을 처리하는 데 Core 측에서 필요한 것을 요청한다.
- 상태: 설계 확정 전 요청이다. 아래 모양은 제안이며, 이름과 API는 Core 쪽 구조에 맞게 정하면 된다. 이 문서 작성으로 코드가 바뀌지는 않았다.

관련 문서

- 설계: [CombatViewBuffer 설계](../specs/2026-09-29-combat-view-buffer-design.md)
- 계획: [CombatViewBuffer 구현 계획](../plans/2026-09-29-combat-view-buffer-plan.md)
- 천명님 문서
  - [View 입력 Buffer 제안](2026-09-29-view-input-buffer-core-contract-ko.md)
  - [캐릭터 Buffer 이전 계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md)(G5, Task 7·8)

## 1. 한 줄 요약

View는 전투 입력을 **`CombatViewBuffer`** 하나에 쓴다. Core 측은 이를 읽기만 한다. 처리 결과에 **번호**가 붙어 있어야 View와 Flow가 이어진다. 그래서 결과 순번, 처리한 명령 번호, 미리보기 응답 번호, 현재 국면, 사건 목록, 처리 서비스를 요청한다.

## 2. 천명님 제안에서 정해진 것

| 천명님 제안 | 이번 설계 |
|---|---|
| 입력은 StageInputBuffer / CombatInputBuffer 둘로 나눈다 | 전투 쪽만 만든다. 이름은 **`CombatViewBuffer`**다. StageInputBuffer(스테이지 맵·상점·회복)는 이번 범위가 아니다 |
| Core는 읽기만, 입력 제거는 View/Framework | 그대로 따른다. Core 측 읽기 인터페이스에는 값을 바꾸는 메서드가 없다 |
| 입력 번호와 결과 대응(협의 필요) | **번호는 Buffer가 발급**한다. 1부터 계속 커지며 다시 쓰지 않고, 전투가 바뀌어도 되돌리지 않는다. 명령과 미리보기가 같은 카운터를 쓰므로 번호 하나로 식별된다 |
| 처리 전 입력 보관 | 실행 명령은 번호 순서대로 쌓는다. View는 Core가 처리했다고 알린 번호 이하만 지운다 |
| 중복 등록 방지 | Core 측이 **마지막 처리 번호 하나**만 기록한다(R2). Job은 Execute할 때 "그 번호보다 큰 첫 명령"을 읽는다. 같은 Job이 두 번 등록돼도 두 번째는 새 명령이 없어 그냥 끝난다 |
| 미리보기는 최신만 | 한 칸만 두고 덮어쓴다. 덮어쓸 때마다 새 번호를 받는다 |
| 애니메이션 완료 통지는 필수 아님 | **넣는다.** View가 "결과 N번까지 연출을 끝냈다"를 기록한다. 전투 Flow가 **연출 대기 Job**으로 이를 기다린다. Core 규칙이나 Core 쪽 Job에는 영향이 없다 |

## 3. View가 보내는 입력

손패·필드 명령은 `EntityId`를 쓴다(천명님 계획 Task 8과 같다). 조준은 Contracts의 `AttackAim`을 그대로 쓴다.

| 실행 명령 | 값 | 현재 Core 진입점 |
|---|---|---|
| `PlaceMainCharacter` | 칸, 방향 | `TryPlaceInitialMainCharacter` |
| `PlaceUnit` | 개체 ID, 칸, 방향 | `TryPlaceOnCombat` |
| `ReturnUnit` | 개체 ID | `TryReturnToAvailableReserve` |
| `EnterCombat` | 없음 | `TryEnterCombat` |
| `Move` | 개체 ID, 칸 | `TryMove` |
| `Attack` | 개체 ID, `AttackAim` | `TryAttack(attackerId, aim)` |
| `EndUnitTurn` | 없음 | `EndUnitTurn` |
| `DrawCharacter` | 없음 | `TryDraw` |

| 미리보기 요청 | 값 | 현재 Core 진입점 |
|---|---|---|
| `MainCharacterPlacement` | 칸, 방향 | `PreviewInitialMainCharacterPlacement` |
| `UnitPlacement` | 개체 ID, 칸, 방향 | `PreviewCombatPlacement` |
| `MoveRange` | 개체 ID | `GetMovementRange` |
| `Move` | 개체 ID, 칸 | `PreviewMove` |
| `AttackRange` | 개체 ID | `GetAttackRange` |
| `Attack` | 개체 ID, `AttackAim` | `PreviewAttack(attackerId, aim)` |

View가 보내지 않는 것은 천명님 제안 §10과 같다. 체력·비용·가능 여부·피해량·승패, DB 행, Snapshot은 보내지 않는다.

Core 측이 읽을 인터페이스(제안):

```csharp
public interface ICombatViewBufferReadSink : IServiceModule
{
    IReadOnlyList<CombatViewCommand> Commands { get; }                        // 번호 오름차순
    bool TryGetNextCommand(int handledThrough, out CombatViewCommand command);
    bool HasPreview { get; }
    CombatViewPreviewRequest Preview { get; }
    int PresentedResultSequence { get; }                                      // View가 연출을 끝낸 결과 순번
}
```

## 4. 전투 Flow가 입력을 처리로 넘기는 방식

전투 Flow·`CombatPlan`·전투 Job은 View 작업(스테이지 내부 담당)이 맡는다. Core 측에는 이 Job들이 부를 **서비스**와 **읽을 값**만 요청한다.

```text
CombatLifeCycle.OnTick (Runner가 비어 있을 때만)
  전투 종료                                  → 아무것도 하지 않음
  자동 진행 국면(적 턴·타임라인·기믹)          → Register(Combat, CombatAdvance)
  입력 국면 + 처리하지 않은 명령 있음          → Register(Combat, CombatCommand)
  입력 국면 + 새 미리보기 요청                → Register(Combat, CombatPreview)

CombatCommand : [명령 처리 Job] → [연출 대기 Job]
CombatAdvance : [진행 Job]      → [연출 대기 Job]
CombatPreview : [미리보기 Job]
```

한 명령의 흐름:

```text
1. View : Submit(Move(unit 7, (3,4)))                       → 명령 42
2. Flow : 입력 국면, 42 > 마지막 처리 41                     → Register(Combat, CombatCommand)
3. Job  : 명령 42를 Core 처리 서비스에 넘김
          → Core 측이 상태 갱신 + 결과 118 기록 + 마지막 처리 = 42   ← 한 번의 호출
4. Job  : 연출 대기 — 확인 값 117 < 118 이면 다음 Tick에 다시 확인
5. View : 결과 118 읽음 → 명령 42 이하 지움 → 연출 → 확인 값 = 118
6. Flow : 다음 Tick에 국면을 다시 보고 다음 등록
```

거절도 결과다. 거절된 명령도 결과 순번을 받고 마지막 처리 번호가 올라간다. 자동으로 다시 실행하지 않는다.

## 5. 요청

| 번호 | 요청 | 쓰는 곳 | 막히는 작업 |
|---|---|---|---|
| **R1** | **결과 순번.** 처리 결과마다 1부터 계속 커지는 번호를 붙이고, 전투가 바뀌어도 되돌리지 않는다. 사용자 명령에서 나온 결과면 **명령 번호**를, 진행(적 턴 등)에서 나온 결과면 0을 적는다. **최신 결과 순번**을 읽을 수 있게 한다 | 연출 대기 Job의 완료 조건. View가 결과와 명령을 대응시킴 | 계획 Phase 5 |
| **R2** | **마지막으로 처리한 명령 번호**를 기록하고 읽을 수 있게 한다. 거절도 처리로 센다 | Flow 등록 판단, 명령 처리 Job의 입력 선택, View의 입력 정리 | Phase 5 |
| **R3** | 미리보기 결과에 **요청 번호**를 적고, 마지막으로 답한 요청 번호를 읽을 수 있게 한다 | Flow의 미리보기 등록 판단, View의 오래된 결과 버리기 | Phase 5 |
| **R4** | **현재 국면**을 읽을 수 있게 한다: 플레이어 입력 대기(배치 단계 포함) / 자동 진행 필요 / 전투 종료 | Flow의 분기 기준. View가 규칙으로 판단하지 않게 한다 | Phase 5 |
| **R5** | 결과에 한 명령 안에서 **일어난 일을 순서대로 담은 목록**을 넣는다. 예: 이동, 공격, 피격, 사망, 반격, 영혼 생성·획득, 승리, 성배 회복. 설계 §10 공격 흐름처럼 한 명령에 여러 결과가 이어질 때 그 순서를 담는다 | 연출. 상태 Buffer에는 최종 상태만 남고, 연출에는 순서와 타격 시점이 필요하다 | 연출 연결(다음 계획) |
| **R6** | Systems에 **처리 서비스**를 둔다. 명령 하나, 진행 한 걸음, 미리보기 하나를 각각 **한 번의 호출**로 처리하고, 상태·결과·R1~R3 값을 한 번에 기록한다. Task 7의 `CombatCommandProcessor`를 감싸는 형태로 예상한다 | 명령 처리·진행·미리보기 Job이 부른다 | Job 몸통 연결(다음 계획) |

R1~R4를 어디에 둘지(결과 Buffer, 전투 상태 Buffer, 별도 Buffer)는 G1과 함께 Core 쪽에서 정하면 된다. **View는 그 값을 읽기만 한다.**

## 6. 요청하지 않는 것

- Core가 `CombatViewBuffer`를 수정하거나 입력을 제거하는 것
- 연출 완료를 Core 규칙이 기다리는 것. 기다림은 Flow의 Plan 순서(연출 대기 Job)가 맡는다
- Run ID·Run 버전·락·일반 트랜잭션·새 공용 실행 프레임워크
- 입력 번호 발급. 발급은 `CombatViewBuffer`가 한다

## 7. 의견을 여쭙고 싶은 것

1. **R1~R4를 언제 받을 수 있는지.** 이 넷이 없으면 전투 Flow(`OnTick`)와 연출 대기 Job을 만들 수 없다. 늦어진다면 View 쪽에서 **필요한 값만 담은 읽기 인터페이스를 먼저 정의**하고, Core 측이 나중에 구현하는 방법도 있다. 이 방법이 괜찮은지 의견을 부탁드린다(아직 View 쪽에서도 확정하지 않았다).
2. **R4의 국면 구분**이 Core 흐름과 맞는지. 예를 들어 기믹 발동 중, 전투 진입 직후 적 선공 등이 "자동 진행"에 모두 들어가는지 확인하고 싶다.
3. **R5의 사건 목록**을 Task 7의 `CombatCommandResult`에 담을 수 있는지. 옛 `StageSessionHost`가 결과 객체에서 발행하던 사건이 기준이다(UnitMoved, UnitAttacked, UnitDamaged, UnitDied, TraitActivated, StageGimmickActivated, CombatFinished 등).
