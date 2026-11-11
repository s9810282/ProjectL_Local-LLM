# Stage Map Intro Implementation Plan — 2026-11-06

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 이 세션에서 이어 온 Codex 직접 수행 방식을 따른다. 프로젝트 사용자 규칙이 스킬의 시험·커밋 절차보다 우선한다.

**Goal:** 챕터 최초 표시에서 플레이어부터 선이 뻗으며 노드가 착지하고, 카메라가 보스까지 갔다 돌아온 뒤 맵 조작을 허용한다.

**Architecture:** 불변 시간표 하나와 Scope 수명의 공통 시계가 낙하·선 그리기·카메라 왕복을 구동한다. 각 표현과 자원은 전용 부품이 맡고, 최초 시작 이력은 System 수명으로 유지한다. 기존 Factory가 부품을 조립하며 Scope는 로딩 준비·입력 정책·취소 경계만 연결한다.

**Tech Stack:** Unity 6000.0.73f1, C#, View DB SO/Data, 기존 Buffer ReadSink, `ILodingPreparation`, Camera Catalog Handle, EffectScope/ColorHandle, View 소유 임시 `Mesh`.

**Spec:** [보완된 맵 진입 연출 설계](../specs/2026-11-06-stage-map-intro-design-Hojin.md). 실행자는 설계와 계획을 함께 읽는다. 설정·표현 요구의 원본은 설계이며 이 문서는 작업 순서·계약·확인 기록을 소유한다.

## Global Constraints

- 상태: **Phase 1 MI1~MI8 코드 작성·정적 검토·문서 인계 완료, 컴파일 미확인. 사용자 Unity 확인 U1~U9 대기**. 기존 노드 낙하 Phase 1의 기본 재생 확인은 [선행 계획의 실행 기록](2026-11-06-stage-node-drop-plan-Hojin.md)에 있으며, 새 기능의 완료 증거로 사용하지 않는다.
- 이 계획은 별도 후속 작업이다. 선행 계획에 없던 Phase 2를 소급해 만들지 않는다. 아래 **새 계획의 Phase 1**을 승인받은 뒤 그 Phase만 수행하고 보고한다. 설계와 다른 결정이 필요하면 구현 전에 확인한다.
- 작업 루트는 `C:/Users/Hyeon2-400/Project_L`. 이번 기능의 Codex 작성 예외는 사용자가 지정했다. 다른 작업자의 변경을 덮어쓰기·stash/reset/clean하지 않으며 커밋·push는 사용자가 직접 한다.
- **View 테스트 코드·시험 도우미 작성, MSBuild·Unity 실행, 커밋·push를 하지 않는다.** 정적 확인과 사용자 Unity 확인을 구분하고 구현 보고에는 **컴파일 미확인**을 적는다. 기존 시험은 사용자 규칙에 해당하는 최소 호환 수정만 허용한다.
- 수정 범위: StageMap View, 그 설정 SO/Data와 운영 에셋, 기존 View Factory, 관련 문서. Core·Buffer 구현·FieldCamera·Architecture·공용 Camera/Render/Loading·셰이더·전투 본부·공용 프리팹은 수정하지 않는다. 연결선 원본 메시·머티리얼·프리팹과 Missing Script 에셋을 유지한다.
- 새 서비스·UI 화면·UIId·입력 Layer·저장 데이터 이력을 만들지 않는다. View는 Buffer의 진행 사실만 읽으며 InGame DB 조회나 Core 쓰기를 추가하지 않는다. 하위 Render 서비스는 기존 Global Context를 통해 받는다.
- **낙하 높이 20 월드 단위, 개별 낙하 0.8초 유지.** 시작 대기 0.6초·전개 7초·보스 정지 0.9초·복귀 2.2초는 설계의 초기 제안값이며 사용자 Unity 확인에서 조절할 수 있다. 기존 `WaveSpreadSeconds`는 제거한다.
- 최초 재생은 각 Run/챕터 한 번. 선 도착과 착지를 맞추며 분기는 동시에 진행한다. 설정 창에서는 전체 scaled time 진행이 멈추고, 마지막 실제 착지 이후 효과 대기 상한은 게임 시간 3초다.
- 네임스페이스는 폴더 경로, 블록 namespace, Allman, 공백 4칸, UTF-8/LF, `sealed` 기본, 한국어 주석. 새 `.cs`에는 `.meta`를 동반하며 이름을 바꾸는 기존 파일의 GUID는 유지한다.

## Review Focus

자동 시험을 추가할 수 없으므로 아래 다섯 위험을 담당 Task의 정적 추적과 사용자 확인 시나리오에 배정한다. 미재현 경로를 통과로 기록하지 않는다.

| 조건 | 기대 동작 | 담당 / 사용자 확인 |
|---|---|---|
| 부분 메시·재사용·준비 중 실패 | 이미 그려진 점선 불변, 원본 메시 복원, 임시 Mesh만 해제 | MI2·MI6 / U2·U8 |
| 카메라 핸들 대여 지연·작성 구도·에디터 미리보기 | 정상 대기는 준비 버전을 유지, 범위 선행 후 초기 구도 적용, 자동 조작자 한 명, 정확한 복귀 | MI4·MI6·MI7 / U3·U8 |
| 큰 deltaTime·시간 0·설정 창 일시정지 | 합류 효과 한 번, 새 효과 즉시 회수 금지, 공통 시계 정지 | MI1·MI3·MI5 / U1·U5·U7 |
| 시작 전/후 취소·Run/맵 교체·늦은 이전 Scope | 시작 전 재시도, 시작 후 생략, 새 표시/핸들에 이전 쓰기 없음 | MI5·MI6 / U6·U8 |
| 차단 중 누름·완료 후 뗌·Alt 조작·보류 팝업 | 잔여 클릭/드래그 없음, 우회 차단, 유효한 팝업만 한 번 복원 | MI7 / U4·U9 |

## 1. 파일과 책임

표의 접두사와 파일명을 합친 것이 정확한 저장소 상대 경로다. 새 폴더 구조를 뜻하지 않는다.

