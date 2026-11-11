using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics
{
    /// <summary>계산기 안에서 시간을 재는 구간입니다. 표시 이름은 측정기를 설치하는 쪽이 정합니다.</summary>
    public enum CoreProfileSection
    {
        EnemyTurn,
        EnemyAttackAim,
        EnemyAimPreview,
        EnemyPursuitTarget,
        EnemyPathfind,
        EnemyCanAttackFrom,
        AttackRange,
        MovementUndoReconcile,
        EnemyPreparation,
        EnemyCandidates,
        EnemyHazardForecast,
        EnemyCheapScore,
        EnemyPreciseAssessment,
        EnemySourceValidation,
        EnemyActionExecution,
        AttackAdmission,
        AttackGeometry,
        AttackSelection,
        AttackProjectionSetup,
        AttackCommonDamage,
        AttackDelivery,
        AttackResultConstruction,
        CommandCompletion,
        AttackEffectBinding,
        TraitRegistryBuild,
        ProjectionCurrentInput,
        TraitPresenceReconcile,
        CommandFinalization,
        AttackSacrificePlanning,
        AttackForcedPath,
        AttackForcedPathStep,
        /// <summary>전투 첫 동작 Core 준비의 단계 하나입니다(<c>CombatFirstUseRehearsal.RunNext</c>).</summary>
        FirstUseRehearsal
    }

    /// <summary>구간 시작·끝을 받는 측정기입니다. 계산기는 Unity를 참조하지 않으므로 Unity Profiler 연결은 Systems가 구현합니다.</summary>
    public interface ICoreProfiler
    {
        void Begin(CoreProfileSection section);

        void End(CoreProfileSection section);
    }

    /// <summary>
    /// 계산기 측정 구간의 입구입니다. 측정기가 설치되지 않았으면 아무것도 하지 않습니다(시험·순수 실행).
    /// 계산 결과에는 영향을 주지 않습니다.
    /// </summary>
    public static class CoreProfiling
    {
        private static ICoreProfiler _profiler;

        /// <summary>측정기를 설치합니다. null이면 측정을 끕니다.</summary>
        public static void Install(ICoreProfiler profiler) => _profiler = profiler;

        /// <summary>구간을 시작하고, <c>using</c>이 끝날 때 닫습니다.</summary>
        public static Scope Sample(CoreProfileSection section)
        {
            ICoreProfiler profiler = _profiler;
            profiler?.Begin(section);
            return new Scope(profiler, section);
        }

        public readonly struct Scope : IDisposable
        {
            private readonly ICoreProfiler _profiler;
            private readonly CoreProfileSection _section;

            internal Scope(ICoreProfiler profiler, CoreProfileSection section)
            {
                _profiler = profiler;
                _section = section;
            }

            public void Dispose() => _profiler?.End(_section);
        }
    }
}
