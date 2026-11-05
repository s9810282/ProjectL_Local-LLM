# 전투 피격 이펙트 Task 계획 (맞은 유닛별)

- 작성일: 2026-10-20. 설계: [hit-effect-design](../specs/2026-10-20-combat-hit-effect-design.md)(HE1~HE7).
- 결정: **Q-HE1~Q-HE4 모두 A**(사용자 "맞은 유닛별 피격 이펙트로 설계 문서 써 줘. 정할 것 1~4는 모두 A로."), **설계에서 정한 것 P1~P4 확정**(사용자 "P1~P4 이대로 확정. Task 계획 써 줘.").
- Phase마다 멈추고 보고한다. 아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `DV/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/`, `T/` = `Assets/Tests/EditMode/Framework/`.
- 검증: MSBuild 컴파일(바꾼 파일 오류·새 경고 0)만 하고, 시험은 Unity에서 사용자가 돌린다(관리 실행 전체는 하지 않음). 사용자가 MSBuild를 건너뛰라고 하면 보고에 "컴파일 미확인"을 적는다.
- 착수: HE-a는 바로 가능. HE-b는 `C/CombatClipAttack.cs`에 Ballista 임시 클립 공격의 커밋 전 변경이 있어(설계 H-F12) 그 묶음 커밋 뒤 시작하거나, 커밋 때 hunk를 나눈다.

## 0. 계획에서 정한 것(보고, 설계 뜻은 그대로)

| # | 정한 것 | 이유 |
|---|---|---|
| PL1 | 값 생성자 `CombatUnitReactionPresentationData`는 끝 선택 인자 `EffectType hitEffect = EffectType.Empty`를 받고 생성자 검사는 더하지 않는다(플래시 값과 같은 관례, 검사는 SO `CreateData`). SO 검사는 `hitFlashDuration` 검사 뒤 | 기존 호출·시험 무변경, 지금 관례 |
| PL2 | 맞은 방향 함수 `C/Display/CombatHitDirection.Resolve(CombatCharacterEvent e, FieldGridMapper mapper, Quaternion current)`(정적 클래스, 새 파일). 반응은 `pose.Rotation`을 넘긴다. 본문은 지금 `ResolveDirection` 그대로 옮기고 인자 null 검사만 더한다 | 설계 HE2, 동작 같음 |
| PL3 | `FieldUnitPresenter`: private 도우미 `TryPlaceBodyBox(key, worldCenter, worldRotation, out Vector3 boxCenter, out Vector3 scaledSize)` 하나를 `BodyOf`(2D 접촉 몸)와 새 `internal Vector3 BodyCenterOf(CharacterCombatFacts, FieldGridMapper)`가 함께 쓴다. 상자가 없으면 배치 중심. 위치가 없으면 `BodyOf`와 같은 예외 | 설계 HE3, 상자 배치 식 한 곳 |
| PL4 | `CombatShownFacts`: 지금 `TryGetBody`의 캐릭터 찾기 규칙(필드·위치·살아 있음)을 private `TryFindFieldCharacter(id, out facts)`로 빼서 `TryGetBody`와 새 `TryGetBodyCenter(EntityId, out Vector3)`(캐릭터만)가 함께 쓴다 | 찾기 규칙 한 곳 |
| PL5 | `CombatEffectPiece`: 생성자를 private `(CombatEffectWindow, EffectType, Func<CombatPieceContext, Pose>)`로, 만드는 함수 `AtClipPoint(EntityId unit, CombatEffectWindow, EffectType)`(지금 동작·예외 문구 그대로)와 `At(CombatEffectWindow, EffectType, Pose)`. `Begin`은 대리자 하나를 부르고 창구로 낸 뒤 끝 | 설계 HE4, 모드 분기 없음 |
| PL6 | 두 조립(`ComposeHit`·`ComposeDeath`)은 끝 선택 인자 `CombatEffectPiece hitEffect = null`을 받아 **맨 끝 항목**으로 시작 순간·기다리지 않게 붙인다(기존 항목 순번 그대로 — 조립 시험이 순번을 봄) | 설계 HE5 |
| PL7 | 캐릭터 처리기: 생성자 끝 선택 인자 `CombatEffectWindow effects = null`, 도우미 `HitEffectOf(id, settings, e, pose)`(창구·`HasHitEffect`·몸 중심 셋 중 하나라도 없으면 null). 회전 = `Quaternion.LookRotation(CombatHitDirection.Resolve(e, mapper, pose.Rotation), mapper.WorldUp)` | 설계 HE5·P1 |
| PL8 | 부르는 자리: 피격은 지금 조립 직전(사실 반영·세대 확인 뒤), 사망은 자세를 빌린 뒤·조립 직전(사실 반영 전) | 설계 HE5, 두 경우 모두 보인 사실에서 몸을 찾음 |

