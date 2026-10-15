using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    #region 서비스

    public interface ICurrentProgressBufferReadSink : IServiceModule
    {
        bool HasMap { get; }
        int CurrentMapKey { get; }
        bool HasSelection { get; }
        StageSelectionData CurrentStage { get; }
        CurrentShopData Shop { get; }
        CurrentRecoveryData Recovery { get; }
    }

    /// <summary>원본 비교 없이 바로 쓰는 경로다. 초기화·테스트 준비와 설계상 남겨 둔 경계(ConfirmStageEntry 등)에서만 쓴다.
    /// 새 게임 흐름은 기대값을 담은 준비→설치 제안(IBufferQuery의 Publish*·Prepare* 경로)을 쓴다.</summary>
    public interface ICurrentProgressBufferWriteSink : IServiceModule
    {
        void SelectMap(int mapKey);
        void SelectStage(StageNodeId nodeId, StageKey stageKey, string stageName);
        void SetShopOffers(IReadOnlyList<ShopPurchaseSnapshot> offers);
        void SetRecoveryOptions(IReadOnlyList<RecoveryOptionSpec> options);
        void Clear();
    }

    internal interface ICurrentProgressBufferStore : IServiceModule
    {
        PreparedCurrentMap PrepareMapSelection(int mapKey);
        void InstallMapSelection(PreparedCurrentMap prepared);
        PreparedCurrentSelection PrepareSelection(BattleEntrySelection selection);
        PreparedCurrentSelection PrepareRecoverySelection(RecoveryEntryPreparation preparation);
        PreparedCurrentSelection PrepareShopSelection(ShopEntryPreparation preparation);
        void InstallSelection(PreparedCurrentSelection prepared);
        CurrentShopData PreparePurchase(StageNodeId nodeId, StageKey stageKey, ShopOfferId offerId);
        void InstallPurchase(CurrentShopData prepared);
        void InstallRecovery(CurrentRecoveryData prepared);
    }

    internal sealed class PreparedCurrentMap
    {
        internal readonly int MapKey;
        internal PreparedCurrentMap(int mapKey) => MapKey = mapKey;
    }

    internal sealed class PreparedCurrentSelection
    {
        internal StageSelectionData Stage { get; }
        internal CurrentShopData Shop { get; }
        internal CurrentRecoveryData Recovery { get; }

        internal PreparedCurrentSelection(StageSelectionData stage, CurrentShopData shop, CurrentRecoveryData recovery)
        {
            Stage = stage;
            Shop = shop;
            Recovery = recovery;
        }
    }

    // 장면 객체와 독립적인 기본값으로 선택 정보를 보관한다.
    [Serializable]
    public readonly struct StageSelectionData
    {
        public readonly int NodeId;
        public readonly int StageKey;
        public readonly string StageName;

        internal StageSelectionData(int nodeId, int stageKey, string stageName)
        {
            NodeId = nodeId;
            StageKey = stageKey;
            StageName = stageName;
        }

        /// <summary>진입 제안(전투·상점·회복)이 공통으로 쓰는 기대 원본·대상 선택의 형식 검사다.</summary>
        internal static void RequireEntry(int expectedChapterKey, bool expectedHasSelection, StageSelectionData expectedStage,
            StageNodeId targetNodeId, StageKey targetStageKey, string targetStageName)
        {
            if (expectedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(expectedChapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("대상 노드 ID가 필요합니다.", nameof(targetNodeId));
            if (targetStageKey.IsEmpty) throw new ArgumentException("대상 스테이지 키가 필요합니다.", nameof(targetStageKey));
            if (string.IsNullOrWhiteSpace(targetStageName))
                throw new ArgumentException("대상 스테이지 이름이 필요합니다.", nameof(targetStageName));
            if (expectedHasSelection)
            {
                if (expectedStage.NodeId < 1 || expectedStage.StageKey < 1 || string.IsNullOrWhiteSpace(expectedStage.StageName))
                    throw new ArgumentException("기존 선택의 유효한 원본이 필요합니다.", nameof(expectedStage));
            }
            else if (expectedStage.NodeId != 0 || expectedStage.StageKey != 0 || expectedStage.StageName != null)
                throw new ArgumentException("선택이 없을 때 원본은 기본값이어야 합니다.", nameof(expectedStage));
        }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CurrentProgressBuffer :
        ICurrentProgressBufferReadSink, ICurrentProgressBufferWriteSink,
        ICurrentProgressBufferStore, IExportSystemServices
    {
        public bool HasMap => CurrentMapKey > 0;
        public int CurrentMapKey { get; private set; }
        public bool HasSelection { get; private set; }
        public StageSelectionData CurrentStage { get; private set; }
        public CurrentShopData Shop { get; private set; }
        public CurrentRecoveryData Recovery { get; private set; }

        public void SelectMap(int mapKey)
            => InstallMapSelection(PrepareMapSelection(mapKey));

        public PreparedCurrentMap PrepareMapSelection(int mapKey)
        {
            if (mapKey < 1) throw new ArgumentOutOfRangeException(nameof(mapKey));
            return new PreparedCurrentMap(mapKey);
        }

        public void InstallMapSelection(PreparedCurrentMap prepared)
        {
            if (CurrentMapKey == prepared.MapKey) return;
            CurrentMapKey = prepared.MapKey;
            CurrentStage = default;
            HasSelection = false;
            Shop = null;
            Recovery = null;
        }

        public void SelectStage(StageNodeId nodeId, StageKey stageKey, string stageName)
        {
            if (!HasMap) throw new InvalidOperationException("Select a map before selecting a stage.");
            if (nodeId.IsEmpty) throw new ArgumentException("Node ID is required.", nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException("Stage key is required.", nameof(stageKey));
            if (string.IsNullOrWhiteSpace(stageName)) throw new ArgumentException("Stage name is required.", nameof(stageName));
            if (HasSelection == false || CurrentStage.NodeId != nodeId.Value ||
                CurrentStage.StageKey != stageKey.Value)
            {
                Shop = null;
                Recovery = null;
            }
            CurrentStage = new StageSelectionData(nodeId.Value, stageKey.Value, stageName);
            HasSelection = true;
        }

        public void Clear()
        {
            CurrentMapKey = 0;
            CurrentStage = default;
            HasSelection = false;
            Shop = null;
            Recovery = null;
        }

        public void SetShopOffers(IReadOnlyList<ShopPurchaseSnapshot> offers)
        {
            if (HasSelection == false || Shop != null || Recovery != null)
                throw new InvalidOperationException("상점 상품을 설치할 현재 선택 상태가 맞지 않습니다.");
            Shop = new CurrentShopData(offers);
        }

        public void SetRecoveryOptions(IReadOnlyList<RecoveryOptionSpec> options)
        {
            if (!HasSelection || Shop != null || Recovery != null)
                throw new InvalidOperationException("회복 선택지를 설치할 현재 선택 상태가 맞지 않습니다.");
            Recovery = new CurrentRecoveryData(options);
        }

        // 진입 제안이 읽은 위치·선택이 지금과 같은지 비교한다(전투·상점·회복 공통).
        private bool MatchesExpected(int chapterKey, bool hasSelection, StageSelectionData expected) =>
            CurrentMapKey == chapterKey && HasSelection == hasSelection &&
            CurrentStage.NodeId == expected.NodeId && CurrentStage.StageKey == expected.StageKey &&
            CurrentStage.StageName == expected.StageName;

        PreparedCurrentSelection ICurrentProgressBufferStore.PrepareSelection(BattleEntrySelection selection)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (!MatchesExpected(selection.ExpectedChapterKey, selection.ExpectedHasSelection, selection.ExpectedStage))
                throw new InvalidOperationException("전투 진입 제안의 선택 원본이 현재 선택과 다릅니다.");

            bool sameStage = HasSelection && CurrentStage.NodeId == selection.TargetNodeId.Value &&
                CurrentStage.StageKey == selection.TargetStageKey.Value;
            return new PreparedCurrentSelection(new StageSelectionData(selection.TargetNodeId.Value,
                selection.TargetStageKey.Value, selection.TargetStageName),
                sameStage ? Shop : null, sameStage ? Recovery : null);
        }

        PreparedCurrentSelection ICurrentProgressBufferStore.PrepareRecoverySelection(RecoveryEntryPreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            if (!MatchesExpected(preparation.ExpectedChapterKey, preparation.ExpectedHasSelection, preparation.ExpectedStage))
                throw new InvalidOperationException("회복 진입 제안의 선택 원본이 현재 선택과 다릅니다.");
            if (HasSelection && CurrentStage.NodeId == preparation.TargetNodeId.Value &&
                CurrentStage.StageKey == preparation.TargetStageKey.Value)
                throw new InvalidOperationException("같은 회복 노드의 선택지를 다시 설치할 수 없습니다.");

            return new PreparedCurrentSelection(new StageSelectionData(preparation.TargetNodeId.Value,
                preparation.TargetStageKey.Value, preparation.TargetStageName), null,
                new CurrentRecoveryData(preparation.Options));
        }

        PreparedCurrentSelection ICurrentProgressBufferStore.PrepareShopSelection(ShopEntryPreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            if (!MatchesExpected(preparation.ExpectedChapterKey, preparation.ExpectedHasSelection, preparation.ExpectedStage))
                throw new InvalidOperationException("상점 진입 제안의 선택 원본이 현재 선택과 다릅니다.");
            if (HasSelection && CurrentStage.NodeId == preparation.TargetNodeId.Value &&
                CurrentStage.StageKey == preparation.TargetStageKey.Value)
                throw new InvalidOperationException("같은 상점 노드의 상품을 다시 설치할 수 없습니다.");

            return new PreparedCurrentSelection(new StageSelectionData(preparation.TargetNodeId.Value,
                preparation.TargetStageKey.Value, preparation.TargetStageName), new CurrentShopData(preparation.Offers), null);
        }

        void ICurrentProgressBufferStore.InstallSelection(PreparedCurrentSelection prepared)
        {
            if (prepared == null) throw new ArgumentNullException(nameof(prepared));
            CurrentStage = prepared.Stage;
            Shop = prepared.Shop;
            Recovery = prepared.Recovery;
            HasSelection = true;
        }

        CurrentShopData ICurrentProgressBufferStore.PreparePurchase(
            StageNodeId nodeId, StageKey stageKey, ShopOfferId offerId)
        {
            if (HasSelection == false || CurrentStage.NodeId != nodeId.Value ||
                CurrentStage.StageKey != stageKey.Value || Shop == null)
                throw new InvalidOperationException("구매 제안이 현재 선택한 상점과 다릅니다.");
            return Shop.WithPurchase(offerId);
        }

        void ICurrentProgressBufferStore.InstallPurchase(CurrentShopData prepared) => Shop = prepared;
        void ICurrentProgressBufferStore.InstallRecovery(CurrentRecoveryData prepared) => Recovery = prepared;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICurrentProgressBufferReadSink>(this);
            sink.AddService<ICurrentProgressBufferWriteSink>(this);
            sink.AddService<ICurrentProgressBufferStore>(this);
        }
    }

    #endregion
}
