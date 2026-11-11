using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem
{
    /// <summary>풀에서 사용하는 효과 프리팹 루트의 월드 시작 위치 보정입니다.</summary>
    [DisallowMultipleComponent]
    public sealed class EffectPrefabOriginOffset : MonoBehaviour
    {
        [Tooltip("이 효과 프리팹 전체의 시작 위치에 더할 월드 좌표 보정입니다. 이펙트 프리팹 루트에 둡니다.")]
        [SerializeField] private Vector3 worldOffset;

        internal Vector3 WorldOffset
        {
            get
            {
                if (!float.IsFinite(worldOffset.x) || !float.IsFinite(worldOffset.y) || !float.IsFinite(worldOffset.z))
                    throw new InvalidOperationException($"[EffectPrefabOriginOffset] 유한하지 않은 위치 보정입니다: {name}.");
                return worldOffset;
            }
        }
    }
}
