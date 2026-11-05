# 특성 발동 몸체 텍스트·효과 설계

상태: **3차 사용자 요청: Activation별 색·크기·직접 문구·Position Offset 추가, Prefab Override 유지**. 현재 변경 설계는 §10이며 1·2차 설명과 충돌하면 §10을 우선한다. Task와 실행 기록은 [구현 계획](../plans/2026-10-31-trait-body-feedback-plan.md) §10을 따른다. 2차 F~J 및 Core 효과 연동의 기존 검증 기록은 유지하고 Unity Play는 사용자 후속이다.

사용자는 버프·디버프 HUD 아이콘과 함께 유닛 몸체 중앙에 발동 이름을 표시하고 서서히 사라지게 하길 원한다. 대표 대상은 깔끔한 뒷정리·가속·희생이다. 가속은 **칸마다 표시하지 않고 이동이 끝나 발동할 때 `가속 X N`을 한 번 표시**한다. N은 이번 발동에 앞선 실제 자발 이동 칸 수다.

## 1. 범위와 표시 계약

| 대상 | 발동 | 표시 유닛 | 시작 시점 | 문구 |
|---|---|---|---|---|
| 깔끔한 뒷정리 | `CleanSweep / AttackGranted` | 버프를 받은 특성 보유자 | 대응 상태 사건의 사실 반영·HUD 갱신 직후, 같은 호출 흐름 | `깔끔한 뒷정리` |
| 가속 | `Acceleration / AttackGranted` | 이동한 유닛 | 이동 완료 후 대응 상태 사건의 사실 반영·HUD 갱신 직후 | `가속 X N` |
| 희생 | `Sacrifice / DamageRedirected` | 수호자 | 실제 가로채기 공격 조립에서 나가기 행동이 시작되는 순간 | `희생` |

- 텍스트는 표시하는 동안 유닛 몸체 중앙을 따라간다. 희생 이동 중에도 수호자를 따라간다. 보호 대상에게 희생 문구를 중복 표시하지 않는다.
- 표시 이름 원본은 `TraitVisualData.DisplayName`이다. 덮어쓰기 문구를 입력한 경우만 그 문구를 쓴다. 이름을 특성별 코드에 쓰지 않는다.
- 효과는 기존 Activation의 Effects를 함께 설정한다. HUD와 맞출 줄은 텍스트·Effects·Shake가 같은 시작 정책을 쓴다. 기존 Effects의 위치·회전·지연 설정은 유지한다.
- 버프를 얻지 않았거나 특성이 발동하지 않았으면 출력하지 않는다. 최종 HUD 갱신, 카메라 이동, 선택, 아이콘 재배치 때문에 다시 출력하지 않는다.
- 이번 첫 연결은 위 세 특성이다. 공용 텍스트 재생은 다른 Activation에서도 사용할 수 있다. 모든 버프·디버프의 상태 대응을 자동 지원한다고 간주하지 않는다.
- 공유 HUD의 설명 팝업은 사용자가 보류한 후속 작업이다. 이 기능과 별개이며 `HUD.prefab`의 설명 패널을 연결하지 않는다.

## 2. 1차 코드에서 확인한 의존성과 2차 변경

| 항목 | 확인 결과 | 설계 영향 |
|---|---|---|
| 발동 줄 | `TraitActivationVisual`: Cues·AttackPresentation·UnitActions·CueTiming·BodyText | 문구/시점은 유지하고 외형 값은 프리팹으로 이전. 일반 공격·피격 큐 전체에는 새 필드를 전파하지 않음 |
| 발동 값 캐시 | `CombatActivationRows / CombatActivationRow` | 연결 동안 값 사본 보관 경로를 확장. SO를 런타임 상태 저장소로 쓰지 않음 |
| 상태 표시 | `CombatFactEventHandler` → `CombatShownFacts.Render` → 유닛 HUD | 사실 반영·Render 직후에 상태 연계 피드백 통지. HUD 자체에 특성 발동 판정을 넣지 않음 |
| 일반 발동 | `CombatTraitEventHandler`가 사실 반영 전 큐 실행 | 상태 연계 줄만 그 시점 출력을 유보. 기존 Start 줄은 그대로 유지 |
| 희생 | `CombatGuardLead`가 공격 앞 항목에 큐·행동을 조립 | 같은 시작점에 텍스트 조각 추가. 가로채기를 일반 상태 부여로 바꾸지 않음 |
| 가속 값 | `TraitActivationSnapshot.Magnitude`는 증가한 공격력 | 이동 횟수로 사용하거나 칸당 증가량으로 나누지 않음 |
| 실제 이동 | `CombatCharacterEvent(Moved)`의 Expected/Final·MovementCause | 이번 경로의 실제 자발 진입 사건 수에서 N을 파생. 경로 요청 길이·두 점 거리를 쓰지 않음 |
| 경로 결과 | `CombatResult.PathMovement.DistanceSpent` 제공 | 단일 경로 결과의 대조 근거. 합성 명령은 마지막 PathMovement만 남으므로 이것 하나로 모든 발동의 N을 결정하지 않음 |
| 몸 위치 | 기존 `FieldUnitPresenter`의 몸 형상·표시 대상 경로 존재 | 현재 표시 자세의 몸 중심을 읽는 좁은 조회를 제공. 논리상 서 있는 자리와 이동 중 실제 자세를 구분 |
| 텍스트 외형 | 현재 코드가 Canvas/TMP를 만들고 SO가 폰트·색·크기를 보관 | 프리팹이 외형 원본, Factory가 Canvas/풀을 조립하고 창구에는 주입 |
| 지원 정책/캡슐화 | 검토 R1~R3에서 지원 조건 중복·Count 잠금·가변 Plan 노출 확인 | 공용 프로필·해석기 등록·수정 가능한 Inspector·불변 계획과 실행 세션으로 보완 |
| 출력 카메라 | 운영/시험 조립에서 이미 전달 | 같은 카메라로 투영. `Camera.main`·씬 전역 검색 및 FieldCamera 수정 없음 |
| 효과 수명 | `CombatEffectWindow`는 필드 아래에 효과를 놓고 유닛을 따라가지 않음 | 텍스트는 추적하지만 기존 파티클은 출력 순간 몸 위치에 놓임. 유닛 추적 파티클은 별도 확장 |

