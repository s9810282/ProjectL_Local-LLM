# 전투 View 작은 묶음 셋 Task 계획 (SB-0~SB-d)

- 날짜: 2026-11-05
- 상태: **SB-0~SB-d 완료(11-05, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 §6 대기). 사용자 지시("정리해서 문서화해주고 그 뒤에 푸쉬해줘")로 Claude가 커밋·push(11-05, §9.7).** 결정(11-05 사용자 "권장으로 진행하고 Q-SB4만 Phase가 모두 종료 후로"): Q-SB1~Q-SB3 A, TP-1~TP-9 그대로, Q-SB4 = SB-a~SB-d를 이어서 하고 Unity 확인은 모든 Phase가 끝난 뒤 한 번에. 실행 기록은 §9. 묶음 끝 검토 낮음 둘(L1·L2)은 사용자 결정 대기(§9.5).
- 설계: [small-bundles-design](../specs/2026-11-05-combat-view-small-bundles-design.md)(SB1~SB3, 2판). 줄 번호는 HEAD `fedd5627` 기준이다.
- 이 계획은 설계 권장안(Q-SB1~Q-SB4 모두 A)을 전제로 쓴다. 다르게 정하면 해당 Phase를 고친다.
  - Q-SB1 A: C2-04는 DB 표 + DB 읽기·검사 일반화 + View까지 한다.
  - Q-SB2 A: 표시 쪽 분류 HashSet을 걷는다.
  - Q-SB3 A: Object 형식 예외 문구를 하나로 한다.
  - Q-SB4(정함): Phase마다 멈추지 않고 이어서 하며, Unity 확인은 모든 Phase가 끝난 뒤 한 번에 한다.
- 대상 문제: C4-08(중간, 독립성) 2단계, C2-04(중간, SOLID), C1-05(중간, SSOT) 2단계.
- 규칙:
  - View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
  - 동작은 같아야 한다. 설계 §4(지킬 것)와 §5(바뀌는 것)를 벗어나면 구현하지 않고 먼저 묻는다.
  - Phase마다 실행 기록(§9)을 적고 다음 Phase로 간다(Q-SB4). 묶음 끝(SB-d)에 검토 에이전트 하나를 돌리고 멈춰 보고한다.
  - 커밋·push는 사용자가 한다.

## 0. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | `CombatCueSinks.ForAttack`은 소리 창구만 null 검사한다. 이펙트·카메라는 지금 입구처럼 검사하지 않는다 | 지금 입구 셋이 검사하던 것은 소리 하나다(설계 P1). 공격 시작은 셋 다 생성자에서 필수로 받는다 |
| TP-2 | `CombatCueAnchors.SetProjectileTimings`는 자기 자신을 돌려준다(이어 쓰기). 부르는 곳은 원거리 클립 조립 하나다. 값을 넣지 않은 표는 null(= 코드 모션의 Cue·Hit)이다 | 다른 칸 설정(`Set`·`StartAt`)과 같은 꼴 |
| TP-3 | `CombatClipTimings` 새 생성자의 검사 실패 예외는 paramName 문자열 `"eventTime"`을 그대로 쓴다 | 예외 문구가 지금과 같아야 해서다(설계 §3 SB2) |
| TP-4 | `ReadTimings`는 클립마다 표 크기의 작은 배열(본 적 있음·시각)을 만든다 | DB 생성 때 한 번이라 Tick 경로가 아니다. 지금 지역 변수 열 개를 대신한다 |
| TP-5 | View 표 검사는 빈 정적 메서드 `CombatClipEventSignals.Validate()`를 `CombatAttackStart` 생성자가 불러 정적 생성자를 돌린다 | 설계 P10. 표 자체는 공격 시작을 모른다 |
| TP-6 | 커서는 시작·끝일 때 순번 −1을 낸다 | 받는 쪽이 종류로 먼저 가르므로 순번을 읽지 않는다 |
| TP-7 | DB 표의 정적 생성자는 줄마다 함수 이름·큐 시점이 겹치지 않는지, 큐 시점이 `Start`·`Hit`·`Contact`·`End`가 아닌지도 본다 | 겹치면 읽기·검사가 다른 번호를 고를 수 있다 |
| TP-8 | `ObjectViewSet`의 예외 → `Clear` → 다시 던짐, `Clear`·`Release`의 순서는 `_classified` 줄만 빼고 그대로다 | 설계 §4 |
| TP-9 | Phase마다 그 Phase가 바꾼 클래스의 클래스 설명(summary·remarks)과 걷은 멤버를 가리키던 cref를 같이 고친다 | 조정자 나누기 TP-9와 같다 |

## 1. SB-0 착수 전 확인

- [ ] 설계 결정 Q-SB1~Q-SB4와 TP-1~TP-9를 받는다.
- [ ] `git fetch origin feature/Yonggeun/Refector-Stage`와 `git status`로 다른 세션·원격 변경을 본다. 고칠 파일(§7)이 바뀌었으면 줄 번호를 다시 잡는다.
- [ ] 고칠 줄의 작성자를 `git blame`으로 다시 센다(설계 §1.4). 경계 커밋(`^…`) 줄은 얕은 clone이라 blame이 실제 작성자를 보이지 않으므로 GitHub 파일 이력(경로별 커밋 목록, 옛 경로 `Combat/Display/`도)으로 확인한다. `ObjectViewSet`·`TerrainViewSet`은 11-05에 확인함(용근님 줄).
- [ ] `Assets/Tests`·`Tool/Editor`에서 바꿀 멤버 이름을 grep한다: `CombatCueSinks`·`ComposeMelee`·`ComposeRanged`·`CombatCodeMotionAttack`·`CombatClipTimings`·`CombatClipEventCursor`·`CombatClipPlayback`·`CombatClipEventKind`·`AnchorOf`·`CombatClipEvents.Read`·`CombatClipData(`·`_classified`. 설계 때는 시험 0건, 에디터 주석 1건(`CharacterClipCopier:16`)이었다. 더 생기면 멈추고 묻는다.

## 2. SB-a 창구 묶음 (설계 SB1)

