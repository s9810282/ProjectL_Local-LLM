# 전투 지형 외형 — 타일 위 / 바닥 타일 교체 나누기 설계

- 날짜: 2026-10-29
- 상태: **결정 완료(2026-10-29 사용자)** — Q-TR1 고침(바닥 교체도 표면 높이를 가짐), Q-TR2 B, Q-TR3·Q-TR4 권장(A). Task 계획 [terrain-tile-replace-plan](../plans/2026-10-29-combat-terrain-tile-replace-plan.md).
- 요청(사용자, 2026-10-29): 지형 외형 `VisualMode`의 `GroundShape`를 (1) 벽·고지대처럼 타일 위로 올라가는 것과 (2) 바닥 타일이 바뀌는 것으로 나눈다.
  - 바닥 교체는 실제로 타일을 바꾼다. 약한 지반은 타일 한 장과 같은 크기의 프리팹으로 바꾸고, 낭떠러지는 그 칸의 타일을 비워서 보인다.
  - 효과 프리팹(옛 PlacedPrefab)은 타일 위 쪽과 합쳐 두 값으로 둔다. 표면 높이 처리에 셋이 필요하면 셋으로 나눈다.
  - 바닥 교체도 표면 높이를 가진다(Q-TR1 답).
  - 낭떠러지 추락은 지금 작아지기만 하지만, 이 변경 뒤에 작아지면서 구멍 아래로 떨어지게 할 예정이다(Q-TR2 답, 이번 범위 밖).
- 관련: [타일 맞춤 설계](2026-10-28-combat-map-tile-alignment-design.md), [타일 높이 설계](2026-10-28-combat-map-tile-height-design.md), [고지대 View 설계](2026-10-20-combat-high-ground-view-design.md), [약한 지반 설계](2026-10-27-combat-weak-ground-collapse-after-move-design.md) WG6, [지형 이웃 모양 설계](2026-10-27-combat-terrain-visual-variants-design.md)

## 1. 지금 코드 (2026-10-29, HEAD `3be9ad77`)

### 1.1 표시 방식이 하는 일

`TerrainVisualMode`는 `PlacedPrefab = 0`, `GroundShape = 1` 두 값이다. 코드에서 읽는 곳은 검사 두 줄뿐이고, 그리는 법은 방식과 상관없이 같다.

| 읽는 곳 | 하는 일 |
|---|---|
| `TerrainVisualTable` 생성자 | `PlacedPrefab`이면 프리팹이 있어야 한다 |
| `TerrainVisualData` 생성자 | 표면 높이가 0보다 크면 `GroundShape`여야 한다(블록 없이 유닛만 뜬 화면을 막음) |

`FieldTerrainPresenter`는 종류마다 프리팹이 있으면 그린다(`CanDisplay`). 놓는 자리는 토네이도를 빼고 모두 칸 바탕(`CellBaseToLocal` = 타일 윗면, 타일이 없으면 바닥면) + 외형 보정이다. 표면 높이는 모든 종류가 "바탕 위로 올림"(`FieldSurfaceHeights.Raise`, 0 이상)으로 쓴다.

### 1.2 지금 에셋

| 종류 | 방식 | 프리팹 | 표면 높이 | 원하는 분류 |
|---|---|---|---|---|
| Wall | PlacedPrefab | 있음 | 0 | 타일 위 |
| HighGround | GroundShape | 있음(이웃 모양 셋 포함) | 0.45 | 타일 위 |
| Sandstorm·Tornado·PresenceModifier | PlacedPrefab | 있음 | 0 | 타일 위(효과) |
| WeakGround | GroundShape | `Terrain_WeakGround`(금 간 사각형) | 0 | 바닥 교체 |
| Cliff | GroundShape | `Terrain_Cliff`(검은 사각형) | 0 | 바닥 교체 |

### 1.3 바닥 타일

