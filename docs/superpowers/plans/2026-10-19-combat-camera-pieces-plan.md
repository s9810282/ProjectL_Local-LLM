# 전투 카메라 연출 조각 Task 계획 (K)

- 작성일: 2026-10-19. 상태: **K-1~K-3 코드 완료, Unity 문제 없음(K-1·K-2·K-3 모두 사용자 확인). 실행 기록 §6, 원칙 검사 [principles-evaluation](../specs/2026-10-19-combat-camera-principles-evaluation.md)(종합 8.0/10).**
- 설계: [카메라 연출 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md)(K1~K7). 결정(2026-10-19, 설계 §6.1): Q-K1·K2·K4·K5·K6 A, Q-K3 B.
- 조건: **Framework 쪽 수정 없음.** `CameraGroup`·`CameraEffectSystem`·두 카탈로그·`DelayService`·화면 전환 코드를 바꾸지 않는다. 공개 `ICameraEffectService.Dispatch`만 쓴다.
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 착수 조건과 공통 규칙

### 0.1 같은 파일의 커밋 전 작업 (착수 전 사용자 결정)

- 가방·아이템 작업(다른 세션, P-1~P-3)이 커밋 전이다. 그 작업은 `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`, `C/CombatSceneDisplay.cs`, `T/CombatSceneEventPresenterTests.cs`, 민형님 Factory 공유 문서를 고친 상태다. 또 사용자 결정으로 컴파일 확인을 건너뛰었다.
- K-1은 Factory 파일과 Factory 공유 문서에 닿는다. 그대로 시작하면 두 작업의 변경이 한 파일에 섞이고, MSBuild 기준선(§0.2)을 잡을 수 없다(어느 오류가 어느 작업 것인지 가를 수 없음).
- 선택지:
  - (a) 권장: 가방·아이템 묶음을 먼저 커밋한 뒤 그 HEAD에서 K-1을 시작한다.
  - (b) 가방 세션을 멈추고, 이 세션이 그 작업 트리 위에서 진행한다. 기준선은 "그 작업 트리 + K 이전"으로 잡고, 커밋은 두 묶음을 나눠서 한다.

### 0.2 Phase마다 지키는 것

- MSBuild 오류·새 경고 0. 새 `.cs`는 csproj에 없으므로 `CustomAfterMicrosoftCommonTargets`로 더해 빌드한다(빌드 확인 메모).
- 관리 실행 회귀 0. 착수 HEAD 산출물 대비 메서드 이름 단위로 비교한다(순번 `@n` 실행, NATIVE 판정 규칙은 빌드 확인 메모). 관리 실행 도구는 세션 스크래치라 다시 만든다.
- Phase마다 검토 에이전트 1개.
- Unity Test Runner 묶음 목록을 넘기고, 결과를 받은 뒤 다음 Phase로 간다.
- 새 `.cs`는 `.meta`(GUID 32자리)와 함께 만든다.

### 0.3 신호 이름과 클립 설계와의 공유

- 신호의 뜻은 [클립 설계](../specs/2026-10-19-combat-unit-animation-clip-design.md) AC6 신호 표가 원본이다. 카메라가 쓰는 신호는 **`CombatPieceSignal.Hit`(대상이 맞는 순간, Gate 해제)**이다.
  - 클립의 `Event`(이벤트 타이밍, 연출 기준 시점)와 다른 값이다(클립 설계 §8 E1). 근접 클립은 이벤트 타이밍 + 피격 선딜레이에 맞고, 원거리는 이벤트 타이밍에 발사해 접촉 때 맞기 때문이다.
  - K-2는 `Hit` 하나만 더한다. `Start`·`Event`는 클립 AC-3이 더한다.
- 신호 일반화 장치(맥락의 신호 입구, 실행의 신호 처리, 신호가 오지 않는 항목 규칙, 시작 안에서 온 신호의 시간 규칙)는 클립 AC-3과 같은 공통 경계다.
  - K-2 착수 때 AC 쪽이 이미 만들었는지 확인한다. 만들었으면 K-2는 `Hit` 값과 정면 공격 조각의 신호 발화(K-2.1·K-2.5)만 한다.

## 1. K-1 창구·조각·장면 채널 (동작 변화 없음)

목표: 카메라 창구, 두 조각, 조립의 장면 채널을 만들고 전투 표시에 연결한다. 쓰는 조립이 없으므로 화면은 바뀌지 않는다.

