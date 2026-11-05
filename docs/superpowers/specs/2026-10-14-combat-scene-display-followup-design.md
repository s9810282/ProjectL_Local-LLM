# CombatSceneDisplay 후속 리팩토링 설계

- 작성: View 작업, 2026-10-14
- 목적: 씬 표시의 단일 창구와 수명 계약을 유지하면서, 조립과 기능 구현을 분리해 한 기능 변경에 필요한 수정·이해·검증 범위를 줄인다. 기존 연결 안전성·재생 소비 계약·표시 상태 개선을 보존한다.
- 사용자 합의: 중심 Facade 유지, 중복 Attach 보완 → 재생 계약 축소 → 입력 피드백 순서.
- 검토 반영(2026-10-14, 사용자 승인): 독립 검토 3건(전체 구조·수명 정확성·입력 피드백)과 코드 대조로 SD-A·SD-B를 보완했다. 입력 피드백은 새 클래스 분리 대신 비용 미리보기 대상 상태를 `FieldUnitPresenter`로 옮기는 **SD-C′**로 바꿨고, 새 클래스(SD-C1)는 보류한다. 경위는 [후속 Task 계획](../plans/2026-10-14-combat-scene-display-followup-plan.md) §7.2.
- 현재 평가 기준(2026-10-14 사용자 추가): SOLID 5원칙·SSOT·JMH/FW에 **기능 변경의 독립성**을 더한다. 이전 SD-C0의 현 구조 유지 결론을 재평가한 방향은 §5.3이다. A/B/C′ 구현과 검증 이력은 유지한다.
- Task와 실행 상태의 원본: A/B/C′·C0 이력은 [후속 Task 계획](../plans/2026-10-14-combat-scene-display-followup-plan.md), 현재 합의한 조립·기능 분리 SD-D0~D5는 [조립·기능 분리 Task 계획](../plans/2026-10-14-combat-scene-display-composition-plan.md). 이번 계획 구체화는 §5.5를 따르며 구현은 아직 시작하지 않았다.
- 선행 설계: [전투 View 구조 리팩토링](2026-10-14-combat-view-structure-refactor-design.md). R0~R4의 기존 수명·소유권 계약을 유지한다. **D4의 Display 직접 조립·생성자 형태·Factory 미도입 결정만** 사용자 합의에 따라 §5.3·§5.5의 조립 분리 방향으로 대체한다. Tables/Services 묶음과 ViewSystem이 개별 표시 의존성을 모르게 하는 목적은 유지한다.
- 원칙: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md).

## 1. 판단과 목표

현재 `CombatSceneDisplay`는 세 가지 일을 한다. 표시 객체를 소유하고, 일부 화면 피드백을 직접 구현하며, 나머지 기능의 호출을 중계한다. 이 세 책임을 구별한다. 좌표 변환·개발 패널 바인딩·사건 재생·이동 편집의 구현은 이미 별도 객체에 있으므로 같은 분리를 다시 하지 않는다.

씬 설치, Mapper 연결, 최종 상태 갱신, 실패 정리에는 공통 순서가 있다. 이 순서와 소유권은 보존하되, 순서를 조정하는 일과 기능별 표시를 구현하는 일을 구별한다. 소비자 대부분도 이미 역할별 좁은 인터페이스를 받으므로 `ICombatBattleDisplay`의 합성 자체를 제거하지 않는다. 단일 창구를 유지하는 결정은 모든 Presenter·Service의 조립과 직접 사용까지 Display 한 곳에 두어야 한다는 뜻이 아니다.

공개 멤버를 역할별로 세면(2026-10-14 착수 전 검토, SD-A/B/C′ 반영 전):

| 성격 | 수 | 내용 |
|---|---|---|
| 수명 조정 | 5 | Install·Refresh·Clear·Release·완료 표시 |
| 위임 | 44 | 대부분 해제 확인 뒤 한 줄로 넘김 |
| 직접 구현 | 13 | 당시 7개를 두 대상 조정·좌표 변환으로 분류했다(고스트와 배치 격자, 유닛과 손패 원본, 고스트 이동, 이동 포인터). 이 분류만으로 Facade 잔류를 정당화하지 않는다. |

착수 전 Display가 다른 Presenter 대신 기억하던 피드백 대상은 비용 미리보기 대상(`_hudPreviewActor`) 하나였다. C′ 뒤 상태 소유권과 남은 직접 구현을 다시 평가한 결과는 §5.3에 있다. 게임 사실 상태의 유무만으로 God Class 여부를 결정하지 않고, 실제 표시 상태·자원·규칙·수명 책임을 함께 점검한다.

착수 때 정한 개선 대상은 세 가지다.
- 생성 전 연결 검사
- 실제 소비보다 넓은 재생 Attach 계약
- Display가 `FieldUnitPresenter` 대신 들고 있는 비용 미리보기 대상

성공 기준은 클래스 수나 공개 함수 수가 아니라, **기능 내부 변경은 해당 기능 구현에, 의존성 연결 변경은 조립 지점에 모이고, 관련 없는 기능의 구현을 읽거나 검증할 필요가 줄어드는 것**이다. 상태 소유권과 기존 수명 보장은 함께 지킨다. 구체 적용 기준은 [설계 인계 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md)에 둔다.

## 2. 범위

### 2.1 포함

| 단계 | 대상 | 변경 의도 |
|---|---|---|
| A | `CombatViewSystem.Attach`와 실제 SceneRoot 경로 회귀 | 이미 연결된 씬에 대한 호출을 새 Display 생성 전에 거절 |
| B | `ICombatPresentation.Attach`, `OrderedCombatPresentation`, 테스트 구현 | 재생에 필요한 `ICombatEventPlayback`만 요구 |
| C′ | `FieldUnitPresenter`의 HUD 비용 미리보기, `CombatSceneDisplay`의 비용·벽 필드 | 비용 미리보기 대상을 HUD View의 주인이 갖게 함. 쓰지 않는 벽 중복 집합 제거 |
| C0 | C′ 뒤 Display에 남은 직접 구현 | 기존 평가 이력은 보존. 기능 변경의 독립성을 추가한 현재 방향은 §5.3(문서) |
| D0~D5 | 구체 조립 지점과 기능별 구현 경계 | §5.3·§5.5와 별도 조립·기능 분리 Task 계획. 구현 미착수 |
| C1 | 입력 피드백 전부를 한 새 클래스로 이전 | **보류**(§5.4). 응집된 기능별 분리까지 보류한다는 뜻은 아님 |

