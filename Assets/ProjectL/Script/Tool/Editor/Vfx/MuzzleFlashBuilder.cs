using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor.Vfx
{
    /// <summary>
    /// 로우 폴리 총구 섬광 이펙트(메시·머티리얼·프리팹)를 생성한다.
    /// 프리팹의 로컬 +Z가 발사 방향이고 원점이 총구 끝이다. 불꽃·중심·연기 모두 3D 메시 파티클이다.
    /// 다시 생성하면 같은 경로의 에셋을 덮어쓴다(GUID 유지). 쓰는 에셋은 모두 RootFolder 안에 있다.
    /// </summary>
    internal static class MuzzleFlashBuilder
    {
        private const string RootFolder = "Assets/ProjectL/Script/Render/Effect/MuzzleFlash";
        private const string FlashShaderPath = RootFolder + "/Shader/LowPolyFlash.shader";
        private const string SparkShaderPath = RootFolder + "/Shader/Spark.shader";
        private const string MaterialFolder = RootFolder + "/Shader";
        private const string MeshFolder = RootFolder + "/Mesh";
        private const string PrefabPath = RootFolder + "/MuzzleFlash_Toon.prefab";

        // 반복 재생 미리보기에서 한 발 간격이 된다.
        private const float Duration = 0.25f;

        // 이펙트 전체 크기 배율. 크기·속도·방출 반경·중력·위치에 함께 곱해 모양 비율을 유지한다.
        private const float EffectScale = 2f;

        // 연기 → 불꽃 → 중심 → 불티 순으로 위에 그린다.
        private const int SmokeQueue = 3000;
        private const int FlameQueue = 3001;
        private const int CoreQueue = 3002;
        private const int SparkQueue = 3003;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int MidColorId = Shader.PropertyToID("_MidColor");
        private static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int FacingPowerId = Shader.PropertyToID("_FacingPower");
        private static readonly int TipCoolingId = Shader.PropertyToID("_TipCooling");
        private static readonly int FacetVariationId = Shader.PropertyToID("_FacetVariation");
        private static readonly int AdditiveId = Shader.PropertyToID("_Additive");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");

        private readonly struct FlashPalette
        {
            public readonly Color Core;
            public readonly Color Mid;
            public readonly Color Outer;
            public readonly float Intensity;
            public readonly float FacingPower;
            public readonly float TipCooling;
            public readonly float FacetVariation;
            public readonly float Additive;
            public readonly float Opacity;
            public readonly float EdgeSoftness;

            public FlashPalette(
                Color core, Color mid, Color outer, float intensity, float facingPower,
                float tipCooling, float facetVariation, float additive, float opacity, float edgeSoftness)
            {
                Core = core;
                Mid = mid;
                Outer = outer;
                Intensity = intensity;
                FacingPower = facingPower;
                TipCooling = tipCooling;
                FacetVariation = facetVariation;
                Additive = additive;
                Opacity = opacity;
                EdgeSoftness = edgeSoftness;
            }
        }

        [MenuItem("ProjectL/VFX/총구 섬광 생성")]
        private static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null
                && !EditorUtility.DisplayDialog(
                    "총구 섬광",
                    $"{PrefabPath}를 다시 만듭니다.\n프리팹을 직접 고친 내용은 사라집니다.",
                    "다시 만들기",
                    "취소"))
            {
                return;
            }

            Shader shader = LoadShader(FlashShaderPath);
            Shader sparkShader = LoadShader(SparkShaderPath);

            EnsureFolder(RootFolder, "Mesh");

            // 굵기 분포·끝 뾰족함·휨이 다른 불꽃 혀 3종을 파티클마다 무작위로 고른다.
            Mesh[] flameMeshes =
            {
                WriteMesh("Flame_A", BuildRevolvedMesh(seed: 11, sides: 10, rings: 12,
                    Profile(baseRoundness: 0.85f, tipSharpness: 1.9f), bend: 0.1f, lobe: 0.12f)),
                WriteMesh("Flame_B", BuildRevolvedMesh(seed: 23, sides: 10, rings: 12,
                    Profile(baseRoundness: 0.8f, tipSharpness: 2.3f), bend: 0.16f, lobe: 0.1f)),
                WriteMesh("Flame_C", BuildRevolvedMesh(seed: 37, sides: 10, rings: 12,
                    Profile(baseRoundness: 0.9f, tipSharpness: 1.7f), bend: 0.06f, lobe: 0.14f)),
            };
            Mesh[] coreMeshes =
            {
                WriteMesh("Core_Blob", BuildRevolvedMesh(seed: 53, sides: 10, rings: 8,
                    Profile(baseRoundness: 0.5f, tipSharpness: 0.5f), bend: 0f, lobe: 0.05f)),
            };
            Mesh[] smokeMeshes =
            {
                WriteMesh("Smoke_Blob", BuildBlobMesh(seed: 71, minRadius: 0.82f, maxRadius: 1.12f)),
                WriteMesh("Smoke_Cube", BuildChamferedCubeMesh(seed: 89)),
            };

            Material flameMaterial = CreateOrUpdateMaterial("MuzzleFlash_Flame", shader, FlameQueue, material => ApplyPalette(material, new FlashPalette(
                new Color(1f, 0.93f, 0.6f), new Color(1f, 0.7f, 0.06f), new Color(1f, 0.3f, 0.02f),
                intensity: 1.1f, facingPower: 1.2f, tipCooling: 0.5f, facetVariation: 0f, additive: 0.15f, opacity: 1f,
                edgeSoftness: 0.2f)));
            Material coreMaterial = CreateOrUpdateMaterial("MuzzleFlash_Core", shader, CoreQueue, material => ApplyPalette(material, new FlashPalette(
                new Color(1f, 0.97f, 0.78f), new Color(1f, 0.88f, 0.42f), new Color(1f, 0.72f, 0.22f),
                intensity: 1f, facingPower: 1f, tipCooling: 0f, facetVariation: 0f, additive: 0.3f, opacity: 1f,
                edgeSoftness: 0.5f)));
            Material smokeMaterial = CreateOrUpdateMaterial("MuzzleFlash_Smoke", shader, SmokeQueue, material => ApplyPalette(material, new FlashPalette(
                new Color(0.9f, 0.89f, 0.88f), new Color(0.72f, 0.71f, 0.71f), new Color(0.5f, 0.49f, 0.51f),
                intensity: 1f, facingPower: 1f, tipCooling: 0f, facetVariation: 0.2f, additive: 0f, opacity: 0.85f,
                edgeSoftness: 0f)));
            Material sparkMaterial = CreateOrUpdateMaterial("MuzzleFlash_Spark", sparkShader, SparkQueue, material =>
            {
                material.SetColor(ColorId, new Color(1f, 0.82f, 0.25f));
                material.SetColor(CoreColorId, new Color(1f, 0.98f, 0.75f));
                material.SetFloat(IntensityId, 1.2f);
                material.SetFloat(AdditiveId, 0.6f);
            });

            GameObject root = BuildHierarchy(
                flameMeshes, coreMeshes, smokeMeshes, flameMaterial, coreMaterial, smokeMaterial, sparkMaterial);
            GameObject prefab;
            try
            {
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException($"프리팹 저장에 실패했습니다: {PrefabPath}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>
        /// 씬에 놓은 이펙트의 반복 재생을 켜고 끈다. 짧은 섬광과 연사 모습을 눈으로 확인할 때 쓴다.
        /// </summary>
        [MenuItem("ProjectL/VFX/선택한 이펙트 반복 재생 전환")]
        private static void ToggleLoopOnSelection()
        {
            ParticleSystem[] systems = Selection.activeGameObject.GetComponentsInChildren<ParticleSystem>(true);
            bool loop = !systems[0].main.loop;

            Undo.RecordObjects(systems, loop ? "이펙트 반복 재생 켜기" : "이펙트 반복 재생 끄기");
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.MainModule main = system.main;
                main.loop = loop;
            }
        }

        [MenuItem("ProjectL/VFX/선택한 이펙트 반복 재생 전환", true)]
        private static bool CanToggleLoopOnSelection()
        {
            GameObject selected = Selection.activeGameObject;
            return selected != null
                && !EditorUtility.IsPersistent(selected)
                && selected.GetComponentInChildren<ParticleSystem>(true) != null;
        }

        private static GameObject BuildHierarchy(
            Mesh[] flameMeshes, Mesh[] coreMeshes, Mesh[] smokeMeshes,
            Material flame, Material core, Material smoke, Material spark)
        {
            // 루트: 발사 축을 따라 뻗는 주 불꽃. 순간 부풀었다가 가늘어지며 사라진다.
            ParticleSystem main = CreateMeshSystem("MuzzleFlash_Toon", null, flame, flameMeshes, 2);
            ParticleSystem.MainModule mainModule = main.main;
            mainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.08f);
            SetStartSize3D(main, 0.085f, 0.105f, 0.51f, 0.61f);
            SetStartRotation3D(main, 6f);
            SetBurst(main, 1, 1);
            SetSizeCurves(main,
                new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0.55f)),
                new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0.8f)));
            SetFade(main, 0.35f, new Color(1f, 0.62f, 0.3f));

            Transform parent = main.transform;

            // 비스듬히 갈라지는 짧은 불꽃 2~3개. 방향이 매번 달라 규칙적인 방사형이 되지 않는다.
            ParticleSystem side = CreateMeshSystem("Flame_Side", parent, flame, flameMeshes, 6);
            ParticleSystem.MainModule sideModule = side.main;
            sideModule.startLifetime = new ParticleSystem.MinMaxCurve(0.045f, 0.065f);
            SetStartSize3D(side, 0.05f, 0.065f, 0.19f, 0.3f);
            SetStartRotation3D(side, 38f);
            SetBurst(side, 2, 3);
            SetSizeCurves(side,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.45f)),
                new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.75f)));
            SetFade(side, 0.3f, new Color(1f, 0.62f, 0.3f));

            // 총구 바로 앞의 작고 밝은 중심. 메시 바닥이 총구에 붙고 앞으로 부푼다.
            ParticleSystem coreSystem = CreateMeshSystem("Core", parent, core, coreMeshes, 2);
            ParticleSystem.MainModule coreModule = coreSystem.main;
            coreModule.startLifetime = new ParticleSystem.MinMaxCurve(0.035f, 0.05f);
            SetStartSize3D(coreSystem, 0.1f, 0.12f, 0.12f, 0.15f);
            SetStartRotation3D(coreSystem, 20f);
            SetBurst(coreSystem, 1, 1);
            var coreCurve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.15f, 1.05f), new Keyframe(1f, 0.5f));
            SetSizeCurves(coreSystem, coreCurve, coreCurve);
            SetFade(coreSystem, 0.25f, new Color(1f, 0.85f, 0.6f));

            // 섬광 뒤에 남는 작은 연기 덩어리. 월드 공간이라 총이 움직여도 제자리에 남는다.
            ParticleSystem puffs = CreateMeshSystem("Smoke", parent, smoke, smokeMeshes, 8);
            puffs.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.World;
            ParticleSystem.MainModule puffsMain = puffs.main;
            puffsMain.startDelay = 0.03f;
            puffsMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            puffsMain.startSizeX = Scaled(0.05f, 0.085f);
            puffsMain.startSizeY = Scaled(0.05f, 0.085f);
            puffsMain.startSizeZ = Scaled(0.05f, 0.085f);
            puffsMain.startSpeed = Scaled(0.5f, 1f);
            puffsMain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            puffsMain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            puffsMain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            puffsMain.gravityModifier = -0.03f * EffectScale;
            puffsMain.simulationSpace = ParticleSystemSimulationSpace.World;
            SetBurst(puffs, 2, 4);
            SetCone(puffs, 20f, 0.02f);
            SetSpeedDecay(puffs, 0.1f);
            var puffCurve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.3f));
            SetSizeCurves(puffs, puffCurve, puffCurve);
            ParticleSystem.RotationOverLifetimeModule puffsRotation = puffs.rotationOverLifetime;
            puffsRotation.enabled = true;
            puffsRotation.separateAxes = true;
            var spin = new ParticleSystem.MinMaxCurve(-120f * Mathf.Deg2Rad, 120f * Mathf.Deg2Rad);
            puffsRotation.x = spin;
            puffsRotation.y = spin;
            puffsRotation.z = spin;
            SetFade(puffs, 0.3f, new Color(0.85f, 0.85f, 0.85f));

            // 총알이 총구를 스치며 튀는 작은 노랑 불티 몇 개. 총구 가장자리에서 앞쪽으로 흩어지고 금방 사라진다.
            ParticleSystem sparks = CreateSystem("Sparks", parent, spark, 6);
            ParticleSystem.MainModule sparksMain = sparks.main;
            sparksMain.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            sparksMain.startSize = Scaled(0.012f, 0.022f);
            sparksMain.startSpeed = Scaled(3.5f, 6f);
            sparksMain.gravityModifier = 0.6f * EffectScale;
            sparksMain.simulationSpace = ParticleSystemSimulationSpace.World;
            SetBurst(sparks, 2, 4);
            SetCone(sparks, 32f, 0.02f);
            ParticleSystem.ShapeModule sparksShape = sparks.shape;
            sparksShape.radiusThickness = 0f;
            SetSpeedDecay(sparks, 0.35f);
            ParticleSystem.SizeOverLifetimeModule sparksSize = sparks.sizeOverLifetime;
            sparksSize.enabled = true;
            sparksSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f)));
            SetFade(sparks, 0.5f, new Color(1f, 0.6f, 0.25f));
            ParticleSystemRenderer sparksRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparksRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparksRenderer.lengthScale = 3f;
            sparksRenderer.velocityScale = 0.02f;

            return main.gameObject;
        }

        /// <summary>
        /// 한 번 터지는 버스트용 파티클 시스템의 공통 설정. 렌더러는 기본 Billboard다.
        /// </summary>
        private static ParticleSystem CreateSystem(string name, Transform parent, Material material, int maxParticles)
        {
            var gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            ParticleSystem system = gameObject.AddComponent<ParticleSystem>();

            // 기본값(5초 무한 반복, 초당 10개)을 한 번 터지는 버스트용으로 바꾼다.
            ParticleSystem.MainModule main = system.main;
            main.duration = Duration;
            main.loop = false;
            main.playOnAwake = true;
            main.startSpeed = 0f;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            return system;
        }

        /// <summary>
        /// 3D 메시를 그리는 파티클 시스템. 기본 정렬은 Local이라 카메라가 아니라 이 Transform의 축(발사 축)을 따른다.
        /// 크기는 축별(X·Y = 굵기, Z = 길이)로 정한다.
        /// </summary>
        private static ParticleSystem CreateMeshSystem(
            string name, Transform parent, Material material, Mesh[] meshes, int maxParticles)
        {
            ParticleSystem system = CreateSystem(name, parent, material, maxParticles);

            ParticleSystem.MainModule main = system.main;
            main.startSize3D = true;
            main.startRotation3D = true;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.SetMeshes(meshes);
            renderer.meshDistribution = ParticleSystemMeshDistribution.UniformRandom;
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.enableGPUInstancing = false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV,
            });

            return system;
        }

        private static ParticleSystem.MinMaxCurve Scaled(float min, float max)
        {
            return new ParticleSystem.MinMaxCurve(min * EffectScale, max * EffectScale);
        }

        private static void SetStartSize3D(
            ParticleSystem system, float widthMin, float widthMax, float lengthMin, float lengthMax)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSizeX = Scaled(widthMin, widthMax);
            main.startSizeY = Scaled(widthMin, widthMax);
            main.startSizeZ = Scaled(lengthMin, lengthMax);
        }

        /// <summary>
        /// 발사 축에서 ±tiltDegrees 안으로 기울이고, 축 둘레로는 무작위로 돌려 매번 다른 단면이 보이게 한다.
        /// </summary>
        private static void SetStartRotation3D(ParticleSystem system, float tiltDegrees)
        {
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            ParticleSystem.MainModule main = system.main;
            main.startRotationX = new ParticleSystem.MinMaxCurve(-tilt, tilt);
            main.startRotationY = new ParticleSystem.MinMaxCurve(-tilt, tilt);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        private static void SetBurst(ParticleSystem system, short minCount, short maxCount)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, minCount, maxCount) });
        }

        private static void SetCone(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius * EffectScale;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
        }

        private static void SetSizeCurves(ParticleSystem system, AnimationCurve width, AnimationCurve length)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = true;
            sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, width);
            sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, width);
            sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, length);
        }

        private static void SetSpeedDecay(ParticleSystem system, float endMultiplier)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -3f),
                new Keyframe(1f, endMultiplier, 0f, 0f)));
        }

        /// <summary>
        /// 밝기와 불투명도를 holdUntil까지 유지한 뒤 빠르게 내리고, 색은 끝으로 갈수록 endTint로 식힌다.
        /// </summary>
        private static void SetFade(ParticleSystem system, float holdUntil, Color endTint)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(endTint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        /// <summary>
        /// 길이 위치 t(0~1)의 반지름을 돌려준다. 최대 반지름은 0.5(지름 1)로 맞춘다.
        /// 모양은 t^baseRoundness × (1 - t)^tipSharpness: 바닥 쪽은 둥글게 부풀고 끝 쪽은 매끈하게 가늘어진다.
        /// baseRoundness가 작을수록 바닥이 빨리 부풀고, tipSharpness가 클수록 끝이 길고 뾰족하다.
        /// </summary>
        private static Func<float, float> Profile(float baseRoundness, float tipSharpness)
        {
            float peakT = baseRoundness / (baseRoundness + tipSharpness);
            float peak = Mathf.Pow(peakT, baseRoundness) * Mathf.Pow(1f - peakT, tipSharpness);
            return t => 0.5f * Mathf.Pow(t, baseRoundness) * Mathf.Pow(1f - t, tipSharpness) / peak;
        }

        /// <summary>
        /// Z축(0 → 1)을 따라 단면을 돌려 만든 매끈한 메시. 바닥(z=0, 총구 쪽)과 끝(z=1)은 한 점으로 모인다.
        /// 단면은 낮은 주파수로만 흔들어(살짝 타원 + 끝으로 갈수록 비틀리는 3갈래 굴곡) 각지지 않은 비대칭을 만들고,
        /// 중심선은 한쪽으로 부드럽게 휜다. 고리는 양 끝에 촘촘하게 놓아 둥근 바닥과 뾰족한 끝을 살린다.
        /// </summary>
        private static SmoothMeshBuilder BuildRevolvedMesh(
            int seed, int sides, int rings, Func<float, float> profile, float bend, float lobe)
        {
            var builder = new SmoothMeshBuilder();
            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            float bendAngle = Range(0f, Mathf.PI * 2f);
            var bendDirection = new Vector3(Mathf.Cos(bendAngle), Mathf.Sin(bendAngle), 0f);
            float ovalPhase = Range(0f, Mathf.PI);
            float lobePhase = Range(0f, Mathf.PI * 2f);
            float twist = Range(-1.2f, 1.2f);

            Vector3 Spine(float t) => bendDirection * (bend * t * t) + new Vector3(0f, 0f, t);

            int basePole = builder.AddVertex(Spine(0f), 0f);
            var ringStart = new int[rings - 1];
            for (int r = 1; r < rings; r++)
            {
                float t = (1f - Mathf.Cos(Mathf.PI * r / rings)) * 0.5f;
                Vector3 center = Spine(t);
                float radius = profile(t);
                ringStart[r - 1] = builder.VertexCount;
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2f / sides;
                    float shape = 1f
                        + lobe * Mathf.Cos(2f * (angle - ovalPhase))
                        + lobe * 0.5f * Mathf.Cos(3f * (angle - lobePhase) + twist * t);
                    var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    builder.AddVertex(center + direction * (radius * shape), t);
                }
            }

            int tipPole = builder.AddVertex(Spine(1f), 1f);

            int firstRing = ringStart[0];
            int lastRing = ringStart[ringStart.Length - 1];
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                builder.AddTriangle(basePole, firstRing + i, firstRing + next, Spine);
                builder.AddTriangle(lastRing + i, lastRing + next, tipPole, Spine);
            }

            for (int r = 0; r < ringStart.Length - 1; r++)
            {
                int current = ringStart[r];
                int upper = ringStart[r + 1];
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    builder.AddTriangle(current + i, current + next, upper + next, Spine);
                    builder.AddTriangle(current + i, upper + next, upper + i, Spine);
                }
            }

            return builder;
        }

        /// <summary>
        /// 꼭짓점 거리를 흔든 정이십면체(연기용). 지름 약 1, 중심이 원점이다.
        /// </summary>
        private static FacetedMeshBuilder BuildBlobMesh(int seed, float minRadius, float maxRadius)
        {
            float phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] vertices =
            {
                new Vector3(-1f, phi, 0f), new Vector3(1f, phi, 0f), new Vector3(-1f, -phi, 0f), new Vector3(1f, -phi, 0f),
                new Vector3(0f, -1f, phi), new Vector3(0f, 1f, phi), new Vector3(0f, -1f, -phi), new Vector3(0f, 1f, -phi),
                new Vector3(phi, 0f, -1f), new Vector3(phi, 0f, 1f), new Vector3(-phi, 0f, -1f), new Vector3(-phi, 0f, 1f),
            };
            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            var builder = new FacetedMeshBuilder(seed);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i].normalized * (0.5f * builder.Range(minRadius, maxRadius));
            }

            AddTriangles(builder, vertices, faces);
            return builder;
        }

        /// <summary>
        /// 모서리를 깎은 정육면체(연기용). 한 변 약 1, 중심이 원점이다.
        /// 여덟 모서리를 각각 다른 깊이로 잘라 삼각 면을 만들고, 꼭짓점을 조금씩 흔들어
        /// 남은 면들이 여러 삼각형으로 꺾이게 한다. 둥글지 않고 깎은 돌 같은 다면체가 된다.
        /// </summary>
        private static FacetedMeshBuilder BuildChamferedCubeMesh(int seed)
        {
            var builder = new FacetedMeshBuilder(seed);
            var points = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f);
                float cut = builder.Range(0.18f, 0.34f);

                // 모서리에서 세 변을 따라 cut만큼 들어온 점 세 개가 깎인 면이 된다.
                points.Add(corner - new Vector3(Mathf.Sign(corner.x) * cut, 0f, 0f));
                points.Add(corner - new Vector3(0f, Mathf.Sign(corner.y) * cut, 0f));
                points.Add(corner - new Vector3(0f, 0f, Mathf.Sign(corner.z) * cut));
            }

            for (int i = 0; i < points.Count; i++)
            {
                var jitter = new Vector3(builder.Range(-0.04f, 0.04f), builder.Range(-0.04f, 0.04f), builder.Range(-0.04f, 0.04f));
                points[i] = (points[i] + jitter) * builder.Range(0.92f, 1.06f);
            }

            AddConvexHull(builder, points);
            return builder;
        }

        /// <summary>
        /// 점들의 볼록 껍질을 삼각형으로 더한다. 나머지 점이 모두 한쪽에 있는 세 점이 껍질 면이다.
        /// 점이 수십 개뿐이라 모든 조합을 검사한다. 점은 흔들어 둬야 한 면에 네 점 이상이 겹치지 않는다.
        /// </summary>
        private static void AddConvexHull(FacetedMeshBuilder builder, IReadOnlyList<Vector3> points)
        {
            const float tolerance = 1e-6f;
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    for (int k = j + 1; k < count; k++)
                    {
                        Vector3 normal = Vector3.Cross(points[j] - points[i], points[k] - points[i]);
                        if (normal.sqrMagnitude < 1e-10f)
                        {
                            continue;
                        }

                        bool front = false;
                        bool back = false;
                        for (int m = 0; m < count && !(front && back); m++)
                        {
                            if (m == i || m == j || m == k)
                            {
                                continue;
                            }

                            float distance = Vector3.Dot(normal, points[m] - points[i]);
                            front |= distance > tolerance;
                            back |= distance < -tolerance;
                        }

                        if (!(front && back))
                        {
                            builder.AddFace(points[i], points[j], points[k], Vector3.zero);
                        }
                    }
                }
            }
        }

        private static void AddTriangles(FacetedMeshBuilder builder, Vector3[] vertices, int[] faces)
        {
            for (int i = 0; i < faces.Length; i += 3)
            {
                builder.AddFace(vertices[faces[i]], vertices[faces[i + 1]], vertices[faces[i + 2]], Vector3.zero);
            }
        }

        private static Mesh WriteMesh(string name, IMeshSource source)
        {
            string path = $"{MeshFolder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh();
            }

            source.ApplyTo(mesh);
            mesh.name = name;

            if (created)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        /// <summary>
        /// 같은 경로의 머티리얼이 있으면 GUID를 유지한 채 이전 셰이더의 속성을 모두 지우고 새로 채운다.
        /// </summary>
        private static Material CreateOrUpdateMaterial(string name, Shader shader, int renderQueue, Action<Material> configure)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var fresh = new Material(shader) { name = name };
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = fresh;
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.CopySerialized(fresh, material);
                UnityEngine.Object.DestroyImmediate(fresh);
            }

            configure(material);
            material.renderQueue = renderQueue;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyPalette(Material material, in FlashPalette palette)
        {
            material.SetColor(CoreColorId, palette.Core);
            material.SetColor(MidColorId, palette.Mid);
            material.SetColor(OuterColorId, palette.Outer);
            material.SetFloat(IntensityId, palette.Intensity);
            material.SetFloat(FacingPowerId, palette.FacingPower);
            material.SetFloat(TipCoolingId, palette.TipCooling);
            material.SetFloat(FacetVariationId, palette.FacetVariation);
            material.SetFloat(AdditiveId, palette.Additive);
            material.SetFloat(OpacityId, palette.Opacity);
            material.SetFloat(EdgeSoftnessId, palette.EdgeSoftness);
        }

        private static Shader LoadShader(string path)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"셰이더를 찾지 못했습니다: {path}");
            }

            return shader;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private interface IMeshSource
        {
            void ApplyTo(Mesh mesh);
        }

        /// <summary>
        /// 꼭짓점을 공유하는 매끈한 메시를 만든다. 법선은 이웃 면을 평균해 부드럽게 이어진다.
        /// UV: x = 길이 방향 위치(0~1), y = 0.5(면 무작위 없음).
        /// </summary>
        private sealed class SmoothMeshBuilder : IMeshSource
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public int VertexCount => _vertices.Count;

            public int AddVertex(Vector3 position, float lengthT)
            {
                _vertices.Add(position);
                _uvs.Add(new Vector2(lengthT, 0.5f));
                return _vertices.Count - 1;
            }

            /// <summary>
            /// 삼각형을 더한다. 같은 높이의 중심선 점(spine)에서 바깥을 향하도록 감는 방향을 맞춘다.
            /// </summary>
            public void AddTriangle(int a, int b, int c, Func<float, Vector3> spine)
            {
                Vector3 pa = _vertices[a];
                Vector3 pb = _vertices[b];
                Vector3 pc = _vertices[c];
                Vector3 normal = Vector3.Cross(pb - pa, pc - pa);
                if (normal.sqrMagnitude < 1e-12f)
                {
                    return;
                }

                Vector3 centroid = (pa + pb + pc) / 3f;
                if (Vector3.Dot(normal, centroid - spine(Mathf.Clamp01(centroid.z))) < 0f)
                {
                    (b, c) = (c, b);
                }

                _triangles.Add(a);
                _triangles.Add(b);
                _triangles.Add(c);
            }

            public void ApplyTo(Mesh mesh)
            {
                mesh.Clear();
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
        }

        /// <summary>
        /// 평면 셰이딩 메시를 만든다(연기용). 삼각형마다 꼭짓점을 따로 두고 면 법선을 넣는다.
        /// UV: x = 0, y = 면마다 다른 난수.
        /// </summary>
        private sealed class FacetedMeshBuilder : IMeshSource
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();
            private readonly System.Random _random;

            public FacetedMeshBuilder(int seed)
            {
                _random = new System.Random(seed);
            }

            public float Range(float min, float max)
            {
                return min + (float)_random.NextDouble() * (max - min);
            }

            /// <summary>
            /// 삼각형을 더한다. 법선이 interior에서 바깥을 향하도록 감는 방향을 맞춘다.
            /// </summary>
            public void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 interior)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-10f)
                {
                    return;
                }

                Vector3 centroid = (a + b + c) / 3f;
                if (Vector3.Dot(normal, centroid - interior) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                normal.Normalize();
                float facet = Range(0f, 1f);
                int start = _vertices.Count;
                AddVertex(a, normal, facet);
                AddVertex(b, normal, facet);
                AddVertex(c, normal, facet);
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
            }

            public void ApplyTo(Mesh mesh)
            {
                mesh.Clear();
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
            }

            private void AddVertex(Vector3 position, Vector3 normal, float facet)
            {
                _vertices.Add(position);
                _normals.Add(normal);
                _uvs.Add(new Vector2(0f, facet));
            }
        }
    }
}
