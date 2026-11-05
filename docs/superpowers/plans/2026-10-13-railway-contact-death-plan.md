# 기차 접촉 시점의 기존 사망 연출 — Task 계획

작성일: 2026-10-13. 작성: Codex / View 작업. 상태: **C1~C3 코드 반영, 별도 Unity 검사 완료. 원본 Unity Test Runner·육안 확인 대기. C4 미착수**.

정책 원본: [접촉 사망 설계](../specs/2026-10-13-railway-contact-death-design.md). 기존 작업: [Railway View 계획](2026-10-12-railway-view-plan.md). 사용자 요청 범위는 기차 앞부분 도달 시 기존 사망 연출 시작이며 새 사망 모션은 포함하지 않는다.

## 1. 실행 규칙

- 설계 확인 후 Phase별로 진행하고 **각 Phase 완료 후 보고·정지**한다. 다음 Phase는 사용자 진행 지시 후 시작한다.
- 구현 전에 기존 환경 폭발 작업의 View/테스트 변경을 다시 읽는다. 같은 파일의 동시 편집을 피하고 기존 변경을 보존한다.
- 코드 이름은 아래 책임을 나타내는 예정 이름이다. 책임·공개 계약·시간/접촉 정책이 달라지면 구현 전에 묻는다.
- 신규 `.cs`는 `.meta`와 함께 관리하고 기존 GUID를 유지한다. 기존 테스트 어셈블리를 사용한다.
- 커밋·push는 사용자가 한다. 독립 빌드, 원본 Unity EditMode, 실제 화면 확인을 서로 다른 증거로 기록한다.

## 2. Phase C1 — 등속 이동·앞 기준점·예약 계산

목표: 열차의 이동과 접촉 예약을 동일한 경로/Duration에서 도출한다. 이 Phase에서는 실제 사망 재생 연결은 아직 하지 않는다.

- [x] C1.1 기존 Railway 경로·프리팹·애니메이션·관련 테스트의 기준선을 확인한다. 미커밋 변경을 작업 전 기록한다.
- [x] C1.2 Train 래퍼에 FrontContact를 추가하고 RailwayTrainVisualBinding으로 명시적으로 연결한다. Animator가 움직이는 계층과 분리하고 보정/배율을 받는지 검증한다.
- [x] C1.3 FieldRailwayPresenter의 기존 시작/끝 값을 공유 가능한 읽기 전용 경로 값으로 제공한다. 등속 이동으로 바꾸고 Clip은 외형만 평가한다. ClipEndZ 비사용 설명을 코드/Tooltip에 반영하되 직렬화 값은 보존한다.
- [x] C1.4 RailwayContactSchedule과 `.meta` 추가. 기존 표시 중심·FrontContact·공유 경로로 시간표를 계산하고 안정적으로 정렬한다. 여러 선로 후보는 가장 이른 도달 가능한 시각을 선택한다.
- [x] C1.5 이전/현재 재생 시간 전달 접점을 만들고 마지막 Tick의 시간 전달이 열차 정리보다 먼저 일어나는지 확인한다. 추가 시계·별도 Update는 만들지 않는다.
- [x] C1.6 순수 시간표 테스트: 거리/속도 계산, ID 역순, 동시 접촉, Reverse, 가로/세로, 여러 칸/여러 선로, 비정상 입력, 초기 접촉, 종료 뒤 미도달.
- [x] C1.7 기존 FieldRailwayPresenterTests의 Clip 루트 진행률 기대값을 새 등속 계약으로 갱신한다. Clip 유무에 따른 동일 이동, 회전/배율/두 표시 모드, 첫·반복 발동/취소 회귀와 프리팹·DataBaseAssetTests를 실행한다. **별도 Unity 엔진 검사 실행. 원본 Test Runner는 에디터 사용 중으로 실행하지 못했으며, 기존 DB 지형 좌표 실패는 아래 기록과 같이 남아 있다.**
- [x] C1.8 변경·검증·기존과 달라진 속도감을 보고하고 정지한다. 육안 속도감 확인은 대기다.

대상: `Display/FieldRailwayPresenter.cs`, `Display/RailwayTrainVisualBinding.cs`, 신규 `Display/RailwayContactSchedule.cs`, `RailwayVisualDataSO.cs`의 설명/비사용 계약, `Prefabs/Stage/Railway/Train.prefab`, 관련 EditMode/에셋 테스트.

