# Unit HUD 카테고리·버프/디버프 배치 설계

> **현재 배치 정정(2026-10-31, 사용자 지시):** 카테고리는 현재 HP바 옆 위치를 유지한다. 기존에 카테고리를 두기로 한 HP바 왼쪽 위에는 고정 아이템 아이콘을 둔다. BuffIconRoot는 그 오른쪽에서 **버프→디버프만** 정렬한다. H1/H2/H3도 이 정정에 맞췄으며, 이전 UH1/UH2 실행 기록의 배치 지시는 이 정정으로 대체한다.

- 작성: 2026-10-31. 상태: 코드/정적 컴파일 및 사용자 고정 Item Icon 정상 동작·버프 아이콘 기본 표시 확인 완료. 세부 입력/수명 사례는 별도 미확인.
- 현재 사용자 결정: 카테고리는 HP바 옆 유지, HP바 왼쪽 위에는 아이템 한 칸 고정. 그 오른쪽 영역에 버프→디버프를 좌하단 기준으로 위로 줄바꿈. 같은 행 혼합 허용. 보유 특성·격파 목표 제외.
- 씬/프리팹 편집·연결과 Unity 에디터 테스트는 사용자 담당이라는 기존 지시를 유지한다. 후속 전체 진행 지시로 코드까지 반영하되 씬·프리팹은 사용자가 연결한다.
- 기준: Assets/AGENTS.md, Framework architecture/programmer guide, [공동 구조 기준 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리).

## 1. 변경 전 경계(설계 작성 당시)

- `FieldUnitPresenter.RenderUnitHud`: 병과 아이콘/수치를 UnitHUD.SetInfo로 전달하고, 격파 목표→아이템→디버프→버프→보유 특성 목록을 만들어 SetStatuses 호출.
- `CombatUnitStatuses`: Core 사실을 상태 외형 표의 Icon/Description으로 변환. Polarity별 분류는 이미 존재하며 현재 디버프부터 추가한다.
- `UnitHUD.SetStatuses`: 상태 키/고정 설명 수명, 아이콘 재사용, UIInputBinding/우클릭 연결, 한 줄 배치를 모두 담당.
- 카테고리는 현재 선택 InfoPanel 내부이며 UnitHUDInfoPanel이 Image를 쓴다. 기본 체력바 위에는 별도 카테고리가 없다.
- UnitHUDLayer는 공용 Overlay Canvas에서 HUD 전체를 투영한다. 선택 패널은 기존 UISystem 등록/개폐를 따른다.

## 2. 확정할 배치

```text
           [버프1][버프2][버프3][버프4]
[아이템]   [디버프1][디버프2]
[카테고리][           HP바             ]
```

그림의 세로선/줄은 위치 설명이며 새 배경/선 에셋을 만드는 요구가 아니다. 아이템은 버프/디버프 행 수에 따라 이동하지 않는다. 아이템이 없으면 고정 슬롯만 숨긴다.

### H1. 카테고리

- 유닛 HUD의 현재 HP바 옆에 한 개 표시. 선택 여부와 무관하며 HUD 자체의 가시성 규칙을 따른다.
- 현재 기본 HUD의 Category Image와 categoryImage 연결을 유지한다. InfoPanel 안에 같은 카테고리를 중복 표시하지 않는다.
- Sprite 원본은 기존 CategoryVisual 표. FieldUnitPresenter가 읽고 UnitHUD가 새 categoryImage 참조로 표시한다. InfoPanel의 category 참조/필수 검사/인자 소비는 제거한다.
- UnitHUD.SetInfo의 category 인자는 UnitHUD에서 소비하고, InfoPanel에는 나머지 행동/이름/공격/방어 값만 전달한다. 별도 데이터 조회·서비스는 만들지 않는다.
- 구조물 HUD에는 카테고리·상태 영역을 표시하지 않는다. 유닛/구조물 재사용과 Clear에서 표시와 Sprite를 정리한다.

### H2. 상태 영역의 항목

| 순서 | 항목 | 조건 |
|---|---|---|
| 1 | 버프 | 상태 외형 Polarity=Buff, Core 사실이 현재 효과를 공개 |
| 2 | 디버프 | 상태 외형 Polarity=Debuff, Core 사실이 현재 효과를 공개 |

