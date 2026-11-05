# DeckEdit 병과 슬롯과 미편성 목록

## 공용 상세 특성 슬롯의 개수 제한 해제 (2026-11-02)

UnitCard와 공유하는 DeckSlotInfoAbilities에서 특성 상한을 제거했다. 3개 이상은 기존 3칸 영역을 사용하고, 4번째부터는 3번 슬롯을 같은 위치에 복제해 원본 뒤에 겹쳐 숨긴다(사용자 배치 지시). 각 복제본에도 해당 특성 내용을 적용하며, 부족할 때만 추가하고 남는 복제본은 비워 숨겨 재사용한다. DeckEdit의 선택·개폐·UIId/부모/Parallel·입력·서비스 조립은 변경하지 않는다. 설계·Task·SSOT/SOLID/FW·수명과 확인 항목은 [UnitCardSharedInfo.md의 Phase 2](UnitCardSharedInfo.md#phase-2--특성-개수-제한-해제-2026-11-02)를 따른다. 컴파일 미확인. Test 코드·MSBuild·Unity 실행은 수행하지 않았다. 아래 이전 단계의 3개 상한/초과 예외는 이 변경 전 이력이다.

## 편성 슬롯 EmptyLabel 병종 표시 (2026-11-02)

사용자 후속 지시: 정상적으로 유닛이 있는 슬롯도 EmptyLabel을 켜고 해당 슬롯 병종(Main·Heavy·Rider·Cannon)을 표시한다. 변경 불가인 편성 슬롯은 기존 `병종 변경 불가`, 빈 슬롯은 기존 `병종 - 비어 있음`과 변경 불가 안내를 유지한다. Render와 교체 모드 전환은 같은 SetChangeMode 표시 경로를 공유하며, 문자열은 고정 슬롯 병종을 원본으로 Bind에서 준비한다. 기존 정책 테스트의 편성 슬롯 표시 기대값도 현재 요구에 맞췄다.

작은 표시 수정 단계로 DeckEditSlotView·기존 관련 테스트·이 문서에 한정한다. 병종 SSOT는 기존 DeckEditSlotLayout이고 새로운 규칙·조회·입력·서비스는 만들지 않는다. 슬롯 표시 책임 안에서 수정하고 기존 수신자 계약·UIWidgetBase 수명을 유지한다(SOLID). 생성·등록·상위 Export·Global Context 소비 및 DeckEditUI의 UIId/부모/Policy·개폐 경로를 변경하지 않는다(JMH·FW). 필터·상세창·Core·다른 View 영역의 수정은 필요하지 않다.

검증: Unity 6000.0.73f1의 Roslyn·기존 응답 파일로 ProjectL.Systems와 ProjectL.Tests.CoreIntegration을 임시 출력 경로에 컴파일해 통과했다. diff 공백 검사 통과. 기존 메인 정책 테스트의 활성화·병종 문구 기대값을 갱신했으며, EditMode 실행·Play 화면 확인은 수행하지 않았다.

## Status·Categories Text 제거 (2026-11-02)

사용자가 삭제한 Status(상태 메시지)·Categories(덱 후보 병과별 수량) Text에 맞춰 DeckEditUI의 statusText·categoryText 직렬화 필드, Render/Clear의 텍스트 갱신과 불필요해진 TMPro using을 제거했다. 실제 프리팹의 빈 참조 두 개와 StageDeckEditUISetup의 생성·연결 코드도 제거했다. 표시 모델의 Status/Categories 데이터 계약과 Core 평가·CanEdit/CanConfirm 판단은 유지하지만 현재 화면에서는 해당 문구를 표시하지 않는다.

작은 정리 단계로 화면 참조 → 프리팹 → 생성 도구 → 문서 순서로 정리한다. 기존 표시 모델을 원본으로 유지하고 별도 상태를 추가하지 않는다(SSOT). 삭제된 표현 의존성만 줄이고 기존 입력·표시 인터페이스를 유지한다(SOLID). Factory 생성/Global 내부 등록 → GlobalSystemGroup 상위 Export → StageMapViewSystem의 Global Context 조회 → DeckEditPopupController의 UISystem 개폐 경로와 UIId.DeckEdit/부모 StageSelect/Exclusive 정책은 그대로다(JMH·FW). 목록 필터·슬롯·상세창 기능과 서비스 조립을 변경하지 않아 관련 없는 기능 변경·검증 범위를 늘리지 않는다.

검증: Unity 6000.0.73f1에 포함된 Roslyn과 기존 응답 파일로 Runtime(ProjectL.Systems)·Editor(Assembly-CSharp-Editor)를 임시 출력 경로에 컴파일해 모두 통과했다. 화면·생성 도구·프리팹에 statusText/categoryText 참조가 남지 않고, 삭제한 두 Text 오브젝트도 프리팹에 없음을 확인했다. 기존 Owned 필터의 네 버튼과 선택 Sprite 참조 유지, diff 공백 검사를 확인했다. EditMode·Play 실행은 하지 않았다.

## 보유 목록 병과 필터 (2026-11-02)

OwnedLabel의 All·Heavy·Rider·Cannon 버튼은 현재 표시 모델의 Owned 중 해당 병과만 표시한다. All은 병과 제한 없이 모두 표시한다. 새로 열기·Clear에서 All로 초기화하며, 열린 화면의 편성 재평가·목록 갱신에서는 선택을 유지한다. 선택 버튼은 `11_0`, 나머지는 기존 미선택 Sprite를 쓴다. 필터 변경은 드래그·교체 모드를 정리하고 가로 스크롤을 처음으로 돌린다. 고정된 공용 상세창의 선택은 필터로 바꾸지 않는다. 결과가 없으면 모든 Owned 슬롯을 Clear/Hide한다.

단일 구현 단계: (1) DeckOwnedFilter에 버튼 참조·선택 상태·병과 일치·Sprite 표시를 모으고, (2) DeckEditUI.Prepare에서 구성 검증과 UIInputBinding을 한 번 연결하고 RenderOwned에서 기존 슬롯을 재사용하며, (3) 실제 프리팹 참조와 변경 범위를 확인한다. 필터는 화면 내부 기능이므로 추가 UIId·서비스로 등록하지 않는다. 버튼의 왼쪽 클릭/Submit·interactable 처리는 기존 UIInputBinding을 사용한다.

설계 검토: 병과의 원본은 기존 DeckEditCardModel.Category이며 InGame DB·Core·초안은 수정하지 않는다(SSOT). 단일 선택 상태는 DeckOwnedFilter만 소유한다. 기존 IDeckEditInput/IDeckEditDisplay 계약을 유지하고 좁은 IUIInputReceiver로 입력을 연결한다(SOLID). 버튼 표현·필터 조건 변경은 DeckOwnedFilter와 관련 프리팹에 한정되고, 목록 수명·드래그 취소 경계는 DeckEditUI가 조정한다. 별도 등록·이벤트 구독·추상 계층은 추가하지 않는다(JMH·FW).

서비스·UI 수명 점검: GlobalSystemFactory가 UISystem을 생성해 Global 내부 Builder에 등록하고, GlobalSystemGroup은 IGlobalSystemGroupService만 상위에 Export한다. StageMapViewSystem은 Global Context.UIServiceCommand를 받고 기존 DeckEditPopupViewFactory/Controller에 전달한다. 기존 DeckEditUI(UIId.DeckEdit, 부모 StageSelect, Exclusive)는 씬 수집·ExportUIService로 등록되고 기존 IUISystemService로 개폐한다. 필터의 버튼 바인딩은 Prepare, 초기화는 새 Open/Clear, 슬롯 재사용·해제는 기존 화면/씬 수명이다. 신규 루트 재등록·우회 조회·FW 필수 계약 변경은 없다.

검증: Unity 6000.0.73f1의 자동 재컴파일·도메인 재로드 완료를 로그와 Systems DLL 갱신으로 확인했다. 같은 Unity Roslyn/응답 파일을 사용한 별도 임시 출력 컴파일도 성공했다(신규 소스 중복 지정 경고 1건은 검사용 응답 파일에서만 발생). 실제 프리팹의 fileID 중복 없음, 네 Button의 All·Heavy·Rider·Cannon 연결 순서, Inspector UnityEvent 비어 있음, 11_0의 GUID/fileID 일치를 검사했다. Unity가 신규 클래스 컴파일 전 가져오기에서 비웠던 필터 참조는 컴파일 뒤 재연결해 검사했다. 변경 파일의 diff 공백 검사 통과. EditMode 실행·EventSystem 입력 검증·Play 화면 확인은 이번 작업에서 수행하지 않았다.

## 메인 변경 임시 boolean (2026-11-01)

사용자 확정: 로비/Run이 같은 UI를 쓸 가능성을 유지하고, Core 규약 전에는 boolean 하나를 열기 시점에 적용한다. `DeckEditUI.allowMainCharacterChange`(Inspector: Allow Main Character Change)가 유일한 임시 설정이며 기본 false다. 호출자는 열기 전에 `AllowMainCharacterChange` 프로퍼티로 바꿀 수 있다. false는 메인 교체·삭제 금지, true는 기존 전체 편집 가능 상태 안에서 메인 변경도 허용한다. RunId/Outcome/씬 이름으로 이 값을 자동 추정하지 않는다. 전체 편집 불가를 true로 우회할 수 없다.

Open에서 진행 중인 교체/드래그를 정리하고 설정에 맞춰 표시를 갱신한다. 열린 상태에서 프로퍼티로 값을 바꿔도 동일하게 정리·반영한다. `CanChangeSlot` 한 곳의 판정을 슬롯 Render·교체/삭제 버튼·Owned 클릭 교체·드래그 시작/드롭이 공유한다. 변경 불가 슬롯은 필수 프리팹 참조를 보존한 채 교체·삭제 Button 오브젝트 자체를 SetActive(false)로 숨긴다(사용자 후속 지시). EmptyLabel은 편성 여부와 무관하게 켜 두며, 변경 불가인 편성 슬롯에는 병과명과 "변경 불가", 빈 슬롯에는 기존 "비어 있음" 뒤에 "변경 불가"를 덧붙인다. 허용 상태로 돌아오면 편성 슬롯은 병종만, 빈 슬롯은 기존 빈칸 문구를 표시한다. 문구는 Bind 때 준비하고 Render와 설정 갱신에서 같은 참조를 사용하므로 반복 갱신에 중복으로 덧붙이거나 입력 콜백에서 문자열을 새로 만들지 않는다. 숨긴 버튼에 있던 키보드 포커스는 해당 슬롯의 정보 버튼으로 옮긴다. 정보 조회 및 보유 레벨 데이터는 편집 정책과 분리해 유지한다.

범위는 단일 구현/리뷰 단계다: (1) boolean·Open 연결, (2) 모든 기존 변경 입력에 같은 판정 적용, (3) 실제 프리팹의 클릭/Submit·드래그·재개방/설정 변경·전체 편집 불가 회귀. 새 서비스·UIId·루트 Export나 입력 구독은 없다. 정책 설정은 호출/저작 경계가, 표시와 입력 차단은 화면이 담당한다(SSOT/SOLID/FW 기존 계약 유지). Core 덱 서비스·규칙은 수정하지 않았으며 임시 UI 제한이 Core의 규칙 보장을 대체하지 않는다. 추후 Core가 공개한 권한으로 이 설정의 공급 지점만 교체한다. 공용 상세창 Phase 2의 Change도 반드시 같은 판정을 사용해야 하며, 이번 작업으로 그 패널을 활성화하지 않는다.

검증: Unity 6000.0.73f1 재컴파일 성공. `DeckMainChangePolicyTests` EditMode 8/8 통과(버튼 오브젝트 숨김·EmptyLabel 후속 수정 포함). 실제 DeckEditUI 프리팹에서 false/true의 버튼·클릭 교체, 메인 드래그/드롭, Open 재호출 시 Inspector 설정 반영·기존 교체 모드 해제, 열린 상태 잠금 전환의 정보 버튼 포커스 이동, 전체 편집 불가 우회 금지를 확인했다. 편성/빈 슬롯의 잠금 안내, 허용 시 원래 문구/표시 복원, 반복 Open에서 문구 중복 없음도 확인했다. EmptyLabel의 기존 Raycast Target=0을 확인해 정보 입력을 가리지 않는 설정을 유지했다. 클릭/Submit/드래그는 EventSystem 핸들러로 전달했다. 최초 포커스 시험의 EditMode EventSystem 등록 누락은 ExecuteAlways 시험 전용 구성으로 보완해 재실행했다. 임시 실행 파일은 제거했고 변경 C#/문서의 diff 공백 검사도 통과했다. 실제 Run/향후 로비 호출자가 설정을 전환하는 Play 확인은 별도 미실행이다.

## 공용 유닛 상세창 확장 설계·Task (2026-11-01)

사용자 범위: OwnedUnitSlot도 DeckSlot과 동일한 새 SlotInfoPannel을 사용한다. EXP·이동/공격 범위 그림·Change까지 포함한다. 앞서 슬롯에서 제거한 Level 텍스트는 복원하지 않으며 레벨 조회는 유지한다.

- 선택 원본은 DeckEditUI의 보유 ID `_infoId` 한 곳이다. Owned 슬롯은 현재 카드의 OwnedId만 명시적 수신자에게 전달하고, 화면이 같은 `_displayModel.Owned`에서 카드를 찾아 Deck과 같은 `ShowInfo`를 호출한다. 같은 ID 재선택은 닫기, 다른 ID는 전환, hover는 선택을 바꾸지 않는다. 최신 모델에서 사라진 슬롯의 입력은 정상적인 오래된 입력으로 무시한다.
- 기본 정보는 같은 DeckEditorSnapshot의 카드와 기존 View DB에서 가져온다. 이름·초상·병과·레벨·HP/공격력/방어력·이동/공격 거리와 특성을 새 저작 필드에 연결한다. 기존 Description은 삭제됐으므로 임의로 예전 패널을 복원하지 않는다. UI 수명/입력은 DeckSlotInfoUI가, 필드 표시와 범위 셀 배치는 내부 위젯이 맡는 방향으로 분리한다.
- Change는 미편성 유닛에서는 같은 병과 슬롯에 기존 PlaceOwned 명령을 전달하고 상세창을 닫는다. 메인 변경 금지·전체 편집 불가 정책은 기존 CanChangeSlot을 공유한다. 편성 유닛에서는 사용자 후속 지시에 따라 텍스트를 `편성된 유닛`으로 표시하고 Close와 같은 경로로 닫는다. 이 경우 편성 명령은 전달하지 않는다. 수락/거절은 기존 Draft/Controller/Core Evaluate 경로를 따르며 임시로 덱을 직접 고치지 않는다.
- EXP 현재값은 OwnedCharacterRecord에 있지만 다음 레벨 요구량은 현 공개 스냅샷에 없다. 비율/잔여 EXP를 View에서 공식을 복제해 만들지 않는다. 전투 밖 범위 그림용 상대 셀/영역도 현재 덱 스냅샷에 없다. 가상 전투를 만들거나 InGame DB를 읽어 범위를 재계산하지 않는다. Core 요청 원본은 `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` §60이다. 필드명과 게시 위치는 Core와 인수 때 확정한다.

| Phase | Task·리뷰 지점 | 상태 |
| --- | --- | --- |
| 1 | Owned 내부 Info 참조/개폐/내부 수치 삭제, Info/카드 클릭·Submit의 공용 ID 전달, Deck/Owned 공용 ShowInfo 경로, 재사용/편성 우선 입력 회귀 | 구현 완료, 아래 실행 결과 참조 |
| 2A | 아래 새 레이아웃 설계의 기본 정보·특성·Close, UISystem 개폐·포커스, 프리팹 참조 검증 후 임시 SlotInfoEnabled 비활성 제거 | 구현 완료, 관련 EditMode 26/26 통과. Phase 리뷰 지점 |
| 2B | 미편성 유닛 ChangeButton 교체·기존 편집 정책, 편성 유닛 문구·닫기, 실제 클릭/Submit 회귀 | 동작 확정, 2A 리뷰 뒤 구현 |
| 3 | Core EXP 계약 인수, 같은 스냅샷→표시 모델→EXP Fill/잔여 문구와 상태 표시 | Core 계약 대기, 미구현 |

기존 Phase 3에 포함했던 전투 밖 이동/공격 범위 그림은 Core 요청 §60을 유지하되 EXP와 별도 후속 Task로 분리한다. 이번 사용자가 명시한 MoveText·AtkDText는 거리 수치이며 범위 그림 계약을 기다릴 필요가 없다. PortraitArea의 이름·초상화는 아래 2026-11-02 사용자 후속 지시로 연결한다. 나머지 저작 영역은 추가 동작을 정하지 않는다.

Phase 1 당시에는 새 레이아웃 복구와 분리해 입력/데이터 경로만 정리하고 `SlotInfoEnabled=false`를 유지했다. Description 참조가 0인 당시 프리팹에서 스위치만 켜면 필수 검증이 실패했다. Phase 2A 구현에서 새 전용 필드·실제 CloseButton을 연결한 뒤 이 임시 스위치와 분기를 제거했다. 실행 검증 완료 여부는 아래 Phase 2A 기록을 따른다.

등록·조립·수명 검토: 등록 단위는 기존 DeckSlotInfo(46), 부모 DeckEdit, Parallel이다. OwnedUnitSlot은 UIWidgetBase 내부 위젯으로 별도 등록하지 않는다. `DeckEditUI.Prepare`는 저작 슬롯과 동적 Owned 슬롯을 기존 소유 화면에 바인딩하며, 독립 상세창은 기존 UISystem.BindScene 등록과 IUISystemService 개폐 경로를 사용한다. 서비스의 생성·내부 등록·Group Export·루트 Builder·Global Context 조회는 기존 경로를 유지하며 새 서비스/루트 우회/중복 등록은 없다. 선택 ID·스냅샷·편성 규칙의 SSOT를 유지하고(S), 새 표시 위젯 확장으로 슬롯별 수치 계산을 늘리지 않는다(O). 기존 UIBase 개폐와 명시적 수신자 계약을 유지한다(L/I/D, FW-001~009). Phase 1은 슬롯 표시·입력과 공용 조회 경계만 수정하고 Core·범위/EXP 계산·서비스 조립을 수정하지 않는다. Phase 2/3에서도 표시 조립과 기능을 분리한다.

Phase 1 실행 결과: Unity 6000.0.73f1 재컴파일 성공. Test Runner API로 `OwnedUnitSlotInfoInputTests` EditMode 5/5 통과. 실제 Owned 프리팹의 Info 클릭/Submit, 본체 클릭/Submit의 편성 우선·미소비 시 정보 전달, 재사용된 새 OwnedId 전달·Clear 뒤 무입력·레벨 모델 유지 검증이다. UI 입력은 Button.onClick을 직접 호출하지 않고 EventSystem 핸들러로 전달했다. 입력 시험의 Unity.ugui 의존성은 기존 CoreIntegration 테스트 어셈블리에만 추가했다. 임시 실행 도구는 제거했다. 변경 C#/계획의 diff 공백 검사 통과. 실제 공용 상세창 개방과 새 패널 표시/포커스는 Phase 2 검증 대상이며 아직 완료가 아니다.

### 레이아웃 완료 후 세부 설계·Task (2026-11-01)

이번 기능 원본은 사용자 1~6번 설명과 추가 ChangeButton 지시다. 추가 답변으로 특성 4개 이상은 3열 배치에 슬롯을 늘려 전부 표시한다. 미편성 Change의 같은 병과 교체 후 닫기·기존 변경 제한 유지에 이어, 후속 지시로 편성 유닛에서도 `편성된 유닛` 문구와 Close와 같은 닫기 동작을 제공하기로 확정됐다. 사용자는 Phase 2A 착수를 승인했다. Change 양쪽 동작은 Phase 2B 범위로 유지한다.

| 저작 필드 | 값·표시 계약 |
| --- | --- |
| CategorySection 하위 Text | 같은 카드의 Category만 표시한다. 현재 병종 View 테이블은 아이콘만 제공하므로 기존 슬롯과 같은 Category 표기를 기본으로 하며 임의의 한국어 병종명 테이블을 만들지 않는다. |
| PortraitArea/Name | 기존 UnitVisual 조회의 DisplayName을 표시한다. 미입력 시 기존과 같이 CharacterKey 문자열을 사용한다. |
| PortraitArea/Portrait | 같은 UnitVisual 조회의 InfoIcon을 표시한다. 이미지 미입력은 기존 View DB의 선택 계약이며 이전 Sprite를 비우고 Image를 끈다. 이미지가 있는 유닛으로 바뀌면 다시 켠다. |
| LevelCount | 같은 카드의 Level 숫자만 표시. `Lv` 등 접두어를 붙이지 않는다. |
| EXP Image | Ready인 경우 Core가 제공한 현재 레벨 내 진행 EXP / 다음 레벨 요구 EXP를 fillAmount에 반영한다. View에서 요구량·성장 공식을 만들지 않는다. |
| LeftEXP | Ready인 경우 `다음 레벨까지 {요구 EXP - 진행 EXP} EXP`. 표시 때만 차이·비율을 계산하고 모델에 파생 필드를 저장하지 않는다. |
| CloseButton | 실제 하위 CloseButton을 UIInputBinding으로 연결. 수신자 → DeckEditUI.CloseInfo → IUISystemService.CloseUI(DeckSlotInfo) → DeckSlotInfoUI.Hide 경로를 사용한다. |
| Statatics/HPText | 같은 카드 Stats.MaxHealth 숫자. 현재 체력과 혼용하지 않는다. 프리팹의 기존 Statatics 이름은 유지한다. |
| AtkText / DefText | Stats.AttackPower / Stats.PhysicalDefense 숫자. |
| MoveText / AtkDText | Stats.MoveRange / Stats.AttackRange 숫자. 전투 밖 그림이나 실제 전투 이동 가능 칸을 View에서 계산하지 않는다. |
| AbilityArea_2 | 특성 0~2개에서만 활성화. 없는 슬롯은 데이터·표시를 비우고 숨긴다. 특성 0개여도 영역 선택 규칙은 유지한다. |
| AbilityArea_3 | 특성 3개 이상에서만 활성화. 3열 배치로 기존 3개 슬롯을 재사용하고 부족한 슬롯만 추가해 전부 표시한다. 전환 시 반대 영역과 남는 슬롯을 정리한다. |
| 각 특성 슬롯 | Icon Image, Name Text, Description Text를 동시에 표시한다. 기존 UnitCardTraitContent 변환을 재사용해 같은 읽기의 특성 설정·가용성 설명을 유지한다. 별도의 클릭으로 수치/특성 설명을 번갈아 표시하지 않는다. |
| ChangeButton | 미편성 유닛은 편집 제한으로 교체할 수 없으면 기존 슬롯과 같은 숨김 정책을 적용한다. 클릭/Submit에서 현재 모델의 OwnedId와 Category를 다시 확인하고 기존 PlaceOwned로 같은 병과 슬롯을 교체한 뒤 닫는다. 빈 병과 슬롯에도 같은 배치 명령을 사용한다. 편성 유닛은 `편성된 유닛` 텍스트로 표시하고 Close와 같은 경로로 닫으며 편성 명령·편집 제한 판정을 요구하지 않는다. |

EXP 대기 표시: 승인된 Phase 2A/2B에서는 EXP Fill을 0, LeftEXP를 `-`로 표시해 실제 진행 0과 구분한다. 정상 Ready만 위 계산·문구를 사용한다. 최대 레벨·성장 비활성 상태는 요구량 0으로 추정하지 않고 Phase 3의 Core 계약 인수 시 문구·Fill 정책까지 확정한다. 요구량 양수 등 Ready 계약 위반은 구성/데이터 예외이며 조용히 0으로 바꾸지 않는다.

책임과 변경 경계:

- DeckEditUI는 `_infoId` 선택, 같은 모델의 카드 조회, 기존 View DB 표시값 공급, CanChangeSlot 판정과 교체 명령 전달을 맡는다. 상세창에 별도의 선택 ID나 덱 사본을 만들지 않는다.
- DeckSlotInfoUI는 기존 UIBase 등록·Open/Hide·포커스와 명시적인 Close/Change 입력 전달을 맡는다. 닫는 본체 Button이라는 옛 전제는 제거하고 실제 하위 CloseButton만 연결한다. 상세창 본문 클릭을 닫기 입력으로 사용하지 않는다.
- 기본 수치 표시와 EXP, 특성 목록은 상세창 내부의 응집된 표시 위젯으로 분리한다. 필수 Image/Text/영역·슬롯 참조는 직렬화로 명시하고 데이터 주입 전에 검사한다. 새 MonoBehaviour가 필요한 경우에도 독립적으로 개폐하는 화면이 아닌 내부 표시 부품이며 UIId/서비스를 추가하지 않는다. 새 C#에는 .meta를 함께 만든다.
- 특성 슬롯은 기존 공용 UnitAbilitySlot의 버튼/아이콘 내부 이름 배치 계약과 다르다. Deck 전용 표시 슬롯을 두어 Name/Description을 직접 연결하고, Combat·Lobby의 공용 버튼 슬롯 계약을 이 레이아웃 때문에 바꾸지 않는다. 공용 Ability.prefab도 다른 사용처를 위해 일괄 수정하지 않고 DeckEditUI의 연결·필요한 인스턴스 설정만 다룬다.
- AbilityArea_3는 프리팹의 셀 크기·간격을 기준으로 3열·필요 행 수를 배치한다. 영역 높이를 넘으면 모든 행이 영역 안에 들어오도록 균일 축소하는 기존 방향을 유지한다. 슬롯 복제·참조 연결은 목록 확장 때만 수행하고 남는 슬롯은 다음 유닛 표시에서 재사용한다. 씬 해제 시 소유 상세창과 함께 해제한다.

Phase별 Task와 리뷰 지점:

| Phase | 순서·변경 대상 | 검증·완료 기준 |
| --- | --- | --- |
| 2A | A1. DeckSlotInfoUI의 Description/단일 Grid/본체 닫기 전제를 새 필드와 내부 위젯으로 교체. A2. DeckEditUI가 기존 View 조회의 이름·병종 및 같은 카드 수치를 전달. A3. 두 AbilityArea와 슬롯의 Icon/Name/Description 연결·재사용. A4. 실제 DeckEditUI.prefab의 참조를 연결하고 상태 변경 전 필수 구성 검증. A5. Close 개폐·포커스 수명을 연결한 뒤 SlotInfoEnabled 임시 차단 제거. EXP는 제안한 대기 표시, Change는 아직 비활성. | Unity 컴파일, 실제 프리팹 표시와 EventSystem 클릭/Submit: Deck/Owned 공용 선택, 동일 ID 재클릭 닫기, 다른 ID 전환, hover 고정 유지, 수치·레벨·문구 일치, 특성 0/1/2/3/4개 이상·많음→적음·반복 열기, Close·부모 닫기·포커스 복원, 누락 참조의 명시적 예외. 결과 보고 후 정지. |
| 2B | B1. 기존 수신자에 공용 상세창 교체 입력을 추가. B2. 최신 미편성 카드와 같은 병과 슬롯을 찾고 CanChangeSlot을 재사용. B3. 교체/드래그 모드를 정리하고 기존 PlaceOwned로 한 번 전달한 뒤 UISystem을 통해 닫기. 편성 유닛은 문구를 `편성된 유닛`으로 바꾸고 Close 경로로만 전달. B4. 모델 갱신·편집 권한 변경 시 버튼 표시/포커스도 갱신. Draft·Core 교체 규칙은 변경하지 않는다. | 실제 프리팹의 클릭/Submit에서 미편성→같은 병과 교체·빈칸 배치, 편성 유닛 문구·닫기·편성 명령 없음, 메인 잠금·전체 편집 불가·편집 권한 전환, 동일 정의의 다른 OwnedId 구분, 사라진 ID/지난 입력 무시, 거절 시 이전 Draft 유지·기존 거절 사유 표시, 명령 중복 없음, 닫기/재개방. 기존 슬롯 교체·드래그 중 영향받는 경로만 회귀. 결과 보고 후 정지. |
| 3 | C1. Core §60 공개 계약과 버전/진행 EXP 의미·최대 레벨·비활성 상태 인수. C2. DeckEditModelBuilder/CardModel에 원본 값과 상태만 전달. C3. EXP 위젯에서 비율·잔여 문구 표시. C4. Ready/대기/비활성/최대 레벨 전환과 유닛 변경 시 이전 Fill 제거. | 레벨업 뒤 잔여 EXP·0 진행·다음 레벨 직전·서로 다른 OwnedId·상태 전환, UI 비율/잔여 문구, 0 나누기 없음과 잘못된 Ready 계약 예외. Core 계산 재현 테스트를 View에 만들지 않는다. Unity 컴파일/대상 EditMode 결과 보고 후 정지. |

등록·서비스 경로 확인:

| 단계 | 현재 코드·유지할 경로 |
| --- | --- |
| 생성·내부 등록 | GlobalSystemFactory가 UISystem을 생성하고 UISystem.ExportServices가 Global 내부 Builder에 IUISystemService와 모듈을 등록한다. Builder는 한 번 Build한다. |
| Group 보관·상위 Export | GlobalSystemGroup.UIServiceCommand가 내부 Reader에서 보관한다. 상위 Export는 IGlobalSystemGroupService·모듈뿐이며 UI 서비스를 루트에 다시 노출하지 않는다. |
| 소비자·조립 | StageMapViewSystem이 global.UIServiceCommand를 StageMapViewServices에 전달 → StageMapSceneFactory → DeckEditPopupViewFactory/Controller. 씬 화면은 UISystem.BindScene에서 IUISystemService를 바인딩받는다. |
| UI 등록 단위 | 기존 DeckSlotInfo=46 / 부모 DeckEdit / Parallel. UISystem.BindScene이 비활성 자식까지 수집·등록한다. Open/Close는 서비스 경로, 실제 활성 변경은 UIBase Open/Hide가 맡는다. 영역·특성 슬롯은 내부 위젯으로 별도 등록하지 않는다. |
| 재사용·종료 | 같은 씬 인스턴스와 추가 슬롯을 재사용한다. 닫기·부모 비활성화에서 선택/포커스를 정리하며 씬 해제는 기존 UISystem 수명을 따른다. 열기 반복으로 입력 바인딩을 누적하지 않는다. |

설계 검토: SSOT는 선택 `_infoId`, 같은 DeckEditorSnapshot의 능력치/경험치, 기존 CanChangeSlot과 Core Evaluate, View 외형/특성 조회에 각각 유지한다. S는 수명·표시·교체 책임을 분리하고, O는 EXP/특성 전용 위젯으로 확장하며, L은 UIBase 및 기존 수신자의 개폐/입력 계약을 지키고, I/D는 필요한 표시값·명시적 수신자·기존 Global Context만 사용한다. FW-001~003의 서비스/Builder 조립은 변경하지 않으며 FW-004·008·009에 따라 UI 이력 우회 없이 등록·개폐한다. FW-005~007의 InputSystem 설정을 UI 버튼 연결 때문에 재구성하지 않는다. 필수 필드/계층 오류는 UIId·필드·객체를 포함한 예외로 상태 변경 전에 보고한다. 입력의 정상 비활성·지난 ID 무시는 기존 명시 계약으로 유지한다. 현 확인 범위에서 새 FW 필수 계약 위반이나 Global 하위 서비스 루트 우회가 필요한 설계는 없다.

기능 독립성 비교: 기존 상세창은 레벨/수치·특성 클릭 설명·Grid 크기·입력 연결이 한 클래스에 섞여 있어 EXP나 특성 배치를 바꿀 때 전체 상세창을 읽어야 한다. 계획 후 문구/수치 변경은 기본 표시 위젯, EXP 계약은 모델 변환+EXP 위젯, 특성 행 배치는 특성 위젯, 교체 권한/명령은 DeckEditUI+기존 수신자, 의존성 변경은 프리팹 참조/초기 연결에 머문다. 공통 경계는 카드 Render·Validate·Open/Hide이며 이 경계가 바뀌면 Deck/Owned 개폐 통합 검증을 함께 한다. Combat·FieldCamera·Architecture·Core 내부 구현은 이 기능 수정/검증에 포함하지 않는다.

### Phase 2A 구현·검증 기록 (2026-11-02, 완료·리뷰 지점)

- `DeckSlotInfoUI`는 기존 UI 등록·Close 입력·포커스 수명만 소유한다. 본체 닫기 Button과 단일 Description 전제를 제거하고 실제 하위 CloseButton에 UIInputBinding을 한 번 연결했다. 기본 수치는 `DeckSlotInfoStats`, EXP 대기는 `DeckSlotInfoExperience`, 특성 영역/목록은 `DeckSlotInfoAbilities`에 분리했다. 추가한 C# 파일은 같은 폴더의 .meta를 포함한다.
- `DeckEditUI`는 같은 카드의 기존 View 조회 이름을 상세창에 전달하고 임시 SlotInfoEnabled 분기를 제거했다. 선택 `_infoId`, 기존 View DB·특성 변환·UISystem 개폐 경로를 유지했다. 반복 Open/다른 유닛 선택에서도 최초 외부 포커스를 보존하며 Close·부모 숨김에서 정리한다.
- 실제 `DeckEditUI.prefab`의 7개 기본 텍스트·EXP Image/LeftEXP·두 Grid·특성 슬롯 5개의 Icon/Name/Description을 연결했다. 공용 Ability 원본은 수정하지 않고 이 프리팹의 중첩 인스턴스에서만 Button을 제거하고 표시용 Graphic의 Raycast Target을 껐다. 슬롯 내부 Content 기준점을 최초 연결 때 만들고, 복제 슬롯에도 매핑하여 행이 늘 때 셀 배경과 내용이 동일 비율로 축소되게 했다. 많은 유닛에서 적은 유닛으로 전환하면 비활성 영역까지 이전 텍스트·아이콘을 지우며 복제 슬롯은 재사용한다.
- ChangeButton은 Phase 2A에서는 숨겨 둔다. 편성 유닛의 `편성된 유닛` 문구·닫기와 미편성 교체는 모두 Phase 2B에서 연결한다. 초기 Phase 2A에서는 PortraitArea 연결을 제외했으며, 아래 후속 지시에서 이름·초상화를 연결한다. AreaInfo·Core·FieldCamera·Architecture·서비스 Factory/Group은 수정하지 않는다.
- `DeckSlotInfoPanelTests`를 실제 프리팹+UISystem 등록/개폐+EventSystem 클릭/Submit으로 구성했다. 특성 0/1/2/3/4/5개, 슬롯 재사용·내용 정리·영역 경계, 기본 수치·EXP 대기, 공용 선택·hover 유지·재클릭·부모 닫기/재개방·포커스, 잘못된 구성 예외와 비활성/오른쪽 클릭 무시를 검증한다. 기존 Owned 입력·메인 정책 테스트를 관련 회귀로 실행한다.
- 사용자 확인: 기존 슬롯 문구 `Main 변경 불가`, `Rider - 비어 있음`, `Rider - 비어 있음 변경 불가`는 사용자가 의도적으로 바꾼 현재 기준이다. 최초 실행의 3건 실패는 이전 줄바꿈 문구를 기대한 테스트에서 나왔다. 표시 코드는 유지하고 기대값만 현재 문구로 정정했다. 공용 상세창 활성화로 변경 불가 유닛 클릭도 정보창을 열 수 있으므로 메인 정책 시험에도 UISystem 등록/바인딩을 추가했다.
- 신규 테스트가 디스크와 MonoScript로는 존재하지만 기존 Unity 컴파일 소스 목록에서 누락된 상태를 확인했다. GUID를 유지한 AssetDatabase 재등록과 컴파일 캐시 재생성 뒤 소스 목록·로드된 테스트 타입을 확인했다. 임시 조사 중 추가한 TestRunner 어셈블리 참조는 필요하지 않아 제거했으며 테스트 asmdef는 작업 전의 참조 구성을 유지한다.
- 최종 검증: Unity 6000.0.73f1 재컴파일 성공, Test Runner API의 관련 EditMode **26/26 통과**(DeckSlotInfoPanelTests 13, OwnedUnitSlotInfoInputTests 5, DeckMainChangePolicyTests 8). 신규 시험의 EventSystem null 복원 오류와 3열 슬롯 일부의 잔여 Button 구성을 수정한 뒤 재실행했다. 프리팹 수정 과정에서 두 Owned 중첩 인스턴스에 잘못 들어간 특성용 override도 되돌리고, 작업 시작 전 프리팹에 의도한 변경만 적용한 결과와 최종 파일 전체를 대조했다. GUID 길이·중복 fileID·상세창 필수 참조의 누락 없음도 확인했다.
- 실제 프리팹의 필드·아이콘·특성 이름/설명·레이아웃 영역 경계·입력/포커스는 위 자동 검증으로 확인했다. 별도의 Play 모드 화면 확인은 수행하지 않았다. 임시 카메라 렌더 시도는 빈 이미지여서 외형 검증 증거로 사용하지 않는다. 테스트 실행/소스 등록 조사용 Editor 스크립트와 .meta는 제거했다. C#/계획의 diff 공백 검사를 통과했다. Core·다른 View 영역·서비스 조립의 변경은 없다.

이 지점에서 Phase 2A를 마치고 사용자 리뷰를 기다린다. 다음 Phase 2B는 편성 상태별 ChangeButton 문구·입력이며 이번 단계에서는 구현하지 않았다.

### Phase 2A 후속 — 병종·이름 분리와 초상화 (2026-11-02)

사용자 지시: CategorySection Text에는 Category만, PortraitArea의 Name에는 유닛 이름, Portrait Image에는 초상화를 표시한다. 단일 후속 수정으로 (1) 기본 표시 위젯의 필드 분리, (2) 기존 View 조회 이름·InfoIcon 전달 및 실제 프리팹 참조 연결, (3) 기존 관련 테스트 기대값과 유닛 전환 시 이미지 정리 검증 순서로 진행한다. Phase 2B는 시작하지 않는다.

`DeckSlotInfoStats.categoryAndName`은 `category`로 바꾸고 FormerlySerializedAs를 붙여 기존 직렬화 참조를 보존한다. `unitName`과 `portrait`를 명시적으로 추가하고 필수 참조·같은 PortraitArea 소속을 표시 전에 검증한다. 이름/초상화는 새로운 조회나 저장소를 만들지 않고 DeckEditUI.GetVisual의 같은 View 데이터에서 받아 DeckSlotInfoUI → Stats로 전달한다. 기존 기본 표시 위젯 안에서 변경하므로 서비스 생성·등록·Export·Global Context·UIId/부모/Policy·개폐·입력 수명은 그대로다. 규칙/선택 SSOT와 SOLID·FW 계약을 유지하고, 표시 필드 변경은 기본 표시·전달 경계·프리팹·관련 시험에 한정한다.

검증: Unity 6000.0.73f1 컴파일 성공, 관련 EditMode 26/26 통과. 기존 기본 표시 시험에서 병종/이름 분리·전달 Sprite 표시·다음 유닛의 이미지 미입력 시 Sprite 제거/Image 비활성화를 확인했다. 공용 선택·포커스 등 기존 회귀도 통과했다. 필수 특성 데이터 변환 실패 때 기존 이름/초상화를 일부만 갱신하지 않는 점도 확인했다. 임시 Editor 실행 스크립트와 .meta는 제거했다. 프리팹은 Unity의 메모리 매핑 잠금 때문에 직접 덮어쓰기가 실패하여 원본 백업 후 파일을 원자적으로 교체했다. 사용자 저작 상태에 요청한 세 참조만 적용한 파일과 전체 대조했고, Unity Refresh 뒤에도 Category/Name/Portrait 참조와 Name 객체명이 유지됨을 확인했다. 실제 Play 화면 확인은 별도이며 Phase 2B는 미진행이다.

### 기존 슬롯·편성 동작과 이전 상세창 이력

- 화면 등록은 기존 `DeckEditUI`(`UIId.DeckEdit`, 부모 `StageSelect`, Exclusive)와 공용 `DeckSlotInfoUI`(`UIId.DeckSlotInfo=46`, 부모 `DeckEdit`, Parallel)다. 외부 개폐는 기존 Controller → IUISystemService 경로를 사용한다. 상세창 개폐도 DeckEditUI가 바인딩된 IUISystemService를 통해 요청한다.
- `DeckEditSlotView`와 `OwnedUnitSlot`은 `UIWidgetBase → UIBase` 기반 Parallel 내부 위젯이다. 개별 UIId나 서비스를 등록하지 않는다. 정적 위젯은 UISystem 씬 수집에서 소유 화면을 검증하며, 추가 생성된 Owned 슬롯은 생성 직후 DeckEditUI에 BindOwner한다.
- 상단 네 칸은 사용자가 제작한 `DeckSlot.prefab`으로 구성하며 `DeckEditSlotLayout`의 Main → Heavy → Rider → Cannon 순서다. UnitCardView를 사용하지 않는다. 유닛이 없어도 슬롯과 드롭 배경·Change 버튼은 남는다. Core의 후보·확정 순서를 표시 순서로 덮어쓰지 않는다.
- 하단은 현재 Draft에 포함되지 않은 보유 개체만 같은 스냅샷에서 표시한다. 식별은 CharacterInstanceId로 한다. 교체가 수락되면 들어간 개체를 제외하고 이전 개체를 다시 표시한다.
- `ownedScrollView`에는 Panel 아래 ScrollRect, Viewport, Content를 연결하고 `ownedSlotPrefab`에는 OwnedUnitSlot 프리팹을 연결한다. Viewport의 Mask에는 같은 객체의 Image가 필요하다.
- Content에 저작한 슬롯을 먼저 재사용한다. 부족하면 프리팹을 복제하고 남는 슬롯은 Clear/Hide한다. 한 줄의 가로 목록이며 `ownedSlotSpacing`은 카드 사이 간격, `ownedSlotPadding`은 좌우·상하 여백이다. 슬롯 수에 따라 Content 크기와 스크롤 범위를 갱신한다.
- DeckSlot의 Info 내부 Name·HPValue·AttackValue·DefenseValue에 동일 스냅샷의 이름·MaxHealth·AttackPower·PhysicalDefense를 전달한다. 현 계약에 별도 보호막 잔량은 없다. 빈 슬롯은 Info 전체를 숨긴다. Owned 슬롯의 내부 Info 의존성은 위 Phase 1에서 제거하고 공용 상세창 수신 경로로 통합했다.
- 2026-11-01: DeckSlot·OwnedUnitSlot에서 레벨 텍스트를 제거한 프리팹에 맞춰 슬롯의 직렬화 참조·필수 검증·갱신/초기화만 제거했다. `DeckEditCardModel.Level`과 `DeckEditModelBuilder`의 레벨 조회, 공용 SlotInfoPannel의 레벨 표시는 유지한다. UI 등록·서비스 조립·입력 수명은 변경하지 않는다.
- 이전 상세창 구현 이력: 본체 클릭 닫기·단일 Description 수치/특성 전환·단일 Grid 가변 열·특성 버튼 복제를 사용했다. 새 레이아웃에서는 위 세부 설계로 교체했으며 현재 동작 요구로 삼지 않는다. 과거 SlotInfoEnabled=false 차단은 Phase 2A에서 제거했다. 검증 완료 여부는 위 Phase 2A 기록을 따른다.
- ChangeButton은 화면의 선택 슬롯 인덱스를 설정한다. 해당 병과의 미편성 OwnedUnitSlot Outline만 켜며, 같은 병과의 슬롯 본체 클릭은 PlaceOwned로 전달한다. 다른 병과 클릭, 배경 클릭, 다른 일반 버튼 입력은 선택을 취소한다. 입력은 EventSystem 핸들러에서 명시적인 IDeckEditInteractionReceiver 호출로 연결한다.
- 선택한 슬롯은 같은 위치의 ChangeButton을 숨기고 DeleteButton을 표시한다. Delete는 해당 병과 유닛을 초안에서 빼고 미편성 목록으로 돌려보낸다. 빈 슬롯의 Delete는 편성 명령 없이 교체 모드만 취소한다. 메인 삭제도 초안에는 반영하되 확정은 Core의 메인 필수 규칙을 따른다.
- 키보드·게임패드 Submit은 버튼의 UIInputBinding을 사용한다. Change/Delete 전환 시 선택된 버튼 포커스를 옮기고, 공용 상세창 개폐 시 포커스를 옮겼다가 복원한다. 호버가 고정된 상세 정보를 바꾸지 않는다.
- 드래그 시작 시 같은 병과 슬롯을 강조하고 raycast를 막지 않는 미리보기를 표시한다. 다른 병과·다른 포인터의 드롭은 편성하지 않는다. 슬롯 밖 드롭은 변경 없이 종료한다.
- 입력은 슬롯의 EventSystem 핸들러 → IDeckEditDragReceiver → IDeckEditInput.PlaceOwned → Controller → Draft의 단일 교체 후보 요청 → 기존 Core Evaluate 경로다. 수락 전에는 Draft 후보를 변경하지 않으며, 거절 시 기존 편성을 유지한다. 평가 사유는 표시 모델에 남지만 Status Text 제거 후 현재 화면에는 출력하지 않는다.
- 닫기·입력 재바인딩·재표시·편집 불가 전환은 드래그·교체 모드·Outline·Delete 버튼을 정리한다. 드래그 시작도 클릭 교체 모드를 해제한다. 화면 숨김은 공용 상세창을 닫는다. 슬롯은 화면이 재사용하고 씬 인스턴스가 해제될 때 함께 파괴된다.
- Core는 병과 중복 편성을 허용할 수 있지만 이 화면은 병과당 한 칸이다. 기존 덱에 같은 병과가 중복되거나 지원하지 않는 병과가 있으면 대상 ID와 병과를 명시한 예외로 구성을 알린다. 임의로 유닛을 제외하거나 덱을 다시 확정하지 않는다.
- 서비스 생성·등록·상위 Export·소비 경로는 변경하지 않는다. 기존 Global Context의 UISystem과 기존 DeckEdit Factory/Controller를 사용한다. 신규 루트 등록이나 소비자의 루트 직접 조회는 없다.
- 생성·등록·바인딩: 저작한 DeckSlot과 SlotInfoPannel을 씬 프리팹으로 생성하고 기존 UISystem.BindScene이 등록 화면과 위젯 소유자를 수집한다. DeckEditUI.Prepare가 슬롯·Owned 목록·공용 상세창의 필수 참조를 먼저 검증한 뒤 입력을 바인딩한다. 서비스의 생성·내부 등록·상위 Export·소비 경로는 기존 Global Context를 그대로 사용하며 신규 서비스 등록은 없다.

검증: `DeckEditPlacementTests`는 표시 순서·빈칸·미편성 목록·교체 원자성·거절·낡은 버전·전체 비우기를 검사한다. `DeckEditSlotInputTests`는 실제 프리팹에 EventSystem 입력을 전달하여 Max HP/방어 수치·슬롯 증설·드래그/클릭 교체·잘못된 병과/배경 취소·삭제/빈칸 취소·공용 Info 개폐·Owned Info 유지를 검사한다.
