# Unit HUD 상태 배치 Task 계획

- 2026-10-31 / 고정 아이템 배치 정정까지 설계·코드·정적 컴파일 완료. 카테고리는 HP바 옆 유지, 아이템은 HP바 왼쪽 위 별도 슬롯. 사용자 Item Icon 연결 및 BuffIconRoot Pivot/Width 수정 후 버프 아이콘 정상 표시 확인 완료. 세부 입력/수명 사례는 별도 미확인.
- 원본: [설계](../specs/2026-10-31-unit-hud-status-layout-design.md).
- 사용자 후속 지시 "작업 다 진행하고 내가 해줘야할거 따로 정리해줘"로 UH2·UH3를 연속 진행한다. 커밋·push는 사용자 담당.
- 씬/프리팹 편집 및 Unity 에디터 테스트는 사용자 담당. Codex는 코드/참조 안내/정적 컴파일을 수행한다.

## Phase UH1 — 표시 대상과 카테고리 책임

1. FieldUnitPresenter의 상태 목록을 아이템→버프→디버프로 조립. 격파 목표/보유 특성 아이콘 추가만 제거한다. 다른 표시/데이터 사용처 유지.
2. CombatUnitStatuses의 분류 호출 순서 변경. 현재 효과인 가속/보호막/공격력 부여 유지.
3. UnitHUD에 기본 categoryImage 참조·표시/정리/구조물 숨김을 둔다. SetInfo의 category를 여기서 소비하고 InfoPanel의 기존 Category 참조/전달/검사 제거.
4. 새 참조의 소속/필수 검증과 Clear/재사용 경로 대조. MSBuild 정적 컴파일.

종료 보고: 변경 계약과 사용자 프리팹 연결 순서. 옛 프리팹은 사용자 연결 전 새 필수 참조 검사에 실패할 수 있음을 기록한다. 임시 중복 Category/fallback은 넣지 않는다.

## Phase UH2 — 아이템 좌상단·상향 줄바꿈

1. UnitHUDStatusLayout과 meta 생성. 동일 셀 크기·유효 폭/간격·열 수·행 수·좌표 계산.
2. SetStatuses의 단일 행 위치 계산을 교체. BuffIconRoot는 좌하단 고정이며 아이템/버프/디버프 순으로 위에서 아래, 행 안은 왼쪽부터 배치.
3. 목록/폭 변경 재배치, 빈 목록 높이 0, 슬롯 재사용과 같은 Key 고정 설명 유지. 자체 LayoutGroup 등 다른 배치 제어와 중복되지 않도록 프리팹 안내에 명시.
4. 경계 대조: 0/1/C/C+1개, 아이템 유무, 버프만/디버프만/혼합, 폭 변경, 삭제·재사용. 아이콘 데이터/정렬 원본 복제 및 프레임당 불필요한 할당 없음 확인. 정적 컴파일.

종료 보고: 코드 완료 범위와 실제 실행 미확인 범위 구분.

## Phase UH3 — 사용자 프리팹 인계·인수 기록

1. 기존 Category를 HUD의 기본 영역으로 이동, UnitHUD.categoryImage에 연결. BuffIconRoot를 오른쪽 영역의 좌하단 기준으로 위치/폭 지정. HoverPanel은 기존 Mask 밖 유지.
2. 사용자가 크기/간격·유닛별 높이/화면 투영·여러 행의 아이템 좌상단/버프 상단/마지막 행 왼쪽 정렬 확인.
3. 상태 아이콘 hover/고정/아이템 우클릭, 선택 InfoPanel, 구조물 숨김, 피해/예상 HP, 풀 재사용 확인은 사용자 에디터 검증으로 남기고 실제 보고 범위만 완료 기록.
4. UnitHUD-info-panels 안내와 현재 설계/계획에 실제 연결·컴파일·사용자 확인을 구분해 갱신.

## 작성 기록