- 배경 방식 `PrefabWithTiles`(지금 Test 스테이지)만 타일이 있다. `StageMapTiles.Build`가 배경 아래 `MapTileRoot`에 타일 맵 칸마다 프리팹을 하나씩 만들고, 그 뒤로는 아무도 건드리지 않는다("전투 중에 바뀌는 타일이 없어 풀 없이 만듦").
- 필드 칸과 타일의 대응 규칙은 하나다: 칸 가운데가 놓인 타일(`CombatMapTilesData.TryTileAt`). `CombatMapTileSurface.Build`가 이 규칙으로 칸별 타일 윗면 높이 표를 만들고, 전투 표시가 설치 때 높이 표의 바탕 층으로 넣는다(`FieldView.SetGroundBase`).
- 타일 윗면 높이(`CombatMapTileVisual.SurfaceHeight`)는 필드 바닥면(0) 기준 절대 높이이고 음수가 된다.
- 타일 맵 밖 칸과 타일이 없는 배경(`Prefab`·`MapDefinition`, 운영 A·B·C·보스)은 바닥면(높이 0)이다.
- 타일 한 장과 칸 하나의 크기는 데이터가 따로 정한다. 지금 Test는 한 장 = 한 칸이다.

### 1.4 높이 표

`FieldSurfaceHeights`는 칸마다 바탕(`_base`, 설치 때 한 번 넣는 타일 윗면)과 지형이 올린 높이(`_terrain`, 0 이상)를 따로 든다. 칸 높이 = 바탕 + 지형이고, 격자 높이 = 지형만이다(TH4 개정 2). 다시 채우기(`BeginRefill`~`EndRefill`)는 바탕에서 시작해 지형을 올린다. `EndRefill`은 칸 높이만 비교한다("바탕은 다시 채우기 사이에 바뀌지 않으므로 칸 높이가 같으면 격자 높이도 같다").

### 1.5 사건 시점

약한 지반이 무너지면 같은 원본의 종류가 낭떠러지로 바뀐다. `FieldTerrainPresenter.ShowChanged`가 붕괴 순간 외형을 바꾸는데, 이웃 모양을 쓰거나 표면 높이가 다르면 결과 끝 `Render`에 맡긴다. 지금 운영 스테이지에는 약한 지반·낭떠러지 배치가 없고, Test에만 약한 지반이 있다(칸 (6,3)).

## 2. 설계

### TR1 표시 방식은 두 값, 표면 높이의 뜻은 방식마다

```csharp
public enum TerrainVisualMode
{
    OnTile = 0,      // 옛 PlacedPrefab: 바닥 타일 위에 프리팹을 놓습니다(효과·벽·고지대 블록)
    ReplaceTile = 1  // 옛 GroundShape: 그 칸의 바닥 타일을 지형 외형으로 바꿉니다(약한 지반·낭떠러지)
}
```

- 정수 값을 그대로 쓴다. 에셋에서 방식이 바뀌는 것은 HighGround 하나(1 → 0)다. Wall은 이미 0이다.
- 두 방식 모두 표면 높이를 가진다(Q-TR1 답). 뜻은 다르다.

| 방식 | 표면 높이의 뜻 | 범위 | 프리팹 | 이웃 모양 | 토네이도 |
|---|---|---|---|---|---|
| OnTile | 바탕 위로 올리는 높이(블록 윗면 − 바탕) | 0 이상 | 필수 | 허용 | 이 방식만 |
| ReplaceTile | 그 칸의 새 바탕 높이(바뀐 타일의 윗면, 필드 바닥면 기준 절대 높이) | 유한하면 음수도 됨 | 선택(비면 타일만 비움) | 허용 | 불가 |

