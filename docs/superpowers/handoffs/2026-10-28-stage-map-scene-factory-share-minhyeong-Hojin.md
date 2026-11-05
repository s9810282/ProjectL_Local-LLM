# 스테이지 맵 씬 조립을 Factory로 — 민형님 공유 (2026-10-28, 호진, 미전달)

- 작성자: 호진(이호진)
- 출처: [검사 후속 정리 계획 §6.4](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) R-a(묶음 2), 설계 [§12.3 묶음 2](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md). 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`24c21b3a3`, 10-29).

## 바뀐 것 (Factory 쪽)

- `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs`의 스테이지 맵 줄: `new StageMapViewSystem(faults)` → `new StageMapViewSystem(faults, StageMapSceneFactory.Instance)`. 연결 줄 하나이고 조립 순서·등록은 그대로입니다.
- 새 `Factory/Runtime/InGame/View/StageMapSceneFactory.cs`(+ `.meta`): 생성 계약 `IStageMapSceneFactory`의 구현입니다. 씬 연결 한 번마다 서비스 묶음으로 팝업 Factory 셋(상점·회복·덱 편집)을 만들어 Scope에 넘깁니다. 상태가 없고 `Instance` 하나만 씁니다(전투 `CombatSceneFactory`와 같은 꼴).

## 왜

- 예전에는 `StageMapViewSystem`이 `PostBindService`에서 팝업 Factory를 람다로 미리 만들고, 시험용 `Bind*` 입구 열 개와 Scope 생성 경로 둘을 가졌습니다. 이제 System은 서비스를 묶음 값(`StageMapViewServices`) 하나로 모으고 씬 노드를 등록하며, 조립은 Factory 쪽 한 곳이 합니다.
- Framework·`ServiceKeys`·`LifeCycleSystem/Base`는 바꾸지 않았습니다. 서비스는 기존 `SystemGroupsService`·`BufferSystemKey` 입구로만 읽습니다.

## 같은 파일의 다른 변경 주의

- 지금 작업 트리의 `InGameViewSystemFactory.cs`에는 이 작업과 무관한 `TraitGrowthRequestLifetime` 줄(using·생성·등록·조립 인자)도 함께 있었습니다(→ 바뀜: 둘 다 `24c21b3a3`로 커밋됨, 10-29).

## 2026-10-29 R-f 작은 정리·제안 (미전달)

- 출처: [검사 후속 정리 계획 §6.9](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#69-phase-r-f--묶음-8-작은-정리-묶음-7-요청) R-f(묶음 7·8). 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`24c21b3a3`, 10-29).

### 바꾼 민형님 줄

- `LifeCycles/Lobby/Jobs/LobbyJob.cs`(+ `.meta`)를 지웠습니다(10-29 사용자 결정). 로비 Plan의 `JobType.Start` 갈래만 만들던 표본 Job이고, 로비는 `Next`만 걸어 운영에서 닿지 않았습니다. 실제 Job 예(`RunStartJob`·`LobbyArtifactCheckJob` 등)가 생겨 표본이 필요 없다고 봤습니다.
- `LifeCycles/Lobby/Artifact/Jobs/ArtifactJob.cs`(+ `.meta`, 민형님 `019e3fca`)도 같은 이유로 지웠습니다. 아티팩트 Plan의 `Start` 갈래만 만들던 표본입니다.
- `ArtifactPlan.Register`(본문 대부분 민형님 줄)와 `JobRegisterSystem.Initialize`의 아티팩트 Plan 줄(`_plans[...] = ... ArtifactPlan.Register(...)`)을 걷었습니다. 그 열쇠로 거는 곳이 없었고, 아티팩트 편집·프리뷰 Job은 로비 Plan이 `Next` 안에서 `ArtifactPlan.CreateCommand`·`CreatePreview`로 만듭니다.
- 그래서 [10-23 로비 Job·Plan 공유](2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md) §2 끝의 "`LobbyJob` 표본·Artifact 루트는 그대로"는 이 절이 대신합니다.
- 민형님 문서 `Systems/Documentation/framework-architecture.md`의 클래스 그림(176~188줄 근처)에 지운 `LobbyJob`·`ArtifactJob`이 남아 있습니다. 호진 쪽은 고치지 않았습니다.
- `ServiceKeys/Runtime/StageSelectionInputKey.cs`·`LobbyInputKey.cs`의 "루트에 등록" 주석을 실제대로 고쳤습니다(주석만). 두 키로 등록·조회하는 곳은 없고, 슬롯은 `InGameViewInputContext`로 전달됩니다.

### 제안 (고치지 않음)

- **입력 키 둘 걷기(F6):** 위 두 키는 쓰는 곳이 없습니다. 걷어도 되는지 정해 주세요.
- **Plan 입력 좁히기(F8):** `JobRegisterSystem`이 Lobby·Stage·Tutorial Plan에 `IBufferQuery` 전체를 넘겨, Plan이 모든 쓰기 싱크에 닿을 수 있습니다(FW-101). Plan마다 쓰는 읽기·쓰기 싱크만 묶은 작은 입력을 넘기는 꼴을 제안합니다. 등록기와 Plan 서명이 바뀌어 민형님 결정이 필요합니다.
- **빈 로딩 노드(F10):** `LifeCycleSystemFactory`가 씬이 없는 예약 번호 4(`SceneIndices.Artifact`)에 `ArtifactLifeCycle`을 등록해 빈 로딩 노드가 생깁니다. 이 LifeCycle은 진입·이탈 훅이 비어 있는 예약 골격입니다(민형님이 만든 파일, 호진·용근 줄 일부). 씬이 생길 때까지 등록하지 않을지, 예약으로 둘지 정해 주세요.
