# 전투 투사체 접촉 이펙트 Task 계획

- 작성일: 2026-10-19. 설계: [projectile-impact-effect-design](../specs/2026-10-19-combat-projectile-impact-effect-design.md)(IE1~IE6).
- 결정: **Q-I1~Q-I4 모두 A**(사용자 "Q-I1~Q-I4 모두 A로 해. Task 계획 쓰고 IE-a부터 진행해.").
- Phase마다 멈추고 보고한다. 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- 검증: MSBuild 컴파일(바꾼 파일 오류·새 경고 0)만 하고, 시험은 Unity에서 사용자가 돌린다(관리 실행 전체는 하지 않음).

## 0. 계획에서 정한 것(보고, 설계 뜻은 그대로)

| # | 정한 것 | 이유 |
|---|---|---|
| P1 | 규칙 파일 `DV/CombatPresentationEffectRules.cs`: `IsKnown`(Empty 포함)·`IsPlayable`(Empty 아님 + 정의됨)·문구 `KnownRule`·`PlayableRule`. 클립 값의 `CombatClipAttackData.IsKnownEffect`·`EffectRule`은 **지우고** 호출처를 옮긴다(이름이 둘이면 원본이 둘처럼 보임). 그 문구를 단정하던 시험 한 줄은 새 문구를 가리킨다 | Q-I4 A, SSOT |
| P2 | 창구·폭발 Presenter는 판정 식만 바꾸고 예외 형식(`ArgumentOutOfRangeException`)은 그대로 | 동작 같음 |
| P3 | 공격 표시 값 `CombatAttackPresentationData`는 생성자 끝에 이름 있는 선택 인자 `impactEffect`(기본 Empty)를 받는다. 이 값 클래스는 지금도 검사를 SO에 맡기므로 생성자 검사는 더하지 않는다 | 위치 인자 23개 사이에 끼우지 않음(설계 D6), 그 클래스의 지금 관례 |
| P4 | SO 검사 순서: 종류 → `impactEffect` 정의됨(모든 종류) → 근접이면 Empty만 → 종류별 값. 문구는 기존 `Invalid(필드)` 꼴 | 설계 IE1 |
| P5 | IE-b 접촉 값의 대체 회전(출발점 = 도착점일 때)은 그 애니메이션이 이미 쓰는 공격 방향 회전(직선 `Facing`·곡사 `_facing`·Ballista 공격 방향)을 넘긴다 | `CombatAttackFacing.Toward`의 대체 인자와 같은 뜻 |
| P6 | IE-b 공격 재생 생성자 끝에 이름 있는 선택 인자 `effects`(없으면 접촉 이펙트 없음, 시험 조립). Presenter는 이미 가진 창구를 넘긴다(Factory 무변경) | 설계 IE5 |

## IE-a — 규칙 하나·공격 표시 SO 칸 (화면 변화 없음)

- **T1 규칙** `DV/CombatPresentationEffectRules.cs`(새, `.meta`): P1.
- **T2 호출처 옮기기:** `DV/CombatClipAttackData.cs`(생성자 검사 → `IsKnown`, `IsKnownEffect`·`EffectRule` 삭제), `DV/CombatUnitAnimationSO.cs`(`attackEffect` 검사), `C/Display/CombatEffectWindow.cs`(`Play` → `IsPlayable`), `C/Display/CombatEnvironmentEffectPresenter.cs`(생성자 → `IsPlayable`).
- **T3 SO 칸** `DV/CombatAttackPresentationSO.cs`: `impactEffect`(발사체 머리말 아래, 툴팁 "투사체가 대상에 닿는 순간 그 자리에 내는 이펙트 — 곡사 착탄·직선 몸 접촉·Ballista 대상마다"), `Validate` P4, `CreateData`가 넘김. 값 `DV/CombatAttackPresentationData.cs` `ImpactEffect`·P3.
- **T4 시험:**
  - `T/CombatAttackPresentationSettingsTests.cs`: 기본은 Empty, 정의된 값이 실림(투사체 셋), 정의 안 된 값 거절(모든 종류), 근접에 값이 있으면 거절.
  - 새 `T/CombatPresentationEffectRulesTests.cs`: `IsKnown`·`IsPlayable` 표(Empty·정의·정의 안 됨) — 순수.
  - `T/CombatUnitAnimationSettingsTests.cs`: 문구 단정·주석 한 줄(P1).
- **확인(Unity):** 컴파일, 위 셋 + `CombatEffectWindowTests`·폭발 Presenter 시험·`DataBaseAssetTests`.

## IE-b — 접촉 사실·네 길 연결

