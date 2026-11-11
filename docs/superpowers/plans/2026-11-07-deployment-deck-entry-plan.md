# 배치 단계 안내와 덱 카드 순차 등장 Task·실행 기록

- 날짜: 2026-11-07
- 설계: [배치 단계 안내와 덱 카드 순차 등장](../specs/2026-11-07-deployment-deck-entry-design.md)
- 사용자 승인 범위: A안, 카드 앞 배치 단계 안내(기존 턴 알림 방식), 배치 외 카드 숨김, 덱 열기·닫기 버튼 비활성화(오브젝트 보존).
- 후속 확인: 튜토리얼 시작 대기는 사용자가 별도 진행으로 지정하여 제외했다.

## Phase D1 — 최초 진입 연결과 배치 전용 UI

| Task | 작업 | 상태 |
|---|---|---|
| D1.1 | 기존 TurnDisplayUI에 배치 안내·표시 완료 조회 추가 | 코드 완료 |
| D1.2 | 카드 내부 순차 등장·입력 억제·취소, 기존 부채꼴 자세와 합성 | 코드 완료 |
| D1.3 | 맵→안내→카드 진입 조합과 이후 국면별 카드 개폐 | 코드 완료 |
| D1.4 | Factory·SceneRoot·Display·Bootstrap 연결, 정보창의 덱 개폐 책임 이전 | 코드 완료 |
| D1.5 | 덱 버튼 자동 제어 제거·프리팹 버튼 비활성화, Combat 씬 카드 참조 연결 | 연결 완료 |
| D1.6 | 정적 검토·문서/공유 메모 갱신·Unity 확인 항목 인계 | 완료, Unity 확인 대기 |

위 Task는 하나의 진입 동작을 구성하는 단일 구현 Phase다. D1 끝에서 멈추고 사용자 Unity 검토를 받는다. 조정이 필요하면 다음 Phase로 범위를 기록한 뒤 진행한다.

## Unity 인수 항목

1. 일반/개발 전투 진입: 맵·적 등장 동안 덱/버튼이 보이지 않으며 배치 입력이 되지 않는다.
2. 맵 완료→검정 `배치 단계`가 기존 턴 글자와 같은 확대·선명화·유지·페이드아웃→종료 뒤 카드 A안. 카드 네 장 순서·최종 위치·각도·크기는 기존과 같다.
3. 카드가 다 안착할 때까지 본체/정보/능력 Hover·클릭·Submit·Drag와 전투 개시가 차단된다. 완료 뒤 기존 배치·Hover·정보 고정이 동작한다.
4. 배치 중 필드 유닛 정보창을 열고 닫아도 카드가 유지된다. 전투 개시→카드와 카드 상세 숨김. 플레이어 턴/적 턴 알림은 기존대로 유지.
5. 밤 종료→배치 자동 복귀, 전투 완료/이탈→카드 숨김. 최초 안내/카드 연출은 일반 상태 갱신에서 반복되지 않는다.
6. 맵 등장 설정 끔/생성 타일 없음에서도 로딩 완료 뒤 안내→카드가 실행된다.
7. 좌측/중앙 정렬, Y Offset, 화면 비율을 바꿔도 카드가 화면 아래에서 시작하고 최종 부채꼴과 Hover 영역이 맞는다. 카드 시간/간격 0도 끝난다.
8. 안내/카드 중 씬 이탈·오류·부모 숨김·재바인딩에서 늦게 열리지 않으며 다음 진입에 잔여 자세·입력 차단이 없다. 기존 일시정지/재개 시계 정책도 확인한다.
9. 1-1 튜토리얼의 기존 설명·카드 강조가 진입과 겹칠 수 있다. 사용자 지시로 시작 순서 연결을 별도 작업으로 남겼으며 D1에서 해결됐다고 판정하지 않는다.

## 실행 기록

- 착수 전 사용자 변경: FMODStudioCache, Akimbo/Ballistar/CactusCannon/ShotGun 프리팹, CombatUnitActionCursorPresenter와 해당 계획. 이번 작업 범위와 분리하여 보존한다.
- Test 코드 작성·수정, MSBuild, Unity 실행, 커밋·push는 하지 않는다. **컴파일 미확인**.
- D1: 맵 구현은 유지하고 CombatEntryPresentation이 맵→배치 안내→카드를 조합한다. 안내는 기존 TurnDisplayUI의 실제 정상 종료를 기다리며 외부 숨김이면 카드를 뒤늦게 열지 않는다. 카드의 시간은 DeckCardEntrance, 자세·입력 억제는 DeckCardFanLayout, 등록·내용·수명 연결은 DeckCardList에 있다.
- 배치→전투/완료의 카드 개폐를 CombatUnitInfo에서 CombatDeploymentPresentation으로 이전했다. UnitCard는 Parallel이므로 배치 중 필드 정보창을 열어도 유지된다. 이후 배치 복귀는 기존 자동 열기를 유지한다.
- 초기 버튼 삭제를 사용자 정정에 따라 취소하고 DeckButton·CloseDeck의 원래 18개 YAML 레코드(버튼·자식·컴포넌트)를 복원했다. 두 GameObject만 비활성화했다. 카드의 기존 씬 Y Offset(-135)·각도(23.6)·중앙 정렬 override와 상세창 참조는 보존했다.
- 정적 검토: 수정 씬/프리팹의 중복 fileID 없음, 기존 대비 신규 미해결 로컬 참조 없음, 삭제한 자동 버튼 API의 호출 잔존 없음, 신규 C# 세 파일의 .meta 생성, UTF-8/LF·diff 형식 확인. 서비스 등록/조회 경로와 진입 대기→입력 재개 호출 경로를 소스로 대조했다. 실행 검증을 의미하지 않는다.
- 조정 위치: Combat 씬 DeckPannel의 DeckCardList → Entrance → Card Seconds / Stagger Seconds. 배치 안내는 D1에서 턴 연출 설정을 공유했으며, D3에서 TurnDisplay → 배치 단계 연출로 분리했다.
- 공용 조립 메모: [Framework 공유](../handoffs/2026-11-07-deployment-deck-entry-framework-share.md), 미전달.

