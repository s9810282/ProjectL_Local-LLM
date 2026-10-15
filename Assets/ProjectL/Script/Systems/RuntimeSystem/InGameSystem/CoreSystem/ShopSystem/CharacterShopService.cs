using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem
{
    public interface ICharacterShopService : IServiceModule
    {
        /// <summary>합법적인 대상 선택과 DB 상품을 함께 게시한다. 화면 전환은 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
        /// <summary>지정된 현재 노드와 고정 상품을 검증하여 구매한다.</summary>
        CharacterPurchaseResult Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId);
        /// <summary>구매 여부와 관계없이 현재 상점 노드만 완료한다. 구매 반영은 다시 실행하지 않는다.</summary>
        ChapterProgressionResult CompleteShop(int chapterKey, StageNodeId nodeId);
        ShopFailureReason PrepareShop(int seed);
        CharacterPurchaseResult Purchase(ShopOfferId offerId);
    }

    /// <summary>현재 상점의 사실을 Core 규칙에 전달하고 Buffer 그룹에 구매 반영을 요청한다.</summary>
    internal sealed class CharacterShopService : ICharacterShopService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly ChapterProgressionCoordinator _progression;
        private IUnitTableQuery _units;
        private IStageTableQuery _stages;
        private IBufferQuery _buffers;

        internal CharacterShopService(ChapterProgressionCoordinator progression)
            => _progression = progression ?? throw new ArgumentNullException(nameof(progression));

        public void InitData(IDataBaseQueryContext data)
        {
            var query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
            _units = query.UnitTableQuery;
            _stages = query.StageTableQuery;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public ShopFailureReason PrepareShop(int seed)
        {
            RequireReady();
            if (RunEnded) return ShopFailureReason.RunEnded;
            var current = _buffers.CurrentProgressReadSink;
            if (current.HasSelection == false || _buffers.CombatStateReadSink.HasBattle)
                return ShopFailureReason.WrongPhase;

            var stage = _stages.GetStage(current.CurrentStage.StageKey);
            if (RunEnded) return ShopFailureReason.RunEnded;
            if (!TryGetShopDetail(stage, current.CurrentMapKey, new StageKey(current.CurrentStage.StageKey), out var shop))
                return ShopFailureReason.WrongPhase;
            if (current.Shop != null)
                return ShopFailureReason.None;

            if (TryPlanOffers(shop, seed, out var offers) == false)
                return ShopFailureReason.InvalidCharacter;
            if (RunEnded) return ShopFailureReason.RunEnded;

            // 선택된 상점에 상품만 늦게 설치하는 기존 경계다(원본 비교 없음). 새 흐름은 EnterShop의 준비→설치 제안을 쓴다.
            _buffers.CurrentProgressWriteSink.SetShopOffers(offers);
            return ShopFailureReason.None;
        }

        public ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            RequireReady();
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 상점에 진입해야 합니다.");

            var current = _buffers.CurrentProgressReadSink;
            int originChapter = current.CurrentMapKey;
            bool originSelected = current.HasSelection;
            StageSelectionData originStage = current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return decision;

            var root = _stages.GetStage(decision.StageKey.Value)
                ?? throw new InvalidOperationException("상점 스테이지 정의가 필요합니다.");
            root.Validate();
            if (!TryGetShopDetail(root, decision.ChapterKey, decision.StageKey, out var shop))
                throw new InvalidOperationException("대상은 지정된 챕터의 일반 상점 스테이지여야 합니다.");
            if (!TryPlanOffers(shop, seed, out var offers))
                throw new InvalidOperationException("상점의 모든 후보는 획득 가능한 플레이어 일반 캐릭터여야 합니다.");

            // 마지막 DB 조회 이후 원본과 대상이 그대로일 때만 Buffer 게시를 요청한다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            // 사전 검사 자체의 DB 조회 중 진행 상태가 바뀐 경우도 다시 판정한다.
            revalidated = _progression.RevalidateStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            if (current.CurrentMapKey != originChapter || current.HasSelection != originSelected ||
                current.CurrentStage.NodeId != originStage.NodeId || current.CurrentStage.StageKey != originStage.StageKey ||
                current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            _buffers.ShopStageWriteSink.PrepareShop(new ShopEntryPreparation(originChapter,
                originSelected, originStage, revalidated.NodeId, revalidated.StageKey,
                $"Stage {revalidated.StageKey.Value}", offers, StageType.Shop, root.MapRole));
            return revalidated;
        }

        private bool TryPlanOffers(ShopStageDetail shop, int seed, out IReadOnlyList<ShopPurchaseSnapshot> offers)
        {
            var candidates = new ShopCandidateSpec[shop.Candidates.Count];
            var definitions = new CharacterDefinitionFacts[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = shop.Candidates[i];
                candidates[i] = new ShopCandidateSpec(candidate.UnitKey, candidate.Cost);
                definitions[i] = CharacterDefinitionAdapter.Read(_units, candidate.UnitKey);
            }

            return CharacterShopRules.TryPlanOffers(candidates, definitions, seed, out offers);
        }

        public CharacterPurchaseResult Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId)
        {
            ValidateShopArguments(chapterKey, nodeId);
            RequireReady();
            ShopFailureReason failure = ValidatePurchaseStorage(chapterKey, nodeId, out StageKey stageKey);
            if (failure != ShopFailureReason.None) return CharacterPurchaseResult.Failure(failure);
            var shop = _buffers.CurrentProgressReadSink.Shop;
            if (!IsShopDefinition(chapterKey, stageKey))
                return CharacterPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            failure = RevalidatePurchase(chapterKey, nodeId, stageKey, shop);
            if (failure != ShopFailureReason.None) return CharacterPurchaseResult.Failure(failure);

            shop.TryGetOffer(offerId, out var offer);
            var definition = offer == null ? null : CharacterDefinitionAdapter.Read(_units, offer.CharacterKey);
            // 마지막 유닛 조회가 선택이나 전투를 바꾸었으면 결제와 획득을 게시하지 않는다.
            failure = RevalidatePurchase(chapterKey, nodeId, stageKey, shop);
            if (failure != ShopFailureReason.None) return CharacterPurchaseResult.Failure(failure);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            int balance = _buffers.CurrencyReadSink.Amount;
            var decision = CharacterShopRules.PlanPurchase(offer, shop.IsPurchased(offerId), balance, definition, main);
            return decision.Succeeded
                ? _buffers.CharacterPurchaseWriteSink.PublishPurchase(nodeId, stageKey, offerId, balance, decision)
                : CharacterPurchaseResult.Failure(decision.FailureReason);
        }

        public ChapterProgressionResult CompleteShop(int chapterKey, StageNodeId nodeId)
        {
            ValidateShopArguments(chapterKey, nodeId);
            RequireReady();
            ChapterProgressionResult target = ValidateCompletionStorage(chapterKey, nodeId);
            if (!target.Accepted) return target;
            var shop = _buffers.CurrentProgressReadSink.Shop;
            if (!IsShopDefinition(chapterKey, target.StageKey))
                throw new InvalidOperationException("현재 선택은 해당 챕터의 일반 상점 스테이지여야 합니다.");
            // 정의 조회 후에도 같은 준비 기록과 미완료 노드여야 완료를 게시한다.
            ChapterProgressionResult current = ValidateCompletionStorage(chapterKey, nodeId);
            if (!current.Accepted) return current;
            if (current.StageKey != target.StageKey || !ReferenceEquals(shop, _buffers.CurrentProgressReadSink.Shop))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            return _progression.CompleteShopStage(chapterKey, nodeId, target.StageKey);
        }

        private static void ValidateShopArguments(int chapterKey, StageNodeId nodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty) throw new ArgumentException("상점 노드 ID가 필요합니다.", nameof(nodeId));
        }

        private bool IsShopDefinition(int chapterKey, StageKey stageKey)
        {
            var root = _stages.GetStage(stageKey.Value)
                ?? throw new InvalidOperationException("현재 상점 스테이지의 정의가 필요합니다.");
            root.Validate();
            return TryGetShopDetail(root, chapterKey, stageKey, out _);
        }

        /// <summary>상점 노드의 지도 역할이다. 상점 진입·준비·구매·완료가 모두 이 값 하나로 판정한다.</summary>
        private const StageMapRole ShopMapRole = StageMapRole.Regular;

        /// <summary>DB 정의가 지정된 챕터·스테이지의 상점인지 판정하고, 맞으면 상점 상세를 돌려준다.</summary>
        private static bool TryGetShopDetail(StageRootData root, int chapterKey, StageKey stageKey, out ShopStageDetail shop)
        {
            shop = root.StageKey == stageKey.Value && root.MapKey == chapterKey && root.MapRole == ShopMapRole
                ? root.StageData as ShopStageDetail : null;
            return shop != null;
        }

        /// <summary>진행 기록의 노드가 상점 노드인지 판정한다. 정의 판정(TryGetShopDetail)과 같은 역할 값을 쓴다.</summary>
        private static bool IsShopNode(StageProgressData node)
            => node.StageType == StageType.Shop && node.MapRole == ShopMapRole;

        private ShopFailureReason RevalidatePurchase(int chapterKey, StageNodeId nodeId, StageKey expectedKey, CurrentShopData expectedShop)
        {
            var failure = ValidatePurchaseStorage(chapterKey, nodeId, out StageKey key);
            if (failure != ShopFailureReason.None) return failure;
            if (key != expectedKey) return ShopFailureReason.EntryMismatch;
            return ReferenceEquals(expectedShop, _buffers.CurrentProgressReadSink.Shop)
                ? ShopFailureReason.None : ShopFailureReason.StaleEntry;
        }

        private ShopFailureReason ValidatePurchaseStorage(int chapterKey, StageNodeId nodeId, out StageKey stageKey)
        {
            stageKey = StageKey.Empty;
            if (RunEnded) return ShopFailureReason.RunEnded;
            if (_buffers.CombatStateReadSink.HasBattle) return ShopFailureReason.WrongPhase;
            var current = _buffers.CurrentProgressReadSink;
            if (current.CurrentMapKey != chapterKey) return ShopFailureReason.EntryMismatch;
            if (!current.HasSelection) return ShopFailureReason.WrongPhase;
            if (current.CurrentStage.NodeId != nodeId.Value) return ShopFailureReason.EntryMismatch;
            stageKey = new StageKey(current.CurrentStage.StageKey);
            if (_buffers.MapProgressReadSink.GetMap(chapterKey).IsClear) return ShopFailureReason.StaleEntry;
            foreach (var node in _buffers.StageProgressReadSink.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                if (node.StageKey != stageKey.Value) return ShopFailureReason.EntryMismatch;
                if (node.IsClear) return ShopFailureReason.StaleEntry;
                if (current.Shop == null || current.Recovery != null || !IsShopNode(node))
                    return ShopFailureReason.WrongPhase;
                return ShopFailureReason.None;
            }
            return ShopFailureReason.EntryMismatch;
        }

        private ChapterProgressionResult ValidateCompletionStorage(int chapterKey, StageNodeId nodeId)
        {
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 상점을 완료해야 합니다.");
            var current = _buffers.CurrentProgressReadSink;
            if (current.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!current.HasSelection)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            if (current.CurrentStage.NodeId != nodeId.Value)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (_buffers.MapProgressReadSink.GetMap(chapterKey).IsClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted);
            foreach (var node in _buffers.StageProgressReadSink.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                if (node.StageKey != current.CurrentStage.StageKey)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
                if (node.IsClear)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared);
                if (current.Shop == null || current.Recovery != null || !IsShopNode(node))
                    throw new InvalidOperationException("준비된 일반 상점 노드가 필요합니다.");
                return ChapterProgressionResult.EnterStage(chapterKey, nodeId, new StageKey(node.StageKey));
            }
            return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
        }

        public CharacterPurchaseResult Purchase(ShopOfferId offerId)
        {
            RequireReady();
            if (RunEnded) return CharacterPurchaseResult.Failure(ShopFailureReason.RunEnded);
            var current = _buffers.CurrentProgressReadSink;
            if (current.HasSelection == false || current.Shop == null ||
                _buffers.CombatStateReadSink.HasBattle)
                return CharacterPurchaseResult.Failure(ShopFailureReason.WrongPhase);

            current.Shop.TryGetOffer(offerId, out var offer);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            int balance = _buffers.CurrencyReadSink.Amount;
            CharacterPurchaseDecision decision = CharacterShopRules.PlanPurchase(offer,
                current.Shop.IsPurchased(offerId), balance,
                offer == null ? null : CharacterDefinitionAdapter.Read(_units, offer.CharacterKey), main);
            if (RunEnded) return CharacterPurchaseResult.Failure(ShopFailureReason.RunEnded);
            if (decision.Succeeded == false)
                return CharacterPurchaseResult.Failure(decision.FailureReason);

            return _buffers.CharacterPurchaseWriteSink.PublishPurchase(
                new StageNodeId(current.CurrentStage.NodeId), new StageKey(current.CurrentStage.StageKey),
                offerId, balance, decision);
        }

        private bool RunEnded => _buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress;

        private void RequireReady()
        {
            if (_units == null || _stages == null || _buffers == null)
                throw new InvalidOperationException("상점 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterShopService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
