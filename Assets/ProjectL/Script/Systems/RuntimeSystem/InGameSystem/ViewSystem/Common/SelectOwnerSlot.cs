using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 입력 수신 자리의 연결 대상과 "누름을 받은 대상만 뗌·취소를 받는다"는 선택 소유자 규칙을 한 곳에 둡니다.
    /// 로비·아티팩트 입력 Slot이 함께 씁니다(로비 리팩토링 2차 설계 M-10). 무엇을 전달할지는 각 Slot이 정합니다.
    /// </summary>
    internal sealed class SelectOwnerSlot<T> where T : class
    {
        private readonly string _name;
        private T _selectOwner;

        internal SelectOwnerSlot(string name)
        {
            _name = string.IsNullOrEmpty(name) ? throw new ArgumentException("이름이 필요합니다.", nameof(name)) : name;
        }

        /// <summary>지금 연결된 수신 대상입니다. 없으면 null입니다.</summary>
        internal T Receiver { get; private set; }

        internal void Bind(T receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (Receiver != null)
                throw new InvalidOperationException($"[{_name}] 입력 수신 대상이 이미 연결되어 있습니다. 먼저 Unbind하세요.");
            Receiver = receiver;
        }

        /// <summary>
        /// 연결을 풉니다. 지금 대상이 아니면 아무것도 하지 않고 false입니다. 풀린 대상이 진행 중인 선택을 갖고 있었으면 true이고,
        /// 호출자가 그 대상에 취소를 한 번 보냅니다. 취소 콜백이 새 대상을 연결해도 덮어쓰지 않도록 상태를 먼저 비웁니다.
        /// </summary>
        internal bool Unbind(T receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (!ReferenceEquals(Receiver, receiver)) return false;
            bool hadSelection = ReferenceEquals(_selectOwner, receiver);
            Receiver = null;
            _selectOwner = null;
            return hadSelection;
        }

        /// <summary>
        /// 누름의 소유자를 지금 대상으로 기록하고 그 대상을 돌려줍니다. 대상이 없으면 null입니다.
        /// 누름 콜백 안에서 Unbind되어도 같은 선택을 한 번 취소할 수 있도록 전달 전에 기록합니다.
        /// </summary>
        internal T BeginSelect()
        {
            T receiver = Receiver;
            if (receiver != null) _selectOwner = receiver;
            return receiver;
        }

        /// <summary>선택의 뗌·취소를 받을 소유자를 꺼내고 지웁니다. 콜백 전에 지워 중복 전달을 막습니다.</summary>
        internal T TakeSelectOwner()
        {
            T owner = _selectOwner;
            _selectOwner = null;
            return owner;
        }
    }
}
