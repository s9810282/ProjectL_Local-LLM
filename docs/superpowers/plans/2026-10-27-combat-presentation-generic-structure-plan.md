# 전투 연출 범용 구조 Task 계획 (G-0~G-f)

> - **2026-10-27 작성. 코드 착수 전.** 설계는 [generic-structure-design](../specs/2026-10-27-combat-presentation-generic-structure-design.md)(G1~G8)이다.
> - **결정:** 사용자 "Q-G1~Q-G10 모두 A로 하고 Task 계획 써줘".
> - **계획에서 새로 정한 것(P1~P18)은 §0.2에 있다.** 설계와 다른 것이 넷(P5·P8·P9·P11)이라 착수 전에 확인을 받는다. 다른 사람 시험 하나는 Q-T1로 묻는다.
> - **2026-10-27 Ballista 걷기(P4·Gb.5·Gb.7)는 G-b보다 먼저 따로 끝냈다**(사용자 "Ballista도 mortar로 바뀌었는데 쓸데없이 구분돼 있지 않나, 정리해줘"). 기록은 §13. G-b에는 공격 묶음 나누기만 남는다. §9의 Ballista 시험 행(`CombatClipShotTests`·`CombatUnitAnimationSettingsTests`의 Ballista 사례)도 끝났다.
> - **2026-10-27 재검토**(사용자 "이거저거 많이 추가하고 변경하고 했는데 지금 작업이랑 관련 있는지 확인 후에 다시 검토해줘"): 계획 뒤 커밋 22개(`a4b2603b..1f8ad75b`)를 이 계획의 파일과 대조했다. 고친 곳은 §0.3·§0.4(사실 9~13)·P4·P9·P14·Q-T1·Gb.8·Gc.6·Gd.6·§9·§10·§11·§12다. 기록은 §13.2에 있다.
> - **진행 규칙**
>   - Phase마다 멈추고 보고한다. 다음 Phase는 사용자 Unity 확인 뒤에 시작한다.
>   - MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다. 보고에는 "컴파일 미확인"을 적는다.
>   - 우리 시험은 확인하는 내용을 그대로 두고 호출만 맞춘다. 걷는 칸·기능을 보던 우리 사례는 지운다(10-22 Q1). 다른 사람 시험·줄은 고치기 전에 묻는다.
>   - 커밋·push는 사용자가 한다.

## 0. 착수 전

### 0.1 줄임말

