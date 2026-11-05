# 전투 맵 바닥 타일 높이 화면 반영 Task 계획

- 날짜: 2026-10-28
- 설계: [타일 높이 설계](../specs/2026-10-28-combat-map-tile-height-design.md) 3판(TH1~TH6). 결정: Q-TH1·Q-TH3·Q-TH4 A, Q-TH2 B(지형은 타일 윗면 위), Q-TH5 B(들어간 타일 = 음수), Q-TH6 C(높이 표에 바탕 층).
- 상태: 계획(코드 착수 전). **계획에서 정한 것 P1~P14는 사용자 확인 대상.**

## 0. 진행 규칙과 착수 조건

- Phase마다 멈추고 보고한다. 새 시험은 쓰지 않고 MSBuild도 돌리지 않는다(View 작업 지시). 보고에는 "컴파일 미확인"을 적는다.
- 검토 에이전트는 묶음 끝(TH-e)에 한 번.
- 착수 조건:
  - 스테이지된 타일 맞춤 묶음(개정 5)과 그 뒤의 타일 세트 높이 칸(맞춤 계획 §6.8, 작업 트리)을 먼저 커밋하는 것을 권한다. TH-a가 같은 파일(`CombatMapTilesData`·`CombatMapTileSetSO`·`CombatMapTileVisual`·`StageViewPropPresenter`)을 고친다.
  - TH-c는 `CombatSceneDisplayFactory`·`CombatSceneDisplay`를 고친다. 이 둘은 이펙트 창구 확장 계획(EC)·걸음 모양 계획(ST)도 고칠 예정이다. 그 작업이 같은 때 진행 중이면 순서를 사용자가 정한다.

## 1. 계획에서 정한 것 (확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| P1 | 바탕 넣기 API는 `FieldSurfaceHeights.ClearBase()` + `SetBase(칸, 높이)`. 타일 표가 스스로 넣는다(`CombatMapTileSurface.ApplyBaseTo(표)`). 표시는 `Install`에서 `_field.Build` 직후 `ClearBase` → `ApplyBaseTo`를 부른다. 바뀐 바탕은 바로 뒤 `RefreshCore`의 다시 채우기에서 화면에 나온다. | 칸 순서 규칙(행 × 가로 + 열)이 표 안 한 곳에 남는다. 같은 크기 필드를 재사용해도(`FieldView.Build`가 Mapper를 그대로 둠) 설치마다 바탕을 다시 넣는다. |
| P2 | `Raise(칸, h)` = `max(지금 값, 바탕 + h)`, h ≤ 0이면 바꾸지 않음. 0 이상 검사는 그대로. | 지형은 타일 윗면 위(Q-TH2 B). 높이 0 지형은 바탕 그대로. |
| P3 | 이름: `RaisedLevels` → `Levels`(0이 아닌 높이, 오름차순), `HasRaised` → `HasLevels`, 새 `MinHeight`(0 이하)·`BaseHeightAt(칸)`. `MaxHeight`는 0 이상 그대로. `Reset`·`BeginRefill`은 바탕으로. | "높은 칸"이 아니라 "바닥이 아닌 칸"이 뜻이 된다. 부르는 곳은 셋(강조·포인터·데칼)뿐이다. |
| P4 | Mapper 자리 함수: `CellSurfaceToLocal`·`CellBaseToLocal`·`CellBaseToWorld`를 더하고 `CellSurfaceToWorld`는 `LocalToWorld(CellSurfaceToLocal)`로. `CellCenterToWorld`(바닥)는 그대로(레일·데칼). | 놓는 자리 규칙 둘(바탕 / 표면)이 Mapper 한 곳. |
| P5 | `SurfaceHeightOf(칸들, 기준)` 시작 값을 0이 아니라 첫 칸 높이로(설계에 없던 고침). | 지금은 0에서 큰 값을 골라, 들어간 칸만 점유한 여러 칸 유닛이 바닥(0)에 뜬다. 넉백 화살표도 이 값에서 시작하므로 들어간 칸 위에서도 실제 높이를 따른다(설계 TH4의 "화살표는 바닥 높이" 문장은 고친다). |
| P6 | ~~데칼: `FieldGridDecal.Show`에 `surfaceBottom`(0 이하) 인자, 상자를 아래로 `surfaceBottom − depth/2`까지.~~ **되돌림(§8.5, 2026-10-28 사용자):** 데칼은 들어간 칸을 보지 않는다. 아래 끝은 늘 바닥면 아래 depth/2. | 격자·호버·포인터는 들어간 칸을 바닥면으로 본다(설계 TH4 개정). |
| P7 | ~~포인터 평면 순서: 양수 높이들 → 바닥 평면(조건부) → 음수 높이들 → 바닥 평면.~~ **되돌림(§8.5):** 양수 높이들(내림차순) → 바닥 평면. 들어간 칸은 바닥 평면에서 잡힌다. | 호버 테두리를 바닥면에 그리므로 포인터도 같은 높이여야 커서와 어긋나지 않는다. |
| P8 | 타일 표 `CombatMapTileSurface`(`Display/Map/`, 불변): 가로·세로·칸별 높이, `HeightAt(칸)`, 정적 `Build(칸 루트, 타일 루트, 타일 배치, 보드 크기, 칸 간격)`, `ApplyBaseTo`. 역함수는 `CombatMapTilesData.TryTileAt(타일 루트 로컬 점, out 가로, out 세로)`(격자 모서리 옆). | 대응 규칙은 데이터 한 곳(`CenterOf`의 짝), 계산은 −195에 한 번. |
| P9 | 칸 가운데 높이 원본: 칸 가운데를 칸 루트 로컬(`FieldGridMapper.CornerLocal` + 반 칸)로 만들고 → 월드 → 타일 루트 로컬 → `TryTileAt`. 타일 맵 밖이면 0. 높이 = `VisualOf(KindAt).SurfaceHeight`(필드 단위, Q-TH1 A). | 회전·타일 한 장 = 칸 k개는 변환이 맡는다. |
| P10 | 표시 생성자에 새 필수 인자 `CombatMapTileSurface tileSurface`(null이면 타일 없음 = 바탕 0). Factory가 `backdrop?.TileSurface`를 넘긴다(연결 줄, 민형님 공유). | 맵 카메라 사실과 같은 길. 표시 생성은 Factory 한 곳(시험 호출 없음 확인). |
| P11 | 크기가 다르면(타일 표 보드 ≠ 설치 보드) `ApplyBaseTo`가 `InvalidOperationException`. | 같은 전투면 늘 같다. 다르면 조립 오류다. |
| P12 | 타일 세트 높이: `Min(0)` 걷기, 검사는 유한 값만(세트 SO·`CombatMapTileVisual` 둘 다), 툴팁을 Q-TH1 A 뜻으로. | Q-TH5 B. |
| P13 | 놓는 자리: 지형 외형 전부(`FieldTerrainPresenter.LocalPosition`, 회오리 반 칸 보정 포함) = 바탕, 오브젝트(`FieldObjectPresenter.LocalPosition`)·가시밭(`FieldThornPresenter`)·환경 폭발(`CombatEnvironmentEffectPresenter`) = 표면, 레일·기차 = 바닥 그대로. 오브젝트 몸(`CombatBody.Flatten`)은 가로만이라 그대로. | 설계 TH5. |
| P14 | 호진님 SO 모드 도구의 3D 미리보기(`CombatStageSoVisuals`)는 지형 높이 규칙 사본을 갖고 타일 높이를 모른다. 고치지 않고 공유 메모로 알린다. | 다른 작업자 영역. |