## HE-a — 값 (화면 변화 없음)

- **T1 SO 칸** `DV/CombatUnitReactionPresentationSO.cs`: "피격" 머리말 아래(플래시 칸 뒤)에 `hitEffect`(`EffectType`, 기본 Empty), 툴팁 "맞는 순간(치명타 포함) 맞은 유닛 몸 중심에 내는 이펙트입니다. 이펙트의 +Z가 맞은 방향(공격 출처 반대쪽)을 봅니다. Empty면 없습니다." `CreateData`: `CombatPresentationEffectRules.IsKnown`이 아니면 `Invalid(nameof(hitEffect))`, 값에 넘김.
- **T2 값** `DV/CombatUnitReactionPresentationData.cs`: PL1, `HitEffect`, `HasHitEffect => CombatPresentationEffectRules.IsPlayable(HitEffect)`, 문서 주석.
- **T3 시험** `T/CombatUnitReactionAssetTests.cs`: 기본은 Empty·`HasHitEffect` 거짓, 정의된 값이 실리고 `HasHitEffect` 참, 정의 안 된 값(`(EffectType)999`)은 `InvalidOperationException`. 기존 사례 무변경.
- **확인(Unity):** 컴파일, `CombatUnitReactionAssetTests`, `DataBaseAssetTests`(실제 반응 에셋은 새 칸 기본 Empty).

## HE-b — 기반 (동작 같음, 화면 변화 없음)

- **T4 맞은 방향** 새 `C/Display/CombatHitDirection.cs`(+ `.meta`): PL2. `C/Display/CombatUnitReactionPresenter.cs`의 `ResolveDirection` 삭제, `BeginHit`이 새 함수를 부름.
- **T5 몸 중심** `C/Display/FieldUnitPresenter.cs`: PL3. `C/CombatShownFacts.cs`: PL4.
- **T6 조각 자리 출처** `C/CombatEffectPiece.cs`: PL5. `C/CombatClipAttack.cs` `AddEffect` 한 줄 → `CombatEffectPiece.AtClipPoint(...)`.
- **T7 시험:**
  - 새 `T/CombatHitDirectionTests.cs`(Unity 전용, 필드 Mapper): 근원 칸 하나 → 근원 반대쪽, 근원 칸이 둘 이상 다름·근원 칸 없음 → 지금 회전의 뒤쪽, 그것도 0이면 기본 뒤쪽, 위치 없는 사건 → 뒤쪽.
  - `T/FieldUnitBodyTests.cs`: `BodyCenterOf` — 상자 중심 높이 포함·XZ는 `BodyOf` 중심과 같음, 회전·배율 반영, 콜라이더 없는 외형은 배치 중심.
  - `T/CombatClipAttackCompositionTests.cs`·`CombatClipAttackSceneTests.cs`: 무변경(지금 예외 문구 단정 그대로) — 회귀 확인만.
  - 새 `T/CombatEffectPieceTests.cs`(Unity 전용, 가짜 이펙트 `FieldRailwayPresenterTests.Effects`): `At` — 시작에 받은 자리에 한 번 내고 바로 끝, 취소·Tick은 아무것도 안 함.
- **확인(Unity):** 컴파일, 위 시험 + 회귀 `CombatUnitReactionTests`·`CombatUnitReactionSetTests`·`CombatUnitDeathTests`(반응 방향 동작 같음)·`FieldUnitBodyTests` 기존 사례·`CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`·`CombatEffectWindowTests`.

## HE-c — 연결 (칸을 채운 반응 에셋의 유닛만 이펙트)

- **T8 조립** `C/CombatCharacterEventHandler.cs` `ComposeHit`·`ComposeDeath`: PL6, 문서 주석에 항목 설명 한 줄씩.
- **T9 처리기** 같은 파일: PL7·PL8, 클래스 요약 주석에 피격 이펙트 한 줄.
- **T10 Presenter** `C/CombatSceneEventPresenter.cs` `:110`: 캐릭터 처리기에 `effects` 넘김(Factory 무변경).
- **T11 시험:**
  - `T/CombatHitCompositionTests.cs`·`T/CombatDeathCompositionTests.cs`: 이펙트 조각을 넘기면 맨 끝에 시작 순간·기다리지 않는 항목, 완료 시점은 넘기지 않을 때와 같음. 기존 사례 무변경(null).
  - 새 `T/CombatHitEffectSceneTests.cs`(Unity 전용, 조립은 `CombatCodeMotionHitTests`의 반응 SO·피해 사건 꼴 + 가짜 이펙트 창구): ① 살아남는 피해 → 이펙트 한 번, 자리 = 맞은 유닛 몸 상자 중심, 회전 +Z = 근원 반대쪽 ② 치명타(`Died`) → 한 번 ③ 칸 Empty → 없음 ④ 창구 없음 → 없음 ⑤ 두 대상 동시 피해 → 대상마다 한 번 ⑥ 결과 재생 취소 → 남은 이펙트 회수(창구 `StopAll`).
