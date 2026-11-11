# CombatEffectFollow 표면 높이 기준 계획 (2026-11-06)

## 설계

- 현재 `CombatEffectWindow`는 `FieldUnitPresenter.TrackBodyPresence`를 받아 Follow 받침을 유닛 프리팹 Collider 중심에 놓는다. Rider 등 프리팹마다 Collider 중심 Y가 달라 같은 효과의 받침 높이도 달라진다.
- 유닛을 따라가는 Follow는 현재 유닛 View의 XZ를 따른다. Y는 `FieldGridMapper.OnSurface`로 해당 XZ의 바닥 타일·지형을 포함한 **표면 높이**에 투영한다. 유닛 루트는 평상시 표면 위치에 놓이지만 도약 시 높아지므로 루트 Y를 그대로 쓰지 않는다. 격자 밖은 Mapper의 기존 `OnSurface` 정책대로 받은 위치를 유지한다.
- 따라가지 않는 효과도 같은 `CombatEffectFollow` 받침을 사용한다. 운영/미리보기 조립에서 표면 투영 함수를 주입해 큐가 지정한 월드 위치의 XZ 아래 표면에 받침을 놓는다. 구형 내부 생성자 호출은 기존 위치를 보존한다. 하붑처럼 손잡이로 받침을 옮기는 기능은 현재 칸→다음 칸의 표면 위치를 넘긴다. 실제 효과 자식의 시작 월드 위치는 그대로 둔다.
- 효과 큐의 시작 위치(`at`)는 그대로 둔다. `ActorBody`·`SubjectBody` 피격/몸 효과, 총구·접촉·칸 효과의 의도된 자리까지 바꾸지 않는다. `CombatEffectWindow.PlayOn`이 새 받침 기준으로 자식의 지역 위치를 계산하므로 생성 순간의 실제 이펙트 월드 위치는 보존된다. 이후에는 바닥 받침의 이동량을 따른다. 바닥에서 **보이는** 효과가 필요하면 효과 큐의 자리 계약을 별도 Phase로 설계해야 한다.
- `TrackBodyPresence`는 기존 몸 중심 조회로 유지하고, 효과 전용 `TrackSurfacePresence`를 추가한다. 운영/특성 미리보기 Factory만 새 함수로 조립한다. Core·Framework·외형 에셋은 수정하지 않는다. SSOT는 `FieldGridMapper.Surface`, Follow 수명은 기존 유닛 대여 ID/버전으로 유지한다. 역할 분리는 FieldUnitPresenter(현재 유닛 위치), Mapper(표면), EffectWindow(조립), Follower(추적)다.

## Phase EF1 — Follow 기준점 전환

1. `FieldUnitPresenter`가 현재 대여 유닛의 표면 위치를 읽는 함수를 제공한다. 기존 숨김·반환·재대여 검사는 보존한다.
2. 운영·특성 미리보기의 `CombatEffectWindow` 조립을 표면 추적·표면 투영으로 바꾼다. Follow 계약의 변수명과 설명을 몸 중심에서 추적 위치로 맞춘다.
3. 하붑의 직접 받침 이동은 큐 보정이 들어간 효과 위치 대신 칸 표면 위치를 사용한다.
4. 정적 연결·차이를 확인하고 멈춰 보고한다. View 시험 코드와 전투 View MSBuild는 실행하지 않는다.

Unity 확인: 평지와 고지대에서 Follow Y가 표면에 붙음, Rider/다른 병과의 Follow 높이 차이가 사라짐, 도약 중에도 Follow Y가 도약 높이를 따르지 않음, 이펙트 자식의 기존 몸·총구·접촉 시작 위치 유지, 숨김·사망·풀 반환 시 종료. 여러 칸을 점유한 유닛은 현재 XZ 아래 칸 표면과 서는 높이(점유 칸 중 최고)가 다를 수 있으므로 별도 확인한다. **컴파일·Unity 재생은 사용자 확인.**

## EF1 구현 기록 (2026-11-06)

