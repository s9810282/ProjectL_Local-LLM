using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>고정된 연결선의 노출 진행률만 전달합니다. 메시와 Renderer는 대상 View가 소유합니다.</summary>
    internal sealed class StageLinkDrawPresentation
    {
        private Draw[] _draws = Array.Empty<Draw>();
        private long _epoch;
        internal bool IsComplete { get; private set; } = true;

        internal void Prepare(IReadOnlyList<IStageLinkDrawTarget> targets,
            IReadOnlyList<StageMapIntroTimeline.LinkTiming> timings)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (timings == null) throw new ArgumentNullException(nameof(timings));
            Cancel();
            var byId = new Dictionary<(int, int), IStageLinkDrawTarget>();
            foreach (IStageLinkDrawTarget target in targets)
            {
                if (target == null) throw new ArgumentNullException(nameof(targets));
                if (!byId.TryAdd((target.FromNodeId, target.ToNodeId), target))
                    throw new InvalidOperationException($"[StageLinkDrawPresentation] 연결선이 중복됩니다: {target.FromNodeId} → {target.ToNodeId}");
            }
            var draws = new Draw[timings.Count];
            for (int i = 0; i < timings.Count; i++)
            {
                StageMapIntroTimeline.LinkTiming timing = timings[i];
                if (!byId.Remove((timing.FromNodeId, timing.ToNodeId), out IStageLinkDrawTarget target))
                    throw new InvalidOperationException($"[StageLinkDrawPresentation] 연결선이 없습니다: {timing.FromNodeId} → {timing.ToNodeId}");
                draws[i] = new Draw(target, timing);
            }
            _draws = draws;
            IsComplete = draws.Length == 0;
            try
            {
                foreach (IStageLinkDrawTarget target in byId.Values) target.Restore();
                foreach (Draw draw in draws) draw.Target.Prepare();
            }
            catch (Exception error)
            {
                List<Exception> errors = new() { error };
                CleanupErrors.Try(Cancel, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[StageLinkDrawPresentation] 선 준비 실패.");
            }
        }

        internal void Tick(double elapsedSeconds)
        {
            if (IsComplete) return;
            long epoch = _epoch;
            bool complete = true;
            foreach (Draw draw in _draws)
            {
                if (draw.Complete) continue;
                double span = draw.Timing.EndSeconds - draw.Timing.StartSeconds;
                float progress = (float)Math.Clamp((elapsedSeconds - draw.Timing.StartSeconds) / span, 0d, 1d);
                draw.Target.SetProgress(progress);
                if (_epoch != epoch) return;
                draw.Complete = progress >= 1f;
                complete &= draw.Complete;
            }
            IsComplete = complete;
        }

        internal void Cancel()
        {
            ++_epoch;
            Draw[] owned = _draws;
            _draws = Array.Empty<Draw>();
            IsComplete = true;
            List<Exception> errors = null;
            foreach (Draw draw in owned) CleanupErrors.Try(draw.Target.Restore, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageLinkDrawPresentation] 선 복원 실패.");
        }

        private sealed class Draw
        {
            internal readonly IStageLinkDrawTarget Target;
            internal readonly StageMapIntroTimeline.LinkTiming Timing;
            internal bool Complete;
            internal Draw(IStageLinkDrawTarget target, StageMapIntroTimeline.LinkTiming timing)
            {
                Target = target;
                Timing = timing;
            }
        }
    }
}
