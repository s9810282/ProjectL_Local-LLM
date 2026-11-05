# Combat 종료 UI — 설계·Task·실행 기록

- 날짜: 2026-10-19
- 사용자 승인: Run 누적 획득 명성·소비 골드 계약만 남기고 나머지 구현. 최초 배치 참가자 사용. 중간 레벨 요구량 없이 레벨 차이만큼 만충/초기화 후 최종 비율 표시.
- 범위: CombatEndCanvas의 승리·패배·포기 표시, 승리 보상·4칸 동시 EXP 연출, 기존 확인/이탈 연결. LastHit은 공란. 사망 캡처는 가능성 점검만.
- 상태: 구현 완료. MSBuild 및 경험치 관리 테스트 완료, 실제 Unity EditMode/재생 확인 대기.

## 1. 설계와 데이터 원본

승리: Completion.Rewards의 StageDepth / (StageWidthIndex + 1), TotalGoldEarned, ClearFame을 표시한다. 현재 계약상 명성은 ClearFame만 있으며 처치 명성 계약은 없다. ClearParticipantIds 순서(메인 우선, 안정적인 보유 ID 순서)의 Characters 행에서 CharacterKey를 찾아 View UnitVisual.InfoIcon을 쓴다. InfoIcon은 기존 외형 계약에서 선택 자원이므로 없으면 초상화만 비운다. 비참가자 처치 경험치 행이 추가되어도 슬롯은 차지하지 않는다. 참가자 0~4명은 유효하며 빈 슬롯은 숨긴다. 운영 승리에 보상 영수증이 없거나 4칸을 넘으면 명시적으로 실패한다.

EXPText는 각 행 TotalExperience를 +N으로 표시한다. LevelBefore/After 차이가 0이면 Before 비율에서 After 비율까지, 양수이면 Before→1, 0→1 반복, 마지막 0→After 비율로 표시한다. 각 구간은 Inspector experienceSegmentSeconds(기본 0.65초), unscaledDeltaTime을 쓴다. 큰 프레임에도 100%와 0%를 건너뛰지 않는다. 첫 만충에서 LvUp을 켜고 완료 화면이 닫힐 때까지 유지한다. 요구량 0은 계약상 경험치 꺼짐이므로 게이지를 끈다. 네 칸이 같은 화면 Update에서 함께 시작하고 개별 완료된다. 중간 요구량·성장 규칙·보상을 View에서 계산하거나 지급하지 않는다.

패배/포기: Completion.Outcome.Reason으로 '패배'/'전투 포기'를 표시한다. CurrentProgress.CurrentMapKey와 완료 NodeId/StageKey로 StageProgress 좌표를 조회해 동일한 n-n 형식으로 표시한다. 조회 실패는 예외다. Run 누적 두 수치는 사용자 결정대로 계약 대기이며 빈칸으로 둔다. 현재 잔액/전역 명성/0으로 대신하지 않는다. LastHit과 하위 안내 문구는 숨기고 비운다.

## 2. UI 등록·조립·수명 점검

- 등록 단위: CombatEndCanvas의 CombatEndUI : UIBase. UIId.CombatEnd=26, ParentID=None, Parallel, OpenUI(ID, true)로 뒤로가기 이력 제외. 내부 슬롯은 별도 UI 등록 없이 화면 소유 위젯이다.
- 생성/등록: 씬이 화면과 슬롯을 생성한다. 기존 UISystem.BindScene이 비활성 UIBase를 수집하고 AddUI/BindUISystemService/BindUIService를 수행한다. 화면은 BufferSystemKey.Buffer에서 필요한 Current/Stage 읽기 싱크만 보관한다.
- 조립: CombatSceneDisplayFactory가 SceneRoot.Completion을 필수 검증하고 UnitVisualTable을 Configure로 주입한 뒤 같은 화면을 Display에 넘긴다. 개별 슬롯의 표시/연출은 Factory에서 구현하지 않는다.
- Global 서비스: 기존 GlobalSystemGroup의 UIServiceCommand를 쓰는 경로를 유지한다. 새로운 IServiceModule/서비스 키/루트 재등록이 없다. UIId는 화면 등록 ID이며 서비스 등록 ID가 아니다. FW-001 중복 서비스 등록과 계층 우회 둘 다 추가하지 않는다.
- 호출: CombatViewSystem.PresentCompletion→CombatSceneDisplay.ShowCompletion→CombatEndUI.Present→IUISystemService.OpenUI. 완료 확인은 UIInputBinding→IUIInputReceiver.Click→CloseUI→기존 confirmed 콜백. Button.onClick/Inspector UnityEvent는 연결하지 않는다.
- 확인은 즉시 가능하며 화면을 닫고 진행 중 게이지를 정리한다. 보상 수령 API나 추가 Core 호출은 없다. 기존 CombatPlan이 표시 확인 후 EndBattle/새 Run/씬 전환을 소유한다.
- Hide/OnDisable/Display.Clear/Release에서 애니메이션과 콜백을 비운다. 반복 Dismiss는 열린 화면이 없으면 정상 무동작이다. 화면 외부 직접 SetActive 호출은 추가하지 않는다.
- 기존 격리 표시 시험의 직접 CombatSceneDisplay 생성은 completion 인수를 생략할 수 있고 옛 CombatHud 확인 입구를 유지한다. 운영 Factory는 SceneRoot.Completion 필수 검증을 거치므로 이 경로가 운영 누락을 숨기지 않는다. HUD 전체 일괄 재구성은 범위 밖이다.
- 구성 검증은 화면을 열기 전에 수행한다. 필수 참조 누락은 UIId·필드·객체가 포함된 InvalidOperationException으로 실패한다. 외형의 선택 아이콘과 바인딩 전/반복 해제만 정상 빈 상태로 취급한다.

