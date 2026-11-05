using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>추가 공격 지점의 직렬화 입력입니다. 값으로 복사하며 외부 변경용 접근자는 두지 않습니다.</summary>
    [Serializable]
    internal struct CombatAttackPoint
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform effectPoint;

        internal CombatAttackPoint(Transform muzzle, Transform effectPoint)
        {
            this.muzzle = muzzle;
            this.effectPoint = effectPoint;
        }

        internal Transform Muzzle => muzzle;
        internal Transform EffectPoint => effectPoint;
    }

    /// <summary>연결 때 복사한 공격 지점 정의입니다. 지점 번호 해석과 기본 지점 0의 대응을 한 곳에서 맡습니다.</summary>
    internal sealed class CombatAttackPoints
    {
        private readonly CombatAttackPoint[] _points;

        internal CombatAttackPoints(Transform muzzle, Transform effectPoint, IReadOnlyList<CombatAttackPoint> additional)
        {
            int count = additional?.Count ?? 0;
            _points = new CombatAttackPoint[count + 1];
            _points[0] = new CombatAttackPoint(muzzle, effectPoint);
            for (int i = 0; i < count; i++)
                _points[i + 1] = additional[i];
        }

        internal bool TryGetMuzzle(int point, out Transform muzzle)
        {
            muzzle = TryGet(point, out CombatAttackPoint value) ? value.Muzzle : null;
            return muzzle != null;
        }

        internal bool TryGetEffectPoint(int point, out Transform effectPoint)
        {
            effectPoint = TryGet(point, out CombatAttackPoint value) ? value.EffectPoint : null;
            return effectPoint != null;
        }

        private bool TryGet(int point, out CombatAttackPoint value)
        {
            if (point < 0) throw new ArgumentOutOfRangeException(nameof(point), point, "공격 지점 번호는 0 이상이어야 합니다.");
            if (point >= _points.Length)
            {
                value = default;
                return false;
            }
            value = _points[point];
            return true;
        }
    }
}
