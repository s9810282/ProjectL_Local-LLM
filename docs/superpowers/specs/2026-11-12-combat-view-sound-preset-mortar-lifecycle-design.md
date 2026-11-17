# 전투 View 소리 프리셋 원본·곡사 공통 수명 설계 (C1-38·C1-42)

> 2026-11-12 2판(독립 검토 반영, §9). 사용자 "C1-38 설정 표 원본으로, C1-42까지 설계부터 진행해". 코드 변경 없음.
> **결정 완료(2026-11-12 사용자 "권장안대로 하고 Task 계획 써줘, 기존 12기준 생각해서 플랜짜"): Q-SP1~Q-SP4·Q-MT1~Q-MT3 모두 A.** Task 계획 [sound-preset-mortar-lifecycle-plan](../plans/2026-11-12-combat-view-sound-preset-mortar-lifecycle-plan.md)(12기준 점검 §2, 계획에서 정한 것 TP-1~TP-18 — 이 설계와 다른 것은 TP-2·TP-11·TP-13·TP-16, 설계 밖 낮음 함께 고침은 TP-7·TP-8).
> 대상: [재평가 문제 목록](2026-11-11-combat-view-reevaluation-issues.md)의 남은 SSOT 중간 넷 가운데 우리 영역 둘.
> 경로 줄임: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/`. 줄 번호는 HEAD `03d3555aa` 기준이다.

## 0. 요지

- **C1-38**: 약한 지반 소리와 추락 소리의 원본을 전투 연출 설정 표 하나로 옮긴다. 코드 숫자는 지운다.
  - 기본값(코드·에셋)은 지금 나는 소리와 같게 맞춘다. 그래서 기획자가 표 값을 바꾸기 전까지 소리는 그대로다.
- **C1-42**: 곡사 애니메이션 `CombatMortarAnimation`이 공통 기반 `CombatAttackMotion` 위에서 돌게 한다. 기반에 훅을 더하고, 곡사의 수명 사본(깃발 여섯·Tick·취소·정리)을 걷는다.
  - 재생 순서와 정리 순서는 그대로다. 바뀌는 것은 잘못된 값이 들어왔을 때의 예외 종류·문구뿐이다(§4).
- 둘 다 동작이 같고, Factory·Core·Framework 변경은 없다. 새 시험은 쓰지 않는다. 기존 시험 중 고칠 곳도 없다(§5.3).
- 예상(같은 감점표에서 중간 둘만 뺀 값): C1 60 → 75, 요청 7기준 82.5 → 84.6, 12기준 85.0 → 86.3.

## 1. 지금 코드 (확인한 사실)

### 1.1 C1-38 소리 프리셋

전투 연출 설정 표(`DV/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`)의 "전투 소리 프리셋" 묶음에 칸 둘이 있다.

| 칸 | 줄 | 기본값(코드·에셋) | 툴팁 | 읽는 곳 |
|---|---|---|---|---|
| `fallSoundPreset` | 178-180 | -1 · -1 | "유닛·구조물이 낭떠러지로 떨어지기 시작할 때. -1이면 소리를 내지 않습니다" | **없음** |
| `weakGroundCollapseSoundPreset` | 181-183 | -1 · -1 | "약한 지반이 무너져 낭떠러지가 될 때. -1이면 소리를 내지 않습니다" | `CombatSoundPresets.WeakGroundCollapse()`(77-78)만 있고, 그것을 부르는 곳이 **없음** |

에셋은 `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` 84-85줄이다.

실제 소리는 `V/Playback/Support/CombatEventSounds.cs`가 코드 숫자로 낸다.

| 순간 | 줄 | 지금 내는 소리 | 작성 |
|---|---|---|---|
| 추락 | 141-144 | `UnitEvent.Fall`을 **프리셋 없이**(`EffectWithoutPreset`). 보인 캐릭터만 내고, 구조물이 떨어지면 내지 않는다 | 현강님 `c0f92a5d8`(11-10) |
| 약한 지반 진입 | 145-146, 189-197 | `SfxEvent.WeakTerrian`, 프리셋 = `min(2, 남은 진입 횟수)`. 붕괴 예정 진입(남은 0)은 내지 않는다 | 현강님 `8fe4a688b`(11-02)·`c0f92a5d8` |
| 약한 지반 붕괴 | 169 | `SfxEvent.WeakTerrian`, 프리셋 **0** | 현강님 `c0f92a5d8` |

Boot 오디오 칸(`Assets/Scenes/Boot.unity`)을 대조했다.

- SFX 5번 `WeakTerrian`(1685-1762줄)에는 프리셋이 셋 있다. FMOD 변수 `CrackLv`를 0·1·2로 둔다. 지금 규칙은 "프리셋 번호 = 남은 진입 횟수(최대 2), 붕괴 = 0"이다.
- Unit 12번 `Fall`(1527-1541줄)에는 **프리셋이 없다**(`parameterPresets: []`).
  - `AudioSystem.ApplyPreset`은 0 이상 번호가 목록 밖이면 예외를 던진다(`AudioSystem.cs:418-426`).
  - 그래서 추락 칸에는 지금 "프리셋 없이 냄"(-1 = 프리셋을 바꾸지 않음, `AudioSystem.cs:86`)만 쓸 수 있다.
- 약한 지반의 진입 한도는 지형 에셋 `Terrain_WeakGround.asset`의 `entryThreshold: 2`다(스테이지의 옛 `staticTerrainPlacements`는 모두 비어 덮어쓰는 값이 없다). 지금 데이터로는 진입 1회(남은 1 → 프리셋 1)와 붕괴(프리셋 0)만 난다. 프리셋 2는 한도가 3 이상일 때 남은 횟수가 2 이상인 진입에서 난다.
- `FmodEventPlayer`는 칸마다 지금 프리셋(`currentPresetIndex`)을 기억한다. -1로 내면 그 기억을 바꾸지 않는다. 그래서 "-1 = 칸 기본 소리"는 그 칸에 프리셋이 없거나 아무도 프리셋을 적용한 적이 없을 때만 맞다. `Fall` 칸은 프리셋이 없어 늘 맞다.

정리하면, 설정 표의 두 칸은 기획자가 바꿔도 아무 일이 없고, 툴팁("-1이면 소리 없음")과 실제(소리가 남)가 반대다. 진입 프리셋은 표에 칸조차 없다.

### 1.2 C1-42 곡사 공통 수명

- 기반 `V/Presentation/Attack/CombatAttackMotion.cs`(181줄)는 공격 동작 하나의 수명을 맡는다.
  - 한 번만 시작, 외부 Tick·중첩 금지, 쓸 수 없으면 `cancelOwner` 뒤 `Cancel`, `Release` 멱등, 정리 예외 묶기, 실패 때 `Cancel` 뒤 다시 던짐.
  - 직선 `CombatLinearAttackAnimation`(17줄)·근접 `CombatMeleeAttackAnimation`(13줄)이 상속한다.
- 곡사 `V/Presentation/Attack/CombatMortarAnimation.cs`(305줄)는 상속하지 않는다(18줄 `: ICombatAttackAnimation`).
  - 같은 수명 규칙을 깃발 여섯(`_begun`·`_cancelled`·`_released`·`_cleanupDone`·`_ticking`·`_version`)과 `Begin`·`Tick`·`Cancel`·`Release`·`HandleUnavailable`·`Cleanup`·`CleanupFailure`·`TryCleanup`으로 다시 적었다.
  - 기반과 이미 어긋난 곳이 있다. 유닛 위치가 유한하지 않으면 기반은 `InvalidOperationException`, 곡사는 `ArgumentOutOfRangeException`을 던진다. 정리 실패 문구와 실패 묶음 모양도 다르다.
- 곡사에만 있는 것은 무기다(`V/Display/Unit/CatusCannonWeaponView.cs`).
  - 시작할 때 무기 자세를 빌리고(`BeginPose` → 판 번호) 무기가 꺼질 때의 취소 콜백을 건다(`BindCancellation`).
  - 그리기마다 무기를 쓸 수 있는지 다시 본다(`CanRender`). 그리는 도중 무기가 꺼지면 그 자리에서 취소한다.
  - 정리 때 콜백을 풀고, 탄을 회수하고, 무기 자세를 되돌린다.
  - 시간 축이 다르다. 기반은 하나의 길이(초)와 진행률(0~1)을 쓴다. 곡사는 준비 → 발사 → 복귀를 초 단위로 그린다. 끝 = `max(준비 + 발사, 발사 순간 + 복귀)`이고, 발사 순간 = 준비 + 발사 × 발사 비율이다.
- 생성은 `V/Presentation/Attack/CombatMortarStyle.cs:84-91` 한 곳이다. 유닛 자세는 취소 콜백 없이 빌리고(`BeginAttackPose(attackerId, null)`), 쓸 수 없음은 애니메이션이 `context.CancelUnavailable`로 알린다.
- 곡사에는 이미 정적 도우미 `private static float Duration(double)`(290줄)이 있다. 기반에 같은 이름을 더하면 가려진다(§3 MT1에서 피함).
- 정리 예외를 모으는 공용 규칙 `Assets/ProjectL/Script/Systems/RuntimeSystem/ErrorHandling/CleanupErrors.cs`(`Try(Action, ref List<Exception>)`·`ThrowIfAny`)가 있고 72개 파일이 쓴다. 기반 `Cleanup`(130-153)과 곡사 `TryCleanup`(276-286)은 이것을 쓰지 않고 손으로 적었다.
- **작성자(GitHub 파일 이력으로 확인):** 기반은 용근 `dad08ef1`(10-13), 곡사는 용근 `37188bd5`(10-13)에서 생겼다. 그 뒤 바뀐 커밋도 모두 용근이다.
  - 얕은 clone의 blame은 경계 커밋(`^4782921e` 천명님, `^83fb2767` 호진님)으로 보인다. 11-05 공유 메모 둘이 이를 그대로 적어 "곡사 = 천명님 파일, 기반 = 호진님 파일"이라고 썼는데, 틀렸다. 우리 줄이라 공유가 필요 없다(§5.4 정정).

## 2. 설계 C1-38 — 소리 프리셋은 설정 표 하나

### SP1 붕괴 = 설정 표 칸

- `CombatEventSounds.Moment`의 붕괴 갈래(169)는 `presets.WeakGroundCollapse()`를 읽는다. 코드 숫자 0을 지운다.
- 칸 `weakGroundCollapseSoundPreset`의 코드 기본값과 에셋 값을 **-1 → 0**으로 바꾼다. 지금 소리와 같다.
- 툴팁 뜻("-1이면 소리 없음")은 그대로이고, 이제 실제와 맞는다.

### SP2 진입 = 설정 표의 남은 횟수별 목록

- 새 칸 `weakGroundEntrySoundPresets`(`int` 목록)를 "전투 소리 프리셋" 묶음의 붕괴 칸 바로 앞에 둔다.
  - 원소 i = 남은 진입 횟수가 (i + 1)인 진입의 프리셋이다.
  - 남은 횟수가 목록보다 크면 마지막 원소를 쓴다.
  - 빈 목록이면 진입 소리를 내지 않는다. 원소 -1은 그 횟수만 소리를 내지 않는다.
  - 기본값(코드·에셋)은 `[1, 2]`다. 지금 식 `min(2, 남은 횟수)`과 결과가 같다.
  - 남은 횟수가 0 이하면 진입 소리를 내지 않는다. 지금은 붕괴 예정 진입(남은 0)을 앞에서 거르므로 닿지 않는다.
  - 툴팁: "약한 지반에 들어갈 때의 효과음 프리셋입니다. 첫 원소는 남은 진입 횟수 1, 둘째는 2, … 남은 횟수가 목록보다 많으면 마지막 원소를 씁니다. -1은 그 횟수에 소리를 내지 않고, 빈 목록은 진입 소리를 내지 않습니다. 무너지는 진입은 붕괴 칸이 냅니다."
- 답하는 곳은 `CombatSoundPresets.WeakGroundEntry(int remaining)` 하나다. "넘으면 마지막 원소"와 "0 이하는 없음" 규칙도 여기 한 곳에 둔다. 소리 표는 진입인지와 남은 횟수만 판단하고, 번호는 묻는다.
  - 지금 `WeakGroundEntryPreset`은 `TryGetWeakGroundEntry(source, out int remaining)`이 된다. 조건(전후 모두 약한 지반, 진입 수 증가, 붕괴 예정 아님)은 그대로다.
- 검사: 원소마다 기존 `ValidateSoundPreset`(-1 또는 0 이상). 목록 자체가 없으면(null) 예외를 던진다.
- 할당: Data는 읽기 전용 목록을 낸다. `CombatSoundPresets` 생성자(표시 표 `CombatDisplayTables.cs:63`가 만듦 — 그것을 부르는 `CombatViewSystem.InitData`는 View 서비스 수명에 한 번이라, 재생 중에 진입 목록을 바꾸면 다음 재생부터 반영된다. 11-13 SM-c 검토 L1로 "전투마다 한 번"을 고침)가 그것을 `int[]`로 한 번 복사한다. 진입 사건마다 할당이 없다.

### SP3 추락 = 설정 표 칸, "-1 = 프리셋을 지정하지 않고 냄"

- 추락 칸에는 Boot 프리셋이 없어 0 이상 번호를 쓸 수 없다(§1.1). 그래서 `fallSoundPreset`의 뜻을 장애물 소리와 같게 바꾼다.
  - -1이면 프리셋을 지정하지 않고 낸다(칸의 지금 프리셋. `Fall` 칸은 프리셋이 없어 늘 기본 소리).
  - 0 이상이면 그 프리셋으로 낸다. Boot `Fall` 칸에 프리셋을 먼저 더해야 한다. 없으면 DB 검사는 지나도 재생 때 AudioSystem이 예외를 던진다(다른 프리셋 칸과 같은 한계).
- 장애물 소리 도우미(`CombatEventSounds.Obstacle`, 199-205)가 이미 이 규칙이다. 규칙을 소리 단서의 함수 하나로 올린다: `CombatSoundCue.EffectKeepingPreset(UnitEvent slot, int preset)`. 장애물 피해·파괴와 추락이 함께 부른다.
  - 이름은 "프리셋을 유지한 채 냄"이다. "-1 = 소리 없음"인 `Effect`와 헷갈리지 않게, 기본 소리라는 말을 쓰지 않는다.
  - 장애물 쪽 주석 "-1 뜻이 다른 곳은 여기 하나입니다"는 "-1 뜻이 다른 곳은 이 함수를 부르는 곳(장애물·추락)입니다"로 고친다.
  - `CombatSoundPresets.None`(27)·`CombatPresentationValueRules.NoSoundPreset` 문서에 "받은 쪽이 `Effect`로 내면 소리 없음, `EffectKeepingPreset`으로 내면 프리셋 없이 냄(장애물·추락)"을 적는다.
- 추락 갈래(141-144)는 보인 캐릭터만 내는 조건을 그대로 두고 `EffectKeepingPreset(UnitEvent.Fall, presets.Fall())`을 낸다. `CombatSoundPresets.Fall()`을 되살린다.
- 칸 기본값 -1과 에셋 값 -1은 그대로다. 지금 소리와 같다.
- 툴팁을 실제에 맞춘다(Q-SP2·Q-SP3): "유닛이 낭떠러지로 떨어지기 시작할 때의 효과음 프리셋입니다. -1이면 프리셋을 지정하지 않고 냅니다(Boot Fall 칸에는 지금 프리셋이 없어 기본 소리). 0 이상을 쓰려면 Boot Fall 칸에 그 프리셋을 먼저 더해야 하며, 없으면 재생 때 오류입니다. 유닛의 이어지는 사망 소리는 따로 납니다."
- 이 칸만 같은 머리말 아래의 다른 칸("-1 = 소리 없음")과 -1 뜻이 다르다. 그래서 칸 이름 옆 머리말을 나눠 "전투 소리 프리셋(-1 = 프리셋 없이 냄)" 아래에 둔다(Q-SP3).

### SP4 설정 표·안내 정리

- 머리말·툴팁: 소리 표 `CombatEventSounds` 클래스 문서의 "소리 창구 2차" 문장, `CombatSoundPresets` 클래스 문서(적 공용·배경음·추락·약한 지반 붕괴·진입)를 고친다.
- 이 칸들의 뜻을 적은 문서 셋을 고친다.
  - 사운드 작업자 안내 `docs/superpowers/handoffs/2026-10-28-combat-sound-guide-for-sound-worker.md`: 추락·붕괴 줄(104-105·160-161)이 "슬롯 대기"로 낡았다. 지금 칸(`Fall`·`WeakTerrian`)과 설정 표 칸 셋으로 고친다.
  - 이펙트·소리 창구 안내 `docs/superpowers/handoffs/2026-10-29-combat-effect-sound-cue-guide.md` 36-37줄.
  - 호진님 설정 표 메모 `docs/superpowers/handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md` 121-122줄. 설정 표 칸을 더할 때마다 이 메모에 절을 더해 온 관례대로 새 절을 쓴다.
- 사건 순간 표 `Moment`(160-172)에 `CombatMomentKind.Fall` 갈래가 없다. 추락 사건은 앞 갈래(141)가 먼저 받아 닿지 않지만, 다른 사건이 추락 순간을 내면 예외가 난다. 이번에는 주석 한 줄로 이유만 적는다. 고치는 것은 범위 밖이다(§7 Q-SP4).

### SP5 바꾸지 않는 것

- 소리를 언제 내는지(사건 순간), 어느 칸에 내는지(`WeakTerrian`·`Fall`)는 그대로다.
- 붕괴 예정 진입을 건너뛰는 규칙, 보인 캐릭터만 추락 소리를 내는 조건도 그대로다.
- Boot 씬·AudioSystem·FMOD 이벤트는 손대지 않는다.

## 3. 설계 C1-42 — 곡사도 공통 기반 위로

### MT1 기반에 더하는 것

| 더하는 것 | 모양 | 쓰는 곳 |
|---|---|---|
| 시작 자원 훅 | `protected virtual void BeginResources() {}` | `Begin`의 try 안, `_begun = true`·판 번호 올린 뒤, `Render(0)` 앞. 실패하면 지금처럼 `Fail` → `Cancel` → 다시 던짐 |
| 쓸 수 없음 처리 | `protected void HandleUnavailable()` | 지금 `Tick`의 "`cancelOwner` 뒤 정리 안 됐으면 `Cancel`" 블록을 이 함수로 옮긴다. 정리가 끝났거나 놓았으면 아무것도 안 한다. 곡사는 그리는 도중·무기 꺼짐 콜백에서 이것을 부른다. 이름은 옛 곡사 이름 그대로다(주인 쪽 `ICombatAttackStyleContext.CancelUnavailable`과 겹치지 않게) |
| 정리 단계 묶기 | `protected virtual void ReleaseResources(ref List<Exception> errors)` | 지금 `ReleaseResources()`를 바꾼다. 자식은 단계마다 공용 규칙 `CleanupErrors.Try(step, ref errors)`를 부른다. 기반 `Cleanup`도 `CleanupErrors.Try(Unit.RestorePose, ref errors)` → `CleanupErrors.ThrowIfAny(errors, 문구)`로 다시 쓴다. 손으로 적은 try/catch와 정리마다의 목록 할당이 없어진다(실패할 때만 목록을 만듦). 새 도우미는 만들지 않는다 |
| 시작 진행 확인 | `Begin`의 `Advance(0f, 0f)`를 `if (IsRunning)` 안에서 부른다 | `Render(0)`이 취소했으면 `Advance`를 부르지 않는다. 직선·근접은 `Render`가 취소하지 않아 동작이 같다 |

- 기반에 길이 속성은 더하지 않는다. 곡사에는 이미 정적 `Duration(double)`이 있어 이름이 겹친다(§1.2). 곡사는 기반의 `Elapsed`(이미 protected)를 쓴다(MT3).
- 직선 공격의 `ReleaseResources`는 새 모양으로 한 줄 바뀐다(`CleanupErrors.Try(() => _pool.Recall(lease), ref errors)`). 근접은 그대로다.
- 기반 생성자의 예외 인자 이름을 고친다. 지금은 목표·위쪽이 잘못돼도 `nameof(duration)`을 던진다. `target`·`up`·`duration`으로 나눈다.

### MT2 곡사가 남기는 것

- 남는 것: 설정, 매퍼(곡사 높이), 풀·계측, 무기, 발사 콜백, 맞음 콜백, 무기 꺼짐 콜백(생성자에서 기반 `HandleUnavailable`로 한 번 만듦), 발사 순간·끝 시각, 탄 대여, 무기 판 번호, 발사함·착탄함 깃발.
- 걷는 것: 유닛·목표·빈 방향·`cancelOwner`·원점·앞·위·방향·경과·판 번호 필드, 깃발 다섯(`_begun`·`_cancelled`·`_released`·`_cleanupDone`·`_ticking`), `Begin`·`Tick`·`Cancel`·`Release`·`HandleUnavailable`·`Cleanup`·`CleanupFailure`·`TryCleanup`·`RequireFinite`. 305줄이 약 170줄이 된다(추정).
- 정적 도우미 `Duration(double)`은 이름을 `SumSeconds(double)`로 바꾼다(기반과 겹치지 않게, 뜻 그대로).
- 기반 생성자에 넘기는 값: 유닛 자세, 목표, `mapper.WorldUp`, 빈 방향, 길이 = 끝 시각, `cancelOwner`.
  - 기반 호출식 안에서 값을 먼저 거른다(직선 `CombatLinearAttackAnimation.cs:43-44`와 같은 꼴). 정적 `EndTimeOf(settings)`가 null → 곡사 종류 → 합 계산(유한하지 않으면 옛 `InvalidOperationException`) 순으로 보고 끝 시각을 돌려준다. `mapper` null도 호출식에서 거른다.
  - 끝 시각 > 0은 성립한다. 곡사 코드 모션은 코드 모션 값이 있을 때만 시작하고(`CombatAttackPlayback.cs:185`), 그때 DB 검사가 발사 길이 > 0을 보장한다(`CombatAttackPresentationData.cs:93-104`).

### MT3 곡사 훅 구현

- `BeginResources`: 무기 자세를 빌리고(`BeginPose`, 안에서 `Validate`) 꺼짐 콜백을 건다.
- 무기 확인 `CanUseWeapon()`(private): `_weapon != null && _weapon.IsAvailable && _weapon.IsPoseCurrent(_weaponVersion)`. 파괴된 무기를 먼저 거르는 `!= null`(Unity null)을 빼지 않는다. 옛 `CanRender`(229)에서 유닛 확인만 뺀 것이다.
- `ResourcesAvailable`: `CanUseWeapon()` ∧ (발사 전 ∨ 착탄 ∨ 탄 대여가 살아 있음). 지금 `Tick` 첫 검사와 같다. 유닛 쓸 수 있음은 기반이 본다.
- `Render(progress)`: `Pose(Elapsed)`. 기반이 `Render`를 부르는 곳은 시작(`Render(0)`, 이때 `Elapsed` = 0)과 Tick 끝(`Render(Progress)`, `Progress` = `Elapsed` / 길이) 둘뿐이라 `Elapsed`가 같은 값이다. 초로 되돌리는 곱셈을 하지 않아 옛 `Render(_elapsed)`와 비트까지 같다. 곡사 자신은 `Render`를 부르지 않고 `Pose`를 바로 부른다.
- `Pose(초)`: 지금 `Render(elapsed)` 본문 그대로다. 확인 자리 둘을 옛 코드와 같게 둔다.
  - 그리기 앞(옛 207-211): `!Unit.IsAvailable || !CanUseWeapon()`이면 `HandleUnavailable()` 뒤 멈춘다.
  - 유닛 자세 뒤(옛 215): `!IsRunning || !CanUseWeapon()`이면 **멈추기만** 한다. 취소하지 않는다(옛 코드도 여기서는 돌아가기만 했다. 취소는 다음 Tick 첫 검사가 한다).
- `Advance(previous, next)`: 첫 줄 `if (_hasFired || next < _fireAt || !IsRunning) return;`(직선 71줄과 같은 꼴). 그다음 `Pose(발사 순간)` 뒤 `Fire()`. 지금 `Tick`이 큰 프레임에서 발사 순간 자세를 먼저 그리는 것과 같다. 시작 때(`Advance(0, 0)`) 발사 순간이 0이면 바로 쏜다. 지금 `Begin`의 `_fireAt == 0` 갈래와 같다.
- `Fire()`의 확인 자리 셋을 옛 코드 자리 그대로 기반 판정으로 바꾼다.
  - 입구(옛 172): `if (_hasFired || !IsRunning) return;`
  - 착탄 콜백(옛 186): `if (!IsCurrent(version)) return;`
  - `FireArc` 직후(옛 194): `if (!IsCurrent(version))`이면 방금 받은 대여를 회수하고 돌아간다.
  - `IsCurrent(version)`은 `IsRunning && version == 지금 판`이라 옛 식 `version != _version || _cancelled || _released`와 같다.
- `ReleaseResources(ref errors)`: 콜백 풀기 → 탄 회수 → 무기 자세 되돌리기(`_weapon != null`이고 지금 판이면). 단계마다 `CleanupErrors.Try`. 기반이 그 뒤 유닛 자세를 되돌린다. 지금 순서(무기 해제 → 탄 회수 → 무기 자세 → 유닛 자세)와 같다.
- `IsComplete`: `IsRunning && PoseDone && _flightDone`. 지금 식과 같다.

### MT4 같은 순서임을 대조할 곳

| 길 | 지금 곡사 | 바꾼 뒤 | 같은가 |
|---|---|---|---|
| 시작 | 유닛 확인 → `Validate` → 원점 확인 → 방향 → 시작 표시 → 무기 빌림·콜백 → 그리기 → (발사 순간 0이면) 발사 | 기반: 유닛·원점 확인 → 방향 → 시작 표시 → `BeginResources`(무기 빌림 안에서 `Validate`)·콜백 → 그리기 → (아직 돌면) `Advance(0,0)`(발사 순간 0이면 발사) | 정상 길은 같음. 발사 순간이 0이면 같은 자세(0초)를 두 번 그림 — 같은 값이라 화면은 같음. `Validate` 실패 시점은 §4 |
| Tick | 쓸 수 없으면 취소 → 다음 시각 → 발사 순간 넘으면 그 자세·발사 → 경과 반영 → 그리기 | 기반: 쓸 수 없으면 `HandleUnavailable` → 경과 반영 → `Advance`(그 자세·발사) → (아직 돌면) 그리기 | 경과를 발사보다 먼저 반영함. 발사 중 경과를 읽는 곳이 없어(발사·`_fired` 콜백·`IsComplete`는 착탄 전 거짓) 결과 같음 |
| 그리는 도중 무기 꺼짐 | 그리기 앞 확인 → 취소, 유닛 자세 뒤 확인 → 멈춤만 | 같은 두 자리, 같은 처리(MT3) | 같음 |
| 무기 꺼짐 콜백(`OnDisable`) | 콜백 → `HandleUnavailable` | 콜백 → 기반 `HandleUnavailable` | 같음. Tick 도중에 와도 주인 취소 → `Cancel`/`Release` 재진입이 같은 결과 |
| 정리 | 깃발 → 콜백 풀기·회수·무기 자세·유닛 자세, 예외 모아 던짐 | 같은 순서, 기반이 모아 던짐 | 같음(문구만 다름) |

### MT5 바꾸지 않는 것

- 곡사의 자세 곡선(준비·반동·복귀), 발사 높이·회전, 착탄 = 히트, 무기 표시 컴포넌트, 전략 `CombatMortarStyle`의 생성 줄(인자 그대로).

## 4. 바뀌는 동작

정상 재생에서 보이는 차이는 없다. 잘못된 값·오류 길에서만 다음이 바뀐다.

| 경우 | 지금 | 바뀐 뒤 |
|---|---|---|
| C1-42: 곡사 시작 때 유닛 위치가 유한하지 않음 | `ArgumentOutOfRangeException` | `InvalidOperationException`(직선·근접과 같음) |
| C1-42: 곡사 정리 단계가 실패 | `[CombatMortarAnimation] 공격 자세·발사체 정리에 실패했습니다.` | `[CombatAttackMotion] 발사체·유닛 자세 정리에 실패했습니다.`(같은 예외 목록) |
| C1-42: 곡사 동작 실패 뒤 정리도 실패 | 문구 있는 `AggregateException(문구, 원인, 정리)` | 기반의 `AggregateException(원인, 정리)` |
| C1-42: 무기 `Validate` 실패 | 동작 안에서는 정리 없이 던지고, 받는 `CombatAttackPresenter.Begin`(37-40·143-156)이 `Cancel`을 불러 정리 전체가 돈다 | 동작 안에서 정리한 뒤 던진다. 받는 쪽 `Cancel`은 이미 정리돼 아무것도 안 한다. 보이는 결과는 같다 |
| C1-42: 생성 인자 검사 순서 | 곡사 본문 순서(설정 → 매퍼 → 풀 → 무기 → 유닛 → 발사 콜백 → 종류 → 목표) | 기반 호출식 인자가 왼쪽부터 먼저(매퍼 → 설정·종류·끝 시각), 기반 본문(유닛 → 목표 → 위쪽 → 길이), 곡사 본문(풀 → 무기 → 발사 콜백)이 뒤. 여러 인자가 함께 잘못됐을 때 먼저 나는 예외가 다를 수 있다(2026-11-12 계획 TP-16 정정 — 1판의 "설정·종류·끝 시각 → 매퍼"는 틀림) |
| C1-42: 곡사 시작 때 유닛을 쓸 수 없음 | `[CombatMortarAnimation] 현재 대여의 살아 있는 유닛 표시가 필요합니다.` | `[CombatAttackMotion] 현재 활성 대여의 유닛 자세가 필요합니다.`(예외 종류는 같은 `InvalidOperationException`, 2026-11-12 SM-b에서 찾아 더함) |
| C1-38: 설정 표 값을 바꿈 | 무시됨 | 그대로 반영됨 |

모두 F1 오류 경계로 가는 길이라 화면 결과는 같다(오류 정지).

## 5. 영향 범위

### 5.1 바꾸는 파일

| 묶음 | 파일 | 작성자 |
|---|---|---|
| C1-38 코드 | `DV/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`(칸 하나 더함, 기본값·툴팁·검사) | 우리(소리 창구 2차) |
| | `V/Playback/Support/CombatSoundPresets.cs`(`Fall`·`WeakGroundEntry` 더함, 문서) | 우리 + 현강님(`Fall` 지운 줄) |
| | `V/Playback/Support/CombatEventSounds.cs`(갈래 셋, 도우미, `EffectKeepingPreset`, 문서) | 현강님 줄 + 우리 |
| C1-38 에셋 | `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset` | 코드와 **한 커밋**. 코드만 들어가면 에셋의 붕괴 칸 -1이 그대로라 붕괴 소리가 사라진다. 진입 목록은 YAML에 칸이 없어도 Unity가 필드 초기값 `[1, 2]`를 남길 가능성이 높지만, 에셋에 값을 적어 두는 것이 확실하다 |
| C1-42 코드 | `V/Presentation/Attack/CombatAttackMotion.cs`, `CombatMortarAnimation.cs`, `CombatLinearAttackAnimation.cs`(한 줄) | 모두 우리(용근) |

### 5.2 바꾸지 않는 것

- Factory, ServiceKeys, Core, Buffer, Framework, GlobalSystem(AudioSystem 포함), Boot 씬, `CatusCannonWeaponView`, `CombatMortarStyle`.

### 5.3 시험

- `Assets/Tests`에서 바뀌는 형식·함수를 부르는 곳이 없다(`CombatMortarAnimation`·`CombatAttackMotion`·`ReleaseResources`·`CombatEventSounds`·설정 칸 이름으로 grep 확인). 새 시험은 쓰지 않는다.

### 5.4 공유

- 현강님: C1-38 갈래 셋이 현강님 줄이다(11-10·11-11에 연이어 고친 자리이고, `c0f92a5d8`은 `presets.Fall()`과 설정 표 경로를 일부러 걷은 커밋이다). 현강님 구조 공유 메모(`handoffs/2026-10-22-combat-view-structure-hyeongang-share.md`) 새 절에 바뀐 줄을 적는다.
  - §23 첫 질문("C1-38 어느 쪽으로")은 사용자가 설정 표로 정했다고 적는다.
  - 추락 소리 대상이 유닛만인지(Q-SP2)는 현강님 확인 항목으로 적는다.
  - 11-12 원격 확인: 현강님 브랜치 `feature/La/AudioSystem_R`(`5be9bba85`)은 이 파일들에서 HEAD와 차이가 없다. `feature/La/SoundSystem`은 옛 브랜치라 전투 폴더 자체가 없다. 착수 때 다시 본다(SM-0).
- 호진님: C1-42는 우리 줄이라 공유가 필요 없다. C1-38은 설정 표 칸을 더하므로 호진님 설정 표 메모에 새 절을 쓴다(SP4).
- 호진님·천명님 정정: 11-05 질문 둘이 틀린 작성자로 적혀 있다. 그 둘에 정정 표시를 단다(C2-04 정정과 같은 꼴).
  - 호진님 구조 공유 §17의 C1-42 질문
  - 천명님 특성 공유 §7의 C1-42 질문
- 민형님·Core 전달 문서: 해당 없음.

## 6. 7기준 점검

12기준 전체 점검과 바꾸는 파일 안의 다른 기준 문제는 Task 계획 §2에 있다.

| 기준 | 지금 | 바꾼 뒤 |
|---|---|---|
| SSOT | 소리 번호 원본이 코드·표 둘(표는 죽은 칸), 수명 규칙이 기반·곡사 둘, 정리 예외 모으기가 공용 규칙 밖에 둘 | 소리 번호는 설정 표 하나, 남은 횟수 → 번호 규칙은 `WeakGroundEntry` 하나, "-1 = 프리셋 없이 냄"은 `EffectKeepingPreset` 하나, 수명은 기반 하나, 정리 예외는 공용 `CleanupErrors` |
| SOLID | 곡사가 수명·자세·무기·탄을 함께 맡음 | 곡사는 자세·무기·탄만. 수명은 기반(개방·폐쇄: 새 공격 종류는 훅만 채움) |
| FW 계약 | 해당 없음 | 해당 없음 |
| 독립성 | 소리 바꾸려면 코드 수정 | 에셋만 고침 |
| 불필요 반복 | 깃발 여섯 사본, `TryCleanup` 사본 | 걷음 |
| C#/Unity | 기반 생성자 예외 인자 이름 틀림 | 고침 |
| 매 프레임 | 변화 없음(진입 프리셋 찾기는 목록 색인 한 번, 할당 없음) | 같음 |

예상 점수(같은 감점표에서 C1 중간 둘만 뺌, 낮음은 상한 그대로):
- C1: 100 − 2 × 7.5 − 10 = 75
- 요청 7기준: 82.5 + 15 / 7 ≈ 84.6
- 12기준: 85.0 + 15 / 12 ≈ 86.3

## 7. 정할 것

결정(2026-11-12 사용자): 모두 **A**.

| id | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-SP1 | 진입 소리를 표에 어떤 모양으로 둘까 | **A** 남은 횟수별 목록 `weakGroundEntrySoundPresets`(기본 `[1, 2]`, 넘으면 마지막 값) | B 칸 둘(마지막 진입·그 밖) / C 코드 규칙 "번호 = 남은 횟수"를 두고 최대값만 표에 |
| Q-SP2 | 추락 소리 대상 | **A** 지금처럼 유닛만(현강님 11-10 변경), 툴팁을 유닛으로 고침 | B 구조물도 다시 냄(소리 창구 2차 L2 결정 A로 되돌림) |
| Q-SP3 | 추락 칸 -1의 뜻 | **A** "-1 = 프리셋 없이 냄"(장애물과 같은 규칙, 함수 `EffectKeepingPreset` 하나, 툴팁·머리말로 다른 칸과 나눔). 소리를 끌 수는 없다 | B 켜기 칸(`playFallSound`, 기본 켬)을 따로 두고 `fallSoundPreset`은 "-1 = 프리셋 없이 냄"만. 끌 수 있지만 칸이 둘 / C "-1 = 소리 없음" 유지하고 기본값 0 — Boot `Fall` 칸에 프리셋이 없어 사운드 작업자가 프리셋을 더하기 전에는 재생 때 예외 |
| Q-SP4 | 사건 순간 표의 빠진 `Fall` 갈래 | **A** 주석으로 이유만(이번 범위 밖) | B 추락 갈래를 순간 표로 옮김(캐릭터 찾기 인자가 필요해 표 모양이 바뀜) |
| Q-MT1 | 곡사를 기반 위로 올리는 방식 | **A** 기반에 훅·확인 넷(§3 MT1)을 더하고 곡사가 상속 | B 수명만 별도 객체로 빼고 셋이 그것을 가짐(구성) — 바꾸는 범위가 직선·근접까지 커짐 |
| Q-MT2 | §4 오류 길 차이(예외 종류·문구) | **A** 받아들임(기반 쪽으로 맞춤) | B 곡사 문구를 남기는 훅을 더 둠 |
| Q-MT3 | 기반 생성자 예외 인자 이름 고치기 | **A** 이번에 함께(같은 파일) | B 그대로 |

## 8. Phase(안)

| Phase | 내용 | 커밋 |
|---|---|---|
| SM-0 | 착수 전 확인: 원격(브랜치·develop·현강님 오디오 브랜치)에 이 파일들의 새 변경이 없는지, 줄 번호 다시 대조 | 없음 |
| SM-a | C1-38: 설정 칸·검사·기본값·툴팁·머리말, 프리셋 답 둘(`Fall`·`WeakGroundEntry`, 목록은 생성자에서 한 번 복사), 소리 표 갈래 셋·`EffectKeepingPreset`, `None` 문서, 에셋 두 줄 | 코드 + 에셋 한 커밋 |
| SM-b | C1-42: 기반 훅·확인 넷·생성자 인자 이름·`Cleanup`을 공용 규칙으로, 곡사 다시 씀(`SumSeconds`·`EndTimeOf`), 직선 한 줄 | 한 커밋 |
| SM-c | 문서(사운드 작업자 안내·이펙트·소리 창구 안내·설계 구현 메모), 공유(현강님 새 절·호진님 설정 표 메모 새 절·정정 둘), 묶음 끝 검토, 평가 아티팩트 15차, CLAUDE.md | 문서 한 커밋 |

Unity 확인은 모든 Phase 뒤 한 번이다.
- 약한 지반: 진입 소리, 붕괴 소리
- 추락: 유닛, 구조물
- 곡사 공격: 보통 재생, 공격 중 유닛·무기 끄기(취소), 결과 취소
- 직선 공격: 회귀 확인

## 9. 독립 검토 반영 (2026-11-12)

새 에이전트 하나가 설계와 코드를 대조했다(읽기 전용). 1판 약 7.5, 반영 뒤 약 9로 보았다. 사실 대조는 대부분 맞았고, 지적은 모두 반영했다.

| id | 등급 | 지적 | 반영 |
|---|---|---|---|
| M1 | 중간 | 기반에 더할 `Duration`이 곡사의 정적 `Duration(double)`과 겹쳐 컴파일 오류 | 길이 속성을 빼고 곡사 `Render`는 `Pose(Elapsed)`, 정적 도우미는 `SumSeconds`(MT1·MT2·MT3) |
| M2 | 중간 | 옛 취소 확인 자리 넷이 명세에 없음. 시작 `Advance(0,0)`이 취소 뒤에도 불림. 유닛 자세 뒤 확인은 옛 코드가 취소하지 않았음 | `Advance` 첫 줄·`Fire` 셋 자리·`Pose` 두 자리를 옛 자리 그대로 적음, 기반 `Begin`은 `if (IsRunning) Advance(0,0)`(MT1·MT3·MT4) |
| M3 | 중간 | 정리 예외 모으기가 공용 `CleanupErrors` 밖에 새로 생김(SSOT) | 훅을 `ReleaseResources(ref List<Exception>)`로, 기반 `Cleanup`도 공용 규칙으로, 새 도우미 없음(MT1) |
| M4 | 중간 | 같은 머리말 아래 -1 뜻이 둘, "칸 기본 소리"는 칸에 프리셋을 적용한 적이 없을 때만 참, 0을 넣으면 재생 때 오류 | 함수 이름 `EffectKeepingPreset`, 툴팁에 한계, `None` 문서, 머리말 나눔, Q-SP3 B를 켜기 칸 안으로 다시 씀(SP3·§1.1) |
| L1 | 낮음 | 진입 목록을 사건마다 읽으면 할당 | 프리셋 객체 생성자에서 한 번 `int[]`로 복사(SP2) |
| L2 | 낮음 | 남은 횟수 0 이하의 뜻이 없음 | "0 이하 = 진입 소리 없음"과 툴팁 문구를 명세(SP2) |
| L3 | 낮음 | 생성 인자 검사 순서가 바뀜, 설정·매퍼 null을 기반 호출식에서 걸러야 함 | `EndTimeOf(settings)`를 기반 호출식에서, §4에 한 줄(MT2·§4) |
| L4 | 낮음 | 파괴된 무기의 Unity null 확인이 빠질 위험 | `CanUseWeapon()` 식을 그대로 적음(MT3) |
| L5 | 낮음 | 기반 `CancelUnavailable`이 주인 쪽 같은 이름과 겹침 | 기반 훅 이름 `HandleUnavailable`(MT1) |
| L6 | 낮음 | 이 칸들을 적은 문서 둘·호진님 설정 표 메모 관례 누락 | 문서 셋과 호진님 새 절을 SM-c에(SP4·§5.4) |
| L7 | 낮음 | 현강님이 같은 줄을 연이어 고침 | 원격 확인 결과를 §5.4에, 착수 전 확인 SM-0(§8) |
| L8 | 낮음 | 발사 순간 0이면 시작 때 같은 자세를 두 번 그림 | MT4 표에 적음 |

사실 정정도 반영했다: 프리셋 2가 나는 조건(§1.1), 커밋 날짜 10-13(§1.2), `Validate` 실패의 지금 동작(§4), 코드만 커밋했을 때 진입 목록(§5.1).

## 10. 구현 메모 (2026-11-12, SM-a·SM-b·SM-c)

계획 §10.2(SM-a)·§10.3(SM-b)·§10.4(SM-c)가 원본 기록이다. 여기는 설계와 달라진 곳만 적는다. 모두 컴파일 미확인, 새 시험 없음, 시험 변경 없음.

- **SP1~SP3 그대로.** 진입 목록은 Data가 복사본으로만 내준다(계획 TP-2, `CopyWeakGroundEntrySoundPresets`). 값 대조에서 다른 경우는 "남은 0 + 붕괴 예정 아님" 하나인데, Core `StaticTerrainFact`에서 그 조합은 꺼진(`Active` false) 약한 지반뿐이라 닿지 않는다.
- **SP4 더한 것:** 수 규칙 `CombatPresentationValueRules` 클래스 요약의 "-1 = 소리 없음" 문구도 고쳤다(같은 규칙을 적은 옛 주석). 사운드 작업자 안내 §2.2 표는 10-29 기준이라 순번이 바뀐 점을 덧붙임으로 적었다(이번 둘은 SFX 5 `WeakTerrian`·Unit 12 `Fall`), 표 전체는 다시 쓰지 않았다.
- **MT3 바뀜:** 곡사 `Pose`의 유닛 자세 뒤 확인은 `!IsRunning || !Unit.IsAvailable || !CanUseWeapon()`이다. MT3의 "`!IsRunning || !CanUseWeapon()`"은 옛 215줄(`!CanRender()`, 유닛 확인 포함)과 달라, MT4 "같음"에 맞게 유닛 확인을 넣었다.
- **MT1·MT3 바뀜(계획 TP-11):** `Advance` 진입 검사(`!IsRunning`)는 기반 한 곳이다. 세 `Advance` 첫 줄에서 걷었다.
- **MT2 바뀜(계획 TP-13):** 곡사에 끝 시각 필드가 없다. 끝 시각은 기반 길이 하나이고, 발사 순간 식은 정적 `FireAtOf` 하나다.
- **§4 더한 것:** 곡사 시작 때 유닛을 쓸 수 없으면 문구가 기반 문구로 바뀐다(표에 한 줄). 생성 인자 검사 순서 줄은 계획 TP-16대로 고쳤다.
- **묶음 끝 검토(SM-c):** 높음·중간 0, 낮음 8, 약 8.8(계획 §10.4). 그중 L1: SP2의 "전투마다 한 번 복사"는 실제로 View 서비스 수명에 한 번이다(프리셋 답을 `CombatViewSystem.InitData`가 만듦). 재생 중에 에셋의 진입 목록을 바꾸면 다음 재생부터 반영된다. 붕괴·추락 칸은 사건마다 읽어 바로 반영된다. 문구는 11-13 사용자 "권장대로 반영해"로 고쳤다(L1·L2·L3·L5 반영, 계획 §10.5).
