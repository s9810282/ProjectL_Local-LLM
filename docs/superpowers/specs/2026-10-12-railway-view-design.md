# 전투 레일 표시와 열차 발동 연출 설계

작성일: 2026-10-12. 대상: Combat View. 상태: 사용자 설계 확인 완료, RW1~RW3 코드·RW4 실제 리소스 연결 반영. 독립 Unity 운영 경로 검사 통과, 원본 Test Runner·Combat 화면 확인 대기. Task·실행 기록은 [Railway View 계획](../plans/2026-10-12-railway-view-plan.md)에만 적는다.

CombatStageData에서 켠 Railway를 GridOutline에서도 항상 식별할 수 있게 하고, RailwayVisual의 선로 바닥·선로·열차 프리팹과 발동 연출을 연결한다. 사용자가 선택한 경로 범위는 **가로·세로 직선 여러 개**다. 승인된 RW1~RW4 진행으로 상시 표시·경로 계산·정적 프리팹·열차 사건 재생과 실제 과거 모델·Clip·Controller 연결을 반영했다. 전투 스테이지 11개는 9×9, **가로 기차 경로(x=0~8, y=4)**다. RW4 추가 승인으로 적 본부는 기존 x·y=7에 둔다. 설정 방법은 [View 안내](../viewDocs/railway-view-guide.md)를 따른다.

## 1 현재 연결과 변경 범위

현재 데이터 전달은 `CombatStageData.railwayEnabled` → `CombatStageDetail.Railway` → `BattleDefinitionAdapter` → `StageConfig` → 준비 결과 → `CombatStateData.RailCells` → `CombatSceneDisplay.Install` → `FieldView.ApplyStaticHighlights`로 연결돼 있다. View가 InGame DB를 추가로 읽을 필요는 없다.

`FieldHighlightState`는 이미 Rail 구역을 저장한다. 설계 시작 시 CellPrefab은 `railColor`로 칠했지만 GridOutline은 Rail 슬롯이 없어 그리지 않았다. 이는 [격자 아웃라인 설계](2026-10-09-field-grid-outline-mode-design.md)의 U3·§5 정책이었다. 이번 승인 설계는 **레일을 숨기는 부분만** 변경하고, 평소 격자 데칼·호버·적 배치 구역을 숨기는 정책은 유지한다. Rail 슬롯은 RW1에서 코드 반영했다.

설계 시작 때는 `RailwayVisualTableQuery.Config`의 Combat View 소비자가 없고 세 프리팹이 모두 Cube였다. 현재 CombatViewSystem이 설정을 주입하며 `RailwayVisual_Default.asset`은 과거 실제 모델을 포함한 View 래퍼 세 개와 TrainSkill·RoadRoller를 참조한다. 선택 Effect는 Empty다.

Core는 발동 결과에 `CombatGimmickEvent(RailwayGimmickActivationSnapshot)`을 먼저 넣고 뒤에 체력·사망 사건을 넣는다. SceneDisplay가 Railway 사건을 전용 Presenter로 라우팅해 열차 통과 완료까지 기다리고, 다른 기믹은 기존 사건 Presenter가 처리한다.

범위는 레일 상시 표시, 세 프리팹 연결, 직선 경로 도출, 열차 재생과 선택적 애니메이션·Render 이펙트, 취소·해제, 필요한 View 테스트와 에셋 입력 안내다. Core 계산·발동 주기·피해·승패, FieldCamera, Architecture, Render 내부·셰이더, 오디오·카메라 효과는 수정하지 않는다. 턴 경고 UI·접촉 순간별 사망·곡선·분기·교차 경로는 후속 설계다.

## 2 화면 동작

