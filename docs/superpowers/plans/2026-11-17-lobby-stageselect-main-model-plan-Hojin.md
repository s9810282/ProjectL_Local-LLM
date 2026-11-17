# 로비·스테이지 선택 메인 캐릭터 모델 Task 계획

- 설계: [로비·스테이지 선택 메인 캐릭터 모델 설계](../specs/2026-11-17-lobby-stageselect-main-model-design-Hojin.md)(MM1~MM5, Q-MM1~Q-MM4 모두 A, 2026-11-17 사용자)
- 작성: 호진 요청으로 Claude가 씀(2026-11-17). 같은 날 사용자 요청으로 Codex가 계획·설계를 검수하고 보완했다(§7.1).
- 현재 상태: **MM-a 로비(§7.2)·MM-b 스테이지 선택(§7.3) 코드 반영·정적 검수 완료**. 2026-11-17 사용자가 MM-a·MM-b의 Unity 컴파일·재생을 확인했다고 답했다. **MM-c 문서·통합 정적 검수 완료, 실제 화면 보정 대기(§7.4)**. Codex 빌드 실행은 없으며 사용자 확인과 직접 관찰을 구분한다.
- 진행 규칙: Phase마다 코드·문서 검수 후 멈추고 보고한다. "진행해"는 그 Phase의 Task만이다. 새 시험 코드는 만들지 않고, MSBuild·EditMode를 자동 실행하지 않는다. 컴파일·재생은 사용자 Unity 확인이며, 미실행 보고에는 "컴파일 미확인"을 적는다. 커밋·push는 사용자가 한다.

## 0. 착수 전 확인

- `Assets/Tests`의 해당 생성자·속성 직접 참조는 2026-11-17 `rg` 재검색에서도 없었다(`new LobbyModelData(`·`new StageMapViewData(`·`LobbyCharacterVisual.Create`·`new LobbySceneServices(`·`new StageMapViewSceneServices(`·`new LobbyViewScene(`·`CharacterPrefab`·`CurrentMarkerPrefab`). 착수 때 삭제 필드의 직렬화 이름·Editor 문자열 접근과 `StageMapViewDisplay.Show` 호출도 다시 검색한다. 기존 시험 수정이 필요하면 AGENTS.md의 작성자·컴파일/실행 구분을 따른다.
- Factory 변경 없음. 의존성 취득은 기존 `PostBindService`, 로비 씬 묶음 연결은 `LobbyViewServices`, 선택 수신자 연결은 `LobbyViewScope`에 둔다. 기능 구현을 이 조립 지점에 넣지 않는다.
- MM-a·MM-b에서는 에셋 파일을 고치지 않는다. `LobbyModel_Default.asset`·`StageMapView_Default.asset`에 남는 옛 칸 값은 Unity가 다음 저장 때 버린다. MM-c의 보정 값 조정은 코드 연결 뒤 별도 인수한다.
- **메인 변경은 로비에서만 한다(2026-11-17 사용자 재확인).** `DeckEditUI.CanChangeSlot`은 메인 변경 옵션을 검사하며, 현재 `StageSelect.unity`와 `DeckEditUI.prefab`의 `allowMainCharacterChange`는 모두 0이다. 덱 내부의 일반적인 교체 계약을 실제 화면의 허용 동작으로 해석하지 않는다. 스테이지 덱 편집 코드·옵션·확정 알림은 이번 변경 대상이 아니다.
- 보유 목록과 확정 덱은 다르다. `Owned` 전체에 메인이 하나라는 조건을 새로 만들지 않고, **`RunDeckReadSink`의 `MainId` → `CharacterReadSink.TryGetOwned` → `CharacterKey`**를 사용한다. 기본 Stage·개발 진입도 `StartNewRun`의 초기 덱 설치 후 맵을 그리는 경로임을 확인했다.

## 1. 계획에서 정한 것 (TP, 사용자 확인 대상)

