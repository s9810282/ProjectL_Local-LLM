using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Background
{
    /// <summary>한 씬 연결에서 배경 한 벌의 생성·재사용·정리를 소유합니다.</summary>
    internal sealed class StageMapBackgroundDisplay : IDisposable
    {
        private readonly IStageMapBackgroundTableQuery _backgrounds;
        private GameObject _instance;
        private GameObject _prefab;
        private Transform _parent;
        private int _chapterKey;
        private long _generation;
        private bool _disposed;

        internal StageMapBackgroundDisplay(IStageMapBackgroundTableQuery backgrounds)
        {
            _backgrounds = backgrounds ?? throw new ArgumentNullException(nameof(backgrounds));
        }

        /// <summary>같은 키·원본·부모의 배경은 유지합니다. 호출자는 맵 준비 뒤 현재 키를 전달합니다.</summary>
        internal void Show(int chapterKey, Transform parent)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("[StageMapBackgroundDisplay] 폐기한 연결에는 배경을 표시할 수 없습니다.");
            }
            if (chapterKey < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chapterKey));
            }
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            long generation = ++_generation;
            StageMapBackgroundData data = _backgrounds.Get(chapterKey);
            if (!IsCurrent(generation))
            {
                return;
            }
            if (data == null || data.ChapterKey != chapterKey)
            {
                throw new InvalidOperationException(
                    $"[StageMapBackgroundDisplay] 챕터 {chapterKey}와 일치하는 배경 데이터가 필요합니다.");
            }

            if (_instance != null && _chapterKey == chapterKey && _prefab == data.BackgroundPrefab &&
                _parent == parent && _instance.transform.parent == parent)
            {
                return;
            }

            GameObject candidate = null;
            try
            {
                candidate = Object.Instantiate(data.BackgroundPrefab, parent, false);
                // Awake/OnEnable이 연결을 폐기하거나 새 표시를 요청할 수 있습니다.
                if (!IsCurrent(generation))
                {
                    return;
                }
                if (candidate == null || parent == null)
                {
                    throw new InvalidOperationException(
                        $"[StageMapBackgroundDisplay] 챕터 {chapterKey} 배경을 생성하는 동안 인스턴스 또는 부모가 사라졌습니다.");
                }

                GameObject previous = _instance;
                _instance = candidate;
                _prefab = data.BackgroundPrefab;
                _parent = parent;
                _chapterKey = chapterKey;
                candidate = null;

                // 이전 배경의 OnDisable이 재진입해도 가장 최근 소유권을 다시 덮어쓰지 않습니다.
                Release(previous);
            }
            finally
            {
                // 소유권을 게시하지 못한 후보만 정리합니다. 새 호출이 게시한 배경은 건드리지 않습니다.
                Release(candidate);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ++_generation;
            GameObject previous = _instance;
            _instance = null;
            _prefab = null;
            _parent = null;
            _chapterKey = 0;
            Release(previous);
        }

        private bool IsCurrent(long generation) => !_disposed && _generation == generation;

        private static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                // Destroy가 지연되는 동안에도 이전 배경이 보이지 않도록 먼저 비활성화합니다.
                instance.SetActive(false);
            }
            finally
            {
                Object.Destroy(instance);
            }
        }
    }
}