- [ ] **Ta.1** `Presentation/Pieces/CombatCueComposer.cs`의 `CombatCueSinks`
  - 생성자 인자 `projectileTimings`와 속성 `ProjectileTimings`를 걷는다(넘기는 곳은 원거리 클립 하나).
  - 공격용 입구 `internal static CombatCueSinks ForAttack(CombatEffectWindow effects, CombatCameraWindow camera, CombatEventAudio sound, CombatEvent soundEvent, CombatProjectileSounds projectileSounds)`를 더한다. `sound`가 null이면 `ArgumentNullException(nameof(sound))`(TP-1). 안에서 생성자를 이름 있는 인자로 부른다.
  - 클래스 요약에 "공격 조립은 `ForAttack` 하나로 만든다"를 더한다(TP-9).
- [ ] **Ta.2** `Presentation/Pieces/CombatCueAnchors.cs`: `SetProjectileTimings(IReadOnlyList<CombatProjectileCueTiming> timings)`(null 거절, 자기 반환, TP-2)와 읽기 속성 `ProjectileTimings`.
- [ ] **Ta.3** `Presentation/Pieces/CombatProjectileCueComposer.cs`: `Add`의 마지막 인자 `timings`를 걷고 `anchors.ProjectileTimings`를 읽는다. `CombatCueComposer.Add`의 호출(93)을 맞춘다.
- [ ] **Ta.4** `Presentation/Pieces/CombatClipAttack.cs`
  - `ComposeMelee(shape, clip, cues, facing, release, CombatCueSinks sinks, Pose? actorBody, CombatAttackLead lead)`
  - `ComposeRanged(shape, clip, cues, facing, fire, release, CombatCueSinks sinks, Pose? actorBody, CombatAttackLead lead)`: `CombatClipRangedAttack.Add`가 낸 시점을 `anchors.SetProjectileTimings(projectileTimings)`로.
  - 두 입구의 `sound == null` 검사를 `sinks == null` 검사로 바꾼다(소리 검사는 `ForAttack`).
  - `AddCues(composition, shape, cues, anchors, sinks, actorBody)`는 받은 묶음을 그대로 `CombatCueComposer.Add`에 넘긴다.
  - 문서 주석: 인자 설명(`effects`·`camera`·`projectileSounds` → `sinks`), remarks "창구·자리·앞 항목 인자는 기본값 없이" 문장을 "창구는 묶음 하나(`CombatCueSinks.ForAttack`), 자리·앞 항목은 기본값 없이"로(TP-9).
- [ ] **Ta.5** `Presentation/Pieces/CombatCodeMotionAttack.cs`: `Compose(shape, cues, tryBeginMotion, release, CombatCueSinks sinks, Pose? actorBody, CombatAttackLead lead)`. 안의 `new CombatCueSinks(...)`를 받은 `sinks`로. 문서 주석 같이.
- [ ] **Ta.6** `Playback/Support/CombatAttackStart.cs`: private `SinksFor(CombatAttackShape shape, CombatProjectileSounds projectileSounds) => CombatCueSinks.ForAttack(_effects, _camera, _sound, shape.Source, projectileSounds)`. 세 호출(99-102·131-133·134-135)을 `SinksFor(shape, CombatProjectileSounds.From(settings))`(코드 모션·원거리)·`SinksFor(shape, null)`(근접)로.
- [ ] **Ta.7** 확인 grep
  - `new CombatCueSinks(effects, camera, sound, shape.Source` 0건, `projectileTimings:` 0건
  - `ProjectileTimings`는 `CombatCueAnchors`·`CombatProjectileCueComposer`에만
  - 다른 처리기(피격·사망·특성·낙석·대신 맞기·순간 큐)의 묶음 생성은 그대로
- 동작 대조(SB-a 기록에 적음): 세 길마다 묶음의 다섯 칸(이펙트·카메라·소리·소리 사건·투사체 소리)이 옛 값과 같은지, 원거리 클립의 탄별 시점이 같은 목록으로 투사체 소리 붙이기에 가는지, 코드 모션은 시점 null인지, 큐 순서가 그대로인지.

## 3. SB-b 클립 이벤트 표 (설계 SB2)

DB 쪽(Tb.1~Tb.8) → View 쪽(Tb.9~Tb.15) 순서다. 한 Phase로 묶는다(DB만 바꾸면 View가 걷은 멤버를 부르므로).

- [ ] **Tb.1** 새 `DataBaseSystem/Data/View/CombatPresentation/CombatClipEventIds.cs`(+ `.meta`, Unity 기본 형식)
  - `internal static class CombatClipEventIds`. 줄 셋 (Event, `nameof(CombatAnimationEvents.ClipEvent)`, Cue), (Event2, `…ClipEvent2`, Cue2), (Event3, `…ClipEvent3`, Cue3).
  - 명시적 정적 생성자에서 검사(설계 P4, TP-7): 모든 `CombatClipEventId` 값에 줄이 하나씩, 첫 줄 = `Event`, 함수 이름·큐 시점 겹침 없음, 큐 시점이 `Start`·`Hit`·`Contact`·`End`가 아님. 어기면 `InvalidOperationException`.
  - 입구: `All`(읽기 전용, 번호 순), `Count`, `IsDefined(id)`, `IndexOf(id)`, `FunctionNameOf(id)`, `AnchorOf(id)`, `TryFindByFunction(string name, out CombatClipEventId id)`, `IsEventAnchor(CombatCueAnchor anchor)`. 반복은 인덱스 루프.
- [ ] **Tb.2** `CombatClipTimings.cs`
  - 생성자를 `(float startTime, float endTime, IReadOnlyList<EventTiming> events)` 하나로(설계 P5). 검사 = 새 `FindTimeError(startTime, endTime, events)`, 실패면 `ArgumentOutOfRangeException("eventTime", error)`(TP-3). 받은 목록은 배열로 복사해 읽기 전용으로.
  - 목록 규칙(검사 안): 비어 있지 않고, 번호는 정의된 번호이며 표 순서대로 겹치지 않고, 첫 항목은 `Event`. 어기면 `"중간 이벤트 목록은 기준 이벤트(ClipEvent)부터 번호 순이어야 합니다"`(호출자 오류에서만 나는 새 문구, 읽기는 늘 지킴).
  - `FindTimeError` 본문은 지금 순서·문구 그대로(설계 §3 SB2 "검사 순서·문구"), 선택 번호 이름은 `CombatClipEventIds.FunctionNameOf`.
  - 옛 `FindTimeError(start, event, end, e2, e3)`는 걷는다.
  - `TryGetEventTime`의 번호 검사를 `CombatClipEventIds.IsDefined`로.
