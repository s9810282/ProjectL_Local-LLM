# Combat 종료 UI Framework 공유

- 날짜: 2026-10-19, View 작업. 로컬 기록, 미전달.
- 상세: [설계·Task·실행 기록](../plans/2026-10-19-combat-end-ui-plan.md)

UIId.CombatEnd(26)를 추가했다. Combat 씬의 비활성 CombatEndCanvas에 CombatEndUI : UIBase를 붙여 기존 UISystem.BindScene의 비활성 수집/등록/바인딩 경로를 쓴다. ParentID=None, Parallel, 고정 OpenUI(ID, true)로 이력에서 제외한다. 내부 4개 CombatRewardSlot은 화면 소유 위젯이어서 따로 등록하지 않는다.

생성은 씬, 등록/보관은 UISystem의 _uis, 상위 서비스 Export는 기존 UISystem→Global UIServiceCommand이며 루트에 하위 서비스를 재등록하지 않는다. 소비자는 기존 Global Context 경로의 IUISystemService를 사용한다. Buffer는 기존 BufferSystemKey.Buffer에서 Current/StageProgress 읽기 싱크만 받아 완료 좌표를 표시한다. 새 서비스 키/IServiceModule/AddModule/Builder 변경은 없다. UIId 추가는 FW-001 서비스 등록을 하나 더 만드는 변경이 아니다.

CombatSceneDisplayFactory는 SceneRoot.Completion을 필수 검증하고 기존 UnitVisualTable을 화면에 주입해 Display에 넘긴다. CombatSceneDisplay의 완료 표시/정리만 새 UI로 위임하며 기존 PresentCompletion→확인→Plan 이탈 계약과 보상 반영 시점을 유지한다. 직접 Display를 조립하는 기존 격리 시험만 옛 HUD 확인 입구를 유지하고, 운영 Factory의 누락은 필수 검증으로 실패한다.

버튼은 UIInputBinding/IUIInputReceiver로 클릭/Submit을 처리한다. 실제 개폐는 IUISystemService→Open/Hide가 수행한다. 완료 확인은 화면을 닫고 콜백을 한 번 비우며, Hide/Disable/Clear/Release에서 슬롯 연출을 취소한다. 새 이벤트 구독/보상 지급 API/씬 전환 API는 없다.

MSBuild 및 순수 EXP 연출 테스트 10건 통과. 네이티브 UI EditMode와 실제 전투 재생은 사용자 Unity 확인 대기다. Run 누적 명성·소비 골드 2개만 Core 요청이며 해당 텍스트는 계약 대기 동안 공란이다. LastHit 캡처는 후속이다. 커밋/push/외부 전달은 하지 않았다.
