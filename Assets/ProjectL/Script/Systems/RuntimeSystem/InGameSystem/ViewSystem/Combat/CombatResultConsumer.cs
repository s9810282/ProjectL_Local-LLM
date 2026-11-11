using System;
using Unity.Profiling;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 확인하지 않은 결과 하나의 소비입니다(조정자 나누기 설계 CS4, 평가 C2-02). 재생 시작 → 연출 끝 대기 → 후처리 → 확인 순서를 맡습니다.
    /// 조정자(<see cref="CombatViewSystem"/>)가 Tick의 입력 단계 뒤에 부릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 수명은 서비스 수명입니다. 조정자가 서비스를 받을 때 입력 분배 다음에 만듭니다. 씬 연결은 필드로 갖지 않고 매 호출 인자로 받습니다.
    /// 외부 호출(연출·표시·입력 묶음·Layer 변경)에서 돌아올 때마다 그 연결로 이어 가도 되는지 재확인 계약(<see cref="ICombatConnectionGuard"/>)과 재생 토큰으로 다시 봅니다.
    /// </para>
    /// <para>
    /// 결과 후처리 순서(설계 CS4): 표시 갱신 → 입력 묶음에 결과 알림 → 대기 해제·Layer 맞춤 → 결과 후처리(재요청) → 처리 번호까지 삭제·결과 확인·재생 상태 해제
    /// → 가용성 조회·확인 뒤 입력 단계. 앞 넷은 단계마다 재생 토큰을 다시 봅니다. 확인은 위 단계가 모두 성공했을 때만 합니다(K1).
    /// </para>
    /// <para>
    /// 예외 문구 앞머리는 조정자에서 옮기기 전과 같은 <c>[CombatViewSystem]</c>입니다(설계 P3).
    /// </para>
    /// </remarks>
    internal sealed class CombatResultConsumer
    {
        private static readonly ProfilerMarker FinalizeMarker = new("Combat.Result.Finalize");

        private readonly ICombatConnectionGuard _guard;
        private readonly RuntimeFaultBoundary _faults;
        private readonly CombatResultPlayback _playback;
        private readonly ICombatStateBufferReadSink _state;
        private readonly CombatDisplayValueReader _displayValues;
        private readonly ICombatViewBufferWriteSink _inputWrite;
        private readonly ICharacterBufferReadSink _characters;
        private readonly CombatInputFactsReader _facts;
        private readonly CombatSceneInputDriver _driver;

        internal CombatResultConsumer(
            ICombatConnectionGuard guard,
            RuntimeFaultBoundary faults,
            CombatResultPlayback playback,
            ICombatStateBufferReadSink state,
            CombatDisplayValueReader displayValues,
            ICombatViewBufferWriteSink inputWrite,
            ICharacterBufferReadSink characters,
            CombatInputFactsReader facts,
            CombatSceneInputDriver driver)
        {
            _guard = guard ?? throw new ArgumentNullException(nameof(guard));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _displayValues = displayValues ?? throw new ArgumentNullException(nameof(displayValues));
            _inputWrite = inputWrite ?? throw new ArgumentNullException(nameof(inputWrite));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        /// <summary>
        /// 확인하지 않은 결과 하나를 진행합니다. 재생 중이 아니면 재생을 시작하고, 연출이 끝났으면 후처리·확인을 합니다.
        /// 아직 연출 중이면 아무것도 하지 않고, 다음 Tick에서 같은 결과의 통지를 다시 봅니다.
        /// </summary>
        /// <param name="connection">이번 Tick의 입력 단계를 끝까지 이어 간 씬 연결입니다. 조정자가 넘깁니다.</param>
        internal void Consume(CombatViewConnection connection, CombatResult result)
        {
            if (!_playback.IsBusy)
            {
                connection.Input.ClearActionAvailability();
                if (!_guard.IsLive(connection))
                    return;
                // 경로 입력 취소 → 공격 상세 프리뷰 정리(입력 기능 묶음이 순서를 정한다, SV4).
                connection.Input.BeforeResult();
                _playback.Begin(result);
                // Play 안의 재진입(오류·Detach)으로 세대가 바뀌었으면 여기서 멈춘다.
                // 연결 항을 더해도 값이 같다. Detach는 연결을 비우기 전에 재생을 먼저 끊는다(SV2).
                if (!_guard.IsLive(connection))
                    return;
            }
            else if (_playback.PresentingSequence != result.Sequence)
            {
                _faults.Report(new InvalidOperationException(
                    $"[CombatViewSystem] 재생 중인 결과 {_playback.PresentingSequence}와 확인할 결과 {result.Sequence}가 다릅니다."));
                return;
            }

            if (_playback.TryGetFailure(out Exception failure))
            {
                // 실패 결과는 확인하지 않는다. 정리는 오류 경계가 StopInput으로 한다.
                _faults.Report(new InvalidOperationException(
                    $"[CombatViewSystem] 결과 {result.Sequence}의 연출이 실패했습니다.", failure));
                return;
            }

            // 아직 연출 중이면 다음 Tick에서 같은 결과의 통지를 다시 본다.
            if (_playback.TryBeginFinalization(out CombatPlaybackToken token))
                FinalizeResult(connection, result, token);
        }

        /// <summary>
        /// 완료된 결과의 후처리입니다. 외부 호출에서 돌아올 때마다 오류·연결·토큰을 다시 확인하고,
        /// 모두 성공했을 때만 처리 번호까지 삭제 → 결과 확인 → 재생 상태 해제를 합니다. 거절 결과도 같은 순서로 확인합니다.
        /// </summary>
        private void FinalizeResult(CombatViewConnection connection, CombatResult result, CombatPlaybackToken token)
        {
            using var sample = FinalizeMarker.Auto();
            if (!result.Accepted)
                UnityEngine.Debug.Log($"[CombatViewSystem] 결과 {result.Sequence}가 거절됐습니다. 사유 코드: {result.Reason}");

            // 값이 같거나 거절이어도 갱신과 확인은 그대로 한다(설계 §6.1). 전투가 정리됐으면 그릴 것이 없다.
            if (_state.HasBattle)
            {
                connection.Inspection.ApplyResult(result);
                connection.Display.Refresh(_displayValues.Read());
            }
            if (!IsCurrent(connection, token))
                return;

            // 최종 거절 사실을 표시한 뒤에도 같은 재생 수명인지 확인합니다.
            connection.Input.OnResult(result);
            if (!IsCurrent(connection, token))
                return;

            // 그린 뒤에만 일치하는 시작/턴 종료 대기를 해제한다(Kind == Command && 번호 일치, §7.2).
            connection.Input.ResolveCommand(result);

            // 결과를 그린 뒤 현재 Core 단계로 Layer를 맞춘다. 종료 상태면 Layer를 다시 켜지 않는다(§5.5·§5.6).
            _driver.SyncInputLayer();
            if (!IsCurrent(connection, token))
                return;

            // 기존 미리보기를 무효화하고, 유효하게 남은 선택·모드만 새 번호로 재요청한다. 외부 입력 차단을 푸는 것은 아니다(§7.1).
            connection.Input.AfterResult(_facts.Read(followUp: true), _characters.Characters);
            if (!IsCurrent(connection, token))
                return;

            // 처리 번호까지만 지우고(K14), 위 단계가 모두 성공한 뒤 마지막으로 확인한다(K1).
            // 이 셋은 Buffer 쓰기·재생 상태 바꾸기라 분리를 일으키지 않으므로, 연결은 위 토큰 확인 때와 같다.
            _inputWrite.RemoveCommandsThrough(_state.HandledCommandNumber);
            _inputWrite.AcknowledgePresented(result.Sequence);
            _playback.CompleteFinalization(token);
            _driver.SyncAvailability(connection);
            if (!_guard.IsLive(connection))
                return;
            // 확인 뒤 경로 입력이 복귀 견적을 맞춘다. 경로 대상이 아니면 경로 입력이 스스로 거른다. Tick도 같은 호출을 조건 없이 한다.
            connection.Input.AfterAcknowledged();
            // 확인 번호(P)·대기 해제·후처리 끝이 모두 반영된 행동 버튼은 이 Tick 끝에서 맞춘다(B6, SV6).
        }

        /// <summary>
        /// 후처리를 이어 가도 되는지입니다: 이 연결로 이어 가도 됨(R-A) ∧ 재생 토큰 일치(R-C, 설계 P8).
        /// 토큰 일치는 재생 연결을 포함하고, Detach는 연결을 비우기 전에 재생을 먼저 끊으므로 연결 항이 더해져도 값이 같습니다(SV2).
        /// </summary>
        private bool IsCurrent(CombatViewConnection connection, CombatPlaybackToken token) =>
            _guard.IsLive(connection) && _playback.IsCurrent(token);
    }
}
