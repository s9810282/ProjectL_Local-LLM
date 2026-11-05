# 로비 Clicker (2026-10-20)

## 승인 범위와 설계

사용자가 만든 Lobby 씬 Clicker에 UIBase를 연결한다. Exclusive 정책으로 시설 안내를 표시하고,
다른 일반 화면으로 교체된 뒤 마지막 일반 화면이 닫히면 복귀한다.
시설 팝업은 아래 Phase 2의 사용자 승인 예외로 별도 수명 경계에서도 안내를 숨기고 복귀시킨다.

- 등록 단위: Clicker 한 개, UIId.Clicker(33), ParentID.None, Policy.Exclusive.
- 씬의 기존 Clicker 오브젝트와 세 안내 자식은 유지한다. 표시 변경은 Clicker.Open/Hide만 수행한다.
- UISystem.BindScene이 비활성 객체까지 수집해 등록·바인딩하고 Initialize에서 초기 표시를 숨긴다.
- 기존 GameBootstrap.ShowLobbyUI(로비 EnterLoaded 200)가 Global UI 표시 후 IUISystemService.OpenUI(Clicker)를 호출한다.
- UIBase.RestoreWhenHistoryEmpty는 기본 false이며 Clicker만 true다. 복귀 화면은 씬의 Exclusive 루트 하나로 제한하며 잘못된 등록은 초기 검증에서 UIId를 포함한 예외를 던진다.
- UISystem은 복귀 화면을 명시적으로 연 뒤에만 복귀 대상으로 기억한다. 다른 Exclusive UI의 교체는 기존 동작을 유지한다.
- CloseUI/BackCloseUI로 마지막 일반 이력이 없어지면 OpenUI로 복귀한다. 여러 일반 UI가 열려 있으면 마지막 UI까지 기다린다.
- Clicker의 명시적 닫기(Clicker에서 뒤로가기 포함), HideAllUI, BindScene, Shutdown은 복귀 예약을 해제한다. 씬 전환 이후 이전 로비 화면을 재개하지 않는다.
- 기존 Parallel·고정 표시·유지 UI 정책은 유지한다. 외부 SetActive나 MonoBehaviour.OnDisable로 등록 상태를 우회하는 닫기는 지원하지 않는다.

## Task · Phase 1

1. Clicker 및 UIId 추가, 기존 씬 오브젝트에 컴포넌트 연결.
2. UIBase의 선택적 복귀 계약과 UISystem의 이력 종료 처리 추가.
3. 기존 로비 진입 지점에서 OpenUI 호출.
4. UIGroupTests의 복귀 회귀와 LobbySceneAssetTests의 씬 연결 검사 추가, 컴파일 확인.

Phase 1 코드 완료. Unity 실행 확인은 아래 잔여 검증을 따른다.

## 구조 검토

- SSOT: 열린 UI·이력·복귀 예약은 UISystem만 소유한다. Clicker는 표시만 담당하며 팝업별 복귀 상태를 복제하지 않는다.
- SOLID: 표시(S)와 진입 조립을 분리하고, 선택적 UIBase 계약(O/L)으로 기존 UI 동작을 유지한다. 새 서비스 인터페이스를 늘리지 않으며(I), 외부 개폐는 IUISystemService에 의존한다(D).
- FW·계층: GlobalSystemFactory가 UISystem을 생성하고 Global 내부 Builder에 IUISystemService와 모듈을 등록한다. GlobalSystemGroup이 GlobalSystemServiceKey.UI로 보관하며 루트에는 IGlobalSystemGroupService만 Export한다. GameBootstrap은 SystemGroupsService.Global → UIServiceCommand를 사용한다. 추가 서비스·루트 재등록은 없다.
- 독립성: 시설 동작·팝업·Core·FieldCamera·Architecture는 수정하지 않는다. 후속 안내 외형은 Clicker와 씬에서 수정할 수 있다. 복귀 동작의 공통 경계는 UISystem이므로 기존 Exclusive/Parallel 이력 회귀를 함께 확인한다.

## Phase 2 · 시설 팝업 별도 수명 예외

사용자 요청: “시설 UI가 UIBase에 없네 … 별도 예외동작으로 추가하고 문서에도 기재”.
시설 UI의 등록 여부만으로 안내의 숨김·복귀를 결정하지 않도록 예외를 승인했다.
현재 덱·아티팩트 화면 클래스 자체는 UIBase이며, 이 예외가 시설 화면 전체의 미등록 상태를 뜻하지는 않는다.

### Task와 구현

