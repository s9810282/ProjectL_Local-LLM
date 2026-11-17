# 관통·죽음의 손·기절 전투 View 대응 Task 계획 (2026-11-16)

설계: [piercing-dead-hand-view-design](../specs/2026-11-16-combat-piercing-dead-hand-view-design.md)(결정 D0~D6, Q-PD1~Q-PD4 모두 결정).

상태: **계획 작성, 코드 착수 전.** 계획에서 정한 것 TP-1~TP-10(§1)은 사용자 확인 대상이다.

진행 규칙:

- Phase마다 멈추고 보고한다. "진행해"는 이 계획에 적힌 Phase 범위만이다.
- View 작업이라 **새 시험을 쓰지 않고 MSBuild도 돌리지 않는다.** 보고에는 "컴파일 미확인"과 Unity 확인 목록을 적는다. 기존 시험이 컴파일에서 깨지는 곳만 최소로 맞추고, 다른 사람이 쓴 시험이면 먼저 묻는다.
- Phase 코드가 끝나면 보고 전에 읽기 전용 새 에이전트 하나로 그 변경을 검토하고 반영한다.
- 새 `.cs`의 `.meta`는 Unity가 만든다. 커밋 때 함께 넣는다. 커밋·push는 사용자가 한다.
- Core·Factory·ServiceKeys는 바꾸지 않는다.

## 1. 계획에서 정한 것 (확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 휩쓸기 머리 사건(`CombatSweepEvent`)은 사건 형식 표에 줄을 더하지 않는다. 묶음 해석기와 싱크 소속 대응이 형식으로 알아본다. | 형식 표는 "보이는 사건"의 원본이고 머리는 건너뛰는 사건이다. 줄을 더하면 처리기 칸·경로 함수가 따라 늘어난다(설계 P2). |
| TP-2 | 탄 하나의 접촉 목록은 새 순수 값 함수 하나(`Presentation/Attack/CombatProjectileContacts`)가 만든다. 몸 조회·진행률 계산은 지금처럼 공격 재생(`ContactProgress`)이 맡고 함수에는 대리자로 넘긴다. | 진행률 대체 경고·몸 조회는 공격 재생이 이미 갖고 있다. 목록 규칙(관통 = 조우들, 아니면 접촉 대상 하나)만 한 곳에 모은다. |
| TP-3 | 해제 규칙은 새 해제기 하나(`Presentation/Attack/CombatContactRelease`)가 맡고, 클립 원거리 조각과 코드 모션 직선이 같이 쓴다. "마지막 탄" = 클립 발사 목록의 마지막 순번, 코드 모션은 탄이 하나라 늘 마지막 탄이다. | 설계 D6을 코드 한 곳에 둔다. 연발 숫자·겉보기 피격(앞 탄)은 지금 입구 그대로다. |
| TP-4 | 벽에서 멈춘 관통 탄은 벽 칸 몸에 처음 닿는 곳에서 사라진다(궤적 `CutProgress`와 같은 함수). | 설계 P1. 칸 가운데면 벽 안으로 들어가 보인다. |
| TP-5 | 출처 수명 계약 `ICombatSourceLifetime`에 `Release(CombatEvent source, EntityId target, int version)` 하나를 더한다. 조기 출처(`CombatDeferredAttackSource`)는 허용 뒤에만 넘긴다. 허용 전 접촉은 해제기가 순서대로 모아 둔다. | 지금 `ReleaseAll`과 같은 규칙이다. 합류 전에 대상이 풀리면 안 된다. |
| TP-6 | 코드 모션 조립(`CombatCodeMotionAttack.Compose`)의 타격 입구에 대상 해제 입구를 하나 더한다. 근접·곡사 코드 모션·포효는 그 입구를 쓰지 않는다(지금처럼 타격 = 전부 해제). | 영향 범위를 직선 코드 모션 하나로 묶는다. |
| TP-7 | 휩쓸기 싱크의 "시작"은 휩쓸기 묶음의 첫 이동 걸음이 시작되는 순간이다(머리 사건은 연출이 없어 시작이 아님). | 당김이 이동 레인 시작을 기준으로 삼는 것과 같은 꼴이다(당김 싱크 설계 §4). |
| TP-8 | 싱크 시작 모드 새 값은 `FromSweepPresentationStart = 2`다. 이 모드는 기준 칸(`attackAnimationStartKind`)을 쓰지 않고 검사하지도 않는다. 공격 클립은 지금 지정 모드처럼 필수다. | 설계 §3.5 A안. |
| TP-9 | SoulEater 전용 클립 묶음은 Boomber 묶음을 복사한 임시 에셋(`CombatUnitAnimation_SoulEater`)으로 만들고, 임시 외형이 그것을 가리킨다. 커밋 여부는 사용자가 정한다. | 설계 P3. Boomber 묶음에 모드를 켜면 Boomber도 바뀐다. |
| TP-10 | 기절 거절 안내는 공격·이동 이유 이름이 같아(`Stunned`) 한 줄씩이다: 안내 "기절 상태라 행동할 수 없습니다.", 짧은 글자 "기절". 개발 특성 미리보기(`PreviewStateWriters`)의 기절 쓰기는 이번 범위 밖이다. | 설계 D5(Q-PD4). |