근거 소스: `CombatMovementCalculator.ProjectRoute`, `MovementProjection.Enter/RecordForcedMove`, `CombatCommandComposition.Merge`, `TraitAttackNotifications.Apply`, `CombatSceneEventPresenter.PresentEvent`, `CombatGuardLead.LeadOf`. Core 소스는 계약 조사 목적으로 읽었으며 변경 대상이 아니다.

## 3. 입력·프리팹·에디터의 원본

2차 변경은 외형 원본을 프리팹으로 옮긴다. 이름·발동 대상·상태 연결·가속 N·희생 시작 시점은 §1/4의 동작을 유지한다.

| 원본 | 보관할 값 | 다른 계층의 역할 |
|---|---|---|
| TraitVisualData | DisplayName | 텍스트가 이름을 가져옴. 특성별 이름을 코드/프리팹에 중복 저장하지 않음 |
| Activation / Body Text | Enabled, Text Source, Override Text, Target, Count, 선택적 Prefab Override, Hold/Fade Seconds | 발동별 내용·대상·시간 결정 |
| Activation / Cue Timing | ActivationStart=0 / StateApplied=1 | 텍스트와 기존 Cues의 시작점 결정 |
| 전투 공통 표시 설정 | Body Text Prefab, Body Text Stack Gap | 기본 프리팹과 출력 간격 선택 |
| 텍스트 프리팹 | 폰트·색·크기·머티리얼·정렬·외곽선·배경·크기/내부 배치·몸 중심 기준 시각 보정 | 표시 컴포넌트는 저장된 외형을 유지하며 문구/페이드만 적용 |
| Result.Events | 발동·상태 전후·실제 이동 | 표시 계획이 읽으며 Core 규칙을 계산하지 않음 |

### 3.1 Activation에 남는 입력

- Enabled 기본 false. Text Source 기본 TraitDisplayName, Override일 때 공백 문구 금지.
- Target은 Source/Targets. Count는 None/CompletedMovementCells. N=1도 `가속 X 1`이다.
- Prefab Override 기본 null은 공통 Body Text Prefab 사용이다. 줄마다 프리팹을 반드시 넣게 하지 않는다. 서로 다른 버프/디버프 외형이 필요하면 기본 프리팹의 Variant를 해당 줄에 지정한다.
- Hold Seconds/Fade Seconds는 기본 0.25/0.6으로 유지한다. 발동의 시간 입력이며 프리팹에 같은 시간을 다시 저장하지 않는다.
- Color·Font Size·Screen Offset은 이전 후 입력/런타임 값에서 제거한다. 코드가 프리팹 외형을 이 값으로 다시 덮어쓰지 않는다. 위치 보정은 프리팹 Content 자식의 로컬 배치로 표현한다.
- 공통 Body Text Font도 이전 후 제거한다. 폰트는 프리팹 Label의 참조 한 곳이 원본이다.
- 첫 버전의 텍스트 한 항목, 기존 Effects/UnitActions 목록, SerializeReference 미사용을 유지한다.

### 3.2 프리팹 계약

기본 위치는 `Assets/ProjectL/Prefabs/UI/CombatOnly/Feedback/TraitBodyText.prefab`이다. 구현 시 해당 위치의 기존 에셋 유무를 확인하며 기존 에셋을 덮어쓰지 않는다.

```text
TraitBodyText (RectTransform, CanvasGroup, CombatBodyTextView)
└─ Content (RectTransform: 시각적 위치 보정)
   ├─ Background (선택)
   └─ Label (TextMeshProUGUI: 폰트·색·크기·정렬 등)
```

