using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 특성 발동 하나의 "행동 끝" 이펙트 묶음입니다(이펙트 끝 규칙 설계 EL11, 계획 TP-E3·TP-E27, Ef.7). 그 발동이 연 유닛 행동 수와,
    /// 끝 <c>UnitAction</c> 큐로 낸 이펙트 손잡이를 들고, 마지막 행동이 끝나면(또는 연 행동이 하나도 없으면 행동을 다 연 순간) 이펙트를 모두 거둡니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 쓰는 순서: 만든 뒤 큐를 내고(<see cref="Hold"/>) 행동마다 <see cref="Open"/>으로 닫기 함수를 받아 행동 완료에 걸고, 행동을 다 연 뒤 <see cref="Seal"/>을 부릅니다.
    /// 기존 위치 이동 행동과 시간 후 동시 교환 모두 발동 단위로 묶어 두 몸의 이펙트를 같이 끝냅니다. 동시 교환만 즉시 회수 모드를 사용합니다.
    /// </para>
    /// <para>
    /// 끝난 뒤에 지연 때문에 늦게 나는 큐는 내지 않습니다(<see cref="IsEnded"/>, 계획 TP-E27). 결과 취소의 전부 지우기와 결과 끝의 안전장치는 창구가 따로 합니다
    /// (<c>CombatEffectWindow.RecallActionBound</c>, 계획 TP-E4).
    /// </para>
    /// </remarks>
    internal sealed class CombatEffectActionGroup
    {
        private readonly List<CombatEffectHandle> _handles = new();
        private int _open;
        private bool _sealed;
        private bool _recallImmediately;

        internal CombatEffectActionGroup(bool recallImmediately = false)
        {
            _recallImmediately = recallImmediately;
        }

        /// <summary>이 발동의 행동이 모두 끝나 이펙트를 거뒀는지입니다. 그 뒤 낼 이펙트는 내지 않습니다.</summary>
        internal bool IsEnded { get; private set; }

        /// <summary>행동 끝 이펙트 하나를 맡깁니다. 이미 끝났으면 바로 거둡니다.</summary>
        internal void Hold(CombatEffectHandle handle)
        {
            if (IsEnded)
            {
                Recall(handle);
                return;
            }
            _handles.Add(handle);
        }

        /// <summary>행동 하나를 엽니다. 돌려준 함수를 그 행동의 완료(또는 시작 실패)에 한 번 부릅니다. 두 번 불러도 한 번만 셉니다.</summary>
        internal Action Open()
        {
            if (_sealed) throw new InvalidOperationException("[CombatEffectActionGroup] 행동을 다 연 뒤에는 새 행동을 열 수 없습니다.");
            _open++;
            bool closed = false;
            return () =>
            {
                if (closed) return;
                closed = true;
                _open--;
                TryEnd();
            };
        }

        /// <summary>이 발동의 행동을 다 열었습니다. 연 행동이 없거나 모두 끝났으면 이펙트를 거둡니다.</summary>
        internal void Seal()
        {
            _sealed = true;
            TryEnd();
        }

        /// <summary>취소·시작 실패 때 이 발동의 이펙트만 즉시 정리합니다. 늦은 닫기는 아무것도 다시 내지 않습니다.</summary>
        internal void EndImmediately()
        {
            if (IsEnded) return;
            _sealed = true;
            _recallImmediately = true;
            End();
        }

        private void TryEnd()
        {
            if (!_sealed || _open > 0 || IsEnded)
                return;
            End();
        }

        private void End()
        {
            IsEnded = true;
            List<Exception> errors = null;
            for (int i = 0; i < _handles.Count; i++)
            {
                CombatEffectHandle handle = _handles[i];
                CleanupErrors.Try(() => Recall(handle), ref errors);
            }
            _handles.Clear();
            CleanupErrors.ThrowIfAny(errors, "[CombatEffectActionGroup] 발동 이펙트 회수에 실패했습니다.");
        }

        private void Recall(CombatEffectHandle handle)
        {
            if (_recallImmediately)
                handle.RecallImmediately();
            else
                handle.Recall();
        }
    }
}