## 2. TH-a 값과 칸 대응 (화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| a.1 | `CombatMapTileSetSO.cs`·`CombatMapTileVisual.cs` | P12 |
| a.2 | `CombatMapTilesData.cs` | `TryTileAt`(P8), 문서 |
| a.3 | 새 `Display/Map/CombatMapTileSurface.cs`(+ `.meta`) | P8·P9(`ApplyBaseTo`·P11은 b.1로, §8.1) |
| a.4 | `StageViewPropPresenter.cs` | `ShowTiles` 맞춤 뒤 `TileSurface` 만들기(칸 간격·보드 크기는 이미 있음), `Release`에서 비움, 문서 |

확인 Ua: 컴파일, Test·다른 스테이지 진입이 지금과 같음.

## 3. TH-b 높이 표와 읽는 곳 (타일이 없으면 화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| b.1 | `FieldSurfaceHeights.cs`·`CombatMapTileSurface.cs` | 바탕 배열·P1·P2·P3, `ApplyBaseTo`·P11(TH-a에서 옮김), 문서(채우는 쪽 = 바탕 + 지형) |
| b.2 | `FieldGridMapper.cs` | P4·P5 |
| b.3 | `FieldOutlineRenderer.cs` | 이름 바뀐 목록(P3), 데칼에 아래 높이(P6) |
| b.4 | `FieldGridDecal.cs` | P6 |
| b.5 | `FieldView.cs` | P7, `RepaintSurface` 문서 |
| b.6 | `FieldTerrainPresenter.cs` | `SyncSurface` 문서(바탕에서 시작), `RaiseSurface`는 그대로(P2가 0 이하를 거름) |

확인 Ub: 컴파일, 고지대 스테이지(보스·Test 고지대 칸)에서 유닛·강조·포인터·데칼·걸음이 지금과 같음.

## 4. TH-c 연결 (타일 높이가 화면에 나옴)

| Task | 파일 | 내용 |
|---|---|---|
| c.1 | `CombatSceneDisplay.cs` | 생성자 인자(P10), `Install`에서 `_field.Build` 직후 `ClearBase` → `ApplyBaseTo`(P1) |
| c.2 | `CombatSceneDisplayFactory.cs` | `backdrop?.TileSurface`를 넘김(연결 줄) |

