# 공격 넉백 미리보기 — Core 설계

날짜: 2026-10-27
상태: 설계 확정, Core 구현 완료(2026-10-27, Core·CoreIntegration 시험 통과 — 사용자, 커밋 전, §12). Q-K1~Q-K4 모두 권장안(2026-10-27 사용자). Task 계획은 [attack-knockback-preview-plan](../plans/2026-10-27-attack-knockback-preview-plan.md). View 표시는 남음(§7).
작성 범위: Core(`CoreCaculator`) 공개 계약과 그것을 채우는 Core 내부. View 표시는 View 작업자에게 넘긴다(§7).

`C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`.

## 1. 요구

- 공격 호버(공격 미리보기) 때 **누가, 어느 방향으로, 어디까지 밀리는지**를 View가 그릴 수 있어야 한다.
- 일반 넉백(`TraitKey.Knockback`, 공격자 기준)과 충격 볼트(`ImpactBolt`, 착탄 칸 기준) 둘 다 다룬다. 넉백은 유닛마다 따로 만들지 않고 공용 계산 하나가 처리하므로, 미리보기도 그 공용 계산에서 한 번만 나와야 한다.
- View는 거리·방향·막힘·면역을 다시 계산하지 않는다. 넉백 규칙의 원본은 Core 하나다.

## 2. 지금 코드 (2026-10-27, HEAD `6482e073`, 소스를 읽어 확인했고 실행은 하지 않음)

| 위치 | 지금 동작 |
|---|---|
| `C/Application/Combat/AttackKnockbackCalculator.Resolve` | 넉백 거리를 평가한다(`EvaluateKnockback`, 기여 중 최댓값). 대상을 ID 순서로 하나씩 민다. 일반 넉백의 후보는 피격 기록 대상 전체(`ledger.Roles`), 충격 볼트의 후보는 직접 전달 대상이다. 죽은 대상, 충격 볼트 중심·비적대 대상(`ImpactBoltTraitRule.TryResolveStep`, 비적대 제외는 같은 날 뒤에 걷음 — §4.3), 면역 대상(`KnockbackImmunityPolicy`)은 건너뛴다. 밀린 경로는 `ResolvedAttackDisplacement`(살아서 도착한 대상과 칸 수)로만 돌려주고 시작 칸·경로·멈춘 이유는 버린다. |
| 같은 파일 `KnockbackObstacle` | 장애물(구조물) 넉백. `ForcedDisplacementPath`에서 칸 수(`int`)만 돌려준다. 일반 넉백에서만 돈다. |
| `C/Application/Combat/AttackForcedMovement.ExecuteUnit` | 칸마다 들어갈 수 있는지 검사하고, 지형(낙사·약한 지반)·가시·중간 칸 오브젝트를 정산한다. 결과는 `ForcedDisplacementPath`(`RequestedDistance`·`Steps`·`Stop` = Completed/Blocked/Interrupted, internal)이다. 끌어오기도 같은 함수를 쓴다. |
| `C/Application/Combat/AttackDeliveryExecutor.Execute` | 순서: 직접 피해 → 넉백(`Resolve`) → 가시밭 적용 → 비용 정산 → 피격 체력 고정 → 도착 칸 오브젝트(`ResolveArrivals`) → (실제 공격만) 처치 연쇄·추가 공격. |
| `C/Application/Combat/CombatAttackCalculator.Execute` | 공개 미리보기(`publicPreview: true`)도 같은 길을 지난다. 다른 점은 둘이다. 환경이 관찰자 공개분(`Environment.VisibleTo(owner)`)이고, 처치 연쇄·추가 공격·밀린 뒤 후속을 건너뛴다. |
| `C/Application/Combat/FollowUpAttackExecutor.Execute` | 같은 방향 추가 공격은 같은 `world`, 새 `CommandAttackLedger`로 `AttackDeliveryExecutor.Execute`를 다시 부른다. 추가 공격도 넉백할 수 있다(실제 공격만). |
| `C/Application/Combat/AttackResultFactory.Create` | 피격 기록(`ledger`)과 투영(`world`)으로 `AttackResult`를 만들고, 그것으로 `AttackPreview.FromFacts`를 만든다. 끌어오기 기록(`pull`)은 인자로 받아 결과와 미리보기에 함께 싣는다. |
| `C/Contracts/Results/AttackPreview` | 넉백 관련 공개 값은 대상별 예상 피해(경로 가시·지형 피해 포함)와 공격자의 `KnockbackTraitStatusSnapshot.DistanceCells`뿐이다. `TargetOccupiedCells`는 원래 자리다. **방향·경로·최종 위치·막힘은 없다.** |
| `C/Contracts/Results/AttackPullSnapshot` | 선례: 끌어오기 대상의 `StartAnchor`·`FinalAnchor`·`Path`. `AttackResult.Pull`과 `AttackPreview.Pull` 둘 다에 실린다. View는 이미 이 값으로 끌어오기 미리보기와 착탄 칸을 정한다(`CombatAttackTarget.DeliveryCell`). |
| 미리보기 전달 | `CombatPreviewService` → `CombatPreviewResult.ForAttack(number, AttackPreview)` → Buffer → View(`AttackTargetingInteraction`). `AttackPreview`에 값을 더하면 Buffer 형식을 고치지 않아도 View까지 간다. |

