# 전투 히트 타이밍 계획 (2026-10-16, 3차: 추가 검수 반영·사용자 결정 분리)

- 설계: [히트 타이밍 설계](../specs/2026-10-16-combat-hit-timing-design.md) H1~H9a, 사용자 결정 Q1(포효 현행 유지)·Q2 A(넉백 대기 유지)·Q3 B(미교차는 접촉 셀 투영)·Q4 A(다중 대상 순차, 동시 반응은 판정 작업 뒤 후속 §5.1). 정책·선택지의 원본은 설계이며 이 문서는 구현 Task·검증·진행 조건만 관리한다.
- 진행: Phase마다 멈추고 보고한다. Claude가 직접 구현한다. 커밋·push는 사용자가 한다.
- 현재 상태: 사용자 요청으로 **문서만 3차 보완**했다. H-1~H-5 구현은 시작하지 않았으며 테스트 실행도 하지 않았다. 이번 문서 갱신은 구현 진행 지시가 아니다.
- 경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 착수 전 (사용자)

- Unity Test Runner 기준선:
  - `CombatEnemyPresentationTests`(SV8 전까지 실행된 적 없음)
  - `CombatStandardAttackTests`·`CombatBallistaAttackTests`·`CombatBallistaAssetTests`·`CombatMortarPresentationTests`·`CombatUnitReactionTests`·`CombatUnitDeathTests`·`CombatRailwayContactPlaybackTests`·`CombatRailwayContactSceneTests`·`FieldRailwayPresenterTests`·`RailwayContactScheduleTests`
  - `RailwayContactScheduleTests`는 H-1에서 `RailwayTravelPath`의 면 접촉 식을 옮기므로 넣었다. 관리 실행 39개 목록에도 없어서, 지금까지 이 시험의 기준 결과가 없다.
- 보스 활성화 작업은 `cbf8073b`로 커밋됐다. 그 작업의 Unity 확인을 먼저 받아 두면 회귀를 가리기 쉽다(권장).
- 사용자 결정은 설계 §6을 따른다. Q1~Q4가 모두 결정돼 H-1~H-3의 결정 대기 조건은 없다. 결정은 구현 진행 지시가 아니며 Phase마다 사용자 지시를 받는다.
- **증거 기록:** 관리 회귀 묶음(기존 기준 39개 + 해당 Phase의 새 시험)의 실제 목록·실행 명령·결과를 기록한다. Unity 시험은 실행 여부·성공/실패·미실행 사유를 구분한다. MSBuild/관리 시험 통과와 Unity 검증 완료를 섞지 않는다. 목록 작성만으로 Phase 검증 완료라고 표시하지 않는다.

## 1. Phase H-1 — 공통 계약 이관과 대기 정책 보정 (H1~H5, H9a)

**목표:** 공통 부품을 만들고 Ballista를 그 위로 옮긴다. 공격별 사건 완료 시점과 접촉 계산 기준은 유지하되 아래 대기 정책을 보정한다. 박치기·직선·곡사는 이 Phase에서 `AnimationEnd` 정책을 사용한다. Q3 B는 H-2(직선)·H-3(Ballista)에서, Q4 A는 H-2·H-3 인수에서 적용한다. H-1에서는 적용하지 않는다.

| H-1에서 달라지는 동작 | 설계 원본 | 검증할 기대값 |
|---|---|---|
| 관통 접촉 밖이지만 대응된 후속 사건을 전체 공격 종료까지 대기 | H2 | 해당 후속은 비행·자원 정리 전 시작하지 않음 |
| 공격자·이동 차단을 후속 해제보다 먼저 적용 | H3 | 대상이 해제돼도 공격자 자세와 이동은 겹치지 않음 |
| 소환을 공통 행동 경계에 포함 | H9a | 소환 전 잔여 공격을 기다리고 이동 경로 끝을 분리 |

기존 Ballista의 발사 완료·셀 투영 접촉 시점, 일반 공격의 전체 완료 시점, 기차의 점 기준 계산·동시 사망은 유지할 동작이다. 기존 시험 중 위 세 정책에 해당하는 단언은 새 기대값으로 바꾸고 실행 기록에 이유를 남긴다.

**파일:**
- 새 파일(+ .meta): `C/CombatHitGate.cs`, `C/CombatActionBoundary.cs`, `C/Display/CombatBody.cs`, `C/Display/CombatContactGeometry.cs`
- 이름 변경: `C/CombatBallistaImpactMap.cs` → `C/CombatHitFollowUps.cs`(.cs·.meta 함께, GUID 유지)
- 수정: `C/CombatAttackPlayback.cs`, `C/CombatSceneEventPresenter.cs`, `C/OrderedCombatPresentation.cs`, `C/Display/CombatBallistaAnimation.cs`(도달 집합을 Gate 알림으로 교체), `C/Display/FieldUnitPresenter.cs`, `C/Display/RailwayTravelPath.cs`(면 접촉 식 이전), `C/ICombatEventPlayback.cs`(completed 뜻 문서)

1. `CombatContactGeometry`: 선분 접촉(Minkowski + slab)과 면 접촉(`min dot`). `RailwayTravelPath.TryGetContactTime`이 면 접촉 식을 부르게 한다(결과 같음).
2. `CombatBody`: 필드 로컬 XZ 중심 + 반 크기. 점 = 반 크기 0.
3. `FieldUnitPresenter`:
   - `TryGetBodyShape(EntityKey)`: H4의 설정 구간별 원본 모양 캐시·점 몸 대체·경고 수명을 구현한다. `Configure`·`Release` 때 무효화한다.
   - `BodyOf(facts, mapper)`: 현재 `FieldUnitScale`과 캐릭터/구조물의 표시 함수를 사용해 필드 로컬 몸을 만든다. 런타임 사실 목록은 보관하지 않는다.
4. `CombatHitFollowUps`: 일반 공격까지 대응을 일반화한다. H2에 따라 접촉 대상이 아닌 관통 후속은 "전체 공격 종료 해제"로 구분한다. 기하 미교차와는 다른 경우다.
5. `CombatActionBoundary.IsAction(e)`: H9a의 분류를 Presenter·`OrderedCombatPresentation`·Gate가 같이 사용한다. 위 동작 변경 표에 맞춰 시험 기대값을 갱신한다.
6. `CombatHitGate`: H3의 API·완료 정책·상태 전이를 구현한다. 발사 통지와 대상 해제는 분리하고 `EndPose` 대신 `EndAnimation`을 사용한다. 빈 해제·중복 신호·취소·이전 공격 신호의 처리는 H3을 따른다.
7. `CombatAttackPlayback`:
   - `_ballistaEvent`·`_impactMap`·Ballista 전용 `CanBegin`·`BlocksNextEvent`·`tickingBallista` 판정을 Gate 상태로 바꾼다.
   - Ballista는 `_reached` 대신 `Release(id)`, 발사 때 `NotifyFired`를 호출한다. 셀 투영 접촉 계산은 H-3까지 유지한다. 일반 공격은 전체 종료 정책으로 연결한다.
   - Gate 준비는 애니메이션 시작보다 먼저, `EndAnimation`은 기존 정상 완료·자원 반환 뒤에 연결한다. 시작 실패·재진입·미지원 프로필·포효 경로는 H3대로 처리한다.
8. `CombatSceneEventPresenter`: `RequireIdle`와 Tick의 겹침 판단을 H3의 순차 진행 허용 상태에 맞춘다. H4의 현재 표시 사실 → 몸 조회 대리자를 기존 공격 재생 생성 지점에 연결한다. 조회만 준비하고 몸 기준 접촉은 H-2/H-3에서 사용한다.
9. 시험:
   - **관리 실행:**
     - `CombatContactGeometryTests`(점·사각형, 시작 겹침, 안 닿음, 축·대각, 면 접촉, 2×2 점유 중심과 경로가 어긋난 작은 몸/점 몸의 미교차). 미교차 대체(Q3 B)는 H-2에서 투영 식과 함께 검증한다.
     - `CombatHitGateTests`(H3 정책별 완료, 순서 1 → 2 → 3, 공격자 후속 대기, 활성 공격 교체 시 해제 초기화, 빈 해제·중복 신호·전체 종료 대체 완료·Clear 뒤 늦은 신호). 특히 Ballista 발사 후 첫 접촉 전의 일반 사건은 허용하고 미해제 피해는 대기한다.
     - `CombatHitFollowUpsTests`(일반·관통·접촉 외 관통 후속·다음 공격에서 끊김·가해자 불일치)
     - `CombatActionBoundaryTests`
   - **Unity 전용:** 몸 모양(콜라이더 있음·없음·크기 0, 중심 오프셋·회전·루트 크기·현재 배율·필드 좌표 변환), 재설정 후 캐시/경고 초기화, 캐릭터·구조물 조회 픽스처 시험. 기존 표시 풀과 설정이 다른 문제를 새 캐시로 숨기지 않는다.
   - **통합:** Ballista 발사 직후 효과 사건 → 미접촉 피해 대기 → 접촉 후 피해 시작, 발사/완료 콜백 중 취소·재진입, 일반 공격의 기존 완료 시점·포효 순서 유지. 위 동작 변경 표의 세 항목 외 Ballista·레일 회귀는 유지한다. §2.1 중 공통 수명과 Ballista에 해당하는 조건을 확인한다.

**완료:** MSBuild 오류 0, 해당 관리 회귀 통과, Unity 시험·위 동작 변경의 확인 결과를 §0 기준으로 기록한다. 미실행 Unity 검증이 있으면 "코드 완료·Unity 확인 대기"로 보고한다. Phase 뒤 멈추고 사용자 리뷰를 받는다.

## 2. Phase H-2 — ① 코드 공격 모션 (H6)

**진행 조건:** H-1 리뷰 뒤 사용자 진행 지시. 결정 대기는 없다(Q3 B·Q4 A).

**파일:** `C/Display/CombatMeleeAttackAnimation.cs`·`CombatLinearAttackAnimation.cs`·`CombatMortarAnimation.cs`, `C/Display/CombatContactGeometry.cs`(점 투영 식 추가), `C/CombatAttackPlayback.cs`, `C/CombatSceneEventPresenter.cs`(H4 조회 연결 점검), 시험.

1. 세 애니메이션에 히트 대리자를 더하고 효과음·전체 완료와 나눈다. 직선은 H4의 발사 시점 조회와 H5 기하를 사용해 `PrimaryTargetId` 몸의 접촉 진행률을 얻고 `FireLinear` 진행률로 알린다.
   - Q3 B는 공격 재생의 시각 결정 경계 한 곳에 반영한다. 몸과 궤적이 안 닿으면 주 셀(`PrimaryCell`)을 경로에 투영한 진행률을 쓰고, 주 셀 사실이 없으면 오류다.
   - 같은 경계에서 진단 경고를 낸다(설계 §6 Q3: 이유·공격자·대상·키, 결과마다 이유·키당 한 번, 점 몸 미교차는 제외). 경고 기록은 `BeginResult`·`Cancel`에서 비운다.
   - 점 투영 식은 `CombatContactGeometry`에 더한다. 지금 `CombatBallistaAnimation`에 있는 같은 식은 H-3에서 이 식으로 바꾼다.
2. `CombatAttackPlayback`은 일반 공격을 H3의 `FirstRelease` 정책으로 바꾸고 히트 대리자를 Gate의 `ReleaseAll`에 잇는다. `EndAnimation`은 전체 종료·자원 반환 뒤에만 연결한다.
3. 포효 `TryBeginFacing`은 히트 대리자를 비우고 Gate에 참여시키지 않는다(Q1 결정: 기존 전체 완료 유지).
4. **바꿀 시험 묶음(완료 시점 단언):**
   - `CombatStandardAttackTests` 대부분. 예: 타격 때 `completed == 0`, 완료 콜백에서 다음 공격 시작.
   - `CombatEnemyPresentationTests`(공격 대기·순서 시험).
   - `CombatMortarPresentationTests`.
   - `CombatUnitReactionTests`(Ballista 픽스처 재사용 부분 확인).
   - 포효 시험: `CombatStandardAttackTests`의 포효 세 개, `CombatAbilityMotionPlaybackTests`. 결과가 같아야 한다.
