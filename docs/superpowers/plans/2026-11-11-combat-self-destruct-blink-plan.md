# 전투 자폭 깜빡임 Task 계획

작성: 2026-11-11 · 설계: [self-destruct-blink-design](../specs/2026-11-11-combat-self-destruct-blink-design.md) · 상태: **SD-a~SD-c·검토 반영·깜빡임 모양 칸 완료, Uc 재생 확인 문제 없음(2026-11-11 사용자, §6.7). 새 시험 없음, 시험 변경 없음. 커밋 전.**

## 0. 결정·착수 조건

- 설계 결정: Q-SD1~Q-SD5 모두 A(2026-11-11 사용자 "권장대로, 계획 써줘").
- 다른 쪽에서 만드는 자폭 이펙트는 SD8대로 **발동 줄 큐 항목 하나**로 꽂는다. 이번에는 임시 `Boom`(TP-8).
- 전투 View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 컴파일·재생 확인은 Phase마다 사용자가 Unity에서 한다.
- 바뀌는 서명을 쓰는 기존 시험은 없다(`Assets/Tests` grep 0).
- Q-SD5 A: 확인할 적에 자폭 특성을 붙이는 것은 사용자다(SD-c 확인 전).

### 계획에서 정한 것(사용자 확인 대상)

| # | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 깜빡임 입력은 위치 교환 칸과 같은 꼴: `enabled` + 색(빨강)·횟수(3)·전체 시간(0.9초). 끄면 값 `null`. | 선례(`CombatPositionSwapVisualInput`) |
| TP-2 | `CombatFlashPiece`의 횟수 인자는 **필수**로 하고 피격 플래시 호출 한 곳이 `1`을 넘긴다(기본값 없음). | 호환용 기본값보다 바른 서명(메모리 규칙) |
| TP-3 | `CombatCuePoints.CreatePiece`의 기본 갈래(`_ =>` = 칸)를 `EventCell`·`ActivationCell` 명시 갈래 + 정의 밖 예외로 바꾼다. | 새 자리가 조용히 칸 목록으로 가지 않게 |
| TP-4 | 자폭 사망 조립의 맞은 방향 기본 회전 = 보인 사실의 서 있는 방향(`StandingPoseOf`). 자세를 빌리지 않는다. | 깜빡임은 자세를 안 바꿈. 아이템 발동 큐와 같은 기준 |
| TP-5 | **바뀜(2026-11-11 사용자 "늘 몸 남김"):** 갈래 순서 = 낙사 → **터질 유닛(결과에 390 있음)** → 반응 설정 없음 → 일반. 터질 유닛은 깜빡임을 쓸 수 없어도(줄에서 꺼짐·색 층 없음·반응 설정 없음) 사망 반응 없이 몸만 남겼다가 폭발 순간에 내린다. 옛 안: 깜빡임을 쓸 수 없으면 지금 사망 반응. | 폭발 이펙트가 몸이 부서지는 모습이라 죽은 모습을 따로 보이면 안 됨(사용자) |
| TP-6 | 한 결과에 같은 출처의 390이 둘이면 계약 오류로 던진다. | Core는 유닛마다 한 번(`handled`) |
| TP-7 | 남은 몸 유지는 결과 취소(`CancelMovementCore`, 대여를 되돌린 뒤·유닛 다시 그리기 전)와 결과 끝(`Refresh`, 최종 사실 반영 전) 두 곳에서 모두 놓는다. | F10 "끝·취소 두 길" |
| TP-8 | **바뀜(2026-11-11):** 자폭 390 줄 큐 = `SelfDestruction`(18) @ `ActivationCell`·Start·회전 보정 0·**끝 Seconds 5초**. 흔들림은 넣지 않는다. 옛 안: 임시 `Boom`·(−90,0,0)·끝 자연. | 민형님 자폭 이펙트(`5e6980555`)가 들어옴. 그 프리팹의 Smoke·Fire가 반복이라 스스로 안 끝남, 다른 파티클 길이 5초 |
| TP-9 | 발동 줄 인스펙터 그리기(`TraitActivationVisualDrawer`)의 칸 목록에 `deathBlink`를 더한다. | 칸 목록을 직접 그려 안 넣으면 안 보임 |

## 1. 바꿀 파일

