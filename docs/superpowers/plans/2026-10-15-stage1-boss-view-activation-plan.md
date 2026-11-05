# 스테이지 1 보스 View 활성화 Task 계획 (2026-10-15)

- 설계: [보스 활성화 설계](../specs/2026-10-15-stage1-boss-view-activation-design.md)(결정 B1~B4, D1~D8). 실행 상태는 이 문서 §6에만 적는다.
- 상위 계획: 보스 스테이지 계획 (Git history: `2026-10-14-boss-stage-objective-plan.md`) Phase 7(T9)을 View 쪽에서 이 계획으로 나눠 실행한다. 끝나면 그 계획과 [보스 View 인계](../handoffs/2026-10-13-boss-summoning-view-handoff.md)에 결과를 적는다.
- 기준: `feature/Yonggeun/Stage` HEAD `88bf9bf8`. 커밋·push는 사용자.
- 검증: MSBuild(오류 0·새 경고 0) + 관리 실행 가능한 시험은 Unity 내장 Mono로 + 사용자 Unity EditMode·재생. 각 근거를 구분해 적는다.

## 1. Phase 1 — 에셋 등록과 보스 스테이지 (D1·D2)

**파일:** `DataBaseSO/InGameGroup/Unit/UnitDataTable.asset`, 지정 방식 표 에셋, `ViewGroup/UnitVisual/UnitVisualDataTable.asset`, `UnitVisual_StageBoss1.asset`, 새 `TraitVisual_Necromancy`·`TraitVisual_Roar`(+ .meta)와 TraitVisual 표, `InGameGroup/Stage/Combat/Combat_Boss.asset`, 시험 `BossUnitAuthoringTests.cs`·`DataBaseAssetTests.cs`(필요한 곳).

1. 시작 전 기준선: 관련 에셋 GUID·현재 값 기록, 실제 카탈로그 생성 성공 확인.
2. 보스·지정 방식·외형 등록, 보스 외형에 Melee·Default 반응 연결, TraitVisual 2개 추가.
3. `Combat_Boss`에 D2 값 적용(본부·EnemyLoadout 참조 제거, 보스 배치 1개·승리 대상).
4. 시험: `PreparedBossAsset_ExportsConfirmedConfiguration`의 "미등록" 임시 단정을 등록 상태로 바꾸고, 운영 에셋 인수 시험(실제 카탈로그 → 보스 키 18·지정 방식 5, 보스 스테이지 지정 목표·대상 1개·본부 없음·EnemyLoadout 없음, 준비 결과에 목표 ID 연결)을 더한다.
5. 실제 에셋을 읽는 시험 목록을 다시 확인해 새 유닛·외형으로 바뀌는 기대를 갱신한다.

**완료:** MSBuild 오류 0, 실제 카탈로그 생성 성공(Unity 확인), 위 시험 추가. 본부 없는 준비가 막히면 멈추고 묻는다.

## 2. Phase 2 — 목표 마커·포효 카운터·승리 문구 (D3·D4·D5)

**파일:** `Display/FieldHighlightLayers.cs`·`FieldHighlightPalette.cs`·`FieldView.cs`(새 층·색), `CombatSceneDisplay.cs`(Refresh에서 목표 층 갱신 — 기존 `RenderActiveActor`와 같은 자리), `Display/FieldUnitPresenter.cs`(상태 설명·목표 항목), `Display/CombatHud.cs`(문구), 시험.

1. `FieldHighlightLayer.ObjectiveTarget` 추가, 우선순위·색·두 표시 모드(CellPrefab·GridOutline) 반영.
2. Refresh에서 `Objective`를 읽어 대상 점유 칸을 층에 넣고 일반 전투·대상 없음·대기 영역이면 비운다.
3. 유닛 HUD 상태: Roar 스냅샷 설명 `"포효 {HitCount}/{HitThreshold}"`, 목표 대상에 "격파 목표" 항목.
4. 완료 문구에 지정 보스 승리 사유.
5. 시험: 강조 층 우선순위·비우기(관리 실행), HUD 상태 설명 생성(표시값 계산 부분), 완료 문구.

**완료:** MSBuild 오류 0, 관리 실행 시험 통과, Unity 시험 목록 기록.

## 3. Phase 3 — 포효·소환 최소 연출과 간격 (D6·D7·D8)

**파일:** `CombatSceneEventPresenter.cs`, `CombatAttackPlayback.cs`(공격 결과 입력 입구), `CombatEnemyPresentationPacing.cs`, 시험(`CombatSceneEventPresenterTests`·`CombatEnemyPresentationTests`·`CombatEnemyPresentationPacingTests`).

1. 공격 재생에 `AttackResult` 입력 입구 추가(기존 `CombatAttackEvent` 입구는 이것을 부르게 해 한 곳으로).
2. `Roared`: 결과가 있으면 공격 재생 + 영향 칸 잠깐 강조, 없으면 반응 동작만. 완료 콜백 한 번, 취소·해제 규칙 유지.
3. `Summoned`: 생성 사실 반영 뒤 소환 칸 잠깐 강조, 적 연출 간격에 포함.
4. 넉백 3칸 연속 `Moved` 회귀 시험.

**완료:** MSBuild 오류 0, 기존 재생 시험 회귀 0(관리 실행 가능 범위), 새 시험 추가, Unity 시험 목록 기록.

## 3b. Phase 3b — 소환 등장·포효 커지기 동작 (D6b, 사용자 요청 2026-10-15)