5. **새 인수 시험:**
   - 단일 대상의 첫 반응이 타격/접촉에서 시작하지만 다음 공격·이동(넉백 포함, Q2 A)은 전체 종료까지 기다리는지 확인한다.
   - 같은 결과의 앞선 이동 뒤 발사, 캐릭터·구조물 몸 조회를 검증한다.
   - Q3 B: 직선의 미교차 → 주 셀 투영 진행률, 주 셀 없음 → 오류, 투영 식 단위 시험.
   - Q3 경고(Unity 전용, `LogAssert.Expect`): 미교차 때 1회, 같은 결과의 같은 키 두 번째는 없음, 다음 결과에서 다시 1회, 점 몸 미교차는 없음, 정상 교차는 없음.
   - Q4 A: 다중 대상은 첫 대상이 히트 때, 뒤 대상은 앞 반응이 끝난 뒤 Core 사건 순서대로 시작한다.
6. **공통 수명:** 아래 §2.1을 일반 공격에도 적용한다. 기존 시험의 완료 시각 숫자만 바꾸는 것으로 끝내지 않는다.

**완료:** H-1과 같은 증거 구분. 단일 대상은 히트 순간 반응 시작, 다중 대상은 Q4 A 순서, 넉백은 기존 대기(2026-10-17 H-2b에서 타격 순서로 바뀜), 포효는 기존 순서(H-2b에서 타격 순간 히트로 바뀜)를 확인한다. Phase 뒤 멈춘다.

### 2.1 공통 수명 통합 인수 — 사건 완료 후 잔여 연출

H3 계약을 실제 Presenter·Ordered 재생·결과 완료 경계까지 연결해 검증한다. H-1은 공통 Gate/기존 Ballista 경로, H-2는 조기 완료를 새로 적용한 일반 공격, H-3은 변경된 접촉 시각과 실패 경로에 적용한다.

| 조건 | 확인할 보장 |
|---|---|
| 히트 후 반동·투사체·반응이 남은 동안 Cancel·Detach·소유 유닛 풀 반환 | 해당 Gate 상태 폐기, 자세·발사체 정리, 늦은 성공 통지·결과 최종화 없음 |
| 히트 콜백·뒤 반응 시작·정상 종료 자원 반환에서 예외 | 기존 오류 경계로 전달, 성공 결과 확인 없음, 다른 자원 정리도 시도 |
| 모든 후속 사건 소비 후에도 공격/반응 잔여 표시 존재 | `IsPresentationComplete` 전 최종 표시·`AcknowledgePresented` 없음 |
| 같은 결과의 다음 공격·같은 대상 재공격 | 이전 해제 집합·완료 콜백·지연된 신호 재사용 없음 |
| 히트와 전체 종료가 같은 Tick | 사건 완료 정확히 1회, 새 반응에 이전 프레임 시간 중복 적용 없음 |
| `FireTime=0` 또는 완료 콜백에서 취소·다시 연결 | Gate가 콜백 전에 준비되고, 이전 호출의 후처리가 새 공격 상태를 지우지 않음 |
| 자세 복귀가 비행보다 먼저 끝나는 느린 투사체 | 전체 애니메이션과 자원 정리 전 다음 행동·공격자 사건 시작 금지 |

단위 시험은 정책 자체를, 통합 시험은 해당 정책이 호출 순서·자원 수명과 연결되는 것을 검증한다. 기존 시험으로 이미 증명되는 보장은 중복 시험 대신 해당 시험과 실행 결과를 기록한다.

## 2.2 Phase H-2b — 포효 히트와 넉백 타격 순서 (2026-10-17 Q1·Q2 재결정)

**진행 조건:** 사용자 지시("포효 넉백도 타격 순서에 맞춰, 강조는 포효가 끝난뒤, 작아지기와 겹쳐도 됨", "일반 공격 넉백도 박치기 타이밍에 맞춰야지", 연결은 사건 순서). H-1~H-3 위에서 진행한다.

**파일:** `C/CombatHitGate.cs`, `C/CombatHitFollowUps.cs`, `C/CombatAttackPlayback.cs`, `C/CombatSceneEventPresenter.cs`, 시험(`CombatHitGateTests`·`CombatHitFollowUpsTests`·`CombatCodeMotionHitTests`·`CombatStandardAttackTests`).

1. **출처 일반화:** Gate·후속 대응의 출처를 `CombatAttackEvent` 전용에서 `CombatEvent`(공격 또는 반응 공격 결과가 있는 포효)로 넓힌다. 가해자·공격 그룹·관통 기록은 출처에서 읽는다.
2. **이동 후속(H2):** 출처부터 다음 새 행동 전까지의 `Moved`·`Returned`·`Relocated`를 이동한 유닛의 후속으로 대응한다. 히트 해제 대상에도 넣는다(관통이면 접촉 대상만).
3. **Gate 규칙 1(H3):** 활성 출처의 이동 후속은 규칙 2로 판단한다. 대응되지 않은 이동과 공격자 자신의 이동은 지금처럼 전체 종료까지 기다린다.
4. **포효 수명(`CombatAttackPlayback`):**
   - 포효 시작 때 Gate를 `FirstRelease`로 연다.
   - 박치기(`TryBeginFacing`)의 히트는 포효 출처의 모두 해제다.
   - 작아지기 끝(강조 끔 뒤)에 `EndAnimation`을 부른다. 시작 실패는 그 포효만 정리한다.
5. **Presenter:**
   - 능력 동작 중 차단(`CanBeginPresentationEvent`·`RequireIdle`)은 Gate가 겹침을 허용하면 풀린다. 소환 등장처럼 Gate가 없는 능력 동작은 그대로 막는다.
   - Tick은 겹침 허용 중에 능력 동작을 진행한 뒤 반응·걸음도 진행한다. 이 프레임에 공격이 끝나 재개된 능력 동작에는 같은 프레임 시간을 다시 쓰지 않는다.
   - `BeginStep`·`BeginMovementEnd`·`ShowReturn`은 그 사건이 지금 시작 가능할 때(`CanBeginPresentationEvent`)만 겹침을 허용해 시작한다.
6. **시험:**
   - 순수: 포효 출처 대응, 이동 후속, 규칙 1 예외(대응된 넉백은 해제 뒤 시작, 대응 안 된 이동·공격자 이동은 대기).
   - Unity:
     - 박치기 넉백이 공격자 복귀 중에 시작한다(기존 `Melee_KnockbackStepAfterHit_WaitsForFullAttackEnd`를 새 기대값으로 바꿈).
     - 포효 타격 순간 피해 → 작아지기와 겹친 반응·넉백 → 강조는 끝난 뒤에 끈다 → 소환은 포효 끝까지 기다린다.
     - 대상 없는 포효는 기존과 같다.
7. **완료:** MSBuild·관리 실행 증거, Unity 대기 구분. 검토 에이전트 1개.

## 3. Phase H-3 — ② Ballista·④ 기차 처음 닿는 기준 (H7)

**진행 조건:** H-2 리뷰 뒤 사용자 진행 지시. Ballista의 몸 조회 실패·미교차는 Q3 B(encounter 셀 투영)다. 기존 기차의 오류 정책은 Q3로 바꾸지 않는다.

