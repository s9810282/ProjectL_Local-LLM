# 선택 유닛 HUD 변경 — 2026-10-30

## 적 본부 정보 선택 (2026-11-01)

사용자 승인: 적 메인 건물을 클릭하면 기존 UnitInfo와 선택 HUD를 연다. 이름·초상(UnitInfo)·HP·공격력은 공개된 값으로 표시하고, 레벨·병과·방어력·특성은 숨긴다. 아래 캐릭터 전용 설명에 이 범위를 추가한다.

설계·Task 범위는 한 Phase다.

1. `CombatSelectionState`가 완료 표시의 구조물 목록도 빌린다. 살아 있는 적 `Structure_Headquarters`의 모든 점유 칸에서 정보 선택을 허용한다. 자격 판정은 `CanInspectStructure` 한 곳이다. 아군 건물·장애물은 이번 대상이 아니다. `SelectedActorId/Cells`는 계속 아군 캐릭터만 반환한다.
2. `CombatUnitInfo`는 구조물 전용 표시를 같은 선택 전환 연출에 연결한다. 값은 `StructureCombatFacts`의 Health/Definition과 기존 UnitVisual에서 읽으며, 가짜 CharacterData나 없는 방어력·레벨 데이터를 만들지 않는다. 다음 일반 유닛 선택에서는 숨겼던 항목을 복원한다.
3. `FieldUnitPresenter`가 적 본부의 HUD 이름·공격력을 전달한다. 선택 InfoPanel의 HP 숫자와 연출은 기존 체력 표시를 공유한다. HoverPanel의 Shield 전체를 숨기는 명시적 `shieldRoot`, UnitInfo의 `levelRoot/categoryRoot`를 기존 프리팹 객체에 연결한다. 새 Inspector 연결 작업은 없다.
4. EditMode에서 선택 유지·파괴/국면 전환·정보 전용 선택과 실제 프리팹의 누락 항목 숨김/복원을 확인한다. 실제 전투 클릭·패널 위치/hover 연출은 별도 재생 확인 대상이다.

등록·수명: UnitInfo는 기존 CombatUnitInfo(Deck 자식, Exclusive), 선택 InfoPanel/HoverPanel은 기존 런타임 등록 쌍(Parallel)을 재사용한다. 선택 변경·사망·씬 해제는 기존 UISystem 개폐/등록 해제 경로를 따른다. 숨김 대상은 등록된 화면 내부 위젯이다. 새 서비스·UIId·이벤트 구독은 추가하지 않는다. 생성/등록/조회는 위 문서의 기존 Factory → Group → Global Context 경로를 유지하며 루트 재노출과 동일 Builder 중복 등록을 추가하지 않는다.

규약 검토: 선택 ID와 표시값 원본을 복제하지 않는다(SSOT). 구조물 표시 함수와 선택 자격을 분리하고 기존 UI 수명 계약을 유지한다(S/O/L). 필요한 구조물 사실만 기존 표시 경계로 전달하며 Core/규칙 DB를 직접 조회하지 않는다(I/D, FW-001~009 경계 유지). 변경 전에는 구조물 선택과 패널 내용 연결이 없었고, 변경 후에는 선택·해당 표시·프리팹 참조만 수정한다. 입력 액션·이동/공격 구현·Factory 조립은 변경하지 않는다. 공통 경계인 선택 해제와 캐릭터 복귀는 회귀 대상이다. 필수 위젯 루트 누락/잘못된 소유 계층은 대상 UIId와 필드명을 포함한 예외로 거절한다.

구현 및 이번 Phase 검증 완료. Unity 6000.0.73f1 재컴파일 성공. Unity Test Runner API로 `CombatStructureInspectionTests` EditMode 7/7 통과(다중 점유 칸, 행동 주체 제외, 아군/장애물/사망 제외, 피해 후 유지·파괴/국면 변경 해제, 실제 DeckUI/HUD 프리팹의 숨김/복원). 임시 실행 도구는 제거했다. 변경 파일 `git diff --check` 통과. 실제 전투에서 건물 클릭 → UnitInfo/InfoPanel 개방·HP 감소·hover 위치·일반 유닛으로 재선택하는 Play 확인은 미실행이다.

