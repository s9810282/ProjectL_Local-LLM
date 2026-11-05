# 화면 입력 조립·소유권 이전 — Framework 작업자 공유 메모

- 작성자: 호진(이호진)
- 원 계획: 2026-10-17. 현재 갱신: **2026-10-18(Asia/Seoul)**.
- 현재 정합 기준: HEAD d76360e6과9d3cc811의 병합. 과거 Phase3 실행 기준은 feature/Hojin/StageSelcet, HEAD8ef109777fe47edda3252dfd8a48db2895ed31bf다.
- 과거 사용자 승인 범위: StageSelcet의 입력 계획 Phase3 통합 검증·현재 가이드 갱신. 그때의 전투 제품·Framework 계약·다른 브랜치 미병합 보존 기록과 이번 병합 정합을 구분한다. 현재 API는 Live 전용이며 삭제된 실행 모드를 복원하지 않는다.
- 이 메모는 전달 준비 문서다. 외부 작업자에게 메시지를 보내거나 합의를 받은 것은 아니다. 이번 병합 충돌4개와 현행 API 정합을 위한 이 메모1개만 Git 해결 표시를 위해 스테이징하며 커밋·push·merge --continue는 하지 않는다.

설계 원본은 [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)의 화면 입력 조립 절, 실행 결과와 실제 XML·실패·제약은 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md)의 입력 조립 결과(옛 입력 계획 §8~11)를 따른다. 현재 충돌 해소·집중 검사 결과와 독립 검토·보존은 같은 계획(옛 §12)에 구분한다. 아래 "계획 §…" 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`). 과거 검증 건수를 현재 병합의 새 실행 결과로 재사용하거나 합산하지 않는다.

## 1. 문제와 현재 책임

기존 Runtime Factory는 화면 슬롯·Controller·Layer·키 설정 대상과 옵션 Router의 구체 생성/등록을 함께 알았다. 현재는 기능 정의·조립·소유·실행·그룹 연결을 다음처럼 구분한다.

| 책임 | 현재 주체 |
|---|---|
| 액션/Layer 이름·허용 맵·키 설정 대상 | 각 View 입력 폴더의 Names/Layers/KeyBindings. 물리 키 기본값·GUID는 기존 InputActionAsset |
| 슬롯·Controller 생성과 같은 슬롯 주입 | StageInputFactory·StageSelectionInputFactory·ArtifactInputFactory·LobbyInputFactory |
| 화면 입력 합성 | InGameViewSystemFactory가 위 결과를 합성하고 InGame 조립 결과로 전달 |
| runtime별 슬롯4개 소유·접근 | InGameViewSystemGroup.Input의 Stage·StageSelection·Artifact·Lobby |
| 등록 목록 전달 | InputRegistrationSet. 목록만 방어 복사하며 같은 Controller 참조를 전달 |
| 공용 옵션 Router 생성·등록·수명 | GlobalSystemFactory가1회 생성하고 Global 마지막 모듈로 Export |
| Configuration 생성·검증·Configure·장치 연결 | UnityInputBridge.InitData에서1회, 이후 기존 InputSystem/LayerRuntime 실행 |
| 최상위 연결 | Runtime Factory가 InGame 등록 결과를 Global에 전달하고 그룹을 순서대로 Export |

Input context·화면 Factory·조립 DTO는 새로운 Framework 서비스나 모듈이 아니다. 슬롯을 얻기 위해 Factory를 다시 호출하거나 정적 공유·루트 fallback을 추가하지 않는다. Controller7·Layer8·키 설정 대상6, 기존 액션/맵/키/GUID/ConflictGroup은 유지한다. 공통 맵 모달 정의는 StageMapPopupInputLayers이며 실제 LayerName `ShopPopup`도 유지한다.

## 2. 현재 조회와 조립 API

```csharp
var global = resolver.GetService(SystemGroupsService.Global);
var input = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input;