| 파일 | Phase | 내용 |
|---|---|---|
| 새 `DataBaseSystem/Data/View/CombatPresentation/CombatDeathBlinkVisualInput.cs` | a | 입력(TP-1) |
| 새 `DataBaseSystem/Data/View/CombatPresentation/CombatDeathBlinkVisualData.cs` | a | 값·검사 |
| `DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs` | a | 칸 `deathBlink`·`DeathBlink`·`AllowsDeathBlink`·검사 |
| `DataBaseSystem/Editor/CombatBodyTextDrawer.cs` | a | 칸 목록 한 줄(TP-9) |
| `DataBaseSystem/Data/View/CombatPresentation/CombatCuePoint.cs` | a | `ActivationCell = 7` |
| `DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs` | a | 특성 발동 칸에 새 자리 허용 |
| `ViewSystem/Combat/Presentation/Pieces/CombatCuePoints.cs` | a | 새 자리 해석(TP-3) |
| `ViewSystem/Combat/Presentation/Pieces/CombatFlashPiece.cs` | a | 횟수(TP-2) |
| `ViewSystem/Combat/Presentation/Pieces/CombatCueComposer.cs` | a | 호출 `1` |
| `ViewSystem/Combat/Playback/Support/CombatTraitFeedbackPlayback.cs` | a | `ForActivation` 직접 호출에 새 인자(null) |
| `ViewSystem/Combat/Playback/Support/CombatActivationRows.cs` | b | 줄 값에 `DeathBlink` |
| 새 `ViewSystem/Combat/Playback/Support/CombatSelfDestructDeaths.cs` | b | 자폭 예정·몸 유지(SD1·SD5) |
| `ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs` | b | 생성·결과 수명·처리기에 넘김 |
| `ViewSystem/Combat/Playback/Handlers/CombatCharacterEventHandler.cs` | b | 자폭 사망 갈래·조립 |
| `ViewSystem/Combat/Playback/Handlers/CombatTraitEventHandler.cs` | b | 390 시작 때 놓기 |
| `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_SelfDestruct.asset` | c | 390 줄(TP-8) |

새 `.cs` 셋의 `.meta`는 Unity가 만든다. Factory·Core·GlobalSystem 무변경.

## 2. Phase SD-a 값 (화면 변화 없음)

- **Ta.1** 깜빡임 입력·값.
  - 입력: `enabled`, `color`(기본 빨강), `pulses`(기본 3, `Min(1)`), `seconds`(기본 0.9, `Min(0)`). `ToData()` = 꺼짐이면 `null`.
  - 값: 생성자에서 횟수 ≥ 1, 시간은 `CombatPresentationValueRules.RequirePositive`. 알파는 쓰지 않는다(플래시와 같음).
- **Ta.2** `TraitActivationVisual`
  - 칸 `deathBlink`(입력), 속성 `DeathBlink`(읽을 때마다 새 값).
  - 규칙 원본 `internal static bool AllowsDeathBlink(TraitKey owner, TraitActivationKind kind)` = `SelfDestruct` · `SelfDestructExploded`.
  - `Validate`: 값이 있는데 허용 줄이 아니면 예외(문구에 특성·종류). 값 검사 예외는 위치 교환처럼 문맥을 붙여 감싼다.
- **Ta.3** 발동 줄 그리기 칸 목록에 `"deathBlink"`(TP-9).
- **Ta.4** 큐 자리 `CombatCuePoint.ActivationCell = 7`(요약: 특성 발동의 선택 칸 `TargetCell`, 칸 표면 가운데·필드 방향, 특성 발동 칸만).
  - `AllowsPoint`: `TraitActivation` 갈래에 더한다. 다른 칸은 그대로 거절.
  - `StateApplied` 줄은 지금 검사(몸 자리만)가 그대로 막는다.
- **Ta.5** `CombatCuePoints`
  - 필드 `_activationCell`(`Pose?`), private 생성자 인자 하나.
  - `ForActivation(trait, bodyOf, mapper)`: `trait.TargetCell`이 있으면 `CellPose`로 채움.
  - `ForActivation(activator, body, subjects, cells, activationCell)`: 새 인자 필수, 호출 한 곳(`CombatTraitFeedbackPlayback.cs:116`)은 `null`.
  - `CountOf`: `ActivationCell => _activationCell.HasValue ? 1 : 0`.
  - `CreatePiece`: 명시 갈래 셋 + 정의 밖 예외(TP-3).
- **Ta.6** `CombatFlashPiece(unit, color, duration, pulses)`
  - 한 번 길이 = `duration / pulses`, 세기 = `1 − 그 안의 진행률`, 끝이면 0. `pulses == 1`이면 지금 식과 같다.
  - 호출 `CombatCueComposer.cs:112`에 `pulses: 1`.

**Ua (사용자):** 컴파일, DB 생성(부트) 정상, 피격 플래시가 지금과 같음, 특성 외형 표 인스펙터에서 발동 줄에 "Death Blink" 칸이 보임.

## 3. Phase SD-b 재생

