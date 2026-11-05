# 아티팩트 팝업 계획

- 작성자: 호진(이호진)
- 상태: 구현 완료(로비 4번 시설에서 운영). 남은 것은 사람의 마우스 Play, Player 성능 측정, 영혼 드랍 받침 수정, 외부 공유 전달이다(§2).
- 설계: [아티팩트 팝업 설계](../specs/2026-10-06-artifact-popup-design-Hojin.md)
- 정리: 2026-10-27 1부(팝업 계획·실행 기록)와 2부(공간 모델 교체)를 결과 요약으로 줄였다. 결정 D1~D19·Q1은 [설계 §3](../specs/2026-10-06-artifact-popup-design-Hojin.md#3-결정-기록)으로 옮겼다. 끝난 Task 단계·시험 수치·검증 로그 경로는 git `10914570`.

## 1 진행 요약

| 작업 | 내용 | 상태 | 확인 | 커밋 |
|---|---|---|---|---|
| Phase A 보드 초기화 | InGame `ArtifactBoardConfig`(4×4)·초기화 Job | 완료. 진입 등록은 LV5에서 Boot 준비로 대체 | 사용자 Test Runner 통과(10-05) | `5376b174` |
| Phase B 요청·Job | 요청 객체·스케줄러·편집/프리뷰 Job | 완료 뒤 SA P7에서 Buffer 번호 경로로 교체·삭제 | 사용자 Test Runner 통과(10-05) | `5376b174` |
| Phase C 팝업 표시 | SceneRoot·View·풀·3D 표현·씬 연결 | 완료 | 당시 EditMode·Boot smoke | `5376b174` |
| Phase D 입력 | Mapper·Interaction·배치/이동/반환/회전·우클릭 회수 | 완료 | 당시 EditMode. 물리 마우스 미확인 | `8b27457a` |
| D4 공용 오류 연결 | `InGameViewFaultCleanup` 단일 콜백 | 완료(SA P3.9, 사용자 승인) | 스테이지 맵 계획 | `7345109e` |
| U1·U2 UI 이전 | `ArtifactPopupUI`(UIBase)·`UIId.Artifact = 13`·씬 연결 | 완료. ESC 닫기는 같은 날 철회 | 당시 EditMode·Boot smoke. U2.4 사용자 Play 미확인 | `5fdfc852` |
| U3 문서·공유 | 화면 문서·현강님 공유 | 문서 완료, 전달 안 함 | — | `5fdfc852` |
| 배치면 정렬 | 안 B(`cellSpacing` 1.79·`modelScale` 0.8136) | 완료 | 실제 Play 화면 미확인 | `5fdfc852` |
| SA P3·P7 | Host/Screen → `IArtifactPopupDisplay`, 요청 객체 → `ArtifactViewBuffer` | 완료 | [스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md) | `7345109e` |
| 호버·문구 수명 | 호버 상세, `OutOfBounds`·`CellOccupied` 3초 숨김 | 완료 | 당시 자동 검증 | `5f27fb9b` |
| LV5 로비 이전 | StageSelect → 로비 4번 시설, Boot 보드 준비 | 완료 | [로비 계획](2026-10-11-lobby-plan-Hojin.md) | `3205df40` |
| View Scope P0~P5 | `LobbyViewScope`·`PopupViewScope`로 수명 정리 | 완료 | [View 구조 계획](2026-10-13-view-structure-plan-Hojin.md) | View 구조 계획 |
| 공간 모델 교체 A·B | `ArtifactSpace_final.fbx`로 배치판 교체, Dummy 5개 제거 | 완료 | 당시 배치 EditMode. Play 미확인 | `4d6d739d` |

## 2 남은 일

- [ ] 사람의 마우스 Play: 시설 도착으로 열기, 닫기 버튼, 호버 상세, 배치·이동·반환·회전, 충돌·범위 밖 문구, 재진입 뒤 보드·배치 유지. ESC가 팝업을 닫지 않고 설정만 여는지(옛 E1~E3·U2.4).
- [ ] Player 빌드 성능 측정(옛 E4): 열기·드래그·회전·닫기 구간의 CPU/GPU frame time·GC.Alloc·GC spike. 기기·CPU·해상도·VSync를 함께 적는다. GTX 1060 250~300 FPS는 참고 목표다.
- [ ] 영혼 드랍 세로 받침이 0.24 짧다(10-06 측정) → Blender 수정.
- [ ] 외부 공유 전달(옛 U3.3): 현강님(UIId 13·표시·수명 계약), 입력 담당(`Artifact/Close` 제거, `Common/OpenSettings` 유지), 전투 작업자(공용 오류 연결). 문서 준비를 전달 완료로 적지 않는다.
- [ ] Core 확인: 운영 규칙 에셋이 canonical effect compiler에 거절되는 문제(설계 §9).
- [ ] 보상 보드 확장(`ExpandBoard`) 연결은 보상 작업 때.

## 3 작업별 결과

### 3.1 Phase A~C (2026-10-05~06)

- A: `ArtifactBoardConfig` Data·SO·Table·키와 InGame 그룹 등록, 초기화 Job(Priority 2). 테이블을 직접 조립하던 천명님 시험 둘에 테이블 한 줄씩 더했다(Core 전달 문서 §5.7).
- 계획과 다른 점: 설정 조회는 `IInGameQuery`가 아닌 별도 `IHasArtifactBoardConfig`로 두었다(D7, 지금은 Boot 준비로 대체돼 진입 등록은 없다).
- B: 요청 객체·스케줄러는 SA P7에서 지웠다. 10-05 사용자가 A·B 시험 통과를 확인했다.
- C: 3D 표현(View DB `ArtifactPopupConfig`, 프리팹별 모델 풀, World Space 배경)과 StageSelect 씬 연결. 전체 배치판 프리팹을 칸마다 16개 만들던 것을 중앙 1개로 고쳤다.

### 3.2 Phase D 입력 (2026-10-06)

- Mapper·Interaction과 기존 슬롯·Layer를 이어 배치·회전 이동·목록 반환을 만들었다.
- 계획과 다른 점: 우클릭을 취소에서 회수로 바꿨다(사용자 10-06). 하단 보유 영역과 가려진 열기 버튼이 겹쳐 회수가 취소되던 것을 고쳤다(열린 팝업 차단 판정에서 열기 버튼 제외).
- 물리 마우스·키보드 조작과 Player FPS는 확인하지 않았다.

### 3.3 UI 이전 U1~U3 (2026-10-06)

- D1을 바꿨다(사용자 10-06, 현강님 UI 구조): `ArtifactPopupUI`(UIBase)에 버튼·목록·문구를 옮기고 `onClick`·이벤트 구독을 없앴다. `ArtifactItem.prefab`에 Button(Transition None, OnClick 없음)을 더했다.
- U4 ESC 닫기는 넣었다가 같은 날 사용자 요청으로 뺐다. 지금 ESC는 `Common/OpenSettings` 전용이다.
- 주의: `ArtifactPopupUI.cs.meta`를 `.cs`보다 먼저 만들어 Unity가 GUID를 새로 만들었다(`1708932e…`).
- 이때 만든 Host/Screen 계약과 UI의 Buffer·DB 직접 조회는 SA P3.10에서 지웠다(설계 §6).

### 3.4 배치면 정렬 (2026-10-06)

- 사용자 결정 안 B. 실제 메시를 재서 Floor 안쪽 배치 면 한 변 7.161 = 4 × 1.79에 맞췄다.
- 바꾼 값: `cellSpacing` 2.2 → 1.79, `modelScale` 1 → 0.8136, `ArtifactFloor.prefab` 자식 위치, `SoulDrop.prefab` 자식 z 0 → −0.55.
- 회복 성배·가시심장 받침은 맞았다. 영혼 드랍 세로 받침은 0.24 짧아 남았다(§2).
- `modelHeight`는 계획에 "0.15 유지, 사용자 선택 대기"로 남았지만 같은 커밋 `5fdfc852`에서 0이 됐다. 지금 값도 0이다.

### 3.5 SA 정렬·호버 (2026-10-11)

- SA P3·P7로 번호 기반 Core 계약과 `IArtifactPopupDisplay` 표시값 전달로 바꿨다. 기록 원본은 [스테이지 맵 계획](2026-10-10-stage-map-plan-Hojin.md)이다.
- 호버 상세와 3초 문구 수명을 더했다. 이때 `ArtifactPopupConfigTests`의 옛 기대값 2.2를 에셋 값 1.79로 맞췄다(에셋 변경 없음).

### 3.6 로비 이전과 Scope 정리 (2026-10-13~15)

- LV5에서 운영 화면·편집 실행을 로비로 옮기고 StageSelect의 열기 버튼·팝업·배치판·아티팩트 카메라를 지웠다. 기록은 [로비 계획](2026-10-11-lobby-plan-Hojin.md).
- P0~P5에서 `ArtifactViewSystem`·`ArtifactPopupSession`을 지우고 `LobbyViewScope`·`PopupViewScope`로 수명을 나눴다. 기록은 [View 구조 계획](2026-10-13-view-structure-plan-Hojin.md).
- 이후 로비 실행 계층 재구성(Flow → Plan·Job, 10-23)으로 지금 경로는 `LobbyPlan`·`ArtifactPlan`·`LobbyArtifactCheckJob`이다.

### 3.7 공간 모델 교체 (2026-10-17)

- 사용자 요청으로 Codex가 `ArtifactSpace_final.fbx`·`RGB_texture.png`로 배치판 외형을 바꿨다. `ArtifactFloor.prefab`의 루트 GUID·fileID·씬 연결, 4×4 논리 배치·입력 평면·아이템 배율은 그대로다.
- 정렬: 자식 균일 배율 1.4997995, 위치 (0, −2.4341009, 0). 루트 배율 0.75와 합쳐 배치면 한 변 7.16·중심 (0,0,0)·높이 0이다. 원본 재질 셋을 추출해 매핑했다.
- 후속 B(사용자 요청): `Dummy021~024`·`Dummy030` 다섯 개를 지웠다(메시 66 → 61). 지우려고 FBX 중첩 인스턴스를 풀었으므로 원본 FBX가 바뀌어도 자동 반영되지 않는다.
- 시험: `ArtifactBoardMapperTests`가 배치면을 첫 메시 대신 이름 `ArtifactSpace_floor_001`로 고르게 고쳤다. `DataBaseAssetTests`의 실패 3건은 교체 전 프리팹에서도 같았다(관련 데이터·시험 무변경).
- 옛 모델·재질은 남겼다. 실제 마우스 Play는 하지 않았다.

### 3.8 인계 E3 대응

천명님 [아티팩트 Framework·View 인계](../handoffs/2026-10-03-artifact-framework-view-handoff-ko.md)의 항목별 처리다.

| 인계 항목 | 처리 |
|---|---|
| E3-1 초기화 주체·시점·크기 출처 | D6·D7 |
| E3-2 의도·ID·좌표·회전 전달 | Q1, Phase B·D |
| E3-3 한 번 실행, 성공·거절·예외 구분 | Phase B → P7 번호 계약 |
| E3-4 결과와 요청 대응, 프리뷰 교체 | Q1·D8·D10 |
| E3-5 팝업 열기·닫기, 입력 중단, 지연 Job | D1·D2, Phase C |
| E3-6 최신 Buffer로 표시, ID·칸 주의 | Phase B·C |
| E3-7 잠금·재고 없음·범위·충돌 실제 경로 | 사람의 Play 남음(§2) |
| E3-8 실제 DB·화면 검증 | 사람의 Play 남음(§2) |

## 4 공유 메모

| 메모 | 받는 사람 | 전달 |
|---|---|---|
| [현강님 공유 — 아티팩트 팝업 UI 연결](../handoffs/2026-10-06-artifact-popup-ui-hyeongang-share-Hojin.md) | 현강님 | 미전달 |
| [공용 오류 정리 변경 기록](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md) | 전투 작업자(용근님) | 미전달 |
| [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.7 | 천명님 | 기록(전달은 Core 전달 문서 상태를 따름) |
