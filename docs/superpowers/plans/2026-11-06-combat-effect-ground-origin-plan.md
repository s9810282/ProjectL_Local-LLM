# 전투 이펙트 바닥 기준 통일 설계·계획 (2026-11-06)

## 목적과 범위

- 전투 격자의 이펙트 시작 위치와 `Position Offset`의 기준 높이를 `CombatEffectFollow`처럼 **해당 XZ 아래 필드 표면**으로 통일한다. 타일 바탕·지형 높이를 포함한 기존 `FieldGridMapper.OnSurface`가 높이의 SSOT다.
- 효과의 XZ는 기존 자리(몸·이펙트 지점·발사 지점·접촉점·사건 칸)를, 회전은 기존 자리 회전을 유지한다. 표면으로 Y를 투영한 **다음** 설정된 위치·회전 보정과 Rider Y 보정을 적용한다. 따라서 보정값 0은 바닥 위치이고, 양수 Y는 바닥 위다. 유닛의 콜라이더 중심 Y와 이펙트 프리팹 내부 Transform Y는 기준 높이에 섞지 않는다.
- `CombatEffectWindow.Play`는 이미 계산된 최종 Pose를 받으므로 그곳에서 다시 Y를 투영하지 않는다. 바닥 투영과 보정 순서는 각 생성 경로의 자리 해석 계층에서 정한다. `CombatEffectFollow` 받침의 표면 추적·숨김·회수 정책은 유지한다. 격자 밖은 `OnSurface`의 기존 정책대로 받은 Y를 유지한다.
- **낙석 터널 경고 폭발은 사용자 지정 예외(G1c)**다. 맵 프롭의 터널 Transform 월드 위치와 원래의 세 폭발 offset을 사용한다. 돌이 착지하는 칸의 효과는 기존 `CellPose` 표면 위치를 사용한다.
- 이 범위는 전투의 Render DB `EffectType` 효과다. 유닛 애니메이션, 발사체의 비행 경로, 타일/지형/스테이지 프롭 자체는 효과 생성 위치가 아니므로 그 위치 계약을 변경하지 않는다.

## 생성 경로와 Phase

| Phase | 대상 | 변경 |
|---|---|---|
| G1 | `CombatCuePoints.At`의 행위자 몸·대상 몸·사건 칸 효과 | 현재 자리 XZ를 표면에 투영한 뒤 기존 보정 적용. 가속 발동 버프 포함. |
| G1b | View SO 위치 보정 환산 | 당시 이펙트 큐 위치 offset을 0으로 설정. G1c에서 낙석 터널 경고 예외 복원. 유지 효과는 일반 유닛 높이 기준으로 환산. |
| G1c | 낙석 터널 경고 위치 복원 | 터널 월드 높이를 보존하고 세 폭발의 원래 위치 offset을 사용. |
| G2 | 클립 effectPoint·muzzle, 발사체 접촉 효과 | 시작 순간의 지점 XZ를 표면에 투영한 뒤 기존 보정 적용. 타이밍·회전·비행 경로 유지. |
| G3 | 창구 직접 호출과 별도 Scope: Nexus 착지, 하붑, 철도 | 이미 표면인 경로를 확인하고, 표면이 아닌 경로의 기준점·보정을 분리. 다른 View 작업자의 동시 편집 파일은 그 변경과 대조한 뒤 다룬다. |
| G4 | 에셋 높이 조정 | 아래 목록의 효과별 offset과 프리팹 내부 Transform을 Unity 재생에서 확인해 필요한 값만 View SO/프리팹에서 조정. |

### G1 Task

1. `CombatEffectWindow`에 현재 주입된 표면 투영 함수를 읽는 작은 내부 메서드를 둔다. 새 Mapper나 서비스 조회를 만들지 않는다.
2. 몸 큐는 보정 전 자리의 Y를 표면에 투영한다. 사건 칸 큐의 일반 원본은 이미 `CellPose` 표면이다. `ActorBody`인 가속 버프의 0 offset은 바닥 시작이 된다.
3. 정적 호출 경로와 `git diff --check`를 확인하고 멈춰 보고한다. View 시험 코드를 작성하거나 전투 View MSBuild를 실행하지 않는다.

### 책임·경계 검토