### 2.2 별도 작업으로 유지

- Audio 호출 위치·프리셋 정책, Core 타임라인 직접 조회 교체(VR6).
- Core·Buffer 계약, FieldCamera, Architecture, 씬·프리팹·DB 에셋 변경.
- `ICombatBattleDisplay` 해체, 모든 소비자의 Display 우회, region별 독립 System·Service 생성.
- 범용 Factory, 이벤트 등록 체계, 생성 실패 전체를 위한 범용 롤백 계층. §5.3의 전투 씬 전용 조립 분리와 구분한다.
- 이미 분리한 재생·입력 객체의 재통합과 기존 R0~R4 구현의 재수행.
- 하위 객체마다 따로 둔 "해제됨" 플래그(Display `_isReleased`, `CombatPointerMapper`, `CombatMovementPlanningPresenter`, `CombatDevLayerPanelBinding`)의 통합. 지금 고칠 가치는 낮다. 이 계획에서 플래그를 새로 늘리지 않는다.

가시밭 DB 이전(D12)은 R 계획에서 완료됐다(2026-10-14, `ThornFieldVisual`). 이 설계는 그 결과를 전제로 한다. 특히 운영 DB를 쓰면 Display 생성 때 가시밭 보관 루트가 항상 만들어진다. 이 범위 밖 변경이 필요하면 구현 전에 설계를 다시 검토한다.

## 3. 책임 경계

아래는 §5.3의 재평가에 따른 목표 경계다. A/B/C′ 외의 분리는 아직 구현하지 않았다.

| 책임 | 목표 주인 | 방향 |
|---|---|---|
| Buffer 읽기, 입력 판단용 사실, 결과 후처리·확인 | `CombatViewSystem` | 유지. 표시 객체의 생성·Configure 세부는 조립 지점에 위임 |
| 씬 표시 객체 생성·Configure·의존성 연결 | 전투 씬 전용 Factory/Assembler | Display 생성자에서 분리. 이미 연결됐는지 확인한 뒤 호출 |
| 공개 표시 입구와 해제 뒤 호출 계약, 씬 공통 수명 순서 | `CombatSceneDisplay` | 유지하되 기능 내부 표현은 해당 구현에 위임 |
| 집기·좌표 변환과 UI Raycast | `CombatPointerMapper` | 기존 구현·연결 수명 유지. 이동 Presenter에는 월드 좌표 전달 |
| 패널 표시·바인딩, 포기·Layer 정책 | 기존 패널·바인딩 객체와 컨트롤러 | 기존 위임 유지. 패널별 형식적 래퍼를 추가하지 않음 |
| 결과 수명·사건 순서·씬 사건 재생 | 기존 Playback·EventPresenter 계층 | B의 좁은 Attach와 실제 Display 전달을 유지 |
| 이동 범위·편집 고스트·원본 흐림·초안 표시 | `CombatMovementPlanningPresenter` | 기존 구현에 `ShowDraft`·`ClearDraft`·`SetPathPlanning`을 모으는 방향 권고 |
| 배치 고스트·배치 격자·필드/손패 원본 표시 조정 | 배치 표시 협력 객체 | 응집된 기능으로 분리 권고. 고스트·원본 대상 자체의 소유권은 기존 객체에 유지 |
| 공격 범위·영향·비용/체력 의도·차단 벽 표시 조정 | 공격 표시 협력 객체 | 배치·이동과 별도 기능으로 분리 권고. 공용 비용 슬롯을 복제하지 않음 |
| 최종 사실의 화면별 표시 변환 | 해당 기존 Presenter를 우선 검토 | 후순위 평가. `Refresh` 전체를 새 큰 객체에 옮기지 않으며 행위자 강조·손패 가시성 등 실제 변경 단위로 판단. 재생 완료 확인과 호출 순서는 공통 수명 경계에 유지 |
| HUD 비용 미리보기의 대상(단일 슬롯)·공격 예상 체력 표시 대상 | `FieldUnitPresenter` | C′의 소유권 유지. 기능 객체는 슬롯 API를 사용 |
| 실제 View·Pool·Scope 반환 | 각 기존 자원 소유자 | 동일 자원 인스턴스와 반환 경로 유지. 공유 참조마다 별도 소유권을 만들지 않음 |

## 4. 확정한 변경

### 4.1 A 연결 전제 검사

착수 전 `Attach(root)`는 `AttachDisplay(new CombatSceneDisplay(...))`를 호출했다. 중복 연결 검사(`_display != null`)는 `AttachDisplay`에서 Display를 만든 뒤에야 했다. 아래는 그 문제와 SD-A에서 정한 해결 방식이다. 생성자는 공유 씬 객체를 바꾼다.

- `PlacementGhost.Configure`가 `Release()`를 불러 떠 있는 고스트를 지운다.
- `ThornFieldVisual` 프리팹이 있으면 `FieldThornPresenter`가 보관 GameObject를 만든다. 운영 DB에서는 항상 있다.
- `FieldUnitPresenter.Configure`·`ConfigureMovementAppearance`·`HandPresenter.Configure`도 공유 MonoBehaviour의 참조를 덮어쓴다. 같은 표·서비스를 넘기므로 관측되는 피해는 없다.
- 나머지 하위 생성자는 참조만 보관하고, GameObject는 설치 때 만든다.

그래서 거절될 호출도 씬을 바꾼다.

