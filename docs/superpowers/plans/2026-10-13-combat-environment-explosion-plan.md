# 화약통·지뢰 BOOM 연결 설계 및 Task 계획 — 기존 EffectScope 재사용

- 작성: View 작업, 2026-10-13
- 상태: **E2 사건·전투 표시 연결 구현 완료, 검증 결과는 §7.3. E2 리뷰 지점.** E1의 기존 배율 재사용 문제와 원본 Unity 화면 확인은 미해결로 유지한다. E3 미착수.
- 사용자 방향: 기존 `EffectScope`·`ParticleStopNotifier`를 다시 확인한 결과를 바탕으로 계획을 세운다.
- 이 문서가 이번 작업의 설계·Task·검증 상태 원본이다. 삭제한 이전 설계와 E1 구현은 채택하지 않는다. **아래 E1은 새 계획의 BOOM 등록 단계다.**
- 각 Phase 완료 후 보고하고 멈춘다. 다음 Phase는 사용자 진행 지시 후 시작한다. 계획 밖 변경이 필요하면 먼저 검토를 요청한다.
- 커밋·push·외부 발송은 사용자가 한다. Core·FieldCamera·Architecture 및 옛 Missing Script 에셋은 수정하지 않는다.

## 1. 목표와 설계 경계

화약통 파괴와 지뢰 발동 사건에서 `Assets/ProjectL/Script/Render/Effect/Boom/BOOM.prefab`을 재생한다. 두 사건은 같은 Render DB 항목과 기존 EffectSystem/EffectPool을 사용한다.

**기존 `EffectScope.Play`와 `StopAll`만 사용한다.** `EffectScope`, `ParticleStopNotifier`, `EffectPool`, `EffectSystem` 구현은 수정하지 않는다. 새 Scope·재생 인터페이스·풀·수명 타이머·파티클 관측 Tick을 만들지 않는다.

폭발은 사건 시점에 시작하고 이후 피해·사망 사건은 기존 순서대로 진행한다. 폭발 종료를 기다리도록 사건 완료나 전투 결과 barrier를 변경하지 않는다. 정상적인 자연 종료는 기존 Notifier 콜백이 반환한다. 취소·필드 교체·씬 해제 때 남은 효과는 `StopAll`로 정리한다. 따라서 **폭발 중 씬을 나가면 남은 연출은 중단될 수 있다.** 끝까지 재생한 뒤 이탈하는 정책은 이번 범위가 아니다.

## 2. 현재 코드에서 확인한 근거

경로 기준: `Assets/ProjectL/Script/Systems/RuntimeSystem/`.

| 코드·에셋 | 확인한 사실 | 적용 |
|---|---|---|
| `GlobalSystem/RenderSystem/Core/EffectSystem/EffectScope.cs` | Play가 풀 대여·배치 후 `Notifier.Play(() => Stop(node))`를 호출한다. Stop은 대여 목록에서 제거하고 소유 풀로 반환한다 | 기존 재생·자연 반환 사용 |
| 같은 폴더 `ParticleStopNotifier.cs` | Awake에서 루트 PS의 stopAction을 Callback으로 설정. Play(true)로 자식도 재생. 종료 콜백을 한 번 전달 | 새 종료 감시 기능을 만들지 않음 |
| 같은 폴더 `EffectPool.cs` | 생성 시 루트 ParticleSystem/Notifier를 사용. 반환 시 비활성화·부모 복원·방출 중단 | BOOM 루트에 기존 Notifier를 연결 |
| `InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs` | 이미 IEffectSystemServices를 주입받으며, 필드 CellsRoot·Mapper·정리 경계를 보유 | 기존 조립 지점 사용 |
| 같은 폴더 `CombatSceneEventPresenter.cs` | PresentEvent에서 실제 사건을 표시하고 완료를 통지. 두 환경 사건을 수락한다 | 이 지점에서만 폭발 재생 호출 |
| `CoreCaculator/Contracts/Combat/CombatEvent.cs` | 화약통 반응과 Object 발동 사건이 별도로 존재 | 상태 차이를 추측하지 않고 사건 해석 |
| `CoreCaculator/Application/Combat/ObstacleReactionCalculator.cs` | 반응의 Origin.PhysicalCell에 장애물 Anchor를 기록 | 피해자·공격자 위치를 사용하지 않음 |
| `CoreCaculator/Application/Combat/MovementEnvironmentCalculator.cs` | Object 발동 → 원본 변경 → 피해 순서로 사건을 기록 | 제거된 지뢰 View를 찾아 위치를 얻지 않음 |

Core 경로는 `InGameSystem/CoreSystem/` 아래이며 읽기 전용으로 확인했다.

