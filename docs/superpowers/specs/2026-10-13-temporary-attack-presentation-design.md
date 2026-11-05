# 임시 공격 연출 4종 이식 설계

- 작성: View 작업, 2026-10-13
- 상태: 공격 4종 범위·Phase 순서 사용자 확정. 구현 상태·검증·리뷰 지점은 아래 Task 기록을 따른다.
- Task·실행 기록: [임시 공격 연출 Task 계획](../plans/2026-10-13-temporary-attack-presentation-plan.md)

2026-10-13 후속 요청: 과거 피격·사망 모션 설계는 §9다. 사용자가 R1·R2의 순차 진행을 지시했다. 기존 Phase 완료 후 보고·정지 규칙을 유지하며, 상세 구현·검증 상태는 Task 계획 §7을 따른다.

일반 총알, 근접 몸통 박치기, Ballista 관통 화살, CatusCannon 곡사 폭탄을 현재 Combat View에 이식한다. CatusCannon은 공격 이후 가시밭 프리팹이 남고 Core 상태에 따라 제거되는 표시까지 포함한다. 과거 외형·동작·조절값을 재사용하고, 사건 재생·좌표 변환·완료·취소 수명은 현재 계약에 맞춘다.

## 1 조사 기준과 현재 연결

과거 코드 원본은 로컬 `develop-4week`, 조사 시 커밋 `ae07af37e9e897e4462c102b67d6b05f7a0bb8db`다. 아래 옛 파일 경로는 이 커밋에서 조회하는 경로이며 현재 작업 트리의 경로가 아니다.

| 연출 | 과거 코드와 에셋 |
|---|---|
| 일반 총알 | `Assets/Scripts/ProjectL/Unity/View/Animation/BulletPresenter.cs`의 `TryShootUnit`, `Bullet.cs`, `BulletPool.cs`, `Assets/Prefabs/Bullet.prefab`, `Assets/DataBase/SO/BulletSO/` |
| 근접 몸통 박치기 | 같은 Animation 폴더의 `UnitProceduralMotion.cs`의 `BodyBash`, `Assets/DataBase/SO/UnitMotionSO/` |
| Ballista | `BallistaWeapon.cs`, `BulletPresenter.TryShootAttack`, `Assets/Prefabs/BallistaBolt.prefab`, `Assets/Prefabs/Stage/Units/EnemyUnit_Ballista.prefab`, `Assets/DataBase/SO/BallistaSO/` |
| CatusCannon | `MortarWeapon.cs`, `BulletPresenter.TryShootMortar`, `Assets/Prefabs/CactusBomb.prefab`, `Assets/Prefabs/Stage/Units/StageUnit_CatusCannon.prefab`, `Assets/DataBase/SO/CactusCannonSO/` |
| 잔류 가시밭 | `Assets/Scripts/ProjectL/Unity/View/Combat/ThornFieldPresenter.cs`, `Assets/Prefabs/ThornCactus.prefab` |

현재 `UnitVisual` DB는 유닛 프리팹을 연결하지만 옛 공격 연출 컴포넌트는 제거돼 있다. `OrderedCombatPresentation`은 사건 완료를 기다리고, `CombatSceneEventPresenter`는 사건 사실을 표시한다. 공격 시각 효과는 아직 없으며 공격 소리와 후속 체력·사망 표시가 현재 사건 순서로 진행된다.

같은 파일에 [적 연출 작업](2026-10-13-enemy-presentation-design.md)의 행동·턴 대기 연결이 진행 중이다. 공격 이식은 그 작업의 보고·리뷰 뒤 안정된 코드에서 시작한다. 구현 직전에 실제 변경 상태를 다시 확인하고 같은 파일을 동시에 편집하지 않는다. 적 회전이 후속으로 연결되면 공격 표시 방향도 같은 override 경로를 사용한다.

## 2 범위와 재사용 기준

### A1 공격 4종과 가시밭

