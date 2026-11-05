# 배치·전투의 밤·아침 스텐실 연결 공유

2026-10-18. 사용자 지시로 연결한 Phase C1 기록이며 외부로 전달하지 않았다.

CombatEntered는 밤(Night Camera / BefalRendering), NightEnded는 아침(Day Camera / AfterRendering)으로 전환한다. 요청 기본값은 CombatPhaseTransitionSettings 한 곳이며 속도 12·목표 반경 100·밤 magenta/아침 cyan이다. 물리 카메라·고정 레이어·원 설정은 기존 ScreenTransitionInitModule을 따른다.

CombatDisplayServices.From → CombatSceneDisplayFactory → CombatSceneEventPlayback → CombatPhaseTransitionPresenter로 실제 서비스를 주입한다. Presenter는 기존 사건 Presenter도 호출해 BGM과 사건 처리를 보존하며 전환 Scop과 기존 사건이 모두 끝나야 반환 후 completed를 알린다. 진행은 Render Tick만 담당한다. 입력 Layer·FinalizeResult·Core 단계 계산은 수정하지 않았다.

사건 재생 중 다음 사건·결과 완료를 막는다. 취소·표시 재설치·Clear·Release·실패는 CancelPendingEvents를 통해 전환 Scop도 반환한다. 세대 검사로 취소된 사건의 늦은 완료 콜백을 무시한다. 기존 테스트 서비스 묶음에서는 전환을 생략할 수 있으며 운영 From은 실제 서비스를 제공한다.

사용자가 Stage.prefab에 카메라를 넣었다고 확인했고 저장된 프리팹의 활성 Day Camera(layer 30)·Night Camera(layer 31) 및 Normal_A의 해당 프리팹 참조를 읽기 확인했다. 씬·프리팹·Core·FieldCamera·Architecture는 편집하지 않았다. 카메라가 런타임에 설치된 뒤 기존 AfterLoad 카탈로그 수집에 포함되는 구성을 사용한다.

MSBuild 두 어셈블리 성공, 변경 소스 새 경고 없음. 관리 테스트 56/56 통과. 실제 Factory/Display/Ordered 연계와 정리 경로 테스트 2개는 컴파일 확인·Unity 실행 대기다. 로그와 SSOT/SOLID/FW·변경 독립성 점검은 [설계·실행 기록](../specs/2026-10-18-combat-phase-stencil-transition-design.md)을 참고한다. 실제 Unity Play 화면 확인은 아직 하지 않았다.
