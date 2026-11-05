# Combat 턴 전환 안내

2026-10-28 사용자 요청. 기존 Combat 씬의 TurnDisplay / TurnText를 사용한다.

## 설계와 작업 범위

단일 Phase: 화면 구현 → 기존 전투 표시 경로에 조립 → 씬 연결 → 컴파일·참조 검증.

- `TurnDisplayUI : UIBase`, `UIId.TurnDisplay = 41`, 부모 None, 정책 Parallel.
- UISystem.BindScene이 비활성 씬 인스턴스도 수집하고 등록·서비스 바인딩한다. 처음에는 숨김 상태다.
- CombatSceneRoot는 참조만 제공하고 CombatSceneDisplayFactory가 Presenter를 만든다.
- CombatSceneDisplay.RefreshCore에서 결과 재생을 마친 Buffer 사실을 Presenter에 전달한다.
- 실제 Player/Enemy 턴에 진입한 경우만 표시한다. Starting 단계는 시작 처리 대기이므로 표시하지 않는다.
- 같은 ControllerTurnSequence의 반복 갱신은 다시 열지 않는다. 배치·완료·Clear·Release에서 안내와 표시 이력을 정리한다.
- ShowPlayerTurn은 검정 `플레이어 턴`, ShowEnemyTurn은 빨강 `적 턴`이다.
- ShowEnvironmentTurn은 예약 함수이며 현재 아무 동작도 하지 않는다.
- 문구와 색을 정한 뒤 OpenUI(ID, true)로 열고 등장·유지·퇴장 연출(아래 "등장·퇴장 연출")이 끝나면 CloseUI(ID)로 닫는다. 활성 상태는 UIBase.Open/Hide만 변경한다.
- 표시 중 새 전환이 오면 최신 문구로 바꾸고 등장부터 다시 한다. 안내 자체가 전투 진행이나 입력을 대기시키지는 않는다.
- 뒤로가기 이력에 넣지 않으며 포인터 입력을 가리지 않는다. 숨김·부모 비활성·씬 해제 시 타이머를 취소한다.
- 필수 TurnText·UISystem 누락과 잘못된 자식 구성은 대상 이름을 포함한 예외다.

## 구조 점검

- SSOT: 턴의 원본은 기존 Buffer의 CombatTurnFacts다. Presenter는 마지막으로 표시한 순번만 보관하며 규칙 상태를 수정하지 않는다. UI는 문구·색·표시 타이머만 소유한다.
- SOLID: 전환 판단(Presenter), 표현(UI), 생성·연결(Factory), 공통 수명(Display)을 분리한다. UIBase 개폐 계약과 기존 UISystem 인터페이스를 사용하며 입력·Core 계약을 확장하지 않는다.
- FW-001/002: 새 Framework 서비스·모듈 등록은 없다. UISystem은 기존 GlobalSystemFactory의 내부 Builder에서 한 번 생성·등록되고 GlobalSystemGroup.UIServiceCommand로 노출된다. 루트에는 IGlobalSystemGroupService만 등록한다.
- 소비 경로: UISystem.BindScene → UIBase.BindUISystemService → TurnDisplayUI. 루트 resolver로 하위 UI 서비스를 조회하거나 재등록하지 않는다.
- 기능 독립성: 문구·색·표시 시간 변경은 TurnDisplayUI에, 전환 판단 변경은 CombatTurnDisplayPresenter에 머문다. 의존성 변경은 Factory와 SceneRoot가 담당한다. 공통 Refresh/Clear/Release 연결만 Display에 추가했다.

## Unity 확인 항목

1. 전투 시작 후 첫 플레이어 턴: 검정 문구가 커진 크기에서 줄어들며 선명해지고, 약 3초 보인 뒤 0.9초 동안 투명해지며 숨는다.
2. 같은 턴에 이동·공격 등 상태를 갱신해도 반복 표시하지 않는다.
3. 적 턴 시작: 빨강 문구, 다음 플레이어 턴 시작: 검정 문구.
4. 배치 복귀·전투 완료·씬 이탈 후 안내가 남지 않는다.
5. ShowEnvironmentTurn 호출은 화면과 진행을 바꾸지 않는다.

Unity 재생 확인은 사용자 확인 대기다. 외부 컴파일 검증은 Unity Test Runner·재생 검증을 대신하지 않는다.

## 등장·퇴장 연출 (2026-11-03 사용자 요청, 컴파일 미확인, 새 시험 없음, Unity 확인 대기)

- 등장: 열리면 `introScale`(1.6배)·투명에서 시작해 `introSeconds`(0.35초) 동안 원래 크기·선명으로. 크기와 투명도는 같은 진행률(처음 빠르고 끝 느림)이라 줄어드는 동안 선명해진다.
- 유지: 등장 뒤 `holdSeconds`(3초) 그대로.
- 퇴장: `fadeOutSeconds`(0.9초) 동안 일정하게 투명해진 뒤 CloseUI. 바로 닫기(`Dismiss`: 배치 복귀·전투 완료·Clear)는 연출 없이 닫는다.
- 투명도는 TurnDisplay 오브젝트의 CanvasGroup(배경·글자 함께). 씬에 없으면 처음 쓸 때 붙이고 Raycast·상호작용을 끈다. 크기는 TurnDisplay 자신의 localScale(처음 값 기준), 숨길 때 원래 크기·불투명으로 되돌린다.
- 값 네 개는 인스펙터 "등장·퇴장 연출" 칸. 씬 값이 없으면 위 기본값. 플레이어 턴·적 턴 같은 연출. Presenter·Factory·씬·열기/닫기 계약 무변경.
