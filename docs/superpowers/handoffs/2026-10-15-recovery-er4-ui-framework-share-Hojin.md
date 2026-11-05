# 회복 ER4 UI·Framework 공유 메모

- 작성자: 호진(이호진)

작성 2026-10-15, 호진 View 범위. UI 현강님·Framework 민형님이 인수할 공개 연결 메모이며 **작성 완료·외부 미전달**이다. 완료/테스트 원본은 [ER4 실행 기록](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#39-er4-팝업-uicontroller-2026-10-15)이다. 사용자가 커밋·push한다.

**같은 날 후속 병합 갱신:** 아래 ER4 당시 `Recovery=16`은 당시 이력이다. `95dbf3f5`의 현재 값은 Confirm=16·KeySetting=17·SoundSetting=18·**Recovery=19**다. 이 공유 메모의 옛 번호를 코드에 재적용하지 않는다.

**ER5 정책 변경 승인(2026-10-15):** 사용자가 회복을 상점과 같은 입력 정책으로 먼저 연결하도록 명시했다. 이번 ER5는 runtime `Assets/ProjectL/Prefabs/UI/Recovery/RecoveryUI.prefab`과 StageSelect의 `StageMapSceneRoot.recoveryUI` 참조·UISystem 등록을 연결한다. 기존 공용 카드·Scope·수명 계약과 현재 Recovery=19를 유지한다. 글로벌 HUD 전체의 입력·Navigation·초점 차단 U0는 공개 UI 계약 인수 뒤 별도 후속으로 분리하며, 이번 씬 연결과 상점 정책의 Play 검증을 막지 않는다. D0는 기존 옵션 1·2·3 순서·임시 노드 외형·그림 없음·현재 설명 fallback 유지로 승인됐고, 비용·회복 규칙·Position·Weight는 바꾸지 않는다. 정책과 진행 상태는 [연결 설계 §7](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#9-팝업-표시복구입력-er4er5)·[연결 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약), 최초 U0 점검 이력과 이후 변경은 [선행 점검표](../specs/2026-10-11-noncombat-event-recovery-design-Hojin.md#121-u0-글로벌-hud-전체-입력-차단-후속)를 따른다.

**ER5 실제 조립 갱신:** Unity Editor API로 [`RecoveryUI.prefab`](../../../Assets/ProjectL/Prefabs/UI/Recovery/RecoveryUI.prefab)을 생성하고 StageSelect의 명시적 참조를 연결했다. 루트는 활성 상태, Panel은 초기 숨김 상태이며 전면 `PointerBlocker`도 Panel 아래에서 함께 숨겨진다. 저작 상태의 `CardsArea`는 비워 두고 실제 옵션 목록으로 채운다. `cardPrefab`은 Panel의 비활성 `Templates` 아래 공용 `StageOfferCardView` nested 프리팹 인스턴스를 가리킨다. 회복 설명과 두 줄의 선택 불가 사유가 잘리지 않도록 이 인스턴스의 설명 높이·폰트·버튼 위치를 조정했으며, 공유 카드 원본과 상점 레이아웃은 수정하지 않았다. 회복 제목·HP 영역도 해당 runtime 프리팹에서 배치했다. 새 글로벌 서비스·입력 Layer·Scope를 추가하지 않았다. 최종 관련 검증·범위 판정의 원본은 [ER5 실행 기록](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#310-er5-상점-입력-정책으로-씬-연결-2026-10-15)이다.

**ER6 검증 인계 갱신:** 현재 실제 Boot/DB의 Event 진입·미선택 완료·맵 복귀/다음 노드·입력 취소와 손상 HP Core fixture의 선택/다음 전투 HP를 구분해 검증했다. 기존 공용 프레임·요청/표시 수명과 상점 정책을 유지했으며 제품 소스·씬·프리팹은 추가 변경하지 않았다. 검증 수치·잔여 사람 확인은 [ER6 실행 기록](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#311-er6-운영-경로-검증-2026-10-15), 현재 호출 흐름·소유와 확인 목록은 [ER6 인계](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#2-남은-일)를 따른다. U0 전체 차단은 여전히 후속이고 이 메모와 새 인계는 외부 미전달이다.

다음 항목은 ER4 당시 구현 인계다. ER5의 정책·실제 에셋 연결 범위와 ER6 검증은 위 갱신을 우선한다.

- `UIId.Recovery = 16`을 추가했고 기존 0~15는 유지했다. `RecoveryUI`는 별도 child Panel·공용 `StageOfferCardView`·HP/Gold/Receipt/상태 문구·나가기 버튼을 받는다. 당시 실제 프리팹·StageSelect UI 등록은 후속 ER5 범위로 남겼다.
- UI 입력은 `UIInputBinding`/`IUIInputReceiver`, 실제 표시 옵션 ID·전체 `RecoveryEntryContext`를 복사해 ER3 `IRecoveryRequestSubmitter`로 제출한다. `BindUIService`는 최상위 `SystemGroupsService.InGame.ViewGroup.RecoveryRequests`를 조회한다. raw Buffer WriteSink·ack·Core 거래 호출을 UI에 주입하지 않는다.
- `IRecoveryPopupDisplay.PendingNumber/PendingCommand`는 현재 표시가 제출한 의도의 읽기 전용 조회다. Controller가 현재 표시 토큰·Context와 `IRecoveryRequestStatus.TryGetResult`를 대조한다. Hide/Detach는 runtime 요청을 취소·소비하지 않으며 ER3가 정확한 결과를 계속 배출한다.
- `StageMapViewSystem.PostBindService`가 기능 Factory를 만들고, 연결별 `StageMapViewScope`가 Controller를 소유한다. Factory는 Query·Status·UI·View 외형·오류 경계만 고정 보관한다. Scope가 상점/회복 현재 상태를 명시적으로 고르며 완료된 회복을 재열지 않는다. 새 Framework 서비스·범용 Router는 없다.
- 기존 `PopupViewScope`와 부모의 공용 `PopupInputSession`을 사용한다. ER4 당시 회복은 `Begin(null)`로 게임 액션을 껐다. **현재 ER5 승인 정책**은 기존 `ShopPopupInputLayers.Popup`으로 맵 선택·진행 중 누름/드래그를 취소·차단하고 CommonMap 설정을 허용하는 것이다. UI 닫기 반환 뒤 자기 토큰만 복원하며 외부 Hide 다음 Tick 복귀·교체된 옛 호스트 보호·기존 D18 예외를 유지한다. 상점 정책 자체는 변경하지 않는다.
- CameraCatalog/CameraEffect/FieldCamera·Core·Framework 내부·기존 공용 Scope 코드는 변경하지 않았다. 카메라 pose·렌즈·드래그 위치를 새로 저장하거나 이동하지 않는다.

ER4에서는 `StageMapSceneRoot.recoveryUI`의 명시적 참조 자리만 제공했고 운영 프리팹·씬을 자동 탐색하거나 임시 등록하지 않았다. 이번 ER5는 이 자리를 실제 runtime 프리팹 인스턴스에 연결한다. 전면 배경 raycast와 맵 Layer 차단을 글로벌 HUD 전체의 Submit/Navigation/초점 차단 증거로 해석하지 않는다. U0의 유지 HUD 입력 차단 lease·정확한 초점 복원은 후속 계약 인수와 별도 검증 대상이다. ER4 fixture, ER5 에셋·씬 조립/관련 테스트, ER6 실제 운영 재생·사람 GameView 인수 결과를 구분한다. 이 메모의 정책 기록 자체는 ER5 테스트나 ER6 운영 검증의 통과 증거가 아니다.
