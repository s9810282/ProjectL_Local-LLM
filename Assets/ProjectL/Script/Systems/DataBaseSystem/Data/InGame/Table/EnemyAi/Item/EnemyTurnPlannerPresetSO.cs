using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.EnemyAi.Item
{
    /// <summary>
    /// 턴 계획 AI의 성향 프리셋입니다(연구 저장소 전용, LLMTest/docs/2026-11-18-core-turn-planner-port-design.md §11).
    /// 조정 에셋(EnemyAiTuningDataSO)의 칸에 넣어 씁니다. DB 테이블 항목이 아니라 조정 에셋이 참조하는 입력 SO이며,
    /// 조정 에셋의 GetData()가 이 값을 순수 값 묶음(<see cref="EnemyTurnPlannerStyle"/>)으로 바꿔 Core에 넘깁니다.
    /// 기본값은 연구 시뮬레이터 plan4:g1:l1과 같습니다. 복제해서 성향마다 하나씩 만들면 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyTurnPlannerPreset", menuName = "ProjectL/Database/InGame/EnemyTurnPlannerPreset (연구용)")]
    internal sealed class EnemyTurnPlannerPresetSO : ScriptableObject
    {
        /// <summary>판 점수 식(조정 에셋과 이 프리셋의 인스펙터에 설명 상자로 보인다). 식의 원본은 Assets/ResearchEnemyPlanner/PlannerEvaluator.cs·PlannerReply.cs다.</summary>
        internal const string Formula =
            "턴 계획 AI 판 점수 (적 입장, 클수록 좋은 판)\n" +
            "V = 적 전력 − 플레이어 전력 − 겁 × 위험 + 기회 × 기회 − 걸음 벌점 − 메인 거리 벌점\n\n" +
            "· 전력 = 필드에 산 유닛마다 공격력 × 남은 HP의 합. 메인 캐릭터는 × 메인 배수.\n" +
            "   적 전력에는 본진(적 캐릭터 전원의 공격력 × 최대 HP 합 × 본진 남은 HP 비율)과 아직 안 나온 증원(× 증원 할인)도 들어갑니다.\n" +
            "· 위험 = 다음 플레이어 턴에 플레이어 유닛마다 우리에게 가장 아픈 공격 하나를 친다고 볼 때 우리가 잃는 전력(대상마다 피해를 더해 HP에서 자름).\n" +
            "· 기회 = 다음 적 턴에 우리 유닛마다 가장 좋은 공격 하나로 빼앗는 전력.\n" +
            "· 걸음 벌점 = 칠 대상이 아무도 닿지 않는 우리 유닛마다, 모자란 걸음 × 걸음 벌점.\n" +
            "· 메인 거리 벌점 = 다음 턴에 메인을 칠 수 없는 우리 유닛마다, 모자란 걸음 × 메인 거리 벌점(%) × 메인 가치 ÷ 100.\n" +
            "   메인 가치 = 메인 공격력 × 최대 HP × 메인 배수.\n\n" +
            "계획: 적 행동을 하나씩 정하며 V가 큰 판을 '빔' 개만 남겨 이어 봅니다. 유닛 하나에서는 V 상위 '유닛당 후보' 개를 펼치고,\n" +
            "   이동만 하는 행동은 먼저 빠른 이동 점수(기회 × 그 칸에서 칠 수 있는 것 − 겁 × 그 칸에서 맞을 피해 − 메인 거리 벌점) 상위 '이동만 후보' 개로 거릅니다.\n" +
            "   끝난 계획 중 V 상위 '다시 매김' 개를 깊게 다시 봐서 가장 좋은 것을 고릅니다.\n" +
            "· 한 턴 더 보기 켬: 깊게 보기 = 플레이어 턴을 엔진으로 끝까지 둬 본 뒤(다시 적 차례) 판의 V.\n" +
            "   플레이어는 유닛 번호 순으로 그때 가장 이득인 공격 하나를 하고, 이득 나는 공격이 없으면 제자리에 있다고 봅니다.\n" +
            "· 끔: 깊게 보기 = 위 V에서 위험·기회·걸음 벌점을 엔진으로 둬 본 값으로 바꾼 것.";

        [InspectorNote(Formula)]
        [Header("성향")]
        [Tooltip("겁 (기본 0.5, 0 이상, 시뮬레이터 :f)\n" +
            "위험(다음 플레이어 턴에 우리가 잃을 전력)에 곱합니다.\n" +
            "· 0: 맞는 것을 신경 쓰지 않고 들어갑니다.\n" +
            "· 높이면: 맞을 자리를 피하고 사거리 밖에서 버팁니다. 대략 0~2에서 고르는 것으로 봅니다(추정).")]
        [SerializeField, Min(0f)]
        private float fear = (float)EnemyTurnPlannerStyle.DefaultFear;

        [Tooltip("기회 (기본 0.5, 0 이상, 시뮬레이터 :o)\n" +
            "기회(다음 적 턴에 우리 유닛이 빼앗을 전력)에 곱합니다.\n" +
            "· 0: 다음 턴 공격 자리를 따로 쳐 주지 않습니다.\n" +
            "· 높이면: 다음 턴에 칠 수 있는 자리로 앞으로 나와 자리를 잡습니다.")]
        [SerializeField, Min(0f)]
        private float opportunity = (float)EnemyTurnPlannerStyle.DefaultOpportunity;

        [Tooltip("메인 배수 (기본 3, 0 이상)\n" +
            "메인 캐릭터의 전력과 메인이 입는·주는 피해를 이 배수로 셉니다. 메인을 잡으면 이기므로,\n" +
            "· 높이면: 메인을 더 집요하게 노리고, 메인의 공격을 더 피합니다.\n" +
            "· 1: 메인을 다른 유닛과 같게 봅니다.\n" +
            "메인 거리 벌점의 크기(메인 가치)에도 들어갑니다.")]
        [SerializeField, Min(0f)]
        private float mainWeight = (float)EnemyTurnPlannerStyle.DefaultMainWeight;

        [Tooltip("메인 거리 벌점 % (기본 1, 0 이상, 시뮬레이터 :g)\n" +
            "다음 턴에 메인을 칠 수 없는 적 유닛마다, 모자란 걸음 하나에 메인 가치의 이 %를 뺍니다.\n" +
            "· 0: 끔. 메인 쪽으로 따로 끌리지 않습니다.\n" +
            "· 높이면: 메인 쪽으로 더 빨리 몰려갑니다.")]
        [SerializeField, Min(0f)]
        private float goalStepPercent = (float)EnemyTurnPlannerStyle.DefaultGoalStepPercent;

        [Tooltip("걸음 벌점 (기본 10, 0 이상, 전력 단위)\n" +
            "칠 대상이 아무도 닿지 않는 적 유닛의 모자란 걸음 하나에 빼는 값입니다(전력 = 공격력 × HP 단위).\n" +
            "· 높이면: 할 일 없는 유닛이 더 빨리 다가옵니다.\n" +
            "· 0: 닿지 않는 유닛을 끌어당기지 않습니다(제자리가 늘어남).")]
        [SerializeField, Min(0f)]
        private float stepPenalty = (float)EnemyTurnPlannerStyle.DefaultStepPenalty;

        [Tooltip("증원 할인 (기본 0.5, 0 이상)\n" +
            "아직 나오지 않은 증원을 이 비율로 적 전력에 셉니다. 본진이 살아 있을 때만 셉니다.\n" +
            "· 높이면: 본진(남은 증원)을 더 지키려 합니다.")]
        [SerializeField, Min(0f)]
        private float reinforcementDiscount = (float)EnemyTurnPlannerStyle.DefaultReinforcementDiscount;

        [Tooltip("한 턴 더 보기 (기본 켬, 시뮬레이터 :l1)\n" +
            "켬: 깊게 볼 때 플레이어 턴 전체를 차례대로 둬 본 뒤 판으로 매깁니다(두 수 읽기). 함께 노리고, 칠 수 없으면 무리하게 들어가지 않는 쪽입니다.\n" +
            "끔: 옛 방식(플레이어 유닛마다 따로 친다고 보는 위험·기회).")]
        [SerializeField]
        private bool lookahead = EnemyTurnPlannerStyle.DefaultLookahead;

        [Header("계산 크기 (계획 시간에 영향)")]
        [Tooltip("빔 (기본 4, 1 이상)\n" +
            "판 = 전투를 한 순간 멈춘 상태(유닛 위치·HP·행동 여부 등)입니다. 적 유닛 하나가 행동할 때마다, 남아 있는 판들에서\n" +
            "아직 안 움직인 유닛을 움직여 본 새 판이 생깁니다(유닛마다 '유닛당 후보' 개, 결과가 같은 판은 하나로 합침).\n" +
            "그중 점수(V)가 높은 판을 이 개수만큼만 남겨 다음 유닛 차례로 이어 봅니다. 나머지 판은 버립니다.\n" +
            "· 1: 매번 가장 좋아 보이는 판 하나만 따라갑니다(지금 좋아 보여도 나중에 나쁜 길을 못 피할 수 있음).\n" +
            "· 키우면: 더 많은 판을 이어 봐 더 좋은 계획을 찾을 수 있지만, 남긴 판마다 다시 다 계산하므로 계획 시간이 거의 그만큼 늘어납니다.")]
        [SerializeField, Min(1)]
        private int beam = EnemyTurnPlannerStyle.DefaultBeam;

        [Tooltip("유닛당 후보 (기본 6, 1 이상)\n" +
            "유닛 하나의 후보(공격이 든 행동 전부 + '이동만 후보')를 모두 엔진으로 돌려 V를 매긴 뒤, 다음 판으로 남기는 V 상위 개수입니다.\n" +
            "그 판에서 할 수 있는 행동 수가 아니라 남기는 수입니다. 후보는 이 값과 상관없이 모두 돌려 보므로,\n" +
            "키워도 시간은 조금만 늘어납니다(남긴 판도 어차피 '빔' 개로 다시 잘림). 키우면 빔에 오르는 후보가 더 다양해집니다.")]
        [SerializeField, Min(1)]
        private int perUnit = EnemyTurnPlannerStyle.DefaultPerUnit;

        [Tooltip("이동만 후보 (기본 3, 0 이상)\n" +
            "공격 없이 이동만 하는 행동 중 빠른 이동 점수 상위 몇 개를 펼쳐 보나입니다. 0이면 이동만 하는 행동을 보지 않습니다.")]
        [SerializeField, Min(0)]
        private int moveOnly = EnemyTurnPlannerStyle.DefaultMoveOnly;

        [Tooltip("다시 매김 (기본 4, 0 이상)\n" +
            "끝난 계획 중 V 상위 몇 개를 깊게 다시 보나입니다. 2보다 작으면 깊게 보지 않아 한 턴 더 보기도 쓰이지 않습니다.\n" +
            "키우면 시간이 늘어납니다(한 개에 플레이어 턴 하나를 엔진으로 둬 봄).")]
        [SerializeField, Min(0)]
        private int rerank = EnemyTurnPlannerStyle.DefaultRerank;

        /// <summary>프리셋 값을 Core가 받는 값 묶음으로 바꿉니다. 범위 밖이면 DB 생성에서 멈춥니다.</summary>
        internal EnemyTurnPlannerStyle ToStyle()
        {
            try
            {
                return new EnemyTurnPlannerStyle(fear, opportunity, mainWeight, goalStepPercent, stepPenalty, reinforcementDiscount,
                    lookahead, beam, perUnit, moveOnly, rerank);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new InvalidOperationException($"[EnemyTurnPlannerPreset {name}] {exception.ParamName} 값이 범위를 벗어났습니다.", exception);
            }
        }
    }

    /// <summary>인스펙터에서 칸 위에 읽기 전용 설명 상자를 그립니다(값은 저장하지 않습니다). 연구 저장소 전용.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class InspectorNoteAttribute : PropertyAttribute
    {
        internal string Text { get; }

        internal InspectorNoteAttribute(string text)
        {
            Text = text;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// <see cref="InspectorNoteAttribute"/>의 설명 상자. UI Toolkit 인스펙터(Unity 6 기본)는 <see cref="CreatePropertyGUI"/>의 요소를 그리고,
    /// IMGUI 인스펙터만 <see cref="GetHeight"/>·<see cref="OnGUI"/>를 부른다. GUI 함수(EditorStyles 등)는 OnGUI 안에서만 부를 수 있으므로
    /// 높이는 OnGUI 밖이면 줄 수로 어림한다.
    /// </summary>
    [UnityEditor.CustomPropertyDrawer(typeof(InspectorNoteAttribute))]
    internal sealed class InspectorNoteDrawer : UnityEditor.DecoratorDrawer
    {
        private const float Gap = 4f;

        private string Text => ((InspectorNoteAttribute)attribute).Text;

        public override UnityEngine.UIElements.VisualElement CreatePropertyGUI()
        {
            var box = new UnityEngine.UIElements.HelpBox(Text, UnityEngine.UIElements.HelpBoxMessageType.None);
            box.style.marginTop = Gap;
            box.style.marginBottom = Gap;
            return box;
        }

        public override float GetHeight()
        {
            // UI Toolkit 인스펙터는 이 함수를 OnGUI 밖에서도 부른다(PropertyField.ResetDecoratorDrawers).
            // 그때 GUI 함수(currentViewWidth·EditorStyles)는 예외를 내므로, 줄 수로 어림한 높이를 쓴다.
            try
            {
                float width = Mathf.Max(100f, UnityEditor.EditorGUIUtility.currentViewWidth - 40f);
                return UnityEditor.EditorStyles.helpBox.CalcHeight(new GUIContent(Text), width) + Gap * 2;
            }
            catch (ArgumentException)
            {
                int lines = 1;
                foreach (char c in Text)
                    if (c == '\n') lines++;
                return lines * UnityEditor.EditorGUIUtility.singleLineHeight + Gap * 2;
            }
        }

        public override void OnGUI(Rect position)
        {
            position.yMin += Gap;
            position.yMax -= Gap;
            UnityEditor.EditorGUI.HelpBox(position, Text, UnityEditor.MessageType.None);
        }
    }
#endif
}
