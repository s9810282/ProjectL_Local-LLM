using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.NodeEvent.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.NodeEvent.View;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    public sealed class StageBufferInteractionTests
    {
        private sealed class BuffContext
        {
            public IBufferQuery BuffQuery { get; }
            public ISystemServiceStoreReader Services { get; }
            public BuffContext(ISystemServiceStoreReader services)
            {
                Services = services;
                BuffQuery = services.GetService(BufferSystemKey.Buffer);
            }
        }

        private sealed class UIService : IUISystemService
        {
            public Action<UIId> OnOpen;
            public UIId Closed;
            public void OpenUI(UIId id) => OnOpen?.Invoke(id);
            public void OpenUI(UIId id, bool isFixable) => OpenUI(id);
            public void OpenGlobalUI() { }
            public void OpenChildUI(UIId parentId, UIId childId) => OpenUI(childId);
            public void CloseChildUI(UIId parentId) { }
            public void CloseUI(UIId id) => Closed = id;
            public void BackCloseUI() { }
            public void HideAllUI() { }
        }

        private static BuffContext CreateBuffer()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var context = new BuffContext(builder.BuildSystemReader().ServiceStoreReader);
            context.BuffQuery.CurrentProgressWriteSink.SelectMap(1);
            return context;
        }

        [Test]
        public void BufferFactory_SharesSinks_AndCreatesIndependentLifetime()
        {
            var first = CreateBuffer().BuffQuery;
            var second = CreateBuffer().BuffQuery;
            first.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "First");
            Assert.That(first.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(101));
            Assert.That(second.CurrentProgressReadSink.HasSelection, Is.False);
            first.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(102), "Second");
            Assert.That(first.CurrentProgressReadSink.CurrentStage.StageName, Is.EqualTo("Second"));
            first.CurrentProgressWriteSink.Clear();
            Assert.That(first.CurrentProgressReadSink.HasSelection, Is.False);
        }

        [Test]
        public void InvalidSelection_DoesNotReplaceExistingRecord()
        {
            var buffer = CreateBuffer().BuffQuery;
            buffer.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "First");
            Assert.Throws<ArgumentException>(() => buffer.CurrentProgressWriteSink.SelectStage(
                StageNodeId.Empty, new StageKey(102), "Invalid"));
            Assert.That(buffer.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(101));
        }

        [Test]
        public void NodeEvent_RebindsWithoutAccumulation_AndClears()
        {
            var go = new GameObject("StageNode Test");
            try
            {
                var node = go.AddComponent<UnityEventNode>();
                int first = 0, second = 0;
                node.RegisterEvent(() => first++);
                node.RegisterEvent(() => second++);
                node.InvokeEvent();
                Assert.That(first, Is.Zero);
                Assert.That(second, Is.EqualTo(1));
                node.ClearEvent();
                node.InvokeEvent();
                Assert.That(second, Is.EqualTo(1));
                node.RegisterEvent(() => second++);
                go.SetActive(false);
                node.InvokeEvent();
                Assert.That(second, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void StageUI_ReadsBufferOnEveryOpen_AndHidesWithoutClearingSelection()
        {
            var data = CreateBuffer();
            var go = new GameObject("StageUI Test", typeof(RectTransform));
            go.SetActive(false);
            try
            {
                var ui = go.AddComponent<StageUI>();
                var text = go.AddComponent<TMPro.TextMeshProUGUI>();
                typeof(StageUI).GetField("stageNameText",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(ui, text);
                var service = new UIService();
                ui.BindUISystemService(service);
                ui.BindUIService(data.Services);
                ui.GetUIData(data.BuffQuery);
                data.BuffQuery.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "First");
                ui.Open();
                Assert.That(text.text, Is.EqualTo("First"));
                ui.Hide();
                Assert.That(go.activeSelf, Is.False);
                data.BuffQuery.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(102), "Second");
                ui.Open();
                Assert.That(text.text, Is.EqualTo("Second"));
                Assert.That(go.activeSelf, Is.True);
                Assert.That(data.BuffQuery.CurrentProgressReadSink.HasSelection, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
