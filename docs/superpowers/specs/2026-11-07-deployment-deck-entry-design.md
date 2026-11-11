# 배치 단계 안내와 덱 카드 순차 등장 설계

- 날짜: 2026-11-07
- 상태: Phase D1·D2·D3 코드·씬 연결 완료, 컴파일 미확인·Unity 확인 대기.
- 계획: [Task·실행 기록](../plans/2026-11-07-deployment-deck-entry-plan.md)

## 1. 확정 요구와 적용 범위

- 전투 씬 최초 진입: 맵 등장 동안 덱을 숨긴다. 보드·선로·구조물·Nexus·적의 기존 진입 연출이 모두 끝난 뒤 `배치 단계`를 알리고, 알림이 끝나면 기존 네 카드를 왼쪽부터 순차 등장시킨다.
- 사용자 선택 A안: 각 카드가 자기 최종 X 위치 아래에서 올라와 기존 부채꼴에 안착한다. 카드 더미·카드 뒷면·추가 드로우를 만들지 않는다. 카드 내용·순서·최종 크기·각도·정렬은 기존 값을 유지한다.
- 안내는 플레이어 턴·적 턴의 `TurnDisplayUI`를 그대로 사용한다. 검정 `배치 단계`, 같은 확대→축소·선명화→유지→페이드아웃이다. D3에서 배치 전용 Inspector 설정을 분리한다(§8). 초기값은 기존과 같은 등장 0.35초, 유지 3초, 퇴장 0.9초다.
- 덱 열기·닫기 버튼 오브젝트는 기존 구성 그대로 보존하고 비활성화만 한다(사용자 정정). 자동 표시·버튼 입력 연결은 제거한다. 덱 카드는 배치 단계에서만 보이며 필드 유닛 정보창과 함께 표시할 수 있다. 전투·완료에서는 숨긴다. 정보창 자체는 전투 중에도 기존대로 사용할 수 있다.
- 최초 카드 등장까지 기존 진입 대기를 유지해 배치·전투 개시·미리보기·자동 진행을 허용하지 않는다. 카드 내부 정보 입력·Hover도 등장 중에는 막는다.
- 이번 시작 연출은 최초 진입 한 번이다. 이후 밤 종료→배치 복귀는 기존 자동 열기 동작을 유지한다. 맵 연출을 끄거나 생성 타일이 없어도 로딩 완료 뒤 안내→카드는 실행한다.
- 카드 수치 제안은 장당 0.28초, 간격 0.07초(네 장 약 0.49초)다. UI 내부 직렬화 설정으로 조정한다. 안내는 기존 unscaled 시간, 카드 진입은 기존 Combat 진입 Tick 시간을 따른다. 일반 Hover·Drag 슬라이드의 unscaled 시간은 유지한다.
- 사용자 추가 결정: 1-1 튜토리얼과의 순서 연결은 이번 범위에서 제외하고 별도 진행한다. 기존 TutorialViewSystem은 로딩 종료만 기다리고 TutorialPlan의 설명 시작 조건에는 진입 대기가 없어, 맵/안내/카드와 설명이 겹치거나 카드 강조가 먼저 목록을 여는 가능성이 남는다.

## 2. 현재 연결과 변경

| 위치 | 현재 동작 | 변경 |
|---|---|---|
| GameBootstrap.OpenCombatUI | 로딩 연결 뒤 UnitCard 즉시 열기 | 최초 자동 열기를 진입 연출로 이전. 공통 HUD·NightChecker는 유지 |
| CombatBoardEntryPresentation | 맵 진입만 소유 | 맵 구현 유지. 외부 조합이 완료 뒤 배치 UI를 시작 |
| CombatSceneDisplay / Factory | 맵 진입 준비·노출·해제 | 맵+배치 UI를 조합한 진입 객체를 준비·진행·취소. 생성은 Factory |
| TurnDisplayUI | 턴 문구와 자체 표시 수명 | ShowDeployment·IsShowing·HasCompleted 추가. 완료 시간 복제 없이 실제 정상 종료와 외부 숨김 구분 |
| CombatUnitInfo.Render | 국면별 덱 개폐도 처리 | 덱 개폐는 전용 배치 표시 부품으로 이전. 정보 표시만 유지 |
| DeckButtonUI | 버튼과 자식 패널 전환 | UIId.Deck 루트·참조 검증 유지. 버튼 의존성과 입력 제거. 기존 클래스/GUID 유지 |
| DeckCardList / DeckCardFanLayout | 카드 내용·입력 / 부채꼴·Hover 자세 | 순차 등장 시간은 내부 DeckCardEntrance, 실제 Transform 쓰기는 FanLayout 한 곳 |