**중복 Attach가 실제로 생기는 경로**
- 운영에서 `Attach`를 부르는 곳은 Combat 씬 Enter 노드(`OnCombatSceneEntered`) 하나다. 공개 `ICombatViewService.Attach`를 부르는 다른 운영 코드는 없다.
- 그래도 중복이 생길 수 있다. `AttachDisplay`는 `_display`를 먼저 저장하고 첫 그리기(Install)를 한다. Install이 실패하면 `_display`가 남은 채 예외가 나가고, Detach 없이 다시 연결하면 중복 Attach가 된다.
- `Initialize`의 Enter 노드 이중 등록에도 방어가 없다.

**결정**
- 공개 `Attach(root)`에서 기존 검사(root null, 오류 정지 뒤 무동작, 미초기화, 비활성 진입)를 모두 마친 뒤, **Display를 만들기 직전에** 이미 연결된 상태를 거절한다.
- 검사 조건은 반드시 `_display != null`이다. `IsAttached`(`_display != null && 재생 연결`)는 쓰지 않는다. `StopInput`의 재생 중단이 재생 연결만 풀고 `_display`는 남기기 때문에, `IsAttached`로 검사하면 그 뒤의 중복 Attach가 통과해 원래 결함이 다시 생긴다.
- 조건과 오류 문구는 **공통 검증 메서드 하나**로 둔다. `Attach`와 `AttachDisplay`가 이 메서드를 함께 쓴다(SSOT). 생성·설치·정리 흐름은 재편하지 않는다.
- 중복 호출을 거절하면서 기존 Display를 해제하거나 자동 재연결하지 않는다.
  - Lobby·StageMap View는 다시 연결할 때 자동으로 Detach한다. 전투가 거절하는 것은 기존 계약(이미 연결돼 있으면 먼저 Detach해야 함)에 따른 **의도된 차이**다. 나중에 "통일"하지 않는다.
- 생성자 주석에 전제를 적는다: "SceneRoot 하나에 살아 있는 Display는 하나다. 생성은 공유 씬 객체(고스트·가시밭 보관 루트)를 바꾸므로 연결 검사 뒤에만 만든다."
- 검증 대상은 예외가 아니라 **기존 고스트·연결·씬 자원 보존**이다. 예외 형식만 보는 테스트는 판별력이 없다. 참조가 빈 테스트 Root는 지금 코드에서도 다른 검사에서 같은 예외를 던지기 때문이다.

**기각한 대안: 생성자에서 부작용을 없애기**
- 고스트 설정을 Install로 옮기면 같은 씬 재설치(Clear → Install)마다 고스트 풀을 파괴한다(D8의 자원 재사용과 충돌).
- Install 전 `ShowGhost`가 "Configure가 먼저 필요" 예외를 던지게 된다. 이를 막는 "한 번만 설정" 플래그는 수명 사실을 복제한다.
- 가시밭 루트를 늦게 만들면 보관 루트가 아직 없는 것과 외부에서 파괴된 것을 구분하는 상태가 필요하다. 또한 D12 코드를 다시 건드리게 된다.

**남은 위험(범위 밖, 기록만)**
- Display를 만든 뒤 `AttachDisplay`가 `_display = display`로 소유권을 인수하기 **전에** 예외가 나면 만든 Display의 해제가 누락될 수 있다. 예: `_presentation.Attach` 안의 Cancel 실패. 인수 **뒤** Install 실패는 `_display`가 남아 Detach로 반환할 수 있으므로 같은 위험으로 묶지 않는다(`CombatViewSystemTests.InstallFailure_Propagates_WithoutBindingInput`).
- `Attach`는 `AttachDisplay`가 **정상 반환한 뒤에만** `_sceneRoot` 이벤트를 구독한다. 예외 경로에서는 구독하지 않는다. `_display`가 null인 채 정상 반환하는 경로가 있다면 Detach가 구독을 풀지 못할 수 있으나, 공개 Attach에서 이 상태에 도달하는 실제 운영 경로는 찾지 못했다. 앞의 소유권 인수 전 예외와 별도 잠재 위험이다.
- 일반 생성 실패의 원자성은 이번에 추가하지 않는다.

### 4.2 B 재생 소비 계약

`ICombatPresentation.Attach(ICombatBattleDisplay display)`를 `Attach(ICombatEventPlayback playback)`으로 좁힌다. `OrderedCombatPresentation`은 이미 받은 값을 `ICombatEventPlayback`으로만 저장하고 쓴다. 이 설계는 R2의 D6(재생 계약 명시)를 마무리하는 성격이다.

- 호출자(`CombatViewSystem.AttachDisplay`)는 계속 `CombatSceneDisplay`를 넘긴다. `_eventPlayback` 내부 객체를 소비자에게 직접 노출하지 않는다.
- **숨은 위험과 대응:** 계약을 좁히면, 해제 방어가 없는 내부 객체 `CombatSceneEventPlayback`(같은 계약 구현)도 타입상 넘길 수 있게 된다. 대응은 둘이다.
  - `ICombatPresentation.Attach` 주석에 "해제 뒤 사건 시작을 거절하는 수명 방어 객체(현재 `CombatSceneDisplay`)를 넘긴다"를 적는다.
  - `ICombatEventPlayback` 주석의 구현 목록에 `CombatSceneEventPlayback`(Display 내부 전용)을 더한다.
- Display의 해제 뒤 사건 시작 거절, 기존 Cancel·Attach·Detach 순서와 완료 계약을 유지한다.
  - 운영에서 실제 방어선은 Detach 순서다. 재생 Detach가 Display Release보다 먼저 온다.
- `ICombatBattleDisplay`와 `ICombatEventPlayback`의 기능 구성은 바꾸지 않는다.
- **검증:** 재생 계약만 구현한 가짜는 새로 만들지 않는다. 그런 가짜는 `ICombatEventPlayback` 멤버 13개를 채워야 해 `FakeDisplay` 기본 구현과 중복된다. 좁은 계약으로 연결되는 것은 서명 변경 자체가 컴파일로 증명한다. 대신 지금 비어 있는 검증 두 가지를 채운다.
  - ViewSystem이 같은 Display 객체를 넘기는지: `FakeCombatPresentation`이 받은 객체를 기록하고 `Is.SameAs(display)`로 확인한다.
  - 재생 경로를 거친 해제 뒤 거절: Release 뒤 `OrderedCombatPresentation.Play`가 예외를 던지는지 한 줄을 추가한다. `FirstInstall_…` 테스트는 Display를 직접 부르므로 B를 검증하지 못한다.