**파일:** 새 `Display/ICombatUnitMotionPose.cs`·`CombatAbilityMotionSettings.cs`·`CombatAbilityMotionPlayback.cs`(+ .meta), `Display/FieldUnitView.cs`·`FieldUnitPresenter.cs`(크기 자세), `CombatSceneEventPresenter.cs`, `CombatEnemyPresentationPacing.cs`(소환 머묾 제거), `CombatSceneRoot.cs`(설정·검사), `Factory/…/CombatSceneDisplayFactory.cs`, 시험.

1. 크기 자세: `PresentationUnitPose`가 시작 때 크기를 기억하고 `ApplyScale(배율)`·`RestorePose`로 되돌린다.
2. 설정: `CombatSceneRoot`에 소환 대기·깊이·시간·바퀴, 포효 커지기 시간·배율. 검사는 `CombatAbilityMotionSettings` 생성자 한 곳(씬 검사도 이것을 부름).
3. 재생: `CombatAbilityMotionPlayback.BeginRise/BeginPulse`, Tick, 취소(자세 복원·완료 버림), 해제, 유닛 표시가 사라지면 다음 Tick에 오류.
4. Presenter: 소환 = 강조 → 사실 반영 → 등장 동작 → 강조 끔 → 완료. 포효 = 강조 → 커지기 → (결과 있으면) Melee → 완료. 다음 사건·최종 표시는 동작이 끝난 뒤.
5. 시험: 설정 검사(관리 실행), 동작 순서·자세 복원·취소(Unity 전용 `CombatStandardAttackTests`), 기존 Phase 3 시험의 소환·빈 포효 기대값 갱신.

**완료:** MSBuild 오류 0, 관리 실행 회귀 0, Unity 시험 목록 기록.

## 4. Phase 4 — 실제 보스 전투 확인과 인계 갱신

1. 사용자 재생: StageSelect에서 보스 노드 진입 → 목표 마커 → 보스 피격 카운터 증가 → 5회째 포효 연출과 넉백 → 소환(최대 2) → 낮/밤 전환 → 보스 처치 승리 문구 → 복귀.
2. 상위 계획 T9 체크리스트, 보스 View 인계 §8, Core 데이터 경계 인계에 결과 기록. CLAUDE.md 상태 줄 갱신.

## 5. 범위 밖(설계 §2)

전투 로그 포효 줄(현강님 UI), 아이콘 위 상시 숫자(현강님 `UnitHUD`), 새 아트·VFX, 상단 보스 바, Core 변경.

## 6. 실행 기록

### 6.1 설계·계획 작성 (2026-10-15)

- 사용자 결정 B1~B4를 받아 설계와 이 계획을 썼다. 코드·에셋 변경 없음. Phase 1 착수는 사용자 지시 대기.

### 6.2 Phase 1 — 에셋 등록·보스 스테이지 (2026-10-15, 위치 값 대기)

- **사용자 진행분(D1 일부·D2):** `UnitDataTable`에 `Unit_Stage1Boss`, 지정 방식 표에 `TargetingMethod_Stage1BossRoar`(key 5), `UnitVisualDataTable`에 새 `UnitVisual_StageBoss1`(key 18, 프리팹 `Prefabs/Stage/Units/1StageBoss.prefab`, 공격 `CombatAttack_Melee`, 반응 `CombatUnitReaction_Default`) 등록. `Combat_Boss`: 목표 2(DesignatedBossDefeated), 본부·EnemyLoadout 참조 제거, 보스 배치 1개·승리 대상.
- **Claude 진행분:** `TraitVisual_Necromancy`(80)·`TraitVisual_Roar`(90) 새 에셋(.meta 포함, 아이콘 비움)과 TraitVisual 표 등록. 시험:
  - `BossUnitAuthoringTests.PreparedBossAsset_ExportsConfirmedConfiguration`: "미등록" 단정을 등록 상태로 교체, 보스 외형(프리팹·공격·반응)과 보스 특성 3개의 TraitVisual 존재 확인 추가.
  - `BossStageAuthoringTests.StageObjective_LegacyAssetAndConstructorsKeepDefaults`: 기본값 확인 대상을 `Combat_Normal_A`로 변경(보스 스테이지가 지정 목표가 됐으므로).
  - 새 `BossStageAuthoringTests.OperationalBossStage_HasSingleDesignatedBossInsideEnemyZone`: 지정 목표, 본부·EnemyLoadout 없음, 승리 대상 보스 하나, **적 배치 구역 안 위치**.
  - `CombatUnitReactionAssetTests`: 반응 공유 대상에 보스 추가(보스 프리팹 Missing Script 검사 포함).
- **MSBuild:** 오류 0. 위 시험은 AssetDatabase를 써서 Unity 전용이다.
- **발견:** `Combat_Boss`의 보스 배치가 `(0, 0)`·R0이다. `(0, 0)`은 플레이어 배치 구역이라 Core 준비(`BattlePreparationCalculator.Place`)가 "초기 배치가 소유자의 배치 구역을 벗어났습니다"로 거절한다. 새 인수 시험도 이 값에서 실패한다. 설계 B1 값은 `(4, 8)`·R180. 사용자 확인 대기.

### 6.3 Phase 1 보완 — 보스 위치·Play From Boot (2026-10-15)

