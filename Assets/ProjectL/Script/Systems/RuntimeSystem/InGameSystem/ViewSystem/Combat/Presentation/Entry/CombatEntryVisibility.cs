using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>진입 연출 동안만 Renderer 표시를 억제합니다. GameObject 활성과 원래 숨김은 바꾸지 않습니다.</summary>
    internal sealed class CombatEntryVisibility
    {
        private readonly Renderer[] _renderers;
        private readonly bool[] _enabled;
        private bool _hidden;

        internal CombatEntryVisibility(Transform target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            _renderers = target.GetComponentsInChildren<Renderer>(true);
            _enabled = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _enabled[i] = _renderers[i].enabled;
        }

        internal void Hide()
        {
            if (_hidden) return;
            _hidden = true;
            foreach (Renderer renderer in _renderers)
                if (renderer != null) renderer.enabled = false;
        }

        internal void Restore()
        {
            if (!_hidden) return;
            _hidden = false;
            List<Exception> errors = null;
            for (int i = 0; i < _renderers.Length; i++)
            {
                try
                {
                    if (_renderers[i] != null) _renderers[i].enabled = _enabled[i];
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryVisibility] 진입 표시 복구에 실패했습니다.");
        }
    }
}
