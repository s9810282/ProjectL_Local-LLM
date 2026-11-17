# 전투 플레이어 액티브 스킬 View 대응 설계 (초안)

- 날짜: 2026-11-16
- 상태: 설계 2판. Q-SK1·Q-SK2(2026-11-16 사용자 결정)·Q-SK3~Q-SK6(같은 날 "권장안대로" = 모두 A) 결정 완료, 설계에서 정한 것 P1~P17은 확인 대상. **SK-a~SK-f 구현 완료(2026-11-17, 컴파일 미확인, 사용자 Unity 확인 Ua~Ue 문제 없음 ·조건부 문구 없앰도 확인됨, 그 뒤 메인을 골랐을 때만 진입·검토 반영은 Unity 확인 대기) — 구현에서 달라진 것은 §9.** Task 계획 [player-skill-view-plan](../plans/2026-11-16-combat-player-skill-view-plan.md)(SK-0~SK-f, TP-1~TP-16).
- 원본(Core): [Core → View 인계](../handoffs/2026-11-15-player-active-skills-core-view-handoff.md), [Core 설계](2026-11-15-player-active-skills-core-design.md). 천명님 `1b76f69db`·`28d29b67e`.
- 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `SI/` = `…/ViewSystem/StageInput/`.

## 0. 사용자 결정 (2026-11-16)

| 번호 | 결정 |
|---|---|
| Q-AS1 | 스킬 키는 **R**. 행동 패널에 스킬 버튼도 둔다(버튼 프리팹·씬 연결은 사용자). |
| Q-AS2 | 스킬마다 따로 대응하지 않고 **일관된 구조 하나**로 짠다. |
| Q-AS3 | 예보는 영향 칸·실제 경로 선·예상 체력·조건부 문구 한 줄(→ 조건부 문구는 2026-11-17 없앰, Q-SK3). **마탄은 점을 찍을 때마다 그 점에 따른 범위를 보인다.** |
| Q-AS4 | 새 View DB 표 `PlayerSkillVisual`(스킬·강화별 아이콘·이름·설명). 공격 연출은 **일단 기존 공격 묶음 SO를 참조**하고 나중에 바꾼다. |
| Q-SK1 | 이동 되돌리기는 **Q 키**로 옮긴다(스킬은 R). |
| Q-SK2 | 확정은 **좌클릭**. 화약꽃처럼 사용자가 범위를 정할 필요가 없으면 **영역 안 아무 칸이나** 좌클릭하면 격발한다. 마탄처럼 점을 고르면 **좌클릭마다 점을 찍고, 모든 점을 찍으면 격발**한다. |

## 1. 확인 사실

### 1.1 Core가 주는 것

- 조회 셋(요청 번호는 기존 Buffer 하나를 공유): `ForPlayerSkillAvailability(actor)`(11), `ForPlayerSkillSelection(actor, points)`(12), `ForPlayerSkillForecast(actor, points)`(13). 답은 `CombatPreviewResult.SkillAvailability / SkillSelection / SkillForecast` 중 하나.
  - 가용성: `RemainingUses`, `StageUseLimit`, `UsedThisTurn`, `CanUse`, `FailureReason`, `SkillKey`, `UpgradeKey`, `Source`.
  - 선택: `SelectedPoints`, `RequiredPointCount`, `PrefixIsValid`, `IsComplete`, `NextCandidateCells`(불가·완료면 빈 목록), `FailureReason`.
  - 예보: `CanUse`, `Coverage`(수락이면 늘 `KnownInformationConditional`), `Prediction`(`PlayerSkillResult`, 거절이면 null), `SelectedPoints`.
- 명령: `CombatViewCommandData.ForPlayerSkill(actor, points, forecast.Source)`(16). Source가 낡으면 `SourceChanged`·`BattleChanged`로 거절되고 사용 횟수는 줄지 않는다.
- 결과: 바깥 사건 목록에 새 사건 `CombatPlayerSkillAttackEvent`(51)가 폭발·투사체 하나마다 그 결과 앞에 들어간다(`PowderBlossomCalculator`·`MagicBulletCalculator`가 `Insert(0, …)`). 사건 안 `PlayerSkillAttackResult`: `ActorId`, `AttackOrdinal`, `AttackGroup`, `AffectedCells`, `ProjectilePath`(실제로 지난 칸, 시전자 칸 제외), `IncomingDirections`·`ImpactCells`(접촉 순번 `DeliveryIndex`별), `Hits`, `Knockbacks`. 체력·사망·기절·넉백 걸음·연쇄 공격은 기존 사건으로 온다. 스킬 키·강화 키는 사건이 아니라 `CombatResult.PlayerSkill`에 있다.
- 스킬 상태(행위자·남은 횟수): `CombatStateData.CommandState.PlayerSkill`. 쓸 수 있는지는 가용성 답이 원본이다.