| Task | 내용 | 파일 |
|---|---|---|
| K-1.1 | **맵 카메라 사실 읽기.** `CombatMapCamera`(읽기 전용 구조체: `Name`, `BaseLens`, `ShakeChannel`)와 `CombatMapCameras.Read(Transform root, List<CombatMapCamera> into)`. 인스턴스 안의 `CinemachineVirtualCamera`를 비활성 포함해 모은다. 기준값 = `m_Lens.Orthographic ? OrthographicSize : FieldOfView`. 채널 = 같은 GameObject의 `CinemachineImpulseListener.m_ChannelMask`(없으면 0). 이름이 겹치면 예외(카탈로그 규칙과 같음) | 새 `C/Display/CombatMapCameras.cs` |
| K-1.2 | **창구 `CombatCameraWindow`.** 생성자 `(ICameraEffectService effects)`, null이면 카메라 없음과 같다.<br>• `AttachMap(IReadOnlyList<CombatMapCamera>)`·`DetachMap()`<br>• `CanZoom`·`TryBeginZoom(Action cancelled, out ICombatCameraZoom zoom)`<br>• `CanShake`·`Shake(CameraShakeData)`(채널만 창구가 채움, 값·검사는 카메라 효과가 원본)<br>대여 `ICombatCameraZoom`: `ZoomTo(ratio, seconds)`·`RestoreZoom()`. 대여 객체는 버전을 가져 반환·취소 뒤 쓰면 예외다(유닛 창구 대여와 같은 규칙). 줌 배율 조건(`ValidateZoomRatio`)만 설정 Data에 더한다(배율은 Framework에 없는 우리 개념).<br>Unity 컴포넌트는 읽지 않는다(사실은 K-1.1이 넘김). 그래서 관리 실행으로 시험할 수 있다 | 새 `C/Display/CombatCameraWindow.cs`, `C/Display/ICombatCameraZoom.cs` |
| K-1.3 | **프랍 Presenter 연결.** 생성자에 창구(null 허용)를 받는다. `Show`에서 세운 직후 `Read` → `AttachMap`, `Release`에서 없애기 직전 `DetachMap`. 같은 스테이지 재설치(인스턴스 유지)는 다시 읽지 않는다 | `C/Display/StageViewPropPresenter.cs` |
| K-1.4 | **조각 둘.**<br>• `CombatCameraShakePiece(window, CameraShakeData)`: `Begin`에서 `Shake`하고 바로 끝난다.<br>• `CombatCameraZoomPiece(ratio, seconds)`: `Begin`에서 `context.CameraZoom().ZoomTo`, 자기 시계로 `seconds` 뒤 끝난다(0이면 시작 안에서). `Cancel`은 아무것도 하지 않는다 | 새 `C/CombatCameraShakePiece.cs`, `C/CombatCameraZoomPiece.cs` |
| K-1.5 | **장면 채널.**<br>• `CombatComposition.LeaseCameraZoom()`·`LeasesCameraZoom`(두 번이면 예외)<br>• `CombatPieceRun`: 생성자에 카메라 창구, `TryBorrow`에서 유닛 대여 뒤 줌 대여(실패하면 빌린 것 모두 되돌리고 false), `RestoreLeases`에서 `RestoreZoom`, `CameraZoom()` 접근자(안 빌렸으면 예외). 줌 대여의 밖 취소는 유닛 대여와 같은 `OnLeaseCancelled`로 간다<br>• `CombatPieceContext.CameraZoom()`<br>• `CombatPieceRunPlayer` 생성자에 카메라 창구<br>• `CombatSceneEventPresenter` 생성자에 이름 있는 선택 인자 `camera`(null이면 카메라 없는 창구) | `C/CombatComposition.cs`, `C/CombatPieceRun.cs`, `C/CombatPieceContext.cs`, `C/CombatPieceRunPlayer.cs`, `C/CombatSceneEventPresenter.cs` |
| K-1.6 | **서비스·Factory.** `CombatDisplayServices`에 선택 인자 `ICameraEffectService cameraEffects`를 더하고, `From`이 `render.CoreGroup.CameraGroup.CameraEffectSystem`을 고른다. Factory가 창구를 하나 만들어 프랍 Presenter와 사건 Presenter에 넘긴다 | `C/CombatDisplayServices.cs`, `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` |
| K-1.7 | **시험.** 아래 표 | 새 `T/CombatCameraWindowTests.cs`, `T/CombatCameraPieceTests.cs`, `T/CombatMapCameraTests.cs`(처음 이름 `CombatMapCamerasTests.cs`, §6.1), 기존 `T/CombatPieceRunTests.cs`·`T/CombatPieceRunPlayerTests.cs`(생성자 인자) |
| K-1.8 | **공유.** 민형님 Factory 공유 문서에 절을 더한다(서비스 묶음 인자 하나, 창구 생성 두 줄) | `docs/superpowers/handoffs/2026-10-15-combat-scene-display-factory-framework-share.md` |

**K-1 시험**

| 시험 | 확인 | 실행 |
|---|---|---|
| 창구 | `ZoomTo`가 카메라마다 `From = 기준 × 지금 배율`, `To = 기준 × ratio`로 보냄. `RestoreZoom`은 배율 ≠ 1일 때만 0초 기준값을 보냄. 대여는 하나뿐. `DetachMap`이 대여 취소 콜백을 부름. 반환 뒤 대여 사용은 예외. 채널 0이면 `CanShake` false이고 `Shake`는 보내지 않음. 효과 서비스 null이면 `CanZoom`·`CanShake` 모두 false | 관리(가짜 효과 서비스가 받은 데이터 기록) |
| 조각 | 줌 조각이 정한 시간에 끝남, 0이면 시작 안에서 끝남, 흔들림 조각은 시작 안에서 끝남 | 관리 |
| 실행 장면 채널 | 줌을 못 빌리면 시작 안 함·유닛 대여 되돌림, 전체 종료·취소에서 `RestoreZoom`, 안 빌린 채 `CameraZoom()`이면 예외, 밖 취소면 사라짐 | 관리 |
| 맵 카메라 읽기 | 직교 버추얼 카메라의 기준값·이름, Listener 채널, Listener 없으면 0, 이름 중복이면 예외 | Unity(Cinemachine 컴포넌트) |

## 2. K-2 타격 신호 `Hit`

목표: 조각이 `End` 말고도 `Hit`(대상이 맞는 순간) 신호를 낼 수 있게 하고, 정면 공격 조각이 박치기 타격 순간(Gate 대상 모두 해제와 같은 순간)에 그 신호를 낸다. 쓰는 조립이 없으므로 화면은 바뀌지 않는다(§0.3: 클립 쪽이 먼저 만들었으면 K-2.5만).