- **Tb.1** `CombatActivationRow`에 `DeathBlink`(`CombatDeathBlinkVisualData`) 속성·생성자 인자, `CombatActivationRows.Of`가 `activation.DeathBlink`를 넘김.
- **Tb.2** 새 `CombatSelfDestructDeaths`(Playback/Support)
  - 생성자: `CombatActivationRows`.
  - `Begin(CombatResult)`: 먼저 `ReleaseAll()`, 그다음 사건을 훑어 `CombatEffectsEvent`의 발동 중 `SelfDestructExploded`를 출처 → (발동, 줄의 `DeathBlink`)로 모음. 같은 출처 둘이면 예외(TP-6).
  - `TryGetBlink(EntityId, out CombatDeathBlinkVisualData)`: 그 출처가 있고 값이 있을 때만 true.
  - `Keep(EntityId, ICombatUnitDeathHold)`: 보관(이미 있으면 예외).
  - `Detonate(TraitActivationSnapshot)`: 390이고 보관한 유지가 있으면 놓고 지움. 아니면 아무것도 안 함.
  - `ReleaseAll()`: 남은 유지를 모두 놓고 비움(놓기 실패는 모아 던짐 — `CleanupErrors` 꼴).
- **Tb.3** Presenter
  - 생성자: `activationRows` 다음에 `_selfDestruct = new CombatSelfDestructDeaths(activationRows)`, 사망 처리기·특성 처리기 생성 인자로 넘김.
  - `BeginPresentation`: `_selfDestruct.Begin(result)`(`ForgetFalls` 옆).
  - `CancelMovementCore`: `_players.CancelAll` 뒤·`RenderUnits` 앞에 `CleanupErrors.Try(_selfDestruct.ReleaseAll, ref errors)`(TP-7).
  - `Refresh`: `_facts.Replace` 앞에 `_selfDestruct.ReleaseAll()`(TP-7).
- **Tb.4** `CombatCharacterEventHandler`
  - 생성자 끝에 필수 인자 `CombatSelfDestructDeaths selfDestruct`.
  - `TryBeginDeath`: 반응 설정 확인 뒤 `_selfDestruct.TryGetBlink(id, out blink) && _presentation.CanFlash(id)`면 `BeginSelfDestructDeath`(TP-5).
  - `BeginSelfDestructDeath`: `HoldDeath` → `ComposeSelfDestructDeath` → `_pieces.TryBegin`(못 하면 예외, 일반 사망과 같음) → 세대가 같으면 `_selfDestruct.Keep(id, hold)` → `ApplyCharacter`·`Render`. 실패하면 유지를 놓고 다시 던짐. 세대가 바뀌었으면 유지를 바로 놓는다.
  - `SubjectBodyOf`를 (id, 사건, 기본 회전)으로 바꾸고, 일반 사망·피격은 `pose.Rotation`, 자폭은 `StandingPoseOf` 회전을 넘김(TP-4).
  - 정적 `ComposeSelfDestructDeath(subject, blink, sound, death, cues, camera, effects, subjectBody)`: `ForUnit(subject, isDeath: true, General).Lease(subject, Flash)` → 0 깜빡임(시작, 기다림) → 사망 칸 큐(End = 깜빡임 끝).
- **Tb.5** `CombatTraitEventHandler`
  - 생성자에 필수 인자 `CombatSelfDestructDeaths selfDestruct`.
  - `TryBeginBeforeFacts` 발동 반복 맨 앞에서 `_selfDestruct.Detonate(trait)` — 큐보다 먼저라 몸이 사라지는 프레임에 폭발이 난다(SD5).

**Ub (사용자):** 컴파일. 자폭 유닛이 없고 390 줄도 비어 있어 화면 변화 없음. 일반 사망·피격·포효가 지금과 같음.

## 4. Phase SD-c 에셋·문서

- **Tc.1** `TraitVisual_SelfDestruct.asset`에 390 줄(TP-8): `kind: 390`, `cueTiming: 0`, `deathBlink: {enabled: 1, 빨강, 3, 0.9}`, 효과 하나(`effect: 2`, `anchor: 1`, `point: 7`, `rotationOffset: {x: -90}`, `end: 0`). 형식은 `TraitVisual_Roar.asset` 줄을 따른다.
- **Tc.2** (사용자, Q-SD5 A) 확인할 적에 자폭 특성(`SelfDestructTraitAuthoring`, 레벨)을 붙인다. Claude는 어디에 붙이는지 안내만 한다.
- **Tc.3** 문서: 설계 §6 구현 메모, 폴더 배치 안내(`Playback/Support/CombatSelfDestructDeaths`), CLAUDE.md 현재 상태, 평가 아티팩트 적용 기록 한 차수(점수 변화 없음, 평가 목록 밖 — 첫 동작 FU 11차와 같은 꼴).
- **Tc.4** 공유(채팅 붙여 넣기 글, 파일 없음): 호진님 — 발동 줄 새 칸 `deathBlink`, 큐 자리 `ActivationCell`, `TraitVisual_SelfDestruct` 390 줄. 이펙트 제작자·민형님 — SD8 꽂는 법(`EffectType` 맨 끝에 붙이기, 표 항목 등록, 390 줄 종류 값 하나 바꾸기).
- **Tc.5** 묶음 끝 검토(새 에이전트 하나, 7기준 + 바뀐 곳의 영향).

