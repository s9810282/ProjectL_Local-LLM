# 전투 레일과 열차 View 설정 안내

작성일: 2026-10-13. 대상: View 작업자. 구현 상태와 검증 증거는 [Railway View Task 계획](../plans/2026-10-12-railway-view-plan.md)을 따른다.

2026-10-13 접촉 사망 C1~C3: Duration 기준 등속 이동과 접촉 예약을 연결했다. **기차 FrontContact 면이 유닛의 몸에 처음 닿은 프레임에 기존 사망 연출을 시작한다**(2026-10-16 히트 타이밍 H-3에서 "표시 중심에 도달한 프레임"을 바꿈, [히트 타이밍 설계](../specs/2026-10-16-combat-hit-timing-design.md) H4·H7). 몸 = 사망 직전 사실(`Expected`)의 배치 + 외형 프리팹 루트 BoxCollider 모양이고, 콜라이더가 없으면 표시 중심 한 점이다. 여러 대상은 독립적으로 쓰러지며 열차와 모든 사망 연출이 끝나야 후속 정산으로 넘어간다. 실제 화면 인수(C4)는 대기이며 단계별 상태·검증은 [접촉 사망 계획](../plans/2026-10-13-railway-contact-death-plan.md)을 따른다.

## 운영 설정과 진입

CombatStageData의 Railway를 켜고 RailCells를 입력하면 Core의 준비 결과를 거쳐 Combat View가 선로를 설치한다. View는 InGame DB의 규칙을 직접 읽지 않는다. 현재 Boss·Normal A~J는 모두 9×9이며 중앙 가로선 **x=0~8, y=4**, 첫 발동·반복 간격은 기존 **5**다. 적 본부는 사용자 승인으로 기존 x를 유지하고 y=7에 둔다.

실제 SO 설정 확인에는 **Database + Live**로 진입한다. Play From Boot 설정 창의 개발 진행을 사용할 때 Chapter 1, Node 2로 선택하면 Normal A를 확인할 수 있다(전투 정의는 늘 Database다. Fixture 모드는 2026-10-28에 지웠다). RecordInputOnly는 입력 기록 모드라 실제 열차 발동 사건이 나오지 않는다.

FieldView의 StageFieldViewData에서 CellPrefab 또는 GridOutline을 선택한다. 두 모드 모두 선로를 설치하며 Rail Tile Prefab을 지정하면 선로 바닥도 설치한다. GridOutline은 대기·배치 중에도 Rail Color의 테두리를 유지한다. 이동·공격의 기존 강조가 그 위에 표시된다. 레일 변경 후 재설치가 필요하며 일반 Refresh에서 선로를 다시 생성하지 않는다.

2026-11-03부터 **Rail Tile Prefab은 None이어도 된다.** None이면 바닥 타일 생성과 바닥의 등장 연출만 생략하며 레일 설치·등장 연출과 열차 동작은 유지한다. Train Head Prefab과 Rail Prefab은 필수다. 컴파일 미확인. Unity에서 None/지정 상태 각각의 전투 진입·선로 등장·열차 발동을 확인한다.

선로·바닥 타일 생성은 `FieldRailwayPresenter.AddSegment` → `AddStatic`에서 처리한다. 프리팹의 로컬 스케일을 그대로 사용하며 칸 크기로 덮어쓰지 않는다. 프리팹에서 크기를 조절하고 Unity에서 선로 간격·연결을 확인한다.

## 열차 칸 구성 (Head + Body N개)

2026-10-20부터 열차는 맨 앞 칸 Head 하나 뒤에 Body를 설정한 수만큼 이어 붙인다. 정책 원본은 [열차 칸 분리 설계](../specs/2026-10-20-railway-train-cars-design.md)(TC1~TC6)이고 단계·검증은 [칸 분리 계획](../plans/2026-10-20-railway-train-cars-plan.md)을 따른다.

| 설정 | 의미 |
|---|---|
| Train Head Prefab | 맨 앞 칸. 옛 Train Prefab 칸이 이름만 바뀐 것이다(기존 참조 유지). 앞면(Front Contact)이 이동·접촉 사망·카메라 시각의 기준이다 |
| Train Body Prefab | Head 뒤에 이어 붙이는 몸통 칸. Body Count가 0이면 비워도 된다 |
| Train Body Count | 붙일 몸통 수. 0이면 Head만 지나간다. 1 이상이면 Body Prefab이 필요하다 |

