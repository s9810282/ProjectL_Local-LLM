using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>임시 미리보기 씬에서 실제 알파 블렌딩과 표면 왕복을 검증한다.</summary>
    internal static class CartoonTransparencyVerification
    {
        private const string Output = "Assets/ProjectL/Script/Render/Shader/View/CustomShader/BaseShader/Verification";

        [MenuItem("ProjectL/Render/Verify Cartoon Transparency %&u")]
        private static void Verify()
        {
            UnityEngine.Shader shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(
                "Assets/ProjectL/Script/Render/Shader/View/CustomShader/BaseShader/Shader/Cartoon.shader");
            if (shader == null)
            {
                throw new InvalidOperationException("Cartoon 셰이더를 찾을 수 없다.");
            }
            Scene scene = EditorSceneManager.NewPreviewScene();
            Material material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);
            try
            {
                Directory.CreateDirectory(Output);
                GameObject cameraObject = new GameObject("CartoonAlphaVerification", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.cameraType = CameraType.Preview;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.28f, 0.5f, 1f);
                camera.transform.position = new Vector3(0, 0, -4);
                camera.orthographic = true;
                camera.orthographicSize = 0.75f;
                camera.targetTexture = target;
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                SceneManager.MoveGameObjectToScene(quad, scene);
                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                material.SetFloat("_AmbientStrength", 0f);
                material.SetFloat("_DirectStrength", 0f);
                material.SetFloat("_SpecularIntensity", 0f);
                material.SetFloat("_RimIntensity", 0f);
                material.SetFloat("_OutlineWidth", 0f);
                material.SetFloat("_Cull", (float)CullMode.Off);
                material.SetColor("_EmissionColor", new Color(0.7f, 0.13f, 0.04f, 1f));
                material.SetFloat("_EmissionStrength", 1f);

                material.SetFloat("_Surface", 1f);
                CartoonMaterialGUI.ApplySurface(material);
                Require(material.renderQueue == 3000 && material.GetFloat("_ZWrite") == 0f, "투명 큐/깊이 상태");
                foreach (string pass in new[] { "Outline", "ShadowCaster", "DepthOnly", "DepthNormals", "NormalOnly" })
                {
                    Require(!material.GetShaderPassEnabled(pass), "투명 보조 패스: " + pass);
                }
                Color transparent = Capture(camera, target, pixels, material, 0f, "Cartoon-Alpha0");
                Color background = pixels.GetPixel(2, 2);
                Color half = Capture(camera, target, pixels, material, 0.5f, "Cartoon-AlphaHalf");
                Color solid = Capture(camera, target, pixels, material, 1f, "Cartoon-Alpha1");
                Require(Distance(transparent, background) < 0.025f, "알파 0은 배경과 같아야 한다");
                Require(Distance(solid, transparent) > 0.15f, "알파 1은 배경과 달라야 한다");
                Require(Distance(half, Color.Lerp(transparent, solid, 0.5f)) < 0.035f, "알파 0.5의 실제 블렌딩");
                Texture2D alphaTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    Color texel = new Color(1, 1, 1, 0.5f);
                    alphaTexture.SetPixels(new[] { texel, texel, texel, texel });
                    alphaTexture.Apply();
                    material.SetTexture("_BaseMap", alphaTexture);
                    Color quarter = Capture(camera, target, pixels, material, 0.5f, "Cartoon-TextureAlpha");
                    Require(Distance(quarter, Color.Lerp(transparent, solid, 0.25f)) < 0.035f, "텍스처와 색 알파의 곱");
                }
                finally
                {
                    material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                    UnityEngine.Object.DestroyImmediate(alphaTexture);
                }
                material.SetFloat("_AlphaClip", 1f);
                CartoonMaterialGUI.ApplySurface(material);
                Color clipped = Capture(camera, target, pixels, material, 0.25f, "Cartoon-Cutout");
                Require(Distance(clipped, background) < 0.025f, "투명 모드의 알파 컷아웃 유지");
                material.SetFloat("_Surface", 0f);
                CartoonMaterialGUI.ApplySurface(material);
                Require(material.renderQueue == 2450, "불투명 컷아웃 큐 복구");
                Color opaqueClip = Capture(camera, target, pixels, material, 0.25f, "Cartoon-OpaqueCutout");
                Require(Distance(opaqueClip, background) < 0.025f, "불투명 모드의 알파 컷아웃 유지");
                material.SetFloat("_AlphaClip", 0f);
                CartoonMaterialGUI.ApplySurface(material);
                Color opaque = Capture(camera, target, pixels, material, 0f, "Cartoon-Opaque");
                Require(material.renderQueue == 2000 && material.GetFloat("_ZWrite") == 1f, "불투명 기본 상태 복구");
                Require(Distance(opaque, solid) < 0.025f, "불투명은 기본색 알파를 무시해야 한다");
                foreach (string pass in new[] { "Outline", "ShadowCaster", "DepthOnly", "DepthNormals", "NormalOnly" })
                {
                    Require(material.GetShaderPassEnabled(pass), "불투명 보조 패스 복구: " + pass);
                }
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                {
                    Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, message.message);
                }
                File.WriteAllText(Output + "/Cartoon-transparency.txt", "Unity " + Application.unityVersion +
                    "\n실제 URP 렌더 검증 통과: Alpha 0 / 0.5 / 1, 텍스처×색 알파 0.25, 불투명 알파 무시, 양 모드 컷아웃, 표면 왕복과 보조 패스 복구.\n" +
                    "기존 씬·머티리얼 에셋 수정 없음.\n");
                Debug.Log("[Cartoon] 투명 모드 URP 렌더 및 상태 왕복 검증 통과.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Color Capture(Camera camera, RenderTexture target, Texture2D pixels, Material material, float alpha, string file)
        {
            material.SetColor("_BaseColor", new Color(1, 1, 1, alpha));
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Output + "/" + file + ".png", pixels.EncodeToPNG());
                return pixels.GetPixel(64, 64);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static float Distance(Color a, Color b) => Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));

        private static void Require(bool success, string message)
        {
            if (!success)
            {
                throw new InvalidOperationException("Cartoon 투명 검증 실패: " + message);
            }
        }
    }
}