| 시점 | 표시 |
|---|---|
| 전투 설치 | RailCells의 각 칸에 선로 바닥과 선로를 설치한다. GridOutline은 같은 칸의 레일 테두리도 표시한다 |
| 대기와 배치 | 선로와 레일 테두리를 계속 유지한다. 열차는 숨긴다 |
| 이동과 공격 미리보기 | 선로는 유지하고 호버·범위·경로 테두리가 그 위에 보인다 |
| Railway 발동 | 사건의 AffectedRailCells에 대응하는 직선마다 열차 하나를 동시에 재생한다 |
| 열차 통과 완료 | 이펙트를 정리하고 열차를 숨긴 뒤 사건 완료를 한 번 통지한다 |
| 다음 사건 | 기존 사건 순서대로 체력·사망·영혼·승패 표시를 진행한다 |
| 오류 정지 또는 씬 해제 | 현재 연출을 취소하고 모든 대여 자원과 씬 소유 인스턴스를 정리한다 |

첫 버전은 **열차 통과 완료 후 피해·사망 표시**다. `KilledUnitIds`로 View가 유닛을 먼저 죽이거나 지우지 않는다. 접촉 순간별 피해 표시를 원하면 뒤따르는 캐릭터 사건과 접촉 시점을 묶는 별도 재생 설계가 필요하다.

Railway를 켜도 RailCells가 없으면 정상 설정이 아니다. 현재 Core 준비 검증을 따른다. Railway가 꺼진 전투는 레일·열차를 만들지 않는다. RecordInputOnly의 Database 준비 결과에는 레일을 표시할 수 있지만 실제 Core 발동 사건은 발생하지 않는다. Fixture는 현재 레일과 기믹을 빈 배열로 준비하므로 SO Railway 확인에는 Database를 사용한다.

## 3 사실의 원본

| 사실 | 원본 | View에서 보관할 수 있는 것 |
|---|---|---|
| 레일 위치 | 준비 결과를 담은 CombatStateData.RailCells | 설치용 불변 사본과 그 좌표에서 도출한 직선 목록 |
| 발동 여부와 영향 칸 | 결과 사건의 RailwayGimmickActivationSnapshot | 현재 사건 재생에 필요한 사본 |
| 피해·사망·승패 | Core의 뒤따르는 결과 사건 | 기존 사건 표시용 facts |
| 프리팹·재생 시간·방향·외형 보정 | RailwayVisualData | 읽기 전용 설정 참조 |
| 레일 강조 색 | FieldView.railColor | 렌더러 Palette |
| 칸 중심·칸 간격 | 현재 표시 모드가 만든 FieldGridMapper | 도출한 선로 배치와 이동 시작·끝 위치 |
| 결과 재생 중 여부와 결과 확인 | CombatResultPlayback와 기존 결과 확인 경계 | 열차 Presenter는 현재 연출의 경과 시간만 소유 |

ViewGroup에 레일 좌표·발동 주기·다음 발동 턴·피해 대상을 복제하지 않는다. 프리팹 콜라이더나 Trigger로 피해를 계산하지 않는다. 경로 끝점과 길이는 좌표에서 계산하며 SO 입력 필드로 추가하지 않는다.

## 4 GridOutline 레일 표시

`FieldOutlineRenderer`에 정적 Rail 슬롯 하나를 추가한다. 상태는 기존 `ZoneAt(index) == FieldStaticZone.Rail`을 사용한다. 새 Rail 비트나 별도 상태 배열은 만들지 않는다.

이 슬롯은 `IsActing`과 `IsPlacementActive`에 관계없이 그린다. 현재 행위자도 지금처럼 평소에 표시하고, 다른 층·호버·격자 데칼은 기존 행동 중 조건을 유지한다. 슬롯 순서는 **레일 → 아군 배치 구역 → 기존 DrawOrder 층 → 호버**로 둔다. 레일 색은 기존 Palette.Rail, 두께·높이는 StageFieldView 설정을 쓴다.

AreaOutline은 레일 칸 집합의 영역 테두리다. 서로 붙은 칸의 내부 경계까지 선로 모양을 그리는 기능은 프리팹이 맡는다. 레일 테두리가 없어도 선로 프리팹 자체는 표시된다. 프리팹의 표시 높이는 강조 테두리와 겹치지 않게 설정한다.