### 1.2 지금 View (코드 대조, 2026-11-16)

- 스킬 입력·재생·표시는 없다. `PlayerSkill`을 쓰는 곳은 기절 출처 문구(`V/Display/Unit/CombatStatusDescriptionFormatter.cs`) 하나뿐.
- 사건 51은 형식 표(`V/Playback/Ordering/CombatEventTypeTable.cs`)에 없어 `OrderedCombatPresentation`이 경고 한 번 뒤 건너뛴다. 출처가 열리지 않아 피해·사망은 그 자리에서 바로 재생되고, 넉백 걸음은 장벽이 된다. 끝 사실은 결과 확인 전 `Refresh`가 맞춘다(멈추지 않음).
- `CombatInputPolicy.StageOf(명령)`·`StageOf(미리보기)`에 스킬 종류가 없어, 지금 스킬을 묻거나 제출하면 `ArgumentOutOfRangeException`.
- **R 키는 비어 있지 않다.** 입력 맵의 `Return`(R)은 옛 복귀 대신 **이동 되돌리기(`UndoMove`)**를 낸다(`V/Input/CombatInputRouter.cs:166-169` → `CombatMovementUndoInput`). 행동 패널 셋째 버튼(`returnButton`)도 같은 길이다. 키 설정 화면 줄 `KeyBindingAction.Return`(현강님 GlobalSystem 열거)·`SI/StageInputKeyBindings.cs:16`(호진님 줄)이 이 키를 쓴다. 시험 참조는 없다.
- **Q 키는 전투에서 비어 있다**(2판 확인). `Combat` 맵 키보드는 Space(턴 종료)·E(공격 모드)·R(되돌리기)·Tab, `Deployment`는 G·A, `CombatCamera`는 WASD·방향키·Z·C다. Q는 `Artifact` 맵(로비 아티팩트 회전)만 쓰고, 그 키 설정 충돌 그룹은 1이라(`ArtifactInputKeyBindings`) 스테이지 그룹 0과 겹치지 않는다. 키 설정 화면의 되돌리기 줄 글자는 이미 "되돌리기"다(`GlobalUICanvas.prefab`).
- 키 설정 화면 `KeySettingUI`는 키 버튼을 **정확히 6개**로 검사한다(`GlobalSystem/UISystem/Screens/Global/KeySettingUI.cs:66-67`, 현강님 줄). 키 겹침 검사는 정의 목록(`StageInputKeyBindings` 등)만 보므로, 정의에 없는 키는 겹쳐도 거르지 않는다(`KeyBindingService.cs:254`).
- 스테이지 입력 수신 계약 `IStageInputReceiver`의 구현은 `StageInputReceiverSlot`·`V/Input/CombatInputReceiver` 둘이다.
- 재쓸 수 있는 것: 공격 조준 입력 소유자 꼴(`AttackTargetingInteraction`), 한 슬롯 배경 요청 규칙(`CombatPreviewSlotRule`), 강조 층 표(`FieldHighlightLayers`), 여러 점 궤적 선(`FieldTrajectoryPath` — 꺾인 선 지원), HUD 예상 체력(`ICombatUnitHudTarget.SetHealthPreview(int?)`), 공격 재생(`CombatAttackStart`·`CombatHitGate`·`ICombatSourceLifetime`·`CombatContactRelease` — 출처 사건 형식을 가리지 않음), 동시 묶음(피해 근원 열쇠), 결과 소비·확인(사건 형식을 보지 않음).
- 사건 형식에 묶인 것: `CombatAttackEventHandler`(형 변환), `CombatAttackShape.Of`, `CombatAttackSource.Of(AttackResult)`, `CombatAttackTarget`, 조기 공격 묶음, `CombatEventSounds`(공격 소리 갈래). 투사체는 한 구간만 난다(`CombatProjectilePool.Flight` From/To, `CombatShotPath.ArcPoint` 한 번의 Lerp).

## 2. 원칙

**View는 스킬 종류를 모른다.** 무엇을 고를 수 있는지·다 골랐는지·쓸 수 있는지는 Core 답(필요 점 수·후보 칸·완료·실패 이유)으로, 어떻게 재생할지는 사건 51의 모양(투사체 경로가 있나)으로 정한다. 스킬 키는 외형 값(아이콘·이름·설명·공격 연출 SO)을 찾을 때만 쓴다. 그래서 Core에 새 스킬·강화가 생겨도 모양이 같으면 View 코드는 바뀌지 않고 외형 줄 하나만 더한다.

