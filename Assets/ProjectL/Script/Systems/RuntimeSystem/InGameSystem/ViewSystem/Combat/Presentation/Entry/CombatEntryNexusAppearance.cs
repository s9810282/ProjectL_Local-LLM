using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>첫 적 Nexus의 자세를 빌려 낙하시키고, 지면 착지 이펙트가 끝날 때까지(착지 뒤 최대 <see cref="ImpactWaitLimitSeconds"/>초) 기다립니다.</summary>
    internal sealed class CombatEntryNexusAppearance
    {
        // 착지 이펙트는 자연 끝(Stay)이라 시간 상한이 없다. 프리팹이 Looping이거나 끝 알림이 빠지면 진입이 영원히 기다리므로,
        // 착지 뒤 이 시간이 지나면 경고를 한 번 남기고 기다림을 끝낸다(평가 C10-03). 남은 이펙트는 진입이 끝날 때(적 등장 뒤 Complete → Cancel) 거둔다.
        private const float ImpactWaitLimitSeconds = 3f;

        private sealed class Target
        {
            internal FieldUnitView View;
            internal EntityId Id;
            internal CombatEntryVisibility Visibility;
            internal ICombatUnitPose Pose;
            internal CombatEffectHandle Impact;
            internal bool Landed;
        }

        private readonly CombatUnitPresentationWindow _window;
        private readonly CombatEffectWindow _effects;
        private readonly CombatNexusDropSettings _settings;
        private readonly List<Target> _targets = new();
        private Vector3 _up;
        private Quaternion _impactRotation;
        private float _elapsed;
        private int _generation;
        private bool _begun;
        private bool _unavailable;

        internal CombatEntryNexusAppearance(CombatUnitPresentationWindow window, CombatEffectWindow effects,
            CombatNexusDropSettings settings)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            settings.Validate();
        }

        internal bool IsComplete { get; private set; } = true;

        internal void Prepare(IReadOnlyList<FieldUnitView> views, FieldGridMapper mapper)
        {
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Cancel();
            if (!_settings.Enabled) return;
            _up = mapper.WorldUp.normalized;
            _impactRotation = mapper.RotationToWorld(Rotation4.R0);
            foreach (FieldUnitView view in views)
            {
                if (view == null) throw new InvalidOperationException("[CombatEntryNexusAppearance] Nexus 표시가 없습니다.");
                var target = new Target { View = view, Id = view.EntityId, Visibility = new CombatEntryVisibility(view.transform) };
                _targets.Add(target);
                target.Visibility.Hide();
                view.SetEntryHudHidden(true);
            }
            IsComplete = _targets.Count == 0;
        }

        internal void Begin()
        {
            if (IsComplete) return;
            if (_begun) throw new InvalidOperationException("[CombatEntryNexusAppearance] 낙하가 이미 시작됐습니다.");
            int generation = _generation;
            // 모두 빌린 뒤에 표시합니다. 중간 실패도 상위 진입 취소가 앞선 대여를 반환합니다.
            foreach (Target target in _targets)
            {
                RequireView(target);
                if (!_window.TryBeginPose(target.Id, () =>
                    {
                        if (_generation == generation) _unavailable = true;
                    }, out ICombatUnitPose pose))
                    throw new InvalidOperationException($"[CombatEntryNexusAppearance] Nexus 자세를 빌릴 수 없습니다: {target.Id}");
                target.Pose = pose;
            }
            foreach (Target target in _targets)
            {
                target.Pose.ApplyPose(target.Pose.Position + _up * _settings.Height, target.Pose.Rotation);
                target.Visibility.Restore();
            }
            _begun = true;
            Tick(0f);
        }

        internal void Tick(float deltaTime)
        {
            if (IsComplete) return;
            if (!_begun) throw new InvalidOperationException("[CombatEntryNexusAppearance] Begin이 먼저 필요합니다.");
            if (_unavailable) throw new InvalidOperationException("[CombatEntryNexusAppearance] Nexus 자세 대여가 취소됐습니다.");
            _elapsed += Mathf.Max(0f, deltaTime);
            float t = _settings.Seconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / _settings.Seconds);
            bool impactWaitOver = _elapsed - Mathf.Max(0f, _settings.Seconds) >= ImpactWaitLimitSeconds;
            bool cutImpact = false;
            bool complete = true;
            foreach (Target target in _targets)
            {
                RequireView(target);
                if (!target.Landed)
                {
                    ICombatUnitPose pose = target.Pose;
                    if (!pose.IsAvailable) throw new InvalidOperationException($"[CombatEntryNexusAppearance] Nexus 자세를 사용할 수 없습니다: {target.Id}");
                    pose.ApplyPose(pose.Position + _up * (_settings.Height * (1f - t * t)), pose.Rotation);
                    if (t >= 1f)
                    {
                        // 원래 배치는 점유 칸의 지면 중심입니다. 공중 위치나 모델 중심에 이펙트를 놓지 않습니다.
                        var impactAt = new Pose(pose.Position, _impactRotation);
                        pose.RestorePose();
                        target.Pose = null;
                        target.Landed = true;
                        target.View.SetEntryHudHidden(false);
                        target.Impact = _effects.Play(EffectType.FallingRocks, impactAt, CombatEffectPlacement.Stay);
                    }
                }
                bool impactLive = target.Landed && target.Impact.IsLive;
                cutImpact |= impactLive && impactWaitOver;
                complete &= target.Landed && (!impactLive || impactWaitOver);
            }
            // 완료되면 다음 Tick은 첫 줄에서 돌아가므로 이 경고는 진입마다 한 번만 남는다.
            if (complete && cutImpact)
                Debug.LogWarning($"[CombatEntryNexusAppearance] 착지 이펙트가 착지 뒤 {ImpactWaitLimitSeconds}초 안에 끝나지 않아 기다림을 끝냅니다. " +
                    "FallingRocks 프리팹이 Looping인지, 끝 알림(ParticleStopNotifier)이 빠졌는지 확인하세요.");
            IsComplete = complete;
        }

        internal void Cancel()
        {
            _generation++;
            IsComplete = true;
            List<Exception> errors = null;
            foreach (Target target in _targets)
            {
                if (target.Pose != null) CleanupErrors.Try(target.Pose.RestorePose, ref errors);
                CleanupErrors.Try(target.Impact.RecallImmediately, ref errors);
                // 풀에서 다른 개체로 재사용된 표시에는 지난 숨김 상태를 덮어쓰지 않습니다.
                if (target.View == null || target.View.EntityId != target.Id) continue;
                CleanupErrors.Try(target.Visibility.Restore, ref errors);
                CleanupErrors.Try(() => target.View.SetEntryHudHidden(false), ref errors);
            }
            _targets.Clear();
            _elapsed = 0f;
            _begun = false;
            _unavailable = false;
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryNexusAppearance] Nexus 낙하 정리에 실패했습니다.");
        }

        private static void RequireView(Target target)
        {
            if (target.View == null || target.View.EntityId != target.Id || !target.View.gameObject.activeInHierarchy)
                throw new InvalidOperationException($"[CombatEntryNexusAppearance] 준비한 Nexus 표시가 사라졌습니다: {target.Id}");
        }
    }
}
