namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans
{
    /// <summary>전투 진행 판단이 고른 다음 단계의 종류입니다(재구성 설계 J2).</summary>
    /// <remarks>
    /// 단계마다 만드는 Job은 한 번 실행으로 끝나거나 예외를 냅니다(다음 Tick까지 기다리지 않음). <see cref="Jobs.CombatNextJob"/>이
    /// 판단한 뒤 자기 안에서 바로 이어 실행하므로, 기다리는 단계 Job은 허용하지 않습니다(끼면 판단 때의 사실이 낡음,
    /// 개선 설계 §5.1, 골격 점검 설계 K-1). 한 번 실행으로 끝나지 않은 단계 Job은 <see cref="Jobs.CombatNextJob"/>이 예외로 멈춥니다.
    /// 기다림은 Job이 아니라 <see cref="CombatPlan.Decide"/>의 조건이 맡습니다.
    /// </remarks>
    internal enum CombatStepKind
    {
        /// <summary>지금은 할 일이 없습니다. 다음 Tick에 다시 판단합니다.</summary>
        None = 0,
        SuspendSubmissions = 1,
        /// <summary>EndBattle → (보스 승리면) 챕터 완료 → 완료 표시 목록을 한 번에 만들고, <see cref="Jobs.CombatNextJob"/>이 차례로 실행합니다. 모든 결과가 같은 순서입니다.</summary>
        Settle = 2,
        /// <summary>사용자 확인 뒤 Run 결과로 정한 목적지로 전환합니다.</summary>
        Leave = 3,
        Abandon = 4,
        Command = 5,
        Preview = 6,
        Advance = 7,
        PrepareAdvance = 8,
        PreparedAdvance = 9
    }

    /// <summary>다음 단계와 그 단계가 확인할 번호입니다. 명령은 N, 미리보기는 요청 번호, 자동 진행은 판단 때의 결과 순번 S입니다.</summary>
    internal readonly struct CombatStep
    {
        internal static readonly CombatStep Nothing = new(CombatStepKind.None, 0);

        internal CombatStep(CombatStepKind kind, int number)
        {
            Kind = kind;
            Number = number;
        }

        internal CombatStepKind Kind { get; }

        internal int Number { get; }

        internal static CombatStep Of(CombatStepKind kind) => new(kind, 0);

        public override string ToString() => Number == 0 ? Kind.ToString() : $"{Kind}({Number})";
    }
}
