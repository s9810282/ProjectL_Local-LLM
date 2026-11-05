#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development
{
    internal sealed class DevelopmentStateService : IDevelopmentStateCommands,
        ISystemPostBindService, ISystemShutdown, IExportSystemServices
    {
        private ICharacterRosterService _characters;
        private IDeckEditAvailabilityRead _availability;
        private IRunProgressBufferReadSink _run;
        private ICurrentProgressBufferReadSink _current;
        private ICombatStateBufferReadSink _combat;
        private ICurrencyBufferReadSink _currency;
        private IDevelopmentBufferWriteSink _transactions;
        private StageServiceManager _stageService;
        private DevelopmentNavigationActions _navigation;
        private bool _bound;
        private bool _stopped;

        internal DevelopmentStateService(ICharacterRosterService characters, IDeckEditAvailabilityRead availability,
            StageServiceManager stageService)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _availability = availability ?? throw new ArgumentNullException(nameof(availability));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (_bound || _stopped)
                throw new InvalidOperationException("개발 기능은 한 번만 바인딩하며 종료한 인스턴스는 재사용하지 않습니다.");
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            if (!(buffers is IDevelopmentBufferProvider provider) || provider.DevelopmentWriteSink == null)
                throw new InvalidOperationException("개발 기능에는 IDevelopmentBufferProvider 거래 공급자가 필요합니다.");
            _run = buffers.RunProgressReadSink;
            _current = buffers.CurrentProgressReadSink;
            _combat = buffers.CombatStateReadSink;
            _currency = buffers.CurrencyReadSink;
            _transactions = provider.DevelopmentWriteSink;
            _navigation = new DevelopmentNavigationActions(_stageService, _run, _current,
                buffers.MapProgressReadSink, buffers.StageProgressReadSink, _transactions, CheckAdmission);
            _bound = true;
        }

        public DevelopmentContext ReadContext()
        {
            if (!_bound || _stopped)
                return new DevelopmentContext(default, 0, false, default, default, DevelopmentFailureReason.Unavailable);
            var selection = _current.CurrentStage;
            var observed = new DevelopmentContext(_run.RunId, _current.CurrentMapKey, _current.HasSelection,
                _current.HasSelection ? new StageNodeId(selection.NodeId) : default,
                _current.HasSelection ? new StageKey(selection.StageKey) : default, DevelopmentFailureReason.None);
            var reason = CheckAdmission(observed);
            return new DevelopmentContext(observed.RunId, observed.ChapterKey, observed.HasSelection,
                observed.NodeId, observed.StageKey, reason);
        }

        public DevelopmentGoldResult GrantGold(DevelopmentContext expected, int amount)
        {
            var reason = CheckAdmission(expected);
            if (reason != DevelopmentFailureReason.None) return DevelopmentGoldResult.Refused(reason);
            if (amount <= 0) return DevelopmentGoldResult.Refused(DevelopmentFailureReason.InvalidAmount);
            int before = _currency.Amount;
            int after;
            try
            {
                after = checked(before + amount);
            }
            catch (OverflowException)
            {
                return DevelopmentGoldResult.Refused(DevelopmentFailureReason.Overflow);
            }

            // 마지막 외부 시점 조회 뒤에는 원본 재검증과 콜백 없는 저장소 거래만 실행한다.
            reason = CheckAdmission(expected);
            if (reason != DevelopmentFailureReason.None) return DevelopmentGoldResult.Refused(reason);
            reason = _transactions.TryGrantGold(expected, before, after);
            return reason == DevelopmentFailureReason.None
                ? DevelopmentGoldResult.Success(before, after) : DevelopmentGoldResult.Refused(reason);
        }

        public DevelopmentUnitResult GrantUnit(DevelopmentContext expected, EntityKey key)
        {
            var reason = CheckAdmission(expected);
            if (reason != DevelopmentFailureReason.None) return DevelopmentUnitResult.Refused(reason);
            // 획득 가능 여부·개체 ID·로스터 버전은 기존 획득 경로가 소유한다. 선택 덱은 바꾸지 않는다.
            return DevelopmentUnitResult.FromAcquisition(_characters.Acquire(key));
        }

        private DevelopmentFailureReason CheckAdmission(DevelopmentContext expected)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            var reason = CheckState(expected);
            if (reason != DevelopmentFailureReason.None) return reason;
            var availability = _availability.ReadDeckEditAvailability()
                ?? throw new InvalidOperationException("설정된 개발 기능 시점 읽기는 결과를 반환해야 합니다.");
            // 외부 흐름 조회가 Run·선택·수명을 바꾸었으면 조회 전 사실로 명령을 실행하지 않는다.
            reason = CheckState(expected);
            if (reason != DevelopmentFailureReason.None) return reason;
            switch (availability.Reason)
            {
                case DeckUnavailableReason.None: return DevelopmentFailureReason.None;
                case DeckUnavailableReason.Loading:
                case DeckUnavailableReason.JobPending:
                case DeckUnavailableReason.RequestPending:
                case DeckUnavailableReason.InteractionOpen:
                    return DevelopmentFailureReason.Busy;
                case DeckUnavailableReason.RunEnded: return DevelopmentFailureReason.RunEnded;
                case DeckUnavailableReason.BattleRetained: return DevelopmentFailureReason.BattleRetained;
                default: return DevelopmentFailureReason.Unavailable;
            }
        }

        public DevelopmentNavigationResult SkipNextNode(DevelopmentContext expected, StageNodeId nodeId) =>
            Navigate(expected, nodeId, false);

        public DevelopmentNavigationResult PrepareJumpToNode(DevelopmentContext expected, StageNodeId targetNodeId) =>
            Navigate(expected, targetNodeId, true);

        private DevelopmentNavigationResult Navigate(DevelopmentContext expected, StageNodeId target, bool jump)
        {
            var reason = CheckAdmission(expected);
            if (reason == DevelopmentFailureReason.None) return _navigation.Execute(expected, target, jump);
            // 거절 결과를 만들기 위해 외부 공급자를 다시 부르지 않는다.
            var selection = _current?.CurrentStage ?? default;
            bool hasSelection = _current?.HasSelection ?? false;
            var current = new DevelopmentContext(_run?.RunId ?? default, _current?.CurrentMapKey ?? 0,
                hasSelection, hasSelection ? new StageNodeId(selection.NodeId) : default,
                hasSelection ? new StageKey(selection.StageKey) : default, reason);
            return new DevelopmentNavigationResult(reason, target, default, Array.Empty<StageNodeId>(), current);
        }

        private DevelopmentFailureReason CheckState(DevelopmentContext expected)
        {
            if (!_bound || _stopped) return DevelopmentFailureReason.Unavailable;
            if (_run.RunId.IsEmpty) return DevelopmentFailureReason.NoActiveRun;
            var selection = _current.CurrentStage;
            if (_run.RunId != expected.RunId || _current.CurrentMapKey != expected.ChapterKey ||
                _current.HasSelection != expected.HasSelection ||
                (expected.HasSelection && (selection.NodeId != expected.NodeId.Value || selection.StageKey != expected.StageKey.Value)))
                return DevelopmentFailureReason.StaleContext;
            if (_run.Outcome != RunOutcome.InProgress) return DevelopmentFailureReason.RunEnded;
            if (_combat.HasBattle) return DevelopmentFailureReason.BattleRetained;
            return DevelopmentFailureReason.None;
        }

        public void Shutdown()
        {
            _stopped = true;
            _characters = null;
            _availability = null;
            _run = null;
            _current = null;
            _combat = null;
            _currency = null;
            _transactions = null;
            _navigation = null;
            _stageService = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IDevelopmentStateCommands>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
#endif