> **줌 비례 크기 인수 안내(2026-10-31, HZ3):** InfoPanel/HoverPanel과 아이콘/피해 숫자는 같은 HUD 루트 배율을 공유한다. 줌에 따른 크기 조정을 위해 추가 연결이나 Canvas 모드 변경은 필요 없다. 기존 고정 아이템/버프 기본 표시의 사용자 확인은 유지하며, 줌 상태의 패널 배치·hover/우클릭·해제는 아직 별도 미확인이다. 확인 목록/결과는 `docs/superpowers/plans/2026-10-31-unit-hud-zoom-scale-plan.md`의 사용자 Unity 확인 표 한 곳에 기록한다.

> **현재 고정 아이템 배치 정정(2026-10-31):** 카테고리는 HP바 옆 유지. HP바 왼쪽 위에 별도 BuffIcon 인스턴스를 배치하고 UnitHUD의 필수 **Item Icon**에 루트 Image를 연결한다. 이 슬롯은 상태 레이아웃 밖이며, 장착 없으면 숨긴다. BuffIconRoot는 아이템 오른쪽의 버프→디버프 전용 영역이다. 아래 최초 UH1/UH2 배치 설명보다 이 정정과 설계 §8/계획의 현재 사용자 연결 목록이 우선한다. 실제 연결·Unity 재생은 사용자 확인 대기다.

> 2026-10-31 UH1 변경: 카테고리는 InfoPanel 안에서 기본 HUD의 체력바 왼쪽 위로 옮기며 `UnitHUD.categoryImage`에 연결한다(필수, 같은 HUD의 InfoPanelMask 밖). BuffIconRoot는 아이템→버프→디버프만 표시하고 격파 목표/보유 특성은 제외한다. 아래 10-30 기록과의 차이는 `docs/superpowers/specs/2026-10-31-unit-hud-status-layout-design.md` 및 `docs/superpowers/plans/2026-10-31-unit-hud-status-layout-plan.md`의 UH1 연결 안내를 따른다. UH2 다중 행 배치 코드 반영: 내용은 좌하단 고정, 첫 아이템은 최상단 첫 칸, 버프→디버프 순으로 행을 채운다. BuffIconRoot는 Pivot=(0,0)·세로 고정 Anchor·한 칸 이상 Width가 필요하며 Height는 코드가 쓴다. 가로/세로 간격은 UnitHUD에서 지정한다. 프리팹 연결·실제 재생은 사용자 확인 대기다.

기본 HPBar는 수치를 표시하지 않는다. Turn 순서·현재 턴 화살표·Actioned 표시는 제거했다. 전투 진입 때 임의 아군을 자동 선택하지 않으며 기존 수동 선택을 유지한다. Deck의 기존 hover 입력 계약은 호환을 위해 남기되 HUD 화살표를 표시하지 않는다.

## 표시 데이터

- 선택 원본은 `CombatSelectionState.SelectedEntityId`다. `CombatSceneDisplay`가 완료 사실 갱신과 클릭 선택 후 같은 ID를 `UnitHUDLayer`에 전달한다. 적도 정보 대상으로 선택할 수 있다.
- Category 이미지는 `CombatDisplayTables.Categories`의 Category Visual Icon이다. 공격력은 Core의 `CombatEffectiveStatsCalculator.AttackPower`를 사용한다.
- **2026-11-02: Shield/sValue는 같은 Buffer 사실의 `TraitEffects.ActiveShieldTotal`을 표시한다.** 이전 유효 물리 방어력 임시 표시는 아래 보호막 HPBar Phase에서 교체했다. Core 상태·흡수 규칙은 변경하지 않는다.
- 2026-11-01: HUD의 행동 슬롯을 제거했다. 행동 기회 표시·참조 검증과 HUD로의 `ActionUse` 전달도 제거했다.
- 기본 HPBar와 InfoPanel HPBar는 UnitHUD의 동일한 감소·SubFill 지연·예상 피해 맥동 결과를 공유한다. HPText의 CurrHP/MaxHP는 InfoPanel 안에서만 표시한다.