## 3. 설계

### SK1 스킬 모드 — 입력 소유자 하나

새 전경 입력 소유자 `V/Input/Interaction/CombatPlayerSkillInteraction`(공격 조준과 같은 꼴, `CombatInputOwner` 어댑터). 모든 스킬이 같은 단계를 지난다.

```text
진입(R·버튼) → 가용성 → (CanUse) 선택(점 목록) ─┬→ 남은 점 2개 이상: 후보 칸 표시, 후보 좌클릭 = 점 추가 → 선택(점 목록) …
                                               ├→ 남은 점 1개: 후보 칸 호버 = 그 칸까지 넣은 예보, 후보 좌클릭 = 그 칸까지 넣어 격발
                                               └→ 완료(남은 점 0): 예보 표시, 영역 안 좌클릭 = 격발
R·버튼을 다시 = 모드 끝, 우클릭 = 마지막 점 빼기(점이 없으면 모드 끝)
```

- **남은 점 = 선택 답의 `RequiredPointCount − SelectedPoints.Count`.** 어느 갈래로 갈지는 이 수 하나로 정하고 스킬 종류는 보지 않는다(Q-SK2를 종류 무관하게 옮긴 것). 화약꽃은 0 → 완료 갈래, 마탄은 A·B(왜곡이면 C)마다 줄어 마지막 점에서 1 → 격발 갈래로 간다.
- 점이 없는 스킬(화약꽃)도 **선택 요청을 한 번 거친다**(`[]` → Core가 완료라고 답함 → 바로 예보). 점 수를 View가 알지 않기 위해서다(P1).
- 행위자는 선택한 유닛이 아니라 스킬 상태의 `ActorId`(메인)다. 메인이 필드에 없거나 못 쓰면 가용성 답의 실패 이유로 안내한다(P2).
- 좌클릭(Q-SK2):
  - 남은 점 2개 이상: 지금 후보 목록에 있는 칸이면 점을 더하고 선택을 다시 묻는다. 후보 밖은 무시(소리만).
  - 남은 점 1개: 후보 칸이면 **(고른 점 + 그 칸)으로 격발**한다. 후보 밖은 무시.
  - 완료: 그려진 영향 칸(P13) 안이면 **(고른 점)으로 격발**한다. 영역 밖은 무시.
- 격발 = 그 점 목록의 지금 예보가 `CanUse`면 그 예보의 `Source`를 실어 명령을 제출한다. 예보가 아직 안 왔거나 다른 점 목록의 것이면 그 점 목록으로 예보를 묻고, 답이 오면 그때 제출한다(**격발 대기**, P14). 대기 중에는 다른 클릭·호버 요청을 받지 않는다. 답이 거절이면 안내하고 대기를 버린다.
- 호버(P15): 남은 점 1개일 때만 후보 칸에 올린 칸으로 예보를 묻는다(공격 조준 호버와 같은 꼴, 칸이 바뀔 때만). 후보 밖이면 예보를 지우고 후보·선 표시로 돌아간다. 남은 점 2개 이상이면 묻지 않고 호버 칸만 강조한다 — 점을 찍으면 그 점에 따른 다음 후보가 나온다(Q-AS3). 완료 갈래는 예보가 포인터와 무관해 한 번만 묻는다.
- 우클릭: 마지막 점 하나를 빼고 선택을 다시 묻는다. 점이 없으면 모드를 끝낸다(모든 스킬 같음). R·스킬 버튼을 다시 누르면 언제든 모드를 끝낸다.
- 낡은 답 거르기: 요청 번호, 답 종류, Source(전투·턴·`QueryRevision`·행위자)를 지금 Buffer 값과, 점 목록을 지금 고른 점과 대조한다. 결과 뒤(`AfterResult`)에는 수락이면 모드를 끝내고, 거절이면 가용성부터 다시 묻는다(P8). 제출은 예보의 `Source`를 그대로 싣고, 낡아서 거절되면 같은 길로 다시 시작한다.
- 다른 입력과 관계: 진입은 `BeginForeground`로 경로·공격 조준을 정리한다(공격 모드 중 R이면 스킬 모드로 넘어감). 포인터 대상에 `Skill`을 더하고 `ResolvePointerTarget`이 스킬 모드면 그쪽으로 보낸다. 미리보기 슬롯은 전경 요청이 덮어쓰고, 배경 요청은 지금 `CombatPreviewSlotRule`대로 기다린다.
- 정책: `StageOf` 셋(미리보기 11·12·13)과 명령 16을 전투 국면으로. 튜토리얼 제한 중에는 허용 목록이 그대로라 스킬은 막힌다(P9).
- 거절 안내: 실패 이유 → 기존 행동 안내 창구(`CombatActionNotice`)에 스킬 이유 문구(쓸 수 없음 9종: 턴 아님·메인 아님·행동 불가·스킬 없음·무장 해제·기절·횟수 소진·이번 턴 사용·전투 없음). 점 오류 7종은 후보로 거르므로 안내하지 않는다.

