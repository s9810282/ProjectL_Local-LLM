# Core 표시 데이터의 Buffer 경계 — 2026-10-27

사용자 지시로 밤 턴 상한과 보유 능력치 표시의 Buffer 경계를 수정했다. 이후 사용자 요청에 따라 DeckEdit·Heal/회복 관련 직전 변경은 되돌렸다. 덱의 동기 읽기·평가·확정과 회복의 기존 Core 조회 경로를 복원했으며, 전용 DeckEditBuffer·RecoveryStateBuffer와 관련 등록·게시·비동기 대기 처리를 제거했다.

## 유지한 설계

- `CombatTurnFacts.PlayerTurnsPerNight`는 Core가 턴 사실을 생성할 때 채운다. NightCheckerUI는 CombatStateBuffer의 같은 턴 스냅샷에서 완료 턴과 상한을 읽고 비율을 계산한다.
- `CharacterStatsBuffer`는 Core가 게시한 불변 결과와 원본 버전을 보관한다. 원본은 Run ID·로스터 버전·Run 결과·전투 유무다. 로스터 버전은 보유·영혼·메인 지속 상태 변경을 포함한다. UI의 읽기는 DB·Core 계산을 호출하지 않는다.
- 게시 전·원본 변경 후에는 `Pending`을 반환한다. UI는 요청 Sink에 재게시 의도를 남긴다. Core Tick은 요청된 최신 원본만 계산하며, 계산 중 원본이 교체되면 게시를 거절하고 다음 Tick에서 재시도한다.

## 생성·등록·조회 경로

| 대상 | 생성·내부 등록 | Group 보관·상위 Export | 소비자 |
|---|---|---|---|
| 보유 능력치 | BufferSystemFactory → CharacterStatsBuffer → Buffer 내부 Builder의 Read/Request/Store | BufferSystemGroup의 프로퍼티; 루트에는 기존 IBufferQuery만 공개 | MainCharactorUI·UnitCardUI → Buffer Read/Request → OwnedCharacterStatsReader |
| Core 게시 | InGameCoreSystemFactory → CharacterPresentationPublisher → Core 내부 Module 등록 | InGameCoreSystemGroup이 Tick 전달; View보다 먼저 실행 | CharacterStats Buffer Store |

이번에 유지한 변경은 Global 하위 서비스 재노출이나 루트 resolver 직접 조회를 추가하지 않는다. DeckEdit·회복의 복원된 경로는 이 Buffer 변경 범위에서 제외한다.

## UI 등록·수명

새 화면 등록 단위는 없다. 기존 UIId·부모·Policy·UISystem 개폐와 UIInputBinding/IUIInputReceiver를 유지한다.

HUD의 Actioned 표시와 Count 활성 연동 분리, 사용하지 않는 행동 순서 표시 비활성화, CombatActionPanel의 상태 변경 전 필수 참조 검증도 유지한다.

## 검증

복원 후 Unity C# 응답 파일로 Contracts·Domain·Application·Systems·Framework 테스트 어셈블리 컴파일을 통과했다. Unity 포함 Mono에서 인자 없는 Test 메서드 기준으로 유지한 Buffer 경계·덱·보유 능력치 26개와 회복 상태 조회·Heal 처리·회복 명령 20개, 총 46개가 통과했다. TestCase는 실행하지 않았다. Unity 포함 Mono의 관리 코드 검증과 Unity Editor Test Runner/Play 검증은 구분한다. 실제 Heal 화면과 씬 전환은 Unity에서 별도 확인이 필요하다.
