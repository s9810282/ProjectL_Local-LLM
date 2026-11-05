using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView
{
    #region 서비스

    /// <summary>Core 처리 계층에 제공하는 읽기 계약입니다. 조회는 요청을 변경하지 않습니다.</summary>
    public interface IArtifactViewBufferReadSink : IServiceModule
    {
        IReadOnlyList<ArtifactViewCommand> Commands { get; }
        bool HasPreview { get; }
        ArtifactViewPreviewRequest Preview { get; }

        /// <summary>
        /// 지정 번호보다 큰 첫 요청을 조회합니다.
        /// handledThrough는 Core 측에서 순서대로 처리 완료한 마지막 번호입니다.
        /// 같은 요청을 두 번 처리하지 않게 막는 것은 로비 실행 계층(LobbyLifeCycle.OnTick이 Runner가 빌 때만 다음 Job을 거는 조건)입니다(N-79).
        /// </summary>
        bool TryGetNextCommand(int handledThrough, out ArtifactViewCommand command);
    }

    /// <summary>View가 요청을 기록하고 결과를 확인한 요청을 정리합니다.</summary>
    public interface IArtifactViewBufferWriteSink : IServiceModule
    {
        /// <summary>요청을 목록에 보관하고 번호를 반환합니다. Core를 직접 호출하지 않습니다.</summary>
        int Submit(ArtifactViewCommandData command);

        int SetPreview(ArtifactViewPreviewData preview);
        void ClearPreview();
        void RemoveCommandsThrough(int handledNumber);
        /// <summary>입력을 비워도 발급 번호는 재사용하지 않습니다.</summary>
        void Clear();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ArtifactViewBuffer :
        IArtifactViewBufferReadSink,
        IArtifactViewBufferWriteSink,
        IExportSystemServices
    {
        private readonly List<ArtifactViewCommand> _commands = new();
        private readonly ReadOnlyCollection<ArtifactViewCommand> _readOnlyCommands;
        private int _lastIssuedNumber;
        private ArtifactViewPreviewRequest _preview;
        private bool _hasPreview;

        public IReadOnlyList<ArtifactViewCommand> Commands => _readOnlyCommands;
        public bool HasPreview => _hasPreview;
        public ArtifactViewPreviewRequest Preview => _preview;

        public ArtifactViewBuffer()
        {
            _readOnlyCommands = _commands.AsReadOnly();
        }

        public int Submit(ArtifactViewCommandData command)
        {
            command.Validate();
            int number = IssueNumber();
            _commands.Add(new ArtifactViewCommand(number, command));
            return number;
        }

        public bool TryGetNextCommand(int handledThrough, out ArtifactViewCommand command)
        {
            if (handledThrough < 0)
                throw new ArgumentOutOfRangeException(nameof(handledThrough));

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

        public int SetPreview(ArtifactViewPreviewData preview)
        {
            preview.Validate();
            int number = IssueNumber();
            _preview = new ArtifactViewPreviewRequest(number, preview);
            _hasPreview = true;
            return number;
        }

        /// <summary>편집 입력과 프리뷰가 함께 쓰는 요청 번호 공간에서 다음 번호를 발급합니다(로비 리팩토링 2차 설계 N-47).</summary>
        private int IssueNumber()
        {
            if (_lastIssuedNumber == int.MaxValue)
                throw new InvalidOperationException("[ArtifactViewBuffer] 요청 번호를 더 발급할 수 없습니다.");
            _lastIssuedNumber++;
            return _lastIssuedNumber;
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
            while (count < _commands.Count && _commands[count].Number <= handledNumber) count++;
            _commands.RemoveRange(0, count);
        }

        public void Clear()
        {
            _commands.Clear();
            ClearPreview();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactViewBufferReadSink>(this);
            sink.AddService<IArtifactViewBufferWriteSink>(this);
        }
    }

    #endregion
}
