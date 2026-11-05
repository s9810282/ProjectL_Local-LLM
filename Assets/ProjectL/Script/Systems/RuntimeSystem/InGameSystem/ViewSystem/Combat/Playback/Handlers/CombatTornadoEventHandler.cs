using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 움직이는 토네이도 사건의 표시입니다(토네이도 View 설계 TN1). 요약·진입 시작 사건은 할 일이 없습니다. 요약의 최종 값을 먼저 쓰면 폭풍이 접촉을 앞질러 갑니다(인계 §4.4).
    /// 중심 사건은 몸을 옮기고, 원본 완료 사건은 최종 자리와 방향 화살표를 맞춥니다. 숨겨진 원본은 표시가 없어 아무 일도 하지 않고, 유닛 던짐은 캐릭터 처리기가 그대로 재생합니다.
    /// </summary>
    internal sealed class CombatTornadoEventHandler : CombatEventHandler
    {
        private readonly ICombatTornadoField _tornado;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatEventAudio _sound;

        internal CombatTornadoEventHandler(ICombatTornadoField tornado, CombatPieceRunPlayer pieces, Func<FieldGridMapper> mapper, CombatEventAudio sound)
        {
            _tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
        }

        /// <summary>1번 이상의 중심으로 몸을 옮깁니다. 도착하면 사건이 끝나고 그 중심의 접촉 사건이 이어집니다.</summary>
        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            if (e is not CombatTornadoCenterEvent center || center.Origin.StepIndex == 0)
                return false;
            long sourceId = center.Origin.SourceId;
            if (!_tornado.TryGetBodyMove(sourceId, center.Center, _mapper(), out Vector3 from, out Vector3 to))
                return false;
            var composition = CombatComposition.ForFieldAction();
            composition.Add(new CombatTornadoMovePiece(_tornado, _tornado.Settings, sourceId, from, to), CombatPieceStart.AtStart(), waits: true);
            return _pieces.TryBegin(composition, completed, version);
        }

        internal override void ApplyFacts(CombatEvent e)
        {
            switch (e)
            {
                // 0번 중심은 출발 자리 확인이고 이동이 시작되니 화살표를 끕니다. 숨겨진 원본의 중심도 여기로 오고 아무 일도 하지 않습니다.
                case CombatTornadoCenterEvent center:
                    // 소리는 이동 한 번에 한 번, 보이는 폭풍만 냅니다(토네이도 View 설계 TN8). 방향이 없으면 움직이지 않으니 내지 않습니다.
                    // 화살표를 끄기 전에 묻습니다.
                    if (center.Origin.StepIndex == 0 && _tornado.IsAimed(center.Origin.SourceId))
                        _sound.Play(CombatEventSounds.TornadoMove());
                    FieldGridMapper centerMapper = _mapper();
                    _tornado.Place(center.Origin.SourceId, center.Center, centerMapper);
                    _tornado.SetDirection(center.Origin.SourceId, center.Center, null, centerMapper);
                    break;
                // 경고면 화살표가 켜지고, 이동이 끝났으면 꺼집니다. 반복 간격이 짧으면 이동 끝 상태가 이미 경고입니다(인계 §4.1).
                case CombatTornadoMemberEvent member:
                    TornadoMemberState final = member.Advance.Final;
                    FieldGridMapper mapper = _mapper();
                    _tornado.Place(final.SourceId, final.Center, mapper);
                    _tornado.SetDirection(final.SourceId, final.Center,
                        final.Phase == TornadoGimmickPhase.Warning ? final.LockedDirection : null, mapper);
                    // 경고는 화살표가 켜졌을 때만입니다. 방향이 없는 경고는 예고할 것이 없습니다.
                    if (final.Phase == TornadoGimmickPhase.Warning && _tornado.IsAimed(final.SourceId))
                        _sound.Play(CombatEventSounds.TornadoWarning(_tornado.Settings.WarningSoundPreset));
                    break;
            }
        }
    }
}
