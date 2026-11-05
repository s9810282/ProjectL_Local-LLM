# 레일웨이 열차 칸 분리 Task 계획 (Head + Body N개)

작성일: 2026-10-20. 상태: **계획 작성, 코드 착수 전.** 설계 [railway-train-cars-design](../specs/2026-10-20-railway-train-cars-design.md)(TC1~TC6, 결정 Q-T1~Q-T5 모두 A — 2026-10-20 사용자 "모두 권장안으로").

아래 `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `R/` = `Assets/ProjectL/Prefabs/Stage/Railway/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 진행 규칙

- Phase마다 멈추고 보고한다. 커밋은 사용자.
- 검증: 바꾼 파일 MSBuild(오류·새 경고 0). 전체 관리 실행은 하지 않는다. EditMode 시험은 사용자가 Unity에서 돌린다.
- 새 `.cs`는 `.meta`와 함께 만든다. 지우는 파일 없음(옛 `Train.prefab`·`Train_Placeholder.prefab`·`Source/Train.prefab`, 클립·컨트롤러 그대로).
- `Train.fbx`·가져오기 설정은 민형님 파일이라 고치지 않는다.

### 0.1 계획에서 정한 것 (사용자 확인 대상)

| # | 내용 | 이유 |
|---|---|---|
| P1 | 설정 속성 이름도 `TrainPrefab` → `TrainHeadPrefab`(필드는 `FormerlySerializedAs("trainPrefab")`) | 이름이 뜻과 맞게. 부르는 곳은 Presenter 한 곳 + 시험 |
| P2 | Body 바인딩 검사: 두 연결점 모두 있음·루트의 자식(루트 자신 아님)·**서로 다른 Transform** | 같은 점이면 칸 길이 0으로 겹침 |
| P3 | Head `rearJoint` 검사는 `Validate(config)`에서 **몸통 수 > 0일 때만**, 자리 규칙은 FrontContact와 같음(루트 자식·ModelRoot 밖) | 옛 Train.prefab이 수 0에서 그대로 통과. 애니메이션이 움직이는 자리 밖 |
| P4 | 콜라이더 끄기는 칸별이 아니라 모델 보정 루트 전체에 한 번 | 몸통 칸도 같이 꺼짐, 호출 하나 |
| P5 | Body 이름 `TrainBody_1..N`, Head는 프리팹 복제 이름 그대로 | 시험·Hierarchy에서 구별 |
| P6 | 몸통 수 상한은 두지 않음 | 화면 밖 보장은 Q-T4 A(운영 에셋 시험)가 맡음 |

## 1. Phase TC-a — 코드와 시험 (화면 변화 없음)

설정 몸통 수 기본 0이라 지금 `Train.prefab` 그대로 나온다.

### Task

- [ ] **TC-a.1 설정** `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Railway/Item/RailwayVisualDataSO.cs`
  - `trainPrefab` → `trainHeadPrefab`(`[FormerlySerializedAs("trainPrefab")]`), 속성 `TrainHeadPrefab`.
  - 새 `trainBodyPrefab`(GameObject)·`trainBodyCount`(int, `Min(0)`, 기본 0), 속성 둘, 툴팁.
  - `Validate`: Head 필수(문구만 Head로), 수 < 0 거절, 수 > 0인데 Body 없음 거절.
- [ ] **TC-a.2 Head 바인딩** `C/Display/RailwayTrainVisualBinding.cs`
  - `rearJoint` 칸·`RearJoint` 속성·툴팁("Body를 붙이는 뒤 연결점, ModelRoot 밖").
  - `Validate(config)`: `config.TrainBodyCount > 0`이면 P3 규칙.
- [ ] **TC-a.3 Body 바인딩** 새 `C/Display/RailwayTrainBodyBinding.cs`(+ `.meta`)
  - `frontJoint`·`rearJoint`, 읽기 속성, `Validate()`(P2). 애니메이션 칸 없음.
- [ ] **TC-a.4 칸 조립** 새 `C/Display/RailwayTrainCars.cs`(+ `.meta`)
  - `internal static RailwayTrainVisualBinding Build(Transform correction, RailwayVisualData config)`(설계 TC3).
  - Head: 만들고 바인딩 없으면 예외, `Validate(config)`.
  - Body `i = 1..N`: 만들고 이름 붙이고 바인딩 없으면 예외, `Validate()`, `position += 앞 칸 rearJoint.position − frontJoint.position`.
- [ ] **TC-a.5 Presenter** `C/Display/FieldRailwayPresenter.cs` `AddTrain`
  - `Instantiate`·`GetComponent`·`Validate` → `RailwayTrainCars.Build(correction.transform, _config)`.
  - `DisableColliders(correction)`(P4). Animator 끄기·`instance.SetActive(true)`는 Head 그대로. 경로·앞면 오프셋·`TrackVisual` 무변경.