2026-10-31: 사용자 그림과 후속 결정, 현재 UnitHUD/InfoPanel/FieldUnitPresenter/CombatUnitStatuses/프리팹 계층을 대조해 설계·계획 작성. 런타임 코드·씬·프리팹·에셋 수정 없음, 시험 실행 없음.
### UH1 구현 기록 (2026-10-31)

- 사용자 지시: "UH1부터 진행해줘. 씬이랑 프리팹은 내가 연결할게." UH1만 구현하고 종료 보고한다. Unity 에디터 테스트/씬/프리팹은 사용자 담당을 유지한다.
- FieldUnitPresenter의 목록을 아이템→버프→디버프로 변경. 보유 특성/격파 목표 HUD 아이콘 추가 제거. CombatUnitStatuses는 Buff→Debuff 순으로 기존 상태 변환을 호출하며 가속/보호막/공격력 부여의 현재 효과를 유지한다.
- UnitHUD.categoryImage(필수 Image)를 추가해 병과 Sprite를 여기서 표시. 같은 HUD의 InfoPanelMask 바깥 소속 검사와 statusRoot의 같은 HUD 소속 검사를 추가했다. 구조물/초기화/풀 반환 Clear에서 카테고리 Sprite와 표시를 지우고, 유닛 바인딩에서 다시 보인다.
- UnitHUDInfoPanel의 category 직렬화 필드/필수 검사/Sprite 전달을 제거했다. SetInfo의 병과 입력은 UnitHUD에서 소비하며 선택 패널의 행동/이름/공격/방어/체력 공유는 유지한다.
- HUD 목표 전용 상태/SetObjectiveTarget 호출을 걷고 CombatSceneDisplay의 목표 칸 강조용 objectiveTarget은 유지했다. Core 목표 계산/외형/승패를 바꾸지 않는다. 더 이상 쓰지 않는 _traitVisuals 저장 필드는 제거했으나 Configure의 선택 인자는 기존 조립 호출 호환을 위해 유지하고 주석으로 명시했다.
- 정렬 의미를 설명하는 UnitStatusPolarity 주석도 Buff 우선으로 맞췄다. 상태 외형 에셋 값/enum 숫자는 바꾸지 않았다.
- 구조 검토: SSOT(병과 표/현재 상태/Polarity 원본 유지), SRP(상태 변환/목록 조립/위젯 표시), SOLID/FW(새 서비스·Builder·UIId 없음), §4.6(카테고리 연결과 표시 대상 변경만 구현; 체력 연출·입력·카메라 조립 변경 없음) 대조. 새 레이아웃 객체/줄바꿈은 UH2에서만 추가한다.
- 씬/프리팹/에셋은 수정하지 않았다. Unity 재생/테스트 실행은 하지 않았다. 정적 컴파일 최종 결과는 후속 기록을 따른다.

### UH1 사용자 프리팹 연결

1. HUD.prefab의 기존 `InfoPanelMask/InfoPanel/Category` 오브젝트를 **같은 HUD 루트 아래, InfoPanelMask 밖**으로 옮긴다. 체력바 왼쪽 위에 배치한다. 복사본을 남기지 않는다.
2. 루트 `UnitHUD` 컴포넌트의 새 **Category Image** 필드에 옮긴 오브젝트의 Image를 연결한다. 기존 InfoPanel의 Category 필드는 코드에서 제거되어 더 이상 연결하지 않는다.
3. `BuffIconRoot`는 같은 HUD 내부에 유지한다. InfoPanel의 HP/행동/마스크/HoverPanel 및 체력 숫자 참조는 유지한다. HoverPanel은 기존처럼 Mask 밖에 둔다.
4. Category Image가 없거나 다른 HUD/InfoPanelMask 안에 있으면 바인딩 검증에서 오류로 멈춘다. 기존 프리팹에는 자동 fallback이나 중복 카테고리를 만들지 않는다.
5. UH1에서는 상태가 아직 기존 한 줄 배치다. 폭 제한/아이템 좌상단/위로 줄바꿈은 UH2 뒤 연결·확인한다.

