#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview
{
    /// <summary>같은 표시 위에서 묶음·대기·전체 반복·취소의 수명을 소유합니다.</summary>
    internal sealed class TraitPreviewSession : IDisposable
    {
        private enum State
        {
            Idle,
            Ready,
            Playing,
            TimeWait,
            ManualWait,
            Boundary,
            Completed
        }

        private readonly Func<CombatTraitPreviewDisplay> _create;
        private readonly Func<TraitPreviewScenarioInput, CompiledTraitPreviewScenario> _compile;
        private readonly OrderedCombatPresentation _ordered = new();
        private CombatTraitPreviewDisplay _display;
        private TraitPreviewScenarioInput _input;
        private CompiledTraitPreviewScenario _program;
        private int _generation;
        private int _cursor;
        private float _elapsed;
        private State _state;
        private bool _disposed;
        private bool _pauseBetween;
        internal string Status { get; private set; } = "준비됨";
        internal string Diagnostic { get; private set; } = "";
        internal int CompletedCount { get; private set; }
        internal bool CanAdvance => _state == State.ManualWait || _state == State.Boundary;
        internal bool BoundaryPause => _pauseBetween;

        internal TraitPreviewSession(Func<CombatTraitPreviewDisplay> create, Func<TraitPreviewScenarioInput, CompiledTraitPreviewScenario> compile)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _compile = compile ?? throw new ArgumentNullException(nameof(compile));
        }

        internal void Play(TraitPreviewScenarioInput input, CompiledTraitPreviewScenario program)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TraitPreviewSession));
            ++_generation;
            _state = State.Idle;
            _input = input.Copy();
            _program = program ?? throw new ArgumentNullException(nameof(program));
            _cursor = 0;
            _elapsed = 0;
            _pauseBetween = input.pauseBetweenPackets;
            ResetDisplay(program.Initial);
            _state = State.Ready;
            Status = "초기 배치 준비됨 — 연출 확인 / 합성 결과";
            Diagnostic = "";
        }

        internal void Tick(float deltaTime)
        {
            if (_disposed)
                return;
            State started = _state;
            _ordered.Tick(deltaTime);
            // 완료 콜백과 같은 프레임에서 다음 묶음을 재진입하지 않습니다.
            if (started == State.Playing)
                return;
            if (_state == State.Ready)
            {
                BeginNext();
                return;
            }

            if (_state == State.TimeWait)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _program.Entries[_cursor].Seconds)
                {
                    ++_cursor;
                    _state = State.Ready;
                }

                return;
            }

            if (_state == State.Completed && _input.repeat)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _input.observationSeconds + _input.repeatInterval)
                {
                    var program = _compile(_input);
                    Play(_input, program);
                }
            }
        }

        private void BeginNext()
        {
            if (_cursor == _program.Entries.Count)
            {
                Complete();
                return;
            }

            var entry = _program.Entries[_cursor];
            Status = $"{_cursor + 1}/{_program.Entries.Count} · {entry.Name}";
            _elapsed = 0;
            if (entry.Kind == PreviewEntryKind.TimeWait)
            {
                _state = State.TimeWait;
                Status += $" — {entry.Seconds}초 대기";
                return;
            }

            if (entry.Kind == PreviewEntryKind.ManualWait)
            {
                _state = State.ManualWait;
                Status += " — 다음 진행 대기";
                return;
            }

            _state = State.Playing;
            Diagnostic = Describe(entry);
            int generation = _generation;
            _ordered.Play(entry.Result, outcome =>
            {
                if (generation != _generation || _disposed)
                    return;
                if (!outcome.IsCompleted)
                    throw outcome.Error ?? new InvalidOperationException("잘못된 재생 완료 통지입니다.");
                // 시나리오는 가시 폭탄 상태를 저작하지 않는다. 부착물은 단계마다 비운다.
                _display.Events.Refresh(entry.After, Array.Empty<StructureCombatFacts>(), ThornBombStateSet.Empty);
                ++_cursor;
                if (_cursor == _program.Entries.Count)
                {
                    Complete();
                    return;
                }

                _state = _pauseBetween ? State.Boundary : State.Ready;
                Status += _pauseBetween ? " — 묶음 완료 / 다음 진행 대기" : " — 묶음 완료";
            });
        }

        private void Complete()
        {
            _state = State.Completed;
            _elapsed = 0;
            ++CompletedCount;
            Status = $"전체 재생 완료 / 잔상 확인 — 누적 {CompletedCount}회 (파티클 종료 판정 아님)";
        }

        internal void Advance()
        {
            if (!CanAdvance)
                return;
            if (_state == State.ManualWait)
                ++_cursor;
            _state = State.Ready;
        }

        internal void SetBoundaryPause(bool value)
        {
            _pauseBetween = value;
            if (_input != null)
                _input.pauseBetweenPackets = value;
            if (!value && _state == State.Boundary)
                Advance();
        }

        internal void Stop()
        {
            if (_disposed)
                return;
            ++_generation;
            _state = State.Idle;
            if (_input != null)
                _input.repeat = false;
            if (_program != null)
                ResetDisplay(_program.Initial);
            _cursor = 0;
            _elapsed = 0;
            Diagnostic = "";
            Status = "중단·초기 배치 복원";
        }

        private void ResetDisplay(IReadOnlyList<CharacterCombatFacts> facts)
        {
            _ordered.Detach();
            _display?.Dispose();
            _display = null;
            _display = _create();
            _display.Events.Refresh(facts, Array.Empty<StructureCombatFacts>(), ThornBombStateSet.Empty);
            _ordered.Attach(_display.Playback);
        }

        private string Describe(CompiledPreviewEntry entry)
        {
            var text = new StringBuilder(entry.Diagnostic);
            text.AppendLine("현재 묶음 입력 전 → 완료 예정 (실제 규칙 계산 아님)");
            for (int i = 0; i < entry.Before.Count; i++)
            {
                var before = entry.Before[i];
                var after = entry.After[i];
                string name = _input.units[i].name;
                text.AppendLine($"{name}: HP {before.Health.CurrentHealth}→{after.Health.CurrentHealth}, " + $"기본 공격 {before.Stats.AttackPower}, 유효 공격 {before.Stats.EffectiveAttackPower}→{after.Stats.EffectiveAttackPower}, " + $"가속 {Held(before)}→{Held(after)}, 부여 {before.TraitEffects.AttackBonus}→{after.TraitEffects.AttackBonus}");
            }

            return text.ToString();
        }

        private static int Held(CharacterCombatFacts facts) => facts.Stats.TraitStatuses.ToArray().OfType<CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.AccelerationTraitStatusSnapshot>().SingleOrDefault()?.HeldAttackBonus ?? 0;
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ++_generation;
            _state = State.Idle;
            try
            {
                _ordered.Detach();
            }
            finally
            {
                _display?.Dispose();
                _display = null;
            }
        }
    }
}
#endif
