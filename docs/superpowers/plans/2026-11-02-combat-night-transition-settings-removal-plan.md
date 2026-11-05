# 전투 Night 화면 전환 설정 제거 — 범위·Task·실행 기록

작성: 2026-11-02. 사용자 범위 확인 뒤 Night 설정까지 삭제하도록 승인한 작업이다. 상태: N1 코드·문서 정리 및 컴파일·기존 EditMode 확인 완료, Play 확인 대기. Phase 리뷰 지점.

## 결정과 범위

- 목적: 배경 맵에 Night Camera·NightMap이 없어도 전투 표시를 조립한다. Day 카메라 존재 검사는 유지한다.
- 현재 자동 화면 전환은 `CombatPhaseTransitionPresenter.TryBegin`이 false를 반환해 재생하지 않는다. 이 상태를 유지한다.
- `CombatPresentationSettingsData`의 밤 전환 레이어·카메라·색 필드, 속성, 이름 검사와 기본 SO 에셋의 같은 키를 제거한다. 아침 설정과 공통 속도·양은 유지한다.
- `CombatPhaseTransitionSettings`는 아침 요청만 생성한다. 사용하지 않는 밤 레이어 조회를 제거하고, Presenter의 주석 처리된 복구 예제도 아침 요청만 표현하도록 맞춘다.
- NightMap 필수 검사는 원래 없다. 맵 프리팹·씬·프로젝트 레이어·공용 Render 화면 전환·수동 TestRuner는 변경하지 않는다. 전투 국면, 밤 BGM, 사건 형식 표의 국면 분류는 대상이 아니다.
- Core·FieldCamera·Architecture는 수정하지 않는다. Factory 변경은 기존 Framework 공유 문서에 미전달 기록으로 남긴다. 커밋·push하지 않는다.

## 설계 점검

- SSOT: 아침 전환 값의 원본은 기존 View DB 한 곳이다. 밤 값을 기본값이나 fallback으로 복제하지 않고 삭제한다.
- SRP: DB는 값 조건, Settings는 요청 변환, Factory는 필요한 맵 카메라 존재 검사만 담당한다.
- OCP: 새 옵션·정책·전역 분기를 추가하지 않는다. LSP: 제공 인터페이스와 완료·취소 계약을 바꾸지 않는다.
- ISP·DIP: 공개 인터페이스·의존성 주입 경계를 유지한다. 사용하지 않는 Night 요청만 내부 설정에서 제거한다.
- JMH/FW: Factory 생성 → 기존 재생 조립 → 주입된 Global Render 소비 경로를 유지한다. Builder 등록·Export·조회 계층·Scope 수명은 바꾸지 않는다.
- 기능 변경 독립성: 밤 배경 요구 제거는 DB 설정·전환 요청 변환·카메라 검사에 한정한다. 이동·공격·HUD·Core 기능 구현을 수정하지 않는다.

## Phase N1 — 설정과 요구조건 제거

- [x] Factory의 Night 카메라 존재 검사 제거, Day 검사 유지.
- [x] DB 밤 전환 필드 3개·속성 3개·이름 검사 2개와 SO 에셋 키 3개 제거.
- [x] Settings의 Night 요청·생성자 인자와 밤 레이어 조회 제거. 비활성 Presenter 주석 참조 정리.
- [x] SO 사용법·씬 연결 안내·화면 전환 안내·Framework 공유 기록 갱신.
- [x] 변경 후 컴파일, 남은 참조·차이 검사, 기존 DataBaseAssetTests 실행 확인.
- [x] 결과와 Unity 미확인 항목 기록. 이 Phase 리뷰 지점에서 보고.

## 검증 기록

- 최종 수정 범위: 코드 4파일(DB 설정, Settings, Factory, 비활성 Presenter 주석) + 기본 SO 에셋 1개 + 안내·공유 문서 4개 + 이 기록 1개. Presenter는 Night 설정을 가리키던 주석 예제만 정리했으며 동작은 그대로다.
- `ProjectL.Systems.csproj` MSBuild 종료 코드 0. 기존 UI CS0649 경고 7건 외 변경 파일 새 경고·오류 없음. 로그: `Logs/night-transition-settings-build.log`.
- 열린 Unity가 있어 배치 테스트는 `HandleProjectAlreadyOpenInAnotherInstance`로 실행하지 못했다. 에디터/잠금 파일을 종료·삭제하지 않고 기존 Unity로 전환했다. 실패 시도 로그: `Logs/night-transition-settings-unity.log`.
- 기존 Unity에서 변경 소스 컴파일·어셈블리 재로드 완료 후 Test Runner로 `ProjectL.Tests.CoreIntegration.DataBaseAssetTests` 실행: **9/9 통과**, 실패 0. 원본 결과를 `Logs/night-transition-settings-tests.xml`로 복사해 확인했다. 이 fixture의 현재 내용은 Core 기획 입력 변환 검사이므로 View 전환 SO 전용 테스트 통과로 확대하지 않는다.
- `Assets`의 C#·SO 에셋에 `NightTransition*`·`nightTransition*` 잔여 참조 없음. Night BGM 설정과 Day 필수 검사 유지 확인. 변경 파일 `git diff --check` 통과, UTF-8·LF 확인.
- 씬·맵 프리팹·Core·FieldCamera·Architecture·공용 Render 변경 없음. 신규 서비스·Builder·등록·Export·공개 계약 변경 없음. 사용자 선행 변경은 그대로 두었고 커밋·push·스테이징하지 않았다.
- 실제 Play 확인은 미실행: Day 카메라만 둔 맵 진입, 배치→전투→배치 복귀, 이동·줌·흔들림, Day 카메라 누락 거절. 프리팹에서 Night 오브젝트를 실제 삭제한 것은 아니다.