### SK2 선택·예보 표시

| 단계 | 그리는 것 | 자리 |
|---|---|---|
| 선택 중 | 다음 후보 칸(`NextCandidateCells`) = "이 점에 따른 범위", 고른 점, 시전자 → 고른 점을 잇는 선, 호버 칸 | 새 강조 층 `SkillCandidate`·`SkillPoint`, 궤적 캔버스(꺾인 선), 호버 칸 |
| 예보(마지막 점 호버·완료) | 영향 칸(예보 안 사건 51들의 `AffectedCells` 합), 실제 경로 선(`ProjectilePath`, 시전자부터), 대상 예상 체력, 넉백 화살표(사건 51의 `Knockbacks`) (조건부 문구는 2026-11-17 없앰) | `Affected` 층, 궤적 캔버스, HUD 예상 체력, 넉백 미리보기 캔버스 |

- 마탄: A를 찍으면 B 후보(A를 지나 꺾이는 방향의 칸), B를 찍으면(왜곡이면) C 후보가 나온다. 마지막 점의 후보에 올리면 그 칸까지 넣은 예보(실제로 날아갈 경로·예상 체력)가 그려지고, 누르면 격발한다. 화약꽃은 들어가자마자 폭발 범위가 그려지고, 그 범위 안 아무 칸이나 누르면 격발한다. 둘 다 같은 표시 코드다.
- 마지막 점 호버의 예보가 보이는 동안 후보 층은 그대로 두고 그 위에 영향 칸을 그린다(다른 후보로 옮겨 갈 수 있다는 것이 보이게).
- 예보 읽기는 한 곳 `V/Display/Feedback/CombatPlayerSkillForecastView`(가칭): 예보의 사건 목록을 차례로 읽어 영향 칸·경로·대상별 마지막 체력을 모은다(P4). 연쇄 폭발까지 포함한 끝 체력이다.
- HUD 예상 체력은 `CombatUnitHudFeedback`에 `(대상, 끝 체력)` 목록을 받는 입구를 더한다(공격 미리보기 행을 만들지 않음).
- 색은 `FieldColorsData`에 두 칸(`skillCandidateColor`·`skillPointColor`)을 더한다. 영향 칸·궤적·넉백은 지금 색.

### SK3 재생 — 사건 51

- 형식 표 한 줄: `Row<CombatPlayerSkillAttackEvent>(Slot.PlayerSkill, isActionStart: true, actor: ActorId, attackSource: new CombatAttackSource(ActorId, AttackGroup, null), pacing: Attack)`. Core가 사건 51을 그 결과 앞에 넣고 접촉 근원을 검사하므로, Gate·이동 후속·동시 묶음은 그대로 쓴다.
- 처리기 하나 `V/Playback/Handlers/CombatPlayerSkillEventHandler`: 공격 모양을 `CombatAttackShape.ForPlayerSkill(event)`로 만들고(방향 = 첫 경로 구간 방향, 없으면 시전자 방향), 외형 줄의 공격 연출 SO를 덮어쓰기로 넘겨 `CombatAttackStart.TryBegin`을 부른다. 스킬 키·강화 키는 결과 시작 때 `CombatResult.PlayerSkill`에서 읽어 둔다(P7).
- **모양이 연출 길을 정한다**(P5):
  - 투사체 경로가 있음 → **경로 비행**: 직선 투사체가 시전자 몸에서 `ProjectilePath` 칸 가운데를 차례로 지나 마지막 칸에서 사라진다. 대상마다 그 대상의 접촉 칸(`Hits` 접촉 순번 → `ImpactCells`)에 닿는 순간 그 대상의 피격을 연다(PD-a의 `CombatContactRelease` 그대로, 마지막 접촉 = 모두 해제). 공격 연출 SO가 직선 투사체 묶음이 아니면 예외.
  - 투사체 경로가 없음 → **영역**: 맞는 순간(코드 모션의 맞음, 클립이면 `ClipEvent` + 피격 지연) 모든 대상을 한 번에 연다. 재생 중 영향 칸을 재생 강조 층에 칠한다(`CollectHighlight`, 포효와 같은 길). 클립이 원거리 묶음이어도 이 길에서는 투사체를 쏘지 않는다.