최초 승인 범위는 네 공격의 방향 전환, 준비·발사·타격·반동·복귀, 발사체와 잔류 가시밭의 표시·풀 수명이다. C1~C4는 체력·사망 사실을 기존 사건 순서로 반영했고 옛 피격 흔들림·사망 넘어짐은 포함하지 않았다. 후속 피격·사망 모션 요청은 §9의 추가 설계로 다룬다. 이동 점프와 Animator/AnimationNotify fallback은 이번 추가 제안에서도 제외한다.

`CatusCannon`은 현재 프리팹 철자이며 Core 키·DB 이름은 `Unit_CactusCannon`이다. 기존 식별자와 프리팹 GUID를 보존한다.

### A2 재사용할 것과 바꿀 것

- 발사체·가시밭 모델, 전용 무기 자식과 피벗, 준비·반동·곡사·회전·활시위 동작 및 원본 SO 값을 재사용한다.
- 새 코드와 SO는 현재 네임스페이스·폴더·코딩 규칙으로 옮긴다. C#에는 `.meta`를 함께 다루고 옮기는 타입은 GUID 유지와 필요한 `MovedFrom`을 적용한다.
- 옛 `CombatEventRelay`, `CombatPresentationGate`, `StageCombatRuntime`, `StageAudio`, 옛 Core 조회와 옛 Pool 네임스페이스는 복원하지 않는다.
- 옛 `UnitProceduralMotion.Cancel`은 완료 콜백을 호출한다. 그대로 가져오지 않고 현재의 **취소는 완료 통지가 아님** 계약으로 바꾼다.
- 현재 유닛 프리팹 전체를 옛 버전으로 덮어쓰지 않는다. 필요한 무기 참조·표시용 바인딩만 현재 모델·스케일·HUD에 연결한다. 원본 PACK, FieldCamera, Architecture는 수정하지 않는다.

## 3 표시 동작과 사건 순서

### A3 공통 공격 시작과 완료

성공한 `CombatAttackEvent`의 공격자 ID를 이미 표시 중인 캐릭터 사실·View와 연결한다. 적 행동 대기는 기존 Pacing에서 먼저 처리하고, 실제 사건 표시 시작에서 공격 연출을 시작한다. 중간 연출에 최신 Character Buffer를 읽어 최종 위치·체력을 먼저 그리지 않는다.

표시 종류는 View 설정에서 정한다. 옛 `AttackRange >= 2` 판정을 복사하지 않는다. 유닛별 표현 설정은 `UnitVisualData`에서 공격 표시 설정을 연결하고, 준비·발사 시간과 프리팹은 공유 View SO 하나를 원본으로 사용한다. 공통 설정값을 씬과 각 유닛에 복제하지 않는다. 명시적 설정이 없는 유닛은 현재 즉시 표시를 유지한다. 설정된 연출의 필수 참조 누락은 조용히 fallback하지 않고 연결 오류로 보고한다.

조준·영향 셀·차단 접촉은 `AttackResult`에서 받은 사실을 사용한다. 단일 지정은 `PrimaryCell`, 방향 지정은 사건의 방향과 제공된 셀을 사용한다. 표시 목적의 월드 좌표·직선별 셀 그룹화는 허용하지만 사거리·피격 대상·벽 통과·가시 생성 칸은 계산하지 않는다. 지원할 수 없는 결과 형식은 구현 전에 계약을 확인하고 질문한다.

공격 표시 상태는 전용 `CombatAttackPresenter`가 소유한다. `CombatSceneEventPresenter`는 사건을 전달하고, `OrderedCombatPresentation`은 순서와 완료만 관리한다. 새 결과 재생 Gate나 Buffer 완료 번호를 만들지 않는다.

### A4 일반 총알

공격 방향을 향하고 발사 반동을 재생한다. 발사 시점에 총알을 대여해 표시 출발점에서 목표 지점까지 직선으로 이동한다. 총알 도착과 반동 복귀가 모두 끝난 뒤 공격 사건을 완료하고 후속 사건을 진행한다. 목표가 비어 있거나 피격 대상이 사망하더라도 사건에 기록된 지점을 사용한다.

