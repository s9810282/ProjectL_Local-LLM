# 튜토리얼 최신 전투 규칙 교체·공격·턴 종료 실습 설계

- 작성: 2026-11-10, Codex. 사용자의 이번 설계 정리 요청에 따른 문서 작업이다.
- 상태: **설계 확정(2026-11-10 사용자 승인). Phase A·B·C 코드 및 D의 17단계 정의 반영·정적 확인 완료, 사용자 Unity 확인 대기. 아군 4종의 Cartoon/Opaque·외곽선 속성은 사용자 확인, 실제 Pass 출력은 미확인(실행 계획 §9.5). D1e 재생 인수 대기, E 미착수.**
- 실행 계획: [최신 규칙 튜토리얼 교체 Task 계획](../plans/2026-11-10-tutorial-current-rules-refresh-plan-Hojin.md). 설계 확정과 계획 작성 요청은 구현 Phase의 실행 승인을 뜻하지 않는다.
- 사전 확인: 사용자 지시로 Phase 0의 파일·코드 조사를 수행했다. 일반 새 Run의 전투 1120/로드아웃 1은 정적 콘텐츠 전제를 만족한다. 초기에는 아군 FBX 재질 지원 미확인으로 C를 보류했으며, 이후 사용자의 재질 구성 확인과 소스 대조를 근거로 C 코드를 진행했다. 실제 Pass 출력은 여전히 Unity 미확인이다. 초기 근거는 실행 계획 §9.1, 후속 확인·재개 근거는 §9.5다.
- 확인 기준: 초기 로컬 HEAD `f7b842aad`, C 재개는 `e53040cea`, D는 `cf1b17e3b`와 현재 작업 파일. 초기 사용자의 `targetMapKey: 10000 → 1` 변경은 A에서 보존했다. D 시작 에셋은 다시 10000이어서 확정 계획 D1d에 따라 1로 맞췄다. 직접 구현 지시에 따라 A의 자동 진행 제거·진입 대기·카드 개폐 소유권 정리, B의 새 실습 판정·입력 제한, C의 UI 점선·모델 강조/정리, D의 17단계 콘텐츠와 빈 강조 설명의 검증/중앙 배치를 반영했다. 검증 근거는 실행 계획 §9.2/§9.3/§9.5/§9.6이다.
- 기본 계약: [기존 튜토리얼 설계](2026-10-18-tutorial-buffer-job-public-contract-design-Hojin.md). 이 문서는 그 문서의 진행 내용·실습·강조·시작 조건 중 아래에 명시한 부분을 대체한다. 기존 공개 요청·세션·정리 구조를 유지하고, 시간 만료로 설명을 넘기는 계약은 제거한다(§6.4, 2026-11-10 사용자 확정).
- 기존 구현 기록: [튜토리얼 구현 계획](../plans/2026-10-19-tutorial-implementation-plan-Hojin.md). 그 문서의 완료 표는 이번 변경의 완료 근거가 아니다.

## 1. 요구와 범위

### 사용자 확정 사항

1. 기존 튜토리얼 틀을 유지하고 바뀐 게임 규칙에 맞춰 교체한다.
2. 기존 배치·이동 실습에 **공격 실습과 턴 종료 실습**을 더한다.
3. 공격 실습은 **적 유닛 또는 적 넥서스에 공격이 닿으면 통과**한다. 피해량이 0이어도 인정한다. 명중하지 않은 정상 공격만으로는 통과하지 않는다.
4. 이번 작업은 설계부터 정리한다. 코드·씬·에셋 구현 승인으로 해석하지 않는다.
5. **몇 초 뒤 자동으로 다음 설명으로 넘어가는 기능을 없앤다.** 설명은 사용자가 기존 다음 입력(마우스 클릭)을 해야 넘어간다. 마지막 설명도 확인 전에는 완료하지 않는다. 실습은 기존처럼 실제 목표 달성으로 진행한다.
6. **UI 강조에는 점선 테두리, 유닛 강조에는 모델 아웃라인을 함께 표시한다.** 기존의 주변을 어둡게 하고 대상을 드러내는 강조에 추가한다. 세부 표현·수명 제안은 §6.5다.

### 승인된 세부 설계

- 아래의 17단계 구성, 세부 문구, 입력 허용표, 공격 회복 경로, 마지막 안내 시점과 §6.5의 표현·수명을 2026-11-10 사용자 설계 승인으로 함께 확정했다. 아래의 ‘제안/기본안’ 표현은 초안 작성 당시의 설명이며 이제 이 구현의 기준이다. 색·두께·점선 간격 등 Unity 조정값은 §6.5대로 구현 시 조정한다. 후속 기능 추가는 별도 변경으로 다룬다.
- 실습 대상 유닛·목적지·공격 대상을 고정하지 않는다. 어느 아군으로든 정상 조작을 배우게 한다.
- 공격 실습에서는 이동·이동 되돌리기·턴 종료를 허용해 사거리/행동 사용 상태 때문에 갇히지 않게 한다. 통과는 공격 결과만 인정한다.
- 턴 종료 실습에서는 입력을 제한하지 않는다. 플레이어는 다른 유닛도 자유롭게 이동·공격한 뒤 턴 종료를 누르고, 적 응답 뒤 다음 안정된 입력 시점에 마지막 안내를 보여 준다(2026-11-11 사용자 변경: "턴 종료를 강제하지 말고 다른 유닛들도 사용할 수 있게". 처음 판은 턴 종료만 제출).
- 종료 확인창은 현재 정책처럼 튜토리얼 입력 제한 중에는 생략한다. 튜토리얼 완료 뒤 일반 전투의 확인창은 그대로다. 24단계는 2026-11-11부터 제한이 없으므로, 행동하지 않은 아군이 있으면 일반 전투처럼 확인창이 뜬다.
- 이번 게임 실행에서 튜토리얼을 마지막 안내까지 마쳤으면, 패배·포기 뒤 새 Run의 1-1에서는 다시 실행하지 않는다. 중간에 끊긴 회차는 세지 않고, 게임을 다시 실행하면 다시 나온다(2026-11-11 사용자 변경, 기본 계약 §15.2·§15.12를 바꿈).

### 포함·제외

포함: 정의 에셋의 단계·문구·강조 대상, 실습 목표 두 종류, 입력 제한 두 종류, 결과 관찰, 진입 연출 대기 연결, **설명 자동 진행 시간·타이머·만료 전이 제거**, **UI 점선 테두리·유닛 모델 아웃라인**, 기존 UI의 강조 영역과 개폐 연결, 관련 문서 정합성.

제외: Core 규칙·수치 변경, 전용 튜토리얼 전장/강제 배치/적 AI 정지, 영구 완료 저장, 전체 건너뛰기, 신규 공격/이동 조작, 이동 되돌리기·특성·아이템·넥서스 증원의 별도 실습, FieldCamera·Architecture 변경. 밤의 고정 턴 수나 이동 거리/비용 수치를 새로 가르치지 않는다.

### 검토한 접근

