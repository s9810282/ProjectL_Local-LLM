using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.ServiceKeys;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectL.Tests.Framework
{
    /// <summary>실제 Boot·DB·Core로 StageSelect 맵의 전투 노드를 입력으로 클릭해 Combat 씬까지 진입하는지 확인합니다.</summary>
    public sealed class StageMapCombatEntryBootSmokeTests
    {
        [UnityTest]
        public IEnumerator BootToStageSelect_ClickReachableCombatNode_EntersCombatScene()
        {
            // Boot 씬의 공용 시작 구간과 관계없이 StageSelect부터 시작합니다(이 PC 덮어쓰기, TearDown에서 복원).
            BootStageStartOverride.UseStageStart();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneIndices.BootPath);
            yield return new EnterPlayMode();

            float deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().buildIndex != SceneIndices.Stage && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.EqualTo(SceneIndices.Stage));

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var runtime = (FrameworkRuntime)typeof(GameBootstrap).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var faults = (RuntimeFaultBoundary)typeof(GameBootstrap).GetField("_faults", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var buffers = runtime.GetService(BufferSystemKey.Buffer);
            var map = Object.FindFirstObjectByType<StageMapSceneRoot>();

            // 새 Run(로스터)과 맵 표시가 준비될 때까지 기다립니다.
            deadline = Time.realtimeSinceStartup + 30f;
            while ((buffers.CharacterReadSink.Owned.Count == 0 || map.MapRoot.childCount == 0 || !buffers.CurrentProgressReadSink.HasMap) &&
                !faults.IsFaulted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            Assert.That(buffers.CharacterReadSink.Owned.Count, Is.GreaterThan(0), "StageSelect 첫 진입에서 새 Run 로스터가 준비돼야 합니다.");

            // 선택이 없는 첫 이동: 깊이 0 시작 노드의 다음 노드 중 전투 노드를 고릅니다.
            Assert.That(buffers.StageProgressReadSink, Is.Not.Null, "StageProgressReadSink");
            int mapKey = buffers.CurrentProgressReadSink.CurrentMapKey;
            var nodes = buffers.StageProgressReadSink.GetStages(mapKey);
            Assert.That(nodes, Is.Not.Null.And.Not.Empty, $"맵 {mapKey}의 노드가 준비돼야 합니다.");
            int[] startNext = null;
            foreach (StageProgressData node in nodes)
            {
                if (node.Depth != 0) continue;
                startNext = node.NextNodeIds.ToArray();
                break;
            }
            Assert.That(startNext, Is.Not.Null, "깊이 0 시작 노드가 있어야 합니다.");
            int targetNodeId = 0;
            var reachableText = new System.Text.StringBuilder();
            foreach (StageProgressData node in nodes)
            {
                if (System.Array.IndexOf(startNext, node.NodeId) < 0) continue;
                reachableText.Append(node.NodeId).Append(":").Append(node.StageType).Append(" ");
                if (targetNodeId == 0 && node.StageType == StageType.Combat) targetNodeId = node.NodeId;
            }
            if (targetNodeId == 0)
                Assert.Inconclusive("이번 맵의 첫 이동 노드에 전투 노드가 없습니다: " + reachableText);

            // 실제 입력 경로: 노드 Collider의 화면 좌표로 StageSelection 포인터 누름·뗌을 보냅니다.
            Transform nodeRoot = map.MapRoot.Find($"StageNode_{targetNodeId}");
            Assert.That(nodeRoot, Is.Not.Null, $"맵에 StageNode_{targetNodeId}가 표시돼야 합니다.");
            Collider hit = nodeRoot.GetComponentsInChildren<Collider>().FirstOrDefault();
            Assert.That(hit, Is.Not.Null, "노드에는 클릭용 Collider(HitTarget)가 있어야 합니다.");
            Assert.That(Camera.main, Is.Not.Null, "StageSelect에 MainCamera 태그 카메라가 있어야 합니다.");
            Vector3 screen = Camera.main.WorldToScreenPoint(hit.bounds.center);
            var input = runtime.GetService(SystemGroupsService.Global).InputServiceCommand.InputSystemServices;
            Assert.That(input, Is.Not.Null, "InputSystemServices");
            input.Submit("StageSelection", "PointerPosition", InputPhase.Performed, InputValueData.FromVector2(screen.x, screen.y), Time.realtimeSinceStartup);
            input.Submit("StageSelection", "Select", InputPhase.Started, InputValueData.FromButton(true), Time.realtimeSinceStartup);
            input.Submit("StageSelection", "Select", InputPhase.Performed, InputValueData.FromButton(false), Time.realtimeSinceStartup);

            deadline = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().buildIndex != SceneIndices.Combat && !faults.IsFaulted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.EqualTo(SceneIndices.Combat), "전투 노드 클릭 뒤 Combat 씬으로 전환돼야 합니다.");

            // 진입한 노드가 Core 선택으로 기록되고 전투가 준비됐는지 확인합니다.
            Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.True);
            Assert.That(buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(targetNodeId));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True, "진입한 전투가 준비돼 있어야 합니다.");
            for (int frame = 0; frame < 60; frame++)
            {
                Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.playModeStartScene = null;
            BootStageStartOverride.Restore();
        }
    }
}
