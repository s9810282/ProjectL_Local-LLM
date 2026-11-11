namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Plans
{
    internal enum StageStepKind
    {
        None,
        /// <summary>끝난 전투 진입 시도를 닫습니다.</summary>
        CloseCombatEntry,
        /// <summary>전투 진입 시도가 확인 전에 끝나 진입 도메인을 멈춥니다.</summary>
        StopCombatEntry,
        /// <summary>제출된 증강 선택을 처리합니다.</summary>
        GrowthCommand,
        /// <summary>선택이 필요한데 제안이 없어 다음 증강 제안을 준비합니다.</summary>
        GrowthOffer,
        /// <summary>전투 노드의 맵 명령으로 전투 진입을 요청합니다.</summary>
        CombatEntry,
        /// <summary>전투가 아닌 노드의 맵 명령을 Core에 처리시킵니다.</summary>
        MapCommand,
        ShopCommand,
        RecoveryCommand,
        AbandonRun
    }

    /// <summary>Stage의 다음 할 일 하나입니다. <see cref="StagePlan.Decide"/>가 고릅니다.</summary>
    internal readonly struct StageStep
    {
        internal static readonly StageStep Nothing = new(StageStepKind.None, 0);

        internal StageStep(StageStepKind kind, int number = 0)
        {
            Kind = kind;
            Number = number;
        }

        internal StageStepKind Kind { get; }

        /// <summary>맵·상점 명령 번호입니다. 그 밖에는 0입니다.</summary>
        internal int Number { get; }

        public override string ToString() => Number > 0 ? $"{Kind}({Number})" : Kind.ToString();
    }
}
