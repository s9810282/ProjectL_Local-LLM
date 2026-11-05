# Combat·StageSelect UI 개편 설계 (2026-10-29)

- 상태: P1~P4 구현·컴파일 확인 완료. P4는 사용자 Play 확인까지 완료(계획 §13). 특성 획득 순서는 별도 Core 후속으로 요청 문서 인계(계획 §14). P5 담당 작업자 인계, P6 미착수.
- 근거: 이번 사용자 요청과 후속 답변, 현재 소스·Unity YAML. 과거 문서의 완료 상태를 이번 실행 결과로 간주하지 않는다.
- 작업 계획: [단계별 계획](../plans/2026-10-29-combat-stage-ui-revision-plan.md).
- 검토 기준: 저장소 AGENTS.md, Framework 개발 가이드 FW-001~016, 2026-09-30 설계 인계 §4·§4.6.
- 프로젝트 루트 아래 경로를 사용한다. 사용자 기존 변경은 그대로 보존한다.

## 1. 범위와 확정된 요구

| 대상 | 변경 후 |
|---|---|
| DeckUI | GlobalUIBase가 아닌 Combat 씬 UIBase. Parallel 루트 |
| Combat 진입 | 최신 요청: 표시 연결 후 DeckPannel 자동 열기. 일반 진입·개발 직행·재진입 모두 동일. 버튼은 기존 패널 열림/닫힘 규칙 유지 |
| DeckPannel / UnitInfo | 같은 Deck 부모 아래 Exclusive 관계 유지 |
| UnitCard | HP는 텍스트만. 이동/공격 비용 표시와 카드 HP 바 연출 제거 |
| UnitCard 특성 | InfoPannel 안 Ability1·Ability2. 특성별 아이콘, 없는 슬롯 비활성. 두 슬롯이 같은 AbilityPannel 사용 |
| AbilityPannel | 선택한 특성의 Icon·Name·Description을 열 때마다 함께 갱신 |
| 특성 표시 순서 | 최신 요구: 기본 특성은 1번, 나중 획득한 특성은 2번. n개도 획득 순서, 강화는 위치 유지. Core의 표시 순서 사실 제공 필요(계획 §14, 미반영) |
| UnitInfo | Name·레벨·카테고리·특성만 유지. 특성 목록은 2개 고정이 아닌 n개 |
| UnitInfo 특성 입력 | 각 슬롯 Hover에서 설명 표시, UnHover에서 닫힘. 미보유 슬롯 비활성 |
| StageSelect 대형 InfoPannel | 사용자 추가 답변: StageSelect에서 클릭할 때만 기존 InfoPannel 대신 사용할 예정. 이번 범위는 함수 준비뿐이며 실제 패널 구현·배치는 제외 |
| 전투 전 덱 편성 | 전투 노드 진입 시도 → 덱 편성 팝업. 확정 문구는 '전투 진입'. 덱 확정 수락 후 기존 스테이지 진입 요청 경로 실행 |
| 튜토리얼 | 사용자 추가 답변: 21번 단계의 UnitInfo 비용 강조만 제거. 다른 비용 안내 유지 |

공통 유닛 선택의 클릭 고정·다른 유닛 호버로 고정 변경 금지·동일 유닛 재클릭 해제 규칙은 유지한다.
편성 카드의 교체/제거, 전투 덱 카드의 배치/드래그, 취소·다시 평가와 Core 덱 규칙을 이번 표시 개편으로 변경하지 않는다.
Core 로직·FieldCamera·Architecture·옛 Missing Script 에셋은 변경 범위 밖이다.

## 2. P1 구현 전 확인한 참조와 불일치

검사 파일: Assets/Scenes/Combat.unity, StageSelect.unity, Boot.unity의 대상 컴포넌트와 Assets/ProjectL/Prefabs/UI/DeckUI.prefab, UnitCard.prefab, UnitCardUI.prefab, Deck/DeckEditUI.prefab.
추가로 여섯 파일(Combat·StageSelect 및 위 네 프리팹)의 0이 아닌 로컬 fileID가 같은 파일에 존재하는지 검사했다.

