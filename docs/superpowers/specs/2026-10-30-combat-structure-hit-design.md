# Structure 피격 표시 설계

- 날짜: 2026-10-30
- 상태: **2026-11-01 코드·에셋 구현 및 코드 리뷰 완료 / Unity 실행 인수 미완료**. 10-30 문서 보완 뒤 11-01 사용자의 전체 Task 진행 지시로 구현했다. 실제 완료 범위·미실행 검증은 Task 실행 기록과 구현 리뷰를 따른다.
- 요청: 적 Structure가 맞는 시점에 체력·피해 숫자·피격 이펙트를 함께 표시한다.
- Task: [Structure 피격 계획](../plans/2026-10-30-combat-structure-hit-plan.md).
- 원칙: SSOT·SOLID 5원칙·JMH Framework/FW 계약·[기능 변경 독립성 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리).
- 기존 문서의 과거 상태를 현재 구현으로 추정하지 않고, 아래 현행 코드를 대조했다. 진행 중인 특성 유닛 행동 변경과 동일 파일이 겹치므로 구현 착수 때 작업 트리를 다시 확인한다.

## 1. 현재 코드와 문제

| 확인한 코드 | 현재 동작 |
|---|---|
| `CombatEventTypeTable`의 `CombatStructureEvent` 행 | 주체·피해 근원이 있으며 처리기는 Facts다 |
| `CombatHitFollowUps`·`CombatHitGate` | 피해 근원의 공격 그룹·가해자로 Structure 후속도 대응하고 대상 해제 뒤 시작한다 |
| `CombatClipAttack`·`CombatFirePiece` | 근접 해제는 ClipEvent + HitDelay, 원거리 해제는 접촉이다 |
| `CombatFactEventHandler.ApplyFacts` | Structure Final만 표시 사실에 반영한다. 숫자·Structure 전용 큐는 없다 |
| `FieldUnitPresenter.RenderFacts` | 살아 있는 적 Structure에 HUD를 붙인다. 사망 Structure는 즉시 표시에서 제외한다 |
| `CombatShownFacts.StandingBodyOf(EntityId)` | 살아 있는 Structure 몸 중심·회전을 이미 조회할 수 있다 |
| `UnitVisualDataSO.cs`의 `UnitVisualData` | Structure도 Definition.Key로 외형을 찾는다. 장애물 전용 큐만 있고 일반 Structure 피격 큐는 없다 |
| `UnitDamageText` | 숫자 연출·시간을 소유한다. HUD 자식이 비활성화되면 숫자도 정리된다 |

따라서 설정만으로 해결할 수 없다. 피격 사건을 기존 시점에 받는 경로는 재사용하고, Structure 피드백과 설정 칸을 추가해야 한다. 특히 치명타에서 HUD 자식에 숫자를 낸 직후 Structure를 회수하면 숫자가 화면에 남지 않는다.

## 2. 범위와 결정

### SH1. 대상과 타이밍

- 대상은 네이티브 장애물이 아닌 적 Structure의 실제 HP 감소다. 아군 Structure HUD 정책 확대는 범위 밖이다.
- `Expected.Health.CurrentHealth > Final.Health.CurrentHealth`일 때 피격이다. 회복·동일 HP·위치 변경만 있는 사건은 피격 숫자와 큐를 내지 않고 기존 사실 갱신은 유지한다.
- 근접은 ClipEvent + HitDelay, 원거리는 기존 접촉 해제를 따른다. 별도 HUD 시계·타이머·공격 그룹 대응표를 만들지 않는다.
- 여러 대상·관통은 기존 대상별 Gate 정책을 유지한다. 이번 변경으로 모든 대상을 첫 접촉에 강제로 풀지 않는다.
- 공격 출처가 없는 환경 피해는 그 사건의 시작 시점이다. 출처 없는 사건을 임의로 공격에 붙이지 않는다.

### SH2. 한 사건의 표시 순서

1. 기존 스케줄러와 Gate가 Structure 사건 시작을 허용한다.
2. Final 반영 전, 현재 표시의 Structure 몸 자리와 숫자 앵커를 확보한다. 치명타에서도 살아 있는 표시로 조회한다.
3. 해당 외형의 피격 큐를 곁들임 실행(Accent)으로 시작하고, 실제 감소량 `Expected HP - Final HP`의 피해 숫자를 시작한다.
4. Final을 `CombatShownFacts`에 반영하고 다시 그린다. 생존이면 새 HP, 치명타면 즉시 파괴 표시다.

2~4는 같은 사건 호출에서 진행한다. 체력과 숫자는 같은 프레임에 바뀌고 이펙트도 같은 시점에 발동한다. 체력바 감소 보간은 기존 HUD 설정을 유지한다. 치명타는 HP 0 사실을 반영한 뒤 모델/HUD를 숨기며, 0 HP HUD를 별도 시간 동안 유지하지 않는다.

### SH3. 큐 설정의 원본

