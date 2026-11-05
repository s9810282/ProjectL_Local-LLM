using Unity.Profiling;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// Core 전투 처리의 Unity Profiler 마커입니다. 이름은 모두 이 한 곳에 둡니다.
    /// 서비스 마커는 Systems 쪽 서비스가 직접 쓰고, 계산기 구간(<see cref="CoreProfileSection"/>)은 재생 시작 때 측정기를 설치해 잇습니다.
    /// 계산기 어셈블리는 Unity를 참조하지 않으므로(noEngineReferences) 이 연결이 Systems에 있습니다.
    /// </summary>
    internal static class CoreProfilerMarkers
    {
        internal static readonly ProfilerMarker Command = new("Core.Combat.Command");
        internal static readonly ProfilerMarker Advance = new("Core.Combat.Advance");
        internal static readonly ProfilerMarker Preview = new("Core.Combat.Preview");
        internal static readonly ProfilerMarker Assemble = new("Core.Combat.AssembleInput");
        internal static readonly ProfilerMarker Calculate = new("Core.Combat.Calculate");
        internal static readonly ProfilerMarker Publish = new("Core.Combat.Publish");
        internal static readonly ProfilerMarker PublishPreview = new("Core.Combat.PublishPreview");

        // 미리보기는 종류마다 마커를 둬서 어느 요청이 무거운지 Profiler에서 바로 나뉘게 한다.
        private static readonly ProfilerMarker[] PreviewKinds = CreatePreviewKinds();

        /// <summary>그 종류의 미리보기 계산 마커입니다. 표에 없는 값이면 공통 미리보기 마커입니다.</summary>
        internal static ProfilerMarker PreviewOf(CombatViewPreviewKind kind)
        {
            int index = (int)kind;
            return index >= 0 && index < PreviewKinds.Length ? PreviewKinds[index] : Preview;
        }

        private static ProfilerMarker[] CreatePreviewKinds()
        {
            var kinds = (CombatViewPreviewKind[])System.Enum.GetValues(typeof(CombatViewPreviewKind));
            int length = 0;
            foreach (CombatViewPreviewKind kind in kinds)
                length = Mathf.Max(length, (int)kind + 1);
            var markers = new ProfilerMarker[length];
            for (int i = 0; i < length; i++)
                markers[i] = Preview;
            foreach (CombatViewPreviewKind kind in kinds)
                if ((int)kind >= 0)
                    markers[(int)kind] = new ProfilerMarker("Core.Combat.Preview." + kind);
            return markers;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallCalculatorProfiler() => CoreProfiling.Install(new UnityCoreProfiler());

        /// <summary>계산기 구간을 구간마다 마커 하나로 잇습니다. Profiler Hierarchy의 Calls 열이 호출 수입니다.</summary>
        private sealed class UnityCoreProfiler : ICoreProfiler
        {
            private readonly ProfilerMarker[] _markers;

            internal UnityCoreProfiler()
            {
                var sections = (CoreProfileSection[])System.Enum.GetValues(typeof(CoreProfileSection));
                _markers = new ProfilerMarker[sections.Length];
                foreach (CoreProfileSection section in sections)
                    _markers[(int)section] = new ProfilerMarker("Core.Calc." + section);
            }

            public void Begin(CoreProfileSection section) => _markers[(int)section].Begin();

            public void End(CoreProfileSection section) => _markers[(int)section].End();
        }
    }
}
