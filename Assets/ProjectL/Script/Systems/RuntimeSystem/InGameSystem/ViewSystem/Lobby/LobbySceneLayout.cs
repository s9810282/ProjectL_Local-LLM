using System;
using ProjectL.Systems.DataBaseSystem.Data.View;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>씬에 저작한 위치를 바꾸지 않고 로비 worldRoot의 참조와 자세(축척 1·Y축 회전)를 검증합니다. <see cref="LobbySceneRoot.Validate"/>가 부릅니다.</summary>
    internal static class LobbySceneLayout
    {
        internal static void Validate(LobbySceneRoot root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Transform world = root.WorldRoot;
            if (world == null || world.gameObject.scene != root.gameObject.scene)
                throw new InvalidOperationException("[Lobby] 같은 씬의 worldRoot가 필요합니다.");
            ValidatePose(world, "worldRoot");
            if (Vector3.Distance(world.lossyScale, Vector3.one) > 0.0001f ||
                Vector3.Distance(world.up, Vector3.up) > 0.0001f)
                throw new InvalidOperationException("[Lobby] worldRoot는 월드 축척 1과 Y축 회전만 지원합니다.");

        }

        private static void ValidatePose(Transform point, string field)
        {
            Quaternion rotation = point.rotation;
            if (!ViewPrefabRules.IsFinite(point.position) || !ViewPrefabRules.IsFinite(point.lossyScale) ||
                !ViewPrefabRules.IsFinite(rotation))
                throw new InvalidOperationException($"[Lobby] {field}의 위치·회전·축척은 유한해야 합니다.");
        }
    }
}
