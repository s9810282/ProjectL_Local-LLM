# UnitCard 공용 상세 패널 연결 (2026-11-02)

## 현재 연결

- 카드 본체는 UnitCardView가 표시하고, UnitCardUI가 기존 Buffer·View DB에서 값을 읽는다. 카드 내부 InfoPannel·AbilityPannel과 관련 필드·특성 버튼·개폐 API는 제거했다.
- InfoButton의 클릭·Submit만 공용 상세를 연다. 호버는 개폐·대상 전환에 관여하지 않는다. 같은 카드 재클릭은 닫고 다른 카드 클릭은 내용을 전환한다. 카드 본체의 배치 선택·드래그는 유지한다.
- Combat Canvas의 ScrollContentFrame은 기본 비활성이며 CombatCardInfoUI(UIId.CombatCardInfo=47, 부모 UnitCard, Exclusive)로 등록한다. 부모 목록/Deck 조상을 유지하며 다른 가지의 Exclusive UI는 닫는다. 개폐는 IUISystemService를 거친다.
- Combat 씬 DeckUI 인스턴스의 DeckCardList.infoPanel override가 공용 상세를 연결한다. 프리팹을 다른 씬에서 새로 사용하면 해당 씬의 공용 상세를 연결해야 한다. ScrollContentFit.viewport는 Combat Canvas RectTransform으로 연결했다.
- 현재 체력·적용 공격력/방어력·이동/공격 거리·레벨·병과와 특성을 같은 조회에서 전달한다. DeckEdit의 기본 수치·EXP 대기·특성 표시 위젯을 공유한다. 특성 개수 상한은 없다. 4번째부터는 3번 슬롯을 같은 위치에 복제해 그 뒤에 겹쳐 숨기고 내용을 적용한다. 남는 복제본은 비워 숨기며 다음 표시에서 재사용한다.
- 상세 위젯의 필수 참조는 등록 전에 검증한다. 목록 닫기·대상 교체/삭제·씬 이탈은 상세 선택도 해제하며, Close·뒤로가기도 UISystem 이력을 따른다. 상세 열기/닫기에 선택 포커스를 보관·복원한다.
- 손패와 로비 위젯은 SetInfoInputEnabled(false)를 사용한다. 공용 상세의 대상/선택은 카드마다 복제하지 않으며, 표시 데이터는 Buffer 변경 때만 갱신해 같은 카드 입력에서 특성 배열/문구를 다시 만들지 않는다.

설계·Task·검증 기록: [UnitCardSharedInfo.md](UnitCardSharedInfo.md). Phase 1·2 구현 완료, 컴파일 미확인. Test 코드·MSBuild·Unity 재생은 수행하지 않았다.

## 아래는 이전 내부 패널 방식의 이력 (현재 연결에는 적용하지 않음)

UnitCardView는 받은 값만 표시하고, UnitCardUI는 Buffer·View DB·기존 보유 능력치 조회 경로에서 값을 읽습니다. UnitCardUI의 열기 정책은 Parallel입니다.

UnitCardView Inspector의 주요 참조:

- Level Text: LevelText. 표시값은 현재 데이터의 레벨을 사용합니다.
- Portrait / Name Text: 초상화와 이름.
- Attack Text / Defense Text: InfoBox 아래 Attack / Defense TMP Text.
- Info Description: InfoPannel 아래 Description. 기존 Details의 전체 능력치 설명을 표시합니다.
- Info Button / Ability Button: 각 설명 패널을 여는 버튼.
- Info Panel / Ability Panel: 카드의 별도 자식 패널. 각 루트에 Button을 두어 클릭·Submit으로 닫습니다.
- Ability Icon / Name / Description: 기존 Trait Visual 조회 결과를 표시합니다. 전투 중 TraitStatuses, 비전투 중 보유 능력치의 TraitKeys를 사용합니다.
- Ability Button Icon: Ability 버튼 하위 Icon Image. 능력 패널과 동일한 아이콘을 사용하며, 스프라이트가 없어도 Image와 오브젝트의 표시·입력 상태를 유지합니다.
- 아이콘 유무와 관계없이 Ability 버튼으로 기존 이름·설명을 확인할 수 있습니다. 특성 설명이 비어 있으면 Ability Description에 '특성이 없습니다.'를 표시합니다. 빈 카드의 설명은 공백으로 유지합니다.
- Health Fill / Health Sub Fill / Health Text: HP 바와 현재체력 / 최대체력 문구.
- Fill Seconds Per Percent / Sub Fill Seconds Per Percent: 공용 HealthFillAnimation에 전달할 HP 1%당 감소 시간.

이름은 카드 Visuals SO의 이름, UnitVisual DB 이름, EntityKey 순서로 결정합니다. 시너지 내용은 더 이상 표시하지 않습니다. 능력 아이콘·이름·설명은 기존 TraitVisualTable에서 조회하며 InGame DB를 직접 읽지 않습니다.

Open Panels On Hover가 켜져 있으면 Hover로 열고 이탈 시 닫습니다. 클릭하면 고정하며, 다른 버튼 Hover로 고정을 바꾸지 않습니다. 패널 자체를 클릭하거나 같은 버튼을 다시 클릭하면 닫습니다. 현재 UnitCard.prefab은 두 패널이 카드 전체를 덮으므로 이 옵션을 꺼 클릭 방식으로 사용합니다.