리뷰 기준: 이동 위치와 예약 시간 계산이 같은 경로/Duration을 사용한다. 실제 프리팹의 앞부분을 기준점이 가리킨다. 아직 통과 후 사망인 중간 상태임을 분명히 보고한다.

## 3. Phase C2 — 기존 사망 연출의 다중 대상 수명

목표: 기존 사망 모션을 그대로 사용하면서 서로 다른 유닛이 독립적으로 사망 연출을 시작할 수 있게 한다.

- [x] C2.1 CombatUnitReactionSet과 `.meta` 추가. EntityId별 기존 CombatUnitReactionPresenter를 관리한다. 같은 유닛 중복 시작은 거부하고 완료한 항목은 닫는다.
- [x] C2.2 CombatSceneEventPresenter의 단일 반응 수명 접근을 Set으로 교체한다. 기존 일반 BeginEvent는 여전히 반응 완료를 기다린다.
- [x] C2.3 일반 Died와 접촉 Died가 공통 사망 시작 코드를 사용하도록 정리한다. 기존 자세/프로필/효과음/HP/DeathPresentationHold/fallback을 보존한다.
- [x] C2.4 좁은 동시 사망 입구를 추가한다. 다른 유닛의 사망과만 중첩을 허용하며 이동/공격/피격·동일 대상 중복은 거부한다.
- [x] C2.5 Tick, IsPresentationComplete, Refresh 전제조건, Cancel/Clear/Release의 전체 반응 정리를 연결한다. 새 반응에 시작 전 deltaTime을 주지 않는다.
- [x] C2.6 검증: 서로 다른 두 유닛의 겹친 사망/독립 완료, 효과음 1회, 표시 facts 적용 1회, 유지권 반환, 재대여 후 늦은 콜백 무효, 반복 취소, 한 대상 정리 실패 시 나머지 정리.
- [x] C2.7 기존 CombatUnitReactionTests/LifecycleTests, CombatSceneEventPresenterTests, 공격·이동·발리스타·박격포 회귀를 실행한다. 일반 단일 사건의 시작/완료 순서가 바뀌지 않아야 한다. **별도 Unity 검사에서 새 실패 없음. 기존 실패 3건과 원본 Test Runner 대기는 아래 기록 참조.**
- [x] C2.8 보고 후 정지한다.

대상: 신규 `Display/CombatUnitReactionSet.cs`, `CombatSceneEventPresenter.cs`, 필요한 최소 범위의 기존 단일 반응 코드/테스트. Core나 사망 프로필 값은 바꾸지 않는다.

리뷰 기준: 사망 알고리즘이 한 곳에 있고, 별도 기차 모션·별도 오디오 경로가 없다. 기존 일반 사건의 직렬 대기를 유지한다.

## 4. Phase C3 — 접촉 예약과 실제 사건 재생 연결

목표: 기차가 유닛별 기준 위치에 도달한 프레임부터 기존 사망 연출을 시작한다.

- [x] C3.1 CombatRailwayContactPlayback과 `.meta` 추가. 결과 안에서 Railway와 바로 뒤의 직접 Died 구간을 검증해 연결한다. 최신 Buffer나 최종 위치를 읽지 않는다.
- [x] C3.2 SceneDisplay.BeginPresentation에서 준비, Railway BeginEvent에서 발동, 기존 열차 진행 시간 전달에서 예약 소비를 연결한다. 사망 시작 콜백은 조립 시 주입한다.
- [x] C3.3 예약 시각을 지난 모든 대상을 시작한다. 전역 반응 Barrier 때문에 다음 접촉이 지연되지 않도록 전용 입구로만 전달한다.
- [x] C3.4 열차 정리/모든 예약/모든 사망 완료를 묶어 원래 Railway 완료 콜백을 한 번 호출한다. 0명·즉시 fallback·완료 재진입도 검증한다.
- [x] C3.5 원본 순회가 미리 표시한 정확한 Died 항목을 만났을 때 중복 없이 완료한다. 다른 사건 및 영혼/깃발/보유 환경/승패는 원래 순서를 유지한다.
- [x] C3.6 ICombatPresentationControl.cs의 관련 순서 보장 주석에 제한된 Railway 묶음 정책을 명시한다. OrderedCombatPresentation/CombatResultPlayback에는 Railway 계산을 넣지 않는다.
- [x] C3.7 Cancel 세대 무효화 → 반응/열차 정리 → 필드/표시 해제 순서를 연결한다. 남은 묶음을 전체 완료/Refresh 검사에 포함한다.
- [x] C3.8 기존 통과 후 사망 테스트를 새 계약으로 교체하고 아래 인수 사례를 실제 SceneDisplay/Ordered 경로로 검증한다.
- [x] C3.9 원본 Unity 컴파일·관련 EditMode 결과와 한계를 보고하고 정지한다. **외부 빌드·별도 Unity 검사 완료. 원본 에디터를 사용 중이므로 원본 Test Runner와 육안 확인은 대기다.**

