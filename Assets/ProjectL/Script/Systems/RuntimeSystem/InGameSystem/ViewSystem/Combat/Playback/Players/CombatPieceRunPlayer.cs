#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatEventProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatEventProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 조각 실행 집합 재생기입니다(조각·조립 설계 BP5). 진행 중인 실행(<see cref="CombatPieceRun"/>) 여럿을 가지고,
    /// 실행 목록(<see cref="CombatPresentationPlayers"/>)의 계약으로 시작 조건·프레임 진행·완료·정리를 답합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 행동 실행은 하나뿐이고 옛 능력 재생기의 규칙을 이어받아 막습니다(겹침 프로필이고 출처가 겹침을 허용할 때만 통과, 설계 B14 O3·O5).
    /// 유닛 실행은 주체마다 하나이고 걸음 재생기와 같은 주체 기준 규칙(<see cref="CombatSubjectBlocking"/>)으로 막습니다(O9·O14).
    /// 피격·사망 반응도 유닛 실행 안의 반응 조각이라 여기서 막고 진행합니다(옛 반응 재생기를 대신함, 피격 구조 정리 설계 HC1).
    /// 곁들임 실행(사실 사건의 순간 큐)은 여럿이 함께 진행하고 아무것도 막지 않습니다. 막기 판정은 행동·유닛 실행만 세고,
    /// 재생 끝 판정(<see cref="IsComplete"/>·<see cref="BlocksFinalFacts"/>)만 모든 실행을 셉니다(이펙트 창구 확장 설계 EC6).
    /// </para>
    /// <para>
    /// 처리기는 사건마다 조립을 만들어 <see cref="TryBegin"/>에 넘깁니다. 실행이 밖에서 대여를 잃으면 사라짐이 되고,
    /// 다음 Tick 맨 앞에서 예외로 알립니다(옛 능력 재생 규칙과 같음).
    /// </para>
    /// </remarks>
    internal sealed class CombatPieceRunPlayer : ICombatPresentationPlayer, ICombatSubjectActivity
    {
        private readonly CombatUnitPresentationWindow _window;
        private readonly CombatCameraWindow _camera;
        private readonly ICombatSourceOverlap _overlap;
        private readonly ICombatSourceLifetime _lifetime;
        private readonly Func<int> _generation;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly Func<int> _currentFrame;
        private readonly Action<CombatPieceRun> _onEnded;
        private readonly Action<CombatPieceRun> _onUnavailable;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal CombatEventProfiler Profiler { get; set; }

#endif
        private readonly List<CombatPieceRun> _runs = new();
        private readonly List<CombatPieceRun> _tickSnapshot = new();
        private CombatPieceRun _actionRun;
        private bool _unavailable;
        private int _frame;

        /// <param name="window">실행이 대여를 빌리는 유닛 표시 창구입니다.</param>
        /// <param name="camera">실행이 장면 채널(카메라 줌)을 빌리는 카메라 창구입니다(카메라 연출 조각 설계 K3).</param>
        /// <param name="overlap">출처 겹침 허용입니다. 공격 재생기가 프레임 시작에 기록한 값을 씁니다(보완 3).</param>
        /// <param name="lifetime">출처 완료 조립이 쓰는 출처 수명 창구입니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다.</param>
        internal CombatPieceRunPlayer(CombatUnitPresentationWindow window, CombatCameraWindow camera, ICombatSourceOverlap overlap,
            ICombatSourceLifetime lifetime, Func<int> generation, Func<FieldGridMapper> mapper)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _overlap = overlap ?? throw new ArgumentNullException(nameof(overlap));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _currentFrame = () => _frame;
            _onEnded = Remove;
            _onUnavailable = _ => _unavailable = true;
        }

        public bool IsComplete => _runs.Count == 0 && !_unavailable;

        public bool BlocksFinalFacts => _runs.Count != 0 || _unavailable;

        /// <summary>
        /// 조립 하나의 실행을 시작합니다. 대여를 하나라도 못 빌리면 아무것도 시작하지 않고 false입니다(틀이 지금처럼 머묾으로 갑니다).
        /// 시작 안에서 끝날 수 있습니다. 행동 실행은 하나만, 유닛 실행은 주체마다 하나만 진행합니다.
        /// </summary>
        /// <param name="completed">사건 완료 통지입니다. 세대가 같을 때만 한 번 전합니다.</param>
        /// <param name="version">사건을 시작한 재생 세대입니다.</param>
        internal bool TryBegin(CombatComposition composition, Action completed, int version)
        {
            return TryBeginCore(composition, completed, version, _lifetime, false, _onUnavailable, out _);
        }

        /// <summary>선행 사건을 막지 않는 조기 공격 실행입니다. 자원·Tick·해제는 같은 실행 목록이 맡습니다.</summary>
        internal bool TryBeginDeferredAttack(CombatComposition composition, ICombatSourceLifetime lifetime,
            int version, Action<CombatPieceRun> unavailable, out CombatPieceRun run)
        {
            if (composition == null || composition.Scope != CombatPieceScope.Action || !composition.Completion.IsSource)
                throw new ArgumentException("출처가 있는 공격 행동 조립이 필요합니다.", nameof(composition));
            return TryBeginCore(composition, null, version, lifetime ?? throw new ArgumentNullException(nameof(lifetime)),
                true, unavailable ?? throw new ArgumentNullException(nameof(unavailable)), out run);
        }

        internal bool HasActionRun => _actionRun != null;

        /// <summary>바로 그 조기 실행만 행동 슬롯에 합류시킵니다. 다른 실행의 슬롯을 덮어쓰지 않습니다.</summary>
        internal void JoinDeferredAttack(CombatPieceRun run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (_actionRun != null && !ReferenceEquals(_actionRun, run))
                throw new InvalidOperationException("다른 행동 실행이 끝나기 전에 조기 공격을 합류할 수 없습니다.");
            if (run.IsActive)
            {
                if (!_runs.Contains(run)) throw new InvalidOperationException("소유하지 않은 조기 공격 실행입니다.");
                _actionRun = run;
            }
        }

        private bool TryBeginCore(CombatComposition composition, Action completed, int version,
            ICombatSourceLifetime lifetime, bool deferred, Action<CombatPieceRun> unavailable, out CombatPieceRun run)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            // 끊긴 실행은 완료를 알리지 않으므로, 끊길 수 있는 실행은 기다리는 사건이 없을 때만 시작합니다(통과 규칙 Cuttable).
            if (composition.SubjectPass?.Cuttable == true && completed != null)
                throw new ArgumentException("[CombatPieceRunPlayer] 끊길 수 있는 실행은 사건 완료 통지 없이 시작합니다.", nameof(completed));
            if (_unavailable)
                throw new InvalidOperationException("[CombatPieceRunPlayer] 표시가 사라진 실행이 있어 새 실행을 시작할 수 없습니다.");
            if (composition.Scope == CombatPieceScope.Action && _actionRun != null)
                throw new InvalidOperationException("[CombatPieceRunPlayer] 이전 행동 실행이 아직 끝나지 않았습니다.");
            if (composition.Scope == CombatPieceScope.Unit && FindUnitRun(composition.Subject) != null)
                throw new InvalidOperationException($"[CombatPieceRunPlayer] 같은 유닛의 실행이 아직 끝나지 않았습니다: {composition.Subject}");

            run = new CombatPieceRun(composition, _window, _camera, lifetime, _generation, _mapper, _currentFrame, completed, version,
                _onEnded, unavailable);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            run.Profile = Profiler?.Of(composition) ?? CombatEventProfiler.Unknown;
