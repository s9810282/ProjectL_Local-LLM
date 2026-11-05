using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>토네이도 외형·유닛 연출이 공유하는 불변 설정입니다. 규칙·논리 위치는 포함하지 않습니다.</summary>
    internal sealed class TornadoPresentationSettingsData
    {
        private readonly AnimationCurve _approach;
        private readonly AnimationCurve _rise;
        private readonly AnimationCurve _descend;
        private readonly AnimationCurve _appearance;
        internal float ApproachSeconds { get; }
        internal float RiseSeconds { get; }
        internal float RiseHeight { get; }
        // 목적지 위 RiseHeight에서 목적지까지 수직으로 떨어지는 시간입니다.
        internal float DescendSeconds { get; }
        internal Vector2 SpinTurnsPerSecondRange { get; }
        internal float AppearanceSeconds { get; }
        /// <summary>유닛이 휩쓸리는 순간(토네이도 중심으로 다가가기 시작)의 효과음 프리셋입니다(소리 창구 2차 설계 Q-ES6′). -1이면 소리 없음입니다.</summary>
        internal int LiftSoundPreset { get; }
        /// <summary>유닛이 목적지에 내려앉을 때의 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        internal int LandSoundPreset { get; }
        /// <summary>휩쓸리는 순간(Start)·내려앉는 순간(End)의 이펙트·흔들림 큐입니다(이펙트 창구 확장 설계 EC5·EC8). 비어 있으면 빈 묶음입니다.</summary>
        internal CombatEventCues Cues { get; }
        /// <summary>폭풍 중심을 한 칸 옮기는 시간입니다(토네이도 View 설계 TN6). 곡선은 <see cref="Approach"/>를 함께 씁니다(계획 TP-1).</summary>
        internal float MoveStepSeconds { get; }
        /// <summary>이동 전 경고 때 켜는 공용 삼각형 화살촉의 재질입니다.</summary>
        internal Material DirectionArrowMaterial { get; }
        internal float DirectionArrowLength { get; }
        internal float DirectionArrowWidth { get; }
        /// <summary>방향 화살촉을 칸 표면 위로 띄우는 월드 단위 높이입니다.</summary>
        internal float DirectionArrowLift { get; }
        /// <summary>방향 화살촉 점등(켜짐 → 앞으로 밀림 → 꺼짐 반복)입니다.</summary>
        internal TornadoArrowPulse DirectionArrowPulse { get; }
        /// <summary>몸 프리팹이 덮도록 만들어진 한 변의 칸 수입니다. 표시 배율은 판정 칸 수 ÷ 이 값입니다.</summary>
        internal int BodyReferenceCells { get; }
        /// <summary>이동 방향을 알리는 경고 순간의 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        internal int WarningSoundPreset { get; }

        /// <exception cref="ArgumentException">큐가 칸 규칙(<see cref="CombatCueEvent.Tornado"/>)에 맞지 않을 때입니다.</exception>
        internal TornadoPresentationSettingsData(float approachSeconds, float riseSeconds, float riseHeight,
            Vector2 spinTurnsPerSecondRange, float appearanceSeconds, AnimationCurve approachCurve,
            AnimationCurve riseCurve, AnimationCurve appearanceCurve, float descendSeconds, AnimationCurve descendCurve,
            int liftSoundPreset, int landSoundPreset, CombatEventCues cues,
            float moveStepSeconds, Material directionArrowMaterial, float directionArrowLength, float directionArrowWidth,
            float directionArrowLift, TornadoArrowPulse directionArrowPulse, int bodyReferenceCells, int warningSoundPreset)
        {
            RequirePositive(approachSeconds, nameof(approachSeconds));
            RequirePositive(riseSeconds, nameof(riseSeconds));
            RequirePositive(riseHeight, nameof(riseHeight));
            RequirePositive(descendSeconds, nameof(descendSeconds));
            RequirePositive(appearanceSeconds, nameof(appearanceSeconds));
            RequirePositive(spinTurnsPerSecondRange.x, nameof(spinTurnsPerSecondRange));
            RequirePositive(spinTurnsPerSecondRange.y, nameof(spinTurnsPerSecondRange));
            if (spinTurnsPerSecondRange.x > spinTurnsPerSecondRange.y)
                throw new InvalidOperationException("토네이도 회전 속도 최소값은 최대값보다 클 수 없습니다.");
            _approach = CopyCurve(approachCurve, nameof(approachCurve));
            _rise = CopyCurve(riseCurve, nameof(riseCurve));
            _descend = CopyCurve(descendCurve, nameof(descendCurve));
            _appearance = CopyCurve(appearanceCurve, nameof(appearanceCurve));
            CombatPresentationSettingsData.ValidateSoundPreset(liftSoundPreset, nameof(liftSoundPreset));
            CombatPresentationSettingsData.ValidateSoundPreset(landSoundPreset, nameof(landSoundPreset));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            CombatPresentationCueRules.Require(cues, CombatCueEvent.Tornado, "[TornadoPresentationSettingsData]");
            RequirePositive(moveStepSeconds, nameof(moveStepSeconds));
            if (directionArrowMaterial == null)
                throw new InvalidOperationException("토네이도 연출에 방향 화살촉 재질이 필요합니다.");
            RequirePositive(directionArrowLength, nameof(directionArrowLength));
            RequirePositive(directionArrowWidth, nameof(directionArrowWidth));
            if (!float.IsFinite(directionArrowLift) || directionArrowLift < 0f)
                throw new InvalidOperationException("토네이도 방향 화살촉 높이는 유한한 0 이상 값이어야 합니다.");
            if (bodyReferenceCells < 1)
                throw new InvalidOperationException($"토네이도 몸 기준 칸 수는 1 이상이어야 합니다: {bodyReferenceCells}.");
            CombatPresentationSettingsData.ValidateSoundPreset(warningSoundPreset, nameof(warningSoundPreset));
            MoveStepSeconds = moveStepSeconds;
            DirectionArrowMaterial = directionArrowMaterial;
            DirectionArrowLength = directionArrowLength;
            DirectionArrowWidth = directionArrowWidth;
            DirectionArrowLift = directionArrowLift;
            DirectionArrowPulse = directionArrowPulse ?? throw new ArgumentNullException(nameof(directionArrowPulse));
            BodyReferenceCells = bodyReferenceCells;
            WarningSoundPreset = warningSoundPreset;
            ApproachSeconds = approachSeconds;
            RiseSeconds = riseSeconds;
            RiseHeight = riseHeight;
            DescendSeconds = descendSeconds;
            SpinTurnsPerSecondRange = spinTurnsPerSecondRange;
            AppearanceSeconds = appearanceSeconds;
            LiftSoundPreset = liftSoundPreset;
            LandSoundPreset = landSoundPreset;
            Cues = cues;
        }

        internal float Approach(float progress) => _approach.Evaluate(Mathf.Clamp01(progress));
        internal float Rise(float progress) => _rise.Evaluate(Mathf.Clamp01(progress));
        internal float Descend(float progress) => _descend.Evaluate(Mathf.Clamp01(progress));
        internal float Appearance(float progress) => _appearance.Evaluate(Mathf.Clamp01(progress));

        private static void RequirePositive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"토네이도 연출 값은 유한한 양수여야 합니다: {name}.");
        }

        private static AnimationCurve CopyCurve(AnimationCurve curve, string name)
        {
            if (curve == null || curve.length < 2)
                throw new InvalidOperationException($"토네이도 연출 곡선에 두 개 이상의 키가 필요합니다: {name}.");
            Keyframe[] keys = curve.keys;
            if (keys[0].time != 0f || keys[0].value != 0f || keys[^1].time != 1f || keys[^1].value != 1f)
                throw new InvalidOperationException($"토네이도 연출 곡선은 (0,0)에서 (1,1)로 이어져야 합니다: {name}.");
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!float.IsFinite(key.time) || !float.IsFinite(key.value) ||
                    !float.IsFinite(key.inTangent) || !float.IsFinite(key.outTangent) || key.value < 0f || key.value > 1f)
                    throw new InvalidOperationException($"토네이도 곡선은 유한하고 연속이며 0~1 범위여야 합니다: {name}.");
                if (i == 0) continue;
                Keyframe before = keys[i - 1];
                double duration = key.time - before.time;
                double outgoing = (before.weightedMode & WeightedMode.Out) != 0 ? before.outWeight : 1d / 3d;
                double incoming = (key.weightedMode & WeightedMode.In) != 0 ? key.inWeight : 1d / 3d;
                if (duration <= 0d || !double.IsFinite(outgoing) || !double.IsFinite(incoming) ||
                    outgoing < 0d || outgoing > 1d || incoming < 0d || incoming > 1d ||
                    !Monotone(0d, outgoing, 1d - incoming, 1d) ||
                    !Monotone(before.value, before.value + before.outTangent * duration * outgoing,
                        key.value - key.inTangent * duration * incoming, key.value))
                    throw new InvalidOperationException($"토네이도 곡선은 시간과 값이 역행하지 않아야 합니다: {name}.");
            }
            return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        // 가중 키도 베지어 도함수의 최솟값으로 검사합니다. 유한 표본 사이의 역행을 놓치지 않습니다.
        private static bool Monotone(double p0, double p1, double p2, double p3)
        {
            double d0 = p1 - p0, d1 = p2 - p1, d2 = p3 - p2;
            double a = d0 - 2d * d1 + d2, b = 2d * (d1 - d0);
            double minimum = Math.Min(d0, d2);
            if (a > 0d)
            {
                double t = -b / (2d * a);
                if (t > 0d && t < 1d) minimum = Math.Min(minimum, (a * t + b) * t + d0);
            }
            return minimum >= -1e-7d;
        }
    }
}
