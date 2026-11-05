# 겹친 강조 2단계 — 둥근 안쪽 모양 · 위 층 테두리 빼기 Task 계획 (2026-10-29)

설계: [overlap-rounded-inset-design](../specs/2026-10-29-combat-overlap-rounded-inset-design.md)(RC1~RC5). 결정: Q-RC1~Q-RC7 모두 A(10-29 사용자 "모두 A").
상태: RC-a 코드·에셋 완료(Ua 대기, §7.1·§7.2), RC-b(3판, 설계 §8) 코드·에셋 완료(§7.3), RC-b(4판, 설계 §9 — 겹친 칸을 덩어리로) 코드·에셋 완료(컴파일 미확인, Ub 대기, §3.1·§7.4). RC-c 문서·공유 완료, 묶음 끝 검토 약 8.4(§7.5) → 권장대로 반영 + 이동 경로·화살촉 덧칠 막기(§7.6, 컴파일 미확인, Ub 대기). 계획에서 정한 것 TP-10~TP-21(§5)은 확인 대상입니다.

## 0. 기준

- 시험 코드는 쓰지 않고 MSBuild도 돌리지 않습니다. 보고는 "컴파일 미확인"입니다. 셰이더는 Unity가 컴파일하므로 Phase마다 사용자 Unity 화면 확인이 완료 증거입니다.
- 기존 시험은 컴파일이 깨지는 곳만 맞춥니다. 지금 `FieldCellFillStack`·`FieldCellFill`을 직접 부르는 시험은 없습니다(`Assets/Tests` 대량 삭제 뒤).
- GlobalSystem(ColorSystem 키·MeshSystem·AreaOutline)은 RC-a에서 바꾸지 않습니다. RC-b(3판)는 사용자 허락(Q-RC8)으로 테두리 메시에 선택 값 둘을 더합니다(기본값이면 같음).
- 착수 조건: 1단계(겹침 축소·호버 이동 범위 + 검토 반영)가 같은 파일(`FieldCellFillStack`·`FieldOutlineRenderer`·`StageFieldViewDataSO`)을 고친 채 커밋 전입니다. 그 묶음을 먼저 커밋하는 것을 권합니다. 그 위에서 이어 가도 됩니다(커밋 때 hunk가 섞임).

## 1. Phase

| Phase | 내용 | 끝나면 |
|---|---|---|
| RC-a | 셰이더·재질·값, 깊이 1 이상 둥근 판(깊이별 재질 사본) | 멈추고 Ua 확인 |
| RC-b | (3판) 둥근 덩어리 테두리, 겹친 칸 테두리를 안쪽 판 둘레에, 채우기 알파 곱·아래 채움 비우기 | 멈추고 Ub 확인 |
| RC-c | 문서·공유·묶음 끝 검토 | 검토 결과 보고 |

## 2. RC-a 둥근 판

| Task | 내용 | 파일 |
|---|---|---|
| Ta.1 | 새 셰이더 `ProjectL/Combat/CellFill`(+ `.meta`, 새 폴더 `.meta`). 바탕은 AreaOutline과 같은 투명 큐·`Cull Off`·`ZWrite Off`·같은 섞기. 값 `_BaseColor`·`_CellSize`·`_CornerRadius`·`_EdgeWidth`·`_EdgeAlpha`(기본 0). 반지름·선 두께가 둘 다 0이면 모양을 계산하지 않고 판 전체를 `_BaseColor`로 칠함(TP-1). 아니면 `(uv − 0.5) × _CellSize`에서 둥근 사각형 거리 값 → 안쪽 채움 알파·안쪽 띠 가장자리 알파·바깥 0, `fwidth`로 가장자리 부드럽게 | `Prefabs/Combat/Shaders/CombatCellFill.shader` |
| Ta.2 | 새 재질 `CellFill.mat`(+ `.meta`), 셰이더 기본값 그대로(반지름·선 0) | `Prefabs/Combat/Materials/CellFill.mat` |
| Ta.3 | View DB "Cell Outline" 묶음에 `overlapCornerRadius`(기본 0.35)·`overlapEdgeAlpha`(기본 1), 읽기 속성 둘, 범위 상수 한 쌍(0~1, TP-2), `ValidateGridOutline`에 범위 검사와 "채우기 재질이 있으면 셰이더 속성 넷이 있어야"(TP-3) | `StageFieldViewDataSO.cs` |
| Ta.4 | 기본 에셋: `highlightFillMaterial` → `CellFill.mat`, 새 칸 둘 | `StageFieldView_Default.asset` |
| Ta.5 | `FieldCellFill`에서 점선 끄기 한 줄 삭제(TP-7), 주석 | `FieldCellFill.cs` |
| Ta.6 | 채우기 부품: 깊이 0은 원본 재질 그대로, 깊이 1 이상은 처음 만들 때 재질 사본 하나(깊이당, 종류끼리 공유)에 `_CellSize`(칸 × 비율^깊이)·`_CornerRadius`(View DB)·`_EdgeWidth`(`outlineThickness`)·`_EdgeAlpha`(View DB)를 넣고 그 깊이 채우기가 씀. `Release`가 깊이 1 이상 객체와 함께 사본을 지움(TP-1). `Uses`에 반지름·가장자리 알파 비교(TP-4) | `FieldCellFillStack.cs` |

- 반지름 = 판 짧은 반변 × `overlapCornerRadius`. 그래서 셰이더에는 사본마다 월드 반지름을 넣습니다(깊이마다 판이 작아지면 반지름도 같은 비율로 작아짐).
- 렌더러별 색은 지금처럼 ColorSystem 속성 블록 `_BaseColor`만 넣습니다. 사본의 값은 만들 때 한 번만 씁니다(매 프레임 쓰기 없음).

Ua(Unity 확인):

1. 컴파일·셰이더 오류 없음(분홍 판 없음).
2. 겹치지 않은 칸의 채우기가 지금과 같음(색·진하기·칸 끝까지 채움).
3. 겹친 칸의 위 판이 모서리 둥근 사각형이고, 안쪽 가장자리에 같은 색 실선이 보임.
4. 세 겹이면 더 안쪽에 더 작은 둥근 판(모양 비율이 같음).
5. `overlapCornerRadius` 0 → 위 판이 네모(선은 있음), `overlapEdgeAlpha` 0 → 선 없음.
6. 칸 간격을 1×0.8로 바꿔도 모서리가 찌그러지지 않고 둥긂(확인 뒤 되돌림).
7. 칸 테두리는 지금처럼 칸 경계에 그대로(테두리 빼기는 RC-b).

## 3. RC-b (3판) 둥근 덩어리 테두리 · 겹친 칸 테두리를 안쪽 판 둘레에