## 2. Phase PD-a — 원거리 접촉 해제 규칙·관통

목표: 설계 §3.1~§3.3. 기존 원거리 공격은 보이는 동작이 같다.

| Task | 내용 | 파일 |
|---|---|---|
| Ta.1 | 공격 모양 값에 Core `PiercingShot`을 싣는다(`Of(attack)`과 다른 생성 입구는 null). | `Presentation/Attack/CombatAttackShape.cs` |
| Ta.2 | 접촉 목록 함수(TP-2): 관통이면 `Encounters` 순서대로 (대상, 진행률, 칸), 아니면 `ContactTargetId` 하나, 없으면 빈 목록. 관통 소실점(TP-4)도 여기. 진행률은 오름차순이어야 한다(같으면 조우 순서). | 새 `Presentation/Attack/CombatProjectileContacts.cs` |
| Ta.3 | 해제기(TP-3·TP-5): 앞 탄 접촉 = 지금 접촉 표시 입구만, 마지막 탄 접촉 = 그 대상 `Release`, 첫 해제 뒤 Hit 신호 한 번, 마지막 탄 끝 = `ReleaseAll` 뒤(아직이면) Hit 신호. 허용(`canRelease`) 전 접촉은 모았다가 순서대로. | 새 `Presentation/Attack/CombatContactRelease.cs` |
| Ta.4 | 출처 수명 계약에 대상 해제 입구(TP-5). 공격 재생은 `_gate.Release`, 조기 출처는 허용 뒤만 넘김. | `Playback/Players/ICombatSourceLifetime.cs`, `Presentation/Attack/CombatAttackPlayback.cs`, `Playback/Support/CombatDeferredAttackSource.cs` |
| Ta.5 | 투사체 진행: 비공개 `FireThrough`를 맥락 계약으로 연다(접촉 목록 + 접촉별 콜백). `FireLinear`(한 접촉)는 그 위 얇은 입구로 남긴다. | `Presentation/Attack/CombatProjectileFlights.cs`, `ICombatAttackStyleContext.cs`, `CombatAttackPlayback.cs` |
| Ta.6 | 클립 원거리: 연발 해제 조각이 해제기를 쓴다. 발사 조각이 탄 순번과 접촉 콜백(대상 포함)을 넘긴다. 직선 출처 발사가 접촉 목록·소실점으로 쏜다. 곡사 출처 발사는 착탄 하나 그대로. | `Presentation/Pieces/CombatVolleyReleasePiece.cs`, `CombatClipRangedAttack.cs`, `CombatFirePiece.cs`, `Presentation/Attack/CombatLinearStyle.cs`, `Playback/Support/CombatAttackStart.cs` |
| Ta.7 | 코드 모션 직선: 접촉 하나(`_hitAt`) → 접촉 목록. 목표점은 관통이면 소실점. 코드 모션 조립에 대상 해제 입구(TP-6). | `Presentation/Attack/CombatLinearAttackAnimation.cs`, `CombatLinearStyle.cs`, `CombatAttackStyle.cs`(직선 목표점 갈래), `Presentation/Pieces/CombatCodeMotionAttack.cs`, `CombatCodeMotionPiece.cs`, `Playback/Support/CombatAttackStart.cs` |
| Ta.8 | 궤적 미리보기: 응답에 `PiercingShot`이 있으면 공격자 → 소실점 한 줄(Ta.2 함수). | `Display/Feedback/CombatAttackTrajectory.cs` |
| Ta.9 | 관통 TraitVisual 이름 "관통"·토큰 문구(설계 §3.3). | `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_PiercingFalloff.asset` |

