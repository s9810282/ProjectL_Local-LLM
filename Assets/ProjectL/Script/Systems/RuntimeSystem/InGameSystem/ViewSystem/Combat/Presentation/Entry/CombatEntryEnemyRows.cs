using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>적이 있는 그리드 행만 큰 Y부터 순번을 매겨 기존 대상 순서의 시작 지연을 만듭니다. 시간 진행과 표시는 맡지 않습니다.</summary>
    internal sealed class CombatEntryEnemyRows
    {
        private readonly float _rowGapSeconds;

        internal CombatEntryEnemyRows(float rowGapSeconds)
        {
            CombatPresentationSettingsData.ValidateSeconds(rowGapSeconds, nameof(rowGapSeconds));
            _rowGapSeconds = rowGapSeconds;
        }

        internal float[] CreateStartDelays(IReadOnlyList<int> gridRows)
        {
            if (gridRows == null) throw new ArgumentNullException(nameof(gridRows));
            var rows = new List<int>(new HashSet<int>(gridRows));
            rows.Sort((left, right) => right.CompareTo(left));
            var starts = new Dictionary<int, float>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                float delay = i * _rowGapSeconds;
                CombatPresentationSettingsData.ValidateSeconds(delay, nameof(delay));
                starts.Add(rows[i], delay);
            }

            var delays = new float[gridRows.Count];
            for (int i = 0; i < gridRows.Count; i++) delays[i] = starts[gridRows[i]];
            return delays;
        }
    }
}