- **확인(Unity):** 컴파일, 위 시험 + 회귀 `CombatCodeMotionHitTests`·`CombatRailwayContactSceneTests`(기차 동시 사망)·`CombatSceneEventPresenterTests`·`CombatClipAttackSceneTests`, 재생(반응 에셋 `hitEffect`에 등록된 종류 하나를 넣고 아군·적 피격·치명타, 대포 착탄과 겹침).

## HE-d — 공유·기록

- **T12 호진님** 클립 메모(`docs/superpowers/handoffs/2026-10-19-unit-animation-clip-hojin-share.md`) 새 절: 반응 SO `hitEffect` 칸, 지금 반응 에셋은 하나를 공유하니 유닛별로 다르게 하려면 반응 에셋을 나눠 외형 항목에 연결.
- **T13 민형님** Factory 공유(`docs/superpowers/handoffs/2026-10-15-combat-scene-display-factory-framework-share.md`) §19에 한 줄: 전투용 `EffectType` 등록 요청 범위에 피격 이펙트 추가, 피격 이펙트는 +Z가 맞은 방향(공격 반대쪽)으로 튀게 만듦(Factory 무변경).
- **T14 기록:** 설계 상태 줄, 폴더 정리 계획 대응표에 `CombatHitDirection` 한 줄(반응 부품 묶음, 폴더 정리 결정 때 확인), CLAUDE.md.

## 커밋 묶음(사용자)

- 새 파일 넷 `.meta`와 함께: `C/Display/CombatHitDirection.cs`, `T/CombatHitDirectionTests.cs`, `T/CombatEffectPieceTests.cs`, `T/CombatHitEffectSceneTests.cs`.
- `C/CombatClipAttack.cs`는 Ballista 임시 클립 공격 변경과 섞이면 hunk를 나눈다.
- 반응 에셋에 이펙트를 고르면 그 에셋은 같은 커밋(값이 없으면 화면 변화 없음).

## 실행 기록

### HE-a~HE-d (2026-10-20, 사용자 "HE-d 까지 진행해, MS Build 및 검토 한번에 하고 스킵하자") — 피격 이펙트 작업 끝

Phase마다 멈추지 않고 이어서 했고, MSBuild·검토는 끝에 한 번만 했다. 착수 때 `CombatClipAttack.cs`의 Ballista 임시 클립 변경은 이미 커밋돼 있어 hunk 나누기는 필요 없다.

- **HE-a:** 반응 SO `hitEffect`(툴팁·`IsKnown` 검사)·값 `HitEffect`·`HasHitEffect`(PL1). 시험 `CombatUnitReactionAssetTests.HitEffect_DefaultsToEmpty_CarriesDefinedValue_AndRejectsUndefined`.
- **HE-b:** 새 `C/Display/CombatHitDirection.cs`(+ `.meta`), 반응 `ResolveDirection` 삭제·새 함수 호출. `FieldUnitPresenter` `PlaceBodyBox`(접촉 몸과 공유, 접촉 몸 결과는 식 순서까지 같음)·`BodyCenterOf`. `CombatShownFacts` `IsOnField`·`TryGetBodyCenter`. `CombatEffectPiece` `AtClipPoint`·`At`(생성자 private), `CombatClipAttack` 한 줄. 새 시험 `CombatHitDirectionTests`(5), `FieldUnitBodyTests` 사례 둘.
- **HE-c:** 두 조립 끝 인자 `hitEffect`(맨 끝 항목), 처리기 창구 인자·`HitEffectOf`, Presenter 한 줄. 시험: `CombatHitCompositionTests` 둘(조립 항목·조각 인자 검사), `CombatDeathCompositionTests` 하나, 새 `CombatHitEffectSceneTests`(4). 모두 Unity 전용.
- **HE-d:** 호진님 클립 메모 §10, 민형님 Factory 공유 §19 한 줄(요청 범위 + 이펙트 +Z 규칙), 폴더 정리 대응표 `Presentation/Reaction/`에 `CombatHitDirection`.

