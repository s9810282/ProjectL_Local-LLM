# 천명님 Core `e413008c` 병합 뒤 전투 View 대응 항목 정리 (2026-10-26, 코드 변경 없음)

- **목적:** 이번에 받은 Core 변경(세 커밋)에 View가 대응해야 할 것을 한 곳에 모은다. 다음 대응 설계(설계 → Task → Phase)의 입력이다. 사실과 판단을 나눠 적고, 정할 것은 §6 질문으로 둔다.
- **기준:** HEAD `7690a5fd` 위에 `origin/feature/Cheon/Core/NewCombatSytem`(`e413008c`)을 `--no-commit --no-ff`로 병합하고 충돌을 정리해 스테이지한 상태다(커밋 전). Unity 컴파일은 문제 없음(사용자 확인). EditMode 시험·재생은 미확인이다. 줄 번호는 이 작업 트리 기준이다.
- **근거:** 천명님 인계 둘 — [충격 볼트](../handoffs/2026-10-26-ballista-impact-bolt-core-to-view-handoff.md)(`e413008c`), [직군·특성 개편](../handoffs/2026-10-26-category-unit-trait-core-view-handoff.md)(`4bd6b574` Phase D). 병합 정리 기록은 [Core 전달 문서 §40](../handoffs/2026-09-24-view-core-data-boundary-handoff.md).
- **약어:** `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `UI/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/`, `VDB/` = `Assets/ProjectL/DataBaseSO/ViewGroup/`, `T/` = `Assets/Tests/EditMode/`.

## 0. 받은 Core 변경

| 커밋 | 내용 | View에 닿는 사실 |
|---|---|---|
| `c816c46d` | Core 시험 복구·문서, `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")` | 없음 |
| `4bd6b574` | 직군·특성 개편: 라이더 환급·헤비 경감·광란 삭제, 사냥꾼의 표식(Shotgun)·끌어오기(Cowboy) 추가, View 연결 Phase D(옛 View 기준) | 끌어오기 사건·스냅숏, 표식 사건·디버프, 기본/전체 공격 범위, 실제 타격 칸 |
| `e413008c` | 헤비 넉백 면역 끔(명시 강제 이동 면역만 남음), 발리스타 관통 → 충격 볼트(`TraitKey.ImpactBolt = 210`, 거리 1), 지정 방식 7 = 사각 범위·각도 무제한·Radial 70%, 벽 무시 전달, 해머는 지정 방식 9로, 정밀 사격은 관통 교체를 뺌 | `AttackResult.PiercingShot`이 늘 null, 충격 볼트 상태·보유 설명 스냅숏, 밀치기는 기존 `CombatCharacterEvent(Moved)` |

- 충격 볼트: 중심 = `PrimaryCell`(끌어오기와 같이 쓰면 `Pull.FinalAnchor`), 후보 = 실제로 맞은 대상만, 방향 = 중심에서 대상 쪽(대각 포함 8방향), 중심 칸 대상은 움직이지 않음, 살아 있는 적 필드 유닛만 밀림, 피해 0이어도 맞았으면 밀림. 새 사건 형식은 없다.
- 공개 `AttackPreview`에는 밀려날 최종 위치가 없다(`TargetOccupiedCells`는 원래 자리).
- 지금 DB에서 `PiercingFalloff`를 가진 유닛은 없다. 충격 볼트는 Ballista, 끌어오기는 Cowboy, 표식은 Shotgun, 넉백은 Gravekeeper·Stage1Boss·DynamiteHammer다.

## 1. 병합 때 이미 한 것

`4bd6b574`가 10-23 시점 옛 View 위에서 한 Phase D를 우리 1~5라운드 구조로 옮겼다(자세한 것은 Core 전달 문서 §40).

