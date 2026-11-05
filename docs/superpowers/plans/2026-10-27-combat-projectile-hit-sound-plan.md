# 전투 투사체·피격 소리 Task 계획

> 날짜: 2026-10-27
> 설계: [projectile-hit-sound-design](../specs/2026-10-27-combat-projectile-hit-sound-design.md)(2판, PS1~PS7, 결정 §4)
> 상태: PS-a·PS-b 코드 완료(Unity 확인 문제 없음 — 사용자), PS-c 문서·검토 반영(§8.3). 기록은 §8.
>
> 지켜야 할 것(상시 지시)
> - 전투 View 작업이므로 MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다.
> - 기존 시험은 컴파일이 깨지는 곳만 최소로 맞춘다. 다른 사람이 쓴 시험은 고치기 전에 묻는다.
> - 검토는 묶음 끝에 한 번만 한다.
> - 커밋은 사용자가 한다.
> - AudioSystem(GlobalSystem)·Boot 플레이어 설정은 고치지 않는다.

## 0. 결정과 착수 조건

### 0.1 결정 (2026-10-27 사용자)

| 질문 | 결정 |
|---|---|
| Q-PS1 | 비행 소리는 발사 때 한 번. 투사체 SO의 지연 값으로만 조절 |
| Q-PS2 | 피격 소리 프리셋은 반응 묶음(`CombatUnitReactionPresentationSO`)에서 설정(일단) |
| Q-PS3 | 피격 소리 칸은 이펙트와 같은 세팅(Anchor·Delay·Point + 프리셋) |
| Q-PS4 | 착탄 이펙트는 그대로 공격 묶음 큐 |
| Q-PS5 | PS-a부터 |

### 0.2 착수 조건

- **PS-a:** 없음. 지금 착수할 수 있다.
- **PS-b:** ~~AudioSystem 슬롯이 생긴 뒤~~ → **슬롯이 없어도 연결 작업을 미리 다 한다**(2026-10-27 사용자 "PS-b에서는 슬롯이 없더라도 미리 연결작업만 다 해줘", 아래 P9).
  - 슬롯 자리는 소리 표 한 곳의 "연결 대기" 칸이다. 그동안은 프리셋이 있어도 소리를 내지 않는다.
  - 슬롯이 생기면 그 칸 셋만 채운다.
  - 요청 메모는 [audio-request](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md)(미전달)다.
- 같은 파일을 다른 작업이 고치고 있지 않은지 착수 때 `git status`로 본다. 범용 연출 구조 G-a~G-d가 커밋 전이고, 같은 `DV/`·큐 붙이기 파일을 이미 바꿨다. 그 묶음을 먼저 커밋하는 것을 권한다.

### 0.3 계획에서 정한 것 (사용자 확인 대상)

- **P1 피격 소리는 목록이다.** 이펙트처럼 0개 이상이고, 비면 소리가 없다. 결정 "이펙트와 같은 세팅"을 목록으로 읽었다.
- **P2 피격 소리 항목의 프리셋은 0 이상이다.** -1 항목은 막는다. 끄려면 항목을 지운다.
- **P3 자리(Point)는 지금 소리 위치에 쓰이지 않는다.** SFX 슬롯이 위치 없이 낸다. 칸과 검사(피격 칸은 SubjectBody만)는 이펙트와 같고, 툴팁에 "3D 소리를 붙일 때 씀"을 적는다.
- ~~**P4 착탄 소리는 지연 없이 착탄(Hit) 순간이다.**~~ → P10으로 바뀜.
- **P5 투사체 소리는 큐 붙이기가 낸다.** 비행 = Cue + 지연, 착탄 = Hit + 착탄 지연(P10). 공격 재생·전략 맥락 계약은 바꾸지 않는다(설계 PS3).
- **P6 피격 칸 시점 Cue는 피격 클립이 있을 때만이다.** 이펙트 큐와 같은 유닛 조건 `FindUnitError`를 소리 목록에도 적용한다.
- **P7 공격·사망 칸은 지금의 `sound`(시점만)를 그대로 쓴다.** 소리 목록은 피격 칸에서만 받는다.
- **P8 새 형식·인자는 끝 선택 인자다.** 기본값은 "소리 없음"이라 지금 생성 호출과 시험은 그대로 둔다.
- **P9 (2026-10-27 사용자 지시) 슬롯 없이 미리 연결한다.**
  - 소리 표 `CombatEventSounds`에 슬롯 칸 셋을 둔다: `SfxEvent?` 착탄·비행·피격, 지금은 null = 연결 대기.
  - 슬롯 함수 셋은 칸이 null이면 소리 없음(`CombatSoundCue.None`)을 돌려준다. 그래서 프리셋을 넣어도 슬롯이 생기기 전에는 소리가 나지 않고, 예외도 없다.
  - 슬롯이 생기면 그 칸 셋만 `SfxEvent.X`로 채운다. 그 줄에 "AudioSystem 슬롯 대기" 주석을 단다.
  - `SfxEvent`에 없는 값을 정수로 바꿔 쓰지 않는다. 숫자를 미리 짐작하면 Boot 플레이어 슬롯 순서와 어긋날 수 있다.