## Phase D2 — Entrance 모드와 FanSpread 추가

사용자 요청: 기존 상승을 살리고 Entrance enum으로 좌측부터 펼치는 연출 추가. 설계 §7을 따른다. 이 Phase 끝에서 Unity 검토 지점으로 멈춘다.

| Task | 작업 | 상태 |
|---|---|---|
| D2.1 | Rise=0/FanSpread=1·접힌 간격/짧은 상승/선명화 설정·자세 계산 | 코드 완료 |
| D2.2 | FanLayout의 공통 자세/CanvasGroup 적용과 Reset 복원 | 코드 완료 |
| D2.3 | 프리팹 Rise 기본·Combat 씬 FanSpread 선택, 기존 조정값 보존 | 연결 완료 |
| D2.4 | 소스/직렬화 정적 확인·설정 안내·Unity 인수 기록 | 완료, Unity 확인 대기 |

Unity 확인: Rise의 기존 네 장 상승, FanSpread의 왼쪽→오른쪽 간격/회전 펼침, 두 모드의 동일한 최종 자세, 좌측/중앙 정렬과 Y Offset, 시간/간격/Reveal 0, 진행 중 Hide/Cancel 뒤 다음 진입의 alpha=1·입력 복원, 기존 Hover/Drag/상세 고정. Test 코드·MSBuild·Unity 실행 없음, **컴파일 미확인**.

D2 실행 기록: 모드와 값 검증을 Begin에 추가하고, 같은 Entrance의 Pose 값 반환을 FanLayout이 적용한다. Rise의 기존 Canvas 하단/회전 범위·ease-out 계산은 보존하고 FanSpread는 첫 카드의 최종 자세로부터 접힌 위치·각도를 계산한다. 모드는 Begin에서 고정한다. FanLayout의 CardFan CanvasGroup이 입력 억제와 전체 선명화를 함께 맡고 Reset은 기본 자세·alpha=1로 복원한다. 기존 Card Seconds/Stagger Seconds·씬 Fan Layout·TurnDisplay 값·두 버튼 비활성화·튜토리얼 별도 범위는 유지했다. 새 .cs 파일·공용 Factory/서비스/UI 등록 변경 없음. 코드 호출 연결·enum 수치/씬 override·참조·diff를 정적으로 확인했으며 Unity 실행 결과로 간주하지 않는다.

## Phase D3 — 배치 단계 안내 설정 분리

사용자 요청과 설계 §8에 따라 단일 Phase로 진행하고 완료 후 Unity 확인 지점에서 멈춘다.

| Task | 작업 | 상태 |
|---|---|---|
| D3.1 | 배치 전용 배율·등장/유지/퇴장 시간, 공통 재생의 설정 선택 | 코드 완료 |
| D3.2 | 현재 Combat 씬에 기존과 같은 초기값 명시, Inspector 안내 갱신 | 연결 완료 |
| D3.3 | 직렬화·호출/수명·diff 정적 확인, Unity 인수 기록 | 완료, Unity 확인 대기 |

Unity 확인: 배치 값 네 개를 바꿔도 플레이어/적 턴에 영향 없음, 역방향도 동일, 알림 종료 후 카드 시작, 시간 0, 배치→턴/턴→배치 표시 교체와 외부 숨김 후 재사용. Test 코드·MSBuild·Unity 실행 없음, **컴파일 미확인**.

D3 실행 기록: TurnDisplayUI의 기존 네 필드는 보존하고 배치 전용 네 필드와 헤더를 추가했다. ShowDeployment/ShowPlayerTurn/ShowEnemyTurn이 각 설정을 선택하며, 공통 Open·Update·ApplyLook에서 동일한 선택을 사용한다. Open에서 선택된 설정을 검증하고 Hide에서 선택을 초기화한다. 참조 인스턴스는 Combat 씬 하나이며 새 네 값을 기존과 같은 1.6/0.35/3/0.9로 명시했다. 기존 턴 값·카드 설정·외부 진입 완료 대기·서비스 조립·튜토리얼 범위는 변경하지 않았다.
