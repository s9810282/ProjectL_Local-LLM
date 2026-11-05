using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback
{
    /// <summary>아침 화면 전환의 요청값입니다. 값의 원본은 View DB 설정 표이고, 여기서는 레이어 이름을 마스크로 바꿉니다(View 3라운드 W9-1).</summary>
    internal sealed class CombatPhaseTransitionSettings
    {
        internal CombatPhaseTransitionSettings(ScreenTransitionData morning)
        {
            Morning = morning;
        }

        internal ScreenTransitionData Morning { get; }

        /// <summary>설정 표 값으로 아침(배치로 복귀) 전환을 만듭니다. 레이어가 없으면 예외입니다.</summary>
        /// <param name="findLayer">레이어 이름 → 번호입니다. 없으면 <see cref="LayerMask.NameToLayer"/>입니다(시험이 바꿈).</param>
        internal static CombatPhaseTransitionSettings From(CombatPresentationSettingsData settings, Func<string, int> findLayer = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            findLayer ??= LayerMask.NameToLayer;
            return new CombatPhaseTransitionSettings(
                Create(settings.MorningTransitionLayer, settings.MorningTransitionCamera, settings.MorningTransitionColor, settings, findLayer));
        }

        private static ScreenTransitionData Create(string layerName, string cameraName, Color color,
            CombatPresentationSettingsData settings, Func<string, int> findLayer)
        {
            int layer = findLayer(layerName);
            if (layer < 0 || layer > 31)
                throw new InvalidOperationException($"전투 화면 전환 레이어가 없습니다: {layerName}");
            return new ScreenTransitionData
            {
                Layer = 1 << layer,
                TargetVirtureCamera = cameraName,
                StencileMaskeColor = color,
                ChangeSpeed = settings.TransitionChangeSpeed,
                TargetChangeAmount = settings.TransitionChangeAmount
            };
        }
    }
}
