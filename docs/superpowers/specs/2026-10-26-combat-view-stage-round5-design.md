# 전투 View 5라운드 설계 — 선택·입력 사실·조립 경계 (2026-10-26)

- **상태: V5-0~d 및 5라운드 완료(2026-10-26).** 승인한 선택 원본·FactsReader·운영/시험 조립 분리를 구현했고 W17~19 정적 검토 및 a/b/c 각각의 사용자 컴파일·기존 시험·Unity 정상 보고를 인수했다. d는 문서/공유/구조 통합 인수만 수행했다. 직접 실행 여부와 확인 공백·후속 작업은 [Task §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26), 구현/허락 이력은 Task §8·§5에 둔다. 공유 문서는 미전달이다.
- Task: [round5-plan](../plans/2026-10-26-combat-view-stage-round5-plan.md).
- 기준: `11c07868`(새 Core 대응 4라운드 커밋). 조사 시작 작업 트리는 깨끗했다. V5-0에서 이 HEAD 이후 V4-n 결과를 확인하여 사용자 **"컴파일·기존 시험·M01~M14 모두 정상"** 보고를 받았다(2026-10-26). 직접 실행/개별 로그 검증과 구분하며 상세 기록은 Task §9다.
- 범위: 직전 제안의 1~3, 즉 옛 V4-b 선택 원본 분리, V4-c FactsReader/선택 명칭, V4-i 운영/시험 조립 정리다. 이전 Phase ID를 재사용하지 않는다.
- 판단 기준: [설계 원칙 §4·§4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md), [Framework 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [FW 개발 계약](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md). 오래된 문서의 점수/완료 표기보다 현재 코드를 기준으로 삼는다.
- 약어: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 목표와 완료 범위

선택의 유일한 원본을 정보창에서 분리하고, 입력 사실 읽기를 조정자에서 분리한 뒤, 운영 조립이 시험용 생략 경로에 의존하지 않도록 한다. **4라운드의 사용자 동작과 Core/Buffer 계약은 유지한다.**

완료는 파일 수/줄 수/예상 점수로 판단하지 않는다. 다음 세 가지를 코드와 기존 회귀/사용자 확인으로 설명할 수 있어야 한다.

1. 정보창의 표시·슬라이드 변경이 선택/이동/가용성 원본을 바꾸지 않는다.
2. 입력 사실 하나의 추가/변경은 Reader와 실제 소비자/필요한 조립에 머물고 조정자·UI의 무관한 구현을 수정하지 않는다.
3. 운영의 의존성 누락은 사용 전에 명확히 진단하고, 부분 기능 시험은 명시적인 시험 조립을 사용한다.

V4-n의 실제 결과를 V5-0에서 정리한다. 미확인 항목은 통과로 간주하지 않는다. 기준판의 실패가 발견되면 구조 변경 전에 원인/담당을 구분하며, 새 기능 수정이 필요하면 이 설계에 섞지 않는다.

## 1. 현재 코드에서 확인한 결합

| 현재 접점 | 확인한 상태 | 5라운드 처리 |
|---|---|---|
| `Display/Hud/CombatUnitInfo` | `_selected`와 완료 표시 캐릭터 목록에서 SelectedActorId/Cells를 계산. 정보창의 표시 상태와 선택 수명이 한 클래스에 있음 | 순수 선택 상태로 이동, UI는 읽기만 |
| `Display/CombatSceneDisplay` | InspectUnit이 UnitInfo.SelectCell을 호출하고 강조도 UnitInfo에서 읽음 | 선택 상태를 바꾼 뒤 정보창/강조를 같은 호출에서 갱신 |
| `CombatViewSystem.ReadFacts` | Buffer·연결/진행·재생·튜토리얼·선택을 한 메서드에서 조합 | 별도 Reader가 현재 사실을 매번 읽음 |
| `F/CombatSceneInputFactory.CreateSceneInteraction` | `display is ICombatUnitInspection`으로 선택을 발견하고 없으면 Empty. Path에도 선택 공급자의 기본 Empty 경로가 있음 | 이동은 같은 Facts 선택을 읽고 실행 시 표시 형식 탐색을 제거 |
| `CombatViewConnection`·AttachCreated | Inspection은 선택 사항이며 테스트에서 생략. 계약이 Hud 파일 안에 선언됨 | 명시적인 선택 읽기/누름 계약을 Selection 폴더로 옮기고 가짜도 명시 |
| `Input/CombatSceneInputSources` | 이미 모든 생성자 인자가 필수이고 null 검사가 있음 | 재설계하지 않음. 실제 추가 의존성만 연결 |
| 배치 확인창/부분 CreateInteraction | 운영 Factory 깊이에서 Text를 확인창 계약으로 캐스팅. 일부 leaf 생성은 시험용 optional 기능과 공유 | 운영 경계의 typed 확인창 의존성, 부분 조립은 시험 도우미로 구분 |

UI가 숨을 때 HP 텍스트가 남는 등 기존 표시와 실제 선택은 구별한다. 시험 이름의 “FallsBack”만 보고 메인 캐릭터를 자동 선택하는 동작을 새로 만들지 않는다.

## 2. 소유권과 조립안 A (권장, 구현 전 결정)

| 대상 | 생성/소유 | 쓰는 쪽 | 수명 |
|---|---|---|---|
| `CombatSelectionState` | 기존 CombatSceneDisplayFactory에서 1개 생성, CombatSceneDisplay가 소유 | Display만 선택/완료 표시 목록을 갱신 | 표시/씬 연결 하나 |
| `ICombatSelectionReader` | 위 상태가 구현. 별도 가변 객체 없음 | 정보창·강조·연결/Reader가 같은 인스턴스를 읽음 | 상태와 같음 |
| `ICombatUnitInspection` | 실제 Display가 구현, 선택 Reader와 누름 입구 제공 | 조정자의 선택 라우팅 | 표시와 같음 |
| `CombatInputFactsReader` | 기존 입력 Factory 계약을 통해 생성, CombatViewSystem이 보관 | 기존 ReadFacts 위임 입구와 입력/후처리 공급자 | 서비스 바인딩 하나; 재바인딩 시 교체 |
| `CombatActionAvailabilityTracker` | 현재 입력 Factory/SceneInput 소유 그대로 | 공격/이동/UI가 빌림 | 현재 씬 연결 그대로 |
| 시험 조립 도우미 | 시험 어셈블리 내부 | 기존 고정물만 사용 | 각 시험 고정물 |

선택 상태와 Reader는 Framework System/Service가 아니다. 서비스 키·Builder·Group Export·루트 조회를 추가하지 않는다. 상태를 Display와 Connection에 두 벌로 만들지 않는다. Connection은 읽기 참조만 보관한다.

## 3. V5-a — 선택 원본과 명시 계약 (W17)

### 3.1 파일/계약

- 새 `V/Selection/CombatSelectionState.cs`: sealed, Unity 참조 없는 선택 상태. `_selected`와 Display가 고정한 완료 표시 목록의 읽기 참조/현재 StagePhase를 보관한다. Core 현재 상태를 자체 동기화하거나 UI와 각각 목록을 복제하지 않는다.
- 새 `V/Selection/ICombatSelectionReader.cs`: `SelectedEntityId`, `SelectedActorId`, `SelectedActorCells`의 읽기 계약. SelectedEntityId는 정보 대상이며 적도 가능하고, SelectedActorId는 현재 선택이 살아 있는 필드 아군일 때만 값이 있다.
- 새 `V/Selection/ICombatUnitInspection.cs`: Hud 파일 안의 기존 인터페이스를 분리한다. `Selection` 읽기 계약과 `InspectUnit(screenX, screenY)`를 제공한다. 입력 사실 소비자는 Inspection 전체 대신 Reader만 읽는다.
- 기존 CombatUnitInfo의 `_shown`·`_pendingCharacter`·슬라이드·비용 사유 표시는 표현 캐시로 남는다. `_selected`를 독립 가변 사본으로 남기지 않는다. 선택 갱신은 UI 호출의 부수효과가 아니다.
- 각 새 .cs에 .meta를 만들고 기존 MonoBehaviour 파일/네임스페이스/GUID와 씬 직렬화 필드는 유지한다. 순수 인터페이스만 새 네임스페이스로 옮긴다.

### 3.2 선택/표시 순서

1. 기존 DisplayFactory가 선택 상태를 만들고 UnitInfo에는 Reader로, Display에는 상태로 주입한다. 기존 ICombatSceneFactory.CreateDisplay의 공용 생성 재료를 새 서비스 묶음으로 확장하지 않는다.
2. Install에서 새 선택 수명을 초기화한다. Install/Refresh가 공유하는 완료 표시 적용 자리에서 외부/UI 콜백보다 먼저 **불변 CharacterData 참조를 표시용 목록으로 한 번 복사**해 Display가 보관한다. 선택 상태와 UnitInfo는 이 동일 목록/국면을 사용하고 상태 동기화 뒤 정보창/강조를 갱신한다. CharacterBuffer.Characters는 같은 내부 목록을 갱신·비우는 읽기 전용 래퍼이므로 참조 보관/AsReadOnly만으로 시점이 고정되지 않는다. 표시 스냅숏만 허용하는 얕은 복사이며 Core 객체/규칙/선택 원본의 복제가 아니다. 중간 연출의 Buffer 갱신·EndBattle만으로 읽기 결과가 바뀌지 않고, 다음 완료 표시 적용 또는 Display.Clear/Release/새 설치에서 교체·정리한다. Getter/Tick마다 복사하거나 선택 상태/UI가 별도 사본을 만들지 않는다.
3. InspectUnit은 기존 UI 가림/좌표 집기를 통과한 뒤 상태의 칸 선택을 바꾸고 정보창/강조를 바로 맞춘다. 외부 UI 호출 뒤 `_isReleased`와 조정자의 동일 연결 검사는 유지한다. 이 호출에서 전체 Refresh/Core 확인은 하지 않는다.
4. 입력 Facts와 이동은 같은 선택 원본을 읽는다. 운영의 표시 형식 검사/Empty 대체는 없앤다. 가용성의 전투·턴·결과·선택 키/요청 순서는 그대로다.

### 3.3 보존할 선택 수명

| 사건 | 선택/표시 계약 |
|---|---|
| 같은 필드 유닛 재누름 | 선택 유지, 다시 Drag 가능 |
| 다른 개체/적/빈 칸 | 교체/적 정보만/해제. 적은 행동 ID Empty |
| 같은 국면의 진영 턴·일반 결과 갱신 | 살아 있는 필드 선택 유지. 해당 Core 가용성은 기존 Tracker가 재검증 |
| 사망/필드 이탈/국면 변경 | 완료 표시 데이터 기준으로 무효 선택 정리 |
| 정보창 Hide·덱 핀·슬라이드 | 표시만 바꿈. 선택 원본의 새 쓰기 입구로 삼지 않음. 덱 재클릭 토글 유지 |
| StopInput/종료 결과 표시 대기 | 새 입력은 기존 수신/Gate에서 차단. 마지막 화면에 필요한 선택을 성급히 지우지 않음 |
| Display.Clear/Release·새 설치 | 소유 Display가 상태를 정리. UI Clear는 빌린 선택 상태를 임의로 초기화하지 않음 |

실제 선택 보호는 기존 Policy/RouteInput에 남긴다. 상태 클래스가 튜토리얼·공용 AP·공격/이동 자격을 판정하지 않는다.

### 3.4 가짜 표시와 실패 소유권

AttachCreated와 시험용 AttachDisplay 모두 Inspection 인자를 명시적으로 전달하고 Reader도 유효해야 한다. 두 입구 모두 TakeDisplay 뒤 BeginUse에서 읽기 참조를 연결하고, 그 뒤 ContinueAttach의 첫 표시/입력을 만든다. 가짜도 명시적인 선택 Reader/Inspection을 제공하며 무조건 Empty인 운영 대체를 만들지 않는다. 현재 CombatViewSystemTests.FakeDisplay의 선택 기본값 1은 해당 고정물의 명시적 선택 값으로 옮겨 기존 기대를 유지한다. AttachDisplay를 호출하는 CombatViewSystemTests·CombatMovementViewTests·CoreStageCombatCycleTests의 고정물/호출 준비도 Q-T9 후보에 포함한다.

TakeDisplay → BeginUse → 첫 표시/입력 생성의 소유권 순서는 보존한다. 검사 추가 때문에 살아 있는 다른 연결의 표시를 반환하지 않는다. AttachCreated는 현재의 인수 전 ReleaseUnowned·오류 상태 정상 반환 시 반환·인수 뒤 Detach 경계를 보존한다. AttachDisplay는 인수하지 않은 표시를 호출자가 보유하는 기존 의미를 유지하며 AttachCreated로 단순 치환하지 않는다. 필수 계약 검사도 각 입구의 기존 소유권/실패 경계 안에 둔다. 첫 그리기 중 재진입 Detach·중복 Attach·종료 화면 유지의 기존 회귀를 보존한다.

### 3.5 Facts와 이동 관측의 비순환 계약

V5-a에서 Path의 선택 공급자를 Facts로 바꾸는 것과 함께, Interaction/Path의 빈 범위 관측을 **선택 ID를 값으로 받는 읽기 메서드**로 바꾼다. Facts 조립은 현재 Selection Reader에서 선택 ID를 먼저 읽고 그 값을 빈 범위 관측에 전달한 뒤 Facts를 완성한다. 관측은 해당 ID와 기존 State/Character/Range 자료만 읽고, 전체 Facts 공급자·일반 ActiveActor()·선택 공급자를 다시 호출하지 않는다. 일반 경로와 관측 경로가 ID를 받아 개체를 찾는 순수 읽기 부분을 공유할 수 있다.

호출 방향은 `ReadFacts → 선택 ID 읽기 → 빈 범위 관측(ID) → Facts 반환`이다. 현재의 매개변수 없는 관측을 남긴 채 선택 공급자만 `ReadFacts().ActiveActorId`로 바꾸면 `ReadFacts → Interaction → Path → ActiveActor → ReadFacts`가 된다. 따라서 이 관측 변경을 V5-c까지 미루지 않는다. V5-b는 비순환 순서를 그대로 Reader로 옮기고, V5-c는 일반 Path의 optional 선택 공급자만 제거한다. 새 선택 캐시/진입 방지 플래그/기본 Empty로 순환을 숨기지 않는다.

## 4. V5-b — 입력 FactsReader와 명칭 (W18)

### 4.1 Reader 책임/생성

새 `V/Input/CombatInputFactsReader.cs`는 Buffer와 기존 진행 객체/연결 관측을 읽어 CombatInputFacts를 만든다. Core 명령·Preview·확인 번호·선택을 쓰지 않으며, UI/DB를 조회하거나 Policy 판정식을 복제하지 않는다.

- 생성은 기존 ICombatSceneInputFactory/CombatSceneFactory에 추가하는 전용 생성 메서드를 거쳐 정적 입력 Factory가 맡는다. 조정자는 BindServices의 현재 입력 연결 생성 뒤 Reader를 한 번 받아 보관한다. 새 범용 Factory/서비스 키는 만들지 않는다.
- 고정 의존성은 state/input ReadSink, 기존 InputConnection·Playback·Progress·BlockedInputReport다. 현재 Connection과 기존 IsInputReady/HasPendingCommand/IsModalOpen 관측은 이름 있는 읽기 공급자로 받는다. 조정자를 통째로 넘기거나 resolver/서비스 가방을 조회하지 않는다.
- 현재 Connection 공급자는 읽는 시점의 연결을 돌려준다. 서비스 수명 Reader/입력 재료에 특정 씬의 Selection/Display를 캡처하지 않는다. 재바인딩 시 Reader와 기존 입력 재료를 함께 교체/무효화하고, InitData의 View 표 교체는 Reader의 의존성이 아니다.
- 조정자의 ReadFacts는 위임 입구로 남길 수 있다. 생성/바인딩 이전의 잘못된 읽기는 명확한 상태 오류로 진단하며, 정상적인 “바인딩 뒤 아직 연결 없음”을 오류로 바꾸지 않는다.

### 4.2 필드별 원본과 읽기 시점

| 사실 | 원본/보존할 의미 |
|---|---|
| HasBattle·Phase·Completion·S·전투/진영 턴 번호 | 현재 CombatState ReadSink. 전투 없음의 기존 기본 국면/번호 의미 유지 |
| P·미처리 명령·현재 Preview 답 | 기존 입력/상태 싱크. 답은 현재 슬롯 번호와 맞을 때만 반환 |
| SelectedActorId | 현재 Connection의 명시적 Selection Reader. 선택 없음/적은 정상 Empty |
| Shown·입력 모드·아이템 Drag·빈 범위 진단 | 현재 연결/입력 객체. 빈 범위는 이번 읽기의 선택 ID를 값으로 전달하여 관측하고 Facts를 재호출하지 않음(§3.5). 첫 표시/입력 생성 전은 정상적인 미준비 상태 |
| Layer·제출 정지·포기·후처리/표시·모달 | 현재 InputConnection/Progress/Playback와 기존 수명 관측. 공통 판정 원본은 옮기거나 복제하지 않음 |
| 튜토리얼 제한/PracticeGoal 대기 | 기존 BlockedInputReport. Reader에서 새 Jobs/Store/DB 접근 없음 |

`Read(includeUnhandledCommand, followUp)`는 매번 현재 값을 읽는다. **Tick 전체의 Facts를 캐시하지 않는다.** 결과 관측 전/후, 확인 전/후, 입력 제출/선택 변경 뒤의 기존 재읽기 순서가 유지돼야 한다. followUp은 기존처럼 후처리의 외부 입력 차단 투영만 구분하고 제출 닫힘/모달/순번 검사를 우회하지 않는다. includeUnhandledCommand는 해당 조회만 제어한다.

### 4.3 이름 변경 경계

- CombatInputFacts의 `ActiveActorId`/`activeActorId`만 `SelectedActorId`/`selectedActorId`로 바꾸고 실제 View 소비/기존 시험의 기계적 참조를 같은 Phase에서 맞춘다. 두 이름을 영구 별칭으로 남기지 않는다.
- Core 계약/진영 턴/ActorId, FieldHighlightLayer.ActiveActor, 소리/레이어 이름은 바꾸지 않는다. 이는 다른 의미/후속 정리다.
- HasCurrentAcceptedEmptyMovementRange 같은 진단 사실은 그대로 옮긴다. 폐지 기능/타임라인/빈 범위 사실의 삭제는 이번 세 항목 밖이다.

## 5. V5-c — 운영 의존성과 시험 조립 (W19)

### 5.1 운영 경계

- 이미 필수인 CombatSceneInputSources 전체를 다시 만들지 않는다. 배치 확인창은 `IUIConfirmationPresentation`을 명시적 필수 재료로 추가하고, 현재 서비스 바인딩에서 확인한 기능을 넘긴다. Factory 깊이에서 Text를 `as`로 바꿔 null을 늦게 넘기는 경로를 없앤다. 확인창의 Yes/No·취소·오류 처리 정책은 유지한다.
- 운영 CreateSceneInput/CreateSceneInteraction은 선택 읽기·가용성·경로·공격 등 운영에 필요한 의존성을 생성 전에 검사하고 모두 명시적으로 연결한다. 실제 누락을 시험 편의의 Empty/무동작으로 덮지 않는다.
- Path의 일반 판단은 기존 Facts 공급자의 SelectedActorId를 읽도록 통일하고 별도의 optional readSelectedActor/Empty 기본값을 없앤다. 빈 범위 관측은 V5-a에서 확정한 값 인자 경로를 유지하여 Facts를 역호출하지 않는다(§3.5). 선택/사실 공급자의 원본을 두 벌로 두지 않는다.
- 명시적 선택 기능·정상 대기·Input 없음/전투 없음·연결 해제는 오류가 아니다. 모든 null/leaf 생성자를 일괄 필수화하지 않는다. 필수성은 운영의 실제 사용 경계에서 보장한다.

### 5.2 부분 기능 시험

부분 CreateInteraction 조립을 Q-T9 c 승인에 따라 시험 어셈블리의 `CombatInputTestFactory`로 옮겼다. 새 시험 사례가 아니라 기존 고정물 조립 도우미다. 운영 전체 조립은 이 시험 API를 사용하지 않는다.

- 운영 전체 경로는 부분 조립 도우미를 호출하지 않는다. 기존 운영 조립 API와 시험 조립 API를 이름/위치로 구별한다.
- 격리 시험은 필요한 기능과 입력/선택/소리/확인창 가짜를 명시한다. Runtime에서만 필요한 전체 기능을 억지로 생성하거나 실제 Gate/가용성을 우회하지 않는다.
- FakeDisplay/확인창/선택의 명시적 가짜는 시험 파일에 둔다. 기존 결과/명령/소유권/클릭·Drag 단언은 유지한다. 필요 없는 부가 기능의 생략은 시험 도우미 계약에 기록한다.
- 확인창 누락처럼 현재 늦게 실패하던 잘못된 운영 조립의 진단 시점 변경은 Q-V41의 결정 사항이다. 그 시점을 직접 검증하는 타인 단언이 발견되면 별도 허락 없이 기대를 바꾸지 않는다.

## 6. 실제 변경 시나리오와 독립성

| 변경 시나리오 | 현재 함께 이해하는 접점 | 목표 수정/이해 범위 |
|---|---|---|
| 정보창의 슬라이드/비용 레이아웃만 변경 | 선택 ID/캐릭터 검색과 입력 조회도 같은 UI 파일 | UnitInfo 표현과 관련 기존 표시 검증. 선택/Reader/가용성은 원본 변경 없음 |
| 선택 정책/무효화 변경 | UnitInfo·Display·입력의 형식 발견 경로 | SelectionState·선택 적용 Display/기존 선택 회귀. Core/음향/결과 Consumer는 수정 없음 |
| 입력 사실 하나 추가 | 조정자 ReadFacts와 해당 기능 | Reader·Facts·실제 소비자·필요한 조립. 사실 추가 이유 없이 UI/결과 재생 내부를 고치지 않음 |
| 기능 의존성 추가/교체 | 운영/부분 시험이 같은 optional Factory 길 | 운영 Factory/명시 계약과 해당 기능, 영향받는 고정물만. Global/루트 등록 변경 없음 |
| 공통 입력 수명/공유 Preview 계약 변경 | 선택/가용성/경로/공격 모두 영향 | 이 경우 통합 회귀가 필요함을 인정. “독립성”을 공유 계약 검증 면제로 해석하지 않음 |

## 7. SSOT·SOLID·FW/JMH 점검

| 기준 | 이 설계에서의 점검 |
|---|---|
| SSOT | 선택 ID는 SelectionState 하나, 표시 캐시는 파생값. 완료 표시 목록은 Display가 한 번 고정하고 Selection/UI가 같은 참조를 읽음. 전투/가용성/비용은 Core/Buffer, 정책은 기존 Policy. Reader는 새 상태 저장소가 아님 |
| SRP | 선택 수명/표현/사실 읽기/조립의 변경 이유를 나눔. 조정자는 기존 실행 순서를 조정 |
| OCP | 입력 사실/부분 시험 확장이 실제 접점에 머물고 Global/조정자의 무관한 분기를 늘리지 않음 |
| LSP | 실제/가짜 Selection Reader의 Empty/적/유효 아군 의미 일치. 가짜가 운영 누락을 정상 성공으로 바꾸지 않음 |
| ISP | UI/Reader는 선택 읽기만, 조정자는 Inspection 입구, Factory는 생성 계약. 전체 Display/조정자를 Reader에 주지 않음 |
| DIP | 입력은 명시적 Reader/기존 Sink에 의존. Display 형식을 실행 중 발견하거나 InGame DB/Core 구현을 직접 읽지 않음 |
| FW-001~004/016 | 새 서비스/모듈/루트 Export/Builder 변경 없음. 현재 생성·소유권 이전·실행 순서를 보존 |
| FW-005~009 | Input Configure/Submit/Layer, UIBase·UIId·부모·Policy·등록/이력·개폐 시점을 유지. 새 이벤트 구독 없음 |
| FW-010~015/자원 수명 | Mesh/Pool/Queue/DB 계약 변경 없음. 기존 생성 실패·Release·대여 반환을 분리로 우회하지 않음 |

## 8. 구현 결정 (2026-10-26 A 승인)

| ID | 권장안 A | 다른 안/범위 영향 |
|---|---|---|
| Q-V39 | DisplayFactory 생성/Display 소유 SelectionState, 공유 표시 스냅숏, UI 읽기 전용, 두 Attach 입구의 Inspection 명시화 | Connection이 직접 상태를 생성하는 안은 표시 생성/주입 계약까지 다시 설계해야 함 |
| Q-V40 | 매번 읽는 FactsReader·View Facts 명칭 변경, 기존 순서/진단 필드 유지 | Tick 캐시/새 입력 정책/범용 Context 재구성은 이번 제외 |
| Q-V41 | 운영의 typed 확인창·선택/가용성 의존성 필수, 부분 조립은 시험 도우미 | 기존 optional 길을 공통 운영 API로 유지하는 안은 선택한 목표를 충족하지 못함 |
| Q-V42 | Q-T9의 명시한 기존 고정물/기계적 참조 변경과 시험 도우미 파일 허용 | 허락되지 않은 파일/단언 변경은 실행 전 범위 확인. 4라운드 Q-T8을 재사용하지 않음 |
| Q-V43 | W17(a)·W18(b)·W19(c) 끝에 새 읽기 전용 검토 각 1개, Phase별 사용자 Unity 확인 | 구현 뒤 해당 W 끝에서 실행. 앞선 설계 검토를 W17~19의 완료로 대체하지 않음 |

문서 작성 요청과 앞선 "새 에이전트로 5라운드 설계랑 Task 계획 검토해줘. 구현은 아직 하지 말고."는 당시 구현 승인이 아니었다. 이후 사용자 **"그럼 수정한 설계랑 Task 계획대로 V5-0부터 차례대로 진행해."**로 위 권장안 A, Q-T9 a~d의 한정 고정물/기계적 참조·도우미, W17~19의 새 읽기 전용 검토 각 1개가 승인됐다. Q-T9 e는 후보 조사만 포함하며 타인 단언 변경/새 사례/허용 파일 확대는 포함하지 않는다. Phase별 보고/Unity 확인 절차는 유지한다.

## 9. 제외와 최종 인수

제외: Core/Buffer/Framework/GlobalSystem/ServiceKeys·FieldCamera/Architecture·다른 씬/기획 데이터, 새 입력 기능, 결과 Consumer·표면 높이·공통 레이어/배경음·폐지 호환 사슬 삭제·예약 키 저장값/씬 참조 삭제·구조 A/B 측정. 새 `[Test]`/`[TestCase]`·새 회귀 사례와 MSBuild는 하지 않는다. 기존 시험·Unity 실행은 사용자가 한다.

최종 인수는 선택 원본/읽기 경로/운영·시험 호출 분리의 코드 근거와 Task의 기존 회귀 확인을 함께 본다. 선택·가용성·AP·턴/실습·수명/실패 정리의 기존 동작이 유지돼야 한다. 미확인·실패·구성 문제와 구조 이월을 구분하며 **컴파일 미확인**을 성공으로 대체하지 않는다. 커밋·push·외부 발송은 사용자가 한다.

## 10. 2026-10-26 — 새 에이전트 설계 검토

사용자의 별도 요청으로 새 읽기 전용 에이전트 1개가 HEAD `11c07868`의 코드와 설계/Task를 대조했다. 부모는 기존 시험 후보·허락 범위·선행조건·문서 연결을 별도로 확인했다. 이 검토는 V5-0 착수나 구현 뒤 W17~19 검토의 완료가 아니다.

| ID/중요도 | 초안의 결함과 코드 근거 | 문서 보완 |
|---|---|---|
| V5-R01 / P1 | ReadFacts → Interaction 빈 범위 관측 → Path.ActiveActor → 선택 공급자. 공급자만 전체 Facts 읽기로 바꾸면 재귀. 근거: CombatViewSystem:898, CombatInteraction:129, CombatPathInput:70/217 | §3.5 비순환 계약, Va.6에서 선택 ID 값 인자 관측을 함께 변경. b/c와 R05에도 보존 조건 연결 |
| V5-R02 / P2 | CharacterBuffer의 AsReadOnly 목록은 InstallViews/EndBattle에서 같은 내부 목록이 바뀜. 참조 보관만으로 완료 표시 시점이 고정되지 않음. 근거: CharacterBuffer:105/594/636, CombatDisplayValueReader:47 | §3.1~3.2, Va.1/3에서 Display 소유의 공유 표시 목록을 한 번 고정. R03/구조 인수에 연출 중 갱신·정리와 표시 수명 구별 |
| V5-R03 / P2 | AttachDisplay가 BeginUse 없이 첫 표시로 진행하므로 형식 발견 제거 뒤 선택 Reader가 없음. 근거: CombatViewSystem:519, CombatViewSystemTests:1375/1588 | §3.4, Va.5/7에서 두 Attach 입구의 명시적 주입과 서로 다른 반환 책임 보존. 실제 호출 파일 2개를 Q-T9 a 후보에 추가, R10 연결 |

부모가 위 근거를 확인하여 **설계/Task 문서만 보완했고, 같은 검토자의 집중 재검토에서 3건 모두 문서상 해소됐으며 추가 구체적 결함/모순은 확인되지 않았다.** 이는 정적 설계 검토의 결과다. 설계 검토 종료 시점에는 새 시험/사례·코드·에셋 변경, 컴파일/Unity/기존 시험 실행이 없었고 Q-T9 허락과 V4-n 실제 결과도 미수집이었다. 이후 순차 진행 승인과 V4-n 사용자 정상 보고를 받은 V5-0의 결과는 Task §9에 기록했다.
