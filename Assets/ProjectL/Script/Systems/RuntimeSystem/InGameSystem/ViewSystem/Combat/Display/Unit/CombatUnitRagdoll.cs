using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>전투 캐릭터의 본과 사망 물리 바디를 연결하고, 해제할 때 빌린 표시 상태를 복원합니다.</summary>
    /// <remarks>프리팹에서 꺼 둡니다. Begin으로 시작한 동안에만 LateUpdate가 본을 따라가게 합니다.</remarks>
    [DisallowMultipleComponent]
    public sealed class CombatUnitRagdoll : MonoBehaviour
    {
        private enum BoneLayout
        {
            Rigify = 0,
            Legacy = 1,
        }

        [SerializeField] private BoneLayout boneLayout;

        private static readonly (string Bone, string End, string Parent, float Radius, float Mass, float Swing)[] Parts =
        {
            ("spine", "spine.003", null, 0.42f, 10f, 0f),
            ("spine.003", "spine.006", "spine", 0.52f, 8f, 20f),
            ("spine.006", "spine.006_end", "spine.003", 0.54f, 3f, 25f),
            ("upper_arm.L", "forearm.L", "spine.003", 0.35f, 2f, 55f),
            ("forearm.L", "hand.L", "upper_arm.L", 0.38f, 1.5f, 65f),
            ("upper_arm.R", "forearm.R", "spine.003", 0.35f, 2f, 55f),
            ("forearm.R", "hand.R", "upper_arm.R", 0.38f, 1.5f, 65f),
            ("thigh.L", "shin.L", "spine", 0.39f, 4f, 45f),
            ("shin.L", "foot.L", "thigh.L", 0.55f, 3f, 60f),
            ("foot.L", "toe.L", "shin.L", 0.34f, 1f, 25f),
            ("thigh.R", "shin.R", "spine", 0.39f, 4f, 45f),
            ("shin.R", "foot.R", "thigh.R", 0.55f, 3f, 60f),
            ("foot.R", "toe.R", "shin.R", 0.34f, 1f, 25f),
        };

        private static readonly string[] ArmoredParts =
        {
            "upper_arm.L", "forearm.L", "upper_arm.R", "forearm.R",
            "thigh.L", "shin.L", "thigh.R", "shin.R",
        };

        // 스나이퍼와 헤비가 공유하는 기존 본 이름입니다.
        private static readonly Dictionary<string, string> LegacyBoneNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "spine", "뼈대" },
            { "spine.003", "뼈대.001" },
            { "spine.006", "뼈대.002" },
            { "spine.006_end", "뼈대.009" },
            { "upper_arm.L", "뼈대.011.L" },
            { "forearm.L", "뼈대.012.L" },
            { "hand.L", "뼈대.013.L" },
            { "thigh.L", "뼈대.004.L" },
            { "shin.L", "뼈대.005.L" },
            { "foot.L", "뼈대.006.L" },
            { "toe.L", "뼈대.007.L" },
            { "upper_arm.R", "뼈대.011.R" },
            { "forearm.R", "뼈대.012.R" },
            { "hand.R", "뼈대.013.R" },
            { "thigh.R", "뼈대.004.R" },
            { "shin.R", "뼈대.005.R" },
            { "foot.R", "뼈대.006.R" },
            { "toe.R", "뼈대.007.R" },
        };

        private readonly List<BoneFollower> _followers = new List<BoneFollower>();
        private readonly List<Animator> _pausedAnimators = new List<Animator>();
        private readonly List<Collider> _pausedColliders = new List<Collider>();
        private readonly List<SkinnedMeshRenderer> _updatedRenderers = new List<SkinnedMeshRenderer>();
        private GameObject _physicsRoot;

        internal void Begin(Vector3 push, Vector3 groundUp, float groundWidth)
        {
            if (!gameObject.activeInHierarchy || _physicsRoot != null)
                throw new InvalidOperationException($"[{name} 랙돌] 활성 표시에서 랙돌을 한 번만 시작할 수 있습니다.");
            var namedBones = FindNamedBones();
            var bones = ResolveParts(namedBones);
            _physicsRoot = new GameObject("Ragdoll Physics");
            try
            {
                _physicsRoot.transform.SetParent(transform, true);
                BuildPhysics(bones, namedBones, push, groundUp, groundWidth);
                enabled = true;
            }
            catch
            {
                End();
                throw;
            }
        }

        private Dictionary<string, Transform> FindNamedBones()
        {
            var namedBones = new Dictionary<string, Transform>(StringComparer.Ordinal);
            string prefix = boneLayout == BoneLayout.Legacy ? "뼈대" : "DEF-";
            foreach (Transform bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name.StartsWith(prefix, StringComparison.Ordinal) ||
                    boneLayout == BoneLayout.Rigify && (bone.name == "shoulder.L" || bone.name == "shoulder.R"))
                    namedBones.Add(bone.name, bone);
            }
            return namedBones;
        }

        private Dictionary<string, Transform> ResolveParts(Dictionary<string, Transform> namedBones)
        {
            var bones = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var part in Parts)
            {
                Transform bone = GetBone(namedBones, part.Bone);
                Transform end = GetBone(namedBones, part.End);
                bones[part.Bone] = bone;
                bones[part.End] = end;
                if ((end.position - bone.position).sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException($"[{name} 랙돌] 본 길이가 너무 짧습니다. {bone.name}");
            }
            return bones;
        }

        private void BuildPhysics(Dictionary<string, Transform> bones, Dictionary<string, Transform> namedBones,
            Vector3 push, Vector3 groundUp, float groundWidth)
        {
            // 격자의 물리 표면이 없는 경우에도 사망한 칸 높이에서 멈춥니다. 미리보기는 씬 바닥을 사용합니다.
            bool usesFieldGround = groundWidth > 0f;
            int physicsLayer = usesFieldGround ? LayerMask.NameToLayer("Ignore Raycast") : 0;
            var existingColliders = GetComponentsInChildren<Collider>();
            var bodies = new Dictionary<string, Rigidbody>(StringComparer.Ordinal);
            var colliders = new List<CapsuleCollider>(Parts.Length);
            foreach (var part in Parts)
            {
                Transform bone = bones[part.Bone];
                Vector3 span = bones[part.End].position - bone.position;
                var segment = new GameObject($"Ragdoll {part.Bone}") { layer = physicsLayer };
                segment.transform.SetPositionAndRotation(bone.position, Quaternion.FromToRotation(Vector3.up, span));
                segment.transform.SetParent(_physicsRoot.transform, true);

                var body = segment.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.mass = part.Mass;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                bodies.Add(part.Bone, body);

                float length = span.magnitude;
                var collider = segment.AddComponent<CapsuleCollider>();
                collider.radius = length * part.Radius;
                collider.height = Mathf.Max(length + collider.radius, 2f * collider.radius);
                collider.center = Vector3.up * (length * 0.5f);
                if (usesFieldGround) collider.excludeLayers = Physics.DefaultRaycastLayers;
                colliders.Add(collider);

                if (part.Parent != null)
                    Connect(body, bodies[part.Parent], part.Swing);
                _followers.Add(new BoneFollower(segment.transform, bone));
            }

            if (boneLayout == BoneLayout.Rigify)
                BindRigifyAttachments(namedBones, bodies);

            if (usesFieldGround)
                CreateFieldGround(push, groundUp, groundWidth, physicsLayer);
            SuspendModel(existingColliders);

            // 초기 형태에서 겹치는 캡슐이 서로 밀어내지 않게 지면 충돌만 먼저 확인합니다.
            for (int i = 0; i < colliders.Count; i++)
                for (int j = 0; j < i; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j]);

            foreach (Rigidbody body in bodies.Values)
                body.isKinematic = false;

            if (push != Vector3.zero)
                Launch(bodies, push, groundUp, usesFieldGround ? groundWidth : 1f);
        }

        private void Launch(Dictionary<string, Rigidbody> bodies, Vector3 push, Vector3 groundUp, float scale)
        {
            Vector3 launchVelocity = push * (2f * scale) + groundUp * 2.4f;
            Vector3 tumble = Vector3.Cross(groundUp, push) * 3.5f + push * 1.1f;
            Vector3 pivot = bodies["spine"].worldCenterOfMass;
            foreach (Rigidbody body in bodies.Values)
            {
                // 골반에서 떨어진 부위에는 회전에 따른 선속도도 줍니다.
                Vector3 spinVelocity = Vector3.Cross(tumble, body.worldCenterOfMass - pivot);
                body.AddForce(launchVelocity + spinVelocity, ForceMode.VelocityChange);
                body.AddTorque(tumble, ForceMode.VelocityChange);
            }

            // 팔꿈치와 무릎은 몸의 회전과 별도로 접히고, 좌우의 세기를 달리합니다.
            Vector3 facing = transform.forward;
            Bend(bodies["forearm.L"], facing, 5.5f);
            Bend(bodies["forearm.R"], facing, 3f);
            Bend(bodies["shin.L"], -facing, 3.5f);
            Bend(bodies["shin.R"], -facing, 5.5f);
        }

        private static void Bend(Rigidbody limb, Vector3 direction, float speed)
        {
            Vector3 axis = limb.transform.right; // CharacterJoint의 swingAxis
            Vector3 turn = Vector3.Cross(limb.transform.up, direction);
            float sign = Vector3.Dot(turn, axis) < 0f ? -1f : 1f;
            limb.AddTorque(axis * (speed * sign), ForceMode.VelocityChange);
        }

        private void CreateFieldGround(Vector3 push, Vector3 up, float width, int layer)
        {
            var floor = new GameObject("Ragdoll Ground") { layer = layer };
            float depth = width * 0.1f;
            bool isLaunched = push != Vector3.zero;
            // 날아가는 쪽에 한 칸을 더 받쳐 착지 직전에 임시 바닥 밖으로 빠지지 않게 합니다.
            Vector3 center = transform.position + push * (width * 0.5f) - up * (depth * 0.5f);
            floor.transform.SetPositionAndRotation(center,
                isLaunched ? Quaternion.LookRotation(push, up) : Quaternion.FromToRotation(Vector3.up, up));
            floor.transform.SetParent(_physicsRoot.transform, true);
            var collider = floor.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, depth, isLaunched ? width * 2f : width);
            collider.excludeLayers = Physics.DefaultRaycastLayers;
        }

        private void SuspendModel(Collider[] existingColliders)
        {
            foreach (var animator in GetComponentsInChildren<Animator>())
            {
                if (!animator.enabled) continue;
                _pausedAnimators.Add(animator);
                animator.enabled = false;
            }
            foreach (var collider in existingColliders)
            {
                if (!collider.enabled) continue;
                _pausedColliders.Add(collider);
                collider.enabled = false;
            }
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (renderer.updateWhenOffscreen) continue;
                _updatedRenderers.Add(renderer);
                renderer.updateWhenOffscreen = true;
            }
        }

        internal void End()
        {
            var physicsRoot = _physicsRoot;
            if (physicsRoot == null) return;
            _physicsRoot = null;
            enabled = false;
            // Destroy는 프레임 끝이므로, 같은 프레임에 풀로 돌아가도 이전 바디를 즉시 멈춥니다.
            physicsRoot.SetActive(false);
            Destroy(physicsRoot);
            foreach (BoneFollower follower in _followers)
                follower.Restore();
            _followers.Clear();
            foreach (var renderer in _updatedRenderers)
                if (renderer != null) renderer.updateWhenOffscreen = false;
            _updatedRenderers.Clear();
            foreach (var collider in _pausedColliders)
                if (collider != null) collider.enabled = true;
            _pausedColliders.Clear();
            foreach (var animator in _pausedAnimators)
                if (animator != null) animator.enabled = true;
            _pausedAnimators.Clear();
        }

        private void OnDisable() => End();

        private void LateUpdate()
        {
            foreach (BoneFollower follower in _followers)
                follower.Apply();
        }

        private Transform GetBone(Dictionary<string, Transform> bones, string role)
        {
            string boneName = boneLayout == BoneLayout.Legacy ? LegacyBoneNames[role] : "DEF-" + role;
            if (bones.TryGetValue(boneName, out var bone))
                return bone;
            throw new InvalidOperationException($"[{name} 랙돌] {boneName} 본을 찾을 수 없습니다.");
        }

        private void BindRigifyAttachments(Dictionary<string, Transform> bones, Dictionary<string, Rigidbody> bodies)
        {
            // 별도 계층에 있는 골반·어깨 스킨과 적의 팔다리 장갑은 시작 오프셋을 유지합니다.
            foreach (string side in new[] { ".L", ".R" })
            {
                Bind("DEF-pelvis" + side, "spine");
                Bind("DEF-shoulder" + side, "spine.003");
                Bind("shoulder" + side, "spine.003");
            }
            foreach (string part in ArmoredParts)
                Bind("DEF-rigid_" + part, part);

            void Bind(string boneName, string bodyName)
            {
                if (bones.TryGetValue(boneName, out var bone))
                    _followers.Add(new BoneFollower(bodies[bodyName].transform, bone));
            }
        }

        private static void Connect(Rigidbody body, Rigidbody parent, float swing)
        {
            var joint = body.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parent;
            joint.axis = Vector3.up;
            joint.swingAxis = Vector3.right;
            joint.lowTwistLimit = new SoftJointLimit { limit = -20f };
            joint.highTwistLimit = new SoftJointLimit { limit = 20f };
            joint.swing1Limit = new SoftJointLimit { limit = swing };
            joint.swing2Limit = new SoftJointLimit { limit = Mathf.Min(swing, 30f) };
            joint.enableProjection = true;
        }

        private readonly struct BoneFollower
        {
            private readonly Transform _body;
            private readonly Transform _bone;
            private readonly Vector3 _offset;
            private readonly Quaternion _rotationOffset;
            private readonly Vector3 _localPosition;
            private readonly Quaternion _localRotation;

            internal BoneFollower(Transform body, Transform bone)
            {
                _body = body;
                _bone = bone;
                _offset = body.InverseTransformPoint(bone.position);
                _rotationOffset = Quaternion.Inverse(body.rotation) * bone.rotation;
                _localPosition = bone.localPosition;
                _localRotation = bone.localRotation;
            }

            internal void Apply()
            {
                _bone.SetPositionAndRotation(_body.TransformPoint(_offset), _body.rotation * _rotationOffset);
            }

            internal void Restore()
            {
                if (_bone == null) return;
                _bone.SetLocalPositionAndRotation(_localPosition, _localRotation);
            }
        }
    }
}
