using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item
{
    /// <summary>맵 표시 SO가 참조하는 공용 호버 스타일입니다. 조회마다 새 불변 값을 만듭니다.</summary>
    [CreateAssetMenu(fileName = "StageNodeHoverStyle", menuName = "ProjectL/Database/View/StageNodeHoverStyle")]
    internal sealed class StageNodeHoverStyleSO : ScriptableObject
    {
        [Header("Available Node Idle")]
        [Tooltip("호버하지 않은 진입 가능 건물의 기본 윤곽선 색상입니다. 불가능한 건물에는 표시하지 않습니다.")]
        [SerializeField, ColorUsage(false)] private Color idleOutlineColor = new(0.2f, 0.55f, 1f, 1f);
        [Tooltip("진입 가능 건물의 기본 윤곽선 두께입니다. 0이면 기본 윤곽선을 끕니다.")]
        [SerializeField, Min(0f)] private float idleOutlineWidth = 0.008f;

        [Header("Available Node")]
        [SerializeField, ColorUsage(false)] private Color availableOutlineColor = new(1f, 0.8f, 0.35f, 1f);
        [Tooltip("윤곽선 메시의 로컬 단위입니다. 0이면 해당 상태의 윤곽선은 표시하지 않습니다.")]
        [SerializeField, Min(0f)] private float availableOutlineWidth = 0.015f;

        [Header("Unavailable Node")]
        [SerializeField, ColorUsage(false)] private Color unavailableOutlineColor = new(0.45f, 0.48f, 0.52f, 1f);
        [SerializeField, Min(0f)] private float unavailableOutlineWidth = 0.006f;

        [Header("Available Connection")]
        [SerializeField, ColorUsage(false)] private Color connectionColor = new(1f, 0.8f, 0.35f, 1f);
        [Tooltip("선택 가능한 호버 노드로 향하는 선의 폭입니다. 맵 로컬 단위입니다.")]
        [SerializeField, Min(0.001f)] private float connectionWidth = 0.18f;

        [Header("Transition")]
        [Tooltip("호버 색상·두께가 바뀌는 시간(초)입니다. 0이면 즉시 바뀝니다. 변경 후 Play를 재시작합니다.")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.12f;

        internal StageNodeHoverStyleData GetData() => new(availableOutlineColor, availableOutlineWidth,
            unavailableOutlineColor, unavailableOutlineWidth, connectionColor, connectionWidth, transitionDuration,
            idleOutlineColor, idleOutlineWidth);
    }
}