- 사건 형식 표 `V/Playback/Ordering/CombatEventTypeTable.cs`에 `CombatPullEvent`(처리기 칸 `Pull`)·`CombatHuntersMarkEvent`(사실 칸) 줄 둘. 처리기 표 `pull` 인자는 필수.
- 층 표 `V/Display/Field/FieldHighlightLayers.cs`에 `PullRange`(`1 << 11`, `:97`)·`PullDestination`(`1 << 12`, `:108`). 천명님 판 `1 << 10`이 `AllyHoverRange`와 겹쳐 바꿨다.
- `CombatAttackTarget.DeliveryCell`을 공격 종류 전략 다섯 곳·조준 방향·궤적 미리보기에, 이동만 하는 끌어오기면 궤적을 지움.
- 카드 특성 설명 `UI/Global/UnitCardTraitContent.cs`는 우리 "사용 불가" 표시와 천명님 설명 분기(가시·표식·끌어오기)를 함께 유지.
- 시험: 천명님 `T/CoreIntegration/PullAndMarkPresentationTests.cs` 생성·`RouteOf` 호출만, 우리 `CombatEventRoutesTests`(20 → 22)·`FieldOutlineRendererTests`(손잡이 넷)·`CombatEventHandlerTableTests`(호출) 맞춤.

## 2. 대응 항목

### A. 반드시 고칠 것

**A1. Ballista 공격이 예외로 멈춘다.**

- 사실: 클립 공격 경로(`CombatClipAttack.ComposeRanged` → `CombatAttackPlayback.FireShot` → 공격 종류 전략)에서 `V/Presentation/Attack/CombatBallistaStyle.cs:71`이 `PiercingShot == null`이면 예외를 던진다. 새 Core는 늘 null이므로 Ballista가 공격할 때마다 F1로 멈춘다.
- 사실: 지금 Ballista 전략은 관통 전용이다. 해제 입구는 비어 있고(`:114` `ReleaseNone`, 관통 대상마다 따로 해제하던 방식), 궤적은 직선이며(`DrawsArc` 기본 false, `V/Presentation/Attack/CombatAttackStyle.cs:43`), 곡사 높이는 0이다(`:41`). 관통 종점이 없으면 목표는 주 칸으로 대신 간다(`:28-30`).
- 사실: 외형 설정 `VDB/CombatPresentation/Setting/CombatAttack_Ballista.asset`은 `kind: 4`(Ballista)이고, 곡사 값 `flightDuration 0.8`·`arcHeightInCells 2`와 접촉 이펙트 `impactEffect: 4`를 이미 갖고 있다.
- 권장(Q-E1 A): Ballista 전략을 "곡선 한 발·착탄 한 번"으로 바꾼다. 목표 = `DeliveryCell`, 궤적 곡선(`DrawsArc` true, 곡사 높이 = 외형 값), 착탄 순간 맞은 대상 모두 해제(해제 입구 기본으로), 외형 종류 Ballista·클립·포구·소리는 그대로. 밀치기는 기존 이동 후속(`V/Presentation/Attack/CombatHitFollowUps.cs:111`, `PiercingShot`이 null이면 모두 히트 해제 대상)과 동시 넉백 경로를 그대로 쓴다.
- 대안(Q-E1 B): 외형 설정 kind를 Mortar로 바꾼다. 곡사 전략의 궤적 출발점이 `CatusCannonWeaponView`를 요구해(`V/Presentation/Attack/CombatMortarStyle.cs:36`) Ballista 프리팹에서는 궤적 미리보기가 사라진다. 권장하지 않는다.

### B. 표시 누락·정할 것

**B1. 충격 볼트 특성 표시가 없다.**

