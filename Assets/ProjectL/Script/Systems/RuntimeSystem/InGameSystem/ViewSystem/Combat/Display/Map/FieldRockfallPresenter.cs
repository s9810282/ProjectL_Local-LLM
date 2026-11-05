using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 낙석 표시입니다(낙석 View 설계 RF2). 경고 칸은 강조 층(테두리·채우기)과 느낌표를 함께 켜고 끕니다. 어느 칸인지는 Core 상태가 원본이고,
    /// 사건 재생이 사건 시점에 켠 표시를 결과 끝 <see cref="Render"/>가 Buffer 값으로 다시 맞춥니다.
    /// </summary>
    internal sealed class FieldRockfallPresenter : ICombatRockfallField
    {
        private readonly FieldView _field;
        private readonly Transform _tunnel;
        private readonly Transform _ridgeStart;
        private readonly Transform _ridgeEnd;
        private readonly bool _tunnelExplosionEnabled;
        private readonly FieldCellMarkers _marks;
        private readonly List<GameObject> _stones = new();
        private readonly Stack<GameObject> _freeStones = new();
        private bool _isReleased;
        private bool _missingTunnelWarned;
        private bool _missingRidgeWarned;

        /// <summary>낙석 표현 설정입니다.</summary>
        public RockfallVisualData Settings { get; }

        /// <param name="tunnel">맵 프랍 안의 터널 폭발 자리입니다(계획 RP-1). 없으면 null이고, 폭발 없이 경고 칸만 켭니다.</param>
        /// <param name="ridgeStart">맵 프랍 안의 능선 한쪽 끝입니다(설계 RG2). 없으면 null입니다.</param>
        /// <param name="ridgeEnd">맵 프랍 안의 능선 다른 쪽 끝입니다. 없으면 null입니다.</param>
        /// <param name="tunnelExplosionEnabled">이 스테이지의 터널 폭발 사용 여부입니다. 거짓이어도 경고 칸·능선·낙하·착지 효과는 유지합니다.</param>
        internal FieldRockfallPresenter(FieldView field, RockfallVisualData settings, Transform tunnel, Transform ridgeStart, Transform ridgeEnd,
            bool tunnelExplosionEnabled)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _tunnel = tunnel;
            _ridgeStart = ridgeStart;
            _ridgeEnd = ridgeEnd;
            _tunnelExplosionEnabled = tunnelExplosionEnabled;
            _marks = new FieldCellMarkers(field.CellsRoot, settings.WarningMarkPrefab, settings.WarningMarkSize,
                settings.WarningMarkLift, FieldSortingOrders.RockfallWarningMark);
        }

        /// <summary>진입·결과 끝에 Core 상태로 표시를 다시 맞춥니다. 경고 단계면 켜고, 대기 단계이거나 상태가 없으면 끕니다. 옛 폭발은 다시 내지 않습니다.</summary>
        internal void Render(StageGimmickStateSet gimmicks, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            IReadOnlyList<StageGimmickState> entries = gimmicks.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is RockfallGimmickState { Phase: RockfallGimmickPhase.Warning } rockfall)
                {
                    ShowWarning(rockfall.WarnedCells, mapper);
                    return;
                }
            }
            ClearWarning();
        }

        public bool TryGetTunnelPose(out Pose pose)
        {
            // 사용자가 끈 표현은 누락이 아닙니다. 터널·자식 능선 오브젝트는 그대로 두고 폭발 큐만 생략합니다(편집 설계 RTE).
            if (!_tunnelExplosionEnabled)
            {
                pose = default;
                return false;
            }
            if (_tunnel == null)
            {
                // 낙석을 쓰는 맵에서 처음 물을 때 한 번만 알립니다. 낙석을 안 쓰는 맵은 여기까지 오지 않습니다(이름 오타를 찾기 위함, 동작은 계획 RP-12 그대로).
                if (!_missingTunnelWarned)
                {
                    _missingTunnelWarned = true;
                    Debug.LogWarning($"[FieldRockfallPresenter] 맵 프랍에 터널 오브젝트 '{StageViewPropPresenter.RockfallTunnelObjectName}'이 없어 폭발 없이 진행합니다.");
                }
                pose = default;
                return false;
            }
            pose = new Pose(_tunnel.position, _tunnel.rotation);
            return true;
        }

        public bool TryGetRidge(out Vector3 start, out Vector3 end)
        {
            if (_ridgeStart == null || _ridgeEnd == null)
            {
                // 터널 경고와 같이 처음 물을 때 한 번만 알립니다. 한쪽만 있는 구간은 쓸 수 없어 없는 것과 같습니다(계획 RGP-2).
                if (!_missingRidgeWarned)
                {
                    _missingRidgeWarned = true;
                    Debug.LogWarning($"[FieldRockfallPresenter] 맵 프랍에 능선 오브젝트 '{StageViewPropPresenter.RockfallRidgeStartObjectName}'·'{StageViewPropPresenter.RockfallRidgeEndObjectName}'가 모두 있지 않아 돌이 칸 위에서 곧게 떨어집니다.");
                }
                start = default;
                end = default;
                return false;
            }
            start = _ridgeStart.position;
            end = _ridgeEnd.position;
            return true;
        }

        public void ShowWarning(IReadOnlyList<CellCoord> cells, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            // 느낌표가 인자를 먼저 검사해, 잘못된 호출이면 강조 층을 바꾸지 않습니다.
            _marks.Show(cells, mapper);
            _field.SetLayerCells(FieldHighlightLayer.RockfallWarning, cells);
        }

        public void ClearWarning()
        {
            _field.ClearLayer(FieldHighlightLayer.RockfallWarning);
            _marks.Clear();
        }

        public GameObject RentStone(float scale)
        {
            ThrowIfReleased();
            GameObject stone = _freeStones.Count > 0 ? _freeStones.Pop() : CreateStone();
            // 풀에서 다시 쓰는 돌에 지난 배율이 쌓이지 않게 늘 프리팹 크기에서 다시 계산합니다(계획 RGP-5).
            stone.transform.localScale = Settings.StonePrefab.transform.localScale * scale;
            stone.SetActive(true);
            return stone;
        }

        public void ReturnStone(GameObject stone)
        {
            // 해제 뒤에는 돌이 이미 없어졌습니다.
            if (_isReleased || stone == null) return;
            stone.SetActive(false);
            _freeStones.Push(stone);
        }

        /// <summary>표시를 끕니다. 전투가 바뀌면 이 경로로 지난 전투의 경고가 남지 않습니다.</summary>
        internal void Clear() => ClearWarning();

        internal void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            Clear();
            _marks.Release();
            foreach (GameObject stone in _stones)
            {
                if (stone == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(stone);
                else UnityEngine.Object.DestroyImmediate(stone);
            }
            _stones.Clear();
            _freeStones.Clear();
        }

        private GameObject CreateStone()
        {
            GameObject stone = UnityEngine.Object.Instantiate(Settings.StonePrefab, _field.CellsRoot, false);
            // 맵 소품 프리팹은 한쪽 카메라 전용 레이어일 수 있어 돌을 만들 때 맞춥니다(계획 RP-13).
            foreach (Transform part in stone.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = FieldRenderLayers.BothRendering;
            _stones.Add(stone);
            return stone;
        }

        private void ThrowIfReleased()
        {
            if (_isReleased) throw new InvalidOperationException("[FieldRockfallPresenter] 해제된 표시입니다.");
        }
    }
}