- `ReplaceTile`의 표면 높이는 타일 세트의 윗면 높이(`CombatMapTileSetSO`)와 같은 뜻이다. "그 칸의 타일을 다른 종류로 바꾸는 것"이라서다. 약한 지반은 그 타일의 윗면, 낭떠러지는 구멍 바닥 높이(음수)를 적는다.
- 효과 프리팹(모래폭풍 등)에 표면 높이를 잘못 넣는 실수는 막지 않는다. 데이터 실수이고 화면에서 바로 드러난다. 셋으로 나누지 않는 이유다.
- 검사 자리는 지금 그대로다(데이터 생성자·표). 이름과 조건만 바꾼다.

### TR2 칸 ↔ 타일 대응은 지금 높이 표 한 곳

`CombatMapTileSurface.Build`가 칸마다 타일을 찾는 같은 반복에서 그 타일 오브젝트(`Transform`)도 적는다. 대응 규칙(`TryTileAt`)은 한 번만 돌고, 높이와 타일 오브젝트가 같은 답을 쓴다.

- 이름은 그대로 둔다. 문서 주석만 "칸별 바닥 타일(윗면 높이·타일 오브젝트)"로 고친다. 그래서 표시 생성자 인자와 Factory 줄(`backdrop?.TileSurface`)은 바뀌지 않는다.
- 새 읽기 `TryGetTile(cell, out Transform tile)`을 둔다. 타일이 없는 칸이나 이미 파괴된 타일이면 false다.
- 타일 한 장을 칸 여럿이 나누면(타일이 칸보다 큼), 한 칸의 교체가 그 타일 전체를 비운다. 맞춤 때 경고를 한 번 남기고 그대로 둔다(Q-TR3 A).

### TR3 교체 그리기

| 칸 | 타일 | 교체 프리팹 |
|---|---|---|
| 타일이 있는 칸 | 끈다 | 있으면 끈 타일과 같은 월드 자세(위치·회전·크기)에 놓고 외형 보정·회전을 더한다(Q-TR4 A) |
| 타일이 없는 칸(배경 `Prefab`·`MapDefinition`, 타일 맵 밖) | 없음 | 있으면 칸 가운데 바닥면(높이 0) + 외형 보정 |

- 교체 프리팹은 타일 프리팹처럼 만든다(같은 크기·같은 피벗). 그러면 끈 타일 자리에 그대로 맞는다.
- 크기는 타일의 월드 크기를 지형 루트 기준으로 바꿔 넣는다. 타일 맞춤이 타일 축과 필드 축을 평행하게 만들므로(`RequireLevel` + 90° 단위 회전 보정) 축별 나눗셈으로 바꿀 수 있다.
- 타일이 없는 칸에서는 타일처럼 만든 프리팹이 바닥면 위로 튀어나올 수 있다. 지금 타일 없는 운영 스테이지에는 교체 지형 배치가 없어 받아들인다.
- 낭떠러지 프리팹은 비운다(Q-TR2 B). 타일 칸은 빈 구멍이 되고, 타일 없는 칸에서는 낭떠러지가 보이지 않는다. 지우지 않는 `Terrain_Cliff.prefab`·`CliffSurface.mat`은 참조 없는 에셋으로 남는다(보고만).

### TR4 타일 켜고 끄기의 주인은 지형 표시

- 새 부품 `Display/Map/FieldTileReplacement`를 `FieldTerrainPresenter`가 갖는다. 부품은 칸 ↔ 타일 표(TR2)를 받고, 이번에 끌 칸을 모은 뒤 타일 켜짐을 맞추고, 정리 때 모두 켠다.
- 끌 칸은 보이는 지형 판단 하나(`TerrainViewSet.VisitShown`)로 모은다. 이웃 표를 채우는 같은 방문에서 함께 표시한다. 그래서 공개되지 않은 약한 지반 칸의 타일은 그대로 있다.
- 프리팹 유무와 상관없이 끈다. 낭떠러지 프리팹이 비어 있어도 타일은 꺼진다.
- `Clear`·`Release` 때 모든 타일을 다시 켠다. 전투 중간 분리(배경은 남음) 뒤 다시 붙으면 다음 `Render`가 다시 끈다.
- 배경은 타일을 만든 뒤 켜짐을 건드리지 않는다. 배경이 먼저 없어져 타일이 파괴됐으면 건너뛴다.
- 연결: 표시 `Install`이 `SetGroundBase` 옆에서 같은 표를 지형 표시에 넘긴다(한 줄). 타일이 없는 배경이면 null이고, 모든 칸이 "타일 없음"이다.

