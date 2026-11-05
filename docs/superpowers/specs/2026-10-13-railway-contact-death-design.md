# 기차 접촉 시점의 기존 사망 연출 설계

작성일: 2026-10-13. 작성: Codex / View 작업. 상태: **사용자 C1~C3 진행 승인, C1~C3 코드 반영. C4 미착수**. 검증 증거와 남은 확인은 Task 계획을 따른다.

사용자 요청: 기차 앞부분이 각 유닛에 닿는 순간 현재 사망 연출을 시작한다. 찌그러짐·밀림·새 사망 모션은 이번 범위에 포함하지 않는다. 구현 Task·검증 상태는 [접촉 사망 계획](../plans/2026-10-13-railway-contact-death-plan.md)만 원본으로 삼는다.

## 1. 설계 당시 구현과 대체 계약

- `CombatGimmickCalculator`는 필드의 살아 있는 유닛 중 레일과 점유 칸이 겹치는 대상의 HP를 0으로 만든다. 진영 필터는 없다.
- 결과는 Railway 사건(`KilledUnitIds` 포함), ID 순서의 직접 사망 사건, 후속 정산 사건 순서로 생성된다. `CombatCommandEvents.Health`는 치명적인 체력 변경을 별도 HealthChanged 없이 **Died 하나**로 기록한다.
- `CombatSceneDisplay.BeginEvent`는 Railway를 `FieldRailwayPresenter.Begin`으로 보내고 열차 전체 통과를 기다린다. 이후 `OrderedCombatPresentation`이 사망 사건을 하나씩 재생한다.
- 열차의 최종 이동은 코드가 담당하지만, 현재 Clip의 루트 Z를 진행률로 사용하는 경로가 있다. 등속 시간표를 사용하려면 이 이동 정책도 명시적으로 바꿔야 한다.
- `CombatSceneEventPresenter`는 `_reaction` 하나와 전역 반응 대기를 사용한다. 이를 그대로 호출하면 앞 유닛의 사망이 끝날 때까지 다음 유닛이 기다리므로 접촉 시점에 맞출 수 없다.
- 기존 사망은 `CombatUnitReactionPresenter.BeginDeath`의 쓰러짐·정지 시간과 `FieldUnitPresenter.HoldDeathPresentation`으로 표시한다. 새 연출을 복사하지 않고 이 구현을 재사용한다.

대체한 기존 정책은 [Railway 설계](2026-10-12-railway-view-design.md) §2·6.2·7의 **통과 후 사망 표시 및 Clip 루트 진행률**이다. C1은 등속 이동·앞 기준점·시간표 계산, C2는 다중 반응 수명, C3는 실제 접촉 재생을 연결했다. 현재 코드에서는 아래 접촉 정책으로 사망을 표시한다. 선로 도출, 정적 표시, 원본 리소스 보존, 자원 반환 정책은 이어받는다.

설계 당시 작업 트리에는 환경 폭발 작업의 `CombatSceneDisplay`, `CombatSceneEventPresenter`, 관련 테스트 변경이 있었다. C1 시작 시 이 변경은 `cbaa0fa4`에 포함돼 작업 트리는 깨끗했다. 해당 작업을 보존하며 C1에서는 두 Presenter를 수정하지 않았다.

## 2. 제안하는 화면 동작

1. Railway 발동 시 각 열차를 코드로 등속 이동시킨다.
2. 발동 전에 계산한 접촉 시각이 되면 해당 유닛의 **기존 사망 자세·효과음·HP 표시 변경**을 시작한다.
3. 열차는 계속 이동한다. 다른 유닛의 사망 연출도 각 접촉 시각에 시작하며 서로 기다리지 않는다.
4. 열차 통과와 해당 사망 연출들이 모두 끝나면 Railway 묶음을 완료한다.
5. 이미 표시한 직접 사망 사건은 일반 순회에서 다시 표시하지 않는다. 영혼·깃발·보유 환경·승패 등 나머지 사건은 원래 순서로 이어간다.

Core는 기존처럼 발동 때 규칙 결과를 확정한다. View는 그 결과를 시간차로 표시한다. 중간에 Core HP를 다시 쓰거나 판정을 늦추지 않는다. 결과 확인과 다음 진행은 기존 `CombatResultPlayback` 경계를 유지한다.