**칸 잇는 규칙은 하나다. 뒤 칸의 Front Joint를 앞 칸의 Rear Joint 위치에 맞춘다.** 간격은 모델의 사실이라 프리팹의 연결점이 원본이다. 겹침·틈은 연결점을 옮겨 조정한다. 칸들은 설치 때 한 번 놓이고 이동 루트의 자식으로 함께 움직인다. 매 프레임 칸마다 하는 일은 없다. 몸통 수를 바꿔도 접촉 사망·카메라 진입·이탈 시각은 같다(모두 Head 앞면 기준).

| 프리팹 | 루트 컴포넌트 | 자식으로 둘 것 |
|---|---|---|
| Head | `RailwayTrainVisualBinding` | Model Root(모델, 루트 자신 아님). Front Contact(머리 앞면, Model Root 밖). Rear Joint(뒤 연결점, Model Root 밖, Body Count가 1 이상일 때 필수). Animator는 Clip·Controller를 쓸 때만 필요하고 Model Root 안에 둔다 |
| Body | `RailwayTrainBodyBinding` | Front Joint·Rear Joint(루트의 자식, 서로 다른 Transform). 애니메이션 없음 |

크기는 프리팹 안에서 맞춘다. 가장 넓은 칸을 폭 1로 맞추고 **Head와 Body는 같은 배율**로 둔다. 칸마다 따로 폭 1로 맞추면 Body가 Head보다 커 보인다. 모델 원본은 `Resources/Environment/Railway/Models/Train.fbx`(민형님 파일, `TrainHead`·`TrainBody` 두 모델, 가져오면 폭 1.43×길이 2.80, 폭 1.20×길이 2.70)이고 가져오기 설정은 고치지 않는다.

몸통을 많이 늘리면 꼬리가 길어진다. 이동 루트(경로 위치)는 Head이므로 끝 위치에서 꼬리가 보드 안에 남지 않는지 Travel Padding Cells와 함께 확인한다. 지금 값(Outside 16 + Padding 18)이면 칸당 약 1.5칸 기준으로 몸통 약 20개까지 여유가 있다.

## 실제 리소스와 래퍼

현재 `Assets/ProjectL/DataBaseSO/ViewGroup/RailwayVisual/Item/RailwayVisual_Default.asset`은 아래 View 소유 래퍼를 참조한다. 런타임 DB 등록이나 Combat 씬의 추가 프리팹 연결은 필요하지 않다.

| 설정 | 연결 파일 |
|---|---|
| Train Head Prefab | Assets/ProjectL/Prefabs/Stage/Railway/New/Train Head.prefab (칸 분리 TC-b 에디터 연결 뒤. 그 전에는 옛 Train.prefab) |
| Train Body Prefab | Assets/ProjectL/Prefabs/Stage/Railway/New/Train Body.prefab |
| Train Body Count | 사용자가 정하는 수 |
| Rail Prefab | Assets/ProjectL/Prefabs/Stage/Railway/Rail.prefab |
| Rail Tile Prefab | Assets/ProjectL/Prefabs/Stage/Railway/RailTile.prefab |
| Clip | 없음(칸 분리 결정 Q-T3 A. 옛 RoadRoller.anim은 그대로 둔다) |
| Controller | 없음(옛 TrainSkill.controller는 그대로 둔다) |
| Effect | Empty |

`Source/`의 Train·TrainLoad·TrainTile 원본 프리팹, `Source/Models/`의 FBX, 애니메이션은 develop-4week의 원본과 GUID를 보존했다. 방향·피벗·크기·높이 보정과 URP Lit 머티리얼 대입은 상위 래퍼에서 한다. 원본 FBX·프리팹·Clip을 수정하지 않는다. Materials/의 네 머티리얼은 원본 색·금속성·광택 값을 옮겼으며 원본은 텍스처 없는 흰색이다.

