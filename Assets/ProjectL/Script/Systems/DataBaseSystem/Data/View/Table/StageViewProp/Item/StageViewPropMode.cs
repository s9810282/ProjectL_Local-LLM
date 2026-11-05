namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>전투 배경을 세우는 방식입니다. 방식에 쓰지 않는 칸은 무시합니다.</summary>
    internal enum StageViewPropMode
    {
        /// <summary>프리팹(<c>gameObject</c>)만 세웁니다.</summary>
        Prefab = 0,

        /// <summary>맵 작업실의 맵 SO(<c>map</c>)로 세웁니다. 그 SO의 프리팹과 베이크 정보를 함께 씁니다.</summary>
        MapDefinition = 1,

        /// <summary>구멍 난 바닥 프리팹(<c>gameObject</c>)을 세우고, 구멍을 바닥 타일 배치(<c>mapTiles</c>)로 채웁니다.</summary>
        PrefabWithTiles = 2
    }
}