10-26 충격 볼트 설계 §9는 "공개 미리보기에 최종 넉백 위치를 싣지 않고 새 표시용 값도 더하지 않는다"고 적었다. 이 설계는 그 결정을 사용자 요청으로 바꾼다. View 쪽 요청은 [e413008 대응 정리 B6·Q-E5](2026-10-26-combat-view-core-e413008-response-inventory.md)다.

## 3. 방향

**공용 넉백 계산이 대상별 "넉백 기록"을 남기고, 공격 결과 생성이 그것을 `AttackResult`·`AttackPreview`에 싣는다.** 끌어오기 기록(`AttackPullSnapshot`)과 같은 꼴이다.

견준 안:

1. View가 `DistanceCells`와 공격자·착탄 위치로 계산한다 — 버림. 벽·유닛 막힘·면역·중간 칸 사망·처리 순서(앞 대상이 밀린 자리 때문에 뒤 대상이 막힘)를 View에 다시 만들어야 해서 SSOT를 깬다.
2. 미리보기 전용 계산을 따로 만든다 — 버림. 실제 공격과 미리보기가 갈라진다.
3. 미리보기 투영의 이동 사건(`world.Events`의 `Moved`)을 공개한다 — 버림. 미리보기에 사건 목록 전체를 내야 하고, View가 사건을 대상별 경로로 다시 묶어야 한다.
4. **공용 계산이 이미 가진 `ForcedDisplacementPath`를 대상별로 모아 공개 값으로 싣는다 — 고름.** 계산은 그대로 두고, 버리던 값을 남기기만 한다.

## 4. 공개 계약

### 4.1 새 값 `AttackKnockbackSnapshot` (`C/Contracts/Results/`)

```csharp
public enum AttackKnockbackStop
{
    Completed = 1,   // 요청 거리를 다 갔다.
    Blocked = 2,     // 다음 칸이 막혀 그 앞에서 멈췄다(0칸 포함).
    Interrupted = 3, // 넉백 진행이 중단됐다(대상 사망 또는 전투 종료). 대상 사망을 뜻하지 않는다.
    Immune = 4,      // 넉백 면역이라 움직이지 않았다(Q-K3).
}

public enum AttackKnockbackTargetKind { Character = 1, Structure = 2 }

public sealed class AttackKnockbackSnapshot
{
    public EntityId TargetId { get; }
    public AttackKnockbackTargetKind TargetKind { get; }
    public CellCoord StartAnchor { get; }
    public CellCoord Step { get; }              // 한 칸 방향(8방향, 각 성분 -1·0·1, 0벡터 아님)
    public int RequestedDistance { get; }        // 1 이상
    public IReadOnlyList<CellCoord> Path { get; } // 실제로 들어간 칸. 비어 있을 수 있음
    public CellCoord FinalAnchor { get; }        // Path 마지막 칸, 비었으면 StartAnchor
    public AttackKnockbackStop Stop { get; }
}
```

