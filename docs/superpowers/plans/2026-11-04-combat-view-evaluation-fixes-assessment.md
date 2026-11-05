# 전투 View 평가 후속 — 중간 문제 29개 코드 확인 기록 (2026-11-04)

> [평가 보고서](../specs/2026-11-04-combat-view-evaluation.md)의 중간 문제 29개를 읽기 전용 검토자 4명이 지금 코드(`032608bf`)로 다시 확인한 기록이다. 수정 순서와 실행 기록(1차·2차 §1~§3, 3차·4차 §8)은 [후속 수정 계획](2026-11-04-combat-view-evaluation-fixes-plan.md)에 있다. 다음 작업의 순서와 묶음은 그 계획 §6이 원본이다(11-04 정리). 이 문서의 절 이름(3차·0단계 뒤·먼저 정할 것)은 문제를 나눈 묶음이고, C1-42·C2-04는 계획 §6.6 구조 단계로 옮겼다.
> 29개 모두 지금 코드에도 그대로 있었다. 줄 수는 1단계(가장 작은 안전한 수정) 기준 추정이다. "GC 작업 파일"은 아직 Unity에서 확인하지 않은 `bbbcb162`에서 바뀐 파일을 고친다는 뜻이다. 경로 `V/` = `ViewSystem/Combat/`, `S/` = `Assets/ProjectL/Script/Systems/`.

## 1차 (2026-11-04 코드 완료)

### G3-01

- 플레이어 영향: 특정 조작에서 남. 배치 결과 A가 재생되는 동안 유닛 B를 필드에서 집거나(TryBeginPlacement 258-263은 CanPreview만 봄) 덱 카드로 집어, 재생이 끝나기 전에 놓으면 난다. CanSubmit(81-84)이 IsUnpresented로 거절하고, CommitDrag(314-321)의 반환값은 버려진다. 이어서 SelectReleased(159-160)가 EndDrag를 무조건 불러 B가 안내 없이 원래 자리로 돌아간다. 빠르게 연속 배치하는 플레이어는 겪는다. 아이템은 CanSelect가 CanSubmit을 보므로 재생 중에는 집을 수 없다. 그래서 '집은 직후 이전 결과가 도착한' 좁은 틈에서만 난다.
- 가장 작은 수정(약 8줄): 1단계(권장 b안, 드래그 유지): 이미 있는 '클릭으로 집은 상태' 규칙을 그대로 쓴다. PlacementDragInteraction.SelectReleased에서 CommitDrag 전에 facts.IsPresenting이면 _pressStartedDrag=false로 두고 return한다. 그러면 EndDrag를 부르지 않아 유닛이 손에 남고, 다음 누름·뗌에서 놓는다(SelectPressed는 _dragging이면 return). CombatItemDragInteraction.SelectReleased에서도 End() 전에 facts.IsPresenting이면 return false로 드래그를 유지한다. 이 둘이면 조용한 소실은 사라진다. a안(대안): PlacementDragInteraction.CanSelectDeckUnit과 TryBeginPlacement의 CanPreview 뒤에 '&& !facts.IsPresenting'을 붙여, 재생 중에는 집기 자체를 막는다. CanInspectUnit(111줄)과 같은 선례다. 단 DeckCardList.Update가 매 프레임 CanSelect로 interactable을 다시 쓰므로, 배치 재생 동안 덱 카드가 회색으로 깜빡인다. 2단계: 놓기를 보류했다가 재생 확인(AfterAcknowledged) 뒤 자동 제출하는 방식은 대기열·사실 재검사가 필요하므로 지금은 하지 않는다.
- 위험 낮음: 갈래 하나를 더할 뿐이다. 놓은 뒤에도 고스트가 남는 상태는 클릭-집기 모드로 이미 있다. 재생이 끝나면 AfterResult가 놓을 수 없게 된 Drag(죽음·국면 변경)를 정리한다. 다른 호출처는 OwnerAdapter.SelectReleased 하나다. 다만 Router 241줄(아이템을 UI 위에 놓으면 취소)은 그대로 먼저 처리되므로 겹치지 않는다.
- Unity 확인: (1) 유닛 A를 놓고, A의 배치 결과가 재생되는 동안 유닛 B를 집어 초록 칸에 놓는다. B가 손에 남아 포인터를 따라오는지 본다. 재생이 끝난 뒤 한 번 더 클릭하면 B가 배치되는지 확인한다. (2) 덱 카드로 집은 경우도 같게 본다. (3) 우클릭·Esc로 남은 Drag가 취소되는지 본다. (4) 덱 패널이 Drag 위치에 머무는 모습이 어색하지 않은지 본다.
- 정할 것·공유: 재생 중에 놓은 것을 '손에 남김(b, 다시 클릭 필요)'으로 할지 '재생 중에는 집기 자체를 막음(a, 덱 카드가 잠깐 회색)'으로 할지 사용자가 정해야 한다. 안내음·문구를 더할지도 정한다.
- 파일: `V/Input/Interaction/PlacementDragInteraction.cs`(용근); `V/Input/Interaction/CombatItemDragInteraction.cs`(용근)
- 확신 높음

### C10-07

- 플레이어 영향: 특정 조작에서 남. 운영 씬은 snapCursorToGrid=1이다. 공격 모드·배치 Drag·경로 지정에서 아래쪽 칸이 덱 UI 밑으로 반쯤 들어가면(Pan·Zoom 위치에 따라) 커서가 UI 아래 칸 중심으로 끌려간다. 그러면 그 칸을 누를 수 없다. CombatCursorSnap 50-55는 포인터 위치의 UI만 본다. 65줄은 중심으로 바로 Warp한다. TryCellCenterToScreen(121-136)은 z<=0만 거른다.
- 가장 작은 수정(약 10줄): 1단계(한 번에): (1) CombatPointerMapper.TryCellCenterToScreen에서 point.z 검사 뒤 '!camera.pixelRect.Contains(new Vector2(point.x, point.y))'이면 false를 돌려준다. 그러면 화면 밖으로 Warp하지 않는다. 이 함수를 부르는 곳은 CombatSceneDisplay.TryCellCenterToScreen 하나뿐이고, 그것을 부르는 곳은 CursorSnap 하나다. (2) CombatCursorSnap.PointerMoved에서 TryCellCenterToScreen이 성공한 뒤, _mapping.TryMapPointer(center, out Vector2 centerGrid, out bool centerOverUi)를 한 번 더 부른다. 실패했거나, centerOverUi이거나, CellAt(centerGrid)!=cell이면 Warp하지 않는다. 이때 _snapped = cell로 두고 false를 돌려준다. 그러면 같은 칸 안에서는 다시 검사하지 않고, 칸의 보이는 부분에서 커서가 자유롭다. 추가 Raycast는 칸이 바뀌는 순간에만 한 번 돈다.
- 위험 낮음: Warp를 덜 하는 방향의 변경이고, 호출처가 각각 하나다. 가려지지 않은 칸의 스냅은 지금과 같다. 칸 중심이 UI 밑인 칸에서는 스냅이 일어나지 않는 것이 새 동작이다. 같은 칸 재검사 조건(CellAt(centerGrid)!=cell)은 고지대 칸 윗면 중심이 다른 칸으로 잡히는 경우도 막아 준다.
- Unity 확인: (1) 아래쪽 칸 줄이 덱 UI에 반쯤 가리도록 카메라를 옮긴다. 공격 모드에서 그 칸의 보이는 윗부분에 포인터를 넣었을 때 커서가 UI 밑으로 끌려가지 않고, 그 칸을 클릭할 수 있는지 본다. (2) 창 모드에서 화면 가장자리 칸의 중심이 화면 밖일 때 커서가 창 밖으로 나가지 않는지 본다. (3) 보통 칸에서는 스냅이 전처럼 되는지 본다.
- 파일: `V/Input/Assist/CombatCursorSnap.cs`(용근); `V/Display/Feedback/CombatPointerMapper.cs`(용근)
- 확신 높음

### C10-03

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 FallingRocks 프리팹은 약 1초 뒤 스스로 끝나서 진입이 멈추지 않는다. 낙석 연출을 다듬으려고 프리팹을 Looping으로 바꾸거나 ParticleStopNotifier를 빼면, 적 Nexus가 있는 모든 스테이지가 진입에서 영구히 멈춘다(입력·명령·포기 모두 막힘).
- 가장 작은 수정(약 8줄): V/Presentation/Entry/CombatEntryNexusAppearance.cs 한 파일만 고친다. (1) 상수 `ImpactWaitLimitSeconds = 3f`(착지 뒤 이펙트를 기다리는 상한)와 경고를 한 번만 남기기 위한 bool을 둔다. (2) Tick 끝의 `complete &= target.Landed && !target.Impact.IsLive;`를 `target.Landed && (!target.Impact.IsLive || _elapsed - _settings.Seconds >= ImpactWaitLimitSeconds)`로 바꾸고, 상한에 걸리면 Debug.LogWarning을 한 번 남긴다. 착지만으로 완료하는 방법은 쓰지 않는다. CombatBoardEntryPresentation.Complete()가 곧바로 Cancel()을 부르고, Cancel이 Impact.RecallImmediately로 이펙트를 즉시 거둔다. 그래서 적이 없는 스테이지에서는 착지하는 프레임에 이펙트가 잘린다. 상한 방식이면 평소 1초짜리 이펙트는 지금처럼 끝까지 나오고, 끝나지 않는 이펙트만 3초 뒤 Cancel이 거둔다. 설정 값으로 빼려면 CombatNexusDropSettings에 필드를 더할 수 있지만, 그러면 에셋 기본값까지 다뤄야 해서 1단계는 상수로 둔다.
- 위험 낮음: 완료 조건 한 줄에 '또는 상한'만 더하므로, 이펙트가 상한 안에 끝나는 지금 에셋에서는 동작이 바뀌지 않는다. IsComplete를 읽는 곳은 CombatBoardEntryPresentation 두 군데(127·151줄)뿐이다. 상한을 넘었을 때만 Cancel의 RecallImmediately가 남은 이펙트를 거두는데, 이 경로는 원래 있던 정리 경로다.
- Unity 확인: 적 Nexus가 있는 스테이지에 들어가 Nexus 낙하, 착지 이펙트(약 1초), 적 등장 순서와 박자가 전과 같은지 본다. 원하면 FallingRocks 루트 파티클을 잠시 Looping으로 바꿔, 착지 약 3초 뒤 경고 한 줄과 함께 진입이 이어지는지 확인하고 되돌린다.
- 정할 것·공유: 상한 길이(제안 3초)와, 상수로 둘지 CombatNexusDropSettings 값으로 뺄지.
- 파일: `V/Presentation/Entry/CombatEntryNexusAppearance.cs`(용근(110-123줄 전부))
- 확신 높음

### C11-02

- 플레이어 영향: 특정 조작에서 남. 필드 유닛이 많고 연쇄 이동·다단 사건이 이어지는 결과 재생에서 Render마다 유닛 수만큼 Capture가 할당돼 GC 끊김이 생길 수 있다. 평소 한두 사건짜리 결과에서는 체감하기 어렵다. HUD 쪽 중복 할당은 GH3(같은 사실 객체면 건너뜀)로 이미 줄었다.
- 가장 작은 수정(약 20줄): V/Presentation/Effects/CombatStatusEffectSource.cs의 Collect만 고친다. GH3와 같은 근거(CharacterCombatFacts는 불변이고 CombatShownFacts는 바뀐 유닛만 새 객체로 바꿈, CombatShownFacts.cs:106-108)로 '사실 객체 참조 → Capture 결과'를 기억한다. 사전 두 개(`_read`, `_next`: EntityId → (CharacterCombatFacts facts, IReadOnlyList<StatusEffectSnapshot> statuses))를 두고, Collect 시작에 `_next.Clear()`를 부른다. 유닛마다 `_read`에 같은 참조(ReferenceEquals)가 있으면 그 결과를 다시 쓰고, 없으면 Capture한다. 결과는 `_next`에 넣고, 반복이 끝나면 두 사전을 바꾼다. 이번 Render에 없는 유닛은 자연히 빠지므로 따로 정리할 필요가 없다. HUD(CombatUnitStatuses.AddTo)와 결과를 나눠 쓰는 2단계는 GH3 덕분에 이득이 작아 하지 않는다.
- 위험 낮음: Capture(unit)는 unit 하나만 입력으로 받으므로 같은 불변 객체면 결과도 같다. 같은 가정을 FieldUnitView의 GH3가 이미 쓰고 있다. Collect를 부르는 곳은 CombatPersistentEffects.Sync 하나다. 반복 중 예외가 나도 사전을 바꾸기 전이라, 다음 호출이 `_next`를 비우고 다시 시작한다. Core StatusEffectReadModel은 건드리지 않는다.
- Unity 확인: 상태이상(보호막·화상 등) 유지 이펙트가 HUD 아이콘과 같은 순간에 켜지고 꺼지는지 본다. 보호막이 소진되면 이펙트가 꺼지는지 본다. 유닛 여럿이 연쇄 이동하는 결과에서 Profiler의 GC Alloc이 줄었는지 본다.
- 파일: `V/Presentation/Effects/CombatStatusEffectSource.cs`(용근(Claude)(20-45줄 전부))
- 확신 높음

