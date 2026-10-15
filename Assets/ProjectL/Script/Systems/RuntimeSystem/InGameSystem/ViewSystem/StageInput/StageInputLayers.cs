using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 스테이지 내부 입력 Layer입니다. 입력을 모두 끌 때는 별도 Layer를 두지 않고 ClearLayer를 씁니다.
    /// </summary>
    internal static class StageInputLayers
    {
        /// <summary>전투 진입 전 배치 단계입니다.</summary>
        internal static readonly InputLayer Deployment = new(
            StageInputNames.Deployment.Map,
            StageInputNames.Common.Map,
            StageInputNames.Deployment.Map);

        /// <summary>전투 단계입니다.</summary>
        internal static readonly InputLayer Combat = new(
            StageInputNames.Combat.Map,
            StageInputNames.Common.Map,
            StageInputNames.Combat.Map);

        /// <summary>루트 조립 지점이 입력 연결 계층에 넘기는 등록 목록입니다.</summary>
        internal static IReadOnlyList<InputLayer> All { get; } =
            Array.AsReadOnly(new[] { Deployment, Combat });
    }
}