- **T5 접촉 값** `C/Display/CombatProjectileContact.cs`(새, `.meta`): `readonly struct`(`Position`·`Rotation`), `Along(from, to, progress, up, fallback)`(progress 0~1로 맞춤, 회전 = `CombatAttackFacing.Toward(from, to, up, fallback)`).
- **T6 애니메이션 맞음 콜백:** `C/Display/CombatMortarAnimation.cs`(발사 때 착탄 접촉 값, 착탄에 넘김), `C/Display/CombatLinearAttackAnimation.cs`(발사 때 `_hitAt` 접촉 값, `SendHit`에 넘김), `C/Display/CombatBallistaAnimation.cs`(대상별 접촉 목록에 값 함께, `_hit(target, contact)`). 세대 확인 그대로.
- **T7 공격 재생** `C/CombatAttackPlayback.cs`: 생성자 `effects`(P6), 함수 `Contacted(settings, contact)`(칸이 비었거나 창구가 없으면 아무것도 안 함), 곡사·직선(직선만, 근접은 그대로)·Ballista 콜백 감싸기, `FireShot` 곡사·직선의 맞음 콜백 감싸기(접촉 값은 발사 때). 순서: 이펙트 → 해제.
- **T8 Presenter** `C/CombatSceneEventPresenter.cs`: 공격 재생에 `effects:` 한 줄.
- **T9 시험:** 애니메이션 시험 도우미 셋(`CombatMortarAnimationTests`·`CombatStandardAttackTests`·`CombatBallistaAttackTests`)의 맞음 콜백 서명, 새 `CombatProjectileContactTests`(위치·회전·대체·progress 맞춤), 장면 시험(곡사 코드 모션 착탄·직선 접촉·Ballista 대상마다·클립 곡사 착탄에 이펙트 한 번씩, 칸이 비면 없음 — 가짜 이펙트는 `FieldRailwayPresenterTests.Effects`).
- **확인(Unity):** 컴파일, 바뀐 시험 묶음 + 기존 공격 연출 시험(`CombatCodeMotionHitTests`·`CombatClipShotTests`·`CombatClipAttackSceneTests`·`CombatMortarPresentationTests`).

## IE-c — 공유·기록

- 호진님 클립 메모(또는 새 메모): 공격 표시 SO `impactEffect` 칸, 등록은 민형님 Render 표(EF-3 요청과 같음).
- 계획 실행 기록, CLAUDE.md, 폴더 정리 대응표(새 파일 둘).

## 실행 기록

### IE-a (2026-10-19, 사용자 "Q-I1~Q-I4 모두 A로 해. Task 계획 쓰고 IE-a부터 진행해.")

- **T1:** 새 `DV/CombatPresentationEffectRules.cs`(+ `.meta`) — `IsKnown`·`IsPlayable`·`KnownRule`·`PlayableRule`.
- **T2:** `DV/CombatClipAttackData.cs` 생성자가 `IsKnown`·`KnownRule`을 부르고 `IsKnownEffect`·`EffectRule`은 지움(P1), `DV/CombatUnitAnimationSO.cs` `attackEffect` 검사도 같은 규칙, `C/Display/CombatEffectWindow.cs` `Play`·`C/Display/CombatEnvironmentEffectPresenter.cs` 생성자가 `IsPlayable`(예외 형식 그대로, 문구는 `PlayableRule` — P2).
- **T3:** `DV/CombatAttackPresentationSO.cs` 칸 `impactEffect`(발사체 머리말 아래, 기본 Empty)·검사(정의됨은 모든 종류, 근접은 Empty만 — P4)·`CreateData`가 이름 있는 인자로 넘김, `DV/CombatAttackPresentationData.cs` `ImpactEffect`·생성자 끝 선택 인자(P3).
- **T4 시험:** `CombatAttackPresentationSettingsTests` 새 3개(사례 8 — 기본 Empty·투사체 셋에 값이 실림, 정의 안 된 값 네 종류 모두 거절, 근접에 값 거절, Unity 전용), 새 `CombatPresentationEffectRulesTests`(+ `.meta`, 사례 5, 순수), `CombatUnitAnimationSettingsTests` 문구 단정·주석 한 줄(P1의 직접 결과).
- **검증:** MSBuild(`ProjectL.Tests.Framework`, 새 시험 파일만 추가 targets) 오류 0·바꾼 파일 새 경고 0, 산출 DLL에 새 형식 있음. 관리 실행 `CombatPresentationEffectRulesTests` 5 PASS(순수). 나머지는 Unity 전용.
- **계획과 다른 점:** 없음.
- **화면:** 변화 없음(기존 에셋은 칸이 없어 Empty, 판정 식 바뀐 곳은 동작 같음).
- **원칙 자체 점검(7기준):** SSOT 9.5(이펙트 종류 판정 네 곳 → 규칙 하나, EF L1 해소) · SOLID 9 · FW 9 · 독립성 9(기존 시험 고침은 문구 한 줄) · 캡슐화 9 · C#·Unity 9(끝 이름 있는 인자) · If·Tick 9.5(새 실행 경로 없음) = 약 9.1.
- **커밋 때(각 `.meta`):** 새 `DV/CombatPresentationEffectRules.cs`·`T/CombatPresentationEffectRulesTests.cs`. 고친 `DV/CombatClipAttackData.cs`·`DV/CombatUnitAnimationSO.cs`(커밋 전 클립 묶음과 함께)·`DV/CombatAttackPresentationSO.cs`·`DV/CombatAttackPresentationData.cs`·`C/Display/CombatEffectWindow.cs`·`C/Display/CombatEnvironmentEffectPresenter.cs`, 시험 두 파일.
- **Unity 확인 대기:** 컴파일, `CombatAttackPresentationSettingsTests`·`CombatPresentationEffectRulesTests`·`CombatUnitAnimationSettingsTests`·`CombatEffectWindowTests`·폭발 Presenter 시험(`CombatEnvironmentEffectPresenterTests`)·`DataBaseAssetTests`.
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 IE-a 시험 문제 없음 확인했어. IE-b 진행해."):** IE-a 문제 없음.