## 등록·개폐·수명

| 등록 단위 | UIId | 부모 | Policy | 개폐 |
| --- | --- | --- | --- | --- |
| 기존 HUD Layer | UnitHUDLayer (38) | 없음 | Parallel | 기존 고정 표시 경로 |
| 선택한 한 유닛의 InfoPanel | UnitHUDInfoPanel (44) | 없음 | Parallel | Layer → IUISystemService.OpenUI(id, true) |
| 그 유닛의 HoverPanel | UnitHUDHoverPanel (45) | UnitHUDInfoPanel | Parallel | InfoPanel의 pointer enter/exit → OpenChildUI/CloseUI |

`CombatUnitHUDFactory`가 공용 Canvas/Layer를 만들고 `FieldUnitPresenter`가 HUD 프리팹을 복제한다. 필수 참조는 Layer 등록 전 검증한다. InfoPanel/HPText/Mask/Category/공격력·방어력 텍스트 누락은 대상 UIId와 객체가 포함된 예외로 거절한다. 선택 전 데이터 준비와 반복 해제는 정상 경로다.

반복 프리팹의 두 패널은 `UIBase.RegisterWithScene = false`로 씬 자동 수집에서 제외한다. 선택되어 화면에 보이는 한 쌍만 `IUIRuntimeRegistry`로 등록·바인딩한다. 선택 변경·화면 이탈·반환·해제 때 HoverPanel부터 등록을 해제한다. 같은 씬 재바인딩에서는 현재 런타임 등록 쌍만 다시 수집한다. Layer 숨김은 UISystem 등록부 순회 중에도 호출될 수 있으므로 패널을 닫기만 하고 등록부를 변경하지 않는다. Layer 재표시 후 선택이 유효하면 다시 연다.

실제 SetActive는 각 패널의 UIBase Open/Hide가 담당한다. InfoPanel은 열릴 때 InfoPanelMask의 RectMask2D 너비를 0에서 원래 너비까지 확장한다. HoverPanel은 잘리지 않도록 Mask 밖의 HUD 자식에 두고, UI 부모 관계는 InfoPanel로 등록한다. InfoPanel 화면 중심이 위쪽 절반(1·2사분면)이면 우측 하단, 아래쪽 절반(3·4사분면)이면 우측 상단에 배치한다. RectTransform 모서리를 기준으로 계산하므로 패널 크기·피벗·HUD 배율·카메라 투영 변화를 따른다. Hover exit·비활성·가시성 상실·재선택·해제에서 hover를 정리한다.

선택된 HUD 루트는 같은 Canvas의 마지막 sibling으로 올려 InfoPanel·HoverPanel이 다른 유닛 HUD에 가려지지 않게 한다. 선택 변경·새 HUD 등록·렌더 전 위치 갱신에서 순서를 유지하며, 이미 마지막이면 Transform 순서를 변경하지 않는다. 풀에서 재사용되는 HUD도 동일한 등록·렌더 경계를 따른다. 선택 유닛의 기본 체력바·상태 아이콘·피해 숫자도 함께 앞으로 올라온다. Canvas 정렬 값(-10)과 씬 UI·팝업 사이의 우선순위는 유지한다.

## 서비스 조립·계층 확인

- 생성 주체: `GlobalSystemFactory.CreateWithInputRegistration`이 기존 UISystem을 생성한다.
- 내부 등록: `UISystem.ExportServices`가 Global 내부 Builder에 IUISystemService를 등록한다. 두 패널은 서비스 Builder가 아닌 UISystem의 UI 등록부에만 등록한다.
- Group 보관·상위 Export: `GlobalSystemGroup`이 내부 `GlobalSystemServiceKey.UI`를 읽어 `UIServiceCommand`로 보관한다. 상위에는 기존 IGlobalSystemGroupService만 Export한다.
- 소비 조회: 기존 Global Context → `CombatDisplayServices.From(global).UI` → `CombatSceneDisplayFactory` → Layer 순서다. 루트 resolver의 UI 직접 조회나 하위 서비스 재등록을 추가하지 않았다.
- 상위/루트 Builder: `RunTimeSystemFactory`는 `GlobalSystemFactory.CreateWithInputRegistration(...).ExportServices(rootBuilder)`를 호출한다. GlobalSystemGroup의 Export는 IGlobalSystemGroupService와 모듈만 등록한다. 소비자는 `CombatViewSystem.PostBindService`에서 `SystemGroupsService.Global`을 조회해 표시 조립에 전달한다.
- 같은 Builder 중복 등록(FW-001)과 Global 하위 서비스를 루트로 재노출하는 구조 변경 모두 추가되지 않았다.