- 경로 비행은 투사체를 여러 점 선으로 일반화한다: `CombatShotPath`에 누적 길이 위치·방향, 풀 `Flight`에 점 배열, `CombatProjectileFlights.FirePath`·맥락 `FirePath`, 접촉 진행률 = 그 접촉 칸까지의 누적 길이 비율. 한 구간 발사는 점 둘인 경로로 같은 코드를 지난다(지금 직선 공격 동작 같음).
- 화약꽃 연쇄 폭발은 폭발마다 사건 51이 따로 오고 공격 그룹이 달라, 지금 차례 규칙대로 하나씩 재생된다. 연쇄 공격(`KillChainAttack`)은 지금처럼 그 사건으로 재생된다.
- 피격 젖힘 방향은 지금처럼 피해 근원 칸(시전자) 기준이다. 꺾인 진입 방향(`IncomingDirections`)을 쓰는 것은 다음 후보(P6).
- 소리: 공격 연출 SO의 큐(소리 시점)와 시전자 소리 프리셋을 쓴다. 사건 소리 표는 바꾸지 않는다.

### SK4 행동 패널·HUD

- 행동 패널 넷째 버튼 `skillButton`(slot 3): 보임 = 메인에 스킬이 묶여 있음, 누를 수 있음 = 가용성 `CanUse`, 켜짐 = 스킬 모드 중. 누르면 R과 같은 입구(토글: 모드 밖이면 진입, 모드 중이면 끝). 격발은 버튼이 아니라 필드 좌클릭이다(Q-SK2). 버튼에 아이콘과 남은 횟수(`RemainingUses/StageUseLimit`). 호버 정보는 Q-SK5.
- 가용성 배경 조회 `V/Input/Interaction/CombatPlayerSkillAvailabilityTracker`(가칭): 판(`QueryRevision`)이 바뀔 때마다 한 번, `CombatPreviewSlotRule`로 슬롯이 비었을 때 묻는다(행동 가능 여부 조회와 같은 꼴). 남은 횟수·이번 턴 사용을 View가 스킬 상태에서 계산하지 않는다(P3).
- 버튼 상태는 `CombatActionButtonState`·`CombatActionButtons`에 스킬 칸 둘(`CanSkill`·`IsSkillActive`)을 더해 지금 흐름(`CombatInputSurfaces.Sync` → `CombatActionPanel.SetAvailable`)으로 민다.

### SK5 외형 표 `PlayerSkillVisual`

- 새 View DB 표(특성 외형 표 꼴): 줄 = `PlayerSkillKey` 하나. 칸: 아이콘, 이름, 설명, 공격 연출 SO(`CombatAttackPresentationSO`, 필수), 영역 사건 칸별 큐(Q-SK4), 강화 덮어쓰기 목록(강화 키, 이름, 설명, 공격 연출 SO — 비우면 기본 줄 값).
- 조회: (스킬, 강화) → 강화 줄이 있으면 그 값, 없으면 기본 줄. 어느 강화가 어느 스킬 것인지는 Core 지식이라 표 검사는 중복·`None`·빈 SO만 본다(P10).
- 처음 에셋 두 줄(임시): 화약꽃 = `CombatAttack_Melee`(영역은 투사체 없는 묶음, P11), 마탄 = `CombatAttack_Linear`(Sniper가 쓰는 직선 투사체 묶음). 강화 줄은 이름·설명만.
- 연결 줄(공유): `ViewTableKey`(민형님), `ViewDataBase`·`ViewDataBaseFactory`, `ViewGroup.asset`, `CombatDisplayTables`. 처리기까지는 `CombatCueSources`로 넘겨 사건 재생 Factory 줄을 건드리지 않는다.

### SK6 키 — 되돌리기 Q, 스킬 R (Q-SK1)