| 확인 위치 | 현재 상태 | 처리 방향 |
|---|---|---|
| Combat.unity UnitInfo / CombatUnitInfo | moveCost·attackCost·moveCostRoot·attackCostRoot가 모두 fileID 0 | 삭제된 UI 계약을 제거. Configure는 현재 비용 텍스트를 필수 요구하므로 실행 시 실패할 경로 |
| Combat.unity UnitInfo / UnitCardView | portrait·infoDescription·attackText·defenseText·infoButton·infoPanel·healthText·HP fill 등이 0 | 의도적 삭제. 전투 UnitInfo를 공용 카드의 빈 필드들로 표현하지 않도록 표시 책임 분리 |
| 같은 UnitInfo | Ability1·Ability2 오브젝트는 있지만 기존 abilityButton은 Ability1만 참조, openPanelsOnHover=0 | 둘 다 명시적 슬롯으로 연결하고 hover를 활성화. 기존 단일 필드 유지로는 요구 충족 불가 |
| UnitCard.prefab | healthFill·healthSubFill=0, healthText는 연결됨 | 빈 바를 복구하지 않고 카드 연출 코드 제거. HP 텍스트는 필수 참조로 유지 |
| UnitCard.prefab | 기존 abilityButton 1개, InfoPannel·AbilityPannel 연결 | 요청한 2개 슬롯 및 InfoPannel 내부 배치로 이관 필요 |
| DeckUI.prefab | openButton·closeButton·cards 연결. DeckUI 프리팹 사용은 확인한 Scenes/Prefabs 검색에서 Combat에 존재 | 빈 참조보다 상속·등록·열기 호출 경로 변경이 필요 |
| Bootstrap | PrepareCombatDeckUI와 OpenCombatUI에서 Deck 유지 루트 열기. 일반 배치 진입에서는 카드 자식도 자동 열기 | 부모 준비 순서 보존. P2에서 자동 열기를 제거했으나 최신 요청에 따라 표시 연결 후 한 곳에서 다시 열기 |
| CombatUnitInfo.Render | 최초 Deployment 관측에서도 카드 자식 자동 열기 | 최초 진입과 이후 국면 전환을 구분해야 함 |
| GlobalUIPresentation.ShowStageSelect | 글로벌 Deck 목록을 조회해 버튼 복원 | Combat 전용화에 맞춰 StageSelect의 Deck 의존 제거 |
| TutorialViewFocusLocator | 삭제된 MoveCostRoot·AttackCostRoot 접근 | 21번 설정에서 UnitInfoActionCosts 제거, 해당 표시 접근 경로 정리 |

0이 아닌 로컬 fileID 누락은 위 여섯 파일에서 발견하지 않았다. 이는 전체 참조 정상 판정이 아니다.
외부 GUID·프리팹 상속/override의 최종 참조·Missing Script·실행 중 바인딩 검증은 Unity에서 별도로 확인한다.
null인 visuals·deadPanel·checkPanel 등은 용도별 선택 계약이 있으므로 일괄 오류로 취급하지 않는다.
현재 git 상태에는 사용자 수정 Combat.unity와 미추적 SCDream7 SDF 폰트 2파일이 있다. 이를 되돌리거나 덮어쓰지 않는다.

## 3. UI 등록과 수명 설계

| 등록 단위 | UIId / 부모 / Policy | 생성·등록·바인딩 | 개폐·해제 |
|---|---|---|---|
| Combat Deck 루트 | Deck(9) / None / Parallel | Combat 씬의 DeckButtonUI를 UIBase로 변경. 씬 BindScene에서 수집·등록·주입 | IUISystemService로 Combat 진입 준비 시 열기. Combat 밖 등록 유지 없음 |
| DeckPannel | UnitCard(11) / Deck / Exclusive | 기존 DeckCardList : UIGroup 유지. 4개 UnitCardUI는 그룹 멤버로만 바인딩 | OpenChildUI(Deck, UnitCard), 닫을 때 그룹·드래그·호버 정리 |
| UnitInfo | CombatUnitInfo(37) / Deck / Exclusive | 기존 Combat 씬 등록 유지. 전용 표시 위젯은 이 화면이 소유 | 선택에 따라 OpenChildUI / CloseUI. DeckPannel과 상호 배타 |
| 편성 팝업 | DeckEdit(32) / StageSelect / Exclusive | 기존 DeckEditUI·StagePopupViewLifetime·PopupInputSession 유지 | IUISystemService·기존 수명 경로. 목적만 편집/전투진입으로 구분 |
| 카드 내부 Info·Ability | 개별 UIId 없음 | 이미 등록된 카드/그룹의 내부 위젯 | 소유 화면의 Open/Hide 안에서만 내부 활성 상태 변경 |
| UnitInfo 특성 설명 | 개별 UIId 없음 | UnitInfo 내부 위젯. 슬롯 수 n, 설명 패널은 하나 | hover로 표시. 부모 닫힘·대상 전환·재바인딩에서 해제 |
| StageSelect 대형 Info | 이번에는 등록 대상 없음 | 함수만 준비. 오브젝트·UIId·가짜 참조를 추가하지 않음 | 실제 구현 시 독립 패널 여부·UIBase 등록을 별도 설계 |

