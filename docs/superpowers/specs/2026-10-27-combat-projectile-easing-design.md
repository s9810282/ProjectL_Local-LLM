# 전투 투사체 완급 곡선 설계 (EZ1~EZ5)

> - **2026-10-27 작성.** EZ-a 코드 완료(컴파일 미확인, 새 시험 없음). 기록은 §7.
> - **요청:** 사용자 "발사체에 Speed와 Duration이 있잖아, 여기에 Curve 같은 걸로 완급 조절하고 싶은데".
> - **결정 완료(2026-10-27 사용자 "Q-EZ1~Q-EZ3 모두 A로 하고, P1~P3도 그대로 해서 EZ-a 진행해"):** Q-EZ1~Q-EZ3 모두 A, P1~P3 그대로.
> - 작업이 작아서 Task는 계획 문서를 따로 두지 않고 §6에 함께 적는다. Phase는 EZ-a 하나다.
> - 진행 규칙은 전투 View 상시 지시를 따른다(MSBuild 없음, 새 `[Test]` 없음, 커밋은 사용자).

## 1. 지금 구조 (코드 확인)

- 투사체 위치는 발사체 풀 `CombatProjectilePool.Advance` 한 곳에서 정한다.
  - 시간 비율 `t = Elapsed / Duration`이다.
  - 위치는 `CombatShotPath.ArcPoint(from, to, up, height, t)`이다. 직선은 높이가 0이다.
  - 진행 알림 `Advanced(t)`도 같은 `t`를 넘긴다.
- 비행 시간을 정하는 값은 종류마다 하나다.
  - 직선: `projectileSpeed`. 시간 = 거리 / 속도.
  - 곡사: `flightDuration`.
- 이 풀을 부르는 곳은 넷이다. 네 길 모두 같은 `Advance`를 지난다.
  - 클립 길: 직선 `CombatLinearStyle.FireShot`, 곡사 `CombatMortarStyle.FireShot`. 투사체 진행 `CombatProjectileFlights`를 거친다.
  - 코드 모션 길: 직선 `CombatLinearAttackAnimation`, 곡사 `CombatMortarAnimation`.
- 맞는 순간
  - 직선은 "경로 진행률이 접촉 진행률(`hitAt`)을 넘는 순간"이다. 접촉 진행률은 경로 위 기하(몸과 닿는 지점)로 정한다.
  - 곡사는 도착(`t = 1`)이다.
- 궤적 미리보기(`CombatAttackTrajectory`)는 실제 발사와 같은 기하 함수(`CombatShotPath`)로 경로 모양을 그린다(공격 궤적 설계 TJ3). 시간은 쓰지 않는다.

## 2. 요지

- **EZ1 곡선 하나 = "시간 비율 → 경로 진행률".** 공격 표시 SO의 발사체 칸에 `projectileEasing`(AnimationCurve)을 하나 더한다.
  - 풀 `Advance`가 `p = 곡선(t)`를 구한다. 위치·진행 알림은 `t` 대신 `p`를 쓴다.
  - 경로 모양은 그대로이고, 같은 길을 가는 빠르기만 바뀐다.
  - 비행 시간은 그대로다. 직선 `projectileSpeed`는 "평균 속도"가 되고, 곡사 `flightDuration`은 그대로 전체 시간이다.
  - 회전(`spinDegreesPerSecond`)은 지금처럼 시간으로 돈다(P1).
- **EZ2 기본값 = 등속.** 칸 초기값이 `AnimationCurve.Linear(0, 0, 1, 1)`이다.
  - YAML에 칸이 없는 에셋 다섯은 Unity가 초기값으로 읽어서 지금 화면과 같다. 에셋을 고칠 필요가 없다.
- **EZ3 값 객체는 곡선을 감춘다.** DB 쪽 새 값 `CombatProjectileEasing`(readonly struct)이 곡선 사본을 갖고 `Evaluate(t)`만 내준다.
  - AnimationCurve는 바뀔 수 있는 참조 형식이다. 그래서 SO의 곡선을 그대로 넘기지 않고, `CreateData` 때 키를 복사한다(P2 "매번 새 불변 값").
  - `default`는 등속이다(곡선 없음 = `t` 그대로).
  - 끝은 정확히 맞춘다. `t ≤ 0`이면 0, `t ≥ 1`이면 1을 돌려준다. 그래서 곡사가 늘 목표 칸에 정확히 떨어진다.
