using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 낙석 사건의 표시입니다(낙석 View 설계 RF1). 경고는 터널 폭발과 경고 칸을 켜고 다음 사건을 막지 않습니다. 터널 자리가 없는 스테이지는 폭발 없이 경고 칸만 켭니다.
    /// 충돌은 경고 칸을 끄고, 경고했던 칸(<c>Expected.WarnedCells</c>)마다 맵의 능선에서(능선이 없는 맵은 칸 위에서) 돌을 떨어뜨립니다. 돌이 칸에 닿으면 그 칸에 충돌 큐가 나오고, 모두 착지하면 사건이 끝나 맞은 유닛의 반응이 이어집니다.
    /// </summary>
    internal sealed class CombatRockfallEventHandler : CombatEventHandler
    {
        private readonly ICombatRockfallField _rockfall;
        private readonly CombatMomentCuePlayback _cues;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatEffectWindow _effects;
        private readonly CombatCameraWindow _camera;
        private readonly CombatShownFacts _facts;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatEventAudio _sound;

        internal CombatRockfallEventHandler(ICombatRockfallField rockfall, CombatMomentCuePlayback cues, CombatPieceRunPlayer pieces,
            CombatEffectWindow effects, CombatCameraWindow camera, CombatShownFacts facts, Func<FieldGridMapper> mapper, CombatEventAudio sound)
        {
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            _cues = cues ?? throw new ArgumentNullException(nameof(cues));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
        }

        internal override void ApplyFacts(CombatEvent e)
        {
            if (e is not CombatRockfallEvent rockfall)
                return;

            // 경고 칸이 없으면 돌이 떨어지지 않는 경고라 폭발·소리도 내지 않습니다(Core가 빈 칸을 허용합니다).
            if (rockfall.Step != RockfallGimmickStep.Warning || rockfall.Final.WarnedCells.Count == 0)
            {
                _rockfall.ClearWarning();
                return;
            }

            _rockfall.ShowWarning(rockfall.Final.WarnedCells, _mapper());
            if (_rockfall.TryGetTunnelPose(out Pose tunnel))
            {
                _cues.PlayAccent(_rockfall.Settings.WarningCues, CombatCuePoints.ForCell(tunnel));
                _sound.Play(CombatEventSounds.RockfallWarning());
            }
        }

        internal override bool TryBeginAfterFacts(CombatEvent e, Action completed, int version)
        {
            if (e is not CombatRockfallEvent { Step: RockfallGimmickStep.Impact } rockfall || rockfall.Expected.WarnedCells.Count == 0)
                return false;

            RockfallVisualData settings = _rockfall.Settings;
            FieldGridMapper mapper = _mapper();
            var composition = CombatComposition.ForAction(CombatPieceCompletion.General);
            var sinks = new CombatCueSinks(_effects, _camera, null, null);
            SplitCells(rockfall, out List<CellCoord> empty, out List<CellCoord> hit);
            for (int i = 0; i < empty.Count; i++)
                AddDrop(composition, sinks, settings, mapper, empty[i], i * settings.DropIntervalSeconds);
            // 유닛이 맞는 칸은 빈 칸 다음에 함께 떨어져, 착지와 곧이어 시작하는 피격 반응이 같은 순간입니다(계획 RP-6).
            float hitDelay = empty.Count * settings.DropIntervalSeconds;
            for (int i = 0; i < hit.Count; i++)
                AddDrop(composition, sinks, settings, mapper, hit[i], hitDelay);

            return _pieces.TryBegin(composition, completed, version);
        }

        private void AddDrop(CombatComposition composition, CombatCueSinks sinks, RockfallVisualData settings, FieldGridMapper mapper, CellCoord cell, float delay)
        {
            Pose landing = CombatCuePoints.CellPose(mapper, cell);
            Vector3 up = landing.rotation * Vector3.up;
            // 돌의 출발 자리·크기·회전은 규칙 한 곳에서 정합니다(설계 RG5). 터널은 폭발 자리일 뿐 돌은 읽지 않습니다(RG6).
            CombatRockfallDrop dropLook = _rockfall.TryGetRidge(out Vector3 ridgeStart, out Vector3 ridgeEnd)
                ? CombatRockfallDrops.FromRidge(landing.position, up, ridgeStart, ridgeEnd, settings)
                : CombatRockfallDrops.Straight(landing.position, up, settings);
            int dropIndex = composition.Add(new CombatRockfallDropPiece(_rockfall, dropLook, settings.DropSeconds),
                CombatPieceStart.AtStart(delay), waits: true);
            CombatCueComposer.Add(composition, new CombatCueAnchors().Set(CombatCueAnchor.End, dropIndex, CombatPieceSignal.End),
                settings.ImpactCues, CombatCuePoints.ForCell(landing), sinks);
            // 돌마다 기존 착지 종료 신호에 소리를 연결합니다. 전체 낙석 완료 시에는 중복 재생하지 않습니다.
            composition.Add(new CombatEventSoundPiece(_sound, CombatEventSounds.RockfallImpact),
                CombatPieceStart.After(dropIndex, CombatPieceSignal.End));
        }

        // 경고 칸을 유닛이 맞는 칸과 빈 칸으로 나눕니다. 줄을 따라 쓸려 보이지 않게 빈 칸 순서를 섞습니다(화면 연출이라 규칙에 닿지 않는 View 난수입니다).
        private void SplitCells(CombatRockfallEvent rockfall, out List<CellCoord> empty, out List<CellCoord> hit)
        {
            var occupied = new HashSet<CellCoord>();
            foreach (EntityId id in rockfall.RecipientIds)
            {
                CharacterCombatFacts recipient = _facts.FindCharacter(id);
                if (recipient != null)
                    occupied.UnionWith(recipient.OccupiedCells);
            }

            empty = new List<CellCoord>();
            hit = new List<CellCoord>();
            foreach (CellCoord cell in rockfall.Expected.WarnedCells)
                (occupied.Contains(cell) ? hit : empty).Add(cell);
            for (int i = empty.Count - 1; i > 0; i--)
            {
                int other = UnityEngine.Random.Range(0, i + 1);
                (empty[i], empty[other]) = (empty[other], empty[i]);
            }
        }
    }
}
