# StageSelect 대형 InfoPannel P5 인계 — 2026-10-29

상태: 담당 작업자에게 넘기도록 사용자 요청. 이 문서만 준비했으며 외부 전달은 미실행이다. 이 세션에서는 함수·패널·입력 연결을 추가하지 않았다.

## 합의된 요구

- StageSelect 씬에서 클릭한 경우만 기존 InfoPannel 대신 대형 InfoPannel을 사용할 예정이다. Hover·Combat에서는 기존 정보 경로를 유지한다.
- 기존 추가 답변은 “현재 미구현이니 함수만 작성”이다. 실제 화면·레이아웃·개폐 구현까지 확대하려면 담당 작업자가 사용자와 범위를 확정한다.
- 정보 카드의 클릭 고정, 동일 유닛 재클릭 해제, 다른 유닛 Hover로 고정 변경 금지는 유지한다. 편성 본체의 교체/제거 입력과 정보 열기 입력은 구분한다.

## 현재 연결 지점

- Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs: AttachCard·ShowCard가 편성 카드와 보유 카드의 입력/표시를 연결한다. 보유 유닛은 CharacterInstanceId로 식별한다.
- Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardView.cs: InfoSlot 입력·ToggleInfoPanel·ShowInfo가 작은 InfoPannel을 표시한다. 전투·로비·손패도 공용 표시를 사용하므로 StageSelect 전용 요청을 이 클래스의 전역 동작으로 바꾸지 않는다.
- Assets/ProjectL/Prefabs/UI/UnitCard.prefab 및 Deck/DeckEditUI.prefab: 공용 카드·StageSelect 편성 프리팹이다. P3에서 InfoPannel 내부 Ability1·2와 하나의 AbilityPannel 연결을 적용했다.
- P4의 CombatUnitInfoView는 전투 UnitInfo 전용이며 Name·Level·Category·n특성/Hover를 담당한다. StageSelect 대형 정보창의 소유자로 사용하지 않는다.

## 담당 작업자의 다음 작업

1. StageSelect 정보 요청의 대상 ID·수신 계약을 확정하고 함수만 작성한다. 예시 이름은 RequestLargeInfoPanel(CharacterInstanceId)이며 확정 API는 아니다.
2. 실제 대형 패널이 없는 상태에서는 클릭을 미완성 경로로 전환하거나 기존 정보 패널을 강제로 닫지 않는다. 향후 StageSelect 클릭 연결 지점을 한국어 주석으로 남긴다.
3. 실제 패널 구현 시 등록 단위/UIId·부모·Policy·씬 바인딩·재사용·해제 수명을 별도로 확인한다. 독립 개폐 화면은 UIBase/UISystem을 기본으로 하고, 카드 내부 위젯이면 기존 소유 화면의 수명을 따른다.
4. UnitCardView의 공용 특성 표시·전투 덱 드래그·편성 교체/제거 기능과 분리해 검토한다. 불필요한 새 서비스·루트 조회·Inspector UnityEvent 연결을 만들지 않는다.

설계 원본은 [UI 개편 설계 §6](../specs/2026-10-29-combat-stage-ui-revision-design.md#6-stageselect-대형-infopannel-함수-준비--담당-작업자-인계), 상태는 [단계별 계획 §7](../plans/2026-10-29-combat-stage-ui-revision-plan.md#7-p5--대형-info-함수-준비-인계)을 따른다. P6 전투 전 편성 확인·스테이지 진입은 별도 단계이며 이 문서가 해당 기능 구현을 승인하지 않는다.