| TP | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 프리팹 검사와 Run 메인 조회를 `ViewSystem/Common/MainModelPrefab.cs`의 독립된 정적 함수로 둔다. 두 화면은 프리팹 검사를 공유하고, Run 조회는 스테이지 선택만 호출한다 | 조건(프리팹 에셋·켜진 루트·Renderer 있음)은 SSOT. 로비에는 Run 덱 의존성을 추가하지 않고 `Selected`만 읽는다 |
| TP-2 | `LobbyViewScene`이 생성자에서 선택 읽기 계약(`ILobbyMainSelectionReader`)을 받는다. 첫 모델은 `Start`에서 `Selected`로 넣는다 | 씬이 시작 순간의 선택을 직접 읽으면 받는 쪽 연결보다 먼저 선택이 바뀌어도 놓치지 않는다 |
| TP-3 | 받는 쪽 연결(`selection.BindModelDisplay(_scene)`)은 `LobbyViewScope` 생성자, `BindDepartureHold` 바로 다음 줄에서 한다 | 이미 있는 연결과 같은 자리·같은 꼴. 선택과 씬은 같은 연결 수명이라 풀 필요가 없다 |
| TP-4 | `StageMapViewDisplay.Show` 세 오버로드 모두 `Transform parent` 바로 뒤에 필수 `GameObject markerPrefab`을 받는다(선택적 `dropTargets`는 끝에 유지). 표시 동일성에도 이 참조를 포함한다 | 디스플레이는 모델의 출처를 모르고, 같은 표시를 재사용할 때 새 인자를 무시하지 않는다 |
| TP-5 | `ChessPieceClone.Prepare`가 복제본의 `CombatUnitRagdoll`을 끈다(Q-MM3 A). `ViewSystem/Common` → `ViewSystem/Combat/Display/Unit` 타입 참조 하나가 생긴다 | 현재 새 Akimbo·Sniper 프리팹은 이미 `m_Enabled: 0`이다. 현재의 매 프레임 비용 제거가 아니라 복제본 표시 정책을 명시하는 변경이다. 원본 프리팹·랙돌 코드는 수정하지 않는다 |
| TP-6 | 스테이지 선택은 `ShowNodes`마다 확정 덱의 메인 프리팹을 읽는다. 맵·노드 외형·마커 프리팹까지 같을 때만 기존 표시를 재사용한다 | 메인 ID/키 사본이나 Tick 감시는 만들지 않는다. 스테이지 덱 편집에서 메인을 바꾸는 기능도 추가하지 않는다 |
| TP-7 | 로비 후보 검사는 `LobbyMainCards.Build`(이전 연결을 떼기 전), 스테이지 선택 검사는 `ShowNodes` 직전에 한다. 준비·재그리기 중 실패는 각 기존 오류 경계(F1)에 전달한다 | 외형 누락을 기본 모델로 대체하거나 로그 후 정상 진행하지 않는다. Run 시작 전 `PostBindService`에서는 덱을 조회하지 않는다 |

### 1.1 생성·교체·오류 계약 (a.5·a.7 구현 기준)

- 첫 모델을 넣는 `ShowModel`이 `Create` 밖으로 이동하므로 **두 호출 사이에 소유권 공백이 없어야 한다.** `Start`는 반환된 `LobbyCharacterVisual`의 첫 모델 조회·생성을 `try/catch`로 감싸고, `_character`에 인계하기 전 실패하면 로컬 소유자를 `Dispose`한 뒤 예외를 전파한다. `canWork`가 false인 경우도 직접 정리한다. 인계 뒤에는 기존 Scope 정리가 책임진다.
- 교체는 이동 루트·보정 루트와 그 Transform을 보존한다. 보정 루트를 잠시 비활성화한 상태에서 이전 복제본을 비활성화·해제하고 새 복제본을 만들어 `ChessPieceClone.Prepare`를 적용한다. **정책 적용 전 새 모델이 활성 계층에 놓이지 않도록 한다.** 이동 루트나 원본 프리팹을 껐다 켜지 않는다. 정상 교체 뒤 보정 루트의 이전 활성 상태를 복원한다.
- 복제본 참조도 소유하고, `_modelPrefab`은 준비 성공 후에만 갱신한다. 실패 시 부분 생성물을 정리하고 성공한 모델처럼 캐시하지 않는다. `Destroy`가 지연되므로 이전 모델·실패한 모델은 먼저 비활성화한다. 플레이 중 `Destroy`, 비재생 중 `DestroyImmediate`를 사용하는 기존 정책을 유지한다.
- `Instantiate`·활성 변경 뒤 연결이 끝난 경우 폐기된 소유자에 새 모델을 게시하거나 다시 켜지 않는다. `Dispose`는 이동 루트·보정 루트·복제본·프리팹 캐시를 비우며 반복 정리가 안전해야 한다. `ShowMain`의 시작 전/정리 후 반환은 기존 설계의 정상 수명 처리다.
- 선택은 먼저 `Selected`에 기록되고 표시 실패는 기존 입력 오류 경계로 전파된다. 실패를 삼켜 출발을 계속하거나 별도 선택값으로 되돌리는 경로는 추가하지 않는다. 같은 키 선택은 기존 `Select == false`, 다른 키라도 같은 프리팹이면 `ShowModel`에서 복제를 건너뛴다.
- 조회 인자 null은 `ArgumentNullException`. 외형 행 없음은 기존 `GetUnitVisual`의 키를 담은 `KeyNotFoundException`을 유지한다. null 행·빈 프리팹·잘못된 프리팹 구성은 키·조건을 담은 `InvalidOperationException`. 모든 조회 예외를 포괄적으로 감싸지 않는다.

## 2. Phase MM-a 로비