생성자 검사(`AttackPullSnapshot`처럼 잘못된 값은 예외):

- `TargetId`는 Empty가 아니다. `Step`은 성분이 -1·0·1이고 0벡터가 아니다. `RequestedDistance ≥ 1`.
- `Path[i] = StartAnchor + Step × (i+1)`. 끌어오기는 상하좌우 이음만 보지만, 넉백은 한 방향으로 곧게 가므로 더 엄격하게 본다.
- `Path.Count ≤ RequestedDistance`. `Completed`이면 같고, `Blocked`이면 작고, `Immune`이면 0이며, `Interrupted`이면 1 이상이다.
- `FinalAnchor`는 위 규칙대로 정해지므로 생성자 인자로 받지 않고 계산한다. 같은 값을 두 번 받지 않기 위해서다.

`Interrupted`의 뜻(2026-10-27 리뷰 반영): 들어간 칸에서 그 대상이 죽은 경우만이 아니다. 같은 칸 효과(예: 지뢰)가 다른 유닛을 죽여 전투가 끝나도 이동 중인 대상은 살아 있는 채 `Interrupted`가 된다(`AttackForcedMovement.ExecuteUnit`의 `arrival.Terminal` 갈래). 그래서 이 값은 "넉백이 중간에 멈췄다"만 뜻하고 사망 표시로 쓰면 안 된다. 대상이 죽었는지는 대상 미리보기의 `TargetKilled`/`TargetHealthAfter`(경로 중 피해는 피격 체력 고정 전이라 들어간다)나 결과의 최종 캐릭터 사실로 본다. 계약 주석에 이 문장을 그대로 적는다.

다칸 대상(장애물 등)의 최종 점유 칸은 싣지 않는다. View는 현재 점유 칸(Buffer)에 `FinalAnchor - StartAnchor`만큼 더해서 얻는다. 계산할 수 있는 값은 두 번 두지 않는다.

### 4.2 결과·미리보기에 싣기

- `AttackResult.Knockbacks` (`IReadOnlyList<AttackKnockbackSnapshot>`, 없으면 빈 목록). `AttackResult.FromFacts`에 끝 선택 인자 `knockbacks = null`을 더한다. 끝에 선택 인자로 더하므로 기존 호출처(시험 포함)는 고치지 않아도 된다.
- `AttackPreview.Knockbacks`는 `FromFacts`에서 `result.Knockbacks`를 그대로 받는다. `Success`·`SuccessForCell`(옛 생성 길)은 빈 목록이고, `Failure`도 빈 목록이다.
- 미리보기 대응 검사(2026-10-27 리뷰 반영): `AttackPreview.FromFacts(result, targets, ...)`는 대상 목록을 결과와 **따로** 받고, 일반 공격에서는 그것이 `result.Hits`와 같은지 검사하지 않는다. 그래서 결과 쪽 검사만으로는 "넉백 기록의 대상이 미리보기 대상 목록에 있다"가 보장되지 않는다. `AttackPreview.FromFacts`에서 기록 대상마다 `targets`에 같은 `TargetId`가 있는지 검사한다. 경로·중복 검사는 결과에서 했으므로 다시 하지 않는다.
- 순서는 Core가 처리한 순서(대상 ID 오름차순, 캐릭터와 구조물을 하나의 ID 순서로)이다. 앞 대상이 밀린 결과가 뒤 대상의 막힘에 들어가므로, 이 순서도 사실의 일부로 둔다.
- 대응 검사: 기록의 대상은 그 공격의 피격 대상(`Targets`/`Hits`) 안에 있어야 한다. 같은 대상의 기록은 하나다. `PiercingShotDelivery.ValidateCorrespondence`처럼 생성 경계에서 검사한다.

### 4.3 무엇을 기록하나 (기록 규칙)

