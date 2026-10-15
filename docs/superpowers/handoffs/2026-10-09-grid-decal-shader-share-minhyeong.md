# GridDecal 셰이더 변경 공유 (민형님께)

- 작성일: 2026-10-09. 작성: 용근(View). 브랜치 `feature/Yonggeun/Stage`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련 기록: View 계획 §7.62, [필드 격자 아웃라인 모드 설계](../specs/2026-10-09-field-grid-outline-mode-design.md)

## 1. 요약

- Shader 브랜치를 병합하면서 `GridDecal.shadergraph`의 GUID가 충돌했습니다. **저희 쪽 복사본을 지우고 민형님 파일만 남겼습니다.**
- 그 뒤 전투 필드 격자에 맞추려고 **민형님 `GridDecal.shadergraph`에 Vector2 속성 두 개(`_GridOrigin`·`_MaskCenter`)를 더했습니다.** 기본값이 (0, 0)이라 값을 넣지 않으면 지금까지와 똑같이 보입니다.
- 이어서 **AreaOutline 점선이 핸들 값을 쓰도록, 그리고 점선 길이 기준으로 나뉘도록 고쳤습니다**(§6, 사용자 지시).
- 확인 부탁드리는 것은 §5에 있습니다.

## 2. GUID 충돌과 정리

| 항목 | 내용 |
|---|---|
| 원인 | 2026-10-08 `4f245ea9`에서 민형님 `GridDecal.shadergraph`를 `Assets/ProjectL/GridDecal.shadergraph`로 복사해 속성을 더했는데, `.meta`의 GUID(`c64749d84c460334cbb841d35d2c773e`)까지 그대로 복사했습니다. 병합 뒤 두 파일의 GUID가 같아져, Unity가 민형님 파일의 GUID를 로컬에서 다른 값으로 바꿔 버렸습니다 |
| 정리 | 저희 복사본(`Assets/ProjectL/GridDecal.shadergraph`, `Assets/ProjectL/Shader Graphs_GridDecal.mat`, 각 `.meta`)을 지웠습니다. 민형님 `.meta`는 원래 GUID `c64749d8…`로 되돌렸습니다. 민형님 머티리얼(`Shader Graphs_GridDecal.mat`)은 이 GUID를 그대로 참조합니다 |
| 민형님 쪽 영향 | 없습니다. 혹시 민형님 로컬에서도 Unity가 GUID를 바꿨다면, 저희 정리 커밋을 받은 뒤 `GridDecal.shadergraph.meta`의 GUID가 `c64749d8…`인지 확인해 주세요 |

## 3. 셰이더 변경 내용

파일: `Assets/ProjectL/Script/Render/Shader/Decal/GridDecal.shadergraph`(GUID 그대로)

| 변경 | 전 | 후 |
|---|---|---|
| 속성 추가 `_GridOrigin`(Vector2, 기본 0,0) | 위치 XZ ÷ `_CellSize` | (위치 XZ − `_GridOrigin`) ÷ `_CellSize`. Subtract 노드 하나를 더했습니다 |
| 속성 추가 `_MaskCenter`(Vector2, 기본 0,0) | 마스크 거리 기준점이 고정 Vector2 노드(0, 0) | 그 노드를 지우고 `_MaskCenter` 속성으로 바꿨습니다 |

- 다른 노드, 연결, 속성 값(`_GridEdgeSize` 슬라이더 범위 포함)은 바꾸지 않았습니다. 병합 전 민형님 원본과 노드 단위로 비교해 확인했습니다.
- 옛 고정값이 (0, 0)이었으므로 새 속성 기본값 그대로면 결과가 같습니다. 민형님 테스트 씬(`JMHTest`)도 그대로일 것입니다.
- `Shader Graphs_GridDecal.mat`의 `_RadiusMaskSize`는 저희 작업 사본에서 0.5 → 10으로 바뀌어 있습니다. 전투 필드 전체에 격자가 보이게 하려는 값입니다. 민형님 의도와 다르면 알려 주세요. 전투 쪽에서 코드로 넣도록 바꾸겠습니다.

## 4. 전투 쪽 사용 방식

- `ViewSystem/Combat/Display/FieldGridDecal.cs`가 **머티리얼 인스턴스를 만들어** 값을 넣습니다. 공유 머티리얼은 바꾸지 않습니다.
  - `_CellSize`: 칸 중심 간격(XZ)
  - `_GridSize`: 1
  - `_GridEdgeSize`: View DB `gridLineWidth`에서 계산
  - `_GridOrigin`: 필드 모서리의 월드 XZ. 격자선이 칸 경계에 옵니다
  - `_MaskCenter`: 필드 중심의 월드 XZ
