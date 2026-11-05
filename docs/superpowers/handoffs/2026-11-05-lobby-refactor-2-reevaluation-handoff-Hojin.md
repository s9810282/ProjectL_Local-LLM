# 로비 리팩토링 2차 R2-f 재평가 인계 (2026-11-05)

- 작성자: 호진(이호진)
- 사용자 요청: "재평가는 다른 컴퓨터에 할거 같으니 문서로 남겨줘".
- 이 문서는 R2-f(재평가)를 다른 컴퓨터에서 실행하는 사람·AI가 읽는 시작점이다. **코드 변경 없음.**

## 1 무엇을 하나

로비 리팩토링 2차(R2-a~R2-e)가 끝난 판을 1차 평가와 **같은 평가 설계**로 다시 잰다. 결과를 1차와 견주고, 고친 문제와 새로 찾은 문제를 나눠 적는다.

| 문서 | 경로 | 쓰임 |
|---|---|---|
| 평가 설계 | [2026-11-05-lobby-view-evaluation-design-Hojin.md](../specs/2026-11-05-lobby-view-evaluation-design-Hojin.md) | 기준 13개·감점표·절차·산출물. **그대로 쓴다** |
| 1차 보고서 | [2026-11-05-lobby-view-evaluation-Hojin.md](../specs/2026-11-05-lobby-view-evaluation-Hojin.md) | 비교 대상(13기준 평균 88.7) |
| 1차 문제 목록 | [2026-11-05-lobby-view-evaluation-issues-Hojin.md](../specs/2026-11-05-lobby-view-evaluation-issues-Hojin.md) | 문제 ID(M-xx·N-xx) 원본 |
| 2차 설계 | [2026-11-05-lobby-refactor-2-design-Hojin.md](../specs/2026-11-05-lobby-refactor-2-design-Hojin.md) | §5 미룬 낮음, §6 예상 점수 |
| 2차 계획·실행 기록 | [2026-11-05-lobby-refactor-2-plan-Hojin.md](../plans/2026-11-05-lobby-refactor-2-plan-Hojin.md) | §2.6 R2-f, §3 Phase별 실행 기록·Codex 검수 |

## 2 시작 전 확인

1. **기준 판:** R2-e를 커밋한 커밋 하나다. 이 문서를 쓸 때 R2-e는 아직 커밋 전이었다(R2-d 커밋은 `b6342c734`). 작업 트리를 `git pull` 한 뒤 `git log`로 R2-e 커밋을 찾아 해시를 보고서 머리에 적는다.
2. **작업 트리가 기준 판과 같은지:** 범위 파일에 커밋 안 된 변경이 없어야 한다(`git status`). Unity 에디터가 바꾼 폰트 SDF 에셋 같은 범위 밖 파일은 상관없다.
3. **범위 파일 목록을 다시 센다**(평가 설계 §2.1, 부록 A의 규칙 그대로). 1차 뒤에 범위 안에서 바뀐 파일은 아래와 같다. 부록 A에 없는 새 파일은 같은 규칙으로 넣는다.
   - 새 파일
     - R2-a: `ViewSystem/Common/FaultReportingUIInputReceiver.cs`, `ViewSystem/Lobby/FaultReportingLobbyReceiver.cs`
     - R2-b: `ViewSystem/Common/BoardRotation.cs`, `ViewSystem/Common/PointerSelectInterpreter.cs`, `ViewSystem/Common/SelectOwnerSlot.cs`, `LifeCycles/Lobby/LobbyJobPriority.cs`
     - R2-c(16개)
       - `DataBaseSystem/…/LobbyVisual/Item/LobbyFacilityKinds.cs`
       - `BufferSystems/LobbyView/ILobbyDepartureState.cs`
       - `UISystem/Screens/Lobby/ILobbyDeckDisplay.cs`
       - `UISystem/Screens/Stage/`: `IArtifactPopupLifetime.cs`, `IArtifactPopupScreenAreas.cs`, `IArtifactPopupCloseReceiver.cs`
       - `ViewSystem/ArtifactPopup/ArtifactPopupServices.cs`
       - `ViewSystem/Lobby/`: `ILobbyFacilityAction.cs`, `LobbyFacilityAction.cs`, `ILobbyPopup.cs`, `ILobbyPopupFactory.cs`, `LobbyArtifactPopupFactory.cs`, `LobbyInputServices.cs`, `LobbySceneServices.cs`, `LobbyPieceMover.cs`
       - `ViewSystem/Lobby/Deck/LobbyDeckPopupFactory.cs`
   - 이름 바꿈
     - R2-b: `Lobby/Artifact/Jobs/ArtifactCommandJob.cs` → `ArtifactCoreCallJob.cs`
     - R2-d: `ViewSystem/ArtifactPopup/ArtifactPopupDisplay.cs` → `ArtifactPopupBoard.cs`
   - 지운 파일: R2-b `Lobby/Artifact/Jobs/ArtifactPreviewJob.cs`(편집 Job과 합침)

## 3 실행

- 절차는 평가 설계 §5 그대로다: 찾기 12명(슬라이스 6·렌즈 6) → 합치기 → 적대적 검증 → 점수.
  - 슬라이스의 줄 수는 다시 센 범위로 나눈다. 새 파일은 자기 묶음 슬라이스에 넣는다(예: `ILobbyPopup`·`LobbyPieceMover`는 S1, `ArtifactPopupServices`는 S2, `SelectOwnerSlot`은 S3, `ILobbyDeckDisplay`·`IArtifactPopup*`는 S4, `LobbyFacilityKinds`는 S5, `LobbyJobPriority`·`ILobbyDepartureState`는 S6).
  - 렌즈 L2의 변경 시나리오에 "시설 하나 더하기·팝업 하나 더하기"가 있다. 2차에서 이 구조를 바꿨으므로 그대로 확인한다.
