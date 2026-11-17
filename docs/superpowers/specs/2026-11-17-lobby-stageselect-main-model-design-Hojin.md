# 로비·스테이지 선택 메인 캐릭터 모델 설계

- 작성: 호진 요청으로 Claude가 씀(2026-11-17)
- 상태: 설계 2판(2026-11-17 Codex 정적 검수·보완, §11). **방향 확인(2026-11-17 사용자 "새 프리팹으로 해서 조절하는게 맞다"): MM1 새 전투 외형 프리팹을 쓰고, 보정 값은 새 모델 기준으로 다시 맞춘다.** **결정 완료(2026-11-17 사용자 "권장안대로 하고 Task 계획 써줘" = Q-MM1~Q-MM4 모두 A), 유지.** Task 계획: [lobby-stageselect-main-model-plan](../plans/2026-11-17-lobby-stageselect-main-model-plan-Hojin.md). MM-a·MM-b 코드 반영·정적 검수 완료 뒤 사용자가 Unity 컴파일·재생 확인을 답했다. MM-c 문서·통합 정적 검수 완료, 실제 화면 보정 대기(계획 §7.4). Codex 빌드 실행은 없다.
- 앞선 설계: [로비 메인 선택 View 연결 설계](2026-11-16-lobby-main-selection-view-design-Hojin.md) Q-LM5에서 "판 위 캐릭터 모습 바꾸기"를 범위 밖(A)으로 미뤘다. 이 문서가 그 B다.

## 1. 요구와 범위

- 요구(사용자): 로비에서 메인 캐릭터를 바꿀 수 있으니, 로비와 스테이지 선택에서 보이는 말(캐릭터 모델)도 고른 메인의 모델로 바뀌어야 한다.
- 범위
  - 로비: "다음 Run 메인으로 선택"을 누르는 순간 판 위 캐릭터 모델을 고른 메인으로 바꾼다.
  - 스테이지 선택: 지금 Run의 메인 모델로 현재 위치 말을 그린다. 전투에서 돌아와도 같다.
  - 두 화면의 위치·크기·방향을 보정하는 구조와 이동(말이 판·노드를 걷는 것)은 유지한다. 보정 값 자체는 새 모델 기준으로 맞춘다(Q-MM2 A).
- 범위 밖: 전투 화면, 메인마다 다른 로비·맵 전용 모델이나 동작, 메인 선택 규칙(후보·기본 메인은 Core가 원본, 로비 메인 선택 설계 LM1 그대로).
- **메인 변경은 로비에서만 가능하고 스테이지 선택 덱 편집에서는 고정한다(2026-11-17 사용자 재확인).** 현재 UI의 메인 교체 금지를 유지하며, 덱 편집 메인 변경 기능·확정 직후 갱신 알림은 추가하지 않는다.

## 2. 착수 전 코드에서 확인한 사실 (2026-11-17)