**파일:** `C/Display/CombatBallistaAnimation.cs`, `C/CombatAttackPlayback.cs`, `C/CombatSceneEventPresenter.cs`(H4의 표시 사실 조회 연결 점검), `C/Display/RailwayContactSchedule.cs`, `C/CombatRailwayContactPlayback.cs`, `C/CombatSceneEventPlayback.cs`(몸 변환 대리자 전달), `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(의존성 연결), `docs/superpowers/viewDocs/railway-view-guide.md`, 공용 변경 공유 문서, 시험(`RailwayContactSchedule`·`CombatRailwayContactPlayback`·`CombatRailwayContactScene`·`FieldRailwayPresenterTests`, Ballista).

1. Ballista는 H4의 발사 시점 조회 연결을 사용해 대상별 몸과 선분 접촉 시각을 얻는다. 미교차·몸 조회 실패는 Q3 B로 그 대상의 encounter 셀 투영 시각을 쓴다. H-2와 같은 시각 결정 경계·같은 투영 식(`CombatContactGeometry`)·같은 진단 경고를 사용한다(몸 조회 실패는 키를 모르면 대상 ID 기준). `CombatBallistaAnimation`의 옛 투영 식은 지우고 애니메이션/Gate에 중복 구현하지 않는다.
2. 기차 `Target`에 몸(모양 H4 + 사망 `Expected` 배치)을 넘기고 면 접촉으로 계산한다. `Expected` → 몸 변환 대리자를 Factory에서 기존 재생 조립 경로로 전달한다. 점 몸 생성자를 남기되, 옛 시험 유지와 새 몸 기준 검증은 구분한다.
3. viewDocs 규칙 문장을 고친다.
4. **새 검증:** 2×2 표적·작은 몸·점 몸·총구 오프셋으로 미교차가 가능한 경우, Q3 B(미교차·몸 조회 실패 → encounter 셀 투영 = 옛 시각, 각각의 경고), 동일 결과의 앞선 이동 뒤 발사, 관통 다중 대상의 Q4 A 순서, 기차의 사망 Expected 배치·기존 미도달 오류를 확인한다. §2.1 중 접촉 시각 변경에 영향받는 통합 조건을 회귀한다.
5. **공용 변경 공유:** 인계 §4.5에 따라 Factory/생성자 주입 변경·영향 범위·검증 상태를 민형님 공유 문서에 기록한다. 대상 문서는 같은 Factory의 기존 공유 문서 [`handoffs/2026-10-15-combat-scene-display-factory-framework-share.md`](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md)이며, 새 절로 보완한다(보스 활성화 변경은 그 문서 §5에 이미 있다). 작성과 외부 전달 여부를 구분하며 발송은 이번 작업에 포함하지 않는다.

**완료:** 같은 증거 구분. 실제 교차 대상은 몸에 처음 닿는 시점, 미교차/조회 실패는 Q3 B 셀 투영 시점, 관통 다중 대상 반응은 Q4 A 순서, 기차는 기존 동시 사망을 확인한다. 공유 문서 작성 상태를 기록하고 Phase 뒤 멈춘다.

## 4. Phase H-4 — ④ 이동 중 오브젝트·지형 접촉 (H8)

- **먼저 상세 설계**를 쓰고 사용자 확인을 받는다(설계 H8의 네 가지). Core 사실이 모자라면 Core 요청 문서에 적고 멈춘다.
- 오브젝트·지형 프리팹 BoxCollider 추가는 사용자 에셋 작업이다.
- **상세 설계(2026-10-17 작성, 결정 완료):** [이동 접촉 상세 설계](../specs/2026-10-17-combat-movement-contact-design.md) M1~M7. 조사 결론은 Core 사실로 충분하다(Core 요청 없음). 지금 걸어서 닿는 대상은 오브젝트 3종과 가시이고, 토네이도는 턴 경계 이동이라 대상이 아니다. 결정은 Q-M1 A·Q-M2 A다. Task는 아래 §4.1~§4.3이다.

- **사용자 결정(2026-10-17):** Q-M1 A(이동자 자신 반응은 걸음 끝 뒤), Q-M2 A(오브젝트는 접촉 순간 숨김). 구현은 사용자 진행 지시 뒤 Phase마다 멈추고 검토 에이전트 1개를 돌린다.

### 4.1 H-4a — 걸음 접촉 해석과 원본 몸 (상세 설계 M1·M2)

- **파일:**
  - 새 `C/CombatStepContacts.cs`(+ .meta)
  - 수정 `C/Display/FieldObjectPresenter.cs`·`C/Display/FieldThornPresenter.cs`(원본 몸 조회), `C/Display/CombatContactGeometry.cs`(필요하면 걸음 진행률 도움 식)
  - 시험
- **Task:**
  1. `CombatStepContacts.Begin(result)`: 걸음 구간(다음 이동·새 행동 전까지), 접촉 원본 사건(가시 소비·오브젝트 발동·이동 원본 변경이 들어간 칸에 있을 때), 접촉 묶음(원본 ~ 다음 원본 전)을 대응한다. 묶음 없는 앞 사건은 걸음 끝이다.
  2. 원본 몸 조회:
     - `FieldObjectPresenter.BodyOf(ObjectSourceFact, mapper)`: 프리팹 루트 BoxCollider, `Apply`와 같은 배치. 콜라이더가 없으면 점 몸이고 설정 구간마다 프리팹당 1회 경고한다.
     - `FieldThornPresenter.BodyOf(cell, mapper)`: 같은 규칙.
  3. 걸음 접촉 진행률: 이동자 `BodyOf`(시작 사실)의 반 크기 사각형이 시작 → 끝 중심을 지날 때 원본 몸과 처음 닿는 `t`. 닿지 않으면 1이다.
- **시험:**
  - 순수 `CombatStepContactsTests`: 구간 경계, 묶음 귀속, 강제 이동 걸음, 경로 도착 사건, 결과 다시 준비.
  - Unity: 오브젝트·가시 몸(콜라이더 있음·없음, 오프셋·회전·크기), 진행률(유닛 콜라이더 앞면이 원본 중심에 닿음).

### 4.2 H-4b — 걸음 접촉 Gate와 걸음 도중 시작 (M3·M4·M5·M7)

- **파일:**
  - 새 `C/CombatStepContactGate.cs`(+ .meta)
  - 수정 `C/CombatUnitMotionPlayback.cs`, `C/CombatSceneEventPresenter.cs`
  - 수정 `C/ICombatMovementPlaybackPresenter.cs`·`C/ICombatEventPlayback.cs`(뜻 문서)
  - 필요하면 `C/CombatMovementPlayback.cs`·`C/OrderedCombatPresentation.cs`
  - 원본 몸 조회 연결이 Factory 인자를 늘리면 `CombatSceneDisplayFactory`와 공유 문서
- **착수 전 확인(H-4a 검토 3, 설계 M5) — 사용자 결정 2026-10-17 "(a)로 가자": 걸음 도중 시작한 사건 끝의 `RenderEvents`가 이동자를 걸음 전 칸으로 되돌린다. 막는 방식 (a) 보간 중 유닛은 `RenderFacts`가 위치·회전을 쓰지 않음(추천) / (b) 걸음 보간을 대여로 바꿈.
- **Task:**
  1. 걸음 Gate: 활성 걸음, 묶음별 해제, 걸음 사건 완료(첫 묶음 해제, 없으면 보간 끝), 겹침 규칙을 둔다.
     - 규칙 1: 새 행동·모든 이동·이동자 자신의 사건(캐릭터 사건, `Characters`에 이동자가 있는 `CombatSoulEvent`) → 남은 걸음 연출 끝까지 대기
     - 규칙 2: 묶음 사건 → 그 묶음 해제 뒤
     - 규칙 3: 그 밖 → 사건 완료 뒤
     - 완료 1회, 늦은 신호 무시, 취소에서 비움.
  2. 걸음 재생:
     - 걸음 시작 때 묶음 진행률을 계산하고, 보간 Tick이 지나면 해제한다. `IsStepReleased`를 더한다(구현: 새 멤버 대신 기존 `IsStepComplete`의 뜻을 "걸음 사건 완료"로 바꿈, 설계 M3, §8.11).
     - 이동자 몸은 보간 중 표시 방향(걸음 방향으로 돈 뒤)으로 계산한다(설계 M2, H-4a 검토 8). "닿지 않으면 1" 정책을 걸음 재생 쪽으로 옮길지도 이때 정한다.
     - 걸음 중 이동자 위치 보존(위 착수 전 확인).
     - 사건 완료 뒤 보간을 계속하고, 미룬 방향 복귀(M3)를 처리한다.
  3. Presenter:
     - `IsMovementComplete` = 걸음 사건 완료.
     - `BeginMovementEnd`: 보간 중이면 방향 복귀를 미루고 바로 완료한다.
     - 겹침 허용 중에는 `RequireIdle`·`CanBeginPresentationEvent`가 걸음 보간 차단을 푼다.
     - Tick 순서는 공격 → 능력 동작 → 걸음 보간 → 반응이고, 겹침 판정은 프레임 시작 기준이다.
     - 공격 Gate와 걸음 Gate를 모두 만족할 때만 시작한다.
- **시험:**
  - 순수 `CombatStepContactGateTests`(규칙·완료·해제 순서·취소).
  - Unity:
    - 깃발·배지 접촉 순간 발동 사건이 시작되고 보간은 계속된다. 화면 변화는 H-4c(접촉 순간 숨김)부터다. 보유 효과 HUD는 결과 끝 Refresh에 바뀐다(보유 효과 사건은 표시 사실을 바꾸지 않음, H-4b 검토 5).
    - 지뢰 접촉 순간 폭발, 이동자 반응은 칸 도착 뒤(Q-M1 A).
    - 가시 접촉 순간 소비(자발적 이동·타일 강제 이동만. 공격 넉백 가시는 Core가 사건 없이 지워 지금처럼 결과 끝, 설계 §1.1).
    - 걸음 도중 사건 뒤에도 이동자가 걸음 전 칸으로 돌아가지 않음, 이동자 영혼 수집은 걸음 끝 뒤.
    - 접촉 없는 걸음·경로 끝 방향 복귀는 지금과 같다.
    - 넉백 걸음 중 오브젝트 발동은 공격 Gate와 함께 활성이다.
    - 취소·연결 해제.
  - 회귀: 이동·경로 끝·적 간격·넉백·포효·D8 묶음.

### 4.3 H-4c — 오브젝트 사건 시점 표시 (M6, Q-M2 A)

- **파일:** `C/Display/FieldObjectPresenter.cs`(`HideActivated(sourceId)`), `C/CombatSceneEventPresenter.cs`(오브젝트 발동 사건 시작 때 호출, Factory가 연결).
- **Task:** 발동 사건 시작 때 그 원본 인스턴스를 숨긴다. `Refresh`의 전체 `Render`가 최종 사실로 다시 맞추고 풀 반환도 그때 한다. 원본 목록을 따로 들지 않는다.
- **시험:**
  - Unity: 발동 순간 숨김, 결과 끝 `Refresh` 뒤 비활성 원본은 반환·활성 원본은 유지, 취소 뒤 `Refresh`.
  - 재생 확인: 깃발·배지·지뢰가 닿는 순간 사라지는지.

## 5. Phase H-5 — ③ 애니메이션 이벤트 (H9)

- 애니메이션이 들어온 뒤 착수한다. 지금은 계획만 둔다.

### 5.1 판정 작업 뒤 후속 — 다중 대상 동시 반응 (Q4 B) → 동시 처리 구조로 넓힘(2026-10-17)

- **2026-10-17 사용자 정의:** "동시 타이밍에 일어난 일들에 대한 병렬처리", "다 하는건데 이걸 일일이 만드는게 아니라, 동시 처리에 대한 구조를 잡고가자". 이 절의 Q4 B는 [동시 처리 구조 상세 설계](../specs/2026-10-17-combat-concurrent-presentation-design.md)(C1~C8, 결정 Q-C1 A·Q-C2 A·Q-C3 A 2026-10-17, 구현 C-1~C-4 2026-10-18 — [동시 처리 계획](2026-10-17-combat-concurrent-presentation-plan.md) §7)로 넓혀 대신한다. 아래 원래 항목은 그 설계 §6·C5로 옮겨 다룬다.

- 사용자 결정(2026-10-16): "Q4에 경우는 일단 이 판정 작업 끝나고 진행할거야". 이번 히트 판정 작업은 Q4 A(순차)로 끝내고, 동시 반응은 그 뒤에 진행한다.
- 착수 전에 별도 상세 설계와 계획을 쓰고 사용자 확인을 받는다. 이 계획의 Phase에 자동으로 넣지 않는다.
- 설계에서 정할 것(원본은 설계 Q4 보충):
  - `CombatUnitReactionSet`의 "다른 유닛 사망만 동시" 허용을 피격 반응으로 넓히는 방식
  - 다음 사건 시작 시점(집합 전체 종료 뒤인지), 사건별 표시 사실(체력) 반영 순서, 도중 취소·실패
  - Ballista 관통에도 적용할지
  - 넉백 대기(Q2 A)와의 관계: 뒤 대상 반응을 기다리던 넉백이 빨라진다
- 언제 시작할지(H-3 뒤인지, H-4·H-5까지 끝난 뒤인지)는 그때 사용자에게 확인한다.

## 6. 기능 변경의 독립성 — 대표 변경 시나리오

| 변경 | 고칠 파일 | 이해해야 할 코드 | 검증 |
|---|---|---|---|
| 박치기 타격 시점 조정 | `CombatMeleeAttackAnimation` | 그 애니메이션과 히트 대리자 | 박치기 시험 + 포효 시험 |
| 공격 완료 계약 변경(H3·H6 자체) | 모션 3개·`CombatAttackPlayback`·Gate·Presenter | H3 정책·전체 종료·Tick/자원 반환 순서 | §2 4번 묶음 + §2.1 통합 인수 |
| 새 공격 모션 종류(③ 포함) | DB Kind·새 애니메이션·`CombatAttackPlayback` 분배 | 히트 대리자 계약 | 새 모션 시험 + Gate 회귀. Presenter 무수정이 목표 |
| 몸 모양 원본 교체 | `FieldUnitPresenter.TryGetBodyShape` | `CombatBody` 형식·Configure 캐시 수명 | 몸 모양·재설정 픽스처 시험. 같은 몸 값/계약을 유지하면 공격 분배 수정 불필요 |
| 콜라이더 없는 처리 변경 | `FieldUnitPresenter.TryGetBodyShape` | 점 몸 규칙과 Q3 경계 | 몸 모양 시험 + 값이 바뀌는 경우 해당 접촉 시각 검증 |
| 표시 사실의 몸 조회 연결 변경 | Presenter의 조회/생성 지점·`CombatAttackPlayback` | H4의 현재 사실 소유권·발사 시점 호출 | 같은 결과의 앞선 이동 뒤 발사, 캐릭터·구조물 조회. 사실 목록 복제 없이 연결 |
| 미교차·몸 조회 실패 표시 정책 변경(Q3) | `CombatAttackPlayback`의 접촉 시각 결정 경계 | 기하 결과와 Core 명중의 구분 | 적용 대상 Linear/Ballista 정책 시험. 순수 기하·기차 오류 정책은 유지 |
| 행동 경계 분류 변경 | `CombatActionBoundary` | 분류를 쓰는 네 곳(Gate·후속 대응 끊김·강조·이동 경로 끝) | Gate·후속 대응·강조·이동 경로 끝 시험 |
| 기차 접촉 기준 변경 | `RailwayTravelPath`·`RailwayContactSchedule`·`CombatRailwayContactPlayback.Prepare`(+ 조립은 `CombatSceneEventPlayback`·Factory) | 면 접촉·몸 배치 | 레일 시험 묶음 |
| Q1 포효 히트 적용 | `TryBeginRoar`·`CombatAbilityMotionPlayback`·`CombatAttackPlayback.TryBeginFacing`(사건을 받지 않아 후속 대응을 따로 줘야 함)·`CombatHitFollowUps`(포효 대응)·Presenter `CanBeginPresentationEvent`(`_abilities.IsPlaying` 차단) | 포효 순서·Gate | 포효 시험 + Gate 회귀 |
| Q2 넉백 히트 직후(지금은 A로 결정, 다시 볼 때) | Core 요청(넉백 `Moved`와 공격 연결) + Gate + Presenter `BeginStep`·`TickPresentation` + 걸음 재생 | 반응·걸음 자세 겹침, 이동 경로 끝 판정(D8) | 이동·넉백·포효 시험 |
| Q4 다중 대상 동시 반응(후속 §5.1) | 별도 상세 설계에서 Presenter·반응 집합·Ordered 재생 영향 확정 | 사실 반영 순서·대상별 자세·집합 완료/취소 | 다중 대상 순서·공통 수명 통합 시험. `ReleaseAll` 변경만으로 처리하지 않음 |
| 기차를 Gate에 합치기 | `CombatRailwayContactPlayback`·`TryConsumeDeath`·`BeginConcurrentDeath` | 대응 근거가 `KilledUnitIds`(`DamageOrigins` 아님) | 레일 시험 전체 |

남는 공통 경계: 히트/발사 신호(출처 → Gate), 전체 종료·취소 수명, H4의 몸 조회 연결과 `CombatBody` 형식, Gate의 겹침 판정(`CanBeginPresentationEvent`·`RequireIdle`·Tick 반환), `CombatActionBoundary`. 공통 계약이 바뀌면 영향받는 출처·순차 재생을 함께 검증한다. 기능 내부 변경에 관계없는 전체 회귀를 관성적으로 붙이지 않으며 검증 범위의 근거를 실행 기록에 남긴다.

## 7. 검토 기록

### 7.1 설계 1차 검토 (2026-10-16, 사용자 지시: SSOT·SOLID·Framework·독립성 검토 에이전트 4개)

아래는 2차 문서가 만들어진 당시의 이력이다. "자세 완료"·"점 몸이면 결과 유지" 등 이후 정정된 표현은 현재 계약이 아니며 설계 3차와 §7.2를 우선한다.

네 검토가 공통으로 지적한 것과 반영:

| # | 지적(심각도·검토) | 반영 |
|---|---|---|
| 1 | Ballista 전용 겹침 판정이 세 곳(`CanBegin`·`BlocksNextEvent`·`Tick` 반환)에 있다. 계획은 한 곳만 옮겨 H-2에서 예외·반응 정지가 난다(높음·4개 모두) | H3 "겹침 판정은 Gate 상태에서만 파생". H-1에 Presenter 포함, `_ballistaEvent` 제거 |
| 2 | 규칙 1·2 우선순위 미정. 공격자가 후속이면 자세 대여 충돌(높음·SOLID) | H3 판단 순서: 대기 규칙(공격자·이동·행동 경계)을 맨 앞(1번)으로 옮겼다 |
| 3 | 공격 사건 완료 시점 모호, 해제 없을 때 완료가 오지 않음, 1회 보장 없음(중간·SOLID·Framework·SSOT) | H3 출처별 완료 + 해제 없으면 자세 완료 + Gate가 1회 보장 + `ICombatEventPlayback` 문서 |
| 4 | 몸을 대여 인스턴스 현재 Transform에서 읽으면 위치 SSOT가 깨짐(자세·크기 대여·앞선 이동·미생성)(높음·SSOT, 중간·SOLID·Framework·독립성) | H4 모양(프리팹, 키별 캐시) + 배치(Core 사실, `CenterOf`·`ModelRotation`) |
| 5 | 해제를 대상 ID만으로 기억하면 같은 대상 두 번째 공격이 미리 열림(중간·Framework) | H3 "활성 공격 + 대상", 교체 시 비움 |
| 6 | 행동 경계 분류 네 곳·소환 누락(중간·SSOT·독립성, 낮음·SOLID) | H9a `CombatActionBoundary` |
| 7 | 넉백 `Moved`에 `DamageOrigins` 없음 → Q2는 Core 사실 필요(높음·독립성, 중간·SSOT) | Q2에 Core 요청 선행 명시(직접 확인: `AttackKnockbackCalculator.cs:74`) |
| 8 | 기차 "공통 H5 사용"이 사실과 다름(면 투영 식이 따로 있음)(중간·SSOT, 낮음·나머지) | H5에 면 접촉 함수 추가, `RailwayTravelPath`가 사용 |
| 9 | "해제 상태 하나" 주장과 기차·H8 불일치, Gate 소유 미정(중간·SSOT·Framework·SOLID) | H1 "사건 묶음마다 원본 하나·한 사건은 한 곳", H3 소유 = `CombatAttackPlayback`, H8에서 재판단 |
| 10 | 완료 시점 변경은 공통 수명 변경, 영향 시험 다수, 기준선 필요(중간·독립성) | §0 기준선, §2 4번 시험 묶음, §6 행 추가 |
| 11 | 포효가 같은 생성 경로라 히트 콜백 분리 필요(중간·독립성) | H6 히트·완료 콜백 분리, `TryBeginFacing` 히트 비움 |
| 12 | H-3에 `CombatSceneEventPlayback`·Factory 누락(중간·독립성·Framework) | §3 파일 목록 |
| 13 | 직선 주 대상 원본 미정, 진행률 오버로드(낮음·Framework·SOLID·SSOT) | H6 `PrimaryTargetId`, `FireLinear` 진행률 |
| 14 | "점 몸 여부" 필드는 파생값(P7)(낮음·SOLID) | H4 점 = 반 크기 0 |
| 15 | ③ "조립 한 줄"·§4 "조립은 Factory" 부정확(낮음·SOLID·독립성) | H9·§4 문장 수정, §6 행 수정 |
| 16 | 관통 대응됐지만 접촉 대상이 아닌 사건의 처리(낮음·SSOT) | H2 "공격 자세 끝 해제" |
| 17 | 기존 재생 시험 유닛에 콜라이더 없음(낮음·독립성) | H-1 몸 모양 픽스처 시험, 기존 시험은 점 대체로 유지 |
| 18 | 이름 변경 .cs·.meta·doc 주석(낮음·Framework) | H2·H-1 4번 |

- **의견이 갈린 점 — Gate 소유:**
  - SSOT 검토는 H8·H9 출처를 위해 Presenter 수준을 권했고, Framework 검토는 `CombatAttackPlayback` 소유를 권했다.
  - 결정: ③은 공격 수명 안의 출처라 `CombatAttackPlayback` 소유로 충분하다. ④ 이동 출처는 H8 상세 설계에서 Gate를 올릴지 따로 둘지 정한다(설계 H1·H8 4번).
- **규칙 3 기본값:** 독립성 검토는 기본 "대기"를 권했다. 효과 사건이 후속 앞에 끼면 피격이 막히므로, 현행 Ballista와 같은 "시작"을 유지했다. 분류는 H9a 한 곳에 모아 새 사건을 넣을 때 한 곳만 본다.

### 7.2 추가 검수·문서 보완 (2026-10-16, Codex + 하위 에이전트 3개)

검수 역할은 SSOT / SOLID·기능 변경 독립성 / JMH Framework·FW 계약이다. 실제 코드와 문서를 대조했고, 사용자 후속 요청에 따라 아래를 문서에 반영했다. **구현·런타임 검증 완료를 뜻하지 않는다.**

| 지적 | 문서 반영 | 남은 사용자 결정 |
|---|---|---|
| 사건 완료·대상 해제·전체 종료가 혼재, Ballista 발사 신호 누락 | 설계 H3 정책/전이 표·`NotifyFired`·`EndAnimation`·겹침 및 재진입 계약, H-1 Task | 없음. 기존 완료 정책의 명확화 |
| Core 확정 대상과 표시 기하가 미교차할 때 처리 미정 | 설계 H5에서 계산과 정책 분리, Q3과 해당 Phase 의존 조건 | Q3. 권장안은 미승인 |
| 표시 사실은 Presenter 소유인데 공격의 몸 조회 연결 누락 | 설계 H4 대리자 연결·발사 시점 조회, H-1~H-3 파일/Task·앞선 이동 검증 | 없음. 사실 소유권을 유지하는 기술 보완 |
| H-1 "동작 변화 없음"과 실제 정책 변경 충돌 | H-1 제목·세 동작 변경·유지할 기준/시험 단언 구분 | 없음. 기존 설계에 이미 적힌 변경을 계획에 명시 |
| 사건 완료 뒤 잔여 연출의 취소·실패 통합 검증 부족 | §2.1 인수표, Phase별 적용 범위, 코드 완료와 Unity 확인 상태 구분 | 없음 |
| 몸 모양 캐시의 설정 수명 누락 | 설계 H4의 원본 캐시·현재 배율 적용·Configure/Release 무효화, 재설정 시험 | 없음. 기존 풀 재설정 정책 확장은 제외 |
| 모두 해제와 모든 반응 동시 시작을 혼동 | 설계 H6·Q4, H-2 다중 대상 완료 조건 분리 | Q4. 동시 선택 시 상세 설계 필요 |
| Factory 변경 공유 기록 Task 누락 | H-3 공유 문서 작성·전달 여부 구분 | 없음. 외부 발송은 포함하지 않음 |

원칙 평가: 후속 대응·진행 규칙·기하 분리는 SRP, 좁은 대리자는 ISP·DIP에 맞는다. Kind/분배 두 곳의 변경은 현재 OCP의 명시된 한계로 유지한다. LSP는 H3 완료/실패 계약과 §2.1 검증으로 보완한다. 기차와 공격은 사건 집합이 다르므로 Gate를 강제로 합치지 않는다. 확정적인 FW MUST 위반으로 판정한 사항은 없으며 수명·재진입 보완은 지침 §18·20·21, FW-103·104 및 인계 §4.6에 따른다.

## 8. 실행 기록

### 8.1 문서 3차 보완 (2026-10-16)

- 사용자 요청: 추가 검수 보완점을 설계·계획에 반영하고 구현은 하지 않으며 사용자 결정 사항을 분리.
- 변경 범위: 이 계획과 대응 설계 문서만. Q1~Q4 선택지·권장안은 설계 §6에 두고 미결정으로 유지했다.
- 상태: H-1~H-5 구현 미착수. 코드·에셋 변경, MSBuild, 관리/Unity 테스트 실행, 커밋·push, 공유 문서 외부 발송 없음.
- 후속 실행은 Phase별로 변경·검증 증거·남은 확인을 기록한다. 문서 보완을 Phase 구현 완료로 세지 않는다.

### 8.2 3차 코드 대조·Q1 결정 (2026-10-16)

- 사용자 요청: "계획 및 설계 업데이트 됬는데 다시 확인해줘" → 대조 결과 중 1~4번과 Q1 반영, 공유 문서 보완.
- 코드와 맞는 것을 확인했다(문서 변경 없음):
  - `CombatAttackPresenter.CompleteIfReady`의 순서는 완료 확인 → `animation.Release()` → 세대가 같고 해제 전일 때만 완료다. Ballista도 `_attack.Begin`을 지나므로 `EndAnimation`의 연결 지점은 일반 공격과 같다.
  - Ballista 전용 판정 세 곳의 줄 번호는 설계 §1과 같다(`CombatAttackPlayback.cs:63-64·214·237`, Presenter `:196-197·536-538`).
  - 구조물도 유닛 외형 표의 프리팹으로 그린다(`FieldUnitPresenter.CreateView`). 루트 크기 × `_unitScale`, 회전은 `RotationToWorld`로 H4와 같다.
  - `FireLinear` 진행률 오버로드(`CombatProjectilePool.cs:82`)가 있다. 기존 짧은 오버로드와 달리 `up`이 필요하다.
  - 인용한 FW-103·104, 지침 §18·20·21·25는 실제로 있다. §0 기준선의 시험 파일도 모두 있다.
- 반영:
  1. 설계 머리말 기준 코드를 `cbf8073b`로 바꿨다. §0의 "커밋 먼저" 권장도 고쳤다(커밋은 됐고 Unity 확인만 남았다).
  2. 설계 §1 경계 분류의 `CanBegin`에 포효를 더했다. 코드 `:80`이 포효를 막는다. H9a의 "Gate 규칙에 소환이 없다"는 그대로 맞다.
  3. H-3 5번의 공유 대상 문서를 기존 Factory 공유 문서로 지정했다. 인계 §4.5가 Factory를 공용 파일로 꼽고, SD-D1 선례 문서가 있다.
  4. §0 기준선에 `RailwayContactScheduleTests`를 더했다.
  5. Q1 결정을 설계 §6에 기록했다. 현행 유지이며, 실제 포효 연출이 들어올 때 다시 정한다.
  6. 사용자 요청 "나머지 결정 설명 더해줘"에 따라 설계 §6 Q2~Q4에 코드 대조 보충을 더했다. 내용은 Core 사건 순서(피해 전부 → 넉백), H-2 뒤 넉백 지연 범위, Ballista 종점과 접촉 셀 차이, 다중 대상 공격 종류, `CombatUnitReactionSet` 동시 사망 기반이다. 선택지와 권장 표기는 바꾸지 않았다.
- 함께 고친 누락: 보스 활성화 Phase 3·3b는 `CombatSceneDisplayFactory`에 재생 강조 콜백과 능력 연출 설정을 더했다. 그런데 Factory 공유 문서에 적지 않았다. 그 문서 §5로 보완했다(미전달).
- 반영하지 않은 선택 제안: H-1 8번의 몸 조회 연결은 H-1 안에서 쓰는 곳이 없어 H-2로 미뤄도 된다. 지금 범위를 유지했다.
- 상태: 코드·에셋 변경 없음, 테스트 실행 없음.

### 8.3 Q2~Q4 결정 반영 (2026-10-16)

- 사용자 답: "Q2 A, Q3 B, Q4 A로 반영해줘, Q4에 경우는 일단 이 판정 작업 끝나고 진행할거야".
- 설계 반영:
  - §6을 "사용자 결정"으로 바꾸고 Q2·Q3·Q4 결정, 결정 표(내용·적용 Phase·다시 볼 때)를 적었다.
  - H3 `EndAnimation`, H5 미교차 정책, H6 직선 행, H6 `ReleaseAll`, H7 Ballista, §5 위험 두 행을 결정에 맞췄다.
- 계획 반영:
  - 머리말, §0 결정 대기 문장, H-1 목표·기하 시험 메모를 고쳤다.
  - H-2·H-3의 진행 조건·Task·인수·완료 기준을 결정에 맞췄다. Q3 B의 점 투영 식은 H-2에서 `CombatContactGeometry`에 더하고, H-3에서 `CombatBallistaAnimation`의 옛 식을 그 식으로 바꾼다.
  - §5.1에 Q4 B 후속 자리를 만들었다. §6 시나리오 행 이름도 바꿨다.
- 정하지 않은 것:
  - Q3 B에 진단 경고를 붙일지. B 선택지 문구에 없어 정하지 않았다.
  - Q4 B를 언제 시작할지(H-3 뒤 / H-4·H-5까지 뒤). 그때 사용자에게 확인한다.
- 상태: 코드·에셋 변경 없음, 테스트 실행 없음.

### 8.4 Q3 진단 경고 추가 (2026-10-16)

- 사용자 지시: "경고 추가해주고". §8.3의 "정하지 않은 것" 중 경고 여부를 이것으로 정했다.
- 설계 §6 Q3에 경고 계약을 적었다. H5 정책 문장과 결정 표에도 반영했다.
  - 내는 곳: 시각 결정 경계(`CombatAttackPlayback`)
  - 내용: 이유·공격자·대상·키·콜라이더 안내
  - 빈도: 결과마다 이유·키당 한 번
  - 제외: 점 몸 미교차(H4 경고와 중복)
  - 진행에는 영향 없음
- 계획 H-2 1·5번, H-3 1·4번에 구현·시험 항목을 더했다.
- 빈도와 제외 규칙은 기존 H4 경고(설정 구간 내 키마다 한 번)와 같은 결의 기술 선택이다. 콘솔이 매 공격마다 차지 않게 정했다.
- 상태: 코드·에셋 변경 없음, 테스트 실행 없음.

### 8.5 H-1 실행 (2026-10-16, 사용자 지시 "H-3 까지 진행해, Phase 단위 작업 끝나면 하위 에이전트 돌려서 검증 진행하고")

- **착수 조건:** §0 Unity 기준선은 받지 못한 채 사용자 지시로 진행했다. 비교 기준은 편집 전 트리의 관리 실행 기준선 `hit0`이다(이전 `boss3d`와 같음).
- **새 파일(+ .meta, 새 GUID):**
  - `C/CombatHitGate.cs`: 완료 정책 `CombatHitCompletion`(AnimationEnd·FirstRelease·Fired)과 Gate 본체.
    - 신호는 공격 사건을 함께 받아 활성 공격과 같을 때만 반영한다. 설계 H3의 "공격 동일성 확인"을 API로 굳힌 것이다.
    - 시작 실패 정리는 `ClearIf(attack)`이다.
  - `C/CombatActionBoundary.cs`
  - `C/Display/CombatBody.cs`: `CombatBody`와 조회 대리자 `CombatBodyLookup`.
  - `C/Display/CombatContactGeometry.cs`
- **이름 변경:** `CombatBallistaImpactMap` → `C/CombatHitFollowUps.cs`. GUID `f42fb316…`를 유지했고 git 인덱스는 건드리지 않았다(옛 파일은 D로 보임).
  - 일반 공격까지 대응한다. 관통 접촉 밖 후속은 `ReleasedByHit=false`다.
  - 대응을 다음 공격뿐 아니라 모든 새 행동(`CombatActionBoundary`)에서 끊는다. Gate 규칙 1 때문에 동작 차이는 없다(검토 L1).
- **수정:**
  - `CombatAttackPlayback`
    - `_ballista`·`_ballistaEvent`·`_impactMap`을 지우고 Gate·대응을 소유한다.
    - `CanBegin` = Gate, `BlocksNextEvent(allowAttackOverlap)` = Gate의 `AllowsOverlap`, `Tick` 반환 = 프레임 시작 때의 `AllowsOverlap`.
    - `BeginAnimation`: Gate 준비 → 애니메이션 시작 순서. `EndAnimation`은 정상 완료 콜백에서만 부른다. 공격 사건이 없는 포효는 Gate를 거치지 않는다.
    - 박치기·직선·곡사는 `AnimationEnd`, Ballista는 `Fired`.
  - `CombatBallistaAnimation`: 도달 집합 대신 히트 대리자 `Action<EntityId>`를 둔다. 시각 순 커서로 대상마다 한 번 알린다. 셀 투영 식은 유지한다.
  - `CombatSceneEventPresenter`: 강조 끄기를 `CombatActionBoundary`로 바꿨다. `RequireIdle(allowAttackOverlap)`로 이름을 바꿨다.
  - `OrderedCombatPresentation.IsLastMovementStep`: `CombatActionBoundary`를 쓴다. 이제 소환도 경로 끝이다.
  - `ICombatEventPlayback.BeginEvent` 문서: `completed`는 사건 완료다.
  - `RailwayTravelPath.TryGetContactTime`: 면 접촉 식 `CombatContactGeometry.PlaneContactDistance`를 쓴다. 점 몸, 방향 y = 0이라 결과가 같다.
    - 생성자에 "높이가 변하는 경로 거절"을 더했다(XZ 계산 전제). 기존 사용처 `FieldRailwayPresenter`는 평면 경로라 영향이 없다(검토 L2 확인).
  - `FieldUnitPresenter`:
    - `TryGetBodyShape(key)`: 프리팹 루트 BoxCollider를 루트 크기 반영으로 읽는다. Configure·Release에서 캐시를 비운다. 콜라이더가 없으면 설정 구간마다 키당 1회 경고한다.
    - `BodyOf(CharacterCombatFacts|StructureCombatFacts, mapper)`: 표시 배치·현재 배율로 필드 로컬 XZ 외접 사각형을 만든다. unitsRoot는 균일 크기를 전제한다.
- **계획과 다르게 한 것(순서만):** §1 8번의 "Presenter 표시 사실 → 몸 조회 대리자 연결"은 H-1에서 쓰는 곳이 없어 첫 사용처인 H-2에서 연결한다(검토 M1). H-1~H-3 연속 진행 지시 안의 순서 조정이며 최종 구조는 설계 H4와 같다. 미사용 필드 경고를 피하려는 목적도 있다.
- **시험(새 파일 + .meta):**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatHitGateTests` | 정책별 완료, 규칙 순서, 공격자 우선, 접촉 밖 관통, 교체·재공격, Clear·ClearIf, 콜백 재진입, 같은 Tick, 인자 | 14개(15케이스) 통과 |
  | `CombatHitFollowUpsTests` | 그룹·가해자, 관통 접촉/접촉 밖, 행동 경계, 장애물, 그룹 0 | 5개 통과 |
  | `CombatContactGeometryTests` | 축·직사각형·시작 겹침·미교차·경계·대각·2×2 미교차·면 접촉·잘못된 값 | 8개 통과 |
  | `CombatActionBoundaryTests` | 분류, 순차 재생 이동 경로 끝(걸음 → 소환 → 걸음) | 3개 통과 |
  | `RailwayContactScheduleTests`(수정) | 경로 높이 거절 시험 추가 | 10개(16케이스) 통과 |
  | `FieldUnitBodyTests` | 적 회전·R90·구조물·2×2 중심·필드 변환 불변·콜라이더 없음 경고·크기 0과 재설정 | Unity 전용(관리 실행 native 7) |
  | `CombatBallistaAttackTests`(수정) | 접촉 밖 후속 대기, 소환 대기, 발사 시점 0, 완료 콜백 안 취소. 직접 생성자 인자 하나 추가 | Unity 전용(native) |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 기존 39묶음은 `h1a`와 `hit0` 요약이 같다. 공격·Ballista·레일 통합 묶음은 관리 실행에서 native라 Unity 결과가 필요하다.