대상: 신규 `Combat/CombatRailwayContactPlayback.cs`, 기존 `CombatSceneDisplay.cs`, `CombatSceneEventPresenter.cs`의 좁은 연결, `ICombatPresentationControl.cs`의 주석, 필요한 열차 진행 연결/테스트.

### C3 인수 사례

| 사례 | 기대 |
|---|---|
| 접촉 직전/도달/직후 | 직전은 생존 표시, 도달 프레임에 기존 사망 시작, 열차는 계속 이동 |
| 유닛 ID와 거리 순서 반대 | ID 순서에 막히지 않고 거리로 계산한 예약 순으로 시작 |
| 가까운 두 명·동일 시각 여러 명 | 첫 사망 종료를 기다리지 않고 각각 시작 |
| 큰 deltaTime·최종 Tick | 건너뛴 예약 모두 1회 시작, 새 반응에 접촉 이전 전체 시간을 부여하지 않음 |
| 여러 선로/Reverse/여러 칸 | 공유 경로/표시 중심 기준, 중복 사망 없음 |
| 0명·프로필 없는 대상 | 0명도 열차 완료 대기, fallback은 기존 즉시 적용이며 교착 없음 |
| 사망이 열차보다 늦게 끝남 | 사망 완료 전 후속 정산/결과 확인 없음 |
| 사망이 열차보다 먼저 끝남 | 열차 완료 전 후속 정산/결과 확인 없음 |
| 적·아군·메인 | 원본 대상 그대로, 메인 사망 후 결과 완료와 기존 이탈 경계 유지 |
| 사망 이후 깃발·영혼·승패 | 직접 사망 이외의 사건을 앞당기거나 누락하지 않음 |
| 목록/사건 불일치 | 조용히 타이밍을 포기하지 않고 시작 전 실패 |
| 취소·재진입·반응/Effect 정리 실패 | 성공 통지 없음, 나머지 정리 시도, 다음 결과/새 모델 오염 없음 |
| 일반 공격·이동·환경 폭발 | 기존 순서·효과음·표시 유지, 환경 폭발 작업 보존 |

## 5. Phase C4 — 실제 화면 검증·문서 인계

- [ ] C4.1 Database + Live에서 적·아군을 레일의 앞/중간/끝에 두고 반복 발동을 확인한다. Fixture/RecordInputOnly를 운영 발동 검증으로 대신하지 않는다.
- [ ] C4.2 두 표시 모드에서 열차 앞 기준점과 유닛 표시 중심의 접촉 순간, 기존 사망 모션, 겹친 사망, 효과음, 긴 차체의 진입/퇴장을 확인한다.
- [ ] C4.3 메인 사망·실제 승패·포기 대기·오류 정지·씬 재진입의 완료/정리를 확인한다. 기존 포기/승패 규칙은 변경하지 않는다.
- [ ] C4.4 접촉 기준 또는 속도감이 요구와 다르면 근거와 수정안을 보고한다. 메시 표면 판정·새 모션·공통 속도 설정을 임의 추가하지 않는다.
- [ ] C4.5 현재 Railway 가이드와 기존 설계/계획에 새 정책 링크와 적용 완료 범위만 연결한다. 상세 정책은 새 설계, 실행 증거는 이 계획에만 둔다.
- [ ] C4.6 최종 SSOT·S/O/L/I/D·FW 체크와 변경 파일 경계, Unity 확인 여부를 보고하고 정지한다.