## 이번 변경 규약 검토 결과

2026-10-30, 이번 HUD 변경 파일과 그 생성·등록·조회·해제 경로를 한 번 검토했다.

- FW 필수 계약: 이번 변경에서 같은 Builder의 서비스 중복 등록이나 등록 키 충돌을 발견하지 않았다. 화면 인스턴스는 서비스 Builder 대신 기존 UI 등록부에서 관리한다.
- 프로젝트 서비스 계층 규약: Global 내부 UI 서비스를 루트에 재노출하거나 루트에서 직접 조회하는 우회를 발견하지 않았다. 위의 Factory → Group → 루트 Export → Global Context 소비 경로를 확인했다.
- 프로젝트 UI 등록·입력 규약: 두 패널은 UIBase/Parallel로 등록되며, 등록 이후 외부 개폐는 IUISystemService를 거친다. 등록 전 Prepare의 숨김은 초기 표시 정리다. Action 이미지는 패널 내부 위젯이므로 개별 등록 대상이 아니다. 새 Button 리스너·입력 이벤트 구독·포인터 콜백의 할당/개별 raycast를 추가하지 않았다. 기존 상태 Button은 UIInputBinding/IUIInputReceiver를 유지한다.
- 발견·수정한 프로젝트 검증 규약 미흡: 필수 참조의 null은 검사했지만 다른 HUD의 InfoPanel/HoverPanel/수치 참조를 연결한 경우를 충분히 거절하지 않았다. 같은 HUD와 각 패널 내부의 소유 계층을 검사하고, UIId·대상 객체·필드가 포함된 InvalidOperationException으로 표시 변경 전에 거절하도록 보완했다. HoverPanel도 Prepare 전 Open을 명시적으로 거절한다.
- 권장사항: 삭제된 화살표의 Deck hover 계약은 기존 호출자 호환을 위해 무동작으로 남아 있다. 필수 규약 위반은 아니며 이번 수정에서 관련 없는 Deck 입력 전체를 재구성하지 않는다.
- 검토 후 이번 범위에서 남은 필수 규약 위반은 발견하지 않았다. 실제 Play 화면과 EventSystem 실행 확인은 아래 미완료 검증과 구분한다.

## Hover 배경의 필드 입력 통과

InfoPanel은 Hover를 받기 위해 Prepare에서 background.raycastTarget을 켠다. 기존 CombatPointerMapper는 GraphicRaycaster의 모든 hit를 입력 차단으로 취급했으므로, 패널 위에서 드래그 시작·이동 경로의 취소 영역 판정·드래그 종료가 막힐 수 있었다. Inspector에서 배경 Raycast Target을 꺼도 Prepare가 다시 켜므로 해결되지 않았다.

이를 위해 UI Base에 `IUIWorldPointerPassThrough` 표시 계약을 추가했다. InfoPanel은 준비가 끝나고 활성화된 동안 이 계약을 제공한다. 필수 background는 InfoPanel과 같은 객체의 Graphic으로 검증한다. CombatPointerMapper의 기존 HitsUi 한 곳에서 Raycast가 맞은 객체의 계약을 확인하고, 통과 배경만 건너뛴 뒤 남은 hit를 계속 검사한다. 부모를 검색하거나 HUD/Canvas 전체를 면제하지 않으므로 자식 Button·상태 아이콘·뒤에 겹친 다른 UI는 계속 필드 입력을 막는다.