핸들은 Build에서 한 번 발급받고 전투 동안 재사용한다. 같은 칸·색이면 Update하지 않고 Release에서 반환한다. 격자선은 대기 중에 켜지지 않는다. CellPrefab은 기존 Rail 색 동작을 유지하고 같은 선로 프리팹을 추가한다. 회전한 필드에서 AreaOutline을 숨기는 현재 제약은 유지하며, 프리팹 위치·열차 이동은 Mapper를 통해 필드 회전을 따른다.

## 5 직선 경로와 프리팹 배치

### 5.1 경로 도출

새 순수 계산 클래스 `Display/RailwayTrackLayout`이 RailCells와 보드 크기를 받아 경로를 도출한다. 열거 순서에 의존하지 않도록 좌표를 정렬한다. 경로는 상하좌우로 이어진 칸 묶음이며, 각 묶음은 X가 같거나 Y가 같은 연속 직선이어야 한다. 끊긴 구간은 별도 경로다.

- X가 같은 직선은 Core +Y, 즉 필드 로컬 +Z로 이동한다.
- Y가 같은 직선은 Core +X, 즉 필드 로컬 +X로 이동한다.
- 칸 하나뿐이면 +Z 직선으로 취급한다.
- Reverse는 각 직선의 시작과 끝을 바꾸고, TrainModelYaw는 이동 방향과 별개로 모델의 앞뒤만 보정한다.
- 서로 떨어진 가로·세로 직선 여러 개를 지원한다. 같은 길이·재생 시간으로 가정하지 않는다. 각 경로는 전체 Duration 안에서 통과하므로 길이에 따라 표시 속도가 달라진다.

예: `(1,0),(1,1),(1,2)`와 `(4,4),(5,4),(6,4)`는 세로·가로 경로 두 개다. 배열 순서가 바뀌어도 같은 결과가 나온다. `(1,0),(1,1),(2,1)`은 꺾인 경로여서 이번 경로 계약에는 맞지 않는다.

이웃한 병렬 선로·교차점은 좌표만으로 독립 선로를 식별할 수 없으므로 이번 자동 도출 범위 밖이다. 이런 입력을 임의로 꺾거나 여러 열차로 분해하지 않는다. **View 경로 검증에서 문제 좌표를 담은 InvalidOperationException으로 진입을 중단**한다. 이는 Core 규칙 제한을 새로 만드는 것이 아니라 이번 View가 표시할 수 있는 입력 계약이다. 더 넓은 배치가 필요하면 구현 전에 경로 식별 설계를 확장한다.

### 5.2 선로 바닥과 선로

`Display/FieldRailwayPresenter`가 설치 때 레일 칸마다 RailTilePrefab과 RailPrefab을 생성한다. 바닥은 칸 중심, 선로는 그 위에 놓으며 가로 선로는 Y축으로 90도 돌린다. Tile과 Rail은 별도 자식 루트여서 서로의 높이·스케일을 보정할 수 있다.

프리팹 작성 계약은 바닥 중심 피벗과 로컬 +Z 선로 방향이다. RailTile과 Rail의 기본 바닥 면적은 1×1, 선로 한 조각의 길이는 1이다. 모델 원본 단위·피벗이 다르면 View 소유 래퍼 프리팹 안에서 맞춘다. 런타임에서 모델 이름·Tag를 찾아 자동 보정하지 않는다.

실제 X·Z 크기는 Mapper의 칸 중심 간격으로 계산한다. CellPrefab의 모델 크기나 임의 상수에서 간격을 다시 읽지 않는다. 높이와 모델 보정은 RailwayVisual 설정을 사용한다. 세 프리팹의 인스턴스는 표시 전용이며 입력·충돌·게임 규칙 스크립트를 포함하지 않는다. Cube 임시 인스턴스의 Collider는 표시 인스턴스에서 비활성화한다.