- [ ] **TC-a.6 기존 시험 이름 맞춤**
  - `T/FieldRailwayPresenterTests.cs`: 반사 필드 이름 `"trainPrefab"` 6곳 → `"trainHeadPrefab"`, `_config.TrainPrefab` → `TrainHeadPrefab`(708).
  - `T/CombatRailwayContactSceneTests.cs:63`: 반사 필드 이름.
  - `T/DataBaseAssetTests.cs` 738·749·767·775: 속성 이름만(내용 고침은 TC-b).
- [ ] **TC-a.7 새 시험** 새 `T/RailwayTrainCarsTests.cs`(+ `.meta`, GameObject를 쓰므로 Unity 전용)
  - 수 0 → Head만, Body 없음.
  - 수 3 → `TrainBody_1..3`, 각 Body `frontJoint`가 앞 칸 `rearJoint`와 같은 위치.
  - 보정 루트에 Yaw 30°·배율 0.8이 있어도 맞음.
  - 수 > 0 · Body 프리팹 없음 → 설정 `Validate` 거절.
  - Body 루트에 바인딩 없음 → 예외 / 연결점 없음·같은 점 → 예외.
  - 수 > 0 · Head `rearJoint` 없음 → 예외, 수 0이면 통과.
  - Presenter 한 사례: 수 2로 설치 → Body가 이동 루트 아래, `Paths[0].FrontStart`가 수 0과 같음(접촉·카메라 무변경 확인).

### 완료 조건

MSBuild 오류·새 경고 0. 사용자 Unity: 컴파일, `RailwayTrainCarsTests`·`FieldRailwayPresenterTests`·`CombatRailwayContactSceneTests`·`DataBaseAssetTests` 레일웨이 두 시험(T-F2로 이미 실패 중일 수 있음 — 실패하면 TC-a 전부터인지 구분).

## 2. Phase TC-b — 에셋·운영 시험·안내 (Head + Body N개)

### 사용자 에디터 작업 (Q-T5 A, TC-a 끝에 자리 표를 다시 드림)

| 대상 | 할 일 |
|---|---|
| 확인 | 지금 운영 `R/Train.prefab`이 메시를 잃었는지(설계 T-F2) |
| `R/New/Train Head.prefab` | 루트에 `RailwayTrainVisualBinding`. `Model Root` = 모델 자식(루트 아님). 빈 자식 `FrontContact`(머리 앞면, ModelRoot 밖)·`RearJoint`(뒤 연결점, ModelRoot 밖). Animator 없음(Q-T3 A). 폭 정규화 배율(가장 넓은 칸 폭 1). 머티리얼. 남은 z −2.62 덮어쓰기(설계 T-F3)는 정리 |
| `R/New/Train Body.prefab` | 루트에 `RailwayTrainBodyBinding`. 빈 자식 `FrontJoint`·`RearJoint`. **Head와 같은 배율**. 머티리얼 |
| `RailwayVisual_Default.asset` | Train Head Prefab = New Head, Train Body Prefab = New Body, Train Body Count = 원하는 수, Clip·Controller = None |

### Claude Task

- [ ] **TC-b.1 운영 에셋 시험** `T/DataBaseAssetTests.cs`
  - `EveryCombatStage_…BoundTrainPrefab`: Head 바인딩 `Validate(config)` + 수 > 0이면 Body 바인딩 `Validate()`.
  - `RailwayVisual_Restored…`: Train은 `Source/` 원본 비교에서 빼고(TrainLoad·TrainTile만 원본 비교), Head·Body는 메시 있음·URP Lit·콜라이더 없음, Clip·Controller 없음, `FrontContact` = Head 메시 앞면(기존 앞면 식 유지).
- [ ] **TC-b.2 운영 재생 시험** `T/FieldRailwayPresenterTests.cs` `RestoredResources_…`
  - Animator 단정 제거(Head에 없음), 시작·끝 Bounds = **열차 아래 모든 렌더러를 합친 범위**가 보드 밖(Q-T4 A).
- [ ] **TC-b.3 안내** `docs/superpowers/viewDocs/railway-view-guide.md`
  - 리소스 표(Head·Body·수, Clip·Controller 없음), "이동은 코드(등속), 외형 애니메이션은 선택" 문장, 칸 연결 규칙·연결점 자리, 몸통을 늘리면 Padding 확인.
- [ ] **TC-b.4 기록** 설계·계획 실행 기록, `CLAUDE.md` 현재 상태.

### 완료 조건

사용자 Unity: 위 시험 넷 통과, Combat 재생에서 Head 뒤 Body N개가 붙어 지나가고 시작·끝에 꼬리가 화면 밖, 접촉 사망·카메라 줌·흔들림 시각 그대로.

## 3. 실행 기록

(아직 없음)