**Uc (사용자, 자폭 적이 있는 스테이지):**
1. 자폭 적을 공격으로 처치 → 맞은 순간 사망 소리·피격 이펙트 → 붉게 3번 깜빡 → 몸이 사라지는 순간 가운데 `Boom` → 주변 유닛 피격 반응이 같은 순간 시작.
2. 주변에 아무도 없을 때도 깜빡임·폭발이 난다.
3. 한 공격에 자폭 적 둘 처치 → 둘이 함께 깜빡이고, 하나씩 터지며 각자 자기 폭발 순간에 사라진다.
4. 폭발이 다른 자폭 적을 죽이면 "깜빡 → 펑 → 깜빡 → 펑".
5. 낭떠러지로 밀어 떨어뜨리면 추락 연출만 나고 깜빡임·폭발이 없다(Core가 390을 안 냄).
6. 처치로 전투가 끝나면(승패 확정) 일반 사망이다.
7. 다른 유닛의 피격 플래시는 지금과 같다(한 번).
8. 알려진 한계: 넉백으로 처치하면 몸은 원래 칸에서 깜빡이고 폭발은 옮겨진 칸에서 난다(설계 SD7).

## 5. 커밋 묶음(사용자)

- SD-a: 새 `.cs` 둘(+ `.meta`)과 바꾼 값·규칙·그리기·조각 파일을 한 커밋(새 큐 자리 값을 규칙 없이 쓰면 DB 생성이 멈춤).
- SD-b: 새 `.cs` 하나(+ `.meta`)와 처리기·Presenter를 한 커밋.
- SD-c: `TraitVisual_SelfDestruct.asset`은 SD-a 코드가 들어간 뒤(없으면 칸·자리 값을 읽지 못해 DB 생성이 멈춤).

## 6. 실행 기록

### 6.1 SD-a 값 (2026-11-11, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 새 `CombatDeathBlinkVisualInput`·`CombatDeathBlinkVisualData`(`DataBaseSystem/Data/View/CombatPresentation/`, `.meta`는 Unity가 만듦): `enabled`·빨강·3번·0.9초, 값 생성자가 횟수 ≥ 1·시간 양수를 검사.
- `TraitActivationVisual`: 칸 `deathBlink`, `DeathBlink`, 규칙 원본 `AllowsDeathBlink`, `ValidateDeathBlink`(값 예외는 문맥을 붙여 감쌈, 허용 줄 밖이면 막음), 큐 툴팁·요약에 선택 칸.
- `TraitActivationVisualDrawer` 칸 목록에 `deathBlink`(TP-9).
- `CombatCuePoint.ActivationCell = 7`, `AllowsPoint`의 특성 발동 갈래, 큐 칸 요약·Rider 보정 툴팁 문구.
- `CombatCuePoints`: 선택 칸과 Mapper를 받아 두고, 그 자리를 고른 큐가 있을 때만 `CellPose`로 바꿈(`CountOf`·`CreatePiece`). `CreatePiece` 기본 갈래 → 명시 갈래 + 정의 밖 예외(TP-3).
- `CombatFlashPiece(unit, color, duration, pulses)` 필수 인자(TP-2), 회차마다 1 → 0. 호출 `CombatCueComposer`에 `pulses: 1`.
- **계획과 다른 점:** Ta.5의 "명시 입구 `ForActivation(activator, body, subjects, cells)`에 새 인자"는 하지 않았다. 그 입구를 쓰는 곳은 상태 반영 시점(StateApplied) 줄 하나이고 그 줄은 몸 자리만 받으므로(검사가 막음), 선택 칸은 늘 비어 있는 것이 맞다. 그래서 `CombatTraitFeedbackPlayback.cs`는 바꾸지 않았다(입구 요약에 한 줄).
- 선택 칸을 미리 칸 자리로 바꾸지 않은 이유: 다른 발동이 선택 칸을 실어도(작렬 등) 그 자리를 고른 큐가 없으면 Mapper·필드 밖 검사로 멈추지 않게 했다. 고른 큐가 있으면 지금처럼 `CellPose`가 크게 멈춘다.
- 화면 변화 없음(깜빡임 칸·새 자리를 쓰는 에셋 없음, 피격 플래시는 횟수 1이라 같은 식).

