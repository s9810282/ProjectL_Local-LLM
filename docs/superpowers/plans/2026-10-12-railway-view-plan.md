# 전투 레일 표시와 열차 연출 Task 계획

작성일: 2026-10-12. 갱신일: 2026-10-13. 대상: Combat View. 상태: RW1~RW3 코드와 RW4 실제 리소스 연결 완료, 독립 Unity 운영 경로 검사 통과. 원본 Unity Test Runner·Combat 화면 확인 대기.

기준은 [레일 표시와 열차 발동 연출 설계](../specs/2026-10-12-railway-view-design.md)다. 사용자가 설계를 확인하고 Task 계획 작성을 지시했으며, 이어서 RW1~RW4 진행을 지시했다. **RW4의 실제 기차·선로·바닥·Clip·Controller 연결을 마쳤고 원본 Unity 검증을 남긴 리뷰 지점이다.** 현재 스테이지 입력은 사용자 RW3 추가 지시에 따라 9×9 중앙 가로선(x=0~8, y=4)이다. RW4 중 추가 승인으로 적 본부는 기존 x·y=7로 맞췄다. 아래 RW2의 세로선·배치 유지 기록은 변경 전 이력이다.

Task 상태·실행 기록·검증 증거는 이 문서에만 적는다. 기존 [View 계획 §7.70](2026-10-01-combat-view-plan.md)은 시작점 링크만 유지한다.

## 1 범위와 진행 규칙

- 가로·세로 직선 여러 개를 지원한다. 서로 떨어진 상하좌우 연결 성분을 직선으로 도출하고, 꺾임·교차·서로 붙은 병렬 선로는 좌표를 담은 View 설정 오류로 진단한다.
- 좌표는 CombatStateData와 결과 사건, 외형은 RailwayVisualData, 강조 상태·색은 기존 FieldHighlightState·FieldView, 결과 재생 상태는 CombatResultPlayback을 원본으로 사용한다.
- 열차 통과를 완료한 뒤 기존 체력·사망 사건을 표시한다. View가 KilledUnitIds나 콜라이더로 피해를 적용하지 않는다.
- Core·Buffer·FieldCamera·Architecture·Render 내부·셰이더를 수정하지 않는다. 새 서비스·Factory 등록·전역 Manager·일반 기믹 레지스트리·테스트 asmdef를 만들지 않는다.
- 설계와 다른 결정이 필요하면 구현 전에 묻는다. 다른 작업자의 변경은 덮어쓰거나 되돌리지 않는다.
- **Phase 하나가 끝나면 보고하고 멈춘다.** 코드 완료와 Unity 확인 완료를 구분한다. 새 지시 없이 다음 Phase를 구현하지 않는다. 커밋·push는 사용자가 한다.

