using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageGimmicks.Definitions
{
    internal abstract class StageGimmickDefinition
    {
        public StageGimmickKey Key { get; }

        protected StageGimmickDefinition(StageGimmickKey key)
        {
            if (key == StageGimmickKey.None ||
                Enum.IsDefined(typeof(StageGimmickKey), key) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            Key = key;
        }
    }
}
