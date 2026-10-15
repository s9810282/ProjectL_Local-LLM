using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.Enum;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem
{
    // ������ ����/������ �� �ʿ��� �ʱ� ���� �����͸� ��� DTO
    public struct LightSnapshotData
    {
        public readonly LightPresetType PresetType; // Ÿ��
        public readonly Transform Parent;           // ����� �θ�
        public readonly Vector3 LocalPosition;      // ��ġ (�θ� ���� ����)
        public readonly Quaternion LocalRotation;   // ȸ�� (�θ� ���� ����)
        public readonly float Intensity;            // ���
        public readonly Color Color;                // �÷�

        public LightSnapshotData(
            LightPresetType presetType,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float intensity,
            Color color)
        {
            PresetType = presetType;
            Parent = parent;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Intensity = intensity;
            Color = color;
        }
    }
}