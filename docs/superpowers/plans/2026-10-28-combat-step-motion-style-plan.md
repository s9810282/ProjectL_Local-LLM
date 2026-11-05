# 전투 걸음 모양(칸마다 뛰어 이동) Task 계획 (2026-10-28)

- **설계:** [step-motion-style-design](../specs/2026-10-28-combat-step-motion-style-design.md)(ST1~ST7).
- **결정(2026-10-28 사용자 "권장안대로"):** Q-ST1~Q-ST7 모두 A.
  - 모양 값은 전투 연출 설정 표 한 곳에 둔다.
  - 밀림(넉백·끌어오기)은 걷기 그대로다.
  - 칸마다 한 번 뛴다.
  - 뛰기 값은 높이·시간 둘이다.
  - 뛰기의 접촉 시점은 착지 순간이다.
  - 뛰기 전에 먼저 돈다.
  - 장애물 건너뛰기는 자리만 연다.
- **진행 규칙(CLAUDE.md·메모리):**
  - View 작업이라 새 시험을 쓰지 않는다. 전투 View라 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적고, 컴파일·재생 확인은 사용자가 Unity에서 한다.
  - Phase가 끝나면 멈추고 보고한다. 검토 에이전트는 묶음 끝(ST-c)에 한 번만 돌린다.
  - 커밋은 사용자가 한다.

## 0. 착수 조건·기준 판

- **기준 판:** HEAD `479ecc69` + 작업 트리.
- **같은 파일 겹침:** 작업 트리의 `CombatPresentationSettingsDataSO.cs`에는 소리 창구 2차 변경(커밋 전)이 있다. ST-a도 같은 파일의 다른 묶음("유닛 이동 연출")을 고친다. 소리 2차를 먼저 커밋하는 것을 권한다. 그 전에 착수하면 커밋 때 hunk로 나눈다.
- **이동 점검 수정:** `CombatPathInput.TryGetAttackDragOrigin`(10-28)은 다른 파일이라 겹치지 않는다.
- **다른 사람 시험:** 천명님 `PullAndMarkPresentationTests.cs:414`는 `CombatEventPresentationValues` 생성자를 위치 인자로 부른다. 새 값은 끝 선택 인자로 받으므로 그 시험은 그대로 컴파일된다.
- **Factory:** `CombatSceneDisplayFactory.cs:125`는 `From(settings)`만 부르므로 바꾸지 않는다.

## 0.1 계획에서 정한 것 (사용자 확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| P1 | 열거 `CombatStepLook`은 설정 SO 옆 `Table/CombatPresentationSettings/Item/CombatStepLook.cs`에 둔다(네임스페이스 `…Table.CombatPresentationSettings.Item`). | 배경 방식 열거 `StageViewPropMode`(같은 날)를 그 SO 옆에 둔 선례. 설정 표만 쓰는 값이다. |
| P2 | 새 칸 셋은 "유닛 이동 연출" 묶음의 `stepClimbHop` 바로 뒤에 둔다. 에셋 YAML도 같은 순서로 넣는다. | 묶음이 하나로 읽힌다. |
| P3 | 검사: `hopSeconds`는 지금 `ValidateStepSeconds`(유한한 양수)를 그대로 쓴다. `hopHeight`는 새 `ValidateHopHeight`(유한한 0 이상), 모양은 새 `ValidateStepLook`(열거에 있는 값)를 둔다. 셋 다 `Validate()`에서 늘 검사한다(설계 ST4). | 조건 원본은 설정 Data 한 곳이다. 문구가 다른 값은 함수를 나눈다. |
| P4 | 값 객체 `CombatStepLookSettings.Walk`(설정을 넘기지 않은 조립의 기본)는 모양 `Walk`, 뛰기 높이 0, 뛰기 시간 0.25다. 뛰기 값은 `Walk`에서 쓰지 않는다. | 생성자 검사를 늘 통과하는 값이다. `CombatFallMotionSettings.Instant`와 같은 꼴이다. |
| P5 | 모양 표 만들기 `CombatStepStyles.From(CombatEventPresentationValues)`는 `Presentation/Movement/`에 둔다. | `Presentation/`이 `Playback.Support` 값을 읽는 선례가 이미 여섯 곳 있다(`CombatClipAttack` 등). "모양(Walk·Hop) → 전략" 대응을 이 한 곳에 둔다. |
| P6 | 모양 표는 인스턴스다(값이 설정에서 오므로). 종류 값으로 찾는 배열을 쓰고, 생성자에서 열거의 모든 값에 전략이 있는지 검사한다. 빠지면 전투 표시 조립에서 실패한다. | 공격 종류 표 `CombatAttackStyles.Build`와 같은 검사다. 정적 표가 아닌 점만 다르다. |
| P7 | `CombatStepStyle`은 추상 클래스로 둔다(멤버: `Seconds`·`HeightAt`·`ContactProgress`). 파생 둘은 상태 없는 `sealed` 불변 객체다. | 공격 전략 `CombatAttackStyle`과 같은 꼴이다. |
| P8 | `CombatStepHeight`에서 호 식을 `Arc(from, to, progress, peakAboveHigher)`로 꺼낸다. `At`은 "높이가 같으면 평평, 아니면 `Arc`"다. 걷기 결과는 지금과 비트 단위로 같다. | 수식 사본을 만들지 않는다(설계 ST2). |
| P9 | 레인 `BeginStep`의 `bool rotate`를 `CombatMovementKind kind`로 바꾼다. 필드 `_rotateMovement`는 `_kind`로 바꾼다. 회전 여부와 접촉 게이트 인자는 `_kind == Voluntary`다. 고른 모양은 `_style`에 둔다. 불변 객체라 취소 때 비우지 않는다. | bool 하나가 두 이름으로 내려가던 것을 종류 하나로 합친다(설계 ST1). |
| P10 | 끌어오기 `BeginPull`은 `_styles.For(Forced)`로 `_style`을 정하고, `TickPull`은 그 시간·높이를 쓴다. 반복 합치기는 하지 않는다. | 설계 ST3·ST7. |
| P11 | 종류 판정은 `CombatMovementPresentation.BeginStep` 한 곳이다(`IsVoluntaryMovement` 결과 → 종류). `EndMovement`의 방향 복귀 판정은 지금 그대로 `IsVoluntaryMovement`를 쓴다. | 방향 복귀는 걸음 모양과 무관하다. 바꾸면 범위가 늘어난다. |
| P12 | `Placement.Interpolate`는 넘어온 모양을 null 검사하지 않는다. 레인이 늘 넘기고, 매 프레임 불리는 함수다. | 무의미한 If를 만들지 않는다. 계약 주석에 "null이면 안 됨"을 적는다. |
| P13 | 기본 에셋 값은 `voluntaryStepLook = Hop(1)`, `hopHeight = 0.3`, `hopSeconds = 0.25`다. 느낌은 Unity 확인 때 사용자가 조정한다. | 설계 ST4. |
| P14 | 천명님께 알릴 끌어오기 줄 변경(`BeginPull`·`TickPull` 세 줄 안팎)은 Core 전달 문서 새 절에 적는다. | 다른 사람 줄을 고치면 변경 정리를 넘긴다(CLAUDE.md 영역 규칙). |