- **보스 위치:** `Combat_Boss` 보스 배치는 `(4, 8)`·R0(사용자 수정 확인). 회전은 R180이 아니라 R0이 맞다: Core 생성 적은 R0(`EnemySpawnCalculator`)이고 View가 적 모델에 180도를 더한다(`FieldUnitPresenter.ModelRotation`). 설계 B1을 정정했다.
- **설정 창 회귀 수정:** 시작 구간 번호 변경(Build Settings 순서) 때 `PlayFromBootSettingsWindow`에 옛 숫자(0 = Combat, 2 = Lobby)가 남아 Lobby 선택에서 Combat 전용 칸이 켜지고 Combat에서 꺼졌다. 상수 `LobbyStart`·`CombatStart`로 교체.
- **개발 진입 보스 노드(사용자 승인 "3번 그렇게 진행해") → 철회:** 보스 노드로 진입을 요청했더니 Core가 `NavigationRejected, NotConnected`로 거절했다(현재 노드와 이어진 노드로만 진입 가능). 아래 §6.4로 대체.
- **MSBuild:** Systems·Tests.Framework·Assembly-CSharp-Editor 오류 0.

### 6.4 개발 진입 보스 스테이지 덮어쓰기 (2026-10-15, 사용자 결정 "개발용 스테이지 덮어쓰기")

- **방식:** Dev Node Id(시작 노드와 이어진 노드)로 평소처럼 진입하되, 전투 정의만 현재 맵의 보스 전투 스테이지에서 읽는다. Core 변경 없음.
- **코드:** 새 `ViewSystem/Dev/DevBossStageBattleSource.cs`(+ .meta, 에디터·개발 빌드 전용). 운영 DB 소스 `DatabaseBattleInitializationSource`를 감싸 재사용하고, 보스 스테이지는 Buffer 현재 맵 노드 중 `StageType.Combat`·`MapRole.Boss` 하나로 고른다(키·이름 분기 없음). 프레임워크가 외부 소스를 모듈로 묶지 않으므로 `GameBootstrap`이 `PostBindService` 뒤 `Bind`로 한 번 연결한다(`RuntimeServices` 어댑터).
- `GameBootstrap`: §6.3의 `DevNodeSelection`·`ResolveDevNodeId`를 지우고 `DevBattleStage`(Node/Boss)로 바꿨다. Database + Boss일 때만 보스 소스를 쓰고, Fixture + Boss는 경고 후 Fixture 전투. EditorPrefs 키는 `DevBattleStage`, 설정 창 "Dev Battle Stage" 선택과 안내, 테스트 설정 목록 갱신.
- **한계:** 맵 진행은 진입한 노드로 처리되므로 보스 승리 뒤의 챕터 완료·Run 종료 흐름은 이 경로로 확인하지 못한다. 배경 프랍은 진입 노드 스테이지 것이다(View가 현재 선택의 StageKey로 고름).
- **MSBuild:** Systems·Tests.Framework·Assembly-CSharp-Editor 오류 0(새 파일은 targets로 포함). Unity 재생 확인 대기.
- **재적용(2026-10-15):** 재생에서 보스가 나오지 않아 확인하니 `GameBootstrap.cs`만 커밋 상태(`9befd3d9`)로 되돌아가 있었다(Claude가 되돌린 것 아님, 에디터·IDE의 변경 버리기로 추정). 같은 변경을 다시 넣고 MSBuild 오류 0 확인.
- **사용자 재생 확인(2026-10-15):** Database + 보스 스테이지 개발 진입에서 보스가 나오는 것을 확인("잘 나오는거 확인했고"). Phase 1 실 에셋 시험(`BossUnitAuthoringTests`·`BossStageAuthoringTests`·`CombatUnitReactionAssetTests`·`DataBaseAssetTests`) 결과는 따로 받지 않았다.
- **Phase 1 판정:** 에셋 등록·보스 스테이지·개발 진입은 완료. 실 에셋 시험 결과는 다음 Unity 실행 때 함께 받는다.

### 6.5 Phase 2 — 목표 마커·포효 카운터·승리 문구 (2026-10-15, 코드 완료·Unity 확인 대기)