UI의 EventSystem Raycast·pointer enter/exit는 유지한다. 입력 차단 판단은 기존 전투 입력 경로에서 수행하며, 새 액션맵·이벤트 구독·개별 컨트롤 raycast·포인터 콜백의 할당은 추가하지 않는다. 드래그·선택·취소 영역이 공유하는 판정이므로 관련 호출자의 일관성을 유지한다. HoverPanel 개폐는 기존 IUISystemService 경로를 유지한다.

규약 검토 결과: 이 계약은 서비스가 아닌 UI의 표시 특성이며 새 MonoBehaviour나 UIId를 만들지 않는다. Factory 생성 → Global 내부 등록 → GlobalSystemGroup 보관/상위 Export → Global Context 소비 경로는 위의 서비스 계층 확인과 동일하다. 서비스 Builder 등록·루트 재노출·직접 resolver 조회를 추가하지 않아 FW 필수 계약과 프로젝트 서비스 계층 규약 위반을 발견하지 않았다. UI 등록·수명과 InputSystem의 액션맵 소유권도 유지한다.

## 검증

런타임 어셈블리와 `UnitHUDInfoPanelTests`의 외부 C# 컴파일은 성공했다. 테스트는 행동 상태 4종, Category·공격력·임시 Shield 값, HP 연출 공유, EventSystem pointer dispatch, 선택 전환·전체 숨김·재표시·등록 해제, 씬 재바인딩, 네 사분면 배치, Mask 확장, 필수 참조 누락을 다룬다.

선택 HUD의 우선순위가 뒤늦게 생성·재등록된 HUD 뒤에서도 유지되는 회귀 테스트와 다른 HUD로 연결된 참조를 거절하는 테스트를 추가했다. 관련 런타임·테스트 코드는 다시 컴파일 오류 0으로 확인했다.

필드 입력 통과 수정에는 실제 EventSystem Raycast에서 InfoPanel 배경이 검출되면서 필드 가림 판정은 false가 되는지, pointer dispatch로 HoverPanel이 정상 개폐되는지, 배경 뒤의 다른 UI와 패널 자식 Button이 여전히 차단되는지, Button의 왼쪽 클릭·Submit이 UIInputBinding을 통해 전달되는지 확인하는 회귀 테스트를 추가했다. 수정 후 런타임·테스트 코드 컴파일은 오류 0이며, Unity 실행 통과와 실제 드래그 재생은 아직 확인하지 않았다.

Unity batch EditMode 실행은 동일 프로젝트가 다른 Unity 인스턴스에 열려 있어 시작하지 못했다. **테스트 실행 통과 및 Play 시각 검증은 아직 확인하지 않았다.** 열린 Unity의 Test Runner에서 `ProjectL.Tests.Framework.UnitHUDInfoPanelTests`를 실행하고, 선택 전환·이동/공격 후 행동 아이콘·Mask 등장·화면 상하 이동 중 HoverPanel 배치를 확인해야 한다.


## 고정 아이템 사용자 확인 (2026-10-31)

부모 BuffIcon 루트 Image를 Item Icon에 다시 연결한 뒤 정상 작동한다고 사용자가 보고했다. 기본 연결/동작 확인 완료. 현재 HUD.prefab의 Description Panel/Text는 비어 있어 아이템·상태 아이콘 설명은 열리지 않는다. 유닛 상세 수치는 선택 InfoPanel 호버로 여는 별도 HoverPanel이다. 세부 입력/해제 사례의 개별 실행 확인은 별도로 남긴다.


## 버프 아이콘 사용자 확인 (2026-10-31)

BuffIconRoot의 Pivot/Width 수정 뒤 버프 아이콘 정상 표시를 사용자가 확인했다. Item Icon의 부모 BuffIcon 연결 수정 후 정상 동작 확인과 함께 기본 연결/표시는 확인 완료다. 에디터 자동 시험 및 개별 hover/다중 행/풀 수명 조합은 별도 미확인이다.


## 일반 줌 비례 크기 사용자 확인 (2026-10-31)

