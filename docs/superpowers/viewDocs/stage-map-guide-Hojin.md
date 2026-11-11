# 스테이지 맵(StageSelect) 안내

- 작성자: 호진(이호진)
- 대상: 스테이지 맵의 에셋을 연결하거나 표시·입력 코드를 고치는 사람(View·아트·기획).
- 무엇을 하나: 만들 것 → 연결할 곳 → 확인할 것 순서로 적는다. 왜 그렇게 되어 있는지(결정·계약)는 [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md), 진행·확인 상태는 [스테이지 맵 계획](../plans/2026-10-10-stage-map-plan-Hojin.md)을 본다.
- 정리: 2026-10-27 스테이지 선택 입력·요청 Buffer 안내를 이 문서로 합쳤다(옛 판 git `10914570`). 2026-11-01 노드 프리팹·바닥·아이콘·완료 봉을 없애 그 안내(옛 §2·§3)를 지웠다.

## 1 어디에 무엇이 있나

| 바꾸려는 것 | 위치 |
|---|---|
| 노드 간격·크기, 건물 높이, 캐릭터 모델·보정, 이동 시간·높이, 클릭 거리 | [`StageMapView_Default.asset`](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/StageMapView_Default.asset)의 `Stage Map View Data` |
| 챕터 최초 연출의 사용 여부·전개·카메라 왕복 시간 | 같은 에셋의 `Map Intro` (§3.1) |
| 노드 낙하의 사용 여부·높이·개별 시간 | 같은 에셋의 `Node Drop` (§3.1) |
| 종류별 건물 | `Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_*.asset`의 `Visual Prefab` |
| 챕터 배경 | `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapBackground/`, 프리팹 `Assets/ProjectL/Prefabs/StageSelect/Backgrounds/` |
| 맵 입력 바인딩 | [`PlayerInputActions.inputactions`](../../../Assets/ProjectL/Input/PlayerInputActions.inputactions)의 `StageSelection` 액션 맵 |
| 씬 참조(맵 루트·배경 루트·맵 카메라·UI) | `Assets/Scenes/StageSelect.unity`의 `StageMapSceneRoot` |
| 표시·입력 코드 | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/` |

## 2 건물 바꾸기

노드는 건물 하나로 보인다. 종류 SO의 **`Visual Prefab`**에 프리팹 에셋을 연결하면 된다. 지금 다섯 SO는 모두 `Resources/Map/Environment/BackgroundSceneTest/Bar_ver1_final.prefab`이다.

| 종류 | SO |
|---|---|
| 일반 전투 | [NodeVisual_Combat_Regular](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_Combat_Regular.asset) |
| 보스 | [NodeVisual_Combat_Boss](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_Combat_Boss.asset) |
| 상점 | [NodeVisual_Shop_Regular](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_Shop_Regular.asset) |
| 이벤트(지금은 회복) | [NodeVisual_Event_Regular](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_Event_Regular.asset) |
| 강화(StageKey 1202 전용) | [NodeVisual_Shop_Enhancement](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_Shop_Enhancement.asset) |

- `Stage Key`가 0이면 종류·역할 공통 외형, 양수면 그 StageKey 전용 외형이다. 전용 외형이 있으면 먼저 쓴다.
- 건물 프리팹에는 켜진 MeshRenderer가 있어야 한다. Collider는 넣지 않아도 된다.
- 건물 **밑면**은 `StageMapView_Default`의 **`Building Height`**(지금 0.05, 연결선 높이와 같음)에 맞춰 놓인다. 켜진 메시를 재서 맞추므로 모델마다 피벗이 달라도 된다. 건물이 뜨거나 묻혀 보이면 이 값 하나를 고친다.
- 시작 노드가 아닌 노드에 건물이 없으면 갈 수 없는 노드가 되므로 표시할 때 오류로 멈춘다.

### 2.1 건물 호버 글 넣기

건물에 마우스를 올리면 전역 Hover 패널(GlobalUICanvas의 `HoverTarget/HoverPannel`)에 제목·설명이 뜬다. 모든 스테이지의 글은 `Assets/ProjectL/DataBaseSO/ViewGroup/StageOfferText/Item/`의 `StageOfferText_*` 한 곳에 있다. 제목 = **`Event Name`**, 설명 = **`Event Description`**.

| 스테이지 | 줄(`Target`) | 에셋 |
|---|---|---|
| 일반 전투(전부) | `Info`, `Stage Keys` 비움, `Stage Type` Combat·`Map Role` Regular | [StageOfferText_Combat](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageOfferText/Item/StageOfferText_Combat.asset) |
| 보스 전투(전부) | `Info`, `Stage Keys` 비움, `Stage Type` Combat·`Map Role` Boss | [StageOfferText_Boss](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageOfferText/Item/StageOfferText_Boss.asset) |
| 용병 고용소·훈련소·상점·회복 | `Stage`, 그 StageKey | `StageOfferText_Mercenary`·`_Training`·`_Shop`·`_Recovery`. 팝업 머리줄과 같은 글이라 한 번 고치면 둘 다 바뀐다 |

- 찾는 순서: 그 StageKey가 든 줄 → 없으면 같은 종류·역할의 공통 줄(`Info`이고 `Stage Keys`가 빈 줄). 그래서 **새 일반·보스 전투 스테이지는 아무것도 등록하지 않아도** 공통 줄 글이 나온다.
- 특정 스테이지만 다른 글을 쓰려면 `Target`을 `Info`로 한 새 `StageOfferText_*`를 만들고 `Stage Keys`에 그 키를 넣어 표(`StageOfferTextDataTable`)에 등록한다. `Info` 줄은 이름·설명만 채우면 되고 버튼·안내 글 칸은 비워 둔다.
- 팝업이 있는 스테이지(상점·훈련소·회복)는 키를 `Stage` 줄의 `Stage Keys`에 꼭 넣는다. 팝업은 키 줄만 읽어서, 빠지면 팝업에서 오류로 멈춘다.
- 종류·역할마다 공통 줄은 하나만 둘 수 있다. 같은 키를 두 줄에 넣어도 안 된다(DB를 만들 때 멈춤).
- 시작 노드, 지금 서 있는 노드, 클리어한 노드에서는 뜨지 않는다. 건물을 누르면 사라지고, 다른 곳에 갔다 와야 다시 뜬다. 팝업·설정 창이 열려 있는 동안에도 뜨지 않는다.
- 글은 게임을 시작할 때 한 번 읽는다. 재생 중에 고치면 재생을 다시 시작해야 보인다.
- 노드 외형 SO의 `Display Name`은 호버에 쓰이지 않는다.

## 3 노드 자리와 클릭

프리팹이 없다. 코드가 노드마다 빈 자리(`StageNode_<id>`)를 만들고 그 아래에 건물을 놓는다. 캐릭터와 연결선도 이 자리를 기준으로 한다.

클릭은 건물을 감싼 상자로 판정한다. 코드가 켜진 메시 크기를 재서 노드 자리에 BoxCollider를 붙인다(메시 모양이 아니라 상자이고, 겹치면 카메라에 가까운 상자가 잡힌다). 건물이 숨은 현재·완료 노드는 눌리지 않고 그 자리의 누름은 카메라 끌기가 된다. Core가 두 경우 모두 거절하므로 잃는 동작은 없다.

### 3.1 챕터 최초 맵 진입 연출

각 Run에서 챕터 맵을 처음 보여줄 때, 현재 플레이어부터 보스 방향으로 선이 뻗고 선 끝의 도착에 맞춰 건물이 착지한다. 갈림길은 함께 전개되며, 이미 나타난 점선의 위치·길이·간격은 유지된다. 카메라는 플레이어가 보이는 시작 구도에서 보스까지 이동하고 잠시 머문 뒤 시작 구도로 돌아온다.

시간표는 현재 플레이어에서 보스까지의 **맵 진행축 거리**를 기준으로 한다. 현재 노드에서 전방으로 도달 가능한 구간만 연출하며, 현재 위치·클리어 건물·시작 장식·플레이어 마커는 낙하 대상이 아니다. 전방 밖은 기존 최종 표시를 유지한다. 현재 노드가 보스이고 출발선이 없으면 연출을 생략한다.

`StageMapView_Default`의 `Stage Map View Data`에서 조정한다. 실행 중 변경은 Play를 다시 시작해야 반영된다.

| Map Intro 필드 | 운영 초기값 | 뜻 |
|---|---|---|
| Enabled | 켜짐 | 전체 연출 사용. 끄면 최종 표시만 하고 새 재생 이력·효과·카메라 왕복을 만들지 않음 |
| Start Hold Seconds | 0.6 | 로딩 종료 후 시작 대기(초) |
| Reveal Seconds | 7 | 플레이어에서 보스까지 선·착지·카메라가 전개되는 시간(초) |
| Boss Hold Seconds | 0.9 | 보스에서 머무는 시간(초) |
| Return Seconds | 2.2 | 시작 구도로 돌아오는 시간(초). 0이면 즉시 복귀 |

| Node Drop 필드 | 운영 값 | 뜻 |
|---|---|---|
| Enabled | 켜짐 | 건물 낙하와 착지 효과 사용. 꺼도 선 그리기·카메라 왕복·최초 이력은 유지 |
| Height | **20** | 맵 위쪽 축을 따른 월드 낙하 높이 |
| Seconds | 0.8 | 건물 하나가 떨어지는 게임 시간(초) |

옛 `Wave Spread Seconds`는 제거했다. 전체 전개 속도는 `Map Intro > Reveal Seconds`로 조정한다. 첫 건물의 낙하가 시작 대기보다 앞서지 않도록 필요하면 전개 전에 준비 시간을 자동으로 더한다. 높이 또는 낙하 시간이 0이면 각 건물의 **도착 시각**에 즉시 착지하고 효과를 재생한다. 모든 시간·높이는 유한한 0 이상이어야 하며, `Reveal Seconds`는 0보다 커야 한다.

로딩 중 배치·숨김·초기 카메라 구도를 준비하고 로딩 종료 후 공통 시계를 시작한다. **낙하·선·카메라 복귀·착지 효과 정리가 모두 끝나야** 맵 조작을 허용한다. 착지 효과는 기존 `FallingRocks`를 쓰며, 마지막 효과를 실제 생성한 때부터 게임 시간 3초가 지나도 남아 있으면 경고 한 번과 함께 회수한다. 효과 대기는 카메라 정지·복귀와 함께 진행한다.

그동안 노드 선택·호버·윤곽선·새 팝업 열기, 배경 드래그·휠과 에디터 Alt 카메라 조작을 막는다. 차단 중 누른 채 완료를 기다렸다 떼어도 선택이나 드래그가 시작되지 않는다. 설정 창은 열 수 있으며, 열면 세 연출·파티클·효과 상한 대기가 함께 멈추고 닫으면 이어진다. 이 추가 차단은 `StageMapIntroInteractionPolicy`에 분리되어 있다.

전투 복귀·상점/회복 종료·같은 맵 다시 그리기에는 반복하지 않는다. 다음 챕터와 새 Run에서는 다시 재생한다. 첫 표시 변화 전 취소는 재시도하고, 시작 후 이탈은 재진입 때 최종 배치를 보여 준다. 이력은 런타임 View에만 있으므로 실행을 새로 시작하면 초기화된다. 보류한 상점/회복 복원은 전체 연출 완료 후 현재 진행을 다시 읽어 선택 화면보다 먼저 처리한다.

노드 누락·보스 중복/도달 불가·진행축 역방향 연결·지원하지 않는 선 메시·플레이어/보스를 진행축 이동으로 보여 줄 수 없는 카메라 구도는 구성 오류로 보고한다. 선 원본 메시·머티리얼·프리팹과 카메라 저장 구도는 연출에서 변경하지 않는다.

새 통합 연출은 **Phase 1 코드 작성·정적 검토 완료, 컴파일 미확인**이다. 사용자 Unity 확인 방법과 미확인 항목은 [맵 진입 연출 계획의 U1~U9](../plans/2026-11-06-stage-map-intro-plan-Hojin.md#사용자-확인-u1u9)에서 확인한다. 선행 낙하의 기본 재생 확인은 [기존 실행 기록](../plans/2026-11-06-stage-node-drop-plan-Hojin.md#phase-1-실행-기록)에 보존하며, 새 선·카메라·입력 연출의 확인을 뜻하지 않는다.

## 4 현재 위치와 클리어 표시

**무엇이 그리나.** `StageMapViewReader`가 현재 진행 Buffer의 `HasSelection`·`CurrentStage.NodeId`를 읽는다. 선택 전에는 표시한 맵의 시작 노드(`IsStart`)를 현재 위치로 쓴다. 클릭한 순간 View가 위치를 먼저 정하지 않는다. 아직 표시하지 않은 다른 맵의 값은 쓰지 않는다.

**노드마다 무엇이 보이나.**

| 노드 상태 | 보이는 것 |
|---|---|
| 현재 노드 | 캐릭터만 |
| 이동 예정 목적지(수락 뒤 이동 중) | 건물(착지하면 숨음) |
| 떠난 완료 노드(Core `IsClear`) | 없음 |
| 떠난 미완료 노드 | 건물 |
| 시작 노드 | 시작 외형(`Start Visual Prefab`, 지금은 없음) |

연결선은 늘 남는다. 이 우선순위는 표시 규칙이며 Core의 완료 사실을 바꾸지 않는다.

**캐릭터 바꾸기.** 캐릭터는 `StageMapView_Default`의 **`Current Marker Prefab`**이 원본이다. 지금은 로비와 같은 `Prefabs/Characters/Colored/Playerable/Akimbo.prefab`을 직접 연결한다(2026-10-26). 새 래퍼 프리팹이나 전투 유닛 데이터는 필요 없다.

| 필드 | 지금 값 | 뜻 |
|---|---|---|
| Marker Offset | `(0, 0.12890838, 0)` | 노드 자리 기준 위치(발끝 보정 + 옛 바닥 윗면 0.12. 뜨거나 묻혀 보이면 고친다) |
| Marker Yaw | 0 | 이동 방향에 더할 회전 |
| Marker Scale | 1.073429 | 모델 배율(로비 2.146858과 따로) |
| Marker Move Duration | 0.6 | 노드 사이 이동 시간(상승·이동·하강 = 12:35:12) |
| Marker Jump Height | 0.8 | 이동 중 들어 올리는 높이 |

- 원본 프리팹의 루트 Transform 대신 위 보정 값을 쓴다. 값은 유한해야 하고 배율은 양수여야 한다.
- `StageCurrentMarkerView`가 노드 루트 아래에 이동 Root → 보정 Root → 모델 복제본을 만든다. 복제본에만 Default Layer·Collider 끔·Animator root motion·animation events 끔을 적용하고 Idle은 유지한다. 원본과 로비·전투 사용처는 바뀌지 않는다.
- 설정을 바꾸고 표시를 갱신하면 보정 값까지 비교해 생성물을 바꾼다.
- 씬 이탈·다시 그리기 때 캐릭터도 함께 정리된다. 카메라 핸들·렌즈에는 관여하지 않는다.

**건물을 누르면.** Core가 수락한 경우에만 캐릭터가 목적지로 뛰어 이동한다. 목적지의 건물은 착지할 때 숨는다. 상점·회복은 착지 뒤에 팝업이 열리고, 전투는 착지 뒤에 전환된다. 거절이면 움직이지 않는다.

## 5 챕터 배경 추가하기

1. 배경 모델을 담은 프리팹을 `Prefabs/StageSelect/Backgrounds/`에 만든다. 루트는 활성·위치 0·회전 identity·배율 1이다. 배치는 프리팹 안에서 한다.
2. `DataBaseSO/ViewGroup/StageMapBackground/Item/`에 배경 SO를 만들고 `Chapter Key`(양수, 표에서 유일)와 `Background Prefab`(프리팹 에셋)을 넣는다.
3. `StageMapBackgroundDataTable`에 그 SO를 등록한다.
4. 그 챕터 맵에 들어가 배경이 한 번만 보이는지, 상점·회복·전투 왕복 뒤에도 하나인지 확인한다.

없는 챕터의 배경을 그리려 하면 구성 오류로 멈춘다. 다른 챕터 배경으로 대신 등록하지 않는다. 지금은 1챕터(`StageMapBackground_Chapter1`)만 있다.

## 6 입력과 선택 확정

### 6.1 실행 경로

```text
PlayerInputActions / StageSelection
 → UnityInputBridge → InputSystem.Submit
 → StageSelectionInputController → StageSelectionReceiverSlot
 → StageMapInteraction → StageViewBuffer
 → StageFlow / StageNodeEntryFlow → JobRegisterSystem → Plan → 공용 JobRunner → Job
 → StageCommandProcessingService: 기대 번호의 입력을 직접 조회·판정·결과 게시
 → StageMapEntryObserver: 결과와 전투 요청 수명 확인 → StageMapNodeMovement 이동·착지 → 입력 정리