Unity 확인(임시 로드아웃: 메인 Sniper):

- 관통: 조준 칸 너머까지 탄이 날아가고, 조우마다 접촉 이펙트와 그 대상의 피격·숫자가 그 순간에 나온다. 아군도 맞는다. 4명 소진·벽(벽 면에서 사라짐)·구조물·판 끝에서 사라진다. 피해 숫자가 100/75/50/25% 꼴로 준다. 궤적 선 끝이 탄이 사라지는 곳과 같다. TraitVisual 문구에 25·4가 나온다.
- 기존 원거리 그대로: Akimbo 2연발(1발째 숫자·겉보기 피격, 2발째 체력·사망), 샷건 부채꼴, 대포·선인장 대포 착탄, 대신 맞기 수호자 접촉, 조기 공격 싱크(카우보이 끌어오기).
- 막힌 조준 궤적, 공격 취소·결과 재생 중 입력 막힘.

## 3. Phase PD-b — 죽음의 손 묶음·화살표·문구

목표: 설계 §3.4·§3.6·§3.7·§3.9. 방향은 기본값(오른쪽)만(D2).

| Task | 내용 | 파일 |
|---|---|---|
| Tb.1 | 묶음 해석: 머리 사건(TP-1) 뒤 같은 행동 구간, 같은 공격 그룹의 공격 사건 앞까지 오는 포착 대상의 `Moved`(원인 `TraitKey.DeadHand`)를 한 묶음(열쇠 = 공격 그룹·공격자). 경로가 끊긴 대상은 지금 규칙 (a)대로 묶음 밖. | `Playback/Ordering/CombatConcurrentGroups.cs` |
| Tb.2 | 화살표·도착 칸: `Append`에 `preview.Sweep` 출처(방향 = 대상 행 `IntendedEndpoint - StartAnchor` 부호, 도착 = 움직인 대상만, 표지 = 완료·중단 밀림·막힘·면역, 미처리 표지 없음). `ShowAim` 조건에 `Sweep`. 표지 바꾸기는 `KindOf` 옆 한 곳. | `Display/Feedback/CombatKnockbackPreview.cs` |
| Tb.3 | 문구 토큰: `[TraitKey.DeadHand]` 한 줄(`{피해계수}` = `DamagePercent`). 죽음의 손 외형 문구에 토큰, 훈련 수치 글 `피해 계수 {0}% → {2}%`. | `ViewSystem/Descriptions/Traits/TraitDescriptionBindings.cs`, `TraitVisual_DeadHand.asset` |

Unity 확인(임시 로드아웃: SoulEater 서브):

- 네 방향 범위 표기, 오른쪽 기준 화살표·도착 칸, 막힘·면역 표지, 판 가장자리 대상.
- 공격하면 대상들이 함께 밀리고(넉백과 같은 걸음), 이동이 끝난 뒤 공격 동작·피해가 나온다.
- 진입 지형 피해·사망이 낀 대상은 그 대상만 차례로, 희생(대신 맞기)은 이동 뒤.
- 정보창·HUD에 "죽음의 손"과 피해 계수 100%.

## 4. Phase PD-c — 공격 동작 싱크(휩쓸기 시작 기준)

목표: 설계 §3.5 A안. 작업자 입력은 `CombatUnitAnimationSO` 그대로.

