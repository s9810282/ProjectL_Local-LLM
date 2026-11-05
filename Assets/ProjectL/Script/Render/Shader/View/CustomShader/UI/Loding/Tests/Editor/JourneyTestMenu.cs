using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;
using ProjectL.Render.Shader.View.CustomShader.UI.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    /// <summary>씬을 교체하지 않고 동기 NUnit 테스트를 메인 스레드에서 실행한다.</summary>
    public static class JourneyTestMenu
    {
        [MenuItem("ProjectL/Render/UI/Run Journey EditMode Tests %&t")]
        public static void Run()
        {
            NUnitTestAssemblyRunner runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(JourneyTestMenu).Assembly, new Dictionary<string, object> { { "NumberOfTestWorkers", 0 } });
            WorkItem work = WorkItem.CreateWorkItem(runner.LoadedTest, TestFilter.Empty);
            work.InitializeContext(new TestExecutionContext { Dispatcher = new MainThreadDispatcher(), IsSingleThreaded = true });
            work.Execute();
            var result = work.Result;
            if (result.PassCount + result.FailCount != runner.LoadedTest.TestCaseCount)
            {
                throw new System.InvalidOperationException("모든 여정 테스트가 완료되지 않았다.");
            }
            string root = JourneyPackageBuilder.Root + "/Verification/";
            File.WriteAllText(root + "EditMode.xml", result.ToXml(true).OuterXml);
            File.WriteAllText(root + "EditMode-summary.txt", "실행: Unity 에디터 메인 스레드의 동기 NUnit (씬 교체 없음)\n결과: " + result.ResultState + "\n통과: " + result.PassCount + "\n실패: " + result.FailCount + "\n");
            Debug.Log("[JourneyUI] NUnit: " + result.ResultState + ", 통과 " + result.PassCount + ", 실패 " + result.FailCount);
        }
        private sealed class MainThreadDispatcher : IWorkItemDispatcher
        {
            public void Dispatch(WorkItem work) => work.Execute();
            // 동기 실행은 메뉴 호출이 끝나기 전에 모두 완료된다.
            public void CancelRun(bool force) { }
        }
    }
}