확인 Uc: 설계 §7 1·2·3(타일 +0.3 / −0.3, 높이 0 지형), 높이 모두 0이면 지금과 같음.

## 5. TH-d 놓는 자리

| Task | 파일 | 내용 |
|---|---|---|
| d.1 | `FieldTerrainPresenter.cs` | 지형 외형 = 바탕(P13) |
| d.2 | `FieldObjectPresenter.cs`·`FieldThornPresenter.cs`·`CombatEnvironmentEffectPresenter.cs` | 표면(P13) |

확인 Ud: 설계 §7 3(고지대·낭떠러지·약한 지반이 타일 윗면에)·5(지뢰·가시), 붕괴로 바뀐 지형도 타일 윗면.

## 6. TH-e 문서·공유·검토

- 민형님 Factory 공유 문서에 c.2 한 줄, 호진님 메모에 P14.
- 설계 §9 구현 메모(설계와 다른 것: P5 화살표 문장), 타일 맞춤 계획 §6.8 "열린 것"을 "화면용으로 정함 → 이 계획"으로.
- 폴더 배치 안내에 `CombatMapTileSurface`(Display/Map).
- 묶음 끝 검토 에이전트 하나(7기준), 권장 반영은 사용자 결정.

## 7. 커밋 묶음 (사용자가 커밋)

- TH-a~TH-d 코드 + 새 `CombatMapTileSurface.cs`(+ `.meta`)는 한 묶음이 안전하다. TH-c의 표시 생성자 인자와 Factory 줄은 반드시 함께.
- 타일 맞춤 묶음을 먼저 커밋했다면 그 위에 얹는다.

## 8. 실행 기록

### 8.1 TH-a 코드 완료 (2026-10-28, 사용자 "TH-a 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 화면 변화 없음)

- 기준: 타일 맞춤 묶음은 사용자 커밋 `407714d7`. 그 위에 맞춤 계획 §6.8(타일 세트 높이 칸·`CombatMapTileVisual`)이 커밋 전으로 있고, TH-a는 그 위에 얹었다.
- a.1 `CombatMapTileSetSO.surfaceHeight`: `Min(0)` 걷음, 검사는 유한 값만, 툴팁 = Q-TH1 A 뜻(필드 바닥면 기준 위아래, 필드 단위, 음수 = 들어간 타일). `CombatMapTileVisual`도 유한 값만·문서.
- a.2 `CombatMapTilesData.TryTileAt(타일 루트 로컬 점, out 가로, out 세로)` — 격자 모서리 `Corner` 옆(규칙 한 곳), 높이는 보지 않음.
- a.3 새 `Display/Map/CombatMapTileSurface.cs`(+ `.meta`, GUID `c72e8da9…`): 불변 `float[가로, 세로]`, `Columns`·`Rows`·`HeightAt(칸)`(범위 밖 예외), 정적 `Build(칸 루트, 타일 루트, 타일 배치, 보드, 칸 간격)` — 칸 가운데(`FieldGridMapper.CornerLocal` + 반 칸) → 월드 → 타일 루트 로컬 → `TryTileAt` → `VisualOf(KindAt).SurfaceHeight`, 타일 맵 밖 0.
- a.4 `StageViewPropPresenter`: `ShowTiles` 맞춤 뒤 `_tileSurface = CombatMapTileSurface.Build(...)`, 속성 `TileSurface`(타일 방식이 아니면 null), `Release`에서 비움. 카메라 읽기 실패 등으로 `Release`되면 함께 비워진다.
- **계획과 다른 점 하나:** P8의 `ApplyBaseTo`와 P11 크기 검사는 TH-b b.1로 옮겼다. 넣을 곳(`FieldSurfaceHeights.SetBase`)이 TH-b에서 생기므로 TH-a에 두면 컴파일이 깨진다. 칸 순서 규칙은 표 안 2차원 배열이라 따로 식이 없다.
- 보고: 임시 세트 에셋의 사용자 값 `surfaceHeight` Temp1 = 1·Temp2 = 0.8은 §6.8의 옛 뜻(피벗 → 윗면, 타일 루트 단위)으로 넣은 값으로 보인다. 새 뜻(바닥면 기준 위아래, 필드 단위)에서는 두 종류가 모두 1·0.8만큼 높은 칸이 된다. TH-c에서 화면에 나오기 전에 기본 타일 = 0, 다른 타일 = 그 차이(필드 단위 = 타일 루트 차이 × 배경 배율 0.5)로 바꿔야 한다. 에셋은 고치지 않았다.
- 확인 Ua(사용자): 컴파일, Test·다른 스테이지 진입이 지금과 같음.

### 8.2 TH-b 코드 완료 (2026-10-28, 사용자 "TH-b 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 화면 변화 없음)

