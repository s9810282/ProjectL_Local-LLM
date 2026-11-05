# 전투 View GC·프레임 끊김 개선 계획 (GC-a~GC-f)

- 날짜: 2026-11-03
- 설계: [gc-hitch-design](../specs/2026-11-03-combat-view-gc-hitch-design.md) GH1~GH6 (결정 완료: Q-GH1·Q-GH2·Q-GH3 A, Q-GH4 B, Q-GH5 B, Q-GH6 B)
- 규칙: View 작업이라 새 시험을 쓰지 않고, 전투 View라 MSBuild도 돌리지 않는다. Phase마다 "컴파일 미확인"으로 보고하고 멈춘다. 확인은 사용자가 Unity Profiler(0단계 구간 표시)로 한다.
- 범위: `ViewSystem/Combat/` 와 0단계 표시를 넣은 골격 Job 셋(GC-f에서 되돌림만). Core·Framework·GlobalSystem·UISystem 파일은 고치지 않는다.

## 0. 계획에서 정한 것 (TP, 사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 열거 값 검사 도우미는 새 `Presentation/Pieces/CombatEnumValues.cs`. 부르는 꼴은 `CombatEnumValues.IsDefined<T>((int)value)`(int 바탕 열거만, 표를 만들 때 검사). | 여덟 곳 중 여섯이 `Pieces/`이고 `Attack/`과는 이미 서로 참조한다. 제네릭 값을 박싱 없이 정수로 바꾸려면 `UnsafeUtility`가 필요해, 부르는 쪽에서 `(int)`로 바꾸는 꼴을 골랐다. |
| TP-2 | 투사체 풀은 맞을 때마다 도는 `Return`만 고친다. 재생 취소의 `Clear`와 한 번 도는 `Release`는 그대로. | 설계 GH4 줄 그대로다. 둘은 자주 돌지 않는다(설계 §1.4의 "같은 꼴" 언급은 기록만). |
| TP-3 | 몸 위 글자 `Show`의 `_track(unit)` 대리자 할당은 그대로 둔다. `Exists` 람다만 반복으로. | 특성 글자 하나마다 한 번이라 드물다. |
| TP-4 | GH1 지연 그리기는 `FieldView`가 켜져 있을 때(`isActiveAndEnabled`)만. 꺼져 있으면 지금처럼 바로 그린다. `Build`는 바로 그리고 기다리던 것을 버린다. `Release`도 버린다. | 설계 GH1 그대로. `LateUpdate`가 돌지 않으면 그리기가 영영 안 된다. |
| TP-5 | GH1에서 늦추는 입구: `ApplyStaticHighlights`·`SetHoverCell`·`ClearHoverCell`·`SetLayerCells`·`ClearLayer`(칸 모으기), `SetPlacementActive`·`SetPathPlanning`·`RepaintSurface`(전체 다시). | `FieldView.cs:185-260`의 그리기 호출 전부다. |
| TP-6 | GH6 수: 유닛 표시 = 키별 살아 있는 캐릭터 수(구역 무관), 고스트 = 플레이어 캐릭터 키의 프리팹마다 1, 투사체 = 프리팹마다 1, 몸 위 글자 = 프리팹마다 2, 강조 판 = 채우기 종류마다 깊이 0 하나(필드 `Build` 직후). | 설계 GH6 원칙. 글자 2는 한 발동에 원인·대상 둘이 흔해서다. |
| TP-7 | GH6에서 미리 만들기가 실패하면 그대로 설치 실패(지금 설치 실패 정리 길, 오류 경계 F1). | 처음 나올 때 났을 실패가 앞당겨질 뿐이다. 실패를 삼키면 원인이 숨는다. |
| TP-8 | GC-f에서 0단계 표시는 넣은 줄만 되돌린다(`using Unity.Profiling;`, 표시 필드, `Auto()` 감싸기). 다른 줄은 그대로. 골격 Job 셋도 같은 방식. | Q-GH6 B. 골격 Job 셋은 우리 영역(`LifeCycles/Combat/`)이다. |

## 1. Phase

### GC-a. GH4 작은 할당 (동작 같음)

| Task | 파일 | 내용 |
|---|---|---|
| a.1 | `Input/MovePath/CombatPathInput.cs` | `CancelInputs(bool)`: 생성자에서 대리자 여섯(상호작용 취소·범위 남김·범위 지움·계획 끝·초안 지움·계획 맞춤)을 한 번 만든다. 본문은 판 번호 올림 → `CleanupErrors.Try` 다섯 번(둘째부터는 판 번호가 같을 때만) → `ThrowIfAny`(문구 그대로). 지금 람다는 실행 순간 판 번호를 봤고, 새 꼴은 부르기 바로 앞에서 본다 — 같은 판정이다. `RunCleanup`은 `Detach`가 계속 쓴다. |
| a.2 | 새 `Presentation/Pieces/CombatEnumValues.cs`(+ `.meta`) | 형식마다 정의된 값 표(`HashSet<int>`)를 처음 한 번 만들고, int 바탕이 아니면 예외. `IsDefined<T>(int)`. |
| a.3 | `CombatPieceStart.cs:91`·`CombatPieceContext.cs:76`·`CombatComposition.cs:88`·`:209`·`:244`·`CombatStandMovePiece.cs:52`·`Attack/CombatHitGate.cs:70`·`Attack/CombatAttackShape.cs:38` | `System.Enum.IsDefined(typeof(T), v)` → `CombatEnumValues.IsDefined<T>((int)v)`. 예외 문구 그대로. |
| a.4 | `Playback/Support/CombatShownFacts.cs:51`·`:73` | `RemoveAll` 람다 → 뒤에서부터 인덱스 반복(같은 개체를 모두 지움, 순서 유지). |
| a.5 | `Presentation/Attack/CombatProjectilePool.cs` `Return` | 실패 목록은 실패 길에서만 만든다(TP-2). |
| a.6 | `Presentation/Text/CombatBodyTextWindow.cs:56` | `Exists` 람다 → 반복 함수(TP-3). |
| a.7 | `Input/MovePath/CombatMovementInteraction.cs:401` | `SequenceEqual` → 개수·인덱스 비교. |