| Task | 내용 | 파일 |
|---|---|---|
| K-2.1 | `CombatPieceSignal.Hit = 2`(클립 AC-3이 먼저 했으면 그쪽 값 배치를 따름) | `C/CombatPieceStart.cs` |
| K-2.2 | 맥락의 신호 입구 `SignalCallback(CombatPieceSignal)`. 항목·신호마다 한 번만 받고, 지난 세대·끝난 실행·끝난 항목의 늦은 호출은 무시한다(끝 입구와 같은 규칙). `End`는 기존 `EndCallback`이 맡는다 | `C/CombatPieceContext.cs` |
| K-2.3 | **실행의 신호 처리.** 지금 `EndItem` 안의 "그 신호를 기다리는 뒤 항목 시작"을 신호 종류를 받는 함수로 뽑는다. 시간 규칙은 BP2 그대로다: 신호로 바로 시작하는 항목은 그 프레임 시간 없음, 신호 뒤 지연은 신호 순간부터 셈, 실행 밖 콜백에서 온 신호의 남은 시간은 0, 시작 안에서 온 신호는 시작의 남은 시간을 이어 받음 | `C/CombatPieceRun.cs` |
| K-2.4 | **신호가 오지 않는 항목 규칙.** 어떤 항목이 끝나면 그 항목은 더 이상 신호를 낼 수 없다. 그래서 그 항목의 끝 외 신호를 기다리던, 아직 시작하지 않은 항목은 그 순간 "시작하지 않고 끝남"이 된다. 그 항목을 기다리던 뒤 사슬도 같다. 기다림 항목이면 완료 계산에 끝난 것으로 넣는다.<br>설계 BP2의 "실행 끝에 끝난 것으로 본다"를 구현 시점으로 구체화한 것이다. 실행 끝까지 미루면 남은 항목 수가 0이 되지 않아 실행이 끝나지 않는다. 조각·조립 설계 BP2에 이 보충을 적는다 | `C/CombatPieceRun.cs`, 조각·조립 설계 BP2 보충 |
| K-2.5 | **정면 공격 조각의 신호.** 시작 입구를 `Func<Action, Action, bool>`(끝, 타격)로 바꾼다. 공격 재생 `TryBeginFacingMotion`에 `Action hit` 인자를 더하고, 지금 `HitAll(source, version)`을 부른 직후 같은 순간에 부른다. 포효 처리기는 맥락의 신호 입구를 넘긴다(포효 조립에 쓰는 항목은 아직 없음) | `C/CombatFacingAttackPiece.cs`, `C/CombatAttackPlayback.cs`, `C/CombatRoarEventHandler.cs` |
| K-2.6 | **시험.** 신호 시점(바로 시작·지연·시작 안 신호·콜백 신호), 신호 미도착 규칙(사슬·기다림 계산·완료), 세대 무시, 정면 공격 조각이 타격 때 신호를 냄. 기존 조립 시험 전체를 회귀로 돈다 | `T/CombatPieceRunTests.cs`, `T/CombatAbilityCompositionTests.cs` 등 |

- 공통 경계 변경이라 Unity 묶음에 포효·소환·피격·사망 재생 시험을 모두 넣는다.

## 3. K-3 포효에 줌·흔들림 적용

목표: 설계 K6의 포효 조립. 화면에는 줌만 보인다. 흔들림은 Listener가 붙은 뒤에 보인다(Q-K3 B).

| Task | 내용 | 파일 |
|---|---|---|
| K-3.1 | **설정 값 넷과 조건.**<br>• `roarZoomRatio` 0.85(유한한 양수, 1이면 없음)<br>• `roarShakeStrength` 0.4(유한한 0 이상, 0이면 없음)<br>• `roarShakeSeconds` 0.25(유한한 양수)<br>• `roarShakeShape` Bump(Recoil~Rumble, Custom 거절)<br>배율·세기 조건은 설정 Data 한 곳에 두고 `Validate`가 부른다(A-5 규칙). 흔들림 시간·모양의 조건 원본은 카메라 효과(`CameraShakeEvent`)다. DB 생성 때 미리 거를지(조건이 두 곳이 됨)는 착수 때 묻는다(K-1 보정, 설계 K1) | `Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` |
| K-3.2 | **카메라 연출 값 `CombatCameraMotionSettings`.** `RoarZoomRatio`·`RoarShake`, `None`(배율 1·세기 0), `From(settings)`. 능력 동작 값(`CombatAbilityMotionSettings`)과 나눈다. 7인자 생성자를 쓰는 기존 시험을 건드리지 않고, 카메라 값만 바꿀 때 이 클래스만 보면 되게 한다 | 새 `C/CombatCameraMotionSettings.cs` |
| K-3.3 | **포효 조립.** `Compose`에 창구와 카메라 값을 받는다. 줌 항목(1·5)과 `LeaseCameraZoom`은 `CanZoom`이고 배율 ≠ 1일 때만, 흔들림 항목(3, `After(정면 공격, Hit)`)은 `CanShake`이고 세기 > 0일 때만 넣는다. 줌 시간 = `roarPulseSeconds / 2` | `C/CombatRoarEventHandler.cs` |
| K-3.4 | **연결.** 사건 Presenter 생성자에 이름 있는 선택 인자 `cameraMotion`(null이면 `None`), Factory가 설정 표로 만들어 넘긴다 | `C/CombatSceneEventPresenter.cs`, Factory |
| K-3.5 | **에셋 값.** `DataBaseSO/ViewGroup/CombatPresentationSettings/` 설정 에셋에 네 값을 넣는다(기본값 그대로) | 설정 에셋 |
| K-3.6 | **시험.** 조건별 항목 유무(줌·흔들림 각각 있음·없음), 항목 순서와 시점, 줌 대여, 전체 종료 프레임이 줌 없는 포효와 같음, 설정 조건 | `T/CombatAbilityCompositionTests.cs`, `T/CombatPresentationSettingsDataTests.cs` |
| K-3.7 | **공유.** 호진님 설정 표 공유 문서에 값 넷을, 민형님 Factory 공유 문서에 연결 한 줄을 더한다 | `handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md`, Factory 공유 문서 |

