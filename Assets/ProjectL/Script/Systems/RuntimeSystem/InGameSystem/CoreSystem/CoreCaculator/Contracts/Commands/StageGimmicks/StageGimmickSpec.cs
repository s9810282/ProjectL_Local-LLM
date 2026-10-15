using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public abstract class StageGimmickSpec
    {
        public StageGimmickKey GimmickKey { get; }

        public StageGimmickSpec(StageGimmickKey key)
        {
            if (key == StageGimmickKey.None || Enum.IsDefined(typeof(StageGimmickKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            this.GimmickKey = key;
        }
    }
}
