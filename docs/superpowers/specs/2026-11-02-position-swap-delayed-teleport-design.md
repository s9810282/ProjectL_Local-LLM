# PositionSwap — 이펙트 선행·시간 후 동시 자리 교환 설계 (2026-11-02)

- 상태: **코드/에셋·정적 인수·Unity 컴파일·교환 미리보기 검증 완료. 기존 EditMode 5,619건 중 5,607 통과·12 실패, PositionSwap 관련 32건 통과. 실제 전투 교환 입력은 샷건 배치 직후 Unity 네이티브 렌더 크래시로 미확인.** 구현/증거/잔여 기록은 Task 계획을 따른다.
- Task/진행 상태 원본: [구현 계획](../plans/2026-11-02-position-swap-delayed-teleport-plan.md).
- 범위: PositionSwap의 View 재생. Core 계산·사건 순서·위치/비용/환경 규칙·Buffer는 유지한다.
- 다른 작업자가 수정 중인 클립/다중 발사·CombatPieceStart·FieldCamera는 변경 대상으로 삼지 않는다. 이전 Core StaleContext 수정/전달 묶음과 독립된 작업이다.

## 1. 사용자 요구와 화면 순서

1. 샷건을 다른 아군의 칸으로 이동시키는 입력을 확정한다. Core가 PositionSwap 발동 결과를 돌려준다.
2. 샷건과 대상은 교환 전 자리에 남는다. 샷건의 걷기·뛰기·미끄러지기 이동 모션을 재생하지 않고 특성 이펙트를 시작한다.
3. View에서 설정한 시간만큼 기다린다.
4. 같은 완료 지점에서 두 유닛의 위치를 동시에 바꾸고, 이 발동이 낸 이펙트를 즉시 제거한다. 그 뒤 후속 사건을 이어 간다.

“다른 유닛 위치로 이동 동작”은 입력/명령 의도로 해석한다. 화면에서 먼저 대상 칸까지 걷는 단계는 없다. 여러 칸 경로도 PositionSwap이 실제 발동한 명령이면 해당 이동자의 선행 걷기를 보여 주지 않는 방향이다. 이 해석과 중간 사건 처리 경계는 코드 Phase 전에 다시 대조한다.

기준은 **설정된 대기시간 하나**다. 자연 파티클 종료를 기다려 위치를 바꾸는 방식으로 해석하지 않는다. 시간 만료가 위치 반영과 이펙트 제거를 함께 발생시킨다.

## 2. 현재 소스와 차이

- CombatPlacementBatchPlan이 Core의 연속 Moved(자발 PositionSwap)·Moved(강제 PositionSwap)를 쌍으로 찾는다.
- CombatMovementPresentation은 승객의 Final만 보류하고, 이동자는 일반 걸음 레인으로 도착시킨다.
- 뒤의 MovementChanged에서 CombatTraitEventHandler가 큐를 내고 승객의 Relocate 행동을 바로 시작한다. 현재 Relocate는 outSeconds 동안 보간하며 완료 때 보류한 사실을 반영한다.
- CombatEffectActionGroup은 열린 행동이 끝나면 이펙트를 거둔다. 따라서 현재 구조의 시간 관계는 “이동 완료 후 이펙트 회수”다.
- Core ProjectRoute는 교환 끝점 전에 일반 이동·지형/피해 사건을 낼 수 있다. 끝점의 PositionSwap 쌍만 보류하면 앞선 걷기가 남으므로 요구를 충족하지 못한다.
- CombatEffectHandle.Recall은 일반 파티클의 방출만 멈춰 잔여 입자를 남길 수 있다. 현재 ForcedMovement 프리팹은 ringBufferMode 1이지만, 즉시 제거 계약을 특정 프리팹 구조에 의존시키지 않는다.

## 3. 책임과 단일 원본

| 사실/기능 | 소유 |
|---|---|
| 교환 가능 여부·이동 경로·비용·도착 효과·최종 위치 | 기존 Core/결과 사건 |
| 두 교환 참여자와 원본 사건 대응 | 기존 CombatPlacementBatchPlan 확장. 별도 특성 쌍 탐색기 복제 금지 |
| 현재 화면에 보이는 위치·중간 HP/상태 | CombatShownFacts |
| 보류/가져간 배치 상태 | 기존 CombatDeferredPlacements. 전투 세계 사본을 추가하지 않음 |
| 대기시간 | PositionSwap 전용 View 설정의 WaitSeconds 한 값 |
| 한 번의 발동 재생·완료/취소 | 새 CombatPositionSwapPresentation + 기존 조각 실행 수명 |
| 이펙트 생성/회수/풀 반환 | 기존 EffectWindow/Handle/Follower |
| 의존성 생성/연결 | 기존 CombatSceneEventPresenter 연결 조립 지점 |