**K-3 Unity 확인(재생)**
- 보스 스테이지 포효에서 커짐과 함께 확대되고, 작아짐과 함께 돌아온다.
- 포효가 끝나는 시점과 다음 사건 시작이 지금과 같다.
- 포효 중·직후 낮↔밤 전환에서 크기가 튀지 않는다.
- 전투 이탈·재진입 뒤 카메라 크기가 기준값(7)이다.
- 흔들림은 Listener가 없어 나오지 않는다(의도). Listener가 붙은 뒤 다시 확인한다.

## 3.1 K-4 피격·사망 흔들림 (설계 K8, 결정 Q-K7 A·Q-K8 B·Q-K9 A·Q-K10 A·Q-K11 A)

목표: 피격·사망 조립에 흔들림을 넣고, 같은 순간 흔들림이 겹치면 더 센 것만 보내게 한다. Listener는 사용자가 붙였다(`339efa14`).

| Task | 내용 | 파일 |
|---|---|---|
| K-4.1 | **설정 값 여섯.** `hitShakeStrength` .15·`hitShakeSeconds` .15·`hitShakeShape` Bump, `deathShakeStrength` .3·`deathShakeSeconds` .3·`deathShakeShape` Explosion. 세기만 `Validate`(결정 A). 에셋 값 | 설정 Data, `CombatPresentationSettings_Default.asset` |
| K-4.2 | **카메라 연출 값.** `CombatCameraMotionSettings`에 피격·사망 흔들림(`HasHitShake`·`HitShake`, `HasDeathShake`·`DeathShake`). 기존 생성자 호출은 그대로 컴파일되게 피격·사망 인자는 선택 인자(세기 0 = 없음) | `C/CombatCameraMotionSettings.cs` |
| K-4.3 | **겹침 규칙(Q-K8 B).** 창구가 진행 중 흔들림의 끝 시각·세기를 기억하고, 진행 중이면 더 센 흔들림만 보낸다. 맵이 바뀌면 잊는다. 시계는 생성자 인자 `Func<float>`(Factory = `Time.time`, Cinemachine Impulse 시간과 같은 기준)로 받는다 | `C/Display/CombatCameraWindow.cs`, Factory, 사건 Presenter 기본 창구 |
| K-4.4 | **피격·사망 조립.** 캐릭터 처리기가 창구·값을 받고, `ComposeHit`·`ComposeDeath`에 선택 인자로 넘겨 `CanShake`이고 세기 > 0일 때 흔들림 조각을 `AtStart(0)`, 기다리지 않는 항목으로 더한다(Q-K10 A·Q-K11 A). 완료 시점은 바뀌지 않는다 | `C/CombatCharacterEventHandler.cs`, `C/CombatSceneEventPresenter.cs` |
| K-4.5 | **시험.** 창구 겹침 규칙(가짜 시계: 약한 것 막음·센 것 보냄·끝난 뒤 보냄·맵 바뀌면 잊음), 피격·사망 조립(흔들림 있음·Listener 없음·세기 0·완료 시점 그대로), 설정 값 조건·복사. 창구 생성자 시계 인자로 바뀐 시험 호출 정리 | `T/CombatCameraWindowTests.cs`, 피격·사망 조립 시험, `T/CombatPresentationSettingsDataTests.cs` 등 |
| K-4.6 | **공유.** 호진님 설정 표 공유 §5에 값 여섯, Factory 공유 §16에 시계 인자 | handoffs |

**K-4 Unity 확인(재생):** 여러 대상이 동시에 맞을 때 한 번만 흔들림, 포효 박치기와 그 대상 피격이 겹칠 때 포효 흔들림만, 사망이 피격보다 세게, 아군·적 모두 흔들림, 사건 완료 시점은 지금과 같음.

## 3.2 K-5 기차 지나갈 때 흔들림 (설계 K9, 결정 §6.3)

목표: 기차 앞면이 필드에 들어오는 순간 Rumble 흔들림을 한 번 보낸다. 줌아웃은 넣지 않고 알림 자리만 둔다(사용자 "열어두기만").

| Task | 내용 | 파일 |
|---|---|---|
| K-5.1 | **설정 값 셋.** `trainShakeStrength` .2·`trainShakeSeconds` 1.5·`trainShakeShape` Rumble. 세기만 `Validate`(결정 A). 에셋 값 | 설정 Data, `CombatPresentationSettings_Default.asset` |
| K-5.2 | **카메라 연출 값.** `CombatCameraMotionSettings`에 `HasTrainShake`·`TrainShake`(선택 인자, 세기 0 = 없음) | `C/CombatCameraMotionSettings.cs` |
| K-5.3 | **필드 진입 시각.** (K-6에서 `TryGetFieldSpan`으로 바뀜) 칸 몸 `FieldGridMapper.CellBody`(칸 크기 사각형), 순수 함수 `RailwayContactSchedule.TryGetFieldEntryTime(paths, cellBody, out time)` = 앞면이 경로 레일 칸 몸에 처음 닿는 가장 이른 시각(경로가 없거나 닿지 않으면 false — 알림만 없고 기차 재생은 막지 않음) | `C/Display/FieldGridMapper.cs`, `C/Display/RailwayContactSchedule.cs` |
| K-5.4 | **진입 알림.** 접촉 예약이 선택 생성자 인자 `fieldEntered`를 받고, 준비 때 묶음마다 알림 시각 = min(필드 진입, 첫 접촉)을 정해, 경과 알림 안에서 사망보다 먼저 한 번 부른다. 취소된 묶음은 부르지 않는다 | `C/CombatRailwayContactPlayback.cs` |
| K-5.5 | **카메라 연결.** 재생 조정이 창구·카메라 값을 받아(선택 인자) 알림에서 `CanShake`이고 세기 > 0이면 흔든다. Factory가 같은 창구·값을 넘긴다(카메라 값은 한 번 만들어 사건 Presenter와 함께 씀) | `C/CombatSceneEventPlayback.cs`, Factory |
| K-5.6 | **시험.** 진입 시각(순수: 가장 이른 칸, 역방향, 선로 여럿, 시작부터 겹침 = 0, 닿지 않음·경로 없음 = false), 설정 값 조건·복사, 진입 알림(Unity 전용: 시각 전 안 부름·한 번·사망보다 먼저·첫 접촉이 더 이르면 그때·취소 뒤 안 부름) | `T/RailwayContactScheduleTests.cs`, `T/CombatRailwayContactPlaybackTests.cs`, `T/CombatPresentationSettingsDataTests.cs` |
| K-5.7 | **공유.** 호진님 설정 표 공유 §5에 값 셋, Factory 공유 §16에 재생 조정 인자 | handoffs |