### 2.1 이번 버전의 접촉 위치

- Train 래퍼의 `RailwayTrainVisualBinding`에 직렬화된 **FrontContact** Transform 참조를 추가한다. 기차 앞부분에 놓고 움직이는 애니메이션 본 아래에는 두지 않는다. 원본 Source/FBX/Clip은 수정하지 않는다.
- 기준점은 래퍼의 모델 보정과 크기를 함께 받되, 루트 애니메이션에 의해 별도로 이동하지 않아야 한다. 시작 때 실제 보정·회전·배율이 반영된 위치를 이동과 같은 좌표계로 변환한다.
- 유닛 측 기준은 **사망 사건 Expected의 기존 표시 중심**이다. 기존 `FieldUnitPresenter.CenterOf(OccupiedCells, Anchor, mapper)`를 재사용한다. 한 칸 유닛은 타일 중심, 여러 칸 유닛은 현재 표시와 같은 점유 칸 중심이다. 사망 Final이나 최신 Buffer에서 위치를 찾지 않는다.
- 즉, 첫 버전은 **기차 앞 기준면이 유닛의 배치 중심에 도달하는 시점**이다. 모델 메시 표면끼리의 정밀 충돌을 보장하는 설계는 아니다. 실제 화면에서 이 기준으로 접촉이 자연스러운지 마지막 Phase에서 확인한다. 유닛별 충돌 반경·추가 기준점이 필요하면 범위 변경으로 먼저 논의한다.
- 여러 선로에 걸친 유닛은 Expected 점유 칸과 해당 선로의 교집합으로 후보 선로를 찾고, 후보별 도달 시간 중 가장 빠른 값을 한 번 사용한다. 이는 이미 Core가 정한 사망 대상의 표시 시간 계산이며, 사망 대상 선정이 아니다.

## 3. 이동과 예약 시간

### 3.1 이동 설정의 원본

이번 제안에서는 기존 `RailwayVisualData.Duration`을 전체 통과 시간의 유일한 입력으로 유지한다. 별도 Speed 필드를 추가하지 않는다. 선로마다 경로 길이가 다르면 도출 속도는 달라진다. 모든 선로를 같은 속도로 만들려면 Duration 대신 속도를 원본으로 삼는 별도 결정이 필요하다.

```text
S = 기존 경로에서 도출한 이동 루트 시작 위치
E = 기존 경로에서 도출한 이동 루트 종료 위치
D = Duration
u = normalize(E - S)
L = length(E - S)
v = L / D
F0 = 시작 배치에서의 FrontContact 위치
P = 유닛의 표시 중심

열차 위치(t) = S + u * v * clamp(t, 0, D)
접촉 시각 = dot(P - F0, u) / v
```

모든 위치·거리·속도는 동일한 필드 로컬 좌표계에서 계산한다. Mapper의 칸 간격·필드 회전·비균일 배율을 다시 추정하지 않는다. 기존 시작/끝 도출을 공유하는 읽기 전용 경로 값으로 이동과 예약을 계산한다. 별도의 경로 생성 코드를 만들지 않는다.

Clip/Controller는 기존 선택 규칙대로 외형만 재생한다. 루트 위치·회전·배율 복원, 수동 PlayableGraph, Root Motion 비활성화는 유지한다. Clip의 Z나 ClipEndZ는 이동·접촉 시간의 입력으로 사용하지 않는다. 기존 `clipEndZ` 직렬화 값은 이번에는 보존하되 코드·Tooltip·가이드에 비사용을 명시하고 새 설정으로 제시하지 않는다. 원본 Clip을 삭제하거나 수정하지 않는다.

### 3.2 시간표와 Tick