## 1. Phase ST-a — 값 (화면 변화 없음)

| Task | 파일 | 할 일 |
|---|---|---|
| Ta.1 | 새 `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatStepLook.cs` (+ `.meta`) | `internal enum CombatStepLook { Walk = 0, Hop = 1 }`. 값마다 한 줄 주석(Walk = 지금 걸음, Hop = 칸마다 뜀) |
| Ta.2 | `…/CombatPresentationSettingsDataSO.cs` | ① "유닛 이동 연출" 묶음 `stepClimbHop` 뒤에 칸 셋을 둔다: `voluntaryStepLook`(기본 `Hop`), `hopHeight`(`Min(0)`, 0.3), `hopSeconds`(`Min(.01)`, 0.25). 툴팁에는 "자발 이동(경로)만, 밀림은 늘 걷기"와 "Walk면 뛰기 칸은 쓰지 않음"을 적는다. ② 읽기 셋 `VoluntaryStepLook`·`HopHeight`·`HopSeconds`. ③ `Validate()`에서 `stepClimbHop` 검사 뒤에 셋을 검사한다. ④ 조건 함수 `ValidateHopHeight`·`ValidateStepLook`(`System.Enum.IsDefined`)을 둔다. |
| Ta.3 | `DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` | `stepClimbHop: 0.1` 뒤에 `voluntaryStepLook: 1`·`hopHeight: 0.3`·`hopSeconds: 0.25`를 넣는다. 이 파일은 CRLF라 Edit 도구로 고친다. |
| Ta.4 | 새 `ViewSystem/Combat/Playback/Support/CombatStepLookSettings.cs` (+ `.meta`) | 값 객체(`Look`·`HopHeight`·`HopSeconds`), 생성자 검사는 설정 조건 함수, `From(settings)`, 정적 `Walk`(P4). `CombatFallMotionSettings`와 같은 꼴이다. |
| Ta.5 | `…/Playback/Support/CombatEventPresentationValues.cs` | 끝 선택 인자 `CombatStepLookSettings stepLook = null`(없으면 `Walk`)과 읽기 `StepLook`, `From`이 `CombatStepLookSettings.From(settings)`를 넘긴다. 문서 주석 한 줄. |

**Ua (사용자 Unity 확인):**
1. 컴파일 오류가 없다.
2. 설정 에셋 인스펙터에 칸 셋이 보이고, 기본 에셋 값이 `Hop`·0.3·0.25다.
3. Combat 진입·이동이 지금과 같다(아직 아무도 읽지 않음).
4. `hopSeconds`를 0으로 바꾸면 부트 DB 생성에서 막힌다(되돌릴 것).

예상 점수: 변화 없음(약 7.9). 값 자리만 생긴다.

## 2. Phase ST-b — 모양 (화면에서 칸마다 뜀)

