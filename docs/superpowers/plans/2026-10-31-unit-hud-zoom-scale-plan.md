# Unit HUD 줌 비례 크기 Task 계획

- 작성: 2026-10-31. 상태: HZ1·HZ2 코드/정적 검증 및 HZ3 문서·인수 안내 완료. 일반 줌에 따른 HUD 확대·축소는 사용자 확인 완료. 기타 시각/입력 인수는 별도 미확인.
- 원본: [설계](../specs/2026-10-31-unit-hud-zoom-scale-design.md).
- 사용자 후속 지시로 HZ1부터 착수. Phase 종료 보고/정지를 따른다. 씬·프리팹은 수정하지 않으며 커밋·push는 사용자 담당.
- 기존 HUD 배치 작업의 사용자 확인/스테이징은 유지한다. 새 문서는 스테이징하지 않는다. 씬/프리팹과 Unity 에디터 실행은 사용자 담당.

## Phase HZ1 — 투영 계산과 등록 수명

1. UnitHUDProjection.cs/meta 작성. 동일 출력 투영으로 로컬 위치/clip/화면 가시성과 pixel/world-unit을 계산한다. ReferenceResolution/기준 구도 7/파생 밀도 한 정의.
2. Layer Entry.BaseLocalScale 저장, 렌더 직전 절대 배율 적용. 정상 투영 불가 프레임은 숨김. Unregister/Release에서 원래 크기 복구, 카메라 교체 시 재기준화 없음.
3. Ortho 1/2배·2배/해상도 보정/clip/반복 갱신/등록 해제 후 복구를 현재 테스트 구조에서 대조한다. Unity 실행은 하지 않고 가능한 계산 검증·정적 검토를 구분한다.
4. 변경 파일 컴파일/메타/공백 검사. Phase 결과 보고 후 멈춘다.

## Phase HZ2 — 운영 조립·기준 크기·표시 연결

1. Factory의 기준 해상도를 Projection의 ReferenceResolution으로 연결. Overlay/Scaler 설정과 서비스 등록/개폐 유지.
2. Presenter 화면 크기 필드 Tooltip을 기준 구도로 갱신. 기존 이름/값/기본 prefabScale×설정값 적용 유지. 새 씬 필드 없음.
3. 선택/호버 패널의 투영 후 위치 갱신, 기본 HUD/아이템/상태/피해 숫자의 상대 크기, 줌만으로 상태 줄 수가 바뀌지 않음을 대조한다.
4. 실제 빌드 가능성에 따라 의존 참조 범위의 MSBuild 확인. 이전 고정 화면 크기를 전제로 한 테스트/안내가 있으면 현 소유 경계에서 갱신. 다른 작업자 영역은 수정하지 않는다. 보고 후 멈춘다.

## Phase HZ3 — 문서/사용자 시각 인수 인계

1. UnitHUD-screen-layer 안내의 고정 크기 정책을 새 설계 참조로 갱신. 예전 컴파일/미실행 기록은 이력으로 보존한다.
2. 사용자 확인 목록: 기준 구도 크기, 일반 줌/연출 줌/블렌딩, 여러 해상도·화면비, HUD 전체 상대 위치, 아이콘/아이템/설명/모달 입력, 적/장애물, 화면 밖/재진입/반복 풀.
3. 프리팹 Canvas 설정 변경이나 필드 재연결이 필요 없음을 안내하고, 실제 사용자 보고 범위만 완료 기록한다. Unity 시각 인수 전에는 완료로 추정하지 않는다.

## 작성 기록

2026-10-31: 실제 Factory/Presenter/Layer/Anchor/InfoPanel·HoverPanel/카메라 View·Window와 10-18 Overlay 이전 기록을 읽고 설계 작성. 기존 크기 설정은 Combat.unity에서 각각 1.5428572 확인. 출력 Camera 기반의 비례 크기와 등록 해제 시 BaseLocalScale 복구를 설계했다. 코드·씬·프리팹·기존 스테이징 변경 없음. 컴파일/시험 실행 없음.
### HZ1 구현 기록 (2026-10-31)