- 사실: `VDB/TraitVisual/`에 key 210 에셋이 없다. HUD는 기본 아이콘과 빈 설명을 보이고(`V/Display/Unit/FieldUnitPresenter.cs:318-333`, 이름은 `ImpactBolt`로 대신), 정보창·카드는 그 특성을 빼고 보인다(`UnitCardTraitContent`가 표에 없으면 건너뜀).
- 할 일: `TraitVisual` 에셋(이름 "충격 볼트"·아이콘·설명)을 만들어 표에 등록한다. 아이콘은 아트가 필요하다.
- 참고(이번 변경과 무관, 정리 중 발견): `TraitVisual_Regeneration.asset`(재령님 `6ea85bc6`, 10-20)이 표 `VDB/TraitVisual/TraitVisualDataTable.asset`에 등록되어 있지 않다. 재생 특성을 가진 적 `Unit_UndeadHeavy`도 같은 증상(HUD 기본 아이콘, 정보창에서 빠짐)이다. 표에 한 줄 더하면 풀린다.

**B2. 충격 볼트 거리 설명이 연결되지 않았다.**

- 사실: 전투 쪽 `ImpactBoltTraitStatusSnapshot.DistanceCells`, 보유 쪽 `OwnedCharacterStatsSnapshot.ImpactBoltDescription`을 쓰는 곳이 없다. 카드 설명 분기(`UI/Global/UnitCardTraitContent.cs:69`)에 충격 볼트가 없고, 보유 카드 경로가 넘기지 않는다(`UI/Global/UnitCardUI.cs:265`, `UI/Stage/DeckEditUI.cs:269`, `V/../StageMap/Deck/DeckEditModelBuilder.cs:47`·`DeckEditDisplayModel.cs:23-24`).
- 할 일: 설명 분기 하나와 보유 카드 경로(카드 UI·덱 편집 모델·빌더·UI)에 거리 값 전달. 설명은 View가 기본값을 넣지 않고 Core 값을 읽는다(인계 §4).
- 영역: UI(`GlobalSystem/UISystem`)·StageMap은 다른 작업자 영역이라 사용자 지시가 필요하다(Q-E2).
- 참고: 덱 편집은 가시 설명을 지금도 넘기지 않는다(`DeckEditUI.cs:269`의 첫 인자 `null`, 천명님 인계가 범위 밖으로 적은 기존 누락).

**B3. 끌어오기 거절 사유 안내가 없다.**

- 사실: 새 `AttackFailureReason` 셋(`PullPathBlocked = 40`·`PullImmune = 41`·`PullNoMovement = 42`)은 `V/Input/Assist/CombatActionNotice.cs:28-32`의 비용·행동 안내에도, 대상 실패 묶음(`:86-101`)에도 없어 안내 없이 끝난다.
- 할 일: 안내 문구를 정해 넣는다(Q-E3, 문구 승인 필요).

**B4. 끌어오기 확장 범위가 GridOutline에서 채워지지 않는다.**

- 사실: 운영 표시 모드는 GridOutline(`VDB/StageFieldView/Item/StageFieldView_Default.asset` `fieldDisplayMode: 1`)이다. 기본 공격 범위(`AttackRange`)는 칸을 채우지만 `PullRange`는 테두리만 그린다(`FieldHighlightLayers.cs:97` 채움 false, 천명님 판의 채우기 목록에 없던 것을 그대로 둠).
- 할 일: 채울지 정한다(Q-E4). 채우면 `FieldOutlineRendererTests`의 채우기 이름 목록이 바뀐다.

**B5. 끌어오기·표식 아이콘이 비어 있다.**

- 사실: `VDB/TraitVisual/Item/TraitVisual_Pull.asset`·`TraitVisual_HuntersMark.asset`의 `icon`이 비어 있다. 표식 HUD는 기본 아이콘을 쓴다(`FieldUnitPresenter.cs:309-316`).
- 할 일: 아트 지정(코드 변경 없음).

**B6. 충격 볼트로 밀려날 칸을 미리 보일 수 없다.**

- 사실: 공개 미리보기에 최종 위치·이동 사건이 없다. 지금 공격 호버는 영향 칸(3×3)과 체력 예상만 보인다. View가 거리만으로 위치를 계산하면 안 된다(인계 §4).
- 할 일: 미리 보이려면 Core에 미리보기 사실을 요청한다(Q-E5). 요청하지 않으면 그대로.