| 항목 | 사실 |
|---|---|
| 로비 모델 | `LobbyViewScene.Start`가 `LobbyCharacterVisual.Create(Root.WorldRoot, _services.Models.CharacterPrefab, …)`로 한 번 만든다. 프리팹은 로비 DB `LobbyModelData.CharacterPrefab` 하나다. 메인 선택과 연결이 없다 |
| 로비 모델 구조 | 이동 루트(`LobbyCharacterRoot`, 입력·말 이동이 쥠) → 보정 루트(`LobbyCharacterVisual`, 로비 DB 보정) → 프리팹 복제본. 복제본만 바꾸면 이동·보정은 그대로다 |
| 로비 메인 선택 | 원본은 `LobbyMainSelection`(연결마다 하나, LM1). `Select(key)`만 값을 바꾸고, 부르는 곳은 덱 Presenter의 선택 버튼 하나다. 출발 중에는 `IsLocked`라 바뀌지 않는다 |
| 로비 외형 조회 | `LobbyViewServices`가 이미 `Func<EntityKey, UnitVisualData>`(View DB `UnitVisualTable.GetUnitVisual`)를 덱 묶음에 넘긴다. 후보 카드의 이름·아이콘이 여기서 온다 |
| 스테이지 선택 모델 | `StageMapViewDisplay`가 Show 때 `mapData.CurrentMarkerPrefab`(맵 표시 DB)을 보관하고, `StageCurrentMarkerView.ShowAt`이 처음 한 번만 복제한다. 그 뒤로는 같은 복제본을 노드 사이로 옮긴다. 다음 Show(구조 교체)는 마커를 새로 만든다 |
| 스테이지 선택 조립 | `StageMapViewSystem.PostBindService`가 `StageMapViewSceneServices`를 만든다. 여기서 Buffer와 View DB를 이미 받는다. Factory 변경이 필요 없다 |
| 지금 Run의 메인 | `IRunDeckReadSink.TryGetCurrent`의 `RunDeckSnapshot.MainId` → `ICharacterBufferReadSink.TryGetOwned` → `CharacterKey`. 보유 목록 전체의 메인 수는 확정 덱의 메인을 뜻하지 않는다. Core 보유 계약은 서로 다른 메인 보유를 허용한다 |
| 스테이지 메인 변경 금지 | `DeckEditUI.CanChangeSlot`이 메인 슬롯의 변경 허용을 검사한다. `StageSelect.unity`와 `Prefabs/UI/Deck/DeckEditUI.prefab`의 `allowMainCharacterChange`는 모두 0이다. 내부 Draft/Core가 일반적인 교체를 지원한다는 이유로 현재 UI에서도 교체 가능하다고 해석하지 않는다 |
| 두 화면의 지금 프리팹 | `LobbyModel_Default.characterPrefab`과 `StageMapView_Default.currentMarkerPrefab` 모두 옛 `Prefabs/Characters/Colored/Playerable/Akimbo.prefab` |
| 전투 외형 프리팹 | 실제 후보는 Core `ReadMainOptions` 응답을 따른다. 현재 운영 확인 대상은 Akimbo(10)·Sniper(12)이고 각각 새 `Prefabs/Characters/New/Playerable/`의 외형을 쓴다. 남아 있는 MainCharacter(14) 외형 행이 Akimbo와 같은 프리팹을 가리켜도 `PlayerMainDefinitionPolicy`가 그 키의 플레이어 정의를 금지하므로 후보로 강제 추가하지 않는다 |
| 복제본 정책 | `ChessPieceClone.Prepare`가 루트 자세·배율 1, 자식 레이어 0, Collider 끔, Animator 루트 모션·이벤트 끔. 새 프리팹 루트 크기 0.7·레이어 6은 이 정책이 덮는다 |
| 옛·새 프리팹 모델 | 옛 `Colored/Playerable/Akimbo.prefab` = `Resources/Characters/Colored/Models/Playerble/Akimbo_Colored_01.fbx`, 새 Akimbo = `Resources/Characters/New/Playerable/Akimbo/Akimbo.fbx`. Idle 컨트롤러는 옛·새 Akimbo가 같은 것(`5df8bba2…`), Sniper는 자기 컨트롤러 |
| 새 프리팹의 전투 부품 | 새 Akimbo·Sniper에 `CombatAnimationEvents`·`CombatUnitRagdoll`·몸 상자 Collider·총구 지점이 있다. 프리팹에 Rigidbody는 없고 랙돌은 이미 `m_Enabled: 0`이다. `CombatUnitRagdoll.Begin`이 물리를 만들고 켠다. Q-MM3 A는 현재 비용 제거가 아니라 복제본에서 꺼짐을 보장하는 정책이다. `CombatAnimationEvents`는 옛 프리팹에도 있었다 |

이 사실에서 나오는 것:

- 바꾸면 **기본 메인도 모습이 바뀐다.** 지금 두 화면은 옛 Akimbo를 쓰고, 전투 외형은 새 Akimbo다. 로비 보정(2.15배)·맵 보정(1.07배)은 옛 모델 기준이라 다시 맞춰야 할 수 있다.
- 새 프리팹은 Idle 컨트롤러가 붙어 있어 로비·맵에서도 Idle이 돈다. Akimbo는 옛 프리팹과 같은 컨트롤러다.
- 새 모델의 재질·셰이더(전투용 Cartoon 재질과 외곽선 패스)가 로비·맵 조명에서 어떻게 보이는지는 코드로 알 수 없다. Unity 확인 대상이다.
- 전투 프리팹을 고치면 로비·맵 말도 함께 바뀐다(공유 경계, §6). 예: 전투 쪽이 프리팹에 매 프레임 도는 부품을 더하면 로비·맵에서도 돈다.