- [ ] **Tb.3** `CombatClipEvents.cs`
  - `ReadTimings`: 표 크기 배열 둘(본 적 있음·시각, TP-4). 함수 이름이 `ClipStart`·`ClipEnd`면 지금처럼, 아니면 `TryFindByFunction`으로 번호 칸에 `Take`, 못 찾으면 모르는 이름 예외(이름 목록 = `ClipStart` · 표 순서 · `ClipEnd`). 루프 뒤 `RequirePresent` ClipStart → 표 첫 줄(ClipEvent) → ClipEnd. 있는 번호만 표 순서로 목록을 만들어 `FindTimeError` → 길이 검사 → 생성.
  - 호출자 없는 `Read`를 걷는다(설계 P9).
- [ ] **Tb.4** `CombatClipShot.cs`: 번호 검사를 `CombatClipEventIds.IsDefined`로(문구 그대로).
- [ ] **Tb.5** `CombatClipData.cs`: 호출자 없는 세 시각 생성자와 `FindTimeError`를 걷는다(설계 P9). 남는 생성자의 `exception cref`를 `CombatClipTimings`로.
- [ ] **Tb.6** `CombatPresentationCueRules.cs`
  - `AllowsAnchor` 원거리 갈래(86-87): `anchor is Start or Cue or Hit or Contact or End || CombatClipEventIds.IsEventAnchor(anchor)`.
  - 공격 클립 검사(216-217): 표의 둘째 줄부터 돌며 `FindEventError`, 처음 나온 오류를 돌려준다(Event2 → Event3 순서 그대로).
  - `UsesAdditionalEvents`: 표의 둘째 줄부터 돌며 `UsesAnchor`.
  - `FindEventError`의 `AnchorOf(id)` → `CombatClipEventIds.AnchorOf(id)`. `AnchorOf` 메서드는 걷는다(설계 P6).
  - 요약(59)의 "Cue2·3"을 "선택 이벤트 시점(Cue2 이상)"으로.
- [ ] **Tb.7** `Table/Trait/Item/TraitActivationVisual.cs:159` 문구의 "Cue2·Cue3"을 "선택 이벤트 시점(Cue2 이상)"으로(조건 그대로).
- [ ] **Tb.8** `Tool/Editor/CharacterClipCopier.cs:16` 주석: `CombatClipEvents.Read` → `CombatClipEvents.ReadTimings`, `CombatClipData.FindTimeError` → `CombatClipTimings.FindTimeError`.
- [ ] **Tb.9** 새 `Presentation/Pieces/CombatClipEventSignals.cs`(+ `.meta`)
  - `internal static class CombatClipEventSignals`: 줄 셋 (Event → `ClipEvent`), (Event2 → `ClipEvent2`), (Event3 → `ClipEvent3`). 명시적 정적 생성자에서 `CombatClipEventIds.All`의 번호마다 줄이 하나씩 있는지 검사.
  - 입구 `SignalOf(id)`(없으면 `ArgumentOutOfRangeException` — 정적 생성자가 막아 닿지 않음), `Validate()`(빈 몸, TP-5).
- [ ] **Tb.10** `CombatClipEventCursor.cs`: `CombatClipEventKind`를 `Start = 1`·`Event = 2`·`End = 3`으로. `TryNext(float time, out CombatClipEventKind kind, out int eventIndex)`(시작·끝은 −1, TP-6). `KindOf`와 세 시각 생성자를 걷는다(설계 P9).
- [ ] **Tb.11** `CombatClipPlayback.cs`: 생성자 `(ICombatUnitClip lease, CombatClipData data, Action started, Action<int> eventReached, Action ended)`. 필드 `_eventTiming`·`_eventTiming2`·`_eventTiming3` → `_eventReached` 하나. `Emit`: Start → `_started?.Invoke()`, Event → `_eventReached?.Invoke(eventIndex)`, End → `_ended?.Invoke()`.
- [ ] **Tb.12** `CombatClipPiece.cs` `Begin`: `IReadOnlyList<EventTiming> events = _clip.Timings.Events`, `_eventSignals = new Action[events.Count]`, 순번마다 `context.SignalCallback(CombatClipEventSignals.SignalOf(events[i].Id))`. 그다음 `_playback = new CombatClipPlayback(context.Clip(_unit), _clip, context.SignalCallback(ClipStart), i => _eventSignals[i](), null)`, `_playback.Begin()`. 신호 입구를 받는 순서는 지금과 같다(이벤트 신호 → 시작 신호, 지금도 Event2·3을 먼저 받고 ClipStart·ClipEvent를 생성자 인자로 받음 — 받는 순서는 동작에 영향 없음, 확인해 기록).
- [ ] **Tb.13** `CombatClipRangedAttack.cs`: `SignalOf`를 걷고 `CombatClipEventSignals.SignalOf`, `ConnectEventCues`는 `CombatClipEventIds.AnchorOf(id)`.
- [ ] **Tb.14** `CombatCueAnchors.cs`: `KindOf`에서 `Cue2 or Cue3` 대신 `_ when CombatClipEventIds.IsEventAnchor(anchor) => Signal`(Cue·Hit·End는 그대로). `Set` 문구(48)를 "뼈대 신호에 잇는 기준 시점은 Cue·선택 이벤트 시점·Hit·End입니다."로.
- [ ] **Tb.15** `CombatAttackStart.cs` 생성자 끝에 `CombatClipEventSignals.Validate();`(TP-5).
- [ ] **Tb.16** 확인 grep
  - `Event2\b`·`Event3\b`·`ClipEvent2`·`ClipEvent3`·`Cue2`·`Cue3`이 남는 곳: 값 정의(`CombatClipEventId`·`CombatCueAnchor`·`CombatPieceSignal`), 이름표 메서드, 두 표의 줄, 툴팁·문서 주석뿐
  - `CombatClipEventKind.Event2`·`AnchorOf(`(표 밖)·`CombatClipEvents.Read(`·`new CombatClipData(clip, ` 세 시각 0건