| 접근 | 범위와 비용 | 선택 |
|---|---|---|
| 문구·강조만 교체 | 가장 작지만 공격·턴 종료를 실제 했는지 확인하지 못함 | 사용자가 실습 추가를 선택하여 제외 |
| 기존 Buffer·Plan·Job·UI에 목표·제한을 확장 | 진행·정리 구조 재사용. 내용과 전투 접점에 변경을 한정 | **사용자가 선택한 방향** |
| 전용 전장·스크립트 전투로 교체 | 배치·사거리·적 행동을 보장하기 쉽지만 맵/전투/기획 작업이 크게 늘어남 | 이번 범위에서 제외 |

## 2. 현재 코드에서 확인한 사실

아래는 설계 시작 시점의 조사 기록이다. 이후 A~D 반영 상태와 검증 수준은 위 상태 및 실행 계획 §9.2~§9.6을 따른다.

아래 코드 경로 약어는 `Assets/ProjectL/Script/Systems/` 아래다. `B`는 `RuntimeSystem/BufferSystems`, `L`은 `RuntimeSystem/LifeCycleSystem/LifeCycles`, `V`는 `RuntimeSystem/InGameSystem/ViewSystem`, `C`는 `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `D`는 `DataBaseSystem`이다. `DataBaseSO/` 에셋 경로의 기준은 `Assets/ProjectL/`이다.

| 현재 사실 | 근거 | 설계 영향 |
|---|---|---|
| 정의는 13단계: 설명 11·실습 2. 실습은 12 배치, 18 이동 | `DataBaseSO/ViewGroup/TutorialDefinition/Item/TutorialDefinition_1-1.asset` | 기존 키를 유지하고 22~25 추가 |
| 일반 행동은 Available에서 이동 가능, Available/Moved에서 공격 가능 | `C/Contracts/Combat/UnitActionUseRules.cs` | 이동 후 공격 가능, 공격 후 이동/재공격 불가를 설명 |
| AP 예산·이동/공격 코스트는 폐지됐고 일반 행동 사용 상태가 남음 | `C/Application/Combat/CombatActionAdmission.cs`, [코스트 폐지 인계](../handoffs/2026-10-29-combat-cost-retirement-consumer-cutover.md) | 17·21의 코스트 설명과 8번 강조 대상을 제거 |
| 플레이어 턴 종료는 `EndPlayerTurn` 명령. 자기 진영 턴 시작 때 살아 있는 필드 유닛의 행동 상태를 Available로 변경 | `C/Application/Combat/CombatTurnCalculator.cs` | 턴 종료는 유닛별 행동 종료와 구별. 회복을 View가 수행하지 않음 |
| 현재 `PlayerTurnsPerNight`는 99 | `C/Contracts/Common/PlayerTurnRules.cs` | 과거 문서의 8턴을 안내에 복사하지 않음. 고정 횟수 안내 자체를 제외 |
| 이동은 드래그 경로를 제출. 공격 지정 방식은 기존 공격 모드가 처리 | `V/Combat/Input/MovePath/CombatPathInput.cs`, `V/Combat/Input/Interaction/AttackTargetingInteraction.cs` | 확인된 드래그 이동을 안내. 클릭 이동 등 새 기능을 전제하지 않음 |
| 현재 강조의 CurrentActorUnit은 실제로 메인 캐릭터를 찾으며 ActionCost도 그 HUD를 찾음 | `V/Tutorial/TutorialViewFocusLocator.cs` | 이 두 대상을 새 정의에서 사용하지 않음 |
| 실습 통과는 결과 순번/수락/실제 이동/표시 완료를 읽음 | `L/Tutorial/TutorialFactsReader.cs` | 같은 방식으로 공격·턴 종료를 추가 |
| `AttackResult.CurrentStage`는 운영 `FromFacts`에서 null일 수 있음 | `C/Contracts/Results/AttackResult.cs` | 공격 대상 진영을 이 필드에서 찾지 않음 |
| 진입 연출 전체 대기는 `ICombatViewService.Progress.IsEntryPresentationPending`으로 이미 공개됨 | `V/Combat/ICombatViewService.cs`, `V/Combat/Input/CombatViewProgress.cs` | 새 타이머/전투 상태 없이 기존 사실을 읽음 |
| 튜토리얼 설명 시작은 현재 진입 연출 대기를 보지 않음 | `L/Tutorial/Plans/TutorialPlan.cs`, [배치 연출 설계 §1](2026-11-07-deployment-deck-entry-design.md) | 설명 시작 조건에 진입 완료 추가 |
| 제한 중 턴 종료는 미행동 유닛 확인창을 생략함 | `V/Combat/Input/ActionButtons/CombatEndTurnEntry.cs` | 새 제한도 이 정책을 유지 |
| 튜토리얼 덮개는 화면 Rect 목록만 받아 구멍을 그림. 프리팹은 Overlay Canvas, 정렬 110, 기준 해상도 1920×1080 | `V/Tutorial/TutorialViewOverlay.cs`, `Prefabs/UI/Tutorial/TutorialUI.prefab` | 기존 Canvas에 입력을 받지 않는 점선 표시 위젯 추가 |
| Cartoon/EntityCartoon에는 `_OutlineColor`·`_OutlineWidth`가 있고 모델 외형 합성은 UnitModelAppearance가 소유 | `Render/Shader/.../BaseShader/Shader/Cartoon.shader`·`EntityCartoon.shader`(Script 아래), `V/Combat/Display/Unit/UnitModelAppearance.cs` | 모델 외형의 같은 작성 경로에 튜토리얼 강조 층을 추가하는 안. 다른 효과와 별도로 PropertyBlock을 덮지 않음 |

오래된 설계의 유닛 턴·공유 AP·8턴·공격 실습 없음은 이번 내용의 기준이 아니다. 코드는 조회만 했고 컴파일·재생은 확인하지 않았다.

## 3. 플레이어에게 보이는 진행

총 **17단계: 설명 13개, 실습 4개**로 확정한다. 목록 순서가 진행 순서이며 단계 키를 연속 번호로 다시 매기지 않는다. 기존 8·9·10·12·13·14·15·16·17·18·19·20·21을 유지하고 22·23·24·25를 붙인다.

| 키 | 종류 | 안내 문구 초안 | 강조/통과 |
|---:|---|---|---|
| 8 | 설명 | 카드를 필드의 배치 가능한 칸으로 드래그해 유닛을 배치하세요. | Hand |
| 9 | 설명 | 메인 캐릭터는 전투에 반드시 배치해야 합니다. | MainCharacterCard |
| 10 | 설명 | 배치를 마쳤다면 전투 진입 버튼으로 전투를 시작하세요. | EnterCombatButton |
| 12 | 실습 | 메인 캐릭터를 포함해 유닛을 배치한 뒤 전투 진입 버튼을 누르세요. | EnterCombat |
| 13 | 설명 | 이 전투에서는 적 넥서스를 파괴하면 승리합니다. | EnemyNexus |
| 14 | 설명 | 메인 캐릭터의 체력이 0이 되면 패배합니다. | MainCharacterUnit·MainCharacterHealth |
| 15 | 설명 | 플레이어 턴에는 행동할 아군을 골라 조작할 수 있습니다. | PlayerUnits(신규) |
| 16 | 설명 | 움직일 아군을 선택하고, 유닛을 이동 가능한 칸까지 드래그하세요. | PlayerUnits |
| 17 | 설명 | 각 유닛은 한 턴에 한 번 이동한 뒤 한 번 공격할 수 있습니다. 공격부터 하면 그 턴에는 이동할 수 없습니다. | PlayerUnits |
| 18 | 실습 | 아군을 한 칸 이상 이동하세요. 움직일 수 없다면 다른 아군을 선택하거나 턴을 종료해 다시 시도하세요. | MoveAtLeastOneCell |
| 19 | 설명 | 공격할 아군을 선택한 뒤 공격 버튼으로 공격 모드를 켜세요. | AttackButton |
| 20 | 설명 | 공격 범위와 미리보기를 확인하고 적 또는 넥서스를 향해 공격하세요. 유닛에 따라 칸이나 방향을 지정합니다. | EnemyUnits·EnemyNexus |
| 21 | 설명 | 공격할 대상이 닿지 않으면 이동하거나 다른 아군을 선택하세요. 행동을 마친 아군은 다음 플레이어 턴에 다시 행동할 수 있습니다. | PlayerUnits·AttackButton |
| 22 | 실습 | 적 또는 넥서스에 공격을 한 번 맞히세요. 필요하면 이동하거나 턴을 종료해 다시 시도하세요. | AttackEnemyOnce(신규) |
| 23 | 설명 | 행동을 마쳤다면 턴 종료를 누르세요. 적이 행동한 뒤 다음 플레이어 턴이 시작됩니다. | EndTurnButton(신규) |
| 24 | 실습 | 턴 종료 버튼을 눌러 플레이어 턴을 마치세요. | EndPlayerTurnOnce(신규) |
| 25 | 설명 | 기본 조작 실습을 마쳤습니다. 각 플레이어 턴이 시작되면 아군은 다시 행동할 수 있습니다. 이제 전투를 이어가세요. | 강조 없음·중앙 배치 |

- 8은 **맵 등장 → 배치 안내 → 카드 등장**이 모두 끝난 뒤 표시한다. 튜토리얼이 먼저 카드를 열지 않는다.
- 12·18·22·24는 기존 상단 실습 띠를 쓰고 덮개를 닫는다. **설명은 클릭으로만 넘기며 시간 제한 없이 기다린다.** 실습은 시간·클릭으로 통과하지 않는다.
- 18에서 움직인 유닛과 22에서 공격하는 유닛은 달라도 된다. 학습 목표가 특정 유닛의 연속 행동을 증명하는 것은 아니다.
- 24 통과 뒤에는 적의 정상 행동과 표시를 기다린다. 25는 기존 PlayerInput 시작 조건을 만족할 때 표시한다. 밤 경계라 Deployment로 돌아온 경우에도 안정된 배치 입력 시점에 같은 중립 문구를 표시한다. 다음 전투 진입을 강요하지 않는다.
- 마지막 설명을 사용자가 확인하고 기존 정리가 끝나면 Finished다. 오래 기다려도 마지막 설명은 자동으로 닫히지 않는다. 전투 클리어는 튜토리얼 정상 완료 조건이 아니다.
- 13의 문구는 넥서스 목표의 첫 일반 전투가 전제다. 현재 실행 대상에 보스 목표 전투가 섞이는 경우, 배포 전에 대상 정의/내용을 별도로 결정해야 하며 이 문구를 보스전에 그대로 쓰지 않는다.

## 4. 실습 통과 계약

### 4.1 공통 판정과 기준점

세션과 같은 `BattleInstanceId`, 현재 실습의 `PracticeResultSequence` 뒤 결과만 읽는다. `Kind == Command`, `Accepted == true`를 공통 전제로 하고, 이동·공격·턴 종료는 `PresentedResultSequence >= result.Sequence`일 때만 통과 사실이 된다. 결과 생성 시점의 ID를 읽으며 현재 선택을 통과 조건으로 쓰지 않는다.

**결과/대상 조회보다 먼저 종료 수명을 확인한다.** FactsReader는 세션과 같은 전투가 존재하고 `HasBattle == true`, `Current != null`, `Completion == null`일 때만 실습 결과의 대상 사실을 조회한다. 같은 전투에 Completion이 있으면 목표 통과 사실을 읽지 않고 기존 종료 사실을 Plan에 전달해 BattleEnded가 먼저 처리되게 한다. EndBattle 이후에는 결과 이력·전투 ID·Completion이 남아도 Character Buffer와 Current가 정리되므로, 그 정상 수명을 대상 누락 오류로 취급하지 않는다. 다른 전투/화면 해제도 기존 정리 계약을 따르며 이전 대상 조회를 하지 않는다.

배치 실습은 기존 계약처럼 `StartResultSequence` 뒤의 수락된 EnterCombat을 본다. 이후 설명은 기존 전투 결과 표시 대기로 막으므로 배치 판정 방식 자체는 바꾸지 않는다.

기준점은 **실습 단계 설치 시** 기존 `TutorialSessionChanges.Enter`가 설치한다. 안내가 표시된 뒤 결과 순번을 다시 잡지 않는다. 이중 클릭, 늦은 콜백, 이전 실습 중 회복용 턴 종료가 다음 실습을 선통과시키지 않는다.

`TutorialFactsReader`는 사실을 읽고 `TutorialPlan.IsPracticeGoalMet`이 목표와 사실을 대응한다. Job·UI·입력 콜백은 성공 여부를 대신 정하지 않는다. 승패/이탈/오류는 기존 우선순위를 유지한다.

### 4.2 이동 — 기존 의미 유지

`CommandKind == Move`, `PathMovement != null`, `EnteredCells.Count >= 1`, 표시 완료. 부분 이동도 인정한다. 적 자동 이동, 강제 이동, 되돌리기, 미리보기, 턴 종료는 인정하지 않는다. 새 목적지·거리 수치를 고정하지 않는다.

### 4.3 공격 — 적 또는 넥서스에 실제 명중

1. 공통 조건에 더해 `CommandKind == Attack`, `Attack.HasValue`, `Attack.Value.Succeeded`.
2. 해당 **일반 공격 결과의 `Hits`**에 다음 대상의 `TargetId`가 하나 이상 있어야 한다.
   - 공개 `ICharacterBufferReadSink`에서 확인되는 `Owner.Enemy` 유닛.
   - 공개 `ICombatStateBufferReadSink.Current.Structures`에서 확인되는 `Owner.Enemy`이고 `Definition.Key == Structure_Headquarters`인 구조물.
3. `DamageDealt > 0`, 처치, 특정 유닛/조준 방식/공격 횟수는 요구하지 않는다. 0피해 명중, 광역·추가 대상 명중도 인정한다. 한 공격에 여러 명이 맞아도 한 번 통과한다.
4. 빈 칸/빗나감으로 Hits가 비었거나 아군/중립 대상만 맞으면 통과하지 않는다. 미리보기, 모드 전환, 취소/거절 명령, 자동 적 공격, 반응·아이템·특성만의 피해는 통과시키지 않는다.
5. 사망·제외된 적도 명중 대상일 수 있으므로 대상 조회에 생존/Field 필터를 넣지 않는다. 현재 Character Buffer는 같은 전투에서 죽은 대상의 기록과 정체성을 보존한다(`PrepareCharacterRecords`). 넥서스도 파괴됐다는 이유로 제외하지 않는다.
6. `Hits.TargetId`는 실제 피격자를 기준으로 한다. 희생 등으로 대신 맞은 적도 인정한다. `Contacts.OriginalTargetId`나 선택 칸만으로 명중을 추정하지 않는다.
7. §4.1의 **활성·미종료 전투 조회 조건을 만족한 상태에서만**, 피격 대상 ID를 공개 사실에서 전혀 확인할 수 없는 경우 성공으로 추정하지 않는다. 구현 시 결과·Buffer 수명 계약 위반 여부를 확인하고 대상 ID/결과 순번이 드러나는 오류로 보고한다. 정상 EndBattle 뒤 기록 정리는 이 오류에 포함하지 않는다. 미래 계약 변경 때문에 자료가 부족하면 Core 요청으로 분리한다. View에서 적 여부/피해를 재계산하지 않는다.

`L/Tutorial/TutorialPlanSources`에는 필요한 `ICharacterBufferReadSink`만 더한다. 기존 CombatState/ResultHistory를 함께 사용하며 InGame DB나 Core Application 호출을 추가하지 않는다. `Attack.CurrentStage`는 사용하지 않는다.

### 4.4 턴 종료 — 실제 종료 명령과 표시

공통 조건에 더해 `CommandKind == EndPlayerTurn`이고 같은 결과에 `CombatTurnEvent(Kind == PlayerTurnEnded)`가 있는 수락된 명령을 확인한다. 버튼/키를 누른 사실, 확인창이 열림, 자동 적 턴 종료, 플레이어 턴 카운터만의 변화로 통과하지 않는다.

24의 성공 조건은 수락된 종료 결과의 표시 완료까지다. 적 턴 완료는 별도 실습 목표/카운터로 저장하지 않는다. 25의 설명 시작 조건이 자동 적 진행과 아직 표시하지 않은 결과를 기다린다. 아직 행동하지 않은 아군이 있거나 아무 유닛도 선택하지 않았어도 정상 종료할 수 있다.

### 4.5 승패와 중단

공격이 넥서스를 파괴하거나 턴 종료 중 기믹/적 공격으로 전투가 끝나면 기존 **BattleEnded 우선**을 유지한다. 같은 기회에 실습 성공이 보여도 Cancelled(BattleEnded)로 정리하고 남은 설명을 띄우지 않는다. Finished로 바꾸거나 전투 종료를 지연하지 않는다. 재진입·재실행 정책도 기존 `TutorialViewTarget`을 유지한다.

## 5. 입력 제한과 막힘 회복

### 5.1 제한 종류와 허용표

기존 `None = 0`, `MoveOnly = 1`은 유지한다. 새 값 `AttackPractice = 2`, `EndTurnOnly = 3`을 제안한다. `TutorialPracticeGoal`은 기존 0~2를 유지하고 `AttackEnemyOnce = 3`, `EndPlayerTurnOnce = 4`를 추가한다.

`TutorialStepRule.CanPass`는 기존 목표/제한 조합을 유지하면서 새 목표는 각각 `AttackEnemyOnce + AttackPractice`, `EndPlayerTurnOnce + None 또는 EndTurnOnly`를 허용한다(2026-11-11 사용자 변경으로 턴 종료 목표에 None을 더함, 24단계는 None을 쓴다). 설명에 실습 목표/제한을 넣거나 이동 목표에 공격 전용 제한을 붙이는 잘못된 정의는 기존 초기화 검증에서 예외로 막는다. 목표 두 종류를 추가하면서 실습의 필수 안내/막힌 입력 안내 검증도 유지한다.

아래는 **같은 전투의 Running/Practice/PracticeGoal 대기**에서만 적용하는 튜토리얼 허용 범위다. 기존 국면·입력 Layer·Core 가용성·미처리 명령·연출·모달·완료 Gate는 모두 추가로 만족해야 한다.

| 단계 | 제한 | 전투 중 허용 명령 | 허용 미리보기 | 실습 통과 |
|---|---|---|---|---|
| 12 배치 | None | 기존 배치 입력 전체 | 기존 그대로 | EnterCombat |
| 18 이동 | MoveOnly | Move, EndPlayerTurn | Move, MoveRange, ActionAvailability | 실제 이동 |
| 22 공격 | AttackPractice | Move, Attack, UndoMove, EndPlayerTurn | Move, MoveRange, AttackRange, Attack, UnitAttackRange, ActionAvailability | 적/넥서스 피격 |
| 24 턴 종료 | None(2026-11-11 바뀜, 처음 판 EndTurnOnly) | 기존 전투 입력 전체 | 기존 그대로 | 실제 플레이어 턴 종료 |

- 선택·카메라·정보 확인·취소·설정·포기는 기존 허용 경로를 유지한다. 정보를 고르는 일이 이동/공격을 제출하지 않는다. 모드 해제·입력 정리는 제한 때문에 막지 않는다.
- 아이템 장착/교체·특성 행동 등을 실습 성공의 대체 경로로 추가하지 않는다. 제한된 실습 중 새 실행 명령은 위 허용 목록만 따른다.
- 18·22가 미완료인 채 Deployment로 돌아오면 **PlaceMainCharacter·PlaceUnit·ReturnUnit·EnterCombat와 대응 배치 미리보기**를 기존 정상 조건 아래 허용한다. 필수 메인 재배치가 필요한 경우도 복구할 수 있게 한다. 이전 MoveOnly의 EnterCombat만 허용하던 회복 범위를 보완하는 제안이다.
- 22에서 사거리 밖/모든 유닛 행동 사용/공격 실패라면 다른 아군·이동·되돌리기·턴 종료로 재시도한다. 튜토리얼이 아군을 이동시키거나 적을 소환하거나 자원을 보충하지 않는다.
- 회복용 턴 종료는 18·22를 통과시키지 않으며, 뒤의 24에서 새로 한 번 종료해야 한다.
- 절대 이동/공격할 수 없는 전장까지 해결한다고 약속하지 않는다. 실행 대상 전장에 이동 가능 아군, 도달/공격 가능한 적 또는 넥서스가 있어야 한다. 이것은 배포 전 콘텐츠 확인 조건이며 자동 건너뛰기나 무한 대기로 숨기지 않는다.

### 5.2 전환 사이 추가 입력 방지

기존 순서인 **실습 상태·제한 설치 → 설명 Layer 반환 → 실습 안내**를 유지한다. 제한된 실습의 안내 표시 중에는 `IsTutorialPracticeGoalPending == false`이므로 새 명령·미리보기를 허용하지 않고, 안내 표시 완료 뒤만 표의 입력을 연다.

실습 통과 뒤 다음 설명의 StartCondition 대기에는 기존 Buffer가 이전 제한을 이어 준다. 이때도 `IsTutorialPracticeGoalPending == false`이므로 새 명령·미리보기는 모두 막는다. 결과 재생·적 자동 진행·기존 정리는 막지 않는다. 따라서 공격 실습 성공 직후 추가 공격이나 턴 종료 실습 성공 직후 다음 턴을 다시 종료하는 틈이 없다.

조건식은 `CombatInputPolicy`에 둔다. 버튼 활성화, 키/포인터 Router, 실제 명령/미리보기 제출은 같은 정책을 사용한다. 늦은 공격/이동 응답도 제출 직전에 현재 제한을 다시 확인한다. 이미 수락된 결과를 폐기하지 않는다.

### 5.3 막힌 입력 안내

18: “먼저 아군을 한 칸 이상 이동하세요.” / 22: “적 또는 넥서스를 공격하세요. 이동과 턴 종료로 다시 시도할 수 있습니다.” / 24: “턴 종료 버튼을 눌러 플레이어 턴을 마치세요.”

비활성 버튼은 기존대로 눌리지 않는다. 키 등 실제 금지 시도만 기존 `ReportBlockedInput`을 보낸다. 호버/매 프레임 갱신은 보고하지 않는다. 통과 뒤 설명 대기는 실습 대기가 아니므로 기존 NotReady 정책을 유지한다. 새 알림 UI는 만들지 않는다.

## 6. 시작 연출·강조·UI 수명

### 6.1 설명 시작 조건

`TutorialPlanSources`가 기존 `service.ViewGroup`의 전투 View에서 **`ICombatProgressView`만** 받게 확장한다. `TutorialFactsReader`가 `IsAttached`, `IsEntryPresentationPending`, `IsSubmissionSuspended`, `IsAbandonRequested`를 사실로 옮긴다.

`TutorialPlan.CanStartExplanation`은 기존 PlayerInput/결과 표시 완료/미처리 명령 없음/세션 전투 미종료/입력 독점 없음에 **View 연결됨, 진입 연출 대기 없음, 제출 정지·포기 요청 없음**을 더한다. 설명 시작 Job이 Layer를 빌리기 직전에 동일 조건을 재검사한다. Tick에서 별도 시작 규칙을 만들지 않는다.

Start 요청과 세션 설치는 기존 Attach에서 진행해도 된다. 첫 설명의 시작만 기다리며, 대기 중 Job이 Runner를 점유하거나 진입 연출 Tick을 멈추지 않는다. 장면 전환/오류 시 기존 정리 경로를 탄다.

### 6.2 강조 대상

- 새 `PlayerUnits = 12`: 현재 Character Buffer의 생존 플레이어 필드 유닛들을 기존 FieldUnitView 경계 계산으로 강조한다. 선택을 바꾸거나 메인으로 대체하지 않는다. ‘선택할 수 있는 아군들’의 안내이므로 전체 아군을 비춘다.
- 새 `EndTurnButton = 13`: `CombatActionPanel`에 기존 AttackButtonRect와 같은 읽기 전용 EndTurnButtonRect를 추가해 참조한다. 버튼 참조 원본은 패널 하나다.
- 기존 `CurrentActorUnit = 7`, `ActionCost = 8`은 새 정의에서 사용하지 않는다. 기존 직렬화 번호를 다른 뜻으로 재사용하지 않는다. 삭제된 11도 재사용하지 않는다.
- AttackButton·MainCharacterHealth·EnemyNexus 등은 재사용한다. 보이지 않는 유닛에 다른 대상을 임의로 비추지 않는다.
- 25는 FocusTargets 비움/Anchor None/기존 중앙 배치로 처리한다. 강조 때문에 진행을 추가로 기다리지 않는다.
- 새 설명의 Layout은 기존 설명 배치 코드를 쓴다. 없는 필수 버튼 참조는 UIId/필드명을 포함한 구성 예외, 순간적으로 없는 유닛은 기존처럼 강조 없음으로 구분한다.
- 위 대상의 어두운 덮개 구멍은 유지하고, UI/월드 대상 구분에 따라 §6.5의 점선 또는 모델 외곽선을 함께 표시한다. 별도의 강조 대상 목록을 정의 에셋에 중복 저장하지 않는다.

### 6.3 카드와 튜토리얼 화면의 소유권

카드 표시는 `CombatEntryPresentation`의 배치 표시 소유자가 맡는다. 새 시작 조건 아래에서는 TutorialViewCardVisibility가 비활성 카드를 강제로 열어 연출을 앞당기지 않는다. 8·9의 강조는 이미 열린 실제 카드를 읽는다. 튜토리얼 종료 때 배치 표시 소유자의 카드를 닫지 않는다. 관련 임시 열기/정리 호출은 이 경로에서 제거하거나 검증 전용으로 축소하며 다른 카드 UI를 재구성하지 않는다.

기존 `TutorialViewUI`/`UIId.Tutorial` 한 개, Parallel 정책, 기존 부모·Canvas 구성을 유지한다. 생성·등록은 기존 씬 BindScene 수명에 두고 Attach에서 필수 참조를 검사한다. 현재 TutorialViewUI의 외부 개폐는 이미 `IUISystemService`를 사용하므로 이 경로를 재사용한다. 내부 설명/띠/덮개 위젯의 활성화는 TutorialViewUI가 담당한다. 새 독립 MonoBehaviour 패널·새 UIId·매 프레임 서비스 재등록을 추가하지 않는다.

설명 중 입력 독점/ESC 차단, 실습 중 설정 허용, 중단·씬 해제 시 Layer/선택/표시 반환은 기존 계약을 유지한다. 창을 비활성화했다 돌아와도 같은 설명에서 사용자의 확인을 기다린다. 덮개 알파 0.95와 사용자 `targetMapKey: 1`은 이번 설계에서 바꾸지 않는다. 자동 진행 시간은 §6.4에 따라 제거한다.

### 6.4 설명 자동 진행 제거 — 사용자 확정

- 설명 표시가 끝나면 사용자 `Continue` 요청만 기다린다. 클릭 한 번이 현재 설명 하나만 넘기며, 설명을 넘긴 입력이 아래 전투 조작으로 이어지지 않도록 기존 Layer·revision 검사를 유지한다.
- `ContinueOrTimeout = 3`은 같은 숫자 값의 `Continue`로 이름과 의미를 맞춘다. `CanContinue`와 Snapshot 검증·요청 처리·표시 완료 전이도 이 대기를 사용한다. 실습의 Continue는 계속 NotReady다.
- `TutorialPlan`의 만료 판단/등록 분기와 `TutorialExpireJob`을 제거한다. `TutorialPlanStepKind.Expire`의 옛 숫자는 다른 의미로 재사용하지 않는다. 설명을 다음으로 넘기는 실행 경로는 기존 Continue 요청 처리 한 곳에 둔다.
- `ITutorialViewExecution.StartTimer`·`IsTimerExpired`, `TutorialFacts`의 만료 사실, FactsReader의 타이머 읽기를 제거한다. `TutorialViewSystem`의 자동 진행용 타이머·Tick·시작/취소 호출과 `TutorialViewTimer`의 사용도 제거한다. 시계·창 포커스 연결은 자동 진행에만 필요한 의존성만 정리하고, 다른 입력/화면 수명에 필요한 처리는 보존한다. 미사용 전용 `.cs`를 정리할 때 `.meta`도 함께 다룬다.
- 정의의 `autoAdvanceSeconds` 필드·`AutoAdvanceSeconds` 속성·양수 시간 검증과 에셋의 직렬화 항목을 제거한다. 시간을 아주 크게 설정하거나 0을 특별값으로 쓰는 방식으로 숨기지 않는다.
- 막힌 입력 안내가 잠시 뒤 사라지는 기존 표시 시간과 맵/카드/공격 연출 시간은 유지한다. 이 시간들은 설명 단계를 넘기지 않는다.
- 이번 변경은 설계에 반영한 것이며, 위 코드·에셋 제거는 승인 뒤 구현 Phase에서 수행한다.

### 6.5 UI 점선 테두리와 모델 아웃라인 — 사용자 요청 반영

#### 보이는 방식

| 강조 대상 | 추가 표시 | 적용 예 |
|---|---|---|
| UI | 대상 화면 영역 바깥쪽의 점선 사각 테두리 | 손패 전체, 메인 카드, 전투 진입·공격·턴 종료 버튼, 메인 체력 |
| 필드 유닛 | 유닛 모델 모양을 따라가는 실선 아웃라인 | 메인 캐릭터, PlayerUnits의 각 아군, EnemyUnits가 가리키는 적 |
| 적 넥서스 | 필드 모델과 같은 실선 아웃라인을 적용하는 제안 | 13·20의 EnemyNexus |
| 강조 없음 | 점선·모델 아웃라인 모두 없음 | 25 마지막 설명 |

- 문구·기존 덮개·밝게 드러나는 영역을 유지하면서 추가 표시한다. 유닛에 UI용 점선 사각형을 대신 그리지 않는다. UI와 유닛이 함께 지정되면 두 종류를 동시에 표시한다.
- 설명이 표시되는 동안만 유지한다. 기본안은 정적인 테두리이며 점멸·점선 이동·맥동 연출을 추가하지 않는다. 자동 넘김 제거와 관계없이 같은 설명에 머무르는 동안 강조도 계속 유지한다.
- 색상은 공통 밝은 강조색을 제안한다. 최종 색·UI 선 굵기·점선 길이/간격·모델 선 굵기는 Unity 확인에서 조정할 표현 값이며 사용자 확정 수치가 아니다. TutorialUI 프리팹의 한 스타일 설정에 모으고 단계별로 복제하지 않는다.
- Hand는 기존 전체 카드 영역을 둘러싼 한 테두리, MainCharacterCard는 해당 카드 영역의 한 테두리다. 여러 UI 대상은 각각 표시한다. 실제 UI 자체의 크기·색·선택·상호작용 상태를 바꾸지 않는다.

#### 대상 결정과 UI 구현 경계

- 현재 FocusLocator의 Rect만 반환하는 결과를 **UI 화면 영역과 월드 개체 ID/실제 모델 표시 참조를 구별하는 표시 자료**로 확장한다. 같은 해석 결과에서 덮개 구멍, 점선 영역, 모델 아웃라인 대상을 파생한다. 표시 부품마다 대상 검색·선택 판단을 다시 하지 않는다.
- 동일 개체가 여러 FocusTarget에 걸리면 중복 제거한다. 현재 설명의 대상을 소유하며 전투의 SelectedEntityId나 호버 상태는 쓰지 않는다. 화면 이동/크기 변화는 기존 RefreshLayout에서 갱신한다.
- UI 점선은 TutorialUI 내부의 전용 `UnityEngine.UI.MaskableGraphic` 위젯(가칭 TutorialFocusBorderGraphic)으로 그린다. 기존 Overlay 위·설명 박스 아래에 배치하고 `raycastTarget = false`로 고정한다. 새 Canvas·UIId·독립 UIBase 등록은 만들지 않는다.
- 덮개와 같은 화면 좌표→Canvas 좌표 변환·공통 강조 여백을 사용한다. 해상도/Canvas 배율이 바뀌어도 구멍과 테두리가 어긋나지 않아야 한다. 설명 박스가 피하는 영역에도 테두리 폭을 포함한다.
- 입력을 막는 것은 기존 Blocker와 Tutorial Layer다. 점선에 클릭/호버 이벤트를 추가하지 않고 설명 중 강조된 버튼이 눌리게 만들지 않는다. 점선 메시/버퍼는 재사용하고 대상·좌표·스타일이 바뀔 때만 다시 그린다.

#### 모델 아웃라인 구현 경계

- 기본안은 기존 Cartoon/EntityCartoon의 외곽선 속성을 **UnitModelAppearance의 합성 층**으로 사용하는 것이다. FieldUnitView에 튜토리얼이 필요한 좁은 강조 대여/해제 창구를 두고, 표시 협력 객체(가칭 TutorialModelHighlight)가 현재 대상의 대여만 보관한다. 대상/진행 판단은 이 객체에 넣지 않는다.
- 렌더 속성의 실제 작성자는 기존 UnitModelAppearance 한 곳이다. 그 안의 기존 ColorSystem 손잡이 경로를 확장해 색·폭을 적용하고, 이동 투명도·피격 플래시·디졸브와 합성한다. 튜토리얼이 별도 ColorHandle이나 직접 SetPropertyBlock으로 같은 모델에 경쟁 쓰기를 하지 않는다.
- 기존 재질/슬롯 PropertyBlock에서 유효한 기본 외곽선 값도 보존한다. 튜토리얼 강조 해제는 해당 층만 제거해 현재 다른 효과와 원래 외곽선으로 복원한다. 모델 sharedMaterial 원본·셰이더 에셋을 수정하거나 모든 외형을 EntityCartoon으로 강제 전환하지 않는다.
- MeshRenderer와 SkinnedMeshRenderer의 실제 모델 렌더러를 지원 대상으로 확인한다. HUD·텍스트·VFX를 유닛 실루엣에 섞지 않고, 다중 재질/무기 부품도 실제 외형 계약에 따라 포함한다. 애니메이션 중에도 몸체를 따라야 한다.
- 기존 SelectOutlineSystem의 Scope는 대상의 MeshFilter를 복사하는 구조라 SkinnedMeshRenderer 전체를 지원한다고 가정하지 않는다. 이 서비스나 공용 셰이더를 이번에 일괄 확장하지 않는다. 현재 모델 재질이 외곽선 속성/Pass를 지원하는지 구현 초기 확인 대상으로 둔다. 필수 대상이 미지원이면 조용히 아웃라인을 생략하지 않고 해당 모델/재질을 명시해 표현 방식의 설계 변경을 먼저 결정한다.
- 모델 외곽선이 기존 덮개 구멍에 잘리지 않도록 모델 화면 영역/여백에 외곽선의 확장분을 반영한다. 벽 뒤 관통 표시·카메라 변경은 추가하지 않는다.

#### 대여와 정리 수명

- 새 설명을 표시할 때 새 대상 집합을 맞추고, 이전 단계 전용 점선·모델 대여를 해제한다. 같은 대상은 중복 대여하지 않는다. 설명 전환·실습 진입·완료·취소·승패 중단·오류·Detach에서는 튜토리얼이 소유한 표시만 정리한다.
- 표시 중 대상이 비활성화/파괴되거나 FieldUnitView가 풀 반환·다른 EntityId로 재바인딩되면 해당 대여를 무효화한다. 늦은 정리가 새 개체의 강조를 지우지 않도록 기존 표시 수명/세대 확인 방식에 맞춘다.
- 창 비활성화만으로 다음 단계로 넘기거나 강조를 영구 해제하지 않는다. 같은 설명으로 돌아오면 현재 대상 표시가 유지/복구돼야 한다.
- 실습 띠만 보이는 동안에는 이 설명용 강조를 유지하지 않는 것을 기본안으로 한다. 실습의 기존 선택·호버·공격 범위 표시는 정상 전투 소유자가 계속 관리한다. 튜토리얼 정리에서 일반 선택·호버 상태나 다른 효과를 초기화하지 않는다.

## 7. 변경 경계와 구조 점검

### 예상 수정 범위

| 묶음 | 주요 대상 | 역할 |
|---|---|---|
| 정의·계약 | `B/Tutorial/TutorialPracticeGoal`, `TutorialCombatInputRestriction`, `TutorialStepRule`; `D/.../TutorialFocusTarget`; 정의 에셋 | 목표·제한·강조 이름과 검증, 단계 콘텐츠 |
| 진행·증거 | `L/Tutorial/TutorialPlanSources`, `TutorialFactsReader`, `TutorialFacts`, `Plans/TutorialPlan` | 공격/종료 결과와 진입 사실 읽기, 통과/설명 시작 판단 |
| 수동 설명 진행 | `TutorialWaitReason`, `TutorialSessionSnapshot`, `Plans/TutorialPlanStep`, `Jobs/TutorialStepJobs`, `ITutorialViewExecution`, `TutorialViewSystem`, `TutorialViewTimer`, `TutorialDefinitionDataSO`와 관련 조립 | 확인 대기 명칭 정리, 자동 진행용 시간·만료 전이·전용 의존성 제거 |
| 표시 | `V/Tutorial/TutorialViewFocusLocator`, `TutorialViewUIDisplay`, `TutorialViewCardVisibility`, `TutorialViewUI`, 전용 점선 위젯·모델 강조 협력 객체, `TutorialUI.prefab` | 대상 종류별 강조, 점선 설정/참조, 카드 개폐 소유권. 기존 UI 등록·서비스 개폐 재사용 |
| 모델 강조 접점 | `V/Combat/Display/Unit/FieldUnitView`, `UnitModelAppearance`와 기존 표시 대여 연결 | 아웃라인 층 적용·복원, 재바인딩/풀 반환 수명. 전투 선택을 바꾸지 않음 |
| 전투 접점 | `V/Combat/Input/CombatInputPolicy`, 필요시 Router/행동 버튼·공격·되돌리기 제출 경로, `Display/Hud/CombatActionPanel` | 공통 제한 적용, 늦은 제출 방지, 턴 종료 버튼 영역 공개 |
| 조립 | Tutorial Plan의 `From` 호출과 기존 View 그룹 접근; 필요한 의존성 전달 지점만 | 좁은 읽기 계약 연결 |
| 문서 | 기존 튜토리얼 설계·계획, T-7 전투 입력 인계 | 승인된 변경과 예전 정책 구분. 실행 기록은 구현 Phase에서 갱신 |

예상 범위이며 파일 수를 완료 조건으로 삼지 않는다. `CombatPlan`의 전투 규칙, Core 파일, DB 규칙 데이터, FieldCamera, Architecture, 입력 Action 에셋의 변경은 요구하지 않는다. 현재 확인한 공개 사실로 설계할 수 있다. 부족한 Core 사실이 발견되면 구현으로 우회하지 않고 요청을 분리한다.

### SSOT·SOLID·JMH/FW

| 기준 | 적용 |
|---|---|
| SSOT | 게임 가능 여부는 Core, 결과·행동 상태는 Buffer, 튜토리얼 단계/기준점은 Tutorial Buffer, 통과·시작 판단은 Plan, 표시 완료는 기존 P 순번, 진입 대기는 기존 Progress, 문구·순서는 정의 에셋 |
| S | FactsReader는 공개 사실 관찰, Plan은 다음 한 걸음 판단, Job은 실행, 표시 코드는 위치·문구, Policy는 제한, 조립은 전달만 담당. 점선 그래픽은 메시, 모델 강조 협력 객체는 대여 수명, UnitModelAppearance는 렌더 속성 합성을 담당 |
| O | 두 실제 목표/제한만 기존 enum·검증·판정 확장점에 추가. 범용 조건 언어/새 Runner/사용하지 않을 목표 레지스트리는 만들지 않음 |
| L | Accepted 요청≠실습 완료, 표시 완료 전 통과 없음, Cancelled≠Finished, 기존 Job 완료/Layer 반환 보장을 그대로 유지 |
| I | Character/Combat/Result ReadSink와 ICombatProgressView만 전달. 기능에 전체 BufferQuery·ViewGroup을 보관하지 않음 |
| D | 공개 결과·읽기 계약에 의존. Core Application/가변 Store/씬 객체로 진행 규칙을 판단하지 않음 |
| JMH | System 수명·Plan 판단·Factory/Group 조립 분리. 변경 이유가 다른 표시/판정/입력을 한 거대 조정자에 합치지 않음. 런타임 DB 수정·규칙 사본·전역 이벤트 추가 없음 |
| FW-001~004 | 기존 Buffer/튜토리얼 모듈 등록 유지, 새 서비스 중복 등록/Build 후 등록/내부 Store 우회 없음 |
| FW-005~009 | 기존 입력 receiver→Router→Buffer, Configure/Bind 수명, UIBase 등록/UISystem 개폐 준수. Button.onClick 구독이나 별도 UI 경로 없음 |
| FW-010~012·수명 | 기존 모델 외형 작성/손잡이 경로 유지, 소유한 강조 대여만 해제. 실습/설명 전환의 제한 연속성, 씬 해제·포기·오류 정리, 늦은 콜백 session/revision 및 모델 재바인딩 수명 확인 |

기준은 [전투 View 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [JMH 개발 지침](../framework/Framework%20Development%20Guidelines.md), [Framework 프로그래머 가이드 §6](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)다.

서비스 경로: BufferSystemFactory의 기존 Tutorial Buffer/Sink 등록 → Buffer 그룹의 `IBufferQuery` → TutorialPlanSources. 기존 InGameViewSystemFactory의 TutorialViewSystem 모듈 → ViewGroup.TutorialExecution → Tutorial Plan. 전투 Progress도 기존 ViewGroup의 전투 서비스에서 꺼낸다. Global의 UI는 기존 Global UI Context 경로를 유지하며 루트에 다시 노출하지 않는다.

### 기능 변경의 독립성

- 문구/순서 변경은 정의 에셋과 콘텐츠 확인에 한정한다.
- 실습 성공 의미 변경은 Goal/StepRule·FactsReader·Plan 및 해당 실습 확인에 한정한다. 공격 계산/렌더링을 수정하지 않는다.
- 허용 조작 변경은 제한 계약·Policy와 제출 접점 확인에 한정한다.
- 강조 위치 변경은 FocusLocator·패널의 읽기 계약·UI 확인에 한정한다. UI 점선 모양 변경은 전용 그래픽/스타일, 모델 아웃라인 합성 변경은 모델 외형 층/대여와 관련 표시 확인에 한정한다. 공격/턴 규칙과 실습 판정은 수정하지 않는다.
- 진입 대기 연결만 기존 Progress의 조립 경로를 읽는다. 전투 진입 연출 구현을 복제하거나 다시 검증할 범위를 늘리지 않는다.

## 8. 인수 확인과 다음 단계

새 Test 코드/시험 도우미를 작성하지 않고 MSBuild·EditMode를 실행하지 않는다. 구현 뒤 컴파일·재생은 사용자가 Unity에서 확인한다. 실제 에셋이 바뀌는 Phase에서는 기존 `DataBaseAssetTests`도 사용자 확인 항목에 포함한다. 기존 시험의 최소 호출 수정이 필요하면 작성자·수정 권한을 먼저 확인하며 보호 규칙을 따른다.

| Unity 확인 | 기대 |
|---|---|
| 첫 전투 맵/배치/카드 연출 | 모두 끝나기 전에 설명·카드 강제 열기 없음 |
| 설명을 클릭하지 않고 오래 대기 | 첫 설명·중간 설명·마지막 설명 모두 그대로 유지, 자동 진행·완료 없음 |
| 설명 클릭·연속 클릭 | 기존 다음 입력으로만 진행, 같은 입력이 전투 명령이나 낡은 revision의 중복 진행으로 이어지지 않음 |
| 카드·버튼·체력 UI 강조 | 점선이 밝게 드러난 대상 경계를 따라 표시되고 문구/대상을 가리지 않음. 클릭 입력 추가 차단/누출 없음 |
| 유닛·넥서스 모델 강조 | 모델 실루엣에 아웃라인, UI용 점선 사각형 없음. 애니메이션·무기/다중 재질·SkinnedMeshRenderer 외형 확인 |
| 혼합 대상·화면 크기 변화 | UI 점선과 모델 아웃라인 동시 표시, 덮개 구멍/대상/테두리 정렬 유지, 외곽선 잘림 없음 |
| 다음 설명·실습·중단·재바인딩 | 이전 단계 강조 잔상·대여 누수 없음. 일반 선택·호버·투명도·플래시·디졸브·원래 외곽선 유지/복원 |
| 배치 실습 | 메인 포함 정상 배치/전투 진입. 13은 진입 결과 표시 뒤 |
| 이동 | 선택한 어느 아군의 한 칸 이상 실제 이동/부분 이동 표시 뒤 통과 |
| 공격 조준 방식 | 칸·방향·광역 모두 실제 적/넥서스 Hits로 통과 |
| 0피해/대신 맞음/처치 | 적 실제 피격이면 통과. 파괴로 승패가 먼저 나면 중단 우선 |
| 빈 칸·아군만 피격·취소·거절 | 실습 유지. 공격 버튼 클릭만으로 성공하지 않음 |
| 사거리 밖/행동 사용 | 다른 아군·이동·되돌리기·턴 종료로 재시도 가능 |
| 배치 복귀 | 미완료 이동/공격 실습에서 정상 재배치·전투 진입 가능, 목표는 유지 |
| 회복용 턴 종료 | 앞 실습을 통과시키거나 24를 미리 통과시키지 않음 |
| 24 턴 종료 | 무선택·미사용 유닛이 있어도 종료 가능. 표시 완료 후 통과 |
| 25 마지막 안내 | 적의 자동 진행·미표시 결과를 가로막지 않고 안정된 다음 입력 시점에 표시 |
| 전환 틈/지연 응답 | 실습 안내 표시 전·실습 통과 뒤 추가 명령 없음, 이미 수락된 결과는 재생 |
| 일반 전투·완료 후 | 입력 제한 해제, 기존 턴 종료 확인창·공격·이동·되돌리기 정상 |
| 포기·승패·씬 이탈·설정·창 비활성 | 기존 종료 사유·정리·입력 독점 복구 보장. 창 복귀만으로 설명이 넘어가지 않음 |
| EndBattle 뒤 이력만 남은 상태 | 공격 대상 조회를 하지 않고 기존 BattleEnded 정리, 정상 대상 정리를 오류로 보고하지 않음 |
| 다른 첫 전투 후보/보스 목표 | 콘텐츠 전제 충족 여부 확인. 지원하지 않는 후보를 완료 검증으로 간주하지 않음 |

설계는 승인됐으며 별도 [Task 계획](../plans/2026-11-10-tutorial-current-rules-refresh-plan-Hojin.md)에 사전 확인과 Phase A~E를 정리했다. 이 문서의 승인은 구현 실행 승인이 아니다. 각 구현 Phase 끝에는 보고하고 멈춘다. C# 작성 역할과 전투 View 접점의 실제 작업자는 기존 역할 경계에 맞춰 정한다. 커밋·push는 사용자가 한다.

## 9. 설계 검토 기록

- 2026-11-10 사용자 최종 승인: “일단 이 설계로 확정하고 구현할 Task 계획을 Phase별로 정리”. 자동 진행 제거·UI 점선·유닛/넥서스 아웃라인을 포함한 현재 설계를 확정하고 별도 실행 계획을 작성했다. 이후 기능 추가 가능성은 이번 범위를 늘리는 승인이 아니다. 구현은 미착수다.
- 2026-11-10: 최신 코드와 기존 튜토리얼 계약을 대조해 초안 작성. 사용자의 명중 기준 답변을 §1·§4.3에 반영.
- 2026-11-10 후속 사용자 정정: 설명 자동 진행 제거 확정. §1·§3·§6.4·변경 범위·인수 항목을 수동 확인 전용으로 갱신했다. 코드·에셋은 아직 수정하지 않았다. 아래 독립 검토는 이 정정 전의 공격·턴 종료 설계에 대한 기록이며, 이번 문서 정정은 관련 시간 계약을 대조해 정적으로 확인했다.
- 2026-11-10 후속 표현 요청: UI 점선 테두리·유닛 모델 아웃라인을 §1·§6.5·변경 경계·확인표에 반영했다. 기존 덮개/Canvas, 모델 외형 합성, Cartoon 외곽선 속성, SelectOutline Scope의 MeshFilter 한계를 읽고 재사용 경계를 정했다. 세부 스타일·넥서스 동일 적용·설명 중에만 유지하는 정책은 설계 제안이다. 화면/재질 호환은 Unity 미확인이며 아래 독립 검토의 범위에는 이번 추가가 포함되지 않는다.
- 문서만 변경. 컴파일 미확인, Unity 재생 미확인. 실제 에셋 교체/실습 구현은 아직 하지 않았다.
- 독립 구현 관점 검토 1회 완료. 지적 1건 수용: EndBattle 뒤 대상 기록이 비워져도 결과 이력과 Completion은 남으므로, Plan의 우선순위만으로는 공격 사실 조회 오류를 피하지 못한다. `CharacterBuffer.EndBattle`, `CombatStateBuffer.EndBattle`, `TutorialFactsReader`를 대조해 §4.1·§4.3과 확인표에 종료 전 조회 경계를 명시했다. 추가 사용자 결정이 필요한 지적은 없었다. 이 검토는 사용자 승인·실행 검증과 별개다.
- 문서 링크 7개와 단계 표(17단계/실습 4개)를 정적으로 확인했다. 기존 정의 에셋의 SHA-256은 조사 시작과 동일하다. 제품 코드·에셋은 변경하지 않았다.
