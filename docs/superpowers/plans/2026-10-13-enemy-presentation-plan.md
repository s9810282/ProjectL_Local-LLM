# 적 행동 간격과 이동 방향 연출 Task 계획

- 작성: View 작업, 2026-10-13
- 설계 원본: [적 연출 설계](../specs/2026-10-13-enemy-presentation-design.md)
- 상태: **완료.** M1/M2/M2A 코드·독립 Unity 검증과 M3 사용자 화면 확인 완료(2026-10-13). 사용자가 아군·적 이동 회전과 도착 후 방향 복귀 정상 동작을 확인했다. 원본 Unity Test Runner 실행 결과는 별도 전달되지 않았다.
- 각 Phase 완료 후 보고하고 멈춘다. 다음 Phase는 사용자 진행 지시 후 착수한다.
- 커밋·push·타 작업자 메시지는 사용자가 담당한다.

## 0. Phase 0 — 조사·설계·계획

- [x] 현재 Ordered/MovementPlayback/SceneEventPresenter/FieldUnitPresenter 흐름 확인.
- [x] 적 이동 단계마다 RenderFacts가 기존 정면을 다시 적용하는 원인 확인.
- [x] 사용자 기본 시간 선택: 행동 간 0.2초, 턴 전환 0.4초, 회전 0.1초.
- [x] SSOT·SOLID·JMH/FW 및 Core/View 경계 점검을 설계 §4에 기록.
- [x] 행동·턴 경계, 최종 이동과 취소 완료 계약, 회귀 검증을 계획.
- [x] 설계 §2 동작 기준과 M1 범위 사용자 리뷰: “좋아, 설계대로 M1부터 진행해.”

조사 시 이미 변경된 `Assets/Scenes/Combat.unity`의 이동 시간 0.2초와 Core 전달 문서/새 연결 벽 인계 문서는 사용자·다른 작업 내용이다. 덮어쓰지 않는다. Phase 0에서는 운영 코드·씬을 수정하지 않는다.

## 1. M1 — 적 행동·턴 딜레이

범위는 설계 E1~E3와 관련 완료/취소 계약이다. 이동 회전은 M2다.

- [x] **M1.1** `CombatEnemyPresentationPacing` 순수 상태와 테스트: 같은 적의 이동→공격/공격→공격, 첫 행동, 턴 경계, 같은 턴 전환 중복 방지, 결과를 넘는 턴 전환, 취소/재연결.
- [x] **M1.2** SceneRoot에 행동/턴 시간과 읽기 프로퍼티·유효값 검사를 추가한다. 회전 값은 M2에서 추가한다.
- [x] **M1.3** SceneDisplay에서 시간을 주입하고 SceneEventPresenter의 기존 Tick에서 시작 전 대기를 처리한다. 사건 실행은 대기 종료 후 한 번, 사건 순서/오디오/피해 표시는 기존 경로를 따른다.
- [x] **M1.4** Combat 씬에 행동 0.2초·턴 0.4초만 연결한다. 사용자가 수정한 한 칸 이동 0.2초는 보존한다.
- [x] **M1.5** 0초/음수 프레임/큰 프레임, 대기 중 Cancel/Detach/재연결, 늦은 콜백, 전체 결과 완료 전 다음 Core 계산 차단을 검증한다.
- [x] **M1.6** 관련 MSBuild 및 관리 테스트·가능한 Unity EditMode를 실행하고 원본 로그/결과를 기록한다. 관리 테스트와 Unity Test Runner를 구분한다. 실행 범위와 원본 Runner 미확인은 §5.2를 따른다.

**리뷰 보고:** 변경 파일, 동작 경계, 검증 결과, Unity 미확인 부분. M1 완료 후 멈춘다.

## 2. M2 — 이동 전 회전·최종 도착 후 정면 복귀

사용자가 “좋아, 설계대로 M2 회전 작업 진행해.”라고 승인했다. 범위는 설계 E4~E6이다.