헤비 넉백 면역 삭제·해머 지정 방식 변경·정밀 사격 설명 변경은 View 할 일이 없다. View에 면역 판정이 없고, 화면 문구(`VDB/TraitVisual`·`VDB/CategoryVisual`, View·UI 코드)에 "헤비 넉백 면역"·"경감"·"환급"을 말하는 곳이 없다.

### C. 구조 정리(선택)

**C1. 관통 전용 View 길이 운영에서 쓰이지 않게 된다.**

- 사실: 관통 특성을 가진 유닛이 없다. A1을 하면 Ballista 관통 발사(`CombatBallistaStyle.cs:68-111`), 관통 접촉 진행률(`V/Presentation/Attack/CombatAttackPlayback.cs:350`·`:440`), 맥락 계약의 `FireThrough`(`:416`, `ICombatAttackStyleContext`)가 운영에서 닿지 않는다. 투사체 진행의 `FireThrough`는 직선 발사도 쓰므로 남는다. `CombatHitFollowUps`의 관통 분기는 일반 계약이라 남는다.
- 판단: Core는 관통 계약을 계속 지원한다. 걷으면 "불필요" 기준이 좋아지고, 남기면 나중에 관통 유닛이 다시 생길 때 쓴다(Q-E6).
- 참고: `VDB/TraitVisual/Item/TraitVisual_PiercingFalloff.asset`은 참조가 끊기지만 규칙대로 지우지 않고 보고만 한다.

**C2. 병합 검토의 낮음 셋(천명님 판과 같은 의미, 고치지 않음).**

- 끌어오기 대상의 레인이 겹침 허용 상태(남은 보간·미룬 방향 복귀)면 `V/Presentation/Movement/CombatUnitMotionSet.cs:126`이 예외를 던질 수 있다. `CombatStepContactGate.IsMoverOwn`(`:143`)이 끌어오기를 이동자 자신의 사건으로 보지 않기 때문이다. 고치면 동작이 바뀐다(Q-E7).
- 표식 아이콘을 읽을 때 null 검사가 없다(`FieldUnitPresenter.cs:312`, 바로 아래 `:322`는 검사함).
- 들여쓰기 두 줄(`CombatUnitMotionSet.cs:130`, `V/Presentation/Movement/CombatUnitMotionPlayback.cs:174`).

## 3. 실행에서 실패할 것으로 보이는 기존 시험

| 시험 | 작성자 | 이유 |
|---|---|---|
| `T/Framework/DataBaseAssetTests.cs:538` `Ballista_LoadedCatalogBuildsSinglePiercingProfile`, `:550` `Ballista_LoadedTraitHasExplicitUnlimitedCap` | 천명님 | 옛 관통 설정을 기대 |
| `T/Framework/DataBaseAssetTests.cs:239` 이후 정밀 사격 사례 | 호진님(대부분) | 기본·교체 관통을 기대 |
| `T/Framework/TargetingAngleAssetTests.cs:30`·`:70`·`:106` | 천명님 | 옛 정면·8방향 직선·지정 방식 3 비교를 기대 |
| `T/Framework/CombatBallistaAssetTests.cs` | 우리 | 외형 종류 Ballista·투사체 값을 확인. A1 권장안이면 그대로 통과 |
| `T/Framework/CombatBallistaAttackTests.cs`(고정물) | 우리 | 합성 관통 자료로 반응·사망·피격 이펙트 시험이 빌려 씀. C1에서 관통 길을 걷으면 영향 |

- 소스로 본 것이고 실제 실패 수가 아니다. EditMode 전체는 아직 돌리지 않았다.
- 다른 사람이 쓴 시험은 고치기 전에 묻는다(Q-E8).

## 4. 수동 확인(인계 문서 목록)

