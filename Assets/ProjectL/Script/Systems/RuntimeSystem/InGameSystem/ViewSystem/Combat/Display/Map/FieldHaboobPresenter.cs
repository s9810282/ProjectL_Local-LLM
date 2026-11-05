using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 하붑 구름 표시입니다(하붑 View 계획 HP-15·HP-17). 구름 칸마다 연기 이펙트 하나를 두고, 칸이 구름에서 빠지면 그 연기를 거둡니다. 어느 칸인지는 Core 상태가 원본이고,
    /// 사건 재생이 사건 시점에 맞춘 표시를 결과 끝 <see cref="Render"/>가 Buffer 값으로 다시 맞춥니다. 칸이 곧 외형이라 맞춤 모양도 그대로 보입니다.
    /// </summary>
    /// <remarks>
    /// 재생 취소로 창구가 이펙트를 모두 멈췄으면 다음 <see cref="Show"/>가 다시 냅니다. 규칙은 칸이 바뀌는 순간 바뀌고, 진입·재연결 때 연기는 천천히 찹니다(계획 HP-21).
    /// 이동은 연기를 거두지 않고 그대로 새 칸으로 옮깁니다(설계 §9.4). 진행은 사건 재생의 조각이 맡고, 재생 중에는 <see cref="Render"/>가 불리지 않습니다.
    /// </remarks>
    internal sealed class FieldHaboobPresenter : ICombatHaboobField
    {
        private readonly CombatEffectWindow _effects;
        private readonly Dictionary<CellCoord, CombatEffectHandle> _clouds = new();
        private readonly HashSet<CellCoord> _wanted = new();
        private readonly List<CellCoord> _gone = new();
        private readonly List<CloudMove> _moves = new();
        private bool _isReleased;

        /// <summary>하붑 표현 설정입니다.</summary>
        public HaboobVisualData Settings { get; }

        internal FieldHaboobPresenter(HaboobVisualData settings, CombatEffectWindow effects)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        /// <summary>진입·결과 끝에 Core 상태로 표시를 다시 맞춥니다. 재시작·재연결에도 지금 칸에 연기가 다시 찹니다. 상태가 없으면 끕니다.</summary>
        internal void Render(StageGimmickStateSet gimmicks, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            IReadOnlyList<StageGimmickState> entries = gimmicks.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is HaboobGimmickState haboob)
                {
                    Show(haboob.Cells, mapper);
                    return;
                }
            }
            Clear();
        }

        public void Show(IReadOnlyList<CellCoord> cells, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            _wanted.Clear();
            for (int i = 0; i < cells.Count; i++)
                _wanted.Add(cells[i]);

            _gone.Clear();
            foreach (KeyValuePair<CellCoord, CombatEffectHandle> cloud in _clouds)
                if (!_wanted.Contains(cloud.Key) || !cloud.Value.IsLive)
                    _gone.Add(cloud.Key);
            for (int i = 0; i < _gone.Count; i++)
            {
                _clouds[_gone[i]].Recall();
                _clouds.Remove(_gone[i]);
            }

            for (int i = 0; i < cells.Count; i++)
                if (!_clouds.ContainsKey(cells[i]))
                    _clouds.Add(cells[i], PlayCloud(cells[i], mapper));
        }

        public bool BeginMove(IReadOnlyList<CellCoord> from, IReadOnlyList<CellCoord> to, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            _moves.Clear();
            // Core는 같은 모양을 평행 이동해 순서·개수가 같습니다(계획 HP-22). 개수가 다르면 짝을 지을 수 없어 바로 맞춥니다.
            if (from.Count != to.Count)
            {
                Show(to, mapper);
                return false;
            }

            // 옛 칸과 새 칸이 겹칠 수 있어, 표에서 모두 뺀 뒤 새 칸 이름으로 다시 넣습니다.
            _gone.Clear();
            for (int i = 0; i < from.Count; i++)
            {
                if (_clouds.Remove(from[i], out CombatEffectHandle cloud) && cloud.IsLive)
                    _moves.Add(new CloudMove(cloud, to[i], CloudPose(from[i], mapper).position, CloudPose(to[i], mapper).position));
                else
                    _gone.Add(to[i]);
            }
            Clear();
            for (int i = 0; i < _moves.Count; i++)
                _clouds.Add(_moves[i].Cell, _moves[i].Cloud);
            // 재생 취소로 멈춘 칸은 옮길 연기가 없어 도착 칸에 다시 냅니다(계획 HP-25).
            for (int i = 0; i < _gone.Count; i++)
                _clouds.Add(_gone[i], PlayCloud(_gone[i], mapper));
            return _moves.Count != 0;
        }

        public void TickMove(float progress)
        {
            for (int i = 0; i < _moves.Count; i++)
                _moves[i].Cloud.MoveTo(Vector3.Lerp(_moves[i].From, _moves[i].To, progress));
        }

        /// <summary>연기를 모두 거둡니다. 전투가 바뀌면 이 경로로 지난 전투의 구름이 남지 않습니다.</summary>
        internal void Clear()
        {
            foreach (CombatEffectHandle cloud in _clouds.Values)
                cloud.Recall();
            _clouds.Clear();
        }

        internal void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            Clear();
        }

        private CombatEffectHandle PlayCloud(CellCoord cell, FieldGridMapper mapper)
        {
            return _effects.Play(Settings.CloudEffect.Effect, CloudPose(cell, mapper), CombatEffectPlacement.Stay.AsHeld());
        }

        private Pose CloudPose(CellCoord cell, FieldGridMapper mapper)
        {
            CombatHeldEffect effect = Settings.CloudEffect;
            return CombatCuePoints.Apply(CombatCuePoints.CellPose(mapper, cell), effect.PositionOffset, effect.RotationOffset);
        }

        private void ThrowIfReleased()
        {
            if (_isReleased) throw new InvalidOperationException("[FieldHaboobPresenter] 해제된 표시입니다.");
        }

        // 옮기는 연기 하나입니다. 자리는 시작할 때 한 번 구합니다.
        private readonly struct CloudMove
        {
            internal CloudMove(CombatEffectHandle cloud, CellCoord cell, Vector3 from, Vector3 to)
            {
                Cloud = cloud;
                Cell = cell;
                From = from;
                To = to;
            }

            internal CombatEffectHandle Cloud { get; }
            internal CellCoord Cell { get; }
            internal Vector3 From { get; }
            internal Vector3 To { get; }
        }
    }
}