| 경우 | 기록 |
|---|---|
| 넉백 거리 0(넉백 특성 없음) | 없음(빈 목록) |
| 직접 피해로 이미 죽은 대상 | 없음. 밀 대상이 아니다 |
| 충격 볼트 중심 대상, `TryResolveStep`이 거른 대상(중심을 덮은 유닛·필드 밖·죽은 유닛) | 없음. 이 특성이 밀 대상으로 삼지 않는다. 2026-10-27 사용자 결정으로 진영은 보지 않는다 — 맞은 아군도 적과 같이 밀리고 기록된다(캐릭터 소유자는 Player·Enemy뿐이라 중립 유닛은 없다) |
| 면역 대상(`KnockbackImmunityPolicy`) | `Immune`, 빈 경로 (Q-K3) |
| 첫 칸부터 막힘 | `Blocked`, 빈 경로 (Q-K2) |
| 몇 칸 가고 막힘 / 다 감 | `Blocked` / `Completed` |
| 경로 중 가시·낙사·지뢰 등으로 사망 | `Interrupted`, 죽은 칸까지 |
| 경로 중 칸 효과로 목표 달성·메인 사망이 일어나 전투 종료(대상은 살아 있을 수 있음) | `Interrupted`, 그 칸까지 |
| 앞 대상 처리 중 전투가 끝나 남은 대상을 건너뜀(`arrival.Terminal`) | 건너뛴 대상은 없음 |
| 장애물 넉백(`KnockbackObstacle`) | `Structure` 종류로 같은 규칙 |
| 같은 방향 추가 공격·처치 연쇄의 넉백 | 이 공격의 기록에 넣지 않는다(§5.2) |

넉백 뒤 도착 칸 오브젝트(`ResolveArrivals`)는 위치를 바꾸지 않는다. 도착 칸 오브젝트의 강제 이동 요청은 지금 `NotSupportedException`이다(`MovementEnvironmentCalculator.ResolveObjectsAt`). 그래서 `FinalAnchor`가 이 공격의 넉백 최종 위치다.

## 5. Core 내부

### 5.1 기록 모으기

- `AttackForcedMovement.ExecuteUnit`은 바꾸지 않는다. 이미 `ForcedDisplacementPath`를 돌려준다.
- `AttackKnockbackCalculator.Resolve`: 대상마다 시작 칸·방향을 잡은 뒤 `ExecuteUnit` 결과로 기록을 만든다. 면역은 `continue` 전에 `Immune` 기록을 남긴다. 장애물 경로(`KnockbackObstacle`)는 `int` 대신 `ForcedDisplacementPath`와 방향을 돌려주게 바꾼다.
- 방향 계산 위치(2026-10-27 리뷰 반영, 계획 T2.2와 같게): 일반 넉백의 `ResolveBranchStep`은 면역 검사 **앞으로** 옮긴다. 면역 기록에도 방향이 필요하기 때문이다. 순수 함수이고, 일반 넉백 대상은 공격자 칸 위에 있을 수 없어 0벡터 예외가 새로 생기지 않는다. 충격 볼트 `TryResolveStep`은 이미 면역 검사 앞에 있으므로 그대로 둔다.
- internal `ForcedDisplacementStop` → 공개 `AttackKnockbackStop` 바꾸기는 기록을 만드는 함수 한 곳에서만 한다. 끌어오기는 이 바꾸기를 쓰지 않는다.

### 5.2 기록을 어디에 두나 — 공격 기록(`CommandAttackLedger`)

`CommandAttackProjection`(world)은 추가 공격이 같이 쓴다(`FollowUpAttackExecutor:139`). 여기에 두면 실제 공격에서만 추가 공격 넉백이 섞여 미리보기와 실제가 달라진다. `CommandAttackLedger`는 공격마다 새로 만들고 피격 역할·피해를 이미 공격 단위로 가지므로, 기록도 여기에 둔다(`ledger.AddKnockback(record)`, 같은 대상 두 번이면 예외). `AttackResultFactory.Create`가 `ledger.Knockbacks`를 `AttackResult.FromFacts`에 넘긴다. 추가 공격 ledger의 기록은 `AttackResult`가 없어서 버려진다. 추가 공격의 이동은 지금처럼 `Moved` 사건으로만 나간다.

