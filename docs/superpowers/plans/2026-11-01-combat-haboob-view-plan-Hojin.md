# 하붑 전투 View 계획

- 작성자: 호진(이호진)
- 상태: HB-a 완료(Ua 문제 없음 — 사용자, §7.1), HB-b 완료(2026-11-02 재생 괜찮음 — 사용자, §7.2), HB-c 코드·문서 완료(2026-11-02, 컴파일 미확인, Uc 대기, §7.3). Q-HP1은 A로 정함(2026-11-02 사용자). 구름 = 칸마다 `Smoke` 이펙트(2026-11-02 사용자, §1.1). 턴 주기는 Core 그대로(2026-11-02 사용자, Q-HP2). 화면 값은 재생 뒤 정한다(§2.1). 구름 이동 HB-d 완료(2026-11-02 사용자 "그대로, HB-d", 연기 흘리기로 고친 뒤 재생 괜찮음 — 사용자, §1.2·§7.6·§7.7).
- 설계: [하붑 View 설계](../specs/2026-11-01-combat-haboob-view-design-Hojin.md)
- 기준: [Core → View 인계](../handoffs/2026-11-01-tornado-rockfall-haboob-core-view-handoff.md) §2·§6·§8·§9.5
- 앞뒤 순서: [토네이도](2026-11-01-combat-tornado-view-plan-Hojin.md) → [낙석](2026-11-01-combat-rockfall-view-plan-Hojin.md) → 하붑

## 0 진행 규칙

- Phase가 끝나면 멈추고 보고한다. 커밋·push는 사용자가 한다.
- 새 시험을 만들지 않고 MSBuild를 돌리지 않는다. 보고에 "컴파일 미확인"을 적는다. 컴파일·재생 확인은 사용자가 Unity에서 한다.
- Core는 고치지 않는다. 공유 파일을 고치면 Phase HB-c에서 담당자 공유 문서에 적는다.
- 작업 트리의 다른 변경은 건드리지 않는다.

착수 조건: 낙석 RF-a~RF-d와 커밋을 먼저 끝낸다. 사건 형식 표·처리기 표·필드 창구·표시 Factory 둘·표 연결 줄·`CombatEnvironmentText`가 같은 파일이다. 착수 때 낙석이 바꾼 서명 셋(처리기 표 `Create`, 필드 창구 생성자, `CombatEnvironmentText.Build`)을 다시 읽는다. 2026-11-02 확인: RF-a~RF-c는 커밋됐고 RF-d는 안 끝났다. HB-a·HB-b는 RF-d와 파일이 거의 겹치지 않지만 **Hc.2는 Rd.2(`Build` 인자 줄이기) 뒤다.**

코드로 확인한 기존 시험 영향은 없다. `Assets/Tests`에 `CombatFieldEventPorts`, `CombatEventHandlerTable.Create`, `CombatEnvironmentText`, `CombatDisplayTables`, `AttackTargetingInteraction`을 만들거나 `IViewQuery`를 구현하는 시험이 지금 없다.

## 1 계획에서 정한 것(확인 대상)

| ID | 정한 것 | 이유 | 설계와 |
|---|---|---|---|
| HP-1 | 구름 자리: `Cells`의 바깥 사각형에서 가장 작은 칸과 가장 큰 칸의 `CellCenterToLocal` 평균. 높이는 있는 `mapper.SurfaceHeightOf(cells, cells[0])`. 여기에 위치 보정(필드 로컬)을 더한다. 회전은 `RotationToWorld(R0)` × 프리팹 회전 | 토네이도와 달리 옮기는 전환이 없어 바닥 교체로 높이가 바뀌어도 튀지 않는다. 그래서 타일 바탕이 아니라 설계대로 칸 표면을 쓴다 | 구체화 |
| HP-2 | 배율: 프리팹 배율 × (가로 칸 수 ÷ 기준, 1, 세로 칸 수 ÷ 기준). 축마다 따로이고 기준 칸 수 칸은 하나다. 칸 크기는 곱하지 않는다 | 맞춤 모양은 정사각형이 아닐 수 있다. 프리팹을 실제 칸 크기로 만드는 것은 토네이도 TP-5와 같다 | 구체화 |
| HP-3 | 경고 인스턴스는 구름과 같은 자리·배율이고 `Phase == RelocationWarning`일 때 켠다. 프리팹이 비면 인스턴스를 만들지 않는다 | 다음 위치가 없어 현재 범위에만 둘 수 있다(인계 §6.1) | 구체화 |
| HP-4 | 이동 이펙트는 `EffectType` 칸 대신 큐 입력 `relocationCues`로 받는다. 새 큐 칸 `CombatCueEvent.HaboobRelocation = 15`(시점 Start, 자리 EventCell, 낙석 13·14 다음). 자리는 `Final.Cells`이고 칸마다 항목 하나다. 사건 형식 표의 순간 칸은 칸 하나만 가져서 쓰지 않고, 처리기가 `CombatMomentCuePlayback.PlayAccent`를 부른다. `CombatCuePoints.ForCells`를 더한다 | 자리 보정·지연·끝이 큐에 이미 있어 규칙이 두 벌이 되지 않는다(낙석 RP-3과 같음). 칸 여럿을 받는 길은 특성 발동이 이미 쓴다 | **다름**(§3.4는 `EffectType` 칸) |
| HP-5 | 기본 큐는 비운다. 먼지 이펙트가 `EffectType`에 없어서다. 소리도 슬롯 대기라 이동은 구름이 바로 옮겨지는 것만 보인다. 이펙트가 오면 칸에 넣기만 한다 | 설계의 "Empty면 없음"과 같다. 안 어울리는 있는 이펙트를 임시로 넣지 않는다 | 같음 |
| HP-6 | 필드 창구에 좁은 계약 `ICombatHaboobField` 필수 인자 하나를 더한다. 멤버는 `Settings`, `Show(state, mapper)` 둘이다. 이펙트는 표시가 아니라 처리기가 내므로 설계의 `PlayRelocation`은 두지 않는다 | 토네이도 TP-3·낙석 RP-4와 같다. 이펙트 창구를 표시에 또 넘기지 않는다 | **다름**(§3.3의 입구 하나 뺌) |
| HP-7 | 사건 줄은 `overlapsUnitRuns`를 켜지 않는다. 같은 턴에 토네이도 유닛이 공중이면 착지 뒤 구름이 옮겨진다 | 토네이도 → 철도 → 하붑 → 낙석의 Core 순서를 지킨다(인계 §2.3). 하붑은 유닛을 건드리지 않아 짧게 기다린다 | 구체화 |
| HP-8 | 소리는 Relocation 사건에 처리기가 한 번 낸다. 경고 소리는 없다. 슬롯은 연결 대기(null) | 칸마다 내면 겹쳐 커진다. 설계는 Relocation 갈래만 있다 | 구체화 |
| HP-9 | 파일 자리: `Table/Haboob/`, 조회 `HaboobVisualTable.Config`, 에셋 `DataBaseSO/ViewGroup/HaboobVisual/`, 프리팹 `Prefabs/Combat/Haboob/`. 표시는 `FieldHaboobPresenter(field.CellsRoot, tables.Haboob)`로 만든다 | `ThornFieldVisual`과 같은 길이다 | 구체화 |
| HP-10 | 임시 프리팹: 루트 아래 납작한 상자 **셋**(가운데 4×2, 위·아래 2×1)으로 겹치지 않게 만든다. 칸 간격 1 기준, 두께 0.1, 위치 보정 y 0.6, URP Unlit 투명, 모래색 알파 0.35, 그림자 끔, 레이어 20, 충돌체 없음. 값은 재생에서 고친다 | 설계대로 둘을 겹치면 반투명이 겹친 가운데 2×2만 진해진다. 레이어 20은 낮·밤 카메라에 모두 보인다 | **다름**(Q-HB1은 상자 둘) |
| HP-11 | 호버 설명은 표의 이름·설명을 쓰고 `CombatEnvironmentText.Build`가 `HaboobGimmickState.Cells`에 든 칸마다 같은 설명을 보인다. 문구 초안: 이름 "먼지구름", 설명 "공격자와 지정한 칸이 모두 구름 안이거나 모두 밖이어야 칸을 지정해 공격할 수 있습니다." | 정적 모래폭풍(사거리 감소)과 다른 기믹이고, 투사체가 막힌다고 쓰지 않는다(인계 §6.1·§6.3) | 구체화 |
| HP-12 | 이동 뒤 다시 그리기(설계 §3.5 셋째 줄)는 코드를 고치지 않는다. 공격 모드는 `AfterResult`가 범위를 다시 요청하고, 유닛 호버는 재생 중 `Suspend` 뒤 다시 고르고, 환경 호버는 결과 순번으로 캐시를 버린다. 재생 확인만 한다 | 코드로 확인했다. 설계가 "확인하고 옛 응답이 남으면 고친다"고 한 것의 확인 결과다 | 구체화 |
| HP-13 | 켜기는 사용자가 `Test`에서 한다(`haboob.enabled` 지금 0, 칸 간격은 2026-11-02 `4219e1a1d`로 1.5) | 토네이도 TP-11·낙석 RP-14와 같다 | 같음 |
| HP-14 | 경고 프리팹은 설계대로 비워 둔다. 경고 갈래는 사용자가 확인 때 아무 프리팹이나 잠시 넣어 본다 | 넣지 않으면 경고 켜기·끄기를 볼 수 없다 | 같음 |