## 3. 원칙 점검

- SSOT: 전투 정산/레벨은 Buffer 영수증, 좌표는 StageProgress, 외형은 View DB. 별도 누적 장부·진행 계산·추측 임계치를 만들지 않는다. 화면의 Fill/시간/콜백은 표시 수명이다.
- SRP: 화면은 결과 표시/개폐, 슬롯은 한 칸 표현, 순수 Animation은 비율 재생, Factory는 조립, Plan은 이탈을 각각 맡는다.
- OCP: 새 화면·위젯·재생기로 확장하며 기존 Plan/Job 분기를 추가하지 않는다.
- LSP: UIBase Open/Hide, IUIInputReceiver와 기존 완료 확인 계약을 유지한다.
- ISP/DIP: 화면은 필요한 Buffer 읽기/외형 조회/UISystem 계약만 받는다. Core 계산기나 InGame DB를 조회하지 않는다.
- 기능 독립성: 게이지 연출 변경은 Animation/Slot과 관련 테스트에 머문다. 화면 배치/문구 변경은 CombatEndUI/씬에 머문다. 의존성 변경만 Factory/SceneRoot에 반영한다. 기존 CombatSceneDisplay에는 완료 표시/정리 위임만 추가한다. 공유 정리 경계 변경에는 기존 표시 테스트 컴파일 및 Unity 회귀 확인이 필요하다.
- FW: 기존 UI 등록·바인딩과 단일 Build를 유지한다. 새 수명 모듈이나 이벤트 구독은 없다. .cs/.meta 쌍을 생성하고 기존 에셋 GUID는 유지했다. Core·FieldCamera·Architecture 로직은 수정하지 않는다.

## 4. Phase E1 Task와 실행

1. [x] 확정된 요구사항/기존 Buffer/완료 확인/UISystem 경로 조사.
2. [x] CombatEndUI와 UIId 추가, SceneRoot/Factory/Display 연결.
3. [x] CombatRewardSlot 및 순수 EXP 애니메이션 구현.
4. [x] 최신 Combat.unity/CharactorReward.prefab에 직렬화 참조 연결. 종료 Canvas/양쪽 패널/LvUp/LastHit 기본 비활성, EXP Fill 초기 0. 기존 HUD의 누락 completionText도 실제 확인 글자로 연결.
5. [x] 관리 테스트: 사용자 예시, 다중 레벨, 레벨업 없음/정확 만충/보상 없음/4칸 동시/꺼짐/잘못된 시간 10건 통과.
6. [x] UI EditMode 테스트 추가: 실제 슬롯 프리팹을 사용한 보상·참가자 표시, 포인터 클릭/Submit, 단일 확인, 패배/포기, 미연결 합계 공란, 재사용/숨김/비활성 입력, 필수 데이터 누락 검사. 컴파일 완료, 네이티브 실행은 별도 대기.
7. [x] Core 요청은 Run 누적 획득 명성·실제 소비 골드 2개뿐. 중간 레벨 요구량 요청 없음.
8. [ ] 사용자 Unity 확인: EditMode CombatRewardExperienceAnimationTests/CombatEndUITests 실행, 운영 Database 모드 실제 승리/패배/Option 포기, 4인·다중 레벨 EXP, 확인 후 기존 목적지 이동.

검증 환경: Unity 에디터 실행 중이므로 같은 프로젝트 batchmode를 중복 실행하지 않았다. VS MSBuild의 스크래치 custom props로 새 파일을 추가하고 Framework 테스트 어셈블리까지 컴파일했다. Unity 생성 csproj는 편집하지 않았다. 산출물은 작업 스크래치에만 두었다. 경고는 새 기능 파일이 아닌 기존 파일의 nullable/직렬화 필드/중복 using 경고다. 전체 Unity 테스트 통과나 수동 화면 인수를 주장하지 않는다.

진행 중 씬이 다시 저장되어 새 SceneRoot 참조와 활성 상태가 달라진 것을 발견했고, 최신 씬을 유지한 채 필요한 필드만 재연결했다. 다른 기존 수정 파일은 되돌리지 않았다. 커밋/push/외부 전달은 수행하지 않았다.

## 5. LastHit 후속 가능성

Unity 6 ScreenCapture.CaptureScreenshotIntoRenderTexture로 프레임 완료 뒤 화면을 캡처하고 RawImage.texture로 표시할 수 있다. https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screencapture/capturescreenshotintorendertexture

프로젝트에는 사망 모델을 사실 갱신 후에도 유지하는 ICombatUnitDeathHold가 있으므로, 메인 사망 재생 중 모델 반환 전/종료 Canvas 개방 전에 프레임을 확보하는 연결 후보가 있다. 정확한 연출 시점과 캡처 범위(HUD 포함 화면 또는 별도 월드 출력), 텍스처 소유/해제는 후속 설계 대상이다. 이번 Phase는 LastHit 공란만 구현하며 캡처 자원·카메라·Render 구조를 바꾸지 않았다.
