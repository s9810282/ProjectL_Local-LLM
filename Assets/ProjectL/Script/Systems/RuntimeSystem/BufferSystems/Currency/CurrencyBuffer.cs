using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Currency
{
    #region 서비스

    /// <summary>
    /// Run 재화다. 한 Run 동안 스테이지 사이에서 유지되고 상점 구매·회복에만 쓴다.
    /// 전투 드로우용 스테이지 골드는 전투 상태(CombatStateData.StageGold)에 있으며 이 잔액과 무관하다.
    /// </summary>
    public interface ICurrencyBufferReadSink : IServiceModule
    {
        int Amount { get; }
    }

    /// <summary>원본 비교 없이 바로 쓰는 경로다. 초기화·테스트 준비에서만 쓴다.
    /// 게임 중 Run 재화 변경은 기대 잔액을 담은 준비→설치 경로(구매·회복 게시)를 쓴다.</summary>
    public interface ICurrencyBufferWriteSink : IServiceModule
    {
        void SetAmount(int amount);
        void Clear();
    }

    internal interface ICurrencyBufferStore : IServiceModule
    {
        int PrepareAmount(int expected, int final);
        void InstallAmount(int prepared);
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CurrencyBuffer :
        ICurrencyBufferReadSink, ICurrencyBufferWriteSink, ICurrencyBufferStore, IExportSystemServices
    {
        public int Amount { get; private set; }

        public void SetAmount(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Amount == amount) return;
            Amount = amount;
        }

        public void Clear() => SetAmount(0);

        int ICurrencyBufferStore.PrepareAmount(int expected, int final)
        {
            if (Amount != expected || final < 0)
                throw new InvalidOperationException("재화 변경의 예상 값 또는 최종 값이 맞지 않습니다.");
            return final;
        }

        void ICurrencyBufferStore.InstallAmount(int prepared) => SetAmount(prepared);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICurrencyBufferReadSink>(this);
            sink.AddService<ICurrencyBufferWriteSink>(this);
            sink.AddService<ICurrencyBufferStore>(this);
        }
    }

    #endregion
}
