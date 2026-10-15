using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.Framework
{
    public sealed class AudioSystemTests
    {
        private readonly List<GameObject> _objects = new();
        private AudioSystem _system;

        [SetUp]
        public void SetUp() => _system = new AudioSystem();

        [TearDown]
        public void TearDown()
        {
            _system.Shutdown();
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void Global_ExportsAudioOnlyThroughGlobalContext()
        {
            var builder = new SystemBuilderHelper();
            //GlobalSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            Assert.That(runtime.GetService(SystemGroupsService.Global).AudioServiceCommand, Is.Not.Null);
            Assert.Throws<KeyNotFoundException>(() => runtime.GetService(AudioSystemServiceKey.Audio));
            runtime.Shutdown();
        }

        [Test]
        public void FailedBinding_CanBeRetried_AndDoesNotSignalReady()
        {
            var consumer = CreateObject(true).AddComponent<AudioTestConsumer>();
            consumer.FailBinding = true;
            Assert.Throws<InvalidOperationException>(() => _system.RegisterConsumer(consumer));
            Assert.That(consumer.Service, Is.Null);
            consumer.FailBinding = false;
            _system.RegisterConsumer(consumer);
            _system.RegisterConsumer(consumer);
            Assert.That(consumer.BindCount, Is.EqualTo(2));
            Assert.That(consumer.ReadyCount, Is.Zero);
            Assert.That(consumer.Service, Is.SameAs(_system));
        }

        [Test]
        public void UnregisterAndShutdown_ClearReferences_AndRejectReuse()
        {
            var consumer = CreateObject(true).AddComponent<AudioTestConsumer>();
            _system.RegisterConsumer(consumer);
            _system.UnregisterConsumer(consumer);
            Assert.That(consumer.Service, Is.Null);
            _system.RegisterConsumer(consumer);
            _system.Shutdown();
            _system.Shutdown();
            Assert.That(consumer.Service, Is.Null);
            Assert.That(consumer.UnbindCount, Is.EqualTo(2));
            Assert.Throws<ObjectDisposedException>(() => _system.RegisterConsumer(consumer));
        }

        [Test]
        public void ReadyCallback_IsDeferredForInactiveConsumer_AndResetOnUnbind()
        {
            var consumer = CreateObject(false).AddComponent<AudioTestConsumer>();
            _system.RegisterConsumer(consumer);
            consumer.NotifyAudioReady();
            Assert.That(consumer.ReadyCount, Is.Zero);
            consumer.gameObject.SetActive(true);
            consumer.NotifyAudioReady();
            consumer.NotifyAudioReady();
            Assert.That(consumer.ReadyCount, Is.EqualTo(1));
            _system.UnregisterConsumer(consumer);
            _system.RegisterConsumer(consumer);
            consumer.NotifyAudioReady();
            Assert.That(consumer.ReadyCount, Is.EqualTo(2));
        }

        [Test]
        public void RequesterLookup_RequiresMatchingObject_EvenForUniqueId()
        {
            // Inactive objects avoid Awake/OnEnable and native FMOD bank loading in EditMode.
            var manager = CreateObject(false).AddComponent<AudioManager>();
            JsonUtility.FromJsonOverwrite(
                "{\"playerIdDefinitions\":[{\"key\":\"test\",\"displayName\":\"Test\"}],\"logWarnings\":false}",
                manager);
            var id = JsonUtility.FromJson<AudioPlayerId>("{\"key\":\"test\"}");
            var first = CreateObject(false).AddComponent<FmodEventPlayer>();
            JsonUtility.FromJsonOverwrite("{\"playerKey\":{\"key\":\"test\"}}", first);
            Assert.That(manager.TryRegister(first, out var firstHandle), Is.True);
            Assert.That(manager.TryGetPlayer(id, first.gameObject, out var found), Is.True);
            Assert.That(found, Is.SameAs(first));
            var unrelated = CreateObject(false);
            Assert.That(manager.TryGetPlayer(id, unrelated, out _), Is.False);
            Assert.That(manager.TryGetPlayer(id, null, out _), Is.False);
            Assert.That(manager.Stop(id, unrelated, 0), Is.False);
            Assert.That(manager.Stop(id, unrelated, "slot"), Is.False);

            var second = CreateObject(false).AddComponent<FmodEventPlayer>();
            JsonUtility.FromJsonOverwrite("{\"playerKey\":{\"key\":\"test\"}}", second);
            Assert.That(manager.TryRegister(second, out _), Is.True);
            Assert.That(manager.TryGetPlayer(id, out _), Is.False);
            Assert.That(manager.TryGetPlayer(id, first.gameObject, out found), Is.True);
            Assert.That(found, Is.SameAs(first));
            Assert.That(manager.Unregister(first), Is.True);
            Assert.That(manager.TryGetPlayer(id, first.gameObject, out _), Is.False);
            Assert.That(manager.TryRegister(first, out var nextHandle), Is.True);
            Assert.That(nextHandle, Is.Not.EqualTo(firstHandle));
            Assert.That(manager.TryGetPlayer(firstHandle, out _), Is.False);
            manager.Unregister(first);
            manager.Unregister(second);
        }

        private GameObject CreateObject(bool active)
        {
            var obj = new GameObject("Audio test");
            obj.SetActive(active);
            _objects.Add(obj);
            return obj;
        }
    }

    public sealed class AudioTestConsumer : AudioBase
    {
        public IAudioSystemService Service => AudioService;
        public bool FailBinding;
        public int BindCount;
        public int ReadyCount;
        public int UnbindCount;
        protected override void OnAudioServiceBound()
        {
            BindCount++;
            if (FailBinding) throw new InvalidOperationException("Test binding failure");
        }
        protected override void OnAudioServiceReady() => ReadyCount++;
        protected override void OnAudioServiceUnbound() => UnbindCount++;
    }
}
