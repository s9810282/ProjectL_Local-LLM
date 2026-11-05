# CombatLog 확장 연출 설계·Task

2026-10-18 사용자 합의 기준.

## 설계

- PannelImage의 상단·폭과 최신 로그·버튼 위치를 유지한다. 높이만 아래로 확장하며 원래 RectTransform 높이로 복귀한다. Sliced 이미지를 유지한다.
- 펼친 끝점은 레이아웃 반영 후 Logs/Viewport 하단이다. 확장 높이를 별도 직렬화하지 않는다.
- 2026-10-26: 프리팹 기준 펼친 PannelImage 높이를 630에서 670으로 변경했다. HistoryReveal과 Logs 높이는 500에서 540으로 늘리고, Logs의 중심 Y는 -270으로 맞춰 상단을 유지한다. 접힌 배경 높이 160, 루트 높이 100과 펼침·접힘 시간은 유지한다.
- 펼침 0.25초, 접힘 0.18초, EaseOutCubic. unscaledDeltaTime으로 진행한다. 도중 반대 입력은 현재 진행률에서 이어간다.
- HistoryReveal(RectMask2D)의 하단 padding으로 배경과 동일한 경계까지 기록을 드러낸다. Logs와 Viewport의 완성 크기는 유지하므로 애니메이션 때문에 행 배치·스크롤 범위를 다시 계산하지 않는다.
- 애니메이션 중에는 기록의 CanvasGroup.blocksRaycasts를 끈다. 완전히 펼친 뒤 스크롤과 스크롤바 입력을 허용한다. 닫기 시작 시 관성을 멈춘다.
- Hide/비활성화/새 전투 진입은 즉시 접힌 상태로 복원한다. 로그 추가는 진행 중 연출을 다시 시작하지 않는다.
- CombatLogUI는 기존 입력·이력·최신 행을 소유하고 CombatLogExpansion에 펼침 의도만 전달한다. 연출 컴포넌트는 Buffer/Core/서비스를 참조하지 않는다. 프리팹이 두 컴포넌트와 RectTransform 참조를 조립한다.
- 기존 공개 DropDown 메서드는 호출 호환성을 위해 유지한다. 연출 미연결 UI는 기존 활성 전환을 유지한다.

## 구조 점검

- SSOT: 목표 펼침 상태는 CombatLogUI, 연출 진행률은 CombatLogExpansion. 높이 원본은 접힌 이미지와 Viewport 배치다. 게임 사실은 기존 Buffer만 읽는다.
- SOLID: S는 이력과 연출 분리, O는 연출 컴포넌트 추가, L은 기존 공개 호출/재활성화 계약 유지, I는 SetExpanded 호출만 사용, D는 서비스 의존성 추가 없이 프리팹이 표현 참조를 연결한다. 교체 사례 없는 범용 인터페이스는 추가하지 않는다.
- JMH/FW: UIBase 등록·이력과 UIInputBinding 직접 수신을 유지한다(FW-008/009/202). 새 서비스·이벤트 구독·Builder·공용 Framework 변경은 없다.
- 독립성: 시간·이징·마스크 변경은 연출 컴포넌트와 연출 테스트에 한정한다. 공격 결과 수집과 Core 계산을 수정하거나 재검증할 필요가 없다.

## Task / 리뷰 지점

### Phase 1 — 코드·프리팹·관련 검증

- [x] 확장 컴포넌트 및 .meta 추가, CombatLogUI의 의도·즉시 초기화 연결.
- [x] 프리팹에 HistoryReveal과 명시적 참조 연결. 같은 Content를 구동하는 불필요한 루트 ScrollRect를 제거하여 Logs의 ScrollRect만 유지.
- [x] 상단 고정·끝점·반전·스크롤 차단·재활성화·프리팹 연결 검증. 기존 버튼 클릭/Submit 및 이력 테스트 유지.
- [x] 컴파일·가능한 EditMode 검증 결과와 미실행 항목을 기록하고 멈춘다.

### Phase 2 — Unity 화면 리뷰 후 조정

- 사용자 화면 확인: 펼침/접힘 속도, 테두리, 행 노출, 스크롤, 연속 토글, 전투 재진입.
- 확인 결과가 있는 항목만 조정한다. 장식 회전·반동·사운드는 현재 범위에 포함하지 않는다.

## 실행 기록

- 시작 시 기존 사용자 변경: Scenes/Combat.unity. 이 작업에서는 편집하지 않는다.
- 저장소 루트 docs는 현재 쓰기 범위 밖이므로 설계·Task를 기능 옆 문서에 기록한다.
- Unity 6000.0.73f1 에디터 컴파일 성공. 기존 LobbyDepartureIntegrationTests.cs의 CS0105 중복 using 경고는 이번 변경 밖이다.
- Unity TestRunner API로 CombatLogUITests EditMode 6/6 통과(신규 2개 + 기존 4개). 패널 상단 고정, Viewport 하단 도달, 마스크와 배경 경계 일치, 반전 연속성, 로그 추가 중 연출 유지, 스크롤 입력 차단/해제, Hide/Open 복원, 프리팹 연결, 클릭/Submit, 기존 이력 수명을 확인했다.
- 최초 실행에서 기존 테스트 fixture가 EditMode의 일반 MonoBehaviour Awake를 실행하지 않아 초기 상태/버튼 연결 검증이 실패했다. CreateUI가 참조 연결 후 Awake를 명시적으로 실행하도록 보완한 뒤 전체 6개 통과를 확인했다.
- 임시 에디터 테스트 실행기와 .meta는 실행 후 제거했다. 전체 게임 Play 및 화면상 속도·테두리·스크롤 조작은 Phase 2 사용자 리뷰 대기다.