- CombatBodyTextView는 Label/CanvasGroup의 명시적 참조를 갖는 sealed 표시 컴포넌트다. Awake/OnEnable/Update에서 서비스 검색·자동 재생·Tick을 하지 않는다. 기존 Player가 진행을 전달한다.
- 컴포넌트는 문구 지정·페이드 비율·가시성·풀 복귀 초기화만 담당한다. 특성 키·HUD 상태·가속 계산·카메라·풀을 알지 않는다.
- Label과 CanvasGroup은 같은 프리팹 안에 있어야 한다. 루트는 RectTransform이며 프리팹 내부에 별도 Canvas/GraphicRaycaster를 만들지 않는다. 입력을 받지 않는 위젯이므로 CanvasGroup의 Interactable/Blocks Raycasts와 모든 Graphic의 Raycast Target은 꺼 둔다.
- 루트의 화면 위치·페이드 비율은 코드가 소유하고, Content/Label의 외형은 에셋이 소유한다. 같은 속성을 Animator와 코드가 동시에 제어하는 구성은 지원하지 않는다.
- CanvasGroup의 작성된 기본 알파에 런타임 페이드 비율을 곱한다. Label 색/알파·폰트·크기·Content 보정은 매 대여마다 코드 기본값으로 바꾸지 않는다.
- 전용 TMP 프리팹을 연결했는지, 필수 컴포넌트/내부 참조/폰트가 유효한지는 변경 전 검증한다. 잘못된 필수 구성은 대상 에셋과 필드를 명시한 예외다. 몸이 정상적으로 사라진 경우와 구분한다.
- SO 입력의 참조 타입은 GameObject 프리팹으로 둔다. CombatBodyTextView의 존재/내부 참조 검사는 표시 조립 경계에서 수행하며, 데이터 계층이 ViewSystem 표시 컴포넌트의 구현을 참조하게 만들지 않는다.

### 3.3 공용 지원 정책과 Inspector

- 허용 특성/Kind/Count는 §4.3의 공용 프로필을 조회한다. Inspector에 370/230/90 같은 숫자나 별도 허용 목록을 작성하지 않는다.
- 첫 StateApplied 지원은 CleanSweep/AttackGranted, Acceleration/AttackGranted다. UnitActions/공격 덮어쓰기와 함께 지정하지 않는다. Count의 CompletedMovementCells는 가속 AttackGranted만 지원한다.
- Anchor는 여전히 Start다. Cue Timing은 그 Start가 열리는 사건을 정한다. StateApplied Effects는 ActorBody/SubjectBody만 지원한다.
- 잘못된 기존 Count가 있으면 경고하고 **현재 문맥에서도 None으로 고칠 수 있게** 한다. 새로 선택 가능한 값만 제한하며 필드 전체를 잠그지 않는다. Inspector 그리기만으로 저장 값을 바꾸지 않는다.
- Kind 변경으로 Cue Timing/Count가 무효가 되어도 같은 화면에서 ActivationStart/None으로 되돌릴 수 있다. 지원 조합·상태/이동 자료의 필요성을 동일 프로필에서 안내한다.
- 지연 Effects는 선택한 시작점 이후 실행한다. 텍스트는 즉시 시작한다. 첫 세 줄의 기존 지연 0과 Effects/Shake 값은 보존한다.
- 비활성 텍스트에는 프리팹/문구를 요구하지 않는다. 활성 줄은 Override 또는 공통 기본 중 하나가 유효해야 한다. 모두 Override를 쓰면 사용하지 않는 공통 기본 참조를 필수로 만들지 않는다.
- 시간 유한성·음수 금지·총시간 양수·enum/조합 검증은 값 생성과 공용 정책에 모은다. 프리팹의 Unity 컴포넌트 검사는 에셋/조립 경계에서 한 번 수행한다.

## 4. 사건과 HUD의 연결

### 4.1 결과 단위 피드백 계획

결과 시작 시 `CombatTraitFeedbackPlan`이 공개된 `CombatResult.Events`에서 파생 계획을 만든다. 사건 순서를 바꾸거나 Final 사실을 미리 Render하지 않는다. 계획은 발동 사건/목록 인덱스, 표시 대상, 대응 상태 사건, 이동 칸 수만 가진다. 원본 식별은 결과 수명과 사건 인덱스로 구분하며 동일 값의 두 발동을 합치지 않는다.

- ActivationStart: 일반 발동 처리기 또는 희생 앞 항목이 원래 시작점에서 텍스트와 Cues를 실행한다.
- StateApplied: 발동 시점에 출력하지 않고, 대응 `CombatTraitStateEvent`가 ApplyFacts → Render를 끝낸 직후 실행한다. 폴링이나 다음 프레임 예약으로 동시성을 흉내 내지 않는다.
- State 대응은 **대상 ID만 같은 다음 사건**으로 선택하지 않는다. 같은 정산 구간 안에서 Source·TraitKey·Kind·대상·공격 Origin(있는 경우)과 실제 상태 변화 근거를 확인한다.
- CleanSweep는 Expected/Final의 `TraitEffects.AttackGrants`에서 해당 특성·소유자 출처의 부여 변경을 확인한다. 가속은 `AccelerationTraitStatusSnapshot.HeldAttackBonus` 증가를 확인한다. 이는 공개 전후 사실의 대조이며 발동 조건이나 보너스 계산의 재구현이 아니다.
- 후보 구간은 발동부터 다음 행동 시작/회수/결과 끝까지다. 다른 특성 Effects가 중간에 있다는 이유만으로 끊지 않는다(가속과 이동 관련 다른 특성 발동이 같은 정산에 올 수 있음). 다음 같은 Source·TraitKey의 발동은 대응 구간을 나눈다.
- 상태 후보가 없거나 여러 후보 중 유일한 연결을 입증할 수 없으면 해당 상태 연계 출력만 생략하고 연결당 같은 사유의 경고를 한 번 남긴다. Start로 몰래 대체하거나 다른 상태 갱신에 붙이지 않는다. 정상 세 프리셋에서 이 경고가 나면 완료가 아니다.
- 앞서 State가 나오는 합성 입력은 정확한 동시 시작을 보장할 수 없으므로 StateApplied 시나리오 작성 오류로 표시한다. 대표 조립 프리셋은 운영 순서인 Activation → State로 맞춘다.