## 2차 (2026-11-04 코드 완료)

### C10-01

- 플레이어 영향: 특정 조작에서 남. 공격 모드나 배치 Drag 중에 마우스는 그대로 두고 키·휠·화면 가장자리로 시점을 옮긴 뒤 바로 클릭할 때만 난다. CombatCameraInput.Tick(75-84)은 Pan·Zoom만 하고 포인터 소비자에게 알리지 않는다. AttackTargetingInteraction.PointerMoved(144-165)는 PointerMoved가 올 때만 칸을 다시 집는다. 반면 SelectReleased(175-181)는 뗀 좌표로 칸을 새로 집어 SubmitTarget을 부른다. 가장자리 스크롤은 포인터를 가장자리에 둔 채 움직이므로 미리보기가 옛 칸에 남는 일은 자주 생긴다. 다만 그 상태로 클릭까지 해야 엉뚱한 칸이 제출된다.
- 가장 작은 수정(약 35줄): 1단계(한 번에): '시점이 바뀐 프레임에 마지막 포인터로 PointerMoved를 한 번 다시 보내기'. (1) ICombatCameraControl.Pan의 반환형을 bool로 바꿔 '움직였는지'를 돌려주고, float ZoomSeconds { get; }를 더한다. 구현은 CombatCameraView 하나뿐이고, 이미 offset==_offset이면 return하므로 Place() 뒤 true만 돌려주면 된다. (2) CombatCameraInput.Tick이 bool을 돌려준다. 이번 프레임에 Pan이 움직였거나 Zoom이 true면 참이다. Zoom이 성공하면 _zoomSettle = ZoomSeconds로 두고 Tick마다 deltaTime만큼 줄인다. 줌 렌즈는 효과 서비스가 ZoomSeconds 동안 보간하므로, 값이 남아 있는 동안과 그다음 한 프레임도 참으로 둔다. 마지막 포인터를 꺼내는 TryGetPointer(out x, out y)도 더한다. _hasPointer·_pointer는 이미 있다. (3) CombatInputRouter에 RefreshPointer(x, y, characters)를 더한다. 이 메서드는 RoutePointer(Request.PointerMoved, …)만 부르고 커서 스냅은 부르지 않는다. 연속 Pan 중에 Warp가 일어나면 커서가 튀기 때문이다. (4) CombatSceneInput.TickCamera에 characters 인자를 더한다. _camera.Tick이 참이고 TryGetPointer가 성공하면 _router.RefreshPointer를 부른다. (5) CombatViewSystem.Tick 313줄 호출에 _characters.Characters를 넘긴다. 이렇게 하면 공격 호버·배치 고스트·아이템 호버·유닛 호버(배경 ObservePointer)가 함께 새 칸을 집는다. 환경 호버는 원래 매 프레임 다시 집는다. 2단계 이후: C7-02의 프레임 포인터 표본으로 옮기면, 다시 보내기 대신 '표본이 낡았다' 표시 하나로 바꿀 수 있다.
- 위험 중간: 새로 집는 경로가 Router.RoutePointer 전체를 지나므로, 의도하지 않은 소유자도 시점 변화에 반응한다. 이동 경로 Drag 중 가장자리 스크롤을 하면 경로가 포인터 아래 칸으로 늘어난다. 공격 모드에서 자기 유닛을 누른 채 Pan하면 CombatAttackDragGesture.TryPromote가 칸이 바뀐 것으로 보고 이동 Drag로 승격한다. 둘 다 '포인터가 가리키는 칸'과는 맞는 동작이지만 지금과 다르다. Pan 호출처는 CombatCameraInput 하나이고 ICombatCameraControl 구현도 CombatCameraView 하나라 서명 변경의 파급은 작다. Tick 안에서 돌므로 F1 오류 경계 안이다.
- Unity 확인: (1) 공격 모드에서 적 A에 포인터를 둔다. 마우스는 두고 D 키·휠·가장자리로 시점을 옮긴다. 영향 칸·예상 체력이 새 칸으로 따라오는지 보고, 클릭하면 보이는 그 칸이 공격되는지 확인한다. (2) 배치 Drag 중에도 같게 하여 고스트가 포인터 아래 칸을 따라가고, 놓으면 그 칸에 배치되는지 본다. (3) 줌 보간(ZoomSeconds)이 끝난 뒤에도 호버가 맞는 칸에 있는지 본다. (4) 이동 경로를 끌면서 가장자리 스크롤을 할 때, 자기 유닛을 누른 채 Pan할 때의 동작이 받아들일 만한지 본다. (5) 시점 이동 중에 커서가 튀지 않는지(스냅 미호출) 본다.
- 정할 것·공유: 시점이 바뀔 때 다시 집기를 경로 Drag·공격 Drag 후보 승격에도 적용할지 정해야 한다. 그대로 두면 Pan만으로 경로가 늘거나 이동 Drag가 시작된다. 한정하려면 RefreshPointer에서 Path 대상과 TryHandleAttackDragPointer를 건너뛰면 된다. Router는 천명님 파일이므로 메서드를 더하면 공유 메모가 필요하다.
- 파일: `V/Input/Contracts/ICombatCameraControl.cs`(용근); `V/Presentation/Cameras/CombatCameraView.cs`(용근); `V/Input/Cameras/CombatCameraInput.cs`(용근); `V/Input/CombatInputRouter.cs`(천명님(RoutePointer 원작성), 새 메서드만 추가); `V/Input/CombatSceneInput.cs`(용근); `V/CombatViewSystem.cs`(용근)
- 함께 고칠 것 C7-02 · 확신 높음

### C7-02

- 플레이어 영향: 지금 데이터에서는 잠복. 정확성 문제가 아니라 비용 문제다. PointerMoved 한 번에 Router 231, 유닛 호버 Pick(CombatUnitRangeHover 183), RouteInput 뒤와 Tick 끝의 SyncEnvironmentHover(CombatEnvironmentHover 116), 경로·Drag 후보의 TryMapPointer→HitsUi가 같은 좌표로 RaycastAll을 4~5번 한다. 환경 호버는 마우스가 멈춰 있어도 매 프레임 한 번 한다. 지금 UI 양에서 끊김이 보고되지는 않았다. C10-01을 고치면 시점 이동 중에도 다시 집기가 매 프레임 돌므로 비용이 더 는다.
- 가장 작은 수정(약 15줄): 1단계(한 번에, 한 파일): CombatPointerMapper.HitsUi 입구에 '같은 프레임·같은 좌표' 기억을 둔다. 필드 세 개(_uiCacheFrame=Time.frameCount, _uiCacheScreen, _uiCacheHit)를 두고, 프레임과 좌표가 같고 EventSystem이 같으면 Raycast 없이 기억한 값을 돌려준다. IsPointerOverUi·TryMapPointer가 모두 HitsUi를 지나므로 소비자는 고칠 필요가 없다. 칸 집기(TryPickCell)는 기억하지 않는다. 같은 프레임 안에서 Pan(Place)이 카메라를 옮기므로 낡은 칸이 나올 수 있기 때문이다. 덧붙여 CombatInputRouter.RoutePointer에서 PointerMoved일 때는 blockedByUi를 공격 Drag 후보가 있을 때만 구하는 지연 계산으로 바꿀 수 있다. 다만 천명님 줄이라 2단계로 미룬다. 2단계 이후: 프레임 포인터 표본(UI 가림·칸·격자점)을 한 곳에서 만들어 Router·유닛 호버·환경 호버·스냅·경로가 읽게 한다. 이것은 CombatEnvironmentHover·CombatPathInput(bbbcb162 파일)을 고친다.
- 위험 낮음: 같은 프레임 안에서 UI 구성이 바뀌면 그 프레임 남은 동안 낡은 답을 줄 수 있다. 예를 들어 SelectPressed가 정보창을 열면 같은 프레임 뒤쪽의 환경 호버가 그 창을 못 본다. 그 영향은 한 프레임뿐이다. 버튼 클릭으로 버튼이 숨는 경우에는 오히려 같은 프레임의 '위에 UI 있음' 답이 유지되어 B5 막기가 더 안정된다. 칸 집기는 기억하지 않으므로 카메라 이동과 충돌하지 않는다.
- Unity 확인: (1) 마우스를 HUD·덱·가방·정보창 위아래로 움직이며, UI 위 클릭이 필드로 새지 않는지 본다(공격 버튼 클릭 뒤 칸 공격 안 됨). (2) 환경 호버가 UI 위에서 숨는지 본다. (3) Profiler에서 마우스를 움직이는 프레임의 EventSystem.RaycastAll 호출 수가 1~2번으로 주는지 본다.
- 파일: `V/Display/Feedback/CombatPointerMapper.cs`(용근(HitsUi 본체), 현강님(통과 계약 검사 9줄 — 바꾸지 않고 앞에 기억 갈래만 추가))
- 함께 고칠 것 C10-01 · 확신 중간

## 3차: 동작이 그대로인 정리 (2026-11-04 코드 완료, 계획 §8)

### C2-02

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금 RouteInput의 정보창 차단 조건(Drag·IsAttackActive·IsPathPlanning)은 IsInputOwnerActive와 결과가 같다. 전경 소유자는 넘겨받기(IsActive 기본 false)·경로·아이템·배치·공격이고 호버는 배경이다. 그래서 지금 플레이어가 겪는 증상은 없다. 새 전경 소유자를 더할 때만 정보창이 함께 열린다.
- 가장 작은 수정(약 4줄): 1단계(한 번에 할 것): V/CombatViewSystem.cs RouteInput(1103-1107)에서 `connection.Input.Interaction.Drag == DragKind.None && !IsAttackActive && !IsPathPlanning` 세 줄을 `!connection.Input.Interaction.IsInputOwnerActive` 한 줄로 바꾼다. 동작은 같고, 새 소유자가 생기면 자동으로 따라간다(CombatInteraction.cs:136-147). 2단계 이후: C8-01과 함께 CombatSceneInput에 묶음 입구(CanInspectOnPress·BeforeResult·AfterResult·Tick)를 두고, 결과 소비(831-911)·입력 분배(RouteInput)·Tick 끝 표면 파생(Sync*)을 연결 수명 객체로 옮긴다. 조정자에는 수명과 순서만 남긴다. 2단계는 수백 줄이고, CanContinue 31곳과 ReferenceEquals(_connection) 16곳의 재확인 줄을 함께 옮겨야 해서 따로 설계부터 한다.
- 위험 낮음: 1단계는 같은 값을 다른 식으로 읽을 뿐이다. 소유자 목록 6개의 IsActive를 직접 확인했다(경로=_path.IsPlanning, 아이템=_item.IsDragging, 배치=_placement.IsDragging, 공격=_attack.IsActive, 넘겨받기=false, 호버=배경). 호출처는 RouteInput 한 곳이다. 2단계는 연결 분리와 재확인 순서를 건드려서 위험이 높다.
- Unity 확인: 배치 드래그, 아이템 드래그, 공격 모드, 경로 지정 중에 각각 다른 유닛을 클릭해 정보창이 열리지 않는지 본다. 아무 모드도 아닐 때 유닛을 클릭하면 정보창이 열리는지 본다. 공격 드래그에서 경로로 넘어가는 몸짓도 한 번 해 본다.
- 정할 것·공유: 2단계(조정자 나누기)를 할지와 언제 할지는 사용자가 정한다. 1단계는 결정 없이 해도 된다.
- 파일: `V/CombatViewSystem.cs`(용근(1103-1109 전부, 파일 1160/1179줄))
- 함께 고칠 것 C8-01·C4-13 · 확신 높음

