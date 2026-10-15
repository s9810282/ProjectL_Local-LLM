using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem
{
    internal static class MaterialOvrridFunction
    {
        // ����ó�� ��ϵ� float Ÿ�� ���� ���� �Լ�
        internal static void OverrideFloat(MaterialPropertyBlock block, int nameID, float value)
        {
            block.SetFloat(nameID, value);
        }

        // ����ó�� ��ϵ� Color Ÿ�� ���� ���� �Լ�
        internal static void OverrideColor(MaterialPropertyBlock block, int nameID, Color value)
        {
            block.SetColor(nameID, value);
        }

        // ����ó�� ��ϵ� int Ÿ�� ���� ���� �Լ�
        internal static void OverrideInt(MaterialPropertyBlock block, int nameID, int value)
        {
            block.SetInteger(nameID, value);
        }

        // ����ó�� ��ϵ� Vector4 Ÿ�� ���� ���� �Լ�
        internal static void OverrideVector(MaterialPropertyBlock block, int nameID, Vector4 value)
        {
            block.SetVector(nameID, value);
        }

    }
}