- 동작 대조(SB-b 기록에 적음): `ReadTimings` 옛·새 갈래를 설계 §3 SB2 "검사 순서·문구" 줄마다, 커서의 같은 시각 순서, 신호 입구가 있는 이벤트만·한 번씩, `AllowsAnchor`·공격 클립 검사·`UsesAdditionalEvents`의 결과.

## 4. SB-c 환경 판정 (설계 SB3)

- [ ] **Tc.1** `Display/Map/CombatEnvironmentFacts.cs`: `IsTerrain(EnvironmentSemanticBindings, long sourceId)`, `IsObject(…, long sourceId)`, `TryGetObject(bindings, MovementTileFact source, out ObjectSourceFact item)`(분류됐는데 형식이 다르면 `InvalidOperationException($"[CombatEnvironmentFacts] Object 분류와 Buffer 원본 형식이 다릅니다: {source.Id}.")`, 설계 P3), `TryGetObstacleAt(bindings, obstacles, structure, CellCoord cell, out ObstacleContentProfile profile)`, `IsDustScreen(MovementTileFact source, out TraitTrailFact trail)`. 모두 인자 null 검사, 인덱스 루프. 클래스 요약에 맡는 판정 목록을 고친다(TP-9).
- [ ] **Tc.2** `Input/Assist/CombatEnvironmentText.cs` `Build`
  - 54 → `CombatEnvironmentFacts.IsDustScreen(source, out TraitTrailFact trail)`
  - 62 → `IsTerrain(bindings, source.Id)`
  - 68-71 → `TryGetObject(bindings, source, out ObjectSourceFact item)`
  - 79-81 → `if (!CombatEnvironmentFacts.TryGetObstacleAt(bindings, obstacles, structure, cell, out ObstacleContentProfile obstacle)) continue;`
  - 쓰지 않게 되는 `Contains(IReadOnlyList<long>, long)`를 걷는다(`ContainsCell`은 남음). 호진님 줄 49-51·78은 그대로다.
- [ ] **Tc.3** `Input/Interaction/CombatHazardLookup.cs` `Collect`: 38-41 → `TryGetObject` + `MineSourceFact`·`IsShown`, 49-51 → `TryGetObstacleAt` + `PowderBarrel`. `Contains` 둘을 걷는다. 클래스 요약의 조건 문장은 그대로 맞는지 본다.
- [ ] **Tc.4** `Input/Assist/CombatDustScreenHoverCells.cs` `Append`: 두 반복의 `is TraitTrailFact` + `IsVisibleDust`를 `CombatEnvironmentFacts.IsDustScreen(sources[k], out TraitTrailFact trail)` + `IsShown(trail)`로 바꾸고 `IsVisibleDust`를 걷는다. 조건은 모두 부작용 없는 읽기라 순서가 바뀌어도 결과가 같다(같은 칸·같은 출처·보임).
- [ ] **Tc.5** `Display/Map/ObjectViewSet.cs`: `_classified`(22)와 채우기(40-41)·비우기(88·127)를 걷고, 47-49를 `if (!CombatEnvironmentFacts.TryGetObject(bindings, tile, out ObjectSourceFact source)) continue;`로(TP-8). `Display/Map/TerrainViewSet.cs`: `_classified`(27)·`Classify`(202-206)와 부르는 곳(51·97)·비우기(81·105·166)를 걷고, `TryShow`(217)는 `CombatEnvironmentFacts.IsTerrain(bindings, source.Id)`. `TryShow`가 분류를 받도록 인자를 더한다(지금은 필드에서 읽음).
- [ ] **Tc.6** `Display/Map/FieldObjectPresenter.cs` 102-108·`FieldTerrainPresenter.cs` 211-217: 안쪽 반복을 `IsObject`·`IsTerrain`으로(문구 그대로).
- [ ] **Tc.7** 확인 grep
  - `ObjectSourceIds`·`TerrainSourceIds`·`ObstacleEntityIds`는 `CombatEnvironmentFacts`와 Core에만
  - "Object 분류와" 문구는 `CombatEnvironmentFacts` 하나
  - `TraitKey.DustScreen` 판정은 `CombatEnvironmentFacts` 하나(다른 곳은 외형 키 조회 등 판정 아닌 쓰임만)
  - `_classified` 0건
- 동작 대조(SB-c 기록에 적음): `Build`·`Collect`의 원본 순서·갈래 순서, `ObjectViewSet`·`TerrainViewSet`의 분류 → 형식 → 보임 순서, 회전 검사 문구.

## 5. SB-d 문서·공유·묶음 끝 검토

- [ ] **Td.1** 폴더 배치 안내 `viewDocs/combat-view-folder-guide.md`: `Presentation/Pieces/` 줄에 `CombatClipEventSignals`, §2에 "클립 이벤트 번호마다 다른 지식" 행(새 번호는 값 넷 + DB 표 `CombatClipEventIds`·View 표 `CombatClipEventSignals` 한 줄씩, 재생·조각·커서·DB 읽기·검사는 고치지 않음), "환경 원본을 보일지" 행에 분류·형식·장애물 칸·먼지 연막 판정도 `CombatEnvironmentFacts`라고 더함, 공격 조립 창구는 `CombatCueSinks.ForAttack` 하나라는 규칙.
- [ ] **Td.2** 설계 §13 구현 메모.
- [ ] **Td.3** 공유: 호진님 실제 줄은 바뀌지 않아 메모 없음(설계 §10). 경계 커밋 줄은 [후속 수정 계획](2026-11-04-combat-view-evaluation-fixes-plan.md) §5.3 표에 더한다(Q-N11). 민형님은 Factory diff 0이면 없음.
- [ ] **Td.4** 묶음 끝 검토: 새 에이전트 하나가 HEAD와 바뀐 판을 대조한다(정적 컴파일, 설계 §4 표, `ReadTimings` 문구·순서, 신호 입구 규칙, 다른 작업자 줄). 높음·중간은 반영, 낮음은 권장과 함께 보고. 세 문제를 '다 풀림'으로 셀지도 판정받는다.
- [ ] **Td.5** 평가 아티팩트 '적용 기록' 8차(CLAUDE.md 규칙): `applied.json`에 C4-08·C2-04·C1-05와 파일·남은 단계를 더하고 페이지를 다시 만든다. '다 풀림' 여부는 Td.4로 정한다.
- [ ] **Td.6** 후속 계획 §6.6 표·§6.7 점수 줄, CLAUDE.md 현재 상태, 이 계획 §9 실행 기록.