| 줄임 | 경로 |
|---|---|
| `DV/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/` |
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` |
| `VDB/` | `Assets/ProjectL/DataBaseSO/ViewGroup/` |
| `T/` | `Assets/Tests/EditMode/Framework/` |

### 0.2 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 설계와 |
|---|---|---|
| P1 | 공격 값 객체(`CombatAttackPresentationData`)는 지금처럼 평평한 값으로 둔다. 부품 SO에서 채우고, 종류는 투사체 타입에서 나온다. 그래서 소비자(전략·애니메이션·궤적)는 그대로다. 코드 모션 부품이 없으면 `HasCodeMotion`이 false이고, 코드 모션 길·정면 모션은 시작하지 않는다(포효는 지금처럼 박치기 없이 커졌다 작아짐) | 같음(G4 구체화) |
| P2 | 창구 계약 셋(`AttackSettingsOf`·`ReactionSettingsOf`·`AnimationSettingsOf`)은 그대로 둔다. 뒤에서 외형 키마다 값 하나(`CombatUnitPresentationValues`)를 보관한다(표시 준비 때 읽고, `Configure`가 비움). 시험 가짜 넷은 고치지 않는다 | 같음(G8 구체화) |
| P3 | 부품 에셋은 값이 같은 것끼리 하나로 묶는다. 투사체 4(직선 총알 1, 곡사 3 — 프리팹이 다름), 코드 모션 4(근접 1, 쏘기 1, 곡사 2 — Ballista 묶음만 발사 시간 0.5). 공격 묶음 5개가 모두 자기 옛 값과 같은 부품을 참조한다. 새 에셋 폴더는 `VDB/CombatPresentation/Projectile/`·`CodeMotion/`이다 | 같음 |
| P4 | Ballista를 걷는 범위 — **걷음:** 열거 값 `Ballista = 4`, `CombatBallistaStyle`, 표 한 줄, 규칙 갈래, 공격 재생의 `BallistaContactProgress`, 전략 맥락의 `FireThrough`·`BallistaContactProgress`. **남김:** `BallistaWeaponView`(빈 컴포넌트, 프리팹이 씀), 투사체 진행 안의 `FireThrough`(직선·곡사도 씀) | 같음(Q-G9). **끝남**(§13.1) |
| P5 | **`Contact` 시점에는 이펙트만 둔다.** 관통 길을 걷으면 투사체 접촉은 언제나 맞음(`Hit`)과 같은 순간이고 한 번뿐이다. 그래서 흔들림·소리는 `Hit`에 두면 같다. 접촉 입구(공격 재생)는 지금처럼 이펙트 창구만 쓰고 카메라·소리를 받지 않는다 | **다름** — 설계 G2·G6은 접촉에 이펙트·흔들림·소리를 허용 |
| P6 | 행위자 지점 자리(`ActorEffectPoint`·`ActorMuzzle`)는 공격 클립이 있는 유닛만 쓴다. 지금처럼 클립 대여에서 읽는다. 코드 모션 유닛은 `ActorBody`를 쓴다. DB 검사가 막는다 | 같음(G6의 "계획에서 정함") |
| P7 | 새 조각 신호 `MotionCue = 5`(코드 모션의 타격·발사 순간). 클립의 `ClipEvent`와 다른 사실이라 값을 나눈다. 기준 시점 `Cue`는 뼈대가 내는 쪽에 대응한다 | 같음(G3 구체화) |
| P8 | 공격 재생의 코드 모션 입구 둘(`TryBegin`·`TryBeginFacingMotion`)을, Gate를 열지 않는 입구 하나(`TryBeginMotion(행위자, 공격 사건 또는 null, …)`)로 합친다. Gate는 조립 실행이 연다(포효와 같음). `BeginAnimation`의 Gate 갈래는 걷는다 | **다름** — 설계 G5는 입구를 합치는지 정하지 않음 |
| P9 | 추락 사망(다른 세션의 임시 연출, 커밋 `24002ca0`)은 사망 칸 큐를 쓰지 않는다. 호진님 토네이도 날림(`TryBeginTornado`, `1ab12e55`)도 같다(Q-G8 A의 환경 사건, 나중에 같은 형식으로). 지금처럼 소리를 시작 순간에 내고, 흔들림·이펙트는 넣지 않는다(맞은 것이 아니라 떨어진 것). `ComposeFall`은 고치지 않는다 | **다름** — 설계에 없던 경로 |
| P10 | 공격 묶음을 종류별로 만드는 시험 도우미 하나(`T/CombatPresentationTestAssets.cs`, `[Test]` 아님)를 둔다. 공격 SO 칸을 직접 넣던 시험의 준비 줄을 이 도우미 호출로 바꾸고, 단언은 그대로 둔다 | 같음 |
| P11 | 걷는 칸을 보던 우리 시험 사례를 지운다. 대상은 공격 SO `kind` 검사, Ballista 사례, 설정 표의 피격·사망 흔들림 사례, 클립 묶음의 소리·이펙트 칸 사례다. 새 위치(부품 SO·큐)의 검사는 새 시험 없이 DB 생성(부트)과 Unity 확인으로 본다 | **다름** — 설계는 "호출만 맞춤"까지만 적음 |
| P12 | 천명님 `CoreIntegration/PullAndMarkPresentationTests.cs`의 `EnableAttack`(공격 SO에 `kind`·`projectilePrefab`을 넣음)은 G-b에서 실행이 깨진다. 고치기 전에 묻는다(Q-T1) | 같음 |
| P13 | 자산 시험 둘(`CombatMortarAssetTests`·`CombatStandardAttackAssetTests`)의 경로가 이미 틀려 있다(`CombatPresentation/CombatAttack_*.asset` — 폴더 이동 전부터). G-b에서 `AttackSetting/` 경로로 고친다(우리 시험, 호출 맞춤) | 새로 찾음 |
| P14 | 검토는 G-d 뒤에 한 번이다. G-a~G-d 전체를 묶음 끝에 정적 검토 에이전트로 보고, 반영 뒤에는 다시 돌리지 않는다(10-27 사용자 지시 "검토는 모든 작업 끝나고 마지막에 일괄적으로"). 처음 판의 "Phase마다 하나, G-c는 둘"을 바꿨다. G-c의 동작 동일 대조는 Gc.6 확인 목록을 구현 중에 코드로 따라가 기록한다 | – |
| P15 | 소리 시점은 공격·사망 칸에 늘 있다("없음" 선택 없음). 기본은 공격 `Cue` + 0, 사망 `Start` + 0이다. 낼지는 지금처럼 프리셋(-1 = 없음)이 정한다 | 같음(Q-G7 구체화) |
| P16 | 플래시는 피격 칸의 큐 하나이고 시간 0이면 없다(지금 규칙). 시점은 `Start`·`Cue`만 받는다 | 같음 |
| P17 | 수 규칙 원본 `DV/CombatPresentationValueRules.cs`를 연출 SO 셋·부품 SO·설정 표가 부른다. 설정 표의 `Validate*` 함수 이름은 그대로 두고 안만 규칙을 부른다(추락 값 등이 그 이름을 씀) | 같음(G8) |
| P18 | View 쪽 큐 붙이기 이름은 `CombatCueComposer`다. 설계의 가칭 `CombatPresentationCues`는 DB 쪽 큐 형식 이름과 헷갈려서 바꿨다. 기준 시점 표는 `CombatCueAnchors`, 자리 해석은 `CombatCuePoints`다 | 이름만 |

- **Q-T1(묻기):** 천명님 `PullAndMarkPresentationTests`를 고쳐도 될까요?
  - 대상은 G-b의 한 곳이다. `EnableAttack`(`:378`·`:379`)이 공격 SO에 `kind`·`projectilePrefab`을 넣는 준비 줄을 시험 도우미(P10) 호출로 바꾼다.
  - 재검토로 바로잡음: 처음 판은 G-c 한 곳도 적었다. 이 시험이 부르는 공격 재생 입구는 `FireShot`(`:365`)뿐이고, G-c는 `TryBegin`·`TryBeginFacingMotion`만 합친다. 그래서 G-c에서 이 시험은 바꾸지 않는다.
  - 단언은 그대로 두고, 기록은 Core 전달 문서 §5에 남긴다.
  - **권장: 허락.** 허락하지 않으면 G-b 뒤 이 시험은 준비 단계에서 실패한다.

### 0.3 착수 조건 (G-0)

- **재검토(10-27) 결과, 처음 판의 착수 조건은 모두 풀렸다.**
  - 낭떠러지 추락 연출은 `24002ca0`, 넉백 미리보기는 `6bcc4c91`, 설계·계획 문서는 `1eed25ca`로 커밋됐다.
  - 지금 작업 트리에 남은 변경은 FMOD 캐시와 `C/Display/Hud/CombatHud.cs`뿐이다. 둘 다 이 계획의 파일이 아니다.
- **기준 판은 지금 HEAD `1f8ad75b`다**(G-f 측정의 B 판). 착수 때 HEAD가 더 나아가 있으면 그 해시를 §13에 적는다.
- 넉백 미리보기(구현 끝)는 입력·궤적 표시를 고쳤다. 이 계획은 궤적 코드를 고치지 않으므로(P1) 겹치는 것이 없다.

**계획 뒤 같은 영역에 들어온 것 (재검토 대조):**

| 들어온 것 | 커밋 | 이 계획에 닿는 곳 | 처리 |
|---|---|---|---|
| Ballista 종류 걷기 | `1eed25ca`(우리) | G-b의 P4·Gb.5·Gb.7, §9 Ballista 행 | 끝남(§13.1). 표에서 지움 |
| 투사체 완급 곡선 `projectileEasing` | `1eed25ca`(우리) | Gb.1·Gb.8. 공격 SO에 칸이 하나 늘었다 | 이미 Gb.1·Gb.8에 반영돼 있다. 곡선을 바꾼 에셋은 Ballista 하나라 그 부품에 옮긴다(Gb.8 표) |
| 낭떠러지 추락 연출 | `24002ca0`(우리) | 사망 처리기, 설정 표, 값 묶음(P9) | 착수 조건이 풀렸다. 추락은 큐를 쓰지 않는다(P9) |
| 토네이도 날림·외형 | `1ab12e55`(호진님) | 캐릭터 처리기(`TryBeginTornado`, 생성자 끝 선택 인자 둘), 사건 Presenter의 그 생성 줄, 실행 목록의 다섯째 재생기(외형), 새 조각 `CombatTornadoLiftPiece`, 지형 외형 표의 `TornadoPresentationSettings` | 범위 밖(Q-G8 A, 환경 사건). 그 줄은 고치지 않는다. Gd.6에서 생성 줄을 바꾸면 호진님 공유 메모(Gc.6·Gd.6·§12) |
| 약한 지반 붕괴 | `2805717b`(우리) | 사실 처리기, 동시 묶음 — 이 계획의 파일이 아님 | 영향 없음 |
| 넉백 미리보기 | `6bcc4c91`(우리) | 입력·궤적 표시·층 표 — 이 계획의 파일이 아님 | 영향 없음 |
| 지형 이웃 모양·스테이지 편집 도구 SO 모드·배치 구역 요청·Core(이동 후 공격·아군 넉백·맵 가중치) | 여러 | 연출 SO·처리기·공격 재생을 읽거나 고치지 않음(편집 도구는 외형 표의 세 칸을 읽지 않음) | 영향 없음 |

### 0.4 계획 중 확인한 사실

1. **코드 모션의 소리 순간**
   - 근접은 타격 순간이다(소리를 먼저 내고 그다음 해제).
   - 직선·곡사는 발사 순간이다.
   - 둘 다 `Cue` 기본값과 같은 순간이다.
2. **클립 묶음 9개의 `soundDelay`·`attackEffect`·`effectDelay`는 모두 0·Empty다.** 옮길 값이 없고 칸만 걷는다.
3. **옮길 값**
   - 공격 묶음 `impactEffect`: 곡사 셋(`CombatAttack_Ballista`·`_CactusCannon`·`_UndeadCannon`)은 Boom(2), 직선·근접은 없음(0).
   - 반응 에셋: `hitEffect`는 Hit(4), 플래시는 빨강·0.18초.
   - 설정 표 흔들림: 피격 0.15·0.15·Bump(2), 사망 0.3·0.3·Explosion(3).
4. **코드 모션을 실제로 쓰는 공격**
   - 공격 클립이 없는 셋: CactusCannon(곡사), Shielder(근접), Sniper(직선).
   - 보스 포효의 정면 모션: StageBoss1(근접).
   - 나머지 열 유닛의 공격은 클립 길이다.
5. **Ballista 종류 값을 쓰는 에셋은 없었다** → 10-27에 걷었다(§13.1).
   - Core의 관통(`PiercingShot`)은 강화 "정밀 사격"으로만 나오는데, 지금 운영에서는 얻을 수 없다.
   - 직선 유닛이 관통 결과를 받으면 지금도 직선 연출로 보인다.
6. **공격 묶음 에셋 넷의 YAML에 옛 Ballista 칸 넷**(`stringTravel` 등)이 남아 있다. `CombatAttack_Ballista`는 완급 곡선을 넣을 때 다시 저장돼 없다(재검토 확인). 이행 때 남은 넷에서 함께 걷는다.
7. **시험 가짜**
   - `ICombatUnitTargets` 가짜는 넷이다(`CombatAttackTrajectoryTests`·`CombatPieceRunTests`·`CombatUnitHudFeedbackTests`·`CombatUnitPresentationWindowTests`). P2로 고치지 않는다.
   - 공격 전략 맥락·처리기를 직접 만드는 시험 가짜는 없다.
8. 공격 SO 칸을 이름으로 넣는 시험은 `DataBaseTestAssets.Set(obj, "칸", 값)`을 쓴다. 칸이 사라지면 컴파일은 되고, 준비 단계에서 실패한다.

재검토(10-27)에서 더한 사실:

9. **완급 곡선을 바꾼 공격 묶음은 `CombatAttack_Ballista` 하나다.** 시작 기울기 0·끝 기울기 2로, 천천히 출발해 빨라진다. 나머지 넷은 키가 없어 등속이다. Ballista는 투사체 부품도 따로라(Gb.8 표), 부품을 더 나눌 일은 없다.
10. **공격 쪽 구조는 계획 때와 같다.**
    - 공격 재생의 코드 모션 입구 둘(`TryBegin`·`TryBeginFacingMotion`)과 `BeginAnimation`의 Gate 갈래가 그대로다.
    - 접촉 입구 `ImpactThen`(전략 맥락)·`PlayImpact`(공격 재생 private)도 그대로다.
    - 바뀐 점 둘: 전략 맥락에서 `PlayImpact`·`ReleaseAll` 등 다섯 멤버가 빠졌고, `TryBeginCodeMotion`이 `abstract`가 됐다. 클립 공격 처리기는 `ReleaseAll`을 직접 부른다.
11. **캐릭터 처리기에 길이 둘 늘었다.**
    - 순서는 토네이도 날림(호진님, 맨 앞) → 사망(추락이면 추락 조립) → 피격이다.
    - 토네이도 날림은 자세 대여 + 날림 조각이고, 소리는 실행 완료 콜백에서 코드로 낸다.
    - 생성자 끝에 선택 인자 둘(`tornadoLiftTarget = null`·`hideTornado = null`)이 있다. "운영 입구 필수"(View 2라운드 V2-g) 규칙과 다르지만 호진님 줄이라 보고만 한다.
12. **실행 목록에 다섯째 재생기 `CombatTornadoAppearancePlayer`(호진님)가 생겼다.** `CombatPresentationPlayers.Create`에 `appearance` 인자가 붙었다. G-c는 실행 목록을 고치지 않지만, 순서 확인(Gc.6)에 넣는다.
13. **연출 값이 있는 곳이 하나 늘었다.**
    - 토네이도 날림 값은 지형 외형 표의 `TornadoPresentationSettings`(호진님)에 있다.
    - 추락 값은 설정 표의 "낭떠러지 추락 연출 (임시)"에 있다.
    - 둘 다 환경 사건이라 범위 밖이다(Q-G8 A). 나중에 같은 형식(사건 묶음 + 큐)으로 옮길 후보로 Ge.2에 적는다.

## 1. Phase 한눈에

| Phase | 내용 | 동작 | 예상 누적(알려진 문제 기준) |
|---|---|---|---|
| G-0 | 기준 판 확인(이미 커밋됨) | – | 7.6(Ballista 걷기로 L6 풀림) |
| G-a | 수 규칙·값 객체·읽기 보관·피격 클립 검사 | 같음 | 7.9 |
| G-b | 공격 묶음 나누기(투사체·코드 모션 부품, 종류 파생, 완급 곡선을 투사체 부품으로) | 같음 | 8.1 |
| G-c | 공격 실행 장치 하나(코드 모션을 조립으로) | 같음 | 8.3 |
| G-d | 큐·기준 시점·자리, 옛 이름 칸 이행, 흔들림 이동 | 같음 | 8.6 |
| G-e | 문서·공유 | – | 8.6 |
| G-f | (원하면) 블라인드 A/B 측정, 기준 판 = G-0 | – | – |

## 2. Phase G-0 — 기준 판

- **G0.1** 커밋할 것은 없다(§0.3). 이번 재검토로 바뀐 계획·설계 문서는 G-a 코드와 함께 커밋해도 된다.
- **G0.2** 착수 때 HEAD 해시를 §13에 적는다(재검토 때 `1f8ad75b`).

## 3. Phase G-a — 값 규칙·값 객체·읽기 보관 (에셋 변경 없음)

| Task | 할 일 | 파일 |
|---|---|---|
| Ga.1 | 수 규칙 원본(P17): 유한·0 이상·양수 판정과 문구 | 새 `DV/CombatPresentationValueRules.cs`(+ `.meta`) |
| Ga.2 | 각자 갖던 수 검사를 규칙으로 바꾼다. 공격 SO의 `RequirePositive/NonNegative/Finite`, 반응 SO의 `Require`, 클립 공격 값의 `IsValidDelay`, 설정 표의 `Validate*` 안(이름 그대로) | `DV/CombatAttackPresentationSO.cs`, `DV/CombatUnitReactionPresentationSO.cs`, `DV/CombatClipAttackData.cs`, `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` |
| Ga.3 | 반응 값 객체를 정리한다. 생성자가 스스로 검사하고 기본값 인자 셋(`hitFlashColor`·`hitFlashSeconds`·`hitEffect`)을 지운다. SO는 생성자를 부르고 실패 문구에 칸·에셋 이름을 붙인다. 공격 값 객체는 G-b에서 다시 쓰므로 여기서는 고치지 않는다 | `DV/CombatUnitReactionPresentationData.cs`, 반응 SO |
| Ga.4 | 읽기 보관(P2): `FieldUnitPresenter._animationSettings`를 외형 키 → `CombatUnitPresentationValues`(공격·반응·클립 셋)로 바꾼다. `Rent`에서 읽고 `Configure`가 비운다. 창구 계약 셋은 보관한 값을 돌려준다 | 새 `C/Display/Unit/CombatUnitPresentationValues.cs`(+ `.meta`), `C/Display/Unit/FieldUnitPresenter.cs` |
| Ga.5 | 피격 클립 검사(L1): 피격 클립이 있는데 반응 묶음이 없으면 부트에서 거절한다. `ValidateUnit`에 반응 유무를 넘긴다. 지금 에셋에는 걸리는 것이 없다 | `DV/CombatUnitAnimationRules.cs`, `DataBaseSystem/Data/View/Table/Unit/Item/UnitVisualDataSO.cs`(호진님 표 파일, 한 줄) |
| Ga.6 | 시험: 반응 값 생성자 호출 맞춤(`T/CombatUnitPresentationWindowTests.cs`), 문구를 비교하는 시험이 있으면 호출만 맞춤. §13 기록 | – |

- **완료 조건:** 동작 같음. 공격·반응 값을 공격 사건·궤적 갱신마다 새로 만들지 않는다.

## 4. Phase G-b — 공격 묶음 나누기 (Q-G4·Q-G9)

| Task | 할 일 | 파일 |
|---|---|---|
| Gb.1 | 투사체 부품 SO: 추상 기반(프리팹, 종류, 완급 곡선 `projectileEasing`)과 직선(속도·높이)·곡사(비행 시간·곡사 높이·회전). 각자 수 검사를 한다. 곡선 규칙은 지금처럼 `CombatProjectileEasing.TryFrom` 하나를 부른다(투사체 완급 설계 §4, 2026-10-27 추가) | 새 `DV/CombatProjectileSO.cs`·`CombatStraightProjectileSO.cs`·`CombatArcProjectileSO.cs`(+ `.meta`) |
| Gb.2 | 코드 모션 부품 SO: 추상 기반과 근접(시간·각 둘·당김·내밂)·쏘기(반동 둘·발사 시간·발사 비율)·곡사(쏘기 값 + 준비·복귀·버킷 각) | 새 `DV/CombatCodeMotionSO.cs`·`CombatMeleeMotionSO.cs`·`CombatShootMotionSO.cs`·`CombatMortarMotionSO.cs`(+ `.meta`) |
| Gb.3 | 공격 묶음 SO를 바꾼다. `kind`·투사체 값·모션 값 칸을 걷고 `projectile`·`codeMotion` 참조 칸을 둔다. `impactEffect`는 G-d까지 남긴다. 검사는 둘이다. 코드 모션 종류가 전달과 맞아야 한다(없음 ↔ 근접, 직선 ↔ 쏘기, 곡사 ↔ 곡사). 근접이면 `impactEffect`는 Empty만 받는다(지금 규칙). `CreateData`는 종류를 정해 평평한 값을 만든다(P1) | `DV/CombatAttackPresentationSO.cs` |
| Gb.4 | 공격 값 객체: 생성자가 종류·투사체 값·모션 값(`HasCodeMotion`)·접촉 이펙트를 받아 스스로 검사한다. 속성은 `internal`이다 | `DV/CombatAttackPresentationData.cs` |
| Gb.5 | ~~규칙: Ballista 필수 갈래·`FiresProjectile`·`CanUseAttackClip`의 Ballista 줄 지우기~~ **끝남(§13.1).** 남은 일: `FiresProjectile`을 "투사체 있음"으로 바꾼다(종류 파생, P1) | `DV/CombatUnitAnimationRules.cs` |
| Gb.6 | View: 코드 모션 전략·정면 모션은 `HasCodeMotion`이 false면 시작하지 않는다(P1) | `C/Presentation/Attack/CombatAttackStyle.cs`·`CombatMortarStyle.cs`·`CombatAttackPlayback.cs` |
| Gb.7 | ~~Ballista 걷기(P4)~~ **끝남(§13.1).** | – |
| Gb.8 | 에셋(P3): 부품 에셋 8개를 만든다(+ `.meta`, 새 GUID, 폴더 `.meta`는 CC-a 선례대로). 공격 묶음 5개의 YAML을 옮긴다(옛 칸 → 부품 참조, 옛 Ballista 칸 넷 걷기). 옮긴 뒤 값을 대응표로 대조한다 | `VDB/CombatPresentation/Projectile/`·`CodeMotion/`, `VDB/CombatPresentation/AttackSetting/*.asset` 5 |
| Gb.9 | 시험: 도우미(P10) 새로 만들기, 준비 줄 맞춤(§9 G-b 행), 사례 지우기(P11), 자산 시험 경로(P13), 천명님 시험(Q-T1 답 뒤) | 새 `T/CombatPresentationTestAssets.cs`(+ `.meta`) |
| Gb.10 | §13 기록 | – |

**에셋 대응표 (Gb.8):**

| 공격 묶음 | 투사체 부품 | 코드 모션 부품 |
|---|---|---|
| `CombatAttack_Melee` | 없음 | `CombatCodeMotion_Melee`(0.48·22·55·0.08·0.25) |
| `CombatAttack_Linear` | `CombatProjectile_Bullet`(직선, 프리팹 `e612c677…`, 20·0.6) | `CombatCodeMotion_Shoot`(0.1·7·0.42·0.3) |
| `CombatAttack_CactusCannon` | `CombatProjectile_CactusCannon`(곡사, `46ae341f…`, 0.8·2·(60,30,15)) | `CombatCodeMotion_Mortar`(0.04·2·0.42·0.3·0.15·0.25·-20) |
| `CombatAttack_UndeadCannon` | `CombatProjectile_UndeadCannon`(곡사, `eff5d1d6…`, 같은 값) | `CombatCodeMotion_Mortar` |
| `CombatAttack_Ballista` | `CombatProjectile_Ballista`(곡사, `48189758…`, 같은 값, **완급 곡선 그대로 옮김** — 키 (0,0 기울기 0)·(1,1 기울기 2)) | `CombatCodeMotion_MortarSlow`(위와 같고 발사 시간 0.5) |

- `CombatAttack_Ballista`의 `projectileSpeed 30`은 곡사가 쓰지 않는 값이라 옮기지 않는다.
- 완급 곡선 `projectileEasing`(2026-10-27 추가)은 공격 묶음에서 그 묶음의 투사체 부품으로 옮긴다. 에셋 YAML에 키가 없거나 기본 직선이면 부품도 기본값(등속)이다. 기획자가 곡선을 바꾼 묶음이 있으면, 같은 투사체 부품을 쓰는 묶음끼리 곡선이 같은지 먼저 대조한다. 다르면 부품을 나눈다. 재검토(10-27) 때 곡선을 바꾼 묶음은 Ballista 하나였다(§0.4 9번). 착수 때 다시 센다.
- **완료 조건:** 동작 같음, DB 생성 성공, 공격 SO 인스펙터에 종류 칸이 없음.

## 5. Phase G-c — 공격 실행 장치 하나 (Q-G5)

| Task | 할 일 | 파일 |
|---|---|---|
| Gc.1 | 신호 `MotionCue = 5`와 문서(P7) | `C/Presentation/Pieces/CombatPieceStart.cs` |
| Gc.2 | 코드 모션 조각: 외부 진행 조각이다. 입구 `(end, cue, hit) → bool`, 시작하지 못하면 바로 끝난다(`CombatFacingAttackPiece`와 같은 꼴). 정면 공격 조각은 이 조각으로 합칠지 그 자리에서 판단해 기록한다 | 새 `C/Presentation/Pieces/CombatCodeMotionPiece.cs`(+ `.meta`) |
| Gc.3 | 공격 재생 입구 하나(P8): `TryBeginMotion(행위자, 공격 사건 또는 null, mapper, cue, hit, end, version)`. 전략 `TryBeginCodeMotion`은 `playAudio` 자리에 `cue` 콜백을 받는다. `BeginAnimation`의 Gate 갈래를 지우고, 전략 맥락 계약도 맞춘다 | `C/Presentation/Attack/CombatAttackPlayback.cs`·`CombatAttackStyle.cs`·`CombatMortarStyle.cs`·`ICombatAttackStyleContext.cs` |
| Gc.4 | 코드 모션 공격 조립: 행동 실행 + 출처 완료(공격, 행위자, 첫 해제). 항목은 0 코드 모션 조각, 1 사건 소리(`After(0, MotionCue)`, 지금 소리 순간)다. G-d에서 소리 항목은 큐가 된다 | 새 `C/Presentation/Pieces/CombatCodeMotionAttack.cs`(+ `.meta`) |
| Gc.5 | 처리기: 공격 처리기는 클립 없는 성공 공격도 조립으로 돌린다(실패 공격·이동만 공격은 지금처럼). 포효 처리기는 정면 입구 이름만 맞춘다 | `C/Playback/Handlers/CombatAttackEventHandler.cs`·`CombatRoarEventHandler.cs` |
| Gc.6 | **확인할 것**(코드로, 기록에 남김) | – |
| Gc.7 | 시험: 계획 때 `TryBegin`·`TryBeginFacingMotion`을 직접 부르는 시험은 없었다(재검토 확인). 착수 때 `grep`으로 다시 보고, 있으면 호출만 맞춘다 | – |
| Gc.8 | §13 기록 | – |

Gc.6에서 확인할 것:

- 신호로 시작하는 항목이 신호를 낸 호출 안에서 시작하는가(`CombatPieceRun`). 근접의 "소리 → 해제" 순서가 그대로여야 한다.
- 출처 Gate가 열리는 프레임이 지금(애니메이션 시작)과 같은가.
- 투사체 진행(`TickShotsOnly`)·실행 목록 순서가 같은가. 실행 목록에는 호진님 외형 재생기(`appearance`, 토네이도)가 다섯째로 붙어 있다. 코드 모션 공격이 조각 실행 재생기로 옮겨 가도 공격 재생기·외형 재생기와의 막는 순서가 같은지 본다.
- 겹침 표 O1~O20 중 공격 행(같은 프레임 시작 출처 겹침, 첫 해제 뒤 다음 사건)이 같은가.
- 시작 실패 정리(자세 되돌림·출처 비우기)를 실행이 맡는가.

**완료 조건:** 동작 같음. 클립·코드 모션 공격이 모두 조각 실행으로 돈다.

## 6. Phase G-d — 큐·기준 시점·자리 (Q-G2·Q-G3·Q-G6·Q-G7)

| Task | 할 일 | 파일 |
|---|---|---|
| Gd.1 | DB 쪽 큐 형식(순수 값): 열거 `CombatCueAnchor`·`CombatCuePoint`, 직렬화 큐(이펙트·흔들림·소리 시점·플래시), 사건 칸 묶음(공격·피격·사망)과 불변 값. 파일 나눔은 지금 관례를 따른다(작은 형식은 한 파일에 여럿) | 새 `DV/CombatCueAnchor.cs`·`CombatCuePoint.cs`·`CombatCueEntries.cs`·`CombatEventCues.cs`(+ `.meta`) |
| Gd.2 | 큐 규칙: 사건·전달별 허용 시점·자리(설계 G3 표 + P5·P6·P16), `Contact` 지연 0, 플래시 하나, 수 규칙 | 새 `DV/CombatPresentationCueRules.cs`(+ `.meta`) |
| Gd.3 | SO·값 칸 | 공격 SO·값, 반응 SO·값, `DV/CombatUnitAnimationSO.cs`·`CombatClipAttackData.cs` |
| Gd.4 | 유닛 검사: 행위자 지점 자리는 공격 클립 + 그 지점 필요(P6). 피격 `Cue`는 피격 클립 필요 | `DV/CombatUnitAnimationRules.cs`(또는 새 유닛 규칙 입구), 외형 표 `Validate` 한 줄 |
| Gd.5 | View 큐 붙이기(P18). 기준 시점 표는 시점 → (항목, 신호) 대응이고, 열거 누락은 표를 만들 때 실패시킨다. 큐 붙이기는 큐 하나를 항목 하나로 만들고 지금 조각을 그대로 쓴다. 자리 해석은 클립 대여의 지점, 보인 사실의 몸 중심 + 모델 방향 / 맞은 방향(`CombatHitDirection`)이다 | 새 `C/Presentation/Pieces/CombatCueAnchors.cs`·`CombatCueComposer.cs`·`CombatCuePoints.cs`(+ `.meta`) |
| Gd.6 | 조립을 바꾼다. 클립 공격 둘, 코드 모션 공격, 피격, 사망 — 기준 시점 표를 채우고 큐 붙이기를 한 줄 부른다. 처리기의 `HitEffectOf`와 흔들림·플래시 `if`를 지운다. `ComposeFall`과 호진님 `TryBeginTornado`는 그대로다(P9). 처리기 생성자에서 인자를 빼게 되면(예: `cameraMotion`), 사건 Presenter의 생성 줄(호진님이 토네이도 인자 둘을 붙인 줄)을 함께 고치므로 호진님 공유 메모에 적는다 | `C/Presentation/Pieces/CombatClipAttack.cs`·`CombatCodeMotionAttack.cs`, `C/Playback/Handlers/CombatCharacterEventHandler.cs`·`CombatAttackEventHandler.cs` |
| Gd.7 | 접촉 큐(P5): 공격 재생의 `PlayImpact`가 공격 값의 `Contact` 이펙트 큐를 낸다(창구는 그대로 이펙트만) | `C/Presentation/Attack/CombatAttackPlayback.cs`·`ICombatAttackStyleContext.cs` |
| Gd.8 | 흔들림 이동(G7): 카메라 연출 값에서 피격·사망 흔들림을 지운다. 설정 SO의 칸 여섯·검사·속성도 지운다. 처리기·조립은 반응 큐의 흔들림을 쓴다 | `C/Playback/Support/CombatCameraMotionSettings.cs`, 설정 SO |
| Gd.9 | 에셋 이행: 아래 표대로 옮긴 뒤 YAML 값을 대조한다 | 공격 5·반응 1·클립 9·설정 1 |
| Gd.10 | 시험: §9 G-d 행 | – |
| Gd.11 | **묶음 끝 검토 하나(P14)**: G-a~G-d 전체를 정적 검토 에이전트 하나로 7기준 점검, 반영, 그 뒤에는 다시 돌리지 않음. §13 기록 | – |

**에셋 이행 (Gd.9):**

| 에셋 | 옮기는 것 |
|---|---|
| 공격 곡사 셋 | `impactEffect` Boom → 큐 이펙트(`Contact`·자리 `Contact`·지연 0). 소리 시점 `Cue` + 0 |
| 공격 직선·근접 | 이펙트 큐 없음. 소리 시점 `Cue` + 0 |
| `CombatUnitReaction_Default` | 피격 칸: 플래시(빨강·0.18·`Start`), 이펙트 Hit(`Start`·`SubjectBody`), 흔들림(0.15·0.15·Bump·`Start`). 사망 칸: 이펙트 Hit(`Start`·`SubjectBody`), 흔들림(0.3·0.3·Explosion·`Start`), 소리 시점 `Start` + 0 |
| 클립 묶음 9 | `soundDelay`·`attackEffect`·`effectDelay` 키 걷음(값 모두 0·Empty) |
| 설정 표 기본 | `hitShake*`·`deathShake*` 키 여섯 걷음 |

- 클립 공격의 소리는 지금 `After(클립, ClipEvent, soundDelay)`이고, 9개 모두 0이라 `Cue` + 0과 같다.
- **완료 조건:** 동작 같음(같은 순간·자리). 처리기에 "값이 있으면 항목" 갈래가 없다.

## 7. Phase G-e — 문서·공유

- **Ge.1** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md)에 새 파일 줄과 규칙 하나를 더한다. 규칙: "연출 덧붙임은 큐 데이터 — 처리기는 뼈대와 기준 시점만".
- **Ge.2** 설계에 구현 메모(계획과 다른 점)를 적는다. 범위 밖 다음 후보도 적는다: 토네이도 날림(`TornadoPresentationSettings`, 소리를 완료 콜백에서 코드로 냄)·추락(설정 표 임시 값)·포효·소환·기차를 같은 형식(사건 묶음 + 큐)으로.
- **Ge.3** 공유 메모(모두 미전달)
  - 호진님: 외형 표 칸 셋의 뜻, 부품 SO 메뉴, 클립 묶음에서 걷은 칸, `Validate` 줄, (바뀌었으면) 사건 Presenter의 캐릭터 처리기 생성 줄.
  - 현강님: 소리 시점이 데이터로 바뀜(Audio 로직·프리셋은 그대로).
  - 천명님: 시험 고침 기록(Q-T1), Core 변경 없음(Core 전달 문서 §5).
  - 민형님: Factory 변경이 있었으면 그 줄.
- **Ge.4** CLAUDE.md 현재 상태와 메모리를 갱신한다.

## 8. Phase G-f — 측정 (선택)

블라인드 A/B로 측정한다. 범위는 연출 SO·부품 SO·큐·조립·처리기·공격 재생이다. B 판은 G-0 커밋이고, 하위 에이전트 셋, 7기준이다.

## 9. 시험 영향

천명님 `PullAndMarkPresentationTests`(Q-T1)와 호진님 토네이도 시험(아래 표 끝, 바꿀 것 없음 예상) 말고는 모두 우리(Droute)가 처음 만든 파일이다. 천명님(GwonCheonmyeong)·현강님(Twinsgm)·민형님(JMH)이 손댄 줄이 있는 파일이 있어서, 고칠 줄은 그 Phase에서 `git blame`으로 보고 다른 사람 줄이면 묻는다. Ballista 사례 행은 §13.1에서 끝나 표에서 뺐다.

| Phase | 시험 | 바꿀 것 | 종류 |
|---|---|---|---|
| G-a | `CombatUnitPresentationWindowTests` | 반응 값 생성자(기본값 인자 삭제) | 호출 맞춤 |
| G-b | `CombatAttackPresentationSettingsTests` | 종류별 검사 사례 → 부품 SO 검사로, `kind` 열거 사례 지움 | 호출 맞춤 + 사례 지움(P11) |
| G-b | `CombatAttackTrajectoryTests`·`CombatClipAttackSceneTests`·`CombatCodeMotionHitTests`·`CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatUnitAnimationSettingsTests` | 공격 SO 준비 줄 → 도우미 | 호출 맞춤 |
| G-b | `CombatClipShotTests` | 준비 줄 → 도우미 | 호출 맞춤 |
| G-b | `CombatMortarAssetTests`·`CombatStandardAttackAssetTests` | 경로(P13)와 값 읽는 자리(부품) | 호출 맞춤 |
| G-b | `CombatBallistaAssetTests` | 값 객체로 읽으면 그대로. 칸을 직접 읽으면 부품으로 | 호출 맞춤 |
| G-b | 천명님 `PullAndMarkPresentationTests` | `EnableAttack` 준비 줄 → 도우미(`FireShot` 호출 `:365`은 그대로) | **Q-T1** |
| G-c | 없음(재검토 때 `TryBegin`·`TryBeginFacingMotion`을 직접 부르는 시험 0) | – | 착수 때 다시 `grep` |
| G-d | `CombatHitCompositionTests`·`CombatDeathCompositionTests`·`CombatClipAttackCompositionTests` | 조립 인자(큐) | 호출 맞춤 |
| G-d | `CombatClipAttackSceneTests`·`CombatUnitAnimationSettingsTests` | `soundDelay`·`attackEffect`·`effectDelay` 칸·검사 사례 | 호출 맞춤(큐로) / 사례 지움 |
| G-d | `CombatUnitPresentationWindowTests` | 클립 공격 값 생성자 인자 | 호출 맞춤 |
| G-d | `CombatHitEffectSceneTests`·`CombatUnitReactionAssetTests` | `hitEffect`·플래시 칸 → 반응 큐 | 호출 맞춤 |
| G-d | `CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatAttackPresentationSettingsTests` | `impactEffect` 칸 → 공격 큐 | 호출 맞춤 |
| G-d | `CombatPresentationSettingsDataTests` | 피격·사망 흔들림 사례·준비 줄 지움 | 사례 지움(P11) |
| G-d | 호진님 `TornadoAppearanceSchedulingTests` | 사건 Presenter를 `CombatViewSystemTests.EventValues`·`CombatCameraMotionSettings.None`으로 만든다. 이 계획은 두 입구와 `None`을 그대로 두므로 바꿀 것이 없을 것이다 | 바뀌면 **묻는다** |

## 10. Unity 확인 (사용자, Phase마다)

| 공통 | 내용 |
|---|---|
| 1 | 컴파일 오류 없음 |
| 2 | 그 Phase가 바꾼 시험 파일 묶음 + §9의 파일 실행 |
| 3 | Play From Boot로 Combat 진입. DB 생성이 새 검사를 통과해야 한다 |

| Phase | 재생에서 볼 것 |
|---|---|
| G-a | 공격·피격·사망이 지금과 같음 |
| G-b | 공격 SO 인스펙터(종류 칸 없음·부품 참조), 근접 클립(Demon 등)·직선 클립(ShotGun 등)·곡사 클립(Ballista·Cannon) 공격, 코드 모션 셋(CactusCannon·Shielder·Sniper), 보스 포효 박치기, 공격 궤적 미리보기 |
| G-c | G-b와 같은 목록. 특히 코드 모션 셋의 소리·맞음 순간, 그 뒤 다음 사건이 이어지는 때 |
| G-d | 곡사 착탄 Boom, 피격 빨강 플래시·Hit 이펙트·흔들림, 사망 Hit 이펙트·흔들림·소리, 추락 사망(낭떠러지·약한 지반 `Test` 스테이지 칸 (6,3)), 토네이도 날림(호진님, 같은 처리기 파일이라 그대로인지) |

## 11. 커밋 묶음

- **G-a:** 새 파일 둘(+ `.meta`)과 바꾼 파일.
- **G-b:** 아래는 **한 커밋**이다(에셋이 없으면 값이 빠지거나 DB 생성이 실패한다).
  - 부품 SO 코드 일곱(+ `.meta`)과 공격 묶음 SO
  - 부품 에셋 8(+ `.meta`, 폴더 `.meta` 둘)과 공격 묶음 에셋 5
  - 시험 도우미(+ `.meta`)(`CombatBallistaStyle.cs` 삭제는 §13.1 커밋 `1eed25ca`로 끝남)
- **G-c:** 새 파일 둘(+ `.meta`).
- **G-d:** DB 쪽 새 파일 다섯·View 쪽 셋(+ `.meta`)과 에셋(공격 5·반응 1·클립 9·설정 1)은 **한 커밋**이다.

## 12. 위험

| 위험 | 대응 |
|---|---|
| G-c에서 Gate 순서·투사체 진행·겹침이 달라짐 | Gc.6 확인 목록을 구현 중에 코드로 따라가 기록, 묶음 끝 검토(P14), Unity 재생. 포효 정면 공격이 이미 같은 꼴로 돈다 |
| 에셋 이행 중 값이 빠짐 | 대응표(§4·§6)대로 옮긴 뒤 YAML 대조 |
| DB 검사가 강해져 부트 실패 | 이행 Phase에서 지금 에셋이 새 검사를 통과하는지 대조 |
| 시험 준비 줄을 바꾸다 단언 뜻이 바뀜 | 도우미는 옛 기본값 그대로 만든다. 단언 줄은 손대지 않는다 |
| 같은 처리기 파일에 다른 작업자 줄(호진님 토네이도 `TryBeginTornado`·생성자 끝 선택 인자 둘, 사건 Presenter의 생성 줄) | 그 줄은 고치지 않는다. 생성 줄을 함께 고치게 되면 호진님 공유 메모에 적는다. 추락 연출과의 겹침은 커밋 `24002ca0`으로 풀렸다 |
| 다른 작업자 파일(`UnitVisualDataSO` 호진님 표) | 줄 수를 최소로, 공유 메모 |
| SO 타입이 늘어 입력이 번거로움 | 부품은 공유한다(투사체 넷·코드 모션 넷). 큐는 묶음 안 목록 |

## 13. 실행 기록

(Phase마다 여기에 적는다.)

### 13.1 Ballista 걷기 먼저 (2026-10-27, Unity 컴파일 문제 없음 — 사용자, 시험·재생 미확인, 새 시험 없음)

- **첫 컴파일 오류(사용자 보고, 고침):** `CombatAttackPlayback.cs(36,73)` CS0737. 맥락 인터페이스에서 `PlayImpact`를 빼먹었다(명시 구현만 지움). 클래스의 `private PlayImpact`가 남아 인터페이스를 구현하지 못했다. 인터페이스 줄을 지운 뒤 다시 컴파일했고, 사용자가 오류 없음을 확인했다.

- **요청:** 사용자 "CombatAttackPresentation 정리 좀 해줄래? 발리스타도 mortar로 바뀌었는데 쓸 데 없이 구분되어 있는 거 아닌가?"
  - Ballista 외형은 2026-10-26부터 곡사(`kind` 3)다. 종류 값 4를 쓰는 에셋은 0개라, 운영에서 Ballista 길에 닿는 곳이 없었다(§0.4 5번).
  - G-b의 Ballista 걷기(P4·Gb.5·Gb.7)만 먼저 했다. 공격 묶음 나누기(Gb.1~Gb.4·Gb.6·Gb.8)는 그대로 G-b에 남는다. 에셋은 고치지 않았다.
- **지운 것**
  - 열거 값 `Ballista = 4`(값 4는 다시 쓰지 않는다고 주석에 적음).
  - `C/Presentation/Attack/CombatBallistaStyle.cs`(+ `.meta`), 전략 표 한 줄.
  - 규칙 `CombatUnitAnimationRules`: Ballista면 공격 클립 필수 갈래, `FiresProjectile`의 Ballista 줄.
  - 전략 맥락 `ICombatAttackStyleContext`: `FireThrough`·`BallistaContactProgress`·`Release`·`HitAll`·`PlayImpact`. 다섯 모두 Ballista 전략만 불렀다. 공격 재생의 명시 구현과 `BallistaContactProgress`도 지웠다.
- **P4 밖에서 함께 걷은 것** — 모두 Ballista만 쓰던 구분이다.
  - 목표점 함수 `TryTarget`·`ResolveTarget`의 `piercingEndpoint` 인자. 관통 종점을 읽는 쪽이 Ballista 전략뿐이었고, 다른 호출은 모두 `null`을 넘겼다. 궤적 미리보기의 `preview.PiercingShot?.Endpoint` 인자도 지웠다.
  - 전략의 클립 해제 입구 `ClipRelease`. Ballista만 덮어썼다. 처리기는 이제 종류와 관계없이 `_attacks.ReleaseAll(attack, version)`을 쓴다. 옛 기본 구현과 같은 동작이다.
  - `TryBeginCodeMotion` 기본 구현(`=> false`)을 `abstract`로 바꿨다. 남은 종류 셋이 모두 덮어써서 기본 구현에 닿는 곳이 없었다.
  - 공격 재생 `ContactProgress`의 "몸 조회 실패" 갈래(몸 인자가 null 허용이었음). 지금 부르는 곳은 직선 하나뿐이고, 직선은 몸이 없으면 이 함수 전에 종점 도착(1)을 돌려준다.
  - 투사체 진행 `FireThrough`는 남겼다(P4). 다만 `FireLinear`만 부르므로 `private`으로 줄였다.
- **남긴 것:** `BallistaWeaponView`(빈 컴포넌트, 프리팹이 씀). Core 관통(`PiercingShot`)을 읽는 `CombatHitFollowUps`도 남겼다. 이것은 종류가 아니라 Core 사실이고, 직선 유닛이 관통 결과를 받으면 지금처럼 직선으로 보인다.
- **시험(모두 우리 것, 걷은 기능 사례 지움 + 호출 맞춤)**
  - `CombatAttackPresentationSettingsTests`: `TestCase(4)` 행 다섯을 지웠고, 투사체 속도 조건의 Ballista 항을 지웠다.
  - `CombatClipShotTests`: Ballista 사례 둘을 지웠다. 거절 시험에서 Ballista 줄을 빼고 이름을 `RejectsKindsThatDoNotFire_AndAMortarWithoutAPrimaryCell`로 바꿨다.
  - `CombatShotPathTests`: Ballista 단언을 지웠다. `TryTarget` 호출 셋에서 인자를 맞췄다.
  - `CombatUnitAnimationSettingsTests`: Ballista `TestCase` 행 셋과 `FiresProjectile(Ballista)` 단언을 지웠다.
  - `CombatAttackFacingTests`: `TryTarget` 호출 하나를 맞췄다.
  - `CombatBallistaAttackTests`: 낡은 주석 한 줄을 고쳤다.
- **문서:** 폴더 배치 안내의 종류 수(넷 → 셋)와 예를 고쳤다. 공유는 없다(바꾼 줄이 모두 우리 것).
- **확인(사용자, Unity):** 컴파일, 위 시험 여섯, Ballista·대포(곡사 클립)·ShotGun 등(직선 클립)·CactusCannon·Sniper(코드 모션) 공격 재생, 공격 궤적 미리보기.
- **커밋:** 지운 `CombatBallistaStyle.cs`(+ `.meta`)와 바꾼 코드·시험은 한 커밋이다(열거 값이 없어지면 그 파일이 컴파일되지 않음).

### 13.2 재검토 — 계획 뒤 바뀐 것 대조 (2026-10-27, 코드·에셋 변경 없음)

- **요청:** 사용자 "이거저거 많이 추가하고 변경하고 했는데 지금 작업이랑 관련 있는 지 확인 후에 다시 검토해줘".
- **범위:** 계획을 쓴 HEAD `a4b2603b` 뒤 커밋 22개(`..1f8ad75b`)와 작업 트리를 이 계획이 고칠 파일(§3~§6)·시험(§9)·에셋과 대조했다.
- **관련 있음(계획을 고침):**
  - Ballista 걷기 끝남 → P4·Gb.5·Gb.7·§9·§11.
  - 완급 곡선이 공격 SO에 들어옴 → Gb.8 표에 Ballista 곡선 옮기기, §0.4 9번.
  - 추락 연출 커밋 → §0.3 착수 조건 풀림, P9.
  - 호진님 토네이도가 같은 처리기·사건 Presenter·실행 목록에 들어옴 → P9·Gc.6·Gd.6·§9·§10·§12, §0.4 11~13번.
- **관련 없음(그대로):** 약한 지반, 넉백 미리보기, 지형 이웃 모양, 스테이지 편집 도구 SO 모드(외형 표의 세 칸을 읽지 않음), 배치 구역 요청, Core 커밋 셋, 작업 트리의 `CombatHud.cs` 한 줄.
- **처음 판을 바로잡은 것:**
  - Q-T1의 G-c 대상은 없다. 천명님 시험은 `FireShot`만 부르고, G-c는 그 입구를 바꾸지 않는다.
  - P14는 메모리 규칙("검토는 마지막에 한 번", 10-27 사용자 지시)과 달랐다. G-d 뒤 한 번으로 바꿨고, Phase마다 있던 검토 Task 넷을 걷었다.
- **다시 확인한 사실(그대로):**
  - 외형 표 16개의 세 칸 연결.
  - 클립 묶음 9개의 소리·이펙트 칸이 0·Empty.
  - 반응·설정 에셋의 옮길 값(설정 표에는 추락 값 셋만 늘었다).
  - 코드 모션 소리 순간.
  - 시험 가짜 `ICombatUnitTargets` 넷.
  - 자산 시험 둘의 틀린 경로(P13).
- **새로 보고만 한 것:** 호진님 캐릭터 처리기 생성자의 선택 인자 둘(`= null`)은 "운영 입구 필수" 규칙(V2-g)과 다르다. 우리 `CombatFactEventHandler`의 `showTerrainChange = null`(약한 지반)도 같은 꼴이다. 이 계획의 범위가 아니라 고치지 않는다.

### 13.3 G-a 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

- **지시:** 사용자 "G-a 부터 쭉 진행해". Phase마다 멈추지 않고 G-d까지 잇고, 검토는 G-d 뒤 한 번(P14)이다. 착수 HEAD는 `1f8ad75b`(재검토 때와 같음).
- **Ga.1** 새 `DV/CombatPresentationValueRules.cs`(+ `.meta`): `IsFinite`·`IsNonNegative`·`IsPositive`와 문구 셋.
- **Ga.2** 수 검사를 규칙으로 바꿨다.
  - 공격 SO의 `RequirePositive`·`RequireNonNegative`·`RequireFinite`는 몸만 규칙을 부른다(G-b에서 부품 SO로 옮김).
  - 클립 공격 값 `IsValidDelay`는 `IsNonNegative`를 부른다. 이름과 `DelayRule`은 남겼다(클립 SO·우리 시험 둘이 씀).
  - 설정 표의 `Validate*` 열하나는 이름·문구를 그대로 두고 판정만 규칙으로 바꿨다. `ValidateFallEndScale`의 "1 이하"는 그 함수에 남는다.
- **Ga.3** 반응 값 생성자가 스스로 검사한다(순서는 옛 SO 검사와 같음). 기본값 인자 셋을 지웠고, 각도 상한은 상수 `MaxHitAngle`이다. SO는 생성자를 부르고, `ArgumentOutOfRangeException`을 받으면 칸 이름·에셋 이름을 붙여 `InvalidOperationException`으로 다시 던진다(우리 자산 시험이 이 형식을 봄).
- **Ga.4** 새 `C/Display/Unit/CombatUnitPresentationValues.cs`(+ `.meta`). `FieldUnitPresenter`는 `_animationSettings` 대신 외형 키 → 값 셋을 보관한다(처음 읽을 때 셋을 함께, `Rent`에서 미리, `Configure`가 비움). 창구 계약 셋은 그 값을 돌려준다. 그래서 공격 사건·궤적 갱신마다 SO에서 새 값을 만들지 않는다. 시험 가짜 넷은 그대로다.
- **Ga.5** `CombatUnitAnimationRules.ValidateUnit`에 넷째 인자 `hasReaction`. 피격 클립이 있는데 반응 묶음이 없으면 부트에서 거절한다. 외형 표 `Validate`(호진님 표 파일, 그 줄은 우리 `e0ee626d`) 한 줄. 반응 묶음이 없는 외형 셋(본부·화약통·보급품)은 클립 묶음도 없어 걸리지 않는다.
- **Ga.6** 우리 `CombatUnitPresentationWindowTests:343` 생성자 호출만 맞췄다(뒤 인자 셋을 옛 기본값으로). 문구를 비교하는 시험은 없다(반응 자산 시험은 예외 형식만 봄).

### 13.4 G-b 코드·에셋 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

- **Gb.1·Gb.2 부품 SO 일곱**(`DV/`, + `.meta`): 투사체 `CombatProjectileSO`(추상: 프리팹·완급 곡선, 종류 = 타입) · `CombatStraightProjectileSO`(속도·높이) · `CombatArcProjectileSO`(비행 시간·곡사 높이·회전), 코드 모션 `CombatCodeMotionSO`(추상: 맞는 종류) · `CombatMeleeMotionSO` · `CombatShootMotionSO`(봉인 안 함) · `CombatMortarMotionSO`(쏘기 값을 이어받음 + 준비·복귀·버킷).
  - 칸 이름은 옛 공격 SO 칸 이름 그대로다. 그래서 YAML 이행과 시험 도우미가 이름으로 옮긴다.
  - **계획과 다른 점(P-b1):** 계획은 "부품 SO가 각자 수 검사"였다. 수 조건의 원본은 공격 값 객체 생성자 하나로 두었다(G8 "생성자가 스스로 검사", SSOT). 부품은 칸만 내준다. 완급 곡선만 Unity 형식이라 투사체 부품이 값으로 바꾸고(`ReadEasing`), 잘못되면 부품 이름으로 멈춘다. 실패 문구에는 묶음·투사체·모션 에셋 이름이 붙는다.
- **Gb.3** 공격 묶음 SO의 칸은 `projectile`·`impactEffect`·`codeMotion` 셋이다. `CreateData`가 종류를 투사체에서 정하고(없으면 근접), 코드 모션 종류가 맞는지 보고, 부품 타입별 값을 한 곳에서 꺼낸다(그 종류에 없는 칸은 0).
- **Gb.4** 공격 값 객체가 스스로 검사한다(옛 SO `Validate` 그대로, 모션 값은 `hasCodeMotion`일 때만). 속성은 `internal`, 새 `HasCodeMotion`. 수 검사 throw 도우미(`RequirePositive` 등)는 수 규칙 파일로 옮겨 반응 값과 함께 쓴다(같은 도우미가 두 값 객체에 있던 것을 하나로).
- **Gb.5** `FiresProjectile`은 그대로 두고 문서에 "= 투사체 부품 있음"을 적었다(종류가 투사체에서 나와 뜻이 같음). 시험 셋이 이 함수를 그대로 부른다.
- **Gb.6** 공격 재생의 코드 모션 입구 둘이 `HasCodeMotion`이 false면 시작하지 않는다(한 곳씩). 전략 파일은 고치지 않았다.
- **Gb.8 에셋:** 폴더 `VDB/CombatPresentation/Projectile/`·`CodeMotion/`(+ 폴더 `.meta`), 부품 8개(+ `.meta`). 공격 묶음 5개 YAML을 부품 참조 셋으로 바꿨다(옛 칸·옛 Ballista 칸 넷 걷음). 대응표 §4 그대로이고, Ballista 완급 곡선 키 둘을 `CombatProjectile_Ballista`에 그대로 옮겼다. 나머지 투사체 부품은 곡선 키를 쓰지 않아 기본(등속)이다. 옮긴 값은 옛 YAML과 칸마다 대조했다.
- **Gb.9 시험**
  - 새 도우미 `T/CombatPresentationTestAssets.cs`(+ `.meta`, `[Test]` 아님): `Attack(assets, kind, prefab)`(옛 기본값 = 직선), `Set(attack, 옛 칸 이름, 값)`(그 칸을 가진 곳에 넣고, 지금 부품에 없는 칸은 기억했다가 종류가 바뀌면 넣음), `Reload(units)`, `Destroy`.
  - 준비 줄을 도우미로 바꾼 우리 시험 여덟: `CombatAttackTrajectoryTests`·`CombatClipAttackSceneTests`·`CombatClipShotTests`·`CombatCodeMotionHitTests`·`CombatMortarAnimationTests`·`CombatMortarPresentationTests`·`CombatStandardAttackTests`·`CombatUnitAnimationSettingsTests`. 단언은 그대로다.
  - `CombatAttackPresentationSettingsTests`: 준비 줄을 도우미로, `UnknownKind_RejectsExplicitSettings`(종류 열거 사례) 삭제(P11), 직렬화 왕복 시험은 곡사 투사체 부품도 실제 자산으로 만든다.
  - 자산 시험: 경로 둘을 `AttackSetting/`로(P13). `CombatStandardAttackAssetTests`는 그 종류의 모션 값만, `CombatBallistaAssetTests`는 옮기지 않은 직선 속도·높이 단언을 걷었다(P11). **보고:** `CombatStandardAttackAssetTests`의 직선 투사체 프리팹 GUID 기대값 `6f0973fc…`는 지금 에셋(`e612c677…`)과 다르다. 경로가 틀려 이 사례가 실제로 돌지 않던 동안 에셋이 바뀐 것으로 보인다. 기대값은 고치지 않았다.
- **G-a 값 보관의 시험 영향(계획 §9에 없던 것):** 유닛을 그린 뒤 SO 값을 바꾸는 우리 시험은 보관된 옛 값을 읽게 된다. 바꾼 줄 뒤에 `CombatPresentationTestAssets.Reload(_units)` 한 줄을 넣었다: `CombatStandardAttackTests`(셋 + `Kind`), `CombatMortarPresentationTests`(넷), `CombatCodeMotionHitTests`(일곱 + `Kind`), `CombatHitEffectSceneTests`·`CombatUnitReactionTests`·`CombatRailwayContactSceneTests`(하나씩). 확인하는 내용은 그대로다. 같은 까닭으로, **플레이 중 에디터에서 공격·반응 SO 값을 바꾸면 다음 전투 진입(`Configure`)부터 보인다**(옛: 공격·반응은 다음 사건부터, 클립 묶음은 원래 다음 진입부터).
- **Ga.5의 시험 영향:** `CombatUnitAnimationSettingsTests`의 외형 도우미가 반응 묶음을 붙인다(피격 클립 사례가 새 검사에 먼저 걸리지 않게).
- **천명님 `PullAndMarkPresentationTests`는 고치지 않았다(Q-T1 답 대기).** 이 시험은 G-b 뒤 준비 단계(`Set(_attackSettings, "kind", …)`, 칸 없음)에서 실패한다. 묻는 범위에 둘을 더한다: `EnableAttack`이 외형의 공격 묶음을 그린 뒤 바꾸면 G-a 값 보관 때문에 `Reload`가 필요할 수 있다.

### 13.5 G-c 코드 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **Gc.1** 신호 `MotionCue = 5`(`CombatPieceStart.cs`, 문서에 "근접은 이 신호 뒤 같은 순간에 해제").
- **Gc.2** 새 `Pieces/CombatCodeMotionPiece.cs`(+ `.meta`): 입구 `(end, cue, hit) → bool`, `MotionCue`·`Hit`를 맥락 신호로 냄, 시작 못 하면 바로 끝. **정면 공격 조각(`CombatFacingAttackPiece`)은 합치지 않았다.** 합치면 포효 조립 입구의 형식과 그 조립 시험(`CombatAbilityCompositionTests`의 `Facing` 도우미)·신호 시험 넷이 바뀐다. 포효를 큐 형식으로 옮길 때(Ge.2 후보) 합친다.
- **Gc.3** 공격 재생: 옛 입구 둘(`TryBegin`·`TryBeginFacingMotion`)과 `HitAll`을 지우고 `HasCodeMotion(행위자, facing)`·`TryBeginMotion(행위자, 공격 사건 또는 null, mapper, cue, hit, end, version)`을 두었다. `BeginAnimation`은 Gate 갈래 없이 애니메이션 수명만 연다(맥락 계약도 `(animation, ended, version)`). 전략 넷은 `playAudio` 자리를 `cue`로 이름만 바꿨다.
  - 계획과 다른 점: 처리기가 조립 전에 물을 `HasCodeMotion`을 더했다. 시작할 모션이 없을 때(설정·코드 모션 부품 없음) 지금처럼 일반 사건(틀의 일반 소리)으로 이어 가기 위해서다. 조립을 만든 뒤 모션이 시작하지 않으면 소리가 건너뛰어져 옛 동작과 달라진다.
- **Gc.4** 새 `Pieces/CombatCodeMotionAttack.cs`(+ `.meta`): 행동 실행 + 출처 완료(공격, 공격자, 첫 해제). 항목 0 코드 모션(타격 = `ReleaseThenSignal(해제, Hit)`), 항목 1 사건 소리 `After(0, MotionCue)`. 자세는 모션이 스스로 빌려 실행 대여에 넣지 않는다(포효와 같음).
- **Gc.5** 공격 처리기: 실패 공격은 앞에서 false, 클립이 있으면 클립 조립, 없으면 `BeginCodeMotionAttack`(모션이 없으면 false). 포효 처리기의 정면 공격 줄(현강님 `9e48ce94`, "공격 재연결")은 새 입구로 바꾸고 해제를 `ReleaseThenSignal`로 묶었다 → 현강님 공유(G-e).
- **Gc.6 확인(코드로 따라감)**
  1. 신호로 시작하는 항목: `CombatPieceRun.Signal` → `Emit` → `Trigger` → 지연 0이면 `StartItem`이 같은 호출 안에서 시작한다. 근접 애니메이션은 `_impact()`(이제 `cue`) 뒤 `_hit`이라, 소리 조각이 소리를 낸 뒤 해제된다. 옛 "소리 → 해제"와 같다. 직선·곡사는 발사 순간 `cue`라 옛 효과음 순간과 같다.
  2. Gate 여는 때: 옛은 처리기 호출 안에서 `BeginAnimation`이 `_gate.Begin` → `_attack.Begin`. 새는 같은 호출 안에서 실행 시작이 `BeginSource` → 항목 0 → `TryBeginMotion` → `_attack.Begin`. 같은 프레임·같은 순서다. 자세 대여가 Gate 열기 뒤로 갔지만 대여에는 콜백이 없어 보이는 차이는 없다.
  3. 투사체 진행: 애니메이션은 그대로 공격 재생의 `_attack`에서 돌아, 공격 재생기 Tick의 `_flights.Tick` + `_attack.Tick`이 그대로다. 클립 발사만 있을 때의 `TickShotsOnly`도 그대로다. 실행 목록 순서는 공격 → 조각 실행 → 걸음 → 반응 → 외형(호진님)이다. 모션 끝은 공격 재생기 Tick 안에서 실행을 끝내고(`EndExternal` → `Finish` → `EndSource`), 그 뒤 조각 실행 재생기 Tick이 돈다. 외형 재생기와의 순서는 바뀌지 않았다.
  4. 겹침: 행동 실행 동안 조각 실행 재생기가 막고(출처가 겹침을 허용하면 엶), 공격 재생기도 `_attack.IsPlaying` + 같은 Gate로 막는다. 프레임 시작 겹침은 두 재생기가 같은 Gate 값(`AllowedAtFrameStart`·프레임 시작 `AllowsOverlap`)을 쓴다. 포효의 정면 공격과 클립 공격이 이미 이 길로 돈다.
  5. 시작 실패: 모션 시작 예외 → 실행의 `TryStart` catch → `Stop(abortSource)` → `AbortSource`(옛 `_gate.ClearIf`와 같음). 자세는 전략의 `StartWithPose`가 되돌린다(그대로). 곡사 무기 없음 시험(`MissingBucketOrWeapon…`)의 기대(예외·자세 없음·소리 없음)와 맞는다.
  6. 끝: 애니메이션 정상 완료 → `ended`(세대 확인) → 항목 끝 → `Finish`(대여 되돌림·재생기에서 빠짐) → `EndSource`. 옛 `_attack.Begin(…, () => EndSource(e, version))`과 같은 호출이다.
- **Gc.7** 옛 입구를 직접 부르는 시험은 없다(`grep` 확인). 그래서 시험은 바꾸지 않았다. 코드 모션 공격 장면 시험(`CombatStandardAttackTests`·`CombatCodeMotionHitTests`·`CombatMortarPresentationTests`)은 이제 조립 실행을 거치므로 Unity 실행으로 확인해야 한다.

### 13.6 G-d 코드·에셋 완료 (2026-10-27, 컴파일 미확인, 새 시험 없음)

- **Gd.1 DB 큐 형식**(`DV/`, + `.meta`): `CombatCueAnchor`(Start·Cue·Hit·Contact·End), `CombatCuePoint`(ActorEffectPoint·ActorMuzzle·ActorBody·SubjectBody·Contact), `CombatCueEntries`(입력 칸 넷 + 불변 값 넷: 이펙트·흔들림·소리 시점·플래시, 수 조건은 값 생성자), `CombatEventCues`(칸 하나의 큐 묶음 + 칸 입력 셋: 공격·피격·사망).
- **Gd.2 큐 규칙** `DV/CombatPresentationCueRules.cs`(+ `.meta`): 칸 `CombatCueEvent`(근접 공격·원거리 공격·피격·사망), 시점 표(설계 G3), 자리 표(접촉 시점 ↔ 접촉점), 접촉 시점은 이펙트만·지연 0(P5), 소리는 공격·사망 칸에 늘(P15), 플래시는 피격만·Start/Cue(P16), 유닛 조건 `FindUnitError`(행위자 지점 자리 = 공격 클립 + 그 지점, 피격 Cue = 피격 클립, P6).
- **Gd.3 SO·값**
  - 공격: `impactEffect` → 공격 칸 큐 `cues`. 값 객체가 큐 규칙을 봅니다(근접은 접촉 시점을 받지 않아 옛 "근접 접촉 이펙트는 Empty만"과 같음).
  - 반응: `hitFlashColor`·`hitFlashDuration`·`hitEffect` → 피격 칸 큐 `hitCues`·사망 칸 큐 `deathCues`. 값 객체는 `HitCues`·`DeathCues`를 갖고 플래시·이펙트 멤버를 지웠다.
  - 클립: `soundDelay`·`attackEffect`·`effectDelay` 칸·검사와 클립 공격 값의 소리·이펙트 멤버를 지웠다. `hitDelay`만 남는다(뼈대).
  - 입력 칸의 `ToCues`는 값만 만들고, 규칙은 값 객체 생성자가 한 번 본다(SSOT).
- **Gd.4 유닛 검사:** `ValidateUnit`이 반응 값(옛 Ga.5의 bool 자리)을 받고, 옛 "공격 칸 이펙트면 이펙트 지점 필수"를 큐 규칙 `FindUnitError`로 넓혔다(묶음이 없어도 본다).
- **Gd.5 View 큐 붙이기**(`C/Presentation/Pieces/`, + `.meta`): `CombatCueAnchors`(시점 → 뼈대 항목 신호, 열거 누락은 처음 쓸 때 실패), `CombatCuePoints`(자리 해석 한 곳: 클립 이펙트 지점·발사 지점, 행위자·주체 몸, 접촉점), `CombatCueComposer`(+ 창구 묶음 `CombatCueSinks`, 붙이는 순서 소리 → 플래시 → 흔들림 → 이펙트).
  - 계획에서 정함: 발사 지점 자리의 회전은 같은 실행이 빌린 자세 방향이다(클립 대여의 발사 지점이 위치만 줌). 이펙트 조각에 `AtClipMuzzle`을 더했다.
  - 흔들림 큐 → 카메라 효과 값 바꾸기는 `CombatCameraMotionSettings.Shake` 하나다(설정 값과 공유).
- **Gd.6 조립:** 클립 공격 둘·코드 모션 공격·피격·사망이 뼈대 → 기준 시점 표 → 큐 붙이기 한 줄이다. 처리기의 `HitEffectOf`와 흔들림·플래시 `if`를 지웠다. 캐릭터 처리기는 주체 몸 자리(`SubjectBodyOf`)만 구한다. 공격 처리기는 카메라 창구·보인 사실을 받아 행위자 몸 자리(`ActorBodyOf`, 쓰는 큐가 있을 때만)를 구한다. `ComposeFall`·`TryBeginTornado`는 그대로(P9).
  - **같은 순간 항목 순서 하나가 바뀌었다:** 피격 조립에서 옛 반응 → 플래시 → 클립이 반응 → 클립 → 플래시다(기준 순간 큐가 클립 항목을 가리켜야 해서 뼈대가 먼저). 같은 프레임이라 화면은 같다. 사망·클립 공격·코드 모션 공격의 순서는 옛 것과 같다.
  - 처리기 생성자: 캐릭터 처리기에서 `cameraMotion`을 뺐고, 공격 처리기에 `camera`·`facts`를 더했다. 사건 Presenter의 생성 줄 둘을 고쳤다(캐릭터 줄은 호진님 병합 줄 → 호진님 공유).
- **Gd.7 접촉 큐:** 공격 재생의 `PlayImpact`가 공격 칸 큐의 접촉 시점 이펙트를 접촉 자리에 낸다(창구는 그대로 이펙트만).
- **Gd.8 흔들림 이동:** 카메라 연출 값과 설정 SO에서 피격·사망 흔들림 칸 여섯·검사·속성을 지웠다.
- **Gd.9 에셋**(옛 YAML을 칸마다 대조): 공격 곡사 셋에 접촉 이펙트 Boom(Contact·Contact·0), 공격 다섯 모두 소리 시점 Cue + 0. 반응 기본: 피격 칸(이펙트 Hit·Start·SubjectBody, 흔들림 .15·.15·Bump·Start, 플래시 빨강·.18·Start), 사망 칸(이펙트 Hit·Start·SubjectBody, 흔들림 .3·.3·Explosion·Start, 소리 Start + 0). 클립 묶음 9의 옛 칸 셋, 설정 표의 흔들림 키 여섯을 걷었다. 옮긴 에셋 YAML은 LF로 맞췄다.
- **Gd.10 시험**(모두 우리 줄, 단언은 그대로 또는 새 자리의 같은 사실)
  - 시험 도우미에 큐 값 만들기(`AttackCues`·`HitCues`·`DeathCues`)와 옛 칸 이름 옮기기(공격 `impactEffect`·`attackEffect`·`effectDelay`·`soundDelay`, 반응 `SetReaction`의 `hitEffect`·플래시)를 더했다.
  - 조립 시험 셋(피격·사망·클립 공격)은 조립 인자를 큐로 맞췄다. 장면 시험 둘(클립 공격·피격 이펙트)은 옛 칸 이름을 도우미로 넣는다. `CombatUnitPresentationWindowTests`는 클립 공격 값 생성자 호출만(반응 값 생성자 줄을 놓침 → §13.7 H1에서 고침).
  - 걷은 칸을 보던 사례를 지웠다(P11): 클립 묶음의 소리 선딜레이 사례 셋·이펙트 사례 넷, 설정 표의 피격·사망 흔들림 사례·준비 줄·단언.
  - 칸 이름 문구를 보던 단언 둘은 새 자리 이름으로 바꿨다: 알 수 없는 접촉 이펙트 → `effect`, 근접 접촉 이펙트 → `Contact`·`Melee`(같은 사실을 새 자리에서 봄).
  - **G-a 때 놓친 것(여기서 함께 풀림):** `CombatHitCompositionTests`가 반응 값 생성자를 `new(…)`로 기본값 인자 없이 불렀다(`grep` 꼴이 달라 Ga.6에서 못 봄). G-d에서 이 상수가 큐 값으로 바뀌어 풀렸다.

### 13.7 G-d 뒤 묶음 끝 검토·반영 (2026-10-27, 컴파일 미확인, 새 시험 없음)

검토 에이전트 하나가 G-a~G-d 전체를 정적으로 봤다(P14, 반영 뒤 다시 돌리지 않음). 컴파일·Unity는 돌리지 않았다. 7기준 예상(H1 고친 뒤) SSOT 8.5 · SOLID 7.5 · FW 9.0 · 독립성 8.5 · 캡슐화 8.5 · C#/Unity 8.0 · If/Tick 8.0 = **약 8.3**(설계 예상 8.6). M3·L4·L5를 반영하면 약 8.7.

- **확인된 것(문제 없음):** 걷은 멤버의 남은 참조 0, 바뀐 서명과 호출 쪽, using·네임스페이스, 추상·override, 직렬화 칸 기본 생성자, `CombatCueAnchors` 정적 초기화 순서, 새 `.meta` GUID 22개 모두 하나뿐. YAML 칸 이름·중첩·정수값이 코드·옛 값과 같고, 공격 묶음 다섯·부품 여덟이 옛 값과 칸마다 같다. 외형 16개 모두 새 유닛 검사를 통과한다. G-c 동작 대조(Gc.6 1~6)가 코드로 맞다.
- **높음 H1(반영):** `CombatUnitPresentationWindowTests:343`의 반응 값 생성자가 G-a 서명(인자 여덟)으로 남아 시험 어셈블리가 컴파일되지 않았다. Ga.6에서 맞춘 줄을 G-d 생성자 변경 때 다시 보지 않았다(Gd.10의 "클립 공격 값 생성자 호출만"은 틀린 기록). 호출을 `(…, HitCues(), DeathCues())`로 맞췄다(우리 시험, 단언 그대로).
- **중간 M1(사용자 결정 대기, Q-T1):** 천명님 `PullAndMarkPresentationTests`는 컴파일되지만 `EnableAttack`의 `Set(_attackSettings, "kind", …)`(`:378`)가 없는 칸이라 두 시험(`:330`·`:349`)이 실행에서 실패한다. 고친다면: 시험 도우미 `CombatPresentationTestAssets.Attack`, 근접 코드 모션 부품(없으면 모션이 없음), `:387` 외형 바꾼 뒤 값 보관 비우기(`Reload`), `:365` `CreateData()`.
- **중간 M2(2026-10-27 사용자 "그대로 두고 기록만" — 시험·에셋 무변경, 이 단언은 실행에서 실패로 남음):** `CombatStandardAttackAssetTests:81-82`는 경로를 고쳐(P13) 이제 실제로 돈다. 기대 GUID `6f0973fc…`(`Bullet.prefab`)와 지금 직선 투사체 `e612c677…`가 달라 실패한다. 기대값을 맞출지(V3-e의 Ballista 값처럼)는 사용자가 정하기로 했고, 지금은 그대로 둔다.
- **중간 M3(다음 후보, 반영 안 함):** 공격 묶음 SO `CreateData`가 부품 값을 `as` 캐스트 다섯으로 꺼내고 값 객체 생성자가 평평한 인자 22개다. 새 투사체·모션 종류는 SO·값·전략을 함께 고친다(OCP). 부품이 자기 값을 채우거나 값 객체가 부품 값 묶음을 갖는 안 — P1(부품은 칸만, 수 조건은 값 생성자)과 맞바꿈이라 따로 정한다.
- **낮음 반영:** L7 `CombatEffectPiece`의 `At` 문서 주석이 `AtClipMuzzle` 위로 밀린 것을 제자리로, L9 공격 묶음 SO `CreateData`가 안쪽 예외를 넘김(반응 SO와 같게), L11 공격 처리기의 낡은 "공격 효과음 콜백" 주석, L8 직렬화 왕복 시험이 곡사 모션 부품도 저장(옛 단일 SO 때처럼 `FireTime`이 기본값 0.3끼리 비교되게, 우리 시험).
- **낮음 기록만:** L1 피격 조립 같은 순간 순서(Gd.6에 기록), L2 시작 실패 때 자세 복원 → `AbortSource` 순서(보이는 차이 없음), L3 모션을 시작하지 못하면 큐가 건너뛰어짐(`CombatCodeMotionAttack` 문서에 이미 적힘, 처리기가 `HasCodeMotion`으로 거름), L4 접촉 지식 네 곳(큐 규칙·`CombatCueAnchors.TryStart`·`CombatCuePoints.CreatePiece`·`PlayImpact`), L5 공격 한 번에 공격 연출 값을 네 번 찾음, L6 몸 자리 계산을 캐릭터 처리기는 늘·공격 처리기는 쓸 때만, L10 실행 중 없는 시점(피격 Cue인데 클립 없음 등)은 DB 검사에 기대고 조립에서 예외.

### 13.8 G-e 문서·공유 (2026-10-27, 코드 변경 없음, 공유는 모두 미전달)

- **Ge.1** [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md): 머리말, `Display/Unit/`·`Presentation/Pieces/` 줄에 새 파일, §2 규칙 "연출 덧붙임(이펙트·카메라 흔들림·소리 시점·플래시)은 큐 데이터 — 처리기는 뼈대와 기준 시점만".
- **Ge.2** 설계 §10 구현 메모: 수 조건은 값 생성자(부품 SO는 칸만), 큐 규칙 한 곳, 정면 공격 조각은 합치지 않음, `HasCodeMotion`, 발사 지점 자리 회전, 피격 같은 순간 순서, 값 보관의 부작용(시험·에디터 실시간 조정), 범위 밖 다음 후보(포효·소환·기차·토네이도·추락을 같은 형식으로, 검토 M3).
- **Ge.3** 공유
  - 호진님 [클립 메모](../handoffs/2026-10-19-unit-animation-clip-hojin-share.md) §11: 공격 묶음 칸 셋(투사체·코드 모션·큐)과 부품 SO 메뉴, 반응 칸 둘, 클립 묶음에서 걷은 칸, 유닛 검사 줄, 사건 Presenter의 캐릭터 처리기 생성 줄(호진님 병합 줄).
  - 현강님 [사건 소리 메모](../handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md) §6: 소리 시점이 큐 데이터로(Audio 로직·프리셋은 그대로).
  - 천명님: Q-T1 = 고치지 않음(2026-10-27 사용자 "고치지 말고 천명님께 넘길 메모만"). 따로 정리한 [천명님 공유](../handoffs/2026-10-27-combat-presentation-generic-structure-cheon-share.md)(실패 사례 넷·제안 코드·값 보관 주의·바뀐 천명님 줄 하나)와 Core 전달 문서 §46(기록). Core 변경 없음.
  - 민형님: 없음(Factory 무변경).
- **Ge.4** CLAUDE.md 현재 상태·메모리 갱신.
- **남은 것:** 사용자 Unity 확인(§10, `PullAndMarkPresentationTests` 사례 넷은 천명님이 고칠 때까지 실패), 커밋(§11). 남은 결정은 없다 — M2는 그대로(`CombatStandardAttackAssetTests:81-82` 실행 실패로 남음), D1도 그대로(2026-10-27 사용자 "그대로 두고 기록만": `UnitVisual_Heavy`는 `CombatUnitAnimation_Cannon`을 계속 쓰고, 전용 `CombatUnitAnimation_Heavy`는 참조 없이 남음 — 지우지 않음).