- `UnitVisualData`에 `structureHitCues`를 추가한다. 조회 키는 사건의 Structure `Definition.Key`다. 별도 Structure 외형 테이블·공용 피격 기본값·캐릭터 반응 설정 복사는 만들지 않는다.
- 기존 `CombatVisualCueInput` → `CombatEventCues`를 사용한다. 구체 입력 타입의 직렬화 형태는 기존 외형 필드와 맞춘다.
- `CombatCueEvent.StructureHit` 규칙을 추가한다. 이번 범위는 **이펙트만**, 기준 시점 Start, 지연 0, 자리 SubjectBody다. 기존 위치·회전 보정을 허용한다. 소리·플래시·카메라 흔들림·End/Cue/Contact 앵커는 허용하지 않는다.
- 빈 큐는 명시적으로 이펙트 미사용이다. 체력·숫자는 그대로 표시한다. 운영 확인용 Structure에는 실제 이펙트를 연결해야 한다.
- 네이티브 장애물 키에 이 칸을 넣으면 DB 검증 오류다. 캐릭터 키와 Structure 키의 분류를 위해 InGame DB를 읽지 않는다. 그 외 잘못된 외형 배정은 에셋 교차 검증과 실제 Structure 소비 경로로 확인한다.
- 잘못된 큐는 DB 생성 시 대상 키·필드·이유를 포함한 예외로 거절한다. 새 직렬화 필드가 없는 기존 에셋은 빈 큐로 읽혀야 한다.

### SH4. 기능 구현과 조립

- `CombatStructureEventHandler`를 별도 처리기로 추가한다. 피격 판정·자리 확보·피드백·Structure 사실 반영을 소유한다. Facts 처리기의 Structure 갈래는 제거해 반영 주체를 하나로 만든다.
- `CombatEventTypeTable`의 처리기 슬롯과 `CombatEventHandlerTable`을 연결한다. 사건 주체·근원·경로는 유지한다. Presenter는 생성·연결만 담당한다.
- 큐 값 조회는 `CombatCueSources`의 Structure 키 조회 입구가 맡는다. **Structure 큐 캐시는 `CombatMomentCuePlayback` 한 곳**이 EntityKey로 소유하고 연결 해제 때 폐기한다. Sources·처리기에는 캐시를 추가하지 않는다. 기존 순간 큐 캐시는 유지하며 같은 Structure 큐를 이중 보관하지 않는다.
- 실행은 기존 `CombatMomentCuePlayback.PlayAccent` → `CombatCueComposer` → `CombatEffectWindow`를 사용한다. 전용 Moment 종류를 추가해 같은 사건을 일반 틀에서도 중복 재생하지 않는다.
- `CombatCuePoints.ForSubject`에 SH2에서 확보한 몸 자리를 전달한다. 공격 접촉 이펙트와 Structure 피격 이펙트는 별도 설정이므로 둘 다 있으면 둘 다 나온다.
- 이 처리기의 `TryBeginBeforeFacts`는 피드백만 내고 false를 반환해 기존 틀이 `ApplyFacts`·Render·완료를 수행하게 한다. 별도 사건 완료 콜백이나 대기는 만들지 않는다. 일반 사건 소리는 기존 경로를 유지한다.

### SH5. 치명타 피해 숫자의 수명 — 리뷰 상세안

- Structure 숫자는 생존·치명타 모두 **모델 대여와 분리된 동일한 숫자 위젯 경로**를 사용한다. 치명타만 특수 타이머로 모델을 붙잡지 않는다.
- 기존 등록 UI `UnitHUDLayer` 안의 세부 위젯으로 두고, 전용 숫자 Presenter가 생성·풀·회수·취소를 소유한다. Layer는 투영·가시성만 담당한다는 기존 책임을 유지한다.
- 숫자 모양·시간의 원본은 기존 `UnitDamageText` 템플릿 하나다. HUD 템플릿의 숫자 위젯에서 생성하는 경로를 조립 시 연결한다. 별도 duration 값을 복사하지 않는다.
- `UnitDamageText`의 자연 완료 통지 계약과 재사용 보호는 SH8을 따른다. 기존 Update가 유일한 숫자 시계이며 Presenter는 경과 시간이나 완료 여부를 매 프레임 검사하지 않는다.
- 표시 시점의 월드 앵커를 값으로 보관하고 기존 HUD Layer의 출력 카메라로 투영한다. 이후 풀에서 같은 EntityId/모델이 재사용돼도 새 대상을 따라가지 않는다. 화면 밖에서는 숨기되 재생은 계속한다.
- 숫자는 결과 사건 진행·다음 행동을 막지 않고 자체 연출 완료 시 회수한다. 정상 결과 완료로 지우지 않으며 전투 취소·오류 정지·씬 해제 시 즉시 회수한다. 이펙트 자연 종료·취소는 기존 EffectWindow 계약을 따른다.
- 활성 적 Structure에서 필수 숫자 템플릿·앵커·Layer 연결이 없으면 구체적인 구성 예외다. 화면 밖·이미 표시 대상이 아닌 정상 조회 실패와 구성 오류를 구분한다.
- 새 독립 화면이나 전역 서비스를 등록하지 않는다. 기존 `UnitHUDLayer`의 UIId·Parallel 정책·UISystem 개폐 수명을 재사용한다. 숫자는 화면 내부 위젯이라 개별 UIBase 등록이 필요하지 않다.

