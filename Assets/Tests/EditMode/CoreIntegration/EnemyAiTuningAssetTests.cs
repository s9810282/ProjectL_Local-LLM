using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.EnemyAi.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>적 AI 기획 조정 에셋(설계 6.5.1, 계획 R-3)이 결정 설정까지 가고, 테이블이 없으면 기본값을 쓰는지 본다.</summary>
    public sealed class EnemyAiTuningAssetTests
    {
        [Test]
        public void AuthoredTuningReachesTheServiceAndMissingTableUsesDefaults()
        {
            var production = (IHasEnemyAiTuningTable)DataBaseAssetTests.ProductionInGame();
            Assert.That(production.EnemyAiTuning?.Tuning, Is.Not.Null);

            // 입력 SO의 값이 Data로 그대로 가고, 범위 밖이면 DB 생성에서 멈춘다.
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<EnemyAiTuningDataSO>();
            CoreAuthoringFixture.Set(asset, "deathTradeThresholdPercent", 30);
            CoreAuthoringFixture.Set(asset, "threatAversionPercent", 400);
            CoreAuthoringFixture.Set(asset, "nextTurnApproachPercent", 70);
            var edited = (EnemyAiTuningData)asset.GetData();
            Assert.That((edited.DeathTradeThresholdPercent, edited.ThreatAversionPercent, edited.NextTurnApproachPercent),
                Is.EqualTo((30, 400, 70)));
            CoreAuthoringFixture.Set(asset, "threatAversionPercent", EnemyDecisionSettings.MaxTuningPercent + 1);
            Assert.Throws<InvalidOperationException>(() => asset.GetData());

            // 서비스는 테이블 값으로 결정 설정을 만들고, 테이블이 없으면 계약 기본값을 쓴다.
            using var source = new EnemyDecisionSourceStampTests.SourceFixture();
            var tuned = Service(source);
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            tuned.InitData(new TunedContext(database.InGameQuery, edited));
            Assert.That((tuned.DecisionSettings.DeathTradeThresholdPercent, tuned.DecisionSettings.ThreatAversionPercent,
                tuned.DecisionSettings.NextTurnApproachPercent), Is.EqualTo((30, 400, 70)));
            var withoutTable = Service(source);
            withoutTable.InitData(database);
            Assert.That(withoutTable.DecisionSettings, Is.SameAs(EnemyDecisionSettings.Default));
        }

        private static CombatAdvanceService Service(EnemyDecisionSourceStampTests.SourceFixture source) =>
            new CombatAdvanceService(source.Assembler, source.Battle.Publication().Publisher,
                Array.Empty<IMovementTileRule>(), () => 0, () => 0, 1, usePreparation: true);

        /// <summary>운영 InGame 조회에 조정 테이블만 바꿔 끼운 DB 문맥이다.</summary>
        private sealed class TunedContext : IDataBaseQueryContext, IInGameQuery, IHasEnemyAiTuningTable, IEnemyAiTuningTableQuery
        {
            private readonly IInGameQuery _inner;

            internal TunedContext(IInGameQuery inner, EnemyAiTuningData tuning)
            {
                _inner = inner;
                Tuning = tuning;
            }

            public EnemyAiTuningData Tuning { get; }
            public IEnemyAiTuningTableQuery EnemyAiTuning => this;
            public IInGameQuery InGameQuery => this;
            public IRenderQuery RenderQuery => null;
            public IViewQuery ViewQuery => null;
            public IStageMapTableQuery StageMapTableQuery => _inner.StageMapTableQuery;
            public IStageTableQuery StageTableQuery => _inner.StageTableQuery;
            public IFieldTableQuery FieldTableQuery => _inner.FieldTableQuery;
            public IUnitTableQuery UnitTableQuery => _inner.UnitTableQuery;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => _inner.TargetingMethodTableQuery;
            public IStructureTableQuery StructureTableQuery => _inner.StructureTableQuery;
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => _inner.EnemyLoadoutTableQuery;
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _inner.PlayerLoadoutTableQuery;
            public IUnitRosterTableQuery UnitRosterTableQuery => _inner.UnitRosterTableQuery;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _inner.ArtifactDefinitionTableQuery;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => _inner.CategoryConfigTableQuery;
            public IStageEconomyTableQuery StageEconomyTableQuery => _inner.StageEconomyTableQuery;
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => _inner.RecoveryOptionTableQuery;
        }
    }
}
