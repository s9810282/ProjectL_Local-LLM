# 공격 넉백 미리보기 — Task 계획

날짜: 2026-10-27
설계: [attack-knockback-preview-design](../specs/2026-10-27-attack-knockback-preview-design.md). Q-K1~Q-K4는 모두 권장안이다(2026-10-27 사용자): 결과와 미리보기 둘 다에 싣고, 0칸 막힘과 면역도 기록하며, 도착 칸 피해 차이는 이번에 다루지 않는다.
상태: KP-1~KP-3 완료(2026-10-27). Core·CoreIntegration 시험은 통과했다(사용자). 커밋 전이다. `CLAUDE.md`는 사용자 결정으로 고치지 않았다(T3.3). 리뷰 반영은 설계 §11, 구현 기록은 설계 §12.

`C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `T` = `Assets/Tests/EditMode`.

## 0. 진행 규칙

- Phase가 끝나면 멈추고 보고한다. 다음 Phase는 사용자 지시 뒤에 시작한다.
- 시험은 **`ProjectL.Tests.Core`와 `ProjectL.Tests.CoreIntegration`만 고치고**, 검증도 사용자가 Unity Test Runner로 그 둘만 돌린다. Framework 시험은 버린 어셈블리라 고치지도 돌리지도 않는다(2026-10-27 사용자 재확인). View·CombatViewBoundary 시험도 마찬가지다. Claude는 MSBuild를 돌리지 않고, 보고에 "컴파일 미확인"과 돌릴 시험 목록을 적는다.
- 새 `.cs`는 `.meta`를 함께 만든다(새 GUID).
- 계산 동작(누가 몇 칸 밀리는지, 피해, 사건 순서)은 바꾸지 않는다. 다른 것은 공개 값이 하나 늘어나는 것뿐이다. 계획과 다른 결정이 필요하면 구현하지 말고 먼저 묻는다.
- 커밋·push는 사용자가 한다.

## 1. Phase 요약

| Phase | 내용 | 끝났을 때 |
|---|---|---|
| KP-0 | 기준선(선택) | 두 시험 어셈블리의 통과 수 기록 |
| KP-1 | 공개 값 `AttackKnockbackSnapshot`, `AttackResult`·`AttackPreview`의 `Knockbacks` | 공개 계약만 생기고 Core는 아직 빈 목록을 냄. 동작은 그대로 |
| KP-2 | 기록 모으기(ledger·넉백 계산·결과 생성) + 계산 시험 | 미리보기와 결과에 넉백 기록이 나옴 |
| KP-3 | 회귀·문서·View 인계 메모 | 기록 정리, View 작업자에게 보낼 글 |

## 2. KP-0 기준선 (선택)

- **T0.1** 사용자가 Test Runner로 `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`을 따로 돌려 통과/실패 수를 알려 준다. 받지 못하면 건너뛰고, KP-2 결과의 실패를 기존 실패와 나눌 수 없다고 보고에 적는다.

## 3. KP-1 공개 계약

**T1.1 새 파일 `C/Contracts/Results/AttackKnockbackSnapshot.cs` (+ `.meta`)**

- `AttackKnockbackStop { Completed = 1, Blocked = 2, Interrupted = 3, Immune = 4 }`
- `AttackKnockbackTargetKind { Character = 1, Structure = 2 }`
- `public sealed class AttackKnockbackSnapshot`: `TargetId`·`TargetKind`·`StartAnchor`·`Step`·`RequestedDistance`·`Path`·`FinalAnchor`·`Stop`.
- 생성자는 `(targetId, targetKind, startAnchor, step, requestedDistance, path, stop)`이다. `FinalAnchor`는 받지 않고 계산한다(경로 마지막 칸, 경로가 비었으면 시작 칸).
- 검사(설계 §4.1, 어기면 `ArgumentException` 계열):
  - 열거 값은 정의된 값이어야 한다(`System.Enum.IsDefined`).
  - `TargetId`는 Empty가 아니다. `Step` 각 성분은 -1·0·1이고 0벡터가 아니다. `RequestedDistance ≥ 1`. `path`는 null이 아니다.
  - `Path[i] == StartAnchor + Step × (i+1)`.
  - 칸 수 규칙: `Completed`이면 요청 거리와 같다. `Blocked`이면 요청 거리보다 작다(0 포함). `Interrupted`이면 1 이상이고 요청 거리 이하다. `Immune`이면 0이다.
- 경로는 복사해서 `Array.AsReadOnly`로 둔다(`AttackPullSnapshot`과 같게).
- XML 주석에 적는다: "이 공격의 넉백 요약이다. 칸 단위 재생 원본은 `Moved` 사건이다(설계 §5.3)."
- `Interrupted` 주석에 적는다: "넉백 진행 중단이며 대상 사망을 뜻하지 않는다. 칸 효과로 전투가 끝나면 살아 있는 대상도 이 값이 된다. 사망은 대상 미리보기의 `TargetKilled`로 본다."(설계 §4.1)

**T1.2 `C/Contracts/Results/AttackResult.cs`**

- 필드 `_knockbacks`와 속성 `Knockbacks => _knockbacks ?? Array.Empty<AttackKnockbackSnapshot>()`(`BlockedContacts`와 같은 꼴).
- private 생성자 끝에 `IReadOnlyList<AttackKnockbackSnapshot> knockbacks = null`을 더하고 복사해 둔다.
- `FromFacts` 끝에 선택 인자 `knockbacks = null`을 더한다. 검사: null 항목 없음, 같은 대상 두 번 없음, 모든 대상이 `hits`의 `TargetId` 안에 있음. 검사는 이 파일 안의 private 함수 하나로 둔다.
- `Success`·`SuccessForCell`·`Failure`는 바꾸지 않는다(빈 목록).

**T1.3 `C/Contracts/Results/AttackPreview.cs`**

- 같은 꼴로 `Knockbacks`를 둔다. `FromFacts`가 `result.Knockbacks`를 넘긴다. 다른 생성 함수는 빈 목록이다.
- 미리보기 대응 검사: `FromFacts`는 `targets`를 결과와 따로 받으므로, 기록 대상마다 `targets`에 같은 `TargetId`가 있는지 검사하고 없으면 `ArgumentException`을 던진다(설계 §4.2). 경로·중복 검사는 결과에서 했으므로 다시 하지 않는다.

**T1.4 새 시험 `T/AttackKnockbackSnapshotTests.cs` (+ `.meta`, `ProjectL.Tests.Core`)**

- 맞는 값: 멈춘 이유 넷이 각각 만들어지고 `FinalAnchor`가 맞다. 대각선 방향도 된다.
- 거절: Empty 대상, 0벡터·성분 2인 방향, 거리 0, null 경로, 곧지 않은 경로, 요청보다 긴 경로, 멈춘 이유별 칸 수 위반(`Completed` 짧음, `Blocked` 꽉 참, `Interrupted` 빈 경로, `Immune` 비지 않음), 정의되지 않은 열거 값.
- `AttackResult.FromFacts`: 인자를 주지 않으면 빈 목록. 피격 대상 밖의 기록, 같은 대상 두 번, null 항목은 거절.
- `AttackPreview.FromFacts`: 결과의 목록을 그대로 싣는다. 결과에는 대상 A의 기록이 있는데 `targets`에 A가 없으면 거절한다(누락 사례).

KP-1 확인(사용자): `ProjectL.Tests.Core` 전체(새 시험 포함), `ProjectL.Tests.CoreIntegration` 전체. 끝 선택 인자라 기존 호출처는 고치지 않는다.

## 4. KP-2 기록 모으기

**T2.1 `C/Application/Combat/CommandAttackLedger.cs`**

- `_knockbacks` 목록, `internal IReadOnlyList<AttackKnockbackSnapshot> Knockbacks`, `internal void RecordKnockback(AttackKnockbackSnapshot record)`(null이거나 같은 대상이 이미 있으면 `InvalidOperationException`).
- 목록 순서는 기록한 순서다(= 넉백 처리 순서, 대상 ID 오름차순).

**T2.2 `C/Application/Combat/AttackKnockbackCalculator.cs`**

- 캐릭터 대상:
  - 시작 칸 `victim.Anchor.Value`를 잡는다.
  - 일반 넉백의 방향 계산(`ResolveBranchStep`)을 면역 검사 **앞으로** 옮긴다. 면역 기록에도 방향이 필요하기 때문이다. 순수 함수라 결과는 같다. 일반 넉백 대상은 공격자 칸 위에 있을 수 없으므로 0벡터 예외가 새로 나지 않는다. 충격 볼트 방향(`TryResolveStep`)은 지금 자리 그대로 둔다.
  - 면역이면 `Immune`(빈 경로)를 기록하고 넘어간다.
  - `ExecuteUnit` 결과로 기록한다. `Steps`가 경로가 되고, `Stop`은 Completed → Completed, Blocked → Blocked, Interrupted → Interrupted로 바꾼다.
- 장애물: `KnockbackObstacle`이 `int` 대신 `ForcedDisplacementPath`와 방향을 돌려주게 바꾼다(밀지 않는 경우는 null). 호출 자리에서 `Structure` 기록을 남기고, `forcedArrivals`에는 지금처럼 칸 수가 1 이상일 때만 넣는다.
- 바꾸는 함수는 private 정적 함수 `Record(ledger, id, kind, start, step, distance, path)` 한 곳이다. 이 함수가 `ForcedDisplacementStop`을 공개 값으로 바꾼다.
- 기록하지 않는 경우는 설계 §4.3 표 그대로다. 거리 0, 직접 피해로 이미 사망, 충격 볼트 중심·제외 대상, 앞 대상 처리 중 전투 종료로 건너뜀, 밀지 않는 구조물.
- `ledger` 인자는 이미 받고 있다. 함수 서명은 바뀌지 않는다.

**T2.3 `C/Application/Combat/AttackResultFactory.cs`**

- `AttackResult.FromFacts(..., pull: pull, knockbacks: ledger.Knockbacks)`.
- 추가 공격(`FollowUpAttackExecutor`)은 ledger가 따로라 본 공격 기록에 섞이지 않는다. 코드 변경은 없다.
- 반응 공격(포효)의 결과에도 기록이 실린다. 미리보기는 지금처럼 없다(`costFree`).

**T2.4 계산 시험 (`ProjectL.Tests.Core`, 사용자 작성 파일에 사례 추가)**

`T/KnockbackPathTests.cs`(배치 도우미 재사용):

- 열린 경로 3칸(`Distance3_OpenPath_MovesThreeCells_WithMovedPerStep`에 더함) → `Completed`, 경로 3칸, `FinalAnchor`가 최종 캐릭터 위치와 같음, **기록 경로가 그 대상 `Moved` 사건 위치 순서와 같음**(요약 = 사건).
- 중간 막힘 / 판 끝 → `Blocked`. 바로 옆 칸이 막힘 → `Blocked`, 빈 경로, 최종 = 시작.
- 낙사 중간 → `Interrupted`, 떨어진 칸까지. 경로 가시로 사망 → `Interrupted`.
- 명시 면역 → `Immune`, 빈 경로, 방향은 공격자에서 대상 쪽.
- 헤비(카테고리 면역 꺼짐) → 기록이 있고 밀림.
- 넉백 중 지뢰로 보스 사망(`MineDuringKnockback_KillsBoss_StopsRemainingTargetsBeforeThorns`에 더함) → 이동하던 대상은 **살아 있고** 기록은 `Interrupted`, 경로는 지뢰 칸까지. 뒤 대상은 기록 없음. `Interrupted`가 사망을 뜻하지 않는다는 계약을 이 사례가 지킨다.
- 두 대상이 같은 줄에 있을 때 처리 순서대로 앞 대상이 민 결과가 뒤 대상의 막힘에 들어감(기록 순서 = ID 순서).
- 미리보기와 실제가 같음: 기존 `HeavyTarget_PreviewMatchesCommittedKnockbackAndThorns`처럼 공개 지형만 있는 판에서 `preview.Knockbacks`와 `result.Knockbacks`의 칸·멈춘 이유가 같음.

`T/ImpactBoltTests.cs`:

- 8방향 사례에 `Step == (dx, dy)`, 경로 = 거리만큼 그 방향인지 단언을 더한다.
- 중심 대상·아군·구조물은 기록 없음.
- 반사 사망 → 빈 목록.
- 장애물 넉백(`OrdinaryKnockback_RetainsObstacleMovement`)에 `Structure` 기록 단언을 더한다.

넉백 특성이 없는 일반 공격 → 빈 목록. 기존 넉백 없는 시험 한 곳에 단언 한 줄을 더한다.

추가 공격 분리(**필수**, 설계 §5.2에서 ledger를 고른 근거): `T/EnhancementDoubleShotTests.cs`에 사례 하나를 더한다.

- 구성: `Input()`을 쓰고, `UnrelatedEffectsRemainEnabled_OnRepeatKills`처럼 행위자 프로필에 `KnockbackTraitSpec(1)`, 상태에 `KnockbackTraitStatusSnapshot(1)`을 더한다. 사거리·대상 위치는 본 공격이 뒤 대상(id 3)을 한 칸 밀고, 같은 방향 반복 공격이 밀린 그 대상을 다시 맞혀 한 번 더 미는 배치로 맞춘다(예: `appointRange`를 넓히고 거리 배율 목록 길이를 맞춤).
- 확인:
  - 반복 공격이 일어났다(`DoubleShot` 발동 1).
  - id 3의 `Moved` 사건은 2개이고, 최종 위치는 두 번 밀린 칸이다. 추가 공격의 이동 사건이 있다는 뜻이다.
  - `result.Attack.Value.Knockbacks`의 id 3 기록은 하나이고 경로는 첫 칸 하나뿐이다. 추가 공격 넉백이 본 공격 기록에 섞이지 않았다는 뜻이다.
- 기존 도우미와 시험용 데이터로 이 배치를 만들 수 없으면 생략하지 않는다. 구현을 멈추고 무엇이 막는지 보고한 뒤 결정을 받는다.

**T2.5 연결 시험 (`ProjectL.Tests.CoreIntegration`)**

- `T/CoreIntegration/ImpactBoltPublicationTests.RepeatedPreview_PublishesConfiguredDamageAndMovementExactlyOnce`에 단언을 더한다. Buffer로 게시된 미리보기(`CombatPreviewResult.Attack`)의 `Knockbacks`가 실행 뒤 게시된 최종 위치와 같아야 한다. 미리보기가 Buffer를 거쳐도 값이 그대로 간다는 확인이다.

KP-2 확인(사용자): `ProjectL.Tests.Core` 전체, `ProjectL.Tests.CoreIntegration` 전체. 특히 `KnockbackPathTests`·`KnockbackTraitTests`·`KnockbackImmunityPolicyTests`·`ImpactBoltTests`·`EnhancementDoubleShotTests`·`PullAndMarkPresentationTests`·`ImpactBoltPublicationTests`.

KP-2 동작 불변 확인. 아래 기존 시험은 **기대값을 고치지 않고** 통과해야 한다. 각 시험이 지키는 것:

| 시험 | 확인하는 것 |
|---|---|
| `KnockbackPathTests.Distance3_OpenPath_MovesThreeCells_WithMovedPerStep` | 칸마다 `Moved` 사건 하나, 위치 순서 |
| `KnockbackPathTests.MineOnPassedCell_ExplodesWhilePassing_BeforeAttackResultIsFixed`·`MineOnFinalCell_KeepsExistingArrivalTiming_AfterAttackResult` | 지나는 칸·도착 칸 효과와 피격 체력 고정의 앞뒤 |
| `KnockbackPathTests.MineBeforeBlockedCell_ResolvesBeforeCheckingRemainingPath` | 칸 효과를 처리한 뒤에 막힘을 검사하는 순서 |
| `KnockbackPathTests.MineDuringKnockback_KillsBoss_StopsRemainingTargetsBeforeThorns` | 전투 종료 뒤 남은 대상의 이동·가시 취소, 결과 사건 하나 |
| `KnockbackPathTests.HeavyTarget_PreviewMatchesCommittedKnockbackAndThorns` | 미리보기·실제 위치·피해가 같고 입력이 바뀌지 않음 |
| `ImpactBoltTests.ThornAndMineTraversal_PreservesDeferredFinalArrival`·`RepeatedPreview_MatchesCommitAndLeavesInputUnchanged` | 충격 볼트 경로 효과 순서, 반복 미리보기 순수성 |
| `EnhancementDoubleShotTests` 기존 사례 전부 | 추가 공격 피해·비용·사망 순서 |

KP-2 구조 점검(보고에 적음):

- 넉백 판정(거리·방향·면역·막힘)이 새로 생긴 곳이 없다.
- `ForcedDisplacementStop`을 공개 값으로 바꾸는 곳이 `Record` 하나다.
- 기록은 `world`가 아니라 ledger에만 있다.
- 일반 넉백의 방향 계산을 옮긴 것 말고는 `Resolve`의 처리 순서가 같다.

## 5. KP-3 마무리

- **T3.1** 설계 문서 끝에 구현 기록 절(바뀐 파일, 계획과 다른 점, 확인 결과)을 둔다. 이 계획 §6에 실행 기록을 둔다.
- **T3.2** View 작업자에게 보낼 짧은 인계 글을 채팅으로 준다(파일 없음). 내용: `AttackPreview.Knockbacks`·`AttackResult.Knockbacks`, 멈춘 이유 넷의 뜻(`Interrupted`는 진행 중단이며 사망이 아니고, 사망은 `TargetKilled`로 본다), 다칸 대상은 현재 점유 칸에 `FinalAnchor - StartAnchor`를 더하면 됨, 재생은 계속 `Moved` 사건, 숨은 지형·추가 공격·도착 칸 피해는 미리보기에 없음. 연결 대상은 e413008 대응 정리 B6·Q-E5다.
- **T3.3** `CLAUDE.md` 현재 상태에 한 줄을 넣는다(사용자 허락 뒤).
- **T3.4** 커밋 묶음 안내: 새 파일 둘(`AttackKnockbackSnapshot.cs`·`AttackKnockbackSnapshotTests.cs`)을 `.meta`와 함께, 바뀐 Core 파일 다섯, 시험 파일 수정분, 문서 둘.

## 6. 실행 기록

### KP-1 (2026-10-27, 코드 완료, 컴파일 미확인)

사용자: "그 부분은 너 권장대로 해 … KP-1부터 진행하자". KP-0은 건너뛰었다. 그래서 KP-2 실패를 기존 실패와 나눌 기준선이 없다. 멈춘 이유별 칸 수 검사는 권장대로 남겼다.

- 새 `C/Contracts/Results/AttackKnockbackSnapshot.cs`(+ `.meta`): 열거 `AttackKnockbackStop`·`AttackKnockbackTargetKind`와 값. 검사는 T1.1대로이고, `Interrupted` 주석은 설계 §4.1 문장이다.
- `AttackResult.cs`: `Knockbacks`, private 생성자와 `FromFacts`에 끝 선택 인자, 대응 검사 `ValidateKnockbacks`(null·중복·피격 대상 밖). `using System.Linq`를 더했다. `Success`·`SuccessForCell`·`Failure`는 바꾸지 않았다.
- `AttackPreview.cs`: `Knockbacks`, private 생성자 끝 인자. `FromFacts`가 `result.Knockbacks`를 싣고, 기록 대상이 `targets`에 있는지 검사한다. 결과가 이미 읽기 전용으로 복사한 목록을 그대로 공유한다.
- 새 시험 `T/AttackKnockbackSnapshotTests.cs`(+ `.meta`, `ProjectL.Tests.Core`): 값 시험 10개(그중 `Interrupted`는 사례 2), 결과 대응 3개, 미리보기 3개.
- 계획과 다른 점은 없다. Core 계산은 아직 빈 목록만 내므로 동작은 그대로다. 기존 시험은 고치지 않았다.
- 확인(2026-10-27 사용자): Core·CoreIntegration 모두 통과.

### KP-2 (2026-10-27, 코드 완료, 컴파일 미확인)

사용자: "Core·CoreIntegration 모두 통과했어. KP-2 진행해".

- `CommandAttackLedger.cs`: `Knockbacks`(처리 순서 목록)와 `RecordKnockback`(null·같은 대상 두 번은 예외).
- `AttackKnockbackCalculator.cs`:
  - 캐릭터 대상은 시작 칸·방향을 잡고, 면역이면 `Immune`(빈 경로)을 기록한다. 아니면 `ExecuteUnit` 결과를 `Record`로 기록한다.
  - 일반 넉백의 `ResolveBranchStep`을 면역 검사 앞으로 옮겼다(T2.2). 충격 볼트 쪽은 그대로다.
  - `KnockbackObstacle`은 `(경로, 시작, 방향)?`을 돌려주고, 호출 자리에서 `Structure`를 기록한다. 칸 수가 1 이상일 때만 도착 목록에 넣는 것은 그대로다.
  - `ForcedDisplacementStop` → `AttackKnockbackStop` 바꾸기는 `Record` 한 곳뿐이다.
- `AttackResultFactory.cs`: `knockbacks: ledger.Knockbacks` 한 줄.
- 시험(모두 Core·CoreIntegration, 기존 기대값은 고치지 않고 단언·사례만 더함):
  - `KnockbackPathTests`:
    - 기존 사례에 단언 추가: 헤비 이동, 넉백 없음 → 빈 목록, 미리보기 = 실제, 열린 경로 3칸(요약 = `Moved` 순서), 중간 막힘, 판 끝, 낙사, 지뢰로 보스 사망(이동 대상 생존 + `Interrupted`, 뒤 대상·보스 기록 없음), 명시 면역.
    - 새 사례: `BlockedAtFirstCell_RecordsEmptyPathAtStart`, `EarlierTargetIsProcessedFirst_SoItCanBeBlockedByLaterTarget`(전체 지정 도우미 `AimAll`), `LethalThornMidway_InterruptsAtThatCell`.
  - `ImpactBoltTests`: 8방향(방향·칸 수·최종), 아군·구조물·중심은 기록 없음, 반사 사망 → 빈 목록, 장애물 넉백 `Structure` 기록.
  - `EnhancementDoubleShotTests`: 새 사례 `RepeatKnockback_MovesAgainButStaysOutOfRootAttackKnockbacks`. 사거리 3, 거리 배율 `[150, 50, 50]`, 넉백 1. `Moved`는 2개, 본 공격 기록 경로는 첫 칸 하나.
  - CoreIntegration `ImpactBoltPublicationTests.RepeatedPreview_…`: Buffer를 거친 미리보기 `Knockbacks`가 게시 결과의 요약·최종 위치와 같고, 중심을 뺀 이웃 둘만 기록된다. 낡은 주석("미리보기는 최초 점유 칸만") 한 줄을 고쳤다.
- 계획과 다른 점: 없음. 넉백 사례 중 "두 대상 순서"는 앞 대상이 뒤 대상에 막히는 배치로 만들었다(ID 순서대로 미는 사실을 직접 보이는 꼴).
- 구조 점검:
  - 넉백 판정(거리·방향·면역·막힘)이 새로 생긴 곳은 없다.
  - 멈춤 이유 바꾸기는 `Record` 하나다. `MovementEnvironmentCalculator:202`·`PullAttackCalculator:98`은 원래 있던 내부 판정이다.
  - 기록은 ledger에만 있다.
  - `Resolve`의 처리 순서는 방향 계산을 옮긴 것 말고 같다.
  - Buffer·Factory·서비스 변경은 없다.
- 확인 대기(사용자 Test Runner): `ProjectL.Tests.Core` 전체, `ProjectL.Tests.CoreIntegration` 전체. 동작 불변 표의 기존 시험은 기대값을 고치지 않았다.
- 확인(2026-10-27 사용자): Core·CoreIntegration 모두 통과. 그래서 동작 불변 표의 기존 시험도 기대값 그대로 통과했다.

### KP-3 (2026-10-27, 문서만)

사용자: "Core·CoreIntegration 모두 통과했어. KP-3 진행해".

- T3.1: 설계 §12에 구현 기록 절을 더하고 머리 상태를 고쳤다. §11(리뷰 반영)이 §10 앞에 들어가 있던 순서도 바로잡았다.
- T3.2: View 작업자에게 보낼 인계 글을 채팅으로 줬다(파일 없음, 미전달).
- T3.3: `CLAUDE.md`는 고치지 않는다(2026-10-27 사용자 "CLAUDE.md는 고치지 마").
- T3.4 커밋 묶음(한 커밋 권장):
  - 새 파일 넷: `C/Contracts/Results/AttackKnockbackSnapshot.cs`, `T/AttackKnockbackSnapshotTests.cs`, 각 `.meta`.
  - 바뀐 Core 다섯: `AttackResult.cs`, `AttackPreview.cs`, `CommandAttackLedger.cs`, `AttackKnockbackCalculator.cs`, `AttackResultFactory.cs`.
  - 바뀐 시험 넷: `KnockbackPathTests.cs`, `ImpactBoltTests.cs`, `EnhancementDoubleShotTests.cs`, `CoreIntegration/ImpactBoltPublicationTests.cs`.
  - 문서 둘: 이 계획, 설계.
  - 제외: 작업 트리의 `docs/superpowers/specs/2026-10-27-combat-click-move-design.md`(이 작업이 아님).
