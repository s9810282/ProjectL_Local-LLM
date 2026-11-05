# 스테이지 1 보스 View 활성화 설계 (2026-10-15)

- 작성: View 작업, 2026-10-15. 사용자 요청: "Core 보스 확인" 뒤 "일단 3번까지 다 할거야" — 에셋 등록, 보스 스테이지 설정, View 표시를 모두 한다.
- Core 계약 원본: [보스 View 인계](../handoffs/2026-10-13-boss-summoning-view-handoff.md), 보스 스테이지 설계 (Git history: `2026-10-14-boss-stage-objective-design.md`) §5, 보스 스테이지 계획 (Git history: `2026-10-14-boss-stage-objective-plan.md`) Phase 7(T9, View 담당).
- Task·실행 기록: [보스 활성화 계획](../plans/2026-10-15-stage1-boss-view-activation-plan.md). 이 문서는 결정과 계약만 적는다.
- 기준 코드: `feature/Yonggeun/Stage` HEAD `88bf9bf8`. Core 보스 커밋(`3f5e2bbe`·`50487702`·`6d345ca6`·`c1b6fbd9`)은 이미 병합돼 있다.

## 1. 사용자 결정 (2026-10-15)

| 번호 | 결정 |
|---|---|
| B1 | 시작 적 구성은 **보스 단독**(Core 제안 그대로). 본부와 EnemyLoadout 참조를 빼고 Unit_Stage1Boss 하나를 (4, 8)·R0·승리 대상(적 모델은 View가 180도 돌려 플레이어를 보게 하므로 생성 적과 같은 R0, 2026-10-15 정정)으로 둔다. 적은 소환으로만 늘어난다. |
| B2 | 목표 마커와 포효 카운터는 **기존 유닛 HUD**를 쓴다. 새 UI 프리팹을 만들지 않는다. |
| B3 | 포효·소환은 **최소 연출**이다. 새 아트 없이 기존 프리셋을 쓴다. |
| B4 | 진행은 **설계 → Task 계획 → Phase별 구현**, Phase마다 멈추고 보고한다. 워크시트는 만들지 않는다(Claude 직접 구현). |

## 2. 범위

- **한다:** 보스·포효 지정 방식·보스 외형 등록, 보스 스테이지 SO 값, 목표 마커, 포효 카운터, 지정 보스 승리 문구, 포효·소환 최소 연출과 간격.
- **하지 않는다:** Core(`RuntimeSystem/InGameSystem/CoreSystem/`) 코드 변경(예외: D6c 포효 범위 칸, 사용자 명시 지시), 새 아트·VFX·프리팹, 화면 상단 보스 바, 전투 로그(`CombatLogUI`, 현강님 UI)의 포효 줄, 공유 에셋 `EnemyLoadout_5`·`UnitRoster_1` 수정, Audio 정책.
- **원칙(Core 인계 §0):** View는 Buffer만 읽는다. 목표 보스는 `Objective.TargetEntityId`로만 고른다. 키 18·스테이지 이름·Boss Category로 분기하지 않는다. 포효 임계값 5를 하드코딩하지 않는다. 포효 발생은 카운터가 아니라 `Roared` 사건으로 안다.

## 3. 결정

### D1. 에셋 등록

| 에셋 | 처리 |
|---|---|
| `InGameGroup/Unit/Item/Unit_Stage1Boss.asset` | `UnitDataTable`에 등록. 값은 Core가 확정한 그대로 둔다 |
| `InGameGroup/Targeting/Item/TargetingMethod_Stage1BossRoar.asset`(key 5) | 지정 방식 표에 등록 |
| `ViewGroup/UnitVisual/Item/UnitVisual_StageBoss1.asset`(key 18) | `UnitVisualDataTable`에 등록. 공격 연출 `CombatAttack_Melee`, 반응 연출 `CombatUnitReaction_Default`를 연결한다(모든 캐릭터가 같은 반응을 공유하는 기존 규칙) |
| TraitVisual(Necromancy 80, Roar 90) | 보스 HUD 상태 칸에 이름·설명을 보이려고 추가한다. 아이콘 스프라이트가 없으면 비워 두고(조회는 `TryGet`, 아이콘 null 허용), 아이콘은 후속 아트 요청으로 남긴다 |

GUID는 유지한다. 새 에셋은 .meta와 함께 만든다.