- 예약은 발동 전에 한 번 구성하고 `(접촉 시각, EntityId)` 순으로 정렬한다. 같은 시각의 대상은 같은 프레임에 모두 시작한다.
- `FieldRailwayPresenter`의 기존 경과 시간이 열차 재생 시간의 원본이다. 별도 Time.time, Stopwatch, Coroutine, Invoke, Task.Delay, 트윈 시계를 만들지 않는다.
- 열차 이동 직후, 정리로 상태를 비우기 전에 그 경과 시간을 접촉 재생 담당에 전달한다. 접촉 담당은 다음 예약부터 `예약 시각 <= 현재 시각`인 항목을 모두 시작한다. 매 프레임 유닛 위치·거리·콜라이더를 검사하지 않는다.
- 기존 반응 Tick을 먼저 수행하고 이번 Tick의 신규 접촉을 시작한다. 방금 시작한 사망에 접촉 이전 프레임 전체 deltaTime을 다시 주지 않는다. 새 반응은 다음 Tick부터 진행한다. 표시 정밀도는 렌더 프레임 단위이며 서브프레임 화면을 약속하지 않는다.
- 프레임 지연으로 여러 시각을 건너뛰어도 예약을 누락하지 않는다. 마지막 Tick에서도 예약을 먼저 처리하고 열차 정리를 완료한다.
- 시작부터 기준면이 대상을 지난 부분 선로는 `t=0`으로 처리하는 초기 접촉 정책을 둔다. 현재 운영 경로에서는 모든 대상보다 앞에서 시작하는지 에셋/화면 검사로 확인한다. 종료 시각 뒤에만 도달하는 대상, 일치 선로 없음, 잘못된 위치/시간은 오류로 진단하고 통과 후 일괄 사망으로 숨기지 않는다.
- `Duration`, 위치, 도출 속도, 예약 시각의 유한성·양수 조건을 검사한다. 입력은 발동 동안 고정하며 실행 중 경로·속도 변경은 이번 범위 밖이다.

## 4. 사건 연결과 책임 분리

### 4.1 Railway 직접 사망 묶음

새 `CombatRailwayContactPlayback`이 결과에서 Railway 발동과 바로 뒤의 직접 Died 사건을 묶는다. 결과를 수정하거나 전체 사건을 재정렬하지 않는다.

- `KilledUnitIds`는 대상 ID 원본이며 Died 사건의 Expected/Final은 그 대상의 변경 사실 원본이다. 두 값을 임의로 합성하지 않는다.
- 현재 Core가 만드는 **Railway 바로 뒤의 직접 Died 구간**에서 ID를 1:1 대응한다. 다른 종류 사건이나 다음 발동을 넘어 임의로 검색하지 않는다. 0명은 빈 묶음이다.
- 누락·중복 ID, 생존→사망이 아닌 사건, 일치하지 않는 직접 Died는 표시 시작 전에 실패시킨다. Core 계약이 달라졌다면 먼저 원인을 확인하고, 필요한 Core 변경은 인계 요청으로 남긴다.
- 접촉 시 해당 원본 Died를 공통 사망 표시 입구에 전달한다. 모델 유지권 획득 → 기존 효과음/표시 사실 적용 → 기존 사망 연출 → 유지권 반환의 경로를 공유한다.
- 완료 뒤 원본 순회가 그 Died에 도달하면 **같은 결과 안에서 미리 처리한 정확한 사건 항목**만 완료 처리한다. 유닛 ID만으로 후속 사건을 무시하지 않는다. 효과음·HP·외부 효과 호출·자세 재생을 반복하지 않는다.
- 영혼·깃발·보유 환경·승패는 앞당기지 않는다. 다른 유닛에 영향을 줄 수 있는 사건을 무조건 접촉 예약으로 옮기지 않는다.

기존 `ICombatPresentationSequencePresenter`/`ICombatPresentationBarrier` 주석의 순서 보장은 이 변경에서 명시적으로 정정한다. **원본 순회와 후속 정산 순서는 유지하며, Railway에 귀속된 직접 사망의 표시만 해당 발동 안에서 접촉 순으로 재생한다.** 일반 사건의 BeginEvent 완료·대기 계약을 몰래 완화하지 않는다.

### 4.2 구성별 역할