- **검토(하위 에이전트 1개, SSOT·SOLID·FW·독립성·정확성):**
  - 반영: M2(Presenter 수준 발사 시점 0·콜백 안 취소, 소환 경로 끝 시험), L1(행동 경계를 쓰는 곳은 "네 곳" — 설계 H9a·§6·주석), L2(높이 거절 시험).
  - 순서 조정: M1(조회 대리자는 H-2).
  - 기록만: L3(`CancelUnavailable` 뒤 Gate가 남아도 `BlocksNextEvent`·다음 Tick 오류가 먼저 막는다, 옛 동작과 같음), L5(외형 표에 없는 키는 `KeyNotFoundException` — 표시 중인 대상은 외형이 있으므로 조회 실패 경로는 H-2 대리자에서 사실 존재로 판단), L6(공격자 판정은 캐릭터만, 옛 동작과 같음).
  - 사용자 보고: L4(`Configure`를 다른 배율·외형 표로 다시 부르면 기존 풀 인스턴스와 새 몸 계산이 어긋날 수 있다. 원래 있던 풀 재설정 문제이며 설계 H4대로 이번에 고치지 않는다).
- **상태:** 코드 완료, Unity 확인 대기(위 Unity 전용 묶음 + §0 기준선 묶음).

### 8.6 H-2 실행 (2026-10-16)