BOOM에는 현재 루트 ParticleSystem이 있고 Notifier는 없다. 루트는 활성 상태이며, 전체 계층은 비루프·1회 Burst 설정이다. 입자 수명은 루트 3초, Brick 3초, Smoke 1~1.5초(지연 0.05초), Fire 0.3~0.7초, Wave 0.5초다. 루트 Duration 1초를 효과 전체 수명 1초로 해석하지 않는다.

`Play(withChildren: true)`는 자식을 함께 재생한다는 계약이다. 루트 종료 콜백이 임의의 모든 프리팹에서 자식 종료까지 보장한다고 가정하지 않는다. 반대로 현재 BOOM이 잘린다고도 단정하지 않는다. **현재 에셋을 기존 경로로 재생해 확인하는 것이 E1의 검증 과제**다. 설정 조사와 실제 Unity 재생 확인을 구분한다.

## 3. 설계 결정

### 3.1 BOOM 등록

- 원본 BOOM 루트에 **기존 `ParticleStopNotifier` 컴포넌트만 추가**한다. 파티클 값·계층·재질·프리팹 GUID는 유지한다.
- 빈 루트 PS를 덧붙이는 래퍼는 만들지 않는다. 현재 루트 PS의 종료 콜백을 그대로 사용한다.
- `EffectType`의 기존 Empty=0/TestFire=1을 유지하고 `Boom=2`를 추가한다.
- `Assets/ProjectL/DataBaseSO/RenderGroup/EffectTable/Items/BoomEffect.asset`을 추가하고 기존 `EffectDataTable.asset`에 한 번 등록한다. Prefab은 원본 BOOM을 참조한다.
- 초기 대여 준비 수는 0, MaxCount 설정값은 16으로 시작한다. 현재 Pool이 MaxCount를 런타임 동시 재생 상한으로 강제한다고 가정하지 않는다. 개수 제한 구현은 이번 범위가 아니다.
- `EffectScope.Play`는 요청 회전으로 루트 회전을 덮어쓴다. 요청은 필드 로컬 칸 중심, BOOM의 기존 축에 맞춘 로컬 X −90° 회전을 사용한다. 이 배치 규칙은 공통 Presenter 한 곳에 둔다. 높이 보정은 우선 0이며, 숨은 수명·크기 보정값을 추가하지 않는다.
- 부모 배율에 따른 크기는 기존 Play의 SetParent 동작을 따른다. 원하는 외형과 맞는지는 E1에서 확인한다. 배율을 보정하기 위해 공용 Scope를 수정하지 않는다.

### 3.2 사건 해석

| 입력 사건 | 재생 조건 | 재생 칸 |
|---|---|---|
| CombatObstacleReactionEvent | ContentKind == PowderBarrel, Reaction == Destroyed | Origin.PhysicalCell |
| CombatObjectActivationEvent | Source가 MineSourceFact | Source.Cell |

Hit만 발생한 화약통, 보급품·다른 장애물, 깃발 등 다른 Object, 단순 원본 변경·체력·사망 사건에서는 재생하지 않는다. 피해 대상 수와 관계없이 해당 발동 사건마다 1회다. 같은 칸에 서로 다른 발동 사건이 있으면 각 사건을 재생하며, 칸이나 AttackGroup으로 임의 중복 제거하지 않는다.

`CombatExplosionRules.cs`에 화약통과 지뢰의 순수 해석 함수를 분리한다. 각 함수는 `CombatEvent → CellCoord?`이며 대상이 아니면 null을 반환한다. Core 계산·InGame DB 조회·Unity 객체 접근은 하지 않는다.

### 3.3 공통 재생 담당과 조립

`Combat/Display/CombatEnvironmentEffectPresenter.cs`를 추가한다. 이 클래스는 사건 해석 결과를 기존 `EffectScopeRequest`로 바꾸고, 소유한 Scope에 Play/StopAll을 전달한다.