### D2. 보스 스테이지 `Combat_Boss.asset`

`objectiveKind = DesignatedBossDefeated`, `headquarters = null`, `enemyLoadout = null`, `initialEnemyPlacements = [Unit_Stage1Boss, (4, 8), R180, isVictoryTarget = true]`. 필드·환경·경제·타임라인·노드 연결은 그대로 둔다. 본부가 없는 전투를 기존 검증(`CombatStageDetail.Validate`·Core 준비)이 받는지는 Phase 1에서 실제 카탈로그 시험으로 확인한다. 막히면 구현을 멈추고 묻는다.

### D3. 목표 마커

- 원본은 `CombatStateData.Current.CommandState.Objective`다(이미 `CombatDisplayValues.State`로 표시에 들어온다).
- `Kind == DesignatedBossDefeated`이고 `TargetEntityId`가 필드에 있으면, 그 캐릭터가 점유한 칸에 **새 강조 층 `ObjectiveTarget`**을 둔다. 강조 상태의 원본은 기존과 같이 `FieldHighlightState` 한 곳이다.
- 색은 `FieldView`의 직렬화 색 하나(`objectiveTargetColor`)로 정한다. 우선순위는 호버 > 영향 > 범위 > 행위자 > **목표** > 정적. 표시 규칙은 같은 상태 층인 현재 행위자와 같다: GridOutline 모드에서 평소에도 테두리를 그리고, 행동 중 판정(`IsActing`, 격자 켜기)에는 넣지 않는다(Phase 2 구현에서 문구를 분명히 함).
- 같은 대상의 유닛 HUD 상태 칸에 "격파 목표" 항목을 더한다(설명 칸). 아이콘이 없으면 비워 둔다.
- 일반 전투(`EnemyHeadquartersDestroyed`)에서는 층을 비운다. 목표가 죽거나 대기 영역(`EnemyWaiting`, 낮)이면 표시하지 않는다.

### D4. 포효 카운터

- 유닛 HUD 상태 칸은 지금 TraitVisual 아이콘만 쓰고 설명은 비어 있다(`FieldUnitPresenter.RenderUnitHud`).
- `RoarTraitStatusSnapshot`이면 설명을 `"포효 {HitCount}/{HitThreshold}"`로 채운다. 이름은 TraitVisual의 표시 이름을 앞에 둔다. 값은 Buffer 스냅샷 그대로이며 View가 세지 않는다.
- 상태 칸 설명은 HUD에서 마우스를 올리거나 고정할 때 보인다. **단, 지금 `HUD.prefab`의 `UnitHUD.descriptionPanel`·`descriptionText`가 비어 있어 설명이 화면에 나오지 않는다**(2026-10-15 Phase 2 확인 중 발견, 설계 때 놓침). 프리팹 연결은 현강님 UI 영역이라 사용자 결정 대기. D3의 "격파 목표" 설명도 같다. **아이콘 위 상시 숫자**는 `UnitHUD`(현강님 UI) 변경이 필요해 이번 범위 밖이다. 필요하면 후속으로 현강님께 요청한다.

### D5. 지정 보스 승리 문구

`CombatHud` 완료 문구에 `StageOutcomeReason.DesignatedBossDefeated` → "보스 격파 · 결과 확인"을 더한다(기존 포기 문구와 같은 방식). 승패 판정은 그대로 `Outcome`을 읽는다.

### D6. 포효 최소 연출

