using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>도약 호 하나의 해당 걸음 부분입니다. 원본 칸별 사건은 경유 시점에 계속 해제합니다.</summary>
    internal sealed class CombatLeapStepStyle : CombatStepStyle
    {
        internal sealed class Arc
        {
            private readonly float _from;
            private readonly float _to;
            private readonly float _height;

            internal Arc(CombatLeapPlan.Span span, CombatLeapMotionSettings settings, FieldGridMapper mapper)
            {
                if (span == null) throw new ArgumentNullException(nameof(span));
                if (settings == null) throw new ArgumentNullException(nameof(settings));
                if (mapper == null) throw new ArgumentNullException(nameof(mapper));
                var first = span.Steps[0].Expected;
                var last = span.Steps[span.Steps.Count - 1].Final;
                _from = mapper.SurfaceHeightOf(first.OccupiedCells, first.Anchor.Value);
                _to = mapper.SurfaceHeightOf(last.OccupiedCells, last.Anchor.Value);
                float highest = Math.Max(_from, _to);
                foreach (var step in span.Steps)
                    highest = Math.Max(highest, mapper.SurfaceHeightOf(step.Final.OccupiedCells, step.Final.Anchor.Value));
                _height = settings.Height + highest - Math.Max(_from, _to);
                Seconds = settings.Seconds / span.Steps.Count;
                Count = span.Steps.Count;
            }

            internal float Seconds { get; }
            internal int Count { get; }
            internal float HeightAt(int index, float progress) =>
                CombatStepHeight.Arc(_from, _to, (index + progress) / Count, _height);
        }

        internal CombatLeapStepStyle(CombatLeapPlan.Step step, Arc arc)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Shape = arc ?? throw new ArgumentNullException(nameof(arc));
        }

        internal CombatLeapPlan.Step Step { get; }
        internal Arc Shape { get; }
        internal override float Seconds => Shape.Seconds;
        internal override float HeightAt(float fromHeight, float toHeight, float progress) => Shape.HeightAt(Step.Index, progress);
        internal override bool TryGetFixedContact(out float progress)
        {
            progress = 1f;
            return true;
        }
        internal override float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source) => 1f;
    }
}
