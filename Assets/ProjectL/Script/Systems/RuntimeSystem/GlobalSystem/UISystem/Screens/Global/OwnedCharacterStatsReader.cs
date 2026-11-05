using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>표시 중 원본이 바뀔 때만 비전투 보유 능력치를 다시 조회합니다.</summary>
    internal sealed class OwnedCharacterStatsReader
    {
        private readonly ICombatStateBufferReadSink _combat;
        private readonly ICharacterStatsReadSink _stats;
        private readonly ICharacterStatsRequestSink _requests;
        private readonly IUIRuntimeFaultHandler _faults;
        private readonly IRunDeckReadSink _deck;
        private CharacterStatsQueryResult _result;
        internal bool IsFaulted => _faults?.IsFaulted ?? false;

        /// <param name="deck">선택된 메인의 출처입니다(런 덱 인계 §4, R6). 확정 덱이 없으면 <see cref="Main"/>은 null입니다.</param>
        internal OwnedCharacterStatsReader(ICharacterBufferReadSink characters,
            ICombatStateBufferReadSink combat, ICharacterStatsReadSink stats, IRunDeckReadSink deck,
            IUIRuntimeFaultHandler faults = null, ICharacterStatsRequestSink requests = null)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _requests = requests;
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _faults = faults;
        }

        internal bool Refresh()
        {
            if (_faults?.IsFaulted == true) return false;
            if (_combat.HasBattle)
            {
                bool changed = _result != null;
                Clear();
                return changed;
            }

            // Buffer 읽기는 게시된 참조 비교뿐이며 계산을 호출하지 않는다. Pending도 정상적인 게시 대기다.
            CharacterStatsQueryResult result;
            try
            {
                result = _stats.ReadOwned();
                if (result.Status == CharacterStatsQueryStatus.Pending)
                {
                    if (_requests == null)
                        throw new InvalidOperationException("[OwnedCharacterStatsReader] 게시 대기 중인 Buffer에는 요청 Sink가 필요합니다.");
                    _requests.RequestRefresh();
                }
            }
            catch (Exception error)
            {
                Clear();
                _faults?.Report(error);
                if (!IsFaulted) throw;
                return false;
            }
            if (ReferenceEquals(_result, result)) return false;
            _result = result;
            return true;
        }

        internal OwnedCharacterStatsSnapshot Find(CharacterInstanceId id)
        {
            if (_result?.Status != CharacterStatsQueryStatus.Ready) return null;
            for (int i = 0; i < _result.Items.Count; i++)
            {
                var item = _result.Items[i];
                if (item.OwnedId == id) return item;
            }
            return null;
        }

        internal OwnedCharacterStatsSnapshot Main
        {
            get
            {
                if (_result?.Status != CharacterStatsQueryStatus.Ready) return null;
                if (!_deck.TryGetCurrent(out var deck)) return null;
                return Find(deck.MainId);
            }
        }

        internal void Clear()
        {
            _result = null;
        }
    }
}
