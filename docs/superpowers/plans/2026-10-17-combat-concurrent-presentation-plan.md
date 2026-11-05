# 전투 재생 동시 처리 구조 — Task 계획

- 설계: [동시 처리 구조 상세 설계](../specs/2026-10-17-combat-concurrent-presentation-design.md) C1~C8.
- 결정(2026-10-17): Q-C1 A(Core 피해 근원 열쇠), Q-C2 A(묶음 뒤 사건은 묶음 전체가 끝난 뒤), Q-C3 A(기차 전용 재생 유지).
- 이 계획은 [히트 타이밍 계획](2026-10-16-combat-hit-timing-plan.md) §5.1(Q4 B)을 대신한다.
- 경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- 진행 규칙: Phase마다 멈추고 보고한다. 하위 검토 에이전트 1개를 돌린다. 설계와 다른 결정이 필요하면 구현 전에 묻는다. 커밋은 사용자가 한다.

## 0. 착수 전 (사용자)

- H-4a~c(이동 접촉)를 먼저 커밋하는 것을 권장한다. 같은 파일(`OrderedCombatPresentation` 주변, `CombatSceneEventPresenter`, `CombatUnitMotionPlayback`, `CombatStepContactGate`, `FieldUnitPresenter`)을 다시 바꾸므로 기준선이 필요하다.
- 가능하면 H-1~H-4 Unity Test Runner 결과를 먼저 받는다. 기존 실패와 이번 회귀를 구분하기 위해서다.
- 관리 실행 기준선은 착수 때 그 HEAD에서 새로 잡는다.

## 1. Phase C-1 — 동시 묶음 해석과 스케줄러 (C1·C2·C3·C4, 동작 변화 없음)

- **파일:**
  - 새 `C/CombatConcurrentGroups.cs`(+ .meta): 순수 해석.
  - 새 `C/CombatEventSubjects.cs`(+ .meta): 사건 주체 판정. 묶음 해석과 표시(C-2)가 같이 쓰는 한 곳.
  - 수정 `C/OrderedCombatPresentation.cs`: 스케줄러.
  - 시험.
- **Task:**
  1. `CombatEventSubjects.TryGetSubject(CombatEvent, out EntityId)`
     - 캐릭터 사건 = 그 캐릭터(`Final`, 없으면 `Expected`), 구조물 = 그 구조물, 보유 효과 = 그 캐릭터, 장애물 반응 = 그 장애물.
     - 그 밖은 주체 없음.
  2. `CombatConcurrentGroups.Begin(result)`: 사건 위치(색인)별 묶음 번호를 매긴다. 묶음 없음은 -1이다.
     - 열쇠는 피해 근원의 (공격 그룹, 가해자)다. 근원이 여럿이면 모두 같은 열쇠일 때만 그 열쇠를 쓰고, 다르면 묶음 밖이다(보수적).
     - 이동 후속(`CombatHitFollowUps`를 이 해석 안에 순수 인스턴스로 둠)은 출처의 열쇠를 받는다.
     - 행동 경계(`CombatActionBoundary`)마다 구간 번호를 올리고, 묶음 = (구간, 열쇠)다.
     - 주체가 없는 사건은 묶음 밖이다. 기차 직후 사망은 근원이 없어 자연히 묶음 밖이다(C7).
     - 색인 기준이라 같은 사건 인스턴스가 두 번 있어도 예외가 나지 않는다.
  3. `OrderedCombatPresentation`을 스케줄러로 바꾼다.
     - 사건별 상태 배열(시작·완료)을 결과마다 한 번 만든다. 첫 미완료 색인을 기억한다.
     - Tick: 표시 진행 → 첫 미완료부터 훑으며 시작 후보(설계 C2)이고 `CanBeginPresentationEvent`가 참인 사건을 모두 시작한다 → 지나갈 수 없는 미완료에서 멈춘다.
     - 비이동 사건은 사건별 완료 콜백으로 완료한다. 세대·색인으로 늦은 완료를 무시한다.
     - **이동 사건은 이번 Phase에서 지금 경로를 유지한다**(진행 중 걸음 하나, `CombatMovementPlayback` 폴링). 걸음이 진행 중이면 다른 걸음을 시작하지 않는다. 계약 변경은 C-3이다.
     - 경로 끝 판단(`IsLastMovementStep`), 표시 계약 없는 사건 건너뛰기, 결과 완료 = 모든 사건 완료 + `IsPresentationComplete`, 취소·다시 연결은 지금과 같다.
- **동작 변화 없음의 근거(C-1 검토 1로 보정):** 표시가 전역으로 막는 것은 반응·능력 동작뿐이고, 공격 Gate·걸음 Gate는 사건마다 답한다. 그래서 같은 묶음의 앞 사건이 사건별 Gate에 막혀 아직 시작 전이면 뒤 사건이 먼저 시작할 수 있었다(지뢰가 걸음 중 폭발할 때 이웃 피해, Ballista 접촉 밖 후속 뒤의 관통 대상). **사용자 결정(2026-10-17): "C-1은 엄밀히 동일, C-2에서 풀기".** C-1의 스케줄러는 시작 전인 앞 사건을 묶음이어도 지나가지 않는다. 시작한 앞 사건(반응 중)은 표시가 전역으로 막으므로 시작 순서·시점이 지금과 같다.
- **시험:**
  - 순수 `CombatConcurrentGroupsTests`: 직접 피격 여러 대상 = 한 묶음, 폭발 그룹, 광란 파동 = 다른 묶음, 넉백 이동 = 출처 묶음, 근원 섞임 = 묶음 밖, 행동 경계에서 끊김, 근원 없는 사건, 같은 인스턴스 중복.
  - 순수 `OrderedCombatSchedulingTests`(가짜 재생): 표시가 허락하면 묶음 안 다른 유닛 사건이 앞 사건 완료 전에 시작한다. 같은 유닛 순서, 묶음 밖 사건은 전부 완료 뒤, 묶음 뒤 사건은 묶음 전체 뒤(Q-C2 A), 표시가 막으면 지금과 같은 순서, 늦은 완료 무시, 취소, 건너뛰는 사건.
  - 회귀(동작 동일): 관리 실행 전체, Unity의 이동·적 간격·공격·기차·광란 묶음.

