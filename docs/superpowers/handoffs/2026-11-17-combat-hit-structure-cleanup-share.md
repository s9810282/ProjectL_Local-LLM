# 전투 피격 구조 정리 — 바뀐 줄 공유 (천명님·민형님·호진님)

- 날짜: 2026-11-17
- 상태: **미전달**(사용자가 전달)
- 설계: [hit-structure-cleanup-design](../specs/2026-11-17-combat-hit-structure-cleanup-design.md), 기록: [plan](../plans/2026-11-17-combat-hit-structure-cleanup-plan.md) §6
- 컴파일 미확인, Unity 확인 전

## 0. 무엇이 바뀌었나 (한 단락)

전투 피격·사망의 코드 움직임(젖힘·밀림·쓰러짐·랙돌)을 조립 안의 반응 조각 하나(`CombatReactionMotionPiece`)가 진행하도록 바꿨습니다. 그래서 반응 부품 목록(`CombatUnitReactionSet`)·반응 재생기(`CombatReactionPlayer`)·외부 진행 조각(`CombatReactionPiece`)을 지웠고, 피격 시작은 캐릭터 처리기에서 새 `Playback/Support/CombatHitStart`로 옮겼습니다. 보이는 동작은 같게 했습니다(예외 문구 몇 개만 다름). 아래는 각자 쓰신 줄 중 바뀐 것입니다.

## 1. 천명님

커밋 `55021fc0f`(자폭 깜빡임)·`d998181ad`(develop 병합)의 줄입니다.

- `Playback/Handlers/CombatCharacterEventHandler.cs`
  - 주체 몸 자리 함수 `SubjectBodyOf`(본문·문서 4줄)를 `CombatHitStart.SubjectBodyOf(facts, mapper, id, e, facing)`(정적)로 옮겼습니다. 자폭 깜빡임 사망은 `CombatHitStart.SubjectBodyOf(_facts, _mapper(), id, death, standing.Value.rotation)`을 부릅니다 — 계산은 그대로입니다.
  - 보통 사망의 조립 호출 줄(몸 자리 인자)과 피격 조립 호출 줄은 새 꼴로 바뀌었습니다(반응 조각 + 조립 자세 대여).
  - 생성 인자 줄 `CombatKnockbackLeadIns knockbackLeadIns, CombatSelfDestructDeaths selfDestruct` → `CombatHitStart hits, CombatSelfDestructDeaths selfDestruct`. 자폭 갈래(`_selfDestruct`·`BeginSelfDestructDeath`·`ComposeSelfDestructDeath`)는 그대로입니다.
- `Playback/CombatSceneEventPresenter.cs`: 캐릭터 처리기 생성 둘째 줄의 마지막 인자 `_knockbackLeadIns` → `hits`. 그 위에 `var hits = new CombatHitStart(…)` 한 줄이 생겼습니다.
- 요청 없음. 확인 부탁: 자폭 깜빡임 사망의 이펙트 자리(몸 중심·맞은 방향)가 예전과 같은지.

## 2. 민형님

커밋 `2ddb5f342`(랙돌)의 줄입니다.

- `Playback/Handlers/CombatCharacterEventHandler.cs` 보통 사망: `_reaction.BeginDeath(death, settings, rented, _mapper(), launchRagdoll, hold.Release, end, _cancelUnavailableReaction)` → 조립에 `CombatReactionMotionPiece.Death(id, settings, death, _mapper(), launchRagdoll, hold.Release)` + `Lease(id, Pose)`. 랙돌 판단(`launchRagdoll = _isAttackHit(original)`)과 반응 부품 `CombatUnitReactionPresenter.BeginDeath`의 랙돌·디졸브 규칙은 그대로입니다.
- 지운 `Presentation/Reaction/CombatUnitReactionSet.cs`의 `BeginDeath`(랙돌 인자) 4줄. 반응 부품 목록 자체를 걷어 같이 없어졌습니다.
- 바뀐 동작: 자세는 이제 조립이 시작할 때 빌립니다(사망 유지 → 조립 시작 → 사실 반영 순서는 같음). 시작 뒤 예외면 조각 취소가 자세 되돌리기와 사망 유지 놓기를 함께 합니다.
- 확인 부탁: 공격 처치의 랙돌이 예전처럼 맞은 방향으로 날아가는지, 쓰러짐·디졸브가 같은지.

## 3. 호진님

- `Playback/Handlers/CombatCharacterEventHandler.cs`: 쓰지 않게 된 `using ProjectL.Systems.RuntimeSystem.ErrorHandling;` 한 줄(커밋 `6c56a50d3`)을 지웠습니다(정리 도우미 `RunCleanup`을 걷으며).
- 토네이도 던짐(`TryBeginTornado`·`FollowStorm`)·갈래 줄은 그대로입니다. 실행 목록 `CombatPresentationPlayers`의 외형 재생기 줄도 그대로이고, `reaction` 자리만 빠졌습니다.
- `Presentation/Pieces/CombatComposition.cs`의 필드 행동 주석 둘(커밋 `f672bb337`, 토네이도 View 설계 TN10)을 고쳤습니다(검토 권장 반영 ⑦, 코드는 그대로).
  - `PausesOtherPlayers` 요약: "뒤 재생기(걸음·반응)" → "히트 전인 동안 뒤 재생기(걸음 등)". 피격·사망 반응은 이제 조각 실행 재생기 안의 유닛 실행이라 행동 실행이 히트 전이어도 멈추지 않습니다.
  - `ForFieldAction` 요약: "진행 중인 걸음·반응은 멈추지 않습니다" → "진행 중인 걸음은 멈추지 않습니다(피격·사망 반응은 유닛 실행이라 원래 멈추지 않음)".

## 4. 함께 알아 둘 것

- 유닛 실행이 그 주체의 어떤 사건을 막지 않거나 끊겨야 하면, 이제 조각 실행 재생기에 갈래를 더하지 않고 통과 규칙(`ICombatRunSubjectPass`) 하나를 조립에 줍니다(`PassSubject(…Instance)`). 지금 둘: 밀림과 함께 가는 피격 `CombatPushAlongsidePass`, 연발 앞 탄 `CombatNextHitPass`. 규칙은 폴더 배치 안내 §2.
- 막는 이유 `CombatStartBlock.Reaction`은 없어지고 `PieceSubject`에 합쳤습니다.
