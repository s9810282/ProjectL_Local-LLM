#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Editor
{
    /// <summary>Inspector와 입력이 공유하는 에디터 세션 작업값입니다. 확정 원본은 씬 카메라입니다.</summary>
    internal sealed class StageMapCameraFramingSettings
    {
        private readonly StageMapSceneRoot _root;
        private readonly string _key;

        internal StageMapCameraFramingSettings(StageMapSceneRoot root)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            if (root.MapCamera == null || root.MapRoot == null)
                throw new InvalidOperationException("[StageMap] 카메라 구도 조정에는 MapCamera와 MapRoot가 필요합니다.");
            _key = root.CameraFramingKey;
        }

        internal bool IsOpen => SessionState.GetBool(_key + ".Open", false) &&
            SessionState.GetBool(_key + ".FullPose", false);
        internal Vector3 Position => Read("Position");
        internal Vector3 Angles => Read("Rotation");
        internal StageMapCameraFramingPending.Confirmation Confirmed => StageMapCameraFramingPending.Find(_key);
        internal const float MinimumSize = 0.01f;
        internal float Size => SessionState.GetFloat(_key + ".Size", _root.MapCamera.m_Lens.OrthographicSize);
        internal string DollyStatus => SessionState.GetString(_key + ".DollyStatus", "가운데 버튼 입력 대기");

        internal void ReportDolly(string message) => SessionState.SetString(_key + ".DollyStatus",
            $"프레임 {Time.frameCount}: {message}");

        internal void Unlock()
        {
            var confirmed = Confirmed;
            Write("Position", confirmed != null ? confirmed.position : _root.MapCamera.transform.position);
            Write("Rotation", confirmed != null ? confirmed.angles : _root.MapCamera.transform.eulerAngles);
            SessionState.SetFloat(_key + ".Size", confirmed != null ? confirmed.size : _root.MapCamera.m_Lens.OrthographicSize);
            SessionState.SetBool(_key + ".FullPose", true);
            SessionState.SetBool(_key + ".Open", true);
            SessionState.SetString(_key + ".DollyStatus", "가운데 버튼 입력 대기");
        }

        internal void Lock() => SessionState.SetBool(_key + ".Open", false);

        internal void Confirm()
        {
            if (!IsValid(Position, Angles) || !IsValidSize(Size))
                throw new InvalidOperationException("[StageMap] 유효한 카메라 구도만 확정할 수 있습니다.");
            if (EditorApplication.isPlaying)
                StageMapCameraFramingPending.Store(_root, Position, Angles, Size);
            else
            {
                StageMapCameraFramingPending.Apply(_root.MapCamera, Position, Angles, Size);
                StageMapCameraFramingPending.Remove(_key);
            }
            Lock();
        }

        internal static bool IsValidSize(float size) => float.IsFinite(size) && size >= MinimumSize;

        internal bool TrySetSize(float size)
        {
            if (!IsValidSize(size)) return false;
            SessionState.SetFloat(_key + ".Size", size);
            return true;
        }

        internal bool TrySetPose(Vector3 position, Vector3 angles)
        {
            if (!IsValid(position, angles)) return false;
            Write("Position", position);
            Write("Rotation", angles);
            return true;
        }

        internal bool IsValid(Vector3 position, Vector3 angles) => Finite(position) && Finite(angles) &&
            new Plane(_root.MapRoot.up, _root.MapRoot.position).Raycast(
                new Ray(position, Quaternion.Euler(angles) * Vector3.forward), out _);

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) &&
            float.IsFinite(value.y) && float.IsFinite(value.z);

        private Vector3 Read(string field) => new(
            SessionState.GetFloat(_key + "." + field + "X", 0f),
            SessionState.GetFloat(_key + "." + field + "Y", 0f),
            SessionState.GetFloat(_key + "." + field + "Z", 0f));

        private void Write(string field, Vector3 value)
        {
            SessionState.SetFloat(_key + "." + field + "X", value.x);
            SessionState.SetFloat(_key + "." + field + "Y", value.y);
            SessionState.SetFloat(_key + "." + field + "Z", value.z);
        }
    }
}
#endif