### C8-01

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금은 CombatViewSystem 1104-1107의 손 조합(Drag==None && !IsAttackActive && !IsPathPlanning)이 CombatInteraction.IsInputOwnerActive와 정확히 같다. 넘겨받기 소유자는 IsActive를 재정의하지 않아 늘 false다. 그래서 플레이어가 겪을 일은 없다. 새 전경 소유자를 더할 때만 정보창이 끼어드는 실수를 부른다.
- 가장 작은 수정(약 25줄): 1단계(한 번에, 동작 변화 없음): CombatSceneInput에 묶음 수준 입구를 더한다. CanInspectOnPress => !Interaction.IsInputOwnerActive, BeforeResult(), AfterResult(facts, characters), AfterAcknowledged(), TickInteraction(facts, characters, latestPreview, answered, readAnswerFacts)를 두고, 각각 Interaction에 그대로 넘긴다. CombatViewSystem의 405·840·896·909줄은 새 입구를 부르게 바꾸고, 1104-1107의 깃발 셋은 connection.Input.CanInspectOnPress 하나로 바꾼다. 2단계 이후: Interaction·Gate·ActionAvailability·MovementUndo 속성을 private으로 내린다. CombatViewSystem(1053·888·1143-1144·1175-1176), CombatInputFactsReader(68·70대 Interaction.IsAttackActive/IsItemDragActive/HasCurrentAcceptedEmptyMovementRange)에는 대응 입구를 만든다. 확인창 HasUnplacedUnits·CountIdleUnits는 private으로 바꾼다. 이 단계는 천명님 줄(CombatSceneInput 63-112)을 바꾸므로 공유 메모가 필요하다.
- 위험 낮음: 1단계는 위임만 더하고 호출 자리를 바꾸므로 순서·조건이 그대로다. CanInspectOnPress는 오늘 소유자 목록(넘겨받기 IsActive=false, 경로 IsActive=IsPlanning, 배치·아이템 IsDragging, 공격 IsActive) 기준으로 기존 식과 같은 값이다. 2단계는 Reader·Surfaces까지 고쳐 파급이 더 크다.
- Unity 확인: (1) 배치·전투에서 빈 상태로 유닛을 클릭하면 정보창이 열리는지 본다. (2) 공격 모드·경로 지정·배치 Drag·아이템 Drag 중의 클릭에는 정보창이 끼어들지 않는지 본다. (3) 결과 재생 전후로 공격 모드 유지·종료, 경로 취소, 복귀 견적이 전과 같은지 본다.
- 정할 것·공유: 2단계(부품 속성 숨기기)를 할지, 언제 할지 정한다. CombatSceneInput 속성 줄은 천명님(천명님)·현강님 작성이라 공유가 필요하다.
- 파일: `V/Input/CombatSceneInput.cs`(용근(새 멤버 추가), 2단계 속성 가시성 변경은 천명님·현강님 줄); `V/CombatViewSystem.cs`(용근)
- 함께 고칠 것 C4-13·C2-02 · 확신 높음

### C4-08

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 운영 호출처는 CombatAttackStart.cs:99·131·134 세 곳이고 모든 인자를 넘긴다. 다음에 창구를 더하다 한 입구에서 빠뜨릴 때에만 조용히 사라진다.
- 가장 작은 수정(약 6줄): 1단계(가장 작음): CombatClipAttack.ComposeMelee·ComposeRanged와 CombatCodeMotionAttack.Compose에서 effects·camera·actorBody·projectileSounds·lead의 기본값 `= null`을 지워 필수 인자로 만든다. 그러면 창구를 하나 더할 때 빠뜨린 입구가 컴파일에서 걸린다. 호출처 세 곳은 이미 모든 값을 위치 순서대로 넘겨 바꿀 필요가 없다. 같은 Assets 안에 다른 호출처나 시험 코드는 없다(grep 확인). 2단계: CombatAttackStart가 공격마다 CombatCueSinks를 한 번 만들어 세 입구에 묶음 하나로 넘긴다. 원거리는 CombatCueSinks에 `WithProjectileTimings(timings)` 사본 메서드를 두어 붙인다. 지금 근접 클립은 projectileSounds를 null로 넘기므로, 묶음을 만들 때 근접 클립에는 투사체 소리를 넣지 않는다는 지금 동작을 지켜야 한다.
- 위험 낮음: 1단계는 기본값만 지우는 것이라 실행 동작은 하나도 바뀌지 않는다. 컴파일이 통과하면 끝이다. 2단계는 약 40줄이고, 근접 클립과 코드 모션 사이의 projectileSounds 차이를 지켜야 해서 위험이 조금 더 있다. 바뀌는 파일은 모두 bbbcb162 밖이다.
- Unity 확인: 1단계는 컴파일만 확인하면 된다. 2단계를 하면 클립 근접, 클립 원거리, 클립 없는 원거리(코드 모션) 공격 하나씩에서 이펙트·화면 흔들림·투사체 비행/착탄 소리가 전과 같이 나는지 본다.
- 파일: `V/Presentation/Pieces/CombatClipAttack.cs`(용근); `V/Presentation/Pieces/CombatCodeMotionAttack.cs`(용근); `V/Presentation/Pieces/CombatCueComposer.cs`(용근, 용근(Claude)); `V/Playback/Support/CombatAttackStart.cs`(용근)
- 확신 높음

### C5-02

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 운영 조립(CombatSceneInputFactory 78-82·150-161)은 늘 null이 아닌 값을 넘긴다. 그래서 지금 플레이어 영향은 없다. 다만 새 조립 경로가 actionAvailability를 빠뜨리면 AttackTargetingInteraction 112줄이 null을 '공격 가능'으로 읽는다. 그러면 사전 거절·거절 안내·궤적이 오류 없이 사라진다.
- 가장 작은 수정(약 35줄): 1단계(한 번에, 생성자 셋): (1) AttackTargetingInteraction 생성자(76-78)의 sounds·rejections·trajectory·actionAvailability에서 '= null'을 없애고 '?? throw new ArgumentNullException'으로 막는다. 112줄의 '_actionAvailability == null ||', 122·354·356·385·477·483·587의 '?.', 170의 '_sounds != null'을 지운다. (2) CombatInputRouter 생성자(77-78)의 cursorSnap·occlusion·reportBlockedInput·movementUndo를 필수로 하고 throw한다. 144·198·207·220·231·237의 '?.'·'!= null' 갈래를 지운다. (3) PlacementDragInteraction 생성자(57)의 sounds를 필수로 하고 '_sounds?.' 3곳을 지운다. Factory 호출은 이미 값을 모두 넘기므로 그대로 컴파일된다. 생성자 호출처는 각각 Factory 한 곳이고, Assets/Tests에는 없다. 2단계 이후: CombatPathInput·CombatMovementInteraction의 rejections·actionAvailability(bbbcb162 파일), 두 확인창의 presentation, CombatInteraction의 item·attackDrag·rangeHover도 필수로 바꾼다. path는 Stop 뒤 null이 되므로 null 갈래를 남긴다. 외부 호출이 없는 CombatInteraction.Poll(latestPreview)는 private으로 내린다. 안에서 부르므로 지우지는 않는다. CombatInputFacts의 선택 인자는 Assets/Tests/Editor/CombatField/StructureHitPresentationTests.cs:168이 이름 인자로 부르므로 함께 고쳐야 한다.
- 위험 낮음: 주로 컴파일 시점에 드러나는 변경이다. 운영 조립이 넘기는 값이 null이 아님을 확인했다. display는 비어 있지 않고, notices는 new로 만들고, CreateCursorSnap도 new로 만든다. sources.ReportBlockedInput은 CombatSceneInputSources:60에서 필수이고, movementUndo도 new로 만든다. 그래서 새 throw가 운영에서 날 일은 없다. 지울 갈래는 기계적이지만 줄 수가 많아 하나를 빠뜨려도 동작은 같다.
- Unity 확인: (1) 전투 씬에 진입했을 때 F1(ArgumentNullException)이 나지 않는지 본다. (2) 공격 모드에서 행동이 끝난 유닛의 공격 사전 거절 안내, 막힌 궤적, 취소음이 나오는지 본다. (3) 튜토리얼 제한에서 막힘 안내가 나오는지 본다. (4) 이동 되돌리기(Return), 커서 스냅이 전과 같은지 본다.
- 정할 것·공유: Router 생성자·RoutePointer 줄은 천명님 작성이라 공유 메모가 필요하다. 2단계에서 CombatPathInput(bbbcb162 파일)과 Tests 파일을 언제 건드릴지 정한다.
- 파일: `V/Input/Interaction/AttackTargetingInteraction.cs`(용근·용근(Claude)); `V/Input/CombatInputRouter.cs`(천명님(생성자·RoutePointer 대부분), 용근); `V/Input/Interaction/PlacementDragInteraction.cs`(용근); `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`(용근)
- 함께 고칠 것 C4-08·C8-02 · 확신 높음

### C3-01

- 플레이어 영향: 지금 데이터에서는 잠복. Core의 TornadoAreaFact.DestinationCells와 View가 다시 계산한 값은 지금 같다. Warning이면 LockedDirection이 있고, 방향이 Zero이면 WarningDirection이 null이라 View도 Zero를 넣는다. Core가 이동 계산이나 기믹 정의 순서를 바꿀 때에만 도착 영역이 실제와 어긋나거나 Member()에서 예외(F1)가 난다.
- 가장 작은 수정(약 20줄): 1단계: V/Input/Assist/CombatTornadoDestinationCells.cs의 Collect에서 22-27줄(Member 찾기 → TornadoGeometry.Travel → Footprint)을 `foreach (CellCoord cell in area.DestinationCells) if (!into.Contains(cell)) into.Add(cell);` 한 줄로 바꾼다. 앞의 IsActive·Warning·클릭 칸 포함 검사는 그대로 둔다. private Member()와 이제 안 쓰는 using(Commands.StageGimmicks)도 지운다. 호출처는 CombatEnvironmentHover.cs:95 하나뿐이고 서명은 그대로다. CombatEnvironmentDescriptions.Tornado의 정의 찾기는 설명 문구에 필요한 멤버 값(이동 주기 등)을 읽는 것이라 규칙 사본이 아니다. 그래서 1단계에서는 건드리지 않는다.
- 위험 낮음: 줄이기만 하는 수정이고 입력과 출력 모양이 같다. 지금 데이터에서는 결과가 Core와 같다. 정의를 못 찾을 때 예외가 나던 길이 사라질 뿐이다. 파일 하나만 바뀌고, 이 파일은 bbbcb162에서 바뀌지 않았다.
- Unity 확인: 토네이도 스테이지에서 Warning(예고) 상태의 토네이도 칸을 클릭한다. 고정 표시되는 도착 영역이 실제로 다음 이동 뒤 토네이도가 선 칸과 같은지 본다. 보드 가장자리에서 막히는 경우와 방향 없이 제자리에 서는 경우도 하나씩 본다.
- 파일: `V/Input/Assist/CombatTornadoDestinationCells.cs`(용근)
- 확신 높음