### TR5 높이 표 — 바탕 바꾸기

- 바탕을 둘로 나눈다. 타일 바탕(설치 때 한 번, 지금 `_base`)과 이번 다시 채우기의 바탕(타일 바탕에서 시작, 교체 지형이 바꿈)이다.
- 새 입구 `Replace(cell, height)`는 그 칸의 이번 바탕을 교체 지형의 표면 높이로 바꾼다. 칸 높이 = 이번 바탕 + 지형 높이다. 한 칸에 교체 원본이 여럿이면 낮은 값이 남는다(구멍이 이김).
- `Raise`는 그대로다(지형 높이, 0 이상). 같은 칸에 교체와 올림이 함께 있어도 순서와 상관없이 같은 값이 나온다.
- 격자 높이(지형 높이만)는 바뀌지 않는다. 바닥 교체는 "타일이 바뀐 것"이라 타일 높이처럼 그리드에 영향을 주지 않는다(TH4 개정 2 규칙 그대로).
- 바탕이 다시 채우기마다 바뀔 수 있으므로 `EndRefill`은 칸 높이와 지형 높이를 모두 비교한다(칸 높이가 같아도 격자 높이가 다를 수 있음).
- `BaseHeightAt`은 이번 바탕을 돌려준다. 타일 위 지형(벽·토네이도 등)은 바뀐 바탕 위에 놓인다.
- 지형 표시의 높이 채우기(`RaiseSurface`)가 방식으로 갈린다: 타일 위 → `Raise`, 바닥 교체 → `Replace`.

### TR6 사건 시점

- 약한 지반 → 낭떠러지(붕괴 순간): 두 종류 모두 `ReplaceTile`이라 타일은 이미 꺼져 있다. 표면 높이가 달라도 외형만 바로 바꾼다(`TryReplace`). 높이 표는 결과 끝 `Refresh`가 맞춘다. 그 칸의 유닛은 추락 연출이 맡는다.
- `ShowChanged`가 결과 끝 `Render`에 맡기는 조건은 "이웃 모양을 씀", "옛·새 방식이 다름", "타일 위 종류이고 표면 높이가 다름"이다.
- 낭떠러지 프리팹이 비어 있으면 약한 지반 외형만 빠지고 구멍이 남는다.

### TR7 바뀌는 화면

- 낭떠러지 칸의 칸 높이가 구멍 바닥(음수)이 된다. 넉백으로 낭떠러지에 들어가는 걸음은 걸음 높이 식(`CombatStepHeight`)대로 구멍 바닥에서 끝나고, 그 뒤 추락 연출(가라앉기·작아지기)이 이어진다.
- "작아지면서 구멍 아래로 떨어지기"(사용자 예정)는 이번 범위 밖이다. 이 설계 뒤에는 그 연출이 칸 높이(구멍 바닥)를 떨어질 목표로 읽을 수 있다.
- 약한 지반 칸은 약한 지반 표면 높이에 유닛이 선다. 교체 프리팹 윗면을 그 값과 같게 만든다는 약속이다.

### TR8 바닥 교체 외형이 빠질 때 — 떨리고 가라앉기 (2026-10-29 추가, 결정 완료 — 사용자 "권장안대로" = Q-CL1 A·Q-CL2 A, 구현 계획 §6.4)

요청(사용자, TR-c 확인 뒤): 약한 지반이 낭떠러지로 바뀔 때 외형을 그냥 끄지 말고, 그 타일이 떨리다가 아래로 내려가게 한다.