### SH6. 장애물과 파괴

- 네이티브 장애물은 `ObstacleContentProfile.IsNativeObstacleKey`로 기존 분류를 재사용한다. 그 키의 Structure 사실은 계속 반영하되 새 일반 Structure 피드백은 내지 않는다.
- 기존 장애물 반응 사건과 `ObstacleHitCues`·`ObstacleDestroyedCues`·소리는 그대로다. 장애물 피해 숫자 확대는 별도 범위다.
- 일반 Structure 치명타에도 같은 피격 이펙트를 한 번 낸다. 파괴 전용 이펙트·파괴 클립·잔해·사망 유지·넉백은 이번 범위에 없다.

### SH7. 투영 등록과 필요한 창구

- 현재 `UnitHUDLayer.Register(UnitHUD, IUnitHUDAnchor)`는 일반 숫자 위젯을 받지 않는다. 따라서 기존 API를 그대로 쓸 수 있다고 가정하지 않고 **UI 투영 등록 계약을 확장**한다.
- Layer 내부에서 공통 투영 항목(RectTransform·CanvasGroup·월드 앵커·가시성)을 처리한다. HUD 등록은 기존 API를 유지하는 어댑터이고, 숫자 등록은 일반 위젯 입구다. 투영 계산은 하나만 둔다. Layer는 Structure·피해량·숫자 완료·풀을 알지 않는다.
- 등록은 해제 가능한 대여를 반환한다. 중복 등록은 구성 오류, 대여 해제는 반복 가능하다. HUD의 AddInstance/RemoveInstance·호버 정리 같은 UI 전용 처리는 HUD 어댑터에 남겨 숫자에 요구하지 않는다.
- 출력 카메라·Canvas·내부 목록을 외부에 공개하지 않는다. 카메라 교체/Layer 숨김은 모든 투영 항목에 적용하되 GameObject를 끄지 않아 숫자 시계는 유지한다. 숫자의 CanvasGroup은 입력·레이캐스트를 항상 차단하지 않는다.
- 숫자의 앵커는 피격 시 확보한 월드 위치 값이며 모델 참조를 갖지 않는다. 표시 중 카메라가 움직이면 공통 렌더 직전 투영만 갱신한다. 숫자 Presenter에 별도 LateUpdate·카메라 검색·화면 밖 판정 루프를 만들지 않는다.
- Structure 처리기는 표시 사실 창구(현재 Structure 몸 조회/Final 반영), Structure 큐 재생 입구(키·몸 자리), 숫자 표시 입구(앵커·피해량)만 받는다. DB 전체·UISystem·카메라·Pool·전역 resolver는 받지 않는다. 기존 내부 창구의 좁은 메서드/명시적 콜백으로 연결하며 모든 협력 객체를 서비스로 등록하지 않는다.

### SH8. 숫자 자연 완료·취소·재사용

- 독립 Structure 숫자 위젯 하나는 피해 사건 하나를 재생한다. 기존 캐릭터 HUD의 2슬롯 Show 동작은 유지한다. 독립 재생 입구에서만 대여 토큰과 완료 수신자를 연결한다.
- 자연 완료는 해당 재생의 활성 슬롯이 모두 끝났을 때 **정확히 1회** 통지한다. 완료 상태 확정·수신자 분리 후 통지하고, 통지 뒤 이전 재생의 필드나 Transform을 만지지 않는다. 콜백에서 즉시 반환/재대여해도 새 재생을 지우지 않아야 한다.
- `Clear`·명시적 취소·`OnDisable`은 토큰/수신자를 먼저 무효화하고 숫자 상태를 정리한다. 자연 완료를 통지하지 않는다. 취소를 성공 완료로 간주하지 않는다.
- Presenter는 자신의 활성 대여 표에 있는 같은 토큰의 자연 완료만 받아 회수한다. 이전 대여의 지연/반복 통지는 무시한다. 토큰은 풀 대여의 유효성일 뿐 공격 그룹·결과 완료 상태의 사본이 아니다.
- 정상 반환 순서: 활성 대여에서 제거·토큰 무효화 → 투영 등록 해제 → 숫자 Clear → 비활성화 → Pool 반환. 취소도 같은 반환 경로를 사용한다. 반환 중 재진입/반복 정리로 두 번 반환하지 않는다.

### SH9. 생성·풀·연결 수명

