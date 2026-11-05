# 모래바람 여정 UI — 설계·Task·구현 기록

## 범위와 기준

- 사용자 승인: 이 UI 폴더 안에서만 실사용 가능한 연출을 구현한다. 기존 씬·로딩·Factory·Core·Framework·다른 셰이더는 수정하거나 연결하지 않는다.
- 기준 외형: 이번 대화의 최종 측면 이미지. 흰 머리 카우보이, 갈색 말, 마차와 부드러운 연속 움직임, 좌→우 모래바람 및 난기류.
- 생성한 이미지는 표현 리소스이며 원본 3D 모델을 자동 복원한 것이 아니다. 두 원본 PNG를 이 폴더 안에 포함하고, 실행 시 생성 이미지 서비스나 외부 파일 경로에 의존하지 않는다.
- 등록 단위: 향후 UISystem에 등록된 UIBase 화면이 이 프리팹을 내부 위젯으로 소유한다. 이 패키지는 UIId, 화면 이력, 별도 Canvas·EventSystem·유지 서비스·씬 로딩 경로를 만들지 않는다. 사용자 요청의 '연결 제외'에 따라 UIBase 소유자 추가/등록은 범위 밖이다.

## 설계

1. `SandstormPlayback`만 전환 단계와 표시 시간을 소유한다. Hidden → Covering → Covered → Revealing → Hidden. Covered는 시간 제한 없이 유지한다.
2. `SandstormTransitionVisual`은 직렬화된 하위 표시 객체에 시간과 단계만 전달한다. `Cover`, `Reveal`, `ShowCovered`, `ResetVisual`, `Advance`를 제공한다. 기본 실행 시간은 unscaledDeltaTime이며, 외부 Tick 사용도 명시적으로 선택할 수 있다.
3. 모래바람은 UGUI 호환 셰이더다. 배경을 샘플링하거나 왜곡하지 않는다. 흐르는 노이즈·난기류·모래 알갱이와 이동 마스크로 덮기/걷기를 처리한다. Hold에서는 알파 1로 화면을 가린다.
4. 말/마차/바닥은 각각 독립 MaskableGraphic이다. 동일한 MotionSettings와 표시 시간을 읽어 연속 메시 변형, 발 디딤, 지연 흔들림, 바퀴·바닥 속도를 계산한다. 프레임 교체/전체 이미지 교차 페이드로 움직임을 만들지 않는다.
5. UI 텍스처는 한 아틀라스로 빌드한다. 좌표와 말 분할 경계는 JourneySpriteLayout 한 곳이 원본이다. 머티리얼은 인스턴스별로 소유하고 해제한다. 공유 에셋에 런타임 값을 쓰지 않는다.
6. 조립은 Editor Builder와 프리팹이 담당한다. 실행 기능에서 Resources.Load, AssetDatabase, Find, 서비스 검색, 로딩 호출을 하지 않는다.
7. 무효 시간·잘못된 전환 순서·필수 참조 누락은 상태 변경 전 예외로 보고한다. 중복 Cover(Covering/Covered), 중복 Reveal(Revealing/Hidden), 반복 Reset은 멱등이다. 반대 방향 전환 중 호출은 거부하며 Reset 후 다시 시작한다.
8. Disable은 전환을 초기화하고 표시/입력 차단을 해제한다. Destroy에서 전용 머티리얼을 해제한다. 재사용 시 숨김 상태에서 시작한다.

## 원칙 점검

- SSOT: 단계/시간=Playback, 걷기 설정=MotionSettings, 원본 좌표=SpriteLayout. 전환 완료는 상태로 조회하고 별도 완료 플래그/타이머를 복제하지 않는다.
- SRP: 전환, 바람, 말, 마차, 바닥, 에디터 조립을 분리한다. OCP: 기존 시스템 수정 없이 추가한다. LSP: MaskableGraphic의 마스크·색·레이캐스트 계약을 유지한다. ISP/DIP: 전투/DB/서비스 인터페이스를 받지 않고 필요한 표현 참조만 주입한다.
- FW-001/002/008/009: 이 패키지는 Framework 서비스나 독립 화면이 아니다. 기존 화면의 등록·이력·Tick을 우회하는 통합 코드를 넣지 않는다. 실제 UIBase 소유자 조립은 이후 연결 작업의 책임이다.
- §4.6: 바람 변경은 셰이더/바람 표시만, 말 발걸음 변경은 HorseGraphic/MotionMath만, 배치 변경은 프리팹 Builder만 확인한다. 공통 시간·전환 API가 바뀌는 경우에만 관련 표시와 상태 검증을 함께 수행한다.

## Task 계획