- 생성 인수: 기존 `IEffectSystemServices`, 표시 부모 `_field.CellsRoot`, 효과 키 `EffectType.Boom`, 읽기 전용 해석 함수 목록.
- `CombatSceneDisplay`에서 화약통·지뢰 함수 두 개를 고정 목록으로 조립한다. 매 사건마다 목록을 만들거나 전역 등록소를 추가하지 않는다.
- 재생 함수는 두 해석 결과를 먼저 확인한다. 매칭 0개면 아무것도 하지 않고, 1개면 한 번 재생한다. 2개 이상이면 구성 오류로 보고하며 여러 번 재생하지 않는다.
- Scope는 첫 재생에서 한 번 발급받고 같은 Display 수명 동안 재사용한다. 사건별·Refresh별로 새 Scope를 만들지 않는다. 철도 Scope와 공유하지 않는다.
- 필드 구성 성공 후 Mapper를 Bind한다. 위치는 `FieldGridMapper.CellCenterToLocal`로 구하고 CellsRoot 아래에 배치한다. 원본 장애물·지뢰 View가 없어져도 재생 위치는 사건 값으로 결정된다.
- EventPresenter에는 기존 패턴과 같은 좁은 `Action<CombatEvent>` 호출을 주입한다. Effect 서비스·프리팹·풀을 EventPresenter가 직접 다루지 않는다.
- 호출 위치는 `PresentEvent`의 기존 PlayAudio와 세대 검사 뒤, 사건 표시 완료 이전이다. 폭발 요청 후에도 `_presentationVersion`을 확인해 재진입 취소 시 이전 완료 콜백이 실행되지 않도록 한다.
- `CanPresent`, Refresh, CanBeginPresentationEvent에서 재생하지 않는다. 실제 PresentEvent에서만 호출한다.

해석 목록은 실제 두 규칙의 재사용 지점이다. 별도 규칙 인터페이스·새 서비스 키·범용 이벤트 버스·Factory 계층은 추가하지 않는다. 다른 폭발 사건을 지원할 때 해석 함수와 조립 목록을 확장하면 공통 재생 담당은 바꾸지 않는다.

### 3.4 수명·정리

| 시점 | 환경 효과 처리 |
|---|---|
| 발동 사건 표시 | 기존 Scope.Play. 기존 사건 완료 흐름 계속 |
| 자연 종료 | 기존 Notifier → Scope.Stop → EffectPool 반환 |
| 결과 완료·일반 Refresh | 별도 정리·대기 없음. 재생 중 효과는 자연 종료 가능 |
| CancelPresentation | Scope.StopAll. Mapper 연결은 유지 |
| Install의 필드 재구성 전 | StopAll 후 Mapper 연결 해제 |
| Install 실패 | 기존 RunAll 정리 목록에 포함해 가능한 정리 모두 시도 |
| Clear | StopAll·Mapper 해제. 이후 재설치 시 기존 Scope 재사용 |
| Release | StopAll·Mapper 해제·추가 재생 차단. 반드시 필드 부모 해제보다 먼저 처리 |

기존 EffectScope에는 공개 Dispose/등록 해제가 없다. Scope 객체는 EffectSystem이 Shutdown까지 관리한다. 이 계획은 씬 해제 때 **활성 효과를 StopAll로 반환**하며, Scope 등록 목록 누적 문제까지 해결한다고 주장하지 않는다.

Play/StopAll 예외는 기존 오류 경계로 전달하고, 새 Presenter가 성공으로 삼키지 않는다. 기존 RunAll의 실패 격리 방식에 정리를 연결한다. 기존 Scope 내부의 부분 시작 실패·반환 실패 보장을 이번 작업에서 확장하지 않는다. 실재 문제가 재현되면 증거와 최소 변경안을 별도로 보고한다.

## 4. SSOT·SOLID·JMH/FW 점검

기준: [View 설계 원칙 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md).

| 기준 | 이번 적용·판정 |
|---|---|
| SSOT — 발동·위치 | Core가 기록한 사건만 사용. View의 체력 비교·충돌 감지·피해 재계산 없음 |
| SSOT — 프리팹·수명 | Render DB의 Boom 항목 하나, 파티클 설정은 BOOM, 대여·반환은 기존 Scope/Pool. View에서 프리팹 복제·완료 카운터·수명 타이머를 보관하지 않음 |
| S | 해석 함수는 대상/칸, 공통 Presenter는 배치/호출/소유 수명, Notifier는 종료 통지, Scope/Pool은 기존 자원 관리. EventPresenter에는 전달 접점만 추가 |
| O | 공용 Render 재생 구현을 수정하지 않고 에셋·enum 등록·소비자를 추가. 폭발 사건 추가는 해석 함수·조립 목록으로 확장 |
| L | 기존 Scope의 Play/StopAll 계약을 그대로 사용. 외부 완료 조회·공개 Dispose·반환 실패 원자성을 보장한다고 가정하지 않음. 해석 함수는 비대상 null/대상 1칸의 동일 계약 |
| I | EventPresenter에는 사건 전달 Action만 주입. 전체 RenderGroup·서비스 저장소·풀 노드를 넘기지 않음. 사용처 하나인 재생 인터페이스를 만들지 않음 |
| D | 기존 IEffectSystemServices로 Scope를 얻는다. 공용 API가 반환하는 EffectScope를 정상적으로 사용하고 구현 캐스트·씬 서비스 검색·리플렉션을 운영 코드에 넣지 않음. 해석 함수는 조립 시 주입 |
| JMH §2/23/25~28 | 기존 책임과 수명 재사용, Query에 런타임 상태 저장 금지, Find/GetComponent로 서비스 탐색 금지, 실제 재사용 지점만 분리 |
| FW-001~004 | 기존 서비스/모듈/Builder 유지. 내부 elements·노드·풀 상태 우회 접근 없음 |
| FW-011 | 자연 반환과 명시 StopAll 사용. View에서 대여 GameObject를 Destroy하거나 외부 풀로 반환하지 않음 |
| FW-012 | 유일한 EffectType, non-null 프리팹, 루트 PS/Notifier, 유효한 풀 개수, 기존 Query 연결을 에셋 검증으로 확인 |
| FW-014 | 기존 CustomPool의 콜백/반환 순서를 그대로 사용. 공용 Pool은 수정하지 않음 |