- 선택 항목: `ICombatMovementPresenter`(public)는 `ICombatBattleDisplay` 합성에만 쓰이고 소비자가 없다. `ICombatBattleDisplay`가 `ICombatMovementFeedback`을 직접 상속하게 바꾸면 멤버 집합은 그대로다. 외부 참조를 다시 확인한 뒤에만 지운다.
- **2026-10-15 후속(구조 개선 SV3):** 위 "숨은 위험"을 주석 대신 컴파일로 막았다. `CombatSceneEventPlayback`·`CombatSceneEventPresenter`는 `ICombatEventPlayback`을 선언하지 않으므로 연출 `Attach`에 넘기면 컴파일 오류다. 구현 목록은 `CombatSceneDisplay`와 테스트 가짜만 남는다. 선택 항목의 `ICombatMovementPresenter`도 외부 참조 0을 확인하고 지웠다(파일은 `ICombatPointerMapping.cs`로 이름 변경, GUID 유지).

## 5. C 입력 피드백 개선과 조립·기능 분리 평가

### 5.1 분석 결과(2026-10-14 검토)

아래 표는 C′ 이전의 분석과 변경 결정이다. 이전 후 평가는 §5.3을 따른다.

| 대상 | Display가 하는 일 | 판단 |
|---|---|---|
| 고스트 표시·이동·색, 원본 숨김·복원 | 두 대상(고스트와 배치 격자, 유닛과 손패)을 함께 맞춤, 좌표 변환 | 유지. 고스트는 `CombatMovementPlanningPresenter`·Clear·Release와 공유하는 자원 |
| 호버·범위·영향 칸, 이동 계획 중 표시 | `FieldView`로 한 줄 전달 | 유지. Range 층은 이동 범위와 공유(공격·이동 범위 충돌을 테스트가 지킴) |
| 공격 체력 미리보기 | 전달 | 유지. 원본은 이미 `FieldUnitPresenter` |
| 복귀 버튼 견적 | 전달 | 유지. 행동 패널의 보임 상태와 합쳐 계산됨 |
| **비용 미리보기 대상 `_hudPreviewActor`** | 공격 비용·이동 초안이 **한 슬롯을 교체**. `ClearAffectedCells`·`ClearDraft`도 이 슬롯을 지움 | **옮김(C′).** Display가 다른 객체의 HUD 표시 대상을 대신 기억(§26 "다른 객체 내부 상태 관리"). Clear·Release에서 비용 정리가 유닛 반환보다 먼저 와야 하는 순서 의존이 생김 |
| 차단 벽 `_blockedWallCells`·`_blockedWallSet` | 접촉 목록의 벽 칸을 모으는 임시 버퍼 | 상태가 아님. `FieldHighlightState.SetLayerCells`가 칸마다 표시를 한 번만 쓰고 목록을 보관하지 않으므로 중복 제거 `HashSet`은 불필요 |

당시 결론: 원안대로 고스트·원본·호버·Range·체력·복귀까지 새 클래스 하나로 옮기면 전달 계층이 늘어난다. 인터페이스를 유지하는 한 Display의 전달 메서드는 그대로 남는다(당시 계산: 위임 44개 → 최대 57개). **이 수와 새 공개 API 추가 시의 변경 지점만으로 기능별 분리를 기각한 판단은 §5.3에서 정정한다.** 기존 API 안의 구현 변경과 공개 계약 추가는 영향 범위가 다르다.

### 5.2 SD-C′ 결정: 비용 미리보기 대상을 `FieldUnitPresenter`로

`FieldUnitPresenter`는 이미 단일 대상 임시 표시를 스스로 기억한다. 숨긴 유닛(`_hiddenEntityId`), 체력 미리보기 대상(`_attackHealthPreviewTargets`), 이동 투명도 대상(`_movementOpacityActor`)이 그렇다. 비용 미리보기만 대상을 호출자(Display)가 기억한다.

- `FieldUnitPresenter`:
  - `ShowHudCostPreview(actor, cost)`가 이전 대상의 표시를 스스로 지우고 새 대상을 기억한다.
  - 지우는 함수는 인자 없는 `ClearHudCostPreview()`가 된다.
  - 자기 Release에서도 대상을 정리한다.
- `CombatSceneDisplay`:
  - `_hudPreviewActor`를 지운다.
  - 공개 입구(`ShowAttackCostPreview`·`ClearAffectedCells`·`ShowDraft`·`ClearDraft`)와 `_isReleased` 확인, 비용 규칙(어느 때 슬롯을 지우고 채우는지)은 그대로 둔다.
  - Clear·Release 목록의 비용 정리는 새 함수로 바꾼다. 순서 의존은 사라지지만 자리는 바꾸지 않는다.
- `_blockedWallSet`을 지운다. 벽 칸 목록은 접촉별 벽 칸을 한 목록으로 펴는 용도로만 둔다(중복 포함 허용).
- 효과:
  - HUD View의 주인이 표시 대상도 갖는다(SSOT: 공용 HUD 슬롯의 소유자는 한 곳).
  - Clear·Release 순서 의존이 사라진다.
  - Display에 남는 자기 피드백 상태는 해제 플래그 하나다.
  - 새 타입과 전달 계층이 없다.
- 비용:
  - `FieldUnitPresenter`가 조금 커진다.
  - `AttackHealthPreviewDisplayTests`의 `ClearHudPreview(id)` 사용이 바뀐다.
  - 비용 슬롯 교체 규칙을 Display 수준에서 지키는 테스트가 지금 없다. 이전 전에 회귀를 먼저 추가한다.
    - 공격 비용 뒤 이동 초안이 앞 비용을 지움
    - `ClearAffectedCells`가 비용을 지우되 체력 미리보기는 남김
    - `ClearDraft`가 비용을 지움
    - 해제 뒤 `ShowAttackCostPreview`·`ShowAttackBlockers`·`ClearAffectedCells`를 무시함