#endif
            _runs.Add(run);
            if (composition.Scope == CombatPieceScope.Action && !deferred)
                _actionRun = run;
            bool started;
            try
            {
                started = run.TryStart();
            }
            catch
            {
                Remove(run);
                throw;
            }
            if (!started)
                Remove(run);
            return started;
        }

        public void BeginResult(CombatResult result)
        {
        }

        public CombatStartBlock FindBlock(in CombatStartQuery query)
        {
            if (_unavailable) return CombatStartBlock.Unavailable;
            // 행동 실행(포효·소환) 중에는 막습니다. 포효가 히트로 사건 완료를 알린 뒤(겹침 허용 프로필)에만 Gate 규칙으로 허용된 사건이 겹칩니다(설계 §6 Q1).
            if (_actionRun != null && !(query.AllowsOverlap && _overlap.AllowsOverlap))
                return CombatStartBlock.PieceBusy;
            // 그 사건 주체의 유닛 실행이 통과 규칙을 들고 있고 그 규칙이 이 사건을 통과시키면 막지 않습니다(피격 구조 정리 설계 HC3).
            // 밀림과 함께 가는 피격·연발 앞 탄의 접촉 피격이 그렇고, 어떤 사건을 통과시키는지는 규칙(CombatComposition.SubjectPass)이 정합니다.
            if (SubjectPassOf(query.Item)?.Passes(query.Item) == true)
                return CombatStartBlock.None;
            return CombatSubjectBlocking.Blocks(query, this) ? CombatStartBlock.PieceSubject : CombatStartBlock.None;
        }

        /// <summary>
        /// 그 사건이 주체의 유닛 실행을 끊는 사건이면(통과 규칙의 <see cref="ICombatRunSubjectPass.IsCutBy"/>) 그 실행을 지금 끊습니다.
        /// 조각을 멈추고 대여(클립·플래시 등)를 되돌리며 완료는 알리지 않습니다. 끊을 실행이 없으면 아무 일도 하지 않습니다.
        /// 그 사건(또는 같은 사건으로 내는 다음 탄의 접촉 피격)을 시작하기 바로 전에, 자세·클립을 빌리기 전에 부릅니다.
        /// </summary>
        internal void CutRunFor(CombatEvent item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!CombatEventTypeTable.TryGetSubject(item, out EntityId subject)) return;
            CombatPieceRun run = FindUnitRun(subject);
            if (run != null && run.Composition.SubjectPass?.IsCutBy(item) == true)
                run.Cancel();
        }

        // 그 사건 주체의 유닛 실행이 들고 있는 통과 규칙입니다. 실행이 없거나 주체가 없거나 규칙이 없으면 null입니다.
        // 실행이 없을 때(재생 중 대부분의 사건)는 사건 형식 표를 읽지 않고 바로 돌아갑니다(묶음 끝 검토 ⑨).
        // 그 유닛의 유닛 실행은 하나뿐이라(주체마다 하나) 통과시켜도 그 유닛의 다른 연출을 건너뛰지 않습니다. 행동 실행 막기는 FindBlock이 먼저 봅니다.
        private ICombatRunSubjectPass SubjectPassOf(CombatEvent item)
        {
            if (_runs.Count == 0 || item == null) return null;
            return CombatEventTypeTable.TryGetSubject(item, out EntityId subject) ? FindUnitRun(subject)?.Composition.SubjectPass : null;
        }

        /// <summary>프레임 번호를 올립니다. 이 프레임에 신호로 바뀐 항목은 이 프레임 시간을 받지 않습니다(설계 BP2 규칙 2, B14 O4).</summary>
        public void BeginFrame(in CombatPlaybackFrame frame) => _frame++;

        public bool Tick(in CombatPlaybackFrame frame)
        {
            if (_unavailable)
                throw new InvalidOperationException("[CombatPieceRunPlayer] 연출 중 빌린 표시(유닛 표시 또는 맵 카메라)가 사라졌습니다.");
            if (_runs.Count == 0)
                return true;
            // Tick 전에 행동 실행이 있었는지로 정합니다(옛 능력 재생기와 같음). 히트 전에는 이 재생기의 실행(행동·유닛)만 진행하고 뒤 재생기(걸음 등)는 멈추며,
            // 히트 뒤에는 넉백 걸음도 같은 프레임에 진행합니다(설계 §6 Q1, B14 O3·O5). 필드 행동은 멈추지 않습니다.
            bool pausesOthers = _actionRun != null && _actionRun.Composition.PausesOtherPlayers;
            _tickSnapshot.AddRange(_runs);
            try
            {
                for (int i = 0; i < _tickSnapshot.Count; i++)
                {
                    CombatPieceRun run = _tickSnapshot[i];
                    if (run.IsActive)
                        run.Tick(frame.DeltaTime);
                }
            }
            finally
            {
                _tickSnapshot.Clear();
            }
            return !pausesOthers || _overlap.AllowedAtFrameStart;
        }

        public void Cancel()
        {
            _unavailable = false;
            if (_runs.Count == 0)
                return;
            // 진행 Tick 안에서 불려도 되도록 따로 복사합니다(취소는 드물어 할당을 감수합니다).
            CombatPieceRun[] runs = _runs.ToArray();
            _runs.Clear();
            _actionRun = null;
            List<Exception> errors = null;
            for (int i = 0; i < runs.Length; i++)
                CleanupErrors.Try(runs[i].Cancel, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatPieceRunPlayer] 조각 실행 정리에 실패했습니다.");
        }

        public void Release() => Cancel();

        bool ICombatSubjectActivity.IsPlaying
        {
            get
            {
                for (int i = 0; i < _runs.Count; i++)
                    if (_runs[i].Composition.Scope == CombatPieceScope.Unit)
                        return true;
                return false;
            }
        }

        bool ICombatSubjectActivity.IsPlayingFor(EntityId subject) => HasUnitRun(subject);

        /// <summary>
        /// 그 유닛의 유닛 실행이 진행 중인지입니다. 유닛 실행은 주체마다 하나라 진행 중이면 <see cref="TryBegin"/>가 예외이므로,
        /// 사건 순간에 곁들여 여는 쪽(특성 발동 유닛 행동, 유닛 행동 계획 Tf5.1)이 먼저 묻습니다.
        /// </summary>
        internal bool HasUnitRun(EntityId subject) => FindUnitRun(subject) != null;

        bool ICombatSubjectActivity.CanOverlapDeaths
        {
            get
            {
                for (int i = 0; i < _runs.Count; i++)
                {
                    CombatComposition composition = _runs[i].Composition;
                    if (composition.Scope == CombatPieceScope.Unit && !composition.IsDeath)
                        return false;
                }
                return true;
            }
        }

        private CombatPieceRun FindUnitRun(EntityId subject)
        {
            for (int i = 0; i < _runs.Count; i++)
            {
                CombatComposition composition = _runs[i].Composition;
                if (composition.Scope == CombatPieceScope.Unit && composition.Subject == subject)
                    return _runs[i];
            }
            return null;
        }

        private void Remove(CombatPieceRun run)
        {
            _runs.Remove(run);
            if (ReferenceEquals(_actionRun, run))
                _actionRun = null;
        }
    }
}
