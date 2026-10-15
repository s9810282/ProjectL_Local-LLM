using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 결과 하나의 재생 수명을 소유하는 유일한 곳입니다(설계 §9.1, W16).
    /// 연결 세대, 재생 중인 결과 번호, 완료/실패 통지, 후처리 진입 여부를 보관하고 토큰으로 오래된 통지·재진입을 막습니다.
    /// </summary>
    /// <remarks>
    /// Buffer·Presenter·Layer·오류 경계를 모릅니다. 통지 콜백은 사실만 기록하며, 실패 보고·그리기·결과 확인은
    /// CombatViewSystem이 Tick에서 합니다. 영구 완료 번호는 저장하지 않습니다(완료 기준은 Buffer의 P).
    /// </remarks>
    internal sealed class CombatResultPlayback
    {
        private readonly ICombatPresentation _presentation;

        private int _generation;
        private bool _isAttached;
        private int _presentingSequence;
        private CombatPresentationOutcome? _outcome;
        private Exception _notificationViolation;
        private bool _isFinalizing;

        internal CombatResultPlayback(ICombatPresentation presentation)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        }

        /// <summary>씬 연결 세대가 살아 있는지입니다. Abort 뒤에는 다음 Attach 전까지 재생을 시작하지 않습니다.</summary>
        internal bool IsAttached => _isAttached;

        /// <summary>재생 또는 후처리 중인 결과가 있는지입니다.</summary>
        internal bool IsBusy => _presentingSequence != 0;

        /// <summary>재생 또는 후처리 중인 결과의 순번입니다. 없으면 0입니다.</summary>
        internal int PresentingSequence => _presentingSequence;

        /// <summary>완료된 결과의 후처리(그리기·입력 정리·확인) 중인지입니다.</summary>
        internal bool IsFinalizing => _isFinalizing;

        /// <summary>새 연결 세대를 시작합니다. 이전 재생이 남아 있으면 먼저 Abort로 정리합니다.</summary>
        internal void Attach()
        {
            Abort();
            _isAttached = true;
        }

        /// <summary>
        /// Play 전에 결과 번호·세대와 재생 상태를 고정한 뒤 연출을 시작합니다.
        /// 같은 결과를 두 번 재생하지 않도록 재생 중이면 거부합니다. Play의 동기 예외는 그대로 전파합니다.
        /// </summary>
        internal CombatPlaybackToken Begin(CombatResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!_isAttached)
                throw new InvalidOperationException("[CombatResultPlayback] 씬 연결 세대가 없어 재생을 시작할 수 없습니다.");
            if (IsBusy)
                throw new InvalidOperationException(
                    $"[CombatResultPlayback] 결과 {_presentingSequence}를 재생 중이라 결과 {result.Sequence}를 시작할 수 없습니다.");

            var token = new CombatPlaybackToken(_generation, result.Sequence);
            _presentingSequence = result.Sequence;
            _outcome = null;
            _notificationViolation = null;
            _isFinalizing = false;

            _presentation.Play(result, outcome => OnFinished(token, outcome));
            return token;
        }

        /// <summary>현재 재생의 실패(실패 통지 또는 잘못된·중복 통지)를 읽기만 합니다.</summary>
        internal bool TryGetFailure(out Exception error)
        {
            if (_notificationViolation != null)
            {
                error = _notificationViolation;
                return true;
            }

            if (_outcome is { IsFailed: true } failed)
            {
                error = failed.Error;
                return true;
            }

            error = null;
            return false;
        }

        /// <summary>
        /// 완료 통지를 받은 결과만 후처리 진입을 한 번 허용합니다. 아직 재생 중이거나 실패면 false입니다.
        /// </summary>
        internal bool TryBeginFinalization(out CombatPlaybackToken token)
        {
            if (!IsBusy || _isFinalizing || _notificationViolation != null || _outcome is not { IsCompleted: true })
            {
                token = default;
                return false;
            }

            _isFinalizing = true;
            token = new CombatPlaybackToken(_generation, _presentingSequence);
            return true;
        }

        /// <summary>토큰의 세대·결과가 아직 유효한지입니다. 외부 호출에서 돌아올 때마다 확인합니다.</summary>
        internal bool IsCurrent(CombatPlaybackToken token)
        {
            return _isAttached
                && token.Sequence != 0
                && token.Generation == _generation
                && token.Sequence == _presentingSequence;
        }

        /// <summary>마지막 결과 확인이 끝난 뒤 일시적 재생 상태를 비웁니다.</summary>
        internal void CompleteFinalization(CombatPlaybackToken token)
        {
            if (!_isFinalizing || !IsCurrent(token))
                throw new InvalidOperationException("[CombatResultPlayback] 후처리 중인 현재 결과가 아니라 완료할 수 없습니다.");

            ClearCurrent();
        }

        /// <summary>
        /// 세대·토큰·완료 예약을 먼저 무효화한 뒤 진행 중 연출을 취소합니다. 반복 호출해도 안전합니다.
        /// 오류 정지·Detach·Shutdown에서만 부르며, 정상 전투 종료에서는 부르지 않습니다.
        /// </summary>
        internal void Abort()
        {
            bool wasPresenting = IsBusy;
            _generation++;
            _isAttached = false;
            ClearCurrent();

            // 무효화를 마친 뒤 취소하므로, 취소 중 늦게 도착한 통지는 오래된 통지로 무시된다.
            if (wasPresenting)
                _presentation.Cancel();
        }

        private void OnFinished(CombatPlaybackToken token, CombatPresentationOutcome outcome)
        {
            // Abort·다음 결과 이후의 늦은 통지는 무시한다. 콜백에서는 사실만 기록하고 아무것도 호출하지 않는다.
            if (!IsCurrent(token))
                return;

            if (_outcome.HasValue || _isFinalizing)
            {
                _notificationViolation ??= new InvalidOperationException(
                    $"[CombatResultPlayback] 결과 {token.Sequence}의 연출 통지가 두 번 왔습니다.");
                return;
            }

            if (!outcome.IsValid)
            {
                _notificationViolation ??= new InvalidOperationException(
                    $"[CombatResultPlayback] 결과 {token.Sequence}의 연출 통지가 완료도 실패도 아닙니다.");
                return;
            }

            _outcome = outcome;
        }

        private void ClearCurrent()
        {
            _presentingSequence = 0;
            _outcome = null;
            _notificationViolation = null;
            _isFinalizing = false;
        }
    }
}