- **P10 (2026-10-27 사용자 "조절할 수 있게 열어주삼") 착탄 시점은 지연으로 조절한다.** 투사체 SO에 `impactSoundDelay`를 두고, 착탄 = Hit + 지연이다.
  - 확인한 사실: 투사체 공격에서 접촉(Contact)과 맞음(Hit)은 **같은 호출**이다. 코드 모션 직선·곡사, 클립 직선·곡사 모두 `ImpactThen`이 접촉 이펙트를 낸 바로 다음에 맞음 신호를 낸다. 곡사(Cannon)의 Hit도 착지 순간이다.
  - 그래서 Hit/Contact를 고르는 칸은 화면·소리에 차이가 없어 두지 않았다. 관통 같은 여러 번 접촉이 생기면 그때 시점 칸을 연다.

## 1. Phase PS-0 — 요청 전달 (사용자)

- **P0.1** [audio-request](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md)를 AudioSystem 담당께 전한다. 슬롯 이름·번호와 프리셋 목록 답을 받는다.

## 2. Phase PS-a — 값 (지금, 화면·소리 변화 없음)

| Task | 파일(`DV/` = `DataBaseSystem/Data/View/CombatPresentation/`) | 할 일 |
|---|---|---|
| Pa.1 | `DV/CombatProjectileSO.cs` | 칸 셋 `impactSoundPreset`·`flightSoundPreset`(`[SerializeField, Min(-1)] int = -1`)·`flightSoundDelay`(`Min(0f)`, 기본 0), 속성 셋, 툴팁(비행 = 발사 + 지연, 착탄 = 맞는 순간). 수 조건은 값 생성자가 본다(범용 구조 P1). |
| Pa.2 | `DV/CombatAttackPresentationData.cs` | 생성자 끝 선택 인자 셋(-1·-1·0)과 속성 `ImpactSoundPreset`·`FlightSoundPreset`·`FlightSoundDelay`. 검사는 `ValidateSoundPreset` 둘·`RequireNonNegative`. |
| Pa.3 | `DV/CombatAttackPresentationSO.cs` | `CreateData`가 투사체 부품에서 셋을 읽어 넘긴다(근접은 기본값). |
| Pa.4 | `DV/CombatCueEntries.cs` | 입력 칸 `CombatPresetSoundCueEntry`(preset·anchor·delay·point, public 기본 생성자 + internal 값 생성자, `ToCue()`)와 불변 값 `CombatPresetSoundCue`(수 조건은 값 생성자: 프리셋 0 이상·지연 0 이상·열거 정의). |
| Pa.5 | `DV/CombatEventCues.cs` | 묶음에 `Sounds`(끝 선택 인자, 기본 빈 목록). 피격 칸 입력 `CombatHitCueInput`에 `sounds` 목록 칸·`ToCues` 반영. |
| Pa.6 | `DV/CombatPresentationCueRules.cs` | `FindError`에 소리 목록 검사: 피격 칸에서만 받음(P7), 시점 `AllowsAnchor`, 자리 `AllowsPoint`(P3, 피격 = SubjectBody). `FindUnitError`의 "피격 Cue = 피격 클립"에 소리 목록도(P6). |
| Pa.7 | `DataBaseSO/ViewGroup/CombatPresentation/HitSetting/CombatUnitReaction_Default.asset` | `hitCues`에 `sounds: []`(명시, 줄 끝 LF 유지). 투사체 부품 넷은 칸을 비워 둔다(-1·-1·0이 필드 기본값). 프리셋 번호는 AudioSystem 답을 받은 뒤 사용자·기획이 넣는다. |
| Pa.8 | 시험 | 바꿀 곳이 없을 것으로 본다(P8). 착수 때 `new CombatEventCues(`·`new CombatAttackPresentationData(`·반응 값 생성 호출을 다시 `grep`한다. 목록을 받는 반응 SO 입력은 `SetReaction`이 다루지 않는 새 칸이라 도우미는 그대로 둔다. |

