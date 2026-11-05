# Unit HUD 줌 비례 크기 설계

- 작성: 2026-10-31. 상태: HZ1·HZ2 코드/정적 검증 및 HZ3 문서·인수 안내 완료. 일반 줌 비례 확대·축소는 사용자 확인 완료. 기타 시각/입력 인수는 별도 미확인.
- 사용자 요청: 줌인/줌아웃하면 Unit HUD도 유닛과 함께 커지고 작아지도록 설계.
- 유지 범위: 현재 카테고리=HP바 옆, 고정 아이템=HP바 왼쪽 위, 버프/디버프=오른쪽 영역. 현재 사용자 확인 완료·스테이징된 HUD 배치 작업을 유지한다.
- 기존 안내: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Unit/UnitHUD-screen-layer.md`의 화면상 고정 크기는 이전 정책이다. 새 정책은 아래 설계를 구현할 때부터 적용한다.
- 기준: Assets/AGENTS.md, Framework architecture/programmer guide, [공동 설계 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리). 씬/프리팹 편집·Unity 에디터 실행 확인은 사용자 담당을 유지한다.

## 1. 현재 구조 확인

| 경계 | 실제 현재 동작 |
|---|---|
| CombatUnitHUDFactory | 공용 Screen Space Overlay Canvas 생성. Scaler=1920×1080, 너비 기준, sortingOrder=-10 |
| FieldUnitPresenter.EnsureUnitHud | 프리팹 배율에 unitHudScreenScale/obstacleHudScreenScale을 한 번 곱함. 현재 Combat.unity 두 값 모두 1.5428572 |
| FieldUnitView / IUnitHUDAnchor | 월드 기준점/가시성 제공. 공격·반응 자세 대여/일반 이동 기준점 정책 유지 |
| UnitHUDLayer | Canvas.preWillRenderCanvases에서 실제 출력 Camera로 위치를 투영하고 화면/clip 밖 숨김. HUD 크기는 갱신하지 않음 |
| CombatCameraView / Window | 보기 크기와 연출 줌을 기존 렌더 창구로 전달. 실제 최종 렌즈/블렌드 결과는 출력 Camera에 반영 |
| InfoPanel/HoverPanel | 같은 HUD 자식. UISystem 동적 등록/개폐, 패널 모서리 기준 호버 위치. Layer가 투영 뒤 HoverPanel 위치를 다시 맞춤 |

따라서 지금은 유닛만 줌에 따라 크기가 변하고 HUD는 화면상 고정 크기다. 위치 추적·공용 Overlay/입력/선택 패널 수명을 유지하면서 투영 배율만 더한다.

## 2. 사용자에게 보일 동작

### Z1. HUD 전체가 같은 배율을 따른다

- 줌인하면 HUD도 커지고 줌아웃하면 작아진다. 일반 줌·연출 줌·카메라 블렌드 모두 **최종 출력 Camera의 그 프레임 투영**을 따른다.
- 범위: 기본 HP바/숫자, 카테고리, 고정 아이템, 버프/디버프, 피해 숫자, 선택 InfoPanel, HoverPanel, 연결된 상태 설명 패널. 같은 HUD 내부에 있으므로 하나의 루트 배율을 함께 적용한다.
- 아이템은 HUD 내부에서 HP바 왼쪽 위에 고정된다. 화면상의 절대 크기/좌표가 고정되는 뜻은 아니다.
- 버프 영역의 Width·셀 크기·간격은 HUD 로컬 단위로 유지한다. 줌만으로 열 수가 바뀌거나 줄바꿈을 다시 계산하지 않는다.
- 카메라 회전에 맞춰 패널을 기울이지 않는다. 화면을 향한 기존 2D UI 형태를 유지한다. 모델 포효 확대 등 개별 모델 연출 배율은 HUD 크기에 곱하지 않는다.
- 모든 등록 유닛/적/구조물 HUD에 적용하되 기존 유닛/장애물 기본 크기 설정은 각각 유지한다.

### Z2. 기본 크기와 투영 보정

현재 값의 의미를 유지하기 위해 **기준 구도 = 1920×1080, Orthographic Size 7**에서 기존 크기와 같도록 보정한다. 이는 2026-10-18 화면 HUD 이전 기록의 근거(옛 월드 배율 0.02 → 현재 화면 배율 1.5428572)와 일치한다.

- 기존 unitHudScreenScale/obstacleHudScreenScale 이름과 직렬화 값은 유지한다. Tooltip/문서의 의미만 "기준 구도에서의 HUD 배율"로 수정한다.
- HUD를 처음 등록할 때 프리팹 배율×기존 설정값인 BaseLocalScale을 보관한다. 게임 상태 사본이 아니라 반환 시 복구할 표시 기준값이다.
- 매번 BaseLocalScale에 현재 투영 보정값을 곱해 **절대 대입**한다. 직전 localScale에 계속 곱하지 않는다.
- 시작/등록 순간의 렌즈를 기준값으로 잡지 않는다. 줌 중 생성한 HUD, 풀 재사용, 카메라 교체가 크기를 다시 기준화하지 않아야 한다.

### Z3. 계산식

HUD 월드 기준점을 p, 최종 출력 Camera의 위쪽 월드 단위 벡터를 u라고 할 때:

```text
현재 pixel/world-unit = 길이XY(Project(p + u) - Project(p))
기준 pixel/world-unit = 1080 / (2 × 7)
보정값 = 현재 pixel/world-unit / (Canvas.scaleFactor × 기준 pixel/world-unit)
HUD.localScale = BaseLocalScale × 보정값
```

- 기준 pixel/world-unit와 Canvas 기준 해상도는 같은 투영 협력 객체의 상수/파생값으로 한 번 정의한다. Factory도 그 ReferenceResolution을 사용해 숫자를 중복 정의하지 않는다. Ortho Size 7은 런타임 렌즈의 원본이 아니라 기존 HUD 크기를 보존하기 위한 역사적 단위 보정이다.
- Orthographic에서는 다른 조건이 같을 때 Size 7→3.5는 HUD 2배, 7→14는 0.5배다.
- Perspective에서는 기준점 깊이/렌즈를 포함한 실제 투영 밀도로 결정된다. 카메라 이동으로 유닛이 작아질 때도 HUD가 함께 작아진다. 단순한 FOV 숫자 비율이나 임의 기준 거리로 계산하지 않는다.
- Canvas.scaleFactor로 나눠 CanvasScaler의 해상도 배율과 이중으로 확대되지 않게 한다. 예: 같은 화면비의 1920×1080→3840×2160은 CanvasScaler=2, pixel/world-unit도 2배라 루트 보정값은 같다. 실제 화면 픽셀 크기는 모델과 함께 2배가 된다.
- 유효한 양수 배율을 그대로 사용한다. 별도 최소/최대 HUD 크기 제한·보간은 넣지 않는다. 줌의 범위/보간은 기존 카메라 소유자가 담당한다.

## 3. 구현 책임

### Z4. UnitHUDProjection 협력 객체

`Combat/Display/Unit/UnitHUDProjection.cs`(internal static, .meta 포함)를 둔다. 출력 Camera·Canvas·Layer Rect·월드 기준점을 입력받아 화면 로컬 위치와 투영 배율을 계산한다. 카메라 선택·대여·렌즈 쓰기·UI 등록·게임 상태는 소유하지 않는다.

- 위치/크기를 같은 투영으로 계산한다. 화면 투영/clip 검사 식을 Layer와 helper에 중복하지 않는다.
- 정상적인 화면 밖/clip 밖/카메라 뒤/0 크기 viewport 등 투영 불가 상태는 false로 돌려 기존 Layer 가시성으로 숨긴다. 화면 밖 샘플 p+u는 배율 계산용이므로 그 샘플이 viewport 밖이라는 이유로 HUD를 숨기지 않는다.
- 비유한/0 투영 결과로 Transform에 NaN/Infinity/0 배율을 쓰지 않는다. 투영 불가 프레임은 표시를 숨기고 유효한 다음 프레임에 다시 계산한다.
- 등록 때의 BaseLocalScale/정상 Canvas 설정은 필수 구성 검증 대상이며 누락/잘못된 값은 대상이 명시된 예외로 거절한다. 지원 경계는 기존 Overlay·일반 Orthographic/Perspective 출력 Camera다. 임의 비선형/특수 projection matrix 지원을 추가하지 않는다.

### Z5. UnitHUDLayer

- Entry에 BaseLocalScale을 추가한다. Register에서 현재 크기를 한 번 보관하고, RefreshPositions에서 helper의 위치·배율을 함께 적용한 뒤 SetVisible/선택 패널 순서를 유지한다.
- 기존 렌더 직전 콜백을 재사용한다. 줌 이벤트 구독/프레임별 임시 배열/별도 카메라 갱신 루프를 만들지 않는다. 허용된 표시 각각에 투영 샘플 1개를 추가하는 O(N) 계산이다.
- 숨김 상태는 기존 CanvasGroup 방식 유지. 화면 밖 숨김으로 HP 연출/고정 설명 데이터를 초기화하지 않는다.
- Unregister 시 HUD가 살아 있으면 BaseLocalScale을 복구한다. Release도 기존 Unregister를 통해 복구한다. 다시 등록할 때 이미 확대된 값을 새 기본값으로 저장하는 누적 오류를 막는다.
- SetOutputCamera는 기존처럼 즉시 숨기고 다음 투영에서 새 출력값을 읽는다. 카메라 교체를 이유로 BaseLocalScale/기준 단위를 다시 저장하지 않는다.
- 위치/크기 적용 뒤 기존 RefreshSelectedInfo와 HoverPanel.RefreshPosition 호출 순서를 유지한다. 같은 Canvas의 마지막 sibling 정책/동적 UI 등록 수명도 유지한다.

### Z6. Factory·Presenter·나머지 표시

- CombatUnitHUDFactory는 기존 Overlay/Scaler/GraphicRaycaster/UIGroup 조립을 유지하고 기준 해상도만 helper의 한 정의를 쓴다.
- FieldUnitPresenter는 기존 크기 적용을 유지하고 Tooltip을 기준 구도 의미로 고친다. 새 필수 Inspector 필드나 월드/화면 단위 혼용을 만들지 않는다.
- FieldUnitView/IUnitHUDAnchor의 기준점·풀/활성 수명과 World Offset을 유지한다. HUD 자체 localScale 소유자는 등록 중 Layer 하나다.
- UnitHUD의 버프 레이아웃/고정 아이템/체력/설명 입력은 수정하지 않는다. InfoPanelMask의 로컬 확장 너비와 HoverPanel의 로컬 gap도 줌과 함께 확대된다.
- UI는 기존 Overlay에서 GraphicRaycaster/EventSystem이 실제 확대된 Rect를 사용한다. 입력 코드에서 역배율 보정이나 별도 raycast를 추가하지 않는다. UI 배경 통과 계약과 아이템 우클릭 정책도 유지한다.
- World Space 전환은 하지 않는다. 현재 Layer의 Overlay 제약/정렬/모달 UI 관계를 그대로 사용한다.

## 4. SSOT·SOLID·FW/JMH·변경 독립성

| 기준 | 점검 |
|---|---|
| SSOT | 렌즈/줌 결과=출력 Camera, 사용자 크기=기존 Presenter 설정, 단위 보정=Projection 한 정의, 등록 중 위치/배율=Layer. 카메라 보기 크기/연출 줌 상태를 HUD에 복제하지 않음 |
| SRP | Factory는 생성/설정, Projection은 좌표/비율 계산, Layer는 등록·가시성/적용 수명, Widget은 내용·입력 |
| OCP | 출력 Camera 결과를 읽으므로 새 줌 연출을 더해도 HUD 기능 분기 불필요 |
| LSP | IUnitHUDAnchor/UIGroup/UIBase의 Open/Hide/Register/Release 의미 유지. 등록 해제 시 원래 배율 복구를 명시 |
| ISP/DIP | 기존 Anchor와 출력 Camera/Canvas만 읽는 좁은 협력. 전체 RenderGroup/Core/DB를 전달하지 않음. 한 helper를 위한 서비스 인터페이스는 추가하지 않음 |
| FW/JMH | 서비스/모듈/Builder/UIId 추가 없음. 기존 Global Context→UI→Factory→Layer 경로 유지. 새 Button 리스너/입력/줌 이벤트 구독 없음 |

§4.6 변경 시나리오: 줌 비율/투영 계산 변경은 Projection과 기하 검증, 표시 생성/해상도 설정 변경은 Factory, 등록/풀/숨김 정책 변경은 Layer에 모인다. 체력/상태 목록/아이템 장착/카메라 조작 내부를 수정하거나 불필요하게 읽을 필요가 없다. 공유 루트 배율 계약이 바뀌므로 선택 패널·호버/아이콘·피해 숫자·가시성·풀 재사용의 실제 화면 회귀는 필요하다.

다른 작업자 Core/FieldCamera/Architecture는 수정하지 않는다. 전투 카메라 조작·렌즈/흔들림/블렌딩 정책도 수정하지 않는다.

## 5. 검증과 인수

- 핵심 기하: Ortho 7/3.5/14 비율, Perspective 거리/FOV, 해상도/화면비/viewport와 CanvasScaler, 여러 깊이/위치의 Anchor, 카메라 roll·블렌드·연출 줌.
- 수명: 줌 중 새 HUD 등록, 숨김→복귀, 반복 Register/Unregister, 카메라 교체/일시 부재, Clear/Exit/재진입에서 누적 확대/잔상 없음.
- 입력/표시: 실제 확대된 아이콘 hover·고정/아이템 우클릭, 모달 가림, 선택 InfoPanel/상세 HoverPanel 위치, HP/피해 연출과 고정 아이템 상대 위치, 버프 열 수 유지.
- 사용자 지시대로 씬/프리팹/에디터 실행은 사용자 담당. Codex는 코드·정적 컴파일과 가능한 순수 계산 검증 결과를 구분해 기록한다. 현재 단계는 문서만 작성했으며 검사 실행/컴파일을 주장하지 않는다.
- 상세 Phase는 [Task 계획](../plans/2026-10-31-unit-hud-zoom-scale-plan.md). 구현 승인 전에는 기존 스테이징 파일·씬·코드를 바꾸지 않는다.

## 6. HZ1 반영 기록 (2026-10-31)

투영 helper와 Layer의 등록 기준 배율/절대 적용/등록 해제 복구를 반영했다. 기준 해상도 Factory 연결과 안내 문구 갱신은 HZ2·HZ3 범위로 남겼다. 씬/프리팹/기존 HUD 스테이징은 변경하지 않았다. 컴파일/수치 검사/실제 Unity 미확인 범위는 [HZ1 실행 기록](../plans/2026-10-31-unit-hud-zoom-scale-plan.md#hz1-구현-기록-2026-10-31)을 따른다.


HZ1 최종 검증: MSBuild 종료 0, 실제 컴파일된 TryGetScale의 순수 수치 검사 17개 통과. 네이티브 카메라 투영·클리핑·등록/풀 복구·입력의 실제 Unity 검증은 수행하지 않았으며 사용자 확인 대기다. HZ2·HZ3는 미착수다.


## 7. HZ2 반영 기록 (2026-10-31)

Factory의 기준 해상도 SSOT 연결과 Presenter의 기준 구도 Tooltip을 반영했다. 기존 직렬화 필드/값과 HUD 생성 시 기본 배율 적용은 유지했다. 선택/호버 위치 갱신 순서, HUD 자식의 상대 크기, 상태 열 수의 로컬 폭 의존을 정적으로 대조했으며 추가 Widget 수정은 필요하지 않았다. 실제 Unity 실행과 HZ3 사용자 인수는 별도 대기다. 자세한 범위는 계획의 HZ2 기록을 따른다.

HZ2 최종 검증: ProjectL.Systems MSBuild 종료 코드 0(기존 의존 DLL 사용), 변경 파일 공백 검사 통과. 실제 Unity 실행은 수행하지 않았으며 HZ3 인수 기록은 별도다.


## 8. HZ3 인수 안내 완료 (2026-10-31)

현재 Overlay 유지/추가 연결 없음과 사용자 Unity 확인 목록을 정리했다. 목록 원본은 [계획의 사용자 Unity 확인 목록](../plans/2026-10-31-unit-hud-zoom-scale-plan.md#사용자-unity-확인-목록--줌-비례-hud)이며 현재 전 항목 미확인이다. HZ1·HZ2 정적 컴파일 및 수치 검증, 이전 HUD 배치의 사용자 정상 작동 보고와 이번 줌 비례의 실제 Unity 인수를 구분한다. 이번 Phase에서는 문서만 변경했다.


## 9. 사용자 일반 줌 확인 (2026-10-31)

줌인·줌아웃 시 HUD도 함께 커지고 작아지는 것을 사용자가 확인했다. 일반 줌 비례 동작 확인 완료이며, 연출/해상도/입력/재사용 등 나머지 항목은 계획의 확인 표에서 별도 관리한다. 직접 Unity 실행이나 자동 시험 통과로 기록하지 않는다.