## 3. 소유권·수명

- `CombatEntryPresentation`은 기존 맵 진입과 `CombatDeploymentPresentation`의 순서를 조합한다. 맵 연출 완료와 배치 UI 진행 상태로 전체 IsPending을 계산하며 별도 완료 타이머를 두지 않는다. 한 프레임 시간을 두 단계에 중복 소비하지 않는다.
- `CombatDeploymentPresentation`은 최초 안내→카드의 순서, 이후 전달된 StagePhase에 따른 카드 개폐를 소유한다. 최초 사실 그리기 전에 준비해 카드를 숨기며, 현재 단계가 Deployment일 때만 시작한다. Core 상태를 쓰거나 별도 규칙을 계산하지 않는다.
- 맵 완료 뒤 안내·카드를 기다리는 동안에도 기존 공개된 기믹의 전환 Tick을 유지한다. 일반 결과 재생은 기존 진입 대기 해제 뒤 재개한다.
- `DeckCardEntrance`는 설정·경과 시간·시작 화면 하단 좌표만 소유한다. 시작 위치는 Canvas 하단과 회전한 카드 윗부분으로 계산해 화면 밖에서 시작한다. 해상도·좌측/중앙 정렬·Y Offset에 고정 픽셀 시작값을 강제하지 않는다.
- `DeckCardFanLayout`은 최종 부채꼴 자세에 진입 Y 변위를 합성한다. Hover와 등장 연출이 같은 Transform을 각각 덮어쓰지 않는다. CardFan 내부 CanvasGroup으로 등장 중 Raycast·Submit·정보 입력을 억제한다.
- Prepare→Open 전에 시작 자세를 만든다. 숨김·재바인딩·취소·씬 해제에서는 카드 자세·입력·Hover를 초기화한다. 정상 완료와 취소를 구분하여 취소가 안내/카드를 새로 열지 않는다. 반복 정리는 안전하다.
- 필수 DeckCardList·TurnDisplay·Canvas·UISystem 누락은 대상과 필드를 포함한 예외로 중단한다. 카드 연출의 시간/간격은 유한한 0 이상이다. 시간 0도 종료 가능하다.

## 4. UI 등록과 Framework 계약

- 등록 단위는 그대로 `DeckButtonUI(UIId.Deck, Parent None, Parallel)` → `DeckCardList(UIId.UnitCard, Parent Deck, Parallel)` → 기존 그룹 멤버 네 장이다. UnitCard의 Exclusive를 Parallel로 바꿔 정보창이 카드 목록을 닫지 않게 한다. 카드 상세는 기존 CombatCardInfo 자식이다.
- TurnDisplay는 기존 UIId.TurnDisplay·Parent None·Parallel·유지 열기를 사용한다. 새로운 UIId·서비스·전역 이벤트를 만들지 않는다.
- 씬 UI는 UISystem.BindScene에서 등록·바인딩하고 기존 초기화 후 개폐한다. 외부 개폐는 IUISystemService만 사용하고 실제 활성 변경은 UIBase/UIGroup 구현이 맡는다(FW-008/009). 카드 내부 CardFan·CanvasGroup은 별도 등록하지 않는 내부 위젯이다.
- UISystem 생성/등록은 기존 GlobalSystemFactory 내부 Builder, 상위 노출은 GlobalSystemGroup.UIServiceCommand, 소비는 기존 CombatDisplayServices.UI와 UIBase.BindUISystemService다. 하위 서비스를 루트에 재등록하지 않는다(FW-001/002/003/004).
- 씬 참조는 CombatSceneRoot, 객체 생성·연결은 CombatSceneDisplayFactory, 공통 Install/Refresh/Clear/Release는 CombatSceneDisplay에 둔다. Framework 기반·Core·FieldCamera·Architecture는 변경하지 않는다.
- DeckUI 프리팹의 DeckButton·CloseDeck 트리와 GUID/fileID를 보존하고 두 GameObject의 m_IsActive만 0으로 설정한다. 버튼 자동 제어용 직렬화 필드는 제거하지만 버튼 자체·자식·컴포넌트는 남긴다. 카드·공용 상세·필드 정보창은 보존한다.