- **독립성(평가 설계 §5·§8):** 검토자에게 아래 §5 목록, 2차 설계·계획, 1차 문제 목록, `CLAUDE.md`의 현재 상태 서술, git 기록을 근거로 주지 않는다. 근거는 지금 코드와 규약 문서(`framework-programmer-guide.md`, `Assets/AGENTS.md`, `CLAUDE.md`의 코딩 규칙·DB 규칙 절)다. §5는 평가가 끝난 뒤 대조할 때만 쓴다.
- 모든 검토자는 읽기 전용이다. **평가 단계에서는 코드를 바꾸지 않는다.**

## 4 산출물

- 보고서: `docs/superpowers/specs/<실행일>-lobby-view-reevaluation-Hojin.md`
  - 머리: 기준 판 해시, 범위 파일 수·줄 수, 검토자 프롬프트에 `CLAUDE.md`가 자동으로 들어갔는지
  - 결과 표: 기준별 점수와 중간·낮음 수(평가 설계 §4 감점표), 1차 점수와 나란히
  - 비교: 1차 문제 중 고쳐진 것, 남은 것, 새로 찾은 것(새 ID)
  - 예상과 실제 차이: 2차 설계 §6 예상(약 99.0, 알려진 문제 기준)과 실제 점수
- 문제 목록: `docs/superpowers/specs/<실행일>-lobby-view-reevaluation-issues-Hojin.md`(검증을 통과한 문제 전부, 위치·내용·실패 시나리오·고칠 방향·기준·판정)
- 평가 끝에 Codex 검수를 받는다(보고서 근거와 점수 계산, 평가 설계 §7). 이 PC에서는 Codex를 중계기 없이 `codex exec`로 바로 돌렸고, 읽기 전용 샌드박스가 저장소 밖 개인 규칙 파일을 못 읽어 규칙 내용을 프롬프트에 붙였다. 다른 컴퓨터의 Codex 설정은 그 컴퓨터 기준으로 맞춘다.
- `CLAUDE.md` 현재 상태와 2차 계획 §3에 R2-f 결과 한 절을 더한다.

## 5 평가 뒤 대조용 — 알려진 남은 문제 (검토자에게 주지 않음)

2차 계획 §3의 실행 기록에서 모은 것이다. 재평가가 이것을 다시 찾으면 "남은 것", 못 찾으면 보고서에 "알려졌지만 이번에 안 잡힌 것"으로 적는다.

| 구분 | 문제 | 까닭 |
|---|---|---|
| 미룬 낮음(2차 설계 §5) | N-53 패널형 팝업 공통 기반 | 현강님 판단 대기 |
| | N-66 아티팩트 UI 폴더·네임스페이스 | 1차 결정으로 그대로 |
| | N-81 로비 글자 묶음 SetActive | 1차 결정 유지 |
| | N-28 덱 시설이 늘 빈 목록 | Core 후보 계약 대기 |
| | N-44 씬 연결 골격 두 벌(로비·스테이지 맵) | 스테이지 선택 정리 때 |
| 사용자 결정으로 미룸 | N-65 보유 슬롯 위젯 프리팹 직렬화 | 슬롯 UI 디자인(아이콘 이미지·스크롤 배경) 대기. 코드 쪽(한 번 찾기)만 함 |
| 되돌림 | N-14 뗀 로비 연결을 다음 진입까지 붙잡음 | 즉시 Supersede가 팝업 입력 Layer 지연 반환을 막아 되돌림. 고치려면 공용 `PopupViewScope` 변경 |
| 주석만 | N-16 입력 슬롯 서비스 등록·키 | Q-R2-7 B(보호 영역 계약 유지) |
| 부분 해결 | N-58 로비 표시 표의 역할 SO switch | 방 프리팹 검사만 한 곳으로 |
| | N-37 미리보기 칸·캔버스 켜고 끄기 왕복 | 응답 대기 중 옛 칸 숨김은 지켜야 함 |
| | N-45 덱 목록 크기 두 곳 | 입력 판정만 하나로, 크기는 역할이 달라 그대로(Q-R2c-2) |
| 그대로 둔 것(근거 있음) | N-22 덱 카드 클릭 `IsInteractiveSlot` | 모델 칸 경계 확인 겸함 |
| | N-25 상태 문구 `_reportedEditNumber` 가드 | 번호 순서 보호 겸함 |
| | N-31 시작 칸 입력 모양 | 에셋을 다시 옮겨야 함 |
| | N-34 로비 누름·뗌의 다시 고르기 | 호버 결과가 다음 Tick까지 낡을 수 있음 |
| | N-19 판 자세 유한성 검사 | 계산 결과 검사라 DB 검사와 중복 아님(R2-d Codex) |

- 2차 Phase마다 Codex 검수를 받았고, 지적은 계획 §3.2·§3.4·§3.6·§3.7·§3.9·§3.11에 있다.
- 2차에서 바뀐 동작(평가 때 "결함"이 아니라 의도로 볼 것)은 계획 §3 각 Phase의 "바뀐 동작"에 있다. 예: 보유 슬롯에서 끌어 나가면 상세가 빔, 로비 가상 카메라가 카탈로그에 없어도 멈추지 않음, 아티팩트 아이콘 선택 칸.

## 6 사용자가 먼저 할 것

- R2-a~R2-e의 Unity 확인 목록(계획 §3.1·§3.3·§3.5·§3.7·§3.8·§3.10)을 아직 다 보지 않았다면, 재평가 전에 확인하고 R2-e를 커밋한다. 재평가는 정적 검토라 실행 확인을 대신하지 않는다.