**확인(사용자, Unity):** 컴파일, 부트 DB 생성(반응·공격 묶음·투사체 부품 검사 통과), 인스펙터에 새 칸이 보이는지(투사체 셋, 반응 피격 칸 소리 목록). 소리는 아직 나지 않는다.

## 3. Phase PS-b — 재생 (슬롯 없이 미리 연결, P9)

| Task | 파일(`C/` = `ViewSystem/Combat/`) | 할 일 |
|---|---|---|
| Pb.0 | — | 착수 전에 시험 영향을 다시 센다. 기본값이 "소리 없음"이면 항목·소리 수는 그대로다. 실제 DB 값을 쓰는 장면 시험이 있으면, PS-a 뒤 기획이 넣은 프리셋 때문에 소리 수를 세는 시험이 바뀔 수 있다. 후보: `CombatEnemyPresentationTests`·`CombatInteractionTests`·`CombatSceneEventPresenterTests`(소리 수를 거르지 않고 셈). 줄마다 blame으로 우리 줄인지 본다. 우리 줄은 슬롯으로 거른 수로 맞추고(확인하는 사실은 그대로), 다른 사람 줄은 묻는다. |
| Pb.1 | `C/Playback/Support/CombatEventSounds.cs` | 슬롯 칸 셋 `SfxEvent?`(지금 null, P9)과 슬롯 함수 셋 `ProjectileImpact(preset)`·`ProjectileFlight(preset)`·`UnitHit(preset)`. 칸이 있으면 `CombatSoundCue.Effect(슬롯, preset)`, 없으면 `None`. `For(e)`는 그대로다(설계 PS2). |
| Pb.2 | `C/Playback/Support/CombatEventAudio.cs` | 단서 하나를 내는 입구 `Play(CombatSoundCue cue)`(옛 `Emit` 공개). |
| Pb.3 | `C/Presentation/Pieces/CombatEventSoundPiece.cs` | 사건 대신 단서를 받는 생성자 하나를 더한다(사건 소리는 지금처럼 `For(e)`). 시간 없는 조각 그대로. |
| Pb.4 | `C/Presentation/Pieces/CombatCueComposer.cs` | ① 소리 목록: 항목마다 `TryStart(anchor, delay)` → `UnitHit(preset)` 조각. 자리는 읽지 않는다(P3). ② 투사체 소리: 창구 묶음이 받은 투사체 소리 값이 있으면 비행 = Cue + 비행 지연, 착탄 = Hit + 착탄 지연(P5·P10). 프리셋이 -1이거나 슬롯이 연결 대기면(P9) 항목을 넣지 않는다. 붙이는 순서는 소리 → 플래시 → 흔들림 → 이펙트를 유지하고, 소리 안에서는 시점 소리 → 목록 → 투사체. |
| Pb.5 | `C/Presentation/Pieces/CombatCueComposer.cs`(창구 묶음) | `CombatCueSinks`에 끝 선택 인자로 투사체 소리 값(착탄·비행 프리셋·지연 둘). 공격 처리기·클립 원거리 조립·코드 모션 공격 조립이 공격 값에서 넘긴다. |
| Pb.6 | `C/Playback/Handlers/CombatCharacterEventHandler.cs` | `ComposeHit`에 끝 선택 인자 `sound`(소리 창구). `TryBeginHit`이 `_sound`를 넘긴다. 창구가 없으면 붙이기가 소리 항목을 넣지 않는다(지금 동작). |
| Pb.7 | 클립 공격·코드 모션 공격 조립 | `ComposeRanged`·`CombatCodeMotionAttack.Compose`가 투사체 소리 값을 창구 묶음에 담는다. 근접(`ComposeMelee`)은 넘기지 않는다. |

**확인(사용자, Unity):** 컴파일. 프리셋을 넣은 투사체로 직선·곡사 공격(코드 모션 유닛과 클립 원거리 유닛 각 하나)을 해 보고, 발사 + 지연에 비행 소리, 착탄 순간에 착탄 소리가 나는지 본다. 피격 소리 항목을 넣은 반응 묶음으로 맞을 때 소리가 나는지, 시점 Start·End·지연이 맞는지 본다. 지연을 길게 주면 다음 행동이 그만큼 기다리는 것도 확인한다(§6 위험 1).

