# 게임 설명의 수치 대응 — 현황 조사와 설계안

- 작성·갱신: 2026-11-02, View 작업.
- 사용자 요구: 게임에 존재하는 모든 Description에서 수치가 들어가는 경우에 대응한다. 기존 일부 대응을 포함해 빠진 범위를 확인한다.
- 상태: **A 환경·D 회복 코드와 B 특성 33종·59개 토큰 등록/읽기 연결 반영, 컴파일 미확인.** `78fdc81`의 최종 `TraitSpec`을 사용하며 SO 본문까지 이전한 것은 7종이다. [토큰 정의와 목록](../viewDocs/description-token-guide.md#8-문구-이전에-정할-토큰-정의안-2026-11-02)을 따른다. 나머지 문구·목록/문자열 표시가 남고 아이템·유물·선택지·잔여 환경값은 공개 계약 대기다. 전체 범위는 Combat 외 씬/UI도 포함한다.
- 실행 계획: [Description 수치 대응 계획](../plans/2026-11-02-description-numeric-values-plan.md).
- Core 요청: [View–Core 경계 인계 §64](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#64-게임-description-수치-공개-요청-2026-11-02-설계안미구현미전달).
- 조사 기준: 현재 작업 트리의 소스·SO. HEAD `039204aee` 및 동시 작업 중 변경이 있으므로 구현 Phase 시작 때 대상 파일을 재확인한다. 과거 문서의 완료 기록을 현재 실행 검증으로 취급하지 않는다.

## 1. 완료 기준과 범위

설명 본문의 피해량·비율·사거리·반경·횟수·주기·지속시간·증가량·조건 임계값이 **설명 대상에 실제 적용되는 공개값**을 따라야 한다. 아래 별도 수치 줄만 갱신되고 본문에 옛 숫자가 남으면 완료가 아니다. `절반`, `한 번`, `한 턴`처럼 한글로 쓴 수치도 조사 대상이다.

- 대상: 특성, 버프·디버프, 환경·기믹·장애물, 아이템, 유물, 훈련·증강·강화·회복 선택지, 실제 사용되는 유닛·병과·스테이지 설명과 공용 툴팁.
- 같은 설명이 덱·손패·전투 정보·장착 HUD·가방·상점에 나오면 각각 소비 경로를 확인한다. 같은 정의 키라도 보유 개체, 등급, 강화, 전투 상태, 제안 선택지에 따라 값이 다를 수 있다.
- 설정 설명(예: 보호막 최대량)과 현재 상태 설명(예: 보호막 잔량)을 구분한다. 아직 구매하지 않은 선택지는 구매 후 값, 훈련 비교는 현재/다음 값, 구매 후 유지 카드는 그 구매의 값을 쓴다.
- 숫자가 없는 풍미 문구와 고정 안내는 보존한다. 현재 설명에 수치가 생략된 아이템·유물도 효과 수치를 표시할 수 있는 전달 경로를 준비한다. 표현 문구 추가와 게임 규칙 변경은 구분한다.
- 이동 방식처럼 실제 불변 규칙에 해당하는 숫자는 원본과 고정 사유를 명시해야 한다. 숫자가 작거나 현재 기본값이라는 이유만으로 고정 문구로 분류하지 않는다.
- 미사용·퇴역 설명과 향후 설명 슬롯은 삭제하거나 새 게임 기능으로 활성화하지 않는다. 운영 사용 여부와 향후 적용 조건을 남긴다.
- 현황은 정적 조사 결과다. 모든 화면의 실제 노출·동작·수치 일치는 Unity 확인 전이다.

## 2. 최초 조사 시점의 대응 범위와 남은 범위

경로 기준: `Assets/ProjectL/Script/Systems/RuntimeSystem/` 아래의 `InGameSystem/ViewSystem`을 View, `GlobalSystem/UISystem`을 UI, `InGameSystem/CoreSystem/CoreCaculator`를 Core로 줄여 적는다. 에셋은 `Assets/ProjectL/DataBaseSO` 아래다.

| 분류 | 현재 수치 경로 | 남은 범위 |
|---|---|---|
| 환경 칸 호버 | `View/Combat/Input/Assist/CombatEnvironmentText.cs`: 지뢰 피해, 깃발 우선도, 칸 보정 공격력/최대 체력, 약한 지반 한도/잔여, 먼지 연막 흔적 감소량, 화약통 피해, 보급품 피격/파괴 보상, 토네이도 주기, 낙석 비율을 공개 사실로 치환 | 고지대·모래폭풍 등의 고정 숫자, 미등록/오타 토큰 검증, 반경·파급 비율 등 본문에 고정된 나머지 규칙값 |
| 특성 공용 표시 | `UI/Screens/Global/UnitCardTraitContent.cs`: 가시밭·끌어오기 본문을 코드에서 생성. 표식·정찰대·위협·재생은 수치 줄 추가 | 나머지 특성의 SO 고정값. View SO/코드/성장 설명의 원문 중복. 전투/보유 카드의 공개값 차이 |
| 보유 카드 | `OwnedCharacterStatsSnapshot`에서 가시밭·표식·끌어오기·충격 볼트·정찰대 설정 공개 | 공용 formatter가 받는 보유 경로에는 충격 볼트가 없음. `UnitCardUI`, `DeckEditModelBuilder`→`DeckSlotInfoUI`의 전달 범위를 함께 맞춰야 함. 나머지 특성 설정은 현재 경로에 충분하지 않음 |
| 상태 효과 | `CombatUnitStatusFactory`→`CombatEffectFormatters`→`UnitStatusVisualData.DescribeBody`: `{0}` 등의 값을 타입별 snapshot에서 읽음. `CombatStatusDescriptionFormatter`가 출처·수명·잔량을 추가 | 이미 동적 처리됨. 특성의 설정 설명과 혼동하지 않고 현재 지원 효과를 보존. 미사용 에셋을 운영 등록으로 오인하지 않음 |
| 훈련 | `TrainingCardContentBuilder`: `CurrentValues`/`NextValues`로 현재·차이·다음 수치 줄 작성 | `offer.Description` 본문은 고정 문자열. 별도 수치 줄과 본문이 서로 다른 값을 나타낼 수 있음 |
| 증강·강화 | `TraitGrowthPopupModelBuilder`의 `choice.Description`, `EnhancementShopOfferContentBuilder`의 `EnhancementDescription` | Core가 넘긴 문자열도 자동 수치 대응을 뜻하지 않음. 선택할 결과의 정량 설정이 필요 |
| 소비 아이템 | 상점은 `ItemShopOfferContentBuilder`가 Core `ItemDescription` 사용. 가방 `PackUI`와 장착 HUD `FieldUnitPresenter.ItemStatus`는 View `Description` 사용 | 효과 설명과 풍미 문구를 구분하고 세 화면이 같은 효과 수치를 사용하도록 연결. 상점 숫자는 InGame SO의 고정 문장 |
| 유물 | `ArtifactBoardRefresher`가 목록·상세에 View `Description` 전달 | 현재 문구는 정성적 효과 설명. 현재 `ArtifactData`는 식별·배치값 중심이라 효과 수치 공개 경로 확인/보완 필요 |
| 회복 | `RecoveryPopupModelBuilder`가 `ProjectedHealAmount`를 별도 줄에 표시 | 공개된 `RecoveryOptionState.ProjectedMaxHealthIncrease`를 설명에서 사용하지 않음. `Option.EffectKind`에 맞게 표시해야 함. 본문 템플릿도 선택지 값으로 처리 |
| 화면 공용 글 | `StageOfferTextData`의 가격·골드·설명 줄은 `string.Format` 및 인자 수 검사 사용 | 설명 문자열을 `{0}` 자리에 넣는 것은 그 설명 안의 수치 치환이 아님. 중첩된 설명은 먼저 완성해서 전달 |
| 유닛·구조물 상세 | `CombatInspectionInfoMapper`는 `visual.Description` 전달 | 현재 일반 유닛 설명은 대부분 빈값. 새 수치 템플릿을 넣기 전에 필요한 사실과 소비 경로를 연결 |

### 2.1 확인된 원문 불일치

`ViewGroup/TraitVisual/Item`과 `InGameGroup/TraitGrowth/Item`에 다음 숫자가 각각 저장돼 있다. 아래는 원문 비교이며 모든 개체의 실제 최종 규칙값을 판정한 표가 아니다.

| 특성 | View 설명 | 성장 설명 | 근거/영향 |
|---|---|---|---|
| FocusedBombardment | 50% | 70% | 성장 SO의 normal도 `extraDamagePercent: 70`, rank2 80, rank3 100. 전역 문장을 어느 한 숫자로 덮어써서는 등급별로 대응할 수 없음 |
| FinishingShot | 20% | 15% | 성장 SO normal `attackThresholdPercent: 15`, rank2 20, rank3 30 |
| MoraleBoost | 20% | 10% | 두 원문을 동시에 관리하는 문제가 존재 |
| Hunter | 공격력 1 | 공격력 3 | 보상 횟수와 누적 보상 상태도 설정량과 별도로 다뤄야 함 |

추가 사례:

- `TerrainVisual_HighGround`에는 30%, `TerrainVisual_Sandstorm`에는 사거리 1·피해 감소 20%가 고정돼 있다. `StaticTerrainFact`는 해당 감소율을 공개하지 않으며 실제 규칙은 `Domain/Movement/EnvironmentTerrainRules.cs`에 있다. View에서 Domain 코드를 호출하거나 상수를 복제하지 않는다.
- `TraitVisual_DustScreen.Description`의 1/20은 고정이지만 같은 특성의 `TrailDescription`은 `{사거리감소량}`/`{피해감소율}`을 사용한다. 흔적 호버 대응이 특성 아이콘 설명 대응을 의미하지 않는다.
- 회복 4번 View 설명은 “최대 체력이 오릅니다.”이고 모델은 모든 옵션에 예상 회복량 줄을 만든다. 최대 체력 증가량은 이미 공개돼 있어 Core 변경 없이 소비할 수 있다.

### 2.2 특성 후속 조사 목록

| 현재 상황 | 대상/필요값 |
|---|---|
| 설정값을 이미 본문/별도 줄에서 사용 | ThornField, Pull, HuntersMark, ReconSquad, Intimidation, Regeneration. 기존 조건·가용성 안내·공개 시점 보존 |
| 전투 snapshot에 관련 값이 있으나 공용 설명에서 미사용 | Knockback 거리, ImpactBolt 거리, DistanceDamageScale 거리별 비율, PiercingFalloff 관통 감소율, Necromancy 최대 소환수/첫 행동 지연, Roar 피격 한도. Roar의 밀치기 거리 등 나머지 숫자까지 모두 공개됐다는 뜻은 아님 |
| 현재 snapshot이 상태/존재 위주이거나 설명에 필요한 설정이 부족 | ArmorBreaker, Acceleration, CallOfBlood, CleanSweep, Cohesion, AnkleBreaker, Disarm, DustScreen, Entrenchment, FinishingShot, FocusedBombardment, GroundCompaction, Hunter, MoraleBoost, Sacrifice, SoloAction, StationaryFire, Taunt, TrackingShot, Vanguard: 비율·양·범위·주기·횟수·조건을 실제 적용 정의 기준으로 확인 |
| 별도 분류 필요 | Focus, MoveAttackDiscount, ChainExplosion 및 키와 표시명이 다른 Shotgun 등: 현재 가용성/퇴역 여부·키 매핑을 유지. PositionSwap, Leap, SteadfastWill, DoorBreaching, DoubleShot, KillChainField, LuckyBullet 등도 한글 수치와 고정 범위 표현을 확인 |

이 표는 현재 발견 목록이다. 구현 전에 각 대상의 **에셋 등록 여부 → 표시되는 화면 → 모든 수치 → 공개 출처 → 필요 변경**을 행별로 확정한다. 존재하지 않는 강화나 미지원 특성 동작을 새로 만들지 않는다.

### 2.3 오판하지 않을 경로

- 화약통·보급품 설명에는 토큰이 있고 상세 mapper는 원문을 전달하지만, `CombatInspectionFacts.CanInspectStructure`가 native 장애물을 상세 선택 대상에서 제외한다. 현재 상세창에 미치환 토큰이 노출된 버그로 단정하지 않는다. 환경 호버가 현재 소비 경로다.
- `DescriptionMaster` Excel/SO와 `DescriptionTable`은 존재하지만 `GetDescription`/`TryGetDescription`/`GetAllDescriptions`의 운영 소비 호출은 이번 검색에서 없었다. 이 테이블을 전체 설명의 현재 원본으로 전제하거나 임포터를 수정하지 않는다.
- `UnitCardVisualsSO.SynergyDescription`/`FunctionDescription`도 선언 외 소비가 검색되지 않았다. 현재 병과 설명이 동적으로 표시된다고 주장하지 않는다.

## 3. 제안하는 데이터·표시 계약

### D1. 규칙 수치는 Core, 문장 구성은 View

`적용된 Core 정의/현재 상태 → 기존 공개 snapshot 또는 읽기 계약 → 기능별 값 연결 → View 설명 틀 → 화면 문자열` 흐름을 따른다.

- View는 InGame DB나 Domain 구현에서 숫자를 읽지 않는다. `TraitKey`/`CharacterKey`만 보고 기본값·등급·강화를 추측하지 않는다.
- 문장에 필요한 값이 공개되지 않았으면 Core 요청으로 남긴다. 0·현재 기본값·다른 카드 값으로 메우지 않는다.
- 설정값은 현재 실행된 정의에서 투영한다. 활성 효과 잔량/대상 수명은 현재 상태에서 읽는다. 같은 숫자처럼 보여도 둘을 서로 대용하지 않는다.
- 성장·상점의 기존 Core 문자열은 값의 SSOT가 아니다. 새 전달값과 View 틀을 연결하는 단위로 이전하며, 전환 전 계약은 유지한다. UI 문장 자체를 Core의 새 숫자 저장소로 만들지 않는다.

### D2. 공통 치환과 기능별 값 연결을 분리

- 여러 영역에서 실제 필요한 **무상태 설명 템플릿 부품**을 `Systems/RuntimeSystem/Common/Descriptions/`에 둔다. 새 Framework System·루트 서비스·전역 Manager는 만들지 않는다.
- 공통 부품의 책임은 토큰 문법, 수치 문자열화, 잘못된 틀/빠진 값 검사뿐이다. 특성·아이템·지형 키, DB 조회, Core 규칙, 화면 수명을 알지 못한다.
- 특성, 환경, 아이템, 유물, 선택지 각각이 필요한 typed 공개값만 받아 토큰에 연결한다. 특성 종류별 변환과 등록 목록을 나눠 모든 특성을 하나의 거대 switch에 누적하지 않는다. 등록은 생성 시 확정한다.
- 덱·손패·전투 정보는 같은 특성 설명 변환을, 상점·가방·장착 HUD는 같은 아이템 효과 설명 변환을 재사용한다. 서로 다른 화면의 값 갱신 시점은 각 기존 모델 소유자가 유지한다.
- 기존 정상 동작 중인 상태 효과 `{0}`·훈련 비교·공용 화면 글을 공통화만을 위해 일괄 재작성하지 않는다. 필요한 경로에서만 동일한 오류/값 출처 원칙을 적용한다.

### D3. 설명 틀과 오류 규칙

- 신규/이전 설명에는 의미가 드러나는 `{피해량}`, `{피해증가율}`, `{지속횟수}` 같은 이름을 사용한다. 토큰과 값의 대응은 해당 기능의 연결 코드 한 곳이 원본이다. SO에는 숫자를 중복 저장하지 않는다.
- 기존 `{n}`은 해당 기능에서 의미를 아는 이전용 별칭으로만 지원한다. 기존 `{0}` 계열은 해당 positional formatter에 남겨 두고, 하나의 틀에서 두 문법을 임의로 섞지 않는다.
- 숫자는 문화권에 영향받지 않는 형식으로 변환하고 `%`·`칸`·`회`·`턴`·부호는 틀에서 명시한다. 실제 수명 단위(자기/상대/대상/관리 턴)를 유지한다.
- Rich Text 태그·줄바꿈·일반 문구를 보존한다. 명시적인 중괄호 escape(`{{`, `}}`)를 지원한다. 토큰에 계산식, 임의 코드, 리플렉션 경로를 허용하지 않는다.
- 템플릿 검증은 준비 시 문법/등록 토큰을, 표시 변환은 해당 context의 필수 값 존재/형식을 검사한다. 실패 시 대상 종류·키·필드·토큰을 포함한 `InvalidOperationException`으로 기존 오류 경계에 보고한다. 미치환 토큰이나 성공처럼 보이는 기본 숫자를 출력하지 않는다.
- 빈 설명이 허용된 기존 선택 기능은 계속 허용한다. 필수 토큰 누락과 선택적 설명 없음은 구분한다. 가시밭의 추가 방어 감소처럼 조건부 문구는 해당 기능이 공개값에 따라 선택하는 문장 조각으로 처리한다.

### D4. 읽기 시점과 개체 식별

- 전투는 현재 표시 중인 완료 사실과 같은 시점의 snapshot을 사용한다. 재생 중 설명만 최신 Buffer로 앞서가면 안 된다.
- 덱은 `OwnedId`별 현재 보유 조회값을 사용한다. 동일 `CharacterKey`의 두 카드가 다른 강화/등급이면 다른 설명이 나와야 한다.
- 훈련·증강·상점은 해당 offer/choice의 값을 사용한다. 선택 직전의 보유값을 구매 후 값으로 대신하지 않는다.
- 전투 밖에 현재 효과가 없으면 기본/설정 설명을 제공한다. 가짜 전투 상태나 잔여 턴을 만들어 넣지 않는다.
- 틀 분석 결과는 DB 연결 수명 동안 재사용할 수 있지만, 개체별 완성 문자열은 같은 키 하나로 전역 캐시하지 않는다. 매 프레임 전체 DB를 훑지 않고 기존 갱신 경계에서 생성한다.

### D5. 공개값이 부족한 범위

정식 요청 원본은 Core 인계 §64다. 전투/보유/선택지의 특성 설정, 아이템 효과 설정, 유물 효과 설정, 고지대·모래폭풍 등의 규칙 수치가 주된 대상이다. 새 공개 API의 구체 배치는 Core 작업자가 결정한다. 이미 공개된 값의 전달 누락과 미공개 값을 구분한다.

## 4. SSOT·SOLID·JMH/FW·독립성 검토

| 항목 | 설계 점검 |
|---|---|
| SSOT/P1~P7 | 규칙은 Core의 적용 정의/상태, 표현은 View 틀. 런타임 SO 쓰기·숫자 복제·InGame 직접 조회 없음. 현재/다음 비교의 단순 차이 표시는 기존 표시 책임을 유지 |
| S | 틀 치환, 종류별 수치 연결, 모델 갱신, UI 표시, Factory 조립을 구분 |
| O | 신규 특성/아이템은 해당 변환과 등록 항목을 추가. 공통 parser나 관련 없는 환경 변환 수정이 필요하지 않게 함 |
| L | 변환은 요구한 context의 값과 완성 문자열을 제공하며, 필수값 오류를 기본값으로 숨기지 않음. 기존 formatter의 같은 입력/출력 보장 유지 |
| I | 각 변환은 해당 typed snapshot과 틀만 사용. 전체 Buffer/DB/서비스 resolver를 공통 치환기에 전달하지 않음 |
| D | 기존 ReadSink/조회 계약과 View Query를 기존 소유자가 받아 연결. Core 구현 클래스나 씬 검색에 의존하지 않음 |
| JMH §2·23·26~28 | 새 상태 저장소·Manager·서비스 계층 없이 현재 여러 소비자의 중복 처리만 분리. 조립은 기존 Factory, 기능 내부 부품은 서비스로 등록하지 않음 |
| FW-001~004 | 새 서비스 등록/Export/루트 Builder 변경 없음. 공개 읽기 경계만 사용. `CombatUnitStatusFactory`의 기존 로컬 부품 조립을 유지 |
| FW-005~016 | 입력·UI 등록/이력·렌더 자원·이벤트·Pool·Queue·FrameworkRuntime 수명 변경 없음. 새 독립 UI를 만들지 않음 |

서비스 경로 점검: 상태 효과 변환은 `CombatUnitStatusFactory` 생성 → `CombatUnitStatuses` 내부 보관 → 정보/HUD 소비이며 상위 Export나 루트 재등록 대상이 아니다. 신규 설명 부품도 같은 로컬 조립 원칙을 따른다. Core 공개 읽기 계약이 추가되는 Phase에서는 생성·내부 등록·상위 Export·소비 경로를 실제 구현과 대조하며, 그 경로를 이번 설계만으로 확정하지 않는다.

| 변경 시나리오 | 현재 영향 | 제안 후 영향/공유 경계 |
|---|---|---|
| 한 특성 수치 문구 변경 | View SO, 성장 SO, 특수 formatter, 카드별 전달을 함께 조사할 수 있음 | 해당 틀·typed 값 연결. 같은 설명을 쓰는 카드/전투/선택지만 확인 |
| 신규 아이템 효과 설명 | 상점 문자열, 가방, 장착 HUD 각 경로 | 아이템 변환·등록·틀, 필요한 공개 계약. 세 소비 화면의 통합 확인 |
| 새 환경 값 추가 | `CombatEnvironmentText`의 수집과 종류별 치환이 섞임 | 해당 환경 변환과 조립, 환경 호버만 확인 |
| 공통 토큰 문법 변경 | 개별 Replace/string.Format마다 영향 다름 | 공통 치환 부품과 그것을 사용하는 모든 영역 확인. 공통 변경의 회귀를 기능 분리로 생략하지 않음 |

## 5. 승인·검토 지점과 검증 한계

- 사용자가 토큰 이름과 공개값을 설명 종류별로 등록하는 방향 및 후속 Phase 연속 진행을 승인했다. D1~D5에 따라 A, 공개된 특성 5종과 회복을 구현했다. 가시밭/끌어오기 문구는 SO로 이전했다. 미공개 설정을 요구하는 Core 문자열의 이전은 해당 공개 계약 확보 후 시행한다.
- Core 직접 수정 권한은 이번 요청에 포함되지 않는다. 필요한 공개값은 요청 문서에만 기록했다. 외부 전달하지 않았다.
- Test 코드를 작성하지 않는다. MSBuild·Unity 컴파일·재생·Test Runner를 실행하지 않는다. **컴파일 미확인.**
- 구현 후 사용자가 Unity에서 기본/강화/훈련 등급, 같은 정의의 서로 다른 개체, 전투 표시 시점, 선택지 전후, 빈 선택 설명, Rich Text/줄바꿈을 확인한다. 에셋 변경 Phase에는 기존 `DataBaseAssetTests`를 확인 항목에 포함한다.
- 커밋·push는 사용자가 한다. 진행 중인 보드 등장·씬·오디오·다른 작업자 변경은 이번 설계 작업에서 수정하지 않았다.

## 6. Phase A 구현 연결

실행 결과·정적 확인·사용자 확인 항목은 [계획 §9](../plans/2026-11-02-description-numeric-values-plan.md#9-phase-a-실행-기록-2026-11-02), 허용 토큰·작성 문법·등록 위치는 [토큰 작성 안내](../viewDocs/description-token-guide.md)를 따른다. 토큰 허용 목록의 원본은 `CombatEnvironmentTokenBindings`이며 작성 안내용 표와 별도 검증 목록을 코드에 중복 저장하지 않는다.

공통 부품 `DescriptionTemplate<T>`는 실제 사용하는 `DescriptionToken<T>` 등록에서 이름 검사와 값 읽기를 함께 준비한다. `T`는 공개 사실/설정의 참조형이고 수치 읽기는 `decimal`로 받아 정수의 값을 보존하고 소수도 표현할 수 있게 했다. 계산식·서식 지정자·리플렉션 속성 경로는 없다. 원본 수치나 완성 문자열을 전역 상태로 저장하지 않는다. 기존 환경 수집·호버 캐시는 유지한다.

## 7. 후속 구현 연결

[계획 §10](../plans/2026-11-02-description-numeric-values-plan.md#10-phase-be-연속-진행-기록-2026-11-02)과 [커버리지 목록](../viewDocs/description-numeric-coverage.md)에 현재 반영/잔여를 기록했다. 특성 5종은 `TraitDescriptionBindings`가 전투/보유 값 선택과 토큰 등록의 원본이며 `TraitDescriptionFormatter`가 SO 문장을 완성한다. 회복은 `RecoveryDescriptionBindings`가 효과 종류/설정량/예상량을 구분한다. 공통 parser에 Combat 의존성을 넣지 않았으므로 다른 씬/UI에도 같은 부품을 사용한다.

**후속 등록(계획 §13):** 특성 등록은 33종·59개로 확장했고 모든 설정은 `TraitSpec`에서 읽는다. 보유 `TraitSettings`와 전투 `TraitSettings.For(character)`를 기존 갱신 경계에서 전달한다. 전투 상세는 `CombatInspectionPresenter.Synchronize`가 같은 `CombatDisplayValues`의 불변 설정 참조를 받고 Clear/Release에서 해제하므로, 연출 중에는 최신 Buffer를 새로 읽지 않는다. 위압/재생의 기존 코드 수치 줄도 SO에 이전해 총 7종 본문이 토큰을 사용한다. 다른 고정 본문·산탄 목록·문자열·선택지 이전은 별도 잔여다.

보유/덱은 특성별 생성자 인자를 늘리지 않고 같은 `OwnedCharacterStatsSnapshot`을 전달한다. 실제 숫자는 formatter가 호출될 때 해당 개체의 값을 읽는다. 가시밭의 추가 방어 감소 문장은 `defenseReductionDescription`에 저장하고 공개된 감소율이 양수일 때만 추가한다. 이는 D3의 조건부 문장 조각 구현이다.