### Phase 1 — 실사용 시각 패키지 (이번 승인 범위)

- [x] P1.1 원본 이미지·메타, Runtime/Editor 어셈블리와 좌표/설정 원본 추가.
- [x] P1.2 전환 상태·수명·공개 API 구현.
- [x] P1.3 마스크/CanvasGroup 대응 모래바람·아틀라스 셰이더 구현.
- [x] P1.4 말·마차·바닥의 연속 메시 움직임 구현.
- [x] P1.5 머티리얼·아틀라스·조립된 프리팹 생성. 한국어 표시는 기존 프로젝트 TMP 폰트를 참조하되 해당 에셋은 수정하지 않는다.
- [x] P1.6 상태/수명/마스킹/셰이더 검증 및 Unity 렌더 확인, 사용법 기록.
- Phase 1이 끝나면 구현·검증 결과와 한계를 보고하고 멈춘다.

### 범위 밖

기존 로딩 화면/씬/UIBase/Factory/ServiceKey 연결, 게임 로딩 API 호출, 외부 작업자 파일 수정, 커밋·push.

## 검증 기록

2026-11-01 Phase 1 완료.

- Unity 6000.0.73f1 에디터에서 Runtime/Editor/Tests 어셈블리 컴파일 완료. 최종 콘솔 오류·경고 0.
- `Verification/EditMode.xml`: 동기 NUnit 19개 통과, 실패 0. 미저장 JMHTest 씬을 교체하지 않기 위해 Unity Test Runner의 씬 저장 단계는 취소하고, 테스트 메뉴에서 NUnit WorkItem을 메인 스레드로 직접 실행했다. 공식 Test Runner 창/PlayMode 테스트 실행 결과와 구분한다.
- 상태 전환·대기 유지·멱등 호출·잘못된 시간·빠른 로딩의 알파·재사용, 발 디딤 경계의 위치/속도 연속성, 완성 프리팹 참조, 독립 머티리얼, 비활성화 입력 해제, 파괴 자원 해제, RectMask2D 아래 시간 갱신 확인.
- URP SingleCameraRequest로 실제 Canvas/프리팹 렌더: 1920×1080, 1080×1920, 숨김 1280×720, 덮기/걷기 중간, 걷기 주기 4개 시점. 완전 가림과 좌→우 마스크 방향은 픽셀 검사도 통과했다. `Verification/*.png`, `asset-validation.txt`에 기록했다.
- 처음의 PreviewScene 캡처는 Native Canvas가 그려지지 않아 실패했다. 검증 전용 추가 씬에서 렌더하도록 수정하고 임시 씬만 닫았다. 사용자 씬은 저장·폐기·교체하지 않았다.
- 편집 모드의 검증에서 생성한 전용 머티리얼도 해제되도록 ExecuteAlways 수명을 적용했다. 자동 Tick은 재생 중인 객체에서만 동작한다.
- 런타임·셰이더·아틀라스·머티리얼·설정·프리팹·원본·테스트와 .meta를 모두 이 UI 폴더 안에 추가했다. 기존 UI.meta는 유지했다. 코드에서 SaveAssets 전체 저장 대신 이 패키지의 SaveAssetIfDirty만 사용한다.
- 에디터 리프레시 중 별도 `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset` 자동 갱신이 작업 트리에 보였다(6줄 변경). 이 파일은 직접 편집하거나 되돌리지 않았다.
- 남은 검증 한계: 플레이어 빌드/실기기 성능, 실제 소유 UIBase와 로딩 연결, Mask의 시각적 조합 및 RectMask2D softness는 이번 실행 범위 밖이다. UI 연결은 사용자 요청에 따라 하지 않았다.
- 커밋·push 없음. 이번 Phase 종료 후 외부 연결 작업을 이어서 수행하지 않는다.

## Phase 3 — 모든 시스템 씬 로딩 연결 (2026-11-01 사용자 추가 승인)

사용자 요청: 로딩 시스템에서 로딩할 때마다 이 연출을 재사용한다. 기존 UI 폴더 한정/연결 제외는 이번 시스템 연결 요청 범위에서 해제된다.

설계: `LodingSystem`이 로딩 수명(덮기 대기 → 기존 Before/Exit/Load/Enter/After 노드 → 걷어내기 대기)을 소유한다. `ILodingPresentation`은 표현 준비/완료 상태와 시작/종료만 제공하고 실제 씬 로드는 하지 않는다. `ILodingSystemService`로 한 표현 소유자만 연결/해제한다. 표현이 없는 기존 저수준 구성은 기존 즉시 로드 경로를 유지한다. 운영 구성은 GlobalUICanvas에 SandstormLoadingUI를 등록해 항상 연결한다. IsLoading은 전환 연출까지 포함하고 재요청을 차단한다. 로드/진입 노드 예외는 UI를 닫고 대기 상태를 해제한다.