- 사용자 지시: 설계대로 HZ1부터, 씬/프리팹 수정 금지·에디터 테스트는 사용자 담당.
- `UnitHUDProjection.cs`와 meta 추가. 출력 Camera에서 기준점/카메라 up 1월드 단위를 투영해 화면 위치와 pixel/world-unit을 구한다. 기준 해상도/기준 Ortho Size 7/파생 픽셀 밀도 한 정의. `TryGetScale`은 Unity 네이티브 호출 없는 순수 수치 경계다.
- Layer의 기존 위치 투영을 helper 호출로 교체해 위치/clip/화면 검사 중복을 제거했다. 화면 밖/clip 밖/비유한 좌표/비정상 viewport·투영 배율은 숨김으로 처리하고 유효 프레임에 다시 계산한다. 샘플 끝점이 화면 밖이라는 이유로 숨기지 않는다.
- Entry.BaseLocalScale은 등록 시 한 번 저장. 매 렌더 때 base×보정값으로 절대 대입하며 직전 localScale에 곱하지 않는다. 결과 벡터도 유한한 양수인지 확인해 오버플로/언더플로 값을 쓰지 않는다.
- Unregister에서 HUD가 살아 있으면 기준 배율 복구. Release는 기존 Unregister 반복 경로를 사용한다. 카메라 교체는 기존 숨김/다음 프레임 재투영만 수행하며 기준 배율을 다시 캡처하지 않는다.
- Register 전에 HUD 기본 배율과 Canvas.scaleFactor의 필수 구성 검증을 추가했다. 기존 UIId/동적 패널 등록/선택 sibling/가시성·호버 정리와 렌더 콜백은 유지했다. 새로운 이벤트/서비스/Builder 없음.
- HZ2의 Factory ReferenceResolution 연결·Presenter Tooltip 변경은 아직 하지 않았다. HZ1 구현으로 기존 운영 Layer의 줌 비례 배율 자체는 적용되며, HZ2는 조립의 기준 해상도 SSOT 연결과 표시 계약 대조를 마무리한다.
- 수명 정적 대조: 카메라 교체·숨김은 BaseLocalScale 변경 없음, Unregister/Release 복구, 파괴된 HUD는 기존 제거 경로, 숨김 CanvasGroup/선택 패널 후처리 순서 유지. 실제 카메라/Canvas/풀 반복 실행을 검증한 것은 아니다.
- 컴파일 최초 시도는 BuildProjectReferences=false에서 로컬 Temp/bin의 FMOD/Core 의존 DLL이 없어 CS0006으로 실패. 저장소 코드를 고치지 않고 의존 프로젝트 빌드를 포함해 재실행했다. 최종 결과는 아래 기록을 따른다.
- 새 helper/Layer/현재 설계·계획만 작업한다. Factory·Presenter·씬·프리팹·Core/FieldCamera/Architecture·기존 HUD 스테이징은 변경하지 않았다. Unity 에디터/Play/Test Runner 실행 없음.
### HZ1 검증·종료 (2026-10-31)