**K-5 Unity 확인(재생, 기차 스테이지):** 기차 앞면이 필드에 들어올 때 흔들림, 치인 유닛의 사망 흔들림도 보임(사망이 더 셈), 기차가 화면 밖에 있을 때는 흔들림 없음, 기차·사망 완료 시점은 지금과 같음.


## 3.3 K-6 기차 지나갈 때 줌인 (설계 K10, 결정 §6.4)

목표: 필드 진입에 살짝 줌인, 앞면이 필드를 빠져나갈 때 복귀. K-5 흔들림과 함께 새 기차 카메라 클래스로 모은다.

| Task | 내용 | 파일 |
|---|---|---|
| K-6.1 | **설정 값 셋.** `trainZoomRatio` .92·`trainZoomInSeconds` .3·`trainZoomOutSeconds` .4, `Validate`(배율·시간 조건 함수). 에셋 값 | 설정 Data, 에셋 |
| K-6.2 | **카메라 연출 값.** `HasTrainZoom`·`TrainZoomRatio`·`TrainZoomInSeconds`·`TrainZoomOutSeconds`(선택 인자, 배율 1 = 없음) | `C/CombatCameraMotionSettings.cs` |
| K-6.3 | **필드 진입·이탈 시각.** 면 거리 `CombatContactGeometry.PlaneLeaveDistance`, 경로 `RailwayTravelPath.LeaveTime`(재생 시간으로 자름), `RailwayContactSchedule.TryGetFieldSpan(paths, cellBody, out entry, out exit)`(K-5 `TryGetFieldEntryTime`을 바꿈) | `C/Display/` 셋 |
| K-6.4 | **이탈 알림.** 접촉 예약이 `fieldLeft`도 받아 같은 경과에서 진입 → 사망 → 이탈 순으로 한 번씩 부름 | `C/CombatRailwayContactPlayback.cs` |
| K-6.5 | **기차 카메라.** `CombatTrainCamera`: 진입(흔들림 + 줌 대여·줌인), 이탈(배율 1로, 자기 시계), `Tick`(돌아가기 끝나면 반환), `End`(남은 줌 바로 되돌림·반환), 맵 해제 취소 콜백. 재생 조정이 만들고 알림·Tick(레일 Tick 앞)·완료 감싸기·기차 취소에 잇는다(K-5 `TrainEnteredField` 제거) | `C/CombatTrainCamera.cs`(새), `C/CombatSceneEventPlayback.cs` |
| K-6.6 | **시험.** 진입·이탈 시각(순수), 기차 카메라(가짜 효과·관리 실행: 줌인·흔들림·복귀·반환·완료 중 되돌림·대여 없음·맵 해제), 접촉 예약 이탈 알림(Unity 전용), 설정 값 | 시험 넷 + 새 `T/CombatTrainCameraTests.cs` |
| K-6.7 | **공유.** 호진님 §5 값 셋, Factory 공유는 변경 없음(Factory 인자 그대로) | handoffs |

**K-6 Unity 확인(재생, 기차 스테이지):** 기차가 필드에 들어오면 살짝 확대, 앞면이 필드를 빠져나가면 원래 크기, 흔들림은 그대로, 기차·사망 완료 시점 같음, 기차 중 낮↔밤 전환이 없어 크기 튐 없음, 전투 이탈 뒤 카메라 크기 7.


## 4. 공유·요청 문서

| 문서 | 내용 | 상태 |
|---|---|---|
| `handoffs/2026-10-19-camera-impulse-listener-request-minhyeong.md` | Stage.prefab Day·Night Camera Listener — 사용자가 붙여(`339efa14`) 알림으로 바꿈 | 작성, 미전달 |
| Factory 공유 문서 새 절 | K-1(서비스 인자·창구 생성), K-3(카메라 값 전달), K-4(창구 시계 인자), K-5(재생 조정에 창구·값) | §16 작성(K-1·K-3·K-4·K-5), 미전달 |
| 호진님 설정 표 공유 새 절 | K-3 값 넷, K-4 값 여섯, K-5 값 셋, K-6 값 셋 | §5 작성(K-3·K-4·K-5·K-6), 미전달 |

- Core 변경 없음. Framework 코드 변경 없음.

## 5. 커밋 묶음 안내 (사용자 커밋)

