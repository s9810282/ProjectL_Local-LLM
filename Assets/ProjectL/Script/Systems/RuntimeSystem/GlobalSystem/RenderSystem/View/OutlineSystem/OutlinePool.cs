using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem
{
    internal class OutlineNode
    {
        public readonly GameObject gameObject;
        public readonly MeshFilter meshFilter;
        public readonly Renderer renderer;
        public readonly IColorHandle colorHandle;

        public OutlineNode(GameObject go, MeshFilter mf, Renderer r, IColorHandle colorHandle)
        {
            this.gameObject = go;
            this.meshFilter = mf;
            this.renderer = r;
            this.colorHandle = colorHandle;
        }
    }

    internal interface IOutlinePoolService<TElement>
    {
        public TElement GetElement();
        public void ReturnElement(TElement element);
    }

    internal class OutlinePool : IOutlinePoolService<OutlineNode>
    {
        private sealed class OutlinePoolSetting : IPoolLifecycle<OutlineNode>
        {
            private readonly Transform root;

            public OutlinePoolSetting(Transform root) => this.root = root;

            void IPoolLifecycle<OutlineNode>.OnGet(OutlineNode item) { }

            void IPoolLifecycle<OutlineNode>.OnBeforeReturn(OutlineNode item)
            {
                item.gameObject.SetActive(false);
                item.gameObject.transform.SetParent(root, false);
            }

            void IPoolLifecycle<OutlineNode>.OnReturn(OutlineNode item) { }
        }

        private CustomPool<OutlineNode> pool;
        private Material OutlineMaterial;
        private IColorSystemService ColorSystemService;
        private readonly Transform root;

        public OutlinePool(
            Material outlineMaterial,
            IColorSystemService colorService,
            Transform root,
            int initCount = 5)
        {
            this.OutlineMaterial = outlineMaterial;
            this.ColorSystemService = colorService;
            this.root = root ?? throw new System.ArgumentNullException(nameof(root));

            this.pool = new CustomPool<OutlineNode>(new PoolContext<OutlineNode>(
                CreateNode,
                initCount,
                null,
                new OutlinePoolSetting(root)
            ));
        }

        public OutlineNode GetElement() => pool.Get();

        public void ReturnElement(OutlineNode element) => pool.Return(element);

        private OutlineNode CreateNode()
        {
            GameObject temp = new GameObject("Outline_Node");
            UnityEngine.Object.DontDestroyOnLoad(temp);
            MeshFilter meshFilter = temp.AddComponent<MeshFilter>();
            Renderer renderer = temp.AddComponent<MeshRenderer>();

            renderer.material = OutlineMaterial;
            temp.SetActive(false);
            temp.transform.SetParent(root, false);

            IColorHandle colorHandle = ColorSystemService.CreateColorHandle(renderer);

            return new OutlineNode(temp, meshFilter, renderer, colorHandle);
        }
    }
}