## 3. 원본 표 (SSOT)

| 사실 | 원본(한 곳) | 읽는 곳 |
|---|---|---|
| 다음 Run 메인 | `LobbyMainSelection.Selected` | 로비 출발 흐름, 로비 판 위 모델(새로) |
| 지금 Run 메인 | 확정 `RunDeckSnapshot.MainId`, 해당 ID의 보유 기록 `CharacterKey` | 스테이지 선택 말(새로) |
| 메인의 모델 프리팹 | View DB `UnitVisualData.Prefab`(전투와 같은 행) | 로비·스테이지 선택 |
| 화면별 위치·크기·방향 보정 | 로비 `LobbyCharacterPresentationData`, 맵 `StageMapViewData.Marker*` | 그대로 |
| 복제본 표시 정책 | `ChessPieceClone.Prepare` | 그대로 |

로비 DB `CharacterPrefab`과 맵 DB `CurrentMarkerPrefab`은 이 설계 뒤 읽는 곳이 없어진다(Q-MM1).

## 4. 설계

### MM1 모델 원본은 전투 외형 표

메인 키 → `UnitVisualData.Prefab`. 로비·맵 전용 모델 칸을 두지 않는다. 메인이 늘어도 전투 외형 행만 넣으면 두 화면이 따라온다.

### MM2 로비: 선택하면 안쪽 모델만 갈아 끼움

- `LobbyCharacterVisual`에 `ShowModel(GameObject prefab)`을 둔다. 보정 루트를 잠시 비활성화한 상태에서 이전 복제본을 비활성화·해제하고 새 모델에 `ChessPieceClone.Prepare`를 적용한 뒤 기존 활성 상태를 복원한다. 같은 프리팹이면 아무것도 안 한다. 이동 루트·보정 루트와 Transform은 보존한다. 실패 정리·캐시 게시·중도 폐기 계약은 계획 §1.1을 따른다.
- `Create`는 프리팹을 받지 않고 루트만 만든다. 첫 모델은 `LobbyViewScene.Start`가 `ShowModel(지금 선택의 프리팹)`으로 넣는다. 이 최초 조회·생성이 실패할 때 아직 씬에 인계되지 않은 로컬 소유자도 반드시 정리한다.
- 알리는 길: `LobbyMainSelection`에 받는 쪽 하나를 한 번 연결한다(`BindModelDisplay(ILobbyMainModelDisplay)`, 이미 있는 `BindDepartureHold`와 같은 꼴). `Select`가 값을 바꾼 뒤(`true`일 때만) `display.ShowMain(key)`를 부른다. 이벤트·Tick·매 프레임 비교는 두지 않는다.
- 받는 쪽은 `LobbyViewScene`. 키 → 프리팹은 기존 `Func<EntityKey, UnitVisualData>`를 씬 묶음(`LobbySceneServices`)에도 넘겨서 읽는다. 씬이 아직 캐릭터를 만들기 전이거나 이미 정리했으면 아무것도 안 한다(시작할 때 `Selected`를 읽으므로 놓치지 않는다).
- 실패(복제 예외)는 선택 버튼 입력 콜백 안에서 나므로 덱 팝업의 기존 입력 오류 경계(`LobbyDeckPopupController`가 감싼 `FaultReportingUIInputReceiver`, 로비 2차 R2-a)로 F1에 간다.

### MM3 스테이지 선택: Show 때 지금 Run 메인을 읽음