- **되돌리기**: 지금 액션 `Return`을 그대로 두고 `Combat` 맵 바인딩만 `<Keyboard>/r` → `<Keyboard>/q`(입력 에셋 한 줄). 받는 길(`CombatInputController` → `RequestReturn` → Router `Return` → `CombatMovementUndoInput`)·행동 패널 `returnButton`·키 설정 줄 `KeyBindingAction.Return`과 글자 "되돌리기"는 그대로이고, 화면에 보이는 키만 Q가 된다.
- **스킬**: 새 액션 `Skill`(Button, `<Keyboard>/r`, 새 GUID 둘)을 `Combat` 맵에. 받는 길은 한 줄씩: `SI/StageInputNames.cs`(이름), `SI/CombatInputController.cs`(`Press(Skill, RequestSkill)`), 수신 계약 `SI/IStageInputReceiver.RequestSkill` → 구현 둘 `SI/StageInputReceiverSlot`·`V/Input/CombatInputReceiver`, `V/Input/CombatInputRouter` 요청 `Skill`(열거 맨 끝) → 스킬 모드 입구(토글). 배치 국면에는 액션이 없다(`Combat` 맵만).
- **키 설정 정의**: 열거 `KeyBindingAction.Skill`(맨 끝 — 화면 줄에 직렬화된 정수가 안 바뀜, 현강님 GlobalSystem 열거)과 `SI/StageInputKeyBindings` 줄 하나(충돌 그룹 0, 호진님 줄). 정의가 있어야 되돌리기를 R로 바꾸려 할 때 스킬과 겹친다고 거른다(정의 없는 키는 겹침 검사에서 빠짐, P16).
- **키 설정 화면 줄**: Q-SK6.
- 다른 작업자 줄은 연결 줄만 고치고 공유한다(P12).

## 4. 범위 밖

- 전투 밖 강화 선택 화면(`TryApplyPlayerSkillUpgrade`) — StageSelect·로비 쪽, 획득 방식 미정.
- 로비 메인 선택(천명님 `0d1f7c3c0`) — 로비 영역.
- 스킬 전용 클립·이펙트·소리 — 지금은 기존 공격 묶음 SO(Q-AS4).

## 5. 정할 것 (모두 정함 — Q-SK3~Q-SK6은 2026-11-16 사용자 "권장안대로" = 권장 A)

| 번호 | 질문 | 권장 |
|---|---|---|
| ~~Q-SK1~~ | 정함(2026-11-16): 되돌리기는 Q(§0, SK6) | — |
| ~~Q-SK2~~ | 정함(2026-11-16): 좌클릭 — 영역 안 아무 칸 / 점마다 찍고 마지막 점에서 격발(§0, SK1) | — |
| Q-SK3 | 조건부 문구("알려진 정보 기준 예상입니다")를 어디에 | ~~A: 예보가 보이는 동안 행동 안내 창구에 한 줄~~ / B: 스킬 버튼 호버 설명에만 → **2026-11-17 바뀜: 문구 없음**(사용자 "띄우는 이유가 없다" — 행동 안내 창구가 공용 안내 팝업이라 호버마다 팝업이 깜빡임, 계획 §6.4) |
| Q-SK4 | 영역 사건의 칸별 이펙트 | **A: 외형 줄에 칸별 큐 칸만 두고 이번에는 비움**(강조만) / B: 칸 없음, 나중에 |
| Q-SK5 | 스킬 버튼 정보 | **A: 아이콘·남은 횟수 + 호버 시 이름·설명**(공용 Hover 패널, UnitInfo 특성 칸과 같은 방식) / B: 아이콘·남은 횟수만 |
| Q-SK6 | 키 설정 화면에 스킬 키 줄을 둘까(2판에서 생김 — 화면이 버튼 6개를 고정 검사) | **A: 이번에**(`KeySettingUI`의 고정 수 6 → 7 한 줄은 현강님 줄을 연결 줄로 고치고 공유, `GlobalUICanvas.prefab` 줄 하나는 사용자) / B: 나중에(스킬 키는 R로 고정, 정의 줄만 둬 겹침은 거름) |

## 6. 설계에서 정한 것 (확인 대상)