- 새 "해제됨" 플래그를 만들지 않는다. 해제 확인은 Display 공개 입구의 `_isReleased` 하나로 한다.

### 5.3 조립·기능 구현 분리 재평가 (2026-10-14)

**현재 권고는 전투 씬 전용 조립 분리와 기능별 직접 구현 분리를 함께 추진하는 것이다.** Display는 공개 진입점과 해제 뒤 호출 계약, 공통 수명 순서를 담당한다. 기능 객체는 자기 기능의 표시 변환을 구현한다. 기존 SD-C0의 "상태 소유·중복·실패 경계 개선이 작으므로 현 구조 유지" 결론은 이 권고로 대체한다. 당시 평가 과정은 후속 계획 §7.7, 새 평가 기록은 §7.8에 보존한다.

SD-C′ 뒤 `_isReleased`는 씬 연결 수명, `_blockedWallCells`는 매 호출 다시 채우는 임시 목록이다. Buffer 사실·비용 대상·입력 초안 캐시를 Display가 중복 소유하지 않는다는 관측은 여전히 맞다. 그러나 상태가 없는 `ShowDraft`도 끝점 색·이동 HUD·비용 표시 조건이라는 독립된 변경 이유를 갖는다. **상태 소유가 올바른 것과 수정하기 쉬운 구조인 것은 별도로 평가한다.** 새 상태나 애니메이션이 생길 때까지 기능 분리를 미루지 않는다.

#### 조립 경계

조립 진입점은 `Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.Create(root, tables, services)`로 정한다(§5.5). 씬이 로드되고 `CombatViewSystem.Attach`의 활성 전투·중복 연결 검사를 통과한 뒤 호출한다. Factory는 씬 참조 검증, 객체 생성, Configure, 의존성·콜백 연결을 맡고, Display는 준비된 협력 객체를 받는다. `Install`·`Refresh`·Tick·최종 Release를 Factory에 옮기지 않는다.

```text
조립: CombatViewSystem.Attach → 기존 연결 검사 → 전투 씬 Factory → Display와 기능 객체
실행: 소비자의 좁은 계약 → Display의 호출 방어 → 해당 기능 구현
수명: Display의 공통 순서 → 기존 자원 소유자의 Bind·Clear·Release
```

- `CombatDisplayTables`·`CombatDisplayServices`는 조립 입력으로 사용한다. 각 기능에는 필요한 표·서비스·View만 주입한다. `Get<T>()` 방식이나 SceneRoot 전체를 통한 임의 조회를 새로 도입하지 않는다. 현재 Inspector에서 읽는 커서 스냅 설정처럼 실제 동적 참조 요구는 보존한다.
- Factory가 여러 구현을 아는 것은 조립 책임이다. 기능별 생성 보조 메서드를 둘 수 있지만, 새 기능마다 Factory 인터페이스·Framework Service·Builder를 만들지 않는다. 씬마다 일반 C# 협력 객체를 생성하는 것과 Build 뒤 서비스 구조를 바꾸는 것은 구분한다.
- Display의 기존 참조를 거대한 생성자나 `Parts` 가방에 그대로 옮기는 것으로 끝내지 않는다. 기능이 사용하는 하위 Presenter 참조는 그 기능 구현으로 모으고, Display는 공개 계약·수명 조정에 실제 필요한 참조를 남긴다. 참조 수를 0으로 만드는 것이 목표는 아니다.
- `_fieldBound` 같은 공통 정리 순서의 정의는 런타임 수명 조정자 한 곳에 둔다. Factory와 기능 객체가 각각 최종 반환 목록을 따로 소유하지 않는다. 기능 수명 분리를 추가할 때는 정리 시점과 공유 자원 소유권부터 확정한다.

#### 기능별 판단