아래 경로 약칭은 파일 목록을 줄이기 위한 것이다. `C`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat`, `D`는 그 아래 `Display`, `R`은 `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Railway/Item`, `T`는 `Assets/Tests/EditMode/Framework`다.

## 2 Phase와 의존성

| Phase | 목표 | 선행 조건 | 리뷰에서 확인할 결과 |
|---|---|---|---|
| RW1 | GridOutline 레일 상시 표시와 직선 경로 계산 | 구현 착수 지시 | 정적 Rail 슬롯, 경로 도출·오류 진단, 관련 EditMode 증거 |
| RW2 | 선로 바닥·선로 프리팹과 비활성 열차 설치 | RW1 리뷰 후 진행 지시 | 두 표시 모드에서 프리팹 위치·회전·폭 일치, 재설치·해제 안전성 |
| RW3 | 발동 사건·열차 이동·선택적 애니메이션·이펙트 | RW2 리뷰 후 진행 지시 | 열차 완료까지 사건 대기, 반복 발동·취소·실패·종료 계약 검증 |
| RW4 | 실제 모델 연결과 운영 재생 확인·인계 | RW3 리뷰 후 진행 지시 | 실제 에셋 상태, Database Live 재생 결과, 남은 검증·모델 입력 구분 |

RW1은 순수 계산과 기존 표시기만 다루므로 실제 기차 모델을 기다리지 않는다. RW2·RW3은 View 소유 임시 래퍼로 기능을 검증할 수 있다. RW4에서는 임시 외형으로 확인한 기능과 완성 모델까지 확인한 표현을 별도로 기록한다.

## 3 Phase RW1 레일 상시 표시와 경로 계산

### 목표와 파일

GridOutline 대기 중에도 레일 테두리가 보이도록 하고, 이후 프리팹·이동에서 사용할 직선 계산을 만든다. `D/FieldOutlineRenderer.cs`, 신규 `D/RailwayTrackLayout.cs`와 .meta, `T/FieldOutlineRendererTests.cs`, 신규 `T/RailwayTrackLayoutTests.cs`와 .meta가 대상이다. `FieldHighlightState`의 상태 구조와 SO 값은 바꾸지 않는다.

### Task

- [x] RW1.1 현재 변경·관련 파일의 동시 작업 여부와 기존 테스트 상태를 확인한다. 코드가 승인 설계의 경계와 달라졌으면 차이를 먼저 정리한다.
- [x] RW1.2 RailwayTrackLayout을 순수 C#으로 작성한다. 보드 크기·좌표 유효성·중복을 검사하고, 상하좌우 연결 성분을 도출한 뒤 X 또는 Y가 같은 직선인지 검증한다. 빈 레일은 빈 경로, 단일 칸은 +Z 경로다.
- [x] RW1.3 좌표·경로 열거 순서를 고정하고 각 경로의 칸·축·끝점을 불변 결과로 제공한다. 가로 기본 방향은 +X, 세로는 +Y다. Reverse를 사용할 때 시작·끝이 뒤집혀도 집합·길이는 유지되는지 확인한다. UnityEngine 타입·DB 조회·Buffer 쓰기를 넣지 않는다.
- [x] RW1.4 FieldOutlineRenderer의 슬롯 첫 번째에 Rail을 추가한다. 기존 ZoneAt와 Palette.Rail을 사용하고, Refresh의 대기 조건에서도 Rail을 수집한다. 기존 행위자·행동 중 층·호버·배치 구역·데칼 조건은 유지한다.
- [x] RW1.5 Rail 핸들의 생성·캐시·반환을 기존 슬롯 수명에 연결한다. 추가 슬롯 때문에 바뀌는 기존 핸들 개수·높이 순서 기대값을 수정하고, 인덱스에 의존하는 테스트를 확인한다.
- [ ] RW1.6 아래 경계 사례와 기존 강조 회귀를 실행한다. **분리 컴파일·관리 검사·독립 Unity 네이티브 41건 통과, 원본 Unity Test Runner·시각 확인 대기.** 증거는 실행 기록을 따른다.
- [x] RW1.7 SSOT·SOLID·FW 점검, 변경 파일·증거·남은 확인·RW2 설명을 보고하고 멈춘다.

### 검증과 완료 조건

경로 테스트는 가로·세로·떨어진 다중 직선·끊긴 구간·단일 칸·빈 배열·배열 순서 차이, 잘못된 크기·중복·보드 밖·L자·교차·이웃 병렬을 다룬다. 오류에 문제 좌표가 포함되는지 확인한다.

표시 테스트는 대기·배치·행동 중 Rail 유지, Rail보다 높은 층의 높이, Rail이 없어도 대기 격자·호버를 켜지 않는 동작, 같은 상태 반복 갱신과 반복 Release를 확인한다. 기존 `FieldOutlineRendererTests`·`FieldHighlightStateTests`를 함께 실행한다. 실제 프리팹이 없는 이 단계의 완료 결과는 **레일 테두리와 경로 계산**이며 기차 연출 완료로 보고하지 않는다.

## 4 Phase RW2 정적 프리팹과 열차 설치

### 목표와 파일

받은 RailCells·Mapper·RailwayVisual 설정으로 선로 바닥과 선로를 설치하고 경로마다 숨긴 열차 하나를 준비한다. 추가 사용자 지시로 전투 스테이지 11개의 필드 참조를 기존 Field_9x9로 바꾸고 x=4, y=0~8의 중앙 세로 레일을 입력한다. 활성화·발동 주기와 배치는 기존 값을 유지한다. 신규 `D/FieldRailwayPresenter.cs`, 신규 `D/RailwayTrainVisualBinding.cs`, `C/CombatViewSystem.cs`, `C/CombatSceneDisplay.cs`, `R/RailwayVisualDataSO.cs`가 대상이다. 새 .cs의 .meta와 View 소유 래퍼 프리팹, 기존 RailwayVisual_Default 에셋 연결, 설치·재사용 테스트를 포함한다.

### Task

- [x] RW2.1 기존 RailwayVisual 직렬화 이름·GUID를 유지하고 `railTileYOffset`·`railYOffset`의 유한성 검증을 추가한다. 높이를 결정할 때 기존 Field 테두리 높이와 겹치지 않게 확인한다. 규칙 값·좌표·경로 길이 필드를 추가하지 않는다.
- [x] RW2.2 RailwayTrainVisualBinding을 만든다. 명시적 modelRoot와 선택적 Animator 참조만 가지며 Update·재생 상태를 소유하지 않는다. 애니메이션 설정이 있는데 Animator가 없으면 설정 오류로 진단할 수 있게 한다.
- [x] RW2.3 `Assets/ProjectL/Prefabs/Stage/Railway`에 바닥·선로·열차의 임시 래퍼를 만든다. 기본 단위·피벗·+Z 방향·열차 폭 1을 맞추고 원본 Cube는 수정하지 않는다. 각 래퍼와 폴더 .meta를 만든다. 기본 RailwayVisual의 세 참조를 이 래퍼로 연결하고 임시 에셋임을 실행 기록에 남긴다.
- [x] RW2.4 FieldRailwayPresenter의 설치·Clear·Release를 작성한다. RW1 경로를 재사용해 칸마다 바닥·선로를 생성하고, 경로마다 비활성 열차 하나를 준비한다. Collider가 남은 임시 인스턴스는 비활성화한다. 프레임별 검색이나 재생 때마다 Instantiate하지 않는다.
- [x] RW2.5 간격은 Mapper.TotalWidth/ColumnCount와 TotalLength/RowCount에서 얻는다. 칸 중심은 CellCenterToLocal, 보드 밖 좌표는 LocalToWorld의 기존 계약을 사용한다. 가로 회전·비정사각 칸·열차 폭·TrainModelYaw를 적용한다. 별도 간격 설정을 복제하지 않는다.
- [x] RW2.6 경계에 닿은 끝에만 OutsideCells 장식 선로를 연장한다. 내부 끝은 비레일 칸으로 연장하지 않는다. 소수 OutsideCells는 끝의 마지막 조각 길이에 반영하며 실제 RailCells·집기에는 포함하지 않는다.
- [x] RW2.7 CombatViewSystem.InitData에서 기존 RailwayVisualTableQuery.Config를 받고 SceneDisplay 생성자에 넘긴다. SceneDisplay.Install은 Field 구성 성공 후 Presenter를 설치한다. Refresh마다 선로를 다시 만들지 않는다.
- [x] RW2.8 설치 실패·재설치·Clear·Release에 Railway 정리를 연결한다. Mapper 해제보다 먼저 정리하고 다른 Presenter 정리도 모두 시도한다. 실패 중 생성한 인스턴스까지 제거한다. 생성자 변경의 기존 테스트 호출자도 갱신한다.
- [ ] RW2.9 **테스트 컴파일·정적 에셋 검사·독립 Unity 설치/DB 검사 완료, 원본 Test Runner·시각 확인 대기.** 설치 위치·수량·회전·비활성 열차·반복 설치/해제 테스트와 `CombatDisplayReuseTests`를 실행한다. 에셋을 바꿨으므로 DataBaseAssetTests를 실행한다. 독립 검증의 기존 회귀 실패는 RW3 기록에 구분한다. Unity 두 표시 모드에서 정적 표시를 확인하고 결과를 기록한다.
- [x] RW2.10 SSOT·SOLID·FW 점검, 임시 프리팹 목록·정적 표시 증거·RW3 설명을 보고하고 멈춘다.

### 검증과 완료 조건

빈 레일에서는 선로·열차가 없고, 각 유효 칸에는 바닥과 선로가 한 번만 생긴다. 서로 다른 칸 간격·비정사각 간격·필드 이동과 Y 회전에서도 Mapper 기준 배치가 일치해야 한다. GridOutline 테두리는 현재 회전 제약을 유지한다.

부분 직선·경계 직선·Reverse에 대해 바깥 선로 연장과 모델 방향이 맞는지 확인한다. Clear·Release·재설치·부분 설치 실패에서 이전 선로와 열차가 남지 않아야 한다. 이 단계에서는 **열차 발동 사건을 아직 지연하지 않는다.** 기존 즉시 기믹 사건 표시를 유지한다.

## 5 Phase RW3 열차 발동 사건과 자원 수명

### 목표와 파일

Railway 사건이 기존 OrderedCombatPresentation에서 열차 완료를 기다리도록 연결한다. `D/FieldRailwayPresenter.cs`, `C/CombatSceneDisplay.cs`, `C/CombatViewSystem.cs`, 필요한 `R/RailwayVisualDataSO.cs` 검증, Presenter·재생·정리 테스트가 대상이다. OrderedCombatPresentation·CombatResultPlayback의 결과 순서와 확인 계약은 변경하지 않는다.

### Task

- [x] RW3.1 FieldRailwayPresenter에 사건 Begin·Tick·Cancel을 추가한다. Begin은 사건 AffectedRailCells를 설치 집합과 비교한다. 배열 순서 차이는 허용하고 집합 차이는 오류로 거절한다. 최신 Buffer를 다시 읽어 사건을 덮지 않는다.
- [x] RW3.2 모든 경로의 열차를 동시에 활성화하고 Duration 동안 시작에서 끝으로 이동한다. Reverse는 방향만 바꾸고 TrainModelYaw는 외형만 보정한다. 내부 끝은 해당 끝에서 생성·숨기며 TravelPaddingCells는 경계 끝의 보드 밖 이동에만 사용한다.
- [x] RW3.3 Clip/Controller 없음은 직선 보간, Clip 지정은 Clip 우선, Controller만 지정은 기본 상태의 외형 재생으로 연결한다. PlayableGraph는 Manual이고 기존 Tick에서만 평가한다. Root Motion·자동 Animator 갱신과 Presenter 이동이 중복되지 않게 한다.
- [x] RW3.4 Clip 루트 Z 시작·현재 값과 ClipEndZ로 진행률을 정규화하고, 이동 곡선이 없는 Clip은 외형만 재생한다. 분모·유한성·Animator 참조를 검사하고 완료 끝점은 정확히 맞춘다. 과거 RoadRoller의 실제 루트 이동 추출을 독립 Unity에서 확인했다.
- [x] RW3.5 CombatViewSystem.PostBindService에서 기존 `global.RenderServiceCommand.CoreGroup.EffectSystemServices`를 받고 SceneDisplay에 주입한다. 새 등록은 하지 않는다. Empty는 요청하지 않고 그 외에는 사건 소유 EffectScope를 받아 열차 앵커에 재생한다.
- [x] RW3.6 모든 경로 완료 뒤 Scope.StopAll·그래프 정리·열차 숨김·내부 재생 상태 비움을 마친 후 완료 콜백을 한 번 호출한다. Scope에 없는 Dispose 계약을 가정하거나 대여 GameObject를 직접 제거하지 않는다.
- [x] RW3.7 CombatSceneDisplay의 CanPresent·BeginEvent에서 Railway만 라우팅한다. 나머지는 기존 CombatSceneEventPresenter로 보낸다. TickPresentation은 두 Presenter, CancelPresentation은 두 Presenter의 정리를 모두 시도한다. 일반 사건 Presenter에 열차 분기를 복제하지 않는다.
- [x] RW3.8 반복 Cancel, Cancel 뒤 Tick, 늦은 완료, 완료 콜백 재진입, Clear·Release 중 재진입을 방어한다. 콜백/세대를 먼저 무효화하고 자원을 정리한다. Cancel은 완료 통지를 보내지 않는다.
- [x] RW3.9 Begin·Tick·정리 실패를 기존 오류 경계에 전달하고 부분 자원 정리를 모두 시도한다. 정리 하나가 실패해도 다른 정리를 계속하고 원래 오류와 함께 보고한다. 실패와 성공 콜백이 함께 나오지 않게 한다. Scope 반환 실패의 공개 계약 한계는 아래 기록·Render 요청으로 분리한다.
- [x] RW3.10 기믹 → 열차 완료 → HealthChanged·Died → 결과 완료의 순서 테스트를 작성한다. 실제 SceneDisplay 라우팅과 KilledUnitIds만으로 유닛을 제거하지 않는지 확인한다. 중간 Tick에서 다음 사건과 결과 완료가 진행되지 않아야 한다. 결과 확인의 기존 Playback 회귀도 통과했다.
- [ ] RW3.11 **독립 Unity의 Railway Presenter 33건·실제 씬 순서 1건과 주요 Playback/View/종료 회귀 통과. 원본 Test Runner·등록 Effect·운영 확인 대기.** 반복 발동·다중 직선·정상 완료·부분 시작 실패·취소·해제·애니메이션·Empty/지정 Effect 사례와 아래 기존 회귀를 실행한다. 독립 검증의 전체 305건 중 17건 실패는 아래 구분한다. 테스트 이펙트는 좁은 기존 계약만 구현하며 새 전역 서비스나 일반 자원 관리 계층은 없다.
- [x] RW3.12 SSOT·SOLID·FW 점검, 사건 순서와 자원 증거·Unity 확인 상태·RW4 설명을 보고하고 멈춘다.

### 검증과 완료 조건

정상 완료 전에는 다음 피해·사망 사건과 결과 확인이 대기해야 한다. 완료 통지 전에 활성 열차·그래프·대여 이펙트가 정리돼 있어야 하며, Cancel 뒤에는 통지가 없다. Cancel은 재사용을 위한 숨긴 인스턴스를 남길 수 있지만 Clear·Release는 모두 제거한다.

Render Scope 반환 자체가 실패하면 View가 미반환 대여 자원을 직접 제거할 수 없다. 이때는 성공 통지를 보내지 않고 그래프 정리·열차 숨김을 끝까지 시도하며, 숨긴 앵커를 보존하고 오류를 전달한다. 부분 반환 뒤 재시도의 안전성은 [Render 확인 요청](../handoffs/2026-10-13-railway-effect-scope-return-note.md)으로 분리한다. 이 실패 경로는 정상 완료·취소·해제의 자원 제거 증거와 구분한다.

기존 `CombatResultPlaybackTests`, `CombatPresentationCleanupTests`, `CombatSceneEventPresenterTests`, `CombatMovementViewTests`, `CombatViewSystemTests`, `CombatProgressIntegrationTests`, `CombatPlanDecideTests`를 변경 영향에 맞춰 확인한다. 정상 종료·포기는 마지막 연출과 확인을 기다리고, 오류·Detach만 취소한다. FocusLost는 기존 입력 취소만 수행하는지 확인한다. 셰이더·Render 내부 자원 반환 결함이 확인되면 해당 작업자 요청으로 분리한다.

## 6 Phase RW4 모델 연결과 운영 확인

### 목표와 파일

실제 프리팹·애니메이션·선택적 이펙트를 기존 RailwayVisual 설정에 연결하고 운영 경로를 확인한다. View 소유 래퍼 프리팹, RailwayVisual_Default, 필요한 검증 fixture, 설계·이 계획·View 인계 안내가 대상이다. 실제 모델의 원본 파일이나 다른 작업자의 배경·카메라는 수정하지 않는다.

### Task

- [x] RW4.1 기존 develop-4week의 Train·TrainLoad·TrainTile, TrainSkill·RoadRoller를 원본 GUID와 함께 복원하고 RailwayVisualConfig 값을 현재 SO에 대조한다. 실제 모델·애니메이션 연결 검사를 임시 래퍼 검사와 구분한다. 선택 TrainEffect는 현재 enum 미등록·원본 의존성 누락으로 Empty를 사용하며 Render 인수 메모에 남긴다.
- [ ] RW4.2 실제 모델을 View 소유 래퍼 안에서 단위·피벗·방향·높이·폭 계약에 맞춘다. Animator와 modelRoot를 명시적으로 연결하고 기본 RailwayVisual 설정을 갱신한다. 기존 운영 재생 시간·외부 거리 설정이 적절한지 화면에서 확인한다.
- [ ] RW4.3 DataBaseAssetTests와 RW1~RW3 관련 EditMode 테스트를 실행한다. 실제 GUID 참조·직렬화 저장/재로드·누락 Animator·Effect 등록을 확인하고 에셋 검증과 Unity 재생 증거를 따로 기록한다.
- [ ] RW4.4 Database Live에서 레일이 있는 선택 스테이지의 대기·배치·이동·공격·첫 발동·반복 발동을 확인한다. 가로·세로·다중·단일·부분/경계 경로, Reverse·TrainModelYaw, 두 표시 모드, Clip/Controller 없음·각각·함께 지정, Empty/등록 Effect를 확인한다.
- [ ] RW4.5 레일 위 유닛 사망·메인 사망·마지막 결과의 일반 종료와 승패 우선·포기 대기, 오류 정지·씬 해제·재진입에서 연출 완료/취소 및 잔여 인스턴스를 확인한다. 실제 게임 규칙은 변경하지 않는다.
- [x] RW4.6 테스트용 RailCells·발동 주기·유닛 배치는 fixture 또는 임시 검증 에셋에서만 준비한다. 기존 운영 스테이지를 임의 레벨 디자인으로 바꾸지 않는다. 실제 스테이지 변경을 요청받았다면 검증용 값과 운영 값의 출처를 기록한다.
- [x] RW4.7 설계의 구현 결과와 이 계획의 증거를 정리하고 설정 안내를 작성한다. 승인된 레일 상시 정책을 기존 GridOutline 구현 상태와 맞춘다. 다른 작업자 공유가 필요하면 문서를 작성하되 외부 발송은 하지 않는다.
- [x] RW4.8 완료·대기·실제 에셋 연결 상태와 남은 외부 입력을 보고하고 멈춘다. 커밋·push는 하지 않는다.

RW4.2의 래퍼 authoring·기본 설정 연결, RW4.3의 독립 네이티브 에셋/관련 검사, RW4.4의 Database Live 첫·반복 발동 검사는 완료했다. 원본 Test Runner와 RW4.2~RW4.5의 전체 화면 확인은 남아 있으므로 해당 Task를 전체 완료로 체크하지 않는다.

### 완료 조건과 대기 구분

**코드 기능 완료**는 두 표시 모드의 정적 선로, Database Live의 열차 사건, 피해·사망 순서, 반복 발동과 정상 종료·취소·해제 계약이 검증된 상태다. **최종 표현 완료**는 실제 선택 모델·Clip·Controller·Effect 연결과 화면 확인까지 끝난 상태다. 실제 모델이나 선택적 자원이 없어 입력 대기인 항목은 체크하지 않고 남긴다. 선택하지 않은 Clip·Controller·Effect는 설계상 선택 기능이므로 없음/Empty로 정상 완료할 수 있다.

## 7 매 Phase 리뷰 점검

| 기준 | 확인 사항 |
|---|---|
| SSOT | 좌표·피해·발동 규칙을 View SO/Presenter에 복제하지 않고, 현재 사건은 사건 스냅샷으로 재생한다 |
| SOLID | 경로 계산과 표현, 사건 순서와 결과 상태, 씬 조립 책임이 분리된다. 일반 사건 Presenter가 열차를 소유하지 않는다 |
| JMH 지침 | SO는 정적 설정, 검색은 설치 시 필요한 명시적 바인딩에 한정, 불필요한 System·Service·Pool·인터페이스 없음 |
| FW-001~004 | 기존 등록 서비스의 공개 계약만 사용하고 Build 이후 구조 변경·내부 Store 우회 없음 |
| FW-011~012 | Rail 핸들과 EffectScope 반환, 실제 Effect 등록 전제 충족, 대여 자원 직접 Destroy 없음 |
| FW-016 | DB·서비스 주입 후 씬 설치, 기존 Tick에서 재생, Mapper 해제 전에 Railway 정리 |
| 작업 경계 | Core·Buffer·FieldCamera·Architecture·Render 내부·옛 Missing Script 에셋 변경 없음 |

검증은 필요한 변경 범위에 맞춰 실행한다. 새 파일이 생성 csproj에 아직 없으면 기존 임시 AdditionalSources.targets 방식으로 컴파일할 수 있으나, 실제 Unity 컴파일·Test Runner·재생 확인과 구분한다. 소스·프리팹 .meta, UTF-8/LF, GUID 충돌, git diff --check를 확인한다. 문서 작성·관리 실행·Unity 실행의 증거를 서로 구분해 기록한다.

## 8 실행 기록

### 2026-10-12 설계 확인과 Task 계획 작성

사용자가 설계를 확인하고 Task 계획 작성을 지시했다. RW1~RW4의 대상 파일·Task·완료 조건·회귀와 Phase 리뷰 지점을 작성했다. 설계 상태와 기존 GridOutline의 레일 숨김 정책 참조를 갱신하고, 기존 View 계획에는 이 문서 링크만 추가했다.

계획 작성 당시 RW1~RW4는 모두 미착수였다. 코드·씬·에셋 변경, 컴파일·EditMode·Unity 재생, 커밋·push는 수행하지 않았다. 구현 시작점은 **RW1 레일 상시 표시와 경로 계산**이며 실제 모델 입력 없이 시작할 수 있도록 나눴다. 이후 결과는 아래 RW1 기록을 따른다.

### 2026-10-12 RW1 코드 완료와 Unity 확인 대기

사용자가 RW1 진행을 지시했다. 아래 작업만 수행하고 Phase 리뷰 지점에서 멈춘다. RW2 프리팹 설치·RW3 열차 사건 연결은 진행하지 않았다.

**구현 결과**

- FieldOutlineRenderer 첫 슬롯에 Rail을 추가했다. 기존 ZoneAt와 Palette.Rail로 대기 중에도 레일 테두리를 유지한다. 레일 때문에 IsActing이나 격자 데칼·호버가 켜지지 않고 기존 배치·행동 조건을 유지한다. 높이 순서는 레일 → 아군 배치 구역 → 기존 층 → 호버다.
- 새 RailwayTrackLayout은 Unity 독립 순수 C#이다. 레일 칸만 HashSet·Queue로 순회하므로 보드 전체 크기로 할당하지 않는다. Y·X 순으로 경로·칸 순서를 고정하고, 불변 Tracks/Cells와 기본 축 방향·Reverse 끝점을 제공한다. null·잘못된 크기·중복·보드 밖·꺾임·교차·이웃 병렬 입력을 진단한다. 실제 SceneDisplay 설치에 이 계산을 연결하는 것은 RW2다.
- RailwayTrackLayoutTests 21건을 추가했다. FieldOutlineRendererTests에 레일 상시 유지·층 높이·캐시·제거·반복 해제 사례를 추가하고 Rail 핸들 추가에 따른 개수 기대값을 바꿨다. 렌더러 fixture는 총 12건이다.
- 신규 소스·테스트 .meta를 작성하고 GUID 중복 없음, 변경 소스 UTF-8/LF, U+FFFD 없음, 순수 계산의 UnityEngine 참조 없음과 git diff --check를 확인했다.

**검증 증거와 한계**

| 검증 | 결과 |
|---|---|
| 현재 트리 MSBuild | 실패. 작업 중 생긴 범위 밖 UnitHUD.cs의 playerHealthColorfh 선언과 playerHealthColor 사용 불일치, CS0103 한 건 |
| RW1 분리 컴파일 | Systems 컴파일 뒤 Framework 테스트 컴파일 성공, RW1 변경 파일 새 경고 0. UnitHUD만 HEAD 버전을 Temp에 복사해 검증 입력으로 사용한 결과이며 현재 트리 전체 성공과 구분 |
| 관리 실행 | 분리 컴파일한 테스트 DLL을 Unity Mono로 실행해 RailwayTrackLayoutTests 21/21, FieldHighlightStateTests 8/8, 합계 29/29 통과 |
| Unity EditMode | 미확인. 열린 에디터도 위 UnitHUD 오류로 컴파일 성공·Test Runner 실행 결과를 얻지 못함. FieldOutlineRendererTests 12건은 컴파일 확인만 완료 |
| Unity 시각 확인 | 대기. Database 준비 결과에 레일이 있는 전투에서 대기·배치·행동 전환의 레일 표시 확인 필요 |

스크래치 증거는 `Temp/RailwayViewRW1/build-rw1.log`(현재 트리 실패), `build-isolated.log`와 `build-isolated-tests.log`(Systems 분리 컴파일과 수정 후 테스트 컴파일), `managed-rw1.log`(29건 통과)다. `IsolatedSources.targets`는 검증에만 UnitHUD 커밋본을 대입한다. 실제 UnitHUD 파일·변경은 그대로 두었으며 수정·되돌리기하지 않았다. 에디터 검증용 임시 소스와 요청 플래그는 제거했고 도구 원본은 Temp에만 남겼다. 자동으로 뒤늦게 테스트를 실행할 요청은 남기지 않았다.

**원칙 점검과 남은 확인**

강조 상태·색을 기존 원본에서 읽고 새 상태 복제를 만들지 않았다. 경로 계산·표시기·결과 재생 책임을 분리했으며 새 서비스·Builder·Pool을 추가하지 않았다. Rail 핸들은 기존 발급·캐시·반환 계약에 연결했다. Core·Buffer·FieldCamera·Architecture·Render 내부·씬·SO 에셋은 수정하지 않았다. 따라서 DataBaseAssetTests의 에셋 변경 검증은 이번 단계에 해당하지 않는다. 커밋·push 없음.

RW1.6을 마치려면 UnitHUD 컴파일 오류 해소 뒤 Unity에서 RailwayTrackLayoutTests·FieldOutlineRendererTests·FieldHighlightStateTests를 실행하고, Database/GridOutline 전투에서 레일이 항상 보이면서 대기 격자는 꺼져 있는지 확인한다. 레일 색은 Combat 씬 FieldView의 Rail Color를 사용한다.

**다음 RW2**는 FieldRailwayPresenter·RailwayTrainVisualBinding·임시 래퍼 프리팹·RailwayVisual 설정을 연결한다. 선로·바닥과 경로당 숨긴 열차를 설치하고 Mapper 간격·회전·폭 및 재설치·해제를 확인한다. 두 표시 모드에서 정적 표시와 DisplayReuse·DataBaseAssetTests를 검증한다. 실제 모델 없이 시작할 수 있지만 프리팹/에셋 변경이 포함되며, 발동 연출은 여전히 RW3 범위다. 진행 지시 전에는 착수하지 않는다.


### 2026-10-12 RW2 코드 완료와 전투 스테이지 경로 입력

사용자가 RW2 진행과 Railway 경로 사전 입력을 지시했다. 경로는 **모든 전투 스테이지, 9×9의 중앙 직선 1개**로 지정했다. 기존 필드에 다른 크기가 있다는 확인 질문에 **모든 필드를 9×9로 변경한 뒤 x=4, y=0~8 적용**을 선택했다. 따라서 이 단계의 스테이지 authoring 변경은 명시적인 사용자 지시 범위다. Core 계산·Buffer는 수정하지 않았다.

**구현 결과**

- FieldRailwayPresenter는 RW1 경로 결과와 Mapper의 중심·간격을 사용해 칸마다 바닥·선로를 설치한다. 경로당 숨긴 열차 하나를 준비하며, 가로 회전·Reverse·비정사각 간격·균등 열차 폭·ModelYaw를 적용한다. 설치 중 루트를 숨기고 성공 후 켜므로 부분 생성 표시를 방지한다. Collider는 소유 인스턴스에서만 비활성화한다.
- OutsideCells는 보드 경계에 닿은 끝에만 적용한다. 마지막 소수 조각은 중심과 길이를 줄이고 실제 RailCells/집기를 추가하지 않는다. 기존 기본값 OutsideCells=16·TravelPaddingCells=18·Duration=3·폭=0.8은 유지했다. 중앙 9칸 경로의 기본 설치는 바닥 41개·선로 41개·비활성 열차 1개다.
- RailwayTrainVisualBinding은 명시적 ModelRoot와 선택적 Animator 참조만 가진다. ModelRoot의 소속과 Animator 위치를 확인하며 Clip/Controller가 있는데 Animator가 없으면 설치 오류로 거절한다. 현재 임시 열차는 Animator·Clip·Controller 없이 사용할 수 있다. 아직 애니메이션이나 발동 이동을 시작하지 않는다.
- 새 View 소유 임시 래퍼는 `Prefabs/Stage/Railway/RailTile_Placeholder.prefab`, `Rail_Placeholder.prefab`, `Train_Placeholder.prefab`이다. 세 참조를 RailwayVisual_Default에 연결했다. 원본 Cube는 수정하지 않았다. 단위 면적·선로 +Z 길이 1·열차 폭 1·바닥 중심 피벗을 맞췄으며 실제 모델 연결은 RW4다.
- 바닥 기본 YOffset=0.002, 선로 YOffset=0.045를 추가하고 유한성을 검사한다. 임시 바닥은 면적 1×1의 낮은 받침과 0.8×0.8의 높은 중앙으로 구성했다. 받침 윗면 0.012는 기본 테두리 0.02보다 낮고 중앙 윗면 0.042는 기본 CellPrefab 윗면 0.025보다 높다. 선로는 그 위에 설치한다. +Z 임시 열차의 기본 ModelYaw는 0으로 맞췄다.
- CombatViewSystem.InitData가 기존 RailwayVisual Query.Config를 받아 SceneDisplay에 주입한다. Install에서 Field.Build 뒤 Railway를 설치하고 Refresh는 다시 생성하지 않는다. 재설치 전·부분 실패·Clear·Release에 Railway 정리를 연결하고 Mapper 해제 전에 제거한다. 표시 해제는 한 Presenter가 실패해도 나머지를 시도한다. 새 서비스·Factory 등록·System Tick은 없다.
- 전투 스테이지 **Boss와 Normal A~J, 총 11개**의 FieldDefinition 참조를 기존 Field_9x9로 통일하고 중앙 세로 9칸을 채웠다. 작업 시작 때 이미 RailwayEnabled=1, 첫 발동·반복 간격=5였으며 그대로 유지했다. 메인·본부·환경 배치도 유지했다. 공유 Field 에셋이나 StageSelect 정의는 수정하지 않았다.
- FieldRailwayPresenterTests 13건을 추가했다. 빈 입력·이동/Y 회전된 필드·높이·가로/Reverse·비정사각 간격·분리 경로·소수 외부 조각·숨긴 열차·재설치·부분 실패·Collider 원본 보존·누락 Animator·비유한 높이·두 필드 표시 모드를 다룬다. DataBaseAssetTests에는 11개 운영 정의의 9×9 중앙 레일과 기본 열차 바인딩 검사를 추가했다. 기존 SceneDisplay 생성자 호출 테스트도 갱신했다.

**검증 증거와 남은 확인**

| 검증 | 결과 |
|---|---|
| 현재 소스 MSBuild Systems·Framework 테스트 | 성공, 오류 0·RW2 변경 파일 새 경고 0. 신규 파일만 임시 Compile 항목으로 보완했고 다른 소스를 대체하지 않음. 기존 의존 프로젝트 산출물을 사용한 `/p:BuildProjectReferences=false` 검사 |
| RW1 경로·강조 상태 관리 회귀 | 새 테스트 DLL을 Unity Mono에서 실행, 29/29 통과. Unity Test Runner 결과와 구분 |
| 정적 에셋 검사 | 11개 9×9 필드 참조·중앙 9칸·기존 5턴 값 일치, 프리팹 3개 참조와 내부 fileID 유효, 신규 9개 GUID 중복 없음 |
| Unity EditMode 설치·재사용·DB 검사 | 대기. 열린 에디터가 새 파일을 다시 불러오지 않아 TestRunner 시작/결과 파일이 생성되지 않음. 신규 설치 13건과 DB·DisplayReuse·생성자 영향 회귀는 컴파일만 확인 |
| Unity 시각 확인 | 대기. Database Live 중앙 레일에서 GridOutline/CellPrefab 바닥·선로·테두리, 배치·행동 전환과 재진입 확인 필요 |

스크래치 증거는 `Temp/RailwayViewRW2/build-systems.log`, `build-tests.log`, `managed-layout-state.log`, `asset-audit.log`다. 임시 TestRunner 도구와 요청 플래그는 제거했다. `Assets/Editor.meta`는 유지했으며 뒤늦게 자동 테스트를 실행할 요청은 남기지 않았다. RW1 때 범위 밖 UnitHUD 오타는 현재 파일에서 해소돼 이번 컴파일에는 HEAD 대체가 필요하지 않았다. FMOD 캐시와 기존 스테이지 활성화·주기 변경은 작업 시작 때 있던 변경이며 되돌리지 않았다.

**원칙 점검과 리뷰 지점**

좌표는 전달받은 RailCells, 간격은 Mapper, 외형 보정은 RailwayVisual이 원본이다. 경로·표현·씬 조립 책임을 분리했고 검색은 설치 중 명시적 바인딩/Collider 확인에만 쓴다. FW 등록·Builder·서비스 내부 Store·대여 Scope를 우회하지 않았다. 생성한 루트와 프리팹은 View 소유라 직접 제거하며 기존 필드/Render 대여 자원은 건드리지 않는다. 수명은 InitData → 씬 Install → Clear/Release에 연결했다. Core·Buffer·FieldCamera·Architecture·Render 내부 코드·옛 Missing Script 에셋 변경 없음. 커밋·push 없음.

RW2.9의 Unity 실행 증거는 남아 있다. RW3에서는 **Railway 사건의 열차 이동 완료를 기다린 뒤 기존 피해·사망 사건을 표시**하도록 연결한다. 이번 단계는 정적 프리팹과 비활성 열차 설치까지만 구현했으며 기존 즉시 기믹 사건 표시를 유지한다. RW3는 아직 착수하지 않았다.

### 2026-10-12 과거 브랜치 리소스 확인과 RW4 입력 대기 정정

사용자가 RailwayVisualData에 필요한 리소스가 과거 브랜치에 있음을 지적했다. RW2에서 현재 브랜치의 Cube 대체 상태만 확인하고 과거 실제 리소스 조사를 빠뜨렸다. 로컬 `develop-4week`(`ae07af37e9e897e4462c102b67d6b05f7a0bb8db`)에서 아래 연결을 확인했다. 설정 이력은 `f7816ece`다. RW4는 새 모델 선택/확보 대기가 아니라 **이 기존 리소스의 복원·래퍼 연결**로 진행한다.

| 용도 | 과거 브랜치 원본 |
|---|---|
| 설정 원본 | Assets/DataBase/SO/Stage/RailwayVisualConfig.asset |
| 기차 | Assets/Prefabs/Tile/Train.prefab + Assets/Resources/Test/Tile/Train.fbx 및 각각 .meta |
| 선로 | Assets/Prefabs/Tile/TrainLoad.prefab + Assets/Resources/Test/Tile/TrainLoad.fbx 및 각각 .meta |
| 선로 바닥 | Assets/Prefabs/Tile/TrainTile.prefab + Assets/Resources/Test/Tile/TrainTile.fbx 및 각각 .meta |
| Controller | Assets/Resources/Test/Animation/Train/TrainSkill.controller 및 .meta |
| Clip | Assets/Resources/Test/Animation/Train/RoadRoller.anim 및 .meta |
| 이펙트 | Assets/Resources/Test/Effect/TrainEffect/TrainEffect.prefab·TrainSpark.mat 및 .meta, Assets/DataBase/SO/RenderGroup/EffectTable/Items/TrainEffect.asset |

과거 설정은 ClipEndZ=-250, Duration=3, TrainWidthInCells=0.8, OutsideCells=16, TravelPaddingCells=18, TrainModelYaw=180, Reverse=false다. Controller·Clip은 연결돼 있고 Effect는 당시 enum의 2다. RW2 기본 설정은 같은 이동 값에서 임시 프리팹·빈 Controller/Clip·Empty Effect·ModelYaw=0을 사용하므로 실제 리소스 연결 때 원본 기준으로 대조한다.

세 FBX는 Git LFS 포인터지만 실제 바이트가 모두 로컬 `.git/lfs/objects`에 존재한다(기차 516,028B, 선로 25,260B, 바닥 23,548B). 원본 프리팹은 해당 FBX의 Mesh와 내장 Material을 참조한다. 현재 Assets에는 세 프리팹·FBX·Controller·Clip GUID가 없으므로 설정 참조만 바꾸면 연결되지 않는다. 복원 시 원본 .meta를 함께 가져온다.

과거 Train 프리팹에는 Animator가 없고 옛 RailwayPresentationFactory가 별도 객체에 Animator·AnimationNotify를 붙였다. 새 설계에서는 View 소유 래퍼에 ModelRoot와 Animator를 명시해 RailwayTrainVisualBinding 계약에 맞춘다. RoadRoller는 루트 위치 곡선(path 빈 값), 180도 루트 회전, 길이 2초와 InvokeStart/InvokeEnd(71) 이벤트를 갖는다. RW3의 Manual PlayableGraph·완료 계약에 맞춰 사용하며 옛 Update/AnimationNotify/EffectScope.Dispose 코드를 그대로 이식하지 않는다.

이펙트는 현재 EffectType이 Empty·TestFire만 있어 숫자 2를 설정에 바로 넣을 수 없다. ParticleStopNotifier GUID는 현재 Render 구현에서도 유지돼 있고 TrainSpark의 Shader GUID는 현재 URP ParticlesUnlit 패키지에 존재한다. 과거 Effect 프리팹의 추가 Material/FBX GUID 두 개는 해당 브랜치 Assets 메타에서 찾지 못했으므로 복원 때 사용되는 자식과 의존성을 확인해야 한다. Render enum·테이블 등록은 다른 작업자 경계와 FW-012를 지켜 별도 인수한다. 실제 프리팹·Clip·Controller 복원과 구분한다.

이번 후속 확인에서는 브랜치를 변경하거나 옛 코드·리소스를 복원하지 않았다. RW2 런타임/에셋 상태와 RW3 미착수 상태는 유지하고 리소스 출처·RW4 Task를 정정했다. 커밋·push 없음.

### 2026-10-13 RW3 코드 반영 — 중앙 가로선과 열차 사건 재생

사용자 지시: **“RW3 진행하고, 기차는 세로가 아니라 가로여야함”**. RW3 구현과 기존 11개 전투 스테이지 입력의 방향 변경을 수행했다. 모든 필드는 기존 9×9 참조를 유지하고 중앙 레일을 **x=0~8, y=4**로 교체했다. 활성화·첫 발동/반복 간격 5·유닛/본부/환경 배치는 변경하지 않았다. 가로·세로·다중 직선 지원 계약은 유지한다.

**구현 결과**

- FieldRailwayPresenter의 Begin은 사건 AffectedRailCells의 중복과 설치 집합 일치를 검사한다. 순서 차이는 허용하며 최신 Buffer나 KilledUnitIds로 사건 내용을 대체하지 않는다. 경로당 기존 열차를 함께 활성화하고 Duration 안에 통과시킨다. 내부 끝에는 외부 Padding을 적용하지 않는다.
- Clip·Controller 없음은 보간, Clip 지정은 Clip 우선, Controller만 있으면 기본 상태 외형 재생이다. 수동 PlayableGraph를 기존 Tick에서 평가하고 Root Motion·AnimationEvent를 끈다. Animator 루트의 이동/회전/스케일은 매 평가 뒤 복원하고 자식 외형 애니메이션은 유지한다. 끝점은 정확히 맞추며 Clip의 역행 진행률은 이전 진행률보다 뒤로 가지 않는다.
- Clip 루트 Z 이동은 시작·중간·끝의 실제 샘플로 판별한다. `hasRootCurves`만 사용하지 않는다. 이동이 있을 때만 ClipEndZ 분모를 검증하며, 정적 Clip/Controller/애니메이션 없음에서는 이 값을 요구하지 않는다. 분모 실패 중 만들어진 그래프도 정리한다.
- CombatViewSystem은 PostBindService에서 기존 Effect 서비스를 받아 기존 BindServices 입구와 SceneDisplay 생성자로 주입한다. 실제 씬을 붙이는 테스트도 같은 필수 의존성을 제공한다. Empty는 Scope를 요청하지 않고, 지정 Effect는 사건 Scope를 통해 각 열차 앵커에 재생한다. 실제 TrainEffect 등록은 RW4다.
- SceneDisplay의 단일 Railway 라우팅에서 사건 완료를 기다린다. 일반 사건 Presenter·OrderedCombatPresentation·CombatResultPlayback의 순서/확인 계약은 수정하지 않았다. 정상 정리에서 Scope 반환·그래프 제거·열차 숨김·현재 콜백 비움을 마친 뒤 완료를 한 번 통지한다. 다음 HealthChanged/Died는 그 이후에 진행한다.
- Cancel은 세대와 콜백부터 무효화하고 완료를 보내지 않는다. 시작/반환 중 재진입 Cancel·Clear·Release, 완료 콜백에서 다음 Begin, 반복 발동·취소, 부분 시작 실패를 방어한다. 시작 중 재설치는 거절한다. Scope 반환 실패에서도 다른 열차/그래프 정리를 계속하며 실패와 성공을 함께 보고하지 않는다.
- Scope.StopAll의 부분 반환 예외는 공개 계약만으로 안전하게 재시도할 수 없다. 대여 자원을 직접 Destroy하지 않도록 실패 후 숨긴 설치 앵커를 보존하고 오류로 중단한다. 정상 해제에서는 모든 View 소유 인스턴스를 제거한다. [Render 계약 확인 요청](../handoffs/2026-10-13-railway-effect-scope-return-note.md)을 작성했으며 **미전달**이다. Render 내부는 수정하지 않았다.

**검증 환경과 증거**

원본 Unity 에디터는 닫거나 씬을 변경하지 않았다. 원본의 신규 소스 다시 불러오기/Test Runner 실행이 대기 상태였으므로 `Temp/RailwayViewRW3/AnimationProbe`에 현재 컴파일 DLL과 필요한 에셋의 복사본을 만들어 **Unity 6000.0.73f1 네이티브 Editor**에서 검사했다. 복제본에서만 소스 GUID를 DLL MonoScript 참조로 바꿨다. NUnit Test/TestCase와 SetUp/TearDown을 반사 호출한 결과이며 **Unity Test Runner 결과와 구분**한다. 그래픽 장치 없는 batch 환경이므로 화면 검증은 아니다.

| 검증 | 결과 |
|---|---|
| 현재 소스 MSBuild Systems·Framework 테스트 | 성공, 오류 0·변경 파일 새 경고 0. 신규 소스만 임시 Compile 항목으로 보완, 기존 참조 산출물을 사용한 `/p:BuildProjectReferences=false` 검사 |
| RailwayTrackLayout·FieldHighlightState·FieldOutlineRenderer | 독립 Unity에서 21+8+12 = **41/41 통과** |
| FieldRailwayPresenter | 독립 Unity에서 **33/33 통과**. 설치·두 모드·방향·반복·취소·부분 시작/반환 실패·재진입·Clip·Controller 단독·Clip 우선·0/NaN/무한 이동 분모 포함 |
| 실제 SceneDisplay Railway 순서 | **1/1 통과**. 열차 중에는 대상 유닛 유지·결과 미완료, 완료 뒤 HealthChanged/Died와 결과 완료 1회 |
| 11개 운영 스테이지 DB 교차 검사 | **9×9 중앙 가로 9칸과 기본 열차 바인딩 검사 통과**. DataBaseAssetTests 전체 21/22 통과, LineMaterial Shader 검사 실패는 아래 구분 |
| Playback·Cleanup·Movement·ViewSystem·Progress·Plan | **14+6+22+64+15+17 = 138/138 통과**. 기존 결과 확인·정상 종료/포기 대기·오류 취소 경로 검사 |
| 실제 씬 이동/복귀·기타 표시 회귀 | 실제 씬 DragPlaybackAndReturn 통과. 전체 독립 검사는 **288 통과·17 실패·0 skip / 305건**이며 전체 통과로 보고하지 않음 |
| 과거 RoadRoller Clip 추출 | 별도 네이티브 Manual Graph 확인. 길이 2, hasRootCurves/hasMotionCurves=false지만 Root Motion=false에서 루트 Z가 0 → -55.18(0.5초) → -168.77(1초) → -246.84(2초)로 평가됨. 현재 Presenter 방식으로 추출 가능 |
| 정적 에셋·형식 | 11개 중앙 가로선, 유지한 5턴 값, 프리팹 3개 참조·내부 fileID·신규 GUID 9개 유일성 검사 통과. diff 공백 오류 없음 |
| 원본 Unity Test Runner·운영/시각 확인 | 대기. 실제 모델/등록 TrainEffect 연결, Database Live 발동과 실제 승패/포기·씬 재진입은 RW4 |

독립 검사의 나머지 17건 실패는 LineMaterial Shader 1건, 손패 RectTransform fixture 2건, 사건 표시 할당량 비교 1건, Unity 가시성 콜백 재진입 2건, UI/EventSystem 포인터 fixture 9건, Terrain 운영 fixture의 벽 공개 조건 2건이다. 원본 Test Runner로 재확인해야 하며 이번 Railway 기능 통과 증거와 분리한다. 그래픽/패키지·EditMode 콜백 제약이 있는 복제 환경의 결과만으로 운영 회귀라고 확정하지 않았다. 이를 통과시키려고 다른 UI/Core/Render 작업을 수정하지 않았다. 이번 검증에서 발견된 RW2 DB 테스트의 ReadOnlyMemory 배열 비교는 ToArray로 고쳤고, RW3 Effect 의존성 누락은 기존 BindServices 테스트 조립에서 고쳤다.

스크래치 증거: `Temp/RailwayViewRW3/build-systems.log`, `build-tests.log`, `asset-audit.log`, `native-checks.log`, `AnimationProbe/native-results.txt`, `AnimationProbe/probe.txt`. 검증 코드·복제 리소스는 Temp 안에만 있고 원본 Assets/Editor에 실행 요청은 남기지 않았다. 과거 리소스는 Clip의 검증 복사본만 사용했으며 현재 운영 에셋으로 복원하거나 연결하지 않았다.

**원칙 점검과 리뷰 지점**

SSOT는 설치 RailCells/현재 사건 스냅샷·Mapper·RailwayVisual·기존 결과 Playback으로 유지한다. SOLID는 경로 계산/경로별 외형 재생/설치·사건 수명/씬 라우팅/전체 결과 소비를 분리해 점검했다. 새 System·Update·서비스 키·Factory 등록·일반 자원 관리자·인터페이스가 없다. FW-001~004의 기존 서비스 주입, FW-011의 Scope 반환·대여 자원 직접 제거 금지, FW-012의 Empty/등록 전제, FW-016의 설치 → 기존 Tick → 취소/역순 해제를 지킨다. 실패한 Scope의 정리 한계는 별도 요청에 공개했다.

Core·Buffer·FieldCamera·Architecture·Render 내부·셰이더·옛 Missing Script 에셋 변경 없음. FMOD 캐시는 작업 시작 때의 다른 변경을 유지했다. RW3 코드 리뷰 지점에서 멈춘다. **RW4는 과거 실제 기차/선로/바닥·Clip·Controller 복원과 운영 연결이며 아직 착수하지 않았다.** 커밋·push 없음.

### 2026-10-13 RW4 실제 리소스 연결과 운영 DB 검사

사용자가 RW4 진행과 과거 실제 기차·레일 프리팹·애니메이션 복원·연결을 지시했다. `develop-4week`의 고정 리비전 **ae07af37e9e897e4462c102b67d6b05f7a0bb8db**에서 아래 8개 리소스와 각각의 .meta를 복원했다. 세 FBX의 실제 바이트는 로컬 Git LFS 객체에서 SHA를 확인해 가져왔다. 원본 파일·meta는 바이트 단위로 일치하고 GUID는 유지했다. 브랜치 변경·커밋·push는 없다.

| 원본 리소스 | 현재 위치 |
|---|---|
| Assets/Prefabs/Tile의 Train·TrainLoad·TrainTile.prefab | Assets/ProjectL/Prefabs/Stage/Railway/Source/ |
| Assets/Resources/Test/Tile의 같은 이름 FBX 3개 | 같은 Source/Models/ |
| Assets/Resources/Test/Animation/Train의 TrainSkill.controller·RoadRoller.anim | 같은 Source/Animation/ |

옛 RailwayVisualConfig는 Missing Script 입력 형식이므로 검증용 Temp 복사본으로 값을 대조하고 현재 RailwayVisual_Default에 연결했다. 새로 복원할 SO로 추가하지 않았고 기존 Missing Script 에셋도 삭제하지 않았다. 원본 TrainEffect는 현 EffectType에 없는 2를 사용하고 Material `fdd3f7739e6420144bffcb85ff863ff3`·Mesh `097f9dec74e108a46be9e2fb54db7d3a` 참조를 찾지 못했다. 선택 Effect는 설계가 허용하는 **Empty**로 유지하고 [Render 인수 메모](../handoffs/2026-10-13-railway-effect-scope-return-note.md)에 의존성·등록 요청을 추가했다. 외부 발송·Render 내부 변경은 없다.

**실제 외형 연결**

- View 래퍼 `Train.prefab`, `Rail.prefab`, `RailTile.prefab`을 기본 설정에 연결했다. 각각 원본 프리팹을 중첩 참조하며 source의 모델·Animator 이벤트·원본 머티리얼을 수정하지 않는다. 임시 Placeholder 세 개는 검증 fixture로 남겼고 운영 기본값은 실제 래퍼를 사용한다.
- 원본 Train의 긴 축은 로컬 Z다. 원본 프리팹의 90도와 옛 Factory의 축 보정·설정 Yaw를 대조해 래퍼를 +Z 기준으로 정규화했다. 새 기본 ModelYaw는 0이다. Animator는 명시적 ModelRoot에 붙여 Root Motion·자동 재생을 끄며 기존 Presenter가 AnimationEvent를 끈다. Clip 우선 정책으로 RoadRoller의 루트 Z를 진행률로 사용한다.
- Train 단위 폭 1·길이 약 30.897·하단 높이 0.2, Rail 폭 0.9·길이 1·높이 0.1, RailTile 면적 0.9×0.9·높이 약 0.263355로 래퍼에서 보정했다. 런타임에서 모델 Bounds를 찾아 보정하지 않는다. 바닥 YOffset=-0.2233546으로 윗면 y=0.04, 선로 YOffset=0.045를 유지해 기본 Cell 윗면 0.025 위에 둔다. GridOutline 기본 선 두께의 안쪽 여백을 확보했다.
- 원본 Standard 머티리얼의 색·금속성·광택을 View 소유 Materials/의 URP Lit 4개에 옮겨 래퍼에서만 대입했다. 원본은 텍스처 없는 흰색이다. 기존 Shader·Render 에셋을 수정하지 않았다.
- 기존 Duration=3·폭=0.8·OutsideCells=16·TravelPaddingCells=18·ClipEndZ=-250·Reverse=false를 유지했다. 실제 열차 길이는 기본 폭에서 약 24.717칸이며 시작·끝 모델 Bounds가 모두 보드 밖인 검사를 통과했다. 카메라 화면의 최종 외형 확인은 남아 있다.

**본부 위치의 승인 변경**

운영 DB EnterBattle 검사에서 9×9 적 배치 구역 y=7~8과 Normal A의 기존 본부 y=5가 맞지 않아 진입이 실패했다. RW2의 기존 배치 유지 계약과 달라 구현 전에 질문했으며 사용자가 **“적 본부를 같은 x의 y=7로 이동”**을 승인했다. 모든 11개 스테이지의 기존 x를 유지하고 y=7로 통일했다. 이전 y는 Boss 8, A 5, B 7, C 6, D 8, E 6, F 8, G 5, H 7, I 6, J 7이다. 메인·본부 회전·환경 배치·기존 Railway 주기는 바꾸지 않았다. 실제 SO authoring 변경이며 Core 계산 수정이 아니다.

**검증 결과와 한계**

RW3와 같은 독립 Unity 6000.0.73f1 프로젝트에서 현재 DLL·리소스 복사본을 사용했다. 원본 Unity 에디터의 씬·설정을 바꾸거나 닫지 않았다. 에셋의 MonoScript 참조 변경은 검증 복사본에만 적용했다. Test/TestCase 반사 호출 결과이며 원본 Unity Test Runner 실행과 구분한다. 검증 프로젝트에는 현재 URP/Core/ShaderGraph의 Shader/HLSL 복사본을 넣어 머티리얼 참조를 확인했으며 원본 패키지는 수정하지 않았다.

| 검증 | 결과 |
|---|---|
| Framework 테스트 MSBuild | 오류 0, RW4 변경 파일 새 경고 0. 기존 참조 산출물·AdditionalSources 사용 |
| DataBaseAssetTests | **23/23 통과**. 11개 9×9 가로 레일·본부 y=7, 실제 원본 Mesh·URP Lit·Clip/Controller·바인딩 포함. RW3의 LineMaterial Shader 검사도 통과 |
| FieldRailwayPresenterTests | **36/36 통과**. 기존 33건에 실제 리소스 이동·반복/해제와 두 표시 모드의 바닥 높이·테두리 여백 검사를 추가 |
| 실제 운영 DB Live | Normal A의 기본 DB 진입·메인 배치·턴 진행·첫 발동·반복 발동 통과. 열차 활성 중 완료 0, 종료 후 완료 1, Clear 후 제거·재설치 1개 유지 |
| 실제 씬 Railway 사건 순서 | 열차 통과까지 유닛 유지·미완료, 이후 HealthChanged/Died·완료 1회 통과 |
| RW1 경로/강조·기존 Playback/Cleanup/이동/ViewSystem/Progress/Plan | 이전 RW3 관련 통과 유지 |
| 독립 전체 검사 | **294 통과·16 실패·0 skip / 310건**. 전체 통과로 보고하지 않음 |
| 정적 에셋 검사 | 원본 8개·meta 바이트 일치, Railway meta 23개 GUID 유일, 실제 래퍼 root fileID·Clip/Controller 참조 유효, 11개 가로선·본부 기존 x/y=7 검사 통과 |
| 원본 Unity Test Runner·Combat 시각 확인 | 대기. 실제 표시 모드·입력 강조·사망/승패/포기·씬 해제/재진입 화면 확인 필요 |

독립 검사에서 남은 실패 16건은 손패 RectTransform fixture 2건, 표시 할당량 비교 1건, Unity 가시성 콜백 재진입 2건, UI/EventSystem 포인터 fixture 9건, Terrain 운영 fixture 벽 공개 조건 2건이다. RW3와 같은 사례이며 원본 Test Runner에서 재확인해야 한다. 이를 통과시키기 위해 다른 작업자 코드나 규칙을 수정하지 않았다.

추가된 운영 검사에서 뒤의 비동기 사건이 앞 사건 완료 Tick 안에서 시작되므로 사건별 추가 Tick으로 재생을 끝까지 소비하도록 fixture를 정정했다. 기존 OrderedCombatPresentation의 계약·구현은 바꾸지 않았다. 별도 모델 형상 프리뷰는 실제 Presenter로 생성하되 복제 인스턴스만 Built-in 셰이딩을 사용한 참고 이미지다. 원본 Combat의 URP 화면 검증으로 사용하지 않는다.

증거: `Temp/RailwayViewRW4/source-manifest.json`, `headquarters-changes.json`, `asset-audit.log`, `build-tests.log`, `native-checks.log`, `native-results.txt`. 원본 복원·래퍼 authoring 및 검증 도구는 Temp에만 두었고 Assets/Editor에 자동 실행 요청을 남기지 않았다. 설정 안내는 [railway-view-guide](../viewDocs/railway-view-guide.md)다.

**원칙 점검과 리뷰 지점**

규칙·사건·좌표의 SSOT는 Core/Buffer, 외형의 SSOT는 RailwayVisual과 View 래퍼다. 본부 입력 변경은 사용자 승인 SO 값만 수정했다. 리소스 복원·단위 보정·설치·결과 재생 책임을 유지하고 새 서비스·Factory·Pool·매 프레임 검색을 추가하지 않았다. FW-001~004 기존 조립, FW-011 Scope 소유/반환, FW-012 Empty·등록 전제, FW-016 기존 Tick·취소·역순 해제를 점검했다. Core·Buffer·FieldCamera·Architecture·Render 내부·셰이더 수정 없음. 작업 시작 때의 FMOD 캐시 등 다른 변경은 유지했다.

**RW4 실제 리소스 연결을 마친 리뷰 지점에서 멈춘다.** RW4.2~RW4.5의 원본 Unity Test Runner·전체 화면 확인은 남아 있고 최종 표현 완료로 표시하지 않는다. 선택 TrainEffect를 사용하려면 Render의 정상 등록·누락 의존성 인수가 필요하다. 커밋·push 없음.
