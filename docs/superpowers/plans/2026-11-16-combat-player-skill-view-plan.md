# 전투 플레이어 액티브 스킬 View 대응 Task 계획

- 날짜: 2026-11-16
- 상태: TP-1~TP-16 그대로(2026-11-16 사용자, SK-a에서 넷 정정 — §6.2). **SK-0~SK-f 완료(2026-11-17, 컴파일 미확인, 새 시험 없음, 시험 변경 없음).** 사용자 Unity 확인 Ua~Ue·§6.4(팝업 없음) 문제 없음(11-17), 그 뒤 변경(§6.6 메인을 골랐을 때만 진입·§6.7 검토 반영)은 Unity 확인 대기. 남은 것: `skillButton` 필수 참조(e.3)·공유 전달. (`KeySettingUI` 6 → 7은 §6.8)
- 설계: [player-skill-view-design](../specs/2026-11-16-combat-player-skill-view-design.md)(SK1~SK6, P1~P17). Core 원본: [Core → View 인계](../handoffs/2026-11-15-player-active-skills-core-view-handoff.md).
- 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `SI/` = `…/ViewSystem/StageInput/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`.
- 규칙(CLAUDE.md): 새 시험 없음, MSBuild 없음(보고에 "컴파일 미확인"), 커밋·push는 사용자, Phase마다 멈추고 보고, Phase 코드가 끝나면 보고 전에 읽기 전용 새 에이전트로 검토·반영.

## 0. 결정

| 번호 | 결정 |
|---|---|
| Q-AS1~Q-AS4 | 설계 §0(스킬 R·행동 패널 버튼, 일관된 구조 하나, 예보 내용, 외형 표 + 임시 공격 묶음 SO) |
| Q-SK1 | 되돌리기 = Q |
| Q-SK2 | 좌클릭 — 범위를 고를 필요 없으면 영역 안 아무 칸, 점을 고르면 좌클릭마다 점·마지막 점에서 격발 |
| Q-SK3 | A — 조건부 문구는 예보가 보이는 동안 행동 안내 창구에 한 줄(2026-11-16 사용자 "권장안대로") → **2026-11-17 없앰**(사용자 "저 문구를 띄우는 이유가 없다", §6.4) |
| Q-SK4 | A — 외형 줄에 영역 칸별 큐 칸만 두고 이번에는 비움(같은 날) |
| Q-SK5 | A — 스킬 버튼 = 아이콘·남은 횟수 + 호버 시 이름·설명(같은 날) |
| Q-SK6 | A — 키 설정 화면에 스킬 줄을 이번에(`KeySettingUI` 고정 수 6 → 7, 프리팹 줄은 사용자, 같은 날) |

### 0.1 계획에서 정한 것 (확인 대상)

- **TP-1 강조 층 둘**: `SkillCandidate = 1 << 20`·`SkillPoint = 1 << 21`(열거 맨 끝). 층 표 줄 자리 — 후보는 `AttackRange` 줄 바로 뒤(채움, 평소에는 안 그림, 원점 열기 `Origin` — 공격 범위처럼 시전자 칸이 열림), 고른 점은 `Affected` 줄 바로 뒤(채움, 열기 없음 — 영향 칸 위에 보임). 색 종류 `FieldHighlightColor.SkillCandidate`·`SkillPoint`(맨 끝).
- **TP-2 색 기본값**: `skillCandidateColor` = (0.35, 0.75, 1, 1) 하늘색, `skillPointColor` = (1, 0.85, 0.2, 1) 노랑. 기본 에셋 `StageFieldView_Default.asset`에도 같은 값(코드와 한 커밋).
- **TP-3 표시 주인**: 스킬 표시 계약 `ICombatPlayerSkillFeedback`의 주인은 `CombatAttackFeedback`(영향 칸·궤적·넉백·예상 체력의 주인 — 포효 예고와 같은 판단). 새 표시 객체를 만들지 않는다.
- **TP-4 스킬 넉백 출처**: `CombatKnockbackPreview`의 조준 출처로 본다(같은 `_aimShown`). 공격 조준과 스킬 모드는 전경이 배타라 함께 그리지 않는다. 점유 칸은 Buffer 캐릭터의 `OccupiedCells`(예보는 지금 사실 기준), 목록에 없으면 시작 칸 하나. 그리기 본문(`Append`)은 넉백 목록과 점유 칸 공급자를 받는 꼴로 나눠 공격·포효·스킬이 함께 쓴다.
- **TP-5 조건부 문구 (2026-11-17 없앰, §6.4)**: `CombatActionNotice`의 새 입구 둘 — 예보를 그릴 때 `ShowNotice(문구)`(시간 없음), 예보를 지울 때 `HideNotice()`. 그 사이에 다른 안내가 오면 그 안내가 덮고, 지울 때 그 안내도 함께 지워질 수 있다(받아들임). 문구는 "알려진 정보 기준 예상입니다. 숨은 효과나 확률에 따라 달라질 수 있습니다."(초안).
- **TP-6 예상 체력**: 예보 사건 목록에서 유닛마다 마지막 `CombatCharacterEvent`의 끝 체력(죽으면 0). 구조물은 지금 공격 미리보기처럼 HUD가 없어 건너뛴다. HUD 피드백에 `(대상, 끝 체력)` 목록 입구 하나를 더하고, 되돌리기 목록은 공격 예상 체력과 같은 것을 쓴다.
- **TP-7 경로 선**: 궤적 구성 `CombatAttackTrajectory`에 꺾인 선 입구 `ShowPath(시전자, 칸 목록, 공격 표시 값)` 하나. 시작 = 시전자 몸 위치를 발사 높이로 올린 점, 다음 = 칸 가운데를 발사 높이로 올린 점. 선택 중 선(시전자 → 고른 점)과 예보의 실제 경로 선이 같은 입구를 쓴다. 막힘 끊기(`CutProgress`)는 하지 않는다(Core 경로가 이미 실제로 지난 칸).
- **TP-8 남은 점 갈래**: `남은 점 = RequiredPointCount − SelectedPoints.Count`를 선택 답에서 한 번 계산해 둔다. 0 = 영역 갈래, 1 = 마지막 점 갈래, 2 이상 = 점 추가 갈래.
- **TP-9 격발 대기**: 대기 중에는 포인터 이동·클릭·우클릭을 받지 않고, R·버튼·취소 키(공통 취소)는 모드를 끝내며 대기도 버린다. 대기 답이 거절이면 행동 안내 뒤 대기를 버리고 같은 갈래로 돌아간다.
- **TP-10 진입 조건**: 진입은 정책 `CanPreview(SkillAvailability)` + 스킬 상태가 있음(`CommandState.PlayerSkill != null`). 쓸 수 있는지는 진입 뒤 가용성 답이 정하고, 못 쓰면 그 이유를 안내하고 모드를 끝낸다(행동 패널은 SK-e부터 배경 답으로 미리 꺼짐).
- **TP-11 클릭 시점**: 점 추가·격발은 좌클릭 **뗌**(`SelectReleased`)에서 한다(공격 조준과 같음 — 버튼 위에서 뗀 클릭을 막는 Router 규칙을 그대로 씀).
- **TP-12 영역 재생 강조**: 투사체 경로가 없는 사건 51은 재생 중 영향 칸을 재생 강조 층(`PresentationHighlight`)에 칠한다(`CollectHighlight`, 포효와 같은 길).
- **TP-13 경로 비행 일반화 범위**: 풀 `Flight`에 점 배열·누적 길이를 두고 지금의 `From/To` 발사는 점 둘인 경로로 같은 코드를 지난다. 곡사(`FireArc`)는 바꾸지 않는다. 회전은 지금 구간의 방향.
- **TP-14 외형 줄 조회 자리**: 처리기는 `CombatCueSources`를 거쳐 외형 표를 받는다(사건 재생 Factory·Presenter 생성 줄 무변경). 표에 줄이 없으면 결과 시작 때 예외(설정 오류).
- **TP-15 행동 패널 스킬 칸**: SK-a에는 선택 참조(비면 스킬 칸 없음, 경고 없음)로 두고, 사용자가 프리팹에 버튼을 연결한 뒤 SK-e에서 필수 검사로 바꾼다. 아이콘·남은 횟수 글자·켜짐 표시 셋.
- **TP-16 키 설정 화면 줄**: `KeySettingUI` 고정 수를 상수 `KeyButtonCount = 7`로(현강님 줄을 연결 줄로 고침, 공유). 프리팹 줄을 사용자가 넣기 전까지는 키 설정 화면을 열면 예외가 나므로, 이 한 줄과 프리팹 줄은 한 커밋(§4).

## 1. 범위·파일