Deck 루트는 UIBase·Parallel이면서 Combat 안에서는 기존 버튼 접근성을 유지하도록 OpenUI(Deck, true)로 연다.
이 true는 UISystem의 이력 제외 루트 열기 계약이며 GlobalUI 상속·씬 간 보존과 별개다. DeckPannel·UnitInfo는 일반 자식 이력에 남는다.
뒤로가기는 자식을 닫고 DeckButton을 남긴다. 씬 재바인딩에서는 일반 씬 UI 등록과 열린 상태가 제거된다.

진입 순서는 기존 SceneNodeOrder를 따른다.
1. BindSceneUI(-200): 씬 UI 등록·계층 검증.
2. PrepareCombatDeck(90): Deck 부모를 열고 진입 초기 상태를 준비.
3. CombatViewAttach(100): 표시 연결. 최초 Deployment라는 이유만으로 덱 패널을 열지 않는다.
4. OpenSceneUI(200): 공통 표시 연결 후 OpenChildUI(Deck, UnitCard)로 DeckPannel을 연다. 최신 사용자 요청에 따른 P2 후속 변경이다.

진입 준비(90)의 자식 닫기는 바인딩 전 정리이며, 진입 완료(200)의 패널 열기가 최종 초기 표시를 결정한다. 최초 Deployment 관측에서는 중복 개방하지 않는다. 일반 Open 호출 때마다 무조건 초기화해 선택된 UnitInfo를 닫지 않는다.
이후 Combat→Deployment 전환에서의 기존 덱 복귀는 유지한다. 최초 국면 관측과 실제 국면 전환을 구별한다.
튜토리얼의 명시적 카드 공개 요청은 해당 단계 요구로 계속 허용하며, 진입 기본 상태와 구별한다.

### 서비스 조립·조회 추적

- 생성: GlobalSystemFactory가 UISystem을 한 번 생성한다.
- 내부 등록: UISystem.ExportServices가 Global 내부 Builder에 IUISystemService를 등록한다.
- 보관: GlobalSystemGroup.UIServiceCommand.
- 상위 Export: GlobalSystemGroup.ExportServices는 IGlobalSystemGroupService와 수명 모듈을 내보낸다. UI를 루트에 재등록하지 않는다.
- 소비: StageMapViewSystem은 SystemGroupsService.Global → UIServiceCommand를 가져와 기존 화면 수명에 전달한다. Combat도 기존 Factory의 UI 의존성 전달을 유지한다.
- UI 인스턴스 등록은 서비스 등록과 별개다. 같은 UIId 그룹 멤버를 루트 UI로 중복 등록하지 않는다.
- 새 특성 위젯·진입 보류 조정자는 내부 협력 객체다. 편의용 서비스나 루트 resolver 조회를 새로 만들지 않는다.

이는 FW-001(인터페이스/중복 등록)과 프로젝트 계층 규약을 따로 점검한 결과다. 현재 확인한 UI 경로에서 루트 재등록을 추가할 필요가 없다.

## 4. UnitCard와 특성 표시

### 4.1 표시 책임

- UnitCardUI: Buffer·보유 능력치·View DB 조회. 전투 수치는 완료된 표시 시점의 사실을 따른다.
- UnitCardView: 이름·레벨·그림·HP 텍스트·공격/방어 텍스트·카드 내부 정보 표시.
- 공용 특성 변환: 기존 UnitCardTraitContent의 '첫 Icon + 합친 Names/Descriptions'를 특성별 항목 목록으로 변경한다.
- 특성 항목은 Key·Icon·Name·Description을 포함한다. 원본 상태는 Buffer, 외형은 View DB다.
- UnitInfo 표시와 특성 슬롯은 공용 카드 전체가 아니라 필요한 표시 계약만 사용한다.