Train 루트의 RailwayTrainVisualBinding은 자식 ModelRoot와 그 Animator를 명시적으로 참조한다. Animator의 Root Motion은 끄고 자동 재생도 끈다. Presenter는 사건 재생 중에만 수동 그래프를 만들며 AnimationEvent를 끈다. Animator가 쓴 루트 위치·회전·스케일을 매 평가 뒤 복원하므로 RailCells가 정한 경로를 벗어나지 않는다.

같은 바인딩의 FrontContact는 ModelRoot 밖의 형제 Transform이다. 현재 실제 모델 앞면에 연결했으며, 런타임 모델 보정·배율을 함께 받는다. 모델을 교체하면 앞면에 맞춰 조정하되 애니메이션이 움직이는 ModelRoot 아래에는 넣지 않는다. 기존 테스트용 Train_Placeholder도 같은 참조가 필요하다.

## 이동과 외형 값

| 값 | 현재 값과 의미 |
|---|---|
| Duration | 3초. 원본 Clip 길이 2초를 이 시간으로 재생 |
| Train Width In Cells | 0.8. 경로 횡방향 칸 간격을 기준으로 균등 스케일 |
| Outside Cells | 16. 보드 경계에 닿은 경로 끝의 바닥·선로 연장 |
| Travel Padding Cells | 18. 외부 선로 끝에서 열차 앵커가 더 이동하는 거리 |
| Train Model Yaw | 0. 래퍼가 원본의 방향 보정을 포함하므로 과거 설정의 180을 그대로 대입하지 않음 |
| Reverse | false. true는 경로 시작·끝과 열차 방향을 뒤집음 |
| Rail Tile Y Offset | -0.2233546. 실제 바닥 래퍼 윗면을 필드 기준 y=0.04로 맞춤 |
| Rail Y Offset | 0.045. 실제 선로는 그 위에 설치 |
| Clip End Z | -250. 이전 직렬화 호환용 보존 값. 현재 이동·접촉 시간 계산에서는 사용하지 않음 |

바닥의 단위 칸 면적은 0.9×0.9라 기본 테두리의 안쪽 여백을 확보한다. 선로는 폭 0.9·길이 1, 열차는 폭 1·길이 약 30.897의 원본 모델 비율을 유지한다. 기본 폭을 적용한 열차 길이는 약 24.717칸이므로 보드보다 길다. 현재 외부 거리 값은 이 모델의 시작·끝을 보드 밖으로 보내는 검사에 통과했으며 실제 카메라에서 최종 화면 확인은 남아 있다.

Clip과 Controller가 함께 있으면 **Clip이 우선**한다. Controller만 있으면 기본 상태 외형을 재생한다. 원본 TrainSkill의 기본 상태는 Idle이므로 Controller만 지정해도 RoadRoller가 자동 시작되는 설정은 아니다. 모든 경우 이동은 경로 길이/Duration에서 구한 등속이며 Clip 루트 이동과 무관하다. 둘 다 없으면 외형 애니메이션 없이 같은 등속으로 이동한다.

열차는 각 직선 경로당 하나를 준비하고 발동 때 함께 재생한다. 가로·세로·단일 칸·서로 떨어진 직선을 지원하며 꺾임·교차·서로 붙은 병렬 경로는 오류로 진단한다. 이동 끝에서 그래프·선택 이펙트를 정리하고 열차를 숨긴 뒤 기존 체력·사망 사건을 표시한다. View가 열차와 유닛의 충돌로 피해를 계산하지 않는다.

## 선택 이펙트와 확인할 화면

현재 Effect는 Empty다. 과거 TrainEffect의 숫자 2는 현 EffectType에 등록되지 않았고 원본 프리팹에는 찾지 못한 Material·Mesh 참조가 있다. Render 작업자가 FW-012 등록·의존성을 제공한 뒤 설정에 연결한다. 상세 요청은 [Render 인수 메모](../handoffs/2026-10-13-railway-effect-scope-return-note.md)에 있으며 미전달이다.

Unity Combat에서 두 표시 모드의 대기·배치·이동·공격 강조, 첫 발동·반복 발동, 실제 모델의 앞뒤·높이·화면 밖 시작/끝을 확인한다. 이어 레일 위 사망·메인 사망·일반 종료·승패 우선·포기 대기·오류 정지·씬 해제·재진입을 확인한다. 원본 Unity Test Runner와 화면 확인의 완료 여부는 Task 계획에 기록한다.
