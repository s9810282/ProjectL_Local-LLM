using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>설치된 선로를 조각 순서대로 낙하시킵니다. 열차 발동과 경로는 변경하지 않습니다.</summary>
    internal sealed class CombatEntryRailwayAppearance
    {
        private const bool RailLandingSoundEnabled = false;

        private sealed class Body
        {
            internal Transform Transform;
            internal Vector3 LocalPosition;
            internal Vector3 OffsetAxis;
            internal float Delay;
            internal CombatEntryVisibility Visibility;
            internal bool RailLandingPending;
        }

        private readonly IReadOnlyList<IReadOnlyList<RailwaySegmentVisual>> _tracks;
        private readonly Transform _field;
        private readonly CombatRailwayDropSettings _settings;
        private readonly IAudioSystemService _audio;
        private readonly List<Body> _bodies = new();
        private float _elapsed;
        private int _railLandingsSinceSound;
        private bool _started;

        internal CombatEntryRailwayAppearance(IReadOnlyList<IReadOnlyList<RailwaySegmentVisual>> tracks,
            Transform field, CombatRailwayDropSettings settings, IAudioSystemService audio)
        {
            _tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            settings.Validate();
        }

        internal bool IsComplete { get; private set; } = true;

        internal void Prepare()
        {
            Cancel();
            if (!_settings.Enabled) return;
            // Factory 생성 시점에는 아직 설치 전입니다. 같은 읽기 전용 목록을 설치 이후에 읽습니다.
            var seen = new HashSet<Transform>();
            foreach (IReadOnlyList<RailwaySegmentVisual> track in _tracks)
            {
                for (int i = 0; i < track.Count; i++)
                {
                    float delay = i * _settings.SegmentGapSeconds;
                    // 미설정 바닥만 생략합니다. 설치 후 파괴된 바닥은 Collect에서 오류로 검사합니다.
                    if (track[i].Tile is not null)
                        Collect(track[i].Tile, delay, seen);
                    Collect(track[i].Rail, delay, seen, isRail: true);
                }
            }
            foreach (Body body in _bodies)
            {
                body.Visibility.Hide();
                body.Transform.localPosition = body.LocalPosition + body.OffsetAxis * _settings.Height;
            }
            _elapsed = 0f;
            IsComplete = _bodies.Count == 0;
        }

        private void Collect(Transform target, float delay, HashSet<Transform> seen, bool isRail = false)
        {
            if (target == null) throw new InvalidOperationException("[CombatEntryRailwayAppearance] 선로 조각이 사라졌습니다.");
            if (!seen.Add(target)) throw new InvalidOperationException("[CombatEntryRailwayAppearance] 선로 조각이 중복 등록됐습니다.");
            Vector3 worldAxis = _field.up;
            _bodies.Add(new Body
            {
                Transform = target,
                LocalPosition = target.localPosition,
                OffsetAxis = target.parent != null ? target.parent.InverseTransformVector(worldAxis) : worldAxis,
                Delay = delay,
                Visibility = new CombatEntryVisibility(target),
                RailLandingPending = isRail && RailLandingSoundEnabled
            });
        }

        internal void Begin()
        {
            _started = true;
            Tick(0f);
        }

        internal void Tick(float deltaTime)
        {
            if (!_started || IsComplete) return;
            _elapsed += Mathf.Max(0f, deltaTime);
            bool complete = true;
            foreach (Body body in _bodies)
            {
                if (body.Transform == null) throw new InvalidOperationException("[CombatEntryRailwayAppearance] 낙하 중인 선로가 사라졌습니다.");
                float elapsed = _elapsed - body.Delay;
                if (elapsed < 0f)
                {
                    complete = false;
                    continue;
                }
                float t = _settings.Seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / _settings.Seconds);
                body.Transform.localPosition = t >= 1f ? body.LocalPosition :
                    body.LocalPosition + body.OffsetAxis * (_settings.Height * (1f - t * t));
                body.Visibility.Restore();
                complete &= t >= 1f;
                if (t >= 1f && body.RailLandingPending)
                {
                    body.RailLandingPending = false;
                    _railLandingsSinceSound++;
                    if (_railLandingsSinceSound == 2)
                    {
                        _railLandingsSinceSound = 0;
                        _audio.Play(SfxEvent.RailDeploy);
                    }
                }
            }
            IsComplete = complete;
        }

        internal void Cancel()
        {
            _started = false;
            _railLandingsSinceSound = 0;
            IsComplete = true;
            List<Exception> errors = null;
            foreach (Body body in _bodies)
            {
                CleanupErrors.Try(() =>
                {
                    if (body.Transform != null) body.Transform.localPosition = body.LocalPosition;
                }, ref errors);
                CleanupErrors.Try(body.Visibility.Restore, ref errors);
            }
            _bodies.Clear();
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryRailwayAppearance] 선로 자세·표시 복구에 실패했습니다.");
        }
    }
}
