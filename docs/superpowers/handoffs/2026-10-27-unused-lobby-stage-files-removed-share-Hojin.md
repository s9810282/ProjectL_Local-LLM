# 참조 없는 로비·스테이지 맵 파일 셋 삭제 — 공유

- 작성자: 호진(이호진)
- 날짜: 2026-10-27
- 작성: 호진(Claude 작업), 미전달
- 대상: 용근님(지운 파일에 용근님 줄 2줄), 예전 `Lobby.unity`를 가진 원격 브랜치 작업자
- 출처: 10-26 Lobby·StageSelect 원칙 검사 6번 C-3, 사용자 결정 Q-C3·Q-C4 A(10-27). [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(옛 작은 정리 설계 §4), [계획](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) Phase C-3.
- 상태: 삭제 반영, 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`b1dd4962`, 10-27)

## 지운 파일(각 `.meta` 포함)

| 파일 | 왜 쓰이지 않았나 |
|---|---|
| `ViewSystem/Lobby/LobbyDepartureText.cs` | 10-20 `3bc33b20`(「로비에 있는 Text 임시 조치」)에서 연결을 뺀 뒤 참조 없음. **용근님 2줄**(10-20 시작 덱 거절 문구 `InvalidInitialDeck`)이 있었음 |
| `GlobalSystem/UISystem/Screens/Lobby/LobbyDepartureStatusUI.cs` | 같은 커밋 뒤 참조 없음. GUID `3f950804a558444baf8e7d9610ed01fb` |
| `ViewSystem/StageMap/StageMapCanvasOrder.cs` | 10-26 `ee55d650`에서 만들었지만 생성하는 코드가 어느 브랜치에도 없음. 같은 커밋의 `StageMapCanvasView`가 Canvas를 맡음. GUID `b50eba566b524ff2b58112421a98a144` |

지우기 전 HEAD·작업 트리의 코드·씬·프리팹·에셋 참조(이름·GUID)가 0임을 확인했다.

## 주의 — 예전 `Lobby.unity`

아래 원격 브랜치의 `Lobby.unity`는 10-20 임시 조치 **이전** 판이라 `LobbyDepartureStatusUI` GUID를 참조한다. 그 씬을 그대로 가져오면 Missing Script가 된다. 지금은 모두 로비를 바꾼 새 커밋이 없다(10-27 확인).

- `8-map1-라이팅-텍스쳐-수정`, `AJRLastDance`, `feature/Hojin/Lobby`, `feature/Hojin/Terrain_Tornado`, `feature/Hojin/View_Refactory`, `feature/Yonggeun/Ref-EventPresenter`

## 출발 안내를 다시 만들 때

출발 거절 사유 표시는 지금 없다(10-20부터 같음). 다시 만들 때는 UI 규약(FW-202)대로 등록된 로비 화면(`LobbyCanvasView`)의 자식 부품으로 두고, 출발 Presenter는 좁은 표시 계약 하나로 문구만 넘기며, 문구는 요청의 질문(`IsPending`·`IsRejected`·`IsFaulted`, 사유 `Result.FailureReason`)으로 고른다. 지운 문구는 git 기록(`45b20161` 판)에서 꺼낼 수 있다.
