using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>완성 위치에서 등장 순서와 시작 지연만 계산합니다. Transform 자세·표시·진행 상태는 변경하지 않습니다.</summary>
    internal static class CombatBoardRevealSchedule
    {
        private sealed class Timing
        {
            internal Vector2 Position;
            internal bool IsTile;
            internal int Index;
            internal float Delay;
        }

        internal static float[] Create(Transform field, IReadOnlyList<CombatBoardRevealTarget> targets,
            CombatBoardRevealSettings settings)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            var byAnchor = new Dictionary<Transform, Timing>();
            var sources = new List<Timing>();
            var assigned = new Timing[targets.Count];
            for (int i = 0; i < targets.Count; i++)
            {
                CombatBoardRevealTarget target = targets[i];
                if (target.Body == null)
                    throw new InvalidOperationException($"[CombatBoardRevealSchedule] 등장 대상이 사라졌습니다. Index: {i}");
                bool hasTile = target.Tile != null;
                Transform anchor = hasTile ? target.Tile : target.Body;
                if (!byAnchor.TryGetValue(anchor, out Timing timing))
                {
                    Vector3 position = field.InverseTransformPoint(anchor.position);
                    timing = new Timing
                    {
                        Position = new Vector2(position.x, position.z),
                        Index = sources.Count
                    };
                    byAnchor.Add(anchor, timing);
                    sources.Add(timing);
                }
                timing.IsTile |= hasTile;
                assigned[i] = timing;
            }

            switch (settings.Mode)
            {
                case CombatBoardRevealMode.Default:
                    SetDefault(sources, settings.SweepSeconds);
                    break;
                case CombatBoardRevealMode.Coner:
                    SetConer(sources, settings);
                    break;
                default:
                    throw new InvalidOperationException($"[CombatBoardRevealSchedule] 시작 시간표가 없는 모드입니다: {settings.Mode}");
            }

            var delays = new float[targets.Count];
            for (int i = 0; i < delays.Length; i++) delays[i] = assigned[i].Delay;
            return delays;
        }

        private static void SetDefault(List<Timing> sources, float seconds)
        {
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            foreach (Timing timing in sources)
            {
                minimum = Mathf.Min(minimum, timing.Position.y);
                maximum = Mathf.Max(maximum, timing.Position.y);
            }
            float span = maximum - minimum;
            foreach (Timing timing in sources)
                timing.Delay = span > .0001f ? Mathf.Clamp01((timing.Position.y - minimum) / span) * seconds : 0f;
        }

        private static void SetConer(List<Timing> sources, CombatBoardRevealSettings settings)
        {
            if (sources.Count == 0) return;
            var tiles = new List<Timing>();
            foreach (Timing timing in sources)
                if (timing.IsTile) tiles.Add(timing);
            if (tiles.Count == 0)
                throw new InvalidOperationException("[CombatBoardRevealSchedule] Coner 모드에는 생성 타일이 필요합니다.");
            tiles.Sort(CompareConer);

            int next = 0;
            int batch = 0;
            while (next < tiles.Count)
            {
                // 남은 수로 먼저 제한하므로 큰 증가량도 정수 변환에서 넘치지 않습니다.
                int size = (int)Math.Min(tiles.Count - next, 1d + batch * (double)settings.ConerBatchGrowth);
                int end = next + size;
                while (next < end) tiles[next++].Delay = batch;
                batch++;
            }
            foreach (Timing tile in tiles)
                tile.Delay = batch > 1 ? (tile.Delay / (batch - 1)) * settings.SweepSeconds : 0f;

            // 타일 없는 지형은 묶음 개수를 늘리지 않고 자기 대각 위치로 지연을 정합니다.
            float minimum = ConerDepth(tiles[0]);
            float span = ConerDepth(tiles[tiles.Count - 1]) - minimum;
            foreach (Timing timing in sources)
            {
                if (timing.IsTile) continue;
                timing.Delay = span > .0001f
                    ? Mathf.Clamp01((ConerDepth(timing) - minimum) / span) * settings.SweepSeconds
                    : 0f;
            }
        }

        private static float ConerDepth(Timing timing) => timing.Position.x + timing.Position.y;

        private static int CompareConer(Timing left, Timing right)
        {
            int order = ConerDepth(left).CompareTo(ConerDepth(right));
            if (order != 0) return order;
            order = left.Position.y.CompareTo(right.Position.y);
            if (order != 0) return order;
            order = left.Position.x.CompareTo(right.Position.x);
            return order != 0 ? order : left.Index.CompareTo(right.Index);
        }
    }
}