- [x] **M2.1** 좁은 이동 시퀀스 표시 계약과 Ordered의 마지막 걸음/종료 완료 처리를 추가한다. 결과 순서와 기존 이동 시작 소리 1회를 보존한다.
- [x] **M2.2** SceneRoot 회전 0.1초와 씬 값을 연결한다.
- [x] **M2.3** FieldUnitPresenter에 ID별 임시 회전 override와 적용/조회/복구를 추가한다. 기존 ModelRotation을 복귀 정면의 원본으로 사용한다. HUD·체력 숫자 방향 보정을 확인한다.
- [x] **M2.4** SceneEventPresenter에서 회전→이동→(마지막 걸음)복귀 회전 상태를 기존 Tick으로 진행한다. 회전 중 위치 고정, 같은 방향이면 추가 회전 생략, 방향이 꺾이면 먼저 회전한다.
- [x] **M2.5** 중간 환경/체력 사건의 RenderFacts에서도 진행 방향 유지, 마지막 도착 후 복귀 완료 전에 다음 행동/결과 확인 금지를 검증한다.
- [x] **M2.6** 표시 취소/실패/필드 이탈/사망/풀 반환/새 ID/재연결에서 회전 상태를 비운다. 아군 이동·체크포인트 복귀·강제 이동의 기존 동작을 유지한다.
- [x] **M2.7** 관련 MSBuild·관리 회귀와 실제 GameObject 회전 Unity EditMode를 실행한다. 독립 reflection 실행이며 원본 Test Runner 미확인은 §5.3을 따른다.

**리뷰 보고:** 회전/이동/복귀 순서, 방향·위치 근거, 취소·풀 재사용 결과, Unity 검증 상태. M2 완료 후 멈춘다.

## 2A. M2A — 아군 이동 회전 확장

사용자 후속 요청 “아군 유닛도 이동 시 회전 들어가야함”을 승인 범위로 삼는다. 기존 회전·복귀·완료/취소 계약을 아군 자발적 Move에도 적용한다. 적 행동/턴 딜레이를 아군에 추가하지 않는다.

- [x] 자발적 이동 판정을 Player/Enemy 공통으로 재사용하고 행동 간격은 Enemy 한정으로 유지한다.
- [x] 회전 시간 설정을 `movementTurnSeconds`(아군·적 공통, 0.1초)로 표시하고 옛 설정 값을 `FormerlySerializedAs`로 인수한다.
- [x] 아군 방향/꺾인 경로·기존 배치 방향 복귀·완료/취소·강제 이동 제외 및 적 회귀를 검증한다.
- [x] 변경·빌드·독립 Unity 증거를 기록하고 사용자 직접 확인인 M3에 인계한다.

## 3. M3 — Unity 재생 확인·인계 (사용자 확인 완료)

담당: 사용자. 2026-10-13 사용자가 “M3 확인 끝났어. 아군·적 이동 회전이랑 도착 후 방향 복귀 잘 돼. 완료로 기록해줘.”라고 보고했다. 이 사용자 화면 확인을 근거로 M3와 해당 작업을 완료 처리한다.

- [x] 아군 이동 회전 정상 동작 사용자 확인.
- [x] 적 이동 회전 정상 동작 사용자 확인.
- [x] 도착 후 기존 방향 복귀 정상 동작 사용자 확인.
- [x] 사용자 M3 완료 보고와 현재 설정 위치(§5.5)를 기록.

기존 세부 확인 목록은 재검증 참고용으로 보존한다. 개별 실행 로그가 전달된 것은 아니므로 아래 모든 항목을 별도로 실행한 증거로 확대하지 않는다.

- 적 직선 경로: 먼저 회전, 칸 간 추가 대기 없음, 최종 칸에서 플레이어 측 복귀.
- 적 꺾인 경로: 위치 정지 상태에서 새 방향으로 회전 후 이동.
- 아군 직선/꺾인 경로: 먼저 회전, 최종 칸에서 기존 배치 방향 복귀, 복귀 뒤 다음 입력 가능. 적 행동/턴 딜레이는 적용하지 않음.
- 이동→공격: 복귀 회전 완료→0.2초→공격과 피해/사망 순서.
- 공격만 하는 적·이동만 하는 적·행동 없는 적, 적→적/적→플레이어/플레이어→적 전환 간격.
- 첫 적 턴, 밤 종료, 전투 승패/포기·씬 이탈, 재진입에서 대기/회전 누수 없음.
- Inspector 세 시간 각각 0 및 다른 값 조정. 기존 이동 0.2초와 독립적으로 동작.
- HUD 방향·배치와 적 모델의 기존 정면, 아군 이동/복귀 동작 확인.

M3 완료 근거는 사용자 기능 확인이며, MSBuild/독립 검증이나 원본 Unity Test Runner 실행 결과와 구분한다.

## 4. 자동 검증의 핵심 사례

