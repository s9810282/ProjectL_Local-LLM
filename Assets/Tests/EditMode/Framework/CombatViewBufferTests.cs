using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatViewBufferTests
    {
        private static readonly EntityId Unit = new(7);
        private static readonly CellCoord Target = new(3, 4);
        private static readonly MovementPath Path = new(new[] { Target + CellCoord.Left, Target });
        private static readonly MovementPreviewContext Context = new(1, Owner.Enemy, CharacterBufferTestData.EnemyFacts(Unit),
            CombatCommandKind.Move, Path);

        [Test]
        public void Registration_ExposesOneBufferThroughReadAndWriteSinks()
        {
            var buffers = CreateBuffers();

            Assert.That(buffers.CombatViewReadSink, Is.Not.Null);
            Assert.That(buffers.CombatViewWriteSink, Is.Not.Null);
            Assert.That(buffers.CombatViewReadSink, Is.SameAs(buffers.CombatViewWriteSink));
        }

        [Test]
        public void SubmitAndSetPreview_ShareOneIncreasingCounter()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            var read = buffers.CombatViewReadSink;

            Assert.That(write.Submit(CombatViewCommandData.Move(Unit, Path, Context)), Is.EqualTo(1));
            Assert.That(write.SetPreview(CombatViewPreviewData.MoveRange(Unit)), Is.EqualTo(2));
            Assert.That(write.Submit(CombatViewCommandData.EndUnitTurn()), Is.EqualTo(3));

            Assert.That(read.Commands.Count, Is.EqualTo(2));
            Assert.That(read.Commands[0].Number, Is.EqualTo(1));
            Assert.That(read.Commands[1].Number, Is.EqualTo(3));
            Assert.That(read.Preview.Number, Is.EqualTo(2));
        }

        [Test]
        public void Clear_RemovesInputsButKeepsCounterAndPresentedSequence()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            var read = buffers.CombatViewReadSink;
            write.Submit(CombatViewCommandData.EnterCombat());
            write.SetPreview(CombatViewPreviewData.AttackRange(Unit));
            write.AcknowledgePresented(5);

            write.Clear();

            Assert.That(read.Commands, Is.Empty);
            Assert.That(read.HasPreview, Is.False);
            Assert.That(read.PresentedResultSequence, Is.EqualTo(5));
            Assert.That(write.Submit(CombatViewCommandData.DrawCharacter()), Is.EqualTo(3));
        }

        [Test]
        public void InvalidValue_IsRejectedWithoutConsumingNumber()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;

            Assert.Throws<ArgumentException>(() => write.Submit(default));
            Assert.Throws<ArgumentException>(() => write.SetPreview(default));

            Assert.That(write.Submit(CombatViewCommandData.EndUnitTurn()), Is.EqualTo(1));
            Assert.That(buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(buffers.CombatViewReadSink.HasPreview, Is.False);
        }

        [Test]
        public void TryGetNextCommand_ReturnsFirstCommandAfterHandledNumber()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            var read = buffers.CombatViewReadSink;
            write.Submit(CombatViewCommandData.Move(Unit, Path, Context));
            write.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            write.Submit(CombatViewCommandData.EndUnitTurn());

            Assert.That(read.TryGetNextCommand(0, out var first), Is.True);
            Assert.That(first.Number, Is.EqualTo(1));
            Assert.That(read.TryGetNextCommand(1, out var second), Is.True);
            Assert.That(second.Number, Is.EqualTo(3));
            Assert.That(second.Data.Kind, Is.EqualTo(CombatViewCommandKind.EndUnitTurn));
            Assert.That(read.TryGetNextCommand(3, out var none), Is.False);
            Assert.That(none.Number, Is.Zero);
        }

        [Test]
        public void RemoveCommandsThrough_RemovesOnlyHandledCommands()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            var read = buffers.CombatViewReadSink;
            write.Submit(CombatViewCommandData.Move(Unit, Path, Context));
            write.Submit(CombatViewCommandData.EndUnitTurn());
            write.Submit(CombatViewCommandData.DrawCharacter());

            write.RemoveCommandsThrough(2);

            Assert.That(read.Commands.Count, Is.EqualTo(1));
            Assert.That(read.Commands[0].Number, Is.EqualTo(3));

            // 이미 지운 범위를 다시 알려도 남은 명령은 그대로입니다.
            write.RemoveCommandsThrough(2);
            write.RemoveCommandsThrough(0);
            Assert.That(read.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveCommandsThrough_RejectsNegativeOrUnissuedNumber()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            write.Submit(CombatViewCommandData.EnterCombat());

            Assert.Throws<ArgumentOutOfRangeException>(() => write.RemoveCommandsThrough(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => write.RemoveCommandsThrough(2));
            Assert.That(buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void Preview_IsReplacedByNewerRequestAndCanBeCleared()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;
            var read = buffers.CombatViewReadSink;

            write.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            int latest = write.SetPreview(CombatViewPreviewData.Move(Unit, Path));

            Assert.That(read.HasPreview, Is.True);
            Assert.That(read.Preview.Number, Is.EqualTo(latest));
            Assert.That(read.Preview.Data.Kind, Is.EqualTo(CombatViewPreviewKind.Move));
            Assert.That(read.Preview.Data.Path, Is.SameAs(Path));

            write.ClearPreview();

            Assert.That(read.HasPreview, Is.False);
            Assert.That(read.Preview.Number, Is.Zero);
        }

        [Test]
        public void AcknowledgePresented_AllowsSameValueAndRejectsDecrease()
        {
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;

            write.AcknowledgePresented(3);
            write.AcknowledgePresented(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => write.AcknowledgePresented(2));
            Assert.That(buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(3));
        }

        [Test]
        public void Commands_CannotBeModifiedThroughReadSink()
        {
            var buffers = CreateBuffers();
            buffers.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
            var commands = (ICollection<CombatViewCommand>)buffers.CombatViewReadSink.Commands;

            Assert.Throws<NotSupportedException>(() => commands.Clear());
            Assert.That(buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void DataFactories_KeepOnlySelectedValues()
        {
            var move = CombatViewCommandData.Move(Unit, Path, Context);
            var attack = CombatViewCommandData.Attack(Unit, AttackAim.ForCell(Target));
            var place = CombatViewPreviewData.UnitPlacement(Unit, Target, Rotation4.R90);

            Assert.That(move.Kind, Is.EqualTo(CombatViewCommandKind.Move));
            Assert.That(move.EntityId, Is.EqualTo(Unit));
            Assert.That(move.Path, Is.SameAs(Path));
            Assert.That(move.MovementContext, Is.SameAs(Context));
            Assert.That(attack.Kind, Is.EqualTo(CombatViewCommandKind.Attack));
            Assert.That(attack.Aim.Kind, Is.EqualTo(AttackAimKind.Cell));
            Assert.That(attack.Aim.Cell, Is.EqualTo(Target));
            Assert.That(place.Kind, Is.EqualTo(CombatViewPreviewKind.UnitPlacement));
            Assert.That(place.Rotation, Is.EqualTo(Rotation4.R90));
        }

        [TestCase(AttackAimKind.Cell)]
        [TestCase(AttackAimKind.Direction)]
        [TestCase(AttackAimKind.All)]
        public void AttackInput_PreservesExplicitChoiceThroughCommandAndPreview(AttackAimKind kind)
        {
            AttackAim aim = kind == AttackAimKind.Cell ? AttackAim.ForCell(Target)
                : kind == AttackAimKind.Direction ? AttackAim.ForDirection(AttackDirection.Up)
                : AttackAim.ForAll();
            var buffers = CreateBuffers();
            int commandNumber = buffers.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(Unit, aim));
            int previewNumber = buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(Unit, aim));

            Assert.That(commandNumber, Is.EqualTo(1));
            Assert.That(previewNumber, Is.EqualTo(2));
            Assert.That(buffers.CombatViewReadSink.Commands[0].Data.EntityId, Is.EqualTo(Unit));
            Assert.That(buffers.CombatViewReadSink.Preview.Data.EntityId, Is.EqualTo(Unit));
            foreach (AttackAim stored in new[]
                     {
                         buffers.CombatViewReadSink.Commands[0].Data.Aim,
                         buffers.CombatViewReadSink.Preview.Data.Aim
                     })
            {
                Assert.That(stored.Kind, Is.EqualTo(kind));
                Assert.That(stored.Cell, Is.EqualTo(kind == AttackAimKind.Cell ? (CellCoord?)Target : null));
                Assert.That(stored.Direction,
                    Is.EqualTo(kind == AttackAimKind.Direction ? AttackDirection.Up : AttackDirection.None));
            }
        }

        [Test]
        public void DataFactories_RejectMalformedValues()
        {
            Assert.Throws<ArgumentException>(() => CombatViewCommandData.ReturnUnit(EntityId.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CombatViewCommandData.PlaceUnit(Unit, Target, (Rotation4)9));
            Assert.Throws<ArgumentException>(() => CombatViewCommandData.Attack(Unit, default));
            Assert.Throws<ArgumentException>(() => CombatViewPreviewData.MoveRange(EntityId.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CombatViewPreviewData.MainCharacterPlacement(Target, (Rotation4)9));
            Assert.Throws<ArgumentException>(() => CombatViewPreviewData.Attack(Unit, default));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(99)]
        public void AttackInput_RejectsUnsupportedKindsWithoutConsumingNumber(int kind)
        {
            // 외부에서 복원한 잘못된 입력을 흉내 내며, 운영 계약에 생성 경로를 추가하지 않는다.
            object boxed = default(AttackAim);
            typeof(AttackAim).GetField("<Kind>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(boxed, (AttackAimKind)kind);
            var aim = (AttackAim)boxed;
            var buffers = CreateBuffers();
            var write = buffers.CombatViewWriteSink;

            Assert.That(Assert.Throws<ArgumentException>(() =>
                write.Submit(CombatViewCommandData.Attack(Unit, aim))).ParamName, Is.EqualTo("aim"));
            Assert.That(Assert.Throws<ArgumentException>(() =>
                write.SetPreview(CombatViewPreviewData.Attack(Unit, aim))).ParamName, Is.EqualTo("aim"));
            Assert.That(buffers.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(buffers.CombatViewReadSink.HasPreview, Is.False);
            Assert.That(write.Submit(CombatViewCommandData.EndUnitTurn()), Is.EqualTo(1));
        }

        private static IBufferQuery CreateBuffers()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
        }
    }
}