지금 코드:
- 붕괴 순간 사실 처리기가 `ShowTerrainChange` → `FieldTerrainPresenter.ShowChanged` → `TerrainViewSet.TryReplace`를 부른다. 낭떠러지는 프리팹이 없어 약한 지반 외형을 바로 풀에 돌려준다(꺼짐).
- 뒤따르는 사건은 그 칸 유닛의 추락(`CombatFallEvent`)과 사망이다. 추락 연출은 가라앉기·작아지기다.
- 같은 꼴의 선례: 토네이도 외형 연출(`CombatTornadoAppearance`)을 지형 표시가 들고, 재생기 `CombatTornadoAppearancePlayer`가 매 프레임 진행·완료·취소를 맡는다. 재생 중이면 다음 사건 시작을 막고(`FindBlock`), 결과 끝 사실 반영도 기다린다(`BlocksFinalFacts`).

설계:
- 빠지는 외형을 풀에 바로 돌려주지 않고 "무너지는 중" 목록에 옮긴다. 떨림 → 가라앉기가 끝나면 그때 돌려준다.
  - `TerrainViewSet.TryReplace`에 내보내는 쪽 콜백을 하나 더 받는다. 연결에서 빠진 View를 반환 대신 그 콜백에 넘긴다(연결은 지금처럼 바로 바뀜).
  - 대상: 사건 시점에 빠지는 바닥 교체 외형(지금은 약한 지반 → 낭떠러지 하나). 결과 끝 `Render`·정리에서 빠지는 외형은 지금처럼 바로 돌려준다.
- 연출 객체 `Presentation/Environment/CombatTileCollapse`(새): 시작 자리(로컬)를 기억하고 시간으로 자리만 바꾼다.
  - 떨림: 가로 두 축으로 정해진 사인 흔들림(같은 값이면 늘 같은 모양).
  - 가라앉기: 위쪽 축 반대로 깊이만큼, 끝으로 갈수록 빨라짐(추락 연출의 가라앉기와 같은 곡선).
  - 끝나면 외형을 끄고 풀에 돌려준다. 취소·건너뛰기·정리면 바로 끝낸다.
- 재생기: 토네이도 외형 재생기를 "필드 외형 재생기"로 넓혀 무너짐도 진행·완료·취소한다. 다음 사건을 막는 구간은 Q-CL1이 정한다.
- 값: Q-CL2.

| 번호 | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-CL1 | 뒤따르는 사건(그 칸 유닛 추락)과의 순서 | A 떨림 동안 다음 사건을 기다리고, 가라앉기와 유닛 추락은 함께 / B 기다리지 않음(떨림과 유닛 추락이 동시) / C 다 가라앉은 뒤 다음 사건 | A — 유닛이 떨리는 타일 위에 서 있다가 함께 꺼진다. C는 유닛이 구멍 위에 떠 있는 순간이 생긴다 |
| Q-CL2 | 값 자리 | A 설정 표 `CombatPresentationSettings`에 "약한 지반 붕괴 연출" 묶음(떨림 시간·세기·빠르기, 가라앉기 시간·깊이) / B 지형 외형 SO의 바닥 교체 칸 | A — 이미 같은 표에 약한 지반 붕괴 소리·큐가 있고, 한 종류만 쓰는 칸을 지형 외형 SO에 더하지 않는다는 10-28 결정과 같음 |