PositionSwap 전용 설정은 TraitActivationVisual에 선택 입력으로 연결하고 CombatActivationRow가 불변 값으로 전달한다. 누락된 기존 줄은 기존 모드를 유지하며, PositionSwap/MovementChanged에서만 새 설정을 허용한다. 별도 서비스 등록/전역 이벤트를 만들지 않는다.

WaitSeconds는 유한한 0 이상 값으로 검사한다. 0은 즉시 교환이다. 초기 제안값 0.5초는 기존 에셋의 outSeconds 0.5를 참고한 값이며 코드 상수로 숨기지 않는다. 새 설정 활성화 시 기존 Relocate 보간 행동을 함께 두지 않는다.

PS1의 입력 계약은 `CombatPositionSwapVisualInput`(기본 비활성) → `CombatPositionSwapVisualData` → 기존 발동 줄 캐시다. 활성화한 줄은 ActivationStart에서 유닛 행동/추가 공격을 비우며, 그 줄의 이펙트는 지연 0·끝 UnitAction을 사용한다. 교환 대기 외의 이펙트별 종료 시계는 두지 않는다. PS2에서 PositionSwap 에셋을 0.5초로 활성화하고 행동 종료 그룹의 즉시 회수 모드와 연결했다.

이펙트 개별 endSeconds와 교환 지연을 따로 세지 않는다. 이 발동의 시간 행동에 이펙트를 묶으면 기존 UnitAction 종료 개념을 활용할 수 있다. 정상 완료·취소 모두 한 소유자가 한 번 회수한다.

## 4. 결과 사건과 표시 보류

실제 PositionSwap 결과 쌍과 대응 MovementChanged를 결과 준비 때 검증한다. 누락/중복/대상 불일치는 맥락을 포함한 구성 오류다. UI 입력에서 PositionSwap 성립을 추측하거나 Core를 다시 계산하지 않는다.

교환 행위자는 원본 Pair의 첫 참여자와 발동 SourceUnitId가 일치하는 것으로 식별한다. 선행 자발 걸음도 그 참여자 ID로 고른다. 선택적 `CombatResult.CommandKind`/`ActorId`는 필수 근거로 삼지 않는다. 기존 특성 조립 미리보기는 두 값 없이도 배치/발동 사실을 제공하므로 같은 사실 소비 계약으로 처리한다(PS3 호환 보완).

교환 명령의 이동자 선행 걸음/끝점 쌍은 모션 없이 소비하되 두 표시 위치는 교환 전 자리에 유지한다. MovementChanged 전에 양쪽 배치를 가져가 기존 ReleaseUnclaimedPlacements 안전장치가 먼저 놓지 않도록 한다.

중간 환경·피해·사망·특성 사건은 제거하거나 재정렬하지 않는다. 걸음 접촉 Gate와 이동 종료 방향 복귀도 함께 대조해야 한다. 모션만 생략하면서 사건 완료가 정상 진행되게 해야 하며, HP/상태 반영 때문에 보류 위치가 조기 적용/해제되지 않도록 표시 사실 경계에서 처리한다.

실제로 교환에 도달하지 못해 PositionSwap 쌍이 없는 결과는 일반 이동/중단 재생을 유지한다. 숨겨진 지뢰 등 때문에 교환이 성립하지 않은 결과를 View가 임의로 교환시키지 않는다.

시간 만료 시 원본 쌍의 Final 두 개를 목록에 함께 적용하고 Render는 한 번 한다. 한 유닛만 먼저 그리거나 최신 Buffer 위치로 대체하지 않는다. 이후 사건은 기존 순서로 소비한다.

방어막·버프/디버프의 HUD와 상태 유지 이펙트는 기존 상태 읽기 경로를 따른다. CombatFactEventHandler가 상태 사건을 ShownFacts에 반영하고 Render 뒤 CombatPersistentEffects가 맞추며, CombatUnitStatuses와 CombatStatusEffectSource는 같은 StatusEffectReadModel/표시 조건을 사용한다. PositionSwap이 이를 직접 생성·삭제하거나 별도의 표시 타이머를 갖지 않는다. 교환 전부터 있던 상태 유지 이펙트도 교환 발동 이펙트와 함께 회수하지 않는다.

환경에서 받은 버프/디버프는 위 상태 경로이고, 지뢰 폭발·지형 변경 등은 독립된 환경 사건/큐다. 환경 효과 전체를 하나로 묶어 PositionSwap 완료에서 실행한다고 설명하지 않는다. 순차 재생이 뒤의 상태 사건 소비를 기다리는 경우 그 상태의 표시 시점도 함께 뒤로 갈 수 있지만, 이는 상태 표시 규칙을 바꾸거나 별도 지연시키는 것과 구분한다. 어떤 사건이 대기를 받는지는 PS1의 기존 순서/Gate 대조에서 확정한다.