- R0의 "사실만" 처리(`CombatSceneEventPresenter.CanPresent`에 포함, 연출 없이 완료)를 바꾼다.
- `CombatRoarEvent.Attack`이 성공 결과면 기존 공격 재생(`CombatAttackPlayback`)을 **공격 결과(`AttackResult`) 입력**으로 한 번 재생한다. 보스 외형의 공격 연출(D1의 Melee)이 동작을 정한다. 같은 동안 결과의 영향 칸을 **재생 강조 층**(아래 D6a)으로 보인다. 재생이 끝나면 다음 사건(피해·칸별 `Moved`)이 기존 순서대로 이어진다(2026-10-17부터는 박치기 타격 순간부터 이어진다 — D6b 변경 메모, 히트 타이밍 설계 §6 Q1).
- ~~`Attack == null`(범위 안 대상 없음)이면 보스에 반응 동작만 짧게 보이고 완료한다.~~ **변경(사용자 결정 2026-10-15 "동작 없이 간격만"):** 반응 동작은 피해 사건(맞은 방향)을 입력으로 받아 피해 없이 부르려면 가짜 사건이 필요하다. 그래서 `Attack == null`이거나 공격 연출을 시작하지 못하면 동작 없이 보스 칸을 재생 강조로 보이고 적 행동 간격(`enemyActionGapSeconds`)만큼 머문 뒤 완료한다.
- 효과음은 정하지 않는다(Audio 로직 제외, 리팩토링 R4 결정). Ballista 외형의 포효는 관통 후속 사건 대응이 공격 사건에 묶여 있어 공격 연출 없이 위 사실 경로를 탄다.
- 재생 계약은 그대로다. 사건 하나가 시작 → 완료 콜백 한 번이며 취소·해제 규칙(D6·D9 기존)을 따른다. 공격 재생에 사건 형식 대신 공격 결과를 넘기는 입구를 하나 더할 뿐, 연출 종류별 분기는 기존 외형 표가 정한다.

### D7. 소환 최소 연출

- R0에서 `Created` 사실을 그 시점에 보이게 한 처리는 유지한다.
- 그 뒤 소환된 칸을 재생 강조 층(D6a)으로 보이고 적 행동 간격만큼 머문다. 적 연출 간격(`CombatEnemyPresentationPacing`)에서 소환은 소환자의 행동 하나로 센다. 보스 턴(이동·공격 없이 소환만)이 눈에 보이게 한다.
- 등장 칸의 효과(지뢰 등)·사망은 뒤따르는 일반 사건이 그대로 표시한다.

### D6a. 재생 강조 층 (사용자 결정 2026-10-15 "연출 전용 층 추가")

- 설계 원안은 `Affected` 층이었으나, 그 층은 공격 조준 호버(`CombatAttackFeedback`)가 쓰고 연출 중에도 미리보기가 허용돼 서로 지운다. 그래서 재생 전용 층 `FieldHighlightLayer.PresentationHighlight`(1 << 7)를 둔다. 쓰는 쪽은 `CombatSceneEventPresenter` 하나(Factory가 `FieldView.SetLayerCells`로 연결한 콜백)다.
- 색은 영향 칸 색을 그대로 쓴다(`ColorOf` → `FieldHighlightColor.Affected`, 새 색 설정 없음). 우선순위는 예정 경로 위·입력 영향 칸 아래다. GridOutline에서는 상태 층처럼 평소에도 그리고 격자를 켜지 않는다.
- 켜는 때: 포효·소환 사건 시작. 끄는 때: 다음 행동(공격·포효·소환·턴 사건, 자발적 이동) 시작, 결과의 최종 사실 표시(`Refresh`), 취소·해제. 피해·사망·넉백 같은 결과 사건 동안은 유지한다.
- **소환은 예외(D6b):** 등장 동작이 끝나면 바로 끈다.

### D6b. 소환 등장·포효 커지기 동작 (사용자 요청 2026-10-15, Phase 3b)

사용자 요청 원문: "소환: 해당 칸 주황색 하이라이트 → 유닛이 바닥에서부터 Y축으로 회전하면서 등장 → 등장 후 칸 하이라이트 종료 / 포효: melee 동작 전 영향 칸 강조 → Melee 동작, 지금은 테스트를 위해 Melee 동작을 하기 전 보스의 scale을 2.5까지 상승했다가 돌아오기".

- **소환:**
  1. 생성 칸을 재생 강조로 보인다.
  2. 유닛을 최종 자리에서 `깊이`만큼 아래에 두고 `대기` 동안 기다린다.
  3. `시간` 동안 최종 자리까지 올라오며 Y축으로 `바퀴 수`만큼 돈다.
  4. 끝나면 원래 자세로 돌리고 강조를 끈 뒤 완료한다.
  - 앞서 정한 "행동 간격만큼 머묾"은 이 동작으로 바뀐다.
