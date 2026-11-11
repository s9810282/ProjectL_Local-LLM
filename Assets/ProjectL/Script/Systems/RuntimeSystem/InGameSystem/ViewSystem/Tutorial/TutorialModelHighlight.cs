using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>현재 설명의 모델 대여만 소유합니다. 대상 선정·전투 선택·단계 판단은 하지 않습니다.</summary>
    internal sealed class TutorialModelHighlight
    {
        private readonly List<Entry> _entries = new();

        internal void Sync(IReadOnlyList<TutorialModelFocus> targets, TutorialHighlightStyle style)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (style == null) throw new ArgumentNullException(nameof(style));
            style.Validate();
            foreach (TutorialModelFocus target in targets)
                if (target.IsCurrent) _ = style.GetModelColor(target.Owner);
            List<Exception> errors = null;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                if (entry.Lease.IsAvailable && Contains(targets, entry.Target)
                    && entry.AppliedColor.Equals(style.GetModelColor(entry.Target.Owner))
                    && entry.AppliedWidth == style.ModelWidth) continue;
                _entries.RemoveAt(i);
                CleanupErrors.Try(entry.Lease.Dispose, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[TutorialModelHighlight] 지난 모델 강조 반환에 실패했습니다.");
            try
            {
                foreach (TutorialModelFocus target in targets)
                {
                    if (!target.IsCurrent || Contains(target)) continue;
                    Color color = style.GetModelColor(target.Owner);
                    float width = style.ModelWidth;
                    _entries.Add(new Entry(target, target.View.BeginOutline(color, width), color, width));
                }
            }
            catch (Exception error)
            {
                try { Clear(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
        }

        internal void Clear()
        {
            List<Exception> errors = null;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                _entries.RemoveAt(i);
                CleanupErrors.Try(entry.Lease.Dispose, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[TutorialModelHighlight] 모델 강조 정리에 실패했습니다.");
        }

        private bool Contains(in TutorialModelFocus target)
        {
            foreach (Entry entry in _entries)
                if (entry.Target.Matches(target)) return true;
            return false;
        }

        private static bool Contains(IReadOnlyList<TutorialModelFocus> targets, in TutorialModelFocus target)
        {
            foreach (TutorialModelFocus candidate in targets)
                if (candidate.Matches(target)) return true;
            return false;
        }

        private readonly struct Entry
        {
            internal TutorialModelFocus Target { get; }
            internal ICombatUnitOutline Lease { get; }
            internal Color AppliedColor { get; }
            internal float AppliedWidth { get; }
            internal Entry(TutorialModelFocus target, ICombatUnitOutline lease, Color appliedColor, float appliedWidth)
            {
                Target = target;
                Lease = lease;
                AppliedColor = appliedColor;
                AppliedWidth = appliedWidth;
            }
        }
    }
}
