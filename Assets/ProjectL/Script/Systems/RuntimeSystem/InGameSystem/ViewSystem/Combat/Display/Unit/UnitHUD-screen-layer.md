# UnitHUD 화면 Layer 전환 — 2026-10-18

> **현재 정책 갱신(2026-10-31, HZ1·HZ2):** 공용 Overlay Canvas와 기존 UI 등록/입력은 유지하고, HUD 크기는 최종 출력 Camera의 투영에 비례한다. 기준 구도는 1920×1080/Ortho Size 7이며 유닛/장애물의 기존 크기 설정을 보존한다. Factory와 Projection은 동일한 ReferenceResolution을 사용한다. 아래의 "줌/회전에도 화면 크기 유지"와 고정 크기 테스트 설명은 10-18 이전 정책의 이력이며 현재 기대 동작이 아니다. 현재 원본은 `docs/superpowers/specs/2026-10-31-unit-hud-zoom-scale-design.md`, 실행/미확인은 같은 날짜 zoom-scale-plan의 HZ1·HZ2 기록을 따른다. 일반 줌 비례 확대·축소는 사용자 확인 완료이며, 입력·연출·해상도·재사용 검증은 별도 미확인이다.

## 현재 사용 지침 — 줌 비례 크기 (2026-10-31)

- 공용 Screen Space Overlay Canvas를 유지한다. 새 필드 연결/씬·프리팹 Canvas 변경은 필요 없다.
- UnitHUDProjection이 실제 출력 Camera의 위치/투영 밀도를 읽고, UnitHUDLayer가 등록 기준 배율에 보정값을 곱해 적용한다. 줌인하면 확대, 줌아웃하면 축소된다. 사용자 줌·연출 줌·블렌드의 별도 상태를 HUD에 복제하지 않는다.
- 기존 유닛/장애물 크기 설정은 기준 구도(1920×1080/Ortho Size 7)의 크기다. 같은 구도로 복귀하면 같은 크기여야 하며 등록 해제 시 기준 배율을 복구한다.
- 같은 HUD의 HP/카테고리/고정 아이템/상태/피해/선택·상세 패널은 함께 확대된다. 내부 로컬 배치/폭은 그대로여서 줌만으로 버프 열 수가 바뀌지 않는다.
- 기존 전투에서 휠 위=줌인/아래=줌아웃을 사용한다. 확인창/연출 중 수동 줌 제한은 기존 입력 정책을 따른다.
- HZ1·HZ2 정적 컴파일/수치 검사는 완료했으며 일반 줌 비례 크기는 사용자 확인 완료, 입력/연출/해상도/재사용은 별도 미확인이다. **확인 목록/결과 원본:** `docs/superpowers/plans/2026-10-31-unit-hud-zoom-scale-plan.md`의 "사용자 Unity 확인 목록 — 줌 비례 HUD". HZ3는 안내까지 완료했고 사용자 확인을 대신하지 않는다.

## 2026-10-18 설계·구현 이력

사용자가 승인한 방향은 공용 Screen Space Overlay Canvas에 UnitHUD를 모으고 최종 출력 Camera로 월드 기준점을 화면에 투영하는 것이다. 수치·상태 표시와 입력은 기존 UnitHUD를 유지한다.

- `IUnitHUDAnchor`: HUD 월드 기준점과 표시 의도만 제공한다. Phase 2에서 FieldUnitView가 구현한다. 공격·반응 자세 대여 중 기준점을 고정하는 현재 정책도 이 제공자가 유지한다.
- `UnitHUDLayer`: 이미 생성된 HUD를 등록받아 화면 위치와 가시성만 관리한다. HUD 생성·Canvas/Scaler 설정·풀·파괴는 조립/호출자가 소유한다. HUD는 Layer의 직접 자식이고 HUD 루트 CanvasGroup의 alpha/interactable/blocksRaycasts는 Layer가 소유한다.
- `UnitHUD`: HP·상태·피해 연출·아이콘 입력을 유지한다. 화면에서 숨길 때 hover만 즉시 정리하는 내부 호출을 추가한다. 핀 선택과 수치/연출 상태는 보존한다.
- `Canvas.preWillRenderCanvases`에서 모든 등록 위치를 갱신한다. 일반 PlayerLoop에서 LateUpdate의 Cinemachine 출력 반영 이후, UI Graphic 재생성 전에 실행한다. 강제 Canvas 갱신은 여러 번 호출될 수 있으므로 프레임당 1회 제한을 두지 않는다. 수동 Camera 갱신을 쓰는 호출자는 마지막 Camera 변경 뒤 Canvas 렌더를 수행해야 한다.
- Camera는 명시적으로 주입한 실제 출력 Camera 하나다. Virtual Camera·Camera.main을 조회하지 않는다. 출력 Camera 교체는 SetOutputCamera 한 곳으로 전달한다.
- 출력 Camera 부재/비활성, 카메라 뒤·near/far 바깥·viewport 밖, 다른 디스플레이는 숨긴다. 자동 가장자리 고정·겹침 회피·월드 물체 가림 검사는 하지 않는다.
- 화면 밖에서 GameObject를 끄지 않는다. CanvasGroup으로 표시·포인터·Submit만 막아 HP/피해 연출이 계속 진행되게 한다. 숨김 전환 때 UIInputBinding의 hover를 한 번 해제하고 핀 선택은 보존한다.
- 등록 해제·Layer 비활성·Release·파괴 때 표시와 입력을 닫고 정적 Canvas 콜백을 해제한다. Layer는 전달받은 HUD를 파괴하거나 풀로 반환하지 않는다.