- 격파 목표·보유 특성 아이콘은 이 목록에서 제외한다. Core 보유 특성이나 목표 계산/다른 목표 표시는 삭제하지 않는다.
- 가속의 쌓인 공격력 버프, 보호막 총량, 공격력 부여는 유지한다. 보유 특성 아이콘을 뺀다고 현재 효과까지 빼지 않는다.
- 분류 내 순서는 기존 CombatUnitStatuses의 Rows 순서를 유지한다. Polarity는 외형 표가 원본이며 UnitHUD가 설명/키 이름으로 다시 분류하지 않는다.
- 아이템은 별도 고정 슬롯이다. UnitHUD.SetItem으로 전달하며 SetStatuses 목록과 레이아웃 Count에는 포함하지 않는다. 아군 우클릭 해제는 기존 입력 제한을 따른다.

### H3. 줄바꿈과 좌하단 정렬

- BuffIconRoot는 고정 아이템 오른쪽부터 체력바 우측 끝까지의 가로 폭을 가진 RectTransform. 프리팹에서 위치/폭을 지정하며 아이템과 겹치지 않는다.
- 내용 묶음의 왼쪽/아래 위치는 고정, 행 수가 늘면 위로 확장한다. 카테고리는 내용 행 수에 따라 위로 움직이지 않는다.
- **순서는 위 행→아래 행, 각 행은 왼쪽→오른쪽**이다. 전체 순서가 버프→디버프이므로 버프가 디버프보다 위에 놓이고, 같은 행에서는 왼쪽에 놓인다.
- 마지막 행도 왼쪽 정렬한다. 버프/디버프 사이에 강제 개행이나 빈 칸을 넣지 않는다.
- 아이템은 상태 묶음 밖의 HP바 왼쪽 위 고정 슬롯이다. 상태 묶음의 줄 수/폭 변경으로 위치가 바뀌지 않는다.
- 빈 목록은 아이콘을 모두 숨기고 내용 높이 0으로 둔다. 카테고리와 별도 아이템 슬롯은 각자 상태를 유지한다. 아이템만 있으면 상태 영역 높이는 0이다.
- 아이콘 바탕은 같은 프리팹 크기/배율을 사용한다. 슬롯 폭·높이는 그 RectTransform과 배율에서 읽고, 가로/세로 간격만 설정값으로 둔다. 아이콘마다 다른 크기를 허용하지 않는다.
- 열 수 = floor((영역 폭 + 가로 간격)/(슬롯 폭 + 가로 간격)). 영역이 한 칸보다 좁거나 크기/간격이 유효하지 않으면 대상 HUD/필드명을 포함한 구성 오류로 거절한다. 임의 확대·잘림·스크롤·최대 행 제한은 추가하지 않는다.
- N개, C열일 때 R=ceil(N/C). 항목 i의 열=i%C, 위에서 센 행=floor(i/C), 아래 기준 행=R-1-floor(i/C). 이 좌표에 동일 슬롯/간격을 적용한다. 마지막 행의 남은 오른쪽 칸은 비워 둔다.

예: 4열, 아이템 1·버프 4·디버프 2이면 고정 아이템은 영역 밖에 남고 첫 행 [B1,B2,B3,B4], 아래 행 [D1,D2]. 단순 Bottom-Left Grid는 버프가 아래에 남으므로 사용하지 않는다.

## 3. 구현 책임과 수명

- `CombatUnitStatuses.AddTo`: 기존 Polarity별 변환을 버프→디버프 순서로 호출. Core 규칙이나 상태 종류 자체를 바꾸지 않는다.
- `FieldUnitPresenter`: 목록에서 격파 목표/보유 특성 추가를 제거하고 고정 아이템 전달과 버프/디버프 변환을 각각 호출. 여타 목표 표시/외형 처리와 TraitVisual 의존성은 사용처 확인 없이 일괄 제거하지 않는다.
- 새 순수 C# 협력 객체 `UnitHUDStatusLayout`을 UnitHUD와 같은 UI 폴더에 둔다. 크기/간격/수만 받아 행 수·슬롯 좌표를 계산하며 Buffer/UI 등록/아이콘 데이터/명령을 알지 못한다. 새 서비스/MonoBehaviour는 만들지 않는다.
- UnitHUD는 기존 아이콘 생성/재사용과 입력/키별 설명 수명을 유지하고 배치 계산만 협력 객체에 맡긴다. SetStatuses 및 실제 Rect 폭 변경 때 재배치하며 프레임마다 상태 목록/좌표 배열을 할당하지 않는다. 최소한의 이전 폭/수 캐시는 표현 무효화용이며 게임 상태 원본이 아니다.
- 아이콘 이동 시에도 상태 Key로 고정 설명을 유지한다. 사라진 항목·슬롯 비활성·Clear·풀 반환은 기존 hover/우클릭 정리 계약을 따른다. 데이터 표시 재배치가 아이템 해제 명령을 만들지 않는다.
- categoryImage/statusRoot는 같은 HUD 소속으로 검증한다. InfoPanelMask 내부 카테고리를 연결한 잘못된 구성도 거절한다. 구조물의 명시적인 간소 HUD 경로는 기존 계약을 유지한다.
- UnitHUD는 기존 UIWidgetBase, Layer는 UIGroup/UIId.UnitHUDLayer, 선택 InfoPanel/HoverPanel은 기존 UIBase/동적 등록/Parallel 정책을 유지한다. 새로운 UIId·등록·구독을 추가하지 않는다. 상태 슬롯의 UIInputBinding·UISecondaryClickBinding도 유지한다.

