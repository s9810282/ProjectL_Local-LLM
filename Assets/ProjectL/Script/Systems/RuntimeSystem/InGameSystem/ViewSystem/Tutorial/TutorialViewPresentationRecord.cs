namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 실행 소유자가 마지막으로 요청한 표시와 그 완료 사실입니다(튜토리얼 설계 §8.3·§10). 사실만 기록하고 단계를 바꾸지 않습니다.
    /// 표시마다 세대를 새로 매겨, 세대가 다른 완료 통지(늦은 통지·지난 세션의 통지)는 버립니다.
    /// </summary>
    internal sealed class TutorialViewPresentationRecord
    {
        private int _generation;
        private int _sessionId;
        private int _stepRevision;
        private TutorialViewPresentationKind _kind;
        private bool _isCompleted;

        /// <summary>새 표시를 기록하고 그 세대를 돌려줍니다. 앞 표시의 완료 사실은 버립니다.</summary>
        internal int Begin(int sessionId, int stepRevision, TutorialViewPresentationKind kind)
        {
            Forget();
            _sessionId = sessionId;
            _stepRevision = stepRevision;
            _kind = kind;
            return _generation;
        }

        /// <summary>그 세대의 표시가 끝났다는 통지입니다. 지금 세대가 아니면 버립니다.</summary>
        internal void Complete(int generation)
        {
            if (generation == _generation)
                _isCompleted = true;
        }

        /// <summary>그 단계에 마지막으로 요청한 표시가 끝났는지와 그 종류입니다.</summary>
        internal bool TryGetCompleted(int sessionId, int stepRevision, out TutorialViewPresentationKind kind)
        {
            if (_isCompleted && sessionId != 0 && _sessionId == sessionId && _stepRevision == stepRevision)
            {
                kind = _kind;
                return true;
            }

            kind = TutorialViewPresentationKind.None;
            return false;
        }

        /// <summary>기록을 놓습니다. 이미 나간 완료 통지는 세대가 달라져 버려집니다.</summary>
        internal void Forget()
        {
            _generation++;
            _sessionId = 0;
            _stepRevision = 0;
            _kind = TutorialViewPresentationKind.None;
            _isCompleted = false;
        }
    }
}
