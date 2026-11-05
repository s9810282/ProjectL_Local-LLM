using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>Structure 숫자의 활성 대여만 소유합니다. 시계는 위젯, 생성·파괴는 조립이 맡습니다.</summary>
    internal sealed class CombatStructureDamage
    {
        private readonly IPoolService<UnitDamageText> _pool;
        private readonly UnitHUDLayer _layer;
        private readonly Action _destroy;
        private readonly Dictionary<long, Rental> _active = new();
        private long _next;
        private bool _released;
        private sealed class Rental
        {
            internal UnitDamageText Widget;
            internal IDisposable Projection;
        }
        internal CombatStructureDamage(IPoolService<UnitDamageText> pool, UnitHUDLayer layer, Action destroy)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _layer = layer != null ? layer : throw new ArgumentNullException(nameof(layer));
            _destroy = destroy ?? throw new ArgumentNullException(nameof(destroy));
        }
        internal void Show(Vector3 world, int damage)
        {
            if (_released) throw new InvalidOperationException("[CombatStructureDamage] 해제한 숫자 창구입니다.");
            if (damage <= 0) return;
            long token = ++_next;
            UnitDamageText widget = _pool.Get();
            var rental = new Rental { Widget = widget };
            _active.Add(token, rental);
            try
            {
                widget.gameObject.SetActive(true);
                rental.Projection = _layer.RegisterWidget((RectTransform)widget.transform, world);
                widget.PlayIndependent(damage, token, Return);
            }
            catch
            {
                Return(token);
                throw;
            }
        }
        private void Return(long token)
        {
            if (!_active.Remove(token, out Rental rental)) return;
            try
            {
                rental.Projection?.Dispose();
            }
            finally
            {
                rental.Widget.Clear();
                rental.Widget.gameObject.SetActive(false);
                _pool.Return(rental.Widget);
            }
        }
        internal void CancelAll()
        {
            List<Exception> errors = null;
            foreach (long token in new List<long>(_active.Keys))
            {
                try
                {
                    Return(token);
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            if (errors != null) throw new AggregateException("Structure 숫자 회수 실패입니다.", errors);
        }
        internal void Release()
        {
            if (_released) return;
            _released = true;
            try
            {
                CancelAll();
            }
            finally
            {
                _destroy();
            }
        }
    }
}