기존 가시밭·표식·끌어오기 동적 설명, 현재/일부 사용 불가 안내를 항목별로 보존한다.
특성 미보유(정상)와 특성은 있지만 필수 View 데이터/슬롯 구성이 누락된 경우(구성 오류)를 구분한다.
아이콘 Sprite 미입력이 허용된 기존 View 데이터는 특성 부재로 오인해 슬롯을 숨기지 않는다.

### 4.2 두 슬롯과 공용 설명

- 카드에서는 Ability1·Ability2를 직렬화 배열/슬롯으로 연결한다. 데이터 개수로 각 슬롯 활성 상태를 정한다.
- 아이콘을 작동시킬 때 해당 인덱스의 항목 전체를 설명 위젯에 전달한 다음 패널을 표시한다.
- 두 슬롯은 같은 설명 위젯을 쓴다. 이전 유닛·이전 특성의 Icon/Name/Description이 섞이지 않도록 한 번에 갱신한다.
- InfoPannel과 그 안 특성 설명은 서로 배타적이지 않다. 기존 Panel.Info / Panel.Ability 단일 enum으로 번갈아 닫는 처리는 분리한다.
- 카드의 기존 클릭/Submit 열기·토글과 hover 옵션은 보존한다. UnitInfo의 설명은 사용자 지시대로 hover 종료 시 닫으며 클릭으로 고정하지 않는다.
- 늦게 도착한 Ability1의 Exit가 이미 Ability2가 연 설명을 닫지 않도록 현재 표시 슬롯을 대조한다.
- 슬롯 비활성·interactable 상실·부모 Hide·재바인딩·대상 변경은 hover를 한 번 정리한다. 선택 고정 상태는 특성 tooltip이 소유하지 않는다.
- UnitInfo는 활성 유닛의 n개 항목을 모두 수용한다. 표시 슬롯을 재사용하고 필요 시 목록 갱신 때만 확장한다. Update/hover 콜백에서 Instantiate하지 않는다.
- UnitCard의 명시된 두 슬롯보다 많은 특성 데이터가 올 때 임의 절단·합치기를 하지 않는다. 해당 화면의 지원 수 계약을 검사하고, 실제 요구가 2개를 넘으면 설계 변경을 먼저 요청한다.

### 4.3 삭제된 수치·연출

ShowDetails/ShowDeckDetails와 호출부에서 카드 표시용 AttackCost·MoveCost 인자를 제거한다. 상세 문자열에도 비용을 남기지 않는다.
HP 텍스트는 유지하고 healthFill·healthSubFill·카드의 HealthFillAnimation·FixedUpdate 연결을 제거한다.
공용 HealthFillAnimation 자체, 필드 유닛 HUD·메인 캐릭터 HP 바·전투 계산 비용은 이 작업 때문에 삭제하지 않는다.

## 5. UnitInfo 간소화와 튜토리얼

CombatUnitInfo의 선택·슬라이드·Buffer 표시 시점·DeckPannel 전환 책임은 유지한다.
표시 위젯은 Name·Level·Category·특성 목록/설명만 요구하도록 전용화한다. CombatUnitInfoView를 내부 UIWidgetBase로 두고, 공용 UnitCardView 의존을 제거했다. 사용자 추가 답변에 따라 동일 간격의 Flexible Grid를 사용하고 공간이 차면 줄바꿈한다. 행 증가 시 화면 높이를 확장해 첫 행과 제목의 상단 상대 배치·하단 경계를 유지한다.
삭제된 portrait·능력치·비용·HP 필드를 null로 둔 UnitCardView를 계속 공유하는 방식은 피한다.
실제 필수 참조는 상태 변경 전에 검사하고 UIId·오브젝트·필드명을 포함한 InvalidOperationException을 낸다.

TutorialDefinition_1-1의 stepKey 21은 현재 focusTargets에 ActionCost(8), UnitInfoActionCosts(11)를 갖는다.
11만 제거하고 8·설명·실습 진행·다른 비용 UI는 유지한다.
TutorialViewFocusLocator의 삭제 영역 접근과 전용 보조 메서드를 정리한다.
기존 직렬화 enum의 번호를 재배치하지 않는다. 삭제된 대상 값이 다른 설정에 남아 있으면 구성 오류로 검출한다.
다른 작업자의 튜토리얼 변경과 겹치므로 실제 수정 전에 최신 diff를 확인하고 변경 근거를 남긴다.

## 6. StageSelect 대형 InfoPannel 함수 준비 — 담당 작업자 인계