| Task | 파일 | 할 일 |
|---|---|---|
| Tb.1 | `ViewSystem/Combat/Presentation/Movement/CombatStepHeight.cs` | 호 식을 `Arc(fromHeight, toHeight, progress, peakAboveHigher)`로 꺼낸다(P8). `At`은 `rise == 0 ? from : Arc(…, hop)`다. 클래스 주석에 "걷기 = At, 뛰기 = Arc, 식 원본은 여기 하나"를 적는다. |
| Tb.2 | 새 `…/Movement/CombatMovementKind.cs` (+ `.meta`) | `internal enum CombatMovementKind { Voluntary = 0, Forced = 1 }`. 주석: 판정 원본은 `IsVoluntaryMovement`, 끌어오기는 `Forced`, 나중 `Leap`은 Core 사실이 생길 때(설계 ST6). |
| Tb.3 | 새 `…/Movement/CombatStepStyle.cs` (+ `.meta`) | 추상 클래스(P7). `internal abstract float Seconds { get; }`, `internal abstract float HeightAt(float fromHeight, float toHeight, float progress)`, `internal abstract float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source)`. 계약 주석(진행률 0~1, 할당·예외 없음, 매 프레임 불림). |
| Tb.4 | 새 `…/Movement/CombatWalkStepStyle.cs` (+ `.meta`) | 생성자(`seconds`, `climbHop`, 설정 조건 함수로 검사). `HeightAt` = `CombatStepHeight.At`, `ContactProgress` = `CombatStepContacts.ContactProgress`(이동 접촉 M2). |
| Tb.5 | 새 `…/Movement/CombatHopStepStyle.cs` (+ `.meta`) | 생성자(`seconds`, `height`). `HeightAt` = `CombatStepHeight.Arc(…, height)`, `ContactProgress` = 1(착지, 설계 ST5). |
| Tb.6 | 새 `…/Movement/CombatStepStyles.cs` (+ `.meta`) | 인스턴스 표(P6): 생성자 `(CombatStepStyle voluntary, CombatStepStyle forced)`가 종류 배열을 채우고 모든 종류를 검사한다. `For(CombatMovementKind)`, 정적 `From(CombatEventPresentationValues)`가 걷기 하나를 만들고, 모양 `Walk` → 그 걷기 / `Hop` → 뛰기로 정한다(모르는 모양은 예외, P5). |
| Tb.7 | `…/Display/Unit/ICombatUnitPlacement.cs`, `CombatUnitPresentationWindow.cs`(`Placement`) | `Interpolate`의 마지막 인자 `float climbHop` → `CombatStepStyle style`. 높이는 `style.HeightAt(fromHeight, toHeight, progress)`. 계약 주석을 고친다(P12). |
| Tb.8 | `…/Movement/CombatUnitMotionPlayback.cs` | 생성자: `stepSeconds`·`stepClimbHop` → `CombatStepStyles styles`(`turnSeconds`는 남김). `BeginStep(step, kind, startNow, mapper, completed)`: `_style = _styles.For(kind)`, `_kind = kind`, 게이트에 `kind == Voluntary`. `StartStep`의 회전 조건, `Tick`의 시간(`_style.Seconds`)·`Interpolate(…, _style)`, `ArmContacts`의 `_style.ContactProgress`, `BeginPull`·`TickPull`(P10)을 고친다. 클래스·인자 주석도 고친다. |
| Tb.9 | `…/Movement/CombatUnitMotionSet.cs` | 생성자 인자를 같은 꼴로 바꾼다(`stepSeconds`·`stepClimbHop` → `styles`). `BeginStep(step, CombatMovementKind kind, …)`, `Lane()`이 표를 넘긴다. |
| Tb.10 | `…/Playback/Support/CombatMovementPresentation.cs` | `BeginStep`: `CombatMovementKind kind = _pacing.IsVoluntaryMovement(step) ? Voluntary : Forced;`(종류 판정 한 곳, P11). 강조 지우기·간격 조건은 `kind == Voluntary`, `_motion.BeginStep(step, kind, …)`. |
| Tb.11 | `…/Playback/CombatSceneEventPresenter.cs` `:100` | `new CombatUnitMotionSet(_presentation, units.BodyOf, CombatStepStyles.From(values), values.MovementTurnSeconds, generation, …)`. 끝 인자 `values.StepClimbHop`는 지운다. |

- **확인할 호출처(컴파일 맞춤):** `BeginStep`의 bool 인자를 부르는 곳은 `CombatMovementPresentation` 하나다. `Interpolate`는 레인 두 곳(`Tick`·`TickPull`)이다. `CombatUnitMotionSet` 생성은 Presenter 하나다. 착수 때 다시 검색한다.
- **남는 값:** `CombatEventPresentationValues.StepClimbHop`·`StepSeconds`는 `From`(모양 표)이 계속 읽는다. 지우지 않는다(천명님 시험 생성자 그대로).

**Ub (사용자 Unity 확인):**
1. 컴파일 오류가 없다.
2. 아군 경로 이동이 칸마다 한 번씩 뛴다(먼저 돌고 뜀). 적 경로 이동도 그렇다.
3. 고지대를 오르내릴 때 꼭대기가 높은 칸 위로 지나고, 블록 모서리를 뚫지 않는다.
4. 넉백·끌어오기는 지금처럼 미끄러진다(뛰지 않음).
5. 가시·지뢰·깃발 칸에 뛰어들면 착지 순간에 반응한다.
6. 경로 끝 방향 복귀와 다음 행동까지의 간격은 그대로다.
7. 설정 에셋 `voluntaryStepLook`을 `Walk`로 바꾸면 지금(걷기)과 같다. 확인 뒤 `Hop`으로 되돌린다.
8. `hopHeight`·`hopSeconds`를 바꾸면 다음 전투 진입부터 반영된다.