옛 RC-b(겹친 칸에서 위 층 테두리 빼기만)는 설계 §8(RC6~RC10, 10-29 사용자 결정)이 대신합니다.

| Task | 내용 | 파일 |
|---|---|---|
| Tb.1 | (GlobalSystem, 사용자 허락 Q-RC8) 테두리 메시 데이터 끝에 선택 인자 `cornerRadius`·`cellLoopScale`(둘 다 기본 0). 메시 생성: `cellLoopScale` > 0이면 칸마다 고리 하나, 아니면 지금 덩어리 길 찾기. 고리 점을 월드 점으로 만든 뒤 꺾인 점마다 사분원 이음(반지름은 앞뒤 변 반으로 자름, 띠 안쪽은 원 중심 넘지 않음), 둘 다 0이면 지금 식 그대로 | `MeshGeometryData.cs`·`MeshHightLevelBuilder.cs`·`AreaOutlineData.cs` |
| Tb.2 | View DB: `outlineCornerRadius`(0~1, 기본 0)·읽기·검사, `overlapEdgeAlpha`·읽기·검사 삭제, 툴팁. 기본 에셋 줄 하나 더하고 하나 지움 | `StageFieldViewDataSO.cs`·`StageFieldView_Default.asset` |
| Tb.3 | 셰이더: 가장자리 값 둘 삭제, 구멍 값 `_HoleSize`·`_HoleRadius`. 재질 기본값 맞춤 | `CombatCellFill.shader`·`CellFill.mat` |
| Tb.4 | 채우기 알파 = 색 알파 × `fillAlpha` | `FieldHighlightPalette.cs` |
| Tb.5 | 채우기 부품: 깊이마다 판 둘(구멍 없음·구멍 있음, 구멍 = 다음 깊이 판 모양), 사본 재질 깊이마다 둘까지, 칸을 쌓을 때 위에 더 있으면 구멍 판, 판 모양 식 하나(`ShapeOf`), 테두리용 묻기(층의 깊이별 칸·호버 테두리 깊이·판 모양), 가장자리 값 삭제 | `FieldCellFillStack.cs` |
| Tb.6 | 렌더러: 덩어리 테두리 반지름(칸 짧은 반변 × `outlineCornerRadius`), `Refresh`에서 채우기를 먼저, 층·호버 슬롯은 깊이 1 이상 칸을 덩어리에서 빼고 깊이마다 안쪽 고리 핸들에 그림 | `FieldOutlineRenderer.cs` |
| Tb.7 | 머리 주석(렌더러·부품·채우기 객체) | 같은 파일들 |

계획에서 정한 것(확인 대상, §5 표에 더함): TP-10 사분원 하나 = 직선 여섯 조각, TP-11 `cellLoopScale` 0 = 덩어리(구조체 기본값이 지금 동작), TP-12 구멍 판·사본은 처음 필요할 때 만듦, TP-13 호버가 합쳐진 칸의 호버 테두리 = 그 칸 맨 위 채우기 깊이, TP-14 `outlineCornerRadius`는 렌더러를 만들 때 읽음(두께처럼 `CanReuse`가 비교하지 않음), TP-15 레일·배치 구역 테두리도 둥글어짐.

Ub(Unity 확인):

1. 컴파일·셰이더 오류 없음. `outlineCornerRadius` 0이면 겹치지 않은 칸 테두리·채우기가 지금과 같음.
2. `outlineCornerRadius`를 올리면 덩어리 테두리의 바깥·안쪽 모서리가 둥글고 점선이 끊기지 않고 흐름. 1이면 한 칸짜리 덩어리가 원.
3. 아군 호버(이동 범위 위 공격 범위): 겹친 칸은 공격 범위 판이 안쪽 둥근 판, 그 둘레에 공격 범위 점선 테두리가 딱 붙음. 판 안은 공격 범위 색만(이동 범위 색이 섞이지 않음).
4. 공격 모드 대상 칸·이동 범위 칸 위 호버: 호버 테두리가 안쪽 판 둘레.
5. 세 겹: 더 작은 판과 그 둘레 테두리, 각 판 색이 섞이지 않음.
6. 색 알파를 낮추면 그 채우기가 옅어짐(테두리도 지금처럼 옅어짐).
7. 칸 간격 1×0.8에서도 판·테두리 모서리가 맞음(확인 뒤 되돌림). CellPrefab 모드는 지금과 같음.

## 3.1 RC-b (4판) 겹친 칸을 덩어리로 · 볼록 모서리만 둥글게

설계 §9(RC11~RC13, 10-29 사용자 "권장안대로 진행" = Q-RC12~Q-RC14 A). §3의 칸별 고리·구멍 판 둘·오목 모서리 둥글게를 대신합니다.

| Task | 내용 | 파일 |
|---|---|---|
| Tc.1 | (GlobalSystem) `cellLoopScale` → `loopInset`(Vector2 월드 거리, 기본 0): 덩어리 길 찾기 뒤 꺾인 점만 남기고(`CornersOf`) 꺾인 점마다 앞뒤 변을 안쪽으로 옮긴 교점(`InsetCorners`). 칸별 고리(`CellLoopPoints`) 삭제. `RoundCorners`는 볼록(오른쪽으로 꺾임)만 사분원, 오목은 사선 식 직각 | `MeshGeometryData.cs`·`MeshHightLevelBuilder.cs`·`AreaOutlineData.cs` |
| Tc.2 | 셰이더: 오브젝트 좌표 → 칸·칸 안 자리, 깊이 칸 지도·높이 지도로 같은 종류·같은 높이 이웃을 보고 덩어리 거리 값(가운데 둥근 사각형 ∪ 이음 ∪ 모서리 사분면), 깊이 0은 칸 전체, 다음 깊이 지도로 구멍. 값 `_GridCorner`·`_CellSize`·`_GridSize`·`_RegionMap`·`_RegionKind`·`_Inset`·`_CornerRadius`·`_HoleMap`·`_HoleInset`·`_HoleRadius`·`_HeightMap`. 재질 기본값 맞춤 | `CombatCellFill.shader`·`CellFill.mat` |
| Tc.3 | 채우기 부품: 판 = 종류·깊이마다 하나(칸 전체 크기 상자), 판마다 재질 사본, 깊이별 종류 지도(R8)·높이 지도(RFloat)를 쌓을 때 채우고 바뀌면 올림, 판 모양 식 `InsetOf` 추가, 판·사본·지도는 처음 필요할 때 만들고 `Release` 때 지움 | `FieldCellFillStack.cs` |
| Tc.4 | 렌더러: 겹친 칸 핸들에 `loopInset` = 부품 `InsetOf`(깊이 0은 0), 주석 | `FieldOutlineRenderer.cs` |
| Tc.5 | 툴팁 셋(볼록만 둥금·덩어리로 이어짐), 채우기 객체 주석 | `StageFieldViewDataSO.cs`·`FieldCellFill.cs` |