OutsideCells는 **보드 경계에 닿은 경로 끝**의 바깥 선로 연장 길이다. 추가 타일은 보드 밖의 장식이며 RailCells·공격 범위·집기에는 추가되지 않는다. 보드 안에서 끝난 부분 직선은 비레일 칸으로 선로를 연장하지 않고 해당 끝에서 열차를 생성·숨긴다. TravelPaddingCells도 보드 밖 진입·퇴장에만 적용한다. 필드 루트의 다른 프랍이나 카메라 구성을 변경하지 않는다.

## 6 열차 이동과 애니메이션

### 6.1 이동 소유권

경로마다 비활성 열차 인스턴스 하나를 설치 시 준비해 반복 발동에 재사용한다. `FieldRailwayPresenter`가 Begin에서 위치·재생 상태를 초기화하고 기존 View Tick의 deltaTime으로 경과 시간을 올린다. 별도 Update MonoBehaviour, Coroutine, Task, 타이머, 트윈을 만들지 않는다.

열차 구조는 `이동 루트 → 모델 보정 루트 → TrainPrefab`이다. 이동 루트의 위치·방향은 Presenter만 쓴다. 모델은 기본 폭 1로 작성하고 TrainWidthInCells와 이동 방향에 수직인 칸 간격으로 균등 확대한다. 모델 고유 길이·높이는 같은 비율로 유지한다. TrainModelYaw는 모델 보정 루트에 적용한다.

각 경로의 앞·뒤 끝은 칸 중심에서 반 칸을 확장한 선분이다. 경계에 닿은 끝에서는 OutsideCells와 TravelPaddingCells를 더해 보드 밖 시작·끝을 만든다. Duration은 이 전체 이동 시간이다. 이동은 보간 가능한 표시일 뿐이며 유닛·구조물의 실제 상태를 바꾸지 않는다.

### 6.2 선택적 애니메이션

| 설정 | 재생 방식 |
|---|---|
| Clip·Controller 모두 없음 | 직선 보간만으로 정상 재생 |
| Clip 지정 | 수동 평가하는 Clip Playable을 사용한다. Controller보다 Clip을 우선한다 |
| Clip 없이 Controller 지정 | 수동 평가하는 AnimatorControllerPlayable로 외형을 재생하고 이동은 직선 보간한다 |

Clip의 로컬 Z 이동 곡선은 기존 ClipEndZ 의미를 살려 이동 진행률로 변환한다. 시작 Z를 샘플하고 `(현재 Z - 시작 Z) / (ClipEndZ - 시작 Z)`를 0~1로 제한한다. 끝점은 반드시 1로 맞춘다. Clip 재생 시간은 Duration에 맞춰 정규화한다. Z 이동 곡선이 없는 Clip은 외형만 재생하고 위치는 직선 보간한다.

애니메이션이 쓴 프리팹 루트 이동·회전은 이동 루트에 중복 적용하지 않는다. 루트 이동을 추출한 뒤 외형 루트의 배치 보정을 되돌리고, 자식의 애니메이션만 유지한다. Animator Root Motion은 끈다. Controller의 기본 상태로 재생하며 문자열 상태명·Trigger·AnimationEvent로 완료를 판단하지 않는다. 열차 래퍼의 `RailwayTrainVisualBinding` 컴포넌트가 modelRoot와 선택적 Animator를 명시적으로 제공한다. 정적 임시 프리팹은 Animator 없이 재생하고, Clip·Controller를 설정했는데 Animator가 없으면 설치 오류로 진단한다.

Controller 재생에서도 자동 Animator Update와 수동 Evaluate가 함께 움직이지 않게 그래프의 업데이트 모드는 Manual로 둔다. 재생 종료·취소 때 그래프를 먼저 정리하고 인스턴스를 숨긴다. 실제 Clip의 루트 곡선과 Controller 기본 상태는 에셋 연결 단계의 확인 항목이다.

### 6.3 선택적 Render 이펙트