- **EZ4 맞는 순간은 자동으로 맞는다.**
  - 직선의 접촉 진행률은 경로 위 위치이고, 이제 진행 알림도 경로 진행률(`p`)이다. 그래서 "몸에 닿는 순간 맞음"이 곡선과 상관없이 지켜진다. 맞는 시각만 곡선을 따라 옮겨진다.
  - 곡사는 지금처럼 도착이 맞음이고, 비행 시간이 같아 맞는 시각도 같다.
  - 소리·접촉 이펙트·Gate는 맞음 콜백에 매여 있어 함께 따라간다.
- **EZ5 궤적 미리보기는 그대로다.** 모양이 같아서 그리는 선이 실제 경로와 계속 같다(TJ3 유지).

## 3. 바꾸는 곳

| 층 | 파일 | 바꾸는 것 |
|---|---|---|
| DB 입력 | `DV/CombatAttackPresentationSO.cs` | "발사체" 머리 아래 `projectileEasing` 칸(+ 툴팁), 투사체 종류(직선·곡사)일 때 검사(§5 Q-EZ3), `CreateData`가 값으로 바꿔 넘김 |
| DB 값 | 새 `DV/CombatProjectileEasing.cs`(+ `.meta`) | readonly struct. 곡선 사본, `Evaluate(t)`, 검사 함수(규칙 원본 한 곳) |
| DB 값 | `DV/CombatAttackPresentationData.cs` | `ProjectileEasing` 속성, 생성자 끝 선택 인자(`default` = 등속) |
| View 풀 | `C/Presentation/Attack/CombatProjectilePool.cs` | `FireLinear`(진행 알림 있는 판)·`FireArc` 끝에 선택 인자 `easing = default`, `Flight`에 보관, `Advance`에서 `p`. 진행 알림 없는 `FireLinear`는 시험만 써서 등속 그대로(§7.1, 검토 L4로 표를 고침) |
| View 진행 | `C/Presentation/Attack/CombatProjectileFlights.cs` | `FireArc`·`FireLinear`(·내부 `FireThrough`)가 곡선을 받아 풀에 넘김 |
| View 맥락 | `C/Presentation/Attack/ICombatAttackStyleContext.cs`, `CombatAttackPlayback.cs` | 맥락 `FireArc`·`FireLinear`에 곡선 인자, 명시 구현이 넘김 |
| View 전략 | `CombatLinearStyle.cs`, `CombatMortarStyle.cs` | `settings.ProjectileEasing`을 넘김 |
| View 코드 모션 | `CombatLinearAttackAnimation.cs`, `CombatMortarAnimation.cs` | 풀 호출에 `_settings.ProjectileEasing`을 넘김 |

- 풀의 곡선 인자를 선택 인자(`default` = 등속)로 두는 이유
  - 풀을 직접 부르는 우리 시험 호출이 약 40곳(`CombatProjectileTests`·`CombatMortarAnimationTests`)이다.
  - 등속은 "시험 때만 비우는 값"이 아니라 정상 값이다. 그래서 운영 null 허용(3·4라운드에서 걷은 꼴)과 다르다.
  - 위의 운영 경로 넷은 모두 값을 명시해서 넘긴다.
- 맥락 `ICombatAttackStyleContext`의 곡선 인자는 필수다. 가짜 구현이 없어(구현은 공격 재생 하나) 시험 영향이 없다.
- 시험: 새 시험은 없다. 기존 시험은 기본값이 등속이라 기대값이 바뀌지 않는다. 컴파일이 깨지는 호출도 없을 것으로 본다(값 객체·풀의 새 인자가 모두 끝 선택 인자).
- 공유: 없다. 바꾸는 줄은 모두 우리 것이다(투사체 진행의 호진님 한 줄은 `using` 한 줄뿐이고 고치지 않는다).

