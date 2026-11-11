using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 하붑 사건의 표시입니다(하붑 View 계획 Hb.6, Hd.5). 경고와 칸이 그대로인 이동은 칸을 맞추고 기다리지 않습니다(계획 §4의 10).
    /// 칸이 바뀌는 이동은 Core에서 순간이지만 화면에서는 구름이 새 자리로 미끄러지고, 도착하면 사건이 끝납니다(설계 §9.4).
    /// </summary>
    internal sealed class CombatHaboobEventHandler : CombatEventHandler
    {
        private readonly ICombatHaboobField _haboob;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatEventAudio _sound;

        internal CombatHaboobEventHandler(ICombatHaboobField haboob, CombatPieceRunPlayer pieces, Func<FieldGridMapper> mapper, CombatEventAudio sound)
        {
            _haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
        }

        internal override void ApplyFacts(CombatEvent e)
        {
            if (e is CombatHaboobEvent haboob && !Moves(haboob))
                _haboob.Show(haboob.Final.Cells, _mapper());
        }

        internal override bool TryBeginAfterFacts(CombatEvent e, Action completed, int version)
        {
            if (e is not CombatHaboobEvent haboob || !Moves(haboob))
                return false;

            // 소리는 출발 순간에 한 번만 냅니다. 칸마다 내면 겹쳐 커집니다(계획 HP-8).
            _sound.Play(CombatEventSounds.HaboobRelocation());
            if (!_haboob.BeginMove(haboob.Expected.Cells, haboob.Final.Cells, _mapper()))
                return false;

            var composition = CombatComposition.ForAction(CombatPieceCompletion.General);
            composition.Add(new CombatHaboobMovePiece(_haboob, _haboob.Settings.RelocationSeconds), CombatPieceStart.AtStart(), waits: true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = e;
#endif
            return _pieces.TryBegin(composition, completed, version);
        }

        // 경고 사건은 칸이 같습니다(Core 사건 생성자가 보장). 다른 자리가 없는 이동도 칸이 같습니다.
        private static bool Moves(CombatHaboobEvent haboob) => !haboob.Expected.Cells.SequenceEqual(haboob.Final.Cells);
    }
}