Effect.Empty는 아무것도 요청하지 않는다. 지정된 Effect는 기존 `IEffectSystemServices.GetEffectHandle()`로 사건 소유 EffectScope 하나를 받아 각 열차의 이펙트 앵커에 재생한다. 열차 통과 완료·취소 시 `StopAll()`을 호출한 뒤 앵커와 열차를 숨긴다. 대여 이펙트 GameObject를 Destroy하거나 외부 풀에 반환하지 않는다.

현재 EffectScope에는 일반 IDisposable 공개 계약이 없으므로 `Dispose()`가 있다고 가정하지 않는다. 자연 종료 콜백과 StopAll의 반환 계약을 그대로 쓰고, Scope 참조는 사건 정리 후 놓는다. Effect가 등록돼 있지 않거나 프리팹 요구조건을 만족하지 못하면 기존 오류 경계로 실패를 전파한다. Render 내부 변경이 필요하면 별도 공유 요청으로 분리한다.

## 7 사건 라우팅과 재생 수명

`CombatSceneDisplay`가 기존 `ICombatResultPresenter` 입구에서 Railway 사건만 FieldRailwayPresenter로 넘긴다. 그 외 사건은 CombatSceneEventPresenter로 넘긴다. Railway 판정은 이 라우팅 한 곳에만 두고 일반 사건 Presenter에 열차 필드·DB·이동 계산을 넣지 않는다. 한 가지 기믹을 위해 범용 기믹 레지스트리나 새 서비스 시스템을 만들지 않는다.

```text
OrderedCombatPresentation
  BeginEvent Railway
    CombatSceneDisplay
      FieldRailwayPresenter.Begin
        영향 칸 확인 → 열차 활성화 → 애니메이션과 이펙트 시작
  기존 TickPresentation
    열차 이동 → 전체 경로 완료 → 이펙트와 그래프 정리 → 열차 숨김
    완료 콜백 1회
  다음 체력·사망 사건
  결과 전체 완료
  CombatViewSystem의 기존 최종 표시와 결과 확인
```

Begin에는 최신 Buffer 대신 `RailwayGimmickActivationSnapshot.AffectedRailCells`를 넘긴다. 이 버전은 설치한 RailCells와 사건 영향 칸 집합이 같아야 한다. 집합이 다르면 최신 상태로 사건을 덮어 그리지 않고 지원하지 않는 변경으로 실패한다. 영향 칸 순서 차이는 정상이다. 동적 선로 변경은 이번 범위 밖이다.

OrderedCombatPresentation과 CombatResultPlayback의 사건 순서·결과 번호·취소 세대·후처리 계약은 유지한다. Presenter에는 현재 사건 콜백·경과 시간·그래프·Scope만 둔다. 전체 전투의 IsPlaying이나 별도 마지막 결과 번호를 저장하지 않는다. 완료 콜백은 메인 스레드 Tick에서 내부 상태와 자원 정리를 끝낸 뒤 정확히 한 번 호출한다.

Begin 또는 Tick 실패는 동기 예외로 기존 RuntimeFaultBoundary에 전달한다. 성공 콜백을 함께 호출하지 않는다. 부분 시작 실패에서도 먼저 시작한 열차·그래프·이펙트 정리를 모두 시도한다. 정리 실패는 원래 오류와 함께 모아서 보고한다.

Cancel은 콜백을 먼저 비우고 세대를 무효화한 뒤 이펙트·그래프·열차를 정리한다. 반복 Cancel은 안전하고 완료 통지를 보내지 않는다. Clear·Release는 Cancel을 먼저 수행하고 선로·열차 인스턴스를 제거한다. Cancel만 할 때는 재사용 가능한 비활성 열차와 정적 선로를 유지한다. 재설치는 이전 구성을 먼저 정리하며 설치 중 실패에도 생성된 모든 인스턴스를 치운다.

CombatSceneDisplay의 TickPresentation은 일반 사건 Presenter와 Railway Presenter를 모두 갱신한다. CancelPresentation·설치 실패 정리·Clear·Release는 기존 RunAll 방식으로 다른 정리도 끝까지 시도한다. 정리 순서는 Railway → Field Mapper 해제다. 정상 종료·포기는 열차를 강제 Cancel하지 않고 기존 마지막 결과 재생과 확인을 기다린다. FocusLost는 기존 입력 취소 경로만 사용하며 결과 연출을 새로 취소하지 않는다.