### 5.3 SSOT 경계

- 칸 단위 사실(재생·최종 캐릭터 위치)의 원본은 지금처럼 `Moved` 사건과 최종 캐릭터 사실이다. View 재생은 계속 사건을 쓴다.
- `Knockbacks`는 "이 공격의 넉백이 대상별로 어떻게 끝났나"의 요약이다. 같은 계산 한 번에서 사건과 함께 만들어지므로 두 원본이 서로 어긋날 수 없다. 끌어오기 `Pull`도 같은 관계다(Q-K1).

## 6. 미리보기가 실제와 다를 수 있는 경우 (지금 계약 그대로)

- 미리보기는 관찰자에게 공개된 환경(`VisibleTo`)으로 계산한다. 숨은 지형·오브젝트 때문에 실제 경로가 달라질 수 있다. 예상 피해도 지금 이렇게 다르므로 넉백도 같은 규칙을 따른다.
- 처치 연쇄·같은 방향 추가 공격과 그 넉백은 미리보기에 없다(지금 예상 피해와 같다).
- 관찰(이 설계 범위 밖): 대상별 `TargetHealthAfter`는 피격 체력 고정 시점 값이라, 넉백 도착 칸 오브젝트(마지막 칸 지뢰 등)의 피해는 들어가지 않는다. 넉백 기록은 위치만 다루므로 이 차이를 바꾸지 않는다. 보이려면 따로 정해야 한다(Q-K4).

## 7. View 인계(이 설계에서 구현하지 않음)

- 읽을 것: `AttackPreview.Knockbacks`(호버), `AttackResult.Knockbacks`(필요하면). Buffer 형식은 바뀌지 않는다.
- 그릴 수 있는 것: 대상별 밀려날 칸·화살표, 막힘(`Blocked`이면서 경로가 요청보다 짧음), 면역 표시, 중간에 멈춤(`Interrupted`). `Interrupted`는 사망을 뜻하지 않는다. 사망은 대상 미리보기의 `TargetKilled`로 본다(§4.1).
- View가 하지 않을 것: 거리·방향·막힘·면역을 다시 계산하기, 재생에서 사건 대신 이 요약 쓰기.
- 공유: View 작업자에게 보낼 짧은 메모(채팅 글). 이 세션은 View 파일을 고치지 않는다.

## 8. 원칙 점검

- **SSOT:** 넉백 규칙의 원본은 공용 계산 하나다. 기록은 그 계산이 버리던 값을 남긴 것이고, View는 계산하지 않는다. `FinalAnchor`는 받지 않고 계산하며, 다칸 점유 칸은 싣지 않는다.
- **SRP:** 무엇을 밀지·방향은 지금 자리(넉백 계산·충격 볼트 규칙)에, 칸 진행은 `AttackForcedMovement`에, 공개 값 조립은 `AttackResultFactory`에 둔다. 새 서비스·새 계산기는 없다.
- **OCP/LSP:** 끝 선택 인자와 빈 목록 기본값은 기존 호출이 그대로 컴파일된다는 근거일 뿐이고, 그것만으로 OCP·LSP가 충족된다고 보지 않는다. 기존 결과의 뜻이 그대로라는 것은 계산 동작이 바뀌지 않았다는 회귀 시험(계획 KP-2 "동작 불변 확인")과 대응 검사로 확인한다.
- **ISP/DIP:** 공개 값은 표시에 필요한 사실만 담고, Core 내부 형식(`ForcedDisplacementPath`)을 드러내지 않는다.
- **FW:** 서비스·모듈·Buffer 등록을 바꾸지 않는다. Core는 계산 결과 값만 늘린다.
- **바뀔 때의 영향:** 넉백 규칙(막힘·면역·방향)을 바꿔도 기록은 따라간다. 같은 계산에서 나오기 때문이다. 새 강제 이동 원인(예: 다른 특성이 미는 경우)이 `AttackKnockbackCalculator`를 지나면 저절로 기록된다. 다른 길로 밀면 그 길에서 기록을 남겨야 하는데, 이것은 계획에서 점검할 항목이다.