```

- 상점·회복 같은 일반 노드: `StagePlan → StageNodeEntryJob → ProcessNextNodeCommand`.
- 전투 노드: `StageLifeCycle.TryRequestCombat → CombatEntryService.RequestEntry(chapter, node, N) → Register(Combat, Entry) → CombatPrepareJob(TryProcessNextBattleCommand) → CombatEntryTransitionJob`. 전환은 캐릭터 착지 뒤에만 일어난다.
- 입력 조회·게임 규칙·결과 게시의 주체는 Core의 `StageCommandProcessingService`다. View는 의도를 쓰고 결과를 대조한다.

### 6.2 액션

| 액션 | 바인딩 | 동작 |
|---|---|---|
| PointerPosition | Pointer position | 노드 호버 갱신, 또는 빈 배경에서 시작한 카메라 드래그 |
| Select | Pointer press, ReleaseOnly | UI 밖의 **같은 노드에서 누르고 떼면** 진입 요청 한 번. 빈 배경 누름은 카메라 드래그 시작 |
| Confirm | Enter | 아무 동작 없음(클릭이 곧 요청) |
| Cancel | 마우스 오른쪽 | 누른 노드·배경 드래그·호버 해제. 요청 없음 |

- Select는 `Press(behavior=1)`, `initialStateCheck=false`다. 정상 뗌은 Performed, Layer 종료 등에 의한 강제 취소는 Canceled다.
- 요청은 맵을 실제로 그린 뒤(`SetDisplayedChapter`)에만 낸다. 입력 Buffer에 미정리 요청이 남아 있으면 더 내지 않는다(연타 방지).
- 노드 집기(`StageNodePicker.TryPickNode`)는 출력 카메라 레이와 `Pick Distance`로 건물을 감싼 클릭 상자만 본다(§3). Layer 필터는 없고, 꺼진 상자는 건너뛴다. 캐릭터에는 Collider가 없다.
- UI 가림은 `StageMapPointerBlocker`가 EventSystem/GraphicRaycaster로 판정한다. UI·노드에서 시작한 누름은 배경으로 끌어도 카메라 드래그가 되지 않는다. 배경 드래그 중에는 호버·진입이 없다. 범례는 11-03부터 숨겨 두어(프리팹 `StageLegendUI`의 `Frame` 꺼짐) 맵 입력을 막지 않는다. 다시 보이려면 `Frame`을 켠다(설계 §5.5).
- 팝업이 열리면 Layer가 `StageMapPopup`(상점·회복·덱 편집 공통, 공용 맵만 켬)으로 바뀌어 진행 중 Select는 Canceled로 끝난다.

## 7 입력 연결과 수명

연결 소유자는 [`StageMapViewSystem`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs)이고, 씬 연결 한 번의 수명은 `StageMapViewScope`가 갖는다. 다른 View에 수신 계약을 다시 구현하거나 같은 슬롯에 또 Bind하지 않는다.

1. `StageSelectionInputFactory`가 Controller·수신 슬롯·Layer(`StageSelectionInputLayers.Selection`·`Popup`)를 만든다. 같은 슬롯을 Controller와 `ViewGroup.Input.StageSelection`이 참조한다. `StageMapViewSystem.PostBindService`가 슬롯·Global 입력 Layer·요청 Buffer·결과 읽기를 받는다.
2. CameraGroup 바인딩(-190) 뒤 View 연결(100)에서 `StageMapSceneRoot`와 출력 카메라를 찾는다. Scope가 피커와 `StageMapCameraDrag`를 만들고 카메라 핸들을 한 번 빌린다. 전투 표시 lease를 받은 뒤 `StageMapInteraction`을 슬롯에 Bind하고 StageSelection Layer를 켠다. 맵이 준비되면 표시한 챕터를 입력에 넘기고, 실제 시작·보스 위치로 드래그 범위를 정한다.
3. Tick은 `StageMapEntryObserver`·`StageMapNodeMovement`·팝업 Controller로 결과를 소비한다. 맵 View는 Core 진입 API를 직접 부르지 않는다. 선택 이동은 캐릭터와 카메라가 함께 끝나야 완료하며(§12), 완료가 바뀌면 `RequestMapRedraw`로 다시 그린다.
4. Detach(-100)는 맵·상점 입력을 Clear하고 팝업·입력·표시를 정리하며 카메라 핸들을 반환한다. 입력 해제는 해석기를 StopInput한 뒤 슬롯을 Unbind하고, 현재 Layer가 StageSelection일 때만 ClearLayer한다.
5. 공용 오류·종료의 `StopInput`은 새 입력·드래그를 막고 핸들을 반환하며 소유 입력을 비운다. 늦게 온 옛 수신자 호출은 새 요청을 쓰지 못한다.

```csharp
var input = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input;
IStageSelectionReceiverSlot slot = input.StageSelection;
var layers = resolver.GetService(SystemGroupsService.Global)
    .InputServiceCommand.InputLayerServices;