리뷰 기준: 실제 화면 확인 없이 시각적 접촉 검증 완료로 기록하지 않는다. 실행 환경이 없으면 해당 검증을 대기로 남기고 코드 검사와 구분한다.

## 6. 원칙 검증의 완료 조건

- [ ] 사망 대상/상태/승패는 기존 Core/Buffer만 원본이다.
- [ ] 이동·시간표에 별도 속도 원본/다른 시계/다른 경로 계산이 없다.
- [ ] 사망 모션·효과음·표시 적용·모델 유지/반환이 기존 공통 경로를 사용한다.
- [ ] S: 이동·시간표·묶음·반응 수명·표시 facts·조립 책임이 분리돼 있다.
- [ ] O: 일반 실행기/단일 모션 내부에 Railway 종류 분기를 추가하지 않았다.
- [ ] L: 완료 1회, 취소 무통지, 일반 순서/대기, 오류 전파 계약을 검증했다.
- [ ] I: 각 구성에 필요한 값/콜백/기존 인터페이스만 전달한다.
- [ ] D: 접촉 담당이 구체 씬/서비스/Presenter를 찾아 호출하지 않는다.
- [ ] JMH/FW: 새 System/범용 스케줄러/Pool 없이 기존 Tick과 서비스/Scope 수명을 사용한다.
- [ ] Core/다른 작업자 영역에 구현 변경이 없다. 필요한 예외는 사전 협의/인계 대상이다.

## 7. 실행 기록

### 2026-10-13 — 설계·Task 작성

- 현재 열차, 직접 사망 사건 생성, 단일 반응 수명, 표시 중심, 기존 문서와 Framework 계약을 읽고 계획을 작성했다.
- 이번 변경은 새 설계와 계획 문서 두 개뿐이다. 코드/에셋 변경과 테스트 실행은 하지 않았다.
- 네 Phase 모두 미착수다. 사용자 검토 후 첫 구현 범위는 **C1만**이다.

### 2026-10-13 — C1 구현과 검증

사용자 지시: **“C1 진행해”**. 시작 HEAD `cbaa0fa4`, 작업 트리 깨끗함. 환경 폭발 변경은 이 커밋에 포함됐으며 그대로 보존했다.

**변경**

- 불변 `RailwayTravelPath`를 추가해 기존 설치 경로의 시작/끝, 앞 기준점, Duration을 공유한다. 이동과 접촉 시간은 이 값에서 계산하며 새 속도 설정/시계/Update는 없다.
- `RailwayContactSchedule`은 대상별 불변 점유 칸/로컬 표시 중심을 받아 가장 이른 도달 가능한 선로의 시간을 구하고 `(시간, ID)`로 정렬한다. 대상/상태 판정이나 Buffer 접근은 없다. C3가 나중에 Expected/CenterOf를 이 입력으로 전달한다.
- Train 래퍼의 ModelRoot 형제에 FrontContact를 연결했다. 실제 메시를 Unity에서 읽어 확인한 래퍼 앞면 `z=15.448300`, 바닥 `y=0.2`를 사용했다. 기존 테스트용 Train_Placeholder에도 같은 필수 계약으로 기준점(`z=1.3`)을 추가했다. GUID·원본 Source/FBX/Clip은 유지했다.
- Clip/Controller는 외형만 재생한다. 루트 보정·그래프·이펙트 정리는 유지한다. ClipEndZ는 직렬화 호환용으로 보존하되 이동에 사용하지 않는다.
- `Begin`의 선택적 진행 콜백은 시작의 `(0,0)`과 이동 직후 이전/현재 시간을 전달한다. 마지막 시간 전달 뒤 열차를 정리한다. 취소/해제/진행 콜백 실패는 기존 성공 통지를 무효화한다.
- **실제 사망 표시 연결은 변경하지 않았다. 지금은 등속 열차가 통과한 뒤 기존 사망 사건을 재생한다.**

**검증 방법과 한계**