- **포효:**
  1. 영향 칸을 재생 강조로 보인다. 대상이 없으면 Core가 준 포효 범위 칸(`CombatRoarEvent.AreaCells`)을, 그것도 없으면 보스 칸을 보인다(D6c).
  2. 보스를 `커지기 시간`의 앞 절반 동안 현재 크기 × `배율`까지 키운다.
  3. 공격 결과가 있으면 **커진 채로** Melee 공격 연출을 한 번 재생하고 끝날 때까지 기다린다.
  4. 뒤 절반 동안 원래 크기로 되돌린다.
  5. 강조를 끄고 포효 사건을 완료한다. 피해·넉백 사건은 그 뒤에 이어진다.
  - **변경(2026-10-17, 사용자 결정 "포효 넉백도 타격 순서에 맞춰, 강조는 포효가 끝난뒤, 작아지기와 겹쳐도 됨"):** 3의 Melee 타격 순간이 히트다. 피해·넉백은 그때부터 차례로 시작해 Melee 복귀·4의 작아지기와 겹친다. 5의 강조 끄기는 작아지기가 끝난 뒤 그대로이고, 소환·턴은 포효가 모두 끝날 때까지 기다린다. 원본은 [히트 타이밍 설계](2026-10-16-combat-hit-timing-design.md) §6 Q1·Q2다.
  - **변경(사용자 요청 2026-10-15 "셀 하이라이트 → 사이즈 커짐 → 공격 모션 → 사이즈 작아짐 → 하이라이트 꺼짐"):** 처음에는 커졌다 돌아온 뒤 Melee를 재생하고 강조를 피해·넉백까지 유지했다. 이를 위 순서로 바꿨다.
  - ~~대상이 없으면 커졌다가 작아지고 강조를 끈다.~~ **변경(사용자 결정 2026-10-15 "대상 없어도 공격 모션까지 똑같이"):** 대상 유무와 관계없이 같은 순서다.
  - **강조 뒤 대기(사용자 요청 2026-10-15 "셀 하이라이트에서 커지기로 가는 시간 빼두기"):** 새 설정 `roarDelaySeconds`(기본 0.2초).
  - **공격 모션 방향(사용자 보고 "보스 Melee가 뒤→앞이 아니라 앞→뒤"):**
    - 원인: 포효는 전체(All) 지정이라 Core 결과의 `AttackDirection`이 기본값 R0(+Y, 플레이어 반대쪽)이다. 공격 연출은 이 방향으로 목표를 고르므로(주 칸이 없으면 R0 앞의 가장 먼 영향 칸, 없으면 제자리 → 방향 = R0), 적 보스가 플레이어 반대쪽을 향해 뒤로 빼고 나가 거꾸로 보였다.
    - 일반 적은 해당 없다. 단일 지정은 주 칸, 방향 지정은 고른 방향을 쓰므로 맞다. 운영 Melee 적 Demon(단일)·Gravekeeper(방향)를 확인했다.
    - 그래서 포효 공격 모션은 결과 없이 **보스가 지금 보는 방향**으로 재생한다(`CombatAttackPlayback.TryBeginFacing`, Linear·Melee만). 대상이 없어도 같은 동작을 쓴다.
  - 크기는 자세 대여와 따로인 **크기 대여**(`ICombatUnitScale`, `FieldUnitView.BeginPresentationScale`)로 바꾼다. 그래서 커진 채로 공격 자세(위치·회전)를 재생할 수 있다. 처음 3b에서 자세에 넣었던 크기 배율(`ICombatUnitMotionPose`)은 이 대여로 옮겼다.
- **배율 기준(사용자 답 "현재 크기가 2일 텐데 2.5 내지는 3까지"):** 보스 프리팹 루트 크기 2에 필드 유닛 크기 0.5가 곱해져 실제로는 1로 그려진다. 그래서 값은 **현재 크기에 대한 배율**로 둔다. 기본 1.25가 프리팹 기준 2.5이고, 1.5로 두면 3이다. HUD는 부모 크기를 상쇄하므로 같이 커지지 않는다.
- **설정 위치(사용자 결정):** `CombatSceneRoot` 씬 설정 "보스 능력 연출(테스트 값)". 모든 소환·포효가 같은 값을 쓴다. 보스마다 다르게 해야 하면 그때 유닛 외형 표로 옮긴다.
  - 기본값: 소환 대기 0.2초, 깊이 1.5, 0.6초 동안 1바퀴, 포효 대기 0.2초, 커지기+작아지기 0.4초, 배율 1.25.
