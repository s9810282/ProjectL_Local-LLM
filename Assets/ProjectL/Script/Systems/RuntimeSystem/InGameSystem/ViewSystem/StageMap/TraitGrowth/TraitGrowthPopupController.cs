using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth
{
    /// <summary>
    /// 증강 선택 화면 한 연결의 수명입니다. 제안을 그리고, 고른 것을 요청 소유자에게 제출하고, 결과가 오면 바로 확인한 뒤
    /// 다음 제안을 그립니다(공통 레이아웃 설계 SO8). 언제 여는지는 호스트가 정하고, 보일 것이 없어지면 스스로 닫습니다.
    /// </summary>
    /// <remarks>
    /// 사실은 매번 원본(Buffer·요청 소유자·Stage 사실)에서 읽고, 이 클래스는 지금 그린 제안 참조만 갖습니다.
    /// 다시 연결돼도 Buffer에서 같은 제안을 그리고, 확인하지 않은 결과가 있으면 먼저 확인합니다.
    /// </remarks>
    internal sealed class TraitGrowthPopupController : IPopupViewController, IStageOfferInput, IDisposable
    {
        private readonly ITraitGrowthStateReadSink _offers;
        private readonly ITraitGrowthResultReadSink _results;
        private readonly ITraitGrowthViewReadSink _inputs;
        private readonly ITraitGrowthViewWriteSink _writer;
        private readonly ITraitGrowthRequestSubmitter _requests;
        private readonly IStageTraitGrowthFacts _facts;
        private readonly TraitGrowthPopupModelBuilder _models;
        private readonly IStageOfferDisplay _screen;
        private readonly StagePopupViewLifetime _lifetime;
        // 매 Tick·열기 판단마다 대리자를 새로 만들지 않게 한 번만 만듭니다(검토 L4).
        private readonly Action _tick;
        private readonly Action _tryOpen;
        private TraitGrowthOffer _drawnOffer;
        private bool _preparingShown;
        private bool _shown;
        private bool? _canSubmit;

        internal TraitGrowthPopupController(ITraitGrowthStateReadSink offers, ITraitGrowthResultReadSink results,
            ITraitGrowthViewReadSink inputs, ITraitGrowthViewWriteSink writer, ITraitGrowthRequestSubmitter requests,
            IStageTraitGrowthFacts facts, TraitGrowthPopupModelBuilder models, IUISystemService ui,
            IStageOfferDisplay screen, PopupInputSession input, IPopupViewHost host, RuntimeFaultBoundary faults)
        {
            _offers = offers ?? throw new ArgumentNullException(nameof(offers));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _models = models ?? throw new ArgumentNullException(nameof(models));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _lifetime = new StagePopupViewLifetime("TraitGrowthPopup", ui, new StageOfferPopupViewScreen(screen), input,
                this, host, faults);
            _tick = TickCore;
            _tryOpen = TryOpenCore;
        }

        internal bool IsOpen => _lifetime.IsOpen;

        /// <summary>증강 선택이 남아 있는지입니다. 호스트가 열지 판단하고 다음 노드로 가는 것을 막는 데 씁니다(계획 P-TG1).</summary>
        internal bool IsPending => HasUnconsumedResult || _offers.ActiveOffer != null || _facts.IsPending;

        private bool HasUnconsumedResult => _results.HandledCommandNumber > _inputs.AcknowledgedResultNumber;

        internal void Start() => _lifetime.Start(() =>
        {
            _screen.SetCanSubmit(false);
            if (_lifetime.CanWork) _screen.BindInput(this);
        }, Dispose);

        /// <summary>호스트가 여는 규칙을 만족할 때 부릅니다. 열 수 없으면 아무것도 하지 않습니다.</summary>
        internal void TryOpen() => Guard(_tryOpen);

        internal void Tick() => Guard(_tick);

        private void TryOpenCore()
        {
            if (IsPending && _lifetime.CanOpen) _lifetime.Open();
        }

        private void TickCore()
        {
            _lifetime.Tick();
            if (!_lifetime.CanWork) return;
            if (!IsPending)
            {
                // 보일 것이 없어지면 자리와 무관하게 닫습니다(설계 §14 TG2).
                if (IsOpen) _lifetime.Close(false);
                return;
            }
            // 멈춘 흐름은 제안이 다시 오지 않고 나갈 길도 없어 오류 경계로 올립니다.
            if (_facts.IsGrowthStopped)
                throw new InvalidOperationException("[TraitGrowthPopup] 증강 흐름이 멈춰 제안을 받을 수 없습니다.");
            if (_shown && IsOpen) Refresh();
        }

        void IPopupViewController.Show()
        {
            if (!_lifetime.IsActive) return;
            _shown = true;
            _drawnOffer = null;
            _preparingShown = false;
            _canSubmit = null;
            Guard(Refresh);
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            _shown = false;
            _drawnOffer = null;
            _preparingShown = false;
            BlockSubmission();
        }

        void IStageOfferInput.SelectCard(int index) => Guard(() =>
        {
            TraitGrowthOffer offer = _offers.ActiveOffer;
            if (!_shown || HasUnconsumedResult || _requests.HasPending || offer == null ||
                !ReferenceEquals(offer, _drawnOffer) || index < 0 || index >= offer.Choices.Count) return;
            // 제출 전에 먼저 잠가 같은 프레임의 두 번째 입력을 막습니다. 번호는 요청 소유자가 같은 호출에서 보관합니다.
            SetCanSubmit(false);
            _requests.Submit(new TraitGrowthChoiceRequest(offer.RunId, offer.OfferId, offer.OwnedId, offer.StepId,
                offer.Choices[index].TraitKey));
        });

        // 증강은 반드시 골라야 해서 나가기 버튼을 그리지 않습니다. 레이아웃이 이 호출을 내지 않습니다(R4).
        void IStageOfferInput.Exit()
        {
        }

        // 결과가 있으면 먼저 확인하고, 제안이 있으면 제안을, 없으면 준비 중을 그립니다. 같은 참조면 다시 그리지 않습니다.
        private void Refresh()
        {
            if (!_shown || !IsOpen) return;
            if (HasUnconsumedResult && !TryAcknowledgeLatestResult()) return;
            TraitGrowthOffer offer = _offers.ActiveOffer;
            if (offer == null)
            {
                // 마지막 확인 뒤처럼 남은 성장이 없으면 그리지 않습니다. 다음 Tick이 닫습니다(검토 L1).
                if (_preparingShown || !IsPending) return;
                _drawnOffer = null;
                _preparingShown = true;
                _screen.Render(_models.Preparing());
                SetCanSubmit(false);
                return;
            }
            if (!ReferenceEquals(offer, _drawnOffer))
            {
                _drawnOffer = offer;
                _preparingShown = false;
                _screen.Render(_models.Offer(offer));
            }
            // 잠금의 원본은 요청 소유자의 대기 번호입니다(계획 §5.3 TG-b). 바뀔 때만 화면에 내립니다.
            SetCanSubmit(!_requests.HasPending);
        }

        /// <summary>결과를 바로 확인합니다. 실패면 안내를 켜고 지금 제안을 다시 그리게 합니다. 성공 안내는 없습니다(설계 SO8).</summary>
        private bool TryAcknowledgeLatestResult()
        {
            TraitGrowthProcessedResult result = _results.LatestResult;
            if (result == null) return false;
            // 확인 Run은 요청의 Run이 아니라 결과를 게시한 Run입니다(Core 인계 §3). 거절되면 고를 수 없는 화면에 갇히므로 멈춥니다.
            if (!_writer.AcknowledgeResult(result.RunId, result.Number))
                throw new InvalidOperationException($"[TraitGrowthPopup] 증강 결과 확인이 거절됐습니다: 요청 {result.Number}.");
            TraitGrowthResult outcome = result.Result;
            if (IsUnexpected(outcome.FailureReason))
                throw new InvalidOperationException(
                    $"[TraitGrowthPopup] 여기서 나오면 안 되는 증강 결과입니다: {outcome.FailureReason}, 요청 {result.Number}.");
            _drawnOffer = null;
            if (!outcome.Succeeded) _screen.ShowError(_models.ErrorText(outcome.FailureReason));
            return true;
        }

        private void SetCanSubmit(bool canSubmit)
        {
            if (_canSubmit == canSubmit) return;
            _canSubmit = canSubmit;
            _screen.SetCanSubmit(canSubmit);
        }

        private void BlockSubmission()
        {
            _canSubmit = false;
            if (_lifetime.IsCurrent) _screen.SetCanSubmit(false);
        }

        // 여기서 나오면 안 되는 결과입니다. 확인한 뒤 오류 경계에 보고합니다(설계 §14 TG4 표).
        private static bool IsUnexpected(TraitGrowthFailureReason reason) =>
            reason == TraitGrowthFailureReason.InvalidRequest || reason == TraitGrowthFailureReason.UnknownDefinition ||
            reason == TraitGrowthFailureReason.AwaitingAcknowledgement;

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        public void Dispose()
        {
            if (_lifetime.IsDisposed) return;
            _shown = false;
            _drawnOffer = null;
            // 공유 화면의 수신자는 수명 부품이 현재 연결일 때만 끊습니다.
            _lifetime.Dispose(BlockSubmission, () => _screen.BindInput(null));
        }
    }
}
