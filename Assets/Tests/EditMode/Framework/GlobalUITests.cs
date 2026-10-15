using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEditor;
using ProjectL.Systems;
using ProjectL.Systems.Factory.Runtime.Buffer;

namespace ProjectL.Tests.Framework
{
    public sealed class GlobalUITestView : GlobalUIBase
    {
        public UIId TestId;
        public int OpenCount;
        public int BindCount;
        public override UIId ID => TestId;
        public override void BindUIService(ISystemServiceStoreReader resolver) => BindCount++;
        public override void GetUIData(IServiceModule query) { }
        public override void Open() { OpenCount++; gameObject.SetActive(true); }
        public override void Hide() => gameObject.SetActive(false);
    }

    public sealed class GlobalUITests
    {
        private sealed class RegularUI : IUIService
        {
            public bool IsVisible;
            public int OpenCount;
            public void BindUISystemService(IUISystemService service) { }
            public void ExportUIService(IUIRegistry registry) { }
            public void GetUIData(IServiceModule query) { }
            public void BindUIService(ISystemServiceStoreReader resolver) { }
            public void Open() { IsVisible = true; OpenCount++; }
            public void Hide() => IsVisible = false;
        }

        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GlobalUITestView CreateGlobal(UIId id)
        {
            var go = new GameObject("Global UI Test");
            _objects.Add(go);
            go.SetActive(false);
            var ui = go.AddComponent<GlobalUITestView>();
            ui.TestId = id;
            return ui;
        }

