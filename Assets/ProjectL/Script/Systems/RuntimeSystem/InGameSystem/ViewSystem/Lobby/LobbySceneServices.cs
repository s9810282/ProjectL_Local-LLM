using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>로비 씬 표시(방·판·캐릭터)가 쓰는 값의 묶음입니다(로비 리팩토링 2차 설계 N-01).</summary>
    internal sealed class LobbySceneServices
    {
        internal LobbySceneServices(LobbyModelData models, LobbyBoardData board,
            LobbyCharacterPresentationData characterPresentation)
        {
            Models = models ?? throw new ArgumentNullException(nameof(models));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            CharacterPresentation = characterPresentation ?? throw new ArgumentNullException(nameof(characterPresentation));
        }

        internal LobbyModelData Models { get; }
        internal LobbyBoardData Board { get; }
        internal LobbyCharacterPresentationData CharacterPresentation { get; }
    }
}