### 1.1 2026-11-02 바뀐 것: 구름 = 칸마다 Smoke 이펙트

사용자 "임시 구름 프리팹이 아니라 Smoke 이펙트를 하붑 칸마다 하나씩". **HP-1·HP-2·HP-3·HP-4·HP-5·HP-6·HP-10·HP-14는 이 절이 대신한다.** HP-7·HP-8·HP-9(프리팹 폴더 빼고)·HP-11~HP-13은 그대로다.

| ID | 정한 것 | 이유 |
|---|---|---|
| HP-15 | 구름은 `Cells` 칸마다 `Smoke` 이펙트 하나다. 칸이 곧 외형이라 맞춤 모양도 저절로 맞고, 기준 칸 수·배율·바깥 사각형 계산이 없다 | 사용자 지시. 설계 §6의 "맞춤 모양이면 외형이 어긋남"이 없어진다 |
| HP-16 | `EffectType.Smoke`를 열거 끝에 더하고(값 16), `RenderGroup/EffectTable/Items/Smoke.asset`을 만들어 표에 한 줄 넣는다. 풀 최대 32 | 지금 등록이 없다. 사라지는 칸의 입자가 최대 5초 남아, 12칸이 바로 이동하면 잠깐 24개가 된다(기본 16은 모자람). `FallingRocks`와 같은 길이다 |
| HP-17 | 표시 `FieldHaboobPresenter(field, settings, effects)`가 칸 → 손잡이 표 하나를 가지고 `Show(cells, mapper)`로 맞춘다. 살아 있는 칸은 그대로, 없는 칸은 `Play(Smoke, 자리, CombatEffectPlacement.Stay.AsHeld())`, 빠진 칸은 `Recall`. 밖에서 끝났으면(`StopAll`) 다음 `Show`가 다시 낸다 | `Smoke`는 Looping(5초)이라 스스로 안 끝난다. 유지 표시 부품과 같은 규칙이지만 그 부품은 유닛 몸 기준이라 쓰지 않는다 |
| HP-18 | 이펙트 자리: `CombatCuePoints.CellPose`(칸 표면·필드 회전)에 위치·회전 보정 칸을 더한다. 값은 표 `HaboobVisual`의 이펙트 종류·보정 둘. 창구는 Factory의 `effects`(`CombatSceneDisplayFactory:134`)를 넘긴다 | 큐 값이 필요 없다(시작·지연·끝이 없음). 필드 창구 계약은 `Settings`, `Show` 그대로다 |
| HP-19 | 경고 프리팹·이동 큐·`CombatCueEvent.HaboobRelocation`·`CombatCuePoints.ForCells`는 만들지 않는다 | 연기 자체가 외형이라 이동 이펙트가 따로 필요 없다. 필요해지면 그때 더한다 |
| HP-20 | **2026-11-02 정함(§2.1 ⑤): 레이어 0.** 프리팹은 고치지 않고 재생에서 먼저 본다. 확인할 값: `Smoke.prefab` 레이어 18(`Blending`). Boot의 낮 카메라(0·20·21·30·31)와 밤 카메라(0·22·30·31)는 18을 그리지 않고 Main Camera만 그려서, 낮·밤 화면에 안 나올 수 있다. 안 나오면 `FallingRocks`와 같은 0으로 바꾸는 것을 권한다. Mesh 입자 크기 3·초당 5개·수명 5초(칸 1칸보다 크게 겹침), 그림자 켜짐, 켜자마자 입자가 없음(prewarm 꺼짐) | 민형님 프리팹이고 재생을 봐야 정할 수 있다. 안 보이거나 어색하면 그때 사용자와 정한다 |
| HP-21 | 규칙은 칸이 바뀌는 순간 바뀌고 연기는 천천히 따라온다. 판정·호버는 Core `Cells`다 | 연기가 사라지는 데 최대 5초, 차는 데 몇 초 걸린다. 화면과 판정이 잠깐 다를 수 있어 §4에서 확인한다. **이동은 §1.2 HP-27이 대신하고, 재시작·재연결은 그대로다** |

