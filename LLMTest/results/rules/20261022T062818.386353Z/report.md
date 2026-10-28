# 룰 AI 오프라인 비교 결과

실행 UTC: 2026-10-22T06:28:18.469696+00:00

synthetic 입력의 가치 평가 결과다. 실제 Core 합법성·승률·재미를 입증하지 않는다.

## 기본 설정의 기대 판단 확인

| 상황 | 기대 판단 | 실제 선택 | 판정 |
|---|---|---|---|
| R01 | 승리 ['a_main_kill'] / 역할 ['a_main_kill'] | 승리 a_main_kill / 역할 a_main_kill | 통과 |
| R02 | 승리 ['a_main_pressure'] / 역할 ['a_main_pressure'] | 승리 a_main_pressure / 역할 a_main_pressure | 통과 |
| R03 | 승리 ['b_finish_regular'] / 역할 ['b_finish_regular'] | 승리 b_finish_regular / 역할 b_finish_regular | 통과 |
| R04 | 승리 ['b_safe_move'] / 역할 ['b_safe_move'] | 승리 b_safe_move / 역할 b_safe_move | 통과 |
| R05 | 승리 ['b_low_ap_exposure'] / 역할 ['b_low_ap_exposure'] | 승리 b_low_ap_exposure / 역할 b_low_ap_exposure | 통과 |
| R06 | 승리 ['a_more_damage'] / 역할 ['b_useful_knockback'] | 승리 a_more_damage / 역할 b_useful_knockback | 통과 |
| R07 | 승리 ['b_preserved_lane'] / 역할 ['b_preserved_lane'] | 승리 b_preserved_lane / 역할 b_preserved_lane | 통과 |
| R08 | 승리 ['a_strong_pressure'] / 역할 ['a_strong_pressure'] | 승리 a_strong_pressure / 역할 a_strong_pressure | 통과 |
| R09 | 승리 ['b_clean_shot'] / 역할 ['b_clean_shot'] | 승리 b_clean_shot / 역할 b_clean_shot | 통과 |
| R10 | 승리 ['a_stay_alive'] / 역할 ['a_stay_alive'] | 승리 a_stay_alive / 역할 a_stay_alive | 통과 |
| R11 | 승리 ['b_wait'] / 역할 ['b_wait'] | 승리 b_wait / 역할 b_wait | 통과 |
| R12 | 미평가 | 미평가 | 통과 |
| R13 | 승리 ['a_remove_responder'] / 역할 ['a_remove_responder'] | 승리 a_remove_responder / 역할 a_remove_responder | 통과 |
| R14 | 승리 ['a_attack_without_response'] / 역할 ['a_attack_without_response'] | 승리 a_attack_without_response / 역할 a_attack_without_response | 통과 |
| R15 | 미평가 | 미평가 | 통과 |
| R16 | 승리 ['a_simultaneous_main_and_hq_death'] / 역할 ['a_simultaneous_main_and_hq_death'] | 승리 a_simultaneous_main_and_hq_death / 역할 a_simultaneous_main_and_hq_death | 통과 |
| R17 | 승리 ['b_regular_kill'] / 역할 ['b_regular_kill'] | 승리 b_regular_kill / 역할 b_regular_kill | 통과 |
| R18 | 승리 ['b_regular_kill_die'] / 역할 ['b_regular_kill_die'] | 승리 b_regular_kill_die / 역할 b_regular_kill_die | 통과 |
| R19 | 승리 ['b_retreat'] / 역할 ['b_retreat'] | 승리 a_lane_shot / 역할 a_lane_shot | 기대 선택 불일치: victory, role |
| R20 | 승리 ['a_lane_shot'] / 역할 ['a_lane_shot'] | 승리 a_lane_shot / 역할 a_lane_shot | 통과 |
| R21 | 승리 ['a_regular_kill'] / 역할 ['a_regular_kill'] | 승리 a_regular_kill / 역할 b_main_push_useful | 기대 선택 불일치: role |
| R22 | 승리 ['b_regular_first'] / 역할 ['b_regular_first'] | 승리 b_regular_first / 역할 b_regular_first | 통과 |
| R23 | 승리 ['a_trade_kill'] / 역할 ['a_trade_kill'] | 승리 b_advance / 역할 b_advance | 기대 선택 불일치: victory, role |
| R24 | 승리 ['a_kill'] / 역할 ['a_kill'] | 승리 a_kill / 역할 a_kill | 통과 |

기대 이유와 미평가 사유:

- R01: 적 진영이 즉시 승리하는 메인 처치를 두 정책 모두 일반 제거보다 먼저 선택해야 한다.
- R02: 즉시 처치가 없는 비슷한 교환에서는 승패 목표인 메인 압박을 두 정책 모두 더 가치 있게 보아야 한다.
- R03: 메인 압박이 항상 우선하지는 않는다. 지금 제거해 상대의 이후 행동 기회를 없애는 선택을 두 정책 모두 선호해야 한다.
- R04: 한 번의 가벼운 반격으로 보지 말고 AP로 가능한 반복 공격의 제거 위험을 고려해야 한다. 두 정책 모두 이 교환에서는 안전한 이동을 선택해야 한다.
- R05: 한 번의 피해가 같아도 반복 공격 예산이 큰 노출은 더 위험하다. 두 정책 모두 적은 반복 기회에 노출되는 후보를 선호해야 한다.
- R06: 승리 정책은 더 큰 즉시 피해를, 역할 정책은 작은 전술 차이 안에서 살아남은 대상을 본진에서 밀어내는 기동·교란 행동을 선택해야 한다.
- R07: 입력을 유지한 채 메인 압박의 가치와 실제 제거 위험을 함께 비교한다. 현재 초안에서는 두 정책이 모두 관통을 선택해도 정상이며 역할 차이를 강제로 만들지 않는다.
- R08: 역할 자체를 잘 수행해도 즉시 전술 가치의 손실이 허용 범위를 넘으면 두 정책 모두 더 강한 메인 압박을 선택해야 한다.
- R09: 합법적인 아군 피해를 일괄 금지하지는 않되, 이 상황에서는 포격 이득보다 온전한 아군의 제거 손실이 커서 두 정책 모두 깨끗한 공격을 선택해야 한다.
- R10: 다른 이득이 같은데 즉시 사망해서 미래 위험 계산이 사라지는 것을 이익으로 삼아서는 안 된다. 두 정책 모두 생존 후보를 선호해야 한다.
- R11: 큰 피해를 적어 두어도 선지불 AP가 부족한 행동은 선택할 수 없다. 두 정책 모두 남은 합법 후보를 선택해야 한다.
- R12: 잘못된 위협 일정을 0 위험이나 유효한 반복 공격으로 고치지 말고 상황 전체 미평가로 표시해야 한다.
  - 미평가 사유: response AP insufficient: refund_attacker attack 0; refund cannot prepay
- R13: 행동 뒤 제거된 상대의 반격은 실제 위험에 남아 있지 않다. 두 정책 모두 위험을 없애면서 상대를 제거하는 후보를 선택해야 한다.
- R14: 위협 수나 사거리만으로 존재하지 않는 반격을 더해서는 안 된다. 다음 행동 전 기회가 없는 이 상황에서는 두 정책 모두 메인 공격을 선택해야 한다.
- R15: 알 수 없는 반격 순서를 0 피해로 넣어 공격을 추천하지 말고 누락 원인이 있는 상황 전체를 미평가로 남겨야 한다.
  - 미평가 사유: responseWindow.complete: incomplete observation/schedule
- R16: 동시 사망은 실제 게임의 메인 사망 우선 판정을 따른다. 본진 손실 점수만 보고 전투 계속 후보를 고르지 말고 적 진영 종료 승리를 두 정책 모두 먼저 선택해야 한다.
- R17: 실제 값에서는 일반 유닛 처치(행동 기회 제거)가 메인 10 피해보다 크다. 메인 반격 5 피해를 감수해도 두 정책 모두 처치해야 한다.
- R18: 빈사 Gravekeeper 하나와 일반 유닛 하나의 교환은 받아들일 만하다. 메인 10 피해만을 위해 자멸하지는 않는다. 후퇴는 이득 없이 접근만 늦춘다.
- R19: Ballista는 대체하기 어려운 원거리 화력이다. 처치가 없는 관통 피해를 얻으려고 제자리 Sniper의 반복 공격에 확정 제거되는 위치에 남지 않는다.
- R20: 위협하는 상대 수는 R19와 같지만 Focus가 초기화되어 실제 반격이 2뿐이다. 관통 공격을 해야 한다.
- R21: 일반 유닛 처치는 플레이어의 행동 기회를 없애므로 역할(본진 보호 넉백)보다 우선한다. 역할 정책도 처치를 포기하지 않는다. [검토 요청: 역할 정책이 유효 넉백을 골라도 되는지]
- R22: 관통 인원이 같으면 첫 대상 피해로 일반 유닛을 처치하는 사선이 낫다. 메인 압박을 위해 처치를 버리지 않는다.
- R23: 가장 약한 적 유닛(Demon) 하나로 플레이어 유닛 하나를 없애는 교환은 적 진영에 유리하다. 두 정책 모두 교환해야 한다. [검토 요청: 유닛별 전력 가치 가설과 연결]
- R24: 반사가 없으면 처치는 이득만 있다. R23과 비교해 ThornArmor가 선택에 주는 영향만 분리한다.