확인 Ua: 적 턴 중 `Combat.View.Input.Tick`의 GC Alloc 0, 경로 Drag 중 같은 값 0, 이동·공격·턴 종료·재생이 지금과 같음.

### GC-b. GH1·GH2 강조 (보이는 차이 없음)

| Task | 파일 | 내용 |
|---|---|---|
| b.1 | `Display/Field/FieldView.cs` | 바뀐 칸 표시 배열(칸 수)·목록과 "전체 다시" 깃발을 `Build` 때 한 번 만든다. TP-5의 입구는 상태만 바꾸고 모은다(TP-4: 꺼져 있으면 바로). `LateUpdate`에서 전체면 `Refresh` 한 번, 아니면 모은 칸으로 `Repaint` 한 번. `Build`·`Release`는 기다리던 것을 버린다. 문서 주석에 "그리기는 프레임 끝". |
| b.2 | `Display/Field/FieldOutlineRenderer.cs` | 슬롯마다 칸 배열을 들고, 칸 수가 같으면 그 배열에 채워 다시 쓴다(`:627` `ToArray`). |
| b.3 | `Display/Field/FieldCellFill.cs` | `Update`의 위치 배열을 칸 수가 같으면 다시 쓴다(`:98`). |

계약 `IFieldHighlightRenderer`·두 표시기의 `Repaint`/`Refresh` 뜻은 그대로. 확인 Ub: 공격 모드 호버 중 `Combat.View.Field.Refresh` Calls가 프레임당 1 이하, 강조·범위·호버·이동 초안 모습이 지금과 같음(CellPrefab 모드도).

### GC-c. GH3 HUD (보이는 차이 없음)

| Task | 파일 | 내용 |
|---|---|---|
| c.0 | (대조) | HUD의 같은 칸을 쓰는 다른 길을 다시 찾는다: `CombatUnitHudFeedback`(피해 숫자·비용·예상 체력), 구조물 `SetStructure`, `UnitHUDLayer` 선택 정보, 상태 보조 동작. 겹치면 그 길이 쓴 뒤 기억을 비운다. |
| c.1 | `Display/Unit/FieldUnitView.cs` | 마지막으로 HUD에 쓴 사실 객체 칸. HUD 붙이기·`Bind`(다른 개체)·`ResetForPool`·HUD 해제 때 비움. |
| c.2 | `Display/Unit/FieldUnitPresenter.cs` | `RenderUnitHud`가 같은 객체면 건너뜀. `Configure` 때 모든 표시의 기억을 비움. |

확인 Uc: 적 진행 중 `Combat.View.Units.Hud` 시간·GC Alloc이 크게 줆(Calls는 그대로, §3.3), 피격·상태·아이템·격파 목표·선택 정보 HUD가 지금과 같음.

### GC-d. GH5 매 프레임 CPU

| Task | 파일 | 내용 |
|---|---|---|
| d.1 | `Display/Unit/UnitHUDLayer.cs` | HUD 위치는 바뀔 때만 쓴다(`:258`). 선택 정보 갱신(`:59`·`:265`)은 선택·보임이 바뀔 때만. |
| d.2 | `Input/Assist/CombatEnvironmentHover.cs:61-65` | 설명 창은 화면 위치가 바뀔 때만 옮긴다. |

확인 Ud: 카메라가 멈춘 동안 HUD 캔버스 다시 묶기가 없음, 카메라·유닛이 움직이면 HUD가 따라감, 선택 정보·환경 설명이 지금과 같음.

### GC-e. GH6 처음 나올 때 생성

| Task | 파일 | 내용 |
|---|---|---|
| e.1 | `Display/Unit/FieldUnitPresenter.cs` | `Prewarm(characters)`: 키별 필요 수(TP-6) − 그 키로 만든 수 만큼 `CreateView` + `EnsureUnitHud` 뒤 풀 반환 길(`ResetForPool`)로 넣는다. 키별 만든 수는 `CreateView`가 센다(`Release`에서 비움). 연출 값도 이때 읽어 둔다. |
| e.2 | `Display/Feedback/PlacementGhost.cs` | `Prewarm(prefabs)`: 프리팹마다 풀이 비었고 쓴 적이 없으면 하나 만들어 넣는다(만들 때 숨김). |
| e.3 | `Presentation/Attack/CombatProjectilePool.cs`·`CombatProjectileFlights.cs`·`CombatAttackPlayback.cs` | 풀 `Prewarm(count)`(꺼진 채 만들어 넣음), 비행 묶음·공격 재생 입구 한 줄씩. |
| e.4 | `Presentation/Text/CombatBodyTextPool.cs`(·창 입구) | 프리팹마다 정해진 수를 만들어 `Available`에 넣는다. |
| e.5 | `Playback/CombatSceneEventPresenter.cs` | `Prewarm(characters)`: 투사체(유닛 공격 값의 프리팹)·몸 위 글자(`CombatCueSources.BodyTextPrefabs`)를 각 주인에게 넘긴다. |
| e.6 | `Display/Field/FieldCellFillStack.cs`(·`FieldOutlineRenderer`) | 구성 끝에 채우기 종류마다 깊이 0 판을 만든다(빈 판은 그리지 않음). |
| e.7 | `Display/CombatSceneDisplay.cs` `Install` | 첫 `RefreshCore` 뒤에 유닛·고스트·사건 Presenter 미리 만들기를 부른다(TP-7: 실패는 설치 실패 정리 길). |

확인 Ue: 배치 국면 첫 배치·첫 Drag, 적 턴 첫 원거리 공격, 첫 특성 글자, 첫 범위 표시 프레임에 `Instantiate`가 없음. 전투 설치 시간 증가 확인. 원점에 보이는 유닛·고스트 없음.

### GC-f. 0단계 표시 지우기·문서·묶음 끝 검토