### C1-05

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 여섯 곳 모두 지금은 `Active && IsKnownToPlayer`로 같다. 공개 정책을 바꾸면서 한 곳을 빠뜨릴 때에만 그리지 않은 지뢰 범위나 흔적이 호버나 설명창에 나온다.
- 가장 작은 수정(약 14줄): 1단계: V/Display/Map/CombatEnvironmentFacts.cs에 `internal static bool IsShown(MovementTileFact source) => source.Active && IsKnownToPlayer(source);`를 더한다(null 검사는 IsKnownToPlayer에 있다). 여섯 곳을 이 함수로 바꾼다: FieldTrailPresenter.cs:142(IsShown 본문), ObjectViewSet.cs:52, TerrainViewSet.cs:224-225(IsShownFact 본문 또는 함수 자체), CombatEnvironmentText.cs:53, CombatDustScreenHoverCells.cs:33, CombatHazardLookup.cs:41. '유일한 판단' 주석 두 곳(Trail·Terrain)은 Facts를 가리키게 고친다. 2단계 이후: 'Object 분류와 원본 형식이 다르면 예외', '살아 있고 칸을 점유한 장애물' 판정의 사본(EnvironmentText·HazardLookup·ObjectViewSet)도 Facts로 모은다. 이건 함수 모양이 서로 조금씩 달라 따로 한다.
- 위험 낮음: 같은 식을 함수 하나로 바꾸는 것이라 동작이 그대로다. TraitTrailFact·ObjectSourceFact는 모두 MovementTileFact로 IsKnownToPlayer에 이미 넘겨지고 있고, Active도 MovementTileFact에 있다(HazardLookup이 tile.Active를 쓴다). 파일 7개가 바뀌지만 모두 한 줄씩이다. bbbcb162에서 바뀐 파일은 하나도 없다(그 커밋은 CombatEnvironmentHover만 고쳤다).
- Unity 확인: 컴파일만 통과하면 동작은 같다. 확인은 가볍게 한다: 공개 전 지뢰는 칸에도, 호버 위험 범위에도 안 나오는지 본다. 공개된 지형·오브젝트·흔적과 먼지막 호버가 전과 같은지, 환경 설명창이 그려진 것만 설명하는지 본다.
- 정할 것·공유: Facts·ObjectViewSet·EnvironmentText·HazardLookup의 바꿀 줄 작성자가 호진님(호진님)이라 공유 메모가 필요하다(한 줄 교체라 사후 공유로 충분할 것).
- 파일: `V/Display/Map/CombatEnvironmentFacts.cs`(호진님); `V/Display/Map/FieldTrailPresenter.cs`(용근); `V/Display/Map/ObjectViewSet.cs`(호진님); `V/Display/Map/TerrainViewSet.cs`(용근); `V/Input/Assist/CombatEnvironmentText.cs`(호진님); `V/Input/Assist/CombatDustScreenHoverCells.cs`(용근); `V/Input/Interaction/CombatHazardLookup.cs`(호진님)
- 확신 높음

### C1-11

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 계획이 결과 전체를 미리 구간으로 나눈다. 그래서 끊는 사건이 걸음 i와 i+1 사이에 있으면 걸음 i가 IsLast가 되고, 레인은 그때 이어갈 도약을 쥐지 않는다. 결국 레인 쪽 EndLeap 조건은 지금 안전망일 뿐이다. 둘이 다르게 바뀔 때에만 BeginStep 예외(F1)가 난다.
- 가장 작은 수정(약 20줄): 1단계: CombatLeapPlan에 `internal static bool EndsLeap(CombatEvent item, EntityId leaper)`를 둔다. 내용은 세 가지다. (1) IsActionStart || OpensAttackSource || CombatPullEvent이면 끊는다. (2) CombatCharacterEvent 중 Kind != Moved이고 Relocated·Returned이면(모든 유닛) 끊는다. (3) 그 밖에 Final.EntityId == leaper이고 사망·필드 이탈·앵커 변경이면 끊는다. Plan.Build는 60줄 검사와 77-81줄 검사를 이 함수 하나로 바꾸고(leaper = run.Count > 0 ? run[0].Expected.EntityId : default), Moved 처리는 지금대로 아래에 둔다. 레인 CombatUnitMotionPlayback.BeforeEvent 307-317줄도 이 함수를 부르고, 그 뒤 HealthChanged/Normalized → PauseLeap 분기만 남긴다. Moved 사건은 PresentEvent가 아니라 BeginStep 길로 오므로 레인이 Moved를 보지 않는다. 그래서 Kind != Moved 제한과 맞는다. 레인이 계획의 넓은 범위(다른 유닛의 Relocated·Returned)를 따르게 되지만, 그런 사건 앞 걸음은 이미 IsLast라 실제 동작은 바뀌지 않는다.
- 위험 낮음: 끊는 범위는 계획 기준(더 넓은 쪽)으로 하나가 된다. 계획은 지금과 같은 조건을 그대로 쓰고, 레인이 넓어지는 차이는 이미 IsLast로 막혀 있다. 다만 Build 안에서 leaper가 비었을 때(run 비어 있음, pending만 있음) Relocated·Returned에서 CloseRun이 예외를 던지는 지금 동작을 그대로 지켜야 한다. bbbcb162에서 바뀐 파일은 없다.
- Unity 확인: 뛰어넘기(Leap) 특성 유닛으로 장애물을 여러 칸 넘는 이동을 한다. 공중 경유가 이어지는지, 착지가 정상인지 본다. 도약 중간 칸에서 피격, 사망, 밀림, 행동 시작이 이어지는 경우가 있으면 하나씩 본다.
- 파일: `V/Presentation/Movement/CombatLeapPlan.cs`(용근); `V/Presentation/Movement/CombatUnitMotionPlayback.cs`(용근)
- 함께 고칠 것 C4-05 · 확신 중간

