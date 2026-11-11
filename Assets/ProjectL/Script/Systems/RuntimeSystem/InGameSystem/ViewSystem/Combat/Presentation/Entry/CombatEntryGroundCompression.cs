using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>착지 시 바닥 자세와 공통 눌림 변위를 관리합니다. 시간은 Nexus가 전달하고 칸 높이 표는 바꾸지 않습니다.</summary>
    internal sealed class CombatEntryGroundCompression
    {
        private sealed class Body
        {
            internal Transform Transform;
            internal Vector3 Position;
            internal Vector3 Axis;
        }

        private readonly CombatNexusDropSettings _settings;
        private readonly List<Body> _bodies = new();

        internal CombatEntryGroundCompression(CombatNexusDropSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            settings.Validate();
        }

        internal bool IsComplete { get; private set; } = true;

        /// <summary>보드가 모두 복구된 Nexus 낙하 시작 시점에 완성 자세를 저장합니다. 착지 전에는 움직이지 않습니다.</summary>
        internal void Begin(IReadOnlyList<Transform> targets, Vector3 worldUp)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            Cancel();
            if (_settings.ImpactDepth <= 0f || _settings.ImpactDownSeconds + _settings.ImpactReturnSeconds <= 0f) return;
            var unique = new HashSet<Transform>();
            foreach (Transform target in targets)
            {
                if (target == null) throw new InvalidOperationException("[CombatEntryGroundCompression] 착지할 바닥이 사라졌습니다.");
                unique.Add(target);
            }
            foreach (Transform target in unique)
            {
                bool followsParent = false;
                foreach (Transform other in unique)
                {
                    if (target == other || !target.IsChildOf(other)) continue;
                    followsParent = true;
                    break;
                }
                if (followsParent) continue;
                _bodies.Add(new Body
                {
                    Transform = target,
                    Position = target.localPosition,
                    Axis = target.parent != null ? target.parent.InverseTransformVector(worldUp) : worldUp
                });
            }
            // 타일 없는 칸에서도 같은 변위를 받은 건물은 눌렸다가 복구합니다.
            IsComplete = false;
        }

        /// <summary>착지 후 경과 시간에 맞춰 바닥을 움직이고 건물·접촉 이펙트가 함께 쓸 월드 높이 변위를 돌려줍니다.</summary>
        internal float Tick(float landedSeconds)
        {
            if (IsComplete) return 0f;
            float elapsed = Mathf.Max(0f, landedSeconds);
            if (elapsed >= _settings.ImpactDownSeconds + _settings.ImpactReturnSeconds)
            {
                Cancel();
                return 0f;
            }
            float offset;
            if (elapsed < _settings.ImpactDownSeconds)
            {
                float t = elapsed / _settings.ImpactDownSeconds;
                offset = -_settings.ImpactDepth * (1f - (1f - t) * (1f - t));
            }
            else
            {
                float t = (elapsed - _settings.ImpactDownSeconds) / _settings.ImpactReturnSeconds;
                offset = -_settings.ImpactDepth * (1f - t * t * (3f - 2f * t));
            }
            foreach (Body body in _bodies)
            {
                if (body.Transform == null)
                    throw new InvalidOperationException("[CombatEntryGroundCompression] 눌림 중인 바닥이 사라졌습니다.");
                body.Transform.localPosition = body.Position + body.Axis * offset;
            }
            return offset;
        }

        internal void Cancel()
        {
            IsComplete = true;
            List<Exception> errors = null;
            foreach (Body body in _bodies)
            {
                CleanupErrors.Try(() =>
                {
                    if (body.Transform != null) body.Transform.localPosition = body.Position;
                }, ref errors);
            }
            _bodies.Clear();
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryGroundCompression] 착지 바닥 복구에 실패했습니다.");
        }
    }
}