| Task | 내용 |
|---|---|
| f.1 | 0단계 구간 표시를 여섯 파일에서 되돌린다(TP-8). |
| f.2 | 폴더 배치 안내에 새 파일(`CombatEnumValues`) 자리, 설계 구현 메모, CLAUDE.md 현재 상태. 현강님 줄 변경 정리(GC-d `UnitHUDLayer`, §3.4)를 현강님 메모에 한 절. |
| f.3 | 묶음 끝 검토 하나(새 에이전트, 7기준). 반영은 사용자 결정. |

## 2. 커밋 묶음 (사용자가 직접)

- 새 `.cs`는 `.meta`와 함께(`CombatEnumValues.cs`).
- 0단계 표시는 GC-f에서 지우므로, 0단계를 따로 커밋하지 않았다면 이 묶음 전체를 한 커밋으로 해도 표시 줄은 남지 않는다.

## 3. 실행 기록

### 3.1 GC-a 코드 완료 (2026-11-03, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

- 사용자 결정 "Q-GH4: 이번에 같이 진행 / Q-GH5: 다른 영역은 알아서 할 테니 나중에 / Q-GH6: 지움 / 나머지는 A로 진행"으로 설계를 고치고(GH6 추가) 이 계획을 쓴 뒤 GC-a를 진행했다. GC-a는 새 결정이 없어 TP 확인 전에 했다. TP-1~TP-3이 GC-a 몫이다.
- a.1 `CombatPathInput`: 정리 대리자 여섯·`CleanupMessage` 상수, `CancelInputs(bool)`가 `CleanupErrors.Try` 다섯 번 + `ThrowIfAny`. `RunCleanup`은 `Detach`용으로 남김(같은 문구 상수).
- a.2 새 `Presentation/Pieces/CombatEnumValues.cs`(+ `.meta`, GUID `880df1e6d2474b67ad556f097febf9a3`).
- a.3 여덟 곳 바꿈. `Attack/CombatAttackShape.cs`에 `Presentation.Pieces` using 한 줄(`CombatHitGate`는 이미 있음). 만들 때 한 번인 `CombatAttackStyles:42`·`CombatTraitStateEvidence:29`는 그대로.
- a.4 `CombatShownFacts.ApplyCharacter`·`ApplyStructure`: 뒤에서부터 인덱스 반복.
- a.5 `CombatProjectilePool.Return`: 실패 목록은 실패 길에서만.
- a.6 `CombatBodyTextWindow`: `IsSlotTaken` 반복 함수.
- a.7 `CombatMovementInteraction.SamePath`: 개수·인덱스 비교(`CellCoord`는 `IEquatable`이라 박싱 없음), 쓰지 않게 된 `using System.Linq;` 삭제.
- 계획과 다른 점: 없음.
- Unity 확인 Ua(사용자):
  1. 컴파일.
  2. 적 턴 중 Profiler `Combat.View.Input.Tick`의 GC Alloc이 0 B(또는 크게 줆). 남으면 그 아래 줄을 알려 주면 다음 Phase 전에 본다.
  3. 경로 Drag 중 같은 구간의 GC Alloc.
  4. 이동 Drag·이동 모드 범위·공격 모드 진입·턴 종료·결과 재생·특성 글자·원거리 공격이 지금과 같음.
- Ua 문제 없음(2026-11-03 사용자).

### 3.2 GC-b 코드 완료 (2026-11-03 사용자 "Ua 문제 없음, 그대로, GC-b, GC-c 진행해" = TP 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

- b.1 `Display/Field/FieldView.cs`: 그리기 대기(바뀐 칸 표시 배열 `_pendingMarks`·목록 `_pendingCells`·`_pendingRefresh`), 입구 여덟이 `QueueRepaint`·`QueueRefresh`(꺼져 있으면 바로 `FlushPending`), `LateUpdate` → `FlushPending`(칸이 있으면 `Repaint` 한 번, 전체 다시면 `Refresh` 한 번), `Build`가 칸 수만큼 표시 배열을 만들고(바뀔 때만) `Release`·`Build` 안 해제가 대기를 버림(`DropPending`). 클래스 문서에 "그리기는 프레임 끝".
- b.2 `FieldOutlineRenderer`: 슬롯마다 `CellArrayOf`(칸 수가 같으면 같은 배열에 복사), `Apply`의 `ToArray` 삭제.
- b.3 `FieldCellFill.Update`: 위치 배열 `_positions` 재사용.
- 계획과 다른 점·알아 둘 것(보이는 차이 없음):
  - 표시기 계약을 그대로 두어, 칸 변경과 칸 무관 변경이 **같은 프레임에 겹치면** GridOutline 표시기는 `Refresh`를 두 번 돈다(`Repaint`가 곧 `Refresh`). 두 번째는 칸·색이 같아 메시를 다시 만들지 않는다(`FieldDrawnCells.SameAs`). 그 밖에는 프레임당 한 번.
  - 그리기 실패 예외가 `LateUpdate`에서 나므로 **오류 경계(F1)가 아니라 Unity 콘솔**로 간다. 전에는 강조를 바꾼 호출자(입력·재생)에서 나와 F1로 멈췄다. 실패해도 대기는 비워 같은 실패를 매 프레임 되풀이하지 않는다.
  - `FieldView.LateUpdate`보다 늦게 도는 `LateUpdate`가 강조를 바꾸면 다음 프레임에 그려진다. 지금 그런 곳은 `UIInputBinding.LateUpdate`의 호버 해제(버튼이 상호작용 불가가 된 순간) 하나다. 실행 순서 속성은 더하지 않았다.
- Unity 확인 Ub(사용자):
  1. 컴파일.
  2. 공격 모드에서 칸 위를 움직일 때 Profiler `Combat.View.Field.Refresh` Calls가 프레임당 1(겹친 프레임은 2) 이하, 같은 구간 GC Alloc이 줆.
  3. 이동 모드 범위·이동 Drag 경로·공격 범위·영향 칸·호버(가능/불가 색)·유닛 호버 범위·위험 범위·넉백 도착 칸·현재 행위자·전투 목표·배치 구역(배치 드래그 중)·레일이 지금처럼 그 프레임에 바로 보이고 지워짐.
  4. 고지대가 있는 스테이지에서 칸 테두리·채우기 높이가 지금과 같음.
  5. 배치 드래그 시작·끝, 이동 경로 지정 시작·끝에 테두리가 지금처럼 바뀜.
  6. (가능하면) CellPrefab 표시 모드 스테이지에서 칸 색 강조가 같음.

