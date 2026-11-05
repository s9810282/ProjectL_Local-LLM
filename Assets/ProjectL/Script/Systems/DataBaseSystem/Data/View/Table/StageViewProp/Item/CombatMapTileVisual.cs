using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>
    /// 바닥 타일 한 종류의 외형 후보와 공유 높이입니다. 타일 세트(<see cref="CombatMapTileSetSO"/>)가 종류마다 하나 만들고,
    /// 타일 배치(<see cref="CombatMapTilesData.VisualOf"/>)가 칸의 종류로 찾습니다.
    /// </summary>
    internal sealed class CombatMapTileVisual
    {
        private readonly GameObject[] _prefabs;

        internal CombatMapTileVisual(GameObject[] prefabs, float surfaceHeight)
        {
            if (prefabs == null)
                throw new ArgumentNullException(nameof(prefabs));
            if (prefabs.Length == 0)
                throw new ArgumentException("타일 프리팹 후보가 필요합니다.", nameof(prefabs));
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null)
                    throw new ArgumentException($"타일 프리팹 후보 [{i}]가 비어 있습니다.", nameof(prefabs));
            }
            if (!float.IsFinite(surfaceHeight))
                throw new ArgumentOutOfRangeException(nameof(surfaceHeight));

            _prefabs = (GameObject[])prefabs.Clone();
            SurfaceHeight = surfaceHeight;
        }

        /// <summary>이 종류의 칸에 놓을 프리팹 후보 수입니다.</summary>
        public int PrefabCount => _prefabs.Length;

        /// <summary>입력 순서의 프리팹 후보입니다. 선택은 생성하는 쪽이 한 번만 합니다.</summary>
        public GameObject PrefabAt(int index)
        {
            if (index < 0 || index >= _prefabs.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _prefabs[index];
        }

        /// <summary>
        /// 타일 윗면이 필드 바닥면(배경과 딱 맞는 높이 = 0)에서 위아래로 떨어진 양입니다. 필드 단위이고, 바닥보다 들어간 타일은 음수입니다(타일 높이 설계 TH1).
        /// 필드 칸마다의 값은 배경 표시가 맞춤 뒤에 만드는 타일 높이 표가 정합니다.
        /// </summary>
        public float SurfaceHeight { get; }
    }
}