- `FieldGridMapper`는 표면 높이, `CombatEffectWindow`는 주입된 표면 투영 제공, `CombatCuePoints`는 큐 자리·보정 조립을 맡는다(SRP·SSOT). 새 효과는 같은 큐 경로를 재사용한다(OCP). 상속을 추가하지 않아 LSP 영향이 없고 작은 기존 의존성만 확장한다(ISP·DIP).
- Framework `EffectScope`의 생성·해제와 FW 등록 계약, Core 결과·Buffer 계약은 바꾸지 않는다. Body/Cell 큐 이외의 효과는 G1에서 수정하지 않아 기능 변경을 독립적으로 확인할 수 있다. 표면 조회는 생성 시 한 번이며 매 프레임 할당을 늘리지 않는다.
- 예전 몸 높이를 전제로 만든 Y offset은 새 기준에서 시각적 위치가 달라진다. 효과별 프리팹 내부 높이와 함께 Unity에서 확인하고 G4에서 조정한다. 격자 밖 투영 정책과 여러 칸 점유 유닛의 XZ/서는 높이 차이를 확인한다.

## 현재 View SO의 0이 아닌 이펙트 offset 목록

2026-11-06 작업 시작 시점의 `Assets/ProjectL/DataBaseSO/ViewGroup` 직렬화 값이다. 아래 파일의 값은 다른 작업자의 미커밋 변경을 포함할 수 있다. `Position Offset`은 효과 자리에 적용되는 값이며, 프리팹 내부 Transform은 별도다. `StageViewProp`, StageNode, UI 등 효과가 아닌 offset은 제외했다.

| 효과/설정 | 파일 | 위치 offset (x,y,z) | Rider Y | 회전 offset (x,y,z) | 경로 |
|---|---|---:|---:|---:|---|
| 유닛 선택 표시 (`KnockLine`) | `CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` | (0, -0.35, 0) | 0 | (0, 150, 0) | 유지 효과, EF3 표면 기준 완료 |
| 처형 상시 (`Execution`) | `TraitVisual/Item/Cannon/TraitVisual_FinishingShot.asset` | (0, -1.5, 0) | +1 | (0, 0, 0) | 유지 효과, EF3 완료 |
| 희생 상시 (`Sacrifice`) | `TraitVisual/Item/Heavy/TraitVisual_Sacrifice.asset` | (0, -0.35, 0) | 0 | (0, 0, 0) | 유지 효과, EF3 완료 |
| 굳건한 의지 상시 (`Knock`) | `TraitVisual/Item/Heavy/TraitVisual_SteadfastWill.asset` | (0, +0.05, 0) | 0 | (0, 60, 0) | 유지 효과, EF3 완료 |
| 약점 노출 상태 (`Targeting`) | `UnitStatusVisual/Item/UnitStatusVisual_WeaknessExposure.asset` | (0, -0.625, 0) | 0 | (0, 0, 0) | 유지 효과, EF3 완료 |
| 낙석 두 번째 폭발 (`Boom`) | `RockfallVisual/Item/RockfallVisual_Default.asset` | (+1, +0.5, +0.5) | 0 | (0, 0, 0) | 사건 칸 큐, G1 |
| 낙석 세 번째 폭발 (`Boom`) | 같은 파일 | (-1, -0.3, +1) | 0 | (0, 0, 0) | 사건 칸 큐, G1 |
| 지뢰 발동 (`Boom`) | `ObjectVisual/Item/ObjectVisual_Mine.asset` | (0, 0, 0) | 0 | (-90, 0, 0) | 사건 칸 큐, G1 |
| 화약통 파괴 (`Boom`) | `UnitVisual/Item/Object/UnitVisual_PowderBarrel.asset` | (0, 0, 0) | 0 | (-90, 0, 0) | 사건 칸 큐, G1 |

- 가속 특성의 발동 버프(`TraitVisual/Item/Rider/TraitVisual_Acceleration.asset`)는 `ActorBody`, 위치·Rider·회전 offset이 모두 0이다. 지금까지 보인 약 0.85의 높이는 offset 값이 아니라 몸 콜라이더 중심을 시작점으로 사용한 결과다. G1 뒤에는 해당 XZ의 표면이 시작점이다.
- 보호막 상태(`UnitStatusVisual_Shield.asset`)와 하붑 구름(`HaboobVisual_Default.asset`)의 명시적 위치 offset은 0이다. Render 이펙트 프리팹 내부에도 자체 Transform 위치가 있다. 예를 들어 Buff 프리팹 (`Increased movement.prefab`)의 자식 중 하나는 로컬 Y +0.05다. 이는 SO 위치 offset과 구분해 G4에서 확인한다.

## 검증·상태

- 각 Phase 완료 시 Unity에서 평지·고지대, 아군·적군, 움직임/숨김/회수와 효과의 실제 높이를 확인한다. G1은 가속 버프와 몸·칸 큐, 낙석의 XZ offset 및 Y 보정을 우선 확인한다.
- SO 값을 바꾸는 Phase에는 `DataBaseAssetTests` 사용자 확인을 추가한다. 모든 View Phase는 **컴파일·Unity 재생 사용자 확인**이며 View 시험 코드와 전투 View MSBuild를 실행하지 않는다. 커밋·push하지 않는다.
- G1~G1c: 코드·에셋 반영, 정적 검사 완료. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.** G2~G4: 미착수.