### 3.3 GC-c 코드 완료 (2026-11-03, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

- c.0 대조 결과(HUD에 쓰는 다른 길):
  - 피해 숫자 `ShowDamage`, 예상 체력 `SetHealthPreview`는 다른 칸이다. 예상 체력은 `view.Apply`가 그리기마다 지운다(지금 그대로).
  - 구조물 `SetStructure`는 구조물 표시만 쓰고, 같은 표시를 캐릭터로 쓰려면 풀 반환·다른 개체 묶기를 거친다.
  - 선택 정보 창(`UnitHUDLayer.RefreshSelectedInfo`)은 HUD가 마지막으로 쓴 내용을 열 뿐 쓰지 않는다. 그 창은 숨길 때 호버만 지운다.
  - HUD 층의 화면 밖 숨김은 `CanvasGroup` 알파라 HUD 내용에 손대지 않는다.
  - **HUD가 꺼질 때(`UIBase.OnDisable` → `UnitHUD.OnUIHidden`) 체력 상태(`_hasHealth`)·예상 체력·설명을 지운다.** → 기억 비우는 때에 "HUD가 숨겨질 때"를 더함(설계 GH3 목록에 없던 것, 계획 c.1의 "HUD 해제 때"를 넓힘). `FieldUnitView.SyncHudActive`가 숨길 때 비우며, 표시가 꺼지는 `OnDisable`·풀 반환도 이 길을 지난다.
  - HUD 내용은 사실 객체(불변, 속성 setter 없음)와 표(외형·직군·상태·아이템)로만 정해진다. 공격력·방어력 계산 `CombatEffectiveStatsCalculator.AttackPower`·`PhysicalDefense(facts)`도 사실만 읽는다.
- c.1 `Display/Unit/FieldUnitView.cs`: `_hudFacts`·`ShowsHudFacts`·`RememberHudFacts`·`ForgetHudFacts`, 비우는 때 = HUD 붙이기·떼기, 다른 개체 `Bind`, HUD 숨김(`SyncHudActive`, 풀 반환 포함).
- c.2 `Display/Unit/FieldUnitPresenter.cs`: `RenderUnitHud`가 같은 사실 객체면 보이기(`ShowUnitHud(true)`)만 맞추고 돌아감, 다 쓴 뒤 기억. `Configure`·`ConfigureHUDCategories`가 모든 표시의 기억을 비움(`ForgetHudFacts`).
- 남는 작은 차이(기록만): HUD 층 오브젝트 자체가 꺼졌다 켜지면(UISystem이 층을 숨길 때) 각 HUD의 `OnUIHidden`이 불리지만 표시는 모른다. 그 뒤 사실이 그대로인 유닛의 **다음 체력 감소가 애니메이션 없이 바로** 줄 수 있다. 전에도 층이 다시 켜진 뒤 첫 그리기가 그 피해 사건이면 같았다(차이는 그 사이에 다른 사건이 있을 때뿐). 막으려면 HUD 기준점 계약(`IUnitHUDAnchor`)에 숨김 알림을 더해야 해 하지 않았다.
- 0단계 표시 `Combat.View.Units.Hud`는 건너뛰는 호출도 감싸므로 **Calls는 유닛 수 그대로**이고, 시간·GC Alloc이 줄어든다(계획 표의 "Calls 줄어듦"은 바로잡음).
- Unity 확인 Uc(사용자):
  1. 컴파일.
  2. 적 진행 중 Profiler `Combat.View.Units.Hud`의 시간·GC Alloc이 크게 줆(Calls는 그대로).
  3. 피격·회복·사망·상태 걸림/풀림·아이템 장착/해제/발동·격파 목표·공격력/방어력 변화가 HUD에 지금처럼 바로 반영됨.
  4. 유닛을 끌어 옮길 때(원래 유닛 숨김) 뒤 다시 보이는 HUD 내용이 같음, 그 뒤 피격 체력바 애니메이션이 지금과 같음.
  5. 유닛 클릭 선택 정보 창 내용이 같음.
  6. 다음 전투 진입(같은 씬 재사용) 때 HUD 내용이 새 전투 값으로 나옴.

### 3.4 GC-d 코드 완료 (2026-11-03 사용자 "Ub, Uc 문제 없음, GC-d 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ud 대기)

- d.1 `Display/Unit/UnitHUDLayer.cs`
  - `RefreshPositions`: HUD 위치를 지금 값과 비교해 바뀔 때만 쓴다(배율과 같은 `!=` 규칙). 카메라·유닛이 멈춰 있으면 HUD `RectTransform`에 쓰지 않는다.
  - `RefreshSelectedInfo`: 선택 HUD 참조 검사(`ValidateReferences`)는 **잡힌 선택 HUD가 바뀔 때만**(새 칸 `_selectedHud`, 선택·보임·HUD 개체가 바뀌거나 선택이 비었다 다시 잡힐 때). `Release`가 비운다. 검사가 실패하면 기억하지 않아 다음 갱신에 다시 실패한다(지금과 같음).
  - 계획과 다른 점: 선택 HUD **찾기 반복**(항목 순회·`EntityId` 비교)과 맨 앞으로 올리기 확인은 매 갱신 그대로 돈다. HUD의 개체는 `UnitHUD.SetUnit`·`Clear`(UISystem)로 층 모르게 바뀌어, 선택·보임 변화만 보면 새로 그 개체가 된 HUD를 놓친다. 순회는 할당 없는 비교뿐이고, 비싼 검사(`IsChildOf` 열 몇 번)만 빠진다. 같은 HUD의 참조는 실행 중 바뀌지 않고 `Prepare`는 한 번만 돌아 보이는 차이 없음.
  - 바꾼 줄은 현강님 줄(`RefreshSelectedInfo`·`RefreshPositions`, blame Twinsgm) → 변경 정리는 GC-f f.2.
