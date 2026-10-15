using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions
{
    internal readonly struct TraitStateChange
    {
        public bool HasChange { get; }
        public bool HasActivation { get; }
        public TraitActivationKind ActivationKind { get; }
        public int ActivationMagnitude { get; }
        public TraitKey TraitKey { get; }
        public EntityId? ActivationTargetId { get; }
        public TraitRuntimeState FinalState { get; }

        private TraitStateChange(bool hasChange, bool hasActivation, TraitActivationKind activationKind, int magnitude, TraitKey key, EntityId? targetId, TraitRuntimeState state)
        {
            HasChange = hasChange;
            HasActivation = hasActivation;
            ActivationKind = activationKind;
            ActivationMagnitude = magnitude;
            TraitKey = key;
            ActivationTargetId = targetId;
            FinalState = state;
        }

        public static TraitStateChange Change(TraitKey key, TraitRuntimeState state)
        {
            ValidateChange(key, state);

            return new TraitStateChange(true, false, TraitActivationKind.None, 0, key, null, state.Clone());
        }

        public static TraitStateChange Change(TraitKey key, TraitRuntimeState state, TraitActivationKind activationKind, int magnitude, EntityId? targetId)
        {
            ValidateChange(key, state);
            if (activationKind == TraitActivationKind.None ||
                Enum.IsDefined(typeof(TraitActivationKind), activationKind) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(activationKind));
            }
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (targetId.HasValue && targetId.Value == EntityId.Empty)
                throw new ArgumentException("발동 대상 ID는 비어 있을 수 없습니다.", nameof(targetId));

            return new TraitStateChange(true, true, activationKind, magnitude, key, targetId, state.Clone());
        }

        public static TraitStateChange NoChange(TraitKey key)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            return new TraitStateChange(false, false, TraitActivationKind.None, 0, key, null, null);
        }

        private static void ValidateChange(TraitKey key, TraitRuntimeState state)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.TraitKey != key)
                throw new ArgumentException("특성 키값이 최종 반영될 특성 키 값과 다릅니다.", nameof(state));
        }
    }
}
