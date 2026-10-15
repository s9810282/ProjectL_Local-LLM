using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatLogUITests
    {
        private GameObject _root;
        [TearDown] public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); }

        [Test]
        public void History_PreservesAllResultsThroughEndBattle_AndResetsOnNextPreparation()
        {
            var store = new CombatStateBuffer();
            var write = (ICombatStateBufferStore)store;
            var preparation = new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(new CharacterInstanceId(1), CharacterBufferTestData.MainState())
            }, CharacterBufferTestData.Combat());
            write.InstallBattle(write.PrepareBattle(preparation));
            var first = new CombatResult(1, CombatProcessingKind.Command, 1, true, "");
            var second = new CombatResult(2, CombatProcessingKind.Command, 2, false, "Rejected");
            write.InstallCommand(new PreparedCombatCommand(store.Current, first));
            write.InstallCommand(new PreparedCombatCommand(store.Current, second));
            Assert.That(store.Results, Is.EqualTo(new[] { first, second }));
            Assert.That(store.TryGetLatest(out var latest), Is.True);
            Assert.That(latest, Is.SameAs(second));
            write.EndBattle();
            Assert.That(store.Results.Count, Is.EqualTo(2));
            write.InstallBattle(write.PrepareBattle(preparation));
            Assert.That(store.Results, Is.Empty);
            Assert.That(store.BattleInstanceId, Is.EqualTo(2));
        }

        [Test]
        public void DropDown_KeepsLatestInFixedHeader_AndShowsOnlyPreviousLogsInScrollView()
        {
            var ui = CreateUI(out var scroll, out var content, out var toggle);
            Assert.That(scroll.gameObject.activeSelf, Is.False, "초기 상태에서는 이전 기록을 숨긴다.");
            Assert.That(_root.transform.Find("Latest").gameObject.activeSelf, Is.False, "첫 로그가 오기 전에는 고정 로그 영역도 숨긴다.");
            Assert.That(((RectTransform)scroll.transform).sizeDelta, Is.EqualTo(new Vector2(421, 173)));
            ui.AddLog(null, null, null, 3, false);
            Assert.That(_root.transform.Find("Latest").gameObject.activeInHierarchy, Is.True);
            Assert.That(VisibleRows(content), Is.Empty, "첫 로그는 고정 영역에만 표시한다.");
            ui.AddLog(null, null, null, 7, true);
            Assert.That(ui.ID, Is.EqualTo(UIId.CombatLog));
            Assert.That(ui.ParentID, Is.EqualTo(UIId.None));
            Assert.That(ui.IsExpanded, Is.False);
            Assert.That(VisibleRows(content).Length, Is.EqualTo(1));
            Assert.That(DamageText(VisibleRows(content)[0]), Is.EqualTo("3"));
            Assert.That(LatestCount(), Is.EqualTo("7"));
            Assert.That(scroll.gameObject.activeSelf, Is.False);
            Assert.That(_root.transform.Find("Latest").gameObject.activeInHierarchy, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(ui.IsExpanded, Is.True);
            Assert.That(scroll.gameObject.activeSelf, Is.True);
            Assert.That(((RectTransform)scroll.transform).sizeDelta, Is.EqualTo(new Vector2(421, 173)),
                "펼쳐도 Inspector에서 설정한 Scroll View 크기를 유지한다.");
            Assert.That(VisibleRows(content).Length, Is.EqualTo(1));
            Assert.That(LatestCount(), Is.EqualTo("7"));
            Assert.That(_root.transform.Find("Latest/LatestLog/Dead/Label").GetComponent<TMP_Text>().text, Is.EqualTo("X"));
            ui.AddLog(null, null, null, 9, false);
            Assert.That(LatestCount(), Is.EqualTo("9"));
            Assert.That(VisibleRows(content).Select(row => DamageText(row)),
                Is.EqualTo(new[] { "7", "3" }), "새 로그 도착 시 이전 최신 행이 기록 맨 위로 이동한다.");
            ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(ui.IsExpanded, Is.False, "같은 토글 버튼으로 접는다.");
            Assert.That(scroll.gameObject.activeSelf, Is.False);
            Assert.That(LatestCount(), Is.EqualTo("9"));
            Assert.That(((RectTransform)scroll.transform).sizeDelta, Is.EqualTo(new Vector2(421, 173)));
            ExecuteEvents.Execute(toggle.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
            Assert.That(ui.IsExpanded, Is.True, "키보드/게임패드 Submit도 같은 토글 동작을 사용한다.");
            ui.Hide();
            ui.Open();
            Assert.That(ui.IsExpanded, Is.False);
            Assert.That(scroll.gameObject.activeSelf, Is.False, "UI를 다시 열어도 이전 기록은 숨긴 상태로 시작한다.");
            Assert.That(ui.LogCount, Is.EqualTo(3));
            ui.ClearLogs();
            Assert.That(_root.transform.Find("Latest").childCount, Is.Zero);
            Assert.That(_root.transform.Find("Latest").gameObject.activeSelf, Is.False);
            Assert.That(VisibleRows(content), Is.Empty);
        }

        [Test]
        public void CurrentLogPrefabNames_InitializeAndToggleWithoutInspectorOnClick()
        {
            var ui = CreateUI(out var scroll, out var content, out var toggle);
            Assert.That(scroll.gameObject.activeSelf, Is.False);
            Assert.That(toggle.GetComponent<UIInputBinding>(), Is.Not.Null);
            Assert.That(toggle.onClick.GetPersistentEventCount(), Is.Zero);
            ui.AddLog(null, null, null, 3, false);
            ui.AddLog(null, null, null, 7, false);
            Assert.That(ui.LogCount, Is.EqualTo(2));
            Assert.That(_root.transform.Find("Latest").GetComponentsInChildren<TMP_Text>()
                .Single(label => label.name == "DMG/HEAL").text, Is.EqualTo("7"));
            ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(scroll.gameObject.activeSelf, Is.True);
            Assert.That(content.GetComponentsInChildren<TMP_Text>().Single(label => label.name == "DMG/HEAL").text,
                Is.EqualTo("3"));
            ExecuteEvents.Execute(toggle.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(scroll.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ResultHistory_CatchesMultiplePublicationsWhileHidden_WithoutDuplicatingAttackFieldAndEvent()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            var main = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState()).Acquired.Single();
            var preparation = new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(main.Id, CharacterBufferTestData.MainState()),
                CharacterBufferTestData.EnemyFacts(new EntityId(2))
            }, CharacterBufferTestData.Combat());
            buffers.BattleWriteSink.PrepareBattle(preparation);
            var ui = CreateUI(out _, out var content, out _);
            ui.BindUIService(services);
            ui.Hide();
            PublishAttack(buffers, 1, 3);
            PublishAttack(buffers, 2, 5);
            Assert.That(buffers.CombatResultHistoryReadSink.Results.Count, Is.EqualTo(2));
            ui.Open();
            ui.RefreshLogs();
            Assert.That(ui.LogCount, Is.EqualTo(2), "필드와 사건에 같은 공격이 있어도 한 번만 기록한다.");
            Assert.That(LatestCount(), Is.EqualTo("5"));
            Assert.That(DamageText(VisibleRows(content)[0]), Is.EqualTo("3"));
            buffers.BattleWriteSink.EndBattle();
            ui.RefreshLogs();
            Assert.That(ui.LogCount, Is.EqualTo(2), "종료 후에도 전투 기록을 유지한다.");
            buffers.BattleWriteSink.PrepareBattle(preparation);
            ui.RefreshLogs();
            Assert.That(buffers.CombatResultHistoryReadSink.Results, Is.Empty);
            Assert.That(ui.LogCount, Is.Zero);
        }

        private static void PublishAttack(IBufferQuery buffers, int sequence, int damage)
        {
            var cell = new CellCoord(1, 1);
            var attack = AttackResult.FromFacts(new EntityId(1), new EntityId(2), Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 1, 0, 1), new[] { cell },
                new[] { new AttackHitResult(new EntityId(2), 10, 10 - damage, AttackTargetRole.Primary) },
                new AttackCellSelection(cell, new[] { cell }, Array.Empty<CellCoord>()));
            var result = new CombatResult(sequence, CombatProcessingKind.Command, sequence, true, "",
                attack: attack, events: new CombatEvent[] { new CombatAttackEvent(attack) });
            // 설치 지점부터 UI까지 검증한다. 공격 계산 자체는 Core 테스트에서 담당한다.
            ((ICombatStateBufferStore)buffers.CombatStateReadSink).InstallCommand(
                new PreparedCombatCommand(buffers.CombatStateReadSink.Current, result));
        }

        private CombatLogUI CreateUI(out ScrollRect scroll, out RectTransform content, out Button toggle)
        {
            _root = new GameObject("Combat Log", typeof(RectTransform));
            _root.SetActive(false);
            var latest = (RectTransform)Child("Latest", _root.transform).transform;
            scroll = Child("History", _root.transform).AddComponent<ScrollRect>();
            ((RectTransform)scroll.transform).sizeDelta = new Vector2(421, 173);
            scroll.viewport = (RectTransform)Child("Viewport", scroll.transform).transform;
            content = (RectTransform)Child("Content", scroll.viewport).transform;
            scroll.content = content;
            var template = (RectTransform)Child("Log", content).transform;
            template.sizeDelta = new Vector2(300, 36);
            foreach (string name in new[] { "Origin", "Action", "Target", "Dead" })
                Child(name, template).AddComponent<Image>();
            Child("DMG/HEAL", template).AddComponent<TextMeshProUGUI>();
            toggle = Child("Toggle", _root.transform).AddComponent<Button>();
            var ui = _root.AddComponent<CombatLogUI>();
            typeof(CombatLogUI).GetField("toggleButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, toggle);
            typeof(CombatLogUI).GetField("latestLogRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, latest);
            typeof(CombatLogUI).GetField("scrollView", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, scroll);
            _root.SetActive(true);
            return ui;
        }

        private static Transform[] VisibleRows(RectTransform content) =>
            content.Cast<Transform>().Where(row => row.gameObject.activeSelf).ToArray();

        private string LatestCount() =>
            DamageText(_root.transform.Find("Latest/LatestLog"));

        private static string DamageText(Transform row) =>
            row.Cast<Transform>().Single(child => child.name == "DMG/HEAL").GetComponent<TMP_Text>().text;

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
