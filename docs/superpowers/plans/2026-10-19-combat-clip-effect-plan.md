# 전투 클립 이펙트 조각 Task 계획

작성일: 2026-10-19. 설계: [clip-effect-design](../specs/2026-10-19-combat-clip-effect-design.md)(EF1~EF6).

- 결정: Q-E1~Q-E4 모두 A(사용자 "Q-E1~Q-E4 모두 A로 해. Task 계획 쓰고 EF-1부터 진행해.") — 이펙트는 그 순간 지점에 놓고 따라가지 않음, `effectPoint` 새 칸·이펙트 있으면 필수, 범위는 공격 클립 이벤트 타이밍만, 재생 취소·해제 때 회수.
- Phase가 끝나면 멈추고 보고한다(CLAUDE.md).
- 아래 `C/` = `ViewSystem/Combat/`, `DV/` = `DataBaseSystem/Data/View/CombatPresentation/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- **작업 트리 주의:** 클립 작업(AC-1~AC-6)이 커밋 전이고 이 계획이 고치는 파일과 겹친다(`DV/` 파일 다수는 아직 새 파일). 클립 묶음을 먼저 커밋하거나 함께 커밋한다.

## 0. 계획에서 정한 것 (보고, 설계 뜻은 그대로)

| # | 정한 것 | 이유 |
|---|---|---|
| P1 | `CombatClipAttackData` 생성자는 끝에 선택 인자 `effect = EffectType.Empty`, `effectDelay = 0f`를 더한다 | 기존 호출(시험 두 곳)을 고치지 않는다. SO는 늘 다섯 값을 모두 넘긴다 |
| P2 | 이펙트 종류 규칙 원본은 `CombatClipAttackData.IsKnownEffect`(정의된 `EffectType`이면 참, Empty 포함). 생성자(어기면 `ArgumentOutOfRangeException`)와 SO(어기면 `InvalidOperationException`)가 같이 부른다 | 선딜레이 규칙 `IsValidDelay`와 같은 방식. 기차·스테이지 맵 칸 검사(`System.Enum.IsDefined`)와 같은 판정 |
| P3 | 이펙트 선딜레이는 이펙트가 Empty여도 공격 칸이 있으면 검사한다 | 피격·소리 선딜레이와 같은 규칙(칸 값은 늘 올바름) |
| P4 | Render DB에 그 종류가 실제로 등록됐는지는 View DB 검사가 보지 않는다. 미등록은 첫 재생 때 `EffectSystem` 예외 → 표시 실패 규칙대로 정지 | View DB는 Render 표를 읽지 않는다(설계 E-F3). 기차 칸과 같은 수준 |
| P5 | 이펙트 지점 필수 규칙은 공격 표시 종류와 상관없이 "공격 칸에 이펙트가 있으면"이다. 원거리 유닛도 `muzzle`과 따로 `effectPoint`를 채운다(같은 Transform이어도 됨) | 칸 하나에 뜻 하나(포구 = 발사, 이펙트 지점 = 이펙트) |
| P6 | 이펙트는 지점의 위치와 회전을 모두 쓴다 | 베기·총구 불꽃은 방향이 있다 |
| P7 | `CombatClipAttack` 두 입구 끝에 선택 인자 `CombatEffectWindow effects = null`. 창구가 없거나 칸에 이펙트가 없으면 항목을 넣지 않는다 | 피격 조립의 카메라 인자 선례. 기존 조립 시험 무변경 |
| P8 | 대여 입구 `ICombatUnitClip.TryGetEffectPoint(out Vector3, out Quaternion)`. 조각은 false면 예외(외형 표 검사가 막아야 함) | 발사 조각의 포구 처리와 같다 |
| P9 | 창구는 Factory가 만들어 Presenter에 이름 있는 인자 `effects:`로 넘긴다. Presenter는 `CancelMovement`에서 `StopAll`, `Release`에서 `Release`를 부르고, 없으면(시험) 처리기에 null을 넘긴다 | 카메라 창구와 같은 길(설계 EF6). 정리는 Presenter의 기존 정리 묶음에 한 줄씩 |

## EF-1 — 값·지점·검사 (화면 변화 없음)

- **T1 값 `DV/CombatClipAttackData.cs`:** `Effect`(`EffectType`), `EffectDelay`, `HasEffect`(`Effect != Empty`), `IsKnownEffect`(P2), 생성자 선택 인자 둘(P1) + 검사. 문서: 선딜레이 셋.
- **T2 입력 `DV/CombatUnitAnimationSO.cs`:** 공격 칸 `attackEffect`(기본 Empty)·`effectDelay`(Min 0), 툴팁, `CreateData`가 선딜레이·종류를 검사하고 값에 넘김(P2·P3).
- **T3 지점 `DV/CombatAnimationEvents.cs`:** 선택 칸 `effectPoint`(Transform) + `EffectPoint`, 클래스 문서의 "작업 입력" 목록에 이펙트 지점.
- **T4 규칙 `DV/CombatUnitAnimationRules.cs`:** `ValidateUnit` — 공격 칸에 이펙트가 있으면 `events.EffectPoint` 필수(P5), 문서.
- **T5 시험 `T/CombatUnitAnimationSettingsTests.cs`(Unity 전용):** 이펙트·선딜레이가 값에 실림, 기본은 이펙트 없음, 잘못된 이펙트 선딜레이 거절(SO), 정의 안 된 종류 거절(SO·값 둘 다), 이펙트 있는 근접·원거리 공격 칸이 `effectPoint` 없으면 거절·있으면 통과(원거리는 포구만으로는 거절). 도우미 `Bundle`·`Visual`·`Unit`에 선택 인자.
- **확인(사용자, Unity):** 컴파일, `CombatUnitAnimationSettingsTests`, 기존 `CombatClipAttackCompositionTests`·`CombatUnitPresentationWindowTests`(값 생성자 호출 무변경 확인).

## EF-2 — 창구·조각·조립·연결 (기본값이면 화면 변화 없음)

- **T6 창구 `C/Display/CombatEffectWindow.cs`(새):** `IEffectSystemServices`·부모 Transform을 받음, Scope 하나(처음 낼 때), `Play(EffectType, Vector3 월드 위치, Quaternion 월드 회전)`(부모 지역 값으로 바꿈, Empty·정의 안 된 값 예외), `StopAll()`, `Release()`(뒤로 쓰면 예외).
- **T7 대여 입구:** `C/Display/ICombatUnitClip.cs` `TryGetEffectPoint`(P8), `C/Display/UnitClipChannel.cs`(준비 때 `events.EffectPoint`를 기억, `_muzzle`과 같은 방식), `C/Display/FieldUnitView.cs` 대여(`RequireAvailable` 뒤 위임). 시험 가짜 대여 셋(`CombatClipPlaybackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`의 `FakeClip`)에 멤버 추가.
- **T8 조각 `C/CombatEffectPiece.cs`(새):** 시간 없는 조각(설계 EF2), 시작에서 `context.Clip(attacker).TryGetEffectPoint` → `window.Play` → 끝.
- **T9 조립 `C/CombatClipAttack.cs`:** 두 입구 끝 선택 인자(P7), `BeginClip` 뒤 공통으로 `After(played, ClipEvent, EffectDelay)` 항목 하나(칸에 이펙트가 있고 창구가 있을 때만). 문서 항목 표에 4번.
- **T10 연결:** `C/CombatAttackEventHandler.cs` 생성자 선택 인자 창구 → 조립에 넘김, `C/CombatSceneEventPresenter.cs` 생성자 이름 있는 인자 `effects`·정리 두 곳(P9), `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` 창구 생성·전달 두 줄.
- **T11 시험:** 조립(이펙트 있으면 항목·시점·선딜레이, 없거나 창구 없으면 항목 없음 — `CombatClipAttackCompositionTests`에 사례 추가), 조각(지점 위치·회전을 창구로, 지점 없으면 예외), 창구(지역 변환·한 Scope·`StopAll`·`Release` 뒤 거절 — `FieldRailwayPresenterTests`의 가짜 Effects 방식). 대부분 Unity 전용.
- **확인(사용자, Unity):** 컴파일, 위 시험 + `CombatClipPlaybackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`·`CombatClipAttackSceneTests`·`UnitClipChannelTests`. 재생 확인은 공격 칸에 `TestFire`나 `Boom`을 임시로 넣고 프리팹에 `effectPoint`를 지정해 본다(확인 뒤 되돌림).

## EF-3 — 공유·기록

- **T12 공유:** 호진님 클립 메모(`handoffs/2026-10-19-unit-animation-clip-hojin-share.md`)에 공격 칸 새 칸 둘·`effectPoint` 필수 규칙, 민형님 Factory 공유 문서에 창구 생성·전달 두 줄 + 전투용 `EffectType` 등록 요청(프리팹 루트 ParticleSystem·`ParticleStopNotifier`, FW-012).
- **T13 기록:** 클립 설계 AC7 표 4번 줄을 이 설계로 연결, 이 계획 실행 기록, CLAUDE.md, 폴더 정리 대응표(새 파일 둘 `CombatEffectWindow`·`CombatEffectPiece`).

## 실행 기록

### EF-1 (2026-10-19, 코드 완료, Unity 확인 대기)

- **바꾼 파일(새 파일 없음):**
  - `DV/CombatClipAttackData.cs` — `Effect`·`EffectDelay`·`HasEffect`, 규칙 원본 `IsKnownEffect`·문구 `EffectRule`(P2), 생성자 선택 인자 둘(P1)과 검사.
  - `DV/CombatUnitAnimationSO.cs` — 공격 칸 `attackEffect`(기본 Empty)·`effectDelay`(Min 0)·툴팁, `CreateData`가 이펙트 선딜레이(P3)·종류(P2)를 검사하고 값에 넘김.
  - `DV/CombatAnimationEvents.cs` — 선택 칸 `effectPoint`·`EffectPoint`, 클래스 문서의 작업 입력 목록.
  - `DV/CombatUnitAnimationRules.cs` — `ValidateUnit`이 "공격 칸에 이펙트가 있으면 `effectPoint` 필수"(P5, 포구로 대신하지 않음).
  - `T/CombatUnitAnimationSettingsTests.cs` — 새 시험 6개(사례 12): 이펙트·선딜레이가 값에 실림, 기본은 이펙트 없음(SO·값), 잘못된 이펙트 선딜레이 거절(이펙트 없어도, 사례 3), 정의 안 된 종류 거절(SO·값), 이펙트 있고 지점 없음 거절(근접·직선·곡사, 원거리는 포구가 있어도), 지점 있음 통과(사례 3). 도우미 `Bundle`(이펙트·선딜레이)·`Visual`·`Unit`(`effectPoint`)에 선택 인자, 새 도우미 `EffectAttackBundle`. 기존 시험 본문은 무변경.
- **계획과 다른 점:** 없음.
- **검증:** MSBuild(`ProjectL.Tests.Framework.csproj`, Unity가 만든 csproj — 새 파일이 없어 extra.targets 불필요) 오류 0, 바꾼 파일의 경고 0, 산출 DLL에 새 멤버·새 시험 이름 확인. 새 시험은 `AnimationClip`·SO를 써서 Unity 전용이라 관리 실행은 하지 않음.
- **화면 변화 없음:** 모든 칸의 기본값이 Empty이고 아직 아무도 이펙트를 내지 않음(EF-2에서 연결).
- **Unity 확인(사용자):** 컴파일, `CombatUnitAnimationSettingsTests` 전체, 값 생성자 호출이 그대로인 `CombatClipAttackCompositionTests`·`CombatUnitPresentationWindowTests`.
- **커밋:** 바꾼 파일 다섯. `DV/` 네 파일은 아직 커밋 전인 클립 묶음의 새 파일이라 클립 묶음과 함께 커밋된다.
- **Unity 확인:** 컴파일·시험 문제 없음(2026-10-19, 사용자 "Unity 컴파일·시험 문제 없음 확인했어. EF-2 진행해.").

### EF-2 (2026-10-19, 코드 완료, Unity 확인 대기)

- **새 파일(+ `.meta`, GUID 32자리 확인):** `C/Display/CombatEffectWindow.cs`(창구, T6), `C/CombatEffectPiece.cs`(조각, T8), `T/CombatEffectWindowTests.cs`(T11).
- **바꾼 파일:**
  - 대여 입구(T7): `C/Display/ICombatUnitClip.cs` `TryGetEffectPoint`, `C/Display/UnitClipChannel.cs`(준비 때 `events.EffectPoint` 기억, `GetPositionAndRotation`), `C/Display/FieldUnitView.cs` 클립 대여 위임.
  - 조립(T9): `C/CombatClipAttack.cs` 두 입구 끝 선택 인자 `effects`, 공통 `AddEffect`(칸에 이펙트가 있고 창구가 있을 때만 `After(클립, ClipEvent, EffectDelay)`), 문서 항목 표 4번.
  - 연결(T10): `C/CombatAttackEventHandler.cs` 생성자 선택 인자 창구 → 조립, `C/CombatSceneEventPresenter.cs` 생성자 끝 이름 있는 인자 `effects`·필드, `CancelMovement`에서 `StopAll`·`Release`에서 `Release`(각각 `CleanupErrors.Try`), `Factory/.../CombatSceneDisplayFactory.cs` 창구 생성(`services.Effects`, `field.CellsRoot`)·전달 두 줄.
  - 시험 가짜 대여 셋(T7): `CombatPieceRunTests.FakeClip`(설정 가능한 `EffectPoint`), `CombatClipPlaybackTests`·`CombatUnitPresentationWindowTests`의 `FakeClip`(없음 반환).
- **새 시험(모두 Unity 전용, 가짜 이펙트 서비스는 기차 시험의 `FieldRailwayPresenterTests.Effects`를 그대로 씀):**
  - `CombatEffectWindowTests` 6개(사례 7): 놓는 자리(부모 지역 변환, 위치·회전), Scope 하나, `StopAll` 뒤 계속 씀, `Release` 뒤 거절·두 번 안전, Empty·정의 안 된 종류 거절, 생성자 null.
  - `CombatClipAttackCompositionTests` 새 6개: 이벤트 타이밍 + 선딜레이에 지점 위치·회전으로 한 번·실행을 붙잡지 않음, 원거리 입구도 같은 항목, 칸에 이펙트 없으면 항목 없음, 창구 없으면 항목 없음, 지점 없으면 시작 때 실패, 이펙트 전 취소면 안 냄. 도우미 `Data`·`Compose`·`ComposeRanged`에 선택 인자.
  - `UnitClipChannelTests` 새 2개: 대여가 지점 위치·회전을 읽음, 지점 없으면 false.
  - `CombatClipAttackSceneTests` 새 1개: 실제 유닛 표시·Presenter·순차 재생에서 근접 클립 공격의 이펙트가 지점에 놓이고(부모 = 창구 부모) 결과 재생 중단 때 회수됨. `Arrange`·`ActorPrefab`에 선택 인자.
- **계획과 다른 점:** 없음. 확인한 사실 하나 — Presenter `CancelMovement`는 결과 재생 취소·연결 끊기·표시 정리에서만 불리고 정상 완료에는 불리지 않는다(`OrderedCombatPresentation.Cancel`·`CombatResultPlayback.Abort`), 그래서 끝난 공격의 이펙트는 자연히 끝난다.
- **검증:** MSBuild(`ProjectL.Tests.Framework.csproj` + extra.targets로 새 시험 파일) 오류 0, 바꾼 파일의 새 경고 0(CS2002 둘은 Unity가 이미 csproj에 넣은 새 코드 파일을 targets가 한 번 더 넣어 생긴 중복 알림), 산출 DLL에 새 형식·새 시험 이름 확인. 관리 실행은 하지 않음(모두 Unity 전용).
- **원칙 자체 점검(7기준) 약 8.7:** 낮음 셋 — L1 "정의된 이펙트 종류" 판정이 값 규칙(`IsKnownEffect`)과 창구(`Play`)에 따로 있음(층이 달라 창구가 공격 칸 값 규칙을 부르지 않음, 폭발 Presenter도 같은 판정), L2 Presenter 생성자 인자 하나 더(카메라 평가 C13과 같은 종류), L3 전투 입장마다 Scope 하나 더 쌓임(G2, Framework 해제 API 대기).
- **화면 변화 없음(지금 에셋):** 모든 공격 칸의 이펙트가 Empty라 항목이 들어가지 않음.
- **Unity 확인(사용자):** 컴파일, `CombatEffectWindowTests`·`CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`·`UnitClipChannelTests` + 가짜를 고친 `CombatPieceRunTests`·`CombatClipPlaybackTests`·`CombatUnitPresentationWindowTests` + `CombatUnitAnimationSettingsTests`. 재생 확인(선택): 클립 묶음 공격 칸에 `Boom`이나 `TestFire`를 임시로 넣고 프리팹 이벤트 컴포넌트에 `effectPoint`를 지정 → 이벤트 타이밍에 그 자리에 이펙트, 확인 뒤 되돌림.
- **커밋:** 새 파일 셋 `.meta`와 함께. `CombatClipAttack.cs`·`ICombatUnitClip.cs`·`UnitClipChannel.cs`와 클립 시험 파일 다섯은 아직 커밋 전인 클립 묶음의 새 파일이라 클립 묶음과 함께 커밋된다.
- **Unity 확인:** 컴파일·시험 문제 없음(2026-10-19, 사용자 "Unity 컴파일·시험 문제 없음 확인했어. EF-3 진행해.").

### EF-3 (2026-10-19, 문서만, 코드 변경 없음)

- **T12 공유(둘 다 미전달):**
  - 호진님 [클립 메모](../handoffs/2026-10-19-unit-animation-clip-hojin-share.md) §8 새 절(공격 칸 `attackEffect`·`effectDelay`, 이벤트 컴포넌트 `effectPoint`, "이펙트 있으면 지점 필수" 검사, 미등록 종류는 첫 재생 때 실패, 재생 확인 방법) + §2 SO 줄·§4 검사 목록에 한 줄씩. 호진님 파일은 이번에 바뀌지 않았다고 적음.
  - 민형님 [Factory 공유 문서](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §19 새 절(Factory 두 줄, 창구 수명 주인 = 사건 Presenter, 이펙트 시스템 공개 API만 사용·Render 코드 무변경, Scope 하나·G2 남음, 필드 루트에 놓는 이유, **전투용 `EffectType` 등록 요청**(프리팹 루트 ParticleSystem·`ParticleStopNotifier`), 미등록 종류의 실패 시점).
- **T13 기록:** [클립 설계](../specs/2026-10-19-combat-unit-animation-clip-design.md) AC7 표 4번 줄을 "구현, 원본은 이펙트 설계"로 고침, [폴더 정리 계획](2026-10-19-combat-folder-restructure-plan.md) 대응표에 `CombatEffectPiece`(Pieces 줄)와 새 줄 `Presentation/Effects/`(`CombatEffectWindow`, 새 폴더 제안 — 폴더 정리 결정 때 확인), 이 기록, 설계 머리 상태, CLAUDE.md.
- **이펙트 작업(EF-1~EF-3) 끝.** 남은 것: 사용자 커밋(클립 묶음과 함께), 전투용 이펙트 등록(민형님), 실제 유닛 프리팹에 `effectPoint`·공격 칸 이펙트 지정(에셋 작업).