## 2. Phase C-2 — 유닛별 반응과 주체 기준 시작 조건 (C5 반응 줄, ①②④)

- **파일:** `C/Display/CombatUnitReactionSet.cs`, `C/CombatSceneEventPresenter.cs`, 필요하면 `C/CombatSceneEventPlayback.cs`. 시험.
- **Task:**
  0. 스케줄러의 C-1 제한(시작 전인 앞 묶음 사건을 지나가지 않음)을 푼다. 시험 `EarlierPendingGroupMember_IsNotPassed_InPhaseC1`을 "먼저 시작"으로 바꾼다. 그 결과 지뢰 이웃 피해가 접촉 순간 시작하고(이동자 피해는 걸음 끝 뒤), Ballista 관통 대상이 앞 접촉 밖 후속을 기다리지 않는다.
  1. 반응 집합: 같은 유닛만 배타로 바꾼다. 서로 다른 유닛의 피격·사망은 함께 시작할 수 있다.
     - 기차의 `BeginConcurrentDeath` 가드(사망만 겹침)는 기차 전용 재생 유지(Q-C3 A)를 위해 그대로 둔다.
  2. `CanBeginPresentationEvent`·`RequireIdle`을 사건 주체 기준으로 바꾼다.
     - 주체가 있는 사건: 그 유닛의 반응이 끝났는지 본다. 걸음 판단은 기존 걸음 Gate 그대로이고, 걸음 유닛별은 C-3이다.
     - 주체 없는 사건·행동 사건: 지금처럼 전역 유휴를 본다.
     - 공격 Gate·걸음 Gate 규칙은 그대로다.
  3. `TickPresentation`: 공격 → 능력 동작 → 걸음 → 모든 반응을 매 프레임 진행한다.
     - "반응 중이면 반응만" 분기를 없앤다. 다른 유닛 반응 중에 시작한 넉백 걸음이 멈추지 않게 한다.
     - 새 반응에는 시작 프레임 시간을 주지 않는다(기존 Version 규칙).
- **결과:**
  - ① 여러 대상 피격, ② 같은 원인 여러 사망, ④ Ballista 관통(접촉 해제 순간마다)이 동시에 시작한다.
  - ③ 넉백은 "앞 대상 넉백 중 뒤 대상 반응"까지 겹치고, 걸음끼리는 C-3에서 겹친다.
- **시험:**
  - Unity: ShotGun 또는 곡사 여러 대상 반응이 같은 프레임에 시작, 두 대상 동시 사망, Ballista 두 대상 접촉마다 시작·겹침, 지뢰 폭발 이웃 동시, 광란 같은 대상은 차례로, 취소 때 모든 반응 정리.
  - 순수: 반응 집합 동시 시작·같은 유닛 거절·전체 취소(가능한 범위).
  - 회귀: 히트 타이밍 Unity 시험 전체(`CombatCodeMotionHitTests`·`CombatBallistaAttackTests`·`CombatStandardAttackTests`·`CombatMortarPresentationTests`·`CombatUnitDeathTests`), 기차, H-4 걸음 접촉.

## 3. Phase C-3 — 걸음 계약과 유닛별 걸음 레인 (C5 걸음 줄·C6, ③)

- **파일:**
  - `C/ICombatMovementPlaybackPresenter.cs`·`C/ICombatEventPlayback.cs`(계약)
  - `C/CombatMovementPlayback.cs`(흡수 또는 걸음별)
  - `C/OrderedCombatPresentation.cs`
  - `C/CombatSceneDisplay.cs`·`C/CombatSceneEventPlayback.cs`·`C/CombatSceneEventPresenter.cs`
  - `C/CombatUnitMotionPlayback.cs`, 새 걸음 레인 집합(예: `C/CombatUnitMotionSet.cs`)
  - `C/CombatStepContactGate.cs`, `C/Display/FieldUnitPresenter.cs`
  - 시험 가짜 2개(`T/CombatViewSystemTests.cs` `FakeDisplay`, `T/FrenzyViewContractTests.cs` `RecordingPlayback`)
