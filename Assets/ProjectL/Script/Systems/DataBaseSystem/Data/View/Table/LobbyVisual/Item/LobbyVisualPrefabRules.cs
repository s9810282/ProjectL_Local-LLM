using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>
    /// 로비 정적 표시 프리팹의 검사입니다. 다른 View 표와 같은 뜻의 검사는 <see cref="ViewPrefabRules"/>를 부르고,
    /// 로비만의 요구(모든 노드 Transform 값, 허용 컴포넌트·LOD, 방의 시설 셋)만 여기에 둡니다. Layer는 검사하지 않습니다.
    /// </summary>
    internal static class LobbyVisualPrefabRules
    {
        /// <summary>DB 생성 때 정적 표시 프리팹의 원본·루트·구조를 검사합니다.</summary>
        internal static void Validate(GameObject prefab, string slotName)
        {
            ValidateReference(prefab, slotName);
            ValidateStructure(prefab, slotName);
        }

        /// <summary>
        /// DB 생성 때 방 프리팹을 검사합니다. 시설은 종류마다 정확히 하나이고(수는 <see cref="LobbyFacilityKinds"/>),
        /// 저마다 같은 객체에 활성 BoxCollider 하나를 갖고 서로 중첩하지 않아야 합니다(TP-R13).
        /// 시설마다 방 안의 활성 안내 글자 하나를 연결하며 두 시설이 같은 글자를 쓰지 않습니다(로비 설계 §16).
        /// </summary>
        internal static void ValidateRoom(GameObject room, string slotName)
        {
            ValidateReference(room, slotName, allowAuthoredRootTransform: true);
            Transform root = room.transform;
            LobbyFacilityView[] facilities = room.GetComponentsInChildren<LobbyFacilityView>(true);
            if (facilities.Length != LobbyFacilityKinds.Count)
                throw Invalid(slotName, $"방에는 시설 종류({string.Join(", ", LobbyFacilityKinds.All)})마다 하나씩, 모두 {LobbyFacilityKinds.Count}개가 필요합니다.");

            var colliders = new HashSet<BoxCollider>();
            var labels = new HashSet<TextMeshProUGUI>();
            int kinds = 0;
            foreach (LobbyFacilityView facility in facilities)
            {
                if (!LobbyFacilityKinds.IsDefined(facility.Kind))
                    throw Invalid(slotName, $"시설 '{facility.name}'의 종류가 올바르지 않습니다.");
                int bit = 1 << (int)facility.Kind;
                if ((kinds & bit) != 0)
                    throw Invalid(slotName, "같은 종류의 시설을 중복 등록할 수 없습니다.");
                kinds |= bit;

                BoxCollider[] boxes = facility.GetComponents<BoxCollider>();
                if (boxes.Length != 1 || !boxes[0].enabled)
                    throw Invalid(slotName, $"시설 '{facility.name}'({facility.Kind})에는 같은 객체의 활성 BoxCollider가 정확히 하나 필요합니다.");
                BoxCollider box = boxes[0];
                if (!ViewPrefabRules.IsFinite(box.center) || !ViewPrefabRules.IsFinite(box.size) ||
                    box.size.x <= 0f || box.size.y <= 0f || box.size.z <= 0f)
                    throw Invalid(slotName, $"시설 '{facility.name}'({facility.Kind})의 BoxCollider 중심은 유한하고 크기는 유한한 양수여야 합니다.");
                if (!facility.enabled || !ViewPrefabRules.IsActiveWithin(facility.transform, root))
                    throw Invalid(slotName, "시설과 클릭 영역은 방 안에서 활성이어야 합니다.");
                // 같은 객체의 BoxCollider 하나·[DisallowMultipleComponent]라 시설끼리 Collider를 나눌 수 없습니다. 아래 구조 검사의 허용 목록에만 씁니다(N-30).
                colliders.Add(box);

                TextMeshProUGUI label = facility.Label;
                // IsActiveWithin은 방 밖 객체에 거짓이라 방 안인지도 함께 봅니다(N-30).
                if (label == null || !label.enabled || !ViewPrefabRules.IsActiveWithin(label.transform, root))
                    throw Invalid(slotName, $"시설 '{facility.name}'({facility.Kind})에는 방 안의 활성 안내 글자(TextMeshProUGUI)를 연결해야 합니다.");
                if (!labels.Add(label))
                    throw Invalid(slotName, "두 시설이 같은 안내 글자를 쓸 수 없습니다.");

                foreach (LobbyFacilityView other in facilities)
                {
                    if (other != facility && other.transform.IsChildOf(facility.transform))
                        throw Invalid(slotName, "시설 루트는 서로 중첩할 수 없습니다.");
                }
            }

            ValidateStructure(room, slotName, component =>
                component is LobbyFacilityView || component is BoxCollider box && colliders.Contains(box) ||
                IsRoomUiComponent(component));
        }

        /// <summary>
        /// 방의 시설 안내 글자 머티리얼이 주어진 셰이더인지 DB 생성 때 확인합니다. 방 프리팹 검사는 모두 이 클래스 한 곳에 둡니다.
        /// 글자 연결을 먼저 검사했다는 순서에 기대지 않고, 빠진 글자도 원인 문구로 막습니다(로비 리팩토링 2차 설계 N-58).
        /// </summary>
        internal static void ValidateRoomLabelShader(GameObject room, Shader shader, string slotName)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (shader == null) throw new ArgumentNullException(nameof(shader));
            foreach (LobbyFacilityView facility in room.GetComponentsInChildren<LobbyFacilityView>(true))
            {
                TextMeshProUGUI label = facility.Label;
                if (label == null)
                    throw Invalid(slotName, $"시설 '{facility.name}'({facility.Kind})에는 안내 글자(TextMeshProUGUI)를 연결해야 합니다.");
                Material material = label.fontSharedMaterial;
                if (material == null || material.shader != shader)
                    throw Invalid(slotName,
                        $"시설 '{facility.name}'({facility.Kind})의 안내 글자 머티리얼은 호버 빛 번짐 머티리얼(LobbyFacilityHoverData.GlowMaterial)과 같은 셰이더여야 합니다.");
            }
        }

        /// <summary>로비 방의 시설 안내 UI만 허용하며 바닥과 공용 모델 규칙에는 적용하지 않습니다.</summary>
        private static bool IsRoomUiComponent(Component component)
        {
            Type type = component.GetType();
            return type == typeof(RectTransform) || type == typeof(Canvas) ||
                type == typeof(CanvasRenderer) || type == typeof(CanvasScaler) ||
                type == typeof(GraphicRaycaster) || type == typeof(TextMeshProUGUI);
        }

        /// <summary>방과 순수 모델이 공유하는 프리팹 원본/루트 조건입니다.</summary>
        private static void ValidateReference(GameObject prefab, string slotName, bool allowAuthoredRootTransform = false)
        {
            string owner = Owner(slotName);
            ViewPrefabRules.RequirePrefabAsset(prefab, owner);
            // 루트 Transform 값은 구조 검사(ValidateStructure)가 모든 노드와 함께 봅니다(N-30).
            ViewPrefabRules.RequireActiveRoot(prefab, owner);
            if (!allowAuthoredRootTransform)
                ViewPrefabRules.RequireIdentityRoot(prefab, owner);
        }

        /// <summary>추가 저작 컴포넌트의 허용 판단은 부르는 쪽이 넘깁니다.</summary>
        private static void ValidateStructure(GameObject prefab, string slotName, Func<Component, bool> allowAdditional = null)
        {
            Transform root = prefab.transform;
            bool hasActiveRenderer = false;
            foreach (Transform node in prefab.GetComponentsInChildren<Transform>(true))
            {
                ValidateTransform(node, slotName);
                foreach (Component component in node.GetComponents<Component>())
                {
                    ValidateComponentType(component, node.name, slotName, allowAdditional);

                    if (component is MeshFilter filter && (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0))
                    {
                        throw Invalid(slotName, $"'{node.name}'의 MeshFilter에 정점이 있는 Mesh가 없습니다.");
                    }

                    if (component is MeshRenderer renderer)
                    {
                        ViewPrefabRules.RequireStaticMesh(renderer, Owner(slotName));
                        hasActiveRenderer |= ViewPrefabRules.IsShown(renderer, root);
                    }

                    if (component is LODGroup group)
                    {
                        ValidateLods(group, root, slotName);
                    }
                }
            }

            if (!hasActiveRenderer)
            {
                throw Invalid(slotName, "유효한 활성 MeshRenderer가 하나 이상 필요합니다.");
            }
        }

        private static void ValidateTransform(Transform node, string slotName)
        {
            Vector3 scale = node.localScale;
            Quaternion rotation = node.localRotation;
            if (!ViewPrefabRules.IsFinite(node.localPosition) || !ViewPrefabRules.IsFinite(scale) ||
                !ViewPrefabRules.IsFinite(rotation))
            {
                throw Invalid(slotName, $"'{node.name}'의 Transform 값은 유한해야 합니다.");
            }

            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            {
                throw Invalid(slotName, $"'{node.name}'의 모든 축척 축은 0보다 커야 합니다.");
            }
        }

        private static void ValidateComponentType(Component component, string nodeName, string slotName,
            Func<Component, bool> allowAdditional)
        {
            if (component == null)
            {
                throw Invalid(slotName, $"'{nodeName}'에 누락된 스크립트 또는 컴포넌트가 있습니다.");
            }

            Type type = component.GetType();
            if (type != typeof(Transform) && type != typeof(MeshFilter) &&
                type != typeof(MeshRenderer) && type != typeof(LODGroup))
            {
                if (allowAdditional == null || !allowAdditional(component))
                    throw Invalid(slotName, $"'{nodeName}'의 컴포넌트 {type.Name}은 표시 구성에서 허용하지 않습니다.");
            }
        }

        private static void ValidateLods(LODGroup group, Transform root, string slotName)
        {
            foreach (LOD lod in group.GetLODs())
            {
                foreach (Renderer renderer in lod.renderers)
                {
                    if (renderer == null || !renderer.transform.IsChildOf(root))
                    {
                        throw Invalid(slotName, $"'{group.name}'의 LOD 참조는 같은 프리팹 안의 Renderer여야 합니다.");
                    }
                }
            }
        }

        private static string Owner(string slotName) => $"[LobbyModelData] {slotName}";

        private static InvalidOperationException Invalid(string slotName, string reason) =>
            new($"{Owner(slotName)}: {reason}");
    }
}
