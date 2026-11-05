# 전투 View 평가 — 검증된 문제 목록 (2026-11-04)

> [평가 보고서](2026-11-04-combat-view-evaluation.md)의 부록이다. 검증을 통과한 문제 301개(중간 29·낮음 272)를 기준별로 적는다. 기준 판 `e70ad3d6`, 코드 변경 없음.

- 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `S/` = `Assets/ProjectL/Script/Systems/`.
- ID는 합치기 단계의 번호다(`C5-U0123`처럼 `U`가 붙은 것은 합치기에서 따로 남았다가 검증을 지난 원시 문제). 기준이 검증에서 바뀐 것은 지금 기준 절에 있다.
- 검증 표기: 중간 이상 후보는 증거·반론 두 검증자(갈리면 판정자)의 판정, 낮음 후보는 묶음 검증자 하나의 판정이다.
- `기반 한계`로 표시한 것은 감점하지 않았다.

## C1 SSOT — 중간 9 · 낮음 51

### C1-05 [중간] 환경 원본 '보임'(Active && IsKnownToPlayer)·분류·장애물 칸 판정이 표시·입력 7곳에 따로 있음

- 위치: `V/Display/Map/CombatEnvironmentFacts.cs:9-24`, `V/Display/Map/FieldTrailPresenter.cs:141-142`, `V/Display/Map/ObjectViewSet.cs:42-52`
- 내용: CombatEnvironmentFacts는 '공용 판단'이라 하면서 IsKnownToPlayer만 주고, Active와 합친 '보여 줄지' 식은 FieldTrailPresenter.IsShown·ObjectViewSet.Render·TerrainViewSet.IsShownFact·CombatEnvironmentText·CombatDustScreenHoverCells·CombatHazardLookup이 각자 적는다. Trail·TerrainViewSet 주석은 각자 '유일한 판단/한 곳'이라 적는다. 분류 소속 검사, 'Object 분류와 원본 형식이 다르면 예외'(같은 문구), '살아 있고 칸 점유한 장애물' 판정도 EnvironmentText·HazardLookup·ObjectViewSet에 사본이 있다.
- 실패·변경 시나리오: 공개 정책이 바뀌어(예: 꺼졌지만 공개된 원본은 흐리게, 안개·관찰자 조건) CombatEnvironmentFacts만 고치면 Active를 직접 보는 자리는 그대로다. CombatHazardLookup만 빠지면 그리지 않은 지뢰의 위험 범위가 호버에 나오고, EnvironmentText가 빠지면 그리지 않은 흔적·지형이 설명창에 나온다.
- 검증 근거(증거 검증자): CombatEnvironmentFacts.cs:9-24는 스스로 '공용 판단'이라 하지만 IsKnownToPlayer만 준다. 그래서 'Active && IsKnownToPlayer' 식이 6곳에 따로 적혀 있다: FieldTrailPresenter.cs:142, ObjectViewSet.cs:52, TerrainViewSet.cs:224-225, CombatEnvironmentText.cs:53, CombatDustScreenHoverCells.cs:33, CombatHazardLookup.cs:41. 제목의 '7곳'은 Facts까지 센 숫자로, 실제 사본은 6곳이다. 'Object 분류와 원본 형식이 다릅니다' 예외는 ObjectViewSet.cs:49-51, CombatEnvironmentText.cs…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S07, S11, S12

### C1-11 [중간] 도약 구간 끊김 규칙이 CombatLeapPlan과 레인 BeforeEvent에 따로 있고 이미 다름

- 위치: `V/Presentation/Movement/CombatLeapPlan.cs:58-83`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:303-320`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:131-136`
- 내용: 끊는 사건(행동 시작·출처 열기·끌어오기, 도약 유닛 사망·이탈·재배치·복귀·앵커 변경)이 CombatLeapPlan.Build와 CombatUnitMotionPlayback.BeforeEvent에 각자 적혀 있다. 계획은 모든 유닛의 재배치·복귀에서 닫고 레인은 도약 유닛만 본다. 끌어오기도 두 곳에 `is CombatPullEvent`로 직접 적힌다.
- 실패·변경 시나리오: 새 중단 사건을 레인 EndLeap 조건에만 더하면 계획은 구간을 닫지 않아 다음 중간 걸음에서 `_continuedLeap`이 비어 BeginStep이 InvalidOperationException을 던지고 이동 도중 F1로 멈춘다.
- 검증 근거(증거 검증자): 도약 구간을 끊는 규칙이 두 곳에 따로 적혀 있다. CombatLeapPlan.cs:60과 CombatUnitMotionPlayback.cs:305에 `IsActionStart || OpensAttackSource || is CombatPullEvent`가 똑같이 있고, 사망·이탈·재배치·복귀·앵커 변경 조건도 CombatLeapPlan.cs:77-81과 CombatUnitMotionPlayback.cs:312-315에 다시 적혀 있다. 하지만 '이미 다르다'는 차이는 지금은 해가 없다. 계획은 레인보다 넓게 끊는다(모든 유닛의 Relocated·Returned, 다른 유닛의 걸음이나 강제 이동에서도 닫음, CombatLeapPlan.cs:77·92-97). 그래서 레인이 구간을 더 오래 들고 있어도…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L05, S20

### C1-14 [중간] 표시 캐릭터 사본 둘: 목표 강조는 결과 시작 스냅숏, 선택 강조는 보인 사실

- 위치: `V/Display/CombatSceneDisplay.cs:71`, `V/Display/CombatSceneDisplay.cs:66-70`, `V/Display/CombatSceneDisplay.cs:282-317`
- 내용: CombatSceneDisplay가 Refresh 때 Buffer 스냅숏을 `_displayedCharacters`로 든다. 선택 강조(ActiveActor)는 CombatShownFacts(사건마다 갱신)로 다시 그리지만, 목표 강조(ObjectiveTarget)와 호버 원점 찾기는 스냅숏을 쓴다. '지금 화면의 유닛 칸'이 두 사본에서 나온다.
- 실패·변경 시나리오: 결과 재생 중 목표 보스가 죽거나 이동·넉백되면 유닛 모델과 ActiveActor는 새 상태를 따르는데 ObjectiveTarget 층은 결과 끝 Refresh까지 옛 칸(시체 칸)에 남는다.
- 검증 근거(증거 검증자): CombatSceneDisplay.cs:286은 결과 끝 Refresh 때만 `_displayedCharacters` 스냅숏을 바꾸고, 목표 강조는 그 스냅숏으로만 그린다(:316, :376-391). 선택 강조는 CombatSelectionState.cs:19가 CombatShownFacts를 읽고, CombatShownFacts.Render가 사건마다 `_rendered`를 부른다(CombatShownFacts.cs:323-328). 이 `_rendered`는 Factory가 inspection.Refresh에 연결해 두었고(CombatSceneDisplayFactory.cs:200), Refresh는 다시 RefreshInspectionSelection→RenderSelectedActor로 이어진다(:…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L02

### C1-21 [중간] 강조 층의 '원점 열기'·호버 범위 소속이 층 표 밖에 하드코딩됨

- 위치: `V/Display/Field/FieldAttackOutlineOpening.cs:13-27`, `V/Display/Field/FieldAttackOutlineOpening.cs:95-97`, `V/Display/Field/FieldHighlightLayers.cs:109-122`
- 내용: FieldHighlightLayers는 층 속성을 표 한 줄에 두고 정적 생성자로 빠짐을 검사하며 '층을 더하면 열거와 이 표 한 줄을 고친다'고 안내한다. 그러나 열린 변을 갖는 층 목록(AttackRange·PullRange·Affected·MoveRange·ActiveActor)과 IsHoverRange 4층은 FieldAttackOutlineOpening에 따로 적혀 검사받지 않는다.
- 실패·변경 시나리오: 새 호버 범위 층을 열거와 표에 추가하면 정적 검사는 통과하지만 Of()가 default를 돌려줘 그 층의 채우기·테두리가 원점 쪽 변이 닫힌 채 조용히 다르게 그려진다.
- 검증 근거(증거 검증자): 코드는 주장대로다. FieldHighlightLayers.cs:109-122의 표와 주석은 "층을 더하면 열거와 이 표 한 줄을 고친다"고 안내하고, 정적 생성자(153-197)도 표만 검사한다. 그런데 원점 쪽 변을 여는 층 목록은 FieldAttackOutlineOpening.cs:15-16·18-19에, 호버 범위 4층은 95-97에 따로 하드코딩돼 있고 이를 검사하는 곳이 없다. 호버 범위 층 소속은 CombatAttackFeedback.cs:117-131의 tone→층 대응에도 따로 있어서, 같은 사실이 두 곳에 있는 셈이다. 지금 데이터로는 잘못 그려지는 곳이 없다. HazardRange는 IsHoverRange에서 빠져 있지만, CombatUnitRangeHover.cs:224를 거쳐 Comba…
- 검증: 증거 낮음 · 반론 중간 · 판정 중간; 찾은 검토자 L01, S03

### C1-22 [중간] 정보창 HP 막대 규칙(분모·스냅·보호막 채움)이 UnitHUD 사본

- 위치: `V/Display/Hud/CombatUnitInfoView.cs:78-81`, `V/Display/Hud/CombatUnitInfoView.cs:297-329`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs:263-278`
- 내용: CombatUnitInfoView가 분모 Max(max, hp+shield), 스냅 조건, 보호막 채움, shieldFill.enabled 규칙을 UnitHUD와 줄 단위로 같게 다시 적었다(주석 'HUD와 같이').
- 실패·변경 시나리오: UnitHUD에서 보호막 분모나 스냅 규칙을 바꾸면 같은 유닛 선택 시 필드 HUD 막대와 정보창 막대의 HP 비율·보호막 길이가 달라진다.
- 검증 근거(증거 검증자): CombatUnitInfoView.cs:81의 분모 `Math.Max(_maxHealth, hp+shield)`는 UnitHUD.cs:109와 글자 그대로 같다. 297-312의 스냅 조건(`!_hasHealth || _maxHealth != next || !isActiveAndEnabled`, 분모가 바뀌면 Snap)은 UnitHUD.cs:265-275와 같고, 322-328의 보호막 채움·`shieldFill.enabled = _currentShield > 0`도 UnitHUD.cs:334-340과 같다. 주석 '// HUD와 같이'(309)는 일부러 맞춰 적은 사본이라는 뜻이다. 공유하는 것은 HealthFillAnimation뿐이고 판정식은 두 벌이며, 컴파일 단계에서 둘을 묶어 주는 것이 없…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S05

### C1-38 [중간] 약한 지반 진입·붕괴 소리 프리셋을 코드 숫자로 박아 DB 설정 값을 무시

- 위치: `V/Playback/Support/CombatEventSounds.cs:156-158`, `V/Playback/Support/CombatEventSounds.cs:178-186`, `V/Playback/Support/CombatSoundPresets.cs:15-18`
- 내용: 설정 표에 weakGroundCollapseSoundPreset(-1이면 소리 없음, 기본 -1)이 있고 CombatSoundPresets.WeakGroundCollapse()가 답하지만 호출자가 없다. CombatEventSounds는 붕괴에 2, 진입에 0/1을 코드에서 직접 만든다.
- 실패·변경 시나리오: 기획자가 설정 표의 붕괴 프리셋을 -1이나 3으로 바꿔도 전투에서는 계속 2번 소리가 난다. 기본값 -1인데 운영에서 소리가 나 표로 동작을 예측할 수 없다.
- 검증 근거(증거 검증자): CombatEventSounds.cs:158은 붕괴 순간에 `CombatSoundCue.Effect(SfxEvent.WeakTerrian, 2)`를 코드 숫자로 낸다. 설정 표 CombatPresentationSettingsDataSO.cs:187-189(weakGroundCollapseSoundPreset, 툴팁 "-1이면 소리 없음")와 CombatSoundPresets.cs:81 `WeakGroundCollapse()`는 있지만, 저장소 전체를 grep해도 `WeakGroundCollapse()` 호출자는 없다. 운영 자산 CombatPresentationSettings_Default.asset:81 값은 -1이다. 그래도 CombatEventTypeTable.cs:163이 내는 붕괴 순간은 Comba…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S16

### C1-42 [중간] 곡사 애니메이션이 CombatAttackMotion의 공통 수명 규칙을 상속 없이 따로 구현

- 위치: `V/Presentation/Attack/CombatAttackMotion.cs:9-31`, `V/Presentation/Attack/CombatAttackMotion.cs:47-165`, `V/Presentation/Attack/CombatMortarAnimation.cs:15-46`
- 내용: CombatMortarAnimation이 _begun/_cancelled/_released/_cleanupDone/_ticking/_version 깃발과 HandleUnavailable·Cleanup·RequireFinite를 따로 든다. 이미 어긋남: 비유한 위치에 기반은 InvalidOperationException, 곡사는 ArgumentOutOfRangeException; 쓸 수 없음 검사도 위치가 다르다.
- 실패·변경 시나리오: 표시가 사라졌을 때의 처리(취소 순서·Tick 중첩·정리 예외 모양)를 CombatAttackMotion에서만 고치면 곡사(CatusCannon)만 옛 정책으로 다른 오류·순서로 정리된다.
- 검증 근거(증거 검증자): CombatAttackMotion.cs:47-165에는 한 번만 시작, Tick 중첩 금지, 쓸 수 없으면 cancelOwner 뒤 Cancel, Release 멱등, Cleanup 예외 묶기, Fail→Cancel→AggregateException으로 이어지는 공통 수명 정책이 있습니다. 같은 폴더의 직선·근접 공격은 이를 상속합니다(CombatLinearAttackAnimation.cs:14, CombatMeleeAttackAnimation.cs:13). 반면 CombatMortarAnimation.cs:15-46, 74-158, 218-290은 상속하지 않고 같은 깃발과 절차를 다시 적었습니다. 이미 어긋난 곳도 있습니다. 유닛 위치가 유한하지 않을 때 기반은 InvalidOperationExcepti…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S17

### C1-57 [중간] 특성 → 상태 근거 표가 Core의 특성 효과 규칙을 코드에 다시 적음

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs:38-48`, `V/Playback/Support/CombatTraitStateEvidence.cs:48-80`, `V/Playback/Support/CombatTraitFeedbackPlan.cs:135-139`
- 내용: CombatTraitFeedbackProfiles가 '가속 = HeldAttackBonus 변화', 'CleanSweep = AttackIncreaseStatus 부여', 'ReconSquad·HuntersMark = WeaknessExposure 부여'를 상수 표로 들고 판독기가 Core 상태 타입을 직접 찾는다. TraitActivationVisual 검사 문구도 같은 사실을 문자열로 적는다. Core가 발동-상태 연결을 주지 않는 한계는 있으나 흡수를 범위 안 사본 표로 한 몫이다.
- 실패·변경 시나리오: Core에서 CleanSweep 공격력 부여를 다른 상태로 바꾸면 DB 검사는 통과하고 CombatAttackGrantEvidence.Matches가 늘 false라 StateApplied 줄의 이펙트·텍스트가 매번 빠진다.
- 검증 근거(증거 검증자): 증거는 코드와 맞다. CombatTraitFeedbackProfiles.cs:38-48에 'CleanSweep → TraitAttackGrant', 'ReconSquad·HuntersMark → WeaknessExposure'가 적혀 있고, CombatTraitStateEvidence.cs:53-79의 판독기는 Core 상태 타입(AttackIncreaseStatus·WeaknessExposureStatus·AccelerationTraitStatusSnapshot.HeldAttackBonus)을 직접 찾는다. 그래서 Core의 CleanSweepTraitRule(TraitAttackGrant 부여)이 바뀌면 View 표도 함께 고쳐야 하는 사본이 실제로 있다. 다만 그런 일이 생겨도 조용히 묻히지는 않는다.…
- 검증: 증거 낮음 · 반론 중간 · 판정 중간; 찾은 검토자 S25

### C3-01 [중간] 토네이도 도착 칸을 Core가 주는 DestinationCells 대신 View가 Travel·Footprint로 다시 계산

- 위치: `V/Input/Assist/CombatTornadoDestinationCells.cs:13-40`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoAreaFact.cs:21-42`, `V/Input/Assist/CombatEnvironmentDescriptions.cs:56-76`
- 내용: Core TornadoAreaFact는 실제 이동(TornadoGimmickRule)과 같은 TornadoGeometry.Travel로 Destination·DestinationCells를 이미 공개한다. CombatTornadoDestinationCells.Collect는 이를 쓰지 않고 기믹 번호→정의 순번 규칙(GimmickId-1)과 SourceId로 멤버 정의를 다시 찾아 Travel(…, WarningDirection ?? Zero)·Footprint를 다시 부른다. 정의 찾기 규칙은 CombatEnvironmentDescriptions에도 한 벌 더 있다. 정의를 못 찾으면 예외를 던지는 추가 실패 경로도 생긴다.
- 실패·변경 시나리오: Core가 토네이도 이동 계산(막힘 처리·방향 기본값 등)을 바꾸면 TornadoAreaFact.DestinationCells는 함께 바뀌지만 View 사본은 옛 인자로 계산해, 클릭해 고정한 도착 영역이 실제 도착 칸과 달라진다. 기믹 정의 순서나 SourceId 규칙이 바뀌면 Core 사실에는 값이 있어도 Member()가 InvalidOperationException을 던져 Tick 예외(F1)로 이어진다.
- 고칠 방향(검토자 제안): Warning 단계이고 클릭 칸을 포함하는 영역의 area.DestinationCells를 중복 없이 모으기만 한다.
- 검증 근거(증거 검증자): CombatTornadoDestinationCells.cs:22-27은 정의를 다시 찾아(31-40, GimmickId-1 색인, SourceId 대조) TornadoGeometry.Travel·Footprint를 다시 부른다. 그런데 Core TornadoAreaFact.cs:33-38이 같은 Travel로 DestinationCells를 이미 공개하고, 같은 state.ReadTornadoAreas(CombatStateData.cs:23)로 이 fact를 받는다. 따라서 View가 Core 규칙을 흉내 낸 사본이다. 지금 결과는 Core와 같다. TornadoGimmickState.cs:32-33이 Warning ⇔ LockedDirection 있음을 강제하므로, `WarningDirection ?? Z…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L01, S11

### C1-01 [낮음] 연결 정리 순서가 Detach·StopInput·OnInputClosed에 세 벌로 손으로 적힘

- 위치: `V/CombatViewSystem.cs:745-781`, `V/CombatViewSystem.cs:787-803`, `V/CombatViewSystem.cs:929-937`
- 내용: Detach(745-781)와 StopInput(787-803)이 같은 6단계(준비 취소 → 확인창 닫기 → 진입 연출 취소 → 재생 Abort → 연결 입력 정지 → 입력 Release)를 각자 적고, Detach만 UnbindRoot·표시 반환을 끼운다. OnInputClosed(929-937)는 일부를 세 번째 변형으로 다시 적는다. 준비·진입 연출은 CombatSceneDisplay가 자기 정리 목록에서도 취소하므로 연결 수명 정리 목록이 사실상 여러 벌이다.
- 실패·변경 시나리오: 연결 수명 자원(예: 진입 연출 뒤 튜토리얼 대기)을 Detach에만 넣고 StopInput에 빠뜨리면 F1 오류 정지 뒤 그 대기가 취소되지 않아 콜백이 돌거나 대여·구독이 씬 이탈까지 남는다. 한 곳만 순서를 바꾸면 F1 정지와 정상 분리의 정리 순서가 달라진다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L06, S01

### C1-02 [낮음] CombatSceneDisplay 정리 목록이 _fieldBound·Install 실패·Clear·Release로 갈려 이미 항목이 다름

- 위치: `V/Display/CombatSceneDisplay.cs:206-210`, `V/Display/CombatSceneDisplay.cs:262-265`, `V/Display/CombatSceneDisplay.cs:393-398`
- 내용: Release 주석(408)은 '소유 자원 해제는 이 목록 한 곳에서만 정의한다'지만, Map 표시기(objects·terrain·trails·tornado·rockfall·haboob) Clear 묶음이 Install catch(262-265)와 Clear(393-398)에 두 번, Release(400-415)에 Release 변형으로 또 나열된다. Clear에 있는 _hud.Clear가 Release엔 없고, Install catch에는 _ghost.Hide·_units.Release·_hud.Clear·HideActionsCore가 없다.
- 실패·변경 시나리오: 새 기믹 표시기를 Clear에만 넣고 Install catch나 Release를 빠뜨리면, 설치가 RefreshCore 도중 실패했을 때 일부 그린 인스턴스가 남거나 씬 연결 해제 뒤 GameObject·이펙트가 회수되지 않는다. 반대로 Release에만 넣으면 전투가 사라진 경로의 Display.Clear() 뒤 화면에 남는다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L01, L12, S02

### C1-03 [낮음] '필드 위 살아 있는 유닛' 판정식이 전투 View 약 15곳에 따로 있고 정의가 이미 갈림

- 위치: `V/Input/CombatCharacterQueries.cs:26-32`, `V/Input/CombatCharacterQueries.cs:41-62`, `V/Input/ActionButtons/CombatDeploymentConfirmation.cs:51-53`
- 내용: `Area == Field && !Health.IsDead`(곳에 따라 Owner == Player, Anchor.HasValue까지)를 입력(CombatCharacterQueries·CombatPathInput.FindActor·CombatActionAvailabilityTracker.HasSelectedActor·배치/턴 종료 확인창·덱 호버), 표시(FieldUnitPresenter·CombatSceneDisplay.RenderOccupiedLayer·Prewarm), 이펙트 출처 셋(Status·OwnedTrait·AttackMode), 재생(CombatShownFacts.IsOnField·CombatLeapPlan)이 각자 적는다. CombatShownFacts.IsOnField는 '한 곳'이라 주석하지만 private이고, 공용 CombatCharacterQueries는 술어를 주지 않으며 Tracker는 이미 있는 FindPlayerInArea도 쓰지 않는다. CombatShownFacts·FieldUnitPresenter는 Anchor(와 사망 유지)를 보고 IsPlaced·이펙트 출처는 보지 않으며, FindAnyAt은 칸이 없으면 Anchor 비교로 대신한다.
- 실패·변경 시나리오: Core가 '필드 위지만 Anchor 없음/행동 불가' 상태나 새 CharacterRuntimeArea를 더하면 FieldUnitPresenter·CombatShownFacts는 몸 없음으로 건너뛰는데, 이펙트 출처는 지속 이펙트를 원하고(bodyOf null로 Sync마다 재시도), 경로 입력·행동 가능 조회는 행위자로 받고, 턴 종료 확인창은 '행동하지 않은 아군'으로 센다. 15곳 중 하나라도 빠지면 표시·입력·이펙트가 같은 유닛을 다르게 본다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L01, S02, S10, S12, S13, S18

### C1-04 [낮음] 진입 적 수집이 RenderFacts의 '그려지는 유닛' 조건을 따로 적고 어긋나면 예외

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:349-362`, `V/Display/Unit/FieldUnitPresenter.cs:398-409`, `V/Presentation/Entry/CombatBoardEntryPresentation.cs:80-81`
- 내용: CollectEntryEnemies는 Enemy·Field·Anchor·!IsDead를 직접 다시 판정한 뒤 표시가 없으면 InvalidOperationException을 던진다. 원본 그리기 조건은 RenderFacts의 건너뛰기 조건(사망 유지 예외 포함)이다. 덱 호버는 Field && !IsDead만 본다. 이미 그린 표시 목록에 묻게 하면 사본이 없어진다.
- 실패·변경 시나리오: RenderFacts 조건이 바뀌면(새 영역·숨김) 진입 수집이 표시 없는 적을 찾아 설치가 예외로 멈춘다.
- 검증: 묶음 낮음; 찾은 검토자 S08, S19

### C1-06 [낮음] 튜토리얼 '실습 목표 대기' 판정을 View가 다시 조립

- 위치: `V/Input/Assist/CombatBlockedInputReport.cs:25-34`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Tutorial/TutorialFactsReader.cs:37-43`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Tutorial/TutorialFacts.cs:107-111`
- 내용: CombatBlockedInputReport.IsPracticeGoalPending이 Running && Practice && 같은 전투 && WaitReason==PracticeGoal을 직접 조립한다. 튜토리얼 쪽은 TutorialFactsReader·TutorialFacts.IsSessionBattle('같은 전투 판정은 여기 한 곳')로 같은 조건을 가진다. View 사본에는 BattleInstanceId != 0 조건이 없다.
- 검증: 묶음 낮음; 찾은 검토자 S11

### C1-07 [낮음] 거절 안내 문구 두 벌, 'InvalidMainCharacter' 사유만 원문 리터럴

- 위치: `V/Input/Assist/CombatActionNotice.cs:29-33`, `V/Input/Assist/CombatActionNotice.cs:49-56`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs:27`
- 내용: '공격 대상이 없거나 공격할 수 없는 위치입니다'가 AttackRejected와 OnResult에 따로 적혀 있다. 다른 사유는 nameof로 묶는데 InvalidMainCharacter만 문자열 리터럴로 Core 사유와 비교한다.
- 검증: 묶음 낮음; 찾은 검토자 S11

### C1-08 [낮음] 배치 진입이 Core의 '메인 배치' 규칙을 다른 식별 방식으로 다시 판정

- 위치: `V/Input/ActionButtons/CombatDeploymentConfirmation.cs:69-86`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs:22-27`, `V/Input/Assist/CombatActionNotice.cs:52-53`
- 내용: CombatDeploymentConfirmation.CheckMainPlacement는 참가 목록 MainId → OwnedId로 찾은 캐릭터가 IsPlaced(Field ∧ !IsDead)인지 보고 제출 전에 막는다. Core EnterCombat은 OwnershipRole == Main으로 같은 검사를 하고 InvalidMainCharacter로 거절하며, CombatActionNotice.OnResult가 그 거절에 같은 안내를 이미 띄운다.
- 실패·변경 시나리오: Core가 진입 조건을 바꾸면(예: 예비 칸 메인 허용) View 사전 검사는 Core가 받아 줄 진입을 '메인캐릭터가 배치되지 않았습니다!'로 계속 막는다. Core에만 조건이 늘면 View는 확인 없이 제출하고 거절을 받는다.
- 검증: 증거 중간 · 반론 낮음 · 판정 낮음; 찾은 검토자 L01, S10

### C1-09 [낮음] CombatInputPolicy 함수마다 바탕 조건을 따로 나열해 겹침·누락이 생김

- 위치: `V/Input/CombatInputPolicy.cs:50-65`, `V/Input/CombatInputPolicy.cs:81-84`, `V/Input/CombatInputPolicy.cs:118-124`
- 내용: '같은 조건식을 복제하지 않습니다'라 하지만 GetDesiredLayer·PassesCommon·CanSelectUnit·CanShowEnvironmentHover·CanMoveCamera·CanRequestAbandon이 HasBattle ∧ IsBattleDisplayed ∧ ¬IsTerminal(대부분 ¬IsEntryPresentationPending·¬IsInputClosed)을 각자 적는다. 진입 연출 대기는 다섯 자리에 따로 있고 IsInputReady도 이미 포함한다.
- 실패·변경 시나리오: 입력을 닫는 새 사실(예: 연출 일시정지)을 더할 때 여섯 함수 중 하나를 빠뜨리면 닫힌 동안 그 기능만(카메라 이동·환경 설명) 계속 동작한다.
- 검증: 묶음 낮음; 찾은 검토자 S10

### C1-12 [낮음] 진입 켜짐 조건이 PrepareDisplay와 Prepare에 두 벌

- 위치: `V/Presentation/Entry/CombatBoardEntryPresentation.cs:64-75`, `V/Presentation/Entry/CombatEntryDeferredDisplay.cs:53-76`
- 내용: `_settings.Enabled && _tiles.Count > 0`가 보류 시작과 진입 시작에 따로 있고, 보류 해제(RevealBoard)는 진입 보드 단계에서만 불린다.
- 실패·변경 시나리오: 한쪽에만 조건을 더하면 보류는 시작됐는데 진입은 시작되지 않아 _boardDeferred가 계속 참이 되고 토네이도·낙석·하붑·선로·유지 이펙트가 전투 내내 숨는다(예외 없음).
- 검증: 묶음 낮음; 찾은 검토자 S19

### C1-13 [낮음] 보드·선로 등장의 Body·Tick·Cancel 골격과 낙하 곡선이 파일마다 사본

- 위치: `V/Presentation/Entry/CombatBoardReveal.cs:12-19`, `V/Presentation/Entry/CombatBoardReveal.cs:66-115`, `V/Presentation/Entry/CombatEntryRailwayAppearance.cs:13-20`
- 내용: CombatBoardReveal과 CombatEntryRailwayAppearance가 같은 Body·Tick·Cancel(복원→비우기→ThrowIfAny)을 따로 가지고, 선로와 Nexus가 낙하 식 Height*(1−t²)를 각자 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C1-15 [낮음] 선택 ID·국면 사본을 UnitHUDLayer·CombatUnitInfo가 각자 들고 밀어 넣기 4곳에 의존

- 위치: `V/Display/Unit/UnitHUDLayer.cs:27-32`, `V/Display/Unit/UnitHUDLayer.cs:46-71`, `V/Display/CombatSceneDisplay.cs:286-311`
- 내용: 선택 원본 CombatSelectionState._selected와 별도로 UnitHUDLayer._selectedEntity가 있고, CombatSceneDisplay가 네 곳에서 SetSelectedEntity로 밀어 넣는다(InspectUnit은 SelectCell 경로와 중복). 국면도 CombatSelectionState._stage와 CombatUnitInfo._stage가 각자 기억한다.
- 실패·변경 시나리오: 선택을 바꾸는 새 길(예: 사망 뒤 다음 아군 자동 선택)이 SetSelectedEntity를 빠뜨리면 선택 HUD 정보 패널이 옛 유닛에 남는다.
- 검증: 묶음 낮음; 찾은 검토자 L02

### C1-16 [낮음] HUD 보임을 FieldUnitView·UnitHUDLayer 두 곳이 쓰고 HUD 기억 무효화가 다섯 길에 흩어짐

- 위치: `V/Display/Unit/FieldUnitView.cs:40-47`, `V/Display/Unit/FieldUnitView.cs:102-132`, `V/Display/Unit/UnitHUDLayer.cs:203-239`
- 내용: FieldUnitView.SyncHudActive가 HUD 보임과 `_hudFacts` 기억을 함께 관리하는데, UnitHUDLayer.Begin/EndPreparation도 SetOwnerVisible을 직접 쓰고 OwnerWasVisible 사본으로 되돌린다. 기억 무효화는 주석의 다섯 길 목록과 OnHudLayerHidden 계약으로 세 클래스가 나눠 지킨다.
- 실패·변경 시나리오: FieldUnitView를 거치지 않는 숨김 길이 늘면 기억이 남아 다음 그리기가 HUD를 다시 쓰지 않고, 다음 체력 변화가 애니메이션 없이 튄다.
- 검증: 묶음 낮음; 찾은 검토자 L02, S09

### C1-17 [낮음] 공용 Hover 패널 사용처를 두 소유자가 그림자 상태와 숨김 번호 폴링으로 각자 추정

- 위치: `V/Input/Assist/CombatEnvironmentHover.cs:28-39`, `V/Input/Assist/CombatEnvironmentHover.cs:104-115`, `V/Input/Assist/CombatEnvironmentHover.cs:200-208`
- 내용: CombatEnvironmentHover(_shown·_observedHideVersion 등)와 CombatUnitInfoView(_abilityHoverShown·_abilityHoverVersion 등)가 같은 패널의 현재 사용자를 따로 추정한다. 환경 호버는 자기 _shown만 보고 UnHover해 UnitInfo 패널을 닫고, UnitInfo가 LateUpdate에서 다시 연다.
- 실패·변경 시나리오: 필드 설명이 뜬 칸에서 UnitInfo 특성 칸으로 옮기면 환경 호버가 특성 설명을 닫았다가 UnitInfo가 다시 연다. 세 번째 사용처가 번호 규칙을 어기면 다른 사용처 설명을 닫거나 다시 열지 못한다.
- 검증: 묶음 낮음; 찾은 검토자 L02, S11

### C1-19 [낮음] 특성 확인(Preview) Factory가 운영 표시 조립·필드 포트·정리 목록을 따로 베껴 이미 어긋남

- 위치: `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:61-108`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:119-137`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:130-178`
- 내용: 필드 표시 묶음(objects·terrain·trails·tornado·rockfall·railway·thorns·HUD 층·units Configure·CombatFieldEventPorts)과 정리 목록을 CombatSceneDisplayFactory와 CombatTraitPreviewFactory가 각각 만든다. 이미 다르다: 운영은 외형 진행·재생 중·시작 막기에 terrain+tornado를 이름 인자로 넣지만 Preview는 terrain만 14개 위치 인자로 넘기고, Preview는 BindStructureDamage를 부르지 않으며, rockfall 기본값(`?? true`)도 다르다.
- 실패·변경 시나리오: 특성 확인 시나리오에 구조물 피해 사건이 들어가면 Preview에는 숫자 창구가 묶이지 않아 InvalidOperationException이 난다. 운영에 새 필드 외형을 포트에 더해도 Preview는 진행·대기하지 않고, 같은 형식 인자를 바꿔 넘겨도 컴파일돼 운영과 다른 재생을 보고 판단하게 된다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L02, L04, S24

### C1-23 [낮음] 렌더 부품마다 Mesh·Color 핸들 생성과 Color→Mesh→Destroy 해제 순서를 따로 구현

- 위치: `V/Display/Field/FieldMovementPathLine.cs:197-279`, `V/Display/Field/FieldTrajectoryLine.cs:84-128`, `V/Display/Field/FieldTrajectoryLine.cs:183-222`
- 내용: FieldMovementPathLine.Part·FieldTrajectoryLine·FieldArrowhead·FieldCellFill·FieldKnockbackMarkers가 GameObject·MeshFilter 생성, Register, 핸들 null 검사, 해제 순서와 실패 모으기를 각자 되풀이한다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C1-24 [낮음] FieldView.OnDestroy가 Release의 부품 해제 사슬을 따로 복사

- 위치: `V/Display/Field/FieldView.cs:444-468`, `V/Display/Field/FieldView.cs:470-504`
- 내용: Release와 OnDestroy가 '궤적 → 넉백 표지 → 표시기' 순서를 try/finally로 각자 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C1-25 [낮음] 렌더 레이어 번호 20을 프로젝트 레이어 설정과 별개로 코드에 적음

- 위치: `V/Display/Field/FieldRenderLayers.cs:3-8`, `V/Display/Map/FieldRockfallPresenter.cs:166`, `V/Display/Map/FieldThornPresenter.cs:307`
- 내용: FieldRenderLayers.BothRendering = 20은 TagManager의 BothRendering 자리를 손으로 옮긴 상수다. 레이어 표가 바뀌면 낙석·가시 표시가 경고 없이 다른 레이어에 놓인다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C1-26 [낮음] 스테이지 외형 회전 표 찾기·검증 규칙이 Object·Terrain 표시에 두 벌

- 위치: `V/Display/Map/FieldObjectPresenter.cs:71-72`, `V/Display/Map/FieldObjectPresenter.cs:95-113`, `V/Display/Map/FieldTerrainPresenter.cs:196-197`
- 내용: `stageKey > 0 && TryGetStageEnvironmentVisual` 찾기와 회전 Source Id 이중 루프 검증이 두 Presenter에 거의 같은 모양으로 있다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C1-27 [낮음] '재생 중이면 Destroy, 아니면 DestroyImmediate' 파괴 정책이 파일마다 복사됨

- 위치: `V/Display/Map/FieldObjectPresenter.cs:168-179`, `V/Display/Map/FieldTerrainPresenter.cs:318-325`, `V/Display/Map/FieldRailwayPresenter.cs:457-459`
- 내용: Map 표시기 다섯 곳에 같은 분기가 있고, 범위 안에 같은 일을 하는 도우미도 여럿 있다.
- 검증: 묶음 낮음; 찾은 검토자 S06, S07

### C1-28 [낮음] ObjectViewSet·TerrainViewSet이 대여 맞추기·실패 되돌리기 정책을 두 벌로 가짐

- 위치: `V/Display/Map/ObjectViewSet.cs:42-93`, `V/Display/Map/ObjectViewSet.cs:107-132`, `V/Display/Map/TerrainViewSet.cs:53-86`
- 내용: 분류 HashSet 채우기, _pass로 빠진 원본 고르기, 예외 시 Clear 후 AggregateException, Clear·TryGet·Remove가 열쇠 타입만 다르게 거의 줄 단위로 겹친다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C1-29 [낮음] 흔적 겹침 높이 0.002가 테두리 층 높이 상수를 참조하지 않고 다시 적힘

- 위치: `V/Display/Map/FieldTrailPresenter.cs:30-31`, `V/Display/Field/FieldOutlineRenderer.cs:44`
- 내용: 주석은 '칸 테두리 높이 규칙과 같은 크기'라 하지만 FieldOutlineRenderer.LayerHeightStep을 참조하지 않는다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C1-30 [낮음] 체력 숫자 보임 조건이 SetEntryHudHidden·ShowUnitHud에 다른 모양으로 두 번

- 위치: `V/Display/Unit/FieldUnitView.cs:74-79`, `V/Display/Unit/FieldUnitView.cs:113-118`
- 내용: `!_showUnitHud && !hidden`와 `!show && !_entryHudHidden`가 같은 규칙을 각자 계산한다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C1-31 [낮음] 공격 조준 호버 표시 정리 목록이 ClearShotPreview와 ResetRangeAnswer 람다에 두 벌

- 위치: `V/Input/Interaction/AttackTargetingInteraction.cs:563-579`, `V/Input/Interaction/AttackTargetingInteraction.cs:589-610`
- 내용: 영향 칸·막힌 벽·넉백·궤적 지우기가 ClearHoverDisplay/ClearShotPreview와 ResetRangeAnswer(int)의 세대 검사 람다 7개에 따로 있고, 주석이 '여기에 더하면 람다에도 더한다'고 손 동기화를 요구한다.
- 실패·변경 시나리오: 새 호버 그림(예: 관통 표시)을 ClearShotPreview에만 더하면 호버 중 ESC·공격 버튼 끄기·턴 종료로 Exit할 때 그 그림이 필드에 남는다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S12

### C1-33 [낮음] 공격 방향→칸 변화 8방향 표를 Core에서 베껴 둠

- 위치: `V/Input/Interaction/AttackTargetingInteraction.cs:546-561`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/AppointShapeCalculator.cs:113-127`
- 내용: TryGetDirectionVector가 Core AppointShapeCalculator.GetDirectionStep과 같은 표를 다시 적는다. Core Contracts가 방향 벡터를 공개하지 않아 생긴 사본이다.
- 검증: 묶음 낮음; 찾은 검토자 S12

### C1-34 [낮음] 경로 입력의 정리 순서와 표시 실패 정리 규칙이 여러 벌

- 위치: `V/Input/MovePath/CombatPathInput.cs:181-193`, `V/Input/MovePath/CombatPathInput.cs:494-515`, `V/Input/MovePath/CombatPathInput.cs:340-358`
- 내용: 정리 순서(상호작용 → 범위 → 편집 종료 → 초안 → 지정 표시)가 Detach의 RunCleanup과 CancelInputs 대리자 사슬에 따로 있고, '표시 콜백이 던지면 같은 세대일 때 LeaveMoveMode' 규칙이 세 곳에 복사돼 있다.
- 검증: 묶음 낮음; 찾은 검토자 S13

### C1-35 [낮음] 구조물 표시 자격(적·고유 장애물 아님)을 피격 처리기·HUD가 따로 적어 세 벌이고 이미 다름

- 위치: `V/Playback/Handlers/CombatStructureEventHandler.cs:24-34`, `V/Selection/ICombatInspectionFactsReader.cs:50-53`, `V/Display/Unit/FieldUnitPresenter.cs:434-447`
- 내용: CanInspectStructure('정보창과 HUD가 공유하는 구조물 표시 자격')가 있는데 CombatStructureEventHandler는 `Owner != Enemy || IsNativeObstacleKey`를 직접 쓰고, FieldUnitPresenter HUD는 `Enemy || 장애물`이라는 다른 식을 쓴다. 지금도 고유 장애물은 HUD에는 보이고 피격 숫자·큐는 나오지 않는다.
- 실패·변경 시나리오: 아군 본부나 중립 구조물을 대상으로 넓혀 CanInspectStructure만 고치면 정보창·HUD에는 나오고 체력도 줄지만 피격 숫자·큐는 나오지 않는다(또는 반대).
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L01, S14

### C1-36 [낮음] 표식 사건 반영이 디버프 6종 필드 목록을 손으로 다시 나열

- 위치: `V/Playback/Handlers/CombatFactEventHandler.cs:42-51`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterDebuffState.cs:14-30`
- 내용: CombatHuntersMarkEvent 반영에서 CharacterDebuffState를 선택 인자 6개로 다시 만든다. 모두 기본값 null이라 Core에 새 디버프가 생겨도 컴파일 오류 없이 이 경로에서 값이 사라진다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C1-37 [낮음] '공격 그룹 0 = 근원 미지정' 판정이 세 곳에 숫자 비교로 따로 있음

- 위치: `V/Playback/Ordering/CombatConcurrentGroups.cs:240-253`, `V/Playback/Ordering/CombatEventSubjects.cs:59-63`, `V/Presentation/Attack/CombatHitFollowUps.cs:82`
- 내용: CombatConcurrentGroups.TryGetKey·CombatEventSubjects.TryGetKey·CombatHitFollowUps가 각자 AttackGroup 0 비교를 적고, CombatAttackSource에는 그 판정 멤버가 없다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C1-39 [낮음] 가속 특성 '완료 이동 칸 수'를 View가 사건을 세어 다시 계산(자발 이동 판정도 사본)

- 위치: `V/Playback/Support/CombatTraitMovementCount.cs:12-28`, `V/Presentation/Movement/CombatMovementKinds.cs:18-24`, `V/Playback/Support/CombatTraitFeedbackPlan.cs:97-108`
- 내용: CombatTraitMovementCount가 사건을 거꾸로 돌며 `Moved ∧ Anchor 바뀜 ∧ Agency != Forced` 사건 수를 센다. 자발 이동 판정 원본 CombatMovementKinds.Of를 쓰지 않고, Core AccelerationEffectRule은 context.Distance로 누적한다. 피드백 계획은 이 값을 Core DistanceSpent와 대조해 어긋나면 횟수를 0으로 만든다.
- 실패·변경 시나리오: 한 Moved가 두 칸 이상 옮기거나(Distance > 1) Core가 누적 조건을 바꾸면 Core 보너스와 몸 위 'N칸'이 어긋난다. 새 이동 원인을 CombatMovementKinds.Of에만 반영하면 DistanceSpent 불일치 경고가 나고 횟수 글자가 사라진다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L01, S16

### C1-40 [낮음] '다음 행동 경계까지' 사건 훑기 멈춤 조건이 세 곳에 조금씩 다르게 있음

- 위치: `V/Playback/Support/CombatTraitFeedbackPlan.cs:124-128`, `V/Playback/Support/CombatTraitFeedbackPlan.cs:164-169`, `V/Playback/Support/CombatTraitMovementCount.cs:18-22`
- 내용: 상태 사건 찾기·방향 찾기·이동 칸 세기가 IsActionStart·Returned·같은 특성 발동 멈춤 조건을 각자 적고, 방향 찾기만 Returned를 보지 않는다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C1-41 [낮음] 공격 행위자 몸 자리를 StandingBodyOf와 다른 회전 규칙으로 다시 만듦

- 위치: `V/Playback/Support/CombatAttackStart.cs:146-151`, `V/Playback/Support/CombatShownFacts.cs:377-387`, `V/Playback/Support/CombatShownFacts.cs:394-409`
- 내용: CombatAttackStart.ActorBodyOf는 위치는 대여 반영, 회전은 StandingPoseOf(대여 미반영)에서 받고, CombatShownFacts.StandingBodyOf는 대여 회전을 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C1-43 [낮음] '투사체를 쏘는 종류'를 DB 규칙과 전략 표가 따로 답함

- 위치: `V/Presentation/Attack/CombatAttackPlayback.cs:254-259`, `V/Playback/Support/CombatAttackStart.cs:115-135`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatUnitAnimationRules.cs:122-123`
- 내용: Prewarm과 CombatAttackStart는 DB 쪽 FiresProjectile(Linear||Mortar)을 쓰고, 실제 발사 가능은 전략 표(근접 FireShot 예외·PoolOf null)가 정한다. 둘이 같은지 검사하지 않는다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C1-44 [낮음] 곡사 궤적 미리보기가 공격 처리기의 '클립 길/코드 모션 길' 선택 규칙을 복제

- 위치: `V/Presentation/Attack/CombatMortarStyle.cs:30-54`, `V/Playback/Support/CombatAttackStart.cs:80-85`
- 내용: CombatMortarStyle.TryResolveTrajectoryOrigin이 TryGetAttackClip 유무로 출발점을 고르며, 주석도 CombatAttackStart.TryBegin과 같은 사실을 본다고 동기화 의존을 밝힌다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C1-45 [낮음] 근접 타격 순간 0.55가 같은 파일에 리터럴로 여러 번 흩어짐

- 위치: `V/Presentation/Attack/CombatMeleeAttackAnimation.cs:36-69`, `V/Presentation/Attack/CombatAttackMotion.cs:167-174`
- 내용: Advance 판정(MeleeDuration * .55f, Render(.55f))과 Render 구간 경계(.55f, /.45f)에 따로 적혀 있다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C1-46 [낮음] 특성 상시 이펙트 찾기 반복문이 두 출처에 복사됨

- 위치: `V/Presentation/Effects/CombatAttackModeEffectSource.cs:61-79`, `V/Presentation/Effects/CombatOwnedTraitEffectSource.cs:33-46`
- 내용: CombatAttackModeEffectSource·CombatOwnedTraitEffectSource가 TraitStatuses 순회 → null·IsAvailable 거르기 → TryGetTraitVisual → AlwaysEffects Condition 거르기를 각자 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S18

### C1-47 [낮음] 보기 크기를 CombatCameraView._size와 CombatCameraWindow._viewLens가 따로 기억

- 위치: `V/Presentation/Cameras/CombatCameraView.cs:27-36`, `V/Presentation/Cameras/CombatCameraView.cs:161-170`, `V/Presentation/Cameras/CombatCameraWindow.cs:39-41`
- 내용: 같은 '지금 보기 크기'와 맵 카메라 목록을 두 곳이 각각 든다. TrySetViewSize가 _viewLens를 먼저 고치고 보내므로 중간 Dispatch 예외 때 두 값이 어긋난다.
- 검증: 묶음 낮음; 찾은 검토자 S18

### C1-48 [낮음] CombatPieceStart에 지연을 더하는 분기가 두 곳에 사본

- 위치: `V/Presentation/Pieces/CombatProjectileCueComposer.cs:44-51`, `V/Presentation/Pieces/CombatCueAnchors.cs:72-78`, `V/Presentation/Pieces/CombatPieceStart.cs:91-110`
- 내용: CombatProjectileCueComposer.AddSound와 CombatCueAnchors.TryStart가 `Kind == AtStart ? AtStart(d+delay) : After(...)`를 각자 다시 짠다. CombatPieceStart에 지연 이동 연산이 없다.
- 검증: 묶음 낮음; 찾은 검토자 S22

### C1-49 [낮음] CombatStandMovePiece만 걸음 값 검사를 설정 Data 한 곳 대신 다른 경로로 함

- 위치: `V/Presentation/Pieces/CombatStandMovePiece.cs:50-53`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:371-383`, `V/Presentation/Pieces/CombatScalePiece.cs:29-30`
- 내용: Rise·Sink·Scale 조각은 CombatPresentationSettingsData.Validate*에 맡기고 설정 Data 문서도 '값 객체도 이 조건을 쓴다'고 하는데, StandMovePiece는 RequireNonNegative·IsDefined로 따로 검사한다.
- 검증: 묶음 낮음; 찾은 검토자 S22

### C1-50 [낮음] 시간 조각 끝 판정식이 조각마다 사본이고 줌 조각 보장이 '같은 식'에 의존

- 위치: `V/Presentation/Pieces/CombatCameraZoomPiece.cs:11-13`, `V/Presentation/Pieces/CombatCameraZoomPiece.cs:33-46`, `V/Presentation/Pieces/CombatFlashPiece.cs:35-49`
- 내용: '_began && _elapsed >= 시간'과 Tick 누적이 Scale·CameraZoom·Flash·PositionSwapDelay 조각에 따로 있고, CameraZoom 문서의 '크기 조각과 같은 프레임에 끝남' 보장은 두 식이 같다는 데 기댄다.
- 검증: 묶음 낮음; 찾은 검토자 S21

### C1-51 [낮음] 전투 화면 Canvas 상수(UI 층 5, 정렬 50/-10, 1920x1080)가 Factory 두 곳에 숫자로 흩어짐

- 위치: `S/Factory/Runtime/InGame/View/CombatBodyTextFactory.cs:27-39`, `S/Factory/Runtime/InGame/View/CombatUnitHUDFactory.cs:15-30`, `V/Display/Unit/UnitHUDProjection.cs:10-12`
- 내용: CombatUnitHUDFactory는 UnitHUDProjection.ReferenceResolution을 읽지만 CombatBodyTextFactory는 같은 값을 new Vector2(1920, 1080)으로 직접 적는다. 두 Canvas의 정렬 관계와 layer = 5도 표 없이 숫자다.
- 실패·변경 시나리오: 기준 해상도를 UnitHUDProjection에서만 바꾸면 몸 위 글자 Canvas는 옛 비율로 남아 HUD와 크기 비율이 어긋난다.
- 검증: 묶음 낮음; 찾은 검토자 L01, S24

### C1-52 [낮음] CombatEventPlaybackFactory가 settings와 그 파생값 values·transitionSettings를 함께 받음

- 위치: `S/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs:23-34`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:103`
- 내용: 두 호출자 모두 같은 presentation에서 파생값을 만들어 원본과 함께 넘겨, 같은 사실이 인자 셋으로 들어오고 다른 출처를 섞을 수 있다.
- 검증: 묶음 낮음; 찾은 검토자 S24

### C1-53 [낮음] 유닛 레벨 결정 규칙(적 레벨, 소유 기록 있으면 그 레벨)이 정보창과 UnitCardUI에 따로 있음

- 위치: `V/Selection/CombatInspectionInfo.cs:91-95`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardUI.cs:252-255`
- 내용: CombatInspectionInfo와 UnitCardUI(범위 밖)가 같은 규칙을 다른 소유 기록 원본으로 각자 구현한다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C1-54 [낮음] 결과 화면 스테이지 표기 '깊이-너비+1'을 승리·패배 경로가 각자 조합

- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:129`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:161-166`
- 내용: 승리는 보상 영수증 값으로, 패배는 StageProgress Buffer를 다시 찾아 같은 형식 문자열을 만든다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C1-55 [낮음] 선택 상태와 정보 Presenter가 같은 표시 사실 reader를 따로 주입받음

- 위치: `V/Selection/CombatSelectionState.cs:9-15`, `V/Selection/CombatInspectionPresenter.cs:14`, `V/Selection/CombatInspectionPresenter.cs:49`
- 내용: 주석은 '같은 reader에서 읽는다'고 보장하지만 Factory가 두 곳에 같은 객체를 넘기는 관례에만 기댄다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C1-56 [낮음] 피격 기울기 상한 60이 상수와 Inspector Range에 두 번 적힘

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatUnitReactionPresentationData.cs:11-12`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatUnitReactionPresentationSO.cs:13`
- 내용: MaxHitAngle = 60f가 있고 주석은 'SO 칸 범위와 같다'지만 SO는 Range(0f, 60f)에 숫자를 직접 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S26

### C1-58 [낮음] 수 값 규칙 원본이 있는데 IsFinite·IsNonNegative·클립 이벤트 번호 검사 사본이 따로 있음

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationValueRules.cs:5-29`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatCameraSettingsData.cs:137-141`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipTimings.cs:64-67`
- 내용: CombatPresentationValueRules가 원본이라 하지만 CombatCameraSettingsData·CombatClipTimings·CombatProjectileEasing이 private IsFinite를 다시 두고, 이벤트 번호 검사가 CombatClipShot·CombatClipTimings에 두 벌이다.
- 검증: 묶음 낮음; 찾은 검토자 L01, S25

### C1-59 [낮음] '소리 없음' 상수 NoSoundPreset이 있는데 -1 리터럴을 여러 곳에 다시 적음

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationValueRules.cs:52-53`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationData.cs:45-47`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationSO.cs:73-75`
- 내용: 공격 값 객체 기본 인자, CreateData 근접 분기, 투사체 SO 기본값·Min(-1)이 -1 리터럴을 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C1-60 [낮음] '아직 보이지 않은 사망' 판정이 처리기·동시 사망 입구·소리 표에 따로 있고 조건이 이미 다름

- 위치: `V/Playback/Handlers/CombatCharacterEventHandler.cs:137-142`, `V/Playback/CombatSceneEventPresenter.cs:390-399`, `V/Playback/Support/CombatEventSounds.cs:105-114`
- 내용: 'Kind == Died ∧ Expected 생존 ∧ Final 사망 ∧ 보인 사실에서 아직 생존' 식을 TryBeginDeath·BeginConcurrentDeath·CombatEventSounds.For가 각자 적는다. 소리 표엔 Final.IsDead 검사가 없고, 동시 사망 입구에만 EntityId 일치 검사가 있으며, 기차 접촉 재생에 네 번째 변형이 있다.
- 실패·변경 시나리오: 사망 연출 조건을 바꿔 TryBeginDeath만 고치면 소리 표는 옛 조건으로 반응 없이 소리를 내거나 두 번 낸다. BeginConcurrentDeath 조건만 남으면 기차 동시 사망이 InvalidOperationException으로 F1 정지한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L01

### C1-61 [낮음] 진입 연출의 적 Nexus 선택이 본부 키 식별식을 다시 적음

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:364-375`, `V/Display/Unit/CombatObjectiveDisplay.cs:10-12`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs:28-32`
- 내용: CombatObjectiveDisplay는 '목표 대상은 BattleObjective로만, 키로 고르지 않는다'고 정했는데 CollectEntryNexuses는 Owner == Enemy ∧ Key == Structure_Headquarters로 고른다. 같은 식이 Core EnterCombat에도 있다.
- 검증: 묶음 낮음; 찾은 검토자 L01

### C3-05 [낮음] Nexus 낙하가 조각 실행기(CombatPieceRun)의 자세 대여·잃음·세대 처리를 따로 구현

- 위치: `V/Presentation/Entry/CombatEntryNexusAppearance.cs:69-124`, `V/Presentation/Entry/CombatEntryEnemyAppearance.cs:56-80`
- 내용: 같은 폴더의 적 등장은 자세 대여·취소·잃음 콜백·세대를 CombatPieceRun+CombatRisePiece에 맡기지만, Nexus 낙하는 TryBeginPose·잃음 콜백·_generation·_unavailable·Tick 예외·RestorePose를 손으로 다시 짠다. 조각 계약에 없는 것은 착지 이펙트 대기뿐이다. 재구현 대상이 범위 안 View 부품이라 대여 수명 규칙 사본 문제(C1) 성격이 더 크다.
- 고칠 방향(검토자 제안): 착지 이펙트 대기를 조각(또는 완료 조건)으로 표현해 CombatPieceRun으로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C6-03 [낮음] MainCamera 대체 정책이 세 곳에 있고 포인터만 Ray마다 Camera.main을 다시 조회

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:111`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:179-188`, `V/Display/Feedback/CombatPointerMapper.cs:33-37`
- 내용: Factory 주석은 'MainCamera 대체 정책은 조립 경계에서 한 번만 적용'한다고 하지만 `root.WorldCamera != null ? root.WorldCamera : Camera.main`이 Factory에 두 번(사건 재생·HUD) 있다. CombatPointerMapper는 지정 카메라 null을 그대로 받아 CurrentCamera()에서 Ray마다 Camera.main으로 다시 대체한다. worldCamera를 비운 씬에서 MainCamera 태그 카메라가 바뀌거나 꺼지면 포인터는 새 카메라로 칸을 고르고 HUD 투영·사건 재생은 조립 때 잡은 옛 카메라를 쓴다. 지정 카메라가 파괴돼도 포인터만 조용히 Camera.main으로 넘어간다.
- 고칠 방향(검토자 제안): Factory에서 출력 카메라를 한 번 정해 지역 변수 하나로 포인터·사건 재생·HUD에 넘기고, CombatPointerMapper의 Camera.main 대체는 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L08, S02

## C2 SOLID 5원칙 — 중간 4 · 낮음 29

### C2-01 [중간] 필드 기믹(토네이도·낙석·하붑·가시·흔적)에 공통 수명 계약이 없어 10여 곳에 이름으로 나열됨(OCP)

- 위치: `V/Display/CombatSceneDisplay.cs:91-99`, `V/Display/CombatSceneDisplay.cs:197-210`, `V/Display/CombatSceneDisplay.cs:264`
- 내용: 기믹 표시에 공통 계약(Render/Clear/Release/Tick)이 없다. CombatSceneDisplay는 기믹마다 필드·생성자 인자·null 검사를 두고, 같은 목록을 정리 목록 세 벌(Install 실패·Clear·Release)과 RefreshCore에 손으로 적는다. CombatEntryDeferredDisplay도 tornado·rockfall·haboob을 필드·Clear·Render에 따로 나열한다. CombatFieldEventPorts는 기믹별 구체 창구를 받고, Factory는 람다 안에서 terrain과 tornado를 손으로 묶는다. 재생 쪽은 처리기 칸 enum·Table.Create switch·Presenter 생성자가 기믹 처리기를 하나씩 만들고, 입력 보조 CombatEnvironmentText는 기믹 상태 형식 검사를 사슬로 잇는다.
- 실패·변경 시나리오: 새 기믹(예: 산사태)을 더하며 CombatSceneDisplay Release 목록에는 넣고 Install 실패·Clear 목록에서 빠뜨리면, 설치 실패나 재설치 때 그 기믹 인스턴스가 남는다. 컴파일러·검사가 잡지 못하고, 한 기능 추가가 Display·Playback·Entry·Input/Assist·Factory·DisplayTables 등 10개 넘는 파일로 번진다.
- 고칠 방향(검토자 제안): IFieldGimmickPresenter(Bind/Render/Clear/Release/TickAppearance) 공통 계약을 두고 Factory가 목록 하나로 조립해, Display·진입 보류 표시·포트가 목록을 돌게 한다.
- 검증 근거(증거 검증자): 증거는 코드와 맞는다. CombatSceneDisplay.cs:95-99·155-157·199-201은 토네이도·낙석·하붑마다 필드, 생성자 인자, null 검사를 따로 둔다. 같은 이름들이 Install 실패 정리(264), Clear(397), Release(414)에 손으로 다시 적혀 있다. CombatEntryDeferredDisplay.cs:15-17·42-44·95-97은 같은 세 기믹을 필드·Begin·RenderNow에 다시 나열한다. CombatFieldEventPorts.cs:33-60은 기믹별 구체 창구를 받는다. CombatSceneDisplayFactory.cs:153-166·194-198은 terrain과 tornado를 람다로 손수 묶고, 38개 위치 인자로 같은 Presenter…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L03, L05, L06, S02, S11, S24

### C2-02 [중간] CombatViewSystem 조정자가 서비스 배선·씬 수명·결과 소비·입력 분배·표면 파생을 모두 맡는 God class

- 위치: `V/CombatViewSystem.cs:79-161`, `V/CombatViewSystem.cs:210-270`, `V/CombatViewSystem.cs:301-416`
- 내용: 1179줄, 필드 30개, BindServices 인자 18개. ① Framework 수명·서비스 배선 ② 로딩 노드·씬 수명 ③ 표시 연결·분리·오류 정지 ④ 결과 소비·후처리 ⑤ 진행 호스트 ⑥ RouteInput의 요청 분기와 정보창 선택 결정(Interaction.Drag·IsAttackActive·IsPathPlanning 직접 조회) ⑦ 매 Tick 가용성·행동 버튼·환경 호버·카메라 파생을 한 클래스가 한다. 협력 객체 10여 개와 입력 재료 16개도 직접 묶는다. 클래스 주석은 '개별 입력 기능을 모른다'지만 입력 기능·연출 타이밍·서비스 배선·씬 수명 어느 것이 바뀌어도 이 파일을 고친다. 외부 호출 뒤 재확인 줄(CanContinue 28곳, ReferenceEquals(_connection…) 16곳)도 이 섞임 때문에 많다.
- 실패·변경 시나리오: 새 입력 소유자(예: 기믹 조준)를 CombatInteraction에 넣어도 SelectPressed 때 정보창 열기를 막는 조건은 RouteInput의 Drag/IsAttackActive/IsPathPlanning 나열에 있어, 이 목록을 고치지 않으면 새 모드 클릭 때 정보창이 함께 열린다. 카메라·환경 호버 추가 때도 Tick·RouteInput·Sync*를 함께 고쳤고, 그 사이 외부 호출 뒤 재확인 한 줄을 빠뜨리면 분리된 연결에 이어 쓰는 경로가 생긴다.
- 고칠 방향(검토자 제안): 결과 소비·입력 분배·표면 동기화를 연결 수명 객체(또는 CombatSceneInput)로 옮기고, '선택 가능' 판정은 Interaction.IsInputOwnerActive 같은 질의 하나로 둔다. 조정자는 수명과 단계 순서만, 재료 조립은 Factory 묶음으로 받는다.
- 검증 근거(증거 검증자): 증거 줄을 열어 보니 주장이 맞다. CombatViewSystem.cs는 1179줄이다. 이 한 클래스가 서비스 배선(PostBindService 188-207, 인자 18개 BindServices 210-270), 씬 수명(Initialize·OnCombatSceneLoaded·ReleaseScene 273-466), 연결·분리·정지(483-827), 결과 소비(831-911), Tick 파이프라인과 끝의 버튼·호버·카메라 파생(301-416), 입력 분기(RouteInput 1086-1141)를 모두 맡고, 입력 재료 16개도 직접 묶는다(CreateInput 692-717). CanContinue는 31곳, ReferenceEquals(_connection…)는 16곳에서 다시 확인한다. failure…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L03, S01

### C2-03 [중간] CombatSceneDisplay가 표시 조정·배경음 정책·결과 화면·행동 버튼·피드백 위임·HUD 층 파괴까지 맡는 God 퍼사드

- 위치: `V/Display/CombatSceneDisplay.cs:49-110`, `V/Display/CombatSceneDisplay.cs:130-169`, `V/Display/CombatSceneDisplay.cs:282-317`
- 내용: 협력 객체 30여 개 필드, 생성자 인자 38개, ICombatBattleDisplay(하위 계약 12개)·ICombatUnitInspection·ICombatGimmickRangeFeedback을 한 클래스가 구현해 공개 멤버가 60개 가까이 된다. 필드 Build·지형/기믹 그리기, 국면→배경음 정책, 완료 화면·완료 뒤 배경음, 유닛 준비·Prewarm 계산, 정보창 선택 동기화, 행동 버튼, 포인터 매핑·커서 스냅, 고스트·범위·공격·넉백·이동 피드백 위임(462-785의 약 45개는 _isReleased 확인 뒤 넘기기만 하는 통과 메서드), 카메라 복원, 유닛 HUD 층 GameObject 파괴를 함께 맡는다. ICombatBattleDisplay가 이것을 한 계약으로 묶어 조정자가 필요 없는 표면까지 받는다.
- 실패·변경 시나리오: 입력 담당이 피드백 하나(예: 기믹 범위 툴팁)를 더하려면 CombatSceneDisplay 생성자·필드·Release 목록·Factory의 38인자 호출을 함께 고치고, 같은 때 표시 담당이 배경음 규칙(예: 밤 보스전 BGM)을 고치면 같은 RefreshCore·생성자에서 충돌한다. 같은 타입 위치 인자(terrain/objects 등)를 바꿔 넣어도 컴파일이 통과한다.
- 고칠 방향(검토자 제안): 배경음 정책은 CombatSoundPresets 쪽 작은 객체로, 입력 피드백 하위 계약은 실제 협력 객체가 직접 구현·노출하게, 필드 그리기와 완료·행동 버튼 표면은 별도 표시 객체로 나눠 Display에는 수명·순서만 남긴다.
- 검증 근거(증거 검증자): 증거 줄의 내용이 주장과 맞다. CombatSceneDisplay.cs:49-110에는 협력 객체 필드가 30개 넘게 있고, 130-169는 38인자 생성자다. 이 클래스 하나가 국면별 배경음 정책(290-292), 결과 화면과 완료 뒤 배경음(417-430), Prewarm 개수 계산(325-342), 행동 버튼(441-459), 커서 스냅 설정(493-495), HUD 층 GameObject 파괴(799-805), 공격·이동·고스트 피드백 통과 메서드 약 45개(462-785)를 함께 맡는다. 그래서 바뀌는 이유가 여럿이다. ICombatBattleDisplay.cs:22-24는 하위 계약 12개를 묶고, 그 운영 구현은 CombatSceneDisplay 하나뿐이다. 입력 Factory는 CombatSc…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L03, L06, S02

### C2-04 [중간] 클립 이벤트 종류가 enum 4개와 대응 switch 여러 곳에 흩어져 이벤트 추가마다 6~8곳 수정(OCP)

- 위치: `V/Presentation/Pieces/CombatClipEventCursor.cs:7-23`, `V/Presentation/Pieces/CombatClipEventCursor.cs:75-81`, `V/Presentation/Pieces/CombatClipPlayback.cs:17-44`
- 내용: 클립 중간 이벤트 하나가 CombatClipEventId(DB)·CombatClipEventKind(커서)·CombatPieceSignal(실행 신호)·CombatCueAnchor(큐 시점) 네 enum에 따로 있다. 대응도 KindOf(Id→Kind), CombatClipPlayback의 이벤트별 Action 필드·생성자 인자·Emit switch, CombatClipPiece.Begin의 Event2/Event3 하드코딩, CombatClipRangedAttack.SignalOf, CombatPresentationCueRules.AnchorOf, CombatCueAnchors.KindOf로 흩어져 있고 Id→Signal은 두 곳에 사본이다.
- 실패·변경 시나리오: 선택 이벤트 Event4를 더하며 CombatClipPlayback·CombatClipPiece의 Event4 콜백 연결 한 곳을 빠뜨리면, SignalOf로 그 신호를 기다리는 발사 항목과 Cue4 큐에 신호가 오지 않아 SkipUnsignalled가 시작 안 한 채 끝난 것으로 처리하고 탄·소리가 조용히 빠진다.
- 고칠 방향(검토자 제안): Id↔Signal↔Anchor 대응을 CombatClipEventId별 표 하나로 모으고, CombatClipPlayback은 Action<CombatClipEventId> 콜백 하나로, CombatClipPiece는 Timings.Events를 돌며 SignalOf로 신호 입구를 만든다.
- 검증 근거(증거 검증자): 주장대로 코드가 되어 있다. 클립 중간 이벤트 하나가 enum 네 개에 따로 있고(CombatClipTimings.cs:8-13의 Id, CombatClipEventCursor.cs:7-23의 Kind, CombatPieceStart.cs:56-60의 Signal, CombatCueAnchor의 Cue2·Cue3), 이를 잇는 switch도 여러 곳에 흩어져 있다(CombatClipEventCursor.cs:75-81 KindOf, CombatClipPlayback.cs:19-23·34-43·80-88의 이벤트별 Action 필드·인자·switch, CombatClipRangedAttack.cs:44-50 SignalOf, CombatPresentationCueRules.cs:225-231 AnchorOf,…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 S21

### C2-05 [낮음] 걸음 레인이 도약을 모양 추상 밖 곁길로 처리함(구체 모양 형 검사·표 우회)

- 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:127-155`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:343-358`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:446-461`
- 내용: CombatStepStyle·CombatStepStyles·CombatMovementKind 문서는 '레인은 어떤 모양인지 모른다·모양은 표 한 곳이 정한다·새 종류는 값·모양·표 한 줄만 더한다'고 약속한다. 그런데 BeginStep이 leapStep 인자로 CombatLeapStepStyle을 직접 만들어 표가 고른 모양을 덮어쓰고, StartStep·FinishStep이 `_style is CombatLeapStepStyle`로 시작 배치 유지·즉시 회전·대여 유지를 고른다. 레인은 걸음·접촉·끌어오기·방향 복귀·도약 이어가기·효과음 상태 필드 33개를 손으로 맞추고, Cancel이 필드 15개를 직접 비운다.
- 실패·변경 시나리오: 문서대로 다칸 이동 종류(예: 장애물 건너뛰기 2칸 호)를 새 종류·모양·표 한 줄로 넣으면, 공중 배치 유지·중간 즉시 회전·대여 유지가 레인의 is CombatLeapStepStyle 분기에만 있어 새 모양은 칸마다 배치를 반환하고 회전을 기다리며 중간 칸에 착지한다. 결국 BeginStep·StartStep·FinishStep·IsActive·Cancel을 함께 고쳐야 한다.
- 고칠 방향(검토자 제안): 구간 동작(이어가기·즉시 회전·시작 배치)을 모양의 가상 멤버로 올리고, 도약 모양 선택도 모양 표에서 하게 한다.
- 검증: 증거 중간 · 반론 낮음 · 판정 낮음; 찾은 검토자 S20

### C2-06 [낮음] 상태 설명 문구가 상태 형식·수명·출처 종류 switch 사슬이라 새 상태마다 여러 곳 수정, 빠지면 실행 중 예외

- 위치: `V/Display/Unit/CombatStatusDescriptionFormatter.cs:21-73`, `S/Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs:16-26`, `V/Display/Unit/CombatEffectFormatters.cs:43-57`
- 내용: CombatStatusDescriptionFormatter.Describe가 source.Status 형식 switch(7종), StatusEffectLifetimeKind switch(7종), EffectOwnerKind switch를 갖고 모두 `_ => throw`다. 새 상태 종류마다 CombatUnitStatusFactory 형식 등록 목록, 이 switch 두 곳, 상태 외형 표를 함께 고쳐야 한다. CombatEffectFormatters는 등록 항목의 인자 수만 검사하고 모든 StatusEffectKey 등록 여부는 검사하지 않는다.
- 실패·변경 시나리오: Core에 새 상태 기록 형식이 추가되고 View가 형식 등록만 하고 Describe의 case를 빠뜨리면, 그 상태가 걸린 유닛을 RenderFacts→RenderUnitHud→AddTo→Describe로 그릴 때 InvalidOperationException으로 재생이 멈춘다(F1). 등록을 빠뜨려도 같은 지점에서 Get이 던진다.
- 고칠 방향(검토자 제안): 형식별 세부 문구·인자 변환을 CombatEffectFormatter 등록 하나에 함께 두고, 조립 때 모든 StatusEffectKey 등록 여부를 검사한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S08

### C2-07 [낮음] FieldUnitPresenter(893줄)가 풀·사실 그리기·HUD 내용 조립·접촉 몸 기하·준비 대여·진입 수집을 모두 맡음

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:40-105`, `V/Display/Unit/FieldUnitPresenter.cs:183-330`, `V/Display/Unit/FieldUnitPresenter.cs:463-557`
- 내용: 한 MonoBehaviour가 (1) 종류별 풀·미리 만들기·계측 (2) RenderFacts (3) HUD 내용 조립(병과 아이콘·이름·공격력·보호막·아이템 문구·상태 칸·우클릭 해제 연결·구조물 정보) (4) 히트 타이밍용 몸 기하(BodyOf·BodyCenterOf·PlaceBodyBox·TrackBodyPresence 등) (5) 로딩 준비 대여 (6) 진입 연출 대상 수집 (7) 계약 3개 구현과 연출 값 캐시를 함께 가진다. HUD 문구·몸 상자 식·풀 수명 어느 것을 바꿔도 같은 파일과 같은 풀 상태를 다룬다.
- 실패·변경 시나리오: HUD 상태 칸에 새 항목(예: 격파 목표)을 넣으려 RenderUnitHud를 고치면, 같은 메서드가 PrepareSelectedUnit(준비 대여)에서도 불리고 RememberHudFacts 캐시·풀 반환 초기화와 얽혀 HUD 작업자가 풀·준비 수명 경로까지 읽고 검증해야 한다. 몸 상자 식을 바꾸는 히트 타이밍 작업도 같은 파일 풀 코드와 충돌한다.
- 고칠 방향(검토자 제안): 몸 기하를 별도 클래스로, HUD 내용 조립(RenderUnitHud·ItemStatus·구조물 정보)을 HUD 작성기로 떼고 Presenter는 풀·대여·사실 그리기 조정만 남긴다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S08

### C2-08 [낮음] FieldUnitView가 HUD·체력 숫자·Transform·대여 5종·외형·클립 준비를 한 클래스에 모아 채널 추가가 여러 곳으로 흩어짐

- 위치: `V/Display/Unit/FieldUnitView.cs:26-61`, `V/Display/Unit/FieldUnitView.cs:331-340`, `V/Display/Unit/FieldUnitView.cs:455-500`
- 내용: 728줄 한 클래스가 HUD 붙이기·HUD 사실 기억, 체력 숫자, 사실 위치·회전 적용, 자세·서는 자리·크기·플래시·클립 대여 5종(중첩 클래스 5개), Renderer 찾기·외형 준비, 클립 채널 준비·해제, 곡사 무기 조회를 맡는다. 대여 채널 하나를 더하려면 칸 필드·Begin(시작 조건 복사)·Reset·ReleasePresentation의 순서 있는 목록·중첩 대여 클래스·ICombatUnitPresentationTarget 멤버와 명시적 구현·창구 메서드를 함께 고쳐야 한다. 계약도 이 묶음을 그대로 옮겨 20개 멤버가 됐다.
- 실패·변경 시나리오: 새 연출 채널(예: 외곽선 층)을 더하며 Begin과 중첩 대여 클래스는 만들고 ReleasePresentation에 Reset을 넣지 않으면, 풀 반환·재대여 때 칸이 비워지지 않아 다음 Begin이 '이전 ...이 아직 남아 있습니다' 예외를 내고 재생이 F1로 멈춘다.
- 고칠 방향(검토자 제안): 대여 채널을 '칸 + 복원 함수 + 시작 조건' 등록 목록으로 묶어 ReleasePresentation이 목록을 돌게 하고, HUD 연결·사실 기억은 별도 부품으로 뗀다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S09

### C2-09 [낮음] 범용 유닛 표시 계약에 특정 유닛 무기 구체 타입(CatusCannonWeaponView)이 박혀 계약 사슬로 퍼짐(DIP)

- 위치: `V/Display/Unit/ICombatUnitPresentationTarget.cs:72-73`, `V/Presentation/Attack/ICombatAttackStyleContext.cs:22`, `V/Display/Unit/CombatUnitPresentationWindow.cs:286-296`
- 내용: ICombatUnitPresentationTarget은 모든 유닛 표시의 계약인데 `CatusCannonWeaponView MortarWeapon { get; }`을 요구한다. 이 sealed MonoBehaviour 타입이 CombatUnitPresentationWindow.TryGetMortarWeapon, ICombatAttackStyleContext, CombatMortarStyle, CombatMortarAnimation까지 퍼져, 일반 곡사 전략이 선인장 대포 한 유닛 리그에 묶인다. FieldUnitView는 이 속성을 부를 때마다 GetComponent를 한다(곡사 공격마다 한 번이라 비용은 작음).
- 실패·변경 시나리오: 버킷 구조가 다른 곡사 유닛(투석기)을 더하면 CatusCannonWeaponView가 sealed라 상속할 수 없어 클래스 일반화·이름 변경 또는 계약 다섯 곳의 타입 수정이 필요하다. 컴포넌트가 없는 리그의 프리팹은 TryGetMortarWeapon이 false가 되어 곡사 연출이 시작하지 않는다.
- 고칠 방향(검토자 제안): ICombatMortarWeapon 같은 역할 계약을 Display/Unit에 두고 CatusCannonWeaponView가 구현하게 하며, 유닛 표시는 준비 때 한 번 찾아 둔 참조를 내준다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L03, L11, S09

### C2-11 [낮음] CombatShownFacts가 보인 사실·검사 조회·위치 교환 보류·끌어오기 재작성·유지 이펙트·몸 기하를 한 클래스에 모음

- 위치: `V/Playback/Support/CombatShownFacts.cs:21-65`, `V/Playback/Support/CombatShownFacts.cs:136-219`, `V/Playback/Support/CombatShownFacts.cs:259-296`
- 내용: 480줄 한 클래스가 (1) 검사 UI 조회 계약 ICombatInspectionFactsReader (2) 위치 교환 보류 절차(Hold/Claim/Release/Discard) (3) 끌어오기 도착·추락 표지와 사건 재작성(ForPresentation) (4) 그리기와 유지 이펙트 동기화 (5) 몸·서는 자리 기하 조회를 가진다. 검사 규칙·위치 교환 규칙·유지 이펙트 시점 중 하나만 바꿔도 모든 처리기·재생기가 의존하는 이 파일을 고친다.
- 실패·변경 시나리오: 검사 UI의 구조물 고르기 규칙이나 위치 교환 보류 규칙을 고치는 작업자가 공격 재생·소리·걸음·특성 처리기가 모두 쓰는 FindCharacter·ApplyCharacter 옆 코드를 함께 읽어야 하고, ApplyCharacter 안 _placements.Drop 같은 숨은 부수효과를 놓치기 쉽다.
- 고칠 방향(검토자 제안): 검사 조회 어댑터, 위치 교환 보류 반영기, 몸 기하 조회를 떼고 보인 사실 목록 하나를 참조하게 한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S16

### C2-12 [낮음] FieldRailwayPresenter가 설치·열차 PlayableGraph·이펙트 수명·재생 시계를 한 클래스에서 맡음

- 위치: `V/Display/Map/FieldRailwayPresenter.cs:23-41`, `V/Display/Map/FieldRailwayPresenter.cs:179-267`, `V/Display/Map/FieldRailwayPresenter.cs:313-416`
- 내용: 표시 클래스 하나가 선로·열차 설치/파괴, PlayableGraph 만들기·평가·루트 복원, EffectScope 수명, 재생 시계(Begin/Tick/완료·진행 콜백)와 재진입 방어를 함께 맡고, 방어에 깃발 7개(_isStarting·_isCleaning·_clearRequested·_isClearing·_isTicking·_hasUnreturnedEffects·_version)가 얽혀 있다. 같은 폴더 하붑 표시는 진행을 재생 조각이 맡는데 선로만 표시 안에 재생 시계를 둔다.
- 실패·변경 시나리오: 열차 도착 이펙트를 더하거나 애니메이션 방식을 바꾸려면 Begin·Tick·CleanupPlayback 콜백 앞뒤의 _version·_isStarting·_clearRequested 검사를 모두 지켜야 하고, 새 콜백 뒤 버전 검사 하나를 빠뜨리면 취소된 재생이 해제된 열차를 움직이거나 완료 콜백이 두 번 불린다.
- 고칠 방향(검토자 제안): 재생 시계와 콜백은 Playback 조각으로 옮기고 표시는 설치와 '시각 t의 열차 자세 적용'만 남긴다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S06

### C2-13 [낮음] CombatCharacterEventHandler가 피해 숫자·피격·사망·추락 사망·토네이도 던짐을 함께 맡음

- 위치: `V/Playback/Handlers/CombatCharacterEventHandler.cs:31-75`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:77-135`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:137-240`
- 내용: 339줄·생성자 인자 13개 처리기가 피해 숫자, 피격·사망 조립, 낭떠러지 추락 조립(임시), 토네이도 던짐(들어 올림 조각·폭풍 따라가기·System.Random 씨앗·착지 소리)을 구현한다. 토네이도 하나 때문에 ICombatTornadoField와 난수 상태가, 추락 때문에 CombatFallMotionSettings 인자가 들어왔다. 사건 모양은 Core가 정하지만 처리기 안에서 갈래별 부품으로 나누지 않은 것은 범위 안 구조다.
- 실패·변경 시나리오: 토네이도 던짐(착지 큐·자세 채널)을 바꾸거나 임시 추락 연출을 걷어내려면 피격·사망 조립과 같은 파일·생성자를 고치고, 같은 형식(CombatCameraWindow·CombatEffectWindow 등) 인자가 이어진 Presenter 159 조립 줄도 다시 확인해야 해 피격/사망 경로가 함께 흔들릴 수 있다.
- 고칠 방향(검토자 제안): 토네이도 던짐·추락 사망을 별도 갈래 부품으로 떼어 처리기는 사건 분류와 위임만 한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S14

### C2-14 [낮음] AttackTargetingInteraction이 모드 수명·조준 기하·이유 요청·결과 표시 조율을 한 클래스에 묶음

- 위치: `V/Input/Interaction/AttackTargetingInteraction.cs:29-93`, `V/Input/Interaction/AttackTargetingInteraction.cs:435-485`, `V/Input/Interaction/AttackTargetingInteraction.cs:487-561`
- 내용: 682줄 클래스가 모드 수명·자동 종료, 범위 응답 캐시·막힘 색인, 호버 미리보기 요청 번호, 하붑 거절 이유 2차 요청, 궤적·넉백·모드 대상·체력 예상 표시 조율, 방향 조준 기하(AimFor·NearestDirectionAim)를 모두 가진다. 생성자 인자 11개 중 4개가 표시 의도이고 Factory는 위치 인자로 display를 반복해 넘긴다.
- 실패·변경 시나리오: 공격 모드 표시 하나(예: 사거리 원)를 더하면 필드·생성자·ApplyRangeAnswer·정리 두 목록과 Factory 인자 순서를 함께 고쳐야 하고, 같은 타입(display) 위치 인자를 잘못 끼워도 컴파일이 통과한다.
- 고칠 방향(검토자 제안): 공격 모드 표시 조율을 표시 의도 객체 하나로 묶어 인자를 하나로 줄이고, 조준 해석은 정적 도우미로 뺀다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S12

### C2-15 [낮음] 계약에 없는 능력을 형변환으로 꺼냄(ICombatGimmickRangeFeedback 형식 검사, FieldUnitView 다운캐스트)

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:45-56`, `V/Display/ICombatBattleDisplay.cs:15-24`, `V/Display/CombatSceneDisplay.cs:49`
- 내용: ICombatBattleDisplay는 문서에 '형식 검사로 꺼내 쓰는 계약은 없습니다'라고 적고 구현으로 테스트 가짜도 들지만, CreateEnvironmentHover는 `display is not ICombatGimmickRangeFeedback`이면 예외를 던진다. ICombatGimmickRangeFeedback은 상속 목록에 없고 CombatSceneDisplay만 따로 구현한다. FieldUnitPresenter도 ICombatUnitTargets.ReleaseRental에서 ICombatUnitPresentationTarget을 `(FieldUnitView)target`으로 강제 변환하고 IsRented를 `is FieldUnitView`로 판정한다. 필요한 능력이 계약에 드러나지 않아 컴파일이 아니라 실행 때 확인된다.
- 실패·변경 시나리오: ICombatBattleDisplay만 구현한 다른 표시(문서가 말하는 테스트 가짜·새 표시)로 연결하면 첫 그리기 뒤 CreateSceneInput에서 InvalidOperationException이 나 연결 오류로 멈춘다. 다른 ICombatUnitPresentationTarget 구현을 ReleaseRental에 넘기면 계약을 지켰어도 InvalidCastException이 난다.
- 고칠 방향(검토자 제안): ICombatGimmickRangeFeedback을 ICombatBattleDisplay 상속 목록에 넣거나 CreateSceneInput 인자로 따로 받고, 유닛 대여 반환은 Release를 가진 대여 핸들로 바꿔 구체 변환을 없앤다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L03, S11, S24

### C2-17 [낮음] ICombatInteractionFeedback(멤버 17개)를 소비자마다 겹치지 않는 일부만 씀(ISP)

- 위치: `V/Input/Contracts/ICombatInteractionFeedback.cs:17-60`, `V/Input/Interaction/CombatItemDragInteraction.cs:32-41`, `V/Input/Interaction/CombatHoverTracker.cs:11-22`
- 내용: 배치 Drag는 고스트·원본 7개, 공격 조준은 범위·영향·차단·예상 체력 8개, CombatHoverTracker는 SetHoverCell/ClearHoverCell 2개만 쓰고, 아이템 Drag는 호버 추적기에 넘기기만 한다. 같은 폴더의 궤적·넉백·모드 대상은 '배치·아이템 입력은 이 계약을 모릅니다'로 좁은 계약으로 나눴는데 이 계약만 큰 묶음으로 남아 기준이 어긋난다.
- 고칠 방향(검토자 제안): 고스트(배치)·조준 범위(공격)·호버 칸 계약 셋으로 나눠 각 객체에 필요한 것만 넘긴다.
- 검증: 묶음 낮음; 찾은 검토자 L03, S11

### C2-18 [낮음] 구현 하나뿐인 생성 계약 ICombatSceneFactory·ICombatSceneInputFactory가 정적 Factory로 위임만 하고 구체 타입을 돌려줌(과도한 추상화)

- 위치: `V/Input/ICombatSceneInputFactory.cs:15-19`, `V/Display/ICombatSceneFactory.cs:9-17`, `S/Factory/Runtime/InGame/View/CombatSceneFactory.cs:17-42`
- 내용: 두 계약의 구현은 CombatSceneFactory.Instance 하나뿐이고 시험 대역도 없으며 정적 Factory에 그대로 위임한다. CreateFactsReader는 같은 10인자를 ICombatSceneInputFactory → CombatSceneFactory → CombatSceneInputFactory → new CombatInputFactsReader 네 서명으로 넘기기만 한다. 조정자는 늘 새 객체인 결과에 `?? throw`를 붙이고, 같은 연결에서 읽을 수 있는 HasPending·IsConfirmationOpen을 대리자 둘로 따로 넘긴다. CreateDisplay는 구체 CombatSceneDisplay를 돌려주고 Attach는 계약에 없는 구체 멤버를 써서 계약이 있어도 바꿔 끼울 수 없다.
- 고칠 방향(검토자 제안): CreateFactsReader 계약을 걷고 조정자가 직접(또는 재료 묶음 하나로) 생성하며, 계약을 유지하려면 ICombatBattleDisplay와 준비·진입 연출을 돌려줘 대체 가능하게 한다.
- 검증: 묶음 낮음; 찾은 검토자 L03, S10, S24

### C2-19 [낮음] View 조회 묶음 CombatDisplayTables가 파생 객체를 조립하고 Factory 계층 CombatUnitStatusFactory를 직접 부름(DIP)

- 위치: `V/CombatDisplayTables.cs:16-25`, `V/CombatDisplayTables.cs:58-66`, `S/Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs:10-27`
- 내용: CombatDisplayTables는 주석에 '참조만 들고 있다'고 적지만 생성자에서 CombatUnitStatuses(표시 변환기 9개)·CombatSoundPresets·CombatCueSources를 조립하고, 상태 표는 Factory 네임스페이스의 정적 CombatUnitStatusFactory.Create를 전체 이름으로 부른다. 전투 View 런타임에서 Factory를 부르는 곳은 이 한 줄뿐이고, 조정자는 이 의존을 피하려 생성 계약을 주입받는다('정적 Factory를 직접 부르지 않습니다'). From마다 파생 객체를 다시 만들어 개발 특성 미리보기는 두 번 만든다.
- 고칠 방향(검토자 제안): 파생 객체 조립을 CombatSceneDisplayFactory 쪽으로 옮겨, 조회 묶음은 표 참조만 받게 한다.
- 검증: 묶음 낮음; 찾은 검토자 L04, S01, S24

### C2-20 [낮음] CombatPieceRun이 출처를 쓰지 않는 진입 연출에도 ICombatSourceLifetime을 강요해 '던지는 가짜'를 넘기게 함(ISP/LSP)

- 위치: `V/Presentation/Entry/CombatEntryEnemyAppearance.cs:68-69`, `V/Presentation/Entry/CombatEntryEnemyAppearance.cs:126-135`, `V/Presentation/Pieces/CombatPieceRun.cs:66-100`
- 내용: CombatPieceRun 생성자는 인자 11개를 받고 일반 완료(General)에도 출처 수명을 null 금지로 요구한다. 그래서 진입은 Begin/End/Abort/ReleaseAll을 모두 throw로 구현한 EntrySources.Instance와 자체 프레임 카운터·세대·콜백을 만들어 넘긴다. 출처 계약은 IsSource 완료에서만 불려 지금은 안전하지만, 실행기가 일반 완료 길에서도 출처를 부르게 바뀌면 진입에서만 실행 중 예외가 난다.
- 고칠 방향(검토자 제안): 출처 항목이 있는 조립에만 출처 수명을 요구하도록 선택 인자나 별도 생성 경로로 나눈다.
- 검증: 묶음 낮음; 찾은 검토자 L03, S19

### C2-21 [낮음] 배치 진입·턴 종료 확인창이 같은 수명 구조를 두 쌍으로 복제하고 묶음은 확인창을 세 집계에 하나씩 나열

- 위치: `V/Input/ActionButtons/CombatDeploymentEntry.cs:20-94`, `V/Input/ActionButtons/CombatEndTurnEntry.cs:18-88`, `V/Input/ActionButtons/CombatDeploymentConfirmation.cs:97-111`
- 내용: Entry(IsFaulted·IsInputReady 확인 → CanSubmit → Router.Route + afterSubmit, CancelInputs)와 Confirmation(IsOpen·Respond·Sync·Close)이 두 쌍 거의 같다. CombatSceneInput은 IsConfirmationOpen·CloseConfirmations·SyncConfirmations에서 두 확인창을 하나씩 나열하고, CloseConfirmations는 같은 파일 Stop이 쓰는 CleanupErrors.Try·ThrowIfAny를 손으로 다시 쓴다. 두 벌은 이미 튜토리얼 막힘 보고에서 갈렸다.
- 실패·변경 시나리오: 세 번째 확인창(예: 포기·아이템 해제)을 더하면 Entry·Confirmation 한 쌍을 베끼고 묶음 생성자·세 집계·Factory를 고쳐야 하며, 집계 하나를 빠뜨리면 그 확인창이 열린 동안 입력이 막히지 않거나 분리 때 닫히지 않는다.
- 고칠 방향(검토자 제안): 확인창 진입을 (종류, 판정, 제출, 질문 문구) 값으로 받는 클래스 하나로 두고 묶음은 목록으로 집계한다.
- 검증: 묶음 낮음; 찾은 검토자 S10

### C2-22 [낮음] 처리기 표 Create가 같은 형식 인자 12개 + 칸 switch라 사건 형식마다 서명·switch·조립 줄을 고침(OCP)

- 위치: `V/Playback/Handlers/CombatEventHandlerTable.cs:21-55`
- 내용: 연출이 있는 사건 형식을 하나 더하려면 처리기 칸 enum(CombatEventTypeTable), CombatEventHandlerTable.Create의 서명과 switch, Presenter 조립 줄을 모두 고쳐야 한다(주석이 이 절차를 적어 둠). 인자 12개가 모두 CombatEventHandler 형식이라 잘못된 연결을 형식으로 잡지 못하고(지금은 이름 붙인 인자), 빠뜨린 갈래는 실행 때 예외로 드러난다.
- 고칠 방향(검토자 제안): 칸 enum + switch 대신 처리기를 칸별로 등록하는 표(사전)로 바꾸고 조립 때 빠진 칸을 검사한다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C2-23 [낮음] 사건 형식 표 TypeRow가 위치 인자 15개라 열 추가가 같은 파일 다섯 곳 수정이고 순서 실수가 컴파일됨

- 위치: `V/Playback/Ordering/CombatEventTypeTable.cs:436-455`, `V/Playback/Ordering/CombatEventTypeTable.cs:489-535`
- 내용: CombatEventTypeTable은 경로·처리기 칸·묶음·간격·화면 전환·소리 순간 등 질문 14개를 한 행에 모은다. 열 하나를 더하려면 Row 인자·Wrap 호출·TypeRow 생성자·속성·질의 메서드 다섯 곳을 고친다. 같은 대리자 형식(actor/subject는 Func<CombatEvent,EntityId>, isMovement/opensAttackSource는 Func<CombatEvent,bool>)이 겹쳐 Wrap 순서를 바꿔도 컴파일 오류가 없다. 표 자체는 SSOT로 잘 되어 있다.
- 고칠 방향(검토자 제안): TypeRow를 init 속성(또는 이름 있는 인자)으로 바꿔 Row<T>에서 이름으로 채운다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C2-24 [낮음] CombatEnvironmentHover 한 클래스에 설명 툴팁·호버 영역·토네이도 클릭 고정이 상태 깃발을 공유하며 얽힘

- 위치: `V/Input/Assist/CombatEnvironmentHover.cs:12-39`, `V/Input/Assist/CombatEnvironmentHover.cs:82-155`, `V/Input/Assist/CombatEnvironmentHover.cs:180-198`
- 내용: 필드 24개로 공용 Hover 패널 텍스트·숨김 번호 맞추기, 기믹·먼지 연막 호버 영역, 클릭 고정 토네이도 도착 영역 세 기능을 맡고 _selectionShown·_rangeShown·_cached 깃발을 공유한다. 외부 텍스트 숨김 갈래가 선택 영역을 다시 그리는 등 한 기능을 고치려면 나머지 두 기능의 상태 전이를 함께 읽어야 한다.
- 검증: 묶음 낮음; 찾은 검토자 S11

### C2-25 [낮음] FieldOutlineRenderer(787줄)가 슬롯·높이 분할·겹침 분할·경로 구간 계산·데칼을 한 클래스에서 맡음

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:43-93`, `V/Display/Field/FieldOutlineRenderer.cs:276-352`, `V/Display/Field/FieldOutlineRenderer.cs:382-476`
- 내용: 채우기·경로 띠·고리·데칼 오브젝트는 부품으로 나뉘었지만, 슬롯 순서·색 모으기(Collect), 지형 높이별 핸들(ApplyArea), 겹친 칸 깊이별 핸들(SplitInsets·ApplyInsets), 경로 높이 구간 점·lift 계산(ApplyPath), 데칼 높이 갱신이 남아 있다. 경로 구간 규칙을 바꿔도 테두리 핸들 코드·상태를 함께 다룬다.
- 고칠 방향(검토자 제안): 경로 구간 점 만들기는 경로 부품으로, 높이별 핸들 나누기는 Slot 쪽 작은 부품으로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C2-26 [낮음] CombatAttackPlayback이 수명 관리 외에 접촉 시각 계산·진단 경고·접촉 이펙트까지 맡음

- 위치: `V/Presentation/Attack/CombatAttackPlayback.cs:36-68`, `V/Presentation/Attack/CombatAttackPlayback.cs:293-356`
- 내용: Gate·후속·투사체·Presenter 수명 조정과 함께 몸 조회로 직선 접촉 진행률 계산(_bodies·ContactProgress), 경고 중복 제거(_contactWarnings), 접촉 큐 이펙트 재생(_effects·PlayImpact), 전략 맥락 어댑터를 한다. 접촉 기하·이펙트·Gate 정책이 바뀔 때 모두 이 파일을 고친다.
- 고칠 방향(검토자 제안): 접촉 진행률 계산(몸 조회·경고)과 접촉 이펙트 재생을 작은 협력 객체로 떼어 맥락에 넘긴다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C2-27 [낮음] CombatCueSources가 큐 답 외에 외형 표 전달과 몸 글자 프리팹 수집·검증까지 맡음

- 위치: `V/Playback/Support/CombatCueSources.cs:73-97`, `V/Playback/CombatSceneEventPresenter.cs:120-123`
- 내용: 순간 큐 답 객체가 상태·특성 외형 표를 그대로 내보내고(유지 이펙트 출처 조립이 사용), 몸 글자 프리팹 수집과 누락 예외(BodyTextPrefabs)도 한다. 큐 값·유지 이펙트 출처·텍스트 프리워밍이라는 다른 이유로 바뀐다.
- 고칠 방향(검토자 제안): 표는 조립(Factory)에서 직접 넘기고 프리팹 수집은 CombatBodyTextFactory 쪽으로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C2-28 [낮음] UnitHUDLayer가 투영 외에 선택 패널 UI 동적 등록·열기·정렬까지 맡음

- 위치: `V/Display/Unit/UnitHUDLayer.cs:34-44`, `V/Display/Unit/UnitHUDLayer.cs:52-113`
- 내용: UnitHUDLayer는 투영·가시성 외에 선택 HUD 정보·호버 패널의 UISystem 동적 등록·해제, 열기, sibling 정렬, 로딩 준비 배치, 일반 위젯 투영까지 맡는다. 등록 계약 IUIRuntimeRegistry는 인자에 드러나지 않고 IUISystemService를 is 캐스팅해 얻는다(캐스팅 자체는 기반 계약 모양 탓, R0337 참고).
- 고칠 방향(검토자 제안): 선택 패널 등록·열기를 별도 부품으로 떼고 레이어는 투영·가시성만 맡는다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C2-30 [낮음] 전역 연출 설정 CombatPresentationSettingsData에 12개 넘는 기능 값 약 55개와 순서 고정 검사가 평평하게 몰림

- 위치: `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:19-195`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:263-325`
- 내용: 보드 등장·선로·Nexus는 하위 묶음으로 나뉘었지만 이동·뛰기·도약·적 간격·보스 능력·추락·지반 붕괴·카메라·아침 전환·BGM/효과음·사실 큐·몸체 텍스트 값은 평평하게 나열된다. Validate는 '새 값은 맨 뒤에 둡니다' 규칙을 여섯 번 반복해 같은 기능 검사가 흩어진다(추락 302-304·315, 뛰기 310-313 등).
- 고칠 방향(검토자 제안): 기능별 [Serializable] 하위 묶음으로 나누고 각 묶음이 자기 Validate를 갖게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S26

### C2-31 [낮음] CombatRockfallDropPiece가 조각 계약의 '자원은 직접 빌리지 않는다' 보장을 어김(LSP)

- 위치: `V/Presentation/Pieces/ICombatPresentationPiece.cs:12`, `V/Presentation/Pieces/CombatRockfallDropPiece.cs:29-64`, `V/Presentation/Pieces/CombatPieceRun.cs:484-498`
- 내용: ICombatPresentationPiece 문서는 조각이 자원을 직접 빌리지 않고 실행이 빌린 대여를 맥락에서 받아 실행이 되돌린다고 보장한다. 그런데 낙석 조각은 Begin에서 _field.RentStone으로 돌을 직접 빌리고 Cancel/Finish에서 직접 돌려준다. 실행의 Leases.Restore는 이 자원을 모르고, 반환은 Stop이 Running 항목에 Cancel을 부르는 경로에만 기댄다.
- 고칠 방향(검토자 제안): 돌 대여를 실행 대여 채널로 옮기거나 계약 문서에 필드 풀 자원 예외를 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S22

### C2-32 [낮음] 걸음 모양 계약이 고정 접촉 모양에도 닿지 않는 ContactProgress 구현을 강요함(ISP)

- 위치: `V/Presentation/Movement/CombatStepStyle.cs:22-32`, `V/Presentation/Movement/CombatHopStepStyle.cs:33-40`, `V/Presentation/Movement/CombatLeapStepStyle.cs:50-55`
- 내용: CombatStepStyle은 TryGetFixedContact와 ContactProgress를 모두 추상 멤버로 요구하고 문서에 '고정 접촉 모양은 부르지 않습니다'라고 적는다. 그래서 뛰기·도약 모양의 ContactProgress(Landing·1f)는 레인이 부르지 않는 죽은 구현이면서 고정 값과 같게 유지해야 하는 두 번째 사본이다.
- 고칠 방향(검토자 제안): 접촉 진행률을 멤버 하나로 묻게 하거나(고정이면 몸 인자 무시) 몸 조회 대리자를 받는 멤버 하나로 합친다.
- 검증: 묶음 낮음; 찾은 검토자 S20

### C2-34 [낮음] 멤버 없는 표지 인터페이스 ICombatAttackUnitPose와 같은 일을 하는 Begin 경로 두 벌(과도한 추상화)

- 위치: `V/Display/Unit/ICombatAttackUnitPose.cs:3-7`, `V/Display/Unit/FieldUnitView.cs:263-267`, `V/Display/Unit/FieldUnitView.cs:465`
- 내용: ICombatAttackUnitPose는 멤버가 없는 ICombatUnitPose 파생이다. 공격 자세와 반응 자세는 같은 PresentationUnitPose 객체이고, 창구의 BeginAttackPose와 BeginPose가 같은 일을 한다. 구분이 어떤 보장도 더하지 않는다.
- 고칠 방향(검토자 제안): 표지 인터페이스와 BeginAttackPose를 걷고 ICombatUnitPose·BeginPose 하나로 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C2-35 [낮음] Display 좌표 변환기 FieldGridMapper가 Presentation 타입 CombatBody에 의존해 폴더 의존이 양방향이 됨(DIP)

- 위치: `V/Display/Field/FieldGridMapper.cs:4`, `V/Display/Field/FieldGridMapper.cs:102-104`
- 내용: FieldGridMapper는 Display/Field의 기반 좌표 변환기인데 기차 진입 시각 계산 하나 때문에 Presentation.Movement의 CombatBody를 만드는 CellBody를 갖는다. Presentation도 이 mapper를 쓰므로 Display↔Presentation 의존이 양방향이다.
- 고칠 방향(검토자 제안): CellBody를 쓰는 쪽(Presentation)으로 옮기고 mapper는 CellCenterToLocal과 셀 크기만 내준다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C2-36 [낮음] 연출 부품(Pieces)이 상위 Playback 조정자 CombatSceneEventPresenter의 문자열 상수에 의존함(DIP)

- 위치: `V/Presentation/Pieces/CombatPieceRun.cs:224`, `V/Presentation/Pieces/CombatPieceRun.cs:465`, `V/Presentation/Pieces/CombatPieceRun.cs:481`
- 내용: CombatPieceRun과 CombatPieceRunLeases가 정리 실패 문구로 CombatSceneEventPresenter.CleanupFailureMessage를 직접 참조한다. 아래 층 부품이 위 층 조정자 클래스 이름에 묶여 Presenter를 나누거나 이름을 바꾸면 Pieces도 고쳐야 한다.
- 고칠 방향(검토자 제안): 정리 실패 문구를 Pieces 쪽 상수나 공용 ErrorHandling 상수로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S21

### C4-10 [낮음] 공격 연출 값 객체가 모든 종류 값을 27인자 평평한 생성자로 받아, 부품 값·새 종류 하나가 SO·CreateData·생성자·검증 사슬을 모두 뚫음

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationData.cs:22-108`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationSO.cs:35-77`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:77-79`
- 내용: CombatAttackPresentationData는 근접·직선·곡사·코드 모션 값을 인자 27개로 받고, 종류별 검증은 생성자 안 kind if/else 사슬이다. CreateData는 부품 SO를 as 캐스트 5번으로 풀어 '없으면 0'을 채우고 처음 8개 float를 이름 없이 위치 인자로 넘긴다. 이미 타입이 나뉜 부품 SO가 있는데 값 계층에서 타입을 지운다. 새 종류는 CreateData 캐스트, 생성자 if 사슬, CombatUnitAnimationRules.FiresProjectile, CombatPresentationCueRules.AttackEventOf, ICombatAttackStyleContext의 종류별 멤버까지 고쳐야 한다. 런타임 전략 표(CombatAttackStyles)는 잘 닫혀 있다.
- 실패·변경 시나리오: 직선 투사체에 칸 하나를 추가하며 CreateData 위치 인자 줄(53-57)에서 projectileHeight와 flightDuration 자리를 바꾸면 컴파일·DB 검사를 모두 통과하고 연출이 조용히 틀어진다. 새 '부메랑' 종류를 FiresProjectile에 넣지 않으면 포구 지점 검사와 발사 경로가 빠지고, 동시에 생성자 기본 else가 직선 규칙(projectileSpeed>0)으로 거절해 부트가 멈춘다.
- 고칠 방향(검토자 제안): 부품 SO가 자기 종류 값 객체(직선·곡사·근접 값)를 만들어 검증하게 하고, 공격 값 객체는 종류와 부품 값 참조만 갖는다. FiresProjectile·AttackEventOf는 부품 또는 Style 속성으로 옮긴다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L03, L05, S25

## C3 JMH Framework 지침·FW 계약 — 중간 0 · 낮음 7

### C3-03 [낮음] 정리 실패 모으기를 공용 CleanupErrors 대신 공격 연출 파일마다 손으로 구현

- 위치: `S/RuntimeSystem/ErrorHandling/CleanupErrors.cs:17-48`, `V/Presentation/Attack/CombatAttackMotion.cs:130-153`, `V/Presentation/Attack/CombatMortarAnimation.cs:233-275`
- 내용: RuntimeSystem/ErrorHandling/CleanupErrors(Try/ThrowIfAny/RunAll)가 '끝까지 정리하고 실패를 모아 한 번 던짐'을 이미 제공하고 같은 폴더의 CombatAttackPlayback은 이를 쓴다. 반면 CombatAttackMotion.Cleanup, CombatMortarAnimation.TryCleanup(CleanupErrors.Try의 사본), CombatAttackPresenter.Cancel, CombatProjectilePool(TryRecall/TryDestroy/ThrowCleanupErrors)은 같은 규칙을 다시 짰고, 풀만 InvalidOperationException(AggregateException)으로 감싸 예외 모양이 이미 다르다.
- 실패·변경 시나리오: 오류 경계의 정리 실패 예외 모양을 CleanupErrors에서 바꿔도 공격 동작·풀·Presenter의 손 구현에는 반영되지 않아, 같은 '정리 실패'가 경로마다 AggregateException과 InvalidOperationException으로 다르게 올라온다.
- 고칠 방향(검토자 제안): 네 곳을 CleanupErrors.Try(ref errors)+ThrowIfAny 또는 RunAll로 바꾼다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S17

### C3-04 [낮음] 낙석 돌만 Framework CustomPool 대신 손으로 만든 Stack 풀을 씀

- 위치: `V/Display/Map/FieldRockfallPresenter.cs:23-24`, `V/Display/Map/FieldRockfallPresenter.cs:124-140`, `V/Display/Map/FieldObjectPresenter.cs:203-217`
- 내용: 같은 폴더의 Object·Terrain·Thorn 표시는 Framework CustomPool<T>를 쓰지만 FieldRockfallPresenter는 _stones 목록과 _freeStones Stack으로 풀을 다시 만들었다. 파괴된 인스턴스 건너뛰기 같은 처리도 없다. 지침 §28(있는 기능 다시 만들지 않기) 위반.
- 고칠 방향(검토자 제안): CustomPool<GameObject>로 바꾸고 반환 때 상태 복원을 다른 표시와 같은 모양으로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C3-06 [낮음] 아이템 칸 갱신 한 줄 때문에 View가 Core Application 층(ItemCharacterChanges)에 의존

- 위치: `V/Playback/Handlers/CombatFactEventHandler.cs:2`, `V/Playback/Handlers/CombatFactEventHandler.cs:89-96`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Items/ItemCharacterChanges.cs:51-56`
- 내용: CombatFactEventHandler.ShowEquippedItem은 Core Application의 ItemCharacterChanges.WithItems를 쓰는데, 이 함수는 null 검사 뒤 c.With(items: items)만 한다. Contracts의 CharacterCombatFacts.With로 바로 할 수 있어 이 의존은 기반 한계가 아니라 불필요하게 넓은 의존이다.
- 고칠 방향(검토자 제안): holder.With(items: …)로 바꾸고 Application using을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C3-07 [낮음 (기반 한계)] View가 표시값을 Buffer 사실 대신 Core Application 규칙 함수로 직접 계산(기반 한계)

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:484-484`, `V/Selection/CombatInspectionInfo.cs:100-100`, `V/Display/Unit/CombatUnitStatuses.cs:27-45`
- 내용: 유효 공격·방어력(CombatEffectiveStatsCalculator), 상태 묶기(StatusEffectReadModel.Capture), 위험 칸 모양(CombatHazardAreas)을 View가 Core Application 계층 static 함수로 계산한다. 규칙 사본은 없지만 가이드 §2의 'Contracts에만 의존'을 벗어나 Core 내부 시그니처에 묶인다. Core가 이 값을 사실로 게시하지 않아 생긴 기반 한계다. 아이템 변화(ItemCharacterChanges)는 Contracts로 대체 가능하므로 따로 적었다.
- 고칠 방향(검토자 제안): Core가 유효 스탯·묶인 상태·위험 칸을 Contracts 사실로 게시하면 그것을 읽는다. 그 전까지는 호출 자리를 읽기 어댑터 하나로 모은다.
- 검증: 묶음 낮음; 찾은 검토자 L04

### C3-08 [낮음 (기반 한계)] Global 서비스에 없는 UI 기능(확인창·동적 등록)을 형식 검사(downcast)로 꺼냄(기반 한계)

- 위치: `V/CombatViewSystem.cs:255-258`, `V/Display/Unit/UnitHUDLayer.cs:34-44`, `S/RuntimeSystem/GlobalSystem/UISystem/UISystem.cs:41-61`
- 내용: CombatViewSystem.BindServices는 textPresentation is IUIConfirmationPresentation으로 확인창을, UnitHUDLayer.AttachUISystem은 ui is IUIRuntimeRegistry로 동적 등록 계약을 꺼낸다. 가이드 §8 '구체 구현을 캐스트로 꺼냄' 쪽 연결이지만 GlobalSystemGroup이 두 기능을 서비스로 내주지 않아 생긴 기반 한계다. 실패 시 예외로 드러나고 각각 한 자리뿐이다.
- 고칠 방향(검토자 제안): GlobalSystemGroup이 IUIRuntimeRegistry·IUIConfirmationPresentation을 별도 서비스로 내주면 그 인터페이스를 받는다.
- 검증: 묶음 낮음; 찾은 검토자 L03, L04, S01

### C3-09 [낮음] UISystemService가 null이면 HUD 부품이 UISystem을 거치지 않고 직접 Open/Hide

- 위치: `V/Display/Hud/CombatHud.cs:25-35`, `V/Display/Hud/CombatActionPanel.cs:130-138`, `V/Display/Unit/UnitHUDLayer.cs:34-44`
- 내용: CombatHud.Present/Dismiss, CombatActionPanel.SetAvailable, UnitHUDLayer.AttachUISystem은 '격리 표시 조립(시험)'을 위해 서비스가 null이면 직접 Open()/Hide()를 부른다. 시험 편의 분기가 운영 코드에 있어, 운영 연결이 빠져도 오류 없이 UISystem 이력 밖에서 화면이 열린다(FW-009 우회가 드러나지 않음).
- 고칠 방향(검토자 제안): 운영 경로에서 서비스 null을 InvalidOperationException으로 거절하고, 격리 시험은 가짜 IUISystemService를 넘긴다.
- 검증: 묶음 낮음; 찾은 검토자 L04

### C3-10 [낮음] 이펙트 개별 회수를 위해 Framework EffectPool 노드의 ParticleSystem·Renderer를 직접 조작

- 위치: `V/Presentation/Effects/CombatEffectFollower.cs:101-112`, `V/Presentation/Effects/CombatEffectFollower.cs:149-171`, `V/Presentation/Effects/CombatEffectFollower.cs:185-197`
- 내용: EffectScope에 요청별 정지 API가 없어(기반 한계) CombatEffectFollower가 자식 풀 노드에 ParticleSystem.Stop·ParticleStopNotifier.StopEmitting을 직접 부르고 Renderer.enabled를 끄고 켠다. FW-004·FW-011의 'Scope/Handle 경로'를 벗어나 풀 노드 내부 구성에 기대며, 숨김 상태로 풀에 돌아간 노드의 Renderer 복원을 다음 LateUpdate에 의존한다. 기반 한계를 흡수하는 범위 안 우회라 base_limit=false.
- 고칠 방향(검토자 제안): EffectScope에 요청별 Stop/Hide 손잡이가 생기면 옮기고, 그 전까지는 직접 조작을 이 받침 하나로 유지하며 풀 노드 구성 전제를 시작 때 한 번 검사한다.
- 검증: 묶음 낮음; 찾은 검토자 L04

## C4 기능 변경의 독립성 — 중간 4 · 낮음 20

### C4-02 [중간] 사건 Presenter 생성자가 처리기·재생기 20여 개를 직접 조립하는 두 번째 조립 지점

- 위치: `V/Playback/CombatSceneEventPresenter.cs:96-170`, `V/Playback/CombatFieldEventPorts.cs:33-60`, `V/Playback/Handlers/CombatEventHandlerTable.cs:27-29`
- 내용: CombatEventPlaybackFactory·CombatSceneDisplayFactory가 있는데도 CombatSceneEventPresenter 생성자(약 75줄)가 지속 이펙트, 재생기 4종, 걸음 레인, 공격 시작, 처리기 12개 남짓을 직접 new 한다. 같은 클래스가 재생 세대·Mapper·사건 틀 수명도 맡는다. 기믹 창구는 CombatFieldEventPorts(필수 대리자·창구 14개 위치 인자)로 들어와 처리기 표 Create 인자로 다시 풀린다. 조정자 CombatViewSystem도 CombatInputConnection·CombatDeckHoverInput·CombatSceneInputSources 등을 직접 만들어 조립 줄이 두 곳에 나뉜다.
- 실패·변경 시나리오: 새 필드 기믹(예: 홍수) 처리기를 넣으려면 ports에 15번째 인자를 더하고, 미리보기 Factory 102줄의 위치 인자를 같은 순서로 고치고, Create 시그니처와 Presenter 생성자를 고쳐야 한다. Func<bool> 대리자가 이어진 위치 인자에서 순서를 바꿔 넣으면 컴파일은 되지만 외형 연출 대기 판정이 뒤바뀐다. 낙석 처리기 협력 객체 하나를 더해도 공격·걸음 조립이 얽힌 Presenter 생성자를 다시 검증해야 한다.
- 고칠 방향(검토자 제안): 처리기·재생기 조립을 CombatEventPlaybackFactory 쪽으로 옮기고 Presenter는 완성된 처리기 표와 재생기 목록만 받는다. 기믹 창구는 기믹별 작은 묶음으로 나눈다.
- 검증 근거(증거 검증자): 증거 줄은 주장과 맞습니다. CombatSceneEventPresenter.cs 96-170의 생성자가 지속 이펙트, 재생기 5종(공격·걸음·반응·조각·외형), 걸음 레인, CombatAttackStart, 처리기 12개를 직접 new 합니다. 반면 CombatEventPlaybackFactory.cs:32는 이 Presenter를 new 하기만 하므로 조립 지점이 Factory와 Presenter 두 곳으로 나뉩니다. CombatEventHandlerTable.cs:24-29의 문서도 연출이 있는 새 형식은 'Create 인자와 사건 Presenter 조립에 더한다'고 적고 있습니다. 그래서 새 필드 기믹 하나를 넣으면 처리기 칸, Create 시그니처, Presenter 생성자, CombatFieldEv…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 L03, L06, S13

### C4-05 [중간] 도약 중간 경유의 배치 대여 유지가 자세·서는 자리를 빌리는 다른 기능과 숨은 순서로 묶임

- 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:303-334`, `V/Display/Unit/CombatUnitPresentationWindow.cs:36-53`, `V/Display/Unit/CombatUnitPresentationWindow.cs:86-87`
- 내용: 도약 중간 걸음이 끝나도 레인은 배치 대여를 쥔 채 다음 사건을 흘려보낸다. 창구는 배치 대여가 있으면 자세 대여를 예외로 막고(RequireNoPlacement) 서는 자리를 빌려 주지 않는다. 그래서 레인 BeforeEvent가 피격 반응을 여는 사건 종류(HealthChanged·HealthNormalized)를 자기 목록으로 갖고 대여를 잠시 반환하며, 실제 자세를 여는 곳은 CombatCharacterEventHandler(HealthChanged·Died)다. 특성 처리기는 선택 콜백 beforeUnitAction(= _motion.PauseLeap)을 받아 불러야 한다.
- 실패·변경 시나리오: 피격 반응을 상태 사건(예: 보호막 깨짐)으로 넓혀 도약 중간 경유 유닛에 BeginPose를 부르면, 그 종류가 레인 BeforeEvent 목록에 없어 배치 대여가 남고 RequireNoPlacement가 InvalidOperationException을 던져 F1로 멈춘다. 서는 자리를 빌리는 새 기능이 PauseLeap을 넘기지 않으면 CanBeginStand가 거짓이라 행동이 경고만 남기고 건너뛰어진다.
- 고칠 방향(검토자 제안): 대여 창구가 다른 대여 요청 때 도약 배치를 스스로 양보하게 하거나, 레인이 자세·서는 자리 시작 알림을 받아 반환하게 해 사건 종류 목록과 선택 콜백을 없앤다.
- 검증 근거(증거 검증자): 증거는 코드와 맞다. 도약 중간 걸음이 끝나면 레인은 배치 대여를 놓지 않고 _continuedLeap로 쥔다(CombatUnitMotionPlayback.cs:456). BeforeEvent는 자세를 열 사건 종류를 HealthChanged·HealthNormalized로 직접 적어 두고 PauseLeap을 부른다(같은 파일 304-320). 그런데 실제로 자세를 여는 조건은 CombatCharacterEventHandler.TryBeginHit에 따로 있다(200-207). 창구의 BeginPose·TryBeginPose는 배치가 남아 있으면 RequireNoPlacement에서 InvalidOperationException을 던지고(CombatUnitPresentationWindow.cs:44-63, …
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S20

### C4-08 [중간] 큐 창구들이 선택 인자 사슬로 처리기 → 공격 조립 3입구 → CombatCueSinks를 뚫고 내려감

- 위치: `V/Presentation/Pieces/CombatClipAttack.cs:50-52`, `V/Presentation/Pieces/CombatClipAttack.cs:80-83`, `V/Presentation/Pieces/CombatClipAttack.cs:125-131`
- 내용: CombatCueSinks가 창구 9개를 생성자 인자로 받고, ComposeMelee·ComposeRanged·CombatCodeMotionAttack.Compose와 AddCues가 effects·camera·actorBody·projectileSounds·lead를 기본 null 선택 인자로 하나씩 다시 받아 넘긴다. 창구 하나를 더하면 Sinks 생성자, 세 입구, AddCues, CombatAttackStart의 두 호출을 모두 고쳐야 한다.
- 실패·변경 시나리오: 새 창구(예: 화면 깜빡임)를 ComposeRanged에는 넘기고 CombatCodeMotionAttack.Compose 호출에는 빠뜨린다. 기본값 null이라 컴파일·부트 검사가 통과하고, 클립 없는 원거리 유닛의 그 큐만 실행 중 조용히 사라진다.
- 고칠 방향(검토자 제안): 처리기가 CombatCueSinks(공격용 창구 묶음)를 한 번 만들어 공격 조립에 묶음 하나만 넘기고, 원거리만 projectileTimings를 덧붙인 사본을 만든다.
- 검증 근거(증거 검증자): 증거 줄의 내용은 주장과 같다. CombatClipAttack.cs:50-52·80-83과 CombatCodeMotionAttack.cs:45-47은 effects·camera·actorBody·(projectileSounds)·lead를 기본값 null인 선택 인자로 하나씩 다시 받는다. 이 값들은 CombatClipAttack.cs:125-131의 AddCues와 CombatCodeMotionAttack.cs:67-68을 거쳐 CombatCueSinks 생성자(CombatCueComposer.cs:26-28)까지 그대로 내려간다. 운영 호출자는 CombatAttackStart.cs:99-102·131-135 두 곳뿐이고 모든 값을 넘긴다. 따라서 창구 하나를 추가하면 Sinks 생성자, 입구 세 개, A…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 S21

### C4-13 [중간] 입력 소유자를 하나 더하면 포인터 대상 판정식 인자가 늘어 경로 입력·환경 호버까지 고쳐야 함

- 위치: `V/Input/CombatInputRouter.cs:54-62`, `V/Input/CombatInputRouter.cs:99-107`, `V/Input/CombatInputRouter.cs:227-250`
- 내용: ResolvePointerTarget(layer, attackActive, itemDragActive)이 소유자마다 bool 인자를 받아, 모드형 소유자 하나를 더하면 서명이 바뀌고 호출 5곳(새 기능과 무관한 CombatPathInput, CombatViewSystem.SyncEnvironmentHover 포함)을 고쳐야 한다. CombatInputFacts는 27개 인자 대부분이 기본값 있는 선택 인자라 Reader에서 새 값을 빠뜨려도 컴파일된다. Router의 UI 막힘 대상 목록도 손으로 나열돼 있다. 아이템 드래그 선례 기준 기존 파일 약 12개.
- 실패·변경 시나리오: 새 '기믹 지정' 소유자의 IsTargetingActive를 CombatInputFacts에 더하고 Reader 전달을 빠뜨리면 컴파일은 되지만, 사실만 읽는 경로 입력과 환경 호버가 지정 모드 중에도 대상을 Path로 판정해 경로 편집·환경 호버가 함께 켜진다. Router 248줄 목록에 새 대상을 빠뜨리면 UI 위 클릭이 새 소유자로 샌다.
- 고칠 방향(검토자 제안): 활성 전경 소유자의 Target을 CombatInteraction이 답하게 하고 판정식은 layer와 그 답만 받는다. UI 막힘 같은 대상 속성은 CombatInputOwner 속성으로 옮긴다.
- 검증 근거(증거 검증자): 주장대로 판정식 `CombatInputRouter.ResolvePointerTarget(layer, attackActive, itemDragActive)`(CombatInputRouter.cs:99-107)은 소유자마다 bool 하나를 받습니다. 운영 호출은 5곳입니다. Router 134·227, CombatInteraction.cs:382, CombatPathInput.cs:128, CombatViewSystem.cs:1154(SyncEnvironmentHover)입니다. 그래서 모드형 소유자를 하나 더하면 경로 입력과 환경 호버까지 서명을 맞춰 고쳐야 합니다. 이 두 기능은 새 소유자와 관련이 없습니다. 이미 `CombatInputOwner`가 Target·IsActive를 계약으로 갖고 있고(Comb…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 L05

### C2-29 [낮음] 유닛 정보창 CombatUnitInfo가 국면별 덱 카드 목록(UnitCard) 개폐 정책까지 맡음

- 위치: `V/Display/Hud/CombatUnitInfo.cs:22`, `V/Display/Hud/CombatUnitInfo.cs:102-122`, `S/RuntimeSystem/InGameSystem/ViewSystem/Tutorial/TutorialViewCardVisibility.cs:48`
- 내용: CombatUnitInfo.Render가 선택 정보 표시 외에 국면 변화에 따라 UIId.UnitCard를 닫고(전투) 다시 연다(배치 복귀). 같은 목록의 개폐를 Bootstrap(최초)과 TutorialViewCardVisibility도 결정해, 덱 목록 표시 정책을 바꾸면 정보창 코드를 함께 읽고 고쳐야 한다.
- 고칠 방향(검토자 제안): 국면별 UnitCard 개폐를 덱(또는 국면 표시 담당) 쪽으로 옮기고 정보창은 자기 개폐만 한다.
- 검증: 묶음 낮음; 찾은 검토자 S05

### C4-03 [낮음] 진입 연출·유닛 준비의 수명 지식이 조정자 15곳 남짓에 흩어짐

- 위치: `V/CombatViewSystem.cs:119-121`, `V/CombatViewSystem.cs:282-292`, `V/CombatViewSystem.cs:346-370`
- 내용: 연결(CombatViewConnection)은 EntryPresentation·Preparation 참조만 들고, 수명 지식은 CombatViewSystem 곳곳에 있다: 덱 호버 차단(119), 준비 노드 등록·조회(282-292), Tick 진입 갈래와 진입 뒤 재연결 두 길(346-370), Attach의 준비 생성(499-516, 구체 CombatSceneDisplay 반환에 기댐), Detach·StopInput 취소(751-766·790-795), 진행 호스트·완료 표시 차단(939-948), IsInputReady 판정식(1011-1016). 판정식에 Preparation != null && _loading.IsLoading 항이 있어 전역 로딩 상태가 섞인다. 덱 UI 차단 방식도 둘(속성 null 반환 / canReceiveUiInput 콜백)이다.
- 실패·변경 시나리오: 진입 중 덱 호버를 허용하거나 준비 단계를 하나 늘리려면 DeckHover 속성·진행 호스트·IsInputReady·Tick 갈래·Detach/StopInput 취소·Attach를 함께 고쳐야 하고, 차단 방식이 둘이라 한쪽만 바꾸기 쉽다. 전투가 붙은 채 LoadingSystem이 다른 로드를 시작하면(지금 운영 흐름엔 없음) TickCore가 진입 갈래에서 돌아와 재생·입력·완료 표시가 멈춘다.
- 고칠 방향(검토자 제안): 준비·진입 연출·대기 판정·Tick·취소를 갖는 진입 관문 객체를 연결이 소유하고, 조정자는 Tick/IsPending/Cancel만 부른다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S01

### C4-04 [낮음] 진입 연출이 표시 클래스 여러 곳에 전용 멤버와 Install 순서 의존을 퍼뜨림

- 위치: `V/Presentation/Entry/CombatBoardEntryPresentation.cs:27-37`, `V/Presentation/Entry/CombatEntryDeferredDisplay.cs:14-18`, `V/Presentation/Entry/CombatEntryDeferredDisplay.cs:34-45`
- 내용: 진입 기능의 몫이 Presentation/Entry 밖에 있다: FieldUnitPresenter 수집기, FieldUnitView.SetEntryHudHidden, FieldTerrainPresenter.CollectBoardRevealTargets, FieldRailwayPresenter·FieldView.SetEntryVisible, CombatShownFacts의 진입 이펙트 보류 깃발. 진입 클래스는 좁은 계약 없이 구체 표시 타입을 받는다. 정확성은 CombatSceneDisplay.Install 내부 순서(_events.Clear·_railway.Install·_events.Bind가 PrepareDisplay 앞, Prepare는 RefreshCore·Prewarm 뒤)에 숨어 있고 주석으로만 지켜진다.
- 실패·변경 시나리오: 설치 정리 순서를 바꿔 _events.Clear()나 _events.Bind를 PrepareDisplay 뒤로 옮기면 CombatShownFacts 보류 깃발이 꺼져, 적이 올라오기 전에 보호막 같은 유지 이펙트가 먼저 보인다(예외 없음). 진입 단계 하나를 더하면 FieldUnitPresenter 수집기·FieldUnitView HUD 숨김·진입 조정자·Factory·CombatSceneDisplay를 함께 고쳐야 한다.
- 고칠 방향(검토자 제안): 진입이 표시에 바라는 것(대상 수집·숨김·보류)을 좁은 계약 하나로 받고, 설치는 진입을 '준비 → 첫 그리기 → 시작' 입구 하나로 부른다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S19

### C4-06 [낮음] 걸음 레인 재료·연출 값이 값 묶음 → Presenter → 레인 집합 → 레인 여러 층을 뚫고, 중간층이 기본값으로 조용히 대체

- 위치: `V/Presentation/Movement/CombatUnitMotionSet.cs:24-33`, `V/Presentation/Movement/CombatUnitMotionSet.cs:50-66`, `V/Presentation/Movement/CombatUnitMotionSet.cs:214-224`
- 내용: CombatUnitMotionSet은 _generation을 빼면 자신이 쓰지 않는 레인 생성 재료 9~10개를 필드로 복사해 Lane()에서 그대로 넘긴다. 도약 값 CombatLeapMotionSettings 같은 새 값 묶음은 설정 표 → CombatEventPresentationValues(생성자·속성·From) → CombatSceneEventPresenter → CombatUnitMotionSet(인자·필드) → CombatUnitMotionPlayback(인자·필드) 6층을 지난다. fallMotion·stepLook·leapMotion이 여러 층에서 '= null' 선택 인자이고 '?? Default'로 대체된다. 지금 운영 경로는 모두 넘기고 있어 결함은 아니다.
- 실패·변경 시나리오: 착지 흔들림 값을 새 CombatLandingSettings로 만들고 CombatUnitMotionSet → CombatUnitMotionPlayback 전달을 빠뜨린다. 선택 인자라 컴파일되고 시험도 기본값으로 통과하지만, 디자이너가 DB에서 바꾼 값이 화면에 반영되지 않는다.
- 고칠 방향(검토자 제안): 레인 생성 재료를 묶음 하나(또는 레인 생성 대리자)로 넘기고, 레인이 값 묶음을 직접 읽게 해 중간층 선택 인자와 ?? Default를 없앤다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L05, S20

### C4-07 [낮음] 입력 사실 Reader·입력 재료 하나를 더하려면 그대로 넘기기만 하는 생성 서명 다섯 층을 뚫어야 함

- 위치: `V/CombatViewSystem.cs:267-269`, `V/Input/ICombatSceneInputFactory.cs:16-19`, `S/Factory/Runtime/InGame/View/CombatSceneFactory.cs:36-41`
- 내용: 조정자 CreateFactsReader 호출 → ICombatSceneInputFactory → CombatSceneFactory → CombatSceneInputFactory.CreateFactsReader → CombatInputFactsReader 생성자의 다섯 서명이 같은 10개 인자(조정자 내부 객체 CombatInputConnection·CombatResultPlayback·CombatViewProgress 등)를 그대로 넘긴다. 구현은 sealed 하나뿐이다. 입력 재료도 BindServices 인자 → CombatSceneInputSources(16개 인자·속성) → 입력 Factory → 기능 객체로 같은 모양이다. (원 제보의 걸음 레인 부분은 걸음 레인 인자 사슬 문제에 합쳤다.)
- 실패·변경 시나리오: 입력 판단에 '튜토리얼 강조 대상' 사실 하나를 더하면 인터페이스·어댑터·정적 Factory·Reader·조정자 호출 다섯 서명과 시험 가짜 Factory까지 고쳐야 한다. 대체 구현이 없어 이 층들은 변경 비용만 늘린다.
- 고칠 방향(검토자 제안): FactsReader는 조정자가 직접 만들거나(생성 계약에서 뺌) 공급자 묶음 하나를 받게 한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L06

### C4-09 [낮음] CombatPieceRunPlayer가 쓰지 않는 의존 다섯 개를 받아 CombatPieceRun에 넘기기만 함

- 위치: `V/Playback/Players/CombatPieceRunPlayer.cs:31-63`, `V/Playback/Players/CombatPieceRunPlayer.cs:85-86`, `V/Playback/CombatSceneEventPresenter.cs:139-141`
- 내용: 재생기는 window·camera·lifetime·generation·mapper를 직접 쓰지 않고 실행 생성자(인자 11개)에 그대로 넘긴다. 조각 실행 의존 하나를 더하려면 Presenter 조립 → 재생기 생성자·필드 → 실행 생성자 세 층을 고쳐야 한다.
- 고칠 방향(검토자 제안): 실행 의존을 맥락 객체 하나(예: CombatPieceRunContext)로 묶어 재생기에는 그 하나만 넘긴다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C4-11 [낮음] 강조 색·층 하나 추가가 FieldView 직렬화 필드·22개 Color 위치 인자·팔레트 switch·테두리 열기 목록을 함께 뚫음

- 위치: `V/Display/Field/FieldView.cs:40-116`, `V/Display/Field/FieldView.cs:601-606`, `V/Display/Field/FieldHighlightPalette.cs:11-34`
- 내용: 칸 강조 색은 FieldView의 SerializeField Color 21개와 fillAlpha로 들어와, CreatePalette가 같은 Color 형식의 위치 인자 22개로 FieldHighlightPalette 생성자에 넘기고 팔레트는 TryGet switch로 다시 고른다. 마지막 gimmickHoverRange는 '= default' 선택 인자다. 새 범위형 층은 FieldAttackOutlineOpening의 '원점 쪽 변을 여는 층' 목록과 IsHoverRange에 손으로 넣어야 하며 층 표(LayerRow)의 열이 아니다. 기존 색 재사용 층은 표 한 줄로 끝난다.
- 실패·변경 시나리오: 새 층 색을 넣으며 CreatePalette에서 allyHoverMoveRangeColor와 enemyMoveRangeColor 자리를 바꿔 넘기면 컴파일은 되고 아군·적 호버 이동 범위 색이 뒤바뀐다. 선택 인자를 빠뜨리거나 TryGet 갈래를 빠뜨리면 그 층이 투명·무색으로 조용히 그려진다.
- 고칠 방향(검토자 제안): 층 색을 FieldHighlightColor → Color 직렬화 표 하나로 받아 구성 때 누락을 검사하고, 테두리 열기·호버 범위 여부를 LayerRow 열로 옮긴다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L05, S03, S04

### C4-12 [낮음] 입력 요청 하나의 처리 갈래가 수신 창구·조정자 RouteInput·CombatSceneInput.Route·Router 네 곳에 흩어짐

- 위치: `V/Input/CombatInputReceiver.cs:106-125`, `V/CombatViewSystem.cs:1086-1136`, `V/Input/CombatSceneInput.cs:120-154`
- 내용: 확인창 통과 목록은 CombatInputReceiver가, 카메라·EnterCombat·EndTurn·SelectPressed 특별 갈래는 조정자 RouteInput이, CameraPan/Zoom 재분배와 요청별 환경 호버(마지막 else = 환경 선택 취소)는 CombatSceneInput.Route가, 나머지는 Router switch가 맡는다. EnterCombat·EndTurn은 확인창 입구를 거쳐 Router를 다시 불러 CombatSceneInput.Route를 건너뛰고, 두 입구는 입력 정리용으로 Request.FocusLost를 보내 Router 갈래 뜻에 숨어 기댄다.
- 실패·변경 시나리오: 새 요청(예: 아이템 단축키)을 더하며 CombatSceneInput.Route 갈래를 빠뜨리면 기본 else가 환경 선택을 말없이 취소한다. '복귀에도 확인창' 변경 때 조정자 갈래를 빠뜨리면 확인창 없이 Router로 바로 제출된다. Router FocusLost 갈래에 동작을 더하면 확인창이 열릴 때마다 그 동작도 돈다.
- 고칠 방향(검토자 제안): 확인창·카메라 분기를 CombatSceneInput.Route 한 분배 표로 모으고, 확인창 입구는 이름 있는 정리 입구(예: Interaction.CancelInputs)를 부른다. 조정자는 수명 검사와 뒤처리만 한다.
- 검증: 증거 중간 · 반론 낮음 · 판정 낮음; 찾은 검토자 L06, S01, S10

### C4-15 [낮음] '다음 사건 시작을 막는가' 규칙이 Factory 람다·사건 재생 조정·재생기 목록 세 곳에 나뉘고, 두 Factory가 다르게 조립

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:163-175`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:102`, `V/Display/Map/FieldTornadoPresenter.cs:84-104`
- 내용: CombatPresentationPlayers는 이름 있는 자리로 순서를 고정하고, CombatSceneEventPlayback은 기차 접촉·국면 전환을 목록 밖에 두어 IsPresentationComplete·CanBeginPresentationEvent·BeginPresentation에서 각각 다른 조합으로 막는다. 필드 외형(약한 지반 무너짐·토네이도)은 FieldTornadoPresenter가 TickAppearance·IsAppearancePlaying·CancelAppearance를 따로 열고, CombatSceneDisplayFactory가 람다 넷으로 묶는다('토네이도는 재생 중이면, 지형은 떨리는 동안만 막는다'는 정책이 조립 줄에 있음). 미리보기 Factory는 같은 포트를 terrain만으로 다르게 묶는다.
- 실패·변경 시나리오: 하붑 이동이나 새 외형 전환이 '다음 사건을 막아야' 할 때 작업자는 Players 목록·SceneEventPlayback 조건·Factory 람다 세 곳을 모두 읽어야 한다. 한 곳만 고치면 IsPresentationComplete와 CanBeginPresentationEvent가 다른 답을 내 RefreshCore의 '남은 연출을 마친 뒤' 예외나 사건 겹침이 생긴다. 미리보기에서 토네이도를 보이게 하면 이미 어긋난 조립 때문에 대기가 빠진다.
- 고칠 방향(검토자 제안): 필드 외형을 ICombatPresentationPlayer 구현으로 재생기 목록에 넣고 기차 접촉·국면 전환도 같은 목록 자리로 옮겨, 차단·완료 판정을 목록 하나가 정하게 한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L06, S07

### C4-16 [낮음] 연출 설정 항목 클래스가 무관한 DB 표·런타임 부품의 값 조건 함수 창고가 됨

- 위치: `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:328-453`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatBoardRevealSettings.cs:33-42`, `V/Presentation/Pieces/CombatRisePiece.cs:39-40`
- 내용: CombatPresentationSettingsData에 ValidateRiseDepth·ValidateFallDepth·ValidateStepSeconds 같은 정적 조건 함수가 15개 넘게 있고, 다른 DB 표 6곳과 View 런타임 부품 약 12곳이 이 함수를 부른다. 대부분 CombatPresentationValueRules.IsNonNegative/IsPositive에 특정 영역 문구를 붙인 감싸개인데, '소환 등장 깊이' 같은 이름이 뜻이 다른 값(보드 등장 depth·overshoot, 일반 Rise 조각)에 다시 쓰인다.
- 실패·변경 시나리오: 소환 등장 깊이를 양수로만 받으려고 ValidateRiseDepth를 IsPositive로 바꾸면 보드 등장 overshoot=0(유효 값)과 CombatRisePiece의 깊이 0 사용처도 함께 거절돼 presentation.Validate가 부트에서 멈춘다. 바꾼 사람은 소환만 고쳤다고 생각한다.
- 고칠 방향(검토자 제안): 수 조건은 CombatPresentationValueRules를 직접 부르고, 영역별 감싸개는 그 값을 가진 설정·값 객체 안에만 둔다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S26

### C4-18 [낮음] 생성 뒤 Bind* 호출 순서가 운영 Factory 한 곳의 관례에 기대고, 미리보기 조립은 일부를 빠뜨림

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:184-208`, `V/Playback/CombatSceneEventPresenter.cs:164-166`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:103-109`
- 내용: 사건 Presenter는 생성 뒤 BindStructureDamage·BindInspectionRendered·selection.Bind·inspection.BindSelectionChanged를 정해진 순서로 받아야 완전해진다. 구조물 처리기는 BindStructureDamage 전에 사건이 오면 InvalidOperationException을 던진다. CombatTraitPreviewFactory는 BindStructureDamage를 부르지 않는다(지금 미리보기는 구조물 사건을 만들지 않아 잠재).
- 고칠 방향(검토자 제안): 구조물 숫자 창구를 CombatEventPlaybackFactory.Create의 필수 인자로 받아 두 조립이 같은 경로로 완성되게 한다.
- 검증: 묶음 낮음; 찾은 검토자 L06

### C4-19 [낮음] 이펙트 창구의 Rider 높이 보정이 Presenter 생성자의 뒤늦은 BindBodyCategories에 숨은 순서 의존

- 위치: `V/Presentation/Effects/CombatEffectWindow.cs:71-90`, `V/Playback/CombatSceneEventPresenter.cs:125-126`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:97-100`
- 내용: CombatEffectWindow는 Factory가 만들지만 몸 병과 조회는 CombatSceneEventPresenter 생성자가 BindBodyCategories로 나중에 꽂는다. 그 전에 riderYOffset이 0이 아닌 값으로 AdditionalWorldOffsetOf가 불리면 InvalidOperationException을 던진다. 같은 창구를 쓰는 다른 부품(하붑 등)이 이 연결 순서를 알아야 하고, 이펙트 창구가 유닛 병과 규칙까지 맡는다.
- 고칠 방향(검토자 제안): 병과 조회를 창구 생성 인자로 받거나, 보정 계산을 부르는 쪽(CombatCuePoints)이 병과를 넘기게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S18

### C4-20 [낮음] FieldUnitPresenter 설정이 네 입구로 흩어져 호출 순서에 숨은 의존이 있고 두 조립이 같은 순서를 반복

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:87-148`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:115-194`, `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs:88-91`
- 내용: ConfigureMovementAppearance·ConfigureHUDCategories·Configure·BindItemUnequip로 나뉜다. Rent/CreateView는 _movementColors가 이미 설정됐다고 가정하고(PrepareMovementAppearance(null)은 조용히 건너뜀), RenderUnitHud는 ConfigureHUDCategories를 따로 요구하며, Configure의 hudLayer·itemVisuals는 선택 인자다. 전투·특성 미리보기 조립이 같은 순서를 각자 반복한다.
- 고칠 방향(검토자 제안): 설정 값을 필수 인자 하나의 Configure로 합쳐 두 조립이 같은 입구를 쓰게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C4-22 [낮음] 테두리 표시가 채우기 부품의 Apply 순서와 내부 깊이 상태에 기댐

- 위치: `V/Display/Field/FieldCellFillStack.cs:159-175`, `V/Display/Field/FieldCellFillStack.cs:298-316`, `V/Display/Field/FieldCellFillStack.cs:536-571`
- 내용: FieldOutlineRenderer는 _fills.Apply를 먼저 불러야 HoverOutlineDepth·HoverOutlineLayer·HoverOutlineCells·CellsOf·DepthCountOf가 유효해진다. 테두리만 쓰는 호버 덩어리 BFS도 채우기 부품 안에 있다. 호버 테두리 규칙을 바꾸려면 채우기 부품을 고쳐야 하고, 순서를 어기면 지난 Apply 값이 조용히 쓰인다.
- 고칠 방향(검토자 제안): Apply가 깊이 결과 객체를 돌려주고 테두리는 그 결과만 받게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C4-23 [낮음] 점유 거절 뒤 확정이 끝나려면 Poll이 같은 옛 응답을 다시 넘겨야 함(숨은 호출 의존)

- 위치: `V/Input/MovePath/CombatMovementInteraction.cs:216-228`, `V/Input/MovePath/CombatMovementInteraction.cs:286-289`, `V/Input/MovePath/CombatMovementInteraction.cs:353-354`
- 내용: DestinationOccupied 편집 분기는 요청 소유를 유지한 채 돌아간다. 그 뒤 Confirm하면 _frozen=true가 되지만 새 요청을 게시하지 않고 _context도 null이라 제출하지 않는다. 확정 거절 처리는 다음 Tick에 Poll이 '번호 무관 마지막 응답'으로 같은 결과를 다시 넘길 때만 일어나며, 이 의존은 주석에만 있다.
- 고칠 방향(검토자 제안): Confirm에서 마지막 결과가 같은 revision의 거절이면 그 자리에서 확정 거절을 처리한다.
- 검증: 묶음 낮음; 찾은 검토자 S13

### C4-24 [낮음 (기반 한계)] 넉백 후속 대응이 Core 공격 처리의 사건 기록 순서에 숨은 의존을 가짐

- 위치: `V/Presentation/Attack/CombatHitFollowUps.cs:21-24`, `V/Presentation/Attack/CombatHitFollowUps.cs:59-64`, `V/Presentation/Attack/CombatHitFollowUps.cs:90-93`
- 내용: 넉백 이동에는 근원이 없어 CombatHitFollowUps가 이동을 '가장 최근에 연 출처'에 붙이고, Core AttackDeliveryExecutor의 기록 순서(넉백 → 도착 정산 → 처치 연쇄 → 추가 공격)를 전제로 한다. Core 사실 모양이 강제한 기반 한계다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C4-27 [낮음] 진입 적 등장이 보스 소환 등장 값(SummonRise*)을 그대로 써 두 연출이 묶임

- 위치: `S/Factory/Runtime/InGame/View/CombatBoardEntryFactory.cs:29`, `V/Presentation/Entry/CombatEntryEnemyAppearance.cs:63-66`, `V/Playback/Support/CombatAbilityMotionSettings.cs:36-54`
- 내용: 보드·선로·Nexus는 각자 설정 묶음이 있는데 적 등장만 CombatAbilityMotionSettings의 소환 대기·시간·깊이·바퀴 수를 쓴다. 보스 소환 연출을 조정하면 모든 타일 스테이지 시작의 적 등장도 바뀌고 진입만 따로 맞출 수 없다. 재사용이 의도일 수 있다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C4-28 [낮음] Nexus 착지 이펙트 종류만 코드 상수(EffectType.FallingRocks)라 낙석 외형과 묶임

- 위치: `V/Presentation/Entry/CombatEntryNexusAppearance.cs:118`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatNexusDropSettings.cs:9-25`, `S/DataBaseSystem/Data/View/Table/Railway/Item/RailwayVisualDataSO.cs:41`
- 내용: Nexus 낙하의 다른 값은 CombatNexusDropSettings에 있고 다른 연출의 이펙트 종류도 DB 칸인데, Nexus 착지 이펙트만 코드에 고정돼 있다. 낙석과 같은 FallingRocks 프리팹이라 낙석 외형을 조정하면 Nexus 진입 모습도 바뀌고, 바꾸거나 끄려면 코드를 고쳐야 한다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C4-29 [낮음] 배경 표시(StageViewPropPresenter)가 낙석 기능의 앵커 이름·필드·초기화를 직접 가짐

- 위치: `V/Display/Map/StageViewPropPresenter.cs:32-36`, `V/Display/Map/StageViewPropPresenter.cs:49-52`, `V/Display/Map/StageViewPropPresenter.cs:80-93`
- 내용: 배경 프랍 클래스가 낙석 전용 이름 상수 3개, Transform 필드 3개와 폭발 사용 여부, 속성 4개, Show의 이름 검색 3회(GetComponentsInChildren<Transform>(true)), Release 초기화 4줄을 갖고, FieldRockfallPresenter가 이 상수를 참조한다. 낙석 등 기믹 맵 앵커를 바꾸거나 더하면 배경 클래스 5곳을 고쳐야 한다.
- 고칠 방향(검토자 제안): 낙석 앵커 바인딩 컴포넌트를 두거나 앵커 읽기를 낙석 쪽 작은 리더로 뗀다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C4-30 [낮음] 표시 조립 Factory가 GlobalSystem TurnCost UI를 닫는 실행 정책을 함께 수행

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:91-92`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:182-188`
- 내용: CombatSceneDisplayFactory.Create가 씬 루트에 TurnCost가 있으면 표시 생성 도중 services.UI.CloseUI(UIId.TurnCost)를 부른다. 조립 줄 안에 UI 상태 변경이 섞여 TurnCost 표시 정책을 바꾸려면 조립 Factory를 읽어야 한다. 출력 카메라 대체식(root.WorldCamera ?? Camera.main)도 같은 Factory에 두 번 적혀 있다.
- 고칠 방향(검토자 제안): TurnCost 닫기를 턴 표시 Presenter나 Display.Install(씬 연결 수명)으로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 L06, S24

## C5 불필요한 반복 작업·일일이 검사 — 중간 2 · 낮음 54

### C5-01 [중간] 꺼 둔 단계(아침) 화면 전환 경로가 조립·설정 검사·재생 판정에 그대로 남음

- 위치: `V/Playback/CombatPhaseTransitionPresenter.cs:11-111`, `V/Playback/CombatSceneEventPlayback.cs:69-74`, `V/Playback/CombatSceneEventPlayback.cs:126-137`
- 내용: CombatPhaseTransitionPresenter.TryBegin 본문이 통째로 주석이라 늘 false다. _scop은 늘 null, IsPlaying은 늘 false, Tick·Cancel·CleanupFailed는 할 일이 없고 _transitions·_settings·_beginEvent 등은 저장만 한다. 그런데 CombatSceneEventPlayback은 IsPresentationComplete·CanBeginPresentationEvent·BeginPresentation·TickPresentation·취소/실패 정리 다섯 곳에서 이 객체를 묻고, 매 프레임 Tick을 부르며 TryBegin을 try/catch로 감싼다. 두 Factory(운영 씬·특성 미리보기)는 CombatPhaseTransitionSettings.From(레이어 이름 없으면 예외)과 전환 카메라 요구(RequireMapCamera)를 계속 만들고 검사하며, 설정 SO의 Validate도 '자동 재생 비활성'이라 적힌 morningTransitionLayer·카메라·속도·양을 필수로 본다. CombatEventTypeTable의 Transition 열·TransitionOf는 주석 속 한 줄만 참조한다.
- 실패·변경 시나리오: 연출 담당이 설정 표의 MorningTransitionLayer 이름을 바꾸거나 맵 프리팹의 Day 카메라 이름을 바꾸면, 쓰지 않는 기능의 검사 때문에 CombatSceneDisplayFactory가 InvalidOperationException을 내 전투 씬 조립과 특성 미리보기 조립이 모두 멈춘다. 재생 Gate를 고치는 작업자는 늘 false인 IsPlaying 조건 다섯 곳을 실제 대기 조건으로 읽고 함께 고쳐야 한다.
- 고칠 방향(검토자 제안): 되살릴 계획이 없으면 Presenter·설정 변환·Factory 인자·표의 Transition 열·재생 연결·설정/카메라 검사를 함께 걷어 낸다. 되살릴 계획이면 조립 단계에서 통째로 빼고 꺼져 있을 때는 검사하지 않는다.
- 검증 근거(증거 검증자): 주장대로 CombatPhaseTransitionPresenter.cs:35-63은 통째로 주석이고 TryBegin은 늘 false를 돌려줍니다(:64). 그래서 _scop은 늘 null, IsPlaying(:27)은 늘 false이고, Tick·Cancel·CleanupFailed와 _transitions·_settings·_beginEvent·_completed는 운영에서 하는 일이 없습니다. 그런데도 CombatSceneEventPlayback.cs:69-74·82·128-137·166·174·178·189가 이 객체를 묻고 try/catch로 감싸며 매 프레임 Tick을 부릅니다. CombatEventTypeTable.TransitionOf(:326-330)와 행의 transition 열(:131-13…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L07, L09, S13, S26

### C5-02 [중간] 입력 계층이 늘 넘겨받는 협력자를 선택 인자로 받고 null 갈래를 곳곳에 둠

- 위치: `V/Input/CombatInputRouter.cs:75-87`, `V/Input/CombatInputRouter.cs:142-145`, `V/Input/CombatInputRouter.cs:218-222`
- 내용: 운영 조립은 CombatSceneInputFactory 하나뿐이고 늘 null 아닌 값을 넘기며(actionAvailability는 Factory가 먼저 ArgumentNullException), Assets/Tests에도 이 생성자 호출이 없다. 그런데 CombatInputRouter(cursorSnap·occlusion·reportBlockedInput·movementUndo), AttackTargetingInteraction(sounds·rejections·trajectory·actionAvailability), CombatInteraction(path·attackDrag·item·rangeHover), PlacementDragInteraction(sounds), CombatPathInput·CombatMovementInteraction(actionAvailability·rejections), 두 확인창(presentation), CombatInputFacts(인자 26개 중 21개 선택), CombatActionButtons·CombatInputSurfaces.Sync가 '= null' 기본값과 '?.'/'== null ||' 갈래를 둔다. '시험 조립' 이유를 든 CombatInteraction.Poll 단일 인자판도 호출이 없다. 같은 파일 묶음에서 CharacterCombatFacts가 보장한 Anchor·Rotation 재확인, cueSources 이중 null 검사도 같은 성격이다. AttackTargetingInteraction 112줄은 의존이 빠지면 '공격 가능'으로 읽는다.
- 실패·변경 시나리오: 새 조립 경로(튜토리얼·연출 확인 씬 등)가 AttackTargetingInteraction에 actionAvailability나 rejections를 빠뜨려도 컴파일·실행이 통과하고, Core 가용성 응답에 따른 공격 사전 거절·거절 안내·궤적 표시가 오류 없이 사라진다(112줄이 null을 허용으로 읽음).
- 고칠 방향(검토자 제안): 운영에서 늘 주는 협력자는 필수 인자로 바꿔 생성자에서 '?? throw'로 막고 쓰는 곳의 '?.'·'== null ||' 갈래와 시험용 오버로드를 지운다. 정말 없을 수 있는 것만 Null Object로 둔다.
- 검증 근거(증거 검증자): 주장이 대체로 맞다. 이 생성자들을 운영에서 부르는 곳은 CombatSceneInputFactory.cs:78-82·150-161 하나뿐이다. 이 조립은 sounds·notices·display·actionAvailability(144-145에서 ArgumentNullException으로 먼저 막음), cursorSnap·occlusion·reportBlockedInput(CombatSceneInputSources.cs:60에서 필수)·movementUndo를 늘 null 아닌 값으로 넘긴다. Assets/Tests를 grep해 봐도 Router·AttackTargeting·CombatInteraction·PathInput·PlacementDrag 생성자를 부르는 곳이 없다. 그런데 CombatInputRo…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 L07, S10, S12, S13

### C5-03 [낮음] 쓰지 않는 HandPresenter·HandCardHoverView가 Combat 씬에 켜진 채 남아 매 프레임 Update

- 위치: `V/Display/Hud/HandPresenter.cs:20-75`, `V/Display/Hud/HandPresenter.cs:142-174`, `V/Display/Hud/HandPresenter.cs:186-213`
- 내용: HandPresenter는 스스로 '쓰지 않음(2026-10-24)'이라 밝히고 CombatSceneRoot도 참조 칸을 걷어 Configure·Render를 부르는 코드가 없다(_views 늘 null). 그런데 Combat.unity의 Hand 오브젝트에 m_Enabled: 1로 붙어 있어 Update·OnEnable·OnDisable·OnApplicationFocus가 돌고 첫 줄에서 바로 빠진다. 풀·레이아웃·호버·집기 판정(TryPickUnit·ContainsDropPoint) 약 400줄이 Missing Script 회피용으로 남아 있고, remarks의 '이 파일과 그 시험을 함께 지웁니다'는 시험이 이미 없어 낡았다. (R0093의 화면 전환 부분은 단계 전환 문제에서 다룬다.)
- 고칠 방향(검토자 제안): Combat.unity에서 컴포넌트를 떼고 두 파일을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L04, L08, L09, L10, S05

### C5-05 [낮음] '시험·진단용 읽기'라 적힌 운영 멤버를 시험도 운영도 쓰지 않음

- 위치: `V/Display/Unit/FieldUnitView.cs:63-68`, `V/Display/Unit/FieldUnitView.cs:263-267`, `V/Display/Unit/UnitClipChannel.cs:112-119`
- 내용: 정의 말고 참조가 0인 멤버가 여럿이다. FieldUnitView.CurrentPresentationPose·ClipChannel·BeginAttackPose·BeginReactionPose, UnitClipChannel.HasBase·HasOverlay·Graph(밖에 PlayableGraph를 내주는 표면), FieldView.HighlightState, CombatTrainCamera.IsZooming, CombatViewBuffer 쓰기 계약의 ClearPreview(), CombatPhaseTransitionSettings.From의 선택 인자 findLayer, UnitHUDLayer.AttachUISystem(null) '격리 시험용' 분기(운영 두 곳은 늘 services.UI를 넘김), CombatPathInput 29줄 멤버. R0379가 함께 든 CursorSnap·StepContacts·RailwayTravelPath·Thorn/Trail/ProjectilePool ActiveCount·CombatInteraction.Poll·EffectRules.KnownRule·AttachDisplay·HandPresenter는 각 파일 문제에서 다룬다.
- 고칠 방향(검토자 제안): 참조 0인 시험·진단용 멤버와 null 분기를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L07, L15, S09

### C5-06 [낮음] 조립 Factory에 쓰지 않는 Create 래퍼·기본 오류 경계·backdrop=null 분기·늘 null인 selectionChanged 인자

- 위치: `S/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs:37-55`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:61-66`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:120-126`
- 내용: InGameViewSystemFactory.Create는 호출자가 없고 운영은 늘 faults를 넘겨 '테스트용' 기본 로그 경계가 닿지 않는다. CombatSceneDisplayFactory.Create의 backdrop = null 기본값과 null 분기는 '표시 시험'용이라 적혔지만 시험이 없다. CombatInspectionPresenter의 selectionChanged 생성자 인자는 유일한 생성 지점이 늘 null을 넘기고 display를 만든 뒤 BindSelectionChanged로 다시 넣어, 같은 콜백을 넣는 길이 두 개다.
- 고칠 방향(검토자 제안): 래퍼·기본값·null 분기를 지우고 selectionChanged는 한 길로만 묶는다.
- 검증: 묶음 낮음; 찾은 검토자 S23, S24

### C5-07 [낮음] 맵 카메라 사실의 시험용 Transform 선택 인자 때문에 운영에 null 검사가 생김

- 위치: `V/Presentation/Cameras/CombatMapCameras.cs:16-31`, `V/Presentation/Cameras/CombatMapCameras.cs:83`, `V/Presentation/Cameras/CombatCameraView.cs:54-57`
- 내용: CombatMapCamera 생성자의 transform = null 기본값은 '줌·흔들림만 보는 창구 시험'용인데 운영 경로(CombatMapCameras.Read)는 늘 transform을 넘긴다. 그래서 CombatCameraView 생성자가 카메라마다 Transform null을 다시 검사하고 BasePosition/BaseRotation에 null 분기가 생겼다.
- 고칠 방향(검토자 제안): transform을 필수로 받고 null 분기를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S18

### C5-08 [낮음] 재생 처리기·이동 재생이 늘 받는 협력자를 선택 인자로 두고 null 갈래·기본값을 겹쳐 둠

- 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:85-105`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:429-444`, `V/Presentation/Movement/CombatUnitMotionSet.cs:47-66`
- 내용: CombatUnitMotionSet·CombatUnitMotionPlayback의 thornBody·objectBody·leapMotion은 '= null'이고 leapMotion ?? Default가 두 생성자와 값 묶음에 세 번 있다. 유일한 생성 호출(사건 Presenter 133줄)은 늘 값을 넘기고 포트 생성자가 null을 거부한다. CombatFactEventHandler의 consumeThorn·hideObject·showTerrainChange·showTrail, CombatTraitEventHandler의 positionSwap·beforeUnitAction, ComposeDeath·ComposeHit·CombatRoarEventHandler.Compose의 camera/effects/sound도 '없으면(시험 조립)'이라 적었지만 시험도 없고 운영은 늘 넘긴다.
- 고칠 방향(검토자 제안): 필수 인자로 바꾸고 '?.Invoke'·기본값 대체를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S14, S20

### C5-09 [낮음] 운영에서 쓰지 않는 표시 생성 경로(의존 없는 계획 Presenter 생성자, entryPresentation 기본값 null)

- 위치: `V/Display/Feedback/CombatMovementPlanningPresenter.cs:30-42`, `V/Display/CombatSceneDisplay.cs:166`, `V/Display/CombatSceneDisplay.cs:251-256`
- 내용: CombatMovementPlanningPresenter의 (FieldView) 생성자는 외부 호출이 없고 4인자 생성자의 체인으로만 쓰이며, 그것으로 만들면 _presentation·_ghost·_config가 null이라 BeginEditing·EndEditing에서 NRE가 난다. CombatSceneDisplay는 entryPresentation = null 기본값과 '?.' 검사 4곳을 두지만 Factory는 늘 CombatBoardEntryFactory.Create 결과를 넘긴다.
- 고칠 방향(검토자 제안): 쓰지 않는 생성자를 합치고 entryPresentation을 필수로 받는다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C5-10 [낮음] CombatViewSystem 연결·표시 경로와 표 묶음이 같은 전제를 층마다 다시 검사

- 위치: `V/CombatViewSystem.cs:451-462`, `V/CombatViewSystem.cs:483-495`, `V/CombatViewSystem.cs:604-615`
- 내용: 진입 활성은 OnCombatSceneEntered·Attach·TakeDisplay에서 세 번, 오류 상태·중복 연결·서비스 없음은 Attach와 TakeDisplay에서 두 번 본다. CreateFactsReader는 늘 new를 돌려주는데 '?? throw'를 붙였다. SyncActionAvailability는 안에서 CanContinue·연결 일치를 보는데 호출 자리도 앞뒤로 같은 검사를 한다. CombatDisplayTables는 From이 표마다 InvalidOperationException으로 검사한 뒤 생성자가 ArgumentNullException으로 다시 보며, ViewDataBase.GetService가 이미 없는 서비스를 던지므로 늘 거짓이다.
- 고칠 방향(검토자 제안): 검사를 입구 한 곳에 모으고 나머지 재검사를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S01

### C5-11 [낮음] CreateSceneInteraction이 이미 보장된 값 11개를 다시 null 검사

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:74-78`, `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:110-146`, `V/Input/CombatSceneInputSources.cs:28-43`
- 내용: 유일한 호출자 CreateSceneInput은 CombatSceneInputSources 생성자가 모두 null 검사한 값과 바로 위에서 new 한 gate·sounds·actionAvailability만 넘긴다. 호출자 하나뿐인 CreateSceneInteraction이 같은 검사를 반복한다.
- 고칠 방향(검토자 제안): 재검사를 지우거나 Sources 하나를 받게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S24

### C5-12 [낮음] 해제 확인이 CombatSceneDisplay와 하위 Presenter·Mapper에서 겹침

- 위치: `V/Display/CombatSceneDisplay.cs:727-783`, `V/Display/CombatSceneDisplay.cs:476-502`, `V/Display/Feedback/CombatMovementPlanningPresenter.cs:45-76`
- 내용: CombatSceneDisplay가 모든 표시 의도 메서드 앞에서 _isReleased를 보고 하위 Release는 Display.Release에서만 불리는데, CombatMovementPlanningPresenter는 ShowDraft·ClearDraft·SetPathPlanning·BeginEditing·UpdatePointer·ShowRange에서 _released를, CombatPointerMapper는 _isReleased/IsBound를 다시 본다.
- 고칠 방향(검토자 제안): 해제 확인을 Display 한 층에 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C5-13 [낮음] 공격 Tick 경로마다 deltaTime 검사·중첩 Tick 가드·IsCurrent 확인을 층층이 반복

- 위치: `V/Presentation/Attack/CombatAttackPresenter.cs:44-56`, `V/Presentation/Attack/CombatAttackPresenter.cs:114-120`, `V/Presentation/Attack/CombatAttackMotion.cs:71-76`
- 내용: deltaTime NaN·무한 검사와 Math.Max(0, dt)가 CombatAttackPresenter·CombatAttackMotion·CombatMortarAnimation·CombatProjectilePool의 Tick에 모두 있다. 중첩 Tick 가드(_ticking)도 CombatAttackPlayback(Tick·TickShotsOnly), 동작, 풀에 겹친다. CompleteIfReady는 부작용 없는 IsComplete 앞뒤로 IsCurrent를 두 번 본다. CombatAttackShape.InterceptionsOf는 Span판·List판 루프가 똑같다.
- 고칠 방향(검토자 제안): 입구(Presenter.Tick)에서 한 번 검사하고 하위는 신뢰한다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C5-14 [낮음] 가용성 응답의 개정 번호·입력 가능 조건을 늘 참인 채로 다시 확인

- 위치: `V/Input/Interaction/CombatActionAvailabilityTracker.cs:45-47`, `V/Input/Interaction/CombatActionAvailabilityTracker.cs:112-132`, `V/Input/Interaction/CombatMovementUndoInput.cs:38-46`
- 내용: CombatActionAvailabilityTracker.Read의 '_answer.SourceRevision == facts.QueryRevision'은 Matches와 Consume이 이미 보장해 늘 참이다. CombatMovementUndoInput.ReadAnswer의 '!facts.IsInputAvailable || facts.HasUnhandledCommand'도 이어 부르는 _availability.Read → CanQueryActionAvailability가 이미 본다.
- 고칠 방향(검토자 제안): 늘 참인 비교와 앞선 재검사를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S12

### C5-15 [낮음] 토네이도 착지 콜백이 CombatPieceRunPlayer가 보장한 세대 검사를 두 번 더 함

- 위치: `V/Playback/Handlers/CombatCharacterEventHandler.cs:112-121`, `V/Playback/Players/CombatPieceRunPlayer.cs:68-75`
- 내용: CombatPieceRunPlayer.TryBegin은 completed를 '세대가 같을 때만 한 번' 전한다고 보장하는데, 토네이도 착지 콜백이 소리 재생 앞뒤로 같은 세대 검사를 다시 한다.
- 고칠 방향(검토자 제안): 콜백 안 세대 검사를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C5-16 [낮음] Core 사건 생성자·호출 순서가 보장한 조건을 View 소비자가 다시 검사

- 위치: `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs:126-137`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs:152-157`, `V/Presentation/Movement/CombatStepContactGate.cs:107-114`
- 내용: CombatCharacterEvent 생성자는 Final·Expected를 non-null·같은 ID로, DamageOrigins를 null 원소 없는 배열로 보장한다. 그런데 CombatStepContactGate.IsMoverOwn은 '?.'로 둘을 다시 비교하고, 형식 표는 e.Final?.EntityId ?? e.Expected?.EntityId ?? Empty로, CombatEventSubjects.TryGetKey는 목록·원소 null을, 기차 사망 대응 Prepare는 death.Expected == null·ID 불일치를 다시 본다(늘 거짓). 같은 성격으로 IsSelfDrivenStep의 'Kind == Moved &&'(Of가 이미 걸러 줌), CompleteInterpolation의 !_armed 갈래(유일한 호출 순서상 닿지 않음, float 배열도 할당), BeginPull·TickPull의 _pull == null 검사도 겹친다. 읽는 사람이 '비어 올 수 있다'고 오해한다.
- 고칠 방향(검토자 제안): Core 계약이 보장한 조건의 재검사를 지우고 계약을 주석 한 줄로 밝힌다.
- 검증: 묶음 낮음; 찾은 검토자 S15, S20, S23

### C5-17 [낮음] 연출 설정을 표 생성·표시 조립·부품 생성·Prepare마다 거듭 Validate

- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/CombatPresentationSettingsTable.cs:33`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:81-83`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:179-188`
- 내용: CombatPresentationSettingsData.Validate는 CombatPresentationSettingsTable이 만들 때 돌고, Display Factory와 Preview Factory가 다시 돌린다. 같은 boardReveal 설정은 CombatBoardReveal 생성자와 CombatBoardRevealSchedule.Create(Prepare마다)에서 또, Railway·Nexus·TileCollapse 값도 각 부품 생성자에서 다시 검사된다. 대상 Body null도 한 호출 안에서 세 번 본다. 클립 부속 목록 모양 검사(FindCompanionError)도 RequireCompanionClips와 CombatClipData 생성자에서 두 번 돈다. 같은 Factory에서 출력 Camera 대체식(WorldCamera ?? Camera.main)도 '한 번만 적용' 주석과 달리 두 번 계산한다.
- 고칠 방향(검토자 제안): 검증은 표 생성(입력 계층) 한 곳에서 하고 조립·부품은 검증된 값을 신뢰한다. Camera 대체식은 한 번 구해 나눠 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S19, S24, S26

### C5-18 [낮음] 카메라 설정 SO와 클립 시각 읽기가 같은 검사를 SO와 값 생성자에서 연달아 두 번

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatCameraSettingsSO.cs:48-58`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatCameraSettingsData.cs:28-31`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipEvents.cs:65-70`
- 내용: CombatCameraSettingsSO.CreateData는 FindError를 부른 뒤 같은 인자로 생성자를 부르고, 생성자가 다시 FindError를 부른다. CombatClipEvents.ReadTimings도 FindTimeError 뒤 CombatClipTimings 생성자가 같은 검사를 한다. 에셋 이름 붙이기는 생성자 예외를 감싸 이룰 수 있다(CombatAttackPresentationSO가 그렇게 함).
- 고칠 방향(검토자 제안): 생성자 예외를 잡아 에셋 이름을 붙이고 앞선 Find*Error 호출을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C5-19 [낮음] 적 연출 간격 계산의 늘 참인 삼항식·안 쓰는 인자·한 줄 전달 검증 계층

- 위치: `V/Playback/Support/CombatEnemyPresentationPacing.cs:73-78`, `V/Playback/Support/CombatEnemyPresentationPacing.cs:28-34`, `V/Playback/Support/CombatEnemyPresentationPacing.cs:117-119`
- 내용: EnemyPhaseEnded 갈래는 _turnGapPaid = true 바로 뒤에 '_turnGapPaid ? _turnGapSeconds : 0f'를 계산해 늘 참이다. BeginResult(result)는 인자를 null 검사만 하고 쓰지 않는다. ValidateSeconds·ValidateStepSeconds는 설정 데이터의 조건 함수를 한 줄로 넘기기만 하고, 같은 생성자 안에서도 직접 호출과 전달 계층이 섞여 있다.
- 고칠 방향(검토자 제안): 삼항식을 상수로, 안 쓰는 인자와 전달 계층을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C5-20 [낮음] CombatEndUI가 전체 참조 검사(슬롯 BindOwner 포함)를 한 표시 흐름에서 여러 번 반복

- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:61`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:87`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatEndUI.cs:191`
- 내용: ValidateReferences는 BindUIService·Present·Open에서 매번 불리고, Present 안의 _ui.OpenUI가 Open을 불러 표시 한 번에 두 번 돈다. 매번 슬롯 4개의 IsChildOf·중복 검사·BindOwner·슬롯 ValidateReferences를 다시 하며 검사 함수에 BindOwner 부수효과가 섞여 있다.
- 고칠 방향(검토자 제안): 검사와 BindOwner를 BindUIService 때 한 번으로 모은다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C5-23 [낮음] 전투 중 바뀌지 않는 회전 표를 결과 끝마다 다시 찾고 다시 검증

- 위치: `V/Display/Map/FieldObjectPresenter.cs:62-74`, `V/Display/Map/FieldTerrainPresenter.cs:72-73`, `V/Display/Map/FieldTerrainPresenter.cs:141-152`
- 내용: Object·Terrain Render는 결과가 끝날 때마다 불리고, 그때마다 회전 표를 다시 찾아 회전 수 × 분류 Id 수만큼 검증 루프를 돈다. Terrain 주석도 전투 중 스테이지가 바뀌지 않는다고 적는다.
- 고칠 방향(검토자 제안): 설치 때 한 번 찾아 검증하고 보관한다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C5-U0025 [낮음] CombatCursorSnap의 SnappedCell·Reset은 운영·시험 어디서도 부르지 않음

- 위치: `V/Input/Assist/CombatCursorSnap.cs:34-35`, `V/Input/Assist/CombatCursorSnap.cs:80-84`, `V/Input/CombatInputRouter.cs:185`
- 내용: Router는 PointerMoved만 부른다. SnappedCell 속성과 Reset()은 저장소 전체(시험 포함)에 호출이 없다. 모드가 바뀔 때 기억을 비우는 일은 비활성 PointerMoved가 대신한다. 죽은 멤버가 문서에 '다음 스냅 모드의 첫 칸에서 다시 옮깁니다'라는 경로를 약속하고 있어 오해를 부른다.
- 검증: 묶음 낮음; 찾은 검토자 S11

### C5-U0029 [낮음] 쓰이지 않는 입력 멤버, 그중 HasCurrentAcceptedEmptyMovementRange는 사실을 읽을 때마다 계산되고 추적기 상태까지 지움

- 위치: `V/Input/CombatInputFacts.cs:140-141`, `V/Input/CombatInputFactsReader.cs:12`, `V/Input/CombatInputFactsReader.cs:70`
- 내용: CombatInputFacts.HasCurrentAcceptedEmptyMovementRange는 저장소 전체에서 읽는 곳이 없다(주석도 '수동 턴 종료 자격에는 쓰지 않습니다'라고 적는다). 그런데 CombatInputFactsReader.Read는 호출마다 Interaction → CombatPathInput.HasCurrentAcceptedEmptyMovementRange를 불러 캐릭터 조회와 범위 사실 비교를 한다. 사실이 맞지 않으면 CombatMovementRangeTracker.HasAcceptedEmptyRange가 InvalidateAcceptedRange()로 상태를 지운다. 이는 '캐시·쓰기·입력 정책 판정은 하지 않습니다'라는 읽기 클래스 설명과 다르다. 사실 읽기는 Tick과 입력마다 여러 번 돈다. 이 사실 하나 때문에 추적기 필드 둘·메서드 둘, 경로 입력과 묶음의 메서드, 사실 칸과 생성 인자가 남아 있다. CombatCommandGate.Pending도 읽는 곳이 없고, 그 값만 읽을 PendingInputCommand.Kind도 마찬가지다.
- 고칠 방향(검토자 제안): 빈 범위 근거 사슬(사실 칸·읽기 줄·묶음·경로 입력 메서드·추적기 필드)과 Gate.Pending을 걷는다.
- 검증: 묶음 낮음; 찾은 검토자 S10

### C5-U0047 [낮음] CombatStepContacts의 쓰이지 않는 표면(TryGetSegment·Group.SourceId·Tile 갈래)

- 위치: `V/Presentation/Movement/CombatStepContacts.cs:17-21`, `V/Presentation/Movement/CombatStepContacts.cs:84-90`, `V/Presentation/Movement/CombatStepContacts.cs:113-124`
- 내용: `TryGetSegment`는 저장소 어디에서도(시험 포함) 부르지 않는다. 구간 사전 `_segments`는 중복을 막는 데에만 쓰인다. `Group.SourceId`는 읽는 곳이 없다. `Tile` 원본 갈래는 `TriggersArrivalRule = true`인, 오브젝트가 아닌 원본을 기다린다. 그러나 Core의 MovementTileFact 하위형은 오브젝트 말고 모두 false라 운영에서 닿지 않는다(문서도 '확장점'이라고 적는다). 레인의 몸 조회도 이 종류를 처리하지 않는다.
- 고칠 방향(검토자 제안): 쓰지 않는 조회·필드는 걷는다. Tile 갈래는 Core에 그런 원본이 생길 때 몸 조회와 함께 더한다.
- 검증: 묶음 낮음; 찾은 검토자 S20

### C5-U0065 [낮음] 진입 보류 시작의 기믹 Clear 셋과 _prepared는 운영에서 닿지 않는 경로를 막는 방어 코드

- 위치: `V/Presentation/Entry/CombatEntryDeferredDisplay.cs:35-45`, `V/Presentation/Entry/CombatBoardEntryPresentation.cs:64-75`, `V/Display/CombatSceneDisplay.cs:258-271`
- 내용: Begin 주석은 '재설치에서 남은 표시'를 비운다고 하지만, 표시와 토네이도·낙석·하붑 표시기는 연결마다 새로 만들고 Install도 연결당 한 번입니다. 재설치가 있더라도 _prepared가 PrepareDisplay를 막아 Begin이 불리지 않습니다. 설치 실패는 CombatSceneDisplay의 catch가 이미 셋을 비웁니다. 그래서 Begin의 Clear 셋은 늘 빈 표시를 비우고, 주석의 '재설치' 설명은 맞지 않습니다. 그런데도 기믹 표시기를 하나 더하면 이 목록·RenderNow·설치 실패 목록·Release를 함께 고쳐야 합니다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C5-U0066 [낮음] CombatTornadoAppearance의 닿지 않는 조건과 파괴 직전 복원

- 위치: `V/Presentation/Environment/CombatTornadoAppearance.cs:57-82`, `V/Display/Map/FieldTornadoPresenter.cs:329-348`
- 내용: Refresh의 else-if가 이미 '재생 중 아님·켜짐·위치 다름'을 보장합니다. 그런데 ShowAt이 `_direction >= 0 && 위치 같음`을 다시 봅니다(Refresh에서 오면 늘 거짓). 몸을 끄는 다른 코드가 없어 Refresh의 activeSelf 조건도 초기화 뒤에는 늘 참입니다. 또 FieldTornadoPresenter.Remove는 곧 파괴할 몸에 Reset을 불러 배율·재질 블록을 되돌립니다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C5-U0099 [낮음] InspectUnit이 선택 변경 콜백이 이미 한 HUD 선택·강조 갱신을 다시 함

- 위치: `V/Display/CombatSceneDisplay.cs:466-474`, `V/Display/CombatSceneDisplay.cs:66-70`, `V/Selection/CombatInspectionPresenter.cs:41-62`
- 내용: `_inspection.SelectCell`은 Refresh에서 `_selectionChanged`(= display.RefreshInspectionSelection)를 부르고, 그것이 `SetSelectedEntity`와 `RenderSelectedActor`를 한다. InspectUnit은 그 직후 같은 두 호출을 또 한다.
- 고칠 방향(검토자 제안): InspectUnit에서 SelectCell 뒤의 두 호출을 지우고 콜백 한 곳에 맡긴다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C5-U0108 [낮음] 갱신마다 같은 계산을 두 번 함(칸별 종류 수집, 층별 열린 변)

- 위치: `V/Display/Field/FieldCellFillStack.cs:248-263`, `V/Display/Field/FieldCellFillStack.cs:282-295`, `V/Display/Field/FieldCellFillStack.cs:382-395`
- 내용: FieldCellFillStack.Apply는 칸마다 CollectKinds를 ResolveKindDepths에서 한 번, Stack에서 또 한 번 부릅니다. 또 채우는 종류 13개마다 FieldAttackOutlineOpening.Of/ForHover로 전체 칸을 훑는데, 바로 뒤 FieldOutlineRenderer.Refresh가 슬롯마다 같은 층의 열린 변을 다시 구합니다. 결과는 같으므로 한 번 구해 나눠 쓸 수 있습니다.
- 고칠 방향(검토자 제안): Apply 안에서 층별 열린 변을 한 번 구해 두고 테두리가 조회하게 한다. 칸별 종류도 한 번만 모은다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C5-U0109 [낮음] 셰이더가 덮어써 쓰이지 않는 재질 값과 늘 참인 검사

- 위치: `V/Display/Field/FieldCellFillStack.cs:683-705`, `V/Display/Field/FieldCellFillStack.cs:64-70`, `V/Display/Field/FieldCellFillStack.cs:470-473`
- 내용: ShapedCopy는 _UseHoleShapes를 1로 켜면서 _HoleInset과 _HoleRadius도 넣습니다. 그러나 셰이더는 _UseHoleShapes>0.5이면 _HoleShapes[holeKind-1] 값으로 덮어쓰므로 이 두 값은 쓰이지 않습니다. 그런데도 구성 검사 목록(MaterialProperties)에 들어 있습니다. 또 RefreshHoleMaps의 `depth < _holeMaps.Count`는 MapOf와 HoleMapOf가 늘 함께 만들어지므로 언제나 참입니다.
- 고칠 방향(검토자 제안): 덮어써지는 값은 넣지도 검사하지도 않는다. 늘 참인 조건은 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C5-U0110 [낮음] 호출자 없는 운영 멤버 FieldCellColorRenderer.CellAt

- 위치: `V/Display/Field/FieldCellColorRenderer.cs:150-154`
- 내용: 주석에는 '테스트에서 칠한 색을 확인할 때 씁니다'라고 되어 있지만, Assets 전체를 grep해도 이 메서드를 부르는 곳이 없습니다. 죽은 코드이고 주석도 틀립니다.
- 고칠 방향(검토자 제안): 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C5-U0119 [낮음] CombatUnitInfo.Configure가 쓰지 않는 표 4개와 이미 받은 UI 서비스를 다시 받음

- 위치: `V/Display/Hud/CombatUnitInfo.cs:30-34`, `V/Display/Hud/CombatUnitInfo.cs:79-99`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:96`
- 내용: Configure는 units·traits·categories·statuses를 받아 null만 검사하고 버린다(표 변환은 CombatInspectionInfoMapper가 따로 받음). ui도 BindUISystemService가 이미 _ui에 넣은 것을 다시 덮어쓴다. statuses null 검사는 view.Prepare 등 부작용 뒤(97줄)에 있어 실패 시 일부만 설정된 상태가 남는다. 표 하나를 바꿀 때 Factory·패널 시그니처를 함께 고쳐야 하는 헛 인자다.
- 고칠 방향(검토자 제안): Configure를 (selection)만 받게 줄이고 UI 서비스는 BindUISystemService 한 경로로만 받는다.
- 검증: 묶음 낮음; 찾은 검토자 S05

### C5-U0129 [낮음] 같은 프레임에 칸 변경과 전체 변경이 겹치면 GridOutline이 전체 Refresh를 두 번 함

- 위치: `V/Display/Field/FieldView.cs:343-365`, `V/Display/Field/FieldOutlineRenderer.cs:207-214`, `V/Display/Field/FieldOutlineRenderer.cs:244-268`
- 내용: FieldOutlineRenderer.Repaint는 바뀐 칸 목록을 보지 않고, 칸이 하나라도 있으면 Refresh를 부른다. FlushPending은 Repaint 뒤에 _pendingRefresh가 있으면 Refresh를 또 부른다. 두 번째 Refresh는 모든 슬롯의 Collect(전체 칸 순회)와 열린 변 계산(FieldAttackOutlineOpening의 전체 칸 순회)을 다시 한다. 유닛 호버처럼 SetHoverCell과 SetHoverRangeSourceCells가 같은 프레임에 오는 흔한 경우에 생긴다.
- 고칠 방향(검토자 제안): FlushPending에서 refresh가 있으면 Repaint를 건너뛰고 Refresh 한 번만 부르거나, 표시기가 '전체 다시' 한 번으로 합치게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C5-U0130 [낮음] FieldOutlineRenderer.CanReuse의 설정 비교가 운영에서는 늘 참

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:149-158`, `V/Display/Field/FieldOutlineRenderer.cs:35-37`, `V/Display/Field/FieldView.cs:573-586`
- 내용: CombatSceneDisplay는 readonly _fieldConfig 하나로만 FieldView.Build를 부르고, 개발용 미리보기도 표 하나를 쓴다. 그래서 운영에서 CanReuse는 늘 같은 설정 객체를 받아 참이 된다. 그런데도 데칼·간격·채우기·고리 모양·경로 재질을 하나씩 비교하고, 두께·높이·점선 값은 비교하지 않는다(TP-14). 새 설정값을 넣을 때마다 비교에 넣을지 정해야 하는 유지 비용만 남는다.
- 고칠 방향(검토자 제안): 표시기가 처음 받은 설정 객체와 같은지(참조 비교)만 확인하거나, 설정 객체가 바뀌면 표시기를 새로 만든다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C5-U0131 [낮음] 같은 칸 간격을 세 필드에 들고, 쓰이지 않는 운영 멤버가 남음

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:54-56`, `V/Display/Field/FieldOutlineRenderer.cs:111-112`, `V/Display/Field/FieldOutlineRenderer.cs:160-170`
- 내용: FieldOutlineRenderer는 config.OutlineCellSpacing을 _cellSpacing(비교용)과 _configuredSpacing에 두고, Build에서 _spacing = MeasureSpacing()(= _configuredSpacing)으로 한 벌 더 만든다. 세 값은 늘 같다. FieldView.HighlightState(설명은 '표시기·테스트용')는 저장소 어디에서도 읽지 않는다(표시기는 Build 인자로 상태를 받는다). FieldTrajectoryLine.IsShown도 쓰는 곳이 없다.
- 고칠 방향(검토자 제안): 간격은 _configuredSpacing 하나만 두고, 쓰지 않는 HighlightState·IsShown은 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C5-U0144 [낮음] FieldThornPresenter.ActiveCount·TryGet에 호출자가 없음

- 위치: `V/Display/Map/FieldThornPresenter.cs:58-58`, `V/Display/Map/FieldThornPresenter.cs:162-172`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:153-155`
- 내용: Assets 전체(시험 포함)를 grep해도 `.ActiveCount` 호출이 없고, 가시 표시의 TryGet(cell, out GameObject)를 부르는 곳도 없다. 조립 코드는 Consume·BodyOf·Sync·Clear·Release만 쓴다.
- 고칠 방향(검토자 제안): 두 멤버를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C5-U0145 [낮음] 가시 표시만 SetActive·SetParent 재진입 방어 기계를 따로 가짐

- 위치: `V/Display/Map/FieldThornPresenter.cs:33-43`, `V/Display/Map/FieldThornPresenter.cs:270-320`, `V/Display/Map/FieldThornPresenter.cs:330-363`
- 내용: FieldThornPresenter에는 다음이 있다. 가시성 깊이 `_visibilityDepth`와 파괴 대기열 `_pendingDestruction`·`_destroying`, SetActiveSafely, SetParent 뒤 CanContinue 버전 검사, Return 안 단계마다 `_released` 재확인, Instantiate 직후의 `gameObject == null` 검사(늘 거짓)다. 같은 CustomPool 수명을 쓰는 Object·Terrain 표시는 같은 상황(외형 프리팹의 OnEnable/OnDisable)을 이렇게 막지 않는다. 그래서 가시 한 종류에만 약 150줄의 방어가 붙어 있다. 정책이 일관되지 않고, 읽고 고치는 비용만 늘어난다.
- 고칠 방향(검토자 제안): 다른 표시와 같은 단순한 대여·반환으로 줄이거나, 재진입 방어가 정말 필요하면 공용 부품으로 올려 정책을 하나로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C5-U0158 [낮음] 호출자가 없는 Count/ActiveCount와 한 줄 전달 ResolveKind

- 위치: `V/Display/Map/ObjectViewSet.cs:32`, `V/Display/Map/ObjectViewSet.cs:134`, `V/Display/Map/TerrainViewSet.cs:40`
- 내용: ObjectViewSet.Count, TerrainViewSet.Count, FieldTrailPresenter.ActiveCount는 저장소 전체(시험 포함)에서 부르는 곳이 없다. ObjectViewSet.ResolveKind는 EnvironmentVisualKinds.ObjectKind를 그대로 넘기는 internal static 래퍼이고, FieldObjectPresenter는 이것을 제네릭 타입 이름을 붙여 부른다(ObjectViewSet<Instance>.ResolveKind).
- 고칠 방향(검토자 제안): 쓰지 않는 Count 멤버는 지우고, ResolveKind 대신 EnvironmentVisualKinds.ObjectKind를 직접 부른다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C5-U0165 [낮음] 구조물 HUD는 사실 그리기마다 내용 전체를 다시 씀(유닛 HUD의 GH3 캐시와 다름)

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:428-455`, `V/Display/Unit/FieldUnitPresenter.cs:470-476`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs:230-249`
- 내용: 유닛 HUD는 ShowsHudFacts로 같은 사실이면 다시 쓰지 않지만, 구조물은 RenderFacts마다 SetStructure(아이템·부가 표시 초기화 포함)와 GetUnitVisual·SetStructureInfo(이름 문자열·정보 패널 복사)를 매번 한다. RenderFacts는 캐릭터 사건 처리기 여러 곳에서 사건마다 불린다.
- 고칠 방향(검토자 제안): 구조물도 마지막에 쓴 StructureCombatFacts 참조를 기억해 같으면 보이기만 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C5-U0181 [낮음] 대여 5종이 같은 시작 조건·IsCurrent 식을 각자 복제하고, 칸이 이미 지키는 조건을 다시 검사함

- 위치: `V/Display/Unit/FieldUnitView.cs:273-313`, `V/Display/Unit/FieldUnitView.cs:374-385`, `V/Display/Unit/FieldUnitView.cs:418-429`
- 내용: Begin* 다섯 곳이 'Current != null이면 예외'를 반복한다. 이 검사는 UnitPresentationLeaseSlot.Hold도 한다. 'EntityId.IsEmpty || !activeInHierarchy' 검사도 다섯 곳에 있다. 중첩 대여 클래스 다섯도 같은 IsCurrent 식(`_view != null && EntityId 비교 && slot.IsCurrent`)을 각자 갖는다. 그런데 Bind·ResetForPool·OnDisable이 EntityId를 바꾸거나 꺼지기 전에 항상 칸을 비운다. 그래서 IsCurrent의 EntityId 비교와 IsAvailable의 activeInHierarchy는 칸 참조 비교가 이미 보장하는 조건을 다시 본다.
- 고칠 방향(검토자 제안): 시작 조건을 FieldUnitView의 RequireLeasable() 하나로, IsCurrent는 칸 참조 비교로 줄인다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C5-U0182 [낮음] 예상 체력 지우기와 표시 정리가 같은 경로에서 두 번 돎

- 위치: `V/Display/Unit/FieldUnitView.cs:179-184`, `V/Display/Unit/FieldUnitView.cs:210-216`, `V/Display/Unit/FieldUnitView.cs:249-260`
- 내용: RenderFacts가 HudFeedback.ClearAttackHealthPreview()로 미리보기를 지운 뒤에도 Apply가 유닛마다 다시 SetAttackHealthPreview(null)을 부른다. 그 안에서 UnitHUD.SetHealthPreview(null)이 또 불린다. ResetForPool도 ReleasePresentation을 부른 뒤 SetActive(false)를 하는데, 이때 OnDisable이 ReleasePresentation을 한 번 더 돌린다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C5-U0183 [낮음] 재생/편집 분기 Destroy 도우미가 여러 벌이고, 외형 클래스의 도우미를 다른 기능이 빌려 씀

- 위치: `V/Display/Unit/FieldUnitView.cs:144-145`, `V/Display/Unit/UnitModelAppearance.cs:308-313`, `V/Display/Unit/FieldUnitPresenter.cs:798-845`
- 내용: `if (Application.isPlaying) Destroy else DestroyImmediate`가 FieldUnitView·FieldUnitPresenter에 인라인으로 있고, 같은 일을 하는 private 도우미가 여러 파일에 따로 있다. UnitModelAppearance.DestroyOwned는 외형 부품의 내부 도우미인데 PlacementGhost·FieldThornPresenter 쪽이 빌려 쓴다. 그래서 외형 클래스를 바꾸면 관계없는 기능까지 영향을 받는다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C5-U0192 [낮음] 쓰이지 않는 공개 멤버(TargetMode/Mode, HasRangeAnswer, IsShown, ItemKey)

- 위치: `V/Input/Interaction/CombatInteraction.cs:44-48`, `V/Input/Interaction/CombatInteraction.cs:119-123`, `V/Input/Interaction/AttackTargetingInteraction.cs:101-102`
- 내용: CombatInteraction.TargetMode 열거형과 Mode 속성, CombatInteraction.HasRangeAnswer와 AttackTargetingInteraction.HasRangeAnswer, CombatUnitRangeHover.IsShown, CombatItemDragInteraction.ItemKey는 저장소 전체(Assets/Tests 포함)에서 읽는 곳이 없다. CombatActionAvailabilityTracker.State도 파일 밖에서 읽지 않는다.
- 고칠 방향(검토자 제안): 지우거나 private로 좁힌다.
- 검증: 묶음 낮음; 찾은 검토자 S12

### C5-U0206 [낮음] CombatMovementInteraction·범위 요청에 호출자 없는 멤버와 늘 같은 세대 검사가 남음

- 위치: `V/Input/MovePath/CombatMovementInteraction.cs:51`, `V/Input/MovePath/CombatMovementInteraction.cs:88-104`, `V/Input/MovePath/CombatMovementInteraction.cs:204-209`
- 내용: CombatMovementInteraction의 FocusLost()·CaptureLost()·Snapshot()은 운영 코드에도 시험 코드에도 호출자가 없다(CombatPathInput은 자기 FocusLost에서 CancelInputs를 부른다). Attach는 생성자에서 한 번만 불리고, Detach 뒤에는 _attached=false라 EnsureLive가 먼저 막는다. 그래서 _lifetime/_requestLifetime 비교(207)는 결과를 바꾸지 못한다. CombatMovementRangeRequest의 _generation도 마찬가지다. Tracker가 늘 최신 세대를 들고 있거나, Drop/Clear 때 요청을 함께 취소하므로 TryConsume의 `generation != _generation` 검사는 IsPending/MatchesFacts 검사에 가려 결과에 영향이 없다.
- 고칠 방향(검토자 제안): 호출자 없는 메서드를 지우고, 수명·세대 카운터를 _attached와 요청 번호 하나로 줄인다.
- 검증: 묶음 낮음; 찾은 검토자 S13

### C5-U0222 [낮음] CombatMovementPlayback 중간 계층이 닿지 않는 검사만 더하고 취소 정리를 두 번 돌게 함

- 위치: `V/Playback/Ordering/CombatMovementPlayback.cs:21-49`, `V/Playback/Ordering/OrderedCombatPresentation.cs:185-214`, `V/Playback/Ordering/OrderedCombatPresentation.cs:64-66`
- 내용: OrderedCombatPresentation은 같은 객체(_eventPlayback)를 ICombatEventPlayback과 CombatMovementPlayback(내부 presenter) 두 길로 쓴다. Cancel은 movement.Detach()로 CancelMovement를 부르고 이어서 CancelPresentation을 부르는데, CombatSceneEventPlayback.CancelPresentation 안에서 _events.CancelPresentation이 다시 CancelMovement가 되므로(Presenter 357) 기차 접촉 취소·_railway.Cancel·사건 Presenter의 CancelMovement(세대 증가·재생기 CancelAll·이펙트 StopAll) 전체가 한 번의 취소에 두 번 돈다. CombatMovementPlayback의 _detached 검사는 Cancel이 _movement를 먼저 null로 바꾸므로 닿지 않고, 종류 검사는 RouteOf가 이미 Step/Return으로 걸렀으며 null 검사는 SceneEventPresenter.BeginStep이 다시 한다. 호출자는 OrderedCombatPresentation 하나뿐이고 Attach와 Play 두 곳에서 따로 만든다.
- 고칠 방향(검토자 제안): CombatMovementPlayback을 없애고 _eventPlayback에 BeginStep/ShowReturn을 바로 부르며, Cancel은 CancelPresentation 한 번만 부른다(CancelPresentation이 이동 취소를 포함한다는 계약을 문서화).
- 검증: 묶음 낮음; 찾은 검토자 S15

### C5-U0224 [낮음] 쓰이지 않는 멤버와 운영에서 닿지 않는 ObjectCreated 갈래

- 위치: `V/Playback/Ordering/CombatConcurrentGroups.cs:40-41`, `V/Playback/Ordering/CombatEventMoment.cs:18-19`, `V/Playback/Ordering/CombatEventTypeTable.cs:177-178`
- 내용: CombatConcurrentGroups.Count는 슬라이스 안팎·시험 어디에서도 읽지 않는다. CombatObjectCreatedEvent는 Core가 만들지 않고(생성자 호출은 시험 하나뿐, CombatEventMoment 주석도 '지금 Core는 이 사건을 내지 않습니다'), 그런데도 형식 표 줄·순간 종류·큐 답·소리 표(슬롯 null)가 이 갈래를 유지한다.
- 고칠 방향(검토자 제안): Count를 지우고, ObjectCreated는 Core가 실제로 낼 때 넣도록 갈래를 걷어내거나(표에 없으면 Skip 경고) 남겨 두는 이유를 한 곳에 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C5-U0240 [낮음] 운영에서 닿지 않는 경로: 옛 사냥 표식 사건 소리 갈래, CombatMovementPresentation.Prepare(CombatResult)

- 위치: `V/Playback/Support/CombatEventSounds.cs:127-129`, `V/Playback/Support/CombatMovementPresentation.cs:44-48`, `V/Playback/CombatSceneEventPresenter.cs:178-180`
- 내용: CombatHuntersMarkEvent는 시험 코드에서만 생성되고(new 검색 결과는 Tests 두 파일뿐) Core 운영 코드는 만들지 않는데, 소리 표가 '옛 기록' 갈래를 유지한다. CombatMovementPresentation.Prepare(CombatResult)는 운영 조립이 PreparePlacements(공유 계획)만 불러 쓰이지 않는다.
- 고칠 방향(검토자 제안): Core가 그 사건을 더 내지 않는다면 소리 갈래를 지우고, 쓰지 않는 Prepare 오버로드도 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L14, S16

### C5-U0241 [낮음 (기반 한계)] 연결 대기 SFX 칸이 null이라 오브젝트·추락·아이템·비행·피격·토네이도 예고 소리가 늘 None

- 위치: `V/Playback/Support/CombatEventSounds.cs:196-207`, `V/Playback/Support/CombatEventSounds.cs:239-240`, `S/RuntimeSystem/GlobalSystem/AudioSystem/AudioSystem.cs:16`
- 내용: Slotted가 null 칸이면 None을 돌려주므로, 프리셋을 DB에서 읽어도 일곱 종류의 소리 경로는 운영에서 결과가 늘 같다. AudioSystem(GlobalSystem)의 SfxEvent에 그 칸이 없는 것이 원인이라 기반 한계에 해당한다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C5-U0248 [낮음] 운영에서 쓰이지 않는 완료 정책·멤버가 남아 있음

- 위치: `V/Presentation/Attack/CombatHitGate.cs:11-21`, `V/Presentation/Attack/CombatHitGate.cs:54-55`, `V/Presentation/Attack/CombatHitGate.cs:82-87`
- 내용: CombatHitCompletion.Fired·AnimationEnd를 넘기는 곳이 저장소 어디에도 없다. 공격 모양 두 공장과 포효 모두 FirstRelease만 쓴다. 그래서 CombatHitGate.NotifyFired와 그 정책 분기는 닿지 않는다. 다음 멤버도 호출자가 없다: CombatHitGate.IsActive, CombatProjectilePool.ActiveCount, 인자 4개짜리 FireLinear 오버로드, CombatProjectileLease.Instance, CombatAttackPresenter.Clear(Cancel의 별칭), CombatAttackTarget.Resolve(in AttackResult…) 오버로드, CombatAttackShape.PrimaryTargetId 속성(생성자 안에서만 씀).
- 고칠 방향(검토자 제안): 쓰이지 않는 열거 값·NotifyFired·IsActive·ActiveCount·오버로드·별칭을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L07, S17

### C5-U0249 [낮음] 없어진 Ballista 관통 경로를 위해 일반화한 코드가 남아 있음

- 위치: `V/Presentation/Attack/CombatProjectileFlights.cs:74-100`, `V/Presentation/Attack/CombatProjectileFlights.cs:156-208`, `V/Presentation/Attack/CombatAttackPlayback.cs:321-356`
- 내용: CombatProjectileFlights.FireThrough는 여러 접촉 순간(목록·오름차순 검사·커서)을 받도록 일반화되어 있다. 그러나 주석이 스스로 'FireLinear(한 순간)만 부른다'고 적는다. CombatAttackPlayback.ContactProgress/WarnContact도 호출자가 LinearContactProgress 하나뿐이고, reason 인자에는 늘 같은 문자열이 들어간다. 주석에 따르면 이 일반화는 옛 관통 경로용이었다.
- 고칠 방향(검토자 제안): Shot이 맞는 순간 하나(hitAt, hit)만 갖게 줄이고, ContactProgress를 LinearContactProgress 안으로 합친다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C5-U0276 [낮음] 쓰이지 않는 생성자·콜백 인자·속성(죽은 코드)

- 위치: `V/Presentation/Pieces/CombatClipEventCursor.cs:39-43`, `V/Presentation/Pieces/CombatClipPlayback.cs:23-43`, `V/Presentation/Pieces/CombatClipPiece.cs:40-41`
- 내용: CombatClipEventCursor(float,float,float) 생성자는 운영 코드에도 시험 코드에도 호출자가 없다. CombatClipPlayback의 ended 인자와 _ended 필드는 유일한 호출자(CombatClipPiece)가 항상 null을 넘기므로 늘 null이다(끝은 IsComplete로 본다). eventTiming2·3의 '기존 호출은 받지 않아도 됩니다'용 선택 인자도 호출자가 하나뿐이다. CombatPieceContext.Version은 어떤 조각도 읽지 않는다(Mapper는 CombatStandMovePiece가 읽음).
- 고칠 방향(검토자 제안): 3-float 생성자, ended 인자·필드, Context.Version을 지운다. 이벤트 콜백은 C2 수정(Id 키 콜백 하나)과 함께 정리한다.
- 검증: 묶음 낮음; 찾은 검토자 S21

### C5-U0295 [낮음] 기차 경로·접촉 대상의 옛 '점 몸' API가 호출자 없이 남아 있음

- 위치: `V/Presentation/Railway/RailwayTravelPath.cs:58-71`, `V/Presentation/Railway/RailwayContactSchedule.cs:93-97`, `V/Presentation/Railway/RailwayContactSchedule.cs:111-115`
- 내용: RailwayTravelPath.ContactTime(Vector3)·TryGetContactTime(Vector3)와 RailwayContactSchedule.Target(EntityId, cells, Vector3) 생성자·PointOf는 저장소 전체(시험 포함)에서 호출자가 없다. 운영 경로는 CombatBody를 받는 오버로드만 쓴다(CombatRailwayContactPlayback 114행). 주석도 '옛 중심 기준과 같은 시각'이라 스스로 옛 경로임을 밝힌다.
- 고칠 방향(검토자 제안): Vector3 오버로드 세 개와 PointOf를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C5-U0297 [낮음] 반응 Presenter는 항목마다 새로 만들어 한 번만 쓰는데 재사용용 장치를 품음

- 위치: `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:28-31`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:67-71`, `V/Presentation/Reaction/CombatUnitReactionSet.cs:18-31`
- 내용: CombatUnitReactionSet.Entry가 반응마다 new CombatUnitReactionPresenter()를 만들고 완료·취소 때 Release한다. 그래서 Presenter의 재시작 검사(IsPlaying·_cleaning)와 공개 Version 속성은 운영에서 쓰이지 않는다. Version은 저장소에서 읽는 곳이 없다(CombatReactionPlayer는 Set의 Version을 읽음). 세대 비교 장치가 Set·Presenter 두 층에 겹친다.
- 고칠 방향(검토자 제안): Presenter.Version을 지우고, 단일 사용으로 굳히거나 Set이 Presenter를 재사용하게 한쪽으로 정한다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C5-U0309 [낮음] SelectedActorCells가 같은 ID로 사실 조회를 두 번 함

- 위치: `V/Selection/CombatSelectionState.cs:17-20`, `V/Display/CombatSceneDisplay.cs:363-370`
- 내용: SelectedActorCells는 SelectedActorId 속성(안에서 TryGet)을 부른 뒤 같은 ID로 TryGet을 또 한다. 강조 갱신(RenderSelectedActor)마다 조회가 두 번 돈다.
- 고칠 방향(검토자 제안): 한 번 TryGet한 facts로 자격과 칸을 함께 판정한다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C5-U0318 [낮음] 호출자 없는 옛 입구가 남아 있다(Read·FindTimeError·3시각 생성자·Sound·KnownRule)

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipEvents.cs:13-23`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipData.cs:20-24`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipData.cs:55-61`
- 내용: CombatClipEvents.Read(out 3개), CombatClipData.FindTimeError, CombatClipData의 세 float 생성자, CombatEventCues.Sound(호환 읽기), CombatPresentationEffectRules.KnownRule은 운영·시험 코드 어디에서도 부르지 않는다(Tool/Editor의 주석 참조뿐). CombatClipData.FindTimeError 주석은 '이 입구는 기존 호출에 남깁니다'라고 하지만 남은 호출이 없다.
- 고칠 방향(검토자 제안): 다섯 멤버를 지우고 CharacterClipCopier 주석의 참조를 ReadTimings·CombatClipTimings.FindTimeError로 고친다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C5-U0380 [낮음] 기능을 걷어 낸 뒤 남은 클래스·상수·인자(행동 상태 문구, 목표 상태 칸, traitVisuals)

- 위치: `V/Display/Hud/CombatActionStateText.cs:8-51`, `V/Display/Unit/CombatObjectiveDisplay.cs:7-38`, `V/Display/Unit/FieldUnitPresenter.cs:129-133`
- 내용: CombatActionStateText는 정적 클래스 전체(행동 상태와 Core 거절 사유를 문구로 바꾸는 switch 두 개)를 참조하는 곳이 없습니다. 같은 거절 사유 문구 대응은 CombatActionNotice에도 따로 있어서 읽는 사람이 어느 쪽이 원본인지 헷갈립니다. CombatObjectiveDisplay의 ObjectiveStatusKey·ObjectiveStatusDescription·DescribeTrait은 'HUD 상태 칸에 격파 목표·특성을 표시하지 않는다(UH1)'로 바뀐 뒤 쓰는 곳이 없습니다. 쓰이는 것은 TargetOf뿐인데 클래스 doc은 여전히 '유닛 HUD 상태 칸 문구를 정합니다'라고 합니다. FieldUnitPresenter.Configure의 traitVisuals 인자는 doc에 '호환용, 사용하지 않음'이라고 적혀 있는데도 두 Factory가 tables.Traits를 계속 넘깁니다.
- 고칠 방향(검토자 제안): CombatActionStateText와 목표 상태 칸 상수·DescribeTrait을 지우고 클래스 doc을 TargetOf에 맞게 고칩니다. traitVisuals 인자와 두 Factory의 인자 전달도 지웁니다.
- 검증: 묶음 낮음; 찾은 검토자 L07, S05, S08

### C5-U0381 [낮음] 덱 카드 Hover 경로 끝이 빈 메서드라 사슬 전체가 헛일임

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:84-85`, `V/Display/Unit/FieldUnitPresenter.cs:578-581`, `V/Input/UiEntry/CombatDeckHoverInput.cs:15-62`
- 내용: DeckCardList(GlobalSystem)가 ICombatViewService.DeckHover를 부르면 CombatViewConnection을 거쳐 CombatDeckHoverInput.Hover가 돕니다. Hover는 hover 때마다 Buffer의 모든 캐릭터를 순회해 소유 ID에 맞는 필드 개체를 찾고 try/catch로 오류 경계에 보고합니다. 그런데 그 끝인 FieldUnitPresenter.SetDeckHoveredUnit은 빈 본문입니다('HUD의 화살표 표시는 제거되었다'). Release·Presenter.Release도 이 빈 메서드를 부릅니다. CombatDeckHoverInput의 클래스 doc은 여전히 'HUD Arrow만 표시합니다'라고 합니다.
- 고칠 방향(검토자 제안): Hover 표시를 되살릴 계획이 없으면 CombatDeckHoverInput·ICombatDeckHoverDisplay·연결 상태의 DeckHover를 걷어 냅니다. 서비스 계약의 DeckHover는 null이나 Null Object를 돌려주게 하거나, GlobalSystem 쪽과 맞춰 뺍니다.
- 검증: 묶음 낮음; 찾은 검토자 L03, L07, S08

### C7-03 [낮음] 사건·걸음마다 모든 유닛·구조물·유지 이펙트·선택 정보창을 통째로 다시 그림

- 위치: `V/Playback/CombatSceneEventPresenter.cs:407-441`, `V/Playback/Support/CombatShownFacts.cs:221-226`, `V/Playback/Support/CombatShownFacts.cs:323-328`
- 내용: PresentEvent는 ApplyFacts 뒤 모든 사건에서 _facts.Render()를 부른다. 유닛 사실을 바꾸지 않는 사건(가시 소모·오브젝트 발동·지형·흔적)에서도 마찬가지다. 걸음 레인도 StartStep·FinishStep마다 Render를 부른다. Render 한 번이 하는 일은 다음과 같다. ① FieldUnitPresenter.RenderFacts가 모든 캐릭터·구조물을 돈다. ② FieldUnitView.Apply가 매번 SetAttackHealthPreview(null)로 예상 체력이 없었어도 UnitHUD.SetHealthPreview를 부르고, 그 결과 체력 글자·바가 다시 쓰인다. ③ 구조물 HUD는 사실 기억 없이 매번 다시 쓴다. ④ 유지 이펙트 출처를 모든 유닛에서 다시 모은다. ⑤ 선택 유닛이 있으면 정보창 객체와 문자열을 새로 만든다. UnitHUD 내부 동작은 기반 쪽이지만, 바뀌지 않은 유닛까지 매번 부르는 것은 View의 선택이다(할당 부분은 C11 성격).
- 실패·변경 시나리오: 적 6명이 3칸씩 걷고 공격하는 적 턴에는 Render가 수십 번 돈다. 그때마다 유닛·구조물 20여 개의 HUD 체력 글자·바·정보 패널을 다시 쓰지만, 사건마다 실제로 바뀐 유닛은 한두 명이다. 즉시 끝나는 사실 사건이 이어지면 한 Tick 안에서 사건 수만큼 전체 그리기가 겹쳐 그 프레임이 끊긴다.
- 고칠 방향(검토자 제안): 처리기가 바꾼 개체 ID만 다시 그리는 입구를 두고, 유닛 사실을 바꾸지 않는 사건은 유닛 그리기를 건너뛴다. SetAttackHealthPreview는 예상 체력이 이미 없으면 HUD를 부르지 않는다. 구조물 HUD에도 사실 기억을 두고, 정보창은 선택 유닛의 사실이 바뀐 때만 다시 만든다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L10

## C6 C#·Unity 권장 구조 — 중간 0 · 낮음 20

### C12-26 [낮음] 보드 등장 모드 이름 오타 Coner(Corner)가 열거·직렬화 칸·툴팁·메서드 이름에 퍼짐

- 위치: `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatBoardRevealMode.cs:3-8`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatBoardRevealSettings.cs:12-15`, `V/Presentation/Entry/CombatBoardRevealSchedule.cs:82-127`
- 내용: CombatBoardRevealMode.Coner, 직렬화 칸 conerBatchGrowth, 툴팁 'Coner', 메서드 SetConer·ConerDepth·CompareConer에 오타가 있다. Inspector에 'Coner'로 보이고 'Corner'로 검색하면 걸리지 않는다. 직렬화 칸이라 나중에 고치려면 FormerlySerializedAs가 필요하다. 주석보다 식별자 이름 문제라 주된 기준은 C6(관례)에 가깝다.
- 고칠 방향(검토자 제안): Corner로 바꾸고 직렬화 칸에 FormerlySerializedAs를 단다(열거 값 1은 유지).
- 검증: 묶음 낮음; 찾은 검토자 S19

### C6-01 [낮음] CombatInputSurfaces가 Unity 객체(씬 루트·UI)에 `?.`를 써서 파괴 판정을 거치지 못함

- 위치: `V/Input/ActionButtons/CombatInputSurfaces.cs:33-51`, `V/Input/ActionButtons/CombatInputSurfaces.cs:57-76`, `V/CombatSceneRoot.cs:44-50`
- 내용: CombatInputSurfaces는 BindRoot·UnbindRoot·Sync·Hide에서 `root.BattleChanger?.`, `_root?.Pack?.`, `_root?.UnitInfo?.`처럼 MonoBehaviour 참조에 `?.`를 쓴다. 같은 클래스의 `root == null`·`_root == null`은 Unity 비교라 두 판정이 섞여 있다. 주석(37)대로 이 null 허용은 UI 없는 가짜 Display 시험 루트 때문인데, 운영 루트는 CombatViewConnection.BindRoot에서 null이면 예외가 나고 CombatSceneRoot.ValidateReferences가 UI 참조를 이미 보장한다. `?.`는 파괴된(fake null) 객체를 거르지 못하므로 정리 경로(UnbindRoot·Hide)에서 이미 파괴된 UI가 남아 있으면 그 메서드로 들어가 MissingReferenceException이나 원인과 다른 예외가 난다. 같은 범위의 CombatSceneDisplay 정리 경로(792-797)는 Unity null 비교를 쓴다.
- 고칠 방향(검토자 제안): 운영 경로는 ValidateReferences 보장에 기대어 `?.`를 지우고 그대로 부른다. 시험 루트에는 빈 UI를 붙인다. null 허용이 꼭 필요하면 `!= null` Unity 비교로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 L08, L12, S10

### C6-02 [낮음] 정보창 HP 막대 감소 연출을 FixedUpdate·fixedDeltaTime으로 돌림

- 위치: `V/Display/Hud/CombatUnitInfoView.cs:316-319`, `V/Display/Hud/CombatUnitInfo.cs:193`, `S/RuntimeSystem/GlobalSystem/UISystem/Base/HealthFillAnimation.cs:59-75`
- 내용: CombatUnitInfoView는 UI 연출인 HP Fill 감소를 FixedUpdate에서 Time.fixedDeltaTime으로 진행한다(고정 간격 0.02초=50Hz). 렌더 프레임과 고정 단계가 달라 60Hz 이상에서는 일부 프레임이 이전 값 그대로 남아 끊겨 보이고, 50Hz 아래에서는 한 프레임에 여러 번 쓴다. 같은 패널의 슬라이드(CombatUnitInfo.Update)는 unscaledDeltaTime을 쓰는데 HP 막대는 scaled 시간이라, 옵션 창이 timeScale을 0으로 둔 동안 막대만 멈춘다. HealthFillAnimation.FixedTick은 경과 시간을 받는 API라 Update에서 불러도 된다. 같은 꼴의 GlobalSystem UnitHUD는 범위 밖.
- 고칠 방향(검토자 제안): Update(또는 LateUpdate)에서 Time.unscaledDeltaTime으로 FixedTick을 부르고, 진행 중일 때만 Fill을 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 L09, S05

### C6-04 [낮음] FieldUnitView.MortarWeapon이 읽을 때마다 GetComponent

- 위치: `V/Display/Unit/FieldUnitView.cs:498-500`, `V/Display/Unit/CombatUnitPresentationWindow.cs:288-296`
- 내용: ICombatUnitPresentationTarget.MortarWeapon이 속성 접근마다 GetComponent<CatusCannonWeaponView>()를 부른다. 곡사 공격 한 번에 TryGetMortarWeapon이 준비·발사 두 곳에서 불려 같은 조회를 되풀이한다. 같은 파일의 clipChannel·appearance는 준비 때 한 번 잡아 두어 관례가 다르다.
- 고칠 방향(검토자 제안): PrepareClipChannel처럼 생성·준비 때 한 번 찾아 필드에 둔다(없으면 null).
- 검증: 묶음 낮음; 찾은 검토자 L08

### C6-05 [낮음] CombatSceneRoot의 문자열 반환 검사 함수가 예외를 던지는 하위 검증을 부름

- 위치: `V/CombatSceneRoot.cs:70-108`, `V/CombatSceneRoot.cs:111-130`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/TurnDisplayUI.cs:140-146`
- 내용: FindMissingReference는 빠진 참조 이름을 돌려주는 함수인데 안에서 turnDisplay.ValidateReferences·completion.ValidateReferences(예외를 던짐)를 부른다(88·101). 에디터 검사 ValidateEditorReferences는 경고만 남기려는 경로인데, 하위 조건 하나가 어긋나면 EditorApplication.delayCall 안에서 예외가 나고 뒤 검사(ghost·actionPanel·하위 FindMissingReference)는 건너뛴다. 운영 검사도 하위 예외가 그대로 나가 '[CombatSceneRoot] 씬 참조가 비어 있습니다' 형식과 섞인다.
- 고칠 방향(검토자 제안): 하위 컴포넌트에 문자열을 돌려주는 Find 형태 검사를 두고 FindMissingReference는 그것만 부르게 하거나, 에디터 경로에서 하위 예외를 받아 경고로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S01

### C6-06 [낮음] CombatBodyTextPool.Rental.Dispose가 멱등이 아님

- 위치: `V/Presentation/Text/CombatBodyTextPool.cs:10-24`, `V/Presentation/Text/CombatBodyTextPool.cs:115-126`, `V/Display/Unit/UnitClipChannel.cs:240-243`
- 내용: Rental.Dispose는 _owner.Return을 바로 부르고, Return은 중복 반환이면 InvalidOperationException, 풀 해제 뒤면 ObjectDisposedException을 던진다. .NET Dispose 규약(여러 번 불러도 안전)과 다르고, 범위 안 다른 IDisposable(UnitClipChannel 등)은 _disposed 가드로 멱등이라 일관성도 깨진다. 지금은 CombatBodyTextWindow가 대여를 혼자 갖고 순서대로 돌려 실제 결함은 없다.
- 고칠 방향(검토자 제안): Rental에 반환 여부 깃발을 두어 두 번째 Dispose는 무시하고, 소유권 위반 검사는 별도 Return 경로로 남긴다.
- 검증: 묶음 낮음; 찾은 검토자 L08

### C6-07 [낮음] CombatTornadoLiftTarget struct의 불변식이 생성자에만 있어 default 값이 검사를 건너뜀

- 위치: `V/Presentation/Environment/CombatTornadoLiftTarget.cs:11-33`, `V/Presentation/Pieces/CombatTornadoLiftPiece.cs:44`
- 내용: readonly struct가 중심 대리자·설정 null을 생성자에서만 막는다. default 값은 _center가 null이라 Center를 읽으면 NullReferenceException이 난다. 그래서 소비자 CombatTornadoLiftPiece가 `target.Settings ?? throw`로 다시 막는다. 대리자를 어차피 할당하므로 struct 이점이 없고, class라면 겹친 검사가 필요 없다.
- 고칠 방향(검토자 제안): sealed class로 바꾸고 소비자의 재검사를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C6-08 [낮음] CombatTornadoLiftPiece가 인자에서 온 null 설정을 InvalidOperationException으로 던짐

- 위치: `V/Presentation/Pieces/CombatTornadoLiftPiece.cs:36-45`, `V/Presentation/Pieces/CombatTornadoMovePiece.cs:21-24`
- 내용: 생성자 인자 target의 Settings가 null이면 InvalidOperationException을 던진다(44). 같은 생성자의 다른 검사(unit·worldUp·landing)는 ArgumentException을 쓰고, 형제 조각 CombatTornadoMovePiece는 같은 null 설정에 ArgumentNullException을 쓴다.
- 고칠 방향(검토자 제안): ArgumentException("토네이도 연출 설정이 없습니다.", nameof(target))로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S22

### C6-09 [낮음] CombatAttackMotion 생성자가 target·up 오류에도 nameof(duration)으로 던짐

- 위치: `V/Presentation/Attack/CombatAttackMotion.cs:19-31`
- 내용: target이 유한하지 않거나 up이 0일 때도 ArgumentOutOfRangeException(nameof(duration))을 던져 잘못된 인자를 알린다.
- 고칠 방향(검토자 제안): target·up·duration을 따로 검사하고 각자의 nameof를 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C6-10 [낮음] 이동 레인 집합·레인의 예외 규약·들여쓰기 어긋남

- 위치: `V/Presentation/Movement/CombatUnitMotionSet.cs:151-165`, `V/Presentation/Movement/CombatUnitMotionSet.cs:174-180`, `V/Presentation/Movement/CombatUnitMotionSet.cs:214-224`
- 내용: ① CombatUnitMotionSet.DeferFacingRestore는 null 인자에 ArgumentNullException 대신 InvalidOperationException을 던진다. ② BeginPull(집합·레인)은 movement·target을 검사하지 않아 null이면 NRE가 나고, 집합 쪽 문구에 [CombatUnitMotionSet] 접두어가 없다. ③ `/// <summary>` 두 줄이 0열에서 시작하고(CombatUnitMotionSet.cs:160, CombatUnitMotionPlayback.cs:213) Lane() 생성 인자 줄바꿈이 흐트러져 있다.
- 고칠 방향(검토자 제안): null 인자는 ArgumentNullException으로, BeginPull 인자를 검사하고 들여쓰기를 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S20

### C6-11 [낮음] 필드 부품(FieldCellFill·FieldArrowhead·FieldGridDecal)의 예외·IDisposable 관례가 다름

- 위치: `V/Display/Field/FieldCellFill.cs:46`, `V/Display/Field/FieldCellFill.cs:70-75`, `V/Display/Field/FieldCellFill.cs:97-98`
- 내용: FieldCellFill은 빈 이름에도 ArgumentNullException을 던진다(FieldArrowhead는 ArgumentException). FieldCellFill.Build는 meshes.Register의 null을 검사하지 않는데 FieldArrowhead는 ?? throw로 막아, null이면 나중에 Update가 '구성하기 전'이라는 엉뚱한 오류를 낸다. FieldGridDecal은 Dispose를 갖고도 IDisposable을 구현하지 않는다.
- 고칠 방향(검토자 제안): 인자·상태 예외를 통일하고 Register 결과를 검사하며 FieldGridDecal에 IDisposable을 구현한다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C6-12 [낮음] FieldOutlineRenderer만 config 재질 누락에 InvalidOperationException을 던짐

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:107-110`, `V/Display/Field/FieldMovementPathLine.cs:48-50`
- 내용: 형제 부품(FieldMovementPathLine·FieldTrajectoryLine·FieldKnockbackMarkers)은 config 인자의 재질이 빠지면 ArgumentException(nameof(config))을 던지는데 FieldOutlineRenderer는 격자 데칼 재질 누락을 InvalidOperationException으로 던진다.
- 고칠 방향(검토자 제안): ArgumentException(…, nameof(config))로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C6-13 [낮음] CombatBodyTextPool·Railway 경로에서 인자 문제에 InvalidOperationException이 섞임

- 위치: `V/Presentation/Text/CombatBodyTextPool.cs:40-45`, `V/Presentation/Text/CombatBodyTextPool.cs:71-78`, `V/Presentation/Railway/RailwayTravelPath.cs:23-24`
- 내용: CombatBodyTextPool은 같은 prefab null 인자를 Rent는 InvalidOperationException, Prewarm은 ArgumentNullException으로 막는다. RailwayTravelPath·RailwayContactSchedule은 인자 목록이 비었을 때 ArgumentException이 아니라 InvalidOperationException을 던진다. 호출 실수와 상태 문제를 예외 종류로 나눌 수 없다.
- 고칠 방향(검토자 제안): 인자 null은 ArgumentNullException, 빈 목록은 ArgumentException으로 맞추고 설정 누락만 InvalidOperationException으로 남긴다.
- 검증: 묶음 낮음; 찾은 검토자 L08

### C6-14 [낮음] 연출 값 생성자(CombatBodyTextData·CombatCameraSettingsData)의 예외 종류·인자 이름 어긋남

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatBodyTextInput.cs:70-71`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatBodyTextInput.cs:81-82`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatCameraSettingsData.cs:30-31`
- 내용: CombatBodyTextData는 정의되지 않은 열거 값에 인자 이름 없는 ArgumentException을 던지고(ArgumentOutOfRangeException+인자 이름이 맞음), 유지·페이드 검사는 fade가 틀려도 nameof(hold)로 던진다. CombatCameraSettingsData도 인자 이름 없는 ArgumentException을 던진다.
- 고칠 방향(검토자 제안): 열거 값마다 ArgumentOutOfRangeException(nameof(...))로 던지고 hold·fade를 나눠 검사한다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C6-15 [낮음] CombatSceneDisplay 생성자 인자 검증이 절반만 됨

- 위치: `V/Display/CombatSceneDisplay.cs:167-210`
- 내용: inspection·selection·pack·audio·thorns 등은 ArgumentNullException으로 거르지만 field·movementPlanning·placement·attack·units·hud·ghost·actions·pointer·events·camera·eventPlayback 등 19개는 검사 없이 보관한다. eventPlayback·movementPlanning·events·pointer·railway는 같은 생성자 안 _fieldBound 배열을 만들 때 바로 역참조돼 ArgumentNullException 대신 NRE가 나고, camera는 Release에서야 NRE가 난다.
- 고칠 방향(검토자 제안): 모든 필수 인자를 같은 규칙으로 검사하거나 검증을 Factory 한 곳에 모아 생성자 검사를 통일한다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C6-16 [낮음] OrderedCombatPresentation.Play가 result·onFinished를 검사하지 않아 늦게 NRE로 드러남

- 위치: `V/Playback/Ordering/OrderedCombatPresentation.cs:93-115`, `V/Playback/Ordering/OrderedCombatPresentation.cs:166-169`
- 내용: Attach는 ArgumentNullException을 던지지만 Play는 result가 null이면 foreach에서 NRE, onFinished가 null이면 모든 사건이 끝난 뒤 Tick의 finished(...)에서야 NRE가 난다. 그때는 _result를 비운 뒤라 호출 지점을 알기 어렵다.
- 고칠 방향(검토자 제안): Play 첫머리에서 result와 onFinished의 null을 확인해 ArgumentNullException을 던진다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C6-17 [낮음] CombatUnitPreparationLease 생성자는 인자를 검사하지 않으면서 Dispose는 null을 방어함

- 위치: `V/Display/Unit/CombatUnitPreparationLease.cs:25-33`, `V/Display/Unit/CombatUnitPreparationLease.cs:72-73`, `V/Display/Unit/CombatUnitPreparationLease.cs:85-101`
- 내용: 생성자는 view·layer·returnView를 검사 없이 받고 Activate는 _layer·_view.Hud를 바로 쓰지만, Dispose는 `_layer != null && _view != null`로 방어한다. 인자 ArgumentNullException 규약과 맞지 않고 어느 쪽이 계약인지 불분명하다.
- 고칠 방향(검토자 제안): 생성자에서 인자를 검사하고, Dispose의 null 방어는 파괴된 Unity 객체 판정 용도로만 남긴다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C6-18 [낮음] 연출 입력 열거·형식 여럿에 한국어 XML doc이 없음

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatBodyTextInput.cs:6-29`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatArcRotationMode.cs:4-8`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs:7-13`
- 내용: CombatActivationCueTiming·CombatBodyTextSource·CombatBodyTextTarget·CombatBodyTextCount와 값, CombatArcRotationMode 값, CombatTraitStateEvidenceKind, CombatAttackPresentationKind 값에 XML doc이 없다. 같은 폴더의 CombatCueAnchor·CombatEffectEnd·CombatUnitActionPlace는 값마다 설명이 있어 관례가 어긋난다.
- 고칠 방향(검토자 제안): 열거와 값마다 한국어 summary를 단다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C6-19 [낮음] Playback/Support 파일의 XML doc 누락·필드 배치 혼재·전체 이름 표기

- 위치: `V/Playback/Support/CombatTraitStateEvidence.cs:12-87`, `V/Playback/Support/CombatShownFacts.cs:23-77`, `V/Playback/Support/CombatMomentCuePlayback.cs:34-54`
- 내용: CombatTraitStateEvidence.cs의 인터페이스·근거 클래스·CombatTraitEffectSource에 문서가 없다. CombatShownFacts의 public TryGet/TryPick에도 문서가 없고 필드가 66-77·138-139에 흩어져 있다. CombatMomentCuePlayback은 PlayStructureHit 메서드가 필드 선언보다 앞에 있다. CombatCueSources는 using을 두고도 System.Collections.Generic·UnityEngine·EntityKey를 전체 이름으로 적는다.
- 고칠 방향(검토자 제안): 문서를 보충하고 필드를 클래스 앞쪽에 모으며 전체 이름 대신 using을 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C6-20 [낮음] CombatSceneRoot·CombatDisplayTables의 Allman·공백 규칙 위반

- 위치: `V/CombatSceneRoot.cs:20-31`, `V/CombatDisplayTables.cs:48`, `V/CombatDisplayTables.cs:86-87`
- 내용: CombatSceneRoot의 한 줄 메서드 본문 `{ if (!focused) FocusLost?.Invoke(); }`(22)와 빈 줄 없이 이벤트·필드·속성이 섞인 묶음(20-31)이 규칙과 다르다. CombatDisplayTables의 'Units =units'(48)와 빈 줄 없이 붙은 속성(86-87)도 같다.
- 고칠 방향(검토자 제안): Allman 본문과 빈 줄 규칙으로 정리한다.
- 검증: 묶음 낮음; 찾은 검토자 S01

## C7 불필요한 매 프레임 반복 — 중간 1 · 낮음 12

### C7-02 [중간] 같은 프레임·같은 포인터 좌표의 UI Raycast·칸 집기를 소비자마다 따로 반복

- 위치: `V/Input/CombatInputRouter.cs:179-186`, `V/Input/CombatInputRouter.cs:224-256`, `V/Input/Interaction/CombatAttackDragGesture.cs:56-64`
- 내용: 포인터가 움직인 프레임에 같은 좌표로 EventSystem.RaycastAll이 여러 번 돈다. 부르는 곳은 ① Router.RoutePointer의 blockedByUi(PointerMoved에서 이 값을 쓰는 곳은 공격 Drag 후보 승격뿐), ② 유닛 호버 Pick(같은 칸 안에서도 이동마다 다시 함), ③ RouteInput 뒤 환경 호버, ④ Tick 끝 환경 호버, ⑤ 경로·배치 Drag와 공격 모드의 커서 스냅·경로 Map·Drag 후보 승격(TryMapPointer → HitsUi)이다. 카메라 Ray·평면 교차 칸 집기도 3~4번 한다. 소비자마다 마지막 포인터를 따로 기억하고 결과도 따로 다시 구한다. 한 프레임·한 좌표의 판정을 함께 쓰는 곳이 없다.
- 실패·변경 시나리오: 플레이어 차례에 필드 위로 마우스를 움직이면 한 프레임에 같은 좌표로 RaycastAll이 4번, 이동 경로 Drag 중이면 5번 돈다. RaycastAll은 모든 GraphicRaycaster의 raycastTarget Graphic(유닛 HUD·덱·가방·정보창)을 훑으므로, UI가 많을수록 마우스를 움직이는 프레임마다 비용이 그 배수로 커진다.
- 고칠 방향(검토자 제안): CombatPointerMapper 같은 한 곳이 프레임(또는 좌표)마다 포인터 표본(UI 가림·칸)을 한 번만 만들고, Router·유닛 호버·환경 호버·커서 스냅·경로는 그 표본을 읽는다. Router는 PointerMoved에서 공격 Drag 후보가 있을 때만 UI 가림을 구한다.
- 검증 근거(증거 검증자): 코드를 열어 보니 주장대로다. PointerMoved 한 번에 Router.RoutePointer가 IsPointerOverUi로 RaycastAll을 하고(CombatInputRouter.cs:231), 소유자가 한가하면 CombatUnitRangeHover.Pick이 같은 좌표로 IsPointerOverUi와 TryPickCell을 다시 한다(CombatUnitRangeHover.cs:179-183). 이어서 RouteInput 뒤 SyncEnvironmentHover(CombatViewSystem.cs:1131)와 Tick 끝 SyncEnvironmentHover(CombatViewSystem.cs:311)가 모두 CombatEnvironmentHover.Refresh에서 IsPointerOverUi와…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L10, S11, S12

### C5-22 [낮음] 곡사 무기 자세 진행 값을 쓸 때마다 Validate(IsChildOf)를 다시 함

- 위치: `V/Display/Unit/CatusCannonWeaponView.cs:32-74`, `V/Display/Unit/CombatUnitPresentationWindow.cs:287-297`
- 내용: SetPrepareProgress·SetReturnProgress가 매 프레임 진행 값을 쓸 때마다 bucket·muzzle의 IsChildOf를 다시 본다. 같은 검사를 BeginPose와 CombatUnitPresentationWindow.TryGetMortarWeapon이 이미 했고, 자세 번호가 같은 대여임을 보장한다.
- 고칠 방향(검토자 제안): 검사를 BeginPose 한 번으로 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C7-01 [낮음] 환경 호버 Refresh가 캐시 비교 전에 UI Raycast·칸 집기를 해서 커서가 멈춰도 매 프레임 돈다

- 위치: `V/Input/Assist/CombatEnvironmentHover.cs:104-127`, `V/CombatViewSystem.cs:301-315`, `V/CombatViewSystem.cs:1131-1156`
- 내용: CombatViewSystem.Tick 끝(311)이 전투 중 매 프레임 SyncEnvironmentHover를 부르고, RouteInput 뒤(1131)에도 한 번 더 부른다. CombatEnvironmentHover.Refresh는 '같은 상태·칸·결과 번호면 그대로' 캐시 비교(122)보다 앞에서 IsPointerOverUi(CombatPointerMapper.HitsUi → EventSystem.RaycastAll)와 TryPickCell(카메라 Ray + 평면 교차)을 먼저 한다(116-117). 그래서 포인터를 한 번이라도 움직인 뒤에는 재생이 아닌 모든 프레임에 결과가 같아도 RaycastAll과 칸 집기가 돈다. 대조로 카메라 입력과 유닛 호버는 필요할 때만 Raycast한다.
- 실패·변경 시나리오: 플레이어 차례에 커서를 필드 위에 두고 10초(60fps) 가만히 있으면 RaycastAll과 카메라 Ray 집기가 약 600번 돈다. 캐시가 같아 화면은 그대로다. 비용은 raycastTarget Graphic 수(전투 캔버스·유닛 수만큼 늘어나는 UnitHUD)에 비례한다.
- 고칠 방향(검토자 제안): Refresh 앞에 문지기를 둔다. 포인터 좌표·상태 참조/결과 번호·숨김 번호·UI 열림 가운데 하나가 바뀐 때만 다시 판정하고, 환경 호버는 Tick 한 곳에서만 맞춘다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L09

### C7-04 [낮음] 경로 입력이 매 Tick 같은 마지막 응답을 다시 받아 초안·고스트를 다시 그리거나 빈 정리를 반복

- 위치: `V/CombatViewSystem.cs:404-407`, `V/Input/Interaction/CombatInteraction.cs:357-399`, `V/Input/MovePath/CombatPathInput.cs:195-222`
- 내용: CombatInteraction.Poll은 번호와 상관없이 마지막 응답을 매 Tick 경로 어댑터에 넘긴다. 어댑터 Poll은 그 응답으로 OnPreview를 부르고 이어 Refresh를 부른다. (1) 경로 입력이 허용되지 않는 동안(적 턴·재생·공격 모드·배치 Layer)에는 OnPreview가 매 프레임 CancelInputs를 부른다. CancelInputs는 _planningVersion을 올리고 정리 다섯 단계를 차례로 부른다(502-503 주석이 매 프레임 불린다고 인정하고 할당만 없앴다). (2) 경로 지정 중(멈춘 Drag, 놓은 뒤 승인 대기)에는 같은 결과를 다시 적용한다. ShowDraft도 매 프레임 UpdateMovementPointer(화면 Ray 투사, 고스트 색·위치·활성 쓰기)와 초안 층 칸 비교(FieldHighlightState.SetLayerCells가 판 전체를 훑음)를 다시 보낸다.
- 실패·변경 시나리오: 배치 국면이나 재생 중에 1분(60fps)이 지나면 빈 CancelInputs가 약 3,600번 돌고, 판 번호와 초안 Revision도 그만큼 오른다. 이동 Drag 중 포인터를 멈추거나 놓은 뒤 승인을 기다리는 동안에는 입력이 없어도 프레임마다 Ray 투사 1회, 고스트 머티리얼·Transform 쓰기, 판 전체 칸 비교가 반복된다.
- 고칠 방향(검토자 제안): OnPreview에 '이미 처리한 응답 번호'를 두어 새 응답이 왔거나 자기 요청이 기다리는 중일 때만 적용·표시한다. 허용에서 불허로 바뀐 순간에만 한 번 정리하고, 지정·초안·대기가 비어 있으면 CancelInputs를 건너뛴다. ShowDraft는 Draft.Revision·응답·포인터가 바뀐 때만 보낸다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L10, S13

### C7-05 [낮음] 한 Tick 안에서 입력 사실(CombatInputFacts)을 11~12번 다시 조립

- 위치: `V/CombatViewSystem.cs:301-315`, `V/CombatViewSystem.cs:400-412`, `V/CombatViewSystem.cs:1019`
- 내용: 조정자 Tick은 ReadFacts를 7번 부른다. 가용성 Sync 두 번(401·410), Interaction.Tick(405), SyncActionButtons(1173과 MovementUndo.CanRequest), SyncEnvironmentHover(1152와 CanReceiveDeckInput 1019), TickCamera(313)다. 경로 입력도 OnPreview·Refresh·RefreshMoveModeRange에서 AllowsInput·ActiveActor·PlanningActor를 거치며 4~6번 더 읽고, 유닛 호버 IsMoveModeStale이 2번을 보탠다. 한 번 읽을 때마다 대리자 몇 개 호출, 미처리 명령 목록 훑기, 튜토리얼 세션·캐릭터 사전 조회를 거쳐 27~30칸짜리 값을 만든다. SyncActionButtons·SyncEnvironmentHover 안의 연달은 두 번처럼, 사이에 Buffer 쓰기나 외부 콜백이 없는 구간에서도 다시 읽는다. CombatActionPanel의 참조 검사도 매 프레임 돈다. 순번·QueryRevision·선택·Layer 같은 변경 표시는 이미 있지만, 이를 묶어 '바뀐 것 없음'을 보는 문지기가 없다. 비용은 프레임당 수십 µs 이하로 작다.
- 실패·변경 시나리오: 아무 입력 없는 대기 프레임에도 같은 사실 값이 프레임당 11~12번 다시 만들어진다. 1분(60fps)이면 약 4만 번이지만 화면은 그대로다.
- 고칠 방향(검토자 제안): Tick 앞에서 사실을 한 번 읽어 Tick 끝 파생 셋·경로 입력·호버에 넘기고, Buffer 쓰기나 외부 콜백 뒤에만 다시 읽는다.
- 검증: 묶음 낮음; 찾은 검토자 L07, L09, L10, S01, S13

### C7-06 [낮음] UnitHUDLayer가 카메라·유닛이 멈춰도 매 프레임 모든 HUD를 투영하고 선택 HUD를 선형 탐색

- 위치: `V/Display/Unit/UnitHUDLayer.cs:350-356`, `V/Display/Unit/UnitHUDLayer.cs:371-407`, `V/Display/Unit/UnitHUDLayer.cs:169-182`
- 내용: Canvas.preWillRenderCanvases에 묶인 RefreshPositions는 층이 열린 동안 매 프레임 돈다. HUD마다 기준점 위치, WorldToScreenPoint 두 번, ScreenPointToLocalPointInRectangle를 계산하고 카메라 유효성을 다시 본다(바깥 canProject로 이미 봤다). 이어서 RefreshSelectedInfo로 선택 HUD를 선형 탐색한다. 위치·배율 쓰기는 바뀔 때만 하지만(177-180), 위젯 alpha는 조건 없이 매 프레임 쓴다. 카메라 자세·화면 크기·기준점이 그대로인 평소 프레임에는 'HUD 수 × 투영 계산'이 모두 헛일이다.
- 고칠 방향(검토자 제안): 출력 카메라 위치·회전·orthographicSize·화면 크기와 기준점 이동 버전이 같으면 투영을 건너뛴다. 선택 HUD 찾기는 등록·선택·보임이 바뀐 때만 하고, 위젯 alpha도 바뀐 때만 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 L09, S09

### C7-07 [낮음] 따라가는 이펙트·상태 설명창·몸 위 글자가 위치가 그대로여도 매 프레임 Transform을 씀

- 위치: `V/Presentation/Effects/CombatEffectFollower.cs:101-143`, `V/Display/Unit/FieldUnitPresenter.cs:196-209`, `V/Presentation/Effects/CombatPersistentEffects.cs:78`
- 내용: CombatEffectFollower.LateUpdate는 받침마다 매 프레임 몸 읽기 대리자(TrackBodyPresence: 표시 집합 조회·isActiveAndEnabled·TransformPoint)를 부른다. 그리고 비교 없이 transform.position을 쓰고 ShowRenderers를 부른다. 상태·특성 유지 이펙트는 전투 내내 살아 있어, 유닛이 서 있는 플레이어 차례에도 받침 수만큼 계속 쓴다. CombatUnitInfoView는 상태 아이콘 호버 동안 설명창 position을, CombatBodyTextWindow는 글자가 떠 있는 동안 anchoredPosition을 같은 방식으로 매 프레임 쓴다. 둘 다 Canvas 아래라 매 프레임 다시 묶인다. 같은 폴더의 UnitHUDLayer는 '바뀔 때만 쓴다'로 막아 두어 규칙이 한 곳에만 적용됐다.
- 고칠 방향(검토자 제안): 마지막으로 쓴 값과 비교하고 쓰는 작은 도우미 하나를 셋이 함께 쓴다. 따라가는 받침은 숨김 상태가 바뀐 때만 Show/HideRenderers를 부르고, 설명창은 슬라이드 중이거나 호버 칸이 바뀐 때만 위치를 다시 잡는다.
- 검증: 묶음 낮음; 찾은 검토자 L09, L10, S18

### C7-08 [낮음] 행동 표면 맞춤이 매 Tick 같은 값 비교 전에 참조 11개를 검사하고, 정보창 가용성 색을 비교 없이 다시 씀

- 위치: `V/CombatViewSystem.cs:1163-1177`, `V/Input/ActionButtons/CombatInputSurfaces.cs:57-67`, `V/Display/Hud/CombatActionPanel.cs:123-129`
- 내용: SyncActionButtons → CombatInputSurfaces.Sync가 매 Tick 돈다. CombatActionPanel.SetAvailable은 '직전 값과 같으면 쓰지 않음' 비교(129)보다 앞에서 FindMissingReference(UnityEngine.Object null 비교 11번)를 매번 한다(125-127). 같은 검사는 연결 때 CombatSceneRoot.ValidateReferences가 이미 했다. CombatUnitInfo.SetActionAvailability는 값 비교 없이 Image.color 둘을 매 Tick 쓴다. 바로 옆 ActionPanel이 직전 값 비교를 하는 것과 어긋난다. Graphic.color setter가 같은 값을 걸러 화면 비용은 작다.
- 고칠 방향(검토자 제안): 같은 값 비교를 먼저 하고 참조 검사는 연결 때 한 번만 한다. 정보창 가용성은 직전 (canMove, canAttack)을 기억해 바뀐 때만 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 L10, S05

### C7-09 [낮음] 토네이도 화살촉 점등 Tick이 할 일이 없어도 매 프레임 돌고, 표 조회와 꺼진 인스턴스 순회를 반복

- 위치: `V/Display/Map/FieldTornadoArrowPulse.cs:22-25`, `V/Display/Map/FieldTornadoPresenter.cs:78-80`, `V/Display/Map/FieldTornadoPresenter.cs:113-125`
- 내용: FieldTornadoArrowPulse.Update는 루트가 생긴 뒤 전투 내내 매 프레임 TickPulse 대리자를 부른다. TickPulse는 토네이도가 하나라도 있으면 매 프레임 Settings 속성으로 GetTerrainVisual(Tornado) 사전 조회와 null 검사를 다시 한다. 보이는 화살촉이 없어도 모든 인스턴스를 돌며 IsVisible을 확인한다. 설정은 전투 중 바뀌지 않는다.
- 고칠 방향(검토자 제안): 설정은 처음 만들 때 캐시한다. 켜진 화살촉 수를 세어 두고, 그 수가 0이거나 IsStill이면 컴포넌트를 끈다(enabled=false).
- 검증: 묶음 낮음; 찾은 검토자 S06, S07

### C7-10 [낮음] 걸음 레인이 전투 내내 쌓이고 재생이 없어도 매 프레임·매 사건 전부 훑음

- 위치: `V/Presentation/Movement/CombatUnitMotionSet.cs:69-104`, `V/Presentation/Movement/CombatUnitMotionSet.cs:116-133`, `V/Presentation/Movement/CombatUnitMotionSet.cs:214-224`
- 내용: CombatUnitMotionSet은 한 번이라도 움직인(밀린) 유닛마다 레인을 Dictionary에 넣고 지우지 않는다(죽은·소환 유닛 포함). CombatStepPlayer.Tick은 재생이 없는 프레임에도 _motion.IsActive로 모든 레인을 훑는다. 사건마다 BeforeEvent·ResumeLeaps·시작 질의(IsActive·AllowsOverlap·CanBegin)도 모든 레인을 돈다. 레인 수만큼 커지는 상시 순회지만 비용은 작다.
- 고칠 방향(검토자 제안): 활성 레인 목록을 따로 두어(걸음 시작 때 넣고 끝날 때 뺌) Tick·질의가 그것만 보게 하거나, 쉬는 레인을 결과 끝에 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L10

### C7-11 [낮음] 정착 대기 IsSettled가 매 프레임 남은 사건 전체를 O(n²)로 다시 훑음

- 위치: `V/Playback/Ordering/OrderedCombatPresentation.cs:78-91`, `V/Playback/Ordering/OrderedCombatPresentation.cs:218-231`, `V/Presentation/Pieces/CombatUnitSettleWaitPiece.cs:48-52`
- 내용: CombatUnitSettleWaitPiece.Tick은 끝날 때까지 매 프레임 IsSettled를 부른다. IsSettled는 _first부터 결과 끝까지(장벽에서 멈추지 않음) 사건마다 IsHitFollowUpOf 사전 조회를 한다. 아직 시작하지 않은 후속마다 CanStart로 _first..i를 다시 훑는다. 상태가 바뀌지 않은 프레임에도 같은 계산을 한다. 사건 수가 보통 적어 비용은 작다.
- 고칠 방향(검토자 제안): 훑는 범위를 출처의 후속 색인 목록(결과 시작 때 만든 CombatHitFollowUps)으로 줄이거나, 사건 시작·완료가 있던 프레임에만 다시 계산한다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C7-12 [낮음] 보드·선로 등장 Tick이 이미 끝난 몸에도 매 프레임 위치를 다시 씀

- 위치: `V/Presentation/Entry/CombatBoardReveal.cs:66-99`, `V/Presentation/Entry/CombatEntryRailwayAppearance.cs:87-108`
- 내용: 모든 몸이 끝날 때까지는 이미 t ≥ 1인 몸에도 매 프레임 localPosition 쓰기와 Visibility.Restore()를 한다. 16×16 보드면 타일 256개와 구역들에 약 84프레임 동안 같은 값을 계속 쓴다. 전투 진입 때 한 번뿐이라 비용은 작다.
- 고칠 방향(검토자 제안): 몸마다 끝남 표시를 두어, 끝난 몸은 최종 위치를 한 번 쓴 뒤 건너뛴다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C7-13 [낮음] 사망 머묾 구간에 같은 최종 자세를 매 프레임 다시 씀

- 위치: `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:62`, `V/Presentation/Reaction/CombatUnitReactionPresenter.cs:107-113`
- 내용: 사망 반응의 전체 시간은 DeathDuration+DeathRestDuration이다(62). _elapsed가 DeathDuration을 넘은 머묾 구간에서는 amount가 1로 고정되는데도 매 프레임 ApplyPose(Transform 쓰기)를 부른다(107-113). 죽는 유닛마다 바뀌지 않는 쓰기가 생긴다.
- 고칠 방향(검토자 제안): 머묾 구간에 처음 들어갈 때 최종 자세를 한 번만 쓰고, 그 뒤로는 시간만 센다.
- 검증: 묶음 낮음; 찾은 검토자 S23

## C8 캡슐화·정보 은닉 (추천) — 중간 1 · 낮음 12

### C8-01 [중간] CombatSceneInput이 부품을 구체 타입으로 내놓아 조정자가 Interaction 내부 단계·모드 깃발을 직접 조합

- 위치: `V/Input/CombatSceneInput.cs:63-112`, `V/CombatViewSystem.cs:1103-1109`, `V/CombatViewSystem.cs:405-407`
- 내용: CombatSceneInput은 Gate·Interaction·Deployment·EndTurn·ActionAvailability·MovementUndo를 구체 타입 internal 속성으로 낸다. 감싼 입구(OnResult·Route·CloseConfirmations·ClearActionAvailability·IsConfirmationOpen)와 부품 직접 호출(Interaction.Tick·BeforeResult·AfterResult·AfterAcknowledged, Gate.Resolve, ActionAvailability.Sync)이 섞여 결과 후처리 순서가 묶음과 CombatViewSystem 두 곳에 흩어진다. SelectPressed 정보창 갈래는 `Interaction.Drag == None && !IsAttackActive && !IsPathPlanning`을 손으로 조합하는데, 거의 같은 판단을 CombatInteraction.IsInputOwnerActive가 전경 소유자 목록으로 이미 한다(넘겨받기 소유자 포함 여부만 다름). CombatInputFactsReader도 connection.Input.Interaction.*·connection.Selection을 직접 따라간다. 덧붙여 확인창의 HasUnplacedUnits·CountIdleUnits는 자기 클래스 안에서만 쓰이는데 internal이다.
- 실패·변경 시나리오: 새 전경 입력 소유자(새 Drag 종류·기믹 조준)를 OwnerAdapter로 목록에 추가하면 IsInputOwnerActive는 저절로 반영하지만 조정자의 세 깃발 조합은 그대로라, 새 모드가 켜진 동안 SelectPressed가 InspectUnit을 불러 정보창 선택이 끼어든다. Interaction의 결과 수명 단계를 바꾸려면 CombatViewSystem의 FinalizeResult 순서도 함께 고쳐야 한다.
- 고칠 방향(검토자 제안): CombatSceneInput에 BeforeResult/AfterResult/AfterAcknowledged/Tick/CanInspectOnPress(=!IsInputOwnerActive)·가용성 맞춤 같은 묶음 수준 입구를 두고 부품 속성은 private으로 숨긴다.
- 검증 근거(증거 검증자): CombatSceneInput.cs:63-112는 Gate·Interaction·Deployment·EndTurn·ActionAvailability·MovementUndo를 구체 타입 internal 속성으로 그대로 내놓는다. 그래서 CombatViewSystem.cs:405-407·836-842·883-909·1139-1145·1163-1177이 부품의 수명 단계(Tick·BeforeResult·Gate.Resolve·AfterResult·AfterAcknowledged·Sync·CanEnterAttack·MovementUndo.CanRequest)를 직접 부른다. 특히 CombatViewSystem.cs:1104-1109는 `Drag == None && !IsAttackActive && !IsPathPl…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 L06, L11, S10, S12

### C8-02 [낮음] 조정자·유닛 표시의 호출자 없는 시험 입구가 운영 null 갈래와 클립 Graph 우회 통로를 만든다

- 위치: `V/CombatViewSystem.cs:1055-1065`, `V/CombatViewSystem.cs:587-598`, `V/CombatViewSystem.cs:209-241`
- 내용: CombatViewSystem의 `Input`·`RequestReturn()`·`AttachDisplay()`와 BindServices 선택 인자 `loading = null, runDeck = null`('시험 조립에서만 생략'), FieldUnitView의 `CurrentPresentationPose`·`ClipChannel`, UnitClipChannel의 `HasBase`·`HasOverlay`·`Graph`, CombatPhaseTransitionSettings.From의 `findLayer`는 저장소(Assets/Tests 포함)에 호출자가 없다. 선택 인자 때문에 `_unitPreparationReader`가 null일 수 있고(241) Attach에서 늦게 예외를 던지며(499) `_loading` null을 검사한다(348, 617). `ClipChannel`·`Graph`는 BeginClip 대여를 거치지 않고 PlayableGraph를 어셈블리 전체에 내준다.
- 실패·변경 시나리오: 운영 조립(PostBindService)을 고치다 runDeck·loading 인자를 빠뜨려도 컴파일·BindServices가 통과하고, 전투 씬 연결 때 '확정 덱의 준비 대상 읽기가 필요합니다' 예외로 처음 드러난다(F1 정지). 같은 어셈블리 코드가 FieldUnitView.ClipChannel.Graph를 Evaluate·Destroy하면 클립 대여의 버전 보호를 우회해 위층 클립 상태가 틀어진다.
- 고칠 방향(검토자 제안): 호출자 없는 시험 입구를 지우거나 private으로 내리고, BindServices의 loading/runDeck은 필수 인자로 바꿔 null 갈래를 없앤다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L11, S01

### C8-03 [낮음] 재생 지원 부품이 운영에서 늘 넘기는 의존을 시험 편의로 null 허용하고 첫 발동 때 예외

- 위치: `V/Playback/Support/CombatTraitFeedbackPlayback.cs:33-43`, `V/Playback/Support/CombatTraitFeedbackPlayback.cs:131-134`, `V/Playback/Support/CombatGuardLead.cs:51-53`
- 내용: CombatTraitFeedbackPlayback의 text·evidence, CombatGuardLead의 feedback, CombatSceneEventPresenter의 bodyText·feedbackEvidence가 기본값 null을 허용한다. 운영 Factory는 늘 넘긴다. null이면 생성 때가 아니라 전투 중 첫 해당 발동에서 InvalidOperationException이 난다.
- 고칠 방향(검토자 제안): 생성자에서 필수로 검사하고 시험은 가짜 창구·빈 근거 등록을 넘긴다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C8-04 [낮음] 시험 때문에 internal static으로 연 FieldCellFillStack.SetOpening

- 위치: `V/Display/Field/FieldCellFillStack.cs:707-721`, `Assets/Tests/Editor/CombatField/CellFillOpeningRenderTests.cs:44`
- 내용: SetOpening은 운영에서는 같은 클래스 안(703, 784)에서만 쓰인다. Editor 시험 CellFillOpeningRenderTests가 직접 부르려고 internal static으로 열려 있어 셰이더 값 이름 규약이 바깥에 드러난다.
- 고칠 방향(검토자 제안): private으로 바꾸고 시험은 Apply 결과로 판정한다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C8-05 [낮음] Buffer 쓰기 계약에 번호 없는 ClearPreview()가 호출자 없이 열려 소유자 번호 보호를 우회 가능

- 위치: `S/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs:41-51`, `S/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs:122-135`, `S/RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs:158-162`
- 내용: ICombatViewBufferWriteSink는 남의 요청을 지우지 않게 번호 비교 ClearPreview(int)를 두면서, '전투 경계 정리·시험용'이라 적힌 번호 없는 ClearPreview()도 같은 계약에 공개한다. 전투 경계 정리는 Clear()가 내부에서 처리하고, 저장소에 이 계약 오버로드의 외부 호출자가 없다. 아무 입력 소유자나 다른 소유자의 미리보기를 지울 통로가 남는다.
- 고칠 방향(검토자 제안): 계약에서 ClearPreview()를 빼고 구현 클래스의 private 도우미로 둔다.
- 검증: 묶음 낮음; 찾은 검토자 L11, S23

### C8-06 [낮음] FieldUnitView가 '유일한 입구' 창구 밖에서 대여 시작 메서드를 internal로 열어 둠

- 위치: `V/Display/Unit/FieldUnitView.cs:262-282`, `V/Display/Unit/FieldUnitView.cs:360-374`, `V/Display/Unit/FieldUnitView.cs:465-488`
- 내용: CombatUnitPresentationWindow는 재생 쪽의 유일한 입구로 걸음 배치와 자세 겹침을 막는다(RequireNoPlacement). 그런데 FieldUnitView는 BeginAttackPose·BeginReactionPose·BeginPresentationScale·BeginHitFlash·BeginClip·SetMovementOpacity·ClearMovementOpacity를 internal로 연다. BeginAttackPose(Action)·BeginReactionPose는 FieldUnitView 밖 호출자가 없고, 나머지는 명시적 인터페이스 구현만 위임해 부른다(BeginStand는 private). 진입 연출은 구체 FieldUnitView 목록을 받으므로 창구 규칙을 지나지 않는 경로가 손에 닿는다.
- 고칠 방향(검토자 제안): 인터페이스 위임용 메서드를 private으로 내리고 호출자 없는 BeginAttackPose·BeginReactionPose를 지운다.
- 검증: 묶음 낮음; 찾은 검토자 L11

### C8-07 [낮음] FieldUnitView가 Hud·HealthLabel을 그대로 내줘 남이 표시 기억(_shownHealth·_hudFacts)을 우회해 씀

- 위치: `V/Display/Unit/FieldUnitView.cs:62`, `V/Display/Unit/FieldUnitView.cs:86`, `V/Display/Unit/FieldUnitView.cs:218-230`
- 내용: FieldUnitView는 체력 숫자(_hasShownHealth/_shownHealth)와 HUD 사실(_hudFacts)에 마지막으로 쓴 값을 기억해 바뀔 때만 쓴다. 그런데 UnitHUD와 TMP_Text를 internal 속성으로 내준다. CombatUnitPreparationLease는 HealthLabel 글자를 '0123456789/'로 직접 바꾸고, FieldUnitPresenter는 구조물 HUD에 SetStructure·SetStructureInfo를 직접 쓴다. 지금은 준비 대여가 풀 반환(ResetForPool의 _hasShownHealth=false)을 거쳐 맞을 뿐, 반환 경로가 바뀌면 기억과 실제 글자가 어긋난다.
- 고칠 방향(검토자 제안): Hud·HealthLabel 속성을 감추고 구조물 HUD 게시·글리프 준비를 FieldUnitView 메서드로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### C8-08 [낮음] 바깥 클래스 둘이 FieldUnitView 자식 Renderer·Collider를 각자 스냅숏 떠서 끄고 켬

- 위치: `V/Presentation/Entry/CombatEntryVisibility.cs:15-30`, `V/Presentation/Entry/CombatEntryEnemyAppearance.cs:40-53`, `V/Display/Unit/CombatUnitPreparationLease.cs:41-70`
- 내용: 진입 연출 CombatEntryVisibility는 view.transform 아래 Renderer.enabled를 따로 저장·복원하고, 준비 대여 CombatUnitPreparationLease는 자식 Renderer·Collider·Animator.fireEvents를 따로 저장·복원한다. 표시 상태 주인인 FieldUnitView는 이 숨김을 모르고, 숨김·복원 규칙이 바깥 두 곳에 따로 있다. (HealthLabel 직접 쓰기는 R0185 쪽 문제와 겹친다.)
- 고칠 방향(검토자 제안): FieldUnitView에 진입 숨김·준비 숨김 대여(또는 표시 억제 메서드 하나)를 두고 바깥은 그것만 부른다.
- 검증: 묶음 낮음; 찾은 검토자 L11

### C8-10 [낮음] 공격 전략 맥락이 구체 투사체 풀 전체(Tick·Clear·Release)를 내줌

- 위치: `V/Presentation/Attack/ICombatAttackStyleContext.cs:24-25`, `V/Presentation/Attack/CombatProjectileFlights.cs:13-16`, `V/Presentation/Attack/CombatProjectilePool.cs:133-134`
- 내용: ICombatAttackStyleContext.PoolOf는 구체 CombatProjectilePool을 돌려준다. 전략·동작이 쓰는 것은 FireLinear/FireArc/Recall뿐인데 Tick·Clear·Release·Prewarm까지 열려, 진행과 정리는 CombatProjectileFlights만 한다는 클래스 계약과 어긋나는 호출이 가능하다.
- 고칠 방향(검토자 제안): 발사·회수만 담은 좁은 인터페이스를 돌려준다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C8-11 [낮음] 반응 목록이 내부 Presenter 객체를 취소 손잡이로 밖에 내줌

- 위치: `V/Presentation/Reaction/CombatUnitReactionSet.cs:57-69`, `V/Presentation/Reaction/CombatUnitReactionSet.cs:137-142`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:156-169`
- 내용: CombatUnitReactionSet.BeginHit/BeginDeath는 CombatUnitReactionPresenter를 그대로 돌려준다. 사건 처리기는 Cancel(id, presenter)의 동일성 표지로만 쓰지만, 받은 쪽이 Tick·Cancel·Release를 직접 불러 Set의 _entries 정리를 거치지 않고 수명을 바꿀 수 있다.
- 고칠 방향(검토자 제안): 취소만 할 수 있는 작은 손잡이(또는 Entry 버전 값)를 돌려준다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C8-12 [낮음] 큐·부속 클립 목록이 원시 배열을 IReadOnlyList로 그대로 반환

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs:80-96`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs:77`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatClipData.cs:89-97`
- 내용: CombatEventCues.Copy·Read와 CombatClipData.Copy는 새 배열을 AsReadOnly 없이 반환해, 받는 쪽이 T[]로 캐스트하면 불변 값 객체 내부를 바꿀 수 있다. 같은 파일의 ReadSoundTimings(77)와 CombatClipTimings는 Array.AsReadOnly를 써 방식이 섞여 있다.
- 고칠 방향(검토자 제안): 반환 전에 Array.AsReadOnly로 감싸 한 방식으로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C8-13 [낮음] 정보창 내부에서만 쓰는 Show·ShowCombat·AnimateTransition이 internal

- 위치: `V/Display/Hud/CombatUnitInfoView.cs:180-181`, `V/Display/Hud/CombatUnitInfoView.cs:265-266`, `V/Display/Hud/CombatUnitInfo.cs:193-196`
- 내용: CombatUnitInfoView.Show(선택 인자 showCharacterDetails 포함)·ShowCombat은 ShowInspection 안에서만, CombatUnitInfo.AnimateTransition은 Update에서만 불린다. 정보창 일부만 그리는 잘못된 조합 호출 경로가 열려 있다.
- 고칠 방향(검토자 제안): 세 메서드를 private으로 줄인다.
- 검증: 묶음 낮음; 찾은 검토자 S05

### C8-14 [낮음] CombatSceneInputFactory 내부 조립 단계 셋이 internal

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:46`, `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:111`, `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:165`
- 내용: CreateEnvironmentHover·CreateSceneInteraction·CreateCursorSnap은 이 파일 안에서만 쓰이는데 internal이라 같은 어셈블리 누구나 입력 부품을 부분 조립할 수 있다.
- 고칠 방향(검토자 제안): private으로 줄인다.
- 검증: 묶음 낮음; 찾은 검토자 S24

## C9 수명·자원 관리 안전성 (추천) — 중간 0 · 낮음 14

### C10-16 [낮음] EntityViewSet 종류 교체 중 대여가 실패하면 null View 항목이 남고 대여 수가 두 번 줄어듦

- 위치: `V/Display/Unit/EntityViewSet.cs:54-71`, `V/Display/Unit/EntityViewSet.cs:134-139`, `V/Display/Unit/FieldUnitPresenter.cs:777-797`
- 내용: Acquire는 이전 View를 먼저 반환한다(대여 수 -1). 그 뒤 _rent가 던지면 항목이 View=null·이전 키로 남는다. End가 이 항목을 지우면서 _return(이전 키, null)을 다시 부르고, Return은 null인데도 _rentedCounts를 한 번 더 줄인다. 그사이 TryGet은 true와 null View를 돌려준다.
- 고칠 방향(검토자 제안): 새 View를 먼저 빌린 뒤 이전 View를 반환한다. Return은 view가 null이면 대여 수를 건드리지 않는다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C9-01 [낮음] Detach의 씬 루트 풀기(UnbindRoot)만 오류 수집 밖에 있어, 실패하면 남은 정리와 루트 구독 해제를 모두 건너뜀

- 위치: `V/CombatViewSystem.cs:736-781`, `V/CombatViewConnection.cs:118-134`, `V/Input/ActionButtons/CombatInputSurfaces.cs:42-51`
- 내용: Detach는 다른 단계를 모두 CleanupErrors.Try로 모으면서 756줄 `_connection?.UnbindRoot();`만 보호 없이 부른다. CombatViewConnection.UnbindRoot는 Surfaces.UnbindRoot(BattleChanger.UnbindInput → Pack.Refresh → UnitInfo.SetActionAvailability, 모두 GlobalSystem UI 호출, 개별 보호 없음)를 먼저 부르고 그 뒤에야 root.FocusLost/Disabled 구독을 푼다. 그래서 UI 호출 하나가 던지면 (1) 루트 이벤트 구독이 남고 (2) 진입 연출 취소·AbortPlayback·StopInput·입력 해제·`_connection = null`·Display.Release가 하나도 실행되지 않는다. '하나가 실패해도 나머지를 시도한다'는 Detach 문서와도 다르다.
- 실패·변경 시나리오: 가방이 보이는 중 UI 연결이 먼저 풀린 상태로 씬 이탈 → ReleaseScene → Detach. UnbindRoot 안의 Pack.Refresh가 'UISystem 연결이 필요합니다'를 던지면 AbortPlayback·StopInput·Display.Release를 건너뛰고 `_connection`이 남는다. 남은 Disabled 구독 때문에 씬이 내려갈 때 Detach가 다시 불려 같은 예외를 내고, 다음 Combat 씬의 Attach는 '이미 씬에 연결돼 있습니다'(ThrowIfDisplayAttached)로 거절된다.
- 고칠 방향(검토자 제안): Detach에서 UnbindRoot를 CleanupErrors.Try로 감싸고, CombatViewConnection.UnbindRoot는 이벤트 -= 를 try/finally로 반드시 실행하며 표면 세 호출도 각각 CleanupErrors로 시도한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L12, S10

### C9-02 [낮음] CellPrefab 모드 셀의 ColorHandle을 돌려주지 않아 전역 ColorSystem에 전투마다 쌓임

- 위치: `V/Display/Field/FieldCellHandle.cs:10-13`, `V/Display/Field/FieldCellHandle.cs:35-46`, `V/Display/Field/FieldCellPool.cs:41-62`
- 내용: FieldCellHandle은 Renderer마다 `colors.CreateColorHandle`로 손잡이를 받지만 'Scope 반환은 ColorSystem.Shutdown이 맡는다'며 Dispose하지 않는다. FieldCellPool·FieldCellColorRenderer.Release/Dispose도 ReturnAll만 하고 손잡이를 해제하는 길이 없다. ColorSystem은 앱 수명의 전역 시스템이라 colorHandles 목록과 renderers 집합에 손잡이·Renderer를 붙잡아 두고, ColorHandle.Dispose는 Renderer가 파괴됐으면 그냥 return하므로 씬이 내려간 뒤에는 Shutdown조차 거두지 못한다. 같은 범위의 FieldArrowhead·FieldTrajectoryLine·PlacementGhost 등은 손잡이를 직접 Dispose해 같은 자원을 두 정책으로 다룬다. 운영 표(StageFieldView_Default)는 GridOutline이라 설정에 따라서만 열리는 길이다.
- 실패·변경 시나리오: 표시 모드를 CellPrefab으로 두고 8x8 전투를 10번 들어갔다 나오면 매번 64칸 × Renderer 수만큼 ColorHandle이 ColorSystem.colorHandles에 남아 파괴된 Renderer 참조와 함께 앱이 끝날 때까지 늘어나고, Shutdown의 Dispose도 Renderer가 null이라 아무것도 거두지 못한다.
- 고칠 방향(검토자 제안): FieldCellHandle에 Dispose(손잡이 반환)를 두고 FieldCellPool에 최종 해제를 만들어 FieldCellColorRenderer.Dispose에서 Renderer가 살아 있을 때 부른다.
- 검증: 증거 낮음 · 반론 중간 · 판정 낮음; 찾은 검토자 L04, L12, S03

### C9-03 [낮음] 선로 발동마다 EffectScope를 새로 받아 이펙트 시스템에 쌓임

- 위치: `V/Display/Map/FieldRailwayPresenter.cs:199-209`, `V/Display/Map/FieldRailwayPresenter.cs:278-293`, `S/RuntimeSystem/GlobalSystem/RenderSystem/Core/EffectSystem/EffectSystem.cs:99-106`
- 내용: FieldRailwayPresenter.Begin은 발동할 때마다 `_effects.GetEffectHandle()`로 EffectScope를 새로 받는다. EffectSystem.CreateHandle은 받은 Scope를 모두 disposaHandles에 넣고 Shutdown 때만 비운다. CleanupPlayback은 StopAll만 부르고 참조를 버리므로 발동마다 빈 Scope가 하나씩 남는다. 같은 범위의 CombatEffectWindow는 바로 이 이유로 Scope를 한 번만 받고 앵커 Transform 재생 창구도 이미 있는데, 선로는 그 창구를 쓰지 않고 Scope 수명·반환 실패 처리(_hasUnreturnedEffects)를 따로 만들었다.
- 실패·변경 시나리오: 선로 기믹 스테이지에서 열차가 N번 발동하면(여러 턴·재시작·다음 전투) disposaHandles에 EffectScope가 N개 쌓여 Shutdown 전까지 풀리지 않는다. 또 재생 취소 때 CombatEffectWindow.StopAll은 선로 이펙트를 거두지 못해 정리 경로가 둘로 갈린다.
- 고칠 방향(검토자 제안): 선로 이펙트를 CombatEffectWindow.Play(앵커 = train.Movement)로 내거나, 최소한 Scope를 설치 수명에 한 번만 받아 다시 쓴다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 S06

### C9-04 [낮음] 무너짐 끝 콜백 하나가 실패하면 그 외형과 FinishAll의 나머지 외형이 풀로 돌아가지 않음

- 위치: `V/Presentation/Environment/CombatTileCollapseSet.cs:64-89`, `V/Display/Map/FieldTerrainPresenter.cs:291-302`
- 내용: Tick은 목록에서 지운 뒤 Finished()를 부르고, FinishAll은 목록을 비운 뒤 콜백(FieldTerrainPresenter.Return → SetActive·SetParent·pool.Return)을 차례로 부른다. 하나가 던지면 나머지 콜백은 불리지 않는다. 그 외형들은 이미 지형 표시 집합(TryReplace의 leave)과 무너짐 목록에서 모두 빠져, 아무도 추적하지 않는 활성 GameObject로 남는다. 다른 정리 경로는 CleanupErrors로 실패를 모으고 이어 가는데 여기만 벗어난다.
- 고칠 방향(검토자 제안): FinishAll(과 Tick)에서 각 Finished를 CleanupErrors.Try로 부르고 끝에 ThrowIfAny로 모아 던진다.
- 검증: 묶음 낮음; 찾은 검토자 L12, S19

### C9-05 [낮음] 표시 조립 실패 정리가 일부만 되돌려 가시밭 루트·선로·지형 등 이미 만든 소유 객체를 남김

- 위치: `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:53-57`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:130-149`, `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:187-224`
- 내용: CombatSceneDisplayFactory.Create는 try 앞에서 FieldThornPresenter(생성자에서 'FieldThorns_Inactive' GameObject를 CellsRoot 아래 생성)·선로·지형·흔적 등을 만든다. try 안에서 실패하면(예: units.Configure의 DB 검사 예외) catch는 events·cameraView·unitHudLayer만 정리하고 나머지는 씬 언로드까지 남는다. units.Configure가 끝난 뒤 실패하면 FieldUnitPresenter는 파괴된 HUD 층을 가리킨다. 이 실패 정리 목록은 CombatSceneDisplay.Release 목록과 따로 손으로 관리되는 사본이다. 55줄 remarks가 '부분 Configure 실패는 복구하지 않는다'고 밝히지만 소유 GameObject 생성까지 포함한다고 적지는 않았다.
- 고칠 방향(검토자 제안): 생성한 표시를 소유 목록에 쌓고 실패하면 역순으로 Release한다. 아니면 remarks에 씬 언로드까지 남는 객체를 명시한다.
- 검증: 묶음 낮음; 찾은 검토자 L12, S24

### C9-07 [낮음] ReleasePresentation이 한 칸 정리 중 예외가 나면 나머지 칸 정리를 건너뛴 채 풀로 돌아감

- 위치: `V/Display/Unit/FieldUnitView.cs:331-340`, `V/Display/Unit/FieldUnitView.cs:445-453`, `V/Display/Unit/UnitPresentationLeaseSlot.cs:53-69`
- 내용: UnitPresentationLeaseSlot.Reset은 finally에서 취소 콜백(밖의 재생 코드)을 부르므로 예외가 밖으로 나갈 수 있다. ReleasePresentation은 투명도·자세·서기·크기·플래시·클립·예상 체력을 보호 없이 차례로 부르므로, 자세 칸 콜백이 던지면 크기·플래시·클립 칸이 남은 채 반환된다. 같은 파일의 OnDestroy는 try/finally 사슬로 모든 정리를 보장한다. 콜백 예외는 F1이 먼저 멈출 가능성이 커 확신은 낮다.
- 고칠 방향(검토자 제안): ReleasePresentation의 각 단계를 CleanupErrors.Try로 모으고 끝에 ThrowIfAny한다.
- 검증: 묶음 낮음; 찾은 검토자 S09

### G1-01 [낮음] 취소 정리가 조각 실행(서는 자리)을 반응(자세)보다 먼저 되돌려, 서는 자리에서 맞던 수호자가 집 자리가 아니라 서는 자리에 남는다

- 위치: `V/Playback/Players/CombatPresentationPlayers.cs:20-32`, `V/Playback/Players/CombatPresentationPlayers.cs:85-90`, `V/Display/Unit/FieldUnitView.cs:325-339`
- 내용: 서는 자리 위에 자세가 쌓이는 순서는 허용된다. 수호자 서는 자리는 조각 실행이 Stand 채널로 빌리고, 그 자리에서 맞으면 피격 처리기가 BeginPose로 자세를 빌린다. 자세 대여는 빌린 순간의 위치(서는 자리)를 되돌릴 자리로 기억한다. FieldUnitView.ReleasePresentation은 이 때문에 '자세 → 서는 자리' 순서를 지킨다고 적어 두었다. 그런데 취소 사슬 CombatPresentationPlayers.CancelAll은 등록 순서(외형, 몸 글자, 공격, 조각, 걸음, 반응)대로 돈다. 그래서 조각 실행 Stop → Leases.Restore → RestoreStand가 먼저 집 자리를 쓰고, 그 뒤 반응 Cancel → ClosePose → RestorePose가 서는 자리를 다시 쓴다. 층 순서가 정리 목록의 재생기 등록 순서에 숨어 있고, 그 순서는 이 규칙과 반대다.
- 실패·변경 시나리오: 공격자가 유닛 행동을 하고, 수호자가 앞으로 나와 대신 맞는다. 피격 반응(자세)이 도는 0.x초 사이에 F1 오류 정지가 온다. 그러면 StopInput → AbortPlayback → CancelAll 순서로 정리된다. 먼저 조각이 수호자를 집 자리로 되돌리고, 이어서 반응이 수호자를 서는 자리로 다시 옮긴다. 정지 화면에서 수호자는 공격자 앞 칸(사실과 다른 자리)에 서 있다. 재생기 목록 순서를 바꾸거나 자세 채널을 조각으로 옮기면 결과가 또 달라진다.
- 고칠 방향(검토자 제안): 취소할 때 유닛 대여 층을 재생기 순서와 상관없이 위 층부터 되돌린다. 예를 들어 CancelAll 앞에 창구가 자세를 먼저 되돌리게 하거나, RestoreStand가 그 유닛에 자세가 남았으면 자세를 먼저 접게 한다.
- 검증: 묶음 낮음; 찾은 검토자 G1

### G1-02 [낮음] 걸음·끌어오기 레인과 토네이도 이동 조각의 취소가 보간 중인 Transform을 사실 자리로 되돌리지 않아, F1 정지 화면에 칸 사이 위치가 남는다

- 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:282-303`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:481-486`, `V/Display/Unit/CombatUnitPresentationWindow.cs:398-402`
- 내용: 레인 Cancel은 배치 대여(Placement.Restore)를 사전에서 지우기만 한다. 유닛 Transform은 마지막 보간 위치에 그대로 남는다. CancelMovement 목록은 표시 회전만 사실 회전으로 되돌린다(ClearAllFacing). 위치를 다시 그리는 단계(_facts.Render)는 없다. F1 StopInput은 표시를 놓지 않고 다시 그리지도 않는다. 그래서 칸 사이 위치가 정지 화면에 남는다. 같은 필드 이동 조각끼리도 정책이 다르다. CombatHaboobMovePiece.Cancel은 끝 위치로 옮기지만(TickMove(1f)), CombatTornadoMovePiece.Cancel은 IsEnded만 켜서 토네이도 몸이 중간에 멈춘다. Detach 경로에서는 표시 반환으로 가려지므로 영향은 F1 정지 화면에 한정된다.
- 실패·변경 시나리오: 적 턴에 유닛이 3칸을 걷는 중(2번째 칸 보간 50%)에 다른 처리기의 예외로 F1이 온다. 정지 화면에 그 유닛이 두 칸 사이 허공에 멈춰 있다. 같은 순간 토네이도가 이동 조각 중이면 토네이도 몸도 칸 사이에 남는다. 하붑만 끝 칸으로 옮겨진다.
- 고칠 방향(검토자 제안): CancelMovement 끝에 Mapper가 있으면 보인 사실을 한 번 다시 그린다(KeepsPlacement가 이미 거짓이라 사실 자리로 간다). 필드 몸 이동 조각의 취소 정책(끝으로 옮김 또는 시작으로 되돌림)도 하나로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 G1

### G1-03 [낮음] [보강] 투사체 풀 반환·재대여가 TrailRenderer를 비우지 않아, 연속 발사에서 이전 착탄점부터 꼬리가 이어진다

- 위치: `V/Presentation/Attack/CombatProjectilePool.cs:282-290`, `V/Presentation/Attack/CombatProjectilePool.cs:387-410`, `Assets/ProjectL/Script/Render/Effect/AttackEffect/Bullet/Bullet.prefab:1-205`
- 내용: S17을 확인했다. Return은 꺼진 상태에서 부모·위치·크기만 되돌리고, Start는 위치를 놓은 뒤 켠다. 어느 쪽도 TrailRenderer.Clear를 부르지 않는다. 운영 투사체 Bullet.prefab에는 TrailRenderer(m_Time 0.1, m_Emitting 1)가 있다. Arrow.prefab은 파티클 3개가 지역 공간(moveWithTransform 0)이라 영향이 없다. 반환된 인스턴스를 0.1초 안에 다시 빌리면(연속 사격에서 앞 탄이 닿자마자 다음 탄 발사) 남은 꼬리 점이 이전 착탄점에서 새 총구까지 선을 긋는다. 꼬리 시간이 짧아 영향은 몇 프레임이다.
- 고칠 방향(검토자 제안): Start에서 SetActive(true) 직후(또는 Return에서) 인스턴스의 TrailRenderer를 Clear한다. 인스턴스를 만들 때 TrailRenderer 배열을 한 번 모아 두면 매 발사 GetComponent를 피할 수 있다.
- 검증: 묶음 낮음; 찾은 검토자 G1

### G2-01 [낮음] 진입 숨김(Renderer·_entryHudHidden)이 풀 초기화에 없고, Nexus 취소는 풀로 돌아간 표시를 '다른 개체'로 보고 복원을 건너뜀

- 위치: `V/Display/Unit/FieldUnitView.cs:249-260`, `V/Display/Unit/FieldUnitView.cs:71-79`, `V/Presentation/Entry/CombatEntryNexusAppearance.cs:126-151`
- 내용: FieldUnitView.ResetForPool은 ReleasePresentation·HUD Clear·ShowUnitHud(false)·EntityId 비우기만 한다. 진입 연출이 끈 자식 Renderer.enabled와 _entryHudHidden 깃발은 되돌리지 않는다. 이 둘은 진입 소유자의 Cancel/Restore만 되돌린다. 그런데 두 진입 소유자의 '표시가 바뀐 경우' 정책이 서로 반대다. CombatEntryNexusAppearance.Cancel은 `target.View.EntityId != target.Id`이면 Visibility.Restore와 SetEntryHudHidden(false)를 모두 건너뛴다(주석: 다른 개체로 재사용된 표시는 덮어쓰지 않음). 하지만 ResetForPool이 반환하는 순간 EntityId를 Empty로 만들기 때문에, 반환만 되고 아직 재사용되지 않은 인스턴스도 이 조건에 걸려 숨김이 영구히 남는다. 반대로 CombatEntryEnemyAppearance.Cancel은 신원 검사 없이 Install 때 뜬 스냅숏을 그대로 다시 쓴다. 그래서 재사용된 표시라면 새 주인의 Renderer 상태를 덮어쓴다. 지금 운영 경로에서는 진입 중에 유닛 그리기가 멈추고(CombatViewSystem.TickCore 352), Clear·Release 목록이 CancelEntryPresentation을 _units.Release보다 앞에 두기 때문에 실제로 일어나지는 않는다. 다만 Nexus.RequireView가 '표시가 사라졌다'는 경우를 오류로 다루는 만큼, 그 경우의 정리는 풀 인스턴스를 망가진 채로 남기도록 짜여 있다.
- 고칠 방향(검토자 제안): FieldUnitView.ResetForPool에서 _entryHudHidden=false로 되돌린다. 진입 Renderer 숨김은 FieldUnitView가 소유하는 대여 칸(Begin/Restore, 반환 때 강제 복원)으로 옮긴다. 그러면 두 진입 소유자의 신원 정책 차이와 목록 순서 의존이 함께 사라진다.
- 검증: 묶음 낮음; 찾은 검토자 G2

### G2-02 [낮음] 기존 필드 View 준비 대여가 View 신원을 고정하지 않아, 준비 중 그 View가 반환·재대여되면 옛 HUD 보임 스냅숏을 새 주인에 되돌림

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:504-532`, `V/Display/Unit/CombatUnitPreparationLease.cs:204-209`, `V/Display/Unit/UnitHUDLayer.cs:203-239`
- 내용: PrepareSelectedUnit의 existing 갈래는 _views에서 꺼낸 View로 CombatUnitPreparationLease를 만든다(rented=false). 이 대여는 EntityId·BodyTrackingVersion을 잡아 두지 않는다. Validate도 `_view.Hud != null`과 HUD 층 Preparing만 본다. UnitHUDLayer.BeginPreparation은 시작 때 hud.OwnerVisible을 OwnerWasVisible로 떠 둔다. EndPreparation은 그 값을 그대로 SetOwnerVisible로 되돌린다. 준비는 여러 프레임에 걸쳐 대여를 쥐고 있다(_leases를 끝까지 보유). 그 사이 사실 그리기가 View를 반환하거나 다른 개체로 다시 빌려 주면 Validate는 그대로 통과한다. 그 뒤 EndPreparation이 옛 개체의 보임 값을 새 개체의 HUD에 쓴다. FieldUnitView는 OnEnable·ShowUnitHud·SetEntryHudHidden 때만 SyncHudActive로 다시 맞추므로, HUD가 꺼진 채로 남을 수 있다. 지금은 CombatViewSystem.TickCore가 준비 중에 결과·연출·입력을 진행하지 않는다(352의 IsPreparationPending return). Tick 끝에서도 카메라·호버만 돈다. 그래서 운영에서 닿지는 않는다. 안전이 대여 객체가 아니라 조정자의 '준비 중에는 그리지 않음' 순서에 숨어 있다.
- 고칠 방향(검토자 제안): 대여 생성 때 view.EntityId·BodyTrackingVersion을 잡아 둔다. Validate에서 다르면 실패시키고, Dispose에서 다르면 OwnerVisible을 복원하는 대신 view 쪽 SyncHudActive로 다시 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 G2

### G3-02 [낮음] 입력 소유자 정리가 상태 깃발을 먼저 내린 뒤 보호 없이 단계를 이어 가서, 한 단계가 실패하면 F1 Stop이 다시 정리할 수 없음(숨긴 원본·고스트·가방 Drag 아이콘이 남음)

- 위치: `V/Input/Interaction/PlacementDragInteraction.cs:323-346`, `V/Input/Interaction/CombatItemDragInteraction.cs:140-148`, `V/Input/Interaction/CombatUnitRangeHover.cs:253-281`
- 내용: CombatInteraction은 Stop에서만 소유자마다 CleanupErrors로 보호합니다(479-487). SelectCanceled·BeforeResult·AfterResult·AfterAcknowledged·CancelPresentation·CancelInputs(250-472)는 보호 없는 루프라서 앞 소유자가 던지면 뒤 소유자가 건너뛰어집니다. 이 예외는 Tick·Receiver catch를 거쳐 F1로 가고, RuntimeFaultBoundary가 StopInput → Interaction.Stop으로 다시 정리하는 구조입니다. 그런데 소유자 안의 정리 함수가 상태 깃발부터 내립니다. PlacementDragInteraction.EndDrag는 `_dragging=false`를 먼저 하고 HideGhost → ShowOriginal → _hover.Clear → ClearOwnedPreview를 보호 없이 차례로 부릅니다. CombatItemDragInteraction.End는 `_dragging=false` → _hover.Clear → HideItemDrag 순서입니다. CombatUnitRangeHover.Hide는 `_shown=false` → ClearHoverRange 순서입니다. AttackTargetingInteraction.Exit는 `_active=false` 뒤의 ClearOwnedPreview를 RunAll 밖에서 부릅니다. 그래서 중간 단계 하나가 실패하면 깃발은 이미 '정리됨'입니다. 이어지는 F1 Stop의 같은 정리는 첫 줄의 `if (!_dragging) return`에서 돌아가므로, 소유자마다 둔 보호가 아무것도 되살리지 못합니다. 가방 Drag 아이콘은 Detach 뒤에도 남습니다. Display.Release 목록에 HideItemDrag가 없고, Release 뒤에는 `_isReleased` 때문에 HideItemDrag가 아무 일도 하지 않기 때문입니다. 아이콘은 PackUI의 dragIcon 활성 상태입니다.
- 실패·변경 시나리오: 배치 Drag 중 결과가 와서 Layer 전환(Apply → CancelPresentation 루프)이 placement.EndDrag를 부릅니다. HideGhost 안에서 고스트 객체가 파괴돼 예외가 납니다. _dragging은 이미 false라서 ShowOriginal·hover.Clear·ClearOwnedPreview가 건너뛰어지고, 루프 뒤쪽의 공격·호버 정리도 건너뛰어집니다. F1이 StopInput → Interaction.Stop을 부르지만 배치 Stop의 EndDrag는 바로 돌아갑니다. 그래서 끈 유닛은 오류 화면 아래에서 계속 숨겨져 있습니다. 아이템 Drag에서 hover.Clear가 실패하면 HideItemDrag가 끝내 불리지 않고, Detach의 Display.Release 뒤에는 HideItemDrag가 무시되므로 PackUI의 dragIcon이 켜진 채 남습니다.
- 고칠 방향(검토자 제안): 정리 함수에서 깃발은 마지막에 내리거나, 단계마다 CleanupErrors.Try로 감싸 남은 단계를 시도합니다. 단계 루프(SelectCanceled·BeforeResult·CancelPresentation·CancelInputs)도 Stop처럼 소유자마다 보호하고 실패를 모아 던집니다. Display.Release 목록에 가방 Drag 숨김을 넣습니다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 G3

### G4-01 [낮음] 재바인딩(BindServices 두 번째 호출)이 옛 입력 연결을 놓지 않고 새 CombatInputConnection으로 바꿔 끼움

- 위치: `V/CombatViewSystem.cs:228-233`, `V/CombatViewSystem.cs:283-291`, `V/Input/CombatInputConnection.cs:14-18`
- 내용: BindServices 첫머리는 '재바인딩이 실패해도 이전 재료를 다시 쓰지 않는다'며 _inputFactsReader·_inputSources·_confirmationPresentation을 비운다. 재바인딩을 경로로 인정하는 것이다. 그런데 서비스 수명 객체인 _inputConnection(슬롯에 Bind된 수신 창구와 Layer)은 Release 없이 283-288에서 새 객체로 덮인다. 연결 중(_connection != null)인지도 확인하지 않는다. 이 뒤의 Detach·StopInput은 새 _inputConnection만 Release한다(775-776, 797-798). 그래서 옛 Receiver가 슬롯에 묶인 채 남고 Layer도 비워지지 않는다. 운영의 PostBindService는 런타임당 한 번만 불러서 지금은 닿지 않는다. 다만 재바인딩을 위해 일부 필드만 비우는 지금 구조에서는 이 정리가 반쪽이다.
- 고칠 방향(검토자 제안): 재바인딩을 지원하지 않는다면 두 번째 BindServices를 InvalidOperationException으로 막고 첫머리의 부분 초기화를 지운다. 지원한다면 연결 중이면 거절하고, 옛 _inputConnection은 Release한 뒤 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 G4

## C10 정확성·오류 처리 (추천) — 중간 7 · 낮음 13

### C10-01 [중간] 카메라 이동·줌 뒤 포인터를 다시 집지 않아 공격·배치 호버는 옛 칸에 남고, 클릭은 다른 칸을 고름

- 위치: `V/Input/Cameras/CombatCameraInput.cs:75-84`, `V/Presentation/Cameras/CombatCameraView.cs:139-170`, `V/Input/Interaction/AttackTargetingInteraction.cs:144-181`
- 내용: CombatCameraInput.Tick은 키·가장자리·휠로 Pan/Zoom만 하고, 포인터를 쓰는 소비자에게 알리지 않는다. 포인터가 멈춰 있으면 PointerMoved가 오지 않는다. 공격 조준과 배치 Drag는 PointerMoved 때만 호버 칸을 집고, 유닛 호버 Poll도 다시 집지 않는다. 환경 호버만 매 프레임 다시 집는다. 그런데 SelectReleased는 클릭 좌표로 새 칸을 집어 바로 제출한다. CanMoveCamera는 공격 모드와 Drag 중에도 카메라 이동을 허용한다.
- 실패·변경 시나리오: 공격 모드에서 적 A 칸에 포인터를 두고 영향 칸·예상 체력을 본다. 마우스는 그대로 두고 D 키나 휠로 카메라를 옮긴다. 이제 포인터 아래는 B 칸인데, 미리보기는 계속 A에 남는다. 이 상태로 클릭하면 SubmitTarget(B)가 불려 미리보기와 다른 대상을 공격한다. 배치 Drag도 같아서, 고스트는 A에 있는데 B에 배치된다.
- 고칠 방향(검토자 제안): 시점이 실제로 바뀐 프레임에 마지막 포인터 좌표로 PointerMoved를 한 번 다시 보낸다. 또는 포인터 표본 갱신을 한 곳에 모으고 시점이 바뀔 때도 그곳을 부른다.
- 검증 근거(증거 검증자): 포인터 사건은 StageSelectInput.cs:38-45에서 Performed 때, 곧 값이 바뀔 때만 오고, CombatCameraInput.Tick(CombatCameraInput.cs:75-84)은 Zoom/Pan만 하며 포인터 소비자에게 다시 알리지 않습니다. 카메라 변경을 알리는 장치도 따로 없습니다(grep 결과 없음). 공격 조준 호버는 PointerMoved 때만 칸을 다시 집습니다(AttackTargetingInteraction.cs:144-165). 그런데 SelectReleased는 뗀 좌표로 칸을 새로 집어 SubmitTarget을 부르고, 이때 미리보기했던 칸과 비교하지 않습니다(같은 파일 175-181, 403-416). 배치 Drag도 고스트를 칸에 고정한 채(Placement…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S11

### C10-02 [중간] Unity 프레임 콜백(FieldView.LateUpdate·이펙트 따라가기·토네이도 맥동·HUD 투영)의 예외가 F1 오류 경계에 닿지 않음

- 위치: `V/Display/Field/FieldView.cs:26-30`, `V/Display/Field/FieldView.cs:311-365`, `V/Presentation/Effects/CombatEffectFollower.cs:101-140`
- 내용: 플레이 모드에서 필드 강조는 모두 FieldView.LateUpdate→FlushPending에서 그린다. 이펙트 따라가기·시간 끝(CombatEffectFollower.LateUpdate), 토네이도 화살촉 맥동(FieldTornadoArrowPulse.Update), HUD 위치 투영(Canvas.preWillRenderCanvases→UnitHUDLayer.RefreshPositions)도 Runtime Tick 밖에서 돈다. 이 경로들에는 try/보고가 없어서, 예외가 나면 Unity 로그에만 남고 RuntimeFaultBoundary에는 가지 않는다. 같은 코드베이스의 OnDisable 콜백은 _faults.Report로 F1에 보고하므로 정책이 서로 다르다. FlushPending은 Repaint 전에 대기 칸 표시를 지우고, finally에서 _pendingCells도 비운다. 그래서 Repaint가 던지면 _pendingRefresh까지 잃는다. 토네이도 맥동은 CanContinue·IsHoldingResult도 보지 않아 F1 정지 뒤에도 계속 돈다.
- 실패·변경 시나리오: 공격 모드에서 SetLayerCells(AttackRange)와 SetAffectedCells가 같은 프레임 대기에 들어간다. 그런데 LateUpdate의 FieldOutlineRenderer.Repaint가 예외를 던진다. 그러면 로그만 남고 대기 칸과 Refresh는 사라진다. 화면에는 이전 범위가 보이는데, 상태·입력 정책은 새 범위를 쓴다. 사용자는 보이지 않는 칸을 조준·제출할 수 있고, F1이 서지 않아 전투가 그대로 계속된다. 토네이도 맥동의 Settings 예외는 F1 정지 뒤에도 콘솔에 매 프레임 찍힌다.
- 고칠 방향(검토자 제안): Unity 콜백 입구들을 OnDisable처럼 try/catch로 감싸 RuntimeFaultBoundary.Report로 보낸다. 또는 그리기 플러시·맥동을 CombatViewSystem.Tick 끝(F1 경계 안)의 명시적 호출로 옮긴다.
- 검증 근거(증거 검증자): Runtime Tick에서 난 예외는 GameBootstrap.cs:206-233의 catch가 _faults.Report로 F1에 넘긴다. 같은 코드베이스의 Tick 밖 입구인 CombatViewSystem.cs:805-823 OnDisable, CombatInputReceiver.cs:14, CombatAbandonController.cs:14도 오류 경계에 보고한다. 그런데 FieldView.cs:337 LateUpdate→FlushPending(341-363), CombatEffectFollower.cs:101-140 LateUpdate(_body 콜백·StopNow·Recall), FieldTornadoArrowPulse.cs:22-25 Update→TickPulse(FieldTornadoPrese…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 L08, L13, S07

### C10-03 [중간] Nexus 착지 이펙트가 저절로 끝나기를 기한 없이 기다려, 끝나지 않는 프리팹이면 전투 시작이 영구히 멈춤

- 위치: `V/Presentation/Entry/CombatEntryNexusAppearance.cs:110-123`, `V/Presentation/Effects/CombatEffectPlacement.cs:41-45`, `V/Presentation/Effects/CombatEffectFollower.cs:43-50`
- 내용: IsComplete는 `Landed && !Impact.IsLive`일 때만 참이다. 이펙트는 Stay(자연 끝)로 내므로 시간 끝도 상한도 없다. IsLive는 이펙트가 풀로 돌아가야 거짓이 되고, 끝나지 않는 파티클은 경고 한 줄만 남긴다. 진입을 기다리는 동안에는 입력이 막히고, CombatPlan은 명령은 물론 포기까지 기다리게 한다. 착지 이펙트(FallingRocks)는 낙석과 함께 쓰는 프리팹이다. 그런데 낙석은 이 이펙트의 끝을 기다리지 않으므로, 낙석용으로 프리팹을 고치면 진입만 멈춘다.
- 실패·변경 시나리오: 낙석 연출을 다듬으려고 FallingRocks 루트 파티클을 Looping으로 바꾸거나 ParticleStopNotifier를 뺀다. 그러면 적 Nexus가 있는 스테이지에서 착지 뒤에도 Impact.IsLive가 계속 참이다. 그래서 입력·명령·포기가 모두 영구히 기다리고, 콘솔에는 경고 한 줄만 남는다.
- 고칠 방향(검토자 제안): 착지만으로 완료로 보고 이펙트는 기다리지 않는다. 또는 시간 끝(Seconds) 놓기 값이나 상한 시간을 둔다.
- 검증 근거(증거 검증자): 코드 서술은 맞다. CombatEntryNexusAppearance.cs:118에서 FallingRocks를 Stay(자연 끝, CombatEffectPlacement.cs:41-45)로 내고, :121은 `Landed && !Impact.IsLive`일 때만 완료로 본다. 시간 상한은 없다. IsLive는 받침이 비어야 거짓이 된다(CombatEffectFollower.cs:45-50). 끝나지 않는 파티클이면 CombatEffectWindow.cs:174-176에서 경고만 남긴다. 진입 대기 중에는 IsInputReady(CombatViewSystem.cs:1011-1016)와 CombatPlan.cs:133-144가 명령과 포기를 모두 막는다. 하지만 지금 데이터로는 멈추지 않는다. FallingRoc…
- 검증: 증거 낮음 · 반론 중간 · 판정 중간; 찾은 검토자 S19

### C10-06 [중간] 보드가 올라오는 동안 숨기지 않은 보드 위 물체(오브젝트·장애물·꺼진 Nexus)가 타일보다 먼저 떠 보임

- 위치: `V/Presentation/Entry/CombatBoardEntryPresentation.cs:76-92`, `V/Presentation/Entry/CombatEntryNexusAppearance.cs:54-55`, `V/Display/Unit/FieldUnitPresenter.cs:364-376`
- 내용: 진입 준비가 숨기는 대상은 타일·배경·지형, 선로, (낙하가 켜졌을 때의) 적 Nexus, 필드 적뿐이다. 필드 오브젝트, Nexus가 아닌 구조물(장애물·체력 HUD 포함), 낙하가 꺼진 Nexus는 설치 때 완성 자리에 그려진 채 보인다. 그동안 타일은 Depth 아래에서 차례로 올라온다. 지금 운영 타일 스테이지에는 이런 배치가 비어 있어 드러나지 않았다(잠복).
- 실패·변경 시나리오: 타일 스테이지에 화약통이나 지뢰를 하나 배치하거나 nexusDrop.enabled를 끈다. 그러면 보드 단계(약 1.4초) 동안 그 물체와 HUD가 빈 구멍 위에 완성 높이로 떠 있다.
- 고칠 방향(검토자 제안): 보드 위 표시를 수집 입구 하나로 모아 보드 단계 동안 숨기거나, 칸 타일의 박자에 묶는다.
- 검증 근거(증거 검증자): 코드에서 주장이 그대로 확인된다. CombatBoardEntryPresentation.cs:76-92가 숨기거나 내리는 대상은 타일·배경·지형(CollectBoardRevealTargets), 선로, CollectEntryNexuses가 모은 살아 있는 적 HQ(FieldUnitPresenter.cs:364-376), 필드 적뿐이다. CombatEntryNexusAppearance.cs:55는 낙하가 꺼져 있으면 Hide를 하지 않고, 장애물 같은 HQ가 아닌 구조물과 그 체력 HUD는 FieldUnitPresenter.cs:428-454에서 완성 자리에 그려진다. 필드 오브젝트는 FieldObjectPresenter.cs:231-236에서 타일과 상관없는 FieldObjects 루트 아래 만들어지고(Com…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S19

### C10-07 [중간] 커서 스냅이 옮길 칸 중심이 UI 아래·화면 밖인지 보지 않아 커서를 UI 밑으로 끌어당김

- 위치: `V/Input/Assist/CombatCursorSnap.cs:49-69`, `V/Input/Assist/CombatCursorSnap.cs:13-18`, `V/Display/Feedback/CombatPointerMapper.cs:121-150`
- 내용: CombatCursorSnap은 UI 가림을 포인터 위치로만 확인한 뒤, 칸 중심 화면 좌표로 바로 Warp한다. TryCellCenterToScreen은 카메라 뒤(z<=0)만 거르고, 화면 범위와 UI 가림은 보지 않는다. 그래서 '다음 포인터 이벤트는 같은 칸'이라는 전제가 깨지고, 다음 사건이 UI 위라 _snapped도 비워진다.
- 실패·변경 시나리오: 아래쪽 칸 줄이 덱 UI 밑으로 반쯤 들어간 상태에서, 공격 모드로 그 칸의 보이는 윗부분에 포인터를 넣는다. 커서가 UI 아래의 칸 중심으로 끌려가고, 다시 올려도 또 끌려 내려간다. 그래서 그 칸은 끝내 클릭할 수 없다. 칸 중심이 화면 밖이면 창 모드에서 커서가 게임 창 밖으로 나간다.
- 고칠 방향(검토자 제안): Warp 전에 중심 좌표가 카메라 pixelRect 안이고 UI에 가리지 않았을 때만 옮긴다.
- 검증 근거(증거 검증자): CombatCursorSnap.cs:50-55는 포인터 위치에서만 UI 가림(overCancelArea)을 보고, 65줄에서는 칸 중심 좌표를 받아 곧바로 Warp합니다. 그런데 CombatPointerMapper.cs:121-136의 TryCellCenterToScreen은 z<=0만 거르고 화면 범위나 UI 가림은 보지 않습니다. 따라서 13-18줄 주석의 "옮긴 뒤 다음 이벤트는 같은 칸"이라는 전제는 칸 중심이 UI 밑이나 화면 밖에 있을 때 깨집니다. 운영 씬 Combat.unity:7362-7363에서 snapCursorToGrid=1, threshold=0.15이므로 이 기능은 운영에서 켜져 있습니다. 또 카메라는 Pan·Zoom으로 움직이므로(ICombatCameraControl.cs:16-1…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S11

### C10-08 [중간] FieldView는 Y축 회전 필드를 허용하는데, GridOutline 표시기는 경고 한 번만 남기고 강조를 전혀 그리지 않음

- 위치: `V/Display/Field/FieldView.cs:37-38`, `V/Display/Field/FieldView.cs:608-627`, `V/Display/Field/FieldOutlineRenderer.cs:172-177`
- 내용: FieldView의 툴팁과 ValidateCellsRoot는 Y축 회전을 허용한다. 그런데 FieldOutlineRenderer.Build는 회전을 보면 경고 한 번만 남기고 _isRotated를 켠다. 그 뒤로 Refresh는 매번 바로 돌아가므로 테두리·채우기·경로·호버·데칼이 하나도 그려지지 않는다. 두 부품이 서로 다른 계약을 받아들이고, 계약 위반을 경고로만 넘긴다.
- 실패·변경 시나리오: 스테이지 씬에서 Cells Root를 Y축으로 90도 돌리고 GridOutline 모드로 둔다. Build는 통과하지만 이동·공격 범위·호버가 전혀 보이지 않는다. 콘솔에는 경고 한 줄만 남는다.
- 고칠 방향(검토자 제안): GridOutline 모드의 회전은 ValidateCellsRoot나 표시기 Build에서 예외로 막고, 툴팁도 모드에 맞춘다.
- 검증 근거(증거 검증자): 주장과 코드가 일치한다. FieldView.cs:37 툴팁과 ValidateCellsRoot(FieldView.cs:608-617, Build의 167줄에서 호출)는 Y축 회전을 허용한다. FieldGridMapper(TransformPoint·InverseTransformPoint, 168-203줄)도 회전을 처리하므로 CellPrefab 모드는 회전한 필드에서 정상으로 동작한다. 그런데 FieldOutlineRenderer.Build(172-177줄)는 identity가 아닌 회전을 보면 경고만 한 번 남기고 _isRotated를 켜고, Refresh(225줄)는 그 뒤로 매번 바로 돌아간다. 그래서 GridOutline 모드에서는 강조·경로·호버·데칼이 하나도 그려지지 않는데 Build는 성공으로 끝나…
- 검증: 증거 중간 · 반론 낮음 · 판정 중간; 찾은 검토자 S04

### G3-01 [중간] 배치·아이템 Drag가 결과 재생 중에도 이어지지만, 재생 중에 놓으면 제출이 조용히 거절돼 배치·장착이 안내 없이 사라짐

- 위치: `V/Input/Interaction/PlacementDragInteraction.cs:148-161`, `V/Input/Interaction/PlacementDragInteraction.cs:302-321`, `V/Input/Interaction/PlacementDragInteraction.cs:366-393`
- 내용: BeforeResult 단계에서 배치·아이템 소유자는 아무 일도 하지 않습니다(어댑터에 BeforeResult가 없음). 그래서 결과 재생이 시작돼도 Drag·고스트·원본 숨김이 그대로 남습니다. 미리보기 정책 CanPreview는 '일반 연출 중(S > P)'을 허용하므로 재생 중에도 칸 미리보기가 나가고, Core 답에 따라 고스트가 '놓을 수 있음'(초록)으로 바뀝니다. 그런데 놓는 순간 쓰는 CanSubmit은 IsUnpresented(P < S)이면 거절하고, Gate.TrySubmit은 0만 돌려주고 끝납니다. 배치 SelectReleased는 CommitDrag 다음에 EndDrag를 무조건 부르고, 아이템 SelectReleased도 End를 먼저 부른 뒤 제출합니다. 결국 Drag가 끝나고 원본이 원래 자리로 돌아오며, 거절 안내는 없습니다(Router의 _reportBlockedInput은 튜토리얼 제한에만 쓰임). 같은 Layer가 이어지는 결과(예: 앞서 놓은 유닛의 배치 결과)에서 생기는 흐름입니다.
- 실패·변경 시나리오: 배치 단계에서 유닛 A를 놓아 결과 A가 재생되는 동안 유닛 B를 집습니다. 칸 미리보기 답으로 고스트가 초록이 되고, 재생이 끝나기 전에 놓습니다. CanSubmit이 P<S로 거절하고 TrySubmit은 0을 돌려줍니다. EndDrag로 고스트가 사라지고 B는 원래 자리에 다시 보이며, 배치 명령도 안내도 없습니다. 가방 아이템을 재생 중에 유닛 위에 놓아도 장착이 조용히 사라집니다.
- 고칠 방향(검토자 제안): 재생·후처리 중에는 Drag 시작·미리보기도 막아 CanPreview와 CanSubmit의 경계를 맞춥니다. 아니면 재생 중 놓기는 Drag를 유지(보류)했다가 확인 뒤 제출하거나, 거절 안내를 냅니다.
- 검증 근거(증거 검증자): 배치 쪽 주장은 코드와 맞습니다. 필드에서 집기(PlacementDragInteraction.cs:258-263)와 덱 카드 선택(:74-81)은 CanPreview만 보는데, CanPreview는 P<S 재생 중에도 통과합니다(CombatInputPolicy.cs:95-103, PassesCommon :290-299에는 재생 검사가 없음). 반면 놓을 때 쓰는 CommitDrag→Gate.TrySubmit(:314-321, CombatCommandGate.cs:38-44)은 CanSubmit의 IsUnpresented(:81-84)에 막혀 0을 돌려주고, 반환값은 버려진 채 EndDrag(:159-160)로 고스트가 사라집니다. 배치 명령은 SubmitPending을 쓰지 않아 대기가 없으므로 재생 중 D…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 G3

### C10-09 [낮음] 튜토리얼 제한 검사가 입구마다 달라, 배치 진입은 안내 없이 무시되고 켜 둔 공격 모드는 빠져나갈 수 없음

- 위치: `V/Input/ActionButtons/CombatDeploymentConfirmation.cs:81-95`, `V/Input/ActionButtons/CombatDeploymentEntry.cs:76-81`, `V/Input/ActionButtons/CombatEndTurnConfirmation.cs:61-73`
- 내용: 배치 진입(CombatDeploymentConfirmation.Request)은 CanSubmit으로 먼저 걸러 조용히 돌아간다. 그래서 Router의 EnterCombat 튜토리얼 거절과 막힘 안내에 닿지 않는다. 턴 종료는 반대로 요청을 Router로 넘겨 안내를 받는다. 공격 모드는 제한이 바뀌어도 끝나지 않는다. Router는 AttackRange 미리보기가 막히면 Cancel·AttackToggle을 거절하므로, 제한 전에 켠 공격 모드를 끌 수 없다. 현재 1-1 데이터에서 실제로 도달하는지는 확인되지 않았다.
- 실패·변경 시나리오: MoveOnly 제한 아래 배치 국면에서 전투 진입 키를 누르면 반응도 안내도 없다. 공격 모드를 켠 채 MoveOnly 실습이 시작되면 취소·토글·칸 클릭이 모두 막히고, 턴 종료로만 빠져나갈 수 있다.
- 고칠 방향(검토자 제안): 배치 진입도 Router로 넘겨 막힘 보고를 받게 한다. 제한이 시작될 때 금지된 모드를 끄거나, 그 모드의 해제 입력은 통과시킨다.
- 검증: 묶음 낮음; 찾은 검토자 S10

### C10-10 [낮음] 같은 결과에서 한 유닛의 두 번째 이동 경로는 이동 시작 알림(이동 효과음)을 받지 못함

- 위치: `V/Playback/Ordering/OrderedCombatPresentation.cs:55`, `V/Playback/Ordering/OrderedCombatPresentation.cs:248-249`, `V/Playback/Ordering/OrderedCombatPresentation.cs:290-302`
- 내용: BeginMovement를 보낸 유닛을 결과 단위 HashSet<EntityId>(_movementStarted)로 기억하고, 결과가 끝날 때까지 비우지 않는다. 그런데 IsLastMovementStep은 행동 시작·다른 대상 이동을 경계로 같은 유닛의 경로를 여럿으로 나눈다. 그래서 두 번째 경로의 첫 걸음에서는 Add가 false라 BeginMovement가 생략된다.
- 실패·변경 시나리오: 적 턴 결과에서 적 A가 아군 P를 넉백하고(소리 남), 이어서 적 B가 행동을 시작해 P를 다시 넉백한다. 두 번째 밀림은 소리 없이 재생된다.
- 고칠 방향(검토자 제안): 유닛 집합 대신 '경로 첫 걸음' 판정(IsLastMovementStep과 짝인 판정)으로 알림을 보낸다.
- 검증: 묶음 낮음; 찾은 검토자 L13

### C10-11 [낮음] 추락 사망 갈래가 쓰지도 않는 반응 설정이 있어야 열림

- 위치: `V/Playback/Handlers/CombatCharacterEventHandler.cs:137-145`, `V/Playback/Handlers/CombatCharacterEventHandler.cs:286-298`, `V/Display/Unit/CombatUnitPresentationWindow.cs:300-307`
- 내용: TryBeginDeath는 TryGetReactionSettings가 참일 때만 다음으로 가고, 그 뒤에 HasFallen을 보고 추락 조립으로 간다. 그런데 ComposeFall은 반응 설정을 쓰지 않는다. 또 ReactionSettingsOf는 외형 값의 Reaction을 그대로 돌려주므로 null일 수 있다.
- 실패·변경 시나리오: 반응 값이 없는 유닛이 낭떠러지로 떨어져 죽으면, 추락 연출 없이 일반 사망 경로로 바로 사라진다.
- 고칠 방향(검토자 제안): HasFallen 판정을 반응 설정 확인보다 앞에 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C10-12 [낮음] 공격자 표시가 없을 때 코드 모션 시작 결과가 종류마다 다름(곡사는 false, 직선·근접은 예외)

- 위치: `V/Presentation/Attack/CombatAttackStyle.cs:69-77`, `V/Presentation/Attack/CombatAttackStyle.cs:129-155`, `V/Presentation/Attack/CombatMortarStyle.cs:64-71`
- 내용: TryBeginCodeMotion 문서는 '시작할 수 없는 공격자는 false'라고 한다. 곡사는 표시가 없으면 조용히 false를 돌려준다. 반면 직선·근접의 BeginAttackPose는 같은 조건에서 InvalidOperationException을 던진다. settings 덮어쓰기 경로에서는 표시를 미리 확인하지 않으므로 이 차이가 그대로 드러난다.
- 고칠 방향(검토자 제안): TryBeginMotion에서 표시 유무를 먼저 확인해 false를 돌려주고, 두 전략이 같은 규칙을 따르게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C10-13 [낮음] ForPresentation이 끌어오기 도착 사건을 다시 만들 때 DamageCause·MovementCause·Tornado 정보를 잃음

- 위치: `V/Playback/Support/CombatShownFacts.cs:276-278`, `V/Playback/Support/CombatShownFacts.cs:292-296`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs:120-128`
- 내용: 위치 보존 갈래는 새 CombatCharacterEvent에 모든 칸을 넘긴다. 반면 끌어오기 도착 갈래는 damageOrigins까지만 넘긴다. 그래서 DamageCause는 기본값으로, MovementCause·TornadoSourceId·TornadoContact는 null로 바뀐다. 지금은 View가 이 칸들을 읽지 않아 드러나지 않는다(잠재 결함).
- 고칠 방향(검토자 제안): 두 갈래가 같은 '배치만 바꾼 사건 복사' 도우미를 쓰게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C10-14 [낮음] 배치 Drag에서 칸이 바뀌었는데 미리보기를 못 내면 옛 요청 번호가 남아, 옛 칸의 답이 새 칸에 칠해짐

- 위치: `V/Input/Interaction/PlacementDragInteraction.cs:284-311`, `V/Input/Interaction/PlacementDragInteraction.cs:166-180`, `V/Input/Interaction/AttackTargetingInteraction.cs:612-618`
- 내용: RequestDragPreview는 CanPreview가 거짓이면 _previewRequestNumber를 그대로 둔다. 공격 조준은 새 칸마다 InvalidateHoverPreview로 번호를 0으로 만들지만, 배치는 그렇게 하지 않는다. 그래서 직전 칸 요청의 늦은 답이 오면, PreviewAnswered가 새 호버 칸에 그 가능/불가 색을 칠한다.
- 고칠 방향(검토자 제안): RequestDragPreview 첫머리에서 ClearOwnedPreview()로 번호를 놓는다.
- 검증: 묶음 낮음; 찾은 검토자 S12

### C10-15 [낮음] 흔적 겹침 차례(StackIndexOf)가 놓는 순서와 다른 흔적의 지난 Fact에 의존해 같은 높이로 겹침

- 위치: `V/Display/Map/FieldTrailPresenter.cs:75-83`, `V/Display/Map/FieldTrailPresenter.cs:86-107`, `V/Display/Map/FieldTrailPresenter.cs:165-191`
- 내용: StackIndexOf는 다른 인스턴스의 지난 Fact를 보고, 같은 칸에서 ID가 더 작은 것을 센다. Show는 옮긴 흔적만 다시 계산한다. Render는 ID 순서를 보장하지 않는 environment.Tiles 순서대로 놓는다. 그래서 아직 갱신되지 않은 이웃의 옛 칸을 보고 차례를 정할 수 있고, 주석이 막겠다던 겹침 깜박임이 다음 Render까지 남는다.
- 고칠 방향(검토자 제안): 놓기를 마친 뒤 칸별로 ID 순서대로 높이를 다시 매기거나, Show에서 옛 칸·새 칸의 흔적 높이를 다시 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C10-17 [낮음] unitHudPrefab 누락을 경로마다 다르게 처리함(준비는 예외, 사실 그리기는 HUD 없이 조용히 진행)

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:48-50`, `V/Display/Unit/FieldUnitPresenter.cs:632-639`, `V/Display/Unit/FieldUnitPresenter.cs:848-852`
- 내용: EnsureUnitHud는 unitHudPrefab이 null이면 그냥 돌아간다. DamageTemplate과 PrepareSelectedUnit은 같은 참조를 필수로 보고 던지며, FindMissingReference는 이 칸을 검사하지 않는다. 특성 미리보기 조립(CombatTraitPreviewFactory)은 DamageTemplate 검사 없이 Configure하므로 HUD가 조용히 빠질 수 있다.
- 고칠 방향(검토자 제안): unitHudPrefab을 FindMissingReference에 넣고, EnsureUnitHud의 null 허용을 없앤다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C10-18 [낮음] 진입 중에는 토네이도 생성 전환이 진행되지 않아, 보드 완성 뒤에도 진입이 끝날 때까지 크기가 0

- 위치: `V/Presentation/Entry/CombatEntryDeferredDisplay.cs:64-76`, `V/Presentation/Environment/CombatTornadoAppearance.cs:64-76`, `V/CombatViewSystem.cs:346-361`
- 내용: 보드 완성 때 RevealBoard가 토네이도를 진행 0(크기 0)으로 켠다. 그런데 진행(TickAppearance)은 _presentation.Tick에서만 돌고, CombatViewSystem은 진입을 기다리는 동안 그 호출 앞에서 돌아간다. 그래서 선로·Nexus·적이 등장하는 내내 토네이도만 보이지 않는다. 낙석 경고·하붑은 바로 보이고, 보류 설명과도 다르다.
- 검증: 묶음 낮음; 찾은 검토자 S19

### C10-20 [낮음] 특성이 4개 이상이면 첫 번째 때만 경고하고, 이후로는 조용히 잘라 표시

- 위치: `V/Display/Hud/CombatUnitInfoView.cs:183-200`
- 내용: 고정 슬롯 3개를 넘는 특성은 앞 3개만 그리고, 경고는 view 수명 동안 한 번만 낸다. 그 뒤 다른 유닛의 초과 특성은 아무 신호 없이 빠진다.
- 고칠 방향(검토자 제안): 슬롯 수를 데이터에 맞게 늘리거나, 초과를 설치 검증에서 거절한다.
- 검증: 묶음 낮음; 찾은 검토자 S05

### C10-21 [낮음] 큐 시점 사용 판정에서 플래시만 IsOn을 보고, 세기 0 흔들림은 '사용'으로 셈

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:278-289`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:219-220`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatCueEntries.cs:273-274`
- 내용: UsesAnchor는 플래시는 IsOn일 때만 시점을 쓴다고 보지만, 흔들림은 세기 0(효과 없음)이어도 쓴다고 센다. 그래서 피격 칸에 세기 0 흔들림을 Cue로 둔 유닛은, 피격 클립이 없다는 이유로 DB 검사에서 거절된다.
- 고칠 방향(검토자 제안): 흔들림도 IsOn일 때만 세도록 한 규칙으로 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C10-22 [낮음] 테두리 정렬 순서의 상한 검사가 낙석 느낌표(90)가 아니라 궤적(100)만 봄

- 위치: `V/Display/Field/FieldOutlineRenderer.cs:136-138`, `V/Display/Field/FieldSortingOrders.cs:24-31`
- 내용: 정렬 순서 표는 낙석 경고 느낌표가 모든 칸 테두리 위에 있다고 적었다. 그런데 FieldOutlineRenderer 생성자는 마지막 테두리 순서가 Trajectory(100)에 닿는지만 검사한다. 테두리가 90 이상이 되어 느낌표가 테두리 아래로 깔려도 검사는 통과한다.
- 고칠 방향(검토자 제안): 검사 상한을 FieldSortingOrders.RockfallWarningMark로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### G3-03 [낮음] '활성 전경 소유자는 하나' 불변식을 아무도 확인하지 않음: 경로 배타는 호출자 순서에, 넘겨받기는 BeginForeground 밖의 직접 전환에 기댐

- 위치: `V/Input/Interaction/CombatInteraction.cs:309-338`, `V/Input/MovePath/CombatPathInput.cs:696-748`, `V/Input/Interaction/CombatAttackDragHandover.cs:78-95`
- 내용: BeginForeground는 다음 소유자가 아닌 전경 소유자에게 CancelPresentation을 부릅니다. 그런데 경로 어댑터는 CancelPresentation을 재정의하지 않아(기본 = 할 일 없음) 경로 지정이 꺼지지 않습니다. 그래서 공격 모드 진입이 경로와 배타가 되는 것은 Router.EnterAttackMode가 LeaveMoveMode를 먼저 부르기 때문일 뿐입니다. 공격 Drag 넘겨받기는 BeginForeground를 거치지 않고 _attack.Exit() 뒤 TryBeginTransferredDrag로 경로를 직접 켭니다. 지금은 배치·아이템이 배치 Layer에만 있고 AttackModeRequested 호출자가 Router 하나뿐이라 두 소유자가 함께 활성이 되지 않습니다. 하지만 IsInputOwnerActive는 '하나라도 활성'만 보고, Router.ResolvePointerTarget은 Layer와 공격·아이템 깃발로만 대상을 고릅니다. 둘이 함께 활성이 되어도 이를 잡는 검사(예외·F1)는 어디에도 없습니다.
- 고칠 방향(검토자 제안): BeginForeground에서 경로도 CancelInputs하도록 하거나(경로 어댑터가 CancelPresentation을 재정의), 활성이 된 뒤 활성 전경 소유자 수를 세어 둘 이상이면 InvalidOperationException을 던져 F1로 보냅니다.
- 검증: 묶음 낮음; 찾은 검토자 G3

## C11 핫패스 힙 할당 (추천) — 중간 1 · 낮음 16

### C11-02 [중간] 유지 이펙트 맞추기가 Render마다 필드 유닛마다 상태 읽기를 새로 할당하고, HUD도 같은 유닛에 또 할당한다

- 위치: `V/Presentation/Effects/CombatStatusEffectSource.cs:29-41`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StatusEffectReadModel.cs:15-44`, `V/Display/Unit/CombatUnitStatuses.cs:33-40`
- 내용: CombatStatusEffectSource.Collect는 필드 위 살아 있는 유닛마다 StatusEffectReadModel.Capture(unit)를 부르고, Capture는 호출마다 new List·LINQ(OfType·Select·ToArray)·스냅샷 객체를 만든다. Collect는 CombatShownFacts.Render/SyncPersistent 때마다(사건 순간·걸음 도착·캐릭터 사건·결과 끝) 돈다. 같은 Render에서 HUD 쪽 CombatUnitStatuses.AddTo도 같은 유닛에 Capture를 또 부른다. 사건 하나에 (필드 유닛 수 × 2)번 할당이 생긴다.
- 실패·변경 시나리오: 필드 유닛 12명, 걸음 도착이 이어지는 연쇄 이동 재생에서 도착마다 Render가 돌고 Capture가 24번(유지 이펙트 12 + HUD 12) 불려 List·배열·스냅샷이 수십 개씩 할당된다. 결과 재생 중 GC가 잦아진다.
- 고칠 방향(검토자 제안): Render 한 번에 유닛별 Capture 결과를 한 번 구해 HUD와 유지 이펙트가 함께 쓰거나, 사실이 바뀐 유닛만 다시 읽는다.
- 검증 근거(증거 검증자): CombatStatusEffectSource.cs:29-34는 CombatPersistentEffects.Sync(CombatPersistentEffects.cs:56-58)가 불릴 때마다 필드 위 살아 있는 유닛 전부에 StatusEffectReadModel.Capture를 다시 부른다. Capture(StatusEffectReadModel.cs:19-44)는 한 번에 List, Sources 6회(OfType·Select 반복기, ToArray, AsReadOnly, 원천 스냅샷), new[] 키 배열, 캡처 람다 Where 2회, OrderBy·ThenBy·ToArray를 만들므로 유닛 하나에 수십 개를 할당한다. Sync는 CombatShownFacts.Render(325-327)를 통해 사건마다 돈…
- 검증: 증거 중간 · 반론 중간; 찾은 검토자 S18

### C11-01 [낮음] 이동 모드가 아닐 때 매 Tick 부르는 이동 범위 Clear가 일찍 돌아가는 경로에서도 캡처 환경을 매번 만든다

- 위치: `V/Input/MovePath/CombatMovementRangeTracker.cs:115-138`, `V/Input/MovePath/CombatPathInput.cs:631-638`, `V/Input/MovePath/CombatPathInput.cs:726-733`
- 내용: CombatViewSystem.TickCore → CombatInteraction.Tick → CombatPathInput.Poll → Refresh → RefreshMoveModeRange가 매 프레임 돈다. 이동 모드 행위자가 없으면(배치 단계·적 턴·선택 없음 등 대부분의 시간) 637줄에서 _range.Clear()를 부른다. Clear는 121줄 `if (IsEmpty) return;`으로 끝나지만, 메서드 본문 최상위 지역 `number`·`shown`과 this를 131~138줄 람다가 캡처하므로 Roslyn은 캡처 환경(display class)을 메서드 진입 시점에 만든다. 117줄 주석의 '정리 대리자를 만들지 않습니다'라는 의도와 달리 평상시 프레임마다 객체 하나가 할당된다.
- 실패·변경 시나리오: 전투 입력 중 아무 유닛도 이동 모드가 아닌 상태로 60fps가 이어지면 이동 범위가 비어 있어도 Clear가 프레임마다 캡처 객체(약 32B)를 만든다. 1분에 약 3,600개가 쌓여 주기적인 GC 수집을 부른다.
- 고칠 방향(검토자 제안): 캡처하는 부분을 별도 private 메서드로 옮겨 IsEmpty 검사 뒤에만 진입하게 한다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L14

### C11-03 [낮음] 끌어당김 걸음 보간 중 Core 사실 객체(CharacterCombatFacts)를 매 프레임 다시 만든다

- 위치: `V/Presentation/Movement/CombatUnitMotionPlayback.cs:178-198`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterCombatFacts.cs:93-95`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterCombatFacts.cs:207-223`
- 내용: TickPull은 while 안에서 프레임마다 `At(_pullFrom, _pull.Path[_pullIndex])`를 부른다. At은 `new[] { cell }`을 만들고 WithPlacement → Rebuild를 거쳐 새 CharacterCombatFacts, 칸 배열 복사·ReadOnlyCollection, 디버프·특성 효과 투영(Import/LINQ)을 돌린다. `next`는 _pullIndex가 바뀔 때(칸 하나 지날 때)만 달라지므로 칸마다 한 번이면 된다. 생성 비용은 Core 쪽이지만 프레임마다 부르는 결정은 범위 안 View 코드다. 끌어당김 재생 중에만 도는 경로다.
- 실패·변경 시나리오: 넉백 끌어당김 공격으로 유닛 둘 이상이 각각 3~4칸 끌려가면 칸마다 보간이 몇 프레임씩 이어지고, 그동안 프레임마다 유닛당 배열 2개·ReadOnlyCollection·사실 객체·LINQ 열거자가 생겨 이미 무거운 공격 프레임에 GC 압력이 더해진다.
- 고칠 방향(검토자 제안): _pullIndex가 바뀔 때만 next를 만들어 필드에 들고 있는다.
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L14, S20

### C11-04 [낮음] 환경 설명 호버가 칸이 바뀔 때마다 ReadTornadoAreas(LINQ·새 객체)를 2~3번 부르고 인터페이스 foreach 4개로 열거자를 박싱한다

- 위치: `V/Input/Assist/CombatEnvironmentText.cs:51-107`, `V/Input/Assist/CombatEnvironmentHover.cs:93-151`, `S/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs:23`
- 내용: CombatEnvironmentHover.Refresh는 칸이 바뀔 때 _formatter.Build를 부르고, Build는 87줄에서 state.ReadTornadoAreas(Owner.Player)를 부른 뒤 Hover 141줄에서 같은 호출을 또 한다(클릭 프레임에는 도착 칸 계산까지 세 번). 이 호출은 Core에서 OfType·Where·Select·ToArray로 TornadoAreaFact 목록을 새로 만든다. Build의 foreach 네 개(51·78·87·94줄)는 IReadOnlyList로 열거해 열거자를 박싱한다. 같은 폴더 CombatEnvironmentFacts는 '포인터마다 불릴 수 있어 인터페이스 열거자를 만들지 않는다'는 규칙을 두며 같은 파일의 다른 도우미는 for를 쓴다. 같은 칸 캐시가 있어 매 프레임은 아니다.
- 고칠 방향(검토자 제안): 상태(불변 객체)당 토네이도 영역을 한 번 읽어 Build와 Collect가 함께 쓰고, foreach를 인덱스 for로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 L14, S11

### C11-05 [낮음] 호버·범위 미리보기 응답마다 칸 목록을 ToArray로 복사해 표시에 넘기지만 받는 쪽은 한 번 훑고 끝난다

- 위치: `V/Input/Interaction/AttackTargetingInteraction.cs:320-327`, `V/Input/Interaction/AttackTargetingInteraction.cs:339-343`, `V/Input/Interaction/CombatUnitRangeHover.cs:146-151`
- 내용: 공격 모드 호버 칸 응답마다 attack.AffectedCells.ToArray()(342줄), 범위 응답에서 RangeCells·BasicAttackCells·AttackAims ToArray(320·327줄), 유닛 호버 응답마다 RangeCells·MoveCells.ToArray()(147~148줄)로 새 배열을 만든다. 받는 쪽 ShowAffectedCells·ShowRangeCells·ShowHoverRange(→ FieldView/FieldHighlightState.SetLayerCells)는 IReadOnlyList를 한 번 훑어 표시하고 참조를 보관하지 않으므로 복사는 형 변환용일 뿐이다. 포인터가 칸을 바꿀 때마다 돈다.
- 고칠 방향(검토자 제안): 표시 계약을 ReadOnlySpan/ReadOnlyMemory로 받거나 재사용 목록에 복사한다.
- 검증: 묶음 낮음; 찾은 검토자 L14, S12

### C11-06 [낮음] 공격 조준 표시가 응답마다 인터페이스 foreach로 열거자를 박싱하고 끌어오기 도착 칸 배열을 새로 만든다

- 위치: `V/Display/Feedback/CombatAttackFeedback.cs:52-66`, `V/Display/Feedback/CombatAttackFeedback.cs:136-141`, `V/Display/Feedback/CombatAttackFeedback.cs:153-161`
- 내용: CombatAttackFeedback.ShowRangeCells·ShowAttackBlockers가 IReadOnlyList<CellCoord>·contact.WallCells를 foreach로 돌아 열거자를 박싱하고, ShowAffectedCells는 끌어오기가 있으면 응답마다 `new[] { pull.FinalAnchor }`를 만든다. 조준 칸이 바뀔 때마다 도는 경로다.
- 고칠 방향(검토자 제안): 인덱스 for로 바꾸고 한 칸 배열은 재사용 버퍼로 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C11-07 [낮음] 열린 변 계산이 호버 갱신마다 인터페이스 foreach로 열거자를 여러 번 박싱한다

- 위치: `V/Display/Field/FieldAttackOutlineOpening.cs:22-24`, `V/Display/Field/FieldAttackOutlineOpening.cs:35-40`, `V/Display/Field/FieldAttackOutlineOpening.cs:83-90`
- 내용: FieldAttackOutlineOpening은 IReadOnlyList<CellCoord>(실체 ReadOnlyCollection)인 HoverRangeSourceCells와 IReadOnlyList(실체 배열)인 FieldHighlightLayers.DrawOrder를 foreach로 돈다. 호버 칸이 바뀔 때마다 도는 Refresh 안에서 채우기 종류마다·테두리 슬롯마다 불려 갱신 한 번에 할당이 여러 번 생긴다.
- 고칠 방향(검토자 제안): 인덱스 for로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S03

### C11-08 [낮음] 타격·사건마다 인터페이스 컬렉션을 foreach로 돌아 열거자를 박싱한다

- 위치: `V/Presentation/Attack/CombatHitGate.cs:99-107`, `V/Presentation/Attack/CombatHitFollowUps.cs:104-106`, `V/Presentation/Attack/CombatHitFollowUps.cs:119-128`
- 내용: CombatHitGate.ReleaseAll은 HitTargetsOf가 IReadOnlyCollection<EntityId>로 돌려준 HashSet을 foreach로 돌아 타격 사건마다 열거자를 박싱한다. CombatHitFollowUps.TryFindSource도 결과의 사건마다 IReadOnlyList<CombatDamageOrigin>을 foreach로 돈다.
- 고칠 방향(검토자 제안): HitTargetsOf를 구체 HashSet(또는 struct enumerator를 주는 타입)으로 돌려주고 목록은 인덱스 for로 돈다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C11-09 [낮음] 특성 프로필 조회 Get이 IReadOnlyList 필드를 foreach로 돌아 호출마다 열거자를 할당한다

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs:38-65`, `V/Playback/Support/CombatTraitMovementCount.cs:15-25`, `V/Playback/Support/CombatTraitStateEvidence.cs:38-43`
- 내용: CombatTraitFeedbackProfiles.Get은 Array.AsReadOnly로 만든 IReadOnlyList 필드를 foreach로 돌아 호출마다 IEnumerator를 힙에 만든다. CombatTraitMovementCount.Before가 결과 사건을 거꾸로 훑는 LINQ Any 람다 안에서, 상태 근거 Matches가 상태 사건마다 부른다.
- 고칠 방향(검토자 제안): 내부 배열을 private로 두고 Get은 배열을 for로 돈다.
- 검증: 묶음 낮음; 찾은 검토자 S25

### C11-10 [낮음] 기차 경과 콜백 Advance가 매개변수 캡처 람다 때문에 매 프레임 캡처 환경을 할당한다

- 위치: `V/Presentation/Railway/CombatRailwayContactPlayback.cs:167-205`, `V/Display/Map/FieldRailwayPresenter.cs:230-246`
- 내용: FieldRailwayPresenter.Tick이 기차가 달리는 동안 매 프레임 _advanced를 부르고 CombatRailwayContactPlayback.Advance(group, version, ...)로 이어진다. Advance의 while 안 사망 완료 람다가 매개변수 group·version을 캡처하므로 컴파일러가 메서드 진입마다 캡처 환경을 만들어, 접촉이 없는 대부분의 프레임에도 할당이 생긴다.
- 고칠 방향(검토자 제안): 접촉 사망 시작 부분을 별도 메서드로 분리해 접촉이 있을 때만 캡처 환경이 생기게 한다.
- 검증: 묶음 낮음; 찾은 검토자 L14

### C11-11 [낮음] 조각 실행 Tick의 _tickSnapshot.AddRange(_runs)가 Mono에서 매 프레임 임시 배열을 할당한다

- 위치: `V/Playback/Players/CombatPieceRunPlayer.cs:121-145`
- 내용: 할당을 피하려고 스냅샷 목록을 재사용하지만, Unity Mono BCL(referencesource)의 List<T>.InsertRange는 ICollection<T> 인자를 받으면 new T[count]에 복사한 뒤 옮긴다. 그래서 실행이 하나라도 있는 동안 매 프레임 작은 배열이 할당된다. 반복문으로 Add하면 할당이 없다.
- 고칠 방향(검토자 제안): for 반복문으로 Add한다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C11-12 [낮음] 사건마다 도는 HUD 다시 쓰기·구조물 피해 숫자에서 문자열·박싱·대리자를 할당한다

- 위치: `V/Display/Unit/CombatUnitStatuses.cs:37-45`, `V/Display/Unit/CombatEffectFormatters.cs:29-36`, `V/Display/Unit/CombatStatusDescriptionFormatter.cs:23-55`
- 내용: 사실이 바뀐 유닛마다 CombatUnitStatuses.AddTo가 "StatusEffect:"+(int)key 문자열을 만들고, CombatEffectFormatter.Values가 object[]와 int 박싱을, Describe가 StringBuilder와 보간 문자열을 만든다. CombatStructureDamage.Show는 피해 숫자마다 Rental 객체와 Return 메서드 그룹 대리자를 새로 만든다. 사건 단위 경로라 낭비는 작다.
- 고칠 방향(검토자 제안): 상태 키 문자열을 키별로 캐시하고 Return 대리자는 필드에 한 번 만들어 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S08

### C11-13 [낮음] 사건·결과 끝마다 흔적 이름 문자열을 새로 만들고, 토네이도 자리 계산마다 Footprint 목록을 새로 할당한다

- 위치: `V/Display/Map/FieldTrailPresenter.cs:165-178`, `V/Display/Map/FieldTornadoPresenter.cs:218-227`, `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoGeometry.cs:25-34`
- 내용: FieldTrailPresenter.Apply는 Show 사건과 결과 끝 Render마다 흔적 하나당 $"FieldTrail_{Key}_{Id}"를 새로 만들어 이름을 다시 쓴다. 열쇠와 ID는 인스턴스를 빌릴 때 정해지므로 그때 한 번이면 된다. FieldTornadoPresenter.BodyPosition은 Place·TryGetBodyMove·Render마다 TornadoGeometry.Footprint를 불러 새 List를 만드는데 Render에서는 area.Cells에 같은 칸 목록이 이미 있다. 사건 빈도라 크지 않다.
- 고칠 방향(검토자 제안): 이름은 대여 때 한 번 쓰고, Render는 area.Cells를 그대로 쓴다.
- 검증: 묶음 낮음; 찾은 검토자 S07

### C11-14 [낮음] 피격·사망 반응 하나마다 Entry·Presenter와 캡처 람다 여러 개를 새로 만든다

- 위치: `V/Presentation/Reaction/CombatUnitReactionSet.cs:23`, `V/Presentation/Reaction/CombatUnitReactionSet.cs:57-69`, `V/Presentation/Reaction/CombatUnitReactionSet.cs:78-92`
- 내용: 반응 시작마다 Entry와 그 안의 new CombatUnitReactionPresenter, settings·pose·damage·mapper를 잡는 시작 람다, 완료·불가 람다 두 개가 힙에 할당된다. 다수 피격 사건에서 사건 수에 비례한다.
- 고칠 방향(검토자 제안): Entry·Presenter를 유닛별로 재사용하고 콜백은 Entry 메서드로 한 번 만들어 둔다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C11-15 [낮음] 몸 글자 표시 하나마다 Entry·Rental과 추적 대리자 두 겹을 새로 만든다

- 위치: `V/Presentation/Text/CombatBodyTextWindow.cs:62-84`, `V/Presentation/Text/CombatBodyTextPool.cs:54-55`, `V/Display/Unit/FieldUnitPresenter.cs:184-208`
- 내용: 풀이 GameObject 생성 비용은 미리 만들기로 없앴지만, Show 한 번마다 new Entry, Pool.Rent의 new Rental, _track(unit)이 만드는 클로저 두 개(TrackBodyPresence의 람다와 그것을 감싼 람다)가 할당된다. 특성 발동 때마다 생긴다.
- 고칠 방향(검토자 제안): Entry·Rental을 풀 항목에 묶어 재사용하고 추적 대리자를 한 겹으로 줄인다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C11-16 [낮음] 결과마다 짜는 특성 피드백 계획이 중첩 루프 안에서 캡처 람다 LINQ를 반복한다

- 위치: `V/Playback/Support/CombatTraitFeedbackPlan.cs:119-140`, `V/Playback/Support/CombatTraitFeedbackPlan.cs:160-170`, `V/Playback/Support/CombatTraitMovementCount.cs:15-27`
- 내용: CombatTraitFeedbackPlan.Build는 발동 특성마다·대상마다 뒤 사건을 훑으며 `other.Traits.Any(t => ...trait...)` 캡처 람다를 사건마다 새로 만든다(127·168줄). CombatTraitMovementCount.Before도 같은 패턴이다. CombatTraitStateEvidence는 ToArray().OfType().SingleOrDefault(), Where().Sum(), Where().ToArray()로 사건 하나에 여러 번 할당한다. 결과 하나에 한 번 도는 경로지만 사건 수 × 발동 수 × 대상 수만큼 늘어난다.
- 고칠 방향(검토자 제안): Any 람다를 for 반복 도우미로 바꾸고 상태 근거는 한 번 훑어 값을 모은다.
- 검증: 묶음 낮음; 찾은 검토자 L14

### C11-17 [낮음] 직선 투사체 발사마다 경고용 대상 이름을 enum.ToString으로 미리 만든다

- 위치: `V/Presentation/Attack/CombatAttackPlayback.cs:321-345`
- 내용: LinearContactProgress는 발사 경로가 정해질 때마다 key.ToString()(EntityKey enum)을 ContactProgress의 subject 인자로 넘긴다. subject는 몸과 궤적이 닿지 않을 때의 진단 경고에서만 쓰이는데, enum.ToString은 새 string 할당이라 정상 경로에서도 비용이 든다.
- 고칠 방향(검토자 제안): EntityKey 값을 넘기고 경고를 낼 때만 문자열로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 L14

## C12 주석·문서 정합성 (추천) — 중간 0 · 낮음 24

### C12-01 [낮음] 운영 코드 주석이 저장소에 없는 시험·시험 도우미·리플렉션 계약을 근거로 든다

- 위치: `V/Display/Unit/FieldUnitView.cs:323-330`, `V/Playback/CombatSceneEventPresenter.cs:41-46`, `V/Playback/CombatSceneEventPresenter.cs:85-87`
- 내용: 주석이 CombatUnitPresentationWindowTests의 …CancelsPoseBeforeScale, CombatViewSystemTests.EventValues·FieldPorts, 시험 어셈블리의 CombatInputTestFactory, '시험이 리플렉션으로 읽는다(_attacks·_reaction·_motion)', findLayer '시험이 바꿈'을 근거로 든다. 저장소 전체(.cs)를 grep하면 이 이름들은 주석 자신에만 나온다. Assets/Tests에는 CombatSceneFactory를 쓰는 곳도 없다. 특히 FieldUnitView.ReleasePresentation은 대여 다섯을 되돌리는 순서가 깨지기 쉬운데, 주석은 이 순서를 시험이 지킨다고 적는다. '시험 조립은 가짜를 넘깁니다'처럼 지금 없는 호출자를 들어 null 허용 갈래를 설명하는 주석도 여러 곳에 있다.
- 실패·변경 시나리오: 누군가 ReleasePresentation에서 자세·크기 대여를 되돌리는 순서를 바꾼다. 주석을 믿고 시험이 잡아 주리라 여기지만 그 시험이 없어서 아무것도 실패하지 않는다. 자세 취소 콜백이 크기 복원보다 늦게 돌면, 끝 자리나 크기가 어긋난 채 풀로 돌아간다. 반대로 없는 리플렉션 계약 때문에 _attacks 등의 필드 이름을 바꾸지 못한다.
- 고칠 방향(검토자 제안): 없는 시험·도우미를 가리키는 문구를 지운다. 순서 제약은 주석에 제약 자체만 남긴다(필요하면 코드에서 단언한다).
- 검증: 증거 낮음 · 반론 낮음; 찾은 검토자 L06, L11, L15, S09, S24

### C12-02 [낮음] 입력 묶음 설명의 개수('여덟')·목록·조건이 지금 12개 객체 구성과 다름

- 위치: `V/CombatViewSystem.cs:688-691`, `V/Input/ICombatSceneInputFactory.cs:21`, `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs:58-63`
- 내용: CombatViewSystem.CreateInput, ICombatSceneInputFactory, CombatSceneInputFactory는 입력 객체가 '여덟'이라고 적는다. 그러나 CombatSceneInput은 12개(gate·interaction·router·deck·item·hover·notices·deployment·endTurn·availability·camera·undo)를 받는다. CombatSceneInput 요약 목록에도 턴 종료·이동 되돌리기가 빠져 있다. 그 밖에 어긋난 설명: '세 표면'(실제 넷), 대기를 '시작/턴 종료'로만 적음, IsInputAvailable에 진입 연출 조건 없음, 확인창 presentation '(시험 조립) null', OnInputClosed '배치 확인창 닫기'(실제로는 둘을 닫음), 사실 읽기 '쓰기 안 함'(무효화를 함). 그리고 CombatSceneDisplayFactory의 '바닥 타일 높이 표…' 주석이 CombatInspectionPresenter 생성 위에 붙어 있어, 그 아래 null 인자가 타일 높이처럼 읽힌다.
- 고칠 방향(검토자 제안): 개수 대신 목록을 한 곳(CombatSceneInput 요약)에만 적고, 나머지는 그곳을 참조한다. 타일 높이 주석은 Display 생성자 인자 옆으로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 L15, S10, S24

### C12-03 [낮음] 조정자 주석이 지운 '타임라인 Core 직접 조회'와 옛 동작을 설명함

- 위치: `V/CombatViewSystem.cs:27`, `V/CombatViewSystem.cs:56-66`, `V/CombatViewSystem.cs:97-98`
- 내용: CombatViewSystem은 '타임라인은 아직 Core에 직접 묻습니다. 묻는 자리는 표시 값 읽기 하나'라고 적고, _displayValues 필드 주석과 CombatDisplayValueReader.Read 요약도 타임라인 조회(전투가 없으면 예외)를 말한다. 그러나 CombatDisplayValueReader는 Buffer만 읽으며, 자기 remarks에도 'Core를 호출하지 않습니다'라고 적는다. 두 파일에는 쓰이지 않는 CoreSystem.CombatSystem using도 남아 있다. 같은 조각의 다른 낡은 설명은 다음과 같다. CombatRefreshSignal·CombatViewRefresh의 'Detach만 한다'(실제는 F1 보고 뒤 분리), CombatDisplayServices 'Render·Audio 묶음'(UI·카메라 효과 포함), CombatSceneRoot '동작은 없고'(포커스·비활성 이벤트 전달), CombatViewConnection 상태 목록 누락.
- 고칠 방향(검토자 제안): 타임라인·Core 조회 문구와 쓰이지 않는 using을 지우고, 조정자 주변 요약을 지금 동작에 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 L04, S01

### C12-04 [낮음] 유닛 표시(Display/Unit) 요약이 지금의 의존·책임·대여 방식과 다르고 고아 XML doc이 남음

- 위치: `V/Display/Unit/FieldUnitPresenter.cs:557-562`, `V/Display/Unit/FieldUnitPresenter.cs:9-11`, `V/Display/Unit/FieldUnitPresenter.cs:31-37`
- 내용: (1) FieldUnitPresenter 559줄에 지운 Forecast 메서드의 summary가 홀로 남아 있어, SetHiddenEntity 위에 summary가 둘이 된다. (2) FieldUnitPresenter remarks는 'Buffer·Core를 모릅니다'라고 하지만 CharacterData(Buffer)와 CombatEffectiveStatsCalculator(Core)를 쓴다. (3) HudFeedback이 '비용'을 맡는다고 하지만 비용이 없다. CombatUnitHudFeedback은 '턴 순서·목표 표시'를 쓴다고 하지만 쓰지 않는다. CombatObjectiveDisplay 요약의 멤버는 쓰이지 않는다. (4) FieldUnitView 요약은 '위치·방향·표시·체력 숫자만'이라 하지만 실제로는 HUD·대여 다섯·외형·클립까지 소유한다. (5) ICombatUnitAppearance '플래시는 B 단계에서'(이미 ICombatUnitFlash로 분리됨), ICombatUnitPresentationTarget '버전으로 늦은 복원 방지'(지금은 참조 비교), UnitPresentationLeaseSlot 채널 목록에 서는 자리 누락.
- 고칠 방향(검토자 제안): 고아 summary를 지우고, Display/Unit 요약을 실제 의존·소유 목록과 대여 방식에 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 L15, S08, S09

### C12-05 [낮음] 없는 타입 이름 'ColorScop'으로 색 손잡이를 설명하고 칸 부모 설명이 틀림

- 위치: `V/Display/Field/FieldCellHandle.cs:10-13`, `V/Display/Field/FieldCellHandle.cs:19`, `V/Display/Field/FieldCellHandle.cs:35`
- 내용: FieldCellHandle과 PlacementGhost 주석은 색을 'ColorScop'으로 바꾼다고 적고 'Scope 반환'을 말한다. 하지만 이 이름의 타입은 없고, 실제로 쓰는 것은 IColorHandle(CreateColorHandle)이다. 필드 이름 _colorScopes에도 옛 이름이 남았다. FieldCellFill remarks 18줄은 표시 오브젝트를 '필드 루트 아래'에 만든다고 하지만, 17줄과 실제 부모는 칸 루트(cellsRoot)다.
- 고칠 방향(검토자 제안): ColorScop/Scope 표기를 IColorHandle로, _colorScopes를 _colorHandles로 바꾸고, FieldCellFill 부모 설명을 칸 루트로 고친다.
- 검증: 묶음 낮음; 찾은 검토자 L15, S03

### C12-06 [낮음] PlacementGhost '유효 색 하나만'과 ICombatBattleDisplay.Refresh '동적 값만' 설명이 지금 동작과 다름

- 위치: `V/Display/Feedback/PlacementGhost.cs:19-22`, `V/Display/Feedback/PlacementGhost.cs:81-99`, `V/Display/ICombatBattleDisplay.cs:46-47`
- 내용: PlacementGhost remarks는 '배치 가능 여부 색은 후속 범위라 지금은 유효 색 하나만 씁니다'라고 한다. 하지만 SetPlaceable은 Core 배치 응답에 따라 DB의 유효·무효 색을 고르고, 이동 고스트는 중립 색까지 쓴다. ICombatBattleDisplay.Refresh는 '동적 값(유닛·구조물·HUD)만 반영'이라 하지만, 구현은 지형 높이·오브젝트·흔적·기믹·가방·정보창·배경음까지 다시 그린다.
- 고칠 방향(검토자 제안): 두 설명을 지금 색 선택·다시 그리기 범위에 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S02

### C12-07 [낮음] FieldView·FieldOutlineRenderer의 메서드 XML doc이 엉뚱한 메서드 위에 붙음

- 위치: `V/Display/Field/FieldView.cs:241-256`, `V/Display/Field/FieldOutlineRenderer.cs:366-376`
- 내용: FieldView에서 SetLayerCells를 설명하는 '한 층의 칸을 바꿉니다…' doc이 SetHoverRangeSourceCells 위에 있다. 그래서 SetLayerCells에는 설명이 없고, SetHoverRangeSourceCells는 틀린 설명을 단다. FieldOutlineRenderer의 '테두리 고리 모양…' 주석도 LoopInsetOf가 아니라 HideArea 위에 있다.
- 고칠 방향(검토자 제안): doc을 해당 메서드 위로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C12-08 [낮음] CombatMovementPresentation의 BeginStep 설명이 _placements 필드 위에 붙음

- 위치: `V/Playback/Support/CombatMovementPresentation.cs:41-55`
- 내용: '걸음 하나를 그 유닛의 레인에서 시작합니다… completed는 걸음 사건 완료 통지' summary가 _placements 필드에 붙어 있고, 정작 BeginStep에는 문서가 없다(paramref가 필드에 달려 있음).
- 고칠 방향(검토자 제안): summary를 BeginStep 위로 옮긴다.
- 검증: 묶음 낮음; 찾은 검토자 S16

### C12-09 [낮음] 이동 경로 부품 설명은 '그림자를 설정하지 않음'이지만 화살촉은 그림자를 켬

- 위치: `V/Display/Field/FieldMovementPathLine.cs:19`, `V/Display/Field/FieldMovementPathLine.cs:73`, `V/Display/Field/FieldArrowhead.cs:60-61`
- 내용: FieldMovementPathLine remarks는 '그림자·정렬 순서는 설정하지 않습니다(설계 P5)'라고 한다. 하지만 Build는 FieldArrowhead에 shadows: true를 넘기고, 화살촉은 그 값으로 shadowCastingMode=On, receiveShadows=true를 설정한다.
- 고칠 방향(검토자 제안): remarks에 화살촉 그림자 예외를 적거나 동작을 설명과 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S04

### C12-10 [낮음] FieldView 강조 우선순위·묶음 수 주석이 원본 층 표(FieldHighlightLayers.Rows)와 어긋남

- 위치: `V/Display/Field/FieldView.cs:40-41`, `V/Display/Field/FieldView.cs:91-93`, `V/Display/Field/FieldHighlightLayers.cs:121-151`
- 내용: FieldView 40-41줄은 강조 색을 '다섯 묶음'이라고 적고 우선순위 사슬을 나열한다. 그러나 실제 Header 묶음은 아홉이다. 사슬에는 원본 Rows에 있는 낙석 경고·기믹 호버 범위·호버 이동 범위 둘·PullRange가 빠져 있다. fillAlpha 툴팁의 '범위 일곱'도 실제 채우는 범위 층(여덟 + 낙석)과 맞지 않는다. 원본이 따로 있는데 주석이 그 사본을 들고 있다.
- 고칠 방향(검토자 제안): 주석의 사슬·개수를 지우고 FieldHighlightLayers.Rows를 참조한다.
- 검증: 묶음 낮음; 찾은 검토자 L15

### C12-11 [낮음] 지형·선로·오브젝트 표시의 예외 접두사·안내 문구·요약이 실제 상황과 다름

- 위치: `V/Display/Map/CombatTerrainLook.cs:58-60`, `V/Display/Map/FieldRailwayPresenter.cs:59-60`, `V/Display/Map/FieldObjectPresenter.cs:94-95`
- 내용: (1) CombatTerrainLook 예외 접두사가 [FieldTerrainPresenter]라서 던진 곳을 잘못 가리킨다. (2) FieldRailwayPresenter.Install은 시작·정리 중이거나 이펙트 반환에 실패한 상태에서도 '해제된 표시입니다'라고 알린다. (3) FieldObjectPresenter.ValidateObjectRotations 요약은 'Terrain 회전은 아직 소비하지 않는다'고 하지만, 지금은 CombatTerrainLook·FieldTerrainPresenter가 Terrain 회전을 소비·검증한다.
- 고칠 방향(검토자 제안): 접두사를 실제 클래스명으로 바꾸고, 상태별 안내 문구와 요약을 고친다.
- 검증: 묶음 낮음; 찾은 검토자 S06

### C12-12 [낮음] 재생기 분리 뒤에도 예외 태그·프레임 순서 설명이 옛 Presenter 기준으로 남음

- 위치: `V/Playback/Players/CombatReactionPlayer.cs:44-49`, `V/Playback/Players/CombatStepPlayer.cs:88-104`, `V/Playback/Players/CombatPieceRunPlayer.cs:159`
- 내용: CombatReactionPlayer·CombatStepPlayer의 예외 문구는 '[CombatSceneEventPresenter]'로 시작하고, CombatPieceRunPlayer는 Presenter의 CleanupFailureMessage를 빌려 쓴다. Presenter.TickPresentation 설명은 순서를 '공격 → 조각 → 걸음·대기 → 반응'이라 적지만, 실제 순서는 외형 → 몸 글자 → 공격 → 조각 → 걸음 → 반응이다. CombatPresentationPlayers 설명에는 bodyText 자리가 없다.
- 고칠 방향(검토자 제안): 태그를 실제 클래스명으로 바꾸고, 순서 설명은 실행 목록을 참조하게 한다.
- 검증: 묶음 낮음; 찾은 검토자 S15

### C12-13 [낮음] 공격 전략으로 옮긴 예외 메시지에 옛 클래스 태그 [CombatAttackPlayback]가 남음

- 위치: `V/Presentation/Attack/CombatAttackStyle.cs:84-86`, `V/Presentation/Attack/CombatLinearStyle.cs:52-53`, `V/Presentation/Attack/CombatMortarStyle.cs:73-75`
- 내용: CombatAttackStyle.FireShot 기본 구현, CombatLinearStyle.FireShot, CombatMortarStyle(TryBeginCodeMotion·FireShot)의 예외가 실제로 던지는 클래스가 아닌 [CombatAttackPlayback] 태그를 단다.
- 고칠 방향(검토자 제안): 태그를 각 전략 클래스명으로 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S17

### C12-14 [낮음] 재생 조정·국면 전환·필드 창구 문서가 지금 코드와 다르거나 문장이 깨짐

- 위치: `V/Playback/CombatSceneEventPlayback.cs:15-18`, `V/Playback/CombatSceneEventPlayback.cs:42`, `V/Playback/CombatPhaseTransitionPresenter.cs:8`
- 내용: CombatSceneEventPlayback 요약은 '그 밖의 사건은 단계 전환은 전용 Presenter에, 그 밖의 사건은'으로 문장이 깨졌다. phaseTransition 인자 문서는 '전투 진입·밤 끝·승리'라 적지만 실제는 아침 하나뿐이고 그마저 꺼져 있다. CombatPhaseTransitionPresenter의 '대여한 Scope 반환'은 대여가 없는 지금과 다르다. CombatFieldEventPorts 문서에는 haboob·낙석·모래폭풍이 빠졌다. CombatSceneEventPresenter 생성자의 '모든 의존을 필수로'는 bodyText·feedbackEvidence가 선택 인자인 것과 다르다. trait 처리기 주석은 pull 인자 뒤 같은 줄에 붙어 있다.
- 고칠 방향(검토자 제안): 깨진 문장을 고치고, 인자·목록 문서를 지금 구성에 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S13

### C12-15 [낮음] CombatFactEventHandler 요약이 처리하지 않는 '구조물'을 반영 대상에 적음

- 위치: `V/Playback/Handlers/CombatFactEventHandler.cs:11-15`, `V/Playback/Handlers/CombatFactEventHandler.cs:38-87`, `V/Playback/Handlers/CombatStructureEventHandler.cs:35`
- 내용: 요약은 반영 대상에 구조물을 넣었지만 switch에는 구조물 갈래가 없다. 구조물은 CombatStructureEventHandler.ApplyFacts가 반영한다. CombatEventHandlerTable.Create 설명의 칸 목록에도 구조물 칸이 빠져 있다.
- 고칠 방향(검토자 제안): 요약에서 구조물을 빼고, 표 설명에 구조물 칸을 더한다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C12-16 [낮음] ApplyFacts 계약은 '표시 사실 반영'이지만 낙석·토네이도 처리기는 그 단계에서 소리·큐를 냄

- 위치: `V/Playback/Handlers/CombatEventHandler.cs:36-39`, `V/Playback/Handlers/CombatRockfallEventHandler.cs:47-65`, `V/Playback/Handlers/CombatTornadoEventHandler.cs:44-69`
- 내용: 기반 계약은 ApplyFacts를 '사건 스냅샷을 표시 사실·가시·오브젝트 표시에 반영'으로 정의한다. 틀은 소리·순간 큐를 그보다 앞 단계에서 낸다. 그런데 낙석 ApplyFacts는 터널 폭발 큐(PlayAccent)와 경고 소리를, 토네이도 ApplyFacts는 이동·경고 소리를 낸다. 문서화된 계약을 구현이 어기는 형태라, 주된 기준은 LSP(C2)에 가깝다.
- 고칠 방향(검토자 제안): 소리·큐를 앞 단계로 옮기거나, 계약 문서에 부수 효과 허용을 명시한다.
- 검증: 묶음 낮음; 찾은 검토자 S14

### C12-17 [낮음] 이펙트 부품 주석의 개수('넷'·'셋')와 사망 유지 숨김 설명이 코드와 다름

- 위치: `V/Presentation/Effects/CombatEffectFollower.cs:8-12`, `V/Presentation/Effects/CombatEffectPlacement.cs:7-11`, `V/Presentation/Effects/CombatEffectPlacement.cs:35-39`
- 내용: CombatEffectFollower는 '하는 일은 넷'이라 하지만 셋만 적고, 실제로 하는 숨김(FE11)이 빠졌다. CombatEffectPlacement는 '값은 셋'이라 하지만 속성은 넷(ActionBound)이다. CombatOwnedTraitEffectSource는 사망 유지 중 '받침이 그리기만 끈다'고 하지만, Collect가 IsDead 유닛을 바로 빼서 Sync에서 거둬진다.
- 고칠 방향(검토자 제안): 개수 대신 목록을 바르게 적고, 사망 유지 동작 설명을 고친다.
- 검증: 묶음 낮음; 찾은 검토자 S18

### C12-18 [낮음] 조각(Pieces) 문서가 Hit 내는 조각·ForAction 용도·새 채널 절차·흔들림 판정 위치를 틀리게 적음

- 위치: `V/Presentation/Pieces/CombatPieceStart.cs:29-36`, `V/Presentation/Pieces/CombatCodeMotionPiece.cs:29-33`, `V/Presentation/Pieces/CombatComposition.cs:43-44`
- 내용: (1) CombatPieceSignal.Hit 문서에 Hit을 내는 CombatCodeMotionPiece가 빠졌다. (2) Action·ForAction은 '포효·소환'용이라 적었지만 클립 공격·코드 모션 공격도 쓴다. (3) CombatPieceRunLeases의 새 채널 절차에 CombatPieceContext 꺼내기 줄이 빠졌다. (4) CombatCameraShakePiece는 '처리기가 CanShake를 본다'고 하지만, 실제로 보는 곳은 CombatCueComposer다.
- 고칠 방향(검토자 제안): 네 문서를 지금 사용처·절차에 맞춘다.
- 검증: 묶음 낮음; 찾은 검토자 S21

### C12-19 [낮음] 입력 계약(Input/Contracts) 문서가 지금 사용처와 다르거나 문장이 깨짐

- 위치: `V/Input/Contracts/ICombatInteractionFeedback.cs:42-45`, `V/Input/Interaction/AttackTargetingInteraction.cs:327`, `V/Input/Contracts/ICombatMovementFeedback.cs:9-13`
- 내용: ICombatInteractionFeedback.ShowRangeCells는 '이동·공격 범위'라고 적지만, 실제로는 공격 조준만 부른다. ICombatMovementFeedback 요약은 '이동 범위 표시…와 이동 범위 표시 계약을 상속합니다'로 문장이 겹쳐 깨졌다. ICombatPointerMapping 사용처에는 공격 Drag 제스처가 빠졌고, 이력 주석 때문에 Playback.Ordering을 using한다. ICombatPointerOcclusion은 'Router가 읽는다'고만 적지만, 호버·카메라 입력도 읽는다.
- 고칠 방향(검토자 제안): 사용처 목록과 깨진 문장을 고치고, 이력용 using을 지운다.
- 검증: 묶음 낮음; 찾은 검토자 S11

### C12-20 [낮음] 행동 버튼 계약은 복귀 슬롯을 '폐지·호출 안 함'이라 적지만 지금은 이동 되돌리기로 호출됨

- 위치: `V/Input/ActionButtons/ICombatActionDisplay.cs:7-21`, `V/Input/ActionButtons/CombatActionButtons.cs:9-16`, `V/Display/Hud/CombatActionPanel.cs:50-51`
- 내용: ICombatActionDisplay는 '폐지된 복귀 슬롯은 요청을 전달하지 않는다', '세 번째 인자는 호환용이며 호출하지 않는다'라고 적고, 계약을 '(공격·턴 종료)'로 설명한다. 그러나 CombatActionPanel은 returnButton을 ReturnSlot으로 받아 세 번째 대리자를 부르고, 이 대리자는 ReturnPressed → RequestReturn → Router Return → CombatMovementUndoInput.Request로 이어진다. 인자 이름 returnToCheckpoint도 옛 이름이다.
- 고칠 방향(검토자 제안): 계약 문서를 공격·턴 종료·이동 되돌리기로 고치고, 인자 이름을 바꾼다.
- 검증: 묶음 낮음; 찾은 검토자 S10

### C12-21 [낮음] 입력 소유자·선택 취소 설명이 실제 어댑터·루프 동작보다 좁게 적힘

- 위치: `V/Input/Interaction/CombatInputOwner.cs:17`, `V/Input/Interaction/CombatAttackDragHandover.cs:118`, `V/Input/Interaction/CombatInteraction.cs:247-254`
- 내용: CombatInputOwner는 공격 Drag 넘겨받기가 '정리 단계에서만' 몸짓을 무효화한다고 하지만, 어댑터는 Poll(매 Tick)에서도 InvalidateStaleCandidate를 돈다. CombatInteraction.SelectCanceled는 '경로 취소·배치/아이템 Drag 끝'만 적지만, 실제로는 넘겨받기 무효화와 유닛 호버 Clear도 한다.
- 고칠 방향(검토자 제안): 두 설명에 실제로 하는 단계를 더한다.
- 검증: 묶음 낮음; 찾은 검토자 S12

### C12-22 [낮음] 이동 명령·미리보기의 Cell 설명('이동 목표 칸')이 경로만 넣는 지금 생성 방식과 다름

- 위치: `S/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs:19-20`, `S/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs:87-95`, `S/RuntimeSystem/BufferSystems/CombatView/CombatViewCommandKind.cs:15-16`
- 내용: CombatViewCommandData.Cell은 '이동의 목표 칸', Kind.Move는 '개체 ID와 목표 칸을 받는다'고 적는다. 그러나 Move 생성은 MovementPath와 문맥을 받고 Cell은 default로 둔다. 미리보기 Cell 주석도 같은 식으로 어긋난다.
- 고칠 방향(검토자 제안): Cell은 배치용이고 이동은 경로를 쓴다고 고친다.
- 검증: 묶음 낮음; 찾은 검토자 S23

### C12-23 [낮음] 재사용한 값 검사 함수의 오류 문구가 실제 검사 값과 맞지 않고 '값 조건은 여기 한 곳' 요약이 틀림

- 위치: `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatBoardRevealSettings.cs:40-41`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:9-12`, `S/DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs:271`
- 내용: 보드 등장 depth·overshoot가 틀리면 '소환 등장 깊이는…'이라는 문구가 나온다. bodyTextStackGap(거리)이 틀리면 '초 단위 값' 문구가 나온다. 클래스 요약은 '값 조건도 여기 한 곳'이라 하지만, 하위 설정(Nexus·Railway·BoardReveal)은 CombatPresentationValueRules를 직접 부르고, ValidateSoundPreset도 '원본은 ValueRules'라고 적는다.
- 고칠 방향(검토자 제안): 검사 함수가 칸 이름·단위를 받아 문구를 만들게 하고, 요약은 ValueRules를 원본으로 적는다.
- 검증: 묶음 낮음; 찾은 검토자 S26

### C12-24 [낮음] 큐 규칙·큐 자리·투사체 소리 주석이 지금 규칙표와 어긋남

- 위치: `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:5-8`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:58-66`, `S/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs:121-127`
- 내용: CombatCueEvent 요약은 '5 이후는 이펙트·흔들림만 받는다'고 하지만, StructureHit(15)은 흔들림을 받지 않고 이펙트 지연도 0만 받는다. remarks 표에는 StructureHit 규칙이 아예 없다. CombatCuePoint.EventCell·SubjectBody 설명에는 TraitActivation·낙석·StructureHit가 빠졌다. CombatProjectileSO는 효과음 슬롯이 '연결 대기'라 하지만, 착탄 소리는 이미 BulletFall에 연결돼 있고 비행 슬롯만 대기 상태다.
- 고칠 방향(검토자 제안): 요약의 규칙 사본을 지우고 규칙표를 참조하게 한다. 빠진 종류와 소리 연결 상태를 고친다.
- 검증: 묶음 낮음; 찾은 검토자 S25

## 검증에서 버린 후보 6개

- **C1-10** (C1 낮음) BattleChangerUI·PackUI가 넘겨받은 입력 사실로 보임 규칙을 따로 계산 — 보임 규칙을 다시 조합하는 코드는 GlobalSystem의 BattleChangerUI.cs:52-54와 PackUI.cs:89-91에 있다. 범위 안인 CombatInputSurfaces.cs:59-61은 blocked와 facts를 넘기기만 한다. Refresh(in CombatInputFacts, bool) 서명은 GlobalSystem이 정했고 해당 UI 동작은 §4의 기반 한계(PackUI·BattleChangerUI 자체 동작)에 들어간다. 범위 밖 코드이고 기반 한계라 감점하지 않는다.
- **C1-18** (C1 낮음) 유닛 표시 미리 만들기가 두 셈 규칙과 풀 수 사본 셋으로 나뉨 — CombatSceneDisplay.Prewarm(CombatSceneDisplay.cs:324-342)은 Buffer의 살아 있는 전투 캐릭터 전부(적 포함)를, CombatUnitPreparationReader(34-49)는 로딩 화면 뒤 준비할 출전 덱을 센다. 대상이 다른 두 결정이고, FieldUnitPresenter.Prewarm(163-181)은 _createdCounts까지만 만들어 겹쳐도 중복 생성이 없다. _rentedCounts·_preparedCounts는 프로파일러 마커 분류(813-817)에만 쓰이고 Disc
- **C10-04** (C10 중간) 근접 클립 공격 조립이 Cue2·Cue3 기준 시점을 잇지 않아, DB 검사를 통과한 데이터로 공격을 시작하면 예외로 멈춤 — 지적한 실패 시나리오는 데이터를 만드는 단계에서 이미 막힌다. 근접 공격 표시 값은 CombatAttackPresentationData.cs:55에서 CombatPresentationCueRules.Require(cues, AttackEventOf(kind))를 거친다. 이때 AllowsAnchor(CombatPresentationCueRules.cs:84)는 MeleeAttack 칸에 Start·Cue·Hit·End만 허용하므로, 이펙트·흔들림·소리 시점이 Cue2·Cue3를 쓰는 근접 큐는 129·145·160줄에서 거절된다. 
- **C10-05** (C10 중간) 특성 피드백 계획이 Core 사건과 맞지 않아도 경고 한 번만 남기고 피드백을 조용히 뺌 — 반론 검증자의 근거가 맞다. 코드는 지적대로 불일치를 issues로만 모으고(CombatTraitFeedbackPlan.cs:100-108, 117-118, 136-140, 147-148) CombatTraitFeedbackPlayback.cs:48-53에서 같은 문자열마다 _warned(:28)로 경고를 한 번 남긴다. 하지만 이것은 설계 2026-10-31-trait-body-feedback-design.md:106·122에 적힌 '상태 연계 출력만 생략하고 경고 한 번, 조립 도구에서는 재생 전 작성 오류'를 그대로 구현한 것
- **C12-25** (C12 낮음) 선로·Nexus 낙하 enabled가 보드 등장 켜짐·생성 타일 유무에 묶인 사실이 설정 쪽에 없음 — CombatBoardEntryPresentation.cs:64-75에서 진입 연출 전체는 _settings.Enabled && _tiles.Count > 0일 때만 시작한다. 그래서 railwayDrop·nexusDrop의 enabled가 그 조건에 묶이는 것은 사실이다. 그러나 CombatNexusDropSettings.cs:11·CombatRailwayDropSettings.cs:11의 enabled 칸에는 틀린 설명이 없고 툴팁 자체가 없을 뿐이다. 이 의존은 표시 쪽 주석(64, CombatBackdropRevealTarge
- **C5-24** (C5 낮음) __remove__ — 문제 파일의 제목과 내용이 '__remove__'이고 증거와 구성원이 비어 있다. 병합할 때 지우기로 표시한 자리표시 항목이라 검증할 주장이 없다.
