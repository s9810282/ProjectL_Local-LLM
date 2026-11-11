using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using UnityEngine;

namespace ProjectL.Systems
{
    /// <summary>
    /// 실제 DatabaseCatalogSO와 FrameworkRuntime을 사용해 렌더 기능을 확인하는 씬용 Runner입니다.
    /// </summary>
    public sealed class TestRuner : MonoBehaviour
    {
        [SerializeField] private DatabaseCatalogSO databaseCatalog;
        [SerializeField] private bool areaOutlineTestsEnabled = true;
        [SerializeField] private Vector2Int[] cells = { Vector2Int.zero, Vector2Int.right };
        [SerializeField] private Vector3 gridOrigin;
        [SerializeField] private Vector2 cellSize = Vector2.one;
        [SerializeField] private float yOffset = 0.01f;
        [SerializeField] private float thickness = 0.05f;
        [SerializeField] private Color color = Color.cyan;
        [Header("Area Outline Animation")]
        [SerializeField] private bool isDashed = true;
        [SerializeField, Min(1f)] private float dashCount = 4f;
        [SerializeField, Range(0.05f, 1f)] private float dashFill = 0.55f;
        [SerializeField] private AreaOutlineFlowDirection flowDirection = AreaOutlineFlowDirection.Clockwise;
        [SerializeField, Min(0f)] private float flowSpeed = 1f;
        [SerializeField] private bool updateEveryFrame;

        private FrameworkRuntime runtime;
        private IAreaOutlineSystemService areaOutlineSystem;
        private IAreaOutlineHandle areaOutlineHandle;
        private bool isRunning;

        private void Start()
        {
            StartRunner();
        }

        private void Update()
        {
            if (!isRunning)
                return;

            try
            {
                runtime.Tick(Time.deltaTime);

                if (updateEveryFrame)
                    ApplyOutline();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopRunner();
            }
        }

        private void OnDisable()
        {
            StopRunner();
        }

        [ContextMenu("Runtime/Start Tests")]
        public void StartRunner()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestRuner] Enter Play Mode to run the framework test.", this);
                return;
            }

            StopRunner();

            if (databaseCatalog == null)
            {
                Debug.LogError("[TestRuner] DatabaseCatalogSO is required.", this);
                return;
            }

            try
            {
                IServiceModule databaseContext = DataBaseFactory.Create(databaseCatalog);

                runtime = FrameworkRuntimeFactory.Create();
                runtime.BindScene();
                runtime.InitData(databaseContext);
                runtime.PostBindService();
                runtime.Initialize();

                IGlobalSystemGroupService global = runtime.GetService(SystemGroupsService.Global);
                if (areaOutlineTestsEnabled)
                {
                    areaOutlineSystem = global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices;
                    areaOutlineHandle = areaOutlineSystem.Create();
                }

                isRunning = true;
                ApplyOutline();
                Debug.Log("[TestRuner] Framework tests ready.", this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopRunner();
            }
        }

        [ContextMenu("Recreate Area Outline")]
        public void RecreateOutline()
        {
            if (!isRunning || areaOutlineSystem == null)
                return;

            try
            {
                IAreaOutlineHandle previous = areaOutlineHandle;
                previous?.Dispose();
                areaOutlineHandle = null;
                areaOutlineHandle = areaOutlineSystem.Create();
                ApplyOutline();
                Debug.Log($"[TestRuner] AreaOutline recreated. Pooled handle reused: {ReferenceEquals(previous, areaOutlineHandle)}", this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopRunner();
            }
        }

        [ContextMenu("Update Area Outline")]
        public void ApplyOutline()
        {
            if (!isRunning || areaOutlineHandle == null)
                return;

            areaOutlineHandle.Update(new AreaOutlineData(
                "Runtime Area Outline",
                cells,
                gridOrigin,
                cellSize,
                yOffset,
                thickness,
                color,
                isDashed,
                dashCount,
                dashFill,
                flowDirection,
                flowSpeed));
        }

        [ContextMenu("Runtime/Stop Tests")]
        public void StopRunner()
        {
            isRunning = false;

            areaOutlineHandle?.Dispose();
            areaOutlineHandle = null;
            areaOutlineSystem = null;

            runtime?.Shutdown();
            runtime = null;
        }
    }
}