        private static IUISystemService CreateSystem(bool initialize = true)
        {
            Type type = typeof(IUISystemService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.UISystem", true);
            var system = (IUISystemService)Activator.CreateInstance(type, true);
            if (initialize)
            {
                ((ISystemPostBindService)system).PostBindService(
                    new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
                ((ISystemInitialize)system).Initialize();
            }
            return system;
        }

        [Test]
        public void GlobalCanvasPrefab_CreatesOneLiveUnitCard_AndRetainsViewsAcrossRebinds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ProjectL/Prefabs/UI/GlobalUICanvas.prefab");
            Assert.That(prefab, Is.Not.Null);
            var canvas = UnityEngine.Object.Instantiate(prefab);
            _objects.Add(canvas);
            var system = CreateSystem(false);
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            ((ISystemBindScene)system).BindScene();
            ((ISystemInitData<IServiceModule>)system).InitData(system);
            ((ISystemPostBindService)system).PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            ((ISystemInitialize)system).Initialize();
            system.OpenGlobalUI();

            var main = canvas.GetComponentInChildren<MainCharactorUI>(true);
            var deck = canvas.GetComponentInChildren<DeckButtonUI>(true);
            var cards = canvas.GetComponentsInChildren<UnitCardUI>(true);
            Assert.That(main.gameObject.activeSelf && deck.gameObject.activeSelf, Is.True);
            Assert.That(cards.Length, Is.EqualTo(1));
            Assert.That(cards[0].gameObject.scene.IsValid(), Is.True);
            Assert.That(cards[0].gameObject.activeSelf, Is.False);

            ((ISystemBindScene)system).BindScene();
            ((ISystemBindScene)system).BindScene();
            Assert.That(main.gameObject.activeSelf && deck.gameObject.activeSelf, Is.True);
            Assert.That(canvas.GetComponentsInChildren<UnitCardUI>(true), Has.Length.EqualTo(1));
            Assert.That(canvas.transform.localScale, Is.EqualTo(Vector3.one));
            ((ISystemShutdown)system).Shutdown();
        }

        [Test]
        public void Initialize_HidesInitiallyVisibleRootsAndChildren_UntilOpenedThroughService()
        {
            var system = CreateSystem(false);
            var registry = (IUIRegistry)system;
            var root = new RegularUI { IsVisible = true };
            var child = new RegularUI { IsVisible = true };
            var global = CreateGlobal(UIId.Setting);
            global.gameObject.SetActive(true);
            registry.AddUI(UIId.Stage, root);
            registry.AddUI(UIId.StageResult, child, UIId.Stage);
            registry.AddUI(global.ID, global);
            ((ISystemPostBindService)system).PostBindService(
                new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);

            ((ISystemInitialize)system).Initialize();

            Assert.That(root.IsVisible, Is.False);
            Assert.That(child.IsVisible, Is.False);
            Assert.That(global.gameObject.activeSelf, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                system.OpenChildUI(UIId.Stage, UIId.StageResult));

            system.OpenUI(UIId.Stage);
            system.OpenChildUI(UIId.Stage, UIId.StageResult);
            system.OpenGlobalUI();

            Assert.That(root.IsVisible, Is.True);
            Assert.That(child.IsVisible, Is.True);
            Assert.That(global.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void BindScene_CollectsGlobalUIOnce_AndPreservesOpenStateAcrossRebinds()
        {
            var first = CreateGlobal((UIId)10001);
            var second = CreateGlobal((UIId)10002);
            var system = CreateSystem(false);
            ((ISystemBindScene)system).BindScene();
            ((ISystemPostBindService)system).PostBindService(
                new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
            ((ISystemInitialize)system).Initialize();
            system.OpenGlobalUI();
            system.CloseUI(second.ID);

            var later = CreateGlobal((UIId)10003);
            ((ISystemBindScene)system).BindScene();

            Assert.That(first.BindCount, Is.EqualTo(1));
            Assert.That(second.BindCount, Is.EqualTo(1));
            Assert.That(later.BindCount, Is.Zero);
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(second.gameObject.activeSelf, Is.False);

            var child = new RegularUI();
            ((IUIRegistry)system).AddUI((UIId)10004, child, first.ID);
            system.OpenChildUI(first.ID, (UIId)10004);
            system.BackCloseUI();
            Assert.That(child.IsVisible, Is.False);
            Assert.That(first.gameObject.activeSelf, Is.True);

            system.OpenGlobalUI();
            Assert.That(first.OpenCount, Is.EqualTo(2));
            Assert.That(second.OpenCount, Is.EqualTo(2));
            Assert.That(later.OpenCount, Is.Zero);
            ((ISystemShutdown)system).Shutdown();
            ((ISystemBindScene)system).BindScene();
            ((ISystemPostBindService)system).PostBindService(
                new SystemBuilderHelper().BuildSystemReader().ServiceStoreReader);
            ((ISystemInitialize)system).Initialize();
            system.OpenGlobalUI();
            Assert.That(later.OpenCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenGlobalUI_OpensOnlyGlobalRoots_AndPreservesRegularHistory()
        {
            var system = CreateSystem();
            var registry = (IUIRegistry)system;
            var first = CreateGlobal(UIId.Setting);
            var second = CreateGlobal(UIId.Stage);
            var regular = new RegularUI();
            var child = new RegularUI();
            registry.AddUI(first.ID, first);
            registry.AddUI(second.ID, second);
            registry.AddUI(UIId.Loading, regular);
            registry.AddUI(UIId.StageSelect, child, first.ID);
            system.OpenUI(UIId.Loading);

            system.OpenGlobalUI();
            system.OpenGlobalUI();

            Assert.That(first.OpenCount, Is.EqualTo(2));
            Assert.That(second.OpenCount, Is.EqualTo(2));
            Assert.That(regular.OpenCount, Is.EqualTo(1));
            Assert.That(regular.IsVisible, Is.True);
            Assert.That(child.OpenCount, Is.Zero);
            system.BackCloseUI();
            system.BackCloseUI();
            Assert.That(regular.IsVisible, Is.False);
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(second.gameObject.activeSelf, Is.True);

            system.OpenChildUI(first.ID, UIId.StageSelect);
            system.BackCloseUI();
            Assert.That(child.IsVisible, Is.False);
            Assert.That(first.gameObject.activeSelf, Is.True);
            system.HideAllUI();
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(second.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void OpenUI_GlobalRoot_UsesPersistentBehavior_AndCanBeClosedExplicitly()
        {
            var system = CreateSystem();
            var ui = CreateGlobal(UIId.Stage);
            ((IUIRegistry)system).AddUI(ui.ID, ui);
            system.OpenUI(ui.ID);
            system.BackCloseUI();
            Assert.That(ui.gameObject.activeSelf, Is.True);
            system.CloseUI(ui.ID);
            Assert.That(ui.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GlobalUI_RequiresInitializationAndRootRegistration()
        {
            var system = CreateSystem(false);
            Assert.Throws<InvalidOperationException>(() => system.OpenGlobalUI());
            var ui = CreateGlobal(UIId.Stage);
            Assert.That(ui.ParentID, Is.EqualTo(UIId.None));
            Assert.Throws<InvalidOperationException>(() =>
                ((IUIRegistry)system).AddUI(ui.ID, ui, UIId.Setting));
        }
    }
}
