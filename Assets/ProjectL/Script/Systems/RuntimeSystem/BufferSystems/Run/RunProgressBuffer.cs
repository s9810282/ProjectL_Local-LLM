using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    public interface IRunProgressBufferReadSink : IServiceModule
    {
        RunId RunId { get; }
        RunOutcome Outcome { get; }
        RunCompletionReceipt Completion { get; }
        /// <summary>
        /// 이번 Run의 스테이지 첫 손패 수(메인 포함). 새 Run 시작 때 시작 로드아웃에서 정해진다.
        /// 새 Run 시작을 거치지 않은 로스터(개발·테스트)면 null이며, 이때 Core는 기본 규칙을 쓴다.
        /// </summary>
        int? StartingHandSize { get; }

        /// <summary>이번 Run에서 확정한 처치 수다. Core의 확정 처치(처치 골드·경험치와 같은 목록)를 센다. Run 초기화 때 0이 된다.</summary>
        int EnemiesDefeated { get; }

        /// <summary>
        /// 가장 최근에 끝난 Run의 요약이다. Run 종료 영수증을 설치할 때 만들고, Run 초기화(새 Run 포함)는 지우지 않는다.
        /// 다음 Run이 끝나면 교체된다. 아직 끝난 Run이 없으면 null이다. 디스크에 저장하지 않는다.
        /// </summary>
        RunSummary LastSummary { get; }
    }

    internal interface IRunProgressBufferStore : IServiceModule
    {
        RunId RunId { get; }
        RunCompletionReceipt PrepareCompletion(RunCompletionReceipt receipt);

        /// <summary>종료 영수증을 설치하고 이 순간의 값으로 <see cref="IRunProgressBufferReadSink.LastSummary"/>를 만든다.</summary>
        /// <param name="goldSpent">설치 순간의 이번 Run 쓴 골드 합계(<c>ICurrencyBufferReadSink.RunSpent</c>)다.</param>
        /// <param name="fameEarned">설치 순간의 이번 Run 획득 명성 합계(<c>IFameBufferReadSink.RunEarned</c>)다.</param>
        void InstallCompletion(RunCompletionReceipt prepared, int goldSpent, int fameEarned);

        /// <summary>처치 수에 더한 합계를 준비한다. 넘치면 예외이며 저장소는 바뀌지 않는다.</summary>
        int PrepareEnemiesDefeated(int added);
        void InstallEnemiesDefeated(int prepared);
        void InstallStartingHandSize(int startingHandSize);
        void InstallRunId(RunId runId);
        void Reset();
    }

    /// <summary>
    /// 종료 기록이 없으면 진행 중이며 명시적 초기화 전에는 다시 열지 않는다. Run 단위 설정(첫 손패 수)과 처치 수도 함께 보관한다.
    /// 마지막 Run 요약만 Run 초기화 범위 밖이다.
    /// </summary>
    internal sealed class RunProgressBuffer : IRunProgressBufferReadSink, IRunProgressBufferStore,
        IExportSystemServices
    {
        public RunId RunId { get; private set; }
        void IRunProgressBufferStore.InstallRunId(RunId runId)
        {
            if (runId.IsEmpty) throw new ArgumentException("Run ID가 필요합니다.", nameof(runId));
            RunId = runId;
        }
        public RunOutcome Outcome => Completion?.Outcome ?? RunOutcome.InProgress;
        public RunCompletionReceipt Completion { get; private set; }
        public int? StartingHandSize { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public RunSummary LastSummary { get; private set; }

        RunCompletionReceipt IRunProgressBufferStore.PrepareCompletion(RunCompletionReceipt receipt)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            if (Completion != null) throw new InvalidOperationException("RunEnded: 이미 종료된 Run입니다.");
            return receipt;
        }

        void IRunProgressBufferStore.InstallCompletion(RunCompletionReceipt prepared, int goldSpent, int fameEarned)
        {
            // 설치 단계라 실패하지 않아야 한다. 쓴 골드·획득 명성·처치 수는 음수가 될 수 없어 요약 생성자 검사는 도달하지 않는다.
            var summary = new RunSummary(RunId, prepared, goldSpent, fameEarned, EnemiesDefeated);
            Completion = prepared;
            LastSummary = summary;
        }

        int IRunProgressBufferStore.PrepareEnemiesDefeated(int added)
        {
            if (added < 0) throw new ArgumentOutOfRangeException(nameof(added));
            return checked(EnemiesDefeated + added);
        }

        void IRunProgressBufferStore.InstallEnemiesDefeated(int prepared) => EnemiesDefeated = prepared;

        void IRunProgressBufferStore.InstallStartingHandSize(int startingHandSize)
        {
            if (startingHandSize < 1) throw new ArgumentOutOfRangeException(nameof(startingHandSize));
            StartingHandSize = startingHandSize;
        }

        void IRunProgressBufferStore.Reset()
        {
            RunId = RunId.Empty;
            Completion = null;
            StartingHandSize = null;
            EnemiesDefeated = 0;
            // LastSummary는 끝난 Run의 기록이라 Run 초기화가 지우지 않는다(설계 S4).
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunProgressBufferReadSink>(this);
            sink.AddService<IRunProgressBufferStore>(this);
        }
    }
}