첫 구현에서 모든 디버프의 전후 값 해석을 추가하지 않는다. 새로운 상태 종류를 지원할 때 그 사실 대응만 별도 등록한다. UI 렌더러·HUD·이동·희생 조립은 수정하지 않아도 되도록 한다.

2차에서는 계획의 Dictionary/List를 호출자에 노출하지 않는다. 아래 §4.4처럼 계획 구성 정보와 재생 중 실행 상태를 구분한다.


### 4.2 가속 X N

N은 발동에 앞선 같은 Source의 이번 자발 이동 경로에서 **실제 완료한 칸 진입 수**다. 결과 전체나 전투 전체 누계가 아니다.

1. 결과 사건 순서에서 Source의 직전 경로 구간을 찾는다. 행동 시작·회수·이전 가속 발동·결과 경계로 이전 경로와 분리한다. 관계없는 타 유닛 사건은 횟수에 넣지 않는다.
2. `Moved`이면서 Anchor가 실제 변경된 자발 이동 사건을 센다. 현재 공개 계약의 null MovementCause는 기본 자발 이동이며 명시적 Forced·Relocated·복귀·희생 UnitAction은 제외한다.
3. 중간 피해·지형 변화·다른 특성 사건은 이동 횟수를 늘리지 않는다. 지뢰 등으로 경로가 중단되면 실제 진입한 칸만 센다. 되돌아오는 경로도 각 실제 진입을 센다.
4. 여러 경로/발동이 한 결과에 있어도 경로별로 구분한다. 현재 단일 이동의 PathMovement.DistanceSpent가 대응함을 확인할 수 있는 경우 사건 수와 대조한다. 마지막 PathMovement를 모든 유닛에 적용하지 않는다.
5. 출처 구간을 유일하게 정할 수 없거나 N을 얻을 사건이 없으면 N을 추측하지 않는다. 운영에서는 이름만 표시하고 경고 한 번, 조립 도구에서는 재생 전 작성 오류를 보여 준다. 단순 Activation 합성으로 이동 횟수를 검증했다고 기록하지 않는다.

예: 3칸 이동을 요청하고 실제 2칸 진입 후 멈췄으며 Core가 가속을 발동시켰으면 `가속 X 2`. 칸당 공격력 증가가 5여서 Magnitude=10이어도 `가속 X 10`으로 표시하지 않는다. Core가 발동을 내지 않았다면 이동만으로 문구를 만들지 않는다.

이 경로는 공개된 실제 이동 사실을 표시용으로 집계하므로 새 칸별 Core 사건을 요구하지 않는다. 구현 중 운영 합성 결과에서 경로 출처를 구분할 수 없는 계약 부족이 확인되면 Core 인계 문서에 요청하고 해당 경우는 미완료로 보고한다. View에 규칙 계산이나 숨은 보정값을 추가하지 않는다.

### 4.3 R1 — 지원 프로필과 사실 해석의 확장

- `CombatTraitFeedbackProfiles`는 `(TraitKey, TraitActivationKind)`를 키로 하는 불변 프로필 조회다. 위치는 View 데이터의 CombatPresentation 경계이며 런타임 재생기나 UnityEditor를 참조하지 않는다.
- 프로필에는 상태 근거 종류(None/AccelerationHeld/TraitAttackGrant)와 허용 Count를 둔다. StateApplied 지원 여부는 근거 종류에서 파생한다. 부울 지원 플래그/별도 허용 표를 중복 저장하지 않는다.
- 프로필이 없는 기존 줄은 ActivationStart·Count=None의 기본 표시를 사용할 수 있다. 첫 두 StateApplied 프로필만 등록하며 모든 특성의 실제 발동 규칙을 View에 옮기지 않는다.
- 데이터 검증·Inspector·조립 진단·런타임 계획이 같은 프로필 조회를 사용한다. 데이터 계층이 Playback 또는 Core Rule을 역참조하지 않는다.
- `ICombatTraitStateEvidence`는 근거 종류와 전후 사실 대조만 제공하는 좁은 계약이다. 첫 구현은 가속 보유량, 특성 출처가 포함된 공격력 부여의 두 처리기이며 Factory의 불변 등록 목록으로 조립한다. 처리기 선택용 TraitKey if를 Plan 안에서 늘리지 않는다.
- 같은 공격력 부여 근거를 쓰는 새 특성은 프로필 등록으로 연결한다. 새 근거 종류가 필요하면 해당 처리기와 등록만 확장한다. Text/풀/희생/Inspector는 수정하지 않는다. 중복 키·해석기 누락은 조립 시 검증한다.