- P1 점이 없는 스킬도 선택 요청을 한 번 거친다(점 수는 Core 답).
- P2 행위자 = 스킬 상태의 `ActorId`. 선택 유닛과 무관. → 2026-11-17: 행위자 출처는 그대로이고, 진입(키·버튼)은 그 메인을 골랐을 때만(사용자, §9).
- P3 버튼의 쓸 수 있음·남은 횟수는 가용성 배경 조회 답만 쓴다(Core 규칙 사본 없음).
- P4 예상 체력 = 예보 사건 목록의 대상별 마지막 체력(연쇄 포함).
- P5 사건 51 모양이 재생 길을 정한다(경로 있음 = 경로 비행, 없음 = 영역). 공격 연출 SO가 모양에 안 맞으면 예외.
- P6 피격 젖힘은 지금처럼 시전자 기준(꺾인 방향은 다음 후보).
- P7 스킬·강화 키는 결과의 `PlayerSkill`에서 읽는다.
- P8 결과 뒤: 수락이면 모드 끝, 거절이면 가용성부터 다시.
- P9 튜토리얼 제한 중에는 스킬 금지(허용 목록 무변경).
- P10 외형 표 검사는 중복·`None`·빈 SO만(강화 소속은 Core 지식).
- P11 화약꽃 임시 연출 = `CombatAttack_Melee`, 마탄 = `CombatAttack_Linear`.
- P12 키 사슬은 다른 작업자 줄(키 설정 열거·키 정의·UI 프리팹 줄)을 연결 줄만 고치고 공유한다.
- P13 "영역 안" = 지금 그려진 영향 칸(연쇄 폭발 칸 포함, 보이는 범위 = 누를 수 있는 범위).
- P14 격발 대기: 그 점 목록의 예보가 아직 없으면 묻고 답이 오면 제출한다(누른 것을 버리지 않음). 대기 중 다른 입력은 받지 않는다.
- P15 예보를 호버로 묻는 것은 남은 점 1개일 때만(칸이 바뀔 때만). 남은 점 2개 이상은 점을 찍어야 다음 범위가 나온다.
- P16 되돌리기 키 Q는 로비 아티팩트 회전 Q와 다른 맵·다른 충돌 그룹이라 겹침이 아니다(두 화면은 함께 켜지지 않음).
- P17 남은 점 수가 0·1·2 이상 중 어느 것인지만 보고 갈래를 고른다(스킬 종류·강화를 보지 않음).

## 7. Phase(안)

| Phase | 내용 | Unity 확인 |
|---|---|---|
| SK-a | 정책 `StageOf` 넷·외형 표 `PlayerSkillVisual`(코드·에셋 두 줄)·키 사슬(되돌리기 Q, 스킬 액션 R, 키 설정 정의)·행동 패널 스킬 칸(버튼 연결 전에는 없어도 되게) | 컴파일, DB 생성, Q가 되돌리기, R이 아무 일도 안 함, 되돌리기 버튼 그대로, 키 설정 화면에 되돌리기 = Q |
| SK-b | 스킬 모드 입력(진입·끝, 가용성 → 선택, 남은 점 2개 이상의 점 추가, 우클릭 빼기), 선택 중 표시(후보·점·선·호버 칸) | 마탄 A·B(·C) 후보, 우클릭 빼기·끝, R 토글, 화약꽃 바로 완료 상태 |
| SK-c | 예보(마지막 점 호버·완료)와 표시(영향 칸·경로·예상 체력·넉백·문구), 격발(좌클릭·격발 대기), 거절 안내 | 예보와 실제가 같은 칸, 영역 밖·후보 밖 클릭 무시, 빠른 클릭도 격발, 낡은 답 거름, 사용 뒤 버튼 꺼짐 |
| SK-d | 사건 51 재생(형식 표·처리기·경로 비행·영역) | 마탄 꺾인 비행·접촉마다 피격·심판 밀림, 화약꽃 폭발·연쇄, 지금 직선 공격 그대로 |
| SK-e | 버튼 상태·가용성 배경 조회·호버 정보 | 남은 횟수, 이번 턴 사용, 기절·무장 해제에 꺼짐 |
| SK-f | 문서·공유(현강님 키 설정·UI, 호진님 키 정의, 민형님 연결 줄, 천명님 확인)·묶음 끝 검토 | — |

## 8. 7기준 점검(초안)

- SSOT: 후보·완료·쓸 수 있음·남은 횟수·예보 결과는 모두 Core 답 하나. 좌클릭 갈래(점 추가·마지막 점 격발·영역 격발)는 남은 점 수 하나로, 재생 길 판단은 처리기 한 곳(사건 모양), 외형은 표 한 곳.
- SOLID·독립성: 스킬 모드 하나·처리기 하나. 새 스킬은 외형 줄만, 새 모양이 생길 때만 처리기 갈래 하나. 경로 비행은 기존 투사체의 일반화라 직선 공격도 같은 코드를 지난다(→ 구현은 일반화 대신 풀의 따로 갈래, §9).
- FW 계약: 새 서비스 등록 없음. 연결 줄(표 키·Factory·키 설정)만.
- 불필요/우회: 0점 스킬도 같은 단계를 지나 요청 하나가 더 들지만, 점 수를 View가 갖지 않기 위한 값이다.

## 9. 구현 메모 (2026-11-17)

실행 기록 원본은 [계획](../plans/2026-11-16-combat-player-skill-view-plan.md) §6.1~§6.6이다. 설계와 달라진 것만 적는다.

