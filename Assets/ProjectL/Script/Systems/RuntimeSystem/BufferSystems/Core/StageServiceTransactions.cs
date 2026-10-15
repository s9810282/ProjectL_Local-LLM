using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 비전투 스테이지(상점 진입·구매, 회복 진입·소비)를 게시한다.
    /// 진입 가능·가격·회복량 판정은 Core가 하고, 여기서는 원본 비교와 준비→설치 순서만 지킨다.
    /// </summary>
    internal sealed class StageServiceTransactions :
        IShopStageBufferWriteSink, ICharacterPurchaseBufferWriteSink, IRecoveryStageBufferWriteSink
    {
        private readonly BufferGuards _guards;
        private readonly ICurrentProgressBufferReadSink _currentRead;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly IMapProgressBufferReadSink _mapRead;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly ICharacterBufferReadSink _characterRead;
        private readonly ICharacterBufferWriteSink _characterWrite;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ICurrencyBufferStore _currencyStore;
        private readonly ICurrentProgressBufferStore _currentStore;

        internal StageServiceTransactions(BufferGuards guards, ICurrentProgressBufferReadSink currentRead,
            ICombatStateBufferReadSink combatRead, IMapProgressBufferReadSink mapRead,
            IStageProgressBufferReadSink stageRead, ICharacterBufferReadSink characterRead,
            ICharacterBufferWriteSink characterWrite, ICharacterBufferStore characterStore,
            ICurrencyBufferStore currencyStore, ICurrentProgressBufferStore currentStore)
        {
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _currentRead = currentRead ?? throw new System.ArgumentNullException(nameof(currentRead));
            _combatRead = combatRead ?? throw new System.ArgumentNullException(nameof(combatRead));
            _mapRead = mapRead ?? throw new System.ArgumentNullException(nameof(mapRead));
            _stageRead = stageRead ?? throw new System.ArgumentNullException(nameof(stageRead));
            _characterRead = characterRead ?? throw new System.ArgumentNullException(nameof(characterRead));
            _characterWrite = characterWrite ?? throw new System.ArgumentNullException(nameof(characterWrite));
            _characterStore = characterStore ?? throw new System.ArgumentNullException(nameof(characterStore));
            _currencyStore = currencyStore ?? throw new System.ArgumentNullException(nameof(currencyStore));
            _currentStore = currentStore ?? throw new System.ArgumentNullException(nameof(currentStore));
        }

        public void PrepareShop(ShopEntryPreparation preparation)
        {
            if (preparation == null) throw new System.ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            if (_combatRead.HasBattle)
                throw new System.InvalidOperationException("남아 있는 전투를 정리한 뒤 상점에 진입해야 합니다.");

            PreparedCurrentSelection current = _currentStore.PrepareShopSelection(preparation);
            if (!IsOpenTarget(preparation.ExpectedChapterKey, preparation.TargetNodeId, preparation.TargetStageKey,
                    preparation.ExpectedTargetStageType, preparation.ExpectedTargetMapRole))
                throw new System.InvalidOperationException("상점 진입 대상의 연결, 종류, 역할 또는 완료 상태가 계산 시점과 다릅니다.");

            // 모든 검증과 준비가 끝난 뒤 선택과 상품을 한 번에 설치한다.
            _currentStore.InstallSelection(current);
        }

        public CharacterPurchaseResult PublishPurchase(StageNodeId nodeId, StageKey stageKey,
            ShopOfferId offerId, int expectedBalance, CharacterPurchaseDecision decision)
        {
            if (decision == null || decision.Succeeded == false)
                throw new System.ArgumentException("승인된 구매 제안이 필요합니다.", nameof(decision));
            _guards.EnsureRunInProgress();

            CurrentShopData shop = _currentStore.PreparePurchase(nodeId, stageKey, offerId);
            if (shop.TryGetOffer(offerId, out var offer) == false ||
                offer.CharacterKey != decision.Request.CharacterKey)
                throw new System.InvalidOperationException("구매 상품과 획득할 카드가 다릅니다.");

            int amount = _currencyStore.PrepareAmount(expectedBalance, decision.FinalBalance);
            PreparedRosterAcquisition roster = _characterStore.PrepareAcquire(new[] { decision.Request });
            if (roster.Result.Succeeded == false)
                return CharacterPurchaseResult.Failure(roster.Result.FailureReason ==
                    RosterAcquireFailureReason.IdentityOverflow
                    ? ShopFailureReason.IdentityOverflow : ShopFailureReason.InvalidCharacter);

            CharacterPurchaseResult result = CharacterPurchaseResult.Success(roster.Result);
            _characterStore.InstallAcquire(roster);
            _currencyStore.InstallAmount(amount);
            _currentStore.InstallPurchase(shop);
            return result;
        }

        public void PrepareRecovery(RecoveryEntryPreparation preparation)
        {
            if (preparation == null) throw new System.ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            if (_combatRead.HasBattle)
                throw new System.InvalidOperationException("남아 있는 전투를 정리한 뒤 회복에 진입해야 합니다.");

            PreparedCurrentSelection current = _currentStore.PrepareRecoverySelection(preparation);
            if (!IsOpenTarget(preparation.ExpectedChapterKey, preparation.TargetNodeId, preparation.TargetStageKey,
                    preparation.ExpectedTargetStageType, preparation.ExpectedTargetMapRole))
                throw new System.InvalidOperationException("회복 진입 대상의 연결, 종류, 역할 또는 완료 상태가 계산 시점과 다릅니다.");

            // 모든 검증과 준비가 끝난 뒤 선택과 선택지를 한 번에 설치한다.
            _currentStore.InstallSelection(current);
        }

        public bool PublishRecovery(StageNodeId nodeId, StageKey stageKey, CharacterRecoveryDecision decision)
        {
            if (decision == null) throw new System.ArgumentNullException(nameof(decision));
            _guards.EnsureRunInProgress();
            _guards.ValidateSelection(_currentRead.CurrentMapKey, nodeId, stageKey);
            if (_combatRead.HasBattle)
                throw new System.InvalidOperationException("전투 중에는 회복 스테이지를 소비할 수 없습니다.");
            CurrentRecoveryData recovery = _currentRead.Recovery;
            if (recovery == null || !recovery.TryGetOption(decision.Option.Id, out var option))
                throw new System.InvalidOperationException("현재 회복 선택지가 아닙니다.");
            if (recovery.AppliedOptionId.HasValue)
            {
                if (recovery.AppliedOptionId != option.Id)
                    throw new System.InvalidOperationException("이미 다른 회복 선택지를 소비했습니다.");
                return false;
            }
            if (option.Cost != decision.Option.Cost || option.Heal.Mode != decision.Option.Heal.Mode ||
                option.Heal.Value != decision.Option.Heal.Value)
                throw new System.InvalidOperationException("회복 제안이 현재 선택지와 다릅니다.");
            // 어느 캐릭터의 영혼을 쓸지는 Core가 정한다. 여기서는 그 출처의 영혼 수가 계산 시점과 같은지만 본다.
            if (!_characterRead.TryGetOwned(decision.SoulSourceId, out _) ||
                _characterRead.GetSoulCount(decision.SoulSourceId) != decision.SoulCount)
                throw new System.InvalidOperationException("회복 최대 체력의 영혼 원본이 다릅니다.");
            int balance = _currencyStore.PrepareAmount(decision.ExpectedBalance, decision.FinalBalance);
            CurrentRecoveryData receipt = recovery.WithRecovery(option.Id, option.Cost, decision.HealedAmount);
            _characterWrite.Apply(decision.Characters);
            _currencyStore.InstallAmount(balance);
            _currentStore.InstallRecovery(receipt);
            return true;
        }

        /// <summary>대상 챕터가 미완료이고, 대상 노드가 Core 판정 때와 같은 종류·역할의 미완료 노드인지 본다.</summary>
        private bool IsOpenTarget(int chapterKey, StageNodeId nodeId, StageKey stageKey,
            StageType stageType, StageMapRole mapRole)
        {
            return !_mapRead.GetMap(chapterKey).IsClear &&
                _stageRead.GetStages(chapterKey).Any(stage =>
                    stage.NodeId == nodeId.Value && stage.StageKey == stageKey.Value &&
                    !stage.IsClear && stage.StageType == stageType && stage.MapRole == mapRole);
        }
    }
}