var stageSlot = input.Stage;                 // 배치·전투
var selectionSlot = input.StageSelection;   // StageSelect
var artifactSlot = input.Artifact;
var lobbySlot = input.Lobby;
var optionRequests = global.OptionCloseRequests;
```

화면별 슬롯과 OptionClose 서비스는 생산 Runtime 루트에 직접 등록하지 않는다. 작은 Builder에 Slot만 Export하는 독립 단위 검사는 유지하며, root 비공개를 확인하는 실패 기대 조회도 유지한다. 키가 존재한다는 사실과 실제 등록 위치를 구분한다.

GlobalSystemFactory는 internal 클래스이며 public 정적 진입점 CreateWithInputRegistration(InputRegistrationSet inputRegistration, RuntimeFaultBoundary faultBoundary = null)을 제공한다. 기존 목록 인수 Create도 같은 경로를 한 번 호출하는 호환 wrapper다. IGlobalSystemGroupService.OptionCloseRequests는 기존 IOptionCloseRequestService를 공개한다. 현재 Runtime/InGame/View Factory는 Live 전용이며 Runtime은 battleSource·faultBoundary의2인수 구조를 사용한다. 삭제된 CombatExecutionMode·RecordInputOnly와 실행모드 인수를 복원하지 않는다.

View의 CreateComposition(RuntimeFaultBoundary faultBoundary = null), InGame의 CreateComposition(IBattleInitializationSource battleSource = null, RuntimeFaultBoundary faultBoundary = null)은 기존 오류 경계를 그대로 전달한다. 실행모드 인수와 두 모드를 유지했다는 기존 설명은 StageSelcet의 e244a929·8ef1097 당시 보존 범위다. Live 전용 호환 보완의 별도 실행 이력은 계획 §11에 남기며 현재 병합의 새 검증과 구분한다.

## 3. 생성·초기화·종료

InGame/View 입력을 먼저 생성해 등록 정보를 얻은 뒤 Global을 조립한다. 생성자와 DTO 합성은 서비스 실행·씬 검색·Configure를 하지 않는다. 루트 Export·InitData 전달은 **Buffer → Global → InGame → LifeCycle**, Build는 Builder마다 한 번이다. 화면 전환은 Bind/Unbind·Layer 전환·override이며 재Configure하지 않는다.

Global Export 순서:

```text
Render → Utility → Audio → Input → Loding → UISystem
  → GlobalUIPresentation(서비스 전용) → UITextPresentation(서비스 전용)
  → Job → OptionInputRouter
```

실제 종료 대상 순서:

```text
Root: LifeCycle → InGame → Global
Global: OptionInputRouter → Job → UI → Loding → Input → Audio → Utility → Render
Input: Bridge.Suspend(구독·Router 유지, 마지막 Canceled 전달)
  → 구독·LayerRuntime·복제 에셋 해제 → InputSystem.Shutdown