- d.2 `Input/Assist/CombatEnvironmentHover.cs`: 설명 창에 마지막으로 넘긴 위치(`_movedX`·`_movedY`)를 기억하고, 같은 칸·같은 결과 갈래에서 포인터 위치가 그대로면 `MoveHover`를 부르지 않는다. 보이기·옮기기는 `MoveHover()` 한 곳. 계획 표의 줄 번호 `:165-168`은 낡아 `:61-65`로 고침(설계 §1 줄도).
- 남는 매 프레임 쓰기(다른 영역, Q-GH5 B 메모 목록에 더함 — 설계 §5): `TextNoticeUI.LateUpdate`가 설명 창이 떠 있는 동안 매 프레임 위치를 다시 계산해 쓴다(d.2는 그 앞의 `MoveTo` 한 번만 덞). 선택 정보의 호버 창 `UnitHUDHoverPanel.RefreshPosition`도 층이 매 갱신 부르고 비교 없이 쓴다. 둘 다 UISystem 화면이라 고치지 않았다.
- 남는 작은 차이(기록만): 전역 UI 캔버스가 꺼진 채 설명 창이 열렸다가 캔버스만 다시 켜지면, 포인터가 움직일 때까지 창이 옛 자리에 있을 수 있다(전에는 다음 프레임에 옮겨짐). 창이 꺼질 때는 숨김 판 번호가 올라 다시 열고 옮기므로 이 경우가 아니다.
- Unity 확인 Ud(사용자):
  1. 컴파일.
  2. 카메라·유닛이 멈춘 프레임에 Profiler `Canvas.SendWillRenderCanvases`·UI 다시 묶기(`Canvas.BuildBatch`·`Layout`)가 HUD 때문에 늘지 않음(설명 창·선택 정보 호버 창을 띄우지 않은 상태로 비교).
  3. 카메라 이동·줌, 유닛 이동·넉백, 화면 밖으로 나갔다 들어오는 유닛에서 HUD가 지금처럼 따라감.
  4. 유닛 클릭 선택 정보 창이 지금처럼 열리고 맨 앞에 보임, 다른 유닛 선택·선택 해제·선택 유닛이 화면 밖으로 나갈 때 지금과 같음.
  5. 지형·오브젝트 칸 위 환경 설명 창이 포인터를 따라가고, 칸이 바뀌면 내용이 바뀌며, UI 위로 가면 사라짐.

### 3.5 GC-e 코드 완료 (2026-11-03 사용자 "Ud 문제 없음, GC-e 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ue 대기)

- e.7 `Display/CombatSceneDisplay.cs`: `Install`이 첫 `RefreshCore` 뒤 `Prewarm(characters)`를 부른다(결과 끝 `Refresh`는 부르지 않음). **셀 캐릭터 규칙은 여기 한 곳**: 살아 있고(`!Health.IsDead`) 제외(`Excluded`)가 아닌 캐릭터 — 필드·손패·적 대기 모두. 키별 수 → 유닛 표시, 그중 플레이어 캐릭터 키 → 고스트, 키 목록 → 사건 Presenter. 실패는 `Install`의 설치 실패 정리 길(TP-7).
- e.1 `Display/Unit/FieldUnitPresenter.cs`: `Prewarm(키별 수)` — 연출 값을 읽고, 키별 만든 수(`_createdCounts`, `CreateView`가 셈)가 모자란 만큼 `CreateView` + `EnsureUnitHud` 뒤 풀 반환(`ResetForPool` 길). HUD 생성이 실패해도 표시는 풀에 넣어 숨긴다(`finally`). 풀 찾기 `PoolOf` 하나로(대여와 공유).
- e.2 `Display/Feedback/PlacementGhost.cs`: `Prewarm(키)` — 그 프리팹 풀이 없으면(쓴 적 없음) 하나 만들어 숨긴 채 넣는다. 풀 찾기 `PoolOf` 하나로.
- e.3 투사체: 풀 `CombatProjectilePool.Prewarm(count)`(만든 수가 모자라면 꺼진 채 만들어 넣음, 정리·해제 중이면 발사와 같은 예외), 비행 묶음 `Prewarm(prefab, count)`, 공격 재생 `Prewarm(공격 연출 값)`(투사체를 쏘는 종류만 — 판정은 `CombatUnitAnimationRules.FiresProjectile` 원본, 한 발).
- e.4 몸 위 글자: 풀 `CombatBodyTextPool.Prewarm(prefab, count)`(묶음마다 만든 수 `Created`, 생성은 대여와 같은 `CreateView` 하나), 창구 `CombatBodyTextWindow.Prewarm(prefabs)`(프리팹마다 2 — 상수 `PrewarmedPerPrefab`)·`DefaultPrefab`.
- e.5 `Playback/CombatSceneEventPresenter.cs`: `Prewarm(키 목록)` — 키마다 공격 연출 값(새 창구 입구 `CombatUnitPresentationWindow.AttackSettingsOf(key)`, 표시가 없어도 읽음)으로 투사체 한 발, 글자 창구가 있으면 `CombatCueSources.BodyTextPrefabs(기본 프리팹)`로 글자. 이를 위해 `_cueSources`·`_bodyText` 칸을 둠.
- e.6 `Display/Field/FieldCellFillStack.cs`: `Build` 끝에서 채우기 종류마다 깊이 0 판(`PlateOf(kind, 0)`)을 만든다. 칸이 없으면 빈 메시라 보이지 않는다(GlobalSystem GridCube가 빈 목록을 받음). 깊이 0 판의 구멍 지도라 깊이 1 칸 지도 텍스처도 이때 생긴다. `FieldOutlineRenderer`는 바꾸지 않음.
- 계획과 다른 점·알아 둘 것:
  - e.1의 "키별 만든 수는 `Release`에서 비움"은 틀린 적음이라 따르지 않았다. 표시 `Release`는 표시를 풀에 돌려줄 뿐 풀은 표시 집합 수명 동안 남으므로, 비우면 다시 설치할 때마다 이미 있는 만큼 또 만든다. 만든 수는 풀과 같이 비우지 않는다.
  - 투사체 수 1은 공격 재생, 글자 수 2는 글자 창구, 고스트 1은 고스트가 각자 갖는다(쓰는 꼴을 아는 곳). 셀 캐릭터와 키별 수만 설치 한 곳.
  - 같은 씬에서 표시를 다시 만들면(앞 표시 `Release`가 `ReleaseHudLayer`로 HUD를 지움) 풀에 남은 표시의 HUD는 지금처럼 처음 빌릴 때 만든다. 미리 만들기는 모자란 **표시** 수만 채운다.
  - 2연발처럼 같은 프리팹 투사체가 동시에 둘 날면 둘째는 지금처럼 그때 만든다. 소환 유닛·가시밭·특성 흔적·약한 지반 교체 외형·특성 추가 공격 덮어쓰기 투사체·이펙트는 설계대로 넣지 않았다.
  - 손패·적 대기 캐릭터의 표시와 HUD가 처음부터 있으므로, HUD 층이 매 프레임 도는 항목 수가 그만큼 늘어난다(숨은 항목은 비교만, GC-d와 같은 꼴).