- Q-CL1 A는 재생기가 "다음 사건을 막는가"(떨림 동안만)와 "끝났는가"(가라앉기까지)를 따로 물어야 한다. 그래서 필드 창구(`CombatFieldEventPorts`)의 토네이도 외형 칸 셋(`TickTornado`·`TornadoPlaying`·`CancelTornado`)을 외형 이름으로 바꾸고 "시작 막기" 칸 하나를 더한다. Factory 연결 줄(`CombatSceneDisplayFactory` 154~157)이 바뀌므로 민형님 공유가 필요하다.
- 기본값(코드): 떨림 0.35초·세기 0.04·빠르기 초당 25회, 가라앉기 0.5초·깊이 1.5. 기본 에셋의 세기는 사용자가 0.1로 바꿨다(2026-10-29 확인). 거리는 유닛 추락 깊이(`fallDepth`)와 같은 월드 단위다(구현 때 정함 — 그 칸 유닛과 함께 가라앉으므로 같은 단위로 둠).
- 범위 밖: 유닛이 구멍 아래로 떨어지는 추락 연출(사용자 예정 작업), 타일 위 외형(벽·고지대)이 빠질 때, 이펙트·소리(이미 붕괴 큐·소리 칸이 있음).

## 3. 설계에서 정한 것

- **P1** 끌 칸 판단은 보이는 지형 판단 하나(VisitShown)다. 공개되지 않은 교체 지형은 타일을 끄지 않는다.
- **P2** 타일 켜짐의 주인은 지형 표시(새 부품) 하나다. 정리 때 모두 켠다.
- **P3** 칸별 타일 오브젝트는 지금 높이 표(`CombatMapTileSurface`)에 더한다. 새 클래스·Factory 변경이 없다.
- **P4** 바닥 교체의 표면 높이는 그 칸의 새 바탕(절대 높이)이다. 한 칸에 여럿이면 낮은 값이다.
- **P5** 타일 위는 표면 높이 0 이상·프리팹 필수, 바닥 교체는 유한한 값·프리팹 선택, 토네이도는 타일 위만, 이웃 모양은 두 방식 모두 허용한다.
- **P6** 사건 시점에 방식이 바뀌거나 타일 위 종류의 높이가 바뀌면 결과 끝 `Render`에 맡긴다. 바닥 교체끼리는 높이가 달라도 바로 바꾼다.
- **P7** 타일이 없는 칸의 교체 프리팹은 칸 가운데 바닥면에 놓는다.

## 4. 결정 (2026-10-29 사용자)

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-TR1 | 방식 값 | 두 값 `OnTile`·`ReplaceTile`. 단, `ReplaceTile`도 표면 높이를 가진다(사용자 "ReplaceTile도 surface가 존재해야 함") → TR1·TR5 |
| Q-TR2 | 낭떠러지 프리팹 | B 비움(빈 구멍). 추락은 이 뒤에 "작아지면서 구멍 아래로"로 바꿀 예정(범위 밖) |
| Q-TR3 | 타일 한 장을 칸 여럿이 나눌 때 | A 경고 한 번 + 그대로 |
| Q-TR4 | 교체 프리팹 자세 | A 끈 타일과 같은 월드 자세 |

## 5. 바뀌는 파일 (예상)

| 파일 | 변경 |
|---|---|
| `Data/View/Table/Terrain/Item/TerrainVisualMode.cs` | 값 이름 둘·문서 |
| `TerrainVisualDataSO.cs` | 검사 조건(방식별 표면 높이·토네이도), 기본값, 툴팁 |
| `Queries/ViewDataBase/ViewTable/TerrainVisualTable.cs` | 검사 이름 |
| `TerrainVisual_HighGround.asset` | `visualMode` 1 → 0 |
| `TerrainVisual_Cliff.asset` | `prefab` 비움 |
| `Display/Field/FieldSurfaceHeights.cs` | 타일 바탕·이번 바탕, `Replace`, `EndRefill` 비교 |
| `Display/Map/CombatMapTileSurface.cs` | 칸별 타일 오브젝트·`TryGetTile`·나눔 사실 |
| `Display/Map/StageMapTiles.cs` | `Build`가 만든 타일을 맵 칸별로 돌려줌 |
| `Display/Map/StageViewPropPresenter.cs` | 표 만들 때 타일 넘김, 나눔 경고 |
| `Display/Map/FieldTileReplacement.cs`(새, + `.meta`) | 타일 끄고 켜기 |
| `Display/Map/FieldTerrainPresenter.cs` | 방식별 높이 채우기, 끌 칸 표시, 교체 자세, `ShowChanged` 조건, 정리 |
| `Display/CombatSceneDisplay.cs` | `Install` 한 줄 |