- 바탕은 아직 아무도 넣지 않는다(TH-c). 그래서 바탕은 늘 0이고 칸 높이·높이 목록·포인터 점·데칼 상자가 지금과 같다.
- b.1 `FieldSurfaceHeights`: 바탕 배열 `_base`, `ClearBase()`·`SetBase(칸, 높이)`(유한 값만)·`BaseHeightAt(칸)`(P1), `Reset`·`BeginRefill`은 바탕으로, `Raise` = `max(지금, 바탕 + h)`·h = 0은 바꾸지 않음(P2), `RaisedLevels` → `Levels`(0이 아닌 높이, 오름차순)·`HasRaised` → `HasLevels`·새 `MinHeight`(P3), 가로·세로 칸 수 `ColumnCount`·`RowCount`(크기 검사용). `CombatMapTileSurface.ApplyBaseTo(표)`: 크기가 다르면 `InvalidOperationException`(P11), 모든 칸을 적으므로 안에서 `ClearBase`는 부르지 않음(표시가 `ClearBase` → `ApplyBaseTo` 순서로 부름, P1 그대로).
- b.2 `FieldGridMapper`: `CellBaseToLocal`·`CellBaseToWorld`·`CellSurfaceToLocal`, `CellSurfaceToWorld` = `LocalToWorld(CellSurfaceToLocal)`(P4). `SurfaceHeightOf` 시작 값 = 첫 칸 높이(P5). 바탕 함수 둘은 TH-d에서 처음 쓴다.
- b.3·b.4 `FieldOutlineRenderer`·`FieldGridDecal`: 바뀐 이름, 데칼은 `MinHeight`도 기억해 위·아래 하나라도 바뀌면 다시 켬, `Show` 끝 선택 인자 `surfaceBottom` — 상자 길이 = depth + 위 − 아래, 윗면 자리는 그대로(피벗이 길이의 절반이라 길이만 늘면 아래로 늘어남)(P6). 테두리 높이별 나누기·경로 구간은 음수 높이도 그대로 받는다(코드 무변경, 문서만).
- b.5 `FieldView.TryRayToSurface`: 높은 평면(내림차순) → 바닥 평면(닿은 칸이 0이거나 격자 밖일 때) → 들어간 평면(얕은 것부터) → 바닥 평면(P7). 높이 평면 판단은 `TryRayToLevel` 하나. 높은 칸만 있는 판은 옛 순서와 같은 점이다(②가 높은 칸 점을 거르면 마지막 바닥 평면이 같은 점을 돌려줌).
- b.6 `FieldTerrainPresenter`: 문서만(다시 채우기는 바탕에서, 실패 때 바탕으로).
- 계획과 다른 점 없음.
- 확인 Ub(사용자): 컴파일, 고지대 스테이지(보스·Test 고지대 칸)에서 유닛·강조·포인터·데칼·걸음이 지금과 같음.

### 8.3 TH-c 코드 완료 (2026-10-28, 사용자 "TH-c 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음 — 타일 높이가 화면에 나옴)

- c.1 `CombatSceneDisplay`: 생성자 끝 인자 `CombatMapTileSurface tileSurface`(자리는 늘 넘기고 null 허용, P10), 필드 `_tileSurface`. `Install`에서 `_field.Build` 직후 `Surface.ClearBase()` → `_tileSurface?.ApplyBaseTo(Surface)`(P1). 크기가 다르면 `ApplyBaseTo` 예외가 Install의 실패 정리로 간다(P11). 칸 높이는 같은 Install의 `RefreshCore` → `SyncSurface`에서 바탕 + 지형으로 바뀌고, 바뀌면 `RepaintSurface`.
- c.2 `CombatSceneDisplayFactory`: 생성 줄 끝에 `backdrop?.TileSurface`(연결 줄, 민형님 공유는 TH-e). 표시 생성자를 부르는 곳은 이 한 줄뿐이다(시험 없음).
- 계획과 다른 점 없음. 같은 두 파일에 이펙트 창구(EC)·걸음 모양(ST) 등 커밋 전 변경이 섞여 있어 커밋 때 나눈다.
- 에셋: 임시 세트 `CombatMapTileSet_Temp`는 사용자가 이미 새 뜻으로 고쳐 두었다(Temp1 = 0, Temp2 = −0.2). Test 체크무늬의 Temp2 칸이 바닥면보다 0.2 들어간 칸이 된다.
- TH-d 전이라 지형 외형·오브젝트·가시밭·환경 폭발은 아직 바닥면(0)에 놓인다. 들어간 칸 위의 그것들은 타일 윗면보다 떠 보인다(Uc 확인 때 알려진 차이).
- 확인 Uc(사용자): 설계 §7 1·2·3(타일 +·− 칸 위 유닛·강조·포인터·데칼·걸음), 높이 0 지형(설치형) 칸이 타일 높이를 따름, 타일 높이가 모두 0이거나 타일 방식이 아닌 스테이지는 지금과 같음.

