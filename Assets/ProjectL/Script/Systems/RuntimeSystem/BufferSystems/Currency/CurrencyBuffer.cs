using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Currency
{
    #region 서비스

    /// <summary>
    /// Run 골드의 유일한 잔액이다. 스테이지와 챕터 사이에서 유지되며 새 Run 초기화 때 교체한다.
    /// 기존 상점 구매·회복은 이 잔액을 쓰며 전투 준비·종료는 잔액을 변경하지 않는다.
    /// </summary>
    public interface ICurrencyBufferReadSink : IServiceModule
    {
        int Amount { get; }

        /// <summary>
        /// 이번 Run에서 쓴 골드 합계다. 게임 중 잔액이 줄어든 설치(구매·회복 등)의 차이를 더한다.
        /// 늘어난 설치(보상)는 더하지 않으며, 잔액을 바로 교체하면(<see cref="ICurrencyBufferWriteSink"/>) 0으로 돌아간다.
        /// </summary>
        int RunSpent { get; }
    }

    /// <summary>원본 비교 없이 바로 쓰는 경로다. 초기화·테스트 준비에서만 쓴다. 잔액을 교체하면 쓴 골드 합계도 0이 된다.
    /// 게임 중 Run 재화 변경은 기대 잔액을 담은 준비→설치 경로(구매·회복·전투 보상 게시)를 쓴다.</summary>
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
        public int RunSpent { get; private set; }

        public void SetAmount(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            // 잔액 교체는 새 Run 시작·초기화 경로다. 쓴 골드도 새 기준에서 다시 센다.
            RunSpent = 0;
            Amount = amount;
        }

        public void Clear() => SetAmount(0);

        int ICurrencyBufferStore.PrepareAmount(int expected, int final)
        {
            if (Amount != expected || final < 0)
                throw new InvalidOperationException("재화 변경의 예상 값 또는 최종 값이 맞지 않습니다.");
            if (final < expected)
                _ = checked(RunSpent + (expected - final));
            return final;
        }

        void ICurrencyBufferStore.InstallAmount(int prepared)
        {
            // 준비에서 넘침을 확인했다. 준비와 설치가 나뉜 거래(회복)에서 사이에 값이 바뀌어도 음수로 감기지 않게 다시 막는다.
            if (prepared < Amount)
                RunSpent = checked(RunSpent + (Amount - prepared));
            Amount = prepared;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICurrencyBufferReadSink>(this);
            sink.AddService<ICurrencyBufferWriteSink>(this);
            sink.AddService<ICurrencyBufferStore>(this);
        }
    }

    #endregion
}
