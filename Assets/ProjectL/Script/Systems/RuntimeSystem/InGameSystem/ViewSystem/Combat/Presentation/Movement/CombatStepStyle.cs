using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걸음 하나의 모양입니다(걸음 모양 설계 ST2). 모양마다 다른 지식 셋(걸음 시간·높이·접촉 시점)을 파생 클래스 하나가 갖습니다.
    /// 걸음 레인(<see cref="CombatUnitMotionPlayback"/>)과 배치 대여는 고른 모양에 묻기만 하고 어떤 모양인지 모릅니다.
    /// 어느 이동 종류가 어느 모양을 쓰는지는 모양 표 <see cref="CombatStepStyles"/> 한 곳이 정합니다.
    /// </summary>
    /// <remarks>파생 클래스는 상태 없는 불변 객체입니다. 멤버는 걸음 진행 중 매 프레임 불리므로 할당과 예외가 없어야 합니다.</remarks>
    internal abstract class CombatStepStyle
    {
        /// <summary>걸음 하나(한 칸)의 보간 시간입니다(초, 유한한 양수). 걸음 전 회전 시간은 들어 있지 않습니다.</summary>
        internal abstract float Seconds { get; }

        /// <summary>
        /// 보간 진행률(0~1)의 높이입니다(필드 로컬 단위). 두 칸의 표면 높이를 받습니다. 가로 위치는 배치 대여가 직선으로 잇습니다.
        /// 높이 식의 원본은 <see cref="CombatStepHeight"/>입니다.
        /// </summary>
        internal abstract float HeightAt(float fromHeight, float toHeight, float progress);

        /// <summary>
        /// 몸과 관계없이 모든 접촉 원본이 같은 진행률(0~1)에 닿는 모양이면 참과 그 진행률입니다. 참이면 레인이 몸을 구하지 않고
        /// <see cref="ContactProgress"/>도 부르지 않습니다. 거짓이면 묶음마다 <see cref="ContactProgress"/>로 정합니다.
        /// </summary>
        internal abstract bool TryGetFixedContact(out float progress);

        /// <summary>이동자 몸이 접촉 원본에 닿는 보간 진행률(0~1)입니다. 닿지 않으면 1(걸음 끝)입니다(이동 접촉 설계 M2). 고정 접촉 모양은 부르지 않습니다.</summary>
        /// <param name="moverStart">걸음 시작 칸에서 보간 중 표시 방향(걸음 방향으로 돈 뒤)의 이동자 몸입니다.</param>
        /// <param name="moverEndCenter">같은 방향으로 걸음 끝 칸에 놓인 이동자 몸의 중심입니다.</param>
        /// <param name="source">접촉 원본(가시·오브젝트)의 몸입니다.</param>
        internal abstract float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source);
    }
}