| Task | 내용 | 파일 |
|---|---|---|
| Tc.1 | 시작 모드 값(TP-8)·검사·인스펙터(기준 칸은 발동 모드에서만, 지연은 두 모드 모두). | `DataBaseSystem/Data/View/CombatPresentation/CombatAttackAnimationStartMode.cs`, `CombatAttackAnimationStartData.cs`, `CombatUnitAnimationSO.cs`, `DataBaseSystem/Editor/CombatUnitAnimationSOEditor.cs` |
| Tc.2 | 소속 대응: 결합 값에 휩쓸기 갈래(발동 없음, 머리 사건 위치). 공격 결과의 `Sweep`과 같은 객체인 머리 사건을 찾고, 머리가 공격 앞에 있는지·공격자가 같은지 검사. | `Playback/Ordering/CombatPreAttackTraitBinding.cs`, `CombatPreAttackTraitMap.cs` |
| Tc.3 | 시작 알림(TP-7): 휩쓸기 모드 설정을 모으고, 휩쓸기 묶음 첫 걸음 시작에서 알린다. 앞 의존 검사 시작 위치는 머리 사건. | `Playback/Support/CombatPreAttackPresentationStarts.cs`, `CombatPreAttackPlayback.cs`, 이동 표시(`Presentation/Movement/CombatMovementPresentation.cs` 또는 걸음 재생기 — 구현 때 한 곳으로) |
| Tc.4 | SoulEater 전용 클립 묶음(TP-9): Boomber 묶음 복사, 모드 = 휩쓸기, 지연 0. 임시 외형이 가리킴. | 새 `CombatUnitAnimation_SoulEater.asset`, `UnitVisual_SoulEater.asset` |

Unity 확인:

- 휩쓸기 첫 걸음과 함께(또는 지연만큼 뒤) 공격 클립이 시작되고, 피해·피격은 이동이 다 끝난 뒤.
- 지연 0·0.3, 대상 없음(빈 휩쓸기), 모든 대상 면역, 전투 끝 중단.
- 다른 유닛(카우보이 끌어오기 지정 모드, 기본 모드 유닛) 그대로.

## 5. Phase PD-d — 기절

목표: 설계 §3.10.

| Task | 내용 | 파일 |
|---|---|---|
| Td.1 | 표시 변환 한 줄. | `Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs` |
| Td.2 | 상태 외형 `UnitStatusVisual_Stun`(effectKey 20, "기절", "이동·공격·스킬을 사용할 수 없습니다.")·표 등록. **Td.1과 한 커밋.** | 새 에셋 + `.meta`, `UnitStatusVisualDataTable.asset` |
| Td.3 | 거절 안내·짧은 글자(TP-10). | `Input/Assist/CombatActionNotice.cs`, `Display/Hud/CombatActionStateText.cs` |

Unity 확인: DB 생성 오류 없음. 기절 HUD 아이콘·설명·거절 안내는 스킬 단죄를 쓸 수 있게 된 뒤 확인한다(지금은 거는 길이 없음).

## 6. Phase PD-e — 문서·공유·묶음 끝 검토

- 폴더 배치 안내에 새 파일(`CombatProjectileContacts`·`CombatContactRelease`)과 "원거리 해제 = 대상마다 마지막 접촉" 규칙 한 줄.
- 설계 구현 메모, 이 계획 실행 기록.
- 공유: 다른 작업자 줄을 바꾸면 그때 정리한다(지금 예상 없음). Core 요청 없음.
- 묶음 끝 검토(새 에이전트 하나), 평가 아티팩트 적용 기록은 평가 목록 밖 기능이라 더하지 않는다.

## 7. 커밋 묶음(사용자)

1. PD-a 코드 + `TraitVisual_PiercingFalloff.asset`
2. PD-b 코드 + `TraitVisual_DeadHand.asset`(+ `.meta`, 표 한 줄)
3. PD-c 코드(인스펙터 포함) — 임시 클립 묶음·외형은 사용자 결정
4. PD-d 코드 + `UnitStatusVisual_Stun.asset`(+ `.meta`, 표 한 줄) — 한 커밋
5. 문서
6. 임시 설정(로드아웃·SoulEater 외형·클립 묶음)은 커밋하지 않는다(설계 §7).

## 8. 실행 기록