## 6. Unity 확인 (사용자)

모든 Phase에 공통: 컴파일 오류 없음, Boot → 전투 진입, 콘솔 예외 없음.

| Phase | 번호 | 확인 |
|---|---|---|
| SB-a | Ua-1 | 클립 근접 공격: 이펙트·흔들림·공격 소리가 전과 같다 |
| | Ua-2 | 클립 원거리 공격(예: 2연발 Akimbo): 탄마다 발사·비행·착탄 소리, Cue2 큐가 전과 같다 |
| | Ua-3 | 클립 없는 원거리(코드 모션) 공격: 비행·착탄 소리와 이펙트가 전과 같다 |
| | Ua-4 | 특성 추가 공격(공격 표시 덮어쓰기)과 대신 맞기 공격이 전과 같다 |
| SB-b | Ub-1 | Boot의 DB 생성에서 예외가 없다(모든 유닛 외형의 클립 이벤트 읽기) |
| | Ub-2 | 클립 근접 공격의 타격 시점(ClipEvent + 선딜레이)이 전과 같다 |
| | Ub-3 | ClipEvent2를 쓰는 원거리 클립의 탄별 발사·Cue2 이펙트·소리가 전과 같다 |
| | Ub-4 | (선택) 클립 하나의 ClipEvent2 이름을 잠깐 틀리게 바꾸면 Boot가 지금과 같은 문구로 멈춘다 — 확인 뒤 되돌림 |
| SB-c | Uc-1 | 지형·오브젝트·특성 흔적이 전과 같이 그려진다 |
| | Uc-2 | 공개 전 지뢰는 칸에도 위험 범위 호버에도 안 나오고, 공개 뒤에는 나온다 |
| | Uc-3 | 화약통 위험 범위 호버가 전과 같다 |
| | Uc-4 | 환경 설명창(지형·오브젝트·장애물·먼지 연막)이 그려진 것만 설명한다 |
| | Uc-5 | 먼지 연막 호버 칸이 전과 같다 |

## 7. 바뀌는 파일과 커밋 묶음 (사용자)

경로는 `ViewSystem/Combat/`(V) 또는 `DataBaseSystem/Data/View/`(DV) 기준이다.

| Phase | 새 파일(+ `.meta`) | 고치는 파일 |
|---|---|---|
| SB-a | — | V `Presentation/Pieces/CombatCueComposer`·`CombatCueAnchors`·`CombatProjectileCueComposer`·`CombatClipAttack`·`CombatCodeMotionAttack`, V `Playback/Support/CombatAttackStart` |
| SB-b | DV `CombatPresentation/CombatClipEventIds`, V `Presentation/Pieces/CombatClipEventSignals` | DV `CombatPresentation/CombatClipTimings`·`CombatClipEvents`·`CombatClipShot`·`CombatClipData`·`CombatPresentationCueRules`, DV `Table/Trait/Item/TraitActivationVisual`, V `Presentation/Pieces/CombatClipEventCursor`·`CombatClipPlayback`·`CombatClipPiece`·`CombatClipRangedAttack`·`CombatCueAnchors`, V `Playback/Support/CombatAttackStart`, `Tool/Editor/CharacterClipCopier`(주석) |
| SB-c | — | V `Display/Map/CombatEnvironmentFacts`·`ObjectViewSet`·`TerrainViewSet`·`FieldObjectPresenter`·`FieldTerrainPresenter`, V `Input/Assist/CombatEnvironmentText`·`CombatDustScreenHoverCells`, V `Input/Interaction/CombatHazardLookup` |
| SB-d | — | 문서(폴더 배치 안내·설계·이 계획·후속 계획·CLAUDE.md) |

- Factory 파일은 고치지 않는다. 새 `.cs`는 `.meta`와 같은 커밋에 넣는다.
- Phase마다 한 커밋으로 나눠도 되고 묶음 하나로 해도 된다.

## 8. 위험과 되돌리기

- SB-b의 DB 읽기 문구·순서가 어긋나는 것: Tb.16 뒤 옛 판(`git show HEAD:…`)과 줄마다 대조하고, Ub-1·Ub-4로 확인한다. 어긋나면 그 Phase만 되돌린다.
- 정적 생성자 예외가 형식 초기화 예외로 감싸져 원인 문구가 안쪽으로 들어가는 것: 줄 누락은 개발 중 데이터 오류라 받아들인다(설계 §11).
- 같은 파일(`CombatAttackStart`·`CombatCueAnchors`)을 SB-a·SB-b가 둘 다 고친다: Phase 순서대로 하고 SB-b 착수 때 SB-a 판을 기준으로 줄을 다시 잡는다.

## 9. 실행 기록

### 9.1 SB-0 착수 전 확인 (2026-11-05, 코드 변경 없음)