| 대상 | 권고 | 변경 독립성의 이득과 경계 |
|---|---|---|
| 생성자 `new`·Configure·콜백 연결 | **전용 Factory로 분리** | 생성 인자·구현 선택 변경을 Display의 실행 코드에서 분리. 생성자만 옮기면 아래 직접 구현의 부담은 남음 |
| 이동 `ShowDraft`·`ClearDraft`·`SetPathPlanning` | **기존 `CombatMovementPlanningPresenter`로 이전 우선** | 경로·끝점·이동 HUD·비용 의도 변경과 경로 지정 중 격자 표시를 이동 구현에서 읽고 수정. `CombatHud` 의존 추가는 실제 기능에 필요한 비용. 새 입력 캐시·비용 대상은 만들지 않음 |
| `UpdateMovementPointer`의 화면→월드 변환 | **기존 Mapper를 사용하는 입구 어댑터 유지** | `UpdatePointer(Vector3?, …)`의 카메라를 모르는 경계 보존. 기능 응집도를 이유로 Raycast·Mapper 수명까지 이동 Presenter에 넣지 않음 |
| 배치 고스트·격자, 필드/손패 원본 숨김 | **배치 표시 협력 객체로 분리 권고** | `Show/HideGhost`, `Hide/ShowOriginal`, 배치 가능 표시를 한 기능에서 수정. `PlacementGhost`의 시각 구현·풀과 원본 대상 기억은 기존 소유자에 유지. 공유 Ghost의 최종 반환을 배치 기능이 독점하지 않음 |
| 공격 범위·영향 칸·비용/체력 의도·벽 접촉 표시 | **공격 표시 협력 객체로 분리 권고** | `Show/ClearRangeCells`의 운영 호출자는 AttackTargetingInteraction이다. 공격 표시를 모으되 이동과 공유하는 Range 층의 기존 정리 계약을 유지. `ClearAffectedCells`의 비용 정리와 `ShowAttackBlockers`의 벽 칸 변환·임시 목록도 함께 이동 가능. 비용·체력 상태는 FieldUnitPresenter에 유지. 실제 공격 애니메이션을 맡는 `CombatAttackPresenter`와 합치지 않음 |
| 일반 Hover 한 줄 위임, 복귀 버튼 | **기존 기능 경계와 위임 유지** | 범용 피드백 클래스에 모으지 않음. 공유 층의 소유 의미를 바꾸지 않고, 복귀 표시 구현은 기존 ActionPanel에 둠 |
| `Refresh`·`RenderActiveActor`의 최종 사실 표시 변환 | **후순위로 기능별 경계 평가** | 손패 가시성은 기존 HandPresenter의 좁은 입력, 행위자 강조는 필드의 지속 표시 기능처럼 각각 판단. `Refresh` 전체를 또 다른 큰 Presenter로 옮기거나 한 조건마다 새 클래스를 만들지 않음. FieldView에 전체 CombatState/Character 목록을 넘겨 저수준 View를 넓히지 않음. 전달받은 스냅샷을 저장하거나 Core 규칙을 계산하지 않음 |
| BGM·승리음과 `ShowCompletion` | **별도 Audio 범위 유지** | 현재 직접 구현이 있음을 인정한다. 오디오 호출 위치·프리셋 정책까지 위 Refresh 분리에 묵시적으로 포함하지 않음(§2.2). 후속 범위 변경 시 명시 |
| 개발 Layer·포기·행동 패널 | **기존 분리 유지** | 패널·바인딩·컨트롤러가 구현을 이미 소유. Display의 해제 방어를 남기고 region마다 새 래퍼를 만들지 않음 |
| 집기·좌표 변환, 사건 재생 | **기존 Mapper·Playback·EventPresenter 유지** | 기능 내부 변경은 이미 국소화된 부분. 조립만 Factory로 이동하고 소비자에게 내부 객체를 직접 노출하지 않음 |
| Install·Clear·Release | **우선 Display의 공통 수명 조정으로 유지** | Build→현재 Mapper 연결, 실패 정리, 재설치와 최종 반환은 기능을 가로지르는 계약. 기능 분리 후에도 별도 변경 이유가 남을 때 전용 수명 객체를 검토하며, 현 단계에서 모든 참조 제거를 위해 또 다른 거대 관리자를 만들지 않음 |

#### 변경·검토·검증 범위 비교

현재도 작은 변경마다 Display 전체나 모든 테스트를 반드시 확인해야 하는 것은 아니다. 다만 기능 구현과 조립·수명이 같은 파일에 있고, 일부 세부 표시 테스트도 전체 씬 조립에 의존한다. 아래는 그 결합을 줄이는 목표다. 기존 API 안의 구현 변경을 기준으로 하며, 새 공개 API나 공유 계약 변경은 영향받는 소비자까지 검토한다.

| 변경 시나리오 | 현재의 수정·이해 범위 | 분리 후 목표와 필요한 검증 |
|---|---|---|
| 이동 초안 후보 칸·유효 색·HUD 표시 조건 | Display의 초안 구현 + Field/Hud/Units 계약. 세부 표시 회귀도 전체 Combat 씬·DB fixture를 사용 | 이동 Presenter와 관련 View 중심. 기능 fixture로 표시 검증, 공용 비용 교체·Display의 늦은 호출 방어는 통합 회귀로 유지 |
| 배치 고스트·격자·원본 숨김 표현 | Display의 배치 구현과 Ghost/Field/Units/Hand, 공유 Ghost를 쓰는 이동 경계 | 배치 표시 구현 중심. 배치 관련 View로 검증. Ghost의 대여·반환 계약을 바꾸면 이동과 교차 회귀 |
| 기능에 새 표시 의존성 추가 | Display 필드·생성·실행부·정리 목록과 표/서비스 공급 경로 | 해당 기능과 Factory 중심. 기존 공급 계약에 없는 표·서비스·씬 참조라면 그 공급 경로도 변경·검증. 항상 두 파일만 수정한다고 보장하지 않음 |
| 폭발 표시 규칙 추가·기존 사건 표현 수정 | 기능 구현과 Display 생성자의 규칙 등록. 수명 변경이 있으면 정리 코드도 검토 | 기존 사건/환경 구현과 Factory 등록. 표시·Scope 정리와 관련 조립 검증. Barrier·완료·취소가 바뀌면 Playback/ViewSystem 통합 회귀 |
| 손패 카드 호버 표현 | 이미 HandPresenter 등 손패 기능 중심 | 현 경계 유지. 공유 계약 변화가 없다면 이동·레일 재생을 추가 검증 대상으로 삼지 않음 |
| Install 실패·Clear/Release 순서 변경 | Display와 관련 자원 소유자·ViewSystem Detach | 여전히 관련 기능의 통합 회귀 필요. 이 비용은 공통 계약 자체의 영향이며 기능 분리로 없애는 대상이 아님 |

실제 검증 경계의 근거는 `CombatPointerMappingTests.SetUp`과 `CombatMovementPlanningDisplayTests`·`CombatMovementRangeDisplayTests`다. 앞 fixture는 Combat 씬·전체 Catalog·EventSystem을 준비하지만, 뒤 fixture들은 필요한 Field/Units/Ghost를 조립해 기존 이동 Presenter를 직접 검사한다. 초안 표시도 기능별 준비가 가능하도록 설계한다. 기존 씬 회귀는 조립·공유 수명 근거로 남기고, 기능 이전을 그대로 복사한 테스트나 모든 인터페이스의 가짜 구현을 추가하지 않는다.

#### 유지할 계약과 조립 실패 경계