- **결정 적용:** Q1(포효 제외), Q2 A(넉백은 공격 전체 종료 뒤), Q3 B(직선 미교차 → 주 셀 투영 + 진단 경고, 주 셀 없음 → 오류), Q4 A(다중 대상은 사건 순서대로).
- **애니메이션(히트 대리자는 생성자 끝의 선택 인자, 없으면 알리지 않음 — 포효와 기존 직접 생성 시험이 그대로 컴파일됨):**
  - `CombatMeleeAttackAnimation`: 타격 순간(55%)에 효과음 다음으로 `hit`. 효과음 콜백이 취소했으면 히트를 알리지 않는다.
  - `CombatLinearAttackAnimation`:
    - 발사 순간 `contactProgress(from, to)`를 한 번 불러 히트 진행률을 정한다(유한성 검사, 0~1로 자름).
    - 진행률 알림이 있는 `FireLinear` 버전을 쓴다. 회전 기준 위쪽은 기존과 같은 월드 `Vector3.up`이라 표시가 같다.
    - 진행률이나 도착 중 먼저 오는 쪽에서 히트를 한 번 알린다.
  - `CombatMortarAnimation`: 착탄 완료 콜백에서 `hit`.
- **`CombatAttackPlayback`:**
  - 박치기·직선·곡사의 완료 정책을 `FirstRelease`로 바꿨다. 히트는 `HitAll` → `_gate.ReleaseAll(e)`(세대 검사).
  - 생성자에 몸 조회 `CombatBodyLookup`을 더했다(H-1 8번 연결을 여기서 함, §8.5).
  - **접촉 시각 결정 경계 `ContactProgress`:**
    - 몸이 있으면 점 투사체와 몸의 선분 접촉 진행률을 쓴다.
    - 닿지 않거나 몸 조회에 실패하면 Core 접촉 칸 투영(`CombatContactGeometry.ProjectOnSegment`)을 쓴다.
    - 접촉 칸 사실이 없으면 `InvalidOperationException`.
  - **진단 경고 `WarnContact`:**
    - 이유는 "몸·궤적 미교차"·"몸 조회 실패" 둘이다. 결과마다 (이유, 키 또는 ID)당 1회이며 `BeginResult`·`Cancel`에서 기록을 비운다.
    - 점 몸의 미교차는 경고하지 않는다.
  - **직선 `LinearContactProgress`:** 주 대상 ID가 없거나 몸 조회에 실패하면 1(종점, 설계 H6 기존 범위)이다. 그 밖에는 `ContactProgress`(주 셀)를 쓴다.
  - 포효(`TryBeginFacing`)는 히트·접촉 없이 Gate 밖 전체 완료를 그대로 쓴다.
