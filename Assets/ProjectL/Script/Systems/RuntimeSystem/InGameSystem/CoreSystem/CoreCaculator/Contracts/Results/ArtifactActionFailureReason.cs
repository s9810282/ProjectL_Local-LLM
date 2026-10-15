namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>아티팩트 배치, 이동, 반환, 보드 확장이 실패한 이유입니다.</summary>
    public enum ArtifactActionFailureReason
    {
        /// <summary>행동이 성공하여 실패 사유가 없습니다.</summary>
        None = 0,

        /// <summary>요청한 ArtifactId에 해당하는 정의가 없습니다.</summary>
        DefinitionNotFound = 1,

        /// <summary>배치할 아티팩트가 Pool에 없습니다.</summary>
        NotInPool = 2,

        /// <summary>이동하거나 반환할 배치 인스턴스가 없습니다.</summary>
        InstanceNotFound = 3,

        /// <summary>배치하려는 인스턴스 ID가 이미 보드에 등록되어 있습니다.</summary>
        InstanceAlreadyPlaced = 4,

        /// <summary>아티팩트의 점유 영역이 보드 범위를 벗어납니다.</summary>
        OutOfBounds = 5,

        /// <summary>아티팩트의 점유 영역이 다른 인스턴스와 겹칩니다.</summary>
        CellOccupied = 6,

        /// <summary>요청한 보드의 너비 또는 높이가 0 이하입니다.</summary>
        InvalidBoardSize = 7,

        /// <summary>요청한 보드의 너비 또는 높이가 기존 크기보다 작습니다.</summary>
        BoardShrinkNotAllowed = 8,

        /// <summary>요청한 보드 크기가 기존 크기와 같습니다.</summary>
        BoardSizeUnchanged = 9,

        /// <summary>확장할 보드에 기존 배치 상태를 복원하지 못했습니다. 내부 상태 점검이 필요합니다.</summary>
        BoardRebuildFailed = 10,

        /// <summary>전투 상태가 유지되는 동안 아티팩트 편집이 차단됩니다.</summary>
        LayoutLocked = 11,

        /// <summary>폐기된 구형 Layout Revision 실패 값입니다. 번호를 재사용하지 않습니다.</summary>
        RevisionOverflow = 12,
    }
}