## 4. 범용 구조 계획(G-b)과의 관계

- G-b는 공격 묶음을 투사체 부품 SO(`CombatProjectileSO` 기반 + 직선·곡사)와 코드 모션 부품으로 나눈다(P1~P18 확인 대기).
- 완급 곡선은 직선·곡사 모두의 성질이라, G-b에서는 **투사체 부품 기반 SO**로 옮긴다. 값 객체 모양(평평한 값, P1)은 같아서 소비자는 그대로다.
- 지금 넣고 기획자가 G-b 전에 곡선을 고치면, G-b의 에셋 이행(Gb.8)이 곡선 YAML도 옮겨야 한다. 이 줄을 G-b 대응표에 더한다(§6 EZa.6).

## 5. 정할 것

- **Q-EZ1 언제·어디에 넣나**
  - **A(권장): 지금 공격 표시 SO에 넣고, G-b에서 투사체 부품 SO로 옮긴다.**
    - 바로 조절해 볼 수 있다. 옮기는 비용은 칸 하나·대응표 한 줄이다.
  - B: G-b에서 투사체 부품 SO를 만들 때 함께 넣는다. 중복 작업은 없지만, G-b 확인(P1~P18)과 G-0~G-a가 끝날 때까지 못 쓴다.
- **Q-EZ2 곡선이 무엇을 바꾸나**
  - **A(권장): 경로 진행률 하나(모양 그대로, 빠르기만).**
    - 곡사의 높이도 같은 진행률을 따라서, 궤적 미리보기가 실제 경로와 계속 같다(TJ3).
    - 꼭대기에서 머무는 느낌은 곡선 가운데를 눕히면 난다.
  - B: 수평 이동만 곡선, 높이는 시간. 같은 곡선이라도 곡사 모양이 바뀌어 미리보기 선과 실제 경로가 어긋난다. 미리보기도 곡선을 알아야 한다.
- **Q-EZ3 곡선 검사**
  - **A(권장): 부트(DB 생성)에서 거절한다.** 거절하는 경우는 다음과 같다.
    - 첫 키가 (0, 0)이 아니거나 끝 키가 (1, 1)이 아니다.
    - 0~1 사이 표본(64개)에서 값이 줄어든다.
    - 값이 0~1 밖이거나 유한하지 않다.
    - 이유: 맞는 순간이 "진행률이 접촉 지점을 한 번 넘는 순간"이라, 되돌아가거나 목표를 지나치는 곡선은 맞음이 일찍 나거나 위치가 목표를 벗어난다.
  - B: 끝 키만 검사하고, 실행 중 값은 0~1로 자른다. 되돌아가는 곡선은 화면에서 이상하게 보여도 오류가 나지 않는다.
  - C: 검사하지 않는다.
- **계획에서 정한 것(확인 대상)**
  - P1: 회전은 시간으로 돈다(지금 그대로). 진행률로 돌리면 느린 구간에서 회전도 멈춰 보인다.
  - P2: 근접 공격 표시는 이 칸을 검사하지 않는다(다른 투사체 칸과 같은 규칙).
  - P3: 표본 검사 사이(키 사이 접선)에서 아주 조금 넘는 값을 막으려고, 실행 중에도 0~1로 자른다. 검사는 의도를, 자르기는 수치 안전을 맡는다.

## 6. Task (Phase EZ-a)

| Task | 할 일 |
|---|---|
| EZa.1 | 값 `CombatProjectileEasing` + 검사 함수(Q-EZ3) |
| EZa.2 | SO 칸·툴팁·검사·`CreateData`, 값 객체 속성·생성자 끝 선택 인자 |
| EZa.3 | 풀: 세 발사 함수 끝 선택 인자, `Flight` 보관, `Advance`에서 `p`(위치·진행 알림), 회전은 시간 |
| EZa.4 | 투사체 진행·맥락·공격 재생 명시 구현·전략 둘·코드 모션 둘이 곡선을 넘김 |
| EZa.5 | 시험: 컴파일이 깨지는 호출만 맞춘다(예상 없음). 정적 검토 하나 |
| EZa.6 | 문서: 범용 구조 계획 Gb.1·Gb.8 대응표에 곡선 한 줄, CLAUDE.md 현재 상태 |

