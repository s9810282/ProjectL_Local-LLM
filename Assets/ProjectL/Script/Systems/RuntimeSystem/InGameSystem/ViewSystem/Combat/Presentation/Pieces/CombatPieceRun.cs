using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 조립 하나의 실행입니다(조각·조립 설계 BP2·BP4). 대여 묶음(<see cref="CombatPieceRunLeases"/>)으로 대여를 한 번에 빌리고, 시점·시간 규칙으로 항목을 시작·진행하며,
    /// 사건 완료·전체 종료·취소·세대·표시 사라짐을 맡습니다. 조각 실행 재생기(<see cref="CombatPieceRunPlayer"/>)가 만들고 진행합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 시간 규칙(설계 BP2, 겹침 규칙 O4를 일반 규칙으로 바꾼 것):
    /// 1. 지연으로 시작하는 항목은 그 프레임의 남은 시간을 받습니다(옛 포효 대기 → 커짐과 같음).
    /// 2. 신호로 바로 시작하는 항목(지연 0)은 그 프레임 시간을 받지 않고 0으로 시작합니다. 신호가 어디서 왔든 같습니다.
    /// 3. 실행을 시작할 때 시작한 항목은 다음 조각 Tick부터 시간을 받습니다.
    /// 신호로 상태가 바뀐 항목(시작·지연 시작)은 그 프레임의 남은 Tick에서 다시 진행하지 않습니다. 그 프레임 시간은 신호 순간의 남은 시간으로만 받습니다.
    /// </para>
    /// <para>
    /// 지연은 경과 시간을 더해 세고(옛 능력 동작과 같은 계산 순서), 남은 시간은 경과에서 지연을 뺀 값입니다.
    /// 신호 순간의 남은 시간은 조각이 시작 안에서 끝났거나 시작 안에서 신호를 냈을 때만 0이 아닙니다(그 시작이 받은 남은 시간). 조각이 자기 Tick 안에서 끝나면 그 프레임 시간을 다 쓴 것으로 보고
    /// 남은 시간을 버립니다(옛 포효 커짐 끝 → 박치기와 같음). 그래서 그때의 신호 뒤 지연은 다음 조각 Tick부터 셉니다.
    /// 외부 진행 조각의 끝은 콜백으로 바로 받습니다(설계 BP1). 그 항목의 시작 안에서 온 끝은 시작의 남은 시간을 이어 받고, 그 밖에서 온 끝의 남은 시간은 0입니다.
    /// </para>
    /// <para>
    /// 끝 말고 다른 신호(설계 BP2 보충, 카메라 연출 조각 설계 K4)도 같은 시간 규칙입니다. 맥락의 신호 입구로 오고, 시작 안에서 온 신호는 낸 순서대로
    /// 시작이 끝난 뒤 시작의 남은 시간으로 처리합니다. 끝이 처리되면 그 뒤에 낸 신호는 버립니다(끝난 항목의 신호와 같음). Tick 안이나 실행 밖 콜백에서 온 신호의 남은 시간은 0입니다.
    /// 항목이 끝나면 그 항목은 더 이상 신호를 낼 수 없습니다. 그래서 그 항목의 끝 말고 다른 신호를 기다리던, 아직 시작하지 않은 항목은
    /// 그 순간 시작하지 않고 끝난 것으로 봅니다. 그 항목을 기다리던 뒤 사슬도 같고, 기다림 항목이면 완료 셈에 끝난 것으로 넣습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatPieceRun
    {
        private enum RunState
        {
            NotStarted,
            Running,
            Finished,
            Cancelled,
        }

        private enum ItemState
        {
            Pending,
            Delaying,
            Running,
            Ended,
        }

        private const int NotTriggered = int.MinValue;

        private readonly CombatComposition _composition;
        private readonly IReadOnlyList<CombatComposition.Item> _items;
        private readonly ItemState[] _states;
        private readonly float[] _elapsed;
        private readonly int[] _triggeredFrame;
        // 항목마다 처리한 신호(끝 말고)의 비트입니다. 같은 신호를 두 번 내면 무시하고, 미도착 판정에서 이미 낸 신호를 가립니다.
        private readonly int[] _signalled;
        // 항목의 시작 안에서 온 신호(끝 포함)입니다. 시작이 끝난 뒤 낸 순서대로 처리합니다.
        private readonly List<(int Item, CombatPieceSignal Signal)> _beginSignals = new();
        private readonly ICombatSourceLifetime _source;
        private readonly Func<int> _generation;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly Func<int> _frame;
        private readonly Action _completed;
        private readonly Action<CombatPieceRun> _ended;
        private readonly Action<CombatPieceRun> _unavailable;
        private int _beginning = -1;
        private int _remainingItems;
        private int _remainingWaits;
        private bool _completionNotified;
        private RunState _state;

        /// <param name="window">대여를 빌리는 유닛 표시 창구입니다.</param>
        /// <param name="camera">장면 채널(카메라 줌)을 빌리는 카메라 창구입니다(카메라 연출 조각 설계 K3).</param>
        /// <param name="source">출처 완료 조립이 쓰는 출처 수명 창구입니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다.</param>
        /// <param name="frame">조각 실행 재생기의 현재 프레임 번호입니다(시간 규칙 2의 "이 프레임에 신호로 바뀜" 판정).</param>
        /// <param name="completed">사건 완료 통지입니다. 세대가 같을 때만 한 번 전합니다.</param>
        /// <param name="ended">실행이 끝나거나 취소돼 재생기에서 빠질 때 부릅니다.</param>
        /// <param name="unavailable">대여가 밖에서 취소돼(표시 꺼짐·재대여·맵 카메라 빠짐) 실행이 사라졌을 때 부릅니다.</param>
        internal CombatPieceRun(CombatComposition composition, CombatUnitPresentationWindow window, CombatCameraWindow camera,
            ICombatSourceLifetime source, Func<int> generation, Func<FieldGridMapper> mapper, Func<int> frame, Action completed, int version,
            Action<CombatPieceRun> ended, Action<CombatPieceRun> unavailable)
        {
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            // 대여는 묶음이 맡습니다(카메라 원칙 평가 C6). 인자 검사 순서는 그대로 둡니다.
            Leases = new CombatPieceRunLeases(window ?? throw new ArgumentNullException(nameof(window)),
                camera ?? throw new ArgumentNullException(nameof(camera)));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _ended = ended ?? throw new ArgumentNullException(nameof(ended));
            _unavailable = unavailable ?? throw new ArgumentNullException(nameof(unavailable));
            _completed = completed;
            Version = version;
            _items = composition.Items;
            int count = _items.Count;
            _states = new ItemState[count];
            _elapsed = new float[count];
            _triggeredFrame = new int[count];
            _signalled = new int[count];
            for (int i = 0; i < count; i++)
            {
                _triggeredFrame[i] = NotTriggered;
                if (_items[i].Waits) _remainingWaits++;
            }
            _remainingItems = count;
        }

        /// <summary>실행을 시작한 재생 세대입니다.</summary>
        internal int Version { get; }

        internal CombatComposition Composition => _composition;

        /// <summary>진행 중인지입니다. 끝나거나 취소된 실행은 다시 시작하지 않습니다.</summary>
        internal bool IsActive => _state == RunState.Running;

        internal FieldGridMapper Mapper => _mapper();

        /// <summary>이 실행이 빌린 대여 묶음입니다. 조각 맥락이 채널을 여기서 꺼냅니다.</summary>
        internal CombatPieceRunLeases Leases { get; }

        /// <summary>
        /// 대여를 한 번에 빌리고 실행을 시작합니다. 하나라도 못 빌리면 아무것도 시작하지 않고 false입니다(틀이 지금처럼 머묾으로 갑니다).
        /// 출처 완료면 항목보다 먼저 출처 수명을 엽니다. 시작 안에서 끝날 수 있습니다(길이 0 사슬).
        /// </summary>
        internal bool TryStart()
        {
            if (_state != RunState.NotStarted)
                throw new InvalidOperationException("[CombatPieceRun] 실행은 한 번만 시작합니다.");
            _composition.MarkUsed();
            if (!Leases.TryBorrow(_composition, OnLeaseCancelled))
            {
                _state = RunState.Cancelled;
                return false;
            }

            _state = RunState.Running;
            try
            {
                CombatPieceCompletion completion = _composition.Completion;
                if (completion.IsSource)
                    _source.BeginSource(completion.Source, completion.Actor, completion.Policy, _completed, Version);
                // 같은 순간에 시작하는 항목은 목록 순서대로 동기로 시작합니다(설계 BP2).
                for (int i = 0; i < _items.Count && _state == RunState.Running; i++)
                {
                    CombatPieceStart start = _items[i].Start;
                    if (start.Kind != CombatPieceStartKind.AtStart || _states[i] != ItemState.Pending)
                        continue;
                    if (start.Delay == 0f)
                    {
                        StartItem(i, 0f, triggered: false);
                        continue;
                    }
                    _states[i] = ItemState.Delaying;
                    _elapsed[i] = 0f;
                }
                if (_state == RunState.Running)
                    CompleteIfDone(!completion.IsSource && _remainingWaits == 0);
            }
            catch (Exception error)
            {
                // 시작 실패는 이 실행의 조각을 멈추고 그 출처만 비운 뒤 대여를 되돌립니다(옛 포효 처리기의 실패 정리와 같은 순서).
                List<Exception> cleanup = null;
                if (_state == RunState.Running)
                    Stop(ref cleanup, abortSource: true);
                if (cleanup != null)
                {
                    cleanup.Insert(0, error);
                    throw new AggregateException(cleanup);
                }
                throw;
            }
            return true;
        }

        /// <summary>한 프레임 진행합니다. 음수 시간은 0으로 봅니다. 이 프레임에 신호로 바뀐 항목은 건너뜁니다(시간 규칙 2).</summary>
        internal void Tick(float deltaTime)
        {
            if (_state != RunState.Running)
                return;
            float delta = Math.Max(0f, deltaTime);
            int frame = _frame();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_state != RunState.Running)
                    return;
                if (_triggeredFrame[i] == frame)
                    continue;
                switch (_states[i])
                {
                    case ItemState.Delaying:
                        _elapsed[i] += delta;
                        float delay = _items[i].Start.Delay;
                        if (_elapsed[i] < delay)
                            break;
                        // 시간 규칙 1: 지연으로 시작하는 항목은 그 프레임의 남은 시간을 받습니다.
                        StartItem(i, _elapsed[i] - delay, triggered: true);
                        break;
                    case ItemState.Running:
                        ICombatPresentationPiece piece = _items[i].Piece;
                        piece.Tick(delta);
                        if (_state == RunState.Running && _states[i] == ItemState.Running && piece.IsEnded)
                            EndItem(i, 0f);
                        break;
                }
            }
        }

        /// <summary>진행을 버립니다. 조각을 멈추고 대여를 되돌리고 출처를 비웁니다. 완료를 알리지 않습니다. 재생 세대는 호출자가 먼저 올립니다.</summary>
        internal void Cancel()
        {
            if (_state != RunState.Running)
                return;
            List<Exception> errors = null;
            Stop(ref errors, abortSource: true);
            CleanupErrors.ThrowIfAny(errors, CombatSceneEventPresenter.CleanupFailureMessage);
        }

        /// <summary>외부 진행 조각의 끝입니다(<see cref="CombatPieceContext.EndCallback"/>). 지난 세대·끝난 실행·끝난 항목의 늦은 콜백은 무시합니다.</summary>
        internal void EndExternal(int item, int version)
        {
            if (version != Version || version != _generation() || _state != RunState.Running ||
                item < 0 || item >= _items.Count || _states[item] != ItemState.Running)
                return;
            // 그 항목의 시작 안에서 온 끝은 시작이 끝난 뒤 시작의 남은 시간으로 이어 갑니다.
            if (_beginning == item)
            {
                if (!_beginSignals.Contains((item, CombatPieceSignal.End)))
                    _beginSignals.Add((item, CombatPieceSignal.End));
                return;
            }
            EndItem(item, 0f);
        }

        /// <summary>
        /// 끝 말고 다른 신호입니다(<see cref="CombatPieceContext.SignalCallback"/>, 설계 BP2 보충). 진행 중인 항목만 낼 수 있고,
        /// 지난 세대·끝난 실행·끝난 항목·이미 낸 신호는 무시합니다. 그 항목의 시작 안에서 온 신호는 시작이 끝난 뒤 낸 순서대로 처리합니다.
        /// </summary>
        internal void Signal(int item, CombatPieceSignal signal, int version)
        {
            if (signal == CombatPieceSignal.End)
            {
                EndExternal(item, version);
                return;
            }
            if (version != Version || version != _generation() || _state != RunState.Running ||
                item < 0 || item >= _items.Count || _states[item] != ItemState.Running)
                return;
            if ((_signalled[item] & (1 << (int)signal)) != 0)
                return;
            if (_beginning == item)
            {
                // 낸 표시는 처리할 때 켭니다. 시작 안에서 끝이 먼저 처리되면 뒤의 신호는 버려지고, 그 신호를 기다리던 항목은 건너뜁니다.
                if (!_beginSignals.Contains((item, signal)))
                    _beginSignals.Add((item, signal));
                return;
            }
            // 시간 규칙 2: Tick 안이나 실행 밖 콜백에서 온 신호의 남은 시간은 0입니다.
            Emit(item, signal, 0f);
        }

        /// <summary>신호를 낸 것으로 표시하고 그 신호를 기다리는 뒤 항목을 시작합니다. 표시는 처리 도중 이 항목이 끝나도 남은 항목을 건너뛰지 않게 합니다.</summary>
        private void Emit(int item, CombatPieceSignal signal, float available)
        {
            _signalled[item] |= 1 << (int)signal;
            Trigger(item, signal, available);
        }

        /// <summary>
        /// 항목을 시작합니다. 시작 안에서 온 신호를 낸 순서대로 남은 시간을 이어 처리하고, 시작 안에서 끝나면 끝 신호를 냅니다.
        /// 끝나지 않았고 남은 시간이 있으면 그만큼 진행합니다.
        /// </summary>
        private void StartItem(int index, float available, bool triggered)
        {
            _states[index] = ItemState.Running;
            _triggeredFrame[index] = triggered ? _frame() : NotTriggered;
            ICombatPresentationPiece piece = _items[index].Piece;
            int previous = _beginning;
            _beginning = index;
            try
            {
                piece.Begin(new CombatPieceContext(this, index));
            }
            finally
            {
                _beginning = previous;
            }
            List<(int Item, CombatPieceSignal Signal)> signals = TakeBeginSignals(index);
            if (_state != RunState.Running || _states[index] != ItemState.Running)
                return;
            if (signals != null)
            {
                for (int i = 0; i < signals.Count; i++)
                {
                    if (_state != RunState.Running || _states[index] != ItemState.Running)
                        return;
                    // 끝이 처리되면 그 뒤에 낸 신호는 버립니다(끝난 항목의 신호와 같음). 그 신호를 기다리던 항목은 끝 처리에서 건너뜁니다.
                    if (signals[i].Signal == CombatPieceSignal.End)
                    {
                        EndItem(index, available);
                        return;
                    }
                    Emit(index, signals[i].Signal, available);
                }
                if (_state != RunState.Running || _states[index] != ItemState.Running)
                    return;
            }
            if (piece.IsEnded)
            {
                EndItem(index, available);
                return;
            }
            if (available <= 0f)
                return;
            piece.Tick(available);
            if (_state == RunState.Running && _states[index] == ItemState.Running && piece.IsEnded)
                EndItem(index, 0f);
        }

        /// <summary>
        /// 그 항목의 시작 안에서 온 신호를 낸 순서대로 꺼냅니다. 없으면 null입니다. 신호 처리 중 다른 항목이 시작해 목록을 바꿔도
        /// 섞이지 않게 먼저 꺼내 둡니다(시작 안 신호가 있을 때만 할당, 사건당 몇 번).
        /// </summary>
        private List<(int Item, CombatPieceSignal Signal)> TakeBeginSignals(int index)
        {
            List<(int Item, CombatPieceSignal Signal)> taken = null;
            for (int i = 0; i < _beginSignals.Count; i++)
            {
                if (_beginSignals[i].Item != index)
                    continue;
                (taken ??= new List<(int Item, CombatPieceSignal Signal)>()).Add(_beginSignals[i]);
                _beginSignals.RemoveAt(i);
                i--;
            }
            return taken;
        }

        /// <summary>
        /// 항목의 끝 신호를 냅니다. 그 신호를 기다리는 뒤 항목을 목록 순서대로 시작하고, 더 이상 오지 않을 신호를 기다리던 항목을 건너뛴 뒤
        /// 완료·전체 종료를 확인합니다.
        /// </summary>
        private void EndItem(int index, float available)
        {
            bool waitsBefore = _remainingWaits > 0;
            _states[index] = ItemState.Ended;
            _remainingItems--;
            if (_items[index].Waits)
                _remainingWaits--;
            Trigger(index, CombatPieceSignal.End, available);
            if (_state != RunState.Running)
                return;
            SkipUnsignalled(index);
            if (_state != RunState.Running)
                return;
            // 기다림 항목이 이 끝(또는 건너뜀)으로 모두 끝났으면 한 번 알립니다. 이미 알렸으면 다시 알리지 않습니다.
            CompleteIfDone(waitsBefore && !_composition.Completion.IsSource && _remainingWaits == 0);
        }

        /// <summary>
        /// 항목 <paramref name="index"/>의 <paramref name="signal"/>을 기다리는 뒤 항목을 목록 순서대로 시작하거나 지연을 시작합니다.
        /// 신호 뒤 지연은 신호 순간부터 세고, 신호 순간의 남은 시간(<paramref name="available"/>, 시작 안에서 온 신호일 때만 0이 아님)이 먼저 들어갑니다.
        /// </summary>
        private void Trigger(int index, CombatPieceSignal signal, float available)
        {
            for (int j = index + 1; j < _items.Count; j++)
            {
                if (_state != RunState.Running)
                    return;
                CombatPieceStart start = _items[j].Start;
                if (start.Kind != CombatPieceStartKind.After || start.Item != index || start.Signal != signal ||
                    _states[j] != ItemState.Pending)
                    continue;
                // 시간 규칙 2: 신호로 바로 시작하는 항목은 그 프레임 시간을 받지 않습니다.
                if (start.Delay == 0f)
                {
                    StartItem(j, 0f, triggered: true);
                    continue;
                }
                _elapsed[j] = available;
                if (_elapsed[j] < start.Delay)
                {
                    _states[j] = ItemState.Delaying;
                    _triggeredFrame[j] = _frame();
                    continue;
                }
                StartItem(j, _elapsed[j] - start.Delay, triggered: true);
            }
        }

        /// <summary>
        /// 신호 미도착 규칙입니다(설계 BP2 보충). 끝난 항목은 더 신호를 낼 수 없으므로, 그 항목의 끝 말고 다른 신호를 기다리던
        /// 아직 시작하지 않은 항목은 시작하지 않고 끝난 것으로 봅니다(예: 정면 공격을 시작하지 못해 타격이 없음).
        /// </summary>
        private void SkipUnsignalled(int index)
        {
            for (int j = index + 1; j < _items.Count; j++)
            {
                CombatPieceStart start = _items[j].Start;
                // 이미 낸 신호는 건너뛰지 않습니다. 그 신호의 처리 도중(뒤 항목 시작 안에서) 이 항목이 끝난 경우에도 남은 항목이 시작됩니다.
                if (start.Kind == CombatPieceStartKind.After && start.Item == index && start.Signal != CombatPieceSignal.End &&
                    _states[j] == ItemState.Pending && (_signalled[index] & (1 << (int)start.Signal)) == 0)
                    SkipItem(j);
            }
        }

        /// <summary>항목을 시작하지 않고 끝난 것으로 봅니다. 그 항목은 어떤 신호도 내지 않으므로, 그 항목을 기다리던 뒤 사슬도 같습니다.</summary>
        private void SkipItem(int index)
        {
            _states[index] = ItemState.Ended;
            _remainingItems--;
            if (_items[index].Waits)
                _remainingWaits--;
            for (int k = index + 1; k < _items.Count; k++)
            {
                CombatPieceStart start = _items[k].Start;
                if (start.Kind == CombatPieceStartKind.After && start.Item == index && _states[k] == ItemState.Pending)
                    SkipItem(k);
            }
        }

        /// <summary>
        /// 일반 완료와 전체 종료를 확인합니다. 모든 항목이 끝났으면 전체 종료(대여 되돌림·재생기에서 빠짐)를 먼저 하고 완료를 알립니다.
        /// 그래서 완료 콜백 안에서는 이 실행이 재생기에 없고, 최종 사실 표시·다음 연출·다음 결과를 바로 시작할 수 있습니다(옛 반응·능력 경로의 "정리 뒤 통지"와 같음).
        /// 콜백이 예외를 던져도 실행이 남지 않습니다. 항목이 남았으면(기다리지 않는 항목이 진행 중) 완료만 알립니다.
        /// 일반 완료 실행이 전체 종료하면 기다림 항목도 모두 끝난 것이라, 아직 알리지 않았으면 여기서 알립니다. 실행이 중첩 경로 안에서
        /// 끝나 바깥의 알림 확인을 지나치는 경우(시작 안에서 모두 끝남, 기다림 항목 뒤 길이 0 항목이 마지막)에도 완료가 사라지지 않습니다.
        /// </summary>
        private void CompleteIfDone(bool completionDue)
        {
            if (_remainingItems == 0)
            {
                Finish();
                if (completionDue || !_composition.Completion.IsSource)
                    NotifyCompleted();
                return;
            }
            if (completionDue)
                NotifyCompleted();
        }

        private void NotifyCompleted()
        {
            if (_completionNotified)
                return;
            _completionNotified = true;
            if (Version == _generation())
                _completed?.Invoke();
        }

        /// <summary>모든 항목이 끝났습니다. 대여를 되돌리고 재생기에서 빠진 뒤, 출처 실행이면 출처 전체 종료를 알립니다.</summary>
        private void Finish()
        {
            _state = RunState.Finished;
            List<Exception> errors = null;
            Leases.Restore(ref errors);
            _ended(this);
            CleanupErrors.ThrowIfAny(errors, CombatSceneEventPresenter.CleanupFailureMessage);
            if (_composition.Completion.IsSource)
                _source.EndSource(_composition.Completion.Source, Version);
        }

        /// <summary>
        /// 대여가 밖에서 취소됐습니다(표시 꺼짐·재대여). 실행을 멈추고 남은 대여를 되돌린 뒤 재생기에 사라짐을 알립니다.
        /// 출처는 비우지 않습니다. 다음 Tick 맨 앞의 오류가 정지를 알립니다(옛 능력 재생 규칙과 같음).
        /// </summary>
        private void OnLeaseCancelled()
        {
            if (_state != RunState.Running)
                return;
            List<Exception> errors = null;
            Stop(ref errors, abortSource: false);
            _unavailable(this);
            CleanupErrors.ThrowIfAny(errors, CombatSceneEventPresenter.CleanupFailureMessage);
        }

        private void Stop(ref List<Exception> errors, bool abortSource)
        {
            _state = RunState.Cancelled;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_states[i] == ItemState.Running)
                    CleanupErrors.Try(_items[i].Piece.Cancel, ref errors);
            }
            // 출처를 먼저 비우고 대여를 되돌립니다. 옛 포효 처리기의 시작 실패 정리(Gate 비우기 → 크기 복원)와
            // 취소(공격 재생기의 Gate 비우기 → 능력 동작의 크기 복원) 순서와 같습니다.
            if (abortSource && _composition.Completion.IsSource)
            {
                var source = _composition.Completion.Source;
                CleanupErrors.Try(() => _source.AbortSource(source), ref errors);
            }
            Leases.Restore(ref errors);
            _ended(this);
        }
    }
}