사용자 확인 대기: 선택하지 않은 유닛의 카테고리, 선택/해제 시 중복 없음, 아이템→버프→디버프 목록, 보유 특성/목표 아이콘 제외, 구조물 숨김/풀 재사용. 확인 보고를 받기 전 완료로 추정하지 않는다.
### UH1 정적 컴파일·종료 (2026-10-31)

- VS 2022 MSBuild ProjectL.Systems.csproj /t:Build /p:Configuration=Debug: 종료 코드 0. 기존 UI 직렬화 내부 필드 CS0649 경고만 확인. 로그: Temp/unit-hud-uh1-build.log(로컬).
- 목표 HUD 전달을 제거하는 중 필드 강조에 쓰던 objectiveTarget 지역 변수까지 제거해 CS0103이 발생했으며, 강조용 변수는 복원하고 HUD setter 호출만 제거해 최종 빌드 통과했다.
- git diff --check 통과. 씬/프리팹/DB 에셋/Core 코드 변경 없음 확인. 사용자 지시대로 Unity 에디터 테스트는 실행하지 않았다.
- UH1 코드 작업 종료. 새 Category Image 연결과 실제 재생은 위 인계대로 사용자 확인 대기이며, UH2는 아직 진행하지 않았다.

### UH2 코드·UH3 연결 인계 (2026-10-31)

- 새 `GlobalSystem/UISystem/Screens/Stage/UnitHUDStatusLayout.cs`(+meta): 같은 셀 크기/영역 폭/간격/개수로 열 수·행 수·높이·좌표 계산. 순수 C# readonly struct이며 게임 상태/Unity/서비스에 의존하지 않는다.
- UnitHUD.SetStatuses의 한 줄 위치 계산을 교체. 동일 키의 설명 고정/슬롯 재사용/UIInputBinding/아이템 우클릭 계약은 유지한다. 첫 항목(아이템)이 최상단 왼쪽, 버프→디버프, 마지막 행도 왼쪽 정렬. 내용 전체는 좌하단 고정이며 높이만 위로 증가한다.
- `statusIconSpacing`은 기존 가로 간격으로 유지, 새 `statusIconRowSpacing`은 세로 간격(기본 4). 크기는 기존 BuffIcon 프리팹의 Rect와 양수 X/Y 배율에서 읽는다. 새 크기/최대 열 수 설정을 중복 저장하지 않는다.
- 목록 변경 때 배치하고 LateUpdate에서 실제 Rect 폭 변경을 확인한다. 같은 폭이면 배치 계산·Transform 쓰기·GetComponent 검증을 건너뛴다. 상태/좌표 배열을 매 프레임 생성하지 않는다. 0개/Clear는 내용 높이를 0으로 정리한다.
- 구성 검증: 상태가 있을 때 statusRoot(RectTransform)/statusIconPrefab 필요. Root Pivot=(0,0), 세로 Anchor 고정, 한 칸 이상 폭, 양수 크기/배율·회전 0·유효한 0 이상 간격. statusRoot의 LayoutGroup/ContentSizeFitter, 아이콘 프리팹의 ContentSizeFitter는 코드 배치와 충돌하므로 명시적 오류로 거절한다. 잘못된 구성을 자동 보정하지 않는다.
- 순수 C# 실제 소스 검증: VS Roslyn으로 UnitHUDStatusLayout.cs와 로컬 harness를 함께 컴파일/실행, **25개 검사 통과**. 0/1/C/C+1개, 4열 7개 혼합(아이템/버프/디버프는 호출자가 정렬한 항목 인덱스로 검증), 좁아지는 폭/한 열, 첫 항목 위쪽·마지막 행 왼쪽, 캐시 동등성, NaN/0 크기/작은 폭/음수 간격/무한 간격/음수 개수 거절. 파일: Temp/unit-hud-layout-check.cs 및 .exe. Unity/Test Runner 실행이 아니다.
- 구조 검토: 배치 식은 Layout 하나, 기존 사실/분류/목록 조립은 UH1 원본 유지. UnitHUD가 입력/슬롯 수명을 소유하고 계산만 위임한다. 새 서비스/Builder/UIId/이벤트 구독 없음. UIBase/UISystem 개폐와 선택 패널 등록·카메라 투영을 유지한다. 가로 폭 변경과 아이콘 수 변화만 배치에 영향을 주며 체력/피해 기능 내부는 변경하지 않았다.
- UH3 코드/안내 작성까지 완료. 씬/프리팹/입력 에셋/Core는 수정하지 않았다. 사용자 실제 연결/Unity 인수는 아래 별도 목록으로 남긴다. 최종 MSBuild 결과는 후속 기록을 따른다.