```

- 같은 슬롯을 얻으려고 Factory를 다시 부르거나 루트에 Export하지 않는다. Configure는 Bridge.InitData에서 한 번이고, 화면 연결·팝업 복귀 때 다시 하지 않는다.
- 슬롯은 연결 안 된 입력을 버린다. 이미 연결된 슬롯에 Bind하면 같은 대상이어도 예외이고, 다른 대상의 Unbind는 무시한다. 누름을 받은 대상만 뗌·취소를 받고, Unbind 중 누름이 진행 중이면 그 대상에게 취소를 한 번 준다. 슬롯은 요청을 보관하지 않는다.

## 8 요청과 결과 계약

요청 생성: `StageViewCommandData.SelectStageNode(int chapterKey, StageNodeId nodeId)`. 표시한 챕터와 고른 노드만 담는다. 화면 좌표·GameObject·DB 행·진입 가능 여부는 넣지 않는다.

| API·데이터 | 뜻 |
|---|---|
| `StageViewReadSink.Commands` | 지워지지 않은 입력 목록 |
| `TryGetNextCommand(handledThrough, out command)` | 처리 번호보다 큰 첫 입력. 조회로 지우지 않음 |
| `StageViewWriteSink.Submit(data)` | 입력 보관·번호 반환. 바로 Core를 실행하지 않음 |
| `RemoveCommandsThrough(handledNumber)` | View가 확인한 번호까지 지움 |
| `Clear()` | 씬 이탈·오류 등 소유 수명 끝에 지움 |
| `StageCommandResults` | Core가 게시한 처리 번호·진입 결과 |

- 발급 번호는 Buffer 인스턴스 안에서 늘어나고 Remove·Clear 뒤에도 다시 쓰지 않는다. 다른 입력 Buffer와 같은 전역 번호가 아니다. `handledThrough`는 Core의 순차 처리 번호다.
- Core는 `HandledMapCommandNumber` 다음 입력이 Job의 기대 번호와 같을 때만 처리하고, `StageEntryCommandResult`에 번호·챕터·노드·Node/Battle 종류·결과를 남긴다.
- `StageMapEntryObserver`는 번호·챕터·노드가 맞는 결과만 소비한다. 전투는 수락만으로 입력을 지우지 않고 `IsActive` 또는 `IsRejected`까지 기다린다. 실행 전 취소(`IsCancelled`)는 거절 결과 없이 그 입력을 정리한다. 결과의 챕터가 지금과 다르면 화면에 적용하지 않는다. 일반 노드 수락 뒤에는 착지를 기다려 상점·회복 팝업을 복원한다.

## 9 실제 화면에서 확인

1. Play를 끈 상태에서 프리팹·SO를 저장한다.
2. `ProjectL > Play From Boot 설정...`을 열고 지금 값을 적어 둔다. 이 PC 덮어쓰기를 켜고 `Start Life Cycle = Stage`, `Execution Mode = Live`로 둔다. Play From Boot를 켠다.
3. Play로 스테이지 선택에 들어간다. 확인이 끝나면 위 설정을 원래대로 돌린다. 씬·GameBootstrap 에셋은 고칠 필요가 없다.
4. 확인할 것:
   - 건물: 배경 위에 떠 있거나 묻히지 않는지, 윗부분을 눌러도 골라지는지. 맵을 끌어 가장자리와 다른 깊이에서도 본다.
   - 캐릭터: 배경에 발이 닿는지(Idle 접지), 방향, 크기.
   - 건물을 눌러 이동·착지·이탈 뒤 표시가 §4 표와 맞는지. 상점·회복 팝업이 열린 동안 맵 입력이 막히는지.
   - 범례·덱 버튼·팝업과의 겹침. 자동 시험의 Main Camera RenderTexture에는 Overlay Canvas가 안 찍히므로 UI 겹침은 Game View에서 본다.
5. 무작위 맵에 어떤 종류가 안 나왔으면 미확인으로 두고 다음 새 Run에서 본다. 이벤트 출현을 강제하지 않는다.
6. 노드 낙하는 §3.1과 전용 계획 U1~U6을 확인한다. 특히 첫 진입의 로딩/순간 노출, 낙하 중 배경 드래그, 설정 창에서 정지/재개, 복귀 시 재생 생략을 본다. SO 변경에 따른 `DataBaseAssetTests`는 사용자 Unity 확인 항목이다.

## 10 커밋할 때

건물을 바꾸면 새 프리팹·Mesh·Material·Texture와 `.meta`, 바꾼 SO를 함께 커밋한다. 최종 모델의 렌더 비용은 실제 메시·재질로 대상 기기에서 확인한다.

## 11 규칙의 출처

- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-006은 입력 서비스 설정 수명, FW-007은 이 InputSystem에 연결된 장치 입력의 `Submit` 경로에 적용되는 MUST다. uGUI 버튼까지 같은 Submit 경로로 보내라는 규칙은 아니다.
- [Assets/AGENTS.md](../../../Assets/AGENTS.md)는 버튼 입력에 `UIInputBinding + IUIInputReceiver`를 쓰고 `Button.onClick.AddListener`·Inspector UnityEvent 연결을 쓰지 않도록 정한다.
- 노드 클릭 확정, View 소유 입력 Buffer, Flow·Plan·Job 경유, Core의 기대 번호 조회, View의 결과 정리는 **이 프로젝트의 결정**이다([설계](../specs/2026-10-10-stage-map-design-Hojin.md) §3·§4). 공용 Framework의 보편 MUST로 넓히지 않는다.
- 공용 Buffer 조회 객체에 WriteSink가 보인다고 View가 Core 소유 상태를 바꾸지 않는다.
- 카메라 조립·수명·검증은 [카메라 Framework 공유](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md)를 따른다.

## 12 노드 이동의 카메라 동행과 복귀 정렬 — 2026-11-07

Phase 1 코드 작성·정적 검토와 독립 검토 지적 2건 보완을 마쳤다. **컴파일·사용자 Unity 확인은 미확인**이다. [설계](../specs/2026-11-06-stage-map-camera-focus-design-Hojin.md), [Phase 1 계획과 U1~U8](../plans/2026-11-07-stage-map-camera-focus-plan-Hojin.md#3-사용자-unity-확인과-phase-인수), [공유 메모·미전달](../handoffs/2026-11-07-stage-map-camera-focus-framework-share-Hojin.md)을 함께 본다.

설정은 [`StageMapView_Default`](../../../Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/StageMapView_Default.asset)의 **Camera Focus**에 있다. 변경 후 Play를 다시 시작한다.

| 설정 | 현재 값 / 용도 |
|---|---|
| Enabled | true. 끄면 클릭 동행과 로딩 중 복귀 정렬만 생략한다. 최초 낙하·선·보스 왕복은 별도다. |
| Player Viewport Y | 0.35. 플레이어의 바닥을 화면 아래에서 35% 높이에 맞춘다. 유한한 0 초과 1 미만이며 맵 경계에서는 기존 이동 범위가 우선한다. |
| Current Marker Motion → Marker Move Duration | 현재 0.6초. 더 빠르게 이동하려면 이 값을 줄인다. 카메라도 같은 상승·이동·하강 전체 시간을 사용하므로 별도 카메라 시간은 없다. |

- 전투 노드는 클릭 후 플레이어와 카메라가 함께 이동하고, 둘 다 끝나면 덱 출전 화면을 연다. 덱 취소·요청 거절/실행 전 취소는 클릭 전 구도로 돌아간다. Submit만으로 복원 정보를 버리지 않으며 실제 수락·준비 뒤 확정한다.
- 상점·회복은 기존 선택 요청의 수락 뒤 두 표시가 함께 이동하고 완료 후 팝업을 연다. 팝업을 닫아 맵을 다시 그릴 때는 추가 정렬하지 않는다.
- 같은 챕터의 전투 복귀는 로딩 가림 아래에서 현재 위치로 정렬한다. 최초 Intro가 재생되는 진입은 기존 왕복 구도를 유지한다. 가림 없는 진입에는 복귀 정렬을 적용하지 않는다.
- 이동 중에는 노드 선택·배경 드래그·휠·Alt·Inspector 구도 편집이 차단된다. 설정 창의 시간 정지에는 플레이어와 카메라가 같이 멈춘다.
- Inspector에서 Position/Rotation/Size를 바꾸거나 Alt로 조정할 때 현재 표시 구도부터 이어진다. Inspector를 보기만 해서는 저장하지 않는다. `확정 및 잠금`은 현재 편집 구도를 저장하고, `조정 취소 및 잠금`은 기존 확정 구도로 돌아간다.
- Run·맵·준비 버전이 달라지면 이전 Delay 콜백과 카메라 이동은 폐기한다. 같은 챕터/노드 번호라도 새 Run으로 이전 이동의 완료나 복원을 전달하지 않는다.

사용자 확인은 우선 전투/상점/회복 클릭의 상승~착지 동행, 두 표시 완료 뒤 팝업, 덱 취소, 일반 승리 후 가림 해제 첫 화면, 자동 이동 뒤 Inspector Size/회전 변경을 본다. 경계·큰 프레임 지연·동일 위치·새 Run 교체는 계획 U1~U8로 확인하며 재현하지 않은 항목은 미확인으로 남긴다. SO 변경에 따른 기존 `DataBaseAssetTests`도 사용자 Unity 확인 항목이다.

이번 작업은 낙하 높이 **20**, 사용자가 조절한 낙하 0.5초·전개 4초·보스 대기 0.5초·복귀 1.2초를 유지했다.