- 새 파일(모두 `.meta`와 함께, `.meta`는 Unity 6 최소 형식):
  - 코드: `C/Display/CombatMapCameras.cs`·`C/Display/CombatCameraWindow.cs`·`C/Display/ICombatCameraZoom.cs`·`C/CombatCameraShakePiece.cs`·`C/CombatCameraZoomPiece.cs`(K-1), `C/CombatCameraMotionSettings.cs`(K-3)
  - 시험: `T/CombatCameraWindowTests.cs`·`T/CombatCameraPieceTests.cs`·`T/CombatMapCameraTests.cs`(K-1), `T/CombatPieceSignalTests.cs`(K-2)
- K-1에서 만들었다가 지운 `C/Display/CombatCameraShake.cs`(+ `.meta`)는 커밋된 적이 없어 커밋 묶음에 없다.
- K-3: 설정 Data 코드와 설정 에셋 `CombatPresentationSettings_Default.asset`은 한 커밋이다(필드가 없는 에셋은 기본값으로 읽히지만, 값 검사와 에셋을 같이 맞춘다).
- K-4: 새 파일 없음. 설정 Data 코드와 설정 에셋은 한 커밋이다. 이 에셋에는 사용자가 Unity에서 바꾼 값(적 행동 간격 0.8·턴 간격 1, 피격 흔들림 모양 Explosion)도 함께 들어 있다.
- K-5: 새 파일 없음. 설정 Data 코드와 설정 에셋은 한 커밋이다(K-4와 같은 에셋, 같은 커밋이면 된다).
- K-6: 새 파일 `C/CombatTrainCamera.cs`·`T/CombatTrainCameraTests.cs`(+ `.meta`). 설정 Data 코드와 설정 에셋은 한 커밋. K-5의 `TryGetFieldEntryTime`은 K-6에서 `TryGetFieldSpan`으로 바뀌므로 K-5만 따로 커밋했다면 K-6 커밋에 스케줄·접촉 예약·재생 조정·시험이 함께 들어가야 한다.
- Listener 노이즈(2026-10-19, 사용자 요청 B): 새 에셋 `Assets/ProjectL/Prefabs/Stage/MapProp/MapCameraShakeNoise.asset`(+ `.meta`, Cinemachine `NoiseSettings`, 위치 X·Y만, 회전 없음). `Stage.prefab`의 두 Listener Secondary Noise 연결은 사용자가 한다. 프로필과 프리팹은 한 커밋(참조가 끊기지 않게).
- 커밋에서 뺄 것(다른 작업자·에디터 변경): `Display/FieldOutlineRenderer.cs`(데칼 상시표기), `Shader Graphs_GridDecal.mat`, `FMODStudioCache.asset`, 다른 세션 문서.

## 6. 실행 기록

### 6.1 K-1 (2026-10-19, 사용자 "가방 작업 끝났고 K-1 진행해")

- **착수:** 가방 묶음 커밋(`1c229b7a`) 뒤 HEAD `cb0587d5`에서 시작했다(§0.1 (a)). 관리 실행 도구는 앞 세션 스크래치의 Runner·runeach를 이 세션 스크래치로 옮겨 썼다.
- **작업 중 HEAD 변경:** 04:55에 사용자가 `origin/feature/La/AudioSystem_R`를 fast-forward로 받아 HEAD가 `a0f9cd95`가 됐다(천명님 Core·현강님 로비 포함, 제 파일과 겹침 없음). 그래서 기준선은 "새 HEAD + 제 파일만 HEAD 판"으로 다시 빌드했다(k0b: 5,607개 = PASS 3,163·NATIVE 2,301·FAIL 94·UNSUPPORTED 49). 처음 기준선(`cb0587d5`, 5,577개)은 버렸다.
- **구현:** 계획 K-1.1~K-1.8대로. 계획과 다른 점:
  - **흔들림 값(사용자 지적):** 처음에 우리 값 형식 `CombatCameraShake`(세기·시간·모양)와 흔들림 시간·모양 조건 함수를 만들었다. Framework의 `CameraShakeData`·`CameraShakeEvent` 검사와 겹쳐 지우고, 조각·창구가 `CameraShakeData`를 그대로 받게 했다(창구는 `Channel`만 채움, 설계 K1 보정).
  - 줌 배율 조건 `ValidateZoomRatio`를 K-1에서 설정 Data에 더했다(계획은 K-3, 배율은 Framework에 없는 우리 값).
  - 프랍 Presenter의 Unity 시험을 맵 카메라 시험 파일에 함께 두었다(검토 반영).
- **검토 에이전트:** 높음·중간 0, 낮음 4 반영. 보내기 전에 배율 기록(일부 카메라만 나간 뒤 실패해도 모두 되돌림), 프랍 `Release`의 try/finally, 진단 문구(맵 카메라 빠짐 포함), 시험 보강(옛 대여·되돌림 실패·부분 실패·프랍 Presenter). 참고: 운영 카메라는 렌즈 모드 덮어쓰기 없음(Brain 출력 카메라에서 직교를 받음)이라 재생으로 확인한다.
- **Unity가 시험 파일 하나를 가져오지 않음:** `T/CombatMapCamerasTests.cs`는 `.meta`(GUID 고유)가 있는데도 Editor.log에 가져온 기록이 없고 csproj·`Library/ScriptAssemblies`에 없었다(415개 중 하나만). 원인은 찾지 못했다(숨김 속성 아님). 새 파일 이벤트가 생기도록 `T/CombatMapCameraTests.cs`(클래스 이름도)로 바꿨다(GUID 유지). **그래서 K-1·K-2 Unity 확인에는 이 Unity 전용 시험 5개가 들어가지 않았다.**
- **검증:** MSBuild 오류 0, 제 변경의 새 경고 0(기준선 대비 새 경고는 바깥 변경 `FieldOutlineRenderer.cs` CS0162뿐). 관리 실행은 K-2와 합쳐 §6.2.