- **`CombatSceneEventPresenter.TryGetShownBody`:** 표시 중인 캐릭터(필드·위치·생존)·구조물(생존)을 찾아 `FieldUnitPresenter.BodyOf`로 몸과 키를 만든다. 사실 목록을 넘기거나 Buffer를 읽지 않는다.
- **`CombatBodyLookup`:** `out EntityKey key`를 더했다(경고용).
- **기존 시험 영향:** 기존 공격 시험(`CombatStandardAttackTests`·`CombatEnemyPresentationTests`·`CombatMortarPresentationTests`)의 피해 사건은 Core 근원이 없거나 그룹이 달라 후속 대응이 없다. 그래서 `FirstRelease`에서도 첫 유효 해제가 없어 사건 완료가 전체 종료 때이고, 단언이 바뀌지 않는다(코드 대조로 확인). 이 묶음들은 Unity 전용이다.
- **새 시험:**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatCodeMotionHitTests`(새, Unity 전용) | 박치기 타격 때 후속 시작·복귀 중 결과 대기, 넉백 걸음은 전체 종료 뒤(Q2 A), 다중 대상 순차(Q4 A), 직선 몸 접촉이 도착보다 먼저, 직선 미교차 → 주 셀 투영 + 경고 1회(Q3 B), 히트 뒤 취소 정리 | native |
  | `CombatMortarPresentationTests`(수정, Unity 전용) | 착탄 때 직접 후속 시작, 긴 무기 복귀 동안 결과 대기 | native |
  | `CombatContactGeometryTests`(수정) | 투영 식 | 9개 통과 |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 기존 39묶음은 `h2a`와 `hit0` 요약이 같다(차이는 새 Unity 전용 시험 수: Ballista 16 → 20, Mortar 13 → 14). 순수 묶음(Gate·대응·기하·경계·레일 일정·적 간격·능력 동작)은 모두 통과했다.
- **검토(하위 에이전트 1개):** 높음 결함 없음. 흐름(히트 → 같은 Tick 후속 → 다음 프레임 반응 → 전체 종료 → 넉백)과 기존 시험의 단언 불변, 새 시험 기대값을 코드·계산으로 확인했다.
  - 반영 M1(빠진 인수 시험) — `CombatCodeMotionHitTests`에 9개를 더했다(이 묶음 모두 Unity 전용, 관리 실행 native 14).
    - 같은 결과의 앞선 이동 뒤 발사
    - 구조물 주 대상 몸
    - 점 몸 미교차(접촉 경고 없음, 콜라이더 경고만)
    - 경고: 결과·키당 1회, 다음 결과에서 다시 1회
    - 정상 교차에는 경고 없음(`NoUnexpectedReceived`)
    - §2.1: 히트와 종료가 같은 Tick, 히트 완료 콜백 예외, 콜백 안 취소, 직선 발사 시점 0
  - 반영 L1 — 공격 표시 사용 불가 중에는 `CanBeginPresentationEvent`가 false다. 다음 Tick의 원래 오류로 실패한다.
  - 반영 L2 — 세 애니메이션의 히트 인자를 필수로 바꿨다(null = 히트 없음, 포효). 빠뜨리면 컴파일 오류다. 직접 생성 시험 2곳도 고쳤다.
  - 기록만 L3 — `TryGetShownBody`의 "필드·위치·생존" 조건이 `RenderFacts`의 표시 조건과 같은 사실을 따로 본다. 표시 규칙을 바꾸면 함께 본다.
  - 기록만 L4 — H-3 주의: 셀 투영이 월드 3D에서 필드 로컬 XZ로 바뀌므로 옛 시각과 정확히 같지 않을 수 있다. 시험은 허용 오차로 본다.
  - 기록만 L5 — 직선의 "주 셀 없음 → 오류"는 Core가 주 셀 없는 주 대상을 거절해(`AttackResult`) 정상 경로로는 닿지 않는 방어 분기다. 시험을 만들지 않았다.
  - 기록만 L6 — §2 4번 "바꿀 시험 묶음"은 실제로 바꿀 단언이 없었다(위 "기존 시험 영향"). `CombatHitCompletion.AnimationEnd`는 운영 코드에서 더는 쓰지 않고 Gate 시험에만 남는다(정책 표의 H-1 단계 기록).
  - 참고: 규칙 3 사건(영혼·효과·아티팩트 등)이 공격자 복귀 중에 시작될 수 있다. 설계 H3대로이며 Unity 재생 확인 목록에 넣는다.
- **상태:** 코드 완료, Unity 확인 대기.

### 8.7 H-3 실행 (2026-10-16)

- **Ballista:**
  - `CombatBallistaAnimation`: 셀 투영 식을 지웠다. 생성자에 필수 대리자 `contactProgress(encounter, from, to)`를 더했다. 발사 순간 대상마다 한 번 부르고, 유한성을 검사하고, 0~1로 자른 뒤 시각 순 커서에 넣는다.
  - `CombatAttackPlayback.BallistaContactProgress`: 몸 조회(`CombatBodyLookup`) 결과를 H-2의 접촉 시각 결정 경계 `ContactProgress`에 넘긴다.
    - 몸이 닿으면 처음 닿는 진행률을 쓴다.
    - 미교차·몸 조회 실패는 그 대상의 encounter 칸 투영을 쓴다(Q3 B). 경고는 H-2와 같고 점 몸 미교차는 제외한다.
  - 기존 Ballista 시험 유닛은 콜라이더가 없다(점 몸). 화살 행 위의 표시 중심이라 선분 접촉 시각이 옛 칸 투영과 허용 오차(`CombatContactGeometry` 0.0001) 안에서 같다.
  - 투영은 필드 로컬 XZ로 바뀌었다(검토 L4). 옛 월드 3D 투영과는 총구 높이가 다르거나 필드가 비균일 크기일 때만 다르다. 기존 Ballista 시험은 평면·같은 높이다.
- **기차:**
  - `RailwayTravelPath.TryGetContactTime(CombatBody)`를 더했다. 점 위치 버전은 이 버전으로 위임한다.
  - `RailwayContactSchedule.Target`가 `CombatBody Body`를 가진다. 점 위치 생성자(옛 중심 = 점 몸)는 남겼다.
  - `CombatRailwayContactPlayback`: 생성자에 필수 `Func<CharacterCombatFacts, FieldGridMapper, CombatBody> bodyOf`를 더했다. `Prepare`가 사망 `Expected`로 몸을 만든다. 경로 미도달·Expected 누락 오류는 그대로다.
  - `CombatSceneEventPlayback`: 생성자에 필수 `characterBody`를 더해 기차 재생에 전달한다.
  - `CombatSceneDisplayFactory`: `units.BodyOf`(캐릭터 오버로드)를 넘긴다. 공유 문서 `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md` §6을 작성했다(미전달).
- **viewDocs:** `railway-view-guide.md` 5행 규칙을 "FrontContact 면이 몸에 처음 닿은 프레임"으로 고쳤다.
- **시험:**

  | 묶음 | 변경 | 관리 실행 |
  |---|---|---|
  | `RailwayContactScheduleTests` | 몸 앞면 접촉(중심보다 먼저, 점 몸은 옛 값), 시작 접촉 0, 반대 방향은 반대쪽 면 | 12개(18케이스) 통과 |
  | `CombatRailwayContactPlaybackTests` | 생성자에 점 몸 변환(옛 중심 기준 유지, 묶음 수명 시험) | Unity 전용 |
  | `CombatRailwayContactSceneTests` | 접촉 시각 도우미를 운영과 같은 몸으로 바꿈. 몸 접촉이 표시 중심보다 먼저인지(콜라이더가 있을 때) 새 시험. 실제 외형 프리팹에는 BoxCollider가 있어 기존 시험의 시각이 이 도우미로 함께 바뀐다 | Unity 전용 |
  | `CombatBallistaAttackTests` | 몸 앞면 해제(칸 중심보다 0.5초 먼저), 2×2 점 몸 미교차(옛 시각·접촉 경고 없음), 작은 콜라이더 미교차(옛 시각·경고 1회), 표시 사실 없는 대상(조회 실패 경고). 직접 생성자 인자 추가 | Unity 전용 |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 기존 39묶음은 `h3b`와 `hit0` 요약이 같다(차이는 새 Unity 전용 시험 수: 레일 장면 9 → 10, Ballista 16 → 24, Mortar 13 → 14). 순수 묶음 7개(Gate·대응·기하·경계·레일 일정·적 간격·능력 동작)는 모두 통과했다. 새 Unity 전용 묶음(`FieldUnitBodyTests` 7, `CombatCodeMotionHitTests` 14, `CombatMortarAnimationTests` 14)은 관리 실행에서 native다.
- **검토(하위 에이전트 1개):** 운영 코드 결함 없음.
  - 확인 1: 기존 Ballista 시험의 접촉 시각 차이는 0.0001초 수준이다(점 몸·같은 높이 경로).
  - 확인 2: 기차 좌표계가 같다. mapper의 fieldRoot = `FieldView.cellsRoot` = `FieldRailwayPresenter._cellsRoot`.
  - 확인 3: 레일 장면 시험은 시작 겹침이 없다(몸 앞면까지 2.95칸). 새 시험의 기대값과 경고 순서도 맞다.
  - 반영(높음): `CombatSceneEventPresenterTests.OrderedRailway_ActualSceneRouterStartsDeathAtFrontContact_AndWaitsForBoth`가 옛 중심 기준으로 접촉 시각을 구해 깨질 상태였다. 실제 외형 `StageUnit_Akimbo` BoxCollider (1,1,1) × 필드 배율 0.5 → 몸 앞면이 중심보다 약 0.01초 먼저 닿는데, 시험 여유는 0.001초였다. 운영과 같은 몸(`FieldUnits.BodyOf`)으로 바꿨다. 이 묶음은 §3 파일 목록에 없던 영향 묶음이다.
  - 반영(낮음): `BallistaContactProgress` 주석을 "같은 칸 중심을 필드 로컬 XZ 경로에 투영"으로 고쳤다. 새 식이 투사체의 선형 XZ 진행과 정확히 맞는다.
  - 갈음(낮음, §3 4번 중 시험 없는 항목):
    - Ballista의 "같은 결과의 앞선 이동 뒤 발사"는 같은 몸 조회 경로(`TryGetShownBody`)를 쓰는 H-2 `CombatCodeMotionHitTests.Linear_TargetMovedEarlierInSameResult_UsesShownMovedBody`로 갈음한다.
    - 총구 오프셋 미교차는 작은 콜라이더 미교차 시험과 같은 결정 경계 경로다.
    - 기차의 2×2 사망 몸은 `FieldUnitBodyTests.MultiCellFootprint`와 레일 장면 시험의 다칸 경로로 부분 검증된다.
  - 기록만:
    - 점 위치 API(`RailwayTravelPath.ContactTime/TryGetContactTime(Vector3)`, `Target(…, Vector3)`)는 운영에서 더 쓰지 않고 시험·호환용으로 남는다.
    - BoxCollider의 XZ 중심이 진행 방향 뒤쪽에 있으면 중심보다 늦게 닿아 "미도달" 오류가 새로 날 수 있다. 지금 유닛 프리팹은 모두 XZ 중심 0이다.
- **상태:** 코드 완료, Unity 확인 대기.

### 8.8 H-1~H-3 Unity 확인 목록 (사용자)

- **Unity Test Runner(EditMode):**
  - 새 묶음: `FieldUnitBodyTests`, `CombatCodeMotionHitTests`
  - 수정 묶음: `CombatBallistaAttackTests`, `CombatMortarPresentationTests`, `CombatRailwayContactSceneTests`, `CombatSceneEventPresenterTests`
  - 영향 묶음(단언 불변 예상): `CombatStandardAttackTests`, `CombatEnemyPresentationTests`, `CombatUnitReactionTests`, `CombatUnitDeathTests`, `CombatRailwayContactPlaybackTests`, `CombatMortarAnimationTests`, `FieldRailwayPresenterTests`, `CombatBallistaAssetTests`
  - §0 기준선 묶음: 위와 겹친다. `CombatEnemyPresentationTests`는 SV8 전까지 실행된 적이 없어 기존 실패가 섞일 수 있다.
- **재생:**
  - 박치기·직선·곡사 피격이 타격·접촉·착탄 순간에 시작하는지.
  - 넉백이 타격 뒤 앞 반응이 끝나면 바로 밀리는지(공격자 복귀·투사체 비행·포효 작아지기와 겹침, 2026-10-17 H-2b).
  - 다중 대상은 차례로 반응하는지.
  - Ballista 화살이 몸에 닿을 때 반응하는지(큰 유닛일수록 이전보다 조금 이름).
  - 기차 사망이 몸 앞면에서 시작하는지.
  - 포효: 박치기 타격 순간 피해 → 피해 반응·넉백이 작아지기와 겹침 → 작아진 뒤 강조 끔 → 소환은 포효가 끝난 뒤(H-2b). 포효 중 근원 없는 사건(사망·효과 등)도 작아지기 동안 시작할 수 있다.
  - Ballista 관통 대상의 넉백이 그 대상 접촉 뒤 화살 비행 중에 시작하는지(H-2b, 관리·Unity 시험 없음).
  - 규칙 3 사건(영혼·효과 등)이 공격자 복귀 중에 시작해도 어색하지 않은지.
  - 콘솔의 "BoxCollider"·"미교차" 경고가 어떤 프리팹에서 나오는지.

### 8.9 H-2b 실행 — 포효 히트와 넉백 타격 순서 (2026-10-17)

- **사용자 결정:**
  - "포효 넉백도 타격 순서에 맞춰, 강조는 포효가 끝난뒤, 작아지기와 겹쳐도 됨"
  - "일반 공격 넉백도 박치기 타이밍에 맞춰야지"
  - 넉백 연결 방식은 질문에 "사건 순서로 묶기(추천)"로 답함
  - 설계 §6 Q1·Q2를 재결정으로 바꿨고, 보스 활성화 설계 D6·D6b에 변경 메모를 남겼다.
- **Core 근거 확인:**
  - 공격 처리 안의 이동은 넉백(`AttackKnockbackCalculator`)과 오브젝트 강제 이동뿐이다. 포효 반응 공격(`BossRoarCalculator` → `CombatAttackCalculator.Reaction`)도 같은 경로다.
  - 보스 턴은 포효 → 소환 → 턴 종료 순이다. `Returned`·`Relocated`는 행동 경계 뒤에만 나온다(검토 확인).
- **코드:**
  - `CombatHitFollowUps`:
    - 출처를 `CombatEvent`로 넓혔다(`TryGetSource`: 공격 사건, `Attack` 값이 있는 포효).
    - 출처~다음 행동 경계의 이동 사건을 이동 후속으로 대응한다.
    - 공격자 자신의 강제 이동은 대응하되 해제 대상에서 뺀다(검토 3).
    - 이동 분류 `IsMovement`는 Gate와 같이 쓰는 한 곳이다(검토 2). 파생 가능한 `FollowUp.IsMovement` 필드는 지웠다.
  - `CombatHitGate`: 출처는 `CombatEvent` + `attackerId`(공격 사건 줄임 오버로드 유지)다. 규칙 1에서 활성 출처의 이동 후속은 규칙 2로 판단한다.
  - `CombatAttackPlayback`:
    - 포효 수명 `BeginRoar`·`EndRoar`·`AbortRoar`를 더했다.
    - `TryBeginFacing(CombatRoarEvent…)`의 히트는 `ReleaseAll(roar)`이다.
    - `AllowsOverlap`를 공개했다. `TryBeginDirect`는 히트 인자를 받는다.
  - `CombatSceneEventPresenter`:
    - `TryBeginRoar`: 포효 Gate를 연다. 작아지기 끝에 강조를 끄고 `EndRoar`를 부른다. 시작 실패 때는 Gate만 비우고 크기 대여를 되돌린다(검토 6).
    - 능력 동작 중 차단(`CanBeginPresentationEvent`·`RequireIdle`)은 Gate가 겹침을 허용할 때만 푼다.
    - `RequireIdleFor`(`BeginStep`·`BeginMovementEnd`·`ShowReturn`): 지금 시작 가능한 이동만 겹침을 허용한다.
    - Tick 순서: 능력 동작(이 프레임에 공격이 끝났으면 다음 프레임부터) → 반응 → 걸음.
    - 겹침 판정은 프레임 시작 기준이다(검토 1: 포효가 끝나는 프레임에 반응·걸음이 시간을 잃지 않음).
- **시험:**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatHitFollowUpsTests` | 포효 출처, 이동 후속(경계까지), 피해 없이 밀린 유닛, 관통 접촉 밖 이동 | 8개 통과 |
  | `CombatHitGateTests` | 대응된 넉백은 해제 뒤, 대응 안 된 이동·공격자 이동은 대기, 공격자 강제 이동만 있으면 완료가 앞당겨지지 않음, 포효 출처(보스 = 공격자) | 17개(18케이스) 통과 |
  | `CombatCodeMotionHitTests` | 박치기 넉백이 공격자 복귀 중에 시작(옛 "전체 종료 뒤" 시험을 바꿈), 포효 전체 시나리오(타격 피해 → 커진 채 넉백 → 강조 유지·소환 대기 → 작아진 뒤 강조 끔 → 소환) | Unity 전용(native 15) |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 기존 39묶음은 `h2d` 요약이 `h3b`와 같다. 순수 묶음 7개는 모두 통과했다. Unity가 csproj를 다시 만들어 새 파일이 포함됐으므로 추가 목록은 비웠다. Editor.log의 최근 컴파일에 오류가 없다(시험 결과는 아님).
