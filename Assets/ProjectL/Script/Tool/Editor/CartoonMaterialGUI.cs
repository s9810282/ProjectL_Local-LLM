using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor
{
    /// <summary>Cartoon의 표면 타입과 실제 렌더 상태를 동기화한다.</summary>
    public sealed class CartoonMaterialGUI : ShaderGUI
    {
        private const string CartoonShaderName = "ProjectL/BaseShader/Cartoon";
        private const string EntityCartoonShaderName = "ProjectL/BaseShader/EntityCartoon";

        private static readonly string[] OpaquePasses =
        {
            "Outline", "ShadowCaster", "DepthOnly", "DepthNormals", "NormalOnly"
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperty("_Surface", properties);
            EditorGUI.BeginChangeCheck();
            materialEditor.PropertiesDefaultGUI(properties);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(materialEditor.targets, "Cartoon 표면 상태 변경");
                foreach (UnityEngine.Object target in materialEditor.targets)
                {
                    ApplySurface((Material)target);
                }
            }
            MaterialProperty surface = FindProperty("_Surface", properties);
            if (!surface.hasMixedValue && surface.floatValue > 0.5f)
            {
                EditorGUILayout.HelpBox("투명도 = 기본 텍스처 알파 × 기본 색 알파. 깊이·그림자·외곽선 출력은 비활성화됩니다. 알파 컷아웃은 별도로 적용됩니다.", MessageType.Info);
            }
        }

        public override void ValidateMaterial(Material material) => ApplySurface(material);

        public override void AssignNewShaderToMaterial(Material material, UnityEngine.Shader oldShader, UnityEngine.Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            if (newShader != null && IsCartoonShader(newShader))
            {
                ApplySurface(material);
            }
        }

        /// <summary>표면 타입을 바꾼 뒤 렌더 상태를 적용한다. 색·텍스처·컷아웃 값은 보존한다.</summary>
        public static void ApplySurface(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }
            if (material.shader == null || !IsCartoonShader(material.shader))
            {
                throw new InvalidOperationException(material.name + ": Cartoon 또는 EntityCartoon 머티리얼이 필요하다.");
            }
            float surface = material.GetFloat("_Surface");
            if (surface != 0f && surface != 1f)
            {
                throw new InvalidOperationException(material.name + ": 표면 타입은 Opaque(0) 또는 Transparent(1)여야 한다.");
            }
            bool transparent = surface == 1f;
            bool cutout = material.GetFloat("_AlphaClip") > 0.5f;
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : cutout ? "TransparentCutout" : "Opaque");
            material.renderQueue = (int)(transparent ? RenderQueue.Transparent : cutout ? RenderQueue.AlphaTest : RenderQueue.Geometry);
            foreach (string pass in OpaquePasses)
            {
                material.SetShaderPassEnabled(pass, !transparent);
            }
        }

        private static bool IsCartoonShader(Shader shader)
        {
            return shader.name == CartoonShaderName || shader.name == EntityCartoonShaderName;
        }
    }
}
