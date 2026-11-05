# Boot 종료·옵션 이동·여정 포기 설계 및 Task 계획

2026-10-19. 사용자 요청 원본은 이 세션. 각 Phase 완료 후 멈추고 리뷰한다. 커밋·push하지 않는다.

## 1. 목표와 책임

- Boot QuitButton: 즉시 플랫폼 종료. 기존 OptionGameExit 재사용. 에디터는 재생 정지, 플레이어는 Application.Quit.
- Option 타이틀로: Confirm Y에서만 요청. N은 이동하지 않는다. 옵션 Hide로 시간·오디오·입력·편집을 정리하고 런타임의 소유자가 기존 런타임과 유지 Canvas를 해제한 뒤 Boot로 재시작한다. 유지 Bootstrap의 중복 인스턴스 방지와 씬 재진입 계약을 보존한다. UI는 SceneManager를 직접 호출하지 않는다.
- Combat 여정 포기: 기존 TryRequestAbandon과 결과 소비·결과창 확인·EndBattle 연쇄를 유지하고 자발적 포기의 목적지를 Lobby로 바꾼다. 일반 패배의 Lobby 정책과 일반 승리·최종 승리 정책을 섞지 않는다.
- StageSelect 여정 포기: Core Run 종료 수락·완료 확인 뒤 Lobby 이동. Core API가 없어 사용자 결정에 따라 요청만 기록하고 연결 대기한다. 새 Run 생성만으로 Run 종료를 대신하지 않는다.

## 2. 구조 점검

입력은 UIInputBinding → IUIInputReceiver의 직접 호출. Inspector UnityEvent·onClick 리스너를 추가하지 않는다. TitleUI는 기존 UIId.Title 루트/Exclusive 등록을 유지하고 Quit은 내부 위젯이다. Bootstrap이 BindStart 때 입력을 한 번 연결하며 씬 수명과 UISystem의 Open/Hide를 따른다. 숨김 상태의 입력은 무시하고 필수 Button 참조 누락은 구성 예외로 처리한다.

서비스 생성·등록·Export·소비: 기존 GlobalSystemFactory가 UISystem/GlobalUIPresentation을 Global Builder에 생성·등록하고 GlobalSystemGroup Context가 보관한다. Bootstrap과 Option은 기존 Global/LifeCycle Context를 사용한다. 새 루트 서비스 재등록은 하지 않는다. 플랫폼 종료 어댑터는 내부 협력 객체로 재사용하며 Framework 서비스로 등록하지 않는다.

SSOT: Run Outcome/Completion은 Core/Buffer, 전투 결과 확인은 기존 ProgressView, 전투 이탈 판단은 CombatPlan, 런타임 종료·재생성 수명은 Bootstrap 소유 경계에 둔다. Option에 Run 상태·씬 전환 상태를 복제하지 않는다. 조립과 기능을 분리하고 타이틀 버튼 표시 변경 때문에 Core/전투 이탈 구현을 수정하지 않게 한다. 새 이동 계약을 추가할 때 Factory → Group/Context → 소비자 경로와 FW-001/002/008/009를 다시 확인한다.

## 3. Task 및 Phase

### Phase 1 — Boot QuitButton

- TitleUI에 필수 quitButton 참조, UIInputBinding 슬롯, 중복 종료 방지 연결.
- Boot.unity의 기존 QuitButton을 연결. 시각 배치·기존 시작 버튼 연결은 유지.
- BootTitleUISetup도 QuitButton 참조를 보존/생성하도록 보완.
- EventSystem 클릭/Submit, 숨김 입력 무시, Boot 직렬화 참조 검증을 추가.
- 상태: 코드 반영. 컴파일 및 실행 검증은 세션 최종 보고를 따른다. 완료 후 리뷰 지점에서 멈춘다.

### Phase 2 — 타이틀 복귀

- Option의 Confirm Y → 명시적인 이동 수신 계약 → 런타임 소유자의 재시작 요청 경로 확정.
- 활성 전투·로비·StageSelect 모두에서 옵션 정리, 입력 정지, 런타임 Shutdown 1회, 유지 Canvas 해제, Bootstrap 중복 방지 해제, Boot 재로드 순서를 설계·구현.
- 프레임 중 실행 중인 Job/Update와 정리의 재진입을 막는다. Confirm 콜백에서 씬을 즉시 교체하지 않는다.
- Confirm N, 중복 Y, 일시정지 복원, Boot에서 새 게임 1회, 유지 Canvas/서비스 중복 없음 검증.
- 상태: 미구현. 런타임 리셋에 따른 진행 소실/정리 의미를 구현 전에 대조한다.

### Phase 3 — 포기 후 Lobby 복귀

- CombatPlan에서 자발적 포기의 목적지만 Lobby로 변경. 결과창 확인과 기존 완료·정리 연쇄 유지.
- 기존 전투 포기의 새 Run 준비를 유지하는 변경과, 종료 상태를 Lobby까지 보존하는 변경은 다르다. 사용자가 요구한 Run 종료의 의미와 현재 로비 출발 계약을 대조하고 후자가 필요하면 먼저 설계 확인한다.
- Option의 씬별 요청은 현재 LifeCycle 사실을 읽는 소유 경계에서 분기한다. UI에서 활성 씬 이름을 검사하거나 Combat View 서비스를 StageSelect에서도 호출하지 않는다.
- StageSelect는 Core 요청 §27 인수 전까지 연결 대기. 요청을 거절/무시한 채 성공처럼 닫거나 이동하지 않는다.
- 일반 승리·패배·최종 승리와 구분한 포기 결과 확인 및 목적지 회귀 검증.
- 상태: 미구현. Core 코드 수정 금지 유지.

## 4. 검증 기록

Unity 재생 및 EditMode 실제 실행과 외부 C# 컴파일을 구분해 보고한다. 다른 작업 영역은 검증 범위에 관성적으로 추가하지 않는다. Core 요청 §27은 문서화만 했으며 외부로 전송하지 않았다.