- Factory·ServiceKeys 변경 없음(민형님 공유 없음).
- 호진님 공유: SO 모드 3D 미리보기는 타일을 그리지 않고, 표면 높이를 모두 "올림"으로 읽는다(`CombatStageSoVisuals.RaiseSurfaces`). 바닥 교체의 높이를 올림으로 잘못 읽는다([SO 모드 3D 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) §5에 한 줄).
- 문서: [PlacedPrefab 지형 안내](../viewDocs/placed-prefab-terrain-guide.md)의 방식 설명, 폴더 배치 안내 `Display/Map/` 줄.
- 시험: 새 시험 없음(View 작업), `Assets/Tests`가 지워져 맞출 시험도 없다.

## 6. Phase

1. **TR-a 값**: 열거 이름·방식별 검사·HighGround 에셋, 높이 채우기는 타일 위만. 화면 변화 없음.
2. **TR-b 높이**: 높이 표 바탕 바꾸기, 바닥 교체가 바탕을 바꿈. 타일은 아직 그대로.
3. **TR-c 타일 교체**: 칸별 타일 표·새 부품·지형 표시·설치 한 줄·낭떠러지 에셋.
4. **TR-d 문서·공유·묶음 끝 검토**.

## 7. 7기준 예상

| 기준 | 예상 | 근거 |
|---|---|---|
| SSOT | 9 | 칸 ↔ 타일 대응 한 곳, 끌 칸 판단은 보이는 지형 판단 하나, 바탕 바꾸기 규칙은 높이 표 한 곳 |
| SOLID | 8.5 | 타일 켜짐은 새 부품 하나. 지형 표시가 자세 갈래 하나·높이 갈래 하나를 더 가짐 |
| FW | 9.5 | Framework 무변경 |
| 독립성 | 8.5 | Factory 무변경. 타일 켜짐을 배경(만들기)과 지형 표시(끄고 켜기)가 나눠 쓰는 규칙이 남음 |
| 캡슐화 | 8.5 | 타일 오브젝트는 표의 읽기 하나로만 나감 |
| C#/Unity 권장 | 8.5 | 크기 변환이 축 평행 가정에 기댐(맞춤이 보장) |
| If/Tick | 9 | 매 프레임 일 없음, Render 때만 |

평균 약 8.8이다.

## 8. 구현 메모 (2026-10-29, TR-a~TR-c2 코드·에셋·검토 반영, 기록 계획 §6.1~§6.6)