줌인·줌아웃 시 HUD가 함께 커지고 작아지는 기본 동작은 사용자 확인 완료다. 패널 위치/hover·아이템 우클릭·해상도·풀 수명의 개별 실행 여부는 별도 미확인으로 유지한다. 기록 원본은 zoom-scale-plan의 확인 표다.

## 보호막 누적 Fill — HUD Phase 1 (2026-11-02)

사용자 확인(2026-11-02): 보호막 HPBar가 잘 동작한다고 보고했다. 기본 동작의 Unity 사용자 확인을 인수한다. 아래의 개별 경계·예상 표시·재사용 사례 전체를 실행했다는 증거와는 구분한다. 이어서 요청한 UnitInfo 확장 기록은 [UnitInfo 변경 기록](../Hud/CombatUnitInfo.md)의 같은 날짜 보호막 Phase에 둔다.

### 설계·Task·범위

사용자 확정: H=현재 HP, M=최대 HP, S=유효 보호막, D=Max(M, H+S)로 계산한다. HP Fill은 H/D, Shield Fill은 (H+S)/D로 왼쪽부터 채운다. Shield → SubFill → Fill 순서로 겹쳐 그려 보호막 중 HP와 겹치는 부분을 가린다. D=0이면 두 비율은 0이다. S=0이면 Shield Image를 비활성화해 예상 HP의 투명도 연출 아래에 보호막색이 나타나지 않게 한다.

Task는 (1) 확정/예상 값 전달과 프리팹 조사 (2) UnitHUD의 보호막 참조·비율·수명 처리 (3) 기본 HPBar 및 같은 결과를 복사하는 InfoPanel HPBar 연결 (4) 정적 검사와 Unity 확인 인계다. 이 Phase는 HUD부터 구현하며 Deck의 CombatUnitInfo에 보호막 표시를 추가하는 후속 Phase는 시작하지 않는다.

### 구현·수명

- `SetUnit`이 이미 받는 Buffer의 CharacterCombatFacts에서 HP/최대 HP와 `TraitEffects.ActiveShieldTotal`을 함께 전달한다. 이는 `CharacterData.StatusEffectSnapshots`의 ShieldEffectSnapshot.RemainingEligibleBudget에 투영되는 것과 같은 유효 잔량이다. View에서 출처별 합산·유효성 판정을 하지 않는다. `SetHealth(current, maximum, shield = 0)`은 기존 두 인자 호출도 유지하며 구조물·Clear는 보호막 0을 쓴다.
- 분모는 정수 덧셈 오버플로를 피하도록 double로 계산하고 매번 같은 원본에서 파생한다. 분모가 같으면 기존 HP 감소 → SubFill 지연을 유지한다. 최초 표시·최대 HP/분모 변경·회복·숨김 후 재표시는 즉시 맞춘다. 보호막 누적 Fill은 확정 합계 비율을 즉시 반영하며 별도의 보호막 감소 잔상은 이번에 추가하지 않는다.
- 사용자가 추가한 기본 Shield Image를 재사용하고 Fill Origin을 Left로, 형제 순서를 SubFill 뒤에 가려지는 위치로 조정했다. InfoPanel에는 같은 색의 Shield Image를 추가해 기본 바와 같은 FillAmount/색/활성 상태를 복사한다. 각 바의 기존 크기·배경·테두리와 사용자가 숨긴 HoverPanel 초기 상태를 유지했다. 두 shieldFill 참조는 필수다. 누락·잘못된 부모/형제 순서·Filled 설정·Sprite 누락은 대상 UIId와 필드가 포함된 예외로 거절한다.
- 공격 미리보기의 AttackTargetPreview에는 예상 잔여 HP만 있고 예상 보호막 잔량은 없다. 실제 보호막과 실제 D를 유지하고 HP 예상 구간만 표시한다. S>0에서는 HP SubFill이 불투명한 상태로 명도를 맥동해 그 아래 누적 Shield가 HP 손실 구간에 비치지 않게 한다. S=0의 기존 Alpha 맥동은 유지한다. 예상 보호막 소모는 계산하지 않으며 미리보기 해제·확정 갱신 때 실제 비율·색으로 복원한다.
- HoverPanel Shield/sValue도 FieldUnitPresenter에서 같은 실제 보호막 잔량을 전달한다. 이전 물리 방어력 임시 표시는 제거했다.