| 설계 | 구현 | 까닭 |
|---|---|---|
| SK1·P2 행위자는 선택과 무관 | 행위자 출처는 스킬 상태 `ActorId` 그대로, **진입(R·버튼)은 그 메인을 골랐을 때만**(`CombatPlayerSkillInteraction.CanEnter` 한 곳, 버튼은 그 판단을 받음) | 사용자 "메인 캐릭터 골랐을 때만" → "R도 맞추기"(계획 §6.6). 고르지 않고 R을 누르면 조용히 아무 일 없음(공격 E와 같음) |
| SK1 진입 조건 | 버튼 배경 조회와 같은 안정 상태(재생 아님·처리 안 된 명령 없음)를 진입에도 | 묶음 끝 검토 M2 — 재생 중 R로 들어가 키와 버튼이 어긋남 |
| SK2·Q-SK3 A 조건부 문구 | **없음** | 행동 안내 창구가 공용 안내 팝업이라 호버마다 깜빡임, 사용자 "띄울 이유가 없다"(계획 §6.4). Core 인계 §5의 "한계를 UI에 보이라"와 다름 → Core 전달 문서 §73에 적음 |
| SK2 선택 표시 입구 둘(후보·점) | `ShowSkillSelection` 하나 | 후보와 점은 늘 같은 답에서 함께 바뀜 |
| SK2 예보 읽기 자리 | `Input/Contracts/CombatPlayerSkillForecastView` | 입력 소유자 → 표시 계약 방향을 지킴 |
| SK3·§8 경로 비행 = 직선 투사체의 일반화 | 풀의 따로 갈래 `FirePath`(새 `CombatShotRoute`가 꺾인 점·접촉 진행률을 만듦), 지금 직선 탄은 무변경 | 지금 직선 공격 동작을 건드리지 않음(계획 TP-13 정정) |
| SK3 경로 없는 길 = 근접 꼴 조립 | `CombatAttackStart.TryBeginArea` 하나 — 클립이면 근접 조립(맞는 순간 모두 해제), 없으면 코드 모션 | 화약꽃 임시 연출이 Akimbo 원거리 클립이라 탄을 쏘지 않게 |
| SK5·Q-SK4 영역 칸별 큐 | 칸 `PlayerSkillCell`만 있고 재생은 아직 안 읽음(비움) | Q-SK4 A "칸만, 이번엔 비움" |
| SK4 스킬 버튼 필수 참조 | 아직 선택 참조(비면 경고 한 번, R로만) | 프리팹 연결 뒤 필수로(계획 e.3) |
| SK6 키 설정 화면 7줄 | 처음엔 `KeySettingUI` 고정 수 6 그대로 → 11-17 프리팹 일곱째 줄(Skill)과 함께 7(계획 §6.8) | 6줄 프리팹에 7을 요구하면 부트가 멈춤 — 프리팹 줄과 한 커밋으로(계획 TP-16) |
| SK1 후보 밖 클릭 = 무시 + 소리 | 소리 없이 무시 | 맞는 효과음 슬롯이 없음(공격 조준도 조용히 무시) |
| P9 튜토리얼 중 스킬 금지 | 설명 갈래 = 보이되 못 누름(`Locked`), 전투 갈래 = 튜토리얼 허용표(`AllowsTutorialPreview(…, SkillAvailability)`)도 봄 | 허용 목록 무변경 그대로, 행동 버튼의 다른 칸과 같은 꼴 |

사용자 질문(11-17): 마탄 점 사거리 4칸은 Core 규칙(M01~M04)이고 View 구조 문제 없음 → 사용자 "지금 그대로"(계획 §6.5). **스킬 분류(2026-11-17 사용자 "Attack Type 분류는 문서에만"):** 지금 구현된 스킬(화약꽃·마탄)은 모두 **공격형(Attack Type)**이다. View 구조가 이 분류를 전제로 한다 — 사건 51은 공격 사건(`CombatPlayerSkillAttackEvent`)이고, 외형 줄의 공격 연출 SO가 필수이며, 처리기는 공격 시작(`CombatAttackStart`)으로 재생하고, 예보는 공격 미리보기와 같은 그리기(영향 칸·궤적·예상 체력·넉백)를 쓴다. 외형 표에 종류 칸은 두지 않는다(코드 변경 없음). 공격이 아닌 스킬(버프·이동·소환 등)이 생기면 그때 Core 사건 형식과 함께 종류 칸·처리기 갈래를 설계한다.

남은 것: 묶음 끝 검토 기록만 L1(마탄 표적 예산 0이면 경로 없는 사건 + 직선 연출 → `TryBeginArea` 예외, 지금 에셋은 닿지 않음 — 천명님 확인 §73)·L4·L7 나머지·L8, `skillButton` 필수로. 키 설정 일곱째 줄은 11-17에 넣음(계획 §6.8).