- **Unity 확인(사용자)**
  1. 컴파일
  2. 기본 곡선 그대로일 때 화면이 지금과 같은지
  3. 곡사 에셋 하나(예: `CombatAttack_UndeadCannon`)·직선 에셋 하나(`CombatAttack_Linear`)에 ease-in·ease-out 곡선을 넣고, 착탄·명중 순간이 투사체가 닿을 때 나는지
  4. 되돌아가는 곡선을 넣으면 부트가 거절하는지(Q-EZ3 A일 때)
  5. 궤적 미리보기가 실제 경로와 같은지
- **커밋:** 새 `CombatProjectileEasing.cs`(+ `.meta`)와 바꾼 코드는 한 커밋이다. 에셋은 바꾸지 않는다.
- **예상 점수(7기준, 알려진 문제 기준):** 바꾸는 범위는 그대로이고, 곡선 적용이 한 곳(풀 `Advance`)뿐이라 구조 점수는 떨어지지 않는다. 새 문제는 G-b 이행 대상 칸이 하나 느는 것뿐이다.

## 7. 실행 기록

### 7.1 EZ-a 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

- **새 파일:** `DV/CombatProjectileEasing.cs`(+ `.meta`)
  - readonly struct다. 곡선 사본·`Evaluate(t)`·`Linear`(= `default`)와 입구 `TryFrom(curve, out easing, out error)`를 둔다. 곡선 규칙의 원본은 `TryFrom` 하나다(검사는 private `FindError`).
  - `Evaluate`는 `t ≤ 0`이면 0, `t ≥ 1`이면 정확히 1이고, 사이 값은 0~1로 자른다(P3).
  - 검사(Q-EZ3 A): 키 둘 이상, 키 값 유한, 첫 키 (0,0)·끝 키 (1,1)(허용 오차 0.0001), 표본 65개(0~64/64)가 0~1 안이고 줄지 않음.
- **계획과 다른 점 하나: 기본 곡선은 등속으로 읽는다.**
  - `TryFrom`이 `AnimationCurve.Linear(0, 0, 1, 1)`과 키가 정확히 같은 곡선(두 키, 접선 1, 무게 없음)을 `Linear`로 바꾼다.
  - 이유: 곡선 계산이 `t`를 비트 단위로 똑같이 돌려준다는 보장이 없다. 그러면 정확한 위치를 비교하는 기존 시험이 흔들릴 수 있다. 기본값이면 옛 등속 식을 그대로 타게 했다.
- **SO** `CombatAttackPresentationSO`
  - "발사체" 머리 아래 `projectileEasing` 칸을 더했다. 초기값은 직선이다. 툴팁에는 규칙과 "일찍 1에 닿으면 남은 시간 동안 멈춤"을 적었다. `projectileSpeed` 툴팁은 "평균 이동 거리"로 고쳤다.
  - `CreateData`가 `ReadEasing`으로 한 번 읽는다. 근접이면 `Linear`이고(P2), 투사체 종류(직선·곡사)는 `TryFrom`으로 검사한다. 잘못된 곡선은 부트(DB 생성)에서 `Invalid projectileEasing: <이유>`로 멈춘다.
- **값 객체** `CombatAttackPresentationData`: 끝 선택 인자 `projectileEasing = default`와 속성 `ProjectileEasing`.
- **발사체 풀** `CombatProjectilePool`
  - `FireLinear`(진행 알림 있는 판)·`FireArc` 끝에 선택 인자 `easing = default`를 붙였다. 진행 알림 없는 `FireLinear`(시험만 씀)는 등속 그대로다.
  - `Flight.Easing`에 보관한다. `Advance`는 `progress = Easing.Evaluate(t)`로 위치(`ArcPoint`)와 진행 알림을 정한다. 회전(`Elapsed`)과 끝 판정(`t < 1`)은 시간이다(P1).
