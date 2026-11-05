# UI 연결 검증 규약 적용 (2026-10-18)

## 설계·Task

필수 연결 오류는 호출자가 실패를 인지할 수 있도록 throw한다. 로그와 return/false로 요청을 삼키지 않는다. Assets/AGENTS.md의 UI 규약이 원본이다.

이번 Phase는 UISystem의 공개 개폐 요청과 UI 화면의 명시적 초기화·필수 목록 연결 점검을 수정한다. 씬, 프리팹, Core 규칙은 변경하지 않는다.

- [x] AGENTS.md에 오류/정상 반환 구분과 예외 종류를 추가.
- [x] UISystem: 미등록 UI 개폐(3곳), 파괴된 UI 열기(2곳), 잘못된 고정 표시 오버로드 인자(1곳).
- [x] CombatLogUI: 레이아웃 초기화 실패(4곳)와 자식 Scroll View 연결 실패(1곳). Initialize는 성공 여부 bool 대신 void/예외 계약으로 변경.
- [x] CombatLogExpansion: 필수 레이아웃 연결 실패(1곳). 후속 좌표 변환에 필요한 부모 RectTransform도 검증.
- [x] ShopUI·RecoveryUI·ArtifactPopupUI: 필수 목록 컨테이너·프리팹 누락(각 1곳). Shop/Recovery는 모델을 화면 상태로 복사하기 전에 검사.
- [x] UISystem 오류 경로 3개 테스트와 CombatLog 초기화 실패 2개 테스트 추가.
- [x] ProjectL.Systems·ProjectL.Tests.Framework 외부 MSBuild 컴파일: 모두 오류 0. 경고는 기존 직렬화 필드/중복 using에서 발생.
- [x] 변경 파일 git diff --check 통과.
- [ ] Unity EditMode 실행 및 화면 재생 확인(외부 컴파일과 구분).

## 정상 반환으로 유지한 경로

- 비활성/상호작용 불가 입력 무시, 범위를 벗어난 과거 슬롯 입력, 이미 열린/초기화된 상태, 반복 해제는 연결 검증 실패가 아니다.
- CombatLog.RefreshLogs의 Buffer 미바인딩 대기는 씬 수집 후 서비스 바인딩 전 Update와 구분한다. 필수 레이아웃 실패는 더 이상 대기로 취급하지 않는다.
- UIInputBinding.Attach(null)는 선택 Button을 생략할 수 있는 기존 도우미 계약이다. 필수 Button 여부는 화면의 Validate가 판단한다.
- 선택 아이콘·문구·연출 참조, 없는 항목의 TryGet/검색, FMOD Bus 작업의 실패 결과는 이 연결 검증 변경과 구분한다.
- UISystem.CloseUI의 이미 파괴된 객체 정리는 정상 해제 수명으로 유지한다. 반면 존재하지 않는 ID로 명시적인 개폐를 요청하면 예외를 던진다.
- OwnedCharacterStatsReader의 오류 경계 보고·정지는 별도 RuntimeFaultBoundary 계약이다. 이번 연결 규약을 이유로 그 계약을 우회하지 않는다.

## 구조 검토

SSOT는 기존 Buffer와 UI 등록부를 유지한다. SOLID: 개별 화면이 자신의 필수 참조를 검사하고 UISystem은 등록·수명 오류만 검사한다. 새 서비스·Builder 등록·상위 Export·소비자 조회 경로를 추가하지 않아 FW-001/002와 Global Context 계층은 그대로다. UIBase 등록 단위/UIId·부모·Policy·활성화 수명을 변경하지 않는다. 기능 구현은 각 UI에, 공용 개폐 계약은 UISystem에 한정해 관련 없는 전투 입력/타임라인 조립을 수정하지 않는다.