### 2026-11-16 PD-a·PD-b 코드 완료(컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- PD-a: 새 `Presentation/Attack/CombatProjectileContacts.cs`(접촉 목록·관통 소실점)·`CombatContactRelease.cs`(해제 규칙), `CombatAttackShape.PiercingShot`, `ICombatSourceLifetime.Release`(+ 공격 재생·조기 출처), 투사체 진행 `FireLinear` = 접촉 여러 개, 코드 모션 해제 입구 타입 `CombatContactRelease`(근접·곡사·포효는 `Finish`/`AllAtOnce`), 클립 발사·연발 집계 접촉 콜백 (대상, 마지막 접촉), 직선 코드 모션 접촉 목록, 궤적 소실점, `TraitVisual_PiercingFalloff` 문구. 검토 에이전트 결과 반영 전.
- PD-b: `CombatConcurrentGroups.AttachSweepMoves`(머리 뒤 공격 사건 전 죽음의 손 밀림 걸음 한 묶음, 묶음 번호 `_nextGroup` 공유), `CombatKnockbackPreview` 휩쓸기 출처(`TryKindOf`·`StepOf`), `TraitDescriptionBindings` `{피해계수}`, `TraitVisual_DeadHand` 토큰 문구·`rankValueFormat`.
### 2026-11-16 PD-c·PD-d 코드 완료(컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기)

- PD-c: 시작 모드 `FromSweepPresentationStart = 2`(기준 칸 안 씀·지연 씀·공격 클립 필수, `UsesSweep`·`UsesPreAttackStart`), 인스펙터 휩쓸기 갈래, 소속 대응 `CombatPreAttackTraitMap.TryGetSweepBinding`(머리 사건과 공격 결과의 같은 `Sweep` 참조, 결합 값 `IsSweep` = 발동 null), 시작 알림 `CombatPreAttackPresentationStarts.StepStarted`(사건 Presenter `BeginStep`이 부름, 걸음 판정은 `CombatConcurrentGroups.IsSweepStep` 하나 공유), 임시 클립 묶음 `CombatUnitAnimation_SoulEater.asset`(+ `.meta`, Boomber 복사, 모드 2·지연 0)을 임시 외형이 가리킴. 계획과 다른 점: 결합 값 구조는 그대로 두고 발동 null을 휩쓸기로 씀(새 형식 없음).
- PD-d: 표시 변환 한 줄(`StatusEffectKey.Stun`), 새 `UnitStatusVisual_Stun.asset`(+ `.meta`, effectKey 20)·표 한 줄(코드와 한 커밋), 거절 안내 "기절 상태라 행동할 수 없습니다."·짧은 글자 "기절".
- PD-e(일부): 폴더 배치 안내에 새 파일 둘과 원거리 피격 해제 규칙 줄.
### 2026-11-16 PD-b~d 검토(새 에이전트 하나) 반영 — 컴파일 오류 없음(정적 대조)

- 높음 H1: 기절은 출처가 스킬(`EffectOwnerKind.PlayerSkill`)이라 HUD 상태 설명(`CombatStatusDescriptionFormatter.Origin`)이 "표시할 수 없는 출처 종류"로 멈춤 → 출처 갈래 "스킬 {키}"(스킬 이름 외형은 스킬 View 작업에서), 상태 문구 "기절".
- 중간 M1(잠복): 같은 결과에서 앞 공격 출처가 열려 있으면 휩쓸기 걸음이 그 출처의 이동 후속으로 붙어 휩쓸기 묶음에 못 들어감 → `CombatHitFollowUps.Begin`이 휩쓸기 머리에서 출처를 끊음.
- 낮음 반영: L1 걸음 시작 알림의 세대를 시작 전에 잡음, L2 결합 값 `IsSweep`을 만들 때 명시(기본값 거짓), L3 낡은 주석·툴팁(넉백 미리보기·동시 묶음·선행 시작 어댑터·클립 SO 툴팁 둘).
- 그대로: L4 들어간 칸 0인 중단 대상에도 밀림 화살표(넉백과 같은 규칙), L5 머리 사건 "표시 계약 없음" 경고 한 번(의도된 건너뜀, TP-1).
- 커밋 묶음(L6): `TraitVisual_DeadHand.asset`(+ `.meta`, 표)과 `TraitDescriptionBindings`, `UnitStatusVisual_Stun.asset`(+ `.meta`, 표)과 `CombatUnitStatusFactory`, `CombatUnitAnimation_SoulEater.asset`(+ `.meta`)과 `UnitVisual_SoulEater`(임시, 커밋 여부 사용자) — 각각 한 커밋.

### 2026-11-16 PD-a 검토(새 에이전트 하나) 반영