| Task | 파일 | 할 일 |
|---|---|---|
| a.1 | 새 `ViewSystem/Common/MainModelPrefab.cs`(+ `.meta`) | `internal static class`. `GameObject Require(EntityKey key, Func<EntityKey, UnitVisualData> visuals)`가 조회한 행을 `Require(EntityKey key, UnitVisualData visual)`에 넘긴다. 두 번째 함수에 `RequirePrefabAsset`·`RequireActiveRoot`·Renderer 존재 검사를 모은다. a.9는 이미 읽은 행을 넘겨 중복 조회하지 않는다. 예외는 §1.1. (b.1에서 `FindRunMain` 추가) |
| a.2 | `ViewSystem/Common/ChessPieceClone.cs` | `Prepare` 끝에 복제본의 `CombatUnitRagdoll`마다 `enabled = false`. 문서 주석에 "전투 사망 부품은 끈다" 한 줄 |
| a.3 | `ViewSystem/Lobby/ILobbyMainModelDisplay.cs`(새, + `.meta`) | `internal interface ILobbyMainModelDisplay { void ShowMain(EntityKey key); }` |
| a.4 | `ViewSystem/Lobby/LobbyMainSelection.cs` | `_modelDisplay` 칸, `BindModelDisplay(ILobbyMainModelDisplay)`(null·두 번 연결은 예외, `BindDepartureHold`와 같은 꼴), `Select`가 값을 바꾼 뒤(`true`일 때) `_modelDisplay?.ShowMain(key)`. 연결 전이면 부르지 않는다(씬 `Start`가 읽음) |
| a.5 | `ViewSystem/Lobby/LobbyCharacterVisual.cs` | `Create(worldRoot, presentation)`은 루트·보정 루트만 만든다. `_visualRoot`·복제본·`_modelPrefab`을 소유한다. `ShowModel`은 같은 프리팹이면 반환(Q-MM4 A), 다르면 §1.1의 비활성 교체·실패 정리·성공 후 캐시 규칙을 따른다 |
| a.6 | `ViewSystem/Lobby/LobbySceneServices.cs`·`LobbyViewServices.cs` | 씬 묶음에 `Func<EntityKey, UnitVisualData> UnitVisuals`. `LobbyViewServices`가 이미 받는 `unitVisuals`를 넘긴다 |
| a.7 | `ViewSystem/Lobby/LobbyViewScene.cs` | 생성자에 `ILobbyMainSelectionReader selection`(null 검사), `ILobbyMainModelDisplay` 구현. `Start`에서 `Create`·최초 `ShowModel`·소유권 인계를 §1.1대로 처리하고 위치 설정·`Show` 뒤 기존 `requireRunning`/`canWork` 검사를 유지한다. `ShowMain(key)`는 `_character`가 없으면 반환, 있으면 `ShowModel(MainModelPrefab.Require(key, UnitVisuals))` |
| a.8 | `ViewSystem/Lobby/LobbyViewScope.cs` | 씬 생성에 `selection` 넘김, `BindDepartureHold` 다음 줄에 `selection.BindModelDisplay(_scene)`(TP-3) |
| a.9 | `ViewSystem/Lobby/Deck/LobbyMainCards.cs` | 후보마다 이미 조회한 `visual`을 `MainModelPrefab.Require(key, visual)`에 넘겨 검사(TP-7). 문서 주석에 프리팹 조건 한 줄 |
| a.10 | `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs`·`LobbyModelDataSO.cs` | `CharacterPrefab`·`characterPrefab`·`ValidateCharacterPrefab` 삭제(Q-MM1 A). 클래스 요약 문구 고침 |

**MM-a Unity 확인(사용자)**
- Ua-1 컴파일 오류 없음, Boot부터 로비 진입.
- Ua-2 로비 판 위 캐릭터가 기본 메인(Akimbo, 새 모델)으로 보이고 Idle이 돈다.
- Ua-3 Core가 제공한 현재 후보(Akimbo·Sniper)를 번갈아 선택 → 판 위 모델이 즉시 바뀐다. 선택 없이 카드 클릭·호버만 하면 모델은 바뀌지 않는다. `Unit_MainCharacter`는 플레이어 정의에서 퇴역한 키라 확인을 위해 후보에 추가하지 않는다. 동일 프리팹 건너뛰기(Q-MM4)는 정적 대조하고, 실제 허용 후보가 같은 프리팹을 공유할 때만 Idle 유지까지 확인한다.
- Ua-4 바꾼 뒤 시설로 말 이동·시설 진입이 그대로다.
- Ua-5 출발 중에는 시설 입력/팝업 입력 수명이 막혀 선택할 수 없다. 버튼이 화면에 보인다고 가정하지 않는다. 팝업 재개방·로비 재진입 시 모델이 각각 유지 선택·기본 선택과 일치한다.
- Ua-6 크기·발 위치는 옛 보정이라 어긋날 수 있다 → MM-c에서 Codex가 조정.
- Ua-7 다른 모델로 여러 번 교체해도 모델이 하나만 보이고 말 위치·방향이 유지된다. 복제본 Collider·루트 모션·애니메이션 이벤트·랙돌이 꺼져 있고, 원본 프리팹은 바뀌지 않는다. 이동·시설 진입 때 콘솔 오류가 없는지 함께 확인한다.

## 3. Phase MM-b 스테이지 선택

