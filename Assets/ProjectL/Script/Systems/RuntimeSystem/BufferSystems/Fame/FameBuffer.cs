using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Fame
{
    /// <summary>Run 종료와 초기화를 넘어 유지되는 실행 중 명성 잔액이다. 디스크 저장은 다루지 않는다.</summary>
    public interface IFameBufferReadSink : IServiceModule
    {
        int Amount { get; }

        /// <summary>
        /// 이번 Run에서 얻은 명성 합계다. 잔액이 늘어난 설치(전투 클리어 보상 등)의 차이를 더한다.
        /// 잔액과 달리 Run 초기화(새 Run 포함) 때 0으로 돌아간다(Run 요약 설계 §10.6).
        /// </summary>
        int RunEarned { get; }
    }

    internal interface IFameBufferStore : IServiceModule
    {
        int PrepareAmount(int expected, int final);
        void InstallAmount(int prepared);

        /// <summary>Run 초기화 때 이번 Run 획득 합계만 0으로 돌린다. 잔액은 바꾸지 않는다.</summary>
        void ResetRunEarned();
    }

    internal sealed class FameBuffer : IFameBufferReadSink, IFameBufferStore, IExportSystemServices
    {
        public int Amount { get; private set; }
        public int RunEarned { get; private set; }

        public int PrepareAmount(int expected, int final)
        {
            if (expected != Amount || final < 0)
                throw new InvalidOperationException("명성 변경의 기대값 또는 최종값이 맞지 않습니다.");
            if (final > expected)
                _ = checked(RunEarned + (final - expected));
            return final;
        }

        public void InstallAmount(int prepared)
        {
            // 준비에서 넘침을 확인했다. 준비와 설치 사이에 값이 바뀌어도 음수로 감기지 않게 다시 막는다.
            if (prepared > Amount)
                RunEarned = checked(RunEarned + (prepared - Amount));
            Amount = prepared;
        }

        public void ResetRunEarned() => RunEarned = 0;

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IFameBufferReadSink>(this);
            sink.AddService<IFameBufferStore>(this);
        }
    }
}