방향 공격·다중 영향 공격은 제공된 방향과 셀로 표시 종점을 정한다. 총알 개수나 모델은 View 표현 설정이며 피해 횟수와 연결하지 않는다. 기존 Akimbo·Sniper·ShotGun 등의 임시 표현은 연결 에셋 조사에서 유닛별로 명시한다. 새로운 탄종별 정식 VFX는 추가하지 않는다.

### A5 근접 몸통 박치기

기본 표시 위치에서 목표 방향으로 준비·기울임·짧은 전진을 재생하고 제자리로 복귀한다. 표시 전진은 Core 이동·칸 변경이 아니다. 과거 타격 지점을 사용하되 후속 피해 사건이 타격 이전에 표시되지 않도록 기다린다. 최초 이식은 박치기 복귀 완료 후 후속 사건을 순서대로 표시하며, 타격 순간과 피격 흔들림의 동시 재생은 별도 확장이다.

공격자 모델만 움직이고 HUD·체력 숫자는 기존 표시 방향을 유지한다. 사망·풀 반환·새 ID 연결 때 공격 오프셋과 임시 방향을 비운다.

### A6 CatusCannon 곡사와 가시밭

1. 공격자가 목표의 실제 월드 방향을 바라본다.
2. 무기 Bucket을 준비 각도로 기울인다.
3. 발사 시점의 Bucket 월드 피벗에서 `CactusBomb`을 발사한다.
4. 목표 셀까지 곡사 궤적과 포탄 회전을 재생한다. 포탄은 착탄 때 풀에 반환한다.
5. 착탄과 공격 자세·무기 복귀가 모두 끝나면 공격 사건을 완료한다.
6. 후속 피해·특성·사망 사건을 기존 순서로 표시한다.
7. 기존 `FinalizeResult`의 `Refresh`에서 `state.CommandState.ThornFields`의 실제 칸에 `ThornCactus`를 표시한다.

**날아간 폭탄을 그대로 남기는 방식이 아니다.** 발사체와 잔류 가시밭은 별도 프리팹·풀을 사용한다. 가시밭의 피해량·생성 위치·남은 턴은 Core 원본이다. View는 매 턴 감소시키거나 중심 제외 8칸을 직접 만들지 않는다. 벽·점유·보드 가장자리로 가시밭이 생기지 않는 칸에도 모델을 배치하지 않는다.

가시밭은 칸 기준으로 View를 재사용하고 사라진 칸만 반환한다. `CombatThornConsumedEvent`가 오면 해당 칸 표시를 사건 순서에서 제거하고, 수명 만료·밤 종료 등 나머지 변화는 결과 완료 `Refresh`에서 대조한다. 최초 Install에서도 현재 가시밭을 표시한다.

현재 생성 사건에는 새 가시밭 목록이 없고 `AttackResult.CurrentStage`도 새 Buffer 경로에서 null일 수 있다. 따라서 최초 이식의 새 가시밭 생성 시점은 **착탄 후 전체 결과 완료 Refresh**로 고정한다. 착탄 순간·결과 중간에 정확한 새 가시밭을 표시해야 한다면 Core의 사건 사실 공개가 필요한 별도 변경이다. 최종 Buffer를 중간에 읽거나 공격 영향 셀로 가시 생성 규칙을 흉내 내지 않는다.

### A7 Ballista 관통 화살

원본 무기 Bolt 피벗, 발사 후 장전 화살 숨김, 활시위 흔들림·턴 종료 재장전 동작을 재사용한다. 여러 방향 발사는 Core가 제공한 실제 셀·방향이 있을 때만 재현하며 3발을 고정하지 않는다.

관통 궤적은 제공되는 `PiercingShotDelivery`의 이동 셀·피격 접촉·종점·종료 사유를 우선 사용한다. 해당 결과가 없는 Ballista 경로에서는 실제 `AffectedCells`·공격 방향·`BlockedContacts`가 필요한 표현 정보를 충분히 제공하는지 B 단계 시작 전에 검증한다. 벽·감쇠·최대 피격 수를 View가 다시 계산하지 않는다. 옛 무한 직진을 현재 벽·종점 계약보다 우선하지 않는다.

