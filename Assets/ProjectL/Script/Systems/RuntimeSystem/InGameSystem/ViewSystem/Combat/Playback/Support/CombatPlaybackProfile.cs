#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using Unity.Profiling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>실행 수명은 원래 재생기가 소유하고, 이 객체는 고정된 계측 이름과 최초 사용한 마커만 캐시합니다.</summary>
    internal sealed class CombatPlaybackProfile
    {
        private readonly string _prefix;
        private ProfilerMarker? _dispatch;
        private ProfilerMarker? _handle;
        private ProfilerMarker? _begin;
        private ProfilerMarker? _tick;
        private ProfilerMarker? _hit;
        private ProfilerMarker? _end;
        private ProfilerMarker? _cancel;
        private ProfilerMarker? _prepare;
        private ProfilerMarker? _interpolate;
        private ProfilerMarker? _motionTick;
        private ProfilerMarker? _projectileBegin;
        private ProfilerMarker? _projectileTick;
        private ProfilerMarker? _projectileEnd;
        private ProfilerMarker? _projectileCancel;

        internal CombatPlaybackProfile(string name)
        {
            _prefix = "Combat.Action.Playback." + name + ".";
        }

        // 처음 사용하는 단계만 생성합니다. Nullable 구조체 필드에 보관하므로 반복 접근에는 박싱·람다·문자열 생성이 없습니다.
        internal ProfilerMarker Dispatch => _dispatch ??= new ProfilerMarker(_prefix + "Dispatch");
        internal ProfilerMarker Handle => _handle ??= new ProfilerMarker(_prefix + "Handle");
        internal ProfilerMarker Begin => _begin ??= new ProfilerMarker(_prefix + "Begin");
        internal ProfilerMarker Tick => _tick ??= new ProfilerMarker(_prefix + "Tick");
        internal ProfilerMarker Hit => _hit ??= new ProfilerMarker(_prefix + "Hit");
        internal ProfilerMarker End => _end ??= new ProfilerMarker(_prefix + "End");
        internal ProfilerMarker Cancel => _cancel ??= new ProfilerMarker(_prefix + "Cancel");
        internal ProfilerMarker Prepare => _prepare ??= new ProfilerMarker(_prefix + "Prepare");
        internal ProfilerMarker Interpolate => _interpolate ??= new ProfilerMarker(_prefix + "Interpolate");
        internal ProfilerMarker MotionTick => _motionTick ??= new ProfilerMarker(_prefix + "MotionTick");
        internal ProfilerMarker ProjectileBegin => _projectileBegin ??= new ProfilerMarker(_prefix + "ProjectileBegin");
        internal ProfilerMarker ProjectileTick => _projectileTick ??= new ProfilerMarker(_prefix + "ProjectileTick");
        internal ProfilerMarker ProjectileEnd => _projectileEnd ??= new ProfilerMarker(_prefix + "ProjectileEnd");
        internal ProfilerMarker ProjectileCancel => _projectileCancel ??= new ProfilerMarker(_prefix + "ProjectileCancel");

        /// <summary>사용한 Owner의 프로필만 한 번 만들고 재사용합니다.</summary>
        internal sealed class ByOwner
        {
            private readonly string _action;
            private CombatPlaybackProfile _player;
            private CombatPlaybackProfile _enemy;
            private CombatPlaybackProfile _unknown;
            private CombatPlaybackProfile _system;
            internal CombatPlaybackProfile Unknown => _unknown ??= new CombatPlaybackProfile("Unknown." + _action);
            internal CombatPlaybackProfile System => _system ??= new CombatPlaybackProfile("System." + _action);

            internal ByOwner(string action)
            {
                _action = action;
            }

            internal CombatPlaybackProfile Of(Owner? owner) => owner switch
            {
                Owner.Player => _player ??= new CombatPlaybackProfile("Player." + _action),
                Owner.Enemy => _enemy ??= new CombatPlaybackProfile("Enemy." + _action),
                _ => Unknown
            };
        }
    }
}
#endif
