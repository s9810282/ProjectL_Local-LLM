using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 특성 발동 연출 줄 값을 연결 동안 보관하는 한 곳입니다(묶음 끝 검토 M2). 특성 처리기(사건 목록 안 발동)와 대신 맞기 앞 항목(공격 결과 안 가로채기,
    /// <see cref="CombatGuardLead"/>)이 같이 씁니다.
    /// </summary>
    /// <remarks>
    /// 줄 값은 읽을 때마다 새로 만들어지므로 (특성, 발동 종류)마다 처음 읽을 때 한 번 만들어 둡니다(줄이 없으면 그 사실도 보관). SO를 에디터에서 고치면 다음 전투 진입부터 보입니다.
    /// 줄을 찾는 규칙은 큐 값 답(<see cref="CombatCueSources.TryGetActivation(TraitKey, TraitActivationKind, CombatActivationDirection, out TraitActivationVisual)"/>) 하나이고,
    /// 방향 줄이 없으면 같은 종류의 Any 줄로 가는 것은 이곳 하나입니다(이펙트 끝 규칙 설계 EL7, 계획 TP-E18). 방향은 피드백 계획이 정해 넘깁니다.
    /// </remarks>
    internal sealed class CombatActivationRows
    {
        private readonly CombatCueSources _sources;
        private readonly Dictionary<(TraitKey Trait, TraitActivationKind Kind, CombatActivationDirection Direction), CombatActivationRow> _rows = new();

        internal CombatActivationRows(CombatCueSources sources)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        }

        /// <summary>그 발동의 방향을 가리지 않는(Any) 연출 줄입니다. 줄이 없으면 <see cref="CombatActivationRow.Exists"/>가 거짓인 기본값입니다.</summary>
        internal CombatActivationRow Of(TraitActivationSnapshot trait) => Of(trait, CombatActivationDirection.Any);

        /// <summary>그 발동·방향의 연출 줄입니다. 방향 줄이 없으면 같은 종류의 Any 줄입니다(이펙트 끝 규칙 설계 EL7).</summary>
        internal CombatActivationRow Of(TraitActivationSnapshot trait, CombatActivationDirection direction)
        {
            if (trait == null) throw new ArgumentNullException(nameof(trait));
            return Of(trait.TraitKey, trait.TraitActivationKind, direction);
        }

        /// <summary>(특성, 발동 종류)의 Any 연출 줄입니다.</summary>
        internal CombatActivationRow Of(TraitKey trait, TraitActivationKind kind) => Of(trait, kind, CombatActivationDirection.Any);

        /// <summary>(특성, 발동 종류, 방향)의 연출 줄입니다. 방향 줄이 없으면 Any 줄입니다.</summary>
        internal CombatActivationRow Of(TraitKey trait, TraitActivationKind kind, CombatActivationDirection direction)
        {
            (TraitKey, TraitActivationKind, CombatActivationDirection) key = (trait, kind, direction);
            if (!_rows.TryGetValue(key, out CombatActivationRow row))
            {
                if (_sources.TryGetActivation(trait, kind, direction, out TraitActivationVisual activation))
                    row = new CombatActivationRow(activation.Cues, activation.AttackPresentation, activation.UnitActions,
                        activation.CueTiming, activation.BodyText, _sources.TraitName(trait), activation.PositionSwap, activation.DeathBlink);
                else
                    row = direction == CombatActivationDirection.Any ? default : Of(trait, kind, CombatActivationDirection.Any);
                _rows.Add(key, row);
            }
            return row;
        }
    }

    /// <summary>
    /// 발동 연출 줄 하나의 값입니다: 큐, 추가 공격 표시 덮어쓰기, 유닛 행동, 선택적 위치 교환 시간 연출, 선택적 터지기 전 깜빡임입니다.
    /// 기본값은 "줄 없음"입니다.
    /// </summary>
    internal readonly struct CombatActivationRow
    {
        internal CombatActivationRow(CombatEventCues cues, CombatAttackPresentationData presentation, IReadOnlyList<CombatUnitActionData> actions,
            CombatActivationCueTiming timing = default, CombatBodyTextData text = null, string name = null,
            CombatPositionSwapVisualData positionSwap = null, CombatDeathBlinkVisualData deathBlink = null)
        {
            Cues = cues ?? throw new ArgumentNullException(nameof(cues));
            Presentation = presentation;
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            Timing = timing;
            Text = text;
            Name = name;
            PositionSwap = positionSwap;
            DeathBlink = deathBlink;
        }

        /// <summary>터지기 전 깜빡임입니다(자폭 깜빡임 설계 SD2). 없으면 null이고, 터질 유닛은 지금 사망 반응을 씁니다.</summary>
        internal CombatDeathBlinkVisualData DeathBlink { get; }

        internal CombatActivationCueTiming Timing { get; }
        internal CombatBodyTextData Text { get; }
        internal string Name { get; }

        internal bool Exists => Cues != null;
        internal CombatEventCues Cues { get; }
        internal CombatAttackPresentationData Presentation { get; }
        internal IReadOnlyList<CombatUnitActionData> Actions { get; }
        internal CombatPositionSwapVisualData PositionSwap { get; }
    }
}
