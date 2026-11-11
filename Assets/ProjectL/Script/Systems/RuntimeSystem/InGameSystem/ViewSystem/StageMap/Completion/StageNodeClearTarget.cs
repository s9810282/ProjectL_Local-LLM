using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>준비된 표시에서 빌린 파괴 대상입니다. 준비 버전이 바뀌면 재사용하지 않습니다.</summary>
    internal readonly struct StageNodeClearTarget
    {
        internal StageNodeClearTarget(StageNodeClearKey key, EffectType breakEffect, Pose worldPose,
            Transform mapRoot, long preparationVersion)
        {
            Key = key;
            BreakEffect = breakEffect;
            WorldPose = worldPose;
            MapRoot = mapRoot;
            PreparationVersion = preparationVersion;
        }

        internal StageNodeClearKey Key { get; }
        internal EffectType BreakEffect { get; }
        internal Pose WorldPose { get; }
        internal Transform MapRoot { get; }
        internal long PreparationVersion { get; }
    }
}
