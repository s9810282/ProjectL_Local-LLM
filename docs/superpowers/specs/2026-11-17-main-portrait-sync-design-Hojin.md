# 메인 초상화 동기화 설계 — 2026-11-17

## 승인 범위

사용자는 로비·스테이지 선택·전투에서 선택한 메인의 초상화를 표시하는 방식과 Codex 구현을 승인했고, 성능·프레임 영향도 확인하도록 요청했다. MM-a/b의 모델 선택과 같은 캐릭터를 표시한다. MM-c의 실제 모델 크기·발 위치 보정 대기는 별도다.

## 원본과 수명

- 로비: `LobbyMainSelection.Selected`. 카드 호버·정보 고정은 바꾸지 않고 메인 선택 확정만 반영한다.
- 스테이지 선택·전투: `IRunDeckReadSink`의 `RunDeckSnapshot.MainId` → `ICharacterBufferReadSink.TryGetOwned` → `CharacterKey`. 능력치 게시나 전투 개체 생성 완료를 기다리지 않는다.
- 이미지: 기존 View DB `UnitVisualData.InfoIcon`. 카테고리는 시너지 표시만 맡고, 아킴보 기본 이미지로 대신하지 않는다.
- 기존 `UIId.MainCharactor`/`GlobalUIBase` 한 개와 얼굴 Image를 재사용한다. `GlobalUICanvas.prefab`의 기존 `portrait` 필드가 테두리 Image를 잘못 가리켜, 실제 얼굴인 자식 `Portrait_Head` Image로 참조 한 줄을 바로잡는다. 새 Canvas·UI 등록·레이아웃·씬 에셋 변경은 없다.
- `GlobalUIPresentation.ShowLobby/ShowStageSelect/ShowCombat`이 초상화의 화면 모드를 지정한다. 기존 UISystem 개폐·이력·부모 정책은 유지한다.
- 로비 Scope는 `Func<EntityKey>` 읽기 연결을 등록하고 `IDisposable`을 보관한다. 선택값 사본은 만들지 않는다. Scope 폐기 시 연결을 해제하며, 이전 연결의 해제가 새 연결을 지우지 않도록 연결 객체의 동일성을 검사한다.
- 초기 화면 정책 지정 전, 로비 연결 전·해제 후, Run 생성 전·리셋 중에는 초상화를 비운다. 준비된 Run에 메인 보유 기록이 없거나 역할이 Main이 아니면 오류다. View DB·InfoIcon·필수 Image 누락은 대상 키/필드를 담은 예외로 기존 UI 오류 경계에 전달한다.

## 책임과 조립

`MainCharacterPortrait`는 이미지·원본 선택·표시 캐시를 담당하는 내부 클래스다. `MainCharactorUI`는 기존 GetUIData/BindUIService에서 좁은 조회 계약을 주고 기존 LateUpdate에서 갱신한다. HP·골드·시너지 계산은 옮기지 않는다.

로비 조립은 `LobbyViewSystem.PostBindService` → `LobbyViewServices.BindMainPortrait` 대리자 → `LobbyViewScope` 수명으로 연결한다. 기존 `GlobalSystemGroup.GlobalUIPresentation` 경로를 사용하며, Global 하위 서비스를 루트에 재등록하지 않는다. Global UI는 로비 구체 클래스에 의존하지 않는다.

## 성능 계약

- 새 Update/LateUpdate, 코루틴, 이벤트 구독, 씬 검색, Resources.Load, 텍스처 복제는 추가하지 않는다.
- 로비 평상시: 읽기 대리자 1회와 EntityKey 값 비교. Run 평상시: 현재 덱 참조와 RosterRevision 비교. 초상화 경로에서 목록 순회·LINQ·정상 프레임별 객체 생성이 없다.
- 덱/로스터 변경 때만 보유 기록 Dictionary 조회. 캐릭터 키가 달라질 때만 View DB 조회. Sprite/활성 상태도 값이 다를 때만 대입한다.
- 연결 객체·읽기 대리자는 로비 연결 때만 생성한다. 메모리 사용량은 연결 및 표시 캐시의 상수 크기다. 에셋의 기존 Sprite 참조를 사용한다.
- 이미지 변경은 uGUI의 dirty/rebuild를 유발할 수 있다. 비용이 0이라고 주장하지 않는다. 해당 프레임의 Canvas 규모·텍스처 배칭·기기 성능은 실측 대상이다. 기존 HP/골드 갱신 전체의 비용까지 개선했다고 주장하지 않는다.

## 구조 검수

- SSOT: 로비 선택/Run 덱/InfoIcon을 그대로 읽는다. 캐시는 마지막 표시 결과이며 게임 상태가 아니다.
- S: 초상화 구현, UI 조립, 로비 연결 수명을 구분한다.
- O: 이미지 매핑 변경은 View DB에, 표시 변경은 초상화 클래스에 한정한다.
- L: 기존 UIBase 개폐 보장과 반복 해제를 유지한다.
- I: 로비에서는 EntityKey 읽기 대리자만, 초상화에서는 Character/RunDeck ReadSink와 UnitVisual query만 받는다.
- D: Global UI는 로비 구현체·Core 명령·InGame DB를 모르며 기존 서비스 경로로 조립한다.
- JMH/FW: 새 Framework 서비스/모듈이 아니다. 기존 UISystem 등록·Global Context·Scope 수명을 사용한다. 조립 변경은 기존 조립 지점에만 둔다.
- 기능 독립성: 이전에는 얼굴 매핑 변경이 HP·시너지 처리 내부 변경을 요구했다. 이후 얼굴 처리 변경은 `MainCharacterPortrait`에 머물며 HP·골드 계산을 읽거나 수정할 필요가 없다. 남는 공통 경계는 Main UI 갱신 호출과 화면 전환/로비 Scope 연결이다.

## 검증 경계

View 시험 코드·MSBuild·자동 EditMode는 작성/실행하지 않는다. 정적 diff·등록/호출 경로·에셋 참조·캐시 수명을 검수한다. Unity 컴파일·재생·Profiler 실측은 사용자 확인이며, 보고에 **컴파일 미확인**과 실측 여부를 분리한다.
