using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 스테이지 내부 입력 Layer입니다. 입력을 모두 끌 때는 별도 Layer를 두지 않고 ClearLayer를 씁니다.
    /// 카메라 조작 맵은 두 Layer가 함께 켜서 배치·전투 단계 모두 움직입니다(카메라 조작 설계 CC2). 두 Layer 사이를 바꿀 때 끄거나 다시 켜지 않습니다.
    /// </summary>
    internal static class StageInputLayers
    {
        /// <summary>전투 진입 전 배치 단계입니다.</summary>
        internal static readonly InputLayer Deployment = new(
            StageInputNames.Deployment.Map,
            StageInputNames.Common.Map,
            StageInputNames.Deployment.Map,
            StageInputNames.CombatCamera.Map);

        /// <summary>전투 단계입니다.</summary>
        internal static readonly InputLayer Combat = new(
            StageInputNames.Combat.Map,
            StageInputNames.Common.Map,
            StageInputNames.Combat.Map,
            StageInputNames.CombatCamera.Map);

        /// <summary>스테이지 입력 조립이 입력 연결 계층에 전달하는 등록 목록입니다.</summary>
        internal static IReadOnlyList<InputLayer> All { get; } =
            Array.AsReadOnly(new[] { Deployment, Combat });
    }
}
