# 전투 View 작은 묶음 셋 설계 (SB1~SB3: 큐 창구 묶음·클립 이벤트 표·환경 판정)

- 날짜: 2026-11-05
- 상태: **구현 완료(11-05, SB-a~SB-d, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기 — 구현 메모 §13).** 결정(11-05 사용자 "권장으로 진행하고 Q-SB4만 Phase가 모두 종료 후로"): Q-SB1~Q-SB3 A, Q-SB4는 SB-a~SB-c를 이어서 하고 Unity 확인은 모든 Phase가 끝난 뒤 한 번에 한다(확인 목록은 Phase별로 나눠 둔다). P1~P11 그대로. 독립 검토 반영(§12).
- 요청: 사용자 "권장안대로 진행해". [평가 후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §6.6 구조 단계의 둘째 묶음이다. 직전 묶음은 [조정자 나누기](2026-11-05-combat-view-coordinator-split-design.md)다.
- 대상 문제([검증된 문제 목록](2026-11-04-combat-view-evaluation-issues.md)):
  - C4-08 [중간, 독립성] 2단계: 큐 창구들이 선택 인자 사슬로 처리기 → 공격 조립 입구 셋 → `CombatCueSinks`를 뚫고 내려간다. 1단계(3차)는 입구 셋의 기본값만 지웠다.
  - C2-04 [중간, SOLID]: 클립 이벤트 종류가 enum 넷과 대응 switch 여러 곳에 흩어져, 이벤트 하나를 더하려면 6~8곳을 고친다.
  - C1-05 [중간, SSOT] 2단계: 환경 원본의 분류 소속·형식 예외·"살아 있고 칸을 점유한 장애물" 판정 사본이 남았다. 1단계(3차)는 보임 판정 `CombatEnvironmentFacts.IsShown`만 모았다.
- 범위: `ViewSystem/Combat/`의 공격 조립·클립 조각·환경 표시와 입력, View DB 입력 계층 `DataBaseSystem/Data/View/CombatPresentation/`의 클립 이벤트 부분. 골격·Core·Framework·GlobalSystem·Factory는 고치지 않는다.
- 기준 판: HEAD `fedd5627`. 줄 번호는 이 판 기준이다.
- 규칙: View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. `Assets/Tests`에서 바꿀 형식을 쓰는 곳은 `StructureHitPresentationTests`의 `CombatPresentationCueRules.FindError`·`Require`뿐이다. 이 둘의 서명은 바뀌지 않아 고칠 시험이 없다(2026-11-05 grep, 검토 확인).

## 1. 지금 구조 (확인 사실)

### 1.1 C4-08 큐 창구 사슬

- 공격 조립 입구는 셋이다.
  - 근접 클립 `CombatClipAttack.ComposeMelee`(51-53): 인자 `sound`·`effects`·`camera`·`actorBody`·`lead`
  - 원거리 클립 `ComposeRanged`(82-85): 위에 `projectileSounds`를 더함
  - 코드 모션 `CombatCodeMotionAttack.Compose`(47-49): `sound`·`effects`·`camera`·`actorBody`·`projectileSounds`·`lead`
- 세 입구는 받은 창구를 그대로 `CombatCueSinks`로 다시 묶는다(`CombatClipAttack.AddCues` 127-133, `CombatCodeMotionAttack` 69-70). 소리 사건은 늘 `shape.Source`다.
- 호출은 공격 시작 `CombatAttackStart` 셋이다(99-102 코드 모션, 131-133 원거리, 134-135 근접). 창구 셋(`_sound`·`_effects`·`_camera`)은 이 클래스가 생성자에서 필수로 받아 늘 갖고 있다(64-72).
- 근접 클립은 투사체 소리를 넘기지 않는다(`projectileSounds: null`, 65). 원거리 클립만 탄별 시점(`projectileTimings`)을 조립 안에서 만들어 묶음에 넣는다(95-99). 탄별 시점을 읽는 곳은 `CombatProjectileCueComposer.Add` 하나다(기준 시점 표와 시점 목록을 함께 받음).
- 묶음 생성자는 필수 넷(`effects`·`camera`·`sound`·`soundEvent`)과 선택 다섯(`flashUnit`·`canFlash`·`projectileSounds`·`actionGroup`·`projectileTimings`)을 받는다. `projectileTimings`를 넘기는 곳은 원거리 클립 하나다.
- 실패 시나리오(평가): 새 창구(예: 화면 깜빡임)를 한 입구에만 넘기고 다른 입구에서 빠뜨리면, 그 입구로 도는 공격의 큐만 조용히 사라진다. 3차가 기본값을 지워 "인자를 빠뜨림"은 컴파일에서 걸리지만, 창구를 더할 때 고칠 곳은 여전히 묶음 생성자·입구 셋·`AddCues`·공격 시작 호출 셋이다.

### 1.2 C2-04 클립 이벤트 종류

이벤트 하나("두 번째 중간 이벤트")가 아래에 따로 있다.

| 층 | 자리 | 지금 |
|---|---|---|
| DB | `CombatClipEventId`(`CombatClipTimings.cs` 8-13) | `Event = 1`·`Event2 = 2`·`Event3 = 3` |
| DB | `CombatAnimationEvents.ClipEvent2`·`ClipEvent3`(40·43) | 애니메이션 창에서 고르는 이름표 |
| DB | `CombatClipEvents.ReadTimings`(26-71) | 함수 이름마다 갈래·지역 변수 둘씩·모르는 이름 문구 |
| DB | `CombatClipTimings` 생성자(37-53)·`FindTimeError`(79-96) | internal 생성자 하나와 검사 하나가 `eventTime2`·`eventTime3` 선택 인자를 받음. 번호마다 검사 한 줄 |
| DB | `CombatClipTimings.TryGetEventTime`(64-67)·`CombatClipShot`(21-22) | `is not (Event or Event2 or Event3)` 두 벌 |
| DB | `CombatPresentationCueRules.AllowsAnchor`(83-97) | 원거리 갈래(86-87)에 `Cue2 or Cue3` 나열 |
| DB | `CombatPresentationCueRules.AnchorOf`(226-232)·`UsesAdditionalEvents`(235-236)·공격 클립 검사(216-217)·`FindEventError`(238-244) | 번호 → 큐 시점 switch, 선택 번호 둘을 손으로 나열 |
| DB | `CombatCueAnchor`(`Cue2 = 6`·`Cue3 = 7`) | 큐 시점 값 |
| View | `CombatClipEventKind`(`CombatClipEventCursor.cs` 7-23)·`KindOf`(75-81) | 커서 종류에 `Event2`·`Event3`, 번호 → 종류 switch |
| View | `CombatClipPlayback`(19-23 필드, 34-45 생성자, 76-91 `Emit`) | 이벤트마다 콜백 필드·생성자 인자·switch 갈래 |
| View | `CombatClipPiece.Begin`(33-43) | `Event2`·`Event3`을 손으로 확인해 신호 입구를 만듦(번호 → 신호 사본) |
| View | `CombatClipRangedAttack.SignalOf`(44-50)·`ConnectEventCues`(34-41) | 번호 → 신호 switch(원본), 큐 시점은 `CombatPresentationCueRules.AnchorOf`(유일한 바깥 호출자) |
| View | `CombatPieceSignal`(`ClipEvent2 = 6`·`ClipEvent3 = 7`) | 실행 신호 값 |
| View | `CombatCueAnchors.KindOf`(94-100)·`Set` 문구(48) | `Cue or Cue2 or Cue3 or Hit or End` 나열, 문구 "Cue·Cue2·Cue3·Hit·End" |

- 글만 고치면 되는 자리(이벤트를 더할 때 문구가 낡음): `CombatPresentationCueRules` 요약(59), `TraitActivationVisual.cs:159` 예외 문구 "Cue2·Cue3", 툴팁 `CombatEventCues.cs:134`·`CombatAttackPresentationSO.cs:24`·`CombatUnitAnimationSO.cs:15`.
- 호출자가 없는 입구 넷(Assets/Tests 포함 grep, 검토 확인): `CombatClipEvents.Read`(17-23), `CombatClipData(clip, start, event, end, companions)`(20-24), `CombatClipData.FindTimeError`(60-61), `CombatClipEventCursor(start, event, end)`(40-43). `Tool/Editor/CharacterClipCopier.cs:16` 문서 주석이 앞의 셋 중 둘을 이름으로 가리킨다.
- 실패 시나리오(평가): `Event4`를 더하다 `CombatClipPlayback`·`CombatClipPiece`의 콜백 연결 한 곳을 빠뜨리면, 그 신호를 기다리는 발사·큐 항목이 시작하지 않은 채 끝난 것으로 처리돼(`SkipUnsignalled`) 탄·소리가 조용히 빠진다.
- 신호 입구(`CombatPieceContext.SignalCallback`)는 항목·신호마다 한 번만 받는다. 받은 신호만 "낼 신호"로 적히고, 조각이 끝날 때 내지 않은 신호를 기다리던 항목은 건너뛴다. 그래서 있는 이벤트의 신호만 받는 지금 규칙을 지켜야 한다.
- 층 규칙: DB 입력 계층과 View는 같은 어셈블리(`ProjectL.Systems`)다. DB가 View 형식(`CombatPieceSignal`)을 쓰지 않는 것은 컴파일 제약이 아니라 층 규칙이다.

### 1.3 C1-05 환경 판정 사본

1단계 뒤 보임은 `CombatEnvironmentFacts.IsShown` 하나다. 장애물 프로필 찾기도 `CombatEnvironmentFacts.TryGetObstacle`(36-57) 하나다. 남은 사본은 아래와 같다.

| 판정 | 자리 |
|---|---|
| Object 분류 소속 | `CombatEnvironmentText` 68(목록 훑기), `CombatHazardLookup` 38(목록 훑기), `ObjectViewSet` 40-46(Render마다 HashSet) |
| Terrain 분류 소속 | `CombatEnvironmentText` 62(목록 훑기), `TerrainViewSet` 202-206·217(HashSet) |
| 분류는 Object인데 원본 형식이 다르면 예외 | `CombatEnvironmentText` 70-71("Object 분류와 Buffer 원본 형식이 다릅니다."), `CombatHazardLookup` 39-40(같은 문구), `ObjectViewSet` 48-49("[ObjectViewSet] Object 분류와 원본 형식이 다릅니다: {id}.") |
| 살아 있고 그 칸을 점유한 장애물 | `CombatEnvironmentText` 79-81, `CombatHazardLookup` 49-50(둘 다 `Health.IsDead`·`OccupiedCells` + `TryGetObstacle`) |
| 회전 Source Id가 분류에 있는지 | `FieldObjectPresenter.ValidateObjectRotations` 102-108, `FieldTerrainPresenter.ValidateTerrainRotations` 211-217(Render마다 = 결과 끝 Refresh마다) |
| 먼지 연막 흔적 | `CombatEnvironmentText` 54(`TraitTrailFact && Key == DustScreen`), `CombatDustScreenHoverCells` 33(같은 식 + 보임) |

- 실패 시나리오(평가): 분류 정책이 바뀌어 한 곳만 고치면, 그리지 않은 원본이 위험 범위 호버나 환경 설명창에 나온다.
- 호출 빈도(검토 확인): `ObjectViewSet`·`TerrainViewSet`의 Render는 설치 때와 결과 끝 Refresh 때만 돈다. 매 프레임·호버가 아니다. 사건 순간에 도는 `TryReplace`·`HideActivated`는 분류를 보지 않는다.
- 지금 `foreach (long id in bindings.…)`는 `IReadOnlyList` 열거자를 박싱한다(Render·회전 검사마다 작은 할당).

### 1.4 다른 작업자 줄 (blame, `fedd5627`)

- C4-08 파일 넷(`CombatClipAttack`·`CombatCodeMotionAttack`·`CombatCueComposer`·`CombatAttackStart`)은 모두 우리 줄이다. `CombatClipAttack`의 일부(54-60·86-93 등)는 경계 커밋 `^8ed3f8cb`(Droute 표시)라 우리 줄로 본다.
- C2-04: `CombatClipTimings`·`CombatClipShot`·`CombatCueAnchor`·`CombatClipRangedAttack`·`CombatCueAnchors`·`TraitActivationVisual:158-159`·`CharacterClipCopier:16`은 우리 줄이다. `CombatPresentationCueRules`의 바꿀 줄(83-97·216-244)도 우리 줄이다(호진님 CCRC_Hojin 13줄은 42-50·62·91-94·110의 큐 칸 값·자리 규칙이라 겹치지 않음). 클립 커서·재생·조각의 다른 줄은 경계 커밋 `^83fb2767`(Leehojin 표시), `CombatClipEvents`·`CombatAnimationEvents`·`CombatClipData`는 경계 커밋 `^ad7e6b28`(Twinsgm 표시), `CombatPieceStart`는 경계 커밋 `^4782921e`(GwonCheonmyeong 표시)다. 실제 작성자는 알 수 없다(Q-N11). 프로젝트 기록상 클립 체계(AC-1~AC-6)는 용근 작업이다.
- C1-05: `CombatEnvironmentFacts`·`ObjectViewSet`·`CombatHazardLookup`·`FieldTerrainPresenter`(203-219)의 다른 줄은 경계 커밋 `^83fb2767`, `TerrainViewSet`·`FieldObjectPresenter`(95-110)는 경계 커밋 `^4782921e`다. 10-20 공유 문서는 `CombatEnvironmentFacts`·`CombatHazardLookup`을 용근 새 파일로 적었다. `CombatEnvironmentText`에서 바뀌는 줄(54·62-72·79-81)도 경계 커밋 `^83fb2767`이다. 호진님 실제 줄(CCRC_Hojin, 49-51·78 등)은 바뀌지 않는다. `CombatDustScreenHoverCells`는 우리 줄이다.
- C1-05 보충(11-05 GitHub 파일 이력으로 확인, 얕은 clone의 blame 대신): `ObjectViewSet`·`TerrainViewSet`은 용근님 커밋으로 처음 생겼고(`b5b13961` Object, `7ec2b981` Terrain, 둘 다 10-12) 분류 HashSet `_classified`도 그때부터 있다. 그 뒤 두 파일을 고친 다른 작업자 커밋은 현강님 `63a1c23e`(10-17, Hover UI) 하나다. 그 커밋이 바꾼 줄은 종류 해석(지금 `ObjectViewSet:52`·`TerrainViewSet:144·218`의 `EnvironmentVisualKinds`)이라 SB3가 고치는 줄과 겹치지 않는다. 그래서 두 파일은 경계 커밋 표에서 빼고, Q-SB2 A에 공유 메모는 없다. blame에 보인 Leehojin·GwonCheonmyeong은 경계 커밋 표시일 뿐이다.

## 2. 목표와 비목표

- 목표: 세 문제가 평가에 적힌 실패 시나리오와 함께 "다 풀림"이 되게 한다. 동작(화면·소리·시점·예외가 나는 조건과 문구)은 같다.
- 비목표:
  - 공격 밖 처리기(피격·사망 `CombatCharacterEventHandler.ComposeHit`·`ComposeDeath`, 포효 조립)의 `= null` 창구 사슬은 C5-08(낮음)이라 이번에 하지 않는다. 이번의 묶음 방식을 그대로 쓸 수 있다.
  - 큐 시점 값 `CombatCueAnchor`·실행 신호 값 `CombatPieceSignal`·DB 번호 `CombatClipEventId`의 값 자체는 그대로다. 직렬화된 에셋(`CombatClipShotEntry.eventId`, 큐의 시점 6·7)과 정수 값이 같아야 한다.
  - 애니메이션 이름표 `CombatAnimationEvents.ClipEventN`은 Unity가 함수 이름으로 고르므로 메서드로 남는다.
  - 툴팁의 번호 나열(디자이너에게 보이는 안내)은 그대로 둔다.
  - 일반 목록 도우미 `ContainsCell`(토네이도·기믹 칸)은 규칙이 아니라 그대로 둔다.

## 3. 설계

### SB1 공격 조립은 창구 묶음 하나를 받는다 (C4-08)

- 묶음에 공격용 생성 입구를 하나 둔다. 인자는 모두 필수다.
  - `CombatCueSinks.ForAttack(CombatEffectWindow effects, CombatCameraWindow camera, CombatEventAudio sound, CombatEvent soundEvent, CombatProjectileSounds projectileSounds)`
  - 공격용 묶음에 새 창구를 더할 때는 이 입구에 인자를 더한다. 그러면 공격 시작의 호출이 컴파일에서 걸린다(검토 M1).
  - 소리 창구가 null이면 `ArgumentNullException`이다. 지금 입구 셋의 `sound == null` 검사를 옮긴 것이다(P1). 투사체 소리는 null이어도 된다(근접 클립·코드 모션 근접).
- 공격 시작(`CombatAttackStart`)이 공격 하나마다 이 입구로 묶음을 한 번 만든다.
  - 코드 모션·원거리 클립: `projectileSounds = CombatProjectileSounds.From(settings)`
  - 근접 클립: `projectileSounds = null`(지금 동작 그대로 — 근접 클립에는 투사체 소리를 넣지 않음)
- 세 입구는 `sound`·`effects`·`camera`·`projectileSounds` 대신 `CombatCueSinks sinks` 하나를 받는다. 자리(`actorBody`)와 앞 항목(`lead`)은 창구가 아니라 공격마다의 값이라 지금처럼 따로 받는다(기본값 없음).
  - `ComposeMelee(shape, clip, cues, facing, release, sinks, actorBody, lead)`
  - `ComposeRanged(shape, clip, cues, facing, fire, release, sinks, actorBody, lead)`
  - `CombatCodeMotionAttack.Compose(shape, cues, tryBeginMotion, release, sinks, actorBody, lead)`
- 원거리 클립의 탄별 시점은 창구가 아니라 그 조립 하나의 시점 사실이다. 그래서 묶음에서 빼고 기준 시점 표(`CombatCueAnchors`)에 둔다(검토 M1).
  - `CombatCueAnchors.SetProjectileTimings(timings)`·`ProjectileTimings`(없으면 null = 코드 모션의 Cue·Hit을 씀, 지금 묶음 칸과 같은 뜻)
  - `CombatProjectileCueComposer.Add`는 시점 목록을 기준 시점 표에서 읽는다.
  - 묶음의 `ProjectileTimings` 속성과 생성자 인자를 걷는다. 묶음은 불변이고 사본 입구가 필요 없다.
- 결과: 공격용 창구를 하나 더할 때 고칠 곳은 묶음(칸·`ForAttack` 인자)과 공격 시작의 도우미 한 줄이다. 입구 셋·`AddCues`는 그대로다. 빠뜨리면 컴파일에서 걸린다.

### SB2 클립 이벤트 번호는 표 하나에서 (C2-04)

번호마다 한 줄인 표를 DB 쪽과 View 쪽에 하나씩 둔다. DB는 층 규칙상 View 형식(`CombatPieceSignal`)을 쓰지 않으므로 층마다 하나다. 두 표 모두 명시적 정적 생성자에서 모든 `CombatClipEventId` 값에 줄이 있는지 검사하고, 없으면 바로 예외다(조용히 빠지지 않음, P4).

- **DB 표 `CombatClipEventIds`**(새 파일 `DataBaseSystem/Data/View/CombatPresentation/CombatClipEventIds.cs`)
  - 줄 = (번호, 이름표 함수 이름 `nameof(CombatAnimationEvents.ClipEventN)`, 큐 시점 `CombatCueAnchor`). 지금 줄은 셋이다: (Event, ClipEvent, Cue), (Event2, ClipEvent2, Cue2), (Event3, ClipEvent3, Cue3).
  - **첫 줄 = 기준 이벤트(Event, 필수), 나머지 = 선택**이다(검토 L3). 필수 여부 칸은 두지 않는다. 기준 이벤트의 검사 문구가 이름 기반이 아니고("이벤트 타이밍…"), `CombatClipTimings.EventTime`도 첫 줄을 기준 이벤트로 보기 때문이다.
  - 입구: `All`(번호 순), `IsDefined(id)`, `FunctionNameOf(id)`, `AnchorOf(id)`, `TryFindByFunction(name, out id)`, `IsEventAnchor(anchor)`. 반복은 인덱스 루프로 할당하지 않는다.
- **DB 사용처**
  - `CombatClipEvents.ReadTimings`: 시작·끝은 지금처럼, 중간 이벤트는 표에서 함수 이름으로 찾아 번호별로 모은다.
  - `CombatClipTimings`: e2/e3 인자를 지우고 (시작, 끝, 번호·시각 목록) 생성자 하나와 그 검사 하나로 줄인다(검토 M3). 목록은 표 순서로 받는다(첫 항목 = 기준 이벤트).
  - `TryGetEventTime`·`CombatClipShot`: 정의된 번호 검사를 `CombatClipEventIds.IsDefined` 하나로.
  - `CombatPresentationCueRules`
    - `AllowsAnchor` 원거리 갈래(86-87): `Start·Cue·Hit·Contact·End`이거나 `CombatClipEventIds.IsEventAnchor(anchor)`. 근접 갈래는 Cue만 받는 지금 그대로다(검토 M2).
    - 공격 클립 검사(216-217)와 `UsesAdditionalEvents`: 표의 선택 줄을 돈다.
    - `AnchorOf`는 바깥 호출자가 원거리 조립 하나라, 그 호출을 표로 바꾸고 걷는다(검토 L4). 안의 `FindEventError`도 표를 부른다.
    - 요약(59)의 "Cue2·3"은 "선택 이벤트 시점(Cue2 이상)"으로 고친다.
  - 호출자 없는 입구 넷(`CombatClipEvents.Read`, `CombatClipData`의 세 시각 생성자·`FindTimeError`, 커서의 세 시각 생성자)을 걷는다(조정자 나누기 CS6과 같은 성격, P9). `CharacterClipCopier.cs:16` 문서 주석은 `ReadTimings`·`CombatClipTimings.FindTimeError`를 가리키게 고친다.
- **검사 순서·문구(지킬 원문, 검토 L2)**
  - `ReadTimings` 루프 안: 모르는 이름이면 그 자리에서 예외("모르는 함수 이름의 이벤트가 있습니다: '{function}'(쓸 수 있는 이름: ClipStart·ClipEvent·ClipEvent2·ClipEvent3·ClipEnd)"), 같은 이름 둘째면 "{function} 이벤트가 둘 이상입니다". 이름 목록은 시작 · 표 순서 · 끝으로 만든다.
  - 루프 뒤: 없음 검사 ClipStart → (표의 첫 줄) ClipEvent → ClipEnd 순("{function} 이벤트가 없습니다") → `FindTimeError` → 길이("끝 이벤트({end}초)가 클립 길이({length}초)보다 뒤입니다").
  - `FindTimeError`: 필수 셋 유한("이벤트 시각이 유한한 수가 아닙니다") → "시작 이벤트가 0초보다 앞섭니다" → "이벤트 타이밍이 시작 이벤트보다 앞섭니다" → "끝 이벤트가 이벤트 타이밍보다 앞섭니다" → 선택 줄마다 번호 순으로 "{name} 시각이 유한한 수가 아닙니다" / "{name}이 앞 이벤트보다 앞섭니다" / "{name}이 끝 이벤트보다 뒤입니다". 앞 이벤트 = 앞에 있는 마지막 이벤트, name = 표의 함수 이름.
  - 생성자의 검사 실패는 지금처럼 `ArgumentOutOfRangeException`(paramName `eventTime`).
  - 모든 예외의 감싸는 문구(`[CombatClipEvents] 클립 이벤트 셋이 잘못되었습니다: {이유}. 클립: {이름}`)는 그대로다.
- **View 표 `CombatClipEventSignals`**(새 파일 `ViewSystem/Combat/Presentation/Pieces/CombatClipEventSignals.cs`): 번호 → 실행 신호(`Event → ClipEvent`, `Event2 → ClipEvent2`, `Event3 → ClipEvent3`). 입구 `SignalOf(id)`. 지금 `CombatClipRangedAttack.SignalOf`를 옮긴다. 공격 시작(`CombatAttackStart`) 생성자가 이 표를 한 번 건드려, 줄이 빠졌으면 첫 클립 공격이 아니라 전투 연결 때 드러나게 한다(검토 L1, P10).
- **View 사용처**
  - 커서: 종류를 `Start`·`Event`·`End` 셋으로 줄이고(값 1·2·3 그대로), 중간 이벤트면 `Timings.Events`의 순번을 함께 낸다(`TryNext(time, out kind, out int eventIndex)`). `KindOf` 삭제. 순번이면 받는 쪽이 찾기 없이 배열을 바로 고른다(검토 L6).
  - 재생: 생성자를 `(lease, data, started, Action<int> eventReached, ended)`로, 콜백 필드 셋과 `Emit` 갈래를 하나로.
  - 조각: `Begin`이 `_playback.Begin()` **전에** `Timings.Events`를 돌며 순번마다 `SignalCallback(SignalOf(id))`를 받아 배열에 두고(0초 이벤트는 `Begin` 안에서 바로 나옴), 재생 콜백은 그 배열의 순번 칸을 부른다. Tick 할당 없음.
  - 원거리 조립: `SignalOf`·`AnchorOf`를 두 표에서 부른다.
  - `CombatCueAnchors`: `KindOf`는 `Cue2`·`Cue3` 나열 대신 `CombatClipEventIds.IsEventAnchor(anchor)`면 신호 시점. `Set` 문구(48)는 "뼈대 신호에 잇는 기준 시점은 Cue·선택 이벤트 시점·Hit·End입니다".
  - `TraitActivationVisual.cs:159` 문구 "Cue2·Cue3"은 "선택 이벤트 시점(Cue2 이상)"으로 고친다.
- 결과: `Event4`를 더할 때 고칠 곳은 값 넷(`CombatClipEventId`·`CombatCueAnchor`·`CombatPieceSignal`·이름표 메서드)과 표 줄 둘(DB·View)이다. 값 넷은 직렬화·Unity 함수 이름 때문에 꼭 필요하다. 표 줄을 빠뜨리면 DB 표는 부트의 DB 생성 때, View 표는 전투 연결 때 예외다. 재생·조각·커서·DB 읽기·검사는 고치지 않는다.

### SB3 환경 판정은 `CombatEnvironmentFacts` 하나 (C1-05 2단계)

- `CombatEnvironmentFacts`에 넣는 것(상태 없음, 인덱스 루프로 할당 없음):
  - `IsTerrain(bindings, sourceId)`·`IsObject(bindings, sourceId)`: 분류 소속. 회전 검사(Source Id)와 원본 검사(사실의 `Id`)가 같이 쓴다.
  - `TryGetObject(bindings, source, out ObjectSourceFact item)`: Object로 분류되지 않았으면 false, 분류됐는데 원본 형식이 다르면 예외 하나(문구는 P3).
  - `TryGetObstacleAt(bindings, obstacles, structure, cell, out profile)`: 살아 있고 그 칸을 점유한 장애물이면 프로필. 안에서 `TryGetObstacle`을 부른다.
  - `IsDustScreen(MovementTileFact source, out TraitTrailFact trail)`: 먼지 연막 흔적인지(형식 + 특성 키). 보임은 부르는 쪽이 `IsShown`으로 따로 본다(검토 L14, P11).
- 사용처
  - `CombatEnvironmentText`: 먼지 연막(54)·Terrain·Object 갈래(62-72)와 장애물 갈래(79-81)를 새 함수로. 바깥 `foreach` 머리(호진님 51·78줄)는 그대로다. 쓰지 않게 되는 `Contains(long)`는 걷는다(검토 L10).
  - `CombatHazardLookup`: Object 갈래(38-41)와 장애물 갈래(49-51)를 새 함수로. 쓰지 않게 되는 `Contains` 둘을 걷는다.
  - `CombatDustScreenHoverCells`: `IsVisibleDust`가 `IsDustScreen` + `IsShown`을 부른다.
  - `ObjectViewSet`·`TerrainViewSet`: Render마다 채우던 분류 HashSet(`_classified`)을 걷고 `TryGetObject`·`IsTerrain`을 부른다(P2, Q-SB2). `VisitShown`이 Render 안에서 다시 불릴 때 집합을 비워 버리던 재진입 위험도 함께 없어진다.
  - `FieldObjectPresenter.ValidateObjectRotations`·`FieldTerrainPresenter.ValidateTerrainRotations`: 분류 소속 반복을 `IsObject`·`IsTerrain`으로(문구 그대로). 박싱 열거자도 없어진다.

## 4. 지킬 것 (동작 같음)

| 자리 | 지킬 것 |
|---|---|
| 공격 큐 | 공격마다 같은 창구·같은 소리 사건(`shape.Source`)·근접 클립 투사체 소리 없음·원거리 클립 탄별 시점·코드 모션은 탄별 시점 없음(Cue·Hit). 큐를 붙이는 순서(소리 시점 → 프리셋 소리 → 투사체 소리 → 플래시 → 흔들림 → 이펙트)는 `CombatCueComposer` 그대로 |
| 클립 신호 | 있는 이벤트의 신호만 받는다(`SignalCallback`은 항목·신호마다 한 번, 순번은 시각 목록에서 하나씩이라 겹치지 않음). 같은 시각이면 시작 → `Events` 순 → 끝. 0초 이벤트는 `Begin`에서. 신호 입구는 `Begin` 전에 받는다 |
| 클립 DB 읽기 | §3 SB2 "검사 순서·문구"의 조건·순서·문구가 같다. 만든 시각 값이 같다 |
| 큐 검사 | `AllowsAnchor`·`UsesAdditionalEvents`·공격 클립 검사의 결과와 문구가 같다(선택 번호 순서 Event2 → Event3) |
| 환경 설명·위험 범위 | 같은 칸에서 같은 원본·같은 순서(보임 → 흔적 → 지형 → 오브젝트, 그다음 장애물)로 설명·범위를 낸다 |
| 환경 표시 | 같은 원본을 그리고 빠진 원본을 돌려준다. 분류 → 형식 예외 → 보임 순서. `ObjectViewSet`의 예외 → `Clear` → 다시 던짐 그대로. 회전 검사 문구 그대로 |

## 5. 바뀌는 것 (보이는 동작 없음)

- Object 분류·형식이 어긋날 때의 예외 문구가 하나로 같아진다(P3). 분류 계약을 어긴 Buffer에서만 닿는다.
- 문구 둘이 바뀐다(§3 SB2에 적은 것, 묶음 끝 검토 L3로 여기 더함): 특성 외형 덮어쓰기 검사(`TraitActivationVisual`, DB 생성 때 디자이너가 봄) "Cue2·Cue3" → "선택 이벤트 시점(Cue2 이상)", 기준 시점 표 `Set` 예외 "Cue·Cue2·Cue3·Hit·End" → "Cue·선택 이벤트 시점·Hit·End".
- 표에 줄이 없는 번호가 생기면 표를 만들 때 예외다. DB 생성 때는 `TypeInitializationException`이 진짜 문구를 안쪽 예외로 감싼다(검토 L5). 지금 데이터는 셋 다 줄이 있다.
- 걷는 것: 커서 종류 `Event2`·`Event3`, 커서 `KindOf`, 재생 콜백 필드 셋, `CombatPresentationCueRules.AnchorOf`, 묶음의 `ProjectileTimings`, `CombatClipTimings`의 e2/e3 인자, 호출자 없는 입구 넷, `ObjectViewSet`·`TerrainViewSet`의 `_classified`, 쓰지 않게 되는 `Contains` 셋.
- `ObjectViewSet`·`TerrainViewSet`·회전 검사가 분류 소속을 목록 훑기(인덱스 루프)로 본다. 결과는 같고 Render마다 하던 집합 채우기와 박싱 열거자가 없어진다.
- 바뀌는 파일
  - 새 파일 둘: `CombatClipEventIds.cs`(DB)·`CombatClipEventSignals.cs`(View). `.meta`는 Unity 기본 형식으로 만든다(P7).
  - SB1: `CombatAttackStart`·`CombatClipAttack`·`CombatCodeMotionAttack`·`CombatCueComposer`(묶음)·`CombatCueAnchors`·`CombatProjectileCueComposer`
  - SB2: DB `CombatClipTimings`·`CombatClipEvents`·`CombatClipShot`·`CombatClipData`·`CombatPresentationCueRules`·`TraitActivationVisual`(문구), View `CombatClipEventCursor`·`CombatClipPlayback`·`CombatClipPiece`·`CombatClipRangedAttack`·`CombatCueAnchors`·`CombatAttackStart`(표 건드리기), 에디터 `Tool/Editor/CharacterClipCopier`(주석)
  - SB3: `CombatEnvironmentFacts`·`CombatEnvironmentText`·`CombatHazardLookup`·`CombatDustScreenHoverCells`·`ObjectViewSet`·`TerrainViewSet`·`FieldObjectPresenter`·`FieldTerrainPresenter`

## 6. Phase(안)

SB-a~SB-d를 이어서 하고, Unity 확인은 모든 Phase가 끝난 뒤 한 번에 한다(Q-SB4 정함). 묶음 끝에 검토 에이전트 하나를 돌린다.

| Phase | 내용 | Unity 확인 |
|---|---|---|
| SB-a | SB1 창구 묶음 | 클립 근접·클립 원거리·코드 모션(클립 없는 원거리) 공격 하나씩에서 이펙트·흔들림·공격 소리·투사체 비행/착탄 소리가 전과 같다 |
| SB-b | SB2 클립 이벤트 표(DB + View) | 부트·전투 진입 때 DB 생성 예외 없음, 클립 근접 타격 시점, 2연발(Event2) 원거리 클립의 탄별 발사·Cue2 큐 |
| SB-c | SB3 환경 판정 | 지형·오브젝트·흔적 표시, 공개 전 지뢰는 칸·위험 범위에 안 나옴, 화약통 위험 범위, 환경 설명창, 먼지 연막 호버 |
| SB-d | 문서·공유·묶음 끝 검토·평가 아티팩트 8차 | — |

## 7. 7기준 점검 (설계안)

- **SSOT**: 클립 이벤트 번호의 이름표·큐 시점·기준/선택은 DB 표 하나, 실행 신호는 View 표 하나다. 환경의 분류·형식·장애물 칸·먼지 연막 판정은 `CombatEnvironmentFacts` 하나다.
- **SOLID**: OCP — 새 공격 창구는 묶음과 `ForAttack`, 새 클립 이벤트는 값과 표 줄만 더한다. SRP — 공격 조립은 뼈대·큐 시점, 창구 고르기는 공격 시작, 조립 하나의 시점 사실은 기준 시점 표가 맡는다.
- **FW 계약**: 서비스·조립 무변경. DB `GetData()`는 지금처럼 매번 새 값을 만든다.
- **독립성**: 창구 추가·클립 이벤트 추가·환경 분류 정책 변경이 각각 한 곳에서 끝난다.
- **불필요·우회**: 입구마다 창구를 다시 받아 다시 묶던 것, 이벤트마다 콜백 필드를 두던 것, Render마다 분류 집합을 채우던 것, 호출자 없는 입구 넷을 걷는다.
- **C#·Unity**: 표 검사는 형식 초기화 때 한 번이다. Tick 경로(커서·재생)는 할당하지 않는다. 조각 `Begin`의 신호 배열은 지금 콜백 셋을 만드는 것과 같은 때 한 번이다.
- **매 프레임**: Tick 분기 수가 같거나 준다.

예상 점수(같은 감점표, 다 풀린 중간만 뺀 추정): 셋이 다 풀리면 요청 7기준 76.1 → 79.3, 12기준 82.5 → 84.4다(중간 하나마다 +1.07 / +0.625, 검토 확인).

## 8. 정할 것

| 번호 | 정할 것 | 권장 |
|---|---|---|
| Q-SB1 | C2-04 범위: (A) DB 표 + `CombatClipTimings`·`ReadTimings`·검사 일반화 + View, (B) View만(평가 확인 기록의 1단계, 약 50줄) | **A.** B는 View의 조용한 빠짐은 없애지만 DB의 번호 나열(읽기·검사·생성자 인자·`AllowsAnchor`)이 남아 '일부'다. A는 약 200줄이고 부트의 DB 생성이 바로 확인한다 |
| Q-SB2 | 표시 쪽 분류 소속: (A) HashSet을 걷고 `CombatEnvironmentFacts` 목록 훑기, (B) HashSet은 두고 형식 검사만 공용 | **A.** 분류 목록과 원본은 수십 개라 훑기 비용이 작고(할당 없음), Render는 설치·결과 끝에만 돈다. B면 소속 판정이 두 벌로 남는다 |
| Q-SB3 | Object 형식 예외 문구를 하나로: (A) `[CombatEnvironmentFacts] Object 분류와 Buffer 원본 형식이 다릅니다: {id}.`, (B) 자리마다 그대로 | **A.** 분류 계약 오류에서만 나는 문구다 |
| Q-SB4 | 진행: (A) Phase마다 멈추고 Unity 확인, (B) SB-a~SB-c를 한 번에 | **A.** SB-b는 부트 DB 생성 경로를 지나므로 따로 확인하는 편이 원인을 가리기 쉽다. **정함(11-05 사용자): Unity 확인은 모든 Phase가 끝난 뒤 한 번에.** 확인 목록은 Phase별로 나눠 두어 문제가 나면 Phase를 가린다 |

## 9. 설계에서 정한 것 (확인 대상)

| 번호 | 정한 것 |
|---|---|
| P1 | 소리 창구 필수 검사는 `CombatCueSinks.ForAttack`이 한다(`ArgumentNullException`). 입구 셋은 `sinks` null만 본다 |
| P2 | `ObjectViewSet`·`TerrainViewSet`의 `_classified` 필드를 걷는다(Q-SB2 A일 때) |
| P3 | Object 형식 예외 문구는 Q-SB3 A 문구 하나다 |
| P4 | DB 표·View 표는 정적 클래스이고, 명시적 정적 생성자에서 모든 `CombatClipEventId` 값에 줄이 있는지(DB 표는 첫 줄이 `Event`인지도) 검사한다 |
| P5 | `CombatClipTimings`는 (시작, 끝, 번호·시각 목록) 생성자·검사 하나로 줄인다. `EventTime`은 첫 항목이다 |
| P6 | `CombatPresentationCueRules.AnchorOf`는 걷는다. `UsesAdditionalEvents`는 특성 외형 검사(`TraitActivationVisual`)가 부르므로 이름 그대로 둔다 |
| P7 | 새 파일 `.meta`는 Unity 기본 형식(MonoImporter, 새 GUID)으로 함께 만든다 |
| P8 | 탄별 시점은 기준 시점 표(`CombatCueAnchors.SetProjectileTimings`)에 두고 묶음에서 뺀다 |
| P9 | 호출자 없는 입구 넷(`CombatClipEvents.Read`, `CombatClipData` 세 시각 생성자·`FindTimeError`, 커서 세 시각 생성자)을 걷는다 |
| P10 | View 표는 `CombatAttackStart` 생성자가 한 번 건드려 전투 연결 때 검사가 돌게 한다 |
| P11 | 먼지 연막 흔적 판정(`IsDustScreen`)도 `CombatEnvironmentFacts`에 넣는다(C1-05 문구 밖이지만 같은 꼴, 우리 줄) |

## 10. 공유 (구현 뒤, 미전달 예정)

- 호진님(실제 커밋 줄): 바뀌는 줄이 없다(`CombatEnvironmentText`의 호진님 줄 49-51·78은 그대로). 메모는 쓰지 않는다.
- 경계 커밋 줄: 구현 때(SB-0) GitHub 파일 이력으로 모두 실제 작성자를 확인했다(계획 §9.1). `CombatEnvironmentText`의 지형·오브젝트 분류 갈래·형식 예외·장애물 생존·점유 줄이 현강님(`63a1c23e`) 줄이라 현강님 메모 §25에 적었다(미전달). 나머지는 모두 용근님 줄이다.
- 민형님: Factory 무변경이라 메모 없음.

## 11. 위험

- SB2가 부트의 DB 생성 경로(모든 유닛 외형의 클립 읽기)를 지난다. 문구·검사 순서가 조금이라도 다르면 부트 메시지가 달라진다. 계획에서 옛 판과 새 판의 갈래를 §3 "검사 순서·문구"대로 줄마다 대조한다.
- 표의 정적 생성자 예외는 형식 초기화 예외로 감싸여, 그 뒤 같은 형식을 쓰는 모든 호출이 같은 예외를 다시 낸다. 줄 누락은 개발 중에만 생기는 데이터 오류라 받아들인다.
- 클립 신호 입구는 항목·신호마다 한 번만 받는다. 순번 배열은 시각 목록의 순번마다 하나라 겹치지 않는다.

## 12. 독립 검토 기록 (2026-11-05)

새 에이전트 하나가 1판과 코드(HEAD `fedd5627`)를 대조했다. 높음은 없었다. 아래처럼 반영했다.

| 지적 | 등급 | 반영 |
|---|---|---|
| 묶음 사본 `WithProjectileTimings`가 새 창구 복사를 빠뜨리면 원거리 클립의 큐만 조용히 사라짐(같은 실패의 재현), 묶음 생성자의 선택 인자도 그대로 | 중간 | 탄별 시점을 기준 시점 표로 옮겨 사본을 없앰(P8), 공격용 생성 입구 `ForAttack`은 인자 모두 필수 |
| `AllowsAnchor` 원거리 갈래의 `Cue2 or Cue3` 나열을 OCP 셈에서 빠뜨림 | 중간 | 원거리 갈래를 표(`IsEventAnchor`)로 |
| `CombatClipTimings` e2/e3 인자를 남기면 호출자 0인 번호 나열이 남아 '일부'로 셀 수 있음, 호출자 없는 입구 넷 | 중간 | e2/e3 인자를 지우고 목록 판 하나(P5), 입구 넷 걷기(P9), "공개 생성자 둘" 서술 정정 |
| View 표 누락은 부트가 아니라 첫 클립 공격에서 드러남 | 낮음 | 공격 시작 생성자가 건드림(P10) |
| 지킬 검사 순서·문구를 설계에 명시 | 낮음 | §3 SB2 "검사 순서·문구" |
| 필수 여부 칸은 과설계(기준 이벤트 문구가 고정, `EventTime`이 첫 항목 가정) | 낮음 | 칸을 빼고 첫 줄 = 기준 이벤트 |
| 원거리 조립이 표를 부르면 `AnchorOf`의 바깥 호출자가 없어짐, P6 근거 틀림 | 낮음 | `AnchorOf` 걷음, P6 고침 |
| 명시적 정적 생성자, 형식 초기화 예외가 안쪽 예외로 감쌈 | 낮음 | P4·§5·§11 |
| 신호 배열은 `_playback.Begin()` 전에, 커서가 순번을 내면 찾기가 없음 | 낮음 | 순번 콜백(`Action<int>`) |
| 글만 고칠 자리(문구·툴팁) | 낮음 | §1.2에 적고, 문구는 고침, 툴팁은 그대로(§2) |
| "DB는 View를 참조할 수 없음"은 컴파일 제약이 아니라 층 규칙 | 낮음 | §1.2·SB2 서술 정정 |
| 회전 검사는 부트가 아니라 Render마다, `TerrainViewSet` 줄 번호 217 | 낮음 | §1.3 정정 |
| SB3 뒤 쓰이지 않는 `Contains` 셋, 새 함수는 인덱스 루프 | 낮음 | 걷기, 인덱스 루프 |
| Render는 설치·결과 끝에만, Q-SB2 근거의 "사건 순간" 틀림, HashSet 재진입 위험 | 낮음 | §1.3·Q-SB2·SB3 서술 |
| 공유 목록의 경계 줄(`FieldObjectPresenter`·`FieldTerrainPresenter`·`CombatEnvironmentText` 바뀌는 줄) | 낮음 | §1.4·§10 고침, 호진님 메모 없음 |
| 피격·사망·포효의 같은 사슬(C5-08)은 범위 밖 | 낮음 | §2 비목표에 적음 |
| 먼지 연막 흔적 판정 사본 | 낮음 | P11로 함께 |

검토자가 맞다고 확인한 것:

- SB1 §1 줄 번호·호출자(공격 조립 입구 셋의 호출자는 공격 시작 하나), 근접 클립 투사체 소리 null, 소리 사건 `shape.Source`, 묶음 생성의 부수효과 없음
- SB2 호출자(재생 생성자 = 조각 하나, 커서 = 재생 하나, `UsesAdditionalEvents` 바깥 = 특성 외형 하나), 신호 입구 한 번 규칙·`SkipUnsignalled`, 커서의 같은 시각 순서, 직렬화 값 그대로, 층 규칙 지킴
- SB3 각 호출자의 판정 순서, 지형·오브젝트 분류가 겹치지 않음(Core 보장), 예외 형식 모두 `InvalidOperationException`
- 점수 산식
- 시험: `StructureHitPresentationTests`만 큐 규칙 입구를 부르고 서명이 그대로

## 13. 구현 메모 (2026-11-05, SB-a~SB-d)

- 기록 원본은 [계획 §9](../plans/2026-11-05-combat-view-small-bundles-plan.md)다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경 없음, Factory 무변경. Unity 확인은 계획 §6 Ua~Uc를 모든 Phase 뒤 한 번에 한다(Q-SB4).
- 설계와 다르게 한 것
  - DB 번호 표에 기준 이벤트 이름 `Primary`를 더했다(읽기·목록 검사가 "첫 줄"을 이름으로 부르려고). 표 검사에 번호 오름차순·시작/끝 이름표 금지·정의된 큐 시점 줄을 더했다.
  - `IsDustScreen`은 null 인자에 예외를 내지 않고 false다(옛 형식 패턴과 같게).
  - `TryGetObstacleAt`은 생존·점유 칸을 본 뒤에야 분류 인자를 검사한다(옛 호출 자리의 순서를 지키려고).
  - 공격 시작 호출에서 `CombatProjectileSounds.From(settings)`가 `ActorBodyOf(...)`보다 먼저 계산된다. 둘 다 부작용 없는 읽기라 결과가 같다.
- 정정: §3 SB3·§12의 "`VisitShown`이 Render 안에서 다시 불릴 때 집합을 비워 버리던 재진입 위험"은 HEAD에서도 닿지 않았다(`FieldTerrainPresenter`가 `VisitShown`을 Render 밖에서 부름, 묶음 끝 검토). 걷은 것은 위험이 아니라 쓸모없는 집합이다.
- 묶음 끝 검토(새 에이전트 하나, 계획 §9.5): 컴파일 오류 없음, 높음·중간 0, 낮음 3. 세 문제 C4-08·C2-04·C1-05 모두 "다 풀림" → 평가 아티팩트 8차(요청 7기준 76.1 → 79.3, 12기준 82.5 → 84.4, 중간 해결 15 → 18).
  - L1: 일반 생성자 `CombatCueSinks(...)`에 선택 인자가 남아, 새 공격 창구를 생성자에만 넣고 `ForAttack`을 고치지 않으면 컴파일이 막지 않는다(세 길이 함께 빠짐 — 평가의 "길마다 다르게 빠짐"은 생기지 않음). 권장: `ForAttack` 문서에 "생성자에 공격용 칸을 더하면 여기 필수 인자로" 한 줄, 또는 C5-08 때 생성자 선택 인자 걷기. **사용자 결정 대기.**
  - L2: `CombatClipTimings.FindListError`가 오류가 없어도 문구를 먼저 만든다(DB 생성 때만, 클립마다 두 번). 권장: 실패 갈래 안에서 만듦. **사용자 결정 대기.**
  - L3: §5에 문구 변경 둘이 빠짐 → §5에 더함(문서만).
- 범위 밖으로 본 것(보고만): DB `StageEnvironmentVisualDataSO.ValidateBindings`(호출자 0, 회전 ↔ 분류 대조의 LINQ 사본), 에디터 `CombatStageSoVisuals:63`(`IsKnownToPlayer` 사본, 다른 어셈블리).