- **Task:**
  1. 계약: `BeginStep(step, completed)`로 바꾸고 `IsMovementComplete`를 없앤다. `BeginMovementEnd`는 유지한다.
     - 순차 재생은 걸음도 사건별 완료 콜백으로 받는다. 경로 끝이면 완료 뒤 `BeginMovementEnd`를 거친다.
  2. 걸음 레인:
     - 유닛마다 `CombatUnitMotionPlayback` + 걸음 Gate를 둔다. 걸음 해석(`CombatStepContacts`)은 결과마다 하나를 레인이 공유한다(Gate가 해석을 소유하지 않게 분리).
     - 걸음 Gate 규칙 1의 "모든 이동"을 "이동자 자신의 이동"으로 바꾼다.
     - 미룬 방향 복귀·접촉 해제는 레인별이다.
  3. 배치 소유 (a): `FieldUnitPresenter`의 보간 중 유닛을 집합으로 바꾼다.
  4. Presenter: 주체 기준 판단에 걸음 레인을 더하고, 행동 사건만 모든 레인 끝을 요구한다.
     - Tick은 모든 레인을 진행한다. 새 레인에는 시작 프레임 시간을 주지 않는다.
     - 적 행동 간격(`_gapRemaining`)은 자발적 경로의 첫 걸음에만 쓰고 레인과 섞지 않는다.
- **결과:** ③ 같은 타격으로 밀린 여러 대상이 각자 반응이 끝난 뒤 동시에 걷는다.
- **기대치(C-1 검토 7):** Core는 대상 경로 사이에 묶음 밖 사건을 끼울 수 있다(출발 때 위치 보정 해제 `HealthNormalized`, 칸마다 새 그룹이거나 근원이 섞인 넉백 가시 피해). 그런 사건이 끼면 뒤 대상의 넉백은 앞 대상 경로 전체 뒤다(보수적이며 맞는 동작). 재생 확인 목록에 넣는다.
  - 정정(C-4 검토 1): 위 문장은 그 사건이 앞 대상 경로나 뒤 대상 첫 걸음 **앞**에 있을 때만 맞았다. 뒤 대상 경로 **안**에 끼면 함께 출발했다가 그 칸에서 앞 대상 경로 끝까지 멈췄다. 사용자 결정 (a)로 끊긴 경로의 이동을 묶음에서 빼(설계 C1·§6.1) 이제 어느 경우든 그 대상은 앞 사건이 모두 끝난 뒤 걷는다.
- **시험:**
  - Unity: 두 대상 넉백 동시 걸음(같은 시간에 출발·도착), 같은 대상 여러 칸은 차례로, 자발적 경로·경로 끝 복귀·적 간격은 지금과 같음, H-4 접촉 시험 전체, 취소 때 모든 레인 정리·배치 소유 해제.
  - 순수: 걸음 레인 집합·Gate 규칙 1 변경, 스케줄러의 걸음 콜백 경로.
  - 회귀: `CombatMovementViewTests`·`CombatEnemyPresentationTests`·`CombatSceneEventPresenterTests`·`CombatResultPlaybackTests`·`CombatViewSystemTests`, 기차.

## 4. Phase C-4 — 문서·인계 (2026-10-18 실행, §7.4)

- 설계 상태, 히트 타이밍 계획 §5.1 연결, CLAUDE.md, 재생 확인 목록을 정리한다.
- 새 동시 경우를 더하는 방법을 viewDocs 짧은 안내로 둘지는 이때 묻는다.
- 기차 이전(C7) 후속 조건과 남은 위험(넉백 겹침 시각, 가시밭 적용 그룹)을 정리한다.

## 5. 기능 변경의 독립성 — 대표 시나리오

| 변경 | 바뀌는 곳 |
|---|---|
| 새 폭발 종류(Core가 같은 근원 열쇠를 줌) | 없음(C1 규칙으로 묶임) |
| 새 동시 기준(예: 가시밭 적용을 같은 순간으로) | Core가 그룹을 공유하면 없음. View 규칙으로는 하지 않는다 |
| 묶음 뒤 사건 시작 시점 변경(Q-C2 B) | 스케줄러 시작 규칙 한 곳 |
| 넉백만 동시에서 빼기 | `CombatConcurrentGroups`의 이동 후속 한 줄 |
| 애니메이션 이벤트 레인(H-5) | C5 표에 부품 하나 + Presenter의 자원 판단 다섯 곳(시작 조건·`RequireIdle`·Tick·완료·취소). 한 판단으로 모으는 일은 Presenter 분리(S 계획) 후보(C-4 검토 10) |
| 기차를 같은 구조로 이전(C7 후속) | 기차 재생·`CombatSceneEventPlayback` 소비 경로, 묶음 해석에 기차 접촉 해제 |

## 6. 검토 기록

### 6.1 C-1 검토 (하위 에이전트 1개, 2026-10-17)

- 높음 없음. 늦은 완료·재진입·`_first` 전진·훑기 멈춤·이동 한 걸음·경로 끝·비용·기존 시험 가짜·Core 열쇠 대조(직접 피해 = Direct + 공격자, 폭발·지뢰·광란·반사·넉백 가시·기차)는 맞다.
- 반영:
  - 1(중간): 실제 표시에서 "동작 변화 없음"이 일부 안 맞음. 사건별 Gate에 막혀 시작 전인 앞 묶음 사건을 뒤 사건이 지나간다(지뢰 이웃, Ballista 관통). 사용자 결정 "C-1은 엄밀히 동일, C-2에서 풀기"에 따라 시작 전인 앞 사건을 지나가지 않게 했다. 푸는 일은 §2 Task 0이다.
  - 2(낮음): 경로 끝 연출 대기(Ending) 뒤 사건을 막는다(옛 동작).
  - 3(낮음): 시험 래퍼 3개(`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatEnemyPresentationTests`의 `Display`)가 `CanBeginPresentationEvent`를 실제 Presenter로 넘기게 했다.
  - 4(낮음, SSOT): 근원 사건 종류·주체 판정을 `CombatEventSubjects`(`TryGetOrigins`·`TryGetSubject`) 한 곳으로 모으고 `CombatHitFollowUps`가 그것을 쓴다(동작 동일, `CombatHitFollowUpsTests` 8개 통과).
  - 5: 열쇠 유일성이 행동 경계 구간에 기대는 전제를 설계 §1.4에 적었다(기존 시험이 고정).
  - 6: 설계 C1의 "회복" 정정(Core 회복에는 근원이 없음).
  - 7: C-3 기대치(넉백 경로 사이에 끼는 묶음 밖 사건)를 §3에 적었다.
  - 8: 빠진 시험 추가(끝난 뒤 다시 재생할 때 늦은 콜백, Ending 차단, (G, G2, G), 동기 완료 연쇄, 장애물 주체).