- 결정: 사용자 "권장으로 진행하고 Q-SB4만 Phase가 모두 종료 후로" → Q-SB1~Q-SB3 A, TP-1~TP-9 그대로, Q-SB4 = Phase마다 멈추지 않고 SB-d까지 이어 하며 Unity 확인(§6)은 끝에 한 번.
- 원격: `git fetch` 뒤 원격 브랜치 = HEAD `fedd5627`. 작업 트리의 다른 변경은 이 묶음 문서(설계·계획·후속 계획·CLAUDE.md)뿐.
- 시험·에디터 grep: 바꿀 멤버를 쓰는 시험 0건. 에디터는 `CharacterClipCopier:16` 주석 하나(설계 때와 같음). `AnchorOf`로 걸린 에디터 넷은 다른 형식(`CombatStageSoDocument.AnchorOf`)이라 무관, `CombatStageSoVisuals:63`은 `IsKnownToPlayer` 사본 주석이라 무관.
- 작성자(얕은 clone이라 경계 커밋 줄은 GitHub 파일 이력으로 확인, 옛 경로 `Combat/Display/`·`Combat/` 포함):
  - 클립 커서·재생·조각(`^83fb2767` 표시), `CombatClipEvents`·`CombatClipData`(`^ad7e6b28` 표시): 처음 만든 커밋은 용근 `e0ee626d`(10-19 클립 시스템)이고 그 뒤도 용근 커밋뿐 → 우리 줄.
  - `CombatEnvironmentFacts`·`CombatHazardLookup`: 용근 `5de448e3`(10-20)이 만듦 → 우리 줄.
  - `ObjectViewSet`·`TerrainViewSet`: 용근 `b5b13961`·`7ec2b981`(10-12). 현강님 `63a1c23e`는 종류 해석 줄만 고쳐 SB3 줄과 안 겹침 → 우리 줄(설계 §1.4 보충).
  - `FieldObjectPresenter`: 용근 커밋뿐. `FieldTerrainPresenter`의 회전 검사(`ValidateTerrainRotations`): 용근 `7ec2b981`(10-12)이 쓴 판과 지금 글자가 같다 → 우리 줄로 본다(그 사이 파일을 고친 호진님 `89cbc44e`·`34ee0de4`의 줄 단위 내용은 보지 않았음).
  - **`CombatEnvironmentText`: 현강님 `63a1c23e`(10-17)이 만든 파일이다.** SB3가 바꾸는 줄 중 지형·오브젝트 분류 갈래(옛 62·68)·형식 예외(옛 70-71)·장애물 생존·점유 줄(옛 80)은 현강님 줄이고(용근 `5de448e3`은 보임 줄과 장애물 찾기 → `TryGetObstacle` 줄만 고침 — 그 커밋 diff로 확인), 먼지 연막 줄(54)은 용근 `950a52c0`. → 현강님 공유 메모에 적는다(Td.3). 호진님 줄(토네이도·기믹 반복)은 바뀌지 않는다.

### 9.2 SB-a 창구 묶음 (2026-11-05, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Ta.1 `CombatCueSinks`: 생성자 인자·속성 `ProjectileTimings` 삭제, 공격용 입구 `ForAttack(effects, camera, sound, soundEvent, projectileSounds)`(소리 null 검사, 이름 있는 인자로 생성), 클래스 요약에 공격 입구·탄별 시점 자리. 안 쓰게 된 `using System.Collections.Generic` 삭제.
- Ta.2 `CombatCueAnchors`: `SetProjectileTimings`(null 거절, 자기 반환)·`ProjectileTimings`(넣지 않으면 null).
- Ta.3 `CombatProjectileCueComposer.Add`: 마지막 인자 삭제, `settings`가 있을 때만 `anchors.ProjectileTimings`를 읽음. `CombatCueComposer.Add`의 호출 한 줄.
- Ta.4·Ta.5 세 입구가 `CombatCueSinks sinks` 하나를 받음(`sound == null` 검사 → `sinks == null`), 원거리 클립은 `AnchorsOf(...).SetProjectileTimings(projectileTimings)`, `AddCues(…, sinks, actorBody)`. 문서 주석(인자·remarks)을 묶음 기준으로. 안 쓰게 된 using(`Playback.Support`·`Presentation.Cameras`·`Presentation.Effects`)을 두 파일에서 뺌(남은 본문·cref에서 그 네임스페이스의 형식을 쓰지 않음을 형식 이름 목록으로 확인).
- Ta.6 `CombatAttackStart.SinksFor(shape, projectileSounds)` = `ForAttack(_effects, _camera, _sound, shape.Source, projectileSounds)`, 코드 모션·원거리 = `From(settings)`, 근접 = null.
- Ta.7 grep: 옛 생성 줄·`projectileTimings:` 0건, `ProjectileTimings`는 묶음 밖 둘(기준 시점 표·투사체 소리 붙이기), 다른 처리기(피격·사망·특성·낙석·대신 맞기·순간 큐)의 묶음 생성 그대로(인자 위치 그대로라 컴파일 영향 없음).
- 동작 대조: 세 길의 묶음 다섯 칸이 옛 값과 같다(이펙트·카메라·소리 = 공격 시작의 필드, 소리 사건 = `shape.Source`, 투사체 소리 = 코드 모션·원거리 `From(settings)`·근접 null). 원거리 클립의 탄별 시점은 같은 목록(`CombatClipRangedAttack.Add`가 늘 null 아닌 읽기 전용 배열을 냄)이 투사체 소리 붙이기로 가고, 코드 모션·근접은 null(= Cue·Hit). 큐 붙이는 순서는 `CombatCueComposer` 그대로.
- 계획과 다른 점: 인자 계산 순서 하나 — 공격 시작 호출에서 `CombatProjectileSounds.From(settings)`가 `ActorBodyOf(...)`보다 먼저 계산된다(인자 자리가 바뀜). 둘 다 부작용 없는 읽기이고 `From`은 null 설정에서만 예외인데 두 자리 모두 설정이 null이 아니라 결과가 같다.

