using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView
{
    /// <summary>회복 입력과 View가 소비 확인한 번호를 읽는다.</summary>
    public interface IRecoveryViewBufferReadSink : IServiceModule
    {
        int AcknowledgedResultNumber { get; }

        /// <summary>양수 번호와 정확히 일치하는 입력만 읽으며 소비하지 않는다.</summary>
        bool TryGetCommand(int expectedNumber, out RecoveryViewCommand command);
    }

    /// <summary>회복 입력 제출·취소와 결과 소비 확인을 담당한다.</summary>
    public interface IRecoveryViewBufferWriteSink : IServiceModule
    {
        /// <summary>한 건을 제출하고 번호를 반환한다. 입력이 남아 있으면 덮어쓰지 않는다.</summary>
        int Submit(RecoveryCommandData command);

        /// <summary>양수 번호와 정확히 일치하는 입력만 제거하며 결과 소비 확인은 바꾸지 않는다.</summary>
        bool RemoveCommand(int number);

        /// <summary>
        /// 실제 결과를 읽고 번호와 전체 명령을 대조한 호출자만 소비 확인을 올린다.
        /// Buffer는 숫자 범위·역행만 검사하고 게시 이력은 Core가 검증한다. 같은 번호는 허용한다.
        /// </summary>
        void AcknowledgeResult(int number);

        /// <summary>입력만 취소한다. 발급 번호와 결과 소비 확인은 보존한다.</summary>
        void Clear();
    }

    /// <summary>runtime 동안 한 회복 입력과 발급/소비 확인 번호를 보관한다.</summary>
    internal sealed class RecoveryViewBuffer : IRecoveryViewBufferReadSink,
        IRecoveryViewBufferWriteSink, IExportSystemServices
    {
        private RecoveryViewCommand _command;
        private int _lastIssuedNumber;
        private int _acknowledgedResultNumber;

        public int AcknowledgedResultNumber => _acknowledgedResultNumber;

        public int Submit(RecoveryCommandData command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (_command.Number != 0)
            {
                throw new InvalidOperationException("이전 회복 입력을 정리한 뒤 제출해야 합니다.");
            }
            if (_lastIssuedNumber == int.MaxValue)
            {
                throw new InvalidOperationException("회복 요청 번호를 더 발급할 수 없습니다.");
            }

            int number = _lastIssuedNumber + 1;
            _command = new RecoveryViewCommand(number, command);
            _lastIssuedNumber = number;
            return number;
        }

        public bool TryGetCommand(int expectedNumber, out RecoveryViewCommand command)
        {
            if (expectedNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedNumber));
            }
            if (_command.Number == expectedNumber)
            {
                command = _command;
                return true;
            }

            command = default;
            return false;
        }

        public bool RemoveCommand(int number)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }
            if (_command.Number != number)
            {
                return false;
            }

            _command = default;
            return true;
        }

        public void AcknowledgeResult(int number)
        {
            if (number < _acknowledgedResultNumber || number > _lastIssuedNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            _acknowledgedResultNumber = number;
        }

        public void Clear()
        {
            _command = default;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            sink.AddService<IRecoveryViewBufferReadSink>(this);
            sink.AddService<IRecoveryViewBufferWriteSink>(this);
        }
    }
}