2026-10-29 최신 요청으로 이 세션의 구현 대상에서 제외한다. 아래 합의와 연결 지점은 [P5 인계 메모](../handoffs/2026-10-29-stage-select-large-info-p5-handoff.md)로 넘긴다. 함수·입력·패널은 추가하지 않았다.

사용자 보완 요청을 따른다. 이번에는 'StageSelect의 클릭으로 큰 정보 표시를 요청하는 함수'의 진입점·인자 계약만 준비한다.
이름은 구현 시 확정하되 예시는 RequestLargeInfoPanel(CharacterInstanceId)다.
구현 예정임을 한국어 주석으로 명시하고 실제 UI 열림·필수 참조 검증·빈 오브젝트 생성을 흉내 내지 않는다.

실제 패널이 없으므로 이번 Phase에서 카드 클릭을 미완성 경로로 전환하거나 기존 InfoPannel을 강제로 닫지 않는다.
StageSelect 클릭만 이 진입점을 사용한다는 향후 연결 지점을 기록한다. Hover·Combat은 기존 정보 경로를 유지한다.
편성 카드 본체의 교체/제거 입력과 정보 열기 입력은 합치지 않는다. 실제 개폐·입력 연결은 큰 패널 구현 단계에서 함께 확정한다.
함수만 준비하는 Phase의 검증은 컴파일·호출 계약 확인이며 큰 패널 동작 완료로 보고하지 않는다.

## 7. 전투 진입 전 덱 편성

### 7.1 진입 의도와 실제 명령을 구분

현재 StageMapInteraction.RequestEntry는 노드 클릭 즉시 StageViewCommandData.SelectStageNode를 Submit한다.
전투 노드는 이 Submit보다 앞에서 편성 확인을 거쳐야 한다. 수락 뒤/씬 전환 뒤 팝업을 열지 않는다.

입력은 명시적 진입 의도 수신 계약에 chapter/node를 전달한다.
별도 조정자가 Buffer의 StageProgressData.StageType으로 전투 여부를 읽고 다음처럼 처리한다.
- 비전투 노드: 기존 Submit 경로.
- 전투 노드: 대상·Run/맵·현재 연결 식별을 보류하고 기존 편성 팝업을 전투진입 목적으로 연다.
- 증강 선택 대기: 기존 증강 우선 차단을 유지한다.
- 처리 중/이동 중/다른 모달/설정 Layer: 기존 수명·입력 제한에 맞춰 새 진입을 받지 않는다.

실제 진입 가능 여부·덱 적합성은 Core가 최종 판정한다. View에서 규칙을 재계산하지 않는다.
보류 상태에는 아직 명령 번호가 없다. 기존 StageEntryIdentity는 Submit 후 영수증 비교용이므로 가짜 번호로 재사용하지 않는다.

### 7.2 편성 목적과 확정

편성 열기 문맥은 일반 편집 / 전투 진입 두 가지다. 문구는 이 목적에서 파생하며 UI가 문자열로 행동을 판단하지 않는다.
일반 편집의 문구와 확정 이후 동작은 유지한다.
전투 진입 목적에서만 confirm label에 '전투 진입'을 표시한다. TMP_Text 참조를 명시적으로 연결·검증한다.

기존 Read → Evaluate → Confirm 계약과 draft·버전 검증·재평가·편성 조작을 재사용한다.
덱 컨트롤러는 노드·씬 이동을 소유하지 않는다. 성공/취소를 명시적 수신 계약으로 진입 조정자에 전달한다.
Button 입력 연결에 Action/클로저/UnityEvent를 추가하지 않는다.

### 7.3 성공·취소·실패 수명

1. 덱 확정이 Accepted일 때만 같은 확정을 다시 호출하지 않는 상태로 전환한다.
2. 현재 Run·맵·대상·연결이 유효한지 확인한다. 보류 중 상태가 바뀌었으면 자동 진입하지 않는다.
3. 기존 popup 수명으로 닫아 모달 Layer를 돌려준다. Dispose/Hide 콜백 재진입과 소유권 교체를 확인한다.
4. 보류 대상을 단 한 번 소비하고 기존 StageView Submit을 수행한다.
5. 기존 StageMapEntryObserver·Job/Plan·수락 확인·노드 이동 연출·Combat 준비 경로가 계속 담당한다.