### 6.2 C-1~C-4 통합 검토 (하위 에이전트 2개, 2026-10-18)

사용자 지시 "C-4 진행하고 하위 에이전트로 전체 검증해". C-2·C-3 검토를 미룬 대신(사용자 지시) 전체를 두 관점으로 나눠 봤다. 반영 실행은 §7.5다.

- **런타임 정확성·동작 보존(Core 사건 순서와 대조):** 높음 없음. 교착·새 예외 경로 없음.
  - 1(중간) **동시 넉백 경로 중간 정지:** 뒤 대상 경로 안에 묶음 밖 사건(넉백 가시 피해·중간 칸 지뢰·지형 등록)이 끼면 함께 출발했다가 그 칸에서 앞 대상 경로가 끝날 때까지 반응 없이 멈췄다(HEAD에 없던 시각 회귀). **사용자 결정 (a)** "그 대상만 차례로": 끊긴 경로의 이동을 묶음에서 뺀다(`CombatConcurrentGroups`, 설계 C1·§6.1).
  - 2(낮음) **자발적 걸음 + 다른 유닛 이동:** 접촉으로 일찍 사건 완료된 자발적 걸음의 남은 보간·미룬 방향 복귀 중에 다른 유닛 이동(위험 타일 밀림·토네이도·복귀)이 겹쳐 시작했다(설계 C2 "묶음 없는 결과는 지금과 같다"와 어긋남). **사용자 결정 "자발적 걸음은 HEAD대로"**: 걸음 Gate 규칙 1이 자발적 걸음이면 모든 이동을 걸음 끝까지 막는다(설계 C5·§6.1).
  - 3(기록) 간격·대기 사건 칸이 하나라 간격 중에는 모든 레인을 멈춘다. 지금은 간격이 행동 경계에만 생겨 안전하다 → 전제가 깨지면 레인이 멈추는 대신 알리도록 `StartGap` 가드를 더했다.
  - 4(기록) ① 여러 대상 반응도 대상 사이에 근원 없는 사건이 끼면 그 지점에서 차례로 돈다(보수적, 정지 아님) → 재생 확인 목록(§7.4)에 넣었다.
  - 5(기록) 레인 사전(`CombatUnitMotionSet._lanes`)은 줄지 않는다. 씬 연결 하나에 전투 하나라 크기가 제한된다.
- **시험·문서·원칙:**
  - 1(높음) 고치지 않은 `CombatBallistaAttackTests.EncounterOrderDoesNotReorderResultEvents`가 새 시험과 정면으로 어긋나 Unity에서 반드시 실패한다(Unity 전용이라 관리 실행에서 안 잡힘) → 지웠다. 같은 사건을 새 규칙으로 고정하는 `PiercedTargets_StartAtOwnContact_EvenWhenCoreListsFarTargetFirst`가 대신한다.
  - 2(중간) 걸음 Gate 규칙 3 변경이 설계에 없고 이동 접촉 설계 M3~M5가 낡았다 → 설계 C5에 규칙 3 행, 이동 접촉 설계 머리말과 M3~M5에 "→ C-3" 주석.
  - 3(중간) 배치 소유 집합을 고정하는 시험이 없다(취소 시험은 진행률 0, 동시 넉백은 같은 프레임 시작·끝) → 걸음 절반에서 취소, 엇갈린 두 걸음 시험.
  - 4(중간) 빠진 시험 → 스케줄러 걸음 경로(늦은 완료, Tick 안 완료, 걸음 묶음 Q-C2 A, 묶음 밖 걸음 장벽), 걸음 중 지뢰, 같은 대상 2칸 넉백, 반응 집합 동시 시작, 기차 동시 사망 중 차단. **광란 같은 대상 Unity 시험은 더하지 않았다:** Core가 파동마다 새 그룹을 줘 순서가 열쇠 규칙(`DifferentAttackGroups_AreDifferentMoments`)과 같은 주체 순서(`SameUnitEvents_StayOrdered_EvenInOneGroup`)에서 나온다. 런타임 검토가 코드로 확인했다.
  - 5(낮음) Presenter 시작 조건(`CanBeginPresentationEvent`)과 시작 전제(`RequireIdle`)가 주체 없는 사건에서 갈림(LSP) → 시작 조건도 `MotionBlocks`를 쓴다.
  - 6(낮음, SSOT) 진단용 `IsMovementComplete`·`IsStepComplete` 이중 원본 → 지우고 `CombatEnemyPresentationTests` 9곳을 걸음 완료 통지 횟수로 바꿨다.
  - 7·8·9(낮음) 문서 상태(히트 타이밍 계획 §5.1 머리, CLAUDE.md, 설계 C4 비용·C5 Version·C6 결정), 낡은 주석 6곳, 걸음 레인 생성자의 쓰지 않는 기본값(필수 인자로) → 반영.
  - 10(낮음) H-5 독립성 주장 과장 → 설계 §4·계획 §5에 "Presenter 다섯 곳, 한 판단으로 모으기는 S 계획 후보"로 정정.
  - 11(기록) 후속 대응 해석 2벌(§7.4에 있음). §7.4 7번 "H-4 걸음 접촉은 지금과 같다"가 엄밀하지 않음 → 런타임 2의 결정으로 자발적 걸음은 같아졌고, 남는 차이(강제 걸음의 접촉 묶음 뒤 다른 유닛 이동)와 지금 Core로는 그 경로를 찾지 못했다는 점을 §7.4에 적었다. `CombatMovementViewTests` 가짜 표시의 걸음 완료 칸 하나(같은 묶음 넉백 둘을 넣으면 앞 콜백을 잃음)는 기록만 한다. `FMODStudioCache.asset` 시간값 변경은 이 작업과 무관 → 커밋에서 뺀다.