- VS 2022 MSBuild ProjectL.Systems.csproj /t:Build /p:Configuration=Debug + Temp/unit-hud-hz1.targets로 신규 helper만 Compile 보충: **종료 코드 0**. 기존 Core CS8632/UI CS0649 경고, 변경 파일 오류/새 경고 없음. 로그: Temp/unit-hud-hz1-build.log. 저장소 csproj는 수정하지 않았다.
- 실제 컴파일된 ProjectL.Systems.dll의 UnitHUDProjection.TryGetScale을 Unity 번들 Mono의 콘솔에서 호출해 **수치 검사 17개 통과**. 기준값 1배/줌 2배·0.5배, 같은 화면비 2배/0.5배 해상도와 CanvasScale 상쇄, viewport 밀도 변화, 0/음수/NaN/Infinity/배율 오버플로 거절을 확인했다. harness: Temp/unit-hud-hz1-check.cs/.exe. 함수의 수치 계산만 실행했고 Unity 에디터/네이티브 Camera 투영은 실행하지 않았다.
- 클리핑/실제 WorldToScreenPoint/Canvas 입력, BaseLocalScale의 실제 풀 Register-Unregister/Release 반복 복구, 카메라 교체·블렌드의 시각 결과는 정적 대조만 했으며 사용자 실행 확인 대기다. 수치 검사 통과를 그 실행 증거로 확대하지 않는다.
- git diff --check 통과. 이번 변경은 UnitHUDLayer.cs/UnitHUDProjection.cs(+meta)와 현재 설계·계획만이다. 사용자 씬/프리팹 및 다른 작업의 변경·Git 인덱스는 조작하지 않았다. 커밋·push 없음.
- HZ1 종료 보고 지점에서 멈춘다. 새 필드 연결이나 Canvas 모드 변경은 필요 없다. HZ2의 Factory 해상도 원본 연결/Presenter Tooltip, HZ3 안내 갱신·사용자 인수 기록은 아직 미착수다.

### HZ2 운영 조립·표시 계약 대조 (2026-10-31)

- 사용자 지시: HZ2 진행, 씬/프리팹 수정 금지, 에디터 테스트는 사용자 담당.
- CombatUnitHUDFactory의 CanvasScaler.referenceResolution을 UnitHUDProjection.ReferenceResolution으로 연결. 중복 1920×1080 정의를 제거했고 Overlay/ScaleWithScreenSize/너비 기준/-10 정렬/출력 display/GraphicRaycaster/Layer 구성은 유지한다.
- FieldUnitPresenter의 unitHudScreenScale/obstacleHudScreenScale Tooltip을 기준 구도에서의 크기·실제 투영 비례로 수정. 필드 이름/직렬화 기본값/유닛·장애물 별도 설정/프리팹 배율 곱하기/AttachUnitHud 순서는 유지했다. 새 필수 연결 필드는 없다.
- 선택·호버: Layer가 위치/배율을 쓴 뒤 RefreshSelectedInfo→HoverPanel.RefreshPosition을 호출한다. InfoPanel은 HUD 자식으로 로컬 마스크 너비를 펼치며, HoverPanel은 같은 HUD의 Mask 밖 자식으로 실제 InfoPanel 월드 모서리→부모 로컬 좌표로 배치한다. 따라서 루트 배율을 이중 적용하지 않으며 gap도 HUD와 같은 비율로 보인다. 등록/개폐/포인터 계약 변경 없음.
- 기본 HP/카테고리/고정 Item Icon/상태/피해 숫자: 같은 HUD 루트의 자식이므로 루트 배율을 공유한다. 고정 아이템의 로컬 위치를 덮어쓰는 코드는 추가하지 않았다. UnitDamageText는 로컬 fontSize/alpha만 갱신하며 HUD 루트 배율을 바꾸지 않는다.
- 버프 줄바꿈: UnitHUDStatusLayout의 입력은 statusRoot.rect.width와 아이콘 프리팹의 로컬 크기/간격이다. HUD 루트 localScale만 변하는 줌에서는 이 폭이 변하지 않아 열 수가 유지된다. 실제 영역 Width를 바꿀 때만 기존 재배치 경로가 동작한다.
- 현재 Assets/Tests 전체(ignored 포함) 검색에서 UnitHUD/Layer 관련 기존 테스트 소스를 찾지 못했다. 옛 안내에 적힌 UnitHUDLayerTests 등의 이름을 이번 시험 실행/통과로 기록하지 않는다. 새 에디터 시험은 작성·실행하지 않았다.
- UnitHUD-screen-layer 안내에 현재 줌 비례 정책을 짧게 표시하고, 이전 고정 크기/시험 기록은 10-18 이력으로 보존했다. HZ3의 전체 사용자 인수/최종 문서 인계는 남겨 둔다.
- 구조 검토: SSOT는 투영 기준을 helper 한 곳에 두고 Factory가 소비, 기능 계산은 HZ1 helper/Layer에 유지. 기존 UI 등록과 Global Context 소비/서비스 계층 변화 없음. 조립 변경은 Factory, Inspector 설명 변경은 Presenter에 한정했다. Core/FieldCamera/Architecture·씬/프리팹·Git 인덱스는 수정하지 않는다.
- 정적 대조는 실제 Unity 표시/입력/동작 검증이 아니다. 컴파일 최종 결과는 아래 후속 기록을 따른다.
### HZ2 검증·종료 (2026-10-31)

