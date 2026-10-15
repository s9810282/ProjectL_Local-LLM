using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions
{
    /// <summary>
    /// 한 평가 단계에서 사용한 상태 기대값, 상태 변경과 발동 결과를 합친 계획이다.
    /// </summary>
    internal sealed class CombatEffectPlan
    {
        public IReadOnlyList<EffectStateExpectation> Expectations { get; }
        public IReadOnlyList<EffectStateChange> Changes { get; }
        public IReadOnlyList<EffectActivation> Activations { get; }

        public CombatEffectPlan(
            EffectStateExpectation[] expectations,
            EffectStateChange[] changes,
            EffectActivation[] activations)
        {
            EffectStateExpectation[] copiedExpectations = CopyExpectations(expectations);
            var expectationsByOrigin = new Dictionary<EffectOriginKey, EffectStateExpectation>(
                copiedExpectations.Length);
            foreach (EffectStateExpectation expectation in copiedExpectations)
                expectationsByOrigin.Add(expectation.Origin, expectation);

            EffectStateChange[] copiedChanges = CopyAndValidateChanges(changes, expectationsByOrigin);
            EffectActivation[] copiedActivations = CopyAndValidateActivations(
                activations,
                expectationsByOrigin);

            Expectations = Array.AsReadOnly(copiedExpectations);
            Changes = Array.AsReadOnly(copiedChanges);
            Activations = Array.AsReadOnly(copiedActivations);
        }

        private static EffectStateExpectation[] CopyExpectations(
            EffectStateExpectation[] expectations)
        {
            if (expectations == null) throw new ArgumentNullException(nameof(expectations));

            var copied = (EffectStateExpectation[])expectations.Clone();
            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectStateExpectation expectation in copied)
            {
                if (expectation.Origin.IsValid == false || expectation.ExpectedState == null)
                    throw new ArgumentException("Invalid state expectation", nameof(expectations));
                if (seen.Add(expectation.Origin) == false)
                    throw new ArgumentException("Duplicate expectation origin", nameof(expectations));
            }

            return copied;
        }

        private static EffectStateChange[] CopyAndValidateChanges(
            EffectStateChange[] changes,
            IReadOnlyDictionary<EffectOriginKey, EffectStateExpectation> expectationsByOrigin)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));

            var copied = (EffectStateChange[])changes.Clone();
            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            var seen = new HashSet<EffectOriginKey>();
            foreach (EffectStateChange change in copied)
            {
                if (change.Origin.IsValid == false ||
                    change.ExpectedState == null ||
                    change.FinalState == null)
                {
                    throw new ArgumentException("Invalid state change", nameof(changes));
                }

                if (seen.Add(change.Origin) == false)
                    throw new ArgumentException("Duplicate change origin", nameof(changes));
                if (expectationsByOrigin.TryGetValue(change.Origin, out EffectStateExpectation expectation) == false)
                    throw new ArgumentException("Change origin has no expectation", nameof(changes));

                Type expectedType = expectation.ExpectedState.GetType();
                if (change.ExpectedState.GetType() != expectedType ||
                    expectation.ExpectedState.ValueEquals(change.ExpectedState) == false)
                {
                    throw new ArgumentException("Change expected state does not match expectation", nameof(changes));
                }

                if (change.FinalState.GetType() != expectedType)
                    throw new ArgumentException("Change final state type does not match expectation", nameof(changes));
            }

            return copied;
        }

        private static EffectActivation[] CopyAndValidateActivations(
            EffectActivation[] activations,
            IReadOnlyDictionary<EffectOriginKey, EffectStateExpectation> expectationsByOrigin)
        {
            if (activations == null) throw new ArgumentNullException(nameof(activations));

            var indexed = new IndexedActivation[activations.Length];
            for (int index = 0; index < activations.Length; index++)
            {
                EffectActivation activation = activations[index];
                if (activation.Origin.IsValid == false)
                    throw new ArgumentException("Invalid activation origin", nameof(activations));
                if (expectationsByOrigin.ContainsKey(activation.Origin) == false)
                    throw new ArgumentException("Activation origin has no expectation", nameof(activations));

                indexed[index] = new IndexedActivation(activation, index);
            }

            Array.Sort(indexed, (left, right) =>
            {
                int comparison = left.Activation.Origin.CompareTo(right.Activation.Origin);
                return comparison != 0 ? comparison : left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            var copied = new EffectActivation[indexed.Length];
            for (int index = 0; index < indexed.Length; index++)
                copied[index] = indexed[index].Activation;

            return copied;
        }

        private readonly struct IndexedActivation
        {
            public EffectActivation Activation { get; }
            public int OriginalIndex { get; }

            public IndexedActivation(EffectActivation activation, int originalIndex)
            {
                Activation = activation;
                OriginalIndex = originalIndex;
            }
        }
    }
}