- `StageMapViewSystem`이 좁은 Run 덱/캐릭터 ReadSink와 외형 조회 대리자를 연결해 `Func<GameObject> readMainModel`을 만들고 `StageMapViewSceneServices`에 넣는다. 내용: 확정 덱의 `MainId`로 보유 기록을 조회해 `CharacterKey`를 얻고 `GetUnitVisual(key).Prefab`을 검사한다. 덱 없음·대상 없음·역할 불일치는 구성 오류이며 기본 모델로 대체하지 않는다. SceneServices는 대리자를 받아 보관만 한다.
- `StageMapViewScene.ShowNodes`가 이 값을 `Display.Show`에 넘기고, 디스플레이는 `mapData.CurrentMarkerPrefab` 대신 보관한다. 새 인자는 `DisplayIdentity`의 일반/이동 중 비교에도 포함한다. 같은 맵이라도 다른 프리팹을 전달한 경우 기존 교체 경로를 타며, 이동 중 구조 교체 금지는 유지한다.
- 읽는 때는 `ShowNodes`뿐이다. 현재 운영 UI는 스테이지 메인 변경을 막으며, 새 Run·전투 후 복귀의 새 연결에서 다시 읽는다. 이는 Core의 모든 보유/덱 계약이 메인 교체를 금지한다는 뜻이 아니다. Tick 감시나 덱 편집 확정 알림은 추가하지 않는다.

### MM4 검사

- 외형 표의 `Prefab`은 칸이 비어도 되는 값이다(예: Shielder). 그래서 메인 모델 조건은 쓰는 쪽에서 본다.
  - 로비: 연결 시작 때 후보 카드를 만드는 `LobbyMainCards.Build`(이미 아이콘을 검사하는 곳, 이전 연결을 떼기 전)에서 후보마다 프리팹이 있는지 본다. 없으면 후보 키를 담은 예외.
  - 스테이지 선택: `readMainModel`이 프리팹이 없으면 메인 키를 담은 예외.
- 지금 맵 DB가 마커 프리팹에 하던 검사(프리팹 에셋·켜진 루트·Renderer 있음)는 공통 `MainModelPrefab.Require`로 모으고 그 안에서 `ViewPrefabRules`를 재사용한다. 로비 후보 카드는 이미 조회한 외형 행을 넘겨 중복 조회하지 않는다. 외형 행 없음은 기존 Query의 `KeyNotFoundException`, 프리팹 구성 문제는 키를 담은 `InvalidOperationException`으로 오류 경계에 전달한다.

### MM5 옛 칸 정리 (Q-MM1)

- 권장: `LobbyModelData.CharacterPrefab`·`StageMapViewData.CurrentMarkerPrefab`과 SO의 직렬화 칸을 코드에서 지운다. 에셋 파일은 지우지 않는다(남은 YAML 값은 Unity가 다음 저장 때 버림). 옛 `Colored/Playerable/Akimbo.prefab`은 참조가 줄어도 지우지 않는다.
- 남겨 두면 "어느 모델이 보이나"의 원본이 둘이 된다.

## 5. 수명 표

| 때 | 로비 | 스테이지 선택 |
|---|---|---|
| 연결 시작 | 선택(기본 메인) → 후보 프리팹 검사 → 씬 Start에서 지금 선택 모델 | Show에서 지금 Run 메인 모델 |
| 선택 버튼 | 값 바꿈 → 안쪽 모델 교체 | 해당 없음 |
| 출발 중 | 선택이 잠겨 모델도 안 바뀜 | 해당 없음 |
| 전투에서 돌아옴 | 해당 없음 | 새 연결의 Show에서 다시 읽음 |
| 연결 해제 | 이동 루트째 지움(지금과 같음) | 마커째 지움(지금과 같음) |

## 6. 변경 독립성 점검

| 변경 | 고칠 곳 | 이해할 곳 |
|---|---|---|
| 메인 후보 추가 | 전투 외형 행(+ 프리팹) | 두 화면 코드 무변경 |
| 로비 보정 값 조정 | 로비 DB 에셋 | 무변경 |
| 맵 말 이동 방식 변경 | `StageCurrentMarkerView`·디스플레이 | 메인 읽기와 무관 |
| 메인을 읽는 원본이 바뀜(예: Run 사실 칸이 생김) | 스테이지의 `readMainModel` 조회 연결 | 디스플레이·로비 선택 무변경 |

남는 공통 경계: `ChessPieceClone.Prepare`(두 화면 공유)와 전투 외형 표(전투와 공유). 프리팹이 바뀌면 전투·로비·맵을 함께 봐야 한다.

