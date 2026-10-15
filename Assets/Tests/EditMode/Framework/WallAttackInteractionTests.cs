using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Tests.Framework
{
    public sealed class WallAttackInteractionTests
    {
        private IBufferQuery _buffers;
        private AttackTargetingInteraction _attack;
        private CombatInteractionTests.FakeFeedback _feedback;
        private CharacterData[] _characters;
        private CombatInputFacts _facts;
        private CombatCommandInput _input;

        [Test]
        public void BlockedSingle_HoverUsesRealRangeEvidenceWithoutPreviewOrClick()
        {
            Create();
            var range = CombatCommandCalculator.GetAttackRange(_input);
            var target = new CellCoord(3, 1);
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(AttackAim.ForCell(target)));
            var request = _buffers.CombatViewReadSink.Preview.Number;
            Answer(CombatPreviewResult.ForAttackRange(request, range));
            Hover(target);
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Invalid));
            AssertBlocker(AttackOcclusionPhase.Direct, new CellCoord(1, 1), target);
            Assert.That(_buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(request));
            _attack.SelectReleased(_facts, _characters, 35, 15);
            Assert.That(_buffers.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(_feedback.AffectedCells, Is.Empty);
            Assert.That(_feedback.AttackCost, Is.Null);
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void PartialGroup_PreviewShowsBlockersAndClearEffects_AndKeepsValidClick(TargetingChoiceMode choice)
        {
            Create(choice: choice, extraTarget: true);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            var request = _buffers.CombatViewReadSink.Preview;
            var preview = CombatCommandCalculator.Preview(WithAim(request.Data.Aim), Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(new[] { new EntityId(20) }));
            Answer(CombatPreviewResult.ForAttack(request.Number, preview));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Valid));
            Assert.That(_feedback.Blockers.Any(c => c.Contact == new CellCoord(3, 1)), Is.True);
            Assert.That(_feedback.AffectedCells, Has.Member(new CellCoord(2, 0)));
            Assert.That(_feedback.CostActor, Is.EqualTo(_input.ActorId));
            Assert.That(_feedback.AttackCost, Is.EqualTo(preview.AttackCost));
            _attack.SelectReleased(_facts, _characters, 35, 15);
            Assert.That(_buffers.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void Splash_PreviewUsesImpactEvidenceRatherThanRangeOrigin()
        {
            Create(wall: new CellCoord(4, 1), splash: TargetingSplash.Radial);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            var request = _buffers.CombatViewReadSink.Preview;
            Answer(CombatPreviewResult.ForAttack(request.Number,
                CombatCommandCalculator.Preview(WithAim(request.Data.Aim), Array.Empty<IMovementTileRule>())));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Valid));
            AssertBlocker(AttackOcclusionPhase.Propagation, new CellCoord(3, 1), new CellCoord(4, 1));
            Assert.That(_feedback.AffectedCells, Has.No.Member(new CellCoord(4, 1)));
            Assert.That(_feedback.AttackCost, Is.Not.Null);
        }

        [Test]
        public void InvalidToValidHover_ClearsOldBlockersWhileWaitingForNewPreview()
        {
            Create(extraTarget: true);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            Assert.That(_feedback.Blockers, Is.Not.Empty);
            Hover(new CellCoord(2, 0));
            Assert.That(_feedback.Blockers, Is.Empty);
            var request = _buffers.CombatViewReadSink.Preview;
            Answer(CombatPreviewResult.ForAttack(request.Number,
                CombatCommandCalculator.Preview(WithAim(request.Data.Aim), Array.Empty<IMovementTileRule>())));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Valid));
            Assert.That(_feedback.Blockers, Is.Empty);
            Assert.That(_feedback.AttackCost, Is.Not.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LateAnswerAfterHoverChangeExitCancelOrTeardown_CannotRestoreHighlights(int change)
        {
            Create(wall: new CellCoord(4, 1), splash: TargetingSplash.Radial);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            var request = _buffers.CombatViewReadSink.Preview;
            var answer = CombatPreviewResult.ForAttack(request.Number,
                CombatCommandCalculator.Preview(WithAim(request.Data.Aim), Array.Empty<IMovementTileRule>()));
            Answer(answer);
            Assert.That(_feedback.Blockers, Is.Not.Empty);
            if (change == 0) Hover(new CellCoord(0, 5));
            else if (change == 1) _attack.PointerMoved(_facts, _characters, -1, -1);
            else _attack.Exit();
            if (change == 3) _buffers.BattleWriteSink.EndBattle();
            Answer(answer);
            Assert.That(_feedback.Blockers, Is.Empty);
            Assert.That(_feedback.AffectedCells, Is.Empty);
            Assert.That(_feedback.AttackCost, Is.Null);
            Assert.That(_feedback.HoverState, Is.EqualTo(change == 0 ? CombatHoverState.Invalid : CombatHoverState.Neutral));
            Assert.That(_buffers.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void OlderAnswerCannotReplaceNewerHoverPreview()
        {
            Create(wall: new CellCoord(4, 1), splash: TargetingSplash.Radial, extraTarget: true);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            var old = _buffers.CombatViewReadSink.Preview;
            var oldAnswer = CombatPreviewResult.ForAttack(old.Number,
                CombatCommandCalculator.Preview(WithAim(old.Data.Aim), Array.Empty<IMovementTileRule>()));
            Hover(new CellCoord(2, 0));
            var current = _buffers.CombatViewReadSink.Preview;
            Answer(oldAnswer);
            Assert.That(_feedback.Blockers, Is.Empty);
            Assert.That(_feedback.AffectedCells, Is.Empty);
            Answer(CombatPreviewResult.ForAttack(current.Number,
                CombatCommandCalculator.Preview(WithAim(current.Data.Aim), Array.Empty<IMovementTileRule>())));
            Assert.That(_feedback.HoverState, Is.EqualTo(CombatHoverState.Valid));
            Assert.That(_feedback.Blockers, Is.Empty);
        }

        [Test]
        public void BlockerLayer_OverridesEffectsButYieldsToPointerAndRestoresUnderlyingColor()
        {
            var lower = FieldHighlightLayer.ActiveActor | FieldHighlightLayer.Range | FieldHighlightLayer.Affected;
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, lower | FieldHighlightLayer.BlockedWall, true),
                Is.EqualTo(FieldHighlightColor.HoverInvalid));
            Assert.That(FieldHighlightLayers.Resolve(true, CombatHoverState.Valid, lower | FieldHighlightLayer.BlockedWall, true),
                Is.EqualTo(FieldHighlightColor.HoverValid));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, lower, true), Is.EqualTo(FieldHighlightColor.Affected));
        }

        [Test]
        public void ResultRefresh_ClearsBlockersAndCostUntilMatchingNewAnswersArrive()
        {
            Create(wall: new CellCoord(4, 1), splash: TargetingSplash.Radial);
            AnswerRange();
            Hover(new CellCoord(3, 1));
            var old = _buffers.CombatViewReadSink.Preview;
            var answer = CombatPreviewResult.ForAttack(old.Number,
                CombatCommandCalculator.Preview(WithAim(old.Data.Aim), Array.Empty<IMovementTileRule>()));
            Answer(answer);
            Assert.That(_feedback.Blockers, Is.Not.Empty);
            Assert.That(_feedback.CostActor, Is.EqualTo(_input.ActorId));
            _attack.AfterResult(_facts);
            Assert.That(_buffers.CombatViewReadSink.Preview.Number, Is.GreaterThan(old.Number));
            Answer(answer);
            Assert.That(_feedback.Blockers, Is.Empty);
            Assert.That(_feedback.AffectedCells, Is.Empty);
            Assert.That(_feedback.AttackCost, Is.Null);
            AnswerRange();
            var current = _buffers.CombatViewReadSink.Preview;
            Answer(CombatPreviewResult.ForAttack(current.Number,
                CombatCommandCalculator.Preview(WithAim(current.Data.Aim), Array.Empty<IMovementTileRule>())));
            AssertBlocker(AttackOcclusionPhase.Propagation, new CellCoord(3, 1), new CellCoord(4, 1));
            Assert.That(_feedback.CostActor, Is.EqualTo(_input.ActorId));
        }

        [Test]
        public void AuthoredScene_BlockerFeedbackUnionsCellsRestoresColorAndClearsOnTeardown()
        {
            if (SceneManager.GetSceneByPath("Assets/Scenes/Combat.unity").isLoaded)
                Assert.Ignore("Combat 씬이 이미 열려 있어 사용자 씬을 보존합니다.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
            var colors = new ColorSystem();
            CombatSceneDisplay display = null;
            try
            {
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CombatSceneRoot>(true)).Single();
                var database = (IDataBaseQueryContext)DataBaseFactory.Create(
                    AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
                display = new CombatSceneDisplay(root, database.ViewQuery.StageFieldViewTableQuery.Config,
                    database.ViewQuery.UnitVisualTableQuery, database.ViewQuery.StageViewPropTableQuery, colors,
                    database.ViewQuery.TraitVisualTableQuery, new CombatViewSystemTests.FakeAreaOutlines());
                Create();
                var values = new CombatDisplayValues(_buffers.CombatStateReadSink.Current, _characters);
                display.Install(values);
                var contact = CombatCommandCalculator.GetAttackRange(_input).BlockedContacts.Single(c => c.Contact == new CellCoord(3, 1));
                var wall = new CellCoord(2, 1);
                display.ShowAffectedCells(new[] { wall });
                var affected = ShownColor(root.Field, wall);
                display.ShowAttackBlockers(new[] { contact, contact });
                Assert.That(Layers(root.Field).Count(l => (l & FieldHighlightLayer.BlockedWall) != 0), Is.EqualTo(1));
                Assert.That(ShownColor(root.Field, wall), Is.EqualTo(FieldValue<Color>(root.Field, "hoverInvalidColor")));
                display.SetHoverCell(wall, CombatHoverState.Valid);
                Assert.That(ShownColor(root.Field, wall), Is.EqualTo(FieldValue<Color>(root.Field, "hoverValidColor")));
                display.ClearHoverCell();
                Assert.That(ShownColor(root.Field, wall), Is.EqualTo(FieldValue<Color>(root.Field, "hoverInvalidColor")));
                display.ClearAttackBlockers();
                Assert.That(ShownColor(root.Field, wall), Is.EqualTo(affected));
                display.ShowAttackBlockers(new[] { contact });
                display.ShowAttackBlockers(Array.Empty<AttackBlockedContact>());
                Assert.That(ShownColor(root.Field, wall), Is.EqualTo(affected));
                display.ShowAttackBlockers(new[] { contact });
                display.Clear();
                display.Install(values);
                Assert.That(Layers(root.Field).Any(l => (l & FieldHighlightLayer.BlockedWall) != 0), Is.False);
            }
            finally { display?.Release(); colors.Shutdown(); EditorSceneManager.CloseScene(scene, true); }
        }

        private static T FieldValue<T>(FieldView field, string name) => (T)typeof(FieldView)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(field);
        private static FieldHighlightLayer[] Layers(FieldView field) => Enumerable
            .Range(0, field.HighlightState.CellCount).Select(field.HighlightState.LayersAt).ToArray();
        private static Color ShownColor(FieldView field, CellCoord cell) => (Color)typeof(FieldCellHandle)
            .GetField("_highlight", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(((FieldCellColorRenderer)FieldValue<IFieldHighlightRenderer>(field, "_renderer"))
                .CellAt(cell.Y * field.Mapper.ColumnCount + cell.X));

        private void Create(CellCoord? wall = null, TargetingChoiceMode choice = TargetingChoiceMode.Single,
            TargetingSplash splash = TargetingSplash.None, bool extraTarget = false)
        {
            _buffers = CombatPreviewPublicationTests.Create(out _, Array.Empty<IMovementTileRule>(),
                new[] { wall ?? new CellCoord(2, 1) }, new CellCoord(3, 1));
            _characters = _buffers.CharacterReadSink.Characters.ToArray();
            _facts = CombatInteractionTests.Combat(_characters.Single().EntityId);
            _feedback = new CombatInteractionTests.FakeFeedback();
            _attack = new AttackTargetingInteraction(_buffers.CombatViewWriteSink, _buffers.CombatViewReadSink,
                new CombatInteractionTests.FakePicker(), _feedback, new CombatCommandGate(_buffers.CombatViewWriteSink));
            _attack.Enter(_facts, _facts.ActiveActorId);
            var source = CombatCommandPublicationTests.Input(_buffers);
            var s = source.State;
            var structures = s.Structures.ToArray();
            if (extraTarget)
                structures = structures.Append(new StructureCombatFacts(new EntityId(20), structures[0].Definition,
                    new HealthStateSnapshot(20, 20), new CellCoord(2, 0), Rotation4.R0, new[] { new CellCoord(2, 0) })).ToArray();
            var state = new CombatCommandState(8, 6, s.Phase, s.ActiveActorId, s.IsNight, structures, s.ThornFields,
                s.Timeline, s.Turn, s.Environment, s.EnvironmentProgress, s.WallBlocking);
            var profiles = CharacterBattlePreparationTests.Initialization().Units.Select(u => new CombatAttackProfile(u,
                new TargetingMethodSpec(TargetingShape.RadialSquare, choice, splash, splash == TargetingSplash.None ? 0 : 70))).ToArray();
            _input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                source.Characters, profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                environmentBindings: source.EnvironmentBindings);
        }

        private CombatCommandInput WithAim(AttackAim aim) => new CombatCommandInput(_input.Number, _input.Kind, _input.ActorId, aim,
            _input.State, _input.Characters, _input.Profiles, _input.Main, _input.Owned, _input.SoulCounts, _input.Souls, _input.Artifacts,
            environmentBindings: _input.EnvironmentBindings);
        private void AnswerRange() => Answer(CombatPreviewResult.ForAttackRange(_buffers.CombatViewReadSink.Preview.Number,
            CombatCommandCalculator.GetAttackRange(_input)));
        private void Hover(CellCoord cell) => _attack.PointerMoved(_facts, _characters, cell.X * 10 + 5, cell.Y * 10 + 5);
        private void Answer(CombatPreviewResult answer) => _attack.PreviewAnswered(new CombatInputFacts(
            _facts.IsBattleDisplayed, _facts.HasBattle, _facts.Phase, _facts.StagePhase, _facts.HasCompletion,
            _facts.ExecutionMode, _facts.CurrentLayer, _facts.LatestResultSequence, _facts.PresentedResultSequence,
            _facts.IsFinalizing, _facts.ActiveActorId, _facts.HasUnhandledCommand, answer), _characters);
        private void AssertBlocker(AttackOcclusionPhase phase, CellCoord origin, CellCoord contact)
        {
            var blocker = _feedback.Blockers.Single();
            Assert.That(blocker.Phase, Is.EqualTo(phase));
            Assert.That(blocker.Origin, Is.EqualTo(origin));
            Assert.That(blocker.Contact, Is.EqualTo(contact));
            Assert.That(blocker.WallSourceIds, Is.EqualTo(new long[] { 71 }));
        }
    }
}