1. 로비 팝업 호스트의 기존 SetPopupBlocked를 시설 개폐 경계로 사용한다.
2. 시설 수명이 시작되면 IUISystemService.CloseUI(Clicker)로 안내를 숨긴다.
3. 시설 수명이 끝나고 입력 Layer가 Lobby로 복귀했으며 출발 요청이 대기 중이지 않을 때 OpenUI(Clicker)로 안내를 연다.
4. 중복 알림·종료 전 복귀·폐기/교체된 연결·출발 중 복귀를 검사하는 회귀를 추가한다.

- 수명 소유자는 기존 LobbyViewScope다. `_clickerSuppressedByFacility`는 이 연결이 안내를 숨겼다는 소유권만 기록한다. 열린 팝업이나 입력 Layer의 사본은 만들지 않는다.
- PopupViewScope → IPopupViewHost.SetPopupBlocked → LobbyViewScope → IUISystemService → Clicker.Open/Hide 경로를 사용한다. Clicker 외부에서 직접 SetActive하지 않는다.
- CloseUI는 일반 이력의 자동 복귀 예약도 해제한다. 따라서 시설 수명이 끝나기 전에 UISystem이 안내를 먼저 복원하지 않는다. 정상 복귀의 OpenUI가 다음 일반 UI 교체를 위한 예약을 다시 설정한다.
- 시설 팝업이 실제로 닫혔어도 입력 Layer가 아직 시설/다른 모달의 소유라면 복귀하지 않는다. 반복 true/false는 중복 개폐하지 않는다.
- Dispose·Supersede·공유 오류 이후의 늦은 알림과 출발 대기 중에는 복귀하지 않는다. 폐기 시 숨김 소유권도 버린다.
- 시설의 외부 숨김은 기존 PopupViewScope의 Tick 정리를 따른다. HideAllUI로 시설까지 숨긴 경우에도 그 정리가 로비 입력을 정상 복원하면 이 예외가 Clicker를 다시 연다. 일반 UI만의 HideAllUI 예약 해제와 구분한다.
- UIId.Clicker의 등록은 필수다. 잘못된 구성은 기존 UISystem 예외로 드러내며 미등록 오류를 무시하지 않는다.

### 규약 예외와 적용 범위

UIBase 등록이 기본이라는 프로젝트 규약을 시설 UI 전체의 일괄 재구성 근거로 사용하지 않는다.
이번 예외는 기존 시설 수명 경계에 안내 표시 연결을 추가하는 범위이며, 새로운 미등록 패널·별도 표시 서비스·입력 구독을 도입하지 않는다.
시설별 내용·입력·UI 등록 구조는 유지한다. 추가 서비스 등록과 루트 조회 우회는 없다.
시설 자체의 동작 수정 없이 안내를 제어하되 공통 PopupViewScope와 UISystem에 시설 종류 분기를 넣지 않는다.

Phase 2 코드 완료. 실제 Unity 재생과 Test Runner 실행은 미확인이다.

## 검증

- Systems 전체 소스 + 새 Clicker를 현재 Unity 참조 DLL과 Roslyn C# 9로 컴파일: 오류 0. Unity 직렬화 필드의 기존 미할당·미사용 경고는 제외했다.
- Framework EditMode 테스트 소스를 변경된 Systems DLL에 대해 컴파일: 오류 0. 기존 LobbyDepartureIntegrationTests의 중복 using 경고 1개.
- 추가 회귀 6건: 반복 교체와 Close/Back 복귀, Parallel 종료 대기, 전체 숨김·명시 닫기 예약 해제(2건), 첫 Open 전 복귀 금지, 부모 등록 거부.
- 추가 에셋 검사 1건: 실제 Lobby 씬에 Clicker 1개, Exclusive·루트 등록, 안내 자식 소유 확인.
- Phase 2 추가 회귀 4건: 중복 숨김과 Lobby Layer 복귀 대기, 폐기·교체된 연결의 늦은 복귀 방지(2건), 출발 대기 중 복귀 방지. LobbyViewSystemTests에 추가했다.
- Unity Test Runner 실행·실제 재생은 미확인이다. UIGroupTests(기존 Exclusive 비복원 회귀 포함)와 LobbySceneAssetTests를 실행한다.
- 재생 확인: 로비 진입 → 안내 표시 → 덱 시설 도착 → 안내 숨김 → 닫기 → 안내 복귀 → 아티팩트에서도 반복 → 로비 이탈·재진입 시 다시 표시.

컴파일 산출물은 OS 임시 폴더 projectl-clicker-check에만 생성했다. 커밋·push는 하지 않았다.