## 7. 원칙 점검

- **SSOT**: 모델 = 외형 표 하나, 다음 Run 메인 = 선택 원본, 지금 Run 메인 = Buffer. 로비·맵에 메인 키를 따로 보관하지 않는다(필요할 때 읽음).
- **SOLID**: SRP는 선택/모델 교체/조립 분리, OCP는 후보별 코드 분기 없는 DB 조회, LSP는 기존 선택 잠김·수명 계약 유지, ISP는 선택 읽기 계약·조회 대리자만 전달, DIP는 System/Scope 조립에서 의존성 주입으로 점검한다. 상세 변경 경계·대조 항목은 계획 §4.1에 둔다.
- **JMH/FW**: 새 서비스·모듈 등록 없음, Factory 무변경. 기존 Buffer Context와 View DB에서 좁은 읽기 의존성만 주입한다. 씬 전체 검색이나 서비스 위치 검색은 추가하지 않는다. 프리팹 Renderer 검사·복제본 컴포넌트 정책에는 기존 `GetComponent(s)InChildren`을 사용한다.
- **성능**: 교체는 버튼 누름 때 한 번, 맵은 Show 때 한 번. 매 프레임 할 일 없음.
- **영역**: 로비·스테이지 선택 View와 그 View DB, 공통 `ChessPieceClone`의 복제본 정책을 수정한다. 전투 외형 표·원본 프리팹·`CombatUnitRagdoll` 코드는 읽기/타입 참조만 하며 수정하지 않는다.

## 8. Unity 확인 (사용자) · 에셋 조정 (Codex 몫)

- 사용자 확인: 로비에서 Core가 제공한 후보(Akimbo·Sniper)를 번갈아 골라 모델이 바뀌는지, 말 이동·시설 진입이 그대로인지, 출발 뒤 스테이지 선택 말이 고른 메인인지, 전투 뒤 돌아와도 같은지, Idle이 도는지, 새 모델의 재질이 로비·맵 조명에서 괜찮은지, 크기·발 위치가 판·노드에 맞는지. 스테이지 메인 슬롯의 교체 금지와 서브 편집 후 모델 유지도 확인한다.
- 에셋 조정(Codex, 승인 뒤): 새 모델 기준으로 로비 보정·맵 보정 값 다시 맞추기(Q-MM2).

## 9. 결정 결과 (Q-MM1~Q-MM4 A 유지)

- **Q-MM1 옛 모델 칸** — A(권장): 로비 `CharacterPrefab`·맵 `CurrentMarkerPrefab` 코드 칸을 지운다(에셋 파일은 둠). B: 칸을 남기고 쓰지 않는다(원본이 둘로 보임).
- **Q-MM2 보정 값** — A(권장): 화면마다 지금처럼 보정 하나를 모든 메인에 같이 쓴다. 메인끼리 크기가 많이 다르면 그때 B로. B: 외형 표나 로비·맵 DB에 메인마다 보정 칸을 둔다(범위 커짐).
- **Q-MM3 복제본의 랙돌 부품** — 1판 질문(Rigidbody를 kinematic으로)은 프리팹에 Rigidbody가 없어 필요 없어졌다(§2). A(선택): `ChessPieceClone.Prepare`가 복제본의 `CombatUnitRagdoll`을 끈다(`enabled = false`). 현재 원본에서도 이미 꺼져 있으나 복제본의 표시 정책을 명시한다. B: 원본의 비활성 설정만 따른다. A를 유지하며 원본·전투 랙돌 구현은 변경하지 않는다.
- **Q-MM4 같은 모델일 때** — A(선택): 두 허용 키가 같은 프리팹을 가리키거나 같은 프리팹이 다시 요청되면 복제하지 않는다. 현재 퇴역한 MainCharacter(14)를 사용자 확인 후보로 넣지 않는다. B: 선택마다 늘 다시 만든다(Idle이 처음부터 다시 돎).

## 10. Phase (안)

