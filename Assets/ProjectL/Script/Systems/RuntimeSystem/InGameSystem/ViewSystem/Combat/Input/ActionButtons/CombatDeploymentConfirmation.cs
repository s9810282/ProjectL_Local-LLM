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
            // 표시를 쓰지 않는 시험 조립은 생략할 수 있지만 확인이 필요한 요청에서는 누락을 오류로 보고합니다.
            _presentation = presentation;
            _canEnter = canEnter ?? throw new ArgumentNullException(nameof(canEnter));
            _enter = enter ?? throw new ArgumentNullException(nameof(enter));
            _cancelInputs = cancelInputs ?? throw new ArgumentNullException(nameof(cancelInputs));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _mainNotPlaced = mainNotPlaced ?? throw new ArgumentNullException(nameof(mainNotPlaced));
        }

        internal bool IsOpen => _presentation?.IsConfirmationOpen(this) ?? false;

        internal bool HasUnplacedUnits()
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
            if (IsOpen || !_canEnter()) return;
            if (!CheckMainPlacement()) return;
            if (!HasUnplacedUnits())
            {
                _enter();
                return;
            }
            if (_presentation == null)
                throw new InvalidOperationException("[CombatDeploymentConfirmation] 미배치 유닛의 전투 시작 확인에 IUIConfirmationPresentation이 필요합니다.");
            _cancelInputs();
            if (_canEnter())
                _presentation.ShowConfirmation("아직 배치하지 않은 유닛이 있습니다. 전투를 시작할까요?", this);
        }

        public void Respond(bool accepted)
        {
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

        internal void Close() => _presentation?.HideConfirmation(this);
    }
}
