#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Unity.Profiling;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>입력·명령·미리보기의 기존 종류로 만든 고정 마커 표입니다. 입력 정책과 상태는 소유하지 않습니다.</summary>
    internal static class CombatInputProfiler
    {
        private static readonly ProfilerMarker _unknown = new("Combat.Action.Input.Unknown.Handle");
        private static readonly ProfilerMarker[] _inputs = Create<CombatInputRouter.Request>("Input", "Handle");
        private static readonly ProfilerMarker[] _blockedInputs = Create<CombatInputRouter.Request>("Input", "Blocked");
        private static readonly ProfilerMarker[] _commands = Create<CombatViewCommandKind>("Command", "Submit");
        private static readonly ProfilerMarker[] _queued = Create<CombatViewCommandKind>("Command", "Queued");
        private static readonly ProfilerMarker[] _blockedCommands = Create<CombatViewCommandKind>("Command", "Blocked");
        private static readonly ProfilerMarker[] _requests = Create<CombatViewPreviewKind>("Preview", "Request");
        private static readonly ProfilerMarker[] _answers = Create<CombatViewPreviewKind>("Preview", "Apply");

        internal static ProfilerMarker Input(CombatInputRouter.Request request) => Get(_inputs, (int)request);
        internal static ProfilerMarker Apply(CombatViewPreviewKind kind) => Get(_answers, (int)kind);

        internal static void InputBlocked(CombatInputRouter.Request request)
        {
            using var sample = Get(_blockedInputs, (int)request).Auto();
        }

        internal static void CommandBlocked(CombatViewCommandKind kind)
        {
            using var sample = Get(_blockedCommands, (int)kind).Auto();
        }

        internal static int Submit(ICombatViewBufferWriteSink input, CombatViewCommandData command)
        {
            using var sample = Get(_commands, (int)command.Kind).Auto();
            int number = input.Submit(command);
            using var outcome = Get(number > 0 ? _queued : _blockedCommands, (int)command.Kind).Auto();
            return number;
        }

        internal static int SetPreview(ICombatViewBufferWriteSink input, CombatViewPreviewData preview)
        {
            using var sample = Get(_requests, (int)preview.Kind).Auto();
            return input.SetPreview(preview);
        }

        private static ProfilerMarker Get(ProfilerMarker[] markers, int index) =>
            index >= 0 && index < markers.Length ? markers[index] : _unknown;

        private static ProfilerMarker[] Create<T>(string category, string phase) where T : struct, Enum
        {
            Array values = Enum.GetValues(typeof(T));
            int length = 0;
            foreach (T value in values)
                length = Math.Max(length, Convert.ToInt32(value) + 1);
            var markers = new ProfilerMarker[length];
            for (int i = 0; i < length; i++)
                markers[i] = _unknown;
            foreach (T value in values)
                markers[Convert.ToInt32(value)] = new ProfilerMarker("Combat.Action." + category + "." + value + "." + phase);
            return markers;
        }
    }
}
#endif