### 8.4 TH-d 코드 완료 (2026-10-28, 사용자 "TH-d 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- d.1 `FieldTerrainPresenter.LocalPosition`: `CellCenterToLocal` → `CellBaseToLocal`(P13). 표시(`Apply`)·토네이도 사건 시점 표시(`ShowAt`)·약한 지반 붕괴 교체(`ShowChanged` → `TryReplace` → `Apply`)가 모두 이 한 곳을 지나므로 셋 다 타일 윗면에 놓인다. 토네이도 반 칸 보정은 그대로.
- d.2 표면(P13):
  - `FieldObjectPresenter.LocalPosition` → `CellSurfaceToLocal`. 접촉 몸(`BodyOf`)은 `Flatten`이라 높이와 무관(127줄 외형 없는 점 몸도 그대로).
  - `FieldThornPresenter.Placement` → `CellSurfaceToWorld`. 접촉 몸은 `CombatBodyPlacement.FromWorldBox`가 `Flatten`해 높이와 무관.
  - `CombatEnvironmentEffectPresenter` 폭발 자리 → `CellSurfaceToLocal`.
- 다시 놓는 때: 오브젝트·지형은 Render마다, 가시밭은 Sync마다 자리를 다시 넣는다(`transform.position != position`이면). 표면 높이는 같은 `RefreshCore` 맨 앞 `SyncSurface`가 먼저 채우므로 지형 높이가 바뀐 결과 끝에 함께 따라간다.
- 레일·기차는 바닥면 그대로(P13). 이펙트 큐 `EventCell`(EC-b `CombatMomentCuePlayback`)은 이미 `CellSurfaceToWorld`라 고칠 것이 없다.
- 계획과 다른 점 없음. 주의: 이펙트 창구 확장 EC-c가 `CombatEnvironmentEffectPresenter`(환경 폭발 창구)를 지울 예정이다. 그때 폭발 자리는 `EventCell`(칸 표면)로 가므로 이 한 줄은 함께 사라져도 높이 규칙은 같다.
- 확인 Ud(사용자): 설계 §7 3(고지대·낭떠러지·약한 지반 외형이 타일 윗면에, 들어간 칸 포함)·5(지뢰 등 오브젝트·가시가 표면에), 약한 지반 붕괴로 바뀐 낭떠러지도 타일 윗면.

### 8.5 격자·호버·포인터는 들어간 칸을 바닥면으로 (2026-10-28, 사용자 "그리드 및 호버링의 경우는 아래로 들어간 높이 신경 쓰지 말고 최상단 높이만" → 범위 질문에 "격자·호버·포인터만", 컴파일 미확인, 새 시험 없음)

- 설계 TH4 개정, 계획 P6·P7 되돌림. 다른 강조 층(이동·공격 범위 테두리·채우기·경로선)과 유닛·지형·오브젝트·가시밭·폭발은 그대로 들어간 높이를 따른다.
- 규칙 한 곳: `FieldSurfaceHeights.OverlayHeightAt(칸) = max(0, 칸 높이)`, 위치 `FieldGridMapper.CellOverlayToWorld`.
- 격자 데칼: `ShowDecal`·`FieldGridDecal.Show`에서 아래 높이(`surfaceBottom`)와 `_decalBottom`을 걷음 → TH-b 전과 같은 상자. `MinHeight`는 읽는 곳이 없어져 삭제. 기본 깊이 1(절반 0.5)이라 Temp2(−0.2) 타일 윗면에는 격자가 그대로 찍힌다.
- 호버: `FieldOutlineRenderer`의 테두리 높이 `HeightOf(슬롯, 표, 칸)` — 호버 슬롯만 `OverlayHeightAt`. 채우기 `ApplyFill(…, overlay)` — 호버 채우기 칸만 `CellOverlayToWorld`.
- 포인터: `FieldView.TryRayToSurface` = 솟은 평면(내림차순, 칸 표시 높이가 그 평면과 같을 때) → 바닥 평면. TH-b의 조건부 바닥·들어간 평면 단계와 `TryRayToLevel`은 걷음. 커서 스냅 `CombatPointerMapper.TryCellCenterToScreen`도 `CellOverlayToWorld`(커서를 옮긴 자리가 같은 칸으로 다시 잡히게).
- 확인(사용자): Test의 Temp2 칸에서 호버 테두리·채우기가 바닥면 높이에, 포인터가 그 칸을 바로 집고 커서 스냅도 같은 칸, 이동·공격 범위 테두리는 들어간 높이, 고지대 칸은 호버·포인터가 윗면 그대로.

### 8.6 TH-e 문서·공유 (2026-10-28, 사용자 "TH-e 진행", 코드 변경 없음, 공유 모두 미전달)

