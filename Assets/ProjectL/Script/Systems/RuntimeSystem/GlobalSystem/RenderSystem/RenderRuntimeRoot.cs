using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem
{
    /// <summary>
    /// 씬 전환에도 유지되는 렌더링 런타임 오브젝트의 최상위 부모입니다.
    /// 각 하위 시스템은 BindScene에서 자신의 Root를 이 부모 아래에 만든 뒤,
    /// Initialize에서 그 Root를 풀의 반환 위치로 전달합니다.
    /// </summary>
    internal sealed class RenderRuntimeRoot
    {
        private readonly Dictionary<string, Transform> systemRoots = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private Transform root;

        public void BindScene()
        {
            if (root != null)
                return;

            var gameObject = new GameObject("RenderRoot");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            root = gameObject.transform;
        }

        public Transform GetOrCreateSystemRoot(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A render system root name is required.", nameof(name));

            BindScene();

            if (systemRoots.TryGetValue(name, out Transform systemRoot) && systemRoot != null)
                return systemRoot;

            var gameObject = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.transform.SetParent(root, false);
            systemRoots[name] = gameObject.transform;
            return gameObject.transform;
        }
    }
}