- 원본 프로젝트 `-runTests`는 다른 에디터 인스턴스 사용 중으로 실행되지 않았다. 사용자 에디터를 종료하거나 잠금 파일을 지우지 않았다.
- VS 2022 MSBuild로 Systems·Framework 테스트 어셈블리를 컴파일했다. 새 세 파일만 `Temp/RailwayContactC1/NewFiles.targets`로 추가하며 생성된 csproj는 수정하지 않는다. 최종 컴파일 오류 0, C1 파일의 새 경고 0.
- 원본과 같은 Unity 6000.0.73f1의 별도 프로젝트에서 컴파일된 어셈블리와 복제 리소스를 사용해 NUnit Test/TestCase 메서드의 SetUp/Test/TearDown을 실행했다. 원본 Unity Test Runner 실행이나 실제 Combat 육안 검증으로 간주하지 않는다. 라이선스 IPC 접근은 승인된 실행 권한으로 해결했고 원본 에디터에는 손대지 않았다.
- 변경 전 82 통과/1 실패. 변경 후 **최종 104 통과/1 실패**(두 표시 모드 도달 검증 포함). FieldRailwayPresenterTests 43/43, RailwayTrackLayoutTests 21/21, RailwayContactScheduleTests 15/15, DataBaseAssetTests 25 통과/1 기존 실패다.
- 두 실행에서 동일한 기존 실패: `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`, Stage 1101 지형 좌표 인덱스 5가 예상 `(7,3)` / 실제 `(6,3)`이다. C1은 해당 DB/Core를 수정하지 않았다.
- 증거: `Temp/RailwayContactC1/baseline-build.log`, `baseline-unity.log`, `baseline-native.txt`, `build-final.log`, `final-native.txt`, `probe-final.log`. 검증 프로젝트/스크립트는 Temp 안에만 있다. 새 GUID 유일성·UTF-8/LF·문서 상대 링크 검사와 diff 공백 검사를 통과했다.

**원칙 리뷰**

SSOT는 기존 경로/Duration/단일 경과 시간과 불변 파생 예약으로 유지했다. S는 설치·이동/경로 값/시간표의 분리, O는 단일 사망 모션·일반 실행기 무변경, L은 기존 Begin 완료·Cancel 무통지·예외 전파 유지, I는 경로 값·대상 값·선택적 시간 콜백만 전달, D는 스케줄 계산에 씬/서비스 구체 의존 없음으로 점검했다. FW-001~004의 기존 조립/소유 경계, FW-011/014의 기존 EffectScope 반환, FW-016의 기존 Tick·정리 순서를 유지했다. Core/Buffer/Flow/FieldCamera/Architecture/Render 내부 변경은 없다.

**다음 리뷰 지점**

C1에서 정지한다. C2는 기존 단일 사망 반응을 EntityId별 수명으로 관리하고 기존 모션·효과음·모델 유지권을 재사용하는 단계다. 대상은 `CombatUnitReactionSet` 신규와 `CombatSceneEventPresenter`의 반응 수명 연결이며, 그 시점의 환경 효과 등 동시 변경을 다시 확인한다. 일반 사건의 직렬 대기를 유지하는 회귀·동시 사망·취소/재대여 검증이 필요하다. C2를 마쳐도 실제 Railway 접촉 연결은 C3다.

### 2026-10-13 — C2 구현과 검증

사용자 지시: **“C2 진행해”**. 시작 시 C1의 미커밋 변경을 보존했다. 이번 Phase에서 기존 환경 효과 콜백과 단일 반응 모션 코드는 변경하지 않았다.

**변경**

- `CombatUnitReactionSet`이 EntityId별 기존 `CombatUnitReactionPresenter`의 시작/진행/완료/취소를 소유한다. 피격은 배타적이며 서로 다른 사망만 겹칠 수 있다. 완료 항목을 닫은 뒤 완료 콜백을 호출한다.
- `CombatSceneEventPresenter.BeginConcurrentDeath`는 아직 표시하지 않은 생존 → 사망 사건만 받아 기존 `PresentEvent`/`TryBeginDeath` 경로를 사용한다. 모션·프로필·오디오·HP·모델 유지권·프로필/모델이 없는 즉시 fallback이 공유된다.
- 일반 `BeginEvent`, 이동, 공격, Refresh와 결과 Barrier는 전체 반응 완료를 계속 기다린다. 이동/공격/피격 도중의 동시 사망과 같은 유닛 중복은 거부한다. 발리스타의 기존 접촉/잔여 비행 정책은 유지한다.
- Tick 시작의 반응 시작 번호와 목록 스냅샷으로 새 반응에 이전 시간을 넘기지 않는다. 완료 콜백 재진입은 허용하되 목록을 중첩 Tick할 수 없다.
- 취소는 항목을 먼저 무효화한 뒤 모든 자세·유지권 반환을 시도하고 오류를 모아 전달한다. 이전 항목의 콜백/취소 핸들이 동일 ID의 새 대여를 건드리지 않는다. Cancel/Clear/Release의 기존 해제 경계를 사용한다.
- C2 신규 런타임 파일은 Set 하나이며 기존 런타임 수정은 EventPresenter 하나다. 신규 Set 테스트와 기존 사망/PlayMode 수명 테스트를 확장했다. **기차 접촉 예약을 실제 사망에 연결하는 C3는 시작하지 않았다.**