공용 enum 추가와 BOOM 컴포넌트 연결은 Framework/Render 담당 공유 문서에 기록한다. 공유 문서 작성과 외부 전달을 구분한다.

## 5. Phase별 Task

### Phase E1 — 기존 경로로 BOOM 등록·재생 확인

선행 충족: 사용자 “E1 작업 진행해”(2026-10-13).

- [x] **E1.1** BOOM 루트에 기존 Notifier만 연결하고 원래 파티클 설정·GUID가 유지됐는지 diff로 확인.
- [x] **E1.2** EffectType.Boom, BoomEffect.asset 및 기존 EffectDataTable 등록. 새 에셋 `.meta` 포함.
- [x] **E1.3** DataBaseAssetTests에 Boom 키의 유일성·정확한 프리팹 참조·루트 요구조건을 검증하는 항목 추가 후 실행.
- [x] **E1.4** 실제 서비스의 GetEffectHandle/Play 경로를 Unity에서 실행. 루트 콜백 시 자식의 가시 입자·잔여 trail이 잘리는지, 반환되는지, 재사용되는지 확인. 반환 시점만 따로 추정하는 타이머를 만들지 않음.
- [ ] **E1.5** 반복·동시 재생, 재생 중 StopAll 및 재사용, 필드 회전/배율, 요청 회전 X −90°의 외형 확인. **일반 배율의 엔진 검증 통과, 부모 배율 3에서 두 번째 재생 크기 검증 실패. 육안 확인 대기.** 기존 EffectScope 구현은 그대로 사용.
- [x] **E1.6** Framework/Render 공유 문서에 enum·프리팹·DB 변경과 확인 결과 기록(미전달). 결과 보고 후 정지.

**리뷰 기준:** 기존 공용 코드 수정 없이 BOOM 재생·자연 반환·재사용이 확인돼야 한다. 잘림·고착·오류가 재현되면 E2에 진입하지 않고 재현 조건과 최소 수정 선택지를 보고한다. 별도 Scope, 빈 루트 래퍼, 임의 수명 연장, Notifier 수정으로 조용히 우회하지 않는다. Unity 확인이 불가능하면 해당 Task를 검증 대기로 남긴다.

### Phase E2 — 사건 해석·전투 표시 연결

선행: E1 결과 보고 뒤 사용자 “E2 진행해”(2026-10-13). 이 지시로 E2 연결만 진행하며, E1.5 배율 문제를 해결·승인 완료로 간주하거나 공용 구현을 수정하지 않는다.

- [x] **E2.1** CombatExplosionRules의 화약통·지뢰 순수 해석 함수와 긍정/부정 사건 테스트 작성.
- [x] **E2.2** CombatEnvironmentEffectPresenter 작성. 기존 서비스/Scope, Mapper, 표시 부모를 사용하며 함수 목록을 주입. 신규 `.cs`의 `.meta` 포함.
- [x] **E2.3** CombatSceneDisplay에서 조립·Bind·Cancel·Clear·Release 연결. 필드 해제 전 StopAll 순서와 Install 실패 정리를 확인.
- [x] **E2.4** CombatSceneEventPresenter의 실제 PresentEvent에 한 번 전달. 오디오·스냅샷·완료 호출의 기존 순서를 보존하고 재진입 세대 검사 유지.
- [x] **E2.5** 사건당 1회, 비대상 0회, 순서·위치, 두 해석기의 같은 효과 사용, 추가 해석 함수 주입, 중복 매칭 오류, 결과 완료가 효과 종료를 기다리지 않는 동작 테스트.
- [x] **E2.6** 설치 전/해제 후 재생 거절, 취소·Clear 반복, 재설치, Install 실패, 연속 결과/Refresh에서 효과가 중복·강제 종료되지 않는지 검증.
- [x] **E2.7** 관련 CombatSceneEventPresenterTests·CombatPresentationCleanupTests·FieldRailwayPresenterTests 회귀 실행. 기존 실패가 있으면 변경 전 기준선과 구분. 코드·검증 결과 보고 후 정지. **전체 통과를 뜻하지 않으며 실패·검증 한계는 §7.3에 기록.**