**Ua — Claude가 Unity 배치 모드로 확인(2026-11-11 사용자 "테스트도 너가 해봐", 테스트 코드는 만들지 않음):** 에디터가 꺼져 있어 `Unity.exe -batchmode -runTests`(EditMode, `ProjectL.Tests.CoreIntegration`·`ProjectL.CombatField.Editor.Tests`, 필터 `DataBaseAssetTests|StructureHitPresentationTests`)로 돌렸다.
1. 컴파일: 성공(`ProjectL.Systems.dll`·`Assembly-CSharp-Editor.dll` 다시 빌드, 오류 0).
2. DB 생성: 실제 카탈로그로 전체 DB를 만드는 시험(`GrowthProduction_LevelJumpExcludesOwnershipAndKeepsUnchosenCandidates` 4건, `DataBaseFactory.Create`)이 통과 → 특성 외형 표 검사(새 깜빡임 칸·큐 자리 포함) 통과.
3. 큐 자리를 쓰는 기존 전투 시험 `StructureHitPresentationTests` 22건 모두 통과.
4. `DataBaseAssetTests` 실패 7건(성장 제안·스테이지 준비 특성·런 골드 시설)은 모두 InGame 데이터 단언이다. 예: "Stage data not found. StageKey: 1101"인데 DataBaseSO 어디에도 키 1101이 없다. 이번 변경은 InGame 데이터·코드를 건드리지 않아 무관으로 본다. 다만 HEAD 기준 판 실행은 못 했다(변경을 잠시 치우는 `git stash`가 권한에서 거절됨).
5. 피격 플래시 같음: 횟수 1의 식이 옛 식과 같다(코드 대조). 화면으로는 확인하지 않았다.
6. 인스펙터 "Death Blink" 칸: 배치 모드(그래픽 없음)라 확인하지 않았다. 사용자 확인 필요.
- 부산물: 배치 실행이 `New Fmod Event Catalog.asset`(1줄)·`SCDream7 SDF.asset`(글꼴 아틀라스 −1532줄)을 다시 썼다. 우리 변경이 아니며, 사용자 결정으로 그대로 둔다(2026-11-11 "그대로 둠"). 커밋 때는 SD-a 묶음에 넣지 않는다.

### 6.2 SD-b 재생 (2026-11-11 사용자 "진행해줘", 새 시험 없음, 시험 변경 없음)

- `CombatActivationRow.DeathBlink`(생성자 끝 선택 인자), `CombatActivationRows.Of`가 줄의 깜빡임 값을 넘김. 특성 연출 확인 도구의 줄 생성(`PreviewTraitFeedbackValidation.cs:36`)은 인자 그대로(깜빡임 없음).
- 새 `Playback/Support/CombatSelfDestructDeaths.cs`(+ `.meta`, Unity가 만듦): `Begin`(남은 유지를 놓고 결과의 390 발동을 출처마다 모음, 같은 출처 둘이면 예외 — TP-6), `TryGetBlink`, `Keep`, `Detonate`, `ReleaseAll`(실패는 모아 던짐).
- Presenter: 줄 보관 다음에 생성, `BeginPresentation`에서 `Begin`, `CancelMovementCore`에서 `_players.CancelAll` 뒤·유닛 다시 그리기 전에 `ReleaseAll`, `Refresh`(결과 끝)에서 최종 사실 반영 전에 `ReleaseAll`(TP-7). 처리기 둘에 넘김. Factory 무변경.
- `CombatCharacterEventHandler`: 생성자 끝 필수 인자, `TryBeginDeath`가 낙사 → 반응 설정 → **자폭(`TryGetBlink` + `CanFlash`)** → 일반 순서(TP-5), `BeginSelfDestructDeath`(사망 유지 → 조립 시작, 못 하면 예외 → 세대가 같으면 `Keep` → 사망 사실 반영·그리기, 세대가 바뀌었거나 실패하면 유지를 바로 놓음), 정적 `ComposeSelfDestructDeath`(Flash 대여, 0 깜빡임 기다림 + 사망 칸 큐, 끝 = 깜빡임 끝), `SubjectBodyOf`가 기본 회전을 받음(반응은 빌린 자세 회전, 깜빡임 사망은 서 있는 몸 회전 — TP-4).
- `CombatTraitEventHandler`: 생성자 필수 인자(위치 교환 선택 인자 앞), 발동 반복 맨 앞에서 `Detonate` — 큐보다 먼저라 몸이 사라지는 프레임에 폭발.
- 계획과 다른 점 없음.
- **Unity 배치 확인(Claude):** 컴파일 성공(`ProjectL.Systems.dll` 다시 빌드, 오류 0). 같은 필터 101건 중 94 통과·7 실패, 실패 7은 SD-a 실행과 같은 InGame 데이터 시험(새 실패 없음). 이 시험들은 재생 코드를 지나지 않으므로 깜빡임·폭발 동작은 SD-c 에셋과 자폭 적이 생긴 뒤 화면으로 확인한다(Uc).
- 화면 변화 없음(390 줄이 비어 있고 자폭 적이 없음). 다른 사망·피격·포효는 그대로다(자폭 갈래는 결과에 390이 있고 줄에 깜빡임 값이 있을 때만).

### 6.3 SD-c 에셋·문서 (2026-11-11 사용자 "SD-c 진행")

