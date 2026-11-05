# Structure 피격·선택 구현 리뷰

- 날짜: 2026-11-01. 사용자 요청의 7가지 기준으로 구현 코드를 검토했다. Unity 실행 인수와 구분한다.
- 기준: [설계](2026-10-30-combat-structure-hit-design.md), [Task/검증 기록](../plans/2026-10-30-combat-structure-hit-plan.md).

## 검토 결과

| 기준 | 코드 검토 결과 |
|---|---|
| SSOT | 시점은 기존 Gate, 표시 HP는 ShownFacts, 선택 ID는 SelectionState, 숫자 시간은 기존 UnitDamageText, 큐 캐시는 MomentCuePlayback. 정보창의 기존 CharacterData 목록·HP 조회는 제거했다 |
| SOLID 5원칙 | Structure 처리기·선택 조정자·정보 변환·View·Factory의 책임을 분리했다(SRP). 기존 슬롯/큐/투영 입구를 확장했다(OCP). false→ApplyFacts 계약과 기존 UI 등록/선택 읽기 의미를 보존했다(LSP). 좁은 reader/표시 콜백을 주입하고 처리기에 DB/UI/Pool을 넘기지 않았다(ISP/DIP) |
| JMH·FW | Factory가 풀 생성/파괴를 조립하고 기능 객체는 대여를 소유한다. 내부 협력 객체의 서비스/루트 재등록 없음. FW-008/009 기존 UIId/개폐 경로 유지, FW-011/012 EffectWindow/Scope·등록 Hit 타입 재사용, FW-014 CustomPool 사용/중복 반환 방지. FW-103 구독/반환 수명은 권장사항으로 별도 대조 |
| 변경 독립성 | 피격 효과는 전용 처리기/외형, 정보 변환은 별도 Mapper, 조립은 Factory에 한정한다. 캐릭터/Structure 정보 전환은 공통 View 적용점에서 처리한다. 공통 투영·숫자 계약 변경 때문에 해당 회귀는 반드시 필요하다 |
| 캡슐화 | 카메라/Layer 목록/Pool 큐를 노출하지 않는다. 일반 위젯은 해제 가능한 투영 대여만 받는다. Selection은 reader로 선택 가능 사실만 읽고, 정보창은 파생 표시값만 적용한다 |
| C#/Unity 구조 | 순수 C# 기능과 MonoBehaviour 표현을 분리. 숫자 루트를 독립 자식으로 정리해 TMP 참조 완결성을 검사한다. 자연 완료 전에 토큰/수신자를 분리해 콜백 재대여를 보존한다. 취소/OnDisable은 성공 완료를 알리지 않는다 |
| If/Tick | HP 감소/적/장애물 여부는 사건 경계에서 판단. 위젯의 기존 애니메이션 Update와 Layer 렌더 투영만 사용. 숫자 완료 폴링·매 프레임 개체 사망 감시·중복 큐 타이머 없음. 클릭 시 점유 칸 검색은 정상 입력 처리이며 캐시 상태를 추가하지 않았다 |

## 검토 중 수정한 사항

1. HUD 전체를 복제하게 되는 템플릿 배치를 발견해 숫자 전용 자식으로 정리했다. 같은 컴포넌트 fileID·TMP·설정값을 유지한다.
2. 조립 실패 때 사건 Presenter도 해제하고, 숫자 생성/등록/시작 실패의 자원을 정리하도록 했다.
3. 숫자 취소는 모든 대여 회수를 시도한 뒤 예외를 모은다. 일반 사건 취소도 나머지 재생기 정리를 계속한다.
4. HUD와 숫자의 투영 계산을 공통 메서드로 수렴시켰다. 숫자는 raycast/interactable=false를 유지한다.
5. 클릭 검색에서 대상마다 같은 목록을 다시 조회하는 중첩 검색을 제거했다. 선택 가능한 Structure 조건은 같은 함수로 조회/클릭이 공유한다.

## 잔여 위험과 판정

후속 사용자 검증에서 CanvasGroup 누락 예외가 발견됐다. 투영 대여의 null 병합 연산자를 Unity Object null 비교로 고쳤고, 누락 컴포넌트 생성·반복 해제 회귀 테스트를 추가했다. Unity Test Runner 전투 View 47건이 모두 통과했다.

컴퓨터 사용으로 Test 스테이지에서 Structure 선택·유닛 정보 전환·근접 피격 2회를 직접 확인했다. HP 512→505→495, 피격 이펙트, 피해 숫자 7 표시와 자연 숨김, 복제 CanvasGroup 생성·입력 차단 해제 확인. 검증 구간 새 예외 없음. 실행 증거와 범위는 Task 계획의 후속 기록이 원본이다.

전체 인수 완료로 판정하지 않는다. 원거리 접촉의 프레임 대조, 치명타 숫자 잔류, 카메라 이동·씬 이탈 중 반환, 장애물 실제 회귀는 남았다. EditMode 테스트 통과는 이 런타임 사례를 대신하지 않는다.

Core·FieldCamera·Architecture 코드는 변경하지 않았다. 기존 사용자 수정 에셋과 `EL-b-effect-structure.patch`는 작업 대상으로 삼지 않았다. 커밋·push하지 않았다.
