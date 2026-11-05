using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>토네이도 외형 전환과 유닛 연출의 조절값입니다. 다음 전투 조립부터 적용됩니다.</summary>
    [CreateAssetMenu(fileName = "TornadoPresentation", menuName = "ProjectL/Database/View/Tornado Presentation")]
    internal sealed class TornadoPresentationSettingsSO : ScriptableObject
    {
        [SerializeField, Min(.001f)] private float approachSeconds = .25f;
        [SerializeField, Min(.001f)] private float riseSeconds = .8f;
        [Tooltip("중심 접근 완료 위치에서 필드 위쪽으로 더할 월드 거리입니다. 떨어지기도 목적지 위 같은 높이에서 시작합니다. 실제 게임 화면에서 조절합니다.")]
        [SerializeField, Min(.001f)] private float riseHeight = 10f;
        [Tooltip("꼭대기에서 목적지 바로 위로 옮긴 뒤 목적지까지 수직으로 떨어지는 시간입니다.")]
        [SerializeField, Min(.001f)] private float descendSeconds = .6f;
        [Tooltip("초당 회전 수 최소/최대입니다. 같은 값이면 속도만 고정됩니다.")]
        [SerializeField] private Vector2 spinTurnsPerSecondRange = new(2f, 2f);
        [SerializeField, Min(.001f)] private float appearanceSeconds = .35f;
        [SerializeField] private AnimationCurve approachCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("떨어진 정도입니다. 0은 목적지 위 RiseHeight, 1은 착지입니다. 기본은 중력처럼 점점 빨라지는 t²입니다.")]
        [SerializeField] private AnimationCurve descendCurve = new(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));
        [SerializeField] private AnimationCurve appearanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Header("폭풍 이동·방향 (토네이도 View 설계 TN6)")]
        [Tooltip("중심을 한 칸 옮기는 시간입니다. 곡선은 Approach Curve를 함께 씁니다.")]
        [SerializeField, Min(.001f)] private float moveStepSeconds = .25f;
        [Tooltip("이동 전 경고 때 폭풍 옆에 켜는 공용 삼각형 화살촉의 재질입니다. _BaseColor가 있어야 하며 재질 색을 그대로 씁니다.")]
        [SerializeField] private Material directionArrowMaterial;
        [Tooltip("방향 화살촉의 진행 방향 길이(필드 로컬 단위)입니다. 이동 경로와 같은 삼각형 모양입니다.")]
        [SerializeField, Min(0f)] private float directionArrowLength = .5f;
        [Tooltip("방향 화살촉 밑변의 너비(필드 로컬 단위)입니다.")]
        [SerializeField, Min(0f)] private float directionArrowWidth = .8f;
        [Tooltip("방향 화살촉을 해당 칸 표면 위로 띄우는 높이(월드 단위)입니다. 0이면 표면에 놓습니다.")]
        [SerializeField, Min(0f)] private float directionArrowLift = .05f;
        [Header("방향 화살촉 점등")]
        [Tooltip("점등 한 주기의 초입니다. 한 주기 = 원위치에서 켜짐 → 앞으로 밀림 → 꺼짐이고 반복합니다. 화살촉이 켜지는 순간 주기 처음부터 시작합니다. 0이면 점등하지 않고 원위치에 계속 켜 둡니다.")]
        [SerializeField, Min(0f)] private float directionArrowPulseSeconds = .9f;
        [Tooltip("한 주기 중 켜져 있는 비율입니다. 나머지는 꺼져 있습니다. 1이면 꺼지지 않고 밀림만 반복합니다.")]
        [SerializeField, Range(.05f, 1f)] private float directionArrowPulseLitRatio = .7f;
        [Tooltip("켜져 있는 동안 진행 방향으로 밀려나는 거리(필드 로컬 단위)입니다. 처음엔 빠르고 끝으로 갈수록 느려집니다. 0이면 움직이지 않습니다.")]
        [SerializeField, Min(0f)] private float directionArrowPulseDistance = .4f;
        [Tooltip("밀림이 끝날 때(꺼지기 직전)의 밝기입니다. 켜지는 순간은 1이고 이 값까지 어두워지며, 재질 색(RGB)에 곱합니다. 1이면 밝기가 바뀌지 않습니다.")]
        [SerializeField, Range(0f, 1f)] private float directionArrowPulseEndBrightness = .4f;
        [Tooltip("몸 프리팹이 덮도록 만들어진 한 변의 칸 수입니다. 배율은 판정 칸 수 ÷ 이 값입니다. 프리팹을 바꾸면 함께 맞춥니다.")]
        [SerializeField, Min(1)] private int bodyReferenceCells = 3;
        [Header("소리 (소리 창구 2차 설계 Q-ES6′)")]
        [Tooltip("유닛이 휩쓸리는 순간의 효과음 프리셋 번호입니다. 토네이도 중심으로 다가가기(approachSeconds)가 시작할 때 나고, 들어 올리기는 그 뒤입니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int liftSoundPreset = -1;
        [Tooltip("유닛이 목적지에 내려앉을 때의 효과음 프리셋 번호입니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int landSoundPreset = -1;
        [Tooltip("이동 방향을 알리는 경고 순간의 효과음 프리셋 번호입니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int warningSoundPreset = -1;
        [Header("연출 큐 (이펙트 창구 확장 설계 EC5)")]
        [Tooltip("휩쓸린 유닛의 이펙트·흔들림입니다. 시점 Start = 휩쓸리는 순간, End = 내려앉는 순간이고, 이펙트 자리는 그 시점의 유닛 몸(SubjectBody)만 됩니다. "
            + "지연은 0만 됩니다(착지 칸을 들어 올림이 끝나는 순간 그리므로, 늦게 끝나는 큐가 있으면 유닛이 휩쓸리기 전 칸으로 되돌아가 보임).")]
        [SerializeField] private CombatVisualCueInput cues = new();

        internal TornadoPresentationSettingsData GetData() => new(approachSeconds, riseSeconds, riseHeight,
            spinTurnsPerSecondRange, appearanceSeconds, approachCurve, riseCurve, appearanceCurve,
            descendSeconds, descendCurve, liftSoundPreset, landSoundPreset,
            (cues ?? throw new InvalidOperationException("[TornadoPresentationSettingsSO] 연출 큐 칸이 없습니다.")).ToCues(),
            moveStepSeconds, directionArrowMaterial, directionArrowLength, directionArrowWidth, directionArrowLift,
            new TornadoArrowPulse(directionArrowPulseSeconds, directionArrowPulseLitRatio, directionArrowPulseDistance,
                directionArrowPulseEndBrightness),
            bodyReferenceCells, warningSoundPreset);
    }
}
