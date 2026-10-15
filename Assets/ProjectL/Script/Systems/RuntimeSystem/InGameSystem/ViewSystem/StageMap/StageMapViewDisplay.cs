using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>전달받은 데이터로 맵 표시와 해제 순서를 조율합니다. Unity 메인 스레드에서 사용합니다.</summary>
    internal sealed class StageMapViewDisplay
    {
        private readonly StageNodeView _nodes = new();
        private readonly StageLinkView _links = new();

        /// <summary>기존 표시를 교체합니다. 외형 조회 함수는 호출자가 기존 Table에 연결하며 null은 외형 미지정입니다.</summary>
        internal void Show(IReadOnlyList<StageProgressData> nodes, StageMapViewData mapData,
            Func<StageProgressData, GameObject> findVisualPrefab, Transform parent)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (mapData == null) throw new ArgumentNullException(nameof(mapData));
            if (findVisualPrefab == null) throw new ArgumentNullException(nameof(findVisualPrefab));
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            Clear();
            try
            {
                _nodes.Show(nodes, mapData, findVisualPrefab, parent);
                _links.Show(nodes, mapData, _nodes, parent);
            }
            catch
            {
                Clear();
                throw;
            }
        }

        /// <summary>표시 중인 노드의 Transform을 빌려줍니다. Clear 이후에는 보관해 사용하지 않습니다.</summary>
        internal bool TryGetNode(int nodeId, out Transform nodeRoot)
        {
            return _nodes.TryGetNode(nodeId, out nodeRoot);
        }

        /// <summary>맞은 오브젝트가 속한 노드 ID를 찾습니다. 표시 중인 노드가 아니면 false입니다.</summary>
        internal bool TryGetNodeId(Transform hit, out int nodeId)
        {
            return _nodes.TryGetNodeId(hit, out nodeId);
        }

        /// <summary>생성한 연결선과 노드만 정리합니다. 씬 이탈 때 호출해야 합니다.</summary>
        internal void Clear()
        {
            _links.Clear();
            _nodes.Clear();
        }
    }
}