- 민형님 Factory 공유 [§37](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md): 표시 생성 줄 끝 `backdrop?.TileSurface`(c.2)와 타일 맞춤 TA-b의 `CreateBackdrop` 셋째 인자(필드 설정)를 한 절에, 배경 방식 열거·민형님 맵 SO 줄이 `MapDefinition` 방식에서만 읽힌다는 것, 타일 방식에서 하는 일. 맞춤 계획 TA-c의 공유 줄은 이것으로 했음(맞춤 계획 §4에 표시).
- 호진님(P14): 새 메모 대신 사본 목록이 이미 있는 [SO 모드 3D 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md)에 §5를 더했다. 게임에서 바뀐 것, 어긋나는 사본 다섯(높이 표·유닛 자리·지형/오브젝트 자리·배경·덧그리기 높이), 이름 읽기에 빠진 칸(`mode`·`mapTiles`)·패널 안내 문구가 게임과 다름, 권장 순서. 도구는 고치지 않았다. 대조로 찾은 것: 타일이 없어도 고지대 칸 위 오브젝트·가시밭은 TH-d로 블록 윗면에 올라가서 도구 3D와 다르다.
- 설계 §9 구현 메모(P5, TH4 개정, `ApplyBaseTo` 자리, §7 4번 문장의 예외 — 고지대 칸 위 오브젝트·가시밭, 환경 폭발이 EC-c로 큐로 옮겨진 것), 설계 상태 줄.
- 타일 맞춤 계획 §6.8 "열린 것" → "정함(화면 표시) → 이 계획".
- 폴더 배치 안내: 갱신 줄, 트리·`Display/Map/` 줄에 `StageViewPropPresenter`·`StageMapTiles`·`CombatMapTileAlignment`·`CombatMapTileSurface`, §2에 "칸 위에 놓는 높이는 Mapper 함수 넷 중 하나" 줄.
- 묶음 끝 검토: §8.7.

### 8.7 묶음 끝 검토 (2026-10-28, 새 에이전트 하나, 7기준, 코드 변경 없음 — 반영은 사용자 결정 대기)

- 점수: SSOT 8.75 · SOLID 9.75 · FW 9.75 · 독립성 7.25 · 캡슐화 9.5 · C#/Unity 9.75 · If/Tick 10 = **약 9.25**(설계 예상 8.6과 기준이 달라 비교하지 않음). 권장을 모두 반영하면 약 9.9.
- **H1(높음, 독립성, 코드 대조로 확인):** 이동 경로선은 높이가 같은 연속 칸마다 한 줄로 나뉘는데(`FieldOutlineRenderer.ApplyPath`, HG-4a P12), GlobalSystem 선 핸들은 점이 2개 이상일 때만 렌더러를 켠다(`LineHandle` `MeshRenderer.enabled = count >= 2`). Test 체크무늬(타일 크기 2 × 배경 배율 0.5 = 칸 하나, Temp1 0 / Temp2 −0.1)에서는 가로·세로 이웃 칸이 늘 높이가 달라 구간이 모두 1칸 → **가로·세로 이동 경로선이 그려지지 않는다**(대각선 구간만). 권장: 구간마다 다음 구간의 첫 칸을 끝에 붙여 그림(그 구간 높이로 칸 중심까지 이어지고, 다음 구간은 자기 높이에서 시작). `DrawPointLineData`는 높이 하나만 받으므로(GlobalSystem 무변경) 이 방법.
- **M1(중간, SSOT, 사용자 결정 필요):** 격자 데칼은 상자(바닥면 −depth/2 = −0.5까지) 안의 실제 표면에 찍혀 Temp2 칸 격자선은 −0.1에, 호버 테두리·채우기·포인터는 0에 놓인다. 카메라가 기울어 있어 그만큼 어긋나 보일 수 있다. (a) 데칼 아래 끝을 바닥면 바로 아래로 → 들어간 타일에는 격자가 없음 / (b) 그대로(설계 TH4 개정 하위 줄대로, 격자는 실제 바닥 표면). 지금 기본 에셋 `showGridDecal: 0`이라 데칼을 켜야 드러난다.
- 낮음(9):
  - L1 칸 가운데 식이 `CombatMapTileSurface.Build`와 `FieldGridMapper`에 따로 → Mapper 정적 함수 하나.
  - L2 "들어간 칸은 덧그리기에서 0" 규칙이 `OverlayHeightAt`·`MaxHeight`의 `> 0`·포인터 반복의 `> 0` 세 곳 → `MaxHeight`를 표시 높이 최댓값으로 정의, 포인터는 그 규칙의 목록을 씀.
  - L3 렌더러 덧그리기 판정이 테두리 `SlotKind.Hover`·채우기 `i == HoverFill` 두 기준 → 판정 함수 하나.
  - **L4 공유 문서 절 번호 `## 36` 두 번(타일·EC) → 반영함:** EC 계획(Ed.3)이 먼저 §36을 예정했으므로 이 묶음 절을 §37로 바꿔 문서 끝으로 옮기고, 이 계획·설계 §9·맞춤 계획 §4의 링크를 §37로 고쳤다.
  - L5 높이별 나누기 때문에 체크무늬에서는 이동·공격 범위 테두리가 칸마다 따로 그려진다(외곽선이 없어짐). HG-4a P12 결정의 결과 — 확인만.
  - L6 이동 고스트는 포인터 점(표시 높이)을 따라 들어간 칸에서 설 자리보다 0.1 뜬다. 배치 고스트는 실제 높이. 권장: 포인터가 칸 안이면 그 칸 표면 높이로, 아니면 설계 TH4 "고스트는 높이 값 그대로" 문장을 고침.
  - L7 토네이도 외형은 2×2인데 바탕은 앵커 칸 하나만 봄 → 체크무늬에서는 늘 0.1 뜨거나 묻힘. 권장: 덮는 칸들(판 안) 바탕 중 큰 값.
  - L8 표시가 `_field.Mapper.Surface`에 바탕을 직접 넣고 `FieldView.Build`의 재사용 규칙을 주석으로 앎 → `FieldView`에 바탕 입구 하나.
  - L9 부르는 곳 없는 `FieldGridMapper.CellBaseToWorld`·`CombatMapTileSurface.HeightAt` → 지우거나 남길 이유를 주석에.
  - L10 `CombatMapTilesData.TryTileAt`이 false일 때도 out 값을 남김 → false면 0.