## 8 조립과 변경할 파일

새 서비스 키·Factory 등록·Framework 모듈을 추가하지 않는다. CombatViewSystem은 InitData에서 기존 RailwayVisualTableQuery의 설정을 받고 PostBindService에서 기존 Render Context의 Effect 서비스를 받는다. 씬 Attach 때 두 값을 CombatSceneDisplay 생성자에 넘긴다. SceneDisplay는 기존 CellsRoot를 부모로 Railway Presenter를 만든다.

| 파일 또는 위치 | 변경 책임 |
|---|---|
| Combat/Display/FieldOutlineRenderer.cs | 정적 Rail 슬롯과 상시 표시 |
| Combat/Display/RailwayTrackLayout.cs 신규 | 직선 분해·검증·정렬, Unity 독립 계산 |
| Combat/Display/FieldRailwayPresenter.cs 신규 | 정적 선로, 사건 단위 열차·그래프·Scope 수명 |
| Combat/Display/RailwayTrainVisualBinding.cs 신규 | 프리팹의 모델 루트·선택적 Animator 참조, 런타임 진행 상태는 없음 |
| Combat/CombatSceneDisplay.cs | 값 전달·Railway 사건 라우팅·Tick·정리 |
| Combat/CombatViewSystem.cs | 기존 View DB·Render 서비스 의존성 주입 |
| View/Table/Railway/Item/RailwayVisualDataSO.cs | 선로·바닥 높이 보정과 조건부 애니메이션 검증 |
| ViewGroup/RailwayVisual/Item/RailwayVisual_Default.asset | 실제 프리팹·연출 설정 연결 |
| Prefabs/Stage/Railway 신규 | 단위·피벗을 맞춘 View 소유 래퍼 프리팹과 명시적 Animator 바인딩 |
| Tests/EditMode 기존 테스트 어셈블리 | 경로·상시 표시·순서·취소·반복 재생·실패 검증 |

신규 .cs와 프리팹에는 .meta를 함께 만든다. 기존 GUID·직렬화 필드명은 유지한다. 선로 바닥·선로 높이는 `railTileYOffset`·`railYOffset` 두 독립 외형 보정값으로 추가한다. 경로 좌표·길이·시작·끝은 추가하지 않는다. ClipEndZ 검증은 이동 Clip을 실제 사용하는 경우에만 적용하고 다른 모션 값은 유한성·범위를 검증한다.

기존 임시 Cube를 실제 모델로 간주하거나 레일·열차용으로 원본 Cube 프리팹을 수정하지 않는다. 실제 모델이 정해지기 전에는 새 래퍼 프리팹에서 Cube를 얇은 바닥·선로·긴 열차 형태로 만들어 코드 동작을 확인할 수 있다. 완성 에셋 확인은 실제 기차·선로 모델 연결 후 수행한다. Render 작업자 영역에 있는 TraneRail.fbx는 원본을 수정하지 않고 모델 선택 때 참조 후보로만 확인한다.

## 9 원칙과 Framework 계약 점검

