using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 걸음·대기 재생기입니다(연출 조립 설계 B12). 유닛별 걸음 레인(<see cref="CombatUnitMotionSet"/>)과 간격·머묾 대기 칸(<see cref="CombatEventWait"/>)을 함께 가집니다.
    /// 간격 동안 레인만 멈추고 반응은 진행해야 해서(B14 O6·O7) "프레임 멈춤" 반환 하나로는 둘을 나눌 수 없기 때문입니다.
    /// 대기 칸은 역할 계약(<see cref="ICombatWaitSlot"/>, 보완 1)으로 사건 Presenter와 이동 표시에 내줍니다.
    /// </summary>
    internal sealed class CombatStepPlayer : ICombatPresentationPlayer, ICombatWaitSlot, ICombatSubjectActivity, ICombatSubjectOverlap
    {
        private readonly CombatUnitMotionSet _motion;
        private readonly CombatEventWait _wait = new();
        private readonly Action<CombatEvent, Action> _present;
        private readonly Func<int> _generation;
        private readonly Func<FieldGridMapper> _mapper;

        /// <param name="present">간격 뒤 꺼낸 대기 사건을 사건 Presenter의 틀로 넘깁니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 재생 세대입니다(D10). 읽기만 합니다.</param>
        /// <param name="mapper">사건 Presenter가 소유한 현재 Mapper입니다. 사본을 갖지 않습니다.</param>
        internal CombatStepPlayer(CombatUnitMotionSet motion, Action<CombatEvent, Action> present, Func<int> generation,
            Func<FieldGridMapper> mapper)
        {
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _present = present ?? throw new ArgumentNullException(nameof(present));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // 걸음 사건 완료 뒤 남은 보간·미룬 방향 복귀도 남은 표시입니다(설계 M3). 대기 칸은 완료 판단에 넣지 않습니다(분리 전과 같음).
        public bool IsComplete => !_motion.IsActive;

        public bool BlocksFinalFacts => false;

        public void BeginResult(CombatResult result) => _motion.BeginResult(result);

        public CombatStartBlock FindBlock(in CombatStartQuery query)
        {
            // 걸음 레인 자원(동시 처리 설계 C5): 주체 없는 사건·행동 사건은 모든 레인의 걸음 사건 완료 뒤, 결과·동시 사망 단정은 모든 레인이 쉰 뒤입니다.
            // 규칙은 반응·유닛 실행과 같은 주체 막기 하나입니다(View 3라운드 W12-3). 걸음 Gate 규칙은 아래에서 따로 봅니다.
            if (CombatSubjectBlocking.Blocks(query, this, this)) return CombatStartBlock.StepLane;
            // 걸음 Gate(이동 접촉 설계 M4). 넉백 걸음 중에는 공격 Gate와 함께 활성일 수 있습니다. 시작할 사건이 있을 때만 봅니다.
            if (query.Item != null && !_motion.CanBegin(query.Item))
                return CombatStartBlock.StepGate;
            // 간격 뒤 처리할 대기 사건이 있으면 막습니다.
            return _wait.HasEvent ? CombatStartBlock.Waiting : CombatStartBlock.None;
        }

        public void BeginFrame(in CombatPlaybackFrame frame)
        {
        }

        // 걸음(걸음 전 간격·간격 뒤 대기 사건 포함)을 진행합니다. 다른 유닛 반응 중에 시작한 넉백 걸음도 멈추지 않습니다(동시 처리 설계 C5).
        public bool Tick(in CombatPlaybackFrame frame)
        {
            if (!_motion.IsActive && !_wait.HasEvent)
                return true;
            int version = _generation();
            TickMotion(frame.DeltaTime);
            return version == _generation();
        }

        public void Cancel()
        {
            // 레인 취소가 실패해도 대기 칸은 비웁니다. 실패는 실행 목록이 모아 끝에 던집니다(결정 Q-R7 A).
            try
            {
                _motion.Cancel();
            }
            finally
            {
                _wait.Clear();
            }
        }

        // 레인·대기 칸은 따로 해제할 자원이 없습니다. 해제 전에 사건 Presenter의 Clear가 취소합니다.
        public void Release()
        {
        }

        // 간격과 간격 뒤 대기 사건 동안은 모든 레인의 진행을 멈춥니다(TickMotion). 간격은 행동 경계(자발적 첫 걸음·행동 사건)에만 생기므로
        // 그때 진행 중인 레인이 없어야 합니다. 앞으로 묶음이나 겹침 규칙을 바꿔 이 전제가 깨지면 레인이 멈추는 대신 여기서 알립니다(동시 처리 검토).
        public void StartGap(float seconds)
        {
            if (_motion.IsActive)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 걸음 레인이 진행 중일 때 행동 간격을 시작할 수 없습니다.");
            _wait.StartGap(seconds);
        }

        public void HoldAfterGap(CombatEvent e, Action completed, bool shownBeforeGap) => _wait.HoldAfterGap(e, completed, shownBeforeGap);

        public void HoldShown(CombatEvent e, Action completed, float seconds) => _wait.HoldShown(e, completed, seconds);

        // 걸음 전 간격, 간격 뒤 대기 사건(행동 사건만 생김) 표시, 걸음 보간·방향 복귀를 진행합니다.
        private void TickMotion(float deltaTime)
        {
            FieldGridMapper mapper = _mapper();
            if (mapper == null)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 필드 구성 성공 후 Mapper 연결이 필요합니다.");
            float remaining = Math.Max(0f, deltaTime);
            if (!_wait.Consume(ref remaining))
                return;

            // 간격 뒤 대기 사건이 있으면 이 프레임은 그것만 처리합니다. 남은 시간은 레인에 주지 않습니다(B14 O7).
            if (_wait.TryTake(out var waiting, out var completed, out bool shown))
            {
                if (shown)
                    completed();
                else
                    _present(waiting, completed);
                return;
            }

            _motion.Tick(remaining, mapper);
        }

        // 주체 막기 규칙이 읽는 레인 상태입니다. 걸음 사건 완료 뒤(남은 보간·미룬 방향 복귀)에는 겹침 허용 프로필에서 다음 사건과 겹칩니다.
        bool ICombatSubjectActivity.IsPlaying => _motion.IsActive;

        bool ICombatSubjectActivity.IsPlayingFor(EntityId subject) => _motion.IsActiveFor(subject);

        bool ICombatSubjectOverlap.AllowsOverlap => _motion.AllowsOverlap;

        bool ICombatSubjectOverlap.AllowsOverlapFor(EntityId subject) => _motion.AllowsOverlapFor(subject);

        // 걸음은 기차 동시 사망과 겹치지 않습니다. 동시 사망 단정은 모든 레인이 쉴 때만 통과합니다(동시 처리 설계 C7).
        bool ICombatSubjectActivity.CanOverlapDeaths => !_motion.IsActive;
    }
}