- Tc.1 `TraitVisual_SelfDestruct.asset`에 390 줄(TP-8): 깜빡임 켬·빨강·3번·0.9초, 효과 `Boom`(2)·Start(1)·`ActivationCell`(7)·회전 보정 (−90,0,0)·끝 자연(0), 흔들림 없음. 에셋은 특성 외형 표에 이미 등록돼 있다(`TraitVisualDataTable.asset`).
- **Unity 배치 확인(Claude):** 새 줄이 들어간 채로 실제 카탈로그 전체 DB 생성 시험 4건 통과 → 깜빡임 허용 줄·`ActivationCell` 자리 검사 통과. 실패는 앞과 같은 InGame 데이터 7건뿐.
- Tc.2 (사용자, Q-SD5 A): 확인할 적 유닛 SO(EnemyOnly, `DataBaseSO/InGameGroup/Unit/Item/Enemy/`)의 **Traits** 목록에 `SelfDestructTraitAuthoring`(자폭, 단계 1~3 = 공격력의 50/75/110%)을 더하고, 그 적이 나오는 스테이지(적 로드아웃)에서 확인한다. 같은 꼴의 예: `Unit_UndeadHeavy.asset`의 재생 특성.
- Tc.3 문서: 설계 §6 구현 메모, 폴더 배치 안내(머리 갱신 줄 + §2 규칙 줄 "뒤에 올 특성 발동에 따라 앞 사건 연출을 바꿀 때"), CLAUDE.md.
  - **평가 아티팩트 적용 기록은 더하지 못했다:** 이 세션 계정에서 아티팩트가 열리지 않는다("artifact not found" — 다른 계정 소유이거나 공유되지 않음). 더할 내용은 "자폭 깜빡임 SD-a~SD-c, 점수 변화 없음(평가 목록 밖 새 기능)" 한 차수이며, 아티팩트를 열 수 있는 계정에서 더해야 한다.
- Tc.4 공유 글(채팅, 파일 없음): 호진님(발동 줄 칸·큐 자리·390 줄), 이펙트 제작자·민형님(SD8 꽂는 법).
- Tc.5 묶음 끝 검토: 새 에이전트 하나(아래 §6.4).

#### 6.3.1 자폭 이펙트 꽂기·늘 몸 남김 (2026-11-11 사용자 "SelfDestruction 이펙트 추가됐어" → 결정 "늘 몸 남김"·"Claude가 등록")

- 민형님 `5e6980555`는 프리팹(`Script/Render/Effect/BuffeEffect/SelfDestruction/SelfDestruction.prefab`)·재질·뼈 FBX(LFS)·시험 씬만 넣었고 Render DB 등록은 없었다.
- **Render DB 등록(Claude, 민형님 공유 대상):** `EffectType`에 `SelfDestruction = 18`을 맨 끝에 번호를 적어 붙임(주석: 중간에 끼우지 말 것), 새 표 항목 `RenderGroup/EffectTable/Items/SelfDestruction.asset`(+ `.meta`, 풀 0/16 — `BoomEffect`와 같음, 프리팹 루트 연결), `EffectDataTable.asset`에 한 줄.
- **390 줄:** 효과 `SelfDestruction`(18), 회전 보정 0(프리팹 방향 그대로 — 재생 확인 대상), 끝 Seconds 5초(TP-8 바뀜).
- **늘 몸 남김(TP-5 바뀜):** `CombatSelfDestructDeaths.IsExploding`, 창구 `CombatUnitPresentationWindow.CanHoldDeath`(켜진 표시 + 유지 중 아님), 사망 처리기는 낙사 다음에 터질 유닛이면 `BeginSelfDestructDeath` — 표시가 없으면 false, 깜빡임을 쓸 수 있으면 조립(반응 설정이 없으면 큐 대신 사건 소리 하나), 쓸 수 없으면 몸 유지만 맡기고 false(틀이 소리·순간 큐·사실 반영·완료). 깜빡임 없는 길에서는 사망 칸 큐(치명타 피격 이펙트·흔들림)가 나오지 않는다(받아들임).
- **Unity 배치 확인(Claude):** 컴파일 성공, 실제 카탈로그 전체 DB 생성 시험 4건 통과(Render 이펙트 표·390 줄 포함), 실패는 같은 InGame 데이터 7건뿐.

### 6.4 묶음 끝 검토 (새 에이전트 하나, §6.3.1 반영 판 기준)

종합 약 8.6(SSOT 8.5·SOLID 8.5·FW 9.5·재사용 9.5·독립성 8.0·불필요 9.0·정확성 7.5). 누수·이중 해제 없음, 390이 깜빡임보다 먼저 시작하지 않음, 취소 안 순서 맞음, `ActivationCell` 지연 해석 맞음, 플래시 식 횟수 1 = 옛 식, 빠진 호출처 없음(검토 확인).