## Task와 Phase

### Phase 1 — 독립 Layer와 계약 (코드 완료)

1. 월드 기준점/표시 의도 계약과 공용 화면 Layer 구현.
2. UnitHUD에 상태 아이콘 hover 정리 경계 추가.
3. 투영·줌/회전·화면 밖 연출 유지·출력 Camera 교체·입력 차단/핀 보존·해제 회귀 테스트 작성 및 컴파일 확인.
4. 이 Phase 결과를 보고하고 멈춘다. 기존 운영 생성 경로·씬·프리팹은 교체하지 않는다.

### Phase 2 — 기존 생성 경로 교체 (사용자 승인 후 코드 완료)

1. Factory에 Combat 수명의 공용 Canvas/CanvasScaler/GraphicRaycaster와 Layer 조립을 둔다. 기존 씬 Canvas와 동일한 1920×1080 기준을 사용하고 모달/유지 UI 아래 정렬한다. 사용자 편집 중인 GlobalUICanvas 프리팹은 수정하지 않는다.
2. 기존 unitHudScale/obstacleHudScale은 월드 배율이므로 화면 배율과 혼용하지 않는다. 실제 프리팹 크기와 기준 시점에서 화면 크기를 확인해 명시적 화면 배율로 이전한다.
3. FieldUnitPresenter의 HUD 생성/재사용과 FieldUnitView의 기준점·요청 가시성·풀 반환·비활성·파괴를 연결한다. World Space Canvas 생성, 빌보드, 부모 크기 상쇄와 카메라 캐시를 제거한다.
4. 전투 종료·재설치·공격 자세/크기·Drag 숨김·덱 화살표·아이콘 입력의 영향 회귀와 Unity 시각 확인을 수행한다.

## 경계와 설계 점검

- SSOT: HP·규칙은 Buffer, 표시 내용은 UnitHUD, 기준점/표시 의도는 Anchor, 투영/가시성 캐시는 Layer가 소유한다. 화면 위치를 전투 상태로 저장하지 않는다.
- S: 수치 표시, 월드 기준점, 투영, 생성/연결을 분리한다. O: 화면 표시 정책은 Layer에서 바꾼다. L: Anchor는 현재 기준점과 표시 의도를 제공하며 Layer는 HUD 내용을 변경하지 않는다. I: Entity/Buffer/전체 Presenter 대신 두 프로퍼티만 받는다. D: Layer는 FieldUnitView 구체 타입 대신 Anchor 계약과 주입된 Camera에 의존한다.
- FW/JMH: 내부 표시 객체를 서비스로 등록하지 않고 기존 Combat 씬 수명에서 조립한다. Framework/서비스 Build·UI 등록/이력·InputSystem의 액션/Layer 계약을 변경하지 않는다. Canvas 콜백은 UI 입력 이벤트가 아니라 최종 투영 시점에 필요한 렌더 수명 연결이며 활성/해제에 맞춰 구독을 닫는다. 입력은 기존 UIInputBinding의 포인터/Submit 직접 호출을 유지한다.
- 독립성: 기존 카메라/줌 대응 수정은 FieldUnitView의 자세·연출 코드까지 이해해야 했다. 전환 후 투영/화면 가시성 변경은 UnitHUDLayer와 해당 테스트에 한정한다. HP 변경은 UnitHUD에, 기준점 고정 변경은 FieldUnitView에 남는다. Phase 2의 공통 경계(풀 반환·숨김·종료)는 관련 연출/입력 회귀가 필요하다.
- Core·FieldCamera·Architecture, DB와 사용자 수정 GlobalUICanvas는 변경 대상이 아니다.