### 이전 사용자 작업 목록(UH2 최초 배치, 아래 고정 아이템 정정으로 대체)

대상: `Assets/ProjectL/Prefabs/UI/CombatOnly/HUD/HUD.prefab`. 기존 FieldUnitPresenter의 참조를 쓰므로 새 씬 오브젝트를 만들 필요는 없다.

1. **카테고리:** 기존 `InfoPanelMask/InfoPanel/Category`를 HUD 루트의 Mask 밖으로 이동해 체력바 왼쪽 위에 배치한다. 복사본을 남기지 않고 UnitHUD의 **Category Image**에 해당 Image를 연결한다. InfoPanel의 옛 Category 필드는 제거됐다.
2. **아이콘 영역:** `BuffIconRoot`를 같은 HUD의 기본 영역(Mask 밖)에 둔다. UnitHUD의 **Status Root**에 해당 RectTransform 연결. Pivot=(0,0), 세로 Stretch 해제(Anchor Min Y=Max Y; 권장 전체 Bottom Left 고정). 카테고리 오른쪽·체력바 바로 위를 시작점으로 놓고, 우측 끝까지 사용할 **Width**를 지정한다. Height는 코드가 자동으로 쓴다.
3. **배치 제어 중복 제거:** BuffIconRoot에 Grid/Horizontal/Vertical LayoutGroup과 ContentSizeFitter를 두지 않는다. HUD 상위의 다른 LayoutGroup도 BuffIconRoot의 높이·위치를 제어하지 않도록 한다.
4. **아이콘 프리팹:** 기존 **Status Icon Prefab = BuffIcon.prefab** 참조를 유지한다. 루트 RectTransform의 Width/Height는 양수, Scale X/Y 양수, Rotation=(0,0,0), ContentSizeFitter 없음. 크기는 모두 이 프리팹을 따른다. 자식 `Icon` Image는 유지한다. 각 생성 슬롯의 Anchor/Pivot/위치는 코드가 정한다.
5. **간격:** UnitHUD의 **Status Icon Spacing**(가로)과 **Status Icon Row Spacing**(세로) 조정. 가로 폭은 최소 아이콘 하나 이상 필요하다. 예: 배율 적용 아이콘 폭 W, 가로 간격 G일 때 4열의 최소 폭은 4W+3G다.
6. **기존 연결 유지:** InfoPanel의 마스크/HP/Action1·2, HoverPanel(Mask 밖), 피해 숫자·상태 설명 참조를 유지한다. 상태 설명은 원래 선택 참조이며, 현재 프리팹의 descriptionPanel/descriptionText가 비어 있으면 여전히 아이콘 설명은 열리지 않는다(새로 필수화하지 않음). Button.onClick은 연결하지 않는다.

사용자 확인(미실행): 미선택에서도 카테고리 하나, 아이템만/버프만/디버프만/혼합, 1행→2행→1행 변화, 아이템이 항상 현재 좌상단, 버프가 디버프보다 위/같은 행 왼쪽, 마지막 행 왼쪽 정렬, 상태 0개 시 카테고리 유지. 추가로 아이템 우클릭·설명 고정/삭제·선택 패널·구조물 숨김·풀 재사용·피해/예상 HP를 확인한다.
### 최종 검증·종료 (2026-10-31)