- **검토(하위 에이전트 1개):** 교착·시간 이중 적용·자세 충돌·Core 근거 위반 없음. 기존 시험 단언은 그대로 유지된다(코드 대조).
  - 반영: 1(프레임 시작 기준), 2(이동 분류 한 곳, 미사용 필드 삭제), 3(공격자 강제 이동은 해제 대상 아님), 5(옛 문구 — 코드 주석·시험 요약·설계 API·정책 문장·Q4 보충·보스 D6·계획 §2 완료·§8.8), 6(시작 실패 때 크기 복원).
  - 기록만: 2의 가해자 판정 세 곳(`TryGetSource`·`BeginRoar`·`Begin(CombatAttackEvent)`)은 대상 없는 포효도 Gate를 열어야 해 명시 인자로 남겼다.
  - 기록만: 4(Ballista 관통 넉백, 포효 중 근원 없는 사건)는 시험 없이 §8.8 재생 확인 항목에 넣었다. 1의 "포효 끝 프레임에 넉백이 걷는 중" 시험은 프레임 경계에 따라 시각 단언이 흔들려 추가하지 않았다.
- **상태:** 코드 완료, Unity 확인 대기.

### 8.10 H-4a 실행 — 걸음 접촉 해석과 원본 몸 (2026-10-17, 사용자 지시 "H-4a 진행해")

- **코드:**
  - 새 `C/CombatStepContacts.cs`: `Begin(result)`이 걸음 구간(Moved 뒤 ~ 다음 이동·새 행동 전)과 접촉 묶음(원본 사건 ~ 다음 원본 전)을 Core 순서로 대응한다. `TryGetStep`·`TryGetSegment`·`TryGetGroup`·`Clear`, 진행률 `ContactProgress`(닿지 않으면 1).
    - 원본 사건: 가시 소비, 이 걸음의 이동자가 발동한 오브젝트(`ActivatorId`), 들어간 기준 칸의 일반 진입 규칙 타일 변경(오브젝트 아님 + `TriggersArrivalRule`, Core `ResolveEntry` 선택식과 같음).
  - 새 `C/Display/CombatBodyPlacement.cs`: 월드 상자 → 필드 로컬 XZ 외접 사각형(유닛·오브젝트·가시 공용).
  - 새 `C/Display/CombatBodyShapeCache.cs`: 프리팹 루트 BoxCollider 모양 캐시(설정 구간마다 프리팹당 1회 경고, 프리팹 없음은 대상당 1회, 유닛은 루트 크기 반영 모드).
  - `FieldUnitPresenter`: 모양 캐시·배치를 위 두 도움으로 옮겼다(몸 수치 불변, 경고 기준만 키 → 프리팹, 설계 H4 문구 갱신).
  - `FieldObjectPresenter.BodyOf(source, mapper)`: 표시된 인스턴스 Transform + 프리팹 콜라이더. 숨은 원본은 칸 중심 + 오프셋 점 몸, 외형 행이 없으면 칸 중심 점 몸. 위치 식은 `Apply`와 같은 `LocalPosition` 한 곳.
  - `FieldThornPresenter.BodyOf(cell, mapper)`: 인스턴스 없이 칸 배치(`Apply`와 같은 `Placement` 한 곳) + 프리팹 콜라이더.