- `FieldUnitPresenter.TrackSurfacePresence`가 현재 대여된 View의 XZ를 `FieldGridMapper.OnSurface`로 투영한다. 유닛 ID·대여 버전·숨김 검사는 기존 몸 추적과 같다.
- 운영·특성 미리보기의 `CombatEffectWindow`는 표면 추적과 표면 투영을 주입받는다. 따라가는 받침은 유닛 아래 표면, 그 밖의 받침은 큐 자리 아래 표면에 둔다. 기존 내부 생성자는 기존 위치를 유지해 다른 호출의 컴파일 계약을 보존한다.
- 하붑 손잡이 이동은 효과 큐의 위치 보정이 더해진 `CloudPose`가 아니라 칸 표면 위치를 받침에 넘긴다. 효과 자식은 처음 받은 지역 보정을 유지한다.
- `git diff --check`와 호출 경로 검색을 확인했다. 사용자 에셋·Core·시험 코드는 변경하지 않았다. **컴파일 미확인, Unity 재생 미확인.**

## Phase EF2 — 굳건한 의지 상시 이펙트 기준점 (2026-11-06)

### 원인·설계

- EF1은 `CombatEffectFollow` 받침을 표면으로 옮겼다. 그러나 `CombatPersistentEffects`가 상시 효과 자식의 첫 `at`을 `StandingBodyOf`(유닛별 Collider 중심)에서 계산한다. `굳건한 의지`의 `Position Offset Y = -0.35`는 양 진영에 동일하지만, 서로 다른 Collider 중심 높이에 더해져 최종 이펙트 높이가 달라진다. 실제 효과 프리팹 내부의 기본 높이는 동일하다.
- `CombatHeldEffect`에 `Body`(기존 기본)·`Surface` 기준점 선택을 추가한다. 데이터의 기본값 0을 Body로 유지해 다른 상태/특성 상시 효과와 하붑을 보존한다. Surface는 이미 `CombatEffectWindow`에 주입된 유닛 표면 추적 위치를 사용하고 회전은 기존 몸 Pose를 재사용한다. `Position Offset`과 Rider Y 보정은 그 선택된 기준점에 적용한다.
- `TraitVisual_SteadfastWill.asset`만 Surface를 선택하고 몸 중심 보정이었던 Y -0.35를 0으로 돌린다. 이펙트 프리팹의 내부 Y 오프셋은 그대로다. 병과/진영을 기준으로 하드코딩하지 않으며 외형 데이터가 기준점을 명시한다(SSOT). Core·Framework·다른 View 기능은 건드리지 않는다. `CombatHeldEffect.SameAs`에도 기준점을 포함해 런타임 값 변경 시 재생성한다.

### EF2 Task

1. 상시 이펙트 입력·값에 기준점을 추가하고, 기존 기본값과 검증을 보존한다.
2. `CombatPersistentEffects`가 Surface 선택에서 유닛 표면 위치를 읽어 효과 자식의 시작 Pose로 쓴다.
3. 굳건한 의지 외형만 Surface/Y 0으로 바꾸고 정적 연결을 확인한 뒤 멈춘다. View 시험 코드를 작성하거나 전투 View MSBuild를 실행하지 않는다.

Unity 확인: 굳건한 의지를 가진 아군·적 유닛에서 효과 프리팹이 같은 표면 상대 높이에 보임, 고지대에서도 동일, 이동 중 따라감, 다른 상시 몸 효과의 기준점 유지, 하붑 구름 위치 유지. DB 에셋 변경이므로 `DataBaseAssetTests`도 사용자가 확인한다. **컴파일·Unity 재생은 사용자 확인.**

### EF2 구현 기록

- `CombatHeldEffectPoint`의 기본값 `Body`와 새 `Surface`를 추가했다. `CombatHeldEffect.SameAs`에 기준점을 포함했다. 기존 에셋은 필드 누락 시 Body(0)를 쓰므로 동작이 유지된다.
- `CombatPersistentEffects`가 Surface 선택에서 `CombatEffectWindow.SurfacePositionOf`로 현재 유닛의 표면 위치를 받아 효과 자식의 시작 Pose로 쓴다. 기존 몸 회전과 위치·Rider 보정은 선택한 기준점에 계속 적용한다.
- `TraitVisual_SteadfastWill.asset`만 `point: Surface(1)`, `positionOffset.y: 0`으로 바꿨다. 기존 Y -0.35는 Collider 중심을 바닥에 가깝게 맞춘 보정이었다. 효과 프리팹의 내부 오프셋은 수정하지 않았다.
- 코드 검색으로 `CombatShownFacts.Render`의 유닛 그리기 다음에 상시 효과 맞추기가 실행되는 것을 확인했다. 변경 파일 `git diff --check` 통과. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**