### 9.3 SB-b 클립 이벤트 표 (2026-11-05, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tb.1 새 `CombatClipEventIds`(+ `.meta`, GUID `a2ba53a5…`): 줄 셋, 명시적 정적 생성자 검사(줄 있음·첫 줄 = Event·번호 오름차순·모든 번호 값에 줄·함수 이름 비지 않음·시작/끝 이름표 아님·겹침 없음·큐 시점 정의됨·Start/Hit/Contact/End 아님·겹침 없음), 입구 `All`·`Count`·`IsDefined`·`IndexOf`·`FunctionNameOf`·`AnchorOf`·`TryFindByFunction`·`IsEventAnchor`. 계획에 없던 것: 기준 이벤트 이름 `Primary`(읽기·목록 검사가 "첫 줄"을 이름으로 부르려고), 표 검사의 번호 오름차순·시작/끝 이름표 금지·정의된 큐 시점 줄.
- Tb.2 `CombatClipTimings`: 생성자 `(start, end, events)` 하나(복사, 실패 paramName `"eventTime"`), `FindTimeError(start, end, events)`(목록 모양 검사 먼저 → 옛 순서·문구, 선택 이름 = 표의 함수 이름), `TryGetEventTime` 번호 검사 = `IsDefined`. 목록 모양이 틀리면 "중간 이벤트 목록은 기준 이벤트(ClipEvent)부터 번호 순이어야 합니다"(읽기는 늘 지켜 호출자 오류에서만).
- Tb.3 `ReadTimings`: 표 크기 배열 둘(`ref seen[index]`), 시작·끝은 그대로, 중간은 `TryFindByFunction`, 모르는 이름 문구의 이름 목록 = 시작 · 표 순서 · 끝(옛 "ClipStart·ClipEvent·ClipEvent2·ClipEvent3·ClipEnd"와 같은 글자), 없음 검사 시작 → 기준 → 끝, 있는 번호만 표 순서로 목록. `Read` 삭제.
- Tb.4 `CombatClipShot`·Tb.5 `CombatClipData`(세 시각 생성자·`FindTimeError` 삭제, 남은 생성자에 인자·예외 설명 옮김)·Tb.6 `CombatPresentationCueRules`(원거리 갈래 `|| IsEventAnchor`, 선택 이벤트 검사·`UsesAdditionalEvents`는 표의 둘째 줄부터, `AnchorOf` 삭제, 요약 문구)·Tb.7 `TraitActivationVisual:159` 문구·Tb.8 `CharacterClipCopier:16` 주석.
- Tb.9 새 `CombatClipEventSignals`(+ `.meta`, GUID `dcb45fc6…`): 줄 셋, 정적 생성자 검사(DB 표의 번호마다 줄 하나, 표에 없는 번호 줄 없음, 신호 겹침 없음), `SignalOf`(옛 문구 그대로), 빈 `Validate()`.
- Tb.10 커서: 종류 `Start`·`Event`·`End`(값 1·2·3), `TryNext(time, out kind, out eventIndex)`(시작·끝 −1), `KindOf`·세 시각 생성자 삭제. Tb.11 재생: `Action<int> eventReached` 하나, `Emit`은 종류 switch(모르는 종류 문구 그대로). Tb.12 조각: `Begin`이 `Timings.Events` 순번마다 신호 입구를 먼저 받고(배열), 재생 콜백은 `EventReached(index)`. Tb.13 원거리 조립: `SignalOf` 삭제, 두 표를 부름. Tb.14 `CombatCueAnchors.KindOf`에 `_ when IsEventAnchor`, `Set` 문구. Tb.15 공격 시작 생성자 끝 `CombatClipEventSignals.Validate()`.
- Tb.16 grep: `Event2`·`Event3`·`ClipEvent2`·`ClipEvent3`·`Cue2`·`Cue3`이 남는 곳은 값 정의(`CombatClipEventId`·`CombatCueAnchor`·`CombatPieceSignal`), 이름표 메서드, 두 표의 줄, 툴팁·문서 주석뿐. `CombatClipEventKind.Event2`·표 밖 `AnchorOf(`·`CombatClipEvents.Read(`·세 시각 `new CombatClipData(` 0건.
- 동작 대조: `ReadTimings`의 함수 이름 갈래(이름이 서로 달라 if 순서 무관)·없음 검사 순서·시각 검사 순서와 문구·길이 검사가 옛 판과 같다. 커서의 같은 시각 순서(시작 → 목록 순 → 끝) 그대로. 신호 입구는 있는 이벤트만 한 번씩, `Begin` 전에 받는다 — 받는 순서는 옛 판(Event2·Event3 → ClipStart·ClipEvent)과 달라졌지만(ClipEvent·Event2·Event3 → ClipStart) 입구는 비트 기록이라 순서가 동작에 영향 없다. `AllowsAnchor` 원거리 집합 {Start·Cue·Hit·Contact·End·Cue2·Cue3} 그대로, 선택 이벤트 검사·`UsesAdditionalEvents`는 Event2 → Event3 순서 그대로.

### 9.4 SB-c 환경 판정 (2026-11-05, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- Tc.1 `CombatEnvironmentFacts`: `TryGetObstacleAt`(구조물 null 검사 → 생존 → 점유 칸 → `TryGetObstacle`, 분류 인자 검사는 그 함수가 — 옛 호출 자리의 검사 순서를 그대로 지키려고), `IsTerrain`·`IsObject`, `TryGetObject`(문구 `[CombatEnvironmentFacts] Object 분류와 Buffer 원본 형식이 다릅니다: {id}.`), `IsDustScreen`, private `Contains`·`Occupies`(인덱스 루프), 클래스 요약에 맡는 판단 목록. 계획과 다른 점: `IsDustScreen`은 null 인자를 검사하지 않고 false다(옛 형식 패턴 `source is TraitTrailFact`과 같게 — 먼지 연막 호버가 그 순서로 부름).
- Tc.2 `CombatEnvironmentText.Build`: 먼지 연막·지형·오브젝트·장애물 갈래를 새 함수로, `Contains` 삭제(`ContainsCell`은 토네이도·기믹이 씀). 호진님 줄 그대로.
- Tc.3 `CombatHazardLookup.Collect`: 칸 → `TryGetObject` → 지뢰·보임, 장애물은 `TryGetObstacleAt` + 화약통. `Contains` 둘 삭제.
- Tc.4 `CombatDustScreenHoverCells.Append`: `IsDustScreen` + `IsShown`, `IsVisibleDust` 삭제(조건은 모두 부작용 없는 읽기).
- Tc.5 `ObjectViewSet`·`TerrainViewSet`: `_classified`와 채우기·비우기 삭제, `TerrainViewSet.Classify` 삭제, `TryShow`는 정적 함수로 분류 인자를 받음, `VisitShown`의 try/finally(분류 집합 비우기만 하던 것) 삭제. 예외 → `Clear` → 다시 던짐, `Clear`·`Release` 순서는 그대로(TP-8).
- Tc.6 회전 검사 둘: 안쪽 반복을 `IsObject`·`IsTerrain`으로(문구 그대로).
- Tc.7 grep: 분류 목록(`ObjectSourceIds`·`TerrainSourceIds`·`ObstacleEntityIds`)은 ViewSystem에서 `CombatEnvironmentFacts`만 읽는다. "Object 분류와 Buffer 원본 형식" 문구는 한 곳. `TraitKey.DustScreen` 판정은 한 곳(`TraitDescriptionBindings`의 사전 열쇠는 판정이 아님). `_classified` 0건.
- 범위 밖으로 본 것(보고만): View DB `StageEnvironmentVisualDataSO.cs:74-77`이 같은 회전 ↔ 분류 대조를 LINQ로 한 벌 더 갖는다(DB 층이라 View 판단을 부를 수 없음, 호출 여부·작성자는 확인하지 않음).
- 동작 대조: 환경 설명·위험 범위는 같은 원본·같은 갈래 순서(보임 → 흔적 → 지형 → 오브젝트, 그다음 장애물). 표시는 분류 → 형식 예외 → 보임 순서 그대로. 바뀐 것은 설계 §5대로 형식 예외 문구 하나와(분류 계약 오류에서만), `TerrainViewSet`에서 분류 목록이 null인 깨진 Buffer일 때 예외가 나는 자리(옛 = `try` 밖 `Classify`, 새 = `try` 안 → 정리 뒤 다시 던짐)뿐이다.


