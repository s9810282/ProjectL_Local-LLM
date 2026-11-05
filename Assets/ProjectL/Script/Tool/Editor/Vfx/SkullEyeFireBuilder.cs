using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor.Vfx
{
    /// <summary>
    /// 해골 눈 불꽃 이펙트(메시·머티리얼·프리팹)를 생성한다. 눈 하나용이며 계속 타오르는 루프 이펙트다.
    /// 프리팹 루트가 눈 중심이고, 로컬 +Y가 위(불꽃이 오르는 방향), 로컬 +Z가 눈구멍 바깥(보는 방향)이다.
    /// 루트에는 그리지 않는 컨트롤러 파티클 시스템이 있어, 루트만 선택·재생해도 자식 파티클이 모두 함께 돈다.
    /// 구성: Core(MeshRenderer 구체) · BigFlame(MeshRenderer 큰 불꽃 3층) · FlameLicks(떨어져 나가는 불 조각, 로컬)
    /// · Flames(메시 파티클, 로컬) · Smoke(작은 메시 조각 파티클, 로컬) · Embers(불씨 메시 파티클 + 꼬리, 월드).
    /// 다시 생성하면 메시·머티리얼은 같은 에셋을 고쳐 쓰고(GUID 유지), 프리팹은 루트를 유지한 채 자식만 다시 만든다.
    /// 구체는 전용 GlowOrb, 불꽃은 물결치는 전용 WavyFlame, 불씨 꼬리는 전용 EmberTrail 셰이더를 쓰고,
    /// 연기·불씨 몸통은 총구 섬광의 LowPolyFlash 셰이더를 수정 없이 참조한다.
    /// </summary>
    internal static class SkullEyeFireBuilder
    {
        private const string RootFolder = "Assets/ProjectL/Script/Render/Effect/SkullEyeFire";
        private const string MaterialFolder = RootFolder + "/Shader";
        private const string MeshFolder = RootFolder + "/Mesh";
        private const string PrefabPath = RootFolder + "/SkullEyeFire.prefab";
        private const string OrbShaderPath = RootFolder + "/Shader/GlowOrb.shader";
        private const string FlameShaderPath = RootFolder + "/Shader/WavyFlame.shader";
        private const string EmberTrailShaderPath = RootFolder + "/Shader/EmberTrail.shader";
        private const string SmokeShaderPath = "Assets/ProjectL/Script/Render/Effect/MuzzleFlash/Shader/LowPolyFlash.shader";

        // 이펙트 전체 크기 배율. 큰 해골에 맞춰 기본 8배(기준 길이 0.8)로 둔다.
        private const float EffectScale = 8f;

        // 이펙트 기준 길이. 불꽃·연기·불씨의 위치·크기·속도는 이 값에 대한 비율로 정한다.
        private const float BaseSize = 0.1f * EffectScale;

        // 눈 구체 지름(기준 길이 대비). 작은 불꽃이 나오는 자리도 구체 표면에 맞춰 함께 줄어든다.
        private const float OrbScale = 0.7f;

        // 불꽃 층 메시에서 아래 둥근 구슬이 끝나는(가장 넓은) 높이 비율. 큰 불꽃 층을 이 높이로 눈 중심에 맞춘다.
        private const float LayerBulbEnd = 0.24f;

        // 구체 → 큰 불꽃(바깥·중간·안쪽) → 불 조각 → 연기 → 작은 불꽃 → 불씨 순으로 그린다.
        // 구체는 깊이를 써서 뒤쪽 불꽃·연기를 가린다. 큰 불꽃은 바깥 층부터 그려 안쪽 층이 위에 겹친다.
        private const int OrbQueue = 2990;
        private const int BigFlameQueue = 2993;
        private const int LickQueue = 2997;
        private const int SmokeQueue = 2999;
        private const int FlameQueue = 3000;
        private const int EmberQueue = 3001;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int MidColorId = Shader.PropertyToID("_MidColor");
        private static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int CoreStartId = Shader.PropertyToID("_CoreStart");
        private static readonly int CoreSoftnessId = Shader.PropertyToID("_CoreSoftness");
        private static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int FacingPowerId = Shader.PropertyToID("_FacingPower");
        private static readonly int TipCoolingId = Shader.PropertyToID("_TipCooling");
        private static readonly int FacetVariationId = Shader.PropertyToID("_FacetVariation");
        private static readonly int AdditiveId = Shader.PropertyToID("_Additive");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");
        private static readonly int WaveLengthId = Shader.PropertyToID("_WaveLength");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int FlickerAmountId = Shader.PropertyToID("_FlickerAmount");
        private static readonly int FlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");

        [MenuItem("ProjectL/VFX/해골 눈 불꽃 생성")]
        private static void Build()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            if (exists && !EditorUtility.DisplayDialog(
                    "해골 눈 불꽃",
                    $"{PrefabPath}의 자식(Core·BigFlame·FlameLicks·Flames·Smoke·Embers)을 다시 만듭니다.\n"
                    + "루트와 씬에 놓인 인스턴스 연결은 유지되지만, 프리팹 안에서 직접 고친 내용은 사라집니다.",
                    "다시 만들기",
                    "취소"))
            {
                return;
            }

            Shader orbShader = LoadShader(OrbShaderPath);
            Shader flameShader = LoadShader(FlameShaderPath);
            Shader smokeShader = LoadShader(SmokeShaderPath);
            Shader emberTrailShader = LoadShader(EmberTrailShaderPath);

            EnsureFolder(RootFolder, "Mesh");

            Mesh orbMesh = WriteMesh("Orb", mesh => BuildOrbMesh().WriteSmooth(mesh));
            Mesh emitterMesh = WriteMesh("FlameEmitter", mesh => WriteEmitterShell(mesh, seed: 17));

            // 큰 불꽃과 같은 매끈한 불꽃 혀 4종. 휘는 방향·물결 수·기울기가 달라 파티클마다 다르게 보인다.
            Mesh[] flameMeshes =
            {
                WriteMesh("Flame_A", mesh => BuildLayerFlameMesh(seed: 101, phaseOffset: 0f,
                    waveAmplitude: 0.18f, waveCycles: 1f, lean: 0.1f).WriteSmooth(mesh)),
                WriteMesh("Flame_B", mesh => BuildLayerFlameMesh(seed: 211, phaseOffset: 0f,
                    waveAmplitude: 0.2f, waveCycles: 1.3f, lean: 0.06f).WriteSmooth(mesh)),
                WriteMesh("Flame_C", mesh => BuildLayerFlameMesh(seed: 307, phaseOffset: 0f,
                    waveAmplitude: 0.15f, waveCycles: 0.8f, lean: 0.12f).WriteSmooth(mesh)),
                WriteMesh("Flame_D", mesh => BuildLayerFlameMesh(seed: 409, phaseOffset: 0f,
                    waveAmplitude: 0.18f, waveCycles: 1.1f, lean: 0.08f).WriteSmooth(mesh)),
            };

            // 구면에 고르게 놓은 점의 볼록 껍질로 만든 불규칙한 다면체 연기 2종.
            Mesh[] smokeMeshes =
            {
                WriteMesh("Smoke_A", mesh => BuildPuffMesh(seed: 401, pointCount: 14).WriteFaceted(mesh, seed: 401)),
                WriteMesh("Smoke_B", mesh => BuildPuffMesh(seed: 503, pointCount: 20).WriteFaceted(mesh, seed: 503)),
            };

            // 구체를 감싸는 큰 불꽃의 세 층(바깥·중간·안쪽). 같은 방향으로 휘되 물결 위상·폭이 조금씩 다르다.
            Mesh[] bigFlameMeshes =
            {
                WriteMesh("BigFlame", mesh => BuildLayerFlameMesh(seed: 611, phaseOffset: 0f,
                    waveAmplitude: 0.22f, waveCycles: 1.1f, lean: 0.08f).WriteSmooth(mesh)),
                WriteMesh("BigFlame_Mid", mesh => BuildLayerFlameMesh(seed: 611, phaseOffset: 0.6f,
                    waveAmplitude: 0.2f, waveCycles: 1.2f, lean: 0.06f).WriteSmooth(mesh)),
                WriteMesh("BigFlame_Inner", mesh => BuildLayerFlameMesh(seed: 611, phaseOffset: 1.2f,
                    waveAmplitude: 0.15f, waveCycles: 1f, lean: 0.04f).WriteSmooth(mesh)),
            };

            // 큰 불꽃 꼭대기에서 떨어져 올라가는 작은 불 조각.
            Mesh[] lickMeshes =
            {
                WriteMesh("FlameLick", mesh => BuildLayerFlameMesh(seed: 649, phaseOffset: 0f,
                    waveAmplitude: 0.15f, waveCycles: 0.8f, lean: 0f).WriteSmooth(mesh)),
            };

            Mesh[] emberMeshes = { WriteMesh("Ember", mesh => BuildEmberMesh().WriteFaceted(mesh, seed: 701)) };

            Material orbMaterial = CreateOrUpdateMaterial("SkullEyeFire_Core", orbShader, OrbQueue, material =>
            {
                material.SetColor(CoreColorId, new Color(1f, 0.95f, 0.78f));
                material.SetColor(MidColorId, new Color(1f, 0.6f, 0.18f));
                material.SetColor(RimColorId, new Color(0.8f, 0.16f, 0.06f));
                material.SetFloat(CoreStartId, 0.5f);
                material.SetFloat(CoreSoftnessId, 0.4f);
                material.SetFloat(RimWidthId, 0.5f);
                material.SetFloat(IntensityId, 1f);
                material.SetFloat(PulseAmountId, 0.08f);
                material.SetFloat(PulseSpeedId, 3f);
            });
            // 작은 불꽃도 큰 불꽃 층과 같은 방식(거의 단색, 가장자리 투명, 겹치면 밝아짐). 물결 값은 파티클 로컬 단위다.
            Material flameMaterial = CreateOrUpdateMaterial("SkullEyeFire_Flame", flameShader, FlameQueue, material =>
                SetLayerFlame(material, body: new Color(1f, 0.48f, 0.08f), edge: new Color(0.85f, 0.16f, 0.04f),
                    tipCooling: 0.45f, additive: 0.6f, opacity: 0.85f, edgeSoftness: 0.5f,
                    waveAmplitude: 0.08f * BaseSize, waveLength: 1.4f * BaseSize, waveSpeed: 7f,
                    flickerAmount: 0.15f * BaseSize, flickerSpeed: 13f));
            Material smokeMaterial = CreateOrUpdateMaterial("SkullEyeFire_Smoke", smokeShader, SmokeQueue, material =>
            {
                material.SetColor(CoreColorId, new Color(0.4f, 0.38f, 0.37f));
                material.SetColor(MidColorId, new Color(0.26f, 0.25f, 0.25f));
                material.SetColor(OuterColorId, new Color(0.13f, 0.12f, 0.13f));
                material.SetFloat(IntensityId, 1f);
                material.SetFloat(FacingPowerId, 1f);
                material.SetFloat(TipCoolingId, 0f);
                material.SetFloat(FacetVariationId, 0.15f);
                material.SetFloat(AdditiveId, 0f);
                material.SetFloat(OpacityId, 0.85f);
                material.SetFloat(EdgeSoftnessId, 0.05f);
            });

            // 큰 불꽃 세 층. 층마다 거의 단색이고 가장자리가 투명해져, 겹치는 안쪽일수록 밝게 빛난다.
            // MeshRenderer라 물결·일렁임 값은 메시 단위(길이 1, 너비 1)다. 층마다 속도가 달라 따로 이글거린다.
            Material[] bigFlameMaterials =
            {
                CreateOrUpdateMaterial("SkullEyeFire_BigFlame", flameShader, BigFlameQueue, material =>
                    SetLayerFlame(material, body: new Color(0.95f, 0.26f, 0.05f), edge: new Color(0.72f, 0.08f, 0.03f),
                        tipCooling: 0.5f, additive: 0.55f, opacity: 0.75f, edgeSoftness: 0.45f,
                        waveAmplitude: 0.12f, waveLength: 0.55f, waveSpeed: 8f,
                        flickerAmount: 0.12f, flickerSpeed: 10f)),
                CreateOrUpdateMaterial("SkullEyeFire_BigFlame_Mid", flameShader, BigFlameQueue + 1, material =>
                    SetLayerFlame(material, body: new Color(1f, 0.47f, 0.07f), edge: new Color(0.92f, 0.24f, 0.05f),
                        tipCooling: 0.4f, additive: 0.6f, opacity: 0.8f, edgeSoftness: 0.4f,
                        waveAmplitude: 0.1f, waveLength: 0.5f, waveSpeed: 10f,
                        flickerAmount: 0.12f, flickerSpeed: 12f)),
                CreateOrUpdateMaterial("SkullEyeFire_BigFlame_Inner", flameShader, BigFlameQueue + 2, material =>
                    SetLayerFlame(material, body: new Color(1f, 0.78f, 0.3f), edge: new Color(1f, 0.52f, 0.1f),
                        tipCooling: 0.3f, additive: 0.65f, opacity: 0.85f, edgeSoftness: 0.4f,
                        waveAmplitude: 0.09f, waveLength: 0.45f, waveSpeed: 12f,
                        flickerAmount: 0.1f, flickerSpeed: 14f)),
            };

            // 떨어져 나가는 불 조각은 파티클이라 물결·일렁임 값이 파티클 시스템 로컬 단위다.
            Material lickMaterial = CreateOrUpdateMaterial("SkullEyeFire_FlameLick", flameShader, LickQueue, material =>
                SetLayerFlame(material, body: new Color(1f, 0.42f, 0.07f), edge: new Color(0.85f, 0.16f, 0.04f),
                    tipCooling: 0.4f, additive: 0.6f, opacity: 0.85f, edgeSoftness: 0.4f,
                    waveAmplitude: 0.05f * BaseSize, waveLength: 0.6f * BaseSize, waveSpeed: 8f,
                    flickerAmount: 0.08f * BaseSize, flickerSpeed: 14f));
            Material emberMaterial = CreateOrUpdateMaterial("SkullEyeFire_Ember", smokeShader, EmberQueue, material =>
            {
                material.SetColor(CoreColorId, new Color(1f, 0.95f, 0.6f));
                material.SetColor(MidColorId, new Color(1f, 0.7f, 0.2f));
                material.SetColor(OuterColorId, new Color(1f, 0.4f, 0.08f));
                material.SetFloat(IntensityId, 1.2f);
                material.SetFloat(FacingPowerId, 0.8f);
                material.SetFloat(TipCoolingId, 0f);
                material.SetFloat(FacetVariationId, 0.1f);
                material.SetFloat(AdditiveId, 0.5f);
                material.SetFloat(OpacityId, 1f);
                material.SetFloat(EdgeSoftnessId, 0f);
            });
            Material emberTrailMaterial = CreateOrUpdateMaterial("SkullEyeFire_EmberTrail", emberTrailShader, EmberQueue, material =>
            {
                material.SetColor(ColorId, new Color(1f, 0.7f, 0.25f));
                material.SetFloat(IntensityId, 1.3f);
                material.SetFloat(AdditiveId, 0.7f);
            });

            // 기존 프리팹이 있으면 루트를 그대로 열어 자식만 바꾼다. 씬 인스턴스의 연결과 루트 Transform 수정이 유지된다.
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("SkullEyeFire");
            GameObject prefab;
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }

                SetupRootController(root);
                BuildCore(root.transform, orbMesh, orbMaterial);
                BuildBigFlame(root.transform, bigFlameMeshes, bigFlameMaterials);
                BuildFlameLicks(root.transform, lickMeshes, lickMaterial);
                BuildFlames(root.transform, flameMeshes, emitterMesh, flameMaterial);
                BuildSmoke(root.transform, smokeMeshes, smokeMaterial);
                BuildEmbers(root.transform, emberMeshes, emberMaterial, emberTrailMaterial);

                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success)
                {
                    throw new InvalidOperationException($"프리팹 저장에 실패했습니다: {PrefabPath}");
                }
            }
            finally
            {
                if (exists)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>
        /// 루트에 그리지 않는 컨트롤러 파티클 시스템을 둔다(방출 없음, 렌더러 꺼짐).
        /// 에디터에서 루트 하나만 선택해도 자식 파티클이 모두 함께 재생되고, 런타임에서는 루트의
        /// Play()/Stop()으로 자식 파티클 전체를 한 번에 제어한다. 구체·큰 불꽃(MeshRenderer)은 GameObject 활성으로 켜고 끈다.
        /// </summary>
        private static void SetupRootController(GameObject root)
        {
            if (!root.TryGetComponent(out ParticleSystem controller))
            {
                controller = root.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = controller.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = true;
            main.maxParticles = 1;

            ParticleSystem.EmissionModule emission = controller.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = controller.shape;
            shape.enabled = false;

            controller.GetComponent<ParticleSystemRenderer>().enabled = false;
        }

        /// <summary>
        /// 눈 구체. 파티클이 아니라 MeshRenderer라 이펙트가 켜져 있는 동안 항상 보인다.
        /// 크기는 Transform Scale(= 지름), 밝기·붉은 가장자리·은은한 밝기 변화는 머티리얼에서 조정한다.
        /// </summary>
        private static void BuildCore(Transform parent, Mesh mesh, Material material)
        {
            GameObject core = CreateMeshObject("Core", parent, mesh, material);
            core.transform.localScale = Vector3.one * (OrbScale * BaseSize);
        }

        /// <summary>
        /// 눈 구체를 감싸며 위로 솟는 큰 불꽃. 바깥(빨강-주황)·중간(주황)·안쪽(노랑) 세 층이 서로를 감싸고,
        /// 아래쪽 둥근 구슬 안에 눈 구체가 들어가 흰 중심이 된다. 파티클이 아니라 MeshRenderer라 항상 있고,
        /// WavyFlame 셰이더가 층마다 다른 속도로 일렁이게 한다.
        /// 크기는 BigFlame 그룹의 Scale로 함께, 층별로는 각 자식의 Scale(X 너비, Y 길이, Z 두께)로 조정한다.
        /// </summary>
        private static void BuildBigFlame(Transform parent, Mesh[] meshes, Material[] materials)
        {
            var group = new GameObject("BigFlame");
            group.transform.SetParent(parent, false);

            // 층마다 구슬이 가장 넓은 높이(길이 × LayerBulbEnd)가 정확히 눈 중심(원점)에 오도록 바닥을 내린다.
            // 세 층 모두 눈과 같은 중심선 위에 있어 구체가 구슬 한가운데 들어간다.
            string[] names = { "Outer", "Mid", "Inner" };
            Vector3[] scales =
            {
                new Vector3(1.7f, 3f, 1.5f), new Vector3(1.38f, 2.35f, 1.2f), new Vector3(1.12f, 1.7f, 0.98f),
            };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject layer = CreateMeshObject(names[i], group.transform, meshes[i], materials[i]);
                layer.transform.localPosition = new Vector3(0f, -LayerBulbEnd * scales[i].y, 0f) * BaseSize;
                layer.transform.localScale = scales[i] * BaseSize;
            }
        }

        /// <summary>
        /// 큰 불꽃 꼭대기 근처에서 떨어져 나와 빠르게 솟으며 작아지는 불 조각. 참고 이미지의 꼭대기 조각처럼
        /// 불꽃 혀가 끊겨 올라가는 느낌을 준다. 로컬 공간이라 머리와 함께 움직인다.
        /// </summary>
        private static void BuildFlameLicks(Transform parent, Mesh[] meshes, Material material)
        {
            ParticleSystem licks = CreateMeshParticles(
                "FlameLicks", parent, material, meshes, 18, ParticleSystemSimulationSpace.Local);

            ParticleSystem.MainModule main = licks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSizeX = Sized(0.25f, 0.4f);
            main.startSizeY = Sized(0.5f, 0.8f);
            main.startSizeZ = Sized(0.25f, 0.4f);
            main.startRotationX = Degrees(-10f, 10f);
            main.startRotationY = Degrees(0f, 360f);
            main.startRotationZ = Degrees(-10f, 10f);

            ParticleSystem.EmissionModule emission = licks.emission;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = licks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f * BaseSize;
            shape.radiusThickness = 1f;
            shape.position = new Vector3(0f, 1.6f * BaseSize, -0.15f * BaseSize);
            shape.scale = new Vector3(1f, 0.5f, 1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = licks.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = Sized(-0.25f, 0.25f);
            velocity.y = Sized(2f, 3f);
            velocity.z = Sized(-0.25f, 0.25f);

            var shrink = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.2f));
            SetSizeCurves(licks, shrink, shrink);

            SetFade(licks, peakAlpha: 1f, fadeInEnd: 0.1f, holdUntil: 0.5f, endTint: new Color(1f, 0.6f, 0.4f));
        }

        /// <summary>
        /// 불꽃 층용 WavyFlame 설정. 몸통은 body 한 색이고, 가장자리·끝으로 갈수록 edge 색으로 식는다.
        /// 면 각도에 따른 명암을 약하게 둬 단단한 물체처럼 보이지 않게 한다.
        /// flickerAmount는 끝이 위아래로 이글거리는 폭(물결 값과 같은 단위)이다.
        /// </summary>
        private static void SetLayerFlame(
            Material material, Color body, Color edge, float tipCooling, float additive, float opacity,
            float edgeSoftness, float waveAmplitude, float waveLength, float waveSpeed,
            float flickerAmount, float flickerSpeed)
        {
            material.SetFloat(FlickerAmountId, flickerAmount);
            material.SetFloat(FlickerSpeedId, flickerSpeed);
            material.SetColor(CoreColorId, body);
            material.SetColor(MidColorId, body);
            material.SetColor(OuterColorId, edge);
            material.SetFloat(IntensityId, 1f);
            material.SetFloat(FacingPowerId, 0.6f);
            material.SetFloat(TipCoolingId, tipCooling);
            material.SetFloat(FacetVariationId, 0f);
            material.SetFloat(AdditiveId, additive);
            material.SetFloat(OpacityId, opacity);
            material.SetFloat(EdgeSoftnessId, edgeSoftness);
            material.SetFloat(WaveAmplitudeId, waveAmplitude);
            material.SetFloat(WaveLengthId, waveLength);
            material.SetFloat(WaveSpeedId, waveSpeed);
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return gameObject;
        }

        /// <summary>
        /// 구체 아래~옆구리 띠(정면 한가운데·정면 아래쪽은 비움)에서 피어올라 구체 둘레를 타고 길게 오르는 불꽃 혀.
        /// 구체 위쪽 한 점으로 살짝 끌려 모이며 올라가 눈을 감싸는 흐름을 만들고, 셰이더가 끝을 물결치게 흔든다.
        /// 로컬 공간이라 머리가 움직여도 불꽃이 눈구멍에서 떨어지지 않는다.
        /// </summary>
        private static void BuildFlames(Transform parent, Mesh[] meshes, Mesh emitter, Material material)
        {
            ParticleSystem flames = CreateMeshParticles(
                "Flames", parent, material, meshes, 32, ParticleSystemSimulationSpace.Local);

            ParticleSystem.MainModule main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = Sized(0.15f, 0.35f);
            main.startSizeX = Sized(0.3f, 0.42f);
            main.startSizeY = Sized(1f, 1.6f);
            main.startSizeZ = Sized(0.3f, 0.42f);
            main.startRotationX = Degrees(-8f, 8f);
            main.startRotationY = Degrees(0f, 360f);
            main.startRotationZ = Degrees(-8f, 8f);

            ParticleSystem.EmissionModule emission = flames.emission;
            emission.rateOverTime = 26f;

            // 정규화된 구면 점 메시(지름 1)의 꼭짓점에서 무작위로 나온다. Shape Scale = 구체 지름(OrbScale × BaseSize).
            ParticleSystem.ShapeModule shape = flames.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Vertex;
            shape.mesh = emitter;
            shape.scale = Vector3.one * (OrbScale * BaseSize);

            ParticleSystem.VelocityOverLifetimeModule velocity = flames.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = Sized(-0.05f, 0.05f);
            velocity.y = Sized(1f, 1.5f);
            velocity.z = Sized(-0.05f, 0.05f);

            // 구체 위쪽 한 점으로 끌어당겨 옆에서 오른 불꽃이 구체 위로 모이며 감싸게 한다.
            velocity.orbitalOffsetX = 0f;
            velocity.orbitalOffsetY = 1.4f * BaseSize;
            velocity.orbitalOffsetZ = 0f;
            velocity.radial = Sized(-0.5f, -0.3f);

            SetSizeCurves(flames,
                new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.35f)),
                new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.85f)));

            // 리본 단면이 천천히 돌며 보이는 폭이 바뀌게 한다. 기울기 흔들림은 셰이더 물결에 맡기고 작게 둔다.
            ParticleSystem.RotationOverLifetimeModule rotation = flames.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = Degrees(-6f, 6f);
            rotation.y = Degrees(-30f, 30f);
            rotation.z = Degrees(-6f, 6f);

            SetFade(flames, peakAlpha: 1f, fadeInEnd: 0.15f, holdUntil: 0.55f, endTint: new Color(1f, 0.72f, 0.5f));
        }

        /// <summary>
        /// 불꽃 위쪽에 자잘하게 떠올랐다 사라지는 작은 연기 조각. 무언가 타서 나는 뭉게 연기가 아니라
        /// 불꽃에 딸린 잔 조각이므로 크게 부풀지 않고 오히려 작아지며 사라진다.
        /// 로컬 공간이라 머리가 움직여도 불꽃과 함께 따라다니고 꼬리가 남지 않는다.
        /// </summary>
        private static void BuildSmoke(Transform parent, Mesh[] meshes, Material material)
        {
            ParticleSystem smoke = CreateMeshParticles(
                "Smoke", parent, material, meshes, 40, ParticleSystemSimulationSpace.Local);

            ParticleSystem.MainModule main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSizeX = Sized(0.11f, 0.19f);
            main.startSizeY = Sized(0.11f, 0.19f);
            main.startSizeZ = Sized(0.11f, 0.19f);
            main.startRotationX = Degrees(0f, 360f);
            main.startRotationY = Degrees(0f, 360f);
            main.startRotationZ = Degrees(0f, 360f);

            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 26f;

            // 불꽃 끝이 닿는 높이 주변에 흩어져 나온다.
            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f * BaseSize;
            shape.radiusThickness = 1f;
            shape.position = new Vector3(0f, 1.7f * BaseSize, 0f);
            shape.scale = new Vector3(1f, 0.6f, 1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = Sized(-0.15f, 0.15f);
            velocity.y = Sized(0.5f, 0.9f);
            velocity.z = Sized(-0.15f, 0.15f);

            var bit = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.5f));
            SetSizeCurves(smoke, bit, bit);

            ParticleSystem.RotationOverLifetimeModule rotation = smoke.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = Degrees(-120f, 120f);
            rotation.y = Degrees(-120f, 120f);
            rotation.z = Degrees(-120f, 120f);

            SetFade(smoke, peakAlpha: 0.95f, fadeInEnd: 0.2f, holdUntil: 0.45f, endTint: new Color(0.85f, 0.85f, 0.85f));
        }

        /// <summary>
        /// 불꽃 둘레에서 옆으로 튀어 날아가는 작은 불씨. 수명 후반으로 갈수록 바람(옆 방향 흐름)·난류(노이즈)·
        /// 눈 둘레를 도는 회전이 점점 세져, 날아가다 끝에서 휘고 돌며 사라진다. 짧은 꼬리가 휘는 궤적을 보여 준다.
        /// 월드 공간이라 머리가 움직여도 이미 튄 불씨는 제 궤적을 따라간다(수명이 짧아 긴 꼬리가 남지 않는다).
        /// </summary>
        private static void BuildEmbers(Transform parent, Mesh[] meshes, Material material, Material trailMaterial)
        {
            ParticleSystem embers = CreateMeshParticles(
                "Embers", parent, material, meshes, 30, ParticleSystemSimulationSpace.World);
            ParticleSystemRenderer renderer = embers.GetComponent<ParticleSystemRenderer>();
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.trailMaterial = trailMaterial;

            ParticleSystem.MainModule main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = Sized(1.2f, 2.2f);
            main.startSizeX = Sized(0.05f, 0.08f);
            main.startSizeY = Sized(0.05f, 0.08f);
            main.startSizeZ = Sized(0.05f, 0.08f);
            main.startRotationX = Degrees(0f, 360f);
            main.startRotationY = Degrees(0f, 360f);
            main.startRotationZ = Degrees(0f, 360f);

            // 꾸준히 튀는 불씨에 더해, 0.35초마다 절반 확률로 2~4개가 한꺼번에 톡 터져 불규칙하게 활발해 보인다.
            ParticleSystem.EmissionModule emission = embers.emission;
            emission.rateOverTime = 8f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 2, 4, 0, 0.35f) { probability = 0.5f },
            });

            // 불꽃 몸통 둘레에서 사방으로 튀어 나간다.
            ParticleSystem.ShapeModule shape = embers.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f * BaseSize;
            shape.radiusThickness = 1f;
            shape.position = new Vector3(0f, 0.7f * BaseSize, -0.1f * BaseSize);

            // 위로 떠오르며, 후반에는 불씨마다 다른 옆 방향 바람과 눈 둘레 회전이 세진다.
            ParticleSystem.VelocityOverLifetimeModule velocity = embers.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(BaseSize, LateRamp(-1f), LateRamp(1f));
            velocity.y = new ParticleSystem.MinMaxCurve(BaseSize, Flat(0.5f), Flat(0.9f));
            velocity.z = new ParticleSystem.MinMaxCurve(BaseSize, LateRamp(-0.8f), LateRamp(0.8f));
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(1f, Flat(0f), Flat(0f));
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1f, LateRamp(-4f), LateRamp(4f));
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(1f, Flat(0f), Flat(0f));

            // 후반에 세지는 난류가 궤적을 말아 올린다.
            ParticleSystem.NoiseModule noise = embers.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.8f * BaseSize, LateRamp(1f));
            noise.frequency = 1.5f;
            noise.scrollSpeed = 0.6f;
            noise.damping = true;
            noise.octaveCount = 1;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var shrink = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0.2f));
            SetSizeCurves(embers, shrink, shrink);

            ParticleSystem.RotationOverLifetimeModule rotation = embers.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = Degrees(-360f, 360f);
            rotation.y = Degrees(-360f, 360f);
            rotation.z = Degrees(-360f, 360f);

            SetFade(embers, peakAlpha: 1f, fadeInEnd: 0.05f, holdUntil: 0.7f, endTint: new Color(1f, 0.45f, 0.15f));

            // 휘는 궤적을 보여 주는 짧은 꼬리. 머리 쪽이 굵고 끝으로 갈수록 가늘고 옅어진다.
            ParticleSystem.TrailModule trails = embers.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.3f);
            trails.minVertexDistance = 0.03f * BaseSize;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f)));
            var trailFade = new Gradient();
            trailFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(trailFade);
        }

        /// <summary>수명 전반(45%)까지 0이었다가 끝에서 end에 이르는 곡선. 후반에만 세지는 효과에 쓴다.</summary>
        private static AnimationCurve LateRamp(float end)
        {
            return new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.45f, 0f), new Keyframe(1f, end));
        }

        private static AnimationCurve Flat(float value)
        {
            return AnimationCurve.Constant(0f, 1f, value);
        }

        /// <summary>
        /// 계속 타오르는 메시 파티클의 공통 설정. 미리 데워(prewarm) 켜지자마자 다 타오른 상태로 보이고,
        /// 시드는 인스턴스마다 자동으로 달라져 두 눈의 움직임이 똑같이 반복되지 않는다.
        /// </summary>
        private static ParticleSystem CreateMeshParticles(
            string name, Transform parent, Material material, Mesh[] meshes, int maxParticles,
            ParticleSystemSimulationSpace space)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);

            ParticleSystem system = gameObject.AddComponent<ParticleSystem>();
            system.useAutoRandomSeed = true;

            ParticleSystem.MainModule main = system.main;
            main.duration = 2f;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startSpeed = 0f;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.maxParticles = maxParticles;
            main.simulationSpace = space;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSize3D = true;
            main.startRotation3D = true;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
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
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            return system;
        }

        /// <summary>기준 길이(BaseSize)에 대한 비율 범위를 실제 길이 범위로 바꾼다.</summary>
        private static ParticleSystem.MinMaxCurve Sized(float min, float max)
        {
            return new ParticleSystem.MinMaxCurve(min * BaseSize, max * BaseSize);
        }

        private static ParticleSystem.MinMaxCurve Degrees(float min, float max)
        {
            return new ParticleSystem.MinMaxCurve(min * Mathf.Deg2Rad, max * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 크기 변화. 메시가 +Y로 길쭉하므로 X·Z = 굵기, Y = 길이다.
        /// </summary>
        private static void SetSizeCurves(ParticleSystem system, AnimationCurve width, AnimationCurve length)
        {
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = true;
            sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, width);
            sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, length);
            sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, width);
        }

        /// <summary>
        /// 알파를 fadeInEnd까지 0 → peakAlpha로 올리고 holdUntil까지 유지한 뒤 0으로 내린다. 색은 끝으로 갈수록 endTint로 식는다.
        /// </summary>
        private static void SetFade(ParticleSystem system, float peakAlpha, float fadeInEnd, float holdUntil, Color endTint)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(endTint, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, fadeInEnd),
                    new GradientAlphaKey(peakAlpha, holdUntil),
                    new GradientAlphaKey(0f, 1f),
                });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        /// <summary>
        /// 지름 1, 중심이 원점인 매끈한 UV 구. 정면 실루엣이 원으로 보이도록 둘레를 24분할한다.
        /// </summary>
        private static MeshData BuildOrbMesh()
        {
            const int segments = 24;
            const int stacks = 14;

            var data = new MeshData();
            int top = data.AddVertex(new Vector3(0f, 0.5f, 0f));
            var rowStart = new int[stacks - 1];
            for (int s = 1; s < stacks; s++)
            {
                float polar = Mathf.PI * s / stacks;
                float y = Mathf.Cos(polar) * 0.5f;
                float radius = Mathf.Sin(polar) * 0.5f;
                rowStart[s - 1] = data.VertexCount;
                for (int i = 0; i < segments; i++)
                {
                    float angle = Mathf.PI * 2f * i / segments;
                    data.AddVertex(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                }
            }

            int bottom = data.AddVertex(new Vector3(0f, -0.5f, 0f));

            Func<Vector3, Vector3> center = _ => Vector3.zero;
            AddCapsAndStrips(data, top, bottom, rowStart, segments, center);
            return data;
        }

        /// <summary>
        /// 큰 불꽃 층·작은 불꽃·불 조각에 쓰는 매끈한 불꽃 메시. 길이 1(+Y), 최대 너비 1이며 바닥이 원점이다.
        /// 아래 24%는 둥근 구슬처럼 부풀고(눈 구체가 들어가는 자리), 그 위는 물결치며 가늘어지는 혀가 된다.
        /// 물결 방향은 seed로 정해 같은 seed의 층끼리 같은 쪽으로 휘고, phaseOffset으로 층마다 굽이를 조금씩 어긋나게 한다.
        /// 단면은 낮은 주파수로만 살짝 굴곡지게 해 유기적으로 보이되, 꼭짓점을 공유해 부드럽게 음영진다.
        /// </summary>
        private static MeshData BuildLayerFlameMesh(
            int seed, float phaseOffset, float waveAmplitude, float waveCycles, float lean)
        {
            const int sides = 16;
            const int rings = 18;
            const float bulbEnd = LayerBulbEnd;
            const float taperPower = 1.15f;

            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            float waveAngle = Range(0f, Mathf.PI * 2f);
            var waveDirection = new Vector3(Mathf.Cos(waveAngle), 0f, Mathf.Sin(waveAngle));
            float wavePhase = Range(0f, Mathf.PI * 2f) + phaseOffset;
            float lobePhase = Range(0f, Mathf.PI * 2f) + phaseOffset;

            // 구슬 부분은 원의 4분의 1, 그 위는 (남은 길이)^taperPower로 가늘어진다. 최대 반지름 0.5.
            float Profile(float t)
            {
                if (t < bulbEnd)
                {
                    float u = (bulbEnd - t) / bulbEnd;
                    return 0.5f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
                }

                return 0.5f * Mathf.Pow((1f - t) / (1f - bulbEnd), taperPower);
            }

            // 구슬 부분은 곧게 두고, 혀 부분만 위로 갈수록 크게 물결치며 한쪽으로 기운다.
            Vector3 Spine(float t)
            {
                float rise = Mathf.Max(0f, t - bulbEnd) / (1f - bulbEnd);
                float sway = Mathf.Sin(Mathf.PI * 2f * waveCycles * rise + wavePhase) - Mathf.Sin(wavePhase);
                return waveDirection * (waveAmplitude * sway * rise + lean * rise * rise) + new Vector3(0f, t, 0f);
            }

            var data = new MeshData();
            int basePole = data.AddVertex(Spine(0f), 0f);
            float step = Mathf.PI * 2f / sides;
            var rowStart = new int[rings - 1];
            for (int r = 1; r < rings; r++)
            {
                // 양 끝에 고리를 촘촘히 둬 둥근 바닥과 가는 끝을 살린다.
                float t = (1f - Mathf.Cos(Mathf.PI * r / rings)) * 0.5f;
                Vector3 center = Spine(t);
                float radius = Profile(t);
                rowStart[r - 1] = data.VertexCount;
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * step;
                    float shape = 1f + 0.06f * Mathf.Cos(3f * angle + lobePhase + 2f * t);
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    data.AddVertex(center + direction * (radius * shape), t);
                }
            }

            int tipPole = data.AddVertex(Spine(1f), 1f);

            AddCapsAndStrips(data, basePole, tipPole, rowStart, sides, c => Spine(Mathf.Clamp01(c.y)));
            return data;
        }

        /// <summary>
        /// 위아래로 조금 긴 팔면체 불씨. 아주 작게 보이므로 면 여덟 개로 충분하다. 중심이 원점이다.
        /// </summary>
        private static MeshData BuildEmberMesh()
        {
            Vector3[] points =
            {
                new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 0f, 0f),
                new Vector3(0f, 0.65f, 0f), new Vector3(0f, -0.65f, 0f),
                new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, -0.5f),
            };
            int[] faces = { 0, 2, 4, 4, 2, 1, 1, 2, 5, 5, 2, 0, 0, 4, 3, 4, 1, 3, 1, 5, 3, 5, 0, 3 };

            var data = new MeshData();
            foreach (Vector3 point in points)
            {
                data.AddVertex(point);
            }

            Func<Vector3, Vector3> center = _ => Vector3.zero;
            for (int i = 0; i < faces.Length; i += 3)
            {
                data.AddTriangle(faces[i], faces[i + 1], faces[i + 2], center);
            }

            return data;
        }

        /// <summary>
        /// 구면에 피보나치 배치로 고르게 놓은 점을 조금씩 흔든 뒤 볼록 껍질을 만든다.
        /// 면이 비슷한 크기로 고르게 나뉘어 둥글지만 면이 보이는 불규칙한 다면체가 된다. 위아래로 살짝 눌렀다.
        /// </summary>
        private static MeshData BuildPuffMesh(int seed, int pointCount)
        {
            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            float phase = Range(0f, Mathf.PI * 2f);
            var points = new List<Vector3>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / pointCount;
                float ring = Mathf.Sqrt(1f - y * y);
                float angle = phase + i * goldenAngle + Range(-0.15f, 0.15f);
                var point = new Vector3(Mathf.Cos(angle) * ring, y * 0.85f, Mathf.Sin(angle) * ring);
                points.Add(point * (0.5f * Range(0.9f, 1.06f)));
            }

            var data = new MeshData();
            foreach (Vector3 point in points)
            {
                data.AddVertex(point);
            }

            AddConvexHull(data, points);
            return data;
        }

        /// <summary>
        /// 불꽃이 나오는 자리. 구면에 피보나치 배치로 고르게 놓은 점들(지름 1 기준, 표면 바로 안쪽) 중
        /// 아래~옆구리 띠(높이 -0.7~0.45)만 써서, 긴 불꽃이 구체 둘레를 타고 올라 위를 감싸게 한다. 다음 영역은 비운다.
        /// 정면 한가운데(+Z에서 35° 안): 눈 중심을 덮지 않게. 정면 아래쪽: 올라오며 정면을 가로지르지 않게.
        /// 바닥: 위로 오르면 구체 안에 묻혀 보이지 않으므로. 위쪽: 옆에서 오른 불꽃이 채우므로.
        /// 법선은 바깥(반지름 방향)을 향해 불꽃이 표면에서 살짝 밀려나며 오른다. 파티클 Shape Scale로 구체 지름에 맞춘다.
        /// </summary>
        private static void WriteEmitterShell(Mesh mesh, int seed)
        {
            const int candidates = 72;
            float frontCap = Mathf.Cos(35f * Mathf.Deg2Rad);

            var random = new System.Random(seed);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            for (int i = 0; i < candidates; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / candidates;
                float ring = Mathf.Sqrt(1f - y * y);
                float angle = i * goldenAngle + Range(-0.2f, 0.2f);
                var direction = new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring);

                bool frontCenter = direction.z > frontCap;
                bool frontBelow = direction.z > 0.25f && direction.y < -0.1f;
                bool outsideBand = direction.y < -0.7f || direction.y > 0.45f;
                if (frontCenter || frontBelow || outsideBand)
                {
                    continue;
                }

                vertices.Add(direction * (0.5f * Range(0.88f, 0.98f)));
                normals.Add(direction);
            }

            // 꼭짓점 방출에는 위치만 쓰지만, 메시가 유효하도록 이웃 점끼리 삼각형을 둔다.
            var triangles = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                triangles.Add(i);
                triangles.Add((i + 1) % vertices.Count);
                triangles.Add((i + 2) % vertices.Count);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// 첫 극점 → 고리들(rowStart 순서) → 마지막 극점 순서로 부채꼴과 띠 삼각형을 잇는다.
        /// </summary>
        private static void AddCapsAndStrips(
            MeshData data, int firstPole, int lastPole, int[] rowStart, int sides, Func<Vector3, Vector3> interiorOf)
        {
            int firstRow = rowStart[0];
            int lastRow = rowStart[rowStart.Length - 1];
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                data.AddTriangle(firstPole, firstRow + i, firstRow + next, interiorOf);
                data.AddTriangle(lastRow + i, lastRow + next, lastPole, interiorOf);
            }

            for (int r = 0; r < rowStart.Length - 1; r++)
            {
                int current = rowStart[r];
                int below = rowStart[r + 1];
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    data.AddTriangle(current + i, current + next, below + next, interiorOf);
                    data.AddTriangle(current + i, below + next, below + i, interiorOf);
                }
            }
        }

        /// <summary>
        /// 점들의 볼록 껍질을 삼각형으로 더한다. 나머지 점이 모두 한쪽에 있는 세 점이 껍질 면이다.
        /// 점이 수십 개뿐이라 모든 조합을 검사한다. 점은 흔들어 둬야 한 면에 네 점 이상이 겹치지 않는다.
        /// data의 꼭짓점 번호는 points의 순서와 같아야 한다.
        /// </summary>
        private static void AddConvexHull(MeshData data, IReadOnlyList<Vector3> points)
        {
            const float tolerance = 1e-6f;
            Func<Vector3, Vector3> center = _ => Vector3.zero;
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
                            data.AddTriangle(i, j, k, center);
                        }
                    }
                }
            }
        }

        private static Mesh WriteMesh(string name, Action<Mesh> fill)
        {
            string path = $"{MeshFolder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh();
            }

            fill(mesh);
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
        /// 같은 경로의 머티리얼이 있으면 GUID를 유지한 채 이전 속성을 모두 지우고 새로 채운다.
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

        /// <summary>
        /// 메시 꼭짓점·삼각형을 모은 뒤 매끈한 메시(꼭짓점 공유) 또는 평면 셰이딩 메시(면마다 꼭짓점 분리)로 쓴다.
        /// UV.x = 길이 방향 위치(LowPolyFlash의 끝 식음), UV.y = 매끈하면 0.5, 평면이면 면마다 다른 난수.
        /// </summary>
        private sealed class MeshData
        {
            private readonly List<Vector3> _positions = new List<Vector3>();
            private readonly List<float> _lengths = new List<float>();
            private readonly List<int> _triangles = new List<int>();

            public int VertexCount => _positions.Count;

            public int AddVertex(Vector3 position, float length = 0f)
            {
                _positions.Add(position);
                _lengths.Add(length);
                return _positions.Count - 1;
            }

            /// <summary>
            /// 삼각형을 더한다. 법선이 interiorOf(무게중심)에서 바깥을 향하도록 감는 방향을 맞춘다.
            /// </summary>
            public void AddTriangle(int a, int b, int c, Func<Vector3, Vector3> interiorOf)
            {
                Vector3 pa = _positions[a];
                Vector3 pb = _positions[b];
                Vector3 pc = _positions[c];
                Vector3 normal = Vector3.Cross(pb - pa, pc - pa);
                if (normal.sqrMagnitude < 1e-12f)
                {
                    return;
                }

                Vector3 centroid = (pa + pb + pc) / 3f;
                if (Vector3.Dot(normal, centroid - interiorOf(centroid)) < 0f)
                {
                    (b, c) = (c, b);
                }

                _triangles.Add(a);
                _triangles.Add(b);
                _triangles.Add(c);
            }

            /// <summary>
            /// 정점색은 흰색으로 채운다(MeshRenderer로 그릴 때 WavyFlame의 투명도 계산이 그대로 맞도록).
            /// </summary>
            public void WriteSmooth(Mesh mesh)
            {
                var uvs = new List<Vector2>(_lengths.Count);
                var colors = new List<Color32>(_lengths.Count);
                foreach (float length in _lengths)
                {
                    uvs.Add(new Vector2(length, 0.5f));
                    colors.Add(new Color32(255, 255, 255, 255));
                }

                mesh.Clear();
                mesh.SetVertices(_positions);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }

            /// <summary>
            /// 정점색은 흰색으로 채운다. 파티클에는 영향이 없고, MeshRenderer로 그릴 때 셰이더의 색·투명도 계산이 그대로 맞는다.
            /// </summary>
            public void WriteFaceted(Mesh mesh, int seed)
            {
                var random = new System.Random(seed);
                var vertices = new List<Vector3>(_triangles.Count);
                var normals = new List<Vector3>(_triangles.Count);
                var uvs = new List<Vector2>(_triangles.Count);
                var colors = new List<Color32>(_triangles.Count);
                var indices = new List<int>(_triangles.Count);
                for (int i = 0; i < _triangles.Count; i += 3)
                {
                    int a = _triangles[i];
                    int b = _triangles[i + 1];
                    int c = _triangles[i + 2];
                    Vector3 normal = Vector3.Cross(_positions[b] - _positions[a], _positions[c] - _positions[a]).normalized;
                    float facet = (float)random.NextDouble();
                    foreach (int index in new[] { a, b, c })
                    {
                        indices.Add(vertices.Count);
                        vertices.Add(_positions[index]);
                        normals.Add(normal);
                        uvs.Add(new Vector2(_lengths[index], facet));
                        colors.Add(new Color32(255, 255, 255, 255));
                    }
                }

                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateBounds();
            }
        }
    }
}
