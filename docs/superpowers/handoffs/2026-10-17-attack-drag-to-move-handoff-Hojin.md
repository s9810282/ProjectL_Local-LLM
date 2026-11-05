# 공격 Drag→이동 전환 인계

- 작성자: 호진(이호진)
- 작성자/기능 소유: Hojin(View).
- 현재 확인 기준: `3265452807cba42866ce62d5ebd30289371af382`, `feature/Hojin/Stage_Shop_Event`, 2026-10-17.
- 상태: **P2 자동 재검증·인계 정리 완료.** 사용자 Play는 부분 확인/미확인을 구분하며, 모든 항목 통과를 뜻하지 않는다.
- 원본: [확정 설계](../specs/2026-10-16-attack-drag-to-move-design-Hojin.md), [작업 계획·P0/P1 기록](../plans/2026-10-17-attack-drag-to-move-plan-Hojin.md), [사용자 승인 Exit 보완](../specs/2026-10-16-attack-drag-to-move-design-Hojin.md#2부-p1-공격-정리-재진입-범위-확인).
- 전투 작업자의 기존 문서는 수정하지 않았다. 별도 메시지를 발송한 문서가 아니며 커밋·push는 사용자가 직접 한다.

## 1. 다음 전투 작업자가 먼저 볼 곳

1. 설계 §4(옛 §3)의 클릭/Drag/UI/release 경계와 아래 §2의 소유권을 읽는다. 공격을 활성화한 채 이동을 시작하지 않도록 주의한다.
2. [CombatInteraction.TryHandleAttackDragPointer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatInteraction.cs)에서 전환 순서를 확인한다.
3. [CombatPathInput](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs)의 Origin 조회·재검증·전환 진입을 기존 일반 경로 진입과 함께 본다. Path의 최신 facts 공급자는 미처리 명령까지 포함한다.
4. 아래 §4의 실제 검증/미확인을 확인한 뒤 옛 계획 §2의 동일한 20개 fixture(목록은 git `10914570`)를 사용한다. 원래 UI 준비 실패 6개를 이번 기능 실패로 바꾸거나 테스트에서 제외하지 않는다.

## 2. 동작과 수명·소유권

- Live의 Combat Layer에서 현재 행동 중인 생존 아군 필드 유닛의 앵커를 누르면 후보만 관찰한다. 공격 모드·범위·요청은 유지한다. 같은 칸 움직임·칸 중심 스냅 echo와 짧은 클릭은 기존 공격 입력을 유지한다.
- 누른 칸을 벗어나 **다른 유효한 필드 칸**에 들어가면 이동 release 소유권을 먼저 확정한다. UI/필드 밖만 가리킨 것은 전환 성립이 아니다. 픽셀 임계값이나 새 감도는 없다.
- Interaction은 기존 공격 정리→최신 자격/정체성/제스처 세대 확인→원래 누름 좌표로 Path 시작→세대 재확인→현재 포인터 전달 순서만 조율한다. 경로·범위·견적·승인 context·놓기 확정·제출은 기존 Path/이동 기능/Core가 맡는다.
- 후보/이동 소유 중 취소, E·공격 버튼, Return, 결과·행위자 변경, 포커스/선택 상실, Layer 해제, 정지·씬 이탈은 이전 누름의 release를 소비한다. 새 누름은 다시 받을 수 있다. 소유권 없는 기존 release와 UI 시작 입력에 전역 press 필수 조건을 추가하지 않았다.
- 정리 콜백에서 같은 전투/행위자/원점/결과의 새 입력이 생겨도 세대로 구분한다. 새 Path가 생겼으면 옛 전환의 시작/현재 포인터/release를 전달하지 않는다. 이미 제출한 명령/Gate/표시 확인 번호는 이 기능이 지우지 않는다.
- CancelPresentation은 기존처럼 Path를 제외한 배치·공격 표현 정리다. 경로까지 정리할 때는 CancelInputs를 쓴다. Stop은 Path 참조를 놓고 Detach하며, 조정자가 이미 가진 명령 대기를 대신 지우지 않는다.
- 승인된 Attack Exit 보완은 외부 표시 콜백 전에 옛 요청·상태를 놓고 공격 수명을 검사한다. 콜백의 새 Attack.Enter 요청·표시는 옛 Exit가 지우지 않는다. 정리 오류는 기존 수신 창구/RuntimeFaultBoundary로 전달한다.
- RecordInputOnly와 배치 입력은 기존 분배를 유지한다. 기존 UI 클릭·Drag·EventSystem 수신·키보드/게임패드 Submit과 버튼 바인딩은 변경하지 않았다.

## 3. 변경 파일과 협업 경계

`Combat/`는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`를 뜻한다.

| 소유 파일 | 책임/읽기 시작점 |
|---|---|
| [CombatAttackDragGesture.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatAttackDragGesture.cs) + meta | 새 내부 기능. Origin/후보·칸 이탈·세대·release 소유권만 소유 |
| [CombatInputRouter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputRouter.cs) | RoutePointer의 기존 UI 판정→새 hook→기존 분배. 대상 식과 포인터 처리 후 스냅 순서 유지 |
| [CombatInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatInteraction.cs) | TryHandleAttackDragPointer와 기존 수명 메서드의 제스처 위임. 기능 간 순서만 연결 |
| [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) | TryGetAttackDragOrigin / IsAttackDragOriginCurrent / TryBeginTransferredDrag. 일반 진입과 같은 자격, 최신 facts·planning version 재검증 |
| [CombatSceneInputFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs) | CreateSceneInteraction에서 생성/주입. path/gesture 없는 기존 조립도 유지 |
| [AttackTargetingInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/AttackTargetingInteraction.cs) | 사용자 승인 범위의 Enter/Exit 수명과 정리 콜백 재진입 보존. 공격 조준·비용·Gate/Core 계약 유지 |
| [HojinCombatAttackDragTests.cs](../../../Assets/Tests/EditMode/Framework/HojinCombatAttackDragTests.cs) + meta | 테스트 소유 좌표계·실제 Router/Interaction/Buffer, 클릭·자격·UI·수명·세대 회귀 |
| [HojinCombatAttackDragIntegrationTests.cs](../../../Assets/Tests/EditMode/Framework/HojinCombatAttackDragIntegrationTests.cs) + meta | 실제 Core 견적/context와 Boot→Combat 운영 DB/Live·공개 Framework Submit·수락/표시 확인 |

Framework·Core/Buffer·Job/Plan/Flow·FieldCamera·Architecture·UI 제품·입력 액션/Layer·씬/프리팹·기존 전투 작업자 문서/테스트 단언은 이 기능의 수정 범위 밖이다. CombatMovementInteraction·MovementDraft·MovementPointerSampler도 수정하지 않았다. 최신 사용자 커밋의 StageLegend/Boot/StageSelect 변경은 별도 작업이며 HojinCombat 인계 범위에 포함하지 않는다.

SSOT는 공격 상태=Attack, 경로/허용=Path/Policy, 규칙/견적=Core, Layer/Submit=Framework, 새 객체=누름 소유권이다. Factory가 조립하고 Interaction이 순서만 연결한다. 새 서비스·액션·UI 리스너·Core 기록 경로를 만들지 않았다. Framework 계약은 [FW 개발 지침](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)의 FW-004~007을 따른다.

## 4. 검증과 기존 실패·미확인

| 단계 | 실제 실행 결과 |
|---|---|
| P0 | 관련 18fixture 381건: 375통과·기존6실패 |
| P1 | 관련20fixture 439건: 433통과·같은6실패. 새52단위+6통합=58/58통과. 기존381건 이름·결과·실패 메시지 변화0 |
| P2 | **439건/433통과/같은6실패**, 새58/58통과. P0기존381·P1전체439 이름/결과/실패 메시지 변화0. 컴파일 오류/경고0/0 |

P1의 실제 Boot 검사는 운영 DB/Live에서 현재 플레이어 입력·Combat Layer·표시 완료를 기다리고, 공개 Framework Submit으로 E·클릭·Drag를 보냈다. 실제 Move1/공격 중복0·Core 수락·최종 위치·결과 표시 확인까지 검증했다. 준비용 배치/전투 진입만 기존 공개 View 입력 Buffer를 썼으며 Core 상태·표시 확인 값을 덮어쓰지 않았다.

단위 52건은 실제 Router/Attack/Path와 테스트 소유 Buffer·Probe 표시를 사용한다. 자격/결과 변경을 주입하는 일부 테스트 준비는 정식 Core transition을 쓴다. 운영 Boot에서 Core 상태를 덮어쓰지 않은 사실과 이 단위 준비를 구분한다. Boot의 짧은 클릭은 기존과 같은 공격 0~1회를 허용하며 무조건 공격1회로 검증한 것이 아니다. 실제 OS 커서 Warp·시각적 범위 품질·실제 창 포커스/씬 이탈을 자동 Boot1건으로 대신 확인하지 않았다.

기존 6개는 모두 CombatDisplayReuseTests의 준비 단계 실패이며 원본 이름/원인은 [계획 §3.1](../plans/2026-10-17-attack-drag-to-move-plan-Hojin.md#42-구현-전부터-있는-실패-6건)(옛 §4.2)에 있다. 행동 패널 4개는 returnText reflection 대상 부재, 손패 2개는 일반 Transform→RectTransform 캐스팅이다. 해당 6개의 실제 행동 단언은 확인되지 않았다. 기대값/준비/제품을 고치거나 필터에서 제외하지 않는다.

사용자 Play 응답은 이동/칸 이탈 뒤 범위 상태 유지, 이동 중 되돌아가기 관련 현재 상태, Drag중 공격키 전환·Space 턴 종료를 보고하고 현재 동작을 선호하며 문제없다고 확인한 것이다. 이 동작은 그대로 유지했다. ② Drag/범위와 ③ 공격키 전환은 **부분 확인**이며, ① 클릭/같은 칸/스냅·④ UI 버튼/Submit/UI 시작 Drag·⑤ 포커스/씬 이탈은 **미확인**이다. 취소 후 옛 release와 누름→놓기 전 과정도 전체 통과로 확대하지 않는다. 자동 통과와 사용자 보고를 구분하고, 전체 EditMode suite·자연 속도·성능 완료로 확대하지 않는다.

[P2 실행·사용자 확인 상세](../plans/2026-10-17-attack-drag-to-move-plan-Hojin.md#p2-results), [P0/P1/P2 비교 원본](../../../.superpowers/sdd/HojinCombat-2026-10-17-attack-drag-to-move/P2/phase-comparison.json), [P2 실제 XML](../../../.superpowers/sdd/HojinCombat-2026-10-17-attack-drag-to-move/P2/native-regression/results.xml). Unity exit2는 위 기존6실패가 남은 결과이며 전체 green이 아니다. (2026-10-27 확인: `.superpowers/sdd/HojinCombat-2026-10-17-attack-drag-to-move` 폴더가 이 PC에 없어 두 증거 링크는 열리지 않는다. 수치는 위 표와 계획의 결과 절이 근거다.)

## 5. 남은 확인과 다음 작업

- P2 자동 재실행·P0/P1 비교·문서/파일 보존·링크·독립 검토를 완료했다. 제품 추가 변경 없이 기존 설계·계획2개와 이 인계1개만 정리했다. 위 수동 미확인과 기존6UI 준비 실패의 행동 검증 공백을 남긴다.
- 이번 기능 때문에 새 Core API나 Framework/UI/Camera 변경을 요청할 사항은 현재 없다. 후속 기능에서 범위를 넓혀야 하면 별도 설계·계획으로 분리한다.
- 다음 작업자는 최신 공유 파일/HEAD/작업 트리를 먼저 확인한다. HojinCombat 소유 문서로 기록하고, 기존 전투 작업자의 문서나 같은 소스를 동시 편집하지 않는다.

P2 결과를 보고하고 멈춘다. 후속 수정·커밋·push·외부 전달을 자동으로 진행하지 않는다.