관통은 각 피격 지점 도달 후 해당 피해 사건을 표시한다. 공격 사건은 발사 준비·발사 완료 뒤 후속 사건으로 넘기되, 연결된 피해·사망 사건의 표시를 `CombatAttackPresenter`의 접촉 도달 상태로 기다린다. 사건을 미리 적용하거나 재정렬하지 않는다. 출처가 다른 환경 피해·연쇄 공격을 같은 화살에 붙이지 않는다.

2026-10-13 C4 인수·사용자 승인: 운영 Core는 단일 `PiercingShotDelivery`를 게시하며 `AttackResult.AttackGroup`을 최소 계약으로 추가해 기존 직접 피해 출처 그룹과 함께 전달한다. Core 변경 상세는 [공격 그룹 인계](../handoffs/2026-10-13-ballista-core-attack-group-handoff.md), 실행 상태는 Task §6.4가 원본이다. 여러 방향을 추정하거나 3발을 고정하지 않는다. 현재 Ballista 표시 프로필은 실제 관통 기록과 양수 공격 그룹을 요구하며, 해당 사실 없는 경로는 명시적으로 거부한다.

이 단계에서는 결과 시작 시 불변 사건 목록을 전달받아 공격과 후속 사건의 대응을 검사한다. 매칭 근거는 Core의 피해 출처·공격 그룹·실제 피격 접촉이다. 근거가 모호한 결과를 공격자 ID만으로 합치지 않는다. 결과 목록 끝에서는 좁은 표시 완료 계약으로 마지막 화살 접촉·반동 완료도 기다린다. 피격 대상이 없는 결과도 종점 도달로 유한하게 끝난다. 필요한 대응 사실이 없다면 Core 요청으로 기록하고 해당 세부 동작은 합의 후 진행한다.

옛 턴 종료 회수·재장전은 수명 누수 없이 재현하되, 실제 벽 또는 종점에 닿은 발사체는 먼저 회수한다. 공격 완료 후 남는 장전 상태는 결과 재생 완료 상태와 별도로 무기 표시가 소유하고 턴 종료·사망·Clear에서 초기화한다.

## 4 설정과 좌표

| 설정 | 과거 값과 적용 |
|---|---|
| 일반 총알 | 속도 20, 높이 0.6. 원본 View SO 값을 이식한다 |
| CatusCannon | 비행 0.8초, 곡사 높이 셀 폭의 2배, 회전 초당 `(60, 30, 15)`도 |
| CatusCannon 무기 | 준비 0.15초, Bucket 각도 -20도, 복귀 0.25초 |
| CatusCannon 반동 | 거리 0.04, 각도 2도, ShootDuration 0.42초, FireTime 0.3은 동작 내 비율 |
| Ballista | 화살 속도 12, 높이 0.6. 활시위 이동 0.3, 동작 0.22초, 진동 2회, 재장전 0.18초 |
| 근접·공통 반동 | 원본 `UnitProceduralMotion` 및 공통 SO 값을 목록화해 이식한다 |

이 값은 과거 원본 복원 기본값이며 운영 밸런스가 아니다. 필수 프리팹·피벗, 유한한 수치와 유효한 시간·속도·비율을 설정 연결 시 검증한다. 과거 유닛별 override가 있는 경우 공통 기본값으로 덮어쓰지 않는다.

좌표는 현재 `FieldGridMapper`만 사용한다. 월드 출발점은 발사 순간의 무기 피벗, 목표는 사건 셀의 Mapper 변환값이다. 곡사 높이는 현재 셀 크기·필드 스케일 기준으로 변환하며 필드가 회전한 경우에도 필드 법선 방향으로 올라간다. 필요한 읽기 전용 좌표 정보를 Mapper에 최소한으로 공개하고 옛 `FieldRoot` 좌표계를 새로 만들지 않는다.

