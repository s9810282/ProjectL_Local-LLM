using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 말을 시설 도착 칸으로 옮기는 표시 쪽 부품입니다. 도착 자세 조회·이동 시작·이동 소리를 맡고,
    /// 입력(<see cref="LobbyInteraction"/>)은 "어느 시설로"만 정합니다(로비 리팩토링 2차 설계 N-55).
    /// </summary>
    internal sealed class LobbyPieceMover : IDisposable
    {
        private LobbyBoard _board;
        private ChessPieceMotion _motion;
        private IAudioSystemService _audio;
        // 이동 요청 세대입니다. 이동 시작 중 취소·새 이동이 다시 들어오면 바깥 요청은 소리를 내지 않습니다(옛 LobbyInteraction 조건).
        private int _generation;

        internal LobbyPieceMover(LobbyBoard board, ChessPieceMotion motion, IAudioSystemService audio)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal bool IsMoving => _motion != null && _motion.IsMoving;

        /// <summary>
        /// 시설 도착 칸으로 옮기기 시작합니다. 같은 자리면 바로 끝나 소리를 내지 않습니다.
        /// 이동 시작 콜백이 이 부품을 정리했으면 소리를 내지 않습니다.
        /// </summary>
        internal void MoveTo(LobbyFacilityKind kind)
        {
            if (_motion == null) throw new ObjectDisposedException(nameof(LobbyPieceMover));
            Pose arrival = _board.GetArrival(kind);
            ChessPieceMotion motion = _motion;
            int generation = ++_generation;
            motion.Begin(arrival.position, arrival.position + arrival.rotation * Vector3.forward);
            if (ReferenceEquals(_motion, motion) && generation == _generation && motion.IsMoving)
                _audio.Play(AmbgmEvent.LobbyWalk);
        }

        /// <summary>이동 완료를 한 번 소비합니다.</summary>
        internal bool TryConsumeCompletion() => _motion != null && _motion.TryConsumeCompletion();

        internal void Cancel()
        {
            ++_generation;
            _motion?.Cancel();
        }

        public void Dispose()
        {
            ChessPieceMotion motion = _motion;
            ++_generation;
            _motion = null;
            _board = null;
            _audio = null;
            motion?.Dispose();
        }
    }
}
