# 특성 몸체 텍스트·효과 구조 검토

후속 상태: 사용자가 프리팹 전환과 본 검토 보완을 요청하여 [설계 §3~5·§9](2026-10-31-trait-body-feedback-design.md)와 [계획 §9 Phase F~J](../plans/2026-10-31-trait-body-feedback-plan.md#9-2차-실행-계획--프리팹-전환과-검토-보완)에 반영했다. 후속 F~J에서 R1~R4를 구현하고 컴파일·정적 검증을 마쳤다. 최신 판정은 §6이며 §1~5는 보완 전 검토 이력이다. Play·Profiler는 여전히 사용자 후속이다.

2026-10-31. 검토 대상은 이번 몸체 피드백의 데이터·계획·재생·창구·공용 Factory·Inspector·조립 검증 경로다. 기존 전투 전체나 별도 HUD 작업 전체를 검토한 것은 아니다. 이번 검토에서는 코드/에셋을 수정하지 않았다. Play·Profiler는 사용자 후속 확인으로 유지한다.

결론: 기본 책임 분리와 기존 재생 수명 사용은 적절하지만 **SSOT·확장 독립성·캡슐화까지 모두 충족했다고 판정할 상태는 아니다.** 아래 R1~R3을 코드 보완 후보로 남긴다. Tick의 존재 자체는 문제가 아니며 R4는 낮은 우선순위다.

## 1. 발견 사항

### R1. P2 — 지원 조합이 여러 곳에 흩어져 있다

- `TraitActivationVisual.cs:102~112`: 가속 Count와 가속/깔끔한 뒷정리 StateApplied 지원을 검증한다.
- `CombatBodyTextDrawer.cs:61`, `:125`: 같은 조합을 370·230·90 숫자로 다시 판단한다.
- `CombatTraitFeedbackPlan.cs:131~143`: 상태 해석기는 두 TraitKey의 if 분기로 구현돼 있다.

새 특성을 같은 상태 피드백에 연결하려면 데이터 검증·런타임 해석·Inspector를 각각 고쳐야 한다. 설계의 “대응 해석기 등록으로 확장”은 실제 등록 구조로 구현되지 않았다. 현재 두 특성의 지원 결과가 틀렸다는 뜻은 아니지만, 지원 정책의 원본과 기능 추가 경계가 분산되어 있다.

보완 방향: 지원 조합/Count 정책을 한 조회로 모으고 Inspector도 그 결과를 사용한다. 상태 해석은 공개 전후 사실의 종류별 처리기 또는 등록된 해석기로 분리한다. 숫자 ID를 다시 쓰지 않는다. 표시/풀/희생 동작은 수정하지 않고 확장할 수 있어야 한다.

### R2. P2 — 잘못된 Count를 Inspector에서 고칠 수 없게 잠근다

`CombatBodyTextDrawer.cs:61~63`은 가속 AttackGranted 외 Count 필드를 비활성화한다. 가속 줄의 Count를 CompletedMovementCells로 선택한 뒤 Kind를 바꾸면 기존 값은 그대로인데 Count를 None으로 되돌릴 수 없다. 같은 상태로 저장하면 런타임 검증은 해당 조합을 거절한다.

보완 방향: 잘못된 기존 값은 경고와 함께 None으로 고칠 수 있게 한다. 팝업에서 새로 선택 가능한 값만 제한하거나 사용자가 누르는 명시적 초기화 동작을 제공한다. Inspector를 그리는 것만으로 값을 몰래 바꾸지는 않는다. R1의 공용 지원 정책을 사용하면 함께 정리할 수 있다.

### R3. P2 — readonly 컬렉션 필드가 내부 변경까지 막지는 않는다

`CombatTraitFeedbackPlan.cs:17~34`의 States·Occurrences·Issues는 internal readonly 필드지만 실제 Dictionary/List는 변경 가능하다. Binding/Occurrence도 internal 가변 필드이며 Started/Shaken을 Playback이 직접 수정하고, 상태 목록은 `AfterFacts`가 직접 Remove한다.

이는 이번 코드에서 실행 상태가 반드시 중복됐다는 증거는 아니다. 다만 사건 연결을 만든 객체의 불변 조건과 실행 플래그가 호출자에 노출돼 있다. 지원 처리가 늘어날수록 외부에서 수정 가능한 범위도 넓어진다.

보완 방향: 컬렉션을 private으로 두고 `TryStart`, `TakeStateFeedback`, `MarkShake`처럼 필요한 동작만 공개한다. 구성 정보는 읽기 전용 값으로 노출한다. 계획과 실행 상태를 분리한다면 각 값의 단일 소유자를 명확히 하고 동일한 상태를 두 저장소에 복제하지 않는다.

### R4. P3 — Tick은 필요하지만 몸 조회와 렌더 콜백의 중복은 개선 여지가 있다

`CombatBodyTextWindow.cs:106`, `:138`에서 활성 문구마다 Position 델리게이트를 렌더 직전과 Tick에서 각각 호출한다. 현재 위치뿐 아니라 대여 유효성·같은 표시 객체 여부도 매번 조회한다. Canvas 콜백은 생성자에서 구독하고 Release에서 해제하여 문구가 없는 연결에도 구독 자체는 유지된다.

- Tick은 유지 시간·페이드를 진행하고, 렌더 직전 투영은 그 프레임의 이동/카메라/Shake 결과를 반영한다. 역할이 다르므로 둘 중 하나를 무조건 삭제하면 안 된다.
- 개선한다면 유효성 검사와 위치 조회를 구분하고, 활성 출력이 0이면 투영 구독/작업을 쉬는 방식을 검토한다. 마지막 위치를 무조건 Tick에 캐시하면 이동/카메라보다 먼저 Tick되는 현재 순서에서 한 프레임 지연이 생길 수 있다.
- Tick/Project에는 매 프레임 LINQ, Find/GetComponent, 새 Dictionary, 전체 유닛/Buffer 폴링이 없다. Show의 슬롯 검색과 델리게이트 생성은 발동 때, 계획의 LINQ/배열 생성은 결과 분석 때다. 전부 Tick 남발로 분류하지 않는다.
- 계획의 후보 탐색은 발동·대상마다 후속 사건을 훑는다. 대형 결과에서 비용이 문제라면 먼저 Profiler로 확인한 뒤 결과 시작 시 한 번 인덱싱하는 방향을 검토한다. 현재 성능 결함을 재현한 것은 아니다.

## 2. 기준별 판정

| 기준 | 판정 | 근거 |
|---|---|---|
| SSOT | 부분 충족 | 이름·표현 설정은 View DB, 상태·이동은 Result.Events. N은 표시용 파생값이므로 자체가 규칙 중복은 아님. 지원 조합은 R1 보완 필요 |
| S — 단일 책임 | 대체로 적합 | 입력/검증, 사건 대응, 재생 연결, 텍스트/풀을 나눴고 HUD에 발동 판정을 넣지 않음. 계획의 구성/실행 상태 경계는 R3 개선 여지 |
| O — 개방/폐쇄 | 보완 필요 | 해석기 등록 대신 TraitKey if와 여러 지원 표를 수정해야 함(R1) |
| L — 치환 | 코드상 적합 | 기존 Player/Accent 계약 사용. 텍스트가 남아도 IsComplete=true는 비차단 표시라는 명시적 정책. Cancel/Release 경로 존재. 실제 재생은 미확인 |
| I — 인터페이스 분리 | 대체로 적합 | 몸 추적에는 좁은 조회 델리게이트 사용. 기존 Player의 공통 수명 메서드가 일부 비어 있다는 이유만으로 새 인터페이스를 더 만들 필요는 없음 |
| D — 의존성 역전 | 대체로 적합 | Factory가 카메라·폰트·몸 조회를 주입. 내부 협력 객체의 구체 타입 사용만으로 서비스 경계 위반으로 보지 않음. 새 전역 서비스/루트 resolver 우회 없음 |
| JMH 개발 지침 | 대체로 적합, R1/R3/R4 보완 후보 | 설정과 실행 상태 분리, 서비스/조회 주입, 수명 해제. 상태 대응은 아직 등록 확장이 아님 |
| FW 계약 | 조사 경로에서 신규 위반 확인 없음 | 기존 생성/바인딩/Player 수명 사용. 아래 적용 구분 참고. 이 판정이 모든 FW 계약을 실행 검증했다는 뜻은 아님 |
| 기능 변경 독립성 | 부분 충족 | 폰트/페이드/몸 추적 변경은 Text 영역에 머묾. 새 StateApplied 특성 추가는 R1 때문에 검증·Inspector·해석을 함께 수정해야 함 |
| 캡슐화 | 부분 충족 | 입력 필드는 private, 런타임 표시 값은 get-only. Plan 컬렉션·가변 실행 플래그는 R3 보완 필요 |
| C#/Unity 구조 | 대체로 적합, 에디터 보완 필요 | sealed·블록 namespace·명시적 수명·TMP 풀·출력 카메라 주입·raycast 비활성. Count 수정 불가(R2), 실제 Canvas 위치/정렬/출력은 Play 확인 필요 |
| if/Tick 사용 | 대부분 필요 | 선택 기능, 대상 소실, 상태 대응, 세대 취소, 화면 밖, 시간 만료에 필요한 분기. 의미 없는 조건문이 대량으로 있다는 판단은 아님. 지원 분기 중복과 위치 중복 조회를 구분하여 개선 |

### FW 적용 범위

- 생성: `CombatEventPlaybackFactory.Create`가 텍스트 창구를 만들고 Presenter에 전달한다. Presenter의 Player 목록이 Tick/Cancel/Release를 소유한다.
- 등록/Export/소비: 새로운 IServiceModule을 등록하지 않았고 Global/루트 Export나 resolver 조회도 추가하지 않았다. 따라서 새 서비스의 FW-001/002/003 위반은 발견하지 않았다. 내부 위젯을 억지로 서비스로 만들 필요도 없다.
- FW-004: Core 규칙/Buffer 내부 상태를 수정하지 않는다.
- UI: 독립 Open/Hide 화면이 아닌 사건 Accent의 일시 위젯이라는 기존 설계 근거로 UIId를 새로 만들지 않았다. HUD/팝업 기능으로 확장하면 UI 등록 정책을 다시 검토해야 한다.
- FW-011/012: 기존 효과는 기존 Effect 창구/Scope 회수 경로를 유지한다. TMP 객체는 직접 생성한 자체 풀 자원이므로 Framework에서 빌린 Unity 객체를 직접 파괴한 경우와 다르다.
- FW-010, FW-013, FW-015: 이번에 Mesh DTO·Bus·JobQueue 변경이 없으므로 해당 변경을 실제 검증한 것으로 쓰지 않는다.
- FW-014: 새 텍스트 풀은 private Stack이다. CustomPool의 공개 계약을 새로 사용한 것은 아니다.
- FW-016 및 수명: 기존 전투 진입/종료 경로를 사용하고 Canvas.willRenderCanvases는 Release에서 구독 해제한다. 새로운 독립 MonoBehaviour.Update 루프는 없다.

## 3. 1차 검토 시점의 사용자 에디터 작업 (이력)

### 먼저 공유 HUD 참조를 맞춘다

1차 검토 당시 `HUD.prefab`에는 `categoryImage`와 `itemIcon` 직렬화 연결이 모두 없었다. 아래는 그 시점의 조사이며 현재 연결 상태의 판정이 아니다. 공유 HUD는 별도 작업자가 변경 중이므로 이번 F~J에서도 수정하지 않았다. 최신 UnitHUD.ValidateReferences는 둘을 필수로 요구한다. 이는 별도 HUD 변경과 프리팹 연결 사이의 문제이며, 이번 검토에서 임의로 수정하지 않았다.

| 항목 | 연결/배치 조건 |
|---|---|
| categoryImage | 같은 HUD 내부, InfoPanelMask 밖의 병과 Image. InfoPanel 안의 기존 Category에 단순 연결하면 계층 검사에 걸릴 수 있음 |
| itemIcon | 같은 HUD 내부, Mask와 Status Root 밖의 별도 고정 슬롯. 병과 Image와 같은 객체/부모자식 관계로 쓰지 않음 |
| itemIcon의 내부 구조 | BuffIcon 인스턴스의 루트 Image를 연결하고, 자식 `Icon`에도 Image가 있어야 함 |
| 기존 연결 | infoPanel·HP·statusRoot·statusIconPrefab은 기존 값 유지하고 필수 참조 오류가 더 없는지 확인 |

HUD 설명 팝업은 이전 사용자 결정대로 후속 보류다. 이번 몸체 텍스트 확인을 위해 설명 패널을 만들 필요는 없다.

### 이미 설정된 몸체 피드백을 확인한다

- `CombatPresentationSettings_Default`: Body Text Font에 기존 SCDream7 SDF, Stack Gap 28이 연결돼 있다. 새 폰트를 만들 필요는 없다.
- TraitVisual_Acceleration: StateApplied, Source, CompletedMovementCells.
- TraitVisual_CleanSweep: StateApplied, Source, None.
- TraitVisual_Sacrifice: ActivationStart, Source, None. 기존 두 UnitActions와 효과는 보존했다.

### Play 확인 순서

1. 조립 모드의 Scenario_Acceleration: 같은 Packet의 Movement → Activation → State. 실제 2칸 이동·증가량 10에서 `가속 X 2`인지 확인한다.
2. Scenario_CleanSweep: 다른 아군 공격 후 특성 보유자의 버프 HUD와 문구가 함께 시작하는지 확인한다.
3. Scenario_Sacrifice와 사망 버전: 수호자 나가기 시작에 `희생`, 이동 중 몸 추적, 보호 대상 중복 없음, 사망/복귀 후 잔여 문구 없음.
4. 유지 0.25초·페이드 0.6초, 위치·색·크기·겹침 가독성을 조정한다. 기존 파티클은 몸을 따라가지 않고 시작 위치에 놓인다는 정책을 구분한다.
5. Stop/Reset/반복·Pause·카메라 이동/Shake·화면 밖·다음 Packet에서 문구 소멸과 진행을 확인한다. 여러 해상도/화면비에서도 몸 중앙에 보이는지 확인한다.

문구가 보이지 않으면 생성된 CombatBodyTextCanvas의 출력 Display·Overlay 계층·화면 정렬과 실제 출력 카메라를 먼저 확인한다. 1차 구성은 필드 아래에 별도 Overlay Canvas를 생성했다. 2차에서는 소유 Scene의 독립 루트로 변경했다. 이를 Unity 6000.0.73f1에서 실제 표시 성공으로 검증한 것은 아니다.

## 4. 코드 후속과 에디터 작업 구분

- 코드 후속: R1 공용 지원 정책/해석 확장 경계 → R2 Inspector 수정 경로 → R3 계획 캡슐화. 사용자에게 YAML 직접 편집이나 코드 수동 수정을 요구하는 항목이 아니다.
- 낮은 우선순위: R4 활성 문구 중심의 조회/구독 최적화. 성능 측정 없이 전투 전체 재생기를 재구성하지 않는다.
- 사용자 작업: 공유 HUD 연결과 위 대표 Play/가독성 확인. 이 확인이 끝나기 전 런타임 동작 전체를 검증 완료로 부르지 않는다.

## 5. 근거와 한계

- 프로젝트 기준: [View 구조 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [FW 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md).
- Unity의 동적 Canvas 분리·비상호작용 UI의 raycast 제외·풀 재사용 권고와 현재 구조를 대조했다. 현재 별도 Canvas·raycastTarget=false·재사용 풀은 이 방향에 맞는다. [Unity UI 최적화 지침](https://create.unity3d.com/Unity-UI-optimization-tips).
- 이 문서는 정적 검토다. 이전 MSBuild 성공 기록은 보존하지만 이번 검토에서 재빌드/Play/Profiler를 실행하지 않았다. 코드 개선안은 구현하지 않았다.


## 6. F~J 보완 후 재검토 — 2026-10-31

후속 사용자 승인으로 코드/에셋을 수정한 뒤 다시 검토했다. §1~5의 보완 전 결론·코드 줄 번호·수동 지침은 당시 이력이다. 현재 사용법은 [프리팹 기반 가이드](../viewDocs/trait-body-feedback-guide.md), 검증 근거는 [계획 §9.9~9.10](../plans/2026-10-31-trait-body-feedback-plan.md#99-2차-실행-기록--fj)에 있다.

| 항목 | 반영과 판정 |
|---|---|
| R1 / SSOT·O | 지원 조합/Count는 CombatTraitFeedbackProfiles 한 곳. 데이터/Inspector/조립/런타임이 함께 조회한다. 근거는 ICombatTraitStateEvidence로 분리하고 Factory 등록. 같은 근거를 쓰는 특성 추가는 프로필 등록으로 가능하며 Text/Pool/Inspector에 특성 분기 추가 불필요 |
| R2 / Inspector | 현재 지원하는 선택지만 제시하되 지원되지 않는 기존 값은 저장 변경 없이 남긴다. Count=None과 CueTiming=ActivationStart를 같은 화면에서 명시적으로 선택 가능. 실제 클릭 동작은 Play/Inspector 사용자 후속 |
| R3 / 캡슐화 | Plan 컬렉션 private·읽기 전용 값·불변 Occurrence/Binding. private 실행 Session이 시작/State 소비/Shake를 소유. 외부 Map.Remove·Started/Shaken 대입 제거 |
| R4 / Tick | 시간·페이드는 Tick, 실제 몸/대여 조회는 활성 렌더 투영 항목당 한 번. 첫 Show에서 구독하고 마지막 반환/Cancel/Release에 해제. 첫 투영 전 숨김, 매 프레임 LINQ/Find/GetComponent 없음. Profiler 측정 효과는 미확인 |
| S | 입력/프로필·공개 사실 근거·불변 계획/private 실행·View/Pool/Window·Factory로 책임 분리 |
| L | 기존 비차단 Player/Accent 계약과 generation·Cancel·Release 유지. 실행 치환 동작의 실제 재생 성공은 미확인 |
| I / D | 근거의 Kind/Matches, 몸 추적 델리게이트 등 필요한 의존성만 사용. Factory가 카메라·자원·프리팹·근거를 주입. 전역 서비스 탐색 없음 |
| C#/Unity | sealed·블록 namespace·명시적 직렬화 참조·UTF-8/LF·meta. 프리팹별 풀, 입력 비활성, 자체 생성 자원의 명시적 회수. Unity 임포트와 Canvas 정렬·외형은 사용자 후속 |
| 기능 독립성 | 외형 변경은 프리팹/Variant에서 끝남. 시간/투영은 Text, 상태 근거는 Profiles/해석기/Factory. 외형 수정으로 공격/규칙/HUD 판정을 고치지 않아도 됨. 공통 취소/Player 계약 변경 시 영향 검증은 필요 |
| JMH/FW | 생성→소유/해제→소비 경로 추적. 새 서비스 등록/Group/Export/루트 조회·Framework 변경 없음. 새 서비스/Bus/Mesh/Job 계약을 실제 검사했다고 과장하지 않음. Accent 세부 위젯 수명과 기존 Effect Scope 경로 구분 |

기본 TraitBodyText.prefab이 외형 원본이고 공통 설정/줄별 Override는 참조만 가진다. 기존 세 줄은 같은 외형으로 확인돼 Variant 없이 이전했다. 공유 HUD Description Panel/Text와는 별개다.

최종 런타임·Editor MSBuild 오류 0, 소스/에셋 참조·GUID·이전 값·보존 영역 정적 대조 통과. Core·Framework·FieldCamera·공유 HUD·기존 폰트·프리셋은 이번 변경으로 수정하지 않았다. §3의 HUD 연결 안내를 현재 사용자의 필수 작업으로 반복하지 않는다. 사용자의 남은 작업은 갱신된 사용법의 대표 재생·외형·반복/종료·필요 시 Profiler 확인이며, 새 View 테스트 코드는 없다.