### 4.4 R3 — 불변 계획과 실행 상태의 캡슐화

- `CombatTraitFeedbackPlan`은 결과 사건에서 만든 불변 구성이다. 발생 ID(사건/발동 인덱스), 대상, 대응 State, N, 진단만 갖고 Started/Shaken 같은 실행 플래그를 갖지 않는다.
- 내부 Dictionary/List는 private이고 외부에는 get-only 값과 변경 불가능한 컬렉션 보기만 준다. IReadOnlyList로 캐스팅만 하고 원본 List를 외부에 노출하는 형태는 피한다.
- `CombatTraitFeedbackSession`이 이번 결과의 Started/소비한 State/Shake 완료 상태를 단독 소유한다. `TryStart`, `TryConsumeState`, `TryBeginShake`, `Clear`처럼 의미가 있는 동작으로만 변경한다. Playback에서 Map.Remove나 플래그 대입을 하지 않는다.
- 계획에는 구성 원본, 세션에는 실행 여부만 둔다. 같은 실행 플래그를 Plan/Playback/Session에 각각 보관하지 않는다. 원본 generation은 기존 Presenter 것이며 별도의 전역 시계를 만들지 않는다.
- BeginResult는 계획/세션만 교체한다. 이미 나온 문구는 자연 만료한다. Cancel/Reset/Detach는 세션과 활성 문구를 함께 비운다. 늦은 호출은 기존 generation 검사로 차단한다.

## 5. 프리팹 생성·재사용·몸 추적·수명

### 5.1 조립과 자원 소유권

- `CombatEventPlaybackFactory`는 `CombatBodyTextFactory`를 통해 운영/시험 공용 표시를 조립한다. 텍스트 Factory가 Canvas 호스트·Prefab별 풀·창구에 필요한 참조를 연결한다.
- Canvas는 텍스트 한 개마다가 아니라 연결당 하나다. 화면 투영용 기반 객체이므로 코드로 만들 수 있다. 글자/배경/레이아웃 외형은 텍스트 프리팹에만 있다. Window의 `EnsureRoot/CreateLabel` 직접 조립과 코드 기본 외형 설정은 제거한다.
- 호스트는 소유 전투/시연과 같은 Scene의 독립 루트 Overlay Canvas로 생성하여 3D Field의 Transform 배율/활성 상태 아래에 묶지 않는다. Factory가 Scene·출력 Display·CanvasScaler를 한 번 설정한다. FieldCamera는 수정하지 않는다.
- `CombatBodyTextWindow`는 주입된 호스트/풀·출력 카메라·몸 조회로 표시 진행만 맡는다. 서비스 탐색이나 직접 UI 조립을 하지 않는다.
- `CombatBodyTextPool`은 프리팹별 풀을 소유한다. Override가 다른 인스턴스를 다른 외형으로 재사용하지 않는다. Rent/Return의 소유 풀과 중복 반환을 확인하고 복귀 시 문구·페이드·가시성을 초기화한다. 폰트·색·크기·Content 배치는 보존한다.
- Factory는 생성 실패 시 만든 자원을 회수하고, 성공한 연결의 최종 소유자는 기존 사건 Presenter의 수명 경로다. Release는 구독 해제 → 활성 인스턴스 반환 → 풀 인스턴스/호스트 파괴 순서이며 반복 해제는 안전해야 한다.
- 새 View 컴포넌트는 풀에 들어가는 텍스트마다 하나다. 모든 유닛 프리팹에 MonoBehaviour를 붙이지 않는다. 내부 풀/해석기를 새 Global 서비스로 등록하거나 루트 Export하지 않는다.

### 5.2 재생 의미

- `CombatBodyTextPiece`는 기존 Accent에서 프리팹 텍스트 요청을 시작하고 즉시 끝난다. Stand/Attack 채널과 다음 사건/결과 완료를 막지 않는다. 희생 앞 항목은 기존 같은 Start를 사용한다.
- 텍스트 Player는 FindBlock=None·IsComplete=true·BlocksFinalFacts=false이며, 기존 공격 Gate의 Tick 조기 반환보다 앞에서 시간만 진행한다. 별도 전역 Update/코루틴 시계를 추가하지 않는다.
- 같은 유닛의 여러 출력은 공통 Stack Gap(현재 28px)으로 쌓고 생존 슬롯을 유지한다. 빈 슬롯을 재사용하며 문구/횟수를 임의로 합치지 않는다.
- 카메라 뒤/화면 밖에서는 숨기되 시간은 흐른다. 실제 표시 몸이 사라지면 다음 투영에서 회수한다. 렌더 콜백이 없는 상태에서도 시간 만료/취소로 회수되므로 영구 잔류하지 않는다.
- 텍스트는 몸을 추적하지만 기존 파티클은 시작 순간의 몸 위치에 놓인다는 정책을 유지한다.

### 5.3 R4 — 필요한 시점에 한 번 조회

