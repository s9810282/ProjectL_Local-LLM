namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base
{
    /// <summary>
    /// Build Settings 씬 번호의 단일 기준입니다(설계 W3, §11). 모든 LifeCycle의 SceneIndex는 이 상수를 씁니다.
    /// 씬을 추가하거나 순서를 바꾸면 이 표와 Build Settings를 함께 고칩니다.
    /// </summary>
    internal static class SceneIndices
    {
        /// <summary>부트스트랩 전용 씬입니다. LifeCycle을 배정하지 않습니다.</summary>
        public const int Boot = 0;

        public const int Stage = 1;

        public const int Combat = 2;

        /// <summary>예약 번호입니다. 씬이 아직 없어 시작 대상으로 쓰지 않습니다.</summary>
        public const int Lobby = 3;

        /// <summary>예약 번호입니다. 씬이 아직 없어 시작 대상으로 쓰지 않습니다.</summary>
        public const int Artifact = 4;

        /// <summary>Build Settings에 실제로 등록하는 씬 경로입니다. 부트스트랩이 시작 전에 대조합니다.</summary>
        public const string BootPath = "Assets/Scenes/Boot.unity";

        /// <summary>
        /// 스테이지 맵 씬입니다(StageLifeCycle). 이름이 비슷한 옛 <c>Stage.unity</c>는 옛 전투 씬이며
        /// 새 구조에서는 Build Settings에 넣지 않고 참고용으로만 둡니다.
        /// </summary>
        public const string StagePath = "Assets/Scenes/StageSelect.unity";

        public const string CombatPath = "Assets/Scenes/Combat.unity";
    }
}