## 5. SSOT·SOLID·기능 독립성 점검

- SSOT: 전투 국면·참가 목록은 Buffer, 턴 안내 수명·시간은 TurnDisplayUI, 부채꼴 기하는 FanLayout, 카드 진입 시간은 Entrance 한 곳이다. 규칙 사실·결과 순번·진입 완료 시간의 복제 없음.
- S: 맵 구현/배치 안내 순서/카드 시간/부채꼴 자세/조립 분리. 기존 정보창에서 덱 개폐 책임을 덜어낸다.
- O: 맵 순서 구현에 카드/안내 분기를 누적하지 않고 기존 진입 계약을 조합한다. 턴 화면의 기존 문구 경로를 확장한다.
- L: ICombatEntryPresentation의 대기·Tick·취소 보장, UIBase/UIGroup 개폐와 정리 보장을 지킨다.
- I: 조합은 맵·배치 표시와 공개된 기믹 Tick만 받는다. UI는 전체 ViewSystem이나 규칙 서비스 대신 필요한 표시 객체·UI 서비스만 받는다.
- D: 조립은 Factory에서 주입하고 화면 전환은 IUISystemService에 의존한다. 프레임마다 씬 검색하거나 전역 이벤트를 도입하지 않는다.
- 변경 시나리오: 카드 속도 수정은 Entrance 설정, 최종 배치 수정은 기존 FanLayout, 알림 표현 수정은 TurnDisplayUI 안에서 끝난다. 이 때문에 맵·소환·턴 규칙 구현을 다시 읽거나 수정할 필요가 없다. 연결 대상을 바꾸는 경우에만 SceneRoot/Factory를 바꾼다.
- 남는 공통 경계는 진입 IsPending, Display의 준비/해제, 기존 턴 UI의 실제 Hide, UISystem의 Parallel 정책, 카드 입력 억제다. 초기/재진입·전투 전환·정보창·Hover/배치·취소를 확인한다.

## 6. 검증

Test 코드를 작성하지 않고 MSBuild·Unity 실행을 하지 않는다. 정적 소스·호출 경로·GUID·프리팹/씬 참조와 diff만 확인한다. **컴파일 미확인**으로 보고하고 Unity 인수 항목은 계획에 남긴다.

## 7. D2 — Entrance 모드 선택과 왼쪽부터 펼치기

2026-11-07 사용자 요청: Entrance에 enum을 두어 기존 상승을 보존하고, 좌측부터 카드가 펼쳐지는 모드를 추가한다.

