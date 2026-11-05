using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>스냅샷 변경을 감지하고 View DB 외형을 표시 값으로 바꿔 3D와 UI에 전달합니다.</summary>
    internal sealed class ArtifactBoardRefresher
    {
        private readonly IArtifactBufferReadSink _board;
        private readonly Func<int, ArtifactViewData> _findVisual;
        private readonly IArtifactPopupPresenter _presenter;
        private readonly IArtifactPopupDisplay _screen;
        private readonly ArtifactPopupStatus _status;
        private readonly Dictionary<int, ArtifactViewData> _visuals = new();
        private ArtifactBufferSnapshot _drawn;
        // 보드가 바뀌기 직전에 끌기를 취소하는 콜백입니다. 늘 넘기므로 필수 인자입니다(로비 리팩토링 2차 설계 N-25).
        private readonly Action _beforeBoardChange;

        internal ArtifactBoardRefresher(IArtifactBufferReadSink board,
            Func<int, ArtifactViewData> findVisual, IArtifactPopupPresenter presenter,
            IArtifactPopupDisplay screen, ArtifactPopupStatus status, Action beforeBoardChange)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _findVisual = findVisual ?? throw new ArgumentNullException(nameof(findVisual));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _beforeBoardChange = beforeBoardChange ?? throw new ArgumentNullException(nameof(beforeBoardChange));
        }

        internal void Reset() => _drawn = null;

        internal void Refresh()
        {
            ArtifactBufferSnapshot current = _board.Current;
            if (!ReferenceEquals(current, _drawn))
            {
                _beforeBoardChange();
                _visuals.Clear();
                if (!current.IsInitialized)
                {
                    _presenter.ClearBoard();
                    _screen.RenderPool(Array.Empty<ArtifactPoolItemContent>());
                }
                else
                {
                    ArtifactPopupItem[] placed = ToItems(current.PlacedItems);
                    ArtifactPopupItem[] pool = ToItems(current.PoolItems);
                    _presenter.Render(new ArtifactPopupBoard(current.BoardWidth, current.BoardHeight, placed, pool));
                    var poolContent = new ArtifactPoolItemContent[current.PoolItems.Count];
                    for (int i = 0; i < poolContent.Length; i++)
                    {
                        ArtifactData item = current.PoolItems[i];
                        ArtifactViewData visual = Visual(item.ArtifactId);
                        poolContent[i] = new ArtifactPoolItemContent(item.ArtifactId, item.InstanceId, item.Cells,
                            visual.DisplayName, visual.Description);
                    }
                    _screen.RenderPool(poolContent);
                }
                _drawn = current;
                _status.SetWaiting(!current.IsInitialized);
            }
        }

        internal void ShowDetail(int artifactId)
        {
            ArtifactViewData visual = Visual(artifactId);
            _screen.ShowDetail(visual.DisplayName, visual.Description);
        }

        internal void ClearDetail() => _screen.ShowDetail(string.Empty, string.Empty);

        /// <summary>외형의 모델 프리팹입니다. 외형 표의 필수 칸이라 DB를 만들 때 검사가 끝났습니다.</summary>
        internal GameObject GetModel(int artifactId) => Visual(artifactId).Prefab;

        private ArtifactViewData Visual(int artifactId)
        {
            if (_visuals.TryGetValue(artifactId, out ArtifactViewData visual)) return visual;
            visual = _findVisual(artifactId);
            _visuals.Add(artifactId, visual);
            return visual;
        }

        private ArtifactPopupItem[] ToItems(IReadOnlyList<ArtifactData> source)
        {
            var items = new ArtifactPopupItem[source.Count];
            for (int i = 0; i < items.Length; i++)
            {
                ArtifactData item = source[i];
                items[i] = new ArtifactPopupItem(item.InstanceId, item.Cells, GetModel(item.ArtifactId), item.Rotation);
            }
            return items;
        }
    }
}