- **D3 목표 층:** `FieldHighlightLayer.ObjectiveTarget = 1 << 6`, 색 종류 `FieldHighlightColor.ObjectiveTarget = 10`. `DrawOrder` 맨 앞(가장 낮음)에 넣어 우선순위가 호버 > … > 행위자 > 목표 > 정적이 된다. 셀 색 표시기는 `Resolve`만 따르므로 코드 변경 없음. 색은 `FieldView.objectiveTargetColor`(기본 분홍 (1, 0.25, 0.75)) → `FieldHighlightPalette.ObjectiveTarget` 한 곳.
- **GridOutline 표시 규칙:** 목표는 현재 행위자와 같은 상태 층이라 평소에도 테두리를 그린다(`FieldOutlineRenderer.IdleLayers = ActiveActor | ObjectiveTarget`). `IsActing`의 행동 층에는 넣지 않아 목표만으로 격자를 켜지 않는다. 채우기(`FillKind`)는 행위자처럼 쓰지 않는다. 설계 D3의 "행동 중 전체 강조 규칙도 기존 층과 같다"를 이 뜻으로 분명히 고쳐 적었다.
- **갱신 자리:** `CombatSceneDisplay.Refresh`가 `CombatObjectiveDisplay.TargetOf(CommandState.Objective)`로 대상을 구한다(지정 보스 목표만 대상, 본부 목표·null은 Empty). `RenderActiveActor`를 `RenderOccupiedLayer(layer, target, characters)`로 일반화해 행위자·목표가 같은 규칙(필드에 있고 살아 있을 때만 점유 칸, 아니면 층 지움)을 쓴다. 행위자 경로에 사망 검사가 더해졌지만 Core가 사망 캐릭터를 필드에 두지 않아 동작 변화 없음.
- **HUD "격파 목표":** `FieldUnitPresenter.SetObjectiveTarget(EntityId)`(Refresh가 유닛 그리기 전에 부름, `Release`에서 비움). 대상 유닛의 상태 칸 첫 항목으로 키 `BattleObjective`·아이콘 없음(HUD 기본 아이콘)·설명 "격파 목표".
- **D4 포효 카운터:** 상태 칸 설명을 `CombatObjectiveDisplay.DescribeTrait(status, displayName)`으로 만든다. `RoarTraitStatusSnapshot`이면 `"{표시 이름} {HitCount}/{HitThreshold}"`(TraitVisual_Roar 표시 이름 "포효" → "포효 3/5"), 다른 특성은 지금처럼 빈 설명. 표시 이름은 TraitVisual이 없으면 `TraitKey` 이름.
- **D5:** `CombatHud.ShowCompletion` — 승리이고 사유가 `DesignatedBossDefeated`이면 "보스 격파 · 결과 확인", 다른 승리는 기존 "승리 · 결과 확인".
- **새 파일:** `ViewSystem/Combat/Display/CombatObjectiveDisplay.cs`(+ .meta, 순수 정적 계산), `Tests/EditMode/Framework/CombatObjectiveDisplayTests.cs`(+ .meta). Core 변경 없음, 새 서비스·ServiceKey 없음.
- **시험 변경:** `CombatObjectiveDisplayTests` 5개(대상 고르기, 포효 설명, 상태 키 충돌 없음, 우선순위·DrawOrder, 팔레트 색). `FieldHighlightStateTests.IsActing_…`에 목표 층(행동 아님) 추가. `FieldOutlineRendererTests` 팔레트 인자 추가, 핸들 수 8→9·16→18, 새 `ObjectiveTarget_StaysVisibleWhileIdle_BelowActiveActor_WithoutGrid`. `CombatDisplayReuseTests`에 `HudCompletion_Victory_ShowsBossTextOnlyForDesignatedBoss`(2 케이스).
- **MSBuild:** Systems·Tests.Framework 오류 0, 새 경고 0(남은 2건은 패치 사본 경로·병합된 기존 시험 파일).
- **관리 실행:** `CombatObjectiveDisplayTests` 5/5, `FieldHighlightStateTests` 8/8, `CombatDisplayReuseTests` 관리 실행 가능 7/7(나머지 20은 Unity 객체 필요), `WallAttackInteractionTests` 8 통과(1 Unity 전용). `FieldOutlineRendererTests`는 전부 Unity 객체가 필요해 Unity 결과로 받는다. 전투 View 39개 묶음 전체 회귀(스크래치 `boss2/summary.txt`)는 SV 기준선(`final`)과 37개가 같고, 다른 2개는 시험 추가분이다(`CombatInteractionTests` 49→52 통과 = 우클릭 회수 시험, `CombatDisplayReuseTests` Unity 전용 19→20 = 새 승리 문구 시험). 실패는 기준선과 같은 `CombatViewSystemTests` 3개(씬 루트 패치 사본에서만 실패하는 Unity 전용 항목)뿐이다.
- **발견(사용자 질문 "D4를 어떻게 확인해야 하지" 확인 중):** `Prefabs/UI/CombatOnly/HUD/HUD.prefab`의 `UnitHUD.descriptionPanel`·`descriptionText`가 `{fileID: 0}`이다. `CanDescribe`가 항상 false라 "포효 n/5"·"격파 목표" 설명이 재생에서 보이지 않고 상태 아이콘도 반응하지 않는다. 문자열 생성은 `CombatObjectiveDisplayTests`로만 확인된다. 프리팹은 현강님 UI 영역이라 현강님께 연결을 부탁한다(사용자 지시, 공유 메모 [`handoffs/2026-10-15-unit-hud-description-panel-hyeongang-share.md`](../handoffs/2026-10-15-unit-hud-description-panel-hyeongang-share.md), 미전달). 카운터 규칙(Core `RoarHitCounter`): 밤의 피격만 센다(0 피해 포함, 회복 제외, 같은 원인은 대상당 1회). 5번째 피격이 든 명령 직후 포효하며 0으로 되돌리므로 화면에는 4/5 다음 0/5가 보이고 5/5는 보이지 않는다.
- **Unity 확인 목록:** EditMode `CombatObjectiveDisplayTests`·`FieldHighlightStateTests`·`FieldOutlineRendererTests`·`CombatDisplayReuseTests`(특히 `HudCompletion_*`)·`WallAttackInteractionTests`, Phase 1 실 에셋 시험 4종. 재생: 보스 칸 분홍 테두리/색(행위자 차례에는 노랑이 위), 보스 HUD 상태 칸 "격파 목표"·"포효 n/5"(마우스 올림), 보스 처치 시 "보스 격파 · 결과 확인". 개발 진입 경로는 진입 노드로 진행이 처리돼 승리 뒤 흐름은 Phase 4 운영 경로에서 본다.

### 6.6 Phase 3 — 포효·소환 최소 연출과 간격 (2026-10-15, 코드 완료·Unity 확인 대기)

