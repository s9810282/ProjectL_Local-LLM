# Stage Node Hover Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. User rules override test/commit steps.

**Goal:** 건물 호버 윤곽선과 진입 가능한 대상의 연결선 강조를 SO로 조절한다.

**Architecture:** 기존 입력·Core 판정·Render Scope를 재사용한다. 공유 스타일은 View SO, 읽기 어댑터·호버 피드백·표시는 각각 독립 클래스로 둔다.

**Tech Stack:** Unity 6000.0.73f1, C#, 기존 SelectOutline/ColorSystem.

**Spec:** `docs/superpowers/specs/2026-11-05-stage-node-hover-design-Hojin.md`

## Global Constraints

- Core 파일·카메라·기존 수정 중인 입력 파일을 수정하지 않는다. 공개 읽기 API 사용은 사용자 승인됨.
- 테스트 코드/도우미·MSBuild·커밋·push 없음. 컴파일·Unity 재생은 사용자 확인.
- 새 .cs마다 .meta, UTF-8/LF. 동작 설정의 원본은 스타일 SO.

## Review Focus

- 빠른 대상 변경: 이전 윤곽선·선이 남지 않는다.
- 정지 포인터의 진행 변경: 캐시를 무효화하고 상태를 다시 표시한다.
- 팝업·이동·표시 교체: 강조를 해제하고 Scope를 부모 파괴 전에 반환한다.
- 복수 메시 건물: 모든 활성 메시가 같은 설정으로 강조된다.
- 시간 0·잘못된 SO 값: 즉시 전환/명확한 설정 오류로 각각 처리한다.

## Phase 1 — 구현과 정적 검토

### Task 1: SO와 운영 설정

- [x] `Data/View/Table/StageNode/Item/StageNodeHoverStyleSO.cs`·`StageNodeHoverStyleData.cs` 추가.
- [x] `StageMapViewDataSO`·`StageMapViewData`에 스타일 연결, 운영 `StageMapView_Default.asset` 참조 연결.
- [x] 신규 .meta/GUID·설정 유효성 확인.

### Task 2: 표현과 수명

- [x] `StageMap/StageHighlightTransition.cs`·`StageNodeOutlineView.cs` 추가. 공용 Outline Scope 반환 계약 준수.
- [x] `StageNodeView`의 보이는 건물 조회, `StageLinkView`의 연결선 전환, `StageMapViewDisplay`의 표시/해제 연결.

### Task 3: 판정과 입력 연결

- [x] `StageNodeAvailabilityReader.cs`·`StageNodeHoverFeedback.cs` 추가. Core 호출은 읽기 어댑터 한 곳.
- [x] `StageMapViewSystem`·`StageMapViewServices`·`StageMapViewScene`에서 의존성 전달.
- [x] `StageMapViewInput`·`StageMapViewScope`에서 Tick/입력 해제 연결. `StageMapInteraction`은 변경하지 않음.

### Task 4: 인계

- [x] 변경 파일과 서비스/자원/예외 경로 검토, `git diff --check`, SO 참조 확인.
- [x] Unity 수동 확인 항목과 컴파일 미확인 보고. 이 Phase 뒤 추가 기능은 진행하지 않는다.

## 실행 기록

- 2026-11-05: 기존 작업 트리에서 사용자가 요청한 기능을 수행한다. 기존 카메라/휠 입력 수정은 보존한다. Core 공개 API 조회 예외 승인 확인.

- 구현·정적 검토 완료: 신규 SO/스타일 데이터, 공용 윤곽선 Scope, 연결선 전환, 조회 어댑터와 입력/Tick 수명 연결. 기존 카메라·입력 수정과 Core 파일은 변경하지 않음.
- 독립 읽기 전용 검토에서 현재 운영 경로의 수정 필요 오류 없음. 0초 전환·빠른 대상 변경·부모 파괴 전 반환·캐시와 클릭 판정 분리 확인.
- 신규 6개 C#의 .meta와 스타일 에셋 .meta 확인, 7개 신규 GUID가 각각 한 메타에만 존재. 설정 SO → 스타일 에셋 → Script GUID 연결 확인. 소유한 27개 파일 UTF-8/LF 정리, git diff --check 통과.
- 컴파일 미확인. Unity 재생·EditMode 미실행. 설계의 Unity 확인 항목은 사용자 인수 대기.
- 공용 SelectOutline은 머티리얼 하나를 사용하는 메시를 기준으로 한다. 현재 운영 건물 프리팹은 이 구성이다. 향후 다중 submesh 모델은 공용 렌더 계약 보완을 먼저 검토한다. 기존 Pool의 PropertyBlock 반환 초기화는 별도 공용 과제이며, 이 기능은 대여 시 사용하는 세 속성을 모두 설정한다.

## Phase 2 — 2026-11-06 사용자 요청: 가능한 노드에만 기본 파란 윤곽선

- [x] 스타일 SO/Data/운영 에셋에 `Idle Outline Color`·`Idle Outline Width` 추가.
- [x] 가시 노드 ID 조회와 노드별 가능 판정 캐시 연결. 맵 표시 시 명시적으로 문맥을 갱신하여 첫 호버 전부터 표시.
- [x] 윤곽선 표시를 가능 기본/가능 호버/불가능 호버/해제 네 상태로 연결. 연결선은 호버 규칙 유지.
- [x] 정적 diff·설정/호출 경로 검토. Unity 기본 파랑→호버 금색→파랑 복원, 불가능 노드 무표시→회색 호버, 진행 후 갱신 확인 인계. 컴파일 미확인.

- 2026-11-06 구현·독립 정적 검토 완료. 첫 표시, 진행 변화, 기본/호버 폭 0, 금색→파란색·회색→없음 복원, 연결선 호버 한정 경로를 확인했다.
- 기본값은 파란색 RGB(0.2, 0.55, 1), 두께 0.008. SO의 Available Node Idle에서 변경한다. Play 재시작 후 적용.
- 이번 변경 12개 파일 UTF-8/LF 확인, git diff --check 통과. Core·시험 파일 변경 없음. 컴파일 미확인·Unity 재생 미확인. 사용자 확인 항목에 DataBaseAssetTests 포함.
