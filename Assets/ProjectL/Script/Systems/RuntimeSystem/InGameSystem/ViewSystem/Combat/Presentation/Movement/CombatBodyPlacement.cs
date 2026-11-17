using System;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 월드에 놓인 상자를 접촉 몸(필드 로컬 XZ 외접 사각형, <see cref="CombatBody"/>)으로 바꿉니다(히트 타이밍 설계 H4).
    /// 유닛·오브젝트 몸이 같은 변환을 씁니다. 회전한 상자는 외접 사각형이며 90도 단위 회전이면 정확합니다.
    /// </summary>
    internal static class CombatBodyPlacement
    {
        /// <summary>월드 중심과 두 반 축(상자 로컬 x·z 방향의 반 크기 벡터)으로 몸을 만듭니다.</summary>
        internal static CombatBody FromWorldBox(Vector3 worldCenter, Vector3 worldHalfX, Vector3 worldHalfZ, FieldGridMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Vector3 local = mapper.WorldToLocal(worldCenter);
            Vector3 localX = mapper.WorldToLocal(worldCenter + worldHalfX) - local;
            Vector3 localZ = mapper.WorldToLocal(worldCenter + worldHalfZ) - local;
            return new CombatBody(CombatBody.Flatten(local), new Vector2(
                Mathf.Abs(localX.x) + Mathf.Abs(localZ.x), Mathf.Abs(localX.z) + Mathf.Abs(localZ.z)));
        }

        /// <summary>
        /// 놓인 Transform 기준의 상자(<paramref name="boxCenter"/>·<paramref name="boxSize"/>는 그 Transform 로컬 값, BoxCollider와 같은 뜻)를 몸으로 바꿉니다.
        /// Transform의 현재 위치·회전·월드 크기를 그대로 씁니다. 자세 대여가 없는 표시(오브젝트)에만 씁니다.
        /// </summary>
        internal static CombatBody FromPlacedBox(Transform placed, Vector3 boxCenter, Vector3 boxSize, FieldGridMapper mapper)
        {
            if (placed == null) throw new ArgumentNullException(nameof(placed));
            return FromWorldBox(placed.TransformPoint(boxCenter),
                placed.TransformVector(new Vector3(boxSize.x * .5f, 0f, 0f)),
                placed.TransformVector(new Vector3(0f, 0f, boxSize.z * .5f)), mapper);
        }
    }
}