- 격자 데칼은 View DB `StageFieldViewData`의 표시 모드가 `GridOutline`일 때만 만듭니다. 그때도 이동 경로 드래그·공격 조준·배치 드래그 중에만 켭니다. 같은 칸 강조는 `AreaOutlineSystem` 핸들로 그립니다(칸 테두리, 층마다 하나).

## 5. 확인 부탁드리는 것

1. **속성 추가를 민형님 파일에 둬도 될지.** 괜찮지 않으면 다른 이름·새 GUID의 전투 전용 그래프로 옮기겠습니다.
2. **AreaOutline 점선 변경(§6)이 괜찮은지.** 특히 공유 머티리얼 `AreaOutline.mat`의 값(`_DashCount` 34.1, `_DashFill` 0.649)이 이제 핸들마다 `AreaOutlineData` 값으로 덮입니다. `TestRuner`도 인스펙터 값이 그대로 적용됩니다.
3. **데칼이 캐릭터를 덮는 문제:** 행동 중에 데칼 상자 안에 들어온 캐릭터 아래쪽에도 격자선이 그려집니다. 해결안이 두 가지 있는데, 민형님 생각을 듣고 싶습니다.
   - (a) 렌더러 Decal Layers를 켜고 Rendering Layer 하나를 추가합니다.
   - (b) 같은 격자 계산을 하는 Unlit(Transparent) 버전 그래프를 만들어 바닥 위 Quad에 씁니다.

## 6. AreaOutline 점선 변경 (2026-10-09, 사용자 지시로 용근 쪽에서 구현)

**문제:** 메시 UV U가 루프마다 `거리 ÷ 둘레`(0 → 1)이고, 셰이더는 그 구간에 `_DashCount`개를 그립니다. 그래서 칸 하나짜리 테두리든 큰 범위든 점선 개수가 같았습니다. 작은 테두리는 촘촘하고 큰 테두리는 점선이 길었습니다. 또 `AreaOutlineHandle.Update`가 메시와 색만 넣어서, `AreaOutlineData`의 점선·흐름 값은 쓰이지 않고 공유 머티리얼 값이 모든 테두리에 적용됐습니다.

| 파일 | 변경 |
|---|---|
| `RenderSystem/View/AreaOutlineSystem/AreaOutlineData.cs` | 선택 인자 `dashLength`(기본 0) 추가. 0보다 크면 점선 하나(점+틈)의 월드 길이, 0이면 지금처럼 `DashCount`를 루프마다 씀. 기존 호출(`TestRuner`)은 그대로 컴파일됩니다 |
| `RenderSystem/Core/MeshSystem/MeshGeometryData.cs` | `GridOutlineCreateData.DashLength`(선택 인자, 기본 0) |
| `RenderSystem/Core/MeshSystem/MeshHightLevelBuilder.cs` | `AddOutlineLoop`: `DashLength`가 있으면 U를 루프마다 `0 → round(둘레 ÷ DashLength)`(최소 1)로 늘림. 정수라 닫히는 이음매에서도 무늬가 이어집니다. 없으면 기존 0 → 1 |
| `RenderSystem/View/AreaOutlineSystem/AreaOutlineHandle.cs` | `Update`가 `_DashEnabled`·`_DashCount`(DashLength가 있으면 1)·`_DashFill`·`_FlowDirection`(Clockwise 1 / Counterclockwise 0, 셰이더 Enum과 같음)·`_FlowSpeed`를 `ColorShaderPropertyKey`로 핸들마다 넣음 |

- 셰이더(`AreaOutline.shader`)는 바꾸지 않았습니다. `phase = u × round(_DashCount)`가 u > 1에서도 그대로 동작합니다.
- 흐름 속도는 "초당 점선 한 칸"이라, 점선 길이가 같아진 덕에 모든 테두리가 같은 월드 속도로 흐릅니다.
- 전투 쪽은 View DB `StageFieldViewData.outlineDashLength`(기본 0.25)·`outlineDashFill`(기본 0.65)를 넘깁니다.
- 검증: MSBuild 오류 0. 메시 U 테스트 `AreaOutlineMeshTests` 3건을 Unity Mono로 직접 실행해 통과했습니다. Unity 재생 확인은 대기 중입니다.