| Task | 파일 | 할 일 |
|---|---|---|
| b.1 | `ViewSystem/Common/MainModelPrefab.cs` | `EntityKey FindRunMain(IRunDeckReadSink deck, ICharacterBufferReadSink characters)`: `TryGetCurrent`의 `MainId`로 `TryGetOwned`를 조회하고 `CharacterKey` 반환. 인자 null 검사. 덱 없음·대상 없음·역할이 Main이 아님은 Run/인스턴스 식별자를 담은 구성 오류. 다른 보유 메인 수는 세지 않는다. 기본 키·첫 보유자 fallback 없음 |
| b.2 | `ViewSystem/StageMap/StageMapViewSceneServices.cs` | 생성자에 `Func<GameObject> readMainModel`을 받아 null 검사 후 `ReadMainModel`에 보관한다. 여기서 Buffer를 조회하거나 새 람다를 조립하지 않는다(설계 MM3 유지) |
| b.3 | `ViewSystem/StageMap/StageMapViewSystem.cs` | `PostBindService`에서 `buffers.RunDeckReadSink`·`buffers.CharacterReadSink`·`view.UnitVisualTableQuery.GetUnitVisual`만 지역 변수로 받아 조회 대리자를 한 번 조립해 b.2에 주입한다. 호출 때 `FindRunMain` → `Require`. 전체 resolver/Buffer Context나 나중에 비우는 `_view`를 캡처하지 않는다 |
| b.4 | `ViewSystem/StageMap/StageMapViewScene.cs` | `ShowNodes`가 `_services.ReadMainModel()`을 `Display.Show`에 넘김(TP-4·TP-6) |
| b.5 | `ViewSystem/StageMap/StageMapViewDisplay.cs` | `Show` 세 오버로드에 필수 `markerPrefab`(null이면 `ArgumentNullException`)을 전달하고 `_markerPrefab = markerPrefab`. 두 `DisplayIdentity` 생성 지점(일반/이동 중)에 같은 참조를 넣고 `Matches`에도 포함한다. 같은 맵에 다른 프리팹을 전달하면 기존 구조 교체 경로를 타며, 이동 중 교체는 기존 예외·lease 소유 순서를 유지한다. 새 메인 변경 기능이나 매 프레임 조회는 추가하지 않는다 |
| b.6 | `DataBaseSystem/Data/View/Table/StageMap/Item/StageMapViewData.cs`·`StageMapViewDataSO.cs` | `CurrentMarkerPrefab`·`currentMarkerPrefab`·검사 세 줄 삭제(Q-MM1 A). 보정 칸 툴팁의 "원본 프리팹" 문구를 "메인 캐릭터 모델"로 |
| b.7 | `ViewSystem/StageMap/StageCurrentMarkerView.cs` | 프리팹은 "DB 생성 때 검사 완료"라는 주석을 "ShowNodes의 메인 모델 조회에서 검사 완료"로 고친다. 보정 값 검사는 기존 맵 DB 책임으로 구분한다. 생성·이동 동작 변경 없음 |

**MM-b Unity 확인(사용자)**
- Ub-1 컴파일 오류 없음.
- Ub-2 로비에서 Sniper로 출발 → 스테이지 선택 말이 Sniper. Akimbo로 출발하면 Akimbo.
- Ub-3 노드 이동(점프 포함)·건물 앞 착지가 그대로다.
- Ub-4 전투 뒤 스테이지 선택으로 돌아와도 같은 메인.
- Ub-5 Play From Boot의 Stage 직접 시작·개발 진행 진입에서도 초기 Run 덱이 설치된 뒤 같은 메인이 표시된다. 보유 메인이 하나라서 성공한다고 판정하지 않는다.
- Ub-6 스테이지 덱 편집에서 메인 슬롯 변경은 계속 막혀 있고 서브 편집 뒤 모델은 유지된다. 메인 변경 옵션을 켜거나 새 갱신 알림을 추가하지 않는다.
- Ub-7 같은 표시 반복 갱신·노드 이동에서 마커가 불필요하게 재생성되지 않는다. 다른 프리팹 인자의 동일성 처리·이동 중 교체 금지는 §4.1의 정적 대조로도 확인한다.

## 4. Phase MM-c 문서·검수·보정 인계

| Task | 할 일 |
|---|---|
| c.1 | 설계 §11 구현 메모(계획과 다른 점, 컴파일 미확인), 로비 메인 선택 설계 Q-LM5에 "이어짐: 2026-11-17 메인 모델 설계" 한 줄 |
| c.2 | MM-a·MM-b 각 Phase 종료 검수 기록을 모으고 최종 코드·문서를 대조한다(사용자가 Codex에 넘김). 지적마다 수용·근거 있는 반려·사용자 판단 필요로 기록. 문서 검수 완료와 Unity 확인 완료를 구분 |
| c.3 | 보정 값 조정 요청(Codex 몫, 승인 뒤): `LobbyCharacterPresentation_Default`(offset·yaw·scale), `StageMapView_Default`(markerOffset·markerYaw·markerScale)를 새 모델 기준으로. Akimbo·Sniper 둘 다 자연스러운지 같이 보고, 하나로 안 맞으면 Q-MM2 B를 사용자에게 다시 묻는다 |
| c.4 | 보정 값 에셋을 바꾼 경우 `DataBaseAssetTests`를 사용자 Unity 확인 목록에 포함한다. 재질·조명·Idle·발 위치·말 이동의 실제 확인 결과와 정적 검수 결과를 따로 기록한다 |

### 4.1 Phase별 정적 대조 (새 시험 코드·시험 도우미 없음)