Ub(4판, Unity 확인):

1. 컴파일·셰이더 오류 없음. 겹치지 않을 때·`outlineCornerRadius` 0이면 지금과 같음.
2. 아군 호버(이동 범위 위 공격 범위): 공격 범위가 겹친 칸들이 한 덩어리 판(맞닿은 칸 사이 틈·선 없음), 바깥 둘레만 안쪽으로 들어가고 튀어나온 모서리만 둥글며 안으로 꺾인 모서리는 직각. 판 둘레에 공격 범위 점선 테두리가 딱 붙음.
3. 판 안은 공격 범위 색만(이동 범위 색이 섞이지 않음), 이동 범위 채움은 판 자리만 비고 나머지는 그대로.
4. 대각선으로만 닿은 겹친 칸은 따로 떨어진 판.
5. `outlineCornerRadius`를 올리면 덩어리 테두리의 튀어나온 모서리만 둥글고 안으로 꺾인 곳은 직각.
6. 세 겹·호버: 더 작은 덩어리 판과 그 둘레 테두리, 호버 테두리는 호버 칸 하나를 줄인 네모.
7. 고지대 칸이 섞인 범위: 높이 경계에서 판과 테두리가 함께 나뉨(어긋나지 않음).
8. 칸 간격 1×0.8에서도 판·테두리가 맞음(확인 뒤 되돌림).

## 3.2 RC-d (5판) 칸마다 테두리 방식

사용자(10-29) "지금 모드 그대로 두고, 모든 그리드에 테두리 4방향을 다 치는 모드로도 하나 만들어줘" → 결정 Q-RC15~Q-RC17(설계 §10). 한 Phase입니다.

- GlobalSystem(허락분, 3판·4판과 같은 파일 셋): `GridOutlineCreateData.EachCell`·`AreaOutlineData.EachCell`(끝 선택 인자, 기본 거짓 = 지금 메시), `CreateGridOutline` 칸마다 갈래(`AddEachCellOutline` → 격자선 `AddCellGridLines` 또는 칸마다 고리), 도우미 `GridPoint`·`DashRepeats`·`RunEnd`·`AddBand`.
- View DB: 새 열거 `FieldCellOutlineStyle`(+ `.meta`), 칸 `cellOutlineStyle`·`cellOutlineScale`·범위 상수·검사·툴팁 넷, 기본 에셋 두 줄(Merged·1 = 화면 같음).
- 모양 식: 새 `Display/Field/FieldCellShape.cs`(+ `.meta`)로 채우기 부품의 식(`InsetOf`·`RadiusOf`·`CornerRadiusFor`·`ScaleOf`)을 옮기고 방식·밑 층 크기를 더함. 표시기가 만들고 채우기 부품에 넘김.
- 셰이더·재질: `_JoinCells`, 모양 조건에 반지름, 재질 값 한 줄.
- 채우기 부품: 모양 값을 받음, `Build(mapper, state)`, 재질 사본에 `_JoinCells`, 호버 테두리·M1 경고는 덩어리 방식만.
- 표시기: 모양 값 하나로 고리 값을 묻고, 모든 테두리에 `eachCell`, 재사용 판정에 모양 값.
- 확인 Uc(사용자, Unity): 아래 §7.7.

## 3.3 RC-e (6판) 호버 공격 범위 크기 통일

사용자(10-29) "이동 구역 위 공격 구역은 작아져 있고 밖 공격 구역은 꽉 찬 사각형이라 이질감, 통일" → 설계 §11 RC18. 한 Phase, 파일 둘(`FieldHighlightLayers` 층 표 받침 층 칸·검사·`StacksOn`, `FieldCellFillStack` 받침 층이 없는 칸의 빈 자리). 정한 것 TP-29·TP-30.

## 4. RC-c 문서·공유·검토

- 호진님 필드 설정 표 메모(`handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md`) 새 절: 칸 둘, `highlightFillMaterial`이 `CellFill.mat`으로 바뀜, 재질 검사 조건(미전달).
- 격자 아웃라인 설계 머리말 한 줄, 폴더 배치 안내(셰이더 자리 `Prefabs/Combat/Shaders/`), 설계 §7 구현 메모.
- 민형님 공유(3판): 테두리 메시 선택 값 둘(`GridOutlineCreateData`·`AreaOutlineData`·`CreateGridOutline`), 기본값이면 같은 메시. Factory 무변경, `AreaOutline.mat`은 테두리·궤적이 계속 씀(미전달).
- 묶음 끝 검토 하나(7기준), 결과를 보고하고 반영은 사용자 결정.

## 5. 계획에서 정한 것 (확인 대상)