- **사용자 결정(구현 전 질문):** (1) 칸 강조는 `Affected` 대신 **연출 전용 층 추가**(입력 조준 호버가 `Affected`를 쓰고 연출 중에도 미리보기가 허용돼 서로 지움). (2) 대상 없는 포효는 **동작 없이 간격만**(반응 동작은 피해 사건이 입력이라 가짜 사건이 필요). 설계 D6·D7 고침, 새 D6a.
- **재생 강조 층:** `FieldHighlightLayer.PresentationHighlight = 1 << 7`(마지막 남은 비트). `DrawOrder`에서 예정 경로 위·입력 영향 칸 아래, 색은 `ColorOf` → `Affected`(새 색 설정 없음). GridOutline에서는 `IdleLayers`에 넣어 평소에도 그리고 `IsActing`(격자)에는 넣지 않았다. 쓰는 쪽은 `CombatSceneEventPresenter` 하나 — 생성자 새 선택 인자 `presentHighlight`(빈 목록 = 지우기), `CombatSceneDisplayFactory`가 `field.SetLayerCells(PresentationHighlight, cells)`로 연결.
- **D6 포효:** `PresentEvent`가 `CombatRoarEvent.Attack`이 있으면 `CombatAttackPlayback.TryBegin(in AttackResult …)`(새 입구)로 공격 연출을 한 번 재생하고 영향 칸을 강조한다. 효과음은 빈 콜백(Audio 제외). 대상이 없거나 연출을 시작하지 못하면(외형 없음·Ballista) 보스 칸을 강조하고 적 행동 간격만큼 머문 뒤 완료한다.
- **공격 재생 입구:** `TryBegin(CombatAttackEvent)`는 Ballista만 사건으로 시작하고 나머지는 `TryBeginResult(AttackResult …)`를 부른다(Linear·Melee·Mortar가 한 곳). Mortar도 결과를 받도록 바꿨다. 새 `TryBegin(in AttackResult …)`는 Ballista면 false(관통 후속 사건 대응이 공격 사건 객체에 묶여 있음).
- **D7 소환:** 생성 사실을 보이는 기존 처리 + 생성 칸 강조 + 적 행동 간격만큼 머묾. 간격(`CombatEnemyPresentationPacing.BeforeEvent`)에서 포효·소환은 보스(소환자)의 행동 하나로 센다. 머무는 시간은 새 `HoldAfter(e)`(포효·소환 = `enemyActionGapSeconds`, 그 밖 0)가 정하고, Presenter는 기존 대기 장치(`_waitingEvent`, "보인 뒤 대기")를 그대로 쓴다.
- **강조 지우는 때:** 다음 행동(공격·포효·소환·턴 사건, 자발적 이동 `BeginStep`) 시작, `Refresh`(결과 최종 사실), `CancelMovement`(취소·해제·재연결). 피해·사망·넉백 동안은 유지.
- **D8 넉백:** 확인 중 문제를 찾아 고쳤다. 자발적 경로의 끝 경계가 공격 사건뿐이라, 같은 유닛이 경로 뒤 포효로 넉백되면 넉백 칸을 앞 경로의 연속으로 보았다. `OrderedCombatPresentation.IsLastMovementStep`과 간격의 `_attackStarted`에 포효를 공격과 같은 경계로 더했다. `CombatAttackPlayback.CanBegin`도 Ballista 화살 비행 중 포효 시작을 공격처럼 막는다(막지 않으면 공격 애니메이션이 겹쳐 시작된다).
- **시험:**
  - 관리 실행: `CombatEnemyPresentationPacingTests` 새 3개(포효 경계 뒤 넉백 비자발, 포효·소환 행동 간격, `HoldAfter`), `CombatDisplayReuseTests` 새 1개(재생 강조 색·우선순위), `FieldHighlightStateTests.IsActing_…`에 재생 강조(행동 아님).
  - Unity 전용: `CombatStandardAttackTests` 새 4개(포효 Melee 재생·강조·무음, 대상 없는 포효 머묾, 소환 표시·강조·다음 턴에 지움, 머무는 중 중단 시 강조 지움), `CombatEnemyPresentationTests.OrderedEnd_StopsAtRoarBoundary_AndThreeKnockbackStepsEndOnce`, `FieldOutlineRendererTests` 핸들 수 9→10·18→20과 새 `PresentationHighlight_…`.
  - 넉백 경계 시험은 같은 내용을 GameObject 없는 스크래치 사본(`boss3x`)으로 관리 실행했다: 수정 코드 통과, 경계 한 줄을 되돌린 변이 사본은 실패(문제가 실제였음 확인).
- **MSBuild:** 오류 0, 새 경고 0.
- **관리 실행 전체 회귀(39개 묶음, 스크래치 `boss3/summary.txt`):** Phase 2(`boss2`)와 37개 같음. 다른 2개는 Unity 전용 새 시험 수(`CombatStandardAttackTests` 12→16, `CombatEnemyPresentationTests` 28→29). 실패는 기준선과 같은 `CombatViewSystemTests` 3개뿐.
- **Core 변경 없음, 새 파일 없음**(모두 기존 파일 수정).
- **Unity 확인 목록:** EditMode `CombatStandardAttackTests`·`CombatEnemyPresentationTests`(GUID 수리 뒤 처음 도는 묶음이라 기존 실패가 섞일 수 있음)·`CombatEnemyPresentationPacingTests`·`FieldOutlineRendererTests`·`CombatDisplayReuseTests`·`FieldHighlightStateTests`. 재생: 밤에 보스 5회 피격 → 보스 박치기(Melee) 한 번 + 영향 칸 주황 강조 → 피해·넉백(3칸이면 칸별 이동) → 다음 행동에서 강조 사라짐. 보스 턴 소환 → 소환 유닛 등장 칸 강조 → 잠깐 머문 뒤 턴 종료.