- **TR-a 값:** 열거 `OnTile`·`ReplaceTile`(정수 그대로), 생성자 검사(유한, 타일 위 0 이상, 토네이도는 타일 위만), "높이 있으면 프리팹" 검사는 표의 "타일 위는 프리팹 필수" 하나로(계획 P8), 칸의 `Min(0)` 뗌. 고지대 에셋 1 → 0. Ua 문제 없음.
- **TR-b 높이:** `FieldSurfaceHeights`에 타일 바탕과 이번 바탕, `Replace`(첫 교체는 타일 바탕을 덮고 그다음 낮은 값, 같은 값이면 버전을 올리지 않음), `EndRefill`·`Reset`이 칸·지형 높이 둘 다 비교(버전 규칙은 검토 L8로 바뀜, 아래). 지형 표시 `FillSurface`가 방식으로 갈림. Ub 문제 없음.
- **TR-c 타일 교체:** 칸 ↔ 타일 표(`CombatMapTileSurface`)가 칸별 타일 `Transform`도 듦(P3, 타일 나눔 사실 → 배경 표시 경고), 새 `FieldTileReplacement`(자기가 끈 타일만 기억·다시 켬, 바뀌는 타일만 `SetActive`), 지형 표시가 이웃 표 방문에서 끌 칸도 표시, 교체 인스턴스는 쓰는 동안 `MapTileRoot` 아래 타일 자리(계획 P11·P12 — 설계 TR3의 "크기 축별 변환" 대신 부모 옮기기), `ShowChanged`는 TR6 조건, 낭떠러지 프리팹 비움. 90° 식은 `FieldGridMapper.YawOf` 하나(계획 밖, 검토 M1로 지움). Uc 문제 없음.
- **TR-c2 무너짐(TR8):** 설정 표 묶음·값 객체 `CombatTileCollapseSettings`·연출 `CombatTileCollapse`(가라앉기 곡선은 `CombatSinkPiece.SinkDistance` 하나), `TerrainViewSet.TryReplace`의 `leave`, 지형 표시 무너짐 목록·`IsAppearanceBlockingStart`, 재생기 `CombatFieldAppearancePlayer`(옛 토네이도 재생기, `git mv`)·막는 이유 `FieldAppearance`, 필드 창구 칸 이름, Factory 연결 줄. 거리 단위는 유닛 추락과 같은 월드 단위(초안의 "필드 단위"에서 바꿈). Uc2 문제 없음.
- **설계와 다른 점:** P11(부모 옮기기), 거리 단위, `TerrainViewSet` 주석 두 줄.
- **묶음 끝 검토 반영(계획 §6.5·§6.6, 사용자 "권장안대로", L5만 그대로):**
  - 타일 칸 외형의 회전은 필드 기준이다(M1). 위치·배율만 타일 루트 기준이다(P12 일부 바뀜).
  - 무너짐 목록은 `Presentation/Environment/CombatTileCollapseSet`이 갖는다(M2).
  - 높이 표 `Version`은 덧그리기(지형) 높이만 센다(L8). 바닥 교체·타일 바탕은 격자를 다시 그리지 않는다.
  - 토네이도 바탕은 타일 바탕 `TileBaseHeightAt`이다(L4). "타일 위는 프리팹 필수"는 외형 Data 생성자 한 곳이다(L2, P8 바뀜).
  - 떨림은 끝에 0으로 줄어든다(L7).
- **유닛 추락 연출(TR2의 "이 뒤 예정", 계획 §6.6):** 크게 떨어지며 끝에만 작아진다.
  - 기본값은 0.6초·깊이 3이고, 새 값 `fallShrinkStart` 0.5 지점부터 작아진다.
  - 칸 낭떠러지(빈 구멍)와 약한 지반 붕괴 둘 다 같은 조립 `ComposeFall`을 쓴다.
- **참조가 없어진 에셋(지우지 않음, 보고만):** `Prefabs/Default/Environment/Terrain_Cliff.prefab`(+ 그것만 참조하는 `Environment/Cliff/CliffSurface.mat`), `Prefabs/Default/Environment/Terrain_WeakGround.prefab`(+ `WeakGround/WeakGroundSurface.mat`·`WeakGroundCrack.png`) — 약한 지반 외형은 사용자가 만든 `Prefabs/Stage/MapProp/Test/WeekGround.prefab`으로 바뀜.
- **공유(모두 미전달):** 민형님 Factory 공유 §39, 호진님 설정 표 메모 §11(설정 묶음·재생기·창구 이름·외형 방식)·§12(추락 값·검토 반영), SO 모드 3D 사본 공유 §6(도구 높이 사본).
- **남은 것:** 사용자 커밋. 같은 작업 트리에 다른 작업(이동 경로 화살표·경로선 값 나눔)과 사용자 에셋 변경(Test 타일 프리팹 이름 바꿈 `Tile1`·`Tile2`, `WeekGround.prefab`)이 섞여 있어 커밋 묶음은 계획 §5를 본다.
