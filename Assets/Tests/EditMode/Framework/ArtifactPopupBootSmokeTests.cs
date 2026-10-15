using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Tests.Framework
{
    /// <summary>실제 Boot·DB·StageSelect 에셋으로 팝업 열기·표시·닫기 경로를 검증합니다.</summary>
    public sealed class ArtifactPopupBootSmokeTests
    {
        [UnityTest]
        public IEnumerator BootToStageSelect_PopupDisplaysWorldBoardAndDetails_AndRestoresMap()
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
            Assert.That(bootstrap, Is.Not.Null);
            var faults = (RuntimeFaultBoundary)typeof(GameBootstrap).GetField("_faults", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var map = Object.FindFirstObjectByType<StageMapSceneRoot>();
            var root = Object.FindFirstObjectByType<ArtifactSceneRoot>();
            Assert.That(root, Is.Not.Null); Assert.DoesNotThrow(root.Validate);
            deadline = Time.realtimeSinceStartup + 30f;
            while (map.MapRoot.childCount == 0 && !faults.IsFaulted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            Assert.That(map.MapRoot.childCount, Is.GreaterThan(0));
            Click(root.PopupUI.OpenButton);
            yield return null; yield return null;
            Assert.That(root.PopupUI.Panel.activeSelf, Is.True);
            Assert.That(map.MapRoot.gameObject.activeSelf, Is.False);
            Assert.That(root.PopupView.WorldRoot.gameObject.activeSelf, Is.True);
            var background = ((GameObject)typeof(ArtifactPopupView).GetField("backgroundRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.PopupView)).GetComponent<RectTransform>();
            var corners = new Vector3[4]; background.GetWorldCorners(corners);
            Assert.That(root.OutputCamera.WorldToViewportPoint(corners[0]).x, Is.LessThanOrEqualTo(0));
            Assert.That(root.OutputCamera.WorldToViewportPoint(corners[0]).y, Is.LessThanOrEqualTo(0));
            Assert.That(root.OutputCamera.WorldToViewportPoint(corners[2]).x, Is.GreaterThanOrEqualTo(1));
            Assert.That(root.OutputCamera.WorldToViewportPoint(corners[2]).y, Is.GreaterThanOrEqualTo(1));
            Assert.That(root.PopupView.WorldRoot.GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(0));
            int floors = 0;
            foreach (Transform child in root.PopupView.WorldRoot)
                if (child.name == "ArtifactFloor(Clone)") floors++;
            Assert.That(floors, Is.EqualTo(1), "전체 Floor 모델은 하나만 표시해야 합니다.");
            var slots = root.PopupUI.GetComponentsInChildren<ArtifactPoolItemView>();
            Assert.That(slots.Length, Is.EqualTo(3));
            ExecuteEvents.Execute(slots[0].gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            TMP_Text name = (TMP_Text)typeof(ArtifactPopupUI).GetField("artifactName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.PopupUI);
            TMP_Text description = (TMP_Text)typeof(ArtifactPopupUI).GetField("effectDescription", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.PopupUI);
            Assert.That(name.text, Is.Not.Empty); Assert.That(description.text, Is.Not.Empty);
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Logs/Artifact3DVerification/popup.png"));
            int borrowed = root.PopupView.BorrowedCount;
            for (int frame = 0; frame < 60; frame++)
            {
                Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
                yield return null;
            }
            Click(root.PopupUI.CloseButton);
            Assert.That(root.PopupView.BorrowedCount, Is.Zero);
            Assert.That(root.PopupUI.Panel.activeSelf, Is.False);
            Assert.That(root.PopupView.WorldRoot.gameObject.activeSelf, Is.False);
            Assert.That(map.MapRoot.gameObject.activeSelf, Is.True);
            Click(root.PopupUI.OpenButton); yield return null;
            Assert.That(root.PopupView.BorrowedCount, Is.EqualTo(borrowed));
            Assert.That(root.PopupUI.OpenButton.GetComponent<UIInputBinding>(), Is.Not.Null);
            Assert.That(root.PopupUI.OpenButton.onClick.GetPersistentEventCount(), Is.Zero);
            // 뒤로가기 버튼은 UISystem을 통해 팝업을 닫습니다.
            var runtime = (FrameworkRuntime)typeof(GameBootstrap).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var input = runtime.GetService(SystemGroupsService.Global).InputServiceCommand.InputSystemServices;
            var layers = runtime.GetService(SystemGroupsService.Global).InputServiceCommand.InputLayerServices;
            Assert.That(layers.CurrentLayer, Is.EqualTo(ArtifactInputLayers.Artifact));
            Click(root.PopupUI.CloseButton);
            yield return null;
            Assert.That(root.PopupUI.Panel.activeSelf, Is.False);
            Assert.That(root.PopupView.BorrowedCount, Is.Zero);
            Assert.That(map.MapRoot.gameObject.activeSelf, Is.True);
            Assert.That(layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact), Is.False, "뒤로가기 버튼으로 닫으면 맵 입력으로 돌아갑니다.");
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FrameworkInput_PreviewsPlacesRotatesMovesReturns_UsingActualCoreAndDatabase()
        {
            // Boot 씬의 공용 시작 구간과 관계없이 StageSelect부터 시작합니다(이 PC 덮어쓰기, TearDown에서 복원).
            BootStageStartOverride.UseStageStart();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneIndices.BootPath);
            yield return new EnterPlayMode();
            float deadline = Time.realtimeSinceStartup + 30;
            while (SceneManager.GetActiveScene().buildIndex != SceneIndices.Stage && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.EqualTo(SceneIndices.Stage));
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var runtime = (FrameworkRuntime)typeof(GameBootstrap).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var faults = (RuntimeFaultBoundary)typeof(GameBootstrap).GetField("_faults", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bootstrap);
            var buffers = runtime.GetService(BufferSystemKey.Buffer);
            while (!buffers.ArtifactReadSink.Current.IsInitialized && Time.realtimeSinceStartup < deadline) yield return null;
            var root = Object.FindFirstObjectByType<ArtifactSceneRoot>();
            Click(root.PopupUI.OpenButton); yield return null;
            var input = runtime.GetService(SystemGroupsService.Global).InputServiceCommand.InputSystemServices;
            var slot = runtime.GetService(ArtifactInputKey.ReceiverSlot);
            var system = (ArtifactViewSystem)slot.GetType().GetField("_receiver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(slot);
            Assert.That(system, Is.Not.Null);
            var pool = root.PopupUI.GetComponentsInChildren<ArtifactPoolItemView>();
            int poolCount = buffers.ArtifactReadSink.Current.PoolItems.Count;
            Vector2 inventoryPoint = RectTransformUtility.WorldToScreenPoint(null, pool[0].transform.position);
            var mapper = new ArtifactBoardMapper(root.PopupView.WorldRoot, root.PopupView.Configuration);
            Vector2 boardPoint = root.OutputCamera.WorldToScreenPoint(root.PopupView.WorldRoot.TransformPoint(mapper.CellPosition(1, 1, 4, 4)));
            Pointer(input, inventoryPoint); Button(input, true);
            Pointer(input, boardPoint);
            input.Submit("Artifact", "RotateRight", InputPhase.Performed, InputValueData.FromButton(true), Time.realtimeSinceStartup);
            deadline = Time.realtimeSinceStartup + 10;
            while (system.Scheduler.LatestPreview == null && !faults.IsFaulted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            Assert.That(system.Scheduler.LatestPreview, Is.Not.Null);
            var firstPreview = system.Scheduler.LatestPreview;
            Assert.That(firstPreview.Rotation, Is.EqualTo(Rotation4.R90));
            Assert.That(firstPreview.Result.FailureReason, Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.ArtifactActionFailureReason.None));
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(system.Scheduler.LatestPreview, Is.SameAs(firstPreview));
            Assert.That(root.PopupView.WorldRoot.Find("ArtifactPreviewCells").gameObject.activeSelf, Is.True);
            for (int warm = 0; warm < 10; warm++) system.Tick(0);
            long allocationBefore = System.GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < 1000; tick++) system.Tick(0);
            long stableTickAllocation = System.GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
            Assert.That(stableTickAllocation, Is.Zero, "고정된 후보의 View Tick은 새 요청·모델·배열을 만들지 않습니다.");
            Button(input, false);
            yield return WaitEdit(system, faults);
            Assert.That(buffers.ArtifactReadSink.Current.PlacedItems.Count, Is.EqualTo(1));
            Assert.That(buffers.ArtifactReadSink.Current.PoolItems.Count, Is.EqualTo(poolCount - 1));
            ArtifactData placed = buffers.ArtifactReadSink.Current.PlacedItems[0];
            Assert.That(placed.Rotation, Is.EqualTo(Rotation4.R90));
            var remaining = root.PopupUI.GetComponentsInChildren<ArtifactPoolItemView>();
            Pointer(input, RectTransformUtility.WorldToScreenPoint(null, remaining[0].transform.position)); Button(input, true);
            Pointer(input, boardPoint);
            deadline = Time.realtimeSinceStartup + 10;
            while (system.Scheduler.LatestPreview == null && !faults.IsFaulted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(system.Scheduler.LatestPreview.Result.FailureReason, Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.ArtifactActionFailureReason.CellOccupied));
            yield return null;
            var status = (TMP_Text)typeof(ArtifactPopupUI).GetField("statusText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root.PopupUI);
            Assert.That(status.text, Is.Not.Empty);
            input.Submit("Artifact", "Select", InputPhase.Canceled, InputValueData.FromButton(false), Time.realtimeSinceStartup);
            Button(input, false);
            Assert.That(status.text, Is.Empty, "취소하면 프리뷰 전용 오류 문구도 없어집니다.");
            Vector2 grabbed = root.OutputCamera.WorldToScreenPoint(root.PopupView.WorldRoot.TransformPoint(mapper.CellPosition(placed.Cells[0].X, placed.Cells[0].Y, 4, 4)));
            var acceptedEdit = system.Scheduler.CurrentEdit;
            Pointer(input, grabbed); Button(input, true);
            Pointer(input, RectTransformUtility.WorldToScreenPoint(null, root.PopupUI.CloseButton.transform.position));
            Button(input, false); yield return null;
            Assert.That(system.Scheduler.CurrentEdit, Is.SameAs(acceptedEdit), "버튼 위 해제는 편집 요청을 만들지 않습니다.");
            Pointer(input, grabbed); Button(input, true);
            input.Submit("Artifact", "Select", InputPhase.Canceled, InputValueData.FromButton(false), Time.realtimeSinceStartup);
            Button(input, false); yield return null;
            Assert.That(system.Scheduler.CurrentEdit, Is.SameAs(acceptedEdit), "강제 입력 취소는 Return이 아닙니다.");
            Pointer(input, grabbed); Button(input, true);
            // 이 테스트는 Framework 라우터에 입력을 제출하므로 Unity ActionMap.Disable의 Canceled도 명시적으로 재현합니다.
            input.Submit("Artifact", "Select", InputPhase.Canceled, InputValueData.FromButton(false), Time.realtimeSinceStartup);
            Click(root.PopupUI.CloseButton);
            Assert.That(root.PopupView.WorldRoot.Find("ArtifactPreviewCells").gameObject.activeSelf, Is.False);
            Click(root.PopupUI.OpenButton); yield return null;
            Assert.That(buffers.ArtifactReadSink.Current.PlacedItems.Count, Is.EqualTo(1), "닫기·재열기는 확정 배치를 보존합니다.");
            Pointer(input, grabbed); Button(input, true);
            Vector2 next = root.OutputCamera.WorldToScreenPoint(root.PopupView.WorldRoot.TransformPoint(mapper.CellPosition(2, 2, 4, 4)));
            Pointer(input, next);
            input.Submit("Artifact", "RotateLeft", InputPhase.Performed, InputValueData.FromButton(true), Time.realtimeSinceStartup);
            Button(input, false); yield return WaitEdit(system, faults);
            placed = buffers.ArtifactReadSink.Current.PlacedItems[0];
            Assert.That(placed.Rotation, Is.EqualTo(Rotation4.R0));
            grabbed = root.OutputCamera.WorldToScreenPoint(root.PopupView.WorldRoot.TransformPoint(mapper.CellPosition(placed.Cells[0].X, placed.Cells[0].Y, 4, 4)));
            Pointer(input, grabbed); Button(input, true);
            input.Submit("Artifact", "Cancel", InputPhase.Performed, InputValueData.FromButton(true), Time.realtimeSinceStartup);
            Button(input, false); yield return WaitEdit(system, faults);
            Assert.That(buffers.ArtifactReadSink.Current.PlacedItems.Count, Is.Zero);
            Assert.That(buffers.ArtifactReadSink.Current.PoolItems.Count, Is.EqualTo(poolCount));
            var completed = system.Scheduler.CurrentEdit;
            var reopenedSlots = root.PopupUI.GetComponentsInChildren<ArtifactPoolItemView>();
            Pointer(input, RectTransformUtility.WorldToScreenPoint(null, reopenedSlots[0].transform.position)); Button(input, true);
            Pointer(input, boardPoint); yield return null;
            system.StopInput();
            Assert.That(root.PopupView.WorldRoot.Find("ArtifactPreviewCells").gameObject.activeSelf, Is.False);
            Assert.That(system.Scheduler.LatestPreview, Is.Null);
            Assert.That(system.Scheduler.CurrentEdit, Is.SameAs(completed));
            Click(root.PopupUI.CloseButton);
            Assert.That(root.PopupView.BorrowedCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        private static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }
        private static void Pointer(IInputSystemService input, Vector2 position)
        {
            input.Submit("Artifact", "PointerPosition", InputPhase.Performed, InputValueData.FromVector2(position.x, position.y), Time.realtimeSinceStartup);
        }
        private static void Button(IInputSystemService input, bool pressed)
        {
            input.Submit("Artifact", "Select", pressed ? InputPhase.Started : InputPhase.Performed, InputValueData.FromButton(pressed), Time.realtimeSinceStartup);
        }
        private static IEnumerator WaitEdit(ArtifactViewSystem system, RuntimeFaultBoundary faults)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (system.Scheduler.IsEditPending && !faults.IsFaulted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(faults.IsFaulted, Is.False, faults.FirstError?.ToString());
            Assert.That(system.Scheduler.CurrentEdit.Result.HasValue, Is.True);
            Assert.That(system.Scheduler.CurrentEdit.Result.Value.Succeeded, Is.True, system.Scheduler.CurrentEdit.Result.Value.FailureReason.ToString());
            yield return null;
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