### 6.7 Phase 4 — 실제 보스 전투 확인과 인계 갱신 (2026-10-15, 문서 갱신 완료·사용자 재생 대기)

- **운영 경로 사전 확인:** StageSelect 노드 진입 → 전투 연결은 들어와 있다(`StageLifeCycle.TryRequestCombat`, 호진님 작업). 보스 노드는 맵 1의 Boss 역할 노드(Stage_Boss, stageKey 2101 → `Combat_Boss`)다. Core는 이어진 노드로만 진입을 허락하므로 앞 노드를 깨고 올라가야 한다. 개발 진입(§6.4)은 보스 정의만 바꾸므로 승리 뒤 챕터 완료·Run 종료는 이 운영 경로에서만 확인된다.
- **인계 갱신(현재 상태, 재생 결과 칸은 비움):** 상위 계획 T9에 View 실행 상태(체크 상자는 운영 확인 전이라 그대로 둠), [보스 View 인계 §8.1](../handoffs/2026-10-13-boss-summoning-view-handoff.md), [Core 데이터 경계 인계 §14.4](../handoffs/2026-09-24-view-core-data-boundary-handoff.md). §14.2의 R180 제안을 R0으로 정한 사실도 적었다.
- **사용자 재생 확인 목록(결과 대기):**
  1. 진입: StageSelect에서 보스 노드 → Combat. 낮(배치)에는 보스가 대기 영역이라 필드에 없고 목표 강조도 없다.
  2. 밤 진입: 보스가 (4, 8)에 나타나 플레이어 쪽을 본다. 보스 칸에 목표 강조(분홍)가 있고, 보스 차례에는 노랑이 위에 보인다.
  3. 소환(§6.8로 바뀜): 보스 턴에 보스 주변 칸이 주황으로 강조되고, 잠깐 뒤 소환 유닛이 바닥 아래에서 돌며 올라온다. 다 올라오면 강조가 꺼진다. 살아 있는 소환은 최대 2.
  4. 포효(§6.9로 바뀜): 밤에 보스를 5번 때리면 영향 칸 강조 → 보스 커짐 → 커진 채 박치기(Melee) → 작아짐 → 강조 꺼짐 → 피해·넉백(3칸이면 칸별 이동). 대상이 없으면 Core가 준 범위 칸 강조 → 커졌다 작아짐 → 강조 꺼짐.
  5. 낮/밤 전환: 밤이 끝나면 보스·소환이 사라지고 다음 밤에 다시 나온다(같은 개체).
  6. 승리: 보스를 처치하면 소환이 살아 있어도 "보스 격파 · 결과 확인"이 나온다. 확인 뒤 챕터 완료·다음 흐름으로 넘어간다.
  7. (현강님 HUD 연결 뒤) 보스 HUD 1번째 아이콘 "격파 목표", 3번째 "포효 n/5"(4/5 다음 0/5).
- **결과 기록 자리:** 사용자 재생 결과를 받으면 이 절, T9, 보스 View 인계 §8.1, 데이터 경계 §14.4에 같은 결과를 적고 CLAUDE.md 상태 줄을 갱신한다.

### 6.8 Phase 3b — 소환 등장·포효 커지기 동작 (2026-10-15, 코드 완료·Unity 확인 대기)

- **사용자 요청:** 소환 = 칸 주황 강조 → 바닥에서 Y축으로 돌며 등장 → 등장 뒤 강조 끔. 포효 = 영향 칸 강조 → (테스트용) 보스가 크기 2.5까지 커졌다 돌아옴 → Melee.
- **사용자 답(구현 전 질문):**
  - 설정은 `CombatSceneRoot` 씬 설정에 둔다.
  - 포효 강조는 피해·넉백까지 유지한다.
  - 대상 없는 포효는 커지기만 한다(§6.6의 "동작 없이 간격만"을 대신함).
  - 크기는 "현재 크기가 2일 텐데 2.5 내지 3까지". 프리팹 루트 2에 필드 유닛 크기 0.5가 곱해지므로 **현재 크기에 대한 배율**로 두고 기본 1.25(= 2.5), 1.5면 3이다. 설계 D6b.
- **코드:**
  - 새 `Display/ICombatUnitMotionPose`(크기 배율을 더한 자세 계약, 기존 `ICombatUnitPose` 유지).
  - `FieldUnitView.BeginMotionPose`: 같은 자세 대여를 쓰고, `PresentationUnitPose`가 시작 크기를 기억해 `ApplyScale`·`RestorePose`로 되돌린다. HUD 크기 상쇄를 다시 맞춘다.
  - `FieldUnitPresenter.TryBeginMotionPose`.
  - 새 `CombatAbilityMotionSettings`(값 조건 한 곳, `Instant` 기본)와 새 `CombatAbilityMotionPlayback`(등장·커지기, 세대 확인, 취소 시 자세 복원·완료 버림, 표시가 사라지면 다음 Tick에 오류). 회전은 관리 코드로 계산한다.
