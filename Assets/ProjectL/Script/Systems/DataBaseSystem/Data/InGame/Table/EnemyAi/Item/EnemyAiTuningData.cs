using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.EnemyAi.Item
{
    /// <summary>
    /// 적 AI 교환 모형의 기획 조정값입니다(설정형, 테이블에 하나). Core가 받는 순수 C# 데이터이며 EnemyAiTuningDataSO가 생성합니다.
    /// </summary>
    /// <remarks>
    /// 값의 범위와 기본값은 Core 결정 설정 계약(<see cref="EnemyDecisionSettings"/>)이 가집니다. 테이블이 없으면 그 기본값을 씁니다.
    /// </remarks>
    public sealed class EnemyAiTuningData : IData
    {
        /// <summary>목숨을 걸 만한 최소 피해(대상 최대 체력의 %)입니다.</summary>
        public int DeathTradeThresholdPercent { get; }
        /// <summary>처치당하지 않는 칸의 예상 피해를 이동 점수에서 빼는 비율(%)입니다.</summary>
        public int ThreatAversionPercent { get; }
        /// <summary>다음 기회에 칠 수 있는 자리의 값 비율(%)입니다.</summary>
        public int NextTurnApproachPercent { get; }
        /// <summary>적 행동을 고르는 방식입니다(연구 저장소 전용 연결, 기본은 지금 AI).</summary>
        public EnemyDecisionModel DecisionModel { get; }

        public EnemyAiTuningData(int deathTradeThresholdPercent, int threatAversionPercent, int nextTurnApproachPercent,
            EnemyDecisionModel decisionModel = EnemyDecisionModel.Utility)
        {
            DeathTradeThresholdPercent = deathTradeThresholdPercent;
            ThreatAversionPercent = threatAversionPercent;
            NextTurnApproachPercent = nextTurnApproachPercent;
            DecisionModel = decisionModel;
        }

        /// <summary>조정값을 담은 결정 설정입니다. 평가 한도는 계약 기본값입니다.</summary>
        public EnemyDecisionSettings ToSettings() => new EnemyDecisionSettings(
            deathTradeThresholdPercent: DeathTradeThresholdPercent,
            threatAversionPercent: ThreatAversionPercent,
            nextTurnApproachPercent: NextTurnApproachPercent,
            decisionModel: DecisionModel);

        public void Validate()
        {
            try
            {
                ToSettings();
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException(
                    $"[EnemyAiTuningData] {exception.ParamName} must be in 0..{EnemyDecisionSettings.MaxTuningPercent}.", exception);
            }
        }
    }
}