### IE-b (2026-10-19, 사용자 "Unity에서 컴파일과 IE-a 시험 문제 없음 확인했어. IE-b 진행해.")

- **T5:** 새 `C/Display/CombatProjectileContact.cs`(+ `.meta`) — `readonly struct`(`Position`·`Rotation`), `Along(from, to, progress, up, fallback)`(위치 = `Vector3.Lerp`(0~1로 맞춤), 회전 = `CombatAttackFacing.Toward`, 진행률이 유한하지 않으면 예외).
- **T6:** `C/Display/CombatMortarAnimation.cs`(발사 때 착탄 자리 `Along(from, 목표, 1, 위쪽, _facing)`, 착탄 히트가 넘김), `C/Display/CombatLinearAttackAnimation.cs`(발사 때 `_hitAt` 자리를 필드에 두고 `SendHit`이 넘김, 대체 = `Facing`), `C/Display/CombatBallistaAnimation.cs`(대상별 접촉 목록에 자리를 함께, `_hit(target, contact)`). 세대 확인은 그대로라 늦은 접촉엔 콜백이 없음.
- **T7:** `C/CombatAttackPlayback.cs` — 생성자 끝 `effects`(P6), `PlayImpact(settings, contact)`(칸·창구가 있을 때만 — "닿으면 무엇을 내나"의 한 곳), 감싸기 `ImpactThen`(코드 모션 = 접촉 자리를 받는 콜백, 출처 발사 = 발사 때 만든 자리를 담은 콜백). 곡사 코드 모션·직선 코드 모션(근접은 그대로)·Ballista(세대 확인 뒤 이펙트 → 그 대상 해제)·`FireShot` 곡사·직선. 순서는 이펙트 → 해제.
- **T8:** `C/CombatSceneEventPresenter.cs` 공격 재생 생성에 `effects: effects` 한 줄(Factory 무변경).
- **T9 시험:**
  - 고친 것: `CombatBallistaAttackTests` 실패 시험의 맞음 람다 서명 한 곳(`_ => {}` → `(_, _) => {}`). 곡사·직선 도우미는 `null`이라 그대로.
  - 장면 시험 셋의 SetUp에 가짜 이펙트 창구(`FieldRailwayPresenterTests.Effects`)를 Presenter에 넘기고 TearDown에서 정리(칸이 비면 아무것도 안 함 — 기존 사례 영향 없음).
  - 새: `CombatProjectileContactTests`(+ `.meta`, 4개·사례 10 — 진행률 지점·맞춤, 수평 진행 방향, 대체 회전, 유한하지 않은 진행률), `CombatStandardAttackTests` 2개(직선 접촉에 한 번·진행 방향 / 칸이 비면 직선·근접 모두 없음), `CombatMortarPresentationTests` 1개(곡사 코드 모션 착탄점에 한 번), `CombatBallistaAttackTests` 1개(관통 대상마다 한 번·경로 순서·같은 방향·경로 끝엔 없음), `CombatClipAttackSceneTests` 1개(클립 곡사 착탄점에 한 번, `Arrange`에 `impact` 인자). 모두 Unity 전용.