- Unity 확인 Ue(사용자):
  1. 컴파일.
  2. 전투 진입 직후 하이어라키: 유닛 루트 아래 손패·적 대기 캐릭터 수만큼 꺼진 유닛 표시, HUD 층에 그만큼 숨은 HUD, 고스트 루트에 플레이어 키 프리팹마다 꺼진 고스트 하나, 투사체 꺼진 루트(`CombatProjectilePool_Inactive`)에 원거리 공격 프리팹마다 하나, 몸 위 글자 캔버스에 글자 프리팹마다 꺼진 글자 둘, 필드 칸 루트에 `FieldFill_*` 판(종류마다).
  3. 원점이나 화면 어디에도 유닛·HUD·고스트·투사체·글자가 잘못 보이지 않음.
  4. Profiler(Deep Profile 끈 채, `Instantiate`/`GC.Alloc`로 확인): 배치 국면 첫 손패 배치·첫 이동 Drag, 적 턴 첫 원거리 공격, 첫 특성 글자, 첫 범위·호버 표시 프레임에 유닛·고스트·투사체·글자·강조 판 생성이 없음. 적 대기에서 나오는 적도 생성 없음.
  5. 전투 설치(로딩) 시간이 조금 늘어남 — 늘어난 양 확인.
  6. 배치·이동·공격·피격·사망·특성 글자·HUD·고스트 색·범위 강조가 지금과 같음(미리 만든 표시를 처음 빌린 유닛 포함).

### 3.6 GC-f 표시 지우기·문서 완료 (2026-11-03 사용자 "Ue 문제 없음, GC-f 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- f.1 0단계 구간 표시 지우기(TP-8, Q-GH6 B): 넣은 줄만 되돌렸다.
  - `LifeCycles/Combat/Jobs/CombatAdvanceJob`·`CombatCommandJob`·`CombatPreviewJob`, `ViewSystem/Combat/CombatViewSystem`: 작업 트리 변경이 표시 줄뿐이라 그 변경을 거꾸로 적용 → **HEAD와 같다**(커밋할 것 없음).
  - `Display/Field/FieldOutlineRenderer`·`Display/Unit/FieldUnitPresenter`: `using Unity.Profiling;`·표시 필드·`Auto()` 감싸기만 지움. GC-b·GC-c·GC-e 변경은 그대로.
  - 다시 재야 하면 넣을 이름(옛 0단계): `Combat.Core.Advance`·`.Command`·`.Preview`(Job 셋의 Core 호출), `Combat.View.Tick`·`.Playback.Tick`·`.Input.Tick`·`.Result.BeforeResult`·`.Result.BeginPlayback`·`.Result.DisplayRefresh`·`.Result.AfterResult`(`CombatViewSystem`), `Combat.View.Field.Refresh`·`.Field.FillApply`(`FieldOutlineRenderer`), `Combat.View.Units.RenderFacts`·`.Units.Hud`(`FieldUnitPresenter`).
- f.2 문서:
  - 폴더 배치 안내: 갱신 줄, `Presentation/Pieces/` 줄에 `CombatEnumValues`, §2 새 줄 "자주 도는 길의 검사·그리기·생성"(열거 검사 도우미·강조는 프레임 끝·처음 생성은 `CombatSceneDisplay.Prewarm`·그리기 배열 재사용).
  - 설계 머리 상태 줄·새 §8 구현 메모.
  - 현강님 메모 [`handoffs/2026-10-22-combat-view-structure-hyeongang-share.md`](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) §21(GC-c `RenderUnitHud` 건너뛰기·GC-d `UnitHUDLayer`·남는 UISystem 매 프레임 쓰기, 미전달).
  - 다른 사람 줄 대조(blame): 실제로 고친 다른 작업자 줄은 현강님 `1013a4df`(HUD 레이아웃) 줄뿐 — `UnitHUDLayer`의 `RefreshSelectedInfo`·`RefreshPositions`, `FieldUnitPresenter.RenderUnitHud`. 호진님 `83fb2767`·천명님 `4782921e`로 나오는 줄은 저장소 전체(약 1.1만 파일)를 올린 커밋 표시라 그분들이 쓴 줄로 보지 않았다(천명님·호진님 메모 없음).
  - CLAUDE.md 현재 상태.
- 커밋 묶음(사용자): 새 `Presentation/Pieces/CombatEnumValues.cs` + `.meta`, 바뀐 View 파일들, 문서. 0단계를 따로 커밋하지 않았다면 표시 줄은 남지 않는다.
- f.3 묶음 끝 검토: 아래 §3.7.

