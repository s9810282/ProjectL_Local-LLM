using System.Collections.Generic;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame;
using ProjectL.Systems.Factory.Runtime.LifeCycle;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime
{
    #region 시스템 모듈

    internal static class FrameworkRuntimeFactory
    {
        /// <summary>
        /// 루트 런타임을 조립합니다. 인자를 생략하면 기본 조립(준비 입력 없음, Live, 로그를 남기는 기본 오류 경계)입니다.
        /// </summary>
        /// <remarks>
        /// 오류 경계는 런타임당 하나이며 같은 인스턴스를 InGame·LifeCycle에 전달합니다(설계 §8.3).
        /// 준비 입력은 Core에만, 실행 모드·오류 경계는 View·LifeCycle에만 전달합니다.
        /// 리플렉션으로 호출할 때는 선택 인자가 자동으로 채워지지 않으므로 인자 배열을 모두 넘깁니다.
        /// </remarks>
        public static FrameworkRuntime Create(
            IBattleInitializationSource battleSource = null,
            CombatExecutionMode executionMode = CombatExecutionMode.Live,
            RuntimeFaultBoundary faultBoundary = null)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper systemBuilderHelper = new();

            // 스테이지 내부 입력: Controller는 Global 입력 경로표에 한 번 등록되고,
            // View는 루트에 등록된 수신 슬롯에 붙었다 떨어진다.
            var stageInputSlot = new StageInputReceiverSlot();
            stageInputSlot.ExportServices(systemBuilderHelper);
            var stageSelectionSlot = new StageSelectionReceiverSlot();
            stageSelectionSlot.ExportServices(systemBuilderHelper);
            var artifactSlot = new ArtifactInputSlot();
            artifactSlot.ExportServices(systemBuilderHelper);
            IInputController[] inputControllers =
            {
                new DeploymentInputController(stageInputSlot),
                new CombatInputController(stageInputSlot),
                new StageSelectionInputController(stageSelectionSlot),
                new ArtifactInputController(artifactSlot)
            };

            // 각 화면의 Layer를 같은 입력 시스템에 한 번만 등록합니다.
            var inputLayers = new List<InputLayer>(StageInputLayers.All)
            {
                StageSelectionInputLayers.Selection,
                ArtifactInputLayers.Artifact
            };

            BufferSystemFactory.Create().ExportServices(systemBuilderHelper);
            GlobalSystemFactory.Create(inputControllers, inputLayers).ExportServices(systemBuilderHelper);
            InGameSystemFactory.Create(battleSource, executionMode, faults).ExportServices(systemBuilderHelper);
            LifeCycleSystemFactory.Create(executionMode, faults).ExportServices(systemBuilderHelper);

            return new FrameworkRuntime(systemBuilderHelper.BuildSystemReader());
        }
    }

    #endregion
}