## 4. Phase PS-c — 문서·공유

- **Pc.1** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) §2 연출 덧붙임 규칙에 한 줄: "투사체 소리 프리셋은 투사체 부품, 피격 소리는 반응 묶음 소리 목록, 시점은 기준 시점 + 칸".
- **Pc.2** 설계 구현 메모(계획과 다른 점).
- **Pc.3** 현강님 사건 소리 메모 §7(소리 표 함수 셋·소리 창구 입구·소리 조각, 미전달). 우리 소리 코드는 현강님 코드를 옮긴 것이다.
- **Pc.4** AudioSystem 담당께 연결 끝 알림(필요하면).
- **Pc.5** 묶음 끝 검토 하나(7기준). 반영한 뒤 다시 돌리지 않는다.
- **Pc.6** CLAUDE.md 현재 상태·메모리.

## 5. 시험 영향

- PS-a: 없다(P8). 새 형식·인자가 모두 끝 선택 인자다.
- PS-b: 기본값이면 없다. 프리셋이 들어간 실제 DB를 쓰는 장면 시험은 Pb.0에서 다시 센다.
- 새 시험은 없다.

## 6. 위험

1. **긴 소리 지연이 다음 행동·사건을 늦춘다.**
   - 소리 항목도 실행의 항목이라 전체 종료가 그만큼 늦어진다.
   - 비행·착탄 지연은 공격 행동을 늦춘다. 피격 소리 지연은 맞은 유닛의 다음 사건을 늦춘다(유닛 실행은 끝까지 주체를 막음, 묶음 끝 검토 낮음 5).
   - 투사체·피격 소리 지연 툴팁에 적었다. 막지는 않는다(값 검사는 0 이상만).
2. **슬롯 없는 재생 = 예외.**
   - 소리 표의 슬롯 칸이 비어 있는 동안(P9)은 재생하지 않아 안전하다.
   - 칸을 채운 뒤 Boot 플레이어에 그 슬롯이 없으면, 프리셋이 0 이상인 첫 재생에서 전투가 멈춘다. 칸은 AudioSystem에 슬롯이 들어간 판에서만 채운다.
3. **반응 묶음이 하나뿐이다.** 피격 소리 변종을 유닛마다 다르게 하려면 반응 묶음을 나눠야 한다(결정의 "일단").
4. **자리 칸이 지금은 효과가 없다**(P3). 기획자가 자리를 바꿔도 소리는 같다. 툴팁에 적는다.

## 7. 커밋 묶음

- **PS-a:** `DV/` 다섯 파일과 반응 기본 에셋은 한 커밋이다(새 칸이 에셋에 없어도 기본값으로 읽히지만, 명시한 `sounds: []`와 코드는 함께 둔다). 범용 연출 구조 묶음이 커밋 전이면 그 뒤에 커밋한다.
- **PS-b:** `C/` 파일들과 문서.
  - 슬롯 칸이 비어 있어 AudioSystem 변경 없이 커밋할 수 있다.
  - 슬롯 칸을 채우는 세 줄 변경은 AudioSystem의 `SfxEvent` 값이 들어간 판 위에서 한다.

## 8. 실행 기록

### 8.1 PS-a 코드·에셋 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

사용자 "PS-b에서는 슬롯이 없더라도 미리 연결작업만 다 해줘 / 착탄의 경우 총알은 Hit, Cannon은 Contact? 조절할 수 있게 열어주삼 / PS-a 시작해".

- **Pa.1** `CombatProjectileSO`: 머리말 "소리"와 칸 넷.
  - `impactSoundPreset`(-1)·`impactSoundDelay`(0, P10)·`flightSoundPreset`(-1)·`flightSoundDelay`(0).
  - 툴팁(맞는 순간 = 곡사 착지·직선 몸 접촉, 지연이 길면 다음 행동이 기다림)과 속성 넷.
  - 계획 때는 칸 셋이었다. `impactSoundDelay`는 P10으로 더했다.
- **Pa.2** `CombatAttackPresentationData`: 생성자 끝 선택 인자 넷(-1·0·-1·0)과 속성 넷.
  - 프리셋 검사는 유닛 외형과 같은 `CombatPresentationSettingsData.ValidateSoundPreset`, 지연은 `RequireNonNegative`.
  - 설정 Item 네임스페이스 using을 하나 더했다(형식 이름 겹침 없음 확인).