## 이전식과 개별 변경 비교

- R02 / legacy: 승리 a_main_pressure / 역할 a_main_pressure → 승리 b_regular_damage / 역할 b_regular_damage
- R04 / legacy: 승리 b_safe_move / 역할 b_safe_move → 승리 a_exposed_shot / 역할 a_exposed_shot
- R05 / legacy: 승리 b_low_ap_exposure / 역할 b_low_ap_exposure → 승리 a_high_ap_exposure / 역할 a_high_ap_exposure
- R07 / legacy: 승리 b_preserved_lane / 역할 b_preserved_lane → 승리 a_single_sniper_kill / 역할 b_preserved_lane
- R08 / legacy: 승리 a_strong_pressure / 역할 a_strong_pressure → 승리 a_strong_pressure / 역할 b_excessive_role_tradeoff
- R21 / legacy: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R04 / main_only: 승리 b_safe_move / 역할 b_safe_move → 승리 a_exposed_shot / 역할 a_exposed_shot
- R05 / main_only: 승리 b_low_ap_exposure / 역할 b_low_ap_exposure → 승리 a_high_ap_exposure / 역할 a_high_ap_exposure
- R21 / main_only: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R02 / risk_only: 승리 a_main_pressure / 역할 a_main_pressure → 승리 b_regular_damage / 역할 b_regular_damage
- R07 / risk_only: 승리 b_preserved_lane / 역할 b_preserved_lane → 승리 a_single_sniper_kill / 역할 b_preserved_lane
- R08 / risk_only: 승리 a_strong_pressure / 역할 a_strong_pressure → 승리 a_strong_pressure / 역할 b_excessive_role_tradeoff
- R19 / risk_only: 승리 a_lane_shot / 역할 a_lane_shot → 승리 b_retreat / 역할 a_lane_shot
- R21 / risk_only: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill

## 한 항목 변경 시 선택이 달라진 상황

- R02 / main_20: 승리 a_main_pressure / 역할 a_main_pressure → 승리 b_regular_damage / 역할 b_regular_damage
- R07 / main_20: 승리 b_preserved_lane / 역할 b_preserved_lane → 승리 a_single_sniper_kill / 역할 b_preserved_lane
- R08 / main_20: 승리 a_strong_pressure / 역할 a_strong_pressure → 승리 a_strong_pressure / 역할 b_excessive_role_tradeoff
- R21 / main_20: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R02 / main_25: 승리 a_main_pressure / 역할 a_main_pressure → 승리 b_regular_damage / 역할 b_regular_damage
- R08 / main_25: 승리 a_strong_pressure / 역할 a_strong_pressure → 승리 a_strong_pressure / 역할 b_excessive_role_tradeoff
- R21 / main_25: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R04 / main_35: 승리 b_safe_move / 역할 b_safe_move → 승리 a_exposed_shot / 역할 a_exposed_shot
- R04 / main_40: 승리 b_safe_move / 역할 b_safe_move → 승리 a_exposed_shot / 역할 a_exposed_shot
- R04 / discount_0.25: 승리 b_safe_move / 역할 b_safe_move → 승리 a_exposed_shot / 역할 a_exposed_shot
- R21 / discount_0.25: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R07 / discount_0.75: 승리 b_preserved_lane / 역할 b_preserved_lane → 승리 a_single_sniper_kill / 역할 b_preserved_lane
- R19 / discount_0.75: 승리 a_lane_shot / 역할 a_lane_shot → 승리 b_retreat / 역할 a_lane_shot
- R07 / discount_1: 승리 b_preserved_lane / 역할 b_preserved_lane → 승리 a_single_sniper_kill / 역할 a_single_sniper_kill
- R19 / discount_1: 승리 a_lane_shot / 역할 a_lane_shot → 승리 b_retreat / 역할 b_retreat
- R06 / role_loss_0: 승리 a_more_damage / 역할 b_useful_knockback → 승리 a_more_damage / 역할 a_more_damage
- R21 / role_loss_0: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R21 / role_loss_6: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill
- R08 / role_loss_18: 승리 a_strong_pressure / 역할 a_strong_pressure → 승리 a_strong_pressure / 역할 b_excessive_role_tradeoff
- R21 / role_bonus_10: 승리 a_regular_kill / 역할 b_main_push_useful → 승리 a_regular_kill / 역할 a_regular_kill

기본 설정: 24개 중 기대 불일치 3개.

입력 원문·설정·해시와 항목별 점수는 같은 폴더의 JSON·CSV에 보관했다.