덱 Confirm이 거절되면 편성 팝업에 머물고 기존 실패·재평가 표시를 유지한다.
닫기/취소/뒤로가기는 보류 대상을 버리고 진입 명령을 쓰지 않는다.
씬 이탈·Scope 교체·Run/맵 변경도 보류 대상·수신 연결을 정리한다.
확정 후 진입 거절은 덱 확정을 자동 취소하거나 Confirm을 재전송하지 않고 기존 진입 실패 표시를 사용한다.
연타·Submit 중복·늦은 콜백은 단일 확정·단일 진입을 보장한다. 재시도는 새 사용자 진입 의도로 시작한다.

이번 범위는 StageSelect의 운영 노드 진입이다. 개발 Combat 직행에 편성 팝업을 강제로 넣지 않는다.
기존 DeckEdit 팝업과 설정 창의 Canvas 정렬/입력 Layer 충돌 가능성은 이 Phase에서 재현·검증하고 관련 부분만 조정한다.

## 8. 원칙 점검과 변경 독립성

| 기준 | 설계 결과 |
|---|---|
| SSOT | 특성·HP·덱·노드 사실은 Buffer/Core, 외형은 View DB. 보류 대상은 진입 조정자 하나, 설명 슬롯 상태는 표시 위젯 하나 |
| SRP | 조회, 카드 표시, 특성 변환, tooltip 표시, 진입 보류, 씬 조립을 각각의 변경 이유로 분리 |
| OCP | 특성 n개는 목록/재사용 슬롯 확장. DeckEdit에 StageType별 switch와 씬 이동을 누적하지 않음 |
| LSP | UIBase 등록·Open/Hide 계약 준수. UnitInfo를 빈 필드뿐인 공용 카드로 억지 대체하지 않음 |
| ISP | UI 입력·특성 표시·진입 의도·편성 완료는 필요한 좁은 계약만 전달. 내부 객체를 서비스화하지 않음 |
| DIP | 포인터 입력과 팝업은 명시적 수신 계약에 의존. Factory/Scope가 구체 객체 연결 |
| FW-001/002 | UI 서비스 중복 등록 없음. 별도 시스템/모듈을 무의미하게 추가하지 않음 |
| FW-009 | 등록된 외부 개폐는 IUISystemService. 내부 슬롯 활성화만 소유 화면이 직접 처리 |
| 프로젝트 계층 | Global Context를 통해 조회. 루트 UI 서비스 재등록·직접 조회 우회 없음 |
| 입력 | UIInputBinding·IUIInputReceiver와 pointer/Submit. 입력 Layer 소유권은 기존 InputSystem/PopupInputSession |
| 실패 처리 | 구성 오류는 필드/대상 명시 예외. 미보유·정상 취소·반복 해제와 구분 |

변경 예:
- 특성 설명 수정은 항목 변환과 그 검증에 한정한다. Deck 클릭·노드 이동·Core 계산 수정이 필요하지 않다.
- UnitInfo 배치/필드 변경은 전용 위젯·씬 연결 중심이며 UnitCard의 HP 표시와 분리된다.
- 전투 전 팝업 순서 변경은 진입 조정자·연결에 한정하고 카드 특성·전투 입력을 다시 구현하지 않는다.
- 공통 특성 모델/카드 계약 변경 때는 실제 소비자(UnitCardUI·DeckEditUI·UnitInfo와 검색으로 확인한 소비자)의 표시 회귀가 필요하다. 공용 경계를 바꾸면서 무회귀를 주장하지 않는다.

## 9. 검증 한계와 후속

초기 설계 조사는 정적 코드·YAML 읽기로 진행했다. P1·P2와 P3 작업 중 Unity 컴파일은 성공했다. P3 최종 소스는 동일 Unity C# 컴파일러·Bee 응답 파일로 런타임 및 기존 소비 어셈블리 컴파일 오류 0을 확인했다. 실제 Play 확인은 미실행이다. Hierarchy는 계획 §11, 최신 진입 정책과 P3의 참조·분기 확인 및 한계는 §12에 기록했다.
구현 단계마다 해당 범위만 검증하고 멈춘다. 새 결정이 필요하면 구현 전에 사용자에게 묻는다.
Unity 관련 테스트는 실제 체크아웃의 기존 테스트 어셈블리와 InternalsVisibleTo를 먼저 확인한다.
과거 문서가 언급한 Framework 테스트 폴더/어셈블리는 현재 파일 목록에서 확인되지 않았으므로 존재한다고 가정하지 않는다. 새 asmdef를 임의로 만들지 않는다.
