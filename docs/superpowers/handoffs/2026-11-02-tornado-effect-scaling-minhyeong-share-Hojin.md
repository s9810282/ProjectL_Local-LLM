# 토네이도 이펙트 파티클 크기 방식 — 민형님 공유 (2026-11-02, 미전달)

전투 View 쪽에서 토네이도 바깥 프리팹의 덮어쓰기를 고쳤습니다. 이펙트 원본과 코드는 그대로입니다. 기록은 [토네이도 View 계획 §4](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md)에 있습니다.

## 1 증상과 원인

- 전투에서 바닥 Smoke만 크게 나오고, 소용돌이는 외형 배율과 관계없이 같은 크기로 나왔습니다.
- `Script/Render/Effect/EventEffect/Toneido/Toneido.prefab`의 파티클 셋(Toneido·Smoke·Rock)은 Scaling Mode가 Local입니다. 그래서 바깥 프리팹이 준 `Toneido` 크기 0.26은 소용돌이에만 적용되고 자식 Smoke·Rock에는 안 내려갔습니다. 코드가 주는 배율(외형 `Local Scale` × 칸 비율)과 등장 커짐도 셋 다 무시했습니다.
- `[민형] 토네이도 수정`(`06835a862`)에서 바깥 프리팹 `Prefabs/Default/Environment/Terrain_Tornado_Toneido.prefab`에 Hierarchy 덮어쓰기 여섯을 넣어 두셨는데, 그 직전 `5770ce1b6`에서 `Toneido.prefab`의 GUID가 `03c1c8ad…` → `b98255d5…`로 바뀌어 덮어쓰기가 옛 GUID를 가리키고 있었습니다. 대상이 없어 Unity가 무시했습니다.

## 2 바꾼 것

- `Terrain_Tornado_Toneido.prefab`: 옛 GUID scalingMode 덮어쓰기 여섯을 걷고, 지금 `Toneido.prefab`의 파티클 셋(Toneido `5089760536900037838`·Smoke `7453062458445576254`·Rock `1659689391383188177`)에 Scaling Mode Hierarchy 덮어쓰기를 넣었습니다.
- `TerrainVisual_Tornado`의 `Local Scale`은 호진님이 1로 바꿔 둔 값 그대로입니다. 지금 소용돌이 실제 배율은 1 × 칸 비율 1.5 × 0.26 = 0.39입니다.

## 3 확인 부탁

- 이펙트 원본도 Hierarchy로 두실지 정해 주세요. 원본에서 바꾸시면 바깥 프리팹 덮어쓰기 셋은 지워도 됩니다.
- 같은 옛 GUID를 가리키는 재질 덮어쓰기 셋(`Terrain_Tornado_Toneido_Layer2~4.mat`)이 바깥 프리팹에 남아 있습니다. 지금 이펙트는 다른 재질을 써서 아무 효과가 없습니다. 지우셔도 되는지 봐 주세요(Unity 덮어쓰기 메뉴의 "Unused overrides" 정리).
- 토네이도 등장·사라짐은 루트 크기를 곡선대로 키우고 줄이는 방식(`CombatTornadoAppearance`)이라, 이제 파티클도 함께 커지며 나옵니다. 셰이더 방식으로 바꾸실 때 이 점도 함께 봐 주세요.