### 6.2 K-2 (2026-10-19, 사용자 "그냥 두고 K-2 진행해")

- **신호 일반화를 이쪽이 먼저 만들었다(§0.3):** 클립 AC-3은 착수 전이었다. `Hit = 2`(클립의 `ClipStart`·`ClipEvent`는 들어온 순서대로 3·4가 된다). 신호 규칙의 원본은 조각·조립 설계 BP2 보충 한 곳이다.
- **구현:** K-2.1~K-2.5. 시작 안 끝 신호의 옛 `_endRequested`는 시작 안 신호 목록(`_beginSignals`, 낸 순서)으로 합쳤다. 끝 신호 처리 고리를 `Trigger(index, signal, available)`로 뽑았다. 미도착 규칙은 "실행 끝"이 아니라 "항목이 끝나는 순간"에 판정한다(그래야 실행이 끝남, BP2 보충).
- **검증(K-1+K-2, k4):** MSBuild 오류 0·새 경고 0, 관리 실행 기준선(k0b) 대비 **기존 시험 상태 변화 0**(5,642개 = PASS 3,193·NATIVE 2,306·FAIL 94·UNSUPPORTED 49), 새 시험 35개(PASS 30, Unity 전용 5).
- **Unity:** K-1·K-2 묶음 문제 없음(2026-10-19, 사용자 확인). 다만 위 맵 카메라 Unity 시험 5개는 빠져 있었다.
- **검토 에이전트(Unity 확인 뒤 반영):** 높음 0, 중간 1·낮음 4.
  - [중간, HEAD에도 있던 잠재 결함] 일반 완료 실행이 중첩 경로 안에서 전체 종료하면(시작 안에서 모두 끝남, 기다림 항목 뒤 길이 0 항목이 마지막) 완료가 한 번도 가지 않는다. 건너뛰기 규칙으로 생기기 쉬워졌다. → 전체 종료 때 아직 알리지 않았으면 일반 완료를 알린다.
  - [낮음] 건너뛰기를 "아직 Pending"이 아니라 "아직 내지 않은 신호"로 판정. 반영하다 시작 안 `끝 → 타격` 순서에서 기다리던 항목이 남는 구멍을 찾아, 낸 표시를 처리할 때 켜도록(`Emit`) 고쳤다.
  - [낮음] 문서 문구(시작 안에서 끝 뒤에 낸 신호는 버림), 시험 보강 8개(완료 누락 두 경우·지연 중 항목·출처 완료 건너뜀·시작 안 타격/끝 두 순서·끝 뒤 늦은 타격·처리 도중 끝남).
  - [낮음, 공유 문서] 클립 계획 T15·T16이 `ClipStart = 2`·옛 `_endRequested`를 가리킨다. 다른 세션 문서라 고치지 않았다(보고만).

### 6.3 K-3 (2026-10-19, 사용자 "Unity 확인 끝나면 K-3 진행해", 결정 A)

- **구현:** K-3.1~K-3.7. 설정 값 넷(배율 .85·세기 .4·시간 .25·모양 Bump)과 에셋 값, `CombatCameraMotionSettings`(None·From·`RoarShake`), 포효 조립(줌인·타격 흔들림·줌 복귀, 조건별로 넣거나 뺌), 사건 Presenter·Factory 연결.
- **결정 A:** 흔들림 시간·모양은 DB 생성에서 거르지 않는다(설계 K5). 배율·세기만 설정 Data의 조건 함수로 검사한다.
- **검토 에이전트:** 높음·중간 0, 낮음 3 반영. 결정 A 뒤 문서 문구(설계 §3.5·K5 표, 설정 Data 요약), 흔들림 값 예외가 타격 순간 공격 재생기 Tick 쪽에서 올라온다는 진단 기록, Presenter의 `None` 기본값 겹침 정리(처리기가 맡음). 참고: 공격 모션이 없는 포효에서 줌 복귀가 같은 프레임 안 줌인 마지막 Tick보다 먼저 나가지만 결과는 같다(기록 배율에서 시작, 효과가 덮어씀).
- **검증(K-1~K-3 + K-2 검토 반영, k5·k6):** MSBuild 오류 0·제 변경의 새 경고 0. 관리 실행 기준선(k0b) 대비 **기존 시험 상태 변화 0**(5,662개 = PASS 3,213·NATIVE 2,306·FAIL 94·UNSUPPORTED 49). 새 시험 55개(PASS 50, Unity 전용 5 = `CombatMapCameraTests`). 마지막 소소한 수정 뒤 관련 시험 226개 결과 같음. Unity 에디터 컴파일 성공(05:58, Editor.log·`Library/ScriptAssemblies`에 새 타입·`CombatMapCameraTests` 들어감, 시험 결과는 아님).
- **Unity:** K-3 문제 없음(2026-10-19, 사용자 확인).
- **Unity 확인 목록(사용자):**
  - Test Runner: `CombatMapCameraTests`(처음 도는 Unity 전용 5개), `CombatCameraWindowTests`, `CombatCameraPieceTests`, `CombatPieceSignalTests`, `CombatAbilityCompositionTests`, `CombatPieceRunTests`, `CombatPieceRunPlayerTests`, `CombatPresentationSettingsDataTests`, `CombatEnemyPresentationTests`, `DataBaseAssetTests`, 그리고 평소 EditMode 전체.
  - 재생(보스 스테이지 2101): 포효 때 커짐과 함께 확대·작아짐과 함께 복귀, 포효 끝과 다음 사건 시작 시점이 이전과 같음, 낮↔밤 전환 전후 크기 튐 없음, 전투 이탈·재진입 뒤 카메라 크기 7. 흔들림은 Listener가 없어 나오지 않는 것이 맞다(Q-K3 B).
  - 줌이 안 보이면: AfterLoad 카탈로그 수집에 Day/Night Camera가 들어갔는지(설계 §3.5 전제), 렌즈 직교 판정(모드 덮어쓰기 없음)을 본다.

