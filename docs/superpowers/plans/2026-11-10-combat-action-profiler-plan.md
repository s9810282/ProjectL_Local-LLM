# 전투 행동별 Profiler·익스포터 구현 계획 — 2026-11-10

- 작성: Codex
- 상태: Phase 1~3 코드 및 Phase 4 정적 검토·사용 안내 완료. Unity 인수 항목 대기, **컴파일 미확인**
- 설계 원본: [전투 행동별 계측 설계](../specs/2026-11-10-combat-action-profiler-design.md)
- 작업 규칙: 사용자의 2026-11-10 **“Phase 4까지 쭉 진행해”**에 따라 Phase 1~4를 연속 진행한다. 설계와 다른 결정이 필요하면 구현 전에 묻는다. 커밋·push 없음.

## 1. 공통 제한

- Core·FieldCamera·Architecture 수정 없음. 전투 View의 결과 수신·연출과 View 측 입력을 계측한다.
- 기존 변경/스테이징된 연발 피해 숫자 작업을 보존한다. 구현 직전 대상 파일의 diff를 다시 읽고 계측 줄을 더한다.
- 새 `.cs`에는 `.meta`, 블록 namespace·Allman·한국어 설명·UTF-8/LF를 적용한다.
- Test 코드 작성 없음. MSBuild·Unity 배치 실행 없음. 각 보고에 **컴파일 미확인**과 사용자 확인 항목을 적는다.
- 새 계측은 선언·초기화·분류·호출까지 `UNITY_EDITOR || DEVELOPMENT_BUILD`로 제한한다. release의 게임 제어 흐름은 그대로다.
- 기능 커버리지 원본은 설계 §4·§5다. 이 계획에서 별도 행동 목록을 유지하지 않는다.

## 2. Phase 0 — 조사·설계·계획

- [x] 기존 CSV가 프레임/마커 대응과 사건 종류를 잃는 원인 확인.
- [x] Unity 공식 ProfilerMarker API와 일반/개발 빌드 차이 확인.
- [x] Request 전체, Command/Preview enum 전체와 우회 경로 확인. Move의 Gate 우회, Camera의 Router 우회 기록.
- [x] 사건 표·Owner 사실·이동/공격/조각 실행 수명 확인. 포효 Hit와 실제 실행 종료의 차이 기록.
- [x] SSOT·SOLID 5원칙·FW·JMH·기능 변경 독립성 검토.
- [x] 설계와 아래 Task 계획 작성.

2026-11-10 사용자가 Phase 4까지 연속 구현을 승인했다. 아래 완료 표시는 코드/정적 검토이며 Unity 인수와 구분한다.

## 3. Phase 1 — 프레임별 원본 내보내기

대상: `Assets/ProjectL/Script/Tool/Editor/ProfilerFrameScan.cs`, `ProfilerFrameExporter.cs`, 필요한 작은 Editor 출력 협력 파일.

- [x] P1.1 프레임×경로 샘플을 순차 출력하는 구조 추가. 기존 이름/경로 합산과 프레임 지표 유지.
- [x] P1.2 `frame_markers.csv`와 `actions.csv` 작성. 새 행동 마커가 없는 이전 캡처도 지원.
- [x] P1.3 marker별 최대 total/self/GC의 프레임 번호, 발생 프레임 기준 평균, CSV 정밀도 보완.
- [x] P1.4 행동별 summary와 해당 프레임 지표 연결. 중첩 total·관측 구간·미관측 값의 의미 명시.
- [x] P1.5 임시 폴더→최종 확정, 취소·예외·파일 핸들 정리, 기존 export 보존 점검.
- [x] P1.6 정적 검토: CSV escaping/InvariantCulture, 열과 행 대응, 0/빈 값, 같은 최대값, 스킵 프레임, 전체 샘플 메모리 누적 없음.

사용자 확인: 기존 `.data` 또는 버퍼로 내보내기 → 기존 요약 유지, frame_markers의 같은 프레임 계층과 Unity 비교, actions 미관측 안내. 취소 시 완성 폴더가 생기지 않는지 확인.

**코드·정적 검토 완료.** 사용자 연속 진행 지시에 따라 Phase 2로 진행했다. Unity 출력 확인 대기.

## 4. Phase 2 — 전투 사용자 행동 계측

