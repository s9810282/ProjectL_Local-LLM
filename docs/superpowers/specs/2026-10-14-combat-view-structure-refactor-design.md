# 전투 View 구조 리팩토링 설계 (2026-10-14)

- **목표:** 10-11~10-14 연출·환경 추가로 커진 `CombatViewSystem`·`CombatSceneDisplay`·`CombatSceneEventPresenter`를 SOLID·SSOT·FW 계약 기준으로 정리한다. 새 Presenter·새 Core 사건을 붙일 때 고쳐야 하는 곳을 한 곳으로 줄인다.
- **결정(2026-10-14, 사용자):** R0 = 연출 없이 사실만 반영·모르는 사건은 경고 기록, R4 = 타임라인 UI 포함·Audio 제외, 진행 = Claude 직접 구현 + Phase마다 실습 워크시트 전달. D12는 가시밭 전용 View 표(`ThornFieldVisual`)로 다시 정했다.
- **Task·진행·검증 상태:** [리팩토링 계획](../plans/2026-10-14-combat-view-structure-refactor-plan.md)에만 적는다. View 계획 §7.77은 시작점 링크만 둔다.
- **선례:** [SOLID·SSOT 개선 설계](2026-10-09-combat-view-solid-improvement-design.md)(D2·D3 컨트롤러 분리, D5 정리 실패 모음), [구조 개선 설계](2026-10-06-combat-view-refactor-design.md)(VR1~VR6).
- **점검 기준:** [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(SSOT·SOLID·FW 계약·경계).

## 0. 범위

**포함**

- `CombatViewSystem`, `CombatSceneDisplay`, `CombatSceneEventPresenter`, `OrderedCombatPresentation`과 이들이 쓰는 표시 계약.
- 타임라인 UI 열기·닫기(현재 `CombatViewSystem.SyncTimelineUI`, 현강님 코드).

**제외**

- **Audio 로직.** BGM 프리셋 결정(3곳)·효과음 프리셋 표의 호출 위치와 값은 바꾸지 않는다. 클래스를 나눌 때 오디오 호출은 원래 클래스에 남기거나 콜백으로 그대로 넘긴다. 발견 사항은 §5에 기록만 한다.
- `GetTimeline` Core 직접 조회(P6 위반) — VR6 대기.
- Core·Buffer 계약, 씬(`Combat.unity`) 참조. DB 변경이 필요한 D12는 착수 전에 따로 확인한다.

## 1. 지금 문제 (2026-10-14 리뷰)

| ID | 원칙 | 위치 | 문제 |
|---|---|---|---|
| P1 | O·버그 | `CombatSceneEventPresenter.CanPresent`, `OrderedCombatPresentation.Play` | 허용 목록에 없는 사건이면 `NotSupportedException`으로 전투가 멈춘다. Core가 이미 `CombatFallEvent`(10-11)·`CombatRoarEvent`·`CombatSummonEvent`(10-13)를 결과에 넣는다 |
| P2 | O·D | `CombatViewSystem` 필드·`InitData`·`BindServices`(15인자)·`Attach` | 표시가 쓰는 View DB 8개·Render 서비스를 조정자가 넘기려고만 보관한다. Object·Terrain·Railway 추가(b5b13961·7ec2b981·d16e95d0)가 매번 이 다섯 곳을 고쳤다 |
| P3 | DRY | 4개 파일 `RunAll`/`RunCleanup`, `CombatViewSystem` 10곳 | 같은 실패 모음 코드가 복사돼 있다 |
| P4 | S | `CombatSceneDisplay` :617-722 | 표시 Facade가 기차 접촉·사건 순서·버전 번호·실패 정리까지 맡는다 |
| P5 | I·L | `OrderedCombatPresentation` :61·76·86·112·144 | Barrier·Sequence·MovementStart·MovementEnd를 `as`/`is`로 꺼내는 숨은 계약. 구현하지 않으면 대기를 조용히 건너뛴다 |
| P6 | O·SSOT | `CombatSceneDisplay` :164·183·240·251·644·662 | 정리 목록 6벌을 손으로 맞춘다. Install 직전 정리와 실패 정리의 구성이 이미 다르다 |
| P7 | L | `CombatSceneDisplay` 표시 의도 메서드 | 해제 뒤 호출 무시(`_isReleased`)가 일부 메서드에만 있다 |
| P8 | S | `CombatSceneDisplay` :267-325 | 개발 Layer 패널 버튼 슬롯 처리가 표시 안에 있다 |
| P9 | S·§25 | `CombatSceneEventPresenter` 전체(829줄) | 표시 사실·이동/회전·간격 대기·공격 4종·투사체 풀·Ballista·반응·오디오가 한 클래스. 턴 종료마다 `GetComponent<FieldUnitView>()` |
| P10 | S·SSOT | `CombatViewSystem.SyncTimelineUI` :689-702 | 타임라인 UI 보임을 명령 종류로 판단한다. 재연결 때 열지 않고, 연결 해제·오류 정지 때 닫지 않는다 |
| P11 | O·SSOT | `CombatSceneDisplay` :149-154 | Core 상태인 가시밭의 프리팹을 `Unit_CactusCannon` 공격 연출 설정에서 찾는다 |

## 2. 결정

### D1. 표시 계약이 없는 사건은 경고하고 건너뛴다 (R0)

- `OrderedCombatPresentation`이 정책의 주인이다. "표시할 수 있는지"는 지금처럼 이동 판정 또는 `ICombatResultPresenter.CanPresent`로 정한다.
- 표시할 수 없는 사건은 **사실 반영 없이** 순서에서 건너뛴다. 결과의 다른 사건은 그대로 재생한다.
- 경고는 `UnityEngine.Debug.LogWarning` 한 줄이다. 같은 씬 연결(`Attach`) 안에서 **사건 형식마다 한 번만** 남긴다. 적 턴마다 같은 경고가 쌓이지 않게 하기 위해서다.
- 근거: 결과 확인 전에 `CombatViewSystem.FinalizeResult`가 Buffer 최종 상태로 `Refresh`하므로, 건너뛴 사건의 사실도 결과가 끝나면 화면에 맞춰진다. 중간 연출만 빠진다.
- 바뀌는 계약: 기존 테스트 `MissingNonMovementHandler_BlocksReconcileAndAcknowledgement`(즉시 정지)를 "경고 → 건너뜀 → 갱신·확인"으로 바꾼다.

### D2. 낙하·포효는 표시 없음, 소환은 생성 사실만 반영한다 (R0)

| 사건 | Core 계약 | View 처리 |
|---|---|---|
| `CombatFallEvent` | 사망·제거는 별도 사실로 온다 | 아무것도 바꾸지 않고 완료 |
| `CombatRoarEvent` | 피해·넉백은 뒤따르는 일반 사건으로 온다 | 아무것도 바꾸지 않고 완료 |
| `CombatSummonEvent` | `Created`는 등장 효과 전 생성 시점 사실 | 표시 사실에 `Created`를 넣고 다시 그린 뒤 완료(애니메이션 없음) |

- 세 사건은 "아는 사건"이므로 `CanPresent`에 넣는다(경고 대상이 아니다). 연출은 보스·지형 연출 작업에서 따로 붙인다.

### D3. 정리 실패 모음 헬퍼 (R1)

- 새 파일 `RuntimeSystem/Common/CleanupErrors.cs`(internal static). `Try(Action, ref List<Exception>)`·`RunAll(string message, params Action[])`·`ThrowIfAny(List<Exception>, string message)`.
- 적용 범위는 전투 View 네 파일(`CombatViewSystem`·`CombatSceneDisplay`·`CombatSceneEventPresenter`·`OrderedCombatPresentation`)과 같은 헬퍼를 복사한 `CombatMovementPlanningPresenter`·`CombatPathInput`이다. Lobby·StageMap 등 다른 작업 영역은 건드리지 않는다.
- 동작 불변: 실패를 모으고 끝까지 진행한 뒤 `AggregateException`을 한 번 던지는 기존 규칙(10-09 D5)을 그대로 따른다.

### D4. 표시 의존성은 묶음 두 개로 넘긴다 (R1)

**후속 변경(2026-10-14):** 아래는 R1 구현 당시의 결정이다. 사용자 합의에 따른 [후속 설계 §5.3·§5.5](2026-10-14-combat-scene-display-followup-design.md)와 [조립·기능 분리 계획 SD-D1](../plans/2026-10-14-combat-scene-display-composition-plan.md)은 Display의 직접 조립·생성자 형태·Factory 미도입 결정을 대체한다. Tables/Services 묶음과 ViewSystem의 개별 표·Presenter 의존 차단은 유지한다. SD-D1 구현 전까지 현재 코드와 R1 완료 이력은 그대로다.

- `CombatDisplayTables`(View DB 묶음): 필드 설정·Railway 설정·유닛·프랍·특성·Object·Terrain·환경 외형. `From(IViewQuery)`가 지금 `InitData`의 null 검사를 그대로 한다. 테스트가 필드 설정만 바꿔 쓸 수 있게 개별 값 생성자도 둔다.
- `CombatDisplayServices`(Render·Audio 서비스 묶음): Color·AreaOutline·DrawPointLine·Effect·Audio. 오디오는 참조 전달 경로만 바뀌고 호출은 그대로다.
- `CombatSceneDisplay(root, tables, services)`로 생성자를 줄인다. `CombatViewSystem`은 묶음 두 개만 보관한다. `BindServices`(테스트 입구)는 Render 서비스 4개 대신 `CombatDisplayServices` 하나를 받는다.
- Factory 클래스는 만들지 않는다. 생성 줄은 하나뿐이라 묶음으로 충분하다(지침 §27).
- 이후 표 하나를 더하면 `CombatDisplayTables`와 `CombatSceneDisplay`만 고친다.
- **함께 확인:** `IsMoveMode`(:85)와 `isPathTarget`(:869)의 판단 차이. 동작이 같으면 Router 판정으로 바꾸고, 다르면 주석만 사실대로 고친다.

### D5. 사건 재생 조정을 표시에서 분리한다 (R2)

- 새 클래스 `CombatSceneEventPlayback`: 기차 접촉(`CombatRailwayContactPlayback`)·사건 Presenter·`_presentationVersion`·실패 정리를 소유한다. 지금 `CombatSceneDisplay` :617-722의 본문이 옮겨 간다.
- `CombatSceneDisplay`는 값과 표시 의도만 Presenter에 나눠 주고, 재생 계약은 이 객체에 넘긴다.
- **2026-10-15 갱신(구조 개선 SV3):** 이 객체의 버전 필드는 범위가 드러나게 `_cleanupVersion`(실패 정리 버전)으로 바꿨다. 사건 Presenter의 재생 세대와는 별도 카운터다. 재생 계약은 Display가 구현하고 호출을 이 객체에 위임하며, 이 객체는 계약을 선언하지 않는다(D6 갱신 참조).

### D6. 숨은 재생 계약을 명시한다 (R2)

- `ICombatEventPlayback`: 결과 표시(`ICombatResultPresenter`)·재생 제어·이동 재생·순서 준비·Barrier·이동 시작/끝 계약을 하나로 묶는다.
- `ICombatBattleDisplay`가 이 계약을 포함하고, `OrderedCombatPresentation`은 `as`/`is` 없이 부른다.
- 테스트 가짜(`CombatViewSystemTests.FakeDisplay`)는 "대기 없음·즉시 완료" 기본 구현을 갖는다. 지금 가짜가 받는 동작과 같다.
- **2026-10-15 형식 갱신(구조 개선 SV3, [SV 계획](../plans/2026-10-15-combat-view-structure-improvement-plan.md) Q6):** 의도(숨은 형식 검사 제거·계약 명시)는 그대로다. 이 합성 외에 소비자가 없던 조각 6개(`ICombatResultPresenter`·`ICombatPresentationControl`·`ICombatMovementStartPresenter`·`ICombatMovementEndPresenter`·`ICombatPresentationSequencePresenter`·`ICombatPresentationBarrier`)는 `ICombatEventPlayback`의 멤버로 흡수했다. 이동 보간이 받는 `ICombatMovementPlaybackPresenter`만 별도 계약으로 남는다. Display 내부 재생 객체(`CombatSceneEventPlayback`·`CombatSceneEventPresenter`)는 이 계약을 더 이상 선언하지 않는다(멤버 이름은 같음).

### D7. 개발 Layer 패널 버튼 연결을 분리한다 (R2)

- 새 plain C# 클래스 `CombatDevLayerPanelBinding`(Display/): 패널 GameObject와 버튼 두 개를 받아 `IUIInputReceiver` 슬롯을 처리한다. 씬·SceneRoot 직렬화 필드는 바꾸지 않는다.
- 버튼 입력 규칙은 10-09 D1(`UIInputBinding` + 슬롯)을 그대로 따른다.

### D8. 정리 목록을 이름 있는 묶음으로 만든다 (R2)

- 필드 연결 해제(`UnbindField`)·연출 취소(`CancelPlayback`)·소유 자원 해제(`ReleaseOwned`) 세 묶음을 한 번씩만 정의하고 Install·Clear·Release·Cancel이 조합한다.
- Install 직전 정리와 실패 정리의 현재 차이(`_movementPlanning` / `_objects`·`_terrain`)는 코드와 해당 수명 회귀로 의도를 확인한다. 의도면 그 이유를 주석으로 남기고, 아니면 통일한다.
- 검증 근거(수명별, 모두 기존 테스트):

  | 수명 | 표시 쪽 근거 (`CombatSceneEventPresenterTests`) | 좌표 쪽 근거 (`CombatPointerMappingTests`) |
  |---|---|---|
  | Install 실패 때 Object·Terrain 정리 | `TerrainFacts_BothFieldModes_CoexistWithObjectsAndReuseMovingTornado`(실패한 Install 뒤 활성 Terrain·Object 0, :348-351) | — |
  | Install 실패 때 환경 효과 회수·사건 연결 해제 | `Explosion_FailedReinstallRecallsOldEffectsAndUnbindsEvents` | — |
  | Build 실패 뒤 이전 연결 차단 | `BuildFailure_DropsOldEventBinding_EvenWhenOldFieldStillExists` | `BuildFailure_UnbindsMapping_EvenWhenOldFieldStillExists` |
  | 이동 중 Clear → 새 Mapper로 재설치·같은 씬 자원 재사용 | `ClearDuringMovement_ReinstallUsesNewMapper_AndSameSceneResources` | `Clear_UnbindsMapping_DropsUiCache_AndReinstallMapsAgain` |
  | Release 뒤 거절 | `FirstInstall_BindsAfterBuild_AndLateEventsAfterReleaseCannotRenderOrComplete` | `Release_ClearsReferences_AndLateCallDoesNotRaycastOrRecreateData` |

- **근거 없음:** Install 직전 `_movementPlanning.Clear`(이동 범위·편집 고스트)를 확인하는 테스트는 없다. R2에서 목록을 통일하거나 이 정리를 옮기기 전에, 이동 계획 표시 중 재설치하면 범위·고스트가 남지 않는지 보는 회귀를 먼저 추가한다.
- `CombatDisplayReuseTests`는 일반 View·UI 재사용 회귀로 함께 유지하며, Install 실패 수명의 근거로 삼지 않는다.
- **결론(R2-2):** 차이는 의도다. Object·Terrain은 Mapper를 붙잡고 스스로 진행하지 않아 Install 직전에는 비우지 않고(Refresh가 새 Mapper로 다시 그림), 실패 때만 비운다. 공통 7개는 한 목록(`_fieldBound`)으로 정의해 세 곳이 함께 쓴다. 세부는 계획 §6.6.

### D9. 해제 뒤 호출 규칙을 한 줄로 고정한다 (R2)

- 표시 의도·집기 메서드는 해제 뒤 **아무것도 하지 않는다**(집기는 false). 사건 시작·Install·Refresh는 해제 뒤 **예외**다. 지금 빠진 메서드(`ClearMovementRange`·`EndMovementPlanning`·`BeginMovement`·집기 메서드)에 같은 규칙을 적용한다.
- **분류(R2-2):** `ClearMovementRange`·`EndMovementPlanning`은 표시 의도(무시), `BeginMovement`는 재생 계약의 사건 시작 계열(예외)이다. `BeginStep`·`BeginMovementEnd`·`ShowReturn`도 예외를 명시했다. 넷 모두 예전에도 사건 Presenter에서 같은 형식으로 던졌으므로 동작은 같다.

### D10. 사건 Presenter를 재생 단위로 나눈다 (R3)

- `CombatAttackPlayback`: 공격 4종(Linear·Melee·Mortar·Ballista) 시작·Tick·취소, 투사체 풀, Ballista 무기·재장전, 관통 대응(`CombatBallistaImpactMap`). 종류 분기는 이 클래스 안 switch 한 곳에 둔다(전략 등록은 만들지 않음, §27).
- `CombatUnitMotionPlayback`: 걸음 보간·회전·복귀.
- `CombatSceneEventPresenter`에는 사건 분배·표시 사실·간격 대기·피격/사망 반응·오디오 호출이 남는다. 오디오는 지금처럼 이 클래스가 부르고, 공격 재생에는 기존처럼 콜백으로 넘긴다.
- **Ballista 무기의 주인은 `FieldUnitPresenter`의 현재 View 대여 한 곳이다(SSOT).** 턴 종료 재장전은 새 조회 `FieldUnitPresenter.TryGetBallistaWeapon(actorId, out weapon)`(기존 `GetBallistaWeapon`의 예외 없는 판)으로 그 순간의 대여에서 찾는다. 이것으로 턴 종료의 `GetComponent<FieldUnitView>()` 루프를 없앤다.
- **ID를 키로 하는 캐시는 두지 않는다.** 풀 반환 뒤 같은 무기 객체를 다른 개체가 빌릴 수 있고 `IsAvailable`로는 구별할 수 없어서, 캐시를 두면 대여 반환·재바인딩에 맞춘 무효화 계약과 결합이 따로 필요해진다.
- Tick 대상은 지금처럼 무기 객체 집합(`HashSet<BallistaWeaponView>`)으로 두어 객체당 한 Tick을 유지한다. 발사·재장전 시작 때 넣고, 취소·Clear·Release에서 비운다. 풀로 돌아간 무기는 `IsAvailable`이 false라 Tick하지 않는다. 턴 종료 사건의 기존 표시 시점 판단(`_ballistaWeapons.Count > 0`)은 그대로 둔다.
- 검증(R3.3a): A의 View를 풀에 반환한 뒤 B가 같은 무기 객체를 빌렸을 때, A의 턴 종료는 재장전하지 않고 B의 턴 종료는 재장전을 한 번만 하며 무기 객체당 Tick이 한 번인지 확인한다. 취소·Clear·Release 뒤 Tick 집합이 비는지도 포함한다. 분리 뒤의 수명 계약이며 현재 코드의 버그가 아니다(현재 코드는 매번 `GetComponent<FieldUnitView>().EntityId`로 현재 주인을 확인한다).

### D11. 삭제된 타임라인 UI의 열기·닫기 경로를 폐기한다 (2026-10-16 후속)

- `993b0068`에서 Combat 씬의 Timeline UI가 삭제됐다. 사용자가 남은 호출 정리를 지시해 기존 독립 `CombatTimelineViewSystem`과 Factory 생성·모듈 등록·오류 정리 연결을 제거한다.
- 밤 진입·결과 표시·씬 이탈·오류 정지 어느 경로에서도 삭제된 `UIId.Timeline`을 열거나 닫지 않는다. 보임 캐시·씬 연결·Tick 자체를 없애며, UI 등록 유무 검사로 남은 시스템을 우회하지 않는다.
- `CombatViewSystem`의 입력·연출·결과 소비, Core의 Timeline 규칙·조회·Buffer는 유지한다. 당시 호출 정리에서는 다른 작업자 소유 `TimelineUI.cs`와 공용 `UIId.Timeline`을 보존했으나, 이후 UI 작업자의 `053e151c`에서 둘 다 삭제됐다. 후속 머지 수정은 이 삭제 의도를 유지하고 회귀 테스트의 정적 타입 참조만 제거한다(계획 §6.13).
- 기존 2026-10-14 독립 시스템 도입 결정·검증은 [리팩토링 계획](../plans/2026-10-14-combat-view-structure-refactor-plan.md) §6.8의 과거 이력이다. 현강님 공유 메모의 기존 내용 정리 요청도 철회한다. 재도입은 별도 설계로 정한다.
- 검증·실행 기록은 같은 계획 §6.12. 실제 Boot·Combat 씬에서 메인 배치·밤 진입의 수락과 표시 완료를 확인하고, 관련 전투 View·수명 정리 테스트를 실행한다.

### D12. 가시밭 칸 외형은 가시밭 전용 View 표가 가진다 (R4.3)

- **결정(2026-10-14, 사용자):** 가시밭 칸의 3D 외형 원본을 새 View 표 `ThornFieldVisual`(설정 하나, `Config`)로 옮긴다. 이 결정은 처음 안(`StageFieldViewData` 필드 표시 설정으로 이전)을 대체한다.
- **근거:**
  - Core는 가시밭 칸을 별도 목록(`CombatCommandState.ThornFields`)으로 넘긴다. 칸 정보에는 칸·남은 턴·피해만 있고 생성 원인이 없다.
  - 생성 경로는 지금 가시밭 특성(`TraitKey.ThornField`, `ThornFieldEffectRule`) 하나뿐이다. Thorn Heart 아티팩트는 칸을 만들지 않는 즉시 피해다.
  - 따라서 외형의 주인은 원인이 아니라 **가시밭 칸 자체**다. 누가 만들었든 가시밭 칸은 같은 모양이다.
  - 같은 형태의 선례가 있다. 레일 칸도 Core가 따로 관리하는 필드 기능이고, 전용 표 `RailwayVisual`(설정 하나, 항목 정확히 하나)이 프리팹과 연출 값을 가진다.
- **검토했지만 쓰지 않은 안:**
  - 특성 외형(`TraitVisual_ThornField`): 이름·아이콘·설명용 UI 표에 대부분 비어 있을 3D 필드가 섞인다. "가시밭 칸 = 가시밭 특성의 결과"라는 대응도 코드에 가정해야 한다.
  - 필드 표시 설정(`StageFieldViewData`, 처음 안): 격자·셀·고스트 설정에 섞이고, 칸을 남기는 기능이 늘 때마다 설정 항목도 는다.
  - View 전용 종류 키를 가진 필드 효과 표: 지금은 종류가 하나뿐이라 과설계다.
  - 씬 `CombatSceneRoot`에 연결: 이번 리팩토링 범위 밖인 씬 참조를 고쳐야 하고, 외형 자산은 View DB에 둔다는 기존 방식과 어긋난다.
  - 현재 위치(`Unit_CactusCannon`의 Mortar 공격 연출): P11 그대로다. 다른 유닛이 특성을 받거나 공격이 Mortar가 아니면 프리팹이 없어 예외가 난다.
- **표 구성:**
  - `ThornFieldVisualData`: 지금은 `prefab` 하나다. `FieldThornPresenter`가 프리팹 말고는 쓰지 않는다(크기·회전은 프리팹 값). 계산 가능한 표현 필드는 두지 않는다(P7). 가시밭 연출 값이 생기면 이 표에 넣는다.
  - 표는 레일처럼 **항목 정확히 하나**를 요구한다.
  - **프리팹은 DB 단계에서 필수로 하지 않는다.** 아트 하나가 빠졌다고 DB 생성·부팅 전체가 멈추지 않게 하려는 것이다.
    - 표시: 프리팹이 없으면 지금처럼 가시밭이 처음 나올 때 `CombatSceneDisplay`가 "가시밭 표시 프로필이 연결되지 않았습니다"로 실패한다.
    - 누락 확인: `DataBaseAssetTests`가 잡는다. InGame에 `ThornField` 특성을 가진 유닛이 있으면 프리팹이 있고 프리팹 에셋이어야 한다.
- **SSOT:** 가시밭 칸 3D 외형의 원본은 `ThornFieldVisual` 한 곳이다. 특성의 이름·아이콘·설명(툴팁)은 `TraitVisual_ThornField`에 그대로 둔다(다른 사실). `Unit_CactusCannon` 키 하드코딩과 Mortar의 가시밭 필수 검사가 사라지고, CactusCannon 공격 연출은 공격만 맡는다.
- **비용과 조율(구현 전 확인):**
  1. **`IViewQuery` 확장:** 이 인터페이스를 직접 구현한 테스트 가짜 4개(`ArtifactPopupConfigTests`·`LobbyInputTests`·`LobbyViewSystemTests`·`UnityStageInputTests`)의 컴파일이 깨진다. 각 한 줄을 고치는데, 앞 3개는 주로 호진님 파일이라 **착수 전에 호진님께 알린다.**
  2. **원자적 반영:** 표 에셋이 없으면 `DataBaseRegistry.GetDataBase`가 "Database of type … not found"를 던져 DB 생성·부팅 전체가 실패한다. 그래서 **코드, 표·항목 에셋, `ViewGroup.asset` 등록은 한 커밋**에 들어가야 한다.
  3. **`ViewGroup.asset` 충돌:** 표 목록 YAML을 호진님·용근님이 최근에 모두 고쳤다(10-12·10-13). 동시에 표를 추가하면 충돌하기 쉽다.
  4. **스테이지별 가시밭 모양은 넣을 수 없다.** 설정이 하나뿐이기 때문이며, 레일도 같은 제약이다. 원인별 모양은 Core 결과에 원인이 없어 어느 안도 불가능하다. 필요해지면 키 있는 행으로 바꾸는 이전이 따로 필요하다.
  5. **부수 수정:** `CombatDisplayTables` 생성자 인자 추가, 테스트의 직접 생성 3곳(`CombatSceneEventPresenterTests` 1·`TerrainOperationalDisplayTests` 2), DB-SO 설계 문서는 갱신하지 않는다(사용자 결정 2026-10-14: 그 문서는 첫 이전의 표 5개만 다루고 이후 표를 싣지 않는다. 이 표의 스펙 원본은 이 D12다).
- **단점이 아닌 것(확인함):**
  - 프리팹 메모리: 지금도 DB의 CactusCannon 공격 연출 SO가 같은 프리팹을 참조해, 로딩 범위가 같다.
  - 전투마다 만드는 보관 GameObject(`FieldThorns_Inactive`): 지금도 DB에 CactusCannon 외형이 있으면 출전 유닛과 무관하게 항상 만든다.
- **순서(세부는 계획 R4.3a~d):** 어느 단계에서도 부팅·Combat 진입·DB 검증이 깨지지 않게, 옛 `thornPrefab`은 마지막에 지운다.
  1. **새 표 추가(R4.3a, 한 커밋):** 아래를 함께 넣는다. `CombatSceneDisplay`와 옛 `thornPrefab`은 아직 그대로다.
     - 표 코드(데이터·SO·표 SO·조회·`IViewQuery`·View DB Factory·서비스 키)
     - 테스트 가짜 4개
     - 표·항목 에셋과 `ViewGroup.asset` 등록
     - 항목의 프리팹은 옛 참조와 같은 에셋(`CombatAttack_CactusCannon.asset`의 `thornPrefab`, guid `09fc9d774997b0349b84c5298e5ceca1`)이다.
     - 에셋은 Claude가 YAML로 쓰거나 사용자가 에디터에서 만든다. 착수 때 정한다.
  2. **연결 확인(R4.3b, 사용자 에디터):** 새 항목의 프리팹이 옛 참조와 같은지, DB 생성·Combat 진입이 그대로인지 확인한다. 확인 전에는 3으로 넘어가지 않는다.
  3. **사용처 이전·옛 필드 제거(R4.3c):** 아래 순서로 한다. 완료 시 가시밭 칸 외형 원본은 `ThornFieldVisual` 한 곳이다.
     - `CombatDisplayTables`에 가시밭 설정을 더한다.
     - `CombatSceneDisplay`가 그 설정으로 `FieldThornPresenter`를 만들게 하고, `Unit_CactusCannon` 조회를 지운다.
     - 그 뒤 `CombatAttackPresentationSO.thornPrefab`·Mortar 필수 검사·`CombatAttackPresentationData.ThornPrefab`을 지운다.
  4. **테스트 갱신·실행(R4.3d):** 아래 표의 테스트와 Mortar·가시밭 생성·소비·재사용 회귀를 돌린다.
- **Unity 주의:** SO 코드에서 필드를 지우면, 그 에셋을 다시 저장할 때 옛 참조 값이 사라진다. 그래서 2에서 새 표의 연결을 확인한 뒤에만 3에서 지운다.

  | 테스트 | 할 일 |
  |---|---|
  | `CombatAttackPresentationSettingsTests` | `MortarSettings_RequireSeparateThornPrefab`은 고치는 것이 아니라 삭제하거나 반대 검증(Mortar가 가시밭 프리팹을 요구하지 않음)으로 바꾼다. `thornPrefab` 설정·비교 사례를 정리한다 |
  | `CombatMortarAnimationTests`, `CombatMortarPresentationTests` | `thornPrefab` 설정을 지우고, 가시밭 Presenter 생성 경로를 `ThornFieldVisual` 기준으로 맞춘다 |
  | `CombatMortarAssetTests` | 옛 위치(`_settings.ThornPrefab`) 조회를 `ThornFieldVisual` 기준으로 바꾼다 |
  | `DataBaseAssetTests` | `Factory_PreservesExistingViewTables`에 새 조회를 더한다. 가시밭 특성 유닛이 있으면 프리팹이 있고 프리팹 에셋인지 대조하는 검사를 더한다 |
  | `CombatSceneEventPresenterTests`, `TerrainOperationalDisplayTests` | `CombatDisplayTables` 직접 생성 3곳에 가시밭 설정 인자를 더한다 |
  | `ArtifactPopupConfigTests`, `LobbyInputTests`, `LobbyViewSystemTests`, `UnityStageInputTests` | `IViewQuery` 가짜에 새 조회 속성을 더한다(R4.3a에서 함께) |

## 3. Phase

| Phase | 결정 | 주요 파일 | 확인 |
|---|---|---|---|
| R0 | D1·D2 | `OrderedCombatPresentation`, `CombatSceneEventPresenter`, `ICombatMovementPresenter.cs` 주석, 테스트 2파일 | MSBuild, 관련 EditMode, 보스·낭떠러지 재생 |
| R1 | D3·D4 | `RuntimeSystem/Common/CleanupErrors.cs`(새), `CombatDisplayTables`·`CombatDisplayServices`(새), `CombatViewSystem`, `CombatSceneDisplay` 생성자, 테스트 생성 호출 | MSBuild, 전투 View EditMode 전체 |
| R2 | D5~D9 | `CombatSceneEventPlayback`·`ICombatEventPlayback`·`CombatDevLayerPanelBinding`(새), `CombatSceneDisplay`, `OrderedCombatPresentation`, `ICombatBattleDisplay`, `FakeDisplay` | MSBuild, 전투 View EditMode 전체, D8 근거 없음 항목의 새 회귀, 재생 |
| R3 | D10 | `CombatAttackPlayback`·`CombatUnitMotionPlayback`(새), `CombatSceneEventPresenter`, `FieldUnitPresenter.TryGetBallistaWeapon` | MSBuild, 공격·적 연출·기차 테스트, 무기 풀 재대여·Tick 집합 정리 회귀, 재생 |
| R4 | D11(·D12) | D11 후속: 삭제된 Timeline 시스템·Factory 등록·오류 정리 연결 제거. D12: `ThornFieldVisual` 표·`IViewQuery`·View DB Factory·서비스 키·공격 연출 SO/Data, `CombatDisplayTables`·`CombatSceneDisplay`, 표·항목 에셋·`ViewGroup.asset`, 관련 테스트 | D11 후속: Boot 밤 진입 회귀·전투 View·수명 정리 EditMode. D12: `DataBaseAssetTests`·Mortar·가시밭 회귀 |

각 Phase가 끝나면 멈추고 보고한다. 보고에는 실습 워크시트와 다음 Phase 상세 설명을 붙인다.

## 4. 원칙 점검

| 항목 | 점검 |
|---|---|
| SSOT | 사건 표시 정책(D1)은 `OrderedCombatPresentation` 한 곳. 표시 의존성 목록(D4)은 묶음 한 곳. 정리 목록(D8)은 묶음 한 번 정의. Ballista 무기의 주인은 `FieldUnitPresenter` 현재 대여 한 곳이고 ID 캐시를 두지 않는다(D10). 가시밭 칸 3D 외형 원본은 D12 구현 시 `ThornFieldVisual` 한 곳(특성 이름·아이콘·설명은 `TraitVisual`에 그대로). 삭제된 Timeline UI의 보임 상태와 호출은 D11 후속에서 함께 폐기한다 |
| S | 표시 Facade / 사건 재생 조정 / 공격 재생 / 이동 재생 / 개발 패널을 나눈다. 삭제된 UI를 위한 시스템과 정리 책임은 남기지 않는다 |
| O | 새 사건은 경고 후 통과(D1), 새 표는 묶음과 표시만 수정(D4), 새 공격 종류는 `CombatAttackPlayback` 한 곳 |
| L | 숨은 계약 제거(D6), 해제 뒤 규칙 고정(D9) |
| I | 소비자는 지금처럼 좁은 계약만 받는다. `ICombatEventPlayback`은 재생 쪽 소비자(`OrderedCombatPresentation`)만 쓴다 |
| D | 조정자는 표시 구현의 개별 의존성을 모른다(D4) |
| FW | 기존 Buffer·서비스·씬 수명 계약 유지. D11 후속은 사용하지 않는 Timeline 모듈 등록·씬 콜백·정리 연결을 제거하며 다른 UI의 `IUISystemService` 경로(FW-008·009)는 유지한다 |
| Core | Core 파일 변경 없음. 새 사건 3종은 공개 Contracts를 읽기만 한다 |
| §27 | R단계에서는 Factory·전략 등록·새 계층을 만들지 않고 묶음·분리 클래스를 사용했다. 후속 전투 씬 전용 Factory의 도입 방향은 D4의 후속 변경 안내를 따른다. 범용 Factory·전략 등록 도입으로 확대하지 않는다 |

## 5. 범위 밖 기록

- **오디오(사용자 결정으로 제외):** BGM 프리셋 결정이 `CombatViewSystem.PresentCompletion`(1)·`CombatSceneDisplay.Refresh`(밤 3·낮 2, 결과마다)·`CombatSceneEventPresenter.PlayAudio`(CombatEntered 3·NightEnded 2) 세 곳에 있다. 유닛별 효과음 프리셋은 `EntityKey` switch 하드코딩이다(기획 값이므로 View DB 원본 후보). 현강님과 정리할 때 참고한다.
- **VR6:** `ReadDisplayValues`의 `GetTimeline` Core 직접 조회는 Core 타임라인 계약 인수 뒤 교체한다.

## 6. 위험

- R2·R3은 연출 수명(버전 번호·늦은 콜백·실패 정리)을 옮긴다. 옮길 때 순서를 바꾸지 않고, 기존 연출 테스트(공격·적 연출·기차 접촉·정리)를 Phase마다 돌린다.
- R1·R2는 테스트 생성 호출을 바꾼다. 테스트의 검증 내용은 바꾸지 않는다.
- Unity 실행은 사용자가 한다. MSBuild 통과와 Unity Test Runner 통과를 보고에서 구분한다.