### 3.7 GC-f 묶음 끝 검토 (2026-11-03, 새 에이전트 하나, 읽기만 → 사용자 "권장대로 반영해", 반영 기록 §3.8)

약 **9.6/10**(SSOT 9.75 · SOLID 9.75 · FW 9.75 · 독립성 9.5 · 캡슐화 9.75 · C#/Unity 9.5 · If/Tick 9.5). 높음·중간 0, 낮음 9. Framework·GlobalSystem·UISystem·Core·Factory 파일 무변경, 0단계 표시 남은 것 없음, 눈으로 본 컴파일 문제 없음.

| # | 내용 | 자리 | 권장 |
|---|---|---|---|
| L1 | HUD 층이 꺼졌다 켜지면 HUD 사실 기억이 남아 다음 체력 감소가 애니메이션 없이 줄어듦(§3.3 기록). §3.3의 "기준점 계약을 바꿔야 해서 안 함"은 틀림 — `IUnitHUDAnchor`·`UnitHUDLayer` 모두 우리 영역(`Display/Unit/`, 확인함) | `FieldUnitView`·`UnitHUDLayer.OnUIHidden` | **반영**: 층이 숨길 때 각 기준점에 알림(계약 메서드 하나) → `FieldUnitView`가 기억 비움 |
| L2 | 강조 그리기 실패가 F1이 아니라 콘솔로(§3.2 기록), `Repaint` 실패 때 같은 프레임의 전체 다시도 사라짐 | `FieldView.FlushPending` | 그대로(받아들인 차이). 바꾸려면 오류 경계 연결이 필요 |
| L3 | 재생 중이 아니면(편집 모드) `LateUpdate`가 돌지 않아 강조가 안 그려짐(추측, 지금 쓰는 곳 없음) | `FieldView.QueueRepaint`·`QueueRefresh` | **반영**: 바로 그리는 조건에 `!Application.isPlaying` |
| L4 | `FieldView.LateUpdate`보다 늦게 도는 `LateUpdate`의 변경은 한 프레임 늦음(§3.2 기록) | `FieldView` | **반영**: `[DefaultExecutionOrder]` 큰 값(이 클래스의 Unity 콜백은 `LateUpdate` 하나뿐) |
| L5 | 사건 Presenter가 미리 만들기만을 위해 칸 둘을 갖고, 글자 창구가 기본 프리팹을 내보냈다 돌려받음, 글자 프리팹 목록 계산이 Factory 검사와 두 번 | `CombatSceneEventPresenter`·`CombatBodyTextWindow` | 그대로(설치 때 한 번, 고치면 창구가 `Playback.Support`를 참조하게 됨) |
| L6 | 키별 만든 수가 HUD 유무를 모름(같은 씬 재설치 때 HUD 미리 만들기 빠짐, §3.5 기록), `CreateView`가 Instantiate 뒤 실패해도 수가 오름 | `FieldUnitPresenter` | 그대로(성능 손해만, 드묾) |
| L7 | `IsDefined<T>(int)`는 T와 값 형식이 묶이지 않음 | `CombatEnumValues` | 그대로(TP-1에서 고른 꼴) |
| L8 | HUD 기억 비우는 규칙이 두 클래스에 나뉨 | `FieldUnitView`·`FieldUnitPresenter` | **반영**: `FieldUnitView` 문서에 비우는 때 목록 한 곳 |
| L9 | `FlushPending`의 `_mapper == null` 갈래는 닿지 않음 | `FieldView` | **반영**: 지움(또는 방어용 주석) |

이미 적힌 것: 칸·칸 무관 변경이 같은 프레임이면 `Refresh` 두 번(§3.2).

### 3.8 묶음 끝 검토 권장 반영 (2026-11-03 사용자 "권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

반영 L1·L3·L4·L8·L9, 그대로 L2·L5·L6·L7.

- **L1** HUD 층 숨김 때 HUD 사실 기억 비움:
  - `Display/Unit/IUnitHUDAnchor.cs`: 계약 메서드 `OnHudLayerHidden()` 하나. 구현은 `FieldUnitView` 하나뿐이다(Assets 전체 대조, 시험 가짜 없음).
  - `Display/Unit/UnitHUDLayer.cs` `OnUIHidden`: `HideAll` 뒤 살아 있는 기준점마다 `OnHudLayerHidden()`. 필드만 비우므로 UISystem `HideAll`의 등록부 순회 중에 불려도 안전하다. `Release` 때는 등록이 먼저 비워져 부를 기준점이 없다.
  - 같은 파일: 기준점 생존 판정 `IsAlive`를 하나로 꺼내 `Register`·`RefreshPositions`·`OnUIHidden`이 부름(식은 그대로).
  - `Display/Unit/FieldUnitView.cs`: 명시적 구현 `IUnitHUDAnchor.OnHudLayerHidden() => _hudFacts = null`.
  - 확인: HUD는 층의 직접 자식이다(`Register` 검사). 층이 꺼지면 HUD의 `UIBase.OnDisable` → `UnitHUD.OnUIHidden`이 체력 상태를 지운다(§3.3 c.0).
  - 결과: 층이 다시 켜진 뒤 첫 그리기가 HUD를 모두 다시 써서, 그다음 체력 감소의 애니메이션이 GC-c 전과 같다. §3.3·§3.7 L1의 "남는 작은 차이"가 없어졌다.
- **L3** `Display/Field/FieldView.cs`: 바로 그리는 조건을 `DrawsAtFrameEnd`(켜져 있고 `Application.isPlaying`) 하나로 모았다. 편집 모드에서는 층 API가 바로 그린다. `QueueRepaint`·`QueueRefresh`가 이 조건을 부른다.
- **L4** 같은 파일: `[DefaultExecutionOrder(DrawOrder)]`, `DrawOrder = 10000`(저장소에 다른 `DefaultExecutionOrder` 없음). 다른 `LateUpdate`(예: `UIInputBinding`의 호버 해제)가 바꾼 강조도 같은 프레임에 그린다. 실행 순서가 정하는 이 컴포넌트의 콜백은 `LateUpdate` 하나다(`OnDestroy`·`OnValidate`는 순서와 무관).
- **L8** `FieldUnitView` `_hudFacts` 주석: 비우는 때 다섯을 한 목록으로 적었다. 붙이기·떼기, 다른 개체 묶기, HUD 숨김(`SyncHudActive`, `OnDisable`·풀 반환 포함), HUD 층 숨김(L1), 표시 집합 `Configure`·`ConfigureHUDCategories`.
- **L9** `FieldView.FlushPending`: `_mapper == null` 갈래를 지웠다. 문서에 불변식을 적었다: 층 API는 구성 전이면 대기에 넣지 않고, `Release`는 좌표 변환을 비우기 전에 대기를 버린다. `Build`의 실패 길도 `Release`를 거친다.
- 바뀐 동작:
  - 층이 다시 켜진 뒤 첫 그리기는 HUD 쓰기를 건너뛰지 않는다(전과 같음).
  - 편집 모드에서 강조가 바로 그려진다(지금 쓰는 곳 없음).
  - `FieldView.LateUpdate`가 다른 `LateUpdate`보다 늦게 돈다.
- 다른 사람 줄: `UnitHUDLayer.OnUIHidden`(현강님 `1013a4df` 줄 앞에 넣음)·`Register`·`RefreshPositions`의 기준점 검사 줄 → [현강님 메모](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) §21 덧붙임(미전달).
- Unity 확인 Uf(사용자):
  1. 컴파일.
  2. 유닛 호버·공격 조준·이동 Drag 강조가 지금처럼 바로 나오고 사라진다. 특히 UI 위로 포인터를 옮길 때 호버 칸이 한 프레임 늦게 지워지지 않아야 한다.
  3. HUD 층이 숨겨졌다 다시 나오는 경우(예: 화면 전환·결과 화면 뒤 같은 씬)에, 그 뒤 피격 체력바 애니메이션이 지금과 같다.
  4. 유닛 클릭 선택 정보 창, 유닛을 끌어 옮긴 뒤 HUD 내용이 같다.

### 3.9 원격 병합 (2026-11-03 사용자 "너 클라우드라서 푸쉬해줘야댐", 컴파일 미확인)

- 커밋 `bbbcb162`(GC-a~GC-f + §3.8) 뒤 `origin/feature/Yonggeun/Refector-Stage`(262커밋 앞섬)를 병합했다. 충돌 12개(코드 9·문서 3), 모두 양쪽을 살렸다.
- 코드 정리:
  - `FieldUnitPresenter`: 원격의 `Rent`·`CreateView` 실패 정리 구조를 그대로 두고 우리 `PoolOf`·미리 만들기를 얹었다. 키별 만든 수는 생성이 성공한 뒤에만 센다(검토 L6의 "실패해도 수가 오름"이 풀림). 원격이 최종 해제 `ReleasePools`에서 풀을 끝내므로 만든 수도 거기서 비운다(§3.5 "비우지 않음"을 바꿈). 하나만 버리는 `DiscardView`는 키를 몰라 빼지 않는다(미리 만들기가 하나 덜 만들 뿐).
  - `UnitHUDLayer`: 원격이 위치 쓰기를 `Project`로 묶어서, 바뀔 때만 쓰는 규칙(GH5)을 `Project` 안에 넣었다(위젯도 같은 규칙). 원격의 준비 중 갈래(`Preparing`)는 그대로, 기준점 검사는 `IsAlive`.
  - `FieldView`: 원격이 더한 강조 입구 둘(`SetHoverRangeSourceCells`·`SetAffectedCells`)이 그리기를 바로 불러, GH1대로 대기열(`QueueRefresh`·`QueueRepaint`)로 바꿨다. 원격 낙석 색 칸은 그대로.
  - `CombatSceneDisplay.Install`: 미리 만들기(GH6) → 등장 연출 준비(`_entryPresentation.Prepare`) 순서. 미리 만들기가 실패해도 준비된 등장 연출이 남지 않게.
  - `FieldUnitView`: 원격 등장 HUD 숨김(`SetEntryHudHidden`)은 `SyncHudActive`를 지나 HUD 사실 기억도 비운다(비우는 때 목록 3에 적음).
  - `CombatShownFacts`·`FieldOutlineRenderer`·`CombatSceneEventPresenter`·`FieldCellFillStack`: 원격 줄(위치 보류·열린 변·공격 모드 대상·구멍 모양) + 우리 줄.
  - GlobalSystem `AreaOutlineData`에 원격이 `opening`을 더했지만 메시는 그 자리에서 만들고 배열을 보관하지 않아 칸 배열 재사용(GH2)은 그대로 안전하다.
- 문서: 현강님 메모는 원격이 §19·§20을 먼저 써서 우리 절을 §21로 옮겼다(이 계획·설계·CLAUDE.md의 가리킴도 고침). 폴더 안내는 두 갱신 줄·`Pieces/` 줄을 합쳤다.
- **겹침(결정 필요, 보고만):** 원격 용근님 `7404e70e` "선택 유닛 사전 준비"(로딩 중 선택 출전 유닛의 모델·HUD를 한 Tick에 하나씩 준비)와 GH6 유닛 미리 만들기(설치 때 셀 캐릭터 전부의 모델·HUD)가 플레이어 유닛 일부를 같이 맡는다. 지금은 둘 다 둔다 — 설치 때 GH6가 먼저 만들고, 사전 준비는 그 표시를 빌려 HUD를 실제 캔버스에서 데운다(새로 만들지 않음). 원격 계측 `PreparedCapacityMiss`·`CreateOutsidePreparedCapacity`는 설치 때 GH6 생성을 "준비 밖"으로 센다. 한 곳으로 줄일지(예: GH6는 적·손패 밖만, 선택 유닛은 사전 준비만)는 사용자 결정.
- Unity 확인 Uf(§3.8)와 함께: 컴파일, Combat 진입 로딩·등장 연출 순서, 같은 씬 재진입 뒤 유닛·HUD.