- `CombatPathInput`은 `EndMovementPlanning`과 `ClearDraft`를 따로 호출한다. 이동 Presenter의 `Clear`는 Install 직전·실패에도 호출되므로 초안 정리를 무조건 합치지 않는다. `ShowDraft`에 없던 `_editing` 전제도 추가하지 않는다.
- 공격·이동은 계속 FieldUnitPresenter의 **같은 비용 슬롯**을 사용한다. 초안/영향 칸 정리로 체력 미리보기를 지우지 않는다. 오래된 이동 Range 정리가 새 공격 Range를 지우지 않게 기존 범위 소유 계약을 유지한다.
- Factory는 동일한 Field/Units/Ghost/이벤트 객체를 필요한 곳에 연결한다. 기능마다 Ghost 풀, 비용 대상, Mapper, 해제 상태를 복제하지 않는다. Mapper는 생성 시 고정하지 않고 Build 성공 뒤 현재 값을 연결한다.
- SD-A의 `_display != null` 검사는 Factory 호출 전에 둔다. SD-B의 재생 Attach에는 여전히 **Display 자체**를 넘겨 해제 방어를 보존한다. Release 멱등·실패 수집·취소 후 반환·옛 콜백 무효화와 §6의 계약은 유지한다.

| 시점 | 현재 코드의 보장·위험 | 후속 설계에서 정할 것 |
|---|---|---|
| 조립 전 | 활성 전투·중복 연결을 검사해 공유 씬 객체 Configure를 막음 | 검사 후 Factory를 호출하는 순서 유지 |
| 조립 도중 | 원자적이지 않음. 예: root의 타이밍 검증에는 `MovementStepSeconds`가 없고, EventPresenter의 step 검사는 Units/Hand/Ghost Configure 뒤에 실행됨 | 사전 검증 범위와 실패 전 만든 자원의 정리 책임. 기존 상태 복원·전체 롤백을 자동 보장한다고 쓰지 않음 |
| Create 성공 → `_display` 저장 전 | 입력 Clear·Presentation Attach 등의 예외로 새 Display를 Detach가 찾지 못할 수 있음(§4.1) | 생성한 쪽에서 ViewSystem으로 소유권을 넘기는 시점과 인수 전 실패의 정리 담당. 구체 보완 범위는 별도 Task로 명시 |
| `_display` 저장 뒤 Install 실패 | Display가 보관되어 후속 Detach로 반환 가능. 기존 `InstallFailure_Propagates_WithoutBindingInput`이 이를 확인 | 재설치·Detach 계약을 보존하고 인수 전 실패와 같은 롤백으로 합치지 않음 |

이 실패 표는 Factory 추출만으로 기존 위험이 해결됐다고 오인하지 않기 위한 설계 조건이다. 범용 롤백이나 호출 순서 변경을 이번 문서 작업에서 구현하지 않는다. `_display=null` 정상 반환 뒤 root를 구독할 잠재 가능성은 §4.1대로 실제 운영 경로 미발견 상태로 구분한다.

**진행 방향:** 착수·주입 명세(D0) → 구체 Factory(D1) → 이동 초안(D2) → 배치(D3) → 공격(D4) → 통합 인수·최종 표시 갱신 경계 평가(D5)로 나눈다. Task·검증·새 에이전트 검사와 보고·정지 조건은 별도 계획이 원본이다. Factory만으로 변경 독립성이 확보됐다고 완료 판정하지 않는다.

### 5.4 SD-C1: 입력 피드백 전체를 한 클래스로 이전하는 안(보류)

- 보류 이유는 배치·이동·공격·복귀라는 서로 다른 변경 이유를 한 객체로 다시 모으기 때문이다. Display의 부담을 다른 큰 클래스로 옮기면 기능 변경의 독립성이 충분히 개선되지 않는다.
- 위임 메서드가 남는다는 사실, 구현·호출자가 하나라는 사실, 새 상태가 없다는 사실은 각각 분리 기각의 충분조건이 아니다. 기존 공개 계약 안의 구현 변경은 인터페이스·FakeDisplay까지 수정하지 않고 해당 기능 안에서 끝날 수 있다.
- §5.3의 응집된 기능별 분리는 C1 전체 이전안과 별개로 권고한다. 호버·공유 Range·복귀까지 편의상 묶지 않고 실제 변경 시나리오와 공유 수명으로 범위를 정한다.

### 5.5 Phase 계획에 적용할 구현 경계

사용자가 §5.3 방향으로 Phase별 계획 작성을 요청해 다음 경계를 구체화했다. 실행 체크리스트와 상태는 [조립·기능 분리 계획](../plans/2026-10-14-combat-scene-display-composition-plan.md)에만 둔다.

- **동작 보존:** D1은 조립 추출이다. §5.3 실패 표의 조립 원자성·사전 타이밍 검증 통합·소유권 인수 전 해제 누락은 별도 후속 개선으로 남긴다. Factory 전환을 이유로 Attach 인수 시점이나 Configure·가시밭 생성 순서를 바꾸지 않는다. 개선 완료와 알려진 잔여 위험을 구분한다.
- **주입:** Display는 준비된 객체를 명시적인 타입으로 받는다. D1의 긴 주입 생성자는 과도기 비용으로 허용하고, 뒤 기능 이전까지 마친 후 남은 각 참조를 공통 수명·공개 전달·최종 표시의 실제 필요로 평가한다. `Parts` 가방·후설정 setter·서비스 조회로 감추지 않으며, 인자 수만 줄이기 위한 새 수명 관리자는 만들지 않는다.
- **가시밭 연결:** Display를 캡처하던 ConsumeThorn 대신 Factory 지역 ThornPresenter 참조를 콜백이 사용하도록 한다. EventPresenter 생성·검증 뒤 ThornPresenter를 생성하는 기존 순서를 유지하고 동일 인스턴스를 사건 재생과 Display에 주입한다. 지역 참조는 조립 중 한 번 설정한 뒤 바꾸지 않는다.
- **이동:** 기존 `CombatMovementPlanningPresenter`의 전체 생성자에 HUD를 필수 주입하고 범위 전용 Field-only 경로는 보존한다. 초안·계획 표시를 이전해도 Clear의 범위와 편집 전제는 확대하지 않는다.
- **배치·공격:** 새 내부 구체 객체는 `CombatPlacementFeedback`과 `CombatAttackFeedback`으로 정한다. 전자는 Field/Ghost/Units/Hand, 후자는 Field/Units만 받는다. 상태·풀·최종 수명을 중복 소유하지 않고 소비자는 계속 Display의 가드를 거친다.
- **후순위 평가:** D5는 통합 인수와 Refresh의 개별 표시 변환 평가다. Refresh·Audio·수명 객체의 추가 분리는 구현하지 않고 필요하면 새 설계·Task로 제안한다.
- **검사:** 각 Phase 작업물이 준비된 뒤 새 에이전트 한 개가 검사한다. 이전 Phase 검토자·해당 Phase 구현자를 재사용하지 않는다. 지적 처리·검증 상태와 다음 범위를 보고한 뒤 멈춘다.