- Factory가 HUD 템플릿의 숫자 위젯 참조를 한 번 검증해 생성 함수를 연결한다. 런타임 Presenter가 HUD 계층을 검색하거나 템플릿 내부 Transform을 탐색하지 않는다. 템플릿 제공 입구는 읽기 전용이며 런타임 수정은 노출하지 않는다.
- 생성 함수는 숫자 루트와 그 하위 TMP 참조가 함께 복제되는 완결된 위젯을 만든다. 원본 템플릿/다른 HUD를 가리키는 외부 TMP 참조는 연결 오류로 거절한다. 복제 시 초기화·활성화 순서를 보장하고 첫 재생 전에 Clear한다.
- 생성/재사용은 기존 Framework `CustomPool`을 사용한다. Factory가 생성·반환·파괴 콜백을 조립하고, 숫자 Presenter는 활성 대여와 투영 대여 수명만 관리한다. 새 범용 Pool을 구현하지 않는다. CustomPool의 실제 파괴/보관 API는 구현 착수 때 대조하며 계약이 부족하면 먼저 설계를 재검토한다.
- Pool 자체는 소유 풀/중복 반환을 보증하지 않으므로 SH8의 활성 대여 표가 반환을 통제한다. 생성 실패·투영 등록 실패·재생 시작 실패 때도 이미 확보한 대여를 역순 정리하고 오류를 전파한다.
- 조립 순서: 기존 HUD Layer 생성·Configure·UISystem 연결 → 숫자 생성/풀과 Presenter 생성 → 사건 처리기에 좁은 입구 주입. 새 서비스 Export·루트 재등록·Builder 변경은 없다.
- 취소/오류 정지는 숫자 Presenter의 CancelAll을 명시적으로 호출한다. 씬 해제는 사건 시작 차단 → 숫자 활성 대여 전부 회수 → 숫자 Pool 보유 객체 정리 → Layer 해제 순서다. 생성 중 실패도 같은 소유 경계로 정리한다. 정상 결과 완료에서는 숫자 Presenter를 취소하지 않는다.

### SH10. 범위 정책과 구조 요구의 구분

- SH3의 이펙트 전용·지연 0은 체력/숫자와 함께 발동시키려는 **이번 기능의 정책**이다. SSOT/SOLID가 소리·흔들림·지연을 일반적으로 금지하는 것은 아니다.
- SH6의 네이티브 장애물 제외도 기존 장애물 연출을 유지하는 **범위 선택**이다. 구조 원칙 검토 통과를 그 정책의 필연성으로 설명하지 않는다.
- 정책을 넓힐 때는 해당 큐 규칙/처리기와 관련 인수 기준을 수정한다. 투영·숫자 완료·Pool 계약까지 바뀌지 않도록 경계를 유지한다.

## 3. 원칙·변경 독립성 검토

| 기준 | 적용과 검증 |
|---|---|
| SSOT | 피격 시점은 Gate, 중간 HP는 사건/ShownFacts, 큐는 외형, 숫자 시간은 UnitDamageText, 숫자 대여 수명은 전용 Presenter |
| SRP | Structure 피드백은 전용 처리기, 큐 해석·실행·UI 투영·생성 연결은 각각 기존 책임을 유지 |
| OCP | 기존 처리기 슬롯·큐·Accent 확장 지점을 사용. 캐릭터 피격 처리기에 Structure 분기를 넣지 않음 |
| LSP | 기존 사건 처리기의 false → 사실 반영 계약 유지. 위젯 재생/반환 계약도 기존 숫자 동작을 보존 |
| ISP | 처리기는 몸 조회·큐 조회·숫자 표시·사실 반영에 필요한 창구만 받음 |
| DIP | Core 계산이나 InGame DB 대신 Buffer 사건과 View 표현 계약에 의존 |
| JMH/FW | 적용 계약과 생성→연결→소비→반환 경로는 아래 표로 구분한다. 관련 없는 계약 번호를 일괄 인용하지 않음 |
| 캡슐화 | Layer 카메라/목록·숫자 슬롯·Pool을 숨기고 등록 대여/표시/완료 입구만 전달(SH7~SH9) |
| C#/Unity 구조 | 순수 C# 처리기·대여 소유자와 MonoBehaviour 표현 분리, 완결된 템플릿 참조·비활성화 수명 보장 |
| If/Tick | 대상·HP 감소는 처리기 한 곳, 대여 유효성은 반환 경계에서 검사. 애니메이션은 기존 Update, 투영은 기존 렌더 콜백만 사용. 완료 폴링·DB 반복 조회·별도 카메라 루프 없음 |

| 적용 경로 | FW 계약/지침과 확인 내용 |
|---|---|
| 생성·조립 | JMH Factory는 생성/연결 담당. 새 서비스/모듈 등록이 없어 FW-001/002 등록을 추가하지 않음. FW-003의 Build 후 사후 등록도 없음 |
| UI 연결·개폐 | 기존 UnitHUDLayer를 UISystem에 등록해 사용하는 FW-008/009 경로 유지. 숫자는 내부 위젯이며 개별 화면/이력 등록을 추가하지 않음 |
| 큐 소비 | 기존 Global Render Context → EffectWindow → EffectScope 경로. 루트 resolver 직접 조회·서비스 재노출 없음. FW-012의 등록된 Effect/데이터 조건 확인 |
| 이펙트 반환 | FW-011의 Scope/Handle 소유 경로 유지. 정상 자연 종료와 StopAll/Release 구분 |
| 숫자 Pool | FW-014의 CreateFunc/non-null·initialSize·반환 콜백 순서 준수. 중복/다른 Pool 반환은 SH8 대여 소유자가 방지 |
| 투영 구독·해제 | 기존 Layer의 렌더 콜백 구독 수명 유지. FW-103은 반환 참조 폐기·구독 수명에 대한 권장사항이며 MUST와 구분 |