- `DeckCardEntrance.EntranceMode`: `Rise = 0`(기존), `FanSpread = 1`(추가). 모드 값이 없는 기존 직렬화는 Rise로 유지한다. DeckUI 프리팹은 Rise, 현재 Combat 씬 인스턴스만 FanSpread를 명시적으로 선택한다.
- Rise는 기존 Canvas 하단 시작 위치·순차 Y 상승·회전 유지·시간 계산을 유지한다.
- FanSpread는 첫 카드의 최종 위치/각도를 기준으로 네 장의 간격·아치·각도를 좁힌 자세에서 시작한다. 전체 카드가 짧게 위로 나타나는 동안, 왼쪽부터 `Card Seconds`와 `Stagger Seconds`에 따라 X/Y 간격과 회전을 기존 최종 부채꼴로 펼친다. 공용 카드 더미·새 카드·규칙 입력은 만들지 않는다.
- 추가 조절값: `Folded Spacing Ratio` 0.12(최종 간격/각도 차이 대비 접힌 비율), `Fan Lift Ratio` 0.12(카드 높이 대비 처음 내려둔 거리), `Fan Reveal Seconds` 0.10(전체 카드의 짧은 상승/선명화). 전체 등장과 펼침은 겹쳐 진행하므로 총 시간은 `max(네 장 순차 시간, Fan Reveal Seconds)`다. 기존 Card Seconds 0.28·Stagger Seconds 0.07이면 약 0.49초다.
- 모드는 Begin에서 고정한다. 잘못된 enum·음수/비유한 시간·비율 범위 오류는 시작 전 예외다. 시간 0, 숨김/취소 후 투명도·자세·입력 복원은 두 모드가 같은 수명을 따른다.
- SSOT/SOLID/독립성: 최종 부채꼴 기하는 FanLayout 한 곳, 모드별 진입 자세·시간은 Entrance 한 곳이다. Entrance가 받은 최종 자세로 시작/중간 자세를 계산하고 FanLayout만 Transform·CanvasGroup에 쓴다(S/D). 모드 확장은 같은 좁은 자세 반환 경로를 사용하고(O/I), 기존 Begin/Tick/Reset·IsPlaying 보장을 유지한다(L). Factory·진입 순서·턴 알림·입력 Policy·Core를 다시 수정할 필요가 없다.
- FW-008/009 등록 단위·개폐 경로 및 FW-001~004 서비스 조립은 D1 그대로다. 추가 클래스 파일/서비스/UIId/이벤트 없음. 변경 검증은 모드 선택, 완성 자세 동일성, 입력 억제, 취소 복원, 직렬화 호환에 한정한다. 튜토리얼 연결과 버튼 비활성 정책은 유지한다.

## 8. D3 — 배치 단계 안내 설정 분리

2026-11-07 사용자 요청: TurnDisplayUI의 조절값 중 배치 단계는 별도로 조절한다.

- 기존 `introScale/introSeconds/holdSeconds/fadeOutSeconds`의 이름·씬 값을 유지하고 Inspector 헤더를 `플레이어 턴·적 턴 연출`로 명확히 한다.
- 같은 컴포넌트의 `배치 단계 연출`에 `deploymentIntroScale/deploymentIntroSeconds/deploymentHoldSeconds/deploymentFadeOutSeconds`를 추가한다. 기본값과 현재 씬은 기존 1.6/0.35/3/0.9를 복사해 분리 자체로 재생 속도를 바꾸지 않는다.
- ShowDeployment는 배치 설정, ShowPlayerTurn/ShowEnemyTurn은 기존 설정을 선택한다. 매 표시 요청에서 선택을 갱신하고 숨김 때 초기화한다. 공통 Open/Update/ApplyLook이 선택된 값을 사용하므로 완료 판정과 모습에 다른 시간이 적용되지 않는다. 선택된 설정의 유한성·시간 0 이상·배율 1 이상을 Open 전에 확인한다.
- SSOT: 두 알림의 독립 조절값은 TurnDisplayUI만 소유하며 외부 진입 조합은 여전히 실제 HasCompleted를 기다린다. 타이머/보간 구현은 하나다. S: 표현 설정은 UI 내부, 순서와 조립은 기존 담당 유지. O: 설정 선택만 확장. L: Open/Hide·완료/취소 계약 유지. I/D: 공개 API와 IUISystemService 의존성 유지, 새 인터페이스 없음.
- FW-001~004 조립·서비스 노출, FW-008/009 UI 등록·개폐 경로는 §4 그대로다. 새 UI/서비스/직접 SetActive 없음. 배치 알림 속도 변경은 TurnDisplayUI·씬 설정만으로 가능하고 카드/맵/Presenter/Factory를 수정하지 않는다. 공통 경계인 동일 화면 재사용·정상 종료·외부 숨김만 함께 확인한다.