### C4-02

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 조립 위치 문제라서 플레이어 증상은 없다. 다만 위치 인자로 쓴 CombatTraitPreviewFactory.cs:102는 운영 Factory와 달리 토네이도 외형 연출(tornado.TickAppearance·IsAppearancePlaying·CancelAppearance)을 넘기지 않는다. 특성 미리보기(개발 도구)에서만 차이가 난다. '두 조립 지점이 어긋나는' 실제 예다.
- 가장 작은 수정(약 15줄): 1단계(한 번에 할 것): CombatTraitPreviewFactory.cs:102의 CombatFieldEventPorts 14개 위치 인자를 운영 Factory(CombatSceneDisplayFactory 152-174)처럼 이름 인자로 바꾼다. 토네이도 외형 연출을 미리보기에도 넣을지 함께 정해 맞춘다. 2단계: 두 Factory에 같은 ports 조립(지형+토네이도 외형 람다 묶기)이 두 벌 있으니, Factory 쪽 정적 함수 하나(예: CombatFieldPortsFactory.Create(objects, terrain, thorns, trails, tornado, rockfall, haboob, field))로 모아 두 곳이 부르게 한다. 3단계(결정 필요): CombatSceneEventPresenter 생성자(96-170)의 처리기·재생기 조립을 CombatEventPlaybackFactory로 옮긴다. 생성자가 _presentationVersion·_mapper·_settlement·_structureDamage 같은 비공개 상태를 람다로 처리기에 넘기고 있어서, 먼저 이것을 묶은 재생 문맥 객체를 만들어야 한다. 그다음 CombatEventHandlerTable.Create와 Presenter는 완성된 표와 목록만 받게 한다. 3단계는 C2-01의 기믹 계약과 함께 한다.
- 위험 중간: 1단계(약 15줄)는 이름 인자로만 바꾸므로 위험이 낮다. 3단계는 약 150줄이고 위험이 높다. 처리기 12개의 세대·Mapper·후속 정리 참조를 옮기면서 하나라도 사본으로 잡으면 재생 세대 판정이 틀어진다. 이 생성자는 GC 작업(bbbcb162, Prewarm용 _cueSources·_bodyText)에서 바뀐 뒤 아직 Unity에서 확인하지 않았다. 호출처는 Presenter 생성자 1곳(CombatEventPlaybackFactory:32), Table.Create 1곳, ports 2곳이고 시험 코드는 없다.
- Unity 확인: 1단계: 특성 미리보기 씬에서 토네이도가 나오는 시나리오(있으면)와 지형 무너짐 시나리오를 재생해 멈추지 않는지 본다. 3단계: 전투 씬에서 공격, 포효, 소환, 이동, 특성, 토네이도, 낙석, 하붑, 구조물 피해를 한 번씩 재생한다.
- 정할 것·공유: 3단계는 기록된 결정 Q-S3 A(2026-10-18 presenter-split 설계 S8: '조립은 Presenter 생성자 안에서')를 뒤집는 일이라 사용자가 정한다. 미리보기에 토네이도 외형 연출을 빼 둔 것이 의도인지도 확인이 필요하다. 1단계는 GC 미검증 파일을 건드리지 않는다.
- 파일: `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs`(용근(102)); `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(용근·호진님(호진님, 기믹 ports 줄)); `V/Playback/CombatSceneEventPresenter.cs`(용근 56·용근(Claude) 14·호진님 5(96-170)); `S/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs`(용근); `V/Playback/CombatFieldEventPorts.cs`(용근 62·호진님 15)
- 함께 고칠 것 C2-01 · GC 작업 파일 · 확신 중간

### C1-21

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금 데이터로는 잘못 그려지는 층이 없다. 다음에 호버 범위 층을 더할 때, 층 표와 열거만 고치면 정적 검사는 통과하는데 원점 쪽 변이 조용히 닫힌다.
- 가장 작은 수정(약 40줄): 1단계: FieldHighlightLayers.LayerRow에 열 하나(`SourceOpening` 열거: None / Origin / HoverOrigin)를 더한다. 표의 줄마다 값을 적는다. AttackRange·PullRange·Affected·MoveRange·ActiveActor는 Origin, AllyHoverRange·AllyHoverMoveRange·EnemyAttackRange·EnemyMoveRange는 HoverOrigin이다. 정적 생성자에서 마스크 두 개(`OpensAtSourceLayers`, `HoverRangeLayers`)를 만든다. FieldAttackOutlineOpening.Of의 15-16줄 목록과 IsHoverRange(95-97줄)는 이 마스크 검사로 바꾼다. 줄 모양을 맞추려면 기본값이 있는 선택 인자보다 명시 열이 낫지만, 그러면 19줄을 모두 고친다. 2단계: CombatAttackFeedback.cs:117-131의 tone→층 대응이 HoverRangeLayers 안에 있는지 같은 정적 검사로 묶는다.
- 위험 낮음: 순수 정적 데이터를 옮기는 일이라 값을 맞게 옮기면 그리기 결과가 같다. 표 값을 잘못 적으면 테두리 열림이 조용히 바뀌므로 Origin·HoverOrigin 아홉 층을 원래 목록과 하나씩 대조해야 한다. 정적 생성자 예외는 TypeInitializationException으로 바로 드러난다. 표의 RockfallWarning 줄이 호진님 줄이라, 열을 명시하면 그 줄도 바뀌어 공유 메모가 필요하다. 두 파일은 bbbcb162에서 바뀌지 않았다.
- Unity 확인: GridOutline 모드에서 공격 범위, 끌어오기 범위, 영향 칸, 이동 범위, 선택 아군 테두리의 원점 쪽 변이 전처럼 열리는지 본다. 아군·적 유닛 호버 범위(공격·이동)도 원점 쪽 변이 전처럼 열리는지 본다. 위험 범위(지뢰·화약통 호버)는 전처럼 닫히는지 본다.
- 정할 것·공유: 표의 모든 줄에 열을 명시할지, 기본값 None인 선택 인자로 해당 줄만 고칠지. 명시하면 호진님 줄(RockfallWarning)도 바뀌므로 공유 메모가 필요하다.
- 파일: `V/Display/Field/FieldHighlightLayers.cs`(용근 대부분, 호진님(139-140줄 RockfallWarning 줄, 158·174-175·195줄 idleFill)); `V/Display/Field/FieldAttackOutlineOpening.cs`(용근)
- 확신 높음

## 0단계(Unity 확인) 뒤: GC 작업과 같은 파일 (2026-11-04 4차 코드 완료, 사용자 지시로 0단계 전에 함, 계획 §8)

### C1-14

- 플레이어 영향: 특정 조작에서 남. 전투 목표(보스) 스테이지에서 결과 재생 중 목표가 넉백·이동·사망하면, 모델과 선택 강조는 따라가는데 목표 강조는 결과 끝 Refresh까지 옛 칸에 남는다. 결과 끝에는 맞춰진다.
- 가장 작은 수정(약 15줄): V/Display/CombatSceneDisplay.cs만 고친다. (1) 필드 `EntityId _objectiveTarget`을 두고, RefreshCore에서 `CombatObjectiveDisplay.TargetOf(...)`를 계산하는 자리(295줄)에서 `_events.Refresh` 전에 저장한다. ClearSelection에서는 Empty로 되돌린다. (2) 새 `RenderObjectiveTarget()`은 보인 사실 reader(`_events.InspectionFacts`, 선택 강조와 같은 원본)에서 `TryGet(_objectiveTarget, out f) && f.Character != null`이면 `f.Cells`로 SetLayerCells하고, 아니면 ClearLayer한다. (3) RefreshInspectionSelection(사건마다 CombatShownFacts.Render → inspection.Refresh → 이 콜백)에서 RenderSelectedActor 다음에 부르고, 316줄의 `RenderOccupiedLayer(ObjectiveTarget, …, _displayedCharacters)`도 이 함수로 바꾼다. 호버 원점(608줄)의 `_displayedCharacters` 사용은 원점 유닛이 없으면 예외를 던지는 경로라, 2단계에서 따로 옮긴다.
- 위험 낮음: 선택 강조가 이미 쓰는 같은 reader와 같은 콜백 경로에 한 층을 더하는 것뿐이다. RefreshInspectionSelection을 부르는 곳은 Factory의 BindSelectionChanged 하나다(CombatSceneDisplayFactory.cs:208). 사용자가 선택을 바꿀 때도 불리지만 같은 칸을 다시 넣는 일이라 해롭지 않다. TryGet이 구조물도 돌려주므로 Character가 null이 아닌지 꼭 확인해야 한다. 파일은 bbbcb162에서 바뀐 파일이다.
- Unity 확인: 보스 목표 스테이지에서 보스를 넉백·이동시키면 목표 강조가 사건 순간에 새 칸으로 옮겨 가는지 본다. 보스를 처치하면 그 순간 강조가 사라지는지 본다(IsOnField가 사망을 거르는지 함께 확인). 결과 끝과 국면 전환 뒤에도 강조가 맞는지 본다.
- 파일: `V/Display/CombatSceneDisplay.cs`(용근(66-71·295-316·356-391줄))
- GC 작업 파일 · 확신 중간

### C10-08

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 운영 필드는 Cells Root가 돌아가 있지 않다. 누군가 GridOutline 스테이지의 Cells Root를 Y축으로 돌리면, 이동·공격 범위·호버가 전혀 그려지지 않고 경고 한 줄만 남는다.
- 가장 작은 수정(약 10줄): 1단계(막기): FieldOutlineRenderer.Build에서 회전을 보면 경고하고 _isRotated를 켜는 대신, 핸들을 만들기 전에 InvalidOperationException("GridOutline 모드는 회전하지 않은 Cells Root만 지원")을 던진다. 그러면 _isRotated·_warnedRotation 필드와 Refresh(225줄)의 `|| _isRotated` 검사가 필요 없어진다. FieldView.Build는 이미 catch → Release → throw로 정리한다. FieldView.cellsRoot 툴팁(37줄)은 'CellPrefab 모드만 Y축 회전 허용, GridOutline은 회전 금지'로 바꾼다. ValidateCellsRoot는 표시 모드를 모르므로 바꾸지 않는다. 2단계(선택): GridOutline에서도 회전을 실제로 지원하는 일. 테두리·채우기·경로 좌표를 mapper 로컬로 바꿔야 하는 큰 작업이라 따로 다룬다.
- 위험 낮음: 회전하지 않은 필드에서는 같은 코드가 돈다. 다만 0.01°를 넘는 월드 회전이면 이제 예외로 설치가 실패해 F1이 선다. 부모 오브젝트에 아주 작은 회전이 남아 있는 씬이 있다면, 지금은 강조가 안 보이던 씬이 이제 설치 실패로 바뀐다. 강조가 안 보이는 일은 이미 눈에 띄었을 것이라 그런 씬은 없을 가능성이 높다. 바꿀 두 파일은 bbbcb162에서 바뀐 파일이다.
- Unity 확인: GridOutline 모드인 운영 전투 씬에서 설치가 그대로 되고, 이동·공격 범위·호버·경로가 전처럼 그려지는지 본다. 시험 씬에서 Cells Root를 90° 돌리면 F1(예외 메시지)이 서는지 보고 되돌린다. 인스펙터 툴팁 문구도 본다.
- 정할 것·공유: 회전을 막을지(1단계), GridOutline에서도 회전을 지원할지. 레벨 디자인에서 돌린 필드가 필요한지 사용자가 정한다.
- 파일: `V/Display/Field/FieldOutlineRenderer.cs`(용근(88-89·165-178·222-226줄)); `V/Display/Field/FieldView.cs`(용근(37줄))
- GC 작업 파일 · 확신 높음

### C4-13

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금 소유자 구성(배치·아이템 / 공격·경로)에서는 판정이 맞다. ResolvePointerTarget(layer, attackActive, itemDragActive)의 호출처는 Router 134·227, CombatInteraction 382, CombatPathInput 128, CombatViewSystem 1154로 5곳이다. 모드형 소유자를 하나 더하면 이 5곳과 CombatInputFacts·Reader를 모두 고쳐야 한다. Reader에서 하나를 빠뜨려도 선택 인자라 컴파일된다.
- 가장 작은 수정(약 20줄): 1단계(한 번에): (1) CombatInteraction에 인스턴스 메서드 ResolvePointerTarget(InputLayer layer)를 둔다. 실시간 IsAttackActive·IsItemDragActive로 지금의 정적 식을 부르는 한 곳이다. (2) CombatInputFacts 끝에 선택 인자 pointerTarget을 더하고 PointerTarget 속성을 둔다. 끝에 더하므로 Tests의 이름 인자 호출이 깨지지 않는다. CombatInputFactsReader가 input?.Interaction.ResolvePointerTarget(layer) ?? CombatInputRouter.ResolvePointerTarget(layer,false,false)로 채운다. (3) Router 134·227과 CombatInteraction 382는 _interaction/this.ResolvePointerTarget(facts.CurrentLayer)로 바꾼다. 382는 '전경 감시 뒤 실시간 상태'라 facts 값을 쓰면 안 되므로 인스턴스 메서드를 쓴다. CombatPathInput 128과 CombatViewSystem 1154는 facts.PointerTarget으로 바꾼다. 그러면 새 소유자를 더할 때 CombatInteraction 한 곳만 고치면 된다. 2단계 이후: 소유자가 자기 Layer·모드형 여부를 답하게 하여 CombatInteraction이 활성 전경 소유자 목록에서 대상을 구한다. Router 248줄의 'UI 위면 막는 대상' 목록은 CombatInputOwner의 BlocksPointerOverUi 속성으로 옮긴다. 정적 판정식과 facts의 IsAttackActive·IsItemDragActive 전달은 정리한다.
- 위험 중간: 계산식은 같지만, facts에 담은 대상은 Read 시점의 사본이다. 경로 입력·환경 호버는 지금도 facts 값을 쓰므로 같다. Poll(382)만 실시간 값을 쓰도록 남기는 데 주의해야 한다. 여섯 파일·세 작성자(천명님·호진님·사용자)의 줄을 건드린다. CombatPathInput.cs는 bbbcb162에서 바뀌었는데 아직 컴파일하지 않은 파일이다.
- Unity 확인: (1) 배치 단계에서 유닛 Drag·아이템 Drag·우클릭 회수가 되는지 본다. (2) 전투에서 경로 지정·공격 모드 전환, 공격 모드 중 경로 편집이 꺼지는지 본다. (3) 환경 호버가 배치·전투에서 나오고, 공격 모드·Drag 중에는 숨는지 본다. (4) 유닛 호버 범위가 경로·배치 대상일 때만 나오는지 본다.
- 정할 것·공유: bbbcb162(CombatPathInput 포함)을 Unity에서 먼저 컴파일·재생해 확인한 뒤에 할지 정한다. Router·CombatInputFacts는 천명님, CombatPathInput 128줄은 호진님 작성이라 공유 메모가 필요하다.
- 파일: `V/Input/Interaction/CombatInteraction.cs`(용근·호진님(382줄 주변 Poll)); `V/Input/CombatInputFacts.cs`(천명님(생성자 대부분)·용근); `V/Input/CombatInputFactsReader.cs`(용근); `V/Input/CombatInputRouter.cs`(천명님); `V/Input/MovePath/CombatPathInput.cs`(호진님); `V/CombatViewSystem.cs`(용근)
- 함께 고칠 것 C8-01·C2-02 · GC 작업 파일 · 확신 중간

### C4-05

- 플레이어 영향: 지금 데이터에서는 잠복. 자세를 여는 피격은 지금 HealthChanged뿐이고 레인 목록과 맞는다. Died는 레인이 EndLeap으로 처리한다. 특성 유닛 행동 두 곳은 beforeUnitAction을 넘긴다. 피격 반응을 다른 사건으로 넓히거나, 서는 자리를 빌리는 새 기능이 생길 때에만 F1이 나거나 행동이 빠진다. CombatGuardLead도 CanBeginStand를 PauseLeap 없이 부르지만, 행동 시작이 도약을 먼저 닫아 지금은 겹치지 않는다.
- 가장 작은 수정(약 45줄): 1단계(대여 창구가 양보): CombatUnitPresentationWindow의 Placement에 '양보 가능' 표시와 양보 콜백을 둔다. 예를 들어 `MarkYieldable(EntityId, Action yielded)`를 두고, 레인 FinishStep이 _continuedLeap을 쥘 때 부른다. RequireNoPlacement(BeginAttackPose·BeginPose·TryBeginPose)와 TryGetStandTarget은 양보 가능한 배치를 만나면 예외 대신 그 배치를 _placements에서 빼고 콜백을 부른다. 콜백에서 레인이 _placement = null로 둔다. 질문인 CanBeginStand는 부작용 없이 양보 가능 배치를 '빌릴 수 있음'으로 본다. 되찾기는 지금처럼 PresentEvent 뒤의 ResumeLeaps가 한다. 이 경로는 이미 있다. 그다음 레인 BeforeEvent의 HealthChanged/HealthNormalized → PauseLeap 분기, CombatTraitEventHandler의 beforeUnitAction 인자와 두 호출, CombatSceneEventPresenter.cs:163의 _motion.PauseLeap 전달, CombatUnitMotionSet.PauseLeap을 지운다. 사건마다 무조건 반환하는 더 작은 안은 쓰지 않는다. 공중 경유 유닛이 사건마다 땅으로 떨어져 보이는 회귀가 생기기 때문이다.
- 위험 중간: 대여 순서(배치 ↔ 자세 ↔ 서는 자리)와 되찾기 시점을 건드리는 수명 변경이다. 레인 IsActive(_continuedLeap != null && _placement == null)로 사건 진행을 기다리는 관계도 지켜야 한다. CombatUnitPresentationWindow.cs와 CombatSceneEventPresenter.cs는 bbbcb162에서 바뀌었고, 그 커밋은 아직 Unity에서 확인되지 않았다.
- Unity 확인: 뛰어넘기 유닛이 도약 중간 칸에서 피해를 받을 때 피격 반응 자세가 나오는지, 그 뒤 공중 경유 위치로 돌아가 도약을 이어가는지 본다. 도약 중간에 특성 유닛 행동(위치 이동이나 순간 행동)이 일어나는 조합도 본다. 콘솔에 RequireNoPlacement 예외나 '서는 자리를 빌릴 수 없음' 경고가 없는지 확인한다.
- 정할 것·공유: bbbcb162(GC 끊김 줄이기)를 Unity에서 먼저 확인한 뒤에 할지 정해야 한다. 창구 양보(1단계 안)로 갈지, 레인이 처리기의 '자세를 여는 사건' 판정 함수를 함께 부르는 더 작은 SSOT 안으로 갈지도 사용자가 고른다.
- 파일: `V/Display/Unit/CombatUnitPresentationWindow.cs`(용근); `V/Presentation/Movement/CombatUnitMotionPlayback.cs`(용근); `V/Presentation/Movement/CombatUnitMotionSet.cs`(용근); `V/Playback/Handlers/CombatTraitEventHandler.cs`(용근, 용근(Claude)); `V/Playback/CombatSceneEventPresenter.cs`(용근)
- 함께 고칠 것 C1-11 · 먼저 고칠 것 C1-11 · GC 작업 파일 · 확신 중간

### C2-01

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금은 정리 목록 세 벌(Install 실패 264, Clear 397, Release 414)에 흔적·토네이도·낙석·하붑이 모두 들어 있다. 진입 보류에서 흔적을 빼고 셋만 다루는 것은 의도다. 새 기믹을 더할 때만 빠뜨릴 위험이 생긴다.
- 가장 작은 수정(약 15줄): 1단계(한 파일): V/Display/CombatSceneDisplay.cs 생성자에서 `_gimmickClears = { _trails.Clear, _tornado.Clear, _rockfall.Clear, _haboob.Clear }`와 `_gimmickReleases`를 한 번 만든다. 정리 목록 세 벌은 손으로 적은 네 이름 대신 이 배열을 Combine으로 붙인다. 기믹 이름은 이 파일에서 한 곳에만 남고, 생성 때 한 번 만들어 프레임 할당도 없다. 2단계: internal interface IFieldGimmickDisplay(Render(CombatStateData, FieldGridMapper)·Clear·Release, 선택으로 TickAppearance·IsAppearancePlaying)를 두고 Field*Presenter 넷이 명시적 구현한다. internal 메서드라 명시적 구현이 필요하다. Factory가 목록 하나를 만들어 CombatSceneDisplay·CombatEntryDeferredDisplay(보류 대상만 고른 목록)·ports의 외형 람다가 그 목록을 돌게 한다. 3단계: 재생 쪽 처리기 칸·Table.Create·Presenter 생성자(C4-02 3단계)와 입력 보조 CombatEnvironmentText의 형식 검사 사슬을 정리한다.
- 위험 낮음: 1단계는 같은 대리자를 같은 순서로 붙일 뿐이다. 다만 Install 실패 정리(264)는 GC 작업에서 Prewarm이 더해진 try 블록 안에 있고 그 블록은 아직 Unity 미확인이라, 확인이 끝난 뒤에 하는 편이 안전하다. 2단계는 Render 인자가 기믹마다 달라서(토네이도는 ReadTornadoAreas(Owner.Player), 진입 보류 경유) 위험이 중간이다. 2단계는 여러 파일에 퍼진다.
- Unity 확인: 토네이도·낙석·하붑·흔적이 있는 전투에 들어가 진입 연출 뒤 보드에 기믹이 보이는지 본다. 전투를 다시 시작하거나(Clear) 씬을 나가도(Release) 기믹 오브젝트·연기가 남지 않는지 Hierarchy에서 본다. 설치 실패는 재현이 어려우니 생략한다.
- 정할 것·공유: 호진님이 11-03까지 토네이도·낙석 View를 계속 고치고 있어서, 2단계 구조 변경은 시기를 맞추고 공유 메모를 남겨야 한다(기믹 줄 작성자가 호진님). 1단계는 사용자 판단으로 해도 된다.
- 파일: `V/Display/CombatSceneDisplay.cs`(기믹 줄은 호진님(호진님, 94-99·199-201·264·397·414), 나머지 용근, 411은 천명님); `V/Presentation/Entry/CombatEntryDeferredDisplay.cs`(용근(107줄 전부)); `V/Display/Map/FieldTornadoPresenter.cs`(주로 호진님(확인 필요)); `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(용근·호진님(기믹 생성 줄))
- 함께 고칠 것 C4-02·C2-03 · GC 작업 파일 · 확신 높음