SandstormLoadingUI는 요청형 GlobalUIBase(UIId.Loading, Parent=None, Parallel, OpenWithGlobalUI=false)로 변경한다. 기존 UISystem 등록·초기 바인딩에서 Global.Loading 서비스에 표현을 연결한다. 개폐는 IUISystemService만 사용하고 위젯은 unscaled 시간원을 유지한다. 일반 씬 UI 재바인딩에서도 인스턴스와 열린 상태를 유지한다. 로비 표시 정책은 로딩 패널을 닫지 않는다. 종료는 기존 Global(UI→Loading) 수명 순서를 지키고 Canvas 파괴/비활성화가 위젯 자원을 정리한다. 로딩 서비스 Shutdown은 이미 종료된 UISystem을 재호출하지 않는다.

Task: P3.1 표현 계약·로딩 수명 연결, P3.2 Global UI와 로비 표시 보존, P3.3 GlobalUICanvas 프리팹에 기존 위젯 조립, P3.4 반복 로딩·빠른 로딩·예외/재진입·UI 재바인딩과 기존 시각 테스트 검증, 완료 후 Phase 보고. Core/FieldCamera/Architecture와 Framework 계약은 변경하지 않는다.

SSOT/SOLID/FW 검토: 씬 진행은 LodingSystem, 모래 상태/시간은 기존 Playback, UI 등록은 UISystem, 에셋 조립은 Editor만 담당한다. 로딩 서비스는 모래 구현/어셈블리를 참조하지 않는다. 기존 LodingFactory의 단일 서비스·모듈 등록을 재사용하며 Root 재노출/추가 전역 서비스/중복 Tick을 만들지 않는다(FW-001/002/008/009/016). 표현 변경은 Integration/Runtime, 타이밍 계약 변경은 Loading, 배치는 Editor/프리팹에서 독립적으로 수정한다. 공통 경계의 회귀는 IsLoading을 사용하는 기존 LifeCycle 진입 제한과 UISystem 유지 UI 바인딩이다.

### Phase 3 완료·검증

- [x] P3.1 `ILodingPresentation`과 로딩 단계 연결. 기존 Playback의 여정 페이드인 완료 상태를 읽고 완전 가림 한 프레임 후 씬을 전환한다. 별도 최소 대기 타이머는 추가하지 않았다.
- [x] P3.2 요청형 Global UI 연결. 소비자는 루트에서 Global Context를 얻고 `LodingServiceCommand.LodingService`로 조회한다. Factory 내부 등록·Group Export·루트 조립은 기존 경로를 유지한다. 로비/타이틀 표시 정책이 진행 중인 로딩 UI를 닫지 않도록 했다.
- [x] P3.3 Boot가 이미 참조하는 `GlobalUICanvas.prefab`에 비활성 로딩 화면과 기존 여정 위젯을 조립했다. 화면 개폐는 UISystem, 중첩 Canvas 정렬은 화면 Open에서 확정한다. 씬별 수동 배치는 필요 없다.
- [x] P3.4 Unity 컴파일, 동기 NUnit 27개 통과·실패 0. 기존 시각 테스트 19개에 로딩 수명 7개와 실제 UI 통합 1개를 추가했다. 결과는 `Verification/EditMode.xml`, `EditMode-summary.txt`다. 공식 Test Runner 창 실행 및 전체 프로젝트 테스트와는 구분한다.
- 실제 Play 모드에서 기존 LodingFactory·UISystem·등록 UI를 사용해 다른 씬 왕복 2회와 같은 씬 요청 1회를 확인했다. 이 중 한 번은 `timeScale = 0`이다. 비동기 씬 로드, 가림 후 전환, UI 재바인딩 후 동일 인스턴스 유지, 걷어내기 종료 및 입력 차단 해제를 검사했다. 같은 씬 요청은 기존 정책대로 Exit/Enter 노드를 재실행하며 씬 파일을 다시 로드하지 않는다.
- `Verification/Loading-PlayMode.txt`와 `Loading-cycle-*-covered.png`에 결과를 남겼다. 공용 Canvas와 같은 Overlay 부모 아래에서 실제 캡처를 확인하고 화면 모서리의 완전 가림도 검사했다. 검증 중 임시로 추가한 Build Settings와 Play 시작 설정은 복원했다. 열려 있던 JMHTest의 사용자 변경은 저장하거나 폐기하지 않았다.
- 노드 실패 시 UI 닫기·대기 해제·재시도, 중복 요청 거부, 빠른 완료, 유지 UI의 뒤로가기 제외, Shutdown 시 종료된 UI 미호출을 검증했다. Core·FieldCamera·Architecture 및 Framework 서비스 등록 계약은 수정하지 않았다.
- 검증 한계: 별도 검증 씬의 실제 로딩 체인을 확인한 결과이며 운영 Boot부터 전투까지의 전체 게임 진행 재생, 플레이어 빌드, 실기기 성능 확인은 포함하지 않는다. `SceneManager.LoadScene*` 직접 호출은 이 경로를 거치지 않는다.
- Phase 3 완료. 커밋·push 없음.