- MSBuild ProjectL.Systems.csproj /t:Build /p:Configuration=Debug + CustomAfterMicrosoftCommonTargets=Temp/unit-hud-uh2.targets: **종료 코드 0**. 신규 Layout 소스만 csproj에 보충하는 로컬 targets이며 저장소 csproj는 수정하지 않았다. 로그: Temp/unit-hud-uh2-build.log. 기존 Core CS8632·UI CS0649 경고, 이번 HUD 변경 파일 오류 없음.
- 순수 C# 배치 계산 25개 검사 통과, 코드·문서 git diff --check 통과. 씬/프리팹/DB 에셋/Core 소스 변경 없음 확인.
- 사용자 요청 범위의 코드·문서 작업 종료. 실제 Unity 연결/표시/입력 인수는 별도 사용자 작업이며 완료로 기록하지 않는다. 외부 발송·커밋·push 없음.

### 고정 아이템 정정 계획·구현 (2026-10-31)

- 사용자 지시: 카테고리는 현재 HP바 옆, 기존 제안 카테고리 자리인 HP바 왼쪽 위에 아이템 고정. 설계/코드 수정, 씬/프리팹은 사용자 연결.
- Task: 설계 §8/H1~H3 정정 → 고정 itemIcon/SetItem 입력 계약 및 기존 설명/우클릭 재사용 → Presenter 분리·수명 정리 → 정적 컴파일 → 아래 사용자 연결 인계.
- UnitHUD의 새 필수 `itemIcon`은 사용자가 배치한 BuffIcon 인스턴스의 루트 Image다. 코드에서 위치/크기를 쓰지 않는다. 자식 Icon의 Sprite, 표시/숨김, 입력 수명만 제어한다. 장착 없으면 숨김.
- Presenter는 SetItem(nullable UnitHUDStatus)과 SetStatuses(버프/디버프만)를 별도 호출한다. 아이템이 상태 레이아웃 Count에 들어가지 않아 행 수/위치를 바꾸지 않는다. 기존 순수 Layout 계산 코드는 변경하지 않았다.
- 기존 UIInputBinding/SecondaryClickBinding을 내부 고정 슬롯 번호로 사용한다. 데이터 접근은 StatusAt/IsStatusValid로 아이템/상태를 구분하며 아이템 해제는 기존 (EntityId, ItemStatusKey) 경로를 유지한다. 상태 목록 변경 시 아이템 설명 고정도 유지하고, 아이템 변경/없어짐·구조물·Clear/풀 반환·숨김에서 표시/hover 수명을 정리한다.
- categoryImage는 기본 HUD에서 병과 Sprite를 표시하는 현재 책임을 유지하고 위치만 사용자 HP바 옆으로 둔다. 새 UIId/서비스/Core/Framework 변경 없음. 기존 선택 패널·체력·카메라 투영 변경 없음.
- 씬/프리팹/에디터 테스트는 수행하지 않았다. 정적 컴파일 결과는 후속 기록을 따른다.

### 현재 사용자 연결 목록 — 고정 아이템 정정

1. **Category Image:** 현재 HP바 옆 위치와 UnitHUD.categoryImage 연결을 유지한다. HP바 위로 옮기지 않는다.
2. **Item Icon(새 필수 필드):** 기존 BuffIcon.prefab을 HUD 루트 아래 별도 인스턴스로 배치한다. InfoPanelMask와 BuffIconRoot 밖에 두고 HP바 왼쪽 위로 위치/크기를 맞춘다. 그 인스턴스의 **루트 배경 Image**를 UnitHUD의 **Item Icon**에 연결한다(자식 Icon Image가 아님). 자식 `Icon`과 기존 프리팹 구조는 유지한다. Button.onClick에 동작을 연결하지 않는다.
3. **BuffIconRoot:** 고정 아이템 오른쪽에 두고 가로 Width를 지정한다. Status Root 연결·Pivot=(0,0)·세로 고정 Anchor·LayoutGroup/ContentSizeFitter 없음 조건은 유지한다. 버프/디버프만 들어가며 Height는 코드가 계산한다.
4. **아이콘 간격/프리팹:** Status Icon Prefab=기존 BuffIcon.prefab 유지. Status Icon Spacing/Row Spacing은 버프/디버프에만 적용한다. 고정 Item Icon의 위치/크기는 사용자가 배치한 인스턴스로 정한다.
5. 확인: 버프/디버프가 0개/여러 행으로 변해도 아이템 위치 고정, 장착 없음은 아이템 숨김, 아군 아이템 우클릭 해제, 아이템/상태 설명 고정 후 목록 변화, 구조물/재진입/풀 재사용 정리. 실제 Unity 확인 전에는 완료로 추정하지 않는다.
### 고정 아이템 정정 최종 검증 (2026-10-31)