| 기준 | 적용 |
|---|---|
| SSOT와 DB P1~P7 | 규칙·좌표는 Core 결과와 Buffer, 외형은 View DB, 강조 상태는 FieldHighlightState에 둔다. View는 InGame Query를 읽지 않는다 |
| SOLID S | 직선 계산, 레일 표시, 결과 순서, 씬 조립의 책임을 분리한다 |
| SOLID O | 기존 좁은 사건 입구에서 Railway만 분배한다. 일반 사건 Presenter에 새 기믹 구현을 누적하지 않는다 |
| SOLID L | 사건 완료 1회, 취소 후 늦은 통지 무효, 멱등 정리와 예외 전파 계약을 지킨다 |
| SOLID I와 D | 기존 ICombatResultPresenter·ICombatPresentationControl과 필요한 Query·Render 서비스만 사용한다. 새 사용처 없는 인터페이스는 추가하지 않는다 |
| 지침 §2·§23·§26~28 | SO를 런타임 상태로 쓰지 않는다. Railway는 씬 표시 객체이고 별도 System·Manager·범용 Pool을 만들지 않는다 |
| 지침 §25 | SceneRoot·Mapper·CellsRoot와 명시적 프리팹 참조를 주입한다. 프레임별 Find·GetComponent를 하지 않는다 |
| FW-001~003 | 이미 등록된 모듈·서비스를 소비한다. 새 등록이나 Build 후 구조 변경이 없다 |
| FW-004·FW-011 | Render 내부 노드에 접근하지 않는다. AreaOutline 핸들과 EffectScope는 발급 계약으로 반환한다. 직접 만든 선로·열차만 직접 제거한다 |
| FW-012 | Empty는 요청하지 않는다. Effect는 기존 등록·ParticleSystem·ParticleStopNotifier·Pool 조건을 충족해야 한다 |
| FW-005~009·FW-013·015 | 입력·UI·이벤트 버스·JobQueue 확장은 없다. 기존 결과는 이벤트 버스 재발행 없이 직접 전달한다 |
| FW-014·016 | 새 CustomPool은 도입하지 않는다. InitData·PostBindService·Attach 이후 기존 Tick과 역순 종료를 따른다 |

근거: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md). Core 수정 요청은 현재 범위에는 없다. 구현 과정에서 사건 계약이나 Render 내부 변경이 필요해지면 구현을 멈추고 해당 경계 문서에 요청으로 분리한다.

## 10 검증과 구현 단계 구분

| 검증 | 완료 조건 |
|---|---|
| 직선 도출 | 가로·세로·다중·끊긴 구간·단일 칸·입력 순서 차이·Reverse가 의도대로 동작한다. 꺾임·교차·이웃 병렬·보드 밖·중복 입력을 진단한다 |
| 정적 표시 | GridOutline 대기·배치·행동 중 레일 유지, 비레일 전투의 핸들·데칼 동작 보존, CellPrefab 레일 색 보존 |
| Mapper 일치 | 두 모드의 서로 다른 칸 간격과 회전한 필드에서 선로·열차가 같은 칸 중심을 사용한다 |
| 사건 순서 | 열차가 끝날 때까지 뒤의 HealthChanged·Died·결과 확인이 실행되지 않는다. 완료 뒤 순서대로 한 번 진행한다 |
| 자원 수명 | 정상 완료·Cancel·Clear·Release·부분 시작/정리 실패·반복 발동·재설치에서 활성 열차·그래프·대여 이펙트가 남지 않는다. 완료 전에 정리가 끝난다 |
| 에셋과 애니메이션 | 프리팹 단위·피벗·폭·높이·앞뒤, Clip/Controller 없음·각각 지정·함께 지정, Empty/등록 Effect를 확인한다 |
| 운영 확인 | Database Live에서 실제 Railway 발동·사망·일반 종료·승패 우선·포기 대기를 확인하고 재진입 시 중복 선로가 없다 |

EditMode 테스트는 기존 어셈블리에 의미 있는 경계·수명·순서 사례를 추가한다. 기존 Playback·이동·DisplayReuse·입력·종료 회귀를 함께 확인한다. 에셋 값을 바꾸는 단계에서는 DataBaseAssetTests를 실행한다. 최종 시각 확인은 Unity Combat 재생으로 한다.

구현 단계는 **레일 상시 표시와 경로 계약 → 정적 프리팹 연결 → 열차 사건 재생과 자원 수명 → 실제 에셋 연결과 통합 확인**이다. Task·상태·실행 증거는 [Railway View 계획](../plans/2026-10-12-railway-view-plan.md)의 RW1~RW4를 따른다. RW1 코드 반영 후 리뷰 지점에서 멈추며 다음 Phase는 별도 진행 지시를 따른다.