- 감점 밖 관찰: 공격 목표점(칸 지정 공격·곡사·막힌 궤적 끝)은 바닥면 0이라 솟은 타일 칸에서는 투사체가 땅속에, 들어간 칸에서는 공중에 떨어진다(고지대 HG-4b로 미룬 범위와 같음, 타일로 해당 칸이 많아짐).
- 에셋 기록 고침: 임시 세트 Temp2는 지금 −0.1이다(§8.3의 −0.2는 그때 값, 사용자가 다시 바꿈). Test `anchorTile`은 (4, 4).
- 검토가 맞다고 확인한 것: 재설치·실패 때 바탕 다시 넣기와 `Version` → `RepaintSurface`, `ApplyBaseTo` 크기 검사가 쓰기 전, 호버 손잡이 높이가 늘 0이나 `Levels` 안의 양수(`IndexOf` −1 없음), 포인터·커서 스냅·호버 높이 규칙 일치, 칸 → 타일 대응(정수배 강제·90° 회전·축 순서), 음수 높이를 받는 곳(`Levels`·`MaxHeight`·`CombatStepHeight.Arc`·`SurfaceHeightOf`·넉백 화살표·테두리·배치 고스트), 지형 외형 자리 한 곳.

### 8.8 검토 권장 반영 (2026-10-28, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 에셋 변경 없음)

- **H1 경로선:** `FieldOutlineRenderer.ApplyPath`가 구간마다 다음 구간의 첫 칸까지 그 구간 높이로 이어 그린다. 한 칸짜리 구간도 점이 둘이라 선이 보이고, 높이가 바뀌는 칸에서 다음 구간이 자기 높이로 이어진다. 평지(높이 없음)는 지금과 같다.
- **L1 칸 가운데:** 새 정적 `FieldGridMapper.CenterLocal(모서리, 가로, 세로, 칸 폭, 칸 길이)`를 `CellCenterToLocal`과 `CombatMapTileSurface.Build`가 함께 부른다(식 순서가 같아 값은 같음).
- **L2 덧그리기 규칙 한 곳:** `FieldSurfaceHeights.OverlayOf`(private, max(0, 높이))를 `OverlayHeightAt`과 새 목록 `OverlayLevels`(바닥이 아닌 덧그리기 높이 = 솟은 칸 높이, 오름차순)가 쓰고, `MaxHeight`는 `OverlayLevels`의 마지막 값, 포인터(`FieldView.TryRayToSurface`)는 `OverlayLevels`를 돈다. `> 0` 사본 둘을 걷었다.
- **L3 덧그리기 판정 하나:** 렌더러의 `IsOverlay(SlotKind)`를 테두리 높이(`HeightOf`)와 채우기 자리(`ApplyFill`)가 함께 쓴다. 채우기 칸 `FillSlot`이 종류(`SlotKind` 배치 구역·층·호버)를 갖고, `ApplyFill`의 `overlay` 인자는 걷었다.
- **L6 고스트 높이:** 새 `FieldGridMapper.OnSurface(월드 점)`(칸 안이면 가로 자리는 두고 높이만 그 칸 표면, 격자 밖은 그대로)와 `CombatPointerMapper.TryRayToStandingPoint`. 이동 고스트(`UpdateMovementPointer`)와 포인터를 따라가는 배치 고스트(`MoveGhostToPointer`)가 이것을 쓴다. 들어간 칸에서는 고스트가 그 칸 타일 윗면에 선다(칸에 붙은 배치 고스트 `CellSurfaceToWorld`와 같음). 칸 고르기·경로/공격 Drag는 그대로 `TryRayToSurface`.
- **L7 토네이도:** 지형 외형 자리 `LocalPosition`이 토네이도면 덮는 2×2 칸(앵커와 +X·+Z, 판 안) 바탕 중 큰 값에 놓는다. 들어 올림 대상(`TornadoLiftTarget`)은 외형 위치를 읽어 따라온다.
- **L8 바탕 입구:** 새 `FieldView.SetGroundBase(CombatMapTileSurface)`(구성 전이면 예외, `ClearBase` → `ApplyBaseTo`). 표시 `Install`은 이 한 줄을 부르고 높이 표를 직접 고치지 않는다.
- **L9:** 부르는 곳 없는 `FieldGridMapper.CellBaseToWorld`·`CombatMapTileSurface.HeightAt`을 지웠다.
- **L10:** `CombatMapTilesData.TryTileAt`은 false면 칸 번호 0.
- **M1 = b(그대로):** 격자 데칼은 상자 안의 실제 바닥 표면(들어간 타일 윗면)에 찍고, 호버·포인터는 바닥면이다. 설계 TH4 개정 하위 줄 그대로.
- **L5 = 그대로:** 높이별 나누기로 체크무늬에서 범위 테두리가 칸마다 따로 그려지는 것은 HG-4a P12 결정의 결과로 둔다.
- 폴더 배치 안내 §2 높이 줄에 `OnSurface`(고스트)를 더했다. 공유 메모는 바꿀 것 없음(Factory 줄 무변경, 도구 사본 목록과 무관).
- 확인(사용자, Test 체크무늬): ① 가로·세로 이동 경로선이 이어져 보이고 높이가 바뀌는 칸에서 계단처럼 이어짐 ② Temp2 칸에서 이동 고스트·포인터를 따라가는 배치 고스트가 타일 윗면에 섬(칸에 붙었을 때와 같은 높이) ③ 토네이도가 덮는 칸 중 높은 타일 윗면에 섬 ④ 호버·포인터·커서 스냅·데칼은 반영 전과 같음 ⑤ 타일 없는 스테이지(고지대 포함)는 경로선·고스트가 지금과 같음.

