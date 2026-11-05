using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment
{
    /// <summary>
    /// 던짐 사건의 빨려 들어갈 중심과 공유 설정입니다. 중심은 접촉 순간 자리에서 시작해 움직이는 폭풍을 따라갑니다(토네이도 View 설계 TN10).
    /// 숨겨진 폭풍은 몸이 없어 접촉 자리에 머물고 나머지는 같은 길로 재생합니다.
    /// </summary>
    internal readonly struct CombatTornadoLiftTarget
    {
        private readonly Func<Vector3> _center;

        /// <param name="center">지금 중심의 월드 자리를 답합니다. 연출 동안 매 프레임 묻습니다.</param>
        internal CombatTornadoLiftTarget(Func<Vector3> center, TornadoPresentationSettingsData settings)
        {
            _center = center ?? throw new ArgumentNullException(nameof(center));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal Vector3 Center
        {
            get
            {
                Vector3 center = _center();
                if (!float.IsFinite(center.x) || !float.IsFinite(center.y) || !float.IsFinite(center.z))
                    throw new InvalidOperationException("토네이도 연출 중심은 유한해야 합니다.");
                return center;
            }
        }

        internal TornadoPresentationSettingsData Settings { get; }
    }
}