- **구조:**
  - 위치·회전은 기존 자세 대여(`ICombatUnitPose`, 소환 등장은 `BeginMotionPose`), 크기는 새 크기 대여(`ICombatUnitScale`)로 나눈다. 기존 `ICombatUnitPose`·공격·반응 자세는 바꾸지 않는다(ISP, 기존 시험 가짜 유지).
  - 원본은 각각 `FieldUnitView`의 대여 하나다. 같은 ID·버전 규칙을 쓰고, 재대여·비활성 때 되돌린다.
  - 동작 재생은 새 `CombatAbilityMotionPlayback`이 맡는다. 사건 Presenter가 소유하고, 재생 세대를 읽고, 취소·해제 규칙을 따른다. 공격·걸음 재생과 같은 방식이다.

### D6c. 대상 없는 포효의 범위 칸 (사용자 요청·지시 2026-10-15)

- 요청: "포효 시 대상이 없더라도 바닥 색 표시 해줘". View가 Fan 모양을 계산하면 Core 규칙 복제(SSOT 위반, P6)라서, Core가 칸을 게시하도록 바꿨다. 사용자가 "Core도 제가 고치기"로 명시적으로 지시했다(CLAUDE.md 예외).
- Core 계약: `CombatRoarEvent.AreaCells`(표시 전용). 직접 공격 영향 칸 식을 `CombatAttackDelivery.DirectAffectedCells` 하나로 모았고, 빈 대상 실패에도 같은 식을 쓴다. 기록은 [데이터 경계 인계 §5.34](../handoffs/2026-09-24-view-core-data-boundary-handoff.md).
- View 순서: `Attack.AffectedCells` → `AreaCells` → 보스 칸.

### D8. 넉백 N칸

칸별 `Moved`는 기존 이동 재생이 순서대로 처리한다고 본다. 같은 유닛의 연속 `Moved` 3개를 재생하는 회귀 시험을 Phase 3에서 확인하고, 문제가 있을 때만 고친다.

- **Phase 3 확인 결과:** 문제가 있었다. 포효 앞에서 자발적 경로를 끝내는 경계가 공격 사건에만 있어, 같은 유닛이 경로 뒤 포효로 넉백되면 넉백 칸을 앞 경로의 연속으로 보았다(`OrderedCombatPresentation.IsLastMovementStep`, 간격의 `IsVoluntaryMovement`). 포효를 공격과 같은 행동 경계로 더해 고쳤다. Ballista 화살 비행 중 포효 시작도 공격처럼 막는다(`CombatAttackPlayback.CanBegin`).

## 4. 원칙 점검

- **SSOT:** 목표 대상은 Buffer `Objective` 한 곳, 강조 상태는 `FieldHighlightState` 한 곳, 포효 횟수·임계값은 스냅샷 한 곳, 연출 종류는 UnitVisual 표 한 곳이다. View 쪽 사본을 만들지 않는다.
- **OCP:** 다음 보스는 SO·표 값만으로 같은 경로를 탄다(키·이름 분기 없음). 포효·소환 재생은 "소환자·포효 유닛"에 일반적이다.
- **ISP·DIP:** 새 표시 의도는 기존 역할 계약에 멤버를 더하는 방식이다(재평가 N5·N6의 비용이 그대로 든다). 공격 재생 입구 추가는 사건 Presenter 안이다.
- **Framework:** 새 서비스·ServiceKey·AddModule 없음. DB 표 등록은 기존 Catalog 경로.

## 5. 위험과 확인

| 위험 | 확인 |
|---|---|
| 본부 없는 전투가 기존 검증·테스트에 막힘 | Phase 1 실제 카탈로그 준비 시험. 막히면 멈추고 묻는다 |
| 등록 뒤 실제 에셋 테스트(`DataBaseAssetTests`, `CombatUnitReactionAssetTests` 등)가 새 유닛을 기대하지 않음 | Phase 1에서 전체 실행·갱신. 누락 참조를 억누르지 않는다 |
| TraitVisual 아이콘 없음 | 아이콘 null 허용 확인, 아트는 후속 |
| 공격 재생이 `CombatAttackEvent`에 묶여 있음 | Phase 3에서 결과 입력 입구를 더하고 기존 시험을 그대로 통과시킨다 |
| 실제 보스 전투 재생 | Phase 4에서 사용자가 확인(진입·소환·포효·목표 처치·완료) |