FW-010은 Mesh 갱신 계약, FW-013은 이벤트 버스 타입 계약, FW-015는 Queue 정렬 계약이므로 이번 변경의 직접 적용 근거로 사용하지 않는다. 기존 FrameworkRuntime 선행 수명(FW-016)은 변경하지 않는다. 숫자 완료는 전역 EventBus가 아닌 해당 대여의 명시적 완료 수신자다.

대표 변경 시나리오: 현재는 Structure 피드백을 Facts 처리기에 더하면 환경·아이템 등 관계없는 갈래와 함께 수정하게 된다. 분리 후 Structure 피드백 변경은 전용 처리기와 관련 검증에 머문다. 이펙트 교체는 외형 에셋만, 숫자 모양 변경은 기존 숫자 템플릿만, 의존성 변경은 조립 지점만 수정한다. 공통 숫자 완료 계약·HUD 투영·취소가 바뀌는 부분은 캐릭터 숫자와 씬 수명 회귀를 함께 확인해야 한다.

Core·FieldCamera·Architecture·Missing Script 에셋은 수정하지 않는다. 현재 사건에 필요한 이전/이후 HP·키·근원이 있으므로 Core 요청도 필요하지 않다. 구현 중 계약 부족이 드러나면 구현을 우회하지 않고 질문/인계한다.

## 4. 인수 기준

1. 근접 선딜레이·ClipEvent·HitDelay 동안 기존 HP 유지, 해제 프레임에 HP·숫자·이펙트 시작.
2. 원거리 비행 동안 기존 HP 유지, 접촉 프레임에 셋 시작. 관통은 기존 대상별 해제를 보존.
3. 비치명타 감소량이 정확하고, 반복 피격은 별도 위젯으로 앞 숫자와 함께 끝까지 재생.
4. 치명타 모델/HUD는 즉시 회수돼도 숫자와 이펙트는 재생된다. 재사용 모델에 붙거나 취소 후 다시 나타나지 않음.
5. 회복·동일 HP·위치만 변경·장애물 사건에는 새 피격 피드백 없음. 일반 Structure 사실 반영 누락·중복 없음.
6. 빈 큐는 HP·숫자만, 잘못된 큐는 DB 생성에서 실패. 구성 누락은 조용히 숨기지 않음.
7. 공격 접촉 이펙트와 피격 이펙트는 각 설정당 한 번. 새 큐 경로 중복 실행 없음.
8. 취소·오류 정지·씬 해제 후 숫자/이펙트 대여가 남지 않으며 다음 Combat 재진입 가능.
9. 자연 완료 1회, Clear/OnDisable/취소 완료 통지 0회. 반환 콜백 안 재대여·이전 토큰 통지·반복 취소에도 새 위젯을 회수하지 않음.
10. HUD와 숫자가 같은 투영 계산을 사용하고 카메라 교체/Layer 숨김을 함께 따름. 숫자 입력 차단·HUD 호버 회귀 없음.
11. 템플릿 외부 TMP 참조·생성/등록/시작 실패를 검출하며 부분 생성 자원도 회수. Sources/처리기에 중복 큐 캐시 없음.

## 5. 리뷰 지점

2026-10-30 문서 보완 시점에는 구현 승인이 없었다. 2026-11-01 사용자가 전체 Task 진행 후 검토를 지시하여 이번 실행은 Phase 중간 보고 후 계속 진행했다. 코드/에셋 연결과 정적 리뷰를 마쳤으며 Unity 실제 실행 검증은 남았다. [구현 리뷰](2026-11-01-combat-structure-hit-implementation-review.md)와 계획의 체크/실행 기록을 확인한다.

## 6. 2026-11-01 추가 요구 — Structure 선택·좌하단 정보

사용자: Structure 분류는 유지하되 유닛처럼 선택하고, 좌하단 현재 선택 유닛 정보창에 표시한다. 기존 구현 보류를 유지하고 설계/Task 범위에 추가한다.

### SH11. 선택 원본과 입력

- 현재 `CombatSelectionState.SelectCell`·Synchronize는 Character 목록만 보고, `CombatUnitInfo.Find`도 Character만 찾는다. Structure는 선택과 정보 해석 양쪽 확장이 필요하다.
- 기존 `SelectedEntityId` 하나를 정보 선택의 SSOT로 유지한다. 살아 있는 일반 적 Structure의 점유 칸을 누르면 선택한다. 여러 칸짜리는 어느 점유 칸을 눌러도 같은 EntityId다. 네이티브 장애물 선택 확대는 이번 범위 밖이다.
- 기존 클릭 수명 유지: 같은 대상 재누름은 선택 유지, 빈 칸은 해제, 다른 대상 클릭은 교체. 캐릭터와 Structure가 같은 칸에 잡히면 기존 캐릭터 선택을 우선하고 Structure는 캐릭터가 없을 때 선택한다. 호버로 선택 대상을 교체하지 않는다.
- 정보 대상과 조작 대상은 기존 계약대로 분리한다. Structure를 선택해도 `SelectedActorId`는 Empty이며 이동·공격·배치·장비 해제 같은 캐릭터 명령의 행위자로 넘기지 않는다. Structure를 CharacterData로 변환하거나 가짜 유닛을 만들지 않는다.
- 클릭 선택은 기존 입력 Router/Inspection 경로로 연결한다. 공격 조준 중 클릭은 기존 명령 경로를 유지하며 정보 선택이 가로채지 않는다. 새 포인터 Raycast·UI onClick 리스너·전역 이벤트는 추가하지 않는다.