- **MM-a 로비**: `LobbyCharacterVisual.ShowModel`, 선택 원본의 받는 쪽 연결, 씬 묶음에 외형 조회, 후보 프리팹 검사, (Q-MM1 A면) 로비 DB 칸 삭제.
- **MM-b 스테이지 선택**: `readMainModel`, 씬 묶음·Show 인자, 검사, (Q-MM1 A면) 맵 DB 칸 삭제.
- **MM-c 문서·검수**: 로비 메인 선택 설계 Q-LM5에 이어짐 표시, Codex 문서·코드 검수, 보정 값 조정은 Codex에 넘김.

각 Phase는 컴파일 미확인으로 보고하고, 새 시험 코드는 만들지 않는다. 기존 시험이 바뀐 생성자 때문에 컴파일이 깨지면 호출만 맞춘다.

## 11. 착수 전 검수 메모 (2026-11-17)

- 사용자 요청으로 Codex가 계획을 보완하면서 이 문서의 근거·수명·조립 설명도 맞췄다. 지적별 수용/반려와 현재 코드 근거는 **계획 §7.1**이 원본이다. 보유 메인 수와 확정 덱 메인을 구분하고, 초기 생성 실패 정리·비활성 교체·프리팹 표시 동일성·현재 후보/랙돌 상태를 보완했다.
- 스테이지 덱 편집에서 메인을 바꿀 수 있다는 검수 중 설명은 실제 UI 설정을 확인해 정정했다. 사용자 정책과 현재 씬/프리팹은 로비에서만 메인을 바꾸는 흐름이다. 해당 기능 확장이나 별도 결정 질문은 남기지 않는다.
- 이 착수 전 검수 시점에는 코드·에셋 구현을 시작하지 않았다. 이후 구현 상태는 아래 Phase 기록과 연결 계획을 따른다.

### 11.1 MM-a 로비 구현 (2026-11-17)

- 사용자 위임으로 Codex가 MM-a의 a.1~a.10을 반영했다. 최초 표시·선택 알림·모델 교체·후보 검사·로비 고정 모델 칸 제거가 연결됐다. 생성 실패 정리와 비활성 교체를 구현하고, 별도 검수에서 지적된 같은 프리팹 재진입 순서도 보완했다.
- 지적 처리·정적 검증의 원본은 계획 §7.2다. 이 단계에서는 씬·프리팹·SO 에셋과 MM-b·MM-c를 변경하지 않았다. **컴파일 미확인, Unity 재생·사용자 인수 대기.**

### 11.2 MM-b 스테이지 선택 구현 (2026-11-17)

- 다음 단계 진행 지시로 Codex가 b.1~b.7을 반영했다. 확정 Run 덱의 `MainId`→보유 기록의 `CharacterKey`→외형 표 프리팹을 `ShowNodes`에서 읽는다. 표시가 재사용되는 일반/이동 중 두 비교에 프리팹을 포함했고 맵 DB의 고정 마커 칸은 코드에서 제거했다.
- 선택 규칙·노드 이동·원본 에셋은 유지한다. 별도 코드 검수에서도 수정이 필요한 정적 결함은 발견되지 않았다. 검수·검증 기록은 계획 §7.3을 따른다. **컴파일 미확인, Unity 재생 미확인.** MM-c 보정과 전체 마무리는 미착수다.

### 11.3 MM-c 통합 정리·표현 보정 (2026-11-17, 실제 화면 보정 대기)

- MM-a·MM-b의 지적 처리와 검증 기록을 계획 §7.4에 모으고, 앞선 로비 메인 선택 설계 Q-LM5에 이번 작업의 이어짐을 표시했다. 새 기능 결정이나 화면별 공통 보정 원칙 변경은 없다.
- 사용자는 MM-a·MM-b를 Unity에서 확인했고 로비를 실행해 두겠다고 답했다. Codex의 컴파일 미확인 기록과 구분한다. 보정 값·화면 관찰 결과·남은 사용자 확인의 원본은 계획 §7.4다.
- 최종 통합 읽기 전용 검수 지적은 없다. 마지막 직접 관찰은 Boot 비재생 편집 모드여서 두 메인의 실제 화면 비교와 보정 값 변경은 아직 하지 않았다. c.1·c.2 완료와 MM-c 전체 완료를 구분한다.