예상 점수: 약 8.8(설계 §5 기준에서 문서·검토 전).

## 3. Phase ST-c — 문서·공유·묶음 끝 검토

| Task | 파일 | 할 일 |
|---|---|---|
| Tc.1 | `docs/superpowers/specs/2026-10-17-combat-movement-contact-design.md` M2 | 보충 한 줄: "접촉 진행률은 걸음 모양이 정한다. 걷기 = 지나며 닿는 순간(이 절), 뛰기 = 착지(걸음 모양 설계 ST5)" |
| Tc.2 | `docs/superpowers/specs/2026-10-20-combat-high-ground-view-design.md` 3단계 | 보충 한 줄: "높이 식 원본은 그대로 `CombatStepHeight`, 뛰기는 `Arc`(걸음 모양 설계 ST2)" |
| Tc.3 | `docs/superpowers/viewDocs/combat-view-folder-guide.md` §1.1 `Movement/` 줄 + §2 | 예에 `CombatStepStyle`·`CombatStepStyles`를 더하고, §2에 "이동 종류마다 다른 걸음 모양 → 전략 + 표 한 줄" 행을 둔다(공격 종류 행과 같은 꼴). |
| Tc.4 | `docs/superpowers/handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md` | 새 §9: 설정 칸 셋(뜻·기본값·검사), 열거 0 = Walk(값 없는 에셋은 걷기). 미전달. |
| Tc.5 | `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` 새 절 | 천명님 작성 레인 끌어오기 줄(`BeginPull`·`TickPull`)의 바뀐 점(P14). Core 요청 없음. 미전달. |
| Tc.6 | 설계 문서 새 §7 "구현 메모" | 설계와 다르게 한 것, 남은 것을 적는다. |
| Tc.7 | 검토 에이전트 하나 | 7기준으로 ST-a·ST-b 변경을 검토한다(새 에이전트, 우리 결론 없이 코드만 줌). 결과는 이 계획 §6에 적고, 반영할지는 사용자가 정한다. |

## 4. 커밋 묶음 (커밋은 사용자)

- **한 커밋에 함께 넣을 것:**
  - 새 `.cs` 일곱 + `.meta`: `CombatStepLook`, `CombatStepLookSettings`, `CombatMovementKind`, `CombatStepStyle`, `CombatWalkStepStyle`, `CombatHopStepStyle`, `CombatStepStyles`.
  - 설정 SO 코드와 기본 에셋(코드에 칸이 없는데 에셋에 값이 있으면 경고, 반대면 걷기로 남음).
- ST-a만 따로 커밋해도 컴파일된다(값만 생김). ST-b는 ST-a 위에서만 컴파일된다.
- `CombatPresentationSettingsDataSO.cs`에 소리 2차 hunk가 같이 있으면 나눈다(§0).

## 5. 실행 기록

### 5.1 ST-a 코드·에셋 완료 (2026-10-28, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

사용자 "ST-a 진행"(P1~P14 그대로).

- **Ta.1:** 새 `Table/CombatPresentationSettings/Item/CombatStepLook.cs`(+ `.meta`, GUID `333353bc…`). `Walk = 0`·`Hop = 1`.
- **Ta.2:** `CombatPresentationSettingsDataSO.cs`.
  - 칸 셋: `voluntaryStepLook`(코드 기본 `Hop`), `hopHeight`(`Min(0)`, 0.3), `hopSeconds`(`Min(.01)`, 0.25). 자리는 "유닛 이동 연출" 묶음의 `stepClimbHop` 뒤다.
  - 읽기 셋을 더했다.
  - 조건 함수 둘(`ValidateHopHeight`·`ValidateStepLook`)을 더했고, `hopSeconds`는 `ValidateStepSeconds`를 쓴다.
- **Ta.3:** 기본 에셋 `CombatPresentationSettings_Default.asset`에 `voluntaryStepLook: 1`·`hopHeight: 0.3`·`hopSeconds: 0.25`를 넣었다(CRLF 유지, 세 줄 추가).
- **Ta.4:** 새 `Playback/Support/CombatStepLookSettings.cs`(+ `.meta`, GUID `248e92ba…`). 값 객체, `From(settings)`, 정적 `Walk`(P4).
- **Ta.5:** `CombatEventPresentationValues`에 끝 선택 인자 `stepLook`(없으면 `Walk`), 읽기 `StepLook`, `From`이 설정 값을 넘긴다.
- **계획과 다른 점 하나:** Ta.2 ③은 검사를 "`stepClimbHop` 검사 뒤"로 적었다. 그런데 이 파일의 `Validate()`는 "새 값은 맨 뒤에 둔다 — 앞 값들이 함께 잘못됐을 때 먼저 알리는 값이 그대로"를 규칙으로 쓴다(추락·2차 소리). 그래서 걸음 모양 검사 셋도 맨 뒤에 두었다. 동작 차이는 여러 값이 함께 잘못됐을 때 먼저 알리는 값뿐이다.
- **확인한 것:**
  - 고친 두 파일(설정 SO·값 묶음)과 에셋은 CRLF 그대로다(CRLF 아닌 줄 0).
  - 새 파일은 LF다.
  - `CombatEventPresentationValues` 생성자를 부르는 곳은 `From`과 천명님 `PullAndMarkPresentationTests.cs:414`(위치 인자 일곱) 둘뿐이라 그대로 컴파일된다.
  - 새 GUID 둘은 저장소에서 겹치지 않는다.
