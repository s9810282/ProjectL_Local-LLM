// 전투 GridOutline 칸 채우기 판(겹친 강조 2단계 설계 RC1, 4판 RC11·RC13).
// 판 하나는 같은 종류·같은 깊이 칸들이고, 칸마다 칸 전체 크기 상자(얇은 판)를 놓는다. 모양은 이 셰이더가 칸 지도로 깎는다.
// 칸 지도(_RegionMap·_HoleMap)는 깊이마다 칸 수 크기 텍스처이고 값은 그 칸 그 깊이의 종류 번호 + 1(비면 0)이다.
// _Inset이 0보다 크면(깊이 1 이상) 같은 종류 칸 덩어리의 바깥 둘레를 변마다 안쪽으로 옮기고, 바깥으로 튀어나온(볼록) 모서리만
// _CornerRadius로 둥글게 한 모양만 칠한다. 같은 종류 이웃 칸과 맞닿은 쪽은 끝까지 이어지고, 안으로 꺾인(오목) 모서리는 직각이다.
// _Inset·_CornerRadius가 모두 0이면(덩어리 방식의 깊이 0) 칸 전체 모양이다. 그라데이션이 켜지면 이 모양의 윤곽 거리도 구한다.
// _JoinCells가 0이면(칸마다 방식, 5판 RC17) 이웃을 잇지 않아 칸마다 줄이고 네 모서리를 둥글게 한 판이다. 칸마다 방식의 깊이 0도 밑 층 크기·반지름으로 깎는다.
// _UseHoleShapes가 켜지면 _HoleMap(RFloat)은 실제 바로 위 종류(하위 8비트)와 그 판 자체의 이웃(상위 8비트)을 담는다.
// _HoleShapes[종류 번호]의 실제 모양만큼 비워 빈 깊이를 건너뛴다. 아래 판/다른 위 판의 유무가 위 판 자체의 이음을 바꾸지 않는다.
// 기존 직접 재질 사용(_UseHoleShapes 0)은 위 판 모양(_HoleInset·_HoleRadius·_HoleJoinCells)만큼 비운다.
// _HoleJoinCells가 음수인 기존 직접 재질 사용은 _JoinCells를 따르고, 운영 표시기는 현재/다음 깊이의 연결 방식을 각각 넣는다.
// 격자 높이(_HeightMap, 지형이 올린 높이)가 다른 이웃은 같은 종류여도 잇지 않는다. 칸 테두리가 높이별로 나뉘는 것과 같다.
// 판 둘레의 선은 칸 테두리(AreaOutline)가 같은 모양으로 그린다(3판 RC8, 4판 RC12). 이 셰이더는 선을 그리지 않는다.
// 칸은 오브젝트 좌표(필드 칸 루트 로컬 x, z)로 찾는다. 채우기 객체가 칸 루트 바로 아래 단위 변환으로 놓이는 것에 기댄다.
// _BaseColor는 렌더러마다 ColorSystem이 넣는다. 전이 폭·중앙 비율은 원본 재질에서 상속하고, 나머지는 FieldCellFillStack이 판마다 넣는다.
// 바탕(투명 큐·양면·깊이 쓰기 없음·섞기)은 AreaOutline 셰이더와 같다.
Shader "ProjectL/Combat/CellFill"
{
    Properties
    {
        _BaseColor ("Fill Color", Color) = (1, 1, 1, 1)
        _EdgeFadeWidth ("Inner Fade Width (plate short-side ratio, 0 = flat)", Range(0, 0.45)) = 0
        _CenterOpacity ("Center Opacity Ratio", Range(0, 1)) = 1
        [HideInInspector] _OpenEdge ("Open Edge (direction x,z; grid line)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _ClearArea ("Clear Boundary Area (min/max grid corners)", Vector) = (0, 0, 0, 0)
        _GridCorner ("Grid Corner (object x, z of cell (0,0) lower-left)", Vector) = (0, 0, 0, 0)
        _CellSize ("Cell Size (x = X, y = Z)", Vector) = (1, 1, 0, 0)
        _GridSize ("Grid Size (x = columns, y = rows)", Vector) = (1, 1, 0, 0)
        [NoScaleOffset] _RegionMap ("Kind Map Of This Depth", 2D) = "black" {}
        _RegionKind ("Kind Of This Plate (map value)", Float) = 0
        _Inset ("Inset (x = Z edges move in X, y = X edges move in Z, 0 = whole cell)", Vector) = (0, 0, 0, 0)
        _CornerRadius ("Convex Corner Radius", Float) = 0
        [NoScaleOffset] _HoleMap ("Kind Map Of The Depth Above", 2D) = "black" {}
        _HoleInset ("Hole Inset", Vector) = (0, 0, 0, 0)
        _HoleRadius ("Hole Convex Corner Radius", Float) = 0
        [NoScaleOffset] _HeightMap ("Cell Grid Height Map", 2D) = "black" {}
        _JoinCells ("Join Same-Kind Neighbors (1 = merged clusters, 0 = each cell alone)", Float) = 1
        [HideInInspector] _HoleJoinCells ("Join Hole Neighbors (-1 = follow plate)", Float) = -1
        [HideInInspector] _UseHoleShapes ("Use Actual Upper-Kind Shapes", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "CellFill"
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            // 정수·비트 연산과 텍스처 Load를 씁니다.
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionXZ : TEXCOORD0;
            };

            TEXTURE2D(_RegionMap);
            TEXTURE2D(_HoleMap);
            TEXTURE2D(_HeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _EdgeFadeWidth;
                float _CenterOpacity;
                float4 _OpenEdge;
                float4 _ClearArea;
                float4 _GridCorner;
                float4 _CellSize;
                float4 _GridSize;
                float _RegionKind;
                float4 _Inset;
                float _CornerRadius;
                float4 _HoleInset;
                float _HoleRadius;
                float _JoinCells;
                float _HoleJoinCells;
                float _UseHoleShapes;
                // 종류 지도는 한 바이트입니다. 실제 사용하는 종류 수만큼만 재질에 전달합니다.
                float4 _HoleShapes[256];
            CBUFFER_END

            // 이웃 칸 방향이다. 비트 i가 offsets[i] 칸이다: +X, −X, +Z, −Z, +X+Z, −X+Z, +X−Z, −X−Z.
            static const int2 NeighborOffsets[8] =
            {
                int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1),
                int2(1, 1), int2(-1, 1), int2(1, -1), int2(-1, -1)
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionXZ = input.positionOS.xz;
                return output;
            }

            // 칸 지도 값(종류 번호 + 1, 0 = 없음)이다. 판 밖 칸은 0이다.
            int KindAt(int2 cell, bool hole)
            {
                if (cell.x < 0 || cell.y < 0 || cell.x >= (int)_GridSize.x || cell.y >= (int)_GridSize.y)
                    return 0;
                float value = hole ? LOAD_TEXTURE2D(_HoleMap, cell).r : LOAD_TEXTURE2D(_RegionMap, cell).r;
                if (hole && _UseHoleShapes > 0.5)
                    return (int)round(value) & 255;
                return (int)round(value * 255.0);
            }

            // 칸의 격자 높이다. 판 안 칸만 부른다.
            float HeightAt(int2 cell)
            {
                return LOAD_TEXTURE2D(_HeightMap, cell).r;
            }

            // 같은 종류이고 격자 높이가 같은 이웃 칸 비트 묶음이다(NeighborOffsets 순서). 판 밖 이웃은 종류가 0이라 빠진다.
            // 판/구멍 각 깊이의 칸마다 방식이면 늘 0이라 칸마다 따로 깎인다.
            uint SameKindNeighbors(int2 cell, int kind, bool hole)
            {
                float joinCells = hole && _HoleJoinCells >= 0.0 ? _HoleJoinCells : _JoinCells;
                if (joinCells < 0.5)
                    return 0u;
                if (hole && _UseHoleShapes > 0.5)
                    return (uint)round(LOAD_TEXTURE2D(_HoleMap, cell).r) >> 8;
                float height = HeightAt(cell);
                uint neighbors = 0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    int2 neighbor = cell + NeighborOffsets[i];
                    if (KindAt(neighbor, hole) == kind)
                    {
                        if (abs(HeightAt(neighbor) - height) < 0.0001)
                            neighbors |= 1u << i;
                    }
                }
                return neighbors;
            }

            // 상자까지의 거리다(안쪽은 음수).
            float BoxDistance(float2 p, float2 halfSize)
            {
                float2 d = abs(p) - halfSize;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            // 모서리 둥근 사각형까지의 거리다(안쪽은 음수).
            float RoundedBoxDistance(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            // 칸 하나 안에서 본 덩어리 모양까지의 거리다(p = 칸 가운데 기준 자리, 안쪽은 음수).
            // 가운데 둥근 사각형(칸 − 안쪽 거리) ∪ 같은 종류 이웃 쪽 이음 ∪ 두 옆과 대각선이 다 같은 종류인 모서리 사분면.
            // 이음·사분면 상자는 칸 경계를 넘어 이웃 칸 가운데까지 뻗고 가운데 판과 겹쳐, 칸 경계와 판 사이에 이음매가 생기지 않는다.
            // 이웃이 있는 쪽 모서리의 둥근 깎임은 이음 상자가 다시 채워, 둥근 것은 두 옆이 모두 빈 볼록 모서리뿐이다.
            float ClusterDistance(float2 p, float2 halfCell, float2 inset, float radius, uint neighbors)
            {
                float2 core = max(halfCell - inset, 0.0001);
                float distance = RoundedBoxDistance(p, core, clamp(radius, 0.0, min(core.x, core.y)));
                if ((neighbors & 0x01u) != 0u) distance = min(distance, BoxDistance(p - float2(halfCell.x, 0.0), float2(halfCell.x, core.y)));
                if ((neighbors & 0x02u) != 0u) distance = min(distance, BoxDistance(p + float2(halfCell.x, 0.0), float2(halfCell.x, core.y)));
                if ((neighbors & 0x04u) != 0u) distance = min(distance, BoxDistance(p - float2(0.0, halfCell.y), float2(core.x, halfCell.y)));
                if ((neighbors & 0x08u) != 0u) distance = min(distance, BoxDistance(p + float2(0.0, halfCell.y), float2(core.x, halfCell.y)));
                // 대각선 이웃이 다르면 이 사분면을 채우지 않아 오목 꺾임이 직각으로 남는다.
                if ((neighbors & 0x15u) == 0x15u) distance = min(distance, BoxDistance(p - halfCell, halfCell));
                if ((neighbors & 0x26u) == 0x26u) distance = min(distance, BoxDistance(p - float2(-halfCell.x, halfCell.y), halfCell));
                if ((neighbors & 0x49u) == 0x49u) distance = min(distance, BoxDistance(p - float2(halfCell.x, -halfCell.y), halfCell));
                if ((neighbors & 0x8Au) == 0x8Au) distance = min(distance, BoxDistance(p + halfCell, halfCell));
                return distance;
            }

            // 거리 값 안이면 1, 밖이면 0이고 경계는 화면 한 칸 폭만큼 부드럽다.
            float Coverage(float distance, float pixel)
            {
                return 1.0 - smoothstep(-pixel, 0.0, distance);
            }

            bool IsClearCell(int2 cell)
            {
                return cell.x >= _ClearArea.x && cell.y >= _ClearArea.y && cell.x < _ClearArea.z && cell.y < _ClearArea.w;
            }

            float RoundedCornerInside(float2 sides, float radius)
            {
                float2 q = radius - sides;
                return radius - length(max(q, 0.0)) - min(max(q.x, q.y), 0.0);
            }

            // 모양 합집합의 min SDF는 내부 상자 이음도 거리로 남깁니다. 전이는 실제 노출 경계만 봅니다.
            float BoundaryInside(float2 p, float2 halfCell, float2 inset, float radius, uint neighbors)
            {
                float2 core = max(halfCell - inset, 0.0001);
                radius = clamp(radius, 0.0, min(core.x, core.y));
                float right = core.x - p.x, left = core.x + p.x;
                float up = core.y - p.y, down = core.y + p.y;
                bool r = (neighbors & 0x01u) != 0u, l = (neighbors & 0x02u) != 0u;
                bool u = (neighbors & 0x04u) != 0u, d = (neighbors & 0x08u) != 0u;
                float distance = 1e10;
                if (!r) distance = min(distance, right);
                if (!l) distance = min(distance, left);
                if (!u) distance = min(distance, up);
                if (!d) distance = min(distance, down);
                if (!r && !u) distance = min(distance, RoundedCornerInside(float2(right, up), radius));
                if (!l && !u) distance = min(distance, RoundedCornerInside(float2(left, up), radius));
                if (!r && !d) distance = min(distance, RoundedCornerInside(float2(right, down), radius));
                if (!l && !d) distance = min(distance, RoundedCornerInside(float2(left, down), radius));
                if (r && u && (neighbors & 0x10u) == 0u) distance = min(distance, length(max(float2(right, up), 0.0)));
                if (l && u && (neighbors & 0x20u) == 0u) distance = min(distance, length(max(float2(left, up), 0.0)));
                if (r && d && (neighbors & 0x40u) == 0u) distance = min(distance, length(max(float2(right, down), 0.0)));
                if (l && d && (neighbors & 0x80u) == 0u) distance = min(distance, length(max(float2(left, down), 0.0)));
                return max(distance, 0.0);
            }

            // 전이 거리에서만 열린 바깥쪽을 이어진 이웃으로 봅니다. 모양/구멍 마스크와 실제 지도는 바꾸지 않습니다.
            uint FadeNeighbors(int2 cell, uint neighbors)
            {
                if (IsClearCell(cell))
                    return 0xFFu;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    if (IsClearCell(cell + NeighborOffsets[i]))
                        neighbors |= 1u << i;
                }
                int2 direction = (int2)round(_OpenEdge.xy);
                int gridLine = (int)round(_OpenEdge.z);
                if (direction.x < 0 && cell.x == gridLine)
                {
                    neighbors |= 0x02u;
                    if ((neighbors & 0x04u) != 0u) neighbors |= 0x20u;
                    if ((neighbors & 0x08u) != 0u) neighbors |= 0x80u;
                }
                else if (direction.x > 0 && cell.x + 1 == gridLine)
                {
                    neighbors |= 0x01u;
                    if ((neighbors & 0x04u) != 0u) neighbors |= 0x10u;
                    if ((neighbors & 0x08u) != 0u) neighbors |= 0x40u;
                }
                else if (direction.y < 0 && cell.y == gridLine)
                {
                    neighbors |= 0x08u;
                    if ((neighbors & 0x01u) != 0u) neighbors |= 0x40u;
                    if ((neighbors & 0x02u) != 0u) neighbors |= 0x80u;
                }
                else if (direction.y > 0 && cell.y + 1 == gridLine)
                {
                    neighbors |= 0x04u;
                    if ((neighbors & 0x01u) != 0u) neighbors |= 0x10u;
                    if ((neighbors & 0x02u) != 0u) neighbors |= 0x20u;
                }
                return neighbors;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 cellSize = _CellSize.xy;
                float2 halfCell = cellSize * 0.5;
                float2 local = input.positionXZ - _GridCorner.xy;
                int2 cell = (int2)floor(local / cellSize);
                float2 p = local - ((float2(cell) + 0.5) * cellSize);
                // 부드러운 경계 폭(월드 길이)이다. 칸마다 갈리는 분기 밖에서 한 번 구한다.
                float pixel = max(max(fwidth(input.positionXZ.x), fwidth(input.positionXZ.y)), 0.0001);

                float alpha = _BaseColor.a;
                float fadeDistance = 0.0;
                float fadeWidth = clamp(_EdgeFadeWidth, 0.0, 0.45);
                if (_Inset.x > 0.0 || _Inset.y > 0.0 || _CornerRadius > 0.0 || fadeWidth > 0.0)
                {
                    int kind = (int)round(_RegionKind);
                    // 판은 자기 칸에만 놓이지만 칸 경계의 조각은 옆 칸(판 밖 포함)으로 셀 수 있다. 옆 칸이 다른 종류면 그 자리는 비어 있다.
                    // 삼항식은 양쪽을 다 계산할 수 있어, 판 밖 칸에서 이웃 지도를 읽지 않게 갈래로 나눈다.
                    // 종류 0은 아직 구성하지 않은 재질입니다. 높이·이웃 지도를 읽지 않습니다.
                    if (kind > 0 && KindAt(cell, false) == kind)
                    {
                        uint neighbors = SameKindNeighbors(cell, kind, false);
                        float shapeDistance = ClusterDistance(p, halfCell, _Inset.xy, _CornerRadius, neighbors);
                        alpha *= Coverage(shapeDistance, pixel);
                        if (fadeWidth > 0.0)
                        {
                            uint fadeNeighbors = FadeNeighbors(cell, neighbors);
                            fadeDistance = -BoundaryInside(p, halfCell, _Inset.xy, _CornerRadius, fadeNeighbors);
                        }
                    }
                    else
                        alpha = 0.0;
                }

                int holeKind = KindAt(cell, true);
                if (holeKind > 0)
                {
                    float2 holeInset = _HoleInset.xy;
                    float holeRadius = _HoleRadius;
                    if (_UseHoleShapes > 0.5)
                    {
                        float4 holeShape = _HoleShapes[holeKind - 1];
                        holeInset = holeShape.xy;
                        holeRadius = holeShape.z;
                    }
                    alpha *= 1.0 - Coverage(ClusterDistance(p, halfCell, holeInset, holeRadius, SameKindNeighbors(cell, holeKind, true)), pixel);
                }

                // 선을 추가하지 않고 기존 채움의 안쪽 투명도만 낮춥니다.
                // 축소한 판의 짧은 변을 기준으로 하여 EachCell·겹친 작은 판도 같은 비율로 전이합니다.
                if (fadeWidth > 0.0)
                {
                    float2 plateHalf = max(halfCell - _Inset.xy, 0.0001);
                    float width = max(2.0 * min(plateHalf.x, plateHalf.y) * fadeWidth, 0.0001);
                    float inner = smoothstep(0.0, width, max(-fadeDistance, 0.0));
                    alpha *= lerp(1.0, saturate(_CenterOpacity), inner);
                }

                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