## Phase EF3 — 유지 이펙트 전체 표면 기준 통일 (2026-11-06)

### 설계·범위

- 사용자 요청에 따라 EF2의 효과별 Body/Surface 선택을 폐기한다. `CombatPersistentEffects`가 내는 특성 상시, 상태 유지, 유닛 선택 표시의 시작 자리는 모두 **현재 유닛 아래 필드 표면**이다. 이미 표면을 따르는 `CombatEffectFollow` 받침과 시작 자리의 높이 기준을 일치시킨다.
- 몸 Pose는 회전만 제공한다. `CombatEffectWindow.SurfacePositionOf`가 제공하는 현재 표면 위치를 시작 Pose의 위치로 사용하고, `CombatCuePoints.Apply`가 각 효과의 `Position Offset`·회전·Rider Y 보정을 한 번 적용한다. 표시가 없거나 숨겨져 표면을 읽지 못하면 효과를 내지 않는다.
- 기준점은 유지 이펙트 표시 정책 한 곳에 둔다(SSOT). 더는 분기할 필요가 없으므로 `CombatHeldEffectPoint` 및 직렬화 필드·값 프로퍼티를 제거한다. 효과별 보정은 외형 데이터에 남긴다. 하붑 구름은 `CombatPersistentEffects`를 지나지 않고 칸 표면을 사용하므로 영향이 없다. Core·Framework·다른 View 기능은 변경하지 않는다.
- 기존 음수 Y 보정은 **필드 표면 기준**으로 의미가 바뀐다. 굳건한 의지는 EF2에서 0으로 보정했다. 다른 효과의 고유 보정은 임의로 재설정하지 않고 Unity에서 높이를 확인한 뒤 외형 데이터에서 조정한다. 특히 처형 `-1.5`/Rider `+1`, 희생 `-0.35`, 약점 노출 `-0.625`, 선택 표시 `-0.35`를 확인한다.
- 책임·원칙 점검: 효과별 값은 View DB, 표면 조회는 기존 EffectWindow, 표시 수명은 PersistentEffects가 맡는다(SRP·SSOT). 새 효과는 데이터 등록만으로 같은 기준을 받는다(OCP). 상속 계약을 추가하지 않아 LSP 영향이 없고, 기존의 작은 표면 조회 창구만 사용한다(ISP·DIP). Framework 이펙트 Scope와 FW 등록·해제 계약은 유지하며, 다른 효과 큐와 하붑을 읽거나 수정하지 않아 기능 변경이 독립적이다.

### EF3 Task

1. `CombatPersistentEffects`의 Body/Surface 분기를 제거하고 모든 유지 이펙트의 시작 자리를 표면으로 통일한다.
2. 더는 사용하지 않는 `CombatHeldEffectPoint`와 에셋 필드를 제거한다. 효과별 보정값은 보존한다.
3. 연결·차이 정적 검사를 하고 Phase를 멈춘다. View 시험 코드를 작성하거나 전투 View MSBuild를 실행하지 않는다.

Unity 확인: 아군·적군 같은 특성의 효과가 동일한 표면 상대 높이에 놓임, 고지대·이동·숨김·사망·풀 반환, 특성 상시 3종·상태 유지 2종·선택 표시의 높이와 Rider 보정. DB 에셋이 변경됐으므로 `DataBaseAssetTests`도 사용자가 확인한다. **컴파일·Unity 재생은 사용자 확인.**

### EF3 구현 기록

- 모든 유지 이펙트의 시작 Pose를 `SurfacePositionOf`의 위치와 기존 몸 회전으로 만들도록 통일했다.
- 불필요해진 기준점 선택 enum·직렬화·값 비교를 제거하고 굳건한 의지 에셋의 `point` 필드를 제거했다. 외형별 위치·회전·Rider 보정은 유지했다.
- 관련 파일의 정적 검색과 `git diff --check`를 확인했다. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**