- **같은 파일의 다른 변경:** `CombatPresentationSettingsDataSO.cs`에는 소리 창구 2차 hunk(커밋 전)가 같이 있다. 커밋 때 나눈다(§0).
- **화면 변화:** 없다. 새 값은 아직 아무도 읽지 않는다(ST-b에서 읽음).
- **Ua:** 문제 없음(사용자, 2026-10-28).

### 5.2 ST-b 코드 완료 (2026-10-28, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

사용자 "Ua 문제 없음, ST-b".

- **Tb.1:** `CombatStepHeight.Arc(from, to, progress, peakAboveHigher)`를 꺼냈다. `At`은 높이가 같으면 시작 높이, 다르면 `Arc(…, hop)`이라 옛 값과 비트 단위로 같다. 클래스 주석에 "걷기 = `At`, 뛰기 = `Arc`"를 적었다.
- **Tb.2~Tb.6 새 파일 다섯(+ `.meta`, 모두 `Presentation/Movement/`, LF):**

  | 파일 | GUID | 내용 |
  |---|---|---|
  | `CombatMovementKind.cs` | `42d360aa…` | `Voluntary = 0`·`Forced = 1` |
  | `CombatStepStyle.cs` | `f7f8f521…` | 추상 모양: `Seconds`·`HeightAt`·`ContactProgress` |
  | `CombatWalkStepStyle.cs` | `3d82fc0b…` | 걷기: `CombatStepHeight.At`·`CombatStepContacts.ContactProgress`(지금 그대로) |
  | `CombatHopStepStyle.cs` | `752958d7…` | 뛰기: `CombatStepHeight.Arc`·접촉 = 1(착지) |
  | `CombatStepStyles.cs` | `71a5584f…` | 인스턴스 표: 종류 배열, 생성자에서 모든 종류 검사, `For(kind)`, 정적 `From(values)` |

  - 모양 생성자는 설정 조건 함수(`ValidateStepSeconds`·`ValidateStepClimbHop`·`ValidateHopHeight`)로 검사한다. 값 묶음과 같은 원본이다.
  - `From`은 걷기 하나를 만들어 밀림에 넣고, 걸음 모양이 `Walk`면 자발 이동에도 같은 걷기를, `Hop`이면 뛰기를 넣는다. 모르는 모양은 예외다.
- **Tb.7:** `ICombatUnitPlacement.Interpolate`의 끝 인자를 `float climbHop` → `CombatStepStyle style`로 바꿨다. `Placement`는 높이를 `style.HeightAt`에 묻는다. null 검사는 하지 않고 계약 주석에 "null이면 안 됨"을 적었다(P12).
- **Tb.8 레인 `CombatUnitMotionPlayback`:**
  - 생성자 `stepSeconds`·`stepClimbHop` → `CombatStepStyles styles`(`turnSeconds`는 남음).
  - `BeginStep(step, CombatMovementKind kind, …)`이 `_style = _styles.For(kind)`, `_kind = kind`를 정하고, 접촉 게이트에는 `kind == Voluntary`를 넘긴다.
  - 걸음 전 회전 조건은 `_kind == Voluntary`, 보간 시간은 `_style.Seconds`, 높이는 `Interpolate(…, _style)`, 접촉 진행률은 `_style.ContactProgress`다.
  - 끌어오기(`BeginPull`)는 `_style = _styles.For(Forced)`이고, `TickPull`은 그 모양의 시간·높이를 쓴다(P10, 반복 합치기 없음).
- **Tb.9:** 레인 집합 `CombatUnitMotionSet` 생성자·`BeginStep`·`Lane()`을 같은 꼴로 바꿨다.
- **Tb.10:** `CombatMovementPresentation.BeginStep`이 종류를 한 곳에서 정한다(`IsVoluntaryMovement` → `Voluntary`/`Forced`, P11). 강조 지우기 조건도 그 값이다. `EndMovement`는 `IsVoluntaryMovement`를 그대로 쓴다.
- **Tb.11:** 사건 Presenter가 `CombatStepStyles.From(values)`를 넘기고 끝 인자 `values.StepClimbHop`을 지웠다.
- **계획과 다른 점(작은 것 셋):**
  - `_kind`도 `_style`처럼 취소 때 비우지 않는다. 걸음·끌어오기를 시작할 때마다 정하고 그동안만 읽기 때문이다. 옛 `_rotateMovement = false` 줄은 지웠다.
  - `BeginStep`은 모양을 맨 먼저 고른다. 모르는 종류면 레인 상태를 바꾸기 전에 예외가 난다.
  - 표 생성자는 열거 값이 음수면 예외다(배열 칸을 정할 수 없어서). 지금 값은 0·1이다.
- **동작:**
  - 밀림(넉백·끌어오기)은 걷기 모양이고 값이 옛것(`movementStepSeconds`·`stepClimbHop`)과 같아 그대로다.
  - 자발 이동은 기본 에셋 값(`Hop`, 0.3, 0.25초)대로 칸마다 한 번 뛴다. `Walk`로 바꾸면 옛것과 같다.
  - 뛰기의 접촉은 착지다. 걸음 시작 때 이미 닿아 있던 원본도 착지에 반응한다(걷기는 시작 즉시였음, 설계 ST5 범위).