- **Pa.3** `CombatAttackPresentationSO.CreateData`: 투사체 부품에서 넷을 읽는다. 근접(부품 없음)은 -1·0이다.
- **Pa.4** `CombatCueEntries`: 입력 칸 `CombatPresetSoundCueEntry`(preset `Min(0)`·anchor Start·delay·point SubjectBody, 자리 툴팁에 "지금 소리에 쓰이지 않음")와 불변 값 `CombatPresetSoundCue`(프리셋 0 이상·열거 정의·지연 0 이상, P2).
- **Pa.5** `CombatEventCues`: 생성자 끝 선택 인자 `sounds`·속성 `Sounds`(기본 빈 목록)·`Read` 오버로드. 피격 칸 입력에 `sounds` 목록(툴팁)·`ToCues` 반영.
- **Pa.6** `CombatPresentationCueRules`
  - `FindError`: 소리 목록은 피격 칸만(P7). 항목마다 시점 `AllowsAnchor`·자리 `AllowsPoint`, 이펙트와 같은 표(피격 칸: 시점 Start·Cue·End, 자리 SubjectBody).
  - `UsesAnchor`: 소리 목록을 더했다. 그래서 피격 Cue 소리는 피격 클립이 있어야 한다(P6).
- **반응 SO 툴팁:** 피격 칸 설명에 "피격 소리(프리셋·시점·지연·자리)"를 더했다.
- **Pa.7** `CombatUnitReaction_Default.asset` `hitCues`에 `sounds: []`. 이 에셋은 지금 CRLF라 Edit로 넣었다(줄 끝 섞이지 않음).
  - 투사체 부품 넷은 손대지 않았다. 새 칸은 필드 기본값(-1·0·-1·0)으로 읽힌다.
- **Pa.8** 시험: 바꾼 곳 없음.
  - 새 인자는 모두 끝 선택 인자다.
  - 피격 칸 큐를 만드는 시험 도우미 `HitCues()`의 4인자 `new(…)`는 그대로 컴파일된다.
  - 반응 SO 리플렉션 도우미는 기존 칸만 다룬다.
- **화면·소리 변화 없음.** 아무도 새 값을 읽지 않는다(PS-b).
- **커밋:** PS-a가 고친 `DV/` 파일은 범용 연출 구조 묶음(커밋 전)의 새 파일과 겹친다(`CombatCueEntries`·`CombatEventCues`·`CombatPresentationCueRules`·`CombatProjectileSO`). 그 묶음과 함께 커밋하거나 그 묶음 커밋 뒤에 한다(나눠 스테이지하기 어려움).
- **사용자 확인(Unity):** 컴파일, 부트 DB 생성(검사 통과), 인스펙터에서 투사체 부품 "소리" 칸 넷과 반응 묶음 피격 칸 `Sounds` 목록이 보이는지. 목록에 항목을 넣어 시점 Cue(피격 클립 없는 유닛) 또는 자리 ActorBody를 고르면 부트 DB 생성이 이유와 함께 멈춰야 한다. 확인한 뒤에는 그 항목을 지운다.

### 8.2 PS-b 코드 완료 — 슬롯 없이 미리 연결 (2026-10-27, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

사용자 "PS-a Unity 확인 문제 없음. PS-b 진행해". 앞서 받은 지시 "슬롯이 없더라도 미리 연결작업만 다 해줘"(P9)대로 했다.

- **Pb.0 시험 영향:** 없다.
  - 슬롯 칸 셋이 연결 대기(null)라 슬롯 함수 셋이 늘 소리 없음을 돌려준다. 붙이기는 소리 없음 단서로 항목을 만들지 않는다.
  - 그래서 프리셋 값과 상관없이 조립 항목 수·소리 수가 그대로다.
  - 슬롯을 채운 뒤에는 프리셋이 0 이상인 투사체·반응 묶음을 쓰는 장면 시험의 소리 수가 바뀔 수 있다. 그때 다시 센다(후보는 Pb.0 표).
- **Pb.1** `Playback/Support/CombatEventSounds.cs`
  - 슬롯 칸 셋 `ProjectileImpactSlot`·`ProjectileFlightSlot`·`UnitHitSlot`(`SfxEvent?` = null). 줄마다 "AudioSystem 슬롯 대기 → SfxEvent.X(요청 안 이름)" 주석을 달았고, 숫자 짐작 금지도 적었다.
  - 슬롯 함수 셋 `ProjectileImpact`·`ProjectileFlight`·`UnitHit`(preset)과 공통 `Slotted`.