PS1 대조 및 PS2 연결: 기존 이동 레인이 접촉 Gate를 소유하므로 모션을 생략할 때 레인을 만들지 않고 원래 걸음 완료를 전달한다. 앞선 중간 사건은 기존 순서로 소비하고, MovementChanged의 완료를 보류하는 동안 그 뒤 방어막/도착 환경 사건은 기다린다. 일반 자발 걸음은 원인이 비어 있을 수 있어 기존 `CombatMovementKinds` 판정을 재사용한다. 이 대응 정보는 기존 `CombatPlacementBatchPlan`의 Pair를 참조하며, 순차 재생기나 Core 사건을 수정하지 않는다. 첫 선행 걸음부터 두 배치를 함께 가져가고, 위치 보존 여부는 기존 보류 항목이 소유한다. 표시 위치는 별도 사본 없이 ShownFacts를 참조한다.

## 5. 대기·이펙트·취소

대기는 기존 조각 실행기의 시간 조각/수명을 우선 재사용한다. 전용 시간 조각이 필요해도 기존 ICombatPresentationPiece를 구현하며 별도 MonoBehaviour.Update·코루틴·전역 폴링은 만들지 않는다.

발동 큐·텍스트는 기존 피드백 경로에서 한 번만 발생한다. 기존 큐 경로와 전용 조립 양쪽에서 같은 이펙트를 중복 재생하지 않는다. 두 유닛을 같은 발동 완료로 묶고 후속 사건/최종 결과 반영은 대기가 끝날 때까지 진행하지 않는다.

기존 Recall의 자연스러운 잔여 입자 정책은 유지하고, 이번 완료/취소 경로가 쓸 즉시 회수 API를 EffectHandle/Follower에 좁게 추가한다. 사용 번호를 검사해 재사용된 다른 이펙트를 제거하지 않는다. 풀 반환은 기존 Scope/Handle/알림 경로를 거친다.

취소·씬 해제·재생 세대 변경에서는 두 배치의 보류/가져감과 해당 이펙트를 정리하고 정상 완료 콜백을 부르지 않는다. 늦은 완료는 위치를 다시 적용하지 않는다. 정상 종료 시 쌍 반영·회수·완료는 각각 한 번이다.

## 6. 구조 검토

| 기준 | 적용 |
|---|---|
| Core 구조 — 최우선 | Core/Contracts/Domain/Buffer 수정 없이 결과를 소비해 View 표시 시점만 변경 |
| SSOT | 쌍 대응은 기존 배치 계획, 표시 사실은 ShownFacts, 시간은 View WaitSeconds, 회수는 기존 이펙트 소유자 |
| S/O | 교환 재생을 전용 협력 객체에 두고 공통 TraitEventHandler/MovementPresentation은 전달만 함. 일반 강제 이동/다른 특성 산식은 수정하지 않음 |
| L | 기존 일반 이동·Relocate·큐 Recall의 의미 유지. 완료 1회·취소 후 완료 없음·세대 무효화 계약 유지 |
| I/D | 필요한 사실/이펙트/조각 창구만 명시 주입. 통째 의존 컨테이너·별도 서비스 등록 없음 |
| JMH/FW | 기존 연결 조립과 실행 수명 사용. FW-004 소유 경계, FW-011 Scope/Handle 회수, FW-012 이펙트/DB 전제 준수. 공용 Queue 정렬/Framework 등록 변경 없음 |
| 변경 독립성 | 시간/교환 표현 변경은 전용 설정/구현에 모음. 공유 배치·사실·취소 경계 변경은 일반 이동/되돌리기 통합 확인 필요 |
| 캡슐화·C#/Unity·if/Tick | 내부 sealed/불변 View 값·한국어 주석·meta/GUID. 새 상태는 발동 수명으로 한정. 기존 실행기의 필요한 시간 진행만 사용 |

대표 변경: 이펙트 종류나 대기시간 조정은 View 에셋에서, 자리 교환 표현 변경은 전용 구현에서 처리한다. 클립/다중 발사 기능과 함께 수정·검증하도록 만들지 않는다. 공통 표시/배치/해제 계약의 회귀 확인까지 없어지는 것은 아니다.

## 7. 확인 제한과 범위

View Test 코드를 작성하지 않고 MSBuild도 실행하지 않는다. 기존 구현 단계는 컴파일·Unity 재생을 사용자 확인으로 남겼다. 이후 사용자 **“실행 및 검증 까지 진행해”**의 명시적 실행 지시를 받아 Unity GUI 컴파일·Play·기존 시험을 실행했다. 최신 실행 결과와 미확인 범위는 Task 계획의 PS3 Unity 실행 결과를 따른다.

현재 PositionSwap 에셋은 사용자가 편집 중이다. 구현 시 최신 diff와 값을 다시 읽어 새 설정·기존 행동 충돌 부분만 병합한다. 이전 읽기에서 본 Seconds/0초 같은 중간 편집 상태를 이 단계에서 덮어쓰지 않는다. 소스/설정·동시 작업 충돌로 계획과 다른 파일/정책이 필요하면 구현 전에 확인한다.