### SH12. 기존 정보창의 Structure 표현

- 기존 `CombatUnitInfo`(UIId.CombatUnitInfo, Parent Deck, Exclusive)와 전환 연출을 재사용한다. 별도 Structure 창·UIId·선택 상태를 만들지 않는다. 개폐는 기존 UISystem 경로다.
- 이름·설명·아이콘은 `Definition.Key`로 찾은 View 외형, 현재/최대 HP와 소유 진영은 표시 중인 Structure 사실에서 읽는다. 없는 아이콘은 기존 외형의 선택적 아이콘 정책을 따른다.
- 캐릭터 전용 레벨·병과·공격/이동 가능·코스트·장착 아이템·특성 등은 Structure가 선택되면 숨기고 이전 캐릭터 내용을 정리한다. 제공되지 않은 값을 0이나 가짜 레벨로 채우지 않는다. 다시 캐릭터를 고르면 캐릭터 표시를 완전히 복원한다.
- 캐릭터/Structure → 정보 표시값 변환을 각각 응집된 변환으로 분리한다. 정보창은 공통 표시값과 전환 수명만 처리하고 개체 종류 분기를 위젯마다 반복하지 않는다. 표시값은 요청 때 만든 파생 값이며 별도의 지속 HP 저장소가 아니다.
- 보류 중인 패널 전환도 CharacterData 참조에 고정하지 않는다. 선택 ID를 기준으로 실제 적용 시 현재 표시 사실을 다시 해석하여, 전환 중 대상 교체/파괴 뒤 옛 내용을 열지 않는다.

### SH13. 피격·파괴와 선택 정보의 동기화

- 좌하단 HP도 HUD와 같은 피격 사건 반영 뒤 갱신한다. 최신 Buffer의 계산 완료 HP를 먼저 읽지 않는다. 표시 사실 원본은 기존 `CombatShownFacts`이며 별도 Structure 목록/HP 캐시를 정보창에 만들지 않는다.
- 선택 조회/정보 해석은 SH14의 읽기 계약을 사용한다. 완료 표시 목록과 재생 사실을 소비자가 전환하지 않고 항상 같은 ShownFacts 원본을 읽는다. Display는 기존 완료 Refresh에서 원본 교체를 요청하는 역할만 유지한다.
- Structure 사실 반영·Render 경계에서 선택 대상의 정보만 갱신한다. 선택 Structure가 파괴되면 같은 경계에서 선택 해제와 정보창 닫기를 요청한다. 최종 결과 갱신까지 파괴된 정보를 남기지 않는다.
- 국면 변경·씬 해제 때 기존 선택 초기화를 유지한다. 사건마다 선택창을 새로 생성하거나 매 프레임 모든 개체를 훑어 파괴 여부를 검사하지 않는다.
- 이 추가는 선택/정보 기능의 별도 Phase로 둔다. 피격 이펙트·숫자 내부 구현과 분리하고, 공유 경계인 표시 사실 갱신/입력/패널 전환만 통합 검증한다. Core·FieldCamera·Architecture 변경은 계획하지 않는다.

### SH14. 표시 사실 읽기 계약 — 단일 원본

- 내부 읽기 계약 `ICombatInspectionFactsReader`(이름은 구현 예정)는 `CombatShownFacts`가 구현한다. 입구는 `TryGet(EntityId, out CombatInspectionFacts)`와 `TryPick(CellCoord, out EntityId)` 두 개다. 선택과 정보 변환은 동일 인스턴스를 주입받는다. Framework 서비스로 등록하지 않는다.
- `CombatInspectionFacts`는 불변 읽기 값이다. ID·외형 키·Owner·HP·점유 칸과 선택적인 캐릭터 정보용 사실을 노출한다. 캐릭터 정보는 표시 중인 불변 CharacterCombatFacts에서 필요한 값을 읽는 뷰이며 CharacterData/최신 Buffer 참조를 보관하지 않는다. Structure에는 캐릭터 정보가 없다. 변경 메서드·원본 목록·DB/서비스 참조를 노출하지 않는다.
- `TryGet`은 SH11의 정보 선택 대상(살아 있는 필드 캐릭터 또는 일반 적 Structure)만 성공한다. `TryPick`은 같은 대상 판정을 재사용하고 캐릭터 우선으로 점유 칸을 조회한다. 선택 가능 조건을 Selection/Info/View에 각각 복제하지 않는다. 정상 대상 없음은 false, 구성 누락은 예외다.
- 완료 Refresh의 기존 `Replace`가 초기/최종 사실을 ShownFacts에 적용하고, 재생 중에는 기존 사건 `Apply`가 그 원본만 바꾼다. 따라서 읽기 계약에 재생 모드 스위치·완료 목록 fallback·별도 HP 캐시는 없다. 기존 `_displayedCharacters`는 다른 기존 소비자를 위해 유지할 수 있지만 새 정보/선택 경로의 사실 원본으로 사용하지 않는다.
- Selection은 `_selected`와 국면만 소유한다. 기존 Character 목록 기반 Find/Synchronize를 이 읽기 계약으로 대체한다. `SelectedActorId`는 같은 조회 결과의 살아 있는 아군 캐릭터일 때만 반환하며, 구조물을 행위자로 올리지 않는다.