### 경계·규약·독립성

- SSOT: 전투 사실은 Buffer, 유효 보호막 합계는 Core가 공개한 불변 사실이다. `_currentShield`는 기존 HP 캐시와 같은 표시 시점의 캐시이며 규칙·변경 이벤트·매 프레임 Buffer 조회를 만들지 않는다. HP/SubFill 알고리즘은 기존 HealthFillAnimation을 그대로 쓴다.
- SOLID: UnitHUD가 표현 비율과 연출 수명, InfoPanel이 복사 표시, 프리팹이 Image 참조 조립을 담당한다(S). 기존 공용 연출을 수정하지 않고 보호막 Image를 조합한다(O). UIWidgetBase/InfoPanel의 Open/Hide 보장과 기존 두 인자 SetHealth 호출을 유지한다(L). 기존 좁은 수치 전달·IUISystemService 계약만 쓰며 새 서비스·인터페이스를 만들지 않는다(I/D).
- 등록·서비스: CombatUnitHUDFactory가 Layer/Canvas를 만들고 FieldUnitPresenter가 기존 HUD 프리팹을 생성한다. UI는 GlobalSystemFactory 생성·Global 내부 IUISystemService 등록 → GlobalSystemGroup.UIServiceCommand → CombatDisplayServices.From → 조립 지점 → Layer의 AttachUISystem 경로를 유지한다. UIId.UnitHUDLayer(38), 선택 InfoPanel(44)·자식 HoverPanel(45)의 Parallel 정책과 Layer의 동적 RegisterRuntimeUI/OpenUI/해제 수명은 변경하지 않는다. 추가 Image는 내부 위젯이며 별도 등록·루트 Export·루트 직접 조회가 없다. FW 필수 계약과 프로젝트 계층 규약에 새 변경은 없다.
- 독립성: 비율·보호막 표현 변경은 UnitHUD, 복사 표시와 참조 조립은 UnitHUDInfoPanel/HUD.prefab에 한정한다. 상세 숫자 원본 교체 때문에 FieldUnitPresenter의 해당 전달 인자만 변경했다. 선택·입력·줌·상태 아이콘·Factory·Core·Framework 공용 코드는 수정하지 않는다. 남는 공통 경계는 기존 HP 연출과 InfoPanel 복사·UI Hide 수명이다.

### 검증·인계

- 정적 확인: 두 shieldFill 직렬화 참조와 Image 설정, Shield → SubFill → Fill 순서, fileID 중복·객체 유실 여부, 기존 사용자 변경 보존을 확인했다. CopyHealth 호출부를 세 인자 계약으로 함께 맞췄다. Test 코드는 작성·수정하지 않았고 MSBuild·컴파일·재생은 수행하지 않았다. **컴파일 미확인**.
- Unity 확인 기준(최대 HP 100): H=40/S=0 → HP 0.4·Shield 숨김, H=40/S=30 → HP 0.4·Shield 0.7(회색 40~70%), H=40/S=80 → HP 약 0.3333·Shield 1, H=100/S=50 → HP 약 0.6667·Shield 1. 보호막만 감소할 때 HP 숫자가 유지되는지, 최대 HP 이하/초과 경계와 회복·보호막 소멸을 확인한다.
- 이어서 같은 분모의 HP 피격 잔상, 예상 HP 맥동/취소/확정, 선택 패널과 기본 바 일치, 구조물·풀 재사용·숨김/재표시에서 보호막 잔상이 남지 않는지 확인한다. 예상 보호막 소모는 미구현이므로 HP가 유지되는 보호막 전용 피격의 예상 소모 구간은 표시하지 않는다.
- 코드·프리팹 Phase 완료 후 사용자 Unity 리뷰 지점에서 멈춘다. 커밋·push하지 않는다.