**리뷰 기준:** 공용 Render 4개 구현 파일의 diff가 없어야 한다. 새 Tick/완료 대기·Core 변경이 없어야 하며 두 발동 조건에서만 재생된다.

### Phase E3 — 실제 전투 검증·인계

선행: E2 리뷰 후 사용자 진행 지시.

- [ ] **E3.1** 화약통 Hit 무효과/Destroyed 폭발, 지뢰 발동 폭발, 지뢰→화약통 및 화약통 여러 개 연쇄 확인.
- [ ] **E3.2** 피해 대상이 여러 명이어도 발동 사건당 한 번, 피해가 없어도 발동 사건이 있으면 한 번, 같은 칸 별도 발동은 각각 재생 확인.
- [ ] **E3.3** 가장자리·숨겨진 지뢰·원본 View 제거 후 위치, 필드 회전/배율, 연속 턴·여러 결과에서 외형과 재사용 확인.
- [ ] **E3.4** 폭발 중 취소·정상 이탈·자발적 포기·재진입 확인. 기존 이탈 정책은 유지하고 실제 정리 시 잔여 효과가 반환되는지 확인. 폭발 완주를 기다리는 조건은 추가하지 않음.
- [ ] **E3.5** 원본 프로젝트 Unity 컴파일·관련 EditMode/실제 파티클 재생·DB 에셋 검사 결과를 출처와 함께 기록. 빌드와 실제 화면 확인을 구분.
- [ ] **E3.6** 실행 기록과 Framework 공유 문서를 갱신하고 기존 View 계획에 이 문서 링크·최종 상태만 연결. 보고 후 정지.

## 6. 변경 허용 범위

| 영역 | 예정 파일 |
|---|---|
| 에셋·키 | BOOM.prefab(Notifier 연결만), 기존 EffectType.cs, 신규 BoomEffect.asset/meta, 기존 EffectDataTable.asset |
| 전투 View | 신규 Display/CombatExplosionRules.cs, Display/CombatEnvironmentEffectPresenter.cs 및 meta, 기존 CombatSceneDisplay.cs·CombatSceneEventPresenter.cs |
| 검증 | DB 에셋 검사, 새 환경 효과 테스트, 기존 사건/정리 회귀의 필요한 주입 부분 |
| 문서 | 이 계획, E1의 공용 변경 공유 메모, E3 완료 시 기존 View 계획 링크 |

`EffectScope.cs`, `ParticleStopNotifier.cs`, `EffectPool.cs`, `EffectSystem.cs`, CustomPool, Buffer, Core, Flow/Job/LifeCycle, 결과 barrier, FieldCamera, Architecture는 변경 대상이 아니다. 새 Render/View DB 테이블·Scope API·씬의 직렬화 프리팹 참조도 만들지 않는다.

## 7. 실행 기록

### 7.1 2026-10-13 — 기존 구조 기준으로 계획 재작성

- 사용자가 이전 작업·문서를 폐기한 뒤 EffectScope 재확인 및 계획 재작성을 요청했다.
- 현재 코드의 Play→Notifier→Stop→Pool 반환, BOOM 파티클 설정, 사건·좌표·조립·정리 접점 및 SSOT/SOLID/JMH/FW 기준을 확인했다.
- 이 계획 문서만 작성했다. 운영 코드·씬·에셋 수정 없음. 이번 계획 작성에서 빌드·Unity 테스트를 실행하지 않았다.
- 이전 폐기 구현의 빌드/테스트 기록을 이 계획의 완료 증거로 사용하지 않는다. E1~E3는 모두 미착수다.

### 7.2 2026-10-13 — E1 BOOM 등록 및 기존 경로 검증

**승인:** 사용자 “E1 작업 진행해”. E1만 진행했다. **E1 전체 완료로 처리하지 않는다.** E1.5의 배율 문제와 화면 확인을 남기고 리뷰 지점에서 멈춘다. E2/E3·커밋·push·외부 발송 없음.

#### 변경

