# 덱 편집 후 출전 — Framework·전투 연결 공유

- 작성자: 호진(이호진)

상태: 코드·프리팹 반영, Unity 컴파일·재생 미확인. 외부 전달하지 않음.

원본: [스테이지 맵 설계 §6.7](../specs/2026-10-10-stage-map-design-Hojin.md#67-전투-노드-출전--덱-확인-뒤-진입-10-29), 결과는 [스테이지 맵 계획 §3.17](../plans/2026-10-10-stage-map-plan-Hojin.md#317-덱-확인-뒤-출전-10-29).

## 변경 계약

- 전투 노드 클릭 직후에는 표시 마커만 이동한다. 사용자가 승인한 미제출 입력이며 실제 현재 노드 Buffer는 그대로다.
- 착지 후 DeckEdit을 열고 `출전`을 누르면, 필요한 덱 Confirm 성공 후 기존 StageView 명령을 제출한다.
- 기존 Core 서비스, Stage/Combat Job·Plan, CombatEntryRequest 및 presentation lease 계약은 변경하지 않았다.
- 실제 준비 결과가 같은 명령 번호·챕터·노드임을 확인한 뒤 기존 lease에 착지 완료를 한 번 게시한다. 이미 끝난 표시 이동을 반복하지 않는다.
- 닫기·취소·Core 거절 시 실제 진행 위치로 표시를 복구한다. 비전투 노드 명령 경로는 유지한다.

## 공용 UI 변경

- `UIId.DeckCombatConfirm = 43` 추가. 부모 `DeckEdit`, 기본 Exclusive.
- 생성 주체: DeckEditUI 프리팹(새 생성은 StageDeckEditUISetup). 등록: 기존 UISystem.BindScene의 UIBase 수집. 새로운 서비스나 루트 Export 없음.
- 소비자: 기존 덱 컨트롤러에 전달된 IUISystemService. 새 확인창은 덱 Canvas 내부의 마지막 형제이며 중첩 Canvas나 Global Notice를 사용하지 않는다.
- 입력: UIInputBinding/IUIInputReceiver. 확인창 키보드 선택을 확인·취소 사이에 제한하고 닫을 때 이전 선택을 복원한다.
- 기존 DeckEditUI 프리팹 참조에 startCombatButton, combatConfirmUI를 추가했다. StageSelect의 기존 Confirm 위치(-200)에 출전 버튼도 맞췄다.

## 검증 상태

- 정적 검수: 프리팹 추가 블록 36개의 ID 중복 없음, 로컬 참조와 부모·자식 연결 확인. 새 C# 3개와 meta 3개 연결 확인. 변경 코드와 씬 diff 공백 검사 통과.
- 작성자 자체 검수에서 반복 Observer Tick의 착지 중복 게시를 보완했다. 별도 에이전트는 사용하지 않았다.
- Unity 컴파일, 버튼/키보드 실제 조작, 변경 덱의 전투 반영, 일반 덱·비전투 회귀는 미확인이다.