- VS 2022 MSBuild ProjectL.Systems.csproj /t:Build /p:Configuration=Debug /p:BuildProjectReferences=false: **종료 코드 0**. 기존 의존 DLL을 사용하는 정적 컴파일이며 Unity 에디터 실행이 아니다.
- 기존 UI 직렬화 필드 CS0649 경고, HZ2 변경 파일 오류/새 경고 없음. 로그: Temp/unit-hud-hz2-build.log. 실제 csproj에 UnitHUDProjection.cs가 포함돼 있어 별도 신규 소스 보충 targets 없이 확인했다.
- HZ2 변경 파일만 대상으로 git diff --check 통과. 화면 배율 계산은 HZ1과 같으므로 이전 수치 검사 17개를 새 실행 건수로 합산하거나 반복 실행하지 않았다.
- Factory/Presenter 코드와 현재 설계·계획·Layer 안내만 갱신했다. 씬/프리팹/에디터 테스트·다른 작업자 영역·Git 인덱스 조작 없음. 실제 선택/호버/아이템/상태/피해 표시와 줌 입력은 사용자 Unity 확인 대기다.
- HZ2 종료 보고 지점에서 멈춘다. Canvas 모드 변경·프리팹 재연결·크기 필드 재입력은 필요 없다. HZ3 전체 인수 안내/사용자 결과 기록은 미착수다.

### HZ3 문서·사용자 인수 인계 완료 (2026-10-31)

- 사용자 지시: HZ3 진행, 씬/프리팹 수정 금지, 에디터 테스트는 사용자 담당.
- UnitHUD-screen-layer 안내를 현재 사용 지침과 10-18 구현 이력으로 구분했다. 이전 화면상 고정 크기 정책·옛 테스트 이름/컴파일/미실행 기록은 지우지 않고 과거 이력으로 보존한다.
- InfoPanel 안내에도 같은 HUD 자식인 패널/아이콘/피해 숫자가 루트 배율을 함께 따름을 명시하고 아래 사용자 목록을 검증 원본으로 연결했다. 사용자 기존 카테고리/고정 아이템/버프 아이콘 정상 표시 확인은 줌 인수와 별개로 유지한다.
- 이번 Phase는 문서만 변경했다. 런타임 코드/씬/프리팹/에셋/다른 작업자 영역/Git 인덱스 변경 없음. Unity/Test Runner/Play 실행·추가 빌드 없음. HZ1 수치 검사 17개와 HZ1·HZ2 MSBuild 통과를 HZ3의 새 실행 결과로 합산하지 않는다.
- 구조 점검: 투영/단위 기준은 Projection, 위치·크기·가시성 수명은 Layer, 생성/Scaler 설정은 Factory, 내용/입력은 기존 Widget이라는 SSOT·SOLID/FW·§4.6 경계를 유지한다. 새 등록/소유/수명 정책을 문서만으로 추가하지 않는다.
- 코드 및 HZ3 문서/인수 안내 작업은 완료. 실제 Unity 결과는 아래 전 항목 미확인이며, 사용자 보고 범위만 추후 확인 완료로 갱신한다. 커밋·push·스테이징은 하지 않는다.

### 사용자 Unity 확인 목록 — 줌 비례 HUD

**새로 연결하거나 바꿀 씬·프리팹 설정은 없다.** 기존 Overlay Canvas를 World Space로 바꾸지 않는다. Category Image/Item Icon/Status Root 등의 기존 연결과 크기 필드 값도 유지한다. 유닛/장애물 기본 크기를 조절하려면 기존 unitHudScreenScale/obstacleHudScreenScale을 기준 구도의 크기 설정으로 사용한다.

