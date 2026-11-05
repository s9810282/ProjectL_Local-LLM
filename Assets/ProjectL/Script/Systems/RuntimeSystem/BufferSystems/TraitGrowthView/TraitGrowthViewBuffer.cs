using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView
{
    public readonly struct TraitGrowthViewCommand
    {
        public int Number { get; }
        public TraitGrowthChoiceRequest Request { get; }
        public TraitGrowthViewCommand(int number, TraitGrowthChoiceRequest request)
        {
            Number = number;
            Request = request;
        }
    }
    public interface ITraitGrowthViewWriteSink : IServiceModule
    {
        int Submit(TraitGrowthChoiceRequest request);
        bool AcknowledgeResult(RunId runId, int number);
    }
    public interface ITraitGrowthViewReadSink : IServiceModule
    {
        bool TryGetCommand(int number, out TraitGrowthViewCommand command);
        int AcknowledgedResultNumber
        {
            get;
        }
    }
    internal interface ITraitGrowthViewBufferStore : IServiceModule
    {
        void Clear();
    }
    internal sealed class TraitGrowthViewBuffer : ITraitGrowthViewWriteSink, ITraitGrowthViewReadSink, ITraitGrowthViewBufferStore, IExportSystemServices
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly ITraitGrowthResultReadSink _results;
        private TraitGrowthViewCommand? _pending;
        private int _lastIssued;
        public int AcknowledgedResultNumber
        {
            get; private set;
        }
        internal TraitGrowthViewBuffer(IRunProgressBufferReadSink run, ITraitGrowthResultReadSink results)
        {
            _run = run;
            _results = results;
        }
        public int Submit(TraitGrowthChoiceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_pending.HasValue)
                throw new InvalidOperationException("처리 결과를 소비하기 전에는 다른 성장 요청을 제출할 수 없습니다.");
            int number = checked(_lastIssued + 1);
            _pending = new TraitGrowthViewCommand(number, request);
            _lastIssued = number;
            return number;
        }
        public bool TryGetCommand(int number, out TraitGrowthViewCommand command)
        {
            command = _pending.GetValueOrDefault();
            return _pending.HasValue && command.Number == number;
        }
        public bool AcknowledgeResult(RunId runId, int number)
        {
            var result = _results.LatestResult;
            if (AcknowledgedResultNumber < 0 || AcknowledgedResultNumber > _results.HandledCommandNumber)
                throw new InvalidOperationException("성장 결과 소비 번호가 처리 번호보다 큽니다.");
            if (runId.IsEmpty || runId != _run.RunId || result == null || result.RunId != runId || result.Number != number)
                return false;
            if (AcknowledgedResultNumber == number)
                return true;
            if (!TryGetCommand(number, out var command) || !command.Request.SameValues(result.Request))
                return false;
            AcknowledgedResultNumber = number;
            _pending = null;
            return true;
        }
        public void Clear()
        {
            _pending = null;
            AcknowledgedResultNumber = 0;
        }
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITraitGrowthViewWriteSink>(this);
            sink.AddService<ITraitGrowthViewReadSink>(this);
            sink.AddService<ITraitGrowthViewBufferStore>(this);
        }
    }
}
