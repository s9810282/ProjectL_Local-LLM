using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>기믹은 보드 완성까지, 상시 효과는 유닛 등장 완료까지 첫 표시를 보류합니다. 시간은 진행하지 않습니다.</summary>
    internal sealed class CombatEntryDeferredDisplay
    {
        private readonly FieldRailwayPresenter _railway;
        private readonly FieldTornadoPresenter _tornado;
        private readonly FieldRockfallPresenter _rockfall;
        private readonly FieldHaboobPresenter _haboob;
        private readonly CombatSceneEventPresenter _events;
        private bool _boardDeferred;
        private StageGimmickStateSet _pendingGimmicks;
        private IReadOnlyList<TornadoAreaFact> _pendingTornadoes;
        private FieldGridMapper _pendingMapper;

        internal CombatEntryDeferredDisplay(FieldRailwayPresenter railway, FieldTornadoPresenter tornado,
            FieldRockfallPresenter rockfall, FieldHaboobPresenter haboob, CombatSceneEventPresenter events)
        {
            _railway = railway ?? throw new ArgumentNullException(nameof(railway));
            _tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            _haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>선로 설치·사건 Bind 뒤, 최초 사실 그리기 전에만 호출합니다.</summary>
        internal void Begin()
        {
            Cancel();
            _boardDeferred = true;
            _events.SetEntryEffectsDeferred(true);
            _railway.SetEntryVisible(false);
            // 재설치에서 남은 표시도 보드보다 먼저 나타나지 않게 비웁니다.
            _tornado.Clear();
            _rockfall.Clear();
            _haboob.Clear();
        }

        /// <summary>보류 중에는 그릴 사실만 고정하고, 공개 이후에는 기존 기믹 그리기를 바로 수행합니다.</summary>
        internal void Render(StageGimmickStateSet gimmicks, IReadOnlyList<TornadoAreaFact> tornadoes, FieldGridMapper mapper)
        {
            if (gimmicks == null) throw new ArgumentNullException(nameof(gimmicks));
            if (tornadoes == null) throw new ArgumentNullException(nameof(tornadoes));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (!_boardDeferred)
            {
                RenderNow(gimmicks, tornadoes, mapper);
                return;
            }
            // 기믹 집합/영역 항목은 불변이고, 목록만 고정합니다. 가변 Buffer는 보관하지 않습니다.
            _pendingGimmicks = gimmicks;
            _pendingTornadoes = new List<TornadoAreaFact>(tornadoes).AsReadOnly();
            _pendingMapper = mapper;
        }

        internal void RevealBoard()
        {
            if (!_boardDeferred) return;
            if (_pendingMapper == null)
                throw new InvalidOperationException("[CombatEntryDeferredDisplay] 보드 공개 전에 기믹 사실 그리기가 필요합니다.");
            StageGimmickStateSet gimmicks = _pendingGimmicks;
            IReadOnlyList<TornadoAreaFact> tornadoes = _pendingTornadoes;
            FieldGridMapper mapper = _pendingMapper;
            _boardDeferred = false;
            ClearPending();
            _railway.SetEntryVisible(true);
            RenderNow(gimmicks, tornadoes, mapper);
        }

        /// <summary>유닛의 최종 등장 자세가 복원된 뒤 상시 효과만 맞춥니다. 유닛을 다시 그리지 않습니다.</summary>
        internal void RevealUnits() => _events.RevealEntryEffects();

        /// <summary>미공개 작업을 버립니다. 취소하면서 기믹이나 상시 효과를 새로 재생하지 않습니다.</summary>
        internal void Cancel()
        {
            bool restoreRailway = _boardDeferred;
            _boardDeferred = false;
            ClearPending();
            List<Exception> errors = null;
            CleanupErrors.Try(() => _events.SetEntryEffectsDeferred(false), ref errors);
            if (restoreRailway) CleanupErrors.Try(() => _railway.SetEntryVisible(true), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryDeferredDisplay] 진입 표시 보류 해제에 실패했습니다.");
        }

        private void RenderNow(StageGimmickStateSet gimmicks, IReadOnlyList<TornadoAreaFact> tornadoes, FieldGridMapper mapper)
        {
            _tornado.Render(tornadoes, mapper);
            _rockfall.Render(gimmicks, mapper);
            _haboob.Render(gimmicks, mapper);
        }

        private void ClearPending()
        {
            _pendingGimmicks = null;
            _pendingTornadoes = null;
            _pendingMapper = null;
        }
    }
}
