using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem.Item
{
    /// <summary>소모 아이템의 표시 외형을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(fileName = "NewConsumableItemVisualData", menuName = "ProjectL/Database/View/ConsumableItemVisualData")]
    internal sealed class ConsumableItemVisualDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField] private Input consumableItemVisualData = new();

        public override IData GetData() => consumableItemVisualData.ToData();

        [Serializable]
        private sealed class Input
        {
            [SerializeField, Min(1)] private int itemKey = 1;
            [SerializeField] private string displayName;
            [Tooltip("가방 칸과 유닛 HUD에 보일 아이콘입니다. 비우면 칸·HUD의 기본 그림을 씁니다.")]
            [SerializeField] private Sprite icon;
            [SerializeField, TextArea(2, 6)] private string description;
            [Tooltip("전투에서 아이템이 쓰일(소모될) 때의 효과음 프리셋 번호입니다. 효과가 겹쳐 수치 없이 소모만 될 때도 같은 소리를 냅니다. -1이면 소리를 내지 않습니다(소리 창구 2차 설계 ES1).")]
            [SerializeField, Min(-1)] private int itemSoundPreset = -1;
            [Tooltip("아이템이 쓰인 순간(효과가 겹쳐 소모만 된 경우 포함)의 이펙트·흔들림입니다. 시점은 Start(사건 순간)만, 이펙트 자리는 소지자 몸(SubjectBody)만 됩니다. "
                + "지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다(이펙트 창구 확장 설계 EC5).")]
            [SerializeField] private CombatVisualCueInput activationCues = new();

            internal ConsumableItemVisualData ToData()
            {
                CombatEventCues cues = (activationCues ?? throw new InvalidOperationException(
                    $"[ConsumableItemVisualData] {nameof(activationCues)} 칸이 없습니다. Key: {itemKey}")).ToCues();
                return new ConsumableItemVisualData(itemKey, displayName, icon, description, itemSoundPreset, cues);
            }
        }
    }
}
