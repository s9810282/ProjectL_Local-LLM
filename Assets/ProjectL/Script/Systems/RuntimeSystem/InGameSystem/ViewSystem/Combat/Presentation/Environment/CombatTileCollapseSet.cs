using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment
{
    /// <summary>
    /// 바닥 교체 외형의 진입·붕괴 연출을 진행합니다. 진입은 낮아진 표면을 남기고, 붕괴는 끝 콜백으로 외형을 반환합니다.
    /// </summary>
    internal sealed class CombatTileCollapseSet
    {
        private readonly struct Entry
        {
            internal readonly CombatTileCollapse Motion;
            internal readonly Action Finished;

            internal Entry(CombatTileCollapse motion, Action finished)
            {
                Motion = motion;
                Finished = finished;
            }
        }

        private readonly List<Entry> _entries = new();
        private readonly CombatTileCollapseSettings _settings;

        internal CombatTileCollapseSet(CombatTileCollapseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>무너지는 중인 외형이 하나라도 있는지입니다. 결과 끝 사실 반영이 이것까지 기다립니다.</summary>
        internal bool IsPlaying => _entries.Count > 0;

        /// <summary>진입 하강 중이거나 붕괴 직전 떨리는 동안 다음 사건 시작을 막습니다.</summary>
        internal bool BlocksStart
        {
            get
            {
                foreach (Entry entry in _entries)
                    if (entry.Motion.BlocksStart) return true;
                return false;
            }
        }

        /// <summary>
        /// 외형 하나의 무너짐을 시작합니다. 연출 시간이 0이면 시작하지 않고 false이며, 그러면 부르는 쪽이 바로 돌려줍니다.
        /// </summary>
        /// <param name="view">무너질 외형입니다. 시작 자리는 지금 월드 위치입니다.</param>
        /// <param name="fieldX">필드 가로(X) 축의 월드 방향입니다.</param>
        /// <param name="fieldZ">필드 세로(Z) 축의 월드 방향입니다.</param>
        /// <param name="fieldUp">필드 위쪽 축의 월드 방향입니다.</param>
        /// <param name="finished">다 무너졌거나 끝냈을 때 한 번 부릅니다.</param>
        internal bool TryBegin(Transform view, Vector3 fieldX, Vector3 fieldZ, Vector3 fieldUp, Action finished)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (finished == null) throw new ArgumentNullException(nameof(finished));
            if (_settings.TotalSeconds <= 0f) return false;
            _entries.Add(new Entry(new CombatTileCollapse(view, fieldX, fieldZ, fieldUp, _settings), finished));
            return true;
        }

        /// <summary>밟은 지반은 진입 배율로 떨림 시간·진폭과 하강 시간을 조절하고 실제 표면 높이 차만큼 내립니다.</summary>
        internal void BeginEntry(Transform view, Vector3 fieldX, Vector3 fieldZ, Vector3 descent,
            Action<float> applySurfaceProgress)
        {
            if (applySurfaceProgress == null) throw new ArgumentNullException(nameof(applySurfaceProgress));
            var settings = _settings.ForEntry(descent.magnitude);
            var motion = new CombatTileCollapse(view, fieldX, fieldZ, -descent, settings, applySurfaceProgress);
            if (settings.TotalSeconds == 0f)
                motion.Tick(0f);
            else
                _entries.Add(new Entry(motion, motion.FinishEntry));
        }

        /// <summary>무너짐을 진행하고, 다 무너진 외형의 끝 콜백을 부릅니다. 콜백 하나가 실패해도 나머지 외형을 돌려준 뒤 모아 던집니다(평가 C9-04).</summary>
        internal void Tick(float deltaTime)
        {
            List<Exception> errors = null;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];
                entry.Motion.Tick(deltaTime);
                if (!entry.Motion.IsDone) continue;
                _entries.RemoveAt(i);
                CleanupErrors.Try(entry.Finished, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatTileCollapseSet] 무너짐 끝 반환 중 실패했습니다.");
        }

        /// <summary>
        /// 남은 무너짐을 연출 없이 모두 끝내고 끝 콜백을 부릅니다(취소·건너뛰기·정리). 목록을 먼저 비워, 콜백이 실패해도 같은 외형을 두 번 돌려주지 않습니다.
        /// 콜백 하나가 실패해도 나머지를 부른 뒤 모아 던집니다(평가 C9-04).
        /// 남은 것이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void FinishAll()
        {
            if (_entries.Count == 0) return;
            Entry[] entries = _entries.ToArray();
            _entries.Clear();
            List<Exception> errors = null;
            foreach (Entry entry in entries)
                CleanupErrors.Try(entry.Finished, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatTileCollapseSet] 무너짐 끝내기 중 실패했습니다.");
        }
    }
}