대상: Combat/Input의 Receiver·Interaction·Hover·UiEntry·Camera와 전투에서 사용하는 손패/가방/정보 UI. 구체 파일은 설계 §4의 기능 소유자다.

- [x] P2.1 기능별 정적 마커 정의 및 개발 빌드 guard 패턴 적용. 거대 중앙 관리자/새 서비스 금지.
- [x] P2.2 모든 Request 수신·차단과 실제 기능 진입을 계측. camera 우회·자동 pointer refresh 구분.
- [x] P2.3 배치·회수·이동 경로·공격 조준·장비 조작 계측. Gate 및 Move 직접 제출 경로의 Queued/Blocked 의미 구분.
- [x] P2.4 모든 PreviewKind의 실제 Request/Apply와 hover Poll/Refresh 계측. 변경 없는 Poll을 입력 횟수로 표시하지 않음.
- [x] P2.5 전투 시작·턴 종료·포기 확인창, 손패/가방/정보/버프 UI의 hover/click/Submit·닫기, 개발 Layer 진입을 전수 대조.
- [x] P2.6 커버리지 표에 실제 파일·메서드·마커 이름을 기록. 새 UI 리스너·게임 상태·문자열 할당 유무 검토.

사용자 확인: 배치/호버/공격/이동/되돌리기/아이템/확인창/카메라/정보 UI를 각각 수행 → actions에서 프레임 검색, Profiler에서 같은 이름 확인. 입력 차단·취소 시 기존 동작과 동일함을 확인.

**코드·정적 검토 완료.** 사용자 연속 진행 지시에 따라 다음 단계로 진행했다. Unity 확인 대기.

## 5. Phase 3 — 적 행동·보스 및 결과 연출 계측

대상: Combat/Playback의 사건 분류·Presenter·Players, Presentation/Movement·Attack·Pieces와 지속 연출의 기존 소유자. 이미 수정 중인 파일의 작업본을 보존한다.

- [x] P3.1 기존 사건 형식 표/Kind와 Actor 사실으로 Event Dispatch 분류. Player/Enemy/Unknown/System 처리.
- [x] P3.2 플레이어·적 이동의 Begin/Tick/End/Cancel. 방향 복귀·끌어오기·넉백 구분.
- [x] P3.3 플레이어·적 공격의 Begin/Tick/Hit/End/Cancel. 일반/원거리/비행/추가 공격/조기 공격 경로 포함.
- [x] P3.4 보스 포효·소환의 조립/실행 라벨 연결. Hit 이후 작아짐·줌 복귀까지 Tick과 End 보존.
- [x] P3.5 다른 결과 사건도 종류별 Dispatch를 유지. 지속 실행이 있으면 해당 소유자에서 진행 비용 계측.
- [x] P3.6 반격 진영, 동시 실행의 호출 수, 완료 콜백 재진입, 취소·오류·씬 해제 시 Scope 닫힘 검토. 게임 수명 상태 추가 없음.

사용자 확인: 적 이동/근접·원거리 공격/반격/포효/소환의 첫 실행과 반복 실행을 각각 캡처. 포효 히트 이후에도 Roar.Tick이 나타나고 실제 종료/취소에 맞춰 끝나는지 확인. 정상 재생·연발 피해 숫자 동작도 확인.

**코드·정적 검토 완료.** 사용자 연속 진행 지시에 따라 다음 단계로 진행했다. Unity 확인 대기.

## 6. Phase 4 — 통합 인수·사용 안내

- [ ] P4.1 새 캡처로 행동별 최대 프레임을 찾아 Unity CPU Hierarchy와 대조. 입력 제출·Core 계산·View 연출 시간을 구분해 보고.
- [ ] P4.2 1프레임/긴 캡처, 같은 종류 동시 행동, 시작/끝이 잘린 캡처, 녹화 중 스킵을 확인. 원본에 없는 행동을 추정하지 않음.
- [ ] P4.3 사용자 Development Build 측정 확인. 일반 비개발 빌드는 새 계측이 제외되는지 사용자 빌드/생성 코드 확인 결과를 기록.
- [x] P4.4 정적 마커 최초 초기화·첫 JIT를 분리하는 반복 측정 방법과 고빈도 호버 GC 확인 항목을 안내. 실제 측정은 사용자 확인 대기.
- [x] P4.5 구현된 출력 형식·마커 이름·미확인 범위를 사용 안내에 기록하고 설계/계획 상태 갱신.

