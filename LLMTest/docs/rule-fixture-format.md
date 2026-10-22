# 룰 AI 오프라인 상황 형식

`rule_fixtures/*.json`은 schemaVersion 1의 synthetic 상황이다. 기존 `fixtures/S01~S06`과 다른 형식이다. 아래 필드는 생략을 안전 값으로 해석하지 않는다. 숫자는 bool·NaN·무한대를 허용하지 않는다. HP·AP·비용·피해는 정수다.

## 공통 상황

- `schemaVersion`: 1.
- `situationId`, `title`: 문자열. ID는 파일 간에도 유일해야 한다.
- `source`: `synthetic`만 지원한다. 실제 Core 적합성은 이 도구가 증명하지 않는다.
- `assumptions`: 비어 있지 않은 문자열 배열. 위치·피해·합법성·관측의 가정을 기록한다.
- `self`: `unit`은 Demon/Gravekeeper/Ballista, `hp`, `maxHp`, 현재 `ap`, `maxAp`, 이번에 남은 `moveRange`.
- `players`: `{id, role, hp, maxHp}` 배열. role은 main 또는 regular이며 살아 있는 main 하나가 필요하다.
- `allies`: 행위자와 본진을 제외한 `{id, hp, maxHp}` 배열. 없으면 빈 배열.
- `headquarters`: `{hp, maxHp}`. 적 본진의 시작 상태.
- `approachCostBefore`: 메인 공격 위치까지의 고정 기준 이동 AP 비용. 정수 또는 null. null은 도달 불가능이며 미상과 다르다.
- `expected`: `{victory: [후보ID], role: [후보ID], reason: 설명}` 또는 `{status: "unevaluated", reason: 설명}`. 평가기는 기대 선택을 읽어 점수를 바꾸지 않는다.
- `candidates`: 아래 형식의 후보 배열.

## 후보

- `id`, `label`: 비어 있지 않은 문자열. ID는 상황 안에서 유일하다.
- `legal`: 명시적인 bool. false이면 `legalityNote`를 기록하고 해당 후보를 제외한다. true는 작성자의 합법성 가정이며 Python은 아래 AP·입력 산술만 추가 검사한다.
- `actions`: 순서 있는 `{kind, cost, refund, steps}` 배열. kind는 attack/move/wait, steps는 move에만 양수이고 나머지는 0. 공격 cost는 양수다. 이동·대기의 cost는 0 이상이다. cost 선지불 후 refund를 더하며 maxAp를 넘을 수 없다. 이동 steps 합이 self.moveRange를 넘으면 제외한다.
- `selfHpAfter`, `headquartersHpAfter`: 해당 시작 HP 이하의 정수.
- `playerHpAfter`, `allyHpAfter`: 시작 배열의 모든 ID를 정확히 한 번씩 키로 갖는 HP map. 회복·최대 HP 변화는 이번 버전에서 지원하지 않는다.
- `approachCostAfter`: approachCostBefore와 같은 정의.
- `hitIds`: 이번 공격에서 실제 양의 피해를 준 서로 다른 player ID 배열. HP 변화 map과 일치해야 한다. 보조 피해와 혼동하지 않도록 작성자가 직접 명시한다.
- `roleFacts`: `{canThreatenNext: bool, nearestPlayerDistance: 정수 또는 null, knockbackDelays: [{targetId: player ID, before: 비용 또는 null, after: 비용 또는 null}]}`. 넉백 대상은 전체 후보 처리 후에도 살아 있어야 하고 targetId를 중복 등록할 수 없다. 필요한 기하 계산은 입력 가정이며 Python에서 게임의 경로·지정 규칙을 구현하지 않는다.
- `responseWindow`: 아래 형식. 종료 후보와 행위자 사망 후보는 미래 위험을 계산하지 않지만 필드 자체는 기록한다.

## 위험 일정

`responseWindow`는 `{complete: true, assumptions: 설명, opportunities: [...], nonThreats: [...]}`다. complete=false 또는 누락된 위협 정보는 상황 전체 미평가다.

`opportunities`는 후보 완료 후부터 행위자의 다음 행동 직전까지의 순서 있는 기회 배열이다. 같은 상대의 여러 기회를 포함할 수 있다. 각 원소는 `opponentId`, `availableAp`, `maxAp`, `mode`를 가진다. mode는 다음 중 하나다.

- `fixed`: `repeatSafe: true`, `attackCost` 양수, `damagePerAttack` 0 이상. 상태·위치·최종 피해·비용이 반복 동안 불변이고 환급이 없다는 작성자의 가정이다. AP로 가능한 공격 횟수를 계산한다.
- `sequence`: `attacks: [{cost, damage, refund}]`. 공격 cost는 양수, damage와 refund는 0 이상. 비용 선지불과 사후 환급의 AP 연결을 검사한다. 효과에 따른 비용·피해 변화는 이미 산출된 입력이며 효과 규칙을 Python이 재현하지 않는다.

`nonThreats`는 `{opponentId, reason}` 배열이다. 일정에 포함되지 않은 살아 있는 플레이어마다 제자리 사거리 밖·기회 없음 등의 이유가 필요하다. 시작 이후 처치된 상대는 위험에서 자동 제외한다. 완전성 선언은 실제 판정의 증거가 아니라 이 synthetic 입력의 범위 선언이다.

두 모드 모두 행위자가 제거되는 지점까지만 적용 피해를 기록한다. capacityDamage는 사망 뒤 일정까지 포함하는 잠재 공격 용량, appliedDamage는 해당 일정이 HP에 적용한 손실이다. 점수는 appliedDamage를 사용한다. 비교용 이전 단발 프로필은 같은 일정의 각 상대 첫 공격 한 번만 사용한다. 실제 피해와 미래 일정의 피해를 중복 집계하지 않는다.

## Python 평가 API

`tools/rule_policy.py`는 `PolicyInputError(ValueError)`, `load_json(path)`, `validate_weights(settings)`, `evaluate_case(case, settings)`, `to_jsonable(value)`를 제공한다. 소수 계산은 Decimal이다.

`evaluate_case`는 입력 누락·미상·잘못된 위협 일정이면 PolicyInputError를 던진다. 평가할 합법 후보가 없어도 미평가다. 개별 행동의 AP 부족·이동 예산 초과·legal=false는 후보 제외로 남긴다. 결과는 situationId/title/source/choices/bestVictoryScore/candidates를 가진다. choices는 `{victory: id, role: id}`다.

후보 결과는 id/label/eligible/reason/outcome/features/contributions/victoryScore/roleScore/roleEligible/response를 포함한다. 제외 후보는 점수가 null이고 나머지 객체는 비어 있다. features는 M,D,K,P,F,R,B,Q, contributions는 main,damage,kill,progress,friendlyLoss,selfLoss,baseLoss,roleBonus다. 감점 contribution은 음수다. response는 적용 피해와 일정 trace를 포함한다. 종료 구간 밖 후보도 점수는 기록하되 선택에서는 제외하고 roleEligible=false다.

`to_jsonable`은 Decimal을 정확한 10진 문자열로 변환한다. 출력 JSON의 점수 문자열과 CSV의 수치는 같은 값을 나타낸다. 실행기는 평가 결과와 별도로 expected를 비교한다.