- MM-a: 새 `.cs`/`.meta`, 생성자·필드 삭제의 전 호출자, 후보 조회와 프리팹 검사, `Create` 후 첫 `ShowModel` 실패 및 중도 연결 종료의 소유권, 준비 전 비활성/실패 생성물 비활성화·해제, 동일 프리팹 캐시, 반복 `Dispose`를 코드 경로로 확인한다. 오류 주입 도구나 가짜 컴포넌트를 만들지 않으며 실제 예외·재진입을 재현하지 않았으면 미재현으로 적는다.
- MM-b: 확정 덱의 `MainId`로만 모델 키를 읽는지, 다른 보유 메인이 있어도 이 조회가 영향을 받지 않는지 확인한다. 덱/대상 누락을 기본 모델로 숨기지 않는다. 새 `Show` 인자가 모든 오버로드·일반/이동 중 동일성 검사에 전달되는지 확인한다.
- 서비스 경로: 기존 View Factory 생성 → 각 ViewSystem `AddModule` → ViewGroup/InGameGroup Export → 기존 Buffer Context·View DB 조회 → 화면별 좁은 대리자 주입을 대조한다. 서비스 추가·하위 서비스의 루트 재등록 없음.
- 원칙: SSOT는 로비 선택/확정 덱/외형 DB를 각각 원본으로 유지한다. SRP는 선택·표시·조립 분리, OCP는 후보별 분기 없이 DB 조회, LSP는 기존 선택 잠김·수명 계약 유지, ISP는 읽기 계약/대리자만 전달, DIP는 조립 시 주입이다. 공통 `ChessPieceClone` 변경은 두 화면의 복제본 정책까지 확인하되 전투 기능 내부 재구성으로 넓히지 않는다.
- 기능 독립성: 로비 모델 교체는 `LobbyCharacterVisual`/씬 연결, Run 메인 조회는 스테이지 조회 대리자, 보정은 화면별 DB가 변경 지점이다. 노드 이동·덱 편집·Core 규칙은 읽기 검수 외 수정 대상이 아니다. 공통 조립 변경으로 영향받는 로비 아티팩트 팝업 열기/닫기와 입력 복귀는 사용자 재생 확인에 포함한다.

## 5. 공유

- 다른 작업자 줄은 없다(로비·스테이지 선택 View와 그 View DB, 공통 `ChessPieceClone`은 호진 영역). `CombatUnitRagdoll`은 이름만 부르고 고치지 않는다.
- 전투 프리팹을 로비·맵이 같이 쓰게 된다는 사실은 전투 외형 담당(민형님·용근님)에게 알릴 거리다(설계 §6). 메모는 MM-c 뒤 사용자 판단으로.

## 6. 커밋 묶음 (제안, 커밋은 사용자)

- MM-a: 새 `.cs` 둘(+ `.meta`)과 로비 파일, `LobbyModelData`·SO 코드 한 커밋.
- MM-b: `MainModelPrefab` 변경, 스테이지 선택 파일, `StageMapViewData`·SO 코드 한 커밋.
- 보정 값 에셋은 Codex 조정 뒤 따로.

## 7. 실행 기록

(착수 뒤 Phase마다 적는다)

### 7.1 착수 전 Codex 검수·보완 (2026-11-17)

검수 범위: 이 계획·연결 설계·관련 현재 C#·에셋의 정적 대조. 사용자 요청으로 **문서만 보완**, 게임 C#·에셋 변경 없음. Q-MM1~Q-MM4 A와 Phase 순서는 유지한다. 컴파일 미확인, Unity 재생 미확인.

| 지적 | 처리·근거 |
|---|---|
| 보유 메인 수를 실제 Run 메인으로 사용 | 수용. `CharacterRosterRules.PlanAcquire/ValidateOwnership`은 복수 메인 보유를 허용하고 `RunDeckSnapshot.MainId`가 확정 메인이다. b.1~b.3·설계 SSOT를 정정 |
| 첫 모델 생성 실패의 소유권 공백·활성 상태에서 복제 정책 적용 | 수용. 기존 `LobbyCharacterVisual.Create`의 정리 책임을 분리 후에도 유지하도록 §1.1·a.5·a.7 보완 |
| 프리팹 인자가 같은 표시 비교에서 누락 | 수용. 기존 `StageMapViewDisplay.Show`의 빠른 반환보다 앞서 두 표시 동일성 경로에 반영(b.5) |
| 퇴역한 MainCharacter를 운영 후보로 검증 | 수용. `PlayerMainDefinitionPolicy.Validate`의 금지와 맞춰 Ua-3·설계 사실을 정정. 동일 프리팹 최적화는 유지 |
| 씬 묶음이 조회 조립까지 담당·검사 시점 설명 오류 | 수용. 설계 MM3대로 System이 대리자를 조립하고 SceneServices는 보관(b.2~b.3). 마커 주석도 수정 대상(b.7) |
| 현재 랙돌이 매 프레임 돈다는 설명 | 수용. 두 새 프리팹의 랙돌 `m_Enabled`는 0. Q-MM3 A는 복제본 정책 보장으로 유지 |
| 스테이지 덱 편집에서 메인 변경 직후 갱신을 추가해야 한다는 검수 의견 | **반려·정정.** 내부 Draft/Core 계약만 보고 UI 허용까지 단정했다. 사용자 설명과 `DeckEditUI.CanChangeSlot`, 씬·프리팹의 `allowMainCharacterChange: 0`을 확인했다. 메인 변경은 로비에만 있으며 스테이지 덱 편집 확장은 추가하지 않음 |