- **Presenter:**
  - 포효 = 강조 → `BeginPulse` → 끝나면 `TryBegin(AttackResult)`(Melee). 공격이 없거나 시작하지 못하면 완료한다.
  - 소환 = 강조 → 생성 사실 반영 → `BeginRise` → 끝나면 강조 끄고 완료한다.
  - 동작 중에는 다음 사건·최종 표시·동시 사망을 막는다(`IsPresentationComplete`·`CanBeginPresentationEvent`·`RequireIdle`·`Refresh`). 취소·해제에 동작 취소를 넣었다.
  - 동작을 못 시작한 경우만 §6.6의 머묾(`HoldAfter`)을 쓴다.
- **씬 설정:** `CombatSceneRoot` "보스 능력 연출 (테스트 값)" 6개 — 대기 .2초, 등장 .6초, 깊이 1.5, 1바퀴, 커지기 .4초, 배율 1.25. `ValidatePresentationTiming`이 설정 생성자를 부르고, `CombatSceneDisplayFactory`가 사건 Presenter에 넘긴다.
- **시험:**
  - 새 `CombatAbilityMotionPlaybackTests` 10개(15 케이스, 관리 실행 통과): 설정 거절, 등장 깊이·대기·ease-out·회전·복원, 커지기 절반 최대·복원, 음수 시간, 즉시 값, 취소, 세대 변경, 표시 사라짐, 중복 시작.
  - Unity 전용: `CombatStandardAttackTests`의 Phase 3 시험 4개를 새 순서로 바꿨다(강조 → 커지기 → Melee, 빈 포효 커지기만, 소환 깊이에서 올라옴·끝나면 강조 끔, 커지기 중 중단 시 크기 복원). `CombatEnemyPresentationTests`에 씬 설정 거절 6 케이스.
- **MSBuild:** 오류 0, 새 경고 0. 새 파일 3개 + 새 시험 1개(.meta 포함).
- **관리 실행 전체 회귀(39개 묶음, 스크래치 `boss3d/summary.txt`):** Phase 3(`boss3`)와 37개 같다. 다른 2개는 시험이 늘어난 것이다(`CombatDisplayReuseTests` 7→8 통과 = Phase 3 재생 강조 시험, `CombatEnemyPresentationTests` Unity 전용 29→30 = 씬 설정 거절 시험). 실패는 기준선과 같은 `CombatViewSystemTests` 3개뿐이다.
- **Unity 확인 목록:** `CombatAbilityMotionPlaybackTests`·`CombatStandardAttackTests`·`CombatEnemyPresentationTests`와 공격·반응 자세 회귀(`CombatUnitReactionTests`·`CombatUnitDeathTests`·`CombatMortarPresentationTests`·`CombatBallistaAttackTests`). `RestorePose`가 크기도 되돌리게 바뀌었기 때문이다. 재생은 §6.7의 3·4번(바뀐 순서). **§6.9에서 크기를 자세와 분리해 이 마지막 이유는 없어졌다.**

### 6.9 대상 없는 포효의 바닥 표시(Core `AreaCells`)와 포효 순서 변경 (2026-10-15, 코드 완료·Unity 확인 대기)

- **요청 1:** "포효 시 대상이 없더라도 바닥 색 표시 해줘". View가 Fan 모양을 계산하면 Core 규칙 복제라서 선택지를 물었고, 사용자가 **"Core도 제가 고치기"**로 Core 수정을 명시적으로 지시했다. 설계 D6c.
  - Core 변경: `CombatRoarEvent.AreaCells`, `CombatAttackDelivery.DirectAffectedCells`, `AttackSelectionEvaluation.EmptyAreaCells`, `CombatAttackQueries.Selection`의 가림 계산 순서, `CombatAttackCalculator.Reaction(…, out emptyAreaCells)`, `BossRoarCalculator`. 상세 기록은 [데이터 경계 인계 §5.34](../handoffs/2026-09-24-view-core-data-boundary-handoff.md).
  - View: 대상이 없으면 `AreaCells`를 칠하고, 비면 보스 칸을 칠한다.
- **요청 2:** "포효 : 셀 하이라이트 → 사이즈 커짐 → 공격 모션 → 사이즈 작아짐 → 하이라이트 꺼짐". 설계 D6b를 고쳤다.
  - 크기를 자세에서 떼어 **크기 대여**로 옮겼다. 새 `Display/ICombatUnitScale`(옛 `ICombatUnitMotionPose` 파일 이름 변경, GUID 유지), `FieldUnitView.BeginPresentationScale`·`PresentationUnitScale`(재대여·비활성 때 되돌림), `FieldUnitPresenter.TryBeginPresentationScale`. 그래서 커진 채로 공격 자세를 재생할 수 있다.
  - `PresentationUnitPose`는 3b 이전처럼 위치·회전만 다룬다. `BeginMotionPose`는 `ICombatUnitPose`를 돌려준다(소환 등장).
  - `CombatAbilityMotionPlayback.BeginRoar(scale, settings, tryBeginAttack, completed, version)`: 커짐(`roarPulseSeconds`의 절반) → 공격 입구 호출, 완료 콜백을 기다림(세대·단계 확인) → 작아짐(나머지 절반) → 크기 복원 → 완료.
  - Presenter는 완료 때 강조를 끄고 사건을 완료한다. 피해·넉백 사건은 그 뒤에 이어진다(이전의 "강조를 피해·넉백까지 유지"를 대신함).
  - 설정 이름은 그대로 두고 툴팁만 고쳤다(`roarPulseSeconds` = 커지기+작아지기 전체 시간).