## 검증 기록

- Phase 1 Task 1~3 코드 완료. `IUnitHUDAnchor`·`UnitHUDLayer`와 `.meta`를 추가하고 UnitHUD에 hover만 해제하는 내부 경계를 추가했다.
- `UnitHUDLayerTests` 6개를 작성했다. 최신 출력 Camera 투영과 Perspective/Orthographic 줌·회전의 크기 유지, 공용 Canvas와 clip/화면 밖 숨김 중 HP 예상 상태 보존, EventSystem 포인터/Submit 차단·hover 해제·핀 유지, 명시적 출력 Camera 교체/부재, 비활성/등록 해제/Release 수명, 잘못된 등록/파괴된 HUD 처리를 확인하는 테스트다.
- VS MSBuild로 `ProjectL.Systems`와 `ProjectL.Tests.Framework` Compile 성공(오류 0, 변경 파일 경고 없음). 신규 파일은 임시 targets로 포함했다. 기존 어셈블리 참조와 빌드 산출물은 OS 임시 폴더 `ProjectL-UnitHUDLayer-20261018` 안에서만 사용했다. 저장소의 자동 생성 csproj는 수정하지 않았다.
- **Unity EditMode 테스트 실행과 Play 시각 확인은 미완료다.** 테스트 작성·컴파일을 실행 통과로 간주하지 않는다. Cinemachine 블렌딩/흔들림·피해 숫자의 실제 시간 진행은 Phase 2 연결 뒤 Play로 확인해야 한다.
- 기존 전투 HUD의 생성 경로는 아직 World Space 방식이다. 새 Layer는 운영 경로에 연결하지 않았다. Phase 2에서 실제 화면 전환을 수행한다.
- `git diff --check` 통과. 사용자 수정 GlobalUICanvas, 씬, Core·FieldCamera·Architecture는 편집하지 않았다. 커밋·push하지 않았다.
- AGENTS.md의 Phase 리뷰 지점에 따라 여기서 멈춘다.

## Phase 2 구현 기록 — 2026-10-18

사용자가 Phase 2 진행을 지시했다. 위 Phase 1의 운영 미연결 상태는 이전 이력이며 아래 상태가 현재다.

### 구현

- `CombatUnitHUDFactory`가 CombatSceneRoot 아래에 공용 Overlay Canvas/CanvasScaler/GraphicRaycaster/UnitHUDLayer를 생성한다. 기준 해상도 1920×1080, 기존 씬과 같은 너비 기준 Scaler, sortingOrder -10으로 씬 UI(0)와 유지/모달 UI(999) 아래에 둔다. 전체 화면을 가리는 Image는 생성하지 않는다. 출력 Camera는 root.WorldCamera, 없으면 기존 정책에 따라 조립 때 Camera.main을 한 번만 선택한다.
- `CombatSceneDisplayFactory`가 새 Layer를 FieldUnitPresenter와 CombatSceneDisplay에 전달한다. 일반 Clear는 HUD/Layer를 재사용하고, 최종 Release는 유닛 풀 안의 HUD까지 해제한 뒤 Layer의 콜백과 Canvas를 정리한다. 같은 씬에서 새 Display를 조립하면 기존 모델 풀에 새 HUD를 연결한다.
- `FieldUnitPresenter`는 HUD 프리팹을 Layer 아래에 생성하고 화면 배율을 적용한다. `FieldUnitView`는 주입된 HUD의 표시 요청·기준점·활성/풀 반환/파괴 수명만 담당한다. 유닛별 Canvas/GraphicRaycaster, HUD 카메라 캐시, LateUpdate 빌보드, 부모 크기 상쇄를 제거했다.
- HUD는 모델의 자식이 아니므로 View의 OnEnable/OnDisable에서 활성 상태를 명시적으로 맞춘다. Drag 숨김과 풀 반환 때 즉시 비활성화하고, 풀 반환 후에는 같은 HUD를 재사용한다. 화면 밖 가시성만 바뀔 때는 Phase 1대로 CanvasGroup을 써서 연출을 유지한다.
- 공격/반응/소환 자세 대여 중에는 기존 시작 위치에 HUD 기준점을 고정한다. 일반 이동은 현재 월드 위치를 따른다. 포효 등의 모델 배율과 화면 HUD 크기는 독립적이다. 기존 3D 체력 숫자 대체 표시는 유지했다.
- 실제 HUD 프리팹의 루트는 60×20, 배율 1이고 기존 Combat Camera는 Orthographic Size 7이다. 1080 높이에서 옛 월드 배율 0.02는 `0.02 × 1080 / (2 × 7) = 1.5428572` 화면 배율에 해당한다. 코드 필드를 `unitHudScreenScale`/`obstacleHudScreenScale`로 교체하고 Combat.unity의 두 값만 이전했다. 옛 월드 배율을 FormerlySerializedAs로 잘못 읽지 않는다. 다른 씬/프리팹에는 해당 직렬화 필드가 없음을 검색했다.

