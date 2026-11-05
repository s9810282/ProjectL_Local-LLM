using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>완성된 타일과 지형에 높이 변위만 더합니다. 칸 높이 표와 활성 상태는 건드리지 않습니다.</summary>
    internal sealed class CombatBoardReveal
    {
        private sealed class Body
        {
            internal Transform Transform;
            internal Vector3 LocalPosition;
            internal Vector3 OffsetAxis;
            internal float Delay;
            internal CombatEntryVisibility Visibility;
        }

        private readonly Transform _field;
        private readonly CombatBoardRevealSettings _settings;
        private readonly List<Body> _bodies = new();
        private float _elapsed;

        internal CombatBoardReveal(Transform field, CombatBoardRevealSettings settings)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            settings.Validate();
        }

        internal bool IsComplete { get; private set; } = true;

        internal void Prepare(IReadOnlyList<CombatBoardRevealTarget> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            Cancel();
            float[] delays = CombatBoardRevealSchedule.Create(_field, targets, _settings);
            var seen = new HashSet<Transform>();
            Vector3 worldAxis = _field.TransformVector(Vector3.up);
            // 검증과 완성 자세 수집을 모두 마친 다음에만 외형을 움직입니다.
            for (int i = 0; i < targets.Count; i++)
            {
                Transform body = targets[i].Body;
                if (body == null) throw new InvalidOperationException("[CombatBoardReveal] 준비할 외형이 사라졌습니다.");
                if (!seen.Add(body)) throw new InvalidOperationException($"[CombatBoardReveal] 외형이 중복 등록됐습니다: {body.name}");
                _bodies.Add(new Body
                {
                    Transform = body,
                    LocalPosition = body.localPosition,
                    OffsetAxis = body.parent != null ? body.parent.InverseTransformVector(worldAxis) : worldAxis,
                    Delay = delays[i],
                    Visibility = new CombatEntryVisibility(body)
                });
            }
            foreach (Body body in _bodies)
            {
                body.Visibility.Hide();
                body.Transform.localPosition = body.LocalPosition - body.OffsetAxis * _settings.Depth;
            }
            _elapsed = 0f;
            IsComplete = _bodies.Count == 0;
        }

        internal void Tick(float deltaTime)
        {
            if (IsComplete) return;
            _elapsed += Mathf.Max(0f, deltaTime);
            bool complete = true;
            foreach (Body body in _bodies)
            {
                if (body.Transform == null) throw new InvalidOperationException("[CombatBoardReveal] 진행 중인 외형이 사라졌습니다.");
                float elapsed = _elapsed - body.Delay;
                if (elapsed < 0f)
                {
                    complete = false;
                    continue;
                }
                float t = _settings.TileSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / _settings.TileSeconds);
                // 앞 75%에서 정점에 닿고, 나머지에서 완성 높이로 부드럽게 내려앉습니다.
                float height;
                if (t < .75f)
                {
                    float rise = t / .75f;
                    float eased = 1f - (1f - rise) * (1f - rise);
                    height = Mathf.Lerp(-_settings.Depth, _settings.Overshoot, eased);
                }
                else
                {
                    float settle = (t - .75f) / .25f;
                    height = _settings.Overshoot * (1f - settle * settle * (3f - 2f * settle));
                }
                body.Transform.localPosition = t >= 1f ? body.LocalPosition : body.LocalPosition + body.OffsetAxis * height;
                body.Visibility.Restore();
                complete &= t >= 1f;
            }
            IsComplete = complete;
        }

        internal void Cancel()
        {
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
            CleanupErrors.ThrowIfAny(errors, "[CombatBoardReveal] 보드 자세·표시 복구에 실패했습니다.");
        }
    }
}