## 5 책임과 변경 파일 범위

| 담당 | 책임 |
|---|---|
| `CombatSceneDisplay` | 설정·Mapper·유닛 표시를 조립하고 공격·가시밭 Tick/Clear/Release를 전달한다 |
| `CombatSceneEventPresenter` | 기존 적 행동 대기 뒤 공격 표시 전달, 후속 사건의 접촉 대기와 최종 사실 적용 |
| `CombatAttackPresenter` 신규 | 단일 공격 동작 수명, 발사 시점, 공격별 접촉 도달, 완료·취소, 전용 무기 표시 호출 |
| 발사체 표시·Pool 신규 | 직선·곡사·관통 보간과 대여·반환, 소유한 인스턴스 최종 파괴 |
| 전용 무기 바인딩 | Bucket/Bolt/String 참조와 현재 인스턴스 자세. 별도 Update 없이 외부 Tick을 받는다 |
| `Display/FieldThornPresenter` 신규 | 전달된 가시밭 목록을 칸별 표시·대조·재사용한다. Buffer를 직접 읽지 않는다 |
| `FieldUnitPresenter`·`FieldUnitView` | 이미 빌린 View의 좁은 공격 접근·표시 자세 적용/복구, HUD 방향 유지 |
| View SO·`UnitVisualData` | 유닛별 표현 종류와 공유 설정 연결. InGame DB 규칙을 추가하지 않는다 |
| `OrderedCombatPresentation`·기존 표시 계약 | 필요한 경우 결과 시작 정보와 끝의 표시 완료 대기만 확장한다 |

운영 Core·Buffer 저장 형식·Flow/Job/Plan·GameBootstrap·입력 정책은 변경하지 않는다. 범용 연출 서비스·플러그인 구조·이벤트 버스는 도입하지 않는다. 기존 `TraitVisual` 정보 카드에 잔류 프리팹 책임을 섞지 않는다.

새 에셋은 현재 `Assets/ProjectL/Prefabs/Combat/`와 `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/` 아래로 정리한다. 기존 유닛 프리팹은 현재 위치를 유지한다. 프리팹·SO·머터리얼·FBX의 GUID 의존성은 복원 전에 확인하고 이미 있는 GUID의 에셋을 중복 복사하지 않는다. 옛 Missing Script SO 48개는 삭제하거나 덮어쓰지 않는다.

## 6 완료와 취소 수명

- 모든 재생은 기존 View Tick에서 진행한다. Coroutine·Task·별도 Update·전역 구독을 추가하지 않는다.
- 정상 완료는 필요한 접촉·반동·무기 복귀와 공격용 대여 자원 정리 후 한 번 통지한다. 전체 결과 확인은 기존 Playback과 `FinalizeResult`만 수행한다.
- Cancel/Detach/오류 정지는 먼저 콜백을 무효화하고 포탄·화살 회수, 무기·모델 자세 복원, 접촉 대기 정리를 모두 시도한다. 취소는 완료를 통지하지 않는다.
- 임시 가시밭 표시 제거와 Core 가시밭 삭제는 구분한다. 표시 Clear는 Core 상태를 수정하지 않으며 재연결은 현재 목록으로 복원한다.
- Clear는 활성 표시를 회수해 재사용할 수 있게 하고 Release는 전용 루트·모든 생성 인스턴스·풀 참조까지 해제한다. 한 정리 실패가 다른 정리를 막지 않도록 오류를 모아 보고한다.
- 풀 재사용 시 이전 발사체 참조·소유자 ID·회전·진행 시간·콜백·무기 장전 상태가 남지 않아야 한다. 같은 인스턴스의 다음 대여를 이전 콜백이 반환하지 못하도록 대여 수명을 확인한다.
- 정상 승패·포기는 기존 남은 결과 소비와 이탈 계약을 유지한다. 마지막 결과를 Cancel로 건너뛰지 않는다.

## 7 SSOT SOLID JMH FW 점검