### 1.2 2026-11-02 구름 이동(HB-d)

사용자 "Core에서는 순간이동이지만 화면에서는 구름이 이동하는 것처럼" → "도착까지 대기". 설계 [§9.4](../specs/2026-11-01-combat-haboob-view-design-Hojin.md). 표의 HP-22~HP-29는 사용자가 그대로 정했다(2026-11-02). HP-27의 `BeginMove` 자리만 구현 때 바뀌었다(§7.6).

| ID | 정한 것 | 이유 |
|---|---|---|
| HP-22 | 짝은 `Expected.Cells[i]` → `Final.Cells[i]`다. 개수가 다르면 움직이지 않고 지금처럼 `Show`로 바로 맞춘다 | Core가 같은 오프셋 목록으로 칸을 만들어 순서·개수가 같다(`SelectCells`). 다만 Core 문서가 약속한 것은 아니어서, 어긋나도 화면만 틀어지고 멈추지 않게 한다 |
| HP-23 | 옮기는 입구는 손잡이 `CombatEffectHandle.MoveTo(Vector3)` + 받침 `CombatEffectFollower.MoveTo(use, position)`이다. 살아 있는 같은 사용 번호일 때만 받침 위치를 바꾼다 | 받침 자리가 곧 연기 자리이고(`CombatEffectWindow.Play`가 지역 값 0으로 놓음), 유닛을 따라가지 않는 받침은 `LateUpdate`가 위치를 덮지 않는다. GlobalSystem 변경이 없다 |
| HP-24 | 표시 입구는 `ICombatHaboobField.BeginMove(from, to, mapper)`(움직일 것이 있으면 true)와 `TickMove(progress)`다. `BeginMove`가 손잡이 표를 새 칸 이름으로 바로 바꾼다 | 결과 끝 `Render`가 아무것도 다시 내지 않는다. 겹치는 칸이 있어도 먼저 목록으로 뺀 뒤 새로 넣는다. `Render`는 연출이 끝나야 불리므로(`RefreshCore` 검사) 이동 중에 끼지 않는다 |
| HP-25 | 도착 칸의 연기가 살아 있지 않으면(재생 취소 뒤) 그 칸은 움직이지 않고 도착 칸에 바로 다시 낸다 | 지금 `Show`의 "밖에서 멈춘 칸은 다시 낸다"와 같다 |
| HP-26 | 새 조각 `Presentation/Pieces/CombatHaboobMovePiece`(낙석 돌 조각 꼴)가 `Mathf.SmoothStep`으로 진행률을 넘긴다. 취소되면 진행률 1을 넘기고 끝난다. 조립은 `CombatComposition.ForAction(General)`이고 `waits: true`다 | 낙석과 같은 길이다. 행동 실행이라 뒤 사건을 막고, 유닛 위 지나감에 아무 판정이 없다. HP-7대로 유닛 실행이 끝난 뒤 시작한다 |
| HP-27 | 처리기: `ApplyFacts`는 칸이 바뀐 `Relocation`이면 소리 + `BeginMove`, 나머지(경고·칸이 그대로)는 지금처럼 `Show`다. `TryBeginAfterFacts`가 조립을 시작한다. `BeginMove`가 false면 시작하지 않고 바로 끝난다 | 소리는 출발 순간 한 번이다(HP-8 그대로). 규칙은 사건 순간에 바뀌지만 재생 중이라 입력이 닫혀 있어, 입력이 열릴 때 화면과 판정이 같다 |
| HP-28 | 값은 표 `HaboobVisual`의 `relocationSeconds` 하나, 기본 1, 0 초과만 받는다(DB를 만들 때 실패). 거리와 상관없이 같은 시간이다 | 대기 시간이 거리로 달라지지 않는다. 맵을 가로지르면 빠르게 보이는데, 재생에서 어색하면 값을 고친다 |
| HP-29 | 처리기 생성 인자 `CombatPieceRunPlayer` 하나, `CombatSceneEventPresenter` 줄 하나만 더한다. Factory·필드 창구 인자·미리보기 Factory는 그대로다 | 표시는 이미 이펙트 창구를 받고, 조각 실행 재생기는 Presenter에 있다 |

## 2 결정

### Q-HP1 지정 거절 안내가 뜨는 길 — A(2026-11-02 사용자)

안내는 밖 → 안, 안 → 밖 모두 **금지된 칸을 클릭할 때** 나온다. 구름 칸 호버는 거절 안내 대신 기믹 설명을 보인다(HP-11, 공격 모드에서도 나옴). 공격자가 구름 안이고 구름 밖 칸을 가리키면 그 칸에는 구름이 없어 호버 설명이 없으므로, 클릭 안내가 이유를 알리는 유일한 길이다.

설계 HB5는 `CombatActionNotice`에 안내 한 줄을 더한다고 했지만, **그 줄만으로는 일반 플레이에서 안내가 뜨지 않는다.** Core가 막힌 칸을 범위와 조준 목록에서 빼기 때문이다(`CombatAttackQueries.RangeCells`). 그런 칸을 누르면 View는 "목록에 없음"으로 보고(`AttackTargetingInteraction.AimFor`) `InvalidAttackAim`을 내고, 화면에는 일반 문구 "공격 대상이 없거나 공격할 수 없는 위치입니다"가 나온다. `HaboobDesignationBlocked`는 미리보기나 실행이 그 칸을 직접 물어야 나온다.