| 번호 | 정한 것 | 설계와 다른가 |
|---|---|---|
| TP-1 | 깊이 0은 셰이더가 모양 계산을 건너뛰어(반지름·선 0) 원본 재질을 그대로 씀. 재질 사본은 깊이 1 이상만, 그 깊이를 처음 만들 때 만들고 `Release` 때 지움 | **다름** — 설계 RC2는 깊이 0도 사본(반지름·선 0). 사본이 줄고 부품에 따로 지우는 입구가 필요 없음 |
| TP-2 | 두 새 칸의 범위 [0, 1]은 상수 한 쌍을 함께 씀(1단계 L2와 같은 꼴) | 아님 |
| TP-3 | 채우기 재질이 있으면 그 셰이더에 `_CellSize`·`_CornerRadius`·`_EdgeWidth`·`_EdgeAlpha`가 있어야 함(`Material.HasProperty`). **바뀜(10-29 사용자 "부품으로 옮김"):** 처음엔 부트 DB 검사였으나, 같은 파일의 기존 규칙(재질 속성은 그리기 부품이 구성 때 봄, 이동 경로 계획 TP-6)에 맞춰 채우기 부품 `Build`가 검사함. 옛 AreaOutline 재질이 남은 에셋은 Combat 필드 구성 때 멈춤 | 설계 RC3은 DB 검사 |
| TP-4 | 재사용 판정(`Uses`)에 반지름·가장자리 알파 비교를 더함(값을 바꾼 에셋은 표시기를 다시 만듦) | 아님 |
| TP-5 | RC-b는 쌓기·그리기를 나누지 않고 `Apply` 호출 자리만 테두리 앞으로 옮김(그리는 순서는 정렬 순서가 정해 메시 갱신 순서와 무관) | **다름** — 설계 RC4는 "쌓기 → 테두리 → 채우기" 셋으로 나눔. 결과는 같고 입구가 하나 덜 생김 |
| TP-6 | 안쪽 기억은 칸별 층 비트 배열 + 호버 안쪽 여부 하나 | 아님 |
| TP-7 | `FieldCellFill`의 점선 끄기 줄(`AreaOutlineDashEnabled`)은 새 셰이더에 없는 속성이라 지움 | 아님(설계 RC2) |
| TP-8 | `AreaOutline.mat`은 채우기에서만 안 쓰게 되고 테두리·궤적이 계속 씀(참조 끊김 아님) | 아님 |
| TP-9 | (3판에서 없어짐 — 가장자리선 삭제) 가장자리선은 판 경계 안쪽으로 `outlineThickness`만큼(칸 테두리처럼 경계에 걸치지 않음) | 아님(설계 RC1 "경계 안쪽 띠") |
| TP-10 | (3판) 둥근 꺾임 하나 = 직선 여섯 조각 | 아님 |
| TP-11 | (3판) `cellLoopScale` 0 = 덩어리(구조체 기본값이 지금 동작), 0보다 크면 칸마다 고리 | 아님 |
| TP-12 | (3판) 구멍 판·그 재질 사본은 처음 필요할 때 만들고 `Release` 때 지움(깊이 0 구멍 없는 판만 구성 때) | 아님 |
| TP-13 | (3판) 호버가 아래 색과 합쳐진 칸의 호버 테두리 = 그 칸 맨 위 채우기 깊이(보이는 판 둘레) | 아님(설계 RC8) |
| TP-14 | (3판) `outlineCornerRadius`는 렌더러를 만들 때 읽음(두께처럼 `CanReuse`가 비교하지 않음) | 아님 |
| TP-15 | (3판) 레일·배치 구역 테두리도 `outlineCornerRadius`로 둥글어짐(칸 테두리 전부) | 아님 |
| TP-16 | (4판) 테두리 메시 안쪽 값은 비율이 아니라 월드 거리 `loopInset`(x = 세로 변, y = 가로 변). 거리 식 원본은 채우기 부품 `InsetOf` 하나이고 렌더러가 묻고 넘김. TP-11을 대신함 | 아님(설계 RC12) |
| TP-17 | (4판) 판은 칸 전체 크기 상자이고 셰이더가 오브젝트 좌표(칸 루트 로컬)로 칸을 찾음. 채우기 객체가 칸 루트 바로 아래 단위 변환이라는 사실에 기댐 | 아님(설계 RC13) |
| TP-18 | (4판) 칸 지도는 깊이마다 R8(값 = 종류 번호 + 1), 판이 구멍을 위해 다음 깊이 지도까지 미리 만듦, 바뀐 깊이만 올림 | 아님 |
| TP-19 | (4판) 판·재질 사본은 종류·깊이마다 하나이고 깊이 0도 칸이 처음 쌓일 때 만듦(구성 때 만들지 않음). TP-1·TP-12를 대신함 | 아님 |
| TP-20 | (4판) 격자 높이가 다른 이웃은 같은 종류여도 판을 잇지 않음(높이 지도 RFloat 하나, 칸 테두리가 높이별 핸들로 나뉘는 것과 맞춤) | **다름** — 설계 RC13에 없던 지도 하나 |
| TP-21 | (4판) 깊이 0 층 칸과 깊이 1 이상 같은 층 칸이 맞닿으면 3판처럼 깊이마다 따로 그림(깊이 0 덩어리 테두리 + 깊이 1 줄어든 덩어리) | 아님 |
| TP-22 | (5판) 사용자 답 "테두리 크기가 1보다 작게"를 새 칸 `cellOutlineScale`(EachCell 밑 층 칸 크기 비율, [0.05, 1], 기본 1)로 둠. Merged에서는 쓰지 않고 검사는 늘 함 | **정함** — 설계 §10 RC14(답에 칸 이름은 없음) |
| TP-23 | (5판) 깊이 k 크기 비율 = 밑 층 비율 × `overlapInsetScale`^k(곱). Merged는 밑 층 비율 1이라 지금과 같음 | 아님(설계 RC15) |
| TP-24 | (5판) EachCell 깊이 0 채우기도 밑 층 모양(크기·`outlineCornerRadius`)으로 깎음. Merged 깊이 0 채우기는 지금처럼 칸 전체 네모 | 아님(설계 RC17) |
| TP-25 | (5판) EachCell에서 칸 크기 1·밑 층 반지름 > 0이면 칸마다 고리(맞닿은 곧은 변에 고리 둘이 겹쳐 진해질 수 있음). 막지 않고 툴팁에 적음 | 아님(설계 §10 한계) |
| TP-26 | (5판) 레일·아군 배치 구역·범위 층·호버 테두리가 모두 방식을 따름. 이동 경로·궤적·데칼은 그대로 | 아님 |
| TP-27 | (5판) 모양 식을 채우기 부품에서 새 `FieldCellShape`로 옮김(채우기 재질이 없는 설정도 EachCell 밑 층 모양이 필요). 재사용 판정은 방식·크기 비율 둘·반지름 비율 둘을 비교함 → `outlineCornerRadius`를 바꾼 에셋도 표시기를 다시 만듦(TP-14 바뀜) | **다름** — TP-14 |
| TP-28 | (5판) 격자선 교차점: 가로 띠가 늘여 덮고 세로 띠는 끊어 겹치지 않음(반투명 색이 두 번 섞이지 않음). 세로 점선 무늬는 이어진 줄 전체 길이 기준 | 아님(설계 RC16) |
| TP-29 | (6판) 통일 방향은 밖 공격 칸을 안쪽 크기로(안 칸을 키우지 않음). 받침 층이 비면 지금처럼 칸 전체 | **정함** — 사용자 "통일"에 방향은 없음 |
| TP-30 | (6판) 받침 층을 정하는 곳은 층 표 칸 하나, 대상은 호버 공격 범위 둘(아군·적)만. 빈 자리는 받침 층이 없는 칸의 그 층 바로 아래에 둠 | 아님(설계 RC18) |

## 6. 커밋 묶음 (사용자)

