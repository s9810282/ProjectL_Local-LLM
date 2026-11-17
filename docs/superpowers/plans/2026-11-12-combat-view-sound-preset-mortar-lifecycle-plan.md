# 전투 View 소리 프리셋 원본·곡사 공통 수명 Task 계획 (2026-11-12)

> 설계: [sound-preset-mortar-lifecycle-design](../specs/2026-11-12-combat-view-sound-preset-mortar-lifecycle-design.md)(SP1~SP5·MT1~MT5).
> 결정(2026-11-12 사용자 "권장안대로 하고 Task 계획 써줘, 기존 12기준 생각해서 플랜짜"): Q-SP1~Q-SP4·Q-MT1~Q-MT3 모두 A.
> 기준 판 `03d3555aa`. TP 그대로(2026-11-12 사용자 "그대로, SM-0 진행해"). **SM-0 완료(§10.1), SM-a 코드 완료(§10.2)·SM-b 코드 완료(§10.3), 둘 다 컴파일 미확인. SM-c 완료(§10.4), 묶음 끝 검토 낮음 L1·L2·L3·L5 반영(§10.5, 컴파일 미확인).**
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`. 줄 번호는 기준 판이다.

## 진행 규칙

- Phase가 끝나면 멈추고 보고한다(CLAUDE.md).
- 새 시험은 쓰지 않는다. MSBuild는 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
- 기존 시험은 바꾸지 않는다. `Assets/Tests`에서 바뀌는 형식·함수를 부르는 곳은 0곳이다(11-12 grep, SM-0에서 다시 셈).
- Unity 확인은 모든 Phase 뒤 한 번에 한다(§8).
- 다른 작업자 줄은 고친 뒤 공유 메모로 넘긴다(§9). 공유는 모두 미전달로 쓴다.
- **12기준 규칙(사용자 지시):** 바꾸는 파일 안에 재평가 문제 목록의 낮음이 있으면 같이 고칠지 TP로 묻는다. 바꾸는 파일 밖 문제는 손대지 않고 §2.3에 "다음 후보"로 적는다. Task마다 12기준 중 새 문제가 생길 수 있는 곳을 확인 줄로 적는다(§2).

## 0. 계획에서 정한 것 (TP, 사용자 확인 대상)

**설계와 다르거나 설계에 없던 것은 굵게** 적었다.

| 번호 | 내용 | 기준 |
|---|---|---|
| TP-1 | 진입 목록 칸은 `List<int> weakGroundEntrySoundPresets = new() { 1, 2 }`이다. 에셋 YAML은 다른 정수 목록 칸(`StageOfferText_Training.asset`의 `stageKeys`)과 같은 줄 목록 꼴로 쓴다 | C6 |
| **TP-2** | Data는 진입 목록을 **복사본으로만** 내준다: `internal int[] CopyWeakGroundEntrySoundPresets()`(부를 때마다 새 배열). 설계는 "읽기 전용 목록을 낸다"였다. `IReadOnlyList<int>`로 직렬화 목록을 그대로 내주면 받는 쪽이 고칠 수 있어서(C8) 바꿨다. 부르는 곳은 프리셋 객체 생성자 하나라 할당은 View 서비스 수명에 한 번이다(C11, 11-13 §10.4 L1로 "전투마다"를 고침) | C8·C11 |
| TP-3 | 칸 순서: 적 공용 → 배경음 셋 → **진입 목록 → 붕괴** → (새 머리말) **추락**. 검사 순서는 지금 줄(추락 → 붕괴)을 그대로 두고 진입 목록 검사를 그 뒤에 더한다. "앞 값들이 함께 잘못됐을 때 먼저 알리는 값이 그대로"라는 파일 규칙(292줄)을 지킨다 | C10 |
| TP-4 | 새 머리말은 `[Header("전투 소리 프리셋 — -1이면 프리셋 없이 냄 (소리 프리셋 원본 설계 SP3)")]`이다. 추락 칸 하나만 그 아래에 둔다 | C12 |
| TP-5 | `CombatSoundPresets.WeakGroundEntry(int remaining)`: 남은 횟수 ≤ 0이거나 목록이 비었으면 `None`, 아니면 `목록[min(remaining, 길이) − 1]`. 원소가 -1이면 그대로 `None`이다 | C1 |
| TP-6 | `CombatSoundCue.EffectKeepingPreset`은 `UnitEvent` 판 하나만 만든다(장애물·추락 모두 Unit 플레이어). `SfxEvent` 판은 쓰는 곳이 없어 만들지 않는다 | C5 |
| **TP-7** | **C1-N19 함께 고침**(재평가 낮음, `CombatEventSounds.cs:91-92`): `CombatSoundCue.Attack`의 `slotIndex >= 0`을 `CombatSoundPresets.IsSound(slotIndex)`로. `AttackSlot`의 "없음"도 `None`이라 같은 규칙이다. 동작 같음 | C1 |
| **TP-8** | **C12-N10 함께 고침**(재평가 낮음, 설정 Data 요약 10-12줄): "하위 설정 묶음은 수 규칙을 바로 부른다"를 실제에 맞춘다(보드 등장 시간은 설정 표의 `ValidateSeconds`를 부름). 주석만 | C12 |
| TP-9 | `CombatPresentationValueRules.NoSoundPreset`(`DV/CombatPresentation/CombatPresentationValueRules.cs:52-53`) 문서도 고친다(설계 SP3가 말한 곳). **설계 §5.1 파일 표에 빠져 있던 파일**이라 여기 적는다. 주석만 | C12 |
| TP-10 | 기반 `HandleUnavailable`의 첫 줄은 `if (!IsRunning) return;`이다. 설계의 "정리가 끝났거나 놓았으면"과 같은 뜻이고, 시작 전 호출도 막는다 | C9 |
| **TP-11** | **`Advance`는 돌고 있을 때만 부른다는 보장을 기반 한 곳에 둔다.** 기반 `Begin`은 `if (IsRunning) Advance(0f, 0f)`(설계 MT1), `Tick`은 이미 돌 때만 부른다. 추상 `Advance` 문서에 이 약속을 적고, **세 `Advance` 첫 줄의 `|| !IsRunning`을 지운다**(직선 71·근접 38·곡사 새 `Advance`). 설계 MT3은 곡사 첫 줄에 `!IsRunning`을 두라고 했다. 같은 검사를 두 층에서 하지 않게 바꿨다(C5-13과 같은 종류). 근접 파일이 바뀌는 범위에 더해진다 | C5 |
| TP-12 | 곡사 `Render(progress)`는 인자를 쓰지 않고 `Pose(Elapsed)`를 부른다(설계 MT3). override 문서에 "기반이 `Render`를 부르는 두 자리(시작·Tick 끝)에서 `progress`와 `Elapsed / 길이`가 같은 값이라, 초 단위인 `Elapsed`를 그대로 씀"을 적는다. 기반 `Render` 문서에도 "진행률을 쓸지 경과 초를 쓸지는 파생 클래스가 정함"을 적는다 | C2(LSP)·C12 |
| **TP-13** | **곡사는 끝 시각 필드를 두지 않는다.** 끝 시각은 기반 길이 하나다(`PoseDone`). 설계 MT2는 "발사 순간·끝 시각"을 남긴다고 했다. 발사 순간 식은 정적 `FireAtOf(settings)` 하나로 두고 생성자와 `EndTimeOf`가 함께 부른다. 그러면 발사 순간 식이 파일 안에 한 번만 있다 | C1 |
| TP-14 | 곡사의 발사(`FireArc`)·착탄 자리·방향에 넘기는 위쪽은 기반 `Up`(생성 때 `mapper.WorldUp`을 정규화한 값)이다. 지금은 `Begin` 때 읽은 `WorldUp`을 그대로(발사) 또는 정규화해(자리·방향) 썼다. 생성과 `Begin`은 같은 호출 안이라(`CombatMortarStyle.cs:86-90` → `CombatAttackPresenter.Begin`) 같은 값이다. 발사는 풀이 다시 정규화하므로(`CombatProjectilePool.cs:146-150`) 차이는 부동소수 끝자리 안이다 | C10 |
| TP-15 | 곡사 생성자의 예외 문구 둘(`곡사 표시 설정이 필요합니다`·`표시 시간의 합이 유한해야 합니다`)은 그대로 둔다. 기반 정리 실패 문구도 그대로다(설계 §4) | C10 |
| **TP-16** | **생성 인자 검사 순서 정정:** 기반 생성자 인자는 왼쪽부터 평가되므로 실제 순서는 매퍼 → 설정·종류·끝 시각 → 유닛 → 목표 → 위쪽 → 길이 → (곡사 본문) 풀 → 무기 → 발사 콜백이다. 설계 §4 표의 "설정·종류·끝 시각 → 매퍼"는 틀려 SM-c에서 고친다. 끝 시각이 0이면 기반이 `duration` 인자 예외를 던진다. 지금 DB 검사가 발사 길이 > 0을 보장해 닿지 않는다(설계 MT2) | C10·C12 |
| **TP-17** | **C3-03은 이번에 둘만 푼다**(기반·곡사 정리를 공용 `CleanupErrors`로). 남은 둘(`CombatAttackPresenter.Cancel`, `CombatProjectilePool`)은 바꾸는 파일 밖이고, 풀은 예외 모양(`InvalidOperationException(AggregateException)`)을 바꿀지 정해야 해서 남긴다. C3-03은 "줄어듦"이다 | C3 |
| TP-18 | C6-09(기반 생성자 인자 이름)는 설계 Q-MT3 A로 푼다. 검사 순서는 목표 → 위쪽 → 길이이고, 각자 `nameof`를 쓴다 | C6 |

## 1. Phase 목록

| Phase | 내용 | 바꾸는 파일 | 커밋 묶음 |
|---|---|---|---|
| SM-0 | 착수 전 확인 | 없음 | — |
| SM-a | C1-38 소리 프리셋 원본 + C1-N19·C12-N10 | 설정 Data·SO, 프리셋 답, 소리 표, 수 규칙(주석), 설정 에셋 | 하나(코드 + 에셋) |
| SM-b | C1-42 곡사 공통 수명 + C6-09·C3-03 일부 | 기반, 곡사, 직선, 근접 | 하나 |
| SM-c | 문서·공유·묶음 끝 검토·평가 아티팩트 15차 | 문서 | 하나 |

## 2. 12기준 점검

### 2.1 Phase별로 볼 것

같은 감점표(높음 −20·중간 −7.5·낮음 −2.5, 낮음은 기준마다 −10까지)다. 재평가에서 낮음은 모든 기준이 상한에 닿아 있어, 점수를 움직이는 것은 중간뿐이다.

| 기준 | 재평가 점수 | 이 범위의 문제 | 이 계획이 하는 것 | 새 문제가 생기지 않게 볼 곳 | 예상 |
|---|---|---|---|---|---|
| C1 SSOT | 60 | 중간 C1-38·C1-42, 낮음 C1-N19 | 소리 번호 원본 = 설정 표, 진입 규칙 = `WeakGroundEntry`, "-1 = 프리셋 없이 냄" = `EffectKeepingPreset`, 수명 = 기반 하나, 발사 순간 식 = `FireAtOf` 하나(TP-13), C1-N19(TP-7) | 진입 규칙 숫자(2)가 코드에 남지 않음, 장애물 도우미와 추락이 같은 함수를 부름, 곡사에 수명 깃발이 남지 않음 | **75** |
| C2 SOLID | 75 | 낮음 C2-30(설정 Data가 평평함) | 곡사는 자세·무기·탄만 맡음, 새 공격 종류는 훅만 채움 | `Render` 인자를 곡사가 쓰지 않는 점을 문서로(TP-12), 설정 Data에 칸 하나가 늘어 C2-30이 조금 커짐(묶음 분리는 범위 밖) | 75 |
| C3 FW | 90 | 낮음 C3-03 | 기반·곡사 정리가 공용 `CleanupErrors`(TP-17) | 새 정리 도우미를 만들지 않음 | 90 |
| C4 독립성 | 82.5 | — | 소리 번호는 에셋만 고치면 됨 | Factory·조립 줄 변경 없음 | 82.5 |
| C5 불필요 | 90 | 낮음 C5-13(공격 Tick 층층 검사) | 곡사의 dt 검사·중첩 가드·수명 깃발 사본이 없어짐, `Advance` 진입 검사 한 층(TP-11) | 쓰지 않게 되는 멤버가 남지 않음(옛 `WeakGroundEntryPreset`·`Obstacle` 도우미·곡사 `CanRender`·`TryCleanup`·`RequireFinite`·`CleanupFailure`), 새 멤버는 모두 부르는 곳이 있음(`WeakGroundCollapse()`·`Fall()` 포함) | 90 |
| C6 C#·Unity | 90 | 낮음 C6-09 | 인자 이름 고침(TP-18) | 무기의 Unity null 확인(`_weapon != null`)을 빼지 않음, 목록 칸에는 `Min`이 안 붙어 검사가 원소마다 봄 | 90 |
| C7 매 프레임 | 90 | — | 곡사 Tick의 검사 수 같음(유닛·무기·탄 확인), 진입 프리셋 찾기는 색인 한 번 | Tick에 새 작업·새 확인을 더하지 않음 | 90 |
| C8 캡슐화 | 90 | — | 진입 목록은 복사본으로만(TP-2), 새 기반 멤버는 `protected`, 곡사 필드는 `private readonly` | `HandleUnavailable`은 `protected`(가상 아님), 직렬화 목록을 밖에 내주지 않음 | 90 |
| C9 수명 | 90 | — | 한 번 시작·멱등 놓기·정리 순서가 기반 하나 | 정리 순서(콜백 풀기 → 탄 회수 → 무기 자세 → 유닛 자세)가 같음, 무기 꺼짐 콜백이 Tick 안·정리 뒤에 와도 안전(TP-10) | 90 |
| C10 정확성 | 82.5 | 중간 C10-02(범위 밖) | 정상 길 같음, 오류 길 차이는 설계 §4 | 설계 MT4 순서 표 대조(Tb.6), 발사 순간 0·큰 프레임·발사 중 취소, 붕괴 소리가 에셋 값 0으로 남(에셋을 코드와 한 커밋) | 82.5 |
| C11 할당 | 90 | — | 진입 목록 복사는 View 서비스 수명에 한 번(TP-2), 정리 실패 목록은 실패할 때만 | 정리는 공격 하나마다 한 번이라 자주 도는 길이 아님. 정리 한 번의 할당은 바꾼 뒤 근접 대리자 1·직선 2·곡사 4, 지금은 근접·직선 목록 1·곡사 목록 1 + 대리자 4 | 90 |
| C12 주석 | 90 | 낮음 C12-N10 | 툴팁·요약·문서 맞춤(TP-8·TP-9) | 고친 줄을 가리키는 옛 주석("-1 뜻이 다른 곳은 여기 하나", "프리셋 없는 효과음(장애물 반응)", "적 공용·배경음·추락·약한 지반 붕괴 프리셋", 곡사 "공통 기반 밖")이 남지 않음 | 90 |

예상 점수(중간 둘만 뺌): 요청 7기준(C1~C7) 82.5 → **84.6**, 12기준 85.0 → **86.3**. 낮음 넷(C1-N19·C6-09·C12-N10, C3-03 일부)은 상한 안이라 점수가 그대로다.

### 2.2 바꾸는 파일 안의 문제와 이번 판단

| 문제 | 등급 | 파일 | 판단 |
|---|---|---|---|
| C1-38 | 중간 | 설정 Data·프리셋 답·소리 표 | 푼다(SM-a) |
| C1-42 | 중간 | 기반·곡사 | 푼다(SM-b) |
| C1-N19 | 낮음 | 소리 표 | 같이 고침(TP-7) |
| C12-N10 | 낮음 | 설정 Data | 같이 고침(TP-8) |
| C6-09 | 낮음 | 기반 | 같이 고침(TP-18) |
| C3-03 | 낮음 | 기반·곡사(+ 밖 둘) | 둘만(TP-17) |
| C5-13 | 낮음 | 기반·곡사(+ 밖 둘) | 곡사 사본만 없어짐, 기반·Presenter·풀의 dt 검사는 남김 |
| C2-30 | 낮음 | 설정 Data | 그대로(묶음 분리는 범위가 큼) |
| C1-60 | 낮음 | 소리 표(+ 처리기·Presenter) | 그대로(세 파일 규칙 하나로 모으는 일, 소리 표만 고치면 사본이 늘어남) |
| C5-U0240 | 낮음 | 소리 표(옛 표식 사건 갈래) | 그대로(Core가 그 사건을 더 내지 않는지는 Core가 정함) |
| C4-16 | 낮음 | 설정 Data | 그대로(새 검사는 기존 `ValidateSoundPreset`을 부를 뿐, 새 감싸개를 만들지 않음) |

### 2.3 다음 후보 (손대지 않음)

C3-03 나머지 둘(`CombatAttackPresenter.Cancel`·`CombatProjectilePool`), C5-13 나머지(Presenter·기반·풀의 dt 검사), C2-09·C6-04·C5-22(곡사 무기 표시 계약), C1-44(곡사 궤적 미리보기의 길 선택 사본), C1-60, C5-U0240.

## 3. SM-0 착수 전 확인 (코드 변경 없음)

- **T0.1 원격:** `git fetch origin feature/Yonggeun/Refector-Stage develop feature/La/AudioSystem_R`. 원격 브랜치가 앞서면 빨리 감기로 받는다. develop·현강님 오디오 브랜치가 아래 파일을 바꿨는지 본다(11-12 확인 때는 차이 없음).
- **T0.2 작성자 줄:** 얕은 clone이라 경계 커밋(`^`) 줄은 GitHub 파일 이력으로 본다(옮기기 전 경로 `Display/…`도 함께, 11-12 C1-42 정정과 같은 방법). 대상:
  - `V/Playback/Support/CombatEventSounds.cs` 79·85·91-92·141-146·169·189-205(현강님 예상), `CombatSoundPresets.cs` 15-28·77-78
  - `DV/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` 9-13·165-183·244-247·292-294
  - `DV/CombatPresentation/CombatPresentationValueRules.cs` 52-53
  - `V/Presentation/Attack/CombatLinearAttackAnimation.cs` 71·119-124, `CombatMeleeAttackAnimation.cs` 38(TP-11)
- **T0.3 시험 참조:** `Assets/Tests`에서 `CombatMortarAnimation`·`CombatAttackMotion`·`ReleaseResources`·`CombatEventSounds`·`CombatSoundPresets`·`CombatSoundCue`·`FallSoundPreset`·`WeakGroundCollapseSoundPreset`·`weakGroundEntrySoundPresets`를 다시 찾는다(11-12: 0곳).
- **T0.4 Boot 오디오 칸:** `Assets/Scenes/Boot.unity`에서 SFX `WeakTerrian` 프리셋 셋(`CrackLv` 0·1·2)과 Unit `Fall`의 `parameterPresets: []`가 그대로인지 본다. 바뀌었으면 설계 SP3·기본값을 다시 본다.
- **T0.5 줄 번호:** 이 계획의 줄 번호를 지금 파일과 다시 맞춘다.

## 4. SM-a 소리 프리셋 원본 (C1-38 + C1-N19·C12-N10)

설정 Data 코드와 에셋은 한 커밋이다. 코드만 들어가면 에셋의 붕괴 칸 -1이 남아 붕괴 소리가 사라진다.

- **Ta.1 설정 Data `CombatPresentationSettingsDataSO.cs`:**
  - 칸(165-183): 배경음 셋 다음에 진입 목록 `weakGroundEntrySoundPresets`(TP-1, 툴팁은 설계 SP2 문구), 그다음 붕괴 칸(기본값 -1 → **0**, 툴팁 그대로), 그다음 새 머리말(TP-4) 아래 추락 칸(기본값 -1 그대로, 툴팁은 설계 SP3 문구 — "유닛"만, Q-SP2 A).
  - 읽기(244-247): `FallSoundPreset` 문서를 "-1이면 프리셋을 지정하지 않고 냅니다(장애물 소리와 같은 규칙)"로, `WeakGroundCollapseSoundPreset` 문서는 그대로. `CopyWeakGroundEntrySoundPresets()`를 더한다(TP-2, 문서에 "부를 때마다 새 배열, 프리셋 객체가 한 번 부름").
  - 검사(292-294 뒤): 목록이 null이면 `InvalidOperationException("[CombatPresentationSettingsData] 약한 지반 진입 소리 프리셋 목록이 필요합니다.")`, 원소마다 `ValidateSoundPreset(preset, nameof(weakGroundEntrySoundPresets))`(TP-3). 새 조건 함수는 만들지 않는다(C4-16).
  - 요약(9-13): TP-8. "하위 설정 묶음 셋(보드 등장·선로 낙하·Nexus 낙하)은 수 규칙을 바로 부르거나, 시간처럼 이 설정의 조건 함수를 씁니다"처럼 실제와 맞춘다. 줄을 쓰기 전에 세 묶음의 `Validate`를 다시 읽고 문장을 정한다.
- **Ta.2 에셋 `CombatPresentationSettings_Default.asset`(84-85):** 붕괴 줄 `-1` → `0`. 같은 자리에 칸 순서대로 `weakGroundEntrySoundPresets:` + `- 1` + `- 2`(TP-1 꼴)를 더한다. 추락 줄은 `-1` 그대로 붕괴 줄 뒤로 옮긴다(칸 순서와 같게, 다시 직렬화할 때 diff를 줄임).
- **Ta.3 프리셋 답 `CombatSoundPresets.cs`:**
  - 필드 `private readonly int[] _weakGroundEntry;` — 생성자(41-48)에서 `settings.CopyWeakGroundEntrySoundPresets()`로 한 번 채운다(C11).
  - `internal int WeakGroundEntry(int remaining)`(TP-5). 문서: "남은 진입 횟수 하나의 진입 소리 프리셋입니다. 첫 원소가 남은 1, 넘으면 마지막 원소, 0 이하·빈 목록은 소리 없음(소리 프리셋 원본 설계 SP2)."
  - `internal int Fall() => _settings.FallSoundPreset;` 되살림. 문서: "-1이면 프리셋 없이 냅니다(받는 쪽이 `EffectKeepingPreset`으로 냄, 설계 SP3)."
  - 클래스 요약(16-17): "적 공용·배경음·추락·약한 지반 진입·붕괴 프리셋은 전투 연출 설정 표"로.
  - `None`(27) 문서: 설계 SP3 문장("받은 쪽이 `Effect`로 내면 소리 없음, `EffectKeepingPreset`으로 내면 프리셋 없이 냄(장애물·추락)").
- **Ta.4 수 규칙 `CombatPresentationValueRules.cs`(52-53):** `NoSoundPreset` 문서에 Ta.3 `None` 문서와 같은 뜻을 적는다(TP-9, 주석만).
- **Ta.5 소리 표 `CombatEventSounds.cs`:**
  - `CombatSoundCue`:
    - `EffectKeepingPreset(UnitEvent slot, int preset) => CombatSoundPresets.IsSound(preset) ? Effect(slot, preset) : EffectWithoutPreset(slot);`(TP-6). 문서: "프리셋이 있으면 그 프리셋으로, 없으면(-1) 칸의 지금 프리셋을 바꾸지 않고 냅니다. -1의 뜻이 '소리 없음'인 `Effect`와 다른 곳은 이 함수를 부르는 곳(장애물·추락)입니다."
    - `Attack`(91-92): `CombatSoundPresets.IsSound(slotIndex)`(TP-7).
    - 종류 문서(20) "프리셋 없이 냅니다(장애물 반응)" → "(장애물·추락·기차 등, 칸의 지금 프리셋)", `Preset` 문서(64)는 그대로.
  - 추락 갈래(141-144): `EffectKeepingPreset(UnitEvent.Fall, presets.Fall())`. 보인 캐릭터만 내는 조건은 그대로.
  - 진입 갈래(145-146): `case CombatMovementSourceEvent source when TryGetWeakGroundEntry(source, out int remaining): return CombatSoundCue.Effect(SfxEvent.WeakTerrian, presets.WeakGroundEntry(remaining));`
  - 도우미(189-197): `WeakGroundEntryPreset` → `private static bool TryGetWeakGroundEntry(CombatMovementSourceEvent source, out int remaining)`. 조건은 그대로이고, 번호는 고르지 않는다. 주석 "번호는 프리셋 답이 남은 횟수로 고릅니다"로.
  - 순간 표(159-173): 붕괴 줄 `CombatSoundCue.Effect(SfxEvent.WeakTerrian, presets.WeakGroundCollapse())`. 장애물 두 줄은 `CombatSoundCue.EffectKeepingPreset(UnitEvent.ObsDamaged, presets.Obstacle(moment.ObstacleKind))`처럼 바로 부르고 `Obstacle` 도우미(199-205)는 지운다. switch 맨 앞이나 `_` 줄 위에 Q-SP4 A 주석 한 줄: "추락(`Fall`) 순간은 앞 갈래(`CombatFallEvent`)가 먼저 받아 여기 오지 않습니다. 다른 사건이 추락 순간을 내게 되면 이 표에 줄을 더해야 합니다."
  - 클래스 문서(104-109): "칸에 넣을 프리셋 번호는 모두 View DB에서 고릅니다(코드 숫자 없음, 소리 프리셋 원본 설계 SP1~SP3)" 한 줄을 더한다.
- **Ta.6 확인(grep):**
  - `V/`·`DV/`에서 `Math.Min(2`·`WeakTerrian, 0`·`EffectWithoutPreset(UnitEvent.Fall`·`WeakGroundEntryPreset`이 0곳.
  - `WeakGroundCollapse()`·`Fall()`·`WeakGroundEntry(`·`CopyWeakGroundEntrySoundPresets(`를 부르는 곳이 각각 한 곳.
  - `EffectWithoutPreset(UnitEvent` 판을 아직 부르는 곳이 있는지(`EffectKeepingPreset` 안 하나 이상이면 됨). 0곳이면 멈추고 보고한다(C5).
  - 소리 단서를 만드는 곳에 `>= 0` 판정이 남지 않음(C1-N19).
- **Ta.7 값 대조(설계 §1.1 표):** 진입 남은 1 → 1, 남은 2 → 2, 남은 3 → 2(넘으면 마지막), 붕괴 → 0, 추락 → 프리셋 없이 `Fall`, 장애물 -1 → 프리셋 없이, 장애물 0 이상 → 그 프리셋. 지금 코드와 같은지 표로 맞춘다.

## 5. SM-b 곡사 공통 수명 (C1-42 + C6-09·C3-03 일부)

- **Tb.1 기반 `CombatAttackMotion.cs`:**
  - using `ProjectL.Systems.RuntimeSystem.ErrorHandling`을 더한다(`CombatAttackPlayback.cs:9`와 같음).
  - 생성자(19-31): 목표 → 위쪽 → 길이 순으로 나눠 각자 `nameof(target)`·`nameof(up)`·`nameof(duration)`(TP-18).
  - `Begin`(47-69): try 안을 `BeginResources(); Render(0f); if (IsRunning) Advance(0f, 0f);`로.
  - `Tick`(71-106): 쓸 수 없음 블록(77-88)을 `HandleUnavailable(); return;`으로.
  - 새 멤버: `protected virtual void BeginResources() { }`, `protected void HandleUnavailable()`(TP-10, 본문은 옛 77-88 블록), `protected virtual void ReleaseResources(ref List<Exception> errors) { }`(옛 무인자 판을 바꿈).
  - `Cleanup`(130-153): `List<Exception> errors = null; ReleaseResources(ref errors); CleanupErrors.Try(Unit.RestorePose, ref errors); CleanupErrors.ThrowIfAny(errors, "[CombatAttackMotion] 발사체·유닛 자세 정리에 실패했습니다.");`. 문구는 그대로다(TP-15). `ReleaseResources`는 훅 안에서 단계마다 `Try`를 쓰므로 바깥에서 감싸지 않는다. 훅 문서에 "단계마다 `CleanupErrors.Try`로, 던지지 말고 모읍니다"를 적는다.
  - 문서: 클래스 요약에 "근접·직선·곡사 코드 모션이 상속합니다. 종류마다 다른 것은 훅(시작 자원·쓸 수 있음·그리기·진행·자원 정리)만 채웁니다(소리 프리셋·곡사 수명 설계 MT1)". 추상 `Render`·`Advance` 문서에 TP-11·TP-12 약속.
- **Tb.2 직선 `CombatLinearAttackAnimation.cs`:**
  - `Advance` 첫 줄(71) `|| !IsRunning` 지움(TP-11).
  - `ReleaseResources`(119-124) → `protected override void ReleaseResources(ref List<Exception> errors)`. 본문은 `var lease = _lease; _lease = null; CleanupErrors.Try(() => _pool.Recall(lease), ref errors);`. using 둘(`System.Collections.Generic`·`ErrorHandling`)을 더한다.
- **Tb.3 근접 `CombatMeleeAttackAnimation.cs`:** `Advance` 첫 줄(38) `|| !IsRunning` 지움(TP-11). 다른 줄은 그대로.
- **Tb.4 곡사 `CombatMortarAnimation.cs` 다시 씀:**
  - 선언 `: CombatAttackMotion`.
  - 남기는 필드: `_settings`·`_mapper`·(`#if`) `_profile`·`_pool`·`_weapon`·`_fired`·`_hit`·`_weaponDisabled`·`_fireAt`·`_lease`·`_weaponVersion`·`_hasFired`·`_flightDone`(TP-13: `_poseEndsAt` 없음).
  - 생성자: `: base(unit, target, mapper != null ? mapper.WorldUp : throw new ArgumentNullException(nameof(mapper)), emptyFacing, EndTimeOf(settings), cancelOwner)`. 본문은 `_settings = settings; _mapper = mapper;` → 풀(`??` 예외)·계측 → 무기(Unity null 확인 예외) → 발사 콜백(`??` 예외) → `_hit = hit; _weaponDisabled = HandleUnavailable; _fireAt = FireAtOf(settings);`. 매개변수 문서 둘은 그대로 둔다.
  - `IsComplete => IsRunning && PoseDone && _flightDone;`
  - `ResourcesAvailable => CanUseWeapon() && (!_hasFired || _flightDone || (_lease != null && _lease.IsActive));`
  - `BeginResources()`: `_weaponVersion = _weapon.BeginPose(); _weapon.BindCancellation(_weaponDisabled);`(`BeginPose`가 `Validate`를 부름).
  - `Render(float progress) => Pose(Elapsed);`(TP-12 문서).
  - `Advance(float previous, float next)`: `if (_hasFired || next < _fireAt) return; Pose(_fireAt); Fire();`(TP-11). 주석 "큰 프레임에서도 실제 발사 시점의 자세와 피벗을 먼저 샘플링합니다"는 옛 135줄 주석을 옮긴다.
  - `Pose(float elapsed)`: 옛 `Render(elapsed)`(204-227) 본문이다. 앞 확인 `if (!Unit.IsAvailable || !CanUseWeapon()) { HandleUnavailable(); return; }`, 유닛 자세 뒤 확인 `if (!IsRunning || !CanUseWeapon()) return;`(취소하지 않음). `_unit`·`_origin`·`_forward`·`_facing`은 기반 `Unit`·`Origin`·`Forward`·`Facing`으로.
  - `Fire()`: 옛 170-202이다. 입구 `if (_hasFired || !IsRunning) return;`, `int version = Version;`, 착탄 콜백 `if (!IsCurrent(version)) return;`, `FireArc` 뒤 `if (!IsCurrent(version)) { _pool.Recall(lease); return; }`. `_target` → `Target`, `_up`·`_up.normalized` → `Up`(TP-14), `_facing` → `Facing`.
  - `CanUseWeapon()`(private): `_weapon != null && _weapon.IsAvailable && _weapon.IsPoseCurrent(_weaponVersion)`.
  - `ReleaseResources(ref List<Exception> errors)`: `CombatProjectileLease lease = _lease; _lease = null;` → `CleanupErrors.Try(() => { if (_weapon != null) _weapon.UnbindCancellation(_weaponDisabled); }, ref errors);` → `CleanupErrors.Try(() => _pool.Recall(lease), ref errors);` → `CleanupErrors.Try(() => { if (_weapon != null && _weapon.IsPoseCurrent(_weaponVersion)) _weapon.ResetPose(); }, ref errors);`. 순서는 옛 `Cleanup`(254-260)과 같고, 유닛 자세는 기반이 그 뒤에 되돌린다.
  - 정적 도우미: `EndTimeOf(settings)`(null → 곡사 종류 → `SumSeconds(Math.Max((double)PrepareDuration + ShootDuration, (double)FireAtOf(settings) + ReturnDuration))`), `FireAtOf(settings)`(`SumSeconds((double)PrepareDuration + ShootDuration * (double)FireTime)`), `SumSeconds(double)`(옛 `Duration`, 문구 그대로). 옛 식과 비트까지 같다(옛 `_poseEndsAt`도 float `_fireAt`을 double로 바꿔 더했다).
  - 지우는 것: `_unit`·`_target`·`_emptyFacing`·`_cancelOwner`·`_origin`·`_forward`·`_up`·`_facing`·`_elapsed`·`_version`·`_begun`·`_cancelled`·`_released`·`_cleanupDone`·`_ticking`·`_poseEndsAt`, `Begin`·`Tick`·`Cancel`·`Release`·옛 `HandleUnavailable`·`Cleanup`·`CleanupFailure`·`TryCleanup`·`Duration`·`RequireFinite`·`CanRender`.
  - using: `System.Collections.Generic`은 훅 서명에 쓰여 남고, `ErrorHandling`을 더한다.
  - 클래스 요약에 "수명(한 번 시작·Tick·취소·놓기·정리 묶기)은 공통 기반 `CombatAttackMotion`이 맡고, 이 클래스는 무기 자세·발사·착탄만 맡습니다(설계 MT2)" 한 줄을 더한다.
- **Tb.5 생성 줄 대조:** `CombatMortarStyle.cs:88-89`의 생성 인자가 그대로인지 본다(바꾸지 않음, 설계 MT5).
- **Tb.6 동작 대조(코드 읽기):** 설계 MT4 표 다섯 줄을 바뀐 코드로 다시 따라간다. 더해서 다음을 본다.
  - 발사 순간 0: 시작 때 `Pose(0)` 두 번 → 발사 한 번.
  - 큰 프레임(한 Tick에 발사 순간과 끝을 넘음): 발사 순간 자세 → 발사 → 끝 자세. 착탄 전이라 완료 아님.
  - 발사 중 취소(`FireArc` 안 재진입): 대여 회수, `_hasFired` 거짓.
  - 착탄이 정리 뒤에 옴: 콜백이 `IsCurrent`에서 거름.
  - 무기 `OnDisable`이 Tick 안에서 옴: 주인 취소 → `Cancel` → Tick의 `if (IsRunning) Render(...)`가 건너뜀.
  - `Validate` 실패: 동작 안에서 정리 후 던짐, `CombatAttackPresenter`의 `Cancel`은 아무것도 안 함(설계 §4).
  - 직선·근접: `Begin`의 `if (IsRunning)`과 `Advance` 첫 줄 변경으로 결과가 같음(둘은 `Render`가 취소하지 않음).
- **Tb.7 확인(grep):** `V/Presentation/Attack/`에서 `TryCleanup`·`CleanupFailure`·`_cleanupDone`·`RequireFinite`가 0곳. `new List<Exception>()`가 기반·곡사·직선에 0곳. `ReleaseResources()` 무인자 판을 부르거나 override하는 곳이 0곳.

## 6. SM-c 문서·공유·검토·아티팩트

- **Tc.1 묶음 끝 검토(새 에이전트 하나):** 기준 판 diff 전체를 본다. 컴파일 오류 대조, 동작 같음(Ta.7·Tb.6), §2.1 표의 "새 문제가 생기지 않게 볼 곳"을 12기준으로 점검한다. 지적은 높음·중간이면 반영하고, 낮음은 권장과 함께 보고한다.
- **Tc.2 문서:**
  - 사운드 작업자 안내 `handoffs/2026-10-28-combat-sound-guide-for-sound-worker.md` 104-105·160-161: 추락·붕괴 줄을 지금 칸(`Fall`·`WeakTerrian`)과 설정 표 칸 셋(진입 목록·붕괴·추락)으로.
  - 이펙트·소리 창구 안내 `handoffs/2026-10-29-combat-effect-sound-cue-guide.md` 36-37.
  - 폴더 배치 안내 `viewDocs/combat-view-folder-guide.md` §2: 소리 프리셋 줄(141)에 "약한 지반 진입·붕괴·추락도 설정 표 칸이 원본, -1 뜻이 다른 곳은 `EffectKeepingPreset`(장애물·추락)"을 더한다. 공격 종류 줄(135) 옆에 "코드 모션의 수명은 `CombatAttackMotion` 하나(곡사 포함), 종류는 훅만"을 더한다. 머리 갱신 줄 하나.
  - 설계: §10 구현 메모, §4 검사 순서 정정(TP-16), §7 "결정" 칸.
- **Tc.3 공유(모두 미전달):**
  - 현강님 구조 공유 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` 새 §30: 바뀐 줄(추락·진입·붕괴 갈래, 장애물 도우미 걷음, `Attack` 판정), §23 첫 질문은 사용자가 설정 표로 정함, 확인 부탁(추락 소리를 유닛만 내는 것 — Q-SP2).
  - 호진님 설정 표 메모 `handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md` 새 §17: 칸 셋(진입 목록 새 칸·붕괴 기본값 0·추락 -1 뜻)과 머리말 나눔, 에셋 두 줄.
  - 정정 표시 둘: 호진님 구조 공유 §17의 C1-42 질문, 천명님 특성 공유 §7의 C1-42 질문(곡사·기반은 용근 줄 — C2-04 정정과 같은 꼴).
  - T0.2에서 다른 작업자 줄이 더 나오면 그 사람 메모에 넣는다.
- **Tc.4 평가 아티팩트 15차:** 재평가 판정 기준이다. C1-38·C1-42 다 풀림(C1 60 → 75), 낮음 C1-N19·C6-09·C12-N10 풀림, C3-03·C5-13 줄어듦. 요청 7기준 82.5 → 84.6, 12기준 85.0 → 86.3(추정). 문제마다 바꾼 것·파일·남은 단계를 적는다.
- **Tc.5 기록:** 이 계획 §10 실행 기록, CLAUDE.md 상태 줄.

## 7. 커밋 묶음 (사용자가 커밋)

- SM-a: 설정 Data·SO 코드, 설정 에셋(한 커밋 필수), 프리셋 답, 소리 표, 수 규칙.
- SM-b: 기반·곡사·직선·근접 넷.
- SM-c: 문서.

## 8. Unity 확인 (모든 Phase 뒤 한 번, 사용자)

| 번호 | 확인 | 볼 것 |
|---|---|---|
| U-1 | 컴파일 | 오류·새 경고 없음 |
| U-2 | 설정 표 인스펙터 | "전투 소리 프리셋" 아래 진입 목록(1, 2)·붕괴(0), 새 머리말 아래 추락(-1) |
| U-3 | 약한 지반 진입 | `Test` 스테이지 칸 (6,3) 첫 진입에 금 가는 소리(지금과 같음) |
| U-4 | 약한 지반 붕괴 | 두 번째 진입의 붕괴 소리(지금과 같음) |
| U-5 | 설정 표 반영 | **재생 전에** 에셋의 진입 목록을 `[2]`로 바꾸면 첫 진입 소리가 바뀜(확인 뒤 되돌림). 진입 목록은 View 서비스가 시작할 때 한 번 읽어, 재생 중에 바꾸면 반영되지 않는다(§10.4 L1) |
| U-6 | 추락 | 넉백으로 낭떠러지에 떨어진 유닛의 추락 소리(지금과 같음), 구조물 추락은 소리 없음(지금과 같음) |
| U-7 | 장애물 | 장애물 피해·파괴 소리 지금과 같음 |
| U-8 | 곡사 공격 | CactusCannon 공격: 준비·발사·반동·무기 복귀·착탄 히트가 지금과 같음 |
| U-9 | 곡사 취소 | 곡사 공격 도중 결과 넘기기·전투 이탈에서 무기 자세·탄이 남지 않음 |
| U-10 | 직선·근접 | Sniper 직선·Gravekeeper 근접 공격이 지금과 같음 |
| U-11 | 공격 소리 | 공격·포효 소리가 지금과 같음(C1-N19) |

## 9. 공유 목록 (구현 뒤 확정)

| 사람 | 줄 | 문서 |
|---|---|---|
| 현강님 | 소리 표 갈래 셋·장애물 도우미(서명 201 포함)·순간 표 넷·`Attack` 판정, 프리셋 답 클래스 요약 16·`Fall` | 현강님 구조 공유 §30 |
| 호진님 | 설정 표 칸 셋(관례), C1-42 질문 정정 | 호진님 설정 표 메모 §17, 호진님 구조 공유 §17 정정 |
| 천명님 | C1-42 질문 정정 | 천명님 특성 공유 §7 정정 |
| 민형님 | 없음(Factory 무변경) | — |

## 10. 실행 기록

TP-1~TP-18은 계획대로 확정했다(2026-11-12 사용자 "그대로, SM-0 진행해").

### 10.1 SM-0 착수 전 확인 (2026-11-12, 코드 변경 없음)

- **T0.1 원격:**
  - 원격 브랜치 `feature/Yonggeun/Refector-Stage`는 HEAD `03d3555aa`와 같다. 받을 것이 없다.
  - develop은 2커밋 앞서 있다(`360ab06e9`·`b47c54a1a`, 천명님 Core — 체력 변경 근거·적 AI 계산 재사용). 고칠 파일 열셋과 Boot 씬에 차이가 없고, `ViewSystem/Combat/`도 바꾸지 않는다.
  - 현강님 `feature/La/AudioSystem_R`(`5be9bba85`, 1커밋 앞섬 "특성 레벨 UI 가시화")도 고칠 파일에 차이가 없다.
- **T0.2 작성자 줄:** 얕은 clone이라 `git fetch --shallow-since=2026-10-15`로 로컬 이력을 늘려 blame했다(작업 트리 변경 없음). 그래도 경계로 보이는 줄은 GitHub 파일 이력(옮기기 전 경로 `Display/…` 포함)으로 확인했다.

  | 파일 | 바꿀 줄 | 작성자 |
  |---|---|---|
  | `CombatEventSounds.cs` | 추락·진입 갈래 141-146, 붕괴 169, 진입 도우미 189-197 | **현강님** `8fe4a688b`(11-02)·`c0f92a5d8`(11-10) |
  | | `Attack` 판정 91-92(TP-7) | **현강님** `686900826`(11-10) |
  | | 장애물 도우미 199-205 | 우리 `efa5e7d73` + 서명 한 줄 **현강님** `df422cbcf`(201) |
  | | 순간 표 159-173 | 우리 `efa5e7d73` 11줄 + **현강님** `c0f92a5d8`·`df422cbcf` 넷 |
  | | 종류 문서 17-21, 클래스 문서 104-109, `Preset`·`EffectWithoutPreset` 문서 64·79 | 우리 `59c39023c`·`efa5e7d73` |
  | `CombatSoundPresets.cs` | 클래스 요약 16 | **현강님** `686900826` |
  | | 요약 17·`None` 27-28·생성자 41-48·`WeakGroundCollapse` 77-78 | 우리 `c86b67591`·`4ec2a7049`·`efa5e7d73` |
  | `CombatPresentationSettingsDataSO.cs` | 요약 9-13·칸 165-183·읽기 244-247·검사 292-294 | 우리(`c417e9ee7`·`28ce918de`·`c86b67591`·`efa5e7d73`·`71390ac71`) |
  | `CombatPresentationValueRules.cs` | 52-53 | 우리 `4ec2a7049` |
  | `CombatPresentationSettings_Default.asset` | 79-86 | 우리 `c86b67591`·`73ed12be7` |
  | `CombatAttackMotion.cs`·`CombatMortarAnimation.cs`·`CombatLinearAttackAnimation.cs`·`CombatMeleeAttackAnimation.cs` | 전부 | 우리(처음 `dad08ef1`·`37188bd5`, 그 뒤 `145b0e047`·`e0ee626d8`·`823aa3c98`·`81354f33e`·`1eed25ca3`·`6e6d86c6a`·`2f065c9f1`, 모두 용근) |

  - 공유 목록(§9)에 더할 것: 현강님 줄 둘 — `CombatSoundPresets` 클래스 요약 16, 장애물 도우미 서명 201(도우미를 지우므로). 그 밖에 §9 그대로다.
  - 호진님·천명님은 이번에 바꾸는 줄이 없다. 정정 표시 둘만 남는다.
- **T0.3 시험 참조:** `Assets/Tests`(`.cs` 510개)에서 `CombatMortarAnimation`·`CombatAttackMotion`·`ReleaseResources`·`CombatEventSounds`·`CombatSoundPresets`·`CombatSoundCue`·`CombatLinearAttackAnimation`·`CombatMeleeAttackAnimation`·`NoSoundPreset`·설정 칸 이름 넷을 찾았다. 0곳이다.
- **T0.4 Boot 오디오 칸:** 설계 때와 같다.
  - SFX `WeakTerrian`(`Boot.unity` 1686~)에 프리셋 셋, `CrackLv` 0·1·2다.
  - Unit 12 `Fall`(1528-1541)은 `parameterPresets: []`이다.
  - 설계 SP1~SP3과 기본값은 그대로다.
- **T0.5 줄 번호:** HEAD가 계획 때와 같아(`03d3555aa`) 계획 §4·§5의 코드 줄 번호가 맞다. 문서 줄도 맞다: 사운드 작업자 안내 104-105·160-161, 이펙트·소리 창구 안내 36-37, 호진님 설정 표 메모 121-122.
  - 새 절 번호: 현강님 구조 공유 §30(지금 마지막 §29), 호진님 설정 표 메모 §17(지금 마지막 §16).
  - 정정 자리: 호진님 구조 공유 §17의 348줄, 천명님 특성 공유(`2026-10-30-combat-trait-system-view-cheon-share.md`) §7의 76줄.
  - 현강님 §23의 C1-38 질문은 442줄이다.

### 10.2 SM-a 소리 프리셋 원본 (2026-11-12 사용자 "SM-a 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **바꾼 파일 다섯**(커밋 하나, 설정 Data 코드와 에셋은 함께 — 코드만이면 붕괴 소리가 사라짐):
  - `DV/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`: 진입 목록 칸 `weakGroundEntrySoundPresets`(`List<int>`, 기본 `{ 1, 2 }`, `using System.Collections.Generic` 더함) → 붕괴 칸(기본 -1 → 0) → 새 머리말(TP-4) 아래 추락 칸(툴팁 = 설계 SP3 문구, "유닛"만). `FallSoundPreset` 문서, 복사 입구 `CopyWeakGroundEntrySoundPresets()`(TP-2), 검사는 붕괴 검사 뒤에 목록 null 예외 + 원소마다 `ValidateSoundPreset`(TP-3), 요약 TP-8(하위 묶음 셋은 수 규칙을 바로 부르고 보드 등장의 시간 칸 둘만 `ValidateSeconds`).
  - `CombatPresentationSettings_Default.asset`: 진입 목록 `- 1`·`- 2`, 붕괴 0, 추락 -1을 붕괴 뒤로(칸 순서와 같게). 이 타입의 에셋은 이것 하나다(grep).
  - `V/Playback/Support/CombatSoundPresets.cs`: `_weakGroundEntry` 배열(생성자에서 한 번 복사), `WeakGroundEntry(remaining)`(TP-5), `Fall()` 되살림, 클래스 요약(진입 더함), `None` 문서(TP-9 뜻).
  - `DV/CombatPresentation/CombatPresentationValueRules.cs`: `NoSoundPreset` 문서(TP-9). **계획과 다른 점 하나:** 같은 파일 클래스 요약 8줄의 규칙 문구 "-1 = 소리 없음"도 "-1 = 프리셋 없음, 0 이상 = 그 프리셋"으로 고쳤다(같은 규칙을 적은 옛 주석이라 C12, 주석만).
  - `V/Playback/Support/CombatEventSounds.cs`: `EffectKeepingPreset(UnitEvent, int)`(TP-6), `Attack` 판정 `IsSound`(TP-7), 종류 문서 20, 클래스 문서 한 줄, 추락 갈래 `EffectKeepingPreset(UnitEvent.Fall, presets.Fall())`, 진입 갈래 `TryGetWeakGroundEntry` + `presets.WeakGroundEntry(remaining)`, 순간 표 붕괴 `presets.WeakGroundCollapse()`·장애물 두 줄 `EffectKeepingPreset` 바로 부름(장애물 도우미 지움, 근거 주석은 그 두 줄 위로)·Q-SP4 A 주석.
  - `<see cref>`는 `CombatSoundPresets`가 AudioSystem 이름공간을 쓰지 않아 `Effect` 쪽만 `<c>` 글자로 적었다.
- **Ta.6 grep(`V/`·`DV/`):** `Math.Min(2`·`WeakTerrian, 0`·`EffectWithoutPreset(UnitEvent.Fall`·`WeakGroundEntryPreset` 0곳. `WeakGroundCollapse()`·`Fall()`·`WeakGroundEntry(`·`CopyWeakGroundEntrySoundPresets(`를 부르는 곳 각 한 곳. `EffectKeepingPreset`는 세 곳(추락·장애물 둘)이 부르고, `EffectWithoutPreset(UnitEvent)`는 그 함수 안에서만 불린다(0곳 아님, 계속 씀). 소리 단서 만드는 곳에 `>= 0` 없음. `Assets/Tests` 참조 0곳 그대로.
- **Ta.7 값 대조(지금 코드 → 바꾼 뒤, 에셋 기본값):**

  | 경우 | 지금 | 바꾼 뒤 | 같음 |
  |---|---|---|---|
  | 진입, 남은 1 | `Effect(WeakTerrian, min(2,1)=1)` | 목록[0] = 1 | 예 |
  | 진입, 남은 2 | 2 | 목록[1] = 2 | 예 |
  | 진입, 남은 3 | 2 | 목록[min(3,2)−1] = 2 | 예 |
  | 진입, 남은 0·붕괴 예정 아님 | `Effect(…, 0)` = 프리셋 0 소리 | `None`(소리 없음) | 다름 — 닿지 않음. Core `StaticTerrainFact`에서 남은 0 = 진입 수 ≥ 한도이고, 그때 붕괴 예정이 아닌 것은 꺼진(`Active` false) 약한 지반뿐이다(설계 SP2) |
  | 붕괴 | `Effect(WeakTerrian, 0)` | 에셋 0 | 예 |
  | 추락(보인 캐릭터) | `EffectWithoutPreset(Fall)` | 칸 -1 → `EffectWithoutPreset(Fall)` | 예 |
  | 장애물, 외형 -1 | `EffectWithoutPreset(slot)` | 같은 식 | 예 |
  | 장애물, 외형 0 이상 | `Effect(slot, n)` | 같은 식 | 예 |
  | 공격 슬롯 | `>= 0` | `IsSound` = `>= 0` | 예 |
- **12기준 확인 줄:** C1 진입 규칙 숫자·붕괴 숫자가 코드에 없음, "-1 = 프리셋 없이 냄"은 함수 하나. C5 옛 도우미 둘을 지웠고 새 멤버는 모두 부르는 곳이 있음. C8 직렬화 목록을 밖에 내주지 않음. C11 진입 사건마다 할당 없음(View 서비스 수명에 복사 한 번 — §10.4 L1로 고침). C12 옛 주석("여기 하나입니다", "장애물 반응)", "유닛·구조물 공용", "Q-ES4") 0곳.
- **다른 작업자 줄:** 현강님 줄(추락·진입 갈래, 붕괴 줄, 진입 도우미, `Attack` 판정, 장애물 도우미 서명, 순간 표 넷, 프리셋 답 요약 16)을 바꿨다 → SM-c 현강님 구조 공유 §30(§9 그대로).

### 10.3 SM-b 곡사 공통 수명 (2026-11-12 사용자 "SM-b 진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **바꾼 파일 넷(`V/Presentation/Attack/`, 커밋 하나):**
  - `CombatAttackMotion.cs`(기반): using `ErrorHandling`, 생성자 검사 셋으로 나눔(`target`·`up`·`duration`, TP-18), `Begin` = `BeginResources()` → `Render(0)` → `if (IsRunning) Advance(0, 0)`, `Tick` 쓸 수 없음 블록 → `HandleUnavailable()`, 새 멤버 `BeginResources`(가상)·`HandleUnavailable`(protected, 첫 줄 `if (!IsRunning) return;` — TP-10)·`ReleaseResources(ref List<Exception>)`(옛 무인자 판을 바꿈), `Cleanup` = 목록 null → 훅 → `CleanupErrors.Try(Unit.RestorePose)` → `ThrowIfAny`(문구 그대로). 클래스 요약·`Render`·`Advance`·훅 문서(TP-11·TP-12).
  - `CombatLinearAttackAnimation.cs`: `Advance` 첫 줄의 `|| !IsRunning` 지움, `ReleaseResources(ref errors)` = `CleanupErrors.Try(() => _pool.Recall(lease), ref errors)`, using 둘.
  - `CombatMeleeAttackAnimation.cs`: `Advance` 첫 줄의 `|| !IsRunning` 지움.
  - `CombatMortarAnimation.cs`: `: CombatAttackMotion`으로 다시 씀(305 → 189줄). 남은 필드는 Tb.4 목록 그대로(끝 시각 필드 없음 — TP-13). 훅 `ResourcesAvailable`·`BeginResources`·`Render(p) => Pose(Elapsed)`·`Advance`·`ReleaseResources`, 비공개 `Fire`·`Pose`·`CanUseWeapon`, 정적 `EndTimeOf`·`FireAtOf`·`SumSeconds`(옛 `Duration`, 문구 그대로). 옛 수명 필드·깃발 다섯·`Begin`·`Tick`·`Cancel`·`Release`·`HandleUnavailable`·`Cleanup`·`CleanupFailure`·`TryCleanup`·`Duration`·`RequireFinite`·`CanRender` 지움.
- **계획과 다른 점 하나:** `Pose`의 유닛 자세 뒤 확인을 계획 Tb.4의 `!IsRunning || !CanUseWeapon()` 대신 `!IsRunning || !Unit.IsAvailable || !CanUseWeapon()`으로 썼다. 옛 215줄 `!CanRender()`는 유닛 확인을 포함했다. 설계 MT3·MT4가 이 자리를 "옛 코드와 같게"라고 했는데, 계획 줄대로면 유닛이 자세 적용 중에 꺼졌을 때 무기 자세를 한 번 더 쓴다(무해하지만 옛 코드와 다름). 이렇게 쓰면 옛 코드와 같다.
- **Tb.5 생성 줄:** `CombatMortarStyle.cs:88-89`의 생성 인자는 바꾸지 않았다. 생성자 서명이 그대로다.
- **Tb.6 동작 대조(코드 읽기):**
  - 설계 MT4 다섯 줄은 바꾼 코드에서 그대로다. 정상 길의 그리기·발사·착탄·정리 순서가 같다.
  - 발사 순간 0: 시작 때 `Render(0)` → `Pose(0)`, `Advance(0, 0)` → `Pose(0)` → `Fire()`. 같은 자세를 두 번 그리고 발사는 한 번이다(옛 `Begin`의 `_fireAt == 0` 갈래와 결과 같음).
  - 큰 프레임(한 Tick에 발사 순간과 끝을 넘음): 기반이 경과를 끝 시각까지 올림 → `Advance`가 `Pose(_fireAt)` → `Fire()` → (돌면) `Render` → `Pose(끝)`. 착탄 전이라 `IsComplete` 거짓이다.
  - 발사 중 취소(`FireArc` 안 재진입): `IsCurrent(version)`이 거짓이라 대여를 회수하고 `_hasFired`는 거짓으로 남는다.
  - 착탄이 정리 뒤에 옴: 콜백 첫 줄 `IsCurrent(version)`이 거른다.
  - 무기 `OnDisable`이 Tick 밖·안에서 옴: 무기가 자세를 먼저 끄고 콜백 → 기반 `HandleUnavailable` → 주인 취소·`Cancel`. 정리의 무기 자세 되돌리기는 지금 판이 아니라 건너뛴다(옛 코드와 같음). Tick 안이면 Tick 끝의 `if (IsRunning) Render(...)`가 건너뛴다.
  - 시작 전 `Cancel`: 기반 정리가 돌고(콜백 풀기 = 연결 없음, 탄 없음, 무기 자세 지금 판 아님, 유닛 자세 되돌림) 그 뒤 `Begin`은 예외다. 옛 곡사와 같다.
  - `Validate` 실패: `BeginResources` 안(`BeginPose` 첫 줄)에서 나서 기반 `Fail` → `Cancel` → 정리 후 다시 던진다. `CombatAttackPresenter.CleanupFailure`의 `Cancel`은 이미 정리돼 아무것도 안 한다(설계 §4).
  - 직선·근접: `Render`가 취소하지 않아 `Begin`의 `if (IsRunning)`은 늘 참이다. `Advance` 진입 때 늘 돌고 있어 지운 `!IsRunning`은 늘 거짓이었다. 정리 실패를 모으는 결과도 같다(옛 기반은 훅 전체를 try로 감쌌고, 지금은 직선 훅이 회수 한 단계를 `Try`로 감쌈).
- **설계 §4에 더할 오류 길 차이 하나(SM-c에서 고침):** 곡사 시작 때 유닛을 쓸 수 없으면 예외 종류는 같고(`InvalidOperationException`) 문구가 기반 문구 "[CombatAttackMotion] 현재 활성 대여의 유닛 자세가 필요합니다."로 바뀐다.
- **Tb.7 grep(`V/Presentation/Attack/`의 네 파일):** `TryCleanup`·`CleanupFailure`·`_cleanupDone`·`RequireFinite` 0곳(같은 폴더 `CombatAttackPresenter`의 `CleanupFailure`는 다른 함수, §2.3 C3-03 나머지). `new List<Exception>()` 0곳. 무인자 `ReleaseResources()`를 부르거나 덮는 곳 0곳(튜토리얼 `TutorialViewSystem`의 같은 이름 비공개 함수는 무관). `Assets/Tests` 참조 0곳 그대로. 생성자 서명이 그대로라 생성 줄(`CombatMeleeStyle`·`CombatLinearStyle`·`CombatMortarStyle`)은 고치지 않는다.
- **12기준 확인 줄:** C1 수명 깃발·정리 규칙 사본 0(곡사에 `_begun` 등 없음), 발사 순간 식 하나. C3 기반·곡사·직선 정리가 공용 `CleanupErrors`. C5 `Advance` 진입 검사 한 층. C6 생성자 인자 이름 셋, 무기 Unity null 확인 유지. C8 새 기반 멤버 `protected`, 곡사 필드 `private`. C9 정리 순서(콜백 풀기 → 탄 회수 → 무기 자세 → 유닛 자세) 같음. C11 정리 목록은 실패할 때만, 정리마다 대리자 할당은 그대로(공격 하나에 한 번). C12 옛 곡사 문서의 "공통 기반 밖" 같은 문구 없음.
- **다른 작업자 줄:** 없음(네 파일 모두 우리 줄, §10.1).

### 10.4 SM-c 문서·공유·검토·아티팩트 (2026-11-12 사용자 "SM-c 진행해", 코드 변경 없음)

- **Tc.1 묶음 끝 검토(새 에이전트 하나, 읽기 전용):** 기준 판 `03d3555aa` 대비 diff 전체(코드 아홉·에셋 하나)를 봤다. 컴파일 오류 대조 0, 높음 0, 중간 0, 낮음 8, 약 8.8. 값 대조(Ta.7)와 동작 대조(Tb.6)는 검토자도 같다고 봤다. 낮음은 고치지 않고 권장과 함께 사용자 결정으로 넘긴다.

  | id | 기준 | 지적 | 권장 |
  |---|---|---|---|
  | L1 | C12 | "전투마다 한 번 복사"는 틀림. 프리셋 답은 `CombatDisplayTables.From`에서 만들고, 그것을 부르는 곳은 `CombatViewSystem.InitData`(View 서비스 수명에 한 번)다. 그래서 재생 중에 에셋의 진입 목록을 바꾸면 다음 재생까지 반영되지 않고, 붕괴·추락 칸은 바로 반영된다. 틀린 문구 자리: `CombatSoundPresets.cs:52`, `CombatPresentationSettingsDataSO.cs:254`, 설계 SP2, 계획 TP-2, 호진님 설정 표 메모 §17 마지막 줄 | 문구만 "View 서비스 수명에 한 번(재생 시작 때)"으로 고침. 읽는 시점을 다른 두 칸과 맞추려면 복사를 걷어야 해 TP-2를 뒤집으므로 권장하지 않음 |
  | L2 | C12 | 옛 "-1 = 소리 없음" 문구가 둘 남음: `ValidateSoundPreset` 요약(`CombatPresentationSettingsDataSO.cs:332`), 예외 문구 "-1(소리 없음)"(`CombatPresentationValueRules.cs:71`) | 둘 다 "-1(프리셋 없음)"으로 고침(주석·문구만) |
  | L3 | C12 | `fallCues` 툴팁(`CombatPresentationSettingsDataSO.cs:189`)이 "유닛·구조물(장애물)"의 큐를 적고 끝에 "소리는 위 추락 프리셋이 냅니다"라고 함. 추락 소리는 유닛만 낸다(Q-SP2 A) | 끝 문장을 "소리는 유닛만 위 추락 프리셋이 냅니다"로 고침 |
  | L4 | C1 | `Attack`이 슬롯 번호에 프리셋 규칙 `IsSound`를 씀. 슬롯 번호 규칙(-1 = 슬롯 없음)은 `UnitVisualDataSO.cs:88`에 따로 있음 | 그대로(TP-7에서 정함, 두 규칙의 값이 같음). 슬롯 규칙 함수를 따로 둘지는 다음 후보 |
  | L5 | C10 | 진입 목록에 `[Min(-1)]`이 없어 인스펙터에서 -2를 넣을 수 있고(부트 DB 검사에서 막힘), 예외에 원소 순번이 없음 | `[SerializeField, Min(-1)]`(Unity는 목록 원소마다 적용)과 예외 인자 이름에 순번(`weakGroundEntrySoundPresets[i]`)을 더함 |
  | L6 | C2 | 곡사 `Render(progress)`가 인자를 쓰지 않고 `Elapsed`로 그림 | 그대로(설계 MT3, 문서에 적음) |
  | L7 | C5 | 곡사 `Fire()` 첫 줄의 `_hasFired` 확인이 `Advance` 확인과 겹침 | 그대로(설계 M2 반영 — `Fire`의 옛 확인 자리를 그대로 둠) |
  | L8 | C1 | 사건 순간 표에 `Fall` 줄이 없음 | 그대로(Q-SP4 A, 주석에 이유) |

  - L1을 따라 Unity 확인 U-5의 순서를 "재생 전에 에셋을 바꿈"으로 고쳤다(§8). 재생 중에 바꾸면 반영되지 않아 확인이 틀리게 보인다.
- **Tc.2 문서:**
  - 사운드 작업자 안내: 제목에 갱신 주제·날짜, 머리 덧붙임, §2.2 순번 덧붙임(SFX 5 `WeakTerrian`·Unit 12 `Fall`), §3 추락·진입·붕괴 줄 = "나옴", §4.2 소개·세 줄(설정 표 칸 셋), §6 새 슬롯 요청 줄 지움 표시(필요 없음).
  - 이펙트·소리 창구 안내: 제목, 36-37줄(추락·붕괴 소리는 설정 표 칸).
  - 폴더 배치 안내: 머리 갱신 줄, §2 공격 종류 줄(135)에 코드 모션 수명 = `CombatAttackMotion` 하나(곡사 포함), 소리 프리셋 줄(141)에 약한 지반·추락 칸과 `EffectKeepingPreset`.
  - 설계: §4 검사 순서 줄 정정(TP-16)·곡사 시작 오류 문구 줄, 새 §10 구현 메모. §7 "결정" 칸은 이미 있어 그대로.
- **Tc.3 공유(모두 미전달):**
  - 현강님 구조 공유 새 §30(바뀐 줄 표, §23 첫 질문은 설정 표로 정함, 확인 부탁 = 추락 소리 유닛만), §23 C1-38 질문에 "정함" 표시.
  - 호진님 설정 표 메모 새 §17(칸 셋·머리말 나눔·에셋 세 줄·목록 칸 관례).
  - 정정 표시 둘: 호진님 구조 공유 §17·천명님 특성 공유 §7의 C1-42 질문(곡사·기반은 용근 줄, SM-b 때 표시).
  - 민형님 없음(Factory 무변경).
- **Tc.4 평가 아티팩트 15차(재평가 기준, 추정):** C1-38·C1-42 다 풀림(C1 60 → 75), 낮음 C1-N19·C6-09·C12-N10 풀림, C3-03 줄어듦. 요청 7기준 82.5 → 84.6, 12기준 85.0 → 86.3, 중간 8 → 6. 재평가 절 메모·남은 중간 목록·순서 표, 11-04 중간 카드·칩, 적용 기록 15차 카드, `applied-data`(차수·남은 것·재평가 뒤 적용)를 함께 고쳤다. 컴파일·Unity 확인 전이다.
- **Tc.5 기록:** 이 절, 머리 상태 줄, CLAUDE.md 11-12 줄.
- **남은 것:** 낮음 L1~L8 결정(사용자), Unity 확인 U-1~U-11(§8), 커밋(§7 묶음 — SM-a는 설정 SO 코드와 에셋 한 커밋), 공유 전달.

### 10.5 묶음 끝 검토 낮음 반영 (2026-11-13 사용자 "권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

§10.4의 권장대로 L1·L2·L3·L5를 고쳤다. L4·L6·L7·L8은 그대로다. 동작은 L5의 검사 예외 인자 이름(순번)과 인스펙터 최솟값 말고 같다.

- **L1(주석·문서):**
  - `CombatSoundPresets` 생성자 주석, `CopyWeakGroundEntrySoundPresets` 문서: "전투마다 한 번" → "이 객체를 만들 때 한 번 = View 서비스가 시작할 때(재생 시작 때)", 재생 중 진입 목록 변경은 다음 재생부터, 붕괴·추락은 사건마다 읽음.
  - 설계 SP2 할당 줄·§10 검토 줄, 이 계획 TP-2·§2.1 C11 줄·§10.2 C11 확인 줄, 호진님 설정 표 메모 §17 마지막 줄(미전달)을 같은 뜻으로 고쳤다.
- **L2(주석·예외 문구):** `ValidateSoundPreset` 요약 "-1 = 소리 없음" → "-1 = 프리셋 없음(소리를 안 내는지, 프리셋 없이 내는지는 그 칸의 툴팁)", `CombatPresentationValueRules.RequireSoundPreset` 예외 문구 "-1(소리 없음)" → "-1(프리셋 없음)". `Assets/Tests`에서 이 문구를 확인하는 곳은 0곳이다.
- **L3(툴팁):** `fallCues` 툴팁 끝 문장 "소리는 위 추락 프리셋이 냅니다" → "소리는 유닛만 위 추락 프리셋이 냅니다".
- **L5(인스펙터·검사):** 진입 목록 칸 `[SerializeField, Min(-1)]`(Unity는 목록 원소마다 적용). 검사는 `for` 반복으로 바꿔 예외 인자 이름에 순번을 넣었다(`weakGroundEntrySoundPresets[1]` 등). 이름 문자열은 검사 때 원소마다 하나 만들지만, 검사는 DB를 만들 때 한 번이라 자주 도는 길이 아니다.
- **바꾼 파일:** 코드 셋(`CombatSoundPresets.cs`·`CombatPresentationSettingsDataSO.cs`·`CombatPresentationValueRules.cs`, 모두 우리 줄 — §10.1 표), 문서 셋(설계·이 계획·호진님 메모). 에셋 변경 없음. 커밋 묶음은 §7 SM-a(같은 파일)와 같다.
- **다른 작업자 줄:** 없음. 공유 추가 없음(호진님 §17은 덧붙임 한 줄).
- **찾았지만 손대지 않은 것(바꾸는 파일 밖, §2.3 다음 후보):** `CombatCueEntries.cs:305` 주석 "-1(소리 없음) 항목"(프리셋 소리 큐 항목 — 그 자리에서는 소리 없음 뜻이 맞아 고칠 필요가 크지 않음).
- **평가 아티팩트:** 낮음만 풀려 점수가 그대로라 차수는 더하지 않았다(낮음은 기준마다 상한). 15차 기록 그대로.

### 10.6 커밋·push (2026-11-13 사용자 "지금까지 한거 커밋하고 푸쉬하는데 하나로 묶어서 해")

사용자 지시로 Claude가 SM-a·SM-b·SM-c와 §10.5 낮음 반영을 **한 커밋**으로 커밋하고 `feature/Yonggeun/Refector-Stage`에 push했다. §7의 세 묶음(SM-a·SM-b·SM-c) 대신 하나로 묶었다(SM-a의 설정 SO 코드와 에셋이 한 커밋이어야 하는 조건은 그대로 지켜짐). 컴파일 미확인, 새 시험 없음, 시험 변경 없음. 남은 것: Unity 확인 U-1~U-11, 공유 전달.