Unity 검증을 받지 못하면 그 항목은 대기로 남긴다. Test 작성·MSBuild로 대체하지 않는다.

## 7. 실행 기록

### 2026-11-10 — Phase 0

설계·Task 계획만 작성했다. 런타임/Editor 코드와 기존 profiler export는 수정하지 않았다. Phase 1~4 미착수. **컴파일 미확인**.

### 2026-11-10 — Phase 1~4 연속 구현

사용자의 “Phase 4까지 쭉 진행해”로 설계 범위 전체의 구현을 진행했다. Phase 4의 실제 캡처 대조·Player 실행(P4.1~3)은 사용자 Unity 인수 대기다. **컴파일 미확인**. Test 작성·MSBuild·Unity 배치 실행·커밋·push는 하지 않았다.

- Phase 1: `ProfilerFrameScan.Sample`을 출력 콜백으로 순차 전달하고 `ProfilerFrameCsvOutput`에서 두 CSV를 기록한다. 기존 세 파일, 최대 프레임·발생 프레임 평균, 6자리 CSV 정밀도, 행동 요약, 고유 임시 폴더 확정을 연결했다. 취소 문구도 실제 임시 출력 정리에 맞췄다.
- Phase 2: enum 기반 공통 수신·제출·미리보기와 기능 소유자의 고정 마커를 연결했다. Move 직접 제출·카메라 우회·공용 UI·거절 응답 Apply를 포함한다. 자동 PointerRefresh/Poll과 입력을 구분한다. 현재 코드에는 개발 Layer 조작 UI가 없어 새 UI 기능을 추가하지 않았다.
- Phase 3: 기존 사건 표와 Character Kind·Owner로 분류했다. 공통 사건 진입에는 Dispatch, 내부 Presenter에는 Handle을 두어 기차/국면 전환 우회도 포함한다. 조립 원본, 이동 종류, 반응, 발사체 수명에 라벨을 붙였다. 조기 공격은 이전 Gate의 진영을 읽지 않으며 발사체는 각 비행에 분류를 보관한다. 포효 Hit 뒤의 Tick과 실제 End/Cancel을 유지한다.
- Phase 4: [사용 안내와 마커 위치](../viewDocs/combat-action-profiler-guide.md)를 작성했다. 첫 사용/반복, 관측 범위·중첩 total, 동시 실행, 미관측, 비개발/개발 빌드 해석과 Unity 확인 목록을 남겼다.

정적 확인 결과:

1. 변경·추가 C# 64개에서 전처리와 괄호 짝을 점검했다. Editor 출력 3개를 제외한 런타임 61개에서 새 계측 코드의 비개발 분기 잔존이 없었다. 런타임 신규 3개는 비개발 분기에서 비어 있다.
2. 기존 런타임 58개 중 48개는 비개발 분기 실행 토큰이 원본과 같고, 10개는 식 본문의 블록화 또는 if 중괄호 추가만 달랐다. 반환값·기존 호출·순서는 유지된다. 이 확인은 C# 컴파일이나 Player 코드 생성 검증이 아니다.
3. 고빈도 경로의 계측은 기존 표 조회·불변 참조·값 타입 Scope로 구성했다. 문자열/배열/사전 생성은 정적 초기화에 한정한다. 진영 조회 delegate는 Presenter 조립 때 한 번 연결한다. 실제 GC/CPU 비용은 미측정이다.
4. 동기 Scope의 using 해제로 early return·예외·재진입을 닫는다. 포효·발사체의 완료 통지를 연출 종료로 대체하지 않았다. 원래 수명 상태를 복제하지 않았다.
5. CSV 헤더/행 대응, InvariantCulture, escaping, 미관측 값, 동률의 첫 프레임, 출력 핸들·실패 정리를 검토했다. 원본 캡처 파일은 변경하지 않았다. 실제 Unity 내보내기는 아직 수행하지 않았다.
6. SSOT·SOLID 5원칙·FW/JMH와 기능별 소유자 분리를 설계 §7과 대조했다. 새 서비스·등록/Export·UI 바인딩 없이 기존 소유자에 계측만 연결했다. Core·FieldCamera·Architecture·에셋·기존 Test 변경 없음.
