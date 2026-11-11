# StageMap 카메라 동행·복귀 정렬 연결 공유 — 2026-11-07

- 상태: **미전달**. Phase 1 코드·정적 검토·문서 인계 완료. 독립 검토 지적 2건을 보완했다(무효 이동 표시 정리, 준비 후 수락 관찰). **컴파일 미확인**, Unity U1~U8 확인 대기.
- 기준: [설계](../specs/2026-11-06-stage-map-camera-focus-design-Hojin.md), [구현 계획](../plans/2026-11-07-stage-map-camera-focus-plan-Hojin.md), [사용 가이드 §12](../viewDocs/stage-map-guide-Hojin.md#12-노드-이동의-카메라-동행과-복귀-정렬--2026-11-07).

## 연결 내용

| 경계 | 반영 내용 |
|---|---|
| 생성·등록 | 기존 StageMapSceneFactory가 CameraFocus를 생성해 Scope에 주입. 새 서비스/Module/Layer 등록 없음 |
| 카메라 | 기존 StageMapCameraDrag의 Catalog lease 하나 사용. Intro와 Focus가 배타 자동 세대를 공유하고 수동 입력과 겹치지 않음 |
| 이동 시간 | 동일 ChessPieceMotionSettings.TotalSeconds와 기존 Delay.CurrentTime 사용. StageMapMovementDelay는 시계를 그대로 전달하고 한 이동의 Tick/End 콜백만 문맥 검사 |
| 표시 완료 | 마커 실제 완료와 카메라 최종 적용 뒤 완료. Departure는 Submit/Pending까지 복원 토큰 유지, CompleteAccepted에서 확정 |
| 로딩 | StageMapLoadingPreparation.Begin의 isCovered와 준비 버전 전달. Intro가 준비 때 생략된 CoveredSceneEntry에서만 정렬 후 Complete |
| Editor | SceneRoot의 비직렬화 편집 연결은 자기 Scope만 해제. Inspector/Alt는 같은 Access로 현재 구도에서 편집하며 자동 이동·Repaint가 저장하지 않음 |

공용 Camera/Loading/Delay·ChessPieceMotion·Core·Buffer·FieldCamera·Architecture·전투 View는 수정하지 않았다. 기존 Catalog Handle Customize와 반환 경로를 사용한다. 확정 진행 원본은 Buffer, 선택 전 목적지는 Departure, 렌즈/offset은 CameraDrag, 설정은 SO다.

## 데이터와 인수

운영 SO에는 Camera Focus Enabled=true, Player Viewport Y=0.35만 추가했다. 기존 낙하 높이 20과 사용자 시간 조절값은 유지한다. 기존 생성자 호출부는 설정 변환과 Factory/Scope 조립에서 연결했다. 새 C#에는 `.meta`를 동반한다.

확인 잔여는 계획 U1~U8이다. 전투/비전투 전 구간 동행, 출전 취소·Pending·수락, 복귀 가림 첫 화면, 최초 Intro 보존, Inspector 직접 편집, 새 Run/준비 버전의 이전 콜백 차단, 설정 비활성화 및 기존 DataBaseAssetTests를 사용자가 Unity에서 확인한다. 자동 시험·컴파일·Unity 실행 결과는 이 문서에 성공으로 기록하지 않는다.
