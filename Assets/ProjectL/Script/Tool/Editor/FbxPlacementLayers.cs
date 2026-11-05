using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>배치 레이어를 맵 루트의 그룹으로 보관해 프리팹 저장과 재열기를 지원합니다.</summary>
    public static class FbxPlacementLayers
    {
        private const string Prefix = "[Layer] ";

        public static bool IsLayer(GameObject root, Transform target) => root != null && target != null &&
            target.parent == root.transform && target.name.StartsWith(Prefix, StringComparison.Ordinal);

        public static string GetName(Transform layer) => layer.name.Substring(Prefix.Length);

        public static List<Transform> GetLayers(GameObject root)
        {
            ValidateRoot(root);
            List<Transform> layers = new List<Transform>();
            foreach (Transform child in root.transform)
                if (IsLayer(root, child)) layers.Add(child);
            return layers;
        }

        public static Transform Resolve(GameObject root, Transform layer)
        {
            ValidateRoot(root);
            if (layer == null || layer == root.transform) return root.transform;
            if (!IsLayer(root, layer)) throw new ArgumentException("이 맵에 속한 배치 레이어를 지정하세요.", nameof(layer));
            return layer;
        }

        public static Transform Create(GameObject root, string name)
        {
            ValidateRoot(root);
            string validName = ValidateName(root, name, null);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("배치 레이어 만들기");
            GameObject layer = new GameObject(Prefix + validName);
            SceneManager.MoveGameObjectToScene(layer, root.scene);
            layer.transform.SetParent(root.transform, false);
            Undo.RegisterCreatedObjectUndo(layer, "배치 레이어 만들기");
            Undo.CollapseUndoOperations(group);
            Undo.IncrementCurrentGroup();
            return layer.transform;
        }

        public static void Rename(GameObject root, Transform layer, string name)
        {
            if (!IsLayer(root, layer)) throw new ArgumentException("이 맵의 배치 레이어를 선택하세요.", nameof(layer));
            string validName = ValidateName(root, name, layer);
            Undo.RecordObject(layer.gameObject, "배치 레이어 이름 변경");
            layer.name = Prefix + validName;
        }

        public static Transform GetContainingLayer(GameObject root, Transform target)
        {
            ValidateRoot(root);
            if (target == null || (target != root.transform && !target.IsChildOf(root.transform)))
                throw new ArgumentException("이 맵의 오브젝트를 지정하세요.", nameof(target));
            Transform top = target;
            while (top != root.transform && top.parent != root.transform) top = top.parent;
            return IsLayer(root, top) ? top : root.transform;
        }

        public static Transform MoveObject(GameObject root, Transform target, Transform destination)
        {
            destination = Resolve(root, destination);
            GetContainingLayer(root, target);
            if (target == root.transform || IsLayer(root, target))
                throw new ArgumentException("레이어로 옮길 배치 오브젝트를 선택하세요.", nameof(target));
            // 메시 자식을 선택해도 FBX 루트 전체를 옮겨 모델 계층을 유지합니다.
            Transform placed = target;
            while (placed.parent != root.transform && !IsLayer(root, placed.parent)) placed = placed.parent;
            if (placed.parent != destination)
                Undo.SetTransformParent(placed, destination, "배치 레이어 변경");
            return placed;
        }

        private static string ValidateName(GameObject root, string name, Transform except)
        {
            string trimmed = (name ?? "").Trim();
            if (trimmed.Length == 0) throw new ArgumentException("레이어 이름을 입력하세요.", nameof(name));
            foreach (Transform child in root.transform)
                if (child != except && child.name == Prefix + trimmed)
                    throw new ArgumentException("같은 이름의 배치 레이어가 있습니다: " + trimmed, nameof(name));
            return trimmed;
        }

        private static void ValidateRoot(GameObject root)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root))
                throw new ArgumentException("가상 씬의 맵 루트가 필요합니다.", nameof(root));
        }
    }
}