| 안 | 내용 | 변경 | 약점 |
|---|---|---|---|
| **A(정함)** | 칸 조준 모드에서 목록에 없는 칸을 누르면 그 칸의 공격 미리보기를 한 번 요청하고, 답의 `FailureReason`으로 안내한다 | `AttackTargetingInteraction` 요청 번호 하나와 응답 갈래, 안내 한 줄 | 입력 상태 하나가 늘고, 다른 거절 이유도 Core 이유로 들어온다(문구는 대부분 일반 문구 그대로) |
| B | 안내 줄만 더하고 일반 문구를 둔다. 규칙은 구름 호버 설명으로 알린다 | 안내 한 줄 | 안내 줄이 거의 닿지 않고, 인계 §6.3·§9.5의 명시적 안내를 채우지 못한다 |
| C | Core에 요청: 범위 응답에 하붑으로 막힌 칸 목록(벽의 `BlockedContacts`와 같은 꼴) | Core 응답·View 표시 | Core 요청이 필요하다. 호버 때도 알릴 수 있지만 이번 범위보다 크다 |

A의 근거: 사거리 검사가 하붑 검사보다 먼저라(`AttackSelectionResolver.TryResolve` → `CanDesignate`) 사거리 밖 칸은 막힘으로 답하지 않는다. View에 규칙 사본이 생기지 않고 Core 변경도 없다. 답이 오기 전에 결과가 오면 범위 재요청이 슬롯을 덮으므로 안내는 나오지 않는다.

### Q-HP2 턴 주기 — Core 그대로(2026-11-02 사용자)

사용자는 처음에 "2턴 뒤 생기고 2턴 뒤 사라짐"을 말했으나, 지금 Core 주기를 확인한 뒤 "Core에 따라 만든다"로 정했다. Core 변경·천명님 시험 변경은 없다. 주기(기본값, 1 카운트 = 한 진영 턴 끝, `HaboobGimmickRule`):

| 카운트 | Core | 화면 |
|---|---|---|
| 0 | 랜덤 자리에 배치 | 칸마다 연기 |
| 1, 3, … | 이동 경고, 칸 그대로, 다음 자리 비공개 | §2.1 ① |
| 2, 4, … | 랜덤한 새 자리로 이동(지금 자리 제외) | 빠진 칸 연기 멎음, 새 칸 연기 켜짐 |

### 2.1 화면에서 정할 것(재생 뒤)

코드로는 정할 수 없어 HB-b 재생을 보고 사용자와 정한다. **2026-11-02 정함: ⑤만 레이어 0으로 바꾸고(사용자가 프리팹 저장), 나머지는 시작 값 그대로(사용자 "괜찮음").**

| ID | 볼 것 | 시작 값 | 어색할 때 |
|---|---|---|---|
| ① 경고 | 이동 1 카운트 전에 알릴지 | 표시 없음(HP-19) | 구름 칸에 짧은 이펙트 또는 소리. 다음 자리는 Core가 안 정해 보여 줄 수 없다 |
| ② 경계 | 어느 칸이 구름 안인지 읽히는지. 지정 규칙이 칸 단위인데 입자 크기가 3이라 옆 칸으로 번진다 | 연기만 | 입자 크기 줄이기(프리팹 값), 또는 구름 칸 테두리 강조 층 |
| ③ 전환 | 판정은 바로 바뀌는데 옛 연기는 최대 5초 남고 새 연기는 서서히 찬다. **HB-d 뒤에도 같다. 뿜는 곳만 옮겨 연기를 흘린다(§7.7)** | 자연스럽게 | 옛 칸은 바로 지우기, 또는 프리팹 Prewarm으로 새 칸을 바로 채우기 |
| ④ 진입 | 전투 시작에 연기가 비어 있다가 차는 것 | 그대로 | ③의 Prewarm과 같은 해결 |
| ⑤ 레이어 | 낮·밤 화면에 보이는지(HP-20) | 프리팹 그대로 | 레이어 0 |
| ⑥ 가림 | 유닛·칸 강조·범위가 연기 너머로 읽히는지 | 프리팹 그대로 | 입자 양·투명도(프리팹 값) |

## 3 Phase

### HB-a 값(화면 변화 없음)

