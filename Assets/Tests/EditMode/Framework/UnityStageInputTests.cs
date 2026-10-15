using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static ProjectL.Tests.Framework.DataBaseTestAssets;
using InputRouter = ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View.InputSystem;
using UnityInput = UnityEngine.InputSystem.InputSystem;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// Unity 입력 연결 계층(Layer 런타임, 설정 검증, 브리지 수명, 입력 팩토리)을 장치 없이 확인합니다.
    /// 맵 켜짐·꺼짐은 InputActionMap.enabled와 InputSystem.onActionChange로 관찰합니다.
    /// </summary>
    public sealed class UnityStageInputTests
    {
        private const string ProjectAssetPath = "Assets/ProjectL/Input/PlayerInputActions.inputactions";
        private const string RuntimeCloneSuffix = "(Runtime)";

        private readonly DataBaseTestAssets _assets = new();
        private int _cloneBaseline;

        [SetUp]
        public void SetUp()
        {
            _cloneBaseline = RuntimeCloneCount();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        #region 실제 프로젝트 에셋

        [Test]
        public void ProjectAsset_SelectActionsKeepReleaseOnlyContract()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectAssetPath);
            Assert.That(asset, Is.Not.Null, ProjectAssetPath);

            AssertReleaseOnly(asset, StageInputNames.Deployment.Map, StageInputNames.Deployment.Press);
            AssertReleaseOnly(asset, StageInputNames.Combat.Map, StageInputNames.Combat.Select);
        }

        [Test]
        public void ProjectAsset_MatchesStageInputConfiguration()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectAssetPath);
            Assert.That(asset, Is.Not.Null, ProjectAssetPath);

            Assert.DoesNotThrow(() => InputConfigurationValidator.ValidateOrThrow(
                asset, StageInputLayers.All, CreateConfiguration()));
        }

        #endregion

        #region 설정 검증

        [Test]
        public void Validator_ReportsEveryProblemAtOnce()
        {
            var asset = CreateStageAsset(includeEndTurn: false, pointerAsButton: true);
            var layers = new[]
            {
                StageInputLayers.Deployment,
                new InputLayer(StageInputNames.Deployment.Map, StageInputNames.Common.Map, StageInputNames.Deployment.Map),
                new InputLayer("Broken", "Missing")
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                InputConfigurationValidator.ValidateOrThrow(asset, layers, CreateConfiguration()));

            Assert.That(exception.Message, Does.Contain("Input Layer name is duplicated: Deployment"));
            Assert.That(exception.Message, Does.Contain("Action Map was not found: Broken.Missing"));
            Assert.That(exception.Message, Does.Contain("cannot be activated by any registered Layer"));
            Assert.That(exception.Message, Does.Contain("Input Action was not found: Combat.EndTurn"));
            Assert.That(exception.Message, Does.Contain("does not match the Controller binding: Deployment.PointerPosition"));
        }

        [Test]
        public void Validator_RejectsEmptyLayerList()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                InputConfigurationValidator.ValidateOrThrow(
                    CreateStageAsset(), Array.Empty<InputLayer>(), CreateConfiguration()));

            Assert.That(exception.Message, Does.Contain("At least one Input Layer is required."));
        }

        #endregion

        #region Layer 런타임

        [Test]
        public void LayerRuntime_StartsInactiveAndEnablesOnlyLayerMaps()
        {
            var asset = CreateStageAsset();
            using var runtime = new UnityInputLayerRuntime(asset, StageInputLayers.All);

            runtime.Resume();
            Assert.That(runtime.CurrentLayer.IsValid, Is.False);
            AssertEnabled(asset, common: false, deployment: false, combat: false);

            runtime.ChangeLayer(StageInputLayers.Deployment);
            AssertEnabled(asset, common: true, deployment: true, combat: false);

            runtime.ChangeLayer(StageInputLayers.Combat);
            AssertEnabled(asset, common: true, deployment: false, combat: true);

            runtime.ClearLayer();
            runtime.ClearLayer();
            Assert.That(runtime.CurrentLayer.IsValid, Is.False);
            AssertEnabled(asset, common: false, deployment: false, combat: false);

            runtime.ChangeLayer(StageInputLayers.Combat);
            AssertEnabled(asset, common: true, deployment: false, combat: true);

            Assert.Throws<ArgumentException>(() => runtime.ChangeLayer(default));
            // 이름은 같지만 등록된 정의와 맵 구성이 다른 Layer는 거부합니다.
            Assert.Throws<InvalidOperationException>(() =>
                runtime.ChangeLayer(new InputLayer(StageInputNames.Combat.Map, StageInputNames.Combat.Map)));
        }

        [Test]
        public void LayerRuntime_SuspendKeepsLayerAndDoesNotEnableUntilResume()
        {
            var asset = CreateStageAsset();
            var runtime = new UnityInputLayerRuntime(asset, StageInputLayers.All);

            runtime.Resume();
            runtime.ChangeLayer(StageInputLayers.Deployment);
            runtime.Suspend();
            Assert.That(runtime.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
            AssertEnabled(asset, common: false, deployment: false, combat: false);

            runtime.ChangeLayer(StageInputLayers.Combat);
            AssertEnabled(asset, common: false, deployment: false, combat: false);

            runtime.Resume();
            AssertEnabled(asset, common: true, deployment: false, combat: true);

            runtime.Dispose();
            AssertEnabled(asset, common: false, deployment: false, combat: false);
            Assert.Throws<ObjectDisposedException>(() => runtime.ChangeLayer(StageInputLayers.Deployment));
            Assert.Throws<ObjectDisposedException>(() => runtime.ClearLayer());
            Assert.DoesNotThrow(() => runtime.Dispose());
        }

        [Test]
        public void LayerRuntime_KeepsCommonMapWhileSwitching()
        {
            var asset = CreateStageAsset();
            using var runtime = new UnityInputLayerRuntime(asset, StageInputLayers.All);
            runtime.Resume();
            runtime.ChangeLayer(StageInputLayers.Deployment);

            var changes = RecordMapChanges(asset, () => runtime.ChangeLayer(StageInputLayers.Combat));

            // 빠지는 맵을 먼저 끄고 들어오는 맵을 켭니다. Common은 끄거나 다시 켜지 않습니다.
            Assert.That(changes, Is.EqualTo(new[]
            {
                $"{InputActionChange.ActionMapDisabled} {StageInputNames.Deployment.Map}",
                $"{InputActionChange.ActionMapEnabled} {StageInputNames.Combat.Map}"
            }));
        }

        [Test]
        public void LayerRuntime_RequestDuringTransitionAppliesLastRequestAfterward()
        {
            var asset = CreateStageAsset();
            using var runtime = new UnityInputLayerRuntime(asset, StageInputLayers.All);
            runtime.Resume();
            runtime.ChangeLayer(StageInputLayers.Deployment);
            InputActionMap deploymentMap = asset.FindActionMap(StageInputNames.Deployment.Map, true);

            bool requested = false;
            void OnChange(object target, InputActionChange change)
            {
                // 맵을 끄는 도중(콜백 안)에 들어온 요청은 보류됐다가 전환이 끝난 뒤 적용됩니다.
                if (requested || change != InputActionChange.ActionMapDisabled || !ReferenceEquals(target, deploymentMap))
                    return;

                requested = true;
                Assert.That(runtime.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
                runtime.ChangeLayer(StageInputLayers.Deployment);
                runtime.ClearLayer();
            }

            UnityInput.onActionChange += OnChange;
            try
            {
                runtime.ChangeLayer(StageInputLayers.Combat);
            }
            finally
            {
                UnityInput.onActionChange -= OnChange;
            }

            Assert.That(requested, Is.True);
            Assert.That(runtime.CurrentLayer.IsValid, Is.False);
            AssertEnabled(asset, common: false, deployment: false, combat: false);
        }

        #endregion

        #region 브리지

        [Test]
        public void Bridge_InitDataConfiguresRouterAndStartsWithAllMapsOff()
        {
            var asset = CreateStageAsset();
            var router = new InputRouter();
            var bridge = CreateBridge(router);

            bridge.InitData(CreateContext(asset));
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.EqualTo(1));
            Assert.DoesNotThrow(() => router.ClearLayer());

            bridge.Initialize();
            Assert.That(router.CurrentLayer.IsValid, Is.False);

            router.ChangeLayer(StageInputLayers.Deployment);
            Assert.That(router.CurrentLayer, Is.EqualTo(StageInputLayers.Deployment));
            // 복제본의 맵만 켜지고 원본 에셋은 바뀌지 않습니다.
            AssertEnabled(asset, common: false, deployment: false, combat: false);

            bridge.Shutdown();
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.Zero);
            Assert.DoesNotThrow(() => bridge.Shutdown());
            router.Shutdown();
        }

        [Test]
        public void Bridge_MissingSettingsFailWithoutConfiguringRouter()
        {
            AssertInitDataFails(new ViewContext(null), "ViewQuery");
            AssertInitDataFails(new ViewContext(new ViewQueryStub(null)), "table");
            AssertInitDataFails(new ViewContext(new ViewQueryStub(new SettingsTableStub(null))), "item");
            AssertInitDataFails(new ViewContext(new ViewQueryStub(new SettingsTableStub(new InputSettingsData()))),
                "action asset");

            void AssertInitDataFails(IHasViewQuery context, string expected)
            {
                var router = new InputRouter();
                var bridge = CreateBridge(router);

                var exception = Assert.Throws<InvalidOperationException>(() => bridge.InitData(context));
                Assert.That(exception.Message, Does.Contain(expected));
                Assert.Throws<InvalidOperationException>(() => router.ClearLayer(), "라우터는 Configure되지 않아야 합니다");
                Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.Zero);
                bridge.Shutdown();
            }
        }

        [Test]
        public void Bridge_WrongContextTypeFailsAtTypedInitData()
        {
            var bridge = CreateBridge(new InputRouter());

            Assert.Throws<InvalidCastException>(() => ((ISystemInitData)bridge).InitData(new OtherModule()));
        }

        [Test]
        public void Bridge_ValidationFailureReleasesCloneAndLeavesRouterUnconfigured()
        {
            var asset = CreateStageAsset(includeEndTurn: false);
            var router = new InputRouter();
            var bridge = CreateBridge(router);

            var exception = Assert.Throws<InvalidOperationException>(() => bridge.InitData(CreateContext(asset)));

            Assert.That(exception.Message, Does.Contain("Combat.EndTurn"));
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => router.ClearLayer());
            Assert.That(asset.FindActionMap(StageInputNames.Combat.Map, false), Is.Not.Null, "원본 에셋은 그대로입니다");
            bridge.Shutdown();
        }

        [Test]
        public void Bridge_LifecycleGuards()
        {
            var router = new InputRouter();
            var bridge = CreateBridge(router);

            Assert.Throws<InvalidOperationException>(() => bridge.Initialize());
            bridge.InitData(CreateContext(CreateStageAsset()));
            Assert.Throws<InvalidOperationException>(() => bridge.InitData(CreateContext(CreateStageAsset())));

            bridge.Shutdown();
            bridge.Shutdown();
            Assert.Throws<ObjectDisposedException>(() => bridge.InitData(CreateContext(CreateStageAsset())));
            Assert.Throws<ObjectDisposedException>(() => bridge.Initialize());

            // InitData 전에 종료해도 안전합니다.
            var unused = CreateBridge(new InputRouter());
            Assert.DoesNotThrow(() => unused.Shutdown());
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.Zero);
        }

        [Test]
        public void Bridge_RejectsInvalidRegistrationLists()
        {
            var router = new InputRouter();
            var slot = new StageInputReceiverSlot();

            Assert.Throws<ArgumentNullException>(() => new UnityInputBridge(router, null, StageInputLayers.All));
            Assert.Throws<ArgumentException>(() =>
                new UnityInputBridge(router, Array.Empty<IInputController>(), StageInputLayers.All));
            Assert.Throws<ArgumentException>(() =>
                new UnityInputBridge(router, new IInputController[] { new CombatInputController(slot), null }, StageInputLayers.All));
            Assert.Throws<ArgumentNullException>(() =>
                new UnityInputBridge(router, new IInputController[] { new CombatInputController(slot) }, null));
        }

        #endregion

        #region 입력 팩토리

        [Test]
        public void InputFactory_ForwardsLifecycleAndShutsDownGroup()
        {
            var slot = new StageInputReceiverSlot();
            var controllers = new IInputController[]
            {
                new DeploymentInputController(slot),
                new CombatInputController(slot)
            };
            var group = (InputSystemGroup)InputSystemFactory.Create(controllers, StageInputLayers.All);

            group.InitData(CreateContext(CreateStageAsset()));
            group.Initialize();
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.EqualTo(1));

            group.InputLayerServices.ChangeLayer(StageInputLayers.Combat);
            Assert.That(group.InputLayerServices.CurrentLayer, Is.EqualTo(StageInputLayers.Combat));

            group.Shutdown();
            Assert.That(RuntimeCloneCount() - _cloneBaseline, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => group.InputLayerServices.ClearLayer());
        }

        #endregion

        #region 도우미

        private static InputConfiguration CreateConfiguration()
        {
            var slot = new StageInputReceiverSlot();
            return InputConfiguration.Create(
                new DeploymentInputController(slot),
                new CombatInputController(slot));
        }

        private static UnityInputBridge CreateBridge(InputRouter router)
        {
            var slot = new StageInputReceiverSlot();
            return new UnityInputBridge(router,
                new IInputController[] { new DeploymentInputController(slot), new CombatInputController(slot) },
                StageInputLayers.All);
        }

        private IHasViewQuery CreateContext(InputActionAsset actions)
        {
            var item = _assets.Create<InputSettingsDataSO>();
            Set(Get(item, "inputSettingsData"), "actions", actions);
            var table = new InputSettingsTable(_assets.Table<InputSettingsDataTableSO>(item));
            return new ViewContext(new ViewQueryStub(table));
        }

        /// <summary>프로젝트 에셋과 같은 이름·종류의 스테이지 맵을 메모리에 만듭니다.</summary>
        private InputActionAsset CreateStageAsset(bool includeEndTurn = true, bool pointerAsButton = false)
        {
            var asset = _assets.Create<InputActionAsset>();
            asset.name = "TestStageActions";

            InputActionMap common = asset.AddActionMap(StageInputNames.Common.Map);
            common.AddAction(StageInputNames.Common.OpenSettings, InputActionType.Button, "<Keyboard>/escape");

            InputActionMap deployment = asset.AddActionMap(StageInputNames.Deployment.Map);
            if (pointerAsButton)
                deployment.AddAction(StageInputNames.Deployment.PointerPosition, InputActionType.Button, "<Mouse>/leftButton");
            else
                deployment.AddAction(StageInputNames.Deployment.PointerPosition, InputActionType.Value,
                    "<Pointer>/position", expectedControlLayout: "Vector2");
            deployment.AddAction(StageInputNames.Deployment.Press, InputActionType.Button,
                "<Pointer>/press", interactions: "Press(behavior=1)");
            deployment.AddAction(StageInputNames.Deployment.Cancel, InputActionType.Button, "<Mouse>/rightButton");
            deployment.AddAction(StageInputNames.Deployment.EnterCombat, InputActionType.Button, "<Keyboard>/enter");
            deployment.AddAction(StageInputNames.Deployment.DrawCharacter, InputActionType.Button, "<Keyboard>/g");

            InputActionMap combat = asset.AddActionMap(StageInputNames.Combat.Map);
            combat.AddAction(StageInputNames.Combat.PointerPosition, InputActionType.Value,
                "<Pointer>/position", expectedControlLayout: "Vector2");
            combat.AddAction(StageInputNames.Combat.Select, InputActionType.Button,
                "<Pointer>/press", interactions: "Press(behavior=1)");
            combat.AddAction(StageInputNames.Combat.Cancel, InputActionType.Button, "<Mouse>/rightButton");
            if (includeEndTurn)
                combat.AddAction(StageInputNames.Combat.EndTurn, InputActionType.Button, "<Keyboard>/space");
            combat.AddAction(StageInputNames.Combat.MoveMode, InputActionType.Button, "<Keyboard>/q");
            combat.AddAction(StageInputNames.Combat.AttackMode, InputActionType.Button, "<Keyboard>/e");
            combat.AddAction(StageInputNames.Combat.Return, InputActionType.Button, "<Keyboard>/r");

            return asset;
        }

        private static void AssertReleaseOnly(InputActionAsset asset, string mapName, string actionName)
        {
            InputAction action = asset.FindActionMap(mapName, true).FindAction(actionName, true);
            string label = $"{mapName}.{actionName}";

            Assert.That(action.type, Is.EqualTo(InputActionType.Button), label);
            Assert.That(action.interactions, Is.EqualTo("Press(behavior=1)"), label);
            Assert.That(action.wantsInitialStateCheck, Is.False, label);
            foreach (InputBinding binding in action.bindings)
                Assert.That(binding.interactions, Is.Null.Or.Empty, $"{label} binding {binding.path}");
        }

        private static void AssertEnabled(InputActionAsset asset, bool common, bool deployment, bool combat)
        {
            Assert.That(asset.FindActionMap(StageInputNames.Common.Map, true).enabled, Is.EqualTo(common), "Common");
            Assert.That(asset.FindActionMap(StageInputNames.Deployment.Map, true).enabled, Is.EqualTo(deployment), "Deployment");
            Assert.That(asset.FindActionMap(StageInputNames.Combat.Map, true).enabled, Is.EqualTo(combat), "Combat");
        }

        private static List<string> RecordMapChanges(InputActionAsset asset, Action action)
        {
            var changes = new List<string>();
            void OnChange(object target, InputActionChange change)
            {
                if ((change == InputActionChange.ActionMapEnabled || change == InputActionChange.ActionMapDisabled) &&
                    target is InputActionMap map && ReferenceEquals(map.asset, asset))
                    changes.Add($"{change} {map.name}");
            }

            UnityInput.onActionChange += OnChange;
            try
            {
                action();
            }
            finally
            {
                UnityInput.onActionChange -= OnChange;
            }

            return changes;
        }

        private static int RuntimeCloneCount()
        {
            return Resources.FindObjectsOfTypeAll<InputActionAsset>()
                .Count(asset => asset != null && asset.name.EndsWith(RuntimeCloneSuffix, StringComparison.Ordinal));
        }

        private sealed class ViewContext : IHasViewQuery
        {
            public IViewQuery ViewQuery { get; }

            internal ViewContext(IViewQuery viewQuery) => ViewQuery = viewQuery;
        }

        private sealed class ViewQueryStub : IViewQuery
        {
            public IStageViewPropTableQuery StageViewPropTableQuery => null;
            public IStageFieldViewTableQuery StageFieldViewTableQuery => null;
            public IStageMapViewTableQuery StageMapViewTableQuery => null;
            public IStageNodeVisualTableQuery StageNodeVisualTableQuery => null;
            public IUnitVisualTableQuery UnitVisualTableQuery => null;
            public IArtifactViewTableQuery ArtifactViewTableQuery => null;
            public ITraitVisualTableQuery TraitVisualTableQuery => null;
            public IRecoveryOptionVisualTableQuery RecoveryOptionVisualTableQuery => null;
            public IRailwayVisualTableQuery RailwayVisualTableQuery => null;
            public IInputSettingsTableQuery InputSettingsTableQuery { get; }

            internal ViewQueryStub(IInputSettingsTableQuery inputSettings) => InputSettingsTableQuery = inputSettings;
        }

        private sealed class SettingsTableStub : IInputSettingsTableQuery
        {
            public InputSettingsData Config { get; }

            internal SettingsTableStub(InputSettingsData config) => Config = config;
        }

        private sealed class OtherModule : IServiceModule
        {
        }

        #endregion
    }
}
