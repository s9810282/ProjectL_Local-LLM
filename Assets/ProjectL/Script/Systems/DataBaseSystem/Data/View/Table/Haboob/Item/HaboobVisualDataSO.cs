using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob.Item
{
    /// <summary>
    /// 하붑의 표현 값입니다(하붑 View 계획 HP-15·HP-18). 칸·단계는 Core 값이라 포함하지 않습니다. 구름은 칸마다 이펙트 하나라 모양·크기 값이 없습니다.
    /// </summary>
    internal sealed class HaboobVisualData : IData
    {
        /// <summary>구름 칸마다 내는 이펙트입니다. 칸이 구름에서 빠질 때까지 남습니다.</summary>
        internal CombatHeldEffect CloudEffect { get; }

        /// <summary>구름이 새 자리로 미끄러지는 시간(초)입니다. 거리와 상관없이 같습니다.</summary>
        public float RelocationSeconds { get; }

        /// <summary>구름이 다른 칸으로 옮겨질 때의 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int RelocationSoundPreset { get; }

        /// <summary>구름 칸 호버 설명의 이름입니다.</summary>
        public string DisplayName { get; }

        /// <summary>구름 칸 호버 설명입니다.</summary>
        public string Description { get; }

        /// <exception cref="InvalidOperationException">낼 수 없는 이펙트 종류이거나, 이동 시간이 0 이하이거나, 소리 프리셋이 범위를 벗어날 때입니다.</exception>
        internal HaboobVisualData(EffectType cloudEffect, Vector3 cloudPositionOffset, Vector3 cloudRotationOffset,
            float relocationSeconds, int relocationSoundPreset, string displayName, string description)
        {
            if (!CombatPresentationEffectRules.IsPlayable(cloudEffect))
                throw new InvalidOperationException($"[HaboobVisualData] 구름 이펙트(cloudEffect): {CombatPresentationEffectRules.PlayableRule} 값: {cloudEffect}");
            if (!(relocationSeconds > 0f))
                throw new InvalidOperationException($"[HaboobVisualData] 이동 시간(relocationSeconds)은 0보다 커야 합니다. 값: {relocationSeconds}");
            CombatPresentationSettingsData.ValidateSoundPreset(relocationSoundPreset, nameof(relocationSoundPreset));

            CloudEffect = new CombatHeldEffect(cloudEffect, cloudPositionOffset, cloudRotationOffset);
            RelocationSeconds = relocationSeconds;
            RelocationSoundPreset = relocationSoundPreset;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }

    [CreateAssetMenu(fileName = "NewHaboobVisualData", menuName = "ProjectL/Database/View/HaboobVisualData")]
    internal sealed class HaboobVisualDataSO : DatabaseItem
    {
        [Header("구름")]
        [Tooltip("구름 칸마다 내는 이펙트입니다. Render DB 이펙트 표에 등록돼 있어야 합니다. 칸이 구름에서 빠질 때까지 남아야 하므로 반복(Loop) 파티클이 맞습니다. 필수이며, Empty면 DB를 만들 때 실패합니다.")]
        [SerializeField] private EffectType cloudEffect = EffectType.Smoke;
        [Tooltip("칸 표면 자리 기준 위치 보정입니다. x = 필드 오른쪽, y = 위, z = 필드 앞이고 월드 단위입니다. 0이면 칸 표면 가운데입니다.")]
        [SerializeField] private Vector3 cloudPositionOffset;
        [Tooltip("필드 회전 뒤에 더하는 회전입니다(오일러 각, 도). 0이면 필드 회전 그대로입니다.")]
        [SerializeField] private Vector3 cloudRotationOffset;
        [Tooltip("구름이 새 자리로 미끄러지는 시간(초)입니다. 거리와 상관없이 같고, 도착할 때까지 다음 연출을 기다립니다. 0보다 커야 합니다.")]
        [SerializeField, Min(0.01f)] private float relocationSeconds = 1f;

        [Header("소리")]
        [Tooltip("구름이 다른 칸으로 옮겨질 때의 효과음 프리셋 번호입니다. 칸마다 내지 않고 한 번만 냅니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int relocationSoundPreset = -1;

        [Header("호버 설명")]
        [SerializeField] private string displayName;
        [Tooltip("구름 칸 호버 설명입니다. 지정 규칙이 정적 모래폭풍(사거리 감소)과 다르니 구분해 적습니다.")]
        [TextArea(2, 6)]
        [SerializeField] private string description;

        public override IData GetData() => new HaboobVisualData(cloudEffect, cloudPositionOffset, cloudRotationOffset,
            relocationSeconds, relocationSoundPreset, displayName, description);
    }
}