## 4. SSOT·SOLID·FW·기능 독립성 검토

- SSOT: 현재 효과는 Core/Buffer, 버프·디버프 분류/외형은 상태 외형 표, 병과 외형은 CategoryVisual, 아이콘 크기는 프리팹, 배치 식은 UnitHUDStatusLayout 한 곳. 정렬용 버프 목록/디버프 목록을 HUD에 중복 저장하지 않는다.
- SRP: 상태 변환/목록 조립/배치 계산/실제 표시·입력 수명을 구분한다. 체력 연출 구현을 배치 알고리즘에 섞지 않는다.
- OCP: 새 상태 추가는 기존 상태 행·외형으로 처리하며 레이아웃 변경 불필요. 아이템/버프/디버프 외 항목 추가는 별도 사용자 결정 대상.
- LSP: UnitHUD의 Open/Hide/Clear와 구조물·풀 재사용 계약을 유지한다. InfoPanel이 준비/등록 전에 열리지 않는 기존 검증도 유지한다.
- ISP/DIP: 배치 객체는 숫자 입력만 받으며 CoreGroup/전체 DB/UISystem에 의존하지 않는다. 한 구현을 위한 서비스 인터페이스는 추가하지 않는다.
- FW/JMH: 기존 Factory→Layer→Presenter/View→Widget 경로와 Global Context의 UISystem 소비 유지. Builder/서비스 등록·루트 재노출 없음. InputSystem의 키/Layer 소유권 유지, Button.onClick/별도 이벤트 구독 추가 없음.
- §4.6 비교: 기존 SetStatuses 안에 다중 행 계산까지 넣으면 정렬 조건 수정 때 상태 설명/우클릭/피해 미리보기 코드를 같이 이해해야 한다. 분리 후 열/행 정렬 변경은 Layout과 해당 검증, 상태 표시 대상 변경은 Presenter/Statuses, 배치 외형은 프리팹에 머문다. 공통 경계인 키별 입력 수명/풀 재사용/폭 변경은 통합 확인이 필요하다.
- Core/FieldCamera/Architecture는 수정하지 않는다. 실제 보호막 잔량과 HoverPanel의 Shield=물리 방어력 불일치 교체는 이번 범위가 아니다.

## 5. 범위와 인수

- 체력바/예상 피해/피해 숫자/선택 정보 펼침·상세 패널의 동작은 유지한다. 카테고리를 선택 패널 밖으로 옮기면서 필요한 참조 계약만 수정한다.
- 구현 Phase와 검증 범위는 [Task 계획](../plans/2026-10-31-unit-hud-status-layout-plan.md).
- 문서 작성은 기능 구현/컴파일/사용자 재생 확인을 의미하지 않는다. 설계 리뷰 후 착수한다.

## 6. UH1 반영 (2026-10-31)

