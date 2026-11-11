using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>공통 시간표의 착지 예약을 소비합니다. 이력과 진행 시계는 상위 연출이 소유합니다.</summary>
    internal sealed class StageNodeDropPresentation
    {
        private const double EffectWaitLimit = 3d;
        private readonly StageNodeDropData _settings;
        private readonly StageNodeDropEffects _effects;
        private readonly IAudioSystemService _audio;
        private readonly HashSet<int> _landedDepths = new();
        private Drop[] _drops = Array.Empty<Drop>();
        private Vector3 _up;
        private float _duration;
        private double _lastSpawnSeconds;
        private long _generation;
        private long _epoch;

        internal StageNodeDropPresentation(StageNodeDropData settings, StageNodeDropEffects effects, IAudioSystemService audio)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings.Validate();
        }

        internal bool IsComplete { get; private set; } = true;

        internal void Prepare(Transform mapRoot, IReadOnlyList<StageNodeDropTarget> targets,
            IReadOnlyList<StageMapIntroTimeline.NodeTiming> timings, float effectiveDropSeconds)
        {
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (timings == null) throw new ArgumentNullException(nameof(timings));
            if (!float.IsFinite(effectiveDropSeconds) || effectiveDropSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(effectiveDropSeconds));
            Cancel();
            var byId = new Dictionary<int, StageNodeDropTarget>();
            foreach (StageNodeDropTarget target in targets)
            {
                if (target == null) throw new ArgumentNullException(nameof(targets));
                if (!byId.TryAdd(target.NodeId, target))
                    throw new InvalidOperationException($"[StageNodeDropPresentation] 중복 낙하 대상: {target.NodeId}");
            }
            var drops = new List<Drop>();
            foreach (StageMapIntroTimeline.NodeTiming timing in timings)
            {
                if (!_settings.Enabled || !timing.HasDrop) continue;
                if (!byId.Remove(timing.NodeId, out StageNodeDropTarget target))
                    throw new InvalidOperationException($"[StageNodeDropPresentation] 낙하 대상이 없습니다: {timing.NodeId}");
                drops.Add(new Drop(target, timing));
            }
            _drops = drops.ToArray();
            _up = mapRoot.up.normalized;
            _duration = effectiveDropSeconds;
            _lastSpawnSeconds = 0d;
            IsComplete = _drops.Length == 0;
            long epoch = _epoch;
            try
            {
                foreach (StageNodeDropTarget target in byId.Values) target.Restore();
                foreach (Drop drop in _drops) drop.Target.Hide();
                if (IsComplete) return;
                long generation = _effects.Begin(mapRoot);
                if (_epoch != epoch)
                {
                    _effects.End(generation);
                    return;
                }
                _generation = generation;
            }
            catch (Exception error)
            {
                List<Exception> errors = new() { error };
                CleanupErrors.Try(Cancel, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[StageNodeDropPresentation] 낙하 준비 실패.");
            }
        }

        internal void Tick(double elapsedSeconds)
        {
            if (IsComplete) return;
            long epoch = _epoch;
            bool allLanded = true;
            foreach (Drop drop in _drops)
            {
                if (drop.Landed) continue;
                if (elapsedSeconds < drop.Timing.DropStartSeconds)
                {
                    allLanded = false;
                    continue;
                }
                float t = _duration == 0f ? 1f :
                    (float)Math.Clamp((elapsedSeconds - drop.Timing.DropStartSeconds) / _duration, 0d, 1d);
                // 부동소수 보간 끝의 오차와 무관하게 예약 도착 때 정확히 착지합니다.
                if (elapsedSeconds >= drop.Timing.ArrivalSeconds) t = 1f;
                drop.Target.ApplyHeightOffset(_up, _settings.Height * (1f - t * t));
                drop.Target.Reveal();
                if (t < 1f)
                {
                    allLanded = false;
                    continue;
                }
                drop.Landed = true;
                drop.Target.Restore();
                _lastSpawnSeconds = elapsedSeconds;
                _effects.Play(_generation, drop.Target.GroundPose);
                if (_epoch != epoch) return;
                if (_landedDepths.Add(drop.Timing.Depth)) _audio.Play(SfxEvent.StageDeploy);
            }
            if (!allLanded) return;
            if (_effects.IsComplete(_generation))
            {
                Finish();
            }
            else if (elapsedSeconds - _lastSpawnSeconds >= EffectWaitLimit)
            {
                Debug.LogWarning("[StageNodeDropPresentation] 착지 효과가 게임 시간 3초 안에 반환되지 않아 회수합니다.");
                if (_epoch == epoch) Finish();
            }
        }

        internal void Cancel() => Finish();

        private void Finish()
        {
            long epoch = ++_epoch;
            Drop[] owned = _drops;
            long generation = _generation;
            _drops = Array.Empty<Drop>();
            _landedDepths.Clear();
            _generation = 0;
            IsComplete = false;
            List<Exception> errors = null;
            CleanupErrors.Try(() => _effects.End(generation), ref errors);
            foreach (Drop drop in owned) CleanupErrors.Try(drop.Target.Restore, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageNodeDropPresentation] 효과 반환 또는 외형 복원 실패.");
            if (_epoch == epoch) IsComplete = true;
        }

        private sealed class Drop
        {
            internal readonly StageNodeDropTarget Target;
            internal readonly StageMapIntroTimeline.NodeTiming Timing;
            internal bool Landed;
            internal Drop(StageNodeDropTarget target, StageMapIntroTimeline.NodeTiming timing)
            {
                Target = target;
                Timing = timing;
            }
        }
    }
}