- BOOM 루트에 기존 ParticleStopNotifier 연결. 루트 참조 1줄과 MonoBehaviour 블록만 추가했다. 이를 제거한 내용이 HEAD의 원본과 일치한다는 비교 결과 `OnlyNotifierAdded=True`를 확인했다.
- EffectType.Boom=2, BoomEffect.asset/meta(초기 0/MaxCount 16), 기존 EffectDataTable 참조 1건 추가.
- `DataBaseAssetTests.Boom_RenderGroupRegistersOriginalPrefabWithRequiredRootComponents` 추가. 실제 RenderGroup 조립으로 중복 키/누락을 검출하고 등록된 원본·루트 컴포넌트·비루프 계층·설정·enum 호환을 검사한다.
- `BoomEffectPlaybackTests.cs`/meta 추가. 두 UnityTest는 원본 에셋·실제 EffectSystem/Scope를 사용한다. 일반 배율의 재생/반환/동시/취소/재사용과 배율 3 부모의 반복 재생을 구분한다. 테스트용 OnDisable 관측 컴포넌트는 반환 시 입자 수를 읽을 뿐 수명에 개입하지 않는다.
- `git diff --check` 통과. 새 C#/에셋 meta 포함, 변경 파일 LF 확인.
- [공용 공유 문서](../handoffs/2026-10-13-combat-environment-effect-framework-share.md) 작성, 미전달.

#### 검증 결과와 한계

| 검증 | 결과 |
|---|---|
| 관리 빌드 | VS 2022 MSBuild, ProjectL.Tests.Framework 및 의존 프로젝트 성공. 오류 0, 변경 파일 새 경고 0. 기존 타 영역 경고는 유지 |
| 신규 DataBaseAssetTests 1건 | 실제 RenderGroup·BOOM으로 통과. 전체 DataBaseAssetTests 실행 결과로 확대하지 않음 |
| 일반 배율 자연 반환 | 원본 BOOM을 Play로 2회 재생. 루트·네 자식 모두 실제 방출, 같은 인스턴스 재사용, 반환 시 잔여 입자 0, poolRoot로 복귀 확인 |
| 동시·중단·재사용 | 동시에 3개 재생, StopAll 2회 후 활성 자원 없음, 재대여 후 다시 방출 확인 |
| 위치·회전 | 회전된 부모 아래 요청 localPosition 및 X −90° localRotation 일치 |
| 부모 배율 3 | 첫 자연 재생/반환 성공. 두 번째 재생에서 lossyScale 유지 assertion 실패(거리 1.15470076, 기준 0.001) |
| Trail | 원본 PS 5개 모두 TrailModule 비활성. 반환 시 잔여 입자 확인과 별도 타이머를 혼동하지 않음 |
| 공용 코드 보존 | EffectScope/ParticleStopNotifier/EffectPool/EffectSystem diff 없음. 원본 BOOM의 PS·재질·계층·GUID 유지 |
| 미실행 | 원본 프로젝트 전체 Unity Test Runner, 재질·방향·크기 육안 확인, 실제 전투 연결/화약통·지뢰 발동 |

독립 Unity 6000.0.73f1의 실제 PlayMode 프레임에서 검사했다. 실행 중인 원본 에디터는 조작하지 않았다. 검증 프로젝트는 `Temp/EffectScopeE1/Probe`를 재사용하되 **현재 코드로 재빌드한 DLL 및 이번 BOOM·RenderGroup 복사본**으로 교체했다. 폐기한 Scope 확장 DLL/테스트 결과는 사용하지 않았다. 복사본의 SO/MonoBehaviour 참조만 DLL의 MonoScript로 매핑했다.

실행은 reflection 기반 NUnit assertion/코루틴 호출이며 원본 Test Runner 실행과는 다르다. `-nographics` 환경이므로 입자 수·변환·활성/반환 상태는 확인했지만 화면 품질은 검증하지 않았다. 독립 프로젝트의 DLL API Updater에서 UI 어셈블리 해석 진단이 있었으나 이후 컴파일·타입 로드·대상 DB/재생 검사는 실행됐다.

로그는 Git 무시 대상의 로컬 검증 자료다:

- `Temp/BoomE1-build-final.log`: 빌드 결과.
- `Temp/BoomE1-native.log`: 현재 에셋의 Unity 재생 로그.
- `Temp/EffectScopeE1/Probe/boom-db-results.txt`: 신규 DB 검사 통과.
- `Temp/EffectScopeE1/Probe/boom-play-results.txt`: 일반 배율 PASS / 배율 3 FAIL.

#### 발견한 기존 배율 문제와 후속 판단

기존 Play의 `SetParent(target)`는 월드 배율을 유지한다. 배율 3 부모에 들어가면 효과 로컬 배율은 1/3이 된다. 기존 풀의 반환은 `SetParent(root, false)`이므로 그 로컬 배율이 유지된다. 다음 Play에서는 작아진 월드 배율을 다시 유지한다. 이번 변경 전부터의 코드이며 **BOOM 자식 종료·Notifier 부재 문제와 별개**다.