### SH15. 선택 검증·정보 갱신의 조정자

- 내부 순수 C# `CombatInspectionPresenter`(구현 예정)가 선택 검증→선택 정보 변환→정보창 전달 순서를 소유한다. 기존 Display의 클릭 처리와 표시 완료 알림은 이 객체에 위임하고 Structure별 UI 로직을 추가하지 않는다.
- 선택 ID 변경 권한은 계속 `CombatSelectionState`만 가진다. 조정자는 `SelectCell`·`ValidateSelection`·`Clear`라는 명시적 명령을 요청한다. 정보창과 개별 사건 처리기는 선택을 직접 쓰지 않는다.
- 클릭: 기존 Inspection 경로→조정자→Selection.SelectCell→정보 갱신. 표시 갱신: ShownFacts.Render가 필드 그리기를 마친 뒤 조립 시 연결한 단일 표시 완료 수신자→조정자→선택 유효성 검사→정보 갱신이다. 특정 Structure 처리기에만 갱신 코드를 붙이지 않아 캐릭터 피격/이동/최종 Replace도 같은 경계를 사용한다.
- 표시 완료 콜백은 ID/HP를 복제해 전달하지 않는다. 조정자가 현재 선택 하나를 읽는다. 일반 Render는 선택 검증 후 갱신, 선택 없음은 기존 창 닫기만 요청한다. 새 변경 ID 집합·dirty 플래그·완료 순번은 만들지 않는다. 개체 전체를 매 프레임 스캔하지 않는다.
- 동기 콜백이 UI 개폐로 재진입할 수 있으므로 연결 세대와 현재 선택 ID를 외부 호출 뒤 확인한다. 이전 호출은 새 선택/새 씬의 창을 닫거나 덮어쓰지 않는다. 세대는 조정자의 연결 유효성만 표현하며 전투 결과 완료 번호를 복제하지 않는다.
- 국면 변경·해제는 조정자의 명시적 입구로 선택 Clear와 패널 정리를 수행한다. 해제 시 표시 완료 수신자를 먼저 끊고 조정자를 정리한다. 새 EventBus 구독·Update·Tick은 없다. Factory/기존 조립 지점이 reader·Selection·변환·정보창과 수신자를 연결한다.

### SH16. 공통 정보 표시값과 패널 적용

- 불변 `CombatInspectionInfo`(구현 예정)의 공통 부분은 ID·표시 이름·설명·아이콘·진영·현재/최대 HP다. 캐릭터 전용 부분은 선택적인 `CharacterInfoSection` 하나로 묶는다. Structure는 해당 부분이 없음으로 표현하고 `isStructure`·항목별 임의 bool·가짜 0 수치를 추가하지 않는다.
- CharacterInfoSection은 기존 창이 실제 사용하는 레벨·병과·전투 능력치·특성/상태·행동 가용성 표시값을 묶는다. 장비/코스트 등은 실제 기존 표시가 있을 때만 포함하며 이번 요구를 이유로 새 정보를 추가하지 않는다. 레벨이 없는 캐릭터의 기존 nullable 의미도 보존한다.
- 변환은 캐릭터용/Structure용으로 분리하고 읽기 사실과 필요한 View 외형 Query를 명시적으로 받는다. 지속 상태를 갖지 않는다. Owned 기록 등 기존 캐릭터 보조 정보는 기존 조회 경계를 통해 레벨 변환에만 사용하고 HP 원본으로 사용하지 않는다.
- View는 공통 부분을 적용한 뒤 캐릭터 전용 그룹 적용/정리를 **한 지점**에서 수행한다. Structure 선택 시 전용 그룹의 내용·호버·가용성 응답을 정리하고 숨긴다. 위젯 각각에서 개체 종류를 재판정하지 않는다. 다음 캐릭터 표시 때 그룹과 내용을 완전히 복원한다.
- 패널에는 표시 중 ID와 전환 수명만 남긴다. 전환 완료 시 현재 선택 ID의 정보를 조정자에게 다시 요청하고, 무효한 대상이면 닫는다. 이전 표시값/CharacterData를 지연 적용하지 않는다. 전환 중 갱신도 같은 요청 경로를 쓴다.
- 행동 가용성은 현재 선택 ID와 캐릭터 전용 그룹 존재가 모두 맞을 때만 적용한다. 늦게 온 캐릭터 응답이 Structure 표시를 다시 켜지 않도록 한다. 정보 표시값은 갱신 시 만든 파생 값이며 선택/HP SSOT를 대체하지 않는다.

