using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>
    /// 덱 편집 화면의 안내 문구 한 곳입니다. 규칙 수치는 문구에 넣지 않습니다(원본은 Core).
    /// 화면에 상태 글 칸은 없고, 거절·편집 불가·낡음 이유는 공용 안내로 한 번 띄웁니다(평가 C-06).
    /// </summary>
    internal static class DeckEditText
    {
        /// <summary>Core가 받은 덱이 화면의 병과 칸(병과당 한 칸)에 맞지 않을 때입니다(평가 C-09).</summary>
        internal const string LayoutMismatch = "덱 편성이 병과 칸(병과당 한 장)과 맞지 않아 여기서는 편집할 수 없습니다.";

        /// <summary>
        /// 지금 초안 상태에서 알릴 이유입니다. 정상 상태(확인 중·확정 가능·확정함·준비 중)면 null입니다.
        /// 놓기 거절은 상태가 아니라 한 번의 사건이라 여기 넣지 않고 컨트롤러가 따로 알립니다.
        /// </summary>
        internal static string Notice(DeckEditDraft draft)
        {
            if (draft == null) throw new System.ArgumentNullException(nameof(draft));
            string text = draft.State switch
            {
                DeckEditDraftState.NotReady => draft.ReadStatus == DeckReadStatus.StateChanged ? Changed : null,
                DeckEditDraftState.Valid => draft.Availability != null && !draft.Availability.CanEdit
                    ? Unavailable(draft.Availability.Reason)
                    : null,
                DeckEditDraftState.Invalid => Failure(draft.Reason),
                DeckEditDraftState.Stale => Changed,
                DeckEditDraftState.Unavailable => Unavailable(draft.Availability?.Reason ?? DeckUnavailableReason.NotReady),
                _ => null
            };
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private const string Changed = "덱 정보가 바뀌었습니다. 다시 평가해 주세요.";

        internal static string Failure(DeckFailureReason reason) => reason switch
        {
            DeckFailureReason.None => string.Empty,
            DeckFailureReason.EmptyDeck => "덱이 비어 있습니다.",
            DeckFailureReason.UnknownOwnedId => "보유하지 않은 카드가 들어 있습니다.",
            DeckFailureReason.DuplicateOwnedId => "같은 카드가 두 번 들어 있습니다.",
            DeckFailureReason.UnsupportedUnit => "덱에 넣을 수 없는 유닛이 들어 있습니다.",
            DeckFailureReason.MissingMain => "메인 캐릭터가 필요합니다.",
            DeckFailureReason.MultipleMains => "메인 캐릭터는 하나만 넣을 수 있습니다.",
            DeckFailureReason.TooManyCards => "덱에 넣을 수 있는 수를 넘었습니다.",
            DeckFailureReason.StaleRun or DeckFailureReason.StaleRoster or DeckFailureReason.StaleDeck or
                DeckFailureReason.StateChanged => Changed,
            DeckFailureReason.WrongPhase => "지금은 덱을 바꿀 수 없습니다.",
            DeckFailureReason.NotReady => "덱 정보를 준비하고 있습니다.",
            _ => "덱을 확인하지 못했습니다."
        };

        internal static string Unavailable(DeckUnavailableReason reason) => reason switch
        {
            DeckUnavailableReason.None => string.Empty,
            DeckUnavailableReason.NotReady => "아직 덱을 바꿀 준비가 되지 않았습니다.",
            DeckUnavailableReason.InactiveStage => "스테이지 맵에서만 덱을 바꿀 수 있습니다.",
            DeckUnavailableReason.Stopped => "지금은 덱을 바꿀 수 없습니다.",
            DeckUnavailableReason.Faulted => "오류가 발생해 덱을 바꿀 수 없습니다.",
            DeckUnavailableReason.Loading => "불러오는 중에는 덱을 바꿀 수 없습니다.",
            DeckUnavailableReason.JobPending or DeckUnavailableReason.RequestPending =>
                "처리 중인 요청이 끝난 뒤 덱을 바꿀 수 있습니다.",
            DeckUnavailableReason.RunEnded => "끝난 Run에서는 덱을 바꿀 수 없습니다.",
            DeckUnavailableReason.BattleRetained => "전투 중에는 덱을 바꿀 수 없습니다.",
            DeckUnavailableReason.InteractionOpen => "지금 노드를 마친 뒤 덱을 바꿀 수 있습니다.",
            DeckUnavailableReason.AuthorityUnbound => "덱 편집이 아직 연결되지 않았습니다.",
            _ => "지금은 덱을 바꿀 수 없습니다."
        };
    }
}