```

Option Router가 먼저 OptionUI를 닫아 편집/capture·입력 override·오디오/timeScale을 복원한 뒤 사용 서비스를 종료한다. 과거 root 옵션 우선 종료와 현재 Global 내부 옵션 우선 종료는 전체 위치가 다르다. 전체 Runtime 종료에서 이미 Unbind한 화면에 Canceled를 다시 강제하지 않는다.

## 4. Framework·다른 작업자 경계

- Framework Builder·Reader·Store·공용 생명주기 및 FW-001~016의 의미/번호는 변경하지 않는다. 문서의 입력 조립·조회·종료 설명만 현재 코드에 맞춘다.
- 전투 제품 코드·Core/Buffer·서비스 키·Global 카메라·FieldCamera·Architecture·씬/프리팹/DB/입력 에셋을 수정하지 않는다.
- UI Button은 UIInputBinding/IUIInputReceiver 직접 전달을 유지한다. 모든 uGUI 입력을 InputSystem.Submit으로 우회시키거나 Button.onClick 구독을 새로 만들지 않는다.
- 선택 취소·오래된 수신자·popup host 복귀·종료의 소유권은 기존 Slot/Scope/Session에 유지한다. 분리를 이유로 상태·요청·정리 소유권을 복제하지 않는다.

## 5. 8ef1097의 Phase3 검증 이력과 현재 병합의 구분

아래 결과는 2026-10-18 StageSelcet의 HEAD8ef1097에서 수행한 Phase3 실행 이력이다. 실제 Boot/DB smoke를 실행하고 Explicit 자연 상점 메서드를 직접 선택해 XML leaf를 확인했다. 선택 취소·팝업 재연결·옵션/키 설정·종료의 원본 XML/exit와 제한은 계획 §10에 있다. synthetic 입력/Submit/EventSystem 조작과 사람의 물리 입력·미관·기기 확인을 구분했다. 이번 Live 전용 병합의 집중161건 중159통과·TextNotice2실패, 컴파일 오류0과 검토·보존 결과는 계획 §12에 별도로 기록한다.

최초 통합29건은22통과·7실패였다. Recovery5건은 같은 수락 결과 뒤 두 프레임만 기다리는 helper에서 명령이 남아 실패했다. 당시 제품은 착지 완료 후 Observer가 명령을 소비하므로 기존 단언을 유지하고 helper의 대기 조건만 그 완료 경계에 맞췄다. 이후 입력 회귀209/209(보완한 Recovery5건 포함), 직접 선택한 자연 상점1/1이 통과했다. 최초 실행과 중복된5건을 단순 합산하지 않는다. 고유 자동 검사234건의 최신 관찰은232통과·아래2실패이며 원본 XML/exit와 미실시 범위는 계획 §10을 따른다.

다음 두 실패는8ef1097의 실행에서 관찰했고 당시 입력 조립/문서 작업에서 제품·에셋을 바꿔 해결하지 않았다. 현재 병합 코드에서도 같은 결과라고 재판정한 것은 아니다.

1. `LobbyBootSmokeTests.Boot_AllStartModesInitializeCatalog_CombatFixture`: 개발 fixture UnitCatalog에 운영 로드아웃의 `Unit_CactusCannon`이 없어 전투 준비가 실패했다. 운영 DB에 해당 유닛 에셋은 존재한다. 개발 정의/로드아웃 계약 확인이 필요하며 Core 계산을 수정한 것은 아니다.
2. `LobbyBootSmokeTests.BootToLobby_ShowsSpaceWithoutStartingRun`: 1920×1080에서 7번 캐릭터 표시와 3D FacilityLabel의 겹침 단언이 실패했다. 입력 동작 단언 실패와 구분하며 레이아웃·카메라·에셋을 수정하지 않는다.

실제 모든 화면의 옵션 조합·물리 입력·수동 미관과 전체 EditMode/플랫폼 빌드·다른 브랜치 통합은 그 Phase3 자동 검사로 보증하지 않았다. 현재 병합 코드의 집중 검사도 전체 통합 인수 완료를 뜻하지 않는다. Recovery 재연결 fixture의 명시적 Runtime.BindScene 호출은 테스트 조립이므로 운영 중복 Bind 없음의 증거로 사용하지 않는다. 같은 씬 재연결에서 Runtime을 다시 Bind하지 않는 NodeMovement smoke와 팝업 구독1회 검사를 별도로 대조한다.

## 6. 현재 가이드

- [스테이지 입력](../viewDocs/stage-input-guide.md)
- [스테이지 선택 입력](../viewDocs/stage-map-guide-Hojin.md)(옛 스테이지 선택 입력 안내를 합친 문서)
- [아티팩트 입력](../viewDocs/artifact-guide-Hojin.md)
- [Framework 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)
- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)
- [OptionUI 기록과 현재 소유권](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/OptionUI.md)

날짜가 붙은 과거 완료 기록·검증 수치를 덮어쓰지 않는다. 현재 설명에서 원본 설계와 분기별 실행 기록·현재 병합의 검증 결과로 연결하며, 문서 작성과 실제 외부 전달을 구분한다.