| 대상 | 증거 |
|---|---|
| Pacing | 첫 행동 0, 이동 전체를 1행동으로 취급, 같은 적 행동 간 0.2초, 턴 전환 0.4초 1회, 플레이어 전용 경로 0 |
| SceneEventPresenter | 대기 전에는 사건/소리 미실행, 종료 뒤 정확히 1회 실행·완료, 시간 0에서도 동일 순서 |
| Ordered/Playback/Flow | 마지막 복귀/대기 중 미확인 유지, 다음 변경 금지, 완료 뒤 1회 확인 |
| 실제 이동 표시 | 회전 중 위치 고정, 4방향/꺾임/동일 방향, 중간 Render 보존, 마지막 정면 복귀 |
| 수명 | 회전/이동/복귀/대기 중 취소 각각, 늦은 완료 무시, 해제 실패 중 다른 정리 시도, 풀 재사용/재연결 |
| 설정/경계 | 음수·NaN·무한대 거절, 사용자 씬 이동값 보존, Core/Buffer/View DB 수정 없음 |

## 5. 실행 기록

### 5.1 Phase 0 (2026-10-13)

사용자 세 기능 요청과 시간 선택을 받아 현재 코드 경계를 조사하고 설계·Task 계획을 작성했다. 런타임 코드·씬 변경과 테스트 실행은 하지 않았다. 다음은 M1이며 설계 §2 동작 기준 확인 후 착수한다.

### 5.2 M1 — 적 행동·턴 딜레이 (2026-10-13)

사용자가 설계대로 M1 진행을 지시했다. 행동/턴 시간과 간격 판정·기존 Tick 대기를 구현하고 M1 리뷰 지점에서 멈췄다. M2 회전·경로 종료 계약과 M3 화면 확인은 착수하지 않았다.

**구현:** `CombatEnemyPresentationPacing`은 결과·사건 사실만으로 행동/턴 이력을 관리한다. `CombatSceneRoot`가 행동 0.2초·턴 0.4초를 소유하고 `CombatSceneDisplay`가 주입한다. `CombatSceneEventPresenter`는 같은 Tick에서 대기하며, 대기 종료 뒤 사건·소리·후속 피해를 시작한다. 적 `TurnEnded`는 먼저 표시한 뒤 완료 통지를 늦추고, 다음 `TurnStarted`에는 중복 대기를 넣지 않는다. Cancel/Detach/재연결은 대기 콜백·지연 이동 소리·이력을 비운다.

강제 이동 제외(E2)에 필요한 결과의 `ActorId`·처리/명령 종류는 좁은 선택 계약 `ICombatPresentationSequencePresenter.BeginPresentation(CombatResult)`로 Ordered→SceneDisplay→사건 Presenter에 전달한다. Ordered는 간격을 계산하거나 최신 Buffer를 조회하지 않는다. 경로 종료/복귀 완료 계약은 M2다. 기존 생성자의 직접 사용자는 기본 간격 0을 유지하고, 운영 설정 원본은 SceneRoot다.

**설정:** Combat 씬의 `CombatSceneRoot > 적 행동 연출`에서 `Enemy Action Gap Seconds`와 `Enemy Turn Gap Seconds`를 조절한다(각각 0.2/0.4초, 0 허용). 음수·NaN·무한대는 연결 시 거절한다. 사용자 변경인 `Movement Step Seconds = 0.2`는 보존했다.

**검증:**

