# 토네이도 전투 View — 특성 미리보기 Factory 변경 공유

- 작성자: 호진(이호진)
- 받는 분: 용근님(특성 연출 미리보기 작성)
- 상태: 미전달
- 근거: [토네이도 View 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md), [계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md) TP-9

## 바꾼 곳

`Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs` 한 파일입니다.

- `var tornado = new FieldTornadoPresenter(field.CellsRoot, tables.Terrains);`를 더했습니다.
- `result.Own(tornado.Release);`로 실행 해제 때 함께 정리합니다.
- `new CombatFieldEventPorts(...)`의 위치 인자에서 `terrain.Hide`·`terrain.ShowAt`을 뺐고, 끝에 `tornado`를 더했습니다.

## 왜

- 필드 창구 `CombatFieldEventPorts`가 토네이도 표시를 필수로 받습니다. 전투 사건 Presenter가 이 창구에서 처리기를 만들기 때문입니다.
- 미리보기는 토네이도를 그리지 않으므로 빈 표시를 넘깁니다. `Render`를 부르지 않아 화면 변화와 비용이 없습니다.
- 지형 표시의 `Hide`·`ShowAt`은 옛 토네이도 전용이라 없앴습니다.

## 미리보기에서 달라지는 것

없습니다. 미리보기에서 토네이도를 보이려면 `Render`를 부르고 합성 사실에 토네이도 사건을 넣어야 합니다. 지금은 범위 밖입니다.

## 2026-11-02 낙석 — 같은 파일 줄 추가

- `var rockfall = new FieldRockfallPresenter(field, tables.Rockfall, backdrop.RockfallTunnel);`를 더하고 `new CombatFieldEventPorts(...)` 끝에 넘깁니다. 토네이도처럼 빈 표시이고 `Render`를 부르지 않습니다.
- 미리보기에서 달라지는 것은 없습니다. 근거는 [낙석 계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md) RP-4입니다.

## 2026-11-02 하붑 — 같은 파일 줄 추가, 공격 조준 변경

- `CombatTraitPreviewFactory.cs`: 이펙트 창구 만드는 줄을 필드 창구보다 앞으로 옮겼습니다. `var haboob = new FieldHaboobPresenter(tables.Haboob, effects);`를 더하고 `new CombatFieldEventPorts(...)` 끝에 넘깁니다. 빈 표시이고 `Render`를 부르지 않습니다.
- `AttackTargetingInteraction.cs`: 칸 조준에서 목록에 없는 칸을 누르면 이유를 묻는 미리보기를 한 번 보냅니다. 답을 받으면 안내만 내고 호버 표시는 건드리지 않습니다. 조준 동작 자체는 같습니다.
- 미리보기에서 달라지는 것은 없습니다. 근거는 [하붑 계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md) Q-HP1입니다.

## 2026-11-02 낙석 능선 — 같은 파일 줄 변경

- `CombatTraitPreviewFactory.cs`: `new FieldRockfallPresenter(field, tables.Rockfall, backdrop.RockfallTunnel, backdrop.RockfallRidgeStart, backdrop.RockfallRidgeEnd)`로 인자 둘을 더했습니다. 빈 표시이고 `Render`를 부르지 않습니다.
- 미리보기에서 달라지는 것은 없습니다. 근거는 [낙석 계획 §7](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)입니다.

## 2026-11-02 하붑 구름 이동 — 이펙트 손잡이 입구 추가

- `CombatEffectHandle.MoveTo`·`CombatEffectFollower.MoveTo`를 더했습니다. 받침을 월드 위치로 옮기는 입구이고, 유닛을 따라가지 않는 이펙트에만 씁니다. 지금 부르는 곳은 하붑 표시 하나이고, 다른 이펙트의 동작은 그대로입니다.
- `CombatTraitPreviewFactory.cs`는 바뀌지 않았습니다. 근거는 [하붑 계획 §1.2](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md)입니다.
