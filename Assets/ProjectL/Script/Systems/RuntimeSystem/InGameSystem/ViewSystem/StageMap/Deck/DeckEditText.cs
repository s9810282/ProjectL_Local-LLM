using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>덱 편집 화면의 안내 문구 한 곳입니다. 규칙 수치는 문구에 넣지 않습니다(원본은 Core).</summary>
    internal static class DeckEditText
    {
        internal static string Status(DeckEditDraft draft)
        {
            // 확정은 이미 일어났으므로 뒤이은 다시 읽기의 상태와 무관하게 먼저 알립니다.
            if (draft.Committed) return "덱을 확정했습니다.";
            switch (draft.State)
            {
                case DeckEditDraftState.NotReady:
                    return draft.ReadStatus == DeckReadStatus.StateChanged ? Changed : "덱 정보를 준비하고 있습니다.";
                case DeckEditDraftState.Unevaluated:
                    return "덱을 확인하고 있습니다.";
                case DeckEditDraftState.Valid:
                    return draft.Availability != null && !draft.Availability.CanEdit
                        ? Unavailable(draft.Availability.Reason)
                        : "확정할 수 있습니다.";
                case DeckEditDraftState.Invalid:
                    return Failure(draft.Reason);
                case DeckEditDraftState.Stale:
                    return Changed;
                case DeckEditDraftState.Unavailable:
                    return Unavailable(draft.Availability?.Reason ?? DeckUnavailableReason.NotReady);
                default:
                    return string.Empty;
            }
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
