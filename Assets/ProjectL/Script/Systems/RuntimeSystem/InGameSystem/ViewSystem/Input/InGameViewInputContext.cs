using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Input
{
    /// <summary>View 그룹이 소유하는 화면별 입력 슬롯의 접근 계약입니다.</summary>
    public interface IInGameViewInputContext
    {
        IStageInputReceiverSlot Stage { get; }
        IStageSelectionReceiverSlot StageSelection { get; }
        IArtifactInputSlot Artifact { get; }
        ILobbyReceiverSlot Lobby { get; }
    }

    internal sealed class InGameViewInputContext : IInGameViewInputContext
    {
        public IStageInputReceiverSlot Stage { get; }
        public IStageSelectionReceiverSlot StageSelection { get; }
        public IArtifactInputSlot Artifact { get; }
        public ILobbyReceiverSlot Lobby { get; }

        internal InGameViewInputContext(IStageInputReceiverSlot stage,
            IStageSelectionReceiverSlot stageSelection, IArtifactInputSlot artifact, ILobbyReceiverSlot lobby)
        {
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            StageSelection = stageSelection ?? throw new ArgumentNullException(nameof(stageSelection));
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Lobby = lobby ?? throw new ArgumentNullException(nameof(lobby));
        }
    }
}