**검증 방법과 결과**

- VS 2022 MSBuild: Systems·Framework 테스트 어셈블리 오류 0, C2 변경 파일 새 경고 0. 신규 파일만 `Temp/RailwayContactC2/NewFiles.targets`로 보충했다. 생성된 csproj는 수정하지 않았다.
- C1과 같은 별도 Unity 6000.0.73f1 프로젝트에서 Test/TestCase의 SetUp/Test/TearDown을 실행했다. 변경 전 **187 통과/3 실패**, 변경 후 **204 통과/동일한 3 실패**. 신규 EditMode 사례 17건은 모두 통과했다.
- 포함한 회귀: CombatUnitReactionTests/AssetTests, CombatUnitDeathTests, CombatSceneEventPresenterTests, CombatAttackPresenterTests, CombatMovementViewTests, CombatStandardAttackTests, CombatBallistaAttackTests, CombatMortarAnimationTests/PresentationTests. 일반 사망 두 건의 순차 완료·결과 대기, 발리스타 비행 중 반응, 콜백 재진입을 포함한다.
- 기존 실패 3건은 모두 `CombatSceneEventPresenterTests`: `CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent(false/true)`의 가시성 콜백 기대값 2건, `WarmFactRefresh_AllocatesLessThanLegacyProjection_AndReusesViews`의 할당량 비교(양쪽 0) 1건이다. 변경 전후 이름·기대값·실제값이 동일하며, 원본 Test Runner에서 재확인이 필요하다. 이번 범위에서 기존 테스트의 기대값을 완화하지 않았다.
- 별도 프로젝트의 **실제 PlayMode**에서 UnityTest 열거자 5건을 실행해 **5/5 통과**했다. 반응·사망·일반 공격·박격포 비활성화 정리, 신규 동시 사망의 한 대상 비활성화 → 두 유지권 반환 → 재대여 후 두 사망 정상 완료를 포함한다. 검사용 실행기는 PlayMode 진입 뒤 Enter/ExitPlayMode 지시를 건너뛰고 테스트 본문을 실행한다. 원본 Unity Test Runner 결과와 구분한다.
- 원본 Unity 에디터(PID 20952)가 프로젝트를 계속 사용 중임을 확인했다. 원본 에디터/잠금 파일에 손대지 않았으며 원본 Test Runner·Combat 육안 확인은 대기다.
- 증거: `Temp/RailwayContactC2/build.log`, `baseline-full-unity.log`, `baseline-native.txt`, `final-unity.log`, `final-native.txt`, `play-unity.log`, `play-native.txt`. 검사용 프로젝트/실행기는 Temp에만 있다.
- C2 파일의 UTF-8/LF, 신규 GUID 유일성, diff 공백 및 문서 링크를 점검했다. 커밋·push하지 않았다.

**SSOT·SOLID·FW 리뷰**

SSOT는 기존 사망 프로필/단일 모션/사건 facts/오디오/유지권 경로를 그대로 재사용한다. S는 유닛별 수명(Set)과 단일 자세(Presenter), 표시 사실(EventPresenter)의 분리다. O는 단일 모션·일반 순회에 Railway 분기를 추가하지 않는 확장, L은 일반 직렬 대기·완료 1회·취소 무통지·실패 전파의 유지로 확인했다. I는 좁은 동시 사망 입구와 기존 Pose/콜백, D는 기존 오디오/자세 계약과 생성자 조립을 유지한다. FW-001~004/011/014/016의 기존 서비스·표시 대여·Tick·해제 순서를 사용하며 새 System/Pool/스케줄러는 없다. Core/Buffer/Flow/FieldCamera/Architecture/Render 내부 변경도 없다.