- VS 2022 MSBuild로 `ProjectL.Tests.Framework.csproj` 및 참조 어셈블리 빌드 성공(오류 0, 변경 파일의 새 경고 0). 새 파일은 `Temp/EnemyPresentationM1/NewFiles.targets`로 포함했고 중복 Compile 항목을 제거했다. 로그: `Temp/EnemyPresentationM1/msbuild-final.log`.
- Unity 6000.0.73f1 독립 검증 프로젝트에서 빌드 DLL의 NUnit fixture를 reflection으로 실행: **138 통과·0 실패·0 건너뜀**. 새 Pacing/실제 GameObject 사례 32개, 기존 MovementView/ResultPlayback/PresentationCleanup/ViewSystem 회귀 106개다. 결과: `Temp/EnemyPresentationM1/Probe/native-results.txt`, 에디터 로그: `Temp/EnemyPresentationM1/native-probe.log`, 실행기: `Temp/EnemyPresentationM1/Probe/Assets/Editor/EnemyM1NativeChecks.cs`.
- 경로 전체 1행동, 이동→공격/공격→공격, 첫 행동, 첫 적 턴, 결과 간 턴 전환 중복 방지, 강제 이동 제외, 0초/음수/큰 프레임, 지연 이동 소리 취소, 대기 중 Detach/재연결, 피해·사망·소리와 완료 통지 순서를 검증했다. 결과 재생 중 Finalization 미허용과 기존 다음 계산 차단 회귀를 유지했다.
- 초기 fixture의 적 보유 ID/소유 역할을 Core 계약대로 null로 수정했다. 독립 프로젝트에는 원본 TMP 설정·폰트와 스크립트 GUID 매핑을 복사해 설정 누락을 보완했다. scratch 복사본만 수정했으며 원본 TMP 에셋은 수정하지 않았다. scratch 로그의 미사용 Cinemachine Editor 참조 누락과 라이선스 토큰 갱신 메시지는 테스트 실패와 구분한다.
- `git diff --check` 통과. 새 C# 세 파일의 `.meta`를 함께 추가했다. Core/Buffer/Job/Plan/FieldCamera/Architecture·유닛 DB 변경 없음. 다른 세션 문서·사용자 씬 이동 시간·기존 사용자 변경은 보존했다.

**남은 확인:** 위 실행은 실제 Unity GameObject를 사용한 독립 reflection 검증이며 **원본 프로젝트의 Unity Test Runner 실행 결과가 아니다**. 원본 Editor 연결용 임시 도구는 로드되지 않아 제거했으며 원본 EditMode Runner·Combat 화면 확인은 대기다. DB 에셋 변경은 없으므로 DataBaseAssetTests 실행 대상은 아니다. 설계 §4의 SSOT·역할 분리·기존 결과 완료/취소 경계·FW 계약을 대조했고 새 서비스/모듈/전역 구독은 추가하지 않았다.

### 5.3 M2 — 이동 회전·최종 정면 복귀 (2026-10-13)

사용자 M2 진행 지시 범위에서 구현·독립 Unity 검증을 완료하고 M2 리뷰 지점에서 멈췄다. M3 원본 Unity 화면 확인은 착수하지 않았다.

**구현:**

- `ICombatMovementEndPresenter.BeginMovementEnd(lastStep, completed)` 선택 계약을 추가했다. Ordered는 같은 대상의 다음 `Moved`까지 중간 표시 사건을 건너뛰어 마지막 걸음을 판정하고, 공격·턴·복귀·다른 대상 이동에서 경로를 끝낸다. 걸음 완료와 복귀 완료를 분리하고 복귀 완료 콜백을 기다린다. 기존 세대/사건 인덱스 검사로 취소 뒤 늦은 완료를 무시한다.
- 자발적 적 이동 여부는 기존 Pacing의 결과 행위자·명령/처리 종류 판정을 재사용한다. 아군·강제 이동·공격 후 밀쳐짐에 이동 회전을 추가하지 않는다. SceneDisplay가 계약과 설정을 전달하며 Core/Buffer/Flow/Job에는 차단 상태를 추가하지 않았다.
- 사건 Presenter는 기존 Tick에서 행동 대기→방향 회전→걸음 보간을 진행한다. 월드 방향은 기존 `FieldUnitPresenter.CenterOf`의 Expected/Final 중심 차이와 Mapper 기준 위 방향으로 구한다. 같은 방향은 회전 시간을 생략하고, 동일 위치는 LookRotation하지 않는다. 첫 이동 소리는 회전 완료 후 한 번 재생한다.
- FieldUnitPresenter가 ID별 임시 방향을 보관해 중간 RenderFacts에서도 유지한다. 경로 마지막 도착에서는 현재 살아 있는 기존 View만 기존 `ModelRotation`으로 복귀하고 override를 지운다. 이동 전체 결과·다음 공격은 복귀 완료 전 진행하지 않는다. 다음 공격의 M1 행동 간격은 복귀 뒤 시작한다.
- FieldUnitView의 방향 적용은 체력 숫자의 템플릿 월드 방향과 HUD의 카메라 방향/월드 위치 보정을 함께 적용한다. Cancel/Detach/Bind는 완료 통지 없이 임시 방향·회전/복귀 상태를 비운다. 사망/필드 이탈/풀 반환/종류 교체/새 ID/Release에서도 방향이 누수되지 않으며 숨긴 View를 복구하려고 다시 보여주거나 빌리지 않는다.