- **Pb.2** `CombatEventAudio.Play(CombatSoundCue)`: 소리 표가 고른 단서 하나를 낸다(옛 `Emit`을 그대로 부름).
- **Pb.3** `CombatEventSoundPiece`: 단서를 받는 생성자를 더했다(소리 없음 단서는 예외). `Begin`은 사건이면 소리 표, 단서면 그 단서를 낸다.
- **Pb.4·Pb.5** `CombatCueComposer.cs`
  - 투사체 소리 값 `CombatProjectileSounds`(같은 파일, `From(공격 값)` — 두 프리셋이 모두 -1이면 null).
  - 창구 묶음 끝 선택 인자 `projectileSounds`.
  - 붙이기: 소리 창구가 있을 때 ① 피격 소리 목록 항목마다 `UnitHit(preset)`을 시점 + 지연에, ② 투사체 비행 = Cue + 비행 지연·착탄 = Hit + 착탄 지연. 공통 `AddSound`(소리 없음이면 항목 없음).
  - 순서는 소리 시점 → 목록 → 투사체(문서 머리에 적음).
- **Pb.6** `CombatCharacterEventHandler.ComposeHit`: 끝 선택 인자 `sound`. 창구 묶음에 소리 창구를 넘긴다(소리 사건은 null, 피격 칸에는 소리 시점이 없음). `TryBeginHit`이 `_sound`를 넘긴다.
- **Pb.7**
  - `CombatClipAttack.ComposeRanged`: 끝 선택 인자 `projectileSounds` → `AddCues` → 창구 묶음. 근접은 null.
  - `CombatCodeMotionAttack.Compose`: 끝 선택 인자 `projectileSounds`.
  - `CombatAttackEventHandler`: 두 길(코드 모션·클립 원거리)이 `CombatProjectileSounds.From(settings)`를 넘긴다. 근접은 프리셋이 -1이라 null이다.
- **계획과 다른 점:** 없다. 투사체 소리 값의 형식은 계획에 이름이 없었다. 큐 붙이기와 같은 파일(`CombatCueSinks` 옆)에 두어 새 파일·`.meta`가 없다.
- **Factory·사건 Presenter 생성 줄:** 바꾸지 않았다. 피격 처리기·공격 처리기는 이미 소리 창구를 가졌다.
- **슬롯이 생긴 뒤 할 일(한 곳):** `CombatEventSounds`의 슬롯 칸 셋을 `SfxEvent.X`로 채운다. 이어서 투사체 부품·반응 묶음에 프리셋을 넣고 계획 §3의 재생 확인을 한다.
- **사용자 확인(Unity, 지금):** 컴파일만 보면 된다. 소리·화면 변화는 없어야 한다(슬롯 연결 대기). 기존 EditMode 시험 결과도 그대로여야 한다.
- 다음은 PS-c다: 폴더 배치 안내 줄, 설계 구현 메모, 현강님 소리 메모 §7, 묶음 끝 검토 하나.
- **컴파일 오류 고침(2026-10-27, 사용자 보고 CS0104):**
  - 원인: `CombatCueComposer.cs`의 `AddSound` 인자 `CombatSoundCue`가 두 형식을 가리켰다. 하나는 DB의 소리 시점 값(`DataBaseSystem.Data.View.CombatPresentation.CombatSoundCue`, 범용 구조 G-d)이고, 다른 하나는 소리 표의 단서(`Playback.Support.CombatSoundCue`)다. 두 네임스페이스를 함께 들이는 파일은 11개(운영 5·시험 6)다. 그 가운데 이 이름을 맨 이름으로 쓰는 곳이 이 파일 하나였다. 처음 적은 "이 파일만 들인다"는 틀렸다(묶음 끝 검토가 바로잡음).
  - 고침: 인자 형식을 `Playback.Support.CombatSoundCue`로 가렸다. 상대 이름 `Playback`은 `Combat.Playback` 네임스페이스 하나뿐임을 확인했다.
  - 다음 후보: 같은 이름의 형식 둘(뜻도 다름: 시점 값 / 무엇을 낼지 단서)은 이름을 나누는 것이 맞다(예: DB 쪽 `CombatSoundTimingCue`). 묶음 끝 검토(PS-c)에 올린다.

