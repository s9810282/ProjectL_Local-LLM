using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>기존 숫자 템플릿과 Framework 풀을 조립합니다.</summary>
    internal static class CombatStructureDamageFactory
    {
        internal static CombatStructureDamage Create(UnitDamageText template, UnitHUDLayer layer, float screenScale)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            template.ValidateTemplate();
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (!UnitHUDProjection.IsValidBaseScale(Vector3.one * screenScale))
                throw new ArgumentOutOfRangeException(nameof(screenScale));
            var created = new List<UnitDamageText>();
            var pool = new CustomPool<UnitDamageText>(new PoolContext<UnitDamageText>(() =>
            {
                UnitDamageText widget = UnityEngine.Object.Instantiate(template, layer.transform, false);
                try
                {
                    widget.gameObject.SetActive(false);
                    widget.ValidateTemplate();
                    widget.transform.localScale = template.transform.localScale * screenScale;
                    created.Add(widget);
                    return widget;
                }
                catch
                {
                    Destroy(widget);
                    throw;
                }
            }, 0));
            return new CombatStructureDamage(pool, layer, () =>
            {
                foreach (UnitDamageText widget in created)
                    if (widget != null)
                        Destroy(widget);
                created.Clear();
            });
        }
        private static void Destroy(UnitDamageText widget)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(widget.gameObject);
            else UnityEngine.Object.DestroyImmediate(widget.gameObject);
        }
    }
}