남은 사용자 결정: 이번 보완에서 추가된 기능 결정 없음. Q-MM2 B는 실제 공통 보정으로 두 모델이 맞지 않을 때만 다시 논의한다. 구현 착수·다음 Phase 인수는 별도다.

### 7.2 MM-a 구현 기록 (2026-11-17, 코드 반영·정적 검수 완료)

- 사용자 지시: 이번 MM-a는 Claude 대신 Codex가 구현한다. 개인 역할 규칙 §2에 이번 단계만의 예외를 먼저 기록했다. MM-b·MM-c는 착수하지 않는다.
- 작업 위치: 기존 Unity checkout `C:/Users/Hyeon2-400/Project_L`, `feature/Hojin/11_03_StageSelect`. 착수 전 대상 C#의 미커밋 변경 없음. 같은 checkout의 새 계획·설계를 기준으로 구현하며 브랜치·worktree·인덱스는 변경하지 않는다.
- 실행 방식: `executing-plans`의 단계별 구현·최종 검수를 적용한다. 사용자 View 규칙을 우선해 새 시험 코드/도우미·빌드·EditMode·커밋은 생략하고, 이 절을 실행 기록으로 사용한다. 컴파일 미확인.
- 사전 계약 대조: a.1 검사는 a.9의 기존 조회 행과 a.7의 조회 대리자를 모두 받는다. a.3/a.4 알림은 a.7 씬이 구현하고 a.8 Scope가 연결한다. a.5의 루트 생성과 최초 모델 생성 사이 정리는 a.7이 소유한다. a.10 생성자 변경 호출자는 `LobbyModelDataSO.GetData` 하나다.
- a.1~a.10 코드 반영. 로비 첫 표시와 선택 변경이 유닛 외형 표의 프리팹을 사용하고, 이동·보정 루트는 유지한 채 안쪽 복제본만 바꾼다. 후보 준비 때 모델 구성을 검사하고 복제본에 공통 표시 정책을 적용한다. 로비 DB의 고정 캐릭터 칸은 코드에서 제거했다. 기존 C# 10개 수정, 새 C# 2개와 각 `.meta` 추가이며 씬·프리팹·SO 에셋은 수정하지 않았다.
- 생성·교체 수명: 첫 모델 조회/생성 실패는 씬에 인계하기 전 로컬 소유자가 정리한다. 새 모델은 비활성 보정 루트에서 준비하고, 성공한 복제본만 게시한다. `ShowModel`의 재진입/폐기 후 오래된 생성 결과 게시를 막는 표시 버전은 모델 소유자 내부에만 둔다. 게임 선택 상태나 이동 상태 사본이 아니다.

별도 읽기 전용 코드 검수의 지적 처리:

| 지적 | 처리·근거 |
|---|---|
| P2: A 표시 중 B 교체의 비활성 콜백에서 A가 다시 요청되면, 같은 프리팹 조기 반환이 B를 취소하지 못함 | **수용·수정.** 표시 버전 증가를 같은 프리팹 검사보다 앞으로 옮겼다. A 요청은 복제를 생략하면서 B의 후속 게시를 무효화하며, 바깥 호출의 `finally`는 살아 있는 기존 모델의 표시를 복원한다. 호출 순서를 정적으로 재대조했다. 실제 재진입 재현은 미실행 |

검증·남은 확인:

- 수정 후 대상 C#의 `git diff --check` 공백 오류 없음. Git의 다음 처리 때 CRLF로 바뀔 수 있다는 설정 경고만 있었으며 실제 작성 파일은 LF로 맞췄다. C#·`.meta` 14개는 UTF-8/LF이며 대체 문자나 임시 주석 잔여 없음.
- `Assets` 전체 C# 검색에서 삭제한 `CharacterPrefab`·`characterPrefab`·`ValidateCharacterPrefab` 참조 없음. 바뀐 생성자·`Create` 호출자는 새 서명으로 연결됐다. 새 `.meta` 두 GUID는 각각 한 파일에서만 확인됐다. 에셋 YAML의 옛 직렬화 칸은 §0대로 그대로 둔다.
- SSOT: 선택 원본과 유닛 외형 표를 재사용. SOLID: 선택 알림·모델 소유/교체·Scope 조립을 분리하고 좁은 표시 계약과 조회 대리자를 주입. JMH/FW: 기존 `PostBindService`→Services→Scope 경로를 유지하며 새 서비스/모듈 등록·Factory 변경 없음. 후보 추가·보정 변경·이동 기능이 각 기존 경계 안에 머무는지 대조했다.
- 전체 워크트리 및 넓은 에셋 경로의 Git 확인은 샌드박스 안의 `git-lfs filter-process` 초기화 오류로 실패했다. 대상 C# 경로의 상태·diff 확인은 성공했다. 저장소 전체가 깨끗하다고 판단하지 않으며 다른 변경을 정리하지 않았다.
- **컴파일 미확인, Unity 재생 미확인.** 새 시험 코드·도우미 작성, MSBuild·EditMode·Unity 실행, 커밋·push 없음. 사용자 확인은 §2의 Ua-1~Ua-7을 따른다. 크기·발 위치 보정은 MM-c에 남긴다.
- MM-a 보고 후 멈췄다. 이 기록 시점에는 MM-b·MM-c 미착수였으며, 이후 사용자의 MM-b 진행 지시는 §7.3에 기록한다. Unity 확인 완료를 인수한 것은 아니다.

