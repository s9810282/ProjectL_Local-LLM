using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>확정된 앞뒤 비율과 레벨 차이만 재생합니다. 중간 레벨의 규칙 요구량은 계산하지 않습니다.</summary>
    internal sealed class CombatRewardExperienceAnimation
    {
        private readonly int _crossings;
        private readonly float _initial;
        private readonly float _final;
        private readonly float _seconds;
        private int _segment;
        private float _elapsed;
        private bool _advance;

        internal float Fill { get; private set; }
        internal bool LeveledUp { get; private set; }
        internal bool IsComplete { get; private set; }
        internal bool Enabled { get; }

        internal CombatRewardExperienceAnimation(CharacterRewardReceipt row, float seconds)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            _seconds = seconds;
            Enabled = row.RequiredExperienceBefore > 0;
            _crossings = row.LevelAfter - row.LevelBefore;
            _initial = Enabled ? (float)row.ExperienceBefore / row.RequiredExperienceBefore : 0;
            _final = Enabled ? (float)row.ExperienceAfter / row.RequiredExperienceAfter : 0;
            Fill = _initial;
            IsComplete = !Enabled || row.TotalExperience == 0;
        }

        /// <summary>각 100%와 초기화된 0%를 적어도 한 번 표시합니다. 긴 프레임에도 레벨 경계를 건너뛰지 않습니다.</summary>
        internal void Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsComplete) return;
            if (_advance)
            {
                _advance = false;
                _segment++;
                _elapsed = 0;
                Fill = 0;
                return;
            }
            _elapsed = Math.Min(_seconds, _elapsed + deltaSeconds);
            float from = _segment == 0 ? _initial : 0;
            float to = _segment < _crossings ? 1 : _final;
            Fill = from + (to - from) * (_elapsed / _seconds);
            if (_elapsed < _seconds) return;
            if (_segment < _crossings)
            {
                LeveledUp = true;
                _advance = true;
            }
            else IsComplete = true;
        }
    }
}