- MSBuild ProjectL.Systems.csproj /t:Build /p:Configuration=Debug /p:BuildProjectReferences=false 및 기존 로컬 신규 소스 보충 targets 사용: **종료 코드 0**. 기존 UI CS0649 경고, 변경 파일 오류 없음. 로그: Temp/unit-hud-fixed-item-build.log.
- UIInputBinding/SecondaryClickBinding의 슬롯 전달, 상태 목록에서의 아이템 제거, 고정 아이템의 설명/정리/우클릭 데이터 접근을 정적 대조했다. 씬/프리팹/Core 변경 없음, git diff --check 통과.
- 실제 Unity 버튼/호버/우클릭·아이템 교체/해제·풀 재사용 검증은 사용자 담당으로 남겼다. 이전 순수 배치 25개 통과는 Layout 알고리즘의 기록이며 이번 고정 아이템 입력 수명의 실행 검증으로 합산하지 않는다.

### 사용자 고정 아이템 연결·정상 작동 확인 (2026-10-31)

- 최초 오류는 Item Icon에 자식 Icon Image를 연결해 발생했다. HUD.prefab의 Status Icon Prefab 참조와 BuffIcon 원본 구조는 정상이었고, 고정 itemIcon 참조만 자식을 가리켰다.
- 사용자 보고: 부모 BuffIcon으로 연결을 바꾸자 정상 작동. **고정 아이템 슬롯의 루트 연결 및 기본 정상 동작을 사용자 확인 완료**로 기록한다. Codex 직접 Unity 실행이나 개별 hover/우클릭/풀 수명 검증을 의미하지 않는다.
- 상세 정보 안내 확인: 선택한 유닛의 InfoPanel에 포인터를 올리면 HoverPanel(이름/공격/현재 Shield 표기값)이 열린다. 아이템/버프/디버프 설명은 UnitHUD.descriptionPanel/descriptionText가 모두 연결돼야 hover·클릭 고정이 가능하다. 현재 HUD.prefab에는 둘 다 fileID 0으로 비어 있어 설명 기능이 비활성이다.

### 사용자 버프 아이콘 표시 확인 완료 (2026-10-31)

- 사용자 보고: BuffIconRoot의 Pivot과 Width를 수정한 뒤 버프 아이콘이 정상적으로 표시됨. **버프 아이콘 배치 설정 연결 및 기본 표시를 사용자 확인 완료**로 기록한다.
- 확인 당시 오류는 Pivot=(0.5,0.5)였고 영역 폭 5가 아이콘 폭 9보다 작은 구성이었다. Pivot=(0,0) 및 한 칸 이상 가로 폭 조건을 안내했고 사용자 수정 후 정상 표시 보고를 받았다.
- 기존 고정 아이템 정상 동작 확인과 함께 기본 HUD 연결/표시 인수 기록으로 보존한다. 다중 행 전체 조합·hover/클릭 고정·아이템 우클릭·풀/해제 수명의 개별 수행 여부까지 확대해 완료 처리하지 않는다. 에디터 자동 시험은 실행하지 않았다.
- 사용자 요청에 따라 Unit HUD 코드/신규 Layout+meta/설계·계획·안내 및 사용자가 연결·조정한 HUD.prefab을 스테이징한다. 특성 연출/Preview 도구 등 다른 변경은 포함하지 않으며 커밋·push는 하지 않는다.