- RC-a: 새 셰이더·재질 + `.meta`(새 폴더 `Shaders/` `.meta` 포함), `StageFieldViewDataSO.cs`와 `StageFieldView_Default.asset`, `FieldCellFill.cs`·`FieldCellFillStack.cs`를 한 커밋(부품 재질 검사 때문에 코드만 들어가면 옛 재질 에셋으로 Combat 필드 구성이 멈추고, 에셋만 들어가면 새 칸이 없는 코드가 그 칸을 무시함).
- RC-b(3판): GlobalSystem 세 파일(`MeshGeometryData`·`MeshHightLevelBuilder`·`AreaOutlineData`), `StageFieldViewDataSO.cs`와 기본 에셋, 셰이더·재질, `FieldHighlightPalette`·`FieldCellFillStack`·`FieldOutlineRenderer`를 한 커밋(셰이더와 부품의 값 이름이 맞물리고, 렌더러가 새 메시 인자를 부름).
- RC-b(4판): 3판 묶음과 같은 파일(GlobalSystem 셋·셰이더·재질·`FieldCellFillStack`·`FieldOutlineRenderer`·`StageFieldViewDataSO`) + `FieldCellFill.cs`. 3판이 커밋 전이면 한 커밋으로 합칩니다(셰이더 값 이름과 부품이 맞물림).
- RC-d(5판): GlobalSystem 셋·셰이더·재질·`StageFieldViewDataSO.cs`와 기본 에셋·`FieldCellFillStack`·`FieldOutlineRenderer` + 새 `FieldCellOutlineStyle.cs`·`FieldCellShape.cs`(+ `.meta`). 3판·4판 묶음이 커밋 전이면 한 커밋으로 합칩니다(채우기 부품 생성자·`Build` 서명과 셰이더 값 이름이 맞물림).

## 7. 실행 기록

사용자 "그대로, RC-a"(10-29) — TP-1~TP-9 그대로. 1단계 반영분이 커밋 전인 트리 위에서 진행했다(같은 파일 셋에 hunk가 섞임).

### 7.1 RC-a 둥근 판 (코드·에셋 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

- Ta.1: 새 `Prefabs/Combat/Shaders/CombatCellFill.shader`(+ `.meta`, 새 폴더 `Shaders.meta`). 셰이더 이름 `ProjectL/Combat/CellFill`. 반지름·선 두께가 둘 다 0이면 `_BaseColor` 그대로(깊이 0), 아니면 `(uv − 0.5) × _CellSize`에서 둥근 사각형 거리 값 → 안쪽 채움 알파, 안쪽 `_EdgeWidth` 띠는 `_EdgeAlpha`, `fwidth`로 경계를 부드럽게. 반지름은 셰이더가 짧은 반변 안으로 한 번 더 자름. 윗면 UV는 u = X·v = Z(MeshSystem 상자 면 순서로 확인)라 `_CellSize`는 (칸 간격 너비, 길이) 그대로.
- Ta.2: 새 `Prefabs/Combat/Materials/CellFill.mat`(+ `.meta`), 반지름·선 0·가장자리 알파 1.
- Ta.3: `StageFieldViewData`에 `overlapCornerRadius`(0.35)·`overlapEdgeAlpha`(1)·읽기 둘, 범위 상수 한 쌍 `MinOverlapRatio`·`MaxOverlapRatio`(TP-2), `ValidateGridOutline`에 범위 검사 둘, `highlightFillMaterial` 툴팁 셰이더 이름. 재질 속성 검사는 처음 여기(`ValidateHighlightFillMaterial`)에 넣었다가 사용자 결정으로 부품으로 옮김(§7.2).
- Ta.4: 기본 에셋 `highlightFillMaterial` → `CellFill.mat`, 새 칸 둘. 이 칸을 가진 에셋은 이것 하나.
- Ta.5: `FieldCellFill`의 점선 끄기 줄 삭제(TP-7), 머리 주석.
- Ta.6: `FieldCellFillStack` — 깊이 0은 원본 재질, 깊이 1 이상은 `MaterialOf(depth)`가 깊이마다 사본 하나(`HideFlags.DontSave`, 종류끼리 공유)에 판 크기·월드 반지름(짧은 반변 × 비율)·선 두께(`outlineThickness`)·알파를 한 번 넣음, `Release`가 깊이 객체를 거둔 뒤 사본을 지움(TP-1), `Uses`에 반지름·알파 비교(TP-4), 비율 계산 `ScaleOf` 하나. 렌더러 머리 주석 한 구절.
- 확인 중 찾은 것(TP-3): 같은 파일의 이동 경로 검사 주석(이동 경로 화살표 계획 TP-6)은 "머티리얼 속성은 렌더 쪽 사실이라 DB 검사에서 보지 않고 그리기 부품이 구성 때 본다"고 정해 두었다(`FieldMovementPathLine.Build`·`FieldKnockbackMarkers`가 그렇게 함). TP-3은 그 규칙과 반대 자리였다 → §7.2.
- 커밋: §6 RC-a 묶음.

### 7.2 TP-3 바뀜 — 재질 속성 검사를 채우기 부품으로 (10-29 사용자 "부품으로 옮김", 컴파일 미확인)

- `StageFieldViewData`: `ValidateHighlightFillMaterial`·속성 이름 상수 넷·이름 배열 삭제, 그 자리에 "재질 속성은 채우기 부품이 구성 때 본다" 주석 한 줄.
- `FieldCellFillStack`: 셰이더 값 이름 상수 넷과 배열(검사·사본 값 넣기가 함께 씀)을 부품 안에 두고, `Build` 맨 앞에서 채우기 재질이 넷을 다 갖는지 검사(없으면 `InvalidOperationException`, 렌더러가 만든 것을 거둠). 이름 원본은 부품 하나라 DB 쪽 상수는 필요 없어짐.
- 결과: 옛 AreaOutline 재질이 남은 에셋은 부트가 아니라 Combat 필드 구성 때 멈춤(이동 경로 재질과 같은 시점).

### 7.3 RC-b(3판) 둥근 테두리 · 겹친 칸 테두리 · 구멍 판 (코드·에셋 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

사용자(10-29) "일단 그리드 라인 테두리 깎을 수 있게 만들고, Overlap의 경우 그리드 라인이 호버 메쉬 위에 딱 맞게 올라가야함" → 결정 Q-RC8 A(GlobalSystem 확장)·Q-RC9 A(판 가장자리선 없앰)·Q-RC10(알파 곱 + 겹친 칸은 아래 색 영향 없음)·Q-RC11 A(깊이 0 채우기 네모). 설계 §8.