### 완료 페이드 수정 (2026-11-01 추가 요청)

로딩 완료 시 바로 사라지는 인상을 줄이기 위해, 마차에만 적용하던 0.18초 페이드와 모래의 별도 걷기 마스크를 전체 페이드로 바꿨다. 기존 `revealDuration`(기본 1.05초)과 Playback의 진행률을 그대로 사용한다. Playback의 `VisibilityOpacity`를 부모 CanvasGroup에 한 번 적용해 모래·마차·말·문구가 함께 서서히 사라진다. 여정 하위 농도는 Reveal 시작 시 값을 유지하므로 이중 페이드나 갑작스러운 밝아짐이 없다. 입력 차단과 `IsLoading`은 기존대로 Hidden이 될 때까지 유지한다. 서비스·Factory·씬 연결은 변경하지 않았다.

- [x] Playback/위젯/셰이더 완료 표현 수정.
- [x] 시작·20%·50%·80%·종료 농도 및 0.18초 이후 마차 유지 회귀 검사. 실제 UI 통합에서 절반 시점 활성/입력 차단/로딩 상태 유지 검사. 동기 NUnit 28개 통과·실패 0.
- [x] Unity 실제 렌더에서 완료 페이드 25%/50%/75% 시점 확인. 전체 농도 0.84375/0.5/0.15625, 셰이더 오류 0. 결과는 `Verification/Reveal-Early.png`, `Reveal-Midpoint.png`, `Reveal-Late.png`와 각 상태 파일에 있다.
- [x] 수정 후 별도 Play 검증 씬에서 씬 왕복·동일 씬 요청 3회 재확인(`timeScale = 0` 포함). 모두 완료 후 닫혔으며 Build Settings/Play 시작 설정을 복원했다. 사용자 JMHTest 미저장 변경은 유지했다.

## Phase 2 — 현재 JMHTest 씬 적용 (2026-11-01 사용자 추가 승인)

사용자 요청: "이제 이 UI를 현재씬에 적용해봐". 기존 UI 폴더 제한은 구현 코드에 유지하고, 열려 있는 JMHTest 씬 배치/저장을 이번 범위에 추가한다.

설계: 현재 LodingCanvas를 재사용하고, 기존 LodingImage는 삭제하지 않고 비활성화한다. SandstormJourney 프리팹을 자식으로 연결한다. 별도 Integration 어셈블리의 SandstormLoadingUI(UIBase, UIId.Loading, Parent=None, Policy=Parallel)가 위젯 표시 수명을 소유한다. 현재 활성 Canvas는 Play 시 여정 표시로 시작하며, UISystem이 구성된 실행에서는 기존 씬 검색·등록·OpenUI/CloseUI 경로를 사용한다. Open은 완전 가림 여정 표시, Hide는 초기화/비활성화다. 로딩 대기를 지원하지 않는 현재 LodingSystem에 가짜 타이머나 씬 로드 호출을 추가하지 않는다. Cover/Reveal로 실제 로딩을 기다리는 전체 전환 통합은 후속 범위다.

SSOT/SOLID/FW: 화면 개폐는 UIBase, 전환/시간은 기존 Playback, 조립은 Editor 적용 메뉴로 분리한다. Runtime 표현 어셈블리에 Systems 의존성을 추가하지 않고 Integration에서만 필요한 UI 계약을 참조한다. 서비스 생성·등록·Export·Global 조회 경로는 변경하지 않는다. Core/카메라/Architecture는 수정하지 않는다. 숨겨진 UI가 다시 표시되거나 중복 등록되지 않도록 필수 참조와 기존 Loading ID 중복을 적용 전에 검사한다.

- [ ] P2.1 UIBase 소유자 및 독립 Integration 어셈블리 추가.
- [ ] P2.2 Undo 가능한 현재 씬 적용 메뉴 구현, 적용 전 씬 사본 보존.
- [ ] P2.3 현재 씬 배치·참조·정렬·배경 교체 후 저장.
- [ ] P2.4 Unity 컴파일 및 현재 씬 Play 움직임 확인, 변경 범위 기록 후 Phase 종료.