- **시험:**
  - Core: `BossRoarTests` 새 2개 + 기존 1개에 단언을 더했다(관리 실행 15/15).
  - View 관리 실행: `CombatAbilityMotionPlaybackTests`를 새 단계로 다시 썼다(11개·16 케이스 통과).
  - Unity 전용: `CombatStandardAttackTests` 포효 3개를 새 순서로 바꿨다(커진 채 박치기, `AreaCells` 강조, 작아진 뒤 강조 끔).
- **MSBuild:** Systems·Tests.Framework·Tests.Core 오류 0, 새 경고 0.
- **Core 관리 실행 전체 회귀(`ProjectL.Tests.Core`, 스크래치 `core3e/summary.txt`):** 시험 묶음 127개 실패 0, 메서드 1277개 통과, Unity 전용 8개.
  - 목록에서 못 찾은 11개 중 10개는 도우미 클래스이고, 1개는 `ThornFieldDomainTests`다. 이 묶음은 `TestCaseSource`만 써서 스크래치 실행기가 찾지 못한다. 이번 변경과 관계없고 Unity에서 돈다.
  - Core 변경 전 기준선(`corebase`, HEAD Core 원본으로 빌드)도 만들어 두었지만, 실패가 없어 비교하지 않았다.
- **Unity 확인 목록:** `BossRoarTests`·Core 공격 시험 전체(가림 계산 순서 변경), `CombatAbilityMotionPlaybackTests`·`CombatStandardAttackTests`·`CombatEnemyPresentationTests`. 재생: 밤 5회 피격 → 영향 칸 강조 → 보스 커짐 → 커진 채 박치기 → 작아짐 → 강조 꺼짐 → 피해·넉백. 대상 없는 포효는 범위 칸 강조 → 커졌다 작아짐 → 강조 꺼짐(§6.10에서 바뀜).

### 6.10 포효 피드백 반영: 대기·대상 없을 때 같은 순서·박치기 방향 (2026-10-15, 코드 완료·Unity 확인 대기)

- **사용자 피드백:**
  1. "대상이 없어도 기존과 같음" → 질문으로 뜻을 확인했다: "대상 없어도 공격 모션까지 똑같이".
  2. "셀 하이라이트에서 커지기로 가는 시간 빼두기".
  3. "보스의 경우 Melee 공격이 뒤로 갔다가 앞이 아니라 앞으로 갔다 뒤로감. Enemy 계열에서 공통적으로 발생하는지 확인".
- **3 원인과 범위:**
  - 공격 연출은 목표를 주 칸 → 없으면 `AttackDirection` 앞의 가장 먼 영향 칸 → 없으면 제자리로 정한다(`CombatAttackTarget.Resolve`). 동작은 `LookRotation(목표 − 원점)`(제자리면 `AttackDirection`) 기준으로 뒤로 뺐다가 나간다(`CombatAttackMotion.Begin`, `CombatMeleeAttackAnimation`).
  - 포효는 전체(All) 지정이라 Core가 `AttackDirection`을 R0으로 준다(`CombatCommandCalculator.AttackRotation`: 칸·방향이 없으면 R0). 그래서 위쪽(플레이어 반대)을 향해 동작해, 플레이어 쪽에서 보면 앞으로 갔다 뒤로 가는 것처럼 보였다.
  - **일반 적은 해당 없다.** 운영 Melee 적은 Demon(TargetingMethod_1, 단일 → 주 칸)·Gravekeeper(TargetingMethod_2, 방향 → 고른 방향)뿐이다. 전체 지정은 보스 포효(TargetingMethod_Stage1BossRoar)만 쓴다. 적 모델 회전(`EnemyFacing` 180도)은 공격 중 `LookRotation`으로 대상 쪽을 보게 되므로 문제없다.
- **코드:**
  - `CombatAttackPlayback.TryBeginFacing(attackerId, …)`: 결과 없이 공격자의 현재 모델 방향 앞을 목표로 Linear·Melee 동작을 재생한다. 공격 사건 경로와 함께 `TryBeginDirect(…, AttackResult? …)` 하나를 쓴다. Mortar·Ballista는 false. §6.6의 `TryBegin(in AttackResult …)`는 포효 전용이었으므로 이것으로 대체했다.
  - Presenter 포효는 대상 유무와 관계없이 `TryBeginFacing`을 쓴다. 보스 표시가 없으면 사실만 보이고 머문다.
  - `CombatAbilityMotionSettings`·`CombatSceneRoot`에 `roarDelaySeconds`(기본 0.2초)를 더했다. 생성자 인자는 7개다. 재생에 `RoarDelay` 단계를 더했고, 남은 시간은 커지기로 넘긴다.
- **시험:**
  - 관리 실행: `CombatAbilityMotionPlaybackTests` 12개·17 케이스 통과(새 대기 시험, 설정 거절에 대기 추가).
  - Unity 전용: `CombatStandardAttackTests`의 포효 시험을 대기·현재 방향 박치기(먼저 뒤로 뺌, 끝나면 방향·위치 복원)·대상 없는 같은 순서로 바꿨다. `CombatEnemyPresentationTests` 씬 설정 거절에 `roarDelaySeconds`를 더했다.
- **MSBuild:** 오류 0, 새 경고 0(추가 목록 없이 Unity csproj만으로).