### 7.3 MM-b 구현 기록 (2026-11-17, 코드 반영·정적 검수 완료)

- 사용자 지시: "다음 단계인 MM-b도 진행해줘." 개인 역할 규칙 §2에 Codex의 MM-b 위임을 추가했다. MM-a의 Unity 확인 완료로 해석하지 않는다. MM-c·시험 코드·빌드·커밋·push는 범위 밖이다.
- 기존 Unity checkout과 `feature/Hojin/11_03_StageSelect`에서 이어서 작업한다. 착수 전 MM-b의 StageMap/View DB 대상은 미커밋 변경이 없고, `MainModelPrefab`은 MM-a에서 추가한 파일임을 확인했다. §7.2와 같이 실행 기록은 이 계획에 둔다.
- 사전 계약 대조: b.1은 확정 덱의 `MainId`를 읽고 `TryGetOwned`가 반환한 역할을 확인한다. b.3은 좁은 두 ReadSink와 외형 조회만 연결해 b.2에 주입한다. b.4→b.5는 세 Show 오버로드와 일반/이동 중 표시 동일성 모두 같은 프리팹을 받는다. b.6의 맵 DB 생성자는 SO의 `ToData()` 호출과 맞춘다.
- b.1~b.7 코드 반영. 누락 덱/대상·역할 불일치는 예외로 전달하며 보유 메인 수 검사나 기본 모델 대체는 없다. 프리팹 조회 시점은 `ShowNodes`이며 PostBindService·Tick에서 조회하지 않는다. 정적 확인·별도 검수를 마쳤고 계획과 다른 기능 결정은 없다.
- 변경 범위: MM-a에서 만든 `MainModelPrefab`에 조회 함수를 추가하고 기존 StageMap/View DB C# 7개를 수정했다. 새 파일·에셋 변경은 없다. 기존 `.meta`를 유지하고 맵 SO 에셋의 옛 `currentMarkerPrefab` YAML도 그대로 둔다.
- 정적 확인: 대상 tracked 파일의 `git diff --check` 통과. C# 8개는 UTF-8/LF이며 공백 오류·대체 문자 없음. `Assets` 전체 C#에서 삭제된 `CurrentMarkerPrefab`·`currentMarkerPrefab` 참조가 없고, 바뀐 생성자·Show 호출부가 새 서명과 일치한다. 기존 시험·Core C#의 diff 없음.
- 표시 동일성: 같은 맵/설정/프리팹이면 기존 인스턴스를 재사용한다. 다른 프리팹은 일반 표시에서 기존 Clear/재생성 경로로 간다. 이동 중에는 두 번째 identity에도 프리팹을 넣어 기존 구조 교체 예외가 Clear보다 먼저 발생하도록 유지했다. 마커의 실제 생성·점프·착지·해제 코드는 수정하지 않았다.
- 사용자 재확인 정책: `StageSelect.unity`와 `DeckEditUI.prefab`의 `allowMainCharacterChange: 0` 유지. 덱 편집의 확정 알림·메인 변경 기능·Tick 감시는 추가하지 않았다.
- **컴파일 미확인, Unity 재생 미확인.** 새 시험 코드/도우미, MSBuild·EditMode·Unity 실행, 커밋·push 없음. 사용자 확인은 §3의 Ub-1~Ub-7이다. 크기·발 위치·조명·재질 확인과 보정은 MM-c에 남긴다.

별도 읽기 전용 코드 검수 결과:

- **수정이 필요한 정적 결함 없음(Critical·Important·Minor 모두 없음).** 확정 덱 조회·오류 전달·좁은 의존성 캡처·세 Show 오버로드/두 표시 동일성·삭제 필드 호출자를 대조했다.
- 초기 진입과 오류 경계도 확인했다. 기본 Stage 진입은 `RunStartJob → StageJob`, 개발 진입은 `StartNewRun → EnterChapter` 순서다. `PublishNewRun`은 보유 기록과 초기 확정 덱을 설치한다. 준비·재그리기의 모델 조회 실패는 `StageMapViewEntryPreparation.PrepareAttempt`의 기존 오류 경계로 전달된다.
- 서비스 경로: `InGameViewSystemFactory` 생성 → `StageMapViewSystem.ExportServices`의 `AddModule` → `InGameViewSystemGroup` Export → `InGameSystemFactory`/`InGameSystemGroup` 유지. Buffer Factory 내부 ReadSink 등록 → `BufferSystemGroup.IBufferQuery`, View DB Factory의 외형 표 등록 → `ViewDataBase.UnitVisualTableQuery`를 기존 소비 지점에서 사용한다. 새 서비스·루트 재등록·InGame DB 읽기·Core 쓰기 없음.
- 원칙: SSOT는 확정 덱/보유 기록/외형 표에 남는다. SRP는 Run 메인 조회·표시·조립 분리, OCP는 후보별 분기 없는 외형 조회, LSP는 기존 표시/이동/해제 계약 보존, ISP는 두 읽기 계약과 좁은 대리자, DIP는 System에서 의존성 주입으로 대조했다. 메인 외형 변경이 노드 이동·덱 편집 기능 수정으로 번지지 않는다.
- 검수에서 실행 판단을 유보한 항목의 처리: 크기·발 위치·재질·Idle 품질은 기존 MM-c/사용자 Unity 확인 대상으로 유지한다. 실제 이동·재진입·예외 콜백은 미재현으로 기록하며 정적 대조를 실행 성공으로 간주하지 않는다. MM-a 자체는 §7.2 검수 기록을 유지하고 이번 MM-b와 무관한 재검수로 넓히지 않는다.
- MM-b 보고 후 멈춘다. MM-c의 전체 마무리·보정 작업과 사용자 Unity 확인은 남아 있다.

