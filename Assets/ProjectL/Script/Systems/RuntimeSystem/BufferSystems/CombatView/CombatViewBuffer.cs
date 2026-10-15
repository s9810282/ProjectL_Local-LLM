using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView
{
    #region 서비스

    /// <summary>
    /// Core 측(Flow·Job·처리 서비스)이 읽는 전투 입력입니다. 값을 바꾸는 메서드는 두지 않습니다.
    /// 처리 여부는 Core 측이 마지막 처리 번호로 따로 기록합니다.
    /// </summary>
    public interface ICombatViewBufferReadSink : IServiceModule
    {
        /// <summary>아직 View가 지우지 않은 실행 명령입니다. 번호 오름차순입니다.</summary>
        IReadOnlyList<CombatViewCommand> Commands { get; }

        /// <summary>번호가 handledThrough보다 큰 첫 명령을 찾습니다.</summary>
        bool TryGetNextCommand(int handledThrough, out CombatViewCommand command);

        bool HasPreview { get; }

        /// <summary>현재 미리보기 요청입니다. HasPreview가 false이면 기본값입니다.</summary>
        CombatViewPreviewRequest Preview { get; }

        /// <summary>View가 연출을 끝낸 Core 결과의 마지막 순번입니다. 0은 아직 없음입니다.</summary>
        int PresentedResultSequence { get; }
    }

    /// <summary>View가 사용자 입력을 기록합니다.</summary>
    public interface ICombatViewBufferWriteSink : IServiceModule
    {
        /// <summary>명령을 쌓고 발급한 번호를 돌려줍니다.</summary>
        int Submit(CombatViewCommandData command);

        /// <summary>미리보기 요청을 교체하고 발급한 번호를 돌려줍니다.</summary>
        int SetPreview(CombatViewPreviewData preview);

        void ClearPreview();

        /// <summary>Core가 처리했다고 알린 번호까지의 명령을 지웁니다.</summary>
        void RemoveCommandsThrough(int handledNumber);

        /// <summary>연출을 끝낸 결과 순번을 기록합니다. 줄어드는 값은 거절합니다.</summary>
        void AcknowledgePresented(int resultSequence);

        /// <summary>전투 경계에서 명령과 미리보기를 비웁니다. 번호 카운터와 연출 확인 값은 유지합니다.</summary>
        void Clear();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CombatViewBuffer :
        ICombatViewBufferReadSink,
        ICombatViewBufferWriteSink,
        IExportSystemServices
    {
        private readonly List<CombatViewCommand> _commands = new();
        private readonly ReadOnlyCollection<CombatViewCommand> _readOnlyCommands;
        private int _lastIssuedNumber;
        private CombatViewPreviewRequest _preview;
        private bool _hasPreview;
        private int _presentedResultSequence;

        public IReadOnlyList<CombatViewCommand> Commands => _readOnlyCommands;
        public bool HasPreview => _hasPreview;
        public CombatViewPreviewRequest Preview => _preview;
        public int PresentedResultSequence => _presentedResultSequence;

        public CombatViewBuffer()
        {
            _readOnlyCommands = _commands.AsReadOnly();
        }

        public bool TryGetNextCommand(int handledThrough, out CombatViewCommand command)
        {
            // 목록이 번호 오름차순이므로 앞에서부터 처음 만나는 명령이 다음 명령입니다.
            foreach (var candidate in _commands)
            {
                if (candidate.Number > handledThrough)
                {
                    command = candidate;
                    return true;
                }
            }

            command = default;
            return false;
        }

        public int Submit(CombatViewCommandData command)
        {
            // 값 검사가 실패하면 번호를 소비하지 않도록 먼저 만들고 나서 번호를 확정합니다.
            int number = PeekNextNumber();
            var record = new CombatViewCommand(number, command);
            _commands.Add(record);
            _lastIssuedNumber = number;
            return number;
        }

        public int SetPreview(CombatViewPreviewData preview)
        {
            int number = PeekNextNumber();
            _preview = new CombatViewPreviewRequest(number, preview);
            _hasPreview = true;
            _lastIssuedNumber = number;
            return number;
        }

        public void ClearPreview()
        {
            _preview = default;
            _hasPreview = false;
        }

        public void RemoveCommandsThrough(int handledNumber)
        {
            if (handledNumber < 0 || handledNumber > _lastIssuedNumber)
                throw new ArgumentOutOfRangeException(nameof(handledNumber));

            int count = 0;
            while (count < _commands.Count && _commands[count].Number <= handledNumber)
                count++;

            _commands.RemoveRange(0, count);
        }

        public void AcknowledgePresented(int resultSequence)
        {
            if (resultSequence < _presentedResultSequence)
                throw new ArgumentOutOfRangeException(nameof(resultSequence),
                    "연출 확인 순번은 줄어들 수 없습니다.");

            _presentedResultSequence = resultSequence;
        }

        public void Clear()
        {
            _commands.Clear();
            ClearPreview();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatViewBufferReadSink>(this);
            sink.AddService<ICombatViewBufferWriteSink>(this);
        }

        private int PeekNextNumber()
        {
            if (_lastIssuedNumber == int.MaxValue)
                throw new InvalidOperationException("CombatViewBuffer 번호를 더 발급할 수 없습니다.");

            return _lastIssuedNumber + 1;
        }
    }

    #endregion
}
