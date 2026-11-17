using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using TMPro;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.CombatField.Editor.Tests
{
    /// <summary>Structure 피격 시점·선택 사실·치명타 숫자 수명의 회귀입니다.</summary>
    public sealed class StructureHitPresentationTests
    {
        private readonly List<GameObject> _objects = new();
        private GameObject New(string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            _objects.Add(go);
            return go;
        }
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }
        private static StructureCombatFacts Structure(int hp = 30, EntityKey key = EntityKey.Structure_Headquarters)
        {
            var cells = new[] { new CellCoord(1, 1), new CellCoord(2, 1) };
            var spec = new StructureSpec(key, 2, 1, new[] { new CellCoord(0, 0), new CellCoord(1, 0) }, 30, false, 0);
            return new StructureCombatFacts(new EntityId(91), spec, new HealthStateSnapshot(hp, 30), cells[0], Rotation4.R0, cells);
        }
        [TestCase((int)CombatCueAnchor.Start, 0f, (int)CombatCuePoint.SubjectBody, true)]
        [TestCase((int)CombatCueAnchor.Start, 0.1f, (int)CombatCuePoint.SubjectBody, false)]
        [TestCase((int)CombatCueAnchor.End, 0f, (int)CombatCuePoint.SubjectBody, false)]
        [TestCase((int)CombatCueAnchor.Start, 0f, (int)CombatCuePoint.ActorBody, false)]
        public void StructureCue_OnlyImmediateSubjectEffect(int anchor, float delay, int point, bool valid)
        {
            var cues = new CombatEventCues(new[] { new CombatEffectCue(EffectType.Hit, (CombatCueAnchor)anchor, delay, (CombatCuePoint)point) }, null, null);
            Assert.That(CombatPresentationCueRules.FindError(cues, CombatCueEvent.StructureHit) == null, Is.EqualTo(valid));
        }
        [TestCase(20, 1)]
        [TestCase(0, 1)]
        [TestCase(30, 0)]
        public void Handler_UsesEventLossAndSameSubjectBeforeFacts(int hp, int count)
        {
            var expected = Structure();
            var final = Structure(hp);
            var calls = new List<string>();
            var handler = new CombatStructureEventHandler(_ => new Pose(Vector3.one, Quaternion.identity),
                (key, id, pose) => calls.Add("cue"),
                (position, loss) => { Assert.That(loss, Is.EqualTo(30 - hp)); calls.Add("damage"); },
                fact => { Assert.That(fact, Is.SameAs(final)); calls.Add("facts"); });
            var e = new CombatStructureEvent(expected, final);
            Assert.That(handler.TryBeginBeforeFacts(e, null, 0), Is.False);
            handler.ApplyFacts(e);
            Assert.That(calls, Is.EqualTo(count == 1 ? new[] { "cue", "damage", "facts" } : new[] { "facts" }));
        }
        [Test]
        public void NativeObstacle_DoesNotDuplicateHitFeedback()
        {
            var handler = new CombatStructureEventHandler(_ => throw new Exception("장애물 몸 조회 금지"),
                (_, __, ___) => Assert.Fail(), (_, __) => Assert.Fail(), _ => { });
            var e = new CombatStructureEvent(Structure(30, EntityKey.Structure_PowderBarrel), Structure(10, EntityKey.Structure_PowderBarrel));
            Assert.That(handler.TryBeginBeforeFacts(e, null, 0), Is.False);
        }
        [Test]
        public void Selection_UsesShownHealthAndClearsDestroyedStructure()
        {
            var units = New("units").AddComponent<FieldUnitPresenter>();
            var facts = new CombatShownFacts(units, () => null, NoPersistentEffects());
            facts.Replace(Array.Empty<CharacterCombatFacts>(), new[] { Structure() });
            var selection = new CombatSelectionState();
            selection.Bind(facts);
            selection.Synchronize(StagePhase.Combat);
            selection.SelectCell(new CellCoord(2, 1));
            Assert.That(selection.SelectedEntityId, Is.EqualTo(new EntityId(91)));
            Assert.That(selection.SelectedActorId, Is.EqualTo(EntityId.Empty));
            Assert.That(facts.TryGet(selection.SelectedEntityId, out var shown), Is.True);
            Assert.That(shown.Health, Is.EqualTo(30));
            facts.ApplyStructure(Structure(10));
            Assert.That(facts.TryGet(selection.SelectedEntityId, out shown), Is.True);
            Assert.That(shown.Health, Is.EqualTo(10));
            facts.ApplyStructure(Structure(0));
            selection.ValidateSelection();
            Assert.That(selection.SelectedEntityId, Is.EqualTo(EntityId.Empty));
        }
        // 보인 사실의 셋째 생성 인자(유지 이펙트 부품, 이펙트 구조 2판 EL-b)만 맞춥니다. 이 시험은 그리지 않아 부품이 쓰이지 않습니다.
        private CombatPersistentEffects NoPersistentEffects() => new(
            new CombatEffectWindow(new NoEffects(), New("effects").transform, _ => () => (false, (Vector3?)null)),
            Array.Empty<ICombatPersistentEffectSource>());
        private sealed class NoEffects : IEffectSystemServices
        {
            public EffectScope GetEffectHandle() => throw new NotSupportedException();
        }
        private sealed class StructureReader : ICombatInspectionFactsReader
        {
            internal StructureCombatFacts Current = Structure();
            public bool TryGet(EntityId id, out CombatInspectionFacts facts)
            {
                facts = new CombatInspectionFacts(Current);
                return id == Current.EntityId && !Current.Health.IsDead;
            }
            public bool TryPick(CellCoord cell, out EntityId id)
            {
                foreach (CellCoord occupied in Current.OccupiedCells)
                    if (occupied == cell && !Current.Health.IsDead) { id = Current.EntityId; return true; }
                id = EntityId.Empty;
                return false;
            }
        }
        [TestCase(StagePhase.Combat)]
        [TestCase(StagePhase.Deployment)]
        public void StructureSelection_IsInspectionOnly_EmptyCellAndPhaseClearIt(StagePhase stage)
        {
            var reader = new StructureReader();
            var selection = new CombatSelectionState();
            selection.Bind(reader);
            selection.Synchronize(stage);
            selection.SelectCell(new CellCoord(1, 1));
            Assert.That(selection.SelectedEntityId, Is.EqualTo(reader.Current.EntityId));
            Assert.That(selection.SelectedActorId, Is.EqualTo(EntityId.Empty));
            selection.SelectCell(new CellCoord(2, 1));
            Assert.That(selection.SelectedEntityId, Is.EqualTo(reader.Current.EntityId));
            selection.SelectCell(new CellCoord(9, 9));
            Assert.That(selection.SelectedEntityId, Is.EqualTo(EntityId.Empty));
            selection.SelectCell(new CellCoord(1, 1));
            selection.Synchronize(stage == StagePhase.Combat ? StagePhase.Deployment : StagePhase.Combat);
            Assert.That(selection.SelectedEntityId, Is.EqualTo(EntityId.Empty));
        }
        [Test]
        public void StructureSelection_DeadTargetIsInvalidated()
        {
            var reader = new StructureReader();
            var selection = new CombatSelectionState();
            selection.Bind(reader);
            selection.Synchronize(StagePhase.Combat);
            selection.SelectCell(new CellCoord(2, 1));
            reader.Current = Structure(0);
            selection.ValidateSelection();
            Assert.That(selection.SelectedEntityId, Is.EqualTo(EntityId.Empty));
        }
        [TestCase(StagePhase.Deployment, CombatFlowPhase.PlayerInput, false, false, true)]
        [TestCase(StagePhase.Deployment, CombatFlowPhase.PlayerInput, true, false, false)]
        [TestCase(StagePhase.Deployment, CombatFlowPhase.PlayerInput, false, true, false)]
        [TestCase(StagePhase.Deployment, CombatFlowPhase.Automatic, false, false, false)]
        [TestCase(StagePhase.Combat, CombatFlowPhase.Automatic, false, false, true)]
        public void InspectionPolicy_DeploymentKeepsPendingAndModalBoundaries(StagePhase stage,
            CombatFlowPhase phase, bool pending, bool modal, bool expected)
        {
            // 입력 사실 인자는 모두 필수가 됐습니다(평가 C5-02). 이 사례가 정하던 값(앞 다섯 + 이름 인자 넷) 말고는 옛 기본값 그대로입니다.
            var facts = new CombatInputFacts(
                isBattleDisplayed: true,
                hasBattle: true,
                phase: phase,
                stagePhase: stage,
                hasCompletion: false,
                currentLayer: CombatInputPolicy.LayerOf(stage),
                latestResultSequence: 0,
                presentedResultSequence: 0,
                isFinalizing: false,
                selectedActorId: default,
                hasUnhandledCommand: false,
                previewAnswer: null,
                hasPendingCommand: pending,
                isPresenting: false,
                isInputAvailable: true,
                isAttackActive: false,
                combatInputRestriction: default,
                hasCurrentAcceptedEmptyMovementRange: false,
                battleInstanceId: 0,
                controllerTurnSequence: 0,
                isSubmissionSuspended: false,
                isAbandonRequested: false,
                isModalOpen: modal,
                isItemDragActive: false,
                isTutorialPracticeGoalPending: false,
                queryRevision: 0,
                isEntryPresentationPending: false,
                pointerTarget: default);
            Assert.That(CombatInputPolicy.CanSelectUnit(facts), Is.EqualTo(expected));
        }
        private UnitDamageText Widget()
        {
            GameObject go = New("damage", typeof(RectTransform));
            var text = New("text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(go.transform, false);
            var widget = go.AddComponent<UnitDamageText>();
            typeof(UnitDamageText).GetField("damageText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(widget, text);
            return widget;
        }
        [Test]
        public void WidgetProjection_AddsMissingCanvasGroup_AndLeaseIsRepeatable()
        {
            var canvas = New("canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var camera = New("camera", typeof(Camera)).GetComponent<Camera>();
            var layer = New("layer", typeof(RectTransform)).AddComponent<UnitHUDLayer>();
            layer.transform.SetParent(canvas.transform, false);
            layer.Configure(camera);
            var rect = New("DamageNumbers(Clone)", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(layer.transform, false);
            Assert.That(rect.GetComponent<CanvasGroup>() == null, Is.True);
            IDisposable lease = layer.RegisterWidget(rect, Vector3.zero);
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            Assert.That(group != null, Is.True);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
            Assert.DoesNotThrow(() => { lease.Dispose(); lease.Dispose(); });
            Assert.That(group.alpha, Is.Zero);
        }
        [Test]
        public void IndependentNumber_CompleteOnce_ReentryCannotClearNextRental()
        {
            UnitDamageText widget = Widget();
            var tokens = new List<long>();
            widget.PlayIndependent(10, 1, token =>
            {
                tokens.Add(token);
                widget.PlayIndependent(20, 2, tokens.Add);
            });
            widget.TickAnimation(2f);
            Assert.That(tokens, Is.EqualTo(new long[] { 1 }));
            widget.TickAnimation(2f);
            widget.TickAnimation(2f);
            Assert.That(tokens, Is.EqualTo(new long[] { 1, 2 }));
        }
        [Test]
        public void IndependentNumber_ClearAndDisableAreNotCompletion()
        {
            UnitDamageText widget = Widget();
            int completed = 0;
            widget.PlayIndependent(10, 1, _ => completed++);
            widget.Clear();
            widget.TickAnimation(2f);
            widget.PlayIndependent(10, 2, _ => completed++);
            widget.gameObject.SetActive(false);
            // EditMode의 일반 MonoBehaviour는 런타임 콜백을 자동 실행하지 않으므로 수명 알림을 명시적으로 전달합니다.
            typeof(UnitDamageText).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(widget, null);
            widget.TickAnimation(2f);
            Assert.That(completed, Is.Zero);
        }
        [Test]
        public void Assets_StructureEffectAndInfoReferencesAreConfigured()
        {
            var visual = AssetDatabase.LoadAssetAtPath<ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item.UnitVisualDataSO>(
                "Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/Item/Enemy/UnitVisual_Headquarters.asset");
            var data = (ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item.UnitVisualData)visual.GetData();
            data.Validate();
            Assert.That(data.StructureHitCues.Effects.Count, Is.GreaterThan(0));
            Assert.That(data.StructureHitCues.Effects[0].StaysInPlace, Is.True);
            var deck = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/DeckUI.prefab");
            var view = deck.GetComponentInChildren<ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud.CombatUnitInfoView>(true);
            var so = new SerializedObject(view);
            Assert.That(so.FindProperty("nameText").objectReferenceValue, Is.Not.Null);
            Assert.That(so.FindProperty("structureInfoText").objectReferenceValue, Is.Not.Null);
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectL/Prefabs/UI/CombatOnly/HUD/HUD.prefab");
            UnitDamageText numbers = hud.GetComponentInChildren<UnitDamageText>(true);
            numbers.ValidateTemplate();
            Assert.That(numbers.gameObject, Is.Not.SameAs(hud));
        }

        [Test]
        public void DatabaseAssets_AllUnitVisualStructureCuesRemainValid()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UnitVisualDataSO", new[] { "Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item.UnitVisualDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                var data = (ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item.UnitVisualData)asset.GetData();
                Assert.DoesNotThrow(() => CombatPresentationCueRules.Require(data.StructureHitCues, CombatCueEvent.StructureHit),
                    AssetDatabase.GetAssetPath(asset));
                if (ObstacleContentProfile.IsNativeObstacleKey(data.Key))
                    Assert.That(data.StructureHitCues.IsEmpty, Is.True, AssetDatabase.GetAssetPath(asset));
            }
        }
    }
}
