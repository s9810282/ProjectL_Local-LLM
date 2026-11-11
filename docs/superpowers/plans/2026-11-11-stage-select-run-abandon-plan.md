# StageSelect 런 포기 구현 계획 — 2026-11-11

## 설계

StageSelect에서 GlobalUI의 RunAbandon을 확인하면 현재 런을 정리하고 로비로 돌아간다. 기존 옵션(`UIId.Setting`, Parallel)과 자식 확인창(`UIId.Confirm`)의 등록·바인딩·개폐 수명을 재사용한다. Stage 전용 확인 문구를 사용하며 Y에서만 요청하고, 요청을 수락한 뒤 UISystem으로 옵션을 닫아 시간·오디오·입력을 복구한다.

전투가 없는 현재 런의 정리는 Core가 맡는다. 대상 Run ID를 다시 비교하고 빈 ID·종료된 런·준비 또는 활성 전투를 거절한다. 기존 초기화 트랜잭션으로 로스터·덱·맵·런 재화와 이번 런 합계를 정리한다. 마지막 완료 요약·누적 명성·영구 유물은 유지하며 다음 런은 로비 출발이 시작한다. 이번 기능의 Core 수정은 사용자 승인 범위다.

Stage는 맵이 준비됐고 Runner가 비었으며 로딩·전투 진입·처리 또는 표시 중인 맵 명령이 없을 때만 요청을 받는다. 맵 이동은 착지까지 입력 명령을 보존하므로 같은 Buffer로 검사한다. 수락 이후에는 Stage Plan이 새 맵 작업보다 포기를 먼저 고른다. Core Job이 끝나고 Runner가 빈 뒤 Stage가 로비로 전환해 기존 씬 이탈 검사를 통과한다. Core가 거절하면 그 요청을 닫고 Stage를 계속한다.

## Phase 1 — Core부터 GlobalUI까지 연결

1. Core 팩토리에서 런 포기 서비스를 생성한다. 내부 Builder에 인터페이스와 바인딩 모듈을 등록하고 Core Group을 통해 Stage Job에 전달한다.
2. Stage LifeCycle의 요청 인터페이스를 기존 LifeCycle Builder와 Group에 등록한다. 이번 방문의 요청·처리 결과는 Stage 진행 객체가 보관하고, Stage Plan과 Job이 처리한다. 로비 전환은 Runner 배출 뒤 LifeCycle에서 한다.
3. OptionUI가 LifeCycle Group으로 Stage 요청을 받는다. 기존 ConfirmUI에 Stage 전용 문구를 넣고 UISystem의 자식 열기·닫기 경로를 사용한다.
4. 서비스 등록·조회와 상태 전이를 정적으로 검사하고 Core 인계 문서 §5 및 §71에 기록한다. Unity 컴파일·재생은 사용자가 확인한다.

2026-11-11 Phase 1 코드 반영 완료. 서비스 생성·등록·조회, 새 스크립트의 Unity 메타 GUID와 변경 공백을 정적으로 확인했다. 컴파일 미확인, Unity 재생 확인 대기.

## 규약 점검

- SSOT/FW-004. Run ID·결과·전투 유무는 Buffer, 정리 규칙은 Core, Stage 포기 요청 수명은 Stage 진행 객체, 옵션 표시 수명은 UISystem이 소유한다.
- SRP/OCP. 기존 Run 시작 서비스와 전투 포기 흐름의 호출 계약을 유지한다. 맵 포기의 가용 시점은 Stage, 정리 범위는 Core 트랜잭션, 문구는 ConfirmUI에서 검토할 수 있다.
- LSP/ISP/DIP. 포기 Job은 처리 통지를 한 뒤 완료하고 각 호출자는 전용 요청 또는 실행 인터페이스만 받는다. Core·LifeCycle 하위 서비스를 루트에 재등록하지 않는다.
- FW-001~003/008~009. 기존 팩토리의 Build 순서와 기존 UI 등록 단위를 사용한다. Stage LifeCycle의 기존 모듈 등록에 서비스만 추가한다.
- Unity 확인. StageSelect에서 Y → 로비 → 출발해 새 런, N/ESC → 맵 유지, 이동·전투 진입 중 포기 차단, 전투에서 기존 포기 확인. 가능하면 상점·회복·증강 팝업에서 포기 후 다음 출발까지 확인한다.