- **확인한 것:**
  - 옛 인자(`stepClimbHop`·`_stepSeconds`·`rotate`) 남은 곳 없음. `Interpolate`를 부르는 곳은 레인 두 곳, `CombatUnitMotionSet`을 만드는 곳은 Presenter 하나다.
  - 시험 폴더에 이 형식들을 쓰는 시험이 없다(시험 변경 없음).
  - 고친 CRLF 파일 여섯과 `CombatStepHeight.cs`는 CRLF 아닌 줄 0이다. 새 GUID 다섯은 저장소에서 겹치지 않는다.
- **Ub:** 문제 없음(사용자, 2026-10-28).

### 5.3 ST-c 문서·공유 완료 (2026-10-28, 공유 모두 미전달)

사용자 "Ub 문제 없음, ST-c".

- **Tc.1:** [이동 접촉 설계](../specs/2026-10-17-combat-movement-contact-design.md) M2 끝에 보충 한 줄(접촉 진행률·걸음 시간은 걸음 모양이 정함, 걷기 = 그 절 그대로, 뛰기 = 착지·`hopSeconds`, 밀림은 늘 걷기).
- **Tc.2:** [고지대 설계](../specs/2026-10-20-combat-high-ground-view-design.md) 3단계 끝에 보충 한 줄(높이 식 원본 `CombatStepHeight` 그대로, 걷기 = `At`, 뛰기 = `Arc`, `Placement.Interpolate`는 모양에 묻음).
- **Tc.3:** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) 머리 갱신 줄, §1 트리·§1.1 `Movement/` 줄, §2 "설정 표에서 꺼낸 연출 값 묶음"에 `CombatStepLookSettings`, §2 새 행 "이동 종류마다 다른 걸음 모양"(공격 종류 행과 같은 꼴). CRLF 유지.
- **Tc.4:** [호진님 설정 표 메모](../handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md) §9(칸 셋·검사·에셋·병합 주의). CRLF 유지.
- **Tc.5:** [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §47(천명님 `4bd6b574` `BeginPull`·`TickPull` 바뀐 줄, 동작 같음, 요청 없음). CRLF 유지.
- **Tc.6:** [설계](../specs/2026-10-28-combat-step-motion-style-design.md) 상태 줄·§2 ST4 끝 문장에 정정 표시·새 §7 구현 메모(설계대로 된 것·다르게 한 것·남은 것).
- **계획과 다른 점 하나(문서 정정과 주석 한 줄):** Tc.4는 "열거 0 = Walk(값 없는 에셋은 걷기)"를 적으라고 했는데 사실과 다르다. 칸 코드 기본값이 `Hop`(Ta.2)이고 Unity는 칸이 없는 에셋을 코드 기본값으로 읽어 `Hop`이 된다. `Walk`는 설정 없이 만든 값 묶음(`CombatStepLookSettings.Walk`)뿐이다. 그래서 메모 §9에는 사실대로 적었고, 설계 ST4 문장에 정정 표시를 달았으며, 같은 말을 하던 `CombatStepLook.cs` 문서 주석 한 줄을 고쳤다(코드 동작 변경 없음). 운영 에셋은 `Hop`을 적어 두어 화면 차이는 없다. §4 커밋 묶음의 "반대면 걷기로 남음"도 같은 이유로 "반대면 코드 기본값 `Hop`"이 맞다.
- **Tc.7:** §6.

## 6. 묶음 끝 검토

### 6.1 결과 (2026-10-28, 새 에이전트 하나, 읽기만, 우리 결론 없이 코드만 줌)

- **약 9.6**(SSOT 9.75 · SOLID 9.75 · FW 10 · 독립성 9.25 · 캡슐화 9.75 · C#/Unity 9.75 · If/Tick 9.25). 같은 파일의 기존 문제까지 감점하면 약 9.4.
- 높음 0: 컴파일 오류 후보 없음(접근 제한·같은 어셈블리·using·이름 겹침·옛 서명 남은 곳·천명님 `:414`·새 `.meta` 대조). 요구와 다른 동작 없음(밀림 = 옛 걷기와 비트 단위로 같음, 끌어오기, 칸마다 한 호, 착지 접촉·사건 완료, 먼저 돌기, Walk면 옛 화면).
- 중간 1·낮음 7. 아래 주장은 코드로 다시 대조했다(M1·L3 줄 위치 맞음).

| # | 심각도·기준 | 내용 | 권장 |
|---|---|---|---|
| M1 | 중간·독립성 | 종류의 성질 "스스로 하는 이동인가"(걸음 전 회전, 접촉 게이트 겹침 규칙 1, 앞 행동 강조 지우기, 경로 끝 방향 복귀)가 `== Voluntary` 비교 셋(`CombatUnitMotionPlayback.cs:117·:274`, `CombatMovementPresentation.cs:45`)과 원래 판정 직접 호출 하나(`CombatMovementPresentation.cs:63`)로 네 곳에 흩어졌다. 문서 셋(`CombatStepStyles` remarks, `CombatMovementKind` 요약, 폴더 안내 §2)은 "새 종류 = 값·모양·표 한 줄, 레인·게이트 무변경"이라 하는데, 나중 Leap을 넣으면 레인·이동 표시를 같이 고쳐야 한다. | 성질을 한 곳에(예: 종류 규칙 정적 함수 `IsSelfDriven(kind)`). 레인 비교 둘·강조 지우기·`EndMovement`가 그것을 부르고, `EndMovement`도 같은 종류 판정(이동 표시 안 도우미 하나)을 쓴다. |
| L1 | 낮음·If/Tick | 뛰기는 접촉 = 1로 고정인데, 레인 `ArmContacts`가 이동자 몸 둘·회전·원본 몸을 다 구한 뒤 버린다(걸음당 한 번). | 모양에 "고정 접촉 시점"을 먼저 묻고, 고정이면 몸 조회를 건너뛴다. |
| L2 | 낮음·If/Tick | 같은 값을 한 길에서 세 번 검사(설정 `Validate()` → 값 객체 생성자 → 모양 생성자). 조건 원본은 하나라 SSOT 문제는 아님. | 모양 생성자나 값 객체 쪽 한 겹을 뺀다. |
| L3 | 낮음·If/Tick | `BeginPull`의 `_kind = Forced`는 아무도 읽지 않는 대입이다(읽는 곳은 걸음 시작 회전뿐). | 대입을 지운다. M1을 반영하면 성질 하나만 저장. |
| L4 | 낮음·SSOT | "밀림 = 늘 걷기"가 표(`From`) 말고 문서 다섯 곳에 사본, `CombatStepLook` 문서에 SO 기본값 사본, 검사 문구 "Walk 또는 Hop"이 열거 값 사본, `CombatStepLookSettings.Walk`의 `.25f`는 쓰지 않는 자리 값. | 문서는 원본에 `<see>`만, 검사 문구는 "열거에 있는 값", `.25f`는 쓰지 않는 자리 값임을 드러냄. |
| L5 | 낮음·SOLID | `CombatStepStyles.From`이 값 묶음 전체에 기대고, 걸음 모양 값이 둘로 갈라짐(걷기 시간·climbHop은 값 묶음, 모양·뛰기 값은 `CombatStepLookSettings`). | 걸음 모양 설정 하나로 모으거나 지금은 좁은 인자로 받는다. |
| L6 | 낮음·캡슐화 | 운영 생성자의 `stepLook = null` → `Walk` 선택 인자(새 운영 호출자가 빠뜨리면 조용히 걷기). `fallMotion`과 같은 꼴이고 계획 P4 결정. | 천명님 시험을 고칠 수 있게 되면 필수 인자로. |
| L7 | 낮음·C#/Unity | "열거 → 배열 표" 만들기(음수 거절·빠진 값·범위 검사)가 `CombatAttackStyles`와 거의 같은 사본. | 표가 하나 더 생길 때 공용 도우미로. |

- **기록만(감점 없음, 이번 변경 전부터):** 레인이 걸음·끌어오기·방향 복귀 셋을 맡음, `TickPull` 보간 반복이 `Tick`과 따로, 자발 이동 판정이 적 간격 클래스 `CombatEnemyPresentationPacing`에 있음, 천명님 줄의 0열 XML 주석(`CombatUnitMotionPlayback.cs:180`, `CombatUnitMotionSet.cs:121`).
- **참고(감점 없음):** 자발 이동 판정은 "이번 결과의 행위자이고 공격 전" 추정이다. Core에 이동 도중 강제 이동을 내는 자리(`MovementStepEffects.ForcedMovement` → `RecordForcedMove`)가 있지만 지금 그 값을 만드는 곳은 없다. 생기면 행위자가 자기 이동 중 밀릴 때 걷지 않고 뛴다(옛 코드에서는 회전만 틀렸던 것).
- **예상:** M1·L1·L3·L4 반영 약 9.9, 모두 반영 10 가까이(L6은 다른 사람 시험).

### 6.2 권장 (사용자 결정 대기)

- **반영 권장:** M1·L1·L3·L4. 모두 걸음 모양 파일 안의 작은 변경이고, 동작은 같다. M1은 설계 ST6("새 이동 방식 = 값·모양·표 한 줄")을 실제로 지키게 한다.
- **그대로 권장:** L2(검사가 겹쳐도 원본은 하나이고 비용 없음), L5(값 자리 다시 나누기는 다음 모양이 생길 때 같이), L6(계획 P4 — 천명님 시험 줄이 막음), L7(공용 표 도우미는 셋째 표가 생길 때).

### 6.3 권장 반영 (2026-10-28 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **M1:** 새 `Presentation/Movement/CombatMovementKinds.cs`(+ `.meta`, GUID `af34316b…`) — 종류 성질 원본 `IsSelfDriven(kind)`(Voluntary 참·Forced 거짓·모르는 종류 예외). 부르는 곳:
  - 레인 `BeginStep`이 한 번 묻고 접촉 Gate 인자와 걸음 전 회전 여부(`_selfDriven`)에 쓴다. 필드 `_kind`는 지웠다.
  - 이동 표시: 종류 판정을 도우미 `KindOf(step)` 하나로 두고, 강조 지우기와 `EndMovement`(경로 끝 방향 복귀)가 `IsSelfDriven(KindOf(…))`를 쓴다. `EndMovement`가 판정 함수를 직접 부르던 줄이 없어졌다.
  - 문서: 새 종류 = 값 하나 + 성질 한 줄 + 모양 클래스 + 표 한 줄(`CombatStepStyles` remarks, `CombatMovementKind` 요약, 폴더 안내 §1.1·§2).
- **L1:** 모양에 `TryGetFixedContact(out float)`(추상)를 더했다. 걷기 = 거짓, 뛰기 = 참·착지(상수 `Landing`). 레인 `ArmContacts`가 고정이면 이동자 몸·회전·원본 몸을 구하지 않고 묶음마다 그 값을 넣는다.
- **L3:** `BeginPull`의 `_kind = Forced` 대입을 지웠다(M1로 필드째 없어짐). 끌어오기는 모양만 정한다.
- **L4:**
  - "밀림 = 늘 걷기" 문장을 `CombatMovementKind`·`CombatWalkStepStyle`·`CombatStepStyles` 생성자 인자·`CombatStepLookSettings.Look`·설정 Data 읽기 속성·`CombatStepLook`에서 걷었다. 원본은 `CombatStepStyles.From` 문서 한 곳이다.
  - `CombatStepLook` 문서의 SO 기본값·`Walk` 값 객체 사본(ST-c에 넣었던 두 줄)도 걷었다.
  - 검사 문구 "Walk 또는 Hop" → "열거 CombatStepLook에 없는 값".
  - `CombatStepLookSettings.Walk`의 `.25f` → 이름 있는 자리 값 `UnusedHopHeight`(0)·`UnusedHopSeconds`(1).
  - **남긴 것:** 설정 SO 툴팁의 "넉백·끌어오기 같은 밀림은 늘 걷습니다"는 그대로다. 인스펙터에서 값을 고치는 사람은 코드 문서를 따라갈 수 없어서다.
- **동작:** 같다. `IsSelfDriven`은 옛 `== Voluntary`·`IsVoluntaryMovement`와 같은 답이고, 뛰기의 접촉 진행률은 전처럼 1이다. 걷기 길은 그대로다.
- **확인:** `== CombatMovementKind` 비교·`_kind` 남은 곳 없음, `IsVoluntaryMovement`를 부르는 곳은 간격 계산 안과 `KindOf` 둘, 모양 둘 다 새 멤버 구현. 고친 CRLF 파일 셋은 CRLF 아닌 줄 0, LF 파일은 그대로. 새 GUID는 저장소에서 겹치지 않는다.
- **커밋:** 새 `.cs`가 여덟이 된다(ST-a 둘, ST-b 다섯, 반영 하나, 각 `.meta`).
- **예상 점수:** 약 9.9(검토 예상, M1·L1·L3·L4 반영).

## 7. 추가 요청 — 끌려가는 방향 보기 (2026-10-28, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

사용자 "끌려 올 때 끌려가는 방향 보게 해줘". 설계 Q-ST2("밀림은 걷기 그대로")는 옮기는 모양에 대한 결정이라 그대로 두고, 끌어오기에만 방향을 더했다. 넉백은 그대로 돌지 않는다.

- **레인 `CombatUnitMotionPlayback`(끌어오기 줄은 천명님 `4bd6b574`):**
  - 걸음의 방향 회전 계산을 `TurnToward(from, to, mapper)` 하나로 꺼냈다. 걸음 경로(`StartStep`, 스스로 하는 이동일 때)와 끌어오기가 함께 부른다.
  - `BeginPull` 끝에서 시작 칸 → 도착 칸(`FinalAnchor`) 방향으로 돈다. `TickPull`은 다 돈 뒤 남은 시간부터 옮긴다(걸음처럼 "먼저 돌고 이동", 회전 시간 = `movementTurnSeconds`).
  - 도착하면 `BeginFacingRestore`로 원래 방향(사실 회전)으로 돌아온 다음 완료를 알린다. 걸음 경로 끝 방향 복귀와 같은 장치다. 회전 표시는 그때 지워진다.
- **문서:** `CombatMovementKinds.IsSelfDriven` 요약에 "걸음 사건의 성질이고, 끌어오기 경로는 레인이 따로 정함"을 적었다. Core 전달 문서 §47에 추가 줄·바뀐 동작을 적었다(미전달).
- **정한 것(2026-10-28 사용자 "이대로 유지" — 셋 모두 그대로):**
  - 끌려가기 전에 먼저 돈다(돌면서 미끄러지지 않음). 걸음과 같은 순서이고, 끌어오기 완료가 회전 시간(기본 0.1초)만큼 늦어진다.
  - 도착 뒤 원래 방향으로 돌아온다. 끌어오기는 Core 사실의 방향을 바꾸지 않아서, 걸음 경로 끝처럼 사실 방향으로 돌아오는 것이 맞다. 다 돌아온 뒤 완료라 그만큼(최대 0.1초) 더 늦어진다.
  - 방향은 경로 전체(시작 → 도착) 한 번이다. 칸마다 돌지 않는다(끌어오기 경로는 직선).
- **Unity 확인:** ① 끌어오기 대상이 먼저 끌려가는 쪽(끌어오는 유닛 쪽)을 보고 미끄러진다. ② 도착하면 원래 보던 방향으로 돌아온다. ③ 넉백은 지금처럼 돌지 않는다. ④ 아군·적 경로 이동의 회전·방향 복귀는 그대로다.
