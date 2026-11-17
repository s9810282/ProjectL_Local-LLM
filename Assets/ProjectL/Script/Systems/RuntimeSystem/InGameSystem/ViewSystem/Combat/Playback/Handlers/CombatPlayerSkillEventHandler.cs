using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 플레이어 액티브 스킬 사건 51(<see cref="CombatPlayerSkillAttackEvent"/>)의 표시입니다(전투 스킬 View 설계 SK3). 스킬 종류를 보지 않고
    /// 사건 모양이 연출 길을 정합니다(설계 P5): 투사체 경로가 있으면 꺾인 직선 비행(접촉마다 그 대상을 풂), 없으면 범위(맞는 순간 모두 풂) + 재생 강조입니다.
    /// </summary>
    /// <remarks>
    /// <para>공격 연출 값은 스킬 외형 표의 (스킬, 강화) 줄에서 읽고, 스킬·강화 키는 결과의 <c>PlayerSkill</c>에서 결과 시작 때 읽어 둡니다(설계 P7).
    /// 어느 연출 값이 맞는지는 경로 유무로 검사합니다 — 경로 비행은 직선 투사체 묶음, 범위는 투사체 없는 묶음이어야 합니다(맞지 않으면 설정 오류로 예외).</para>
    /// <para>피해·사망·기절·넉백 걸음·연쇄 공격은 기존 사건으로 오고, 출처를 연 이 사건에 피해 근원으로 맺혀 해제 때 재생됩니다(사건 형식 표의 공격 출처 칸).
    /// 시작하지 못하면 틀의 일반 경로(소리·사실 반영)로 이어 갑니다.</para>
    /// </remarks>
    internal sealed class CombatPlayerSkillEventHandler : CombatEventHandler
    {
        private readonly CombatAttackStart _start;
        private readonly CombatCueSources _cueSources;
        private readonly CombatShownFacts _facts;
        private bool _hasSkill;
        private PlayerSkillKey _skill;
        private PlayerSkillUpgradeKey _upgrade;
        private bool _missingWarned;

        internal CombatPlayerSkillEventHandler(CombatAttackStart start, CombatCueSources cueSources, CombatShownFacts facts)
        {
            _start = start ?? throw new ArgumentNullException(nameof(start));
            _cueSources = cueSources ?? throw new ArgumentNullException(nameof(cueSources));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
        }

        /// <summary>결과 재생 시작입니다. 그 결과의 스킬·강화 키를 읽어 둡니다(스킬 결과가 아니면 없음).</summary>
        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            PlayerSkillResult skill = result.PlayerSkill;
            _hasSkill = skill != null;
            _skill = skill?.SkillKey ?? PlayerSkillKey.None;
            _upgrade = skill?.UpgradeKey ?? PlayerSkillUpgradeKey.None;
        }

        /// <summary>결과 재생 끝·취소입니다.</summary>
        internal void Clear()
        {
            _hasSkill = false;
            _skill = PlayerSkillKey.None;
            _upgrade = PlayerSkillUpgradeKey.None;
        }

        // 범위 사건은 재생 중 영향 칸을 재생 강조에 칠합니다(포효와 같은 길, 계획 TP-12). 경로 비행은 탄이 보여 칠하지 않습니다.
        internal override void CollectHighlight(CombatEvent e, List<CellCoord> cells)
        {
            var skill = (CombatPlayerSkillAttackEvent)e;
            if (skill.Attack.ProjectilePath.Count > 0)
                return;
            for (int i = 0; i < skill.Attack.AffectedCells.Count; i++)
                cells.Add(skill.Attack.AffectedCells[i]);
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var skill = (CombatPlayerSkillAttackEvent)e;
            // 스킬 결과 밖의 사건 51은 Core 계약상 없습니다. 오면 연출 없이 사실만 반영하고 한 번 알립니다.
            if (!_hasSkill)
            {
                if (!_missingWarned)
                {
                    _missingWarned = true;
                    Debug.LogWarning("[CombatPlayerSkillEventHandler] 스킬 결과가 아닌 결과에 스킬 사건이 있습니다. 연출 없이 사실만 반영합니다.");
                }
                return false;
            }

            PlayerSkillVisualLook look = _cueSources.PlayerSkills.Get(_skill, _upgrade);
            CombatAttackPresentationData settings = look.Attack;
            CombatAttackShape shape = CombatAttackShape.ForPlayerSkill(skill, CasterRotationOf(skill));
            if (shape.SkillPath == null)
                return _start.TryBeginArea(shape, settings, completed, version);
            if (settings.Kind != CombatAttackPresentationKind.Linear)
                throw new InvalidOperationException(
                    $"[CombatPlayerSkillEventHandler] 투사체 경로가 있는 스킬({_skill}/{_upgrade})의 공격 연출은 직선 투사체 묶음이어야 합니다({settings.Kind}). PlayerSkillVisual 표를 확인하세요.");
            return _start.TryBegin(shape, settings, completed, version);
        }

        // 경로가 없을 때의 공격 방향입니다. 보인 사실의 시전자 방향이고, 없으면 기본 방향입니다.
        private Rotation4 CasterRotationOf(CombatPlayerSkillAttackEvent skill) =>
            _facts.FindCharacter(skill.Attack.ActorId)?.Rotation ?? default;
    }
}