### C2-03

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 한 클래스에 책임이 많이 모인 문제라서 플레이어 증상은 없다. 두 사람이 같은 생성자와 RefreshCore를 고칠 때 충돌하거나, 같은 타입 위치 인자를 바꿔 넣는 실수가 생긴다.
- 가장 작은 수정(약 30줄): 1단계(작은 것 하나): 배경음 정책만 뺀다. V/Playback/Support/CombatSoundPresets.cs 파일 안(새 .meta를 피함)이나 새 파일에 internal sealed class CombatBgmPolicy(IAudioSystemService, CombatSoundPresets)를 두고, ApplyPhase(bool isNight)·PlayVictory()·ApplyAfterCompletion() 세 함수로 RefreshCore 292-294줄과 ShowCompletion 421·426-428줄을 옮긴다. CombatSceneDisplay 생성자 안에서 기존 audio·sounds 인자로 만들어 Factory 호출은 바꾸지 않는다. 2단계: ICombatBattleDisplay의 입력 피드백 하위 계약(고스트·공격·넉백·이동 피드백)을 실제 협력 객체가 직접 구현·노출하게 바꾸고, 통과 메서드 약 45개(462-785)를 없앤다. CombatSceneInputFactory도 바뀐다. 3단계: 필드 그리기(지형·기믹, C2-01 2단계와 함께)와 완료·행동 버튼 표면을 별도 표시 객체로 나누고, 38인자 생성자를 묶음 인자로 줄인다.
- 위험 낮음: 1단계는 호출 몇 줄을 옮기기만 하고 Factory 시그니처도 그대로라 위험이 낮다. 다만 파일 자체가 GC 작업(bbbcb162, Prewarm 추가)에서 바뀌고 아직 Unity 미확인이라, 오류가 나면 원인 구분이 어려워진다. 2·3단계는 ICombatBattleDisplay 참조 약 15곳과 입력 Factory를 함께 바꾸므로 위험이 중간 이상이다.
- Unity 확인: 1단계: 전투 진입 시 낮 배경음, 밤 전환 시 밤 배경음, 승리 시 보상 효과음, 완료 확인 뒤 완료 배경음이 나오는지 본다. 확인을 거절하면 배경음이 바뀌지 않는지 본다.
- 정할 것·공유: 2·3단계 범위(ICombatBattleDisplay 하위 계약을 협력 객체가 직접 내놓게 할지)는 사용자가 정한다. 1단계는 GC 작업 Unity 확인 뒤에 하는 것을 권한다.
- 파일: `V/Display/CombatSceneDisplay.cs`(바꿀 줄(288-295·416-429)은 용근. 파일에는 용근(Claude) 44(GC 작업)·호진님 15·현강님 12·천명님 4); `V/Playback/Support/CombatSoundPresets.cs`(용근(확인 필요)); `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`(용근(확인 필요))
- 함께 고칠 것 C2-01 · GC 작업 파일 · 확신 중간

## 먼저 정하거나 물어 둘 것

### C10-02

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 알려진 그리기·이펙트·HUD 투영 예외는 없다. 예외가 한 번이라도 나면 화면(옛 범위)과 상태·입력(새 범위)이 어긋난 채 F1 없이 전투가 이어져 원인을 찾기 어렵다. 토네이도 맥동 예외는 F1 뒤에도 매 프레임 찍힌다.
- 가장 작은 수정(약 20줄): 1단계(FieldView만): CombatViewSystem이 표시를 만든 바로 뒤(503줄 `_sceneFactory.CreateDisplay` 직후) `created.BindFrameFaults(_faults.Report)`를 부른다. CombatSceneDisplay.BindFrameFaults는 FieldView로 넘긴다. FieldView는 `Action<Exception> _reportFrameFault`를 두고, LateUpdate를 `try { FlushPending(); } catch (Exception e) when (_reportFrameFault != null) { _reportFrameFault(e); }`로 감싼다. 연결이 없으면(편집 모드 등) 지금처럼 던진다. FlushPending에서 Repaint가 던지면 refresh가 사라지는 문제는, Refresh 호출을 finally 뒤가 아니라 Repaint와 별개로 시도하게 순서를 바꾸면 줄일 수 있다(선택). 2단계: 같은 BindFrameFaults로 UnitHUDLayer.RefreshPositions(Twinsgm 줄), FieldTornadoArrowPulse/Presenter(천명님 파일, 맥동은 IsFaulted면 멈춤), CombatEffectWindow → CombatEffectFollower.LateUpdate를 차례로 감싼다. 플러시를 Tick 끝으로 옮기는 안은 '실행 순서 맨 뒤 LateUpdate로 UI 호버 해제까지 같은 프레임에 그린다'는 GH1 계약을 깨므로 쓰지 않는다.
- 위험 중간: F1 보고는 Tick 밖(LateUpdate)에서 정리를 시작한다. OnDisable 경로(CombatViewSystem.cs:812-823)에 선례가 있지만, 정리 중 FieldView 층 API가 대기만 쌓는 등 재진입 동작을 Unity에서 확인해야 한다. 11-03 계획 §3.7 L2('그리기 실패는 Unity 콘솔')를 되돌리는 결정이다. FieldView·CombatSceneDisplay·UnitHUDLayer는 bbbcb162에서 바뀐 파일이다. 2단계는 현강님·천명님 줄을 건드려 공유 메모가 필요하다.
- Unity 확인: 평소 전투에서 강조 그리기(이동·공격 범위·호버)가 전과 같은지 본다. 개발용으로 FieldOutlineRenderer.Repaint에 잠시 throw를 넣어 F1 화면이 서고 입력이 멈추는지 확인한 뒤 되돌린다. F1 뒤 콘솔에 같은 예외가 매 프레임 쌓이지 않는지 본다.
- 정할 것·공유: 11-03에 받아들인 차이 L2(그리기 실패는 콘솔만)를 되돌려 F1로 올릴지 사용자가 정한다. 2단계의 UnitHUDLayer(현강님 줄)와 토네이도 맥동(천명님 파일)은 해당 작업자와 공유한다.
- 파일: `V/Display/Field/FieldView.cs`(용근(Claude)(337-363줄)); `V/Display/CombatSceneDisplay.cs`(용근(새 함수 추가)); `V/CombatViewSystem.cs`(확인 필요(503줄 근처에 한 줄 추가)); `V/Display/Unit/UnitHUDLayer.cs`(현강님(RefreshPositions 대부분, 문서상 현강님 줄), 용근(Claude)·용근 일부); `V/Display/Map/FieldTornadoArrowPulse.cs`(천명님(천명님)); `V/Presentation/Effects/CombatEffectFollower.cs`(용근 32줄, 용근(Claude) 8줄)
- 함께 고칠 것 C10-08 · GC 작업 파일 · 확신 중간

### C1-38

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 플레이어에게는 붕괴 때 2번 소리가 나고, 이건 현강님이 의도한 소리다. 표 값(-1)을 바꿔도 아무 일이 없다는 것은 기획자가 표를 만질 때에만 드러난다.
- 가장 작은 수정(약 4줄): 1단계: 표 값과 코드를 같은 커밋에서 함께 바꾼다. (a) CombatPresentationSettings_Default.asset의 weakGroundCollapseSoundPreset을 -1에서 2로 바꾼다. (b) CombatEventSounds.Moment의 158줄을 `CombatSoundCue.Effect(SfxEvent.WeakTerrian, presets.WeakGroundCollapse())`로 바꾼다. Effect가 -1이면 None을 돌려 '-1 = 소리 없음'이 그대로 지켜진다. 코드만 바꾸고 표를 그대로 두면 운영에서 붕괴 소리가 사라지는 회귀가 생긴다. 진입 소리 0/1(WeakGroundEntryPreset)에는 지금 설정 칸이 없다. 그래서 1단계에서는 코드에 두고 주석으로 'AudioSystem WeakTerrian 프리셋 0·1 = 진입 1·2회, 2 = 붕괴'를 밝힌다. 2단계: 진입 프리셋 두 칸을 설정 표에 더한다. 또는 반대로 정해 설정 칸 weakGroundCollapseSoundPreset과 WeakGroundCollapse()를 지운다.
- 위험 낮음: 코드는 한 줄만 바뀐다. 다만 asset 값을 함께 바꾸지 않으면 소리가 사라지니, 둘을 반드시 같은 커밋에 넣는다. 붕괴 시점(CombatEventTypeTable)과 큐는 건드리지 않는다.
- Unity 확인: 약한 지반을 밟아 무너뜨려 붕괴 소리(2번)가 전처럼 나는지 본다. 진입 1·2회 소리도 그대로인지 본다. 설정 표 값을 -1로 바꿔 소리가 꺼지는지 한 번 확인한 뒤 2로 되돌린다.
- 정할 것·공유: 158·178-186줄은 현강님(현강님)이 2026-11-02 커밋 8fe4a688b('환경요소 사운드 연결')에서 DB 값을 코드 숫자 2로 일부러 바꾼 줄이다. DB 값으로 되돌릴지(표 기본값 2), 설정 칸을 지우고 코드 숫자를 원본으로 할지 현강님과 정해야 한다. 진입 프리셋도 표로 올릴지 함께 정한다.
- 파일: `V/Playback/Support/CombatEventSounds.cs`(현강님); `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset`(용근)
- 확신 높음

### C1-22