- Tb.1(GlobalSystem, 민형님 파일 셋): `GridOutlineCreateData`에 `CornerRadius`·`CellLoopScale` 필드와 생성자 끝 선택 인자, `AreaOutlineData`에 같은 속성 둘·생성자 끝 선택 인자·`ToMeshData` 전달. `CreateGridOutline`: `CellLoopScale` > 0이면 칸마다 `CellLoopPoints` 고리(꼭짓점 순서는 길 찾기가 칸 하나에 만드는 순서와 같음), 아니면 지금 길 찾기 → `GridLoopPoints`. `AddOutlineLoop`이 월드 점을 받아 `CornerRadius` > 0이면 `RoundCorners`(곧은 변 가운데 점 빼기 → 꺾인 점마다 사분원 일곱 점, 반지름은 앞뒤 변 반으로 자름, 띠 바깥 = 반지름 + 두께 반, 안쪽 = 반지름 − 두께 반이고 반지름이 두께 반 이하면 두 변 안쪽 선의 교점), 아니면 `MiterSides`(옛 사선 식 그대로). 둘 다 0이면 옛 메시와 같은 식·순서. 새 줄은 파일처럼 CRLF.
- Tb.2: `StageFieldViewData`에 `outlineCornerRadius`(기본 0)·읽기·검사, `overlapEdgeAlpha`·읽기·검사 삭제, 범위 상수 이름 `MinCornerRadiusRatio`·`MaxCornerRadiusRatio`, 툴팁 셋. 기본 에셋 `outlineCornerRadius: 0` 더하고 `overlapEdgeAlpha` 줄 지움(화면은 값 0이라 직각 그대로 — 사용자가 올려 확인).
- Tb.3: 셰이더 `_EdgeWidth`·`_EdgeAlpha` 삭제, `_HoleSize`(0 = 없음)·`_HoleRadius`, 모양 덮개 함수 `RoundedBoxCoverage` 하나를 판·구멍이 함께 씀. 반지름·구멍이 둘 다 없으면 판 전체(옛 깊이 0과 같음). 재질 값 맞춤.
- Tb.4: `FieldHighlightPalette.ToFill` 알파 = 색 알파 × `fillAlpha`, `FieldView.fillAlpha` 툴팁.
- Tb.5: `FieldCellFillStack` — 깊이마다 판 둘(`Plain`·`Holed`, 판 = 채우기 객체·칸·그린 값), 칸을 쌓을 때(`Stack`) 위에 더 있으면 구멍 판, 재질 사본은 구멍 없는 깊이 1 이상(`PlainMaterialOf`)과 구멍 판 깊이 0 이상(`HoledMaterialOf`, 구멍 = 다음 깊이 판 크기·반지름), 판 모양 식 `ScaleOf`·`RadiusOf`(깊이 0 반지름 0) internal, 테두리용 `DepthCountOf`·`CellsOf`·`HoverOutlineDepth`, `Uses`에서 가장자리 알파 비교 삭제. `FieldCellFill` 머리 주석 한 단어.
- Tb.6: `FieldOutlineRenderer` — 덩어리 반지름 = 칸 짧은 반변 × 비율(`Build`), `Refresh`가 채우기 `Apply`를 먼저(TP-5), 층·호버 슬롯은 `SplitInsets`로 깊이 1 이상 칸을 덩어리에서 빼고 `ApplyInsets`가 깊이마다 핸들(`Slot.Insets`, `InsetDepth`, 처음 칸이 생길 때 만듦)에 판 모양 고리로 그림, 높이별 나눔·정렬 순서·두께·점선은 그 슬롯 그대로, `ForgetDrawn`·`ReleaseHandles`가 겹친 칸 핸들까지.
- 다른 쓰는 곳: `TestRuner.cs`의 `AreaOutlineData` 생성은 끝 선택 인자라 그대로. 시험 참조 없음.
- 커밋: §6 RC-b(3판) 묶음. 공유(민형님 GlobalSystem 줄·호진님 View DB 칸)는 RC-c.

### 7.4 RC-b(4판) 겹친 칸을 덩어리로 · 볼록 모서리만 둥글게 (코드·에셋 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

사용자(10-29) 그림 + "다른 타일과 이어진 부분의 면은 그리지않는다" → "권장안대로 진행"(Q-RC12~Q-RC14 A). 설계 §9. 3판(§7.3) 코드 위에서 고쳤다(둘 다 커밋 전).

- Tc.1(GlobalSystem, 민형님 파일 셋): `GridOutlineCreateData.CellLoopScale`·`AreaOutlineData.CellLoopScale`과 생성자 인자를 `LoopInset`(Vector2, 기본 0)으로 바꿈. `CreateGridOutline`의 칸별 갈래·`CellLoopPoints` 삭제. `AddOutlineLoop`이 `LoopInset`이 있으면 `CornersOf`(곧은 변 점 빼기) → `InsetCorners`(꺾인 점 + 앞 변 안쪽 이동 + 뒤 변 안쪽 이동, 안쪽 = 위 × 진행 방향, 가로 변은 y·세로 변은 x 거리). `RoundCorners`는 `CornersOf`를 쓰고, 오른쪽으로 꺾인 점(볼록, `Cross(앞, 뒤).y > 0`)만 사분원 일곱 점, 왼쪽(오목)은 점 하나에 사선 식 직각 양쪽 점. 격자 고리는 진행 방향 오른쪽이 늘 안쪽이라 띠 '+' 쪽이 늘 바깥(주석). 둘 다 0이면 옛 메시와 같은 식. 새 줄 CRLF.
- Tc.2: 셰이더를 다시 씀 — 정점은 오브젝트 x·z만 넘김, 조각에서 칸·칸 안 자리, 종류 지도 `Load`(판 밖 0), 높이 지도로 같은 높이만 이웃, `ClusterDistance`(가운데 둥근 사각형 + 같은 종류 이웃 쪽 이음 상자 + 두 옆·대각선이 다 있는 사분면, 상자는 칸 경계를 넘어 뻗어 이음매 없음), 부드러운 경계 폭은 분기 밖에서 위치 미분으로 한 번. 깊이 0은 안쪽 거리 0이라 모양 없이 칸 전체, 자기 칸 종류가 다르면 0(경계 조각). 재질에 새 값·텍스처 칸.
- Tc.3: `FieldCellFillStack` 다시 씀 — `Kind.Depths`가 판 목록(구멍 판 나눔 삭제), `PlateOf`가 판·재질 사본(`ShapedCopy`: 격자·높이 지도·그 깊이 지도·종류·안쪽 거리·반지름·다음 깊이 지도·구멍 모양)을 처음 쓸 때 만듦, `CellMap<T>`(값 배열·올린 값·텍스처, 다를 때만 올림)로 깊이별 종류 지도(R8)와 높이 지도(RFloat, `Build` 때 만듦), `Stack`이 판 칸과 지도 값을 함께 넣음, `Apply`가 쌓은 뒤 지도를 올리고 판을 칸 전체 크기로 그림, `InsetOf` 추가, `Build`가 셰이더 값 열하나·텍스처 형식 지원·격자 크기 일치를 검사, `Release`가 판 → 사본 → 지도 순으로 지움. 종류 수가 한 바이트를 넘으면 생성자에서 예외.
- Tc.4: `FieldOutlineRenderer` — `LoopScaleOf` → `LoopInsetOf`(깊이 0은 0, 그 밖은 부품 `InsetOf`), `loopInset:` 인자, 주석 넷.
- Tc.5: `StageFieldViewDataSO` 툴팁 셋, `FieldCellFill` 주석 두 줄.
- 계획과 다른 점: 설계 RC13에 없던 높이 지도(TP-20). 칸 테두리가 높이별 핸들로 나뉘는데 판만 이어지면 높이 경계에서 둘이 어긋나서 더함.
- 남는 것(보고): 같은 층이 깊이 0 칸과 깊이 1 칸에 걸치면 둘은 따로 그려짐(TP-21, 3판과 같음). 덩어리 모양 규칙이 선(메시)·면(셰이더) 두 구현이라 한쪽만 고치면 어긋남(설계 §9 7기준 SSOT 낮음).
- 다른 쓰는 곳: `cellLoopScale`을 부르던 곳은 렌더러 하나였음. `TestRuner.cs`는 끝 선택 인자라 그대로. 시험 참조 없음.

