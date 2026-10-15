using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks
{
    public abstract class StageGimmickActivationSnapshot
    {
        public StageGimmickId GimmickId { get; }
        public StageGimmickKey GimmickKey { get; }

        protected StageGimmickActivationSnapshot(
            StageGimmickId gimmickId,
            StageGimmickKey gimmickKey)
        {
            if (gimmickId.IsEmpty)
                throw new ArgumentException("Stage Gimmick ID는 비어 있을 수 없습니다.", nameof(gimmickId));
            if (gimmickKey == StageGimmickKey.None ||
                Enum.IsDefined(typeof(StageGimmickKey), gimmickKey) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(gimmickKey));
            }

            GimmickId = gimmickId;
            GimmickKey = gimmickKey;
        }
    }
}
