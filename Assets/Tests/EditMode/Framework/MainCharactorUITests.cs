using System;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Tests.Framework
{
    public sealed class MainCharactorUITests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void BufferChanges_UpdateVisibleUI_AndReopeningReadsMissedChanges()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            IBufferQuery buffers = services.GetService(BufferSystemKey.Buffer);
            _root = new GameObject("Main UI Test", typeof(RectTransform));
            _root.SetActive(false);
            var ui = _root.AddComponent<MainCharactorUI>();
            var hp = Child<Slider>("HP");
            var hpText = Child<TextMeshProUGUI>("HP Text");
            var gold = Child<TextMeshProUGUI>("Gold");
            SetField(ui, "hp", hp);
            SetField(ui, "hpText", hpText);
            SetField(ui, "goldText", gold);
            ui.BindUIService(services);
            ui.Open();
            Assert.That(hpText.text, Is.EqualTo("0 / 0"));
            Assert.That(gold.text, Is.EqualTo("0"));

            var initial = CharacterBufferTestData.MainState();
            var owned = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, initial).Acquired[0];
            TickUI(ui);
            Assert.That(hpText.text, Is.EqualTo("10 / 10"));
            // 전투 임시 최대 체력 보너스까지 Slider와 텍스트에 반영한다.
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(owned.Id, initial, temporaryMaxHealth: 3)
            }, CharacterBufferTestData.Combat()));
            TickUI(ui);
            Assert.That(hp.maxValue, Is.EqualTo(13));
            Assert.That(hp.value, Is.EqualTo(10));
            Assert.That(hp.interactable, Is.False);
            Assert.That(hp.handleRect, Is.Null);

            var damaged = CharacterBufferTestData.MainState(7);
            buffers.BattleWriteSink.Apply(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(initial, damaged)));
            buffers.CurrencyWriteSink.SetAmount(42);
            TickUI(ui);
            Assert.That(hpText.text, Is.EqualTo("7 / 13"));
            Assert.That(hp.value, Is.EqualTo(7));
            Assert.That(gold.text, Is.EqualTo("42"));

            ui.Hide();
            buffers.CurrencyWriteSink.SetAmount(80);
            buffers.BattleWriteSink.EndBattle();
            Assert.That(gold.text, Is.EqualTo("42"));
            ui.Open();
            Assert.That(gold.text, Is.EqualTo("80"));
            Assert.That(hpText.text, Is.EqualTo("7 / 10"));
            buffers.RunProgressWriteSink.ResetRunProgress();
            TickUI(ui);
            Assert.That(hpText.text, Is.EqualTo("0 / 0"));
            Assert.That(gold.text, Is.EqualTo("0"));
            buffers.CurrencyWriteSink.SetAmount(int.MaxValue);
            TickUI(ui);
            Assert.That(gold.text, Is.EqualTo("2147483647"));
            buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState(int.MaxValue, int.MaxValue));
            TickUI(ui);
            Assert.That(hpText.text, Is.EqualTo("2147483647 / 2147483647"));
        }

        [Test]
        public void CategoryChange_UpdatesBothSprites_AndResetClearsImages()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            _root = new GameObject("Main UI Test", typeof(RectTransform));
            _root.SetActive(false);
            var ui = _root.AddComponent<MainCharactorUI>();
            var portrait = Child<Image>("Portrait");
            var synergy = Child<Image>("Synergy");
            SetField(ui, "portrait", portrait);
            SetField(ui, "synergy", synergy);
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                Type visualType = typeof(MainCharactorUI).GetNestedType("CategoryVisual", BindingFlags.NonPublic);
                object visual = Activator.CreateInstance(visualType);
                visualType.GetField("Category").SetValue(visual, CategoryKey.Rider);
                visualType.GetField("Portrait").SetValue(visual, sprite);
                visualType.GetField("Synergy").SetValue(visual, sprite);
                Array visuals = Array.CreateInstance(visualType, 1);
                visuals.SetValue(visual, 0);
                SetField(ui, "categoryVisuals", visuals);
                ui.BindUIService(services);
                ui.Open();
                var initial = CharacterBufferTestData.MainState();
                var owned = buffers.CharacterWriteSink.Acquire(new[]
                {
                    new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
                }, initial).Acquired[0];
                var main = CharacterBufferTestData.MainFacts(owned.Id, initial);
                buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                    new[] { main }, CharacterBufferTestData.Combat()));
                TickUI(ui);
                Assert.That(portrait.enabled, Is.False);
                var changed = CharacterBufferTestData.WithStatuses(main,
                    CategorySnapshot.CreateRider(false));
                buffers.BattleWriteSink.Apply(new CharacterChanges(
                    new[] { new CharacterStateChange(main, changed) }, Array.Empty<SoulCountChange>()));
                TickUI(ui);
                Assert.That(portrait.sprite, Is.SameAs(sprite));
                Assert.That(synergy.sprite, Is.SameAs(sprite));
                Assert.That(portrait.enabled && synergy.enabled, Is.True);
                buffers.RunProgressWriteSink.ResetRunProgress();
                TickUI(ui);
                Assert.That(portrait.sprite, Is.Null);
                Assert.That(synergy.enabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(sprite); }
        }

        private T Child<T>(string name) where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return child.AddComponent<T>();
        }

        private static void TickUI(MainCharactorUI ui) =>
            typeof(MainCharactorUI).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ui, null);

        private static void SetField(MainCharactorUI ui, string name, object value) =>
            typeof(MainCharactorUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, value);
    }
}
