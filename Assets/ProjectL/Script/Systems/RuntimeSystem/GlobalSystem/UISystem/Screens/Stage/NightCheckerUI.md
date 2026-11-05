# NightChecker UI 연결

## 설계와 Task

- 등록 단위: Combat.unity의 NightChecker 한 개. NightCheckerUI는 UIBase를 상속하고 UIId.NightChecker(22), ParentID.None, Parallel 정책을 사용한다. 자식 Text와 NightClock/Fill은 내부 위젯으로 별도 UI 등록 없이 같은 수명을 따른다.
- 2026-10-19: 기존 CurrDelay Text 하나를 progressText로 연결해 `현재 Delay/밤 종료 기준`(예: `0/1000`)을 표시한다. 기존 currDelay 직렬화 참조는 FormerlySerializedAs로 유지하고 nightEnd 참조는 제거했다. 두 숫자 중 하나가 바뀔 때 전체 문구를 갱신한다. UI 등록·수명·Buffer 조회 경로는 유지한다.
- 씬의 기존 배치·Text를 유지하고 컴포넌트와 직렬화 참조만 추가한다.
- 값 원본은 Combat Buffer다. 분자는 밤(StagePhase.Combat)의 Current.CommandState.Turn.CompletedPlayerTurnsThisNight, 분모는 같은 턴 스냅샷의 PlayerTurnsPerNight다. Core가 턴 사실을 만들 때 상한을 함께 게시하며 UI는 규칙 상수를 직접 읽지 않는다. 배치·활성 전투 없음은 진행량 0이며, 배치 복귀 뒤 남는 직전 밤의 누적값을 표시하지 않는다.
- UI Update/Open/바인딩에서 최신 상태를 읽고, 값·단계가 바뀔 때 Text와 Fill을 함께 갱신한다. 완료한 아군 턴만 세며 진행 중인 턴을 미리 더하지 않는다.
- 2026-10-26: NightClock/Fill을 progressFill로 연결했다. fillAmount는 완료 턴 / 전체 턴을 0~1로 제한한 값이며, 전투가 없으면 0이다. 씬 초기 Fill은 0이고 기존 Radial360 방향·원점은 유지한다. Bind/Open 시 Text·Fill 필수 참조와 Image.Type.Filled 구성을 상태 변경 전에 검증한다.
- [x] UIId·UIBase 구현, 씬 Text 연결.
- [x] 기존 Combat UI 개방 경로 연결.
- [x] 씬 컴포넌트 ID 중복과 Script/Text 참조 확인.
- [x] 새 파일을 Compile에 포함한 외부 MSBuild 컴파일: 오류 0, 변경 대상 밖 직렬화 필드의 CS0649 경고 7개.
- [ ] Unity 컴파일·EditMode 테스트·Combat 재생 확인.

## 조립과 수명 검사

- 생성: GlobalSystemFactory가 UISystem을 생성하고 내부 Builder에 IUISystemService/생명주기 모듈을 등록한다. NightChecker는 씬이 생성한다.
- 보관·Export: GlobalSystemGroup.UIServiceCommand가 내부 UI 서비스를 보관한다. 루트에는 기존 Global 그룹 계약을 공개하며 NightChecker용 서비스나 루트 UI 재등록을 추가하지 않는다.
- 조회: GameBootstrap은 SystemGroupsService.Global의 UIServiceCommand를 사용한다. NightCheckerUI는 BufferSystemKey.Buffer의 CombatStateReadSink만 읽는다.
- 등록·바인딩: 기존 BindLoadedUI(-200) → UISystem.BindScene → UIBase 수집/ExportUIService → BindUIService. Combat UI 개방(200)은 IUISystemService.OpenUI(NightChecker, true)를 사용한다. 고정 표시로 뒤로가기 이력에서 제외한다.
- 실제 활성 변경은 NightCheckerUI.Open/Hide만 담당한다. 숨김 뒤 다시 열면 즉시 최신 값을 표시한다. 씬 전환 때 기존 씬 UI 레지스트리를 재구성하고 씬 객체는 해제된다. 이벤트 구독·대여 자원은 없다.

## 구조 검토

- SSOT: 진행값/종료 기준은 Buffer 한 곳에만 둔다. UI 캐시는 마지막 표시 숫자만 보관하며 규칙 계산·Buffer 쓰기를 하지 않는다.
- SOLID: 표시 책임은 NightCheckerUI, 등록/개방 조립은 UISystem/GameBootstrap에 둔다. UIBase의 Open/Hide 계약과 기존 ReadSink를 사용하며 새 서비스 인터페이스를 만들지 않는다.
- FW-001/002: 새 서비스·모듈 등록을 추가하지 않는다. UIId는 기존 번호를 바꾸지 않고 22를 추가한다. Global 하위 서비스의 루트 재노출도 추가하지 않는다.
- 기능 독립성: 숫자 표현 변경은 NightCheckerUI와 씬 Text 연결에 한정된다. 전투 입력·타임라인·기존 HUD에 기능을 넣지 않는다. 공통 변경은 UIId 항목과 Bootstrap의 개방 호출뿐이다.

## Unity 확인 항목

1. Combat 진입 직후 진행값 0/8, Fill 0.
2. 밤 진입 시 0에서 시작, 아군 턴 완료마다 Fill이 1/8씩 증가한다. 적 응답만으로 추가 증가하지 않는다. 밤의 8번째 아군 턴 완료 시 8/8, Fill 1.
3. 밤 종료로 배치에 복귀한 프레임부터 진행값 0/8, Fill 0. 활성 전투가 없으면 0/0, Fill 0.
4. 옵션/Deck UI와 동시 표시, 뒤로가기로 NightChecker가 닫히지 않음.
5. UI Hide/Open 시 최신 값 복원, Combat 재진입 시 이전 전투 숫자를 남기지 않음.
