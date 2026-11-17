using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>텍스트 요청을 넘기고 즉시 끝나는 곁들임 조각입니다.</summary>
    internal sealed class CombatBodyTextPiece : ICombatPresentationPiece
    {
        private readonly CombatBodyTextWindow _window;
        private readonly EntityId _unit;
        private readonly string _text;
        private readonly CombatBodyTextData _data;
        internal CombatBodyTextPiece(CombatBodyTextWindow window, EntityId unit, string text, CombatBodyTextData data)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _unit = unit;
            _text = text;
            _data = data;
        }

        public void Begin(CombatPieceContext context)
        {
            _window.TryShow(_unit, _text, _data);
            IsEnded = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public bool IsEnded { get; private set; }

        public void Cancel()
        {
        }
    }
}