### 7.4 MM-c 문서·통합 검수·보정 기록 (2026-11-17, 실제 화면 보정 대기)

- 사용자 지시: "다음 단계인 MM-c도 진행해줘." 개인 역할 규칙에 c.1~c.4 문서·검수와 로비/맵 표현 보정 에셋 범위를 기록했다. 사용자 답변 "확인했고 로비를 실행해 두겠습니다"는 MM-a·MM-b의 컴파일·재생 확인으로 기록하되, 세부 Ua/Ub 항목 모두의 개별 실행 증거로 확장하지 않는다.
- c.1: 로비 메인 선택 설계 Q-LM5에 이번 설계로의 이어짐을 추가했다. MM-a의 같은 프리팹 재진입 보완은 계획 §1.1의 수명 계약 구현이며 기능 결정 변경은 없다. MM-b도 계획과 다른 기능 결정 없음. 컴파일은 Codex 미실행/미확인, 사용자 확인 답변은 위 기록을 따른다.
- c.2 완료: MM-a 정적 검수 P2(같은 프리팹 재진입 순서)는 수용·수정 완료(§7.2), MM-b 별도 정적 검수는 지적 없음(§7.3). 최종 통합 읽기 전용 검수에서도 Critical·Important·Minor 지적 없음. 로비 선택→모델/출발, 확정 덱→보유 키→외형, 기존 F1 오류 경계, 두 표시 동일성, P2 수정 순서와 문서 기록을 대조했다. 생성자 소비자·삭제 필드 잔여 참조·새 `.meta` GUID 유일성과 대상 `git diff --check`도 확인했다.
- 통합 원칙 점검: SSOT는 선택/확정 덱/외형 표에 유지된다. SOLID 다섯 원칙과 조립/기능 분리, 기존 Factory 생성→ViewSystem 등록→Group Export→좁은 읽기 의존성 주입 경로가 §7.2~7.3과 일치한다. 모델 추가나 보정 변경이 노드 이동·덱 편집 구현 수정으로 번지지 않는다. 통합 검수에서 유보한 실제 크기·발 위치·재질·Idle·이동·재진입·예외 콜백 결과는 직접 화면 관찰 또는 사용자 확인으로만 채운다.
- 도구 확인: Unity CLI는 Pipeline 연결을 찾지 못했으며 현재 프로젝트 manifest에 Pipeline 패키지가 없다. 실제 창에서는 Unity 6000.0.73f1의 Project_L이 열려 있음을 확인했다. 새 패키지·Editor 스크립트를 추가하지 않고 사용자 실행 화면에서 보정한다. CLI 업데이트 조회는 DNS 오류로 실패했고 설치·업데이트하지 않았다.
- 보정 전 값: 로비 `characterOffset=(0,-0.08218316,0)`, `characterYaw=0`, `characterScale=2.146858`; 맵 `markerOffset=(0,0.12890838,0)`, `markerYaw=0`, `markerScale=1.073429`. 두 에셋의 착수 전 미커밋 변경 없음. 원본 캐릭터 프리팹·씬·이동 규칙은 수정하지 않는다.
- c.3/c.4 대기: 사용자의 실행 준비 답변 뒤 실제 창 상태를 재확인했으나 마지막 관찰은 `Project_L - Boot`의 비재생 편집 모드였다. 로비/맵의 Akimbo·Sniper를 보정 전후로 비교하지 못했으므로 두 보정 에셋은 수정하지 않았고 숫자를 추정해 확정하지 않았다. 이 상태를 MM-c 전체 완료로 보고하지 않는다.
- 이어서 할 일: 사용자가 로비를 실행해 두면 두 모델의 크기·발 위치·방향을 관찰하고 승인된 두 에셋의 공통 보정 값을 맞춘다. 맵도 두 모델로 확인해 재질·조명·Idle·노드 이동을 별도로 기록한다. 에셋을 변경하면 `DataBaseAssetTests`를 사용자 Unity 확인 항목에 추가한다. 한 공통 보정으로 두 메인을 맞출 수 없을 때만 Q-MM2 B를 다시 논의한다.
- 이번 MM-c 변경은 계획·연결 설계·앞선 메인 선택 설계의 문서 3개다. C#·보정 에셋·원본 프리팹·씬·이동 규칙은 변경하지 않았다. 새 시험 코드·시험 도우미·MSBuild·EditMode·커밋·push 없음.