**설정:** Combat 씬 `CombatSceneRoot > 적 행동 연출 > Enemy Turn Seconds` = **0.1초**. 이동 전 회전과 최종 도착 후 정면 복귀에 각각 적용한다. 0은 방향을 즉시 적용한다. 음수·NaN·무한대는 연결 시 거절한다. M1 행동 0.2초·턴 전환 0.4초와 사용자 한 칸 이동 0.2초는 보존했다. 동시에 진행 중인 손패 호버 씬/코드 변경도 보존했다.

**검증:**

- VS 2022 MSBuild `ProjectL.Tests.Framework.csproj`와 참조 어셈블리 빌드 성공, 오류 0·변경 파일의 새 경고 0. 로그: `Temp/EnemyPresentationM2/msbuild-final.log`, 새 파일 포함: `Temp/EnemyPresentationM2/NewFiles.targets`.
- Unity 6000.0.73f1 독립 프로젝트의 실제 GameObject와 NUnit fixture reflection 실행: **162 통과·0 실패·0 건너뜀**. M2 신규 24개, M1 32개, 기존 이동/결과 처리/정리/ViewSystem 회귀 106개. 결과: `Temp/EnemyPresentationM2/Probe/native-results.txt`, 에디터 로그: `Temp/EnemyPresentationM2/native-probe.log`, 실행기: `Temp/EnemyPresentationM2/Probe/Assets/Editor/EnemyM1NativeChecks.cs`(실행 클래스 `EnemyM2NativeChecks`).
- 4방향·회전/기울어진 필드, 회전 중 위치 고정, 같은 방향 생략, 꺾임과 중간 체력 사건의 방향 유지, 마지막 복귀 전 공격·Finalization 차단, 이동 소리 1회, 0초·큰 프레임, 회전/이동/복귀 중 Detach와 늦은 콜백, 사망/필드 이탈/Release 후 새 ID 풀 재사용, 숨긴 대상 취소, 아군/강제 이동/체크포인트 복귀, HUD/숫자 방향, 사망 대상 복귀 시 재대여 없음 검증.
- 초기 풀 반환 fixture가 이전 표시 목록을 다시 그려 두 대상을 만든 것은 새 표시 목록으로 수정했다. 신규 fixture 컴파일 인자와 이 설정 실패를 수정한 뒤 최종 검증을 재실행했다. 최초 독립 실행 기록은 `Temp/EnemyPresentationM2/native-1-results.txt`에 남겼다. M1의 결과/로그는 덮어쓰지 않았다.
- `git diff --check` 통과. M2 신규 C# 파일은 없으며 기존 코드·계약·테스트를 확장했다. Core/Buffer/Job/Plan/FieldCamera/Architecture/DB·Render 내부 변경 없음.

**구조 리뷰:** 설계 §4 및 Framework 적용 기준을 대조했다. 시간은 SceneRoot, 자발적 행동 판정은 Pacing, 순서/완료는 Ordered, 실제 시간 진행은 사건 Presenter, 임시 회전은 FieldUnitPresenter가 각각 소유한다. 기존 위치 중심·ModelRotation·HUD 방향 보정을 재사용한다. 새 서비스·Builder·전역 구독·별도 타이머·트윈 자원은 없고 기존 오류/취소·결과 확인 경계를 유지했다.

**남은 확인:** 독립 검증은 Unity Test Runner 결과나 Combat Play 화면 확인을 대체하지 않는다. 원본 EditMode Runner와 실제 프리팹 전방/HUD 배치는 M3에서 확인한다. scratch 로그의 미사용 Editor DLL 참조/라이선스 갱신 메시지는 M1과 마찬가지로 fixture 실패와 구분한다. 커밋·push하지 않았다.

### 5.4 M3 사용자 직접 확인 인계 (2026-10-13)

사용자가 M3를 직접 진행하겠다고 지시했다. Codex 구현은 M2에서 종료하고 M3 체크리스트를 사용자에게 인계한다. 원본 Test Runner/Combat 화면 검증은 완료로 처리하지 않았으며 사용자 결과를 기다린다. 이 인계에서 코드·씬 변경이나 추가 테스트 실행은 하지 않았다.

### 5.5 M2A — 아군 이동 회전 확장 (2026-10-13)

