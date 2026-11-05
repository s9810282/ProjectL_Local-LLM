using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput
{
    /// <summary>튜토리얼 입력 어댑터를 연결·해제하는 자리입니다. 실행 소유자가 런타임 동안 연결합니다.</summary>
    internal interface ITutorialInputReceiverSlot
    {
        void Bind(ITutorialInputReceiver receiver);

        void Unbind(ITutorialInputReceiver receiver);
    }

    /// <summary>
    /// 튜토리얼 입력 어댑터를 연결하는 수신 자리입니다. Controller는 입력 조립 때, 어댑터는 실행 소유자가 서비스를 받은 뒤 만들어지기 때문입니다.
    /// 연결된 수신자가 없으면 입력을 버립니다.
    /// </summary>
    internal sealed class TutorialInputReceiverSlot : ITutorialInputReceiverSlot, ITutorialInputReceiver
    {
        private ITutorialInputReceiver _receiver;

        public void Bind(ITutorialInputReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (_receiver != null)
                throw new InvalidOperationException("튜토리얼 입력 수신 대상이 이미 연결되어 있습니다. 먼저 Unbind하세요.");

            _receiver = receiver;
        }

        public void Unbind(ITutorialInputReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (ReferenceEquals(_receiver, receiver))
                _receiver = null;
        }

        public void OnNextPressed() => _receiver?.OnNextPressed();

        public void OnNextReleased() => _receiver?.OnNextReleased();

        public void OnNextCanceled() => _receiver?.OnNextCanceled();
    }
}