- **설계 사실 보정(구현 중 확인, 설계 §1.1·M1에 기록):** 약한 지반 칸은 걸음마다 `WeakGroundCalculator.RegisterEntry`가 진입 처리보다 먼저 지형 원본 변경을 기록한다. 설계 M1 문구 그대로면 이것이 접촉 묶음이 되어 "지형 시점 불변"(설계 §2)·"지형 몸은 확장점"(M2)과 어긋난다. 그래서 타일 원본을 Core 진입 처리와 같은 기준으로 좁혔다. 깃발 떨어뜨림 즉시 획득 같은 다른 도착자의 발동도 원본에서 뺐다.
- **시험:**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatStepContactsTests`(새, 순수) | 가시·오브젝트 묶음, 접촉 원본이 아닌 변경·다른 도착자 발동, 약한 지반 등록(앞 사건), 첫 원본 앞 사건, 행동·다른 이동 경계(강제 이동 걸음), 경로 도착·이동 성공 효과(마지막 묶음), 넉백(가시 사건 없음·S12 늦은 발동), 다시 준비, 진행률 | 10개 통과 |
  | `FieldObjectPresenterTests`(추가) | 표시 인스턴스 배치 + 콜라이더(회전 없음·R90), 콜라이더 없음·숨은 원본 점 몸과 경고 1회 | Unity 전용 |
  | `CombatThornPresentationTests`(추가) | 칸 배치 + 콜라이더(인스턴스 없이), 콜라이더 없음 점 몸과 경고 1회 | Unity 전용 |

- **회귀:** 기준선 `h4base`(HEAD `c9b8c4d6`)를 새로 잡았다. 같은 HEAD에서 `CombatTimelineViewSystemTests`가 실행 목록에 없는 것은 호진님 커밋 `70de0066`이 타임라인 UI와 그 시험을 지웠기 때문이다(이번 작업과 무관). MSBuild 오류 0, 새 경고 0. 관리 실행 39묶음과 추가 묶음 10개는 `h4a4` 요약이 `h4base`와 같다. Unity가 csproj를 다시 만들어 새 파일이 포함됐으므로 추가 목록은 비웠다.
- **검토(하위 에이전트 1개):** 시점 오배정 버그는 없다. 두 보정(`TriggersArrivalRule`·`ActivatorId`)은 Core 기준으로 맞고 빠진 경우가 없다.
  - 반영:
    - 1(앞 사건 = 규칙 3 — 설계 M1·코드 주석·시험 메시지)
    - 2(넉백은 가시 사건이 없고 마지막 칸 오브젝트는 S12 — 설계 §1.1과 순수 시험)
    - 4(프리팹 없음 경고 1회)
    - 5(루트 크기 반영 모드로 크기 0 경고 복원, H4 문구 "프리팹마다")
    - 6(배치 식 한 곳 — 가시 `Placement`, 오브젝트 `LocalPosition`)
    - 7(숨은 원본 대체를 설계 M2에 명시, 외형 행 없음 대체)
    - 8(진행률 인자 뜻 문서화, 이동자 몸 방향은 H-4b)
    - 9(확장 조건을 설계 §4에)
    - 10(칸 판정을 Core처럼 기준 칸으로)
    - 11(경로 도착 시험, 이 기록, 라벨 boxing 제거)
  - H-4b로 넘김: 3(걸음 도중 사건 끝의 `RenderEvents`가 이동자를 걸음 전 칸으로 되돌림, 이동자 영혼 수집은 규칙 1) — 설계 M4·M5와 §4.2 "착수 전 확인"에 적었다. 지금 코드는 걸음 중 다른 사건을 시작하지 않아 드러나지 않는다.
  - 기록만:
    - `Step.Add` 캡슐화: 중첩 클래스라 내부 공개로 둔다.
    - 가시 `Clear`가 재생 취소마다 불려 경고가 다시 나온다(설정 구간 정의상 허용).
- **상태:** 코드 완료, Unity 확인 대기(위 Unity 전용 시험 2묶음과 기존 `FieldUnitBodyTests`). 재생 동작은 바뀌지 않는다(H-4b 전까지 연결 없음).
- **커밋 때 새 파일(.cs + .meta):** `C/CombatStepContacts.cs`, `C/Display/CombatBodyPlacement.cs`, `C/Display/CombatBodyShapeCache.cs`, `Tests/EditMode/Framework/CombatStepContactsTests.cs`.

### 8.11 H-4b 실행 — 걸음 접촉 Gate와 걸음 도중 시작 (2026-10-17, 사용자 지시 "(a)로 가자, H-4b 진행해")

- **사용자 결정:** 걸음 중 이동자 위치 보존은 설계 M5 후보 (a)다. 보간 중인 유닛은 `RenderFacts`가 위치·회전을 쓰지 않는다(설계 §6에 기록).
- **코드:**
  - 새 `C/CombatStepContactGate.cs`(순수)
    - 상태: 활성 걸음, 묶음별 해제(Core 순서 누적 최대 시각), 걸음 사건 완료(첫 묶음 해제, 없으면 보간 끝), 걸음 끝(`EndStep`, 보간 + 미룬 방향 복귀)
    - 겹침 규칙 1~3(`CanBegin`)
    - 규칙 1: 새 행동·모든 이동·이동자 캐릭터 사건·이동자 포함 영혼 수집
  - `C/CombatUnitMotionPlayback.cs`
    - Gate를 소유한다.
    - 보간 시작 때(걸음 전 회전 뒤) 묶음별 접촉 진행률을 정한다.
      - 이동자 몸: 지금 표시 방향의 `BodyOf(사실, 회전)`
      - 원본 몸: 주입받은 가시·오브젝트 몸 조회. 조회가 없거나 타일·지형이면 1(보간 끝)이다.
    - Tick마다 해제한다.
    - `IsStepComplete` = 걸음 사건 완료. 계획 Task의 `IsStepReleased`는 새 멤버 대신 이 뜻 변경으로 구현했다.
    - `DeferFacingRestore`: 보간 끝 뒤 복귀하고, 복귀 끝에 `EndStep`.
    - 보간 동안 `FieldUnitPresenter.BeginStepPlacement`/`EndStepPlacement`(취소에서도 끝).
    - `IsActive`는 걸음 끝 정리 구간(보간 끝 표시 ~ Gate 끝)도 포함한다(검토 8).
  - "닿지 않으면 1" 정책은 `CombatStepContacts.ContactProgress`에 두고, 몸 조회 실패 대체 1은 `ArmContacts`에 둔다(계획 Task 2의 위치 결정).
  - `C/CombatSceneEventPresenter.cs`
    - 생성자 선택 인자 `thornBody`·`objectBody`
    - 결과 준비 때 걸음 구간 해석
    - `IsPresentationComplete`에 남은 걸음 포함
    - 시작 조건 = 공격 Gate ∧ 걸음 Gate
    - `RequireIdle(allowOverlap)`이 걸음 겹침도 허용한다.
    - `BeginMovementEnd`: 보간 중이면 복귀를 미루고 바로 완료한다.
    - Tick: 걸음 겹침이 프레임 시작에 허용됐으면 반응 중에도 보간을 먼저 진행한다(설계 M5).
  - `C/Display/FieldUnitPresenter.cs`
    - `_steppingActor`, `RenderFacts`의 배치 유지(사망 유지와 같은 분기)
    - 회전을 지정하는 `BodyOf(사실, 회전, mapper)`
  - `C/CombatStepContacts.cs`: 같은 사건 인스턴스가 결과에 두 번 있어도 첫 위치를 쓴다. 이번에 결과 준비가 해석을 처음 부르게 되어 예외 경로를 막았다(검토 8).
  - 계약 문서: `ICombatMovementPlaybackPresenter`·`ICombatEventPlayback`·`CombatMovementPlayback`.
  - Factory: `thorns.BodyOf`·`objects.BodyOf` 연결. 공유 문서 §7(민형님, 미전달).
- **시험:**

  | 묶음 | 내용 | 관리 실행 |
  |---|---|---|
  | `CombatStepContactGateTests`(새, 순수) | 묶음 없는 걸음, Core 순서 누적 해제, 뒤 묶음 대기, 규칙 1(이동자 피해·이동자 영혼·다른 이동·행동), 첫 묶음 앞 사건 = 규칙 3, 가드·취소, 결과에 없는 걸음, 넉백 걸음에서 두 Gate 동시 활성 | 8개 통과 |
  | `CombatSceneEventPresenterTests`(추가 5) | 가시 접촉 중 소비·이동자 위치 유지(a)·이동자 피해는 도착 뒤, 경로 끝 방향 복귀 미룸(옆 칸 걸음으로 실제 회전), 다른 유닛 반응 중 보간 진행(시간 손실·이중 적용 없음), 지뢰 접촉 폭발(오브젝트 몸 연결), 취소 뒤 Refresh 배치 | Unity 전용 |

- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 39묶음은 `h4b4` 요약이 `h4base`와 같다. 차이는 `CombatSceneEventPresenterTests` Unity 전용 수 23 → 28(추가 5)뿐이다. 추가 묶음 10개도 같고, 순수 묶음 `CombatStepContactsTests` 10개·`CombatStepContactGateTests` 8개가 통과했다. 새 파일은 추가 목록으로 컴파일했다.
- **검토(하위 에이전트 1개):** 높음 없음. 교착·새 예외·접촉 묶음 없는 걸음의 동작 변화·시간 이중 적용은 없다(호출 순서 추적).
  - 반영:
    - 1(경로 끝 복귀 시험이 회전 없는 걸음이라 증명 못 함 → 옆 칸 걸음, 접촉 뒤·보간 끝·복귀 끝 세 시점 단언)
    - 2(반응 분기 보간 진행 시험 추가)
    - 3(이동자 피해 대기를 `CanBeginPresentationEvent`·체력 표시로 직접 단언)
    - 4(지뢰로 오브젝트 몸 연결 시험)
    - 5(깃발·배지 HUD 확인 항목 정정)
    - 6(이 기록, CLAUDE.md, 계획 Task 문구)
    - 7(용어 "보간 끝(칸 도착)")
    - 8(`IsActive` 정리 구간 포함, 중복 사건 인스턴스)
    - 9(설계 M5 (b) 근거 정정)
    - 10(규칙 1 확장 조건을 설계 M4에)
- **상태:** 코드 완료, Unity 확인 대기. 재생 확인은 아래 목록이다.
  - 가시: 닿는 순간 사라지고, 이동자는 칸 중심까지 계속 걷고, 체력은 도착 뒤 바뀐다.
  - 지뢰: 닿는 순간 폭발한다. 지뢰 표시는 H-4c부터 닿는 순간 사라진다(§8.12).
  - 경로 끝 방향 복귀는 도착 뒤에 한다.
  - 접촉 없는 걸음은 지금과 같다.
- **커밋 때 새 파일(.cs + .meta):** `C/CombatStepContactGate.cs`, `Tests/EditMode/Framework/CombatStepContactGateTests.cs`(H-4a 새 파일 4개와 함께).

### 8.12 H-4c 실행 — 오브젝트 사건 시점 숨김 (2026-10-17, 사용자 지시 "H-4c 진행해")

- **코드:**
  - `C/Display/FieldObjectPresenter.cs` `HideActivated(sourceId)`: 표시된 그 원본 인스턴스만 끈다. 연결·풀은 그대로 둔다. 표시되지 않은 원본과 해제 뒤에는 아무것도 하지 않는다.
  - `C/CombatSceneEventPresenter.cs`: 선택 인자 `hideObject`. `CombatObjectActivationEvent` 표시 때 부른다(가시 소비와 같은 자리, 지뢰 폭발 효과 뒤).
  - 결과 끝 `Refresh`의 전체 `Render`가 최종 사실로 다시 맞춘다. 비활성이면 반환하고, 활성이면 같은 인스턴스를 다시 보인다. 원본 목록은 따로 들지 않는다.
  - Factory: `objects.HideActivated` 연결. 공유 문서 §8(민형님, 미전달).
- **시험(모두 Unity 전용):**
  - `FieldObjectPresenterTests`:
    - 그 원본만 숨김, 모르는 원본·해제 뒤는 무시
    - 활성 사실 Render는 같은 인스턴스를 다시 보임
    - 비활성 사실 Render는 반환하고, 다음 원본이 같은 인스턴스를 다시 씀
  - `CombatSceneEventPresenterTests`:
    - 표시된 깃발이 보간 중 닿는 순간 사라지고, 결과 끝 Refresh가 반환함(다음 원본이 같은 인스턴스를 씀)
    - 접촉으로 숨긴 뒤 취소하면, 활성 사실 Render가 다시 보임
- **회귀:** MSBuild 오류 0, 새 경고 0. 관리 실행 39묶음은 `h4c` 요약이 `h4base`와 같다. 차이는 `CombatSceneEventPresenterTests` Unity 전용 수 23 → 29뿐이다(H-4b 5, H-4c 1). 추가 묶음 10개와 순수 묶음 2개도 같다. 검토 반영(시험 보강·주석)은 `h4c2`로 컴파일만 다시 확인했다(Presenter Unity 전용 30, `FieldObjectPresenterTests` 6).
- **검토(하위 에이전트 1개):** 높음·중간 없음.
  - 확인된 것: 발동 사건을 만드는 Core 경로 전부에서 숨김이 최종 사실과 맞는다(자발적 진입·타일 강제 이동·넉백 도중·S12·배치·소환·깃발 즉시 획득). 숨긴 인스턴스는 Refresh·Install·Clear·Release·Render 실패 경로에서 항상 정리된다.
  - 반영:
    - 1(반환 분기를 풀 재사용으로 증명)
    - 2(취소 뒤 다시 보임을 사건 경로에서 확인. 시험 Buffer에는 Object 원본이 없어 Refresh 대신 Refresh가 부르는 Render로 확인)
    - 3(이 기록, CLAUDE.md, §8.11 옛 문구)
    - 4(확장 조건을 설계 §4에: 발동 뒤에도 활성인 Object 규칙이 생기면 숨김 기준을 원본 변경 사건으로 옮김)
    - 7(주석 용어)
  - 기록만:
    - 6(`SetActive` 재진입은 기존 가시 소비와 같은 형태)
    - 8(사건 Presenter 생성자 선택 인자 14개. 환경 표시 콜백을 좁은 포트로 묶는 일은 구조 재평가 후보)
- **상태:** 코드 완료, Unity 확인 대기. H-4(이동 접촉) 계획 범위는 H-4a~c로 끝났다.
- **재생 확인 목록(사용자, H-4a~c 통합):**
  - 깃발·배지·지뢰: 이동자가 닿는 순간 사라지고(지뢰는 폭발), 이동자는 칸 중심까지 계속 걷는다.
  - 가시: 닿는 순간 사라지고, 이동자 체력·흔들림은 칸 도착 뒤에 온다.
  - 경로 끝 방향 복귀는 도착 뒤에 한다.
  - 접촉 없는 걸음, 넉백 걸음, 적 행동 간격은 지금과 같다.
  - 걸음 밖 발동(배치·소환 도착)도 사건 시점에 사라진다. 소환 도착은 등장 동작이 끝난 뒤다.
  - 오브젝트·가시 프리팹에 BoxCollider를 붙이면 그 모양으로 더 일찍 닿는다(없으면 점 몸, 콘솔 경고 1회).
- **커밋 때:** H-4c는 새 파일이 없다. H-4a·H-4b 새 파일 6개(.cs + .meta)와 함께 커밋한다.
