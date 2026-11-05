# 스테이지 노드 설정 정리 — 용근님 공유 메모

- 작성자: 호진(이호진)
- 날짜: 2026-10-26
- 상태: 문서 준비, 미전달.
- 근거: [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(옛 이름 조회 제거 설계 2판) Q-N6 사용자 승인, [검사 후속 정리 계획](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md)(옛 N-1 실행 계획).

## 변경

`StageMapViewDataSO.cs`의 StageMapViewData에서 운영 소비자가 없던 노드 강조 색 다섯(current/selectable/hovered/candidate/unavailable)과 프로퍼티·유한값 검사를 제거했다. 같은 파일의 nodeMask·NodeMask와 DB의 HitTarget Layer 포함 검사를 제거했다. 해당 줄은 용근님 `39b634fe`에서 도입된 부분이며 이번 사용자 승인 범위에 포함된다.

호진님 코드의 사용하지 않던 hoverEffect/selectEffect와 검증, 이름 조회 상수 넷, TryGetNodeId 및 역조회 사전도 제거했다. NodePicker·Display의 내부 호출은 마스크 인자 없이 `TryPickNode(Ray, float pickDistance, out int)`를 사용한다. 거리 제한·가장 가까운 바닥·비활성 Collider 제외는 유지한다. 건물/아이콘 Collider는 선택 대상으로 등록하지 않는다.

노드 루트의 `StageNodeViewParts`가 네 직접 자식 참조와 규칙을 소유한다. DB는 원본 프리팹을 검증하며 런타임은 복제본의 참조를 읽는다. 기존 IconRoot는 유지하고 CurrentMarkerAnchor·Highlight를 제거했다. 캐릭터는 노드 루트를 부모로 쓰고, SC-1의 SO 표시 보정과 이동 계약은 유지한다. 운영 SO의 제거 필드와 기존 노드 프리팹 연결을 코드와 함께 반영해야 한다.

Core·Buffer·Factory·Framework 서비스 등록과 카메라 코드는 변경하지 않았다. 노드 Layer는 선택 필터에서 빠졌지만 카메라 Culling Mask의 표시 규칙은 별개로 유지된다.

## 검증과 후속

실행 증거와 남은 항목은 검사 후속 정리 계획의 N-1 결과(옛 N-1 계획 실행 기록)를 원본으로 본다. 이 메모는 발송 기록이 아니며 커밋·push는 사용자가 한다.

## 2026-10-27 덱 편집 팝업 연결 필수화 (R-b)

- 근거: [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(옛 필수 의존 설계) Q-R3 A(사용자 승인), [검사 후속 정리 계획 Phase R-b](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) TP-6.
- 바뀐 줄: `StageMapViewSystem.PostBindService`의 덱 편집 Factory 준비(용근님 `38869ec1`). 전에는 `inGame.CoreGroup?.Deck`이 없으면 팝업을 만들지 않았다. 지금은 `inGame.CoreGroup.Deck`으로 늘 만들고, 시험은 새 입구 `BindDeck(DeckEditPopupViewFactory)`로 먼저 넣을 수 있다. 덱 Factory가 없으면 `Initialize`·씬 연결이 "덱 편집 팝업"을 이유로 거부한다. Scope는 덱 편집 컨트롤러를 늘 만든다.
- 바뀌지 않은 것: `DeckEditPopupViewFactory`·`DeckEditPopupController`·`DeckEditUI`, Core `IRunDeckService`, 운영 조립(이미 Core 그룹에 덱 서비스가 있음). Factory 안의 `_faults != null &&` 갈래(`DeckEditPopupViewFactory.cs:27`)는 이번 범위 밖으로 남겼다.
- 상태: 문서 준비, 미전달.
