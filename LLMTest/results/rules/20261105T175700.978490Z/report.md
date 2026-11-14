# 룰 AI 오프라인 비교 결과

실행 UTC: 2026-11-05T17:57:01.000492+00:00

synthetic 입력의 가치 평가 결과다. 실제 Core 합법성·승률·재미를 입증하지 않는다.

## 기본 설정의 기대 판단 확인

| 상황 | 기대 판단 | 실제 선택 | 판정 |
|---|---|---|---|
| F01 | 승리 ['a_kill_sniper'] / 역할 ['a_kill_sniper'] | 승리 a_kill_sniper / 역할 a_kill_sniper | 통과 |
| F02 | 승리 ['b_hit_shotgun'] / 역할 ['b_hit_shotgun'] | 승리 c_retreat / 역할 c_retreat | 기대 선택 불일치: victory, role |
| F03 | 승리 ['b_move_main'] / 역할 ['b_move_main'] | 승리 b_move_main / 역할 b_move_main | 통과 |
| F04 | 승리 ['a_hit_main'] / 역할 ['a_hit_main'] | 승리 a_hit_main / 역할 a_hit_main | 통과 |
| F04b | 승리 ['b_retreat'] / 역할 ['b_retreat'] | 승리 b_retreat / 역할 b_retreat | 통과 |
| F05 | 승리 ['b_safe_akimbo'] / 역할 ['b_safe_akimbo'] | 승리 b_safe_akimbo / 역할 b_safe_akimbo | 통과 |
| F06 | 승리 ['a_hit_main'] / 역할 ['a_hit_main'] | 승리 a_hit_main / 역할 a_hit_main | 통과 |

기대 이유와 미평가 사유:

- F01: 처치는 다음 아군 턴의 공격 하나를 없앤다. 같은 자리에서 메인 5 피해보다 낫다.
- F02: 확실히 죽는 자리(제자리 위협만으로 사망)는 피한다. 이동해야만 닿는 위협은 피해 이득이 있으면 감수한다.
- F03: 같은 피해라도 메인을 치고 노출이 적은 쪽이 낫다.
- F04: 앞선 Demon이 Sniper를 처치해 남은 위협은 메인 하나다. 공격해도 산다.
- F04b: Sniper가 살아 있으면 메인 6 + Sniper 4 = 10으로 확실히 죽는다. 처치 없는 메인 피해를 위해 죽지 않는다.
- F05: 예고된 기믹은 반드시 일어나는 피해다. 메인 3 피해를 위해 확실히 죽는 칸에 서지 않는다.
- F06: 연쇄는 확실하지 않고 자리를 옮겨도 피할 수 없다. 피해 이득을 포기하면서까지 피하지 않는다.

## 이전식과 개별 변경 비교

선택이 달라진 상황 없음.

## 한 항목 변경 시 선택이 달라진 상황

- F02 / in_place_0.5: 승리 c_retreat / 역할 c_retreat → 승리 a_hit_main / 역할 a_hit_main
- F04b / in_place_0.5: 승리 b_retreat / 역할 b_retreat → 승리 a_hit_main / 역할 a_hit_main
- F05 / in_place_1: 승리 b_safe_akimbo / 역할 b_safe_akimbo → 승리 c_wait / 역할 c_wait
- F02 / after_move_0.25: 승리 c_retreat / 역할 c_retreat → 승리 c_retreat / 역할 b_hit_shotgun
- F05 / after_move_0.25: 승리 b_safe_akimbo / 역할 b_safe_akimbo → 승리 c_wait / 역할 c_wait

기본 설정: 7개 중 기대 불일치 1개.

입력 원문·설정·해시와 항목별 점수는 같은 폴더의 JSON·CSV에 보관했다.
