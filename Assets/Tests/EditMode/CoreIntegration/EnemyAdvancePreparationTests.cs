using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using ProjectL.Systems.ServiceKeys.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyAdvancePreparationTests
    {
        [Test]
        public void OneFrameSpendsAtMostOneSlice()
        {
            using var f = new Fixture();
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Required));
            f.Service.PrepareAdvanceSlice();
            long version = f.Service.PreparationWorkVersion;
            Assert.That(f.Service.PrepareAdvanceSlice(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            Assert.That(f.Service.PreparationWorkVersion, Is.EqualTo(version));
            Assert.That(f.Service.HasPreparation, Is.True);
            Assert.Throws<InvalidOperationException>(() => f.Service.AdvancePreparedCombat());
        }

        [Test]
        public void ReadyIsVisibleOnlyNextFrame()
        {
            using var f = new Fixture();
            var buffers = f.Source.Battle.Buffers;
            buffers.CombatViewWriteSink.Submit(f.Source.Battle.EndPlayerTurn());
            var pending = buffers.CombatViewReadSink.Commands.ToArray();
            int handled = buffers.CombatStateReadSink.HandledCommandNumber;
            f.Ready();
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Ready));
            var before = f.Source.Battle.Buffers.CombatStateReadSink.LatestResultSequence;
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
            Assert.That(f.Source.Battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(before + 1));
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            Assert.That(f.Service.HasPreparation, Is.False);
            Assert.That(buffers.CombatViewReadSink.Commands, Is.EqualTo(pending));
            Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
        }

        [Test]
        public void PrepareAfterDeadlineYieldsAndRecordsAtomicOverrun()
        {
            using var f = new Fixture();
            f.TickStep = 5000;
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.PreparationWorkVersion, Is.Zero, "Capture consumes the first atom, not a full AI decision.");
            Assert.That(f.Service.AtomicOverrunCount, Is.GreaterThan(0));
            Assert.That(f.Service.LargestPreparationAtomTicks, Is.GreaterThanOrEqualTo(5000));
            f.Frame++;
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.PreparationWorkVersion, Is.EqualTo(1));
        }

        [Test]
        public void StaleDoesNotDrain()
        {
            using var f = new Fixture();
            var before = f.Source.Battle.CaptureStores();
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Stale));
            Assert.That(f.Service.HasPreparation, Is.False);
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void FreshInputCheckedBeforeExecute()
        {
            using var f = new Fixture();
            f.Ready();
            f.Source.Battle.Buffers.CurrencyWriteSink.SetAmount(999);
            var before = f.Source.Battle.CaptureStores();
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Stale));
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(f.Service.HasPreparation, Is.False);
        }

        [Test]
        public void UnchangedSourceAssembledOnlyOnceDuringPreparation()
        {
            using var f = new Fixture();
            f.Ready();
            Assert.That(f.Service.InputCaptureCount, Is.EqualTo(1));
            for (int i = 0; i < 20; i++) Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Ready));
            Assert.That(f.Service.InputCaptureCount, Is.EqualTo(1));
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
            Assert.That(f.Service.InputCaptureCount, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(37)]
        [TestCase(1000)]
        public void SynchronousDrainMatchesPreparedExecution(int ticksPerRead)
        {
            using var f = new Fixture();
            using var direct = new EnemyDecisionSourceStampTests.SourceFixture();
            f.TickStep = ticksPerRead;
            var run = f.Source.Battle.Buffers.RunProgressReadSink.RunId;
            var before = f.Source.Battle.CaptureStores();
            f.Ready();
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
            Assert.That(direct.Battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(f.Source.Battle.Buffers.RunProgressReadSink.RunId, Is.EqualTo(run));
            AssertBattleValues(f.Source.Battle.Buffers, direct.Battle.Buffers);
            f.Source.Battle.Buffers.CombatResultReadSink.TryGetLatest(out var prepared);
            direct.Battle.Buffers.CombatResultReadSink.TryGetLatest(out var immediate);
            AssertValues(prepared, immediate, "result");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExternalTerminalOrSourceChangeReleasesOnNextCleanupTick(bool terminal)
        {
            using var f = new Fixture();
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.HasPreparation, Is.True);
            if (terminal) f.Source.Battle.EndRun();
            else f.Source.Battle.Buffers.CurrencyWriteSink.SetAmount(999);
            var before = f.Source.Battle.CaptureStores();
            ((ISystemTick)f.Service).Tick(100);
            Assert.That(f.Service.HasPreparation, Is.False);
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void ShutdownReleases()
        {
            using var f = new Fixture();
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.HasPreparation, Is.True);
            ((ISystemShutdown)f.Service).Shutdown();
            Assert.That(f.Service.HasPreparation, Is.False);
        }

        [Test]
        public void CancelAndRestartCannotSpendSecondFrameBudget()
        {
            using var f = new Fixture();
            f.Service.PrepareAdvanceSlice();
            var before = f.Source.Battle.CaptureStores();
            f.Service.CancelPreparation();
            f.Service.CancelPreparation();
            Assert.That(f.Service.PrepareAdvanceSlice(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
            f.Frame++;
            Assert.That(f.Service.PrepareAdvanceSlice(), Is.EqualTo(AdvancePreparationState.Preparing));
        }

        [Test]
        public void ProductionFactoryKeepsImmediateRoute()
        {
            using var f = new EnemyDecisionSourceStampTests.SourceFixture();
            Assert.That(f.Battle.Core.CombatAdvance.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.NotRequired));
            Assert.That(f.Battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
        }

        [Test]
        public void ExplicitFactoryCompositionUsesExistingCoreGroupRegistration()
        {
            using var f = new EnemyDecisionSourceStampTests.SourceFixture();
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(f.Battle.Buffers);
            InGameCoreSystemFactory.Create(f.Definitions, usePreparedEnemyAdvance: true).ExportServices(builder);
            var built = builder.BuildSystemReader();
            var runtime = new FrameworkRuntime(built);
            try
            {
                runtime.InitData(f.Battle.Database);
                runtime.PostBindService();
                var core = runtime.GetService(InGameSystemGroupServiceKey.Core);
                Assert.That(core.CombatAdvance.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Required));
                Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                    built.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.CombatAdvance));
                Assert.That(core.CombatAdvance, Is.InstanceOf<ISystemTick>().And.InstanceOf<ISystemShutdown>());
            }
            finally { runtime.Shutdown(); }
        }

        [Test]
        public void RetryWaitsAnotherFrameAndKeepsRejectedProgress()
        {
            using var f = new Fixture(hiddenTerrain: true);
            f.Ready();
            var before = f.Source.Battle.CaptureStores();
            var diagnostics = f.Service.PreparationDiagnostics;
            int calls = diagnostics.PreviewCalls;
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.RetryPreparation));
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(f.Service.PreparationDiagnostics, Is.SameAs(diagnostics));
            Assert.That(diagnostics.RejectedAttempts, Is.EqualTo(1));
            Assert.That(diagnostics.PreviewCalls, Is.EqualTo(calls));
            Assert.That(f.Service.PrepareAdvanceSlice(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            f.Frame++;
            f.Ready();
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
        }

        [Test]
        public void ControllerStartBypassesPreparationAndDefersFollowingEnemyWork()
        {
            using var f = new Fixture(startEnemy: false);
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.NotRequired));
            Assert.That(f.Service.AdvanceCombat(), Is.True);
            Assert.That(f.Service.InputCaptureCount, Is.Zero);
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            f.Frame++;
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Required));
            Assert.Throws<InvalidOperationException>(() => f.Service.AdvanceCombat());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SourceChangeDuringCalculationNeverPublishes(bool endRun)
        {
            using var f = new Fixture();
            f.Ready();
            int sequence = f.Source.Battle.Buffers.CombatStateReadSink.LatestResultSequence;
            CoreProfiling.Install(new ExecutionProbe(() =>
            {
                if (endRun) f.Source.Battle.EndRun();
                else f.Source.Battle.Buffers.CurrencyWriteSink.SetAmount(999);
            }));
            try
            {
                Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(endRun ? PreparedAdvanceResult.Cancelled : PreparedAdvanceResult.Stale));
                Assert.That(f.Source.Battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(f.Service.HasPreparation, Is.False);
            }
            finally { CoreProfiling.Install(null); }
        }

        [Test]
        public void CalculatorFaultReleasesPreparationAndPropagates()
        {
            using var f = new Fixture();
            f.Ready();
            var before = f.Source.Battle.CaptureStores();
            CoreProfiling.Install(new ExecutionProbe(() => throw new InvalidOperationException("injected")));
            try
            {
                Assert.Throws<InvalidOperationException>(() => f.Service.AdvancePreparedCombat());
                Assert.That(f.Service.HasPreparation, Is.False);
                Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(before));
            }
            finally { CoreProfiling.Install(null); }
        }

        [Test]
        public void RunEndsDuringFreshAssemblyDoesNotCalculate()
        {
            using var f = new Fixture();
            f.Ready();
            int reads = 0;
            int calculations = 0;
            f.Source.Definitions.OnRead = () => { if (++reads == 4) f.Source.Battle.EndRun(); };
            CoreProfiling.Install(new ExecutionProbe(() => calculations++));
            try
            {
                Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Cancelled));
                Assert.That(calculations, Is.Zero);
            }
            finally { CoreProfiling.Install(null); }
        }

        [Test]
        public void PendingRoarIsDiscoveredOnceAndUsesImmediateRouteNextFrame()
        {
            using var f = new Fixture(pendingRoar: true);
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Required));
            Assert.That(f.Service.PrepareAdvanceSlice(), Is.EqualTo(AdvancePreparationState.NotRequired));
            Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
            Assert.That(f.Service.PreparationDiagnostics, Is.Null);
            f.Frame++;
            for (int i = 0; i < 20; i++)
                Assert.That(f.Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.NotRequired));
            Assert.That(f.Service.InputCaptureCount, Is.EqualTo(1));
            Assert.That(f.Service.AdvanceCombat(), Is.True);
            f.Source.Battle.Buffers.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(f.Service.HasPreparation, Is.False);
        }

        [Test]
        public void BattleEndReleasesOnCleanupTickWithoutPublication()
        {
            using var f = new Fixture();
            f.Service.PrepareAdvanceSlice();
            f.Source.Battle.Buffers.BattleWriteSink.EndBattle();
            var afterEnd = f.Source.Battle.CaptureStores();
            f.Service.Tick(0);
            Assert.That(f.Service.HasPreparation, Is.False);
            Assert.That(f.Source.Battle.CaptureStores(), Is.EqualTo(afterEnd));
        }

        [Test]
        public void StaleRejectedIdentityDoesNotLeakToReplacementSource()
        {
            using var f = new Fixture(hiddenTerrain: true);
            f.Ready();
            Assert.That(f.Service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.RetryPreparation));
            var old = f.Service.PreparationDiagnostics;
            f.Source.Battle.Buffers.CurrencyWriteSink.SetAmount(999);
            f.Service.Tick(0);
            Assert.That(f.Service.HasPreparation, Is.False);
            f.Frame++;
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.PreparationDiagnostics, Is.Not.SameAs(old));
            Assert.That(f.Service.PreparationDiagnostics.RejectedAttempts, Is.Zero);
        }

        [Test]
        public void LowFrequencyClockDoesNotRoundTheBudgetToZero()
        {
            using var f = new Fixture(timestampFrequency: 1);
            f.TickStep = 1;
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.HasPreparation, Is.True);
            f.Frame++;
            f.Service.PrepareAdvanceSlice();
            Assert.That(f.Service.PreparationWorkVersion, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidClockFrequencyIsRejected(long frequency)
        {
            using var source = new EnemyDecisionSourceStampTests.SourceFixture();
            var publication = source.Battle.Publication();
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatAdvanceService(source.Assembler, publication.Publisher,
                Array.Empty<IMovementTileRule>(), () => 0, () => 0, frequency, usePreparation: true));
        }

        private sealed class ExecutionProbe : ICoreProfiler
        {
            private Action _action;
            internal ExecutionProbe(Action action) => _action = action;
            public void Begin(CoreProfileSection section)
            {
                if (section != CoreProfileSection.EnemyActionExecution || _action == null) return;
                var action = _action;
                _action = null;
                action();
            }
            public void End(CoreProfileSection section) { }
        }

        internal static void AssertBattleValues(IBufferQuery actual, IBufferQuery expected)
        {
            AssertValues(actual.CombatStateReadSink.Current.CommandState, expected.CombatStateReadSink.Current.CommandState, "commandState");
            Assert.That(actual.CombatStateReadSink.SoulField.SameValues(expected.CombatStateReadSink.SoulField), Is.True);
            Assert.That(actual.CharacterReadSink.Characters.Count, Is.EqualTo(expected.CharacterReadSink.Characters.Count));
            foreach (var character in actual.CharacterReadSink.Characters)
            {
                Assert.That(expected.CharacterReadSink.TryGet(character.EntityId, out var other), Is.True);
                Assert.That(character.ToFacts().SameValues(other.ToFacts()), Is.True, character.EntityId.ToString());
                Assert.That(actual.CombatStateReadSink.Current.TraitSettings.For(character.ToFacts()).SameValues(
                    expected.CombatStateReadSink.Current.TraitSettings.For(other.ToFacts())), Is.True);
            }
            Assert.That(actual.CharacterReadSink.Owned.Count, Is.EqualTo(expected.CharacterReadSink.Owned.Count));
            foreach (var owned in actual.CharacterReadSink.Owned)
            {
                Assert.That(expected.CharacterReadSink.TryGetOwned(owned.Id, out var other), Is.True);
                Assert.That(owned.SameValues(other), Is.True);
                Assert.That(actual.CharacterReadSink.GetSoulCount(owned.Id), Is.EqualTo(expected.CharacterReadSink.GetSoulCount(owned.Id)));
                if (actual.CharacterReadSink.TryGetMainState(owned.Id, out var main))
                {
                    Assert.That(expected.CharacterReadSink.TryGetMainState(owned.Id, out var otherMain), Is.True);
                    Assert.That(main.SameValues(otherMain), Is.True);
                }
            }
            Assert.That(actual.ItemInventoryReadSink.Current.SameValues(expected.ItemInventoryReadSink.Current), Is.True);
            AssertValues(actual.ArtifactReadSink.Current, expected.ArtifactReadSink.Current, "artifacts");
            AssertValues(actual.CombatStateReadSink.Completion, expected.CombatStateReadSink.Completion, "completion");
            Assert.That(new[] { actual.CurrencyReadSink.Amount, actual.CurrencyReadSink.RunSpent, actual.FameReadSink.Amount,
                actual.FameReadSink.RunEarned, actual.RunProgressReadSink.EnemiesDefeated }, Is.EqualTo(new[] {
                expected.CurrencyReadSink.Amount, expected.CurrencyReadSink.RunSpent, expected.FameReadSink.Amount,
                expected.FameReadSink.RunEarned, expected.RunProgressReadSink.EnemiesDefeated }));
            Assert.That(actual.RunProgressReadSink.Outcome, Is.EqualTo(expected.RunProgressReadSink.Outcome));
        }

        // 서로 다른 실행의 불변 결과 객체는 참조 대신 공개된 값과 사건 순서를 대조한다.
        internal static void AssertValues(object actual, object expected, string path)
        {
            if (ReferenceEquals(actual, expected)) return;
            Assert.That(actual, Is.Not.Null, path);
            Assert.That(expected, Is.Not.Null, path);
            var type = actual.GetType();
            Assert.That(type, Is.EqualTo(expected.GetType()), path);
            // 두 시험 Run은 생성 때 별도 GUID를 받는다. 각 Run 내부의 동일성은 별도 검증한다.
            if (actual is RunId run)
            {
                Assert.That(run.IsEmpty, Is.False, path);
                Assert.That(((RunId)expected).IsEmpty, Is.False, path);
                return;
            }
            if (type.IsPrimitive || type.IsEnum || actual is string || actual is decimal || actual is Guid)
            {
                Assert.That(actual, Is.EqualTo(expected), path);
                return;
            }
            if (actual is IEnumerable sequence)
            {
                var left = sequence.Cast<object>().ToArray();
                var right = ((IEnumerable)expected).Cast<object>().ToArray();
                Assert.That(left.Length, Is.EqualTo(right.Length), path);
                for (int i = 0; i < left.Length; i++) AssertValues(left[i], right[i], $"{path}[{i}]");
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
            {
                var toArray = type.GetMethod("ToArray", Type.EmptyTypes);
                AssertValues(toArray.Invoke(actual, null), toArray.Invoke(expected, null), path + ".Items");
                return;
            }
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0).ToArray();
            Assert.That(properties, Is.Not.Empty, path);
            foreach (var p in properties) AssertValues(p.GetValue(actual), p.GetValue(expected), path + "." + p.Name);
        }

        internal sealed class Fixture : IDisposable
        {
            internal EnemyDecisionSourceStampTests.SourceFixture Source { get; }
            internal CombatAdvanceService Service { get; }
            internal int Frame = 10;
            internal long Timestamp;
            internal long TickStep = 1000;

            internal Fixture(bool hiddenTerrain = false, bool startEnemy = true, bool pendingRoar = false, long timestampFrequency = 1000000)
            {
                Source = new EnemyDecisionSourceStampTests.SourceFixture(hiddenTerrain, startEnemy, pendingRoar);
                var publication = Source.Battle.Publication();
                Service = new CombatAdvanceService(Source.Assembler, publication.Publisher,
                    CombatMovementRules.Validated(new IMovementTileRule[] {
                        new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() }),
                    () => Frame, () => Timestamp += TickStep, timestampFrequency, usePreparation: true);
                Service.PostBindService(Source.Reader);
            }

            internal void Ready()
            {
                for (int i = 0; i < 100000; i++)
                {
                    var state = Service.ReadPreparationState();
                    if (state == AdvancePreparationState.Ready) return;
                    Service.PrepareAdvanceSlice();
                    Assert.That(Service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.FrameDeferred));
                    Frame++;
                }
                Assert.Fail("Decision failed to make finite progress.");
            }
            public void Dispose()
            {
                Service.CancelPreparation();
                Source.Dispose();
            }
        }
    }
}
