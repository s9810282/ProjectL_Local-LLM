using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>적 본부의 선택 수명과 실제 프리팹의 건물/캐릭터 표시 전환을 검증합니다.</summary>
    public sealed class CombatStructureInspectionTests
    {
        private static readonly CellCoord Anchor = new(2, 2);
        private static readonly CellCoord SecondCell = new(3, 2);

        private GameObject _factsRoot;
        private CombatShownFacts _facts;

        [SetUp]
        public void SetUp()
        {
            _factsRoot = new GameObject("InspectionFacts");
            var units = _factsRoot.AddComponent<FieldUnitPresenter>();
            var effects = new CombatEffectWindow(new NoEffects(), _factsRoot.transform, _ => () => (false, (Vector3?)null));
            _facts = new CombatShownFacts(units, () => null,
                new CombatPersistentEffects(effects, Array.Empty<ICombatPersistentEffectSource>()));
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_factsRoot);

        private void Synchronize(CombatSelectionState selection, StructureCombatFacts[] structures, StagePhase stage)
        {
            _facts.Replace(Array.Empty<CharacterCombatFacts>(), structures);
            selection.Bind(_facts);
            selection.Synchronize(stage);
        }

        // 선택 사실만 검증하므로 이펙트를 생성하지 않습니다.
        private sealed class NoEffects : IEffectSystemServices
        {
            public EffectScope GetEffectHandle() => throw new NotSupportedException();
        }

        [Test]
        public void Headquarters_AllOccupiedCellsSelectInfoWithoutSelectingActor()
        {
            var selection = new CombatSelectionState();
            StructureCombatFacts headquarters = Structure();
            Synchronize(selection, new[] { headquarters }, StagePhase.Combat);
            foreach (CellCoord cell in headquarters.OccupiedCells)
            {
                selection.SelectCell(cell);
                Assert.That(selection.SelectedEntityId, Is.EqualTo(headquarters.EntityId));
                Assert.That(selection.SelectedActorId.IsEmpty, Is.True);
                Assert.That(selection.SelectedActorCells, Is.Empty);
            }
            selection.SelectCell(null);
            Assert.That(selection.SelectedEntityId.IsEmpty, Is.True);
        }

        [TestCase(Owner.Player, EntityKey.Structure_Headquarters, 10)]
        [TestCase(Owner.Enemy, EntityKey.Structure_PowderBarrel, 10)]
        [TestCase(Owner.Enemy, EntityKey.Structure_Headquarters, 0)]
        public void NonTargetStructures_DoNotOpenInfo(Owner owner, EntityKey key, int health)
        {
            var selection = new CombatSelectionState();
            Synchronize(selection, new[] { Structure(owner, key, health) }, StagePhase.Combat);
            selection.SelectCell(Anchor);
            Assert.That(selection.SelectedEntityId.IsEmpty, Is.True);
        }

        [Test]
        public void Headquarters_RefreshPreservesSelectionAndDeathOrPhaseChangeClearsIt()
        {
            var selection = new CombatSelectionState();
            Synchronize(selection, new[] { Structure() }, StagePhase.Combat);
            selection.SelectCell(Anchor);
            Synchronize(selection, new[] { Structure(health: 5) }, StagePhase.Combat);
            Assert.That(selection.SelectedEntityId, Is.EqualTo(new EntityId(100)));
            Synchronize(selection, new[] { Structure(health: 0) }, StagePhase.Combat);
            Assert.That(selection.SelectedEntityId.IsEmpty, Is.True);
            Synchronize(selection, new[] { Structure() }, StagePhase.Combat);
            selection.SelectCell(Anchor);
            Synchronize(selection, new[] { Structure() }, StagePhase.Deployment);
            Assert.That(selection.SelectedEntityId.IsEmpty, Is.True);
            selection.SelectCell(Anchor);
            Assert.That(selection.SelectedEntityId, Is.EqualTo(new EntityId(100)));
            Assert.That(selection.SelectedActorId.IsEmpty, Is.True);
        }

        [Test]
        public void HudPrefab_StructureHasNoDefenseAndUnitRestoresIt()
        {
            GameObject canvas = new("HUD Test Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/CombatOnly/HUD/HUD.prefab");
                var instance = UnityEngine.Object.Instantiate(prefab, canvas.transform);
                var hud = instance.GetComponent<UnitHUD>();
                hud.SetStructure(Structure());
                hud.SetStructureInfo("적 본부", 0);
                var hover = instance.GetComponentInChildren<UnitHUDHoverPanel>(true);
                Assert.That(FieldObject(hover, "shieldRoot").activeSelf, Is.False);
                Assert.That(Text(hover, "unitName"), Is.EqualTo("적 본부"));
                Assert.That(Text(hover, "aValue"), Is.EqualTo("0"));
                hud.SetInfo("유닛", 4, 2);
                Assert.That(FieldObject(hover, "shieldRoot").activeSelf, Is.True);
                Assert.That(Text(hover, "sValue"), Is.EqualTo("2"));
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }

        private static GameObject FieldObject(Component component, string field)
        {
            var value = new SerializedObject(component).FindProperty(field).objectReferenceValue;
            return value is GameObject gameObject ? gameObject : ((Component)value).gameObject;
        }

        private static string Text(Component component, string field)
        {
            var text = new SerializedObject(component).FindProperty(field).objectReferenceValue;
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }

        private static StructureCombatFacts Structure(Owner owner = Owner.Enemy,
            EntityKey key = EntityKey.Structure_Headquarters, int health = 10, int attackPower = 0) =>
            new(new EntityId(100), new StructureSpec(key, 2, 1, new[] { CellCoord.Zero, new CellCoord(1, 0) }, 10, attackPower > 0, attackPower),
                new HealthStateSnapshot(health, 10), Anchor, Rotation4.R0, new[] { Anchor, SecondCell }, owner);
    }
}