- Tick은 활성 문구의 시간·페이드·만료만 처리한다. 매 Tick에 몸 Position 델리게이트를 호출하거나 전체 유닛/Buffer를 조회하지 않는다.
- 실제 몸 중심·대여 유효성은 렌더 직전의 활성 문구 투영에서 한 번 함께 조회한다. 값이 없으면 그 항목을 회수한다. 풀 복귀를 같은 프레임에 중복 수행하지 않는다.
- 렌더 투영은 그 프레임의 유닛 이동과 카메라/Shake 뒤에 수행한다. Tick에서 먼저 읽은 위치를 캐시하여 한 프레임 늦게 따라가게 하지 않는다. 콜백은 필요한 렌더 패스 기준이며 무조건 Time.frameCount로 첫 호출만 채택하지 않는다.
- 활성 문구가 0→1일 때 렌더 콜백을 구독하고, 마지막 항목 반환·Cancel·Release에서 해제한다. 필수 공통 Player 수명 메서드는 유지하되 비활성 출력 목록을 훑는 별도 Tick은 추가하지 않는다.
- Show에서는 아직 투영하지 않은 새 인스턴스가 화면 원점에 잠깐 보이지 않도록 숨겨 준비하고, 첫 유효 투영에서 표시한다. Prefab/Rent 시 구성 검사를 마치며 매 프레임 GetComponent/Find를 반복하지 않는다.
- 시간 계산은 기존 전투 deltaTime만 사용한다. Pause·다음 Packet·반복·취소의 기존 의미를 유지한다.

### 5.4 UI 등록 판단

이 프리팹은 독립 개폐 화면/팝업이 아니라 사건 Accent의 세부 표시다. UIId·부모 UI·Open Policy·UI 이력·입력은 없고 Presenter가 생성/풀/해제 수명을 소유한다. HUD 설명 패널의 UI 등록을 우회하기 위한 별도 창이 아니다. 앞으로 독립 HUD/팝업으로 확장하면 UIBase/UISystem 등록 설계를 다시 검토한다. 공유 HUD.prefab의 Description Panel/Text는 연결하지 않는다.

## 6. 조립 도구와 에셋

- 운영과 TraitActivationPreview는 같은 `CombatEventPlaybackFactory` 조립을 사용한다. 시험 전용 텍스트 렌더러를 따로 만들지 않는다.
- 에디터에는 Cue Timing 및 Body Text 설정을 표시하고, Kind·Context·Count의 불가능한 조합은 이유와 함께 막는다. `StateApplied` 단독 Moment는 대응 State가 없으므로 조립 모드 사용을 안내한다. 묵시적 상태를 만들어 주지 않는다.
- 가속 프리셋은 Movement → Activation → State를 한 Packet 안에 둔다. N은 Movement 블록이 만든 실제 사건에서 나온다. State와 Activation을 별도 Packet에 두면 같은 HUD 시작점으로 묶지 않는다.
- 깔끔한 뒷정리는 선행 아군 공격 → 소유자 Activation → 소유자 State. 희생은 기존 Interception을 유지한다. 생존/사망 프리셋 모두 텍스트 확인 대상으로 둔다.
- 첫 에셋 연결은 `TraitVisual_CleanSweep`, `TraitVisual_Acceleration`, `TraitVisual_Sacrifice`, 전투 공통 표시 설정의 기본 프리팹/간격, 해당 조립 프리셋이다. 현재 에셋의 정확한 경로는 Task A에서 확인한다. 기존 GUID·아이콘·설명·특성 수치·희생 UnitActions 값을 보존한다.
- 간단 모드와 기존 저장 프리셋 형식은 유지한다. StateApplied를 요구하는 줄은 단독 합성의 한계를 명시한다. 다른 Start 줄은 기존과 같이 재생한다.

## 7. 2차 구조 점검 목표

| 기준 | 적용·검토 결과 |
|---|---|
| SSOT | 이름은 TraitVisual, 발동/상태/실제 이동은 Result.Events, 문구/시점/시간은 발동 줄, 외형/폰트는 프리팹, 기본 프리팹/간격은 공통 설정, 지원 정책은 공용 프로필. 계획/집계는 결과에서 파생한 일시 값 |
| S | 데이터/프로필 검증, 공개 사실 대응, 불변 계획/실행 세션, 프리팹 표시/풀/조립을 구분. HUD는 상태 표시 책임 유지 |
| O | 상태 대응 해석기를 등록하여 확장. 첫 범위는 가속·특성 공격력 부여. 특성마다 전용 텍스트 MonoBehaviour를 추가하지 않음 |
| L | 기존 Accent·Cancel·Release 계약을 지키며 텍스트는 사건/결과를 막지 않음. 기존 Start의 의미 유지 |
| I | 텍스트 창구에는 출력/정리·몸 중심 조회만 전달. Core Query/전체 HUD/Flow를 주지 않음 |
| D | Factory가 프리팹/풀·호스트·카메라·조회·창구를 조립. Framework나 Render 서비스에 게임 특성 의존성을 넣지 않음 |
| JMH/FW | 기존 Player/generation 수명, 새 서비스/Export 없음(FW-001~004 경계 유지). 자체 Prefab 풀과 기존 EffectScope 소유권 구분(FW-011/012). FW-010 Mesh·FW-013 Bus·FW-015 JobQueue는 변경 없음으로 실제 적용 범위 구분. 풀·구독/취소·초기화 수명은 정적/컴파일 확인하고 Play는 사용자 후속 |
| 캡슐화 | 불변 Plan과 private 실행 세션을 분리하고 Map/플래그 직접 변경 API를 제거 |
| if/Tick | 지원 조건은 프로필, 상태 선택은 등록된 해석기. 시간은 Tick, 몸/카메라는 활성 투영에서만 조회 |
| 기능 독립성 §4.6 | 외형 변경은 프리팹, 페이드/겹침은 Text 영역, 상태 추가는 프로필/해석기, 의존성 연결은 Factory. 텍스트 외형 수정 때문에 공격·Core·HUD 상태 로직을 다시 고치지 않음 |
| 영향받는 공용 계약 | Presenter의 Render 후 통지·Tick/Cancel/Release 연결과 Activation 캐시 확장. 이 경계의 일반 발동/추가 공격/희생 회귀는 확인 |