## 7. 실행 기록

### 7.1 C-1 실행 (2026-10-17, 사용자 지시 "C-1 진행해, Phase 끝나면 하위 에이전트로 검증하고")

- **기준선:** HEAD `fc98408f`(H-4 커밋 `35d6c379` + 다른 브랜치 병합)에서 `c1base`를 새로 잡았다.
  - 이 기준선에서 `CombatProgressIntegrationTests` 5개가 이미 실패한다. 병합된 메인 캐릭터 덱 등록·초기 배치 변경(`BattlePreparationCalculator`·`InitialDrawPlan` 등)으로 보이며, 이번 작업 범위 밖이라 손대지 않았다.
- **코드:**
  - 새 `C/CombatEventSubjects.cs`: 사건 주체, 피해 근원 목록·열쇠(근원이 섞이거나 그룹 0이면 열쇠 없음).
  - 새 `C/CombatConcurrentGroups.cs`: 색인별 묶음(행동 구간 + 열쇠, 넉백은 출처 열쇠), `CanOverlap`·`IsBarrier`.
  - `C/OrderedCombatPresentation.cs`: 사건별 상태 배열 스케줄러.
    - 첫 미완료부터 훑으며 시작 후보를 모두 시작하고, 미완료 묶음 밖 사건에서 멈춘다.
    - 이동은 지금처럼 한 번에 한 걸음이고, 경로 끝(Ending) 뒤 사건은 막는다.
    - C-1 제한: 시작 전인 앞 사건은 지나가지 않는다.
  - `C/CombatHitFollowUps.cs`: `CombatEventSubjects`를 쓰고, 중복 인스턴스는 `TryAdd`로 첫 위치를 쓴다.