### 6.4 K-4 (2026-10-19, 사용자 "피격 및 사망에 넣어줘", "Q-K7~Q-K11 만 권장안대로")

- **구현:** K-4.1~K-4.6. 설정 값 여섯과 에셋 값, `CombatCameraMotionSettings`의 `HitShake`·`DeathShake`(세기 → 효과 값 변환은 `ShakeValues` 한 곳), 창구 겹침 규칙(진행 중이면 더 센 흔들림만, 시계 주입 `Func<float>`, Factory = `Time.time`), 피격·사망 조립에 흔들림 조각(시작 순간, 기다리지 않음, 아군·적 모두), 사건 Presenter → 캐릭터 처리기 전달.
- **검토 에이전트:** 사용자 지시("검토 멈추고")로 중간에 멈춰 결과 없음.
- **사용자 에디터 변경(같은 에셋):** 적 행동 간격 0.8·턴 간격 1, 피격 흔들림 모양 Explosion, 기차 흔들림 세기 3·Explosion. Listener Secondary Noise에 노이즈 프로필(아래 6.6 끝).

### 6.5 K-5 (2026-10-19, 사용자 "기차 지나갈 때도 카메라 효과 넣어줘", 결정 §6.3)

- **구현:** 설정 값 셋(세기 .2·1.5초·Rumble), 칸 몸 `FieldGridMapper.CellBody`, 필드 진입 시각, 접촉 예약의 진입 알림(같은 경과의 사망보다 먼저, 첫 접촉보다 늦지 않게), 재생 조정 연결. 줌아웃은 사용자 "열어두기만"으로 알림 자리만 둠 → K-6에서 사용.

### 6.6 K-6 (2026-10-19, 사용자 "기차 나올 때 약간의 줌도", 결정 §6.4 줌인·Q-K16 A)

- **구현:** 설정 값 셋(배율 .92·들어감 .3초·돌아옴 .4초), 면 거리 `PlaneLeaveDistance`(접촉과 `Reach` 공유)·`RailwayTravelPath.LeaveTime`·`RailwayContactSchedule.TryGetFieldSpan`(K-5 진입 함수를 바꿈), 접촉 예약의 이탈 알림(진입 → 사망 → 이탈 순), 새 `CombatTrainCamera`(흔들림 + 줌 대여·줌인·복귀·자기 시계·완료·취소, K-5의 재생 조정 흔들림 함수를 옮김), 재생 조정이 알림·Tick(레일 Tick 앞)·완료(정리 뒤 통지)·기차 취소에 연결.
- **검증(K-4~K-6 합친 빌드 k10, 기준선 k7 = HEAD `339efa14` 전체 5,662개 = PASS 3,213·NATIVE 2,306·FAIL 94·UNSUPPORTED 49):** MSBuild 오류 0·새 경고 0(새 파일 둘은 targets로 더함). 관리 실행 **기존 시험 상태 변화 0**. 카메라 새 시험 31개(PASS 27, Unity 전용 4 = 접촉 예약 진입·이탈 알림). K-4+K-5 단계 실행(k9)도 상태 변화 0·새 시험 20개였다.
  - 같은 작업 트리에 다른 세션의 이동 범위 작업이 있어 k10 빌드에 들어갔다: `CombatMoveModeRangeTests` 8개·`CombatMovementRangeFeedbackTests` 새 이름 8개(모두 PASS), 옛 이름 6개 사라짐. 카메라 작업과 무관하다.
- **원칙 검사 2차:** [평가 문서](../specs/2026-10-19-combat-camera-principles-evaluation.md) 2차 — 7.8/10(SSOT 7·SOLID 7·FW 9·독립성 8), 새 낮음 C11~C14.
- **Listener 노이즈(사용자 요청 B):** `Assets/ProjectL/Prefabs/Stage/MapProp/MapCameraShakeNoise.asset`(위치 X·Y만 0.7 + 0.3, Perlin, 회전 없음). `6D Shake`는 좌우 진폭이 위아래의 약 1/7이라 좌우가 보이지 않았다. 사용자가 두 Listener에 연결했다(스테이징됨).
- **Unity 확인 목록(사용자):**
  - Test Runner: `CombatRailwayContactPlaybackTests`(새 4개 포함), `CombatRailwayContactSceneTests`, `CombatTrainCameraTests`, `RailwayContactScheduleTests`, `CombatCameraWindowTests`, `CombatHitCompositionTests`, `CombatDeathCompositionTests`, `CombatPresentationSettingsDataTests`, `DataBaseAssetTests`, 평소 EditMode 전체.
  - 재생(일반 스테이지): 여러 대상이 동시에 맞을 때 한 번만 흔들림, 사망이 피격보다 셈, 아군·적 모두, 피격·사망 완료 시점 같음.
  - 재생(보스): 포효 박치기 흔들림 중 그 대상 피격 흔들림은 안 나감.
  - 재생(기차 스테이지): 앞면이 필드에 들어올 때 흔들림 + 살짝 줌인, 앞면이 필드를 빠져나가면 원래 크기, 기차가 화면 밖일 때 흔들림·줌 없음, 기차·사망 완료 시점 같음, 전투 이탈 뒤 카메라 크기 7. 지금 기차 흔들림 세기 3이면 기차 흔들림 1.5초 동안 사망 흔들림은 막힘(겹침 규칙).
