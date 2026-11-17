using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 유닛 실행 하나가 그 주체의 어떤 사건을 막지 않거나, 어떤 사건에 끊기는지의 규칙입니다(피격 구조 정리 설계 HC3).
    /// 조립이 <see cref="CombatComposition.PassSubject"/>로 하나를 들고 오고, 조각 실행 재생기는 이 규칙에 묻기만 합니다(재생기에 사건 지식 갈래를 두지 않음).
    /// </summary>
    /// <remarks>
    /// 막기 판정에서 통과하면(<see cref="Passes"/>) 그 사건은 이 실행이 끝나기 전에 시작합니다. 그 사건이 이 실행과 같은 채널을 빌리면
    /// 사건을 시작하는 쪽이 먼저 끊어야 하므로(<see cref="IsCutBy"/>), 통과만 하고 끊지 않는 규칙은 같은 채널을 빌리지 않는 사건만 통과시켜야 합니다.
    /// 구현은 사건 지식이 필요해 <c>Playback/Support/</c>에 둡니다. 필드가 없는 객체라 클래스마다 <c>Instance</c> 하나를 함께 씁니다(설계 P2).
    /// </remarks>
    internal interface ICombatRunSubjectPass
    {
        /// <summary>이 실행이 그 주체의 이 사건을 막지 않는지입니다. 사건의 주체가 이 실행의 주체인지는 재생기가 먼저 봅니다.</summary>
        bool Passes(CombatEvent item);

        /// <summary>이 사건이 시작하며 이 실행을 끊는지입니다. 끊긴 실행은 대여를 되돌리고 완료를 알리지 않습니다.</summary>
        bool IsCutBy(CombatEvent item);

        /// <summary>
        /// 이 규칙의 실행이 끊길 수 있는지입니다. 끊기면 완료를 알리지 못하므로 재생기가 완료 통지가 있는 시작을 거절합니다.
        /// <see cref="IsCutBy"/>만으로는 시작할 때 미리 알 수 없어 따로 둡니다(계획 §5 R2).
        /// </summary>
        bool Cuttable { get; }

        /// <summary>이 규칙의 실행이 자세 채널을 빌리면 안 되는지입니다(걸음 배치와 겹치는 실행).</summary>
        bool ForbidsPose { get; }
    }
}