Core·FieldCamera·Architecture·Framework 공용 코드·공유 HUD 프리팹·다른 특성의 일괄 연출 등록은 변경 범위가 아니다. 새로운 View 테스트 코드/자동 검증 메뉴를 만들지 않는다.

## 8. 완료 기준

대표 세 특성의 시점·표시 대상·문구·소멸 및 취소 후 회수가 수동 Play에서 확인되어야 한다. 가속은 N과 Magnitude가 다른 예로 확인한다. 코드·에셋 정적 대조와 컴파일을 수행하고, 실제 재생하지 못한 항목은 미확인으로 기록한다. 테스트 툴에서 확인한 합성 사건과 인게임 Core 발동 확인은 구분한다. 알려진 기존 종료 오류는 이 기능 때문에 수정하지 않는다.

2차 코드 작업의 마무리는 컴파일·정적 대조·이전 결과·확인 목록 전달까지다. 실제 Play는 사용자 후속이며, 결과를 받기 전 전체 동작 검증을 완료로 기록하지 않는다.


## 9. 2차 이전과 검토 항목 처리

- 수정 근거: 사용자가 “텍스트 외형은 프리팹으로 편집하고 런타임에 생성·재사용…검토 보완도 같이 반영”을 요청했다. [구조 검토](2026-10-31-trait-body-feedback-structure-review.md)의 R1~R4를 후속 Phase F~J로 연결한다. 설계 수정은 구현 완료가 아니다.
- 기존 에셋의 실제 Enabled·폰트·색·크기·오프셋을 착수 시 조사한다. 현재 대표 세 줄은 같은 흰색/28/오프셋 0이므로 기본 프리팹 하나로 이전할 수 있다. 사용자가 그 뒤 바꾼 값은 덮어쓰지 않는다.
- 기존 활성 줄에 서로 다른 외형이 있으면 동일한 외형 조합끼리 프리팹/Variant를 공유하여 이전한다. 줄마다 무조건 새 프리팹을 만들거나 특성 이름을 프리팹에 고정하지 않는다.
- 비활성 줄도 저장된 맞춤 외형이 있으면 이전 표에 포함하여 나중에 Enabled를 켰을 때 값이 사라지지 않게 한다. 비활성이라는 이유로 필수 프리팹 검증을 강제하지는 않는다.
- 기존 Color/Font Size/Screen Offset와 공통 Body Text Font는 대응 프리팹이 준비되고 참조가 연결된 뒤 코드·SO에서 제거한다. 임시 구형 호환 필드를 영구로 남기거나 두 설정 중 무엇이 우선인지 런타임에서 추측하지 않는다.
- 이름·아이콘·설명·Effects·Shake·UnitActions·Kind·Cue Timing·Count·유지/페이드·가속 N과 Magnitude의 구분·기존 GUID는 보존한다. 기존 폰트 에셋을 다시 저장하지 않는다.
- 다른 브랜치의 구형 에셋은 병합 시 같은 명시적 이전이 필요하다. SerializeReference나 새 View 테스트/자동 검증 메뉴를 추가하지 않는다.
- 공유 HUD의 categoryImage/itemIcon 연결 및 설명 팝업은 이번 후속 범위에 넣지 않는다. 에디터 Play 검증은 사용자가 별도로 수행한다는 지시를 유지하고, 구현 완료 시 확인 목록을 제공한다.