- 높음 1(컴파일 오류): 진입 연출의 출처 수명 구현 `CombatEntryRiseAppearance.EntrySources`에 새 멤버 `Release`가 없었음 → 다른 멤버처럼 예외 한 줄.
- 낮음 반영: L2 연발 집계가 표시 콜백 안 재진입 때 같은 항목을 두 번 처리할 수 있음 → `_draining` 깃발로 중첩 호출은 쌓기만, L3 해제기가 아무것도 풀지 않은 접촉에서 신호를 내지 않게, L4 `CombatHitMoment.ReleaseThenSignal`이 해제기 `AllAtOnce`를 부르게(규칙 한 곳), L6 코드 모션 `Compose` 인자 순서를 `ComposeRanged`와 같게(대상 해제 → 모두 해제), 낡은 주석(공격 종류 전략·조각 신호·코드 모션 입구·몸 조회 실패).
- 그대로: L5 비관통 공격에서 `Profile.Hit` 표지가 두 번 남을 수 있음(측정 수치만), L7 새 `.meta`는 Unity가 다시 씀(import 뒤 커밋 권장).
- 기존 동작 대조(검토): 단발 직선·Akimbo 연발·곡사·근접·포효·조기 공격·대신 맞기 모두 HEAD와 같은 순서, 관통 조우 대상은 Gate 히트 대상.

### 2026-11-16 평가 아티팩트 적용 기록 16차 초안(아티팩트에 아직 못 넣음)

- 사용자 "평가 아티팩트 적용 기록에 이번 차수 더해줘". 이 Claude Code 계정에서는 평가 아티팩트(https://claude.ai/artifact/5WNzcEZ1sPSaFw5ennuoaY)를 열 수 없어(읽기 404, 내 목록·공유 목록에 없음) 넣지 못했다. 아티팩트를 가진 계정으로 열리면 아래를 '적용 기록' 절과 `applied-data`에 그대로 옮긴다. 차수 번호는 CLAUDE.md의 마지막 기록(15차, 소리 프리셋·곡사 수명) 다음으로 셌다 — 아티팩트에 그 뒤 차수가 있으면 번호만 고친다.
- 주제: 관통·죽음의 손·기절 View 대응(PD-a~PD-d). 재평가 문제를 고치려던 작업이 아니라 새 Core 기능 대응이다(평가 목록 밖).
- 바꾼 것: 원거리 피격 해제를 "대상마다 그 대상을 맞히는 투사체의 마지막 접촉"으로 통일(새 `Presentation/Attack/CombatProjectileContacts`·`CombatContactRelease`, 출처 수명 `Release(source, target)`), 관통 탄 조우마다 접촉 이펙트·피격과 Endpoint 소실·궤적, 죽음의 손 휩쓸기 동시 묶음·화살표·`{피해계수}`·시작 모드 `FromSweepPresentationStart`, 기절 표시 변환·외형·안내·출처 "스킬". 바꾼 파일 목록은 §2~§5·위 실행 기록.
- 재평가 문제 대조(2026-11-11 문제 목록, 바뀐 파일 기준): **풀린 문제 없음.** C2-26(`CombatAttackPlayback`이 수명 외에 접촉 시각 계산·진단·접촉 이펙트까지 맡음)은 접촉 목록 모으기(`CollectContacts`)가 더해져 그대로(조금 커짐), C1-43(투사체 쏘는 종류 판정 두 곳) 그대로, 조기 공격 묶음의 낮음들(소속 맵 `TryGetAttack`의 포효 포함 등) 그대로.
- 기준별 점수 변화: **없음**(요청 7기준·12기준 그대로, 중간 수 그대로 — 같은 감점표에서 다 풀린 문제가 없음).
- 새로 생긴 낮음(기록만, 점수에 넣지 않음): 비관통 공격에서 `Profile.Hit` 표지가 두 번 남을 수 있음(PD-a L5), 들어간 칸 0인 중단 대상에도 밀림 화살표(PD-b~d L4), 휩쓸기 머리 사건 "표시 계약 없음" 경고 한 번(PD-b~d L5).
- 남은 단계: 컴파일·Unity 확인(관통·죽음의 손은 확인용 로드아웃 필요, 설계 §7), SoulEater 새 공격 모션이 오면 외형·클립 묶음 재작업(사용자).
