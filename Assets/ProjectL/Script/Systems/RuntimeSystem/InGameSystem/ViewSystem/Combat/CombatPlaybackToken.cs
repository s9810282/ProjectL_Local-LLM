namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 재생 중인 결과 하나와 그 씬 연결 세대를 가리키는 호출용 값입니다(설계 §9.1).
    /// 완료 번호를 따로 저장하는 카운터가 아닙니다. 기본값은 어떤 재생도 가리키지 않습니다.
    /// </summary>
    internal readonly struct CombatPlaybackToken
    {
        internal CombatPlaybackToken(int generation, int sequence)
        {
            Generation = generation;
            Sequence = sequence;
        }

        /// <summary>발급 때의 연결 세대입니다. Attach·Abort마다 바뀝니다.</summary>
        internal int Generation { get; }

        /// <summary>재생 중인 결과의 순번입니다. 0이면 빈 토큰입니다.</summary>
        internal int Sequence { get; }
    }
}
