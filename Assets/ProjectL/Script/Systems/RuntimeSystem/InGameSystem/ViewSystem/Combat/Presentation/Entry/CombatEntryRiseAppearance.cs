using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
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
    /// <summary>
    /// 진입 때 보드 위 표시 목록(첫 필드 적, 보드 위 구조물)에 지정된 시작 지연으로 기존 소환 상승 조각을 실행합니다. Core 사건·결과 번호를 만들지 않습니다.
    /// 준비 때 모델과 HUD를 숨기고, 각 조각이 아래쪽 자세를 적용한 뒤 해당 모델을, 모두 안착한 뒤 HUD를 보입니다.
    /// </summary>
    internal sealed class CombatEntryRiseAppearance
    {
        private readonly CombatUnitPresentationWindow _window;
        private readonly CombatCameraWindow _camera;
        private readonly CombatAbilityMotionSettings _settings;
        // 오류 문구의 대상 이름입니다("적", "구조물").
        private readonly string _subject;
        private readonly List<FieldUnitView> _views = new();
        // 준비 때 표시의 신원입니다. 정리 때 다른 개체로 재사용된 표시는 건드리지 않습니다(Nexus 낙하와 같은 정책, 평가 G2-01).
        private readonly List<EntityId> _ids = new();
        private readonly List<CombatEntryVisibility> _visibility = new();
        private readonly List<float> _startDelays = new();
        private CombatPieceRun _run;
        private FieldGridMapper _mapper;
        private int _generation;
        private int _frame;
        private bool _unavailable;

        internal CombatEntryRiseAppearance(CombatUnitPresentationWindow window, CombatCameraWindow camera,
            CombatAbilityMotionSettings settings, string subject)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _subject = string.IsNullOrEmpty(subject) ? throw new ArgumentException("대상 이름이 필요합니다.", nameof(subject)) : subject;
        }

        internal bool IsComplete => _run == null && !_unavailable;

        internal void Prepare(IReadOnlyList<FieldUnitView> views, FieldGridMapper mapper, IReadOnlyList<float> startDelays = null)
        {
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (startDelays != null)
            {
                if (startDelays.Count != views.Count)
                    throw new ArgumentException("대상별 시작 지연 개수는 표시 개수와 같아야 합니다.", nameof(startDelays));
                foreach (float delay in startDelays) CombatPresentationSettingsData.ValidateSeconds(delay, nameof(startDelays));
            }
            Cancel();
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            for (int i = 0; i < views.Count; i++)
            {
                FieldUnitView view = views[i];
                if (view == null) throw new InvalidOperationException($"[CombatEntryRiseAppearance] {_subject} 표시가 없습니다.");
                _views.Add(view);
                _ids.Add(view.EntityId);
                _startDelays.Add(startDelays == null ? 0f : startDelays[i]);
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
            for (int i = 0; i < _views.Count; i++)
            {
                FieldUnitView view = _views[i];
                if (view == null) throw new InvalidOperationException($"[CombatEntryRiseAppearance] 준비한 {_subject} 표시가 사라졌습니다.");
                composition.Lease(view.EntityId, CombatPieceChannel.Pose);
                var rise = new CombatRisePiece(view.EntityId, _settings.SummonRiseDelaySeconds,
                    _settings.SummonRiseSeconds, _settings.SummonRiseDepth, _settings.SummonRiseTurns, _mapper.WorldUp);
                composition.Add(new VisibleRisePiece(rise, _visibility[i]),
                    CombatPieceStart.AtStart(_startDelays[i]), waits: true);
            }
            var run = new CombatPieceRun(composition, _window, _camera, EntrySources.Instance,
                () => _generation, () => _mapper, () => _frame, null, _generation, Ended, Unavailable);
            _run = run;
            if (!run.TryStart())
            {
                _run = null;
                string ids = string.Join(", ", _views.ConvertAll(view => view.EntityId.ToString()));
                throw new InvalidOperationException($"[CombatEntryRiseAppearance] {_subject} 등장 자세를 빌릴 수 없습니다. EntityId: {ids}");
            }
            RestoreHudIfComplete();
        }

        internal void Tick(float deltaTime)
        {
            if (_unavailable) throw new InvalidOperationException($"[CombatEntryRiseAppearance] {_subject} 등장 중 자세 대여가 취소됐습니다.");
            _frame++;
            _run?.Tick(deltaTime);
            if (_unavailable) throw new InvalidOperationException($"[CombatEntryRiseAppearance] {_subject} 등장 중 표시가 사라졌습니다.");
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
            // Renderer 켜짐은 늘 되돌립니다(같은 키 풀의 같은 모델 스냅숏이라 개체와 무관). HUD 숨김은 같은 개체이거나 반환만 된 표시에만 되돌립니다(평가 G2-01).
            foreach (CombatEntryVisibility visibility in _visibility) CleanupErrors.Try(visibility.Restore, ref errors);
            for (int i = 0; i < _views.Count; i++)
            {
                FieldUnitView view = _views[i];
                if (view == null || !CombatEntryNexusAppearance.IsSameOrReturned(view, _ids[i])) continue;
                CleanupErrors.Try(() => view.SetEntryHudHidden(false), ref errors);
            }
            _views.Clear();
            _ids.Clear();
            _visibility.Clear();
            _startDelays.Clear();
            _mapper = null;
            _unavailable = false;
            CleanupErrors.ThrowIfAny(errors, $"[CombatEntryRiseAppearance] {_subject} 등장 정리에 실패했습니다.");
        }

        /// <summary>지연 중인 모델은 숨긴 채 두고, 실제 상승 시작 자세를 적용한 뒤에만 공개합니다. 시간과 곡선은 기존 상승 조각이 소유합니다.</summary>
        private sealed class VisibleRisePiece : ICombatPresentationPiece
        {
            private readonly CombatRisePiece _rise;
            private readonly CombatEntryVisibility _visibility;

            internal VisibleRisePiece(CombatRisePiece rise, CombatEntryVisibility visibility)
            {
                _rise = rise;
                _visibility = visibility;
            }

            public bool IsEnded => _rise.IsEnded;

            public void Begin(CombatPieceContext context)
            {
                _rise.Begin(context);
                _visibility.Restore();
            }

            public void Tick(float deltaTime) => _rise.Tick(deltaTime);
            public void Cancel() => _rise.Cancel();
        }

        /// <summary>진입은 일반 완료 조립만 씁니다. 잘못된 전투 출처 조립은 즉시 오류로 알립니다.</summary>
        private sealed class EntrySources : ICombatSourceLifetime
        {
            internal static readonly EntrySources Instance = new();
            private static InvalidOperationException InvalidSource() => new("[CombatEntryRiseAppearance] 진입 연출에는 전투 출처를 사용할 수 없습니다.");
            public void BeginSource(CombatEvent source, EntityId actorId, CombatHitCompletion completion, Action completed, int version) => throw InvalidSource();
            public void EndSource(CombatEvent source, int version) => throw InvalidSource();
            public void AbortSource(CombatEvent source) => throw InvalidSource();
            public void ReleaseAll(CombatEvent source, int version) => throw InvalidSource();
            public void Release(CombatEvent source, EntityId target, int version) => throw InvalidSource();
        }
    }
}