- 플레이어 영향: 지금 데이터에서는 잠복. 지금은 두 규칙이 글자 그대로 같아서 HUD 막대와 정보창 막대가 같게 보인다. 한쪽 규칙만 바뀔 때 어긋난다.
- 가장 작은 수정(약 40줄): 1단계: S/RuntimeSystem/GlobalSystem/UISystem/Base/HealthFillAnimation.cs 옆(또는 같은 파일)에 internal static HealthBarRule을 둔다. Denominator(max, hp, shield), Amount(value, denominator), ShieldAmount(hp, shield, denominator), ShouldSnap(hasHealth, prevMax, nextMax, isActive, prevDenominator, nextDenominator) 네 개다. 그다음 V/Display/Hud/CombatUnitInfoView.cs의 HealthBarDenominator·SetHealth(297-312)·ApplyHealthFill(322-328)이 이것을 부르게 바꾼다. UnitHUD.cs의 HealthBarDenominator(109)·SetHealth(263-278)·HealthBarAmount·ApplyShieldFill(326-340)도 같은 함수를 부르게 바꾼다. UnitHUD만 가진 예상 체력 Snap(268-269)은 그대로 둔다. UnitHUD 쪽을 함께 못 고치면, 우리 쪽만 먼저 함수로 바꾸는 것은 의미가 적으니 한 번에 한다.
- 위험 낮음: 식을 옮기기만 한다. 다만 스냅 조건 순서(UnitHUD는 예상 표시 Snap 뒤에 분모를 비교한다)를 잘못 합치면 피해 애니메이션이 사라지거나 분모 변경을 피해처럼 보여 줄 수 있다. 두 파일 모두 다른 사람 줄이라서 병합 충돌 위험도 있다.
- Unity 확인: 보호막이 있는 유닛을 고른 채 피해, 회복, 보호막 소멸, 최대 HP 변화를 일으킨다. 필드 HUD 막대와 정보창 막대의 HP·보호막 길이가 같은지 본다. 최대 HP가 바뀔 때 감소 애니메이션 없이 바로 맞춰지는지 본다. 공격 예상 체력 표시를 켰다 끈 뒤 실제 피해가 직전 HP에서 줄어드는지 본다.
- 정할 것·공유: 현강님(GlobalSystem, UnitHUD 작성자)과 공통 함수의 위치·이름을 합의한다. 바꿀 줄 작성자가 모두 현강님이라 공유 메모가 필요하다.
- 파일: `S/RuntimeSystem/GlobalSystem/UISystem/Base/HealthFillAnimation.cs`(다른 영역, 현강님(현강님, ad7e6b28)); `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs`(다른 영역, 현강님(현강님, 바꿀 줄 전부)); `V/Display/Hud/CombatUnitInfoView.cs`(현강님(바꿀 HP 줄 전부, 현강님)·파일 전체에는 호진님 11-03 변경(245d6be3) 있음)
- 확신 높음

### C1-57

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 Core 규칙(CleanSweep→AttackIncreaseStatus, ReconSquad·HuntersMark→WeaknessExposure, 가속→HeldAttackBonus)과 View 표가 맞는다. Core가 상태 형식을 바꾸면 그 특성의 StateApplied 이펙트·글자가 빠진다. 이때 CombatTraitFeedbackPlayback.Begin이 LogWarning을 한 번 남겨서, 완전히 조용히 묻히지는 않는다.
- 가장 작은 수정(약 0줄): 1단계(지금 할 것, 코드 변경 없음): Core 전달 문서(천명님)에 요청을 적는다. 'TraitActivationSnapshot 또는 CombatTraitStateEvent가 이 발동이 부여·변경한 상태 항목(EffectOriginKey와 상태 식별)을 함께 공개해 달라'. 함께 지금 View가 흉내 내는 대응 세 줄(CombatTraitFeedbackProfiles.cs:38-48)을 적어 둔다. 2단계(Core가 연결 사실을 준 뒤): CombatTraitStateEvidence.cs의 판독기 셋(Acceleration·AttackGrant·WeaknessExposure)을 출처 기반 판독기 하나로 바꾼다. 프로필 표에서 Evidence 열과 CombatTraitStateEvidenceKind를 지우고 Count 열만 남긴다. TraitActivationVisual 검사 문구도 맞춘다. 그전까지는 지금 코드를 그대로 둔다. 이미 특성 미리보기(PreviewTraitFeedbackValidation)와 실행 중 경고가 어긋남을 알린다.
- 위험 낮음: 1단계는 문서뿐이다. 2단계(약 60줄)는 Core 계약이 바뀐 뒤에 하는데, StateApplied 연결 판정(CombatTraitFeedbackPlan 120-148의 '유일 후보' 규칙)이 새 판독기와 같은 결과를 내는지 미리보기로 확인해야 해서 위험이 중간이다.
- Unity 확인: 2단계 때: 특성 미리보기 씬에서 가속·CleanSweep·ReconSquad·HuntersMark 시나리오를 재생해 상태 이펙트와 몸 글자가 나오는지 본다. Console에 [CombatTraitFeedback] 경고가 없는지 본다.
- 정할 것·공유: 천명님(Core)이 발동-상태 연결 사실을 공개할지 정한다. 공개하지 않으면 지금 사본 표를 받아들이는 것으로 기록만 남긴다.
- 파일: `S/DataBaseSystem/Data/View/CombatPresentation/CombatTraitFeedbackProfiles.cs`(용근(38-48 전부)); `V/Playback/Support/CombatTraitStateEvidence.cs`(용근(48-80 전부)); `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/Traits(TraitActivationSnapshot)`(다른 영역, Core(천명님))
- 확신 중간

### C5-01

- 플레이어 영향: 지금 데이터에서는 잠복. TryBegin은 늘 false이고 IsPlaying도 늘 false라서 재생에는 영향이 없다. 운영 자산에는 AfterRendering 레이어(TagManager:29)와 Day Camera(맵 프랍 4개)가 있어서 지금은 조립이 통과한다. 누가 레이어나 카메라 이름을 바꾸면 그때 전투 씬과 특성 미리보기 조립이 예외로 멈춘다.
- 가장 작은 수정(약 45줄): 1단계(한 번에 할 것, 되살릴 길은 남김): (a) V/Playback/CombatSceneEventPlayback.cs에서 _phaseTransition 필드·생성자 인자를 빼고, IsPresentationComplete·CanBeginPresentationEvent·BeginPresentation의 `_phaseTransition.IsPlaying` 3곳, BeginEvent의 TryBegin try/catch(126-137), TickPresentation의 transitionPlaying·Tick(166·174·178), CancelPendingEvents의 CancelPhaseTransition(186-191)을 지운다. (b) S/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.Create에서 transitions·transitionSettings 인자와 33줄 new를 지운다. (c) CombatSceneDisplayFactory 84-89줄의 CombatPhaseTransitionSettings.From와 RequireMapCamera 호출, 그리고 Create 호출 인자 두 개를 지운다. RequireMapCamera 메서드(228-237)는 Day 카메라 요구를 유지하기로 하면 남기고, 아니면 지운다. (d) CombatTraitPreviewFactory.cs:103 호출 인자 두 개를 지운다. CombatPhaseTransitionPresenter.cs·CombatPhaseTransitionSettings.cs·형식 표 Transition 열·SO 필드는 그대로 두고, 파일 머리에 '조립에서 빠짐, 되살릴 때 연결할 곳' 주석 한 줄만 단다. 2단계(되살리지 않기로 정하면): Presenter·Settings 파일, CombatEventTypeTable의 CombatPhaseTransitionKind·transition 열·TransitionOf, SO의 morningTransition*·Validate 줄을 걷어 낸다. SO 필드는 직렬화 값이 남아도 해가 없지만 Inspector를 정리하려면 지운다.
- 위험 낮음: 지우는 조건은 모두 늘 같은 값이라서(IsPlaying=false, TryBegin=false) 재생 순서는 바뀌지 않는다. 호출처는 생성자 1곳과 Factory.Create 2곳(운영 씬, 특성 미리보기)이고 시험 코드는 없다. 조립 검사가 줄어 더 관대해질 뿐이다. 남는 위험은 Day 카메라 요구가 다른 곳(카메라 효과 카탈로그 열쇠)의 숨은 전제였을 가능성인데, grep으로는 CombatSceneDisplayFactory 한 곳뿐이다.
- Unity 확인: 전투 씬에 들어가 밤→아침(NightEnded)까지 한 턴 넘게 진행하며 사건이 멈추지 않는지 본다. 결과가 끝날 때 다음 입력이 열리는지 본다. 도중에 씬을 나갔다 다시 들어와도 오류가 없는지 본다. 특성 미리보기 씬을 열어 시나리오 하나를 재생한다.
- 정할 것·공유: (1) 아침 자동 전환을 되살릴 계획이 있는지. 문서는 '민형님 e18c8313'이라 적었지만 이 저장소에 그 커밋은 없다. 실제로 TryBegin을 주석 처리한 사람은 호진님(b9c4f3b4 '밤 전환 안되게 테스트로 수정')이라서 호진님에게 확인한다. 민형님(600d0636)은 Night Camera를 지웠다. (2) 용근 주석 '자동 전환은 비활성이지만 Day 카메라 요구는 유지'를 계속 지킬지는 사용자가 정한다.
- 파일: `V/Playback/CombatSceneEventPlayback.cs`(용근(바꿀 줄 41줄 전부)); `S/Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs`(용근); `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(용근(83-89, 228-237)); `S/Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs`(용근(103)); `V/Playback/CombatPhaseTransitionPresenter.cs`(호진님(주석 처리 b9c4f3b4, 27줄)·용근(10줄))
- 확신 높음

### C10-06

- 플레이어 영향: 지금 데이터에서는 잠복. 지금 운영 타일 스테이지에는 필드 오브젝트·장애물이 없고 nexusDrop도 켜져 있다. 타일 스테이지에 화약통·지뢰·장애물을 두거나 nexusDrop을 끄면, 보드가 올라오는 약 1.4초 동안 그 물체와 체력 HUD가 빈 구멍 위에 떠 보인다.
- 가장 작은 수정(약 60줄): 1단계(구조물): FieldUnitPresenter에 `CollectBoardStructures(structures, includeNexus, List<FieldUnitView>)`를 더한다. 살아 있는 구조물 가운데 적 HQ를 뺀 것을 모으고, 낙하가 꺼졌으면 HQ도 넣는다. CombatEntryNexusAppearance는 `internal bool Drops => _settings.Enabled`를 내놓는다. CombatBoardEntryPresentation.Prepare는 이 표시들을 `new CombatBoardRevealTarget(view.transform, 기준 칸 타일 또는 null)`로 targets에 더하고, `view.SetEntryHudHidden(true)`를 부른다. ShowField 시점(보드 완성)과 Cancel에서는 SetEntryHudHidden(false)로 되돌린다. 2단계(필드 오브젝트): ObjectViewSet에 VisitViews를 더하고, FieldObjectPresenter에 CollectBoardRevealTargets(타일 null이면 자기 필드 깊이)를 더한다. CombatBoardEntryFactory.Create와 CombatSceneDisplayFactory 호출에 objects 인자를 더한다. 오브젝트 쪽은 천명님 파일이라 공유 메모와 함께 따로 한다.
- 위험 중간: CombatBoardReveal은 대상 Body에 높이 변위를 더했다가 Cancel에서 되돌린다. 보드 단계 중에 구조물이나 오브젝트의 위치를 다시 쓰는 Render(view.Apply)가 끼면 변위가 덮이거나 복원 위치가 어긋날 수 있다. 진입 중에는 입력이 막혀 결과 Render가 없을 것으로 보이지만 확인이 필요하다. 2단계는 파일 넷 이상, 작성자 둘에 걸친다. FieldUnitPresenter는 bbbcb162에서 바뀐 파일이다.
- Unity 확인: 타일 스테이지(운영 데이터)의 진입 연출이 전과 같은지 본다. 시험용으로 타일 스테이지에 장애물을 두거나 nexusDrop을 끄고, 그 구조물과 체력 HUD가 타일과 함께 올라오고 HUD는 보드 완성 뒤 나타나는지 본다. 진입 도중 포기·씬 이탈 뒤 구조물 높이가 원래대로인지 본다.
- 정할 것·공유: 보드 위 물체를 '숨겼다 보드 완성 때 보이기'로 할지 '칸 타일과 함께 올라오기'로 할지(연출 결정). 필드 오브젝트 쪽(2단계)을 천명님 파일에서 고칠지.
- 파일: `V/Presentation/Entry/CombatBoardEntryPresentation.cs`(용근(76-92줄)); `V/Display/Unit/FieldUnitPresenter.cs`(용근(364-376줄 근처, 1줄 호진님). 새 함수 추가); `V/Presentation/Entry/CombatEntryNexusAppearance.cs`(용근); `V/Display/Map/FieldObjectPresenter.cs`(천명님(천명님, 파일 전체)); `S/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(확인 필요(207줄 CombatBoardEntryFactory.Create 호출))
- 함께 고칠 것 C10-03 · GC 작업 파일 · 확신 중간

