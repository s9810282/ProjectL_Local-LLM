using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProjectL.Tool.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectL.Render.RendererFeatures.Systems.EffectOcclusionOutline.Editor
{
    /// <summary>기존 씬을 유지하고 임시 파이프라인/씬에서 실제 GPU 출력을 대조한다.</summary>
    internal static class EffectOcclusionOutlineVerification
    {
        private const string Output = EffectOcclusionOutlineInstaller.Root + "/Verification";
        private static bool _running;

        [MenuItem("ProjectL/Render/Verify Effect Occlusion Outline %&o")]
        private static void Verify()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("검증은 재생을 끈 상태에서 한 번씩 실행하세요.");
            }
            var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (source == null)
            {
                throw new InvalidOperationException("활성 URP 파이프라인이 없다.");
            }
            _running = true;
            RenderPipelineAsset previous = QualitySettings.renderPipeline;
            var pipeline = UnityEngine.Object.Instantiate(source);
            var renderer = UnityEngine.Object.Instantiate(source.rendererDataList[0]);
            renderer.rendererFeatures.Clear();
            var rendererProperties = new SerializedObject(renderer);
            rendererProperties.FindProperty("m_OpaqueLayerMask").intValue = -1;
            rendererProperties.FindProperty("m_TransparentLayerMask").intValue = -1;
            rendererProperties.FindProperty("m_DefaultStencilState").FindPropertyRelative("overrideStencilState").boolValue = false;
            rendererProperties.ApplyModifiedPropertiesWithoutUndo();
            var feature = ScriptableObject.CreateInstance<EffectOcclusionOutlineFeature>();
            feature.SetShader(AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(EffectOcclusionOutlineInstaller.Root + "/EffectOcclusionOutline.shader"));
            renderer.rendererFeatures.Add(feature);
            renderer.SetDirty();
            var serialized = new SerializedObject(pipeline);
            SerializedProperty renderers = serialized.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.msaaSampleCount = 1;
            QualitySettings.renderPipeline = pipeline;
            // 다음 에디터 갱신에서 임시 파이프라인으로 전환된 뒤 그린다.
            IEnumerator routine = Run(feature);
            void Tick()
            {
                bool complete = false;
                try
                {
                    complete = !routine.MoveNext();
                }
                catch (Exception exception)
                {
                    complete = true;
                    Debug.LogException(exception);
                }
                finally
                {
                    if (complete)
                    {
                        EditorApplication.update -= Tick;
                        (routine as IDisposable)?.Dispose();
                        QualitySettings.renderPipeline = previous;
                        UnityEngine.Object.DestroyImmediate(pipeline);
                        UnityEngine.Object.DestroyImmediate(renderer);
                        UnityEngine.Object.DestroyImmediate(feature);
                        _running = false;
                    }
                }
            }
            EditorApplication.delayCall += () => EditorApplication.update += Tick;
        }

        private static IEnumerator Run(EffectOcclusionOutlineFeature feature)
        {
            Directory.CreateDirectory(Output);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var owned = new List<UnityEngine.Object>();
            RenderTexture target = new RenderTexture(192, 192, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(192, 192, TextureFormat.RGBA32, false, true);
            owned.Add(target);
            owned.Add(pixels);
            var report = new List<string> { "Unity " + Application.unityVersion, "실제 GPU 출력 비교" };
            try
            {
                var cameraObject = new GameObject("EffectOutlineVerificationCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.transform.position = new Vector3(0, 0, -5);
                camera.orthographic = true;
                camera.orthographicSize = 1.5f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 30f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1);
                camera.cullingMask = (1 << 6) | (1 << 7) | (1 << 12);
                camera.allowMSAA = false;
                camera.allowHDR = false;
                camera.targetTexture = target;
                camera.GetUniversalAdditionalCameraData().SetRenderer(0);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

                var cartoon = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(
                    "Assets/ProjectL/Script/Render/Shader/View/CustomShader/BaseShader/Shader/Cartoon.shader");
                Material characterMaterial = MakeMaterial(cartoon, new Color(0.1f, 0.3f, 0.7f), owned);
                Material effectMaterial = MakeMaterial(cartoon, new Color(0.65f, 0.4f, 0.18f), owned);
                Material wallMaterial = MakeMaterial(cartoon, new Color(0.2f, 0.2f, 0.2f), owned);
                GameObject character = Primitive(scene, PrimitiveType.Sphere, "Character", 6, characterMaterial);
                character.transform.localScale = Vector3.one * 1.4f;
                GameObject effect = Primitive(scene, PrimitiveType.Quad, "Effect", 7, effectMaterial);
                effect.transform.localScale = Vector3.one * 3f;
                effect.transform.position = new Vector3(0, 0, -1f);
                GameObject wall = Primitive(scene, PrimitiveType.Quad, "Wall", 12, wallMaterial);
                wall.transform.localScale = Vector3.one * 3f;
                wall.transform.position = new Vector3(0, 0, -0.85f);
                wall.SetActive(false);

                feature.Settings.characterLayers = 1 << 6;
                feature.Settings.effectLayers = 1 << 7;
                feature.Settings.outlineColor = new Color(0f, 1f, 0f, 1f);
                feature.Settings.widthPixels = 4f;
                feature.Settings.minimumOpacity = 0.02f;
                feature.Settings.opacitySoftness = 0.8f;

                Color[] Capture(string name)
                {
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, 192, 192), 0, 0);
                        pixels.Apply();
                        File.WriteAllBytes(Output + "/" + name + ".png", pixels.EncodeToPNG());
                        return pixels.GetPixels();
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                }
                int Difference(string name)
                {
                    feature.SetActive(false);
                    Color[] baseline = Capture(name + "-off");
                    feature.SetActive(true);
                    Color[] enabled = Capture(name);
                    int count = 0;
                    for (int i = 0; i < enabled.Length; i++)
                    {
                        if (enabled[i].g - baseline[i].g > 0.06f)
                        {
                            count++;
                        }
                    }
                    report.Add(name + ": 윤곽선이 추가된 픽셀 수 = " + count);
                    return count;
                }
                void Check(bool condition, string name)
                {
                    if (!condition)
                    {
                        throw new InvalidOperationException("Effect Outline 검증 실패: " + name + "\n" + string.Join("\n", report));
                    }
                    report.Add("PASS: " + name);
                }

                effectMaterial.SetFloat("_Surface", 1f);
                effectMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.7f));
                CartoonMaterialGUI.ApplySurface(effectMaterial);
                yield return null;
                int front = Difference("01-transparent-front");
                Check(front > 80, "반투명 이펙트 앞: 선 표시");
                int[] quadrants = new int[4];
                Color[] silhouette = pixels.GetPixels();
                for (int y = 0; y < 192; y++)
                {
                    for (int x = 0; x < 192; x++)
                    {
                        Color pixel = silhouette[y * 192 + x];
                        if (pixel.g > 0.8f && pixel.r < 0.2f)
                        {
                            quadrants[(y >= 96 ? 2 : 0) + (x >= 96 ? 1 : 0)]++;
                        }
                    }
                }
                Check(Array.TrueForAll(quadrants, count => count > 40), "상하좌우 바깥 윤곽 모두 표시: " + string.Join(",", quadrants));
                effect.transform.position = new Vector3(0, 0, 1.5f);
                yield return null;
                Check(Difference("02-effect-behind") == 0, "이펙트 뒤: 선 없음");
                effect.transform.position = new Vector3(0, 0, -1f);
                effectMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0));
                yield return null;
                Check(Difference("03-alpha-zero") == 0, "알파 0: 선 없음");
                effectMaterial.SetFloat("_Surface", 0f);
                CartoonMaterialGUI.ApplySurface(effectMaterial);
                yield return null;
                Check(Difference("04-opaque-alpha-zero") > 80, "Opaque는 기본색 알파 0이어도 가림");
                effectMaterial.SetFloat("_AlphaClip", 1f);
                CartoonMaterialGUI.ApplySurface(effectMaterial);
                yield return null;
                Check(Difference("05-opaque-clipped") == 0, "Opaque 컷아웃 구멍: 선 없음");
                effectMaterial.SetFloat("_AlphaClip", 0f);
                effectMaterial.SetFloat("_Surface", 1f);
                effectMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.7f));
                CartoonMaterialGUI.ApplySurface(effectMaterial);
                wall.SetActive(true);
                yield return null;
                Check(Difference("06-wall-between") == 0, "연기와 캐릭터 사이 벽: 캐릭터 노출 방지");
                wall.transform.position = new Vector3(0, 0, -1.5f);
                yield return null;
                Check(Difference("07-wall-front") == 0, "연기 앞의 벽: 선 없음");
                wall.SetActive(false);
                feature.Settings.widthPixels = 2f;
                yield return null;
                int thin = Difference("08-thin");
                feature.Settings.widthPixels = 7f;
                yield return null;
                int thick = Difference("09-thick");
                Check(thin > 0 && thick > thin * 1.5f, "굵기 조절: 메시 확장량 증가");
                feature.Settings.effectLayers = 1 << 13;
                yield return null;
                Check(Difference("10-unselected-layer") == 0, "선택하지 않은 이펙트 레이어 제외");
                feature.Settings.effectLayers = 1 << 7;
                camera.orthographic = false;
                camera.fieldOfView = 35f;
                yield return null;
                Check(Difference("11-perspective-front") > 80, "원근 카메라 앞: 선 표시");
                effect.transform.position = new Vector3(0, 0, 1.5f);
                yield return null;
                Check(Difference("12-perspective-behind") == 0, "원근 카메라 뒤: 선 없음");

                camera.orthographic = true;
                effect.transform.position = new Vector3(0, 0, -1f);
                feature.Settings.widthPixels = 4f;
                var halfTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                owned.Add(halfTexture);
                halfTexture.SetPixels(new[] { new Color(1, 1, 1, 0), Color.white, new Color(1, 1, 1, 0), Color.white });
                halfTexture.Apply();
                effectMaterial.SetTexture("_BaseMap", halfTexture);
                yield return null;
                int partial = Difference("13-texture-partial");
                Check(partial > 80 && partial < front * 0.65f, "텍스처의 투명한 절반에는 선 없음");
                int left = 0;
                int right = 0;
                for (int y = 0; y < 192; y++)
                {
                    for (int x = 0; x < 192; x++)
                    {
                        Color pixel = pixels.GetPixel(x, y);
                        if (pixel.g > 0.8f && pixel.r < 0.2f)
                        {
                            if (x < 96) { left++; } else { right++; }
                        }
                    }
                }
                Check(left == 0 && right > 80, "부분 가림 위치 일치: 왼쪽 " + left + ", 오른쪽 " + right);
                effectMaterial.SetFloat("_Surface", 0f);
                effectMaterial.SetFloat("_AlphaClip", 1f);
                CartoonMaterialGUI.ApplySurface(effectMaterial);
                yield return null;
                int cutout = Difference("14-cutout-partial");
                Check(cutout > 80 && cutout < front * 0.65f, "Opaque 컷아웃 텍스처의 구멍에도 선 없음");
                foreach (UnityEngine.Shader shader in new[] { cartoon, UnityEngine.Shader.Find("Hidden/ProjectL/EffectOcclusionOutline") })
                {
                    foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    {
                        Check(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, message.message);
                    }
                }
                report.Add("기존 씬/머티리얼/Renderer Data 에셋 수정 없음. 임시 파이프라인 원복.");
                File.WriteAllText(Output + "/verification.txt", string.Join("\n", report) + "\n");
                Debug.Log("[Effect Outline] 실제 URP 렌더 검증 통과. " + Output);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                target.Release();
                foreach (UnityEngine.Object item in owned)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }
        }

        private static Material MakeMaterial(UnityEngine.Shader shader, Color color, List<UnityEngine.Object> owned)
        {
            var material = new Material(shader);
            owned.Add(material);
            material.SetFloat("_AmbientStrength", 0);
            material.SetFloat("_DirectStrength", 0);
            material.SetFloat("_SpecularIntensity", 0);
            material.SetFloat("_RimIntensity", 0);
            material.SetFloat("_OutlineWidth", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetColor("_EmissionColor", color);
            CartoonMaterialGUI.ApplySurface(material);
            return material;
        }

        private static GameObject Primitive(Scene scene, PrimitiveType type, string name, int layer, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.layer = layer;
            SceneManager.MoveGameObjectToScene(item, scene);
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }
    }
}