구현 결과: 위 2차 구조를 반영했으며 기본 프리팹 하나로 기존 세 줄을 이전했다. 상세 파일·값 보존·컴파일 근거·자원/서비스 경계는 [계획 §9.9~9.10](../plans/2026-10-31-trait-body-feedback-plan.md#99-2차-실행-기록--fj), 편집법은 [사용법](../viewDocs/trait-body-feedback-guide.md)을 따른다. 이 기록은 실제 Play 성공을 의미하지 않는다.


## 10. 3차 — Activation별 문구·색·크기·위치

사용자 지시: “색, 크기도 Activations 내로 옮기고, 들어갈 text도 쓸 수 있게하자, Prefab Override 는 그냥 두고”, “position offset도 넣어야해”. 이 요청은 2차의 색/크기 전체 프리팹 소유 정책을 변경한다. 프리팹 생성·재사용과 기존 피드백 시점/근거 계약은 유지한다.

### 10.1 입력과 단일 원본

| 설정 | 현재 설계 / 기본값 |
|---|---|
| Text Source | 기존 TraitDisplayName / Override enum·직렬화 값 유지 |
| Text | 기존 overrideText를 재사용한 여러 줄 입력. Override 모드에서 작성하고 공백만 있는 입력 거절. 새 중복 text 필드 없음 |
| Color | Activation의 Color, 기본 흰색 RGBA(1,1,1,1). 성분은 유한값 |
| Font Size | Activation의 fontSize, 기본28. 유한한 양수 |
| Position Offset | Activation의 positionOffset(Vector2), 기본(0,0). 몸 중심 투영 뒤 적용하는 Canvas 기준 좌표. +X 오른쪽/+Y 위쪽. 현재 CanvasScaler의1920×1080 기준으로 스케일되는 px이며 월드 거리/물리 화면 고정px가 아님 |
| Prefab Override | 기존 참조/공통 프리팹 fallback 유지. 폰트·머티리얼·정렬·배경·장식·Content 계층은 프리팹에서 편집 |
| Target / Count / Hold / Fade / Cue Timing | 기존 값·지원 정책 유지. 직접 문구에도 Count의 X N을 한 번 덧붙임 |

프리팹 Label의 색/크기/샘플 문구는 프리팹 미리보기 값이다. 실제 출력은 매 대여 때 Activation의 확정값을 적용하며 프리팹 값과 우선순위를 추측하지 않는다. TMP Auto Size는 실제 표시 인스턴스에서 꺼 Font Size를 보장한다. 폰트·머티리얼·장식/Content 배치는 유지한다. 색 알파는 기존 CanvasGroup 기본 알파·페이드와 곱해진다.

### 10.2 실행·풀·투영

`CombatBodyTextInput → 불변 CombatBodyTextData → 기존 Activation 캐시/요청 → Window → Pool.Rent → View.Prepare(text,color,fontSize)`로 전달한다. View는 DB/특성/대상/카메라를 받지 않는다. 모든 Rent에서 색·크기·Auto Size를 적용하여 같은 프리팹의 다음 발동에 이전 스타일이 새지 않게 한다. Pool은 기존 프리팹별 버킷을 유지하며 스타일별 별도 풀/서비스는 만들지 않는다.

루트 위치는 `몸 중심의 Canvas 좌표 + Position Offset + 위쪽(Stack Gap × 슬롯)`로 매 렌더 갱신에서 대입한다. 기존 위치에 더하지 않아 이동·Shake·반복 재생에서 누적되지 않는다. Content의 내부 배치는 그대로다. Tick은 기존 시간/페이드/만료만 맡고 추가 Update·Buffer polling·GetComponent/Find는 없다. 첫 투영 전 숨김·몸 소실 회수·Cancel/Release·generation·공격 Gate를 유지한다.

### 10.3 에셋 이전·범위

현재 활성4줄(가속/깔끔한 뒷정리/희생/표식)은 모두 같은 공통 프리팹, 흰색28·Content 기준 보정0이다. color/size/positionOffset을 해당 SO에 명시해 기존 화면을 보존한다. 착수 후 바뀐 Override/프리팹 값이 있으면 다시 조사하고 임의 초기화하지 않는다. Kind/문구/대상/Count/시간/Effect/Shake/UnitActions/정책 설명/GUID·기존 프리셋은 유지한다. 프리팹/폰트/공유 HUD·Core·FieldCamera·Architecture·Framework는 수정하지 않는다.

소규모 하나의 연결 변경으로 Task Phase K에서 입력→표시→에셋→검증을 함께 완료한다. 새 View 테스트/검증 메뉴는 만들지 않는다. 컴파일/정적 검증 후 사용자에게 색·크기·직접 문구·오프셋·같은 풀 재사용 확인 목록을 제공하며 현재 Play는 제어하지 않는다.

### 10.4 구조 점검

SSOT는 발동별 내용/색/크기/위치를 Activation, 구조/폰트/장식을 프리팹, 사실/시점을 기존 Core 결과/피드백에 둔다. S: 입력검증/표시/풀/투영 분리, O: 기존 표시 인자 확장, L: 비차단·취소/반환 계약 유지, I: View에는 문구와 스타일 값만 전달, D: 기존 Factory 주입과 자체 자원 소유 유지. 새 서비스 등록/Export/Builder/UIId를 추가하지 않아 JMH/FW 계층을 바꾸지 않는다. 외형 변경에 Core/HUD/공격 구현을 수정할 필요가 없고 각 Rent의 완전한 스타일 적용·절대 좌표 대입으로 상태 누출/누적을 막는다.


3차 구현 결과: Phase K에서 위 입력/Inspector/대여별 스타일 적용/Canvas offset 및 기존4줄 이전을 완료했다. 코드 컴파일·정적 검증은 통과했고 실제 에디터/Play는 사용자 후속이다. 원본 실행 기록은 [계획 §10.1](../plans/2026-10-31-trait-body-feedback-plan.md#101-phase-k-실행-기록--2026-10-31)이다.