- **SSOT:** 피해·규칙·가시 수명은 Core/Buffer, 표시 셀은 사건·가시밭 목록, 외형·시간은 View SO, 공격 재생 수명은 AttackPresenter, 결과 확인은 Playback, 적 대기는 기존 Pacing이다. 파생 표시 캐시에 전투 판정 책임을 넣지 않는다.
- **S/O:** 사건 순서와 공격 종류별 표시·발사체·잔류 표시를 분리한다. 같은 재생 코드에 유닛 이름 분기를 계속 늘리는 대신 명시적 View 프로필과 네 종류의 표시 구현을 연결한다.
- **L/I/D:** 기존 Tick·완료·취소 보장을 지키고 좁은 표시 계약을 확장한다. Core 서비스·씬 검색·공용 저장소 내부를 호출해 연결을 우회하지 않는다.
- **JMH:** 역할 분리, DB와 런타임 상태 분리, 자원 소유자·반환 시점 명시, 풀 재사용 초기화, Build 뒤 구조 불변, 과도한 추상화 금지를 점검한다. 적용 원본은 [View 설계 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)다.
- **FW-001/002/003/016:** 새 서비스·모듈·Builder·실행 루프를 추가하지 않고 기존 조립·수명에서 생성한다.
- **FW-004/011:** Framework 내부 렌더 자원·Pool을 우회하지 않는다. 이번 모델 발사체는 View 전용 CustomPool 소유 자원이다. Render Scope/Handle을 사용하게 되는 경우 그 발급 경로로 반환한다.
- **FW-014:** non-null Create/Return과 PoolContext 조건을 지킨다. CustomPool이 소유권·중복 반환을 검사한다고 가정하지 않고 소유자가 보장한다.
- **P5/P6/P7:** View 표현 설정만 확장하고 규칙 값은 Buffer에서 받는다. 공격 영향·가시 생성·벽 판정을 View DB나 표현 코드에 복제하지 않는다.

## 8 구현 전 리뷰 지점

공격 4종 범위는 사용자 확정이다. 구현 전에는 CatusCannon 가시밭의 결과 완료 Refresh 생성, 근접 복귀 후 후속 피해 표시, Ballista의 접촉별 사건 대기와 현재 Core 종점 우선, 피격·사망 모션 제외, C1~C5 순서를 함께 리뷰한다. 설계와 다른 동작이나 새로운 Core 사실이 필요하면 해당 구현에 들어가기 전에 사용자에게 묻는다.

2026-10-13 사용자 조정: Akimbo는 사거리 1을 유지하면서 `UnitVisualDataSO.attackPresentation`만 일반 총알 프로필로 사용자가 직접 변경한다. 표시 종류에 InGame Enum을 추가하지 않으며 View는 사거리를 기준으로 종류를 추정하지 않는다. C3 과거 복원 기록은 당시 상태로 보존한다.

## 9 피격·사망 모션 추가 설계 (2026-10-13)

