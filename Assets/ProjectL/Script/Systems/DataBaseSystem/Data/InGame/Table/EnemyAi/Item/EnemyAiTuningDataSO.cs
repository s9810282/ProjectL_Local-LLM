using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.EnemyAi.Item
{
    /// <summary>EnemyAiTuningData의 입력 SO입니다. 테이블에 하나만 둡니다(설정형). 값은 게임 시작 때 한 번 읽으므로 다음 재생부터 적용됩니다.</summary>
    [CreateAssetMenu(fileName = "NewEnemyAiTuningData", menuName = "ProjectL/Database/InGame/EnemyAiTuningData")]
    internal sealed class EnemyAiTuningDataSO : DatabaseItem
    {
        // 점수는 '최대 체력의 %' 단위다: 공격으로 대상 최대 체력의 30%를 깎으면 +30%, 처치는 대상 최대 체력의 200%를 더 쳐 준다.
        // 세 값은 그 단위에 곱하는 비율이라 100이 기준과 같은 무게, 50이 절반이다. 의미 있는 범위는 대략 0~300이다.
        [Header("적 AI 교환 조정 (%, 100 = 기준과 같은 무게)")]
        [Tooltip("죽음 교환 기준 (기본 50, 범위 0~1000)\n" +
            "플레이어가 다음 턴에 이 적을 처치할 수 있는 칸(반격당하면 죽는 칸)을 고를 때 치르는 '목숨값'입니다. 대상 최대 체력의 %로 셉니다.\n" +
            "그 칸에서 얻는 것(대상 최대 체력 대비 피해 %, 처치면 +200%)이 이 값보다 커야 그 칸을 고릅니다.\n" +
            "· 0: 조금이라도 때릴 수 있으면 죽을 자리에도 들어갑니다(매우 무모).\n" +
            "· 50: 대상 최대 체력의 절반 이상을 깎거나 처치할 때만 목숨을 겁니다.\n" +
            "· 100: 대상 최대 체력만큼 깎을 때, 사실상 처치할 때만 겁니다. 그냥 찔러 보는 공격이 사라집니다.\n" +
            "· 200 이상: 처치할 때만 겁니다. 300 이상이면 처치로도 거의 걸지 않습니다.\n" +
            "· 1000: 반격당하면 죽는 칸을 사실상 고르지 않습니다.\n" +
            "높이면 신중해지고, 낮추면 희생을 각오하고 들어옵니다. 기믹·지형으로 죽는 칸은 이 값과 상관없이 고르지 않습니다.")]
        [SerializeField, Range(0, EnemyDecisionSettings.MaxTuningPercent)]
        private int deathTradeThresholdPercent = EnemyDecisionSettings.DefaultDeathTradeThresholdPercent;

        [Tooltip("위협 회피 (기본 50, 범위 0~1000)\n" +
            "반격으로 죽지는 않지만 맞을 수 있는 칸에서, 예상 피해(이 적 최대 체력의 %)에 이 비율을 곱해 손해로 뺍니다. 100이면 예상 피해를 그대로, 50이면 절반만 손해로 봅니다.\n" +
            "예: 플레이어가 이 적 최대 체력의 40%를 줄 수 있는 칸이면 25 → -10%, 50 → -20%, 100 → -40%. 이 손해는 공격으로 주는 피해와 같은 단위로 비교됩니다.\n" +
            "· 0: 죽지 않는 한 맞을 자리를 신경 쓰지 않고 돌진합니다.\n" +
            "· 25~50: 공격·접근을 우선하고, 비슷하면 덜 맞는 자리를 고릅니다.\n" +
            "· 100: 받는 피해와 주는 피해를 같은 무게로 봅니다. 손해 보는 교환은 피합니다.\n" +
            "· 200 이상: 매우 조심스럽습니다. 사거리 밖으로 한두 칸 물러나거나, 덜 맞는 자리(대각선 등)에서 머무는 일이 늘어납니다.\n" +
            "처치당할 수 있는 칸에는 이 값이 아니라 죽음 교환 기준을 씁니다.")]
        [SerializeField, Range(0, EnemyDecisionSettings.MaxTuningPercent)]
        private int threatAversionPercent = EnemyDecisionSettings.DefaultThreatAversionPercent;

        [Tooltip("다음 턴 접근 (기본 50, 범위 0~1000)\n" +
            "이동을 마친 칸에서 다음 턴에 공격 위치까지 갈 수 있고 대상을 지정할 수 있으면, '다음 턴 그 대상에게 줄 기본 공격 한 번의 가치'에 이 비율을 곱해 더합니다. " +
            "기본 공격 가치는 대상 최대 체력 대비 피해 %이고, 그 공격으로 처치할 수 있으면 +200%입니다.\n" +
            "예: 한 번에 대상 최대 체력의 30%를 깎는 적이면 50 → +15%, 100 → +30%, 150 → +45%.\n" +
            "· 0: 다음 턴 공격 자리를 따로 쳐 주지 않습니다. 진척(한 칸 +2%, 최대 +10%)으로만 천천히 다가가고, 대기가 늘어납니다.\n" +
            "· 50: 다가가는 것을 쳐 주되, 지금 때리는 것(100%)보다 낮게 봅니다.\n" +
            "· 100: 다음 턴 공격 자리를 지금 공격과 같은 무게로 봅니다.\n" +
            "· 100 초과: 사거리 안으로 들어가는 것을 더 높게 봐서, 맞을 위험을 무릅쓰고 빨리 다가옵니다. " +
            "다만 이미 칠 수 있는 자리에서는 공격과 대기가 같은 값을 받으므로, 공격 빈도 자체를 올리지는 않습니다.\n" +
            "처치할 수 있는 대상은 값이 크게 올라 그 대상 쪽으로 몰립니다.")]
        [SerializeField, Range(0, EnemyDecisionSettings.MaxTuningPercent)]
        private int nextTurnApproachPercent = EnemyDecisionSettings.DefaultNextTurnApproachPercent;

        // 연구 저장소 전용 연결(LLMTest/docs/2026-11-18-core-turn-planner-port-design.md CP2). 팀 저장소에는 없는 칸이다.
        [Header("적 AI 종류 (연구용)")]
        [Tooltip("Utility: 지금 게임 적 AI(기본).\n" +
            "TurnPlanner: 연구용 턴 계획 AI(Assets/ResearchEnemyPlanner). 적 턴 전체를 한 번에 계획하고, 위 세 조정값은 쓰지 않습니다.")]
        [SerializeField]
        private EnemyDecisionModel decisionModel = EnemyDecisionModel.Utility;

        public override IData GetData()
        {
            var data = new EnemyAiTuningData(deathTradeThresholdPercent, threatAversionPercent, nextTurnApproachPercent, decisionModel);
            data.Validate();
            return data;
        }
    }
}
