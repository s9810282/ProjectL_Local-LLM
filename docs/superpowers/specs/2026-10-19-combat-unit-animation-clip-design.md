# 전투 유닛 애니메이션 클립·이벤트 기반 설계

- 작성일: 2026-10-19. 상태: **설계 4차(idle 바닥 층 반영), 결정 완료(Q-AC1~Q-AC10 모두 A, §6), AC-1~AC-4 구현·Unity 확인 완료(계획 §8.5).** **5차(§10, 원거리 곡사·직선 클립 공격 AC-5) 결정 완료(Q-AC13 B·Q-AC14~Q-AC17 A, §10.5).** **7기준 구조 검사(§11): 8.3/10, Q-AC18 A·Q-AC19 A 결정, K3·K16·K17은 AC-5에 넣음(2026-10-19).** **6차(§12, 부속 클립 — 무기 테이크를 몸 클립과 같이 재생) 설계 초안: Q-AC12 A → B(2026-10-19 사용자), Q-AC20~Q-AC22 모두 A(2026-10-19), 계획 §10.** §2 범위의 "Mortar 클립 공격 하지 않음"은 5차로 바뀌었다(곡사·직선은 함, Ballista는 여전히 하지 않음).
- Task 계획: [unit-animation-clip-plan](../plans/2026-10-19-combat-unit-animation-clip-plan.md)(AC-1~AC-4). 4차 반영은 계획 §0.5에 적는다.
- UML: [전투 클립 연출 흐름](https://claude.ai/artifact/DysLcFNSLmR4okBskHTYdF)(4차 기준으로 고침).
- 기준 코드: `feature/Yonggeun/Stage` HEAD `2c6a2fbb` + 커밋 전 카메라 K-1~K-3. 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`. 폴더 정리 계획(M)이 먼저 들어가면 경로는 그 계획 M-4.3이 새 폴더로 고친다.

### 사용자 요청과 결정의 흐름

| 차수 | 사용자 말(원문) | 설계에 준 영향 |
|---|---|---|
| 1차 | "미리 베이스 잡아두려는거야 / 1. 애니메이션 클립을 이용하는 경우 유닛별로 사용하는 애니메이션 클립이 다를 수가 있음 / 2. 애니메이션 노티파이를 만들어서 애니메이션 클립이 실행되는 시작, 끝, 히트타이밍 등의 연출을 위한 타이밍 까지해서 총 3곳에서 Action을 쏠 예정" | 유닛별 클립 묶음, 이벤트 셋 → Action 셋 |
| 2차 | "0. 애니메이션 클립마다 직접 [시작,] [끝], [이벤트 타이밍] 총 3개의 애니메이션 이벤트를 넣을거고 그에 따른 함수를 구조화, 통일해서 빼놔야겠지 / 1. 현재 공격 모션은 코드로 진행 중, 이걸 클립이 존재할 경우 클립으로 진행 / 2. 기존 공격 타이밍에 맞춘 피격자 피격 모션 출력. 애니메이션 이벤트를 통한 이벤트 타이밍에 각 종 연출 (이펙트, 사운드, 피격자 피격모션 실행 등) 시작, 연출에는 선딜레이가 있을 수 있겠지." | 이벤트 셋 필수, 함수 셋 통일, 클립 공격 = 조립(이벤트 타이밍 + 선딜레이) |
| 3차 | "모두 권장안으로 하는데 SSOT(…)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성로 평가해" | Q-AC1~Q-AC8 모두 A, 원칙 평가 E1~E16(§8) |
| 4차 | "우리 idle 모션 있어" → "idle 모션 들어갈거라 컨트롤러 필요할거같다고" → "그니까 컨트롤러에서는 그냥 Idle만 두면 된다고?" → "오케이 그러면 이거 기반으로 다시 설계해줄래, 카메라 작업도 거의 마무리 단계야" | 컨트롤러 = Idle(평상시)만, 사건 클립은 코드가 그 위에 얹음, 끝나면 Idle로, 바로 갈아 끼움(AC9·AC10) |
| 4차 결정 | "Q-AC9(컴포넌트는 입력 계층)과 Q-AC10(Unity 게임 시간 그래프 + 루트 곡선 에디터 검사) 둘 다 A로 정해" | 결정 완료(§6) |

## 1. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 유닛 표시 계약에 클립 채널이 없다. 창구가 내주는 것은 자세·크기·걸음 위치·방향·이동 투명도·피격 플래시·무기다 | `C/Display/ICombatUnitPresentationTarget.cs`, `CombatUnitPresentationWindow` |
| F2 | 조립 채널 열거형은 유닛 채널 `Scale`·`Pose`·`Flash`와 장면 채널 `CameraZoom = 101`이다. 대여·복원은 실행 대여 묶음 한 곳에서 채널마다 한 칸이다(카메라 C6, `3c50a379`, 계획 §0.7) | `C/CombatComposition.cs`, `C/CombatPieceRunLeases.cs` |
| F3 | **신호 장치는 카메라 K-2가 만들었다(커밋 전, Unity 확인 완료).** 신호 열거형 `End = 1`·`Hit = 2`, 맥락의 `SignalCallback`, 실행의 `Signal`, 미도착 규칙(`SkipUnsignalled`), 규칙 원본은 조각·조립 설계 BP2 보충, 조각 계약 문서도 고쳐졌다 | `C/CombatPieceStart.cs`, `C/CombatPieceContext.cs`, `C/CombatPieceRun.cs`, `C/ICombatPresentationPiece.cs`, 카메라 계획 상태 |
| F4 | `Hit`은 대상이 맞는 순간(Gate 해제, `HitAll`과 같은 순간)이다. 클립의 이벤트 타이밍과는 다른 사실이다 | 카메라 설계 K4, `CombatPieceStart.cs` 문서 |
| F5 | 공격 표시 종류 넷은 모두 코드 모션이다. 시점은 Linear가 SO 비율, Melee가 코드 상수 55%다 | `C/Display/CombatAttackMotion.cs` 파생 클래스 |
| F6 | 공격 재생이 목표·방향을 정하고 Gate 수명을 소유한다. 대상이 해제되면 그 대상의 피격 사건(반응 + 플래시 조립)이 시작된다 | `C/CombatAttackPlayback.cs`, `C/CombatHitGate.cs` |
| F7 | 포효는 이미 "조립 + 출처 수명(Gate)"으로 재생된다 | `C/CombatRoarEventHandler.cs` |
| F8 | 출처 수명 창구에는 해제 입구가 없다 | `C/ICombatSourceLifetime.cs` |
| F9 | 사건 처리기와 조각 실행 재생기는 Presenter 생성자에서 조립된다(Factory가 아님) | `C/CombatSceneEventPresenter.cs` |
| F10 | 소리 조각은 있고 이펙트 조각은 없다 | `C/CombatEventSoundPiece.cs` |
| F11 | 유닛별 연출 선택은 `UnitVisualData`(키 `EntityKey`)가 공격 SO·반응 SO를 참조하는 방식이다 | `UnitVisualDataSO.cs` |
| F12 | `FieldUnitView`는 프리팹 인스턴스 루트에 런타임으로 붙고, `OnEnable`·`OnDisable`·`ResetForPool`·`Bind`에서 대여를 리셋한다. 모델(FBX) 자식의 Animator는 아무도 쓰지 않는다 | `C/Display/FieldUnitView.cs` |
| F13 | 클립 재생 선례는 기차다. 바인딩이 Animator를 명시하고, 사건 재생 중에만 수동 그래프를 만들며, 컨트롤러가 오면 `AnimatorControllerPlayable`로 그래프에 넣는다. 평가 직후 Animator 루트를 되돌린다 | `C/Display/FieldRailwayPresenter.cs` `TrackVisual` |
| F14 | 캐릭터 FBX 14개는 Generic 리그이고 애니메이션 곡선이 없다. 프로젝트의 `.anim`·`.controller`는 기차용 둘뿐이다. idle은 앞으로 들어온다(사용자) | FBX 본문 검사(`AnimationCurveNode` 0개) |
| F15 | **표시 쪽에는 매 프레임 Tick이 없다.** 연출 Tick(`OrderedCombatPresentation.Tick`)은 결과를 재생하는 동안에만 돈다. 입력 국면에는 아무 연출 Tick도 돌지 않는다 | `OrderedCombatPresentation.Tick`(`_result == null`이면 돌아감), `CombatViewSystem.TickCore` |
| F16 | 런타임은 `GameBootstrap.Update`에서 돈다. 그래서 연출 Tick이 정한 값은 같은 프레임 Unity 애니메이션 평가(Update 뒤)에 반영된다 | `GameBootstrap.Update` → `_runtime.Tick` |
| F17 | `DataBaseSystem`은 `ViewSystem`을 참조하지 않고 MonoBehaviour도 없다 | 코드 검색(계획 §0.1 P1) |
| F18 | 폴더 정리 계획(M)은 권장 순서가 "카메라 K-1 커밋 → 폴더 정리 → 카메라 K-2·K-3·클립 AC"이고, 클립 계획의 경로를 M-4.3에서 새 폴더로 고친다 | 폴더 정리 계획 §8 Q-M2·§6 M-4.3 |

## 2. 범위

- **한다(기반):**
  - 유닛별 클립 표(View DB)와 이벤트 셋(시작·이벤트 타이밍·끝)
  - **Idle 바닥 층:** 프리팹 Animator의 컨트롤러(Idle)를 그래프 바닥에 깔고, 사건 클립을 그 위에 얹었다가 끝나면 Idle로 돌아감(AC9)
  - 유닛 클립 채널(창구 대여), 클립 재생 단위(Action 셋)
  - 클립 조각과 신호 값 둘(`ClipStart`·`ClipEvent`)
  - 클립 공격 조립(이벤트 타이밍 + 선딜레이로 피격 해제·소리, 이펙트 자리)
  - 피격자 쪽 피격 클립
- **하지 않는다:**
  - Idle과 사건 클립을 섞는 블렌드(바로 갈아 끼움, 사용자 결정). 끊김이 보이면 섞는 시간 값 하나로 나중에 더한다
  - 컨트롤러 안의 공격·피격 상태, 컨트롤러 파라미터, 루트 모션, 재생 속도 조절
  - 이펙트 조각 자체, 사망·포효·소환 전용 클립, Mortar·Ballista 클립 공격
  - Core 변경, Framework 변경, 새 Service·ServiceKey·AddModule

## 3. 결정

### AC1. 유닛별 클립 표 — View DB에 둔다

- 새 입력 SO `CombatUnitAnimationSO`(클립 묶음)를 `UnitVisualData`의 칸 하나가 참조한다. 여러 유닛이 공유할 수 있고, 비우면 지금처럼 코드 모션만 쓴다. DB 카탈로그·`ViewGroup.asset`은 바뀌지 않는다.
- 묶음은 이름 있는 두 칸, **공격 칸**과 **피격 칸**이다(계획 P2).
- 값은 둘로 나눈다(E6): `CombatClipData`(클립, 세 이벤트 시각), `CombatClipAttackData`(클립 값 + 피격·소리 선딜레이). `CreateData()`가 매번 새로 만든다(P2). 시각은 클립에서 읽은 사본이라 저장하지 않는다(P7).
- **Idle은 이 묶음에 넣지 않는다.** Idle의 원본은 프리팹 Animator의 컨트롤러다(AC9).
- DB 검사(부트에서 정지). 규칙의 원본은 `CombatUnitAnimationRules` 한 곳이고, 외형 표(`UnitVisualData.Validate`)와 묶음 SO는 부르기만 한다(원칙 검토 2026-10-19, 계획 §8.2):
  - 사건 클립: 클립 있음, 길이 양수, 반복 클립 금지, 루트 곡선 금지, 이벤트 셋 규칙(AC2, `CombatClipEvents.Read`), 선딜레이 0 이상
  - 유닛: 이벤트 컴포넌트가 있으면(묶음이 없어도 — 그 Animator를 전투 재생이 넘겨받으므로) 정확히 하나이고 같은 GameObject에 Animator(`FindIn`, AC4), 그 컨트롤러(Idle) 클립에 루트 곡선 없음. 묶음이 있으면 컴포넌트가 있어야 하고, 공격 칸은 Melee만(AC-5 전, 계획 P5)
  - **루트 곡선 검사는 에디터에서만(AC10 A):** Animator GameObject 자신의 Transform을 움직이는 곡선을 막는다. `AnimationUtility`는 에디터 전용이라 플레이어 빌드에서는 이 검사만 빠지고(I3), 실제 카탈로그 에셋 시험이 같은 검사를 돈다

### AC2. 이벤트 셋 — 함수 통일과 규칙

- 클립마다 **시작·이벤트 타이밍·끝을 하나씩 반드시** 넣는다. 빠짐·중복·순서 오류는 DB 생성에서 실패한다. 같은 시각이면 시작 → 이벤트 타이밍 → 끝 순이다.
- **함수 통일:** 컴포넌트 `CombatAnimationEvents` 하나에 `ClipStart()`·`ClipEvent()`·`ClipEnd()`를 둔다. 애니메이션 창에서 목록으로 고르고, 이름의 원본은 이 컴포넌트다(코드는 `nameof`).
- **실행 중에는 우리 재생기가 부른다(Q-AC2 A):** DB가 이벤트 시각을 읽어 두고, Unity 애니메이션 이벤트는 끈다(`fireEvents = false`). 그래서 컴포넌트 함수 셋은 이름표이고 실행 중에 불리지 않는다. 불리면 오류 로그를 한 번 남긴다(E9). Idle 클립의 이벤트도 같이 꺼진다.
- **뜻:** 시작 = 클립 연출 시작 알림, 이벤트 타이밍 = 이펙트·소리·피격 모션의 기준 시점(대상이 맞는 순간 `Hit`과 다른 사실, F4), 끝 = 클립 재생의 끝(끝 뒤 프레임은 재생하지 않음, Q-AC4 A).
- **발화 규칙:** (이전, 지금] 구간 교차, 한 Tick에 여러 개면 시각 순서대로, 끝까지 발화하면 재생 단위가 끝남, 취소 뒤에는 Action이 오지 않음. Action은 타이밍 알림이지 정리 수단이 아니다.

### AC3. 유닛 클립 채널 — 창구 대여 (4차 개정, 대여 `Play` 인자는 6차 §12 AC17에서 고침)

- 표시 계약 `SupportsClip`·`BeginClip(cancelled)`, 창구 `TryBeginClip`, 조립 채널 `CombatPieceChannel.Clip`, 맥락 `Clip(unit)`. 피격 플래시와 같은 모양이다.
- 채널 구현은 `UnitClipChannel`(일반 C#)이고, `FieldUnitView`는 들고 위임만 한다(E5).
- **대여 규칙:** 유닛당 하나(배타). 재대여·표시 꺼짐·풀 반환이면 취소를 알린다. 복원은 여러 번 불러도 안전하다.
- **대여 계약 `ICombatUnitClip`:** `Play(AnimationClip)`(사건 클립을 위층에 꽂고 시각 0), `SetTime(float time)`(사건 클립 시각만 정함, 평가는 AC10), `RestoreClip()`(사건 클립을 빼고 바닥으로 돌아감), `IsAvailable`. 3차의 `Evaluate`는 AC10 A에서 평가를 하지 않으므로 `SetTime`으로 이름을 바꾼다.
- **다른 채널과 겹침:** 자세(루트 위치·회전)·크기(루트 크기)·플래시(렌더러 색)와 동시에 쓸 수 있다. 클립과 Idle은 모델 자식 Animator 아래만 움직인다.

### AC4. Animator 자리 — 이벤트 컴포넌트가 표시한다

- `CombatAnimationEvents`가 붙은 GameObject의 Animator가 우리 그래프가 맡는 Animator다. 규칙(정확히 하나, 같은 GameObject에 Animator)은 `CombatAnimationEvents.FindIn(root)` 한 곳에만 있고, DB 검사와 표시 준비가 함께 부른다(G2).
- **컴포넌트가 있는 유닛은 우리 그래프가, 없는 유닛은 Unity가 Animator를 움직인다.** 이 규칙도 `FindIn` 결과 하나로 정해진다.
  - 묶음이 있는 유닛은 컴포넌트가 필수다(AC1 검사).
  - 묶음 없이 Idle만 있는 유닛은 컴포넌트를 붙이지 않아도 된다. Unity가 컨트롤러를 평소처럼 재생한다. 코드 모션 공격·반응은 루트를 움직이므로 Idle과 함께 돈다.
- **층 위치(Q-AC9 A, 결정):** 컴포넌트를 입력 계층(`DataBaseSystem/Data/View/CombatPresentation/`)에 둔다. 이름 원본·이벤트 읽기·검사가 DB 쪽 한 곳에 있고 View는 읽기만 한다(F17). DataBaseSystem의 첫 MonoBehaviour이고, 범위는 이름표·자리 표시·규칙 함수(`FindIn`)로 한정한다(G5).

### AC5. 클립 재생 단위 — 이벤트 셋에서 Action 셋으로

- 순수 커서 `CombatClipEventCursor`(Unity 없음)가 발화 규칙을 갖고, `CombatClipPlayback`이 대여 하나·클립 값·Action 셋(`started`·`eventTiming`·`ended`)으로 재생한다. 시계는 연출 Tick이고, 매 Tick `SetTime`으로 사건 클립 시각을 넣는다.
- 클립을 재생하는 곳은 모두 이 단위를 쓴다. 지금은 클립 조각 하나다(O4).

### AC6. 조립의 클립 조각과 신호 (4차 개정: 신호 장치는 이미 있음)

- `CombatClipPiece(unit, clip)`는 자기 시계 조각이다. 맥락의 클립 대여로 재생 단위를 시작한다.
- **신호:** 장치(맥락의 `SignalCallback`, 시간 규칙, 미도착 규칙)는 카메라 K-2가 만들었다(F3). 이 설계는 값 둘만 더한다.

| 신호 | 뜻 | 내는 조각 | 더한 곳 |
|---|---|---|---|
| `End` | 조각이 끝남. 클립 조각에서는 끝 이벤트 | 모든 조각 | 기존 |
| `Hit` | 대상이 맞는 순간(Gate 해제). 해제 뒤에 냄 | 정면 공격 조각, 해제 조각(AC7), 발사 조각(AC-5) | 카메라 K-2 |
| `ClipStart` | 클립의 시작 이벤트 | 클립 조각 | 클립 AC-3 |
| `ClipEvent` | 클립의 이벤트 타이밍(연출 기준 시점) | 클립 조각 | 클립 AC-3 |

- 선딜레이는 이미 있는 시점 지연이다: `After(clip, ClipEvent, 선딜레이)`.
- **피격자 쪽:** 피격 조립(반응 + 플래시)에, 피격 칸이 있으면 클립 조각을 기다림 항목으로 더한다. 클립이 끝나면 그 유닛은 Idle로 돌아간다(AC9).

### AC7. 클립 공격 — 포효와 같은 조립

- 공격 처리기가 공격 칸이 있으면 `CombatClipAttack.Compose`(정적 함수 하나, E3)의 조립을 조각 실행 재생기에 넘기고, 없으면 지금처럼 공격 재생(코드 모션)을 부른다. Factory는 바뀌지 않는다(F9).
- 조립: 행동 실행, 출처 완료(공격 사건, 공격자, 첫 해제), 대여는 자세·클립이다.

| # | 항목 | 시점 | 하는 일 |
|---|---|---|---|
| 0 | 방향 조각 | 시작 | 목표 쪽을 봄(코드 공격과 같은 공유 함수, E2) |
| 1 | 클립 조각(공격 칸) | 시작 | Idle 위에 공격 클립, 신호 셋 |
| 2 | 해제 조각 | `After(1, ClipEvent, 피격 선딜레이)` | 대상 모두 해제 → 피격자의 피격 사건 시작, 이어서 `Hit`(카메라 항목이 거는 타격 순간, 계획 §0.6 CK4) |
| 3 | 소리 조각 | `After(1, ClipEvent, 소리 선딜레이)` | 공격 소리 |
| 4 | 이펙트 조각(`CombatEffectPiece`, 2026-10-19 구현) | `After(1, ClipEvent, 이펙트 선딜레이)` | 공격 칸에 이펙트가 있을 때만, 공격자의 이펙트 지점에서 한 번(원본 [클립 이펙트 설계](2026-10-19-combat-clip-effect-design.md) EF1~EF6) |

- 완료: 사건 완료 = 첫 해제, 전체 종료 = 모든 항목 끝(클립 끝 이벤트까지). 끝나면 공격자는 Idle로 돌아간다.
- 공용 변경: 출처 수명 창구에 `ReleaseAll(source, version)`(해제 규칙 원본은 Gate 하나, F8).
- 두 길의 공통 규칙(E13): 방향(공유 함수)·소리 내용(소리 표)·해제와 완료(Gate)·시작 조건(실행 목록·Gate)이 각각 한 곳이고, 다른 것은 몸 동작과 시점 원본뿐이다.
- 원거리(AC-5)는 해제 조각 자리에 발사 조각이 들어가고 접촉 때 `Hit`을 낸다.

### AC8. 값의 자리

| 값 | 원본 |
|---|---|
| 어느 유닛이 어떤 사건 클립을 쓰나 | `UnitVisualData` → 클립 묶음 SO |
| **Idle(평상시 동작)** | **프리팹 Animator의 컨트롤러(Idle만)** |
| 이벤트 셋의 시각 | 클립 자신 |
| 이벤트 함수 이름 | `CombatAnimationEvents` |
| 우리가 맡는 Animator | 같은 컴포넌트가 붙은 GameObject |
| 선딜레이 | 공격 칸 항목 |
| 공격 해제 시점 | 코드 모션이면 SO 비율·코드 상수, 클립이면 이벤트 타이밍 + 피격 선딜레이 |
| 신호의 뜻 | AC6 신호 표 |
| 신호 시간·미도착 규칙 | 조각·조립 설계 BP2 보충 |
| 발사 지점(5차, §11 K17) | 외형 종류마다 다른 사실: 곡사 코드 모션 = `CatusCannonWeaponView.MuzzlePosition`, Ballista = `BallistaWeaponView.MuzzlePosition`, 클립 공격 = `CombatAnimationEvents`의 `muzzle`. 직선 코드 모션은 포구 없이 올린 유닛 위치(`CombatShotPath.LinearStart`) |

### AC9. Idle 바닥 층 (4차 신설, 위층 모양은 6차 §12 AC17에서 고침)

- **나누는 규칙:** 전투 사건이 시작시키는 동작은 클립 묶음 SO와 코드가, 사건과 상관없이 늘 도는 동작은 컨트롤러가 가진다. 컨트롤러에는 **Idle만** 둔다(Idle끼리의 전환은 가능, 파라미터 없음, 공격·피격 상태 없음, 루트 모션 없음). 사용자 결정이다.
- **그래프 모양(컴포넌트가 있는 유닛):**

```text
AnimationPlayableOutput(Animator)
  └─ AnimationMixerPlayable
       ├─ 입력 0: AnimatorControllerPlayable(프리팹의 컨트롤러 = Idle)   평소 가중치 1
       └─ 입력 1: AnimationClipPlayable(사건 클립, 대여 중에만)          대여 중 가중치 1, 입력 0은 0
```

- 표시 준비 때 채널이 Animator의 컨트롤러를 읽어 입력 0에 넣고, Animator의 컨트롤러 칸은 비운다. 우리 그래프 하나만 Animator를 움직이게 하기 위해서다. Override Controller도 `RuntimeAnimatorController`라 그대로 들어간다.
- **전환은 바로 갈아 끼운다**(가중치 0/1, 사용자 결정). 대여가 끝나면 입력 1을 빼고 입력 0을 1로 돌린다. 화면은 Idle로 돌아간다.
- **컨트롤러가 없는 유닛:** 입력 0이 비어 있다. 대여 중에만 Animator를 켜고, 대여가 끝나면 끄고 기억한 Transform 값으로 되돌린다(계획 P3를 이 경우에만 씀).
- Idle과 함께 도는 것: 코드 반응(흔들림)·플래시·포효 크기·소환 등장은 루트나 렌더러를 쓰므로 Idle과 겹친다. 사망 반응 중에도 Idle이 돈다(사망 클립 칸은 범위 밖, §5).

### AC10. 그래프 시간과 수명 (4차 신설, Q-AC10)

- **문제:** Idle은 입력 국면에도 돌아야 하는데, 표시 쪽에는 매 프레임 Tick이 없다(F15).
- **A(결정): 그래프를 Unity 게임 시간으로 돌린다(`DirectorUpdateMode.GameTime`).**
  - Idle(입력 0)은 Unity가 진행한다. 사건 클립(입력 1)은 속도 0으로 두고, 연출 Tick이 `SetTime`으로 시각을 넣는다.
  - 런타임이 `Update`에서 돌아 Unity 평가보다 앞서므로 한 프레임 밀림이 없다(F16).
  - 조정자·표시 계약에 매 프레임 경로를 새로 만들지 않는다.
  - 대가: 평가가 Unity 쪽에서 일어나, 기차처럼 평가 직후 Animator 루트를 되돌릴 수 없다(F13). 그래서 "Idle·사건 클립은 Animator GameObject 자신의 Transform을 움직이지 않는다"를 에디터 검사로 막는다(AC1). 루트가 움직이지 않으므로 배치·방향의 원본은 계속 Core 사실과 코드 자세다.
- B(고르지 않음): 기차처럼 수동 평가를 유지하고, 조정자 Tick 끝에서 매 프레임 표시 애니메이션 Tick을 새로 둔다. 평가 직후 루트 되돌림이 유지되지만 조정자·표시 계약을 고치고 매 프레임 경로가 하나 늘어난다.
- **수명(두 안 같음):** 그래프는 유닛 표시 수명(풀 단위)이다. 표시 준비 때 만들고, 표시가 꺼지면 멈추고(`Stop`), 켜지면 다시 돌리고(`Play`), 표시 파괴·표시 집합 해제 때 파괴한다. 사건 클립 입력은 대여마다 만들고 대여가 끝나는 모든 길(정상 끝·취소·꺼짐·풀 반환·재대여·파괴)에서 뺀다(E10).
- **Animator:** 컨트롤러가 있으면 표시가 켜져 있는 동안 켜 둔다. 없으면 대여 중에만 켠다. 루트 모션·이벤트는 늘 끈다.

## 4. 원칙 점검 요약

4차 상세 평가는 §9, 3차까지의 평가는 §8이다.

| 기준 | 판단(4차) |
|---|---|
| SSOT | 지킴. Idle의 원본은 컨트롤러, 사건 클립의 원본은 SO로 나뉘고 규칙이 문서·검사로 정해진다. 남는 위험은 컨트롤러에 공격 상태를 넣는 실수다(I1) |
| SOLID | 지킴. 채널 하나가 "이 유닛의 Animator 구동"을 맡고, Idle 변형은 에셋만 바뀐다. 대여 계약은 `Evaluate` → `SetTime`으로 뜻을 맞춘다(I2) |
| JMH 지침·FW 계약 | 지킴. 그래프가 사건마다에서 유닛 수명으로 바뀌어 할당이 줄고, `OnEnable`·`OnDisable` 수명에 맞춰 멈추고 돈다. 에디터 전용 검사는 `UNITY_EDITOR` 안에만 둔다(I3) |
| 변경 독립성 | 좋아짐. Idle을 바꾸면 에셋만, 사건 클립을 바꾸면 SO만, A안은 조정자를 건드리지 않는다 |

## 5. 위험과 Unity 확인 항목

- **컨트롤러를 우리 그래프로 옮기는 동작(Unity 확인):** 표시 준비 때 Animator의 컨트롤러 칸을 비우고 `AnimatorControllerPlayable`로 넣었을 때 Idle이 그대로 도는지, 섞개 가중치 0/1 전환이 같은 프레임에 반영되는지 확인한다. 기차 선례(F13)가 컨트롤러 길을 이미 쓴다.
- **게임 시간 그래프와 우리 시각(Unity 확인, A):** 속도 0인 클립 입력에 `SetTime`만 넣고 Unity가 평가할 때, 그 프레임 자세가 넣은 시각과 맞는지 확인한다(메모리 클립 EditMode 시험은 `Evaluate`를 직접 불러 확인).
- **루트 곡선 실수:** Idle이나 사건 클립이 Animator GameObject 자체를 움직이면 A안에서는 되돌릴 수 없다. 에디터 검사와 에셋 시험이 막고, 작업 안내에 적는다.
- **컨트롤러 안 공격 상태(I1):** 컨트롤러에 공격·피격 상태를 넣어도 우리 그래프는 그 상태로 가지 않지만(파라미터를 보내지 않음), "공격 클립이 무엇인가"가 두 곳에 생긴 것처럼 보인다. 작업 안내와 공유 메모에 "컨트롤러는 Idle만"을 적는다.
- **사망 중 Idle:** 사망 반응(코드)이 도는 동안 Idle이 계속 돈다. 어색하면 사망 클립 칸(범위 밖)이나 사망 때 Idle을 멈추는 규칙을 다음에 정한다.
- **바로 갈아 끼움의 끊김:** Idle 중간 자세에서 사건 클립 첫 프레임으로 툭 바뀐다. 보이면 섞는 시간 값 하나를 더한다.
- **같은 유닛의 클립 대여 겹침:** 공격자 자신의 사건은 Gate 규칙 1로 전체 종료(대여 반환) 뒤에 시작하고, 피격 실행은 유닛마다 하나라 겹치지 않는다. 겹치면 표시 규칙 위반으로 예외다.
- **같은 파일을 고치는 작업:** 카메라 K-1~K-6과 실행 대여 묶음 분리(C6)는 커밋됐다. 남은 것은 폴더 정리 M(파일 위치·네임스페이스)과 궤적 TJ-3(`FieldUnitPresenter`)이다. 착수 순서는 계획 §0.3·§0.7이다.

## 6. 사용자 결정

- **결정 완료(3차, 2026-10-19):** Q-AC1~Q-AC8 모두 A(Q-AC3은 사용자 설명으로 정함). Q-AC2 A(우리 재생기가 부름)·Q-AC4 A(끝 = 재생 끝)·Q-AC5 A(포효와 같은 조립)·Q-AC6 A(원거리 이벤트 = 발사)·Q-AC7 A(AC-1~AC-4 지금)·Q-AC8 A(선딜레이는 공격 칸).
- **결정 완료(4차, 2026-10-19, 사용자 설명):** 컨트롤러는 Idle만, 사건 클립은 코드가 위에 얹음, 끝나면 Idle로, 바로 갈아 끼움.
- **결정 완료(4차, 2026-10-19, 사용자 "둘 다 A로 정해"):**
  - **Q-AC9(계획 P1). 이벤트 컴포넌트의 층:** A — 입력 계층(`DataBaseSystem/Data/View/CombatPresentation/`). B(표시 층에 두고 검사는 표시 준비 때)는 고르지 않음.
  - **Q-AC10. 그래프 시간:** A — Unity 게임 시간(조정자 변경 없음, 루트 곡선은 에디터 검사로 막음). B(수동 평가 + 조정자 매 프레임 Tick)는 고르지 않음.

## 7. Phase(계획으로 상세화)

| Phase | 내용 | 4차에서 바뀐 점 |
|---|---|---|
| AC-1 | 이벤트 셋 규칙·커서, 이벤트 컴포넌트(`FindIn`), 이벤트 읽기·검사, 클립 묶음 SO·Data, 외형 표 칸 | 루트 곡선 에디터 검사 추가(Q-AC10 A일 때) |
| AC-2 | 클립 채널(`UnitClipChannel`, **유닛 수명 그래프 + Idle 바닥 + 사건 클립 위층**), 대여 계약(`SetTime`), 재생 단위 | 그래프 수명·모양·Animator 켜기 규칙이 바뀜 |
| AC-3 | 신호 값 `ClipStart`·`ClipEvent`, 클립 조각, 피격 클립 | 신호 장치는 카메라 K-2 것을 씀(일반화 Task 없음) |
| AC-4 | 방향 공유 함수, `ReleaseAll`, `CombatClipAttack.Compose`, 공격 처리기 분기 | 없음 |
| AC-5 | 원거리 발사 조각(첫 원거리 클립 뒤) | 없음 |

- 실제 클립·Idle이 들어온 뒤 확인할 것(완료 조건 아님): Idle 재생, Idle ↔ 공격 클립 전환, 실제 FBX 이벤트 읽기, 리그 일치, 루트 곡선 검사가 실제 에셋에서 통과하는지.

## 8. 3차까지의 원칙 평가(요약)

- 3차 결정 뒤 평가 E1~E15와 계획 때 찾은 E16, 추천안 평가 G1~G6의 상세는 [UML 아티팩트](https://claude.ai/artifact/DysLcFNSLmR4okBskHTYdF) 7장과 계획 §0.4에 있다. 여기에는 결론만 둔다.

| # | 내용 | 상태(4차 기준) |
|---|---|---|
| E1·G1 | `Event`(클립 큐)와 `Hit`(맞는 순간)은 다른 신호, 신호 시간 규칙은 BP2 보충 한 곳 | 반영. 장치는 카메라 K-2가 BP2 보충대로 구현 |
| E2·E3·E13 | 방향 공유 함수, 클립 공격 조립 정적 함수 하나, 두 길 공통 규칙 표 | AC7에 반영 |
| E5·E6 | 채널 구현 분리, 클립 값·공격 칸 값 분리 | AC1·AC3에 반영 |
| E10 | 그래프는 모든 길에서 정리 | AC10으로 바뀜(그래프는 유닛 수명, 사건 클립 입력이 대여마다) |
| E16·Q-AC9 | DB → View 참조가 생기는 문제 | 결정 A(컴포넌트를 입력 계층에) |
| G2·G3·G5 | `FindIn` 하나, 클립 값은 세 시각만, DB 폴더 MonoBehaviour 범위 | 반영 |
| G4·G6 | 조각 계약 문서, 카메라와 같은 파일 순서 | 카메라 K-2가 처리(F3) |
| O1~O4 | 대여 배관 반복, 실행 채널 분기, 공격 재생 비대화, 재생 단위 소비자 하나 | 관찰 유지 |

## 9. 4차 원칙 평가(2026-10-19)

기준은 [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)다. Q-AC9·Q-AC10은 결정(A) 기준이다.

### 9.1 SSOT

| # | 정도 | 찾은 것 | 대응 |
|---|---|---|---|
| I1 | 중간 | Idle(컨트롤러)과 사건 클립(SO)이 다른 곳에 있다. 둘은 다른 사실(평상시 / 사건)이라 원본이 둘인 것은 맞지만, 누가 컨트롤러에 공격 상태를 넣으면 "공격 동작"이 두 곳에 생긴 것처럼 보인다 | 규칙을 AC9 한 곳에 적고, 작업 안내·공유 메모에 같은 문장을 가리킨다. 컨트롤러 안 상태는 런타임에 검사하지 않는다(에디터 API가 필요하고 이득이 작음) |
| 확인 | — | "우리 그래프가 맡는 유닛"은 `FindIn` 결과 하나로 정해진다. Idle 시간은 Unity, 사건 클립 시각은 우리 시계로 주인이 하나씩이다. 루트 곡선 규칙은 에디터 검사 한 곳이다 | — |

### 9.2 SOLID

| # | 정도 | 찾은 것 | 대응 |
|---|---|---|---|
| I2 | 낮음(L) | 3차 대여 계약의 `Evaluate(t)`는 "그 시각 자세를 지금 그린다"는 뜻이었는데, A안에서는 시각만 정하고 평가는 Unity가 한다. 이름과 보장이 어긋난다 | `SetTime(t)`로 이름을 바꾸고 문서에 "평가는 그래프 주인이 한다"를 적는다(AC3) |
| 확인 | — | S: `UnitClipChannel` 하나가 "이 유닛의 Animator 구동"(바닥·위층·수명)을 맡는다. O: Idle 변형·추가는 컨트롤러 에셋만, 사건 클립은 SO만 바뀐다. D: 컨트롤러는 프리팹 Animator에서 읽어 SO가 컨트롤러를 알 필요가 없다 | — |

### 9.3 JMH 지침·FW 계약

| # | 정도 | 찾은 것 | 대응 |
|---|---|---|---|
| I3 | 낮음 | 루트 곡선 검사에 쓰는 `AnimationUtility`는 에디터 API다. 런타임 어셈블리(`ProjectL.Systems`)에서 쓰려면 `UNITY_EDITOR` 안에만 둬야 하고, 플레이어 빌드에서는 검사가 빠진다 | 검사 함수 하나를 `UNITY_EDITOR`로 감싸고, 실제 카탈로그 에셋 시험(Unity)이 같은 함수를 부른다. 플레이어 빌드는 에디터에서 통과한 에셋만 쓴다 |
| 확인 | — | §19: 그래프 할당이 사건마다에서 유닛 수명(풀 생성 때)으로 줄고, Tick 경로 할당이 없다. §20·21: 그래프는 표시의 `OnEnable`·`OnDisable`·파괴에 맞춰 돌고 멈추고 파괴된다(구독 없음). §25: `FindIn`은 자기 프리팹 안에서 한 번. FW-001·002: 새 Service·Key·AddModule·Factory 변경 없음. A안은 조정자(`CombatViewSystem`)를 고치지 않는다 | — |

### 9.4 기능 변경의 독립성

| 시나리오 | 고칠 곳 | 3차와 비교 |
|---|---|---|
| Idle 동작 바꾸기·변형 추가 | 컨트롤러·Idle 클립(에셋) | 3차에는 Idle이 없었다 |
| 유닛마다 다른 Idle | Override Controller(에셋) | 같음 |
| 공격 클립·이벤트 위치·선딜레이 | 클립 묶음 SO·클립(에셋) | 같음 |
| Idle ↔ 사건 클립을 섞기 | `UnitClipChannel` + 섞는 시간 값 하나 | 새로 생긴 확장 자리 |
| 사망 중 Idle 멈춤 | 사망 처리기 조립 한 줄 또는 채널 규칙 | 새로 생긴 확장 자리 |
| 그래프 시간 주인을 바꾸기(A ↔ B) | 채널 + (B면) 조정자 Tick 한 줄과 표시 계약 | A는 조정자를 건드리지 않는다 |

남는 공통 경계: 조각 신호 열거형과 실행의 신호 처리(카메라와 공유), 조립 채널 열거형과 실행 대여 묶음의 채널 칸, 출처 수명 창구(해제 입구), 공격 처리기의 두 길 분기, 방향 공유 함수, 이벤트 셋 규칙, 그리고 4차의 **클립 채널 그래프 모양(바닥·위층)**이다. 그래프 모양을 바꾸면 클립을 쓰는 모든 유닛의 Idle과 사건 클립에 닿는다.

## 10. 5차 — 원거리(곡사) 클립 공격(AC-5) (2026-10-19, 설계 초안, 코드 변경 없음)

사용자 요청: "AC-5까지 작업하자" → "원거리 공격 Enemy의 CannonEnemy이 곡사형태의 원거리 공격 클립이야". 처음 원거리 클립이 들어와 AC-5("첫 원거리 클립 뒤")를 설계한다. AC-1~AC-4는 Unity 확인 완료(계획 §8.5).

### 10.1 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F19 | `Models/Enemy/CannonEnemy.fbx`(민형님 `cad28dba`)의 테이크는 적 넷이 함께 가진 9개이고 곡사 적 몫은 `Scull|CannonIdle`·`Scull|CannonAttack`·`Scull|CannonAttackBody`다. 모두 아마추어 `Scull` 아래(뼈대)를 움직이고 Animator 루트 경로 곡선은 없다(루트 곡선 검사 통과 예상). 최상위에 메시 `Cylinder002`·`구체`가 있다 | FbxDump(스크래치 `fbx/FbxDump.cs`) |
| F20 | 가져오기 설정은 기본값(`clipAnimations: []`)이라 **클립 이벤트가 아직 없다.** 이 FBX를 쓰는 프리팹·에셋도 없다 | `CannonEnemy.fbx.meta`, GUID 검색 |
| F21 | 곡사 적 유닛은 `Unit_UndeadCannonCart`(키 25)로 보인다: 벽 무시 전달(곡사)·단일 칸 지정·주변 피해. 하지만 **Core 유닛 표(`UnitDataTable`)에 없고 View 외형 표에도 항목이 없어 지금은 전투에 나올 수 없다**(`Unit_DynamiteHammer`도 같음). **AC-5c에서 확인:** 이것은 의도된 상태다 — [Core 인계](../handoffs/2026-10-16-undead-cannon-cart-view-handoff.md)가 표 등록·외형 항목(표시 이름 `언데드 마인카트`, EnemyOnly)·적 배치를 View 작업자 몫으로 넘겼다. 그 문서는 벽 정책을 Cactus와 같은 `RequireClear`라고 적지만 지금 두 SO의 저장값은 `wallDelivery: 2`(`IgnoreWalls`)다(Core 영역, 보고만) | `InGameGroup/Unit/Item/Unit_UndeadCannonCart.asset`·`TargetingMethod_UndeadCannonCart`, GUID 참조 0 |
| F22 | 곡사 코드 길은 아군 CactusCannon 전용이다. 무기 View `CatusCannonWeaponView`(버킷 준비·복귀, 총구 위치)가 없으면 예외이고, 몸 반동·버킷 값은 공격 설정 SO에 있다. 공격 설정 SO의 곡사 값 중 투사체 프리팹·비행 시간·곡사 높이·회전만 투사체 자체의 값이다 | `CombatAttackPlayback.TryBeginMortar`, `CombatMortarAnimation`, `CombatAttackPresentationSO` |
| F23 | **투사체 진행(풀 Tick)은 공격 재생이 코드 모션(`CombatAttackPresenter`)을 돌릴 때만 한다.** 조각 실행에서 쏜 투사체는 지금 구조로는 진행되지 않는다. 풀은 프리팹마다 하나를 여러 공격이 함께 쓴다 | `CombatAttackPlayback.Tick`(`!_attack.IsPlaying`이면 풀 Tick 전에 돌아감) |
| F24 | 직선·Ballista의 접촉 진행률(몸 조회·대체 경고)은 공격 재생 안에 있다. 곡사는 착탄 = 히트라 몸 조회가 없다 | `LinearContactProgress`·`BallistaContactProgress`, `CombatMortarAnimation.Fire` |
| F25 | **사용자 확인(2026-10-19):** `CannonEnemy` = `Unit_UndeadCannonCart`가 맞다. 공격 테이크는 "일단은 `CannonAttack` 클립 2개 모두 대응됨"(`CannonAttack`·`CannonAttackBody` 어느 쪽을 공격 칸에 넣어도 됨). `구체` 메시는 포탄이 아니다(풀 투사체와 겹치지 않음) | 사용자 답 |
| F26 | 직선 코드 길은 출발점 = 올린 유닛 위치 + 0.2 앞(`CombatShotPath.LinearStart`), 종점 = 목표점을 발사 높이만큼 올린 점, 주 대상 몸 접촉 진행률에서 히트, 도착에서 끝(접촉이 없었으면 도착 때 히트). 조준 궤적 미리보기도 같은 출발점 규칙을 쓴다(궤적 평가 V8) | `CombatLinearAttackAnimation.Advance`, `CombatAttackTrajectory` |

### 10.2 결정안(권장)

**AC11. 범위 — 곡사 + 직선(Q-AC13 B, 사용자 결정).** 곡사(실제 클립 있음)와 직선(같은 발사 조각에 접촉 진행률만 다름)을 한다. Ballista는 무기 View의 시위·재장전 연출과 얽혀 제외한다. 검사 규칙 `CombatUnitAnimationRules.CanUseAttackClip`이 Melee·Linear·Mortar를 허용한다(한 함수만 고침, P5). 직선 클립은 아직 없어 메모리 클립 시험으로만 확인한다.

> **임시 예외(2026-10-20, 사용자 지시 "시위·장전 화살·재장전은 무시하고 일반 공격처럼, 관통 대상별 피격은 유지").** Ballista도 공격 칸을 받고 원거리로 친다(`FiresProjectile`에 Ballista, 포구 필수·피격 선딜레이 0 검사가 같이 걸림). 출처 발사 `CombatAttackPlayback.FireShot`의 Ballista 칸(`FirePiercingShot`)은 코드 연출과 같은 규칙으로 쏜다: 목표 = 관통 종점(`CombatShotPath.TryTarget`), 대상마다 몸 접촉 진행률(`BallistaContactProgress`), 그 순간에 그 대상의 접촉 이펙트 → 그 대상만 해제(`CombatHitGate.Release`). 맞음 신호(`Hit`)는 첫 해제 뒤 한 번(대상이 없으면 도착 때). 투사체 진행은 맞는 순간 여러 개를 받는 `CombatProjectileFlights.FireThrough`(직선 `FireLinear`·곡사 `FireArc`는 순간 하나짜리로 같은 `Shot`을 씀), 처리기는 Ballista면 발사 조각의 "맞으면 모두 해제"를 비운다(`CombatAttackEventHandler.ReleaseNone`). **코드 길과 다른 점:** `BallistaWeaponView`의 시위·장전 화살·재장전이 움직이지 않음(턴 끝 재장전도 없음), 반동은 클립이 맡음, 사건 완료가 발사(Fired)가 아니라 첫 해제(FirstRelease). 클립이 없는 Ballista는 코드 연출 그대로. 공격 궤적 미리보기(`CombatAttackTrajectory.TryResolveShot`)는 무기 표시가 있으면 그 총구, 없고 공격 클립이 있으면 유닛 위치를 발사 높이로 올린 점에서 시작한다(실제 클립 발사는 `CombatAnimationEvents.muzzle`에서 나가 출발점이 조금 다를 수 있음, 둘 다 없으면 예외 그대로). 무기 연출과 클립을 함께 쓰는 설계는 나중(시위·화살을 누가 움직일지, 발사·재장전 이벤트).

**AC12. 발사 조각과 투사체 진행 주인 — 공격 재생에 "출처 발사" 입구(Q-AC14 A).**
- 공격 재생(`CombatAttackPlayback`)에 출처 발사 입구 하나(`FireShot(공격 사건, 공격 설정, 발사 지점, Mapper, 맞음, 도착, 세대)`)를 둔다. 종류로 갈린다:
  - **곡사:** 목표점 = `CombatShotPath.TryTarget(Mortar, …)`(주 칸 중심), `FireArc`(비행 시간·곡사 높이·회전), 착탄 = 맞음 → 도착.
  - **직선:** 목표점 = `TryTarget(Linear, …)`를 발사 높이만큼 올린 점, `FireLinear`(속도), 맞음 = 주 대상 몸 접촉 진행률(코드 길과 같은 `LinearContactProgress`·대체 경고), 도착에서 끝(그때까지 맞음이 없었으면 도착 때 맞음 먼저).
  - 투사체 풀(프리팹별 공유)·Tick·정리는 지금처럼 공격 재생 한 곳이고, Tick 조건이 "코드 모션 진행 중 **또는** 출처 발사 진행 중"으로 넓어진다. 콜백은 세대가 같을 때만 부른다. 취소(`Cancel`)·해제(`Release`)는 지금처럼 모든 투사체를 회수한다. 출처 발사만 있을 때 Tick은 다른 진행을 막지 않는다(막기는 조각 실행이 맡음).
- 발사 조각 `CombatFirePiece`(외부 진행 조각): 시작에서 발사 지점을 읽어 입구로 쏘고, 맞음 콜백에서 **대상 모두 해제 → `Hit`**, 도착 콜백에서 끝(해제 조각과 같은 순서 규칙, `CombatPieceSignal.Hit` 문서). 조각 취소 때는 그 투사체만 회수한다.
- 다른 안: B 조각이 자기 풀·Tick을 가짐 — 풀이 두 주인이 되고 같은 프리팹을 두 번 Tick할 위험이 있어 고르지 않음.

**AC13. 발사 지점 — 이벤트 컴포넌트의 선택 칸(Q-AC15 A).** `CombatAnimationEvents`에 선택 칸 `muzzle`(Transform, 모델 안 포구 뼈·빈 GameObject)을 둔다. 원거리 공격 클립을 쓰는 유닛이면 DB 검사가 필수로 본다(규칙 원본 `CombatUnitAnimationRules`). 표시 계약은 "지금 발사 지점 월드 위치" 읽기 하나를 더한다(클립 채널과 같은 컴포넌트에서 찾음). 발사 순간의 포구 위치는 그 프레임 Unity 평가 전 값(앞 프레임 자세)이다 — 한 프레임 차이는 받아들인다.
- 다른 안: B 유닛 위치 + 발사 높이(직선과 같은 `Raise`) — 작업 입력이 없지만 포탄이 포신이 아닌 몸 가운데서 나온다. C 따로 컴포넌트 — 붙일 것이 늘어 고르지 않음. DB 층 MonoBehaviour 범위(평가 G5 "프리팹에 붙는 작업 입력의 이름표·자리 표시")에 들어간다.

**AC14. 원거리의 시점 — 발사 = 이벤트 타이밍(Q-AC6 A 그대로, Q-AC16 A).** 원거리 클립은 이벤트 타이밍에 바로 쏜다(설계 Q-AC6 A "원거리 이벤트 = 발사"). 맞는 순간은 착탄이다. 공격 칸의 **피격 선딜레이는 원거리에서 쓰지 않으므로 0만 허용**한다(DB 검사, 뜻이 둘이 되지 않게). 소리 선딜레이는 근접과 같다. 쏘는 순간을 늦추고 싶으면 이벤트 위치를 옮긴다.
- 다른 안: B 피격 선딜레이 칸을 원거리에서 "발사 선딜레이"로 읽음 — 칸 하나에 뜻이 둘. C 발사 선딜레이 칸을 새로 둠 — 이벤트 위치로 같은 일을 할 수 있어 값이 겹침(P7).

**AC15. 원거리 클립 조립.** `CombatClipAttack.Compose`가 공격 연출 종류로 갈린다. 근접은 AC7 그대로, 곡사·직선은 항목 2가 발사 조각(`After(1, ClipEvent)`)이다. 출처·완료(첫 해제 = 곡사 착탄·직선 몸 접촉)·대여(자세·클립)·방향(`CombatAttackFacing.For(종류, …)`)·소리 조각은 같다. 전체 종료는 클립 끝과 투사체 도착이 모두 끝난 뒤다(발사 조각이 도착 때 끝남). 클립 공격은 무기 View(`CatusCannonWeaponView`)가 필요 없고, 공격 설정 SO의 버킷·반동·발사 비율 값은 읽지 않는다(투사체 값만 읽음).

| # | 항목 | 시점 | 하는 일 |
|---|---|---|---|
| 0 | 방향 조각 | 시작 | 목표 쪽을 봄 |
| 1 | 클립 조각 | 시작 | 공격 클립 |
| 2 | 발사 조각 | `After(1, ClipEvent)` | 포구에서 쏨 → 맞을 때(곡사 착탄·직선 몸 접촉) 대상 모두 해제 → `Hit`, 도착 때 끝 |
| 3 | 소리 조각 | `After(1, ClipEvent, 소리 선딜레이)` | 공격 소리 |

- **관찰(낮음):** 조준 궤적 미리보기는 코드 길의 출발점(올린 유닛 위치 + 0.2)을 쓰므로, 클립 공격자의 실제 포구 출발점과 조금 다르다(궤적 평가 V8과 같은 종류). 이번에는 고치지 않는다.

### 10.3 에셋 쪽(코드 밖) — 정할 것·요청

AC-5 코드는 메모리 클립 시험으로 끝낼 수 있지만, 실제 화면으로 보려면 아래가 필요하다. 각각 다른 작업자 영역이 섞여 있다.

| 할 일 | 영역 | 비고 |
|---|---|---|
| (사실 확인) `CannonEnemy` = `Unit_UndeadCannonCart`인지, 공격 테이크가 `CannonAttack`·`CannonAttackBody` 중 무엇인지, `구체` 메시가 포탄인지(클립이 포탄을 날리면 풀 투사체와 겹침) | 사용자·민형님 | **확인됨(F25)** |
| FBX 가져오기 설정에서 공격 클립에 이벤트 셋(`ClipStart`·`ClipEvent`·`ClipEnd`) 찍기 | 민형님(모델 작업자) 또는 사용자 | 클립마다 하나씩, 이름은 `CombatAnimationEvents`의 함수 이름 |
| 프리팹: FBX + `CombatAnimationEvents`(Animator와 같은 GameObject) + `muzzle` 지정 + Idle 컨트롤러(`CannonIdle`만) | 민형님(프리팹) | |
| View DB: 외형 표 항목(`UnitVisual_UndeadCannonCart`), 공격 설정 SO(곡사), 클립 묶음 SO(`attackClip` = 공격 테이크, 피격 선딜레이 0, 소리 선딜레이) | 호진님(외형 표) | 공유 메모 |
| Core 유닛 표 등록·스테이지 배치 | Core(천명님) | 데이터 경계 전달 문서에 요청으로 |

### 10.4 원칙 점검(권장안)

| 기준 | 판단 | 남는 것 |
|---|---|---|
| SSOT | 투사체 풀·Tick·회수는 공격 재생 한 곳, 목표점은 `CombatShotPath.TryTarget`, 해제는 `ReleaseAll`, `Hit` 순서 규칙은 신호 문서, 검사 규칙은 `CombatUnitAnimationRules` 한 곳. 피격 선딜레이 칸은 원거리에서 0만 허용해 뜻이 하나 | 곡사 투사체 값(비행 시간·높이·회전)은 코드 길·클립 길이 같은 SO를 읽음(원본 하나) |
| SOLID | 발사 조각은 "쏘고 착탄을 기다림"만, 투사체 진행은 공격 재생, 조립은 종류 분기 한 곳. 공격 재생에 입구 하나가 더해져 그 클래스(514줄) 책임이 조금 늘어남 | 공격 재생 비대화(설계 E7 관찰)가 커짐 — 투사체 부분을 따로 뺄 시점이 가까워짐 |
| FW 지침·계약 | Factory·ServiceKeys 무변경, Tick 할당 0(발사 때 대여 하나), DB 층 MonoBehaviour 범위(G5) 안의 칸 하나 | — |
| 변경 독립성 | 직선 클립이 오면 발사 조각에 접촉 진행률 입구 하나, 다른 곡사 적은 에셋만. 근접 클립 길은 그대로 | 조립 함수의 종류 분기가 늘수록 조립이 길어짐(종류가 셋 이상이면 종류별 조립 함수로 나눔) |
| **예상** | **약 8.8/10**(직선 포함 결정 뒤 약 8.7 — 입구의 종류 분기 둘·직선 접촉 경로 하나가 더해짐) | 공격 재생 비대화가 가장 큰 감점 |

### 10.5 사용자 결정 — 결정 완료(2026-10-19)

사용자: "Q-AC13 : 직선형 포함 / 나머지는 추천안대로 / Q-AC17 : 1. 맞음. 2. 일단은 CannonAttack 클립 2개 모두 대응됨 3. 구체 메시 포탄 아님."

- **Q-AC13. 범위: B — 곡사 + 직선**(권장은 A 곡사만이었음). Ballista 제외.
- **Q-AC14. 투사체 진행 주인: A** — 공격 재생에 출처 발사 입구.
- **Q-AC15. 발사 지점: A** — 이벤트 컴포넌트의 선택 칸 `muzzle`.
- **Q-AC16. 원거리 시점: A** — 이벤트 타이밍에 바로 발사, 피격 선딜레이는 원거리에서 0만.
- **Q-AC17. 에셋: A** — 코드만, 에셋은 공유 메모·Core 요청. 사실 확인 셋은 F25.

### 10.6 Phase(결정 뒤 계획으로 상세화)

| Phase | 내용 |
|---|---|
| AC-5a | 발사 지점 칸(`muzzle`)·검사 규칙(Melee·Mortar 허용, 원거리 `muzzle` 필수·피격 선딜레이 0)·표시 계약의 발사 지점 읽기 |
| AC-5b | 공격 재생 출처 발사 입구·Tick 조건, 발사 조각, 조립 종류 분기, 신호 문서 |
| AC-5c | 시험(메모리 클립 곡사 조립·장면), 공유 메모(민형님·호진님)·Core 요청 |

## 11. 애니메이션 연출 구조 검사 — 7기준 (2026-10-19, 코드 변경 없음)

사용자 요청: "지금 계획한 AC-5 계획 포함해서 SSOT / SOLID 5원칙 / JMH Framework 개발 지침·FW 계약 / 기능 변경의 독립성 / 캡슐화 / C# 및 유니티 권장 구조 / 무의미한 If문 혹은 Tick 검사 남발 여부로 지금 애니메이션 연출 구조 검사". 대상은 AC-1~AC-4 구현(Unity 확인 완료)과 AC-5 계획(계획 §9). 하위 에이전트 없이 코드를 직접 읽어 판단했다.

### 11.1 점수

| 기준 | 점수 | 근거 요약 |
|---|---|---|
| SSOT | 8 | 시각 규칙·이벤트 이름·컴포넌트 규칙·대여 수명·해제 입구·방향 계산·목표점이 각 한 곳. 감점: 곡사 코드 모션의 방향 계산이 공유 함수 밖(K1), 대여 시작 검사 8줄 반복(K5), 선딜레이·시각 검사가 층마다 반복(K8·K9) |
| SOLID | 7.5 | 채널(그래프)·대여(수명)·재생 단위(시각·이벤트)·조각·조립·처리기가 나뉨. 감점: 공격 재생(514줄)에 AC-5 출처 발사가 더해짐(K2), `FieldUnitView` 576줄(K6), 조립 함수의 종류별 선택 인자(K3), 클립 대여에 포구 읽기(K4) |
| JMH 지침·FW 계약 | 9 | Factory·ServiceKeys·AddModule 무변경, 프레임 경로 할당 0, 그래프 수명이 `OnEnable`·`OnDisable`·파괴·풀 반환과 맞음, DB 층 MonoBehaviour는 범위(G5) 안. 감점: 공격·반응 연출 값은 사건마다 새로 만듦(K10, 기존 동작) |
| 기능 변경의 독립성 | 8.5 | 클립이 없으면 모든 길이 그대로, 새 클립 종류는 에셋만, 새 신호·채널은 열거 값 + 한 칸. 감점: 새 유닛 채널 하나에 닿는 곳이 일곱(K7) |
| 캡슐화 | 8 | 대여는 계약으로만 밖에 나가고, 채널·그래프는 표시 안에 숨음. 감점: 시험용 내부 읽기가 그래프 구조체까지 내줌(K11), 대여 중첩 클래스가 표시의 private 상태를 직접 만짐(K12, 받아들임) |
| C#·Unity 권장 구조 | 8.5 | `IDisposable` 그래프, `RequireComponent`·`DisallowMultipleComponent`, 속도 0 + `SetTime`의 Playable 수동 시각, `sealed`·불변 Data. 감점: EditMode 시험이 수명 함수를 리플렉션으로 부름(K13, 기존 방식), 오프스크린도 평가(`AlwaysAnimate`, K14) |
| 무의미한 If·Tick 검사 | 8.5 | 매 프레임 폴링 없음(그래프는 Unity가 평가, 연출 Tick은 결과 재생 중에만). 감점: 클립 하나의 프레임 경로에서 시각 검사가 세 층(재생 단위·채널·커서), 쓰이지 않는 분기 하나(K8·K15) |
| **종합** | **8.3** | 가장 큰 감점은 공격 재생 비대화(K2)와 곡사 방향 계산 두 벌(K1) — 둘 다 AC-5가 바로 건드리는 곳 |

### 11.2 발견 사항

| # | 등급 | 기준 | 내용 | 권장 |
|---|---|---|---|---|
| K1 | 중간 | SSOT | 곡사 코드 모션(`CombatMortarAnimation.Begin`)은 방향을 직접 계산한다(빈 방향 = 유닛 지금 방향, 위쪽 정규화 안 함). AC-4의 공유 함수 `CombatAttackFacing`은 근접·직선·Ballista(기반 클래스)만 덮는다. AC-5가 곡사 클립을 `For(Mortar, …)`(빈 방향 = 공격 방향)로 돌리면 **같은 곡사에 방향 규칙이 둘**이 된다. 빈 방향 규칙도 종류마다 다르다(근접·직선 = 공격 방향, Ballista = 고정 R0, 곡사 = 지금 방향) | AC-5에 넣음: 곡사 코드 모션도 `Toward`를 부르고, 곡사의 빈 방향 규칙을 하나로 정함(결정 필요, §11.3 Q-AC18) |
| K2 | 중간 | SOLID(SRP) | `CombatAttackPlayback`(514줄)이 Gate 수명·코드 모션 넷·투사체 풀·Ballista 무기·재장전·접촉 진행률·경고를 모두 갖는다. AC-5 계획 T29는 여기에 출처 발사 기록과 Tick 조건 분기를 더한다(설계 E7 관찰이 커짐) | AC-5에 넣음: 투사체 진행을 `CombatProjectileFlights`(풀 사전·Tick·회수·출처 발사 기록)로 먼저 떼고(동작 동일), 출처 발사는 그쪽에 둔다. 공격 재생은 Gate·코드 모션만. 결정 필요(Q-AC19) |
| K3 | 낮음 | SOLID(OCP·ISP) | AC-5 계획의 `Compose(…, kind, …, fire, …)`는 근접이면 `fire`가 null이고 원거리면 필수다(인자의 뜻이 다른 인자에 달림) | 입구를 둘로: `ComposeMelee`·`ComposeRanged`(공통 앞부분은 private 함수 하나). 계획 글만 고침 |
| K4 | 낮음 | SOLID(응집) | AC-5 계획 P9는 포구 읽기를 클립 대여(`ICombatUnitClip`)에 둔다. 대여의 뜻(클립 재생)과 모델 사실 읽기가 섞인다 | 받아들임 권장(닿는 곳이 가장 적음). 계약 doc에 "같은 컴포넌트의 자리 표시를 읽는다"를 적음 |
| K5 | 낮음 | SSOT·If | `FieldUnitView`의 대여 시작 넷이 같은 검사 둘(이전 대여 남음·활성 대여)을 8줄로 반복하고, 앞의 것은 대여 칸 `Hold`가 다시 검사한다. 대여 넷의 `IsAvailable`도 같은 꼴 | 시작 검사 함수 하나(`RequireActiveRental(what)`), "이전 대여" 검사는 칸이 문구를 받아 한 번만. 예외 문구가 바뀌므로 문구 단정 시험 확인 뒤 |
| K6 | 낮음 | SOLID(SRP) | `FieldUnitView` 576줄: HUD·체력 글자·외형 층·대여 다섯·클립 채널 | 대여 넷과 채널을 "표시 대여 묶음"으로 뺄 수 있음. 지금은 기록만(폴더 정리 M 뒤가 자연스러움) |
| K7 | 낮음 | 독립성 | 새 유닛 채널 하나에 열거 값·대여 묶음 칸·맥락·창구·표시 계약·표시·시험 가짜 넷이 닿는다 | 받아들임(경로가 한 모양이라 빠뜨리면 컴파일 오류로 드러남) |
| K8 | 낮음 | If·Tick | 클립 하나의 프레임 경로: 재생 단위가 시각을 [0, 끝]으로 맞춘 뒤 채널 `SetTime`이 유한·음수를 다시 보고, 커서 `TryNext`가 NaN을 다시 본다. 대여 `SetTime`도 매번 가용성을 본다 | 계약 경계(채널·대여)의 검사는 남기고, 같은 클래스 안 중복만 줄임 — 비용이 작아 우선순위 낮음 |
| K9 | 낮음 | SSOT·If | 클립 묶음 SO가 선딜레이 규칙을 검사한 뒤 같은 규칙을 `CombatClipAttackData` 생성자가 다시 검사한다(문구에 에셋 이름을 붙이려는 것) | 생성자 예외를 잡아 문구를 감싸는 한 길로(SO의 `ReadClip`과 같은 방식) |
| K10 | 낮음 | FW | 공격·반응 연출 값(`AttackSettingsOf`·`ReactionSettingsOf`)은 사건마다 `CreateData`로 새로 만든다. 클립 묶음만 외형별로 보관한다(AC-2). 클립 공격 길은 공격 설정을 한 번 더 읽는다 | 같은 보관 방식으로 맞추는 것은 별도 작업(기존 동작 변경) |
| K11 | 낮음 | 캡슐화 | 시험용 읽기 `UnitClipChannel.Graph`가 `PlayableGraph`를 그대로 내줘 밖에서 파괴할 수 있다. `FieldUnitView.ClipChannel`·`CurrentPresentationPose`도 내부 객체를 준다 | 시험용 평가 함수 하나(`EvaluateForTests(dt)`)와 상태 읽기만 내주기 |
| K12 | 수용 | 캡슐화 | 대여 중첩 클래스가 표시의 private 필드(칸·채널·외형)를 직접 쓴다 | C# 중첩 클래스의 정상 쓰임, 대여 규칙이 표시 안에 갇혀 있어 받아들임 |
| K13 | 수용 | C#·Unity | EditMode 시험이 `OnEnable`·`OnDisable`을 리플렉션으로 부른다(`ExecuteAlways`가 아니라 EditMode에서 불리지 않음) | 기존 시험 방식과 같음(구조 원칙 평가 R8 범위) |
| K14 | 수용 | C#·Unity | 클립 채널은 `AlwaysAnimate`라 화면 밖 유닛도 평가된다 | 전투 유닛 수가 적어 받아들임. 많아지면 `CullUpdateTransforms` 검토 |
| K15 | 낮음 | If | `CombatClipPlayback.Begin`의 "시작 전 취소" 분기는 조각 쓰임새에서 생기지 않는다 | 지워도 되나 대여 계약 문서와 같이 판단 — 보류 |
| K16 | 낮음 | SSOT | "해제 뒤 `Hit`" 순서가 세 곳에서 구현된다(정면 공격 모션 람다·해제 조각·계획된 발사 조각). 규칙 문장은 신호 문서 한 곳 | 작은 함수 하나(`CombatHitMoment.ReleaseThenSignal(release, hit)`)를 셋이 부름. AC-5에 같이 넣을 수 있음 |
| K17 | 낮음 | SSOT | 발사 지점의 원본이 외형 종류마다 다르다: 곡사 코드 = `CatusCannonWeaponView.MuzzlePosition`, Ballista = 무기 View, 클립 = `CombatAnimationEvents.muzzle` | 종류가 다른 사실이라 받아들임. 설계 AC8 값의 자리 표에 "발사 지점" 줄을 더함 |
| K18 | 낮음 | 독립성 | 조준 궤적 미리보기의 출발점은 코드 길 규칙이라 클립 공격자의 포구와 다르다(궤적 평가 V8과 같은 종류) | 기록만(§10.2 AC15 관찰) |

### 11.3 AC-5 착수 전 정할 것(새 결정)

- **결정 완료(2026-10-19, 사용자 "Q-AC18·Q-AC19 모두 A로 하고 K3·K16·K17도 AC-5에 넣어. 계획 고친 뒤 AC-5a부터 진행해."):** Q-AC18 A, Q-AC19 A, K3·K16·K17은 AC-5에 넣음(계획 §9.0b). 계획 P9~P13도 그대로.
- **Q-AC18. 곡사 방향(K1):** **A(권장) 곡사 코드 모션도 `CombatAttackFacing.Toward`를 부르고, 빈 방향은 공격 방향으로 통일**(곡사는 주 칸이 있어야 쏘므로 빈 방향이 실제로 쓰일 일은 거의 없음 — 화면 변화 없음) / B 곡사 빈 방향만 "지금 방향"으로 남기고 계산만 공유 / C 그대로 둠(두 벌).
- **Q-AC19. 투사체 진행 분리(K2):** **A(권장) AC-5 첫 Task로 `CombatProjectileFlights` 분리(동작 동일, 기존 공격 시험으로 확인) 뒤 출처 발사를 그쪽에** / B 계획대로 공격 재생에 바로 더함(분리는 나중).
- 낮음 등급 중 AC-5에 같이 넣기 권장: K3(조립 입구 둘), K16(해제 뒤 `Hit` 함수 하나), K17(값의 자리 표 한 줄). 나머지(K5·K6·K8·K9·K10·K11·K15)는 기록만 하고 별도 정리 작업으로 둔다.
- 권장안대로 하면 예상: SSOT 9 · SOLID 8.5 · FW 9 · 독립성 8.5 · 캡슐화 8 · C#·Unity 8.5 · If·Tick 8.5 = **약 8.6**.

## 12. 6차 — 부속 클립(무기 클립을 몸 클립과 같이 재생) (2026-10-19, 설계 초안, 코드 변경 없음)

사용자: "지금 CannonEnemy.fbx를 보면 CannonBone|CannonAttackBody 라는 무기 전용 애니메이션 clip이 생겼는데 이걸 몸체 애니와 같이 실행해야하는데 대응됨?" → (지금은 안 됨, A 한 테이크 요청 유지 / B 부속 클립 지원 제시) → **"B로 하자. 부속 클립 지원 설계부터 고쳐서 보여줘."** 이로써 계획 §0.8의 **Q-AC12 A(무기는 한 테이크로) → B(부속 클립 칸)** 로 바뀐다.

### 12.1 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F27 | 민형님 재내보내기(`13eb1dbc`)로 `CannonEnemy.fbx`에 포신 아마추어 `CannonBone`과 테이크 `CannonBone|CannonAttackBody`(fileID `4125226451315635580`)가 생겼다. 몸 테이크는 아마추어 `Scull` 쪽이다(`Scull|CannonAttack` 등). 기존 테이크 fileID는 그대로다(클립 묶음 SO 참조 유효) | FBX 테이크 이름, Library 가져오기 결과 |
| F28 | Blender FBX 내보내기는 기본으로 **아마추어마다 테이크를 따로** 만든다(`아마추어|동작`). 그래서 `Scull|CannonAttackBody`처럼 다른 아마추어용 동작이 몸 아마추어에도 따라 나온 테이크가 섞일 수 있다(F25의 "둘 다 가능"은 이 사실로 다시 봐야 함 — 몸 = `Scull|CannonAttack`, 무기 = `CannonBone|CannonAttackBody`). 계획 §0.8 X2에 적은 "NLA 트랙 이름을 맞추면 한 테이크"는 **확인하지 않은 내용**이었다 | 테이크 목록, 계획 §0.8 |
| F29 | 지금 코드는 사건마다 클립 **하나**다: 값 `CombatClipData.Clip` 하나, 대여 `ICombatUnitClip.Play(AnimationClip)`, 채널 위층 `AnimationClipPlayable` 하나(섞개 입력 1). 클립이 흘러가는 길은 값 → 재생 단위(`CombatClipPlayback.Begin`이 `_lease.Play(_data.Clip)`) → 대여(`FieldUnitView.PresentationUnitClip`) → 채널(`UnitClipChannel.Begin`) 한 줄뿐이다 | 코드 |
| F30 | 아군도 무기 동작이 캐릭터와 다른 테이크다(계획 §0.8 X2). 부속 클립을 받으면 같은 문제를 코드 한 번으로 덮는다 | 계획 §0.8 |
| F31 | Idle은 컨트롤러가 원본이라 무기 Idle은 **컨트롤러에 레이어를 하나 더** 두면 된다(코드 무변경). 컨트롤러 클립의 루트 곡선 검사는 이미 모든 레이어 클립(`controller.animationClips`)을 본다 | `CombatUnitAnimationRules.ValidateUnit` |

### 12.2 결정안(권장)

**AC16. 사건 클립 = 주 클립 + 부속 클립(Q-AC20 A).** 클립 묶음의 칸(공격·피격)마다 **주 클립 하나 + 부속 클립 목록(0개 이상)** 이다.
- **주 클립**(몸)은 지금 그대로다: 이벤트 셋(`ClipStart`·`ClipEvent`·`ClipEnd`)을 찍고, 그 시각이 연출 시점과 재생 길이의 원본이다.
- **부속 클립**(무기 등)은 주 클립과 **같은 시각**으로만 돈다. 이벤트를 찍지 않는다(시점의 원본은 주 클립 하나).
- 입력: `CombatUnitAnimationSO`에 `attackCompanionClips`·`hitCompanionClips`(`AnimationClip[]`, 비워도 됨). 값: `CombatClipData.Companions`(읽기 전용 목록, 없으면 빈 목록). 피격 칸도 같은 모양이라 값·검사·재생 길이 하나를 같이 쓴다.
- 다른 안: B 공격 칸에 부속 하나만 — 칸 모양이 칸마다 달라지고, 무기가 둘(쌍권총 등)이면 다시 바꿈. C 무기만 따로 모은 SO — 시각 원본(주 클립)과 떨어져 묶음이 둘이 됨.

**AC17. 채널 위층 = 레이어 섞개(Q-AC21 A).** 위층(섞개 입력 1)에 `AnimationClipPlayable` 하나 대신 **`AnimationLayerMixerPlayable` 하나**를 꽂는다. 층 0 = 주 클립, 층 1..N = 부속 클립, 모두 가중치 1·덮어쓰기. 부속은 주 클립과 **다른 뼈**만 움직이므로(AC18 검사) 서로 덮지 않는다. 부속이 없어도 같은 모양(층 하나)이라 갈래가 없다.
- `SetTime(t)`는 모든 층의 클립에 같은 시각을 넣는다(할당 없음, 층 수만큼 반복). `End`는 레이어 섞개와 그 아래 클립을 함께 파괴하고 지금처럼 바닥으로 돌아간다(컨트롤러 없는 유닛은 Transform 되돌림 — 이미 Animator 아래 모든 자식을 기억하므로 무기 뼈도 덮음).
- 길이: 부속이 주 클립의 끝 이벤트보다 **짧으면 마지막 자세를 유지**하고, **길면 끝 이벤트에서 잘린다**(주 클립과 같은 규칙). 길이를 맞추라는 검사는 두지 않는다(Unity 확인 항목 U-C2).
- 대여 계약: `ICombatUnitClip.Play(AnimationClip)` → **`Play(CombatClipData clip)`**, 채널 `Begin(CombatClipData clip)`. 사건 클립 값 하나가 값 → 재생 단위 → 대여 → 채널로 그대로 간다(인자 목록을 층마다 되풀이하지 않음). 대여·채널은 값의 시각을 쓰지 않는다(문서에 적음).
- 다른 안: B 부속이 있을 때만 레이어 섞개 — 같은 일에 두 모양(무의미한 갈래). C 섞개(`AnimationMixerPlayable`)에 모두 가중치 1 — 한 입력이 움직이지 않는 곡선이 기본값 쪽으로 섞여 자세가 틀어질 수 있어 고르지 않음.

**AC18. 부속 클립 검사(Q-AC22 A).** 규칙 원본은 지금처럼 둘로 나눈다.
- **값 불변(`CombatClipData` 생성자):** 부속 목록에 빈 칸·중복·주 클립과 같은 클립이 없어야 한다(런타임에서도 지켜야 하는 모양).
- **클립 규칙(`CombatUnitAnimationRules.RequireCompanionClip`, DB 생성 때):** 주 클립과 같은 사건 클립 규칙(유한한 양수 길이·Loop Time 끔·루트 곡선 없음, `RequireEventClip`과 공유)에 더해 **이벤트 없음**(찍어도 쓰이지 않아 헷갈림), **주 클립·다른 부속과 곡선 겹침 없음**(같은 뼈·같은 속성을 둘이 움직이면 뒤 층이 이겨 몸 동작이 지워짐 — 예: `Scull|CannonAttackBody`를 부속으로 넣으면 걸림). 곡선 목록은 에디터에서만 읽히므로 겹침 검사는 루트 곡선 검사처럼 **에디터에서만** 한다(설계 I3).
- 클립 묶음 SO는 지금처럼 예외를 받아 칸 이름·에셋 이름을 붙인다(`ReadClip`).
- 다른 안: B 겹침 검사 없음 — 잘못 고른 부속이 몸 동작을 지워도 화면을 보기 전엔 모름.

**AC19. 에셋 쪽.** 무기 테이크를 따로 받으므로 민형님 요청 메모 §2(R2 "한 테이크로")를 **"무기 테이크는 따로 내보내도 됨 — 이벤트는 몸 테이크에만, 무기 테이크는 무기 뼈만"** 으로 고친다. 곡사 적 묶음 `CombatUnitAnimation_Cannon`은 부속에 `CannonBone|CannonAttackBody`를 넣는다(코드가 들어간 뒤). 무기 Idle이 필요하면 Idle 컨트롤러에 레이어를 더한다(F31).

### 12.3 바뀌는 곳

| 파일 | 바뀌는 것 |
|---|---|
| `DataBaseSystem/Data/View/CombatPresentation/CombatClipData.cs` | `Companions` + 생성자 선택 인자(없으면 빈 목록) + 값 불변 검사 |
| `…/CombatUnitAnimationSO.cs` | 칸 `attackCompanionClips`·`hitCompanionClips`, `ReadClip`이 부속 규칙을 부르고 값에 넘김 |
| `…/CombatUnitAnimationRules.cs` | `RequireCompanionClip`(이벤트 없음·겹침 없음, 사건 클립 규칙 공유) |
| `C/Display/UnitClipChannel.cs` | 위층 = 레이어 섞개, `Begin(CombatClipData)`, `SetTime`이 모든 층에, `End`가 섞개째 파괴 |
| `C/Display/ICombatUnitClip.cs`·`C/Display/FieldUnitView.cs`(대여) | `Play(CombatClipData)` |
| `C/CombatClipPlayback.cs` | `_lease.Play(_data)` 한 줄 |
| 시험 | 가짜 대여 셋(`CombatClipPlaybackTests`·`CombatPieceRunTests`·`CombatUnitPresentationWindowTests`)의 `Play` 서명, `UnitClipChannelTests`의 `Begin`·`Play` 호출(값 도우미 하나), 새 시험: 값 불변·부속 규칙(묶음 시험)·두 층이 다른 자식을 함께 움직임·짧은 부속이 마지막 자세 유지·`End`가 둘 다 되돌림(채널, Unity 전용) |
| 에셋·문서 | `CombatUnitAnimation_Cannon.asset` 부속 칸, 민형님 메모 §2·호진님 메모 §4·§7, 계획 §0.8(Q-AC12 B) |

조각·조립·처리기·창구·Factory는 바뀌지 않는다(클립 조각은 값 하나를 넘길 뿐이고 재생 길이·신호는 주 클립 그대로).

### 12.4 원칙 점검(권장안, 7기준)

| 기준 | 점수 | 근거 |
|---|---|---|
| SSOT | 9 | 시점·재생 길이의 원본은 주 클립 하나(부속은 이벤트 금지), 부속 규칙은 `CombatUnitAnimationRules` 한 곳, 값 모양 규칙은 `CombatClipData` 한 곳, 그래프 모양은 채널 한 곳 |
| SOLID | 8.5 | 채널은 여전히 그래프만, 값은 "사건 클립 하나"의 뜻을 넓힘(주 + 부속). 감점: 대여·채널이 쓰지 않는 시각까지 든 값을 받음(ISP 작은 감점, 대신 인자 되풀이를 없앰) |
| JMH 지침·FW 계약 | 9 | Factory·ServiceKeys 무변경, 프레임 경로 할당 0(`SetTime`은 층 반복만), 할당은 사건 시작 때 섞개·클립 Playable(지금도 클립 하나를 만듦) |
| 기능 변경의 독립성 | 9 | 무기 수·종류는 에셋만, 모델 내보내기 방식(아마추어별 테이크)에 코드가 맞춤, 공격·피격 같은 길 |
| 캡슐화 | 8 | 층 구조는 채널 안에 숨음. 남는 것: 시험용 그래프 읽기(K11 그대로) |
| C#·Unity 권장 구조 | 8.5 | 여러 클립을 층으로 겹치는 것은 Unity가 의도한 `AnimationLayerMixerPlayable` 쓰임새, 값은 읽기 전용 목록 복사 |
| 무의미한 If·Tick | 9 | 부속 유무로 갈리지 않는 한 모양, 매 프레임 검사 추가 없음(겹침 검사는 DB 생성 때 에디터에서만) |
| **예상** | **약 8.7** | 가장 큰 남는 것은 K11(시험용 그래프 노출, 기존) |

### 12.5 위험과 Unity 확인 항목

| # | 확인할 것 | 확인 방법 |
|---|---|---|
| U-C1 | 레이어 섞개 덮어쓰기 층이 **자기가 움직이는 곡선만** 덮는지(다른 뼈의 주 클립 자세가 남는지) | 채널 시험: 메모리 클립 둘이 다른 자식을 움직이고 둘 다 움직였는지(Unity 전용) |
| U-C2 | 반복 아닌 짧은 부속이 길이를 넘은 시각에서 마지막 자세를 유지하는지 | 채널 시험: 짧은 부속 + 긴 시각 |
| U-C3 | 실제 곡사 적: 몸 `Scull|CannonAttack` + 포신 `CannonBone|CannonAttackBody`가 같이 돌고, 포구(`muzzle`, 포신 뼈 아래)가 포신을 따라가는지 | 재생 확인(이벤트·프리팹 준비 뒤) |

### 12.6 사용자 결정 — 결정 완료(2026-10-19)

사용자: "Q-AC20~Q-AC22 모두 A로 해. 계획 쓰고 AC-6a부터 진행해. MSBuild 생략하고 AC-6c 까지 쭉 진행". 계획 §10.

- **Q-AC20. 칸 모양:** **A(권장) 칸(공격·피격)마다 부속 클립 목록** / B 공격 칸에 부속 하나만 / C 무기만 따로 모은 SO.
- **Q-AC21. 위층 모양:** **A(권장) 언제나 레이어 섞개 하나(층 0 주 클립, 층 1..N 부속)** + 대여·채널은 사건 클립 값 하나를 받음 / B 부속이 있을 때만 레이어 섞개.
- **Q-AC22. 부속 검사:** **A(권장) 사건 클립 규칙 + 이벤트 없음 + 곡선 겹침 없음(에디터)** / B 겹침 검사 없음.
- 결정 뒤 Phase(계획 §10으로 상세화): **AC-6a** 값·입력·검사(화면 변화 없음) → **AC-6b** 채널 레이어 섞개·대여 계약·재생 단위·시험 → **AC-6c** 에셋(곡사 묶음 부속 칸)·공유 메모 고침.
- **구현 기록(2026-10-19, 계획 §10.1):** AC-6a~AC-6c 코드 완료(MSBuild 생략, Unity 확인 대기). 계획에서 설계 빈틈 하나를 메움(계획 P17): 위층을 빼면 Idle이 움직이지 않는 뼈(무기 뼈)는 그래프 바인딩에서 빠져 공격 마지막 자세에 멈추므로, **클립이 끝나면 컨트롤러가 있어도 Animator 아래 Transform을 준비 때 값으로 되돌린다**(같은 프레임의 Unity 평가에서 Idle이 자기 뼈를 다시 덮음). AC17의 "컨트롤러 없는 유닛은 Transform 되돌림"을 모든 유닛으로 넓힌 것이다.

## 13. 공격·피격 애니메이션 연출 구조 7기준 검토 2차 (2026-10-20, 하위 에이전트 3개, 코드 변경 없음)

사용자: "이번에 만든 공격/피격 애니메이션 연출 구조에 대해서 1.SSOT 2.SOLID 3.JMH Framework 개발 지침·FW 계약 4.기능 변경의 독립성 5.캡슐화 6.C# 및 유니티 권장 구조 7.무의미한 If문 혹은 Tick 검사 … 1-10 스케일로 검토해줘, 이번엔 하위 에이전트 3개 추가로 돌려서 진행해줘". 대상은 클립 AC-1~AC-6·클립 이펙트 EF-1~EF-3·투사체 접촉 이펙트 IE-a~IE-c의 코드(DB 입력 10·연출 20·표시 12 파일). 에이전트 셋이 기준을 나눠 읽기 전용으로 봤다(① SSOT·독립성 ② SOLID·캡슐화 ③ FW·C#/Unity·If/Tick). 결함 후보 둘(V2·V8)은 코드를 다시 읽어 확인했다.

### 13.1 점수

| 기준 | 점수 | 근거 요약 | 권장 반영 뒤 |
|---|---|---|---|
| SSOT | 8 | 시각·이벤트 이름·원거리 판정·이펙트 종류·해제 뒤 Hit·방향·위층 모양·휴식 자세가 한 곳씩. 감점: 규칙 함수 밖 직접 비교(Empty 넷), 공격 결과 인자 풀기 다섯 벌, SO·값 이중 검사(K9가 2 → 4개), 클립 조각과 클립 대여 짝을 조립마다 따로 | 9 |
| SOLID | 7.5 | 조각은 작고 새 조각은 고칠 곳 0(OCP 좋음). 감점: 공격 재생 575줄·바뀌는 이유 여섯(K2 재발), 창구·이펙트 출구가 구체 형식, 클립 대여 계약에 재생과 자리 표시 읽기가 섞임, 계약 문서와 실제가 다른 곳 | 8.7 |
| JMH 지침·FW 계약 | 8.5 | Framework 무변경·새 서비스 없음·`EffectScope` 그대로·`StopAll` 회수·프레임 할당 0. 감점: 전투마다 Scope 최대 3개 누적(G2), 이펙트 등록 여부는 첫 재생 때(FW-012 꼴), 연출 값 사건마다 새로 만듦(K10) | 9.2 |
| 기능 변경의 독립성 | 8 | 새 이펙트·부속 수는 에셋만, 다른 작업자 파일은 호진님 한 줄·민형님 Factory 한 줄·Core 0. 감점: 자리 표시(포구·이펙트 지점)가 늘 때마다 대여 계약·채널·가짜 셋이 함께 바뀜(AC-5·EF-2에서 두 번), 공격 재생이 네 기능의 공동 수정 파일 | 8.8 |
| 캡슐화 | 7.5 | 불변 값·`readonly struct`·대여 칸 부품은 좋음. 감점: 대여 초기화 목록 세 곳이 이미 어긋남(V2, 결함 후보), 시험용 읽기가 그래프·채널을 그대로 내줌(K11), 공격 표시 값의 불변식이 SO에만 | 8.6 |
| C#·Unity 권장 구조 | 8 | `IDisposable` 그래프·GameTime·`OnEnable`/`OnDisable` 맞춤·Unity null 비교·`#if UNITY_EDITOR` 위치 맞음. 감점: Animator 속성 다섯 중 둘만 돌려줌(V8), K11, 예외 인자 이름 오기, 이름 없는 선택 인자 | 9 |
| 무의미한 If·Tick | 7.5 | 폴링 없음(시간 없는 조각은 시작에서 끝, 발사 조각은 콜백). 감점: 맞는 순간 하나에 세대 확인 3~4겹(V6), 이펙트 종류 검사 5층, 클립 시각 검사 매 프레임 4층(K8), 맞은 뒤에도 매 프레임 세대 조회 | 8.7 |
| **종합** | **7.9** | §11(8.3, 직접 검사)보다 낮음 — AC-5·AC-6·EF·IE로 범위가 넓어졌고(공격 재생 478 → 575줄, 대여 계약 넓어짐) 에이전트 셋이 세대 확인 겹·초기화 목록 어긋남 같은 새 항목을 찾음 | **약 8.9** |

### 13.2 이전 검사(§11 K1~K18) 현황 — 코드로 확인

- **해결:** K1(곡사도 `Toward`), K3(조립 입구 둘), K16(`CombatHitMoment` 하나), 이펙트 설계 L1(종류 규칙 `CombatPresentationEffectRules` 하나).
- **재발:** K2 — 투사체 진행은 뗐지만 출처 발사·접촉 이펙트로 공격 재생이 다시 575줄(V1).
- **악화:** K4(수용이었음) — EF-2 `TryGetEffectPoint`로 대여 계약의 자리 표시 읽기가 둘(V3). K9 — 이중 검사 2 → 4개(V9).
- **그대로:** K5(대여 시작 검사 반복), K6(`FieldUnitView` 588줄), K8(시각 검사 층), K10, K11, K15.
- **수용 유지:** K7, K12~K14, K17, K18.

### 13.3 발견(세 보고 합침, 중복은 하나로)

| # | 등급 | 기준 | 내용(근거) | 권장 | 출처 |
|---|---|---|---|---|---|
| V1 | 중간 | SOLID·독립성 | `CombatAttackPlayback` 575줄, 바뀌는 이유 여섯: Gate 수명, 코드 모션 넷, Ballista 무기·재장전, 클립 출처 발사(`FireShot`), 접촉 진행률·진단 경고, 접촉 이펙트. 자세 복원 try/catch 세 벌(Ballista만 `AggregateException` 없음) | 접촉 진행률·경고를 `CombatContactTiming`으로, 출처 발사·접촉 이펙트를 `CombatSourceShots`로 떼어 공격 재생은 Gate·코드 모션·재장전만(약 380줄). 자세 시작·실패 복원 도우미 하나 | ②①③ |
| **V2** | **중간(결함 후보, 확인함)** | 캡슐화·독립성 | 대여 초기화 목록이 세 곳(`FieldUnitView.Bind`·`OnDisable`·`ResetForPool`)이고 `ResetForPool`에만 `ResetPresentationScale`이 없다. 게다가 `EntityId`를 비운 뒤 `SetActive(false)`라 이어지는 `OnDisable`의 크기 복원도 `IsCurrent`에서 걸려 칸만 비운다 → **포효 크기 대여가 살아 있는 채 풀로 반환되면 커진 크기가 다음 유닛에 남을 수 있다**(경로가 실제로 생기는지는 미확인). 10-15 코드부터 있던 것 | 대여 칸 넷을 묶은 `ResetAll()` 하나를 세 곳이 부르고, `ResetForPool`은 ID를 비우기 전에 부름(K5·K6도 함께 줄어듦) | ② |
| V3 | 중간 | 독립성·ISP | 클립 대여 계약 `ICombatUnitClip`에 재생(`Play`·`SetTime`·`RestoreClip`)과 자리 표시 읽기(`TryGetMuzzle`·`TryGetEffectPoint`)가 섞임. 자리 하나를 더하면 컴포넌트 칸·채널 필드·계약·대여·시험 가짜 셋이 함께 바뀜(AC-5·EF-2 두 번). 그래프 클래스 `UnitClipChannel`도 그래프와 상관없는 자리 읽기를 가짐 | 자리 종류 열거(`Muzzle`·`Effect`) + `TryGetPoint(kind, out pos, out rot)` 한 입구를 좁은 계약으로 떼어 냄. 새 자리 = 열거 값 + 컴포넌트 칸 + 규칙 한 줄 | ①② |
| V4 | 중간 | ISP·DIP | `CombatUnitPresentationWindow`(구체, 멤버 약 25개)가 대여 발급·소유 사실·연출 값 읽기 세 묶음. 쓰는 쪽은 일부만 씀 | 클래스는 하나뿐인 입구(B13)로 두고 역할 인터페이스 둘(`TryBegin*`·`TryGet*`)을 구현, 쓰는 쪽은 인터페이스를 받음 | ② |
| V5 | 낮음 | DIP·If | `CombatEffectWindow` 구체 형식이 조각·조립·재생에 그대로 들어가 `StopAll`·`Release`까지 노출, 조각 시험이 Unity 전용. "창구 없음" null 검사 넷(카메라는 null 객체 방식과 다름) | 쓰는 쪽은 `ICombatEffectSink { Play }`, Presenter만 구체 형식. 없을 때는 빈 출구 | ②③ |
| V6 | 중간 | If·Tick | 원거리 클립이 한 번 맞을 때 세대 확인 3~4겹(투사체 Shot·`ReleaseAll`·실행 `Signal`, 정면 공격 모션·Ballista 람다도 한 겹 더). 취소는 늘 "세대 올림 + 전체 취소"라 소유자 한 곳의 표시로 이미 걸러짐. 맞은 뒤에도 매 프레임 세대 조회(조건 순서) | "늦은 콜백은 그 콜백을 만든 소유자 한 곳에서 거른다"로 정하고 `FireArc`·`FireLinear`·`FireShot`의 `version` 인자 제거, `ReleaseAll`의 비교는 코드 모션 입구로. 조건 순서 `!_hitSent && progress >= _hitAt && IsCurrent` | ③ |
| V7 | 낮음 | SSOT·If | 이펙트 "없음" 판정을 규칙 함수 밖에서 직접 비교(넷), 종류 검사 5층(SO·값·조립·조각·창구). 조각 검사는 `IsPlayable`보다 약함 | 규칙에 `IsNone`, 값에 `HasImpactEffect`, 경계 둘(입력 = SO가 값 예외를 감쌈, 출력 = 창구)만 남김 | ①③ |
| **V8** | 낮음(확인함) | C#·Unity | `UnitClipChannel`이 넘겨받을 때 Animator 속성 다섯(컨트롤러·applyRootMotion·fireEvents·cullingMode·enabled)을 바꾸고 실패 정리·`Dispose`는 둘만 돌려줌 | 다섯 값을 모두 기억·복원 | ③ |
| V9 | 낮음 | SSOT·If | K9 확대: SO가 선딜레이 셋·이펙트 종류를 먼저 보고 값 생성자가 다시 봄(4개) | SO가 값 생성자 예외를 잡아 칸·에셋 이름을 붙여 다시 던지는 한 길(`ReadClip` 방식) | ①③ |
| V10 | 낮음 | SSOT | 공격 결과에서 목표점 인자 8개를 풀어 넘기는 코드·대체 회전 `RotationToWorld(AttackDirection)`이 다섯 벌. 접촉 자리 대체 회전이 코드 모션(`Facing`)과 클립 출처 발사(공격 방향)에서 다름(수평 길이 0일 때만) | `CombatShotPath.TryTarget(kind, in AttackResult, mapper, origin, out)`·`CombatAttackFacing.Fallback(in AttackResult, mapper)` 하나씩, 정규화 위쪽 한 곳 | ① |
| V11 | 낮음 | SSOT | "접촉 이펙트 → 해제" 세 벌(`ImpactThen` 둘 + Ballista 람다), "진행률이 hitAt을 지나면 한 번 맞음" 두 벌(직선 코드 모션·Shot), 진행률 유한 검사 + Clamp01 세 벌 | 감싸기 함수 하나를 Ballista도, 진행률 정규화 함수 하나 | ① |
| V12 | 낮음 | SSOT·독립성 | 클립 조각 ↔ 클립 채널 대여 짝을 조립마다 따로 적음(빠뜨리면 실행 때 예외), `After(played, ClipEvent, 지연)` 네 번 | `composition.AddClip(subject, clip, start, waits)`·`AtClipEvent(played, delay)` | ① |
| V13 | 낮음 | FW·SSOT | Scope 수명 규칙 두 벌(이펙트 창구·폭발 Presenter), 기차까지 전투마다 Scope 최대 3개 누적 | 폭발(가능하면 기차도)이 창구 하나를 받음(Factory 한 줄 — 민형님 공유). Scope 해제 API는 민형님께 요청 | ①③ |
| V14 | 낮음 | 캡슐화 | K11: `UnitClipChannel.Graph`·`FieldUnitView.ClipChannel`·`CurrentPresentationPose`가 내부 객체를 내줌 | `EvaluateForTests(dt)`와 bool 읽기만 | ②③ |
| V15 | 낮음 | 캡슐화 | `CombatAttackPresentationData` 생성자가 검사 없음 — "근접이면 접촉 이펙트 Empty" 불변식이 SO에만 | `FindError` 하나를 생성자·SO가 함께 | ② |
| V16 | 낮음 | LSP(문서) | 조각 계약 문서 "외부 진행 조각 Cancel은 아무 일도 안 함"과 달리 `CombatFirePiece.Cancel`은 투사체 회수. `ICombatUnitClip.Play`는 "대여 하나에 한 번"을 적지 않음(위층이 남으면 예외), 자리 읽기가 취소 뒤 예외인 것도 미기재. `ICombatSourceLifetime.ReleaseAll`은 인터페이스로 불리는 곳 없음 | 계약 문서 고침, `ReleaseAll`은 인터페이스에서 빼거나 처리기가 인터페이스를 받음 | ② |
| V17 | 낮음 | OCP | 기능마다 선택 인자가 덧붙음: `ComposeHit` 7개, 처리기 11개, Presenter 선택 인자 17개. 새 공격 종류는 분기 7곳 이상(종류 4개로 닫혀 있어 전략 패턴은 과함) | 장면 연출 창구(카메라·이펙트·연출 값)를 값 하나로 묶음, 종류 분류 함수 한 곳 | ② |
| V18 | 낮음 | If·Tick | K8·K15 그대로(클립 시각 검사 매 프레임 4층, 조각 길에서 닿지 않는 분기), 곡사 코드 모션 `CanRender()` 프레임당 세 번, `PrepareClipChannel` 첫 생성 때 두 번, `Tick`·`TickShotsOnly` 공통부 겹침 | 경계 검사만 남기고 안쪽 중복 제거, 한 번 보고 결과를 넘김 | ③ |
| V19 | 낮음 | C#·Unity | 세 시각 중 무엇이 틀려도 예외 인자 이름 `nameof(eventTime)`, 이름 없는 선택 인자 넘김(`_effects`·`effects`·`ComposeHit` 카메라 둘) | 인자 이름 바로잡기, 모두 이름 있는 인자 | ③ |
| V20 | 낮음 | FW | K10: 연출 값은 사건마다 `CreateData`(클립 묶음만 보관), 곡사는 한 공격에 두 번 | 클립 묶음과 같은 외형별 보관 | ③ |
| V21 | 확인 필요 | FW·Unity | 이펙트가 필드 루트(씬 객체) 아래로 옮겨지므로, 씬이 내려가기 전에 Presenter `Release`의 `StopAll`이 먼저 돌지 않으면 풀에 파괴된 노드가 남을 수 있음(환경·기차 이펙트도 같은 꼴) | Combat 이탈 때 View Exit(-100)이 씬 언로드보다 먼저인지 확인 | ③ |
| V22 | 수용 | — | 이벤트 셋 고정(AC2), 대여가 쓰지 않는 시각까지 든 값을 받음(AC17), 불변 목록 형변환 가능(internal), 중첩 대여 클래스의 private 접근(K12), Ballista `IsComplete` getter 상태 변경(기존 코드, 범위 밖 기록), `ConnectLayer` 실패 때 Playable은 그래프 파괴로 회수 | 그대로 | ①②③ |

### 13.4 권장 묶음(결정 대기)

| 묶음 | 항목 | 동작 | 예상 효과 |
|---|---|---|---|
| A 결함·작은 정리 | V2(대여 초기화 하나 + 풀 반환 순서), V8(Animator 다섯 값), V19, V7·V9(검사 경계 둘), V16(계약 문서) | V2는 결함 고침, 나머지 같음 | 캡슐화 +0.5, C#·Unity +0.6, If +0.3 |
| B 구조 | V1(공격 재생 분리), V3(자리 표시 입구 하나·좁은 계약), V6(세대 확인 한 곳), V4·V5(역할 인터페이스·이펙트 출구) | 같음(시험 가짜·계약 문구 영향) | SOLID +1.0, 독립성 +0.6, If +0.6 |
| C 낮음 정리 | V10~V13, V14, V15, V17, V18, V20, V21 확인 | 같음 | 각 +0.1~0.3 |

A만 하면 약 8.2, A + B면 약 8.7, 모두 하면 약 8.9.