### 8.3 PS-c 문서·공유·묶음 끝 검토 (2026-10-27, 코드는 주석·한 갈래만, 컴파일 미확인)

사용자 "PS-b Unity 확인 문제 없음. PS-c 진행해".

- **Pc.1** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md): 머리 갱신 줄을 고치고, §2 연출 덧붙임 줄에 소리 주인 규칙을 더했다. 규칙은 공격·사망 = 유닛 외형 프리셋 + 칸 시점, 투사체 = 부품 프리셋·지연 + Hit·Cue 고정, 피격 = 반응 묶음 소리 목록, 슬롯 대응 = 소리 표 한 곳이다. 새 파일은 없다.
- **Pc.2** 설계 §6 구현 메모.
- **Pc.3** 현강님 [사건 소리 메모](../handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md) §7(미전달).
- **Pc.4** [AudioSystem 요청 메모](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md) §3: "우리 쪽 연결·값 칸은 이미 끝남, 슬롯이 생기면 칸 셋을 채움"으로 고쳤다(미전달).
- **Pc.5 묶음 끝 검토(하나, 반영 뒤 다시 돌리지 않음):** 높음 0·중간 2·낮음 9. 7기준 예상은 SSOT 7.5·SOLID 7.5·FW 9·독립성 8.5·캡슐화 7.5·C#/Unity 7.5·If/Tick 8, **약 7.9**다(설계 예상 8.3).
  - 확인된 것(문제 없음)
    - CS0104가 다시 날 자리가 없다.
    - 오버로드(`Play` 둘·소리 조각 생성자·`Read`)가 모호하지 않다.
    - 시험 호출의 뜻이 바뀌지 않는다.
    - 슬롯이 null이면 항목·오디오 호출·예외가 모두 0이다.
    - 투사체 소리를 넣는 모든 길에 Cue·Hit 시점이 이어져 있다.
    - 피격 Cue 규칙이 외형 표 검사 길에서 실제로 돈다.
  - **반영(이번):**
    - 낮음 2: `CombatProjectileSounds.From`의 null 반환을 걷었다. 소리를 낼지는 `AddSound` 한 곳이 거른다.
    - 낮음 5: 피격 소리 지연 툴팁과 위험 1에 "다음 사건이 늦어짐"을 더했다.
    - 낮음 6: 설계 §6에 "치명타에는 피격 소리 없음(사망 조립이 맡음)"을 적었다.
    - 낮음 8: 낡은 주석 셋 — 공격 값 머리(소리 프리셋은 -1), 지연 param 둘, "재생 연결은 PS-b" 둘.
    - 문서 불일치 다섯: 계획 머리·P5·§3 제목·위험 2·§7, 설계 머리·PS5. §8.2의 "이 파일만 두 네임스페이스를 들인다"는 틀려서(11개가 들이고, 맨 이름을 쓰는 곳이 이 파일 하나) 바로잡았다.
  - **사용자 결정 대기(묶음 R):**
    - 중간 1: 이름이 같은 `CombatSoundCue` 둘(DB 소리 시점 값 / 소리 표 단서)과 `Sound`/`Sounds` 혼동. 두 네임스페이스를 함께 들이는 파일이 11개라 맨 이름을 쓰면 CS0104가 다시 난다. 권장은 커밋 전인 DB 쪽을 `CombatSoundTimingCue`(+ `…Entry`)로, `CombatEventCues.Sounds`를 `PresetSounds`로 바꾸는 것이다(반응 에셋 키 `sounds` 한 곳 포함). 그 뒤 큐 붙이기의 네임스페이스 가림을 걷는다.
    - 중간 2: "-1 = 없음 / 0 이상 = 소리" 규칙이 네 곳에 있다(설정 표 `ValidateSoundPreset`, `CombatPresetSoundCue`의 `preset < 0`, 런타임 `CombatSoundPresets.None`·`IsSound`, 속성 `Min`). 권장은 수 규칙 원본 `CombatPresentationValueRules`에 소리 프리셋 판정·검사를 두고 모두 그것을 부르는 것이다. 그러면 공격 값이 설정 표 네임스페이스에 기대지 않는다.
    - 낮음 1: `CombatProjectileSounds`를 `Playback/Support/` 새 파일로 옮긴다(폴더 안내 "설정에서 꺼낸 연출 값 묶음 → Support"). M3 때 DB 값 묶음으로 바꾸며 걷는다.
    - 낮음 3: 소리 조각의 두 모드(사건·단서)를 조각 둘로 나누거나 `Func<CombatSoundCue>` 하나로 만든다.
    - 낮음 4: 소리 창구의 `Emit`을 `Play(cue)`로 합친다.
    - 반영하면 약 8.4로 본다.
  - **그대로 둠:** 낮음 7(자리 칸이 지금 죽은 데이터 — 결정대로 두고 툴팁에 적었다), 낮음 9(평평한 생성자 인자 26개 — 알려진 M3 후속).