**리뷰 지점**

C2에서 정지한다. 다음 C3는 결과의 Railway 직접 Died 구간과 C1 예약을 대응하고, 접촉 때 이 동시 사망 입구를 호출한 뒤 열차와 모든 사망의 완료를 함께 기다리는 연결이다. 현재 게임의 기차 사망 시점은 여전히 통과 후다.

### 2026-10-13 — C3 접촉 재생 연결과 검증

사용자 지시: **“C3 진행해”**. 시작 시 C1·C2 미커밋 변경을 보존했다. Core의 현재 생성 순서(Railway → 직접 Died → 후속 정산)를 읽기만 했으며 Core를 수정하지 않았다.

**변경**

- `CombatRailwayContactPlayback`을 추가했다. 결과 준비 때 모든 Railway의 바로 뒤 Died 구간과 KilledUnitIds를 1:1 검증하고 기존 시간표를 만든다. 누락·중복·다른 ID·생존→사망이 아닌 사건·중간에 끼어든 다른 사건은 열차 시작 전에 실패한다. 뒤쪽 묶음도 첫 발동 전에 검증한다.
- 접촉 위치는 원본 Died의 **Expected + 기존 FieldUnitPresenter.CenterOf**다. `FieldGridMapper.WorldToLocal`의 단순 역변환 접점을 추가해 C1 경로와 같은 좌표계로 바꾸며, 중심/속도/시계를 다시 계산하는 구현은 만들지 않았다.
- SceneDisplay 생성자에서 기존 사망 입구와 열차 Begin 콜백을 주입한다. BeginPresentation은 준비, BeginEvent는 원본 Railway 시작·이미 재생한 정확한 사망 항목 소비만 맡는다. 범용 Ordered/ResultPlayback 구현은 바꾸지 않았다.
- 열차 이동 직후 전달되는 경과 시간으로 도달한 예약을 모두 시작한다. SceneDisplay의 기존 반응 → 열차 Tick 순서를 유지해 신규 접촉에 이전 deltaTime을 넘기지 않는다. ID 역순·동시 접촉·가까운 두 대상 모두 앞 사망이 끝나길 기다리지 않는다.
- 열차 정리 완료·모든 예약 시작·모든 사망 완료를 함께 기다린다. 원본 순회에서는 미리 표시한 동일 Died 객체만 소비하며 HP·오디오·모션·외부 효과를 반복하지 않는다. 후속 영혼/깃발/보유 환경/승패 사건은 원본 순회에 그대로 남긴다.
- 전체 결과 Barrier와 Refresh는 준비/진행/미소비 묶음을 포함한다. 열차 중 일반 사건·직접 이동 호출은 거부한다. CancelMovement를 포함한 취소/설치/해제에서는 접촉 세대를 먼저 무효화하고 기존 반응·열차·필드를 정리한다.
- 기차 묶음 재생/정리 실패 때 나머지 반응·열차 정리를 시도하고 예외를 전파한다. SceneDisplay의 표시 세대로 마지막 사망 완료 콜백이 시작한 다음 열차에 이전 프레임 시간을 주거나 이전 실패로 취소하지 않도록 했다. 비Railway의 기존 실패 처리 정책은 유지한다.
- 인터페이스 주석에 Railway 직접 사망의 제한된 접촉 순서 예외를 명시했다. 기존 단일 모션/프로필/오디오, 환경 폭발 구현, C1 열차 구현 및 C2 반응 구현은 이번 Phase에서 수정하지 않았다.

**검증**