### 9.5 SB-d 묶음 끝 검토 (2026-11-05, 새 에이전트 하나, 정적 검토)

- 결과: 컴파일 오류 없음(서명·호출자·걷은 멤버·using·out 변수 범위·정적 초기화 순서·switch 식 모두 확인), 높음 0·중간 0·낮음 3. 설계 §4 동작 같음 대조 통과(`ReadTimings` 문구·순서 글자까지, 신호 입구 규칙, `AllowsAnchor` 집합, 세 공격 길의 묶음 다섯 칸, 환경 갈래 순서, 회전 검사 문구). 새 `.meta` 둘 정상(GUID 유일).
- 판정: C4-08·C2-04·C1-05 모두 **다 풀림**(평가의 실패 시나리오 기준). C4-08은 묶음을 만드는 곳이 `SinksFor` 하나라 길마다 다르게 빠지는 일이 없고, C2-04는 번호별 갈래·콜백·하드코딩이 표 둘로 모였으며 빠뜨리면 DB 생성·전투 연결·컴파일에서 걸리고, C1-05는 ViewSystem에서 분류 목록을 읽는 곳이 `CombatEnvironmentFacts` 하나다.
- 낮음
  - L1(권장, 사용자 결정 대기): 일반 생성자 `CombatCueSinks(...)`에 선택 인자가 남아, 새 공격 창구를 생성자 칸·`CombatCueComposer.Add`에만 넣고 `ForAttack`을 고치지 않으면 컴파일이 막지 않는다(세 길이 함께 빠짐). 권장: `ForAttack` 문서 주석에 "생성자에 공격용 칸을 더하면 이 입구에 필수 인자로 더함" 한 줄. 일반 생성자의 선택 인자 걷기는 C5-08(피격·사망·포효 창구 사슬) 때.
  - L2(권장, 사용자 결정 대기): `CombatClipTimings.FindListError`가 오류가 없어도 문구(보간·`FunctionNameOf`)를 먼저 만든다. DB 생성 때 클립마다 두 번(읽기 + 생성자). 권장: 실패 갈래 안에서 만듦.
  - L3(반영, 문서만): 설계 §5에 문구 변경 둘(`TraitActivationVisual:159`, `CombatCueAnchors.Set`)이 빠짐 → §5에 더함.
- 검토가 바로잡은 것: 설계가 든 `VisitShown` 재진입 위험은 HEAD에서도 닿지 않았다(설계 §13 정정).
- 범위 밖(보고만): DB `StageEnvironmentVisualDataSO.ValidateBindings`(호출자 0, 회전 ↔ 분류 대조의 LINQ 사본), `FieldTrailPresenter:92`의 `is TraitTrailFact`(흔적 형식 판정, 분류 정책 아님), 에디터 `CombatStageSoVisuals:63`(`IsKnownToPlayer` 사본, 다른 어셈블리).

### 9.6 SB-d 문서·공유 (2026-11-05)

- Td.1 폴더 배치 안내: 머리 갱신 줄, `Presentation/Pieces/` 줄에 `CombatClipEventSignals`·`CombatCueSinks`, §2 규칙 줄 둘(클립 이벤트 번호마다 다른 지식, 공격 조립이 받는 창구)과 환경 판정 줄 넓힘.
- Td.2 설계 §13 구현 메모, §5 문구 변경 둘(L3), §10 공유 정정.
- Td.3 공유: 현강님 메모 [§25](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md)(`CombatEnvironmentText` 갈래 넷, 미전달). 호진님·민형님 없음(호진님 줄 그대로, Factory 무변경). 경계 커밋 줄은 SB-0에서 GitHub 파일 이력으로 실제 작성자를 확인해(§9.1) 후속 계획 §5.3에 더할 줄이 없다.
- Td.5 평가 아티팩트 8차: C4-08·C2-04·C1-05 해결, 요청 7기준 76.1 → 79.3, 12기준 82.5 → 84.4, 중간 해결 18/29. 7차의 패치 칸을 커밋 `fedd5627`로, 남은 목록에서 C2-04를 뺐다.
- Td.6 후속 계획 §6.6 표·§6.7 점수 줄, CLAUDE.md 현재 상태.
- 바뀐 파일(커밋 묶음, 사용자): 새 `.cs` 둘 + `.meta`(DV `CombatPresentation/CombatClipEventIds`, V `Presentation/Pieces/CombatClipEventSignals`), 고친 `.cs` 스물다섯(§7 표 그대로), 문서(설계·계획·후속 계획·폴더 안내·현강님 메모·CLAUDE.md).

### 9.7 커밋·push (2026-11-05)

- 사용자 "일단 지금 작업 내용 정리해서 문서화해주고 그 뒤에 푸쉬해줘" → 세션 인계 [2026-11-05-combat-view-structure-bundles-session-handoff](../handoffs/2026-11-05-combat-view-structure-bundles-session-handoff.md)를 쓰고, §9.6 묶음을 한 커밋으로 넣어 `feature/Yonggeun/Refector-Stage`에 push했다.
- 같은 커밋에 후속 계획 §6.5(C2-04 질문 필요 없어짐)·§6.8(전달 목록 둘 더함)을 고친 것이 들어갔다.