| # | 심각도 | 내용 | 코드 확인 |
|---|---|---|---|
| F1 | 중간 | 사망 큐에 지연이 있으면(Death는 Start·End + 지연 허용) 일반 완료(깜빡임 끝) 뒤에도 실행이 남아 Flash 대여를 쥔다. (a) 390이 맞힌 대상이 하나면 390 주체가 그 이웃이라 막히지 않고, `Detonate` → 몸 반환 → 대여 취소 → `_unavailable` → 정지. (b) 대상 0·2개 이상이면 390은 막히지만 같은 묶음의 이웃 피격이 먼저 시작. 지금 기본 반응 에셋은 지연 0이라 안 남 | 맞음(`CombatPresentationCueRules.cs:90`, `CombatPieceRun.cs:181·386·458·500`) |
| F2 | 낮음 | 깜빡임 값은 Any 줄(`_rows.Of`), 폭발 큐는 방향까지 본 줄(`_feedback.RowOf`) — 방향별 390 줄이 생기면 어긋남 | 맞음 |
| F3 | 낮음 | 깜빡임 조립 시작 뒤 사실 반영이 던지면 유지만 놓고 실행은 취소하지 않음(어차피 정지 경로) | 맞음 |
| F4 | 낮음 | `Mathf.Repeat` 부동소수 경계에서 끝 직전 한 프레임 세기 1 가능 | 맞음(거의 안 닿음) |
| F5 | 낮음 | 취소 때 `_blinks`를 비우지 않음(다음 `Begin`이 비움, 낙사 표지와 다름) | 맞음 |
| F6 | 낮음 | `EffectType`이 `SelfDestruction = 18`만 번호 명시 — 누가 번호 없이 끼우면 같은 18이 되어 조용히 다른 이펙트 | 맞음(Render DB, 민형님 영역) |

### 6.5 검토 반영 (2026-11-11 사용자 "권장대로 반영")

- **F1:** 조립 완료 방식에 "모든 항목 완료" `CombatPieceCompletion.AllItems`(+ `WaitsForAllItems`)를 더함 — 기다림 여부와 상관없이 모든 항목이 끝나 대여를 되돌린 뒤 한 번 알림. `CombatPieceRun`의 일찍 알리는 두 곳(시작·항목 끝)이 이 방식이면 건너뛰고, 전체 종료 갈래가 알림. 자폭 사망 조립만 씀 → 사망 큐 지연이 있어도 사건은 실행 끝에 끝나 자폭 발동(몸 대여를 거둠)이 실행보다 먼저 시작하지 않음. 다른 조립은 그대로(일반 완료).
- **F2:** 코드 변경 없이 근거 주석만. 방향별 줄은 결속 방어력 줄에서만 만들 수 있어(`AllowsDirection`, DB 검사가 막음) 자폭 390 줄은 늘 Any 하나이고, 깜빡임 값과 폭발 큐가 같은 줄을 읽는다(검토가 말한 어긋남은 지금 규칙으로 생기지 않음).
- **F4:** 플래시 식을 회차 번호를 마지막 회차로 묶는 식으로(`index = Min((int)(t/len), pulses-1)`, 세기 = `1 − Clamp01((t − index·len)/len)`). 횟수 1이면 옛 식과 정확히 같다.
- **F5:** `CombatSelfDestructDeaths.Clear`(목록 비움 + 남은 유지 놓기)를 결과 시작·취소·끝이 부름. `ReleaseAll`은 안으로(private).
- **그대로:** F3(이미 정지 경로), F6(Render DB는 민형님 파일 — 공유 글에서 "모든 값에 번호를 적자" 제안).
- **Unity 배치 확인(Claude):** 컴파일 성공, 같은 필터 101건 중 94 통과·같은 InGame 데이터 7 실패(새 실패 없음).

### 6.6 깜빡임 모양 칸 (2026-11-11 사용자 "붉게 변하는 속도 못 바꿔?" → "칸 더해줘")