- **끌어오기·표식:** 사거리 안/밖 적 공격, 막힌 끌어오기, 아군 이동 + 지형 피해, 장애물은 그대로, A=3/P=2, 표식 부여·소모·중첩 없음, 퍼센트 바꿈, 씬을 나갈 때 정리. 셀 프리팹·GridOutline 두 모드 모두.
- **충격 볼트:** 중심이 빈 칸, 중심 대상 제외, 8방향, 거리 1/2, 피해 0인 대상 밀기, 명시 면역, Heavy 밀림, 막힌 칸·보드 가장자리, 전투 종료 시 멈춤, 반복·취소.

## 5. 공유(모두 미전달)

- **천명님:** Core 전달 문서 §40에 적음(Phase D 이식 방식 — 층 비트 11·12, `pull` 필수 인자, 시험 호출 맞춤).
- **현강님·호진님:** B2를 하면 카드 UI·덱 편집 변경을 알린다.
- **민형님:** 천명님이 ServiceKeys(`InGameCoreSystemServiceKey`의 `HeavyProtection` 키)와 Factory(`CombatSystemFactory` 등록 줄)를 지웠다. 민형님 공유 문서에 한 줄 남길 만하다.

## 6. 정할 것과 권장 순서

| 질문 | 내용 | 권장 |
|---|---|---|
| Q-E1 | Ballista 발사 방식 | A — 전략을 곡선 한 발·착탄 한 번으로 |
| Q-E2 | 충격 볼트 거리 설명을 위해 UI·StageMap 파일을 고칠지 | 사용자 결정(다른 작업자 영역) |
| Q-E3 | 끌어오기 거절 안내 문구 | 사용자 결정(문구) |
| Q-E4 | 끌어오기 확장 범위를 GridOutline에서 채울지 | 사용자 결정 |
| Q-E5 | 밀려날 칸 미리보기를 Core에 요청할지 | 사용자 결정 |
| Q-E6 | 관통 전용 View 길을 걷을지 | 사용자 결정 |
| Q-E7 | 끌어오기 시작 전 레인 겹침(`IsMoverOwn`)을 고칠지 | 사용자 결정 |
| Q-E8 | 다른 사람이 쓴 Ballista·정밀 사격 시험을 고칠지 | 사용자 결정 |

권장 순서: A1 → B1 → (B2·B3·B4·C1 결정에 따라) → §3 시험 → §4 수동 확인. A1은 다른 항목과 겹치지 않아 먼저 할 수 있다.

## 7. 인계 문서와 우리 코드가 다른 점

천명님 인계는 천명님 브랜치의 옛 View(10-23 기준)를 가리킨다. 우리 코드에서는 자리가 다르다.

| 인계가 가리키는 것 | 우리 코드 |
|---|---|
| `CombatAttackPlayback.TryBeginBallista`(~458), `FirePiercingShot`(~397) | 3라운드 V3-e·V3-f에서 걷음. 같은 자리는 `CombatBallistaStyle`(`:68` `FireShot`) |
| 재사용 후보 `TryBeginMortar`·Mortar `FireFromSource`·`CombatShotPath.TryTarget(kind, …)` | 공격 종류 전략 `CombatMortarStyle`(`TryBeginCodeMotion`·`FireShot`), 목표점 원본 `CombatAttackStyle.TryTarget` |
| `CombatEventRoutes`·`CombatEventSubjects.TryGetSubject` | 사건 형식 표 `CombatEventTypeTable` |
| `FieldHighlightLayers.DrawOrder` 목록·`ColorOf` switch·`ActingLayers` 상수 | 층 표 `Rows` 한 곳에서 파생 |
| `CombatBallistaLifecycleTests` | V3-e에서 지움 |
| `CombatAttackTarget.DeliveryCell`이 "이미 있음" | `4bd6b574`가 가져온 것(병합 뒤에 있음) |
