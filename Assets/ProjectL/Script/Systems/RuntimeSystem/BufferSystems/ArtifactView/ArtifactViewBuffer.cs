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

        /// <summary>
        /// 지정 번호보다 큰 첫 요청을 조회합니다.
        /// handledThrough는 Core 측에서 순서대로 처리 완료한 마지막 번호이며,
        /// 처리 중 중복 등록 방지도 Core 처리 계층이 관리합니다.
        /// </summary>
        bool TryGetNextCommand(int handledThrough, out ArtifactViewCommand command);
    }

    /// <summary>View가 요청을 기록하고 결과를 확인한 요청을 정리합니다.</summary>
    public interface IArtifactViewBufferWriteSink : IServiceModule
    {
        /// <summary>요청을 목록에 보관하고 번호를 반환합니다. Core를 직접 호출하지 않습니다.</summary>
        int Submit(ArtifactViewCommandData command);

        /// <summary>결과를 확인한 요청 하나를 삭제합니다. 이미 없으면 false입니다.</summary>
        bool RemoveCommand(int number);
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

        public IReadOnlyList<ArtifactViewCommand> Commands => _readOnlyCommands;

        public ArtifactViewBuffer()
        {
            _readOnlyCommands = _commands.AsReadOnly();
        }

        public int Submit(ArtifactViewCommandData command)
        {
            command.Validate();
            if (_lastIssuedNumber == int.MaxValue)
                throw new InvalidOperationException("ArtifactViewBuffer 요청 번호를 더 발급할 수 없습니다.");

            int number = _lastIssuedNumber + 1;
            _commands.Add(new ArtifactViewCommand(number, command));
            _lastIssuedNumber = number;
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

        public bool RemoveCommand(int number)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            for (int index = 0; index < _commands.Count; index++)
            {
                if (_commands[index].Number == number)
                {
                    _commands.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactViewBufferReadSink>(this);
            sink.AddService<IArtifactViewBufferWriteSink>(this);
        }
    }

    #endregion
}