| Task | 내용 | 파일 |
|---|---|---|
| Ha.1 | 새 표 `HaboobVisual`: 구름 이펙트 종류(필수, 재생 가능한 값), 위치·회전 보정, 이동 소리 프리셋(기본 -1), 이름·설명. 값 검사 | `DataBaseSystem/Data/View/Table/Haboob/Item/HaboobVisualDataSO.cs`, `Table/Haboob/HaboobVisualDataTableSO.cs` |
| Ha.2 | 조회 | `Queries/ViewDataBase/ViewTable/HaboobVisualTable.cs` |
| Ha.3 | `EffectType.Smoke`(값 16), `Smoke.asset`(풀 최대 32), 이펙트 표 한 줄(HP-16) | `Data/Render/Table/Effect/Enum/EffectType.cs`, `DataBaseSO/RenderGroup/EffectTable/` |
| Ha.4 | 연결 줄 | `ServiceKeys/DBFactory/Group/ViewTableKey.cs`, `ViewDataBase.cs`, `Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `CombatDisplayTables.cs` |
| Ha.5 | 설정 에셋·표 에셋·`ViewGroup.asset` 한 줄 | `DataBaseSO/ViewGroup/HaboobVisual/` |

확인: 컴파일, Combat 진입에 변화 없음, 인스펙터에 칸이 보임.

### HB-b 표시·사건(구름이 보임)

| Task | 내용 | 파일 |
|---|---|---|
| Hb.1 | 좁은 계약(HP-6) | 새 `Display/Map/ICombatHaboobField.cs` |
| Hb.2 | 표시: 칸마다 `Smoke` 손잡이(HP-17·HP-18), `Show(state, mapper)`, `Render(gimmicks, mapper)`(상태가 없으면 `Clear`), `Clear`, `Release` | 새 `Display/Map/FieldHaboobPresenter.cs` |
| Hb.3 | 표시 연결: 생성자, `RefreshCore`의 `Render`, 정리 목록 셋 | `Display/CombatSceneDisplay.cs` |
| Hb.4 | 창구·조립: 필수 인자, 표시 생성(이펙트 창구를 넘김). 미리보기 Factory는 빈 표시(토네이도와 같음) | `Playback/CombatFieldEventPorts.cs`, `CombatSceneDisplayFactory.cs`, `CombatTraitPreviewFactory.cs` |
| Hb.5 | 사건 줄·처리기 칸(HP-7) | `Playback/Ordering/CombatEventTypeTable.cs`, `Playback/Handlers/CombatEventHandlerTable.cs` |
| Hb.6 | 처리기 `ApplyFacts`: `Final`로 `Show`, 칸이 바뀌는 사건이면 소리 한 번. 칸이 그대로여도 멈추지 않는다 | 새 `Playback/Handlers/CombatHaboobEventHandler.cs`, `Playback/CombatSceneEventPresenter.cs` |
| Hb.7 | 소리 슬롯(HP-8) | `Playback/Support/CombatEventSounds.cs` |

확인(`Test`에서 켬): §4의 1·2·3·6·8·9. 재생으로 §2.1 여섯 가지를 함께 본다.

### HB-c 안내·문서·검토

| Task | 내용 | 파일 |
|---|---|---|
| Hc.1 | 거절 안내 한 줄(문구는 설계 §3.5)과 Q-HP1 A 입력: 칸 조준 모드에서 목록에 없는 칸을 누르면 미리보기를 한 번 요청하고 답의 `FailureReason`으로 안내 | `Input/Assist/CombatActionNotice.cs`, `Input/Interaction/AttackTargetingInteraction.cs` |
| Hc.2 | 호버 설명(HP-11). 낙석이 줄인 `Build(state, cell)`에 갈래 하나, 생성자 인자 하나 | `Input/Assist/CombatEnvironmentText.cs`, `CombatSceneInputFactory.cs` |
| Hc.3 | 문서: 폴더 배치 안내, 설계 구현 메모·§3.3·§3.4·§5 고침, 사운드 작업자 안내 | `docs/superpowers/` |
| Hc.4 | 공유(작성만): 민형님 Factory 공유(표 연결·표시 줄), 현강님(안내 한 줄), 용근님 메모(미리보기 줄, 공격 조준 변경), 천명님 Core 전달 문서(대응 완료) | `docs/superpowers/handoffs/` |
| Hc.5 | 묶음 끝 검토(새 에이전트 하나, 코드만 주고 평가), 반영은 사용자 결정. **범위(2026-11-02 사용자): 내부 구현 + 연결부(Factory·표 등록·사건 줄·창구·입력) + 화면에 보이는 것(레이어·카메라·입자 값·높이·정렬·강조 층과 겹침·수명).** 화면은 에셋 값과 코드로 따지고, 실제 모습은 §4의 사용자 재생 확인으로 닫는다 | |

확인: §4의 4·5·7, 다른 전투 연출에 변화 없음.

### HB-d 구름 이동(실행 기록 §7.6)

| Task | 내용 | 파일 |
|---|---|---|
| Hd.1 | 값 `relocationSeconds`(HP-28), 에셋에 1 | `Table/Haboob/Item/HaboobVisualDataSO.cs`, `DataBaseSO/ViewGroup/HaboobVisual/` |
| Hd.2 | 손잡이·받침 `MoveTo`(HP-23) | `Presentation/Effects/CombatEffectHandle.cs`, `CombatEffectFollower.cs` |
| Hd.3 | 표시·계약 `BeginMove`·`TickMove`(HP-22·HP-24·HP-25) | `Display/Map/FieldHaboobPresenter.cs`, `ICombatHaboobField.cs` |
| Hd.4 | 조각(HP-26) | 새 `Presentation/Pieces/CombatHaboobMovePiece.cs`(+ `.meta`) |
| Hd.5 | 처리기 두 단계·생성 줄(HP-27·HP-29) | `Playback/Handlers/CombatHaboobEventHandler.cs`, `Playback/CombatSceneEventPresenter.cs` |
| Hd.6 | 문서: 폴더 배치 안내 `Pieces/` 줄, 호진 설정 표 메모 §14 칸 하나, 용근님 메모 끝 절(이펙트 손잡이 `MoveTo`), 설계 §10 구현 메모. Core 전달 문서는 고칠 것이 없다 | `docs/superpowers/` |

확인: §4의 3·10·11.

## 4 Unity 확인 목록(인계 §9.5)

1. 첫 턴 종료 전, 배치 단계부터 랜덤 자리에 연기가 있다. 재시작·재연결 뒤에도 현재 범위에 연기가 다시 찬다. 경고 사건에 칸이 그대로고 다음 위치가 드러나지 않는다.
2. 칸마다 연기가 하나씩이고, 십자 모양과 맞춤 모양이 칸대로 보인다.
3. 칸이 바뀌면 빠진 칸의 연기가 멎고 새 칸에 차며, 다음 주기도 Buffer 값대로 반복된다. 연기가 따라오는 동안 판정이 어색하지 않다(HP-21). HB-d 뒤에는 11이 이동 모습을 대신하고, 주기 반복만 본다.
4. 안↔안, 밖↔밖 지정은 다른 규칙이 막지 않으면 된다. 안↔밖은 범위에 없고, 빈 칸도 같다. 사거리 안의 그런 칸을 누르면 밖 → 안, 안 → 밖 모두 하붑 안내가 나오고 공격 모드는 유지된다. 사거리 밖 칸은 일반 문구다.
5. 양 끝이 밖이면 선이 구름을 지나가도 지정된다. 범위·방향 공격과 파급은 구름을 넘어 맞는다.
6. 호버·공격 범위·미리보기가 이동 뒤 새 구름 기준이다(HP-12).
7. 구름 칸 호버에 설명이 공격 모드에서도 나오고, 정적 모래폭풍 설명과 다르다. 포인터·유닛 선택이 구름에 막히지 않는다.
8. 유닛과 칸 강조·범위가 연기 너머로 읽힌다. **낮·밤 화면 모두에서 연기가 보인다(레이어 0, §2.1 ⑤).** 높이가 다른 칸에서 묻히거나 뜨지 않는다. 그림자가 어색하지 않다.
9. 값을 바꾼 스테이지(시간·칸 모양)에서 View가 따른다. 맞춤 모양도 칸대로 보인다.
10. 같은 턴에 토네이도 유닛이 공중이면 착지 뒤 구름이 옮겨진다(HP-7). 칸이 그대로인 이동도 멈추지 않고 끝난다.
11. (HB-d, §7.7) 뿜는 곳이 약 1초 동안 새 자리로 옮겨 가며 연기를 흘리고, 옛 자리와 길의 연기는 수명대로 사라진다. 낙석과 다음 턴은 뿜는 곳이 도착한 뒤에 온다. 구름이 유닛 위를 지나가도 피해·반응이 없다. 재시작·재연결 뒤에는 이동 없이 제자리에 보인다.

인계 §9.2의 시험은 새 시험을 만들지 않는 규칙으로 재생 확인이 대신한다.

## 5 커밋 묶음

사용자가 Phase마다 또는 묶어서 커밋한다. 한 커밋이어야 하는 것:

- HB-a: 새 표 코드와 `DataBaseSO/ViewGroup/HaboobVisual/`·`ViewGroup.asset`, `EffectType`·`Smoke.asset`·이펙트 표(`.meta`와 폴더 `.meta` 포함). 없으면 DB 생성이 실패한다.
- HB-b: 새 `.cs`와 `.meta`. 창구 인자와 Factory 둘은 함께. `Smoke.prefab` 레이어 변경도 함께(없으면 연기가 안 보임).
- HB-c: `CombatEnvironmentText` 생성자와 `CombatSceneInputFactory`는 함께.
- HB-d: 새 조각 `.cs`와 `.meta`, `HaboobVisualDataSO.cs`와 `HaboobVisual_Default.asset`은 함께. HB-a~HB-c보다 먼저 들어가면 컴파일되지 않는다.

## 6 원칙 점검(코드 전 예상)

| 기준 | 판단 |
|---|---|
| SSOT | 칸·단계는 Core 상태, 판정은 Core. 거절 이유도 Core 답이다(Q-HP1 A). 구름 모양은 칸 목록 하나다(HP-15로 프리팹 사본이 없어짐) |
| SOLID | 표시·처리기를 나눈다. 약점은 처리기 표와 필드 창구 인자가 는다는 것이다 |
| FW | 새 서비스 없음. 표는 ThornField와 같은 길, 이펙트는 있는 큐·창구 |
| 독립성 | 구름 외형은 프리팹·SO만 고친다. 낙석·토네이도 내부를 몰라도 된다. 공유 파일에는 줄만 더한다. Q-HP1 A의 입력 갈래는 공격 조준 한 파일이다 |

## 7 실행 기록

### 7.1 HB-a 값·에셋(2026-11-02, 사용자 "HB-a 시작", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| 파일 | 한 일 |
|---|---|
| 새 `Table/Haboob/Item/HaboobVisualDataSO.cs`, `Table/Haboob/HaboobVisualDataTableSO.cs`(+ `.meta`, 폴더 `.meta`) | 구름 이펙트 종류·위치 보정·회전 보정, 이동 소리 프리셋, 이름·설명. Empty 이펙트와 범위 밖 소리는 DB를 만들 때 실패 |
| 새 `Queries/ViewDataBase/ViewTable/HaboobVisualTable.cs`(+ `.meta`) | 설정형 조회 `Config`, 아이템 정확히 1개 |
| `EffectType.cs`, 새 `RenderGroup/EffectTable/Items/Smoke.asset`(+ `.meta`), `EffectDataTable.asset` | `Smoke` = 16, 풀 최대 32, 프리팹 `Render/Effect/EventEffect/SmokeEffect/Smoke.prefab` |
| `ViewTableKey.cs`, `ViewDataBase.cs`, `ViewDataBaseFactory.cs`, `CombatDisplayTables.cs` | 연결 줄. `CombatDisplayTables.Haboob`은 HB-b부터 읽는다 |
| 새 `DataBaseSO/ViewGroup/HaboobVisual/`(표·`HaboobVisual_Default`, + `.meta`), `ViewGroup.asset` 한 줄 | 이펙트 16, 보정 0, 소리 −1, 이름 "먼지구름", 설명은 HP-11 초안 |
| `CombatHeldEffect.cs` | 값 설명 한 줄(아래) |

계획과 다른 점: 구름 이펙트 값은 새로 만들지 않고 있는 값 `CombatHeldEffect`를 쓴다. 낼 수 있는 종류 검사와 보정 값이 같아 규칙이 두 벌이 되지 않고, `CombatCuePoints.Apply`에 그대로 넘긴다. 입력 칸은 묶음 `CombatHeldEffectEntry`를 쓰지 않고 SO에 직접 둔다. 그 묶음의 툴팁이 "몸 자리" 기준이라 칸에 쓰면 기획자가 헷갈리기 때문이다.

프리팹 확인: 파티클 방향은 프리팹 안에서 이미 위쪽이라(모양 회전 −90) 회전 보정은 0으로 시작한다. 프리팹 루트의 y −1.99는 재생할 때 놓는 자리가 덮으므로 HB-b 재생에서 높이를 본다.

**커밋:** 이 표의 파일은 한 커밋이다(없으면 DB 생성 실패). `ViewTableKey`·`ViewDataBase`·`ViewDataBaseFactory`·`ViewGroup.asset`에는 스테이지 제안 UI의 `StageOfferText` 줄이 커밋 전으로 함께 있어, 그 작업을 먼저 커밋하거나 줄을 나눠 넣는다.

Ua 확인: 컴파일, Combat 진입에 변화 없음, `HaboobVisual_Default` 인스펙터에 칸이 보임(Cloud Effect = Smoke). 문제 없음(사용자).

### 7.2 HB-b 표시·사건(2026-11-02, 사용자 "문제 없음, HB-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| 파일 | 한 일 |
|---|---|
| 새 `Display/Map/ICombatHaboobField.cs`(+ `.meta`) | 좁은 계약 `Settings`, `Show(cells, mapper)` |
| 새 `Display/Map/FieldHaboobPresenter.cs`(+ `.meta`) | 칸 → 손잡이 표. 그대로인 칸은 두고, 빠진 칸은 거두고, 새 칸과 밖에서 멈춘 칸은 다시 낸다. `Render`는 상태가 없으면 `Clear` |
| 새 `Playback/Handlers/CombatHaboobEventHandler.cs`(+ `.meta`) | `ApplyFacts`에서 `Final.Cells`로 맞추고 기다리지 않음. 소리는 `Relocation`이고 칸이 바뀔 때 한 번 |
| `CombatEventTypeTable.cs`, `CombatEventHandlerTable.cs`, `CombatSceneEventPresenter.cs` | 처리기 칸 `Haboob`, 사건 줄(`overlapsUnitRuns` 없음, HP-7), 처리기 연결 |
| `CombatFieldEventPorts.cs`, `CombatSceneDisplay.cs` | 창구 필수 인자, `RefreshCore`의 `Render`, 정리 목록 셋 |
| `CombatSceneDisplayFactory.cs`, `CombatTraitPreviewFactory.cs` | 표시 생성. 미리보기는 이펙트 창구를 창구 묶음보다 먼저 만들도록 줄을 옮김(빈 표시) |
| `CombatEventSounds.cs` | 슬롯 `HaboobRelocationSlot`(연결 대기 null), 함수 `HaboobRelocation` |

계획과 다른 점: 표시 생성자는 HP-17의 `(field, settings, effects)`가 아니라 `(settings, effects)`다. 칸 자리는 `Show`가 받은 Mapper로 구하고 이펙트의 부모는 창구가 정해 필드를 쓸 곳이 없다.

코드로 확인한 것:

- `Smoke.prefab`에 `ParticleStopNotifier`가 있어 거둔 연기가 입자가 다 사라진 뒤 풀로 돌아간다. `Looping`이지만 `AsHeld`라 끝나지 않는 파티클 경고는 나오지 않는다.
- 재생 취소(`StopAll`)로 멈춘 칸은 손잡이의 `IsLive`가 거짓이라 다음 `Show`가 다시 낸다.
- Core는 다른 자리가 없으면 같은 칸으로 `Relocation`을 낸다(`HaboobGimmickRule.SelectCells`). 그때 연기와 소리는 그대로다.
- `Test`의 기본 모양은 12칸이고 주기는 이동 간격 2·경고 1이다. 풀 최대 32면 옛 칸 12 + 새 칸 12가 겹쳐도 남는다.

**커밋:** 새 `.cs` 셋과 `.meta`, 창구 인자와 Factory 둘은 한 커밋이다. HB-a 묶음보다 먼저 들어가면 컴파일되지 않는다.

Ub 확인: `Test`의 `haboob.enabled`를 켜고 §4의 1·2·3·6·8·9를 본다. 재생으로 §2.1 여섯 가지를 함께 정한다.

Ub 결과: 처음 재생에서 연기가 보이지 않았다. 원인은 프리팹 레이어 18이 메인 카메라 Culling Mask(0·20·21·30·31)에 없는 것이었고, 재생 중 Default로 바꾸면 보이는 것을 사용자가 확인한 뒤 프리팹을 0으로 저장했다. HP-20의 낮·밤 카메라 설명은 민형님 `600d0636e`(렌더 카메라 제거) 전 기준이고, 지금 Boot 카메라는 Main 하나다. 이후 화면 값은 사용자 "괜찮음".

### 7.3 HB-c 안내·문서(2026-11-02, 사용자 "괜찮음, HB-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

| 파일 | 한 일 |
|---|---|
| `Input/Assist/CombatActionNotice.cs` | `HaboobDesignationBlocked` 안내 한 줄(설계 §3.5 문구) |
| `Input/Interaction/AttackTargetingInteraction.cs` | 이유 요청 번호 하나, `TryRequestRejectionReason`, `ApplyRejectionAnswer`, 정리 자리 셋 |
| `Input/Assist/CombatEnvironmentText.cs`, `CombatSceneInputFactory.cs` | 구름 칸 호버 설명, 생성자 끝 인자 `HaboobVisualData`, 낙석 주석 자리 옮김 |

계획과 다른 점 둘:

- 답의 `FailureReason`을 그대로 넘기지 않고, `HaboobDesignationBlocked`일 때만 하붑 안내를 내고 나머지는 지금까지처럼 일반 안내로 보낸다. 안내 표(`IsTargetFailure`)가 모르는 이유가 조용히 지나가는 것을 막아, 하붑 밖 동작이 그대로다.
- 조준 목록이 비었을 때도(범위 응답은 성공) 칸으로 묻는다. 구름 때문에 사거리 안 칸이 모두 막히면 목록이 비어 칸 조준인지 알 수 없는데, 이때가 안내가 가장 필요하다. 방향 조준 유닛이면 Core가 다른 이유로 답해 일반 문구가 나온다(`AttackSelectionResolver`가 예외 없이 실패로 답함, 코드로 확인).

코드로 확인한 것: 결과가 오면 `AfterResult`가 번호를 지운다. 답 전 포인터 이동과 안내 시간은 §7.5에서 고쳤다.

Hc.3 문서: 폴더 배치 안내, 설계 머리말·§3.3·§3.4·§3.7·§5·§10, 사운드 작업자 안내, 이펙트·소리 창구 안내.

Hc.4 공유(모두 미전달): 민형님 Factory 공유 §48(프리팹 레이어 변경·윤곽선 확인 부탁 포함), 현강님 메모 §20, 호진 설정 표 메모 §14, 용근님 메모 끝 절, Core 전달 문서 §63(제목 함께). 작성 중 찾음: 낙석 RF-d가 적었다는 Core 전달 문서 §63이 파일에 없어(커밋 이력에도 없음) 이번 §63에 낙석까지 함께 적었다.

Uc 확인(§4의 4·5·7): 사거리 안의 금지 칸을 누르면 하붑 안내가 나오고 공격 모드가 유지된다. 사거리 밖 칸은 일반 문구다. 구름 칸 호버에 "먼지구름" 설명이 공격 모드에서도 나온다. 다른 환경 호버와 다른 공격 조준은 그대로다.

### 7.4 묶음 끝 검토(2026-11-02, 새 에이전트 하나, 코드·에셋만, 실행 없음, 약 9.43, 높음 0·중간 2·낮음 11, 반영은 §7.5)

기준별: SSOT 9.25, SOLID 9.75, FW 10, 독립성 9.0, 캡슐화 10, C#·Unity 8.25, If·Tick 9.75. 화면 평가는 값으로 따진 것이다.

- M1 프리팹 y −1.99가 실행 때 덮여 연기가 칸 표면에서 시작한다. 값으로 보면 가장 굵은 부분이 표면 위 약 2.5이고 농도가 높아 구름 안 유닛이 가려질 수 있다(추정). 사용자가 재생을 보고 "괜찮음"이라 해서 값은 그대로다. 어색해지면 `cloudPositionOffset.y`부터 맞춘다.
- M2 레이어 0으로 윤곽선 기능의 이펙트 레이어에서 빠졌고, 지금 18번을 쓰는 프리팹이 없다. 사용자가 0으로 정했고 민형님 공유 §48에 적었다.
- 권장 반영 후보: L1 안내 문구 30자가 넘는데 시간 0.5초, L2 답 전에 포인터가 옮겨 가면 안내가 사라지고 번호만 남음, L10 처리기의 `Step` 조건이 칸 비교와 겹침, L11 프리팹 그림자 칸 켜짐.
- 그대로 둘 후보: L3 기믹 없는 스테이지도 일반 안내가 Core 왕복만큼 늦음, L4 사거리 안 아군·빈 칸도 하붑 문구(Core 판정 순서), L5 그대로인 칸의 바닥 높이가 바뀌면 연기가 옛 높이(운영 데이터 조합 드묾), L6 하붑 상태 하나 전제, L7 "먼지구름"이 코드·에셋 두 곳, L8 같은 값 검사 두 번, L9 창구가 설정 전체를 노출.
- 범위 밖 보고: `EffectScope.cs:2`의 `using NUnit.Framework`는 런타임 코드라 플레이어 빌드 문제 여부를 확인하지 못했다. 작업 트리 `StageRootDataTable.asset`에서 `Stage_Shop_Item`이 빠져 있다.
- 문제를 찾지 못한 것: 컴파일 후보, 연결부, DB 등록, 손잡이·수명, 미리보기 번호, 재생 순서. 실행 확인은 아니다.

### 7.5 검토 L1·L2·L10 반영(2026-11-02, 사용자 "L1, L2, L10을 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- L1: 문구는 그대로 두고 이 안내만 2초(`CombatActionNotice.RuleDuration`, 임시 값). 줄이면 규칙 설명이 빠져서 시간을 늘렸다. 다른 안내는 0.5초 그대로다.
- L2: 호버 요청이 슬롯을 덮기 직전에 답 안 온 이유 요청을 놓고 일반 안내를 낸다(`DropRejectionRequest`). 번호가 남지 않고 클릭이 조용히 지나가지 않는다. 이때는 하붑 이유를 알 수 없어 일반 문구다.
- L10: 처리기의 `Step` 조건을 지웠다. 경고 사건은 Core 사건 생성자가 같은 칸을 보장한다(`StageGimmickEvents.cs:33-34`).
- L11(프리팹 그림자)은 사용자가 고르지 않아 그대로다.

Uc에 더함: 금지 칸 안내가 2초 보인다. 금지 칸을 누른 직후 조준 가능한 칸으로 빠르게 옮기면 일반 안내가 나오고 호버 표시는 정상이다.

### 7.6 HB-d 구름 이동(2026-11-02, 사용자 "그대로, HB-d", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| 파일 | 한 일 |
|---|---|
| `HaboobVisualDataSO.cs`, `HaboobVisual_Default.asset` | `relocationSeconds`(기본 1, 0 이하면 DB를 만들 때 실패) |
| `CombatEffectHandle.cs`, `CombatEffectFollower.cs` | `MoveTo`. 살아 있는 같은 사용 번호일 때만 받침을 옮긴다 |
| `ICombatHaboobField.cs`, `FieldHaboobPresenter.cs` | `BeginMove`·`TickMove`, 자리 계산 `CloudPose` 하나 |
| 새 `Presentation/Pieces/CombatHaboobMovePiece.cs`(+ `.meta`) | `SmoothStep` 진행, 취소되면 끝 자리로 |
| `CombatHaboobEventHandler.cs`, `CombatSceneEventPresenter.cs` | 생성 인자 `piecePlayer` 하나, `TryBeginAfterFacts`에서 이동 시작 |
| 문서 | 폴더 배치 안내 한 줄, 호진 설정 표 메모 §14, 용근님 메모 끝 절(모두 미전달), 설계 §10 |

계획과 다른 점: HP-27은 `ApplyFacts`에서 `BeginMove`를 부른다고 적었지만 `TryBeginAfterFacts`로 옮겼다. 두 단계 사이에 "움직일 것이 있다"는 값을 처리기가 들고 있지 않아도 되고, 그 사이에 구름 칸을 읽는 곳이 없어 화면 결과가 같다. 소리도 같은 자리로 옮겨 출발 순간에 한 번이다.

코드로 확인한 것:

- `Smoke.prefab`은 파티클 하나이고 시뮬레이션 공간이 Local이다(`moveWithTransform: 0`). 재생으로는 아직 보지 않았다.
- 표시의 `Render`는 `RefreshCore` 안에만 있고, `RefreshCore`는 연출이 끝나기 전이면 예외다. 이동 중에 끼지 않는다.
- `HaboobVisualData`와 처리기를 만드는 곳은 각각 SO와 Presenter 하나다. Factory·필드 창구·미리보기 Factory는 그대로다.
- 묶음 끝 검토는 이번 Phase에 두지 않았다(계획 Hd에 없음).

**커밋:** 새 조각 `.cs`와 `.meta`, `HaboobVisualDataSO.cs`와 `HaboobVisual_Default.asset`은 함께 넣는다.

Ud 확인: §4의 3·10·11.

### 7.7 구름 이동 모양 고침(2026-11-02, 사용자 재생 뒤 "기존 자리에서 Smoke를 하나씩 흘리는 느낌으로 뒤에 있다가 사라지면", 컴파일 영향 없음, 재생 미확인)

한 덩어리 이동이 부자연스럽다는 보고에 따라 `Smoke.prefab` 시뮬레이션 공간을 Local → World(`moveWithTransform: 1`)로 바꿨다. 이동 코드는 그대로이고 받침 주석 한 줄만 고쳤다. 이 프리팹을 참조하는 곳은 `Smoke.asset`뿐이라 다른 연출에는 영향이 없다.

- 멈춰 있을 때 모습은 같다. 받침이 움직일 때만 달라진다.
- 이미 나온 입자는 제자리에 남아 수명(5초)대로 사라지고, 옮겨 가는 동안 뿜는 곳마다 초당 5개(1초 이동이면 약 5개)가 길을 따라 떨어진다.
- 도착 뒤 새 칸은 서서히 짙어진다. 입력은 뿜는 곳이 도착하면 열리므로, 옛 자리에 연기가 최대 5초 남아 있고 새 칸은 옅은 동안에도 판정은 새 칸이다.
- 연기가 듬성하거나 많으면 `Smoke.prefab` 방출량 또는 `relocationSeconds`로 조절한다. 재생을 본 뒤 정한다.

**커밋:** 프리팹 값 변경을 HB-d 묶음에 함께 넣는다. 민형님 공유 §48에 적었다(미전달).

재생 결과: 흘리는 모습 괜찮음(2026-11-02 사용자). 방출량·`relocationSeconds`는 지금 값 그대로다.
