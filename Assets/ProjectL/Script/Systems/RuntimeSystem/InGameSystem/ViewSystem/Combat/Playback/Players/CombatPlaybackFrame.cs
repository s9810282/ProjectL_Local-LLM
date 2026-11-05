namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 재생기에 넘기는 한 프레임의 맥락입니다(연출 조립 설계 B12). 재생기끼리의 사실(공격 진행 여부 등)은 넣지 않고,
    /// 필요한 재생기가 자기 출처에서 <see cref="ICombatPresentationPlayer.BeginFrame"/>에 기록합니다.
    /// </summary>
    internal readonly struct CombatPlaybackFrame
    {
        internal CombatPlaybackFrame(float deltaTime, bool mapperBound)
        {
            DeltaTime = deltaTime;
            MapperBound = mapperBound;
        }

        internal float DeltaTime { get; }

        /// <summary>필드 구성 뒤 Mapper가 연결돼 있는지입니다.</summary>
        internal bool MapperBound { get; }
    }
}