### 7.5 RC-c 문서·공유·묶음 끝 검토 (코드 변경 없음, 공유 모두 미전달)

- 문서: 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §41(테두리 메시 선택 값 `cornerRadius`·`loopInset`), 호진님 [필드 설정 표 메모](../handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md) "겹친 칸 둥근 판·둥근 테두리" 절(칸 둘·재질 바뀜·알파·앞 절 낡은 문장 정정), 격자 아웃라인 설계 머리말 한 줄, 폴더 배치 안내(`FieldCellFillStack` 설명·전투 전용 셰이더·재질 자리 규칙), 설계 §7 구현 메모.
- 묶음 끝 검토(새 에이전트 하나, 7기준): 높음 0·중간 1·낮음 9, 약 8.4(SSOT 8.5·SOLID 7.5·FW 8.5·독립성 8.0·캡슐화 8.5·C#/Unity 8.5·If/Tick 9.0). 메시와 셰이더가 같은 모양을 내는지는 십자·2×2·링·L자·대각선·핀치 예로 맞음, 값 0이면 옛 메시와 같음, 해제 순서·다시 구성·매 Refresh 비용 문제 없음.
  - M1(조건부, 지금 층 조합으로는 안 닿음): 깊이 k 이음이 "같은 깊이·같은 종류"만 봐서 3겹 이상에서 위 덩어리가 아래 덩어리 밖으로 나갈 수 있음(이웃 두 칸의 깊이 1 종류가 다르고 깊이 2 종류가 같을 때). 고치려면 "아래 깊이까지 모두 같은 칸끼리만 이음"(지도 값 = 쌓인 종류 묶음 번호 + 렌더러 핸들도 그 묶음으로 나눔), 또는 한계로 적고 감지 때 경고 한 번.
  - L1: 호버가 아래 색과 합쳐진 칸(공격 조준의 영향 칸)의 호버 테두리는 한 칸짜리 줄인 네모라, 덩어리로 이어진 영향 판 안쪽에 떠 보임(3판에서는 판 둘레와 같았음, 설계 RC8 문장과 다름).
  - L2: 기본 에셋 `outlineCornerRadius`가 0.004(코드 기본 0, 사용자가 에디터에서 넣은 값으로 보임). 반지름 0.002 < 테두리 두께 반 0.03이라 띠 바깥 모서리만 약 0.032로 둥글고 안쪽은 꼭짓점.
  - L3: GlobalSystem 새 값의 범위 검사·문서 없음(한 축만 양수면 다른 축 음수가 그대로, 칸 반 이상이면 고리 뒤집힘).
  - L4: 셰이더 삼항식이 판 밖 칸에서도 높이 지도를 읽을 수 있음(결과는 버려짐) → `if`로, `#pragma target 3.5` 명시.
  - L5: `PlateOf`가 아래 깊이 판까지 만듦, 판 구성 실패 때 재질 사본이 `Release`까지 남음.
  - L6: 채우기 부품 `Build`를 `Release` 없이 두 번 부르는 것을 막지 않음.
  - L7: 깊이 0 반지름 식이 렌더러와 부품 두 곳, `ScaleOf`는 private이면 됨.
  - L8: 채우기 부품 책임이 많음(쌓기·판 수명·재질 값·지도·모양 식·테두리용 묻기), "채우기 먼저"·"구성 뒤" 순서는 주석으로만 지킴.
  - L9: 축소 비율이 0.95에 가까우면 깊이 0·1 테두리 띠가 겹침(툴팁 한 줄).

### 7.6 검토 권장 반영 + 이동 경로·화살촉 덧칠 막기 (컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

사용자(10-29) "권장대로 반영하는데, 색 겹침 현상 호버 타일 뿐만 아니라 경로 + 경로 끝 화살표도 포함해줘".

- **경로·화살촉 덧칠(사용자 추가):** 원인 — 경로 띠·화살촉은 불투명(깊이 씀)이고 높이가 경로 값 + 자기 층 칸 번호 × 0.002였다. 그보다 뒤 칸의 테두리(호버 테두리 포함)는 더 높아 깊이 시험을 지나, 반투명 테두리가 띠·화살촉 위에 덧칠됐다(호버 칸 안쪽 테두리 고리가 화살촉을 가로지름). 채우기는 0.0205로 이미 아래였다.
  - 고침: 경로 높이 = 경로 값 + 모든 테두리 칸 수 × 0.002(마지막 테두리 칸보다 한 단계 위, `FieldOutlineRenderer.ApplyPath`, 쓰지 않게 된 칸 번호 인자 삭제). View DB 검사 `movementPathYOffset ≥ outlineYOffset`(GridOutline, `ValidateMovementPath`)·툴팁. 기본 에셋은 둘 다 0.02라 그대로 통과.
  - 처음엔 경로 재질 사본을 투명 큐·정렬 순서 90으로 옮겼으나 되돌렸다: 그러면 깊이를 쓰지 않는 투명 물체(이동 중 반투명 유닛·배치/이동 고스트)보다 늦게 그려져, 그 위에 있어야 할 고스트·유닛 위에 띠·화살촉이 덮인다. 높이로 정하면 불투명 깊이 시험이 그대로라 그 물체들과의 앞뒤가 맞다.
  - 이동 경로 화살표 설계 P5(정렬 순서 없음)는 그대로이고, 높이 규칙만 더함. 경로는 지금보다 약 0.02~0.04 높아진다(층 수에 따라).
- **M1(문서 + 경고):** 채우기 부품 `WarnBrokenNestingOnce` — 깊이 2 이상에서 이어진(같은 종류·같은 높이) 두 칸의 바로 아래 깊이 종류가 다르면 처음 한 번 경고(`Debug.LogWarning`, 칸 좌표·깊이). 부품 머리 주석·설계 §9에 한계로 적음.
- **L1:** 호버가 아래 판과 색이 같아 합쳐졌고 그 판이 깊이 1 이상이면, 호버 테두리 칸 = 호버 칸이 든 덩어리 전부(셰이더가 잇는 규칙과 같은 상하좌우·같은 종류·같은 높이로 찾음, `HoverOutlineCells`) → 테두리가 덩어리 판 둘레에 놓임. 렌더러 `SplitInsets` 호버 갈래가 그 칸을 씀. 깊이 0과 따로 판이 있는 호버는 지금처럼 호버 칸 하나.
- **L3(GlobalSystem):** `LoopInset` 음수·NaN 축은 0으로(`Vector2.Max`), 범위(셀 반 미만)를 필드·속성 주석에.
- **L4(셰이더):** 자기 칸 종류 검사를 삼항식 대신 갈래로(판 밖 칸에서 이웃 지도를 읽지 않음), `#pragma target 3.5`.
- **L5:** `PlateOf`가 그 깊이 판만 만듦(아래 깊이는 null 자리, 순회하는 곳은 건너뜀·`CellsOf`는 빈 목록), 판 구성 실패 때 그 재질 사본을 바로 지움.
- **L6:** 채우기 부품 `Build`가 `Release` 없이 다시 불리면 예외.
- **L7:** 모서리 반지름 식 `FieldCellFillStack.CornerRadiusFor(크기, 비율)` 하나를 겹친 판(`RadiusOf`)과 렌더러 덩어리 테두리가 함께 씀, `ScaleOf` private.
- **L9:** `overlapInsetScale` 툴팁에 0.95 근처 테두리 띠 겹침 한 줄.
- 그대로: L2(에셋 `outlineCornerRadius` 0.004, 사용자 값), L8(채우기 부품 나누기, 다음 후보).
- Ub 더함: ⑨ 이동 경로를 끌 때 띠·화살촉 위에 호버·범위 테두리 점선이 덧칠되지 않음, 이동 중 반투명 유닛·도착 고스트는 띠·화살촉보다 앞에 보임. ⑩ 공격 조준에서 영향 칸이 이동 범위 등 위에 겹쳐 안쪽 판일 때, 호버 테두리가 영향 덩어리 판 둘레에 놓임.

### 7.7 RC-d(5판) 칸마다 테두리 방식 (코드·에셋 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

사용자(10-29) "지금 모드 그대로 두고, 모든 그리드에 테두리 4방향을 다 치는 모드로도 하나 만들어줘" → 답(10-29): 맞닿은 변은 한 번만, 호버나 테두리 크기가 1보다 작으면 두 번 / 겹친 칸도 칸마다, 둥근 네모는 겹친 층·밑 층 둘 다 / GlobalSystem 선택 값 더함. 계획 §3.2, 정한 것 TP-22~TP-28, 설계 §10.

- 작업 중 트리가 바뀜(10-29 22:06): 브랜치가 `6b0755be2`로 옮겨지고 스태시를 다시 꺼낸 충돌 넷(CLAUDE.md·`CombatMovementCalculator.cs`·`TraitVisualDataTable.asset`·글꼴)이 남아 있었다. 충돌 파일은 건드리지 않았다. 바꾼 파일은 모두 충돌이 없는 파일이고, 줄 끝은 그 뒤 작업 트리 그대로(CRLF) 맞췄다.
- 바꾼 것은 §3.2 목록 그대로. Merged(기본)는 모양 값이 옛 식과 같다: 깊이 0 안쪽 거리 0·판 반지름 0·테두리 반지름 = 칸의 `outlineCornerRadius`, 깊이 1 이상 = 칸 × 비율^k의 `overlapCornerRadius`, `_JoinCells` 1.
- 계획과 다른 점: 없음(이 Phase 계획은 구현과 함께 씀).
- Uc(사용자, Unity, `StageFieldView_Default`의 `cellOutlineStyle`을 EachCell로 바꾼 뒤 Combat 진입):
  1. 컴파일 오류 없음, Merged(기본)로 두면 지금 화면과 같음(덩어리 테두리·겹친 덩어리 판).
  2. EachCell·크기 1·`outlineCornerRadius` 0: 이동 범위 등 칸마다 네 변이 보이고, 맞닿은 변은 한 줄(진하게 겹치지 않음), 교차점이 비거나 두 번 칠해지지 않음, 점선이 흐름.
  3. EachCell·크기 0.9 정도: 칸마다 떨어진 네모 테두리와 같은 크기의 채우기.
  4. `outlineCornerRadius`를 올리면 밑 층 칸마다 네 모서리가 둥글고 채우기도 같은 모양(크기 1이면 맞닿은 곧은 변이 진해 보이는 것은 TP-25대로).
  5. 겹친 칸(공격 조준 영향 칸이 이동 범위 위 등): 칸마다 작은 둥근 네모 판·테두리(`overlapCornerRadius`), 아래 판은 그 자리만큼 비어 색이 섞이지 않음.
  6. 호버: 호버 칸 하나에 테두리(겹치면 안쪽 판 둘레).
  7. 레일·배치 구역 테두리도 칸마다.
  8. 고지대 칸이 섞여도 테두리가 높이별로 나뉘어 그 높이에 그려짐.

### 7.8 RC-e(6판) 호버 공격 범위 크기 통일 (코드 완료, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ue 대기)

- 바꾼 것: `FieldHighlightLayers` — 층 줄 끝 선택 칸 `stacksOn`(받침 층), 호버 공격 범위 두 줄에 그 유닛의 이동 칸 층, 표 검사 둘(앞에 있는 단일 층·둘 다 채움), 묻기 `StacksOn`, 열거 문서 한 줄. `FieldCellFillStack` — Apply가 칸을 쌓기 전에 이번에 칠해진 층 합을 모으고, `Stack`이 받침 층이 칠해져 있는데 이 칸에 없으면 그 종류 바로 아래에 빈 자리(`EmptyDepth`, 판·지도 값 없음)를 둠. 렌더러·셰이더·GlobalSystem 무변경(테두리는 채우기 깊이를 그대로 물어 따라감).
- 커밋: 3판~5판 묶음과 같은 파일(`FieldCellFillStack`) + `FieldHighlightLayers.cs`. 앞 묶음이 커밋 전이면 한 커밋.
- Ue(사용자, Unity):
  1. 아군 유닛 호버: 공격 범위가 이동 칸 위·밖 모두 같은 안쪽 크기이고, Merged에서는 이동 칸 경계를 넘어 이어진 덩어리 하나(경계에 이음매 없음), 둘레 테두리도 덩어리 하나.
  2. 적 유닛 호버도 같음(적 이동 칸 색 위).
  3. 움직일 수 없는 유닛(이동 칸 없음) 호버: 공격 범위가 칸 전체(지금과 같음).
  4. 이동 칸 채우기는 공격 판 자리만큼 비어 색이 섞이지 않음.
  5. 공격 모드·이동 모드 범위·지뢰/화약통 위험 범위는 지금과 같음.
  6. EachCell 방식에서도 공격 칸 크기가 모두 같음.
