using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>첫 적 목록에 기존 소환 조각을 함께 실행합니다. Core 사건·결과 번호를 만들지 않습니다.</summary>
    internal sealed class CombatEntryEnemyAppearance
    {
        private readonly CombatUnitPresentationWindow _window;
        private readonly CombatCameraWindow _camera;
        private readonly CombatAbilityMotionSettings _settings;
        private readonly List<FieldUnitView> _views = new();
        private readonly List<CombatEntryVisibility> _visibility = new();
        private CombatPieceRun _run;
        private FieldGridMapper _mapper;
        private int _generation;
        private int _frame;
        private bool _unavailable;

        internal CombatEntryEnemyAppearance(CombatUnitPresentationWindow window, CombatCameraWindow camera,
            CombatAbilityMotionSettings settings)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal bool IsComplete => _run == null && !_unavailable;

        internal void Prepare(IReadOnlyList<FieldUnitView> views, FieldGridMapper mapper)
        {
            if (views == null) throw new ArgumentNullException(nameof(views));
            Cancel();
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            foreach (FieldUnitView view in views)
            {
                if (view == null) throw new InvalidOperationException("[CombatEntryEnemyAppearance] 적 표시가 없습니다.");
                _views.Add(view);
                var visibility = new CombatEntryVisibility(view.transform);
                _visibility.Add(visibility);
                visibility.Hide();
                view.SetEntryHudHidden(true);
            }
        }

        internal void Begin()
        {
            if (_views.Count == 0) return;
            var composition = CombatComposition.ForAction(CombatPieceCompletion.General);
            foreach (FieldUnitView view in _views)
            {
                if (view == null) throw new InvalidOperationException("[CombatEntryEnemyAppearance] 준비한 적 표시가 사라졌습니다.");
                composition.Lease(view.EntityId, CombatPieceChannel.Pose);
                composition.Add(new CombatRisePiece(view.EntityId, _settings.SummonRiseDelaySeconds,
                    _settings.SummonRiseSeconds, _settings.SummonRiseDepth, _settings.SummonRiseTurns, _mapper.WorldUp),
                    CombatPieceStart.AtStart(), waits: true);
            }
            var run = new CombatPieceRun(composition, _window, _camera, EntrySources.Instance,
                () => _generation, () => _mapper, () => _frame, null, _generation, Ended, Unavailable);
            _run = run;
            if (!run.TryStart())
            {
                _run = null;
                string ids = string.Join(", ", _views.ConvertAll(view => view.EntityId.ToString()));
                throw new InvalidOperationException($"[CombatEntryEnemyAppearance] 적 등장 자세를 빌릴 수 없습니다. EntityId: {ids}");
            }
            // Begin이 아래쪽 자세를 적용한 뒤에만 모델을 드러냅니다. HUD는 전원 안착 뒤 복원합니다.
            foreach (CombatEntryVisibility visibility in _visibility) visibility.Restore();
            RestoreHudIfComplete();
        }

        internal void Tick(float deltaTime)
        {
            if (_unavailable) throw new InvalidOperationException("[CombatEntryEnemyAppearance] 적 등장 중 자세 대여가 취소됐습니다.");
            _frame++;
            _run?.Tick(deltaTime);
            if (_unavailable) throw new InvalidOperationException("[CombatEntryEnemyAppearance] 적 등장 중 표시가 사라졌습니다.");
            RestoreHudIfComplete();
        }

        private void Ended(CombatPieceRun run)
        {
            if (ReferenceEquals(_run, run)) _run = null;
        }

        private void Unavailable(CombatPieceRun run)
        {
            _unavailable = true;
            Ended(run);
        }

        private void RestoreHudIfComplete()
        {
            if (!IsComplete) return;
            foreach (FieldUnitView view in _views)
                if (view != null) view.SetEntryHudHidden(false);
        }

        internal void Cancel()
        {
            _generation++;
            CombatPieceRun run = _run;
            _run = null;
            List<Exception> errors = null;
            if (run != null) CleanupErrors.Try(run.Cancel, ref errors);
            foreach (CombatEntryVisibility visibility in _visibility) CleanupErrors.Try(visibility.Restore, ref errors);
            foreach (FieldUnitView view in _views)
                if (view != null) CleanupErrors.Try(() => view.SetEntryHudHidden(false), ref errors);
            _views.Clear();
            _visibility.Clear();
            _mapper = null;
            _unavailable = false;
            CleanupErrors.ThrowIfAny(errors, "[CombatEntryEnemyAppearance] 적 등장 정리에 실패했습니다.");
        }

        /// <summary>진입은 일반 완료 조립만 씁니다. 잘못된 전투 출처 조립은 즉시 오류로 알립니다.</summary>
        private sealed class EntrySources : ICombatSourceLifetime
        {
            internal static readonly EntrySources Instance = new();
            private static InvalidOperationException InvalidSource() => new("[CombatEntryEnemyAppearance] 진입 연출에는 전투 출처를 사용할 수 없습니다.");
            public void BeginSource(CombatEvent source, EntityId actorId, CombatHitCompletion completion, Action completed, int version) => throw InvalidSource();
            public void EndSource(CombatEvent source, int version) => throw InvalidSource();
            public void AbortSource(CombatEvent source) => throw InvalidSource();
            public void ReleaseAll(CombatEvent source, int version) => throw InvalidSource();
        }
    }
}
