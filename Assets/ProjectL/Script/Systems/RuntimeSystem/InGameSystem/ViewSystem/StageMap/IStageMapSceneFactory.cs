using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬 연결 한 번의 Scope를 만드는 유일한 입구입니다. 팝업 Factory 조립은 구현(Factory 폴더)이 맡고,
    /// System은 씬을 찾고 서비스 묶음을 넘기기만 합니다(검사 후속 정리 계획 TP-R1).
    /// </summary>
    internal interface IStageMapSceneFactory
    {
        /// <param name="output">화면을 그리는 출력 카메라입니다(Boot의 Main Camera).</param>
        StageMapViewScope Create(StageMapSceneRoot root, Camera output, StageMapViewServices services,
            RuntimeFaultBoundary faults);

        ILodingPreparation CreatePreparation(StageMapViewScope scope, RuntimeFaultBoundary faults);

        StageNodeClearPresentation CreateCompletion(StageMapViewScene scene, StageMapViewCompletionServices services,
            StageMapCameraFocus focus, Func<bool> canWork, Func<StageMapMovementContext> captureContext,
            Func<StageMapMovementContext, bool> isContextCurrent, Action clearPointer);
    }
}