| 구분 | 파일 | 작성자(SK-0에서 blame으로 다시 확인) |
|---|---|---|
| 정책 | `V/Input/CombatInputPolicy.cs`(`StageOf` 둘) | 우리 |
| 키 | 입력 에셋 `Assets/ProjectL/Input/PlayerInputActions.inputactions`, `SI/StageInputNames.cs`, `SI/CombatInputController.cs`, `SI/IStageInputReceiver.cs`, `SI/StageInputReceiverSlot.cs`, `V/Input/CombatInputReceiver.cs`, `V/Input/CombatInputRouter.cs` | 우리(입력 Controller·수신 계약 일부는 현강님·천명님 줄) |
| 키 설정 | `GlobalSystem/InputSystem/View/IKeyBindingService.cs`(열거), `GlobalSystem/UISystem/Screens/Global/KeySettingUI.cs`(고정 수), `SI/StageInputKeyBindings.cs`, `Prefabs/UI/GlobalUICanvas.prefab`(줄, 사용자) | 현강님(Twinsgm)·호진님 |
| 외형 표 | 새 `DV/Table/PlayerSkill/PlayerSkillVisualDataTableSO.cs`·`Item/PlayerSkillVisualData.cs`·`Item/PlayerSkillVisualDataSO.cs`, 새 `DataBaseSystem/Queries/ViewDataBase/ViewTable/PlayerSkillVisualTable.cs`, `ServiceKeys/DBFactory/Group/ViewTableKey.cs`, `DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs`, `Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `V/CombatDisplayTables.cs`, 에셋 `DataBaseSO/ViewGroup/PlayerSkillVisual/`(표 + 줄 둘)·`ViewGroup.asset` | 새 파일 우리, 연결 줄 민형님·호진님 |
| 색 | `DV/Table/StageField/Item/FieldColorsData.cs`, `StageFieldView_Default.asset`, `V/Display/Field/FieldHighlightLayers.cs`·`FieldHighlightPalette.cs` | 우리 |
| 스킬 입력 | 새 `V/Input/Interaction/CombatPlayerSkillInteraction.cs`, `V/Input/Interaction/CombatInteraction.cs`, `V/Input/CombatSceneInput.cs`, `Factory/…/CombatSceneInputFactory.cs`, `V/Input/Assist/CombatActionNotice.cs` | 우리(안내 창구 일부 호진님 줄) |
| 스킬 표시 | 새 `V/Input/Contracts/ICombatPlayerSkillFeedback.cs`, 새 `V/Display/Feedback/CombatPlayerSkillForecastView.cs`, `V/Display/Feedback/CombatAttackFeedback.cs`·`CombatKnockbackPreview.cs`·`CombatAttackTrajectory.cs`, `V/Display/Unit/CombatUnitHudFeedback.cs`, `V/Display/CombatInputFeedback.cs` | 우리 |
| 재생 | `V/Playback/Ordering/CombatEventTypeTable.cs`, 새 `V/Playback/Handlers/CombatPlayerSkillEventHandler.cs`, `V/Playback/Handlers/CombatEventHandlerTable.cs`, `V/Playback/CombatSceneEventPresenter.cs`(처리기 생성 한 줄), `V/Playback/Support/CombatCueSources.cs`, `V/Presentation/Attack/CombatAttackShape.cs`·`CombatProjectileContacts.cs`·`CombatShotPath.cs`·`CombatProjectilePool.cs`·`CombatProjectileFlights.cs`·`CombatLinearStyle.cs`·`ICombatAttackStyleContext.cs`·`CombatAttackPlayback.cs` | 우리 |
| 버튼 | `V/Input/ActionButtons/CombatActionButtonState.cs`·`CombatActionButtons.cs`·`CombatInputSurfaces.cs`, `V/Input/CombatSceneInputDriver.cs`, `V/Display/Hud/CombatActionPanel.cs`, 새 `V/Input/Interaction/CombatPlayerSkillAvailabilityTracker.cs` | 우리·현강님 줄 섞임 |

- 시험: `Assets/Tests`에서 바뀌는 형식을 쓰는 곳은 `Editor/CombatField/AttackOutlineOpeningTests.cs`(층 열거 값만 — 새 값을 더해도 깨지지 않음)뿐인 것으로 보인다. SK-0에서 다시 grep한다.
- Core·Buffer 무변경. Framework·서비스 키는 연결 줄만(`ViewTableKey` 한 줄).

## 2. Phase와 Task

### SK-0 착수 전 확인 (코드 변경 없음)

- [ ] 0.1 원격·HEAD 확인, 작업 트리의 스테이지된 PD 묶음이 커밋됐는지(안 됐으면 사용자에게 물음 — 같은 파일 `CombatAttackShape`·`CombatProjectileContacts`·`CombatLinearStyle`·`CombatKnockbackPreview`를 고친다).
- [ ] 0.2 §1 파일의 고칠 줄 작성자 표(`git blame`). 다른 작업자 줄은 공유 목록에.
- [ ] 0.3 `Assets/Tests` 참조 grep(§1 형식·생성자·열거 수). 깨지는 곳이 있으면 보고.
- [ ] 0.4 Core 계약 다시 대조: 사건 51 위치(그 결과 앞)·`Hits[].Contacts[].DeliveryIndex` → `ImpactCells`·`CombatResult.PlayerSkill`·선택 답 `RequiredPointCount`.
- [ ] 0.5 `CombatAttack_Linear.asset`이 직선 투사체 부품을 쓰는지, `CombatAttack_Melee.asset`이 투사체 없는 묶음인지 확인(설계 P11).

### SK-a 값·키·외형 표 (화면 변화: Q·R 키만)

- [x] a.1 정책: `StageOf(명령)`에 `UsePlayerSkill`, `StageOf(미리보기)`에 `SkillAvailability`·`SkillSelection`·`SkillForecast` → `StagePhase.Combat`. 튜토리얼 허용표는 그대로(P9).
- [x] a.2 입력 에셋: `Combat` 맵 `Return` 바인딩 `<Keyboard>/r` → `<Keyboard>/q`, 새 액션 `Skill`(Button) + 바인딩 `<Keyboard>/r`(새 GUID 둘).
- [x] a.3 받는 길: `StageInputNames.Combat.Skill`, `CombatInputController`에 `Press(Skill, RequestSkill)`, `IStageInputReceiver.RequestSkill` + 구현 둘, Router 요청 `Skill`(열거 맨 끝). Router의 `Skill`은 SK-b 전까지 아무 일도 하지 않는다(빈 갈래 + 주석). 실제 이름은 `SkillRequested`(정정 §6.2).
- [~] a.4 키 설정: `KeyBindingAction.Skill`(맨 끝), `StageInputKeyBindings`에 `(Skill, Combat.Map, Combat.Skill, 0)`, `KeySettingUI` 고정 수 `KeyButtonCount = 7`(TP-16). **고정 수는 프리팹 줄이 생긴 뒤로 미룸(§6.2, 검토 H1).**
- [x] a.5 외형 표 `PlayerSkillVisual`: 줄 = `PlayerSkillKey`(None 금지), 칸 = 아이콘(필수)·이름·설명·공격 연출 SO(필수)·영역 칸별 큐(`CombatVisualCueInput`, 비워도 됨, Q-SK4 A)·강화 덮어쓰기 목록(강화 키 None 금지·중복 금지, 이름·설명·공격 연출 SO — 비우면 기본 줄 값). 조회 `Get(스킬, 강화)`·`TryGet`. 검사는 중복·None·빈 SO만(P10).
- [x] a.6 연결 줄: `ViewTableKey.PlayerSkillVisual`, `ViewDataBase` 속성·읽기, `ViewDataBaseFactory` 한 줄, `CombatDisplayTables` 생성 인자·속성(+ `From`).
- [x] a.7 에셋: `DataBaseSO/ViewGroup/PlayerSkillVisual/PlayerSkillVisualDataTable.asset`, `Item/PlayerSkillVisual_PowderBlossom.asset`(`CombatAttack_Melee`, 강화 줄 만개·단죄 이름·설명), `Item/PlayerSkillVisual_MagicBullet.asset`(`CombatAttack_Linear`, 강화 줄 왜곡·심판), `ViewGroup.asset` 한 줄. 아이콘은 임시로 기존 특성 아이콘 하나를 빌린다(없으면 사용자에게 물음).
- [x] a.8 색·층: `FieldColorsData` 두 칸·읽기·검사, 기본 에셋 두 값(TP-2), 층 열거 둘·색 종류 둘·층 표 두 줄(TP-1)·팔레트 두 줄.
- [x] a.9 행동 패널: `CombatActionPanel`에 선택 참조 `skillButton`·`skillIcon`·`skillUsesText`·`skillActiveIndicator`(TP-15, 비면 스킬 칸 없음). 묶음 값은 SK-e까지 늘 숨김. 실제로는 `skillButton` 하나만, 나머지 셋은 SK-e(정정 §6.2).
- [x] a.10 Phase 검토(새 에이전트) → 반영 → 보고.
- **Ua (Unity)**: 컴파일, DB 생성(부트), Q = 이동 되돌리기·R = 아무 일 없음, 되돌리기 버튼 그대로, 키 설정 화면(프리팹 줄을 넣은 뒤) 되돌리기 = Q·스킬 = R, 되돌리기를 R로 바꾸려 하면 겹침.

### SK-b 스킬 모드 입력·선택 표시

- [x] b.1 표시 계약 `ICombatPlayerSkillFeedback`(선택 쪽): `ShowSkillCandidates(cells)`, `ShowSkillPoints(caster, points, settings)`(점 칸 + 시전자 → 점 선, TP-7), `ClearSkillSelection()`. 주인 `CombatAttackFeedback`(TP-3), 묶음 `CombatInputFeedback.PlayerSkill`.
- [x] b.2 입력 소유자 `CombatPlayerSkillInteraction`(+ `OwnerAdapter`, `Target = PointerTarget.Skill`): 진입·끝(토글), 가용성 요청 → 답 `CanUse`면 선택 `[]` 요청, 아니면 안내 뒤 끝. 선택 답 반영(후보·점·선, 남은 점 계산 TP-8), 좌클릭 뗌 = 남은 점 2개 이상이면 후보 칸 점 추가(후보 밖은 무시 + 소리), 우클릭 = 마지막 점 빼기(없으면 끝), 호버 칸 강조(`CombatHoverTracker`). 낡은 답 거르기(번호·종류·Source 넷·점 목록). 행위자 = 스킬 상태 `ActorId`(P2). 자기 번호만 `ClearPreview(number)`.
- [x] b.3 포인터 대상: `PointerTarget.Skill = 5`, `ResolvePointerTarget`에 스킬 모드 인자(스킬 > 공격 > 경로), `CombatInteraction.ResolvePointerTarget`·사실 읽기가 같은 값.
- [x] b.4 묶음 연결: 소유자 목록에 스킬(공격 뒤, 호버 앞), `SkillModeRequested(facts)` 입구(`BeginForeground` → 진입), `IsSkillActive`, `CancelDragOrAttack`·`Cancel`이 스킬 모드도 끝냄, `CommandResolved` 전달.
- [x] b.5 Router: `Skill` 요청 = 스킬 모드면 끝, 아니면 경로 취소(`LeaveMoveMode`) 뒤 진입. 튜토리얼 제한 중이면 막힘 보고(P9). 버튼용 `SkillToggle`은 같은 갈래라 따로 두지 않는다(입구 하나).
- [x] b.6 거절 안내: `ICombatActionRejectionSink.SkillRejected(PlayerSkillFailureReason)` + 문구 아홉(설계 SK1). 점 오류 일곱은 안내하지 않음.
- [x] b.7 입력 Factory: 소유자 생성 줄(쓰기·읽기 Buffer·집기·표시 계약·게이트·호버·안내·소리·상태 읽기).
- [x] b.8 Phase 검토 → 반영 → 보고. (SK-e 뒤 묶음 끝 검토 하나로, §6.3)
- **Ub**: R로 진입·끝, 메인이 아닌 유닛 선택 중에도 메인 기준, 마탄 A 후보 → A 찍으면 B 후보(+ 시전자 → A 선), 우클릭 빼기·끝, 화약꽃은 진입하자마자 후보 없음(완료 상태, 예보는 SK-c), 스킬 모드 중 E·이동 Drag 안 됨, 결과 재생 중 진입 거절.

### SK-c 예보·격발

- [x] c.1 예보 읽기 `CombatPlayerSkillForecastView`(순수): 예보 `Prediction.Events`에서 영향 칸 합(사건 51 `AffectedCells`)·경로(첫 사건 51 중 투사체 경로가 있는 것의 `ProjectilePath`)·유닛별 끝 체력(TP-6)·넉백 목록(사건 51 `Knockbacks` 모두). 결과를 재사용 목록에 담아 매번 새로 만들지 않음.
- [x] c.2 표시 계약(예보 쪽): `ShowSkillForecast(view, caster, settings)`·`ClearSkillForecast()`. `CombatAttackFeedback`이 `Affected` 층·궤적 `ShowPath`·넉백(TP-4)·HUD 예상 체력(TP-6)을 그림. 마지막 점 호버 중에도 후보 층은 남김(설계 SK2).
- [x] c.3 `CombatKnockbackPreview`: 그리기 본문을 넉백 목록 + 점유 칸 공급자 꼴로 나누고 입구 `ShowSkill(knockbacks, occupiedOf)`·`ClearSkill()`(조준 출처).
- [x] c.4 HUD: `CombatUnitHudFeedback.ShowHealthPreview(IReadOnlyList<(EntityId, int)>)`, 지우기는 공격과 같은 목록.
- [x] c.5 궤적: `CombatAttackTrajectory.ShowPath`(TP-7). 시전자 출발점 = 직선 투사체 출발점 식(`CombatShotPath.LinearStart` 등 지금 직선 공격과 같은 식).
- [x] c.6 소유자 예보 갈래: 남은 점 0 → 선택 답 직후 예보 한 번 요청, 1 → 후보 칸 호버가 바뀔 때만 (점 + 그 칸) 예보 요청(후보 밖이면 예보 지움). 답 반영은 그 점 목록이 지금 고른 점(+ 호버 칸)과 같을 때만.
- [x] c.7 격발(Q-SK2·TP-9·TP-11): 남은 점 1 → 후보 칸 뗌, 0 → 그려진 영향 칸 안 뗌(P13). 그 점 목록의 예보가 `CanUse`면 `CombatViewCommandData.ForPlayerSkill(actor, points, forecast.Source)`를 게이트로 제출, 없으면 격발 대기(그 점 목록 예보 요청 → 답이 오면 제출, 거절이면 안내). 제출 뒤 표시는 그대로 두고 입력만 닫음.
- [x] c.8 결과: `CommandResolved` = 이 모드의 명령 번호면 수락 여부 기억, `AfterResult` = 수락이면 끝, 거절(낡은 Source 포함)이면 표시를 지우고 가용성부터 다시(P8). `BeforeResult` = 예보·넉백·궤적 지움.
- [x] c.9 조건부 문구(TP-5): 예보를 그릴 때·지울 때. → 2026-11-17 걷음(§6.4).
- [x] c.10 Phase 검토 → 반영 → 보고. (묶음 끝 검토, §6.3)
- **Uc**: 화약꽃 진입 = 폭발 범위·예상 체력·문구, 범위 안 클릭 = 격발, 밖 클릭 무시. 마탄 마지막 점 후보 호버 = 실제 경로 선·영향 칸·예상 체력·(심판) 넉백 화살표, 클릭 = 격발. 호버 전 빠른 클릭도 격발(대기). 예보와 실제 결과의 칸·체력 비교. 이동한 뒤·턴이 바뀐 뒤 낡은 답 거름. 사용 뒤 같은 턴 재진입 = "이번 턴 사용" 안내.

### SK-d 사건 51 재생

- [x] d.1 형식 표: 처리기 칸 `CombatEventHandlerSlot.PlayerSkill`(맨 끝), `Row<CombatPlayerSkillAttackEvent>(Slot.PlayerSkill, isActionStart: true, actor: e => e.Attack.ActorId, attackSource: e => new CombatAttackSource(e.Attack.ActorId, e.Attack.AttackGroup, null), pacing: _ => CombatEventPacing.Attack)`.
- [x] d.2 공격 모양: `CombatAttackShape.ForPlayerSkill(CombatPlayerSkillAttackEvent)` — 방향 = 첫 경로 구간 방향(경로 없으면 시전자 보인 방향), 타격 칸 없음, 영향 칸 = `AffectedCells`, 주 대상 없음, 완료 = 첫 유효 해제, 새 칸 `SkillPath`(투사체 경로 칸, 없으면 null)·`SkillContacts`(대상 → 접촉 칸, `Hits[].Contacts[].DeliveryIndex` → `ImpactCells`).
- [x] d.3 경로 비행 일반화(TP-13): `CombatShotPath`에 꺾인 선의 누적 길이·위치·방향 함수, 풀 `Flight`에 점 배열(점 둘이면 지금과 같음), `CombatProjectileFlights.FirePath`·맥락 `FirePath`, `CombatProjectileContacts.CollectAlongPath`(접촉 칸까지의 누적 길이 비율 = 진행률, 같은 대상은 마지막 접촉이 해제 — D6 규칙 `CombatContactRelease` 그대로).
- [x] d.4 직선 종류의 경로 발사: `CombatLinearStyle`에 `SkillPath`가 있으면 경로 발사(시전자 출발점 → 칸 가운데들, 마지막 칸에서 사라짐). 지금 직선 공격은 점 둘 경로로 같은 코드.
- [x] d.5 처리기 `CombatPlayerSkillEventHandler`: 결과 시작 때 `CombatResult.PlayerSkill`의 (스킬, 강화) → 외형 줄(TP-14, 없으면 예외) → 공격 연출 SO, 모양이 길을 정함(P5): 경로 있음 = 공격 연출 SO가 직선 투사체여야 함(아니면 예외) → `CombatAttackStart.TryBegin(shape, settings, …)`, 경로 없음 = 투사체를 쏘지 않는 근접 꼴 조립(맞는 순간 모두 해제) + `CollectHighlight`(TP-12).
- [x] d.6 처리기 표·Presenter 생성 한 줄, `CombatCueSources`에 외형 표.
- [x] d.7 Phase 검토 → 반영 → 보고. (묶음 끝 검토, §6.3)
- **Ud**: 마탄 꺾인 비행·접촉마다 그 대상 피격(관통이면 여러 대상), 심판 첫 대상 뒤 밀림, 화약꽃 폭발·연쇄(만개 8 → 20 → 36), 단죄 기절 아이콘, 연쇄 공격(처치 연쇄) 그대로, **지금 직선 공격(Sniper·Akimbo 등)·관통이 그대로**, 사건 51 경고가 더 안 나옴.

### SK-e 행동 패널 버튼·배경 조회·호버

- [x] e.1 가용성 배경 조회 `CombatPlayerSkillAvailabilityTracker`(행동 가능 여부 조회 `CombatActionAvailabilityTracker`와 같은 꼴: 열쇠 = 전투·턴·`QueryRevision`·결과 번호·행위자, 슬롯 규칙 `CombatPreviewSlotRule`, 전경이 있으면 대기만 내려놓음). 행위자 = 스킬 상태 `ActorId`, 스킬 상태가 없으면 조회 없음.
- [x] e.2 버튼 상태: `CombatActionButtonState`·`CombatActionButtons`에 `HasSkill`·`CanSkill`·`IsSkillActive`·아이콘·남은 횟수/한도, `CombatInputSurfaces.Sync`·`CombatSceneInputDriver` 한 줄. 튜토리얼 갈래(`CombatActionButtons.cs:58`, 호진님 줄)는 스킬을 못 누르게, 같음·해시(`:78`·`:84`)에 새 칸(SK-0 결과).
- [~] e.3 `CombatActionPanel`: 스킬 버튼 묶음(네 번째 칸) 적용, 누름 = R과 같은 입구(`BindActions`에 넷째 입구), 필수 참조로 바꿈(TP-15, 사용자가 프리팹 연결 뒤). **필수로 바꾸기는 프리팹 연결 뒤(§6.3).**
- [x] e.4 호버(Q-SK5 A): 공용 Hover 패널(`IUITextPresentation.ShowHover(이름, 설명)`·`MoveHover`·`UnHover`, UnitInfo 특성 칸과 같은 방식)으로 (스킬, 강화) 외형 줄의 이름·설명 + 남은 횟수 한 줄.
- [x] e.5 Phase 검토 → 반영 → 보고. (묶음 끝 검토, §6.3)
- **Ue**: 남은 횟수 표시·사용 뒤 줄어듦, 같은 턴 사용 뒤 꺼짐·다음 턴 켜짐, 기절·무장 해제에 꺼짐, 메인이 필드에 없으면 꺼짐, 버튼 누름 = R, 호버 설명.

### SK-f 문서·공유·묶음 끝 검토

- [x] f.1 폴더 배치 안내(새 파일 위치·규칙 한 줄: "플레이어 스킬은 스킬 모드 하나 + 사건 51 처리기 하나, 갈래는 Core 답(남은 점 수)·사건 모양으로").
- [x] f.2 공유(모두 미전달, 대상 줄은 §6.1 표): 현강님(키 설정 열거·`KeySettingUI` 고정 수·프리팹 줄, 행동 안내 계약·문구, 행동 패널 넷째 입구·스킬 칸), 호진님(`StageInputKeyBindings` 줄, 행동 버튼 튜토리얼 갈래·같음·해시, 외형 표 넣는 법), 민형님 Factory 공유(`ViewTableKey`·`ViewDataBaseFactory` 줄), 천명님 Core 전달 문서(요청 없음, 확인 셋 — 예보 사건 순서·접촉 칸 대조·표적 예산 0이면 경로 없음).
- [x] f.3 설계 구현 메모, 묶음 끝 검토(새 에이전트, 7기준), 평가 아티팩트 차수는 사용자 지시 대기(지금 보류 중). (§6.7)

## 3. Unity 확인

Phase마다 위 Ua~Ue. 사용자가 확인하고, 보고에는 "컴파일 미확인"을 적는다.

## 4. 커밋 묶음 (커밋은 사용자)

1. SK-a 한 커밋: 외형 표 코드 + 에셋 셋 + `ViewGroup.asset`, `FieldColorsData` 코드 + `StageFieldView_Default.asset`, 입력 에셋 + 받는 길 코드, `KeySettingUI` 고정 수 + `GlobalUICanvas.prefab` 줄(TP-16). 없으면 DB 생성 실패·키 설정 화면 예외.
2. SK-b~SK-e는 Phase마다 하나씩(새 `.cs`는 `.meta`와 함께).
3. SK-f 문서.

## 5. 7기준 점검(계획)

- SSOT: 쓸 수 있음·후보·완료·예보·남은 횟수 = Core 답. 갈래 = 남은 점 수 하나, 재생 길 = 사건 모양 하나, 외형 = 표 한 곳. 넉백·예상 체력·궤적은 기존 그리기 한 곳을 공격·포효와 함께 씀.
- SOLID: 스킬 모드 소유자 하나, 처리기 하나. 표시 주인은 기존 주인에 계약 하나(TP-3).
- FW: 새 서비스 없음, 연결 줄만.
- 독립성: 새 스킬 = 외형 줄 하나. 새 모양만 처리기 갈래 하나.
- 불필요: 0점 스킬도 선택 요청 한 번(점 수를 View가 갖지 않기 위한 값).
- 매 프레임: 호버 예보는 칸이 바뀔 때만, 예보 읽기는 답이 올 때만, 목록 재사용.

## 6. 실행 기록

### 6.1 SK-0 착수 전 확인 완료 (2026-11-16, 사용자 "그대로, SK-0 진행" = TP-1~TP-16 그대로, 코드 변경 없음)

**0.1 원격·HEAD**
- HEAD = `5d83f87ae`(사용자 커밋 "[용근] 전투 View, SoulEater, Sniper 및 그에 따른 특성 추가") = 원격 브랜치. 관통·죽음의 손 묶음이 이 커밋에 들어가 착수 조건이 풀렸다. 스테이지에는 문서 둘(CLAUDE.md·관통 계획)만 있다.
- `origin/develop`이 9커밋 앞선다(천명님 장애물 타수·기둥·이동 플랫폼·넥서스 증원 Core, 민형님 애니메이션 교체). **View 코드는 없고** 이 계획의 고칠 파일과 겹치지 않는다. 다만 새 사건 52~58(장애물 내구도·기둥·플랫폼·환경 무작위)과 미리보기 종류 `PlatformForecast = 15`가 생겨, 병합하면 그 사건들은 지금처럼 경고 뒤 건너뛴다 — 스킬과 별개의 View 작업이다. SK는 지금 HEAD에서 시작해도 되고, develop 병합은 사용자 판단.

**0.2 고칠 줄 작성자 (`git blame`, 얕은 clone 아님)** — 우리 = Droute·Claude·choi yong geun.

| 파일·자리 | 다른 작업자 줄 | 이 계획의 변경 |
|---|---|---|
| `GlobalSystem/UISystem/Screens/Global/KeySettingUI.cs:66-67`(버튼 6개 검사) | 현강님(Twinsgm) | 고정 수 6 → 7(SK-a, 공유) |
| `GlobalSystem/InputSystem/View/IKeyBindingService.cs`(열거 `KeyBindingAction`) | 현강님 | 맨 끝에 `Skill` 한 줄(공유) |
| `SI/StageInputKeyBindings.cs`(파일 전체) | 호진님(CCRC_Hojin) | 정의 한 줄 더함(공유) |
| `V/Input/Assist/CombatActionNotice.cs:10-14`(계약 `ICombatActionRejectionSink`)·클래스 대부분 | 현강님 | 계약에 `SkillRejected` 한 줄, 클래스에 스킬 문구(SK-b, 공유 — 조건부 문구 입구는 11-17 걷음) |
| `V/Input/ActionButtons/CombatActionButtons.cs:58`(튜토리얼 갈래)·`:78`(같음)·`:84`(해시) | 호진님 | 스킬 칸을 더해 세 줄 모두 고침(SK-e, 공유 — 튜토리얼 갈래는 스킬 못 누름) |
| `V/Display/Hud/CombatActionPanel.cs:94-96`(세 입구 함께 연결 검사)·필드·`SetAvailable`·`FindMissingReference` | 현강님(대부분)·호진님 일부 | 넷째 입구·스킬 칸 필드·적용·검사(SK-a 선택 참조, SK-e 필수, 공유) |
| `ServiceKeys/DBFactory/Group/ViewTableKey.cs` | 민형님(JMH) 대부분 | 키 한 줄 더함(연결 줄, 민형님 Factory 공유) |
| `DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs`·`Factory/DBFactory/Group/ViewDataBaseFactory.cs`·`V/CombatDisplayTables.cs` | 호진님·현강님 줄 이웃 | 줄 더하기만(남의 줄 무변경) |
| `SI/StageInputNames.cs:41`·`CombatInputController.cs:32`·`IStageInputReceiver.cs:39-40`·`StageInputReceiverSlot.cs:100`·`V/Input/CombatInputReceiver.cs:93`·`CombatInputRouter.cs:59-60` | 현강님(병종 순환 줄) | 그 줄 옆에 더하기만(무변경) |
| `V/Input/CombatInputRouter.cs` `Route` 갈래 | 호진님·천명님·현강님 줄 | 새 갈래만 더함, `RoutePointer`의 UI 가림 조건 한 줄(우리 줄)에 스킬 대상 |
| `V/Input/Interaction/CombatInteraction.cs`·`V/Input/CombatSceneInput.cs`·`Factory/…/CombatSceneInputFactory.cs` | 현강님·천명님·호진님 줄 이웃 | 생성자·목록·입구 더하기(남의 줄 무변경) |
| `V/Display/Feedback/CombatAttackFeedback.cs` | 천명님(끌어오기 범위 `:37-38`·`:119-141`) | 새 영역만 더함(무변경) |
| `V/Playback/Ordering/CombatEventTypeTable.cs` | 호진님(처리기 칸 토네이도·낙석·하붑) | 칸·줄을 맨 끝에 더함(무변경) |
| `V/Playback/CombatSceneEventPresenter.cs:205-217`(처리기 표 생성) | 여러 작업자 줄 섞임 | 처리기 인자 한 줄 더함 |
| `V/Presentation/Attack/CombatShotPath.cs:35-37` | 민형님(곡사 출발 접선) | 곡사 줄은 무변경, 꺾인 선 함수만 더함 |
| `V/Presentation/Attack/CombatAttackPlayback.cs` | 민형님·천명님 일부 | 경로 발사 입구만 더함 |
| 그 밖의 §1 파일(정책 `StageOf`, 표시·재생·투사체 파일, 버튼 상태·입력 표면·입력 분배 Sync 줄) | 없음(정책 `:308` 천명님 한 줄은 무변경) | — |

**0.3 시험 참조**: `Assets/Tests`(563개)에서 고칠 형식을 쓰는 곳은 `Editor/CombatField/StructureHitPresentationTests.cs`(`CombatInputPolicy.LayerOf`·`CanSelectUnit`만)와 `Editor/CombatField/AttackOutlineOpeningTests.cs`(층 열거 값만) 둘. 새 열거 값·생성자 인자로 깨지는 곳 없음(예상). `IStageInputReceiver`·`KeyBindingAction`·버튼 상태를 구현·생성하는 시험 가짜 없음.

**0.4 Core 계약**(설계와 같음)
- 마탄은 사건 51 하나를 결과 맨 앞에(`MagicBulletCalculator.cs:83` `Insert(0, …)`), 화약꽃은 폭발마다 그 폭발 사건들 앞에(`PowderBlossomCalculator.cs:74` `Insert(eventStart, …)`).
- `ProjectilePath`는 늘 null이 아닌 목록(`PlayerSkillResult.cs` 생성자), `ImpactCells`의 칸은 모두 경로 안(Core 생성자 검사). 접촉 순번 = `Hits[].Contacts[].DeliveryIndex`. 화약꽃은 경로·접촉 칸을 넘기지 않아 빈 목록 → 영역 길.
- 스킬 키·강화 키 = `CombatResult.PlayerSkill`(`CombatResult.cs:31`), 스킬 상태 = `CombatCommandState.PlayerSkill`(`:36`, 행위자 `ActorId`).
- 찾은 것: 마탄의 표적 예산이 0으로 작성되면(`MagicBulletCalculator.cs:73`) 경로가 빈 목록이라 영역 길로 재생된다(비행 없음). 지금 에셋 값(예산 1 또는 관통 4)에서는 닿지 않는다 — 그대로 두고 기록만.

**0.5 임시 연출 에셋**: `CombatAttack_Melee` = 투사체 없음 + 코드 모션(영역 길에 맞음), `CombatAttack_Linear` = `CombatProjectile_Bullet`(`CombatStraightProjectileSO`) + 코드 모션(경로 비행에 맞음, Sniper·MainCharacter·2연발이 씀). 빌릴 임시 아이콘은 특성 외형 에셋에 있다(예: 작렬). SK-a에서 하나 고른다.

**계획 고침(SK-0 결과)**: SK-e e.2에 "튜토리얼 갈래(`CombatActionButtons.cs:58`, 호진님 줄)는 스킬을 못 누름"을 더하고, SK-f 공유 목록에 위 표의 다른 작업자 줄을 넣는다.

### 6.2 SK-a 값·키·외형 표 완료 (2026-11-16~17, 사용자 "SK-a 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

**바꾼 것**
- a.1 정책 `CombatInputPolicy.StageOf` 둘: `UsePlayerSkill`·`SkillAvailability`·`SkillSelection`·`SkillForecast` → 전투 국면.
- a.2 입력 에셋 `PlayerInputActions.inputactions`: `Combat/Return` 바인딩 R → Q, 새 액션 `Skill`(Button, R, GUID `2d7002fa…`·바인딩 `0f7ae4c5…`). 줄 끝 CRLF 그대로, 래퍼 코드 생성 없음.
- a.3 받는 길: `StageInputNames.Combat.Skill`, `CombatInputController` `Press(Skill, SkillRequested)`, `IStageInputReceiver.SkillRequested()`(계획의 `RequestSkill` 대신 같은 파일의 `…Requested` 꼴로 — 정정), 구현 `StageInputReceiverSlot`·`CombatInputReceiver`, Router `Request.Skill`(맨 끝)·`Route` 빈 갈래. `IStageInputReceiver.RequestReturn` 문서의 옛 "체크포인트 복귀"를 "이동 되돌리기"로.
- a.4 키 설정: `KeyBindingAction.Skill`(맨 끝, 현강님 열거), `StageInputKeyBindings` 정의 줄(그룹 0, 호진님 줄). **`KeySettingUI` 6 → 7은 되돌림(검토 H1)** — 아래.
- a.5·a.6 외형 표: 새 `DV/Table/PlayerSkill/PlayerSkillVisualDataTableSO.cs`·`Item/PlayerSkillVisualData.cs`(값 `PlayerSkillVisualData`·`PlayerSkillUpgradeVisual`·고른 값 `PlayerSkillVisualLook`)·`Item/PlayerSkillVisualDataSO.cs`, 새 `Queries/ViewDataBase/ViewTable/PlayerSkillVisualTable.cs`(`Get`·`TryGet(스킬, 강화)`), 연결 줄 `ViewTableKey.PlayerSkillVisual`(민형님 파일)·`ViewDataBase`·`ViewDataBaseFactory`·`CombatDisplayTables`(생성 인자·속성·`From`). 새 큐 칸 `CombatCueEvent.PlayerSkillCell = 16`(맞음 Hit만, 자리 칸만, 흔들림 금지).
- a.7 에셋: `DataBaseSO/ViewGroup/PlayerSkillVisual/` 표 + 줄 둘(화약꽃 = `CombatAttack_Melee`·아이콘 `KillChainAttack.png`, 마탄 = `CombatAttack_Linear`·아이콘 `PiercingFalloff.png` — 둘 다 특성 아이콘을 빌린 임시), 강화 줄 넷(이름·설명만), `ViewGroup.asset` 끝 한 줄. `.cs`·폴더·에셋 `.meta`는 직접 만듦(새 GUID 11개, 저장소 안 중복 없음 — 검토 확인).
- a.8 색·층: `FieldColorsData` 두 칸(머리말 "플레이어 스킬 색")·읽기·검사, 기본 에셋 두 값, 층 `SkillCandidate = 1 << 20`·`SkillPoint = 1 << 21`, 색 종류 23·24, 층 표 두 줄, 팔레트 두 줄.
- a.9 행동 패널: 선택 참조 `skillButton` 하나만(연결돼 있으면 늘 숨김). 아이콘·남은 횟수·켜짐 표시 칸은 쓰는 SK-e에서 더한다(안 쓰는 직렬화 칸의 경고를 피함 — 정정).
- 문서: `viewDocs/stage-input-guide.md` 되돌리기 기본 키 Q·스킬 줄(검토 L5).

**계획과 다른 점(TP 정정)**
- TP-1: 후보 층의 원점 열기를 `Origin` → **없음**. 원점은 선택 유닛 칸(ActiveActor 층)인데 시전자(메인)가 선택 유닛과 다를 수 있어서.
- TP-14: 외형 줄이 없으면 "결과 시작 때 예외" → **전투 조립 때(`CombatDisplayTables`) Core 스킬 키마다 줄이 있는지 검사**(상태 외형 표와 같은 방식, 검토 L7).
- TP-15: SK-a의 행동 패널 칸은 `skillButton` 하나.
- TP-16: `KeySettingUI` 6 → 7은 **프리팹 줄이 생긴 뒤에** 바꾼다. GlobalUICanvas가 Boot 씬에 있어 6줄 프리팹에 7을 요구하면 부트의 `UISystem.PostBindService` → `KeySettingUI.BindButtons`에서 예외로 모든 Play가 멈추기 때문(검토 H1). 그 전까지 스킬 키는 키 설정 화면에 안 보이지만, 정의 줄이 있어 겹침 검사는 된다.

**검토(새 에이전트 하나, 읽기만)**: 컴파일 오류 없음(접근 수준·using·이름 충돌·계약 구현·층 표 정적 검사·팔레트·`StageOf`·계측 표·키 설정 서비스 초기화·inputactions JSON·GUID·직렬화 이름 모두 확인). 높음 1·중간 1·낮음 9.
- 반영: H1(위 TP-16), M1 `PlayerSkillCell` 칸 흔들림 금지(`CombatPresentationCueRules.FindError`), L6 강화 줄 오류에 스킬 키 붙임, L7(위 TP-14), L8 설명 문구에서 Core 수치(50%·8/20/36칸·150%·2칸·1턴) 뺌(한 사실 두 곳 방지), L5 안내 문서.
- 기록만: L3 계획 이름 차이(위 정정), L4 예전 저장 키 설정이 줄 수 4 → 5로 맞지 않아 기본값으로 돌아감(경고 한 번, 멈추지 않음 — 고치려면 현강님 GlobalSystem 읽기 규칙을 바꿔야 해 공유 대상), L9 임시 아이콘, L10 커밋에서 뺄 작업 트리 변경(`PlayerLoadout_1`·`UnitRoster_1`·`Unit_Akimbo`·`Unit_ShotGun`·`TutorialDefinition_1-1`·FMOD 캐시·카탈로그·글꼴 — 우리 변경 아님), L11 공유 대상(SK-f, §6.1 표).

**Ua (Unity) — 바뀐 목록**: 컴파일, 부트 DB 생성(새 표·에셋), Q = 이동 되돌리기·R = 아무 일 없음, 되돌리기 버튼 그대로, 키 설정 화면 되돌리기 = Q(스킬 줄은 프리팹 줄 뒤), 공격 범위·이동 범위 등 지금 강조 모양 그대로(층 표 줄 더함).

**남은 것(SK-a)**: 사용자가 `GlobalUICanvas.prefab` 키 설정 화면에 일곱째 줄(Action = Skill, Button, Text)을 더하면 `KeySettingUI` 고정 수 6 → 7 한 줄(현강님 줄)을 고친다 — 둘을 한 커밋.

### 6.3 SK-b~SK-e 스킬 모드·예보·격발·사건 51 재생·버튼 완료 (2026-11-17, 사용자 "SK-e까지 일괄 진행하고 마지막에 검토 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub~Ue 대기)

Phase마다 검토하지 않고 SK-e 뒤 한 번 검토했다(사용자 지시, b.8·c.10·d.7·e.5를 묶음 끝 검토 하나로).

**바꾼 것**
- SK-b 스킬 모드: 새 `Input/Contracts/ICombatPlayerSkillFeedback.cs`(선택 `ShowSkillSelection(시전자, 후보, 점, 공격 연출)`·`ClearSkillSelection`, 예보 `ShowSkillForecast`·`ClearSkillForecast`), 주인 `CombatAttackFeedback`(후보·점 층, 시전자 → 점 선), 묶음 `CombatInputFeedback.PlayerSkill`. 새 입력 소유자 `Input/Interaction/CombatPlayerSkillInteraction.cs`(단계 가용성 → 선택 → 예보, 요청 하나만 기억, 답은 번호·종류·출처 넷·점 목록 대조, 자기 번호만 비움). 포인터 대상 `PointerTarget.Skill = 5`(스킬 > 공격 > 아이템), 묶음 `CombatInteraction`(소유자 목록 공격 뒤·호버 앞, `SkillModeRequested`·`CancelSkill`·`SkillBack`·`IsSkillActive`·`CanEnterSkill`), Router(`Skill` = 켜짐이면 끔, 아니면 튜토리얼 막힘 보고 → `LeaveMoveMode` → 진입, 우클릭 = 스킬 모드면 `SkillBack`, UI 가림 조건에 스킬 대상), 거절 안내 `ICombatActionRejectionSink.SkillRejected`(현강님 계약, 문구는 `CombatActionNotice`), 정책 `CanQuerySkillAvailability`, 입력 Factory 생성 줄.
- SK-c 예보·격발: 새 `Input/Contracts/CombatPlayerSkillForecastView.cs`(예보 사건의 영향 칸·첫 경로·유닛별 끝 체력(사망·재구성 = 0, 바뀐 것만)·넉백·점유 칸, 목록 재사용), 표시 `Affected` 층(닫힌 테두리)·궤적 `CombatAttackTrajectory.ShowPath`(꺾인 선)·넉백 `CombatKnockbackPreview.ShowSkill(넉백, 점유 칸)`(그리기 본문 `AddKnockback`을 조준과 공유)·HUD `CombatUnitHudFeedback.ShowHealthPreview(목록)`, 조건부 문구 `ShowSkillForecastCondition`·`HideSkillForecastCondition`(행동 안내 채널, Q-SK3 A). 격발 = 남은 점 1이면 후보 칸 클릭, 0이면 그려진 영향 칸 안 클릭, 예보 전이면 격발 대기 → 그 점 목록 예보가 오면 제출. 결과 = 수락이면 끝, 거절이면 가용성부터.
- SK-d 사건 51 재생: 형식 표 줄·처리기 칸 `PlayerSkill`(맨 끝), 새 `Playback/Handlers/CombatPlayerSkillEventHandler.cs`(결과 시작 때 `CombatResult.PlayerSkill`로 외형 줄, 경로 있음 = 직선 투사체 연출만 받아 `CombatAttackStart.TryBegin`, 없음 = `TryBeginArea` + 영역 칸 강조), 공격 모양 `CombatAttackShape.ForPlayerSkill`(`SkillPath`·`SkillContacts` — 접촉 칸은 `DeliveryIndex` → `ImpactCells`), 새 `Presentation/Attack/CombatShotRoute.cs`(시전자 → 칸 가운데 꺾인 선, 접촉 진행률 = 누적 길이 비율), 꺾인 선 식 `CombatShotPath.FillPathLengths`·`PathPoint`·`PathTangent`, 풀 `FirePath`·투사체 진행 `FirePath`·맥락 `FirePath`, 직선 전략·직선 코드 모션의 경로 갈래, `CombatAttackStart.TryBeginArea`(클립 = 근접 조립으로 맞는 순간 모두 해제, 클립 없음 = 코드 모션), 소리 표 사건 51 = 시전자 공격 슬롯, `CombatCueSources.PlayerSkills`, 처리기 표·Presenter 생성 줄.
- SK-e 버튼: 새 `Input/ActionButtons/CombatSkillButton.cs`(보임·누름·켜짐·남은/한도·아이콘·이름·설명), 새 `Input/Interaction/CombatPlayerSkillAvailabilityTracker.cs`(행동 가능 여부 조회와 같은 꼴, 행위자 = 스킬 상태 메인), 버튼 상태 `CombatActionButtonState`·`CombatActionButtons`(튜토리얼 갈래 = `Locked`, 같음·해시에 스킬 칸 — 호진님 줄), 입력 표면·입력 분배·연결 입력 묶음, 넷째 입구 `CombatInputConnection.SkillPressed` → `ICombatActionDisplay.BindActions` → `CombatActionPanel.Bind`(현강님 줄), 행동 패널 스킬 칸(아이콘·남은 횟수·켜짐 표시·공용 Hover 패널 설명 + 남은 횟수 줄). 새 `.cs` 일곱의 `.meta`는 Unity 기본 형식으로 직접 만듦.

**계획과 다른 점**
- b.1: 선택 표시를 `ShowSkillCandidates`·`ShowSkillPoints` 둘 대신 `ShowSkillSelection` 하나로(후보와 점은 늘 같은 답에서 함께 바뀜).
- c.1: 예보 읽기는 입력 계약 폴더 `Input/Contracts/`(입력 소유자 → 표시 계약 방향을 지킴). c.4 HUD 인자는 튜플 대신 값 `CombatHealthPreview`.
- d.3·d.4(TP-13 정정): 지금 직선 공격을 "점 둘 경로"로 바꾸지 않고, 경로 비행은 풀의 따로 갈래 `FirePath`로 둠(지금 직선 탄 동작을 건드리지 않음). 경로 접촉은 `CombatProjectileContacts.CollectAlongPath` 대신 새 `CombatShotRoute`가 만든다(경로 점·접촉 진행률을 한 곳에서).
- d.5: 경로 없는 길은 "근접 꼴 조립" 대신 `CombatAttackStart.TryBeginArea` 하나 — 화약꽃 임시 연출의 Akimbo 원거리 클립이 탄을 쏘지 않도록 클립이면 근접 조립, 아니면 코드 모션.
- Q-SK4: 영역 칸별 큐 칸(`PlayerSkillCell`)은 아직 재생이 읽지 않는다(칸만, 비움).
- e.3(TP-15): `skillButton`은 아직 선택 참조(비어 있으면 스킬이 보일 때 경고 한 번, R 키로만). 프리팹 연결 뒤 필수로.
- b.2 "후보 밖 클릭 = 무시 + 소리": 소리는 맞는 슬롯이 없어(`UnitEvent`에 "안 됨" 소리 없음, 공격 조준도 조용히 무시) 소리 없이 무시한다.

**묶음 끝 검토(새 에이전트 하나, 읽기만)**: 컴파일 오류 찾지 못함(바뀐 서명·호출처, 계약 구현 누락, using·접근 수준, `in` 인자, 이름 충돌, `Assets/Tests` 영향 없음까지 대조). 높음 0·중간 2·낮음 8, 약 8.3.
- 반영 M1(설계 SK1 위반): 남은 점 1개에서 포인터가 후보 밖·필드 밖으로 나가면 그린 예보만 지우고 답 안 온 호버 예보 요청은 남아, 늦게 온 지나간 칸 예보가 포인터 없는 칸 위에 그려짐 → `LeaveHoverForecast`가 그 요청도 놓음(격발 대기 요청은 놓지 않음).
- 반영 M2(계획 Ub "결과 재생 중 진입 거절"과 다름): 키 진입 `CanEnter`가 재생 중을 보지 않아 재생 위에 후보·예보가 그려지고 격발이 조용히 무시되며, 같은 때 버튼은 꺼져 키와 버튼이 어긋남 → 진입 조건에 버튼 배경 조회와 같은 식 `CombatInputPolicy.CanQuerySkillAvailability`(재생 아님·처리 안 된 명령 없음)를 더함.
- 반영 L3(TP-9와 다름): 격발 대기 중 우클릭이 대기를 버리고 이미 확정한 점을 뺌 → 대기 중 우클릭은 받지 않음(R·버튼은 끝냄, TP-9 그대로).
- 반영 L5 넉백 미리보기 문서 주석이 다른 함수에 붙어 있던 것, L6 후보 색 툴팁(TP-1 정정 — 시전자 칸 열기 없음), L7 스킬 행위자 읽기·출처 네 칸 대조를 정적 함수 하나씩(`CombatPlayerSkillInteraction.ActorOf`·`IsSource`, 버튼 배경 조회가 부름).
- 기록만 L1: 마탄 표적 예산이 0이면 경로 없는 사건 51 + 직선 연출이라 `TryBeginArea`가 예외(F1). 지금 에셋 값(예산 1·관통 4)에서는 닿지 않음(§6.1 0.4와 같은 사실) → 천명님 확인 항목(SK-f f.2).
- 기록만 L2 → **11-17 풀림(조건부 문구를 없애서, §6.4)**: 0점 스킬(화약꽃) 명령이 거절되면 거절 안내 직후 가용성 → 예보가 다시 와 조건부 문구(시간 없는 안내)가 거절 문구를 수 프레임 안에 덮음. 고치려면 현강님 안내 클래스에 "시간 있는 안내가 떠 있는지"를 더해야 해 Unity 확인 뒤 결정.
- 기록만 L4(위 b.2), L7 나머지(경로 점 "칸 가운데 + 발사 높이" 반복이 궤적 `ShowPath`와 `CombatShotRoute`에 따로 — 첫 점 식은 공유), L8 선택 요청을 그때 물을 수 없으면 다시 시도하지 않음(지금 길에서는 모달·포기가 모드를 먼저 끝내 드묾).

**Unity 확인(Ub~Ue, 위 각 Phase 목록)**: 컴파일부터. Ue 버튼은 `Combat.unity`의 `CombatActionPanel`에 `skillButton`(+ 선택 `skillIcon`·`skillUsesText`·`skillActiveIndicator`)을 연결해야 보인다(사용자 에디터 작업, 그 전엔 R 키로만 — 경고 한 번).

**남은 것**: Unity 확인 Ub~Ue, `skillButton` 프리팹 연결 뒤 필수 참조로(e.3), 키 설정 일곱째 줄(SK-a), SK-f(폴더 배치 안내·공유 넷·설계 구현 메모). 커밋 때 새 `.cs` 일곱 + `.meta`는 바꾼 코드와 한 커밋(형식 표·처리기 표·입력 Factory가 서로 참조).

### 6.4 조건부 문구 없앰 (2026-11-17, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 계기: 사용자 마탄 확인 — 남은 점 하나에서 후보 칸에 올리면 "알려진 정보 기준 예상입니다" 팝업이 뜸 → "저 문구를 띄우는 이유가 없다". Q-SK3 A를 걷는다(문구 없음).
- 원인: 행동 안내 창구(`CombatActionNotice`)는 공용 안내 창(`UIId.Notice`, 확인창과 같은 창)을 쓰므로 한 줄이 아니라 화면 가운데 팝업이었고, 호버 예보마다 열고 후보를 떠날 때마다 닫아 깜빡였다.
- 바꾼 것: `CombatActionNotice`의 `ShowSkillForecastCondition`·`HideSkillForecastCondition` 삭제(현강님 클래스 — 우리가 SK-c에서 더한 두 입구를 걷기만 함, 공유 목록의 "조건부 문구 입구" 줄은 필요 없어짐), `CombatPlayerSkillInteraction.ApplyForecast`의 부르기 한 줄, `ClearForecast`는 `_feedback.ClearSkillForecast()`만.
- 함께 풀린 것: 묶음 끝 검토 L2(0점 스킬 거절 안내가 조건부 문구에 덮임).
- Unity 확인: 마탄·화약꽃 예보 때 팝업이 뜨지 않음, 스킬 거절 안내(같은 턴 재사용 등)는 그대로 보임.

### 6.5 마탄 점 사거리 확인 (2026-11-17, 코드 변경 없음)

- 사용자 확인: Uc 화약꽃까지 문제 없음, 조건부 문구 팝업 없음.
- 질문 "마탄은 사거리 제한이 없는데 후보 칸이 4칸까지인 이유": View는 사거리를 읽지 않고 Core 선택 답의 `NextCandidateCells`를 그대로 칠한다. 4칸은 Core 규칙 — 점마다 앞 점(첫 점은 시전자)에서 스나이퍼 공격 사거리(`Unit_Sniper.attackRange` 4, 모래폭풍·먼지 연막으로 줄어듦) 안(`MagicBulletCalculator.ValidatePoint` `PointOutOfRange`, Core 설계 M01~M04). 탄은 마지막 점 뒤 판 끝·벽까지 날아간다(제한이 없는 것은 비행 거리).
- 제한을 풀어도 View 구조 문제 없음(후보 칸이 길어질 뿐). Core 규칙 변경이라 천명님 설계·시험이 바뀜 → **사용자 "지금 그대로"**: Core 요청 쓰지 않음.

### 6.6 스킬 버튼은 메인을 골랐을 때만 누름 (2026-11-17, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 사용자 확인: Ud 마탄 재생·Ue 스킬 버튼까지 문제 없음 → 요청 "스킬 버튼은 메인 캐릭터 골랐을 때만 활성화".
- 바꾼 것: 처음엔 `CombatPlayerSkillAvailabilityTracker.ReadButton`에만 "고른 유닛(`CombatInputFacts.SelectedActorId`) = 스킬 행위자(메인)"를 더했다가, 사용자 "R도 맞추기"로 그 조건을 진입 판단 `CombatPlayerSkillInteraction.CanEnter` 한 곳으로 옮김(버튼은 `canEnter`로 받음 — 키와 버튼이 다시 같은 조건, 묶음 끝 검토 M2 규칙 유지). 버튼은 지금처럼 보이고(메인에 스킬이 묶여 있으면), 메인을 고르지 않았으면 누를 수 없는 모양 — 공격 버튼과 같은 꼴. 스킬 모드 중 끄기는 늘 누름, 호버 설명은 그대로.
- 정한 것(확인 대상): "활성화" = 누를 수 있음(숨김 아님). 메인을 고르지 않고 R을 누르면 조용히 아무 일 없음(공격 키 E와 같은 꼴, 스킬이 없을 때의 "스킬 없음" 안내는 그대로). 설계 P2(행위자 = 선택이 아니라 스킬 상태의 메인)는 행위자 출처로 그대로이고, 진입에 "그 메인을 골랐을 것"이 더해짐.
- Unity 확인: 메인 고름 → 버튼 누름·R 진입 가능, 다른 아군·적·빈 칸 고름 → 버튼 눌리지 않음·R 아무 일 없음, 스킬 모드 중 → 버튼·R로 끄기.

### 6.7 SK-f 문서·공유·묶음 끝 검토 (2026-11-17, 사용자 "SK-f 진행해", 코드는 검토 반영만 — 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

**문서**
- f.1 폴더 배치 안내: 갱신 줄(11-17), 표 다섯 칸(`Input/Interaction/` 스킬 모드·버튼 배경 조회, `Input/Contracts/` 스킬 표시 계약·예보 읽기, `Input/ActionButtons/` 스킬 버튼 값, `Playback/Handlers/` 사건 51 처리기, `Presentation/Attack/` 꺾인 경로), §2 규칙 줄 "플레이어 액티브 스킬".
- f.2 공유(모두 미전달): 현강님 구조 공유 §31(키 열거·`KeySettingUI` 6 보류·예전 저장 키 설정·행동 안내 계약/문구·행동 패널 넷째 칸·옆에 더한 줄), 호진님 구조 공유 §22(키 정의 줄·행동 버튼 튜토리얼 갈래/같음/해시·스킬 외형 표 넣는 법·옆에 더한 줄), 민형님 Factory 공유 §59(`ViewTableKey`·`ViewDataBaseFactory`·입력 Factory 생성 줄), 천명님 Core 전달 문서 §73(제목 갱신, 요청 없음, 확인 셋 — 사건 순서·접촉 칸 대조·표적 예산 0, 인계 §5 "예보 한계 문구"를 보이지 않기로 한 것).
- f.3 설계 §9 구현 메모(설계와 달라진 열두 줄).
- 입력 Factory 주석 한 줄("거절·조건부 안내" → "거절 안내", §6.4 뒤 낡음).

**묶음 끝 검토(새 에이전트 하나, 읽기만)**: 컴파일 오류 후보 없음(지운 조건부 문구 입구를 부르는 곳 없음, `Assets/Tests` 영향 없음), 높음·중간 0, 낮음 5, 7기준 평균 **8.5**(SSOT 8.5·SOLID 8.0·FW 9.5·독립성 8.5·불필요 8.0·C#/Unity 8.5·매 프레임 8.5). §6.6 동작 확인: `SelectedActorId`는 입력 사실 읽기 한 곳에서 채워지고 키·버튼 경로가 같은 읽기, 스킬 모드 중 선택은 바뀌지 않음(정보창 막기·UI 가림·병종 순환은 모드를 끝냄), 끄기는 늘 됨.
- 반영 L1: 메인이 아닌 유닛을 고르고 R을 누르면 진입 실패 전에 `LeaveMoveMode`가 그 유닛의 경로 초안을 지움(§6.6 뒤 흔한 경우) → Router `Request.Skill`이 `CanEnterSkill`이 참일 때만 경로를 취소("조용히 아무 일 없음"이 사실이 됨). 공격 E는 그대로(그쪽은 선택 유닛이 행위자라 실패가 드묾).
- 반영 L3: 스킬 모드가 구체 `CombatActionNotice` 대신 계약 `ICombatActionRejectionSink`를 받음(§6.4 뒤 `SkillRejected`만 씀, 공격 조준과 같은 꼴).
- 반영 L4: 낡은 주석 — `CombatInteraction` 클래스 요약·포인터 대상 둘·`CommandResolved`, Router 판정식 "둘 다" → "셋 다"·실시간 상태 목록, `CombatActionPanel.Bind` 요약에 스킬.
- 그대로 L2: 진입 검사 중복으로 보인 것 — `CanQuerySkillAvailability`는 입력 사실의 대기(`facts.HasPendingCommand`), 뒤 `CanPreview`는 Gate 대기(`_gate.HasPending`)를 봐 같은 검사가 아님. 버튼 쪽 튜토리얼 허용표 두 번은 공격 버튼과 같은 꼴(호진님 줄)이라 그대로.
- 그대로 L5: `CombatPlayerSkillForecastView.cs`에 값 `CombatHealthPreview`가 함께 있음(예보 읽기만 쓰는 값).
- 문서 불일치 셋 반영: 이 계획 상태 줄, §73·현강님 §31·설계 머리말에 "§6.6 뒤 변경은 Unity 확인 대기"(§6.4는 사용자가 확인), 호진님 §22 튜토리얼 문장(제한 중 허용표에 없으면 막힘이 맞음). 그 밖의 줄 번호·이름·Core 사실 인용은 모두 맞음(검토 확인).

**Unity 확인(남은 것)**: §6.4(예보 때 팝업 없음 — 확인됨), §6.6(메인 고름 → R·버튼 진입, 다른 유닛 고름 → R 아무 일 없음·그 유닛의 경로 초안 그대로·버튼 눌리지 않음), §6.7 L1.

**남은 것**: `KeySettingUI` 6 → 7(사용자가 키 설정 프리팹 일곱째 줄을 넣은 뒤, 한 커밋), `skillButton` 필수 참조(e.3 — `Combat.unity`에 연결돼 있음), 공유 넷 전달(사용자), 평가 아티팩트 차수(사용자 지시 대기). 사용자 메모 "현재 스킬은 Attack Type으로 분류"는 반영 방식 답 대기(설계 §9).

### 6.8 키 설정 화면 스킬 줄·스킬 분류 기록 (2026-11-17, 사용자 "KeySettingUI 작업까지 진행하자, Attack Type 분류는 문서에만", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

**키 설정 화면(SK-a 남은 것, TP-16)**
- 프리팹 `Prefabs/UI/GlobalUICanvas.prefab`(Boot 씬 인스턴스, 씬 덮어쓰기 없음 확인): `KeySetting` 아래 되돌리기 줄(`Return` → `Button` → `ButtonKey`, GameObject 셋·컴포넌트 열)을 복사해 새 줄 `Skill`(라벨 "스킬", 키 글자 자리 "R" — 실행 때 현재 키로 바뀜)을 `Return` 바로 뒤 자식으로 넣고, `KeySettingUI.keyButtons` 끝에 `action: 6`(`Skill`) 줄을 더함. 새 fileID 13개는 무작위(파일 안 중복 없음), 파일 안 참조 끊김 0, 줄 끝 CRLF 그대로(스크립트로 확인).
- 배치: 줄은 손으로 놓은 자리(레이아웃 그룹 없음)라, "전투 조작" 묶음을 위로 올리고 간격을 100 → 90으로 좁혀 아래 "아티팩트 조작" 묶음은 그대로 둠 — 머리글 375 → 425, 전투 시작 250 → 310, 턴 종료 150 → 220, 공격 모드 50 → 130, 되돌리기 -50 → 40, 스킬 -50. 임시 배치라 보기 나쁘면 에디터에서 옮기면 됨(현강님 UI).
- 코드 `KeySettingUI.cs:66`(현강님 줄): 고정 수 6 → 7(+ 문구), 근거 주석 한 줄. 프리팹과 한 커밋이어야 한다(한쪽만이면 부트의 `BindButtons`가 예외로 멈춤).
- Unity 확인: 컴파일·부트 통과, 옵션 → 키 설정에 "스킬" 줄이 보이고 R로 나옴, 스킬 키를 바꾸면 전투에서 그 키로 스킬 모드, 되돌리기·턴 종료 등과 같은 키로 바꾸면 "중복" 안내, 줄 배치가 겹치지 않음.

**스킬 분류(문서만)**: 지금 스킬(화약꽃·마탄)은 모두 공격형(Attack Type) — 설계 §9에 적음. 외형 표에 종류 칸은 두지 않고, 공격이 아닌 스킬이 생기면 그때 설계한다.

**공유**: 현강님 구조 공유 §31에 덧붙임(프리팹 줄·배치·고정 수), 호진님 §22 외형 표 항목에 "공격형 스킬만" 한 줄.
