using System.Reflection;
using System;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.Framework
{
    public sealed class DeckButtonUITests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void DeckButtonAndOutsideClick_ClosePanel_WhilePanelBackgroundDoesNot()
        {
            var ui = CreateUI(out var panel, out var content, out var open);
            ui.Open();
            ExecuteEvents.Execute(open.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
            Assert.That(panel.activeSelf, Is.True);
            var outside = _root.transform.Find("DeckOutsideClickArea").gameObject;
            Assert.That(outside.activeSelf, Is.True);

            ExecuteEvents.ExecuteHierarchy(content.gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.True, "패널 내부 빈 영역의 클릭은 부모 덱 버튼으로 전달하지 않는다.");
            ExecuteEvents.Execute(outside,
                new PointerEventData(null) { button = PointerEventData.InputButton.Right },
                ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.True);
            ExecuteEvents.Execute(outside,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(outside.activeSelf, Is.False);

            ExecuteEvents.Execute(open.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
            ExecuteEvents.Execute(open.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
            Assert.That(panel.activeSelf, Is.False);
            ui.OpenPanel();
            ui.Hide();
            Assert.That(outside.activeSelf, Is.False);
        }

        [Test]
        public void Deck_ShowsMainFirstAndPersistentSubsIncludingDuplicates_AndReusesButtonsAfterReset()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            var ui = CreateUI(out var panel, out var content, out var open);
            ui.BindUIService(services);
            buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Demon, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_ShotGun, CharacterOwnershipRole.Sub)
            }, CharacterBufferTestData.MainState());

            ui.Open();
            Assert.That(panel.activeSelf, Is.False);
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(ui.ID, Is.EqualTo(UIId.Deck));
            Assert.That(ui.ParentID, Is.EqualTo(UIId.None));
            Assert.That(buffers.CharacterReadSink.Characters.Count, Is.Zero);
            Assert.That(content.GetComponentsInChildren<Button>().Length, Is.EqualTo(6));
            var grid = content.GetComponent<GridLayoutGroup>();
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(5));
            Assert.That(grid.cellSize, Is.EqualTo(new Vector2(200, 250)), "덱 항목은 프리팹에 제작된 크기를 유지해야 한다.");
            var first = content.GetChild(0).GetComponent<Button>();
            Assert.That(((RectTransform)first.transform).rect.size, Is.EqualTo(new Vector2(200, 250)));
            Assert.That(first.name, Does.StartWith("Deck_Unit_MainCharacter_"));
            Assert.That(content.GetChild(1).name, Does.StartWith("Deck_Unit_Akimbo_"));
            Assert.That(content.GetChild(2).name, Does.StartWith("Deck_Unit_Akimbo_"));
            Assert.That(typeof(DeckButtonUI).GetMethod("DeckChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(ui, null), Is.False, "메인을 앞으로 배치한 뒤 변경 감지도 같은 순서를 사용해야 한다.");
            Assert.That(first.image.enabled, Is.True, "SO가 없어도 기본 버튼이 보여야 한다.");

            // 열린 패널에서 Buffer 변경 감지와 풀 반환을 확인한다.
            buffers.RunProgressWriteSink.ResetRunProgress();
            typeof(DeckButtonUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Assert.That(content.GetComponentsInChildren<Button>().Length, Is.Zero);
            Assert.That(content.childCount, Is.EqualTo(6));
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.False);
            buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub)
            });
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(content.GetComponentsInChildren<Button>().Length, Is.EqualTo(1));
            Assert.That(content.GetChild(0).GetComponent<Button>(), Is.SameAs(first));
            buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState());
            typeof(DeckButtonUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Assert.That(content.GetComponentsInChildren<Button>().Length, Is.EqualTo(2));
            Assert.That(content.GetChild(0).name, Does.StartWith("Deck_Unit_MainCharacter_"));
            Assert.That(content.GetChild(1).name, Does.StartWith("Deck_Unit_Sniper_"));
            ui.Hide();
            ui.Open();
            Assert.That(panel.activeSelf, Is.False);
        }

        [Test]
        public void DeckItems_HoverPreview_ClickPins_DuplicateCardsUseTheirOwnSouls_AndClosingDeckHidesCard()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;
            var buffers = services.GetService(BufferSystemKey.Buffer);
            var ui = CreateUI(out var panel, out var content, out var open);
            var card = Child("Unit Card", _root.transform).AddComponent<UnitCardUI>();
            var details = Child("Details", card.transform).AddComponent<TextMeshProUGUI>();
            // 표시 참조는 RequireComponent로 함께 붙는 UnitCardView에 있다(손패 카드화 H1).
            typeof(UnitCardView).GetField("detailsText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card.View, details);
            SetField(ui, "unitCard", card);
            ui.BindUIService(services);
            ui.GetUIData(new DeckInGameQuery());
            var owned = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }).Acquired;
            buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(owned[0].Id, 0, 2) }));
            ui.Open();
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            var first = content.GetChild(0).GetComponent<Button>();
            var second = content.GetChild(1).GetComponent<Button>();
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(card.OwnedCharacterId, Is.EqualTo(owned[0].Id));
            Assert.That(details.text, Is.EqualTo("HP : 12 / 12\nDEF : 0\nATK : 7\nATK COST : 1\nMAX COST : 4\nPRIORITY : 1"));
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            Assert.That(card.gameObject.activeSelf, Is.False);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            Assert.That(card.gameObject.activeSelf, Is.True, "Click으로 고정한 카드는 Hover 이탈로 닫히지 않는다.");
            buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(owned[0].Id, 2, 3) }));
            typeof(UnitCardUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(card, null);
            Assert.That(details.text, Does.Contain("HP : 13 / 13"));
            ExecuteEvents.Execute(second.gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            Assert.That(card.OwnedCharacterId, Is.EqualTo(owned[0].Id), "다른 유닛의 Hover는 클릭 고정을 유지한다.");
            Assert.That(details.text, Does.Contain("HP : 13 / 13"));
            ExecuteEvents.Execute(second.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            Assert.That(card.gameObject.activeSelf, Is.True, "다른 슬롯의 이탈은 현재 카드를 닫지 않는다.");
            ExecuteEvents.Execute(second.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(card.OwnedCharacterId, Is.EqualTo(owned[1].Id), "다른 유닛 클릭은 고정 대상을 전환한다.");
            Assert.That(details.text, Does.Contain("HP : 10 / 10"));
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            Assert.That(card.OwnedCharacterId, Is.EqualTo(owned[1].Id));
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            ExecuteEvents.Execute(second.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(card.gameObject.activeSelf, Is.False);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            Assert.That(card.gameObject.activeSelf, Is.True, "고정 해제 후 Hover 미리보기가 다시 동작한다.");
            Assert.That(card.OwnedCharacterId, Is.EqualTo(owned[0].Id));
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(card.gameObject.activeSelf, Is.False);
            ExecuteEvents.Execute(open.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(first.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            buffers.RunProgressWriteSink.ResetRunProgress();
            typeof(DeckButtonUI).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Assert.That(card.gameObject.activeSelf, Is.False, "선택된 보유 카드가 제거되면 표시도 닫힌다.");
        }

        private sealed class DeckInGameQuery : IHasInGameQuery
        {
            public IInGameQuery InGameQuery { get; } = new CharacterRosterDatabase();
        }

        private DeckButtonUI CreateUI(out GameObject panel, out RectTransform content,
            out Button open)
        {
            _root = new GameObject("Deck UI", typeof(RectTransform), typeof(Canvas));
            _root.SetActive(false);
            var ui = _root.AddComponent<DeckButtonUI>();
            open = Child("Open", _root.transform).AddComponent<Button>();
            panel = Child("Panel", _root.transform);
            var scroll = Child("Scroll", panel.transform).AddComponent<ScrollRect>();
            content = (RectTransform)Child("Content", scroll.transform).transform;
            scroll.content = content;
            var template = Child("Template", _root.transform);
            ((RectTransform)template.transform).sizeDelta = new Vector2(200, 250);
            var image = template.AddComponent<Image>();
            var button = template.AddComponent<Button>();
            button.targetGraphic = image;
            template.SetActive(false);
            SetField(ui, "openButton", open);
            SetField(ui, "panel", panel);
            SetField(ui, "scrollView", scroll);
            SetField(ui, "deckButtonPrefab", button);
            return ui;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetField(DeckButtonUI ui, string name, object value) =>
            typeof(DeckButtonUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, value);
    }
}
