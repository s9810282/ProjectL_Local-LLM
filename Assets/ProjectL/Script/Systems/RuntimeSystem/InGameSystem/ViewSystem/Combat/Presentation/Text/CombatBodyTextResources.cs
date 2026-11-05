using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text
{
    /// <summary>연결 하나의 호스트와 풀 자원을 함께 소유합니다.</summary>
    internal sealed class CombatBodyTextResources : IDisposable
    {
        private bool _released;
        internal CombatBodyTextResources(RectTransform root)
        {
            Root = root;
            Pool = new CombatBodyTextPool(root);
        }

        internal RectTransform Root { get; }
        internal CombatBodyTextPool Pool { get; }

        public void Dispose()
        {
            if (_released)
                return;
            _released = true;
            try
            {
                Pool.Dispose();
            }
            finally
            {
                if (Root != null)
                    CombatBodyTextObjects.Destroy(Root.gameObject);
            }
        }
    }
}