- **검증:** MSBuild(`ProjectL.Tests.Framework`, 새 시험 파일 둘을 추가 targets) 오류 0·바꾼 파일 새 경고 0, 산출 DLL에 새 형식 있음. 관리 실행은 해당 없음(새·바뀐 시험 모두 네이티브).
- **계획과 다른 점:** 없음. (계획 T7의 함수 이름 `Contacted`는 `PlayImpact` + 감싸기 `ImpactThen` 둘로 나눔 — 출처 발사는 접촉 자리를 발사 때 담아야 해서 감싸기 모양이 둘.)
- **원칙 자체 점검(7기준):** SSOT 9(접촉 이펙트 종류 = 공격 표시 SO 한 칸, 접촉 자리 = `Along` 하나·회전은 `Toward`, "닿으면 무엇을 내나" = `PlayImpact` 하나) · SOLID 8.5(애니메이션 = 사실, 공격 재생 = 결정. 공격 재생 533 → 575줄(대부분 문서 주석) — K2 관찰 계속) · FW 9(Factory·풀 무변경, 프레임 할당 0 — 자리 값은 발사 때 한 번, 감싸기 람다는 발사 때) · 독립성 9(이펙트 추가 = SO 칸 값만, 코드 모션·클립 두 길 공통) · 캡슐화 9 · C#·Unity 8.5(이펙트 등록 누락은 첫 재생 때 드러남 — EF와 같은 Framework 정책) · If·Tick 9(새 Tick 없음, 접촉마다 칸 확인 한 번) = **약 8.9**(설계 예상과 같음).
- **화면:** 공격 표시 SO의 `impactEffect`를 채운 유닛만 접촉 순간 이펙트가 나옴. 지금 에셋은 모두 Empty라 변화 없음. 실제로 보려면 Render 표에 등록된 이펙트(지금 `TestFire`·`Boom`)를 칸에 넣는다.
- **커밋 때(각 `.meta`):** 새 `C/Display/CombatProjectileContact.cs`·`T/CombatProjectileContactTests.cs`. 고친 `C/Display/CombatMortarAnimation.cs`·`CombatLinearAttackAnimation.cs`·`CombatBallistaAnimation.cs`·`C/CombatAttackPlayback.cs`·`C/CombatSceneEventPresenter.cs`, 시험 `CombatStandardAttackTests`·`CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatClipAttackSceneTests`(클립 묶음과 함께). IE-a 묶음이 커밋 전이면 함께.
- **Unity 확인 대기:** 컴파일, `CombatProjectileContactTests`·`CombatStandardAttackTests`·`CombatBallistaAttackTests`·`CombatMortarPresentationTests`·`CombatClipAttackSceneTests`·`CombatMortarAnimationTests`·`CombatCodeMotionHitTests`·`CombatClipShotTests`·`CombatEffectWindowTests`.
- **Unity 확인 결과(2026-10-19, 사용자 "Unity에서 컴파일과 IE-b 시험 문제 없음 확인했어. IE-c 진행해."):** IE-b 문제 없음.

### IE-c (2026-10-19, 사용자 "Unity에서 컴파일과 IE-b 시험 문제 없음 확인했어. IE-c 진행해.") — 투사체 접촉 이펙트 작업(IE-a~IE-c) 끝

- 문서만(코드 변경 없음).
- **공유(미전달):** 호진님 클립 메모 `handoffs/2026-10-19-unit-animation-clip-hojin-share.md` §9(공격 표시 SO `impactEffect` — 곡사 착탄·직선 접촉·Ballista 대상마다, 수평 진행 방향, 근접은 Empty만, 규칙 한 곳 `CombatPresentationEffectRules`·옛 `IsKnownEffect` 삭제, 재생 확인 방법), 민형님 Factory 공유 §19에 한 줄(전투용 `EffectType` 등록 요청 범위가 착탄·관통까지 넓어짐, Factory는 이번에 무변경).
- **폴더 정리 대응표:** `Presentation/Attack/` 줄에 `CombatProjectileContact*`. `DV/CombatPresentationEffectRules`는 폴더 정리 범위(`ViewSystem/Combat/`) 밖이라 줄 없음.
- **남은 것(코드 밖):** 민형님 Render 표에 전투용 이펙트 등록 → 공격 표시 SO의 `impactEffect`에 지정 → 실제 재생 확인(착탄·관통 자리·방향).
- **커밋 묶음(IE 전체, 각 `.meta`):** 새 `DV/CombatPresentationEffectRules.cs`·`C/Display/CombatProjectileContact.cs`·`T/CombatPresentationEffectRulesTests.cs`·`T/CombatProjectileContactTests.cs`. 고친 파일은 IE-a·IE-b 기록의 목록 합. `CombatClipAttackData.cs`·`CombatUnitAnimationSO.cs`·`CombatClipAttackSceneTests.cs`는 커밋 전 클립 묶음과 함께.