- **시험:**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatConcurrentGroupsTests`(새) | 직접 피격 한 묶음, 넉백 = 출처 묶음, 다른 그룹, 폭발, 섞인·미지정 근원·주체 없음, 장애물 주체, 행동 경계, 중복 인스턴스 | 8개 통과 |
  | `OrderedCombatSchedulingTests`(새, 가짜 재생) | 같은 순간 동시 시작, 전역 차단 표시는 옛 순서, 같은 유닛 순서, 묶음 뒤 사건(Q-C2 A), 넉백(자기 반응 뒤, 한 걸음씩), 시작 전 앞 사건(C-1 제한), 취소·늦은 완료, 끝난 뒤 다시 재생, Ending 차단, (G, G2, G), 동기 완료 연쇄, 건너뛰는 사건 | 11개 통과, Unity 전용 1(경고 로그) |
  | 시험 래퍼 3개 | 시작 조건 전달 | Unity 전용 |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 39묶음 요약과 추가 묶음이 `c1base`와 같다(`c1f`). `CombatProgressIntegrationTests` 실패 5개는 기준선과 같은 시험이다.
- **상태:** 코드 완료. 화면 동작은 이전과 같아야 한다. Unity Test Runner 확인 대기(이동·적 간격·공격·기차·광란·H-4 시험).
- **커밋 때 새 파일(.cs + .meta):** `C/CombatEventSubjects.cs`, `C/CombatConcurrentGroups.cs`, `T/CombatConcurrentGroupsTests.cs`, `T/OrderedCombatSchedulingTests.cs`.

### 7.2 C-2 실행 (2026-10-17~18, 사용자 지시 "C-2 진행해, Phase 끝나면 하위 에이전트로 검증이번에 넘기고 마지막에 한번에 할게")

- **검토 에이전트:** 사용자 지시로 이번 Phase는 생략했다. C-2·C-3를 마지막에 한 번에 검토한다.
- **코드:**
  - `OrderedCombatPresentation`: C-1 제한(시작 전인 앞 묶음 사건은 지나가지 않음)을 풀었다(Task 0). 지뢰 이웃은 접촉 순간, Ballista 관통 대상은 앞 접촉 밖 후속을 기다리지 않고 시작한다.
  - `CombatUnitReactionSet`: 같은 유닛만 배타로 바꾸고 `IsPlayingFor(id)`를 더했다. 기차의 사망 겹침 가드(`CanOverlapDeaths`)는 그대로다(C7).
  - `CombatSceneEventPresenter`:
    - 시작 조건·`RequireIdle`의 반응 판단을 사건 주체 기준(`ReactionBlocks`)으로 바꿨다. 주체 없는 사건·행동 사건은 모든 반응이 끝나야 한다.
    - Tick은 걸음 → 모든 반응을 매 프레임 진행한다("반응 중이면 반응만" 분기와 프레임 시작 걸음 겹침 플래그를 없앰). 간격·대기 사건은 행동 사건에만 생겨 반응과 함께 있지 않다.
- **바꾼 기존 시험(옛 계약을 단언하던 것):**
  - `CombatCodeMotionHitTests.Melee_MultiTarget_FirstAtHit_NextAfterPreviousReaction` → `Melee_MultiTarget_AllTargetsReactAtHit_Together`(히트 타이밍 Q4 A 인수 기준을 이 구조가 대신함)
  - `CombatUnitDeathTests.ConcurrentDeaths_…_AndBlockOrdinaryEvents` → `…_AndBlockEventsOfDyingUnits`(Presenter는 주체 기준. 다른 유닛 사건은 순차 재생·기차 재생이 막음)
  - `CombatUnitReactionTests`의 반응 중 차단 단언을 같은 유닛 사건으로 바꿨다.
  - `OrderedCombatSchedulingTests`의 C-1 제한 시험을 "먼저 시작"으로 되돌렸다.
- **새 시험(Unity 전용):**
  - `CombatSceneEventPresenterTests`: 같은 폭발의 두 적 피격·사망 동시(2케이스), 동시 반응 중 취소 정리
  - `CombatBallistaAttackTests`: Core가 먼 대상을 먼저 적어도 가까운 대상은 자기 접촉에 시작
- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행은 `c1base`와 같다. 차이는 Unity 전용 수 증가뿐이다(Presenter 30 → 32, Ballista 24 → 25). 순수 묶음은 모두 통과했다(`c2b`). `CombatProgressIntegrationTests` 실패 5개는 기준선과 같다.
- **상태:** 코드 완료, Unity 확인 대기.

### 7.3 C-3 실행 (2026-10-18, 사용자 지시 "C-3 진행해")

- **검토 에이전트:** C-2와 함께 마지막에 한 번에 한다(사용자 지시).
- **코드:**
  - 계약(C6): `ICombatMovementPlaybackPresenter.BeginStep(step, completed)`. `IsMovementComplete`를 계약에서 뺐다. `CombatSceneDisplay`·`CombatSceneEventPlayback`은 콜백을 넘긴다.
  - `CombatMovementPlayback`: 연결 수명(걸음·복귀 전달, 끊을 때 한 번 취소)만 맡는 얇은 도우미로 줄였다(폴링·한 걸음 가드 제거). 파일은 남겼다.
  - `OrderedCombatPresentation`:
    - 걸음별 완료 통지는 상태만 바꾼다(`StepDone`). 경로 끝 판단·`BeginMovementEnd`는 Tick 훑기가 한다(걸음 Tick 안 재진입 방지).
    - 한 걸음 가드를 없앴다. 같은 유닛 걸음은 앞 사건 완료를 기다려 차례로, 다른 유닛 걸음은 같은 묶음이면 함께 돈다.
  - 새 `CombatUnitMotionSet`: 유닛별 걸음 레인을 소유한다. 결과의 걸음 해석(`CombatStepContacts`)은 결과마다 하나를 레인이 공유한다. 레인 모두의 Gate가 허락해야 시작하며, 주체 레인 기준의 진행·겹침 조회와 전체 Tick·취소를 맡는다.
  - `CombatUnitMotionPlayback`: 레인 하나다.
    - 공유 해석으로 Gate를 만든다.
    - 걸음 사건 완료를 한 번만 알린다(같은 세대).
    - 보간 배치 소유를 유닛별로 끝낸다.
  - `CombatStepContactGate`:
    - 해석을 소유하지 않는다(생성자로 받음, `BeginResult`·`Clear` 제거).
    - 규칙 1 = 새 행동·이동자 자신의 사건(이동 포함). 규칙 3 = 그 밖은 막지 않음(순서는 순차 재생).
  - `FieldUnitPresenter`: 보간 배치 소유를 유닛 집합으로 바꿨다(`EndStepPlacement(id)`).
  - `CombatSceneEventPresenter`:
    - 걸음 레인 집합 사용
    - `MotionBlocks`: 주체 레인 기준. 주체 없는 사건·행동 사건은 모든 레인이 겹침 허용이어야 한다.
    - 걸음 전 간격은 자발적 첫 걸음에만 설정
    - `IsMovementComplete`는 진단·시험 조회로 남김
- **시험:**
  - 순수: `OrderedCombatSchedulingTests`(넉백 두 대상 함께 걸음, 같은 유닛 걸음 차례) 12개 통과(Unity 전용 1). `CombatStepContactGateTests`(공유 해석·레인 규칙 1·3) 8개, `CombatPresentationCleanupTests` 5개(6케이스) 통과.
  - Unity 전용: `CombatSceneEventPresenterTests`(한 타격의 두 대상 넉백 같은 프레임 출발·도착, 동시 걸음 중 취소 정리)
  - 계약 변경으로 고친 시험: 시험 가짜·래퍼 7곳(`CombatViewSystemTests.FakeDisplay`, `FrenzyViewContractTests`, `CombatMovementViewTests`, `CombatPresentationCleanupTests`, Ballista·Mortar·Standard·Enemy 래퍼), 직접 호출 17곳(완료 콜백 추가), 표시 `IsMovementComplete` 단언 3곳(`IsPresentationComplete`·완료 통지 단언으로)
- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행은 `c1base`와 같다(`c3e`). 차이는 Unity 전용 수 증가뿐이다(Presenter 30 → 34, Ballista 24 → 25). 순수 묶음은 모두 통과했다. `CombatProgressIntegrationTests` 실패 5개는 기준선과 같다.
- **상태:** 코드 완료, Unity 확인 대기. 재생 확인: 같은 타격으로 밀린 두 대상이 함께 걷는지, 자발적 경로·경로 끝 복귀·적 간격이 지금과 같은지, H-4 접촉, 기차(넉백 경로 사이에 묶음 밖 사건이 끼면 차례로, §3 기대치).
- **커밋 때 새 파일(.cs + .meta):** `C/CombatUnitMotionSet.cs`(C-1 새 파일 4개와 함께).

### 7.4 C-4 실행 — 문서·인계 (2026-10-18, 사용자 지시 "C-4 진행하고 하위 에이전트로 전체 검증해")

- **문서:**
  - 설계 머리말에 구현 상태
  - 히트 타이밍 설계 Q4에 "동시 처리 구조로 대신함"
  - 히트 타이밍 계획 §5.1 연결(C-1 때)
  - CLAUDE.md 현재 상태, 이 기록
- **Factory·공유 문서:** C-1~C-3에서 `CombatSceneDisplayFactory`는 바뀌지 않았다(사건 Presenter 생성자 그대로). Framework 공유 문서에 더할 절이 없다.
- **viewDocs 안내(새 동시 경우를 더하는 방법):** 사용자 결정(2026-10-18) "분리 작업 뒤에 쓰자". Presenter 분리로 유닛 자원 판단 위치가 바뀌므로, 분리 완료 뒤 그 구조 기준으로 `docs/superpowers/viewDocs/`에 짧은 안내를 쓴다. 담을 것: Core가 같은 근원 열쇠를 주면 코드 변경 없이 묶임, 끊긴 넉백 경로 규칙, 새 유닛 자원을 더할 곳.
- **재생 확인 통합 목록(사용자, Unity):**
  1. ShotGun·곡사·포효 범위로 여러 대상을 맞히면 모두 같은 히트에 흔들린다.
  2. 한 번에 여러 대상이 죽으면 사망 모션이 함께 시작한다.
  3. Ballista 관통: 각 대상이 화살이 닿는 순간 흔들리고, 앞 대상 반응과 겹친다.
  4. 지뢰·화약통 폭발: 맞은 유닛들이 함께 반응한다. 걸음 중 지뢰면 이웃은 접촉 순간, 이동자는 칸 도착 뒤다.
  5. 같은 타격의 넉백: 각자 반응이 끝난 뒤 함께 걷는다. 경로 사이나 경로 안에 위치 보정 해제·넉백 가시 피해·중간 칸 발동이 끼면 그 대상은 앞 대상 경로가 끝난 뒤 걷고, 경로 중간에 멈추지 않는다(§3 기대치 정정, C-4 결정 (a)).
  6. 광란 같은 대상 연속 피격·한 대상의 여러 칸 넉백은 차례로다.
  7. 대상이 하나인 공격, 자발적 경로·경로 끝 방향 복귀, 적 행동 간격, 기차, H-4 걸음 접촉은 지금과 같다. 자발적 걸음의 남은 연출 중 다른 유닛 이동도 지금처럼 걸음 끝 뒤다(C-4 결정). 남는 차이: 강제 걸음(넉백)의 접촉 묶음이 일찍 풀린 뒤의 다른 유닛 이동·복귀는 걸음 끝이 아니라 그 걸음 사건 완료를 기다린다. 검토에서 지금 Core로 이 경로가 생기는 경우는 찾지 못했다(타일 접촉은 몸 조회가 없어 보간 끝에 해제, 넉백 가시는 접촉 묶음 없음).
  8. 취소(포기·연결 해제) 뒤 남는 흔들림·걸음이 없다.
  9. 여러 대상 피격 사이에 근원 없는 사건(피해 뒤 아이템 반응·위치 보정 해제·근원이 섞인 피해)이 끼면 그 지점부터 차례로 돈다(보수적, 멈춤 아님, C-4 검토 런타임 4).
- **Unity Test Runner 확인 대상:**
  - 새·바뀐 시험: `CombatSceneEventPresenterTests`, `CombatBallistaAttackTests`, `CombatCodeMotionHitTests`, `CombatUnitDeathTests`, `CombatUnitReactionTests`, `CombatUnitReactionSetTests`, `CombatRailwayContactSceneTests`
  - 계약 변경 회귀: `CombatMovementViewTests`, `CombatEnemyPresentationTests`, `CombatViewSystemTests`, `CombatPresentationCleanupTests`, `FrenzyViewContractTests`, `CombatStandardAttackTests`·`CombatMortarPresentationTests`(래퍼), `OrderedCombatSchedulingTests`의 Unity 전용 1
  - 기차 회귀 기준(설계 C7): `CombatRailwayContactPlaybackTests`, `RailwayContactScheduleTests`
- **후속·남은 위험:**
  - 기차 이전(설계 C7): 기차 사망을 "접촉 시각 해제가 있는 같은 순간 묶음"으로 옮기면 `TryConsumeDeath`·`BeginConcurrentDeath`·`CanOverlapDeaths`가 사라진다. 회귀 기준은 기차 시험 묶음 3개다.
  - 가시밭 적용(`ThornFieldApplicationCalculator`)은 Core가 칸마다 다른 그룹을 줘서 동시가 아니다. 원하면 Core에 그룹 공유를 요청한다(데이터 경계 인계).
  - 넉백 동시 걸음이 화면에서 잠깐 겹칠 수 있다(Core는 대상 순서로 경로 계산, 설계 §5).
  - 사건 Presenter 생성자 선택 인자 수(H-4c 검토 8)와 묶음 해석 2벌(스케줄러 `CombatConcurrentGroups` 안의 후속 대응 인스턴스, 공격 재생의 후속 대응) 정리는 구조 재평가 후보다.
  - 레인 사전(`CombatUnitMotionSet._lanes`)은 전투 동안 줄지 않는다(유닛 수만큼, C-4 검토 런타임 5).
  - `CombatMovementViewTests` 가짜 표시는 걸음 완료 칸이 하나다. 같은 묶음 넉백 둘을 그 시험에 넣으려면 칸을 목록으로 바꾼다(C-4 검토 11).

### 7.5 통합 검토 반영 (2026-10-18, 사용자 지시 "검토 결과 나오면 반영하고 보고해줘")

- **검토:** §6.2(하위 에이전트 2개). 설계 규칙을 바꾸는 두 건은 구현 전에 물었다 → 사용자 결정 (a) "그 대상만 차례로", "자발적 걸음은 HEAD대로"(설계 §6.1).
- **코드:**
  - `CombatConcurrentGroups`: 끊긴 넉백 경로의 이동을 묶음 밖으로(`SplitInterruptedPaths`, 판정을 모두 마친 뒤 바꿈).
  - `CombatStepContactGate`: `BeginStep(moved, voluntary)`. 자발적 걸음이면 규칙 1이 모든 이동 사건(`Moved`·`Returned`·`Relocated`)을 걸음 끝까지 막는다. `CombatUnitMotionPlayback`이 자발 여부(걸음 전 회전 여부와 같은 값)를 넘긴다.
  - `CombatSceneEventPresenter`:
    - 시작 조건도 `MotionBlocks`를 써서 시작 전제와 같은 판단을 쓴다.
    - 간격 설정은 `StartGap` 하나로 모으고, 레인이 진행 중이면 예외를 낸다(지금은 닿지 않는 전제 확인).
    - 진단용 `IsMovementComplete`를 지웠다.
  - `CombatUnitMotionSet`·`CombatUnitMotionPlayback`: 진단용 `IsStepComplete` 제거. 레인 생성자의 공유 해석(`contacts`)을 필수 인자로 바꿨다.
  - 낡은 주석: Presenter 클래스 설명·`RequireIdleFor`, 걸음 Gate 해제 설명, 레인 클래스 설명, 순차 재생 클래스 설명, `CombatHitFollowUps.IsMovement`.
- **시험:**
  - 지움: `CombatBallistaAttackTests.EncounterOrderDoesNotReorderResultEvents`. 옛 Q4 A 순서를 단언해 Unity에서 반드시 실패한다. `PiercedTargets_StartAtOwnContact_EvenWhenCoreListsFarTargetFirst`가 같은 사건을 새 규칙으로 고정한다.
  - 순수, 모두 통과:
    - `OrderedCombatSchedulingTests` +5 = 17. 늦은 걸음 완료(취소 뒤·이전 결과), Tick 안 걸음 완료 → 같은 Tick 훑기에서 경로 끝·다음 사건, 넉백 묶음 뒤 사건(Q-C2 A), 묶음 밖 걸음 장벽, 끊긴 넉백 경로.
    - `CombatConcurrentGroupsTests` +2 = 10. 끊긴 경로는 묶음 밖, 한 걸음 경로와 경로 뒤 사건은 묶음에 남음.
    - `CombatStepContactGateTests` +1 = 9시험·10케이스. 자발적 걸음 / 강제 걸음과 다른 유닛 이동.
  - Unity 전용:
    - `CombatSceneEventPresenterTests` +3 = 37. 엇갈린 두 걸음의 배치 소유, 같은 대상 2칸 넉백, 걸음 중 지뢰(이웃은 접촉 순간, 이동자는 도착 뒤). 동시 걸음 중 취소는 걸음 절반에서 하도록 바꿨다.
    - `CombatUnitReactionSetTests` +1. 다른 유닛 피격 동시, 같은 유닛 거절.
    - `CombatRailwayContactSceneTests`: 동시 사망 재생 중 다른 사건 차단 단언.
    - `CombatEnemyPresentationTests`: `IsMovementComplete` 단언 9곳을 걸음 완료 통지 횟수로 바꿨다.
- **회귀:** MSBuild 오류 0, 새 경고 0(`c4s`). 관리 실행 요약은 `c1base`와 같다. 차이는 Unity 전용 수뿐이다(Presenter 30 → 37, Ballista 25 → 24는 옛 시험 삭제). 추가 순수 묶음은 모두 통과했다. `CombatProgressIntegrationTests` 실패 5개는 기준선과 같다.
- **상태:** C-1~C-4 완료. Unity Test Runner·재생 확인 대기(§7.4 목록).
- **커밋 때:**
  - 새 파일(.cs + .meta)은 §7.1·§7.3의 5개 그대로다. 이번 반영으로 새 파일은 없다.
  - `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`은 이 작업과 무관한 편집기 캐시 변경이라 빼는 것을 권장한다.
