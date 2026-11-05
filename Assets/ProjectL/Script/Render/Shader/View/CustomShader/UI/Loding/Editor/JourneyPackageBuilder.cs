using System;
using System.IO;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Editor
{
    /// <summary>이 폴더 안에서만 아틀라스·머티리얼·사용 프리팹을 조립한다.</summary>
    public static class JourneyPackageBuilder
    {
        public const string Root = "Assets/ProjectL/Script/Render/Shader/View/CustomShader/UI";
        public const string PrefabPath = Root + "/Prefabs/SandstormJourney.prefab";
        private const string AtlasPath = Root + "/Textures/JourneyAtlas.png";
        private const string NoisePath = Root + "/Textures/SandstormNoise.png";
        private const string MotionPath = Root + "/Materials/JourneyMotion.asset";
        private const string FontPath = "Assets/ProjectL/Resources/Font/Hahmlet-Bold SDF.asset";

        [MenuItem("ProjectL/Render/UI/Build Sandstorm Journey Assets")]
        public static void BuildAssets()
        {
            BuildAtlas();
            BuildNoise();
            JourneyMotionSettings motion = AssetDatabase.LoadAssetAtPath<JourneyMotionSettings>(MotionPath);
            if (motion == null)
            {
                motion = ScriptableObject.CreateInstance<JourneyMotionSettings>();
                AssetDatabase.CreateAsset(motion, MotionPath);
            }
            motion.ValidateConfiguration();
            Material atlasMaterial = GetMaterial("JourneyAtlasUI", "JourneyAtlas", "JourneyAtlas");
            Material windMaterial = GetMaterial("SandstormUI", "Sandstorm", "Sandstorm");
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            windMaterial.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath));
            windMaterial.SetFloat("_Phase", 0f);
            EditorUtility.SetDirty(windMaterial);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null || !font.HasCharacter('여') || !font.HasCharacter('정') || !font.HasCharacter('중'))
            {
                throw new InvalidOperationException("여정 프리팹에 필요한 기존 Hahmlet 한국어 폰트를 확인할 수 없다.");
            }
            GameObject root = NewRect("SandstormJourney", null, Vector2.zero, Vector2.zero);
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                CanvasGroup visibility = root.AddComponent<CanvasGroup>();
                visibility.alpha = 0f;
                visibility.blocksRaycasts = false;
                visibility.interactable = false;
                RawImage wind = NewRect("Sandstorm", rootRect, Vector2.zero, Vector2.zero).AddComponent<RawImage>();
                wind.rectTransform.anchorMin = Vector2.zero;
                wind.rectTransform.anchorMax = Vector2.one;
                wind.material = windMaterial;
                wind.raycastTarget = true;
                RectTransform layout = NewRect("Journey", rootRect, Vector2.zero, new Vector2(1000, 563)).GetComponent<RectTransform>();
                CanvasGroup journeyVisibility = layout.gameObject.AddComponent<CanvasGroup>();
                journeyVisibility.alpha = 0f;
                journeyVisibility.interactable = false;
                journeyVisibility.blocksRaycasts = false;
                JourneyRoadGraphic road = MakeGraphic<JourneyRoadGraphic>("Road", layout, Vector2.zero, new Vector2(1000, 563), atlas, motion, atlasMaterial);
                JourneyWagonGraphic wagon = MakeGraphic<JourneyWagonGraphic>("Wagon", layout, new Vector2(185, -107), new Vector2(430, 1024f * 430f / 1536f), atlas, motion, atlasMaterial);
                JourneyHorseGraphic horse = MakeGraphic<JourneyHorseGraphic>("Horse", layout, new Vector2(600, -94), new Vector2(240, 724f * 240f / 543f), atlas, motion, atlasMaterial);
                TextMeshProUGUI label = NewRect("JourneyLabel", layout, new Vector2(500, -423), new Vector2(300, 80)).AddComponent<TextMeshProUGUI>();
                label.rectTransform.anchorMin = new Vector2(0, 1);
                label.rectTransform.anchorMax = new Vector2(0, 1);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.font = font;
                label.text = "여정 중\n· · ·";
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.Top;
                label.color = new Color(0.224f, 0.153f, 0.098f, 1f);
                label.raycastTarget = false;
                SandstormTransitionVisual visual = root.AddComponent<SandstormTransitionVisual>();
                SerializedObject so = new SerializedObject(visual);
                Assign(so, "wind", wind);
                Assign(so, "visibility", visibility);
                Assign(so, "journeyVisibility", journeyVisibility);
                Assign(so, "journeyLayout", layout);
                Assign(so, "road", road);
                Assign(so, "wagon", wagon);
                Assign(so, "horse", horse);
                so.ApplyModifiedPropertiesWithoutUndo();
                visual.ValidateConfiguration();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssetIfDirty(motion);
                AssetDatabase.SaveAssetIfDirty(atlasMaterial);
                AssetDatabase.SaveAssetIfDirty(windMaterial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            Debug.Log("[JourneyUI] 실사용 에셋 조립 완료: " + PrefabPath);
        }

        /// <summary>배치 검증 진입점. 기존 씬을 열거나 저장하지 않는다.</summary>
        [MenuItem("ProjectL/Render/UI/Validate Sandstorm Journey Assets %&j")]
        public static void BuildAndVerify()
        {
            BuildAssets();
            VerifyShader("SandstormUI");
            VerifyShader("JourneyAtlasUI");
            Capture(1920, 1080, "Covered-1920x1080", true);
            Capture(1080, 1920, "Covered-1080x1920", true);
            Capture(1280, 720, "Hidden-1280x720", false);
            Capture(1000, 563, "Cover-Midpoint", false, SandstormPhase.Covering, 0.475);
            Capture(1000, 563, "Reveal-Midpoint", false, SandstormPhase.Revealing, 0.525);
            Capture(1000, 563, "Reveal-Early", false, SandstormPhase.Revealing, 0.2625);
            Capture(1000, 563, "Reveal-Late", false, SandstormPhase.Revealing, 0.7875);
            for (int frame = 0; frame < 4; frame++)
            {
                Capture(1000, 563, "Gait-" + frame, true, SandstormPhase.Covered, frame * 0.255);
            }
            VerifyShader("SandstormUI");
            VerifyShader("JourneyAtlasUI");
            File.WriteAllText(Root + "/Verification/asset-validation.txt", "Unity " + Application.unityVersion + "\n아틀라스/프리팹 생성, 셰이더 오류 0, 가로·세로·숨김 렌더 완료.\n");
        }

        private static void Capture(int width, int height, string fileName, bool covered,
            SandstormPhase phase = SandstormPhase.Hidden, double time = 2.8)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D capture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                GameObject cameraObject = new GameObject("JourneyAssetValidationCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.cameraType = CameraType.Game;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.22f, 0.34f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 281.5f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.targetTexture = target;
                camera.cullingMask = 1 << 5;
                GameObject canvasObject = new GameObject("JourneyAssetValidationCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                canvasObject.layer = 5;
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(563f * width / height, 563f);
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1000, 563);
                scaler.matchWidthOrHeight = 0.5f;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                instance.transform.SetParent(canvas.transform, false);
                Canvas.ForceUpdateCanvases();
                SandstormTransitionVisual visual = instance.GetComponent<SandstormTransitionVisual>();
                visual.UseExternalClock = true;
                if (phase == SandstormPhase.Covering)
                {
                    visual.Cover();
                    visual.Advance(time);
                }
                else if (phase == SandstormPhase.Revealing)
                {
                    visual.ShowCovered();
                    visual.Reveal();
                    visual.Advance(time);
                }
                else if (covered)
                {
                    visual.ShowCovered();
                    visual.Advance(time);
                }
                Canvas.ForceUpdateCanvases();
                File.WriteAllText(Root + "/Verification/" + fileName + "-state.txt",
                    "phase=" + visual.Phase + " rootAlpha=" + visual.GetComponent<CanvasGroup>().alpha +
                    " rootRect=" + visual.GetComponent<RectTransform>().rect + " windRect=" + visual.GetComponentInChildren<RawImage>().rectTransform.rect +
                    " windCull=" + visual.GetComponentInChildren<RawImage>().canvasRenderer.cull +
                    " inheritedAlpha=" + visual.GetComponentInChildren<RawImage>().canvasRenderer.GetInheritedAlpha() +
                    " shaderPhase=" + visual.GetComponentInChildren<RawImage>().materialForRendering.GetFloat("_Phase") +
                    " rendererDepth=" + visual.GetComponentInChildren<RawImage>().canvasRenderer.absoluteDepth + " cameraScene=" + camera.scene.name);
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    capture.Apply();
                }
                finally
                {
                    RenderTexture.active = previous;
                }
                Color corner = capture.GetPixel(8, 8);
                File.WriteAllBytes(Root + "/Verification/" + fileName + ".png", capture.EncodeToPNG());
                if (covered && corner.b > corner.r)
                {
                    throw new InvalidOperationException("모래바람이 화면 모서리를 가리지 못했다: " + fileName);
                }
                Color opposite = capture.GetPixel(width - 9, 8);
                bool leftIsSand = corner.r > corner.b;
                bool rightIsSand = opposite.r > opposite.b;
                if ((!covered && phase == SandstormPhase.Hidden && (leftIsSand || rightIsSand)) ||
                    (phase == SandstormPhase.Covering && (!leftIsSand || rightIsSand)))
                {
                    throw new InvalidOperationException("숨김 또는 전환 마스크 방향이 잘못됐다: " + fileName);
                }
                if (phase == SandstormPhase.Revealing &&
                    (visual.GetComponent<CanvasGroup>().alpha <= 0f || visual.GetComponent<CanvasGroup>().alpha >= 1f ||
                     Mathf.Abs(corner.r - opposite.r) > 0.12f))
                {
                    throw new InvalidOperationException("완료 페이드가 전체 화면에 적용되지 않았다: " + fileName);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                UnityEngine.Object.DestroyImmediate(capture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Material GetMaterial(string shaderFile, string shaderName, string materialName)
        {
            UnityEngine.Shader shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(Root + "/Shaders/" + shaderFile + ".shader");
            if (shader == null)
            {
                throw new InvalidOperationException("여정 셰이더가 없다: " + shaderName);
            }
            string path = Root + "/Materials/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.shader != shader)
            {
                throw new InvalidOperationException("기존 여정 머티리얼의 셰이더가 다르다: " + path);
            }
            return material;
        }

        private static void VerifyShader(string shaderFile)
        {
            UnityEngine.Shader shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(Root + "/Shaders/" + shaderFile + ".shader");
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
            {
                if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                {
                    throw new InvalidOperationException(shaderFile + ": " + message.message);
                }
            }
        }

        private static GameObject NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.layer = 5;
            RectTransform rect = result.GetComponent<RectTransform>();
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return result;
        }

        private static T MakeGraphic<T>(string name, RectTransform parent, Vector2 position, Vector2 size,
            Texture2D atlas, JourneyMotionSettings motion, Material material) where T : JourneyGraphic
        {
            T graphic = NewRect(name, parent, position, size).AddComponent<T>();
            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            graphic.material = material;
            graphic.raycastTarget = false;
            SerializedObject so = new SerializedObject(graphic);
            Assign(so, "atlas", atlas);
            Assign(so, "motion", motion);
            so.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(serialized.targetObject.name + ": 필드가 없다: " + field);
            }
            property.objectReferenceValue = value;
        }

        private static void BuildAtlas()
        {
            Texture2D wagon = ReadSource("JourneyWagonSide.png");
            Texture2D horse = ReadSource("JourneyHorseSideStrip.png");
            Texture2D atlas = new Texture2D(JourneySpriteLayout.AtlasSize, JourneySpriteLayout.AtlasSize, TextureFormat.RGBA32, false);
            try
            {
                if (wagon.width != 1536 || wagon.height != 1024 || horse.width != 2172 || horse.height != 724)
                {
                    throw new InvalidOperationException("여정 원본 이미지 크기가 SpriteLayout 계약과 다르다.");
                }
                Color32[] output = new Color32[JourneySpriteLayout.AtlasSize * JourneySpriteLayout.AtlasSize];
                Blit(output, wagon.GetPixels32(), wagon.width, wagon.height, JourneySpriteLayout.Wagon, 0, ReadOnlySpan<Vector2>.Empty);
                Color32[] horsePixels = horse.GetPixels32();
                int sourceX = JourneySpriteLayout.HorseSourceFrame * JourneySpriteLayout.HorseWidth;
                Blit(output, horsePixels, horse.width, horse.height, JourneySpriteLayout.HorseBody, sourceX, JourneySpriteLayout.BodyMask);
                Blit(output, horsePixels, horse.width, horse.height, JourneySpriteLayout.HorseHind, sourceX, JourneySpriteLayout.HindMask);
                Blit(output, horsePixels, horse.width, horse.height, JourneySpriteLayout.HorseFore, sourceX, JourneySpriteLayout.ForeMask);
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 2044; x < 2048; x++)
                    {
                        output[y * 2048 + x] = new Color32(255, 255, 255, 255);
                    }
                }
                atlas.SetPixels32(output);
                atlas.Apply();
                File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
                ImportTexture(AtlasPath, false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wagon);
                UnityEngine.Object.DestroyImmediate(horse);
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private static Texture2D ReadSource(string file)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(Root + "/SourceArt/" + file)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("여정 원본 PNG를 읽을 수 없다: " + file);
            }
            return texture;
        }

        private static void Blit(Color32[] output, Color32[] source, int width, int height, RectInt destination, int sourceX, ReadOnlySpan<Vector2> mask)
        {
            for (int y = 0; y < destination.height; y++)
            {
                for (int x = 0; x < destination.width; x++)
                {
                    Color32 pixel = source[(height - 1 - y) * width + sourceX + x];
                    if (!mask.IsEmpty && !Contains(mask, new Vector2(x + 0.5f, y + 0.5f)))
                    {
                        pixel.a = 0;
                    }
                    int target = (JourneySpriteLayout.AtlasSize - 1 - destination.y - y) * JourneySpriteLayout.AtlasSize + destination.x + x;
                    output[target] = pixel;
                }
            }
        }

        private static bool Contains(ReadOnlySpan<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        private static void BuildNoise()
        {
            Texture2D noise = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            try
            {
                Color32[] pixels = new Color32[64 * 64];
                uint seed = 12761;
                for (int i = 0; i < pixels.Length; i++)
                {
                    seed = unchecked(seed * 1664525 + 1013904223);
                    byte value = (byte)Math.Round(seed / 4294967296.0 * 255.0);
                    pixels[i] = new Color32(value, value, value, 255);
                }
                noise.SetPixels32(pixels);
                noise.Apply();
                File.WriteAllBytes(NoisePath, noise.EncodeToPNG());
                ImportTexture(NoisePath, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(noise);
            }
        }

        private static void ImportTexture(string path, bool noise)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = !noise;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !noise;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = noise ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
