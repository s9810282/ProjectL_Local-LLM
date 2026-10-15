using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ProjectL.Tests.Framework
{
    /// <summary>V4: 실제 씬 Presenter로 Mapper 수명과 사건 중간 표시를 검증합니다.</summary>
    public sealed class CombatSceneEventPresenterTests
    {
        private Scene _scene;
        private CombatSceneRoot _root;
        private CombatSceneDisplay _display;
        private ColorSystem _colors;
        private CombatDisplayValues _values;
        private CharacterCombatFacts _initial;
        private EventSystem[] _previousSystems;
        private EventSystem _previousCurrent;

        [SetUp]
        public void SetUp()
        {
            _scene = default;
            _display = null;
            _colors = null;
            _previousSystems = null;
            _previousCurrent = null;
            if (SceneManager.GetSceneByPath("Assets/Scenes/Combat.unity").isLoaded)
                Assert.Ignore("사용자 Combat 씬을 보존합니다. 다른 씬에서 실행합니다.");
            _previousCurrent = EventSystem.current;
            _previousSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                .Where(system => system.enabled).ToArray();
            foreach (var system in _previousSystems) system.enabled = false;
            _scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
            _root = _scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CombatSceneRoot>(true)).Single();
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(
                AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
            _colors = new ColorSystem();
            _display = new CombatSceneDisplay(_root, database.ViewQuery.StageFieldViewTableQuery.Config,
                database.ViewQuery.UnitVisualTableQuery, database.ViewQuery.StageViewPropTableQuery, _colors,
                database.ViewQuery.TraitVisualTableQuery, new CombatViewSystemTests.FakeAreaOutlines());
            var buffers = CombatPreviewPublicationTests.Create(out _);
            _values = new CombatDisplayValues(buffers.CombatStateReadSink.Current, buffers.CharacterReadSink.Characters);
            _initial = _values.Characters[0].ToFacts();
        }

        [TearDown]
        public void TearDown()
        {
            try { _display?.Release(); }
            finally
            {
                _colors?.Shutdown();
                if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
                if (_previousSystems != null)
                    foreach (var system in _previousSystems)
                        if (system != null) system.enabled = true;
                if (_previousCurrent != null) EventSystem.current = _previousCurrent;
            }
        }

        [Test]
        public void FirstInstall_BindsAfterBuild_AndLateEventsAfterReleaseCannotRenderOrComplete()
        {
            Assert.That(_root.Field.Mapper, Is.Null);
            AssertEventsBlocked();
            _display.Install(_values);
            AssertPosition(Unit(_initial.EntityId), _initial.Anchor.Value);
            _display.BeginStep(Move());
            _display.Release();
            _display.Release();
            AssertEventsBlocked();
            Assert.That(_root.Field.Mapper, Is.Null);
            Assert.That(ActiveUnits(), Is.Empty);
            Assert.Throws<InvalidOperationException>(() => _display.Install(_values));
        }

        [TestCase(CombatEventKind.ObjectActivated)]
        [TestCase(CombatEventKind.ObjectCreated)]
        [TestCase(CombatEventKind.HeldEnvironmentChanged)]
        [TestCase(CombatEventKind.ObstacleReacted)]
        [TestCase(CombatEventKind.TornadoActivated)]
        public void EnvironmentRecord_RealScenePresentationCompletesOnceWithoutChangingCharacterFacts(CombatEventKind kind)
        {
            _display.Install(_values);
            var flag = new FlagSourceFact(31, _initial.Anchor.Value, 1, 7, true, new[] { Owner.Player });
            var tornado = new TornadoSourceFact(32, _initial.Anchor.Value, new[] { Owner.Player });
            var held = new CharacterCombatFacts(_initial.EntityId, _initial.OwnedId, _initial.CharacterKey,
                _initial.Owner, _initial.OwnershipRole, _initial.Area, _initial.Health, _initial.Stats,
                _initial.ReserveSlotIndex, _initial.Anchor, _initial.Rotation, _initial.OccupiedCells,
                _initial.MovementCheckpointAnchor, _initial.CommittedMoveDistance, _initial.PendingMoveActionCost,
                heldEnvironment: new CharacterHeldEnvironmentState(flag: HeldFlag.From(flag)));
            CombatEvent e = kind switch
            {
                CombatEventKind.ObjectActivated => new CombatObjectActivationEvent(flag, _initial.EntityId),
                CombatEventKind.ObjectCreated => new CombatObjectCreatedEvent(flag),
                CombatEventKind.HeldEnvironmentChanged => new CombatHeldEnvironmentEvent(_initial, held),
                CombatEventKind.ObstacleReacted => new CombatObstacleReactionEvent(new EntityId(9999),
                    ObstacleContentKind.PowderBarrel, ObstacleReactionKind.Destroyed,
                    new CombatDamageOrigin(flag.Cell, _initial.EntityId, 1), 1),
                _ => new CombatTornadoEvent(tornado, tornado.AfterActivation(CellCoord.Zero))
            };
            Assert.That(_display.CanPresent(e), Is.True);
            var unit = Unit(_initial.EntityId);
            var position = unit.transform.position;
            var health = unit.HealthLabel.text;
            var ordered = new OrderedCombatPresentation();
            ordered.Attach(_display);
            int completed = 0;
            ordered.Play(new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                events: new[] { e }), _ => completed++);
            ordered.Tick(1); ordered.Tick(1);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(unit.transform.position, Is.EqualTo(position));
            Assert.That(unit.HealthLabel.text, Is.EqualTo(health));
            ordered.Detach();
        }

        [Test]
        public void TornadoRelocation_UsesCapturedDestinationImmediatelyWithoutTraversal()
        {
            _display.Install(_values);
            var destination = _initial.Anchor.Value + CellCoord.Up;
            var e = new CombatCharacterEvent(CombatEventKind.Relocated, _initial, Copy(destination), tornadoSourceId: 31);
            var ordered = new OrderedCombatPresentation();
            ordered.Attach(_display);
            int completed = 0;
            ordered.Play(new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                events: new[] { e }), _ => completed++);
            Assert.That(_display.IsMovementComplete, Is.True);
            AssertPosition(Unit(_initial.EntityId), destination);
            Assert.That(completed, Is.EqualTo(1));
            ordered.Tick(1);
            Assert.That(completed, Is.EqualTo(1));
            ordered.Detach();
        }

        [Test]
        public void ClearDuringMovement_ReinstallUsesNewMapper_AndSameSceneResources()
        {
            _display.Install(_values);
            var oldMapper = _root.Field.Mapper;
            var oldUnit = Unit(_initial.EntityId);
            _display.BeginStep(Move());
            _display.Clear();
            AssertEventsBlocked();
            Assert.That(ActiveUnits(), Is.Empty);
            _root.Field.transform.position += new Vector3(4f, 0f, 3f);
            _display.Install(_values);
            Assert.That(_root.Field.Mapper, Is.Not.SameAs(oldMapper));
            Assert.That(Unit(_initial.EntityId), Is.SameAs(oldUnit), "재설치도 같은 풀의 View를 재사용한다.");
            _display.BeginStep(Move());
            _display.TickPresentation(_root.MovementStepSeconds + .01f);
            AssertPosition(oldUnit, Move().Final.Anchor.Value);
        }

        [Test]
        public void BuildFailure_DropsOldEventBinding_EvenWhenOldFieldStillExists()
        {
            _display.Install(_values);
            var oldMapper = _root.Field.Mapper;
            _display.BeginStep(Move());
            // 기존 필드는 살아 있지만 다음 Build의 루트 검증이 실패하는 경로를 만든다.
            var invalidState = (ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateData)
                typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_values.State, null);
            typeof(ProjectL.Systems.RuntimeSystem.BufferSystems.Combat.CombatStateData)
                .GetField("<BoardWidth>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(invalidState, _values.State.BoardWidth + 1);
            var invalid = new CombatDisplayValues(invalidState, _values.Characters);
            var serialized = new SerializedObject(_root.Field);
            var cellsRoot = (Transform)serialized.FindProperty("cellsRoot").objectReferenceValue;
            cellsRoot.localScale = Vector3.one * 2f;
            Assert.Throws<InvalidOperationException>(() => _display.Install(invalid));
            Assert.That(_root.Field.Mapper, Is.SameAs(oldMapper));
            AssertEventsBlocked();
            cellsRoot.localScale = Vector3.one;
            _display.Clear();
            _display.Install(_values);
            _display.ShowReturn(new CombatCharacterEvent(CombatEventKind.Returned, Move().Final, _initial));
            AssertPosition(Unit(_initial.EntityId), _initial.Anchor.Value);
        }

        [Test]
        public void OrderedMovement_DoesNotApplyDamageBeforeArrival_AndCompletesOnce()
        {
            _display.Install(_values);
            var unit = Unit(_initial.EntityId);
            var mapper = _root.Field.Mapper;
            var moved = Move();
            var dead = Copy(moved.Final.Anchor.Value, 0);
            var ordered = new OrderedCombatPresentation();
            ordered.Attach(_display);
            int completed = 0;
            ordered.Play(new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                events: new CombatEvent[] { moved, new CombatAttackEvent(default),
                    new CombatCharacterEvent(CombatEventKind.Died, moved.Final, dead) }), _ => completed++);
            AssertPosition(unit, _initial.Anchor.Value);
            ordered.Tick(_root.MovementStepSeconds * .5f);
            Assert.That(Vector3.Distance(unit.transform.position, Vector3.Lerp(
                mapper.CellCenterToWorld(_initial.Anchor.Value), mapper.CellCenterToWorld(moved.Final.Anchor.Value), .5f)), Is.LessThan(.001f));
            Assert.That(unit.gameObject.activeSelf, Is.True);
            Assert.That(completed, Is.Zero);
            ordered.Tick(_root.MovementStepSeconds);
            Assert.That(unit.gameObject.activeSelf, Is.False);
            Assert.That(completed, Is.EqualTo(1));
            ordered.Tick(1f);
            Assert.That(completed, Is.EqualTo(1));
            ordered.Detach();
            _display.Refresh(_values);
            AssertPosition(Unit(_initial.EntityId), _initial.Anchor.Value);
            Assert.That(_root.Field.Mapper, Is.SameAs(mapper), "결과 Refresh는 필드를 재구성하지 않는다.");
        }

        [Test]
        public void Cancel_StopsInterpolationWithoutResultCompletion_AndReturnAppliesImmediately()
        {
            _display.Install(_values);
            var ordered = new OrderedCombatPresentation();
            ordered.Attach(_display);
            int completed = 0;
            ordered.Play(new CombatResult(1, CombatProcessingKind.Command, 1, true, string.Empty,
                events: new[] { Move() }), _ => completed++);
            ordered.Tick(_root.MovementStepSeconds * .5f);
            var unit = Unit(_initial.EntityId);
            var stopped = unit.transform.position;
            ordered.Cancel();
            ordered.Tick(10f);
            _display.TickPresentation(10f);
            Assert.That(unit.transform.position, Is.EqualTo(stopped));
            Assert.That(completed, Is.Zero);
            _display.ShowReturn(new CombatCharacterEvent(CombatEventKind.Returned, Move().Final, _initial));
            AssertPosition(unit, _initial.Anchor.Value);
            ordered.Detach();
        }

        [Test]
        public void SoulAndStructureEvents_UseEventFacts_AndRefreshRestoresProvidedFacts()
        {
            _display.Install(_values);
            var changed = Copy(_initial.Anchor.Value, Math.Max(1, _initial.Health.CurrentHealth - 1));
            var souls = new SoulFieldChange(SoulFieldSnapshot.Empty, SoulFieldSnapshot.Empty);
            var collection = new SoulActionChanges(new CharacterChanges(
                new[] { new CharacterStateChange(_initial, changed) }, Array.Empty<SoulCountChange>()), souls, Array.Empty<long>());
            int completed = 0;
            _display.BeginEvent(new CombatSoulEvent(souls, _initial.EntityId, collection), () => completed++);
            Assert.That(Unit(_initial.EntityId).HealthLabel.text, Does.StartWith(changed.Health.CurrentHealth.ToString()));
            var spec = new StructureSpec(_initial.CharacterKey, 1, 1, new[] { CellCoord.Zero }, 3, false, 0);
            var structure = new StructureCombatFacts(new EntityId(9999), spec, new HealthStateSnapshot(3, 3),
                CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero });
            _display.BeginEvent(new CombatStructureEvent(structure, structure), () => completed++);
            var structureView = Unit(structure.EntityId);
            AssertPosition(structureView, CellCoord.Zero);
            var dead = new StructureCombatFacts(structure.EntityId, spec, new HealthStateSnapshot(0, 3),
                CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero });
            _display.BeginEvent(new CombatStructureEvent(structure, dead), () => completed++);
            Assert.That(structureView.gameObject.activeSelf, Is.False);
            Assert.That(completed, Is.EqualTo(3));
            _display.Refresh(_values);
            Assert.That(Unit(_initial.EntityId).HealthLabel.text, Does.StartWith(_initial.Health.CurrentHealth.ToString()));
        }

        [Test]
        public void WarmFactRefresh_AllocatesLessThanLegacyProjection_AndReusesViews()
        {
            _display.Install(_values);
            var events = (CombatSceneEventPresenter)typeof(CombatSceneDisplay)
                .GetField("_events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_display);
            var unit = Unit(_initial.EntityId);
            for (int i = 0; i < 32; i++)
            {
                events.Refresh(_values.Characters, _values.State.Structures);
                _root.FieldUnits.Render(_values.Characters, _values.State.Structures, _root.Field.Mapper);
            }
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++) events.Refresh(_values.Characters, _values.State.Structures);
            long current = GC.GetAllocatedBytesForCurrentThread() - start;
            start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 128; i++)
                _root.FieldUnits.Render(_values.Characters, _values.State.Structures, _root.Field.Mapper);
            long legacy = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.That(current, Is.LessThan(legacy), "동일 RenderFacts 비용 외의 중복 투영/배열 생성을 비교한다.");
            Assert.That(Unit(_initial.EntityId), Is.SameAs(unit));
            TestContext.WriteLine($"V4b warm fact refresh x128: current={current}, legacy projection={legacy} managed bytes.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent(bool movement)
        {
            _display.Install(_values);
            var unit = Unit(_initial.EntityId);
            var probe = unit.gameObject.AddComponent<CombatEventVisibilityProbe>();
            int completed = 0;
            try
            {
                if (movement)
                {
                    unit.gameObject.SetActive(false);
                    probe.Changed = _display.CancelPresentation;
                    _display.BeginStep(Move());
                    Assert.That(_display.IsMovementComplete, Is.True, "표시 중 취소한 이동을 다시 진행 상태로 쓰지 않는다.");
                    _display.TickPresentation(10f);
                    AssertPosition(unit, _initial.Anchor.Value);
                }
                else
                {
                    probe.Changed = _display.CancelPresentation;
                    _display.BeginEvent(new CombatCharacterEvent(CombatEventKind.Died, _initial,
                        Copy(_initial.Anchor.Value, 0)), () => completed++);
                }
                Assert.That(completed, Is.Zero);
            }
            finally
            {
                probe.Changed = null;
            }
        }

        private void AssertEventsBlocked()
        {
            int completed = 0;
            Assert.Throws<InvalidOperationException>(() => _display.BeginStep(Move()));
            Assert.Throws<InvalidOperationException>(() => _display.ShowReturn(
                new CombatCharacterEvent(CombatEventKind.Returned, Move().Final, _initial)));
            Assert.Throws<InvalidOperationException>(() => _display.BeginEvent(
                new CombatCharacterEvent(CombatEventKind.HealthChanged, _initial, _initial), () => completed++));
            Assert.DoesNotThrow(() => _display.TickPresentation(10f));
            Assert.DoesNotThrow(_display.CancelPresentation);
            Assert.That(completed, Is.Zero);
        }

        private CombatCharacterEvent Move() => new(CombatEventKind.Moved, _initial, Copy(_initial.Anchor.Value + CellCoord.Up));
        private CharacterCombatFacts Copy(CellCoord anchor, int? health = null) => new(
            _initial.EntityId, _initial.OwnedId, _initial.CharacterKey, _initial.Owner, _initial.OwnershipRole,
            _initial.Area, new HealthStateSnapshot(health ?? _initial.Health.CurrentHealth, _initial.Health.MaxHealth),
            _initial.Stats, null, anchor, _initial.Rotation, new[] { anchor }, _initial.MovementCheckpointAnchor,
            _initial.CommittedMoveDistance, _initial.PendingMoveActionCost);
        private FieldUnitView[] ActiveUnits() => _root.FieldUnits.GetComponentsInChildren<FieldUnitView>();
        private FieldUnitView Unit(EntityId id) => ActiveUnits().Single(unit => unit.EntityId == id);
        private void AssertPosition(FieldUnitView unit, CellCoord cell) => Assert.That(
            Vector3.Distance(unit.transform.position, _root.Field.Mapper.CellCenterToWorld(cell)), Is.LessThan(.001f));
    }

    public sealed class CombatEventVisibilityProbe : MonoBehaviour
    {
        internal Action Changed;
        private void OnEnable() => Changed?.Invoke();
        private void OnDisable() => Changed?.Invoke();
    }
}
