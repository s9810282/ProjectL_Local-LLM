using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// View 계획 D.6a: 표시 재사용 구조(설계 §6.1, W19)와 좌표 변환.
    /// 같은 값 재적용에 추가 대여·반환·속성 쓰기가 없고, 추가·제거된 대상만 바뀌는지 본다. 성능 측정은 §8 X4.
    /// </summary>
    public sealed class CombatDisplayReuseTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects)
            {
                if (item != null)
                    UnityEngine.Object.DestroyImmediate(item);
            }

            _objects.Clear();
        }

        #region EntityViewSet

        [Test]
        public void SameTargets_ReuseViews_WithoutRentOrReturn()
        {
            var pool = new CountingPool();
            var set = new EntityViewSet<FakeView>(pool.Rent, pool.Return);
            Render(set, (1, EntityKey.Unit_Akimbo), (2, EntityKey.Unit_Sniper));
            set.TryGet(new EntityId(1), out FakeView first);

            Render(set, (1, EntityKey.Unit_Akimbo), (2, EntityKey.Unit_Sniper));

            Assert.That(pool.Rented, Is.EqualTo(2));
            Assert.That(pool.Returned, Is.Zero);
            Assert.That(set.TryGet(new EntityId(1), out FakeView again) && ReferenceEquals(first, again), Is.True);
        }

        [Test]
        public void AddedAndRemovedTargets_ChangeOnlyThoseViews()
        {
            var pool = new CountingPool();
            var set = new EntityViewSet<FakeView>(pool.Rent, pool.Return);
            Render(set, (1, EntityKey.Unit_Akimbo), (2, EntityKey.Unit_Sniper));
            set.TryGet(new EntityId(1), out FakeView kept);

            Render(set, (1, EntityKey.Unit_Akimbo), (3, EntityKey.Unit_Demon));

            Assert.That(pool.Rented, Is.EqualTo(3), "새로 보인 3만 빌린다.");
            Assert.That(pool.Returned, Is.EqualTo(1), "사라진 2만 돌려준다.");
            Assert.That(pool.LastReturnedKey, Is.EqualTo(EntityKey.Unit_Sniper));
            Assert.That(set.TryGet(new EntityId(1), out FakeView same) && ReferenceEquals(kept, same), Is.True);
            Assert.That(set.TryGet(new EntityId(2), out _), Is.False);
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void ChangedKey_ReturnsOldViewAndRentsNewKind()
        {
            var pool = new CountingPool();
            var set = new EntityViewSet<FakeView>(pool.Rent, pool.Return);
            Render(set, (1, EntityKey.Unit_Akimbo));

            Render(set, (1, EntityKey.Unit_ShotGun));

            Assert.That(pool.Rented, Is.EqualTo(2));
            Assert.That(pool.Returned, Is.EqualTo(1));
            Assert.That(set.TryGet(new EntityId(1), out FakeView view) && view.Key == EntityKey.Unit_ShotGun, Is.True);
        }

        [Test]
        public void Clear_ReturnsEveryView_AndContinuesAfterFailure()
        {
            var pool = new CountingPool { ThrowOnKey = EntityKey.Unit_Akimbo };
            var set = new EntityViewSet<FakeView>(pool.Rent, pool.Return);
            Render(set, (1, EntityKey.Unit_Akimbo), (2, EntityKey.Unit_Sniper));

            Assert.Throws<InvalidOperationException>(set.Clear);
            Assert.That(pool.Returned, Is.EqualTo(1), "실패한 하나 외의 View는 돌려준다.");
            Assert.That(set.Count, Is.Zero);
        }

        [Test]
        public void Acquire_OutsideRender_IsRejected()
        {
            var pool = new CountingPool();
            var set = new EntityViewSet<FakeView>(pool.Rent, pool.Return);

            Assert.Throws<InvalidOperationException>(() => set.Acquire(new EntityId(1), EntityKey.Unit_Akimbo));
            Assert.Throws<InvalidOperationException>(set.End);
        }

        #endregion

        #region View 속성 쓰기

        [Test]
        public void FieldUnitView_SameValues_WriteNothing_AndRestoreExternalChanges()
        {
            FieldUnitView view = Create<FieldUnitView>();
            var position = new Vector3(1f, 0f, 2f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            Assert.That(view.Apply(position, rotation, 5, 10, true), Is.True);
            Assert.That(view.Apply(position, rotation, 5, 10, true), Is.False, "같은 값이면 쓰지 않는다.");

            // 숨김·풀 재사용처럼 다른 경로가 실제 표시를 바꾸면 Core 값이 같아도 되돌린다.
            view.transform.position = Vector3.zero;
            view.gameObject.SetActive(false);
            Assert.That(view.Apply(position, rotation, 5, 10, true), Is.True);
            Assert.That(view.transform.position, Is.EqualTo(position));
            Assert.That(view.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void FieldUnitView_ResetForPool_HidesAndForgetsIdentity()
        {
            FieldUnitView view = Create<FieldUnitView>();
            view.Bind(new EntityId(4), EntityKey.Unit_Demon);

            view.ResetForPool();

            Assert.That(view.EntityId, Is.EqualTo(EntityId.Empty));
            Assert.That(view.gameObject.activeSelf, Is.False);
        }

        #endregion

        #region 칸 강조 층 (E2, 설계 §21.1)

        [Test]
        public void HighlightLayers_PriorityIsHoverAffectedDraftRangeActorStatic()
        {
            const FieldHighlightLayer all = FieldHighlightLayer.ActiveActor | FieldHighlightLayer.Range |
                FieldHighlightLayer.Affected | FieldHighlightLayer.MovementDraft;

            Assert.That(FieldHighlightLayers.Resolve(true, CombatHoverState.Invalid, all, true), Is.EqualTo(FieldHighlightColor.HoverInvalid));
            Assert.That(FieldHighlightLayers.Resolve(true, CombatHoverState.Valid, all, true), Is.EqualTo(FieldHighlightColor.HoverValid));
            Assert.That(FieldHighlightLayers.Resolve(true, CombatHoverState.Neutral, all, true), Is.EqualTo(FieldHighlightColor.HoverNeutral));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, all, true), Is.EqualTo(FieldHighlightColor.Affected));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral,
                all & ~FieldHighlightLayer.Affected, true), Is.EqualTo(FieldHighlightColor.MovementDraft));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral,
                FieldHighlightLayer.ActiveActor | FieldHighlightLayer.Range, true), Is.EqualTo(FieldHighlightColor.Range));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, FieldHighlightLayer.ActiveActor, true),
                Is.EqualTo(FieldHighlightColor.ActiveActor));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, FieldHighlightLayer.None, true),
                Is.EqualTo(FieldHighlightColor.Static));
            Assert.That(FieldHighlightLayers.Resolve(false, CombatHoverState.Neutral, FieldHighlightLayer.None, false),
                Is.EqualTo(FieldHighlightColor.None), "모든 층이 지워지면 셀 기본 색으로 돌아간다.");
        }

        #endregion

        #region 좌표 변환

        [Test]
        public void FieldGridMapper_CenterAndWorldToCell_RoundTrip()
        {
            Transform root = Create<Transform>();
            root.position = new Vector3(10f, 0f, -5f);
            var mapper = new FieldGridMapper(root, 8, 7, 1.5f, 2f);

            Assert.That(mapper.CellCenterToLocal(new CellCoord(0, 0)), Is.EqualTo(new Vector3(-5.25f, 0f, -6f)));
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var cell = new CellCoord(x, y);
                    Assert.That(mapper.TryWorldToCell(mapper.CellCenterToWorld(cell), out CellCoord back), Is.True);
                    Assert.That(back, Is.EqualTo(cell));
                }
            }

            Assert.That(mapper.TryWorldToCell(root.position + new Vector3(100f, 0f, 0f), out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => mapper.CellCenterToLocal(new CellCoord(8, 0)));
        }

        [Test]
        public void FieldCellSpacing_AddsGapAndPadding()
        {
            FieldCellSpacing spacing = FieldCellSpacing.Calculate(1f, 2f, 0.5f, 0f);

            Assert.That(spacing.Width, Is.EqualTo(1f + 0.01f + 0.5f).Within(1e-5f));
            Assert.That(spacing.Length, Is.EqualTo(2f + 0.01f).Within(1e-5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FieldCellSpacing.Calculate(0f, 1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FieldCellSpacing.Calculate(1f, 1f, -1f, 0f));
        }

        #endregion

        #region 손패 카드 (손패 카드화 H2)

        [Test]
        public void HandCards_OrderBySlot_CenterInRoot_ReuseCards_AndHideWithRoot()
        {
            HandPresenter hand = CreateCardHand(1000f, out RectTransform root);
            CharacterData first = HandCard(1, slot: 0);
            CharacterData second = HandCard(2, slot: 1);

            // 입력 순서와 관계없이 슬롯 순서로 놓는다. 손패가 아닌 개체는 그리지 않는다.
            hand.Render(new[] { second, first, FieldCard(3) }, true);

            Assert.That(root.childCount, Is.EqualTo(2));
            var left = (RectTransform)root.GetChild(0);
            var right = (RectTransform)root.GetChild(1);
            // 카드 폭 100(400 × 0.25) + 간격 10 → 가운데 기준 ±55. 높이 137.5, 영역 높이 300, 아래 여백 0.
            Assert.That(left.anchoredPosition.x, Is.EqualTo(-55f).Within(1e-4f));
            Assert.That(right.anchoredPosition.x, Is.EqualTo(55f).Within(1e-4f));
            Assert.That(left.anchoredPosition.y, Is.EqualTo(-150f + 68.75f).Within(1e-4f));
            Assert.That(left.localScale, Is.EqualTo(Vector3.one * 0.25f));

            hand.Render(new[] { first, second }, true);
            Assert.That(root.GetChild(0), Is.SameAs(left), "같은 개체는 같은 카드를 다시 쓴다.");
            Assert.That(root.GetChild(1), Is.SameAs(right));

            hand.Render(new[] { first, second }, false);
            Assert.That(root.gameObject.activeSelf, Is.False, "배치 단계가 아니면 카드 영역을 숨긴다.");
            Assert.That(hand.TryPickUnit(left.anchoredPosition, out _), Is.False);
            Assert.That(hand.ContainsDropPoint(Vector2.zero), Is.False);
        }

        [Test]
        public void HandCards_PickByScreenPoint_FrontCardWins_HiddenCardIsSkipped_DropZoneIsRoot()
        {
            // 영역이 좁아 간격이 50으로 줄고 카드가 겹친다(±25). 가운데 점은 두 카드 모두에 걸린다.
            HandPresenter hand = CreateCardHand(150f, out RectTransform root);
            CharacterData first = HandCard(1, slot: 0);
            CharacterData second = HandCard(2, slot: 1);
            hand.Render(new[] { first, second }, true);
            float y = -150f + 68.75f;

            Assert.That(hand.TryPickUnit(new Vector2(0f, y), out EntityId picked), Is.True);
            Assert.That(picked, Is.EqualTo(second.EntityId), "겹치면 앞에 그려진 카드가 이긴다.");
            Assert.That(hand.TryPickUnit(new Vector2(-70f, y), out picked), Is.True);
            Assert.That(picked, Is.EqualTo(first.EntityId));
            Assert.That(hand.TryPickUnit(new Vector2(0f, 140f), out _), Is.False, "카드 밖은 집지 않는다.");

            hand.SetHiddenEntity(second.EntityId);
            Assert.That(hand.TryPickUnit(new Vector2(0f, y), out picked), Is.True);
            Assert.That(picked, Is.EqualTo(first.EntityId), "Drag 중 숨긴 카드는 집지 않는다.");
            hand.ClearHiddenEntity();
            Assert.That(root.GetChild(1).gameObject.activeSelf, Is.True);

            Assert.That(hand.ContainsDropPoint(new Vector2(0f, 140f)), Is.True, "Drop 영역은 카드가 아닌 영역 전체다.");
            Assert.That(hand.ContainsDropPoint(new Vector2(200f, 0f)), Is.False);
            hand.Release();
            Assert.That(hand.TryPickUnit(new Vector2(0f, y), out _), Is.False);
        }

        /// <summary>Canvas 없는 RectTransform 손패입니다. 카메라 없는 화면 좌표 = 월드 좌표이므로 원점 영역이면 로컬 좌표로 확인할 수 있습니다.</summary>
        private HandPresenter CreateCardHand(float width, out RectTransform root)
        {
            root = Create<RectTransform>();
            root.sizeDelta = new Vector2(width, 300f);
            UnitCardView prefab = Create<UnitCardView>();
            ((RectTransform)prefab.transform).sizeDelta = new Vector2(400f, 550f);
            HandPresenter hand = Create<HandPresenter>();
            SetHandField(hand, "cardRoot", root);
            SetHandField(hand, "cardPrefab", prefab);
            SetHandField(hand, "cardScale", 0.25f);
            SetHandField(hand, "cardGap", 10f);
            SetHandField(hand, "bottomMargin", 0f);
            SetHandField(hand, "sideMargin", 0f);
            hand.Configure(new NoUnitVisuals());
            return hand;
        }

        private static void SetHandField(HandPresenter hand, string name, object value)
        {
            typeof(HandPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hand, value);
        }

        private static CharacterData HandCard(int id, int slot)
        {
            var entityId = new EntityId(id);
            CharacterCombatFacts source = CharacterBufferTestData.SubFacts(new CharacterInstanceId(id), entityId);
            return new CharacterData(new CharacterCombatFacts(
                entityId, source.OwnedId, EntityKey.Unit_Akimbo, source.Owner, source.OwnershipRole, CharacterRuntimeArea.Hand,
                source.Health, source.Stats, slot, null, null, Array.Empty<CellCoord>(), default, 0, 0));
        }

        private static CharacterData FieldCard(int id)
        {
            var entityId = new EntityId(id);
            var cell = new CellCoord(0, 0);
            CharacterCombatFacts source = CharacterBufferTestData.SubFacts(new CharacterInstanceId(id), entityId);
            return new CharacterData(new CharacterCombatFacts(
                entityId, source.OwnedId, EntityKey.Unit_Akimbo, source.Owner, source.OwnershipRole, CharacterRuntimeArea.Field,
                source.Health, source.Stats, null, cell, Rotation4.R0, new[] { cell }, cell, 0, 0));
        }

        private sealed class NoUnitVisuals : IUnitVisualTableQuery
        {
            public UnitVisualData GetUnitVisual(EntityKey key) => throw new KeyNotFoundException(key.ToString());

            public bool TryGetUnitVisual(EntityKey key, out UnitVisualData data)
            {
                data = null;
                return false;
            }

            public IReadOnlyCollection<UnitVisualData> GetAllUnitVisuals() => Array.Empty<UnitVisualData>();
        }

        #endregion

        [Test]
        public void AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
            var colors = new ColorSystem();
            CombatViewSystem view = null;
            try
            {
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CombatSceneRoot>(true)).Single();
                root.ValidateReferences();
                var database = (IDataBaseQueryContext)DataBaseFactory.Create(
                    AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
                var b = CombatPreviewPublicationTests.Create(out var service);
                var actor = b.CharacterReadSink.Characters.Single();
                var origin = actor.Anchor.Value;
                var stop = origin + CellCoord.Up;
                var slot = new StageInputReceiverSlot();
                var layers = new CombatMovementViewTests.ProbeLayers();
                var faults = new RuntimeFaultBoundary(e => Assert.Fail(e.ToString()));
                view = new CombatViewSystem(CombatExecutionMode.Live, faults, new OrderedCombatPresentation());
                view.InitData(database);
                view.BindServices(b.CombatStateReadSink, b.CombatResultReadSink, b.CharacterReadSink, b.CurrentProgressReadSink,
                    b.CombatViewReadSink, b.CombatViewWriteSink, slot, layers,
                    new CombatViewSystemTests.FakeLoadingRegister(), colors, new CombatViewSystemTests.FakeAreaOutlines(),
                    new CombatViewSystemTests.EntryRead(),
                    new CombatViewSystemTests.AbandonProbe());
                view.Attach(root); view.Tick(0);
                var mapper = root.Field.Mapper;
                var fractional = mapper.WorldToGrid(mapper.CellCenterToWorld(origin));
                Assert.That(Vector2.Distance(fractional, new Vector2(origin.X + .5f, origin.Y + .5f)), Is.LessThan(.001f));
                Vector3 a = root.WorldCamera.WorldToScreenPoint(mapper.CellCenterToWorld(origin));
                Vector3 z = root.WorldCamera.WorldToScreenPoint(mapper.CellCenterToWorld(stop));
                view.Input.MoveModeRequested(); view.Input.PointerMoved(a.x, a.y);
                view.Input.PointerMoved(z.x, z.y); view.Input.SelectPressed(z.x, z.y); view.Input.SelectReleased(z.x, z.y);
                Assert.That(service.ProcessLatestPreview(), Is.True);
                view.Tick(0);
                Assert.That(b.CombatViewReadSink.Commands.Single().Data.Path.Cells, Is.EqualTo(new[] { origin, stop }));
                Assert.That(service.ProcessNextCommand(), Is.True);
                view.Tick(0);
                var unit = root.FieldUnits.GetComponentsInChildren<FieldUnitView>().Single(v => v.EntityId == actor.EntityId);
                Assert.That(Vector3.Distance(unit.transform.position, mapper.CellCenterToWorld(origin)), Is.LessThan(.001f));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
                view.Tick(root.MovementStepSeconds + .01f);
                Assert.That(Vector3.Distance(unit.transform.position, mapper.CellCenterToWorld(stop)), Is.LessThan(.001f));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(b.CombatStateReadSink.LatestResultSequence));
                Assert.That(service.ProcessLatestPreview(), Is.True); view.Tick(0);
                var hud = new SerializedObject(root.Hud);
                var button = (UnityEngine.UI.Button)hud.FindProperty("returnButton").objectReferenceValue;
                Assert.That(button.interactable, Is.True);
                Click(button);
                Assert.That(service.ProcessLatestPreview(), Is.True); view.Tick(0);
                Assert.That(service.ProcessNextCommand(), Is.True); view.Tick(0);
                Assert.That(Vector3.Distance(unit.transform.position, mapper.CellCenterToWorld(origin)), Is.LessThan(.001f));
                Assert.That(b.CharacterReadSink.Characters.Single().PendingMoveDistance, Is.Zero);
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(b.CombatStateReadSink.LatestResultSequence));
            }
            finally
            {
                view?.Shutdown(); colors.Shutdown(); EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void HudMovement_UpdatesQuoteValidityAndFailure_WithoutChangingMessages()
        {
            CombatHud hud = CreateFeedbackHud(out TMP_Text movement, out _, out _);
            MovePreview preview = CalculateMoveQuote();
            hud.ShowMovement(preview, true);
            Assert.That(movement.text, Is.EqualTo($"거리 {preview.QuotedDistance} · 비용 {preview.QuotedActionCost}"));
            MovePreview changed = MovePreview.Success(preview.PathMovement, preview.ProjectedTraitStatuses.ToArray(),
                preview.ProjectedCategory, preview.ProjectedActor, preview.PublicEffects, preview.QuotedActionCost + 1);
            hud.ShowMovement(changed, true);
            Assert.That(movement.text, Does.EndWith($"비용 {preview.QuotedActionCost + 1}"));
            hud.ShowMovement(changed, false);
            Assert.That(movement.text, Is.EqualTo("끝점에서 이어 그리세요"));
            hud.ShowMovement(MovePreview.Failure(MovementFailureReason.StaleContext), false);
            Assert.That(movement.text, Is.EqualTo("이동 불가: StaleContext"), "Core 거절 이유가 포인터 무효보다 먼저다.");
            hud.ShowMovement(MovePreview.Failure(MovementFailureReason.StaleContext), true);
            Assert.That(movement.text, Is.EqualTo("이동 불가: StaleContext"));
            hud.ShowMovement(null, true);
            Assert.That(movement.text, Is.Empty);
        }

        [Test]
        public void HudReturn_RebindRestoresSameQuoteAndButton_AndReplacesListener()
        {
            CombatHud hud = CreateFeedbackHud(out _, out TMP_Text text, out Button button);
            var preview = ReturnQuote();
            int first = 0, second = 0;
            hud.BindReturn(() => first++);
            hud.ShowReturn(preview);
            hud.BindReturn(() => second++);
            Assert.That(button.interactable, Is.False);
            hud.ShowReturn(ReturnQuote());
            Assert.That(button.interactable, Is.True);
            Assert.That(text.text, Is.EqualTo("복귀 · 비용 +2 / 거리 +1"));
            Click(button);
            Assert.That(first, Is.Zero);
            Assert.That(second, Is.EqualTo(1));
            hud.ShowReturn(new CheckpointReturnPreview(false, MovementFailureReason.NoPendingMovement));
            Assert.That(button.interactable, Is.False);
            Assert.That(text.text, Is.EqualTo("복귀 불가: NoPendingMovement"));
            hud.ShowReturn(null);
            Assert.That(text.text, Is.EqualTo("복귀"));
        }

        [Test]
        public void HudFeedback_ClearAndNewVisibleLifetime_InvalidateCachedValues()
        {
            CombatHud hud = CreateFeedbackHud(out TMP_Text movement, out TMP_Text returned, out Button button);
            var preview = CalculateMoveQuote();
            hud.ShowMovement(preview, true); hud.ShowReturn(ReturnQuote());
            string expected = movement.text;
            hud.Clear();
            movement.text = "clear marker"; returned.text = "clear marker";
            hud.ShowMovement(preview, true); hud.ShowReturn(ReturnQuote());
            Assert.That(movement.text, Is.EqualTo(expected));
            Assert.That(returned.text, Is.EqualTo("복귀 · 비용 +2 / 거리 +1"));
            // EditMode에서는 [ExecuteAlways]가 없는 MonoBehaviour의 OnDisable/OnEnable이 SetActive로 불리지 않는다.
            // 재생 중 수명주기와 같게 콜백을 직접 호출해 표시 수명이 바뀌면 캐시가 무효화되는지 본다(V4).
            hud.gameObject.SetActive(false);
            InvokeLifecycle(hud, "OnDisable");
            movement.text = "disabled marker"; returned.text = "disabled marker";
            button.interactable = false;
            hud.gameObject.SetActive(true);
            InvokeLifecycle(hud, "OnEnable");
            hud.ShowMovement(preview, true); hud.ShowReturn(ReturnQuote());
            Assert.That(movement.text, Is.EqualTo(expected));
            Assert.That(returned.text, Is.EqualTo("복귀 · 비용 +2 / 거리 +1"));
            Assert.That(button.interactable, Is.True);
        }

        [Test]
        public void HudFeedback_WarmRepeatedValues_DoNotAllocateStrings()
        {
            CombatHud hud = CreateFeedbackHud(out _, out _, out _);
            MovePreview move = CalculateMoveQuote();
            CheckpointReturnPreview returned = ReturnQuote();
            for (int i = 0; i < 32; i++) { hud.ShowMovement(move, true); hud.ShowReturn(returned); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 512; i++) { hud.ShowMovement(move, true); hud.ShowReturn(returned); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "동일 이동/복귀 견적 512쌍의 반복 표시만 측정한다.");
        }

        [TestCase(StageOutcomeReason.VoluntaryAbandonment, "포기 · 결과 확인")]
        [TestCase(StageOutcomeReason.PlayerMainCharacterDied, "패배 · 결과 확인")]
        public void HudCompletion_DefeatByAbandonment_ShowsAbandonText(StageOutcomeReason reason, string expected)
        {
            // 자발적 포기는 패배로 기록되지만 문구를 따로 보여 준다(포기 UI 설계 C5·P4).
            CombatHud hud = CreateFeedbackHud(out _, out _, out _);
            var completion = new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new StageOutcomeSnapshot(StageOutcome.Defeat, reason),
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(101)));

            hud.ShowCompletion(completion, () => { });

            var text = (TMP_Text)typeof(CombatHud).GetField("completionText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            Assert.That(text.text, Is.EqualTo(expected));
        }

        [Test]
        public void AbandonPanel_ButtonsReachEntriesThroughEventSystem_NotOnClick()
        {
            CombatAbandonPanel panel = Create<CombatAbandonPanel>();
            Button open = Create<Button>();
            Button confirm = Create<Button>();
            Button cancel = Create<Button>();
            SetField(panel, "abandonButton", open);
            SetField(panel, "confirmDialog", Create<Transform>().gameObject);
            SetField(panel, "confirmButton", confirm);
            SetField(panel, "cancelButton", cancel);
            int opens = 0, confirms = 0, cancels = 0;

            panel.Bind(() => opens++, () => confirms++, () => cancels++);
            Click(open);
            Click(confirm);
            Click(cancel);
            Submit(open);
            Assert.That((opens, confirms, cancels), Is.EqualTo((2, 1, 1)), "클릭과 Submit 모두 슬롯 입구로 간다.");

            Assert.That(open.GetComponent<UIInputBinding>(), Is.Not.Null);
            Assert.That(open.onClick.GetPersistentEventCount(), Is.Zero);
            open.onClick.Invoke();
            Assert.That(opens, Is.EqualTo(2), "onClick에는 아무것도 연결하지 않는다.");

            open.interactable = false;
            Click(open);
            Assert.That(opens, Is.EqualTo(2), "누를 수 없는 버튼은 전달하지 않는다.");
            open.interactable = true;

            int replaced = 0;
            panel.Bind(() => replaced++, () => { }, () => { });
            Click(open);
            Assert.That((opens, replaced), Is.EqualTo((2, 1)), "다시 연결하면 이전 입구는 불리지 않는다.");

            panel.Bind(null, null, null);
            Click(open);
            Assert.That(replaced, Is.EqualTo(1), "해제 뒤에는 아무것도 하지 않는다.");
        }

        [Test]
        public void HudCompletion_ConfirmsOnceThroughEventSystem_AndHideDropsIt()
        {
            CombatHud hud = CreateFeedbackHud(out _, out _, out _);
            var button = (Button)typeof(CombatHud).GetField("completionButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            var completion = new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(101), false));
            int calls = 0;

            hud.ShowCompletion(completion, () => calls++);
            Click(button);
            Click(button);
            Submit(button);
            Assert.That(calls, Is.EqualTo(1), "완료 확인은 표시 한 번에 한 번만 전달한다.");
            Assert.That(button.interactable, Is.False);

            int later = 0;
            hud.ShowCompletion(completion, () => later++);
            hud.HideCompletion();
            button.gameObject.SetActive(true);
            button.interactable = true;
            Click(button);
            Assert.That(later, Is.Zero, "숨긴 뒤에는 이전 확인 입구가 불리지 않는다.");
        }

        [Test]
        public void AuthoredCombatScene_DevLayerButtons_UseInputBinding()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
            var colors = new ColorSystem();
            CombatSceneDisplay display = null;
            try
            {
                var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CombatSceneRoot>(true)).Single();
                var database = (IDataBaseQueryContext)DataBaseFactory.Create(
                    AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
                var views = database.ViewQuery;
                display = new CombatSceneDisplay(root, views.StageFieldViewTableQuery.Config, views.UnitVisualTableQuery,
                    views.StageViewPropTableQuery, colors, views.TraitVisualTableQuery, new CombatViewSystemTests.FakeAreaOutlines());
                int deployments = 0, combats = 0;

                display.ShowDevLayerPanel(() => deployments++, () => combats++);
                Click(root.DevCombatButton);
                Click(root.DevDeploymentButton);
                Assert.That((deployments, combats), Is.EqualTo((1, 1)));
                Assert.That(root.DevCombatButton.onClick.GetPersistentEventCount(), Is.Zero);

                display.HideDevLayerPanel();
                root.DevLayerPanel.SetActive(true);
                Click(root.DevCombatButton);
                Assert.That(combats, Is.EqualTo(1), "숨긴 뒤에는 입구를 부르지 않는다.");

                display.ShowDevLayerPanel(() => deployments++, () => combats++);
                display.Release();
                root.DevLayerPanel.SetActive(true);
                Click(root.DevCombatButton);
                Assert.That(combats, Is.EqualTo(1), "Release 뒤에는 입구를 부르지 않는다.");
            }
            finally
            {
                display?.Release();
                colors.Shutdown();
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(null), ExecuteEvents.pointerClickHandler);
        }

        private static void Submit(Button button)
        {
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(null), ExecuteEvents.submitHandler);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void InvokeLifecycle(CombatHud hud, string method)
        {
            typeof(CombatHud).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        }

        private CombatHud CreateFeedbackHud(out TMP_Text movement, out TMP_Text returned, out Button button)
        {
            CombatHud hud = Create<CombatHud>();
            foreach (string field in new[] { "stageGoldText", "remainingDrawText", "movementText", "returnText", "completionText" })
                typeof(CombatHud).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(hud, Create<TextMeshProUGUI>());
            button = Create<Button>();
            typeof(CombatHud).GetField("returnButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, button);
            typeof(CombatHud).GetField("completionButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, Create<Button>());
            movement = (TMP_Text)typeof(CombatHud).GetField("movementText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            returned = (TMP_Text)typeof(CombatHud).GetField("returnText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            return hud;
        }

        private static MovePreview CalculateMoveQuote()
        {
            var buffers = CombatPreviewPublicationTests.Create(out var service);
            var actor = buffers.CharacterReadSink.Characters[0];
            var origin = actor.Anchor.Value;
            buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(actor.EntityId,
                new MovementPath(new[] { origin, origin + CellCoord.Up })));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Move.Value.CanMove, Is.True);
            return preview.Move.Value;
        }

        private static CheckpointReturnPreview ReturnQuote() => new(true, MovementFailureReason.None,
            new CheckpointReturnResult(CellCoord.Up, CellCoord.Zero, 2, 1));

        private T Create<T>() where T : Component
        {
            var item = new GameObject(typeof(T).Name + " (Test)");
            _objects.Add(item);
            return typeof(T) == typeof(Transform) ? (T)(Component)item.transform : item.AddComponent<T>();
        }

        private static void Render(EntityViewSet<FakeView> set, params (int Id, EntityKey Key)[] targets)
        {
            set.Begin();
            foreach ((int id, EntityKey key) in targets)
                set.Acquire(new EntityId(id), key);
            set.End();
        }

        private sealed class FakeView
        {
            internal EntityKey Key;
        }

        private sealed class CountingPool
        {
            internal int Rented;
            internal int Returned;
            internal EntityKey LastReturnedKey;
            internal EntityKey? ThrowOnKey;

            internal FakeView Rent(EntityKey key)
            {
                Rented++;
                return new FakeView { Key = key };
            }

            internal void Return(EntityKey key, FakeView view)
            {
                if (ThrowOnKey == key)
                    throw new InvalidOperationException("반환 실패(테스트)");
                Returned++;
                LastReturnedKey = key;
            }
        }
    }
}