**계획과 다른 점(셋):**
1. **사망 사건의 맞은 방향:** 구현 중 사망 사건의 결과 사실은 필드에서 빠진 모양(제외 구역·위치 없음)이라, 옛 `ResolveDirection` 그대로면 치명타 이펙트가 근원 방향을 잃고 "지금 회전의 뒤쪽"이 되는 것을 찾았다. `CombatHitDirection`이 유닛 위치를 "결과 위치, 없으면 사건 전 위치"로 읽는다. 피격 반응은 결과 위치가 늘 있어 동작이 같다. 시험 `DeathWithoutAFinalPosition_UsesThePositionBeforeTheEvent`, 장면 시험의 치명타 사건도 Core 모양(위치 없음)으로 만듦.
2. **PL4 모양:** 찾기 규칙 공유를 `TryFindFieldCharacter` 대신 판정 함수 `IsOnField` 하나로 했다(`TryGetBody`는 캐릭터를 찾았지만 필드 밖이면 구조물을 보지 않고 실패해야 해서, 찾기와 판정을 나누는 편이 그 규칙을 그대로 둠).
3. **시험 자리:** 새 `CombatEffectPieceTests`는 만들지 않았다 — 조각을 시작하려면 실행 맥락(`CombatPieceRun`)이 필요해 단독 조립이 커서, `At`의 인자 검사·자리·한 번은 `CombatHitCompositionTests`가 실제 조각 실행 재생기로 본다. 장면 시험 ④ 창구 없음·⑤ 두 대상 동시는 넣지 않았다(④는 창구 없이 조립하는 기존 장면 시험 모두가 지나는 길, ⑤는 대상마다 조립이 따로라 동시 묶음 구조에 맡김 — 직접 확인은 재생으로).

**검증:** MSBuild 한 번(`ProjectL.Tests.Framework`, 새 시험 파일 둘은 targets로 넣음) — **바꾼·새 파일 오류 0·경고 0.** 시험 프로젝트 전체는 **오류 2로 실패**: `CombatDeploymentConfirmationTests.cs`(현강님 `fc133bf7`)의 시험 가짜 `CharacterSource`가 `ICharacterBufferReadSink`의 새 멤버 `RosterRevision`·`TryGetMainState`(천명님 `16333690`)를 구현하지 않음 — HEAD에 이미 있는 문제라 고치지 않았다(Unity 시험 어셈블리도 이 때문에 컴파일되지 않음). 관리 실행 없음. 검토(자체): 결함 0, 위 다른 점 셋.

**이어서(2026-10-20, 사용자 지시 "CharacterSource에 RosterRevision·TryGetMainState를 채워서 시험 어셈블리 컴파일되게 해 줘"):** 현강님 시험 가짜 `CombatDeploymentConfirmationTests.CharacterSource`에 `RosterRevision`을 더하고 옛 한 인자 `TryGetMainState`를 새 서명(`CharacterInstanceId` 받음)으로 바꿈 — 배치 확인은 `Owned`·`Characters`만 읽어 둘 다 `NotSupportedException`, 시험 본문·단언 그대로. 현강님 가방 메모 §4 한 줄. 다시 빌드는 사용자 지시("MSBuild 스킵")로 중간에 멈춰 **컴파일 미확인**.

**확인(Unity, 사용자):** 시험 어셈블리가 컴파일되면 `CombatUnitReactionAssetTests`·`CombatHitDirectionTests`·`FieldUnitBodyTests`·`CombatHitCompositionTests`·`CombatDeathCompositionTests`·`CombatHitEffectSceneTests` + 회귀 `CombatUnitReactionTests`·`CombatUnitReactionSetTests`·`CombatUnitDeathTests`·`CombatCodeMotionHitTests`·`CombatRailwayContactSceneTests`·`CombatSceneEventPresenterTests`·`CombatClipAttackCompositionTests`·`CombatClipAttackSceneTests`·`CombatEffectWindowTests`·`DataBaseAssetTests`. 재생: `CombatUnitReaction_Default`의 `hitEffect`에 `Boom`을 임시로 넣고 아군·적 피격·치명타, 대포 착탄과 겹침(확인 뒤 되돌림).

**커밋 묶음:** 새 파일 셋 `.meta`와 함께 — `C/Display/CombatHitDirection.cs`, `T/CombatHitDirectionTests.cs`, `T/CombatHitEffectSceneTests.cs`. 바뀐 파일 — `DV/CombatUnitReactionPresentationSO.cs`·`CombatUnitReactionPresentationData.cs`, `C/Display/CombatUnitReactionPresenter.cs`·`FieldUnitPresenter.cs`, `C/CombatShownFacts.cs`·`CombatEffectPiece.cs`·`CombatClipAttack.cs`·`CombatCharacterEventHandler.cs`·`CombatSceneEventPresenter.cs`, 시험 `CombatUnitReactionAssetTests`·`FieldUnitBodyTests`·`CombatHitCompositionTests`·`CombatDeathCompositionTests`, 문서(설계·계획·공유 메모 둘·폴더 정리 계획·CLAUDE.md). 에셋 무변경.