재현 테스트의 실패를 숨기거나 기존 Scope/Pool을 수정하지 않았다. 부모 배율에 대한 지원 범위 또는 공용 반환 시 변환 복원 여부는 사용자 리뷰가 필요하다. 계획의 “잘림·고착·오류가 재현되면 E2에 진입하지 않고 … 보고” 기준에 따라 E2로 넘어가지 않는다.

#### SSOT·SOLID·FW 점검

- 프리팹 참조는 Render DB의 Boom 항목 하나, 파티클 수명은 원본 BOOM, 대여·반환은 기존 Scope/Pool에 유지했다.
- E1은 enum·에셋 등록으로 확장했고 공용 클래스의 책임·계약·추상화는 추가하지 않았다(S/O/L/I/D). E2의 해석/재생 분리는 아직 구현하지 않았다.
- FW-011의 기존 Play/StopAll 경로와 FW-012의 등록·루트 조건을 사용했다. FW-014의 풀 동작은 변경하지 않았으며 확인된 배율 제약을 따로 기록했다.

### 7.3 2026-10-13 — E2 사건 해석·전투 표시 연결

**승인:** E1 결과 보고 뒤 사용자 “E2 진행해”. E1.5를 미해결로 유지하고 E2만 진행했다. 이 지시는 공용 배율 문제 수정 승인이 아니다. E3·커밋·push·외부 발송 없음.

#### 구현

- `CombatExplosionRules`의 순수 함수 두 개가 화약통 Destroyed의 `Origin.PhysicalCell`, 지뢰 Activation의 `Source.Cell`을 읽는다. 비대상은 null, 대상 사건의 좌표 누락은 오류다. 피해자 수·공격자 위치·상태 차이로 발동을 추측하지 않는다.
- `CombatEnvironmentEffectPresenter`는 주입한 함수 목록을 먼저 검사하고 매칭 1개일 때만 기존 Scope로 Boom을 요청한다. 중복 매칭과 필드 밖 좌표는 대여 전에 오류 처리한다. 부모는 CellsRoot, 위치는 Mapper의 로컬 칸 중심, 회전은 X −90°다.
- Scope는 최초 발동 때 발급받아 Display 수명 동안 재사용한다. 별도 효과 완료 상태·타이머·Tick·풀을 만들지 않았다. 재진입 취소는 기존 Scope의 Play가 대여 등록을 마친 뒤 StopAll을 적용한다. 세대/작업 중 플래그는 소유자 취소 제어만 담당하며 파티클 수명을 관측하지 않는다.
- `CombatSceneDisplay`가 기존 효과 서비스로 조립하고 필드 구성 뒤 Bind한다. CancelPresentation, 재설치 전/실패, Clear, Release에 정리를 연결했다. 필드 부모 해제보다 먼저 효과 정리를 시도한다.
- `CombatSceneEventPresenter.PresentEvent`의 오디오·세대 검사 뒤 Action을 한 번 호출하고 세대를 다시 검사한다. CanPresent·Refresh에서는 재생하지 않는다. 결과 완료·barrier·기존 Tick은 변경하지 않았다.
- 신규 운영 코드 2개·테스트 1개의 `.meta` 포함. `EffectScope`, `ParticleStopNotifier`, `EffectPool`, `EffectSystem` diff 없음. Core·Buffer·Flow·LifeCycle·씬·FieldCamera·Architecture 변경 없음.

#### 검증 결과와 한계

| 검증 | 결과 |
|---|---|
| MSBuild | ProjectL.Tests.Framework와 의존 프로젝트 빌드 성공, 오류 0. 기존 경고 유지, E2 변경 파일 새 경고 없음 |
| 새 환경 효과 테스트 | 14개 케이스 통과. 두 발동 조건/비대상, 같은 칸 연속 발동, Boom 키·좌표·회전, Scope 재사용, 함수 확장·중복, 잘못된 좌표, 취소·해제·재진입·반환 예외 |
| 새 실제 씬 연결 테스트 | 4건 통과. 사전 검사 무재생·사건당 1회, Refresh 유지, 결과 완료가 효과를 기다리지 않음, 취소·Clear·Release·재설치/실패 정리, 재진입 취소의 완료 차단 |
| 기존 정리·철도 회귀 | CombatPresentationCleanupTests 6건, FieldRailwayPresenterTests 36건 통과 |
| 기존 씬 회귀 첫 비교 | 23건 중 19건 통과, 4건 실패. 신규 4건을 합친 씬 fixture는 23건 통과/4건 실패. 전체 선택 fixture 79건 통과/4건 실패 |
| 변경 전 기준선 | SceneDisplay·SceneEventPresenter·SceneEventPresenterTests만 HEAD 소스로 별도 빌드해 비교. 같은 묶음에서 76건 통과/3건 실패(신규 씬 테스트 4건 제외). 실패 3건은 변경 후와 같은 assertion |
| 최종 E2 재실행 | 현재 코드 DLL로 복원해 동일 묶음 80건 통과/3건 실패. 신규 18건 모두 통과, 운영 DB 철도 테스트도 이번 실행에서는 통과. 남은 실패 3건은 기준선과 동일 |
| 미실행 | 원본 프로젝트 Unity Test Runner, 전투 화면에서 실제 BOOM의 재질·크기·방향 확인. E1 부모 배율 3 재사용 실패는 해결하지 않음 |

