using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>미배치된 편성 개체가 있으면 시작 확인을 거치며, 확정 때 현재 입력 상태를 다시 검사합니다.</summary>
    internal sealed class CombatDeploymentConfirmation : IUIConfirmationReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIEnterCombatAccept = new("Combat.Action.UI.EnterCombat.Accept");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIEnterCombatDecline = new("Combat.Action.UI.EnterCombat.Decline");
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIEnterCombatRequest = new("Combat.Action.UI.EnterCombat.Request");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIEnterCombatRespond = new("Combat.Action.UI.EnterCombat.Respond");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIEnterCombatClose = new("Combat.Action.UI.EnterCombat.Close");
#endif

        private readonly ICharacterBufferReadSink _characters;
        private readonly Func<RunDeckSnapshot> _membership;
        private readonly IUIConfirmationPresentation _presentation;
        private readonly Func<bool> _canEnter;
        private readonly Action _enter;
        private readonly Action _cancelInputs;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Action _mainNotPlaced;

        /// <param name="membership">이번 전투의 참가 목록(진입 때 포착한 덱)입니다. 전투가 없으면 null을 돌려줍니다.</param>
        internal CombatDeploymentConfirmation(ICharacterBufferReadSink characters, Func<RunDeckSnapshot> membership,
            IUIConfirmationPresentation presentation,
            Func<bool> canEnter, Action enter, Action cancelInputs, RuntimeFaultBoundary faults, Action mainNotPlaced)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _membership = membership ?? throw new ArgumentNullException(nameof(membership));
            // 운영은 늘 넘깁니다(입력 재료 묶음이 null을 거절). 쓰는 곳마다 다시 보지 않도록 생성 때 막습니다(평가 C5-02).
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _canEnter = canEnter ?? throw new ArgumentNullException(nameof(canEnter));
            _enter = enter ?? throw new ArgumentNullException(nameof(enter));
            _cancelInputs = cancelInputs ?? throw new ArgumentNullException(nameof(cancelInputs));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _mainNotPlaced = mainNotPlaced ?? throw new ArgumentNullException(nameof(mainNotPlaced));
        }

        internal bool IsOpen => _presentation.IsConfirmationOpen(this);

        private bool HasUnplacedUnits()
        {
            // 덱 밖 보유 유닛은 전투에 들어오지 않으므로 참가 목록만 셉니다(Run 덱 설계 D10, 계획 RD-2 Q-RD3).
            RunDeckSnapshot membership = _membership();
            if (membership == null) return false;
            var participants = membership.OrderedIds;
            for (int i = 0; i < participants.Count; i++)
            {
                CharacterData character = FindCharacter(participants[i]);
                // 사망 사실이 Buffer에 있는 개체만 제외합니다. 아직 생성·배치되지 않은 개체를 사망으로 추정하지 않습니다.
                if (character != null && character.Health.IsDead) continue;
                if (!IsPlaced(character)) return true;
            }
            return false;
        }

        private CharacterData FindCharacter(CharacterInstanceId ownedId)
        {
            var characters = _characters.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                if (character.OwnedId == ownedId) return character;
            }
            return null;
        }

        private static bool IsPlaced(CharacterData character) =>
            character != null && character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead;

        private bool CheckMainPlacement()
        {
            // 메인은 참가 목록의 MainId입니다. 보유 목록에서 역할로 찾지 않습니다(런 덱 Core → View 인계 §4).
            RunDeckSnapshot membership = _membership();
            if (membership != null && IsPlaced(FindCharacter(membership.MainId))) return true;
            _mainNotPlaced();
            return false;
        }

        internal void Request()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIEnterCombatRequestSample = _profileUIEnterCombatRequest.Auto();
#endif

            if (IsOpen) return;
            // 제출할 수 없는 요청은 기존 경로(Router)로 넘겨 튜토리얼 막힘 안내를 받습니다. 턴 종료 확인창과 같습니다(평가 C10-09).
            // 다른 이유로 제출할 수 없으면 Router의 대기 제출이 지금처럼 조용히 거절합니다.
            if (!_canEnter())
            {
                _enter();
                return;
            }
            if (!CheckMainPlacement()) return;
            if (!HasUnplacedUnits())
            {
                _enter();
                return;
            }
            _cancelInputs();
            if (_canEnter())
                _presentation.ShowConfirmation("아직 배치하지 않은 유닛이 있습니다. 전투를 시작할까요?", this);
        }

        public void Respond(bool accepted)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileDecision = (accepted ? _profileUIEnterCombatAccept : _profileUIEnterCombatDecline).Auto();
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIEnterCombatRespondSample = _profileUIEnterCombatRespond.Auto();
#endif

            try
            {
                if (accepted && _canEnter() && CheckMainPlacement()) _enter();
            }
            catch (Exception error) { _faults.Report(error); }
        }

        internal void Sync()
        {
            if (IsOpen && !_canEnter()) Close();
        }

        internal void Close()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIEnterCombatCloseSample = _profileUIEnterCombatClose.Auto();
#endif
            _presentation.HideConfirmation(this);
        }
    }
}