## 9. 변경 범위

| 파일 | 변경 |
|---|---|
| `C/Contracts/Results/AttackKnockbackSnapshot.cs` (+ `.meta`) | 새 값·열거 둘 |
| `C/Contracts/Results/AttackResult.cs` | `Knockbacks`, `FromFacts` 끝 선택 인자, 대응 검사 |
| `C/Contracts/Results/AttackPreview.cs` | `Knockbacks`, `FromFacts`가 결과 값을 받음 |
| `C/Application/Combat/AttackKnockbackCalculator.cs` | 기록 만들기, 면역 기록, 장애물 경로 반환형 |
| `C/Application/Combat/CommandAttackLedger.cs` | 기록 목록·추가 함수 |
| `C/Application/Combat/AttackResultFactory.cs` | 기록 넘기기 |

시험(`ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`만):

- 값 검사: 방향·경로 이음·멈춘 이유별 칸 수·빈 경로·잘못된 인자.
- 계산: 일반 넉백(완료·중간 막힘·첫 칸 막힘·면역·경로 중 가시 사망·낙사), 충격 볼트 8방향과 중심 제외, 장애물 넉백, 두 대상 순서 막힘(앞 대상이 민 자리), 앞 대상 처리 중 전투 종료.
- 미리보기와 실제가 같음: 공개 지형만 있는 판에서 `preview.Knockbacks`와 `result.Knockbacks`가 같다.
- 요약과 사건이 같음: 추가 공격이 없는 공격에서 기록 경로 = 그 대상 `Moved` 사건 위치의 순서.
- 미리보기 대응: 기록 대상이 미리보기 대상 목록에 없으면 `AttackPreview.FromFacts`가 거절한다.
- 추가 공격 넉백이 본 공격 기록에 섞이지 않음(실제 공격, **필수**): 추가 공격의 이동 사건은 있지만 본 공격 기록에는 없다.
- `Interrupted` 대상 생존: 지뢰로 보스가 죽어 전투가 끝난 경우 이동 대상은 살아 있고 `Interrupted`다.
- 회귀: 기존 `KnockbackPathTests`·`KnockbackTraitTests`·`ImpactBoltTests`·`KnockbackImmunityPolicyTests`, CoreIntegration `ImpactBoltPublicationTests`·`PullAndMarkPresentationTests`.

