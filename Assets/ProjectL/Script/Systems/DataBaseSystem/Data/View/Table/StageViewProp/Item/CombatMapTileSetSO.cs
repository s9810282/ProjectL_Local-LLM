using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>
    /// 바닥 타일 종류별 외형(프리팹·윗면 높이) 입력입니다. 스테이지 맵 프랍의 타일 배치(<c>StageViewPropData.mapTiles</c>)가 참조하고
    /// 여러 스테이지가 하나를 함께 쓸 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatMapTileSet", menuName = "ProjectL/Database/View/Combat Map Tile Set")]
    internal sealed class CombatMapTileSetSO : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private CombatMapTileKind kind;
            [Tooltip("이 종류의 칸마다 고를 프리팹 후보입니다. 하나면 그대로 쓰고 여러 개면 등록 항목 중 하나를 같은 확률로 고릅니다. 후보는 같은 크기·피벗·윗면 높이로 만듭니다.")]
            [SerializeField] private GameObject[] prefabs;
            [Tooltip("이 종류 타일 윗면이 필드 바닥면(배경과 딱 맞는 높이 = 0)에서 위아래로 떨어진 양입니다. 필드 단위(지형 외형 surfaceHeight와 같음)이고 배경 배율과 무관합니다. 위는 +, 바닥보다 들어간 타일은 −입니다. 그 칸의 유닛·강조·포인터가 이 높이를 따르고, 지형은 이 윗면 위에 섭니다(타일 높이 설계 TH1).")]
            [SerializeField] private float surfaceHeight;

            internal CombatMapTileKind Kind => kind;
            internal GameObject[] Prefabs => prefabs;
            internal float SurfaceHeight => surfaceHeight;
        }

        [Tooltip("종류마다 한 줄입니다. 같은 종류를 두 번 적을 수 없습니다.")]
        [SerializeField] private Entry[] tiles = Array.Empty<Entry>();

        /// <summary>입력을 검사하고 종류 → 외형 표를 매번 새로 만듭니다(DB 규칙 P2).</summary>
        internal Dictionary<CombatMapTileKind, CombatMapTileVisual> CreateVisuals()
        {
            var visuals = new Dictionary<CombatMapTileKind, CombatMapTileVisual>();
            foreach (Entry entry in tiles ?? Array.Empty<Entry>())
            {
                if (!System.Enum.IsDefined(typeof(CombatMapTileKind), entry.Kind))
                    throw new InvalidOperationException($"[CombatMapTileSetSO] 알 수 없는 타일 종류입니다: {(int)entry.Kind}, Asset: {name}");
                GameObject[] candidates = entry.Prefabs ?? Array.Empty<GameObject>();
                if (candidates.Length == 0)
                    throw new InvalidOperationException($"[CombatMapTileSetSO] {entry.Kind} 타일 프리팹 후보가 비어 있습니다. Asset: {name}");
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] == null)
                        throw new InvalidOperationException($"[CombatMapTileSetSO] {entry.Kind} 타일 프리팹 후보 [{i}]가 비어 있습니다. Asset: {name}");
                }
                if (!float.IsFinite(entry.SurfaceHeight))
                    throw new InvalidOperationException($"[CombatMapTileSetSO] {entry.Kind} 타일 윗면 높이가 잘못되었습니다: {entry.SurfaceHeight}, Asset: {name}");
                if (!visuals.TryAdd(entry.Kind, new CombatMapTileVisual(candidates, entry.SurfaceHeight)))
                    throw new InvalidOperationException($"[CombatMapTileSetSO] {entry.Kind} 타일이 두 번 있습니다. Asset: {name}");
            }

            return visuals;
        }
    }
}