| 접두사 | 경로 |
|---|---|
| D | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageMap/Item/` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/` |
| F | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/` |
| A | `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/` |

| 종류 | 파일 | 책임 / Task |
|---|---|---|
| 생성 | `D/StageMapIntroData.cs` | 불변 진입 연출 설정 / MI1 |
| 수정 | `D/StageMapViewData.cs`, `D/StageMapViewDataSO.cs`, `D/StageNodeDropData.cs`, `A/StageMapView_Default.asset` | 설정 변환·검사·Wave 설정 제거 / MI1 |
| 생성 | `V/Entry/StageMapIntroTimeline.cs` | 불변 노드·선·카메라 예약 시각 / MI1 |
| 생성 | `V/Entry/IStageLinkDrawTarget.cs`, `V/Entry/StageLinkDrawPresentation.cs` | 빌려 쓰는 선 조작 계약·진행률 / MI2 |
| 수정 | `V/StageLinkView.cs`, `V/StageMapViewDisplay.cs`, `V/StageMapViewScene.cs` | 선 임시 메시 소유·대상 수집·표시 연결 / MI2·MI6 |
| 수정 | `V/Entry/StageNodeDropPresentation.cs` | 공통 시각을 소비하는 낙하·착지 효과 / MI3 |
| 생성 | `V/Entry/IStageMapCameraTourControl.cs`, `V/Entry/StageMapCameraTour.cs` | 같은 카메라 소유자의 좁은 제어 창구·왕복 보간 / MI4 |
| 수정 | `V/StageMapCameraDrag.cs` | 유일한 핸들/offset 소유, 자동 조작 토큰·복원 / MI4 |
| 이름 변경 | `V/Entry/StageNodeDropHistory.cs` → `V/Entry/StageMapIntroHistory.cs` | 전체 연출의 Run/챕터 이력 한 개, `.meta` GUID 유지 / MI5 |
| 생성 | `V/Entry/StageMapIntroPresentation.cs`, `V/Entry/StageMapIntroInteractionPolicy.cs` | 공통 시계·전체 상태 / MI5, 제거 가능한 추가 차단 / MI7 |
| 수정 | `V/StageMapViewSystem.cs`, `V/StageMapViewServices.cs`, `F/StageMapSceneFactory.cs`, `V/StageMapViewScope.cs` | 이력 참조 이전·생성/주입·준비/정리 / MI5·MI6·MI7 |
| 수정 | `V/StageMapViewInput.cs`, `V/StageMapInteraction.cs`, `V/Editor/StageMapCameraFramingReceiver.cs` | 일반·에디터 입력 경계 / MI7 |
| 갱신 | `docs/superpowers/viewDocs/stage-map-guide-Hojin.md`, 이 계획, 대응 설계의 상태 | 사용법·실행/확인 기록 / MI8 |
| 생성 | `docs/superpowers/handoffs/2026-11-06-stage-map-intro-framework-share-Hojin.md` | Factory/로딩/소유권 변경 공유 메모, 미전달 / MI8 |

기존 `StageNodeDropTarget`·`StageNodeDropEffects`·`StageNodeView`의 자세/표시 정책과 효과 받침 구현은 재사용한다. `StageMapLoadingPreparation`·`IStageMapSceneFactory`의 공개 계약도 유지한다. 이 파일들은 호출 계약을 대조하지만 필요 없이 재작성하지 않는다. 표시 부품의 보조 상태는 해당 소유자의 private 중첩 타입으로 두며 서비스나 범용 연출 프레임워크로 확대하지 않는다.

## 2. Phase 1 — 통합 연출 구현·정적 검토·인계

의존 순서는 **MI1 → MI2 → MI3 → MI4 → MI5 → MI6 → MI7 → MI8**이다. MI2~MI4의 표현 책임은 분리하지만 동일 준비·완료·취소 경계를 사용하므로 하나의 Phase에서 연결한다. 중간 Task는 단독 배포/인수 단위가 아니다. Phase 종료 후 멈추고 사용자 Unity 확인을 받는다.

| Task | 작업 | 결과 |
|---|---|---|
| MI1 | 설정과 공통 시간표 | 높이 20을 유지하고 낙하·선·카메라의 예약 시각을 한 곳에서 계산 |
| MI2 | 연결선 그리기 | 이미 나타난 점선의 위치·간격을 유지하면서 선 끝이 이동하며 순차 노출 |
| MI3 | 기존 낙하 연동 | 선이 도착하는 시각에 노드가 착지하고 효과를 한 번 재생 |
| MI4 | 카메라 왕복 | 플레이어 구도에서 보스까지 이동·정지한 뒤 처음 구도로 복귀 |
| MI5 | 전체 진행과 최초 이력 | 챕터별 한 번 재생, 일시정지·취소·전체 완료 조건 관리 |
| MI6 | 로딩과 씬 연결 | 표시 준비·정상 대기·취소를 구분하고 부품을 생성·연결·정리 |
| MI7 | 임시 입력 차단 | 전체 완료까지 노드 선택·수동 카메라 조작을 막고 이후 기존 UI 재개 |
| MI8 | 정적 검토와 인계 | 구조·호출 계약 검토, 가이드/공유 문서 갱신, Unity 확인 항목 보고 |

체크박스는 코드 작성 및 지정 정적 확인의 기록이다. MI1~MI8은 아래 실행 기록에 따라 완료했으며, 사용자 실행 확인 U1~U9는 모두 미확인이다. 시그니처는 `internal`을 기본으로 하고 인터페이스 구현은 C# 접근 규칙을 따른다.

### MI1: 설정 이전과 공통 시간표

**Files:** 생성 `D/StageMapIntroData.cs`, `V/Entry/StageMapIntroTimeline.cs`와 각 `.meta`; 수정 파일은 §1의 MI1 행. 설계 §5·§9를 구현한다.

**Interfaces:**
- `StageMapIntroData(bool enabled, float startHoldSeconds, float revealSeconds, float bossHoldSeconds, float returnSeconds)`와 동명의 PascalCase get-only 프로퍼티, `void Validate()`.
- `StageNodeDropData(bool enabled, float height, float seconds)`로 축소. `StageMapViewData` 생성자의 `nodeDrop` 뒤, 선택 인수 `hoverStyle` 앞에 필수 `StageMapIntroData mapIntro`를 추가하고 `MapIntro` 프로퍼티와 검증을 연결한다.
- `StageMapIntroTimeline.Build(StageMapIntroData intro, StageNodeDropData drop, IReadOnlyList<StageProgressData> nodes, IReadOnlyDictionary<int, Vector3> positions, int currentNodeId, Vector3 forward, IReadOnlyCollection<int> dropNodeIds)` → `StageMapIntroTimeline`. 위치는 **건물 Visual이 아닌 노드 루트의 월드 좌표**다.
- 결과는 `bool HasTargets`, `int CurrentNodeId`, `int BossNodeId`, `Vector3 PlayerPosition`, `Vector3 BossPosition`, `double FirstChangeSeconds`, `float EffectiveDropSeconds`, `IReadOnlyList<NodeTiming> Nodes`, `IReadOnlyList<LinkTiming> Links`, `CameraTiming Camera`를 가진다. 반환 목록은 외부에서 변형할 수 없는 준비 시점 사본이다.
- 같은 파일의 중첩 불변 값: `NodeTiming(int NodeId, double ArrivalSeconds, double DropStartSeconds, bool HasDrop)`; `LinkTiming(int FromNodeId, int ToNodeId, double StartSeconds, double EndSeconds)`; `CameraTiming(double RevealStartSeconds, double RevealEndSeconds, double ReturnStartSeconds, double EndSeconds)`. 표기는 데이터 항목 계약이며 가변 프로퍼티를 요구하지 않는다.

- [x] **설정 추가:** SO의 `stageMapViewData.mapIntro` 입력과 새 Data의 검사/변환을 연결한다. Enabled=true와 Global Constraints의 초기 시간값을 운영 에셋에 명시한다. 시간은 유한해야 하며 Reveal만 양수, 나머지는 0 이상이다. 기존 높이 20·시간 0.8은 보존하고 새 NodeDropInput의 높이 초기값도 20으로 맞춘다.
- [x] **설정 이전:** `WaveSpreadSeconds`의 Data/생성자/검사와 SO `waveSpreadSeconds`, 운영 에셋의 해당 필드를 제거한다. `rg`로 생성자 호출과 target-typed `new`를 대조한다. 과거 문서의 기록은 남기고 현행 사용법은 MI8에서 교체한다.
- [x] **그래프 검증:** 현재부터 전방 도달 집합만 만든다. 누락/중복 ID·중복 보스·도달 불가 보스·비유한 좌표/축·0 길이/역방향 간선·보스 뒤 노드를 대상 ID와 함께 구성 오류로 보고한다. 현재가 보스이며 출발선이 없는 경우는 빈 시간표다. 현재/클리어 건물의 숨김이 경로 연결을 지우지 않게 한다.
- [x] **예약 계산:** 설계 §5의 `q`, `P`, `T0`, `A`를 그대로 구현한다. `dropNodeIds` 중 전방 대상만 HasDrop으로 표시하고 유효 D=0 규칙을 적용한다. Nodes에는 경로 노드를, Links에는 경로 간선을 한 번씩 넣는다. FirstChange는 실제 낙하 시작/선 전개 시작 중 가장 이른 시각이다. 카메라 정지/복귀 경계도 여기서 계산하고 다른 부품이 시간을 재계산하지 않는다.
- [x] **정적 산술 대조:** H=0.6/R=7/D=0.8, qMin=0.2이면 P=0, 첫 도착=2.0·낙하 시작=1.2·보스 도착=7.6·복귀 종료=10.7초다. R=1이면 P=0.6, 첫 낙하 시작=0.6초다. 합류 노드 도착이 진입선 수와 무관한지, 높이/시간 0·낙하 꺼짐·대상 없음에서 분모 0과 음수 시작이 없는지 표로 기록한다. **사용자 확인: U1·U6·U7.**

### MI2: 고정 점선 패턴의 순차 노출

**Files:** §1의 MI2 파일과 새 파일 `.meta`. 설계 §6 연결선을 구현한다.

**Interfaces:**
- `IStageLinkDrawTarget`: `int FromNodeId { get; }`, `int ToNodeId { get; }`, `void Prepare()`, `void SetProgress(float progress)`, `void Restore()`. `StageLinkView` 내부 상태가 구현하며 호출자는 자원을 소유하지 않는다. 만료된 대상의 호출은 새 세대에 영향을 주지 않는다.
- `StageLinkView.CollectDrawTargets(List<IStageLinkDrawTarget> targets)`, 이를 전달하는 `StageMapViewDisplay.CollectLinkDrawTargets(List<IStageLinkDrawTarget> targets)`와 `StageMapViewScene.CollectLinkDrawTargets(List<IStageLinkDrawTarget> targets)`를 추가한다. 수집 자체는 전체 메시를 만들거나 숨기지 않는다.
- `StageLinkDrawPresentation.Prepare(IReadOnlyList<IStageLinkDrawTarget> targets, IReadOnlyList<StageMapIntroTimeline.LinkTiming> timings)`, `Tick(double elapsedSeconds)`, `Cancel()`, `bool IsComplete`. Prepare가 시간표에 있는 대상만 준비/숨김 처리하고, 나머지는 최종 상태로 복원한다.

- [x] **소유 상태 추가:** 기존 선별 상태에 MeshFilter·원본 Mesh·Renderer enabled·최종 배치와 원본 정점/UV 대응을 보관한다. 읽기 가능한 4정점 XY Quad, 두 삼각형, 길이 방향 UV.x와 양쪽 정점 짝을 확인한다. 메시 정점 순서의 우연에 의존하지 않으며 지원하지 않는 구성을 해당 간선 ID로 진단한다.
- [x] **부분 노출 구현:** 대상별 준비 때 임시 Mesh와 작업 배열을 한 번 만든다. 전체 Transform의 중점·길이·회전·높이를 고정하고 도착 쪽 두 정점/UV만 원본 출발→도착 사이로 보간한다. p=0은 숨김, 0<p<1은 임시 메시, p=1은 원본 복원/임시 해제다. 원본 Bounds를 유지하며 매 Tick 배열/Mesh를 만들지 않는다. `_DashCount`·머티리얼·sharedMesh 원본에는 쓰지 않는다.
- [x] **진행·취소 연결:** LinkTiming으로 선형 진행률을 계산한다. 취소는 연출 참조를 먼저 비우고 빌린 대상의 Restore를 호출한다. 선 View의 Clear는 대상 무효화→원본 재연결→자기 임시 Mesh 해제→ColorHandle 반환→GameObject 파괴 순서로, 한 예외에도 나머지를 정리한다. 부분 준비 실패와 반복 Restore/Clear도 같은 순서를 지킨다.
- [x] **재사용 대조:** 새 표시와 같은 표시 재사용 모두 Display의 기존 선에서 대상을 수집한다. 재사용 때 완성/부분 메시를 다시 원본으로 정상화한 후 준비한다. 기존 호버는 색상과 폭만 변경하고 별도 MaterialPropertyBlock으로 ColorHandle 값을 덮지 않는다.
- [x] **정적 도형 대조:** p=0.25/0.5/0.75/1에서 이미 드러난 같은 지점의 UV가 같은지, 간선 도착 시간이 점선 빈 구간에 영향받지 않는지 확인한다. 준비 실패/완성/취소/재사용마다 원본과 임시 자원의 소유 경로를 추적한다. **사용자 확인: U2·U8.**

### MI3: 낙하를 공통 시간표 소비자로 변경

**Files:** `V/Entry/StageNodeDropPresentation.cs`. `StageNodeDropTarget`·`StageNodeDropEffects`는 기존 계약을 사용한다. 설계 §4·§5·§7을 구현한다.

**Interfaces:**
- 생성자 `StageNodeDropPresentation(StageNodeDropData settings, StageNodeDropEffects effects)`.
- `Prepare(Transform mapRoot, IReadOnlyList<StageNodeDropTarget> targets, IReadOnlyList<StageMapIntroTimeline.NodeTiming> timings, float effectiveDropSeconds)`, `Tick(double elapsedSeconds)`, `Cancel()`, `bool IsComplete`.
- 기존 Effects의 `Begin(Transform)` → `long`, `Play(long, Pose)`, `IsComplete(long)` → `bool`, `End(long)` 및 대상의 `NodeId/GroundPose/ApplyHeightOffset/Reveal/Restore`를 유지한다.

- [x] **상태 축소:** Drop에서 최초 이력·독립 누적 시계·평면 거리 지연·로딩 대기를 제거한다. HasDrop 노드와 대상 ID를 한 번 연결하고, 전방 대상이 아닌 건물과 낙하 꺼짐의 건물은 기존 최종 표시로 복원한다.
- [x] **예약 낙하:** DropStart부터 Arrival까지 기존 `height * (1 - t*t)`와 맵 월드 up을 적용한다. 착지 때 정확한 원래 자세/Renderer 상태를 복원하고 노드별 효과를 한 번 재생한다. 높이/시간 0은 도착 시 즉시 착지·효과, 낙하 꺼짐은 효과 없음이다. Collider·논리 노드 루트·마커는 움직이지 않는다.
- [x] **효과 완료:** 마지막 착지 효과를 실제 생성한 Tick의 공통 경과 시각을 저장한다. 이후 `elapsedSeconds - 실제 생성 시각`으로 3초 상한을 판정하며 큰 deltaTime 이전 구간을 새 효과의 경과로 사용하지 않는다. 카메라 복귀 중에도 자연 종료/상한 회수를 확인한다. 상한 경고는 한 번이고 반환 완료까지 IsComplete=false다.
- [x] **정리 유지:** 취소/오류에서 세대를 먼저 무효화한 뒤 Effects.End→대상 Restore를 예외 집계로 수행한다. 기존 독립 unit-scale 효과 받침과 재진입 방어를 보존하고 Drop이 풀의 Unity 객체를 직접 파괴하지 않는다.
- [x] **정적 경계 대조:** 한 Tick의 여러 착지·합류 중복·효과 Play 도중 취소·설정 창 정지·시간 0을 추적한다. 미재현 경로는 미확인으로 남긴다. **사용자 확인: U1·U5·U7·U8·U9.**

### MI4: 기존 카메라 소유자를 통한 왕복

**Files:** §1의 MI4 파일과 새 파일 `.meta`. 설계 §6 카메라를 구현한다.

**Interfaces:**
- `IStageMapCameraTourControl`: `bool IsReady { get; }`, `long BeginTour(Vector3 playerPosition, Vector3 bossPosition)`, `void SetTourProgress(long generation, float progress)`, `void EndTour(long generation, bool restoreStart)`. 기존 `StageMapCameraDrag`가 구현한다. IsReady는 유효 핸들·구도·범위 준비 여부다. 토큰은 자동 조작의 소유 세대를 뜻한다.
- `StageMapCameraTour.Prepare(IStageMapCameraTourControl camera, Vector3 playerPosition, Vector3 bossPosition, StageMapIntroTimeline.CameraTiming timing)`, `Tick(double elapsedSeconds)`, `Cancel(bool restoreStart)`, `bool IsComplete`.

- [x] **자동 조작 소유:** BeginTour는 진행 중 드래그를 끝내고 동일 핸들의 자동 조작 권한을 얻는다. MI6의 범위 초기화→초기 구도 적용/범위 재계산→핸들 준비를 마친 뒤 실제 플레이어가 보이도록 필요한 진행축 보정을 로딩 중 적용하고 그 결과를 시작 snapshot으로 한 번 보관한다. 초기 구도 준비와 BeginTour에 사용자 입력 허용 함수 `CanInteractWithMap()`을 적용하지 않는다. 유효한 준비 문맥·핸들·자동 조작 소유권은 별도로 검사한다. 미준비 호출은 명확히 실패하며 대기는 MI6에서 한다.
- [x] **목표 계산:** 실제 보스의 월드 위치를 기존 맵 평면/진행축 계산에 넣어 목표 offset을 구한다. `_maximum`을 목표로 사용하지 않는다. 기존 렌즈·회전·가로 구도를 유지하며, 화면 포함 판단은 준비 중인 실제 구도를 사용한다. 출력 카메라의 지난 프레임 자세만 보고 보정을 확정하지 않는다.
- [x] **왕복 적용:** 공통 CameraTiming의 전개 구간은 0→1 선형, 정지 구간은 1, 복귀는 `1 - (3u² - 2u³)`로 진행한다. ReturnSeconds=0이면 복귀 시작 시 즉시 0으로 복원한다. 정확한 snapshot 복원과 자동 소유권 반환 후 IsComplete=true다. 직접 Camera Transform 쓰기·새 Catalog 대여를 추가하지 않는다.
- [x] **소유권 복원:** 일반 BeginDrag/MoveDrag/Scroll은 자동 소유 중 쓰지 못하게 한다. EndTour는 해당 토큰일 때만 복원/해제하며 만료 핸들·이전 세대는 새 카메라에 쓰지 않는다. 내부 수동 offset도 복원한다. 자동 소유 중 PreviewFraming 적용을 보류하되 사용자 저장값/EditorPrefs는 변경하지 않는다. 정상 복귀 뒤 미변경 preview를 다시 적용해 offset이 튀는지도 대조한다.
- [x] **정적 경로 대조:** AfterLoad 바인딩 전·진행 중 해제·작성 구도가 허용 범위를 넓힌 경우·복귀 0·첫 수동 입력을 추적한다. 플레이어가 진행축 보정만으로 화면에 들어올 수 없는 구도는 회전/줌으로 임의 수정하지 않고 구성 오류로 보고한다. **사용자 확인: U3·U7·U8·U9.**

### MI5: 단일 이력과 전체 연출 상태

**Files:** `V/Entry/StageMapIntroHistory.cs`로 이름 변경 및 `.meta` 이동, 생성 `V/Entry/StageMapIntroPresentation.cs`와 `.meta`; System/Services의 이력 타입·프로퍼티 이름을 `IntroHistory`로 맞춘다. 설계 §7·§9를 구현한다.

**Interfaces:**
- `StageMapIntroHistory(IRunProgressBufferReadSink runs)`, `HasStarted(RunId runId, int mapKey)` → `bool`, `TryMarkStarted(RunId runId, int mapKey)` → `bool`: 기존 계약 유지.
- `StageMapIntroPresentation(StageMapIntroData settings, StageMapIntroHistory history, StageNodeDropPresentation drops, StageLinkDrawPresentation links, StageMapCameraTour camera)`.
- `BeginPreparation()`, `Prepare(RunId runId, int mapKey, Transform mapRoot, StageMapIntroTimeline timeline, IReadOnlyList<StageNodeDropTarget> dropTargets, IReadOnlyList<IStageLinkDrawTarget> linkTargets, IStageMapCameraTourControl cameraControl, Func<bool> isCurrent)`, `Tick(float scaledDeltaTime, bool isLoading, bool isPaused)`, `Cancel(bool restoreCamera)`, `bool IsPending`, `bool IsPrepared`.
- isCurrent는 해당 Run/맵/Scope 준비 세대가 아직 유효한지 확인하는 함수다. 준비 때 한 번 연결하며 매 Tick delegate를 새로 만들지 않는다. 하위 부품에 전체 Services/Buffer를 넘기지 않는다.

- [x] **이력 이전:** 기존 HashSet과 최신 Run 비교 동작을 유지하며 이름/GUID/호출처를 함께 변경한다. 낙하·선·카메라별 이력을 추가하지 않는다. Effects의 기존 System 수명은 유지한다.
- [x] **준비/생략 상태:** Preparing→PreparedWaitingForLoading→Playing→Complete와 Cancelled/Failed를 구분한다. IsPrepared는 배치·숨김·카메라 준비가 끝난 경우에만 true, IsPending은 준비/대기/재생 동안 true다. 전체 꺼짐·이미 본 챕터·빈 시간표는 노드/선을 최종 상태로 복원하고 새 효과·왕복·이력 없이 Complete로 간다. 생략 경로는 Tour.Prepare를 호출하지 않는다.
- [x] **공통 시계/기록:** 로딩이 끝난 뒤 scaled 시간 하나를 누적한다. 로딩/일시정지 중에는 전혀 진행하지 않는다. FirstChangeSeconds 도달 직전 최신 문맥과 TryMarkStarted를 확인한 후 첫 표시 변경을 허용한다. 준비와 초기 대기만으로는 기록하지 않는다. 낙하만 꺼져 있어도 선/카메라 시작으로 이력을 남긴다.
- [x] **재생/완료 조합:** 같은 경과 시각을 Drop→Link→Camera에 전달하고 외부 호출 뒤 문맥/epoch를 다시 검사한다. 전체 Complete는 세 하위 IsComplete가 모두 참일 때만 게시한다. Drop의 효과 대기는 정지/복귀와 병행한다. 반환 도중 오류가 나면 Failed로 유지하고 기존 오류 경계에 보고하며 정상 완료로 풀지 않는다.
- [x] **취소:** 상태/epoch를 먼저 무효화하고 Drop.Cancel→Link.Cancel→Camera.Cancel 순으로 정리하며 예외를 집계한다. 현재 Scope 취소는 시작 구도를 복원하고, Supersede/소유권 상실은 restoreCamera=false다. 시작 전 취소는 다음에 재시도, 시작 후 취소는 같은 Run/맵에서 생략한다. 취소는 timeScale=0이어도 즉시 작동한다.
- [x] **정적 상태표 확인:** 준비 중 취소·대기 중 취소·첫 변경 직후 취소·이미 본 챕터·새 Run·전체 꺼짐·낙하만 꺼짐을 대조한다. 효과/정리 콜백의 재진입이 새 연출 상태를 지우지 않는지 확인한다. **사용자 확인: U5·U6·U7·U8.**

### MI6: Factory 조립과 로딩·표시 수명 연결

**Files:** `F/StageMapSceneFactory.cs`, `V/StageMapViewScope.cs`, `V/StageMapViewScene.cs`, `V/StageMapViewDisplay.cs`, `V/StageMapViewSystem.cs`, `V/StageMapViewServices.cs`. 설계 §4·§7·§9를 구현한다.

**Interfaces:** MI1~MI5의 계약을 소비한다. `StageMapViewScope` 생성자의 `StageNodeDropPresentation appearance` 자리를 `StageMapIntroPresentation intro`로 바꾼다. 기존 `TryPrepareEntry()` → `LodingPreparationState`, `CancelEntryPreparation()`, `EntryPreparationVersion`, `StageMapLoadingPreparation`/`IStageMapSceneFactory.CreatePreparation` 계약을 유지한다.

- [x] **생성/주입:** Factory에서 설정·기존 Effects를 써 Drop/Link/Tour/Intro를 만들고 Scope에 주입한다. Scope/Scene은 생성한 카메라 소유자를 준비 시 좁은 계약으로 전달한다. 기능별 시간·정점·보간 수식을 Factory/Scope에 넣지 않는다.
- [x] **준비 순서:** Run/맵 확인→배경/노드 표시→현재 마커 동기화→노드 루트 월드 위치 사본과 노드/선 대상 수집→카메라 범위 초기화→초기 구도 적용/범위 재계산→유효 카메라 핸들 확인→Timeline.Build→Intro.Prepare→준비 문맥 게시 순으로 연결한다. `ConfigureCameraBounds`를 먼저 호출해 `_hasBounds`를 만든 뒤 에디터 초기 구도를 적용한다. 기존 `PreviewFraming`은 범위가 없으면 반환하고 구도가 바뀌면 내부에서 범위를 재계산하므로 이 선행 조건을 유지한다. 플레이어 보정과 시작 snapshot 보관은 그 이후 MI4의 BeginTour에서 수행한다. 플레이어 빌드는 기존 구도와 범위만 사용한다. 핸들이 아직 없으면 Pending이며 IsLoading=false를 기다리지 않는다. 기존 AfterLoad 대여가 준비보다 앞선다는 계약을 유지한다.
- [x] **정상 대기와 취소 분리:** `TryPrepareEntry`의 결과와 대상 인수 여부를 별도로 보관한다. 정상 Pending에서는 `EntryPreparationVersion`과 Intro의 Preparing 상태를 유지하고 `CancelEntryPreparation`/Intro.Cancel을 호출하지 않는다. 이번 시도에서 인수하지 못한 대상만 finally에서 복원하고 `_preparing`을 풀어 같은 버전으로 재시도한다. 기존 `!adopted && targets != null`만으로 전체 취소하는 finally 분기는 결과별 정리로 바꾼다. 실제 취소·문맥 상실·실패에서만 해당 준비를 무효화한다. 대기 중 복원 자체가 실패하면 오류 경계에 보고해 Failed로 처리하며 정상 Pending으로 숨기지 않는다.
- [x] **준비 재진입:** 같은 표시를 재사용해 재시도할 때 이미 숨겨진 상태를 원래 enabled로 잘못 저장하지 않도록 정상화한다. 현재·클리어·전방 밖 건물은 최종 표시 정책으로 복원한 뒤 Reveal한다. 핸들 준비 전에는 Intro.Prepare로 대상/자동 카메라 소유권을 인수하지 않는다. 이전 시도의 뒤늦은 정리는 새 시도의 대상·상태를 복원하거나 취소하지 않는다.
- [x] **재생 연결:** 기존 Scope Tick의 낙하 호출을 Intro.Tick으로 바꾼다. 준비 Complete는 두 대상의 숨김과 카메라 시작 준비까지만 의미한다. 연출 진행 중 RefreshDisplay가 최종 표시로 덮어쓰지 않도록 기존 준비 대기 경계를 전체 연출로 확장한다. 같은 맵 전투 복귀에서는 자동 왕복 snapshot을 적용하지 않는다.
- [x] **정리 연결:** 이탈/오류/Run·맵 교체/재그리기/재연결마다 준비 버전을 먼저 갱신한다. Intro 취소로 효과 반환·외형/선 복원·자동 카메라 반환을 마친 뒤 Scene의 표시 파괴/핸들 반환을 수행한다. Supersede의 이전 Scope는 새 표시를 Restore하지 않으며 미인수 대상 정리에도 소유 세대를 확인한다.
- [x] **정적 호출 추적:** 정상 진입, AfterLoad 지연, 로딩 준비 Cancel, 같은 표시 재사용, 준비 후 Scope 교체를 각각 추적해 Complete/Failed/Cancelled와 자원 귀속을 기록한다. 특히 대상 수집 후 핸들 미준비인 경우를 `Pending → Pending → 핸들 준비 → Complete`로 추적해 어댑터가 보관한 준비 버전이 유지되는지 확인한다. 같은 지점에서 실제 Cancel/오류를 낸 경로는 취소/실패로 끝나야 한다. 공용 Loading 구현 변경이 필요해지면 범위를 확대하지 말고 설계 판단으로 보고한다. **사용자 확인: U1·U3·U6·U8·U9.**

### MI7: 제거 가능한 입력 차단과 기존 팝업 재개

**Files:** 생성 `V/Entry/StageMapIntroInteractionPolicy.cs`와 `.meta`; 수정 §1의 MI7 파일 및 Factory/Scope 조립. 설계 §8을 구현한다.

**Interfaces:**
- `StageMapIntroInteractionPolicy(StageMapIntroPresentation intro)`, `bool BlocksInteraction { get; }`. 값은 Intro.IsPending에서만 파생하며 포인터/카메라를 직접 변경하지 않는다.
- Scope에 `CanInteractWithMap()`을 두어 기존 준비/오류/Layer/팝업/요청 조건과 `!policy.BlocksInteraction`을 조합한다. 기존 노드별 진입 조건은 유지한다.
- `StageMapViewInput.Create`의 `canInteractWithNodes` 뒤에 필수 `Func<bool> canInteractWithMap`을 추가한다. `StageMapInteraction`에도 같은 delegate를 주입한다. `StageMapCameraFramingReceiver` 생성자 마지막에 `Func<bool> canInteractWithMap`을 추가한다. Factory에서 Policy를 만들고 Scope 생성자에 Intro 다음 인수로 전달한다.

- [x] **일반 입력:** 노드 누름·뗌·최종 제출, 배경 드래그 시작/이동, 휠에서 새 가드를 확인한다. 차단 진입 시 기존 포인터/드래그를 한 번 정리한다. 차단 중 새 누름은 Blocked로 남기고 해제 뒤 뗌만으로 진입/드래그를 시작하지 않는다. 차단된 휠은 버린다.
- [x] **호버 정리:** `TickHover`의 기존 배경 드래그 보존 목적의 분기를 정리한다. 임시 연출 차단 동안에는 hover만 지워 Blocked 소유권을 유지하며, Layer 이탈/팝업/설정 전환 등의 기존 전체 Cancel 의미는 유지한다. 매 프레임 ClearPointer로 Blocked를 지우지 않는다.
- [x] **에디터 우회:** Alt 누름/이동/회전/돌리/휠과 일반 Tick의 PreviewFraming 적용에는 조작 가능 여부/자동 소유 경계를 적용한다. MI6의 초기 구도 준비 호출은 사용자 입력 경로와 분리하고 `CanInteractWithMap()`이나 아직 완료되지 않은 `IsPrepared`를 선행 조건으로 삼지 않는다. 대신 최신 Run/맵/준비 버전과 카메라 소유권을 확인하며 자동 왕복 중 재적용하지 않는다. 차단 중 Alt gesture도 완료 뒤 이어 시작되지 않게 하고, 구도 준비/연출은 사용자 설정 파일·EditorPrefs 값을 쓰지 않는다. 설정 창 열기/닫기는 기존 Common 입력 경로를 유지한다.
- [x] **완료 후 재개:** 보류 상점/회복 요청은 대기 동안 보관하고 전체 완료 뒤 최신 Run/맵/현재 방문을 재확인하여 한 번 복원한다. 기존 순서인 보류 복원→증강/선택 UI→호버를 유지한다. Request/선택 예약을 단순히 지우지 않는다.
- [x] **정책 제거 경계 확인:** 임시 차단 판단이 Policy와 조립에만 있는지 확인한다. 원래 준비/오류/팝업 조건이나 카메라 단일 소유권까지 제거하지 않는다. 후속 기능은 정책과 자동/수동 인계 계약을 바꿀 수 있으며 Drop/Link/Tour 내부에 입력 서비스 잠금 호출을 넣지 않는다. **사용자 확인: U4·U5·U9.**

### MI8: 구조 검토·문서·Phase 보고

**Files:** §1의 MI8 문서. 게임 코드 변경은 MI1~MI7의 미충족 계약을 고치는 범위에 한정한다.

- [x] **정적 대조:** §3의 확인을 수행하고 결과/미확인을 실행 기록에 남긴다. 설정·컴파일 참조·소유 순서·에디터 조건부 컴파일 분기를 대조하되 MSBuild/테스트/Unity를 실행하지 않는다.
- [x] **원칙 검토:** §4의 SSOT·SOLID 5원칙·JMH/FW·변경 독립성 표를 실제 최종 코드와 대조한다. 기존 코드 전체를 범위 없이 리뷰하지 않는다.
- [x] **사용법/공유:** 가이드에 Map Intro 설정, 높이 20 유지, Wave 제거, 최초 이력, 임시 입력 차단, U1~U9를 반영한다. 선행 계획은 과거 증거로 보존하고 새 현행 안내에 이 계획을 연결한다. Factory/로딩 준비/카메라 소유권 공유 메모를 작성하고 **미전달**로 표시한다.
- [x] **Phase 보고:** 변경 파일·정적 확인·미재현 경로·Unity 확인 항목을 기록한다. **컴파일 미확인**을 명시하고 사용자에게 U1~U9를 인계한 뒤 멈춘다. 사용자 실행 결과를 받을 때 해당 항목만 갱신하며 코드 작성 완료와 사용자 인수를 합치지 않는다.

## 3. 검증 방법과 사용자 Unity 확인

### Codex 정적 확인

아래 명령은 저장소 루트 PowerShell 기준이다. 검색의 일치/불일치를 확인하는 작업이며 빌드/시험 실행을 대신하지 않는다.

| 확인 | 방법 / 기대 결과 |
|---|---|
| 이전 식별자 제거 | `rg -n -e StageNodeDropHistory -e WaveSpreadSeconds -e waveSpreadSeconds Assets/ProjectL/Script Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView`. 대상 운영 코드/에셋에서 일치 0건(exit 1)이 기대값 |
| 설정·호출자 일치 | `rg -n --glob '*.cs' -e StageMapIntro -e StageNodeDropData -e StageNodeDropPresentation -e StageMapViewData Assets/ProjectL/Script Assets/Tests` 후 변경 생성자, SO target-typed new, Editor 분기의 모든 호출을 대조. 시그니처 잔존 불일치 0건 |
| 자원/시계 소유 | 변경 코드의 `sharedMesh`, `Destroy`, `Dispose`, `StopAll`, `Customize`, `Time`, `_elapsed` 사용을 `rg -n`으로 확인. 원본 메시 쓰기/직접 대여 자원 파괴/두 번째 카메라 대여/하위 누적 시계 없음 |
| 파일/GUID | 새 C#마다 `.meta` 존재, 이력 파일 이동 전후 GUID 동일, 기존 에셋/씬/프리팹 참조 손실 없음. SO의 높이 20 및 나머지 기존 튜닝 보존 |
| 범위/문법 형식 | 변경 파일만 `git -c core.autocrlf=false diff --check -- <변경 파일 목록>` 수행하여 출력 0건. untracked 파일은 별도로 UTF-8/LF·후행 공백·충돌 마커 확인. 승인 범위 밖 변경은 기존 변경과 구분 |

정적 대조에서 보호 영역 또는 다른 작성자의 시험 수정이 필요하면 사용자 규칙에 따라 먼저 확인한다. 새 검증 스크립트·테스트 도우미를 운영 프로젝트에 만들지 않는다.

### 사용자 확인 U1~U9

확인은 사용자가 Unity에서 수행한다. 기존 Boot 설정과 운영 챕터/카메라 구도 도구를 사용하고, 값 변경 전 운영 값을 기록해 확인 뒤 복원한다. 관찰하기 어려운 예외는 **미재현**으로 남기며 확인을 위해 새 버튼·시험 코드를 추가하지 않는다.

| ID | 확인 방법 | 기대 결과 |
|---|---|---|
| U1 기본·분기·합류 | 새 Run의 챕터 1, 다음 챕터 2에 최초 진입. 분기/합류 구간을 보며 필요하면 설정 창으로 정지 | Reveal 전 완성 건물/선 노출 없음. 플레이어부터 분기 동시 전개, 합류 노드/효과 한 번, 선 끝 도착=착지. 카메라 복귀와 효과 정리 전 조작 불가 |
| U2 점선·메시 | 같은 선의 25/50/75/100%에서 설정 창으로 정지해 위치/간격 비교. 대각선·긴/짧은 선·회전/비균일 스케일 맵도 확인. Unity Inspector/Profiler로 runtime Mesh와 참조 관찰 | 이미 나타난 점선 불변, 빈 구간에서도 시간 진행, 잘못된 컬링 없음. 완성/취소 후 원본 메시 복원, 프레임 종료 뒤 임시 메시 누적 없음, 원본 메시/머티리얼 변경 없음 |
| U3 카메라 | 에디터 구도 도구의 작업/확정 구도가 씬 기본값과 다른 상태에서도 최초 진입. 플레이어 구도→보스 정지→복귀와 첫 드래그·휠 확인. 작성 구도가 범위를 넓힌 경우와 회전/스케일 맵도 확인 | 입력 차단 중에도 로딩 준비에서 초기 구도가 적용됨. 플레이어와 보스가 각 구간에 보임. 기존 각도/배율 유지, 초기 구도 적용과 플레이어 보정을 마친 시작 구도로 정확히 복원, 뒤늦은 구도 적용/수동 조작 인계에 튐 없음 |
| U4 입력 | 시작 대기·낙하·복귀 중 노드 클릭/배경 드래그/휠/호버/Alt 이동·회전·돌리·휠 시도. 누른 채 완료를 기다린 뒤 뗌 | 모든 맵 조작/호버 차단, 잔여 누름으로 동작 없음. 완료 뒤 새 누름·휠만 정상. 설정 입력은 정상 |
| U5 정지 | 시작 대기·낙하/선·보스 정지·복귀·효과 대기 각각 설정 창 열고 닫기. 가능하면 정지 중 씬 종료 | 세 연출과 효과 상한이 함께 멈추고 이어짐. 정지 중 취소는 즉시 자원 정리 |
| U6 이력·부분 구간 | 같은 Run의 전투/상점/회복 복귀 및 재그리기, 새 Run, 다음 챕터, 개발 중간 노드/현재 보스 첫 진입. 시작 전/후 이탈을 가능한 도구로 재현 | 같은 챕터 생략, 새 Run/챕터 재생. 중간 노드부터 전방만 연출, 현재 보스는 최종 표시. 시작 전 재시도/후 생략, 일반 복귀 카메라 정책 유지 |
| U7 설정 경계 | 전체 꺼짐, 낙하만 꺼짐, 높이/시간 0, 정지/복귀 0을 각각 확인. 전개 시간을 짧게 해 P가 생기는 경우도 확인하고 운영 값 복원 | 설계 §9와 일치. 전체 꺼짐은 이력/효과/왕복 없음, 낙하만 꺼져도 선/왕복 진행. 도착 시각의 즉시 착지, 초기 대기 전 낙하 없음. 높이 운영 값은 20 유지 |
| U8 수명·실패 | 기존 도구로 씬 이탈/재연결/재그리기, 대상 수집 후 핸들 준비 지연과 그 대기 중 실제 취소/오류, 큰 프레임 지연/효과 반환 지연을 재현 가능한 만큼 확인 | 정상 대기는 같은 준비 버전으로 이어져 핸들 준비 후 Complete, 실제 취소/오류는 Cancelled/Failed. 노드/선 복원·효과 반환·카메라 소유 해제, 늦은 이전 세대 쓰기 없음. 새 효과가 생성 즉시 상한으로 회수되지 않음. 정리 실패가 정상 완료/입력 해제로 바뀌지 않음 |
| U9 기존 동작·DB | 완료/생략 뒤 상점·회복 보류 팝업, 증강/선택 예약, 노드 선택, 호버 색/폭, 카메라 구도 도구, 여러 번의 FallingRocks 크기 확인. SO 변경 후 기존 `DataBaseAssetTests` 사용자 실행 | 유효한 팝업 한 번 복원, 기존 기능 정상, 파티클 크기 누적 없음. DB 에셋 검증 결과는 실행 증거를 별도로 기록 |

## 4. 구조 원칙과 변경 범위 인수 기준

원칙 원본은 [적용 기준 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [JMH 개발 지침 §2·§23·§26~28](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)이다.

| 기준 | 최종 코드에서 확인할 내용 |
|---|---|
| SSOT | 진행=Buffer, 배치=기존 NodeView/Layout, 예약=Timeline, 공통 경과/완료=Intro, 이력=History 하나, 카메라 핸들/offset=CameraDrag, 점선 패턴=원본 메시/머티리얼 |
| SRP | 표현 계산이 각 부품에 있고 Scope/Factory에 정점/낙하/카메라 수식이 없음 |
| OCP | StageMap의 부품/기존 연결 지점으로 확장하며 공용 Loading/Camera/Render에 StageMap 분기를 추가하지 않음 |
| LSP | 로딩 Complete는 준비 완료, Cancel은 제출하지 않음, 반복 정리 안전, 생략/완성의 최종 표시 계약 유지 |
| ISP | 선 대상·자동 카메라 조작의 좁은 계약과 시간표 일부만 하위 부품에 전달; 전체 Services/Buffer 참조 없음 |
| DIP | Factory가 생성/주입하며 기존 서비스 공개 API·Global Context 사용; 내부 Store/Pool 접근 없음 |
| JMH/FW | 내부 부품 새 서비스 등록 없음. FW-004 내부 소유권, FW-011 Scope/Handle 반환, FW-012 기존 효과/DB 계약, FW-016 바인딩 선행 유지. 직접 만든 임시 Mesh만 View가 파괴 |
| 기능 변경 독립성 | 선 표현 수정은 MI2, 낙하 모양은 MI3, 카메라 보간은 MI4, 임시 차단 교체는 MI7에 머물 수 있는지 확인. 연결 설정은 Factory에 모으고 공통 시간표/이력/취소 변경만 MI5~MI6 통합 회귀가 필요함을 기록 |

## 5. 실행 기록

- 2026-11-06: 사용자 요청으로 계획 작성. 보완 설계의 점선 고정 노출·높이 20, 단일 완료 조건·임시 입력 차단, 로딩/자원/이력/취소 경계를 MI1~MI8에 배정했다.
- 계획 자체 대조: 설계 §4~§5→MI1·MI3·MI5, §6→MI2·MI4, §7→MI5·MI6, §8→MI7, §9→MI1·MI5, §10→MI8/§4, §11→U1~U9. 현재 코드의 호출 계약과 설계 범위를 읽어 계획을 작성했으며 실행 검증은 하지 않았다.
- 2026-11-06 후속 검수 2건을 사용자 요청으로 문서에 보완했다. MI6에 정상 Pending의 준비 버전 유지·미인수 대상 복원과 실제 취소/실패의 분리를 명시했다. MI4·MI6·MI7에는 범위 선행→초기 구도 적용/범위 재계산→시작 구도 보관 순서와 준비용 적용/사용자 입력 차단의 분리를 명시했다. Review Focus·U3·U8도 같은 계약으로 맞췄다. 구현 수정이나 실행 확인을 뜻하지 않는다.
- 계획 작성 시점에는 MI1~MI8 미착수였으며 문서만 변경했다. 이후 사용자 승인으로 수행한 Phase 1 결과는 아래 진행/완료 기록을 따른다.
- 현재 다음 단계: 사용자 Unity 확인 U1~U9. Phase 1 범위에서 멈추며 선행 낙하 기능의 사용자 확인을 새 선/카메라/입력 연출 인수로 간주하지 않는다.

### Phase 1 진행 기록

- 2026-11-06 사용자 Phase 1 구현 지시 수신. 작업 기준 HEAD `88377dec`, 기존 `feature/Hojin/11_03_StageSelect` 작업 폴더에서 이어서 진행한다. 선행 낙하와 사용자 에셋 튜닝 및 FMOD/FBX 변경을 보존한다. 사용자 규칙에 따라 시험 코드/빌드/Unity 실행/커밋은 제외하고 이 절을 실행 기록으로 사용한다.
- 계약 사전 대조: MI1의 NodeTiming/LinkTiming/CameraTiming을 MI2·MI3·MI4가 각각 소비하며, 세 부품의 Prepare/Tick/Cancel/IsComplete를 MI5가 조합한다. MI5의 IsPending/IsPrepared와 Cancel 인자를 MI6·MI7에 연결한다. 준비 Pending과 실제 취소의 구분 및 초기 카메라 구도 선행 조건은 보완된 MI6·MI7을 따른다.
- MI1~MI4 코드 작성: View 설정·시간표, 원본 정점/UV를 보존하는 선 메시 절단, 공통 시각 낙하·실제 착지 기준 효과 상한, 동일 핸들 자동 카메라 소유권/왕복을 반영했다. `WaveSpreadSeconds` 운영 참조 0건, 정점/UV 쓰기는 임시 메시뿐임을 검색으로 확인했다. MI5 이후 조립 변경 진행 중이며 이 기록은 Phase 완료/컴파일 확인이 아니다.
- MI5~MI7 코드 작성: History 이름/GUID 이전, 전체 연출 상태/시계, Factory 주입, 정상 Pending과 취소 분리, 범위 선행/초기 구도 적용, 일반·에디터 입력 가드를 연결했다. 기존 생성자 호출처와 로딩 어댑터의 버전 계약을 대조했다. 이미 준비된 맵의 매 Tick은 대상/문맥 캡처를 다시 만들지 않게 준비 시도 메서드와 분리했다. 최종 정적 검수와 MI8 문서 인계 진행 중이며 컴파일 미확인이다.

### Phase 1 완료 기록 — 코드·정적 검토, 사용자 인수 대기

- MI1~MI8의 코드·설정·문서 작업을 마쳤다. 새 시간표/설정, 선 메시 소유, 낙하 소비자, 카메라 자동 조작, Intro/History, Scope/Factory 조립, 일반/에디터 입력 정책을 연결했다. 정확한 파일 목록은 §1을 따른다. 기존 StageNodeDropTarget/Effects·LoadingPreparation·공개 Factory 계약은 재사용했다.
- 운영 값은 높이 **20**, 개별 낙하 0.8초를 유지한다. Map Intro는 켜짐·시작 0.6·전개 7·보스 정지 0.9·복귀 2.2초다. Wave 필드만 제거하고 기존 다른 튜닝은 보존했다.
- 정적 검색: 운영 코드/설정의 `StageNodeDropHistory`·`WaveSpreadSeconds`·`waveSpreadSeconds` 잔존 0건. 변경 생성자 및 SO target-typed new, Editor 조건부 호출을 대조했다. 새 시험 코드나 기존 시험 변경은 없다.
- 자원 대조: 경과 시간 누적은 Intro 한 곳, 임시 Mesh/배열 생성은 선 준비 시점, sharedMesh 쓰기는 임시 연결/원본 복원뿐이다. 새 카메라 대여·원본 메시/Material 쓰기·빌린 파티클 직접 파괴는 없다. 기본 카메라 offset/핸들 소유자를 유지한다.
- 파일 확인: 변경 C# 24개의 UTF-8/LF·후행 공백·충돌 마커·동반 meta를 확인했다. History 이동 GUID는 `c4dcbc6c579d4727ae59aa366c9c9ae0`로 유지했다. 선 원본 에셋·보호 영역은 수정하지 않았으며 기존 FMOD/FBX 변경도 보존했다.
- 최종 형식 확인: 변경 C# 24개·문서 4개의 검사와 `git -c core.autocrlf=false diff --check`에서 문제 없음. 해당 C# GUID 24개는 Assets 전체에서 각각 한 번만 정의된다. Unity YAML의 기존 빈 필드 뒤 공백은 유지했으며 변경 줄의 공백 오류는 없다. 가이드 전체 링크 검사에서 이번에 수정하지 않은 §2의 옛 에셋 링크 3개가 누락된 사실은 별도 기존 사항으로 구분한다.
- 별도 읽기 전용 최종 검수: 확정 수정 지적 **0건**. 시간표/착지 효과, 고정 정점·UV, 정상 Pending의 버전 유지, 초기 구도 선행, 입력 차단·수명 경계를 대조했다. SSOT·SOLID 5원칙·JMH/FW·기능 변경 독립성에서 구체적 위반을 발견하지 않았다. 선/낙하/카메라/임시 정책 변경은 각 부품에 머물며 공통 수명 변경은 Intro/Scope 통합 확인이 필요하다.
- 가이드 §3.1을 통합 연출 기준으로 교체하고 [Factory·로딩·소유권 공유 메모](../handoffs/2026-11-06-stage-map-intro-framework-share-Hojin.md)를 작성했다. 공유 메모는 **미전달**이다.
- **컴파일 미확인.** MSBuild·Unity·시험 실행·커밋·push를 하지 않았다. 실제 화면의 점선 불변/구도, Unity 콜백 재진입과 자원 반환, U1~U9 실행 결과는 판단을 보류한다. 코드 작성 완료와 사용자 인수는 별개다.

#### 예약·도형 정적 대조

다음 값은 구현 수식과 소스 계약을 읽어 대조한 값이며 실행 시험 결과가 아니다. 시간은 초, qMin=0.2이며 별도 표시가 없으면 H=0.6/R=7/D=0.8이다.

| 조건 | 계산과 코드 경로 |
|---|---|
| 기본값 | P=0, T0=0.6, 첫 도착=2.0, 첫 낙하 시작=1.2, 보스 도착=7.6, 복귀 종료=10.7 |
| R=1 | P=0.6, T0=1.2, 첫 도착=1.4, 첫 낙하 시작=0.6. 시작 대기 앞의 낙하 없음 |
| 높이 0 또는 시간 0 | 유효 D=0, P=0. DropStart=Arrival에 즉시 착지·효과 |
| 낙하 꺼짐 | 유효 D=0, HasDrop=false. 선/카메라는 동일 시간표, 낙하 효과 없음 |
| 실제 낙하 대상 없음 | qMin 미설정이면 P=0. Drop은 완료 상태, 선/카메라 대상이 있으면 계속 전개 |
| 현재 보스·출발선 없음 | 빈 시간표로 생략. 보스 거리 나눗셈 전에 반환 |
| 일반 경로·합류 | 보스 축 거리는 양수 확인 후 나눔. 한 노드의 Arrival은 q 한 번으로 결정하며 진입선 수와 무관. Link의 종료는 목적지 Arrival |
| p=0.25/0.5/0.75/1 | 운영 Quad의 끝 y=-0.25/0/0.25/0.5, 끝 UV.x=0.25/0.5/0.75/1. 이미 나타난 y=-0.3의 UV.x=0.2는 네 경우 모두 같음. p=1은 원본 복원 |
| 선 완성·취소·재사용 | 완성/취소에서 원본 연결 후 임시 Mesh 해제. 새 대여 세대로 이전 대상 쓰기를 막고 새 준비 전에 원본으로 정상화 |

#### 상태·수명 정적 추적

| 경로 | 코드에서 확인한 결과 |
|---|---|
| 대상 수집 후 핸들 대기 | Pending → Pending 동안 버전 유지·미인수 대상 복원. 핸들 준비 뒤 같은 버전으로 Intro에 인수하여 Complete |
| 준비 대기 중 실제 Cancel/오류 | Cancel은 버전 무효화, 오류는 기존 FaultBoundary 보고. 복원 실패도 Failed이며 정상 Pending으로 숨기지 않음 |
| 준비/초기 대기 중 취소 | 최초 표시 변화 전에는 이력 없음. 즉시 자원 정리, 다음 준비에서 재시도 |
| 첫 변화 직후 취소·이미 본 챕터 | 시작 이력 유지. 다음 준비는 최종 표시로 생략하며 자동 왕복을 새로 시작하지 않음 |
| 새 Run·전체 꺼짐·낙하만 꺼짐 | 새 Run은 이력 교체. 전체 꺼짐은 새 이력/효과/왕복 없음. 낙하만 꺼짐은 선/카메라 시작에 이력 기록 |
| 큰 deltaTime·동시/합류 착지 | 노드별 Landed를 효과 호출 전에 기록. 실제 생성 Tick을 마지막 효과 시각으로 삼아 새 효과의 즉시 상한 회수 방지 |
| 효과 호출 중 취소·늦은 이전 Scope | 외부 호출 뒤 epoch/문맥 재검사. 이전 효과/선/카메라 세대는 새 소유 대상에 쓰지 않음. 실제 Unity 재진입은 U8 미확인 |
| 설정 창 정지·정지 중 취소 | 공통 시계는 정지, Cancel은 scaled 시간과 무관. 파티클의 실제 정지/반환은 U5·U8 확인 필요 |
| 차단 중 누름·완료 후 뗌 | Blocked 누름 유지, 일반/Alt 입력 가드 적용. 기존 드래그는 정리하고 release만으로 새 조작을 시작하지 않음 |
| 복귀·보류 팝업 | 시작 offset 복원 후 자동 토큰 해제. 전체 완료 뒤 최신 진행을 확인하여 보류 복원 → 증강/선택 → 호버 순서 유지 |

U1~U9는 모두 **사용자 확인 대기**다. 선행 낙하 기능의 정상 재생 확인을 위 경로의 실행 증거로 옮기지 않는다.
