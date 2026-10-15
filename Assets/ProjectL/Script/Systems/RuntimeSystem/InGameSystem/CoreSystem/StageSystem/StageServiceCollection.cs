using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    /// <summary>StageType별 처리 함수와 Listener 바인딩을 한곳에서 제공합니다.</summary>
    internal static class StageServiceCollection
    {
        internal static Dictionary<StageType, Action<StageRootData>> CreateBindings()
        {
            return new Dictionary<StageType, Action<StageRootData>>
            {
                { StageType.Combat, HandleCombat },
                { StageType.Shop, HandleShop },
                { StageType.Event, HandleEvent }
            };
        }

        private static void HandleCombat(StageRootData data) => Handle(StageType.Combat, data);
        private static void HandleShop(StageRootData data) => Handle(StageType.Shop, data);
        private static void HandleEvent(StageRootData data) => Handle(StageType.Event, data);

        private static void Handle(StageType expectedType, StageRootData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.StageData.StageType != expectedType)
                throw new InvalidOperationException($"Expected {expectedType} stage data.");

            // 실제 Combat / Shop / Event 진입 처리는 각 서비스가 추가될 때 이 함수에 연결합니다.
            data.StageData.Validate();
        }
    }
}
