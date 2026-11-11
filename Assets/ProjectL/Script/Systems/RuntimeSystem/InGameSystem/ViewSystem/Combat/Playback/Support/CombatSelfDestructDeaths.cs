using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 한 결과 안에서 터질 유닛과 그 몸 유지의 한 곳입니다(자폭 깜빡임 설계 SD1·SD5).
    /// "터지는지"는 결과 안의 Core 자폭 발동(<see cref="TraitActivationKind.SelfDestructExploded"/>) 하나로만 정합니다. View는 자폭 특성 보유·피해 사망·낙사·승패를 판정하지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 결과를 시작할 때 사건을 한 번 훑어 자폭 발동의 출처 유닛과 그 발동 줄의 깜빡임 값을 모읍니다. 사망 처리기는 터질 유닛을 사망 반응 없이 몸만 남기고
    /// (깜빡임 값이 있고 색 층을 쓸 수 있으면 깜빡임을 덧붙임), 몸 유지를 여기에 맡깁니다. 그 유닛의 자폭 발동 사건이 시작하면 특성 처리기가 <see cref="Detonate"/>로 놓아,
    /// 몸이 폭발(몸이 부서지는 이펙트)과 같은 순간에 사라집니다. 쓰러짐·디졸브·시체는 보이지 않습니다.
    /// </para>
    /// <para>
    /// 남은 유지는 결과 취소·결과 끝(최종 사실)·다음 결과 시작에서 목록과 함께 모두 놓습니다(<see cref="Clear"/>). 그래서 붙잡힌 모델이 화면에 남지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatSelfDestructDeaths
    {
        private readonly CombatActivationRows _rows;
        // 이 결과에서 터질 유닛과 그 깜빡임 값입니다. 깜빡임 줄이 없으면 값은 null입니다(그 유닛은 지금 사망 반응).
        private readonly Dictionary<EntityId, CombatDeathBlinkVisualData> _blinks = new();
        // 깜빡임 사망이 맡긴 몸 유지입니다. 그 유닛의 자폭 발동이 시작하면 놓습니다.
        private readonly Dictionary<EntityId, ICombatUnitDeathHold> _holds = new();

        internal CombatSelfDestructDeaths(CombatActivationRows rows)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        /// <summary>
        /// 결과 하나를 시작합니다. 남은 유지를 먼저 놓고, 사건 안의 자폭 발동을 출처마다 모읍니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">한 결과에 같은 유닛의 자폭 발동이 둘일 때입니다(Core는 유닛마다 한 번만 냄, 계획 TP-6).</exception>
        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Clear();
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is not CombatEffectsEvent effects) continue;
                for (int j = 0; j < effects.Traits.Count; j++)
                {
                    TraitActivationSnapshot trait = effects.Traits[j];
                    if (trait.TraitActivationKind != TraitActivationKind.SelfDestructExploded) continue;
                    if (_blinks.ContainsKey(trait.SourceUnitId))
                        throw new InvalidOperationException(
                            $"[CombatSelfDestructDeaths] 한 결과에 같은 유닛의 자폭 발동이 둘 있습니다(Core는 유닛마다 한 번만 냄): {trait.SourceUnitId}");
                    // 방향 없는(Any) 줄을 읽습니다. 방향별 줄은 결속 방어력 줄에서만 만들 수 있어(TraitActivationVisual.AllowsDirection) 자폭 줄은 늘 Any 하나이고,
                    // 특성 처리기가 폭발 큐를 읽는 줄(방향까지 본 줄, 없으면 Any)과 같은 줄입니다(묶음 끝 검토 F2).
                    _blinks.Add(trait.SourceUnitId, _rows.Of(trait).DeathBlink);
                }
            }
        }

        /// <summary>그 유닛이 이 결과에서 터지는지입니다. 터질 유닛은 깜빡임 값과 상관없이 사망 반응 없이 몸을 남깁니다(계획 TP-5, 2026-11-11 사용자 "늘 몸 남김").</summary>
        internal bool IsExploding(EntityId unit) => _blinks.ContainsKey(unit);

        /// <summary>그 유닛이 이 결과에서 터지고 그 발동 줄에 깜빡임 값이 있으면 true입니다.</summary>
        internal bool TryGetBlink(EntityId unit, out CombatDeathBlinkVisualData blink) =>
            _blinks.TryGetValue(unit, out blink) && blink != null;

        /// <summary>깜빡임 사망의 몸 유지를 맡깁니다. 그 유닛의 자폭 발동이 시작할 때 놓습니다.</summary>
        /// <exception cref="InvalidOperationException">이 결과에서 터지지 않는 유닛이거나 이미 맡긴 유지가 있을 때입니다.</exception>
        internal void Keep(EntityId unit, ICombatUnitDeathHold hold)
        {
            if (hold == null) throw new ArgumentNullException(nameof(hold));
            if (!_blinks.ContainsKey(unit))
                throw new InvalidOperationException($"[CombatSelfDestructDeaths] 이 결과에서 터지지 않는 유닛의 몸 유지입니다: {unit}");
            if (_holds.ContainsKey(unit))
                throw new InvalidOperationException($"[CombatSelfDestructDeaths] 같은 유닛의 몸 유지를 두 번 맡겼습니다: {unit}");
            _holds.Add(unit, hold);
        }

        /// <summary>
        /// 자폭 발동이 시작했습니다. 그 출처의 몸 유지가 있으면 놓아 몸을 내립니다. 자폭 발동이 아니거나 유지가 없으면(지금 사망 반응을 쓴 유닛) 아무것도 하지 않습니다.
        /// </summary>
        internal void Detonate(TraitActivationSnapshot trait)
        {
            if (trait == null) throw new ArgumentNullException(nameof(trait));
            if (trait.TraitActivationKind != TraitActivationKind.SelfDestructExploded) return;
            if (!_holds.Remove(trait.SourceUnitId, out ICombatUnitDeathHold hold)) return;
            hold.Release();
        }

        /// <summary>
        /// 이 결과의 터질 유닛 목록을 비우고 남은 몸 유지를 모두 놓습니다. 결과 취소·결과 끝·다음 결과 시작에서 부릅니다(낙사 표지와 같은 수명, 묶음 끝 검토 F5).
        /// 놓기가 실패해도 목록은 비우고, 나머지를 놓은 뒤 모아 던집니다.
        /// </summary>
        internal void Clear()
        {
            _blinks.Clear();
            ReleaseAll();
        }

        // 남은 몸 유지를 모두 놓습니다. 놓기가 실패해도 나머지를 놓고 끝에 모아 던집니다.
        private void ReleaseAll()
        {
            if (_holds.Count == 0) return;
            var holds = new List<ICombatUnitDeathHold>(_holds.Values);
            _holds.Clear();
            List<Exception> errors = null;
            for (int i = 0; i < holds.Count; i++)
                CleanupErrors.Try(holds[i].Release, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatSelfDestructDeaths] 터질 유닛의 몸 유지를 놓지 못했습니다.");
        }
    }
}
