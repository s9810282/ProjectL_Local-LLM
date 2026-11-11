using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 행동 버튼을 맞출 때 연결 입력 묶음에서 읽는 입력 쪽 사실 넷입니다(조정자 나누기 설계 CS2).
    /// 연결 입력 묶음(<see cref="CombatSceneInput.ReadActionButtons"/>)이 만들고, 입력 분배(<c>CombatSceneInputDriver.SyncActionButtons</c>)가 입력 표면(<see cref="CombatInputSurfaces.Sync"/>)에 넘깁니다.
    /// </summary>
    /// <remarks>
    /// 매 Tick·입력마다 만들므로 값 형식입니다(계획 TP-5). 생성자 인자 순서는 묶음이 읽는 순서(공격 진입 가능 → 공격 중 → 되돌리기 가능 → 가용성)와 같습니다.
    /// 되돌리기 가능은 입력 사실을 다시 읽으므로 이 순서를 지킵니다.
    /// </remarks>
    internal readonly struct CombatActionButtonState
    {
        internal CombatActionButtonState(bool canEnterAttack, bool isAttackActive, bool canUseReturn, CombatActionAvailability availability)
        {
            CanEnterAttack = canEnterAttack;
            IsAttackActive = isAttackActive;
            CanUseReturn = canUseReturn;
            Availability = availability;
        }

        /// <summary>지금 공격 모드에 들어갈 수 있는지입니다.</summary>
        internal bool CanEnterAttack { get; }

        /// <summary>공격 모드인지입니다.</summary>
        internal bool IsAttackActive { get; }

        /// <summary>이동 되돌리기를 요청할 수 있는지입니다.</summary>
        internal bool CanUseReturn { get; }

        /// <summary>선택 아군의 Core 가용성 답입니다. 입력이 닫혔거나 아직 답이 없으면 null입니다.</summary>
        internal CombatActionAvailability Availability { get; }
    }
}