사용자가 과거 브랜치의 피격·사망 모션도 필요하다고 요청하고, 이 설계와 R1·R2의 순차 진행을 승인했다. 원본 조사는 [에셋 조사 §6](../handoffs/2026-10-13-temporary-attack-source-inventory.md#6-피격사망-모션-후속-조사-2026-10-13)에 기록했다. 각 Phase 완료 뒤 보고·정지하고 다음 구현으로 넘어간다.

### R-A 재사용 동작과 대상

원본 `UnitProceduralMotion.Hit`·`FallBackward`의 절차적 Transform 동작을 재사용한다. 별도 피격·사망 AnimationClip을 옮기는 작업은 아니다.

| 동작 | 원본 기본값 | 새 연결 |
|---|---|---|
| 피격 | 0.18초, 셀 폭의 0.05만큼 밀림, 기울기 4도, `sin(πt)` 왕복 | 체력 감소 사건을 반영한 뒤 피격 자세를 시작하고 복귀 완료를 기다린다 |
| 사망 | 0.55초 동안 SmoothStep으로 뒤로 90도 넘어짐, 0.2초 유지 | 사망 사실을 반영하고 같은 대여 모델을 유지한다. 총 0.75초 뒤 풀 반환 후 사건 완료를 통지한다 |

아군·적 캐릭터를 대상으로 한다. 건물·장애물의 파괴 표시는 기존 동작을 유지한다. `UnitVisualData`에 공격 종류와 독립적인 optional 반응 프로필을 연결한다. 공유 `CombatUnitReactionPresentationSO`와 불변 Data에 위 표현 값만 두고, 프로필 미연결은 기존 즉시 체력·사망 표시를 유지한다. 사거리나 InGame 데이터로 반응 종류를 정하지 않는다.

### R-B 사건·접촉 시점

- 살아 있는 대상의 `HealthChanged`에서 `Final.Health.CurrentHealth < Expected.Health.CurrentHealth`인 경우 피격한다. 회복·동일 체력·`HealthNormalized`에는 피격 모션을 붙이지 않는다.
- 살아 있던 캐릭터가 `Died` 사건으로 죽으면 피격 왕복을 거치지 않고 사망 모션만 시작한다. 현재 Core는 치명 피해를 별도 `HealthChanged`와 `Died` 두 사건으로 나누지 않는다.
- 체력은 모션 시작 시 사건의 `Final` 값으로 표시한다. Core의 사망·점유·승패는 이미 확정된 사실이며, 모델을 잠시 유지하는 것은 표시 자원의 수명이다.
- Ballista는 C4의 실제 접촉 대기를 통과한 뒤 해당 반응을 시작한다. 반응을 기다리는 동안 화살 비행·공격 반동 Tick은 계속한다. 다음 사건은 원본 목록 순서와 현재 반응 완료를 모두 지킨다. 여러 대상을 동시에 반응시키거나 사건을 재정렬하는 확장은 하지 않는다.
- 일반 총알·곡사는 착탄과 공격 자세 복귀 뒤, 근접은 전체 공격 자세 복귀 뒤에 후속 반응을 시작한다. 이번 추가 작업에서는 C2·C3의 승인된 완료 시점을 유지한다. 근접 타격 순간에 피격을 겹치는 연출은 별도 확장이다.
- 가시밭·연쇄·폭발 등도 전달된 캐릭터 피해 사건으로 반응한다. 피해량·피해 대상·죽음 여부를 View가 다시 계산하지 않는다. 구조물 사건이나 결과 최종 Refresh만으로 새로운 반응을 만들어 내지 않는다.

### R-C 방향과 좌표

피격 방향은 `DamageOrigins.PhysicalCell`에서 표시 대상의 사건 위치로 향하는 방향을 Mapper로 변환하고 필드 평면에 투영한다. 물리 셀이 하나로 명확하고 서로 다른 위치일 때 사용한다. 서로 다른 물리 셀이 혼재하거나 출처가 없거나 같은 칸이면 현재 모델의 뒤쪽을 사용한다. 과거 `UnitHitPresenter`의 공격자 모델 조회를 전투 사실의 출처로 사용하지 않는다.

피격 거리는 `FieldGridMapper.WorldCellWidth × HitCellFraction`으로 구한다. 사망은 현재 표시 방향의 뒤로 넘어진다. 두 동작 모두 `WorldUp`과 현재 모델 회전을 보존해 회전·비균등 배율 필드에서도 동작한다. HUD·체력 숫자는 기본 표시 위치와 기존 방향을 유지한다.

### R-D 책임과 수명

| 담당 | 추가 책임 |
|---|---|
| 반응 표시 객체 신규 | 피격·사망 진행, 외부 Tick, 완료·취소·자세 복원. EventPresenter에 동작 수식을 넣지 않는다 |
| `FieldUnitView` | 기존 공격 자세 소유권을 공격·반응에 공용으로 쓸 수 있게 최소 일반화. 동시에 두 객체가 같은 Transform을 쓰지 않으며 ID·대여 버전으로 늦은 복원을 거부한다 |
| `FieldUnitPresenter` | 사망 모델 유지·해제. 이미 빌린 View만 유지하며 완료 후 기존 EntityViewSet/CustomPool 반환 경로를 사용한다 |
| `CombatSceneEventPresenter` | 사건 적용 전에 반응과 사망 유지 준비, 사실 반영·기존 오디오, 반응 시작과 완료 전달. 공격 Tick과 반응 Tick을 함께 진행한다 |
| 기존 Display·Ordered·Playback | 기존 Tick/Clear/Release 연결과 사건 완료 대기. 결과 확인·최종 Refresh의 주인은 유지한다 |

사망 View는 `RenderFacts`가 죽은 대상을 걸러 풀 반환하기 전에 유지권을 얻는다. 유지 중에는 사망 체력과 모델을 표시하며, 정상 모션 종료 때 유지권·자세를 정리하고 반환한 뒤 완료 콜백을 한 번 호출한다. 이미 없는 View를 사망 모션을 위해 새로 빌리지 않는다.

운영 사망 Final은 `Excluded`이고 위치가 제거될 수 있다. 이 경우에도 사실은 그대로 두고 유지권이 식별한 기존 대여 모델의 자세만 유지한다. View 컴포넌트 비활성화로 반환한 대여는 다음 OnGet에서 활성 수명을 다시 사용할 수 있게 초기화한다. R2 구현·검증 상태의 원본은 [Task 계획 §7.4](../plans/2026-10-13-temporary-attack-presentation-plan.md#74-r2-사망-모델-유지모션풀-반환-완료-2026-10-13)다.

Cancel/Detach/비활성화/풀 반환/오류에서는 먼저 콜백을 무효화하고 반응·사망 유지권·자세를 정리한다. 취소를 성공 완료로 통지하지 않는다. 죽은 모델은 반환하고 살아 있는 모델은 원래 자세로 복원한다. Clear·Release 반복과 콜백 재진입, 부분 시작 실패에서도 다음 대여를 이전 반응이 건드리지 않는다. 정상 전투 이탈은 마지막 사망 모션·반환·결과 확인을 기다린다.

### R-E 경계 점검과 Phase

- **SSOT/P5/P6/P7:** 체력·죽음·출처·순서는 기존 사건, 좌표는 Mapper, 표현 조절값은 공유 View 프로필이 원본이다. 사망 유지 목록은 자원 수명만 보관한다. Core·Buffer 계약 추가 없이 현재 사실로 연결할 수 있다.
- **SOLID/JMH:** 반응 진행은 전용 객체, 대여는 FieldUnitPresenter, 사건 순서는 기존 Ordered에 둔다. 이미 존재하는 자세 계약을 좁게 일반화하고 범용 애니메이션 서비스나 이벤트 버스를 만들지 않는다. DB에 진행 시간·죽음 상태를 저장하지 않는다.
- **FW-001/002/003/016:** 기존 조립·Tick·종료를 사용한다. 별도 서비스 등록·Update·Coroutine·Task는 추가하지 않는다.
- **FW-004/014:** Framework 내부를 수정하지 않고 기존 풀 반환을 사용한다. 유지권의 소유자가 중복 반환을 막으며 `OnBeforeReturn`에서도 자세·대여 수명을 초기화한다.
- **완료 증거:** 회전 필드, HUD, 치명 피해, 다중 출처, Ballista 접촉과 꼬리 비행, 재진입·취소·실제 OnDisable, 다음 풀 대여, 정상 이탈까지 검증한다. 실제 GameView 외형 확인은 자동 수명 테스트와 구분한다.

제안 순서는 **R1 피격·공용 자세 → 리뷰 → R2 사망 유지·반환 → 리뷰 → 기존 C5 통합 재생·인계**다. C1~C4의 구현·검증 기록은 그대로 유지한다. Core·FieldCamera·Architecture 수정이 필요한 새 문제가 발견되면 해당 구현을 멈추고 별도 협의한다.