HP 감소는 Fill을 먼저 줄이고 SubFill을 뒤이어 줄입니다. 최초 표시·최대체력 변경·회복·다른 대상 연결은 즉시 맞춥니다. HP Text는 애니메이션 비율과 무관하게 실제 현재체력 또는 최대체력이 변경될 때 즉시 갱신하며, 정수 정밀도를 유지합니다.

입력은 UIInputBinding으로 연결합니다. Info·Ability 버튼과 패널 클릭은 카드 본체 배치 입력으로 전파되지 않습니다. Canvas에 GraphicRaycaster와 EventSystem이 필요합니다.

## 사망 표시 (2026-10-19)

- Dead Panel은 UnitCard.prefab의 DeadPanel을 연결하며 기본 비활성입니다. Buffer의 CharacterData.Health.IsDead를 그대로 표시하고 HP로 사망을 재판정하지 않습니다. UnitCardUI와 CharacterData를 받는 UnitCardView.ShowDetails에서 전달합니다.
- 스테이지 이탈의 CharacterBuffer.EndBattle이 전투 개체를 비우면 UnitCardUI.Refresh가 표시를 해제합니다. 카드 비활성화·ClearDetails·다른 대상 표시에서도 초기화하고, 다시 열면 최신 Buffer로 복구합니다.
- 등록 단위는 기존 DeckCardList(UIId.UnitCard, 부모 Deck, Parallel)와 그 UIGroup 멤버 UnitCardUI를 유지합니다. 생성·등록·바인딩·Factory·Group Export·Global Context 조회 경로는 변경하지 않습니다. DeadPanel은 카드 내부 상태 위젯이므로 UIBase나 UIId를 추가하지 않고 소유자 UnitCardView가 활성 상태를 담당합니다. 사망 표시가 없는 카드 구성은 참조 생략을 허용하되, 연결한 참조가 카드의 자식이 아니면 구성 예외를 던집니다.
- UISystem.OpenChildUI의 Exclusive는 부모·조상 및 Parallel UI를 보존하지만 다른 Exclusive 이력을 닫습니다. DeadPanel은 그 경로를 호출하지 않으므로 다른 화면·Info·Ability를 닫지 않습니다.
- SSOT는 Buffer, 조회와 표시는 기존 UnitCardUI/UnitCardView 책임 분리를 유지합니다. 새 서비스·입력·구독 없이 사망 표시만 변경하므로 FW 등록·수명 계약과 기존 입력 정책에 영향이 없습니다. 다른 기능 구현을 수정할 필요가 없습니다.
- 검증: VS Roslyn으로 ProjectL.Systems와 ProjectL.Tests.Framework를 스크래치에 컴파일하여 모두 오류 0을 확인했습니다. 사망 표시의 초기값·다른 패널 보존·비활성화/재사용 초기화 및 프리팹 참조 검증을 UnitCardViewTests에 추가했습니다. Unity EditMode 실행과 실제 사망 → 스테이지 이탈 재생 확인은 대기합니다.

## 배치 완료 표시 (2026-10-20, 단일 Phase)

- Task: CheckPanel 프리팹 구조·현재 Buffer 경로 확인 → UnitCardUI에서 배치 단계/Field 여부 전달 → UnitCardView 내부 표시·초기화와 프리팹 참조 연결 → 컴파일·정적 확인. 구현 Phase 완료.
- CheckPanel은 배치 단계(StagePhase.Deployment)이면서 해당 CharacterData.Area가 Field인 경우에만 켠다. 전투·비전투·배치 취소·개체 없음에서는 끈다. UnitCardUI.Update는 기존 캐릭터/보유 능력치 변경 감지에 배치 단계 변경 감지를 더하므로, 캐릭터 객체가 유지된 채 전투로 전환돼도 해제한다.
- UnitCard.prefab의 CheckPanel을 UnitCardView.checkPanel에 연결했다. Awake·OnDisable·ClearDetails·다른 종류 표시 시 초기화하고 재열기 때 최신 Buffer로 복구한다. 다른 정보·능력 패널은 닫지 않는다. 참조 생략은 표시가 없는 손패·전투 정보창에서 허용하며, 연결된 참조가 카드 자식이 아니면 구성 예외를 던진다.
- SSOT: 배치 단계와 위치는 CombatState/Character Buffer가 원본이며, _displayedDeployment는 갱신 감지 캐시다. SOLID·독립성: 판정/조회는 UnitCardUI, 활성 변경은 UnitCardView, 조립은 프리팹 참조가 맡는다. 배치 표시 수정에 Core·CombatUnitInfo·입력·Factory 구현 변경은 필요 없다.
- UI 등록 단위·UIId·부모·Policy·생성/등록/바인딩·재사용/씬 해제 수명은 위 사망 표시와 같다. CheckPanel은 기존 등록 카드의 내부 위젯으로 별도 UI 등록이나 서비스·Builder·Export·resolver 조회를 추가하지 않았다. 기존 Global Context와 UISystem/UIGroup 바인딩 경로를 유지한다(FW-001/002/008/009).
- 검증: ProjectL.Systems MSBuild Compile 오류 0, 변경 소스 경고 없음. CheckPanel 프리팹 참조와 기본 비활성을 정적으로 확인했다. Unity EditMode 실행·실제 배치/배치 취소/전투 전환/재열기 확인은 대기한다. 커밋·push하지 않았다.
