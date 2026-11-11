# StageMap 노드 낙하의 Factory·로딩 준비 연결 — 2026-11-06

- 작성 목적: Framework/View 작업자에게 StageMap 조립·씬 준비 연결 변경을 공유하기 위한 메모.
- 전달 상태: **미전달**. 외부 메시지 발송 없음.
- 범위: [노드 낙하 계획](../plans/2026-11-06-stage-node-drop-plan-Hojin.md) Phase 1. 컴파일·Unity 재생 미확인.

## 조립 변경

`StageMapViewSystem.PostBindService`가 기존 `Buffer.RunProgressReadSink`, `Global.LodingServiceCommand.LodingService`, `Global.RenderServiceCommand.CoreGroup.EffectSystemServices`를 받는다. 최초 재생 History와 전용 Effects는 이 연결 수명에 한 번 생성하여 `StageMapViewServices`로 전달한다. 새로운 루트 서비스·키·Group 등록은 없다.

기존 `StageMapSceneFactory.Create` 서명은 유지한다. 내부에서 씬별 `StageNodeDropPresentation`을 만들고 Scope에 주입한다. `IStageMapSceneFactory`에는 `CreatePreparation(StageMapViewScope scope, RuntimeFaultBoundary faults)`를 추가했으며 새 `StageMapLoadingPreparation`을 반환한다. 준비 객체는 Scope와 준비 세대만 잠시 보유하고 terminal 상태에서 참조를 놓는다.

## 로딩·해제 순서

Stage의 기존 EnterLoaded(100)·ExitLoaded(-100)·AfterLoad 카메라 연결(100)은 유지한다. `RegisterScenePreparation(SceneIndices.Stage, ...)`만 추가했다. 준비와 일반 Scope Tick은 같은 `TryPrepareEntry`를 사용한다.

Run·맵 생성 전에는 Pending이며, 준비된 맵에서 현재 노드가 누락되면 기존 오류 경계에 보고한다. 배경→노드 배치/숨김→현재 마커→카메라 범위→낙하 준비를 마치면 Complete다. **준비는 IsLoading 종료나 낙하 완료를 기다리지 않는다.** Reveal 뒤 별도의 연출 Tick이 IsLoading을 확인하여 시작한다.

이탈·오류·재그리기는 연출 취소/효과 반환/원래 외형 복원을 노드·배경 정리 전에 수행한다. 준비 세대와 효과 소유 세대를 검사하므로 이전 준비 객체의 Cancel이나 이전 씬의 반복 정리가 새 연결을 건드리지 않는다. History는 씬 이탈로 지우지 않는다.

## 서비스 경로 대조

| 단계 | 기존 제공/신규 소비 |
|---|---|
| 생성 | `Factory/Runtime/Global/Render/RenderSystemFactory`의 `EffectSystem` 생성 |
| 내부 등록 | `EffectSystem.ExportServices`의 `IEffectSystemServices` 등록과 모듈 등록 |
| Group 보관 | `RenderCoreGroup.EffectSystemServices`가 내부 서비스 키로 조회 |
| 상위 제공 | 기존 Global → RenderServiceCommand → CoreGroup 경로 |
| 소비 | `StageMapViewSystem.PostBindService` → 전용 Effects → Factory의 씬 Presentation 주입 |
| 자원 반환 | 전용 EffectScope 하나 재사용, 현재 세대만 StopAll, 풀 반환을 확인한 뒤 자신이 만든 받침 Destroy |

공용 Loading/Render 구현·전투 본부 구현·Core/Buffer 구현·FieldCamera·Architecture는 변경하지 않았다. 기존 EffectScope에는 개별 완료 손잡이가 없으므로, 전투의 받침 반환 관찰과 동일하게 효과만 자식으로 둔 받침의 childCount가 0이 되었는지 확인한다. 빌린 파티클을 직접 Destroy하거나 콜백을 교체하지 않는다.

착지 받침은 맵 Transform 아래가 아닌 소유 씬의 독립 루트다. 월드 위치/회전은 착지 Pose로 정하고 스케일은 1로 유지한다. 맵 스케일을 상속하면 기존 EffectScope의 월드 보존 부모 변경과 풀의 local 유지 반환 때문에 파티클 크기 보정이 누적되는 것을 별도 리뷰에서 확인하여, Stage 쪽 받침 배치만 보완했다.

## 확인 인계

- 최초 맵 생성이 늦는 경우 준비 전 Reveal이 없는지, 로딩과 연출이 서로 기다리지 않는지.
- 취소·재연결과 자연 효과 반환/3초 상한에서 자원 수명이 유지되는지.
- 전체 사용자 확인과 실행 상태는 계획의 U1~U6 및 Phase 1 실행 기록이 원본이다.