- VS 2022 MSBuild로 Systems·Framework 테스트 어셈블리 컴파일: 오류 0, C3 파일의 새 경고 0. 신규 파일만 `Temp/RailwayContactC3/NewFiles.targets`로 보충하며 생성된 csproj는 수정하지 않았다.
- 같은 Unity 6000.0.73f1 별도 프로젝트의 Test/TestCase 실행: 변경 전 **276 통과/3 실패**, 최종 **314 통과/동일한 3 실패**. 신규 묶음 검사 17건 + 실제 SceneDisplay/Ordered/Result 검사 21건 = **38건 전부 통과**.
- 실제 씬 검사: 접촉 직전/직후 HP와 활성 모델, ID와 거리 순서 반대, 가로/세로·Reverse·두 표시 모드, 인접/동시 사망, 여러 선로에 걸친 여러 칸 대상, 큰 deltaTime/마지막 Tick, 0명/프로필 없음, 열차보다 이른/늦은 사망 완료, 후속 환경 효과·결과 완료 대기, 효과음 각 1회, Cancel/Clear/Release·재설치·재대여를 확인했다.
- 실패·재진입 검사: 목록 불일치는 출발/오디오 전 실패, 오래된 열차/사망 콜백 무효, 여러 묶음 분리, 반응 반환 실패에도 다른 반응/열차 이펙트 반환, 열차 이펙트 반환 실패에도 새 사망 전부 취소, 마지막 사망 완료가 다음 열차를 시작하고 예외를 던져도 새 열차 보존을 확인했다.
- 기존 회귀는 C2의 사망/피격·공격·이동·발리스타·박격포 검사에 FieldRailwayPresenterTests/RailwayContactScheduleTests/CombatEnvironmentEffectPresenterTests를 더한 범위다. 실제 Database + Live Core의 첫/반복 발동 테스트도 통과했다. 이는 자동 검사이며 C4의 실제 플레이 육안 인수나 메인 사망 이후 화면 이탈 확인을 대체하지 않는다.
- 남은 3건은 C2와 동일한 `CombatSceneEventPresenterTests`의 가시성 콜백 2건(false/true), 할당량 비교 1건이다. 변경 전후 실패 이름·기대값·실제값이 동일하다. 원본 Unity Test Runner에서의 확인은 남아 있다.
- 최초 새 씬 검사 fixture에서 FieldView가 허용하지 않는 비균일 배율/기울기를 넣은 오류를 수정했다. 실제 씬 검사는 허용된 Y축 회전·단위 배율을 사용하며 Mapper의 일반 변환 검증과 구분한다. 또한 실제 Catalog의 View Data가 공유되는 것을 확인해 모든 임시 설정/프로필을 TearDown에서 원복하게 했다. 원본 DB 에셋 파일은 변경하지 않았다.
- 원본 프로젝트는 기존 Unity 에디터가 사용 중이므로 원본 에디터를 종료하거나 잠금을 지우지 않았다. 별도 프로젝트의 컴파일된 어셈블리/복제 리소스 검사이며 원본 Test Runner·원본 Combat 육안 검증과 구분한다.
- 증거: `Temp/RailwayContactC3/build-final.log`, `baseline-unity.log`, `baseline-native.txt`, `final-unity.log`, `final-native.txt`. 검사 fixture 수정 이력은 `first-native.txt`, `second-native.txt`에 남겼다. 신규 GUID·UTF-8/LF·문서 링크·diff 공백 점검을 통과했다.

**SSOT·SOLID·FW 리뷰와 범위**

SSOT: 대상/사망 사실은 원본 사건, 경로/시간은 C1의 불변 경로와 열차 시계, 자세/HP/오디오/유지권은 C2 공통 경로를 사용한다. S: 묶음 대응/예약 소비만 새 담당에 두고 SceneDisplay는 조립·수명 경계를 맡는다. O: 일반 실행기·단일 모션에 Railway 분기를 추가하지 않았다. L: 일반 순회와 완료/취소/실패 계약을 유지하고 제한된 순서 예외를 주석·테스트로 드러냈다. I: 사망 시작·열차 시작 콜백과 필요한 경로/Mapper 값만 전달한다. D: 접촉 담당이 씬/서비스/구체 Presenter를 검색하지 않는다. FW-001~004/011/014/016: 새 서비스/Pool/별도 Update 없이 기존 소유권·Tick·반환 경계를 따른다.

Core/Buffer/Flow/FieldCamera/Architecture/Render 내부·원본 FBX/애니메이션·운영 DB 에셋 변경은 없다. 커밋·push하지 않았다. **C3에서 정지하며 C4는 미착수**다. 다음 리뷰는 실제 Database + Live에서 앞 기준점과 유닛 표시 중심의 접촉 체감, 모션 겹침, 메인 사망/포기/이탈/재진입을 확인하는 단계다.
