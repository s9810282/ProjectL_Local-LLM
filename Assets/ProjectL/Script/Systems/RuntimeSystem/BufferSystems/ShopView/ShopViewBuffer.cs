using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView
{
    public interface IShopViewBufferReadSink : IServiceModule
    {
        IReadOnlyList<ShopViewCommand> Commands { get; }
        bool TryGetNextCommand(int handledThrough, out ShopViewCommand command);
    }
    public interface IShopViewBufferWriteSink : IServiceModule
    {
        int Submit(ShopViewCommandData command);
        void RemoveCommandsThrough(int handledNumber);
        void Clear();
    }

    /// <summary>View 입력만 보관하며 Core의 처리 완료 기록은 소유하지 않습니다.</summary>
    internal sealed class ShopViewBuffer : IShopViewBufferReadSink, IShopViewBufferWriteSink, IExportSystemServices
    {
        private readonly List<ShopViewCommand> _commands = new();
        private readonly ReadOnlyCollection<ShopViewCommand> _readOnlyCommands;
        private int _lastIssuedNumber;
        public IReadOnlyList<ShopViewCommand> Commands => _readOnlyCommands;

        internal ShopViewBuffer()
        {
            _readOnlyCommands = _commands.AsReadOnly();
        }

        public int Submit(ShopViewCommandData command)
        {
            command.Validate();
            if (_lastIssuedNumber == int.MaxValue) throw new InvalidOperationException("ShopViewBuffer 요청 번호를 더 발급할 수 없습니다.");
            int number = _lastIssuedNumber + 1;
            _commands.Add(new ShopViewCommand(number, command));
            _lastIssuedNumber = number;
            return number;
        }
        public bool TryGetNextCommand(int handledThrough, out ShopViewCommand command)
        {
            if (handledThrough < 0) throw new ArgumentOutOfRangeException(nameof(handledThrough));
            for (int i = 0; i < _commands.Count; i++)
            {
                if (_commands[i].Number <= handledThrough) continue;
                command = _commands[i];
                return true;
            }
            command = default;
            return false;
        }
        public void RemoveCommandsThrough(int handledNumber)
        {
            if (handledNumber < 0 || handledNumber > _lastIssuedNumber) throw new ArgumentOutOfRangeException(nameof(handledNumber));
            int count = 0;
            while (count < _commands.Count && _commands[count].Number <= handledNumber) count++;
            _commands.RemoveRange(0, count);
        }
        public void Clear() => _commands.Clear();
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IShopViewBufferReadSink>(this);
            sink.AddService<IShopViewBufferWriteSink>(this);
        }
    }
}
