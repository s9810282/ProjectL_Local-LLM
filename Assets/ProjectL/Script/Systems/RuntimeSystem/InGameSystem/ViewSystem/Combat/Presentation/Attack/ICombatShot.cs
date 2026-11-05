namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 출처 발사(클립 공격)로 쏜 투사체 하나의 회수 손잡이입니다(클립 설계 5차 AC12, 계획 T29b). 발사 조각이 취소될 때 그 투사체만 회수합니다.
    /// </summary>
    internal interface ICombatShot
    {
        /// <summary>아직 날고 있으면 투사체를 회수하고, 그 뒤로 맞음·도착 콜백을 부르지 않습니다. 이미 도착했거나 회수했으면 아무 일도 하지 않습니다.</summary>
        void Recall();
    }
}