기준선에서도 실패한 것은 `CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent(false/true)`와 `WarmFactRefresh_AllocatesLessThanLegacyProjection_AndReusesViews`다. 앞의 두 건은 이 격리 EditMode 호출에서 기대한 MonoBehaviour 가시성 콜백을 관측하지 못했고, 할당량은 두 경로 모두 0으로 측정됐다. 원본 Test Runner에서 같은 결과라고 확대하지 않는다.

나머지 `OperationalDatabaseLive_FirstAndRepeatedRailwayUseRestoredTrainBeforeResultCompletion`은 첫 변경 후 실행에서 전투 진입의 스테이지 종류 검사로 실패했으나, 기준선의 단독/전체 실행에서는 통과했다. 실제 StageMap_Chapter1은 `randomizeSeedEachRun: 1`이고 StageGeneratorSystem은 Environment.TickCount 시드로 일반 스테이지를 배정한다. 테스트는 노드 2·스테이지 1101을 고정 가정하므로 실행별 진입 대상이 달라질 수 있다. 폭발 표시 이전 실패이며 해당 테스트·Core·DB 값은 이번 E2에서 수정하지 않았다.

검증은 E1과 같은 격리 Unity 6000.0.73f1의 reflection 기반 NUnit assertion 실행이다. 현재 DLL과 Combat 씬·Catalog의 참조 에셋 복사본을 사용했다. 최초 시도는 검증 프로젝트의 TMP Settings 누락으로 씬 fixture가 실패해 TMP 리소스를 보완하고 재실행했다. 원본 프로젝트는 수정하지 않았으며, 이 준비 실패를 코드 회귀 수에 포함하지 않는다. E2의 효과 테스트는 기존 Scope와 테스트용 대여 서비스를 사용한다. 실제 BOOM 자연 반환 증거는 E1 §7.2와 구분한다.

로컬 로그(모두 Git 무시 대상):

- `Temp/BoomE2-build-final.log`: 최종 코드 빌드.
- `Temp/BoomE2-native-retry.log`, `Temp/BoomE2-results.txt`: TMP 보완 후 79/4 결과.
- `Temp/BoomE2-baseline-build.log`, `Temp/BoomE2-baseline-full-native.log`, `Temp/EffectScopeE1/Probe/boom-e2-baseline-full-results.txt`: 변경 전 동일 묶음 76/3 비교.
- `Temp/BoomE2-native-final.log`, `Temp/EffectScopeE1/Probe/boom-e2-results.txt`: 최종 E2 80/3 결과.

최종 `git diff --check` 통과. E2 변경 C# 파일의 LF·UTF-8과 새 파일 meta를 확인했다.

#### SSOT·SOLID·JMH/FW 점검

- SSOT: 발동·칸은 사건, 프리팹은 Render DB Boom 항목, 수명·반환은 기존 Scope/Notifier/Pool에 유지했다. View의 규칙 재계산·피해자별 중복 재생·수명 캐시 없음.
- S/O: 해석 두 함수와 배치/소유자 정리를 분리했다. 추가 함수 주입 테스트로 공통 Presenter 수정 없는 확장을 확인했다.
- L/I/D: 비대상 null/대상 한 칸 계약, 기존 Play/StopAll 사용, EventPresenter에는 Action만 주입, 서비스는 IEffectSystemServices로 받는다. 공용 구현 캐스트·서비스 검색·신규 재생 인터페이스 없음.
- JMH §23/25~28 및 FW-001~004/011~014: Query 상태 변경·서비스 재등록·내부 노드 접근·직접 Destroy·새 풀 없이 기존 조립과 Scope 반환을 사용한다. 기존 부분 실패/Scope 등록 해제/배율 보장을 확장하지 않는다.

**다음 리뷰:** E2 연결 코드를 확인한 뒤 E3는 별도 진행 지시를 받는다. E1.5의 배율 지원 판단, 원본 Unity 검증과 실제 전투 외형 확인은 계속 남아 있다.