사용자가 아군도 이동 회전이 필요하다고 요청했다. 설계 E4~E6의 대상을 아군·적 자발적 이동으로 넓히고, 기존 마지막 도착 후 해당 Owner의 ModelRotation 복귀·완료/취소 계약을 재사용했다. 아군 Core의 배치 방향은 이동에서도 보존되므로 별도 복귀 방향 상태를 만들지 않았다. 중립/강제 이동/공격 후 밀쳐짐은 회전 대상에 추가하지 않았다.

Pacing의 `IsVoluntaryMovement`를 공통 판정으로 사용하며 `BeforeStep`의 적 행동 이력/간격은 Owner.Enemy 조건을 유지한다. 실제 이동·복귀 회전은 기존 사건 Presenter 두 진입점에서 이 판정을 쓴다. Ordered/FieldUnitPresenter/Core/Buffer/Flow/Job은 수정하지 않았다. 읽기 전용 보조 검토에서 아군 방향 보존·결과 종류/행위자 계약과 diff 경계를 대조했고 코드 문제를 찾지 못했다. SSOT·SOLID·JMH/FW 기준상 새 상태/서비스·타이머·구독을 추가하지 않고 기존 값/소유자를 재사용한다.

**현재 설정:** `CombatSceneRoot > 유닛 이동 연출 > Movement Turn Seconds` = **0.1초**(아군·적 공통). 옛 `enemyTurnSeconds`는 `FormerlySerializedAs`로 인수하고 Combat 씬 키만 `movementTurnSeconds`로 바꿔 값을 유지했다. 이동 시간 0.2초와 적 행동 0.2초·턴 간격 0.4초도 유지했다. 과거 §5.3의 `Enemy Turn Seconds` 안내는 현재 이름으로 대체된다.

**검증:**

- VS 2022 MSBuild 빌드 성공(오류 0, 변경 파일의 새 경고 0). 로그: `Temp/AllyMovementRotation/msbuild-final.log`, 포함 설정: `Temp/AllyMovementRotation/NewFiles.targets`.
- 독립 Unity 6000.0.73f1 실제 GameObject/NUnit fixture reflection 실행 **173 통과·0 실패·0 건너뜀**. 신규 아군/설정 이전 9개와 현재 기존 회귀 164개다. 결과: `Temp/AllyMovementRotation/Probe/native-results.txt`, 로그: `Temp/AllyMovementRotation/native-probe.log`, 실행기: `Temp/AllyMovementRotation/Probe/Assets/Editor/EnemyM1NativeChecks.cs`(클래스 `AllyRotationNativeChecks`). M1/M2 원본 결과는 덮어쓰지 않았다.
- 실제 Command/Move 결과로 아군 배치 방향 R0/R90/R180/R270과 기울고 회전된 필드, 회전 중 위치 고정·소리 대기, 도착 후 복귀 완료 전 Finalization 차단, 꺾임/중간 체력 사건, 다음 공격에 적 간격 없음, 회전/이동/복귀 중 Detach를 검증했다. 기존 강제 이동 fixture의 아군은 Empty ActorId로 수정해 강제 이동 제외를 계속 검증한다.
- 임시 prefab을 옛 `enemyTurnSeconds: 0.37` 형식으로 다시 가져와 새 `MovementTurnSeconds = 0.37` 인수를 실제 Unity 직렬화로 확인했다. 임시 prefab은 finally에서 삭제했고 독립 프로젝트에 남지 않았다. 코드의 새 기본값으로 기존 설정을 덮어쓰지 않음을 확인한 증거다.
- `git diff --check` 통과. Core/Buffer/Flow/Job/FieldCamera/Architecture/DB/Render 수정·커밋·push 없음. 원본 Unity Test Runner/Combat 화면 확인과 M3 담당은 사용자를 유지한다.

### 5.6 M3 사용자 확인 완료 (2026-10-13)

사용자가 “M3 확인 끝났어. 아군·적 이동 회전이랑 도착 후 방향 복귀 잘 돼. 완료로 기록해줘.”라고 보고했다. 아군·적 이동 회전과 최종 도착 후 기존 방향 복귀의 정상 동작을 사용자 Unity 화면 확인 완료로 기록하고 M3 및 작업 상태를 완료로 갱신했다. 원본 Unity Test Runner 실행 여부나 세부 재검증 항목별 로그는 전달되지 않아 별도 실행 완료로 기록하지 않는다. 이전 빌드·독립 Unity 173건 검증 증거는 §5.5를 유지한다. 이번에는 문서만 갱신했으며 코드·씬 변경, 추가 테스트 실행, 커밋·push는 하지 않았다.