- **Pc.6** CLAUDE.md·메모리.

### 8.4 묶음 R 반영 (2026-10-27, 사용자 "묶음 R(중간 1·2, 낮음 1·3·4) 권장대로 반영해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **중간 1 이름 나눔**
  - DB `CombatSoundCue` → `CombatSoundTimingCue`, `CombatSoundCueEntry` → `CombatSoundTimingCueEntry`(`CombatCueEntries.cs`·`CombatEventCues.cs`, 다른 파일에서 쓰는 곳 없음).
  - `CombatEventCues.Sounds` → `PresetSounds`(생성자 인자 `presetSounds`·빈 목록 `NoPresetSounds`). 피격 칸 입력 칸 `sounds` → `presetSounds`, 반응 기본 에셋 키 `presetSounds: []`(이 키를 가진 에셋은 이것 하나).
  - 큐 규칙·큐 붙이기의 `cues.Sounds` → `cues.PresetSounds`. 큐 붙이기 `AddSound`의 `Playback.Support.` 가림과 그 주석을 걷었다.
  - 직렬화 필드 형식 이름만 바뀐 칸(공격·사망 `sound`)은 Unity가 칸 이름으로 읽어 에셋 영향이 없다.
- **중간 2 소리 프리셋 규칙 하나:** `CombatPresentationValueRules`에 `NoSoundPreset`(-1)·`IsSoundPreset`·`RequireSoundPreset(preset, name, allowNone)`(문구는 옛 둘과 같음).
  - 설정 표 `ValidateSoundPreset`은 이것을 부르는 한 줄로 남겼다(유닛 외형 SO가 부르므로 그 파일은 바꾸지 않음).
  - 공격 값은 `RequireSoundPreset(…, allowNone: true)`이고 설정 Item using을 걷었다. 프리셋 소리 큐는 `allowNone: false`다.
  - 런타임 `CombatSoundPresets.None` = `NoSoundPreset`, `IsSound` = `IsSoundPreset`. DV using을 더했다(형식 이름 겹침 없음 확인).
- **낮음 1:** `CombatProjectileSounds`를 `Playback/Support/CombatProjectileSounds.cs`(+ `.meta`, 새 GUID)로 옮겼다. 쓰는 넷(큐 붙이기·클립 공격·코드 모션 공격·공격 처리기)은 이미 Support를 들인다.
- **낮음 3:** 소리 조각 생성자를 `(CombatEventAudio, Func<CombatSoundCue>)` 하나로 바꿨다.
  - 사건 소리는 `() => sound.CueOf(e)`로 시작 순간에 고른다(사망 조립·큐 붙이기 소리 시점).
  - 투사체·피격 소리는 `() => cue`다. 쓰지 않게 된 Contracts.Combat using을 걷었다.
- **낮음 4:** 소리 창구의 `Emit`을 `Play(CombatSoundCue)`로 합쳤다. `CueOf(e)`를 더했고, `Play(e)`는 `Play(CueOf(e))`, 걸음 소리도 `Play`다.
- **문서:** 폴더 배치 안내(머리·Support 줄에 `CombatProjectileSounds`), 설계 §6(묶음 R)·§2 이름, 현강님 메모 §7(소리 창구·조각), CLAUDE.md.
- **동작:** 같다. 소리를 고르는 순간·내는 순간·슬롯 연결 대기가 그대로다.
- **시험:** `HEAD`의 시험에는 바뀐 이름을 쓰는 곳이 없다(`git grep`). 작업 트리의 `Assets/Tests`는 지금 984개 파일이 삭제된 채 스테이지돼 있다. 이번 작업이 한 것이 아니다. 사용자가 의도한 삭제로 확인했다(2026-10-27 사용자 "그대로 두고 기록만"). 되돌리지 않는다.
- **커밋:** 새 `Playback/Support/CombatProjectileSounds.cs` + `.meta`를 함께 넣는다.
