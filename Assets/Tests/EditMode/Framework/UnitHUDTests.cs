using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Tests.Framework
{
    public sealed class UnitHUDTests
    {
        private GameObject _root;
        private UnitHUD _hud;
        private Transform _costs, _statuses;
        private Slider _health;
        private TMP_Text _healthText, _order, _description;
        private GameObject _panel;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("HUD", typeof(RectTransform));
            _hud = _root.AddComponent<UnitHUD>();
            _health = Child("Health").AddComponent<Slider>();
            _healthText = Text("HealthText");
            _order = Text("Order");
            _description = Text("Description");
            _panel = Child("Panel");
            _panel.SetActive(false);
            _costs = Child("Costs").transform;
            _statuses = Child("Statuses").transform;
            var costPrefab = Child("CostPrefab").AddComponent<Image>();
            costPrefab.rectTransform.sizeDelta = new Vector2(20f, 20f);
            costPrefab.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            costPrefab.gameObject.SetActive(false);
            var status = Child("StatusPrefab");
            var icon = status.AddComponent<Image>();
            icon.rectTransform.sizeDelta = new Vector2(18f, 18f);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            status.SetActive(false);
            Set("healthSlider", _health); Set("healthText", _healthText); Set("turnOrderText", _order);
            Set("showTurnOrder", true);
            Set("spentCostRotationDuration", 0f);
            Set("actionCostRoot", _costs); Set("actionCostPrefab", costPrefab);
            Set("statusRoot", _statuses); Set("statusIconPrefab", icon);
            Set("descriptionPanel", _panel); Set("descriptionText", _description);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Structure_ShowsOnlyHealth_UpdatesDamage_AndUnitReuseRestoresExtras()
        {
            _hud.SetUnit(new CharacterData(CharacterBufferTestData.EnemyFacts(new EntityId(3), 3)));
            _hud.SetStatuses(new[] { new UnitHUDStatus("buff", null, "Buff") });
            Dispatch(0, ExecuteEvents.pointerClickHandler);
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 20, false, 0);
            _hud.SetStructure(new StructureCombatFacts(new EntityId(4), definition,
                new HealthStateSnapshot(15, 20), CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }));

            Assert.That(_health.value, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(_healthText.text, Is.EqualTo("15 / 20"));
            Assert.That(_costs.gameObject.activeSelf, Is.False);
            Assert.That(_statuses.gameObject.activeSelf, Is.False);
            Assert.That(_order.gameObject.activeSelf, Is.False);
            Assert.That(_panel.activeSelf, Is.False);

            _hud.SetStructure(new StructureCombatFacts(new EntityId(4), definition,
                new HealthStateSnapshot(8, 20), CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }));
            Assert.That(_healthText.text, Is.EqualTo("8 / 20"));
            Assert.That(_health.value, Is.EqualTo(0.4f).Within(0.001f));
            _hud.Clear();
            _hud.SetUnit(new CharacterData(CharacterBufferTestData.EnemyFacts(new EntityId(3), 3)));
            Assert.That(_costs.gameObject.activeSelf, Is.True);
            Assert.That(_statuses.gameObject.activeSelf, Is.True);
            Assert.That(_order.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Health_ExplicitPreviewRestoresActual_AndZeroMaximumIsSafe()
        {
            _hud.SetHealth(6, 10);
            Assert.That(_health.value, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(_healthText.text, Is.EqualTo("6 / 10"));
            _hud.SetHealthPreview(2);
            Assert.That(_health.value, Is.EqualTo(0.2f).Within(0.001f));
            _hud.ClearPreview();
            Assert.That(_healthText.text, Is.EqualTo("6 / 10"));
            _hud.SetHealth(0, 0);
            Assert.That(_health.value, Is.Zero);
        }

        [Test]
        public void Cost_PendingMovementIsTranslucent_AttackPreviewAddsToIt_AndSlotsAreReused()
        {
            _hud.SetActionCost(2, 5, 1);
            AssertCost(0, false, 1f); AssertCost(1, false, 1f);
            AssertCost(2, false, 0.4f); AssertCost(3, true, 1f);
            _hud.SetActionCostPreview(1);
            AssertCost(0, false, 1f); AssertCost(1, false, 0.4f); AssertCost(2, false, 0.4f);
            _hud.ClearPreview();
            AssertCost(1, false, 1f); AssertCost(2, false, 0.4f);
            _hud.SetActionCost(1, 2);
            Assert.That(_costs.childCount, Is.EqualTo(5));
            Assert.That(_costs.GetChild(2).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Cost_PreviewAlphaPulsesBetweenMinimumAndOriginal_WithoutChangingSpentOrAvailableSlots()
        {
            _hud.SetActionCost(2, 4, 1);
            _hud.SetActionCostPreview(1);
            _hud.AnimateCostPreview(0f);
            AssertCost(0, false, 1f); AssertCost(1, false, 0.4f); AssertCost(2, false, 0.4f);
            AssertCost(3, true, 1f);
            _hud.AnimateCostPreview(0.25f);
            AssertCost(1, false, 0.7f); AssertCost(2, false, 0.7f);
            _hud.SetActionCostPreview(1);
            _hud.AnimateCostPreview(0.5f);
            AssertCost(1, false, 1f); AssertCost(2, false, 1f);
            _hud.AnimateCostPreview(1f);
            AssertCost(1, false, 0.4f);
            _hud.ClearPreview();
            _hud.AnimateCostPreview(1f);
            AssertCost(1, false, 1f); AssertCost(2, false, 0.4f);
            _hud.SetActionCost(4, 4);
            _hud.AnimateCostPreview(1f);
            AssertCost(2, false, 1f);
        }

        [Test]
        public void Cost_SpentAndRecovered_RestoreColorAndPrefabRotationWithoutAccumulation()
        {
            _hud.SetActionCost(1, 3);
            AssertCost(0, false, 1f); AssertCost(1, true, 1f); AssertCost(2, true, 1f);
            _hud.SetActionCost(1, 3);
            AssertCost(1, true, 1f);
            _hud.SetOwner(Owner.Enemy);
            AssertCost(0, false, 1f, true); AssertCost(1, true, 1f, true);
            _hud.SetActionCost(3, 3);
            AssertCost(1, false, 1f, true); AssertCost(2, false, 1f, true);
            _hud.SetOwner(Owner.Player);
            AssertCost(1, false, 1f);
        }

        [Test]
        public void Cost_RotationDuration_InterpolatesAndReversesWithoutRestartingOnRefresh()
        {
            Set("spentCostRotationDuration", 1f);
            _hud.SetActionCost(3, 3);
            _hud.SetActionCost(2, 3);
            _hud.AnimateCostRotation(0.5f);
            Assert.That(Quaternion.Angle(_costs.GetChild(2).localRotation,
                Quaternion.Euler(0f, 0f, 37.5f)), Is.LessThan(0.1f));
            _hud.SetActionCost(2, 3);
            _hud.AnimateCostRotation(0.5f);
            AssertCost(2, true, 1f);
            _hud.SetActionCost(3, 3);
            _hud.AnimateCostRotation(0.5f);
            Assert.That(Quaternion.Angle(_costs.GetChild(2).localRotation,
                Quaternion.Euler(0f, 0f, 37.5f)), Is.LessThan(0.1f));
            _hud.SetActionCost(2, 3);
            _hud.AnimateCostRotation(0.5f);
            AssertCost(2, true, 1f);
            Set("spentCostRotationDuration", 0f);
            _hud.SetActionCost(3, 3);
            AssertCost(2, false, 1f);
        }

        [Test]
        public void Cost_PrefabCountAndSpacing_ReuseHiddenSlotsWhenMaximumChanges()
        {
            _hud.SetActionCost(3, 3);
            Assert.That(_costs.childCount, Is.EqualTo(3));
            Assert.That(_costs.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(10f));
            Assert.That(_costs.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(40f));
            Set("actionCostSpacing", 25f);
            _hud.SetActionCost(1, 2);
            Assert.That(_costs.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(55f));
            Assert.That(_costs.GetChild(2).gameObject.activeSelf, Is.False);
            _hud.SetActionCost(4, 4);
            Assert.That(_costs.childCount, Is.EqualTo(4));
            Assert.That(_costs.GetChild(2).gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Unit_BufferDataSuppliesOwnerHealthAndCost_AndRebindClearsOldSelection()
        {
            var facts = CharacterBufferTestData.EnemyFacts(new EntityId(3), 3);
            var stats = facts.Stats;
            var availableStats = new UnitStatsSnapshot(stats.MoveMethod, stats.MoveRange, stats.MoveCost,
                stats.AppointRange, stats.AttackPower, stats.PhysicalDefense,
                stats.MaxActionCost, stats.AttackCost, stats.RemainingMoveRange, stats.MaxActionCost,
                stats.Priority, stats.Delay, stats.TraitStatuses.ToArray(), stats.Category);
            var enemy = new CharacterData(CharacterBufferTestData.WithSnapshot(facts, stats: availableStats));
            _hud.SetUnit(enemy);
            Assert.That(_healthText.text, Is.EqualTo("3 / 5"));
            Assert.That(_costs.childCount, Is.EqualTo(stats.MaxActionCost));
            AssertCost(0, false, 1f, true);
            _hud.SetStatuses(new[] { new UnitHUDStatus("a", null, "Buff") });
            Dispatch(0, ExecuteEvents.pointerClickHandler);
            _hud.SetUnit(enemy);
            Assert.That(_panel.activeSelf, Is.True);
            _hud.SetUnit(new CharacterData(CharacterBufferTestData.SubFacts(new CharacterInstanceId(1), new EntityId(4))));
            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_statuses.GetChild(0).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Status_PrefabCountSpacingAndReuse_PreserveRotation_AndZeroCountHidesAll()
        {
            var a = new UnitHUDStatus("a", null, "Buff");
            var b = new UnitHUDStatus("b", null, "Debuff");
            _hud.SetStatuses(new[] { a, b });
            Assert.That(_statuses.childCount, Is.EqualTo(2));
            Assert.That(_statuses.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(37f));
            Assert.That(Quaternion.Angle(_statuses.GetChild(1).localRotation,
                Quaternion.Euler(0f, 0f, 12f)), Is.LessThan(0.1f));
            Assert.That(_statuses.GetChild(0).GetComponent<Button>(), Is.Not.Null);
            var first = _statuses.GetChild(0);
            _hud.SetStatuses(new[] { a });
            Assert.That(_statuses.GetChild(1).gameObject.activeSelf, Is.False);
            Set("statusIconSpacing", 20f);
            _hud.SetStatuses(new[] { a, b });
            Assert.That(_statuses.GetChild(0), Is.SameAs(first));
            Assert.That(_statuses.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, Is.EqualTo(47f));
            _hud.SetStatuses(System.Array.Empty<UnitHUDStatus>());
            Assert.That(_statuses.GetChild(0).gameObject.activeSelf, Is.False);
            Assert.That(_statuses.GetChild(1).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Status_MissingDescriptionOrReferences_DisablesInputWithoutThrowing()
        {
            _hud.SetStatuses(new[] { new UnitHUDStatus("a", null, "") });
            var icon = _statuses.GetChild(0).GetComponent<Image>();
            Assert.That(icon.raycastTarget, Is.False);
            Assert.That(icon.GetComponent<Button>().interactable, Is.False);
            Dispatch(0, ExecuteEvents.pointerEnterHandler);
            Dispatch(0, ExecuteEvents.pointerClickHandler);
            Assert.That(_panel.activeSelf, Is.False);
            Set("descriptionPanel", null);
            Set("descriptionText", null);
            Assert.DoesNotThrow(() => _hud.SetStatuses(new[] { new UnitHUDStatus("a", null, "Unwired") }));
            Assert.DoesNotThrow(() => Dispatch(0, ExecuteEvents.pointerClickHandler));
            Assert.That(icon.raycastTarget, Is.False);
        }

        [Test]
        public void FieldUnit_HudPrefabReplacesHealthLabel_FacesCamera_AndClearsOnPoolReturn()
        {
            var hudPrefab = Object.Instantiate(_hud, _root.transform, false);
            hudPrefab.gameObject.SetActive(false);
            var unitObject = Child("FieldUnit");
            var view = unitObject.AddComponent<FieldUnitView>();
            var camera = Child("Camera").AddComponent<Camera>();
            camera.transform.rotation = Quaternion.Euler(35f, 10f, 0f);
            unitObject.transform.position = new Vector3(2f, 0f, 3f);
            unitObject.transform.localScale = Vector3.one * 0.5f;
            view.AttachHealthLabel(_healthText, Vector3.up);
            view.AttachUnitHud(hudPrefab, Vector3.up * 1.5f, 0.02f, camera);
            view.ShowUnitHud(true);
            view.Hud.SetUnit(new CharacterData(CharacterBufferTestData.EnemyFacts(new EntityId(3), 3)));
            typeof(FieldUnitView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            Assert.That(view.HealthLabel.gameObject.activeSelf, Is.False);
            Assert.That(view.Hud.GetComponentInParent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(view.Hud.GetComponentInParent<Canvas>().transform.position, Is.EqualTo(new Vector3(2f, 1.5f, 3f)));
            Assert.That(Quaternion.Angle(view.Hud.GetComponentInParent<Canvas>().transform.rotation,
                camera.transform.rotation), Is.LessThan(0.1f));
            Assert.That(view.Hud.GetComponentInParent<Canvas>().transform.lossyScale.x, Is.EqualTo(0.02f).Within(0.001f));
            view.ResetForPool();
            Assert.That(unitObject.activeSelf, Is.False);
        }

        [Test]
        public void Status_EventDispatchPins_SurvivesReorder_AndToggleCloses()
        {
            var a = new UnitHUDStatus("a", null, "Buff");
            var b = new UnitHUDStatus("b", null, "Debuff");
            _hud.SetStatuses(new[] { a, b });
            Dispatch(0, ExecuteEvents.pointerEnterHandler);
            Assert.That(_description.text, Is.EqualTo("Buff"));
            Dispatch(0, ExecuteEvents.pointerClickHandler);
            Dispatch(0, ExecuteEvents.pointerExitHandler);
            Dispatch(1, ExecuteEvents.pointerEnterHandler);
            Assert.That(_description.text, Is.EqualTo("Buff"));
            _hud.SetStatuses(new[] { b, a });
            Assert.That(_description.text, Is.EqualTo("Buff"));
            Dispatch(1, ExecuteEvents.pointerClickHandler);
            Assert.That(_panel.activeSelf, Is.False);
            Dispatch(0, ExecuteEvents.pointerEnterHandler);
            Assert.That(_description.text, Is.EqualTo("Debuff"));
            ExecuteEvents.Execute(_statuses.GetChild(0).gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
            Dispatch(0, ExecuteEvents.pointerExitHandler);
            Assert.That(_panel.activeSelf, Is.True);
            _hud.SetStatuses(new[] { a });
            Assert.That(_panel.activeSelf, Is.False);
        }

        [Test]
        public void DisableAndClear_RemoveSelectionAndPreview_AndTurnOrderUsesN()
        {
            _hud.SetHealth(8, 10);
            _hud.SetHealthPreview(1);
            _hud.SetStatuses(new[] { new UnitHUDStatus("a", null, "Buff") });
            Dispatch(0, ExecuteEvents.pointerClickHandler);
            _root.SetActive(false);
            _root.SetActive(true);
            Assert.That(_panel.activeSelf, Is.False);
            Assert.That(_healthText.text, Is.EqualTo("8 / 10"));
            _hud.SetTurnOrder(true, 5);
            Assert.That(_order.text, Is.EqualTo("N"));
            _hud.SetTurnOrder(false, 2);
            Assert.That(_order.text, Is.EqualTo("2"));
            _hud.Clear();
            Assert.That(_order.text, Is.Empty);
            Assert.That(_statuses.GetChild(0).gameObject.activeSelf, Is.False);
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return child;
        }
        private TMP_Text Text(string name) => Child(name).AddComponent<TextMeshProUGUI>();
        private void Set(string field, object value) => typeof(UnitHUD)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_hud, value);
        private void Dispatch<T>(int slot, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler =>
            ExecuteEvents.Execute(_statuses.GetChild(slot).gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left }, handler);
        private void AssertCost(int index, bool spent, float alpha, bool enemy = false)
        {
            var image = _costs.GetChild(index).GetComponent<Image>();
            Assert.That(image.gameObject.activeSelf, Is.True);
            Color expected = spent ? Color.gray : enemy ? new Color(1f, 0.5f, 0f, 1f) : new Color(0f, 0.45f, 1f, 1f);
            expected.a *= alpha;
            Assert.That(image.color.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(image.color.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(image.color.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(image.color.a, Is.EqualTo(expected.a).Within(0.001f));
            Assert.That(Quaternion.Angle(image.rectTransform.localRotation,
                Quaternion.Euler(0f, 0f, spent ? 60f : 15f)), Is.LessThan(0.1f));
        }
    }
}
