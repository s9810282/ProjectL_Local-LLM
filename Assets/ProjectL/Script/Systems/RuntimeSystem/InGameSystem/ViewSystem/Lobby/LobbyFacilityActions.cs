using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 정상 도착한 시설의 실행 항목을 종류로 찾아 엽니다. 모든 시설은 Scope의 시설 실행 판정(막힘 아님 + 로비 Layer)을
    /// 먼저 거칩니다(로비 리팩토링 2차 설계 M-02). 종류 → 항목 표 하나가 실행 지식의 원본입니다(M-13·N-54).
    /// </summary>
    internal sealed class LobbyFacilityActions : ILobbyFacilityActions
    {
        private readonly Dictionary<LobbyFacilityKind, ILobbyFacilityAction> _actions = new();
        private readonly Func<bool> _canRun;

        /// <summary>모든 시설 종류에 항목이 정확히 하나씩 있어야 합니다.</summary>
        internal LobbyFacilityActions(IEnumerable<KeyValuePair<LobbyFacilityKind, ILobbyFacilityAction>> actions,
            Func<bool> canRun)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            _canRun = canRun ?? throw new ArgumentNullException(nameof(canRun));
            foreach (KeyValuePair<LobbyFacilityKind, ILobbyFacilityAction> pair in actions)
            {
                if (pair.Value == null)
                    throw new ArgumentException($"[Lobby] 시설 '{pair.Key}'의 실행 항목이 없습니다.", nameof(actions));
                if (!_actions.TryAdd(pair.Key, pair.Value))
                    throw new ArgumentException($"[Lobby] 시설 '{pair.Key}'의 실행 항목이 두 번 있습니다.", nameof(actions));
            }
            foreach (LobbyFacilityKind kind in LobbyFacilityKinds.All)
            {
                if (!_actions.ContainsKey(kind))
                    throw new ArgumentException($"[Lobby] 시설 '{kind}'의 실행 항목이 필요합니다.", nameof(actions));
            }
            if (_actions.Count != LobbyFacilityKinds.Count)
                throw new ArgumentException("[Lobby] 정의되지 않은 시설 종류의 실행 항목이 있습니다.", nameof(actions));
        }

        public bool TryExecute(LobbyFacilityKind kind)
        {
            if (!_canRun() || !_actions.TryGetValue(kind, out ILobbyFacilityAction action) || !action.CanOpen) return false;
            action.Open();
            return true;
        }
    }
}