### 검증과 남은 확인

- `ProjectL.Systems`와 `ProjectL.Tests.Framework` MSBuild Compile 통과(오류 0, 변경 파일 새 경고 없음). 에디터가 이미 csproj에 반영한 파일은 임시 targets의 추가 목록에서 제거해 중복 Compile 입력도 정리했다. 산출물은 이전과 같은 OS 임시 폴더에만 썼다.
- `UnitHUDLayerTests`에 실제 FieldUnitView의 숨김/재활성/해제를 추가했다(총 7개). UnitHUDTests의 기존 월드 Canvas 검증을 화면 Canvas·모델 배율 독립·풀 재사용으로 교체했다. CombatSceneEventPresenterTests에는 Clear/Install 재사용, Drag 숨김, Release 후 Canvas/HUD 제거, 같은 씬 재연결과 모델 풀 재사용을 검증하는 테스트를 추가했다.
- 기존 장애물·포탄 자산/반동·적 회전 테스트는 HUD 기준점/화면 Rect를 검증하도록 변경했다. 배치/공격/이동/예상 HP fixture는 같은 운영 Factory로 화면 Layer를 조립한다. 포인터/Submit 검증은 EventSystem handler dispatch를 유지한다.
- Unity 6000.0.73f1의 EditMode 배치 실행을 시도했으나 `HandleProjectAlreadyOpenInAnotherInstance`에서 거절돼 **테스트는 실행되지 않았다**. 새 배치 프로세스가 종료됐으며 열려 있는 에디터를 닫거나 조작하지 않았다. 로그는 OS 임시 폴더의 `unity-phase2.log`, 컴파일 로그는 `systems-phase2.log`/`tests-phase2.log`다.
- 남은 실행 확인: 열린 Unity Test Runner에서 UnitHUDLayerTests·UnitHUDTests·CombatSceneEventPresenterTests·CombatMortarPresentationTests·CombatMortarAssetTests·CombatEnemyPresentationTests·CombatPlacementFeedbackTests·CombatAttackFeedbackTests·CombatMovementDraftDisplayTests·AttackHealthPreviewDisplayTests 실행. Play에서 카메라 블렌드/줌/흔들림 중 위치와 고정 크기, 버프 hover/핀/Submit, 화면 밖 피해 연출 복귀, 장애물 HP, 포기/종료 후 HUD 잔상 여부를 확인한다. **Play 시각 검증은 아직 미완료다.**

### 경계 재점검

- SSOT/SOLID: 규칙·수치/선택 원본은 변경하지 않았다. 화면 변환은 Layer, 기준점과 표시 의도는 View, HUD 프리팹 대여는 Presenter, Canvas 조립은 Factory, 연결의 최종 해제 순서는 Display가 소유한다. 별도 서비스나 범용 중간 계층은 추가하지 않았다.
- FW/JMH/독립성: 서비스 Build·UI 이력·InputSystem/입력 바인딩을 변경하지 않았다. 생성/해제 연결 때문에 Factory와 Display를 수정했으며, 투영 변경은 이후 Layer와 해당 테스트에서 처리할 수 있다. 이번 공통 수명 변경의 영향은 풀/Drag/공격 자세/크기/종료 회귀로 확인하도록 범위를 명시했다.
- Core·FieldCamera·Architecture·DB·사용자 수정 GlobalUICanvas는 수정하지 않았다. Combat.unity 변경은 화면 배율 필드 두 개뿐이다. 커밋·push하지 않았으며 이 Phase 리뷰 지점에서 멈춘다.


## 사용자 일반 줌 확인 (2026-10-31)

줌인·줌아웃 시 HUD도 함께 확대·축소됨을 사용자가 확인했다. 상세 확인 범위/잔여는 zoom-scale-plan의 사용자 확인 표와 마지막 기록을 따른다. 다른 입력/연출/재사용 항목까지 확인 완료로 추정하지 않는다.