| 구성 | 책임 / 받는 값 |
|---|---|
| `RailwayTrainVisualBinding` | 프리팹 내부의 명시적 FrontContact·ModelRoot·Animator 참조 및 검증 |
| `FieldRailwayPresenter` | 기존 설치/경로/등속 이동/외형/EffectScope 수명. 불변 경로 값과 진행 시간을 제공. 유닛 사망을 직접 처리하지 않음 |
| 새 `RailwayContactSchedule` | 경로 값·접촉점·사건의 표시 위치로 예약 시간 계산/정렬. Buffer·씬 검색·서비스 접근 없음 |
| 새 `CombatRailwayContactPlayback` | 결과 안의 사건 대응, 예약 소비, 먼저 표시한 항목, 열차와 사망의 묶음 완료/취소. 사망 자세·HP·효과음 구현은 소유하지 않음 |
| 새 `CombatUnitReactionSet` | EntityId별 반응 실행기 수명/진행/정리. 기존 `CombatUnitReactionPresenter`를 구성으로 재사용하고 사망 모션을 복제하지 않음 |
| `CombatSceneEventPresenter` | 기존 표시 facts·오디오·사망 적용 경로. 일반 사건과 접촉 사망이 공통 사망 처리 함수를 사용 |
| `CombatSceneDisplay` | 위 구성 조립, 기존 사건 입구 라우팅, Tick/Cancel/Clear/Release 연결만 수행 |
| 기존 Ordered/Result Playback | 원본 결과 순회·전체 결과 수명·확인. Railway 규칙이나 거리 계산을 추가하지 않음 |

`CombatSceneEventPresenter`에는 좁은 접촉 사망 입구를 둔다. 예: `BeginConcurrentDeath(CombatCharacterEvent, Action completed)`. 일반 BeginEvent의 대기 계약은 유지하며, 접촉 입구는 다른 유닛의 사망과만 동시 실행을 허용한다. 공격·이동·피격과의 임의 중첩, 같은 대상의 반응 중복은 거부한다. 프로필이 없는 경우는 기존 즉시 표시 fallback을 유지한다.

접촉 담당은 생성 때 주입받은 사망 시작 콜백만 호출한다. 전체 SceneDisplay, BufferGroup, 오디오 서비스나 전투 상태를 받지 않는다. 열차 경로는 필요한 읽기 전용 값으로 넘긴다. 반응 Set은 단일 대상 Presenter의 취소/완료 계약을 감싸며, 일반 피격/사망 호출자도 이 Set을 사용한다. 별도의 기차 전용 사망 구현을 만들지 않는다.

## 5. 완료·취소·실패

- Railway 사건 완료 조건은 **모든 예약 시작 + 열차 통과/정리 완료 + 시작한 모든 사망 반응 완료**다. 유닛이 없는 경우도 열차는 끝까지 재생한다.
- 완료 판단과 통지는 접촉 묶음 담당 한 곳에서 한다. 전체 결과 재생 여부나 마지막 결과 번호는 복제하지 않는다.
- 즉시 완료 fallback·동시 완료·완료 콜백 재진입에서도 중복 완료가 없어야 한다. 시작 중인 구간에는 완료를 유예하고, 내부 상태를 닫은 뒤 한 번 통지한다.
- Cancel은 묶음 세대와 콜백을 먼저 무효화한 뒤 예약·반응·열차를 정리한다. 씬의 취소 순서도 이 순서를 따른다. 성공 완료는 호출하지 않는다.
- 반응마다 모델 유지권과 자세를 각각 반환한다. 한 대상 정리가 실패해도 나머지 대상·열차·EffectScope 정리를 시도하고 오류를 합쳐 기존 경계로 전달한다.
- Clear/Release/Install 실패에서도 유닛 표시와 필드 부모를 제거하기 전에 반응·열차를 정리한다. 늦은 콜백이 다음 결과·재대여된 모델에 영향을 주지 않도록 기존 세대/대여 식별 보장을 유지한다.
- `IsPresentationComplete`와 Refresh 전제조건에 묶음/모든 반응의 잔여 상태를 반영한다. 메인 사망·정상 이탈·포기의 기존 결과 대기는 유지한다.

## 6. SSOT·SOLID 5원칙·JMH/FW 점검

