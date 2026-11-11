using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>공격 클립 대여 중에만 프리팹의 상하 조준을 적용하는 선택 기능입니다.</summary>
    [DisallowMultipleComponent]
    internal sealed class CombatWeaponPitchRig : MonoBehaviour
    {
        [SerializeField] private RigBuilder rigBuilder;
        [SerializeField] private MultiAimConstraint pitch;
        private bool _ownsRig;
        private CombatWeaponAim? _aim;
        private Vector3 _axis;
        private Vector3 _restDirection;
        private float _raiseAngle;

        internal Playable Bind(PlayableGraph graph, Playable clipMixer)
        {
            if (rigBuilder == null || rigBuilder.gameObject != gameObject || rigBuilder.enabled ||
                rigBuilder.layers.Count != 1 || pitch == null || !pitch.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[CombatWeaponPitchRig] 비활성 RigBuilder와 조준 Constraint가 필요합니다: {name}");

            MultiAimConstraintData data = pitch.data;
            if (data.constrainedObject == null || !data.constrainedObject.IsChildOf(transform) ||
                data.sourceObjects.Count != 1 || data.sourceObjects[0].transform == null ||
                !data.constrainedXAxis || data.constrainedYAxis || data.constrainedZAxis ||
                data.maintainOffset || data.offset.y != 0f || data.offset.z != 0f ||
                data.worldUpType != MultiAimConstraintData.WorldUpType.None)
                throw new InvalidOperationException($"[CombatWeaponPitchRig] X축만 보정하는 단일 타깃 조준이 필요합니다: {name}");

            rigBuilder.layers[0].active = false;
            _ownsRig = true;
            if (!rigBuilder.Build(graph) || graph.GetOutputCount() != 2)
                throw new InvalidOperationException($"[CombatWeaponPitchRig] 전투 클립 그래프에 조준 리그를 연결하지 못했습니다: {name}");

            // RigBuilder 1.4의 순서는 Scene Sync → 단일 Rig입니다.
            // Animator 출력 하나에 연결해 클립에서 평가한 자세에 조준을 적용합니다.
            PlayableOutput syncOutput = graph.GetOutput(0);
            PlayableOutput rigOutput = graph.GetOutput(1);
            Playable sync = syncOutput.GetSourcePlayable();
            Playable aim = rigOutput.GetSourcePlayable();
            graph.DestroyOutput(rigOutput);
            graph.DestroyOutput(syncOutput);
            sync.AddInput(clipMixer, 0, 1f);
            aim.AddInput(sync, 0, 1f);
            rigBuilder.SyncLayers();
            return aim;
        }

        internal void Aim(CombatWeaponAim aim, Transform muzzle)
        {
            MultiAimConstraintData data = pitch.data;
            Transform pivot = data.constrainedObject;
            if (!muzzle.IsChildOf(pivot))
                throw new InvalidOperationException($"[CombatWeaponPitchRig] 포구가 조준 본 아래에 있어야 합니다: {name}");

            Vector3 axis = pivot.parent.right;
            Vector3 toTarget = Vector3.ProjectOnPlane(aim.Point - pivot.position, axis);
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
                return;

            Vector3 bore = Vector3.ProjectOnPlane(muzzle.forward, axis).normalized;
            Vector3 aimAxis = ((IMultiAimConstraintData)data).aimAxis;
            Vector3 reference = pivot.TransformDirection(Quaternion.Euler(-data.offset) * aimAxis);
            float boreAngle = Vector3.SignedAngle(reference, bore, axis);
            // 발사축이 피벗의 중심을 지나지 않아 생기는 각도 차이를 보정합니다.
            float boreOffset = Vector3.Dot(muzzle.position - pivot.position, Vector3.Cross(axis, bore));
            float parallax = Mathf.Asin(Mathf.Clamp(boreOffset / distance, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 fullAim = Quaternion.AngleAxis(-boreAngle - parallax, axis) * toTarget;
            _axis = axis;
            _restDirection = Vector3.ProjectOnPlane(reference, axis).normalized * distance;
            _raiseAngle = Mathf.Min(0f, Vector3.SignedAngle(_restDirection, fullAim, axis));
            _aim = aim;
            SetTime(0f);
            rigBuilder.layers[0].active = true;
            rigBuilder.SyncLayers();
        }

        internal void SetTime(float time)
        {
            if (_aim is not CombatWeaponAim aim)
                return;

            float progress;
            if (time < aim.FirstShot)
                progress = Mathf.InverseLerp(0f, aim.FirstShot, time);
            else if (time <= aim.LastShot)
                progress = 1f;
            else
                progress = 1f - Mathf.InverseLerp(aim.LastShot, aim.EndTime, time);

            MultiAimConstraintData data = pitch.data;
            float angle = _raiseAngle * Mathf.SmoothStep(0f, 1f, progress);
            data.sourceObjects[0].transform.position =
                data.constrainedObject.position + Quaternion.AngleAxis(angle, _axis) * _restDirection;
        }

        internal void ResetAim()
        {
            _aim = null;
            if (!_ownsRig || rigBuilder == null)
                return;
            rigBuilder.layers[0].active = false;
            rigBuilder.SyncLayers();
        }

        // 외부 그래프의 출력이 사라진 뒤 리그 Job의 자원을 해제합니다.
        internal void Clear()
        {
            if (!_ownsRig)
                return;
            _ownsRig = false;
            if (rigBuilder != null)
                rigBuilder.Clear();
        }
    }

    /// <summary>현재 클립 대여에 곡사의 조준점과 발사 시각을 전달합니다.</summary>
    internal interface ICombatWeaponPitchClip
    {
        void AimWeapon(CombatWeaponAim aim);
    }

    /// <summary>조준점과 클립의 첫 발사·마지막 발사·끝 시각입니다.</summary>
    internal readonly struct CombatWeaponAim
    {
        internal CombatWeaponAim(Vector3 point, float firstShot, float lastShot, float endTime)
        {
            Point = point;
            FirstShot = firstShot;
            LastShot = lastShot;
            EndTime = endTime;
        }

        internal Vector3 Point { get; }
        internal float FirstShot { get; }
        internal float LastShot { get; }
        internal float EndTime { get; }
    }
}
