using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectL.Systems.DataBaseSystem.Data.View
{
    /// <summary>
    /// View 표 여럿이 같은 뜻으로 쓰는 프리팹·표시 보정 검사의 원본입니다(검사 후속 정리 DM2).
    /// 쓰는 곳마다 다른 요구(로비 Layer·허용 컴포넌트·시설, 맵 LineRenderer)는 그 표가 갖습니다.
    /// 오류 문구 앞에는 부르는 쪽이 넘긴 <c>owner</c>(표 이름과 칸)를 붙입니다.
    /// </summary>
    internal static class ViewPrefabRules
    {
        /// <summary>참조가 있고 씬 인스턴스가 아닌 실제 프리팹 에셋인지 봅니다. 실제 에셋인지는 에디터에서만 확인할 수 있습니다.</summary>
        internal static void RequirePrefabAsset(GameObject prefab, string owner)
        {
            if (prefab == null)
                throw Invalid(owner, "프리팹 참조가 없거나 파괴되었습니다.");
            if (prefab.scene.IsValid())
                throw Invalid(owner, "씬 인스턴스 대신 프리팹 에셋을 지정해야 합니다.");
#if UNITY_EDITOR
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw Invalid(owner, "실제 프리팹 에셋을 지정해야 합니다.");
#endif
        }

        /// <summary>프리팹 원본의 루트가 활성이고 부모가 없는지 봅니다. null은 거르지 않으니 <see cref="RequirePrefabAsset"/> 뒤에 부릅니다.</summary>
        internal static void RequireActiveRoot(GameObject prefab, string owner)
        {
            if (!prefab.activeSelf || prefab.transform.parent != null)
                throw Invalid(owner, "프리팹 원본의 루트는 활성이고 부모가 없어야 합니다.");
        }

        /// <summary>
        /// 루트가 위치 0·회전 identity·축척 1인지 봅니다. 회전은 w가 −1인 identity도 받습니다.
        /// null은 거르지 않으니 <see cref="RequirePrefabAsset"/> 뒤에 부릅니다.
        /// </summary>
        internal static void RequireIdentityRoot(GameObject prefab, string owner)
        {
            Transform root = prefab.transform;
            Quaternion rotation = root.localRotation;
            if (!root.localPosition.Equals(Vector3.zero) || !root.localScale.Equals(Vector3.one) ||
                rotation.x != 0f || rotation.y != 0f || rotation.z != 0f || Mathf.Abs(rotation.w) != 1f)
                throw Invalid(owner, "프리팹 루트는 위치 0·회전 identity·축척 1이어야 합니다.");
        }

        /// <summary>정적 메시 렌더러는 같은 객체에 정점이 있는 Mesh와 빠진 것 없는 Material 하나 이상이 있어야 합니다.</summary>
        internal static void RequireStaticMesh(MeshRenderer renderer, string owner)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
                throw Invalid(owner, $"'{renderer.name}'의 MeshRenderer에는 같은 객체의 MeshFilter와 정점이 있는 Mesh가 필요합니다.");
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0 || Array.Exists(materials, material => material == null))
                throw Invalid(owner, $"'{renderer.name}'에 Material이 없거나 빠진 Material이 있습니다.");
        }

        /// <summary>렌더러가 켜져 있고 <paramref name="root"/>까지 모든 객체가 활성인지입니다. 루트 밖 렌더러는 거짓입니다.</summary>
        internal static bool IsShown(Renderer renderer, Transform root) =>
            renderer.enabled && IsActiveWithin(renderer.transform, root);

        /// <summary><paramref name="node"/>부터 <paramref name="root"/>까지 모든 객체가 활성인지입니다. 루트 밖 객체는 거짓입니다.</summary>
        internal static bool IsActiveWithin(Transform node, Transform root)
        {
            for (Transform current = node; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                    return false;
                if (current == root)
                    return true;
            }
            return false;
        }

        /// <summary>복제본에 주는 표시 보정(위치·Y 회전·배율)은 유한하고 배율은 양수여야 합니다.</summary>
        internal static void RequirePresentation(Vector3 offset, float yaw, float scale, string owner)
        {
            if (!IsFinite(offset) || !float.IsFinite(yaw))
                throw Invalid(owner, "표시 위치와 회전은 유한한 값이어야 합니다.");
            if (!float.IsFinite(scale) || scale <= 0f)
                throw Invalid(owner, "표시 배율은 유한한 양수여야 합니다.");
        }

        internal static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        /// <summary>회전 네 성분이 모두 유한한지입니다. 로비·아티팩트의 자세 검사가 함께 씁니다(로비 리팩토링 2차 설계 N-49).</summary>
        internal static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);

        private static InvalidOperationException Invalid(string owner, string reason) => new($"{owner}: {reason}");
    }
}