### 8.9 TH4 개정 2: GridOutline 전부와 포인터는 타일 높이를 보지 않음 (2026-10-28, 사용자 "그리드 및 호버링 그려지는 위치는 CombatMapTileSet의 SurfaceHeight 영향 받으면 안 된다니까", 컴파일 미확인, 새 시험 없음)

- 바로잡은 해석: §8.5에서 "격자"를 바닥 데칼 하나로 읽었으나, 사용자 뜻은 GridOutline이 그리는 것 전부다. 기본 에셋은 데칼이 꺼져 있어(`showGridDecal: 0`) 보이는 격자는 칸 테두리·채우기이고, 이것들이 타일 높이(체크무늬 0 / −0.1)를 따라 칸마다 쪼개져 있었다(§8.7 L5와 같은 원인).
- `FieldSurfaceHeights`: 지형 높이 배열 `_terrain`을 따로 들고 칸 높이 = 바탕 + 지형(`Raise`·`Reset`·`BeginRefill`이 함께 맞춤). `OverlayHeightAt` = 지형 높이, `OverlayLevels`(0이 아닌 지형 높이, 오름차순)·`HasOverlayLevels`·`MaxHeight`(그 최댓값). 빼기로 구하지 않아 높이별 손잡이 묶음이 실수 오차로 갈리지 않는다. `max(0, h)` 규칙 `OverlayOf`, 칸 높이 목록 `Levels`·`HasLevels`(읽는 곳 없어짐)는 삭제.
- `FieldOutlineRenderer`: 모든 테두리(`ApplyArea`)·경로선(`ApplyPath`)·채우기(`ApplyFill` = `CellOverlayToWorld`)가 격자 높이. §8.8 L3의 `IsOverlay`·`HeightOf`·채우기 칸 종류는 고를 것이 없어 걷음(채우기 칸은 옛 꼴로). 경로선 이어 그리기(H1)는 남김(한 칸짜리 고지대 구간용).
- 포인터(`FieldView.TryRayToSurface`)·커서 스냅은 코드 그대로(격자 높이 목록·`CellOverlayToWorld`), 주석만. 데칼 상자 위 끝 = 지형 최고 높이.
- 칸 높이를 그대로 쓰는 것: 유닛·걸음, 고스트(`OnSurface`), 지형 외형, 오브젝트·가시밭·이펙트 큐, 넉백 화살표(유닛 자리 높이).
- 남는 것(설계 TH4 개정 2): ① 데칼을 켜면 바닥면에서 depth/2 안의 타일 윗면에 격자선이 찍힌다(데칼은 실제 표면에 투영) ② 바닥 타일 위 고지대 칸의 테두리는 지형 높이(0.777)라 블록 윗면과 타일 높이만큼 어긋난다.
- 확인(사용자, Test): 배치 구역·이동/공격 범위 테두리·채우기·경로선·호버가 체크무늬와 상관없이 바닥면에 한 덩어리로 그려짐, 포인터·커서 스냅이 같은 칸, 유닛·고스트는 타일 윗면, Temp 높이를 +로 바꿔도 격자는 바닥면 그대로, 고지대 스테이지는 지금과 같음.