- **넘기는 곳**
  - 투사체 진행 `FireArc`·`FireLinear`·내부 `FireThrough`, 맥락 `ICombatAttackStyleContext`(필수 인자), 공격 재생 명시 구현.
  - 전략 `CombatLinearStyle`·`CombatMortarStyle`의 `FireShot`(클립 길), 코드 모션 `CombatLinearAttackAnimation`·`CombatMortarAnimation`.
- **시험:** 고친 것이 없다.
  - 맥락 가짜가 없고, 투사체 진행·값 객체 생성자를 직접 부르는 시험도 없다.
  - 풀을 직접 부르는 시험(`CombatProjectileTests`·`CombatMortarAnimationTests`)은 선택 인자라 그대로다.
  - SO로 값을 만드는 시험은 초기값(직선 → 등속)을 받는다.
- **공유:** 없다. 바꾼 줄이 모두 우리 것이다.
- **문서:** 범용 구조 계획 Gb.1(투사체 부품 기반 SO에 곡선)·Gb.8 대응표 아래 줄(곡선 옮기기·대조), CLAUDE.md 현재 상태.
- **커밋:** 새 `CombatProjectileEasing.cs`(+ `.meta`)와 바꾼 코드 열 파일은 한 커밋이다. 에셋은 Unity가 다음 저장 때 칸을 써 넣는다(값은 기본 직선).
  - 바꾼 코드 중 일곱(`CombatAttackPresentationSO`·`CombatAttackPresentationData`·`ICombatAttackStyleContext`·`CombatAttackPlayback`·`CombatProjectileFlights`·`CombatLinearStyle`·`CombatMortarStyle`)에는 같은 날 커밋 전인 Ballista 걷기 hunk가 섞여 있다. 두 작업을 한 커밋에 넣는 편이 쉽다.
- **Unity 확인(사용자):** §6 목록 다섯.

### 7.2 묶음 끝 검토 (2026-10-27, 정적 검토 에이전트 하나, 빌드·시험 안 돌림)

- **높음·중간 0.**
  - 컴파일: 서명 다섯 곳 일치, 오버로드 모호함·접근성·using 문제 없음.
  - 기본 곡선이면 위치·진행 알림·완료·맞는 시점이 옛것과 비트 단위로 같다. Unity `Linear`의 접선 값이 `IsIdentity`와 맞고, YAML에 칸이 없는 에셋은 초기값을 쓴다.
  - 직선이 아닌 곡선의 맞는 시점: 두 직선 길 모두 경로 진행률끼리 비교하고, 원시 `t`가 남은 곳은 회전·끝 판정뿐이다.
  - 기존 시험 중 실행에서 깨질 곳은 없다.
- **낮음 넷**
  - **L1 → 반영:** 공격 표시 값은 읽을 때마다 `CreateData`가 돈다(공격 사건·궤적 응답마다, 범용 구조 G-a Ga.4가 보관으로 고칠 기존 성질). 그런데 곡선 검사가 `Validate`와 `From`에서 두 번 돌았다. 그래서 입구를 `TryFrom` 하나로 바꿔 검사·키 읽기를 한 번으로 줄였다(`From`·공개 `FindError` 삭제, SO `ReadEasing`).
  - **L2 → 툴팁으로 반영:** 끝보다 먼저 1에 닿고 평평한 곡선은 통과한다. 직선은 일찍 맞고 목표 위에 머물고, 곡사는 착탄 자리에 멈춘 채 비행 시간이 끝날 때 맞는다. 규칙은 그대로 두고 툴팁에 적었다.
  - **L3 → 그대로:** 표본 사이의 좁은 되돌림은 잡지 못한다(설계 P3의 한계). 맞음은 커서가 한 번만 내서 화면 문제에 그친다.
  - **L4 → 반영:** §3 표의 "`FireLinear` 둘"을 실제(진행 알림 있는 판 하나)로 고쳤다.
- 반영 뒤 다시 검토하지 않았다(검토는 묶음 끝 한 번).