H1·H2의 표시 책임/대상과 순서를 코드에 반영했다. H3 줄바꿈은 UH2 범위로 남겼다. 기존 Category 오브젝트 이동과 UnitHUD.categoryImage 연결은 사용자가 수행한다. 정적 컴파일·사용자 연결·실행 확인은 [계획의 UH1 기록](../plans/2026-10-31-unit-hud-status-layout-plan.md#uh1-구현-기록-2026-10-31)을 따른다.


## 7. UH2·UH3 반영 (2026-10-31)

H3를 UnitHUDStatusLayout과 기존 UnitHUD 슬롯 경로에 반영했다. 순수 C# 배치 검사 25개 통과. 최종 연결 필드·RectTransform 조건·검증 구분은 [Task 계획의 최종 사용자 작업 목록](../plans/2026-10-31-unit-hud-status-layout-plan.md#최종-사용자-작업-목록)에 있다. 실제 Unity 사용자 인수는 아직 미확인이다.

## 8. 고정 아이템 슬롯 정정 — 현재 계약 (2026-10-31)

- 사용자 승인 배치: 카테고리=현재 HP바 옆, 아이템=HP바 왼쪽 위 고정, 상태 영역=아이템 오른쪽. 상태가 0개이거나 행 수가 늘어도 아이템 좌표는 바뀌지 않는다. 아이템 없음은 해당 아이콘을 숨기며 BuffIconRoot에 빈칸을 예약하지 않는다.
- 병과 Sprite 원본/UnitHUD.categoryImage 책임은 유지한다. 위치만 사용자가 현재 HP바 옆에 둔다. InfoPanel로 되돌리거나 카테고리를 중복 생성하지 않는다.
- UnitHUD에 필수 `itemIcon` Image 참조 추가. 사용자가 기존 BuffIcon 프리팹 인스턴스를 같은 HUD의 Mask와 BuffIconRoot 밖 고정 위치에 놓고 그 **루트 배경 Image**를 연결한다. 자식 Icon Image를 사용하며 크기/위치는 프리팹에서 정한다. 코드가 복제·자동 위치 지정하지 않는다.
- FieldUnitPresenter는 장착 아이템을 UnitHUD.SetItem(nullable UnitHUDStatus)로 전달하고 SetStatuses에는 버프/디버프만 전달한다. 상태 외형/규칙을 UnitHUD가 다시 판정하지 않는다.
- 고정 아이템도 기존 UIInputBinding/UISecondaryClickBinding, 설명 hover/클릭 고정, 아군 우클릭 해제 입구를 재사용한다. 내부 전용 슬롯 번호를 사용해 상태 슬롯 번호와 구분하고, 실제 해제는 기존 입력 제한을 따른다. 아이템 변경/없어짐·Clear·숨김·구조물·풀 반환 때 이전 표시/hover/고정을 정리한다.
- 상태 재배치는 기존 UnitHUDStatusLayout을 그대로 사용한다. 입력 Count는 버프/디버프 수만이며 아이템은 계산에 포함하지 않는다. 버프가 디버프보다 위/같은 행 왼쪽, 좌하단 고정/위로 확장/마지막 행 왼쪽 정렬은 유지한다.
- itemIcon/categoryImage/statusRoot는 다른 HUD나 서로의 내부에 잘못 연결할 수 없도록 검증한다. 아이템의 루트는 Mask 밖, 같은 HUD 소속, categoryImage와 별개여야 한다. 필수 누락은 대상 UIId/필드명이 있는 오류로 거절한다.
- SSOT/SOLID/FW/§4.6: 항목별 사실 원본은 Presenter/Buffer, 상태 배치는 Layout, 아이템 고정 위치는 프리팹 한 곳. 아이템과 상태의 데이터 목록을 분리하되 hover/입력 수명 구현을 재사용하며 새 서비스/UIId/이벤트 구독은 없다. 카메라/Core/선택 패널/체력 연출 변경 없음.
- 계획: 고정 슬롯 입력 계약→Presenter 분리/수명 정리→정적 컴파일→사용자 연결 안내. 씬/프리팹·Unity 에디터 테스트는 사용자 담당이다.

## 9. 사용자 확인 (2026-10-31)

Item Icon의 연결을 자식 Icon에서 부모 BuffIcon 루트 Image로 바꾼 뒤 정상 작동한다고 사용자가 확인했다. 고정 아이템 연결/기본 동작 확인 완료이며 세부 hover/우클릭/풀 수명은 별도 미확인이다. 상세 설명은 현재 프리팹의 descriptionPanel/descriptionText 미연결로 비활성 상태다.


## 10. 사용자 버프 아이콘 표시 확인 (2026-10-31)

BuffIconRoot Pivot/Width를 수정한 뒤 버프 아이콘이 정상 표시된다고 사용자가 보고했다. 기본 연결/표시 확인 완료이며, 자동 시험이나 개별 다중 행/입력/해제 조합 전체의 실행 증거를 뜻하지 않는다. 자세한 기록은 계획의 사용자 확인 항목을 따른다.
