using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction
{
    /// <summary>
    /// 유닛별 기존 반응의 수명을 소유합니다. 같은 유닛의 반응만 배타이고, 서로 다른 유닛의 피격·사망은 함께 진행합니다(동시 처리 설계 C5).
    /// 무엇을 함께 시작할지는 순차 재생(스케줄러)과 사건 Presenter의 시작 조건이 정합니다. 기차 동시 사망의 "사망끼리만 겹침" 가드는
    /// <see cref="CanOverlapDeaths"/>로 남깁니다(설계 C7).
    /// </summary>
    internal sealed class CombatUnitReactionSet
    {
        private sealed class Entry
        {
            internal readonly EntityId Id;
            internal readonly bool Death;
            internal readonly int Version;
            internal readonly CombatUnitReactionPresenter Presenter = new();

            internal Entry(EntityId id, bool death, int version)
            {
                Id = id;
                Death = death;
                Version = version;
            }
        }

        private readonly Dictionary<EntityId, Entry> _entries = new();
        private readonly List<Entry> _tickSnapshot = new();
        private bool _ticking;
        private bool _cleaning;
        private bool _released;
        private int _version;

        internal bool IsPlaying => _entries.Count != 0;

        /// <summary>그 유닛의 반응이 진행 중인지입니다. 사건 주체 기준 시작 조건(동시 처리 설계 C5)이 씁니다.</summary>
        internal bool IsPlayingFor(EntityId id) => _entries.ContainsKey(id);

        internal int Version => _version;
        internal bool CanOverlapDeaths
        {
            get
            {
                if (_released || _cleaning) return false;
                foreach (var entry in _entries.Values)
                    if (!entry.Death) return false;
                return true;
            }
        }

        internal CombatUnitReactionPresenter BeginHit(EntityId id, CombatUnitReactionPresentationData settings, ICombatUnitPose pose,
            CombatCharacterEvent damage, FieldGridMapper mapper, Action completed, Action unavailable)
        {
            return Begin(id, false, completed, unavailable, (presenter, done, lost) =>
                presenter.BeginHit(settings, pose, damage, mapper, done, lost));
        }

        internal CombatUnitReactionPresenter BeginDeath(CombatCharacterEvent death, CombatUnitReactionPresentationData settings,
            ICombatUnitPose pose, FieldGridMapper mapper, bool launchRagdoll, Action releaseModel, Action completed, Action unavailable)
        {
            return Begin(death.Final.EntityId, true, completed, unavailable, (presenter, done, lost) =>
                presenter.BeginDeath(settings, pose, death, mapper, launchRagdoll, releaseModel, done, lost));
        }

        private CombatUnitReactionPresenter Begin(EntityId id, bool death, Action completed, Action unavailable,
            Action<CombatUnitReactionPresenter, Action, Action> start)
        {
            if (_released || _cleaning || _entries.ContainsKey(id))
                throw new InvalidOperationException("이전 반응을 정리해야 합니다. 같은 유닛의 반응은 동시에 시작할 수 없습니다.");
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (unavailable == null) throw new ArgumentNullException(nameof(unavailable));
            var entry = new Entry(id, death, ++_version);
            _entries.Add(id, entry);
            try
            {
                start(entry.Presenter, () =>
                {
                    if (!IsCurrent(entry)) return;
                    _entries.Remove(id);
                    entry.Presenter.Release();
                    // 다음 반응은 이 항목을 닫은 뒤 시작할 수 있습니다.
                    completed();
                }, () =>
                {
                    if (IsCurrent(entry)) unavailable();
                });
            }
            catch
            {
                _entries.Remove(id);
                throw;
            }
            return entry.Presenter;
        }

        /// <summary>호출자가 프레임 시작에 읽은 Version 이후의 새 반응에는 이전 시간을 주지 않습니다.</summary>
        internal void Tick(float deltaTime, int startedThroughVersion)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (_ticking) throw new InvalidOperationException("반응 목록의 Tick을 중첩 호출할 수 없습니다.");
            _ticking = true;
            try
            {
                _tickSnapshot.AddRange(_entries.Values);
                foreach (var entry in _tickSnapshot)
                {
                    if (entry.Version > startedThroughVersion || !IsCurrent(entry)) continue;
                    try
                    {
                        entry.Presenter.Tick(deltaTime);
                    }
                    finally
                    {
                        // 실패로 자세가 닫힌 경우에도 다음 대여를 이전 항목으로 지우지 않습니다.
                        if (!entry.Presenter.IsPlaying && IsCurrent(entry))
                        {
                            _entries.Remove(entry.Id);
                            entry.Presenter.Release();
                        }
                    }
                }
            }
            finally
            {
                _tickSnapshot.Clear();
                _ticking = false;
            }
        }

        internal void Cancel(EntityId id, CombatUnitReactionPresenter presenter)
        {
            if (!_entries.TryGetValue(id, out var entry) || !ReferenceEquals(entry.Presenter, presenter)) return;
            _entries.Remove(id);
            entry.Presenter.Release();
        }

        internal void Cancel()
        {
            if (_cleaning) return;
            var entries = new List<Entry>(_entries.Values);
            // 반환 중 발생하는 늦은 콜백은 다른 항목이나 새 대여를 취소할 수 없습니다.
            _entries.Clear();
            _cleaning = true;
            try
            {
                List<Exception> errors = null;
                foreach (var entry in entries)
                {
                    try
                    {
                        entry.Presenter.Release();
                    }
                    catch (Exception error)
                    {
                        (errors ??= new List<Exception>()).Add(error);
                    }
                }
                if (errors != null) throw new AggregateException(errors);
            }
            finally
            {
                _cleaning = false;
            }
        }

        internal void Release()
        {
            if (_released) return;
            _released = true;
            Cancel();
        }

        private bool IsCurrent(Entry entry) =>
            _entries.TryGetValue(entry.Id, out var current) && ReferenceEquals(current, entry);
    }
}