| 항목 | 조작/확인 | 기대 결과 | 현재 상태 |
|---|---|---|---|
| 1. 기본 크기·일반 줌 | 기존 전투 진입 후 휠 위/아래로 줌인·줌아웃 | HUD가 유닛과 함께 확대·축소. 같은 구도로 돌아오면 처음 크기. 기준 해상도 1920×1080/Ortho Size 7에서는 기존 크기 | 사용자 확인 완료: 줌 비례 확대·축소. 기준 구도 크기/복귀 개별 검증은 미확인 |
| 2. HUD 내부 배치 | 카테고리/고정 아이템/버프 여러 개를 띄우고 줌 | 아이템의 HP바 왼쪽 위 상대 위치와 카테고리의 HP바 옆 상대 위치 유지. 줌만으로 버프 열 수/순서가 바뀌지 않음 | 미확인 |
| 3. 선택·상세 패널 | 유닛 선택, InfoPanel 호버, 패널이 화면 위/아래에 있을 때 각각 줌 | InfoPanel/HoverPanel도 같은 배율, 펼침/우측 상·하 배치와 간격 정상. 다른 HUD에 가려지지 않음 | 미확인 |
| 4. 입력 | 확대/축소 상태에서 아이콘 hover·클릭 고정, 배치 단계 아군 아이템 우클릭, 모달 열기 | 실제 보이는 Rect에 입력이 맞음. 기존 우클릭 해제/모달 차단 정책 유지. 상태 설명은 Description Panel/Text가 연결된 구성에서만 확인 | 미확인 |
| 5. HP·피해·대상 종류 | 피해/예상 HP 표시 중 줌, 아군/적/장애물 확인 | 숫자/HP 연출이 재시작되거나 멈추지 않음. 유닛/장애물별 기본 크기와 가시성 정책 유지 | 미확인 |
| 6. 연출·카메라 전환 | 기존 포효/기차 등 연출 줌, 낮/밤 또는 카메라 블렌드, 흔들림 | 최종 출력 카메라와 같은 프레임의 크기/위치 반영. 이중 확대·지연·튀는 크기 없음. 연출 중 수동 휠 거절은 기존 정책 | 미확인 |
| 7. 해상도·화면비 | Game 뷰 해상도와 화면비를 바꿔 같은 구도 확인 | CanvasScaler와 이중 확대되지 않으며 유닛 대비 크기 유지. 패널 위치/클릭 영역 일치 | 미확인 |
| 8. 가시성·재사용 | 화면 밖으로 이동 후 복귀, Drag 숨김/취소, 씬 이탈/재진입, 반복 풀 사용 | 이전 HUD 잔상/입력 잔존 없음. 다시 나타나도 확대 배율이 누적되지 않음 | 미확인 |

확인할 때 문제가 있으면 사용한 줌/연출, 선택한 유닛 종류, Game 뷰 해상도, 화면에 보인 현상과 콘솔 오류를 함께 기록한다. 로컬 Temp 빌드/수치 로그는 정적 검증 증거이며 이 사용자 확인 표의 실행 증거가 아니다.
### 사용자 일반 줌 비례 크기 확인 완료 (2026-10-31)

- 사용자 보고: "줌인·줌아웃할 때 HUD도 같이 커지고 작아지는 거 확인했어." 일반 줌에 따른 HUD 확대·축소를 **사용자 확인 완료**로 기록한다.
- Codex의 직접 Unity 실행/자동 시험 결과가 아니다. 연출 줌·블렌드·해상도·패널/아이콘 입력·풀 재사용 등 표의 다른 항목까지 확대해 완료 처리하지 않는다. 기존 코드/정적 컴파일/수치 검사 기록과 구분한다.
- 사용자 요청에 따라 줌 비례 HUD의 코드/신규 Projection+meta/설계·계획·현재 안내만 스테이징한다. 다른 에셋·카메라/맵·몸체 텍스트 작업은 포함하지 않는다. 커밋·push는 하지 않는다.