### G1 구현 기록

- `CombatEffectWindow.SurfaceOf`가 조립 시 주입된 `FieldGridMapper.OnSurface`를 기존 EffectWindow 수명 안에서 제공한다.
- `CombatCuePoints`가 행위자 몸·대상 몸의 XZ와 회전을 받아, Y를 표면에 맞춘 뒤 기존 `Position Offset`·Rider Y·회전 offset을 적용한다. 사건 칸의 일반 원본은 `CellPose`가 이미 표면에 둔다. 이 경로를 쓰는 가속 특성의 0 offset은 몸 콜라이더 중심 대신 바닥에서 시작한다.
- SO 보정값·Framework·Core·다른 View 기능은 수정하지 않았다. 정적 호출 경로와 변경 파일 `git diff --check`를 확인했다. Unity에서 가속·낙석·피격/사망·특성 발동 큐를 확인해야 한다. **컴파일 미확인.**

### G1b — 기존 위치 offset 환산 (사용자 결정·구현 기록)

- 당시 이펙트 큐(`CombatEffectCue`)의 **위치** offset을 모두 0으로 두었다. 조사한 View SO에서 0이 아니었던 큐 값은 낙석 두 번째 `(1, 0.5, 0.5)`와 세 번째 `(-1, -0.3, 1)`뿐이었다. **현재 낙석 두 값은 G1c에서 원래 값으로 복원했다.** 가속 등 나머지 큐는 0이다. 방향을 위한 회전 offset은 위치 보정이 아니므로 유지한다(지뢰·화약통 X -90°).
- 상시·상태·선택 효과는 큐가 아니므로 효과별 높이를 유지한다. 보통 유닛 프리팹의 BoxCollider 중심 Y `0.63` × 필드 유닛 배율 `0.5` = `0.315`를 예전 몸 기준 Y에 더하고, 수치를 간단히 반올림했다. 이 기준은 모든 유닛에 정확하지 않으므로 Unity에서 Rider·보스·구조물 등도 확인한다.

| 유지 효과 | 변경 전 Y | 변경 후 Y | 비고 |
|---|---:|---:|---|
| 유닛 선택 표시 | -0.35 | 0 | 표면 근처로 정리 |
| 처형 상시 | -1.5 | -1.2 | `-1.5 + 0.315 ≈ -1.2`; Rider Y +1 유지 |
| 희생 상시 | -0.35 | 0 | 표면 근처로 정리 |
| 약점 노출 상태 | -0.625 | -0.3 | `-0.625 + 0.315 ≈ -0.3` |
| 굳건한 의지 상시 | +0.05 | +0.05 | 이미 표면 기준으로 조정된 사용자 값 유지 |

- 당시 낙석의 XZ offset도 0으로 바꾸었으나, 사용자가 터널 경고 폭발의 이전 위치를 요청해 G1c에서 복원했다. 지연 시점 `0.1`초·`0.2`초와 효과 종류는 계속 유지한다.
- 프리팹 내부 Transform 위치, 회전 offset, Rider Y 보정은 수정하지 않았다. G1/G1b 경로 외 효과는 G2·G3에서 바닥 기준으로 전환한다. `git diff --check`와 View SO 큐 위치 offset 재검색을 확인했다. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**

### G1c — 낙석 터널 경고 폭발 위치 복원 (사용자 결정·구현 기록)

- `RockfallVisual_Default` 경고 큐의 두 번째 위치 offset `(1, 0.5, 0.5)`, 세 번째 `(-1, -0.3, 1)`을 기존 값으로 복원했다. 첫 번째는 `(0, 0, 0)`이다.
- 경고 큐는 `FieldRockfallPresenter.TryGetTunnelPose`의 터널 Transform 월드 위치를 받는다. `CombatCuePoints`는 몸 큐만 표면에 투영하고 `EventCell`은 받은 Pose를 그대로 쓴다. 일반 착지 큐는 `CellPose`가 표면 높이를 제공하므로 바닥 기준을 유지한다. 따라서 터널 경고의 높이와 퍼짐은 G1 이전과 같다.
- 터널 폭발의 발생 시점·종류, 돌 낙하·착지, 다른 효과 큐의 0 위치 offset은 변경하지 않았다. Unity에서는 터널 안팎 높이와 세 폭발의 퍼짐, 착지 효과의 표면 높이를 확인한다. `git diff --check`와 경고/충돌 호출 경로를 확인했다. **컴파일 미확인, Unity 재생·DataBaseAssetTests 미확인.**