### 5.6 SD-D5 인수 평가 연결 (2026-10-15)

D1 Factory, D2 이동, D3 배치, D4 공격 분리 후의 변경 지도·Display 참조 26개 전수 점검·Refresh 후속 판단은 [조립·기능 분리 인수 평가](2026-10-15-combat-scene-display-composition-acceptance.md)에 기록한다. 생성·표시 구현의 변경 지점은 실제로 나뉘었으며 공통 수명·공유 상태 소유권은 유지한다. 남은 참조는 실제 용도로 판단하고 수를 줄이기 위한 가방·수명 관리자를 추가하지 않는다.

Refresh의 손패 가시성과 활성 행위자 강조는 독립 변경 이유가 남는 후속 후보다. 전체 Refresh·Audio·수명 이전은 구현하지 않았다. 현재 검증의 사용자 확인, 과거 수정 전 기준선 미확보, 알려진 실패 복구 한계를 구분하며 Task·최종 검사 상태는 [조립 계획 §10.7](../plans/2026-10-14-combat-scene-display-composition-plan.md)에만 둔다.

## 6. 보존할 계약

기존 계약의 원본은 [구조 리팩토링 설계 D6·D8·D9·D10](2026-10-14-combat-view-structure-refactor-design.md)과 [전투 View 설계](2026-09-30-combat-view-design.md)다. 이번 변경에서 대조할 항목은 다음과 같다.

- Build 성공 후 현재 Mapper를 연결한다. Build·Install 실패 뒤 옛 Mapper를 쓰는 입력·재생은 재개되지 않는다.
- Clear는 같은 씬의 재설치를 허용하고 Release는 최종 해제다. Object·Terrain 재사용 때문에 다른 정리 구성을 하나의 Reset으로 통일하지 않는다.
- Release는 멱등이다. 하나의 정리 실패가 다른 자원의 반환을 막지 않는다.
- 해제 뒤 일반 입력 피드백은 무시하고, 집기는 false를 반환한다. 사건 시작·Install·Refresh는 예외다. 개발 Layer 패널의 새 연결인 `ShowDevLayerPanel`도 해제 뒤에는 예외를 던지는 기존 계약을 유지한다. `HideDevLayerPanel`과 null 콜백으로 끊는 UI 바인딩 정리는 계속 허용한다.
- 취소는 완료 통지가 아니다. 이전 연결의 콜백이나 프레임 시간이 새 재생에 적용되지 않는다. 취소 범위가 다른 세대 번호는 유지한다.
- 결과 재생 완료 → 최종 Refresh → 입력 후처리 → 처리 명령 제거·결과 확인 순서를 유지한다. 외부 호출 후 토큰 확인과 실패 시 미확인 상태를 보존한다.
- 비용 미리보기는 한 대상만 보인다. 공격 비용과 이동 초안이 같은 슬롯을 교체하고, 영향 칸·초안 정리는 비용을 지우되 체력 미리보기는 남긴다(C′에서 주인만 바뀜).
- Buffer는 게임 사실의 원본이다. 사건 중간 표시 사실과 HUD 정리용 상태는 표현 상태로 유지하며 게임 규칙을 재계산하지 않는다.
- B의 Attach 인자 축소를 제외한 소비자 계약, 씬 직렬화 참조, 버튼 입력의 `UIInputBinding` 경로를 유지한다.

## 7. 원칙 점검

| 기준 | 적용 |
|---|---|
| SSOT | Buffer 사실·결과 수명·명령 대기의 원본을 유지. A의 연결 조건은 공통 메서드 하나. C′에서 비용 미리보기 대상의 주인은 HUD View를 가진 `FieldUnitPresenter` 하나 |
| SRP | 조립, 공개 수명 계약, 이동·배치·공격 등 기능 구현의 변경 이유를 분리. 여러 화면을 함께 바꾼다는 이유만으로 모두 Display에 두지 않음 |
| OCP | 기존 공개 계약 안의 기능 표현 변경은 해당 구현에 한정. 의존성 교체는 조립 지점에 모음. 새 공개 계약이 필요할 때 관련 입구·호출자 변경은 허용하며 범용 등록 체계는 만들지 않음 |
| LSP | 중복 Attach 거절의 부작용 제거. 취소·완료·해제 뒤 호출 계약 보존 |
| ISP | B의 좁은 재생 계약과 기존 소비자별 인터페이스 유지. 합성 Facade를 해체하거나 내부 객체를 소비자에게 모두 노출하지 않음 |
| DIP | 전역 서비스는 기존 인터페이스로 받고 조립 지점에서 필요한 의존성만 주입. 구체 C# 협력 객체의 사용 자체를 위반으로 보지 않으며 서비스 전체 묶음을 각 기능에 넘기지 않음 |
| FW 계약 | 서비스·모듈 등록, Builder, 입력·UI·Render 경계 변경 없음. 내부 객체의 독립 서비스 등록 없음 |
| JMH §26·§27·§28 | 큰 API·의존성과 수정 부담을 검토. 실제 기능 경계를 분리하되 형식적 래퍼·범용 Factory·새 서비스 등록은 피함. 구현이 하나여도 현재의 변경 범위를 줄이면 구체 분리를 허용 |
| 기능 변경의 독립성 | §5.3의 시나리오별 수정·이해·검증 범위로 평가. 조립만 이동하거나 모든 구현을 다른 큰 클래스에 옮긴 것을 완료로 보지 않음 |