시험은 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`만 고치고 돌린다. Framework 시험은 버린 어셈블리라 고치지도 돌리지도 않는다(2026-10-27 사용자 재확인). View·CombatViewBoundary 시험도 마찬가지다. 끝 선택 인자라 그 어셈블리들의 컴파일에는 영향이 없다.

## 10. 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-K1 | 기록을 `AttackResult`에도 실을지, `AttackPreview`에만 실을지 | **A 둘 다.** 끌어오기와 같은 꼴이고, 미리보기가 결과에서 만들어지는 생성 길이 하나로 남는다. 미리보기·실제 일치 시험을 같은 값으로 비교할 수 있다. 재생 원본은 사건이라고 계약 주석에 적는다. (B: 미리보기에만 — 요약이 결과 쪽에 하나 더 생기지 않지만, `FromFacts` 밖에서 따로 넘겨야 한다.) |
| Q-K2 | 첫 칸부터 막힌 대상(0칸)도 기록할지 | **A 기록(`Blocked`, 빈 경로).** View가 "밀려야 하지만 막힘"을 보일 수 있다. 안 남기면 "넉백 대상 아님"과 구분이 안 된다. |
| Q-K3 | 면역 대상을 기록할지 | **A 기록(`Immune`).** 면역 판정은 Core에 있고 View는 이유만 받아 그린다. B(기록 안 함)면 면역 표시는 따로 요청해야 한다. |
| Q-K4 | 넉백 도착 칸 오브젝트 피해가 `TargetHealthAfter`에 안 들어가는 지금 차이(§6)를 이번에 다룰지 | **B 이번에는 다루지 않음.** 피격 체력 고정 순서(I-1~I-7)를 건드리는 다른 변경이다. 필요하면 따로 설계한다. |

결정 뒤 Task 계획을 따로 쓴다(Phase: 공개 값·검사 → 계산 기록·ledger → 결과 연결·시험 → View 인계 메모).

## 11. 리뷰 반영 (2026-10-27)

사용자가 붙여 준 리뷰(P2 셋 + 문서 불일치 하나)를 코드와 대조해 모두 맞음을 확인하고 반영했다.

| 지적 | 확인 | 반영 |
|---|---|---|
| 미리보기 대응 검사 생략 | `AttackPreview.FromFacts`는 `targets`를 따로 받고, 일반 공격에서 `result.Hits`와 같은지 보지 않는다 | §4.2에 미리보기 쪽 대상 존재 검사, §9 시험 |
| `Interrupted` = 사망 설명 오류 | `ExecuteUnit`은 `arrival.Terminal`이면 살아 있는 대상도 중단한다. `MineDuringKnockback_KillsBoss_…`가 그 경우다 | §4.1 뜻 문단, enum 주석, §4.3 표, §7 인계 |
| 추가 공격 분리 시험을 도우미 편의로 생략 | `EnhancementDoubleShotTests`에 특성을 더하는 패턴이 있다(`UnrelatedEffectsRemainEnabled_OnRepeatKills`) | 필수 시험으로 바꿈(계획 T2.4) |
| "사건 목록 비교 시험"이 막연함 | — | 계획 KP-2 "동작 불변 확인"에 시험 이름과 확인 내용을 적음 |
| 방향 계산 위치가 설계·계획 서로 다름 | — | §5.1을 계획 T2.2(면역 검사 앞)로 맞춤 |
| OCP·LSP 근거 | — | §8 문구를 고침 |

## 12. 구현 기록 (2026-10-27)

KP-1·KP-2 구현 뒤 사용자가 Test Runner로 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 돌려 둘 다 통과했다(Phase마다 한 번씩). Framework·View 시험은 고치지도 돌리지도 않았다. KP-0 기준선은 건너뛰었다. 실행 기록은 [계획 §6](../plans/2026-10-27-attack-knockback-preview-plan.md#6-실행-기록)에 있다.

바뀐 파일(`C` 기준):

| 파일 | 내용 |
|---|---|
| `Contracts/Results/AttackKnockbackSnapshot.cs` (+ `.meta`, 새 파일) | 열거 `AttackKnockbackStop`·`AttackKnockbackTargetKind`, 값과 생성자 검사(§4.1) |
| `Contracts/Results/AttackResult.cs` | `Knockbacks`, `FromFacts` 끝 선택 인자, 대응 검사(null·중복·피격 대상 밖) |
| `Contracts/Results/AttackPreview.cs` | `Knockbacks`, 결과에서 받음, 미리보기 대상 존재 검사 |
| `Application/Combat/CommandAttackLedger.cs` | 넉백 기록 목록·`RecordKnockback` |
| `Application/Combat/AttackKnockbackCalculator.cs` | 캐릭터·장애물·면역 기록, 멈춤 이유 바꾸기 `Record` 한 곳, 일반 넉백 방향 계산을 면역 검사 앞으로 |
| `Application/Combat/AttackResultFactory.cs` | 기록 넘기기 한 줄 |

시험: 새 파일 `AttackKnockbackSnapshotTests`(+ `.meta`), 기존 `KnockbackPathTests`·`ImpactBoltTests`·`EnhancementDoubleShotTests`·CoreIntegration `ImpactBoltPublicationTests`에 단언과 사례를 더했다(기존 기대값은 고치지 않음).

설계와 다른 점: 없음. Buffer·Factory·서비스·Framework·View는 바뀌지 않았다. 계산 동작(누가 몇 칸 밀리는지, 피해, 사건 순서)은 그대로다.

남은 것:
- View 표시는 View 작업자의 일이다(§7, 인계 글은 채팅으로 줌, 미전달).
- 이 변경은 아직 커밋되지 않았다.
- §6의 차이(숨은 지형, 추가 공격, 도착 칸 피해 Q-K4)는 그대로다.