기준은 [프로젝트 적용 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [Framework 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)이다.

| 기준 | 이번 설계의 보장 / 리뷰 시 확인 |
|---|---|
| SSOT — 규칙 | 사망 대상은 KilledUnitIds, 변경은 원본 Died, 진행 상태는 Core/Buffer. View의 충돌 판정으로 대상을 추가하거나 HP를 쓰지 않음 |
| SSOT — 위치/시간 | 경로는 기존 RailCells/Mapper에서 한 번 도출. 표시 중심 계산은 기존 CenterOf 공유. Duration 하나에서 속도·예약을 도출. 재생 시간은 열차 Presenter 하나 |
| SSOT — 연출/수명 | 기존 반응 프로필·사망 실행기·DeathPresentationHold 재사용. 예약/소비 커서는 현재 결과의 파생 표시 상태이며 새 게임 상태 저장소가 아님 |
| S — 단일 책임 | 이동, 순수 시간표, 사건 묶음, 반응 수명, 표시 사실, 씬 조립을 위 표대로 분리. SceneDisplay에 수학·사망 자세 코드 금지 |
| O — 개방/폐쇄 | 새 묶음/시간표가 기능을 담당하고 기존 사건 입구에 연결. 일반 실행기나 단일 대상 모션에 Railway 분기 없음. 모든 파일 무수정이 O의 뜻은 아니며, 반응 수명 확장과 라우팅 접점은 명시적으로 변경 |
| L — 리스코프 치환 | 일반 BeginEvent의 완료 1회/취소 무통지/예외 전파 유지. 병렬 입구를 별도로 제한하고 기존 Barrier의 일반 대기를 약화하지 않음. 순서 보장 주석의 Railway 예외를 공개하고 테스트 |
| I — 인터페이스 분리 | 시간표는 값만, 접촉 담당은 사망 시작 콜백만, 기존 반응은 ICombatUnitPose만 받음. 목적 없는 서비스·큰 인터페이스 추가 없음 |
| D — 의존성 역전 | 접촉 조정이 구체 EventPresenter를 찾아 부르지 않고 조립 시 주입된 좁은 동작에 의존. 기존 IEffectSystemServices/IAudioSystemService/ICombatUnitPose 유지. 운영 리플렉션·전역 검색·구현 캐스트 없음 |
| JMH §2/23/25~28 | 기존 Combat View 수명 아래의 표시 객체로 구성. Query에 상태 저장 금지. 프레임별 Find/GetComponent/LINQ 정렬 금지. 실제 책임이 있는 세 구성만 분리하고 범용 스케줄러/기믹 레지스트리/새 Pool은 만들지 않음 |
| FW-001~003 | 기존 서비스·모듈·Builder 사용. 새 System 등록이나 Build 후 등록 변경 없음 |
| FW-004/011/014 | Buffer/Render/Pool 내부 우회 없음. 열차 Effect는 기존 Scope.StopAll, 유닛은 기존 유지권/대여 수명으로 정리 |
| FW-012 | FrontContact 필수 참조 검증, 기존 Railway DB/프리팹 검증 확장. 새 Render Effect 키나 테이블 없음 |
| FW-016 | 설치/바인딩 후 기존 Tick 사용, 반응/열차 정리 후 표시/부모 해제. 실패·재진입 포함 검증 |
| FW-005~010/013/015 | 입력/UI/메시/이벤트 버스/JobQueue 정책 변경 없음. 별도 이벤트 재발행·작업 큐를 만들지 않음 |

## 7. 범위와 승인할 결정

이 계획의 선택은 **Duration 유지 + 코드 등속 이동**, **기차 FrontContact → 유닛의 기존 표시 중심**, **동시 사망은 기존 모션 재사용**, **후속 정산은 묶음 완료 뒤 원래 순서**다. 기존 가감속이 사라지므로 실제 열차의 체감 속도는 달라질 수 있다. 속도감 조정은 Unity 확인에서 하되 설정 원본 변경은 별도 합의를 따른다.

Core/Buffer/Flow/Job/LifeCycle, FieldCamera, Architecture, Render 내부, 원본 애니메이션·FBX는 변경하지 않는다. 새 피격/압착/카메라 효과도 추가하지 않는다. Core 사건이 이 묶음 계약을 만족하지 않는 실제 사례가 나오면 임의 대응 없이 Core 인계 문서에 근거와 요청을 기록한다.