### 보완 후 변경 경계

| 변경 시나리오 | 수정/검증 범위 |
|---|---|
| Structure 정보 항목 변경 | Structure 변환·표시값/해당 View 표시, 캐릭터 명령·피격 숫자 구현과 분리 |
| 선택 정책 변경 | reader 대상 판정/선택 정책 및 클릭 회귀. 위젯마다 조건을 복제하지 않음 |
| 표시 갱신 계약 변경 | ShownFacts 완료 경계·조정자 및 캐릭터/Structure 통합 회귀 |
| 의존성 추가 | 조립 지점과 해당 기능 계약. Display에 조회/변환/선택 구현을 누적하지 않음 |

7기준 적용: SSOT는 ShownFacts/SelectedEntityId, SRP는 읽기·선택·조정·변환·View, OCP는 종류별 변환, LSP는 기존 UI/선택 읽기 의미 유지, ISP/DIP는 좁은 읽기/명령 입구로 점검한다. FW-008/009의 기존 UI 등록/이력은 유지하고 새 내부 협력 객체는 서비스로 등록하지 않는다. 순수 C# 조정과 불변 값/Unity View를 분리하며 표시 완료·클릭·전환 완료에서만 갱신한다. 점수 상승을 완료 근거로 삼지 않고 아래 인수 증거로 확인한다.

### 추가 인수 기준

1. 살아 있는 일반 적 Structure를 모든 점유 칸에서 선택해 기존 좌하단 창에 올바른 외형 정보와 HP 표시.
2. Structure 선택으로 캐릭터 명령이 제출되지 않고 공격 조준 입력도 기존대로 동작.
3. 캐릭터↔Structure 빠른 교체·호버·빈 칸·패널 전환 중 파괴에서 이전 정보/레벨/아이콘 잔류 없음.
4. 근접/원거리 피격 전 좌하단 HP 유지, 피격 사실 반영 뒤 HUD와 같은 HP. 파괴 시 선택 해제/정보창 닫기.
5. 네이티브 장애물·기존 캐릭터 선택·국면 전환·씬 재진입 동작 회귀 없음.
6. 초기 Replace→사건 Apply→최종 Replace에서 동일 reader가 같은 시점의 사실을 반환. 최신 Buffer가 먼저 바뀌어도 선택 정보 HP는 유지.
7. 캐릭터 피격/이동과 Structure 파괴 모두 공통 Render 완료 경계로 선택 검증. 개별 처리기의 중복 알림·새 Tick 감시 없음.
8. 빠른 대상 교체·전환 완료·늦은 행동 가용성 응답·UI 콜백 재진입·해제 후 통지에서 새 선택을 덮거나 옛 전용 그룹을 다시 열지 않음.

## SH17 — 2026-11-01 사용자 후속: 건물 오프셋·배치 조회

- 사용자 요청으로 SH11~SH16의 전투 전용 정보 조회 범위를 Deployment까지 확장한다. 표시 사실/선택 ID/Mapper/UI 등록의 원본은 그대로 유지한다. 배치에서 조회한 선택은 전투 국면 전환 시 기존 규칙대로 해제한다.
- FieldUnitPresenter가 일반 유닛·일반 Structure·네이티브 장애물의 월드 오프셋을 각각 조립한다. 새 structureHudOffset 기본값은 기존 unitHudOffset과 같은 (0, 1.5, 0)이며, 기존 유닛/Rider/장애물 설정은 보존한다. Structure 분류는 호출 시 이미 알고 있는 사실 종류로 전달하며 키별 DB/규칙 추측을 추가하지 않는다.
- Rider의 기존 HUD.prefab riderHudYOffset은 일반 유닛 오프셋에 더하는 월드 Y 보정(현 값 1)으로 유지한다. 건물 이름/설명/InfoIcon은 ViewGroup UnitVisual_Headquarters, HP는 ShownFacts의 Buffer 사실이 원본이다.
- 배치 조회는 기존 CanSelectUnit 정책·SelectPressed 경로를 확장한다. 진행 중인 배치/아이템 Drag는 조회하지 않고 기존 배치 입력으로 전달한다. UI 가림·모달·재생·대기 차단은 기존 정책을 유지한다. 전투의 자동 행동 중 조회 허용은 배치에 확장하지 않는다.
- 기존 UIId.Deck의 배타 자식 정책을 유지한다. 필드 정보 선택 시 UnitCard 대신 CombatUnitInfo가 열리며 Deck 버튼으로 목록에 돌아간다. 새 UI/서비스/구독/Tick/선택 저장소는 만들지 않는다.
- 사용자 확인: Combat 씬 최초 배치 진입부터 Deck 목록을 닫고 Deck 버튼으로 연다. Bootstrap의 기존 최초 개폐 지점에서 CloseChildUI를 사용하며 Deck 부모/버튼의 등록·수명은 유지한다. 전투 중 선택/배치 복귀 정책을 새 감시 루프로 덮어쓰지 않는다.