### C1-42

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금 곡사(CatusCannon)는 따로 짠 수명으로도 정상 동작한다. 예외 종류가 다른 것(ArgumentOutOfRange와 InvalidOperation)은 비유한 좌표라는 오류 경로에서만 보인다. 기반 수명 정책을 바꿀 때 곡사만 빠지는 위험이다.
- 가장 작은 수정(약 130줄): 1단계: CombatAttackMotion에 훅 두 개를 더한다. protected virtual BeginResources()는 Begin에서 Render(0) 전에 부르고, 곡사가 _weapon.Validate·BeginPose·BindCancellation을 여기서 한다. protected CancelUnavailable()은 지금 Tick 안의 'cancelOwner 뒤 Cancel' 블록을 함수로 뺀 것이다. 그다음 CombatMortarAnimation이 CombatAttackMotion을 상속하게 바꾼다. duration은 _poseEndsAt, up은 mapper.WorldUp이다. Advance(prev, next)에서 발사 시각을 넘으면 Render(_fireAt) 뒤 Fire()를 부르고, _fireAt == 0이면 Advance(0,0)에서 쏜다. ResourcesAvailable은 무기 가용·자세 세대·비행 중 lease 활성으로, ReleaseResources는 무기 해제·pool.Recall·무기 자세 복원으로 옮긴다. IsComplete는 IsRunning && _flightDone && PoseDone이다. 발사 콜백의 version 비교는 IsCurrent(version)로 바꾼다. 그러면 _begun/_cancelled/_released/_cleanupDone/_ticking/_version, Cleanup, CleanupFailure, TryCleanup, RequireFinite를 지울 수 있다. 미리 확인할 것: 기반 생성자는 duration > 0을 요구하는데, 곡사 설정 검사가 _poseEndsAt > 0을 보장하는지 본다.
- 위험 중간: 곡사 하나의 수명(시작, 발사, 비행 대기, 취소, 정리 순서)을 통째로 다시 쓴다. 정리 순서가 '무기 해제 → 탄 회수 → 무기 자세 → 유닛 자세'에서 기반의 ReleaseResources → RestorePose로 바뀐다. 무기 SetActive 재진입 취소 경로(Render 도중 불가)도 CancelUnavailable로 옮겨야 한다. 기반이 바뀌므로 직선·근접도 컴파일 영향을 받는다. bbbcb162에서 바뀐 파일은 아니다(그 커밋은 CombatProjectilePool만 고쳤고, 이 수정은 그것을 부르기만 한다).
- Unity 확인: CatusCannon 곡사 공격이 준비 → 발사 → 반동 → 무기 복귀 → 착탄 히트까지 전처럼 보이는지, 공격 완료가 착탄과 복귀 뒤인지 본다. 공격 중 결과 건너뛰기, 공격자 사망·표시 꺼짐, 무기 비활성으로 취소될 때 탄이 남지 않는지, 자세가 돌아오는지, 콘솔 예외가 없는지 본다. 직선·근접 공격도 한 번씩 본다.
- 정할 것·공유: 곡사 파일은 천명님(천명님), 기반 파일은 호진님(호진님)이 썼다. 두 분께 공유 메모를 보내야 하고, 특히 기반에 훅 두 개를 더하는 것에 대한 합의가 필요하다.
- 파일: `V/Presentation/Attack/CombatAttackMotion.cs`(호진님); `V/Presentation/Attack/CombatMortarAnimation.cs`(천명님)
- 확신 중간

### C2-04

- 플레이어 영향: 플레이어는 못 겪음(다음 변경 때 실수). 지금 Event·Event2·Event3는 모든 대응이 다 채워져 있어 신호가 빠지지 않는다. Event4 같은 새 이벤트를 더하다 한 곳을 빠뜨릴 때에만 탄이나 소리가 조용히 빠진다.
- 가장 작은 수정(약 50줄): 1단계(View Pieces 안만): (a) Id→Signal 대응 SignalOf를 CombatClipRangedAttack에서 꺼낸다. 지금 있는 곳에 두고 공유해도 되고, 새 정적 CombatClipEventSignals로 옮겨도 된다. (b) CombatClipEventCursor.TryNext가 Start/Event/End 세 종류와 중간 이벤트의 CombatClipEventId(또는 Events 순번)를 함께 내게 한다. CombatClipEventKind의 Event2·Event3와 KindOf를 지운다. (c) CombatClipPlayback 생성자를 (lease, data, started, Action<CombatClipEventId> eventReached, ended)로 바꾼다. 이벤트별 Action 필드 셋과 Emit switch 갈래를 하나로 줄인다. (d) CombatClipPiece.Begin은 Event2·3 하드코딩 대신 Begin에서 _clip.Timings.Events를 돌며 SignalCallback(SignalOf(id)) 배열을 만든다. 그 배열을 찾는 콜백 하나를 넘긴다. 이렇게 하면 Tick에서 할당이 없다. 2단계 이후: DB 쪽 CombatClipEventId·CombatClipTimings 생성자(eventTime2/3)·CombatClipEvents 함수 이름 파싱·CombatPresentationCueRules.AnchorOf·CombatCueAnchor를 표 하나로 모은다. DB 검사와 asset 직렬화에 닿으므로 따로 한다.
- 위험 중간: 모든 클립 공격(근접·원거리)의 신호 시점이 이 경로를 지난다. 첫 Event → ClipEvent 대응과 같은 시각의 '시작 → 중간 → 끝' 순서를 그대로 지켜야 한다. CombatClipPlayback 생성자 호출처는 CombatClipPiece 하나다. 바뀌는 파일은 bbbcb162 밖이다(CombatPieceStart는 그 커밋에서 바뀌었지만 이 수정은 건드리지 않는다).
- Unity 확인: 클립 근접 공격의 타격 시점(ClipEvent + 선딜레이)을 본다. ClipEvent2·3을 쓰는 원거리 클립 유닛이 있으면 탄별 발사 시점과 Cue2·Cue3 이펙트·소리가 전과 같은지 본다. 0초 이벤트를 가진 클립의 시작 시점도 본다.
- 정할 것·공유: Cursor·Playback·Piece 줄의 대부분을 호진님(호진님)이 썼다. 공유 메모가 필요하다. 2단계(DB enum·검사 통합)를 할지는 따로 정한다.
- 파일: `V/Presentation/Pieces/CombatClipEventCursor.cs`(호진님, 용근); `V/Presentation/Pieces/CombatClipPlayback.cs`(호진님, 용근); `V/Presentation/Pieces/CombatClipPiece.cs`(호진님, 용근); `V/Presentation/Pieces/CombatClipRangedAttack.cs`(용근)
- 확신 중간

## 묶음별 검토자 의견

- 입력·포인터: 먼저 G3-01과 C10-07을 고치길 권한다. 둘 다 플레이어가 겪는 결함이고, 고칠 곳이 사용자 쪽 파일 1~2개에 10줄 안팎이며, bbbcb162 파일을 건드리지 않아 Unity에서 바로 따로 확인할 수 있다. G3-01만 '손에 남김'과 '집기 막기' 가운데 하나를 정해야 한다. 그다음은 C10-01이다. 효과는 가장 크지만 카메라 계약 서명을 바꾸고, Router에 메서드를 더하며(천명님 파일), Pan으로 경로가 늘거나 이동 Drag가 시작되는 새 동작을 정해야 해서 따로 한 번에 확인하는 편이 낫다. C7-02의 1단계(HitsUi 프레임 기억, 한 파일)는 C10-01 바로 뒤나 같은 회차에 넣으면 다시 집기로 늘어나는 Raycast 비용을 상쇄한다. 구조 문제 가운데 C8-01 1단계는 동작 변화가 없는 위임이라 가장 안전하고, 다음 회차로 적당하다. C5-02 1단계(생성자 셋 필수화)는 그 뒤에 넣는다. C4-13은 아직 확인하지 않은 GC 작업 파일(CombatPathInput.cs)과 세 작성자의 줄을 건드린다. 그래서 bbbcb162·5e668b2a를 Unity에서 먼저 컴파일·재생해 확인하고 천명님·호진님께 공유 메모를 보낸 뒤 C8-01과 묶어 하는 편이 좋다.
- 표시·진입·필드: 순서는 작고 위험이 낮은 것부터 잡았다. 1순위는 C10-03(8줄)과 C11-02(20줄)다. 둘 다 한 파일이고, GC 작업에서 확인되지 않은 파일을 건드리지 않으며, 지금 에셋에서는 동작이 바뀌지 않아 Unity에서 바로 확인할 수 있다. C10-03은 프리팹 하나만 바뀌어도 전투 진입이 영구히 멈추는 위험을 없앤다. 다음은 C10-08(10줄)과 C1-14(15줄)다. 수정은 작지만 bbbcb162에서 바뀐 FieldOutlineRenderer·FieldView·CombatSceneDisplay를 건드리므로, 사용자가 GC 작업을 컴파일·재생으로 먼저 확인한 뒤 하는 것이 좋다. C1-14만 플레이어가 실제로 겪을 수 있는 문제(sometimes)다. C1-21은 개발자에게만 보이는 문제이고 호진님 줄이 섞여 있어, 공유 메모와 함께 여유가 있을 때 한다. C10-06(구조물 1단계, 오브젝트 2단계, 천명님 파일)과 C10-02(L2 차이를 되돌리는 결정, 현강님·천명님 줄)는 사용자와 작업자 결정이 먼저라 맨 뒤에 둔다. C10-02는 1단계를 FieldView만 감싸는 데서 멈춰도 효과가 크다.
- 재생·연출 SSOT: 먼저 C3-01과 C1-05를 한다. 둘 다 줄이거나 한 줄씩 바꾸는 저위험 수정이고 GC 작업(bbbcb162) 파일도 건드리지 않는다. 이어서 C4-08 1단계(선택 인자 기본값만 제거)를 한다. 컴파일만 통과하면 확인이 끝난다. C1-38은 코드로는 4줄이지만 현강님이 11-02에 일부러 넣은 숫자 2라서, 지금 바로 현강님께 방향(DB 값 2로 되돌리기 또는 설정 칸 삭제)을 물어 두고 답이 오면 asset 값과 함께 한 커밋에 넣는다. 도약 쪽은 C1-11(끊김 판정 함수 하나)을 먼저 해서 BeforeEvent를 정리하고, C4-05(창구가 배치를 양보)는 그 위에 얹는다. C4-05는 bbbcb162에서 바뀐 CombatUnitPresentationWindow·CombatSceneEventPresenter를 고치므로 GC 작업을 Unity에서 확인한 뒤에 한다. C2-04와 C1-42는 플레이어 영향이 없고(dev-only) 품이 크며 호진님·천명님 줄이 대부분이다. 그래서 공유 메모로 합의한 뒤 맨 나중에 하나씩 한다.
- 큰 구조·다른 작업자 접점: 가장 먼저 C2-02 1단계를 권한다. RouteInput 조건 세 줄을 IsInputOwnerActive 하나로 바꾸는 일로, 동작이 같은지 소유자 6개로 확인했다. GC 미검증 파일도 아니어서 Unity에서 클릭 몇 번으로 바로 확인할 수 있다. 다음은 C5-01 1단계다. 재생 Gate와 Factory 두 곳에서 늘 거짓인 전환 경로를 빼면, 쓰지 않는 레이어·카메라 이름 검사 때문에 조립이 멈출 위험이 사라진다. 다만 실제로 주석 처리한 사람은 문서에 적힌 민형님이 아니라 호진님(b9c4f3b4)이라, 되살릴 계획을 호진님에게 먼저 물어야 한다. C1-22는 작고 안전하지만 바꿀 줄이 모두 현강님(현강님) 것이고 UnitHUD가 GlobalSystem이라 합의 뒤에 한 번에 하고, C1-57은 지금은 Core 요청 문서만 쓰면 된다. C2-01·C2-03·C4-02의 큰 구조 변경은 고칠 파일(CombatSceneDisplay·CombatSceneEventPresenter)이 bbbcb162 GC 작업과 겹친다. 그러니 그 작업을 Unity에서 확인한 뒤에, 그리고 기믹 줄을 계속 고치는 호진님과 시기를 맞춰 하는 것이 좋다. 그중 C4-02 1단계(미리보기 Factory ports를 이름 인자로 바꾸고 빠진 토네이도 외형 연출 확인)만 GC와 무관해서 먼저 해도 된다. C4-02 3단계는 기록된 결정 Q-S3 A를 뒤집는 일이라 사용자 결정이 필요하다.
