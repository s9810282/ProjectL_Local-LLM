# 비전투 View 구조 개선 — 현강님 UI 공유 (2026-10-15)

- 작성자: 호진(이호진)
- 상태: **작성 완료·미전달.** 사용자가 직접 전달한다. 합의·외부 인수를 완료했다는 뜻이 아니다.
- 범위: 비전투 View 계획 P6.3. [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md) D5·D6·D10·D13·D16·D18·D19와 [View 구조 계획](../plans/2026-10-13-view-structure-plan-Hojin.md)이 원본이다. 둘은 2026-10-27에 옛 비전투 View 설계·계획을 합친 문서이며, § 번호는 합치기 전 판 기준이다(옛 판은 git `10914570`).

**최신 코드 대조(2026-10-16):** HEAD `70de006629feaf880f876c7b8e41fa14297237cf`에서 아래 공용 팝업·로비 흡수 구조를 다시 확인했다. P0~P6·R6·P2.5/P3.8의 자동 검증·Play·Profiler 수치는 각 당시 실행 기록이며 이번에는 소스·문서만 대조했다. 전달 상태는 여전히 미전달이다.

로비 덱·아티팩트와 스테이지 맵 상점의 열기/닫기·입력 소유를 공용 `PopupViewScope`로 옮겼다. 씬 연결은 `LobbyViewScope`·`StageMapViewScope`가 소유한다. 아티팩트는 로비 시설에서 열며 독립 `ArtifactViewSystem`·`ArtifactPopupSession`은 제거했다.

`OpenStateChanged`의 수명 구독은 `PopupViewScope` 한 곳에서 화면별 `XxxPopupViewScreen` 어댑터를 통해 한다. UI가 소유하는 `IArtifactPopupDisplay` 등 기존 표시 계약과 `UIInputBinding`·`IUIInputReceiver`는 그대로 사용한다. View 내부의 `IPopupViewHost`는 현재 씬 연결인지 검사하는 계약이며 UI가 구현하지 않는다. 화면은 UI 상태·표시·입력 전달을 맡고, Buffer·DB 조회와 게임 규칙 판단은 맡지 않는다.

입력 순서는 호스트 차단 → 팝업 Layer → `OpenUI`다. 명시적 닫기는 `CloseUI` 반환 뒤 Layer를 반납한다. `HideAllUI`·Back·다른 화면 교체처럼 외부에서 숨겨지면 표시·수신기를 같은 호출에서 정리하고, Layer 반납과 호스트 입력 복귀는 **다음 Tick**에 한다. 다른 화면이 Layer를 가져갔다면 그 Layer를 보존한다. 외부 열림도 다음 Tick에 넘겨받으며, 연결 때 이미 열린 화면은 `Start`에서 넘겨받는다([정책 원본(옛 §4.2)](../specs/2026-10-13-view-structure-design-Hojin.md)). UI 알림 안에서는 Layer를 바꾸거나 UISystem을 재귀 호출하지 않는다.

`OpenStateChanged(bool)`만으로는 숨김 사유·모든 UI dispatch 경계를 알 수 없다는 한계가 남아 있다. 숨김 사유 API가 생기면 그 계약을 인수한 뒤 별도로 조정한다. 이번 작업으로 UISystem·UIBase·UIInputBinding의 계약이나 UI 소유 파일을 바꾸지는 않았다. View 동작 객체에는 `XxxPopupController` 이름을 쓰고, UI 표시 데이터의 `XxxContent`와 구분한다. `StageMapViewSystem`의 기존 BGM/AMB 호출 주체와 정상 이탈 순서는 유지했다.

아티팩트 화면 구성·열기 경로·닫기 수신과 확인 항목은 [다시 작성한 아티팩트 UI 공유 문서](2026-10-06-artifact-popup-ui-hyeongang-share-Hojin.md)를 확인한다. P6 뒤 별도 ER3~ER5에서 회복 요청 수명·팝업·운영 연결을 구현했고, 현재 `StageMapViewScope`가 회복 Controller도 소유하며 기존 `PopupViewScope`를 재사용한다. 상점 입력 정책·U0 후속과 ER6 자동/fixture/출력·미실행 사람 확인은 [회복 연결 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#1-진행-요약)·[회복 UI·Framework 공유](2026-10-15-recovery-er4-ui-framework-share-Hojin.md)를 따른다. 이 메모 대조로 회복을 구현하거나 검증한 것은 아니다.

P3.8의 **즉시 새 호스트 게시·새 Layer 미취득** 조합은 사용자 승인으로 검증 보존에서 제외했다. UI 반환 뒤 정리와 새 Layer를 소유한 즉시 교체 보호를 각각 확인한 후속이며, 무Layer 즉시 교체까지 보장한다는 뜻이 아니다([현재 수명 계약의 좁은 예외(옛 §9)](../specs/2026-10-13-view-structure-design-Hojin.md)).