- 깜빡임 칸에 셋을 더함: **최대 세기** `peakStrength`(0.05~1, 기본 1 — 회차마다 가장 붉을 때), **점점 빨라지기** `lastPulseRatio`(0.1~1, 기본 1 — 끝 회차 길이 ÷ 첫 회차 길이, 전체 시간은 그대로), **회차 곡선** `pulseCurve`(가로 = 회차 안 시간 0~1, 세로 = 세기 0~1, 기본 1 → 0 직선). 설계 Q-SD3 A(같은 간격)는 기본값으로 남고, 가속은 값으로 고를 수 있게 됨.
- 새 DB 값 `CombatFlashPulseShape`(+ `.meta`): 투사체 완급 곡선과 같은 꼴 — 키 복사, 키 둘 이상·값 0~1 표본 검사, 기본 곡선은 직선(정확히 1 − u)으로 읽음. 깜빡임 값 생성자가 세기·비율 범위를 검사, 입력 `ToData`가 곡선 오류를 `ArgumentException`으로 던지고 발동 줄 검사가 문맥을 붙여 감쌈.
- 새 View 값 `Presentation/Pieces/CombatFlashPulses`(+ `.meta`): 회차 시간표(등비 나눔, 비율 1이면 같은 간격·F4 경계 처리)와 세기 계산의 한 곳, 호출마다 할당 없음. `CombatFlashPiece(unit, color, CombatFlashPulses)` — 피격 플래시는 `Single(seconds)`(한 회차·직선·최대 1 = 옛 값과 같음), 깜빡임은 `From(blink)`. 시간 검사는 시간표 생성자로 옮김.
- 에셋 390 줄에 새 칸 셋을 기본값으로 적음(화면 같음).
- 바꾸는 법: 특성 외형 표 → 자폭 → Activations → 390 줄 → Death Blink. 다음 전투 진입부터 보임.
- **Unity 배치 확인(Claude):** 컴파일 성공, 전체 DB 생성 시험 4건 통과, 같은 InGame 데이터 7 실패뿐.

### 6.7 재생 확인 (2026-11-11)

- **Uc 문제 없음(사용자).** 확인용으로 `Unit_Demon.asset`에 자폭 특성(단계 1)을 붙여 재생했다(남길지는 사용자 결정).
- 사용자가 에디터에서 깜빡임 값을 고침(에셋이 원본): 전체 시간 0.9 → **1.75초**, 끝 회차 길이 비율 1 → **0.15**(점점 빨라짐), 회차 곡선 = 키 셋(1 → 약 0.47 @ 0.41 → 0)의 부드러운 내림. 색·횟수·최대 세기는 그대로(빨강·3·1).
- 남은 것: 사용자 커밋(이번 작업 파일만 — FMOD 목록·캐시, 글꼴 아틀라스 둘, `StageTest.prefab`은 우리 변경 아님), 공유 글 전달(호진님·민형님), 평가 아티팩트 적용 기록(이 계정에서 열리지 않음).

### 6.8 커밋·develop 병합·push (2026-11-11 사용자 "커밋 푸시해주고, develop 꺼도 받고 싶어", 자동 생성 파일은 develop 따름, `StageTest.prefab`은 사용자 수정이라 함께)

- 커밋 `55021fc0f`(이번 작업 + `StageTest.prefab`). FMOD 목록·캐시, 글꼴 아틀라스 둘, Addressables 설정(줄 끝만)은 되돌려 develop을 따름. `Unit_Demon.asset`(확인용 자폭 특성)은 말씀이 없어 커밋하지 않고 로컬에 둠.
- `origin/develop` 병합 충돌 12개 정리. develop(용근님 작렬·재구성·넉백 피격·아킴보·히트 아웃라인)이 같은 기능을 따로 넣은 곳은 develop을 따름:
  - **큐 자리:** develop `SelectedCell = 7`(뜻·번호 같음)을 쓰고 제 `ActivationCell`은 걷음. 자리 해석 `CombatCuePoints`도 develop 판(선택 칸을 미리 칸 자리로 바꿈 — 제 지연 해석은 걷음). 코드 안내 문구 둘도 `SelectedCell`로. 이 계획·설계의 `ActivationCell` 글은 이력으로 남김.
  - **이펙트 번호:** develop `BoomSmall = 18`·`FallingRocksSmall = 19`와 겹쳐 `SelfDestruction = 20`으로 옮김(표 항목 `effectType: 20`, 390 줄 `effect: 20`). `EffectDataTable`은 셋 다 등록.
  - **플래시:** develop의 플래시 모습 `CombatFlashLook`(몸 색·외곽선)에 회차 시간표를 얹어 `CombatFlashPiece(unit, look, pulses)`. 피격 플래시 = 큐의 모습 + `Single`, 깜빡임 = 몸 색만(외곽선 폭 0) + `From(blink)`.
  - **사망 처리기:** develop의 넉백 피격 목록(`CombatKnockbackLeadIns`)과 자폭 목록을 둘 다 받음(생성자 끝 둘). develop도 `SubjectBodyOf`가 방향을 받게 바꿔 그대로 맞음. Presenter는 develop 조립 순서(캐릭터 처리기를 공격 시작보다 먼저)에 자폭 목록만 얹음, 결과 시작 때 둘 다 `Begin`.
  - **390 줄 에셋:** 사용자가 고친 값(깜빡임 1.75초·0.15·곡선, `SelfDestruction`·5초)을 남김.
  - CLAUDE.md·폴더 배치 안내는 양쪽 줄을 모두 살림.
- **Unity 배치 확인(Claude, 병합 트리):** 컴파일 성공, 전체 DB 생성 시험 통과, 같은 InGame 데이터 7 실패뿐. 병합 뒤 재생 확인은 아직(develop의 피격 플래시 외곽선과 깜빡임이 함께 도는지).
