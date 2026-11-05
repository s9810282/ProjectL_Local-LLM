using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem
{
    /// <summary>상점 요청의 실행·영수증·완료 순서와 동기 재진입 차단을 소유한다.</summary>
    internal sealed class ShopCommandExecutor
    {
        private readonly IShopService _shop;
        private IShopViewBufferReadSink _input;
        private IStageCommandResultReadSink _results;
        private IStageCommandResultStore _store;
        private bool _executing;

        internal ShopCommandExecutor(IShopService shop)
            => _shop = shop ?? throw new ArgumentNullException(nameof(shop));

        internal void Bind(IShopViewBufferReadSink input, IStageCommandResultReadSink results, IStageCommandResultStore store)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        internal bool ProcessNext(int expectedRequestNumber)
        {
            if (expectedRequestNumber < 1) throw new ArgumentOutOfRangeException(nameof(expectedRequestNumber));
            if (_input == null) throw new InvalidOperationException("상점 실행기 바인딩이 필요합니다.");
            if (_executing) throw new InvalidOperationException("상점 요청이 이미 실행 중입니다.");
            // 한 칸짜리 결과를 View가 읽고 입력을 지울 때까지 다음 요청을 기다린다.
            if (!_input.TryGetNextCommand(0, out var command) ||
                command.Number <= _results.HandledShopCommandNumber || command.Number != expectedRequestNumber)
                return false;
            _executing = true;
            try
            {
                var data = command.Data;
                switch (data.Kind)
                {
                    case ShopViewCommandKind.Purchase:
                    case ShopViewCommandKind.PurchaseTraining:
                        var isTraining = data.Kind == ShopViewCommandKind.PurchaseTraining;
                        var kind = isTraining ? ShopCommandKind.PurchaseTraining : ShopCommandKind.Purchase;
                        var purchase = isTraining
                            ? _shop.PurchaseTraining(data.Context, data.OwnedId, data.TraitKey, data.ExpectedRank)
                            : _shop.Purchase(data.Context, data.OfferId);
                        if (_results.HandledShopCommandNumber >= command.Number) return true;
                        var first = new ShopCommandResult(command.Number, data.Context,
                            kind, data.OfferId, purchase, null, data.OwnedId, data.TraitKey, data.ExpectedRank);
                        // 완료가 예외를 던져도 이미 반영한 구매의 증거를 먼저 남긴다.
                        _store.InstallShopResult(first);
                        if (!purchase.Succeeded) return true;
                        var completion = _shop.CompleteShop(data.Context);
                        if (StillOwnsReceipt(first))
                            _store.InstallShopResult(new ShopCommandResult(command.Number, data.Context,
                                kind, data.OfferId, purchase, completion, data.OwnedId, data.TraitKey, data.ExpectedRank));
                        return true;
                    case ShopViewCommandKind.Complete:
                        var result = _shop.CompleteShop(data.Context);
                        if (_results.HandledShopCommandNumber < command.Number)
                            _store.InstallShopResult(new ShopCommandResult(command.Number, data.Context,
                                ShopCommandKind.Complete, ShopOfferId.Empty, null, result));
                        return true;
                    default:
                        throw new InvalidOperationException("알 수 없는 상점 입력입니다.");
                }
            }
            finally
            {
                _executing = false;
            }
        }

        private bool StillOwnsReceipt(ShopCommandResult expected)
            => _results.TryGetShopResult(out var current) &&
               current.RequestNumber == expected.RequestNumber && current.Context == expected.Context &&
               current.Kind == expected.Kind && current.OfferId == expected.OfferId &&
               current.OwnedId == expected.OwnedId && current.TraitKey == expected.TraitKey &&
               current.ExpectedRank == expected.ExpectedRank &&
               ReferenceEquals(current.PurchaseResult, expected.PurchaseResult) && current.CompletionResult == null;
    }
}
